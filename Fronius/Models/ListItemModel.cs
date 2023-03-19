namespace De.Hochstaetter.Fronius.Models;

public class ListItemModel<T> : IHaveDisplayName, IHaveToolTip
{
    public virtual string DisplayName { get; init; } = string.Empty;
    public virtual string ToolTip { get; init; } = string.Empty;
    public T? Value { get; init; }

    public override string ToString() => DisplayName;
}

public class EnumListItemModel<T> : ListItemModel<T> where T : Enum
{
    public override string DisplayName
    {
        get => Value?.ToDisplayName() ?? string.Empty;
        init { throw new NotSupportedException(); }
    }

    public override string ToolTip
    {
        get => Value?.ToToolTip() ?? string.Empty;
        init { throw new NotSupportedException(); }
    }
}
