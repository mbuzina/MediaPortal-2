using System;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class InstallCustomizePageViewModel : BaseViewModel
  {
    #region Members

    private string _clientInstallDir;
    private string _serverInstallDir;

    private string _clientDataDir;
    private string _serverDataDir;

    #endregion

    #region Properties

    public string ClientInstallDir
    {
      get { return this._clientInstallDir; }

      set
      {
        if (this._clientInstallDir != value)
        {
          this._clientInstallDir = value;
          base.OnPropertyChanged("ClientInstallDir");
        }
      }
    }

    public string ServerInstallDir
    {
      get { return this._serverInstallDir; }

      set
      {
        if (this._serverInstallDir != value)
        {
          this._serverInstallDir = value;
          base.OnPropertyChanged("ServerInstallDir");
        }
      }
    }

    public string ClientDataDir
    {
      get { return this._clientDataDir; }

      set
      {
        if (this._clientDataDir != value)
        {
          this._clientDataDir = value;
          base.OnPropertyChanged("ClientDataDir");
        }
      }
    }

    public string ServerDataDir
    {
      get { return this._serverDataDir; }

      set
      {
        if (this._serverDataDir != value)
        {
          this._serverDataDir = value;
          base.OnPropertyChanged("ServerDataDir");
        }
      }
    }

    #endregion

    #region ICommands

    public ICommand BackCommand { get; private set; }

    public ICommand BrowseClientInstallDirCommand { get; private set; }
    public ICommand BrowseServerInstallDirCommand { get; private set; }

    public ICommand BrowseClientDataDirCommand { get; private set; }
    public ICommand BrowseServerDataDirCommand { get; private set; }

    #endregion

    public InstallCustomizePageViewModel()
    {
      this.BackCommand = new RelayCommand(param => this.DoBackCommand());

      this.BrowseClientInstallDirCommand = new RelayCommand(param => this.BrowseClientInstallDir());
      this.BrowseServerInstallDirCommand = new RelayCommand(param => this.BrowseServerInstallDir());

      this.BrowseClientDataDirCommand = new RelayCommand(param => this.BrowseClientDataDir());
      this.BrowseServerDataDirCommand = new RelayCommand(param => this.BrowseServerDataDir());
    }
    
    #region Implementation

    private void DoBackCommand()
    {
      MessageBox.Show("DoBackCommand");
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
