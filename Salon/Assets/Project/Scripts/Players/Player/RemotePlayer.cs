using UnityEngine;
using Salon.Firebase.Database;

namespace Salon.Character
{
    public class RemotePlayer : Player
    {
        private Vector3 networkPosition;
        private Vector3 networkDirection;
        private const float MOVE_SPEED = 20f;

        [SerializeField]
        private float interpolationSpeed = 10f;

        public override void Initialize(string displayName)
        {
            base.Initialize(displayName);
            networkPosition = transform.position;
            networkDirection = Vector3.zero;
        }

        private void Update()
        {
            Vector3 predictedPosition = networkPosition + (networkDirection * MOVE_SPEED * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, predictedPosition, Time.deltaTime * interpolationSpeed);
        }

        public void GetNetworkPosition(NetworkPositionData posData)
        {
            var position = posData.GetPosition();
            if (position.HasValue)
            {
                networkPosition = position.Value;
            }

            networkDirection = posData.GetDirection();
        }
    }
}