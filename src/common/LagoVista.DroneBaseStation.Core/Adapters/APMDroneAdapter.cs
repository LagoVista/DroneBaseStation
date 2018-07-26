using LagoVista.Core.Models.Geo;
using LagoVista.Drone;
using LagoVista.DroneBaseStation.Core.Interfaces;
using System;
using System.Threading.Tasks;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.Adapters
{
    public class APMDroneAdapter : IDroneAdapter
    {
        public Task UpdateDroneAsync(IDrone drone, MAVLink.MAVLinkMessage msg)
        {

            switch((MAVLink.MAVLINK_MSG_ID)msg.msgid)
            {
                case MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
                    var hb = MavlinkUtil.ByteArrayToStructure<mavlink_heartbeat_t>(msg.payload);
                    

                    break;
                case MAVLINK_MSG_ID.RAW_IMU:
                    var imu = MavlinkUtil.ByteArrayToStructure<mavlink_raw_imu_t>(msg.payload);
                    drone.Acc.X = imu.xacc;
                    drone.Acc.Y = imu.yacc;
                    drone.Acc.Z = imu.zacc;

                    drone.Gyro.X = imu.xgyro;
                    drone.Gyro.Y = imu.ygyro;
                    drone.Gyro.Z = imu.zgyro;

                    drone.Magnometer.X = imu.xmag;
                    drone.Magnometer.Y = imu.xmag;
                    drone.Magnometer.Z = imu.zmag;

                    break;
                case MAVLINK_MSG_ID.ATTITUDE:
                    var att = MavlinkUtil.ByteArrayToStructure<mavlink_attitude_t>(msg.payload);
                    drone.Pitch = att.pitch;
                    drone.Roll = att.roll;
                    drone.Yaw = att.yaw;
                    drone.PitchSpeed = att.pitchspeed;
                    drone.RollSpeed = att.rollspeed;
                    drone.YawSpeed = att.yawspeed;
                    break;

                case MAVLINK_MSG_ID.RC_CHANNELS:
                    var rc = MavlinkUtil.ByteArrayToStructure<mavlink_rc_channels_raw_t>(msg.payload);
                    drone.Channels[0].Value = rc.chan1_raw;
                    drone.Channels[1].Value = rc.chan2_raw;
                    drone.Channels[2].Value = rc.chan3_raw;
                    drone.Channels[3].Value = rc.chan4_raw;
                    drone.Channels[4].Value = rc.chan5_raw;
                    drone.Channels[5].Value = rc.chan6_raw;
                    drone.Channels[6].Value = rc.chan7_raw;
                    drone.Channels[7].Value = rc.chan8_raw;
                    break;
                case MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                    var gpsint = MavlinkUtil.ByteArrayToStructure<mavlink_global_position_int_t>(msg.payload);

                    var location = new GeoLocation()
                    {
                        Longitude = gpsint.lon / 1000000.0f,
                        Latitude = gpsint.lat / 1000000.0f,
                        Altitude = gpsint.alt / 1000.0f
                    };

                    drone.Location = location;

                    break;

                case MAVLINK_MSG_ID.SERVO_OUTPUT_RAW:
                    var srvout = MavlinkUtil.ByteArrayToStructure<mavlink_servo_output_raw_t>(msg.payload);
                    drone.ServoOutputs[0].Value = srvout.servo1_raw;
                    drone.ServoOutputs[1].Value = srvout.servo2_raw;
                    drone.ServoOutputs[2].Value = srvout.servo3_raw;
                    drone.ServoOutputs[3].Value = srvout.servo4_raw;
                    drone.ServoOutputs[4].Value = srvout.servo5_raw;
                    drone.ServoOutputs[5].Value = srvout.servo6_raw;
                    drone.ServoOutputs[6].Value = srvout.servo7_raw;
                    drone.ServoOutputs[7].Value = srvout.servo8_raw;
                    drone.ServoOutputs[8].Value = srvout.servo9_raw;
                    drone.ServoOutputs[9].Value = srvout.servo10_raw;
                    drone.ServoOutputs[10].Value = srvout.servo11_raw;
                    drone.ServoOutputs[11].Value = srvout.servo12_raw;
                    drone.ServoOutputs[12].Value = srvout.servo13_raw;
                    drone.ServoOutputs[13].Value = srvout.servo14_raw;
                    drone.ServoOutputs[14].Value = srvout.servo15_raw;
                    drone.ServoOutputs[15].Value = srvout.servo16_raw;

                    break;
                case MAVLINK_MSG_ID.MISSION_ACK:
                    break;
                case MAVLINK_MSG_ID.MISSION_CLEAR_ALL:
                    break;
                case MAVLINK_MSG_ID.MISSION_COUNT:
                    break;
                case MAVLINK_MSG_ID.MISSION_CURRENT:
                    break;
                case MAVLINK_MSG_ID.MISSION_ITEM:
                    break;
                case MAVLINK_MSG_ID.MISSION_ITEM_INT:
                    break;
                case MAVLINK_MSG_ID.MISSION_ITEM_REACHED:
                    break;
                case MAVLINK_MSG_ID.MISSION_REQUEST:
                    break;
                case MAVLINK_MSG_ID.MISSION_REQUEST_INT:
                    break;
                case MAVLINK_MSG_ID.MISSION_REQUEST_LIST:
                    break;
                case MAVLINK_MSG_ID.MISSION_REQUEST_PARTIAL_LIST:
                    break;
                case MAVLINK_MSG_ID.MISSION_SET_CURRENT:
                    break;
                case MAVLINK_MSG_ID.MISSION_WRITE_PARTIAL_LIST:
                    break;

            }

            
            //msg.MessageInfo.Type
                return Task.FromResult(default(Object));
        }
    }
}
