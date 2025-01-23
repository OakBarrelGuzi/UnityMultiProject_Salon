using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

namespace Salon.Character
{
    public class AnimController : MonoBehaviour
    {
        private Animator animator;

        private AnimatorOverrideController currentController;

        private string originalMotionName = "ArmSwing";

        public SpriteRenderer EmojiImage;

        private bool isShowingEmoji = false;

        public bool IsPlayingAnimation { get; private set; }
        public event Action<bool> OnAnimationStateChanged;
        public event Action<string> OnEmojiChanged;
        private string currentAnimationName;
        public string CurrentAnimationName => currentAnimationName;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            currentController = SetupOverrideController();
            IsPlayingAnimation = false;
        }

        private AnimatorOverrideController SetupOverrideController()
        {
            RuntimeAnimatorController Controller = animator.runtimeAnimatorController;
            AnimatorOverrideController overrideController = new AnimatorOverrideController(Controller);
            animator.runtimeAnimatorController = overrideController;
            return overrideController;
        }

        public void ClipChange(AnimationClip animationClip)
        {
            if (currentController == null)
            {
                currentController = SetupOverrideController();
            }

            if (animationClip != null)
            {
                currentController[originalMotionName] = animationClip;
            }
        }

        public async void SetEmoji(string emojiName)
        {
            if (string.IsNullOrEmpty(emojiName)) return;

            OnEmojiChanged?.Invoke(emojiName);
            EmojiImage.sprite = ItemManager.Instance.GetEmojiSprite(emojiName);
            EmojiImage.gameObject.SetActive(true);
            isShowingEmoji = true;

            StartCoroutine(UpdateEmojiFacing());

            await Task.Delay(3000);
            isShowingEmoji = false;
            EmojiImage.gameObject.SetActive(false);

        }

        private IEnumerator UpdateEmojiFacing()
        {
            while (isShowingEmoji && EmojiImage != null && EmojiImage.gameObject.activeSelf)
            {
                if (Camera.main != null)
                {
                    EmojiImage.transform.forward = -Camera.main.transform.forward;
                }
                yield return null;
            }
        }

        public void SetAnime(string animName)
        {
            if (string.IsNullOrEmpty(animName)) return;

            AnimationClip clip = ItemManager.Instance.GetAnimClip(animName);
            if (clip != null)
            {
                currentAnimationName = animName;
                ClipChange(clip);
                IsPlayingAnimation = true;
                OnAnimationStateChanged?.Invoke(true);
                animator.SetTrigger("ASTrigger");

                StartCoroutine(WaitForAnimationEnd(clip.length));
            }
            else
            {
                Debug.LogError($"[AnimController] 애니메이션 클립을 찾을 수 없습니다: {animName}");
            }
        }

        private IEnumerator WaitForAnimationEnd(float duration)
        {
            yield return new WaitForSeconds(duration);
            IsPlayingAnimation = false;
            OnAnimationComplete();
        }

        // 애니메이션 종료 시 호출되는 이벤트 함수
        public void OnAnimationComplete()
        {
            OnAnimationStateChanged?.Invoke(false);
        }
    }
}
