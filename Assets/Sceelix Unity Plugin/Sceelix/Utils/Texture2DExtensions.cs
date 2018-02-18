using UnityEngine;

namespace Assets.Sceelix.Utils
{
    public static class Texture2DExtensions
    {
        /// <summary>
        /// Converts the given texture to a normal texture.
        /// </summary>
        /// <param name="texture2D"></param>
        /// <returns></returns>
        public static Texture2D ToNormalTexture(this Texture2D texture2D)
        {
            var normalTexture = new Texture2D(texture2D.width, texture2D.height, TextureFormat.ARGB32, true);
            Color32[] colours = texture2D.GetPixels32();
            for (int i = 0; i < colours.Length; i++)
            {
                Color32 c = colours[i];
                c.a = c.r;
                c.g = (byte)(255 - c.g);
                c.r = c.b = 0;
                colours[i] = c;
            }
            normalTexture.SetPixels32(colours);
            normalTexture.Apply();

            return normalTexture;
        }


        /// <summary>
        /// Converts the given texture to a mippmapped version.
        /// </summary>
        /// <param name="texture2D"></param>
        /// <returns></returns>
        public static Texture2D ToMipmappedTexture(this Texture2D texture2D)
        {
            var mipmappedTexture = new Texture2D(texture2D.width, texture2D.height, texture2D.format, true);
            mipmappedTexture.SetPixels(texture2D.GetPixels());

            mipmappedTexture.Apply();

            return mipmappedTexture;
        }
    }
}
