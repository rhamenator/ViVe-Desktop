using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Albacore.ViVe;
using Albacore.ViVe.NativeEnums;
using Albacore.ViVe.NativeStructs;
using Microsoft.Win32;

namespace ViVe.Desktop;

public record FeatureRow(uint Id, string Name, string State, string Priority, uint Variant, uint Payload);
public partial class MainWindow : Window
{
    private List<FeatureRow> rows = new();
    private readonly Dictionary<uint,string> names = new();
    private bool busy;
    private bool Admin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    private RTL_FEATURE_CONFIGURATION_TYPE CurrentStore => Store.SelectedIndex == 1 ? RTL_FEATURE_CONFIGURATION_TYPE.Boot : RTL_FEATURE_CONFIGURATION_TYPE.Runtime;
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            foreach(var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory,"FeatureDictionary.pfs")))
            {
                var split = line.LastIndexOf(',');
                if(split > 0 && uint.TryParse(line[(split+1)..], out var id)) names[id]=line[..split];
            }
        }
        catch(Exception ex) { Write("Dictionary unavailable: "+ex.Message); }
        EnableButton.IsEnabled = DisableButton.IsEnabled = ResetButton.IsEnabled = Admin;
        Write(Admin ? "Administrator access active." : "Read-only session. Use Run as administrator to change features.");
        Loaded += async (_,_) => await LoadFeatures();
    }
    private FeatureRow Row(RTL_FEATURE_CONFIGURATION c) => new(c.FeatureId, names.GetValueOrDefault(c.FeatureId,"Unknown"),c.EnabledState.ToString(),c.Priority.ToString(),c.Variant,c.VariantPayload);
    private void Write(string message) { Log.AppendText($"[{DateTime.Now:T}] {message}{Environment.NewLine}"); Log.ScrollToEnd(); }
    private async Task LoadFeatures()
    {
        if(busy) return;
        busy=true; IsEnabled=false;
        var store=CurrentStore;
        try {
            var configs=await Task.Run(()=>FeatureManager.QueryAllFeatureConfigurations(store));
            if(configs == null) { rows.Clear(); Write("No configurations returned. The store may be empty or unavailable."); }
            else { rows=configs.Select(Row).OrderBy(r=>r.Id).ToList(); Write($"Loaded {rows.Count} configurations from {store}."); }
            ApplyFilter();
        } catch(Exception ex) { Write("Query failed: "+ex.Message); }
        finally {busy=false; IsEnabled=true;}
    }
    private void ApplyFilter()
    {
        if(Features == null || Search == null) return;
        var q=Search.Text.Trim();
        var filtered=rows.Where(r=>r.Id.ToString().Contains(q) || r.Name.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();
        Features.ItemsSource=filtered;
        Summary.Text=$"{filtered.Count:N0} shown / {rows.Count:N0} configurations · {CurrentStore}";
    }
    private void Filter(object s, TextChangedEventArgs e)=>ApplyFilter();
    private void Selected(object s,SelectionChangedEventArgs e) { if(Features.SelectedItem is FeatureRow row) FeatureId.Text=row.Id.ToString(); }
    private async void Refresh(object s,RoutedEventArgs e)=>await LoadFeatures();
    private async void StoreChanged(object s,SelectionChangedEventArgs e) { if(IsLoaded) await LoadFeatures(); }
    private uint ReadId()
    {
        if(!uint.TryParse(FeatureId.Text.Trim(),out var id) || id==0) throw new ArgumentException("Enter a positive 32-bit feature ID.");
        return id;
    }
    private void Query(object s,RoutedEventArgs e)
    {
        try {
            var id=ReadId(); var config=FeatureManager.QueryFeatureConfiguration(id,CurrentStore);
            Write(config is {} c ? JsonSerializer.Serialize(Row(c)) : $"Feature {id}: no configuration returned (absent or query failed).");
        } catch(Exception ex) {Write(ex.Message);}
    }
    private async void EnableFeature(object s,RoutedEventArgs e)=>await Change(RTL_FEATURE_ENABLED_STATE.Enabled,false);
    private async void DisableFeature(object s,RoutedEventArgs e)=>await Change(RTL_FEATURE_ENABLED_STATE.Disabled,false);
    private async void ResetFeature(object s,RoutedEventArgs e)=>await Change(RTL_FEATURE_ENABLED_STATE.Default,true);
    private async Task Change(RTL_FEATURE_ENABLED_STATE state,bool reset)
    {
        try {
            if(!Admin) throw new InvalidOperationException("Administrator access is required.");
            var id=ReadId(); var store=CurrentStore;
            var action=reset ? "Remove the User-priority override for" : $"Set {state} for";
            if(MessageBox.Show(this,$"{action} feature {id} in the {store} store?\n\nOther priorities are preserved. A restart may be needed.","Confirm feature change",MessageBoxButton.OKCancel,MessageBoxImage.Warning)!=MessageBoxResult.OK) return;
            // Persist a pre-change diagnostic snapshot before calling any write API.
            var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ViVeDesktop","Snapshots");
            Directory.CreateDirectory(folder);
            var path=Path.Combine(folder,$"{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}-{store}.json");
            SaveSnapshot(path);
            var update=new RTL_FEATURE_CONFIGURATION_UPDATE {FeatureId=id,Priority=RTL_FEATURE_CONFIGURATION_PRIORITY.User,EnabledState=state,Operation=reset ? RTL_FEATURE_CONFIGURATION_OPERATION.ResetState : RTL_FEATURE_CONFIGURATION_OPERATION.FeatureState};
            var result=FeatureManager.SetFeatureConfigurations(new[]{update},store);
            if(result!=0) throw new InvalidOperationException($"Feature change failed: 0x{result:X8}. Snapshot: {path}");
            Write($"Changed {id} in {store}. Pre-change snapshot: {path}");
            if(store==RTL_FEATURE_CONFIGURATION_TYPE.Boot) {
                var status=FeatureManager.GetBootFeatureConfigurationState(out var current);
                if(unchecked((uint)status)==0xC0000034) status=FeatureManager.InitializeBootStatusDataFile();
                if(status==0) status=FeatureManager.SetBootFeatureConfigurationState(BSD_FEATURE_CONFIGURATION_STATE.BootPending);
                if(status!=0) Write($"Feature override saved, but boot status update failed: 0x{status:X8}.");
            }
            await LoadFeatures();
        } catch(Exception ex) { Write(ex.Message); MessageBox.Show(this,ex.Message,"Unable to complete change",MessageBoxButton.OK,MessageBoxImage.Error); }
    }
    private void SaveSnapshot(string path)
    {
        var configs=FeatureManager.QueryAllFeatureConfigurations(CurrentStore) ?? throw new InvalidOperationException("Cannot capture a snapshot: store query returned no data.");
        File.WriteAllText(path,JsonSerializer.Serialize(new {FormatVersion=1,CreatedUtc=DateTime.UtcNow,WindowsBuild=Environment.OSVersion.Version.ToString(),Store=CurrentStore.ToString(),Configurations=configs.Select(c=>new {c.FeatureId,c.CompactState,c.VariantPayload})},new JsonSerializerOptions {WriteIndented=true}));
    }
    private void Export(object s,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog {Filter="JSON snapshot|*.json",FileName=$"ViVe-{CurrentStore}-{DateTime.Now:yyyyMMdd-HHmmss}.json"};
        if(dialog.ShowDialog(this)!=true) return;
        try {SaveSnapshot(dialog.FileName); Write("Exported "+dialog.FileName);} catch(Exception ex) {Write("Export failed: "+ex.Message);}
    }
    private void Elevate(object s,RoutedEventArgs e)
    {
        if(Admin) {Write("Already running as administrator."); return;}
        try {Process.Start(new ProcessStartInfo(Environment.ProcessPath!) {UseShellExecute=true,Verb="runas"}); Close();}
        catch(Exception ex) {Write("Administrator launch canceled or failed: "+ex.Message);}
    }
}
