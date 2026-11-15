using MyPlants.Businesses;
using MyPlants.Models;

using System.Globalization;

namespace MyPlants.Tests.Businesses;

public class DateDisplayFormatterTests
{
    private static readonly DateTime Sample = new(2026, 11, 25);

    [Theory]
    [InlineData("dd/MM/yyyy", "25/11/2026")]
    [InlineData("MM/dd/yyyy", "11/25/2026")]
    [InlineData("yyyy-MM-dd", "2026-11-25")]
    [InlineData("dd.MM.yyyy", "25.11.2026")]
    public void Gregorian_dates_follow_the_chosen_pattern(string pattern, string expected)
    {
        var text = DateDisplayFormatter.Format(Sample, new DateDisplaySettings(DateCalendar.Gregorian, pattern));

        Assert.Equal(expected, text);
    }

    [Fact]
    public void Day_and_month_are_zero_padded()
    {
        var text = DateDisplayFormatter.Format(new DateTime(2026, 1, 5), DateDisplaySettings.Default);

        Assert.Equal("05/01/2026", text);
    }

    [Fact]
    public void Hijri_date_converts_back_to_the_same_gregorian_day()
    {
        var text = DateDisplayFormatter.Format(Sample, new DateDisplaySettings(DateCalendar.Hijri, "yyyy-MM-dd"));

        var parts = text.Split('-').Select(part => int.Parse(part, CultureInfo.InvariantCulture)).ToArray();
        var back = new UmAlQuraCalendar().ToDateTime(parts[0], parts[1], parts[2], 0, 0, 0, 0);

        Assert.Equal(1448, parts[0]);
        Assert.Equal(Sample, back);
    }

    [Fact]
    public void Date_outside_the_hijri_range_falls_back_to_an_international_format()
    {
        var text = DateDisplayFormatter.Format(
            new DateTime(1850, 1, 1),
            new DateDisplaySettings(DateCalendar.Hijri, "dd/MM/yyyy"));

        Assert.Equal("1850-01-01", text);
    }
}