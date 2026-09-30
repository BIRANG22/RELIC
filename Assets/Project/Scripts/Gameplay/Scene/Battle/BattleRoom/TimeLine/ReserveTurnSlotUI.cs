using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReserveTurnSlotUI : MonoBehaviour, IPointerClickHandler
{
    public const int MaxCommandCount = 5;

    [Header("Click")]
    [SerializeField] private bool autoBindButtonsInChildren = true;

    [Header("Slot Number Text")]
    [Tooltip("TurnMark/Key/KeyText에 표시할 슬롯 번호 텍스트입니다. 비어 있으면 KeyText를 자동 탐색합니다.")]
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private bool autoFindKeyText = true;

    private readonly List<PlayerReservedCommand> commands = new();

    private BattleTimelineController owner;
    private int slotIndex;

    public int SlotIndex => slotIndex;
    public IReadOnlyList<PlayerReservedCommand> Commands => commands;
    public int CommandCount => commands.Count;
    public int RemainingCommandCapacity => Mathf.Max(0, MaxCommandCount - commands.Count);

    public CharacterRuntimeData ReservedCharacter
    {
        get
        {
            if (commands == null || commands.Count <= 0)
                return null;

            if (commands[0] == null)
                return null;

            return commands[0].UserRuntime;
        }
    }

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
        RefreshKeyText();
        BindButtons();
    }

    private void Awake()
    {
        ResolveKeyText();
        BindButtons();
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

    private void RefreshKeyText()
    {
        ResolveKeyText();

        if (keyText != null)
            keyText.text = (slotIndex + 1).ToString();
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

    public bool AddCommand(PlayerReservedCommand command)
    {
        if (command == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SkillReservationMissing));
            return false;
        }

        if (!CanAcceptCharacter(command.UserRuntime))
        {
            string reservedId = ReservedCharacter != null ? ReservedCharacter.CharacterId : "None";
            string newId = command.UserRuntime != null ? command.UserRuntime.CharacterId : "None";

            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SlotOccupiedByOtherCharacter));
            Debug.LogWarning($"[ReserveTurnSlotUI] 이 슬롯은 이미 다른 캐릭터가 사용 중입니다. Reserved:{reservedId} / New:{newId}");
            return false;
        }

        if (!CanAddCommand())
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

    public void Clear()
    {
        commands.Clear();
    }

    public void SetActiveSlot(bool active)
    {
        // 선택 표시는 BattleTimelineGroupUI에서 처리.
    }

    public bool CanAcceptCharacter(CharacterRuntimeData character)
    {
        if (character == null)
            return false;

        CharacterRuntimeData reservedCharacter = ReservedCharacter;

        if (reservedCharacter == null)
            return true;

        return reservedCharacter.CharacterId == character.CharacterId;
    }

    public void OnClickSlot()
    {
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

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickSlot();
    }
}
