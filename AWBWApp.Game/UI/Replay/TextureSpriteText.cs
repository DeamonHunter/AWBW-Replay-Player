using System.Threading.Tasks;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Text;

namespace AWBWApp.Game.UI.Replay
{
    public partial class TextureSpriteText : SpriteText
    {
        private TextureGlyphLookupStore glyphStore;
        private string glyphPath;

        public TextureSpriteText(string path)
        {
            glyphPath = path;
            Shadow = false;
            UseFullGlyphHeight = false;
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            glyphStore = new TextureGlyphLookupStore(store, glyphPath);
        }

        protected override TextBuilder CreateTextBuilder(ITexturedGlyphLookupStore store) => base.CreateTextBuilder(glyphStore);

        private class TextureGlyphLookupStore : ITexturedGlyphLookupStore
        {
            private NearestNeighbourTextureStore textureStore;
            private string path;

            public TextureGlyphLookupStore(NearestNeighbourTextureStore textureStore, string path)
            {
                this.textureStore = textureStore;
                this.path = path;
            }

            public ITexturedCharacterGlyph Get(string fontName, char character)
            {
                //Todo: Some characters can't be handled like this

                Texture texture;

                switch (character)
                {
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                        texture = textureStore.Get($"{path}/{character}-Upper"); //These characters will conflict with the lower case letters on windows
                        break;

                    case ' ':
                        texture = textureStore.Get($"{path}/Space");
                        break;

                    default:
                        texture = textureStore.Get($"{path}/{character}");
                        break;
                }

                if (texture == null)
                    return null;

                return new TexturedCharacterGlyph(new CharacterGlyph(character, 0, 0, texture.Width, texture.Height, null), texture, 1f / texture.ScaleAdjust);
            }

            public Task<ITexturedCharacterGlyph> GetAsync(string fontName, char character) => Task.Run(() => Get(fontName, character));
        }
    }
}
