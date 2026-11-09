using System.Security.Principal;
using System.Windows;
namespace ViVe.Desktop;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
            MessageBox.Show("ViVe Desktop requires administrator access. Start ViVe.Desktop.exe and accept the Windows elevation prompt.","Administrator access required",MessageBoxButton.OK,MessageBoxImage.Exclamation);
            Shutdown(1);
            return;
        }
        base.OnStartup(e);
    }
}
