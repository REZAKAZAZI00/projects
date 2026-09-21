using Licensing.Infrastructure.Security;

namespace Licensing.Tests;

public class LicenseKeyServiceTests
{
    private readonly LicenseKeyService _service = new();

    [Fact]
    public void GenerateLicenseKey_ProducesUniqueUnpredictableKeys()
    {
        var keys = Enumerable.Range(0, 100).Select(_ => _service.GenerateLicenseKey()).ToHashSet();
        Assert.Equal(100, keys.Count);
        Assert.All(keys, key =>
        {
            Assert.Matches("^[A-Z2-9]{5}(-[A-Z2-9]{5}){4}$", key);
        });
    }

    [Fact]
    public void HashAndVerify_WorksWithNormalizedInput()
    {
        var key = _service.GenerateLicenseKey();
        var hash = _service.HashLicenseKey(key);
        Assert.True(_service.VerifyLicenseKey(key.ToLowerInvariant(), hash));
        Assert.False(_service.VerifyLicenseKey(_service.GenerateLicenseKey(), hash));
    }

    [Fact]
    public void GetKeyPrefix_DoesNotExposeFullKey()
    {
        var key = _service.GenerateLicenseKey();
        var prefix = _service.GetKeyPrefix(key);
        Assert.True(prefix.Length <= 8);
        Assert.NotEqual(key, prefix);
    }
}
