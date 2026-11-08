# Học tập — Vue + ASP.NET Core + Supabase PostgreSQL

Dữ liệu ứng dụng được lưu trong schema riêng `hoctap` trên Supabase PostgreSQL:
Identity (tài khoản, vai trò), phân quyền, refresh token, nhật ký, bài giảng, ngân hàng
câu hỏi và đáp án, đề thi, phiên làm bài, câu trả lời, điểm số và tiến độ leo tháp.
API tiếp tục dùng ASP.NET Identity/JWT hiện tại. Không cần Supabase Auth hay
Supabase JavaScript SDK; frontend gọi API như trước. Tùy chọn giao diện, mã khách
và token đăng nhập vẫn lưu trong trình duyệt. Nội dung chat AI vẫn thuộc phiên trình duyệt.

## 1. Kết nối Supabase

Trong Supabase Dashboard, mở **Connect** và lấy thông tin PostgreSQL. Với backend
chạy lâu dài, dùng direct connection (cần IPv6) hoặc **Session pooler** cổng 5432
(khi máy chỉ có IPv4). Không dùng transaction pooler cổng 6543 cho migration này.
Username của pooler thường là `postgres.PROJECT_REF`; sao chép chính xác host và
username trong Dashboard.

Cần .NET SDK 8 và Node.js tương thích với Vite 8. Tại thư mục dự án:

```sh
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Host=YOUR_SESSION_POOLER_HOST;Port=5432;Database=postgres;Username=postgres.YOUR_PROJECT_REF;Password=YOUR_DATABASE_PASSWORD;SSL Mode=Require;Timeout=30;Command Timeout=60' --project API/QuanLyTaiKhoanAPI
```

Cấu hình thêm khóa JWT riêng (thay placeholder bằng chuỗi ngẫu nhiên tối thiểu 32 byte):

```sh
dotnet user-secrets set 'Jwt:Key' 'YOUR_PRIVATE_RANDOM_JWT_KEY_AT_LEAST_32_BYTES' --project API/QuanLyTaiKhoanAPI
```

Repo không cung cấp khóa JWT hay mật khẩu admin dùng chung. Ứng dụng sẽ từ chối
khởi động nếu thiếu khóa JWT hợp lệ.

Giá trị này là connection string **Npgsql**, không phải project URL hay anon key.
Thay toàn bộ placeholder bằng thông tin của bạn. Với direct connection, dùng host
`db.PROJECT_REF.supabase.co` và username `postgres` theo Dashboard.
Không đưa mật khẩu database vào frontend hoặc commit vào Git.

Trên hosting, đặt biến môi trường `ConnectionStrings__DefaultConnection`.
Production cần thêm `Jwt__Key`, `App__ClientUrl`, `Cors__AllowedOrigins__0` và
cấu hình SMTP riêng. `appsettings.Production.json` không chứa connection string thật.

