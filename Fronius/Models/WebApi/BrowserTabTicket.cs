namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// The one-time ticket that turns a browser tab the client opens into a session of the user who is logged in. A
/// tab cannot be opened with an <c>Authorization</c> header, so the client asks <c>POST api/Identity/tabTicket</c>
/// for a ticket and puts it into the address of the tab; the server swaps it for a cookie and redirects to the same
/// address without it.
/// </summary>
public static class BrowserTabTicket
{
    /// <summary>The query parameter the ticket travels in.</summary>
    public const string QueryParameter = "tabTicket";
}
