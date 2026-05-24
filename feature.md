# Feature: LB Doc-Affinity + Redis Session + Client Endpoint Config

## 1) File đã đọc

- `DOCUMENTATION.md` (kiến trúc socket, protocol packet, deploy hiện tại, session/TLS)
- `MarkTogether.sln`
- `MarkTogether.slnx`
- `MarkTogether.Shared/Packet.cs`
- `MarkTogether.Shared/PacketHelper.cs`
- `MarkTogether.Server/Program.cs`
- `MarkTogether.Server/Network/ClientHandler.cs`
- `MarkTogether.Server/Network/SessionManager.cs`
- `MarkTogether.Server/Server.csproj`
- `MarkTogether.Client/Network/SocketClient.cs`
- `MarkTogether.Client/LoginForm.cs`
- `MarkTogether.Client/RegisterForm.cs`
- `MarkTogether.Client/ForgotPasswordForm.cs`
- `MarkTogether.Client/Client.csproj`
- `MarkTogether.Client/server.config`
- `deploy/env/marktogether.env.example`
- `deploy/systemd/marktogether.service`
- `deploy/scripts/deploy-release.sh`

## 2) File đã sửa/thêm

### Sửa
- `MarkTogether.Client/Client.csproj`
- `MarkTogether.Client/ForgotPasswordForm.cs`
- `MarkTogether.Client/LoginForm.cs`
- `MarkTogether.Client/Network/SocketClient.cs`
- `MarkTogether.Client/RegisterForm.cs`
- `MarkTogether.Client/server.config`
- `MarkTogether.Server/Network/ClientHandler.cs`
- `MarkTogether.Server/Network/SessionManager.cs`
- `MarkTogether.Server/Server.csproj`
- `MarkTogether.sln`
- `MarkTogether.slnx`
- `deploy/env/marktogether.env.example`

### Thêm mới
- `MarkTogether.Client/Network/ServerEndpointConfig.cs`
- `MarkTogether.Server/Network/ISessionStore.cs`
- `MarkTogether.Server/Network/InMemorySessionStore.cs`
- `MarkTogether.Server/Network/RedisSessionStore.cs`
- `MarkTogether.Server/Network/SessionStoreFactory.cs`
- `LoadBalancing/MarkTogether.Gateway/MarkTogether.Gateway.csproj`
- `LoadBalancing/MarkTogether.Gateway/Properties/AssemblyInfo.cs`
- `LoadBalancing/MarkTogether.Gateway/GatewayConfig.cs`
- `LoadBalancing/MarkTogether.Gateway/BackendRouting.cs`
- `LoadBalancing/MarkTogether.Gateway/ProxySession.cs`
- `LoadBalancing/MarkTogether.Gateway/Program.cs`
- `LoadBalancing/MarkTogether.Gateway/gateway.config.example`
- `deploy/env/marktogether-lb.env.example`
- `deploy/systemd/marktogether-lb.service`
- `deploy/scripts/deploy-lb-release.sh`
- `deploy/LB_DEPLOYMENT.md`

## 3) Nguyên nhân gốc

1. Kiến trúc hiện tại chỉ có app server đơn lẻ, chưa có gateway LB hiểu protocol packet để route theo `docID`.
2. Session đăng nhập lưu in-memory theo process (`SessionManager`), nên khi multi-server sẽ không share được token.
3. Client đang hardcode IP server trong form login/register/forgot, không phù hợp khi đổi endpoint sang LB.
4. Nếu LB phải chuyển upstream backend, việc server xoá session khi disconnect có thể làm mất token toàn cục.
5. Build release bằng `dotnet build MarkTogether.sln` lỗi do:
   - `Server.csproj` copy `System.ValueTuple.dll` từ path VS hardcode không tồn tại.
   - `Client.csproj` yêu cầu `GenerateResource` chạy `x86` task host khi build bằng dotnet.

## 4) Cách sửa

