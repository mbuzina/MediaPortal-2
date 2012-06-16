using System.Windows;
using System.Windows.Input;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class CancelPageViewModel : BaseViewModel
  {
    #region ICommands

    public ICommand CancelCommand { get; private set; }
    public ICommand ContinueCommand { get; private set; }

    #endregion

    public CancelPageViewModel()
    {
      this.CancelCommand = new RelayCommand(param => MediaPortalBA.View.Close());
      this.ContinueCommand = new RelayCommand(param => MediaPortalBA.View.GoToPreviousScreen());
    }
  }
}