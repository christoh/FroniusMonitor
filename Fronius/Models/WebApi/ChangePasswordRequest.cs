using System.ComponentModel.DataAnnotations;

namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// What a user sends to change their own password. Unlike <see cref="UserAccount"/>, giving the current password
/// is mandatory: a hijacked session alone must not be enough to lock the real user out of their own account.
/// </summary>
public class ChangePasswordRequest
{
    [Required(AllowEmptyStrings = false)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string NewPassword { get; set; } = string.Empty;
}
