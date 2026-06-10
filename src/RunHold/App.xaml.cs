using System.IO;
using System.Windows;
using System.Windows.Threading;
using RunHold.Models;
using RunHold.Services;
using Microsoft.Win32;

namespace RunHold;

public partial class App
{
    private SingleInstanceService? singleInstance;
    private NotifyIconHost? notifyIconHost;
    private KeyboardHookService? keyboardHook;
    private MouseHookService? mouseHook;
    private RunHoldEngine? engine;
    private MainWindow? mainWindow;
    private StartupSplashWindow? startupSplashWindow;
    private bool safetyHandlersRegistered;

    protected override void OnStartup(StartupEventArgs e)
    {
        singleInstance = new SingleInstanceService();
        var singleInstanceResult = singleInstance.TryStart();
        base.OnStartup(e);

        if (!singleInstanceResult.IsFirstInstance)
        {
            if (singleInstanceResult.SignalDelivered)
            {
                Shutdown();
                return;
            }

            ThemeService.Apply(RunHold.Models.ThemeMode.System);
            ShowStartupSplashAndExit();
            return;
        }

        singleInstance.StartListening(() => QueueOnUi(ShowStartupSplash));

        var config = new ConfigService();
        var settings = config.Load();
        var isFirstRun = !settings.HasSeenFirstRun;
        ThemeService.Apply(settings.Theme);

        var sender = new SendInputService();
        engine = new RunHoldEngine(settings, sender);
        keyboardHook = new KeyboardHookService(engine);
        mouseHook = new MouseHookService(engine);
        notifyIconHost = new NotifyIconHost(engine, ShowMainWindow, ExitApplication);
        mainWindow = new MainWindow(settings, config, engine, new StartupService());
        RegisterSafetyHandlers();
        if (isFirstRun)
        {
            settings.HasSeenFirstRun = true;
            try
            {
                config.Save(settings);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                mainWindow.AddDiagnostic(new DiagnosticEntry(DateTime.Now, $"Could not save first-run setting: {ex.Message}"));
            }
        }

        keyboardHook.Start();
        mouseHook.Start();
        notifyIconHost.Update(engine.Status);

        engine.StatusChanged += (_, status) =>
        {
            QueueOnUi(() =>
            {
                notifyIconHost.Update(status);
                mainWindow?.UpdateStatus(status);
            });
        };

        engine.DiagnosticLogged += (_, entry) =>
        {
            QueueOnUi(() => mainWindow?.AddDiagnostic(entry));
        };

        engine.HoldRecorded += (_, entry) =>
        {
            QueueOnUi(() => mainWindow?.AddHoldHistory(entry));
        };

        if (settings.LaunchToTray)
        {
            if (isFirstRun)
            {
                ShowStartupSplash();
            }

            mainWindow.Hide();
            return;
        }

        ShowMainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ReleaseHeldKeys("Application exit");
        UnregisterSafetyHandlers();
        engine?.Dispose();
        keyboardHook?.Dispose();
        mouseHook?.Dispose();
        notifyIconHost?.Dispose();
        singleInstance?.Dispose();
        base.OnExit(e);
    }

    private void ShowMainWindow()
    {
        if (mainWindow is null)
        {
            return;
        }

        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }

    private void ExitApplication()
    {
        mainWindow?.AllowClose();
        Shutdown();
    }

    private void ShowStartupSplash()
    {
        if (startupSplashWindow is { IsVisible: true })
        {
            startupSplashWindow.Restart();
            return;
        }

        var splash = new StartupSplashWindow();
        startupSplashWindow = splash;
        splash.Closed += (_, _) =>
        {
            if (ReferenceEquals(startupSplashWindow, splash))
            {
                startupSplashWindow = null;
            }
        };
        splash.Show();
    }

    private void ShowStartupSplashAndExit()
    {
        ShowStartupSplash();
        if (startupSplashWindow is not { } splash)
        {
            Shutdown();
            return;
        }

        splash.Closed += (_, _) => Shutdown();
    }

    private void RegisterSafetyHandlers()
    {
        if (safetyHandlersRegistered)
        {
            return;
        }

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        safetyHandlersRegistered = true;
    }

    private void UnregisterSafetyHandlers()
    {
        if (!safetyHandlersRegistered)
        {
            return;
        }

        DispatcherUnhandledException -= App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
        SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
        SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        safetyHandlersRegistered = false;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReleaseHeldKeys("Unhandled UI exception");
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ReleaseHeldKeys("Unhandled exception");
    }

    private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        ReleaseHeldKeys("Process exit");
    }

    private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
    {
        ReleaseHeldKeys("Session ending");
    }

    private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            ReleaseHeldKeys("Power suspend");
        }
    }

    private void ReleaseHeldKeys(string reason)
    {
        try
        {
            engine?.ReleaseAll(reason);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
        }
    }

    private void QueueOnUi(Action action)
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }

        try
        {
            Dispatcher.InvokeAsync(action, DispatcherPriority.Background);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
