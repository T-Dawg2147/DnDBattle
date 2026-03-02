namespace DnDBattle.Core.Interfaces;

public interface INavigationService
{
    void NavigateTo(string viewKey);
    void NavigateBack();
    bool CanNavigateBack { get; }
}
