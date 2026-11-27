using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>명시적으로 연결된 씬 생성 지점을 초기화하고 캐릭터 설정창을 엽니다.</summary>
[DisallowMultipleComponent]
public sealed class LobbyWorldCharacterSettingOpenObject : MonoBehaviour
{
    [Header("Scene Spawn Points")]
    [SerializeField] private List<LobbyCharacterSpawnPoint> spawnPoints = new();

    [Header("Optional Reference")]
    [SerializeField] private Setting setting;

    private Coroutine selectRoutine;

    private void Awake()
    {
        ResolveSettingIfNeeded();
        if (spawnPoints == null)
            return;
        foreach (LobbyCharacterSpawnPoint point in spawnPoints)
            if (point != null)
                point.Initialize(this);
    }

    public void OpenCharacter(string characterId)
    {
        if (UIPanelButton.IsMenuPanelOpen || UIHoverLight2DFalloff.IsPointerBlockedByUI())
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
/// 생성 지점이 런타임에 프리팹 Collider2D에 연결하는 클릭 전달용 컴포넌트입니다.
/// Inspector에서 직접 추가할 필요가 없습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyWorldCharacterClickRelay : MonoBehaviour
{
    private LobbyWorldCharacterSettingOpenObject owner;
    private string characterId;

    public string CharacterId => characterId;

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
