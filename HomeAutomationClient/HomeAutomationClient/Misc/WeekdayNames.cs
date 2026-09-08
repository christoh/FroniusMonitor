namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// The days of the week, as the region the machine is set to writes them.
/// </summary>
/// <remarks>
/// <para>
/// Not from the resource files: a weekday is not a term of this application that somebody has to translate, it is
/// something every culture on the machine already knows, and .NET has all of them - <c>Määntig</c> for
/// <c>gsw-CH</c> and <c>glindesdi</c> for <c>rm-CH</c> included.
/// </para>
/// <para>
/// From <see cref="CultureInfo.CurrentCulture"/> and not the UI culture, which is where the resource files come
/// from. A weekday is not a word this application chose, it is part of how the machine writes dates - the method
/// that answers it lives on <see cref="DateTimeFormat"/>, next to the date and number formats, and those follow
/// the region rather than the display language. Windows keeps the two apart on purpose: a Swiss machine with an
/// English display language reports a culture of <c>gsw-CH</c> and a UI culture of <c>en-US</c>, and somebody
/// who has set their region to Switzerland is asking for Swiss days.
/// </para>
/// <para>
/// The single letters in the heading of the schedule stay in the resource files, and the reason is the same
/// framework: its own short names for <c>gsw</c> give <c>D</c> for <c>Ziischtig</c>, which begins with a Z, and
/// for <c>rm</c> they give <c>G</c> to both <c>glindesdi</c> and <c>gievgia</c>. Which letter to use when two
/// days collide is a matter of judgement, and the resource files make it deliberately.
/// </para>
/// </remarks>
public static class WeekdayNames
{
    public static string Monday => NameOf(DayOfWeek.Monday);

    public static string Tuesday => NameOf(DayOfWeek.Tuesday);

    public static string Wednesday => NameOf(DayOfWeek.Wednesday);

    public static string Thursday => NameOf(DayOfWeek.Thursday);

    public static string Friday => NameOf(DayOfWeek.Friday);

    public static string Saturday => NameOf(DayOfWeek.Saturday);

    public static string Sunday => NameOf(DayOfWeek.Sunday);

    private static string NameOf(DayOfWeek day) => CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(day);
}
