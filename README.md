# Licensing Server

سامانه مرکزی مدیریت License و Subscription بر پایه **ASP.NET Core 8** و **MariaDB/MySQL**.

## ساختار Solution

| پروژه | نقش |
|--------|-----|
| `Licensing.Domain` | Entityها و Enumها |
| `Licensing.Application` | Business Logic، DTO، Validation |
| `Licensing.Infrastructure` | EF Core، امنیت کلید، Audit |
| `Licensing.Api` | REST API + Swagger |
| `Licensing.Tests` | Unit Test منطق اصلی |

## پیش‌نیاز

- .NET 8 SDK
- MariaDB یا MySQL

## پیکربندی

Connection string و JWT را در `src/Licensing.Api/appsettings.json` تنظیم کنید.

```json
"ConnectionStrings": {
  "LicensingDatabase": "Server=localhost;Port=3306;Database=LicensingDb;User=root;Password=YOUR_PASSWORD;"
}
```

## اجرا

```powershell
cd src/Licensing.Api
dotnet run
```

Swagger: `https://localhost:7xxx/swagger`

کاربر پیش‌فرض Seed (قابل تغییر در appsettings):

- Username: `admin`
- Password: `ChangeMe!123`

## Migration

```powershell
dotnet ef database update --project src/Licensing.Infrastructure --startup-project src/Licensing.Api
```

## APIهای اصلی

### مدیریتی (JWT Admin)

| Method | Route |
|--------|--------|
| POST | `/api/v1/auth/login` |
| POST | `/api/v1/licenses` |
| GET | `/api/v1/licenses` |
| GET | `/api/v1/licenses/{id}` |
| PUT | `/api/v1/licenses/{id}` |
| POST | `/api/v1/licenses/{id}/renew` |
| POST | `/api/v1/licenses/{id}/suspend` |
| POST | `/api/v1/licenses/{id}/resume` |
| POST | `/api/v1/licenses/{id}/revoke` |
| GET | `/api/v1/licenses/{id}/activations` |
| GET | `/api/v1/licenses/{id}/history` |
| GET | `/api/v1/licenses/customers/{customerId}` |
| GET | `/api/v1/catalog/products` |
| GET | `/api/v1/catalog/plans` |
| GET | `/api/v1/catalog/products/{productId}/features` |
| GET | `/api/v1/audit-logs` |

### Client (بدون JWT، Rate Limited)

| Method | Route |
|--------|--------|
| POST | `/api/v1/client/licenses/activate` |
| POST | `/api/v1/client/licenses/deactivate` |
| POST | `/api/v1/client/licenses/validate` |

## تست

```powershell
dotnet test
```

## امنیت

- License Key با `RandomNumberGenerator` تولید می‌شود؛ در DB فقط Hash و Prefix ذخیره می‌شود.
- تصمیم نهایی اعتبارسنجی در Server انجام می‌شود.
- APIهای Client دارای Rate Limit هستند.
- تاریخ‌ها در Backend به UTC ذخیره می‌شوند.

## گزارش پیشرفت

فایل `docs/PROGRESS-REPORT-01.md` برای بازه سه‌روزه اول.
