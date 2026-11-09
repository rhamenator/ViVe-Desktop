using System.Diagnostics;
using System.Reflection;
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
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd,int command);
    [DllImport("advapi32.dll",SetLastError=true)] private static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);

    private static bool SameUser(Process process,string sid)
    {
        IntPtr token=IntPtr.Zero;
        try {
            if(!OpenProcessToken(process.Handle,8,out token)) return false;
            using var identity=new WindowsIdentity(token);
            return identity.User?.Value==sid;
        } catch {return false;}
        finally {if(token!=IntPtr.Zero) CloseHandle(token);}
    }
    private static Version? ProductVersion(Process process)
    {
        try {
            var info=process.MainModule?.FileVersionInfo;
            return info?.ProductName=="ViVe.Desktop" && Version.TryParse(info.FileVersion,out var version) ? version : null;
        } catch {return null;}
    }
    private bool Acquire()
    {
        try {return instanceMutex!.WaitOne(0);}
        catch(AbandonedMutexException) {return true;}
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
            MessageBox.Show("ViVe Desktop requires administrator access. Start ViVe.Desktop.exe and accept the Windows elevation prompt.","Administrator access required",MessageBoxButton.OK,MessageBoxImage.Exclamation);
            Shutdown(1); return;
        }
        var sid=WindowsIdentity.GetCurrent().User!.Value;
        var prefix=@"Local\ViVeDesktop-"+sid;
        instanceMutex=new Mutex(false,prefix+"-instance");
        ownsMutex=Acquire();
        if(!ownsMutex) {
            var version=Assembly.GetExecutingAssembly().GetName().Version!;
            var existing=new List<Process>();
            foreach(var process in Process.GetProcessesByName("ViVe.Desktop")) {
                if(process.Id!=Environment.ProcessId && process.SessionId==Process.GetCurrentProcess().SessionId && SameUser(process,sid) && ProductVersion(process)!=null)
                    existing.Add(process);
                else process.Dispose();
            }
            try {
                var newer=existing.Count>0 && existing.All(p=>ProductVersion(p) is {} old && version>old);
                if(newer) {
                    // Normal close runs the old window's layout/preferences save handlers.
                    var requested=existing.All(p=>p.CloseMainWindow());
                    if(requested) {
                        try {ownsMutex=instanceMutex.WaitOne(TimeSpan.FromSeconds(10));}
                        catch(AbandonedMutexException) {ownsMutex=true;}
                    }
                    if(!ownsMutex) {
                        MessageBox.Show("The older ViVe Desktop window is busy or could not close. Finish its current operation, close it, and try this version again. It has not been force-closed.","Update could not replace running version",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                    }
                }
                if(!ownsMutex) {
                    foreach(var process in existing) {
                        try {AllowSetForegroundWindow(process.Id);} catch { }
                    }
                    for(int attempt=0;attempt<20;attempt++) {
                        if(EventWaitHandle.TryOpenExisting(prefix+"-activate",out var signal)) {
                            using(signal) signal.Set();
                            break;
                        }
                        Thread.Sleep(50);
                    }
                    Shutdown(); return;
                }
            }
            finally {foreach(var process in existing) process.Dispose();}
        }
        activationEvent=new EventWaitHandle(false,EventResetMode.AutoReset,prefix+"-activate");
        activationWait=ThreadPool.RegisterWaitForSingleObject(activationEvent,(_,_)=>Dispatcher.BeginInvoke(new Action(()=>{
            var window=MainWindow;
            if(window==null) return;
            if(window.WindowState==WindowState.Minimized) ShowWindow(new System.Windows.Interop.WindowInteropHelper(window).Handle,9);
            window.Show();window.Activate();
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
