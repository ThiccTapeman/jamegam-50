using System.Collections;
using UnityEngine;
using ThiccTapeman.Input;

public class UICameraLerp : MonoBehaviour
{
    [Header("Positions")]
    [SerializeField] Transform startPoint;
    [SerializeField] Transform targetPoint;

    [Header("Lerp")]
    [SerializeField, Min(0.01f)] float moveDuration = 0.75f;
    [SerializeField] bool useUnscaledTime = true;

    [Header("Input")]
    [SerializeField] bool allowBackInput = true;
    [SerializeField] string backActionMap = "UI";
    [SerializeField] string backActionName = "Back";
    [SerializeField] bool fallbackToEscapeBinding = true;

    Vector3 cachedStartPosition;
    Coroutine moveRoutine;
    InputItem backAction;

    void Awake()
    {
        cachedStartPosition = startPoint != null ? startPoint.position : transform.position;
    }

    void OnEnable()
    {
        if (!allowBackInput) return;

        InputManager inputManager = InputManager.GetInstance();
        backAction = inputManager.GetAction(backActionMap, backActionName);
        if (backAction == null && fallbackToEscapeBinding)
        {
            backAction = inputManager.GetTempAction("UICameraLerp_Back", "<Keyboard>/escape");
        }
    }

    void Update()
    {
        if (!allowBackInput) return;
        if (backAction != null && backAction.GetTriggered(true))
        {
            MoveToStart();
        }
    }

    public void MoveToTarget()
    {
        if (targetPoint == null) return;
        StartMove(targetPoint.position);
    }

    public void MoveToStart()
    {
        Vector3 start = startPoint != null ? startPoint.position : cachedStartPosition;
        StartMove(start);
    }

    void StartMove(Vector3 destination)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }
        moveRoutine = StartCoroutine(MoveRoutine(destination));
    }

    IEnumerator MoveRoutine(Vector3 destination)
    {
        Vector3 from = transform.position;
        float duration = Mathf.Max(0.01f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(from, destination, eased);
            yield return null;
        }

        transform.position = destination;
        moveRoutine = null;
    }
}
