using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPartyStatusIconPresenter : MonoBehaviour
{
    [SerializeField] private Image[] partyIconImages = new Image[3];

    private readonly string[] cachedCharacterIds = new string[3];
    private bool forceRefresh = true;

    private void Awake()
    {
        AutoBindIconsIfNeeded();
    }

    private void OnEnable()
    {
        forceRefresh = true;
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBindIconsIfNeeded();
    }
#endif

    public void Refresh()
    {
        AutoBindIconsIfNeeded();

        DataManager dataManager = DataManager.Instance;
        PartyRuntimeStore partyStore = dataManager != null ? dataManager.PartyRuntimeStore : null;

        for (int i = 0; i < partyIconImages.Length; i++)
        {
            string characterId = partyStore != null ? partyStore.GetCharacterId(i) : null;
            bool shouldRetryMissingIcon =
                !string.IsNullOrWhiteSpace(characterId) &&
                partyIconImages[i] != null &&
                partyIconImages[i].sprite == null;

            if (!forceRefresh && cachedCharacterIds[i] == characterId && !shouldRetryMissingIcon)
                continue;

            cachedCharacterIds[i] = characterId;
            ApplyIcon(i, characterId, dataManager);
        }

        forceRefresh = false;
    }

    private void ApplyIcon(int slotIndex, string characterId, DataManager dataManager)
    {
        Image image = partyIconImages[slotIndex];

        if (image == null)
            return;

        Sprite icon = ResolveIcon(characterId, dataManager);
        image.sprite = icon;
        image.enabled = icon != null;
        image.preserveAspect = true;

        if (icon != null)
            image.color = Color.white;
    }

    private static Sprite ResolveIcon(string characterId, DataManager dataManager)
    {
        if (string.IsNullOrWhiteSpace(characterId) || dataManager == null)
            return null;

        if (dataManager.CharacterDatabase != null &&
            dataManager.CharacterDatabase.TryGet(characterId, out CharacterMasterData master) &&
            master.Icon != null)
        {
            return master.Icon;
        }

        if (dataManager.CharacterIconDatabase != null &&
            dataManager.CharacterIconDatabase.TryGetIcon(characterId, out Sprite icon))
        {
            return icon;
        }

        return null;
    }

    private void AutoBindIconsIfNeeded()
    {
        if (partyIconImages == null || partyIconImages.Length != 3)
            partyIconImages = new Image[3];

        TryBindIcon(0, "Character1/Icon");
        TryBindIcon(1, "Character2/Icon");
        TryBindIcon(2, "Character3/Icon");
    }

    private void TryBindIcon(int index, string path)
    {
        if (index < 0 || index >= partyIconImages.Length)
            return;

        if (partyIconImages[index] != null)
            return;

        Transform found = transform.Find(path);

        if (found != null)
            partyIconImages[index] = found.GetComponent<Image>();
    }
}
