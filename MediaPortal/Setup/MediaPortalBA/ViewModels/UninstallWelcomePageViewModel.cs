using System.Windows;
using System.Windows.Input;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class UninstallWelcomePageViewModel : BaseViewModel
  {
    #region Members

    private static bool _removeSettings;
    private static bool _uninstallDokan;

    #endregion

    #region Properties

    public bool RemoveSettings
    {
      get { return UninstallWelcomePageViewModel._removeSettings; }
      set
      {
        UninstallWelcomePageViewModel._removeSettings = value;
        base.OnPropertyChanged("RemoveSettings");
      }
    }

    public bool UninstallDokan
    {
      get { return UninstallWelcomePageViewModel._uninstallDokan; }
      set
      {
        UninstallWelcomePageViewModel._uninstallDokan = value;
        base.OnPropertyChanged("UninstallDokan");
      }
    }

    #endregion

    #region ICommands

    public ICommand UninstallCommand { get; private set; }

    #endregion

    public UninstallWelcomePageViewModel()
    {
      this.UninstallCommand = new RelayCommand(param => this.Uninstall());
    }

    #region Implementation

    private void Uninstall()
    {
      MediaPortalBA.Model.Engine.Plan(LaunchAction.Uninstall);
    }

    #endregion
  }
}