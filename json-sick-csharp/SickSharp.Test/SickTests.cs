using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SickSharp.Format;
using SickSharp.Format.Tables;
using SickSharp.IO;
using Index = SickSharp.Encoder.Index;

namespace SickSharp.Test;

public class SickTests
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

        foreach (var file in _files)
        {
            var fi = new FileInfo(file);
            var name = Path.GetFileNameWithoutExtension(fi.Name);
            DoWrite(file, Path.Combine(PathOut, $"{name}-CS.bin"));
        }
    }

    [Test]
    public void Test1_Queries()
    {
        var input = Path.Join(PathOut, "petstore-with-external-docs-CS.bin");

        using (var reader = SickReader.OpenFile(input, ISickCacheManager.NoCache, ISickProfiler.Noop(),
                   loadInMemoryThreshold: 32768))
        {
            var root = reader.ReadRoot(RootName);

            var o1 = root.Query("info.version").AsString();
            Assert.That(o1, Is.EqualTo("1.0.0"));

            var o2 = root.Query("swagger").AsString();
            Assert.That(o2, Is.EqualTo("2.0"));

            var o3 = root.Query("schemes[0]").AsString();
            Assert.That(o3, Is.EqualTo("http"));

            var o4 = root.Query("schemes.[0]").AsString();
            Assert.That(o4, Is.EqualTo("http"));

            var o5 = root.Query("schemes.[-1]").AsString();
            Assert.That(o5, Is.EqualTo("http"));
        }
    }

    [Test]
    public void Test_Query_Benchmark()
    {
        var input = Path.Join(PathOut, "petstore-with-external-docs-CS.bin");

        using (var reader = SickReader.OpenFile(input, ISickCacheManager.NoCache, ISickProfiler.Noop(), loadInMemoryThreshold: 0))
        {
            for (int i = 0; i < 500000; i++)
            {
                var root = reader.ReadRoot(RootName);
                var o3 = root.Query("schemes[0]").AsString();
                Debug.Assert(o3 != null);
            }
        }
    }

    //
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


    public int Traverse(SickJson json, int count, short limit)
    {
        // Console.WriteLine(reader.ToJson(reference));

        var readFirst = false;
        var next = count + 1;
        if (count >= limit)
        {
            return count;
        }

        if (json is SickJson.Array arr)
        {
            if (arr.Count == 0)
            {
                return next;
            }

            SickRef entrySickRef;
            int index;
            if (readFirst)
            {
                index = 0;
                entrySickRef = arr.GetValues().First().Ref;
            }
            else
            {
                index = arr.Count / 2;
                entrySickRef = arr.GetValues().ElementAt(index).Ref;
            }

            var entry = arr.ReadIndex(index);
            Debug.Assert(entry.Ref == entrySickRef);
            return Traverse(entry, next, limit);
        }

        if (json is SickJson.Object obj)
        {
            if (obj.Count == 0)
            {
                return next;
            }

            KeyValuePair<string, SickRef> fieldRef;
            int index;
            if (readFirst)
            {
                index = 0;
                fieldRef = obj.GetReferences().First();
            }
            else
            {
                index = obj.Count / 2;
                fieldRef = obj.GetReferences().ElementAt(index);
            }

            var field = obj.Read(fieldRef.Key);
            Debug.Assert(field.Ref == fieldRef.Value);
            return Traverse(field, next, limit);
        }

        return count;
    }

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
    //         var entry = arr.Content().First();
    //         var entryRef = reader.ReadArrayElementRef(reference, 0);
    //         Debug.Assert(entry == entryRef);
    //         return Traverse(entryRef, reader, next, limit);
    //     }
    //
    //     if (reference.Kind == RefKind.Obj)
    //     {
    //         var obj = ((SickJson.Object)reader.Resolve(reference)).Value;
    //         if (obj.Count == 0)
    //         {
    //             return next;
    //         }
    //
    //         var firstEntry = obj.Content().First();
    //         var fieldVal = reader.ReadObjectFieldRef(reference, firstEntry.Key);
    //         Debug.Assert(fieldVal == firstEntry.Value);
    //         return Traverse(firstEntry.Value, reader, next, limit);
    //     }
    //
    //     return count;
    // }

    [Test]
    public void Test2_Read()
    {
        var inputs = Directory.EnumerateFiles(PathOut, "*.bin", SearchOption.TopDirectoryOnly).ToList();
        inputs.Sort();

        Assert.IsNotNull(inputs.Find(x => x.Contains("-CS")), "No file containing `-CS` found!");
        Assert.IsNotNull(inputs.Find(x => x.Contains("-SCALA")),
            "No file containing `-SCALA` found! Run Scala tests to generate");

        foreach (var input in inputs)
        {
            try
            {
                var fi = new FileInfo(input);
                var name = fi.Name;
                Console.WriteLine($"Processing {name} ({fi.Length} bytes)...");

                using (var reader = SickReader.OpenFile(input, ISickCacheManager.NoCache, ISickProfiler.Noop(),
                           loadInMemoryThreshold: 32768))
                {
                    var root = reader.ReadRoot(RootName);

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    Console.WriteLine($"Going to perform {iters} traverses...");
                    for (int x = 0; x < iters; x++)
                    {
                        Traverse(root, 0, 10);
                    }

                    stopwatch.Stop();
                    TimeSpan stopwatchElapsed = stopwatch.Elapsed;
                    Console.WriteLine($"Finished in {stopwatchElapsed.TotalSeconds} sec");
                    Console.WriteLine($"Iters/sec {Convert.ToDouble(iters) / stopwatchElapsed.TotalSeconds}");

                    Debug.Assert(root != null, $"No root entry in {name}");
                    Console.WriteLine($"{name}: found {RootName}, ref={root}");

                    switch (root)
                    {
                        case SickJson.Object obj:
                            Console.WriteLine($"{name}: object with {obj.Count} elements");
                            break;
                        default:
                            break;
                    }

                    Console.WriteLine();

#if SICK_DEBUG_TRAVEL
            Console.WriteLine($"Total travel = {SickReader.TotalTravel}, total index lookups = {SickReader.TotalLookups}, ratio = {Convert.ToDouble(SickReader.TotalTravel) / SickReader.TotalLookups}");
#endif
                }
            }
            catch
            {
                Console.WriteLine($"Failed on {input}");
                Console.WriteLine();
                throw;
            }
        }
    }

    public void DoWrite(string inPath, string outPath)
    {
        using var sr = new StreamReader(inPath);
        using var jreader = new JsonTextReader(sr);
        jreader.DateParseHandling = DateParseHandling.None;

        var loaded = JToken.Load(jreader);
        var index = Index.Create();
        var root = index.append(RootName, loaded);

        using (BinaryWriter binWriter = new BinaryWriter(File.Open(outPath, FileMode.Create)))
        {
            var data = index.Serialize().data;
            Console.WriteLine(
                $"Serialized with {index.Settings.BucketCount} buckets and {index.Settings.Limit} limit, size: {data.Length} bytes, source: {new FileInfo(inPath).Name}");
            binWriter.Write(data);
        }
    }
}