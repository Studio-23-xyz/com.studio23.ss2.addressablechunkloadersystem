using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomInstance:MonoBehaviour
    {
        [FormerlySerializedAs("defaultPlayerSpawnPoint")] 
        public Transform _defaultPlayerSpawnPoint;
        [FormerlySerializedAs("Room")] 
        public RoomData _room;
        public bool DoesRoomPosMatch => _room.WorldPosition == transform.position;

        private void Awake()
        {
            if (!DoesRoomPosMatch)
            {
                Debug.LogWarning($"<color=#0000FF>Room {_room}<color=#FF0000> worldpos {_room.WorldPosition} <color=#FF0000>doesn't match roominstance worldPos</color> {transform.position}");
            }
        }


        private void OnDrawGizmosSelected()
        {
            if (_room == null)
            {
                return;
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_room.WorldPosition, .125f);
            Gizmos.DrawRay(_room.WorldPosition, Vector3.up* 9);

            if (!DoesRoomPosMatch)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, .125f);
                Gizmos.DrawRay(transform.position, Vector3.up* 9);

                Gizmos.DrawLine(_room.WorldPosition, transform.position); 
            }
        }

        private void OnDrawGizmos()
        {
            if (_room == null)
            {
                return;
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _room.RoomLoadRadius);
        }
    }
}