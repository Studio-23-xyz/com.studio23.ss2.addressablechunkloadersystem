using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
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