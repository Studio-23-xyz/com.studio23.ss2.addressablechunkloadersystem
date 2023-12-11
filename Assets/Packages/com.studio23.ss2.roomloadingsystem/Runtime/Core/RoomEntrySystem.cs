using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomEntrySystem:MonoBehaviour, IRoomLoadSubSystem
    {
        public event Action<FloorData> OnFloorEntered;
        public event Action<FloorData> OnFloorExited;
        /// <summary>
        /// Fired when room entered and loaded
        /// </summary>
        public event Action<RoomData> OnRoomEntered;
        /// <summary>
        /// Fired when room exited and unloaded
        /// </summary>
        public event Action<RoomData> OnRoomExited;
        /// <summary>
        /// Fired when room itself + all the required rooms for the entered room have been loaded
        /// </summary>
        public event Action<RoomData> OnEnteredRoomDependenciesLoaded;
        
 
        private async UniTask ExitFloor(FloorData prevFloor)
        {
            Debug.Log("exit floor " + prevFloor, prevFloor);
            OnFloorExited?.Invoke(prevFloor);

            foreach (var roomToUnload in prevFloor.RoomsInFloor)
            {
                _roomsInLoadingRange.Remove(roomToUnload);  
                await RemoveRoomExteriorToLoad(roomToUnload);
            }


            foreach (var roomToUnload in prevFloor.AlwaysLoadRooms)
            {
                await RemoveRoomExteriorToLoad(roomToUnload);
            }
        }
        private void ForceEnterRoom(RoomData room)
        {
            _currentEnteredRoom = room;
            room.HandleRoomEntered();
        }
        public async UniTask EnterRoom(RoomData room)
        {
            if (!RoomLoader.RoomExteriorLoadHandles.ContainsKey(room))
            {
                //the room has been entered but the exterior isn't marked as loaded
                //this is possible if we start in this scene from the editor
                //in which case, exterior is already loaded.
                //we just need to add a dummy handle
                //that won't unload the scene as an addressable.
                
                RoomLoader.addHandleForAlreadyLoadedExterior(room);
            }
            if (_currentEnteredRoom != room)
            {
                var prevFloor = CurrentFloor;
                var prevRoom = _currentEnteredRoom;

                _currentEnteredRoom = room;
                bool isDifferentFloor = prevFloor != _currentEnteredRoom.Floor;
                
                if (prevRoom != null)
                {
                    ExitRoom(prevRoom);
                    if(isDifferentFloor && prevFloor != null)
                    {
                        ExitFloor(prevFloor);
                    }
                }

                
                ForceEnterRoom(room);
                OnRoomEntered?.Invoke(room);
                await AddRoomInteriorToLoad(room);

                loadRoomDependencies(room, isDifferentFloor);
            }
        }
        
        
        private async UniTask ExitRoom(RoomData prevRoom)
        {
            prevRoom.HandleRoomExited();
            OnRoomExited?.Invoke(prevRoom);
            await RemoveRoomInteriorToLoad(prevRoom);
            foreach (var adjacentRoom in prevRoom.AlwaysLoadRooms)
            {
                await RemoveRoomExteriorToLoad(adjacentRoom);
            }
        }
    }
}