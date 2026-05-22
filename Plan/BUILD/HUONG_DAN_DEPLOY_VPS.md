# Phase 6 — Production Deployment Architecture trên 2 Server

## 1. Mục tiêu phase

Phase này triển khai production deployment cho MarkTogether theo mô hình **2 VPS tách riêng App Server và Database Server**:

- App Server chạy MarkTogether Server bằng `systemd` + Mono.
- Database Server chạy PostgreSQL production qua Docker.
- Không hardcode secret trong code/config commit.
- App Server đọc cấu hình qua environment:
  - `MARKTOGETHER_PORT`
  - `MARKTOGETHER_DB_CONNECTION`
  - `MARKTOGETHER_STORAGE_PATH`
- PostgreSQL chỉ mở port `5432` cho đúng App VPS, không mở public cho toàn internet.
- Có script migration cho database đã tồn tại.
- Có backup database + storage.
- Có quy trình deploy Release từ Windows lên App VPS an toàn hơn quy trình copy thủ công.

## 2. Thông tin 2 server production

```text
DB VPS  : 67.205.130.221
App VPS : 159.203.184.87
```

Vai trò:

| Server | Vai trò | Thành phần chạy |
|---|---|---|
| `67.205.130.221` | Database Server | Docker, PostgreSQL 16, migration, DB backup |
| `159.203.184.87` | Application Server | Mono, MarkTogether.Server.exe, systemd, storage, app backup |

Luồng kết nối:

```text
Client WinForms
  -> TCP 159.203.184.87:5000
      -> MarkTogether Server trên App VPS
          -> PostgreSQL 67.205.130.221:5432
```

## 3. Phân tích codebase hiện tại

### Runtime server

File liên quan:

- `MarkTogether.Server/Program.cs`
- `MarkTogether.Server/Network/SocketServer.cs`

Server đã hỗ trợ:

- Đọc `MARKTOGETHER_PORT`.
- Fallback về `5000` nếu không set hoặc giá trị không hợp lệ.
- Giữ `--service` để chạy dưới `systemd`.

Backward compatibility:

- Local dev không cần đổi gì.
- Nếu không set env, server vẫn chạy port `5000`.

### Database connection

File liên quan:

- `MarkTogether.Server/Database/DbConnectionFactory.cs`

Code đã hỗ trợ:

```csharp
Environment.GetEnvironmentVariable("MARKTOGETHER_DB_CONNECTION")
```

và fallback về `App.config`, sau đó fallback connection string local cũ.

Production 2 server chỉ cần set trên **App VPS**:

```text
MARKTOGETHER_DB_CONNECTION=Host=67.205.130.221;Port=5432;Database=marktogether_db;Username=marktogether_user;Password=CHANGE_ME_STRONG_PASSWORD
```

### Image/media storage

File liên quan:

- `MarkTogether.Server/Services/ImageStorageService.cs`

Server đã hỗ trợ:

- Đọc `MARKTOGETHER_STORAGE_PATH`.
- Fallback về `<AppDomain.BaseDirectory>/Storage` cho dev.
- Production dùng `/opt/marktogether/storage` trên **App VPS**.

Storage nằm ở App VPS vì ảnh được app đọc/ghi trực tiếp. DB VPS chỉ lưu metadata trong PostgreSQL.

## 4. File deployment liên quan

### `docker-compose.prod.yml`

Dùng trên **DB VPS** để chạy PostgreSQL.

Đã chỉnh theo hướng 2 server:

```yaml
ports:
  - "${MARKTOGETHER_DB_BIND_ADDRESS:-127.0.0.1}:${MARKTOGETHER_DB_PORT:-5432}:5432"
```

Khi deploy 2 server, file `.env` trên DB VPS cần set:

```text
MARKTOGETHER_DB_BIND_ADDRESS=0.0.0.0
```

> Chỉ dùng `0.0.0.0` khi firewall DB VPS đã allowlist `159.203.184.87` vào port `5432`.

### `deploy/env/marktogether.env.example`

Dùng trên **App VPS** làm mẫu cho `/etc/marktogether/env`.

Đã chỉnh connection string sang DB VPS:

```text
MARKTOGETHER_DB_CONNECTION=Host=67.205.130.221;Port=5432;Database=marktogether_db;Username=marktogether_user;Password=CHANGE_ME_STRONG_PASSWORD
```

