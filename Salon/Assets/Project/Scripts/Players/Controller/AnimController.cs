using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimController : MonoBehaviour
{

    private Animator animator;

    private AnimatorOverrideController currentController;

    private string originalMotionName = "ArmSwing";

    public SpriteRenderer EmojiImage;

    private bool isShowingEmoji = false;

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
        AnimationClip clip = ItemManager.Instance.GetAnimClip(animName);
        ClipChange(clip);
        animator.SetTrigger("ASTrigger");
    }
}
