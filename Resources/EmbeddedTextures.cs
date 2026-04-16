using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ModUtils.Resources
{
    public static class EmbeddedTextures
    {
        private static readonly IDictionary<string, Texture2D> SharedTextures = new Dictionary<string, Texture2D>();

        public static Texture2D GetTexture(Assembly assembly, string resourcePrefix, string fileName)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (string.IsNullOrEmpty(resourcePrefix))
                throw new ArgumentException("Resource prefix is required.", "resourcePrefix");

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name is required.", "fileName");

            string cacheKey = assembly.FullName + "|" + resourcePrefix + "|" + fileName;
            Texture2D texture;
            if (SharedTextures.TryGetValue(cacheKey, out texture) && texture != null)
                return texture;

            return SharedTextures[cacheKey] = LoadTexture(assembly, resourcePrefix, fileName);
        }

        private static Texture2D LoadTexture(Assembly assembly, string resourcePrefix, string fileName)
        {
            string resourceName = resourcePrefix + "." + fileName;
            string error = "Failed to load embedded texture: " + resourceName;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException(error);

                using (BinaryReader reader = new BinaryReader(stream))
                {
                    byte[] data = reader.ReadBytes((int)stream.Length);
                    Texture2D texture = new Texture2D(2, 2);
                    if (!texture.LoadImage(data))
                        throw new InvalidOperationException(error);

                    texture.wrapMode = TextureWrapMode.Clamp;
                    return texture;
                }
            }
        }
    }
}
