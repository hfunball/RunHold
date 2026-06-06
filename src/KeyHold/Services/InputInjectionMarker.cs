namespace KeyHold.Services;

internal static class InputInjectionMarker
{
    public const string AcceptExternalInjectedInputEnvironmentVariable = "KEYHOLD_ACCEPT_EXTERNAL_INJECTED_INPUT_FOR_SMOKE";

    public static readonly UIntPtr KeyHoldInput = new(0x4B48444);
}
