using ReservationBackend.Validation;

namespace ReservationBackend.Test;

public class ValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateServiceName_Blank_ReturnsError(string? name)
    {
        var error = Validators.ValidateServiceName(name);

        Assert.NotNull(error);
        Assert.Equal(Validators.ServiceNameRequiredCode, error.Value.Code);
    }

    [Theory]
    [InlineData("Consultation")]
    [InlineData("  Massage  ")]
    public void ValidateServiceName_NonBlank_ReturnsNull(string name)
    {
        Assert.Null(Validators.ValidateServiceName(name));
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ValidateServiceDuration_ZeroOrNegative_ReturnsError(int duration)
    {
        var error = Validators.ValidateServiceDuration(duration);

        Assert.NotNull(error);
        Assert.Equal(Validators.ServiceDurationInvalidCode, error.Value.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(120)]
    public void ValidateServiceDuration_Positive_ReturnsNull(int duration)
    {
        Assert.Null(Validators.ValidateServiceDuration(duration));
    }


    [Fact]
    public void ValidateSlotServiceId_Empty_ReturnsError()
    {
        var error = Validators.ValidateSlotServiceId(Guid.Empty);

        Assert.NotNull(error);
        Assert.Equal(Validators.SlotServiceRequiredCode, error.Value.Code);
    }

    [Fact]
    public void ValidateSlotServiceId_Provided_ReturnsNull()
    {
        Assert.Null(Validators.ValidateSlotServiceId(Guid.CreateVersion7()));
    }


    [Fact]
    public void ValidateSlotStartTime_NowOrPast_ReturnsError()
    {
        var error = Validators.ValidateSlotStartTime(DateTime.UtcNow);

        Assert.NotNull(error);
        Assert.Equal(Validators.SlotStartInPastCode, error.Value.Code);
    }

    [Fact]
    public void ValidateSlotStartTime_InFuture_ReturnsNull()
    {
        Assert.Null(Validators.ValidateSlotStartTime(DateTime.UtcNow.AddMinutes(5)));
    }


    [Fact]
    public void ValidateSlotId_Empty_ReturnsError()
    {
        var error = Validators.ValidateSlotId(Guid.Empty);

        Assert.NotNull(error);
        Assert.Equal(Validators.SlotIdRequiredCode, error.Value.Code);
    }

    [Fact]
    public void ValidateSlotId_Provided_ReturnsNull()
    {
        Assert.Null(Validators.ValidateSlotId(Guid.CreateVersion7()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateClientName_Blank_ReturnsError(string? name)
    {
        var error = Validators.ValidateClientName(name);

        Assert.NotNull(error);
        Assert.Equal(Validators.ClientNameRequiredCode, error.Value.Code);
    }

    [Fact]
    public void ValidateClientName_NonBlank_ReturnsNull()
    {
        Assert.Null(Validators.ValidateClientName("  Jane Doe  "));
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizePhoneNumber_Blank_ReturnsNull(string? phone)
    {
        Assert.Null(Validators.NormalizePhoneNumber(phone));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("+37529 123-45-67 extra")]
    [InlineData("12345")] // too few digits
    [InlineData("1234567890123456")] // too many digits (16)
    [InlineData("++375291234567")]
    [InlineData("375+291234567")] // plus not at start
    public void NormalizePhoneNumber_Malformed_ReturnsNull(string phone)
    {
        Assert.Null(Validators.NormalizePhoneNumber(phone));
    }

    [Theory]
    [InlineData("+375291234567", "+375291234567")]
    [InlineData("375291234567", "375291234567")] // without leading +
    [InlineData("+375 (29) 123-45-67", "+375291234567")]
    [InlineData("+375 29 123-45-67", "+375291234567")]
    [InlineData("   +375  291 234 567  ", "+375291234567")]
    public void NormalizePhoneNumber_Valid_ReturnsNormalized(string input, string expected)
    {
        Assert.Equal(expected, Validators.NormalizePhoneNumber(input));
    }
}