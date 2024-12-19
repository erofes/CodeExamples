using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Logging;
using Game.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace Game.Addressables
{
    public sealed class AddressablesManager : MonoBehaviour
    {
        public static bool isDebug = false;
        public static Color debugColor = Log.consoleColor;
        public static int debugPrio;
        
        private static List<ScriptableObjectStringReferenceData> _SoKeyDatas = new List<ScriptableObjectStringReferenceData>();
        private static Dictionary<string, ScriptableObjectStringReferenceData> _SoKeyDataDict = new Dictionary<string, ScriptableObjectStringReferenceData>();

        private List<AssetReferenceTexture> _textureReferences = new List<AssetReferenceTexture>();
        private List<AssetReferenceSprite> _spriteReferences = new List<AssetReferenceSprite>();
        private List<PrefabReferenceData> _prefabReferences = new List<PrefabReferenceData>();
        private List<AssetReference> _soReferenceDatas = new List<AssetReference>();
        private List<AssetReferenceT<AnimationClip>> _animationClipReferences = new List<AssetReferenceT<AnimationClip>>();
        private Dictionary<AssetReferenceTexture, TextureReferenceData> _textureReferencesDict = new Dictionary<AssetReferenceTexture, TextureReferenceData>();
        private Dictionary<AssetReferenceSprite, SpriteReferenceData> _spriteReferencesDict = new Dictionary<AssetReferenceSprite, SpriteReferenceData>();
        private Dictionary<AssetReferenceGameObject, PrefabReferenceData> _prefabReferencesDict = new Dictionary<AssetReferenceGameObject, PrefabReferenceData>();
        private Dictionary<AssetReference, ScriptableObjectReferenceData> _soReferenceDataDict = new Dictionary<AssetReference, ScriptableObjectReferenceData>();
        private Dictionary<AssetReferenceT<AnimationClip>, AnimationClipReferenceData> _animationClipReferencesDict = new Dictionary<AssetReferenceT<AnimationClip>, AnimationClipReferenceData>();
        private SceneType _currentSceneType = SceneType.None;
        
        [System.Flags]
        public enum SceneType
        {
            None = 0,
            AppLoadingScene = 1,
            HomeScene = 2,
            LobbyScene = 4,
            GameScene = 8
        }

        private void FixedUpdate()
        {
            UpdateDataReferences();
            UpdateSpriteReferences();
            UpdateGameObjectReferences();
        }

        private void UpdateDataReferences()
        {
            for (var index = 0; index < _SoKeyDatas.Count; index++)
            {
                var data = _SoKeyDatas[index];

                if (data.IsReleaseRequired())
                {
                    ReleaseScriptableObjectKey(data);
                    _SoKeyDatas.RemoveAt(index);
                    index--;
                }
            }
        }

        private void UpdateSpriteReferences()
        {
            for (var index = 0; index < _spriteReferences.Count; index++)
            {
                var spriteReference = _spriteReferences[index];
                var data = _spriteReferencesDict[spriteReference];

                if (data.IsReleaseRequired())
                {
                    ReleaseSprite(spriteReference, false);
                    _spriteReferences.RemoveAt(index);
                    index--;
                }
            }
        }

        private void UpdateGameObjectReferences()
        {
            for (var index = 0; index < _prefabReferences.Count; index++)
            {
                var data = _prefabReferences[index];

                if (data.IsReleaseRequired())
                {
                    ReleasePrefab(data, false);
                    _prefabReferences.RemoveAt(index);
                    index--;
                }
            }
        }

        public static T GetData<T>(string assetPath) where T : ScriptableObject
        {
            var isLoaded = _SoKeyDataDict.TryGetValue(assetPath, out var value);
            if (isLoaded)
            {
                value.UpdateLastRequestTime();
                return value.Data as T;
            }

            var handle = Addressables.LoadAssetAsync<T>(assetPath);
            var result = handle.WaitForCompletion();
            value = new ScriptableObjectStringReferenceData(result, assetPath, handle);
            if (value.Data == null)
            {
                Log.E($"Not found {typeof(T).Name} at path {assetPath}");
                return default;
            }

            _SoKeyDatas.Add(value);
            _SoKeyDataDict.Add(assetPath, value);
            return result;
        }
        
#if UNITY_EDITOR
        public static bool ConvertToAssetReference<T>(List<T> list, ref List<AssetReferenceT<T>> listRef) where T : ScriptableObject
        {
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item == null)
                    {
                        Log.E($"One of items is null!");
                        return false;
                    }
                }

                var newItemRefs = new List<AssetReferenceT<T>>(list.Count);

                foreach (var item in list)
                {
                    if (AddressablesManager.GetEditorAssetReferenceT(item, out var newReference))
                    {
                        newItemRefs.Add(newReference);
                    }
                    else
                    {
                        Log.W($"Can't set addressable links for item: {item.name}", item);
                        return false;
                    }
                }
                listRef = newItemRefs;
                list.Clear();
            }

            return true;
        }
        
        public static bool ConvertToAssetReference(List<Texture> list, ref List<AssetReferenceTexture> listRef)
        {
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item == null)
                    {
                        Log.E($"One of items is null!");
                        return false;
                    }
                }

                var newItemRefs = new List<AssetReferenceTexture>(list.Count);

                foreach (var item in list)
                {
                    if (GetEditorAssetReference(item, out var newReference))
                    {
                        newItemRefs.Add(newReference);
                    }
                    else
                    {
                        Log.W($"Can't set addressable links for item: {item.name}", item);
                        return false;
                    }
                }
                listRef = newItemRefs;
                list.Clear();
            }

            return true;
        }
        
        public static bool GetEditorAssetReferenceT<T>(T directLink, out AssetReferenceT<T> readyReference) where T : Object
        {
            // Check if icon direct link exist or asset reference is set or raise warning and return
            if (directLink == null)
            {
                Log.W("Direct link isn't set for any asset");
                readyReference = null;
                return false;
            }
            
            // Check for addressables existance
            if (!IsAssetAddressable(directLink))
            {
                Log.W($"Direct link isn't marked as addressable! AssetName:{directLink.name}", directLink);
                readyReference = null;
                return false;
            }

            // Create AssetReference links
            var assetPath = AssetDatabase.GetAssetPath(directLink);
            var assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            readyReference = new AssetReferenceT<T>(assetGuid);

            readyReference.SetEditorSubObject(directLink);

            return true;
        }
        
        public static bool GetEditorAssetReference(Sprite directLink, out AssetReferenceSprite readyReference)
        {
            // Check if icon direct link exist or asset reference is set or raise warning and return
            if (directLink == null)
            {
                Log.W("Direct link isn't set for any asset");
                readyReference = null;
                return false;
            }
            
            // Check for addressables existance
            if (!IsAssetAddressable(directLink))
            {
                Log.W($"Direct link isn't marked as addressable! AssetName:{directLink.name}", directLink);
                readyReference = null;
                return false;
            }

            // Create AssetReference links
            var assetPath = AssetDatabase.GetAssetPath(directLink);
            var assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            readyReference = new AssetReferenceSprite(assetGuid)
            {
                SubObjectName = directLink.name
            };

            readyReference.SetEditorSubObject(directLink);

            return true;
        }
        
        public static bool GetEditorAssetReference(Texture directLink, out AssetReferenceTexture readyReference)
        {
            // Check if icon direct link exist or asset reference is set or raise warning and return
            if (directLink == null)
            {
                Log.W("Direct link isn't set for any asset");
                readyReference = null;
                return false;
            }
            
            // Check for addressables existance
            if (!IsAssetAddressable(directLink))
            {
                Log.W($"Direct link isn't marked as addressable! AssetName:{directLink.name}", directLink);
                readyReference = null;
                return false;
            }

            // Create AssetReference links
            var assetPath = AssetDatabase.GetAssetPath(directLink);
            var assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            readyReference = new AssetReferenceTexture(assetGuid);

            readyReference.SetEditorSubObject(directLink);

            return true;
        }
        
        public static bool GetEditorAssetReference(GameObject directLink, out AssetReferenceGameObject readyReference)
        {
            // Check if icon direct link exist or asset reference is set or raise warning and return
            if (directLink == null)
            {
                Log.W("Direct link isn't set for any asset");
                readyReference = null;
                return false;
            }
            
            // Check for addressables existance
            if (!IsAssetAddressable(directLink))
            {
                Log.W($"Direct link isn't marked as addressable! AssetName:{directLink.name}", directLink);
                readyReference = null;
                return false;
            }

            // Create AssetReference links
            var assetPath = AssetDatabase.GetAssetPath(directLink);
            var assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            readyReference = new AssetReferenceGameObject(assetGuid);

            readyReference.SetEditorSubObject(directLink);

            return true;
        }
        
        public static bool IsAssetAddressable(Object obj)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
            return entry != null;
        }
