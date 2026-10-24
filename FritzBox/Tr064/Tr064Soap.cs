namespace De.Hochstaetter.FritzBox.Tr064;

/// <summary>
/// Builds TR-064 SOAP requests and reads the answers. Kept apart from the HTTP transport so that both can be tested
/// without a box.
/// </summary>
public static class Tr064Soap
{
    private static readonly XNamespace soapEnvelope = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace avm = "avm.de";

    /// <summary>
    /// The SOAP envelope for <paramref name="action"/> of <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="secondFactorToken">
    /// The token of a second factor process the action belongs to, sent in the SOAP header, or <see langword="null"/>.
    /// </param>
    public static string CreateRequest(string serviceType, string action, IEnumerable<KeyValuePair<string, string>> arguments, string? secondFactorToken = null)
    {
        XNamespace service = serviceType;

        var header = secondFactorToken == null
            ? null
            : new XElement
            (
                soapEnvelope + "Header",
                new XElement
                (
                    avm + "token",
                    new XAttribute(XNamespace.Xmlns + "avm", avm.NamespaceName),
                    new XAttribute(soapEnvelope + "mustUnderstand", "1"),
                    secondFactorToken
                )
            );

        var envelope = new XElement
        (
            soapEnvelope + "Envelope",
            new XAttribute(XNamespace.Xmlns + "s", soapEnvelope.NamespaceName),
            new XAttribute(soapEnvelope + "encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/"),
            header,
            new XElement
            (
                soapEnvelope + "Body",
                new XElement
                (
                    service + action,
                    new XAttribute(XNamespace.Xmlns + "u", serviceType),
                    arguments.Select(argument => new XElement(argument.Key, argument.Value))
                )
            )
        );

        return """<?xml version="1.0" encoding="utf-8"?>""" + envelope.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>
    /// The output arguments of the answer to <paramref name="action"/>, by name.
    /// </summary>
    /// <exception cref="Tr064Exception">The answer is a SOAP fault.</exception>
    /// <exception cref="InvalidDataException">The answer is neither a fault nor the response to <paramref name="action"/>.</exception>
    public static IReadOnlyDictionary<string, string> ParseResponse(string xml, string action)
    {
        var body = XDocument.Parse(xml).Root?.Element(soapEnvelope + "Body") ?? throw new InvalidDataException($"The answer to {action} has no SOAP body");

        if (body.Element(soapEnvelope + "Fault") is { } fault)
        {
            var upnpError = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "UPnPError");
            var errorCode = upnpError?.Elements().FirstOrDefault(e => e.Name.LocalName == "errorCode")?.Value;
            var errorDescription = upnpError?.Elements().FirstOrDefault(e => e.Name.LocalName == "errorDescription")?.Value;

            throw new Tr064Exception
            (
                action,
                int.TryParse(errorCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) ? code : -1,
                errorDescription ?? fault.Elements().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value
            );
        }

        var response = body.Elements().FirstOrDefault(e => e.Name.LocalName == action + "Response") ??
                       throw new InvalidDataException($"The answer to {action} has no {action}Response");

        return response.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);
    }

    /// <summary>TR-064 writes booleans as 0 and 1.</summary>
    public static bool ParseBoolean(string value) => value is "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);

    /// <summary>TR-064 writes booleans as 0 and 1.</summary>
    public static string FormatBoolean(bool value) => value ? "1" : "0";
}
