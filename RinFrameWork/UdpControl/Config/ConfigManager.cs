
using System.IO;
using UnityEngine;
namespace BigHead
{
    public static class ConfigManager
    {
        public static void SaveConfig(string json, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, json);
        }
        public static string LoadConfigFile(string path)
        {
            if (File.Exists(path)) return File.ReadAllText(path);
            return string.Empty;
        }
        public static string InitConfig(object target, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(target, prettyPrint);
        }
        public static T LoadConfig<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
        public static T Load<T>(T target, string path)
        {
            if (!File.Exists(path))
            {
                string json = InitConfig(target);
                SaveConfig(json, path);
                return target;
            }
            else
            {
                string json = LoadConfigFile(path);
                return LoadConfig<T>(json);
            }
        }
    }
}