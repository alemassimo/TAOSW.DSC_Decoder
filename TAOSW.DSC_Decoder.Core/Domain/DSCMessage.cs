using TAOSW.DSC_Decoder.Core.Domain;

public class DSCMessage
{
    public string? Frequency { get; set; }
    public List<int> Symbols { get; set; }
    public FormatSpecifier Format { get; set; }
    public CategoryOfCall Category { get; set; }
    public NatureOfDistress? Nature { get; set; }
    public string NatureDescription { get; set; }
    public string To { get; set; }
    public string From { get; set; }
    public FirstCommand TC1 { get; set; }
    public SecondCommand TC2 { get; set; }
    public string Position { get; set; }
    public string Time { get; set; }
    public EndOfSequence EOS { get; set; }
    public int CECC { get; set; }
    public string Status { get; set; }

    public DSCMessage()
    {
        Symbols = new List<int>();
    }

    public override string ToString()
    {
        return $"SYMB: {string.Join(" ", Symbols)}\n" +
               $"FMT: {Format}\n" +
               $"CAT: {Category}\n" +
               $"TO: {To}\n" +
               $"FROM: {From}\n" +
               $"TC1: {TC1}\n" +
               $"TC2: {TC2}\n" +
               $"FREQ: {Frequency}\n" +
               $"POS: {Position}\n" +
               $"EOS: {EOS}\n" +
               $"cECC: {CECC} {Status}";
    }
}

