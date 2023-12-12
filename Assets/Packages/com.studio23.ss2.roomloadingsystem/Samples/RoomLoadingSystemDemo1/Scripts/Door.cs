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
        private Renderer _renderer;
        [FormerlySerializedAs("innerRoom")] public RoomData InnerRoom;
        [FormerlySerializedAs("outerRoom")] public RoomData OuterRoom;

        [FormerlySerializedAs("offsetDist")] public float OffsetDist = 1.5f;
        public Vector3 OuterPoint => transform.position +  transform.right *OffsetDist;
        public Vector3 InnerPoint => transform.position +  -transform.right *OffsetDist;
        private Vector3 _ogPos;
        [FormerlySerializedAs("openingOffset")] public Vector3 OpeningOffset = Vector3.up * 4;
        public Material HoverMat;
        public Material NormalMat;

        private void Awake()
        {
            _ogPos = transform.position;
            _renderer = GetComponent<Renderer>();
        }

        public void HandleInteractHoverStart()
        {
            _renderer.sharedMaterial = HoverMat;
        }
        
        public void HandleInteractHoverEnd()
        {
            _renderer.sharedMaterial = NormalMat;
        }

        public async UniTask DoorOpenAnim()
        {
            FiniteTimer openAnimDuration = new FiniteTimer(.6f);
            while (!openAnimDuration.isComplete)
            {
                openAnimDuration.updateTimer(Time.deltaTime);
                transform.position = _ogPos + OpeningOffset * openAnimDuration.Ratio;

                await UniTask.Yield();
            }
        }
        
        public async UniTask DoorCloseAnim()
        {
            transform.position = _ogPos;
            await UniTask.Yield();
        }

        public Vector3 GetPosAfterDoorOpen()
        {
            var positionAfterEntry = RoomManager.Instance.CurrentEnteredRoom == InnerRoom
                ? OuterPoint
                : InnerPoint;
            return positionAfterEntry;
        }

        public async UniTask Open()
        {
            var roomToLoad = RoomManager.Instance.CurrentEnteredRoom == InnerRoom
                ? OuterRoom
                : InnerRoom;
            await RoomManager.Instance.EnterRoom(roomToLoad);
            await DoorOpenAnim();
        }        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(OuterPoint, Vector3.up * 6); 
            Gizmos.color = Color.red;
            Gizmos.DrawRay(InnerPoint, Vector3.up * 6); 
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.right * 6); 
        }
    }
}