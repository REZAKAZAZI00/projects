# گزارش پیشرفت — بازه ۱ (روز ۱ تا ۳)

**تاریخ:** ۱۴۰۴/۰۶/۳۰  
**پروژه:** سامانه مدیریت License و Subscription

## خلاصه فعالیت‌های انجام‌شده

1. راه‌اندازی Solution چندلایه (`Domain`, `Application`, `Infrastructure`, `Api`, `Tests`).
2. طراحی و پیاده‌سازی Entityهای دیتابیس مطابق Task (Customers, Products, Plans, Features, Subscriptions, Licenses, Activations, History, Renewals, Audit).
3. پیاده‌سازی Business Logic اصلی:
   - صدور License و تولید کلید امن
   - Activation / Deactivation با سقف تعداد
   - Validation با بررسی Product و Instance
   - Suspend / Resume / Revoke / Renew
   - انقضای خودکار (Background Service + بررسی Lazy در API)
4. REST API مدیریتی و Client + Swagger + JWT Admin.
5. Audit Log برای عملیات حساس.
6. Migration اولیه EF Core و Seed داده نمونه.
7. Unit Test برای License Key و سناریوهای Activation/Validation.

## وضعیت نسبت به برنامه ۲۰ روزه

| فاز | برنامه | وضعیت |
|-----|--------|--------|
| طراحی DB | روز ۱–۳ | **تکمیل اولیه** |
| Core Business | روز ۴–۱۰ | **~۴۰٪** (چرخه اصلی License آماده) |
| API + امنیت | روز ۸–۱۴ | **~۵۰٪** |
| تست + مستندات | روز ۱۵–۲۰ | **~۲۵٪** |

**تخمین کلی پیشرفت:** حدود **۳۵–۴۰٪** از Backend طبق معیار تحویل Task.

## بخش‌های تکمیل‌شده

- مدل داده و روابط اصلی
- چرخه عمر License (Pending → Active → Suspended/Expired/Revoked)
- Activation محدود + Deactivation
- Validation API
- Audit و Status History
- Renewal License (+ ثبت Renewal برای Subscription متصل)
- جستجو/فیلتر لیست License
- مستندات Swagger و README

## بخش‌های باقی‌مانده

- پنل مدیریتی Frontend (UI) — در Scope فعلی فقط API
- APIهای کامل Subscription (ایجاد/لغو/Grace Period/Billing hook)
- CRUD کامل Product/Plan/Feature از Admin
- Integration Test End-to-End با MySQL واقعی
- سخت‌گیری بیشتر Rate Limit / IP blocking / API Key برای Client
- گزارش‌های تحلیلی (License نزیک انقضا در UI)
- Localization پیام خطا

## چالش‌ها و پیشنهاد رفع

| چالش | پیشنهاد |
|------|---------|
| وابستگی به MySQL در Migration/Run | Docker Compose برای MariaDB در Dev |
| Performance Validation در Scale بالا | Index روی `LicenseKeyPrefix` + Batch expire job |
| جستجو با License Key کامل | Prefix برای UI؛ Verify با Hash در Activate/Validate |
| اتصال Billing آینده | نگه‌داشت `Subscription.MetadataJson` و رویداد Renewal |

## موارد کشف‌شده (خارج از Task اولیه)

- ذخیره **Hash** کلید به‌جای Plain Text در DB (الزام امنیتی؛ کلید فقط یک‌بار در پاسخ Create برگردانده می‌شود).
- نیاز به **Admin User** و JWT برای APIهای مدیریتی.
- Background job هر ۵ دقیقه برای Expire (علاوه بر بررسی در Validation).

## گام‌های پیشنهادی بازه ۲ (روز ۴–۶)

1. تکمیل Subscription Service و APIهای Renewal/Cancel.
2. Admin CRUD برای Product/Plan/Feature.
3. Integration Tests + Docker Compose.
4. سخت‌سازی Production (Secrets, HTTPS-only, Log redaction).
