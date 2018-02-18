using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Sceelix.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Sceelix.Editor
{
    public class AssetReferenceManager
    {
        public static void CleanupAndUpdate(String assetFolder)
        {
            var assetReferencesFile = Path.Combine(assetFolder, "AssetReferencesFile.asset");

            //if there is a file, load its references
            var assetRefs = File.Exists(assetReferencesFile) ? JsonSerialization.FromFile<Dictionary<string, List<string>>>(assetReferencesFile) : new Dictionary<string, List<string>>();

            //go over existing scene files
            var sceneGuids = AssetDatabase.FindAssets("t: Scene");
            foreach (string sceneGuid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

                //let's update only the loaded scenes
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.isLoaded)
                {
                    
                    var rootGameObjects = scene.GetRootGameObjects();

                    List<String> assets = GetAssetList(rootGameObjects, assetFolder).ToList();
                    //List<string> assets = rootGameObjects.SelectMany(gameObject => gameObject.GetComponentsInChildren<SceelixObjectComponent>().SelectMany(component => component.Assets)).Distinct().ToList();

                    //first, add the assets that may be missing.
                    foreach (string asset in assets)
                    {
                        List<string> sceneList;
                        if (!assetRefs.TryGetValue(asset, out sceneList))
                            assetRefs.Add(asset, sceneList = new List<string>());

                        if (!sceneList.Contains(sceneGuid))
                            sceneList.Add(sceneGuid);
                    }

                    //next, see if any if the references has dissapeared and can be deleted
                    foreach (KeyValuePair<string, List<string>> keyValuePair in assetRefs.ToList())
                    {
                        if (!assets.Contains(keyValuePair.Key))
                            keyValuePair.Value.Remove(sceneGuid);
                    }
                }
            }

            //next, see if any if the references has dissapeared and can be deleted
            foreach (KeyValuePair<string, List<string>> keyValuePair in assetRefs.ToList())
            {
                //if the list is empty, remove the asset reference and delete the asset
                if (!keyValuePair.Value.Any())
                {
                    assetRefs.Remove(keyValuePair.Key);

                    AssetDatabase.DeleteAsset(keyValuePair.Key);
                }
            }

            JsonSerialization.ToFile(assetReferencesFile, assetRefs);
        }




        private static IEnumerable<string> GetAssetList(IEnumerable<GameObject> gameObjects, String assetFolder)
        {
            //EditorUtility.CollectDependencies(gameObjects);

            foreach (UnityEngine.Object obj in EditorUtility.CollectDependencies(gameObjects.ToArray()))
            {
                if (obj is Texture || obj is Material || obj is Mesh)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (path.StartsWith(assetFolder))
                        yield return path;
                }
            }
        }
    }

}
