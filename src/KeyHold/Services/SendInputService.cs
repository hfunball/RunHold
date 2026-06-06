using System.Runtime.InteropServices;

namespace KeyHold.Services;

public sealed class SendInputService : IInputSender
{
    private const uint KeyEventFKeyUp = 0x0002;

    public void SendKeyDown(int virtualKey)
    {
        SendKey(virtualKey, 0);
    }

    public void SendKeyUp(int virtualKey)
    {
        SendKey(virtualKey, KeyEventFKeyUp);
    }

    private static void SendKey(int virtualKey, uint flags)
    {
        keybd_event((byte)virtualKey, 0, flags, InputInjectionMarker.KeyHoldInput);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
