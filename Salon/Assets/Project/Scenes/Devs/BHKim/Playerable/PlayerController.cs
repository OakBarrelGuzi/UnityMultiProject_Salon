using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private string displayName;
    private bool isLocalPlayer;

    public void Initialize(string displayName, bool isLocalPlayer)
    {
        this.displayName = displayName;
        this.isLocalPlayer = isLocalPlayer;

        if (!isLocalPlayer)
        {
            // 네트워크로만 동기화되는 플레이어는 입력 컨트롤러를 비활성화
            var inputController = GetComponent<PlayerInputController>();
            if (inputController != null)
            {
                inputController.enabled = false;
            }
        }
    }

    public void UpdatePosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}
