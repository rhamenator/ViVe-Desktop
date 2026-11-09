using System.Runtime.InteropServices;
using Albacore.ViVe;
using Albacore.ViVe.NativeEnums;
using Albacore.ViVe.NativeStructs;
static void Assert(bool ok,string name) {if(!ok) throw new Exception(name); Console.WriteLine("PASS: "+name);}
Assert(Marshal.SizeOf<RTL_FEATURE_CONFIGURATION>()==12,"Native configuration layout");
Assert(Marshal.SizeOf<RTL_FEATURE_CONFIGURATION_UPDATE>()==32,"Native update layout");
var c=new RTL_FEATURE_CONFIGURATION {CompactState=0xFFFF0000,FeatureId=123};
c.Priority=RTL_FEATURE_CONFIGURATION_PRIORITY.User; c.EnabledState=RTL_FEATURE_ENABLED_STATE.Enabled;c.Variant=63;
Assert(c.Priority==RTL_FEATURE_CONFIGURATION_PRIORITY.User && c.EnabledState==RTL_FEATURE_ENABLED_STATE.Enabled && c.Variant==63 && (c.CompactState&0xFFFF0000)==0xFFFF0000,"Bitfields preserve unrelated bits");
try {c.Variant=64;throw new Exception("Expected overflow");} catch(Albacore.ViVe.Exceptions.FeaturePropertyOverflowException) {Console.WriteLine("PASS: Variant bounds");}
try {FeatureManager.SetFeatureConfigurations(new[]{new RTL_FEATURE_CONFIGURATION_UPDATE{Priority=RTL_FEATURE_CONFIGURATION_PRIORITY.Security}});throw new Exception("Expected immutable guard");} catch(ArgumentException) {Console.WriteLine("PASS: Immutable priority blocked before native write");}
try {FeatureManager.SetFeatureConfigurations(new[]{new RTL_FEATURE_CONFIGURATION_UPDATE{Priority=RTL_FEATURE_CONFIGURATION_PRIORITY.UserPolicy,Variant=1}});throw new Exception("Expected policy guard");} catch(ArgumentException) {Console.WriteLine("PASS: Invalid policy properties blocked before native write");}
foreach(var store in Enum.GetValues<RTL_FEATURE_CONFIGURATION_TYPE>()) {
    var configs=FeatureManager.QueryAllFeatureConfigurations(store);
    Assert(configs is {Length:>0},"Read-only native query "+store);
    int checkedCount=0;
    foreach(var entry in configs) {
        var result=ViVe.Desktop.FeatureLookup.Query(entry.FeatureId,store);
        if(result.Configuration?.FeatureId!=entry.FeatureId)
            throw new Exception($"Query ID failed for listed feature {entry.FeatureId} in {store}: 0x{result.Status:X8}");
        checkedCount++;
    }
    Assert(checkedCount==configs.Length,$"Query ID finds all {checkedCount} listed IDs in {store}");
}
var absent=ViVe.Desktop.FeatureLookup.Query(uint.MaxValue,RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
Assert(absent.OtherStore==RTL_FEATURE_CONFIGURATION_TYPE.Boot,"Lookup identifies alternate store");
if(absent.Configuration==null) Assert(absent.Status!=0,"Missing configuration preserves native status");

var reported=ViVe.Desktop.FeatureLookup.Query(61934903,RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
Assert(reported.Configuration?.FeatureId==61934903,"User-reported ID 61934903 resolves in Runtime");
Console.WriteLine($"Reported ID: state={reported.Configuration?.EnabledState}, priority={reported.Configuration?.Priority}");

Assert(ViVe.Desktop.FeatureLookup.FindClosest(new uint[]{61934903,61934904,62000000},61934903)==61934903,"Closest match prefers exact ID");
Assert(ViVe.Desktop.FeatureLookup.FindClosest(new uint[]{60000000,61934903,61934904},6193)==61934903,"Partial ID prefers prefix over numeric distance");
Assert(ViVe.Desktop.FeatureLookup.FindClosest(new uint[]{100,110},106)==110,"Nearby ID selects numeric nearest");
Assert(ViVe.Desktop.FeatureLookup.FindClosest(new uint[]{110,100},105)==100,"Equal distances select lower ID consistently");
Assert(ViVe.Desktop.FeatureLookup.FindClosest(new uint[]{1,uint.MaxValue},uint.MaxValue-1)==uint.MaxValue,"Numeric distance does not overflow");
Assert(ViVe.Desktop.FeatureLookup.FindClosest(Array.Empty<uint>(),123)==null,"Empty list has no closest match");
