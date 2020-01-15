using System;
using System.Threading;
using Meadow.Hardware;

namespace Meadow.Foundation.ICs.ADCs
{
    //128 LED driver
    //39 key input
    public abstract class Ads1x15
    {
        #region Properties

        public GainType Gain { get; set; }

        #endregion

        #region Enums

        public enum GainType : ushort
        {
            GAIN_TWOTHIRDS = ADS1015_REG_CONFIG_PGA_6_144V,
            GAIN_ONE = ADS1015_REG_CONFIG_PGA_4_096V,
            GAIN_TWO = ADS1015_REG_CONFIG_PGA_2_048V,
            GAIN_FOUR = ADS1015_REG_CONFIG_PGA_1_024V,
            GAIN_EIGHT = ADS1015_REG_CONFIG_PGA_0_512V,
            GAIN_SIXTEEN = ADS1015_REG_CONFIG_PGA_0_256V
        };

        #endregion Enums

        #region Member variables / fields
        /// <summary>
        ///     Ads1x15 ADC driver
        /// </summary>
        protected II2cPeripheral ads1x15;

        protected byte conversionDelay;
        protected byte bitShift;
   

        protected const byte ADS1015_ADDRESS = 0x48;

        protected const byte ADS1015_CONVERSIONDELAY = 1;
        protected const byte ADS1115_CONVERSIONDELAY = 8;

        private const byte ADS1015_REG_POINTER_MASK = 0x03;
        private const byte ADS1015_REG_POINTER_CONVERT = 0x00;
        private const byte ADS1015_REG_POINTER_CONFIG = 0x01;
        private const byte ADS1015_REG_POINTER_LOWTHRESH = 0x02;
        private const byte ADS1015_REG_POINTER_HITHRESH = 0x03;

        private const ushort ADS1015_REG_CONFIG_OS_MASK      = 0x8000;
        private const ushort ADS1015_REG_CONFIG_OS_SINGLE    = 0x8000;  // Write: Set to start a single-conversion
        private const ushort ADS1015_REG_CONFIG_OS_BUSY      = 0x0000;  // Read: Bit = 0 when conversion is in progress
        private const ushort ADS1015_REG_CONFIG_OS_NOTBUSY   = 0x8000;  // Read: Bit = 1 when device is not performing a conversion

        private const ushort ADS1015_REG_CONFIG_MUX_MASK     = 0x7000;
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_0_1 = 0x0000;  // Differential P = AIN0, N = AIN1 (default)
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_0_3 = 0x1000;  // Differential P = AIN0, N = AIN3
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_1_3 = 0x2000;  // Differential P = AIN1, N = AIN3
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_2_3 = 0x3000;  // Differential P = AIN2, N = AIN3
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_0 = 0x4000;  // Single-ended AIN0
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_1 = 0x5000;  // Single-ended AIN1
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_2 = 0x6000;  // Single-ended AIN2
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_3 = 0x7000;  // Single-ended AIN3

        private const ushort ADS1015_REG_CONFIG_PGA_MASK     = 0x0E00;
        private const ushort ADS1015_REG_CONFIG_PGA_6_144V   = 0x0000;  // +/-6.144V range = Gain 2/3
        private const ushort ADS1015_REG_CONFIG_PGA_4_096V   = 0x0200;  // +/-4.096V range = Gain 1
        private const ushort ADS1015_REG_CONFIG_PGA_2_048V   = 0x0400;  // +/-2.048V range = Gain 2 (default)
        private const ushort ADS1015_REG_CONFIG_PGA_1_024V   = 0x0600;  // +/-1.024V range = Gain 4
        private const ushort ADS1015_REG_CONFIG_PGA_0_512V   = 0x0800;  // +/-0.512V range = Gain 8
        private const ushort ADS1015_REG_CONFIG_PGA_0_256V   = 0x0A00;  // +/-0.256V range = Gain 16

        private const ushort ADS1015_REG_CONFIG_MODE_MASK = 0x0100;
        private const ushort ADS1015_REG_CONFIG_MODE_CONTIN = 0x0000;  // Continuous conversion mode
        private const ushort ADS1015_REG_CONFIG_MODE_SINGLE = 0x0100; // Power-down single-shot mode (default)

        private const ushort ADS1015_REG_CONFIG_DR_MASK = 0x00E0;
        private const ushort ADS1015_REG_CONFIG_DR_128SPS = 0x0000; // 128 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_250SPS = 0x0020;  // 250 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_490SPS = 0x0040;  // 490 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_920SPS = 0x0060;  // 920 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_1600SPS = 0x0080;  // 1600 samples per second (default)
        private const ushort ADS1015_REG_CONFIG_DR_2400SPS = 0x00A0;  // 2400 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_3300SPS = 0x00C0;  // 3300 samples per second

