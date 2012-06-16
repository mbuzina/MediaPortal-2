using System.Windows;
using System.Windows.Input;

namespace MediaPortal.InstallerUI.ViewModels
{
  /// <summary>
  /// ViewModel that represents the "Do you really want to cancel the installation?"-Dialog.
  /// </summary>
  public class CancelPageViewModel : BaseViewModel
  {
    #region ICommands

    /// <summary>
    /// Yes, the user wants to cancel the installation.
    /// </summary>
    public ICommand CancelCommand { get; private set; }
    /// <summary>
    /// No, the user want to continue the installation.
    /// </summary>
    public ICommand ContinueCommand { get; private set; }

    #endregion

    public CancelPageViewModel()
    {
      this.CancelCommand = new RelayCommand(param => MediaPortalBA.View.Close());
      this.ContinueCommand = new RelayCommand(param => MediaPortalBA.View.GoToPreviousScreen());
    }
  }
}