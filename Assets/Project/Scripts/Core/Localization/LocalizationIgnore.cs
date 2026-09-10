using UnityEngine;

/// <summary>Marks a TMP as dynamic output. It remains localizable through presenter code, but not static binding tools.</summary>
[DisallowMultipleComponent]
public sealed class LocalizationIgnore : MonoBehaviour
{
}
