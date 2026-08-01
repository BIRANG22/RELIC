using System;
using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LobbyRelicOfferButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField, Min(1f)] private float hoverIconScale = 1.12f;

    [Header("Rarity Ring")]
    [SerializeField] private GameObject rarityRingRoot;
    [SerializeField] private ParticleSystem rarityParticles;

    [Header("Rarity Particle Colors")]
    [Tooltip("?쇰컲 ?깃툒 ?뚰떚???됱긽")]
    [SerializeField]
    private Color commonParticleColor =
        new Color32(200, 208, 217, 255);

    [Tooltip("怨좉툒 ?깃툒 ?뚰떚???됱긽")]
    [SerializeField]
    private Color uncommonParticleColor =
        new Color32(92, 219, 131, 255);

    [Tooltip("?ш? ?깃툒 ?뚰떚???됱긽")]
    [SerializeField]
    private Color rareParticleColor =
        new Color32(78, 141, 255, 255);

    [Tooltip("?좊땲???깃툒 ?뚰떚???됱긽")]
    [SerializeField]
    private Color uniqueParticleColor =
        new Color32(255, 179, 71, 255);

    [Tooltip("?????녿뒗 ?깃툒???ъ슜??湲곕낯 ?됱긽")]
    [SerializeField] private Color defaultParticleColor = Color.white;

    private string relicId;
    private Action<string> purchaseRequested;
    private Action<string, bool> hoverChanged;
    private Vector3 iconOriginalScale = Vector3.one;
    private bool iconScaleCached;
    private bool isHovered;

    private bool clickListenerRegistered;
    private bool missingViewWarningLogged;

    private ParticleSystem[] rarityRingParticleSystems =
        Array.Empty<ParticleSystem>();

    private Coroutine rarityRingHideCoroutine;

    private void Awake()
    {
        EnsureView();
    }

    public void Bind(
        LobbyRelicOffer offer,
        Sprite icon,
        RelicRarity rarity,
        Action<string> callback)
    {
        Bind(offer, icon, rarity, callback, null);
    }

    public void Bind(
        LobbyRelicOffer offer,
        Sprite icon,
        RelicRarity rarity,
        Action<string> callback,
        Action<string, bool> hoverCallback)
    {
        if (!EnsureView())
            return;

        ResetHoverState();
        relicId = offer.RelicId;
        purchaseRequested = callback;
        hoverChanged = hoverCallback;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        priceText.text = offer.Price.ToString();
        button.interactable = true;

        ShowRarityRing(rarity);
    }

    public void ShowSold()
    {
        if (!EnsureView())
            return;

        ResetHoverState();
        priceText.text = "?먮ℓ ?꾨즺";
        button.interactable = false;

        FadeOutRarityRing();
    }

    public void ShowEmpty()
    {
        ResetHoverState();
        HideRarityRingImmediately();

        if (!EnsureView())
            return;

        relicId = null;
        hoverChanged = null;

        iconImage.sprite = null;
        iconImage.enabled = false;

        priceText.text = string.Empty;
        button.interactable = false;
    }

    public void SetInteractable(bool interactable)
    {
        if (EnsureView())
            button.interactable = interactable;
    }

    private bool EnsureView()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
        {
            iconImage =
                transform.Find("RelicIcon")?.GetComponent<Image>();
        }

        if (iconImage != null && !iconScaleCached)
        {
            iconOriginalScale = iconImage.rectTransform.localScale;
            iconScaleCached = true;
        }

        if (priceText == null)
        {
            priceText =
                transform.Find("Price")?.GetComponent<TMP_Text>();
        }

        EnsureRarityRingReferences();

        if (button == null ||
            iconImage == null ||
            priceText == null)
        {
            if (!missingViewWarningLogged)
            {
                Debug.LogWarning(
                    $"[LobbyRelicOfferButtonUI] " +
                    $"Serialized view references are missing on '{name}'.",
                    this);

                missingViewWarningLogged = true;
            }

            return false;
        }

        EnsureClickListener();
        return true;
    }

    private void EnsureClickListener()
    {
        if (button == null || clickListenerRegistered)
            return;

        button.onClick.AddListener(RequestPurchase);
        clickListenerRegistered = true;
    }

    private void RequestPurchase()
    {
        if (button == null ||
            !button.interactable ||
            string.IsNullOrWhiteSpace(relicId))
        {
            return;
        }

        purchaseRequested?.Invoke(relicId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.interactable || string.IsNullOrWhiteSpace(relicId))
            return;

        isHovered = true;
        if (iconImage != null)
            iconImage.rectTransform.localScale = iconOriginalScale * hoverIconScale;

        hoverChanged?.Invoke(relicId, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverState();
    }

    private void ResetHoverState()
    {
        if (iconImage != null && iconScaleCached)
            iconImage.rectTransform.localScale = iconOriginalScale;

        if (isHovered && !string.IsNullOrWhiteSpace(relicId))
            hoverChanged?.Invoke(relicId, false);

        isHovered = false;
    }

    private void EnsureRarityRingReferences()
    {
        if (rarityRingRoot == null)
        {
            rarityRingRoot =
                transform.Find("magic_ring_06")?.gameObject;
        }

        if (rarityParticles == null &&
            rarityRingRoot != null)
        {
            ParticleSystem[] particles =
                rarityRingRoot.GetComponentsInChildren<ParticleSystem>(
                    true);

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null &&
                    particles[i].name == "03")
                {
                    rarityParticles = particles[i];
                    break;
                }
            }
        }

        if (rarityRingRoot != null)
        {
            rarityRingParticleSystems =
                rarityRingRoot.GetComponentsInChildren<ParticleSystem>(
                    true);
        }
    }

    private void ShowRarityRing(RelicRarity rarity)
    {
        EnsureRarityRingReferences();
        CancelRarityRingFade();

        if (rarityRingRoot == null)
            return;

        rarityRingRoot.SetActive(false);

        if (rarityParticles != null)
        {
            ParticleSystem.MainModule main =
                rarityParticles.main;

            main.startColor = GetParticleColor(rarity);
        }

        rarityRingRoot.SetActive(true);
        EnsureRarityRingReferences();

        for (int i = 0;
             i < rarityRingParticleSystems.Length;
             i++)
        {
            ParticleSystem particles =
                rarityRingParticleSystems[i];

            if (particles == null ||
                !particles.gameObject.activeInHierarchy)
            {
                continue;
            }

            particles.Clear(false);
            particles.Play(false);
        }
    }

    /// <summary>
    /// ?몄뒪?숉꽣??吏?뺥븳 ?깃툒蹂??됱긽??諛섑솚?⑸땲??
    /// </summary>
    private Color GetParticleColor(RelicRarity rarity)
    {
        switch (rarity)
        {
            case RelicRarity.Common:
                return commonParticleColor;

            case RelicRarity.Uncommon:
                return uncommonParticleColor;

            case RelicRarity.Rare:
                return rareParticleColor;

            case RelicRarity.Unique:
                return uniqueParticleColor;

            default:
                return defaultParticleColor;
        }
    }

    private void FadeOutRarityRing()
    {
        EnsureRarityRingReferences();
        CancelRarityRingFade();

        if (rarityRingRoot == null ||
            !rarityRingRoot.activeSelf)
        {
            return;
        }

        bool stoppedParticles = false;

        for (int i = 0;
             i < rarityRingParticleSystems.Length;
             i++)
        {
            ParticleSystem particles =
                rarityRingParticleSystems[i];

            if (particles == null ||
                !particles.gameObject.activeInHierarchy)
            {
                continue;
            }

            particles.Stop(
                false,
                ParticleSystemStopBehavior.StopEmitting);

            stoppedParticles = true;
        }

        if (!stoppedParticles)
        {
            rarityRingRoot.SetActive(false);
            return;
        }

        rarityRingHideCoroutine =
            StartCoroutine(HideRarityRingWhenFinished());
    }

    private IEnumerator HideRarityRingWhenFinished()
    {
        while (IsRarityRingAlive())
            yield return null;

        rarityRingHideCoroutine = null;

        if (rarityRingRoot != null)
            rarityRingRoot.SetActive(false);
    }

    private bool IsRarityRingAlive()
    {
        for (int i = 0;
             i < rarityRingParticleSystems.Length;
             i++)
        {
            ParticleSystem particles =
                rarityRingParticleSystems[i];

            if (particles != null &&
                particles.gameObject.activeInHierarchy &&
                particles.IsAlive(false))
            {
                return true;
            }
        }

        return false;
    }

    private void HideRarityRingImmediately()
    {
        CancelRarityRingFade();
        EnsureRarityRingReferences();

        if (rarityRingRoot != null)
            rarityRingRoot.SetActive(false);
    }

    private void CancelRarityRingFade()
    {
        if (rarityRingHideCoroutine == null)
            return;

        StopCoroutine(rarityRingHideCoroutine);
        rarityRingHideCoroutine = null;
    }

    private void OnDestroy()
    {
        if (button != null && clickListenerRegistered)
        {
            button.onClick.RemoveListener(RequestPurchase);
            clickListenerRegistered = false;
        }
    }

    private void OnDisable()
    {
        ResetHoverState();
    }
}
