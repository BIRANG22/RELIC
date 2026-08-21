using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 로비의 Character1 / Character2 / Character3 이미지 오브젝트에 직접 붙여서 사용합니다.
/// 클릭한 파티 슬롯에 현재 배치된 캐릭터로 CharacterSettingPanel을 엽니다.
/// </summary>
[DisallowMultipleComponent]
public class LobbyPartyCharacterSettingOpenButton : MonoBehaviour, IPointerClickHandler
{
    public enum PartySlot
    {
        Auto = 0,
        Character1 = 1,
        Character2 = 2,
        Character3 = 3
    }

    [Header("Party Slot")]
    [Tooltip("Auto이면 이 오브젝트 또는 부모의 이름(Character1~3)으로 슬롯을 자동 판별합니다.")]
    [SerializeField] private PartySlot partySlot = PartySlot.Auto;

    [Header("Optional References")]
    [Tooltip("비워 두면 씬의 Setting 컴포넌트를 자동으로 찾습니다.")]
    [SerializeField] private Setting setting;

    [Tooltip("비워 두면 기존 Setting 버튼의 LobbyPanelTransitionButton을 자동으로 찾습니다.")]
    [SerializeField] private LobbyPanelTransitionButton transitionButton;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        Open();
    }

    /// <summary>
    /// Unity Button OnClick에서도 직접 호출할 수 있습니다.
    /// </summary>
    public void Open()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        int partyIndex = ResolvePartyIndex();
        if (partyIndex < 0)
        {
            Debug.LogWarning(
                "[LobbyPartyCharacterSettingOpenButton] 파티 슬롯을 판별할 수 없습니다. " +
                "Character1~3 오브젝트에 붙이거나 Party Slot을 직접 지정해주세요.",
                this);
            return;
        }

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.PartyRuntimeStore == null)
        {
            Debug.LogWarning(
                "[LobbyPartyCharacterSettingOpenButton] 파티 데이터를 찾을 수 없습니다.",
                this);
            return;
        }

        string characterId = dataManager.PartyRuntimeStore.GetCharacterId(partyIndex);
        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning(
                $"[LobbyPartyCharacterSettingOpenButton] Character{partyIndex + 1} 슬롯이 비어 있습니다.",
                this);
            return;
        }

        ResolveReferences();

        if (setting == null)
        {
            Debug.LogWarning(
                "[LobbyPartyCharacterSettingOpenButton] Setting 컴포넌트를 찾을 수 없습니다.",
                this);
            return;
        }

        if (transitionButton == null)
        {
            Debug.LogWarning(
                "[LobbyPartyCharacterSettingOpenButton] 기존 Setting 버튼의 LobbyPanelTransitionButton을 찾을 수 없습니다.",
                this);
            return;
        }

        // CharacterSettingPanel이 아직 비활성 상태이므로 먼저 슬롯 선택을 예약합니다.
        // 패널이 실제로 활성화되고 초기화가 끝난 뒤 Setting이 해당 캐릭터를 적용합니다.
        setting.OpenPartySettingWhenActive(partyIndex);

        // 기존 Setting 버튼과 같은 패널/배경/카메라 전환을 시작합니다.
        transitionButton.Execute();
    }

    private int ResolvePartyIndex()
    {
        switch (partySlot)
        {
            case PartySlot.Character1:
                return 0;
            case PartySlot.Character2:
                return 1;
            case PartySlot.Character3:
                return 2;
        }

        Transform current = transform;
        while (current != null)
        {
            if (TryParseCharacterObjectName(current.name, out int index))
                return index;

            current = current.parent;
        }

        return -1;
    }

    private static bool TryParseCharacterObjectName(string objectName, out int partyIndex)
    {
        partyIndex = -1;

        if (string.Equals(objectName, "Character1", System.StringComparison.OrdinalIgnoreCase))
        {
            partyIndex = 0;
            return true;
        }

        if (string.Equals(objectName, "Character2", System.StringComparison.OrdinalIgnoreCase))
        {
            partyIndex = 1;
            return true;
        }

        if (string.Equals(objectName, "Character3", System.StringComparison.OrdinalIgnoreCase))
        {
            partyIndex = 2;
            return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (setting == null)
            setting = FindFirstObjectByType<Setting>(FindObjectsInactive.Include);

        if (transitionButton != null)
            return;

        LobbyPanelTransitionButton[] transitionButtons =
            FindObjectsByType<LobbyPanelTransitionButton>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < transitionButtons.Length; i++)
        {
            LobbyPanelTransitionButton candidate = transitionButtons[i];
            if (candidate == null)
                continue;

            if (string.Equals(candidate.gameObject.name, "Setting", System.StringComparison.OrdinalIgnoreCase))
            {
                transitionButton = candidate;
                return;
            }
        }
    }
}
