using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DartGame : MonoBehaviour
{
    private const float TURNTIME = 10f;
    private const float BREATHTIME = 3f;

    private int Round = 1;
    private int Turn = 0;
    
    private float turnTime = TURNTIME;
    private float breathTime = BREATHTIME;

    private bool isWaitInput = false;
    private bool isWaitBreath = false;
    private bool isWaitShootButton = false;

    private bool isRandomMoving = false;

    private bool isMove = true;
    private bool isMoving = false;

    private Vector3 randomTargetPosition;

    private List<TextMeshProUGUI> scoreTextList = new List<TextMeshProUGUI>();


    [SerializeField,Header("조이스틱 무브 스피드")] 
    private float joystickMoveSpeed = 0.3f;
    [SerializeField,Header("랜덤 방향 무브 스피드")] 
    private float randomMoveSpeed = 0.15f;

    [SerializeField]
    private DartGameUI gameUi;

    [SerializeField]
    private Dart dart;

    [SerializeField]
    private JoyStick joyStick;

    [SerializeField]
    private AimingRing aimingRingPrefab;

    private AimingRing targetAiming;

    [SerializeField]
    private Arrow arrowPrefab;

    [SerializeField]
    private Transform arrowShootPoint;

    private void Start()
    {
        SpawnAimingRing();
        StartCoroutine(GameRoutine());
    }

    private void Update()
    {
        AimingMove();
        RandomMove();
        DartGameTimeDecay();

        DartGameUiUpdate();


    }
    private void DartGameUiUpdate()
    {
        gameUi.turnTimeSlider.value = turnTime / TURNTIME;
        gameUi.turnTimeText.text = $"{(int)turnTime} / {TURNTIME}";
        gameUi.breathTimeSlider.value = breathTime / BREATHTIME;
    }

    private void DartGameTimeDecay()
    {
        if (isMoving == false && isWaitBreath == false) turnTime -= Time.deltaTime;
        else if(isWaitBreath == false) { breathTime -= Time.deltaTime; }

        if (breathTime <= 0f && isWaitBreath == false)
        {
            isWaitBreath = true;
            isMove = false;
            isMoving = false;
        }
        if (turnTime <= 0f && isWaitBreath == false)
        {
            ShootButtonClick();
        }
    }

    public void AimingMove()
    {
        if (isMove == false) return;
        if (targetAiming == null)
        {
            SpawnAimingRing();
        }

        Vector3 moveDirection = GetJoystickDirection();
        if (moveDirection != Vector3.zero)
        {
            MoveTarget(moveDirection, joystickMoveSpeed);
        }
    }

    private void RandomMove()
    {
        if (!isRandomMoving || targetAiming == null) return;

        Vector3 moveDirection = (randomTargetPosition - targetAiming.transform.position).normalized;
        MoveTarget(moveDirection,randomMoveSpeed);
    }

    private Vector3 GetJoystickDirection()
    {
        Vector2 direction = joyStick.GetDirection();
        float magnitude = joyStick.GetMagnitude();

        if (magnitude < 1f)
        {
            isMoving = false;
            return Vector3.zero;
        }
        if (isWaitInput == false) isWaitInput = true;
        isMoving = true;

        return new Vector3(direction.x, direction.y, 0);
    }

    private void MoveTarget(Vector3 direction,float speed)
    {
        Vector3 newPosition = targetAiming.transform.position + direction * Time.deltaTime * speed;
        newPosition = ClampPositionToDartboard(newPosition);
        targetAiming.transform.position = newPosition;
    }

    private Vector3 ClampPositionToDartboard(Vector3 position)
    {
        float distanceFromCenter = Vector3.Distance(position, dart.center.position);
        float maxAllowedDistance = Vector3.Distance(dart.center.position, dart.outerLine.position)
            - Vector3.Distance(targetAiming.transform.position, targetAiming.outerLine.position);

        if (distanceFromCenter > maxAllowedDistance)
        {
            Vector3 directionFromCenter = (position - dart.center.position).normalized;
            position = dart.center.position + directionFromCenter * maxAllowedDistance;
        }

        return new Vector3(position.x, position.y, targetAiming.transform.position.z);
    }

    public void SpawnAimingRing()
    {
        AimingRing aiming = Instantiate(aimingRingPrefab);
        targetAiming = aiming;

        float dartDistance = Vector3.Magnitude(dart.center.position - dart.outerLine.position);
        float aminingDistance = Vector3.Magnitude(aiming.transform.position - aiming.outerLine.position);

        //print(dartDistance);
        //print(aminingDistance);

        float maxSpawnDistance = dartDistance - aminingDistance;

        //print(maxSpawnDistance);
        Vector2 ranPos = Random.insideUnitCircle;

        ranPos = ranPos * maxSpawnDistance;

        Vector3 spawnPosition = dart.center.position + new Vector3(ranPos.x, ranPos.y, -0.01f);

        aiming.transform.position = spawnPosition;

    }

    public void SpawnArrow()
    {
        float distance = Vector3.Distance(targetAiming.transform.position, targetAiming.innerLine.transform.position);

        Vector2 ran = Random.insideUnitCircle * distance;

        Vector3 arrowPoint = targetAiming.transform.position + new Vector3(ran.x, ran.y, 0);

        arrowPoint.z = dart.transform.position.z;

        Arrow arrow = Instantiate(arrowPrefab,arrowShootPoint);

        arrow.SetTargetPosition(arrowPoint);
    }

    private void SetRandomTargetPosition()
    {
        float dartDistance = Vector3.Distance(dart.center.position, dart.outerLine.position);
        float aimingDistance = Vector3.Distance(targetAiming.transform.position, targetAiming.outerLine.position);
        float maxDistance = dartDistance - aimingDistance;

        Vector2 randomDirection = Random.insideUnitCircle;
        Vector3 randomPosition = dart.center.position +
                               new Vector3(randomDirection.x, randomDirection.y, -0.01f) * maxDistance;
        
        randomPosition.z = targetAiming.transform.position.z;

        randomTargetPosition = randomPosition;
    }

    public void ShootButtonClick()
    {
        print("ShootButton호출");
        isRandomMoving = false;
        isMove = false;
        isWaitInput = true;
        isWaitBreath = true;

        gameUi.shootButton.onClick?.RemoveListener(ShootButtonClick);
        StartCoroutine(reduceAimingRoutine());
    }

    public void ShootDartArrow()
    {
        isWaitShootButton = true;
        gameUi.shootButton.onClick?.RemoveListener(ShootDartArrow);
        SpawnArrow();
    }

    public void AddScores()
    {
        if (scoreTextList.Count < Round)
        {
            TextMeshProUGUI scoretext = Instantiate(gameUi.scoreTextPrefab, gameUi.scoreTextField);
            scoreTextList.Add(scoretext);
            scoreTextList[Round -1].text = dart.roundscores.ToString();
        }
        else
        {
            scoreTextList[Round -1].text = dart.roundscores.ToString();
        }
    }

    private IEnumerator GameRoutine()
    {
        StartCoroutine(RingRandomMoveRoutine());
        gameUi.shootButton.onClick.AddListener(ShootButtonClick);
        yield return new WaitUntil(() => isWaitInput == true);

        yield return new WaitUntil(() => isWaitBreath == true);
        gameUi.shootButton.onClick?.RemoveListener(ShootButtonClick);
        gameUi.shootButton.onClick.AddListener(ShootDartArrow);

        yield return new WaitUntil(()=> isWaitShootButton == true);
        gameUi.shootButton.onClick?.RemoveListener(ShootDartArrow);

        yield return new WaitForSeconds(arrowPrefab.duration + 2f);
        AddScores();
    }
    
    private IEnumerator reduceAimingRoutine()
    {   
        //TODO:원 최소 사이즈 제한 (코루틴 탈출)
        while (!isWaitShootButton)
        {
            targetAiming.transform.localScale = targetAiming.transform.localScale * 0.97f;
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator RingRandomMoveRoutine()
    {
        while (!isWaitBreath)
        {
            isRandomMoving = !isWaitBreath;
            if (isRandomMoving)
            {
                SetRandomTargetPosition();

                yield return new WaitForSeconds(1.5f);
            }
            yield return null;
        }
        isRandomMoving = false;
    }
}
