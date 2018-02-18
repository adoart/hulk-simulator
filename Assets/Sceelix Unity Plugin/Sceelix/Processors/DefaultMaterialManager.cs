using System;
using Assets.Sceelix.Annotations;
using Assets.Sceelix.Contexts;
using Assets.Sceelix.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Sceelix.Processors
{
    public class DefaultMaterialManager
    {
        public static Texture CreateOrGetTexture(IGenerationContext context, JToken textureToken, bool setAsNormal = false)
        {
            if(textureToken == null)
                return null;

            var name = textureToken["Name"].ToObject<String>();

            return context.CreateOrGetAssetOrResource(name + ".asset", () => textureToken["Content"].ToTexture(setAsNormal));
        }


        [MaterialProcessor("RemoteMaterial")]
        public static Material RemoteMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            var path = jtoken["Properties"]["Path"].ToObject<String>();

            if (!path.EndsWith(".mat"))
                path = path + ".mat";

            var remoteMaterial = context.GetExistingResource<Material>(path);
            if(remoteMaterial == null)
                Debug.LogWarning(String.Format("Could not find material with the path {0}.", path));

            return remoteMaterial;
        }


        [MaterialProcessor("ColorMaterial")]
        public static Material ColorMaterialMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            Material colorMaterial = new Material(Shader.Find("Standard"));
            colorMaterial.color = jtoken["Properties"]["DefaultColor"].ToColor();
            return colorMaterial;
        }


        [MaterialProcessor("SingleTextureMaterial")]
        public static Material SingleTextureMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            Material singletextureMaterial = new Material(Shader.Find("Standard"));
            
            singletextureMaterial.mainTexture = CreateOrGetTexture(context, jtoken["Properties"]["Texture"]);
            singletextureMaterial.SetFloat("_Glossiness", 0);
            singletextureMaterial.SetFloat("_Mode", 1);
            singletextureMaterial.DisableKeyword("_ALPHABLEND_ON");
            singletextureMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            singletextureMaterial.EnableKeyword("_ALPHATEST_ON");

            return singletextureMaterial;
        }


        [MaterialProcessor("TextureAndBumpMaterial")]
        public static Material TextureAndBumpMaterialMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            Material textureAndBump = new Material(Shader.Find("Standard"));

            textureAndBump.SetTexture("_MainTex", CreateOrGetTexture(context, jtoken["Properties"]["DiffuseTexture"]));
            textureAndBump.SetTexture("_MetallicGlossMap", CreateOrGetTexture(context, jtoken["Properties"]["SpecularTexture"]));
            textureAndBump.SetTexture("_BumpMap", CreateOrGetTexture(context, jtoken["Properties"]["NormalTexture"],true));
            textureAndBump.EnableKeyword("_NORMALMAP");

            return textureAndBump;
        }


        [MaterialProcessor("ParallaxOcclusionMaterial")]
        public static Material ParallaxOcclusionMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            //for some reason, looking for the StandardSpecular doesn't return anything...
            Material parallaxOcclusionMaterial = new Material(Shader.Find("Standard"));

            parallaxOcclusionMaterial.SetTexture("_MainTex", CreateOrGetTexture(context, jtoken["Properties"]["DiffuseTexture"]));
            parallaxOcclusionMaterial.SetTexture("_MetallicGlossMap", CreateOrGetTexture(context, jtoken["Properties"]["SpecularTexture"]));
            parallaxOcclusionMaterial.SetTexture("_BumpMap", CreateOrGetTexture(context, jtoken["Properties"]["NormalTexture"],true));
            parallaxOcclusionMaterial.SetTexture("_ParallaxMap", CreateOrGetTexture(context, jtoken["Properties"]["HeightTexture"]));
            parallaxOcclusionMaterial.EnableKeyword("_NORMALMAP");
            parallaxOcclusionMaterial.EnableKeyword("_PARALLAXMAP");

            return parallaxOcclusionMaterial;
        }


        [MaterialProcessor("TransparentMaterial")]
        public static Material TransparentMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            Material transparentMaterial = new Material(Shader.Find("Standard"));

            transparentMaterial.SetFloat("_Mode",3);
            transparentMaterial.SetTexture("_MainTex", CreateOrGetTexture(context, jtoken["Properties"]["Texture"]));
            transparentMaterial.color = new Color(1,1,1, jtoken["Properties"]["Transparency"].ToObject<float>());
            transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMaterial.SetInt("_ZWrite", 0);
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            transparentMaterial.DisableKeyword("_ALPHABLEND_ON");
            transparentMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMaterial.renderQueue = 3000;
            
            return transparentMaterial;
        }

        [MaterialProcessor("ImportedMaterial")]
        public static Material ImportedMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            Material importedMaterial = new Material(Shader.Find("Standard"));

            if(jtoken["Properties"]["ColorDiffuse"] != null)
                importedMaterial.color = jtoken["Properties"]["ColorDiffuse"].ToColor();

            if (jtoken["Properties"]["Shininess"] != null)
                importedMaterial.SetFloat("_Glossiness", jtoken["Properties"]["Shininess"].ToObject<float>());

            if (jtoken["Properties"]["Shininess"] != null)
                importedMaterial.SetFloat("_Glossiness", jtoken["Properties"]["Shininess"].ToObject<float>());

            if (jtoken["Properties"]["DiffuseTexture"] != null)
                importedMaterial.SetTexture("_MainTex", CreateOrGetTexture(context, jtoken["Properties"]["DiffuseTexture"]));

            if (jtoken["Properties"]["SpecularTexture"] != null)
                importedMaterial.SetTexture("_MetallicGlossMap", CreateOrGetTexture(context, jtoken["Properties"]["SpecularTexture"]));

            if (jtoken["Properties"]["NormalTexture"] != null)
                importedMaterial.SetTexture("_BumpMap", CreateOrGetTexture(context, jtoken["Properties"]["NormalTexture"],true));

            if (jtoken["Properties"]["HeightTexture"] != null)
                importedMaterial.SetTexture("_ParallaxMap", CreateOrGetTexture(context, jtoken["Properties"]["HeightTexture"]));
            

            return importedMaterial;
        }



        [MaterialProcessor("CustomMaterial")]
        public static Material CustomMaterialProcessor(IGenerationContext context, JToken jtoken)
        {
            var shaderName = jtoken["Shader"].ToObject<String>();

            Material customMaterial = new Material(Shader.Find(shaderName));

            
            foreach (JToken propertyToken in jtoken["Properties"].Children())
            {
                var propertyName = propertyToken["Name"].ToObject<String>();
                var propertyType = propertyToken["Type"].ToObject<String>();
                switch (propertyType)
                {
                    case "TextureSlot":
                        var textureType = propertyToken["Value"]["Type"].ToObject<String>();
                        bool isNormal = textureType == "Normal";
                        customMaterial.SetTexture(propertyName, CreateOrGetTexture(context, propertyToken["Value"], isNormal));
                        break;
                    case "Boolean":
                        var status = propertyToken["Value"].ToObject<bool>();
                        if(status)
                            customMaterial.EnableKeyword(propertyName);
                        else
                            customMaterial.DisableKeyword(propertyName);
                        break;
                    case "Color":
                        customMaterial.SetColor(propertyName, propertyToken["Value"].ToColor());
                        break;
                    case "Int32":
                        customMaterial.SetInt(propertyName, propertyToken["Value"].ToObject<int>());
                        break;
                    case "Single":
                        customMaterial.SetFloat(propertyName, propertyToken["Value"].ToObject<float>());
                        break;
                    case "Vector4":
                        customMaterial.SetVector(propertyName, propertyToken["Value"].ToVector4());
                        break;
                    case "String":
                        customMaterial.SetOverrideTag(propertyName, propertyToken["Value"].ToObject<String>());
                        break;
                }
            }

            return customMaterial;
        }
    }
}