#endif

        public void SetCurrentSceneType(SceneType sceneType)
        {
            if (isDebug) Log.D($"Set {nameof(sceneType)}:{sceneType}, old:{_currentSceneType}", debugColor, debugPrio, 1);
            _currentSceneType = sceneType;
        }

        // Extension for common use
        public T GetDataSync<T>(AssetReferenceT<T> assetReference, GameObject gameObjectCondition) where T : ScriptableObject
        {
            return GetData(assetReference, gameObjectCondition, true, false, true).GetCache();
        }

        // Extension for common use (unload of current scene by default)
        public T GetDataSync<T>(AssetReferenceT<T> assetReference) where T : ScriptableObject
        {
            return GetData(assetReference, true).GetCache();
        }

        public QAction<T> GetData<T>(AssetReferenceT<T> assetReference, GameObject gameObjectCondition, bool activeSelfCondition, bool activeInHierarchyCondition, bool sync = false) where T : ScriptableObject
        {
            if (assetReference.AssetGUID.IsNullOrEmpty())
            {
                Log.E("Passing empty asset reference!");
                return null;
            }
            
            if (!_soReferenceDataDict.TryGetValue(assetReference, out var data))
            {
#if UNITY_EDITOR
                if (isDebug) Log.D($"New data {nameof(assetReference)}:{assetReference.editorAsset.name}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", assetReference.editorAsset, debugColor, debugPrio, 2);
#endif
                data = CreateScriptableObjectData(assetReference);
            }
#if UNITY_EDITOR
            else if (isDebug) Log.D($"Existing sprite {nameof(assetReference)}:{assetReference.editorAsset.name}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", assetReference.editorAsset, debugColor, debugPrio, 2);
#endif
            
            var assetReferenceT = (ScriptableObjectReferenceData<T>)data;
            assetReferenceT.AddGameObjectCondition(gameObjectCondition, activeSelfCondition, activeInHierarchyCondition);
            return assetReferenceT.LoadResource(sync);
        }

        public QAction<T> GetData<T>(AssetReferenceT<T> assetReference, bool sync = false) where T : ScriptableObject
        {
            if (assetReference.AssetGUID.IsNullOrEmpty())
            {
                Log.E("Passing empty asset reference!");
                return null;
            }
            
            if (!_soReferenceDataDict.TryGetValue(assetReference, out var data))
            {
                if (isDebug) Log.D($"New data {nameof(assetReference)}:{assetReference}, set current:{_currentSceneType}", debugColor, debugPrio, 2);
                data = CreateScriptableObjectData(assetReference);
            }
            else if (isDebug) 
                Log.D($"Existing sprite {nameof(assetReference)}:{assetReference}, set current:{_currentSceneType}", debugColor, debugPrio, 2);

            var assetReferenceT = (ScriptableObjectReferenceData<T>)data;
            assetReferenceT.SetSceneUnloadCondition(_currentSceneType);
            return assetReferenceT.LoadResource(sync);
        }
        
        public QAction<Texture> GetTexture(AssetReferenceTexture textureReference, GameObject gameObjectCondition, bool activeSelfCondition, bool activeInHierarchyCondition, bool sync = false)
        {
            if (!_textureReferencesDict.TryGetValue(textureReference, out var data))
            {
                if (isDebug) Log.D($"New texture {nameof(textureReference)}:{textureReference}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);
                data = CreateTextureData(textureReference);
            }
            else if (isDebug) 
                Log.D($"Existing texture {nameof(textureReference)}:{textureReference}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);

            data.AddGameObjectCondition(gameObjectCondition, activeSelfCondition, activeInHierarchyCondition);
            return data.LoadResource(sync);
        }

        public QAction<Texture> GetTexture(AssetReferenceTexture textureReference, bool sync = false)
        {
            if (!_textureReferencesDict.TryGetValue(textureReference, out var data))
            {
                if (isDebug) Log.D($"New texture {nameof(textureReference)}:{textureReference}, set current:{_currentSceneType}", debugColor, debugPrio, 4);
                data = CreateTextureData(textureReference);
            }
            else if (isDebug) 
                Log.D($"Existing texture {nameof(textureReference)}:{textureReference}, set current:{_currentSceneType}", debugColor, debugPrio, 4);

            data.SetSceneUnloadCondition(_currentSceneType);
            return data.LoadResource(sync);
        }

        public QAction<Sprite> GetSprite(AssetReferenceSprite spriteReference, GameObject gameObjectCondition, bool activeSelfCondition, bool activeInHierarchyCondition, bool sync = false)
        {
            if (!_spriteReferencesDict.TryGetValue(spriteReference, out var data))
            {
                if (isDebug) Log.D($"New sprite {nameof(spriteReference)}:{spriteReference}, sub:{spriteReference.SubObjectName}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);
                data = CreateSpriteData(spriteReference);
            }
            else if (isDebug) 
                Log.D($"Existing sprite {nameof(spriteReference)}:{spriteReference}, sub:{spriteReference.SubObjectName}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);

            data.AddGameObjectCondition(gameObjectCondition, activeSelfCondition, activeInHierarchyCondition);
            return data.LoadResource(sync);
        }

        public QAction<Sprite> GetSprite(AssetReferenceSprite spriteReference, MonoBehaviour enabledCondition, bool sync = false)
        {
            if (!_spriteReferencesDict.TryGetValue(spriteReference, out var data))
            {
                if (isDebug) Log.D($"New sprite {nameof(spriteReference)}:{spriteReference}, {nameof(enabledCondition)}:{enabledCondition.name}, active:{enabledCondition.enabled}", enabledCondition, debugColor, debugPrio, 3);
                data = CreateSpriteData(spriteReference);
            }
            else if (isDebug) 
                Log.D($"Existing sprite {nameof(spriteReference)}:{spriteReference}, {nameof(enabledCondition)}:{enabledCondition.name}, active:{enabledCondition.enabled}", enabledCondition, debugColor, debugPrio, 3);

            data.AddEnabledCondition(enabledCondition);
            return data.LoadResource(sync);
        }

        public QAction<Sprite> GetSprite(AssetReferenceSprite spriteReference, bool sync = false)
        {
            if (!_spriteReferencesDict.TryGetValue(spriteReference, out var data))
            {
#if UNITY_EDITOR
                if (isDebug) Log.D($"New sprite {nameof(spriteReference)}:{spriteReference.editorAsset.name}, set current:{_currentSceneType}", spriteReference.editorAsset, debugColor, debugPrio, 4);
#endif
                data = CreateSpriteData(spriteReference);
            }
#if UNITY_EDITOR
            else if (isDebug) 
                Log.D($"Existing sprite {nameof(spriteReference)}:{spriteReference.editorAsset.name}, set current:{_currentSceneType}", spriteReference.editorAsset, debugColor, debugPrio, 4);
#endif

            data.SetSceneUnloadCondition(_currentSceneType);
            return data.LoadResource(sync);
        }

        public QAction<GameObject> GetPrefab(AssetReferenceGameObject prefabReference, GameObject gameObjectCondition, bool activeSelfCondition, bool activeInHierarchyCondition, bool sync = false)
        {
            if (!_prefabReferencesDict.TryGetValue(prefabReference, out var data))
            {
                if (isDebug) Log.D($"New prefab {nameof(prefabReference)}:{prefabReference}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);
                data = CreatePrefabData(prefabReference);
            }
            else if (isDebug) 
                Log.D($"Existing prefab {nameof(prefabReference)}:{prefabReference}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);

            data.AddGameObjectCondition(gameObjectCondition, activeSelfCondition, activeInHierarchyCondition);
            return data.LoadResource(sync);
        }

        public QAction<GameObject> GetPrefab(AssetReferenceGameObject prefabReference, MonoBehaviour enabledCondition, bool sync = false)
        {
            if (!_prefabReferencesDict.TryGetValue(prefabReference, out var data))
            {
                if (isDebug) Log.D($"New prefab {nameof(prefabReference)}:{prefabReference}, {nameof(enabledCondition)}:{enabledCondition.name}, active:{enabledCondition.enabled}", enabledCondition, debugColor, debugPrio, 3);
                data = CreatePrefabData(prefabReference);
            }
            else if (isDebug) 
                Log.D($"Existing prefab {nameof(prefabReference)}:{prefabReference}, {nameof(enabledCondition)}:{enabledCondition.name}, active:{enabledCondition.enabled}", enabledCondition, debugColor, debugPrio, 3);

            data.AddEnabledCondition(enabledCondition);
            return data.LoadResource(sync);
        }

        public QAction<GameObject> GetPrefab(AssetReferenceGameObject prefabReference, SceneType sceneUnloadCondition, bool sync = false)
        {
            if (!_prefabReferencesDict.TryGetValue(prefabReference, out var data))
            {
                if (isDebug) Log.D($"New prefab {nameof(prefabReference)}:{prefabReference}, {nameof(sceneUnloadCondition)}:{sceneUnloadCondition}, current:{_currentSceneType}", debugColor, debugPrio, 4);
                data = CreatePrefabData(prefabReference);
            }
            else if (isDebug) 
                Log.D($"Existing prefab {nameof(prefabReference)}:{prefabReference}, {nameof(sceneUnloadCondition)}:{sceneUnloadCondition}, current:{_currentSceneType}", debugColor, debugPrio, 4);

            data.SetSceneUnloadCondition(sceneUnloadCondition);
            return data.LoadResource(sync);
        }

        public QAction<GameObject> GetPrefab(AssetReferenceGameObject prefabReference, bool sync = false)
        {
            if (!_prefabReferencesDict.TryGetValue(prefabReference, out var data))
            {
                if (isDebug) Log.D($"New prefab {nameof(prefabReference)}:{prefabReference}, set current:{_currentSceneType}", debugColor, debugPrio, 4);
                data = CreatePrefabData(prefabReference);
            }
            else if (isDebug) 
                Log.D($"Existing prefab {nameof(prefabReference)}:{prefabReference}, set current:{_currentSceneType}", debugColor, debugPrio, 4);

            data.SetSceneUnloadCondition(_currentSceneType);
            return data.LoadResource(sync);
        }

        public QAction<AnimationClip> GetAnimationClip(AssetReferenceT<AnimationClip> animationClip, GameObject gameObjectCondition, bool activeSelfCondition, bool activeInHierarchyCondition, bool sync = false)
        {
            if (!_animationClipReferencesDict.TryGetValue(animationClip, out var data))
            {
                if (isDebug) Log.D($"New animation clip {nameof(animationClip)}:{animationClip}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);
                data = CreateAnimationClipData(animationClip);
            }
            else 
                if (isDebug) Log.D($"Existing animation clip {nameof(animationClip)}:{animationClip}, {nameof(gameObjectCondition)}:{gameObjectCondition.name}, active:{gameObjectCondition.activeSelf}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObjectCondition, debugColor, debugPrio, 2);
            
            data.AddGameObjectCondition(gameObjectCondition, activeSelfCondition, activeInHierarchyCondition);
            return data.LoadResource(sync);
        }

        public QAction<AnimationClip> GetAnimationClip(AssetReferenceT<AnimationClip> animationClip, bool sync = false)
        {
            if (!_animationClipReferencesDict.TryGetValue(animationClip, out var data))
            {
                if (isDebug) Log.D($"New animation clip {nameof(animationClip)}:{animationClip}, set current:{_currentSceneType}", debugColor, debugPrio, 4);
                data = CreateAnimationClipData(animationClip);
            }
            else if (isDebug) 
                Log.D($"Existing animation clip {nameof(animationClip)}:{animationClip}, set current:{_currentSceneType}", debugColor, debugPrio, 4);

            data.SetSceneUnloadCondition(_currentSceneType);
            return data.LoadResource(sync);
        }

        public T GetByRandom<T>(IList<AssetReferenceT<T>> assetReferences, GameObject go, System.Func<T, int, bool> condition) where T : ScriptableObject
        {
            var isNull = assetReferences == null || assetReferences.Count == 0;  
            if (isNull)  
            {  
                Log.E($"assetReferences == null || assetReferences.Count == 0");  
                return null;  
            }

            var count = assetReferences.Count;
            var randomIteration = ArrayUtil.GetIndexArray(0, count - 1, true);
            
            T randomAsset = null;
            foreach (var index in randomIteration)
            {
                var assetRef = assetReferences[index];
                if (assetRef.AssetGUID.IsNullOrEmpty())
                    continue;
                
                var asset = GetDataSync(assetRef, go);
                if (condition(asset, index))
                {
                    randomAsset = asset;
                    break;
                }

                ReleaseScriptableObject(assetRef, true);
            }

            return randomAsset;
        }

        public T GetByConditionOrFirst<T>(IList<AssetReferenceT<T>> assetReferences, GameObject go, System.Func<T, int, bool> condition) where T : ScriptableObject
        {
            var isNull = assetReferences == null || assetReferences.Count == 0;  
            if (isNull)  
            {  
                Log.E($"assetReferences == null || assetReferences.Count == 0");  
                return null;  
            }  
  
            T firstAsset = null;
            T foundAsset = null;
            int firstIndex = -1;
            for (var index = 0; index < assetReferences.Count; index++)
            {
                var assetRef = assetReferences[index];
                if (assetRef.AssetGUID.IsNullOrEmpty())
                    continue;
  
                var asset = GetDataSync(assetRef, go);
                if (firstAsset == null)
                {
                    firstIndex = index;
                    firstAsset = asset;
                }
                
                if (condition(asset, index))
                {
                    foundAsset = asset;
                    if (foundAsset != firstAsset)
                        ReleaseScriptableObject(assetReferences[firstIndex], true);
                    break;
                }
                
                if (firstAsset != asset)
                    ReleaseScriptableObject(assetRef, true);
            }

            if (foundAsset == null)
                return firstAsset;
            return foundAsset;
        }
        
        public T GetByCondition<T>(IList<AssetReferenceT<T>> assetReferences, GameObject go, System.Func<T, int, bool> condition) where T : ScriptableObject
        {
            var isNull = assetReferences == null || assetReferences.Count == 0;  
            if (isNull)  
            {  
                Log.E($"assetReferences == null || assetReferences.Count == 0");  
                return null;  
            }  
  
            T foundAsset = null;
            for (var index = 0; index < assetReferences.Count; index++)
            {
                var assetRef = assetReferences[index];
                if (assetRef.AssetGUID.IsNullOrEmpty())
                    continue;
  
                var asset = GetDataSync(assetRef, go);
                
                if (condition(asset, index))
                {
                    foundAsset = asset;
                    break;
                }
                
                ReleaseScriptableObject(assetRef, true);
            }

            if (foundAsset == null)
                return null;
            return foundAsset;
        }
        
        public List<T> GetByWhere<T>(IList<AssetReferenceT<T>> assetReferences, GameObject go, System.Func<T, int, bool> condition) where T : ScriptableObject
        {
            var isNull = assetReferences == null || assetReferences.Count == 0;  
            if (isNull)  
            {  
                Log.E($"assetReferences == null || assetReferences.Count == 0");  
                return null;  
            }  
  
            List<T> resultList = new List<T>(assetReferences.Count);

            for (var index = 0; index < assetReferences.Count; index++)
            {
                var assetRef = assetReferences[index];
                if (assetRef.AssetGUID.IsNullOrEmpty())
                    continue;
  
                var asset = GetDataSync(assetRef, go);
#if UNITY_EDITOR
                if (asset == null)
                    Log.E("Got null asset!", assetRef.editorAsset);
#endif
                if (condition(asset, index))
                    resultList.Add(asset);
                else
                    ReleaseScriptableObject(assetRef, true);
            }

            return resultList;
        }

        private ScriptableObjectReferenceData<T> CreateScriptableObjectData<T>(AssetReferenceT<T> assetReference) where T : ScriptableObject
        {
            if (isDebug) Log.D($"Existing data {nameof(assetReference)}:{assetReference}", debugColor, debugPrio, 5);
            _soReferenceDatas.Add(assetReference);
            var data = new ScriptableObjectReferenceData<T>(assetReference, this);
            _soReferenceDataDict.Add(assetReference, data);
            return data;
        }

        private TextureReferenceData CreateTextureData(AssetReferenceTexture textureReference)
        {
            if (isDebug) Log.D($"Existing texture {nameof(textureReference)}:{textureReference}", debugColor, debugPrio, 5);
            _textureReferences.Add(textureReference);
            var data = new TextureReferenceData(textureReference, this);
            _textureReferencesDict.Add(textureReference, data);
            return data;
        }

        private SpriteReferenceData CreateSpriteData(AssetReferenceSprite spriteReference)
        {
            if (isDebug) Log.D($"Existing sprite {nameof(spriteReference)}:{spriteReference}", debugColor, debugPrio, 5);
            _spriteReferences.Add(spriteReference);
            var data = new SpriteReferenceData(spriteReference, this);
            _spriteReferencesDict.Add(spriteReference, data);
            return data;
        }

        private PrefabReferenceData CreatePrefabData(AssetReferenceGameObject prefabReference)
        {
            if (isDebug) Log.D($"Existing sprite {nameof(prefabReference)}:{prefabReference}", debugColor, debugPrio, 5);
            var data = new PrefabReferenceData(prefabReference, this);
            _prefabReferences.Add(data);
            _prefabReferencesDict.Add(prefabReference, data);
            return data;
        }

        private AnimationClipReferenceData CreateAnimationClipData(AssetReferenceT<AnimationClip> animationClipReference)
        {
            if (isDebug) Log.D($"Existing animation clip {nameof(animationClipReference)}:{animationClipReference}", debugColor, debugPrio, 5);
            _animationClipReferences.Add(animationClipReference);
            var data = new AnimationClipReferenceData(animationClipReference, this);
            _animationClipReferencesDict.Add(animationClipReference, data);
            return data;
        }

        private void ReleaseScriptableObjectKey(ScriptableObjectStringReferenceData scriptableObjectStringReferenceData)
        {
#if UNITY_EDITOR
            if (isDebug) Log.D($"Release {scriptableObjectStringReferenceData.Data.name}", debugColor, debugPrio, 8);
#endif
            _SoKeyDataDict.Remove(scriptableObjectStringReferenceData.Key);
            Addressables.Release(scriptableObjectStringReferenceData.Handle);
        }

        private void ReleaseScriptableObject<T>(AssetReferenceT<T> assetReference, bool alsoFromList) where T : ScriptableObject
        {
#if UNITY_EDITOR
            if (isDebug) Log.D($"Release {assetReference.editorAsset.name}", assetReference.editorAsset, debugColor, debugPrio, 8);
#endif
            if (alsoFromList)
                _soReferenceDatas.Remove(assetReference);
            _soReferenceDataDict.Remove(assetReference);
            assetReference.ReleaseAsset();
        }

        private void ReleaseTexture(AssetReferenceTexture textureReference, bool alsoFromList)
        {
            if (isDebug) Log.D($"Release {textureReference}", debugColor, debugPrio, 8);
            if (alsoFromList)
                _textureReferences.Remove(textureReference);
            _textureReferencesDict.Remove(textureReference);
            textureReference.ReleaseAsset();
        }

        private void ReleaseSprite(AssetReferenceSprite spriteReference, bool alsoFromList)
        {
            if (isDebug) Log.D($"Release {spriteReference}", debugColor, debugPrio, 8);
            if (alsoFromList)
                _spriteReferences.Remove(spriteReference);
            _spriteReferencesDict.Remove(spriteReference);
            spriteReference.ReleaseAsset();
        }

        private void ReleasePrefab(PrefabReferenceData prefabReference, bool alsoFromList)
        {
            if (isDebug) Log.D($"Release {prefabReference}", debugColor, debugPrio, 8);
            if (alsoFromList)
                _prefabReferences.Remove(prefabReference);
            _prefabReferencesDict.Remove(prefabReference.Asset);
            prefabReference.Asset.ReleaseAsset();
        }

        private void ReleaseAnimationClip(AssetReferenceT<AnimationClip> animationClipReference, bool alsoFromList)
        {
            if (isDebug) Log.D($"Release {animationClipReference}", debugColor, debugPrio, 8);
            if (alsoFromList)
                _animationClipReferences.Remove(animationClipReference);
            _animationClipReferencesDict.Remove(animationClipReference);
            animationClipReference.ReleaseAsset();
        }

        private sealed class ScriptableObjectStringReferenceData
        {
            //TODO: Timer only condtion will lead to missing refs of content users, should not use direct links in SO
            //TODO: so you can just load needed content and then unload whole database, leaving only your needed objects alive
            private const float TIMEOUT = 9e20f;

            private readonly ScriptableObject _scriptableObject;
            private string _key;
            private float _releaseTime;
            private AsyncOperationHandle _handle;

            public ScriptableObject Data => _scriptableObject;
            public string Key => _key;
            public AsyncOperationHandle Handle => _handle;

            public ScriptableObjectStringReferenceData(ScriptableObject scriptableObject, string key, AsyncOperationHandle handle)
            {
                _scriptableObject = scriptableObject;
                _key = key;
                _handle = handle;
                _releaseTime = Time.time + TIMEOUT;
            }

            public void UpdateLastRequestTime()
            {
                _releaseTime = Time.time + TIMEOUT;
            }

            public bool IsReleaseRequired()
            {
                return Time.time >= _releaseTime;
            }

            public override int GetHashCode()
            {
                return _scriptableObject.GetHashCode();
            }
        }

        private abstract class ScriptableObjectReferenceData { }
        
        private sealed class ScriptableObjectReferenceData<T> : ScriptableObjectReferenceData where T : ScriptableObject
        {
            private AsyncOperationHandle<T> _loadHandle;
            private List<GameObject> _activeSelfGameObjectConditions = new List<GameObject>();
            private List<GameObject> _activeInHierarchyGameObjectConditions = new List<GameObject>();
            private List<MonoBehaviour> _enabledConditions = new List<MonoBehaviour>();
            private SceneType _unloadSceneCondition = SceneType.None;
            private AssetReferenceT<T> _assetReference;
            private AddressablesManager _manager;
            private QAction<T> _onComplete;
            private bool _isLoadCalled;

            public ScriptableObjectReferenceData(AssetReferenceT<T> assetReference, AddressablesManager manager)
            {
                _assetReference = assetReference;
                _manager = manager;
            }

            public void AddGameObjectCondition(GameObject gameObject, bool activeSelfCondition, bool activeInHierarchyCondition)
            {
                if (isDebug) Log.D($"{nameof(gameObject)}:{gameObject.name}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObject, debugColor, debugPrio, 6);

#if UNITY_EDITOR
                if (activeSelfCondition && !gameObject.activeSelf) Log.W("Passing game object condition which was already inactive self");
                if (activeInHierarchyCondition && !gameObject.activeInHierarchy) Log.W("Passing game object condition which was already inactive in hierarchy");
#endif

                if (activeSelfCondition)
                {
                    _activeSelfGameObjectConditions.RemoveAll(x => x == null);
                    _activeSelfGameObjectConditions.Add(gameObject);
                }

                if (activeInHierarchyCondition)
                {
                    _activeInHierarchyGameObjectConditions.RemoveAll(x => x == null);
                    _activeInHierarchyGameObjectConditions.Add(gameObject);
                }
            }

            public void AddEnabledCondition(MonoBehaviour script)
            {
                if (isDebug) Log.D($"{nameof(script)}:{script.name}, active:{script.enabled}", script, debugColor, debugPrio, 6);
                _enabledConditions.RemoveAll(x => x == null);
                _enabledConditions.Add(script);
            }

            public void SetSceneUnloadCondition(SceneType sceneType)
            {
                if (isDebug) Log.D($"{nameof(sceneType)}:{sceneType}, current:{_manager._currentSceneType}", debugColor, debugPrio, 6);
                _unloadSceneCondition = sceneType;
            }

            public QAction<T> LoadResource(bool sync)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);

                if (!_isLoadCalled)
                {
                    _isLoadCalled = true;
                    _onComplete = new QAction<T>(true);
                    _loadHandle = _assetReference.LoadAssetAsync();
                    _loadHandle.Completed += OnComplete;
                    if (sync)
                        _loadHandle.WaitForCompletion();
                }

                return _onComplete;
            }
            
            public bool IsReleaseRequired()
            {
                if (_unloadSceneCondition != SceneType.None && (_manager._currentSceneType & _unloadSceneCondition) != 0)
                    return false;

                for (var index = 0; index < _activeInHierarchyGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeInHierarchyGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeInHierarchyGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeInHierarchy)
                        return false;
                }

                for (var index = 0; index < _activeSelfGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeSelfGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeSelfGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeSelf)
                        return false;
                }

                for (var index = 0; index < _enabledConditions.Count; index++)
                {
                    var script = _enabledConditions[index];
                    if (script == null)
                    {
                        _enabledConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (script.enabled)
                        return false;
                }

                return true;
            }

            private void OnComplete(AsyncOperationHandle<T> handle)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);
                _loadHandle.Completed -= OnComplete;

                OnCompleteBody(handle);
            }

            private void OnCompleteBody(AsyncOperationHandle<T> handle)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Log.E($"Error while trying to load icon reference: {_assetReference}");
                    return;
                }

                // If condition game object was destroyed or disabled, or same to script, or scene was changed, we should release resource
                if (IsReleaseRequired())
                {
                    if (isDebug) Log.D($"Auto released: {_assetReference.Asset.name}", 
#if UNITY_EDITOR
                        _assetReference.editorAsset,
#endif
                        debugColor, debugPrio, 7);
                    _manager.ReleaseScriptableObject(_assetReference, true);
                    return;
                }

                if (isDebug) Log.D($"Result is ready: {handle.Result.name}", debugColor, debugPrio, 7);
                _onComplete.Invoke(handle.Result);
            }
        }

        private sealed class PrefabReferenceData
        {
            private AsyncOperationHandle<GameObject> _loadHandle;
            private List<GameObject> _activeSelfGameObjectConditions = new List<GameObject>();
            private List<GameObject> _activeInHierarchyGameObjectConditions = new List<GameObject>();
            private List<MonoBehaviour> _enabledConditions = new List<MonoBehaviour>();
            private SceneType _unloadSceneCondition = SceneType.None;
            private AssetReferenceGameObject _prefabReference;
            private AddressablesManager _manager;
            private QAction<GameObject> _onComplete;
            private bool _isLoadCalled;
            public AssetReferenceGameObject Asset => _prefabReference;
            
            public PrefabReferenceData(AssetReferenceGameObject prefabReference, AddressablesManager manager)
            {
                _prefabReference = prefabReference;
                _manager = manager;
            }
            
            public void AddGameObjectCondition(GameObject gameObject, bool activeSelfCondition, bool activeInHierarchyCondition)
            {
                if (isDebug) Log.D($"{nameof(gameObject)}:{gameObject.name}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObject, debugColor, debugPrio, 6);

#if UNITY_EDITOR
                if (activeSelfCondition && !gameObject.activeSelf) Log.W("Passing game object condition which was already inactive self");
                if (activeInHierarchyCondition && !gameObject.activeInHierarchy) Log.W("Passing game object condition which was already inactive in hierarchy");
#endif

                if (activeSelfCondition)
                {
                    _activeSelfGameObjectConditions.RemoveAll(x => x == null);
                    _activeSelfGameObjectConditions.Add(gameObject);
                }

                if (activeInHierarchyCondition)
                {
                    _activeInHierarchyGameObjectConditions.RemoveAll(x => x == null);
                    _activeInHierarchyGameObjectConditions.Add(gameObject);
                }
            }
            
            public void AddEnabledCondition(MonoBehaviour script)
            {
                if (isDebug) Log.D($"{nameof(script)}:{script.name}, active:{script.enabled}", script, debugColor, debugPrio, 6);
                _enabledConditions.RemoveAll(x => x == null);
                _enabledConditions.Add(script);
            }

            public void SetSceneUnloadCondition(SceneType sceneType)
            {
                if (isDebug) Log.D($"{nameof(sceneType)}:{sceneType}, current:{_manager._currentSceneType}", debugColor, debugPrio, 6);
                _unloadSceneCondition = sceneType;
            }
            
            public QAction<GameObject> LoadResource(bool sync)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);

                if (!_isLoadCalled)
                {
                    _isLoadCalled = true;
                    _onComplete = new QAction<GameObject>(true);
                    _loadHandle = _prefabReference.LoadAssetAsync();
                    _loadHandle.Completed += OnComplete;
                    if (sync)
                        _loadHandle.WaitForCompletion();
                }

                return _onComplete;
            }

            public bool IsReleaseRequired()
            {
                if (_unloadSceneCondition != SceneType.None && (_manager._currentSceneType & _unloadSceneCondition) != 0)
                    return false;

                for (var index = 0; index < _activeInHierarchyGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeInHierarchyGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeInHierarchyGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeInHierarchy)
                        return false;
                }

                for (var index = 0; index < _activeSelfGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeSelfGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeSelfGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeSelf)
                        return false;
                }

                for (var index = 0; index < _enabledConditions.Count; index++)
                {
                    var script = _enabledConditions[index];
                    if (script == null)
                    {
                        _enabledConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (script.enabled)
                        return false;
                }

                return true;
            }
            
            private void OnComplete(AsyncOperationHandle<GameObject> handle)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);
                _loadHandle.Completed -= OnComplete;

                OnCompleteBody(handle);
            }

            private void OnCompleteBody(AsyncOperationHandle<GameObject> handle)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Log.E($"Error while trying to load prefab reference: {_prefabReference}");
                    return;
                }

                // If condition game object was destroyed or disabled, or same to script, or scene was changed, we should release resource
                if (IsReleaseRequired())
                {
                    if (isDebug) Log.D($"Auto released: {_prefabReference.Asset.name}", 
#if UNITY_EDITOR
                        _prefabReference.editorAsset,
#endif
                        debugColor, debugPrio, 7);
                    _manager.ReleasePrefab(this, true);
                    return;
                }

                if (isDebug) Log.D("Result is ready", debugColor, debugPrio, 7);
                _onComplete.Invoke(handle.Result);
            }
        }
        
        private sealed class TextureReferenceData
        {
            private AsyncOperationHandle<Texture> _loadHandle;
            private List<GameObject> _activeSelfGameObjectConditions = new List<GameObject>();
            private List<GameObject> _activeInHierarchyGameObjectConditions = new List<GameObject>();
            private List<MonoBehaviour> _enabledConditions = new List<MonoBehaviour>();
            private SceneType _unloadSceneCondition = SceneType.None;
            private AssetReferenceTexture _textureReference;
            private AddressablesManager _manager;
            private QAction<Texture> _onComplete;
            private bool _isLoadCalled;

            public TextureReferenceData(AssetReferenceTexture textureReference, AddressablesManager manager)
            {
                _textureReference = textureReference;
                _manager = manager;
            }

            public void AddGameObjectCondition(GameObject gameObject, bool activeSelfCondition, bool activeInHierarchyCondition)
            {
                if (isDebug) Log.D($"{nameof(gameObject)}:{gameObject.name}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObject, debugColor, debugPrio, 6);

#if UNITY_EDITOR
                if (activeSelfCondition && !gameObject.activeSelf) Log.W("Passing game object condition which was already inactive self");
                if (activeInHierarchyCondition && !gameObject.activeInHierarchy) Log.W("Passing game object condition which was already inactive in hierarchy");
#endif

                if (activeSelfCondition)
                {
                    _activeSelfGameObjectConditions.RemoveAll(x => x == null);
                    _activeSelfGameObjectConditions.Add(gameObject);
                }

                if (activeInHierarchyCondition)
                {
                    _activeInHierarchyGameObjectConditions.RemoveAll(x => x == null);
                    _activeInHierarchyGameObjectConditions.Add(gameObject);
                }
            }

            public void AddEnabledCondition(MonoBehaviour script)
            {
                if (isDebug) Log.D($"{nameof(script)}:{script.name}, active:{script.enabled}", script, debugColor, debugPrio, 6);
                _enabledConditions.RemoveAll(x => x == null);
                _enabledConditions.Add(script);
            }

            public void SetSceneUnloadCondition(SceneType sceneType)
            {
                if (isDebug) Log.D($"{nameof(sceneType)}:{sceneType}, current:{_manager._currentSceneType}", debugColor, debugPrio, 6);
                _unloadSceneCondition = sceneType;
            }

            public QAction<Texture> LoadResource(bool sync)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);

                if (!_isLoadCalled)
                {
                    _isLoadCalled = true;
                    _onComplete = new QAction<Texture>(true);
                    _loadHandle = _textureReference.LoadAssetAsync();
                    _loadHandle.Completed += OnComplete;
                    if (sync)
                        _loadHandle.WaitForCompletion();
                }

                return _onComplete;
            }
            
            public bool IsReleaseRequired()
            {
                if (_unloadSceneCondition != SceneType.None && (_manager._currentSceneType & _unloadSceneCondition) != 0)
                    return false;

                for (var index = 0; index < _activeInHierarchyGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeInHierarchyGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeInHierarchyGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeInHierarchy)
                        return false;
                }

                for (var index = 0; index < _activeSelfGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeSelfGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeSelfGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeSelf)
                        return false;
                }

                for (var index = 0; index < _enabledConditions.Count; index++)
                {
                    var script = _enabledConditions[index];
                    if (script == null)
                    {
                        _enabledConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (script.enabled)
                        return false;
                }

                return true;
            }

            private void OnComplete(AsyncOperationHandle<Texture> handle)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);
                _loadHandle.Completed -= OnComplete;

                OnCompleteBody(handle);
            }

            private void OnCompleteBody(AsyncOperationHandle<Texture> handle)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Log.E($"Error while trying to load icon reference: {_textureReference}");
                    return;
                }

                // If condition game object was destroyed or disabled, or same to script, or scene was changed, we should release resource
                if (IsReleaseRequired())
                {
                    if (isDebug) Log.D($"Auto released: {_textureReference.Asset.name}", 
#if UNITY_EDITOR
                        _textureReference.editorAsset,
#endif
                        debugColor, debugPrio, 7);
                    _manager.ReleaseTexture(_textureReference, true);
                    return;
                }

                if (isDebug) Log.D("Result is ready", debugColor, debugPrio, 7);
                _onComplete.Invoke(handle.Result);
            }
        }
        
        private sealed class SpriteReferenceData
        {
            private AsyncOperationHandle<Sprite> _loadHandle;
            private List<GameObject> _activeSelfGameObjectConditions = new List<GameObject>();
            private List<GameObject> _activeInHierarchyGameObjectConditions = new List<GameObject>();
            private List<MonoBehaviour> _enabledConditions = new List<MonoBehaviour>();
            private SceneType _unloadSceneCondition = SceneType.None;
            private AssetReferenceSprite _spriteReference;
            private AddressablesManager _manager;
            private QAction<Sprite> _onComplete;
            private bool _isLoadCalled;

            public SpriteReferenceData(AssetReferenceSprite spriteReference, AddressablesManager manager)
            {
                _spriteReference = spriteReference;
                _manager = manager;
            }

            public void AddGameObjectCondition(GameObject gameObject, bool activeSelfCondition, bool activeInHierarchyCondition)
            {
                if (isDebug) Log.D($"{nameof(gameObject)}:{gameObject.name}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObject, debugColor, debugPrio, 6);

#if UNITY_EDITOR
                if (activeSelfCondition && !gameObject.activeSelf) Log.W("Passing game object condition which was already inactive self");
                if (activeInHierarchyCondition && !gameObject.activeInHierarchy) Log.W("Passing game object condition which was already inactive in hierarchy");
#endif

                if (activeSelfCondition)
                {
                    _activeSelfGameObjectConditions.RemoveAll(x => x == null);
                    _activeSelfGameObjectConditions.Add(gameObject);
                }

                if (activeInHierarchyCondition)
                {
                    _activeInHierarchyGameObjectConditions.RemoveAll(x => x == null);
                    _activeInHierarchyGameObjectConditions.Add(gameObject);
                }
            }

            public void AddEnabledCondition(MonoBehaviour script)
            {
                if (isDebug) Log.D($"{nameof(script)}:{script.name}, active:{script.enabled}", script, debugColor, debugPrio, 6);
                _enabledConditions.RemoveAll(x => x == null);
                _enabledConditions.Add(script);
            }

            public void SetSceneUnloadCondition(SceneType sceneType)
            {
                if (isDebug) Log.D($"{nameof(sceneType)}:{sceneType}, current:{_manager._currentSceneType}", debugColor, debugPrio, 6);
                _unloadSceneCondition = sceneType;
            }

            public QAction<Sprite> LoadResource(bool sync)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);

                if (!_isLoadCalled)
                {
                    _isLoadCalled = true;
                    _onComplete = new QAction<Sprite>(true);
                    _loadHandle = _spriteReference.LoadAssetAsync();
                    _loadHandle.Completed += OnComplete;
                    if (sync)
                    {
                        // Bugfix: check for addressables deadlock limitations https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/SynchronousAddressables.html
                        if (SceneManagerUtil.IsLoadingScene)
                            throw new Exception("Trying to use addressables WaitForCompletion while scene loading is in progress, it can cause deadlock!");
                        _loadHandle.WaitForCompletion();
                    }
                }

                return _onComplete;
            }
            
            public bool IsReleaseRequired()
            {
                if (_unloadSceneCondition != SceneType.None && (_manager._currentSceneType & _unloadSceneCondition) != 0)
                    return false;

                for (var index = 0; index < _activeInHierarchyGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeInHierarchyGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeInHierarchyGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeInHierarchy)
                        return false;
                }

                for (var index = 0; index < _activeSelfGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeSelfGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeSelfGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeSelf)
                        return false;
                }

                for (var index = 0; index < _enabledConditions.Count; index++)
                {
                    var script = _enabledConditions[index];
                    if (script == null)
                    {
                        _enabledConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (script.enabled)
                        return false;
                }

                return true;
            }

            private void OnComplete(AsyncOperationHandle<Sprite> handle)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);
                _loadHandle.Completed -= OnComplete;

                OnCompleteBody(handle);
            }

            private void OnCompleteBody(AsyncOperationHandle<Sprite> handle)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Log.E($"Error while trying to load icon reference: {_spriteReference}");
                    return;
                }

                // If condition game object was destroyed or disabled, or same to script, or scene was changed, we should release resource
                if (IsReleaseRequired())
                {
                    if (isDebug) Log.D($"Auto released: {_spriteReference.Asset.name}", 
#if UNITY_EDITOR
                        _spriteReference.editorAsset,
#endif
                        debugColor, debugPrio, 7);
                    _manager.ReleaseSprite(_spriteReference, true);
                    return;
                }

                if (isDebug) Log.D("Result is ready", debugColor, debugPrio, 7);
                _onComplete.Invoke(handle.Result);
            }
        }
        
        private sealed class AnimationClipReferenceData
        {
            private AsyncOperationHandle<AnimationClip> _loadHandle;
            private List<GameObject> _activeSelfGameObjectConditions = new List<GameObject>();
            private List<GameObject> _activeInHierarchyGameObjectConditions = new List<GameObject>();
            private List<MonoBehaviour> _enabledConditions = new List<MonoBehaviour>();
            private SceneType _unloadSceneCondition = SceneType.None;
            private AssetReferenceT<AnimationClip> _animationClipReference;
            private AddressablesManager _manager;
            private QAction<AnimationClip> _onComplete;
            private bool _isLoadCalled;

            public AnimationClipReferenceData(AssetReferenceT<AnimationClip> animationClipReference, AddressablesManager manager)
            {
                _animationClipReference = animationClipReference;
                _manager = manager;
            }

            public void AddGameObjectCondition(GameObject gameObject, bool activeSelfCondition, bool activeInHierarchyCondition)
            {
                if (isDebug) Log.D($"{nameof(gameObject)}:{gameObject.name}, {nameof(activeSelfCondition)}:{activeSelfCondition}, {nameof(activeInHierarchyCondition)}:{activeInHierarchyCondition}", gameObject, debugColor, debugPrio, 6);

#if UNITY_EDITOR
                if (activeSelfCondition && !gameObject.activeSelf) Log.W("Passing game object condition which was already inactive self");
                if (activeInHierarchyCondition && !gameObject.activeInHierarchy) Log.W("Passing game object condition which was already inactive in hierarchy");
#endif

                if (activeSelfCondition)
                {
                    _activeSelfGameObjectConditions.RemoveAll(x => x == null);
                    _activeSelfGameObjectConditions.Add(gameObject);
                }

                if (activeInHierarchyCondition)
                {
                    _activeInHierarchyGameObjectConditions.RemoveAll(x => x == null);
                    _activeInHierarchyGameObjectConditions.Add(gameObject);
                }
            }

            public void AddEnabledCondition(MonoBehaviour script)
            {
                if (isDebug) Log.D($"{nameof(script)}:{script.name}, active:{script.enabled}", script, debugColor, debugPrio, 6);
                _enabledConditions.RemoveAll(x => x == null);
                _enabledConditions.Add(script);
            }

            public void SetSceneUnloadCondition(SceneType sceneType)
            {
                if (isDebug) Log.D($"{nameof(sceneType)}:{sceneType}, current:{_manager._currentSceneType}", debugColor, debugPrio, 6);
                _unloadSceneCondition = sceneType;
            }

            public QAction<AnimationClip> LoadResource(bool sync)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);

                if (!_isLoadCalled)
                {
                    _isLoadCalled = true;
                    _onComplete = new QAction<AnimationClip>(true);
                    _loadHandle = _animationClipReference.LoadAssetAsync();
                    _loadHandle.Completed += OnComplete;
                    if (sync)
                        _loadHandle.WaitForCompletion();
                }

                return _onComplete;
            }
            
            public bool IsReleaseRequired()
            {
                if (_unloadSceneCondition != SceneType.None && (_manager._currentSceneType & _unloadSceneCondition) != 0)
                    return false;

                for (var index = 0; index < _activeInHierarchyGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeInHierarchyGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeInHierarchyGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeInHierarchy)
                        return false;
                }

                for (var index = 0; index < _activeSelfGameObjectConditions.Count; index++)
                {
                    var gameObject = _activeSelfGameObjectConditions[index];
                    if (gameObject == null)
                    {
                        _activeSelfGameObjectConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (gameObject.activeSelf)
                        return false;
                }

                for (var index = 0; index < _enabledConditions.Count; index++)
                {
                    var script = _enabledConditions[index];
                    if (script == null)
                    {
                        _enabledConditions.RemoveAt(index);
                        index--;
                        continue;
                    }
                    
                    if (script.enabled)
                        return false;
                }

                return true;
            }

            private void OnComplete(AsyncOperationHandle<AnimationClip> handle)
            {
                if (isDebug) Log.D($"{nameof(_isLoadCalled)}:{_isLoadCalled}", debugColor, debugPrio, 7);
                _loadHandle.Completed -= OnComplete;

                OnCompleteBody(handle);
            }

            private void OnCompleteBody(AsyncOperationHandle<AnimationClip> handle)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Log.E($"Error while trying to load icon reference: {_animationClipReference}");
                    return;
                }

                // If condition game object was destroyed or disabled, or same to script, or scene was changed, we should release resource
                if (IsReleaseRequired())
                {
                    if (isDebug) Log.D($"Auto released: {_animationClipReference.Asset.name}", 
#if UNITY_EDITOR
                        _animationClipReference.editorAsset,
#endif
                        debugColor, debugPrio, 7);
                    _manager.ReleaseAnimationClip(_animationClipReference, true);
                    return;
                }

                if (isDebug) Log.D("Result is ready", debugColor, debugPrio, 7);
                _onComplete.Invoke(handle.Result);
            }
        }
    }
}
