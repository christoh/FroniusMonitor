using Microsoft.AspNetCore.Authorization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization
{
    public class BasicAuthorizeAttribute : AuthorizeAttribute
    {
        public BasicAuthorizeAttribute()
        {
            AuthenticationSchemes = "Basic";
        }
    }
}
