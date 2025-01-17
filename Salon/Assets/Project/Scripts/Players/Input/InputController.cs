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
        public float moveSpeed;

        public Vector3 CurrentVelocity => cachedInput * moveSpeed;

        private Rigidbody rb;
        private bool isInitialized = false;

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

            rb = GetComponent<Rigidbody>();

            isInitialized = true;
            Debug.Log("[InputController] 초기화 완료");
        }

        private void Update()
        {
            HandleInput();
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

                if (cachedInput != Vector3.zero)
                {
                    transform.forward = cachedInput.normalized;
                }
            }
            else
            {
                if (lerpStopping)
                {
                    cachedInput = Vector3.Lerp(cachedInput, Vector3.zero, moveSpeed * Time.deltaTime);
                }
                else
                {
                    cachedInput = Vector3.zero;
                }
            }
        }

        private void Move()
        {
            if (rb != null)
            {
                Vector3 movement = cachedInput * moveSpeed * Time.fixedDeltaTime;

            }
        }

        private void FixedUpdate()
        {
            RaycastHit hit;
            Vector3 movement = cachedInput * moveSpeed * Time.fixedDeltaTime;
            if (!Physics.Raycast(transform.position + Vector3.up * 5f, movement.normalized, out hit, movement.magnitude
                , Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                transform.position += movement;
            }
            else
            {
                print("충돌 감지: " + hit.collider.name);
            }

        }

        private void UpdateAnimation()
        {
            if (animator != null)
            {
                float currentSpeed = cachedInput.magnitude;
                animator.SetFloat("MoveSpeed", currentSpeed);
            }
        }
    }
}