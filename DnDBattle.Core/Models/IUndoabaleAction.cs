namespace DnDBattle.Services.UI
{
    public interface IUndoabaleAction
    {
        void Do();
        void Undo();
        string Description { get; }
    }
}
