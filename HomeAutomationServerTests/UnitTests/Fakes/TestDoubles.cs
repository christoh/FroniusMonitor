using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>A fixed server secret, so a test can rely on the same key across the whole test run.</summary>
internal sealed class TestAesKeyProvider : IAesKeyProvider
{
    public byte[] GetAesKey() => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
}

/// <summary>A clock a test can move, so an expiry can be reached without waiting for it.</summary>
internal sealed class SettableTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class TestUsers
{
    public const string Password = "correct horse battery staple";

    public static User Create(string userName, Roles roles, string password = Password)
    {
        var user = new User { Username = userName, Roles = roles };
        user.SetPassword(password);
        return user;
    }

    /// <summary>
    /// The real options plumbing rather than a stub, because that is how <c>Program.cs</c> hands the user list to
    /// the authentication code - as an <see cref="IOptionsMonitor{T}"/> over a configured <see cref="UserList"/>.
    /// </summary>
    public static IOptionsMonitor<UserList> AsOptions(params User[] users) => new ServiceCollection()
        .AddOptions()
        .Configure<UserList>(list => list.Users = [.. users])
        .BuildServiceProvider()
        .GetRequiredService<IOptionsMonitor<UserList>>();
}
