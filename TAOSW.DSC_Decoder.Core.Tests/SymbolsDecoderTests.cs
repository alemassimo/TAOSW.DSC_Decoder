using FluentAssertions;
using MathNet.Numerics.Providers.LinearAlgebra;
using MathNet.Numerics.RootFinding;
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

        

        //TIME: 2025-03-21 19:41:51 FREQ: 2187.5 DIST: 992 Km
        //SYMB: 102 102 004 040 003 005 008 108 000 022 075 040 000 109 126 002 018 020 002 018 020 127 049 127 127 
        // FMT: AREA
        // CAT: SAF
        //  TO: AREA 44°N=>05° 003°E=>08°
        //FROM: COAST,002275400,FRA,CROSS La Garde
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 02182.0/02182.0KHz
        // POS: -- 
        // EOS: EOS
        //cECC: 49 OK
        [TestMethod]
        public void DecodeAreaTest()
        {
            var symbols = new List<int> { 102, 102, 004, 040, 003, 005, 008, 108, 000, 022, 075, 040, 000, 109, 126, 002, 018, 020, 002, 018, 020, 127, 049, 127, 127 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.GeographicAreaGroupCall,
                Category = CategoryOfCall.Safety,
                To = "North-East (NE), Reference point: 44°, 3°, Vertical side: 5°, Horizontal side: 8°",
                From = "002275400",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                Frequency = "02182.0/02182.0",
                EOS = EndOfSequence.OtherCalls,
                CECC = 49,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-21 19:52:24 FREQ: 2187.5 DIST: 2121 Km
        //SYMB: 116 116 108 000 022 041 002 020 109 126 002 012 030 001 069 080 127 089 127 ~~~
        // FMT: ALL
        // CAT: SAF
        //FROM: COAST,002241022,ESP,Coruna Radio
        //  TO: ALL SHIPS
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 02123.0/01698.0KHz
        // POS: --
        // EOS: EOS
        //cECC: 89 OK
        [TestMethod]
        public void DecodeAllShipsCallTest2()
        {
            var symbols = new List<int> { 116, 116, 108, 000, 022, 041, 002, 020, 109, 126, 002, 012, 030, 001, 069, 080, 127, 089, 127, -1, -1, -1 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.AllShipsCall,
                Category = CategoryOfCall.Safety,
                To = "ALL SHIPS",
                From = "002241022",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                Frequency = "02123.0/01698.0",
                EOS = EndOfSequence.OtherCalls,
                CECC = 89,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-22 17:35:29 FREQ: 2187.5 DIST: -- Km
        //SYMB: 120 120 000 025 070 000 000 108 023 020 019 071 050 109 126 055 005 085 030 001 034 117 018 117 117 
        // FMT: SEL
        // CAT: SAF
        // TO: COAST,002570000,NOR,Public Correspondence
        //FROM: SHIP,232019715,???
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: --
        // POS: 58.53°N 001.34°E
        // EOS: REQ
        //cECC: 18 OK
        [TestMethod]
        public void DecodeRequestTest2()
        {
            var symbols = new List<int> { 120, 120, 000, 025, 070, 000, 000, 108, 023, 020, 019, 071, 050, 109, 126, 055, 005, 085, 030, 001, 034, 117, 018, 117, 117 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "002570000",
                From = "232019715",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                Position = "North-East (NE), Latitude: 058.53°, Longitude: 001.34°",
                EOS = EndOfSequence.AcknowledgeRQ,
                CECC = 18,
                Status = "OK",
                
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-21 21:07:12 FREQ: 2187.5 DIST: -- Km
        //SYMB: 120 120 000 024 070 000 020 108 000 024 070 012 040 109 126 002 018 020 002 018 020 117 066 117 117 
        // FMT: SEL
        // CAT: SAF
        //  TO: COAST,002470002,SCY,Palermo Radio
        //FROM: COAST,002470124, UNID
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 02182.0/02182.0KHz
        // POS: --
        // EOS: REQ
        //cECC: 66 OK
        [TestMethod]
        public void DecodeRequestTest3()
        {
            var symbols = new List<int> { 120, 120, 000, 024, 070, 000, 020, 108, 000, 024, 070, 012, 040, 109, 126, 002, 018, 020, 002, 018, 020, 117, 066, 117, 117 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "002470002",
                From = "002470124",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                Frequency = "02182.0/02182.0",
                EOS = EndOfSequence.AcknowledgeRQ,
                CECC = 66,
                Status = "OK"
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
                NatureDescription = "Position Requested",
                CECC = 81,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-22 16:08:47 FREQ: 2187.5 DIST: 1938 Km
        //SYMB: 102 102 006 000 003 008 014 108 000 021 091 000 000 109 126 001 073 040 002 007 080 127 030 127 127 
        // FMT: AREA
        // CAT: SAF
        //  TO: AREA 60°N=>08° 003°E=>14°
        //FROM: COAST,002191000,DNK,Lyngby Radio
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 01734.0/02078.0KHz
        // POS: -- 
        // EOS: EOS
        //cECC: 30 OK
        [TestMethod]
        public void DecodeAreaTest2()
        {
            var symbols = new List<int> { 102, 102, 006, 000, 003, 008, 014, 108, 000, 021, 091, 000, 000, 109, 126, 001, 073, 040, 002, 007, 080, 127, 030, 127, 127 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.GeographicAreaGroupCall,
                Category = CategoryOfCall.Safety,
                To = "North-East (NE), Reference point: 60°, 3°, Vertical side: 8°, Horizontal side: 14°",
                From = "002191000",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                Frequency = "01734.0/02078.0",
                EOS = EndOfSequence.OtherCalls,
                CECC = 30,
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
        public void DecodeJ3ETestMessageTest3()
        {
            var symbols = new List<int> { 120, 120, 000, 023, 071, 000, 004, 100, 023, 082, 030, 000, 000, 109, 126, 008, 041, 045, 126, 126, 126, 117, 007, 117, 117 };
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
        public void DecodeAllShipsCallTest3()
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

        //TIME: 2025-03-20 10:41:44 FREQ: 8414.5 DIST: -- Km
        //SYMB: 120 120 000 023 071 000 000 108 024 017 026 000 000 109 126 055 003 075 060 023 038 117 067 117 117 
        // FMT: SEL
        // CAT: SAF
        //  TO: COAST,002371000,GRC,Olympia Radio
        //FROM: SHIP,241726000,???
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: --
        // POS: 37.56°N 023.38°E
        // EOS: REQ
        //cECC: 67 OK
        [TestMethod]
        public void DecodeRequestTest4()
        {
            var symbols = new List<int> { 120, 120, 000, 023, 071, 000, 000, 108, 024, 017, 026, 000, 000, 109, 126, 055, 003, 075, 060, 023, 038, 117, 067, -1, 117, -1, -1 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "002371000",
                From = "241726000",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                Position = "North-East (NE), Latitude: 037.56°, Longitude: 023.38°",
                EOS = EndOfSequence.AcknowledgeRQ,
                CECC = 67,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-22 19:11:30 FREQ: 2187.5 DIST: -- Km
        //SYMB: 120 120 000 022 041 002 020 108 025 075 030 000 000 ~~~ 111 ~~~ ~~~ 126 126 ~~~ ~~~ ~~~ ~~~ 117 ~~~
        // FMT: SEL
        // CAT: SAF
        //  TO: COAST,002241022,ESP,Coruna Radio
        //FROM: SHIP,257530000,???
        // TC1: UNK/ERR
        // TC2: MEDICAL TRANSPORTS
        //FREQ: --
        // POS: --
        // EOS: REQ
        //cECC: 30 ERR
        [TestMethod]
        public void DecodeUnknownErrorTest2()
        {
            var symbols = new List<int> { 120, 120, 000, 022, 041, 002, 020, 108, 025, 075, 030, 000, 000, -1, 111, -1, -1, 126, 126, -1, -1, -1, -1, 117, -1 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "002241022",
                From = "257530000",
                TC1 = FirstCommand.Error,
                TC2 = SecondCommand.MedicalTransports,
                EOS = EndOfSequence.AcknowledgeRQ,
                CECC = -1,
                Status = "Error"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-22 19:33:43 FREQ: 2187.5 DIST: 2121 Km
        //SYMB: 116 116 108 000 022 041 002 020 109 126 002 013 020 001 070 070 127 071 127 127 
        // FMT: ALL
        // CAT: SAF
        //FROM: COAST,002241022,ESP,Coruna Radio
        //  TO: ALL SHIPS
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 02132.0/01707.0KHz
        // POS: --
        // EOS: EOS
        //cECC: 71 OK
        [TestMethod]
        public void DecodeAllShipsCallTest4()
        {
            var symbols = new List<int> { 116, 116, 108, 000, 022, 041, 002, 020, 109, 126, 002, 013, 020, 001, 070, 070, 127, 071, 127, 127 };
            var expected = new DSCMessage
            {
                Frequency = "02132.0/01707.0",
                Symbols = symbols,
                Format = FormatSpecifier.AllShipsCall,
                Category = CategoryOfCall.Safety,
                To = "ALL SHIPS",
                From = "002241022",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.OtherCalls,
                CECC = 71,
                Status = "OK"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-22 19:40:57 FREQ: 2187.5 DIST: 2182 Km
        //SYMB: 120 120 025 075 030 000 000 108 000 023 020 001 040 109 126 002 ~~~ 020 002 018 020 122 027 122 122 
        // FMT: SEL
        // CAT: SAF
        //  TO: SHIP,257530000,???
        //FROM: COAST,002320014,ENG,Falmouth Coastguard
        // TC1: J3E TP
        // TC2: NOINF
        //FREQ: 02~~2.0/02182.0KHz
        // POS: --
        // EOS: ACK
        //cECC: 27 ERR
        [TestMethod]
        public void DecodeAckTestMessageTest2()
        {
            var symbols = new List<int> { 120, 120, 025, 075, 030, 000, 000, 108, 000, 023, 020, 001, 040, 109, 126, 002, -1, 020, 002, 018, 020, 122, 027, 122, 122 };
            var expected = new DSCMessage
            {
                Frequency = "02__2.0/02182.0",
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "257530000",
                From = "002320014",
                TC1 = FirstCommand.J3ETP,
                TC2 = SecondCommand.NoInformation,
                EOS = EndOfSequence.AcknowledgeBQ,
                CECC = 27,
                Status = "Error"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }

        //TIME: 2025-03-22 19:48:50 FREQ: 2187.5 DIST: -- Km
        //SYMB: 120 120 024 091 ~~~ ~~~ 000 108 ~~~ ~~~ ~~~ ~~~ ~~~ 100 ~~~ 126 126 126 126 ~~~ 126 122 004 122 122 
        // FMT: SEL
        // CAT: SAF
        //  TO: SHIP,2491~~~~0,???
        //FROM: SHIP,~~~~~~~~~,???
        // TC1: F3E/G3E
        // TC2: UNK/ERR
        //FREQ: --
        // POS: --
        // EOS: ACK
        //cECC: 5 ERR
        [TestMethod]
        public void DecodeAckTestMessageTest3()
        {
            var symbols = new List<int> { 120, 120, 024, 091, -1, -1, 000, 108, -1, -1, -1, -1, -1, 100, -1, 126, 126, 126, 126, -1, 126, 122, 004, 122, 122 };
            var expected = new DSCMessage
            {
                Symbols = symbols,
                Format = FormatSpecifier.IndividualStationCall,
                Category = CategoryOfCall.Safety,
                To = "2491____0",
                From = "_________",
                TC1 = FirstCommand.AllModesTP,
                TC2 = SecondCommand.Error,
                EOS = EndOfSequence.AcknowledgeBQ,
                NatureDescription = "Position Requested",
                CECC = 4,
                Status = "Error"
            };
            var result = SymbolsDecoder.Decode(symbols);
            expected.Should().BeEquivalentTo(result);
        }


    }
}
