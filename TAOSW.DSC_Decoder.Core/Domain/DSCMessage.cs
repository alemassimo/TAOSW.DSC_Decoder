// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using TAOSW.DSC_Decoder.Core.Domain;

public class DSCMessage
{
    public string? Frequency { get; set; }
    public List<int> Symbols { get; set; }
    public FormatSpecifier Format { get; set; }
    public CategoryOfCall Category { get; set; }
    public NatureOfDistress? Nature { get; set; }
    public string? NatureDescription { get; set; }
    public string To { get; set; }
    public string From { get; set; }
    public FirstCommand TC1 { get; set; }
    public SecondCommand TC2 { get; set; }
    public string? Position { get; set; }
    public DateTimeOffset? Time { get; set; }
    public EndOfSequence EOS { get; set; }
    public int CECC { get; set; }
    public string Status { get; set; }

    public DSCMessage()
    {
        Symbols = [];
    }

    public override string ToString()
    {
        return $"Symbols: {string.Join(" ", Symbols)}\n" +
               $"Format: {Format}\n" +
               $"Category: {Category}\n" +
               $"To: {To}\n" +
               $"From: {From}\n" +
               $"TC1: {TC1}\n" +
               $"TC2: {TC2}\n" +
               $"Frequency: {Frequency}\n" +
               $"Position: {Position}\n" +
               $"EOS: {EOS}\n" +
               $"Nature: {Nature}\n" +
               $"NatureDescription: {NatureDescription}\n" +
               $"Time: {Time}\n" +
               $"Status: {Status}\n" +
               $"cECC: {CECC}";
    }
}

