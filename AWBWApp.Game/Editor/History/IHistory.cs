using AWBWApp.Game.UI.Editor;
using osu.Framework.Localisation;

namespace AWBWApp.Game.Editor.History
{
    public interface IHistory
    {
        LocalisableString DisplayName { get; }

        void Undo(EditorGameMap map);

        void Redo(EditorGameMap map);
    }
}
