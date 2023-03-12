using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public class ToshibaAcResponse<T> : BindableBase where T : new()
{
    private bool isSuccess;

    [JsonPropertyName("IsSuccess")]
    [JsonRequired]
    public bool IsSuccess
    {
        get => isSuccess;
        set => Set(ref isSuccess, value);
    }

    private T data = new();

    [JsonPropertyName("ResObj")]
    [JsonRequired]
    public T Data
    {
        get => data;
        set => Set(ref data, value);
    }

    private string message = string.Empty;
    [JsonPropertyName("Message")]
    [JsonRequired]
    public string Message
    {
        get => message;
        set => Set(ref message, value);
    }
}