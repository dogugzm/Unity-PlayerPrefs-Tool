using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PlayerPrefScanner : EditorWindow
{
    private Vector2 scrollPos;
    private Dictionary<string, List<string>> playerPrefsByScript;

    [MenuItem("Tools/Get All Player Prefs")]
    public static void ShowWindow()
    {
        GetWindow(typeof(PlayerPrefScanner), false, "GetPlayerPrefs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Get Player Prefs", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan All Player Prefs"))
        {
            ScanProject();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (playerPrefsByScript != null)
        {
            foreach (var kvp in playerPrefsByScript)
            {
                GUILayout.Label(kvp.Key, EditorStyles.boldLabel);
                foreach (var playerPref in kvp.Value)
                {
                    if (PlayerPrefs.HasKey(playerPref))
                    {
                        if (GUILayout.Button(playerPref))
                        {                            
                            PlayerPrefs.DeleteKey(playerPref);
                            Debug.Log($"{playerPref} deleted succesfully");                                                        
                        }
                    }
                    else
                    {
                        GUILayout.Label(playerPref + " (Deleted)");

                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        Handles.color = Color.white;
                        Handles.DrawLine(new Vector3(lastRect.x, lastRect.y + (lastRect.height / 2)), new Vector3(lastRect.x + lastRect.width, lastRect.y + (lastRect.height / 2)));
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void ScanProject()
    {
        playerPrefsByScript = new Dictionary<string, List<string>>();

        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in scriptFiles)
        {
            if (Path.GetFileNameWithoutExtension(file) == this.GetType().ToString())
            {
                continue;
            }

            string scriptContent = File.ReadAllText(file);
            if (scriptContent.Contains("PlayerPrefs.Set"))
            {
                string scriptName = Path.GetFileNameWithoutExtension(file);
                List<string> playerPrefsInScript = GetPlayerPrefsInScript(scriptContent);
                playerPrefsByScript.Add(scriptName, playerPrefsInScript);
            }
        }
    }

    private List<string> GetPlayerPrefsInScript(string scriptContent)
    {
        List<string> playerPrefs = new List<string>();

        int index = 0;
        while ((index = scriptContent.IndexOf("PlayerPrefs.Set", index)) != -1)
        {
            int start = index + "PlayerPrefs.Set".Length;
            int openQuoteIndex = scriptContent.IndexOf('"', start);
            if (openQuoteIndex == -1)
                break;

            int closeQuoteIndex = scriptContent.IndexOf('"', openQuoteIndex + 1);
            if (closeQuoteIndex == -1)
                break;

            string key = scriptContent.Substring(openQuoteIndex + 1, closeQuoteIndex - openQuoteIndex - 1);
            playerPrefs.Add(key);

            index = closeQuoteIndex;
        }

        return playerPrefs;
    }






}
