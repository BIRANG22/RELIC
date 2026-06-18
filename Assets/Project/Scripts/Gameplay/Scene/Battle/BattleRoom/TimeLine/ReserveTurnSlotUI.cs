using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReserveTurnSlotUI : MonoBehaviour, IPointerClickHandler
{
    public const int MaxCommandCount = 3;

    [Header("Click")]
    [SerializeField] private bool autoBindButtonsInChildren = true;

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

    public void Init(BattleTimelineController owner, int slotIndex)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;

        BindButtons();
    }

    private void Awake()
    {
        BindButtons();
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
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (!CanAcceptCharacter(command.UserRuntime))
        {
            string reservedId = ReservedCharacter != null ? ReservedCharacter.CharacterId : "None";
            string newId = command.UserRuntime != null ? command.UserRuntime.CharacterId : "None";

            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning($"[ReserveTurnSlotUI] 이 슬롯은 이미 다른 캐릭터가 사용 중입니다. Reserved:{reservedId} / New:{newId}");
            return false;
        }

        if (!CanAddCommand())
        {
            ShowBattleWarning("한 슬롯에는 최대 3개의 스킬만 예약할 수 있습니다.");
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
        if (owner != null)
            owner.OnTimelineSlotClicked(slotIndex);
        else
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            Debug.LogWarning("[ReserveTurnSlotUI] owner가 없습니다.");
        }
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
