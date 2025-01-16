using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Controller;
using UnityEngine.UI;

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

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (inputMove == null)
                inputMove = UIManager.Instance.gameObject.GetComponentInChildren<MobileController>();

            if(popupButton == null)
            {
                popupButton = UIManager.Instance.gameObject.GetComponentInChildren<PopupButton>();

                popupButton.gameObject.SetActive(false);
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (animator == null)
                animator = GetComponent<Animator>();

            rb = GetComponent<Rigidbody>();
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