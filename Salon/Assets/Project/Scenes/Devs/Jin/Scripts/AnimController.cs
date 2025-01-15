using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;


//TODO: 애니클립교체 개발개발중
public class AnimController : MonoBehaviour 
{
    public List<AnimationClip> clipList = new List<AnimationClip>();
    
    public AnimationClip TestClip;
    public AnimationClip TestClip2;

    public Animator animator;

    private AnimatorOverrideController currentController;

    [SerializeField,Header("바꿀려고하는 Animator의 State이름 다르면 오류뜸")]
    private string targetClipName = "ActionState";

    private string originalClipName = "";

    private void Start()
    {
       GetOriginalClipName();
       ClipChange(TestClip);
       ClipChange(TestClip2);
       animator.SetTrigger("ASTrigger");
    }
 
    public void Initialize()
    {
        animator = GetComponent<Animator>();

        currentController = SetupOverrideController();

        GetOriginalClipName();
    }

    private AnimatorOverrideController SetupOverrideController()
    {
        RuntimeAnimatorController Controller = animator.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(Controller);
        animator.runtimeAnimatorController = overrideController;
        return overrideController;
    }

    private void GetOriginalClipName()
    {
        if(animator.runtimeAnimatorController is AnimatorController controller)
        {
             ChildAnimatorState[] states = controller.layers[0].stateMachine.states;

           foreach(ChildAnimatorState state in states)
            {
                if(state.state.name == targetClipName &&
                    state.state.motion is AnimationClip clip)
                {
                    originalClipName = clip.name;
                    print(originalClipName);
                }
            }
        }


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
            currentController[originalClipName] = targetClip;
        }
        else
        {
            print($"리스트에 '{clipname}' 이거없음");
        }
    }

    public void ClipChange(AnimationClip animationClip)
    {
        if (currentController == null)
        {
            currentController = SetupOverrideController();
        }

        if (animationClip != null)
        {
            currentController[originalClipName] = animationClip;
        }
    }

    [Tooltip("Animator를 변경할때 사용 변경할려는 State이름 동일하게 해야함")]
    public void AnimatorChange(RuntimeAnimatorController controller)
    {
        animator.runtimeAnimatorController = controller;

        currentController = SetupOverrideController();
        GetOriginalClipName();
    }

}
