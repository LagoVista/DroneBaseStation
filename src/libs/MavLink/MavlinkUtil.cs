using LagoVista.Drone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static MAVLink;

/// <summary>
/// Static methods and helpers for creation and manipulation of Mavlink packets
/// </summary>
public static class MavlinkUtil
{
    const int gcssysid = 255;

    /// <summary>
    /// Create a new mavlink packet object from a byte array as recieved over mavlink
    /// Endianess will be detetected using packet inspection
    /// </summary>
    /// <typeparam name="TMavlinkPacket">The type of mavlink packet to create</typeparam>
    /// <param name="bytearray">The bytes of the mavlink packet</param>
    /// <param name="startoffset">The position in the byte array where the packet starts</param>
    /// <returns>The newly created mavlink packet</returns>
    public static TMavlinkPacket ByteArrayToStructure<TMavlinkPacket>(this byte[] bytearray, int startoffset = 6)
        where TMavlinkPacket : struct
    {
        return ReadUsingPointer<TMavlinkPacket>(bytearray, startoffset);
    }

    public static TMavlinkPacket ByteArrayToStructureBigEndian<TMavlinkPacket>(this byte[] bytearray,
        int startoffset = 6) where TMavlinkPacket : struct
    {
        object newPacket = new TMavlinkPacket();
        ByteArrayToStructureEndian(bytearray, ref newPacket, startoffset);
        return (TMavlinkPacket) newPacket;
    }

    public static void ByteArrayToStructure(byte[] bytearray, ref object obj, int startoffset, int payloadlength = 0)
    {
        int len = Marshal.SizeOf(obj);

        IntPtr iptr = Marshal.AllocHGlobal(len);

        //clear memory
        for (int i = 0; i < len; i += 8)
        {
            Marshal.WriteInt64(iptr, i, 0x00);
        }

        for (int i = len - (len % 8); i < len; i++)
        {
            Marshal.WriteByte(iptr, i, 0x00);
        }

        // copy byte array to ptr
        Marshal.Copy(bytearray, startoffset, iptr, payloadlength);

        obj = Marshal.PtrToStructure(iptr, obj.GetType());

        Marshal.FreeHGlobal(iptr);
    }

    public static TMavlinkPacket ByteArrayToStructureT<TMavlinkPacket>(byte[] bytearray, int startoffset)
    {
        int len = bytearray.Length - startoffset;

        IntPtr i = Marshal.AllocHGlobal(len);

        try
        {
            // copy byte array to ptr
            Marshal.Copy(bytearray, startoffset, i, len);
        }
        catch (Exception ex)
        {
            Console.WriteLine("ByteArrayToStructure FAIL " + ex.Message);
        }

        var obj = Marshal.PtrToStructure(i, typeof (TMavlinkPacket));

        Marshal.FreeHGlobal(i);

        return (TMavlinkPacket) obj;
    }

    public static byte[] TrimePayload(ref byte[] payload)
    {
        var length = payload.Length;
        while (length > 1 && payload[length - 1] == 0)
        {
            length--;
        }
        if (length != payload.Length)
            Array.Resize(ref payload, length);
        return payload;
    }

    public static T ReadUsingPointer<T>(byte[] data, int startoffset) where T : struct
    {
        unsafe
        {
            fixed (byte* p = &data[startoffset])
            {
                return (T) Marshal.PtrToStructure(new IntPtr(p), typeof (T));
            }
        }
    }

    public static T ByteArrayToStructureGC<T>(byte[] bytearray, int startoffset) where T : struct
    {
        GCHandle gch = GCHandle.Alloc(bytearray, GCHandleType.Pinned);
        try
        {
            return (T) Marshal.PtrToStructure(new IntPtr(gch.AddrOfPinnedObject().ToInt64() + startoffset), typeof (T));
        }
        finally
        {
            gch.Free();
        }
    }

    public static void ByteArrayToStructureEndian(byte[] bytearray, ref object obj, int startoffset)
    {

        int len = Marshal.SizeOf(obj);
        IntPtr i = Marshal.AllocHGlobal(len);
        byte[] temparray = (byte[]) bytearray.Clone();

        // create structure from ptr
        obj = Marshal.PtrToStructure(i, obj.GetType());

        // do endian swap
        object thisBoxed = obj;
        var test = thisBoxed.GetType().GetTypeInfo();

        int reversestartoffset = startoffset;

        // Enumerate each structure field using reflection.
        foreach (var field in test.GetFields())
        {
            // field.Name has the field's name.
            object fieldValue = field.GetValue(thisBoxed); // Get value

            // Get the TypeCode enumeration. Multiple types get mapped to a common typecode.
            TypeCode typeCode = Type.GetTypeCode(fieldValue.GetType());

            if (typeCode != TypeCode.Object)
            {
                Array.Reverse(temparray, reversestartoffset, Marshal.SizeOf(fieldValue));
                reversestartoffset += Marshal.SizeOf(fieldValue);
            }
            else
            {
                var elementsize = Marshal.SizeOf(((Array)fieldValue).GetValue(0));

                reversestartoffset += ((Array)fieldValue).Length * elementsize;
            }

        }

        try
        {
            // copy byte array to ptr
            Marshal.Copy(temparray, startoffset, i, len);
        }
        catch (Exception ex)
        {
            Console.WriteLine("ByteArrayToStructure FAIL" + ex.ToString());
        }

        obj = Marshal.PtrToStructure(i, obj.GetType());

        Marshal.FreeHGlobal(i);

    }

