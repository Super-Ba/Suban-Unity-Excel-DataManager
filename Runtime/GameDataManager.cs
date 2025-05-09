using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Suban.DataManager
{
    public interface IGameData
    {
        public int Index { get; }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public class GameDataBaseAttribute : Attribute
    {
        public Type GameDataType { get; }
        public string JsonFileName { get; }

        public GameDataBaseAttribute(Type gameDataType, string fileName)
        {
            GameDataType = gameDataType;
            JsonFileName = fileName;
        }
    }

    public abstract class GameDataBase
    {
        public abstract void RegisterData(IGameData data);
        public abstract void OnInitialize(GameDataManager manager);

        public GameDataBase(int capacity)
        {
            
        }
    }


    public class GameDataManager
    {
        public bool IsInitialized { get; private set; } = false;
        private const string JsonPath = "GameData/";

        private Dictionary<Type, GameDataBase> _databases;

        public async UniTask Initialize()
        {
            _databases = new Dictionary<Type, GameDataBase>();

            try
            {
               await ParseGameData();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }


        private async UniTask ParseGameData()
        {
            var dataBaseTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
                typeof(GameDataBase) != t && typeof(GameDataBase).IsAssignableFrom(t));

            List<UniTask<(Type, GameDataBase)>> tasks = new(dataBaseTypes.Count());

            foreach (var dataBaseType in dataBaseTypes)
            {
                var attribute = dataBaseType.GetCustomAttribute<GameDataBaseAttribute>();

                var json = await GetJson(attribute);

                if (json == null)
                {
                    continue;
                }

                tasks.Add(SetDataBase(dataBaseType, attribute, json));
            }

            var databases = await UniTask.WhenAll(tasks);

            foreach (var database in databases)
            {
                _databases.Add(database.Item1, database.Item2);
            }
            
            foreach (var database in _databases)
            {
                database.Value.OnInitialize(this);
            }

            IsInitialized = true;
        }

        private async UniTask<JArray> GetJson(GameDataBaseAttribute attribute)
        {
            var json = await Addressables.LoadAssetAsync<TextAsset>(JsonPath + attribute.JsonFileName + ".json");
            
            if (!json)
            {
                Debug.LogError($"No json files were found matching {attribute.GameDataType}. JsonFilePath : {JsonPath}/{attribute.JsonFileName}");
                return null;
            }

            var result = JArray.Parse(json.text);

            if (result.Count != 2)
            {
                Debug.LogError($"I have a problem with json file. JsonFilePath : {JsonPath}/{attribute.JsonFileName}");
                return null;
            }

            return result;
        }

        private async UniTask<(Type, GameDataBase)> SetDataBase(Type dataBaseType, GameDataBaseAttribute attribute, JArray jsonResult)
        {
            var database = Activator.CreateInstance(dataBaseType, jsonResult[1].Count()) as GameDataBase;
            
            var gameDataTypes = new Dictionary<string, PropertyInfo>();
            var propertyInfos = attribute.GameDataType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (JToken gameData in jsonResult[0])
            {
                var property = propertyInfos.FirstOrDefault(t => t.CanWrite && t.Name == gameData.ToString());

                if (property != null)
                {
                    gameDataTypes.Add(gameData.ToString(), property);
                }
            }
            
            foreach (JObject gameData in jsonResult[1])
            {
                var data = Activator.CreateInstance(attribute.GameDataType) as IGameData;

                foreach (var type in gameDataTypes)
                {
                    if (gameData.TryGetValue(type.Key, out var value))
                    {
                        var parsedValue = ParseValue(type.Value.PropertyType, value.ToString());

                        if (parsedValue != null)
                        {
                            type.Value.SetValue(data, parsedValue);
                        }
                        else
                        {
                            Debug.LogError($"{dataBaseType} : Conversion of an unsupported type. Type : {type.Key} Value : {value}");
                        }
                    }
                }

                database.RegisterData(data);
            }

            return (dataBaseType, database);
        }

        

        private object ParseValue(Type type, string value)
        {
            if (type == typeof(string))
            {
                return value;
            }
            if (type == typeof(int))
            {
                if (int.TryParse(value, out var result))
                {
                    return result;
                }
            }
            if (type == typeof(byte))
            {
                if (byte.TryParse(value, out var result))
                {
                    return result;
                }
            }
            if (type == typeof(float))
            {
                if (float.TryParse(value, out var result))
                {
                    return result;
                }
            }
            if (type == typeof(bool))
            {
                if (bool.TryParse(value, out var result))
                {
                    return result;
                }
            }
            if (type.IsEnum && Enum.TryParse(type, value, out var enumValue))
            {
                return enumValue;
            }

            return null;
        }


        // Sample:
        // GameDataManager.GetDataBase<TestGameDataBase>().GetData(1).Desc;
        public T GetDataBase<T>() where T : GameDataBase
        {
            if (_databases.TryGetValue(typeof(T), out var dataBase))
            {
                return (T)dataBase;
            }

            return default(T);
        }
    }

}
