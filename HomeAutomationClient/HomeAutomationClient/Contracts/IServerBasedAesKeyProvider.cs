using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.HomeAutomationClient.Contracts;

public interface IServerBasedAesKeyProvider : IAesKeyProvider
{
    /// <summary>
    /// Fetches the key this server hands out for <paramref name="username"/>, or sets the all-zero key when it is
    /// <see langword="null"/> - which is what reads the user name out of a cache whose password is still locked.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> where the key is now set, and what went wrong otherwise. The server has to be asked
    /// for a key, so this is where an unreachable or wrong address first shows up; the caller is the one that
    /// knows how to put that in front of the user.
    /// </returns>
    public Task<ProblemDetails?> SetKeyFromUserName(string? username);
}