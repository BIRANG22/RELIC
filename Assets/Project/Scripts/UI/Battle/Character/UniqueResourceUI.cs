using UnityEngine;
using System.Collections.Generic;

public class UniqueResourceUI : MonoBehaviour
{
    [SerializeField] private List<SpriteRenderer> dots;

    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.black;

    public void Refresh(int current)
    {
        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i < current
                ? activeColor
                : inactiveColor;
        }
    }
}