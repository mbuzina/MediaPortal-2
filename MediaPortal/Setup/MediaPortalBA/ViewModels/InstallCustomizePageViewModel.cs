using System;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class InstallCustomizePageViewModel : BaseViewModel
  {
    #region Members

    private static string _clientInstallDir;
    private static string _serverInstallDir;

    private static string _clientDataDir;
    private static string _serverDataDir;

    #endregion

    #region Properties

    /// <summary>
    /// Property that holds the path, where the client will be installed to.
    /// </summary>
    public string ClientInstallDir
    {
      get { return InstallCustomizePageViewModel._clientInstallDir; }

      set
      {
        if (InstallCustomizePageViewModel._clientInstallDir != value)
        {
          InstallCustomizePageViewModel._clientInstallDir = value;
          base.OnPropertyChanged("ClientInstallDir");
        }
      }
    }

    /// <summary>
    /// Property that holds the path, where the server will be installed to.
    /// </summary>
    public string ServerInstallDir
    {
      get { return InstallCustomizePageViewModel._serverInstallDir; }

      set
      {
        if (InstallCustomizePageViewModel._serverInstallDir != value)
        {
          InstallCustomizePageViewModel._serverInstallDir = value;
          base.OnPropertyChanged("ServerInstallDir");
        }
      }
    }

    /// <summary>
    /// Property that holds the path, client will save it's settings.
    /// </summary>
    public string ClientDataDir
    {
      get { return InstallCustomizePageViewModel._clientDataDir; }

      set
      {
        if (InstallCustomizePageViewModel._clientDataDir != value)
        {
          InstallCustomizePageViewModel._clientDataDir = value;
          base.OnPropertyChanged("ClientDataDir");
        }
      }
    }

    /// <summary>
    /// Property that holds the path, server will save it's settings.
    /// </summary>
    public string ServerDataDir
    {
      get { return InstallCustomizePageViewModel._serverDataDir; }

      set
      {
        if (InstallCustomizePageViewModel._serverDataDir != value)
        {
          InstallCustomizePageViewModel._serverDataDir = value;
          base.OnPropertyChanged("ServerDataDir");
        }
      }
    }

    #endregion

    #region ICommands

    /// <summary>
    /// Command to go back to the Welcome page
    /// </summary>
    public ICommand BackCommand { get; private set; }

    /// <summary>
    /// Command opens a dialog to browse for the folder and change <see cref="ClientInstallDir"/>
    /// </summary>
    public ICommand BrowseClientInstallDirCommand { get; private set; }
    /// <summary>
    /// Command opens a dialog to browse for the folder and change <see cref="ServerInstallDir"/>
    /// </summary>
    public ICommand BrowseServerInstallDirCommand { get; private set; }

    /// <summary>
    /// Command opens a dialog to browse for the folder and change <see cref="ClientDataDir"/>
    /// </summary>
    public ICommand BrowseClientDataDirCommand { get; private set; }
    /// <summary>
    /// Command opens a dialog to browse for the folder and change <see cref="ServerDataDir"/>
    /// </summary>
    public ICommand BrowseServerDataDirCommand { get; private set; }

    #endregion

    public InstallCustomizePageViewModel()
    {
      this.ClientInstallDir = MediaPortalBA.Model.Engine.StringVariables[Constants.VariableNames.ClientInstallDir];
      this.ServerInstallDir = MediaPortalBA.Model.Engine.StringVariables[Constants.VariableNames.ServerInstallDir];
      this.ClientDataDir = MediaPortalBA.Model.Engine.StringVariables[Constants.VariableNames.ClientDataDir];
      this.ServerDataDir = MediaPortalBA.Model.Engine.StringVariables[Constants.VariableNames.ServerDataDir];

      this.BackCommand = new RelayCommand(param => this.DoBackCommand());

      this.BrowseClientInstallDirCommand = new RelayCommand(param => this.BrowseClientInstallDir());
      this.BrowseServerInstallDirCommand = new RelayCommand(param => this.BrowseServerInstallDir());
      this.BrowseClientDataDirCommand = new RelayCommand(param => this.BrowseClientDataDir());
      this.BrowseServerDataDirCommand = new RelayCommand(param => this.BrowseServerDataDir());
    }
    
    #region Implementation

    private void DoBackCommand()
    {
      MediaPortalBA.View.GotoScreen(Screens.InstallWelcomePage);
    }

    private void BrowseClientInstallDir()
    {
      using (FolderBrowserDialog fbd = new FolderBrowserDialog())
      {
        fbd.ShowNewFolderButton = true;
        fbd.Description = "LOC_Browse the install dir for client";
        fbd.RootFolder = Environment.SpecialFolder.MyComputer;

        if (!string.IsNullOrEmpty(this.ClientInstallDir))
          fbd.SelectedPath = this.ClientInstallDir;

        if (fbd.ShowDialog() != DialogResult.OK)
          return;

        this.ClientInstallDir = fbd.SelectedPath;
      }
    }

    private void BrowseServerInstallDir()
    {
      using (FolderBrowserDialog fbd = new FolderBrowserDialog())
      {
        fbd.ShowNewFolderButton = true;
        fbd.Description = "LOC_Browse the install dir for server";
        fbd.RootFolder = Environment.SpecialFolder.MyComputer;

        if (!string.IsNullOrEmpty(this.ServerInstallDir))
          fbd.SelectedPath = this.ServerInstallDir;

        if (fbd.ShowDialog() != DialogResult.OK)
          return;

        this.ServerInstallDir = fbd.SelectedPath;
      }
    }

    private void BrowseClientDataDir()
    {
      using (FolderBrowserDialog fbd = new FolderBrowserDialog())
      {
        fbd.ShowNewFolderButton = true;
        fbd.Description = "LOC_Browse the data dir for client";
        fbd.RootFolder = Environment.SpecialFolder.MyComputer;

        if (!string.IsNullOrEmpty(this.ClientDataDir))
          fbd.SelectedPath = this.ClientDataDir;

        if (fbd.ShowDialog() != DialogResult.OK)
          return;

        this.ClientDataDir = fbd.SelectedPath;
      }
    }

    private void BrowseServerDataDir()
    {
      using (FolderBrowserDialog fbd = new FolderBrowserDialog())
      {
        fbd.ShowNewFolderButton = true;
        fbd.Description = "LOC_Browse the data dir for server";
        fbd.RootFolder = Environment.SpecialFolder.MyComputer;

        if (!string.IsNullOrEmpty(this.ServerDataDir))
          fbd.SelectedPath = this.ServerDataDir;

        if (fbd.ShowDialog() != DialogResult.OK)
          return;

        this.ServerDataDir = fbd.SelectedPath;
      }
    }

    #endregion
  }
}
