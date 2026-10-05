namespace ISI.IDE.ViewModels;

public enum SaveChoice { Save, Discard, Cancel }

/// <summary>UI services the view-models need; implemented by the main window (keeps VMs UI-free).</summary>
public interface IDialogService
{
    Task<string?> PickFolderAsync(string title);
    Task<string?> PickFileAsync(string title);
    Task<string?> SaveFileAsync(string title, string suggestedName);
    Task<string?> PromptAsync(string title, string message, string initial = "");
    Task<SaveChoice> AskSaveAsync(string fileName);
    Task ShowSettingsAsync();
    Task ShowAboutAsync();
    Task ShowPackagesAsync(string pythonPath);
    void CloseMainWindow();
}
