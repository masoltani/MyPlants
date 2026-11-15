using MyPlants.Models;
using MyPlants.Services;

namespace MyPlants.Tests.Businesses;

public class BackupValidationTests
{
    [Fact]
    public void Default_backup_is_valid() => Assert.True(BackupService.IsValid(new BackupFile()));

    [Fact]
    public void Null_is_invalid() => Assert.False(BackupService.IsValid(null));

    [Fact]
    public void File_from_another_application_is_invalid() =>
        Assert.False(BackupService.IsValid(new BackupFile { App = "SomethingElse" }));

    [Fact]
    public void File_from_a_newer_format_is_invalid() =>
        Assert.False(BackupService.IsValid(new BackupFile { FormatVersion = BackupFile.CurrentFormatVersion + 1 }));

    [Fact]
    public void Too_many_plants_is_invalid()
    {
        var backup = new BackupFile
        {
            Plants = Enumerable.Range(0, BackupService.MaxPlants + 1).Select(_ => new Plant()).ToList()
        };

        Assert.False(BackupService.IsValid(backup));
    }

    [Fact]
    public void Exactly_the_maximum_number_of_plants_is_valid()
    {
        var backup = new BackupFile
        {
            Plants = Enumerable.Range(0, BackupService.MaxPlants).Select(_ => new Plant()).ToList()
        };

        Assert.True(BackupService.IsValid(backup));
    }
}