using System;
using System.Collections.Generic;
using System.IO;
using Assets.Sceelix.Annotations;
using Assets.Sceelix.Contexts;
using Assets.Sceelix.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Sceelix.Processors
{
    public class DefaultEntityManager
    {
        private static Dictionary<String, ComponentProcessorAttribute> _componentProcessorAttributes;

        public static void InitializeProcessors()
        {
            _componentProcessorAttributes = FunctionProcessorAttribute.GetFunctionsWithAttribute<ComponentProcessorAttribute>();
            DefaultComponentManager.InitializeProcessors();
        }


        [EntityProcessor("UnityEntity")]
        public static IEnumerable<GameObject> ProcessGameObject(IGenerationContext context, JToken entityToken)
        {
            //first of all, let's see if we are loading a prefab
            var prefabPath = entityToken["Prefab"].ToTypeOrDefault<String>();
            var scaleMode = entityToken["ScaleMode"].ToTypeOrDefault<String>();

            GameObject gameObject;

            //if a prefab instruction is passed, load it
            if (!String.IsNullOrEmpty(prefabPath))
            {
                if (!prefabPath.StartsWith("Assets/"))
                    prefabPath = "Assets/" + prefabPath;

                //make sure the extension is set
                prefabPath = Path.ChangeExtension(prefabPath, ".prefab");

                gameObject = context.InstantiatePrefab(prefabPath);

                if (gameObject == null)
                {
                    gameObject = new GameObject();
                    Debug.LogWarning(String.Format("Could not create instance of prefab {0}. Please verify that it exists in the requested location.", prefabPath));
                    prefabPath = String.Empty;
                }
            }
            else
            {
                gameObject = new GameObject();
            }

            gameObject.name = entityToken["Name"].ToObject<String>();
            gameObject.isStatic = entityToken["Static"].ToTypeOrDefault<bool>();
            gameObject.SetActive(entityToken["Enabled"].ToTypeOrDefault<bool>());


            var tag = entityToken["Tag"].ToTypeOrDefault<String>();
            if (!String.IsNullOrEmpty(tag))
                context.AddTag(gameObject, tag);

            var layer = entityToken["Layer"].ToTypeOrDefault<String>();
            if (!String.IsNullOrEmpty(layer))
            {
                var layerValue = LayerMask.NameToLayer(layer);

                //unfortunately we can't create the layer programmatically, so
                if (layerValue < 0)
                    throw new ArgumentException("Layer '" + layer + "' is not defined. It must be created manually in Unity first.");

                gameObject.layer = layerValue;
            }
            

            gameObject.transform.position = entityToken["Position"].ToVector3();
            gameObject.transform.rotation *= Quaternion.LookRotation(entityToken["ForwardVector"].ToVector3(), entityToken["UpVector"].ToVector3());

            //if this is a prefab, we need to make its size and position match the same size
            if (!String.IsNullOrEmpty(prefabPath))
            {
                //try to get the bounds of the object
                var objectBounds = GetObjectBounds(gameObject);
                if (objectBounds.HasValue)
                {
                    var objectSize = objectBounds.Value.size;
                    var intendedSize = entityToken["Size"].ToVector3();

                    if (scaleMode == "Stretch To Fill")
                    {
                        var scale = new Vector3(1/ objectSize.x, 1/ objectSize.y, 1/ objectSize.z);

                        gameObject.transform.localScale = Vector3.Scale(intendedSize, scale);
                    }
                    else if (scaleMode == "Scale To Fit")
                    {
                        var scale = new Vector3(intendedSize.x / objectSize.x, intendedSize.y / objectSize.y, intendedSize.z / objectSize.z);
                        var minCoordinate = Math.Min(Math.Min(scale.x, scale.y), scale.z);
                        var newScale = new Vector3(minCoordinate,minCoordinate,minCoordinate);

                        gameObject.transform.localScale = newScale;
                    }

                    gameObject.transform.Translate(Vector3.Scale(-objectBounds.Value.min, gameObject.transform.localScale));

                }
            }
            else
            {
                var intendedSize = entityToken["Scale"].ToVector3();
                gameObject.transform.localScale = intendedSize;
            }


            //now, iterate over the components
            //and look for the matching component processor
            foreach (JToken jToken in entityToken["Components"].Children())
            {
                ComponentProcessorAttribute componentProcessorAttribute;
                
                if (_componentProcessorAttributes.TryGetValue(jToken["ComponentType"].ToObject<String>(), out componentProcessorAttribute))
                    componentProcessorAttribute.Invoke(context, gameObject, jToken);
                else
                {
                    Debug.LogWarning(String.Format("There is no defined processor for component type {0}.", jToken["ComponentType"]));
                }
            }

            yield return gameObject;
        }

        private static Bounds? GetObjectBounds(GameObject gameObject)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                return meshFilter.sharedMesh.bounds;
            }
            var terrain = gameObject.GetComponent<TerrainCollider>();
            if (terrain != null)
            {
                return terrain.bounds;
            }
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            return null;
        }


        /// <summary>
        /// Because Unity doesn't handle a Scale of 0 very well, we need this function.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float GetOneIfZero(float value)
        {
            return Math.Abs(value) < Single.Epsilon ? 1 : value;
        }



        [EntityProcessor("MeshEntity")]
        public static IEnumerable<GameObject> ProcessMeshEntity(IGenerationContext context, JToken entityToken)
        {
            GameObject gameObject = new GameObject("Mesh Entity");
            gameObject.isStatic = true;

            //use the processors already defined for the component
            DefaultComponentManager.MeshFilterProcessor(context, gameObject, entityToken["MeshFilter"]);
            DefaultComponentManager.MeshRendererProcessor(context, gameObject, entityToken["MeshRenderer"]);

            yield return gameObject;
        }


        [EntityProcessor("BillboardEntity")]
        public static IEnumerable<GameObject> ProcessBillboardEntity(IGenerationContext context, JToken entityToken)
        {
            GameObject gameObject = new GameObject("Billboard Entity");

            gameObject.transform.position = entityToken["Position"].ToVector3();
            gameObject.transform.rotation *= Quaternion.LookRotation(entityToken["ForwardVector"].ToVector3(), entityToken["UpVector"].ToVector3());
            gameObject.transform.localScale = entityToken["Scale"].ToVector3();

            //use the processors already defined for the component
            DefaultComponentManager.BillboardProcessor(context, gameObject, entityToken);

            yield return gameObject;
        }


        [EntityProcessor("MeshInstanceEntity")]
        public static IEnumerable<GameObject> ProcessMeshInstanceEntity(IGenerationContext context, JToken entityToken)
        {
            GameObject gameObject = new GameObject("Mesh Instance Entity");

            gameObject.transform.position = entityToken["Position"].ToVector3();
            gameObject.transform.rotation *= Quaternion.LookRotation(entityToken["ForwardVector"].ToVector3(), entityToken["UpVector"].ToVector3());
            gameObject.transform.localScale = entityToken["Scale"].ToVector3();

            //use the processors already defined for the component
            DefaultComponentManager.MeshFilterProcessor(context, gameObject, entityToken["MeshFilter"]);
            DefaultComponentManager.MeshRendererProcessor(context, gameObject, entityToken["MeshRenderer"]);

            yield return gameObject;
        }


        [EntityProcessor("SurfaceEntity")]
        public static IEnumerable<GameObject> ProcessSurfaceEntity(IGenerationContext context, JToken entityToken)
        {
            GameObject gameObject = new GameObject("Surface Entity");

            gameObject.transform.position = entityToken["Position"].ToVector3();

            //use the processors already defined for the component
            DefaultComponentManager.TerrainProcessor(context, gameObject, entityToken);

            yield return gameObject;
        }

    }
}
