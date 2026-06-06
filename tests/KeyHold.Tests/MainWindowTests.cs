using System.IO;
using System.Windows;
using System.Windows.Controls;
using KeyHold.Models;
using KeyHold.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeyHold.Tests;

[TestClass]
public sealed class MainWindowTests
{
    private const int A = 0x41;

    [STATestMethod]
    public void ToggleMode_ShowsOneKeyboardTrigger()
    {
        var window = CreateWindow(new AppSettings { ActivationMode = ActivationMode.Toggle });
        try
        {
            Assert.AreEqual(Visibility.Visible, Find<FrameworkElement>(window, "EnableBindingPanel").Visibility);
            Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(window, "StopBindingPanel").Visibility);
            Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(window, "MouseBindingPanel").Visibility);
            Assert.AreEqual("Toggle key", Find<TextBlock>(window, "EnableBindingLabel").Text);
            Assert.AreEqual("Set Toggle Key", Find<Button>(window, "CaptureEnableButton").Content);
        }
        finally
        {
            Close(window);
        }
    }

    [STATestMethod]
    public void MouseTriggerMode_ShowsMouseTriggerInsteadOfKeyboardTriggers()
    {
        var window = CreateWindow(new AppSettings { ActivationMode = ActivationMode.MouseTrigger });
        try
        {
            Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(window, "EnableBindingPanel").Visibility);
            Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(window, "StopBindingPanel").Visibility);
            Assert.AreEqual(Visibility.Visible, Find<FrameworkElement>(window, "MouseBindingPanel").Visibility);
            Assert.AreEqual(Visibility.Visible, Find<FrameworkElement>(window, "EmergencyBindingPanel").Visibility);
        }
        finally
        {
            Close(window);
        }
    }

    [STATestMethod]
    public void CaptureButtonShowsPromptAndStoresKey()
    {
        var window = CreateWindow(new AppSettings { ActivationMode = ActivationMode.Toggle });
        try
        {
            var button = Find<Button>(window, "CaptureEnableButton");

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.AreEqual("Press a key...", Find<TextBox>(window, "EnableBindingText").Text);
            Assert.AreEqual("Listening...", button.Content);

            window.CompleteKeyCaptureForTest(A);

            Assert.AreEqual("A", Find<TextBox>(window, "EnableBindingText").Text);
            Assert.AreEqual("Set Toggle Key", button.Content);
        }
        finally
        {
            Close(window);
        }
    }

    private static MainWindow CreateWindow(AppSettings settings)
    {
        var path = Path.Combine(Path.GetTempPath(), "KeyHold.Tests", $"{Guid.NewGuid():N}.json");
        var engine = new KeyHoldEngine(settings, new RecordingInputSender());
        return new MainWindow(settings, new ConfigService(path), engine, new FakeStartupService());
    }

    private static T Find<T>(MainWindow window, string name)
        where T : FrameworkElement
    {
        var element = window.FindName(name);
        Assert.IsNotNull(element, $"Expected {name} to exist.");
        return (T)element;
    }

    private static void Close(MainWindow window)
    {
        window.AllowClose();
        window.Close();
    }

    private sealed class FakeStartupService : IStartupService
    {
        public bool Enabled { get; private set; }

        public bool IsEnabled()
        {
            return Enabled;
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }
    }

    private sealed class RecordingInputSender : IInputSender
    {
        public void SendKeyDown(int virtualKey)
        {
        }

        public void SendKeyUp(int virtualKey)
        {
        }
    }
}
