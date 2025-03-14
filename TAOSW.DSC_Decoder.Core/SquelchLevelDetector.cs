using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAOSW.DSC_Decoder.Core
{
    public class SquelchLevelDetector
    {
        private float _squelchLevel;
        private float _squelchThreshold;
        private float _squelchHysteresis;
        private bool _squelchOpen;

        public SquelchLevelDetector(float squelchThreshold, float squelchHysteresis)
        {
            _squelchThreshold = squelchThreshold;
            _squelchHysteresis = squelchHysteresis;
            _squelchOpen = false;
        }

        public bool Detect(float[] signal)
        {
            float power = 0;
            for (int i = 0; i < signal.Length; i++)
                power += signal[i] * signal[i];
            power /= signal.Length;

            if (power > _squelchThreshold)
            {
                _squelchLevel = power;
                _squelchOpen = true;
            }
            else if (power < _squelchThreshold - _squelchHysteresis)
            {
                _squelchOpen = false;
            }

            return _squelchOpen;
        }
    }
}
