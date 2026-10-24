namespace De.Hochstaetter.FritzBox.Usp;

/// <summary>The box answered a USP request with an Error Message.</summary>
public sealed class UspException(Error error) : Exception(CreateMessage(error))
{
    public uint ErrorCode { get; } = error.ErrCode;

    public IReadOnlyList<Error.Types.ParamError> ParameterErrors { get; } = error.ParamErrs.ToList();

    private static string CreateMessage(Error error)
    {
        var text = $"USP error {error.ErrCode}: {error.ErrMsg}";

        return error.ParamErrs.Count == 0
            ? text
            : text + " (" + string.Join("; ", error.ParamErrs.Select(e => $"{e.ParamPath}: {e.ErrCode} {e.ErrMsg}")) + ")";
    }
}
