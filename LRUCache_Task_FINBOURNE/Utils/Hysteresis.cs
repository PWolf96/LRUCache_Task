namespace LRUCache_Task_FINBOURNE.Utils
{
    public class Hysteresis
    {
        private readonly double _lowerThreshold;
        private readonly double _upperThreshold;

        private bool _state;
        public bool State => _state;
        private bool _aboveUpperThreshold;
        public bool AboveUpperThreshold => _aboveUpperThreshold;
        /// <summary>
        /// Default implementation of a Hysteresis. Has a state which tracks if the value is inside the optimal range.
        /// </summary>
        /// <param name="lowerThreshold"></param>
        /// <param name="upperThreshold"></param>
        /// <exception cref="ArgumentException"></exception>
        public Hysteresis(double lowerThreshold, double upperThreshold)
        {
            if (lowerThreshold >= upperThreshold)
            {
                throw new ArgumentException("Lower threshold must be less than upper threshold");
            }

            _lowerThreshold = lowerThreshold;
            _upperThreshold = upperThreshold;
            _state = false;
        }

        public void Check(double value)
        {

            if (_state && value < _lowerThreshold)
            {
                _state = false;
            }

            else if (!_state && value > _upperThreshold)
            {
                _state = true;
            }

            if (value > _upperThreshold)
            {
                if (!_aboveUpperThreshold)
                {
                    _aboveUpperThreshold = true;
                }
            }
            else
            {
                if (_aboveUpperThreshold)
                {
                    _aboveUpperThreshold = false;
                }
            }
        }
    }
}
