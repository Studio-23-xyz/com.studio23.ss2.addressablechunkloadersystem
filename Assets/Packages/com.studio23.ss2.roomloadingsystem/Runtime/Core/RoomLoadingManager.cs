using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Studio23.SS2.RoomLoadingSystem.Runtime.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomLoadingManager:MonoBehaviourSingletonPersistent<RoomLoadingManager>
    {
        [SerializeField] List<FloorData> _allFloors;
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

        public RoomData CurrentEnteredRoom => _currentEnteredRoom;
        [Required][SerializeField] private RoomData _currentEnteredRoom;
        [ShowNativeProperty] public FloorData CurrentFloor => _currentEnteredRoom ? _currentEnteredRoom.Floor : null;
        
        private HashSet<RoomData> _mustLoadRoomExteriors;
        private HashSet<RoomData> _mustLoadRoomInteriors;
        private HashSet<RoomData> _roomsInLoadingRange;
        private HashSet<RoomData> _exteriorRoomsToLoad;
        private HashSet<RoomData> _interiorRoomsToLoad;

        private Dictionary<RoomData, RoomLoadHandle> _roomInteriorLoadHandles;
        private Dictionary<RoomData, RoomLoadHandle> _roomExteriorLoadHandles;

        /// <summary>
        /// IS NOT A LIST THAT CONTIANS ROOMS THAT ARE BEING UNLOADED. FOR INTERNAL USE ONLY
        /// </summary>
        private List<RoomData> _roomsToUnloadListCache;
        
        //#TODO separate this
        private Transform player;
        protected override void Initialize()
        {
            _mustLoadRoomExteriors = new HashSet<RoomData>();
            _mustLoadRoomInteriors = new HashSet<RoomData>();
            _exteriorRoomsToLoad = new HashSet<RoomData>();
            _interiorRoomsToLoad = new HashSet<RoomData>();
            _roomsInLoadingRange = new HashSet<RoomData>();
            _roomInteriorLoadHandles = new Dictionary<RoomData, RoomLoadHandle>();
            _roomExteriorLoadHandles = new Dictionary<RoomData, RoomLoadHandle>();
            _roomsToUnloadListCache = new List<RoomData> ();

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

        public RoomLoadHandle GetOrCreateRoomInteriorLoadHandle(
            RoomData room,
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {
            RoomLoadHandle handle;
            if (!_roomInteriorLoadHandles.TryGetValue(room, out handle))
            {
                handle = RoomLoadHandle.AddressableLoad(room, room.InteriorScene, loadSceneMode, activateOnLoad, priority);
                _roomInteriorLoadHandles.Add(room, handle);
            }

            return handle;
        }
        
        public RoomLoadHandle RemoveRoomInteriorLoadHandle(RoomData room)
        {
            var handle = _roomInteriorLoadHandles[room];
            _roomInteriorLoadHandles.Remove(room);
            return handle;
        }
        
        public RoomLoadHandle GetOrCreateRoomExteriorLoadHandle(
            RoomData room,
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {
            RoomLoadHandle handle;
            if (!_roomExteriorLoadHandles.TryGetValue(room, out handle))
            {
                handle = RoomLoadHandle.AddressableLoad(room, room.ExteriorScene, loadSceneMode, activateOnLoad, priority);
                _roomExteriorLoadHandles.Add(room, handle);
            }

            return handle;
        }
        
        public RoomLoadHandle RemoveRoomExteriorLoadHandle(RoomData room)
        {
            var handle = _roomExteriorLoadHandles[room];
            _roomExteriorLoadHandles.Remove(room);
            return handle;
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

        private void Start()
        {
            player = GameObject.FindWithTag("Player").transform;
        }

        

        public virtual bool CheckIfRoomExteriorShouldBeLoaded(RoomData room)
        {
            if (_currentEnteredRoom == room)
            {
                Debug.Log($"{room} is current room");
                return true;
            }
            
            if (_mustLoadRoomExteriors.Contains(room))
            {
                Debug.Log($"{room} is global must load");
                return true;
            }
            
            if (_currentEnteredRoom != null)
            {
                if (_currentEnteredRoom.IsAdjacentTo(room))
                {
                    Debug.Log($"{room} is adjacent to current room {_currentEnteredRoom}");

                    return true;
                }

                if (CurrentFloor != null && CurrentFloor.WantsToAlwaysLoad(room))
                {
                    Debug.Log($"{room} is must load for current floor {CurrentFloor}");

                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            updateRoomsInPlayerRange();
            UpdateRoomUnloadTimer();
        }

        private void UpdateRoomUnloadTimer()
        {
            //the timeout could be handled by starting an async wait task for the duration
            //however, it complicates the following case
            //Frequently entering and exiting range.
            //we need to cancel and restart the unitask/async wait repeatedly.
            //also prevent duplicate waits
            //this is simpler for now
            _roomsToUnloadListCache.Clear();
            foreach ((var room, var handle) in _roomExteriorLoadHandles)
            {
                if (_roomsInLoadingRange.Contains(room))
                {
                    //Debug.Log($"{room} unload timer reset becayse player in range");
                    handle.UnloadTimer.reset();
                }
                else
                {
                    if (handle.UnloadTimer.tryCompleteTimer(Time.deltaTime))
                    {
                        Debug.Log($"{room} unload timer completed. Try Unloading");

                        _roomsToUnloadListCache.Add(room);
                    }
                }
            }
            //this updates the dict. So removal is done separately
            foreach (var roomToUnload in _roomsToUnloadListCache)
            {
                RemoveRoomExteriorToLoad(roomToUnload);       
            }
        }

        private void updateRoomsInPlayerRange()
        {
            if(_currentEnteredRoom == null)
                return;
            if (CurrentFloor == null)
                return;
            foreach (var roomData in CurrentFloor.RoomsInFloor)
            {
                if (roomData.IsPosInLoadingRange(player.transform.position)|| 
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

        public virtual bool CheckIfRoomInteriorShouldBeLoaded(RoomData room)
        {
            if (_currentEnteredRoom == room)
            {
                Debug.Log($"{room} interior is current entered room");
                return true;
            }
            
            if (_mustLoadRoomInteriors.Contains(room))
            {
                Debug.Log($"{room} interior is global must load room");

                return true;
            }
            
            if (_currentEnteredRoom != null && CurrentFloor != null)
            {
                if (CurrentFloor.WantsToAlwaysLoad(room))
                {
                    Debug.Log($"{room} interior is current floor's must load room");
                    return true;
                }
            }

            return false;
        }


        public void SetRoomAsMustLoad(RoomData room)
        {
            _mustLoadRoomExteriors.Add(room);
        }
        
        public void UnsetRoomAsMustLoad(RoomData room)
        {
            _mustLoadRoomExteriors.Remove(room);
        }

        public void HandleRoomEnteredLoadingRange(RoomData room)
        {
            if (_roomsInLoadingRange.Add(room))
            {
                AddRoomExteriorToLoad(room);
            }
        }
        
        public void HandleRoomExitedLoadingRange(RoomData room)
        {
            //actually unloading the room requries waiting for timer
            //handled in different function
            _roomsInLoadingRange.Remove(room);
        }


        public async UniTask AddRoomExteriorToLoad(RoomData room)
        {
            if (_exteriorRoomsToLoad.Add(room))
            {
                Debug.Log($"add {room} exterior to load ", room);
                await room.loadRoomExterior();
            }
        }
        
        public async UniTask RemoveRoomExteriorToLoad(RoomData room)
        {
            
            if (!CheckIfRoomExteriorShouldBeLoaded(room))
            {
                if (_exteriorRoomsToLoad.Remove(room))
                {
                    Debug.Log($"Acutally unload {room}", room);
                    await room.unloadRoomExterior();
                }
            }
        }
        
        public async UniTask AddRoomInteriorToLoad(RoomData room)
        {
            if (_interiorRoomsToLoad.Add(room))
            {
                await room.loadRoomInterior();
            }
        }
        
        public async UniTask RemoveRoomInteriorToLoad(RoomData room)
        {
            Debug.Log($"remove interior {room}", room);
            
            if (!CheckIfRoomInteriorShouldBeLoaded(room))
            {
                if (_interiorRoomsToLoad.Remove(room))
                {
                    Debug.Log($"should no longer load interior {room}", room);

                    await room.unloadRoomInterior();
                }
            }
            else
            {
                Debug.Log($"keep loaded interior {room}", room);
            }
        }


        public async UniTask EnterRoom(RoomData room)
        {
            if (!_roomExteriorLoadHandles.ContainsKey(room))
            {
                //the room has been entered but the exterior isn't marked as loaded
                //this is possible if we start in this scene from the editor
                //in which case, exterior is already loaded.
                //we just need to add a dummy handle
                //that won't unload the scene as an addressable.
                _roomExteriorLoadHandles.Add(room, RoomLoadHandle.ForAlreadyLoadedScene(room, room.ExteriorScene));
                _exteriorRoomsToLoad.Add(room);
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

        [Button]
        void PrintAllRoomHandle()
        {
            foreach((var room, var handle ) in _roomExteriorLoadHandles)
            {
                Debug.Log($" {(handle)}");
            }
        }

        [Button]
        void PrintExteriorsToLoad()
        {
            foreach (var room in _exteriorRoomsToLoad)
            {
                Debug.Log($"{room} exterior will be loaded ");
            }
        }

        [Button]
        void printRoomsInLoadingRange()
        {
            foreach (var room in _roomsInLoadingRange)
            {
                Debug.Log($"{room} in loading range");
            }
        }

        private void ForceEnterRoom(RoomData room)
        {
            _currentEnteredRoom = room;
            room.HandleRoomEntered();
        }
       


    }
}