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

    public interface IGameDataBase
    {
        public void RegisterData(IGameData data);
    }


    public class GameDataManager
    {
        public bool IsInitialized { get; private set; } = false;
        private const string JsonPath = "GameData/";

        private Dictionary<Type, IGameDataBase> _databases;

        public void Initialize()
        {
            _databases = new Dictionary<Type, IGameDataBase>();

            try
            {
                ParseGameData();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }


        private async UniTaskVoid ParseGameData()
        {
            var dataBaseTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
                typeof(IGameDataBase) != t && typeof(IGameDataBase).IsAssignableFrom(t));

            List<UniTask<(Type, IGameDataBase)>> tasks = new(dataBaseTypes.Count());

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

            IsInitialized = true;
        }

        private async UniTask<JArray> GetJson(GameDataBaseAttribute attribute)
        {
            var json = await Addressables.LoadAssetAsync<TextAsset>(JsonPath + attribute.JsonFileName + ".json");
            
            if (json == null)
            {
                Debug.LogError($"{attribute.GameDataType}과 매칭되는 json 파일을 찾을 수 없습니다. JsonFilePath : {JsonPath}/{attribute.JsonFileName}");
                return null;
            }

            var result = JArray.Parse(json.text);

            if (result == null || result.Count != 2)
            {
                Debug.LogError($"json파일의 형식에 문제가 있습니다. JsonFilePath : {JsonPath}/{attribute.JsonFileName}");
                return null;
            }

            return result;
        }

        private async UniTask<(Type, IGameDataBase)> SetDataBase(Type dataBaseType, GameDataBaseAttribute attribute, JArray jsonResult)
        {
            var database = Activator.CreateInstance(dataBaseType) as IGameDataBase;

            var gameDataTypes = new Dictionary<string, PropertyInfo>();
            var propertyInfos = attribute.GameDataType.GetProperties();

            foreach (JToken gameData in jsonResult[0])
            {
                var property = propertyInfos.FirstOrDefault(t => t.Name == gameData.ToString());

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
                            Debug.LogError($"{dataBaseType} : 지원되지 않는 형식의 변환입니다. Type : {type.Key} Value : {value}");
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


        // 조리예
        // GameDataManager.GetDataBase<TestGameDataBase>().GetData(1).Desc;
        public T GetDataBase<T>() where T : IGameDataBase
        {
            if (_databases.TryGetValue(typeof(T), out var dataBase))
            {
                return (T)dataBase;
            }

            return default(T);
        }
    }

}
