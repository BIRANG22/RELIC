using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleUIBlurRootCollector
{
    private const string MenuRootName = "MenuRoot";
    private const string MenuPanelName = "MenuPanel";

    public static void ConfigureForPanel(GameObject panelRoot)
    {
        if (panelRoot == null)
            return;

        UIBlurBackground[] blurBackgrounds =
            panelRoot.GetComponentsInChildren<UIBlurBackground>(true);
        if (blurBackgrounds == null || blurBackgrounds.Length == 0)
            return;

        List<GameObject> roots = Collect(panelRoot.transform);
        for (int i = 0; i < blurBackgrounds.Length; i++)
        {
            UIBlurBackground blurBackground = blurBackgrounds[i];
            if (blurBackground == null)
                continue;

            blurBackground.SetRuntimeBlurredUiRoots(roots);
        }
    }

    public static void ConfigureForMenuPanel(GameObject menuPanel)
    {
        UIBlurBackground blurBackground = UIBlurBackground.EnsureForPanel(menuPanel);
        if (blurBackground == null)
            return;

        blurBackground.SetRuntimeBlurredUiRoots(Collect(menuPanel.transform));
    }

    private static List<GameObject> Collect(Transform ownerTransform)
    {
        List<GameObject> roots = new();

        AddExplicitBlurRoots(roots, ownerTransform);
        AddActivePanelRoots<BattleRewardPanelUI>(roots, ownerTransform);
        AddActivePanelRoots<BattleRewardEquipPanelUI>(roots, ownerTransform);

        return roots;
    }

    private static void AddExplicitBlurRoots(List<GameObject> roots, Transform ownerTransform)
    {
        UIBlurInclude[] includes = UnityEngine.Object.FindObjectsByType<UIBlurInclude>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < includes.Length; i++)
        {
            UIBlurInclude include = includes[i];
            if (include == null || !include.gameObject.activeInHierarchy)
                continue;

            AddUniqueRoot(roots, include.gameObject, ownerTransform);
        }
    }

    private static void AddActivePanelRoots<T>(List<GameObject> roots, Transform ownerTransform)
        where T : Component
    {
        T[] panels = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            T panel = panels[i];
            if (panel == null || !panel.gameObject.activeInHierarchy)
                continue;

            AddUniqueRoot(roots, panel.gameObject, ownerTransform);
        }
    }

    private static void AddUniqueRoot(List<GameObject> roots, GameObject root, Transform ownerTransform)
    {
        if (roots == null || root == null)
            return;

        Transform rootTransform = root.transform;
        if (IsSelfOrChildOf(ownerTransform, rootTransform))
            return;

        if (IsMenuRootOrChild(rootTransform) || string.Equals(root.name, MenuPanelName, StringComparison.Ordinal))
            return;

        for (int i = roots.Count - 1; i >= 0; i--)
        {
            GameObject existing = roots[i];
            if (existing == null)
            {
                roots.RemoveAt(i);
                continue;
            }

            Transform existingTransform = existing.transform;
            if (existingTransform == rootTransform)
                return;

            if (existingTransform.IsChildOf(rootTransform))
                roots.RemoveAt(i);
            else if (rootTransform.IsChildOf(existingTransform))
                return;
        }

        roots.Add(root);
    }

    private static bool IsSelfOrChildOf(Transform ownerTransform, Transform target)
    {
        return ownerTransform != null &&
               target != null &&
               (target == ownerTransform || target.IsChildOf(ownerTransform));
    }

    private static bool IsMenuRootOrChild(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.name, MenuRootName, StringComparison.Ordinal))
                return true;

            current = current.parent;
        }

        return false;
    }
}
