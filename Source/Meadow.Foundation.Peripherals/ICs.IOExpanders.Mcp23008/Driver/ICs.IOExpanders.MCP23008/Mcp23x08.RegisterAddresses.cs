using System;
namespace Meadow.Foundation.ICs.IOExpanders
{
    public partial class Mcp23x08
    {
        public static class RegisterAddresses
        {
            /// <summary>
            /// IODIR
            /// </summary>
            public const byte IODirectionRegister = 0x00; //IODIR
            /// <summary>
            /// IPOL
            /// </summary>
            public const byte InputPolarityRegister = 0x01; //IPOL
            /// <summary>
            /// GPINTEN
            /// </summary>
            public const byte InterruptOnChangeRegister = 0x02; //GPINTEN
            /// <summary>
            /// DEFVAL
            /// </summary>
            public const byte DefaultComparisonValueRegister = 0x03; //DEFVAL
            /// <summary>
            /// INTCON
            /// </summary>
            public const byte InterruptControlRegister = 0x04; //INTCON
            /// <summary>
            /// IOCON
            /// </summary>
            public const byte IOConfigurationRegister = 0x05; //IOCON
            /// <summary>
            /// GPPU
            /// </summary>
            public const byte PullupResistorConfigurationRegister = 0x06; //GPPU
            /// <summary>
            /// INTF
            /// </summary>
            public const byte InterruptFlagRegister = 0x07; //INTF
            /// <summary>
            /// INTCAP
            /// </summary>
            public const byte InterruptCaptureRegister = 0x08; //INTCAP
            /// <summary>
            /// GPIO
            /// </summary>
            public const byte GPIORegister = 0x09; //GPIO
            /// <summary>
            /// OLAT
            /// </summary>
            public const byte OutputLatchRegister = 0x0A; //OLAT

        }
    }
}
