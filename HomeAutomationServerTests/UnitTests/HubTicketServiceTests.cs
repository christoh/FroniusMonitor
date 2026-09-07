using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What a hub ticket has to be worth: good for exactly one named user, only while it is fresh, and useless the
/// moment anything about it is changed - including by whoever holds it.
/// </summary>
public class HubTicketServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_freshly_issued_ticket_names_the_user_it_was_issued_for()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var (service, _) = Create(bob);

        Assert.Same(bob, service.Validate(service.Issue(bob)));
    }

    [Fact]
    public void A_ticket_carries_neither_the_password_nor_its_hash()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var (service, _) = Create(bob);

        var ticket = service.Issue(bob);

        Assert.DoesNotContain(TestUsers.Password, ticket, StringComparison.Ordinal);
        Assert.DoesNotContain(bob.PasswordHash, ticket, StringComparison.Ordinal);
        Assert.DoesNotContain(bob.Salt, ticket, StringComparison.Ordinal);
    }

    [Fact]
    public void A_ticket_is_refused_once_it_has_expired()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var (service, clock) = Create(bob);

        var ticket = service.Issue(bob);

        clock.Now = Now + HubTicketService.Lifetime;
        Assert.Same(bob, service.Validate(ticket));

        clock.Now = Now + HubTicketService.Lifetime + TimeSpan.FromSeconds(1);
        Assert.Null(service.Validate(ticket));
    }

    [Fact]
    public void Changing_the_password_invalidates_the_tickets_of_that_user()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var (service, _) = Create(bob);

        var ticket = service.Issue(bob);
        Assert.Same(bob, service.Validate(ticket));

        bob.SetPassword("something else entirely");
        Assert.Null(service.Validate(ticket));
    }

    [Fact]
    public void A_ticket_of_a_user_who_is_gone_is_refused()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var alice = TestUsers.Create("alice", Roles.User);

        // Issued while bob existed, checked against a list that only has alice.
        var issuer = new HubTicketService(new TestAesKeyProvider(), TestUsers.AsOptions(bob, alice), new SettableTimeProvider(Now), NullLogger<HubTicketService>.Instance);
        var ticket = issuer.Issue(bob);

        var afterRemoval = new HubTicketService(new TestAesKeyProvider(), TestUsers.AsOptions(alice), new SettableTimeProvider(Now), NullLogger<HubTicketService>.Instance);
        Assert.Null(afterRemoval.Validate(ticket));
    }

    [Fact]
    public void A_ticket_signed_with_another_servers_secret_is_refused()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var (ours, _) = Create(bob);

        var theirs = new HubTicketService(new OtherAesKeyProvider(), TestUsers.AsOptions(bob), new SettableTimeProvider(Now), NullLogger<HubTicketService>.Instance);

        Assert.Null(ours.Validate(theirs.Issue(bob)));
    }

    [Fact]
    public void The_user_of_a_ticket_cannot_be_swapped_for_somebody_else()
    {
        var bob = TestUsers.Create("bob", Roles.Guest);
        var root = TestUsers.Create("root", Roles.Administrator);
        var (service, _) = Create(bob, root);

        var parts = service.Issue(bob).Split('.');
        var forged = string.Join('.', parts[0], Base64Url.EncodeToString("root"u8), parts[2], parts[3]);

        Assert.Null(service.Validate(forged));
    }

    [Fact]
    public void The_expiry_of_a_ticket_cannot_be_pushed_out()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var (service, clock) = Create(bob);

        var parts = service.Issue(bob).Split('.');
        var forged = string.Join('.', parts[0], parts[1], clock.Now.AddYears(1).ToUnixTimeSeconds(), parts[3]);

        Assert.Null(service.Validate(forged));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("nonsense")]
    [InlineData("v1.only.three")]
    [InlineData("v1.a.b.c.d")]
    [InlineData("v2.Ym9i.99999999999.AAAA")]
    [InlineData("v1.Ym9i.not-a-number.AAAA")]
    [InlineData("v1.!!!.99999999999.AAAA")]
    [InlineData("v1.Ym9i.99999999999.!!!")]
    public void A_ticket_of_the_wrong_shape_is_refused(string? ticket)
    {
        var (service, _) = Create(TestUsers.Create("bob", Roles.User));

        Assert.Null(service.Validate(ticket));
    }

    [Fact]
    public void Every_bit_of_the_signature_matters()
    {
        var bob = TestUsers.Create("bob", Roles.User);
        var (service, _) = Create(bob);

        var ticket = service.Issue(bob);
        var signature = ticket[(ticket.LastIndexOf('.') + 1)..];
        var payload = ticket[..ticket.LastIndexOf('.')];
        var bytes = Base64Url.DecodeFromChars(signature);

        for (var index = 0; index < bytes.Length; index++)
        {
            var flipped = bytes.ToArray();
            flipped[index] ^= 0x01;

            Assert.Null(service.Validate($"{payload}.{Base64Url.EncodeToString(flipped)}"));
        }
    }

    private static (HubTicketService Service, SettableTimeProvider Clock) Create(params User[] users)
    {
        var clock = new SettableTimeProvider(Now);
        return (new HubTicketService(new TestAesKeyProvider(), TestUsers.AsOptions(users), clock, NullLogger<HubTicketService>.Instance), clock);
    }

    private sealed class OtherAesKeyProvider : IAesKeyProvider
    {
        public byte[] GetAesKey() => [16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];
    }
}
