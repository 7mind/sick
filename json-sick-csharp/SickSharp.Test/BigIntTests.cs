using System.Numerics;
using SickSharp.Encoder;
using SickSharp.Decoder;

namespace SickSharp.Test;

public class BigIntTests
{
    private readonly BigIntEncoder _encoder = new();

    [Test]
    public void TestBigIntRoundTrip()
    {
        var original = BigInteger.Zero;
        AssertRoundTrip(original, "Zero value");

        var original1 = new BigInteger(12345);
        AssertRoundTrip(original1, "Simple positive value");

        var original2 = new BigInteger(-54321);
        AssertRoundTrip(original2, "Simple negative value");

        var original3 = BigInteger.Parse("123456789012345678901234567890");
        AssertRoundTrip(original3, "Large positive value");

        var original4 = BigInteger.Parse("-987654321098765432109876543210");
        AssertRoundTrip(original4, "Large negative value");

        var original5 = BigInteger.Parse("12345678901234567890123456789012345678901234567890123456789012345678901234567890");
        AssertRoundTrip(original5, "Very large value");

        var original6 = BigInteger.Pow(2, 100);
        AssertRoundTrip(original6, "Power of 2 (2^100)");
    }

    [Test]
    public void TestJavaCompat()
    {
        var testCases = new[]
        {
            (value: "100", expectedBytes: new byte[] { 0x64 }),
            (value: "-100", expectedBytes: [0x9C]),
            (value: "-10000000000", expectedBytes: [0xFD, 0xAB, 0xF4, 0x1C, 0x00]),
            (value: "10000000000", expectedBytes: [0x02, 0x54, 0x0B, 0xE4, 0x00]),
            (value: "10000000", expectedBytes: [0x00, 0x98, 0x96, 0x80]),
            (value: "-10000000", expectedBytes: [0xFF, 0x67, 0x69, 0x80])
        };

        foreach (var (value, expectedBytes) in testCases)
        {
            var original = BigInteger.Parse(value);

            var encoded = _encoder.Bytes(original);

            Assert.That(encoded, Is.EqualTo(expectedBytes),
                $"C# BigInteger.ToByteArray() for {value} should match Java format");

            var decoded = BigIntDecoder.Decode(encoded);
            Assert.That(decoded, Is.EqualTo(original), $"Round-trip failed for {value}");
        }
    }

    private void AssertRoundTrip(BigInteger original, string testCase)
    {
        var encoded = _encoder.Bytes(original);
        var decoded = BigIntDecoder.Decode(encoded);
        Assert.That(decoded, Is.EqualTo(original), $"Round-trip failed for {testCase}");
    }
}
