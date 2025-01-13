using UnityEngine;
using Salon.Firebase.Database;

namespace Salon.Character
{
    public class RemotePlayer : Player
    {
        private Vector3 targetPosition;
        private Vector3 targetDirection;
        private Vector3 currentVelocity;
        private Vector3 currentAngularVelocity;

        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
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
            currentAngularVelocity = Vector3.zero;
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

            // 방향 보간
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 360f * rotationSmoothTime);
            }

            // 애니메이션 업데이트
            float targetSpeed = isMoving ? 1f : 0f;
            currentMoveSpeed = Mathf.SmoothDamp(
                currentMoveSpeed,
                targetSpeed,
                ref moveSpeedVelocity,
                0.1f);

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