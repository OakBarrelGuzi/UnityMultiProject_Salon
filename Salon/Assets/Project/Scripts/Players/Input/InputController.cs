using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Controller;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace Salon.Character
{
    public class InputController : MonoBehaviour
    {
        [SerializeField] private MobileButton inputMove;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Animator animator;

        public PopupButton popupButton;
        private Vector3 cachedInput;
        public bool lerpStopping;
        public float moveSpeed = 5f;
        public float rotationSpeed = 10f;
        public float smoothTime = 0.1f;

        public Vector3 CurrentVelocity => smoothVelocity;

        private CharacterController characterController;
        private bool isInitialized = false;
        private Vector3 currentVelocity;
        private Vector3 smoothVelocity;
        private Vector3 smoothDampVelocity;

        void Start()
        {
            Initialize();
        }

        public async void Initialize()
        {
            if (isInitialized) return;

            // UIManager가 초기화될 때까지 대기
            int retryCount = 0;
            while (UIManager.Instance == null && retryCount < 10)
            {
                await Task.Delay(100);
                retryCount++;
            }

            if (UIManager.Instance == null)
            {
                Debug.LogError("[InputController] UIManager.Instance가 null입니다.");
                return;
            }

            if (inputMove == null)
            {
                inputMove = UIManager.Instance.gameObject.GetComponentInChildren<MobileController>(true);
                if (inputMove == null)
                {
                    Debug.LogError("[InputController] MobileController를 찾을 수 없습니다.");
                    return;
                }
            }

            if (popupButton == null)
            {
                popupButton = UIManager.Instance.gameObject.GetComponentInChildren<PopupButton>(true);
                if (popupButton != null)
                {
                    popupButton.gameObject?.SetActive(false);
                }
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (animator == null)
                animator = GetComponent<Animator>();

            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                characterController.slopeLimit = 45f;
                characterController.stepOffset = 0.3f;
                characterController.skinWidth = 0.08f;
                characterController.minMoveDistance = 0.001f;
            }

            // Rigidbody 제거 (만약 있다면)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb);
            }

            isInitialized = true;
            Debug.Log("[InputController] 초기화 완료");
        }

        private void Update()
        {
            if (!isInitialized) return;

            HandleInput();
            HandleMovement();
            UpdateAnimation();
        }

        private void HandleInput()
        {
            if (inputMove.isFingerDown)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                cachedInput = cameraRight * inputMove.directionXZ.x + cameraForward * inputMove.directionXZ.z;
                cachedInput = Vector3.ClampMagnitude(cachedInput, 1f);

                if (cachedInput != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(cachedInput);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
            else
            {
                cachedInput = Vector3.zero;
            }
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            Vector3 targetVelocity = cachedInput * moveSpeed;
            smoothVelocity = Vector3.SmoothDamp(smoothVelocity, targetVelocity, ref smoothDampVelocity, smoothTime);

            if (characterController.isGrounded)
            {
                // 지면에 있을 때만 이동
                characterController.Move(smoothVelocity * Time.deltaTime);
            }
            else
            {
                // 공중에 있을 때는 중력 적용
                smoothVelocity.y += Physics.gravity.y * Time.deltaTime;
                characterController.Move(smoothVelocity * Time.deltaTime);
            }
        }

        private void UpdateAnimation()
        {
            if (animator != null)
            {
                float currentSpeed = smoothVelocity.magnitude / moveSpeed;
                animator.SetFloat("MoveSpeed", currentSpeed);
            }
        }
    }
}