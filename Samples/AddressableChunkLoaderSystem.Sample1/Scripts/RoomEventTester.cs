using System;
using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
{
    public class RoomEventTester:MonoBehaviour
    {
        private void Start()
        {
            RoomManager.Instance.OnRoomEntered += HandleRoomEntry;
            RoomManager.Instance.OnRoomExited += HandleRoomExited;
            RoomManager.Instance.OnFloorEntered += HandleFloorEntered;
            RoomManager.Instance.OnFloorExited += HandleFloorExited;

            RoomManager.Instance.RoomLoader.OnRoomExteriorLoaded += HandleExteriorRoomLoaded;
            RoomManager.Instance.RoomLoader.OnRoomExteriorUnloaded += HandleRoomExteriorUnloaded;
            RoomManager.Instance.RoomLoader.OnRoomInteriorLoaded += HandleRoomInteriorLoaded;
            RoomManager.Instance.RoomLoader.OnRoomInteriorUnloaded += HandleRoomInteriorUnloaded;
        }

        private void HandleRoomInteriorUnloaded(RoomData room)
        {
            Debug.Log($"{room} interior unload callback");
        }

        private void HandleRoomInteriorLoaded(RoomData room)
        {
            Debug.Log($"{room} interior load callback");
        }

        private void HandleRoomExteriorUnloaded(RoomData room)
        {
            Debug.Log($"{room} exterior unload callback");
        }

        private void HandleExteriorRoomLoaded(RoomData room)
        {
            Debug.Log($"{room} exterior load callback");
        }

        private void HandleFloorExited(FloorData room)
        {
            Debug.Log(room + " OnFloorExited callback");
        }

        private void HandleFloorEntered(FloorData room)
        {
            Debug.Log(room + " OnFloorEntered callback");
        }

        private void HandleRoomExited(RoomData room)
        {
            Debug.Log(room + " OnRoomExited callback");
        }

        private void HandleRoomEntry(RoomData room)
        {
            Debug.Log(room + " OnRoomEntered callback");
        }

        private void OnDestroy()
        {
            RoomManager.Instance.OnRoomEntered -= HandleRoomEntry;
            RoomManager.Instance.OnRoomExited -= HandleRoomExited;
            RoomManager.Instance.OnFloorEntered -= HandleFloorEntered;
            RoomManager.Instance.OnFloorExited -= HandleFloorExited;
            
            RoomManager.Instance.RoomLoader.OnRoomExteriorLoaded -= HandleExteriorRoomLoaded;
            RoomManager.Instance.RoomLoader.OnRoomExteriorUnloaded -= HandleRoomExteriorUnloaded;
            RoomManager.Instance.RoomLoader.OnRoomInteriorLoaded -= HandleRoomInteriorLoaded;
            RoomManager.Instance.RoomLoader.OnRoomInteriorUnloaded -= HandleRoomInteriorUnloaded;
        }
    }
}