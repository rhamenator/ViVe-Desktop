using Albacore.ViVe.NativeEnums;
using Albacore.ViVe.NativeMethods;
using Albacore.ViVe.NativeStructs;

namespace ViVe.Desktop;

public record EffectiveConfiguration(string State,string Priority,uint? Variant,string Detail);
public static class EffectiveStates
{
    public static EffectiveConfiguration Query(uint id,RTL_FEATURE_CONFIGURATION_TYPE store)
    {
        try {
            ulong stamp=0;
            var status=Ntdll.RtlQueryFeatureConfiguration(id,store,ref stamp,out var config);
            return Interpret(id,store,status,config);
        }
        catch(Exception ex) {return new("Unknown","Unavailable",null,$"Windows could not resolve this feature: {ex.Message}");}
    }

    public static EffectiveConfiguration Interpret(uint id,RTL_FEATURE_CONFIGURATION_TYPE store,int status,RTL_FEATURE_CONFIGURATION config)
    {
        if(status<0 || config.FeatureId!=id)
            return new("Unknown","Unavailable",null,$"{store}: Windows query failed or returned a different ID (status 0x{status:X8}).");
        var state=config.EnabledState switch {
            RTL_FEATURE_ENABLED_STATE.Enabled => "Enabled",
            RTL_FEATURE_ENABLED_STATE.Disabled => "Disabled",
            RTL_FEATURE_ENABLED_STATE.Default => "Default",
            _ => "Unknown"
        };
        var detail=$"{store}: Windows resolved {state} at {config.Priority} priority, variant {config.Variant}.";
        if(state=="Default") detail+=" The consuming application's built-in default is not exposed by this API; do not interpret Default as Disabled.";
        return new(state,config.Priority.ToString(),config.Variant,detail);
    }

    public static Dictionary<uint,EffectiveConfiguration> QueryAll(IEnumerable<uint> ids,RTL_FEATURE_CONFIGURATION_TYPE store)
        => ids.Distinct().ToDictionary(id=>id,id=>Query(id,store));

    public static FeatureRow Apply(FeatureRow row,EffectiveConfiguration effective)
        => row with {EffectiveState=effective.State,EffectivePriority=effective.Priority,EffectiveDetail=effective.Detail};
}
