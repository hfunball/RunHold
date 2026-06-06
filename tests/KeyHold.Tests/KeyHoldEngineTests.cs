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
        using var engine = CreateEngine(new AppSettings(), sender);

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
        using var engine = CreateEngine(new AppSettings(), sender);

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
        using var engine = CreateEngine(settings, sender);

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
        using var engine = CreateEngine(settings, sender);

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
        using var engine = CreateEngine(settings, sender);

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
        using var engine = CreateEngine(settings, sender);

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
        using var engine = CreateEngine(new AppSettings(), sender);

        engine.HandleKeyboardEvent(Down(W));
        engine.HandleKeyboardEvent(Down(PageUp));
        Assert.IsTrue(engine.Status.IsActive);

        Assert.IsTrue(engine.HandleKeyboardEvent(Down(F12)));

        CollectionAssert.Contains(sender.UpKeys, W);
        Assert.IsFalse(engine.Status.IsActive);
    }

    [TestMethod]
    public void ActiveHold_RepeatsHeldKeysUntilStop()
    {
        var sender = new RecordingInputSender();
        using var engine = new KeyHoldEngine(new AppSettings(), sender, TimeSpan.FromMilliseconds(10));

        engine.HandleKeyboardEvent(Down(W));
        engine.HandleKeyboardEvent(Down(PageUp));

        Assert.IsTrue(WaitUntil(() => sender.DownCount(W) >= 3));

        Assert.IsTrue(engine.HandleKeyboardEvent(Down(W)));
        Assert.IsTrue(engine.HandleKeyboardEvent(Down(PageDown)));

        var downCountAfterStop = sender.DownCount(W);
        Thread.Sleep(40);

        Assert.AreEqual(downCountAfterStop, sender.DownCount(W));
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

    private static KeyHoldEngine CreateEngine(AppSettings settings, RecordingInputSender sender)
    {
        return new KeyHoldEngine(settings, sender, TimeSpan.FromSeconds(10));
    }

    private static bool WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(1);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    private sealed class RecordingInputSender : IInputSender
    {
        private readonly object gate = new();
        private readonly List<int> downKeys = [];
        private readonly List<int> upKeys = [];

        public int[] DownKeys
        {
            get
            {
                lock (gate)
                {
                    return [.. downKeys];
                }
            }
        }

        public int[] UpKeys
        {
            get
            {
                lock (gate)
                {
                    return [.. upKeys];
                }
            }
        }

        public void SendKeyDown(int virtualKey)
        {
            lock (gate)
            {
                downKeys.Add(virtualKey);
            }
        }

        public void SendKeyUp(int virtualKey)
        {
            lock (gate)
            {
                upKeys.Add(virtualKey);
            }
        }

        public int DownCount(int virtualKey)
        {
            lock (gate)
            {
                return downKeys.Count(key => key == virtualKey);
            }
        }
    }
}