Tài liệu kết nối: [Supabase PostgreSQL](https://supabase.com/docs/guides/database/connecting-to-postgres).

## 2. Khởi tạo database

Nếu có SQLite cũ cần giữ, thực hiện bước 3 trước khi chạy API bình thường.
Lệnh sau chỉ áp dụng EF migration, không seed tài khoản/quyền và không mở web server:

```sh
dotnet run --project API/QuanLyTaiKhoanAPI -- --migrate-only
```

Migration chỉ tạo schema `hoctap` và các bảng của ứng dụng; không sửa schema
`auth`/`storage` của Supabase. Schema `hoctap` không dùng cho Data API. Migration
thu hồi quyền của `PUBLIC`, `anon`, `authenticated`; backend truy cập bằng tài khoản
PostgreSQL được cấp quyền riêng. Giữ schema này ngoài danh sách exposed schemas.
Không dùng `EnsureCreated` trên Supabase vì database đã chứa các schema hệ thống.

Development mặc định tự áp dụng migration lúc khởi động. Production mặc định tắt;
chạy `--migrate-only` trong bước deploy với cùng cấu hình production (bao gồm JWT),
rồi khởi động API. Chỉ chạy một tiến trình migration tại một thời điểm.

Để tạo migration tiếp theo:

```sh
dotnet tool restore
dotnet ef migrations add TenThayDoi --project API/QuanLyTaiKhoanAPI
```

## 3. Chuyển dữ liệu SQLite cũ

Công cụ import giữ nguyên ID, password hash, liên kết tài khoản/quyền, token,
nhật ký, đề thi, điểm và tiến độ. Câu hỏi từng lưu trong RAM không thể phục hồi từ
SQLite; hãy nhập lại Excel. Phiên làm bài cũ không có câu hỏi đã lưu cần bắt đầu lại.
Các câu hỏi/phiên mới sẽ được lưu bền vững trên Supabase.

1. Dừng API, sao lưu SQLite (giữ cả WAL/SHM nếu có), và giữ bản gốc cho đến khi kiểm tra xong.
2. Chạy `--migrate-only` ở bước 2. Database đích phải chưa có dữ liệu ứng dụng;
   nếu đã chạy API và seed dữ liệu, công cụ sẽ từ chối import. Dùng một database
   đích sạch hoặc xử lý dữ liệu đích riêng trước khi tiếp tục.
3. Cài công cụ vào môi trường Python riêng:

```sh
python3 -m venv .venv
.venv/bin/python -m pip install -r scripts/requirements.txt
```

Đặt `SUPABASE_DATABASE_URL` bằng URI PostgreSQL từ Dashboard (direct hoặc session
pooler), có `sslmode=require`. Biến này dành cho Python/libpq, khác định dạng Npgsql
ở bước 1. Password chứa ký tự đặc biệt cần URL-encode khi dùng URI. Dùng secret
manager của terminal/hosting để đặt biến; không ghi mật khẩu vào file trong Git.

Kiểm tra toàn bộ import rồi rollback:

```sh
.venv/bin/python scripts/migrate_sqlite_to_supabase.py API/QuanLyTaiKhoanAPI/quanlytaikhoan.db
```

Nếu kiểm tra thành công, chạy lại để ghi dữ liệu:

```sh
.venv/bin/python scripts/migrate_sqlite_to_supabase.py API/QuanLyTaiKhoanAPI/quanlytaikhoan.db --apply
```

Công cụ đọc SQLite ở chế độ chỉ đọc, kiểm tra database đích rỗng, chuyển kiểu
boolean/số/ngày UTC, kiểm tra số dòng và khóa ngoại, cập nhật identity sequences.
Toàn bộ dữ liệu đích được ghi trong một transaction; lỗi sẽ rollback. Không tự xóa
hay ghi đè dữ liệu đã có. Không sao chép migration history SQLite sang PostgreSQL.
Khởi động API sau khi import để bổ sung quyền mới, rồi kiểm tra đăng nhập bằng tài
khoản cũ, nhật ký, ranking và tiến độ trước khi bỏ bản sao lưu.

## 4. Chạy ứng dụng

```sh
npm --prefix UI ci
npm run api
```

Mở terminal thứ hai:

```sh
npm run dev
```

UI: `http://localhost:5173`; API/Swagger: `http://localhost:5080/swagger`.
Nếu dùng database mới hoàn toàn, bật `BootstrapAdmin:Enabled` và đặt username,
email, password qua user-secrets để tạo admin đầu tiên; tắt lại sau lần khởi tạo.
Nếu đã import tài khoản cũ, đăng nhập bằng tài khoản đó.

## Kiểm tra

```sh
dotnet test API/QuanLyTaiKhoanAPI.Tests
npm --prefix UI run build
python3 -m unittest discover -s scripts -p 'test_*.py'
```

Test tự động kiểm tra lưu câu hỏi/phiên thi/câu trả lời qua nhiều DbContext, xóa câu
hỏi không làm hỏng phiên thi, chống nộp lại, giới hạn truy cập kết quả theo chủ sở
hữu và SQL/schema do Npgsql sinh ra. Test lưu trữ offline dùng SQLite trong project
test; API chính chỉ dùng Npgsql. Các test này không thay thế kiểm tra tích hợp với
project Supabase thật. Sau khi cấu hình, cần chạy migration, import thử, đăng nhập,
nhập Excel, bắt đầu bài thi, khởi động lại API rồi nộp bài để xác nhận môi trường.

## Nội dung được đưa lên GitHub

Repo chứa mã nguồn, migration, test và cấu hình mẫu không chứa mật khẩu. Các file
`appsettings.Development.json`, `.env`, khóa riêng, database/backup, thư mục build
và dữ liệu trong `outputs/` được giữ cục bộ, không đưa lên GitHub. Khi clone trên
máy mới, cấu hình secrets theo hướng dẫn trên trước khi chạy ứng dụng.
