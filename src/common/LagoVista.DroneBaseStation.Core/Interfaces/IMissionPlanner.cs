using LagoVista.Core.Validation;
using LagoVista.Drone;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.DroneBaseStation.Core.Interfaces
{
    public interface IMissionPlanner
    {
        Task GetWayPoints(IDrone drone, ISerialTelemetryLink link);
    }
}
