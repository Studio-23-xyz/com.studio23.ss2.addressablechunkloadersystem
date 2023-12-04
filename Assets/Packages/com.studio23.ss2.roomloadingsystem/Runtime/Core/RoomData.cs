using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomData:ScriptableObject
    {
        [ShowNativeProperty] public Vector3 WorldPosition { get; internal set; }
        [ShowNativeProperty] public FloorData Floor { get; internal set; }

        [SerializeField] List<RoomData> _adjacentRooms;
        HashSet<RoomData> _adjacentRoomSet;

        public float RoomLoadRadius = 4; 
        //#TODO figure out how to include FMOD
        //List<FMODBank> banks

        public event Action<RoomData> OnRoomEntered;
        public event Action<RoomData> OnRoomExited;
        
        public void Initialize(FloorData floor)
        {
            Floor = floor;
            _adjacentRoomSet = _adjacentRooms.ToHashSet();
        }

        public void HandleRoomEntered()
        {
            OnRoomEntered?.Invoke(this);
        }
        
        public void HandleRoomExited()
        {
            OnRoomExited?.Invoke(this);
        }

        public bool IsAdjacentTo(RoomData room)
        {
            return _adjacentRooms.Contains(room);
        }
        public bool IsPosInLoadingRange(Vector3 position)
        {
            return (position - WorldPosition).magnitude <= RoomLoadRadius;
        }
    }
}