using System.Security.Cryptography;

namespace De.Hochstaetter.FritzBox.Http;

/// <summary>
/// Decides on a certificate the operating system does not trust, which the box's own self-signed one is when the box
/// is addressed by IP or as <c>fritz.box</c>.
/// </summary>
/// <remarks>
/// With a pinned fingerprint, only that certificate is accepted. Without one, any is, as it always was, and the log
/// says once which certificate that was, so it can be pinned. This was chosen on 2026-10-24 over refusing
/// unpinned certificates, because boxes are addressed by IP over https today and nothing may break with the update.
/// </remarks>
internal sealed class UntrustedCertificatePolicy(Uri baseUri, string? pinnedFingerprint, ILogger logger)
{
    private readonly string? pinned = Normalize(pinnedFingerprint);
    private int warned;

    public bool Accept(X509Certificate2 certificate)
    {
        var fingerprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);

        if (pinned != null)
        {
            if (fingerprint == pinned)
            {
                return true;
            }

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("The FRITZ!Box at {BaseUri} presents the certificate {Subject} with the SHA-256 fingerprint {Fingerprint}, not the pinned {Pinned}. It is refused.", baseUri, certificate.Subject, fingerprint, pinned);
            }

            return false;
        }

        if (Interlocked.Exchange(ref warned, 1) == 0 && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("The FRITZ!Box at {BaseUri} presents the certificate {Subject}, which the system does not trust. It is accepted because no fingerprint is pinned. To have it checked, set CertificateFingerprint of the connection to {Fingerprint}.", baseUri, certificate.Subject, fingerprint);
        }

        return true;
    }

    /// <summary>Upper case hex without separators, as <see cref="X509Certificate.GetCertHashString(HashAlgorithmName)"/> writes it; <see langword="null"/> for nothing.</summary>
    internal static string? Normalize(string? fingerprint) =>
        string.IsNullOrWhiteSpace(fingerprint) ? null : new string(fingerprint.Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray());
}
