using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory
{
    public interface IRoomMemory
    {
        public string ID { get; set; }
        public GameObject gameObject { get; }
        public string GetTempSaveData();
        public void TempLoadData(string data);
    }
}