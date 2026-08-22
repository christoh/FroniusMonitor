using System.Numerics;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace De.Hochstaetter.Fronius.Models;

// Most likely INotifyPropertyChanged is not needed. Verification required.
public struct HaColor(uint value) : IEquatable<HaColor>, IEquatable<uint>, IParsable<HaColor>, INotifyPropertyChanged
{
    public HaColor() : this((uint)HaColors.Black) { }

    private static HaColors[]? allColors;

    private static HaColors[] AllColors => allColors ??= Enum.GetValues<HaColors>();
    
    private uint uintColor = value;

    public static implicit operator uint(HaColor color) => color.uintColor;

    public static implicit operator HaColor(uint value) => new(value);

    public static implicit operator HaColor(HaColors haColor) => new((uint)haColor);

    [Obsolete("Change your code to use Hx.Mustang.Platform.Models.Colors.Color")]
    public static implicit operator HaColor(System.Drawing.Color color) => new(unchecked((uint)color.ToArgb()));

    [Obsolete("Change your code to use Hx.Mustang.Platform.Models.Colors.Color")]
    public static implicit operator System.Drawing.Color(HaColor color) => System.Drawing.Color.FromArgb(unchecked((int)color.uintColor));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HaColor FromRgb(byte r, byte g, byte b) => FromArgb(255, r, g, b);

    public static HaColor FromArgb(byte a, byte r, byte g, byte b) => new(unchecked((uint)((a << 24) | (r << 16) | (g << 8) | b)));

    public (byte A, byte R, byte G, byte B) ToArgb() => (A, R, G, B);

    public byte A
    {
        get => unchecked((byte)(uintColor >>> 24));
        set => SetField(ref uintColor, (uintColor & 0x00ffffff) | ((uint)value << 24));
    }

    public byte R
    {
        get => unchecked((byte)((uintColor & 0xff0000) >>> 16));
        set => SetField(ref uintColor, (uintColor & 0xff00ffff) | ((uint)value << 16));
    }

    public byte G
    {
        get => unchecked((byte)((uintColor & 0xff00) >>> 8));
        set => SetField(ref uintColor, (uintColor & 0xffff00ff) | ((uint)value << 8));
    }

    public byte B
    {
        get => unchecked((byte)(uintColor & 0xff));
        set => SetField(ref uintColor, (uintColor & 0xffffff00) | value);
    }

