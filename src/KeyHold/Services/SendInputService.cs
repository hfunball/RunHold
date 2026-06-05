using System.Runtime.InteropServices;

namespace KeyHold.Services;

public sealed class SendInputService : IInputSender
{
    private const int InputKeyboard = 1;
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
        var input = new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = (ushort)virtualKey,
                    ScanCode = 0,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = UIntPtr.Zero
                }
            }
        };

        _ = SendInput(1, [input], Marshal.SizeOf<Input>());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public int Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }
}

