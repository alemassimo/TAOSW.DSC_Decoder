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

        private bool ProcessSubSequence(List<int> subBitBuffer)
        {
            int Errors = 0;
            var dataOut = new List<int>();
            var bitArray = subBitBuffer.ToArray();
            for (int i = 0; i + 10 < bitArray.Length; i += 10)
            {

                var data = RetriveDataByte(bitArray, i);

                bool check = CheckParity(data, bitArray.Skip(i + 7).Take(3).ToArray());
                if (!check)
                {
                    dataOut.Add(-1);
                    Errors++;

                    if (Errors > ERROR_THRESHOLD) return false;
                }
                else
                {

                    dataOut.Add(data);
                    //if (data == 122) break;
                }
            }

            var processed = false;
            if (IsPhased(dataOut.ToArray(), 0))
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
            var dataOut = new List<int>();
            var bitArray = bitBuffer.ToArray();
            if (bitArray.Length < MESSAGE_LENGTH) return;


            for (int i = 0; i < 10; i += 1)
            {

                var preambleLength = GetPreambleLength(bitArray, i);
                var processed = ProcessSubSequence(bitArray.Skip(i).Take(MESSAGE_LENGTH + preambleLength).ToList());

                if (bitBuffer.Any()) bitBuffer.Dequeue();
            }
        }

        public static bool IsPreambleGroup(int[] bits, int start)
        {
            int[] preambleGroup = { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0 };
            int[] preambleGroup2 = { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
            try
            {
                var subSequence = bits.Skip(start).Take(10).ToArray();
                if (!subSequence.SequenceEqual(preambleGroup) && !subSequence.SequenceEqual(preambleGroup2)) return false;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
            return true;
        }

        private bool IsPhased(int[] bytes, int start)
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
            var bitsArray = bits.ToArray();
            int dataByte = 0;
            for (int j = 0; j < 7; j++) dataByte |= bitsArray[i + j] << j;


            return dataByte;
        }

        private bool CheckParity(int value, int[] parityBits)
        {
            try
            {
                int computedParity = ComputeParity(value);
                int receivedParity = parityBits[0] << 2 | parityBits[1] << 1 | parityBits[2];
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