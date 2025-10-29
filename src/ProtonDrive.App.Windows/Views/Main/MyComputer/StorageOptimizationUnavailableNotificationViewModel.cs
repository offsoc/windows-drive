using ProtonDrive.App.Mapping;
using ProtonDrive.App.Windows.Dialogs;
using ProtonDrive.App.Windows.Toolkit.Converters;

namespace ProtonDrive.App.Windows.Views.Main.MyComputer;

internal sealed class StorageOptimizationUnavailableNotificationViewModel : ConfirmationDialogViewModelBase
{
    private static readonly string ContentText =
        Resources.Strings.Main_MyComputer_Folders_StorageOptimizationUnavailableNotification_Message_1 + Environment.NewLine + Environment.NewLine +
        Resources.Strings.Main_MyComputer_Folders_StorageOptimizationUnavailableNotification_Message_2;

    public StorageOptimizationUnavailableNotificationViewModel()
        : base(Resources.Strings.Main_MyComputer_Folders_StorageOptimizationUnavailableNotification_Title, message: ContentText)
    {
        IsCancelButtonVisible = false;
    }

    public void SetArguments(string folderName, StorageOptimizationErrorCode errorCode, string? conflictingProviderName)
    {
        var reason = string.Format(EnumToDisplayTextConverter.Convert(errorCode) ?? string.Empty, conflictingProviderName);

        Message = string.Format(ContentText, folderName, reason);
    }
}
