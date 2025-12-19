using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Services
{
    public static class UndoManager
    {
        private static readonly Stack<IUndoabaleAction> _undo = new Stack<IUndoabaleAction>();
        private static readonly Stack<IUndoabaleAction> _redo = new Stack<IUndoabaleAction>();
        public static event EventHandler StateChanged;

        public static int Limit { get; set; } = 200;

        public static bool CanUndo => _undo.Count > 0;
        public static bool CanRedo => _redo.Count > 0;

        public static void Record(IUndoabaleAction action, bool performNow = true)
        {
            if (action == null) return;
            if (performNow) action.Do();
            _undo.Push(action);

            while (_undo.Count > Limit) _undo.Pop();
            _redo.Clear();
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Undo()
        {
            if (_undo.Count == 0) return;
            var act = _undo.Pop();
            try { act.Undo(); }
            catch { }
            _redo.Push(act);
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Redo()
        {
            if (_redo.Count == 0) return;
            var act = _redo.Pop();
            try { act.Do(); }
            catch { }
            _undo.Push(act);
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Clear()
        {
            _undo.Clear();
            _redo.Clear();
            StateChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public interface IUndoabaleAction
    {
        void Do();
        void Undo();
        string Description { get; }
    }
}
