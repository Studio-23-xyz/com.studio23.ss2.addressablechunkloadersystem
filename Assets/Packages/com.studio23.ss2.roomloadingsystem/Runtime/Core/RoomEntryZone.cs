using System;
using NaughtyAttributes;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    /// <summary>
    /// Example room entry detection component
    /// Write your own if needed
    /// </summary>
    public class RoomEntryZone:MonoBehaviour
    {
        [SerializeField] RoomData _room;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                RoomLoadingManager.Instance.EnterRoom(_room);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                RoomLoadingManager.Instance.ExitRoom(_room);
            }
        }
    }
}