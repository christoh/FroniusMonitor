namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

/// <summary>
///     What became of a command sent to one or more air conditioners: the message id it went out under, and the
///     targets that did not echo it within the timeout. An empty <see cref="Unconfirmed" /> means every target took it.
/// </summary>
/// <remarks>
///     An air conditioner that has taken a command answers with a <c>CMD_FCU_FROM_AC</c> carrying the same message
///     id as the command. Message queuing loses messages now and then, so a target that stays silent may have taken
///     the command all the same; the caller is told and decides what to show.
/// </remarks>
public class ToshibaHvacCommandResult
{
    public string MessageId { get; set; } = string.Empty;

    /// <summary>The targets that did not echo the command, by the id the caller named them with.</summary>
    public List<string> Unconfirmed { get; set; } = [];

    public bool IsSuccess => Unconfirmed.Count == 0;
}
