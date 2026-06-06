using KeyHold.Models;

namespace KeyHold.Services;

public sealed class KeyHoldEngine
{
    private readonly object gate = new();
    private readonly IInputSender inputSender;
    private readonly HashSet<int> physicalKeysDown = [];
    private readonly HashSet<int> heldKeys = [];
    private AppSettings settings;
    private bool uiCaptureActive;

    public KeyHoldEngine(AppSettings settings, IInputSender inputSender)
    {
        this.settings = settings;
        this.inputSender = inputSender;
        Status = new HoldStatus(false, Array.Empty<int>(), "Idle");
    }

    public HoldStatus Status { get; private set; }

    public event EventHandler<HoldStatus>? StatusChanged;

    public event EventHandler<DiagnosticEntry>? DiagnosticLogged;

    public void UpdateSettings(AppSettings newSettings)
    {
        lock (gate)
        {
            settings = newSettings;
        }

        Log("Settings updated.");
    }

    public void SetUiCaptureActive(bool isActive)
    {
        lock (gate)
        {
            uiCaptureActive = isActive;
        }
    }

    public bool HandleKeyboardEvent(KeyboardInputEvent input)
    {
        if (input.IsInjected)
        {
            return false;
        }

        var suppress = false;

        lock (gate)
        {
            if (uiCaptureActive)
            {
                return false;
            }

            if (input.IsDown)
            {
                if (settings.EmergencyBinding.MatchesKeyboard(input.VirtualKey))
                {
                    ReleaseAllLocked("Emergency hotkey");
                    LogLocked($"Emergency release: {VirtualKeyNames.GetName(input.VirtualKey)}");
                    return settings.SuppressTriggerInput;
                }

                if (IsEnableTrigger(input.VirtualKey))
                {
                    ActivateOrToggleLocked();
                    suppress = settings.SuppressTriggerInput;
                }
                else if (IsStopTrigger(input.VirtualKey))
                {
                    ReleaseAllLocked("Stop hotkey");
                    suppress = settings.SuppressTriggerInput;
                }
                else
                {
                    physicalKeysDown.Add(input.VirtualKey);
                }
            }
            else
            {
                if (IsAnyKeyboardTrigger(input.VirtualKey))
                {
                    suppress = settings.SuppressTriggerInput;
                }
                else
                {
                    physicalKeysDown.Remove(input.VirtualKey);
                }

                if (heldKeys.Contains(input.VirtualKey))
                {
                    suppress = true;
                }
            }

            LogLocked($"{(input.IsDown ? "Down" : "Up")}: {VirtualKeyNames.GetName(input.VirtualKey)}{(suppress ? " (suppressed)" : string.Empty)}");
        }

        return suppress;
    }

    public bool HandleMouseEvent(MouseInputEvent input)
    {
        if (input.IsInjected || !settings.MouseTrigger.MatchesMouse(input.Button))
        {
            return false;
        }

        lock (gate)
        {
            if (uiCaptureActive)
            {
                return false;
            }

            if (input.IsDown && settings.ActivationMode == ActivationMode.MouseTrigger)
            {
                ActivateOrToggleLocked();
            }

            LogLocked($"{(input.IsDown ? "Mouse down" : "Mouse up")}: {input.Button}");
            return settings.SuppressTriggerInput;
        }
    }

    public void ReleaseAll(string reason)
    {
        lock (gate)
        {
            ReleaseAllLocked(reason);
        }
    }

    private void ActivateOrToggleLocked()
    {
        if ((settings.ActivationMode == ActivationMode.Toggle || settings.ActivationMode == ActivationMode.MouseTrigger) && heldKeys.Count > 0)
        {
            ReleaseAllLocked("Toggle release");
            return;
        }

        if (heldKeys.Count > 0)
        {
            ReleaseAllLocked("New hold started");
        }

        var snapshot = physicalKeysDown
            .Where(key => !IsAnyKeyboardTrigger(key))
            .OrderBy(key => key)
            .ToArray();

        if (snapshot.Length == 0)
        {
            LogLocked("Activation ignored: no non-trigger keys are currently held.");
            PublishStatusLocked("No keys captured");
            return;
        }

        foreach (var key in snapshot)
        {
            inputSender.SendKeyDown(key);
            heldKeys.Add(key);
        }

        PublishStatusLocked("Hold active");
        LogLocked($"Holding {string.Join(", ", snapshot.Select(VirtualKeyNames.GetName))}.");
    }

    private void ReleaseAllLocked(string reason)
    {
        if (heldKeys.Count == 0)
        {
            PublishStatusLocked(reason);
            return;
        }

        foreach (var key in heldKeys.OrderByDescending(key => key).ToArray())
        {
            inputSender.SendKeyUp(key);
        }

        heldKeys.Clear();
        PublishStatusLocked(reason);
        LogLocked($"Released all keys: {reason}.");
    }

    private bool IsEnableTrigger(int virtualKey)
    {
        return settings.ActivationMode switch
        {
            ActivationMode.SeparateKeys => settings.EnableBinding.MatchesKeyboard(virtualKey),
            ActivationMode.Toggle => settings.EnableBinding.MatchesKeyboard(virtualKey),
            _ => false
        };
    }

    private bool IsStopTrigger(int virtualKey)
    {
        return settings.ActivationMode == ActivationMode.SeparateKeys && settings.StopBinding.MatchesKeyboard(virtualKey);
    }

    private bool IsAnyKeyboardTrigger(int virtualKey)
    {
        if (settings.EmergencyBinding.MatchesKeyboard(virtualKey))
        {
            return true;
        }

        return settings.ActivationMode switch
        {
            ActivationMode.SeparateKeys => settings.EnableBinding.MatchesKeyboard(virtualKey)
                || settings.StopBinding.MatchesKeyboard(virtualKey),
            ActivationMode.Toggle => settings.EnableBinding.MatchesKeyboard(virtualKey),
            _ => false
        };
    }

    private void PublishStatusLocked(string reason)
    {
        Status = new HoldStatus(heldKeys.Count > 0, heldKeys.ToArray(), reason);
        StatusChanged?.Invoke(this, Status);
    }

    private void Log(string message)
    {
        DiagnosticLogged?.Invoke(this, new DiagnosticEntry(DateTime.Now, message));
    }

    private void LogLocked(string message)
    {
        DiagnosticLogged?.Invoke(this, new DiagnosticEntry(DateTime.Now, message));
    }
}
