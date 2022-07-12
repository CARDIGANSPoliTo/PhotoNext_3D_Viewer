using System;

[Serializable]
public enum TypeMessage : byte
{
    DataPacket,
    RequestConfig,
    ConfigPacket,
    RequestDataStart,
    RequestDataEnd,
    EndThreadPacket
};