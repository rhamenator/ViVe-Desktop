using Albacore.ViVe;
using Albacore.ViVe.NativeEnums;
using Albacore.ViVe.NativeStructs;
using Microsoft.Win32;

namespace ViVe.Desktop;

public record FeatureChangeResult(int RuntimeStatus,int? BootStatus,int? BootMarkerStatus)
{
    public bool Saved => RuntimeStatus==0 && BootStatus==0;
    public string Description => RuntimeStatus!=0 ? $"Runtime write failed (0x{RuntimeStatus:X8}). Boot was not changed."
        : BootStatus!=0 ? $"Runtime was changed, but Boot failed (0x{BootStatus:X8}). The change was only partially saved."
        : BootMarkerStatus!=0 ? $"Runtime and Boot were saved, but the boot-status update failed (0x{BootMarkerStatus:X8})."
        : "Runtime and Boot were saved and verified.";
}

public static class FeatureChanges
{
    public static FeatureChangeResult SaveBoth(RTL_FEATURE_CONFIGURATION_UPDATE update,
        Func<RTL_FEATURE_CONFIGURATION_UPDATE,RTL_FEATURE_CONFIGURATION_TYPE,int> write,
        Func<int> markBoot)
    {
        var runtime=write(update,RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
        if(runtime!=0) return new(runtime,null,null);
        var boot=write(update,RTL_FEATURE_CONFIGURATION_TYPE.Boot);
        if(boot!=0) return new(runtime,boot,null);
        try {return new(runtime,boot,markBoot());} catch(Exception ex) {return new(runtime,boot,ex.HResult);}
    }

    public static FeatureChangeResult SaveBoth(RTL_FEATURE_CONFIGURATION_UPDATE update)
        => SaveBoth(update,WriteAndVerify,MarkBootPending);

    private static int WriteAndVerify(RTL_FEATURE_CONFIGURATION_UPDATE update,RTL_FEATURE_CONFIGURATION_TYPE store)
    {
        try {
            var status=FeatureManager.SetFeatureConfigurations(new[]{update},store);
            if(status!=0) return status;
            var reset=update.Operation==RTL_FEATURE_CONFIGURATION_OPERATION.ResetState;
            if(store==RTL_FEATURE_CONFIGURATION_TYPE.Runtime) {
                var configs=FeatureManager.QueryAllFeatureConfigurations(store);
                if(configs==null) return unchecked((int)0xC0000001);
                var matching=configs.Where(c=>c.FeatureId==update.FeatureId && c.Priority==update.Priority).ToArray();
                return (reset ? matching.Length==0 : matching.Any(c=>c.EnabledState==update.EnabledState)) ? 0 : unchecked((int)0xC0000001);
            }
            var keyPath=$@"SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\{(int)update.Priority}\{ObfuscationHelpers.ObfuscateFeatureId(update.FeatureId)}";
            using var key=Registry.LocalMachine.OpenSubKey(keyPath);
            return (reset ? key==null : key?.GetValue("EnabledState") is int state && state==(int)update.EnabledState) ? 0 : unchecked((int)0xC0000001);
        } catch(Exception ex) {return ex.HResult;}
    }

    private static int MarkBootPending()
    {
        var status=FeatureManager.GetBootFeatureConfigurationState(out var current);
        if(unchecked((uint)status)==0xC0000034) status=FeatureManager.InitializeBootStatusDataFile();
        if(status!=0) return status;
        return current==BSD_FEATURE_CONFIGURATION_STATE.BootPending ? 0 :
            FeatureManager.SetBootFeatureConfigurationState(BSD_FEATURE_CONFIGURATION_STATE.BootPending);
    }
}
