using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배틀씬의 캐릭터 월드 HUD를 관리합니다.
/// BattleCharacterPanel 바로 아래에 CharacterHUDSlot을 캐릭터별로 생성하고,
/// 캐릭터를 호버하거나 클릭 선택한 동안만 해당 HUD를 표시합니다.
/// </summary>
public class BattleCharacterHUDController : MonoBehaviour
{
    public static BattleCharacterHUDController Instance { get; private set; }
    [Header("References")]
    [SerializeField] private CharacterHUDSlot characterHudPrefab;
    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private BattleTurnExecutor turnExecutor;

    [Header("View")]
    [SerializeField] private float hudScale = 0.4f;
    [SerializeField, Min(0f)] private float hitHudVisibleDuration = 1.5f;

    private sealed class CharacterHudBinding
    {
        public BattleCharacter Character;
        public CharacterHUDSlot Hud;
        public Collider2D Collider;
        public bool IsVisible;
    }

    private readonly List<CharacterHudBinding> bindings = new();
    private readonly Dictionary<string, float> hitHudVisibleUntilByCharacterId = new();
    private string timelineIconHoveredCharacterId = "";

    private void Awake()
    {
        Instance = this;
        EnsureTimelineController();
        EnsureTurnExecutor();
    }

    private void OnEnable()
    {
        Instance = this;
        BattleTurnExecutor.BattleExecutionStarted -= HandleBattleExecutionStarted;
        BattleTurnExecutor.BattleExecutionStarted += HandleBattleExecutionStarted;
        BattleEffectUtility.OnPlayerHudRefreshRequested -= HandlePlayerHudRefreshRequested;
        BattleEffectUtility.OnPlayerHudRefreshRequested += HandlePlayerHudRefreshRequested;
        hitHudVisibleUntilByCharacterId.Clear();
        RefreshCharacterBindings();
        RefreshVisibility();
    }

    private void Update()
    {
        // 캐릭터 목록과 런타임 수치는 매 프레임 재탐색/폴링하지 않습니다.
        // 선택/호버/임시 표시 시간만 갱신하고, 실제 수치 변화는
        // BattleEffectUtility.OnPlayerHudRefreshRequested 이벤트에서 처리합니다.
        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        BattleTurnExecutor.BattleExecutionStarted -= HandleBattleExecutionStarted;
        BattleEffectUtility.OnPlayerHudRefreshRequested -= HandlePlayerHudRefreshRequested;
        hitHudVisibleUntilByCharacterId.Clear();
        timelineIconHoveredCharacterId = "";
        HideAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void HandlePlayerHudRefreshRequested(BattleCharacter character)
    {
        ShowCharacterHudTemporarily(character);
    }

    private void HandleBattleExecutionStarted()
    {
        hitHudVisibleUntilByCharacterId.Clear();
        timelineIconHoveredCharacterId = "";
        HideAll();
    }

    public void ShowCharacterHudForEffect(BattleCharacter character)
    {
        ShowCharacterHudTemporarily(character);
    }

    private void ShowCharacterHudTemporarily(BattleCharacter character)
    {
        CharacterRuntimeData runtime = character != null ? character.RuntimeData : null;
        if (runtime == null || runtime.IsDead || string.IsNullOrWhiteSpace(runtime.CharacterId))
            return;

        hitHudVisibleUntilByCharacterId[runtime.CharacterId] =
            Time.unscaledTime + Mathf.Max(1.5f, hitHudVisibleDuration);

        CharacterHudBinding binding = FindBinding(character);
        if (binding == null)
        {
            CreateBinding(character);
            binding = FindBinding(character);
        }

        if (binding != null && binding.Hud != null)
        {
            if (binding.Collider == null)
                binding.Collider = character.GetComponentInChildren<Collider2D>();

            binding.Hud.Bind(runtime);
            binding.Hud.SetFollowTarget(character.transform, binding.Collider);
            binding.Hud.Show();
            binding.IsVisible = true;
        }
    }

    public void SetTimelineIconHoverCharacter(string characterId, bool hovered)
    {
        if (hovered)
        {
            timelineIconHoveredCharacterId = characterId ?? "";
        }
        else if (string.IsNullOrWhiteSpace(characterId) || timelineIconHoveredCharacterId == characterId)
        {
            timelineIconHoveredCharacterId = "";
        }

        RefreshVisibility();
    }

    public void ShowTimelineIconCharacterHUD(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
            return;

        timelineIconHoveredCharacterId = character.RuntimeData.CharacterId ?? "";

        CharacterHudBinding binding = FindBinding(character);
        if (binding == null)
        {
            CreateBinding(character);
            binding = FindBinding(character);
        }

        if (binding == null || binding.Hud == null)
            return;

        if (binding.Collider == null)
            binding.Collider = character.GetComponentInChildren<Collider2D>();

        binding.Hud.Bind(character.RuntimeData);
        binding.Hud.SetFollowTarget(character.transform, binding.Collider);
        binding.Hud.Show();
        binding.IsVisible = true;
    }

    public void HideTimelineIconCharacterHUD(BattleCharacter character)
    {
        string characterId = character != null && character.RuntimeData != null
            ? character.RuntimeData.CharacterId
            : timelineIconHoveredCharacterId;

        if (string.IsNullOrWhiteSpace(characterId) || timelineIconHoveredCharacterId == characterId)
            timelineIconHoveredCharacterId = "";

        RefreshVisibility();
    }

    public void RefreshNow()
    {
        RefreshCharacterBindings();
        RefreshVisibility();
    }

    private void RefreshCharacterBindings()
    {
        if (characterHudPrefab == null || ResolveHudRoot() == null)
            return;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        RemoveMissingBindings(characters);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (FindBinding(character) != null)
                continue;

            CreateBinding(character);
        }
    }

