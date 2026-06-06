using KeyHold.Models;
using KeyHold.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeyHold.Tests;

[TestClass]
public sealed class KeyHoldEngineTests
{
    private const int W = 0x57;
    private const int Shift = 0x10;
    private const int PageUp = 0x21;
    private const int PageDown = 0x22;
    private const int F12 = 0x7B;

    [TestMethod]
    public void SeparateKeys_CapturesHeldKeysAndSuppressesTheirRelease()
    {
        var sender = new RecordingInputSender();
        var engine = new KeyHoldEngine(new AppSettings(), sender);

        Assert.IsFalse(engine.HandleKeyboardEvent(Down(W)));
        Assert.IsFalse(engine.HandleKeyboardEvent(Down(Shift)));
        Assert.IsTrue(engine.HandleKeyboardEvent(Down(PageUp)));

        CollectionAssert.AreEquivalent(new[] { Shift, W }, sender.DownKeys);
        Assert.IsTrue(engine.Status.IsActive);

        Assert.IsTrue(engine.HandleKeyboardEvent(Up(W)));
        Assert.IsEmpty(sender.UpKeys);
    }

    [TestMethod]
    public void SeparateKeys_StopKeyReleasesHeldKeys()
    {
        var sender = new RecordingInputSender();
        var engine = new KeyHoldEngine(new AppSettings(), sender);

        engine.HandleKeyboardEvent(Down(W));
        engine.HandleKeyboardEvent(Down(PageUp));
        Assert.IsTrue(engine.HandleKeyboardEvent(Down(PageDown)));

        CollectionAssert.Contains(sender.UpKeys, W);
        Assert.IsFalse(engine.Status.IsActive);
    }

    [TestMethod]
    public void ToggleMode_SecondActivationReleasesHeldKeys()
    {
        var sender = new RecordingInputSender();
        var settings = new AppSettings { ActivationMode = ActivationMode.Toggle };
        var engine = new KeyHoldEngine(settings, sender);

        engine.HandleKeyboardEvent(Down(W));
        engine.HandleKeyboardEvent(Down(PageUp));
        Assert.IsTrue(engine.Status.IsActive);

        engine.HandleKeyboardEvent(Down(PageUp));

        CollectionAssert.Contains(sender.UpKeys, W);
        Assert.IsFalse(engine.Status.IsActive);
    }

    [TestMethod]
    public void ToggleMode_CanHoldSeparateModeStopKey()
    {
        var sender = new RecordingInputSender();
        var settings = new AppSettings { ActivationMode = ActivationMode.Toggle };
        var engine = new KeyHoldEngine(settings, sender);

        engine.HandleKeyboardEvent(Down(PageDown));
        engine.HandleKeyboardEvent(Down(PageUp));

        CollectionAssert.Contains(sender.DownKeys, PageDown);
        Assert.IsTrue(engine.Status.IsActive);
    }

    [TestMethod]
    public void MouseTriggerMode_MouseButtonStartsAndStopsHold()
    {
        var sender = new RecordingInputSender();
        var settings = new AppSettings { ActivationMode = ActivationMode.MouseTrigger };
        var engine = new KeyHoldEngine(settings, sender);

        engine.HandleKeyboardEvent(Down(W));
        Assert.IsTrue(engine.HandleMouseEvent(new MouseInputEvent(MouseTriggerCode.XButton1, true, false)));
        Assert.IsTrue(engine.Status.IsActive);

        Assert.IsTrue(engine.HandleMouseEvent(new MouseInputEvent(MouseTriggerCode.XButton1, true, false)));

        CollectionAssert.Contains(sender.UpKeys, W);
        Assert.IsFalse(engine.Status.IsActive);
    }

    [TestMethod]
    public void MouseTriggerMode_CanHoldKeyboardTriggerDefaults()
    {
        var sender = new RecordingInputSender();
        var settings = new AppSettings { ActivationMode = ActivationMode.MouseTrigger };
        var engine = new KeyHoldEngine(settings, sender);

        engine.HandleKeyboardEvent(Down(PageUp));
        engine.HandleKeyboardEvent(Down(PageDown));
        engine.HandleMouseEvent(new MouseInputEvent(MouseTriggerCode.XButton1, true, false));

        CollectionAssert.Contains(sender.DownKeys, PageUp);
        CollectionAssert.Contains(sender.DownKeys, PageDown);
        Assert.IsTrue(engine.Status.IsActive);
    }

    [TestMethod]
    public void EmergencyHotkeyReleasesHeldKeys()
    {
        var sender = new RecordingInputSender();
        var engine = new KeyHoldEngine(new AppSettings(), sender);

        engine.HandleKeyboardEvent(Down(W));
        engine.HandleKeyboardEvent(Down(PageUp));
        Assert.IsTrue(engine.Status.IsActive);

        Assert.IsTrue(engine.HandleKeyboardEvent(Down(F12)));

        CollectionAssert.Contains(sender.UpKeys, W);
        Assert.IsFalse(engine.Status.IsActive);
    }

    private static KeyboardInputEvent Down(int key)
    {
        return new KeyboardInputEvent(key, true, false, false);
    }

    private static KeyboardInputEvent Up(int key)
    {
        return new KeyboardInputEvent(key, false, false, false);
    }

    private sealed class RecordingInputSender : IInputSender
    {
        public List<int> DownKeys { get; } = [];

        public List<int> UpKeys { get; } = [];

        public void SendKeyDown(int virtualKey)
        {
            DownKeys.Add(virtualKey);
        }

        public void SendKeyUp(int virtualKey)
        {
            UpKeys.Add(virtualKey);
        }
    }
}
