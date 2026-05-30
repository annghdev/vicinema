# 🎬 Vicinema - Movie Ticket Booking
  
![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=blue)
![React](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?style=for-the-badge&logo=postgresql)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis)
![Docker](https://img.shields.io/badge/Docker-28-2496ED?style=for-the-badge&logo=docker)

> Nền tảng đặt vé xem phim trực tuyến thời gian thực (Full-Stack) được xây dựng bằng **.NET 10** và **React 19** — được thiết kế nhằm trình bày mô hình kiến trúc Clean Architecture chuẩn production, thiết kế hướng tên miền (Domain-Driven Design - DDD) và các quy trình DevOps hiện đại.

### Đọc bằng ngôn ngữ khác
- [English/Tiếng Anh](./README.md)


## 📖 Mục Lục

- [📌 Tổng Quan](#tổng-quan)
- [🚀 Tính Năng Chính](#tính-năng-chính)
- [🛠 Công Nghệ Sử Dụng](#công-nghệ-sử-dụng)
- [🏗 Kiến Trúc](#kiến-trúc)
- [📂 Cấu Trúc Thư Mục Dự Án](#cấu-trúc-thư-mục-dự-án)
- [⚙️ Hướng Dẫn Cài Đặt](#hướng-dẫn-cài-đặt)
- [📸 Live Demo](#live-demo)
- [🖼 Hình Ảnh Nổi Bật](#hình-ảnh-nổi-bật)
- [📬 Liên Hệ](#liên-hệ)
- [📄 Bản Quyền](#bản-quyền)

---

## Tổng Quan

Cinema Ticket Booking là một ứng dụng web toàn diện (End-to-End) cho phép khách hàng duyệt phim, chọn suất chiếu, chọn ghế ngồi theo thời gian thực và thực hiện thanh toán qua cổng VNPay hoặc ví điện tử MoMo. Trang quản trị Admin (ASP.NET Core MVC) cung cấp cho các nhà vận hành rạp chiếu phim khả năng kiểm soát toàn bộ thông tin phim, phòng chiếu, lịch chiếu, chính sách giá vé và quản lý đơn đặt vé.

**Mục tiêu:**
- Mang lại trải nghiệm đặt vé thời gian thực mượt mà với cơ chế khóa giữ ghế và cập nhật trạng thái trực tiếp.
- Minh họa các mẫu kiến trúc cấp doanh nghiệp (Enterprise Patterns): Clean Architecture, CQRS, Domain Events và Unit of Work.
- Cung cấp môi trường phát triển và sản xuất được container hóa hoàn toàn, tích hợp sẵn hệ thống giám sát và đo lường (Observability).

### Báo cáo tổng hợp
Để xem phân tích chi tiết về cấu trúc dự án, bao gồm các Domain Aggregates, Wolverine CQRS Commands/Queries, Event Handlers và thống kê đầy đủ các API Endpoints, vui lòng xem [Báo cáo Tổng hợp Dự án Cinema Ticket Booking (Bản tiếng Việt)](documents/reports/summary-report-28-06-26.vi.md) hoặc [Bản tiếng Anh](documents/reports/summary-report-28-06-26.md).

---

## Tính Năng Chính

| Tính năng | Mô tả |
|---|---|
| **Cập nhật trạng thái ghế thời gian thực** | SignalR Hubs truyền phát tức thì các sự kiện khóa/mở khóa vé — nhiều người dùng cùng xem sơ đồ ghế ngồi trực tuyến đồng bộ theo thời gian thực |
| **Cơ chế khóa giữ ghế** | Cơ chế giữ ghế dựa trên Redis Distributed Locks, có cơ chế dự phòng sang Postgres Advisory Locks và cơ chế kiểm soát đồng thời lạc quan (Optimistic Concurrency) qua thuộc tính `xmin` |
| **Quy tắc chọn ghế trực tuyến** | Các quy tắc kiểm tra thông minh ngăn chọn ghế không hợp lệ (tránh để trống ghế đơn lẻ, tránh ghế cô lập giữa các ghế đã chọn, giới hạn số hàng ghế, kiểm tra chia cắt lối đi, v.v.). Quản trị viên có thể cấu hình linh hoạt mức độ: Chặn/Cảnh báo/Cho phép với mỗi quy tắc |
| **Tích hợp thanh toán trực tuyến** | Tích hợp cổng VNPay & ví điện tử MoMo với xác thực webhook IPN, cấu hình thời gian hết hạn và cho phép người dùng có thể thử lại phương thức thanh toán khác nếu gặp lỗi |
| **Chính sách giá vé linh hoạt** | Tự động tính giá vé dựa trên chính sách linh hoạt cho từng phòng chiếu, ngày trong tuần và suất chiếu — quản lý dễ dàng từ trang quản trị. |
| **Chương trình khách hàng thân thiết (Loyalty)** | Tích lũy điểm thưởng khi đặt vé thành công và thăng hạng thành viên (Silver, Gold, Platinum...) để hưởng chiết khấu ưu đãi và nhận mã giảm giá |
| **Mã giảm giá (Coupons)** | Cho phép khách hàng áp dụng mã giảm giá trực tiếp trong quá trình đặt vé |
| **Chương trình khuyến mãi (Promotions)** | Tự động quét và áp dụng chương trình khuyến mãi đạt điều kiện khi đặt vé. Hỗ trợ quản lý linh hoạt với phạm vi áp dụng, giới hạn giá trị và loại giảm giá |
| **Lập lịch chiếu** | Quản lý giờ chiếu tự động phát hiện xung đột phòng chiếu, tự động tính toán cộng dồn thời lượng phim, thời gian chiếu trailer và thời gian dọn dẹp phòng, tự động thay đổi trạng thái suất chiếu đúng giờ |
| **Xác thực & Phân quyền bảo mật** | Sử dụng ASP.NET Core Identity kết hợp JWT Access Token + HttpOnly Refresh Token Cookie bảo mật, phân quyền theo vai trò (RBAC), hỗ trợ đăng nhập từ mạng xã hội (Google & Facebook OAuth) |
| **Trang quản trị** | Giao diện quản trị phong phú giúp quản trị Rạp, Phòng chiếu, Phim, Lịch chiếu, Giá vé, Khuyến mãi, Mã giảm giá, Phân quyền vai trò chi tiết và bảng thống kê doanh thu trực quan |
| **Tự động phục hồi trạng thái hệ thống** | Các tác vụ nền chạy ngầm định kỳ quét và giải phóng các ghế bị khóa quá hạn (quá 10 phút), tự động hóa lịch chiếu rạp hàng ngày |

---

## Công Nghệ Sử Dụng

### Backend
| Công nghệ | Phiên bản | Mục đích |
|---|---|---|
| **.NET / ASP.NET Core** | 10.0+ | Web framework — MVC cho admin, Minimal APIs cho frontend |
| **Entity Framework Core** | 10.0+| ORM, migrations, seed dữ liệu mặc định |
| **Dapper** | 2.1+ | Truy vấn SQL thuần hiệu năng cao cho báo cáo thống kê |
| **Wolverine** | 5.8+ | Message Bus thực thi CQRS và điều phối các tác vụ ngầm |
| **SignalR** | Tích hợp sẵn .NET 10 | Giao tiếp WebSocket thời gian thực (hub khóa ghế & thanh toán) |
| **ASP.NET Core Identity** | 10.0+ | Quản lý người dùng, phân quyền vai trò bảo mật |
| **Scalar** | 2.13+ | Tài liệu API tương tác trực quan (chuẩn OpenAPI) |
| **Serilog** | 10.0+ | Ghi log có cấu trúc (ghi file, console và đẩy về Loki) |
| **xUnit** | 2.9+ | Viết kiểm thử đơn vị (Unit Test) và tích hợp (Integration Test) |

### Frontend
| Công nghệ | Phiên bản | Mục đích |
|---|---|---|
| **React** | 19.2+ | Xây dựng ứng dụng đơn trang (SPA) cho luồng khách hàng đặt vé |
| **TypeScript** | 6.0+ | Phát triển frontend an toàn kiểu dữ liệu (Type-safe) |
| **Vite** | 8.0+ | Công cụ build và dev server siêu tốc |
| **Tailwind CSS** | 3.4+ | Tiện ích CSS giúp thiết kế giao diện nhanh chóng |
| **Axios** | 1.15+ | Client HTTP giao tiếp API với backend |
| **@microsoft/signalr** | 10.0+ | Kết nối thời gian thực tới backend hubs |
| **React Router** | 19.2+ | Điều hướng trang phía Client |

### Cơ Sở Dữ Liệu & Bộ Nhớ Đệm
| Công nghệ | Phiên bản | Mục đích |
|---|---|---| 
| **PostgreSQL 17** | 17+ | Cơ sở dữ liệu quan hệ chính |
| **Redis** | 7+ | Bộ nhớ đệm phân tán & Quản lý khóa giữ ghế thời gian thực |

### Quy Trình DevOps & Giám Sát
| Công nghệ | Phiên bản | Mục đích |
|---|---|---|
| **Docker & Docker Compose** | 28.3+ | Container hóa môi trường phát triển và sản xuất |
| **.NET Aspire** | 13.2+ | Điều phối tài nguyên, khám phá dịch vụ, cấu hình OpenTelemetry |
| **GitHub Actions** | - | Tự động hóa CI/CD — kiểm thử → build → push Docker images → deploy |
| **Coolify** | 4.0.0 | PaaS tự lưu trữ giúp triển khai ứng dụng tự động trên AWS EC2 |
| **Prometheus** | 2.55+ | Thu thập số liệu đo lường hệ thống (app, Postgres, Redis) |
| **Grafana** | 11.6+ | Bảng điều khiển trực quan hóa metrics và cấu hình cảnh báo |
| **Loki** | 3.2+ | Thu thập và quản lý logs tập trung |
| **Tempo** | 2.6+ | Theo dõi vết Requests (qua OpenTelemetry) |

---

## Kiến Trúc

### Nguyên Tắc Thiết Kế

- **Clean Architecture** — Đảm bảo nguyên tắc đảo ngược phụ thuộc nghiêm ngặt: Domain → Application → Infrastructure → Presentation (WebServer)
- **Domain-Driven Design (DDD)** — Các thực thể nghiệp vụ chứa đựng logic cốt lõi được đóng gói quy tắc rõ ràng, kết hợp Domain Events và Value Objects
- **CQRS** — Phân định rõ ràng giữa các tác vụ Command (ghi) và Query (đọc) qua Wolverine Message Bus
- **Repository + Unit of Work** — Trừu tượng hóa việc truy cập cơ sở dữ liệu và đảm bảo tính nhất quán của giao dịch dữ liệu
- **Domain Events** — Điều phối các tác vụ phụ ẩn dưới nền (side effects) thông qua Message Pipeline của Wolverine (ví dụ: `BookingConfirmed → Gửi Email Xác Nhận`)

### Sơ Đồ Hệ Thống

![System Diagram](assets/system-diagram.png)

### Sơ Đồ Thực Thể Quan Hệ (ERD)

![Entity Relationship Diagram](assets/erd.png)

---

## Cấu Trúc Thư Mục Dự Án

```
📁 src/
├── Aspire.AppHost/                        # .NET Aspire điều phối tài nguyên, khám phá dịch vụ, OpenTelemetry
├── Aspire.ServiceDefaults/                # Các cấu hình mặc định dùng chung (logging, tracing config)
│
├── CinemaTicketBooking.Domain/            # Logic nghiệp vụ cốt lõi (không phụ thuộc thư viện bên ngoài)
│   ├── Abstractions/                      # Lớp thực thể cơ sở
│   ├── Constants/                         # Các hằng số MaxLength cho validation, sơ đồ ghế, v.v.
│   ├── Entities/                          # Thực thể: Cinema, Screen, Movie, ShowTime, Booking, Ticket...
│   ├── Enums/                             # Các kiểu liệt kê: BookingStatus, PaymentMethod, SeatType...
│   ├── Events/                            # Các sự kiện miền (Domain Events)
│   ├── Repositories/                      # Giao diện của các Repository
│   ├── Services/                          # Dịch vụ nghiệp vụ (Domain Services)
│   └── Utilities/                         # Công cụ tiện ích bổ trợ
│
├── CinemaTicketBooking.Application/       # Các ca sử dụng (Use Cases) & Điều phối luồng
│   ├── Abstractions/                      # Giao diện dịch vụ, Unit of Work
│   ├── Common/                            # Các lớp DTO dùng chung, phân trang, kết quả trả về
│   ├── Features/                          # Các Wolverine Commands & Queries tương ứng theo Aggregate
│   └── EventHandling/                     # Lắng nghe và xử lý sự kiện miền bất đồng bộ
│
├── CinemaTicketBooking.Infrastructure/    # Triển khai các dịch vụ tích hợp bên ngoài
│   ├── Auth/                              # Quản lý danh tính Identity, JWT, phân quyền tài khoản
│   ├── Cache/                             # Hiện thực hóa Redis Cache
│   ├── FileStorages/                      # Lưu trữ tệp tin MinIO/AWS S3
│   ├── Notifications/                     # Dịch vụ gửi Email qua Brevo
│   ├── Payments/                          # Tích hợp VNPay, MoMo và giả lập thanh toán
│   └── Persistence/                       # EF Core DbContext, các Migration và hiện thực Repositories
│
├── CinemaTicketBooking.WebServer/         # Dự án khởi chạy ứng dụng ASP.NET Core
│   ├── ApiEndpoints/                      # Định nghĩa các Minimal API Endpoints
│   ├── Controllers/                       # Các MVC Controller cho giao diện Admin
│   ├── CronJobs/                          # Các dịch vụ chạy ngầm định kỳ
│   ├── Hubs/                              # Các SignalR Hubs thời gian thực
│   ├── Middlewares/                       # Http Middlewares
│   ├── Models/                            # Cấu trúc ViewModel phục vụ MVC
│   └── Views/                             # Các Razor Views cho giao diện quản trị Admin
│
└── CinemaTicketBooking.WebApp/            # Giao diện Client React 19 (Vite + TypeScript)
    ├── src/                               # Components, pages, hooks, services giao tiếp API
    └── public/                            # Tài nguyên tĩnh (ảnh, favicon...)

📁 tests/
├── CinemaTicketBooking.UnitTests/         # Kiểm thử đơn vị (Unit Tests) cho Domain logic
└── CinemaTicketBooking.IntegrationTests/  # Kiểm thử tích hợp sử dụng Testcontainers (Postgres, Redis)

📁 dockers/
├── development/                           # Docker Compose phát triển local (app + DB + cache + monitoring)
├── production/                            # Docker Compose chạy production thực tế
└── monitoring/                            # Bảng điều khiển Grafana & Cấu hình Prometheus

📁 .github/workflows/
└── deploy.yml                             # CI/CD: Chạy test → build → push Docker images → deploy lên Coolify
```

---

## Hướng Dẫn Cài Đặt


### Cách 1: Sử dụng Docker Compose (Khuyên Dùng)
**Yêu cầu cài đặt sẵn Docker Desktop**

Khởi chạy toàn bộ hệ thống (backend, frontend, database, cache, monitoring) chỉ với một câu lệnh duy nhất:

```bash
# 1. Sao chép mã nguồn (Clone repository)
git clone https://github.com/annghdev/vicinema.git
cd vicinema

# 2. Sao chép và cấu hình biến môi trường
cp .env.example .env
# Chỉnh sửa file .env với các API keys và Secret keys cần thiết của bạn (tôi không thể chia sẻ chúng ở đây, bạn cần tự đăng ký chúng và chỉnh sửa để không gặp lỗi khi deploy)

# 3. Khởi chạy tất cả các dịch vụ (tự động biên dịch mã nguồn nội bộ)
docker compose up -d --build

# 4. Truy cập ứng dụng
#    - Giao diện Client (React): http://localhost:5173
#    - Trang quản trị (Admin):     http://localhost:8080
#    - Tài liệu API (Scalar):     http://localhost:8080/scalar/v1
#    - Hệ thống giám sát Grafana: http://localhost:3000
```

### Cách 2: Sử dụng .NET Aspire Orchestrator (hỗ trợ tốt hơn cho việc dev local)
**Điều kiện tiên quyết**

| Công cụ | Phiên bản | Mục đích |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | Bộ phát triển .NET |
| [Node.js](https://nodejs.org/) | 20+ | Môi trường chạy Javascript |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Mới nhất | Chạy các container bổ trợ local |
| [Aspire Workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup) | 13.2 | Điều phối resources |


```bash
# 1. Sao chép mã nguồn (Clone repository)
git clone https://github.com/annghdev/vicinema.git
cd vicinema

# 2. Đảm bảo cấu hình thông tin kết nối trong file appsettings.json

# 3. Đảm bảo ứng dụng Docker Desktop đang hoạt động

# 4. Đảm bảo cài đặt Aspire workload phiên bản mới nhất
dotnet tool install --g Aspire.Cli 
# hoặc cập nhật nếu đã cài đặt
aspire update --self

# 5. Khởi chạy .NET Aspire Orchestrator
aspire run src/Aspire.AppHost/Aspire.AppHost.csproj

# 6. Khởi chạy ứng dụng Frontend
cd src/CinemaTicketBooking.WebApp
npm install
npm run dev

# 7. Truy cập hệ thống
#    - Giao diện Client (React): http://localhost:5173
#    - Trang quản trị (Admin):     http://localhost:8080
#    - Tài liệu API (Scalar):     http://localhost:8080/scalar/v1
#    - Hệ thống giám sát Grafana: http://localhost:3000
```

### Các Tài Khoản Mặc Định

Hệ thống tự động khởi tạo dữ liệu (seed data) cho các tài khoản sau trong lần chạy đầu tiên:

| Vai trò | Tên tài khoản | Email | Mật khẩu |
|---|---|---|---|
| Quản trị hệ thống (SysAdmin) | `sysadmin` | `sysadmin@cinema.com` | `SysAdmin@123!` |
| Quản trị rạp (Admin) | `admin` | `admin@cinema.com` | `Admin@123!` |
| Quản lý rạp (Manager) | `manager` | `manager@cinema.com` | `Manager@123!` |
| Nhân viên bán vé (TicketStaff) | `ticketstaff` | `staff@cinema.com` | `Staff@123!` |
| Cộng tác viên (Coordinator) | `coordinator` | `coordinator@cinema.com` | `Coordinator@123!` |
| Khách hàng (Customer) | `customer` | `customer@cinema.com` | `Customer@123!` |

### Cấu Hình Biến Môi Trường

Các mục cấu hình chính trong file `appsettings.json`:

| Mục cấu hình | Mô tả |
|---|---|
| `ConnectionStrings:cinemadb` | Chuỗi kết nối PostgreSQL |
| `ConnectionStrings:redis` | Chuỗi kết nối Redis (không bắt buộc — hệ thống vẫn chạy bình thường nếu tắt Redis) |
| `Jwt` | Cấu hình nhà phát hành, người nhận, chữ ký số bảo mật, thời hạn của token JWT |
| `VnPay` | Thông tin kết nối và tích hợp cổng thanh toán VNPay |
| `Momo` | Thông tin kết nối và tích hợp thanh toán ví MoMo |
| `Cors:AllowedOrigins` | Danh sách nguồn gốc tên miền Client được phép truy cập (CORS) |
| `Authentication:Google/Facebook` | Thông tin OAuth mạng xã hội từ Google/Facebook |

---

## Live Demo

| | |
|---|---|
| Giao diện Client (React) |  **https://vici.annghdev.online** |
| Trang Quản Trị (MVC) | **http://vici-portal.annghdev.online** |
| Tài liệu API Backend | **http://vici-portal.annghdev.online/scalar/v1**  |

---
## Hình Ảnh Nổi Bật

### Luồng Đặt Vé Chính
![Booking with Momo Payment](assets/booking-main-flow.gif)

### Cập Nhật Trạng Thái Ghế Thời Gian Thực
![Seat Status Real-time Update](assets/seat-status-realtime.gif)

### Trang Quản Trị (ASP.NET Core MVC)

**Bảng Thống Kê Doanh Thu (Dashboard)**
![Admin Dashboard](assets/admin-dashboard.gif)

**Lên Lịch Chiếu Phim**
![Showtime Scheduling](assets/showtime-scheduling.gif)

**Quản Lý Mã Giảm Giá (Coupon)**
![Coupon Management](assets/coupon-management.gif)

**Cấu HÌnh Quy Tắc Chọn Ghế**
![Seat Selection Rules Configuration](assets/seat-selection-rules-config.gif)

**Ma Trận Phân Quyền Hạn Nhân Viên (Access Control)**
![Permissions Matrix](assets/admin/access-control.png)

### Giám Sát & Đo Lường Hệ Thống (Observability)
![Observability & Monitoring](assets/monitoring.gif)

### Bảng Điều Khiển Aspire (Công cụ hỗ trợ cho môi trường phát triển local)
![Aspire Dashboard](assets/aspire-dashboard.gif)

### Tài liệu API Tương Tác - Test API trực tiếp trên trình duyệt (Scalar API Docs)
![API Docs (Scalar)](assets/scalar-api-docs.gif)

---

## Liên Hệ

- **Tên:**  `Nguyễn Hữu An`
- **Email:**  `annghdev@gmail.com`
- **Điện thoại/Zalo:** `0867 662 945`

---

## Bản Quyền

Dự án này được cấp phép theo các điều khoản của [Giấy phép MIT](LICENSE.txt).

```
//                                               _oo0oo_
//                                              o8888888o
//                                              88" . "88
//                                              (| -_- |)
//                                              0\  =  /0
//                                            ___/`---'\___
//                                          .' \\|     |// '.
//                                         / \\|||  :  |||// \
//                                        / _||||| -:- |||||- \
//                                       |   | \\\  -  /// |   |
//                                       | \_|  ''\---/''  |_/ |
//                                       \  .-\__  '-'  ___/-. /
//                                     ___'. .'  /--.--\  `. .'___
//                                  ."" '<  `.___\_<|>_/___.' >' "".
//                                 | | :  `- \`.;`\ _ /`;.`/ - ` : | |
//                                 \  \ `_.   \_ __\ /__ _/   .-` /  /
//                                 \  \ `_.   \_ __\ /__ _/   .-` /  /
//                              ====`-.____`.___ \_____/___.-`___.-'=====
//                                               `=---='
//                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//                                ~ Phật Tổ phù hộ - Không bao giờ Bug ~
//                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
```