1. Thêm project `MarkTogether.Gateway` (net472) làm application-layer TCP/TLS gateway:
   - TLS listener cho client.
   - Parse packet theo format hiện có (`PacketHelper`).
   - Route packet có `docID` bằng `doc -> backend` mapping.
   - Packet không có `docID` chọn backend theo least-connections (khi chưa bind backend).
   - Mapping doc ổn định bằng rendezvous hashing.
   - Health check backend định kỳ.
   - Failover rule: chỉ remap doc khi backend cũ không healthy và doc không còn active stream quá timeout.
   - Giới hạn 1 active doc/connection.

2. Server session store:
   - Tách interface `ISessionStore`.
   - Thêm `InMemorySessionStore` + `RedisSessionStore` + `SessionStoreFactory`.
   - `SessionManager` dùng Redis khi có `MARKTOGETHER_REDIS_CONNECTION`, fallback in-memory nếu thiếu/lỗi.
   - Thêm `RemoveOnDisconnect` policy:
     - In-memory: `true` (giữ behavior cũ).
     - Redis: `false` để không làm mất token khi upstream connection bị đóng/switch ở LB.

3. Client endpoint:
   - Thêm `ServerEndpointConfig` đọc `HOST/PORT/CERT_THUMB` từ `server.config`.
   - Thêm `SocketClient.ConnectFromConfig()`.
   - Login/Register/ForgotPassword chuyển sang `ConnectFromConfig()` (bỏ hardcode IP).
   - `server.config` mặc định trỏ `HOST=209.97.164.211`.

4. Deploy assets:
   - Bổ sung env app server cho Redis (`MARKTOGETHER_REDIS_CONNECTION`, `MARKTOGETHER_SESSION_TTL_SECONDS`).
   - Thêm systemd/env/script cho LB service.
   - Thêm hướng dẫn triển khai chi tiết tại `deploy/LB_DEPLOYMENT.md`.
5. Build-fix:
   - `Server.csproj`: target copy `System.ValueTuple.dll` chỉ chạy khi file thật sự tồn tại.
   - `Client.csproj`: thêm `GenerateResourceMSBuildArchitecture=CurrentArchitecture`.
   - Thêm `SkipGetTargetFrameworkProperties=true` cho `ProjectReference` tới Shared để ổn định build bằng dotnet.

## 5) Cách test đã chạy

### Compile check
- `dotnet msbuild MarkTogether.Server/Server.csproj /t:Compile /p:Configuration=Debug /m:1`
- `dotnet msbuild MarkTogether.Client/Client.csproj /t:Compile /p:Configuration=Debug /m:1`
- `dotnet msbuild LoadBalancing/MarkTogether.Gateway/MarkTogether.Gateway.csproj /t:Compile /p:Configuration=Debug /m:1`

Kết quả: compile pass.

### Solution release build check
- `dotnet build MarkTogether.sln -c Release /m:1`

Kết quả: build pass (2 warning cũ ở `TypeRenderForm*`, không phát sinh error).

Ghi chú môi trường local: có warning `MSB3101` (không ghi được `*.AssemblyReference.cache`) nhưng không chặn compile.

## 6) Thay đổi phát sinh / logging

- Gateway có log tối thiểu theo UTC timestamp + session id + action routing.
- Gateway log thêm packet type khi forward để debug routing doc-affinity, ví dụ `Forwarding OP_INSERT doc=... -> backend#...`.
- Gateway tránh đánh dấu active doc stream trước khi packet đầu tiên gửi thành công và khóa thao tác đóng upstream trong lúc đang gửi để giảm race `MobileAuthenticatedStream disposed`.
- Gateway xử lý việc đổi backend trên cùng socket sau `DOC_LEAVE`: upstream pump cũ không shutdown toàn bộ client session khi chính Gateway chủ động đóng upstream để switch backend.
- Session store có log mode đang dùng (Redis hoặc fallback in-memory).
- Không log dữ liệu nhạy cảm (không log password, token raw, API key raw).

## 7) Backward compatibility

- Không đổi wire format packet.
- Không đổi schema DB.
- Không đổi OT engine.
- Nếu không cấu hình Redis: server tự fallback in-memory như cũ.
