using UnityEngine;
using Salon.Firebase.Database;
using Character;
using System.Collections.Generic;

namespace Salon.Character
{
    //ToDo : 회전에 약간 보간 추가
    public class RemotePlayer : Player
    {
        private Vector3 targetPosition;
        private Vector3 targetDirection;
        private Vector3 currentVelocity;
        private Vector3 previousPosition;
        private CharacterCustomizationManager customizationManager;

        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float movementThreshold = 0.01f;

        private Animator animator;
        private bool isMoving;
        public AnimController animController { get; private set; }

        public override void Initialize(string displayName)
        {
            base.Initialize(displayName);
            animator = GetComponent<Animator>();
            animController = GetComponent<AnimController>();

            customizationManager = GetComponent<CharacterCustomizationManager>();
            if (customizationManager == null)
            {
                customizationManager = gameObject.AddComponent<CharacterCustomizationManager>();
            }

            if (animController == null)
            {
                Debug.LogError($"[RemotePlayer] AnimController를 찾을 수 없습니다: {displayName}");
            }
            targetPosition = transform.position;
            previousPosition = transform.position;
            targetDirection = transform.forward;
            currentVelocity = Vector3.zero;
        }

        public void UpdateCustomization(Dictionary<string, string> customizationData)
        {
            if (customizationManager != null && customizationData != null)
            {
                customizationManager.ApplyCustomizationData(customizationData);
                Debug.Log($"[RemotePlayer] {displayName}의 커스터마이제이션 업데이트 완료");
            }
            else
            {
                customizationManager.SetDefault();
            }
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

        public void GetNetworkPosition(string compressedData)
        {
            if (string.IsNullOrEmpty(compressedData)) return;

            (Vector3 position, Vector3 direction, bool moving) = NetworkPositionCompressor.DecompressToVectors(compressedData);

            targetPosition = position;
            if (direction.magnitude > 0.01f)
            {
                targetDirection = direction;
            }
            isMoving = moving;

            Debug.Log($"[RemotePlayer] {displayName} 업데이트 - Pos: {targetPosition}, Dir: {targetDirection}, Moving: {isMoving}");
        }

        public void PlayAnimation(string animName)
        {
            if (string.IsNullOrEmpty(animName)) return;

            if (animController != null)
            {
                animController.SetAnime(animName);
            }
            else
            {
                Debug.LogError($"[RemotePlayer] {displayName}의 AnimController가 null입니다.");
            }
        }

        public void PlayEmoji(string emojiName)
        {
            if (string.IsNullOrEmpty(emojiName)) return;

            if (animController != null)
            {
                animController.SetEmoji(emojiName);
            }
            else
            {
                Debug.LogError($"[RemotePlayer] {displayName}의 AnimController가 null입니다.");
            }
        }
    }
}