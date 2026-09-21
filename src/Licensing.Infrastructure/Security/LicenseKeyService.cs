using System.Security.Cryptography;
using System.Text;
using Licensing.Application.Abstractions;

namespace Licensing.Infrastructure.Security;

public sealed class LicenseKeyService : ILicenseKeyService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int SegmentLength = 5;
    private const int SegmentCount = 5;

    public string GenerateLicenseKey()
    {
        var segments = new string[SegmentCount];
        for (var i = 0; i < SegmentCount; i++)
            segments[i] = GenerateSegment(SegmentLength);

        return string.Join('-', segments);
    }

    public string HashLicenseKey(string licenseKey)
    {
        var normalized = NormalizeKey(licenseKey);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyLicenseKey(string licenseKey, string hash)
    {
        var computed = HashLicenseKey(licenseKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash));
    }

    public string GetKeyPrefix(string licenseKey)
    {
        var normalized = NormalizeKey(licenseKey);
        var firstSegment = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? normalized;
        return firstSegment.Length >= 8 ? firstSegment[..8] : firstSegment.PadRight(8, 'X');
    }

    private static string NormalizeKey(string licenseKey) =>
        licenseKey.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static string GenerateSegment(int length)
    {
        Span<char> chars = stackalloc char[length];
        Span<byte> random = stackalloc byte[length];
        RandomNumberGenerator.Fill(random);

        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[random[i] % Alphabet.Length];

        return new string(chars);
    }
}
