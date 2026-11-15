using MyPlants.Businesses;
using MyPlants.Models;

namespace MyPlants.Tests.Businesses;

public class PlantSorterTests
{
    private static readonly DateTime Today = new(2026, 11, 14);

    private static Plant MakePlant(string name, int? interval = 3, DateTime? lastWatered = null, DateTime? planted = null, bool archived = false) =>
        new() { Name = name, WateringInterval = interval, LastWatered = lastWatered, DatePlanted = planted, IsArchived = archived };

    [Fact]
    public void Overdue_plant_has_negative_days()
    {
        // Arrosée le 10, intervalle 3 jours : à arroser le 13, donc en retard d'un jour le 14.
        var plant = MakePlant("A", lastWatered: new DateTime(2026, 11, 10));

        Assert.Equal(-1, PlantSorter.GetDaysUntilWatering(plant, Today));
    }

    [Fact]
    public void Plant_due_today_has_zero_days()
    {
        var plant = MakePlant("A", lastWatered: new DateTime(2026, 11, 11));

        Assert.Equal(0, PlantSorter.GetDaysUntilWatering(plant, Today));
    }

    [Fact]
    public void Plant_due_later_has_positive_days()
    {
        var plant = MakePlant("A", lastWatered: new DateTime(2026, 11, 12));

        Assert.Equal(1, PlantSorter.GetDaysUntilWatering(plant, Today));
    }

    [Fact]
    public void Time_of_day_is_ignored()
    {
        var plant = MakePlant("A", lastWatered: new DateTime(2026, 11, 11, 23, 59, 0));

        Assert.Equal(0, PlantSorter.GetDaysUntilWatering(plant, Today.AddHours(8)));
    }

    [Fact]
    public void Never_watered_plant_counts_from_its_planting_date()
    {
        var plant = MakePlant("A", interval: 7, planted: new DateTime(2026, 11, 1));

        Assert.Equal(-6, PlantSorter.GetDaysUntilWatering(plant, Today));
    }

    [Fact]
    public void Unknown_schedule_gives_null()
    {
        Assert.Null(PlantSorter.GetDaysUntilWatering(MakePlant("archivée", lastWatered: Today, archived: true), Today));
        Assert.Null(PlantSorter.GetDaysUntilWatering(MakePlant("sans intervalle", interval: null, lastWatered: Today), Today));
        Assert.Null(PlantSorter.GetDaysUntilWatering(MakePlant("sans date"), Today));
    }

    [Fact]
    public void Urgency_order_puts_the_most_overdue_first_and_unknown_last()
    {
        var later = MakePlant("later", lastWatered: new DateTime(2026, 11, 13));    // +2
        var overdue = MakePlant("overdue", lastWatered: new DateTime(2026, 11, 8));  // -3
        var unknown = MakePlant("unknown");

        var sorted = PlantSorter.Sort([later, unknown, overdue], PlantSortOrder.Urgency, Today);

        Assert.Equal(["overdue", "later", "unknown"], sorted.Select(plant => plant.Name));
    }

    [Fact]
    public void Name_order_ignores_case()
    {
        var sorted = PlantSorter.Sort([MakePlant("banana"), MakePlant("Apple"), MakePlant("cherry")], PlantSortOrder.Name, Today);

        Assert.Equal(["Apple", "banana", "cherry"], sorted.Select(plant => plant.Name));
    }

    [Fact]
    public void Date_planted_order_puts_the_most_recent_first_and_undated_last()
    {
        var old = MakePlant("old", planted: new DateTime(2025, 1, 1));
        var recent = MakePlant("recent", planted: new DateTime(2026, 6, 1));
        var undated = MakePlant("undated");

        var sorted = PlantSorter.Sort([old, undated, recent], PlantSortOrder.DatePlanted, Today);

        Assert.Equal(["recent", "old", "undated"], sorted.Select(plant => plant.Name));
    }
}