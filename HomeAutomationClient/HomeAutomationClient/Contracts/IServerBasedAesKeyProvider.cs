using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.HomeAutomationClient.Contracts;

public interface IServerBasedAesKeyProvider:IAesKeyProvider
{
    public Task SetKeyFromUserName(string? username);
}