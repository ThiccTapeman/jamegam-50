using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    static SceneTransition instance;

    public static SceneTransition GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<SceneTransition>();
            if (instance == null)
            {
                GameObject obj = new GameObject("SceneTransition");
                instance = obj.AddComponent<SceneTransition>();
            }
        }
        return instance;
    }

    [Header("Panels")]
    [SerializeField] Transform leftPanel;
    [SerializeField] Transform rightPanel;
    [SerializeField] Canvas transitionCanvas;

    [Header("Positions (local)")]
    [SerializeField] float closedCenterX = 0f;
    [SerializeField] bool useCurrentAsOpen = true;
    [SerializeField] float openLeftX = -4f;
    [SerializeField] float openRightX = 4f;
    [SerializeField] bool useCenterOffsets = true;
    [SerializeField] float openOffsetX = 4f;
    [SerializeField] bool startClosed = false;

    [Header("Timing")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] bool dontDestroyOnLoad = true;

    Vector3 leftOpenLocal;
    Vector3 rightOpenLocal;
    Vector3 leftClosedLocal;
    Vector3 rightClosedLocal;
    bool isTransitioning;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (dontDestroyOnLoad)
        {
            Transform root = transitionCanvas != null ? transitionCanvas.transform : transform;
            if (root.parent != null)
                root.SetParent(null);
            DontDestroyOnLoad(root.gameObject);
        }

        if (leftPanel == null || rightPanel == null) return;
        if (transitionCanvas != null)
        {
            if (leftPanel.parent != transitionCanvas.transform)
                leftPanel.SetParent(transitionCanvas.transform, true);
            if (rightPanel.parent != transitionCanvas.transform)
                rightPanel.SetParent(transitionCanvas.transform, true);
        }
        else
        {
            if (leftPanel.parent != transform)
                leftPanel.SetParent(transform, true);
            if (rightPanel.parent != transform)
                rightPanel.SetParent(transform, true);
        }

        if (useCenterOffsets)
        {
            leftOpenLocal = leftPanel.localPosition;
            rightOpenLocal = rightPanel.localPosition;
            leftOpenLocal.x = closedCenterX - Mathf.Abs(openOffsetX);
            rightOpenLocal.x = closedCenterX + Mathf.Abs(openOffsetX);
        }
        else if (useCurrentAsOpen)
        {
            leftOpenLocal = leftPanel.localPosition;
            rightOpenLocal = rightPanel.localPosition;

            if (Mathf.Abs(leftOpenLocal.x - rightOpenLocal.x) < 0.001f)
            {
                leftOpenLocal.x = closedCenterX - Mathf.Abs(openOffsetX);
                rightOpenLocal.x = closedCenterX + Mathf.Abs(openOffsetX);
            }
        }
        else
        {
            leftOpenLocal = new Vector3(openLeftX, leftPanel.localPosition.y, leftPanel.localPosition.z);
            rightOpenLocal = new Vector3(openRightX, rightPanel.localPosition.y, rightPanel.localPosition.z);
        }

        leftClosedLocal = new Vector3(closedCenterX, leftOpenLocal.y, leftOpenLocal.z);
        rightClosedLocal = new Vector3(closedCenterX, rightOpenLocal.y, rightOpenLocal.z);

        if (startClosed)
        {
            leftPanel.localPosition = leftClosedLocal;
            rightPanel.localPosition = rightClosedLocal;
        }
        else
        {
            leftPanel.localPosition = leftOpenLocal;
            rightPanel.localPosition = rightOpenLocal;
        }
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        if (leftPanel == null || rightPanel == null)
        {
            Debug.LogWarning("SceneTransition missing panel references.");
            return;
        }
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;

        yield return MovePanels(leftClosedLocal, rightClosedLocal);

        SceneManager.LoadScene(sceneName);

        yield return MovePanels(leftOpenLocal, rightOpenLocal);

        isTransitioning = false;
    }

    IEnumerator MovePanels(Vector3 leftTarget, Vector3 rightTarget)
    {
        if (moveSpeed <= 0f)
        {
            leftPanel.localPosition = leftTarget;
            rightPanel.localPosition = rightTarget;
            yield break;
        }

        while (Vector3.Distance(leftPanel.localPosition, leftTarget) > 0.001f ||
               Vector3.Distance(rightPanel.localPosition, rightTarget) > 0.001f)
        {
            leftPanel.localPosition = Vector3.MoveTowards(
                leftPanel.localPosition,
                leftTarget,
                moveSpeed * Time.deltaTime
            );
            rightPanel.localPosition = Vector3.MoveTowards(
                rightPanel.localPosition,
                rightTarget,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
    }
}
