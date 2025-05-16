namespace SickSharp.Format.Tables
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
        Dbl = 7,
        Flt = 8,
        BigDec = 9,
        Str = 10,
        Arr = 11,
        Obj = 12,
        Root = 15
    }

    public record Ref(RefKind Kind, int Value);
}