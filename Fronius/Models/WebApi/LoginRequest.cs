using System.ComponentModel.DataAnnotations;

namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// What a client logs in with. It goes in the body of a POST, never in the query string, because a query string
/// is written to the access log of every server and proxy on the way.
/// </summary>
public class LoginRequest
{
    [Required(AllowEmptyStrings = false)]
    public string UserName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = string.Empty;
}
