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
        if (providedApiKey is null)
        {
            return false;
        }

        return MatchesSha256HexHash(ComputeSha256Hex(providedApiKey), configuredHash);
    }

    public static bool MatchesSha256HexHash(string? candidateHash, string? configuredHash)
    {
        if (!IsValidSha256HexHash(candidateHash) || !IsValidSha256HexHash(configuredHash))
        {
            return false;
        }

        var candidateHashBytes = Encoding.ASCII.GetBytes(candidateHash!.Trim());
        var configuredHashBytes = Encoding.ASCII.GetBytes(configuredHash!.Trim());

        return CryptographicOperations.FixedTimeEquals(candidateHashBytes, configuredHashBytes);
    }

    public static bool MatchesAnySha256HexHash(string? candidateHash, IEnumerable<string> configuredHashes)
    {
        if (!IsValidSha256HexHash(candidateHash))
        {
            return false;
        }

        var isMatch = false;
        foreach (var configuredHash in configuredHashes)
        {
            isMatch |= MatchesSha256HexHash(candidateHash, configuredHash);
        }

        return isMatch;
    }

    private static bool IsLowercaseHexCharacter(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f';
    }
}
