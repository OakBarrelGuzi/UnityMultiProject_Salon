using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Controller;

namespace Salon.Character
{
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private MobileButton inputMove;
        private Vector3 cachedInput;
        public bool lerpStopping;
        public float moveSpeed;

        public void Initialize()
        {
            inputMove = UIManager.Instance.GetUI<MobileController>();
        }
        private void Update()
        {
            if (inputMove.isFingerDown)
            {
                cachedInput = inputMove.directionXZ;
                transform.forward = cachedInput;
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
            transform.Translate(cachedInput * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}