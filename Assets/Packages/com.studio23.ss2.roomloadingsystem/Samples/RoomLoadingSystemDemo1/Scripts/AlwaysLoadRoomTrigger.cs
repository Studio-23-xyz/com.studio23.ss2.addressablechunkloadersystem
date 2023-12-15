using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    public class AlwaysLoadRoomTrigger:MonoBehaviour
    {
        public RoomData Room;
        public bool ShouldLoad = true;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (ShouldLoad)
                {
                    RoomManager.Instance.SetRoomAsMustLoad(Room);
                }
                else
                {
                    RoomManager.Instance.UnsetRoomAsMustLoad(Room);
                }
            }
        }
    }
}