using System.Text;
using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.Core
{
    public class SymbolsDecoder
    {
        public static DSCMessage Decode(IEnumerable<int> symbols)
        {
            // if first symbol is -1, then the format specifier is the second symbol
            var format = symbols.First() != -1 ? symbols.First() : symbols.ElementAt(1);
            var formatType = GetFormatSpecifier(format);
            try
            {
                return formatType switch
                {
                    FormatSpecifier.DistressAlert => DecodeDistressAlert(symbols),
                    FormatSpecifier.AllShipsCall => DecodeAllShipsCall(symbols),
                    FormatSpecifier.GroupCall => DecodeGroupCall(symbols),
                    FormatSpecifier.IndividualStationCall => DecodeIndividualStationCall(symbols),
                    FormatSpecifier.GeographicAreaGroupCall => DecodeGeographicAreaGroupCall(symbols),
                    FormatSpecifier.AutomaticServiceCall => DecodeAutomaticServiceCall(symbols),
                    _ => throw new ArgumentException("Invalid format specifier", nameof(symbols))
                }; 
            }
            // catch all ArgumentException and NotImplementedException and return a DSCMessage with the error message
            catch ( Exception ex) when (ex is ArgumentException || ex is NotImplementedException)
            {
                return new DSCMessage
                {
                    Symbols = symbols.ToList(),
                    Format = formatType,
                    Status = $"Error -> {ex.Message}"
                };
            }
            

        }

        private static DSCMessage DecodeAutomaticServiceCall(IEnumerable<int> symbols)
        {
            var From = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            throw new NotImplementedException("AutomaticServiceCall format not implemented");
        }

        private static DSCMessage DecodeGeographicAreaGroupCall(IEnumerable<int> symbols)
        {
            var To = ExtractGeographicArea(symbols.Skip(2).Take(5));
            var category = ExtractCategoryOfCall(symbols.ElementAt(7));
            var From = ExtractMmsiNumber(symbols.Skip(8).Take(5));
            var TC1 = ExtractFirstCommand(symbols.ElementAt(13));
            var TC2 = ExtractSecondCommand(symbols.ElementAt(14));
            var freq = ExtractFrequencies(symbols.Skip(15).Take(6).ToList());
            var eos = ExtractEos(symbols.Skip(21).Take(4));
            var ecc = symbols.ElementAt(22);
            return new DSCMessage
            {
                Symbols = symbols.ToList(),
                Format = FormatSpecifier.GeographicAreaGroupCall,
                Category = category,
                To = To,
                From = From,
                TC1 = TC1,
                TC2 = TC2,
                EOS = eos,
                CECC = ecc,
                Frequency = TC1 == FirstCommand.J3ETP ? freq : null,
                Status = CheckEcc(symbols, 22) ? "OK" : "Error"
            };
        }

        private static DSCMessage DecodeGroupCall(IEnumerable<int> symbols)
        {
            var To = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            throw new NotImplementedException("GroupCall format not implemented.");
        }

        private static DSCMessage DecodeAllShipsCall(IEnumerable<int> symbols)
        {
            var To = "ALL SHIPS";
            var category = ExtractCategoryOfCall(symbols.ElementAt(2));
            var From = ExtractMmsiNumber(symbols.Skip(3).Take(5));
            var TC1 = ExtractFirstCommand(symbols.ElementAt(8));
            var TC2 = ExtractSecondCommand(symbols.ElementAt(9));
            var freq = ExtractFrequencies(symbols.Skip(10).Take(6).ToList());
            var eos = ExtractEos(symbols.Skip(16).Take(4));
            var ecc = symbols.ElementAt(17);
            return new DSCMessage
            {
                Symbols = symbols.ToList(),
                Format = FormatSpecifier.AllShipsCall,
                Category = category,
                To = To,
                From = From,
                TC1 = TC1,
                TC2 = TC2,
                EOS = eos,
                CECC = ecc,
                Frequency = TC1 == FirstCommand.J3ETP ? freq : null,
                Status = CheckEcc(symbols, 17) ? "OK" : "Error"
            };
        }

        private static DSCMessage DecodeIndividualStationCall(IEnumerable<int> symbols)
        {
            var To = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            var category = ExtractCategoryOfCall(symbols.ElementAt(7));
            var From = ExtractMmsiNumber(symbols.Skip(8).Take(5));
            var TC1 = ExtractFirstCommand(symbols.ElementAt(13));
            var TC2 = ExtractSecondCommand(symbols.ElementAt(14));
            string? freq = null;
            string? position = null;
            string? description = null;
            switch (symbols.ElementAt(15))
            {
                case 55:
                    position = ExtractPosition(symbols.Skip(16).Take(5));
                    break;
                case 126:
                    description = "Position Requested";
                    break;
                default:
                    freq = ExtractFrequencies(symbols.Skip(15).Take(6).ToList());
                    break;
            }

            var eos = ExtractEos(symbols.Skip(21).Take(4));
            var ecc = symbols.ElementAt(22);
            return new DSCMessage
            {
                Symbols = symbols.ToList(),
                Format = FormatSpecifier.IndividualStationCall,
                Category = category,
                To = To,
                From = From,
                TC1 = TC1,
                TC2 = TC2,
                EOS = eos,
                CECC = ecc,
                Frequency = TC1 == FirstCommand.J3ETP ? freq : null,
                Position = position,
                Status = CheckEcc(symbols, 22) ? "OK" : "Error",
                NatureDescription = description
            };
        }

        private static CategoryOfCall ExtractCategoryOfCall(int symbol)=> GetEnumValue<CategoryOfCall>(symbol);
        
        private static DSCMessage DecodeDistressAlert(IEnumerable<int> symbols)
        {
            var From = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            var nature = ExtractNaturOfDistress(symbols.ElementAt(7));
            var position = ExtractPosition(symbols.Skip(8).Take(5));
            var time = ExtractTime(symbols.Skip(13).Take(2));
            var eos = ExtractEos(symbols.Skip(16).Take(4)) ;
            var ecc = symbols.ElementAt(17);
            var status = CheckEcc(symbols, 17)  ? "OK" : "Error";

            return new DSCMessage
            {
                Symbols = symbols.ToList(),
                Format = FormatSpecifier.DistressAlert,
                Category = CategoryOfCall.Distress,
                Nature = nature,
                To = "ALL SHIPS",
                From = From,
                Position = position,
                Time = time,
                EOS = eos,
                CECC = ecc,
                Status = status
            };
        }

        //The seven information bits of the ECC shall be equal to the least significant bit of
        //the modulo-2 sums of the corresponding bits of all information characters(i.e.even vertical parity). 
        //The format specifier and the EOS characters are considered to be information characters.The phasing
        //characters and the retransmission(RX) characters shall not be considered to be information
        //characters.Only one format specifier character and one EOS character should be used in constructing
        //the ECC.The ECC shall also be sent in the DX and RX positions.
        private static bool CheckEcc(IEnumerable<int> symbols, int eccPosition)
        {
            var ecc = symbols.ElementAt(eccPosition);
            var info = new List<int>();
            for (int i = 0; i <= eccPosition; i++)
            {
                info.Add(symbols.ElementAt(i));
            }
            return ValidateECC(info);
        }

        public static bool ValidateECC(List<int> data)
        {
            int ecc = data[^1]; // Ultimo elemento
            List<int> infoChars = data.Skip(1).Take(data.Count()-2).ToList();

            // Calcola la parità verticale per i 7 bit meno significativi (bit 0-6)
            int[] parity = new int[7];
            foreach (var ch in infoChars)
            {
                for (int bit = 0; bit < 7; bit++)
                {
                    int bitValue = (ch >> bit) & 1;
                    parity[bit] ^= bitValue; // modulo-2 sum = XOR
                }
            }

            // Costruisci i 7 bit in un numero
            int calculatedECC = 0;
            for (int bit = 0; bit < 7; bit++)
            {
                calculatedECC |= (parity[bit] << bit);
            }

            // Confronta solo i 7 bit meno significativi
            return (ecc & 0b01111111) == calculatedECC;
        }

        private static EndOfSequence ExtractEos(IEnumerable<int> symbols)
        {
            if (symbols == null || symbols.Count() != 4)
            {
                throw new ArgumentException("Input must be a list of exactly 4 integers (each representing 2 digits).");
            }
            // get position 1,2,4 of the symbols
            var symbol1 = symbols.ElementAt(0);
            var symbol3 = symbols.ElementAt(2);
            var symbol4 = symbols.ElementAt(3);

            // get symbol != -1
            var symbol = symbol1 != -1 ? symbol1 : symbol3 != -1 ? symbol3 : symbol4;

            return GetEnumValue<EndOfSequence>(symbol);
        }

        private static FormatSpecifier GetFormatSpecifier(int symbol)=> 
            GetEnumValue<FormatSpecifier>(symbol);

        private static TEnum GetEnumValue<TEnum>(int symbol) where TEnum : Enum
        {
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
                if (Convert.ToInt32(enumValue) == symbol) return enumValue;

            return GetEnumValueWithException<TEnum>(-1);
        }

        private static TEnum GetEnumValueWithException<TEnum>(int symbol) where TEnum : Enum
        {
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
                if (Convert.ToInt32(enumValue) == symbol) return enumValue;

            throw new ArgumentException("Invalid symbol", nameof(symbol));
        }

        private static NatureOfDistress ExtractNaturOfDistress(int symbol) => GetEnumValue<NatureOfDistress>(symbol);
        private static FirstCommand ExtractFirstCommand(int symbol) => GetEnumValue<FirstCommand>(symbol);
        private static SecondCommand ExtractSecondCommand(int symbol) => GetEnumValue<SecondCommand>(symbol);
        private static string ExtractMmsiNumber(IEnumerable<int> symbols)
        {
            if (symbols.Count() != 5) throw new ArgumentException("Invalid number of symbols", nameof(symbols));

            StringBuilder mmsi = new StringBuilder();
            foreach (var symbol in symbols)
            {
                if (symbol == -1) mmsi.Append("__");
                else
                mmsi.Append(Utils.IntTo2CharString(symbol));
            }

            // remove last char of string
            return mmsi.Remove(mmsi.Length - 1, 1).ToString();
        }

        private static string ExtractTime(IEnumerable<int> input)
        {
            if (input == null || input.Count() != 2)
                throw new ArgumentException("Input must be a list of exactly 2 integers (each representing 2 digits).");
            
            string digits = string.Join("", input.ToList().ConvertAll(n => n.ToString("D2")));

            var hh = digits.Substring(0, 2) == "-1" ? "__" : digits.Substring(0, 2);
            var mm = digits.Substring(2, 2) == "-1" ? "__" : digits.Substring(2, 2);
            return $"{hh}:{mm}";
        }

        private static string ExtractPosition(IEnumerable<int> input)
        {
            if (input.Contains(-1)) return "--error--";
            if (input == null || input.Count() != 5)
            {
                throw new ArgumentException("Input must be a list of exactly 5 integers (each representing 2 digits).");
            }
            string digits = string.Join("", input.ToList().ConvertAll(n => n.ToString("D2")));

            int quadrant = int.Parse(digits.Substring(0, 1));
            string quadrantName = quadrant switch
            {
                0 => "North-East (NE)",
                1 => "North-West (NW)",
                2 => "South-East (SE)",
                3 => "South-West (SW)",
                _ => "Unknown quadrant"
            };

            var latitude = digits.Substring(0, 3)+"." + digits.Substring(3, 2);
            var longitude = digits.Substring(5, 3) + "." + digits.Substring(8, 2);

            return $"{quadrantName}, Latitude: {latitude}°, Longitude: {longitude}°";
        }

        private static string ExtractGeographicArea(IEnumerable<int> input)
        {
            if (input.Contains(-1)) return "--error--";

            if (input == null || input.Count() != 5)
            {
                throw new ArgumentException("Input must be a list of exactly 7 integers (each representing 2 digits).");
            }

            // Unpack the 5 integers into 10 digits
            string digits = string.Join("", input.ToList().ConvertAll(n => n.ToString("D2")));

            int quadrant = int.Parse(digits.Substring(0, 1));
            string quadrantName = quadrant switch
            {
                0 => "North-East (NE)",
                1 => "North-West (NW)",
                2 => "South-East (SE)",
                3 => "South-West (SW)",
                _ => "Unknown quadrant"
            };

            int latitude = int.Parse(digits.Substring(1, 2));
            int longitude = int.Parse(digits.Substring(3, 3));
            int verticalSide = int.Parse(digits.Substring(6, 2));
            int horizontalSide = int.Parse(digits.Substring(8, 2));

#if DEBUG
            Console.WriteLine($"Quadrant: {quadrantName}");
            Console.WriteLine($"Reference point latitude: {latitude}°");
            Console.WriteLine($"Reference point longitude: {longitude}°");
            Console.WriteLine($"Vertical side of rectangle (North-South): {verticalSide}°");
            Console.WriteLine($"Horizontal side of rectangle (West-East): {horizontalSide}°");
#endif      
            return $"{quadrantName}, Reference point: {latitude}°, {longitude}°, Vertical side: {verticalSide}°, Horizontal side: {horizontalSide}°";
        }

        private static string ExtractFrequencies(List<int> input)
        {
            if (input == null || input.Count != 6) 
                throw new ArgumentException("Input must be a list of exactly 6 integers.");
            
            // Convert the list of integers to a string of digits
            string digits = string.Join("",  input.ConvertAll(n => n==-1 ? "__" : n.ToString("D2")));

            // Extract the two frequencies
            string frequency1 = digits.Substring(0, 5) + "." + digits.Substring(5, 1);

            // check if second frequency exists
            string frequency2 = input.Skip(3).All(n => n > 99) ? "" : digits.Substring(6, 5) + "." + digits.Substring(11, 1);

            return String.IsNullOrEmpty( frequency2) ? frequency1 : $"{frequency1}/{frequency2}";
        }
    }
}
