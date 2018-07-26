using LagoVista.Core.Models;
using LagoVista.Drone.Models;
using System;
using System.Collections.ObjectModel;

namespace LagoVista.Drone
{
    public interface IDrone
    {
        bool MavLink2 { get; }
        bool Signing { get; }
        byte SendLinkId { get; }

        byte[] SigningKey { get; set; }

        ulong TimeStamp { get; set; }

        String DeviceId { get; }
        EntityHeader DeviceType { get; }
        EntityHeader DeviceConfiguration { get; }

        ThreeDOFSensor Acc {get;}
        ThreeDOFSensor Gyro { get; }
        ThreeDOFSensor Magnometer { get; }

        float Pitch { get; set; }
        float Yaw { get; set; }
        float Roll { get; set; }
        float PitchSpeed { get; set; }
        float YawSpeed { get; set; }
        float RollSpeed { get; set; }

        ObservableCollection<RCChannel> Channels { get; }
        ObservableCollection<ServoOutput> ServoOutputs { get; }

        LagoVista.Core.Models.Geo.GeoLocation Location { get; set; }

        byte SystemId { get; }
        byte ComponentId { get; }
    }
}
