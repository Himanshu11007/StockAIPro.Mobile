using StockAIPro.Mobile.Services.Authentication;
using StockAIPro.Mobile.Tests.Fakes;

namespace StockAIPro.Mobile.Tests;

public class PinServiceTests
{
    private static PinService BuildService(InMemoryPinStore? store = null, PinLockoutPolicy? policy = null) =>
        new(store ?? new InMemoryPinStore(), policy);

    [Fact]
    public async Task IsPinSetAsync_is_false_before_any_pin_is_set()
    {
        var service = BuildService();
        Assert.False(await service.IsPinSetAsync());
    }

    [Fact]
    public async Task SetPinAsync_then_IsPinSetAsync_is_true()
    {
        var service = BuildService();
        await service.SetPinAsync("1234", "1234");
        Assert.True(await service.IsPinSetAsync());
    }

    [Fact]
    public async Task SetPinAsync_never_stores_the_raw_pin()
    {
        var store = new InMemoryPinStore();
        var service = BuildService(store);

        await service.SetPinAsync("1234", "1234");

        var record = await store.GetAsync();
        Assert.NotNull(record);
        Assert.DoesNotContain("1234", record!.Hash);
        Assert.DoesNotContain("1234", record.Salt);
    }

    [Theory]
    [InlineData("123")]     // too short
    [InlineData("12345")]   // too long
    [InlineData("12ab")]    // non-numeric
    [InlineData("")]        // empty
    public async Task SetPinAsync_rejects_invalid_format(string invalidPin)
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetPinAsync(invalidPin, invalidPin));
    }

    [Fact]
    public async Task SetPinAsync_rejects_mismatched_confirmation()
    {
        var service = BuildService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetPinAsync("1234", "4321"));
    }

    [Fact]
    public async Task VerifyPinAsync_with_no_pin_set_returns_NoPinSet()
    {
        var service = BuildService();
        var result = await service.VerifyPinAsync("1234");
        Assert.Equal(PinVerifyOutcome.NoPinSet, result.Outcome);
    }

    [Fact]
    public async Task VerifyPinAsync_with_correct_pin_succeeds()
    {
        var service = BuildService();
        await service.SetPinAsync("1234", "1234");

        var result = await service.VerifyPinAsync("1234");

        Assert.Equal(PinVerifyOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task VerifyPinAsync_with_wrong_pin_fails_without_lockout_while_attempts_remain()
    {
        var policy = new PinLockoutPolicy { FreeAttempts = 3 };
        var service = BuildService(policy: policy);
        await service.SetPinAsync("1234", "1234");

        var result = await service.VerifyPinAsync("0000");

        Assert.Equal(PinVerifyOutcome.WrongPin, result.Outcome);
        Assert.Equal(2, result.AttemptsRemaining);
    }

    [Fact]
    public async Task VerifyPinAsync_locks_out_after_exceeding_free_attempts()
    {
        var policy = new PinLockoutPolicy { FreeAttempts = 2, LockoutDurations = [TimeSpan.FromSeconds(30)] };
        var service = BuildService(policy: policy);
        await service.SetPinAsync("1234", "1234");

        await service.VerifyPinAsync("0000"); // 1st wrong - still free
        await service.VerifyPinAsync("0000"); // 2nd wrong - still free
        var third = await service.VerifyPinAsync("0000"); // 3rd wrong - locks out

        Assert.Equal(PinVerifyOutcome.LockedOut, third.Outcome);
        Assert.NotNull(third.LockoutRemaining);
    }

    [Fact]
    public async Task VerifyPinAsync_rejects_even_the_correct_pin_while_locked_out()
    {
        var policy = new PinLockoutPolicy { FreeAttempts = 1, LockoutDurations = [TimeSpan.FromMinutes(5)] };
        var service = BuildService(policy: policy);
        await service.SetPinAsync("1234", "1234");

        await service.VerifyPinAsync("0000"); // free wrong attempt
        await service.VerifyPinAsync("0000"); // locks out

        var attemptWithCorrectPin = await service.VerifyPinAsync("1234");

        Assert.Equal(PinVerifyOutcome.LockedOut, attemptWithCorrectPin.Outcome);
    }

    [Fact]
    public async Task VerifyPinAsync_resets_failed_attempts_after_a_correct_verification()
    {
        var policy = new PinLockoutPolicy { FreeAttempts = 2, LockoutDurations = [TimeSpan.FromSeconds(30)] };
        var service = BuildService(policy: policy);
        await service.SetPinAsync("1234", "1234");

        await service.VerifyPinAsync("0000"); // 1 failed attempt used
        await service.VerifyPinAsync("1234"); // correct - resets counter

        var afterReset = await service.VerifyPinAsync("0000"); // should be free attempt #1 again, not a lockout
        Assert.Equal(PinVerifyOutcome.WrongPin, afterReset.Outcome);
    }

    [Fact]
    public async Task ClearPinAsync_removes_the_pin_entirely()
    {
        var service = BuildService();
        await service.SetPinAsync("1234", "1234");

        await service.ClearPinAsync();

        Assert.False(await service.IsPinSetAsync());
        var result = await service.VerifyPinAsync("1234");
        Assert.Equal(PinVerifyOutcome.NoPinSet, result.Outcome);
    }

    [Fact]
    public async Task Fallback_to_full_authentication_remains_available_regardless_of_lockout_state()
    {
        // This is a documentation-style assertion: PinService exposes no
        // mechanism that ever blocks ClearPinAsync (the operation the
        // "use another login method" UI flow calls) based on lockout state -
        // a locked-out PIN must never be a dead end.
        var policy = new PinLockoutPolicy { FreeAttempts = 0, LockoutDurations = [TimeSpan.FromHours(1)] };
        var service = BuildService(policy: policy);
        await service.SetPinAsync("1234", "1234");
        await service.VerifyPinAsync("0000"); // immediately locks out

        await service.ClearPinAsync(); // must not throw or be blocked by the lockout

        Assert.False(await service.IsPinSetAsync());
    }
}
