using System.Numerics;
using SickSharp.Decoder;
using SickSharp.Encoder;
using SickSharp.Format.Tables;

namespace SickSharp.Test;

public class BigDecimalTests
{
    private readonly BigDecEncoder _encoder = new();

    [Test]
    public void TestBigDecimalRoundTrip()
    {
        // 123.45
        var original = new BigDecimal(new BigInteger(12345), 2, 5, 1);

        AssertRoundTrip(original, "Simple decimal");

        var original1 = new BigDecimal(
            BigInteger.Parse("123456789012345678901234567890123456789012345678901234567890"),
            60, 60, 1);

        AssertRoundTrip(original1, "High precision decimal");

        var original2 = new BigDecimal(
            BigInteger.Parse(
                "31415926535897932384626433832795028841971693993751058209749445923078164062862089986280348253421170679"),
            100, 100, 1);

        AssertRoundTrip(original2, "Pi constant");

        var original3 = new BigDecimal(
            BigInteger.Parse(
                "27182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274"),
            98, 98, 1);

        AssertRoundTrip(original3, "E constant");

        // -2.71828
        var original4 = new BigDecimal(new BigInteger(271828), 5, 6, -1);

        AssertRoundTrip(original4, "Negative value");

        var original5 = new BigDecimal(BigInteger.Zero, 0, 1, 0);

        AssertRoundTrip(original5, "Zero value");

        // 1.23...e200
        var original6 = new BigDecimal(
            BigInteger.Parse("123456789012345678901234567890123456789012345678901234567890"),
            -140, 60, 1);

        AssertRoundTrip(original6, "Large scientific notation");
    }

    private void AssertRoundTrip(BigDecimal original, string testCase)
    {
        var encoded = _encoder.Bytes(original);
        Assert.That(encoded, Is.Not.Null, "Encoding should not return null");
        Assert.That(encoded.Length, Is.GreaterThan(12), "Encoded data should be longer than 3 Ints");

        var decoded = BigDecimalDecoder.Decode(encoded);

        Assert.That(decoded, Is.EqualTo(original));
    }
}