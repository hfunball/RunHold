namespace KeyHold.Models;

public sealed class AppSettings
{
    public ActivationMode ActivationMode { get; set; } = ActivationMode.Toggle;

    public InputBinding EnableBinding { get; set; } = InputBinding.Keyboard(0x24);

    public InputBinding StopBinding { get; set; } = InputBinding.Keyboard(0x22);

    public InputBinding EmergencyBinding { get; set; } = InputBinding.Keyboard(0x7B);

    public InputBinding MouseTrigger { get; set; } = InputBinding.Mouse(MouseTriggerCode.XButton1);

    public KeyEmulationMode KeyEmulationMode { get; set; } = KeyEmulationMode.StableHold;

    public int RepeatedPressIntervalMilliseconds { get; set; } = 45;

    public ThemeMode Theme { get; set; } = ThemeMode.Dark;

    public bool LaunchToTray { get; set; } = true;

    public bool ShowNotifications { get; set; } = true;

    public bool SuppressTriggerInput { get; set; } = true;

    public bool HasSeenFirstRun { get; set; }
}
