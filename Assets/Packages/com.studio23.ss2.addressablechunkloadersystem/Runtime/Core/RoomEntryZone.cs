using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core
{
    [RequireComponent(typeof(RoomInstance))]
    /// <summary>
    /// Example room entry detection component
    /// Write your own if needed
    /// </summary>
    public class RoomEntryZone:MonoBehaviour
    {
        private RoomInstance _roomInstance;
        private void Awake()
        {
            _roomInstance = GetComponent<RoomInstance>();
        }
        
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                RoomManager.Instance.EnterRoom(_roomInstance._room).Forget();
            }
        }

    }
}