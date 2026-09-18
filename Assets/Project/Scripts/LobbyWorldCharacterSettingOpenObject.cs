using System.Collections;
using UnityEngine;

/// <summary>
/// Lobby > Character 부모에 하나만 붙여 사용합니다.
/// 자식의 Cha_01_idle_0 ~ Cha_04_idle_0 월드 오브젝트를 자동으로 찾아
/// 클릭한 캐릭터를 CharacterSettingPanel에서 선택된 상태로 엽니다.
///
/// 각 캐릭터 쪽에는 클릭 판정을 위한 Collider2D만 있으면 됩니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyWorldCharacterSettingOpenObject : MonoBehaviour
{
    [Header("Optional Reference")]
    [Tooltip("비워두면 씬의 CharacterSettingPanel에 붙은 Setting을 자동으로 찾습니다.")]
    [SerializeField] private Setting setting;

    [Header("Auto Binding")]
    [Tooltip("활성화하면 자식 Collider2D를 자동으로 찾아 클릭 연결을 구성합니다.")]
    [SerializeField] private bool autoBindChildren = true;

    private Coroutine selectRoutine;

    private void Awake()
    {
        ResolveSettingIfNeeded();

        if (autoBindChildren)
            BindCharacterChildren();
    }

    private void OnEnable()
    {
        ResolveSettingIfNeeded();

        if (autoBindChildren)
            BindCharacterChildren();
    }

    /// <summary>
    /// Character 부모 아래의 Collider2D를 찾아 캐릭터 클릭 릴레이를 자동 등록합니다.
    /// Cha_01_idle_0 같은 캐릭터 루트 이름은 Char_01로 자동 변환합니다.
    /// </summary>
    public void BindCharacterChildren()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D targetCollider = colliders[i];
            if (targetCollider == null)
                continue;

            string characterId = ResolveCharacterIdFromHierarchy(targetCollider.transform);
            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            LobbyWorldCharacterClickRelay relay = targetCollider.GetComponent<LobbyWorldCharacterClickRelay>();
            if (relay == null)
                relay = targetCollider.gameObject.AddComponent<LobbyWorldCharacterClickRelay>();

            relay.Bind(this, characterId);
        }
    }

    /// <summary>
    /// 자동 등록된 자식 클릭 릴레이에서 호출합니다.
    /// </summary>
    public void OpenCharacter(string characterId)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        // 다른 전면 모달이 열린 상태에서 뒤쪽 캐릭터가 눌리는 것을 막습니다.
        if (LobbyPositionModalInputBlocker.IsBlocked)
            return;

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        ResolveSettingIfNeeded();

        if (setting == null)
        {
            Debug.LogWarning(
                "[LobbyWorldCharacterSettingOpenObject] CharacterSettingPanel의 Setting을 찾을 수 없습니다.",
                this);
            return;
        }

        GameObject characterSettingPanel = setting.gameObject;

        // 이미 CharacterSettingPanel이 열려 있다면 패널을 다시 열지 않고 캐릭터만 변경합니다.
        if (characterSettingPanel.activeInHierarchy)
        {
            ApplyCharacterSelection(characterId);
            return;
        }

        // 다른 PositionPanel 모달이 열려 있다면 기존 공용 패널 전환 규칙으로 정리합니다.
        if (!LobbyPositionSharedModalBackground.PrepareForPanelSwitch(characterSettingPanel))
            return;

        TitleManager.CloseTitleModePanelsExceptInScene(characterSettingPanel);
        characterSettingPanel.SetActive(true);

        if (selectRoutine != null)
            StopCoroutine(selectRoutine);

        // Setting.OnEnable 및 CharacterSettingPanel 내부 초기화가 끝난 다음 캐릭터를 선택합니다.
        selectRoutine = StartCoroutine(ApplyCharacterSelectionNextFrame(characterId));
    }

    private IEnumerator ApplyCharacterSelectionNextFrame(string characterId)
    {
        yield return null;
        selectRoutine = null;
        ApplyCharacterSelection(characterId);
    }

    private void ApplyCharacterSelection(string characterId)
    {
        if (setting == null || !setting.gameObject.activeInHierarchy)
            return;

        CharPick charPick = setting.GetComponentInChildren<CharPick>(true);
        if (charPick != null)
        {
            if (!charPick.SelectViewedCharacterForSetting(characterId))
            {
                Debug.LogWarning(
                    "[LobbyWorldCharacterSettingOpenObject] CharacterSelect에서 캐릭터를 선택할 수 없습니다: " + characterId,
                    this);
                return;
            }
        }

        setting.OpenCharacterSetting(characterId);
    }

    private string ResolveCharacterIdFromHierarchy(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (TryResolveCharacterId(current.name, out string characterId))
                return characterId;

            if (current == transform)
                break;

            current = current.parent;
        }

        return null;
    }

    private static bool TryResolveCharacterId(string objectName, out string characterId)
    {
        characterId = null;

        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        for (int i = 1; i <= 99; i++)
        {
            string number = i.ToString("00");
            if (!objectName.Contains("Cha_" + number))
                continue;

            characterId = "Char_" + number;
            return true;
        }

        return false;
    }

    private void ResolveSettingIfNeeded()
    {
        if (setting != null)
            return;

        Setting[] settings = FindObjectsByType<Setting>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < settings.Length; i++)
        {
            Setting candidate = settings[i];
            if (candidate == null)
                continue;

            if (!string.Equals(
                    candidate.gameObject.name,
                    "CharacterSettingPanel",
                    System.StringComparison.OrdinalIgnoreCase))
                continue;

            setting = candidate;
            return;
        }

        if (settings.Length > 0)
            setting = settings[0];
    }
}

/// <summary>
/// 부모 LobbyWorldCharacterSettingOpenObject가 런타임에 자식 Collider2D에 자동으로 붙이는 클릭 전달용 컴포넌트입니다.
/// Inspector에서 직접 추가할 필요가 없습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyWorldCharacterClickRelay : MonoBehaviour
{
    private LobbyWorldCharacterSettingOpenObject owner;
    private string characterId;

    public void Bind(LobbyWorldCharacterSettingOpenObject targetOwner, string targetCharacterId)
    {
        owner = targetOwner;
        characterId = targetCharacterId;
    }

    private void OnMouseUpAsButton()
    {
        if (owner == null || string.IsNullOrWhiteSpace(characterId))
            return;

        owner.OpenCharacter(characterId);
    }
}
