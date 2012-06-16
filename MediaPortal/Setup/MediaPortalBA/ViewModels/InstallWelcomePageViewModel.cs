using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class InstallWelcomePageViewModel : BaseViewModel
  {
    #region Members

    private static bool _acceptedLicense;

    #endregion

    #region Properties

    public bool AcceptedLicense
    {
      get { return InstallWelcomePageViewModel._acceptedLicense; }
      set
      {
        InstallWelcomePageViewModel._acceptedLicense = value;
        base.OnPropertyChanged("AcceptedLicense");
      }
    }

    #endregion

    #region ICommands

    public ICommand ViewLicenseCommand { get; private set; }
    public ICommand CustomizeInstallCommand { get; private set; }
    
    public ICommand InstallSingleSeatCommand { get; private set; }
    public ICommand InstallClientOnlyCommand { get; private set; }
    public ICommand InstallServerOnlyCommand { get; private set; }

    #endregion

    public InstallWelcomePageViewModel()
    {
      this.ViewLicenseCommand = new RelayCommand(param => this.ViewLicense());
      this.CustomizeInstallCommand = new RelayCommand(param => this.CustomizeInstall());
      this.InstallSingleSeatCommand = new RelayCommand(param => this.InstallSingleSeat());
      this.InstallClientOnlyCommand = new RelayCommand(param => this.InstallClientOnly());
      this.InstallServerOnlyCommand = new RelayCommand(param => this.InstallServerOnly());
    }

    #region Implementation

    private void ViewLicense()
    {
      string folder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      MediaPortalBA.LaunchUrl(System.IO.Path.Combine(folder, "License.htm"));
    }

    private void CustomizeInstall()
    {
      MediaPortalBA.View.GotoScreen(Screens.InstallCustomizePage);
    }

    private void InstallSingleSeat()
    {
      MediaPortalBA.Model.InstallMode = InstallMode.SingleSeat;
      MediaPortalBA.Model.Engine.Plan(LaunchAction.Install);
    }

    private void InstallClientOnly()
    {
      MediaPortalBA.Model.InstallMode = InstallMode.ClientOnly;
      MediaPortalBA.Model.Engine.Plan(LaunchAction.Install);
    }

    private void InstallServerOnly()
    {
      MediaPortalBA.Model.InstallMode = InstallMode.ServerOnly;
      MediaPortalBA.Model.Engine.Plan(LaunchAction.Install);
    }

    #endregion
  }
}