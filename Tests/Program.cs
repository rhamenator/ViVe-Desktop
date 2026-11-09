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

var filterRow=new ViVe.Desktop.FeatureRow(61934903,"Context menu teaching tip","Disabled","User",3,0);
Assert(ViVe.Desktop.FeatureFilter.Matches(filterRow,"TEACHING","Disabled","User","3"),"Name search is case insensitive and combines all filters");
Assert(!ViVe.Desktop.FeatureFilter.Matches(filterRow,"6193","Enabled","User","3"),"State filter excludes other states");
Assert(!ViVe.Desktop.FeatureFilter.Matches(filterRow,"6193","Disabled","Service","3"),"Priority filter excludes other layers");
Assert(!ViVe.Desktop.FeatureFilter.Matches(filterRow,"6193","Disabled","User","2"),"Variant filter uses exact numeric value");
Assert(!ViVe.Desktop.FeatureFilter.Matches(filterRow,"","All states","All priorities","oops"),"Invalid variant cannot silently match");
Assert(ViVe.Desktop.FeatureFilter.Matches(filterRow,"6193","All states","All priorities",""),"Partial ID search combines with unfiltered attributes");

var initialNames=new Dictionary<uint,string> {{61934903,"Teaching tip [community label]"},{1000,"OldName"}};
var dump=string.Join("\n",Enumerable.Range(1000,101).Select(id=>$"  Feature_{id}: {id}"))+"\n  888888: 888888\n<html>not a name</html>";
var merged=ViVe.Desktop.DictionaryUpdater.MergeDump(initialNames,dump);
Assert(merged[61934903]==initialNames[61934903],"Update preserves labels not replaced by symbolic names");
Assert(merged[1000]=="Feature_1000" && !merged.ContainsKey(888888),"Update refreshes symbolic names and skips numeric placeholders");
try {ViVe.Desktop.DictionaryUpdater.MergeDump(initialNames,"<html>failure</html>");throw new Exception("Expected invalid download rejection");} catch(InvalidDataException) {Console.WriteLine("PASS: Invalid dictionary download is rejected");}
var cacheDirectory=Path.Combine(Path.GetTempPath(),"ViVeDictionaryTests-"+Guid.NewGuid());
Directory.CreateDirectory(cacheDirectory);
var cachePath=Path.Combine(cacheDirectory,"names.pfs");
try {
    var update=new ViVe.Desktop.DictionaryUpdate(merged,"test","test.txt",101);
    ViVe.Desktop.DictionaryUpdater.Save(update,cachePath);
    Assert(ViVe.Desktop.DictionaryUpdater.Read(cachePath)[1000]=="Feature_1000","Saved update reloads correctly");
    ViVe.Desktop.DictionaryUpdater.Save(update with {Names=initialNames},cachePath);
    Assert(ViVe.Desktop.DictionaryUpdater.Read(cachePath+".bak")[1000]=="Feature_1000","Previous dictionary retained as backup");
} finally {Directory.Delete(cacheDirectory,true);}
if(args.Contains("--download")) {
    var live=await ViVe.Desktop.DictionaryUpdater.DownloadAsync(initialNames);
    Assert(live.Names.Count>100 && live.Revision.Length==40,"Live GitHub dictionary download validates and merges");
    Console.WriteLine($"Downloaded {live.SourceFile}, {live.Changed} changes, revision {live.Revision}");
}