### `deploy/systemd/marktogether.service`

Dùng trên **App VPS**.

Nhiệm vụ:

- Chạy `/usr/bin/mono /opt/marktogether/server/MarkTogether.Server.exe --service`.
- Load env từ `/etc/marktogether/env`.
- Restart tự động.
- Hardening bằng `NoNewPrivileges`, `ProtectSystem=strict`, `PrivateTmp`.
- Cho phép ghi vào:
  - `/opt/marktogether/storage`
  - `/opt/marktogether/backups`
  - `/opt/marktogether/logs`

Lưu ý cho mô hình 2 server:

- Service file đã bỏ phụ thuộc `docker.service`.
- App VPS không cần cài Docker nếu chỉ chạy MarkTogether Server.
- Docker chỉ bắt buộc trên DB VPS để chạy PostgreSQL container.

### `deploy/scripts/run-migrations.sh`

Dùng trên **DB VPS**, vì script chạy `docker exec` vào container PostgreSQL local.

Không chạy script này trên App VPS trong mô hình 2 server.

### `deploy/scripts/backup.sh`

Dùng theo 2 phần:

- DB backup: chạy trên **DB VPS** vì cần `docker exec marktogether-postgres`.
- Storage backup: chạy trên **App VPS** vì storage nằm tại `/opt/marktogether/storage`.

Script hiện tại backup DB container local + storage local. Trong mô hình 2 server, có 2 cách dùng:

1. Chạy trên DB VPS để backup DB.
2. Chạy trên App VPS chỉ nên dùng cho storage nếu có chỉnh/tách script riêng.

Khuyến nghị thực tế: dùng lệnh backup riêng ở từng VPS như hướng dẫn bên dưới để tránh nhầm server.

### `deploy/scripts/deploy-release.sh`

Dùng trên **App VPS**.

Nhiệm vụ:

- Deploy release folder vào `/opt/marktogether/server`.
- Backup release cũ.
- Restart `marktogether.service`.

## 5. Setup lần đầu trên DB VPS

SSH vào DB VPS:

```bash
ssh root@67.205.130.221
```

Cài dependency:

```bash
apt update
apt install -y docker.io docker-compose-plugin ufw
systemctl enable --now docker
```

Tạo thư mục:

```bash
mkdir -p /opt/marktogether/app
mkdir -p /opt/marktogether/backups
mkdir -p /opt/marktogether/logs
mkdir -p /var/lib/marktogether/pg_data
```

Copy deployment files từ Windows local lên DB VPS:

```cmd
scp docker-compose.prod.yml root@67.205.130.221:/opt/marktogether/app/
scp -r MarkTogether.Server\Database\Scripts root@67.205.130.221:/opt/marktogether/app/MarkTogether.Server/Database/
scp -r deploy root@67.205.130.221:/opt/marktogether/app/
```

Tạo `/opt/marktogether/app/.env` trên DB VPS:

```bash
cat > /opt/marktogether/app/.env <<'EOF'
MARKTOGETHER_DB_NAME=marktogether_db
MARKTOGETHER_DB_USER=marktogether_user
MARKTOGETHER_DB_PASSWORD=123
MARKTOGETHER_DB_PORT=5432

# 2-server mode: PostgreSQL container listens on all interfaces,
# firewall below restricts access to App VPS only.
MARKTOGETHER_DB_BIND_ADDRESS=0.0.0.0
EOF

chmod 600 /opt/marktogether/app/.env
```

Start PostgreSQL:

```bash
cd /opt/marktogether/app
docker compose -f docker-compose.prod.yml --env-file .env up -d
docker ps
docker exec marktogether-postgres pg_isready -U marktogether_user -d marktogether_db
```

Firewall DB VPS:

```bash
ufw allow OpenSSH
ufw allow from 159.203.184.87 to any port 5432 proto tcp
ufw deny 5432/tcp
ufw --force enable
ufw status verbose
```

Kiểm tra DB chỉ cho App VPS truy cập port `5432`.

## 6. Setup lần đầu trên App VPS

SSH vào App VPS:

```bash
ssh root@159.203.184.87
```

Cài dependency:

```bash
apt update
apt install -y mono-complete rsync ufw
```

Tạo user và thư mục:

