using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using BDeshi.Logging;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core
{
    [RequireComponent(typeof(RoomLoader))]
    [RequireComponent(typeof(RoomMemorySaver))]
    public class RoomManager:MonoBehaviourSingletonPersistent<RoomManager>
    {
        [SerializeField] List<AssetReferenceT<FloorData>> _allFloorAssets;
        [SerializeField] List<FloorData> _allFloors = new List<FloorData>();
        public bool CanLoadNewRoomsInRange = true;
        public bool CanUnloadRoomsOutOfRange = true;

        private RoomLoader _roomLoader;
        private bool _isUnloading = false;
        public RoomLoader  RoomLoader=> _roomLoader;
        [SerializeField] RoomMemorySaver _roomMemorySaver;
        public RoomMemorySaver RoomMemorySaver => _roomMemorySaver;
        
        
        public event Action<FloorData> OnFloorEntered;
        public event Action<FloorData> OnFloorExited;
        
        /// <summary>
        /// Fired when room loaded
        /// NOTE: DOES NOT FIRE IF THE ROOM'S FLOOR IS NOT IN _allFloors
        /// EX: STAIRS
        /// DIRECTLY SUB TO THE SCRIPTABLE IN SUCH CASES
        /// </summary>
        public event Action<RoomData> OnRoomLoaded;

        /// <summary>
        /// Fired when room loaded
        /// NOTE: DOES NOT FIRE IF THE ROOM'S FLOOR IS NOT IN _allFloors
        /// EX: STAIRS
        /// DIRECTLY SUB TO THE SCRIPTABLE IN SUCH CASES
        /// </summary>
        public event Action<RoomData> OnRoomUnloaded;

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

        public ICategoryLogger<RoomLoadLogCategory> Logger => _logger; 
        [SerializeField] SerializableCategoryLogger<RoomLoadLogCategory> _logger = new ((RoomLoadLogCategory)~0); 
        
        protected override void Initialize()
        {
            _logger.DefaultContext = this;
            
            _isUnloading = false;
            _roomLoader = GetComponent<RoomLoader>();
            _roomMemorySaver = GetComponent<RoomMemorySaver>();
            foreach (var floorAssetRef in _allFloorAssets)
            {
                var loadHandle = floorAssetRef.LoadAssetAsync();
                var floor = loadHandle.WaitForCompletion();
                _allFloors.Add(floor);
            }
            InitializeFloors(_allFloors);
        }

        private void InitializeFloors(List<FloorData> floors)
        {
            _allFloors = floors;
            SubToFloor();
        }

  

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnsubToFloor();
            }
        }

        public void ChangeFloorSetup(List<FloorData> floors)
        {
            if (_allFloors != null)
            {
                UnsubToFloor();
            }

            _logger.Log(RoomLoadLogCategory.FloorEnter,"FLoor change");
            InitializeFloors(floors);
        }
        
        private void SubToFloor()
        {
            foreach (var floor in _allFloors)
            {
                _logger.Log(RoomLoadLogCategory.FloorEnter,$"Sub to floor {floor}");
                floor.Initialize();

                floor.OnFloorEntered += OnFloorEntered;
                floor.OnFloorExited += OnFloorExited;

                foreach (var roomData in floor.RoomsInFloor)
                {
                    roomData.OnRoomEntered += OnRoomEntered;
                    roomData.OnRoomExited += OnRoomExited;
                    roomData.OnRoomExteriorLoaded += OnRoomLoaded;
                    roomData.OnRoomExteriorUnloaded += OnRoomUnloaded;
                }
            }
        }

        private void UnsubToFloor()
        {
            foreach (var floor in _allFloors)
            {
                floor.OnFloorEntered -= OnFloorEntered;
                floor.OnFloorExited -= OnFloorExited;

                foreach (var roomData in floor.RoomsInFloor)
                {
                    roomData.OnRoomEntered -= OnRoomEntered;
                    roomData.OnRoomExited -= OnRoomExited;
                    roomData.OnRoomExteriorLoaded -= OnRoomLoaded;
                    roomData.OnRoomExteriorUnloaded -= OnRoomUnloaded;
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
                if (roomData.CanBeLoaded(Player.transform.position) ||
                    CurrentEnteredRoom == roomData)
                {
                    if (CanLoadNewRoomsInRange)
                    {
                        HandleRoomEnteredLoadingRange(roomData);
                    }
                }
                else
                {
                    if(CanUnloadRoomsOutOfRange)
                    {
                        HandleRoomExitedLoadingRange(roomData);
                    }
                }
            }
        }


        public void SetRoomAsMustLoad(RoomData room)
        {
            _logger.Log(RoomLoadLogCategory.MustLoad, $"Add Must load {room}");
            AddRoomExteriorFlag(room, RoomFlag.IsGeneralMustLoad);
            AddRoomInteriorFlag(room, RoomFlag.IsGeneralMustLoad);
        }

        public void UnsetRoomAsMustLoad(RoomData room)
        {
            _logger.Log(RoomLoadLogCategory.MustLoad, $"Remove Must load {room}");

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

        

        public async UniTask EnterRoom(RoomData room, bool forceLoadIfMissing = false, bool waitForDependencies = false)
        {
            if (_isUnloading)
            {
                Logger.LogWarning(RoomLoadLogCategory.RoomEntry,$"Can't enter room {room} when UNLOADING");
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
                Logger.LogWarning(RoomLoadLogCategory.HandleCreation,"AddHandleForAlreadyLoadedInterior " + room, room);
                _roomLoader.AddHandleForAlreadyLoadedRoom(room, RoomFlag.IsCurrentRoom);
            }

            //#NOTE: scriptable references become different in build between addressable and 
            //non addressable scenes
            //resulting in the == check failing for same room 
            //instead, check for equality with name for now
            if (_currentEnteredRoom != room)
            // if (_currentEnteredRoom == null || room.name !=  _currentEnteredRoom.name )
            {

                var prevFloor = CurrentFloor;
                var prevRoom = _currentEnteredRoom;

                _currentEnteredRoom = room;
                bool isDifferentFloor = prevFloor != room.Floor;

                if (prevRoom != null)
                {
                    Logger.LogWarning(RoomLoadLogCategory.RoomExit,$"Change room {prevRoom} -> {room}");
                    ExitRoom(prevRoom);
                    if (isDifferentFloor && prevFloor != null)
                    {
                        Logger.LogWarning(RoomLoadLogCategory.FloorExit,$"Change floor {prevFloor} -> {room.Floor}");
                        ExitFloor(prevFloor);
                    }
                }
                
                ForceEnterRoom(room);

                if (!isAlreadyLoadedRoom)
                {
                    Logger.Log(RoomLoadLogCategory.Load,$"load new room {room}");
                    await AddRoomExteriorFlagAndWait(room, RoomFlag.IsCurrentRoom);
                    await AddRoomInteriorFlagAndWait(room, RoomFlag.IsCurrentRoom);
                }
                else
                {
                    Logger.Log(RoomLoadLogCategory.Load,$"already loaded room {room}");
                }

                OnRoomEntered?.Invoke(room);
                if (waitForDependencies)
                {
                    await LoadCurrentRoomDependencies(room, isDifferentFloor);
                }
                else
                {
                    LoadCurrentRoomDependencies(room, isDifferentFloor).Forget();
                }
            }
        }

        //#TODO better unload 
        public async UniTask UnloadAllRooms()
        {
            if (_isUnloading)
            {
                return;
            }
            Logger.Log(RoomLoadLogCategory.Unload,"start unloading all rooms");

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
            Logger.Log(RoomLoadLogCategory.Unload,"unloaded all rooms");
        }
        
        public float LoadingPercentageForRoom(RoomData room, bool considerInterior, bool includeMustLoadRooms)
        {
            float progress = 0;
            int numRoomsToLoad = 1;
            progress += _roomLoader.GetExteriorLoadingPercentage(room);
            if (considerInterior)
            {
                numRoomsToLoad++;
                progress += _roomLoader.GetInteriorLoadingPercentage(room);
            }

            if (includeMustLoadRooms)
            {
                //#TODO this doesn't account for room dependencies that are shared between room and floor
                //#TODO store dependent rooms in a hashset
                numRoomsToLoad += room.AlwaysLoadRooms.Count;
                foreach (var mustLoadRoom in room.AlwaysLoadRooms)
                {
                    progress += _roomLoader.GetExteriorLoadingPercentage(mustLoadRoom);
                }
                

                if (room.Floor != null)
                {
                    numRoomsToLoad += room.Floor.AlwaysLoadRooms.Count;
                    foreach (var mustLoadRoom in room.Floor.AlwaysLoadRooms)
                    {
                        progress += _roomLoader.GetExteriorLoadingPercentage(mustLoadRoom);
                    }
                }
            }
            return progress/numRoomsToLoad;
        }

        private void ExitFloor(FloorData prevFloor)
        {
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
                    Logger.Log(RoomLoadLogCategory.FloorEnter, $"LOAD FLOOR DEPENDENCIES {room.Floor}");
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
    
    [Flags]
    public enum RoomLoadLogCategory
    {
        RoomEntry = 1 << 0,
        RoomExit = 1 << 1,
        FloorEnter = 1 << 2,
        FloorExit = 1 << 3,
        Load = 1 << 4,
        Unload = 1 << 5,
        MustLoad = 1 << 7,
        HandleCreation = 1 << 8,
        AddFlag = 1 << 9,
        RemoveFlag = 1 << 10,
    }
}