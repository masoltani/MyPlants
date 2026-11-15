using MyPlants.Businesses;

namespace MyPlants.Tests.Businesses;

public class PasswordRulesTests
{
    [Fact]
    public void Too_short_password_is_rejected()
    {
        Assert.NotNull(PasswordRules.Validate("short", "short"));
    }

    [Fact]
    public void Different_confirmation_is_rejected()
    {
        Assert.NotNull(PasswordRules.Validate("long-enough-1", "long-enough-2"));
    }

    [Fact]
    public void Valid_password_is_accepted()
    {
        Assert.Null(PasswordRules.Validate("long-enough-1", "long-enough-1"));
    }

    [Fact]
    public void Minimum_length_is_accepted()
    {
        var password = new string('a', PasswordRules.MinLength);

        Assert.Null(PasswordRules.Validate(password, password));
    }
}