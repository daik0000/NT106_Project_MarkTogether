# MarkTogether LB Deployment Guide (Detailed Runbook)

Tai lieu nay huong dan deploy mo hinh LB + 2 app servers + Redis session store cho MarkTogether.

## 0) Muc tieu va mo hinh

Muc tieu:
- Client ket noi vao LB duy nhat: `<LB_IP>:5000`.
- LB route request theo `docID` de giu cung mot document tren cung backend.
- Request khong lien quan document (login/logout/forgot...) phan phoi theo least-connections.
- Session dang nhap duoc chia se qua Redis de hop le tren nhieu app servers.
- TLS:
  - Client -> LB: TLS
  - LB -> App Server: TLS (re-encrypt)

## 1) Bien gia tri can thay truoc khi lam

Thay toan bo gia tri mau ben duoi:

```text
LB_IP=<lb-server-ip>
APP1_IP=<app-server-1-ip>
APP2_IP=<app-server-2-ip>    # neu chua co thi de trong va bo qua buoc APP2
DB_REDIS_IP=<db-redis-server-ip>
REDIS_PASSWORD=<strong-password>
LB_PFX_PATH_LOCAL=<duong-dan-lb.pfx-tren-may-ban>
LB_PFX_PASSWORD=<password-pfx>
APP_PFX_PATH_LOCAL=<duong-dan-app-cert.pfx-tren-may-ban>
APP_PFX_PASSWORD=<password-pfx>
```

Luu y:
- Cac placeholder dang `<...>` can duoc thay bang IP/secret that cua moi server truoc khi chay.
- Hien tai neu ban moi co 1 app server, van deploy duoc voi `APP1_IP`.
- Sau nay them `APP2_IP` chi can update `MARKTOGETHER_LB_BACKENDS` va firewall.

## 2) Build artifact tren may Windows

Dung lenh sau (quan trong: `/m:1`):

```powershell
dotnet build MarkTogether.sln -c Release /m:1
```

Artifact can dung:
- App server: `MarkTogether.Server/bin/Release/*`
- LB: `LoadBalancing/MarkTogether.Gateway/bin/Release/*`

Khuyen nghi dong goi:
```powershell
Compress-Archive -Path MarkTogether.Server\bin\Release\* -DestinationPath server-release.zip -Force
Compress-Archive -Path LoadBalancing\MarkTogether.Gateway\bin\Release\* -DestinationPath lb-release.zip -Force
```

## 3) Cai Redis tren DB VPS (chay chung DB)

### 3.1 Cai va bat dich vu

```bash
sudo apt update
sudo apt install -y redis-server
sudo systemctl enable redis-server
```

### 3.2 Cau hinh redis

Mo file:
```bash
sudo nano /etc/redis/redis.conf
```

Cac dong quan trong:
```text
bind 127.0.0.1 <DB_REDIS_PRIVATE_IP>
port 6379
requirepass <REDIS_PASSWORD>
appendonly yes
save 900 1
maxmemory 512mb
maxmemory-policy allkeys-lru
```

Khoi dong lai:
```bash
sudo systemctl restart redis-server
sudo systemctl status redis-server --no-pager
```

### 3.3 Firewall Redis

```bash
sudo ufw allow from <APP1_IP> to any port 6379 proto tcp
# neu da co APP2
sudo ufw allow from <APP2_IP> to any port 6379 proto tcp
sudo ufw deny 6379/tcp
```

## 4) Deploy tren App Server 1 (APP1)

### 4.1 Chuan bi runtime + thu muc

```bash
sudo apt update
sudo apt install -y mono-complete rsync
sudo adduser --system --group --home /opt/marktogether marktogether || true
sudo mkdir -p /opt/marktogether/server /opt/marktogether/storage /opt/marktogether/logs /etc/marktogether/certs
```

### 4.2 Copy artifact va cert

Tu may local:
```bash
scp server-release.zip user@<APP1_IP>:/tmp/
scp <APP_PFX_PATH_LOCAL> user@<APP1_IP>:/tmp/app.pfx
scp deploy/env/marktogether.env.example user@<APP1_IP>:/tmp/marktogether.env.example
scp deploy/systemd/marktogether.service user@<APP1_IP>:/tmp/marktogether.service
```

