using Avalonia.Platform;
using ScottPlot;
using SkiaSharp;
using FontWeight = ScottPlot.FontWeight;
using FontSlant = ScottPlot.FontSlant;
using FontSpacing = ScottPlot.FontSpacing;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// Gives ScottPlot the Inter font the rest of the app is drawn with, out of the same embedded files Avalonia
/// uses (<c>Avalonia.Fonts.Inter</c>).
/// </summary>
/// <remarks>
/// <para>
/// ScottPlot resolves a font name through Skia's system font manager. In the browser there is no system font
/// manager worth the name, so the answer depended on what the browser's Skia happened to fall back to - the chart
/// came up in a monospace face on one start of the container and in a proportional one on the next. Avalonia has
/// the same problem and solves it by embedding Inter and registering it with its own font manager, which ScottPlot
/// knows nothing about; this resolver hands ScottPlot the very same files.
/// </para>
/// <para>
/// <see cref="Register"/> puts it first in <see cref="Fonts.FontResolvers"/> and makes <see cref="FamilyName"/>
/// the default, so every label of every plot is Inter unless a plot asks for something else. Two faces are
/// enough for a chart: regular and bold.
/// </para>
/// </remarks>
public sealed class InterFontResolver : IFontResolver
{
    public const string FamilyName = "Inter";

    private static readonly Lock registration = new();
    private static bool isRegistered;

    private readonly Lazy<SKTypeface?> regular = new(() => Load("Inter-Regular.ttf"));
    private readonly Lazy<SKTypeface?> bold = new(() => Load("Inter-Bold.ttf"));

    /// <summary>Registers the resolver once for the process. Safe to call from every chart that is created.</summary>
    public static void Register()
    {
        lock (registration)
        {
            if (isRegistered)
            {
                return;
            }

            Fonts.FontResolvers.Insert(0, new InterFontResolver());
            Fonts.Default = FamilyName;
            isRegistered = true;
        }
    }

    public SKTypeface? CreateTypeface(string fontName, FontWeight weight, FontSlant slant, FontSpacing spacing)
    {
        if (!string.Equals(fontName, FamilyName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return weight >= FontWeight.SemiBold ? bold.Value ?? regular.Value : regular.Value;
    }

    public SKTypeface? CreateTypeface(string fontName, bool isBold, bool isItalic) => CreateTypeface(fontName, isBold ? FontWeight.Bold : FontWeight.Normal, FontSlant.Upright, FontSpacing.Normal);

    /// <summary>
    /// The font file as Avalonia embeds it. Null where the asset cannot be read, in which case ScottPlot goes on
    /// to its other resolvers rather than the chart failing.
    /// </summary>
    private static SKTypeface? Load(string fileName)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri($"avares://Avalonia.Fonts.Inter/Assets/{fileName}"));
            return SKTypeface.FromStream(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
