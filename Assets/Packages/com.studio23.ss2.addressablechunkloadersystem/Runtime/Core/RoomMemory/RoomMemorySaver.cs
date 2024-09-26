using System;
using System.Collections.Generic;
using System.IO;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory
{
    public class RoomMemorySaver:MonoBehaviour
    {

        /// <summary>
        /// Generates unique save path for  roomMemory for a given room
        /// This has the benefit of the ID only needing to be unique in the same room aka scene
        /// </summary>
        /// <param name="roomData"></param>
        /// <param name="roomMemory"></param>
        /// <returns></returns>
        public static string GetRoomMemorySavePath(RoomData roomData, IRoomMemory roomMemory)
        {
            var dirPath = GetSaveDir();
            System.IO.Directory.CreateDirectory(dirPath);
            return Path.Combine(
                dirPath,
                $"{roomData.name}_{roomMemory.ID}.ff"
                );
        }

        private static string GetSaveDir()
        {
            return Path.Combine(
                Application.persistentDataPath,
                "addressableChunkloader"
            );
        }

        public void SaveAllMemoriesInRoom(RoomData room, List<IRoomMemory> roomMemories)
        {
            foreach (var roomMemory in roomMemories)
            {
                SaveRoomMemoryAsync(room,roomMemory);
            }
        }

        private void SaveRoomMemoryAsync(RoomData roomToSaveUnder, IRoomMemory roomMemory)
        {
            var data = roomMemory.GetTempSaveData();
            var path = GetRoomMemorySavePath(roomToSaveUnder,roomMemory);
                
            if (File.Exists(path)) File.Delete(path);
            //The string is generated and has no dependency on the IRoomMemory afterwards
            //so we can call writeAllTextAsync
            File.WriteAllTextAsync(path, data);
        }

        public void LoadAllMemoriesInRoom(RoomData room, List<IRoomMemory> roomMemories)
        {
            foreach (var roomMemory in roomMemories)
            {
                LoadMemory(room, roomMemory);
            }
        }

        [ContextMenu("ClearAllMemory")]
        public void ClearAllMemory()
        {
            FileUtil.DeleteFileOrDirectory(GetSaveDir());
        }


        private void LoadMemory(RoomData room,IRoomMemory roomMemory)
        {
            var path = GetRoomMemorySavePath(room, roomMemory);

            if (File.Exists(path))
            {
                var data = File.ReadAllText(path);
                //IRoomMemory may get destroyed by the time we call this
                if (roomMemory != null)
                {
                    roomMemory.TempLoadData(data);
                }
            }
        }
    }
}