    private void CreateBinding(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null)
            return;

        Transform hudRoot = ResolveHudRoot();
        if (hudRoot == null)
            return;

        CharacterHUDSlot hud = Instantiate(characterHudPrefab, hudRoot);
        hud.name = characterHudPrefab.name + "(Clone)";

        RectTransform rect = hud.GetComponent<RectTransform>();
        if (rect != null)
            rect.localScale = Vector3.one * Mathf.Max(0f, hudScale);

        Collider2D characterCollider = character.GetComponentInChildren<Collider2D>();

        hud.Bind(character.RuntimeData);
        hud.SetFollowTarget(character.transform, characterCollider);
        hud.Hide();

        bindings.Add(new CharacterHudBinding
        {
            Character = character,
            Hud = hud,
            Collider = characterCollider,
            IsVisible = false
        });
    }


    private Transform ResolveHudRoot()
    {
        BattleCharacterPanelUI parentPanel = GetComponentInParent<BattleCharacterPanelUI>(true);
        if (parentPanel != null)
            return parentPanel.transform;

        BattleCharacterPanelUI scenePanel = Object.FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Include);
        if (scenePanel != null)
            return scenePanel.transform;

        return null;
    }

    private void RemoveMissingBindings(BattleCharacter[] currentCharacters)
    {
        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            CharacterHudBinding binding = bindings[i];

            if (binding == null || binding.Character == null || !ContainsCharacter(currentCharacters, binding.Character))
            {
                if (binding != null && binding.Hud != null)
                    Destroy(binding.Hud.gameObject);

                bindings.RemoveAt(i);
            }
        }
    }

    private static bool ContainsCharacter(BattleCharacter[] characters, BattleCharacter target)
    {
        if (characters == null || target == null)
            return false;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == target)
                return true;
        }

        return false;
    }

    private CharacterHudBinding FindBinding(BattleCharacter character)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            CharacterHudBinding binding = bindings[i];
            if (binding != null && binding.Character == character)
                return binding;
        }

        return null;
    }

    private void RefreshVisibility()
    {
        EnsureTimelineController();
        EnsureTurnExecutor();

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        CharacterRuntimeData selectedRuntime = GetSelectedCharacterRuntime();
        bool monsterInfoSelected = MonsterUnit.CurrentInfoSelectedMonster != null;
        bool canShowSelectedHud = turnExecutor == null || turnExecutor.CanAcceptPlayerInput;

        for (int i = 0; i < bindings.Count; i++)
        {
            CharacterHudBinding binding = bindings[i];

            if (binding == null || binding.Character == null || binding.Hud == null)
                continue;

            CharacterRuntimeData runtime = binding.Character.RuntimeData;
            if (runtime == null || runtime.IsDead)
            {
                binding.Hud.Hide();
                binding.IsVisible = false;
                continue;
            }

            if (binding.Collider == null)
                binding.Collider = binding.Character.GetComponentInChildren<Collider2D>();

            bool hovered = IsHovered(binding.Collider, mouseWorldPosition);
            bool timelineIconHovered =
                !string.IsNullOrWhiteSpace(timelineIconHoveredCharacterId) &&
                runtime.CharacterId == timelineIconHoveredCharacterId;
            bool selected = canShowSelectedHud &&
                            !monsterInfoSelected &&
                            IsSameCharacter(runtime, selectedRuntime);
            bool hitTemporary = IsHitHudVisible(runtime.CharacterId);

            bool shouldShow = hovered || timelineIconHovered || selected || hitTemporary;

            if (shouldShow)
            {
                if (!binding.IsVisible)
                {
                    binding.Hud.Bind(runtime);
                    binding.Hud.SetFollowTarget(binding.Character.transform, binding.Collider);
                    binding.Hud.Show();
                    binding.IsVisible = true;
                }
            }
            else if (binding.IsVisible)
            {
                binding.Hud.Hide();
                binding.IsVisible = false;
            }
        }
    }

    private CharacterRuntimeData GetSelectedCharacterRuntime()
    {
        if (timelineController == null)
            return null;

        return timelineController.SelectedCharacter;
    }

    private static bool IsSameCharacter(CharacterRuntimeData a, CharacterRuntimeData b)
    {
        if (a == null || b == null)
            return false;

        if (ReferenceEquals(a, b))
            return true;

        return !string.IsNullOrWhiteSpace(a.CharacterId) &&
               a.CharacterId == b.CharacterId;
    }

    private static bool IsHovered(Collider2D collider, Vector3 mouseWorldPosition)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            return false;

        return collider.OverlapPoint(mouseWorldPosition);
    }

    private static Vector3 GetMouseWorldPosition()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return Vector3.zero;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(camera.transform.position.z);
        return camera.ScreenToWorldPoint(mousePosition);
    }

    private void EnsureTimelineController()
    {
        if (timelineController != null)
            return;

        timelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private void EnsureTurnExecutor()
    {
        if (turnExecutor != null)
            return;

        turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
    }

    private bool IsHitHudVisible(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) ||
            !hitHudVisibleUntilByCharacterId.TryGetValue(characterId, out float visibleUntil))
        {
            return false;
        }

        if (Time.unscaledTime <= visibleUntil)
            return true;

        hitHudVisibleUntilByCharacterId.Remove(characterId);
        return false;
    }

    private void HideAll()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i] != null && bindings[i].Hud != null)
            {
                bindings[i].Hud.Hide();
                bindings[i].IsVisible = false;
            }
        }
    }
}
