using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor
{
    public static class Templates
    {
        public static void CreateScriptFromTemplate(
            string scriptPath,
            string templateName,
            string targetNamespace
        )
        {
            AssetDatabase.CreateAsset(new TextAsset(), scriptPath);

            List<string> lines = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Plugins/Unisave/Templates/" + templateName
            ).text.Split('\n').ToList();
            
            SetNamespace(lines, targetNamespace);
            
            File.WriteAllText(
                scriptPath,
                string.Join("\n", lines)
            );

            AssetDatabase.ImportAsset(scriptPath);
        }

        private static void SetNamespace(
            List<string> lines,
            string targetNamespace
        )
        {
            bool insideNamespace = false;
            
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == "$NAMESPACE_BEGIN$")
                {
                    insideNamespace = true;
                    
                    if (targetNamespace == null)
                    {
                        lines.RemoveAt(i);
                        i--;
                        continue;
                    }
                    
                    lines[i] = $"namespace {targetNamespace}";
                    lines.Insert(i, "{");
                    i++;
                    continue;
                }

                if (lines[i] == "$NAMESPACE_END$")
                {
                    insideNamespace = false;
                    
                    if (targetNamespace == null)
                    {
                        lines.RemoveAt(i);
                        i--;
                        continue;
                    }

                    lines[i] = "}";
                }

                if (insideNamespace && targetNamespace == null)
                {
                    lines[i] = RemoveIndent(lines[i]);
                }
            }
        }

        private static string RemoveIndent(string line)
        {
            if (line.StartsWith("    "))
                return line.Substring(4);

            return line;
        }
    }
}