namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

/// <summary>
///     What <c>POST /api/IoTHub/SendCommand</c> answers: whether the service passed the command on, and the targets
///     it could not (<see cref="FailedTargetIds" />) or would not (<see cref="UnauthorizedTargetIds" />) deliver to.
///     Delivery is not execution - the air conditioner confirms that with an echo of the message id.
/// </summary>
public class ToshibaHvacCommandDelivery
{
    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("FailedTargetIds")]
    public List<string> FailedTargetIds { get; set; } = [];

    [JsonPropertyName("UnauthorizedTargetIds")]
    public List<string> UnauthorizedTargetIds { get; set; } = [];
}
