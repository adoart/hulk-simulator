using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Sceelix.Annotations;
using Assets.Sceelix.Components;
using Assets.Sceelix.Contexts;
using Assets.Sceelix.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Sceelix.Processors
{
    public class DefaultComponentManager
    {
        private static Dictionary<string, MaterialProcessorAttribute> _materialProcessorAttributes;
        

        public static void InitializeProcessors()
        {
            _materialProcessorAttributes = FunctionProcessorAttribute.GetFunctionsWithAttribute<MaterialProcessorAttribute>();
        }


        [ComponentProcessor("Billboard")]
        public static void BillboardProcessor(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            if(gameObject.GetComponent<BillboardComponent>() 
                || gameObject.GetComponent<MeshFilter>() != null
                || gameObject.GetComponent<MeshRenderer>() != null)
                return;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = BillboardComponent.GetMesh();

            var imageToken = jtoken["Image"];
            var name = imageToken["Name"].ToObject<String>();

            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = context.CreateOrGetAssetOrResource<Material>("Material_" + name + ".mat", delegate()
            {
                var billboardMaterial = new Material(Shader.Find("Standard"))
                {
                    mainTexture = context.CreateOrGetAssetOrResource(name + ".asset", () => imageToken["Content"].ToTexture())
                };

                billboardMaterial.SetFloat("_Glossiness",0);
                billboardMaterial.SetFloat("_Mode", 1);
                billboardMaterial.DisableKeyword("_ALPHABLEND_ON");
                billboardMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                billboardMaterial.EnableKeyword("_ALPHATEST_ON");

                return billboardMaterial;
            });
            
            //don't forget about the billboardcomponent!
            gameObject.AddComponent<BillboardComponent>();
            
        }



        [ComponentProcessor("MeshFilter")]
        public static void MeshFilterProcessor(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            //if a meshfilter already exists, don't overwrite it
            if (gameObject.GetComponent<MeshFilter>() != null)
                return;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

            if (meshFilter == null)
                return;

            var meshToken = jtoken["Mesh"];
            var meshName = meshToken["Name"].ToObject<String>();

            
            
            var newMesh = context.CreateOrGetAssetOrResource<Mesh>(meshName +".asset", () =>
            {
                var mesh = new Mesh();
                mesh.vertices = meshToken["Positions"].Children().Select(x => x.ToVector3()).ToArray();
                mesh.normals = meshToken["Normals"].Children().Select(x => x.ToVector3()).ToArray();
                mesh.colors = meshToken["Colors"].Children().Select(x => x.ToColor()).ToArray();
                mesh.uv = meshToken["UVs"].Children().Select(x => x.ToVector2()).ToArray();
                //mesh.uv2 = meshToken["UVs"].Children().Select(x => x.ToVector2()).ToArray();
                mesh.tangents = meshToken["Tangents"].Children().Select(x => x.ToVector4()).ToArray();
                
                var triangleSetList = meshToken["Triangles"].Children().ToList();
                
                mesh.subMeshCount = triangleSetList.Count;

                for (int index = 0; index < triangleSetList.Count; index++)
                {
                    int[] subList = triangleSetList[index].Children().Select(x => x.ToObject<int>()).ToArray();

                    mesh.SetTriangles(subList, index);
                }

                return mesh;
            });

            meshFilter.sharedMesh = newMesh;
        }




        [ComponentProcessor("MeshRenderer")]
        public static void MeshRendererProcessor(IGenerationContext context,GameObject gameObject, JToken jtoken)
        {
            //if a MeshRenderer already exists, don't overwrite it
            if (gameObject.GetComponent<MeshRenderer>() != null)
                return;

            //var sceelixObjectComponent = gameObject.GetComponent<SceelixObjectComponent>();

            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();

            //GenericData[] genericMaterials = genericGameComponent.Get<GenericData[]>("Materials");
            var materialTokens = jtoken["Materials"].Children().ToList();

            Material[] sharedMaterials = new Material[materialTokens.Count];
            for (int index = 0; index < materialTokens.Count; index++)
            {
                var materialToken = materialTokens[index];
                var materialName = materialToken["Name"].ToObject<String>();

                var material = context.CreateOrGetAssetOrResource<Material>(materialName + ".mat", delegate ()
                {
                    MaterialProcessorAttribute materialProcessorAttribute;

                    if (materialToken["Type"] == null)
                    {
                        Debug.LogWarning("Could not load material. It was expected to have been loaded before. This could have been caused by a failure in a previous load.");
                        return null;
                    }
                    
                    //if there is a type field, use it to find its processor
                    var materialType = materialToken["Type"].ToObject<String>();
                    if (_materialProcessorAttributes.TryGetValue(materialType, out materialProcessorAttribute))
                        return materialProcessorAttribute.Invoke(context, materialToken);


                    Debug.LogWarning(String.Format("There is no defined processor for material type {0}.", materialType));
                    return null;

                });

                sharedMaterials[index] = material;
            }

            renderer.sharedMaterials = sharedMaterials;
        }


        [ComponentProcessor("Terrain")]
        public static void TerrainProcessor(IGenerationContext context, GameObject gameObject,JToken jtoken)
        {
            //if a Terrain already exists, don't overwrite it
            if (gameObject.GetComponent<Terrain>() != null)
                return;

            var heights = jtoken["Heights"].ToObject<float[,]>();
            var resolution = jtoken["Resolution"].ToObject<int>();
            var sizes = jtoken["Size"].ToVector3();


            //initialize the terrain data instance and set height data
            //unfortunately unity terrain maps have to be square and the sizes must be powers of 2
            TerrainData terrainData = new TerrainData();

            terrainData.heightmapResolution = resolution;
            terrainData.alphamapResolution = resolution;
            terrainData.size = sizes;
            terrainData.SetHeights(0, 0, heights);

            var materialToken = jtoken["Material"];
            if (materialToken != null)
            {
                var defaultTexture = Texture2D.whiteTexture.ToMipmappedTexture();
                List<SplatPrototype> splatPrototypes = new List<SplatPrototype>();
                
                var splatmap = materialToken["Splatmap"].ToObject<float[,,]>();
                var tileSize = materialToken["TileSize"].ToVector2();
                foreach (JToken textureToken in materialToken["Textures"].Children())
                {
                    var name = textureToken["Name"].ToObject<String>();

                    splatPrototypes.Add(new SplatPrototype()
                    {
                        texture = String.IsNullOrEmpty(name) ? defaultTexture : context.CreateOrGetAssetOrResource(name, () => textureToken["Content"].ToTexture()),
                        tileSize = tileSize
                    });
                }

                terrainData.splatPrototypes = splatPrototypes.ToArray();

                terrainData.SetAlphamaps(0, 0, splatmap);
            }


            //finally, create the terrain components
            Terrain terrain = gameObject.AddComponent<Terrain>();
            TerrainCollider collider = gameObject.AddComponent<TerrainCollider>();

            terrain.terrainData = terrainData;
            collider.terrainData = terrainData;
        }



        [ComponentProcessor("RigidBody")]
        public static void RigidBodyProcessor(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();

            //if a rigidbody already exists, this will be null, so don't overwrite it
            if (rigidbody == null)
                return;

            rigidbody.mass = jtoken["Properties"]["Mass"].ToObject<float>();
            rigidbody.drag = jtoken["Properties"]["Drag"].ToObject<float>();
            rigidbody.angularDrag = jtoken["Properties"]["Angular Drag"].ToObject<float>();
            rigidbody.useGravity = jtoken["Properties"]["Use Gravity"].ToObject<bool>();
            rigidbody.isKinematic = jtoken["Properties"]["Is Kinematic"].ToObject<bool>();
            rigidbody.interpolation = jtoken["Properties"]["Interpolate"].ToEnum<RigidbodyInterpolation>();
            rigidbody.collisionDetectionMode = jtoken["Properties"]["Collision Detection"].ToEnum<CollisionDetectionMode>();
        }



        [ComponentProcessor("Mesh Collider")]
        public static void MeshColliderProcessor(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();

            //if a meshCollider already exists, this will be null, so don't overwrite it
            if (meshCollider == null)
                return;

            meshCollider.sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            meshCollider.convex = jtoken["Properties"]["IsConvex"].ToObject<bool>();
            meshCollider.isTrigger = jtoken["Properties"]["IsTrigger"].ToObject<bool>();
        }



        [ComponentProcessor("Light")]
        public static void LightProcessor(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            Light light = gameObject.AddComponent<Light>();

            light.type = jtoken["Properties"]["LightType"].ToEnum<LightType>();
            light.range = jtoken["Properties"]["Range"].ToObject<float>();
            light.color = jtoken["Properties"]["Color"].ToColor();
            light.intensity = jtoken["Properties"]["Intensity"].ToObject<float>();
            light.bounceIntensity = jtoken["Properties"]["Bounce Intensity"].ToObject<float>();
            light.renderMode = jtoken["Properties"]["Render Mode"].ToEnum<LightRenderMode>();
            light.shadows = jtoken["Properties"]["Shadow Type"].ToEnum<LightShadows>();
            
        }


        [ComponentProcessor("Camera")]
        public static void CameraProcessor(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            Camera camera = gameObject.AddComponent<Camera>();

            camera.clearFlags = jtoken["Properties"]["Clear Flags"].ToEnum<CameraClearFlags>();
            camera.backgroundColor = jtoken["Properties"]["Background"].ToColor();
            camera.fieldOfView = jtoken["Properties"]["Field of View"].ToObject<float>();
            camera.nearClipPlane = jtoken["Properties"]["Clipping Plane (Near)"].ToObject<float>();
            camera.farClipPlane = jtoken["Properties"]["Clipping Plane (Far)"].ToObject<float>();
            camera.depth = jtoken["Properties"]["Depth"].ToObject<float>();
            camera.renderingPath = jtoken["Properties"]["Rendering Path"].ToEnum<RenderingPath>();
            camera.useOcclusionCulling = jtoken["Properties"]["Occlusion Culling"].ToObject<bool>();
            camera.hdr = jtoken["Properties"]["HDR"].ToObject<bool>();
        }



        [ComponentProcessor("Custom")]
        public static void CustomProcessor(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            var componentName = jtoken["ComponentName"].ToObject<String>();

            //if the optimized processor does not exist, simply go for the slower, but very generic Reflection approach
            var componentType = typeof(DefaultComponentManager).Assembly.GetType(componentName);

            if (componentType == null)
                Debug.LogWarning(String.Format("Component '{0}' is not defined.", componentName));
            else
            {
                Component customComponent = gameObject.AddComponent(componentType);

                var properties = jtoken["Properties"];

                foreach (var genericProperty in properties.Children())
                {
                    var propertyFieldName = genericProperty["Name"].ToObject<String>();
                    var propertyFieldType = Type.GetType(genericProperty["Type"].ToObject<String>());

                    //the indicated value can be field or property - try field first
                    FieldInfo fieldInfo = componentType.GetField(propertyFieldName);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(customComponent, genericProperty["Value"].ToObject(propertyFieldType));
                    }
                    else
                    {
                        //otherwise, try property and let the user know if it failed
                        PropertyInfo propertyInfo = componentType.GetProperty(propertyFieldName);
                        if (propertyInfo != null)
                            propertyInfo.SetValue(customComponent, genericProperty["Value"], null);
                        else
                            Debug.LogWarning(String.Format("Property/Field '{0}' for component '{1}' is not defined.", propertyFieldName, componentName));
                    }
                }
            }
        }
    }
}
