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

        private void Start()
        {
            foreach (var floor in RoomManager.Instance.AllFloors)
            {
                floor.Initialize();

                floor.OnFloorEntered += OnFloorEntered;
                floor.OnFloorExited += OnFloorExited;

                foreach (var roomData in floor.RoomsInFloor)
                {
                    roomData.OnRoomEntered += OnRoomEntered;
                    roomData.OnRoomExited += OnRoomExited;
                }
            }
        }
        
        private void OnDestroy()
        {
            if(RoomManager.Instance == null)
                return;
            foreach (var floor in RoomManager.Instance.AllFloors)
            {
                floor.OnFloorEntered -= OnFloorEntered;
                floor.OnFloorExited -= OnFloorExited;

                foreach (var roomData in floor.RoomsInFloor)
                {
                    roomData.OnRoomEntered -= OnRoomEntered;
                    roomData.OnRoomExited -= OnRoomExited;
                }
            }
        }


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
        
        private async UniTask loadRoomDependencies(RoomData room, bool floorNewlyEntered)
        {
            foreach (var adjacentRoom in room.AlwaysLoadRooms)
            {
                Debug.Log(room+ $" always load {adjacentRoom}");
                await AddRoomExteriorToLoad(adjacentRoom);
            }
            if (floorNewlyEntered)
            {
                Debug.Log("laod room dep floorNewlyEntered " + floorNewlyEntered);
                if(room.Floor != null)
                {
                    OnFloorEntered?.Invoke(room.Floor);
                    foreach (var roomToLoad in room.Floor.AlwaysLoadRooms)
                    {
                        await AddRoomExteriorToLoad(roomToLoad);
                    }
                }
            }
                
            OnEnteredRoomDependenciesLoaded?.Invoke(room);
        }
    }
}