    /// <summary>
    /// Convert a struct to an array of bytes, struct fields being reperesented in 
    /// little endian (LSB first)
    /// </summary>
    /// <remarks>Note - assumes little endian host order</remarks>
    public static byte[] StructureToByteArray(object obj)
    {
        int len = Marshal.SizeOf(obj);
        byte[] arr = new byte[len];
        IntPtr ptr = Marshal.AllocHGlobal(len);
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, len);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// Convert a struct to an array of bytes, struct fields being reperesented in 
    /// big endian (MSB first)
    /// </summary>
    public static byte[] StructureToByteArrayBigEndian(params object[] list)
    {
        // The copy is made becuase SetValue won't work on a struct.
        // Boxing was used because SetValue works on classes/objects.
        // Unfortunately, it results in 2 copy operations.
        object thisBoxed = list[0]; // Why make a copy?
        Type test = thisBoxed.GetType();

        int offset = 0;
        byte[] data = new byte[Marshal.SizeOf(thisBoxed)];

        object fieldValue;
        TypeCode typeCode;

        byte[] temp;

        // Enumerate each structure field using reflection.
        foreach (var field in test.GetFields())
        {
            // field.Name has the field's name.

            fieldValue = field.GetValue(thisBoxed); // Get value

            // Get the TypeCode enumeration. Multiple types get mapped to a common typecode.
            typeCode = Type.GetTypeCode(fieldValue.GetType());

            switch (typeCode)
            {
                case TypeCode.Single: // float
                {
                    temp = BitConverter.GetBytes((Single) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (Single));
                    break;
                }
                case TypeCode.Int32:
                {
                    temp = BitConverter.GetBytes((Int32) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (Int32));
                    break;
                }
                case TypeCode.UInt32:
                {
                    temp = BitConverter.GetBytes((UInt32) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (UInt32));
                    break;
                }
                case TypeCode.Int16:
                {
                    temp = BitConverter.GetBytes((Int16) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (Int16));
                    break;
                }
                case TypeCode.UInt16:
                {
                    temp = BitConverter.GetBytes((UInt16) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (UInt16));
                    break;
                }
                case TypeCode.Int64:
                {
                    temp = BitConverter.GetBytes((Int64) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (Int64));
                    break;
                }
                case TypeCode.UInt64:
                {
                    temp = BitConverter.GetBytes((UInt64) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (UInt64));
                    break;
                }
                case TypeCode.Double:
                {
                    temp = BitConverter.GetBytes((Double) fieldValue);
                    Array.Reverse(temp);
                    Array.Copy(temp, 0, data, offset, sizeof (Double));
                    break;
                }
                case TypeCode.Byte:
                {
                    data[offset] = (Byte) fieldValue;
                    break;
                }
                default:
                {
                    //System.Diagnostics.Debug.Fail("No conversion provided for this type : " + typeCode.ToString());
                    break;
                }
            }
            ; // switch
            if (typeCode == TypeCode.Object)
            {
                int length = ((byte[]) fieldValue).Length;
                Array.Copy(((byte[]) fieldValue), 0, data, offset, length);
                offset += length;
            }
            else
            {
                offset += Marshal.SizeOf(fieldValue);
            }
        } // foreach

        return data;
    } // Swap

    public static MAVLink.message_info GetMessageInfo(this MAVLink.message_info[] source, uint msgid)
    {
        foreach (var item in source)
        {
            if (item.MsgId == msgid)
                return item;
        }

        return new MAVLink.message_info();
    }

    private static Object objLock = new object();


    private static int _packetCount;

    public static byte[] GeneratePacket(IDrone drone, MAVLINK_MSG_ID messageType, object indata)
    {
        return GeneratePacket(drone, (int)messageType, indata);
    }


