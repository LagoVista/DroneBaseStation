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

            var result = await link.RequestDataAsync<mavlink_mission_count_t>(drone, MAVLINK_MSG_ID.MISSION_REQUEST_LIST, req, MAVLINK_MSG_ID.MISSION_COUNT, TimeSpan.FromSeconds(1000));
            
            if (result.Successful)
            {

                for (ushort idx = 0; idx < result.Result.count; ++idx)
                {
                    var reqf = new mavlink_mission_request_int_t();

                    reqf.target_system = drone.SystemId;
                    reqf.target_component = drone.ComponentId;

                    reqf.seq = idx;
                    var wpResult = await link.RequestDataAsync<mavlink_mission_item_t>(drone, MAVLINK_MSG_ID.MISSION_REQUEST_INT, reqf, MAVLINK_MSG_ID.MISSION_ITEM, TimeSpan.FromSeconds(1000));
                    if (wpResult.Successful)
                    {
                        Debug.WriteLine(wpResult.Result.x + " " + wpResult.Result.y + " " + wpResult.Result.z);
                    }
                    else
                    {
                        Debug.WriteLine($"No joy on {idx}");
                    }
                }

                Debug.WriteLine($"Get go our response {result.Result.count}");
            }
            else
            {
                Debug.WriteLine($"No joy");
            }

        }
    }
}
