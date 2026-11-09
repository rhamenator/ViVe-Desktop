// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ViVe.Desktop;

public record SavedLayout(int Version,double Left,double Top,double Width,double Height,bool Maximized,double TableShare)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Valid => (Version==1 || Version==2) && new[]{Left,Top,Width,Height,TableShare}.All(double.IsFinite)
        && Width>=200 && Height>=200 && Width<=32000 && Height<=32000
        && Math.Abs(Left)<1000000 && Math.Abs(Top)<1000000 && TableShare>0 && TableShare<1;
}

public static class WindowLayoutStore
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ViVeDesktop","window-layout.json");
    public static SavedLayout? Load(string path)
    {
        try {
            var value=JsonSerializer.Deserialize<SavedLayout>(File.ReadAllText(path));
            return value is {Valid:true} ? value : null;
        } catch {return null;}
    }
    public static void Save(string path,SavedLayout value)
    {
        if(!value.Valid) throw new InvalidDataException("Invalid window layout.");
        var folder=Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(folder);
        var temporary=Path.Combine(folder,Guid.NewGuid()+".tmp");
        try {
            File.WriteAllText(temporary,JsonSerializer.Serialize(value,new JsonSerializerOptions {WriteIndented=true}));
            File.Move(temporary,path,true);
        } finally {if(File.Exists(temporary)) File.Delete(temporary);}
    }
    public static Rect Fit(Rect saved,IReadOnlyList<Rect> workAreas)
    {
        var areas=workAreas.Where(r=>r.Width>0 && r.Height>0).ToArray();
        if(areas.Length==0) return saved;
        static double Distance(Rect a,Rect b) {
            var dx=(a.Left+a.Width/2)-(b.Left+b.Width/2);
            var dy=(a.Top+a.Height/2)-(b.Top+b.Height/2);
            return dx*dx+dy*dy;
        }
        static double Overlap(Rect a,Rect b) {var r=Rect.Intersect(a,b);return r.IsEmpty ? 0 : r.Width*r.Height;}
        var area=areas.OrderByDescending(r=>Overlap(saved,r)).ThenBy(r=>Distance(saved,r)).First();
        var width=Math.Min(saved.Width,area.Width);
        var height=Math.Min(saved.Height,area.Height);
        return new Rect(Math.Clamp(saved.Left,area.Left,area.Right-width),Math.Clamp(saved.Top,area.Top,area.Bottom-height),width,height);
    }
}
