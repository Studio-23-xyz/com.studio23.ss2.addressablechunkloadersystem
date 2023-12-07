using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEditor;
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

        private Dictionary<RoomData, AsyncOperationHandle<SceneInstance>> _roomInteriorLoadHandles;
        private Dictionary<RoomData, AsyncOperationHandle<SceneInstance>> _roomExteriorLoadHandles;
        private Transform player;
        protected override void Initialize()
        {
            _mustLoadRoomExteriors = new HashSet<RoomData>();
            _exteriorRoomsToLoad = new HashSet<RoomData>();
            _interiorRoomsToLoad = new HashSet<RoomData>();
            _roomsInLoadingRange = new HashSet<RoomData>();
            _roomInteriorLoadHandles = new Dictionary<RoomData, AsyncOperationHandle<SceneInstance>>();
            _roomExteriorLoadHandles = new Dictionary<RoomData, AsyncOperationHandle<SceneInstance>>();

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

        public AsyncOperationHandle<SceneInstance> GetOrCreateRoomInteriorLoadHandle(
            RoomData room,
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {
            AsyncOperationHandle<SceneInstance> handle;
            if (!_roomInteriorLoadHandles.TryGetValue(room, out handle))
            {
                handle = Addressables.LoadSceneAsync(room.InteriorScene, loadSceneMode, activateOnLoad, priority);
                _roomInteriorLoadHandles.Add(room, handle);
            }

            return handle;
        }
        
        public AsyncOperationHandle<SceneInstance> RemoveRoomInteriorLoadHandle(RoomData room)
        {
            var handle = _roomInteriorLoadHandles[room];
            _roomInteriorLoadHandles.Remove(room);
            return handle;
        }
        
        public AsyncOperationHandle<SceneInstance> GetOrCreateRoomExteriorLoadHandle(
            RoomData room,
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {
            AsyncOperationHandle<SceneInstance> handle;
            if (!_roomExteriorLoadHandles.TryGetValue(room, out handle))
            {
                handle = Addressables.LoadSceneAsync(room.ExteriorScene, loadSceneMode, activateOnLoad, priority);
                _roomExteriorLoadHandles.Add(room, handle);
            }

            return handle;
        }
        
        public AsyncOperationHandle<SceneInstance> RemoveRoomExteriorLoadHandle(RoomData room)
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

                if (CurrentFloor.WantsToAlwaysLoad(room))
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
        }

        private void updateRoomsInPlayerRange()
        {
            if(_currentEnteredRoom ==null)
                return;
            foreach (var roomData in CurrentFloor.RoomsInFloor)
            {
                if (roomData.IsPosInLoadingRange(player.transform.position))
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
            
            if (_mustLoadRoomExteriors.Contains(room))
            {
                Debug.Log($"{room} interior is global must load room");

                return true;
            }
            
            if (_currentEnteredRoom != null)
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
            if (_roomsInLoadingRange.Remove(room))
            {
                RemoveRoomExteriorToLoad(room);
            }
        }


        public async UniTask AddRoomExteriorToLoad(RoomData room)
        {
            if (_exteriorRoomsToLoad.Add(room))
            {
                await room.loadRoomExterior();
            }
        }
        
        public async UniTask RemoveRoomExteriorToLoad(RoomData room)
        {
            
            if (!CheckIfRoomExteriorShouldBeLoaded(room))
            {
                if (_exteriorRoomsToLoad.Remove(room))
                {
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
            if (_currentEnteredRoom != room)
            {
                var prevFloor = CurrentFloor;
                var prevRoom = _currentEnteredRoom;
                _currentEnteredRoom = room;
                bool isDifferentFloor = prevFloor != _currentEnteredRoom.Floor;
                
                if (prevRoom != null)
                {
                    ExitRoom(prevRoom);
                    
                    if (isDifferentFloor)
                    {
                        ExitFloor(prevFloor);
                    }
                }

                ForceEnterRoom(room);
                OnRoomEntered?.Invoke(room);
                await AddRoomInteriorToLoad(room);

                loadRoomDependencies(room, prevFloor);
            }
        }

        private async UniTask ExitFloor(FloorData prevFloor)
        {
            OnFloorExited?.Invoke(prevFloor);
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
                await AddRoomExteriorToLoad(adjacentRoom);
            }
            if (floorNewlyEntered)
            {
                OnFloorEntered?.Invoke(room.Floor);
                foreach (var roomToLoad in room.Floor.AlwaysLoadRooms)
                {
                   await AddRoomExteriorToLoad(roomToLoad);
                }
            }
                
            OnEnteredRoomDependenciesLoaded?.Invoke(room);
        }

        private void ForceEnterRoom(RoomData room)
        {
            _currentEnteredRoom = room;
            room.HandleRoomEntered();
        }
       


    }
}