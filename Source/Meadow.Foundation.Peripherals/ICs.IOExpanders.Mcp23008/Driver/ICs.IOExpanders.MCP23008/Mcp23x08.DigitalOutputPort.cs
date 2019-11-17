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

            protected DigitalOutputPort(
                Mcp23x08 mcpController,
                IPin pin,
                IDigitalChannelInfo channel,
                bool initialState = false)
                : base(pin, channel, initialState)
            {
                _mcp = mcpController;
            }


            public override void Dispose()
            {
                
            }
        }
    }
}
