using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Controller;

namespace Salon.Character
{
    public class InputController : MonoBehaviour
    {
        [SerializeField] private MobileButton inputMove;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Animator animator;

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

        private void FixedUpdate()
        {
            if (rb != null)
            {
                RaycastHit hit;
                Vector3 movement = cachedInput * moveSpeed * Time.fixedDeltaTime;
                if (!Physics.Raycast(transform.position, movement.normalized, out hit, movement.magnitude))
                {
                    rb.MovePosition(rb.position + movement);
                }
                else
                {
                    Debug.Log("충돌 감지: " + hit.collider.name);
                }
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