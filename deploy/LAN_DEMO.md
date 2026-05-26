# MarkTogether LAN Demo Runbook

Muc tieu demo:
- 1 may trong LAN chay tat ca backend:
  - PostgreSQL: `127.0.0.1:15432`
  - Redis: `127.0.0.1:16379`
  - App server #1: `127.0.0.1:5101`
  - App server #2: `127.0.0.1:5102`
  - Load balancer: `0.0.0.0:5000`
- Client o may khac cung LAN chi ket noi vao `LAN_IP_CUA_MAY_LB:5000`.

## 1) So do

```text
Client A/B trong LAN
        |
        | TLS, port 5000
        v
LB tren may demo 0.0.0.0:5000
        |
        | TLS upstream
        +--> App #1 127.0.0.1:5101
        +--> App #2 127.0.0.1:5102
              |
              +--> PostgreSQL 127.0.0.1:15432
              +--> Redis      127.0.0.1:16379
```

LB la diem duy nhat mo ra LAN. App #1, App #2, PostgreSQL va Redis chi bind local.

## 2) Chuan bi

Can co:
- Docker Desktop hoac Docker Engine.
- `openssl` trong PATH.
- .NET SDK/MSBuild co the build solution.

Lay IP LAN cua may chay LB:

```powershell
ipconfig
```

Vi du IP LAN: `192.168.1.10`.

## 3) Build Release

```powershell
dotnet build MarkTogether.sln -c Release /m:1
```

## 4) Start PostgreSQL + Redis local

```powershell
powershell -ExecutionPolicy Bypass -File deploy/scripts/lan-demo-start-db.ps1
```

Mac dinh:
- DB: `marktogether_db`
- User: `marktogether_user`
- Password demo: `marktogether_demo_123`
- PostgreSQL host port: `15432`
- Redis host port: `16379`

## 5) Tao cert demo

Thay `<LAN_IP>` bang IP LAN cua may chay LB:

```powershell
powershell -ExecutionPolicy Bypass -File deploy/scripts/lan-demo-create-certs.ps1 -LanIp <LAN_IP>
```

Script tao:
- `certs/lb.pfx`
- `certs/app.pfx`

Script cung in ra SHA1 thumbprint cua LB cert. Client se pin thumbprint nay.

## 6) Ghi client server.config

```powershell
powershell -ExecutionPolicy Bypass -File deploy/scripts/lan-demo-write-client-config.ps1 -LanIp <LAN_IP>
```

File duoc update:
- `MarkTogether.Client/server.config`
- `MarkTogether.Client/bin/Release/server.config` neu da build Release

Neu client chay tren may khac, copy thu muc `MarkTogether.Client/bin/Release` sang may client sau khi file `server.config` da dung:

```text
HOST=<LAN_IP>
PORT=5000
CERT_THUMB=<LB_CERT_SHA1_NO_COLON>
```

## 7) Mo firewall cho LB port

Tren may chay LB, chi can mo port `5000/tcp`:

```powershell
New-NetFirewallRule -DisplayName "MarkTogether LAN LB 5000" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5000
```

Khong can mo `5101`, `5102`, `15432`, `16379` vi cac port nay chi dung noi bo.

## 8) Chay 2 app server va LB

Mo 3 terminal rieng.

Terminal 1:

```powershell
powershell -ExecutionPolicy Bypass -File deploy/scripts/lan-demo-server1.ps1
```

Terminal 2:

```powershell
powershell -ExecutionPolicy Bypass -File deploy/scripts/lan-demo-server2.ps1
```

Terminal 3:

```powershell
powershell -ExecutionPolicy Bypass -File deploy/scripts/lan-demo-lb.ps1
```

LB log mong doi:

```text
[LB] Starting on 0.0.0.0:5000
[LB] Backends: 127.0.0.1:5101, 127.0.0.1:5102
[LB] Upstream TLS: True
```

## 9) Test tu may client trong LAN

Tren may client:

```powershell
Test-NetConnection <LAN_IP> -Port 5000
```

Ket qua can co:

```text
TcpTestSucceeded : True
```

Sau do mo `MarkTogether.Client.exe`, dang ky/dang nhap, tao document, mo cung document tren client thu 2 va go noi dung de xem OT sync.

## 10) Quan sat log khi demo

LB:

```text
Client TLS established.
Received client packet: AUTH_LOGIN ...
Forwarding AUTH_LOGIN ...
Activated doc stream: doc_... -> backend#...
Forwarding OP_INSERT doc=... -> backend#...
```

App #1/#2:

```text
[SessionStore] Dung Redis session store.
[Server] Dang lang nghe tren 127.0.0.1:5101...
[Server] Dang lang nghe tren 127.0.0.1:5102...
```

## 11) Reset demo data

Neu muon xoa DB/Redis demo va tao lai tu dau:

```powershell
docker compose -f docker-compose.lan-demo.yml down -v
```

Sau do chay lai buoc 4.

## 12) Goi y khi loi

- Client timeout nhung LB khong co log: sai IP LAN, firewall chua mo port `5000`, hoac client dang tro nham `server.config`.
- LB co `Client TLS established` nhung khong `Received client packet`: client khong gui packet hoac dang dung build cu.
- LB co `Received client packet` nhung khong `Forwarding`: kiem App #1/#2 co dang chay va LB ket noi duoc `127.0.0.1:5101`, `127.0.0.1:5102`.
- Dang nhap OK nhung session bi mat khi route sang server khac: Redis chua chay hoac `MARKTOGETHER_REDIS_CONNECTION` sai.
- OT khong dong bo: dam bao hai client mo cung document va LB log forward cac packet `OP_*` ve cung backend doc-affinity.
