using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core
{
    [RequireComponent(typeof(RoomLoader))]
    public class RoomManager:MonoBehaviourSingletonPersistent<RoomManager>
    {
        [SerializeField] List<FloorData> _allFloors;
        private RoomLoader _roomLoader;
        private bool _isUnloading = false;
        public RoomLoader  RoomLoader=> _roomLoader;
        
        public event Action<FloorData> OnFloorEntered;
        public event Action<FloorData> OnFloorExited;

        /// <summary>
        /// Fired when room entered and loaded
        /// NOTE: DOES NOT FIRE IF THE ROOM'S FLOOR IS NOT IN _allFloors
        /// EX: STAIRS
        /// DIRECTLY SUB TO THE SCRIPTABLE IN SUCH CASES
        /// </summary>
        public event Action<RoomData> OnRoomEntered;

        /// <summary>
        /// Fired when room entered and loaded
        /// NOTE: DOES NOT FIRE IF THE ROOM'S FLOOR IS NOT IN _allFloors
        /// EX: STAIRS
        /// DIRECTLY SUB TO THE SCRIPTABLE IN SUCH CASES
        /// </summary>
        public event Action<RoomData> OnRoomExited;

        public RoomData CurrentEnteredRoom => _currentEnteredRoom;
        [Required] [SerializeField] private RoomData _currentEnteredRoom;

        [ShowNativeProperty]
        public FloorData CurrentFloor => _currentEnteredRoom ? _currentEnteredRoom.Floor : null;

        public Transform Player;
        
        protected override void Initialize()
        {
            _roomLoader = GetComponent<RoomLoader>();

            foreach (var floor in _allFloors)
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
            foreach (var floor in _allFloors)
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


        private void Update()
        {
            if(_willGetDestroyed || _isUnloading)
                return;
            
            if (Player != null)
            {
                UpdateRoomsInPlayerRange();
            }
            //called explicitly to ensure that timer starts on same frame
            RoomLoader.UpdateRoomUnloadTimer();
        }


        private void UpdateRoomsInPlayerRange()
        {
            if (_currentEnteredRoom == null)
                return;
            if (CurrentFloor == null)
                return;
            foreach (var roomData in CurrentFloor.RoomsInFloor)
            {
                if (roomData.IsPosInLoadingRange(Player.transform.position) ||
                    CurrentEnteredRoom == roomData)
                {
                    HandleRoomEnteredLoadingRange(roomData);
                }
                else
                {
                    HandleRoomExitedLoadingRange(roomData);
                }
            }
        }


        public void SetRoomAsMustLoad(RoomData room)
        {
            AddRoomExteriorFlag(room, RoomFlag.IsGeneralMustLoad);
            AddRoomInteriorFlag(room, RoomFlag.IsGeneralMustLoad);
        }

        public void UnsetRoomAsMustLoad(RoomData room)
        {
            RemoveExteriorRoomFlag(room, RoomFlag.IsGeneralMustLoad);
            RemoveInteriorRoomFlag(room, RoomFlag.IsGeneralMustLoad);
        }

        public void RemoveExteriorRoomFlag(RoomData room, RoomFlag flags)
        {
            _roomLoader.RemoveRoomExteriorLoadFlag(room, flags);
        }
        
        public void RemoveInteriorRoomFlag(RoomData room, RoomFlag flags)
        {
            _roomLoader.RemoveRoomInteriorLoadFlag(room, flags);
        }

        public void HandleRoomEnteredLoadingRange(RoomData room)
        {
            AddRoomExteriorFlag(room, RoomFlag.IsInLoadingRange);
        }

        public void HandleRoomExitedLoadingRange(RoomData room)
        {
            //actually unloading the room requries waiting for timer
            //handled in different function
            _roomLoader.RemoveRoomExteriorLoadFlag(room, RoomFlag.IsInLoadingRange);
        }

        RoomLoadHandle AddRoomExteriorFlag(RoomData room, RoomFlag flags)
        {
            return _roomLoader.AddExteriorLoadRequest(new RoomLoadRequestData(room), flags);
        }
        
        internal async UniTask AddRoomExteriorFlagAndWait(RoomData room, RoomFlag flags)
        {
            var handle = _roomLoader.AddExteriorLoadRequest(new RoomLoadRequestData(room), flags);
            await handle.LoadScene();
        }
        
        internal async UniTask AddRoomInteriorFlagAndWait(RoomData room, RoomFlag flags)
        {
            var handle = _roomLoader.AddInteriorLoadRequest(new RoomLoadRequestData(room), flags);
            await handle.LoadScene();
        }
        
        RoomLoadHandle AddRoomInteriorFlag(RoomData room, RoomFlag flags)
        {
            return _roomLoader.AddInteriorLoadRequest(new RoomLoadRequestData(room), flags);
        }

        

        public async UniTask EnterRoom(RoomData room, bool forceLoadIfMissing = false)
        {
            if (_isUnloading)
            {
                Debug.LogWarning("Can't enter room when UNLOADING", gameObject);
                return;
            }
            
            bool isAlreadyLoadedRoom = _currentEnteredRoom == null && !_roomLoader.RoomInteriorLoadHandles.ContainsKey(room) && !forceLoadIfMissing;
            if (isAlreadyLoadedRoom)
            {
                //the room has been entered but the exterior isn't marked as loaded
                //this is possible if we start in this scene from the editor
                //in which case, exterior is already loaded.
                //we just need to add a dummy handle
                //that won't unload the scene as an addressable.
                Debug.Log("AddHandleForAlreadyLoadedInterior " + room, room);
                _roomLoader.AddHandleForAlreadyLoadedRoom(room, RoomFlag.IsCurrentRoom);
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
                    if (isDifferentFloor && prevFloor != null)
                    {
                        ExitFloor(prevFloor);
                    }
                }
                
                ForceEnterRoom(room);

                if (!isAlreadyLoadedRoom)
                {
                    Debug.Log($"load new room {room}");
                    await AddRoomExteriorFlagAndWait(room, RoomFlag.IsCurrentRoom);
                    await AddRoomInteriorFlagAndWait(room, RoomFlag.IsCurrentRoom);
                }
                else
                {
                    Debug.Log($"already loaded room {room}");
                }

                OnRoomEntered?.Invoke(room);

                LoadCurrentRoomDependencies(room, isDifferentFloor);
            }
        }

        //#TODO better unload 
        public async UniTask UnloadAllRooms()
        {
            if (_isUnloading)
            {
                return;
            }
            Debug.Log("start unloading all rooms");

            _isUnloading = true;
            foreach (var (room,  handle) in _roomLoader.RoomExteriorLoadHandles)
            {
                await handle.UnloadScene();
            }
            foreach (var (room,  handle) in _roomLoader.RoomInteriorLoadHandles)
            {
                await handle.UnloadScene();
            }
            _roomLoader.RoomExteriorLoadHandles.Clear();
            _roomLoader.RoomInteriorLoadHandles.Clear();

            _currentEnteredRoom = null;
            _isUnloading = false;
            Debug.Log("unloaded all rooms");
        }
        
        public float LoadingPercentageForRoom(RoomData room, bool considerInterior, bool includeMustLoadRooms)
        {
            float progress = 0;
            int numRoomsToLoad = 1;
            if (considerInterior)
            {
                numRoomsToLoad++;
            }

            RoomLoadHandle handle;
            
            numRoomsToLoad += room.AlwaysLoadRooms.Count;
            if (includeMustLoadRooms && 
                _roomLoader.RoomExteriorLoadHandles.TryGetValue(room, out handle))
            {
                progress += handle.GetLoadingPercentage();
            }

            numRoomsToLoad += room.Floor == null ? room.Floor.AlwaysLoadRooms.Count : 0;
            if (includeMustLoadRooms && 
                _roomLoader.RoomExteriorLoadHandles.TryGetValue(room, out handle))
            {
                progress += handle.GetLoadingPercentage();
            }
    
            return progress/numRoomsToLoad;
        }

        private void ExitFloor(FloorData prevFloor)
        {
            Debug.Log("exit floor " + prevFloor, prevFloor);
            OnFloorExited?.Invoke(prevFloor);

            foreach (var roomToUnload in prevFloor.RoomsInFloor)
            {
                RemoveExteriorRoomFlag(roomToUnload, RoomFlag.IsInLoadingRange);
            }
            
            foreach (var roomToUnload in prevFloor.AlwaysLoadRooms)
            {
                RemoveExteriorRoomFlag(roomToUnload, RoomFlag.IsCurrentFloorMustLoad);
            }
        }

        private void ExitRoom(RoomData prevRoom)
        {
            prevRoom.HandleRoomExited();
            RemoveExteriorRoomFlag(prevRoom, RoomFlag.IsCurrentRoom);
            RemoveInteriorRoomFlag(prevRoom, RoomFlag.IsCurrentRoom);
            
            foreach (var roomToUnload in prevRoom.AlwaysLoadRooms)
            {
                // note:
                // if this is the only flag, this can start an unload
                // say, the next room/floor has this room as an AlwaysLoadRoom
                // then the room would be loaded again
                // this is only in the edge case where the timer runs out between swapping rooms
                // so not fixing atm
                RemoveExteriorRoomFlag(roomToUnload, RoomFlag.IsCurrentFloorMustLoad);
            }
        }

        private async UniTask LoadCurrentRoomDependencies(RoomData room, bool floorNewlyEntered)
        {
            //NOTE:FLAGS are set after the previous room is loaded
            //it is not syncronous
            //a better way would be to set all the flags and then wait on the handles
            foreach (var adjacentRoom in room.AlwaysLoadRooms)
            {
                await AddRoomExteriorFlagAndWait(adjacentRoom, RoomFlag.IsCurrentFloorMustLoad);
            }
    
            if (floorNewlyEntered)
            {
                if (room.Floor != null)
                {
                    OnFloorEntered?.Invoke(room.Floor);
                    foreach (var roomToLoad in room.Floor.AlwaysLoadRooms)
                    {
                        await AddRoomExteriorFlagAndWait(roomToLoad, RoomFlag.IsCurrentFloorMustLoad);
                    }
                }
            }
            
            room.HandleRoomDependenciesLoaded();
        }



        
        /// <summary>
        /// Returns if the exterior for the room is loaded
        /// NOTE: For rooms with invalid exterior scene asset ref, this returns false
        /// </summary>
        /// <param name="roomData"></param>
        /// <returns></returns>
        public bool IsRoomExteriorLoaded(RoomData roomData)
        {
            if (_roomLoader.RoomExteriorLoadHandles.TryGetValue(roomData, out var handle))
            {
                return handle.IsLoaded();
            }

            return false;
        }
        


        /// <summary>
        /// Returns if the exterior for the room is loaded
        /// NOTE: For rooms with invalid exterior scene asset ref, this returns false
        /// </summary>
        /// <param name="roomData"></param>
        /// <returns></returns>
        public bool IsRoomInteriorLoaded(RoomData roomData)
        {
            if (_roomLoader.RoomInteriorLoadHandles.TryGetValue(roomData, out var handle))
            {
                return handle.IsLoaded();
            }

            return false;
        }
        


        [Button]
        void PrintRoomsInLoadingRange()
        {
            foreach (var (room, handle) in _roomLoader.RoomExteriorLoadHandles)
            {
                if (handle.HasFlag(RoomFlag.IsInLoadingRange))
                {
                    Debug.Log($"{room} in loading range");
                }
            }
        }
        [Button]
        void PrintRoomsInMustLoad()
        {
            foreach (var (room, handle) in _roomLoader.RoomExteriorLoadHandles)
            {
                if (handle.HasFlag(RoomFlag.IsGeneralMustLoad))
                {
                    Debug.Log($"{room} is GeneralMustLoad");
                }
                if (handle.HasFlag(RoomFlag.IsCurrentRoomMustLoad))
                {
                    Debug.Log($"{room} is Current room MustLoad");
                }
                if (handle.HasFlag(RoomFlag.IsCurrentFloorMustLoad))
                {
                    Debug.Log($"{room} is Current floor MustLoad");
                }
            }
        }

        private void ForceEnterRoom(RoomData room)
        {
            _currentEnteredRoom = room;
            room.HandleRoomEntered();
        }
    }
}