﻿using irsdkSharp.Models;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Session;
using System;
using System.Collections.Generic;
using System.Text;

namespace irsdkSharp.Serialization
{
    public static class IRacingSDKExtensions
    {
        public static IRacingSessionModel GetSerializedSessionInfo(this IRacingSDK racingSDK)
        {
            if (racingSDK.IsInitialized && racingSDK.Header != null)
            {
                byte[] data = new byte[racingSDK.Header.SessionInfoLength];
                IRacingSDK.GetFileMapView(racingSDK).ReadArray(racingSDK.Header.SessionInfoOffset, data, 0, racingSDK.Header.SessionInfoLength);

                //Serialise the string into objects, tada!
                return IRacingSessionModel.Serialize(Encoding.Default.GetString(data).TrimEnd(new char[] { '\0' }));
            }
            return null;
        }

        public static IRacingDataModel GetSerializedData(this IRacingSDK racingSDK)
        {
            if (racingSDK.IsInitialized)
            {
                var length = (int)IRacingSDK.GetFileMapView(racingSDK).Capacity;
                var data = new byte[length];
                IRacingSDK.GetFileMapView(racingSDK).ReadArray(0, data, 0, length);

                //Get header
                var header = new IRacingSdkHeader(data);
                var headers = GetVarHeaders(header, data);

                //Serialise the string into objects, tada!
                return IRacingDataModel.Serialize(data[header.Buffer..(header.Buffer + header.BufferLength)], headers);
            }
            return null;
        }

        private static List<VarHeader> GetVarHeaders(IRacingSdkHeader header, Span<byte> span)
        {
            var nullChar = new char[] { '\0' };
            var headers = new List<VarHeader>();
            for (int i = 0; i < header.VarCount; i++)
            {
                int type = BitConverter.ToInt32(span.Slice(header.VarHeaderOffset + (i * VarHeader.Size)));
                int offset = BitConverter.ToInt32(span.Slice(header.VarHeaderOffset + (i * VarHeader.Size) + IRacingSDK.VarOffsetOffset));
                int count = BitConverter.ToInt32(span.Slice(header.VarHeaderOffset + (i * VarHeader.Size) + IRacingSDK.VarCountOffset));
                string nameStr = Encoding.Default
                    .GetString(span.Slice(header.VarHeaderOffset + (i * VarHeader.Size) + IRacingSDK.VarNameOffset, Constants.MaxString))
                    .TrimEnd(nullChar);
                string descStr = Encoding.Default
                    .GetString(span.Slice(header.VarHeaderOffset + (i * VarHeader.Size) + IRacingSDK.VarDescOffset, Constants.MaxDesc))
                    .TrimEnd(nullChar);
                string unitStr = Encoding.Default
                    .GetString(span.Slice(header.VarHeaderOffset + (i * VarHeader.Size) + IRacingSDK.VarUnitOffset, Constants.MaxString))
                    .TrimEnd(nullChar);
                headers.Add(new VarHeader(type, offset, count, nameStr, descStr, unitStr));
            }
            return headers;
        }
    }

}