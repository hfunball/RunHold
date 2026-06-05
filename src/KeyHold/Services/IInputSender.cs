namespace KeyHold.Services;

public interface IInputSender
{
    void SendKeyDown(int virtualKey);

    void SendKeyUp(int virtualKey);
}

