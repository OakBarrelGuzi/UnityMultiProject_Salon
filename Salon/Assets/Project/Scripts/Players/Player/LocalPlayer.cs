using UnityEngine;
using Salon.Controller;
using Salon.Firebase.Database;

namespace Salon.Character
{
    public class LocalPlayer : Player
    {
        [SerializeField]
        private float positionUpdateInterval = 3f;
        private float lastPositionUpdateTime;
        private InputController inputController;

        public override void Initialize(string displayName)
        {
            base.Initialize(displayName);
            inputController = GetComponent<InputController>();
            if (inputController != null)
            {
                inputController.enabled = true;
                inputController.Initialize();
            }
            lastPositionUpdateTime = Time.time;
        }

        public NetworkPositionData UpdateNetworkPosition()
        {
            bool shouldUpdatePosition = Time.time - lastPositionUpdateTime >= positionUpdateInterval;
            if (shouldUpdatePosition)
            {
                lastPositionUpdateTime = Time.time;
            }

            Vector3 direction = inputController != null ? inputController.CurrentVelocity.normalized : transform.forward;
            return new NetworkPositionData(transform.position, direction, shouldUpdatePosition);
        }
    }
}