using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class ViewModel : BaseViewModel
  {
    public ManualResetEvent DetectCompletedEvent = new ManualResetEvent(false);

    #region ICommands

    /// <summary>
    /// Command that closes the main window
    /// </summary>
    public ICommand CloseCommand { get; private set; }
    /// <summary>
    /// Command that minimizes the main window to taskbar.
    /// </summary>
    public ICommand MinimizeCommand { get; private set; }

    #endregion

    public ViewModel()
    {
      this.CloseCommand = new RelayCommand(param => MediaPortalBA.View.Close());
      this.MinimizeCommand = new RelayCommand(param => MediaPortalBA.View.Minimize());
    }

    #region Implementation

    #endregion

    public void Initialize()
    {
      MediaPortalBA.Model.Engine.Detect();
    }
  }
}