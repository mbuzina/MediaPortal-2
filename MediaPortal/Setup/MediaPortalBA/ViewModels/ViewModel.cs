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

    public ICommand CloseCommand { get; private set; }
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