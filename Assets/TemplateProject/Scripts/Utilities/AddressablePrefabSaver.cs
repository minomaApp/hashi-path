#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    public class AddressablePrefabSaver : MonoBehaviour
    {
        public string prefabBasePath = "Assets/Prefabs/Levels/";
        public string addressableGroupName = "MyLevelsGroup";
 
        public void SaveAndAssignPrefab(GameObject levelRoot, int levelIndex)
        {
            if (!Directory.Exists(prefabBasePath))
            {
                Directory.CreateDirectory(prefabBasePath);
                AssetDatabase.Refresh();
            }

            var prefabName = $"Level_{levelIndex}";
            var prefabPath = prefabBasePath + prefabName + ".prefab";

            PrefabUtility.SaveAsPrefabAsset(levelRoot, prefabPath);
            Debug.Log($"Prefab saved at: {prefabPath}");

            AddToAddressables(prefabPath, addressableGroupName);
        }

        private void AddToAddressables(string prefabPath, string groupName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (!settings)
            {
                Debug.LogError("Addressables settings not found. Please initialize Addressables.");
                return;
            }

            var group = settings.FindGroup(groupName);
            if (!group)
            {
                group = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
            }

            var assetGUID = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.FindAssetEntry(assetGUID);

            if (entry != null) return;
            entry = settings.CreateOrMoveEntry(assetGUID, group);
            entry.SetLabel("Level", true, true);
            entry.address = Path.GetFileNameWithoutExtension(prefabPath);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, entry, true);
            AssetDatabase.SaveAssets();                
            string labels = string.Join(", ", entry.labels);
            Debug.Log($"Prefab assigned to Addressables group: {groupName}, with Labels = {labels}");
        }

        public void RemovePrefabFromAddressablesAndDelete(int levelIndex)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (!settings)
            {
                Debug.LogError("Addressables settings not found. Please initialize Addressables.");
                return;
            }

            var prefabName = $"Level_{levelIndex}";
            var prefabPath = prefabBasePath + prefabName + ".prefab";
            var assetGUID = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.FindAssetEntry(assetGUID);

            if (entry != null)
            {
                settings.RemoveAssetEntry(entry.guid);
                Debug.Log($"Prefab removed from Addressables: {prefabPath}");
            }
            else
            {
                Debug.LogWarning("Prefab not found in Addressables group.");
            }

            if (File.Exists(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.Refresh();
                Debug.Log($"Prefab deleted from project: {prefabPath}");
            }
            else
            {
                Debug.LogWarning("Prefab file does not exist.");
            }
        }
    }
}
#endif