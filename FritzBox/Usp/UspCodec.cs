namespace De.Hochstaetter.FritzBox.Usp;

/// <summary>
/// Wraps USP Messages into Records and back. Only Records without a session context are used: the box and the
/// controller exchange whole Messages, without E2E session, segmentation or payload security.
/// </summary>
public static class UspCodec
{
    /// <summary>The MQTT 5.0 Content Type a USP Record is sent with (R-MQTT.27).</summary>
    public const string ContentType = "usp.msg";

    /// <summary>The registered MIME type, which a receiver has to accept as well (R-MQTT.27a).</summary>
    public const string MimeType = "application/vnd.bbf.usp.msg";

    private const string ReplyToMarker = "/reply-to=";

    public static Msg CreateRequest(Request request) => new()
    {
        Header = new Header { MsgId = Guid.NewGuid().ToString("N"), MsgType = GetMessageType(request) },
        Body = new Body { Request = request },
    };

    /// <summary>The answer to a Notify that asked for one, with the Notify's own message id.</summary>
    public static Msg CreateNotifyResponse(Msg notify) => new()
    {
        Header = new Header { MsgId = notify.Header.MsgId, MsgType = Header.Types.MsgType.NotifyResp },
        Body = new Body { Response = new Response { NotifyResp = new NotifyResp { SubscriptionId = notify.Body.Request.Notify.SubscriptionId } } },
    };

    public static byte[] Wrap(Msg message, string version, string toId, string fromId) => new Record
    {
        Version = version,
        ToId = toId,
        FromId = fromId,
        NoSessionContext = new NoSessionContextRecord { Payload = message.ToByteString() },
    }.ToByteArray();

    public static Record ParseRecord(byte[] data) => Record.Parser.ParseFrom(data);

    /// <summary>The Message a Record carries, or <see langword="null"/> for a Record that carries none (connect, disconnect).</summary>
    public static Msg? GetMessage(Record record) => record.RecordTypeCase == Record.RecordTypeOneofCase.NoSessionContext
        ? Msg.Parser.ParseFrom(record.NoSessionContext.Payload)
        : null;

    /// <summary>
    /// The topic to publish to for an MQTT 3.1.1 recipient: its topic, followed by our "reply to" topic with every
    /// "/" written as "%2F" (R-MQTT.24).
    /// </summary>
    public static string AppendReplyTo(string topic, string replyTo) => topic + ReplyToMarker + replyTo.Replace("/", "%2F", StringComparison.Ordinal);

    /// <summary>The "reply to" topic an MQTT 3.1.1 sender put at the end of the topic name (R-MQTT.25), or <see langword="null"/>.</summary>
    public static string? GetReplyTo(string topic)
    {
        var index = topic.LastIndexOf(ReplyToMarker, StringComparison.Ordinal);
        return index < 0 ? null : topic[(index + ReplyToMarker.Length)..].Replace("%2F", "/", StringComparison.Ordinal);
    }

    private static Header.Types.MsgType GetMessageType(Request request) => request.ReqTypeCase switch
    {
        Request.ReqTypeOneofCase.Get => Header.Types.MsgType.Get,
        Request.ReqTypeOneofCase.GetSupportedDm => Header.Types.MsgType.GetSupportedDm,
        Request.ReqTypeOneofCase.GetInstances => Header.Types.MsgType.GetInstances,
        Request.ReqTypeOneofCase.Set => Header.Types.MsgType.Set,
        Request.ReqTypeOneofCase.Add => Header.Types.MsgType.Add,
        Request.ReqTypeOneofCase.Delete => Header.Types.MsgType.Delete,
        Request.ReqTypeOneofCase.Operate => Header.Types.MsgType.Operate,
        Request.ReqTypeOneofCase.Notify => Header.Types.MsgType.Notify,
        Request.ReqTypeOneofCase.GetSupportedProtocol => Header.Types.MsgType.GetSupportedProto,
        Request.ReqTypeOneofCase.Register => Header.Types.MsgType.Register,
        Request.ReqTypeOneofCase.Deregister => Header.Types.MsgType.Deregister,
        _ => throw new ArgumentException($"The request carries no {nameof(Request.ReqTypeCase)}", nameof(request)),
    };
}
