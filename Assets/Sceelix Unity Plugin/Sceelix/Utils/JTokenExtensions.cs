using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Sceelix.Utils
{
    public static class JTokenExtensions
    {
        public static T ToTypeOrDefault<T>(this JToken token)
        {
            if (token != null)
                return token.ToObject<T>();

            return default(T);
        }

        public static Vector2 ToVector2(this JToken jToken)
        {
            var coordinates = jToken.ToString().Split(',');

            return new Vector3(Convert.ToSingle(coordinates[0], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[1], CultureInfo.InvariantCulture));
        }

        public static Vector3 ToVector3(this JToken jToken)
        {
            var coordinates = jToken.ToString().Split(',');

            return new Vector3(Convert.ToSingle(coordinates[0], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[1], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[2], CultureInfo.InvariantCulture));
        }

        public static Vector4 ToVector4(this JToken jToken)
        {
            var coordinates = jToken.ToString().Split(',');

            return new Vector4(Convert.ToSingle(coordinates[0], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[1], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[2], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[3], CultureInfo.InvariantCulture));
        }

        public static Color ToColor(this JToken jToken)
        {
            var coordinates = jToken.ToString().Split(',');

            return new Color(Convert.ToSingle(coordinates[0], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[1], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[2], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[3], CultureInfo.InvariantCulture));
        }


        public static Quaternion ToQuaternion(this JToken jToken)
        {
            var coordinates = jToken.ToString().Split(',');

            return Quaternion.Euler(Convert.ToSingle(coordinates[0], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[1], CultureInfo.InvariantCulture), Convert.ToSingle(coordinates[2], CultureInfo.InvariantCulture));
        }

        public static T ToEnum<T>(this JToken jToken)
        {
            String enumString = jToken.ToObject<String>();
            return (T)Enum.Parse(typeof(T), enumString);
        }

        //private static int coiso = 0;
        public static Texture2D ToTexture(this JToken jToken, bool setAsNormal = false)
        {
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, true);// {alphaIsTransparency = true}

            byte[] bytes = jToken.ToObject<byte[]>();
            //
            //File.WriteAllBytes(@"C:\Users\pedro\Desktop\Coiso" + coiso++ + ".png",bytes);

            texture2D.LoadImage(bytes);

            if (setAsNormal)
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

            return texture2D;
        }
        
    }
}
