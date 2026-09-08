using Avalonia.Controls.Primitives;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// A <see cref="DataGridTextColumn"/> whose text sits where the cell says it should, so that the padding of the
/// cell is the whole horizontal inset and one number decides it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DataGridTextColumn"/> puts <c>Margin="12,0"</c> on the <see cref="TextBlock"/> it generates, and
/// that lands inside the padding of the cell rather than replacing it. Measured in the event log: the text of a
/// cell began 20 pixels in - 8 of them the cell's padding, 12 the column's margin - while the severity column,
/// whose content the column does not generate, began at 8. Two different insets in one grid, and the wider one
/// far too wide for a dense dialog.
/// </para>
/// <para>
/// A style cannot fix it. The margin is set on the instance, and in Avalonia a local value beats a style setter -
/// the same reason the check boxes of the settings dialog are scaled in a <c>Viewbox</c> instead of being told to
/// be shorter. So the element is taken as the column made it and the margin cleared.
/// </para>
/// </remarks>
public class CompactTextColumn : DataGridTextColumn
{
    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        var element = base.GenerateElement(cell, dataItem);
        element.Margin = new Thickness(0);
        return element;
    }
}
