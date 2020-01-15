using System;
using Meadow.Hardware;

namespace Meadow.Foundation.ICs.ADCs
{
    #region Constructors

    public class Ads1015 : Ads1x15
    {
        /// <summary>
        ///     Default Ads1115 constructor is private to prevent it from being used.
        /// </summary>
        private Ads1015() { }

        /// <summary>
        ///     Create a new Ads1115 object using the default parameters
        /// </summary>
        /// <param name="address">Address of the bus on the I2C display.</param>
        /// <param name="i2cBus">I2C bus instance</param>
        public Ads1015(II2cBus i2cBus, byte address = 0x48)
        {
            ads1x15 = new I2cPeripheral(i2cBus, address);

            Initialize();
        }

        #endregion Constructors

        private void Initialize()
        {
            conversionDelay = ADS1015_CONVERSIONDELAY;
            bitShift = 4;
            Gain = GainType.GAIN_TWOTHIRDS; /* +/- 6.144V range (limited to VDD +0.3V max!) */
        }
    }
}
