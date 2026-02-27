using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AvatarChat.Main.Configs
{
    public abstract class ScriptableConfig : ScriptableObjectIdentity
    {
        [Button(ButtonSizes.Medium, Name = "Add to Config Provider")]
        private void AddToProvider()
        {
#if UNITY_EDITOR
            var contextPrefab = Resources.Load<GameObject>("ProjectContext");

            if (contextPrefab == null)
            {
                Debug.LogError("ProjectContext prefab not found in Resources!");
                return;
            }

            var provider = contextPrefab.GetComponentInChildren<ConfigProvider>(true);

            if (provider == null)
            {
                Debug.LogError("ConfigProvider not found in ProjectContext prefab!");
                return;
            }

            Undo.RecordObject(provider, "Add Config to Provider");

            var field = typeof(Installers.ProviderInstaller<ScriptableConfig>)
                .GetField("_elements", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                var elements = (ScriptableConfig[])field.GetValue(provider) ?? new ScriptableConfig[0];

                if (elements.Contains(this))
                {
                    Debug.LogWarning($"{name} already exists in the provider.");
                    return;
                }

                var newElements = elements.Append(this).ToArray();
                field.SetValue(provider, newElements);

                EditorUtility.SetDirty(provider);
                AssetDatabase.SaveAssets();

                Debug.Log($"{name} successfully added to ProjectContext prefab.");
            }
#endif
        }
    }
}