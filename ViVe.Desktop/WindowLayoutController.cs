// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ViVe.Desktop;

/// <summary>Saves normal window bounds in screen pixels; works independently of WPF's current DPI.</summary>
public sealed class WindowLayoutController : IDisposable
{
    private readonly Window window;
    private readonly string path;
    private readonly Func<double> tableShare;
    private readonly Action<double> restoreShare;
    private Rect normalBounds;
    private bool lastMaximized;
    private bool restored;
    public WindowLayoutController(Window window,string path,Func<double> tableShare,Action<double> restoreShare)
    {
        this.window=window;this.path=path;this.tableShare=tableShare;this.restoreShare=restoreShare;
        window.SourceInitialized+=Restore;
        window.LocationChanged+=RememberNormal;
        window.SizeChanged+=RememberSize;
        window.StateChanged+=RememberState;
        window.Closed+=Save;
    }
    private void Restore(object? sender,EventArgs e)
    {
        var hwnd=new WindowInteropHelper(window).Handle;
        var saved=WindowLayoutStore.Load(path);
        if(saved!=null) {
            var bounds=WindowLayoutStore.Fit(new(saved.Left,saved.Top,saved.Width,saved.Height),WorkAreas());
            // Avoid a minimum DIP size forcing the restored window beyond a smaller work area.
            var dpi=Math.Max(96,GetDpiForWindow(hwnd))/96d;
            window.MinWidth=Math.Min(window.MinWidth,bounds.Width/dpi);
            window.MinHeight=Math.Min(window.MinHeight,bounds.Height/dpi);
            SetWindowPos(hwnd,IntPtr.Zero,(int)Math.Round(bounds.Left),(int)Math.Round(bounds.Top),(int)Math.Round(bounds.Width),(int)Math.Round(bounds.Height),0x0014);
            normalBounds=bounds;
            restoreShare(Math.Clamp(saved.TableShare,.25,.9));
            lastMaximized=saved.Maximized;
            if(saved.Maximized) window.WindowState=WindowState.Maximized;
        }
        restored=true;
        RememberNormal(null,EventArgs.Empty);
    }
    private void RememberSize(object sender,SizeChangedEventArgs e)=>RememberNormal(sender,e);
    private void RememberState(object? sender,EventArgs e)
    {
        if(window.WindowState!=WindowState.Minimized) lastMaximized=window.WindowState==WindowState.Maximized;
        RememberNormal(sender,e);
    }
    private void RememberNormal(object? sender,EventArgs e)
    {
        if(!restored || window.WindowState!=WindowState.Normal) return;
        var hwnd=new WindowInteropHelper(window).Handle;
        var placement=new Placement {Length=Marshal.SizeOf<Placement>()};
        if(hwnd!=IntPtr.Zero && GetWindowPlacement(hwnd,ref placement) && placement.ShowCommand==1 && GetWindowRect(hwnd,out var r) && r.Right>r.Left && r.Bottom>r.Top)
            normalBounds=new Rect(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top);
    }
    public void ResetToDefaults()
    {
        window.WindowState=WindowState.Normal;
        window.MinWidth=980;window.MinHeight=720;
        restoreShare(2.2/3.2);
        var hwnd=new WindowInteropHelper(window).Handle;
        if(hwnd==IntPtr.Zero) {window.Width=1300;window.Height=800;return;}
        var dpi=Math.Max(96,GetDpiForWindow(hwnd))/96d;
        var areas=WorkAreas();
        GetWindowRect(hwnd,out var current);
        var currentRect=new Rect(current.Left,current.Top,Math.Max(1,current.Right-current.Left),Math.Max(1,current.Bottom-current.Top));
        var fitted=WindowLayoutStore.Fit(currentRect,areas);
        var area=areas.FirstOrDefault(r=>r.Contains(new Point(fitted.Left+fitted.Width/2,fitted.Top+fitted.Height/2)));
        if(area.IsEmpty || area.Width<=0) area=new Rect(0,0,1300*dpi,800*dpi);
        var target=WindowLayoutStore.Fit(new Rect(area.Left+(area.Width-1300*dpi)/2,area.Top+(area.Height-800*dpi)/2,1300*dpi,800*dpi),areas);
        window.MinWidth=Math.Min(980,target.Width/dpi);window.MinHeight=Math.Min(720,target.Height/dpi);
        SetWindowPos(hwnd,IntPtr.Zero,(int)target.Left,(int)target.Top,(int)target.Width,(int)target.Height,0x0014);
        normalBounds=target;lastMaximized=false;
    }
    public void SaveCurrent()
    {
        try {
            RememberNormal(null,EventArgs.Empty);
            if(!normalBounds.IsEmpty && normalBounds.Width>=200 && normalBounds.Height>=200)
                WindowLayoutStore.Save(path,new(2,normalBounds.Left,normalBounds.Top,normalBounds.Width,normalBounds.Height,lastMaximized,Math.Clamp(tableShare(),.25,.9)));
        } catch { }
    }
    private void Save(object? sender,EventArgs e) {SaveCurrent();Dispose();}
    private static List<Rect> WorkAreas()
    {
        var result=new List<Rect>();
        MonitorCallback callback=(IntPtr monitor,IntPtr hdc,ref NativeRect bounds,IntPtr data)=>{
            var info=new MonitorInfo {Size=Marshal.SizeOf<MonitorInfo>()};
            if(GetMonitorInfo(monitor,ref info)) result.Add(new Rect(info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top));
            return true;
        };
        EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,callback,IntPtr.Zero);
        return result;
    }
    public void Dispose()
    {
        window.SourceInitialized-=Restore;window.LocationChanged-=RememberNormal;window.SizeChanged-=RememberSize;
        window.StateChanged-=RememberState;window.Closed-=Save;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect {public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint {public int X,Y;}
    [StructLayout(LayoutKind.Sequential)] private struct Placement {public int Length,Flags,ShowCommand;public NativePoint MinPosition,MaxPosition;public NativeRect Normal;}
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo {public int Size;public NativeRect Monitor,Work;public int Flags;}
    private delegate bool MonitorCallback(IntPtr monitor,IntPtr hdc,ref NativeRect bounds,IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc,IntPtr rect,MonitorCallback callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool GetWindowPlacement(IntPtr hwnd,ref Placement placement);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
}
