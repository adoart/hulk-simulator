using System;
using System.Collections.Generic;
using System.IO;
using Assets.Sceelix.Contexts;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Sceelix.Editor
{
    public class EditorGenerationContext : IGenerationContext
    {
        private readonly Dictionary<String, Object> _cachedResources = new Dictionary<string, Object>();

        public void ReportStart()
        {
            EditorUtility.DisplayProgressBar("Loading Sceelix Data", "Please wait...", 0);

            if (StorePhysicalAssets && !AssetDatabase.IsValidFolder(AssetsFolder))
                Directory.CreateDirectory(AssetsFolder);
        }


        public void ReportProgress(float percentage)
        {
            EditorUtility.DisplayProgressBar("Loading Sceelix Data", "Please wait...", percentage);
        }

        

        public void ReportEnd()
        {
            if(StorePhysicalAssets)
                AssetReferenceManager.CleanupAndUpdate(AssetsFolder);

            EditorUtility.ClearProgressBar();
        }



        public void ReportObjectCreation(GameObject sceneGameObject)
        {
            if (FrameResult)
            {
                var view = SceneView.lastActiveSceneView;
                if (view != null)
                {
                    Selection.activeGameObject = sceneGameObject;
                    view.FrameSelected();

                }
            }
        }



        public GameObject InstantiatePrefab(string prefabPath)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, (typeof(GameObject))));
        }



        public T GetExistingResource<T>(string assetPath) where T : Object
        {
            if (!assetPath.StartsWith("Assets/"))
                assetPath = "Assets/" + assetPath;

            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }



        public T CreateOrGetAssetOrResource<T>(string assetPath, Func<T> creationFunction) where T : Object
        {
            if (StorePhysicalAssets)
            {
                return CreateOrGetPhysicalAssetOrResource(assetPath, creationFunction);

            }

            Object asset;
            if (!_cachedResources.TryGetValue(assetPath, out asset))
            {
                asset = creationFunction.Invoke();

                if (asset != null)
                {
                    _cachedResources.Add(assetPath, asset);
                }
            }

            return (T)asset;
        }

        

        public T CreateOrGetPhysicalAssetOrResource<T>(string assetName, Func<T> creationFunction) where T : Object
        {
            var assetLocation = Path.Combine(AssetsFolder,assetName);

            if (!File.Exists(assetLocation))
            {
                //call the delegate that creates the asset
                T createdAsset = creationFunction.Invoke();

                //and store it to the disk
                AssetDatabase.CreateAsset(createdAsset, assetLocation);

                AssetDatabase.ImportAsset(assetLocation);
            }


            return AssetDatabase.LoadAssetAtPath<T>(assetLocation);
        }



        public void AddTag(GameObject gameObject, string tagName)
        {
            Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                SerializedObject so = new SerializedObject(asset[0]);
                SerializedProperty tags = so.FindProperty("tags");

                for (int i = 0; i < tags.arraySize; ++i)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                    {
                        gameObject.tag = tagName; // Tag already present, nothing to do.
                        return;
                    }
                }

                tags.InsertArrayElementAtIndex(0);
                tags.GetArrayElementAtIndex(0).stringValue = tagName;
                so.ApplyModifiedProperties();
                so.Update();
            }

            gameObject.tag = tagName;
        }

        

        public bool StorePhysicalAssets
        {
            get; set;
        }



        public bool RemoveOnRegeneration
        {
            get; set;
        }



        public string AssetsFolder
        {
            get; set;
        }



        public bool FrameResult
        {
            get; set;
        }
    }
}