    /// <summary>
    /// Generate a Mavlink Packet and write to serial
    /// </summary>
    /// <param name="messageType">type number = MAVLINK_MSG_ID</param>
    /// <param name="indata">struct of data</param>
    public static byte[] GeneratePacket(IDrone drone, int messageType, object indata, bool forcemavlink2 = false, bool forcesigning = false)
    {
        lock (objLock)
        {
            var data = MavlinkUtil.StructureToByteArray(indata);
            var packet = new byte[0];
            int i = 0;

            // are we mavlink2 enabled for this sysid/compid
            if (!drone.MavLink2 && messageType < 256 && !forcemavlink2)
            {
                var info = MAVLink.MAVLINK_MESSAGE_INFOS.SingleOrDefault(p => p.MsgId == messageType);
                if (data.Length != info.minlength)
                {
                    Array.Resize(ref data, (int)info.minlength);
                }

                //Console.WriteLine(DateTime.Now + " PC Doing req "+ messageType + " " + this.BytesToRead);
                packet = new byte[data.Length + 6 + 2];

                packet[0] = MAVLINK1_STX;
                packet[1] = (byte)data.Length;
                packet[2] = (byte)_packetCount;

                _packetCount++;

                packet[3] = gcssysid;
                packet[4] = (byte)MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER;
                packet[5] = (byte)messageType;

                i = 6;
                foreach (byte b in data)
                {
                    packet[i] = b;
                    i++;
                }

                ushort checksum = MavlinkCRC.crc_calculate(packet, packet[1] + 6);

                checksum = MavlinkCRC.crc_accumulate(MAVLINK_MESSAGE_INFOS.GetMessageInfo((uint)messageType).crc, checksum);


                byte ck_a = (byte)(checksum & 0xFF); ///< High byte
                byte ck_b = (byte)(checksum >> 8); ///< Low byte

                packet[i] = ck_a;
                i += 1;
                packet[i] = ck_b;
                i += 1;
            }
            else
            {
                // trim packet for mavlink2
                MavlinkUtil.TrimePayload(ref data);

                packet = new byte[data.Length + MAVLINK_NUM_HEADER_BYTES + MAVLINK_NUM_CHECKSUM_BYTES + MAVLINK_SIGNATURE_BLOCK_LEN];

                packet[0] = MAVLINK2_STX;
                packet[1] = (byte)data.Length;
                packet[2] = 0; // incompat
                if (drone.Signing || forcesigning) // current mav
                    packet[2] |= MAVLINK_IFLAG_SIGNED;
                packet[3] = 0; // compat
                packet[4] = (byte)_packetCount;

                _packetCount++;

                packet[5] = gcssysid;
                packet[6] = (byte)MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER;
                packet[7] = (byte)(messageType & 0xff);
                packet[8] = (byte)((messageType >> 8) & 0xff);
                packet[9] = (byte)((messageType >> 16) & 0xff);

                i = 10;
                foreach (byte b in data)
                {
                    packet[i] = b;
                    i++;
                }

                ushort checksum = MavlinkCRC.crc_calculate(packet, packet[1] + MAVLINK_NUM_HEADER_BYTES);

                checksum = MavlinkCRC.crc_accumulate(MAVLINK_MESSAGE_INFOS.GetMessageInfo((uint)messageType).crc, checksum);

                byte ck_a = (byte)(checksum & 0xFF); ///< High byte
                byte ck_b = (byte)(checksum >> 8); ///< Low byte

                packet[i] = ck_a;
                i += 1;
                packet[i] = ck_b;
                i += 1;

                if (drone.Signing || forcesigning)
                {
                    //https://docs.google.com/document/d/1ETle6qQRcaNWAmpG2wz0oOpFKSF_bcTmYMQvtTGI8ns/edit

                    /*
                    8 bits of link ID
                    48 bits of timestamp
                    48 bits of signature
                    */

                    // signature = sha256_48(secret_key + header + payload + CRC + link-ID + timestamp)

                    var timestamp = (UInt64)((DateTime.UtcNow - new DateTime(2015, 1, 1)).TotalMilliseconds * 100);

                    if (timestamp == drone.TimeStamp)
                        timestamp++;

                    drone.TimeStamp = timestamp;

                    var timebytes = BitConverter.GetBytes(timestamp);

                    var sig = new byte[7]; // 13 includes the outgoing hash
                    sig[0] = drone.SendLinkId;
                    Array.Copy(timebytes, 0, sig, 1, 6); // timestamp

                    //Console.WriteLine("gen linkid {0}, time {1} {2} {3} {4} {5} {6} {7}", sig[0], sig[1], sig[2], sig[3], sig[4], sig[5], sig[6], timestamp);

                    var signingKey = drone.SigningKey;

                    if (signingKey == null || signingKey.Length != 32)
                    {
                        signingKey = new byte[32];
                    }

                    using (SHA256Managed signit = new SHA256Managed())
                    {
                        signit.TransformBlock(signingKey, 0, signingKey.Length, null, 0);
                        signit.TransformBlock(packet, 0, i, null, 0);
                        signit.TransformFinalBlock(sig, 0, sig.Length);
                        var ctx = signit.Hash;
                        // trim to 48
                        Array.Resize(ref ctx, 6);

                        foreach (byte b in sig)
                        {
                            packet[i] = b;
                            i++;
                        }

                        foreach (byte b in ctx)
                        {
                            packet[i] = b;
                            i++;
                        }
                    }
                }
            }

            return packet;
        }
    }
}