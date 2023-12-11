using System;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    [Flags]
    public enum RoomFlag
    {
        None = 0,
        IsInLoadingRange = 1 << 0,
        IsCurrentFloorMustLoad = 1 << 1,
        IsCurrentRoomMustLoad = 1 << 1,
    }
}