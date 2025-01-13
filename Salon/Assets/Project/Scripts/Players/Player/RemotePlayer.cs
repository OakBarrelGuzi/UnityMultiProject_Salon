using UnityEngine;
using Salon.Firebase.Database;

namespace Salon.Character
{
    //ToDo : 회전에 약간 보간 추가
    public class RemotePlayer : Player
    {
        private Vector3 targetPosition;
        private Vector3 targetDirection;
        private Vector3 currentVelocity;
        private Vector3 previousPosition;

        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float movementThreshold = 0.01f;

        private Animator animator;
        private bool isMoving;

        public override void Initialize(string displayName)
        {
            base.Initialize(displayName);
            animator = GetComponent<Animator>();
            targetPosition = transform.position;
            previousPosition = transform.position;
            targetDirection = transform.forward;
            currentVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (isTesting) return;

            previousPosition = transform.position;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                positionSmoothTime,
                maxSpeed);

            isMoving = Vector3.Distance(transform.position, previousPosition) > movementThreshold;

            if (targetDirection.magnitude > 0.01f)
            {
                transform.forward = targetDirection;
            }

            if (animator != null)
            {
                animator.SetFloat("MoveSpeed", isMoving ? 1f : 0f);
            }
        }

        public void GetNetworkPosition(NetworkPositionData posData)
        {
            if (posData == null) return;

            var position = posData.GetPosition();
            if (position.HasValue)
            {
                targetPosition = position.Value;
            }

            Vector3 newDirection = posData.GetDirection();
            if (newDirection.magnitude > 0.01f)
            {
                targetDirection = newDirection;
            }

            Debug.Log($"[RemotePlayer] {displayName} 업데이트 - Pos: {targetPosition}, Dir: {targetDirection}, Moving: {isMoving}");
        }
    }
}