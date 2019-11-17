using System;
using Meadow.Hardware;

namespace Meadow.Foundation.ICs.IOExpanders
{
    public partial class Mcp23x08
    {
        public class DigitalOutputPort : DigitalOutputPortBase
        {
            Mcp23x08 _mcp;

            public override bool State {
                get => this.state;
                set {
                    _mcp.WriteToPort(this.Pin, value);
                }
            } protected bool state;

            public DigitalOutputPort(
                Mcp23x08 mcpController,
                IPin pin,
                bool initialState = false)
                : base(pin, (IDigitalChannelInfo)pin.SupportedChannels[0], initialState)
            {
                _mcp = mcpController;
            }


            public override void Dispose()
            {
                
            }
        }
    }
}
