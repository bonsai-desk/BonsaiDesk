using System;
using System.Collections;
using System.Text;
using NobleConnect;
using UnityEditor;
using UnityEngine;

public class SetupWizard : EditorWindow
{
    const string windowTitle = "Noble Connect";
    const string titleText = "Noble Connect Setup";
    const string bodyText = "\nEnter your game id or email address to enable Noble Connect.\n\n" +
                        "The service is free to use for development but bandwidth and CCU is limited.\n" +
                        "Visit noblewhale.com to upgrade to a paid account and remove the bandwidth and CCU limits.";
    const string signUpSuccessText = "Successful account signup. \n\n" +
        "Load up an example to get started or visit our website to upgrade to a paid account.\n";
    const string accountAlreadyExistsText = "User already exists. \n\n" +
        "Log in at noblewhale.com to get your game ID.\n";
    const string otherErrorText = "An error has occurred. \n\n" +
        "Log in at noblewhale.com to get your game ID.\n";
    const string enteredGameIDText = "GameID entered. \n\n" +
        "Welcome back. Visit noblewhale.com to upgrade to a paid account\n" +
        "and remove the bandwidth and CCU limits.\n";
    Texture2D logo, bg;

    GUIStyle headerStyle = new GUIStyle();
    GUIStyle titleStyle = new GUIStyle();
    GUIStyle logoStyle = new GUIStyle();
    GUIStyle bodyStyle = new GUIStyle();
    GUIStyle secondScreenStyle = new GUIStyle();

    bool clickedActivate = false;
    bool accountActivated = false;
    bool accountAlreadyExists = false;
    bool otherError = false;
    bool enteredGameID = false;

    IEnumerator createAccountRequest;

    string emailOrGameID;

    SetupWizard()
    {
        minSize = new Vector2(530, 300);
    }

