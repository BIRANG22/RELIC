using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class LobbyWorldCharacterSpawner
{
    public static GameObject Spawn(
        LobbyCharacterSpawnPoint point,
        GameObject prefab,
        LobbyWorldCharacterSettingOpenObject owner)
    {
        if (point == null || prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, point.transform, false);
        instance.name = "LobbyWorld_" + point.CharacterId;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // 기존 로비 프리팹의 앞/뒤 SpriteRenderer 역할을 유지한다.
        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length > 0)
        {
            renderers[0].flipX = point.FlipX;
            renderers[0].sortingLayerID = point.SortingLayerId;
            renderers[0].sortingOrder = point.FrontSortingOrder;
        }
        if (renderers.Length > 1)
        {
            renderers[1].sortingLayerID = point.SortingLayerId;
            renderers[1].sortingOrder = point.BackSortingOrder;
        }

        if (point.OverrideLayer)
        {
            instance.layer = point.ObjectLayer;
            if (point.ApplyLayerToChildren)
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = point.ObjectLayer;
        }

        Collider2D hoverCollider = instance.GetComponent<Collider2D>()
            ?? instance.GetComponentInChildren<Collider2D>(true);
        if (hoverCollider != null)
        {
            // 기존 공유 프리팹의 로비 상호작용을 연결하는 호환 어댑터.
            var hoverName = hoverCollider.GetComponent<LobbyWorldObjectHoverName>();
            if (hoverName == null)
                hoverName = hoverCollider.gameObject.AddComponent<LobbyWorldObjectHoverName>();
            hoverName.Configure(point.HoverNameType);

            Light2D light = instance.GetComponentInChildren<Light2D>(true);
            if (light != null)
            {
                var hoverLight = hoverCollider.GetComponent<UIHoverLight2DFalloff>();
                if (hoverLight == null)
                    hoverLight = hoverCollider.gameObject.AddComponent<UIHoverLight2DFalloff>();
                hoverLight.Configure(light);
            }
        }

        BindClicks(instance, owner, point.CharacterId);
        instance.SetActive(true);
        return instance;
    }

    public static void BindClicks(GameObject instance, LobbyWorldCharacterSettingOpenObject owner, string characterId)
    {
        if (instance == null || owner == null || string.IsNullOrWhiteSpace(characterId))
            return;
        foreach (Collider2D collider in instance.GetComponentsInChildren<Collider2D>(true))
        {
            var relay = collider.GetComponent<LobbyWorldCharacterClickRelay>();
            if (relay == null)
                relay = collider.gameObject.AddComponent<LobbyWorldCharacterClickRelay>();
            relay.Bind(owner, characterId);
        }
    }
}
