using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    [RequireComponent(typeof(RoomLoader))]
    public class RoomManager:MonoBehaviourSingletonPersistent<RoomManager>
    {
        [SerializeField] List<FloorData> _allFloors;
        private RoomLoader _roomLoader;
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

        /// <summary>
        /// IS NOT A LIST THAT CONTIANS ROOMS THAT ARE BEING UNLOADED. FOR INTERNAL USE ONLY
        /// </summary>
        private List<RoomData> _roomsToUnloadListCache;

        //#TODO separate this
        private Transform player;

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

        private void Start()
        {
            FindPlayer();
        }

        protected virtual void FindPlayer()
        {
            player = GameObject.FindWithTag("Player").transform;
        }



        private void Update()
        {
            updateRoomsInPlayerRange();
            //called explicitly to ensure that timer starts on same frame
            RoomLoader.UpdateRoomUnloadTimer();
        }


        private void updateRoomsInPlayerRange()
        {
            if (_currentEnteredRoom == null)
                return;
            if (CurrentFloor == null)
                return;
            foreach (var roomData in CurrentFloor.RoomsInFloor)
            {
                if (roomData.IsPosInLoadingRange(player.transform.position) ||
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

        internal async UniTask AddRoomExteriorFlag(RoomData room, RoomFlag flags)
        {
            var handle = _roomLoader.AddExteriorLoadRequest(new RoomLoadRequestData(room), flags);
            await handle.LoadScene();
        }
        
        internal async UniTask AddRoomInteriorFlag(RoomData room, RoomFlag flags)
        {
            var handle = _roomLoader.AddInteriorLoadRequest(new RoomLoadRequestData(room), flags);
            await handle.LoadScene();
        }

        public async UniTask EnterRoom(RoomData room)
        {
            if (_currentEnteredRoom == null && !_roomLoader.RoomExteriorLoadHandles.ContainsKey(room))
            {
                //the room has been entered but the exterior isn't marked as loaded
                //this is possible if we start in this scene from the editor
                //in which case, exterior is already loaded.
                //we just need to add a dummy handle
                //that won't unload the scene as an addressable.
                _roomLoader.addHandleForAlreadyLoadedExterior(room, RoomFlag.IsCurrentRoom);
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
                await AddRoomExteriorFlag(room,RoomFlag.IsCurrentRoom);
                await AddRoomInteriorFlag(room,RoomFlag.IsCurrentRoom);
                OnRoomEntered?.Invoke(room);

                loadCurrentRoomDependencies(room, isDifferentFloor);
            }
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
                RemoveExteriorRoomFlag(roomToUnload, RoomFlag.IsCurrentFloorMustLoad);
            }
        }

        private async UniTask loadCurrentRoomDependencies(RoomData room, bool floorNewlyEntered)
        {
            //NOTE:FLAGS are set after the previous room is loaded
            //it is not syncronous
            //a better way would be to set all the flags and then wait on the handles
            foreach (var adjacentRoom in room.AlwaysLoadRooms)
            {
                Debug.Log(room + $" always load {adjacentRoom}");
                await AddRoomExteriorFlag(adjacentRoom, RoomFlag.IsCurrentFloorMustLoad);
            }
    
            if (floorNewlyEntered)
            {
                Debug.Log("load room dep floorNewlyEntered " + floorNewlyEntered);
                if (room.Floor != null)
                {
                    OnFloorEntered?.Invoke(room.Floor);
                    foreach (var roomToLoad in room.Floor.AlwaysLoadRooms)
                    {
                        await AddRoomExteriorFlag(roomToLoad, RoomFlag.IsCurrentFloorMustLoad);
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
        void printRoomsInLoadingRange()
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
        void printRoomsInMustLoad()
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