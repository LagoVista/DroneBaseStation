using System;
using System.Collections.Generic;
using System.Text;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.Interfaces
{
    public interface IMavLinkMessageParser
    {
        event EventHandler<MAVLinkMessage> MessageParsed;
        void HandleBuffer(byte[] buffer, int readCount);
    }

}
