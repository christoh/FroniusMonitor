using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// A decorator whose child is written as a template and built later than the XAML around it. Where that child is
/// a hundred visuals the user may never look at, building it along with the page costs the page its start: the
/// dashboard came up with 2900 visuals - 250 gauges among them, ten of which could be seen - in one layout pass
/// that held the UI thread for seconds. The subclasses decide when the child is built, and nothing else about them
/// differs: <see cref="LazyPanel"/> builds when it is first laid out, which is when it is first shown, and
/// <see cref="DeferredContent"/> a frame or two after it is attached. See Dashboard.Loading.
/// </summary>
/// <remarks>
/// The content is a <c>Template</c> in XAML, which the XAML compiler keeps as a factory instead of building it
/// with the rest of the file. A template starts a scope of its own, so the compiled bindings inside it say their
/// <c>x:DataType</c> again. The child inherits the data context the way any child does, so the bindings find what
/// they would have found in place.
/// </remarks>
public abstract class TemplatedDecorator : Decorator
{
    /// <summary>What the child is built from. Set from XAML, once.</summary>
    [Content]
    public ITemplate<Control>? Template { get; set; }

    /// <summary>Whether the child has been built.</summary>
    public bool IsBuilt => Child != null;

    /// <summary>
    /// Builds the child from the template, once. Nothing happens without a template, and nothing a second time.
    /// </summary>
    protected void BuildChild()
    {
        if (Child == null && Template is { } template)
        {
            Child = template.Build();
        }
    }
}
