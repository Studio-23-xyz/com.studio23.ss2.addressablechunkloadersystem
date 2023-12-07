using System;
using NaughtyAttributes;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    [RequireComponent(typeof(RoomInstance))]
    /// <summary>
    /// Example room entry detection component
    /// Write your own if needed
    /// </summary>
    public class RoomEntryZone:MonoBehaviour
    {
        private RoomInstance _roomInstance;
        [SerializeField] private bool isPlayerInRoom;
        private void Awake()
        {
            _roomInstance = GetComponent<RoomInstance>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInRoom = true;
                
                RoomLoadingManager.Instance.EnterRoom(_roomInstance.Room);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInRoom = false;

                RoomLoadingManager.Instance.ExitRoom(_roomInstance.Room);
            }
        }
    }
}