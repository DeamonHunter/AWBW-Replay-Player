using System.Collections.Generic;
using AWBWApp.Game.UI.Editor;
using osu.Framework.Localisation;

namespace AWBWApp.Game.Editor.History
{
    public class HistoryManager
    {
        public bool NeedsSave;

        private readonly List<IHistory> registeredStates = new List<IHistory>();
        private int currentIndex;

        public void RegisterHistory(IHistory history)
        {
            //Any history past the current index is no longer relevant
            if (currentIndex < registeredStates.Count)
                registeredStates.RemoveRange(currentIndex, registeredStates.Count - currentIndex);

            registeredStates.Add(history);
            currentIndex = registeredStates.Count;

            NeedsSave = true;
        }

        public void Undo(EditorScreen screen, EditorGameMap map)
        {
            if (currentIndex <= 0)
                return;

            NeedsSave = true;

            currentIndex--;
            registeredStates[currentIndex].Undo(map);
            screen.ShowMessage(LocalisableString.Format("Undo {0}", registeredStates[currentIndex].DisplayName));
        }

        public void Redo(EditorScreen screen, EditorGameMap map)
        {
            if (currentIndex >= registeredStates.Count)
                return;

            NeedsSave = true;

            registeredStates[currentIndex].Redo(map);
            screen.ShowMessage(LocalisableString.Format("Redo {0}", registeredStates[currentIndex].DisplayName));
            currentIndex++;
        }
    }
}
