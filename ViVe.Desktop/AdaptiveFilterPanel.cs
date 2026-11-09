// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using System.Windows;
using System.Windows.Controls;

namespace ViVe.Desktop;

/// <summary>Wraps labeled controls at their minimum widths and shares spare row width.</summary>
public sealed class AdaptiveFilterPanel : Panel
{
    private const double Gap=10;
    public static readonly DependencyProperty WeightProperty=DependencyProperty.RegisterAttached(
        "Weight",typeof(double),typeof(AdaptiveFilterPanel),new FrameworkPropertyMetadata(1d,FrameworkPropertyMetadataOptions.AffectsParentMeasure));
    public static void SetWeight(DependencyObject element,double value)=>element.SetValue(WeightProperty,value);
    public static double GetWeight(DependencyObject element)=>(double)element.GetValue(WeightProperty);

    private List<List<UIElement>> Rows(double available)
    {
        var rows=new List<List<UIElement>>();
        var row=new List<UIElement>();
        double used=0;
        foreach(UIElement child in InternalChildren) {
            if(child.Visibility==Visibility.Collapsed) continue;
            var width=Minimum(child);
            if(row.Count>0 && used+Gap+width>available) {rows.Add(row);row=new();used=0;}
            if(row.Count>0) used+=Gap;
            row.Add(child); used+=width;
        }
        if(row.Count>0) rows.Add(row);
        return rows;
    }
    private static double Minimum(UIElement child)=>child is FrameworkElement f ? Math.Max(1,f.MinWidth) : Math.Max(1,child.DesiredSize.Width);

    private static double[] Widths(List<UIElement> row,double available)
    {
        var widths=row.Select(Minimum).ToArray();
        var spare=Math.Max(0,available-widths.Sum()-Gap*(row.Count-1));
        var active=Enumerable.Range(0,row.Count).Where(i=>GetWeight(row[i])>0).ToList();
        while(spare>.1 && active.Count>0) {
            var total=active.Sum(i=>GetWeight(row[i]));
            double assigned=0;
            foreach(var i in active) {
                var max=row[i] is FrameworkElement f ? f.MaxWidth : double.PositiveInfinity;
                var grow=Math.Min(Math.Max(0,max-widths[i]),spare*GetWeight(row[i])/total);
                widths[i]+=grow; assigned+=grow;
            }
            spare-=assigned;
            active.RemoveAll(i=>row[i] is FrameworkElement f && widths[i]>=f.MaxWidth-.1);
            if(assigned<.1) break;
        }
        return widths;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width=double.IsInfinity(availableSize.Width) ? InternalChildren.Cast<UIElement>().Where(c=>c.Visibility!=Visibility.Collapsed).Sum(Minimum)+Gap*Math.Max(0,InternalChildren.Count-1) : availableSize.Width;
        double height=0,desiredWidth=0;
        foreach(var row in Rows(width)) {
            var widths=Widths(row,width);
            double rowHeight=0;
            for(int i=0;i<row.Count;i++) {
                row[i].Measure(new Size(widths[i],double.PositiveInfinity));
                rowHeight=Math.Max(rowHeight,row[i].DesiredSize.Height);
            }
            if(height>0) height+=Gap;
            height+=rowHeight;
            desiredWidth=Math.Max(desiredWidth,widths.Sum()+Gap*(row.Count-1));
        }
        return new Size(Math.Min(width,desiredWidth),height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y=0;
        foreach(var row in Rows(finalSize.Width)) {
            var widths=Widths(row,finalSize.Width);
            var height=row.Max(child=>child.DesiredSize.Height);
            double x=0;
            for(int i=0;i<row.Count;i++) {
                row[i].Arrange(new Rect(x,y,widths[i],height));
                x+=widths[i]+Gap;
            }
            y+=height+Gap;
        }
        return finalSize;
    }
}
