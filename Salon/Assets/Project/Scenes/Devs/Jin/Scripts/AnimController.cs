using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;


//TODO: 애니클립교체 개발개발중
public class AnimController : MonoBehaviour 
{
    public List<AnimationClip> clipList = new List<AnimationClip>();
    
    public AnimationClip TestClip;

    public Animator animator;

    private AnimatorOverrideController currentController;

    private void Start()
    {
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

    public void ClipChange(string clipname)
    {
        if (currentController == null)       
        {
            currentController = SetupOverrideController();
        }

        //클립 string으로 찾아 변경
        AnimationClip targetClip = clipList.Find(clip => clip.name == clipname);
        if(targetClip != null)
        {
            currentController["ActionState"] = targetClip;
        }
        else
        {
            print($"Animation clip '{clipname}' not found in clipList");
        }
    }

    public void ClipChange(AnimationClip animationClip)
    {
        if (currentController == null)
        {
            currentController = SetupOverrideController();
        }

        //클립 자체로 변경(클립 이름을 꺼내야 바꿀수있음)
        if (animationClip != null)
        {
            currentController["IdleArmSwing"] = animationClip;
        }
    }

    public void AnimatorChange(RuntimeAnimatorController controller)
    {
        animator.runtimeAnimatorController = controller;

        currentController = SetupOverrideController();
    }

}
