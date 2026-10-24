namespace De.Hochstaetter.FritzBox.Tr064;

/// <summary>
/// A TR-064 action the box answered with a SOAP fault carrying a UPnP error.
/// </summary>
public sealed class Tr064Exception(string action, int errorCode, string? errorDescription)
    : Exception($"TR-064 action {action} failed with UPnP error {errorCode} ({errorDescription})")
{
    /// <summary>The action needs a second factor: a button pressed on the box or a code dialed on a phone.</summary>
    public const int SecondFactorRequired = 866;

    /// <summary>Too many attempts at the second factor. The box allows the next one after up to 60 minutes.</summary>
    public const int SecondFactorBlocked = 867;

    /// <summary>Another second factor process is running. The box allows the next one after up to two minutes.</summary>
    public const int SecondFactorBusy = 868;

    public string Action { get; } = action;

    public int ErrorCode { get; } = errorCode;

    public string? ErrorDescription { get; } = errorDescription;
}
