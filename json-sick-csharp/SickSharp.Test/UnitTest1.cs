using System.Diagnostics;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SickSharp.Encoder;
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
        
        var loaded = JToken.Load(new JsonTextReader(new StreamReader(path)));
        var index = Index.Create();
        var root = index.append("config.json", loaded);
        Console.WriteLine(root);
        
        using (BinaryWriter binWriter =  
               new BinaryWriter(File.Open(outPath, FileMode.Create)))  
        {  
            binWriter.Write(index.Serialize().data);  
        }

        using (var stream = File.Open(outPath, FileMode.Open))
        {
            var reader = new SickReader(stream);
            Console.WriteLine(reader.Header);
            var configRef = reader.GetRoot("config.json")!;
            Console.WriteLine(configRef);
        }
    }
    
    [Test]
    public void Test1()
    {
        var path = "../../../../../json-sick-scala/output.bin";
        using (var stream = File.Open(path, FileMode.Open))
        {
            var reader = new SickReader(stream);
            Console.WriteLine(reader.Header);

            // var firstArr = reader.Arrs.Read(0);
            // Console.WriteLine(reader.Arrs.Count);
            // for (var i = 0; i < firstArr.Count; i++) Console.WriteLine($"{i} == {firstArr.Read(i)}");
            //
            // var firstObj = reader.Objs.Read(0);
            // Console.WriteLine(reader.Objs.Count);
            // for (var i = 0; i < firstObj.Count; i++) Console.WriteLine($"{i} == {firstObj.Read(i)}");

            // for (int i = 0; i < 10 /*reader.Strings.Count*/; i++)
            // {
            //     Console.WriteLine($"{i} == {reader.Strings.Read(i)}");
            // }
            // Console.WriteLine($"last == {reader.Strings.Read(reader.Strings.Count-1)}");
            
            Debug.Assert(reader.Bytes.Count == 59);
            Debug.Assert(reader.Shorts.Count == 2042);
            Debug.Assert(reader.Ints.Count == 8);
            Debug.Assert(reader.Longs.Count == 0);

            Debug.Assert(reader.Floats.Count == 5);
            Debug.Assert(reader.Doubles.Count == 0);

            var configRef = reader.GetRoot("config.json")!;
            Console.WriteLine(configRef);

                
            var config = reader.Resolve(configRef);
            Console.WriteLine(config);
            Console.WriteLine(config.Match(new TestMatcher()));
            foreach (var key in ((JObj)config).Value) Console.WriteLine($"{key.Key} -> {key.Value}");

            var queryResult = reader.Query(configRef, "mutations[0].segments[-1].segment");
            Console.WriteLine($"{queryResult} == {String.Join(", ", ((JObj)queryResult).Value.Content().ToList())}");
        }
        
        //Assert.Pass();
    }
    
    private class TestMatcher : JsonMatcher<string>
    {
        public override string? OnObj(OneObjTable value)
        {
            return $"Object with fields {{ {string.Join(", ", value.ReadAll())} }}";
        }
    }
}