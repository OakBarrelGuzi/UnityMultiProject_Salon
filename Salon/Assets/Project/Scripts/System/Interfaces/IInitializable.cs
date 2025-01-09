using UnityEngine;

namespace Salon.Interfaces
{
    public interface IInitializable
    {
        bool IsInitialized { get; }
        void Initialize();
    }
}