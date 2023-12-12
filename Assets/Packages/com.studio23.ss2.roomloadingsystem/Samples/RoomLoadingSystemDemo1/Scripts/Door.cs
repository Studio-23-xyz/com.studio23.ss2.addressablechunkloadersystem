using System;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    public class Door:MonoBehaviour
    {
        public RoomData innerRoom;
        public RoomData outerRoom;

        public float offsetDist = 1.5f;
        public Vector3 outerPoint => transform.position +  transform.right *offsetDist;
        public Vector3 innerPoint => transform.position +  -transform.right *offsetDist;
        private Vector3 ogPos;
        public Vector3 openingOffset = Vector3.up * 4;

        private void Awake()
        {
            ogPos = transform.position;
        }

        public async UniTask doorOpenAnim()
        {
            FiniteTimer openAnimDuration = new FiniteTimer(.6f);
            while (!openAnimDuration.isComplete)
            {
                openAnimDuration.updateTimer(Time.deltaTime);
                transform.position = ogPos + openingOffset * openAnimDuration.Ratio;

                await UniTask.Yield();
            }
        }
        
        public async UniTask doorCloseAnim()
        {
            transform.position = ogPos;
            await UniTask.Yield();
        }

        public Vector3 getPosAfterDoorOpen()
        {
            var positionAfterEntry = RoomManager.Instance.CurrentEnteredRoom == innerRoom
                ? outerPoint
                : innerPoint;
            return positionAfterEntry;
        }

        public async UniTask Open()
        {
            var roomToLoad = RoomManager.Instance.CurrentEnteredRoom == innerRoom
                ? outerRoom
                : innerRoom;
            await RoomManager.Instance.EnterRoom(roomToLoad);
            await doorOpenAnim();
        }        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(outerPoint, Vector3.up * 6); 
            Gizmos.color = Color.red;
            Gizmos.DrawRay(innerPoint, Vector3.up * 6); 
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.right * 6); 
        }
    }
}