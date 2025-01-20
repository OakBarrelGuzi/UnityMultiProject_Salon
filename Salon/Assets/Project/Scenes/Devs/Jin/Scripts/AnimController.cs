using UnityEngine;

public class AnimController : MonoBehaviour
{

    private Animator animator;

    private AnimatorOverrideController currentController;

    private string originalCMotionName = "IdleArmSwing";

    public AnimationClip TestClip;

    //가지고있는대상에서 명시적으로 초기화 해야함
    private void Start()
    {
        Initialize();
        ClipChange(TestClip);
        animator.SetTrigger("ASTrigger");
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
            currentController[originalCMotionName] = animationClip;
        }
    }

    [Tooltip("Animator를 변경할때 사용 변경할려는 State이름 동일하게 해야함")]
    public void AnimatorChange(RuntimeAnimatorController controller)
    {
        animator.runtimeAnimatorController = controller;

        currentController = SetupOverrideController();
    }

}
