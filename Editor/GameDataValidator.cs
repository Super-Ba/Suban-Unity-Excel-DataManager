using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Suban.DataManager
{
    public class GameDataValidator : EditorWindow
    {  
        private class DataInfo
        {
            public bool IsOpen;
            public string Name;

            public string[] GameDataFields;
            public string[] JsonFields;

            public bool JsonNotFound;
            public int NotMatch;
        }

        private static readonly string JsonDataPath = $"{Directory.GetCurrentDirectory()}/Assets/Data/GameData";

        [MenuItem("GameData/GameData Validator", false, 0)]
        public static void OpenGameDataValidator()
        {
            GetWindow(typeof(GameDataValidator));
        }

        private List<DataInfo> _infos;

        private Vector2 _scroll;

        private void Awake()
        {
            titleContent = new GUIContent("GameData Validator");
            
            SetDataInfos();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("새로고침")) SetDataInfos();

            if (_infos != null)
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                
                foreach (var info in _infos)
                {
                    EditorGUILayout.Space();
                    DrawInfo(info);
                    
                    if (!info.IsOpen)
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    DrawFields("JsonData" , info.JsonFields, GUILayout.ExpandWidth(true), GUILayout.MinWidth(200));
            
                    DrawFields($"{info.Name}" , info.GameDataFields, GUILayout.MaxWidth(999));
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawInfo(DataInfo info)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(30));

            EditorGUILayout.BeginHorizontal();
            
            var foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontSize = 15;
            foldoutStyle.fontStyle = FontStyle.Bold;
            info.IsOpen = EditorGUILayout.Foldout(info.IsOpen, info.Name, true, foldoutStyle);

            var errorStyle = new GUIStyle(EditorStyles.largeLabel);
            
            if (info.JsonNotFound)
            {
                errorStyle.normal.textColor = Color.red;
                GUILayout.Label("Json 파일 없음", errorStyle, GUILayout.ExpandWidth(false));
            }
            else if(info.NotMatch > 0)
            {
                errorStyle.normal.textColor = Color.yellow;
                GUILayout.Label($"NotMatch : {info.NotMatch}", errorStyle, GUILayout.ExpandWidth(false));
            }
            EditorGUILayout.Space(10, false);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawFields(string name, string[] info, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            
            var titleStyle = new GUIStyle(EditorStyles.largeLabel);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            
            GUILayout.Label($"{name}", titleStyle);
            GUILayout.Space(10);
            
            foreach (var fieldName in info)
            {
                EditorGUILayout.SelectableLabel($"{fieldName}", EditorStyles.largeLabel, GUILayout.Height(20));
                DrawLine();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void SetDataInfos()
        {
            var dataBaseTypes = Assembly.GetAssembly(typeof(IGameDataBase)).GetTypes().Where(t =>
                typeof(IGameDataBase) != t && typeof(IGameDataBase).IsAssignableFrom(t));

            _infos = new List<DataInfo>(dataBaseTypes.Count());

            foreach (var dataBaseType in dataBaseTypes)
            {
                var attribute = dataBaseType.GetCustomAttribute<GameDataBaseAttribute>();
                var json = JArray.Parse(File.ReadAllText($"{JsonDataPath}/{attribute.JsonFileName}.json"));

                var dataInfo = new DataInfo
                {
                    Name = attribute.GameDataType.Name
                };

                dataInfo.JsonNotFound = json == null;
                if (!dataInfo.JsonNotFound)
                {
                    dataInfo.JsonFields = new string[json[0].Count()];

                    for (int i = 0; i < dataInfo.JsonFields.Length; i++)
                    {
                        dataInfo.JsonFields[i] = json[0][i].ToString();
                    }
                }
                
                var propertyInfos = attribute.GameDataType.GetProperties().ToList();
                var gameDataFields = new string[dataInfo.JsonFields.Length].ToList();

                for (var i = 0; i < dataInfo.JsonFields.Length; i++)
                {
                    foreach (var property in propertyInfos)
                    {
                        if (property.Name.Equals(dataInfo.JsonFields[i]))
                        {
                            gameDataFields[i] = property.Name;
                            propertyInfos.Remove(property);
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(gameDataFields[i]))
                    {
                        dataInfo.NotMatch++;
                    }
                }
                
                foreach (var property in propertyInfos)
                {
                    gameDataFields.Add(property.Name);
                }
                
                dataInfo.GameDataFields = gameDataFields.ToArray();

                _infos.Add(dataInfo);
            }
        }
        
        private void DrawLine( int height = 1 )
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height );
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color ( 0.3f,0.3f,0.3f, 1.333f ) );
        }
    }
}