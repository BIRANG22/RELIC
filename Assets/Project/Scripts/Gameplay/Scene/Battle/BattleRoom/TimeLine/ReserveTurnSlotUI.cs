using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReserveTurnSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Click")]
    [SerializeField] private bool autoBindButtonsInChildren = true;

    private readonly List<PlayerReservedCommand> commands = new();

    private BattleTimelineController owner;
    private int slotIndex;

    public int SlotIndex => slotIndex;
    public IReadOnlyList<PlayerReservedCommand> Commands => commands;

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
        return commands.Count < 5;
    }

    public bool AddCommand(PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        if (!CanAcceptCharacter(command.UserRuntime))
        {
            string reservedId = ReservedCharacter != null ? ReservedCharacter.CharacterId : "None";
            string newId = command.UserRuntime != null ? command.UserRuntime.CharacterId : "None";

            Debug.LogWarning($"[ReserveTurnSlotUI] 이 슬롯은 이미 다른 캐릭터가 사용 중입니다. Reserved:{reservedId} / New:{newId}");
            return false;
        }

        if (!CanAddCommand())
            return false;

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
        Debug.Log($"[ReserveTurnSlotUI] Click Slot:{slotIndex}");

        if (owner != null)
            owner.OnTimelineSlotClicked(slotIndex);
        else
            Debug.LogWarning("[ReserveTurnSlotUI] owner가 없습니다.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickSlot();
    }
}