        private const ushort ADS1015_REG_CONFIG_CMODE_MASK = 0x0010;
        private const ushort ADS1015_REG_CONFIG_CMODE_TRAD = 0x0000;  // Traditional comparator with hysteresis (default)
        private const ushort ADS1015_REG_CONFIG_CMODE_WINDOW = 0x0010;  // Window comparator

        private const ushort ADS1015_REG_CONFIG_CPOL_MASK = 0x0008;
        private const ushort ADS1015_REG_CONFIG_CPOL_ACTVLOW = 0x0000; // ALERT/RDY pin is low when active (default)
        private const ushort ADS1015_REG_CONFIG_CPOL_ACTVHI = 0x0008;  // ALERT/RDY pin is high when active

        private const ushort ADS1015_REG_CONFIG_CLAT_MASK = 0x0004;  // Determines if ALERT/RDY pin latches once asserted
        private const ushort ADS1015_REG_CONFIG_CLAT_NONLAT = 0x0000; // Non-latching comparator (default)
        private const ushort ADS1015_REG_CONFIG_CLAT_LATCH = 0x0004;  // Latching comparator

        private const ushort ADS1015_REG_CONFIG_CQUE_MASK = 0x0003;
        private const ushort ADS1015_REG_CONFIG_CQUE_1CONV = 0x0000; // Assert ALERT/RDY after one conversions
        private const ushort ADS1015_REG_CONFIG_CQUE_2CONV = 0x0001;  // Assert ALERT/RDY after two conversions
        private const ushort ADS1015_REG_CONFIG_CQUE_4CONV = 0x0002;  // Assert ALERT/RDY after four conversions
        private const ushort ADS1015_REG_CONFIG_CQUE_NONE = 0x0003;  // Disable the comparator and put ALERT/RDY in high state (default)

        #endregion Member variables / fields

        #region Methods

        public ushort ReadAdcSingleEnded(byte channel)
        {
            if (channel > 3)
            {
                return 0;
            }

            // Start with default values
            ushort config = ADS1015_REG_CONFIG_CQUE_NONE | // Disable the comparator (default val)
                              ADS1015_REG_CONFIG_CLAT_NONLAT | // Non-latching (default val)
                              ADS1015_REG_CONFIG_CPOL_ACTVLOW | // Alert/Rdy active low   (default val)
                              ADS1015_REG_CONFIG_CMODE_TRAD | // Traditional comparator (default val)
                              ADS1015_REG_CONFIG_DR_1600SPS | // 1600 samples per second (default)
                              ADS1015_REG_CONFIG_MODE_SINGLE;   // Single-shot mode (default)

            // Set PGA/voltage range
            config |= (ushort)Gain;

            // Set single-ended input channel
            switch (channel)
            {
                case (0):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_0;
                    break;
                case (1):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_1;
                    break;
                case (2):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_2;
                    break;
                case (3):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_3;
                    break;
            }

            // Set 'start single-conversion' bit
            config |= ADS1015_REG_CONFIG_OS_SINGLE;

            // Write config register to the ADC
            ads1x15.WriteUShort(ADS1015_REG_POINTER_CONFIG, config);

            // Wait for the conversion to complete
            Thread.Sleep(conversionDelay);

            // Read the conversion results
            // Shift 12-bit results right 4 bits for the ADS1015
            return (ushort)(ads1x15.ReadRegister(ADS1015_REG_POINTER_CONVERT) >> bitShift);
        }

        public ushort ReadADC_Differential_0_1()
        {
            // Start with default values
            ushort config = ADS1015_REG_CONFIG_CQUE_NONE | // Disable the comparator (default val)
                              ADS1015_REG_CONFIG_CLAT_NONLAT | // Non-latching (default val)
                              ADS1015_REG_CONFIG_CPOL_ACTVLOW | // Alert/Rdy active low   (default val)
                              ADS1015_REG_CONFIG_CMODE_TRAD | // Traditional comparator (default val)
                              ADS1015_REG_CONFIG_DR_1600SPS | // 1600 samples per second (default)
                              ADS1015_REG_CONFIG_MODE_SINGLE;   // Single-shot mode (default)

            // Set PGA/voltage range
            config |= (ushort)Gain;

            // Set channels
            config |= ADS1015_REG_CONFIG_MUX_DIFF_0_1;          // AIN0 = P, AIN1 = N

            // Set 'start single-conversion' bit
            config |= ADS1015_REG_CONFIG_OS_SINGLE;

            // Write config register to the ADC
            ads1x15.WriteUShort(ADS1015_REG_POINTER_CONFIG, config);

            // Wait for the conversion to complete
            Thread.Sleep(conversionDelay);

            // Read the conversion results
            ushort res = (ushort)(ads1x15.ReadRegister(ADS1015_REG_POINTER_CONVERT) >> bitShift);

            if (bitShift == 0)
            {
                return (ushort)res;
            }
            else
            {
                // Shift 12-bit results right 4 bits for the ADS1015,
                // making sure we keep the sign bit intact
                if (res > 0x07FF)
                {
                    // negative number - extend the sign to 16th bit
                    res |= 0xF000;
                }
                return (ushort)res;
            }
        }

