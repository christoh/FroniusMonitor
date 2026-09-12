using System.Text.Json;
using System.Text.Json.Serialization;
using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The JSON shape of what the server publishes about an air conditioner and what a client sends back as a command.
/// </summary>
/// <remarks>
/// The state bytes are the one thing that must not be left to the default object serialization: a byte that is
/// 0xff ("not set") reads back as a temperature of -1, and -1 is a legitimate temperature the setter encodes as 0x7e
/// - so a round trip through the properties would silently turn "leave it alone" into "set it to minus one".
/// As the hex string of the wire format the bytes survive untouched.
/// </remarks>
public sealed class ToshibaHvacJsonTests
{
    /// <summary>The options the server's controllers and hub serialize with, see <c>SignalRRegistration</c>.</summary>
    private static readonly JsonSerializerOptions serverOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyProperties = true,
        IgnoreReadOnlyFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public void The_state_is_the_hex_string_of_the_wire_format()
    {
        var state = new ToshibaHvacStateData { IsTurnedOn = true, Mode = ToshibaHvacOperatingMode.Heating, TargetTemperatureCelsius = 22 };

        var json = JsonSerializer.Serialize(state, serverOptions);

        Assert.Equal($"\"{state}\"", json);
        Assert.Equal(19 * 2 + 2, json.Length);
        Assert.StartsWith("\"304316ffff", json);
    }

    [Fact]
    public void Unset_bytes_survive_a_round_trip()
    {
        var command = new ToshibaHvacStateData { TargetTemperatureCelsius = 22 };

        var back = JsonSerializer.Deserialize<ToshibaHvacStateData>(JsonSerializer.Serialize(command, serverOptions), serverOptions)!;

        Assert.Equal(command.StateData, back.StateData);
        Assert.Equal(0xff, back.StateData[0]);
        Assert.Equal<sbyte?>(22, back.TargetTemperatureCelsius);
    }

    [Fact]
    public void A_device_carries_its_state_as_that_string_and_reads_back()
    {
        var device = new ToshibaHvacMappingDevice
        {
            Name = "Living room",
            DeviceUniqueId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            AcModelId = 3,
            MeritFeature = 0x6002,
            State = new ToshibaHvacStateData { IsTurnedOn = true, FanSpeed = ToshibaHvacFanSpeed.Auto },
        };

        var json = JsonSerializer.Serialize(device, serverOptions);
        var back = JsonSerializer.Deserialize<ToshibaHvacMappingDevice>(json, serverOptions)!;

        Assert.Contains($"\"ACStateData\":\"{device.State}\"", json);
        Assert.Equal(device.DeviceUniqueId, back.DeviceUniqueId);
        Assert.Equal("Living room", back.Name);
        Assert.Equal(device.State.StateData, back.State.StateData);
        Assert.True(back.State.IsTurnedOn);
        Assert.Equal(ToshibaHvacFanSpeed.Auto, back.State.FanSpeed);
    }

    [Fact]
    public void The_azure_device_id_is_six_digits_and_a_bad_one_is_replaced_by_a_random_one()
    {
        Assert.Equal("004711", ToshibaHvacAzureDeviceId.ToString(4711));
        Assert.Equal(4711u, ToshibaHvacAzureDeviceId.Parse("004711", null));

        var replaced = ToshibaHvacAzureDeviceId.Parse("not a number", null);

        Assert.InRange(replaced, 0u, 999999u);
        Assert.Equal(6, ToshibaHvacAzureDeviceId.ToString(replaced).Length);
    }
    [Fact]
    public void A_realtime_frame_carries_the_command_the_hub_used_to_deliver()
    {
        // Captured from the Web PubSub socket: the group is the air conditioner, the data has no target list, and
        // the state came with 22 bytes where the IoT Hub used to deliver 19.
        const string frame = """{"type":"message","from":"group","fromUserId":null,"group":"993b1afd-3cab-410e-8e01-582b96ebf329","dataType":"json","data":{"sourceId":"993b1afd-3cab-410e-8e01-582b96ebf329","messageId":"0000000650","cmd":"CMD_FCU_FROM_AC","timeStamp":"006aa59516","payload":{"data":"3142184131640010177ffe0bffff100200ffffffffff"}}}""";
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var envelope = JsonSerializer.Deserialize<ToshibaHvacRealtimeEnvelope>(frame, web)!;
        var command = envelope.Data!.Value.Deserialize<ToshibaHvacAzureSmMobileCommand>(web)!;
        var state = command.PayLoad.GetProperty("data").Deserialize<ToshibaHvacStateData>(web)!;

        Assert.Equal("message", envelope.Type);
        Assert.Equal("993b1afd-3cab-410e-8e01-582b96ebf329", envelope.Group);
        Assert.Equal("CMD_FCU_FROM_AC", command.CommandName);
        Assert.Equal(envelope.Group, command.DeviceUniqueId);
        Assert.Equal("0000000650", command.MessageId);
        Assert.Empty(command.TargetIds);
        Assert.Equal(22, state.StateData.Count);
        Assert.Equal(0x31, state.StateData[0]);
    }

    [Fact]
    public void The_realtime_token_and_the_delivery_result_read_back_from_the_service_shape()
    {
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var token = JsonSerializer.Deserialize<ToshibaHvacResponse<ToshibaHvacRealtimeToken>>("""{"ResObj":{"Url":"wss://x.webpubsub.azure.com/client/hubs/h?access_token=t","ExpiresAt":"2026-09-12T16:23:29.6717883+00:00","Groups":["a","b"]},"IsSuccess":true,"Message":"Success","StatusCode":"Success"}""", web)!;
        var delivery = JsonSerializer.Deserialize<ToshibaHvacResponse<ToshibaHvacCommandDelivery>>("""{"ResObj":{"Status":"Success","FailedTargetIds":[],"UnauthorizedTargetIds":["c"]},"IsSuccess":true,"Message":"Success","StatusCode":"Success"}""", web)!;

        Assert.True(token.IsSuccess);
        Assert.StartsWith("wss://", token.Data.Url);
        Assert.Equal(new DateTimeOffset(2026, 9, 12, 16, 23, 29, TimeSpan.Zero).AddTicks(6717883), token.Data.ExpiresAt);
        Assert.Equal(["a", "b"], token.Data.Groups);
        Assert.Equal("Success", delivery.Data.Status);
        Assert.Empty(delivery.Data.FailedTargetIds);
        Assert.Equal(["c"], delivery.Data.UnauthorizedTargetIds);
    }
}
