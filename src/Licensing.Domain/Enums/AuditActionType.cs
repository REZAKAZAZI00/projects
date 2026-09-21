namespace Licensing.Domain.Enums;

public enum AuditActionType
{
    LicenseCreated = 0,
    LicenseUpdated = 1,
    LicenseActivated = 2,
    LicenseDeactivated = 3,
    LicenseRenewed = 4,
    LicenseSuspended = 5,
    LicenseResumed = 6,
    LicenseRevoked = 7,
    LicenseValidated = 8,
    LicenseExpired = 9,
    SubscriptionCreated = 10,
    SubscriptionRenewed = 11,
    SubscriptionCancelled = 12,
    SubscriptionGracePeriodStarted = 13,
    UnauthorizedAccess = 14
}