Tren APP1:
```bash
sudo unzip -o /tmp/server-release.zip -d /opt/marktogether/server
sudo cp /tmp/app.pfx /etc/marktogether/certs/app.pfx
sudo chown -R marktogether:marktogether /opt/marktogether
sudo chmod 600 /etc/marktogether/certs/app.pfx
```

### 4.3 Tao env app

```bash
sudo mkdir -p /etc/marktogether
sudo cp /tmp/marktogether.env.example /etc/marktogether/env
sudo nano /etc/marktogether/env
```

Noi dung toi thieu:
```text
MARKTOGETHER_PORT=5000
MARKTOGETHER_STORAGE_PATH=/opt/marktogether/storage
MARKTOGETHER_DB_CONNECTION=Host=<DB_REDIS_IP>;Port=5432;Database=marktogether_db;Username=marktogether_user;Password=<db-password>
MARKTOGETHER_REDIS_CONNECTION=<DB_REDIS_IP>:6379,password=<REDIS_PASSWORD>,ssl=false,abortConnect=false
MARKTOGETHER_SESSION_TTL_SECONDS=86400
MARKTOGETHER_GEMINI_KEY=
```

TLS app server duoc doc tu `MarkTogether.Server.exe.config` (`TlsCertPath`, `TlsCertPassword`).
Dam bao file config tren server dang tro den:
- Path: `/etc/marktogether/certs/app.pfx`
- Password: `<APP_PFX_PASSWORD>`

### 4.4 Cai service app

```bash
sudo cp /tmp/marktogether.service /etc/systemd/system/marktogether.service
sudo systemctl daemon-reload
sudo systemctl enable marktogether
sudo systemctl restart marktogether
sudo systemctl status marktogether --no-pager
```

### 4.5 Firewall app

```bash
sudo ufw allow from <LB_IP> to any port 5000 proto tcp
sudo ufw deny 5000/tcp
```

## 5) Deploy App Server 2 (APP2) - khi ban san sang

Lam y chang APP1:
- Copy artifact
- Copy cert
- Cau hinh `/etc/marktogether/env` (DB + Redis giong APP1)
- Bat service `marktogether`
- Mo firewall chi cho LB

Sau do update LB backend pool (Buoc 6.4).

## 6) Deploy LB tren VPS `<LB_IP>`

### 6.1 Chuan bi runtime + thu muc

```bash
sudo apt update
sudo apt install -y mono-complete rsync
sudo adduser --system --group --home /opt/marktogether marktogether || true
sudo mkdir -p /opt/marktogether/lb /opt/marktogether/logs /etc/marktogether/certs /etc/marktogether
```

### 6.2 Copy artifact LB + cert LB

Tu may local:
```bash
scp lb-release.zip user@<LB_IP>:/tmp/
scp <LB_PFX_PATH_LOCAL> user@<LB_IP>:/tmp/lb.pfx
scp deploy/env/marktogether-lb.env.example user@<LB_IP>:/tmp/marktogether-lb.env.example
scp deploy/systemd/marktogether-lb.service user@<LB_IP>:/tmp/marktogether-lb.service
```

Tren LB:
```bash
sudo unzip -o /tmp/lb-release.zip -d /opt/marktogether/lb
sudo cp /tmp/lb.pfx /etc/marktogether/certs/lb.pfx
sudo chown -R marktogether:marktogether /opt/marktogether
sudo chmod 600 /etc/marktogether/certs/lb.pfx
```

### 6.3 Cau hinh env LB

```bash
sudo cp /tmp/marktogether-lb.env.example /etc/marktogether/lb.env
sudo nano /etc/marktogether/lb.env
```

Neu moi co 1 app server:
```text
MARKTOGETHER_LB_LISTEN_HOST=0.0.0.0
MARKTOGETHER_LB_LISTEN_PORT=5000
MARKTOGETHER_LB_CERT_PATH=/etc/marktogether/certs/lb.pfx
MARKTOGETHER_LB_CERT_PASSWORD=<LB_PFX_PASSWORD>
MARKTOGETHER_LB_BACKENDS=<APP1_IP>:5000
MARKTOGETHER_LB_UPSTREAM_TLS=true
MARKTOGETHER_LB_HEALTHCHECK_INTERVAL_SECONDS=5
MARKTOGETHER_LB_DOC_REMAP_TIMEOUT_SECONDS=120
MARKTOGETHER_LB_CONNECT_TIMEOUT_MS=5000
```

