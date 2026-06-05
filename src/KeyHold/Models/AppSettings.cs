namespace KeyHold.Models;

public sealed class AppSettings
{
    public ActivationMode ActivationMode { get; set; } = ActivationMode.SeparateKeys;

    public InputBinding EnableBinding { get; set; } = InputBinding.Keyboard(0x21);

    public InputBinding StopBinding { get; set; } = InputBinding.Keyboard(0x22);

    public InputBinding EmergencyBinding { get; set; } = InputBinding.Keyboard(0x7B);

    public InputBinding MouseTrigger { get; set; } = InputBinding.Mouse(MouseTriggerCode.XButton1);

    public ThemeMode Theme { get; set; } = ThemeMode.Dark;

    public bool LaunchToTray { get; set; } = true;

    public bool ShowNotifications { get; set; } = true;

    public bool SuppressTriggerInput { get; set; } = true;
}

