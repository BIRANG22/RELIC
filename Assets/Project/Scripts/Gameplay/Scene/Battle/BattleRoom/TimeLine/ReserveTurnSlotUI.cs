using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReserveTurnSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public const int MaxCommandCount = 5;

    [Header("Click")]
    [SerializeField] private bool autoBindButtonsInChildren = true;

    [Header("Slot Number Text")]
    [Tooltip("TurnMark/Key/KeyText에 표시할 슬롯 번호 텍스트입니다. 비어 있으면 KeyText를 자동 탐색합니다.")]
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private bool autoFindKeyText = true;

    [Header("TurnMark Hover")]
    [SerializeField] private Transform turnMarkTransform;
    [SerializeField] private Vector2 turnMarkNormalScale = new Vector2(1.5f, 1f);
    [SerializeField] private Vector2 turnMarkHoverScale = new Vector2(1.6f, 1.1f);

    private readonly List<PlayerReservedCommand> commands = new();

    private BattleTimelineController owner;
    private int slotIndex;
    private bool keyTextVisible = true;

    public int SlotIndex => slotIndex;
    public IReadOnlyList<PlayerReservedCommand> Commands => commands;
    public int CommandCount => commands.Count;
    public int OccupiedOrderCount
    {
        get
        {
            int total = 0;
            foreach (PlayerReservedCommand command in commands)
                if (command != null) total += command.TimelineEntryCost;
            return total;
        }
    }
    public int RemainingCommandCapacity => Mathf.Max(0, MaxCommandCount - OccupiedOrderCount);

    public void SetAutoBindButtonsInChildren(bool enabled)
    {
        autoBindButtonsInChildren = enabled;

        if (enabled)
        {
            BindButtons();
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].onClick.RemoveListener(OnClickSlot);
        }
    }

    public void Init(BattleTimelineController owner, int slotIndex)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;

        ResolveKeyText();
        ResolveTurnMark();
        RefreshKeyText();
        BindButtons();
    }

    private void Awake()
    {
        ResolveKeyText();
        ResolveTurnMark();
        BindButtons();
    }

    private void OnDisable()
    {
        SetTurnMarkHovered(false);
    }

    private void ResolveKeyText()
    {
        if (keyText != null || !autoFindKeyText)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child.name != "KeyText")
                continue;

            keyText = child.GetComponent<TMP_Text>();
            if (keyText != null)
                return;
        }
    }

    public void SetKeyTextVisible(bool visible)
    {
        keyTextVisible = visible;
        RefreshKeyText();
    }

    private void RefreshKeyText()
    {
        ResolveKeyText();

        if (keyText != null)
        {
            keyText.text = keyTextVisible ? (slotIndex + 1).ToString() : string.Empty;
        }
    }

    private void BindButtons()
    {
        if (!autoBindButtonsInChildren)
            return;

        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            buttons[i].onClick.RemoveListener(OnClickSlot);
            buttons[i].onClick.AddListener(OnClickSlot);
        }
    }

    public bool CanAddCommand()
    {
        return RemainingCommandCapacity > 0;
    }

    public bool CanAddCommand(PlayerReservedCommand command)
    {
        return command != null && command.UserRuntime != null &&
               (command.IsFacingFlipCommand || command.TimelineEntryCost > 0) &&
               RemainingCommandCapacity >= command.TimelineEntryCost;
    }

    public bool AddCommand(PlayerReservedCommand command)
    {
        if (command == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SkillReservationMissing));
            return false;
        }

        if (!CanAddCommand(command))
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SlotActionLimit));
            return false;
        }

        commands.Add(command);
        return true;
    }

    public bool RemoveCommandAt(int index, out PlayerReservedCommand removedCommand)
    {
        removedCommand = null;

        if (index < 0 || index >= commands.Count)
            return false;

        removedCommand = commands[index];
        commands.RemoveAt(index);

        return true;
    }

    public bool InsertCommand(int index, PlayerReservedCommand command)
    {
        if (command == null || !CanAddCommand(command))
            return false;

        commands.Insert(Mathf.Clamp(index, 0, commands.Count), command);
        return true;
    }

    public int IndexOfCommand(PlayerReservedCommand command)
    {
        return command != null ? commands.IndexOf(command) : -1;
    }

    public void Clear()
    {
        commands.Clear();
    }

    public void SetActiveSlot(bool active)
    {
        // 선택 표시는 BattleTimelineGroupUI에서 처리.
    }

    public void OnClickSlot()
    {
        if (BattleBossCinematicInputGate.IsBlocked) return;
        EnsureOwner();

        if (owner != null)
            owner.OnTimelineSlotClicked(slotIndex);
        else
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.TimelineControllerMissing));
            Debug.LogWarning("[ReserveTurnSlotUI] owner가 없습니다.");
        }
    }

    private void EnsureOwner()
    {
        if (owner != null)
            return;

        BattleTimelineBarUI barUI = GetComponentInParent<BattleTimelineBarUI>(true);

        if (barUI != null && barUI.TryGetOwner(out BattleTimelineController foundOwner))
        {
            owner = foundOwner;
            return;
        }

        owner = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    private void ResolveTurnMark()
    {
        if (turnMarkTransform != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == "TurnMark")
            {
                turnMarkTransform = children[i];
                return;
            }
        }
    }

    private void SetTurnMarkHovered(bool hovered)
    {
        ResolveTurnMark();
        if (turnMarkTransform == null)
            return;

        Vector2 scale = hovered ? turnMarkHoverScale : turnMarkNormalScale;
        Vector3 current = turnMarkTransform.localScale;
        turnMarkTransform.localScale = new Vector3(scale.x, scale.y, current.z);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (BattleBossCinematicInputGate.IsBlocked) return;
        SetTurnMarkHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetTurnMarkHovered(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        OnClickSlot();
    }
}
