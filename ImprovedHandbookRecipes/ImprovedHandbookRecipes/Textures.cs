using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImprovedHandbookRecipes;
public static class Textures {
    private static readonly List<Texture> all = new();
    private static ICoreClientAPI api;

    public static readonly Texture Wrench = new("wrench");

    public static void Load(ICoreClientAPI api) {
        Textures.api = api;
        foreach (Texture texture in all) {
            texture.Load();
        }
    }

    public class Texture {
        private readonly string path;
        private BitmapRef bitmap;
        private LoadedTexture texture;

        public Texture(string name) {
            path = $"improvedhandbookrecipes:textures/{name}.png";
            all.Add(this);
        }

        public void Load() {
            bitmap = api.Assets.Get(path).ToBitmap(api);
        }

        public LoadedTexture Tex {
            get {
                texture ??= new(api);
                if (texture.TextureId == 0) {
                    api.Render.LoadTexture(bitmap, ref texture);
                }
                return texture;
            }
        }

        public int Id 
            => Tex.TextureId;
    }
}
