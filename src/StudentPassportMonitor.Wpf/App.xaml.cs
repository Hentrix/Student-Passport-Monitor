using System.Text;
using System.Windows;

namespace StudentPassportMonitor.Wpf;

public partial class App : Application
{
    public App()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ThemeManager.Initialize();
    }
}
