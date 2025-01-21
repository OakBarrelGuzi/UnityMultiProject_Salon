using UnityEngine;
using UnityEngine.UI;
using Salon.Firebase;

namespace Salon.DartGame
{
    public class ReCheckPanel : MonoBehaviour
    {
        public Button rechckYesButton;
        public Button rechckNoButton;

        private void Awake()
        {
            rechckYesButton.onClick.AddListener(async () =>
            {
                // 씬 전환 전에 현재 플레이어 정리
                RoomManager.Instance.DestroyAllPlayers();
                await RoomManager.Instance.UnsubscribeFromChannel();

                UIManager.Instance.ClosePanel(PanelType.Dart);

                // 씬 전환만 수행하고 나머지는 ScenesManager가 처리
                ScenesManager.Instance.ChanageScene("LobbyScene");
                gameObject.SetActive(false);
            });

            rechckNoButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}