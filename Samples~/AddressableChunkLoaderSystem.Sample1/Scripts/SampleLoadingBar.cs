using System;
using Cysharp.Threading.Tasks;
using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
{
    public class SampleLoadingBar:MonoBehaviour
    {
        public Slider LoadSlider;

        public async UniTask UpdateLoadingBar(RoomData roomData)
        {
            LoadSlider.gameObject.SetActive(true);
            LoadSlider.value = 0;
            float percentage = 0;
            while (percentage < 1)
            {
                await UniTask.Yield();
                await UniTask.NextFrame();
                percentage = RoomManager.Instance.LoadingPercentageForRoom(roomData, true, true);
                Debug.Log($"loading bar percentage {percentage}");
                
                LoadSlider.value = percentage;
            }
            Debug.Log("loading bar done. hiding in 1 sec");
            //keep loading bar active for a bit to not miss it
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            Debug.Log("loading bar hidden ");
            
            LoadSlider.gameObject.SetActive(false);
        }

    }
}