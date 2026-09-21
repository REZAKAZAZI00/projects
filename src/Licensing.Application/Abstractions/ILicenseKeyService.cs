namespace Licensing.Application.Abstractions;

public interface ILicenseKeyService
{
    string GenerateLicenseKey();
    string HashLicenseKey(string licenseKey);
    bool VerifyLicenseKey(string licenseKey, string hash);
    string GetKeyPrefix(string licenseKey);
}
