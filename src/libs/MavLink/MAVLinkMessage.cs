﻿using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public partial class MAVLink
{
    public class MAVLinkMessage
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MAVLinkMessage));

        public static readonly MAVLinkMessage Invalid = new MAVLinkMessage();
        object _locker = new object();

        private byte[] _buffer;

        public byte[] buffer
        {
            get { return _buffer; }
            set
            {
                _buffer = value;
                processBuffer(_buffer);
            }
        }

        public DateTime rxtime { get; set; }
        public byte header { get; set; }
        public byte payloadlength { get; set; }

        public byte incompat_flags { get; set; }
        public byte compat_flags { get; set; }

        public byte seq { get; set; }
        public byte sysid { get; set; }
        public byte targetsysid { get; set; }
        public byte targetcomponentid { get; set; }
        public byte compid { get; set; }

        public uint msgid { get; set; }

        public int payload_index { get; set; }

        public byte[] payload = new byte[255];

        public bool ismavlink2
        {
            get
            {
                if (buffer != null && buffer.Length > 0)
                    return (buffer[0] == MAVLINK_STX);

                return false;
            }
        }

        public string msgtypename
        {
            get { return MAVLINK_MESSAGE_INFOS.GetMessageInfo(msgid).name; }
        }

        object _data;
        public object data
        {
            get
            {
                // lock the entire creation of the packet. to prevent returning a incomplete packet.
                lock (_locker)
                {
                    if (_data != null)
                        return _data;

                    _data = Activator.CreateInstance(MAVLINK_MESSAGE_INFOS.GetMessageInfo(msgid).type);

                    try
                    {
                        // fill in the data of the object
                        if (ismavlink2)
                        {
                            MavlinkUtil.ByteArrayToStructure(buffer, ref _data, MAVLINK_NUM_HEADER_BYTES, payloadlength);
                        }
                        else
                        {
                            MavlinkUtil.ByteArrayToStructure(buffer, ref _data, 6, payloadlength);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }

                return _data;
            }
        }

        public T ToStructure<T>()
        {
            return (T)data;
        }

        public ushort crc16 { get; set; }

        public byte[] sig { get; set; }

        public byte signature { get; set; }

        public byte sigLinkid
        {
            get
            {
                if (sig != null)
                {
                    return sig[0];
                }

                return 0;
            }
        }

        public ulong sigTimestamp
        {
            get
            {
                if (sig != null)
                {
                    byte[] temp = new byte[8];
                    Array.Copy(sig, 1, temp, 0, 6);
                    return BitConverter.ToUInt64(temp, 0);
                }

                return 0;
            }
        }

        public int Length
        {
            get
            {
                if (buffer == null) return 0;
                return buffer.Length;
            }
        }

        public MAVLinkMessage()
        {
            this.rxtime = DateTime.MinValue;
        }

        public MAVLinkMessage(byte[] buffer) : this(buffer, DateTime.UtcNow)
        {
        }

        public MAVLinkMessage(byte[] buffer, DateTime rxTime)
        {
            this.buffer = buffer;
            this.rxtime = rxTime;

            processBuffer(buffer);
        }

        internal void processBuffer(byte[] buffer)
        {
            _data = null;

            if (buffer[0] == MAVLINK_STX)
            {
                header = buffer[0];
                payloadlength = buffer[1];
                incompat_flags = buffer[2];
                compat_flags = buffer[3];
                seq = buffer[4];
                sysid = buffer[5];
                compid = buffer[6];
                msgid = (uint)((buffer[9] << 16) + (buffer[8] << 8) + buffer[7]);

                var crc1 = MAVLINK_CORE_HEADER_LEN + payloadlength + 1;
                var crc2 = MAVLINK_CORE_HEADER_LEN + payloadlength + 2;

                crc16 = (ushort)((buffer[crc2] << 8) + buffer[crc1]);

                if ((incompat_flags & MAVLINK_IFLAG_SIGNED) > 0)
                {
                    sig = new byte[MAVLINK_SIGNATURE_BLOCK_LEN];
                    Array.ConstrainedCopy(buffer, buffer.Length - MAVLINK_SIGNATURE_BLOCK_LEN, sig, 0,
                        MAVLINK_SIGNATURE_BLOCK_LEN);
                }
            }
            else
            {
                header = buffer[0];
                payloadlength = buffer[1];
                seq = buffer[2];
                sysid = buffer[3];
                compid = buffer[4];
                msgid = buffer[5];

                var crc1 = MAVLINK_CORE_HEADER_MAVLINK1_LEN + payloadlength + 1;
                var crc2 = MAVLINK_CORE_HEADER_MAVLINK1_LEN + payloadlength + 2;

                crc16 = (ushort)((buffer[crc2] << 8) + buffer[crc1]);
            }
        }

        public override string ToString()
        {
            return String.Format("{5},{4},{0},{1},{2},{3}", sysid, compid, msgid, msgtypename, ismavlink2, rxtime);
        }
    }
}