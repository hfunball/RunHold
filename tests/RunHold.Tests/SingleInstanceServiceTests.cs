using RunHold.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RunHold.Tests;

[TestClass]
[DoNotParallelize]
public sealed class SingleInstanceServiceTests
{
    [TestMethod]
    public void TryStart_ReturnsFirstInstanceForUnusedName()
    {
        using var service = CreateService();

        var result = service.TryStart();

        Assert.IsTrue(result.IsFirstInstance);
        Assert.IsFalse(result.SignalDelivered);
        StringAssert.StartsWith(service.MutexName, @"Local\RunHold.Tests.SingleInstance.");
        StringAssert.StartsWith(service.SignalName, @"Local\RunHold.Tests.ShowSplash.");
    }

    [TestMethod]
    public void TryStart_DuplicateSignalsPrimaryInstance()
    {
        var userIdentifier = Guid.NewGuid().ToString("N");
        using var primary = CreateService(userIdentifier);
        using var duplicate = CreateService(userIdentifier);
        using var received = new ManualResetEventSlim();

        var primaryResult = primary.TryStart();
        primary.StartListening(() => received.Set());

        var duplicateResult = TryStartOnSeparateThread(duplicate);

        Assert.IsTrue(primaryResult.IsFirstInstance);
        Assert.IsFalse(duplicateResult.IsFirstInstance);
        Assert.IsTrue(duplicateResult.SignalDelivered);
        Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public void TryStart_DuplicateReportsSignalFailureWhenPrimaryHasNoSignalEvent()
    {
        var userIdentifier = Guid.NewGuid().ToString("N");
        using var namingService = CreateService(userIdentifier);
        using var duplicate = CreateService(userIdentifier);
        using var existingMutex = new Mutex(false, namingService.MutexName);
        Assert.IsTrue(existingMutex.WaitOne(0));

        try
        {
            var result = TryStartOnSeparateThread(duplicate);

            Assert.IsFalse(result.IsFirstInstance);
            Assert.IsFalse(result.SignalDelivered);
        }
        finally
        {
            existingMutex.ReleaseMutex();
        }
    }

    private static SingleInstanceService CreateService(string? userIdentifier = null)
    {
        return new SingleInstanceService("RunHold.Tests", userIdentifier ?? Guid.NewGuid().ToString("N"));
    }

    private static SingleInstanceStartResult TryStartOnSeparateThread(SingleInstanceService service)
    {
        SingleInstanceStartResult result = default;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = service.TryStart();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(2)), "Timed out waiting for duplicate launch simulation.");
        if (exception is not null)
        {
            throw exception;
        }

        return result;
    }
}
