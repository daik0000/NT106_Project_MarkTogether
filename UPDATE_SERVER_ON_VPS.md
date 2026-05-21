# Quy trình ngắn mỗi lần update server

Trên Windows, build bản Release:
```cmd
cd C:\Users\Admin\source\repos\NT106_Project_MarkTogether

"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" MarkTogether.Server\Server.csproj /p:Configuration=Release /t:Rebuild
```

Copy code ở folder Server Release vào VPS:
```
scp -r MarkTogether.Server\bin\Release\* root@YOUR_VPS_APP_IP:/opt/marktogether/
```

Trên VPS APP:

```bash
systemctl restart marktogether
systemctl status marktogether
journalctl -u marktogether -f
```

---

# Nếu bị lỗi sau khi upload

## 1. Service không tồn tại

```text
Unit marktogether.service not found
```

Tạo lại service:

```bash
nano /etc/systemd/system/marktogether.service
systemctl daemon-reload
systemctl enable marktogether
systemctl start marktogether
```

## 2. Server tự dừng

Check service có `--service` chưa:

```bash
cat /etc/systemd/system/marktogether.service
```

Phải có:

```ini
ExecStart=/usr/bin/mono /opt/marktogether/MarkTogether.Server.exe --service
```

## 3. Lỗi thiếu DLL

Ví dụ:

```text
Could not load file or assembly System.Text.Json
```

Nghĩa là bạn upload thiếu DLL. Upload lại toàn bộ folder Release:

```cmd
scp -r MarkTogether.Server\bin\Release\* root@YOUR_VPS_APP_IP:/opt/marktogether/
```
