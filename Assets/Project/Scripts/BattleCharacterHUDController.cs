using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배틀씬의 캐릭터 월드 HUD를 관리합니다.
/// PlayerHUD_Root 아래에 CharacterHUDSlot을 캐릭터별로 생성하고,
/// 캐릭터를 호버하거나 클릭 선택한 동안만 해당 HUD를 표시합니다.
/// </summary>
public class BattleCharacterHUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerHudRoot;
    [SerializeField] private CharacterHUDSlot characterHudPrefab;
    [SerializeField] private BattleTimelineController timelineController;

    [Header("View")]
    [SerializeField] private float hudScale = 0.4f;

    [Header("Refresh")]
    [SerializeField, Min(0.05f)] private float characterScanInterval = 0.25f;

    private sealed class CharacterHudBinding
    {
        public BattleCharacter Character;
        public CharacterHUDSlot Hud;
        public Collider2D Collider;
    }

    private readonly List<CharacterHudBinding> bindings = new();
    private float nextCharacterScanTime;

    private void Awake()
    {
        if (playerHudRoot == null)
            playerHudRoot = transform;

        EnsureTimelineController();
    }

    private void OnEnable()
    {
        nextCharacterScanTime = 0f;
        RefreshCharacterBindings();
        RefreshVisibility();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextCharacterScanTime)
        {
            nextCharacterScanTime = Time.unscaledTime + Mathf.Max(0.05f, characterScanInterval);
            RefreshCharacterBindings();
        }

        RefreshVisibility();
    }

    private void OnDisable()
    {
        HideAll();
    }

    public void RefreshNow()
    {
        RefreshCharacterBindings();
        RefreshVisibility();
    }

    private void RefreshCharacterBindings()
    {
        if (characterHudPrefab == null || playerHudRoot == null)
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

        CharacterHUDSlot hud = Instantiate(characterHudPrefab, playerHudRoot);
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
            Collider = characterCollider
        });
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

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        CharacterRuntimeData selectedRuntime = GetSelectedCharacterRuntime();
        bool monsterInfoSelected = MonsterUnit.CurrentInfoSelectedMonster != null;

        for (int i = 0; i < bindings.Count; i++)
        {
            CharacterHudBinding binding = bindings[i];

            if (binding == null || binding.Character == null || binding.Hud == null)
                continue;

            CharacterRuntimeData runtime = binding.Character.RuntimeData;
            if (runtime == null || runtime.IsDead)
            {
                binding.Hud.Hide();
                continue;
            }

            if (binding.Collider == null)
                binding.Collider = binding.Character.GetComponentInChildren<Collider2D>();

            bool hovered = IsHovered(binding.Collider, mouseWorldPosition);
            bool selected = !monsterInfoSelected && IsSameCharacter(runtime, selectedRuntime);

            if (hovered || selected)
            {
                binding.Hud.Bind(runtime);
                binding.Hud.SetFollowTarget(binding.Character.transform, binding.Collider);
                binding.Hud.Show();
            }
            else
            {
                binding.Hud.Hide();
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

    private void HideAll()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i] != null && bindings[i].Hud != null)
                bindings[i].Hud.Hide();
        }
    }
}
