// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using Albacore.ViVe;
using Albacore.ViVe.NativeEnums;
using Albacore.ViVe.NativeStructs;
using Microsoft.Win32;
namespace ViVe.Desktop;
public record ChangeNeed(bool Runtime,bool Boot)
{
    public bool Any=>Runtime || Boot;
    public bool WasChanged(FeatureChangeResult result)=>(Runtime && result.RuntimeStatus==0) || (Boot && result.BootStatus==0);
}
public static class RebootPlanning
{
    public static bool ShouldPrompt(bool changed,bool busy)=>changed && !busy;
    public static ChangeNeed Needed(RTL_FEATURE_CONFIGURATION_UPDATE update)
    {
        var reset=update.Operation==RTL_FEATURE_CONFIGURATION_OPERATION.ResetState;
        var configs=FeatureManager.QueryAllFeatureConfigurations(RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
        var matching=configs?.Where(c=>c.FeatureId==update.FeatureId && c.Priority==update.Priority).ToArray();
        bool runtime=matching==null || (reset ? matching.Length>0 : !matching.Any(c=>c.EnabledState==update.EnabledState));
        using var key=Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\{(int)update.Priority}\{ObfuscationHelpers.ObfuscateFeatureId(update.FeatureId)}");
        bool boot=reset ? key!=null : key?.GetValue("EnabledState") is not int state || state!=(int)update.EnabledState;
        return new(runtime,boot);
    }
}
