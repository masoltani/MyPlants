using MyPlants.Businesses;
using MyPlants.Models;

namespace MyPlants.Tests.Businesses;

public class ReminderSnapshotTests
{
    private static readonly DateTime Today = new(2026, 11, 16);

    private static Plant MakePlant(int? interval, DateTime? lastWatered, bool archived = false) =>
        new() { Name = "P", WateringInterval = interval, LastWatered = lastWatered, IsArchived = archived };

    [Fact]
    public void Each_plant_gives_its_next_watering_date_sorted()
    {
        var dates = ReminderSnapshot.BuildDates(
            [
                MakePlant(7, new DateTime(2026, 11, 14)),   // 21 nov.
                MakePlant(3, new DateTime(2026, 11, 10)),   // 13 nov. (en retard)
                MakePlant(1, new DateTime(2026, 11, 15))    // 16 nov. (aujourd'hui)
            ],
            Today);

        Assert.Equal(["2026-11-13", "2026-11-16", "2026-11-21"], dates);
    }

    [Fact]
    public void Plants_without_a_known_schedule_are_left_out()
    {
        var dates = ReminderSnapshot.BuildDates(
            [
                MakePlant(null, new DateTime(2026, 11, 14)),                 // pas d'intervalle
                MakePlant(3, new DateTime(2026, 11, 10), archived: true),    // archivée
                MakePlant(3, null)                                           // ni arrosage ni plantation
            ],
            Today);

        Assert.Empty(dates);
    }

    [Fact]
    public void Dates_use_the_international_format_whatever_the_culture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("ar-SA");

            var dates = ReminderSnapshot.BuildDates([MakePlant(1, new DateTime(2026, 11, 15))], Today);

            Assert.Equal(["2026-11-16"], dates);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}