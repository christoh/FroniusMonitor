using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace De.Hochstaetter.HomeAutomationServer.Models.Authorization;

[Flags]
public enum Roles
{
    None = 0,
    Guest = 1<<0,
    User = 1<<1,
    PowerUser = 1<<2,
    Operator = 1<<3,
    Administrator = 1<<4,
    All=Guest|User|PowerUser|Operator|Administrator,
}

public class User
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    [XmlIgnore] [JsonIgnore] public byte[] SaltBytes => Convert.FromBase64String(Salt);
    
    public Roles Roles { get; set; } = Roles.None;

    public bool Authenticate(string password)
    {
        var hashBytes = SaltBytes.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
        var hash=Convert.ToBase64String(SHA3_512.HashData(hashBytes));
        return hash == PasswordHash;
    }
}