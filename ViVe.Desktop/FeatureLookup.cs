using Albacore.ViVe;
using Albacore.ViVe.NativeEnums;
using Albacore.ViVe.NativeMethods;
using Albacore.ViVe.NativeStructs;

namespace ViVe.Desktop;

public record FeatureLookupResult(RTL_FEATURE_CONFIGURATION? Configuration, int Status,
    RTL_FEATURE_CONFIGURATION_TYPE OtherStore, RTL_FEATURE_CONFIGURATION? OtherConfiguration, int OtherStatus);

public static class FeatureLookup
{
    public static uint? FindClosest(IEnumerable<uint> ids, uint entered)
    {
        var candidates=ids.Distinct().ToArray();
        if(candidates.Length==0) return null;
        var digits=entered.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return candidates.OrderBy(id=>id==entered ? 0 : id.ToString(System.Globalization.CultureInfo.InvariantCulture).StartsWith(digits,StringComparison.Ordinal) ? 1 : 2)
            .ThenBy(id=>Math.Abs((long)id-entered)).ThenBy(id=>id).First();
    }

    public static FeatureLookupResult Query(uint id, RTL_FEATURE_CONFIGURATION_TYPE store)
    {
        var configuration=Read(id,store,out var status);
        var otherStore=store==RTL_FEATURE_CONFIGURATION_TYPE.Runtime
            ? RTL_FEATURE_CONFIGURATION_TYPE.Boot : RTL_FEATURE_CONFIGURATION_TYPE.Runtime;
        RTL_FEATURE_CONFIGURATION? other=null;
        int otherStatus=0;
        if(configuration is null) other=Read(id,otherStore,out otherStatus);
        return new(configuration,status,otherStore,other,otherStatus);
    }

    private static RTL_FEATURE_CONFIGURATION? Read(uint id,RTL_FEATURE_CONFIGURATION_TYPE store,out int status)
    {
        ulong stamp=0;
        status=Ntdll.RtlQueryFeatureConfiguration(id,store,ref stamp,out var configuration);
        if(status>=0 && configuration.FeatureId==id) return configuration;
        // Enumeration can expose configured entries that an individual query cannot resolve.
        var all=FeatureManager.QueryAllFeatureConfigurations(store);
        if(all!=null)
            foreach(var entry in all)
                if(entry.FeatureId==id) return entry;
        return null;
    }
}
