using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleButtonSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class MoveTarget
    {
        public Transform target;

        [Header("Move Position")]
        public Vector3 startLocalPosition;
        public Vector3 endLocalPosition;

        [Header("Move Rotation")]
        public Vector3 startLocalEulerAngles;
        public Vector3 endLocalEulerAngles;

        [Header("Move Option")]
        public float moveDuration = 0.5f;
    }

    [Header("Button")]
    [SerializeField] private Button titleButton;

    [Header("Objects")]
    [SerializeField] private GameObject object0; // GameObject
    [SerializeField] private GameObject object1; // GameObject (1)
    [SerializeField] private GameObject object2; // GameObject (2)

    [Header("GameObject Move Targets")]
    [SerializeField] private MoveTarget[] object0MoveTargets;

    [Header("GameObject (1) Move Targets")]
    [SerializeField] private MoveTarget[] object1MoveTargets;

    [Header("GameObject (2) Move Targets")]
    [SerializeField] private MoveTarget[] object2MoveTargets;

    private Coroutine moveCoroutine;

    private void Awake()
    {
        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(OnClickTitleButton);
            titleButton.onClick.AddListener(OnClickTitleButton);
        }
    }

    private void OnDestroy()
    {
        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(OnClickTitleButton);
        }
    }

    private void OnClickTitleButton()
    {
        int currentIndex = GetCurrentActiveIndex();
        int nextIndex = GetRandomNextIndex(currentIndex);

        SetActiveObject(nextIndex);
    }

    private int GetCurrentActiveIndex()
    {
        if (object0 != null && object0.activeSelf)
        {
            return 0;
        }

        if (object1 != null && object1.activeSelf)
        {
            return 1;
        }

        if (object2 != null && object2.activeSelf)
        {
            return 2;
        }

        return -1;
    }

    private int GetRandomNextIndex(int currentIndex)
    {
        if (currentIndex == 0)
        {
            return Random.Range(0, 2) == 0 ? 1 : 2;
        }

        if (currentIndex == 1)
        {
            return Random.Range(0, 2) == 0 ? 0 : 2;
        }

        if (currentIndex == 2)
        {
            return Random.Range(0, 2) == 0 ? 0 : 1;
        }

        return Random.Range(0, 3);
    }

    private void SetActiveObject(int index)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (object0 != null)
        {
            object0.SetActive(index == 0);
        }

        if (object1 != null)
        {
            object1.SetActive(index == 1);
        }

        if (object2 != null)
        {
            object2.SetActive(index == 2);
        }

        if (index == 0)
        {
            moveCoroutine = StartCoroutine(MoveTargetsCoroutine(object0MoveTargets));
        }
        else if (index == 1)
        {
            moveCoroutine = StartCoroutine(MoveTargetsCoroutine(object1MoveTargets));
        }
        else if (index == 2)
        {
            moveCoroutine = StartCoroutine(MoveTargetsCoroutine(object2MoveTargets));
        }
    }

    private IEnumerator MoveTargetsCoroutine(MoveTarget[] moveTargets)
    {
        if (moveTargets == null)
        {
            yield break;
        }

        for (int i = 0; i < moveTargets.Length; i++)
        {
            MoveTarget moveTarget = moveTargets[i];

            if (moveTarget == null)
            {
                continue;
            }

            if (moveTarget.target == null)
            {
                continue;
            }

            moveTarget.target.localPosition = moveTarget.startLocalPosition;
            moveTarget.target.localRotation = Quaternion.Euler(moveTarget.startLocalEulerAngles);
        }

        float maxDuration = GetMaxDuration(moveTargets);
        float elapsedTime = 0f;

        while (elapsedTime < maxDuration)
        {
            elapsedTime += Time.deltaTime;

            for (int i = 0; i < moveTargets.Length; i++)
            {
                MoveTarget moveTarget = moveTargets[i];

                if (moveTarget == null)
                {
                    continue;
                }

                if (moveTarget.target == null)
                {
                    continue;
                }

                float duration = Mathf.Max(0.01f, moveTarget.moveDuration);
                float t = Mathf.Clamp01(elapsedTime / duration);

                moveTarget.target.localPosition = Vector3.Lerp(
                    moveTarget.startLocalPosition,
                    moveTarget.endLocalPosition,
                    t
                );

                moveTarget.target.localRotation = Quaternion.Lerp(
                    Quaternion.Euler(moveTarget.startLocalEulerAngles),
                    Quaternion.Euler(moveTarget.endLocalEulerAngles),
                    t
                );
            }

            yield return null;
        }

        for (int i = 0; i < moveTargets.Length; i++)
        {
            MoveTarget moveTarget = moveTargets[i];

            if (moveTarget == null)
            {
                continue;
            }

            if (moveTarget.target == null)
            {
                continue;
            }

            moveTarget.target.localPosition = moveTarget.endLocalPosition;
            moveTarget.target.localRotation = Quaternion.Euler(moveTarget.endLocalEulerAngles);
        }

        moveCoroutine = null;
    }

    private float GetMaxDuration(MoveTarget[] moveTargets)
    {
        float maxDuration = 0f;

        if (moveTargets == null)
        {
            return maxDuration;
        }

        for (int i = 0; i < moveTargets.Length; i++)
        {
            if (moveTargets[i] == null)
            {
                continue;
            }

            if (moveTargets[i].moveDuration > maxDuration)
            {
                maxDuration = moveTargets[i].moveDuration;
            }
        }

        return Mathf.Max(0.01f, maxDuration);
    }
}