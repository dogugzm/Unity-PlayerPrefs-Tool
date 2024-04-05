using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
                GUILayout.Label(kvp.Key, EditorStyles.boldLabel); //script name as header.
                foreach (var playerPref in kvp.Value)
                {
                    if (PlayerPrefs.HasKey(playerPref))
                    {
                        if (GUILayout.Button(playerPref))  // pref name as button.
                        {
                            PlayerPrefs.DeleteKey(playerPref);
                            Debug.Log($"{playerPref} deleted succesfully");
                        }
                    }
                    else
                    {
                        GUILayout.Label(playerPref + " (Deleted)");
                        //trick to achieve strike-through style text in editor.
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
            //ignore this script
            if (Path.GetFileNameWithoutExtension(file) == GetType().ToString())
            {
                continue;
            }

            string scriptName = Path.GetFileNameWithoutExtension(file);
            string scriptContent = File.ReadAllText(file);

            if (!scriptContent.Contains("PlayerPrefs.Set"))
            {
                continue;
            }

            List<string> Keys = GetKeysInScript(scriptContent);

            playerPrefsByScript.Add(scriptName, Keys);

        }
    }

    private List<string> GetKeysInScript(string scriptContent)
    {
        List<string> Keys = new();

        int index = 0;
        while ((index = scriptContent.IndexOf("PlayerPrefs.Set", index)) != -1)
        {
            int start = index + "PlayerPrefs.Set".Length;
            int openParenIndex = scriptContent.IndexOf('(', start);
            if (openParenIndex == -1)
                break;

            int closeParenIndex = scriptContent.IndexOf(')', openParenIndex + 1);
            if (closeParenIndex == -1)
                break;

            string args = scriptContent.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);

            // split by comma to find the key
            string[] argParts = args.Split(',');
            if (argParts.Length >= 2)
            {
                string key = argParts[0].Trim();

                // check if it is a inline string or setted value
                if (!(key.StartsWith("\"") && key.EndsWith("\"")))
                {
                    string actualValue = FindProportyValueForKey(scriptContent, key);
                    actualValue = actualValue.Trim();
                    if (!Keys.Contains(actualValue))
                    {
                        Keys.Add(actualValue);
                    }

                }
                else
                {
                    string actualValue = key.Trim('"');
                    if (!Keys.Contains(actualValue))
                    {
                        Keys.Add(actualValue);
                    }
                }
            }

            index = closeParenIndex;
        }

        return Keys;
    }

    /// <summary>
    /// Search the script for specific matched pattern with proporty key
    /// </summary>
    /// <param name="scriptContent"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private string FindProportyValueForKey(string scriptContent, string key)
    {
        string constantValue = "";

        // both constant and non-constant string declarations
        string pattern = $@"\b(?:const\s+)?string\s+{key}\s*=\s*""(.+?)""";
        Match match = Regex.Match(scriptContent, pattern);
        if (match.Success)
        {
            constantValue = match.Groups[1].Value;
        }

        return constantValue;
    }
}
