using UnityEngine;

namespace Salon.Character
{
    public abstract class Player : MonoBehaviour
    {
        protected string displayName;

        [SerializeField]
        protected bool isTesting = false;

        public virtual void Initialize(string displayName)
        {
            this.displayName = displayName;
        }
    }
}