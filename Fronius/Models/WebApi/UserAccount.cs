namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// What an administrator sends to create or change a user. The password is optional when changing one: an empty
/// or missing password keeps the one the user has.
/// </summary>
public class UserAccount : UserInfo
{
    public string? Password { get; set; }
}