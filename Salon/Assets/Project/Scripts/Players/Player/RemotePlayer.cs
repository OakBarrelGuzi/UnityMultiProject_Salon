using UnityEngine;
using Salon.Firebase.Database;

namespace Salon.Character
{
    public class RemotePlayer : Player
    {
        private Vector3 targetPosition;
        private Vector3 targetDirection;
        private Vector3 currentVelocity;

        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float maxSpeed = 20f;

        private Animator animator;
        private bool isMoving;
        private float currentMoveSpeed;
        private float moveSpeedVelocity;

        public override void Initialize(string displayName)
        {
            base.Initialize(displayName);
            animator = GetComponent<Animator>();
            targetPosition = transform.position;
            targetDirection = transform.forward;
            currentVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (isTesting) return;

            // 위치 보간
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                positionSmoothTime,
                maxSpeed);

            // 방향 즉시 설정 (보간 제거)
            if (targetDirection != Vector3.zero)
            {
                transform.forward = targetDirection;
            }

            // 애니메이션 업데이트 - isMoving 상태에 따라 즉시 변경
            float targetSpeed = isMoving ? 1f : 0f;
            currentMoveSpeed = targetSpeed; // 보간 제거

            if (animator != null)
            {
                animator.SetFloat("MoveSpeed", currentMoveSpeed);
            }
        }

        public void GetNetworkPosition(NetworkPositionData posData)
        {
            if (posData == null) return;

            var position = posData.GetPosition();
            if (position.HasValue)
            {
                targetPosition = position.Value;
                isMoving = posData.IsPositionUpdate;
            }

            targetDirection = posData.GetDirection();

            Debug.Log($"[RemotePlayer] {displayName} 위치 업데이트 - Pos: {targetPosition}, Dir: {targetDirection}, Moving: {isMoving}");
        }
    }
}