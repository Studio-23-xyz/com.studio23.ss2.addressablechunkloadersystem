using System;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomInstance:MonoBehaviour
    {
        public Transform defaultPlayerSpawnPoint;
        public RoomData Room;
        public bool DoesRoomPosMatch() => Room.WorldPosition == transform.position;

        private void Awake()
        {
            if (!DoesRoomPosMatch())
            {
                UnityEngine.Debug.Log($"Room {Room} worldpos {Room.WorldPosition} doesn't match roominstance worldPos {transform.position}");
            }
        }


        private void OnDrawGizmosSelected()
        {
            if (Room == null)
            {
                return;
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(Room.WorldPosition, .125f);
            Gizmos.DrawRay(Room.WorldPosition, Vector3.up* 9);

            if (!DoesRoomPosMatch())
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, .125f);
                Gizmos.DrawRay(transform.position, Vector3.up* 9);

                Gizmos.DrawLine(Room.WorldPosition, transform.position); 
            }
        }

        private void OnDrawGizmos()
        {
            if (Room == null)
            {
                return;
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Room.RoomLoadRadius);
        }
    }
}