using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
	public class TestCharacterInputs : MonoBehaviour
	{
		[FormerlySerializedAs("_move")] [Header("Character Input Values")]
		public Vector2 Move;
		[FormerlySerializedAs("_look")] public Vector2 Look;
		[FormerlySerializedAs("_jump")] public bool Jump;
		[FormerlySerializedAs("_sprint")] public bool Sprint;

		 [FormerlySerializedAs("_analogMovement")] [Header("Movement Settings")]
		public bool AnalogMovement;

		[FormerlySerializedAs("_cursorLocked")] [Header("Mouse Cursor Settings")]
		public bool CursorLocked = true; 
		[FormerlySerializedAs("_cursorInputForLook")] public bool CursorInputForLook = true;
		public event Action OnInteractPressed;
		public void OnInteract()
		{
			OnInteractPressed?.Invoke();
		}
		
		public void OnInteract(InputValue value)
		{
			OnInteractPressed?.Invoke();
		}
		
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(CursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}


		public void MoveInput(Vector2 newMoveDirection)
		{
			Move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			Look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			Jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			Sprint = newSprintState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(CursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}
