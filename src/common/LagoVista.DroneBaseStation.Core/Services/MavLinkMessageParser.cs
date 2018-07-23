using LagoVista.DroneBaseStation.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.Services
{
    public class MavLinkMessageParser : IMavLinkMessageParser
    {
        public event EventHandler<MAVLinkMessage> MessageParsed;

        MAVLinkMessage _currentMessage;

        enum MavLinkParserStates
        {
            ExpectingStx, /* 0x00 */
            Expecting2PayloadLen, /* 0x01 */
            Expecting2IncompatFlags, /* 0x02 */
            Expecting2CompatFlag, /* 0x03 */
            Expecting2Seq, /* 0x04 */
            Expecting2SysId, /* 0x05 */
            Expecting2ComponentId, /* 0x06 */
            Expecting2MsgId07, /* 0x07 */
            Expecting2MsgId815, /*0x08 */
            Expecting2MsgId1623, /* 0x09 */
            Expecting2TargetSysId, /* 10 */
            Expecting2TargetComponentId, /* 11 */
            ReadingPayload2, /* 12 to 12 + LEN */
            Expecting2CheckSum1, /* 12 + LEN */
            Expecting2CheckSum2, /* 13 + LEN */
            Expecting2Signature, /* 14 + LEN -> (14 + LEN + 13) */

            ExpectingPayloadLen,
            ExpectingSeqNumber,
            ExpectingSystemId,
            ExpectingComponentId,
            ExpectingMessageId,
            ReadingPayloadData,
            ExpectingCRC1,
            ExpectingCRC2
        }

        MavLinkParserStates _mavLinkParserState = MavLinkParserStates.ExpectingStx;

        int _signatureIndex = 0;
        const int SIGNATURE_LEN = 13;

        public void HandleBuffer(byte[] buffer, int readCount)
        {
            for (var idx = 0; idx < readCount; ++idx)
            {
                var value = (byte)buffer[idx];
                if (_currentMessage != null)
                {
                    _currentMessage.add_byte(value);
                }

                switch (_mavLinkParserState)
                {
                    case MavLinkParserStates.ExpectingStx:
                        if (value == MAVLINK2_STX)
                        {
                            _currentMessage = new MAVLinkMessage();
                            _currentMessage.add_byte(value);
                            _mavLinkParserState = MavLinkParserStates.Expecting2PayloadLen;
                        }
                        if (value == 254)
                        {
                            _currentMessage = new MAVLinkMessage();
                            _currentMessage.add_byte(value);
                            _mavLinkParserState = MavLinkParserStates.ExpectingPayloadLen;
                        }

                        break;
                    case MavLinkParserStates.Expecting2PayloadLen:
                        _currentMessage.payloadlength = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2IncompatFlags;
                        break;
                    case MavLinkParserStates.Expecting2IncompatFlags:
                        _currentMessage.incompat_flags = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2CompatFlag;
                        break;
                    case MavLinkParserStates.Expecting2CompatFlag:
                        _currentMessage.compat_flags = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2Seq;
                        break;
                    case MavLinkParserStates.Expecting2Seq:
                        _currentMessage.seq = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2SysId;
                        break;
                    case MavLinkParserStates.Expecting2SysId:
                        _currentMessage.sysid = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2ComponentId;
                        break;
                    case MavLinkParserStates.Expecting2ComponentId:
                        _currentMessage.compid = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2MsgId07;
                        _currentMessage.msgid = 0x00;
                        break;
                    case MavLinkParserStates.Expecting2MsgId07:
                        _currentMessage.msgid = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2MsgId815;
                        break;
                    case MavLinkParserStates.Expecting2MsgId815:
                        _mavLinkParserState = MavLinkParserStates.Expecting2MsgId1623;
                        _currentMessage.msgid += (UInt32)(value << 8);
                        break;
                    case MavLinkParserStates.Expecting2MsgId1623:
                        _mavLinkParserState = MavLinkParserStates.Expecting2TargetSysId;
                        _currentMessage.msgid += (UInt32)(value << 16);
                        break;
                    case MavLinkParserStates.Expecting2TargetSysId:
                        _currentMessage.targetsysid = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2TargetComponentId;
                        break;
                    case MavLinkParserStates.Expecting2TargetComponentId:
                        _currentMessage.targetcomponentid = value;
                        _currentMessage.payload_index = 0;
                        _mavLinkParserState = MavLinkParserStates.ReadingPayload2;
                        break;
                    case MavLinkParserStates.ReadingPayload2:
                        if (_currentMessage.payload_index < _currentMessage.payloadlength)
                        {
                            _currentMessage.payload[_currentMessage.payload_index++] = value;
                        }

                        if (_currentMessage.payload_index == _currentMessage.payloadlength)
                        {
                            _mavLinkParserState = MavLinkParserStates.Expecting2CheckSum1;
                        }

                        break;
                    case MavLinkParserStates.Expecting2CheckSum1:
                        _currentMessage.crc16 = value;
                        _mavLinkParserState = MavLinkParserStates.Expecting2CheckSum2;
                        break;
                    case MavLinkParserStates.Expecting2CheckSum2:
                        _currentMessage.crc16 += (UInt16)(value << 8);
                        _mavLinkParserState = MavLinkParserStates.Expecting2Signature;
                        _currentMessage.sig = new byte[13];
                        _signatureIndex = 0;
                        break;
                    case MavLinkParserStates.Expecting2Signature:
                        _currentMessage.sig[_signatureIndex++] = value;
                        if (_signatureIndex == SIGNATURE_LEN)
                        {
                            Debug.WriteLine($"MAVLINK2 - Message Id: {_currentMessage.msgid} - {_currentMessage.payloadlength} - {_currentMessage.seq} - {_currentMessage.crc16}");

                            _currentMessage.processBuffer(_currentMessage.buffer);
                            Debug.WriteLine($"MAVLINK2 - Message Id: {_currentMessage.msgid} - {_currentMessage.payloadlength} - {_currentMessage.seq} - {_currentMessage.crc16}");
                            _mavLinkParserState = MavLinkParserStates.ExpectingStx;
                            _currentMessage = null;
                        }
                        break;



                    case MavLinkParserStates.ExpectingPayloadLen:
                        _currentMessage.payloadlength = value;
                        _mavLinkParserState = MavLinkParserStates.ExpectingSeqNumber;
                        break;
                    case MavLinkParserStates.ExpectingSeqNumber:
                        _currentMessage.seq = value;
                        _mavLinkParserState = MavLinkParserStates.ExpectingSystemId;
                        break;
                    case MavLinkParserStates.ExpectingSystemId:
                        _currentMessage.sysid = value;
                        _mavLinkParserState = MavLinkParserStates.ExpectingComponentId;
                        break;
                    case MavLinkParserStates.ExpectingComponentId:
                        _currentMessage.compid = value;
                        _mavLinkParserState = MavLinkParserStates.ExpectingMessageId;
                        break;
                    case MavLinkParserStates.ExpectingMessageId:
                        _currentMessage.msgid = value;
                        _mavLinkParserState = MavLinkParserStates.ReadingPayloadData;
                        break;
                    case MavLinkParserStates.ReadingPayloadData:
                        if (_currentMessage.payload_index < _currentMessage.payloadlength)
                        {
                            _currentMessage.payload[_currentMessage.payload_index++] = value;
                        }

                        if (_currentMessage.payload_index == _currentMessage.payloadlength)
                        {
                            _currentMessage.MessageInfo = MAVLink.MAVLINK_MESSAGE_INFOS.GetMessageInfo(_currentMessage.msgid);

                            _currentMessage.crc16Calc = MavlinkCRC.crc_calculate(_currentMessage.buffer, _currentMessage.buffer_index);
                            _currentMessage.crc16Calc = MavlinkCRC.crc_accumulate(_currentMessage.MessageInfo.crc, _currentMessage.crc16Calc);

                            _mavLinkParserState = MavLinkParserStates.ExpectingCRC1;

                        }
                        break;
                    case MavLinkParserStates.ExpectingCRC1:
                        _currentMessage.crc16 = value;
                        _mavLinkParserState = MavLinkParserStates.ExpectingCRC2;
                        break;
                    case MavLinkParserStates.ExpectingCRC2:
                        _currentMessage.crc16 += (UInt16)(value << 8);

                        if ((_currentMessage.crc16 >> 8) != (_currentMessage.crc16Calc >> 8) || (_currentMessage.crc16 & 0xff) != (_currentMessage.crc16Calc & 0xff))
                        {                            
                            Debug.WriteLine($"INVALID CHECK SUM MAVLINK1 - Message Id: {_currentMessage.msgid} - {_currentMessage.payloadlength} - {_currentMessage.seq} - {_currentMessage.crc16:x2} - {_currentMessage.crc16Calc:x2}");
                        }
                        else
                        {
                            MessageParsed?.Invoke(this, _currentMessage);
                            Debug.WriteLine($"VALID CHECK SUM MAVLINK1 - Message Id: {_currentMessage.msgid} - {_currentMessage.payloadlength} - {_currentMessage.seq} - {_currentMessage.crc16:x2} - {_currentMessage.crc16Calc:x2}");
                        }
                        _currentMessage.processBuffer(_currentMessage.buffer);
                        _currentMessage = null;

                        _mavLinkParserState = MavLinkParserStates.ExpectingStx;
                        break;
                }
            }
        }
    }
}