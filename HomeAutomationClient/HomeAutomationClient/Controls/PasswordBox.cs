using De.Hochstaetter.HomeAutomationClient.Assets.Images;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// A text box for a secret: dots instead of the characters, and a button at its right end that shows them.
/// </summary>
/// <remarks>
/// <para>
/// It <em>is</em> a <see cref="TextBox"/> and keeps that style key, so everything that styles a text box styles
/// this one with nothing said twice: the compact sizes of a dialog, the validation frame and its tooltip, the focus
/// colour, a <c>Grid.Form &gt; TextBox</c> width. The only thing that differs is what is in the box, and that is a
/// property of the box itself.
/// </para>
/// <para>
/// The reveal is <see cref="TextBox.RevealPassword"/>, and the button only toggles it. Whether the characters are
/// on screen is visual state of the control and nobody else's business, so it does not go through a view model -
/// the login dialog used to carry a flag and a command for it, and spelt the box out inline.
/// </para>
/// </remarks>
public class PasswordBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    public PasswordBox()
    {
        PasswordChar = '•';

        var icon = new VisibilityIcon();
        icon.Bind(VisibilityIcon.VisibleProperty, this.GetObservable(RevealPasswordProperty));

        // Not focusable: a Tab out of the box must land on the next field, not on the eye beside it.
        var reveal = new Button { Classes = { "TransparentButton", "RevealPassword" }, Content = icon, Focusable = false };
        reveal.Click += (_, _) => RevealPassword = !RevealPassword;
        InnerRightContent = reveal;
    }
}
