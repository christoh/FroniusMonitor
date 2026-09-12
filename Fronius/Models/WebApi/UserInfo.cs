using System.ComponentModel.DataAnnotations;

namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>A user of the home automation server as the server describes them: the name and the roles.</summary>
public class UserInfo
{
    [Required(AllowEmptyStrings = false)]
    public string UserName { get; set; } = string.Empty;

    public Roles Roles { get; set; }
}