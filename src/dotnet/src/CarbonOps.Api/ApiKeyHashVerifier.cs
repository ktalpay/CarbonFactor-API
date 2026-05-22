using System.Security.Cryptography;
using System.Text;

namespace CarbonOps.Api;

internal static class ApiKeyHashVerifier
{
    private const int Sha256HexLength = 64;

    public static string ComputeSha256Hex(string apiKey)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool IsValidSha256HexHash(string? configuredHash)
    {
        if (string.IsNullOrWhiteSpace(configuredHash))
        {
            return false;
        }

        var normalizedHash = configuredHash.Trim();
        return normalizedHash.Length == Sha256HexLength
            && normalizedHash.All(IsLowercaseHexCharacter);
    }

    public static bool VerifySha256Hex(string? providedApiKey, string? configuredHash)
    {
        if (providedApiKey is null || !IsValidSha256HexHash(configuredHash))
        {
            return false;
        }

        var providedHashBytes = Encoding.ASCII.GetBytes(ComputeSha256Hex(providedApiKey));
        var configuredHashBytes = Encoding.ASCII.GetBytes(configuredHash!.Trim());

        return CryptographicOperations.FixedTimeEquals(providedHashBytes, configuredHashBytes);
    }

    private static bool IsLowercaseHexCharacter(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f';
    }
}
