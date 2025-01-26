using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

public class Principal : IPrincipal
{
    public IIdentity Identity =>Auth;
    
    public Identity Auth { get; } = new Identity();
    
    public Roles Roles { get; set; }
    
    public bool IsInRole(string roleName)
    {
        return Enum.Parse(typeof(Roles), roleName) is Roles role&&(role&Roles)!=Roles.None;
    }
}