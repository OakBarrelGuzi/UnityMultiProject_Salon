using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class JoyStick : MonoBehaviour, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    [SerializeField, Header("조이스틱 핸들")]
    private RectTransform handle;
    //터치한 위치를 담을 변수
    private Vector2 touch;
    //반지름을 담을 변수
    private float widthHalf;
    //핸들 시작지점
    private Vector2 startHandlePoint;

    [SerializeField, Header("핸들 움직임 속도")]
    private float speed = 0.35f;

    [SerializeField, Header("핸들 최대 이동거리")]
    private float length = 50f;

    [SerializeField, Header("핸들 포커스 민감도")]
    private float joyStickSensitive = 10f;

    [SerializeField, Header("핸들 이동방향 포커스 오브젝트")]
    private JoyStickFocus[] handleFocus;

    private Dictionary<FOCUSTYPE, JoyStickFocus> handleFocusDict =
        new Dictionary<FOCUSTYPE, JoyStickFocus>();

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        widthHalf = GetComponent<RectTransform>().sizeDelta.x * 0.5f;

        startHandlePoint = handle.localPosition;

        foreach (JoyStickFocus joyStickFocus in handleFocus)
        {
            handleFocusDict.Add(joyStickFocus.focusType, joyStickFocus);
            joyStickFocus.gameObject.SetActive(false);
        }
    }

    //터치하는 순간 메세지 함수
    public void OnPointerClick(PointerEventData eventData)
    {
        print("클릭함");
    }

    //드래그중 메세지 함수
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPos = ((Vector2)transform.position - eventData.position) * speed / widthHalf;

        if (touchPos.magnitude > length)
        {
            touchPos = touchPos.normalized * length;
        }

        touch = touchPos / speed;
        handle.anchoredPosition = -touchPos * widthHalf;
        FocusSetActive();
    }

    //방향에따른 포커스 꺼짐켜짐
    private void FocusSetActive()
    {

        Dictionary<(float value, float threshold), FOCUSTYPE> directionMap = new Dictionary<(float value, float threshold), FOCUSTYPE>
    {
        { (touch.x, joyStickSensitive), FOCUSTYPE.RIGHT },
        { (-touch.x, joyStickSensitive), FOCUSTYPE.LEFT },
        { (touch.y, joyStickSensitive), FOCUSTYPE.TOP },
        { (-touch.y, joyStickSensitive), FOCUSTYPE.BOTTOM }
    };

        foreach (var focus in handleFocusDict.Values)
        {
            focus.gameObject.SetActive(false);
        }

        foreach (var direction in directionMap)
        {
            if (direction.Key.value > direction.Key.threshold)
            {
                if (handleFocusDict.TryGetValue(direction.Value, out JoyStickFocus focus))
                {
                    focus.gameObject.SetActive(true);
                }
            }
        }
    }

    //드래그끝 메세지 함수
    public void OnEndDrag(PointerEventData eventData)
    {
        handle.anchoredPosition = startHandlePoint;
        touch = Vector2.zero;
        foreach (JoyStickFocus handle in handleFocus)
        {
            handle.gameObject.SetActive(false);
        }
    }

    //조이스틱이 향하고있는 방향
    public Vector2 GetDirection()
    {
        return touch.normalized;
    }

    //조이스틱의 강도
    public float GetMagnitude()
    {
        return touch.magnitude / length;
    }

}