```bash
useradd --system --home /opt/marktogether --shell /usr/sbin/nologin marktogether || true

mkdir -p /opt/marktogether/app
mkdir -p /opt/marktogether/server
mkdir -p /opt/marktogether/storage
mkdir -p /opt/marktogether/backups
mkdir -p /opt/marktogether/logs
mkdir -p /etc/marktogether

chown -R marktogether:marktogether /opt/marktogether
```

Copy deployment folder lên App VPS:

```cmd
scp -r deploy root@159.203.184.87:/opt/marktogether/app/
```

Tạo `/etc/marktogether/env` trên App VPS:

```bash
cat > /etc/marktogether/env <<'EOF'
MARKTOGETHER_PORT=5000
MARKTOGETHER_STORAGE_PATH=/opt/marktogether/storage
MARKTOGETHER_DB_CONNECTION=Host=67.205.130.221;Port=5432;Database=marktogether_db;Username=marktogether_user;Password=123
MARKTOGETHER_GEMINI_KEY=AIzaSyBGksiM3ZaIR8EMmgX9Nao-Y-OBcBFyTe8
EOF

chown root:marktogether /etc/marktogether/env
chmod 640 /etc/marktogether/env
```

> `CHANGE_ME_STRONG_PASSWORD` phải giống password trong `/opt/marktogether/app/.env` trên DB VPS.

Cài systemd service:

```bash
cp /opt/marktogether/app/deploy/systemd/marktogether.service /etc/systemd/system/marktogether.service
chmod +x /opt/marktogether/app/deploy/
rm -f /opt/marktogether/app/deploy/scripts/run-migrations.sh
scripts/*.sh
systemctl daemon-reload
systemctl enable marktogether
```

Firewall App VPS:

```bash
ufw allow OpenSSH
ufw allow 5000/tcp
ufw --force enable
ufw status verbose
```

## 7. Build Release trên Windows

Từ máy Windows local:

```cmd
cd /d D:\hoc_tren_truong\CN\lap_trinh_mang\Đồ An\NT106_Project_MarkTogether

"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" MarkTogether.Server\Server.csproj /p:Configuration=Release /t:Rebuild
```

Nếu dùng Visual Studio path khác, chỉnh path `MSBuild.exe` tương ứng.

## 8. Deploy mỗi lần update server

Upload release folder từ Windows local lên **App VPS**:

```cmd
ssh root@159.203.184.87 "rm -rf /tmp/marktogether-release && mkdir -p /tmp/marktogether-release"
scp -r MarkTogether.Server\bin\Release\* root@159.203.184.87:/tmp/marktogether-release/
```

Trên **App VPS**:

```bash
cd /opt/marktogether/app

# Backup storage trước khi restart/deploy
mkdir -p /opt/marktogether/backups/storage_latest
rsync -a --delete /opt/marktogether/storage/ /opt/marktogether/backups/storage_latest/

# Deploy release mới và restart service
./deploy/scripts/deploy-release.sh /tmp/marktogether-release
```

Nếu có migration DB, chạy migration trên **DB VPS**, không chạy trên App VPS:

```bash
ssh root@67.205.130.221
cd /opt/marktogether/app
./deploy/scripts/run-migrations.sh
```

Kiểm tra service trên App VPS:

```bash
systemctl status marktogether --no-pager
journalctl -u marktogether -f
```

## 9. Migration strategy

Docker init scripts chỉ chạy khi PostgreSQL volume mới được tạo lần đầu.

DB mới trên DB VPS:

- `schema_init.sql` và `migration_v2.sql` tự chạy qua `/docker-entrypoint-initdb.d`.

DB đã tồn tại trên DB VPS:

```bash
cd /opt/marktogether/app
./deploy/scripts/run-migrations.sh
```

`migration_v2.sql` được thiết kế idempotent bằng `IF NOT EXISTS`, nên có thể chạy lại an toàn.

## 10. Backup strategy theo 2 server

### DB backup trên DB VPS

Chạy thủ công:

```bash
ssh root@67.205.130.221
mkdir -p /opt/marktogether/backups
docker exec marktogether-postgres pg_dump -U marktogether_user marktogether_db | gzip > /opt/marktogether/backups/db_$(date +%Y%m%d_%H%M%S).sql.gz
find /opt/marktogether/backups -type f -name "db_*.sql.gz" -mtime +7 -delete
```

Cron daily 03:00 trên DB VPS:

