using De.Hochstaetter.FritzBox.Usp;
using Usp;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The Records and Messages the FritzBox library writes for USP, and the MQTT 3.1.1 "reply to" topic.
/// </summary>
public sealed class FritzBoxUspCodecTests
{
    [Theory]
    [InlineData(nameof(Request.Get), Header.Types.MsgType.Get)]
    [InlineData(nameof(Request.GetSupportedDm), Header.Types.MsgType.GetSupportedDm)]
    [InlineData(nameof(Request.GetSupportedProtocol), Header.Types.MsgType.GetSupportedProto)]
    [InlineData(nameof(Request.Set), Header.Types.MsgType.Set)]
    [InlineData(nameof(Request.Add), Header.Types.MsgType.Add)]
    [InlineData(nameof(Request.Delete), Header.Types.MsgType.Delete)]
    public void A_request_gets_the_message_type_of_its_body_and_a_fresh_id(string body, Header.Types.MsgType expected)
    {
        var request = body switch
        {
            nameof(Request.Get) => new Request { Get = new Get() },
            nameof(Request.GetSupportedDm) => new Request { GetSupportedDm = new GetSupportedDM() },
            nameof(Request.GetSupportedProtocol) => new Request { GetSupportedProtocol = new GetSupportedProtocol() },
            nameof(Request.Set) => new Request { Set = new Set() },
            nameof(Request.Add) => new Request { Add = new Add() },
            _ => new Request { Delete = new Delete() },
        };

        var first = UspCodec.CreateRequest(request);
        var second = UspCodec.CreateRequest(request);

        Assert.Equal(expected, first.Header.MsgType);
        Assert.NotEqual(first.Header.MsgId, second.Header.MsgId);
    }

    [Fact]
    public void A_wrapped_message_comes_out_of_its_record_unchanged()
    {
        var message = UspCodec.CreateRequest(new Request { Get = new Get { ParamPaths = { "Device.DeviceInfo." } } });

        var record = UspCodec.ParseRecord(UspCodec.Wrap(message, "1.3", "os::box", "proto::test"));

        Assert.Equal("1.3", record.Version);
        Assert.Equal("os::box", record.ToId);
        Assert.Equal("proto::test", record.FromId);
        Assert.Equal(message, UspCodec.GetMessage(record));
    }

    [Fact]
    public void A_notify_response_carries_the_message_id_and_the_subscription_of_the_notify()
    {
        var notify = new Msg
        {
            Header = new Header { MsgId = "n-1", MsgType = Header.Types.MsgType.Notify },
            Body = new Body { Request = new Request { Notify = new Notify { SubscriptionId = "switches", SendResp = true } } },
        };

        var answer = UspCodec.CreateNotifyResponse(notify);

        Assert.Equal(Header.Types.MsgType.NotifyResp, answer.Header.MsgType);
        Assert.Equal("n-1", answer.Header.MsgId);
        Assert.Equal("switches", answer.Body.Response.NotifyResp.SubscriptionId);
    }

    [Fact]
    public void The_MQTT_3_reply_to_topic_is_written_and_read_as_TR_369_shows_it()
    {
        const string topic = "usp/agents/cid:3AA3F8:my-unique-usp-id-42/reply-to=usp%2Fcontrollers%2Foui:00256D:my-unique-bbf-id-42";

        Assert.Equal(topic, UspCodec.AppendReplyTo("usp/agents/cid:3AA3F8:my-unique-usp-id-42", "usp/controllers/oui:00256D:my-unique-bbf-id-42"));
        Assert.Equal("usp/controllers/oui:00256D:my-unique-bbf-id-42", UspCodec.GetReplyTo(topic));
        Assert.Null(UspCodec.GetReplyTo("usp/agents/box"));
    }
}
