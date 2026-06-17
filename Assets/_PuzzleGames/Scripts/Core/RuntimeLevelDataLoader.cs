using BoxPuller.Scripts.Data;
using Newtonsoft.Json;
using UnityEngine;

public static class RuntimeLevelDataLoader
{
    public static LevelData LoadLevel(int levelIndex)
    {
        TextAsset textAsset = Resources.Load<TextAsset>($"LevelData/Level{levelIndex}");

        if (textAsset == null)
        {
            Debug.LogError($"[RuntimeLevelDataLoader] LevelData/Level{levelIndex} bulunamadý.");
            return null;
        }

        return JsonConvert.DeserializeObject<LevelData>(
            textAsset.text,
            new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            }
        );
    }
}