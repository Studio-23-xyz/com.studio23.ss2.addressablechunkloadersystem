using System;
using Bdeshi.Helpers.Utility;
using Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
{
    public class RoomMemoryTest:MonoBehaviour,IRoomMemory
    {
        public int EntryCount = 0;
        
        public string ID
        {
            get => _id;
            set => _id = value;
        }

        [SerializeField] string _id;
        public string GetTempSaveData()
        {
            return EntryCount.ToString();
        }

        public void TempLoadData(string data)
        {
            EntryCount = int.Parse(data);
            Debug.Log($"After Loading EntryCount = {EntryCount}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                EntryCount++;
                UnityEngine.Debug.Log($"ENtry count now {EntryCount}");
            }
        }
    }
}