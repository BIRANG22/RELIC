using UnityEngine;

public enum LobbyBackgroundState
{
    Position,
    CharacterSetting,
    ErosionSelect,
    RelicShop,
    CultureTank
}

[DisallowMultipleComponent]
public sealed class LobbyBackgroundStateController : MonoBehaviour
{
    [Header("Backgrounds")]
    [SerializeField] private GameObject positionBackground;
    [SerializeField] private GameObject characterSettingBackground;
    [SerializeField] private GameObject erosionSelectBackground;
    [SerializeField] private GameObject relicShopBackground;
    [SerializeField] private GameObject cultureTankBackground;

    [Header("Position Panel")]
    [SerializeField] private GameObject positionCharacterSetting;

    [Header("Auto Binding")]
    [SerializeField] private bool autoFindSceneReferences = true;

    public LobbyBackgroundState CurrentState { get; private set; } = LobbyBackgroundState.Position;

    private void Awake()
    {
        ResolveSceneReferences();
        ShowBackground(CurrentState);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveSceneReferences();
    }
#endif

    public void ShowBackground(LobbyBackgroundState state)
    {
        CurrentState = state;
        ResolveSceneReferences();

        SetActive(positionBackground, state == LobbyBackgroundState.Position);
        SetActive(characterSettingBackground, state == LobbyBackgroundState.CharacterSetting);
        SetActive(erosionSelectBackground, state == LobbyBackgroundState.ErosionSelect);
        SetActive(relicShopBackground, state == LobbyBackgroundState.RelicShop);
        SetActive(cultureTankBackground, state == LobbyBackgroundState.CultureTank);
        SetActive(positionCharacterSetting, state == LobbyBackgroundState.Position);
    }

    private void ResolveSceneReferences()
    {
        if (!autoFindSceneReferences)
            return;

        if (positionBackground == null)
            positionBackground = FindSceneObject("Position_Back");

        if (characterSettingBackground == null)
            characterSettingBackground = FindSceneObject("CharacterSetting_Back");

        if (erosionSelectBackground == null)
            erosionSelectBackground = FindSceneObject("ErosionSelect_Back");

        if (relicShopBackground == null)
            relicShopBackground = FindSceneObject("RelicShop_Back");

        if (cultureTankBackground == null)
            cultureTankBackground = FindSceneObject("CultureTank_Back");

        if (positionCharacterSetting == null)
            positionCharacterSetting = FindPositionCharacterSetting();
    }

    private static GameObject FindPositionCharacterSetting()
    {
        GameObject positionPanel = FindSceneObject("PositionPanel");
        if (positionPanel == null)
            return null;

        Transform found = positionPanel.transform.Find("SettingButton/CharacterSetting");
        if (found == null)
            found = positionPanel.transform.Find("CharacterSetting");

        return found != null ? found.gameObject : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject[] objects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate != null && candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
}
