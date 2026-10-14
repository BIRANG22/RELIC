using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LocaleFontRoleAttribute))]
public sealed class LocaleFontRoleDrawer : PropertyDrawer
{
    private const string DefaultDatabasePath = "Assets/Resources/Localization/LocaleFontDatabase.asset";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        LocaleFontDatabase database = ResolveDatabase(property);
        string[] roles = database != null
            ? database.GetRoleIds()
            : new[] { LocaleFontDatabase.BodyRoleId, LocaleFontDatabase.TitleRoleId, LocaleFontDatabase.NumberRoleId };
        int currentIndex = System.Array.IndexOf(roles, property.stringValue);
        int selectedIndex = EditorGUI.Popup(position, label.text, Mathf.Max(0, currentIndex), roles);
        if (roles.Length > 0 && selectedIndex >= 0 && selectedIndex < roles.Length)
            property.stringValue = roles[selectedIndex];
    }

    private static LocaleFontDatabase ResolveDatabase(SerializedProperty property)
    {
        SerializedProperty databaseProperty = property.serializedObject.FindProperty("database");
        if (databaseProperty?.objectReferenceValue is LocaleFontDatabase assigned)
            return assigned;
        return AssetDatabase.LoadAssetAtPath<LocaleFontDatabase>(DefaultDatabasePath);
    }
}
