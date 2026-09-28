using UnityEngine;

/// <summary>
/// Marks text whose presenter already resolves the final localized value.
/// Runtime source matching must not attach or replay another localization writer.
/// </summary>
[DisallowMultipleComponent]
public sealed class LocalizationAutoBindingIgnore : MonoBehaviour
{
}
