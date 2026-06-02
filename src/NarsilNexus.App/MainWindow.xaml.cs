using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace NarsilNexus.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyDarkTitleBar();
    }

    private void ApplyDarkTitleBar()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) is false)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        var useDarkMode = 1;

        _ = DwmSetWindowAttribute(handle, 20, ref useDarkMode, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 19, ref useDarkMode, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}
