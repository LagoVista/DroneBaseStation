using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.Interfaces
{
    public interface ISerialTelemetryLink : IDisposable
    {
        event EventHandler<MAVLinkMessage> MessageParsed;
        Task OpenAsync(SerialPortInfo portInfo);
        Task CloseAsync();
        int MessagesReceived { get; }
        int Errors { get; }
        long BytesReceived { get; }
        bool IsConected { get; }
        TimeSpan Timeout { get; set; }
    }
}
