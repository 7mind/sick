using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SickSharp.Format;
using SickSharp.Format.Tables;
using Index = SickSharp.Encoder.Index;

namespace SickSharp.Test;


public class Tests
{
    private const string PathOut = "../../../../../output";
    private const string PathInput = "../../../../../samples";
    private const string RootName = "sample.json";
    private const uint iters = 100_000;

    private List<string> _files;
    

    [SetUp]
    public void Setup()
    {
        Directory.CreateDirectory(PathOut);
         _files = Directory.EnumerateFiles(PathInput, "*.json", SearchOption.AllDirectories).ToList();
    }

    [Test]
    public void Test1_EncodeAll()
    {
        foreach (var file in _files)
        {
            DoWrite(file, Path.Combine(PathOut, $"cs-{new FileInfo(file).Name}.bin"));
        }
    }
    
    // [Test]
    // public void Test3_repro()
    // {
    //     var input = Path.Combine(PathOut, "default_config");
    //     using (var stream = File.Open(input, FileMode.Open))
    //     {
    //         var name = new FileInfo(input).Name;
    //         Console.WriteLine($"Processing {name}...");
    //         var reader = new SickReader(stream);
    //         var rootRef = reader.GetRoot("data");
    //
    //         reader.Query(rootRef, "nodes.node_1.bool_node");
    //
    //     }
    // }


    // public int Traverse(Ref reference, SickReader reader, int count, short limit)
    // {
    //     var next = count + 1;
    //     if (count >= limit)
    //     {
    //         return count;
    //     }
    //     if (reference.Kind == RefKind.Arr)
    //     {
    //         var arr = ((JArr)reader.Resolve(reference)).Value;
    //         if (arr.Count == 0)
    //         {
    //             return next;
    //         }
    //
    //         var entry = arr.Content().Last();
    //         var entryRef = reader.ReadArrayElementRef(reference, arr.Count -1);
    //         Debug.Assert(entry == entryRef);
    //         return Traverse(entryRef, reader, next, limit);
    //     }
    //
    //     if (reference.Kind == RefKind.Obj)
    //     {
    //         var obj = ((JObj)reader.Resolve(reference)).Value;
    //         if (obj.Count == 0)
    //         {
    //             return next;
    //         }
    //
    //         var firstEntry = obj.Content().Last();
    //         var fieldVal = reader.ReadObjectFieldRef(reference, firstEntry.Key);
    //         Debug.Assert(fieldVal == firstEntry.Value);
    //         return Traverse(firstEntry.Value, reader, next, limit);
    //     }
    //
    //     return count;
    // }
    
    public int Traverse(Ref reference, SickReader reader, int count, short limit)
    {
        var next = count + 1;
        if (count >= limit)
        {
            return count;
        }
        if (reference.Kind == RefKind.Arr)
        {
            var arr = ((JArr)reader.Resolve(reference)).Value;
            if (arr.Count == 0)
            {
                return next;
            }

            var entry = arr.Content().First();
            var entryRef = reader.ReadArrayElementRef(reference, 0);
            Debug.Assert(entry == entryRef);
            return Traverse(entryRef, reader, next, limit);
        }

        if (reference.Kind == RefKind.Obj)
        {
            var obj = ((JObj)reader.Resolve(reference)).Value;
            if (obj.Count == 0)
            {
                return next;
            }

            var firstEntry = obj.Content().First();
            var fieldVal = reader.ReadObjectFieldRef(reference, firstEntry.Key);
            Debug.Assert(fieldVal == firstEntry.Value);
            return Traverse(firstEntry.Value, reader, next, limit);
        }

        return count;
    }

    [Test]
    public void Test2_Read()
    {
        var inputs = Directory.EnumerateFiles(PathOut, "*.bin", SearchOption.TopDirectoryOnly).ToList();

        foreach (var input in inputs)
        {
            using (var reader = SickReader.OpenFile(input))
            {
                var name = new FileInfo(input).Name;
                Console.WriteLine();
                Console.WriteLine($"Processing {name}...");
                var rootRef = reader.GetRoot(RootName);
                
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Console.WriteLine($"Going to perform {iters} traverses...");
                for (int x = 0; x < iters; x++)
                {
                    Traverse(rootRef!, reader, 0, 10);
                }

                stopwatch.Stop();
                TimeSpan stopwatchElapsed = stopwatch.Elapsed;
                Console.WriteLine($"Finished in {stopwatchElapsed.TotalSeconds} sec");
                Console.WriteLine($"Iters/sec {Convert.ToDouble(iters) / stopwatchElapsed.TotalSeconds}");
                
                Debug.Assert(rootRef != null, $"No root entry in {name}");
                Console.WriteLine($"{name}: found {RootName}, ref={rootRef}");
#if DEBUG_TRAVEL
            Console.WriteLine($"Total travel = {SickReader.TotalTravel}, total index lookups = {SickReader.TotalLookups}, ratio = {Convert.ToDouble(SickReader.TotalTravel) / SickReader.TotalLookups}");
#endif
                
            }
        }
    }
    
    public void DoWrite(string inPath, string outPath)
    {
        ushort buckets = 256;
        using var sr = new StreamReader(inPath);
        using var jreader = new JsonTextReader(sr);
        jreader.DateParseHandling = DateParseHandling.None;
        
        var loaded = JToken.Load(jreader);
        var index = Index.Create(buckets);
        var root = index.append(RootName, loaded);
        
        using (BinaryWriter binWriter =  
               new BinaryWriter(File.Open(outPath, FileMode.Create)))
        {
            var data = index.Serialize().data;
            Console.WriteLine($"Serialized with {buckets} buckets, size: {data.Length} bytes, source: {new FileInfo(inPath).Name}");
            binWriter.Write(data);  
        }
    }
}