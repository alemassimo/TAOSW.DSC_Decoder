using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private static DSCMessage DecodeAutomaticServiceCall(IEnumerable<int> symbols)
        {
            var From = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            throw new NotImplementedException();
        }

        private static DSCMessage DecodeGeographicAreaGroupCall(IEnumerable<int> symbols)
        {
            var From = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            throw new NotImplementedException();
        }

        private static DSCMessage DecodeIndividualStationCall(IEnumerable<int> symbols)
        {
            var To = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            throw new NotImplementedException();
        }

        private static DSCMessage DecodeGroupCall(IEnumerable<int> symbols)
        {
            var To = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            throw new NotImplementedException();
        }

        private static DSCMessage DecodeAllShipsCall(IEnumerable<int> symbols)
        {
            var To = "ALL SHIPS";
            throw new NotImplementedException();
        }

        private static DSCMessage DecodeDistressAlert(IEnumerable<int> symbols)
        {
            var From = ExtractMmsiNumber(symbols.Skip(2).Take(5));
            var nature = ExtractNaturOfDistress(symbols.ElementAt(8));
            throw new NotImplementedException();
        }

        private static FormatSpecifier GetFormatSpecifier(int symbol)=> GetEnumValue<FormatSpecifier>(symbol);

        private static TEnum GetEnumValue<TEnum>(int symbol) where TEnum : Enum
        {
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
                if (Convert.ToInt32(enumValue) == symbol) return enumValue;

            throw new ArgumentException("Invalid symbol", nameof(symbol));
        }

        
        private static string ExtractNaturOfDistress(int symbol) => GetEnumValue<NatureOfDistress>(symbol).ToString();
        private static string ExtractFirstCommand(int symbol) => GetEnumValue<FirstCommand>(symbol).ToString();
        private static string ExtractSecondCommand(int symbol) => GetEnumValue<SecondCommand>(symbol).ToString();
        private static string ExtractMmsiNumber(IEnumerable<int> symbols)
        {
            // il numero di simboli deve essere 5
            if (symbols.Count() != 5) throw new ArgumentException("Invalid number of symbols", nameof(symbols));

            StringBuilder mmsi = new StringBuilder();
            foreach (var symbol in symbols)
            {
                if (symbol == -1) mmsi.Append("__");
                mmsi.Append(Utils.IntTo2CharString(symbol));
            }

            // remove last char of string
            return mmsi.Remove(mmsi.Length - 1, 1).ToString();
        }

        private static string DecodeGeographicArea(List<int> input)
        {
            // check if input contain a -1 symbol and so is impossibile to decode
            if (input.Contains(-1)) return "--error--";

            if (input == null || input.Count != 5)
            {
                throw new ArgumentException("Input must be a list of exactly 5 integers (each representing 2 digits).");
            }

            // Unpack the 5 integers into 10 digits
            string digits = string.Join("", input.ConvertAll(n => n.ToString("D2")));

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
            {
                throw new ArgumentException("Input must be a list of exactly 6 integers.");
            }

            // Convert the list of integers to a string of digits
            string digits = string.Join("",  input.ConvertAll(n => n==-1 ? "__" : n.ToString("D2")));

            // Extract the two frequencies
            string frequency1 = digits.Substring(0, 5) + "." + digits.Substring(5, 1);
            string frequency2 = digits.Substring(6, 5) + "." + digits.Substring(11, 1);

            return $"{frequency1}/{frequency2}";
        }
    }
}
