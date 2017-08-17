using UnityEngine;
using System.Collections;
using UnityEditor;
using A1050055001a;

[CustomEditor(typeof(Configuration))]
public class ConfigurationInspector : Editor
{
    private bool showImericoParameters = true;
    private bool showBeaconParameters = true;

    public override void OnInspectorGUI()
    {
        // Styles
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontStyle = FontStyle.Bold;

        Configuration c = (Configuration)target;

        EditorGUILayout.LabelField("Client Keys", EditorStyles.boldLabel);

        c.ClientAccessKey = EditorGUILayout.TextField("Access Key:", c.ClientAccessKey);
        c.ClientSecretKey = EditorGUILayout.PasswordField("Secret Key:", c.ClientSecretKey);

        if (string.IsNullOrEmpty(c.ClientAccessKey) || string.IsNullOrEmpty(c.ClientSecretKey))
            EditorGUILayout.HelpBox("Client keys are available on the dashboard in the technicians management section (only for contractor).", MessageType.Info, true);

        EditorGUILayout.Space();

        // Imerico
        showImericoParameters = EditorGUILayout.Foldout(showImericoParameters, "Imerico", foldoutStyle);
        if (showImericoParameters)
        {
            c.CategoryId = EditorGUILayout.IntField("Category ID:", c.CategoryId);
            c.BufferSize = EditorGUILayout.IntSlider("Buffer Size:", c.BufferSize, 1, 20);
        }

        // Beacon
        showBeaconParameters = EditorGUILayout.Foldout(showBeaconParameters, "Beacon", foldoutStyle);
        if (showBeaconParameters)
        {
            c.BeaconsUUID = EditorGUILayout.TextField("UUID:", c.BeaconsUUID);

            if (string.IsNullOrEmpty(c.BeaconsUUID))
                EditorGUILayout.HelpBox("UUID available on the dashboard in the beacon devices management section.", MessageType.Info, true);
            else
            {
                try
                {
                    new System.Guid(c.BeaconsUUID);
                } catch (System.Exception)
                {
                    EditorGUILayout.HelpBox("Not a valid UUID.", MessageType.Error, true);
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Dashboard"))
        {
            Application.OpenURL("http://plus.xloudia.net");
        }

        if (GUILayout.Button("Support"))
        {
            Application.OpenURL("http://support.xloudia.com");
        }

        EditorGUILayout.EndHorizontal();
    }


}
