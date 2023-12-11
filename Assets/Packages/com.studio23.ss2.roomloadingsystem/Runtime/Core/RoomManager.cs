using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    [RequireComponent(typeof(RoomLoader))]
    public class RoomManager:MonoBehaviourSingletonPersistent<RoomManager>
    {
        [SerializeField] List<FloorData> _allFloors;

        
        private HashSet<RoomData> _mustLoadRoomExteriors;
        private HashSet<RoomData> _mustLoadRoomInteriors;

        public RoomLoader RoomLoader { get; private set; }
        


        //#TODO separate this
        protected override void Initialize()
        {
            _mustLoadRoomExteriors = new HashSet<RoomData>();
            _mustLoadRoomInteriors = new HashSet<RoomData>();

            RoomLoader = GetComponent<RoomLoader>();

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