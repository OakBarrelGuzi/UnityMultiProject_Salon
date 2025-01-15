using System.Collections;
using UnityEngine;

public class Card : MonoBehaviour
{
    internal bool cardOpen = false;

    internal bool isTurning = false;

    private MemoryGameManager memoryGameManager;

    public InGameCard cardData {get;set;}

    public Quaternion startQuaternion { get;set;}

    public Quaternion targetQuaternion { get;set;}

    private SpriteRenderer spriteRenderer;

    public float turnSpeed { get; set; } = 1f;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
    }

    public void Initialize(MemoryGameManager Manager)
    {
        this.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

        this.memoryGameManager = Manager;

        spriteRenderer.sprite = cardData.cardSprite;

        startQuaternion = gameObject.transform.localRotation;

        targetQuaternion = Quaternion.Euler(0, 0, 180f);
    }

    private void OnMouseDown()
    {
        if (cardOpen == true || memoryGameManager.isCardFull == true) return;
        memoryGameManager.CardOpen(this);

    }

    public IEnumerator TurnRoutine(Card card)
    {
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;
            card.transform.localRotation =
                Quaternion.Slerp(startQuaternion,
                targetQuaternion,
                elapsedTime * turnSpeed);
            yield return null;
        }
    }


}

public struct InGameCard
{
    public CARDTYPE cardType;
    public Sprite cardSprite;
}


public enum CARDTYPE
{
    one,
    two,
    three,
    four,
    five,
    six,
    seven,
}
