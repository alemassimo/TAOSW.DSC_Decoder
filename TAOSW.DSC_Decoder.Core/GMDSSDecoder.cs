// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

namespace TAOSW.DSC_Decoder.Core
{
    public class GMDSSDecoder
    {
        private Queue<int> bitBuffer = new Queue<int>();
        private const int BUFFER_SIZE = 1024;
        private const int MESSAGE_LENGTH = 900;
        private const int ERROR_THRESHOLD = 75;
        private readonly int[] RX_PHASE_SEQUENCE = [111, 110, 109, 108, 107, 106, 105, 104];
        private readonly int[] DX_PHASE_SEQUENCE = [125, 125, 125, 125, 125, 125, 125];

        public event Action<DSCMessage>? OnMessageDecoded;

        public void AddBits(IEnumerable<int> bits)
        {
            foreach (var bit in bits)
            {
                if (bitBuffer.Count >= BUFFER_SIZE)
                    bitBuffer.Dequeue();
                bitBuffer.Enqueue(bit);
            }

            TryProcessBuffer();
        }

        private bool ProcessSubSequence(int[] bitArray, int start, int length)
        {
            int errors = 0;
            int end = start + length;
            var dataOut = new List<int>((length + 9) / 10);
            for (int i = start; i + 10 < end; i += 10)
            {
                var data = GetDataByte(bitArray, i);
                bool check = CheckParity(data, bitArray, i + 7);
                if (!check)
                {
                    dataOut.Add(-1);
                    errors++;

                    if (errors > ERROR_THRESHOLD) return false;
                }
                else
                {
                    dataOut.Add(data);
                }
            }

            var processed = false;
            if (IsPhased(dataOut, 0))
            {
                Console.WriteLine($"IsPhased.");
                dataOut.ForEach(@byte =>
                {
                    switch (@byte)
                    {
                        case -1:
                            Console.Write($"e ");
                            break;
                        default:
                            Console.Write($"{@byte} ");
                            break;
                    }
                });
                ProcessMessage(dataOut);
                processed = true;
            }

            return processed;
        }

        private void TryProcessBuffer()
        {
            var bitArray = bitBuffer.ToArray();
            if (bitArray.Length < MESSAGE_LENGTH) return;

            for (int i = 0; i < 10; i++)
            {
                var preambleLength = GetPreambleLength(bitArray, i);
                int length = Math.Min(MESSAGE_LENGTH + preambleLength, bitArray.Length - i);
                ProcessSubSequence(bitArray, i, length);

                if (bitBuffer.Count > 0) bitBuffer.Dequeue();
            }
        }

        public static bool IsPreambleGroup(int[] bits, int start)
        {
            if (start < 0 || start > bits.Length - 10) return false;

            int firstBit = bits[start];
            if (firstBit is not (0 or 1)) return false;

            for (int i = 1; i < 10; i++)
            {
                if (bits[start + i] != (firstBit ^ (i & 1))) return false;
            }

            return true;
        }

        private bool IsPhased(IReadOnlyList<int> bytes, int start)
        {
            var dxMatchs = 0;
            var rxMatchs = 0;
            var cpd = 0;
            foreach (int val in DX_PHASE_SEQUENCE)
            {

                if (val == bytes[start + cpd]) dxMatchs++;
                cpd += 2;
            }

            var cpr = 1;
            foreach (int val in RX_PHASE_SEQUENCE)
            {

                if (val == bytes[start + cpr]) rxMatchs++;
                cpr += 2;
            }
            var totalMatchs = dxMatchs + rxMatchs;
            if (totalMatchs >= 3 && rxMatchs != 0)
            {
                Console.WriteLine($"dx: {dxMatchs} rx: {rxMatchs}");
                return true;
            }
            return false;
        }
        private int GetPreambleLength(int[] bits, int start)
        {
            int length = 0;
            while (start + length + 10 <= bits.Length && IsPreambleGroup(bits, start + length))
            {
                length += 10;
            }
            return length;
        }


        private void ProcessMessage(List<int> dataBytes)
        {
            var symbols = GMDSSDecoderHelper.ExtractSymbolsFromMessage(dataBytes);
            Console.WriteLine("Symbols: ");
            foreach (var item in symbols)
            {
                Console.Write($" {item}");
            }
            Console.WriteLine();
            var message = ParseDSCMessage(symbols.ToList());
            if (message is not null) OnMessageDecoded?.Invoke(message);

        }

        private List<int> ExtractDataBytes(List<int> bits)
        {
            List<int> bytes = new List<int>();
            for (int i = 0; i < bits.Count; i += 10)
            {
                try
                {
                    int dataByte = RetriveDataByte(bits, i);
                    Console.Write($"-");
                    foreach (var item in bits.Skip(i + 7).Take(3)) Console.Write(item);
                    

                    bool check = CheckParity(dataByte, bits.Skip(i + 7).Take(3).ToArray());
                    Console.Write($" check: {check}");
                    Console.Write($" data: {dataByte}");
                    bytes.Add(dataByte);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore in ExtractDataBytes: {ex.Message}");
                }
            }
            return bytes;
        }

        public static int RetriveDataByte(IEnumerable<int> bits, int i)
        {
            if (bits is IReadOnlyList<int> indexedBits)
                return GetDataByte(indexedBits, i);

            return GetDataByte(bits.ToArray(), i);
        }

        private static int GetDataByte(IReadOnlyList<int> bits, int i)
        {
            int dataByte = 0;
            for (int j = 0; j < 7; j++) dataByte |= bits[i + j] << j;
            return dataByte;
        }

        private bool CheckParity(int value, int[] parityBits, int start = 0)
        {
            try
            {
                int computedParity = ComputeParity(value);
                int receivedParity = parityBits[start] << 2 | parityBits[start + 1] << 1 | parityBits[start + 2];
                return computedParity == receivedParity;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private int ComputeParity(int value)
        {
            int zeroCount = 0;
            for (int i = 0; i < 7; i++)
            {
                if ((value >> i & 1) == 0)
                    zeroCount++;
            }
            return zeroCount;
        }

        private DSCMessage? ParseDSCMessage(List<int> data)
        {
            try
            {
                return SymbolsDecoder.Decode(data);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"Errore in ParseDSCMessage: {ex.Message}");
#endif
                throw;
            }
        }
    }

    public class GMDSSDecoderHelper
    {

        public static IEnumerable<int> ExtractSymbolsFromMessage(IEnumerable<int> byteStream)
        {
            List<int> symbols = new List<int>();
            List<int> dxStream = new List<int>();
            List<int> rxStream = new List<int>();

            int cursor = 0;
            foreach (var _byte in byteStream)
            {
                if (cursor % 2 == 0) dxStream.Add(_byte); else rxStream.Add(_byte);
                cursor++;
            }

            int dxCursor = 6;
            foreach (var _dx in dxStream.Skip(6))
            {

                if (_dx != -1) symbols.Add(_dx);
                else if (rxStream.Count > dxCursor + 2) symbols.Add(rxStream[dxCursor + 2]);
                else symbols.Add(-1);

                dxCursor++;
            }

            return symbols;
        }


    }
}