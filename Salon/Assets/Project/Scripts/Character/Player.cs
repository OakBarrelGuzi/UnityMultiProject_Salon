using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Controller;
using UnityEngine.AI;
namespace Salon.Character
{
    public class Player : MonoBehaviour
    {
        private string displayName;
        private bool isLocalPlayer;
        private NavMeshAgent agent;

        public void Initialize(string displayName, bool isLocalPlayer)
        {
            this.displayName = displayName;
            this.isLocalPlayer = isLocalPlayer;

            if (isLocalPlayer)
            {
                //Todo : UIManager 통해서 inputController 할당 받기
            }
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            agent.SetDestination(newPosition);
        }
    }
}