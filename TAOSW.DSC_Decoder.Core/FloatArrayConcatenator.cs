namespace TAOSW.DSC_Decoder.Core
{
    using System;
    using System.Collections.Generic;

    namespace TAOSW.DSC_Decoder.Core
    {
        public class FloatArrayConcatenator
        {
            private readonly int _targetLength;
            private readonly List<float> _memoryArray;

            public event Action<float[]> OnTargetLengthReached;

            public FloatArrayConcatenator(int targetLength)
            {
                _targetLength = targetLength;
                _memoryArray = new List<float>();
            }

            public void AddData(float[] data)
            {
                _memoryArray.AddRange(data);

                if (_memoryArray.Count >= _targetLength)
                {
                    OnTargetLengthReached?.Invoke(_memoryArray.ToArray());
                    _memoryArray.Clear();
                }
            }
        }
    }

}
