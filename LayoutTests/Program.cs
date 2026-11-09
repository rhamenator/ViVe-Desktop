#pragma warning disable WPF0001
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViVe.Desktop;
internal class Probe {
 [STAThread] static void Main() {
  var app=new Application {ThemeMode=ThemeMode.System,ShutdownMode=ShutdownMode.OnExplicitShutdown};
  var buttons=new Style(typeof(Button));buttons.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(16,9,16,9)));buttons.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(0,0,8,0)));app.Resources.Add(typeof(Button),buttons);
  var textboxes=new Style(typeof(TextBox));textboxes.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(10,8,10,8)));app.Resources.Add(typeof(TextBox),textboxes);
  var window=new MainWindow(null);
  var content=(FrameworkElement)window.Content;
  double previousTable=0,previousSearch=0;
  foreach(var (width,height) in new[]{(980d,720d),(1300d,800d),(1800d,1100d)}) {
   content.Measure(new Size(width-16,height-40));
   content.Arrange(new Rect(0,0,width-16,height-40));
   content.UpdateLayout();
   var search=(FrameworkElement)window.FindName("Search");
   var table=(FrameworkElement)window.FindName("Features");
   var parent=VisualTreeHelper.GetParent(search);
   var panel=(AdaptiveFilterPanel)VisualTreeHelper.GetParent(parent);
   var controls=panel.Children.Cast<FrameworkElement>().ToArray();
   var rectangles=controls.Select(c=>new Rect(c.TransformToAncestor(panel).Transform(new Point()),c.RenderSize)).ToArray();
   for(int i=0;i<rectangles.Length;i++) {
    var rect=rectangles[i];
    if(rect.Right>panel.ActualWidth+.1 || rect.X<-.1) throw new Exception("Filter clipped horizontally.");
    for(int j=i+1;j<rectangles.Length;j++) {
     var overlap=Rect.Intersect(rect,rectangles[j]);
     if(!overlap.IsEmpty && overlap.Width>.1 && overlap.Height>.1) throw new Exception("Filter controls overlap.");
    }
   }
   if(table.ActualHeight<160) throw new Exception("Table lost its minimum height.");
   if(table.ActualHeight<=previousTable || search.ActualWidth<previousSearch) throw new Exception("Layout does not grow with window.");
   if(width==1300 && rectangles.Select(r=>Math.Round(r.Bottom,1)).Distinct().Count()!=1) throw new Exception("Default filter bar should fit on one line.");
   previousTable=table.ActualHeight;previousSearch=search.ActualWidth;
   Console.WriteLine($"PASS {width}x{height}: filter lines={rectangles.Select(r=>Math.Round(r.Bottom,1)).Distinct().Count()}, search={search.ActualWidth:F0}px, table={table.ActualHeight:F0}px");
  }
  window.Close();
  var temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ViVeLayout-"+Guid.NewGuid(),"layout.json");
  try {
   var saved=new SavedLayout(1,100,80,1120,740,true,.65);
   WindowLayoutStore.Save(temp,saved);
   if(WindowLayoutStore.Load(temp)!=saved) throw new Exception("Settings round trip failed.");
   var fit=WindowLayoutStore.Fit(new Rect(5000,4000,1600,1000),new[]{new Rect(0,0,1280,800)});
   if(fit!=new Rect(0,0,1280,800)) throw new Exception("Disconnected monitor recovery failed.");
   var secondMonitor=new Rect(-1920,0,1920,1080);
   var positioned=new Rect(-1700,100,1200,800);
   if(WindowLayoutStore.Fit(positioned,new[]{new Rect(0,0,1920,1080),secondMonitor})!=positioned) throw new Exception("Valid secondary monitor position changed.");
   System.IO.File.WriteAllText(temp,"broken json");
   if(WindowLayoutStore.Load(temp)!=null) throw new Exception("Corrupt settings were accepted.");
   WindowLayoutStore.Save(temp,saved with {Maximized=false,TableShare=.62});
   double restoredShare=0;
   var first=new Window {Width=1300,Height=800};
   using(var controller=new WindowLayoutController(first,temp,()=>.62,share=>restoredShare=share)) {
    first.Show();first.UpdateLayout();
    first.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    if(Math.Abs(restoredShare-.62)>.001) throw new Exception("Divider was not restored.");
    first.Width=1140; first.Height=760; first.Left=120;first.Top=90;first.UpdateLayout();
    first.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    first.Close();
   }
   var before=WindowLayoutStore.Load(temp) ?? throw new Exception("Window did not save layout.");
   if(Math.Abs(before.Width-1140)>2 || Math.Abs(before.Height-760)>2) throw new Exception($"Wrong saved window size: {before}");
   var next=new Window {Width=900,Height=700};
   using(var controller=new WindowLayoutController(next,temp,()=>.62,share=>restoredShare=share)) {
    next.Show();next.UpdateLayout();
    next.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    next.WindowState=WindowState.Maximized;
    next.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    next.Close();
   }
   var after=WindowLayoutStore.Load(temp) ?? throw new Exception("Restored window did not save layout.");
   if(!after.Maximized || Math.Abs(after.Width-before.Width)>2 || Math.Abs(after.Left-before.Left)>2 || Math.Abs(after.Top-before.Top)>2 || Math.Abs(after.TableShare-.62)>.001) throw new Exception($"Normal bounds or maximized state lost: {after}");
   var maximized=new Window {Width=900,Height=700};
   using(var controller=new WindowLayoutController(maximized,temp,()=>.62,share=>restoredShare=share)) {
    maximized.Show();maximized.UpdateLayout();
    maximized.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    if(maximized.WindowState!=WindowState.Maximized) throw new Exception("Maximized state was not restored.");
    maximized.WindowState=WindowState.Minimized;
    maximized.Close();
   }
   if(WindowLayoutStore.Load(temp)?.Maximized!=true) throw new Exception("Closing minimized lost last maximized state.");
   var prefsPath=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(temp),"view-preferences.json");
   var prefs=new ViewPreferences(1,"menu",1,1,"Disabled","User","0",true,
       new(){new("Id",155,DataGridLengthUnitType.Pixel),new("Name",2.4,DataGridLengthUnitType.Star)},
       new(){new("EffectiveState",System.ComponentModel.ListSortDirection.Ascending),new("Id",System.ComponentModel.ListSortDirection.Descending)});
   ViewPreferencesStore.Save(prefsPath,prefs);
   var preferences=WindowLayoutStore.Load(temp);
   var configured=new MainWindow(temp);
   var searchBox=(TextBox)configured.FindName("Search");
   var data=(DataGrid)configured.FindName("Features");
   if(searchBox.Text!="menu" || ((ComboBox)configured.FindName("Store")).SelectedIndex!=1 ||
      ((ComboBoxItem)((ComboBox)configured.FindName("StateFilter")).SelectedItem).Content.ToString()!="Disabled" ||
      ((ComboBox)configured.FindName("PriorityFilter")).SelectedItem.ToString()!="User" ||
      ((TextBox)configured.FindName("VariantFilter")).Text!="0" ||
      ((CheckBox)configured.FindName("ShowPriorityDetails")).IsChecked!=true) throw new Exception("Filters were not restored.");
   if(data.Columns.First(c=>c.SortMemberPath=="Id").Width.Value!=155 ||
      data.Columns.First(c=>c.SortMemberPath=="Name").Width.UnitType!=DataGridLengthUnitType.Star) throw new Exception("Column widths were not restored.");
   data.ItemsSource=new List<FeatureRow> {
       new(10,"Test","Enabled","User",0,0,"Enabled","User"),
       new(20,"Test","Enabled","User",0,0,"Enabled","User"),
       new(30,"Test","Disabled","User",0,0,"Disabled","User")};
   ViewPreferencesStore.ApplySorts(data,prefs.Sorts);
   if(((FeatureRow)data.Items[0]).Id!=30 || ((FeatureRow)data.Items[1]).Id!=20) throw new Exception("Multicolumn sort was not applied.");
   searchBox.Text="context";
   if(!ViewPreferencesStore.ReadSorts(data).SequenceEqual(prefs.Sorts)) throw new Exception("Filter reset the sort order.");
   configured.Close();
   var roundtrip=ViewPreferencesStore.Load(prefsPath) ?? throw new Exception("View preferences were not saved.");
   if(roundtrip.Search!="context" || roundtrip.Store!=1 || !roundtrip.Sorts.SequenceEqual(prefs.Sorts) || !roundtrip.Columns.Any(c=>c.Key=="Id" && c.Width==155)) throw new Exception("View preferences did not survive close.");
   System.IO.File.WriteAllText(prefsPath,"bad json");
   if(ViewPreferencesStore.Load(prefsPath)!=null) throw new Exception("Corrupt view settings were accepted.");
   System.IO.File.Delete(prefsPath);
   Console.WriteLine("PASS: Restored filters, pixel/star widths, multicolumn sorting, sort preserved across filtering, close/save and corrupt preference fallback.");
   Console.WriteLine("PASS: JSON round trip, corrupt file fallback, monitor recovery, actual window close/reopen, divider restore and maximized normal-bounds preservation.");
  } finally {
   var preferenceFile=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(temp),"view-preferences.json"); if(System.IO.File.Exists(preferenceFile)) System.IO.File.Delete(preferenceFile);
   if(System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
   if(System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(temp))) System.IO.Directory.Delete(System.IO.Path.GetDirectoryName(temp));
  }
  app.Shutdown();
 }
}
