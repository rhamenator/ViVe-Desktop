using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows;

namespace ViVe.Desktop;
public partial class App : Application
{
    private Mutex? instanceMutex;
    private bool ownsMutex;
    private EventWaitHandle? activationEvent;
    private RegisteredWaitHandle? activationWait;
    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(int processId);

    protected override void OnStartup(StartupEventArgs e)
    {
        if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
            MessageBox.Show("ViVe Desktop requires administrator access. Start ViVe.Desktop.exe and accept the Windows elevation prompt.","Administrator access required",MessageBoxButton.OK,MessageBoxImage.Exclamation);
            Shutdown(1); return;
        }
        var user=WindowsIdentity.GetCurrent().User!.Value;
        var prefix=@"Local\ViVeDesktop-"+user;
        instanceMutex=new Mutex(true,prefix+"-instance",out ownsMutex);
        if(!ownsMutex) {
            // Give only our existing process permission to bring its window forward.
            foreach(var process in Process.GetProcessesByName("ViVe.Desktop")) {
                try {
                    if(process.Id!=Environment.ProcessId && process.SessionId==Process.GetCurrentProcess().SessionId)
                        AllowSetForegroundWindow(process.Id);
                } catch { }
                finally {process.Dispose();}
            }
            for(int attempt=0;attempt<20;attempt++) {
                if(EventWaitHandle.TryOpenExisting(prefix+"-activate",out var existing)) {
                    using(existing) existing.Set();
                    break;
                }
                Thread.Sleep(50);
            }
            Shutdown(); return;
        }
        activationEvent=new EventWaitHandle(false,EventResetMode.AutoReset,prefix+"-activate");
        activationWait=ThreadPool.RegisterWaitForSingleObject(activationEvent,(_,_)=>Dispatcher.BeginInvoke(new Action(()=>{
            var window=MainWindow;
            if(window==null) return;
            if(window.WindowState==WindowState.Minimized) window.WindowState=WindowState.Normal;
            window.Show(); window.Activate();
        })),null,Timeout.Infinite,false);
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        activationWait?.Unregister(null);
        activationEvent?.Dispose();
        if(ownsMutex) instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
