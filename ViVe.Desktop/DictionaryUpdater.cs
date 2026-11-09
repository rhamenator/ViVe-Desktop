// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ViVe.Desktop;

public record DictionaryUpdate(Dictionary<uint,string> Names,string Revision,string SourceFile,int Changed);
public static partial class DictionaryUpdater
{
    public static string CachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ViVeDesktop","Dictionary","FeatureDictionary.pfs");
    private const string Repository="phantomofearth/windows-velocity-feature-lists";

    public static Dictionary<uint,string> Read(string path)
    {
        var result=new Dictionary<uint,string>();
        foreach(var line in File.ReadLines(path)) {
            var split=line.LastIndexOf(',');
            if(split>0 && uint.TryParse(line[(split+1)..],NumberStyles.None,CultureInfo.InvariantCulture,out var id) && id>0)
                result[id]=line[..split];
        }
        return result;
    }

    public static Dictionary<uint,string> MergeDump(IReadOnlyDictionary<uint,string> existing,string dump)
    {
        var merged=existing.ToDictionary(p=>p.Key,p=>p.Value);
        int valid=0;
        foreach(var line in dump.Split('\n')) {
            var match=DumpLine().Match(line);
            if(!match.Success || !uint.TryParse(match.Groups[2].Value,out var id) || id==0) continue;
            merged[id]=match.Groups[1].Value;
            valid++;
        }
        if(valid<100) throw new InvalidDataException("The downloaded feature list did not contain enough valid symbolic names. Your current dictionary was kept.");
        return merged;
    }

    public static async Task<DictionaryUpdate> DownloadAsync(IReadOnlyDictionary<uint,string> existing,CancellationToken token=default)
    {
        using var client=new HttpClient {Timeout=TimeSpan.FromSeconds(30)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ViVeDesktop/1.1");
        // Pin all subsequent reads to one commit so the file cannot change between requests.
        using var commit=JsonDocument.Parse(await GetText(client,$"https://api.github.com/repos/{Repository}/commits/main",token));
        var sha=commit.RootElement.GetProperty("sha").GetString()!;
        if(!Regex.IsMatch(sha,"^[a-f0-9]{40}$")) throw new InvalidDataException("Invalid source revision.");
        using var tree=JsonDocument.Parse(await GetText(client,$"https://api.github.com/repos/{Repository}/git/trees/{sha}?recursive=1",token));
        if(tree.RootElement.TryGetProperty("truncated",out var truncated) && truncated.GetBoolean()) throw new InvalidDataException("Source listing was incomplete.");
        var file=tree.RootElement.GetProperty("tree").EnumerateArray()
            .Where(e=>e.GetProperty("type").GetString()=="blob")
            .Select(e=>e.GetProperty("path").GetString()!)
            .Where(path=>SourceFile().IsMatch(path))
            .OrderByDescending(path=>Version.Parse(Path.GetFileNameWithoutExtension(path))).FirstOrDefault()
            ?? throw new InvalidDataException("No supported Windows feature dump was found.");
        var text=await GetText(client,$"https://raw.githubusercontent.com/{Repository}/{sha}/{file}",token);
        var merged=MergeDump(existing,text);
        var changed=merged.Count(p=>!existing.TryGetValue(p.Key,out var old) || old!=p.Value);
        return new(merged,sha,file,changed);
    }

    private static async Task<string> GetText(HttpClient client,string url,CancellationToken token)
    {
        using var response=await client.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token);
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>8*1024*1024) throw new InvalidDataException("Dictionary response is too large.");
        await using var stream=await response.Content.ReadAsStreamAsync(token);
        using var memory=new MemoryStream();
        var buffer=new byte[8192];
        int read;
        while((read=await stream.ReadAsync(buffer,token))>0) {
            if(memory.Length+read>8*1024*1024) throw new InvalidDataException("Dictionary response is too large.");
            memory.Write(buffer,0,read);
        }
        return System.Text.Encoding.UTF8.GetString(memory.ToArray());
    }

    public static void Save(DictionaryUpdate update,string path)
    {
        var folder=Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(folder);
        var temp=Path.Combine(folder,Guid.NewGuid()+".tmp");
        try {
            File.WriteAllLines(temp,update.Names.OrderByDescending(p=>p.Key).Select(p=>$"{p.Value},{p.Key}"));
            if(File.Exists(path)) File.Copy(path,path+".bak",true);
            File.Move(temp,path,true);
        }
        finally {if(File.Exists(temp)) File.Delete(temp);}
    }

    [GeneratedRegex(@"^\s+([A-Za-z_][A-Za-z0-9_]*):\s+(\d+)\s*$")]
    private static partial Regex DumpLine();
    [GeneratedRegex(@"^rs_prerelease/amd64/\d+\.\d+\.txt$")]
    private static partial Regex SourceFile();
}
