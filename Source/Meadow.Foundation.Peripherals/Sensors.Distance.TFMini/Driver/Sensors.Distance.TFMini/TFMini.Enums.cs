using System;
namespace Sensors.Distance
{
    public partial class TFMini
    {
        public enum OperatingMode
        {
            Configuration,
            SingleScan,
            Continuous
        }

        public enum States
        {
            Ready = 0,
            SerialError_NoHeader = 1,
            SerialError_BadChecksum = 2,
            SerialError_TooManyAttempts = 3,
            Measurement_OK = 10
        }
    }
}
