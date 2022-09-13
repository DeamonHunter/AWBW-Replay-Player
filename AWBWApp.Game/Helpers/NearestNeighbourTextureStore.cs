using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;

namespace AWBWApp.Game.Helpers
{
    public class NearestNeighbourTextureStore : TextureStore
    {
        public NearestNeighbourTextureStore(IRenderer renderer, IResourceStore<TextureUpload> store = null)
            : base(renderer, store, filteringMode: TextureFilteringMode.Nearest)
        {
        }
    }
}
