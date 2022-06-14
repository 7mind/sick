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
    
    [Test]
    public void Test2()
    {
        var path = "../../../../../json-sick-scala/config.json";
        var outPath = "../../../../../json-sick-scala/config-sharp.bin";
        
        using var sr = new StreamReader(path);
        using var jreader = new JsonTextReader(sr);
        jreader.DateParseHandling = DateParseHandling.None;
        
        var loaded = JToken.Load(jreader);
        var index = Index.Create();
        var root = index.append("config.json", loaded);
        
        using (BinaryWriter binWriter =  
               new BinaryWriter(File.Open(outPath, FileMode.Create)))  
        {  
            binWriter.Write(index.Serialize().data);  
        }

        using (var stream = File.Open(outPath, FileMode.Open))
        {
            var reader = new SickReader(stream);
            VerifyConfigJson(reader);
        }
    }
    
    [Test]
    public void Test1()
    {
        var path = "../../../../../json-sick-scala/output.bin";
        using (var stream = File.Open(path, FileMode.Open))
        {
            var reader = new SickReader(stream);
            VerifyConfigJson(reader);
        }
    }

    private static void VerifyConfigJson(SickReader reader)
    {
        Console.WriteLine(reader.Header);
        // Debug.Assert(reader.Bytes.Count == 59);
        // Debug.Assert(reader.Shorts.Count == 1983);
        Debug.Assert(reader.Ints.Count == 8);
        Debug.Assert(reader.Longs.Count == 0);

        Debug.Assert(reader.Floats.Count == 5);
        Debug.Assert(reader.Doubles.Count == 0);
        Debug.Assert(reader.Strings.Count == 27365);
        Debug.Assert(reader.Arrs.Count == 5124);
        Debug.Assert(reader.Objs.Count == 44130);

        var configRef = reader.GetRoot("config.json")!;
        Console.WriteLine(configRef);


        var config = reader.Resolve(configRef);
        Console.WriteLine(config);
        Console.WriteLine(config.Match(new TestMatcher()));
        foreach (var key in ((JObj)config).Value) Console.WriteLine($"{key.Key} -> {key.Value}");

        var queryResult = reader.Query(configRef, "mutations[0].segments[-1].segment");
        Console.WriteLine($"{queryResult} == {String.Join(", ", ((JObj)queryResult).Value.Content().ToList())}");
    }

    private class TestMatcher : JsonMatcher<string>
    {
        public override string? OnObj(OneObjTable value)
        {
            return $"Object with fields {{ {string.Join(", ", value.ReadAll())} }}";
        }
    }
}