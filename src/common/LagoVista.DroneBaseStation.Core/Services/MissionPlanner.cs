using LagoVista.Drone;
using LagoVista.DroneBaseStation.Core.Interfaces;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static MAVLink;
using System.Security.Cryptography;
using System.Diagnostics;

namespace LagoVista.DroneBaseStation.Core.Services
{
    public class MissionPlanner : IMissionPlanner
    {
        public MissionPlanner()
        {
            

        }

        private void _link_MessageParsed(object sender, MAVLinkMessage e)
        {

        }

        public async Task GetWayPoints(IDrone drone, ISerialTelemetryLink link)
        {
            var req = new mavlink_mission_request_list_t();

            req.target_system = drone.SystemId;
            req.target_component = drone.ComponentId;
            
            await link.SendMessage(drone, MAVLINK_MSG_ID.MISSION_REQUEST_LIST, req);

            var result = await link.WaitForMessageAsync(MAVLINK_MSG_ID.MISSION_COUNT, TimeSpan.FromMilliseconds(1000));
            if (result.Successful)
            {
                Debug.WriteLine($"Get go our response!");
            }
            else;
            {
                Debug.WriteLine($"No joy");
            }

        }
    }
}
