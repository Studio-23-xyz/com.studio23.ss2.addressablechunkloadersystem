using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class FloorData:ScriptableObject
    {
        public List<RoomData> RoomsInFloor;
        [SerializeField] List<RoomData> AlwaysLoadRooms;

        public event Action<FloorData> OnFloorEntered;
        public event Action<FloorData> OnFloorExited;

        public void Initialize()
        {
            foreach (var roomData in RoomsInFloor)
            {
                roomData.Initialize(this);
            }
        }
        
        public void HandleFloorEntered()
        {
            OnFloorEntered?.Invoke(this);
        }
        
        public void HandleFloorExited()
        {
            OnFloorExited?.Invoke(this);
        }

        public bool WantsToAlwaysLoad(RoomData room)
        {
            return AlwaysLoadRooms.Contains(room);
        }
    }
}