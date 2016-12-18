using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZG
{
    public static class EditorHelper
    {
        public static IEnumerable<Type> loadedTypes
        {
            get
            {
                Assembly assembly = Assembly.Load("UnityEditor");
                if (assembly != null)
                {
                    Type editorAssemblies = assembly.GetType("UnityEditor.EditorAssemblies");
                    if (editorAssemblies != null)
                    {
                        MethodInfo loadedTypes = editorAssemblies.GetMethod("get_loadedTypes", BindingFlags.NonPublic | BindingFlags.Static);
                        IEnumerable<Type> types = loadedTypes.Invoke(null, null) as IEnumerable<Type>;

                        return types;
                    }
                }

                return null;
            }
        }

        public static IEnumerable<SerializedProperty> GetSiblings(this SerializedProperty property, int level)
        {
            if (property == null || level < 1)
                yield break;

            SerializedObject serializedObject = property.serializedObject;
            if (serializedObject == null)
                yield break;

            string propertyPath = property.propertyPath;
            if (propertyPath == null)
                yield break;

            Match match = Regex.Match(propertyPath, @".Array\.data\[([0-9]+)\]", RegexOptions.RightToLeft);
            if (match == Match.Empty)
                yield break;

            int matchIndex = match.Index;
            SerializedProperty parent = serializedObject.FindProperty(propertyPath.Remove(matchIndex));
            int arraySize = parent == null ? 0 : parent.isArray ? parent.arraySize : 0;
            if (arraySize < 1)
                yield break;

            StringBuilder stringBuilder = new StringBuilder(propertyPath);
            Group group = match.Groups[1];
            int index = int.Parse(group.Value), startIndex = group.Index, count = group.Length, i;
            for (i = 0; i < arraySize; ++ i)
            {
                if (i == index)
                    continue;

                stringBuilder = stringBuilder.Remove(startIndex, count);

                count = stringBuilder.Length;
                stringBuilder = stringBuilder.Insert(startIndex, i);
                count = stringBuilder.Length - count;

                yield return serializedObject.FindProperty(stringBuilder.ToString());
            }

            foreach (SerializedProperty temp in parent.GetSiblings(level - 1))
            {
                arraySize = temp == null ? 0 : temp.isArray ? temp.arraySize : 0;
                if (arraySize > 0)
                {
                    stringBuilder.Remove(0, matchIndex);
                    startIndex -= matchIndex;

                    propertyPath = temp.propertyPath;
                    stringBuilder = stringBuilder.Insert(0, propertyPath);
                    matchIndex = propertyPath == null ? 0 : propertyPath.Length;
                    startIndex += matchIndex;
                    for (i = 0; i < arraySize; ++i)
                    {
                        stringBuilder = stringBuilder.Remove(startIndex, count);

                        count = stringBuilder.Length;
                        stringBuilder = stringBuilder.Insert(startIndex, i);
                        count = stringBuilder.Length - count;

                        yield return serializedObject.FindProperty(stringBuilder.ToString());
                    }
                }
            }
        }

        public static string GetPropertyPath(string path)
        {
            return Regex.Replace(path, @".Array\.data(\[[0-9]+\])", "$1");
        }

        public static void HelpBox(Rect position, GUIContent label, string message, MessageType type)
        {
            float width = position.width;
            position.width = EditorGUIUtility.labelWidth;
            EditorGUI.PrefixLabel(position, label);
            position.x += position.width;
            position.width = width - position.width;
            EditorGUI.HelpBox(position, message, MessageType.Error);
        }

        public static string DelayedTextField(Rect rect, string value, string allowedLetters, GUIStyle style)
        {
            MethodInfo delayedTextField = typeof(EditorGUI).GetMethod(
                "DelayedTextField",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Rect), typeof(string), typeof(string), typeof(GUIStyle) },
                null);
            if (delayedTextField == null)
                return value;

            object[] parameters = new object[] { rect, value, allowedLetters, style };
            
            return delayedTextField.Invoke(null, parameters) as string;
        }

        public static int DelayedIntField(Rect rect, GUIContent label, int value)
        {
            rect = EditorGUI.PrefixLabel(rect, label);
            return int.Parse(DelayedTextField(rect, value.ToString(), "0123456789-", EditorStyles.numberField));
        }

        public static string ToolbarSearchField(Rect rect, string[] searchModes, ref int searchModeIndex, string text)
        {
            MethodInfo toolbarSearchField = typeof(EditorGUI).GetMethod(
                "ToolbarSearchField", 
                BindingFlags.Static | BindingFlags.NonPublic, 
                null, 
                new Type[] { typeof(Rect), typeof(string[]), typeof(int).MakeByRefType(), typeof(string) }, 
                null);
            if (toolbarSearchField == null)
                return null;
            
            object[] parameters = new object[] { rect, searchModes, searchModeIndex, text };

            string result = toolbarSearchField.Invoke(null, parameters) as string;

            searchModeIndex = (int)parameters[2];

            return result;
        }

        public static void ObjectIconDropDown(Rect position, UnityEngine.Object[] targets, bool showLabelIcons, Texture2D nullIcon, SerializedProperty iconProperty)
        {
            MethodInfo objectIconDropDown = typeof(EditorGUI).GetMethod(
                "ObjectIconDropDown",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Rect), typeof(UnityEngine.Object[]), typeof(bool), typeof(Texture2D), typeof(SerializedProperty) },
                null);
            if (objectIconDropDown == null)
                return;

            object[] parameters = new object[] { position, targets, showLabelIcons, nullIcon, iconProperty };

            objectIconDropDown.Invoke(null, parameters);
        }

        public static void DrawHeaderGUI(Editor editor, string title)
        {
            MethodInfo drawHeaderGUI = typeof(Editor).GetMethod("DrawHeaderGUI", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Editor), typeof(string) }, null);
            if (drawHeaderGUI != null)
                drawHeaderGUI.Invoke(null, new object[] { editor, title });
        }
        
        public static void CreateAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
                path = "Assets";
            else if (Path.GetExtension(path) != "")
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + asset.name + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        public static T CreateAsset<T>(string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            if (asset != null)
            {
                asset.name = assetName;

                CreateAsset(asset);
            }

            return asset;
        }
    }
}
