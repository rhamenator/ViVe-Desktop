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


public partial class MainWindow : Window
{
    private List<FeatureRow> rows = new();
    private readonly Dictionary<uint,string> names = new();
    private Dictionary<uint,EffectiveConfiguration> effective = new();
    private bool busy;
    private bool restoringPreferences;
    private string? viewPreferencesPath;
    private List<SortPreference> activeSorts=new();
    private bool Admin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    private RTL_FEATURE_CONFIGURATION_TYPE CurrentStore => Store.SelectedIndex == 1 ? RTL_FEATURE_CONFIGURATION_TYPE.Boot : RTL_FEATURE_CONFIGURATION_TYPE.Runtime;
    private readonly WindowLayoutController? layoutController;
    public MainWindow() : this(WindowLayoutStore.DefaultPath) { }
    public MainWindow(string? layoutPath)
    {
        InitializeComponent();
        if(layoutPath!=null) layoutController=new WindowLayoutController(this,layoutPath,
            ()=>TableRegion.ActualHeight+LogRegion.ActualHeight>0 ? TableRegion.ActualHeight/(TableRegion.ActualHeight+LogRegion.ActualHeight) : .75,
            share=>{TableRegion.Height=new GridLength(share,GridUnitType.Star);LogRegion.Height=new GridLength(1-share,GridUnitType.Star);});
        var previousLayout=layoutPath==null ? null : WindowLayoutStore.Load(layoutPath);
        if(previousLayout?.Version==1) Loaded+=(_,_)=>{
            var total=TableRegion.ActualHeight+LogRegion.ActualHeight;
            if(total>0) {
                var logHeight=Math.Max(LogRegion.ActualHeight,total*(1-previousLayout.TableShare)+36);
                var share=Math.Clamp((total-logHeight)/total,.25,.9);
                TableRegion.Height=new GridLength(share,GridUnitType.Star);
                LogRegion.Height=new GridLength(1-share,GridUnitType.Star);
            }
        };
        PriorityFilter.ItemsSource=new[]{"All priorities"}.Concat(Enum.GetNames<RTL_FEATURE_CONFIGURATION_PRIORITY>()).ToArray();
        PriorityFilter.SelectedIndex=0;
        viewPreferencesPath=layoutPath==null ? null : Path.Combine(Path.GetDirectoryName(layoutPath)!,"view-preferences.json");
        RestoreViewPreferences();
        Features.Sorting+=(_,_)=>Dispatcher.BeginInvoke(new Action(()=>activeSorts=ViewPreferencesStore.ReadSorts(Features)),System.Windows.Threading.DispatcherPriority.DataBind);
        Closed+=(_,_)=>SaveViewPreferences();
        Closing+=(_,e)=>{if(busy) {e.Cancel=true;Write("Finish the current operation before closing or updating the app.");}};
        try {
            foreach(var pair in DictionaryUpdater.Read(Path.Combine(AppContext.BaseDirectory,"FeatureDictionary.pfs"))) names[pair.Key]=pair.Value;
            if(File.Exists(DictionaryUpdater.CachePath)) {
                foreach(var pair in DictionaryUpdater.Read(DictionaryUpdater.CachePath)) names[pair.Key]=pair.Value;
                DictionaryStatus.Text=$"Dictionary: {names.Count:N0} names · saved update from {File.GetLastWriteTime(DictionaryUpdater.CachePath):g}.";
            } else DictionaryStatus.Text=$"Dictionary: {names.Count:N0} bundled names · Update Dictionary connects to GitHub on request.";
        } catch(Exception ex) { Write("Dictionary unavailable: "+ex.Message); }
        EnableButton.IsEnabled = DisableButton.IsEnabled = ResetButton.IsEnabled = Admin;
        Write("Administrator access active. Changes are saved to both Runtime and Boot. Restart Windows after changing features.");
        Loaded += async (_,_) => await LoadFeatures();
    }
    private void RestoreViewPreferences()
    {
        if(viewPreferencesPath==null) return;
        var saved=ViewPreferencesStore.Load(viewPreferencesPath);
        if(saved==null) return;
        restoringPreferences=true;
        try {
            Store.SelectedIndex=saved.Store;
            StateScope.SelectedIndex=saved.StateScope;
            foreach(var item in StateFilter.Items.OfType<ComboBoxItem>())
                if((string)item.Content==saved.State) {StateFilter.SelectedItem=item;break;}
            if(PriorityFilter.Items.Contains(saved.Priority)) PriorityFilter.SelectedItem=saved.Priority;
            Search.Text=saved.Search;
            VariantFilter.Text=saved.Variant;
            ShowPriorityDetails.IsChecked=saved.Details;
            activeSorts=saved.Sorts;
            foreach(var preference in saved.Columns) {
                var column=Features.Columns.FirstOrDefault(c=>c.SortMemberPath==preference.Key);
                if(column!=null) column.Width=new DataGridLength(preference.Width,preference.Unit);
            }
        } finally {restoringPreferences=false;}
    }
    private void SaveViewPreferences()
    {
        if(viewPreferencesPath==null) return;
        try {
            // Capture the live view as well, including a header click immediately before closing.
            activeSorts=ViewPreferencesStore.ReadSorts(Features);
            var columns=Features.Columns.Where(c=>ViewPreferencesStore.Keys.Contains(c.SortMemberPath))
                .Select(c=>new ColumnPreference(c.SortMemberPath,c.Width.Value,c.Width.UnitType)).ToList();
            ViewPreferencesStore.Save(viewPreferencesPath,new(1,Search.Text,Store.SelectedIndex,StateScope.SelectedIndex,
                SelectedFilter(StateFilter,"All states"),SelectedFilter(PriorityFilter,"All priorities"),VariantFilter.Text,
                ShowPriorityDetails.IsChecked==true,columns,activeSorts));
        } catch { }
    }
    private FeatureRow Row(RTL_FEATURE_CONFIGURATION c) => EffectiveStates.Apply(new(c.FeatureId, names.GetValueOrDefault(c.FeatureId,"Name unavailable"),c.EnabledState.ToString(),c.Priority.ToString(),c.Variant,c.VariantPayload),effective.GetValueOrDefault(c.FeatureId,new("Unknown","Unavailable",null,"Not resolved by Windows.")));
    private void Write(string message) { Log.AppendText($"[{DateTime.Now:T}] {message}{Environment.NewLine}"); Log.ScrollToEnd(); }
    private async Task LoadFeatures()
    {
        if(busy) return;
        busy=true; IsEnabled=false;
        var store=CurrentStore;
        try {
                        var snapshot=await Task.Run(()=> {
                var configs=FeatureManager.QueryAllFeatureConfigurations(store);
                var resolved=EffectiveStates.QueryAll(configs?.Select(c=>c.FeatureId) ?? Enumerable.Empty<uint>(),store);
                return (Configs:configs,Effective:resolved);
            });
            var configs=snapshot.Configs;
            effective=snapshot.Effective;
            if(configs == null) { rows.Clear(); Write("No configurations returned. The store may be empty or unavailable."); }
            else { rows=configs.Select(Row).OrderBy(r=>r.Id).ToList(); Write($"Loaded {rows.Count} configurations from {store}. Effective states resolved by Windows for {effective.Count} distinct IDs."); }
            ApplyFilter();
        } catch(Exception ex) { Write("Query failed: "+ex.Message); }
        finally {busy=false; IsEnabled=true;}
    }
    private string SelectedFilter(ComboBox control,string fallback) => control.SelectedItem is ComboBoxItem item ? (string)item.Content : control.SelectedItem as string ?? fallback;
    private void ApplyFilter()
    {
        if(restoringPreferences || Features==null || Search==null || StateFilter==null || PriorityFilter==null || VariantFilter==null || StateScope==null || ShowPriorityDetails==null || RowStateColumn==null || Summary==null || FilterHint==null) return;
        var state=SelectedFilter(StateFilter,"All states");
        var priority=SelectedFilter(PriorityFilter,"All priorities");
        var variant=VariantFilter.Text.Trim();
        var valid=variant.Length==0 || uint.TryParse(variant,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var number) && number<=63;
        var filtered=valid ? rows.Where(r=>FeatureFilter.Matches(r,Search.Text,state,priority,variant,StateScope.SelectedIndex==0)).ToList() : new List<FeatureRow>();
                var details=ShowPriorityDetails.IsChecked==true;
        var visible=FeaturePresentation.VisibleRows(filtered,details);
        Features.ItemsSource=visible;
        ViewPreferencesStore.ApplySorts(Features,activeSorts);
        foreach(var column in new[]{RowStateColumn,RowPriorityColumn,RowVariantColumn,PayloadColumn})
            column.Visibility=details ? Visibility.Visible : Visibility.Collapsed;
        Summary.Text=$"{visible.Count:N0} {(details ? "priority rows" : "features")} shown · {rows.Select(r=>r.Id).Distinct().Count():N0} total features · browsing {CurrentStore}";
        FilterHint.Text=!valid ? "Variant must be a whole number from 0 to 63, or empty for all." :
            details ? "Priority layers shown; Effective state is the resolved result." : "One row per feature · state resolved by Windows.";
    }
    private void Filter(object s, TextChangedEventArgs e)=>ApplyFilter();
    private void FilterChanged(object s,SelectionChangedEventArgs e)=>ApplyFilter();
    private void DetailsChanged(object s,RoutedEventArgs e)=>ApplyFilter();
    private void ClearFilters(object? s,RoutedEventArgs? e)
    {
        Search.Clear(); StateFilter.SelectedIndex=0; PriorityFilter.SelectedIndex=0; VariantFilter.Clear(); StateScope.SelectedIndex=0; ApplyFilter();
    }
    private void SearchKeyDown(object s,System.Windows.Input.KeyEventArgs e)
    {
        if(e.Key==System.Windows.Input.Key.Escape) ClearFilters(null,null);
    }
    private void IdKeyDown(object s,System.Windows.Input.KeyEventArgs e)
    {
        if(e.Key==System.Windows.Input.Key.Enter) {e.Handled=true; Query(s,e);}
    }
    private async void UpdateDictionary(object s,RoutedEventArgs e)
    {
        if(busy) return;
        busy=true; IsEnabled=false; UpdateDictionaryButton.IsEnabled=false;
        DictionaryStatus.Text="Downloading names from GitHub…";
        try {
            var update=await DictionaryUpdater.DownloadAsync(new Dictionary<uint,string>(names));
            DictionaryUpdater.Save(update,DictionaryUpdater.CachePath);
            names.Clear();
            foreach(var pair in update.Names) names[pair.Key]=pair.Value;
            var selected=Features.SelectedItem as FeatureRow;
            rows=rows.Select(r=>r with {Name=names.GetValueOrDefault(r.Id,"Name unavailable")}).ToList();
            ApplyFilter();
            if(selected!=null) Features.SelectedItem=rows.FirstOrDefault(r=>r.Id==selected.Id && r.Priority==selected.Priority);
            DictionaryStatus.Text=$"Dictionary: {names.Count:N0} names · updated {DateTime.Now:g} · {update.Changed:N0} added or changed.";
            Write($"Dictionary updated from {update.SourceFile} at {update.Revision}. {update.Changed:N0} added or changed names. Saved locally; no features changed.");
        } catch(Exception ex) {
            DictionaryStatus.Text="Dictionary update failed. Existing names were kept; you can try again.";
            Write("Dictionary update failed: "+ex.Message);
        }
        finally {busy=false; IsEnabled=true; UpdateDictionaryButton.IsEnabled=true;}
    }
    private void Selected(object s,SelectionChangedEventArgs e) { if(Features.SelectedItem is FeatureRow row) FeatureId.Text=row.Id.ToString(); }
    private async void Refresh(object s,RoutedEventArgs e)=>await LoadFeatures();
    private async void StoreChanged(object s,SelectionChangedEventArgs e) { if(IsLoaded) await LoadFeatures(); }
    private uint ReadId()
    {
        if(!uint.TryParse(FeatureId.Text.Trim(),out var id) || id==0) throw new ArgumentException("Enter a positive 32-bit feature ID.");
        return id;
    }
    private async void Query(object s,RoutedEventArgs e)
    {
        if(busy) return;
        try {
            var id=ReadId();
            var store=CurrentStore;
            busy=true; IsEnabled=false;
            var queryResult=await Task.Run(()=> (Lookup:FeatureLookup.Query(id,store),Effective:EffectiveStates.Query(id,store)));
            var result=queryResult.Lookup;
            effective[id]=queryResult.Effective;
            rows=rows.Select(r=>r.Id==id ? EffectiveStates.Apply(r,queryResult.Effective) : r).ToList();
            if(result.Configuration is {} config)
            {
                var row=Row(config);
                var existing=rows.FindIndex(r=>r.Id==id && r.Priority==row.Priority); if(existing>=0) rows[existing]=row; else rows.Add(row);
                rows=rows.OrderBy(r=>r.Id).ToList();
                ClearFilters(null,null);
                Search.Text=id.ToString();
                ApplyFilter();
                Features.SelectedItem=row;
                Features.ScrollIntoView(row);
                var nameDetail=names.TryGetValue(id,out var featureName) ? $"Name: {featureName}." : "Name unavailable in the bundled dictionary; the ID was found.";
                Write($"Found feature {id} in {store}. Effective state: {row.EffectiveState} ({row.EffectivePriority}). Row state: {row.State}. Row priority: {row.Priority}. Variant: {row.Variant}. Payload: {row.Payload}. {nameDetail}");
            }
            else
            {
                var known=names.TryGetValue(id,out var name) ? $"Bundled dictionary: {name}. " : "This ID is not in the bundled dictionary; that dictionary is incomplete. ";
                var other=result.OtherConfiguration is {} otherConfig
                    ? $"Found in {result.OtherStore}: {JsonSerializer.Serialize(Row(otherConfig))}. Select that store to inspect it. "
                    : $"No configuration was returned by {result.OtherStore} either (status 0x{result.OtherStatus:X8}). ";
                var message=$"Feature {id}: {known}\n{store} returned no configuration (Windows status 0x{result.Status:X8}).\n{other}\nAn ID can be valid without a configuration on this Windows build. This query does not establish that the feature is nonexistent.";
                Write(message);
                var closest=FeatureLookup.FindClosest(rows.Select(r=>r.Id),id);
                if(closest is {} matchId)
                {
                    var match=rows.First(r=>r.Id==matchId);
                    ClearFilters(null,null);
                    Search.Text=matchId.ToString();
                    ApplyFilter();
                    Features.SelectedItem=match;
                    Features.ScrollIntoView(match);
                    Write($"No exact configuration for {id} in {store}. Selected closest match {matchId}: {match.Name}, {match.State}, priority {match.Priority}. The Feature ID field now contains {matchId}.");
                }
                else MessageBox.Show(this,message,"Feature query result",MessageBoxButton.OK,MessageBoxImage.Information);
            }
        } catch(Exception ex) {Write("Query failed: "+ex.Message);}
        finally {busy=false; IsEnabled=true;}
    }
    private async void EnableFeature(object s,RoutedEventArgs e)=>await Change(RTL_FEATURE_ENABLED_STATE.Enabled,false);
    private async void DisableFeature(object s,RoutedEventArgs e)=>await Change(RTL_FEATURE_ENABLED_STATE.Disabled,false);
    private async void ResetFeature(object s,RoutedEventArgs e)=>await Change(RTL_FEATURE_ENABLED_STATE.Default,true);
    private async Task Change(RTL_FEATURE_ENABLED_STATE state,bool reset)
    {
        if(busy) return;
        bool attempted=false;
        try {
            if(!Admin) throw new InvalidOperationException("Administrator access is required.");
            var id=ReadId();
            var action=reset ? "Remove the User-priority override for" : $"Set {state} for";
            if(MessageBox.Show(this,$"{action} feature {id} in BOTH Runtime and Boot?\n\nThe browsing-store selector does not limit changes. Other priorities are preserved. You must reboot Windows for restart-dependent features to take effect.","Confirm feature change",MessageBoxButton.OKCancel,MessageBoxImage.Warning)!=MessageBoxResult.OK) return;
            busy=true; IsEnabled=false;
            var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ViVeDesktop","Snapshots");
            Directory.CreateDirectory(folder);
            var prefix=Path.Combine(folder,$"{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}");
            // Capture BOTH stores before either one is changed.
            SaveSnapshot(prefix+"-Runtime.json",RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
            SaveSnapshot(prefix+"-Boot.json",RTL_FEATURE_CONFIGURATION_TYPE.Boot);
            var update=new RTL_FEATURE_CONFIGURATION_UPDATE {FeatureId=id,Priority=RTL_FEATURE_CONFIGURATION_PRIORITY.User,EnabledState=state,Operation=reset ? RTL_FEATURE_CONFIGURATION_OPERATION.ResetState : RTL_FEATURE_CONFIGURATION_OPERATION.FeatureState};
            attempted=true;
            var result=await Task.Run(()=>FeatureChanges.SaveBoth(update));
            Write($"Feature {id}: {result.Description} Pre-change snapshots: {prefix}-Runtime.json and {prefix}-Boot.json");
            if(!result.Saved) {
                MessageBox.Show(this,result.Description+"\n\nThe change was not fully saved. See the activity log and snapshots.","Feature change failed",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            RestartNotice.Text="Restart required: reboot Windows to apply feature changes.";
            MessageBox.Show(this,$"Feature {id} was saved to Runtime and Boot.\n\nREBOOT WINDOWS for this change to take full effect.\n\n{(result.BootMarkerStatus!=0 ? result.Description+"\n\n" : "")}Windows will not restart automatically.","Restart required",MessageBoxButton.OK,MessageBoxImage.Exclamation);
        } catch(Exception ex) { Write(ex.Message); MessageBox.Show(this,ex.Message,"Unable to complete change",MessageBoxButton.OK,MessageBoxImage.Error); }
        finally {
            busy=false; IsEnabled=true;
            if(attempted) await LoadFeatures();
        }
    }
    private void SaveSnapshot(string path,RTL_FEATURE_CONFIGURATION_TYPE? requestedStore=null)
    {
        var store=requestedStore ?? CurrentStore;
        var configs=FeatureManager.QueryAllFeatureConfigurations(store) ?? throw new InvalidOperationException("Cannot capture a snapshot: store query returned no data.");
        File.WriteAllText(path,JsonSerializer.Serialize(new {FormatVersion=1,CreatedUtc=DateTime.UtcNow,WindowsBuild=Environment.OSVersion.Version.ToString(),Store=store.ToString(),Configurations=configs.Select(c=>new {c.FeatureId,c.CompactState,c.VariantPayload})},new JsonSerializerOptions {WriteIndented=true}));
    }
    private void Export(object s,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog {Filter="JSON snapshot|*.json",FileName=$"ViVe-{CurrentStore}-{DateTime.Now:yyyyMMdd-HHmmss}.json"};
        if(dialog.ShowDialog(this)!=true) return;
        try {SaveSnapshot(dialog.FileName); Write("Exported "+dialog.FileName);} catch(Exception ex) {Write("Export failed: "+ex.Message);}
    }
}