    void OnEnable()
    {
        if (logo == null)
        {
            string[] paths = AssetDatabase.FindAssets("whale_256 t:Texture2D");
            if (paths != null && paths.Length > 0)
            {
                logo = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(paths[0]));
            }
        }
        if (bg == null)
        {
            string[] paths = AssetDatabase.FindAssets("Noble Setup Title Background t:Texture2D");
            if (paths != null && paths.Length > 0)
            {
                bg = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(paths[0]));
            }
        }
    }

    void Update()
    {
        if (createAccountRequest != null) createAccountRequest.MoveNext();
    }

    void OnGUI()
    {
        DrawHeader();

        if (!accountActivated && !accountAlreadyExists && !otherError && !enteredGameID)
        {
            bodyStyle.padding = new RectOffset(10, 10, 0, 5);
            EditorGUILayout.BeginVertical(bodyStyle);

            GUILayout.Label(bodyText);
            GUILayout.Label("\nEmail or Game ID");
            emailOrGameID = EditorGUILayout.TextField(emailOrGameID);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (!clickedActivate && GUILayout.Button("Activate"))
            {
                createAccountRequest = ActivateAccount();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        // Account creation success.
        if (accountActivated)
        {
            bodyStyle.padding = new RectOffset(15, 10, 15, 5);
            EditorGUILayout.BeginVertical(bodyStyle);
            GUILayout.Label(signUpSuccessText);
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            secondScreenStyle.padding = new RectOffset(15, Screen.width, 10, 5); 
            if (GUILayout.Button("Sign Up for Pro"))
            {
                Application.OpenURL("http://noblewhale.com");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();            
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Close Window"))
            {
                this.Close();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        // Found account with this email already.
        else if (accountAlreadyExists)
        {
            bodyStyle.padding = new RectOffset(15, 10, 15, 5);
            //GUI.contentColor = Color.red;
            //bodyStyle.normal.textColor = Color.red;
            EditorGUILayout.BeginVertical(bodyStyle);
            GUILayout.Label(accountAlreadyExistsText);// secondScreenStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            secondScreenStyle.padding = new RectOffset(15, Screen.width, 10, 5);
            if (GUILayout.Button("Go to noblewhale.com"))
            {
                Application.OpenURL("http://noblewhale.com");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();            
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Close Window"))
            {
                this.Close();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }       
        else if (otherError)
        {
            bodyStyle.padding = new RectOffset(15, 10, 15, 5);
            //bodyStyle.normal.textColor = new Color(204,0,0);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(otherErrorText, bodyStyle);
            EditorGUILayout.EndVertical();
            //bodyStyle.normal.textColor = Color.black;
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            secondScreenStyle.padding = new RectOffset(15, Screen.width, 10, 5);
            if (GUILayout.Button("Go to noblewhale.com"))
            {
                Application.OpenURL("http://noblewhale.com");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Close Window"))
            {
                this.Close();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        else if (enteredGameID)
        {
            bodyStyle.padding = new RectOffset(15, 10, 15, 5);
            //bodyStyle.normal.textColor = new Color(204,0,0);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(enteredGameIDText, bodyStyle);
            EditorGUILayout.EndVertical();            
            //bodyStyle.normal.textColor = Color.black;
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            secondScreenStyle.padding = new RectOffset(15, Screen.width, 10, 5);
            if (GUILayout.Button("Go to noblewhale.com"))
            {
                Application.OpenURL("http://noblewhale.com");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();            
            EditorGUILayout.BeginVertical(secondScreenStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Close Window"))
            {
                this.Close();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    IEnumerator ActivateAccount()
    {
        clickedActivate = true;
        string gameID = emailOrGameID;
        if (emailOrGameID.Contains("@"))
        {
            gameID = null;
            WWWForm form = new WWWForm();
            form.AddField("username", emailOrGameID);
            form.AddField("email", emailOrGameID);

#if UNITY_5 || UNITY_2017
            WWW w = new WWW("https://robot:z3hZAY*1ESiq7ecUR&OxFFNO@noblewhale.com/wp-json/wp/v2/users", form);

            while (!w.isDone) yield return 0;

            if (w.error != null && w.error != "")
            {
                if (w.text.Contains("existing_user_login"))
                {
                    Debug.LogError("User already exists. Log in at noblewhale.com to get your game ID.");
                    // TODO: Display error notification window
                    accountAlreadyExists = true;
                }
                else
                {
                    Debug.LogError(w.error + " " + w.text);
                    otherError = true;
                }
            }
            else
            {
                // Get the newly created game id from the response text
                // Manually parsing some json to avoid third party libraries and a bunch of needless overhead
                string key = "\"game_id_0\"";
                int keyIndex = w.text.IndexOf(key);
                int valueStartIndex = w.text.IndexOf("\"", keyIndex + key.Length + 1) + 1;
                int valueEndIndex = w.text.IndexOf("\"", valueStartIndex + 1);
                string value = w.text.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
                gameID = value;

                accountActivated = true;
            }

            w.Dispose();
#else
            using (var w = UnityEngine.Networking.UnityWebRequest.Post("https://robot:z3hZAY*1ESiq7ecUR&OxFFNO@noblewhale.com/wp-json/wp/v2/users", form))
            {
                var amazonCertificateHandler = new NobleConnect.Internal.AmazonCertificateHandler();
                w.certificateHandler = amazonCertificateHandler;

                yield return w.SendWebRequest();
                while (!w.isDone) yield return 0;

                var result = w.downloadHandler.text;

                if (w.isNetworkError || w.isHttpError)
                {
                    if (result.Contains("existing_user_login"))
                    {
                        Debug.LogError("User already exists. Log in at noblewhale.com to get your game ID.");
                        // TODO: Display error notification window
                        accountAlreadyExists = true;
                    }
                    else
                    {
                        Debug.LogError(w.error + " " + result);
                        otherError = true;
                    }
                }
                else
                {
                    // Get the newly created game id from the response text
                    // Manually parsing some json to avoid third party libraries and a bunch of needless overhead
                    Debug.Log(result);

                    StringBuilder sb = new StringBuilder();
                    foreach (var dict in w.GetResponseHeaders())
                    {
                        sb.Append(dict.Key).Append(": \t[").Append(dict.Value).Append("]\n");
                    }

                    // Print Headers
                    Debug.Log(sb.ToString());

                    string key = "\"game_id_0\"";
                    int keyIndex = result.IndexOf(key);
                    int valueStartIndex = result.IndexOf("\"", keyIndex + key.Length + 1) + 1;
                    int valueEndIndex = result.IndexOf("\"", valueStartIndex + 1);
                    string value = result.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
                    gameID = value;

                    accountActivated = true;
                }
            }
#endif
        }
        else
        {
            enteredGameID = true;
            // TODO: Test gameID somehow
        }

        if (gameID != null)
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));
            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<NobleConnectSettings>();
                if (!AssetDatabase.IsValidFolder("Assets/Noble Connect"))
                {
                    AssetDatabase.CreateFolder("Assets", "Noble Connect");
                }
                if (!AssetDatabase.IsValidFolder("Assets/Noble Connect/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets/Noble Connect", "Resources");
                }
                AssetDatabase.CreateAsset(settings, "Assets/Noble Connect/Resources/NobleConnectSettings.asset");
            }
            settings.gameID = gameID;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    void DrawHeader()
    {
        headerStyle.normal.background = bg;
        headerStyle.fixedHeight = 68;
        EditorGUILayout.BeginHorizontal(headerStyle);

        titleStyle.fontSize = 22;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.padding = new RectOffset(10, 10, 20, 10);
        GUILayout.Label(titleText, titleStyle);

        GUILayout.FlexibleSpace();

        logoStyle.fixedWidth = 50;
        logoStyle.margin = new RectOffset(0, 11, 7, 7);
        GUILayout.Label(logo, logoStyle);

        EditorGUILayout.EndHorizontal();
    }

    [MenuItem("Window/Noble Connect/Setup", false, 0)]
    protected static void MenuItemOpenWizard()
    {
        GetWindow(typeof(SetupWizard), false, windowTitle, true);
    }

    [InitializeOnLoad]
    public class ShowSetupWizard : EditorWindow
    {
        static bool hasChecked = false;
        static ShowSetupWizard()
        {
            EditorApplication.update += Update;
        }
        static void Update()
        {
            if (EditorApplication.timeSinceStartup > 3.0f && !hasChecked)
            {
                hasChecked = true;
                var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));
                if (!settings || (settings.gameID == ""))
                {
                    SetupWizard window = (SetupWizard)GetWindow(typeof(SetupWizard));
                    window.Show();
                }
            }
        }
    }
}