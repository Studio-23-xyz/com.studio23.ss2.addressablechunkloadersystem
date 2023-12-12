using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    public class AlwaysLoadRoomTrigger:MonoBehaviour
    {
        public RoomData room;
        public bool shouldLoad = true;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (shouldLoad)
                {
                    RoomManager.Instance.SetRoomAsMustLoad(room);
                }
                else
                {
                    RoomManager.Instance.UnsetRoomAsMustLoad(room);
                }
            }
        }
    }
}