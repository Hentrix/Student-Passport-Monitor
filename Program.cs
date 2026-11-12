using System.Text;
using StudentPassportMonitor.Forms;

namespace StudentPassportMonitor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.Run(new MainForm());
    }
}