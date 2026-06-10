using System.Security.Principal;
using System.Text;

namespace RunHold.Services;

public sealed class SingleInstanceService : IDisposable
{
    private readonly string mutexName;
    private readonly string signalName;
    private Mutex? mutex;
    private EventWaitHandle? signalEvent;
    private RegisteredWaitHandle? registeredWait;
    private bool ownsMutex;

    public SingleInstanceService()
        : this("RunHold", WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName)
    {
    }

    internal SingleInstanceService(string appName, string userIdentifier)
    {
        var safeAppName = NormalizeNamePart(appName);
        var safeUserIdentifier = NormalizeNamePart(userIdentifier);
        mutexName = $@"Local\{safeAppName}.SingleInstance.{safeUserIdentifier}";
        signalName = $@"Local\{safeAppName}.ShowSplash.{safeUserIdentifier}";
    }

    internal string MutexName => mutexName;

    internal string SignalName => signalName;

    public SingleInstanceStartResult TryStart()
    {
        mutex = new Mutex(false, mutexName);
        try
        {
            ownsMutex = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            ownsMutex = true;
        }

        if (ownsMutex)
        {
            signalEvent = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
            return SingleInstanceStartResult.FirstInstance;
        }

        mutex.Dispose();
        mutex = null;
        return new SingleInstanceStartResult(false, SignalExistingInstance());
    }

    public void StartListening(Action signalReceived)
    {
        if (!ownsMutex || signalEvent is null)
        {
            throw new InvalidOperationException("Only the primary RunHold instance can listen for duplicate launches.");
        }

        registeredWait = ThreadPool.RegisterWaitForSingleObject(
            signalEvent,
            (_, timedOut) =>
            {
                if (!timedOut)
                {
                    signalReceived();
                }
            },
            null,
            Timeout.InfiniteTimeSpan,
            false);
    }

    public void Dispose()
    {
        registeredWait?.Unregister(null);
        signalEvent?.Dispose();

        if (ownsMutex && mutex is not null)
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }

        mutex?.Dispose();
    }

    private bool SignalExistingInstance()
    {
        try
        {
            using var existingSignal = EventWaitHandle.OpenExisting(signalName);
            existingSignal.Set();
            return true;
        }
        catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string NormalizeNamePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '.' ? character : '_');
        }

        return builder.Length == 0 ? "Unknown" : builder.ToString();
    }
}

public readonly record struct SingleInstanceStartResult(bool IsFirstInstance, bool SignalDelivered)
{
    public static SingleInstanceStartResult FirstInstance { get; } = new(true, false);
}
