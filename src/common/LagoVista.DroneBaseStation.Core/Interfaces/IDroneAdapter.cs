using LagoVista.Drone;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.Interfaces
{
    public interface IDroneAdapter
    {
        Task UpdateDroneAsync(IDrone drone, MAVLinkMessage msg);
    }
}