    /// <summary>
    ///     Returns a new <see cref="HaColor" /> that is mixed with another <see cref="HaColor" />
    /// </summary>
    /// <param name="otherColor">The other <see cref="HaColor" /> that you want to mix with the current color</param>
    /// <param name="amount">An <see cref="IFloatingPoint{TSelf}"/> that contains a weight (between 0 and 1) the other <see cref="HaColor" /> should get in the new <see cref="HaColor" />.
    /// The default is .5 (mix both colors with equal weight)</param>
    /// <typeparam name="T">The concrete floating point type (<see langword="double"/>, <see langword="float"/>, ...)</typeparam>
    /// <returns>A new color that is mixed with <paramref name="otherColor" /></returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount" /> is not between 0 and 1</exception>
    public HaColor MixWith<T>(HaColor otherColor, T amount) where T : IFloatingPoint<T>
    {
        if (!(amount >= T.Zero && amount <= T.One))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), @$"{nameof(amount)} must be between 0 and 1");
        }

        return FromArgb
        (
            MixColorComponent(A, otherColor.A, amount),
            MixColorComponent(R, otherColor.R, amount),
            MixColorComponent(G, otherColor.G, amount),
            MixColorComponent(B, otherColor.B, amount)
        );
    }


    /// <summary>
    ///     Returns a new <see cref="HaColor" /> that is mixed with another <see cref="HaColor" />
    /// </summary>
    /// <param name="otherColor">The other <see cref="HaColor" /> that you want to mix with the current color</param>
    /// <returns>A new color that is mixed with <paramref name="otherColor" /></returns>
    public HaColor MixWith(HaColor otherColor) => MixWith(otherColor, .5f);

    public bool Equals(HaColor other) => uintColor == other.uintColor;

    public bool Equals(uint other) => uintColor == other;

    public override bool Equals(object? obj) => obj switch
    {
        HaColor other => Equals(other),
        uint other => Equals(other),
        int other => Equals(unchecked((uint)other)),
        long other => Equals((uint)other),
        ulong other => Equals((uint)other),
        UInt128 other => Equals((uint)other),
        Int128 other => Equals((uint)other),
        _ => false,
    };

    public override int GetHashCode() => uintColor.GetHashCode();

    public static bool operator==(HaColor left, HaColor right) => left.Equals(right);

    public static bool operator!=(HaColor left, HaColor right) => !(left == right);

    public static bool operator==(HaColor left, uint right) => left.Equals(right);

    public static bool operator!=(HaColor left, uint right) => !(left == right);

    public static bool operator==(uint left, HaColor right) => right.Equals(left);

    public static bool operator!=(uint left, HaColor right) => !(left == right);

    // ReSharper disable once CommentTypo
    /// <summary>
    /// Returns a known color name or '#AARRGGBB'. The returned <see langword="string"/> is guaranteed to work with <see cref="Parse"/>
    /// </summary>
    public override string ToString()
    {
        var localColor = this;

        if (localColor.uintColor == (uint)HaColors.Transparent)
        {
            return nameof(HaColors.Transparent);
        }

        var knownColor = AllColors.FirstOrDefault(c => (uint)c == localColor.uintColor);
        return (uint)knownColor != (uint)HaColors.Transparent ? knownColor.ToString() : $"#{uintColor:X8}";
    }

    /// <summary>
    /// <para>Parses a color string (hex or case-insensitive known color name)</para>
    /// <para>Examples:</para>
    /// <para>
    /// #ff00ff00 (green with no transparency)<br/>
    /// ff0000 (red no transparency)<br/>
    /// 800000ff (blue 50% transparency)<br/>
    /// #f0f (purple no transparency)<br/>
    /// DodgerBlue (a case-insensitive known color name from the <see cref="HaColors"/> enum)<br/>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>The hashtag character (#) forces hex parsing. You only need it if a known color name consists of only hex letters (A to F) to indicate that you want to parse as hex.</para>
    /// <para>Instead of using <see cref="Parse"/>, you can also say '<see cref="HaColor"/> myColor = 0xffff0000U' but always use 8 digits (6 digits always gives a 100% transparent color). This is much faster.<br/>
    /// <see cref="Parse"/> is mainly here for deserialization from JSON or XML.</para>
    /// </remarks>
    /// <param name="colorString"></param>
    /// <param name="unused">The <see cref="IFormatProvider"/> is unused. It only exists to implement <see cref="IParsable{T}"/></param>
    /// <returns>A <see cref="HaColor"/> according to the string.</returns>
    /// <exception cref="FormatException">The color string is not valid.</exception>
    public static HaColor Parse(string colorString, IFormatProvider? unused = null)
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            throw new FormatException("Color string cannot be empty.");
        }

        const string errorText = $"{nameof(colorString)} must be a known color name (Red, DodgerBlue, DarkGreen, etc.) or 3, 6 or 8 hex digits. A leading '#' is allowed";

        return TryParseCore(colorString, out var result) ? result : throw new FormatException(errorText);
    }

    /// <summary>
    /// <para>Parses a color string</para>
    /// <para>Examples:</para>
    /// <para>
    /// #ff00ff00 (green with no transparency)<br/>
    /// #ff0000 (red no transparency)<br/>
    /// #800000ff (blue 50% transparency)<br/>
    /// #f0f (purple no transparency)
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>The hashtag character (#) at the beginning is optional.</para>
    /// <para>Instead of using <see cref="Parse"/>, you can also say '<see cref="HaColor"/> myColor = 0xffff0000U' but always use 8 digits (6 digits always gives a 100% transparent color). This is much faster.<br/>
    /// <see cref="Parse"/> is mainly here for deserialization from JSON or XML.</para>
    /// </remarks>
    /// <param name="s">The string to parse</param>
    /// <param name="providerUnused">The <see cref="IFormatProvider"/> is unused. It only exists to implement <see cref="IParsable{TSelf}"/></param>
    /// <param name="result">The parsed color</param>
    /// <returns><see langword="true"/> if the string was parsed successfully. If <see langword="false"/> is returned, <see langword="out"/> <paramref name="result"/> is undefined.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? providerUnused, out HaColor result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = 0U;
            return false;
        }

        return TryParseCore(s, out result);
    }

    /// <summary>
    /// The actual parsing logic shared by <see cref="Parse"/> and <see cref="TryParse"/>. Never throws, so
    /// <see cref="TryParse"/> does not have to pay for exception handling on invalid input.
    /// </summary>
    /// <param name="colorString">A non-empty color string.</param>
    /// <param name="result">The parsed color, or <see cref="HaColors.Transparent"/> if parsing failed.</param>
    /// <returns><see langword="true"/> if <paramref name="colorString"/> was parsed successfully.</returns>
    private static bool TryParseCore(string colorString, out HaColor result)
    {
        result = 0U;

        var hasHash = colorString[0] == '#';
        var parseString = hasHash ? colorString[1..] : colorString;

        if (parseString.Length == 0)
        {
            return false;
        }

        // Enum.TryParse accepts comma-separated lists even for non-flags enums and ORs them together, so
        // 'Red,Blue' would silently become Magenta. Reject lists outright, and validate the result against the
        // defined members so that bare numbers cannot slip through either.
        if
        (
            !hasHash && char.IsLetter(parseString[0]) && !parseString.Contains(',') &&
            Enum.TryParse<HaColors>(parseString, true, out var colorEnum) && Enum.IsDefined(colorEnum)
        )
        {
            result = colorEnum;
            return true;
        }

        var hexString = parseString.Length switch
        {
            3 => CreateFullColor(parseString),
            6 => "ff" + parseString,
            8 => parseString,
            _ => null,
        };

        if (hexString == null || !uint.TryParse(hexString, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var uintColor))
        {
            return false;
        }

        result = uintColor;
        return true;
    }

    private static string CreateFullColor(string parseString)
    {
        var builder = new StringBuilder(8);
        builder.Append("ff");

        foreach (var character in parseString)
        {
            builder.Append(character);
            builder.Append(character);
        }

        return builder.ToString();
    }

    private static byte MixColorComponent<T>(byte first, byte second, T amount) where T : IFloatingPoint<T>
    {
        return T.ConvertToInteger<byte>
        (
            T.Round
            (
                T.CreateChecked(first) * (T.One - amount) + T.CreateChecked(second) * amount, MidpointRounding.AwayFromZero
            )
        );
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1069
[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum HaColors : uint
{
    Transparent = 0, // 0x00000000
    Black = 4278190080, // 0xFF000000
    Navy = 4278190208, // 0xFF000080
    DarkBlue = 4278190219, // 0xFF00008B
    MediumBlue = 4278190285, // 0xFF0000CD
    Blue = 4278190335, // 0xFF0000FF
    DarkGreen = 4278215680, // 0xFF006400
    Green = 4278222848, // 0xFF008000
    Teal = 4278222976, // 0xFF008080
    DarkCyan = 4278225803, // 0xFF008B8B
    DeepSkyBlue = 4278239231, // 0xFF00BFFF
    DarkTurquoise = 4278243025, // 0xFF00CED1
    MediumSpringGreen = 4278254234, // 0xFF00FA9A
    Lime = 4278255360, // 0xFF00FF00
    SpringGreen = 4278255487, // 0xFF00FF7F
    Aqua = 4278255615, // 0xFF00FFFF
    Cyan = 4278255615, // 0xFF00FFFF
    MidnightBlue = 4279834992, // 0xFF191970
    DodgerBlue = 4280193279, // 0xFF1E90FF
    LightSeaGreen = 4280332970, // 0xFF20B2AA
    ForestGreen = 4280453922, // 0xFF228B22
    SeaGreen = 4281240407, // 0xFF2E8B57
    DarkSlateGray = 4281290575, // 0xFF2F4F4F
    LimeGreen = 4281519410, // 0xFF32CD32
    MediumSeaGreen = 4282168177, // 0xFF3CB371
    Turquoise = 4282441936, // 0xFF40E0D0
    RoyalBlue = 4282477025, // 0xFF4169E1
    SteelBlue = 4282811060, // 0xFF4682B4
    DarkSlateBlue = 4282924427, // 0xFF483D8B
    MediumTurquoise = 4282962380, // 0xFF48D1CC
    Indigo = 4283105410, // 0xFF4B0082
    DarkOliveGreen = 4283788079, // 0xFF556B2F
    CadetBlue = 4284456608, // 0xFF5F9EA0
    CornflowerBlue = 4284782061, // 0xFF6495ED
    MediumAquamarine = 4284927402, // 0xFF66CDAA
    DimGray = 4285098345, // 0xFF696969
    SlateBlue = 4285160141, // 0xFF6A5ACD
    OliveDrab = 4285238819, // 0xFF6B8E23
    SlateGray = 4285563024, // 0xFF708090
    LightSlateGray = 4286023833, // 0xFF778899
    MediumSlateBlue = 4286277870, // 0xFF7B68EE
    LawnGreen = 4286381056, // 0xFF7CFC00
    Chartreuse = 4286578432, // 0xFF7FFF00
    Aquamarine = 4286578644, // 0xFF7FFFD4
    Maroon = 4286578688, // 0xFF800000
    Purple = 4286578816, // 0xFF800080
    Olive = 4286611456, // 0xFF808000
    Gray = 4286611584, // 0xFF808080
    SkyBlue = 4287090411, // 0xFF87CEEB
    LightSkyBlue = 4287090426, // 0xFF87CEFA
    BlueViolet = 4287245282, // 0xFF8A2BE2
    DarkRed = 4287299584, // 0xFF8B0000
    DarkMagenta = 4287299723, // 0xFF8B008B
    SaddleBrown = 4287317267, // 0xFF8B4513
    DarkSeaGreen = 4287609999, // 0xFF8FBC8F
    LightGreen = 4287688336, // 0xFF90EE90
    MediumPurple = 4287852763, // 0xFF9370DB
    DarkViolet = 4287889619, // 0xFF9400D3
    PaleGreen = 4288215960, // 0xFF98FB98
    DarkOrchid = 4288230092, // 0xFF9932CC
    YellowGreen = 4288335154, // 0xFF9ACD32
    Sienna = 4288696877, // 0xFFA0522D
    Brown = 4289014314, // 0xFFA52A2A
    DarkGray = 4289309097, // 0xFFA9A9A9
    LightBlue = 4289583334, // 0xFFADD8E6
    GreenYellow = 4289593135, // 0xFFADFF2F
    PaleTurquoise = 4289720046, // 0xFFAFEEEE
    LightSteelBlue = 4289774814, // 0xFFB0C4DE
    PowderBlue = 4289781990, // 0xFFB0E0E6
    Firebrick = 4289864226, // 0xFFB22222
    DarkGoldenrod = 4290283019, // 0xFFB8860B
    MediumOrchid = 4290401747, // 0xFFBA55D3
    RosyBrown = 4290547599, // 0xFFBC8F8F
    DarkKhaki = 4290623339, // 0xFFBDB76B
    Silver = 4290822336, // 0xFFC0C0C0
    MediumVioletRed = 4291237253, // 0xFFC71585
    IndianRed = 4291648604, // 0xFFCD5C5C
    Peru = 4291659071, // 0xFFCD853F
    Chocolate = 4291979550, // 0xFFD2691E
    Tan = 4291998860, // 0xFFD2B48C
    LightGray = 4292072403, // 0xFFD3D3D3
    Thistle = 4292394968, // 0xFFD8BFD8
    Orchid = 4292505814, // 0xFFDA70D6
    Goldenrod = 4292519200, // 0xFFDAA520
    PaleVioletRed = 4292571283, // 0xFFDB7093
    Crimson = 4292613180, // 0xFFDC143C
    Gainsboro = 4292664540, // 0xFFDCDCDC
    Plum = 4292714717, // 0xFFDDA0DD
    BurlyWood = 4292786311, // 0xFFDEB887
    LightCyan = 4292935679, // 0xFFE0FFFF
    Lavender = 4293322490, // 0xFFE6E6FA
    DarkSalmon = 4293498490, // 0xFFE9967A
    Violet = 4293821166, // 0xFFEE82EE
    PaleGoldenrod = 4293847210, // 0xFFEEE8AA
    LightCoral = 4293951616, // 0xFFF08080
    Khaki = 4293977740, // 0xFFF0E68C
    AliceBlue = 4293982463, // 0xFFF0F8FF
    Honeydew = 4293984240, // 0xFFF0FFF0
    Azure = 4293984255, // 0xFFF0FFFF
    SandyBrown = 4294222944, // 0xFFF4A460
    Wheat = 4294303411, // 0xFFF5DEB3
    Beige = 4294309340, // 0xFFF5F5DC
    WhiteSmoke = 4294309365, // 0xFFF5F5F5
    MintCream = 4294311930, // 0xFFF5FFFA
    GhostWhite = 4294506751, // 0xFFF8F8FF
    Salmon = 4294606962, // 0xFFFA8072
    AntiqueWhite = 4294634455, // 0xFFFAEBD7
    Linen = 4294635750, // 0xFFFAF0E6
    LightGoldenrodYellow = 4294638290, // 0xFFFAFAD2
    OldLace = 4294833638, // 0xFFFDF5E6
    Red = 4294901760, // 0xFFFF0000
    Fuchsia = 4294902015, // 0xFFFF00FF
    Magenta = 4294902015, // 0xFFFF00FF
    DeepPink = 4294907027, // 0xFFFF1493
    OrangeRed = 4294919424, // 0xFFFF4500
    Tomato = 4294927175, // 0xFFFF6347
    HotPink = 4294928820, // 0xFFFF69B4
    Coral = 4294934352, // 0xFFFF7F50
    DarkOrange = 4294937600, // 0xFFFF8C00
    LightSalmon = 4294942842, // 0xFFFFA07A
    Orange = 4294944000, // 0xFFFFA500
    LightPink = 4294948545, // 0xFFFFB6C1
    Pink = 4294951115, // 0xFFFFC0CB
    Gold = 4294956800, // 0xFFFFD700
    PeachPuff = 4294957753, // 0xFFFFDAB9
    NavajoWhite = 4294958765, // 0xFFFFDEAD
    Moccasin = 4294960309, // 0xFFFFE4B5
    Bisque = 4294960324, // 0xFFFFE4C4
    MistyRose = 4294960353, // 0xFFFFE4E1
    BlanchedAlmond = 4294962125, // 0xFFFFEBCD
    PapayaWhip = 4294963157, // 0xFFFFEFD5
    LavenderBlush = 4294963445, // 0xFFFFF0F5
    SeaShell = 4294964718, // 0xFFFFF5EE
    Cornsilk = 4294965468, // 0xFFFFF8DC
    LemonChiffon = 4294965965, // 0xFFFFFACD
    FloralWhite = 4294966000, // 0xFFFFFAF0
    Snow = 4294966010, // 0xFFFFFAFA
    Yellow = 4294967040, // 0xFFFFFF00
    LightYellow = 4294967264, // 0xFFFFFFE0
    Ivory = 4294967280, // 0xFFFFFFF0
    White = 4294967295, // 0xFFFFFFFF
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CA1069
