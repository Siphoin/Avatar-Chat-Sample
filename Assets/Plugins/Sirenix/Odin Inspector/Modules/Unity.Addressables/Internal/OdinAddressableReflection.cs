#if UNITY_EDITOR && ODIN_ADDRESSABLES_SUPPORT

using System.Reflection;
using UnityEditor.AddressableAssets.Settings;

namespace Sirenix.OdinInspector.Modules.Addressables.Editor.Internal
{
    internal static class OdinAddressableReflection
    {
        public static FieldInfo AddressableAssetEntry_mGUID_Field;

        static OdinAddressableReflection()
        {
            AddressableAssetEntry_mGUID_Field = typeof(AddressableAssetEntry).GetField("m_GUID", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        internal static void EnsureConstructed() { }
    }
}

#endif