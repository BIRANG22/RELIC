using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReserveTurnSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Orders")]
    [SerializeField] private GameObject[] orderObjects;

    [Header("Click")]
    [SerializeField] private bool autoBindButtonsInChildren = true;

    private readonly List<BattleReservedCommand> commands = new();

    private BattleTimelineController owner;
    private int slotIndex;

    public int SlotIndex => slotIndex;
    public IReadOnlyList<BattleReservedCommand> Commands => commands;

    public void Init(BattleTimelineController owner, int slotIndex)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;

        AutoFindOrdersIfNeeded();
        BindButtons();
        RefreshOrderObjects();
    }

    private void Awake()
    {
        AutoFindOrdersIfNeeded();
        BindButtons();
        RefreshOrderObjects();
    }

    private void AutoFindOrdersIfNeeded()
    {
        if (orderObjects != null && orderObjects.Length > 0)
            return;

        List<GameObject> found = new();

        for (int i = 1; i <= 5; i++)
        {
            Transform order = transform.Find("Order" + i.ToString("00"));

            if (order != null)
                found.Add(order.gameObject);
        }

        orderObjects = found.ToArray();
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
        return commands.Count < orderObjects.Length;
    }

    public bool AddCommand(BattleReservedCommand command)
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
        RefreshOrderObjects();

        Debug.Log($"[ReserveTurnSlotUI] AddCommand / Slot:{slotIndex} / Character:{command.CharacterId} / Skill:{command.SkillName}");
        return true;
    }

    public bool RemoveCommandAt(int index, out BattleReservedCommand removedCommand)
    {
        removedCommand = null;

        if (index < 0 || index >= commands.Count)
            return false;

        removedCommand = commands[index];
        commands.RemoveAt(index);

        RefreshOrderObjects();

        Debug.Log($"[ReserveTurnSlotUI] RemoveCommand / Slot:{slotIndex} / Index:{index}");

        return true;
    }

    public void Clear()
    {
        commands.Clear();
        RefreshOrderObjects();
    }

    private void RefreshOrderObjects()
    {
        AutoFindOrdersIfNeeded();

        if (orderObjects == null)
            return;

        for (int i = 0; i < orderObjects.Length; i++)
        {
            if (orderObjects[i] != null)
                orderObjects[i].SetActive(i < commands.Count);
        }
    }

    public void SetActiveSlot(bool active)
    {
        // 슬롯 선택 표시는 BattleTimelineGroupUI의 스케일 효과에서 처리.
        // 여기서는 TurnMark를 끄지 않음.
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

    public bool CanAcceptCharacter(CharacterRuntimeData character)
    {
        if (character == null)
            return false;

        CharacterRuntimeData reservedCharacter = ReservedCharacter;

        if (reservedCharacter == null)
            return true;

        return reservedCharacter.CharacterId == character.CharacterId;
    }
}