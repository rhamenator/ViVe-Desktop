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
Console.WriteLine($"Read {configs.Length} {store} configurations.");
}
