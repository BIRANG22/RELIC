using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimelineSlotUI : MonoBehaviour
{
    [Header("Owner Icon")]
    [SerializeField] private Image ownerIconImage;

    [Header("Action Type Icons Max 3")]
    [SerializeField] private Image[] actionTypeIconImages;

    [Header("Slot Setting")]
    [SerializeField] private int maxActionCount = 3;

    [Header("Action Icon Layout X Only")]
    [SerializeField] private float oneIconX = 0f;

    [SerializeField] private float twoIconLeftX = -30f;
    [SerializeField] private float twoIconRightX = 30f;

    [SerializeField] private float threeIconLeftX = -48f;
    [SerializeField] private float threeIconCenterX = 0f;
    [SerializeField] private float threeIconRightX = 48f;

    private class ActionIconData
    {
        public Sprite Icon;
        public string MonsterRuntimeId;
    }

    private readonly List<ActionIconData> actionTypeIcons = new();

    private void Awake()
    {
        Refresh();
    }

    public void SetOwnerIcon(Sprite ownerIcon)
    {
        if (ownerIconImage == null)
            return;

        ownerIconImage.sprite = ownerIcon;
        ownerIconImage.gameObject.SetActive(ownerIcon != null);
        ownerIconImage.enabled = ownerIcon != null;
    }

    public void AddActionTypeIcon(Sprite actionTypeIcon, string monsterRuntimeId = "")
    {
        if (actionTypeIcon == null)
        {
            Debug.LogWarning($"{name}: ActionType 아이콘이 null입니다.");
            return;
        }

        if (actionTypeIcons.Count >= maxActionCount)
            actionTypeIcons.RemoveAt(0);

        actionTypeIcons.Add(new ActionIconData
        {
            Icon = actionTypeIcon,
            MonsterRuntimeId = monsterRuntimeId
        });

        Refresh();
    }

    public void Clear()
    {
        actionTypeIcons.Clear();

        if (ownerIconImage != null)
        {
            ownerIconImage.sprite = null;
            ownerIconImage.gameObject.SetActive(false);
            ownerIconImage.enabled = false;
        }

        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < actionTypeIconImages.Length; i++)
        {
            if (actionTypeIconImages[i] == null)
                continue;

            actionTypeIconImages[i].sprite = null;
            actionTypeIconImages[i].enabled = false;
            actionTypeIconImages[i].gameObject.SetActive(false);

            RectTransform rt = actionTypeIconImages[i].rectTransform;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        for (int i = 0; i < actionTypeIcons.Count && i < actionTypeIconImages.Length; i++)
        {
            if (actionTypeIconImages[i] == null)
                continue;

            actionTypeIconImages[i].sprite = actionTypeIcons[i].Icon;
            actionTypeIconImages[i].enabled = true;
            actionTypeIconImages[i].gameObject.SetActive(true);

            BattleTimelineMonsterHoverTarget hoverTarget =
                actionTypeIconImages[i].GetComponent<BattleTimelineMonsterHoverTarget>();

            if (hoverTarget == null)
                hoverTarget = actionTypeIconImages[i].gameObject.AddComponent<BattleTimelineMonsterHoverTarget>();

            hoverTarget.SetMonsterRuntimeId(actionTypeIcons[i].MonsterRuntimeId);
        }

        UpdateActionIconLayout();
    }

    private void UpdateActionIconLayout()
    {
        int count = Mathf.Min(actionTypeIcons.Count, actionTypeIconImages.Length);

        if (count == 1)
        {
            SetActionIconX(0, oneIconX);
        }
        else if (count == 2)
        {
            SetActionIconX(0, twoIconLeftX);
            SetActionIconX(1, twoIconRightX);
        }
        else if (count >= 3)
        {
            SetActionIconX(0, threeIconLeftX);
            SetActionIconX(1, threeIconCenterX);
            SetActionIconX(2, threeIconRightX);
        }
    }

    private void SetActionIconX(int index, float x)
    {
        if (index < 0 || index >= actionTypeIconImages.Length)
            return;

        if (actionTypeIconImages[index] == null)
            return;

        RectTransform rt = actionTypeIconImages[index].rectTransform;

        Vector3 pos = rt.localPosition;
        pos.x = x;          // 로컬 기준 x만 변경
        rt.localPosition = pos;
    }
}