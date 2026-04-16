using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace ModUtils.UI
{
    public static class TextureAtlasBuilder
    {
        private const string UiShaderName = "UI/Default UI Shader";

        public static UITextureAtlas CreateAtlas(string atlasName, Texture2D texture, IDictionary<string, Rect> spritePixels)
        {
            if (string.IsNullOrEmpty(atlasName))
                throw new ArgumentException("Atlas name is required.", "atlasName");

            if (texture == null)
                throw new ArgumentNullException("texture");

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.name = atlasName;
            atlas.material = CreateAtlasMaterial(texture);
            AddSprites(atlas, texture, spritePixels);
            return atlas;
        }

        public static void AddSprites(UITextureAtlas atlas, Texture2D texture, IDictionary<string, Rect> spritePixels)
        {
            if (atlas == null)
                throw new ArgumentNullException("atlas");

            if (texture == null)
                throw new ArgumentNullException("texture");

            if (spritePixels == null)
                throw new ArgumentNullException("spritePixels");

            foreach (KeyValuePair<string, Rect> entry in spritePixels)
            {
                AddSprite(atlas, texture, entry);
            }
        }

        private static Material CreateAtlasMaterial(Texture2D texture)
        {
            Shader shader = Shader.Find(UiShaderName);
            if (shader == null)
                throw new InvalidOperationException("Cannot create atlas material. Shader not found: " + UiShaderName);

            Material material = new Material(shader)
            {
                mainTexture = texture
            };
            return material;
        }

        private static void AddSprite(UITextureAtlas atlas, Texture2D texture, KeyValuePair<string, Rect> entry)
        {
            string spriteName = entry.Key;
            Rect pixelRect = entry.Value;
            ValidateSpriteInput(texture, spriteName, pixelRect);

            UITextureAtlas.SpriteInfo info = new UITextureAtlas.SpriteInfo
            {
                name = spriteName,
                texture = texture,
                region = ToAtlasRegion(texture, pixelRect)
            };
            atlas.AddSprite(info);
        }

        private static void ValidateSpriteInput(Texture2D texture, string spriteName, Rect pixelRect)
        {
            if (string.IsNullOrEmpty(spriteName))
                throw new ArgumentException("Sprite name is required.", "spriteName");

            if (pixelRect.width <= 0f || pixelRect.height <= 0f)
                throw new ArgumentOutOfRangeException("pixelRect", "Sprite size must be positive.");

            if (pixelRect.x < 0f || pixelRect.y < 0f)
                throw new ArgumentOutOfRangeException("pixelRect", "Sprite position cannot be negative.");

            if (pixelRect.xMax > texture.width || pixelRect.yMax > texture.height)
                throw new ArgumentOutOfRangeException("pixelRect", "Sprite rectangle exceeds texture bounds.");
        }

        private static Rect ToAtlasRegion(Texture2D texture, Rect pixelRect)
        {
            float textureWidth = texture.width;
            float textureHeight = texture.height;

            return new Rect(
                pixelRect.x / textureWidth,
                pixelRect.y / textureHeight,
                pixelRect.width / textureWidth,
                pixelRect.height / textureHeight);
        }
    }
}
