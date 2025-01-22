using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class AnimController : MonoBehaviour
{

    private Animator animator;

    private AnimatorOverrideController currentController;

    private string originalMotionName = "ArmSwing";

    public SpriteRenderer EmojiImage;

    //가지고있는대상에서 명시적으로 초기화 해야함
    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        animator = GetComponent<Animator>();
        currentController = SetupOverrideController();
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
        EmojiImage.sprite = ItemManager.Instance.GetEmojiSprite(emojiName);
        EmojiImage.gameObject.SetActive(true);

        // 이모지 스프라이트가 항상 카메라를 향하도록 설정
        if (Camera.main != null)
        {
            // 카메라의 forward 방향의 반대 방향을 바라보도록 설정 (빌보드 효과)
            EmojiImage.transform.forward = -Camera.main.transform.forward;
        }

        await Task.Delay(3000);
        EmojiImage.gameObject.SetActive(false);
    }

    public void SetAnime(string animName)
    {
        AnimationClip clip = ItemManager.Instance.GetAnimClip(animName);
        ClipChange(clip);
        animator.SetTrigger("ASTrigger");
    }
}
