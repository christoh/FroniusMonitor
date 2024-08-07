﻿using System.Text.Json;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaHvacAzureSmMobileCommand : BindableBase
{
    private string deviceUniqueId = string.Empty;

    [JsonPropertyName("sourceId")]
    [JsonRequired]
    public string DeviceUniqueId
    {
        get => deviceUniqueId;
        set => Set(ref deviceUniqueId, value);
    }

    private string messageId = string.Empty;

    [JsonPropertyName("messageId")]
    public string MessageId
    {
        get => messageId;
        set => Set(ref messageId, value);
    }

    private IList<string> targetIds = Array.Empty<string>();

    [JsonPropertyName("targetId")]
    public IList<string> TargetIds
    {
        get => targetIds;
        set => Set(ref targetIds, value);
    }

    private string commandName = string.Empty;

    [JsonPropertyName("cmd")]
    public string CommandName
    {
        get => commandName;
        set => Set(ref commandName, value);
    }

    private JsonElement payLoad;

    [JsonPropertyName("payload")]
    public JsonElement PayLoad
    {
        get => payLoad;
        set => Set(ref payLoad, value);
    }

    private string timeStamp = string.Empty;

    [JsonPropertyName("timeStamp")]
    public string TimeStamp
    {
        get => timeStamp;
        set => Set(ref timeStamp, value);
    }

    private string? timeZone;
    
    [JsonPropertyName("timeZone")]
    public string? TimeZone
    {
        get => timeZone;
        set => Set(ref timeZone, value);
    }
}