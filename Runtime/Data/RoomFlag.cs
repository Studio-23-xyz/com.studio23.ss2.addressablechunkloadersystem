using System;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Data
{
    [Flags]
    public enum RoomFlag
    {
        None = 0,
        IsCurrentRoom = 1 << 0,
        IsCurrentFloorMustLoad = 1 << 1,
        IsCurrentRoomMustLoad = 1 << 2,
        IsGeneralMustLoad = 1 << 3,
        IsInLoadingRange = 1<< 4,
    }
    //#TODO refactor into class
}