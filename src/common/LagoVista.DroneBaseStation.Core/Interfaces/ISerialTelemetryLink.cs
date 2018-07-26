using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.Drone;
using System;
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

        Task<InvokeResult<TMavlinkPacket>> WaitForMessageAsync<TMavlinkPacket>(MAVLINK_MSG_ID messageId, TimeSpan timeout) where TMavlinkPacket : struct;

        Task<InvokeResult<TMavlinkPacket>> RequestDataAsync<TMavlinkPacket>(IDrone drone, MAVLINK_MSG_ID outgoingMessageId, Object req, MAVLINK_MSG_ID incomingMessageId, TimeSpan timeout) where TMavlinkPacket : struct;
        Task<InvokeResult> SendMessage(IDrone drone, MAVLINK_MSG_ID messageId, Object req);

    }
}