Khi co APP2, doi thanh:
```text
MARKTOGETHER_LB_BACKENDS=<APP1_IP>:5000,<APP2_IP>:5000
```

### 6.4 Cai service LB

```bash
sudo cp /tmp/marktogether-lb.service /etc/systemd/system/marktogether-lb.service
sudo systemctl daemon-reload
sudo systemctl enable marktogether-lb
sudo systemctl restart marktogether-lb
sudo systemctl status marktogether-lb --no-pager
```

### 6.5 Firewall LB

```bash
sudo ufw allow 5000/tcp
```

## 7) Lay CERT_THUMB cua cert tren LB

Tren LB:

```bash
openssl pkcs12 -in /etc/marktogether/certs/lb.pfx -clcerts -nokeys | openssl x509 -noout -fingerprint -sha1
```

Ket qua vi du:
```text
sha1 Fingerprint=AA:BB:CC:DD:...
```

Gia tri `CERT_THUMB` trong client phai la chuoi da bo dau `:`:
```text
AABBCCDD...
```

## 8) Client rollout

Sua file `MarkTogether.Client/server.config`:

```text
HOST=<LB_IP>
PORT=5000
CERT_THUMB=<LB_CERT_THUMBPRINT_NO_COLON>
```

Luu y:
- Production nen pin cert (khong de trong `CERT_THUMB`).
- Neu thay cert tren LB, phai cap nhat lai `CERT_THUMB` trong client.

## 9) Checklist verify sau deploy

### 9.1 Health check service

Tren APP1/APP2:
```bash
sudo systemctl status marktogether --no-pager
```

Tren LB:
```bash
sudo systemctl status marktogether-lb --no-pager
sudo journalctl -u marktogether-lb -n 200 --no-pager
```

### 9.2 Chuc nang co ban

1. Login/logout thanh cong qua LB.
2. Mo cung 1 document tren 2 client, realtime edit/chat/comment van dong bo.
3. AI request van response duoc.

### 9.3 Session cross-server

- Login voi client A.
- Thuc hien request auth-sensitive khi LB co the route qua backend khac.
- Ket qua phai pass (Redis session shared OK).

### 9.4 Failover test

1. Tat APP1: `sudo systemctl stop marktogether` tren APP1.
2. Kiem tra LB log backend unhealthy.
3. Sau `MARKTOGETHER_LB_DOC_REMAP_TIMEOUT_SECONDS`, document co the remap qua backend healthy.

## 10) Troubleshooting nhanh

### A. `dotnet build MarkTogether.sln -c Release` fail im lang (0 error)

Dung lenh:
```powershell
dotnet build MarkTogether.sln -c Release /m:1
```

### B. Client bao loi TLS / cert mismatch

- Kiem tra `CERT_THUMB` dung cert LB hien tai.
- Kiem tra da bo dau `:` trong thumbprint.

### C. Login duoc nhung request tiep theo bi "phien dang nhap khong hop le"

- Kiem tra `MARKTOGETHER_REDIS_CONNECTION` tren tat ca app servers.
- Kiem tra Redis reachable tu APP1/APP2 (firewall + password).

### D. LB len service nhung khong route duoc

- Kiem tra `MARKTOGETHER_LB_BACKENDS` dung IP:PORT.
- Kiem tra APP firewall chi allow LB IP vao port 5000.
- Kiem tra cert app backend hop le neu `MARKTOGETHER_LB_UPSTREAM_TLS=true`.

## 11) Rollback nhanh

Neu can rollback ngay:

1. Tren client, tro lai endpoint cu (neu co).
2. Tren LB, stop service:
```bash
sudo systemctl stop marktogether-lb
```
3. Tren app, deploy lai release truoc do (neu can).
4. Restore config tu backup.

Khuyen nghi: luon giu 1 ban backup artifact + env file truoc moi lan deploy.
