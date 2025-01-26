using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;

namespace De.Hochstaetter.Fronius.Models.Settings;

public class UserList:AuthenticationSchemeOptions
{
    public IEnumerable<User> Users { get; set; } = [];
}