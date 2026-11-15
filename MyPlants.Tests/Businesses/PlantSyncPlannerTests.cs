using MyPlants.Businesses;
using MyPlants.Models;

namespace MyPlants.Tests.Businesses;

public class PlantSyncPlannerTests
{
    private static readonly DateTime T0 = new(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);

    private static Plant MakePlant(Guid? id = null, DateTime? updatedAt = null, bool isSynced = false, DateTime? deletedAt = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Ficus",
            UpdatedAt = updatedAt ?? T0,
            IsSynced = isSynced,
            DeletedAt = deletedAt
        };

    [Fact]
    public void Local_plant_unknown_to_cloud_and_unsynced_is_pushed()
    {
        var local = MakePlant();

        var plan = PlantSyncPlanner.Plan([local], []);

        Assert.Single(plan.ToPush);
        Assert.Empty(plan.ToPull);
    }

    [Fact]
    public void Local_plant_unknown_to_cloud_but_synced_is_left_alone()
    {
        var plan = PlantSyncPlanner.Plan([MakePlant(isSynced: true)], []);

        Assert.Empty(plan.ToPush);
        Assert.Empty(plan.ToPull);
    }

    [Fact]
    public void Newer_cloud_version_is_pulled()
    {
        var id = Guid.NewGuid();
        var local = MakePlant(id, T0, isSynced: true);
        var cloud = MakePlant(id, T0.AddMinutes(5), isSynced: true);

        var plan = PlantSyncPlanner.Plan([local], [cloud]);

        Assert.Empty(plan.ToPush);
        Assert.Same(cloud, Assert.Single(plan.ToPull));
    }

    [Fact]
    public void Newer_cloud_version_wins_over_an_unsynced_older_local_edit()
    {
        var id = Guid.NewGuid();
        var local = MakePlant(id, T0, isSynced: false);
        var cloud = MakePlant(id, T0.AddMinutes(5));

        var plan = PlantSyncPlanner.Plan([local], [cloud]);

        Assert.Empty(plan.ToPush);
        Assert.Single(plan.ToPull);
    }

    [Fact]
    public void Newer_unsynced_local_version_is_pushed()
    {
        var id = Guid.NewGuid();
        var local = MakePlant(id, T0.AddMinutes(5), isSynced: false);
        var cloud = MakePlant(id, T0);

        var plan = PlantSyncPlanner.Plan([local], [cloud]);

        Assert.Same(local, Assert.Single(plan.ToPush));
        Assert.Empty(plan.ToPull);
    }

    [Fact]
    public void Same_version_and_synced_means_nothing_to_do()
    {
        var id = Guid.NewGuid();

        var plan = PlantSyncPlanner.Plan([MakePlant(id, T0, isSynced: true)], [MakePlant(id, T0)]);

        Assert.Empty(plan.ToPush);
        Assert.Empty(plan.ToPull);
    }

    [Fact]
    public void Same_version_but_unsynced_is_pushed()
    {
        var id = Guid.NewGuid();

        var plan = PlantSyncPlanner.Plan([MakePlant(id, T0, isSynced: false)], [MakePlant(id, T0)]);

        Assert.Single(plan.ToPush);
        Assert.Empty(plan.ToPull);
    }

    [Fact]
    public void Plant_created_on_another_device_is_pulled()
    {
        var cloud = MakePlant();

        var plan = PlantSyncPlanner.Plan([], [cloud]);

        Assert.Same(cloud, Assert.Single(plan.ToPull));
        Assert.Empty(plan.ToPush);
    }

    [Fact]
    public void Deleted_cloud_plant_unknown_locally_is_ignored()
    {
        var plan = PlantSyncPlanner.Plan([], [MakePlant(deletedAt: T0)]);

        Assert.Empty(plan.ToPull);
        Assert.Empty(plan.ToPush);
    }

    [Fact]
    public void Versions_are_compared_as_instants_whatever_their_time_zone_kind()
    {
        var id = Guid.NewGuid();
        var utc = MakePlant(id, T0, isSynced: true);
        var sameInstantInLocalTime = MakePlant(id, T0.ToLocalTime());

        var plan = PlantSyncPlanner.Plan([utc], [sameInstantInLocalTime]);

        Assert.Empty(plan.ToPull);
        Assert.Empty(plan.ToPush);
    }
}