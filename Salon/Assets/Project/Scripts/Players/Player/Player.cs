using UnityEngine;

namespace Salon.Character
{
    public abstract class Player : MonoBehaviour
    {
        protected string displayName;

        public virtual void Initialize(string displayName)
        {
            this.displayName = displayName;
        }
    }
}