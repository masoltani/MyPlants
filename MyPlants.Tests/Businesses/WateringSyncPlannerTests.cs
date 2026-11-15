using MyPlants.Businesses;
using MyPlants.Models;

namespace MyPlants.Tests.Businesses;

public class WateringSyncPlannerTests
{
    private static Watering MakeWatering(Guid plantId, Guid? id = null, bool isSynced = false) =>
        new() { Id = id ?? Guid.NewGuid(), PlantId = plantId, IsSynced = isSynced };

    [Fact]
    public void Unsynced_watering_of_a_plant_known_to_the_cloud_is_pushed()
    {
        var plantId = Guid.NewGuid();
        var watering = MakeWatering(plantId);

        var plan = WateringSyncPlanner.Plan([watering], [], new HashSet<Guid> { plantId });

        Assert.Same(watering, Assert.Single(plan.ToPush));
        Assert.Empty(plan.ToPull);
        Assert.Empty(plan.ToMarkSynced);
    }

    [Fact]
    public void Unsynced_watering_of_a_plant_missing_from_the_cloud_stays_local()
    {
        var watering = MakeWatering(Guid.NewGuid());

        var plan = WateringSyncPlanner.Plan([watering], [], new HashSet<Guid>());

        Assert.Empty(plan.ToPush);
        Assert.Empty(plan.ToMarkSynced);
    }

    [Fact]
    public void Unsynced_watering_already_in_the_cloud_is_only_marked_as_synced()
    {
        var plantId = Guid.NewGuid();
        var id = Guid.NewGuid();

        var plan = WateringSyncPlanner.Plan(
            [MakeWatering(plantId, id)],
            [MakeWatering(plantId, id, isSynced: true)],
            new HashSet<Guid> { plantId });

        Assert.Empty(plan.ToPush);
        Assert.Single(plan.ToMarkSynced);
        Assert.Empty(plan.ToPull);
    }

    [Fact]
    public void Synced_local_watering_needs_nothing()
    {
        var plantId = Guid.NewGuid();

        var plan = WateringSyncPlanner.Plan(
            [MakeWatering(plantId, isSynced: true)], [], new HashSet<Guid> { plantId });

        Assert.Empty(plan.ToPush);
        Assert.Empty(plan.ToPull);
        Assert.Empty(plan.ToMarkSynced);
    }

    [Fact]
    public void Cloud_watering_unknown_locally_is_pulled()
    {
        var cloud = MakeWatering(Guid.NewGuid(), isSynced: true);

        var plan = WateringSyncPlanner.Plan([], [cloud], new HashSet<Guid>());

        Assert.Same(cloud, Assert.Single(plan.ToPull));
    }

    [Fact]
    public void Cloud_watering_already_known_locally_is_not_pulled_again()
    {
        var plantId = Guid.NewGuid();
        var id = Guid.NewGuid();

        var plan = WateringSyncPlanner.Plan(
            [MakeWatering(plantId, id, isSynced: true)],
            [MakeWatering(plantId, id, isSynced: true)],
            new HashSet<Guid> { plantId });

        Assert.Empty(plan.ToPull);
    }
}