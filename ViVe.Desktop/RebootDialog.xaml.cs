// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using System.Windows;
namespace ViVe.Desktop;
public enum RestartChoice {KeepOpen,Later,RestartNow}
public partial class RebootDialog : Window
{
    public RestartChoice Choice {get;private set;}=RestartChoice.KeepOpen;
    public RebootDialog(){InitializeComponent();}
    private void RestartNow(object sender,RoutedEventArgs e){Choice=RestartChoice.RestartNow;DialogResult=true;}
    private void Later(object sender,RoutedEventArgs e){Choice=RestartChoice.Later;DialogResult=true;}
    private void KeepOpen(object sender,RoutedEventArgs e){Choice=RestartChoice.KeepOpen;DialogResult=false;}
}
