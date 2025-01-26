using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization
{
    public class BasicAuthenticationHandler(IOptionsMonitor<UserList> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var x = options.CurrentValue.Users;
            string authHeader;

            if (Request.Headers.TryGetValue("Authorization", out var value))
            {
                authHeader = value.ToString();
            }
            else
            {
                if (!Request.Cookies.TryGetValue("auth", out authHeader!))
                {
                    return Task.FromResult(AuthenticateResult.Fail("You need to properly authenticate"));
                }
            }
            

            if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Incorrect Auth header"));
            };

            var split = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader[6..])).Split(':', 2);

            if (split.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Incorrect Auth header"));
            }
            
            var (username, password) = (split[0], split[1]);

            var user = x.FirstOrDefault(u => u.Username==username);

            if (user == null||!user.Authenticate(password))
            {
                return Task.FromResult(AuthenticateResult.Fail("Access denied"));
            }
           

            var identity = new Identity { IsAuthenticated = true, Name = username };

            var claims = new List<Claim> { /*new Claim(ClaimTypes.Name,username)*/ };
            
            claims.AddRange
            (
                from role in Enum.GetValues<Roles>().Except([Roles.All, Roles.None])
                where (user.Roles & role) != Roles.None
                select new Claim(ClaimTypes.Role, role.ToString())
            );

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(identity,claims));

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, identity.AuthenticationType)));
        }
    }
}