        /**************************************************************************/
        /*! 
            @brief  Reads the conversion results, measuring the voltage
                    difference between the P (AIN2) and N (AIN3) input.  Generates
                    a signed value since the difference can be either
                    positive or negative.
        */
        /**************************************************************************/
        public ushort ReadADC_Differential_2_3()
        {
            // Start with default values
            ushort config = ADS1015_REG_CONFIG_CQUE_NONE | // Disable the comparator (default val)
                              ADS1015_REG_CONFIG_CLAT_NONLAT | // Non-latching (default val)
                              ADS1015_REG_CONFIG_CPOL_ACTVLOW | // Alert/Rdy active low   (default val)
                              ADS1015_REG_CONFIG_CMODE_TRAD | // Traditional comparator (default val)
                              ADS1015_REG_CONFIG_DR_1600SPS | // 1600 samples per second (default)
                              ADS1015_REG_CONFIG_MODE_SINGLE;   // Single-shot mode (default)

            // Set PGA/voltage range
            config |= (ushort)Gain;

            // Set channels
            config |= ADS1015_REG_CONFIG_MUX_DIFF_2_3;          // AIN2 = P, AIN3 = N

            // Set 'start single-conversion' bit
            config |= ADS1015_REG_CONFIG_OS_SINGLE;

            // Write config register to the ADC
            ads1x15.WriteUShort(ADS1015_REG_POINTER_CONFIG, config);

            // Wait for the conversion to complete
            Thread.Sleep(conversionDelay);

            // Read the conversion results
            ushort res = (ushort)(ads1x15.ReadRegister(ADS1015_REG_POINTER_CONVERT) >> bitShift);

            if (bitShift == 0)
            {
                return (ushort)res;
            }
            else
            {
                // Shift 12-bit results right 4 bits for the ADS1015,
                // making sure we keep the sign bit intact
                if (res > 0x07FF)
                {
                    // negative number - extend the sign to 16th bit
                    res |= 0xF000;
                }
                return (ushort)res;
            }
        }

        /**************************************************************************/
        /*!
            @brief  Sets up the comparator to operate in basic mode, causing the
                    ALERT/RDY pin to assert (go from high to low) when the ADC
                    value exceeds the specified threshold.
                    This will also set the ADC in continuous conversion mode.
        */
        /**************************************************************************/
        public void StartComparator_SingleEnded(byte channel, ushort threshold)
        {
            // Start with default values
            ushort config = ADS1015_REG_CONFIG_CQUE_1CONV | // Comparator enabled and asserts on 1 match
                              ADS1015_REG_CONFIG_CLAT_LATCH | // Latching mode
                              ADS1015_REG_CONFIG_CPOL_ACTVLOW | // Alert/Rdy active low   (default val)
                              ADS1015_REG_CONFIG_CMODE_TRAD | // Traditional comparator (default val)
                              ADS1015_REG_CONFIG_DR_1600SPS | // 1600 samples per second (default)
                              ADS1015_REG_CONFIG_MODE_CONTIN | // Continuous conversion mode
                              ADS1015_REG_CONFIG_MODE_CONTIN;   // Continuous conversion mode

            // Set PGA/voltage range
            config |= (ushort)Gain;

            // Set single-ended input channel
            switch (channel)
            {
                case (0):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_0;
                    break;
                case (1):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_1;
                    break;
                case (2):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_2;
                    break;
                case (3):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_3;
                    break;
            }

            // Set the high threshold register
            // Shift 12-bit results left 4 bits for the ADS1015
            ads1x15.WriteUShort(ADS1015_REG_POINTER_HITHRESH, (ushort)(threshold << bitShift));

            // Write config register to the ADC
            ads1x15.WriteUShort(ADS1015_REG_POINTER_CONFIG, config);
        }

        /**************************************************************************/
        /*!
            @brief  In order to clear the comparator, we need to read the
                    conversion results.  This function reads the last conversion
                    results without changing the config value.
        */
        /**************************************************************************/
        public ushort GetLastConversionResults()
        {
            // Wait for the conversion to complete
            Thread.Sleep(conversionDelay);

            // Read the conversion results
            ushort res = (ushort)(ads1x15.ReadRegister(ADS1015_REG_POINTER_CONVERT) >> bitShift);

            if (bitShift == 0)
            {
                return res;
            }
            else
            {
                // Shift 12-bit results right 4 bits for the ADS1015,
                // making sure we keep the sign bit intact
                if (res > 0x07FF)
                {
                    // negative number - extend the sign to 16th bit
                    res |= 0xF000;
                }
                return res;
            }
        }

        #endregion Methods
    }
}