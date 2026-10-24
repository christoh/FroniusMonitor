using System.Security.Cryptography;

namespace De.Hochstaetter.FritzBox.Http;

/// <summary>
/// The challenge-response of <c>login_sid.lua?version=2</c>, as AVM's "Session ID" technical note describes it:
/// PBKDF2 for a challenge that starts with <c>2$</c> (FRITZ!OS 7.24 and later), MD5 otherwise.
/// </summary>
public static class FritzBoxLogin
{
    public static string CreateResponse(string challenge, string password) => challenge.StartsWith("2$", StringComparison.Ordinal)
        ? CreatePbkdf2Response(challenge, password)
        : CreateMd5Response(challenge, password);

    /// <summary><c>2$&lt;iter1&gt;$&lt;salt1&gt;$&lt;iter2&gt;$&lt;salt2&gt;</c>, answered with <c>&lt;salt2&gt;$&lt;hash2&gt;</c>.</summary>
    private static string CreatePbkdf2Response(string challenge, string password)
    {
        var parts = challenge.Split('$');

        if (parts.Length != 5)
        {
            throw new InvalidDataException($"The PBKDF2 challenge has {parts.Length} parts instead of 5");
        }

        var hash1 = Rfc2898DeriveBytes.Pbkdf2
        (
            Encoding.UTF8.GetBytes(password),
            Convert.FromHexString(parts[2]),
            int.Parse(parts[1], CultureInfo.InvariantCulture),
            HashAlgorithmName.SHA256,
            32
        );

        var hash2 = Rfc2898DeriveBytes.Pbkdf2(hash1, Convert.FromHexString(parts[4]), int.Parse(parts[3], CultureInfo.InvariantCulture), HashAlgorithmName.SHA256, 32);
        return $"{parts[4]}${Convert.ToHexStringLower(hash2)}";
    }

    /// <summary>
    /// MD5 of the UTF-16LE bytes of <c>&lt;challenge&gt;-&lt;password&gt;</c>, where every character above U+00FF counts
    /// as ".".
    /// </summary>
    private static string CreateMd5Response(string challenge, string password)
    {
        var text = new string($"{challenge}-{password}".Select(c => c > 'ÿ' ? '.' : c).ToArray());
        return $"{challenge}-{Convert.ToHexStringLower(MD5.HashData(Encoding.Unicode.GetBytes(text)))}";
    }
}
