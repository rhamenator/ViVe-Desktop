// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Data;

namespace ViVe.Desktop;

public record ColumnPreference(string Key,double Width,DataGridLengthUnitType Unit);
public record SortPreference(string Key,ListSortDirection Direction);
public record ViewPreferences(int Version,string Search,int Store,int StateScope,string State,string Priority,string Variant,bool Details,List<ColumnPreference> Columns,List<SortPreference> Sorts);

public static class ViewPreferencesStore
{
    public static readonly HashSet<string> Keys=new() {"Id","Name","EffectiveState","EffectivePriority","State","Priority","Variant","Payload"};
    public static ViewPreferences? Load(string path)
    {
        try {
            var view=JsonSerializer.Deserialize<ViewPreferences>(File.ReadAllText(path));
            if(view==null || view.Version!=1 || view.Search==null || view.Variant==null || view.State==null || view.Priority==null
                || view.Store is <0 or >1 || view.StateScope is <0 or >1 || view.Search.Length>1000 || view.Variant.Length>1000
                || view.Columns==null || view.Sorts==null) return null;
            return view with {
                Columns=view.Columns.Where(c=>Keys.Contains(c.Key) && double.IsFinite(c.Width) && c.Width>0 && c.Width<=8000 && Enum.IsDefined(c.Unit)).DistinctBy(c=>c.Key).ToList(),
                Sorts=view.Sorts.Where(s=>Keys.Contains(s.Key) && Enum.IsDefined(s.Direction)).DistinctBy(s=>s.Key).ToList()
            };
        } catch {return null;}
    }
    public static void Save(string path,ViewPreferences value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp=Path.Combine(Path.GetDirectoryName(path)!,Guid.NewGuid()+".tmp");
        try {
            File.WriteAllText(temp,JsonSerializer.Serialize(value,new JsonSerializerOptions {WriteIndented=true}));
            File.Move(temp,path,true);
        } finally {if(File.Exists(temp)) File.Delete(temp);}
    }

    public static void ApplySorts(DataGrid grid,IReadOnlyList<SortPreference> sorts)
    {
        if(grid.ItemsSource==null) return;
        var view=CollectionViewSource.GetDefaultView(grid.ItemsSource);
        if(!view.CanSort) return;
        using(view.DeferRefresh()) {
            view.SortDescriptions.Clear();
            foreach(var sort in sorts.Where(s=>Keys.Contains(s.Key)))
                view.SortDescriptions.Add(new SortDescription(sort.Key,sort.Direction));
        }
        foreach(var column in grid.Columns)
            column.SortDirection=sorts.FirstOrDefault(s=>s.Key==column.SortMemberPath)?.Direction;
    }

    public static List<SortPreference> ReadSorts(DataGrid grid)
    {
        if(grid.ItemsSource==null) return new();
        return CollectionViewSource.GetDefaultView(grid.ItemsSource).SortDescriptions
            .Where(s=>Keys.Contains(s.PropertyName)).Select(s=>new SortPreference(s.PropertyName,s.Direction)).ToList();
    }
}
