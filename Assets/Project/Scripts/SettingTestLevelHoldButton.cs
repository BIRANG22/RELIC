using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingTestLevelHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private enum HoldDirection
    {
        LevelUp,
        LevelDown
    }

    [Header("Target")]
    [SerializeField] private Setting targetSetting;
    [SerializeField] private HoldDirection direction = HoldDirection.LevelUp;

    [Header("Hold Repeat")]
    [SerializeField] private float repeatStartDelay = 0.35f;
    [SerializeField] private float repeatInterval = 0.08f;

    private Button button;
    private Coroutine holdCoroutine;
    private bool isHolding;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (targetSetting == null)
            targetSetting = FindFirstObjectByType<Setting>(FindObjectsInactive.Include);
    }

    private void OnDisable()
    {
        StopHold();
    }

    public void Setup(Setting setting, bool isLevelUpButton, float startDelay, float interval)
    {
        targetSetting = setting;
        direction = isLevelUpButton ? HoldDirection.LevelUp : HoldDirection.LevelDown;
        repeatStartDelay = Mathf.Max(0f, startDelay);
        repeatInterval = Mathf.Max(0.02f, interval);

        if (button == null)
            button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        if (button != null && !button.interactable)
            return;

        isHolding = true;

        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);

        holdCoroutine = StartCoroutine(HoldRepeatRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHold();
    }

    private IEnumerator HoldRepeatRoutine()
    {
        if (repeatStartDelay > 0f)
            yield return new WaitForSecondsRealtime(repeatStartDelay);

        while (isHolding)
        {
            InvokeLevelButton();

            if (repeatInterval > 0f)
                yield return new WaitForSecondsRealtime(repeatInterval);
            else
                yield return null;
        }
    }

    private void InvokeLevelButton()
    {
        if (targetSetting == null)
            targetSetting = FindFirstObjectByType<Setting>(FindObjectsInactive.Include);

        if (targetSetting == null)
            return;

        if (direction == HoldDirection.LevelUp)
            targetSetting.OnClickTestLevelUp();
        else
            targetSetting.OnClickTestLevelDown();
    }

    private void StopHold()
    {
        isHolding = false;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }
}
