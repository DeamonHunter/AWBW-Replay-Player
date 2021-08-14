using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osuTK.Graphics.ES30;

namespace AWBWApp.Game.Helpers
{
    public class NearestNeighbourTextureStore : TextureStore
    {
        public NearestNeighbourTextureStore(IResourceStore<TextureUpload> store)
            : base(store, filteringMode: All.Nearest)
        {
        }
    }
}
