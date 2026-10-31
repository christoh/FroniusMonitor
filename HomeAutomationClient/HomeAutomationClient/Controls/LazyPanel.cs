namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// A panel that is built the first time it is shown, and is a plain decorator from then on. For the panels of a
/// device card, of which the card has a dozen or more and shows one: bind <see cref="Visual.IsVisible"/> to
/// whether this is the one, as the panels were bound before, and put the panel itself in the <c>Template</c>.
/// Built up front, the dashboard's two inverter cards alone brought five hundred visuals and a hundred and eighty
/// gauges nobody looked at. The first click on a mode builds its panel - a few milliseconds - and switching back
/// is as quick as it always was.
/// </summary>
public sealed class LazyPanel : TemplatedDecorator
{
    /// <summary>
    /// Layout applies the template of every control it measures, and it measures only what is visible: this is the
    /// moment a hidden panel is first shown, and the place <c>TemplatedControl</c> builds its own template too.
    /// </summary>
    public override void ApplyTemplate()
    {
        BuildChild();
        base.ApplyTemplate();
    }
}
