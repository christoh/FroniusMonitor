namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>What a successful login tells the client about the user who logged in.</summary>
public class UserInfo
{
    public string UserName { get; set; } = string.Empty;

    public Roles Roles { get; set; }
}