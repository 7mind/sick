namespace SickSharp
{
    public enum RefKind : byte
    {
        Nul = 0,
        Bit = 1,
        SByte = 2,
        Short = 3,
        Int = 4,
        Lng = 5,
        BigInt = 6,
        Double = 7,
        Float = 8,
        BigDec = 9,
        String = 10,
        Array = 11,
        Object = 12,
        Root = 15
    }

    public record Ref(RefKind Kind, int Value);
}