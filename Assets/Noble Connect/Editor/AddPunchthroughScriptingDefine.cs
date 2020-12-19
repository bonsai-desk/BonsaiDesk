#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace NobleConnect
{
    /// <summary>
    /// Adds the given define symbols to PlayerSettings define symbols.
    /// Just add your own define symbols to the Symbols property at the below.
    /// </summary>
    [InitializeOnLoad]
    public class AddScriptingDefine : Editor
    {

        /// <summary>
        /// Symbols that will be added to the editor
        /// </summary>
        public static readonly string[] Symbols = new string[] {
            "NOBLE_CONNECT", // Noble Connect exists
            "NOBLE_CONNECT_1", // Major version
            "NOBLE_CONNECT_1_07" // Major and minor version
        };

        /// <summary>
        /// Add define symbols as soon as Unity gets done compiling.
        /// </summary>
        static AddScriptingDefine()
        {
            // Get the current scripting defines
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            // Convert the string to a list
            List<string> allDefines = definesString.Split(';').ToList();
            // Remove any old version defines from previous installs
            for (int i = allDefines.Count-1; i >= 0; i--)
            {
                if (allDefines[i].StartsWith("NOBLE_CONNECT") && !Symbols.Contains(allDefines[i]))
                {
                    allDefines.RemoveAt(i);
                }
            }
            // Add any symbols that weren't already in the list
            allDefines.AddRange(Symbols.Except(allDefines));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", allDefines.ToArray())
            );
        }

    }
}
#endif