```cron
0 3 * * * docker exec marktogether-postgres pg_dump -U marktogether_user marktogether_db | gzip > /opt/marktogether/backups/db_$(date +\%Y\%m\%d_\%H\%M\%S).sql.gz
```

### Storage backup trên App VPS

Chạy thủ công:

```bash
ssh root@159.203.184.87
mkdir -p /opt/marktogether/backups/storage_latest
rsync -a --delete /opt/marktogether/storage/ /opt/marktogether/backups/storage_latest/
```

Cron daily 03:10 trên App VPS:

```cron
10 3 * * * rsync -a --delete /opt/marktogether/storage/ /opt/marktogether/backups/storage_latest/ >> /opt/marktogether/logs/storage-backup.log 2>&1
```

## 11. Client configuration

Client WinForms connect App VPS.

File:

```text
MarkTogether.Client/server.config
```

Nội dung production:

```xml
<appSettings>
  <add key="ServerHost" value="159.203.184.87" />
  <add key="ServerPort" value="5000" />
</appSettings>
```

Client không connect trực tiếp DB VPS.

## 12. Security checklist

- DB VPS chỉ allow port `5432` từ App VPS `159.203.184.87`.
- DB VPS không mở PostgreSQL public cho toàn internet.
- App VPS chỉ mở SSH và TCP `5000`.
- Secret DB không commit vào git.
- `/etc/marktogether/env` trên App VPS chmod `640`.
- App chạy user riêng `marktogether`, không chạy root.
- `systemd` hardening bật `NoNewPrivileges`, `ProtectSystem=strict`.
- Storage nằm ngoài release folder để tránh mất ảnh khi deploy.
- Backup DB trên DB VPS.
- Backup storage trên App VPS.
- Không đưa mật khẩu VPS hoặc DB thật vào repo.

## 13. Rollback

Release cũ trên App VPS được backup tại:

```text
/opt/marktogether/release_backups/server_YYYYMMDD_HHMMSS/
```

Rollback app trên App VPS:

```bash
systemctl stop marktogether
rsync -a --delete /opt/marktogether/release_backups/server_YYYYMMDD_HHMMSS/ /opt/marktogether/server/
chown -R marktogether:marktogether /opt/marktogether/server
systemctl restart marktogether
journalctl -u marktogether -n 100 --no-pager
```

DB rollback trên DB VPS:

```bash
gzip -dc /opt/marktogether/backups/db_YYYYMMDD_HHMMSS.sql.gz | docker exec -i marktogether-postgres psql -U marktogether_user -d marktogether_db
```

Chỉ restore DB khi thật sự cần vì có thể ghi đè dữ liệu mới phát sinh sau backup.

## 14. Test strategy

### Local build

```cmd
MSBuild.exe MarkTogether.Server\Server.csproj /p:Configuration=Release /t:Rebuild
```

### DB VPS smoke test

```bash
docker ps
docker exec marktogether-postgres pg_isready -U marktogether_user -d marktogether_db
ss -ltnp | grep ':5432'
ufw status verbose
```

### App VPS smoke test

```bash
systemctl status marktogether --no-pager
journalctl -u marktogether -n 100 --no-pager
ss -ltnp | grep ':5000'
```

Test App VPS connect DB VPS:

```bash
nc -vz 67.205.130.221 5432
```

Nếu chưa có `nc`:

```bash
apt install -y netcat-openbsd
nc -vz 67.205.130.221 5432
```

### Functional test từ client

- Login/Register.
- List documents.
- Open document.
- Upload image.
- Share link.
- Search/profile features từ phase trước.
- Confirm image vẫn load sau service restart.

## 15. Tóm tắt thay đổi Phase 6

Phase 6 hiện được chỉnh theo hướng production deployment **2 server**:

- DB VPS `67.205.130.221` chạy PostgreSQL Docker.
- App VPS `159.203.184.87` chạy MarkTogether Server bằng systemd + Mono.
- Client chỉ connect App VPS.
- App VPS connect DB VPS bằng `MARKTOGETHER_DB_CONNECTION`.
- `docker-compose.prod.yml` hỗ trợ bind address qua `MARKTOGETHER_DB_BIND_ADDRESS`.
- `deploy/env/marktogether.env.example` đã đổi sang DB VPS.
- Migration/DB backup chạy trên DB VPS.
- App deploy/storage backup chạy trên App VPS.