using PaunixGuard.Core.Security;
using Xunit;

namespace PaunixGuard.App.Tests.Core;

public sealed class PinHasherTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPin()
    {
        var hasher = new PinHasher();
        var hash = hasher.HashPin("1234");

        Assert.True(hasher.Verify("1234", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPin()
    {
        var hasher = new PinHasher();
        var hash = hasher.HashPin("1234");

        Assert.False(hasher.Verify("0000", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSamePin()
    {
        var hasher = new PinHasher();
        var hash1 = hasher.HashPin("1234");
        var hash2 = hasher.HashPin("1234");

        Assert.NotEqual(hash1, hash2);
        Assert.True(hasher.Verify("1234", hash1));
        Assert.True(hasher.Verify("1234", hash2));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForEmptyOrNull()
    {
        var hasher = new PinHasher();
        var hash = hasher.HashPin("test");

        Assert.False(hasher.Verify("", hash));
        Assert.False(hasher.Verify("", ""));
    }

    [Fact]
    public void Hash_ThrowsForEmptyPin()
    {
        var hasher = new PinHasher();

        Assert.Throws<ArgumentException>(() => hasher.HashPin(""));
        Assert.Throws<ArgumentException>(() => hasher.HashPin("  "));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash()
    {
        var hasher = new PinHasher();

        Assert.False(hasher.Verify("1234", "garbage"));
        Assert.False(hasher.Verify("1234", "pbkdf2-sha256$bad$more$parts"));
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenStoredIterationsAreUnsafe()
    {
        var hasher = new PinHasher();

        Assert.False(hasher.Verify("1234", "pbkdf2-sha256$0$AAAA$AAAA"));
        Assert.False(hasher.Verify("1234", "pbkdf2-sha256$1000001$AAAA$AAAA"));
    }
}
