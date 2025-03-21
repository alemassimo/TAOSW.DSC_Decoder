using FluentAssertions;
using MathNet.Numerics.Providers.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TAOSW.DSC_Decoder.Core.Domain;
using static System.Net.Mime.MediaTypeNames;

namespace TAOSW.DSC_Decoder.Core.Tests
{
    //test class for SymbolsDecoder
    [TestClass]
    public class SymbolsDecoderTests
    {
        // test DistressAlert Message
        //FMT: DIS
        //CAT: DIS
        //FROM: SHIP,255805997,???
        // TC1: --
        // TC2: --
        //FREQ: --
        // POS: 45.26°N 013.07°E
        // EOS: EOS
        //cECC: 52 OK
        //DATA: TIME 12:52, POSITION 45.26°N 013.07°E, NATURE: Undesignated

        [TestMethod]
        public void DecodeDistressAlertTest()
        {
            var symbols = new List<int> { 112, 112, 025, 058, 005, 099, 070, 107, 004, 052, 060, 013, 007, 012, 052, 109, 127, 052, 127, 127 };
            var expected = new DSCMessage
            {
                Category = CategoryOfCall.Distress,
                Format = FormatSpecifier.DistressAlert,
                Nature = NatureOfDistress.UndesignatedDistress,
                From = "255805997",
                Position = "North-East (NE), Latitude: 045.26°, Longitude: 013.07°", // "45.26°N 013.07°E",
                EOS = EndOfSequence.OtherCalls,
                CECC = 52,
                Status = "OK",
                Time = "12:52",
                Symbols = symbols,
                To = "ALL SHIPS"

            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        [TestMethod]
        public void DecodeDistressAlertTestWithError()
        {
            var symbols = new List<int> { 112, 112, 025, 058, 005, 099, 070, 107, 004, 052, 060, 013, 007, 012, 052, 109, 127, 051, 127, 127 };
            var expected = new DSCMessage
            {
                Category = CategoryOfCall.Distress,
                Format = FormatSpecifier.DistressAlert,
                Nature = NatureOfDistress.UndesignatedDistress,
                From = "255805997",
                Position = "North-East (NE), Latitude: 045.26°, Longitude: 013.07°", // "45.26°N 013.07°E",
                EOS = EndOfSequence.OtherCalls,
                CECC = 51,
                Status = "Error",
                Time = "12:52",
                Symbols = symbols,
                To = "ALL SHIPS"

            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-19 15:13:23 FREQ: 8414.5 DIST: -- Km
        //SYMB: 120 120 000 023 071 000 000 108 032 051 042 000 000 118 126 038 075 000 038 075 000 117 000 117 117 
        // FMT: SEL
        // CAT: SAF
        //  TO: COAST,002371000,GRC,Olympia Radio
        //FROM: SHIP,325142000,???
        // TC1: TEST
        // TC2: NOINF
        //FREQ: --
        // POS: --
        // EOS: REQ
        //cECC: 0 OK
        [TestMethod]
        public void DecodeReqTestMessageTest()
        {
            var symbols = new List<int> { 120, 120, 000, 023, 071, 000, 000, 108, 032, 051, 042, 000, 000, 118, 126, 038, 075, 000, 038, 075, 000, 117, 000, 117, 117 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "002371000",
                From = "325142000",
                TC1 = FirstCommand.Test,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.AcknowledgeRQ,
                CECC = 0,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-19 15:15:25 FREQ: 8414.5 DIST: 618 Km
        //SYMB: 120 120 032 051 042 000 000 108 000 023 071 000 000 118 126 004 010 010 004 039 030 122 054 122 122 
        // FMT: SEL
        // CAT: SAF
        //  TO: SHIP,325142000,???
        //FROM: COAST,002371000,GRC,Olympia Radio
        // TC1: TEST
        // TC2: NOINF
        //FREQ: --
        // POS: --
        // EOS: ACK
        //cECC: 54 OK
        [TestMethod]
        public void DecodeAckTestMessageTest()
        {
            var symbols = new List<int> { 120, 120, 032, 051, 042, 000, 000, 108, 000, 023, 071, 000, 000, 118, 126, 004, 010, 010, 004, 039, 030, 122, 054, 122, 122, -1 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "325142000",
                From = "002371000",
                TC1 = FirstCommand.Test,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.AcknowledgeBQ,
                CECC = 54,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }


        //TIME: 2025-03-19 12:24:13 FREQ: 8414.5 DIST: 618 Km
        //SYMB: 120 120 034 018 055 000 000 100 000 023 071 000 000 109 126 004 010 010 004 039 030 122 027 122 122 
        // FMT: SEL
        // CAT: RTN
        //  TO: SHIP,341855000,???
        //FROM: COAST,002371000,GRC,Olympia Radio
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 04101.0/04393.0KHz
        // POS: --
        // EOS: ACK
        //cECC: 27 OK
        [TestMethod]
        public void DecodeJ3ETestMessageTest()
        {
            var symbols = new List<int> { 120, 120, 034, 018, 055, 000, 000, 100, 000, 023, 071, 000, 000, 109, 126, 004, 010, 010, 004, 039, 030, 122, 027, 122, 122 };
            var expected = new DSCMessage
            {
                Frequency = "04101.0/04393.0",
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Routine,
                To = "341855000",
                From = "002371000",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.AcknowledgeBQ,
                CECC = 27,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-20 10:46:18 FREQ: 8414.5 DIST: -- Km
        //SYMB: 120 120 000 023 071 000 004 100 023 082 030 000 000 109 126 008 041 045 126 126 126 117 007 117 117 
        // FMT: SEL
        // CAT: RTN
        //  TO: COAST,002371000,GRC,Olympia Radio
        //FROM: SHIP,238230000,???
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 08414.5KHz
        // POS: --
        // EOS: REQ
        //cECC: 7 OK
        [TestMethod]
        public void DecodeJ3ETestMessageTest2()
        {
            var symbols = new List<int> { 120, 120, 000, 023, 071, 000, 004, 100, 023, 082, 030, 000, 000, 109, 126, 008, 041, 045, 126, 126, 126, 117, 007, 117, 117, -1, -1, -1 };
            var expected = new DSCMessage
            {
                Frequency = "08414.5",
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Routine,
                To = "002371000",
                From = "238230000",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.AcknowledgeRQ,
                CECC = 7,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-20 09:58:53 FREQ: 8414.5 DIST: 618 Km
        //SYMB: 116 116 108 000 023 071 000 000 109 126 004 012 050 004 012 050 127 036 127 127 
        // FMT: ALL
        // CAT: SAF
        //FROM: COAST,002371000,GRC,Olympia Radio
        //  TO: ALL SHIPS
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 04125.0/04125.0KHz
        // POS: --
        // EOS: EOS
        //cECC: 36 OK
        [TestMethod]
        public void DecodeAllShipsCallTest()
        {
            var symbols = new List<int> { 116, 116, 108, 000, 023, 071, 000, 000, 109, 126, 004, 012, 050, 004, 012, 050, 127, 036, 127, 127 };
            var expected = new DSCMessage
            {
                Frequency = "04125.0/04125.0",
                Symbols = symbols,
                Format = FormatSpecifier.AllShipsCall,
                Category = CategoryOfCall.Safety,
                To = "ALL SHIPS",
                From = "002371000",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.OtherCalls,
                CECC = 36,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-19 10:48:55 FREQ: 8414.5 DIST: -- Km
        //SYMB: 120 120 000 021 050 010 000 108 022 093 064 000 000 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1
        // FMT: SEL
        // CAT: SAF
        //  TO: COAST,002150100,MLT,Malta Radio
        //FROM: SHIP,229364000,???
        // TC1: UNK/ERR
        // TC2: UNK/ERR
        //FREQ: --/--
        // POS: --
        // EOS: ~~~
        //cECC: 50 ERR
        [TestMethod]
        public void DecodeUnknownErrorTest()
        {
            var symbols = new List<int> { 120, 120, 000, 021, 050, 010, 000, 108, 022, 093, 064, 000, 000, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "002150100",
                From = "229364000",
                TC1 = FirstCommand.Error,
                TC2 = SecondCommand.Error,
                EOS = EndOfSequence.Error,
                CECC = -1,
                Status = "Error"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-21 14:06:08 FREQ: 8414.5 DIST: 1036 Km
        //SYMB: 120 120 051 089 099 019 050 100 000 027 011 000 000 126 126 126 126 126 126 126 126 117 081 117 117 
        // FMT: SEL
        // CAT: RTN
        //  TO: SHIP,518999195,???
        //FROM: COAST,002711000,TUR,Istanbul Radio
        // TC1: NOINF
        // TC2: NOINF
        //FREQ: --
        // POS: --
        // EOS: REQ
        //cECC: 81 OK
        [TestMethod]
        public void DecodeRequestTest()
        {
            var symbols = new List<int> { 120, 120, 051, 089, 099, 019, 050, 100, 000, 027, 011, 000, 000, 126, 126, 126, 126, 126, 126, 126, 126, 117, 081, 117, 117 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Routine,
                To = "518999195",
                From = "002711000",
                TC1 = FirstCommand.NoInformation,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.AcknowledgeRQ,
                CECC = 81,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

    }
}
