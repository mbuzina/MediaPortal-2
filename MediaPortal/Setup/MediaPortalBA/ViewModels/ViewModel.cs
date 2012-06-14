using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class ViewModel : BaseViewModel
  {
    public ManualResetEvent DetectCompletedEvent = new ManualResetEvent(false);

    #region ICommands

    public ICommand CloseCommand { get; private set; }
    public ICommand MinimizeCommand { get; private set; }
    public ICommand CancelPromptCommand { get; private set; }

    #endregion

    public ViewModel()
    {
      this.CloseCommand = new RelayCommand(param => CancelPrompt());
      this.MinimizeCommand = new RelayCommand(param => MediaPortalBA.View.Minimize());
      this.CancelPromptCommand = new RelayCommand(param => CancelPrompt());
    }

    #region Implementation

    #endregion

    public void Initialize()
    {
      MediaPortalBA.Model.Engine.Detect();
    }

    private void CancelPrompt()
    {
      MessageBoxResult result = MessageBox.Show("Do you really want to cancel?", "CancelPrompt", MessageBoxButton.YesNo);

      if (result == MessageBoxResult.Yes)
      {
        MediaPortalBA.View.Close();
      }
    }
  }
}