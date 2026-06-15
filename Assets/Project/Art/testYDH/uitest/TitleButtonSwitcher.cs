using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleButtonSwitcher : MonoBehaviour, IPointerEnterHandler
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

    [System.Serializable]
    public class SwitchObject
    {
        [Header("Main Object")]
        public GameObject targetObject;

        [Header("Back Main")]
        public GameObject backMainObject;

        [Header("Move Targets")]
        public MoveTarget[] moveTargets;
    }

    [Header("Button")]
    [SerializeField] private Button titleButton;

    [Header("Objects")]
    [Min(1)]
    [SerializeField] private int objectCount = 3;
    [SerializeField] private SwitchObject[] objects = new SwitchObject[3];

    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private SfxType hoverSfx = SfxType.MoveButtonHover;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.MoveButtonClick;

    private Coroutine moveCoroutine;

    private void OnValidate()
    {
        objectCount = Mathf.Max(1, objectCount);
        ResizeObjectsArray();
    }

    private void Awake()
    {
        ResizeObjectsArray();

        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(OnClickTitleButton);
            titleButton.onClick.AddListener(OnClickTitleButton);
        }
    }

    private void Start()
    {
        int currentIndex = GetCurrentActiveIndex();

        if (currentIndex < 0)
        {
            int firstValidIndex = GetFirstValidObjectIndex();

            if (firstValidIndex >= 0)
            {
                SetActiveObject(firstValidIndex);
            }
        }
        else
        {
            SetActiveObject(currentIndex);
        }
    }

    private void OnDestroy()
    {
        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(OnClickTitleButton);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSound();
    }

    private void OnClickTitleButton()
    {
        PlayClickSound();

        int currentIndex = GetCurrentActiveIndex();
        int nextIndex = GetRandomNextIndex(currentIndex);

        SetActiveObject(nextIndex);
    }

    private void PlayHoverSound()
    {
        if (!playHoverSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(hoverSfx);
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(clickSfx);
    }

    private void ResizeObjectsArray()
    {
        if (objects == null)
        {
            objects = new SwitchObject[objectCount];

            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = new SwitchObject();
            }

            return;
        }

        if (objects.Length == objectCount)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    objects[i] = new SwitchObject();
                }
            }

            return;
        }

        SwitchObject[] resizedObjects = new SwitchObject[objectCount];

        int copyCount = Mathf.Min(objects.Length, resizedObjects.Length);

        for (int i = 0; i < copyCount; i++)
        {
            resizedObjects[i] = objects[i];
        }

        for (int i = 0; i < resizedObjects.Length; i++)
        {
            if (resizedObjects[i] == null)
            {
                resizedObjects[i] = new SwitchObject();
            }
        }

        objects = resizedObjects;
    }

    private int GetCurrentActiveIndex()
    {
        if (objects == null)
        {
            return -1;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            if (objects[i].targetObject == null)
            {
                continue;
            }

            if (objects[i].targetObject.activeSelf)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetRandomNextIndex(int currentIndex)
    {
        int validCount = GetValidObjectCount();

        if (validCount <= 0)
        {
            return -1;
        }

        if (validCount == 1)
        {
            return GetFirstValidObjectIndex();
        }

        int nextIndex = currentIndex;

        while (nextIndex == currentIndex)
        {
            nextIndex = GetRandomValidObjectIndex();
        }

        return nextIndex;
    }

    private int GetValidObjectCount()
    {
        int validCount = 0;

        if (objects == null)
        {
            return validCount;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            if (objects[i].targetObject == null)
            {
                continue;
            }

            validCount++;
        }

        return validCount;
    }

    private int GetFirstValidObjectIndex()
    {
        if (objects == null)
        {
            return -1;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            if (objects[i].targetObject == null)
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private int GetRandomValidObjectIndex()
    {
        int validCount = GetValidObjectCount();

        if (validCount <= 0)
        {
            return -1;
        }

        int randomValidOrder = Random.Range(0, validCount);
        int currentValidOrder = 0;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            if (objects[i].targetObject == null)
            {
                continue;
            }

            if (currentValidOrder == randomValidOrder)
            {
                return i;
            }

            currentValidOrder++;
        }

        return -1;
    }

    private void SetActiveObject(int index)
    {
        if (index < 0)
        {
            return;
        }

        if (objects == null)
        {
            return;
        }

        if (index >= objects.Length)
        {
            return;
        }

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            bool isActive = i == index;

            if (objects[i].targetObject != null)
            {
                objects[i].targetObject.SetActive(isActive);
            }

            if (objects[i].backMainObject != null)
            {
                objects[i].backMainObject.SetActive(isActive);
            }
        }

        if (objects[index] != null)
        {
            moveCoroutine = StartCoroutine(MoveTargetsCoroutine(objects[index].moveTargets));
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