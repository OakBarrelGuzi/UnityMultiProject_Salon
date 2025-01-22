
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class AnimController : MonoBehaviour
{

    private Animator animator;

    private AnimatorOverrideController currentController;

    private string originalMotionName = "IdleArmSwing";

    public Image EmojiImage;

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

    [Tooltip("Animator를 변경할때 사용 변경할려는 State이름 동일하게 해야함")]
    public void AnimatorChange(RuntimeAnimatorController controller)
    {
        animator.runtimeAnimatorController = controller;

        currentController = SetupOverrideController();
    }

    public async void SetEmoji(string emojiName)
    {
        EmojiImage.sprite = ItemManager.Instance.GetEmojiSprite(emojiName);
        EmojiImage.gameObject.SetActive(true);
        await Task.Delay(3000);
        EmojiImage.gameObject.SetActive(false);
    }

    public void SetAnime(string animName)
    {
        AnimationClip clip = ItemManager.Instance.GetAnimeClip(animName);
        ClipChange(clip);
        animator.SetTrigger("ASTrigger");
    }
}
