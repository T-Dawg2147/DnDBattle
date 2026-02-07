using DnDBattle.Services.UI;

namespace DnDBattle.Tests.Services.UI
{
    public class UndoManagerTests
    {
        // Test implementation of IUndoabaleAction
        private class TestAction : IUndoabaleAction
        {
            public int DoCount { get; private set; }
            public int UndoCount { get; private set; }
            public string Description { get; }

            public TestAction(string description = "Test")
            {
                Description = description;
            }

            public void Do() => DoCount++;
            public void Undo() => UndoCount++;
        }

        public UndoManagerTests()
        {
            // Clear state before each test since UndoManager is static
            UndoManager.Clear();
        }

        [Fact]
        public void Initially_CannotUndoOrRedo()
        {
            Assert.False(UndoManager.CanUndo);
            Assert.False(UndoManager.CanRedo);
        }

        [Fact]
        public void Record_PerformNow_ExecutesAction()
        {
            var action = new TestAction();
            UndoManager.Record(action, performNow: true);
            Assert.Equal(1, action.DoCount);
        }

        [Fact]
        public void Record_PerformNowFalse_DoesNotExecute()
        {
            var action = new TestAction();
            UndoManager.Record(action, performNow: false);
            Assert.Equal(0, action.DoCount);
        }

        [Fact]
        public void Record_EnablesUndo()
        {
            UndoManager.Record(new TestAction(), performNow: false);
            Assert.True(UndoManager.CanUndo);
        }

        [Fact]
        public void Record_ClearsRedoStack()
        {
            UndoManager.Record(new TestAction(), performNow: false);
            UndoManager.Undo();
            Assert.True(UndoManager.CanRedo);
            UndoManager.Record(new TestAction(), performNow: false);
            Assert.False(UndoManager.CanRedo);
        }

        [Fact]
        public void Undo_CallsUndoOnAction()
        {
            var action = new TestAction();
            UndoManager.Record(action, performNow: false);
            UndoManager.Undo();
            Assert.Equal(1, action.UndoCount);
        }

        [Fact]
        public void Undo_EnablesRedo()
        {
            UndoManager.Record(new TestAction(), performNow: false);
            UndoManager.Undo();
            Assert.True(UndoManager.CanRedo);
            Assert.False(UndoManager.CanUndo);
        }

        [Fact]
        public void Redo_CallsDoOnAction()
        {
            var action = new TestAction();
            UndoManager.Record(action, performNow: true);
            UndoManager.Undo();
            UndoManager.Redo();
            Assert.Equal(2, action.DoCount); // Once from Record, once from Redo
        }

        [Fact]
        public void Redo_MovesBackToUndo()
        {
            UndoManager.Record(new TestAction(), performNow: false);
            UndoManager.Undo();
            UndoManager.Redo();
            Assert.True(UndoManager.CanUndo);
            Assert.False(UndoManager.CanRedo);
        }

        [Fact]
        public void Undo_EmptyStack_DoesNotThrow()
        {
            UndoManager.Undo(); // Should not throw
        }

        [Fact]
        public void Redo_EmptyStack_DoesNotThrow()
        {
            UndoManager.Redo(); // Should not throw
        }

        [Fact]
        public void Clear_ResetsAllStacks()
        {
            UndoManager.Record(new TestAction(), performNow: false);
            UndoManager.Undo();
            UndoManager.Clear();
            Assert.False(UndoManager.CanUndo);
            Assert.False(UndoManager.CanRedo);
        }

        [Fact]
        public void StateChanged_FiresOnRecord()
        {
            int count = 0;
            UndoManager.StateChanged += (_, _) => count++;
            UndoManager.Record(new TestAction(), performNow: false);
            UndoManager.StateChanged -= (_, _) => count++;
            Assert.True(count > 0);
        }

        [Fact]
        public void Record_NullAction_DoesNothing()
        {
            UndoManager.Record(null!, performNow: true);
            Assert.False(UndoManager.CanUndo);
        }

        [Fact]
        public void MultipleUndoRedo_WorksCorrectly()
        {
            var a1 = new TestAction("Action 1");
            var a2 = new TestAction("Action 2");
            var a3 = new TestAction("Action 3");

            UndoManager.Record(a1, performNow: false);
            UndoManager.Record(a2, performNow: false);
            UndoManager.Record(a3, performNow: false);

            UndoManager.Undo(); // Undo a3
            Assert.Equal(1, a3.UndoCount);
            UndoManager.Undo(); // Undo a2
            Assert.Equal(1, a2.UndoCount);

            UndoManager.Redo(); // Redo a2
            Assert.Equal(1, a2.DoCount);
        }
    }
}
