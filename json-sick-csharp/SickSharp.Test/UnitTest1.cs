using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SickSharp.Format;
using SickSharp.Format.Tables;
using Index = SickSharp.Encoder.Index;

namespace SickSharp.Test;


public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    public void Write()
    {
        ushort buckets = 256;
        var path = "../../../../../data.json";
        var outPath = "../../../../../data.bin";

        using var sr = new StreamReader(path);
        using var jreader = new JsonTextReader(sr);
        jreader.DateParseHandling = DateParseHandling.None;
        
        var loaded = JToken.Load(jreader);
        var index = Index.Create(buckets);
        var root = index.append("config.json", loaded);
        
        using (BinaryWriter binWriter =  
               new BinaryWriter(File.Open(outPath, FileMode.Create)))
        {
            var data = index.Serialize().data;
            Console.WriteLine($"Serialized with {buckets} buckets, size: {data.Length} bytes");
            binWriter.Write(data);  
        }
    }
    
    [Test]
    public void Test1()
    {
        Write();
        var path = "../../../../../data.bin";
        using (var stream = File.Open(path, FileMode.Open))
        {
            
            var reader = new SickReader(stream);
            var rootRef = reader.GetRoot("config.json");
            var nodesValue = reader.QueryRef(rootRef!, "nodes");
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var iters = 1_000_000;
            Console.WriteLine($"Going to perform {iters} lookups...");
            for (int x = 0; x < iters; x++)
            {
                var f1 = reader.ReadObjectFieldRef(nodesValue, "node_399");
                var f2 = reader.ReadObjectFieldRef(f1, "int_node");
                var resolved = reader.Resolve(f2);

                // reader.Query(nodesValue, "node_399.int_node");
                // reader.Query(nodesValue, "node_398.int_node");
                // reader.Query(nodesValue, "node_397.int_node");
            }
            stopwatch.Stop();
            TimeSpan stopwatchElapsed = stopwatch.Elapsed;
            Console.WriteLine($"Finished in {stopwatchElapsed.TotalSeconds} sec");
            Console.WriteLine($"Iters/sec {Convert.ToDouble(iters) / stopwatchElapsed.TotalSeconds}");
            #if DEBUG_TRAVEL
            Console.WriteLine($"Total travel = {SickReader.TotalTravel}, total index lookups = {SickReader.TotalLookups}, ratio = {Convert.ToDouble(SickReader.TotalTravel) / SickReader.TotalLookups}");
            #endif
            // Console.WriteLine(tgt);
            //VerifyConfigJson(reader);
        }
    }
    
    // [Test]
    // public void Test2()
    // {
    //     var path = "../../../../../json-sick-scala/config.json";
    //     var outPath = "../../../../../json-sick-scala/config-sharp.bin";
    //     
    //     using var sr = new StreamReader(path);
    //     using var jreader = new JsonTextReader(sr);
    //     jreader.DateParseHandling = DateParseHandling.None;
    //     
    //     var loaded = JToken.Load(jreader);
    //     var index = Index.Create();
    //     var root = index.append("config.json", loaded);
    //     
    //     using (BinaryWriter binWriter =  
    //            new BinaryWriter(File.Open(outPath, FileMode.Create)))  
    //     {  
    //         binWriter.Write(index.Serialize().data);  
    //     }
    //
    //     using (var stream = File.Open(outPath, FileMode.Open))
    //     {
    //         var reader = new SickReader(stream);
    //         VerifyConfigJson(reader);
    //     }
    // }
    //
    // [Test]
    // public void CheckKHash()
    // {
    //     Debug.Assert(KHash.Compute("test") == 1753799407);
    //     Debug.Assert(KHash.Compute("segments") == 2958995695);
    // }
    //  
    // [Test]
    // public void Test1()
    // {
    //     var path = "../../../../../json-sick-scala/output.bin";
    //     using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65535))
    //     {
    //         var reader = new SickReader(stream);
    //         VerifyConfigJson(reader);
    //     }
    // }
    //
    // private static void VerifyConfigJson(SickReader reader)
    // {
    //     Console.WriteLine(reader.Header);
    //     // Debug.Assert(reader.Bytes.Count == 59);
    //     // Debug.Assert(reader.Shorts.Count == 1983);
    //     Debug.Assert(reader.Ints.Count == 8);
    //     Debug.Assert(reader.Longs.Count == 0);
    //
    //     Debug.Assert(reader.Floats.Count == 5);
    //     Debug.Assert(reader.Doubles.Count == 0);
    //     Debug.Assert(reader.Strings.Count == 27365);
    //     Debug.Assert(reader.Arrs.Count == 5124);
    //     Debug.Assert(reader.Objs.Count == 44130);
    //
    //     var configRef = reader.GetRoot("config.json")!;
    //     Console.WriteLine(configRef);
    //
    //
    //     var config = reader.Resolve(configRef);
    //     Console.WriteLine(config);
    //     Console.WriteLine(config.Match(new TestMatcher()));
    //     foreach (var key in ((JObj)config).Value) Console.WriteLine($"{key.Key} -> {key.Value}");
    //
    //     var queryResult = reader.Query(configRef, "mutations[0].segments[-1].segment");
    //     Console.WriteLine($"{queryResult} == {String.Join(", ", ((JObj)queryResult).Value.Content().ToList())}");
    // }
    //
    // private class TestMatcher : JsonMatcher<string>
    // {
    //     public override string? OnObj(OneObjTable value)
    //     {
    //         return $"Object with fields {{ {string.Join(", ", value.ReadAll())} }}";
    //     }
    // }
}