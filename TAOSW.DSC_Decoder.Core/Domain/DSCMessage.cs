public class DSCMessage
{
    public DateTime Time { get; set; }
    public double Frequency { get; set; }
    public string Distance { get; set; }
    public List<int> Symbols { get; set; }
    public string Format { get; set; }
    public string Category { get; set; }
    public string To { get; set; }
    public string From { get; set; }
    public string TC1 { get; set; }
    public string TC2 { get; set; }
    public string Position { get; set; }
    public string EOS { get; set; }
    public int CECC { get; set; }
    public string Status { get; set; }

    public DSCMessage()
    {
        Symbols = new List<int>();
    }

    public override string ToString()
    {
        return $"TIME: {Time} FREQ: {Frequency} DIST: {Distance}\n" +
               $"SYMB: {string.Join(" ", Symbols)}\n" +
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

