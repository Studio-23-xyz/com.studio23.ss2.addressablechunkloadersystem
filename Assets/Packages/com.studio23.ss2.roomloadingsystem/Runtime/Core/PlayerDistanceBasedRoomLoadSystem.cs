using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class PlayerDistanceBasedRoomLoadSystem:MonoBehaviour, IRoomLoadSubSystem
    {
        private HashSet<RoomData> _roomsInLoadingRange;
        public RoomData CurrentEnteredRoom => _currentEnteredRoom;
        [Required] [SerializeField] private RoomData _currentEnteredRoom;
        [ShowNativeProperty] public FloorData CurrentFloor => _currentEnteredRoom ? _currentEnteredRoom.Floor : null;
        private Transform player;

        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            player = GameObject.FindWithTag("Player").transform;
        }


        public void Initialize()
        {
            _roomsInLoadingRange = new HashSet<RoomData>();
        }
        public void HandleRoomEnteredLoadingRange(RoomData room)
        {
            if (_roomsInLoadingRange.Add(room))
            {
                RoomManager.Instance.RoomLoader.AddExteriorLoadRequest(new RoomLoadRequestData(room), this);   
            }
        }
        
        public void HandleRoomExitedLoadingRange(RoomData room)
        {
            if (_roomsInLoadingRange.Contains(room))
            {
                RoomManager.Instance.RoomLoader.RemoveExteriorLoadRequest(room, this);                
            }
        }
        private void Update()
        {
            updateRoomsInPlayerRange();
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
    }
}