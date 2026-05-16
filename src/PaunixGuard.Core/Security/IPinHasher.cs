namespace PaunixGuard.Core.Security;

public interface IPinHasher
{
    string HashPin(string pin);

    bool Verify(string pin, string storedHash);
}

