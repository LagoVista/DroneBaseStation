﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using LagoVista.Core.Models;
using LagoVista.Core.Models.Geo;

namespace LagoVista.Drone.Models
{
    public class Drone : ModelBase, IDrone
    {
        public Drone()
        {
            Channels = new ObservableCollection<RCChannel>();
            for (var idx = 0; idx < 8; ++idx)
            {
                Channels.Add(new RCChannel());
            }

            ServoOutputs = new ObservableCollection<ServoOutput>();
            for(var idx = 0; idx < 16; ++idx)
            {
                ServoOutputs.Add(new ServoOutput());
            }


            SendLinkId = (byte)(new Random().Next(256));
            Signing = false;

            Acc = new ThreeDOFSensor();
            Gyro = new ThreeDOFSensor();
            Magnometer = new ThreeDOFSensor();

            ComponentId = 1;
            SystemId = 1;
        }

        public string DeviceId { get; }

        public EntityHeader DeviceType { get; }
        public EntityHeader DeviceConfiguration { get; }

        public ThreeDOFSensor Acc { get; }

        public ThreeDOFSensor Gyro { get; }

        public ThreeDOFSensor Magnometer { get; }

        public float _pitch;
        public float Pitch
        {
            get => _pitch;
            set => Set(ref _pitch, value);
        }

        public float _yaw;
        public float Yaw
        {
            get => _yaw;
            set => Set(ref _yaw, value);
        }

        public float _roll;
        public float Roll
        {
            get => _roll;
            set => Set(ref _roll, value);
        }

        public float _pitchSpeed;
        public float PitchSpeed
        {
            get => _pitchSpeed;
            set => Set(ref _pitchSpeed, value);
        }

        public float _yawSpeed;
        public float YawSpeed
        {
            get => _yawSpeed;
            set => Set(ref _yawSpeed, value);
        }

        public float _rollSpeed;
        public float RollSpeed
        {
            get => _rollSpeed;
            set => Set(ref _rollSpeed, value);
        }

        public ObservableCollection<RCChannel> Channels { get; }
        public ObservableCollection<ServoOutput> ServoOutputs { get; }

        GeoLocation _location;
        public GeoLocation Location
        {
            get { return _location; }
            set { Set(ref _location, value); }
        }

        public byte SystemId { get; }

        public byte ComponentId { get; }

        public bool MavLink2 { get; set; }

        public bool Signing { get; set; }

        public byte SendLinkId { get; set; }

        public byte[] SigningKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ulong TimeStamp { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
