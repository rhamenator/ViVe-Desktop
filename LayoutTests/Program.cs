using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViVe.Desktop;
internal class Probe {
 [STAThread] static void Main() {
  var app=new App();app.InitializeComponent();
  var window=new MainWindow();
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
  window.Close();app.Shutdown();
 }
}
