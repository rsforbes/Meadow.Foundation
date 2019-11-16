using System;
namespace Sensors.Distance
{
	public partial class TFMini
	{
        protected static class Commands
        {
            /// <summary>
            /// Enters the configuration mode so settings can be changed.
            /// </summary>
            public static byte[] EnterConfigMode = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 };

            /// <summary>
            /// Exits the configuration mode.
            /// </summary>
            public static byte[] ExitConfigMode = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x00, 0x02 };

            /// <summary>
            /// Echo'd back in byte 3 of the command when last command was successful
            /// </summary>
            public static byte Success = 0x01;

            /// <summary>
            /// Echo'd back in byte 3 of the command when last command errored
            /// </summary>
            public static byte InstructionError = 0xFF;

            /// <summary>
            /// Echo'd back in byte 3 of the command when last command had an incorrect parameter
            /// </summary>
            public static byte ParameterError = 0x0F;

            /// <summary>
            /// Distances returned in TFMini frame format.
            /// </summary>
            public static byte[] StandardOutputFormat = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x01, 0x06 };

            //public byte[] DataOutputCycle(int millisecond)
            //{ 
            //}

            public static byte[] RangeLimitDisabled = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x01, 0x19 };

            public static byte[] RangeLimit(int range)
            {
                // lower
                return new byte[] { 0x42, 0x57, 0x02, 0x00, 0xEE, 0x00, 0x00, 0x20 };

                // upper
                // { 0x42, 0x57, 0x02, 0x00, 0xEE, 0xFF, 0x??, 0x21 }
            }

            /// <summary>
            /// 0-2 meters
            /// </summary>
            public static byte[] ShortDistanceMode = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x00, 0x11 };

            /// <summary>
            /// 0.5 - 5 meters
            /// </summary>
            public static byte[] MiddleDistanceMode = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x03, 0x11 };

            /// <summary>
            /// 1 - 12 meters
            /// </summary>
            public static byte[] LongDistanceMode = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x07, 0x11 };

            /// <summary>
            /// Set the distance units to milimeter.
            /// </summary>
            public static byte[] DistanceUnitMm = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x00, 0x1A };

            /// <summary>
            /// Set the distance units to cenitmeter.
            /// </summary>
            public static byte[] DistanceUnitCm = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x01, 0x1A };

            /// <summary>
            /// Automatic triggering, for continuous reading mode.
            /// 100Hz by default.
            /// </summary>
            public static byte[] InternalTriggerMode = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x01, 0x40 };

            /// <summary>
            /// Tells the device to only make a reading when a trigger command
            /// is set. Use in conjunction with SendReadTrigger.
            /// </summary>
            public static byte[] ExternalTriggerMode = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x00, 0x40 };

            /// <summary>
            /// Triggers the device to make a reading.
            /// </summary>
            public static byte[] SendReadTrigger = {
                0x42, 0x57, 0x02, 0x00, 0x00, 0x00, 0x00, 0x41 };

            /// <summary>
            /// Resets the device settings. Manual says use with caution.
            /// ¯\_(ツ)_/¯
            /// </summary>
            public static byte[] Reset = {
                0x42, 0x57, 0x02, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };

            public static byte[] SetBaudRate(int baud)
            {
                // lower
                return new byte[] {
                    0x42, 0x57, 0x02, 0x00, 0xEE, 0x00, GetBaudRateSpecifier(baud), 0x08 };
            }
    
            public static byte GetBaudRateSpecifier(int baud) {
                switch (baud) {
                    case 9600:
                        return 0x00;
                    case 14400:
                        return 0x01;
                    case 19200:
                        return 0x02;
                    case 38400:
                        return 0x03;
                    case 56000:
                        return 0x04;
                    case 57600:
                        return 0x05;
                    case 115200:
                        return 0x06;
                    default:
                        throw new ArgumentException("Baud rate not supported.");
                }
                // the MCU on the meadow doesn't really go beyond that
                //b128000 = 0x07,
                //b230400 = 0x08,
                //b256000 = 0x09,
                //b460800 = 0x10,
                //b500000 = 0x0b,
                //b512000 = 0x0c
            }
        }
	}
}
