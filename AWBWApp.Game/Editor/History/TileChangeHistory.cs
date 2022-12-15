using System.Collections.Generic;
using AWBWApp.Game.UI.Editor;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Localisation;

namespace AWBWApp.Game.Editor.History
{
    public class TileChangeHistory : IHistory
    {
        public LocalisableString DisplayName => "Tile Change";

        public bool HasChanges => tileChanges.Count > 0;

        private readonly Dictionary<Vector2I, TileChange> tileChanges = new Dictionary<Vector2I, TileChange>();

        public void AddChange(Vector2I position, TileChange change)
        {
            if (tileChanges.TryGetValue(position, out var previous))
            {
                change.TileBefore = previous.TileBefore;
                change.AltBefore = previous.AltBefore;
            }

            tileChanges[position] = change;
        }

        public void Undo(EditorGameMap map)
        {
            foreach (var change in tileChanges)
                map.ChangeTile(change.Key, change.Value.TileBefore, change.Value.AltBefore, false);
        }

        public void Redo(EditorGameMap map)
        {
            foreach (var change in tileChanges)
                map.ChangeTile(change.Key, change.Value.TileAfter, change.Value.AltAfter, false);
        }
    }
}
