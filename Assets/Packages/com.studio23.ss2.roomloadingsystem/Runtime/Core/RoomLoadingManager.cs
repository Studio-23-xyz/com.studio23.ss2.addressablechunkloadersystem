using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using NaughtyAttributes;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomLoadingManager:MonoBehaviourSingletonPersistent<RoomLoadingManager>
    {
        public event Action<FloorData> OnFloorEntered;
        public event Action<FloorData> OnFloorExited;
        public event Action<RoomData> OnRoomEntered;
        public event Action<RoomData> OnRoomExited;
        
        [ShowNativeProperty] public RoomData CurrentEnteredRoom { get; private set; }
        [ShowNativeProperty] public FloorData CurrentFloor => CurrentEnteredRoom ? CurrentEnteredRoom.Floor : null;
        private HashSet<RoomData> _mustLoadRooms;
        private HashSet<RoomData> _roomsInLoadingRange;
        private HashSet<RoomData> _roomsToLoad;

        
        protected override void Initialize()
        {
            _mustLoadRooms = new HashSet<RoomData>();
            _roomsToLoad = new HashSet<RoomData>();
            _roomsInLoadingRange = new HashSet<RoomData>();
        }

        // public void AddRoomToLoad(RoomData room, IRoomLoader roomLoader)
        // {
        //     if (_roomLoadRequesters.TryGetValue(room, out var roomRequesters))
        //     {
        //         if (roomRequesters.Add(roomLoader))
        //         {
        //             
        //         }
        //     }
        //     else
        //     {
        //         //this is the first time this room was entered
        //         var newRequesterSet = new HashSet<IRoomLoader> { roomLoader };
        //         _roomLoadRequesters.Add(room, newRequesterSet);
        //         
        //         OnRoomEntered?.Invoke(room);
        //     }
        // }

        public void EnterRoom(RoomData room)
        {
            if (CurrentEnteredRoom != room)
            {
                ForceExitCurrentRoom();
                ForceEnterRoom(room);
            }
        }

        private void ForceEnterRoom(RoomData room)
        {
            CurrentEnteredRoom = room;
            room.HandleRoomEntered();
            OnRoomEntered?.Invoke(room);
        }

        public virtual bool CheckIfRoomShouldBeLoaded(RoomData room)
        {
            if (CurrentEnteredRoom == room)
                return true;
            
            if (_mustLoadRooms.Contains(room))
            {
                return true;
            }
            
            if (CurrentEnteredRoom != null)
            {
                if (CurrentEnteredRoom.IsAdjacentTo(room))
                {
                    return true;
                }

                if (CurrentFloor.WantsToAlwaysLoad(room))
                {
                    return true;
                }
            }

            return true;
        }


        public void SetRoomAsMustLoad(RoomData room)
        {
            _mustLoadRooms.Add(room);
        }
        
        public void unsetRoomAsMustLoad(RoomData room)
        {
            _mustLoadRooms.Remove(room);
        }

        public void HandleRoomEnteredLoadingRange(RoomData room)
        {
            if (_roomsInLoadingRange.Add(room))
            {
                
            }
        }
        
        public void HandleRoomExitedLoadingRange(RoomData room)
        {
            if (_roomsInLoadingRange.Remove(room))
            {
                
            }
        }


        public void addRoomToLoad(RoomData room)
        {
            if (_roomsToLoad.Add(room))
            {
                
            }
        }

        public void ExitRoom(RoomData room)
        {
            if (CurrentEnteredRoom == room)
            {
                if (CurrentEnteredRoom != null)
                {
                    ForceExitCurrentRoom();
                }
            }
        }

        private void ForceExitCurrentRoom()
        {
            if (CurrentEnteredRoom != null)
            {
                CurrentEnteredRoom.HandleRoomExited();
                OnRoomExited?.Invoke(CurrentEnteredRoom);
                CurrentEnteredRoom = null;
            }
        }
    }
}