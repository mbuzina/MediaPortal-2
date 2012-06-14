//-------------------------------------------------------------------------------------------------
// <copyright file="InstallationViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
//    
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl1.0.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//    
//    You must not remove this notice, or any other, from this software.
// </copyright>
// 
// <summary>
// The model of the installation view.
// </summary>
//-------------------------------------------------------------------------------------------------

using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using System.IO;
using ErrorEventArgs = Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ErrorEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace MediaPortal.InstallerUI.ViewModels.other
{
  /// <summary>
  /// The states of the installation view model.
  /// </summary>
  public enum InstallationState
  {
    Initializing,
    DetectedAbsent,
    DetectedPresent,
    DetectedNewer,
    Applying,
    Applied,
    Failed,
  }

  /// <summary>
  /// The model of the installation view in WixBA.
  /// </summary>
  public class InstallationViewModel : PropertyNotifyBase
  {
    private RootViewModel root;

    private Dictionary<string, int> downloadRetries;
    private bool downgrade;
    private readonly string mpHomePageUrl = "http://www.team-mediaportal.com/";
    private readonly string mpNewsUrl = "http://forum.team-mediaportal.com/forums/general.529/";

    private bool planAttempted;
    private LaunchAction plannedAction;
    private IntPtr hwnd;

    private ICommand launchHomePageCommand;
    private ICommand launchNewsCommand;
    //private ICommand installCommand;
    private ICommand repairCommand;
    private ICommand uninstallCommand;
    private ICommand tryAgainCommand;

    private string message;
    private DateTime cachePackageStart;
    private DateTime executePackageStart;

    /// <summary>
    /// Creates a new model of the installation view.
    /// </summary>
    public InstallationViewModel(RootViewModel root)
    {
      this.root = root;

      this.ClientInstallDir =
        MpBootstrapperApplication.Model.Engine.StringVariables["ClientInstallDir"];
      this.ServerInstallDir =
        MpBootstrapperApplication.Model.Engine.StringVariables["ServerInstallDir"];

      this.downloadRetries = new Dictionary<string, int>();

      this.root.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(this.RootPropertyChanged);

      MpBootstrapperApplication.Model.Bootstrapper.DetectBegin += this.DetectBegin;
      MpBootstrapperApplication.Model.Bootstrapper.DetectRelatedBundle += this.DetectedRelatedBundle;
      MpBootstrapperApplication.Model.Bootstrapper.DetectPackageComplete += this.DetectedPackage;
      MpBootstrapperApplication.Model.Bootstrapper.DetectComplete += this.DetectComplete;
      MpBootstrapperApplication.Model.Bootstrapper.PlanPackageBegin += this.PlanPackageBegin;
      MpBootstrapperApplication.Model.Bootstrapper.PlanComplete += this.PlanComplete;
      MpBootstrapperApplication.Model.Bootstrapper.ApplyBegin += this.ApplyBegin;
      MpBootstrapperApplication.Model.Bootstrapper.CacheAcquireBegin += this.CacheAcquireBegin;
      MpBootstrapperApplication.Model.Bootstrapper.CacheAcquireComplete += this.CacheAcquireComplete;
      MpBootstrapperApplication.Model.Bootstrapper.ExecutePackageBegin += this.ExecutePackageBegin;
      MpBootstrapperApplication.Model.Bootstrapper.ExecutePackageComplete += this.ExecutePackageComplete;
      MpBootstrapperApplication.Model.Bootstrapper.Error += this.ExecuteError;
      MpBootstrapperApplication.Model.Bootstrapper.ResolveSource += this.ResolveSource;
      MpBootstrapperApplication.Model.Bootstrapper.ApplyComplete += this.ApplyComplete;
    }

    void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if ("State" == e.PropertyName)
      {
        base.OnPropertyChanged("Title");
        base.OnPropertyChanged("CompleteEnabled");
        base.OnPropertyChanged("ExitEnabled");
        base.OnPropertyChanged("RepairEnabled");
        base.OnPropertyChanged("InstallEnabled");
        base.OnPropertyChanged("TryAgainEnabled");
        base.OnPropertyChanged("UninstallEnabled");
      }
    }

    /// <summary>
    /// Gets the title for the application.
    /// </summary>
    public string Version
    {
      get { return String.Concat("v", MpBootstrapperApplication.Model.Version.ToString()); }
    }

    public string Message
    {
      get { return this.message; }

      set
      {
        if (this.message != value)
        {
          this.message = value;
          base.OnPropertyChanged("Message");
        }
      }
    }

    /// <summary>
    /// Gets and sets whether the view model considers this install to be a downgrade.
    /// </summary>
    public bool Downgrade
    {
      get
      {
        return this.downgrade;
      }

      set
      {
        if (this.downgrade != value)
        {
          this.downgrade = value;
          base.OnPropertyChanged("Downgrade");
        }
      }
    }

    #region Commands & Control locking

    public ICommand CloseCommand
    {
      get { return this.root.CloseCommand; }
    }

    #region from Wix

    public ICommand LaunchHomePageCommand
    {
      get
      {
        if (this.launchHomePageCommand == null)
        {
          this.launchHomePageCommand = new RelayCommand(param => MpBootstrapperApplication.LaunchUrl(this.mpHomePageUrl), param => true);
        }

        return this.launchHomePageCommand;
      }
    }

    public ICommand LaunchNewsCommand
    {
      get
      {
        if (this.launchNewsCommand == null)
        {
          this.launchNewsCommand = new RelayCommand(param => MpBootstrapperApplication.LaunchUrl(this.mpNewsUrl), param => true);
        }

        return this.launchNewsCommand;
      }
    }

    public bool ExitEnabled
    {
      get { return this.root.State != InstallationState.Applying; }
    }

    public ICommand RepairCommand
    {
      get
      {
        if (this.repairCommand == null)
        {
          this.repairCommand = new RelayCommand(param => this.Plan(LaunchAction.Repair), param => this.root.State == InstallationState.DetectedPresent);
        }

        return this.repairCommand;
      }
    }

    public bool RepairEnabled
    {
      get { return this.RepairCommand.CanExecute(this); }
    }

    public bool CompleteEnabled
    {
      get { return this.root.State == InstallationState.Applied; }
    }

    public ICommand UninstallCommand
    {
      get
      {
        if (this.uninstallCommand == null)
        {
          this.uninstallCommand = new RelayCommand(param => this.Plan(LaunchAction.Uninstall), param => this.root.State == InstallationState.DetectedPresent);
        }

        return this.uninstallCommand;
      }
    }

    public bool UninstallEnabled
    {
      get { return this.UninstallCommand.CanExecute(this); }
    }

    public ICommand TryAgainCommand
    {
      get
      {
        if (this.tryAgainCommand == null)
        {
          this.tryAgainCommand = new RelayCommand(param => this.Plan(this.plannedAction), param => this.root.State == InstallationState.Failed);
        }

        return this.tryAgainCommand;
      }
    }

    public bool TryAgainEnabled
    {
      get { return this.TryAgainCommand.CanExecute(this); }
    }

    #endregion

    #endregion

    public string Title
    {
      get
      {
        switch (this.root.State)
        {
          case InstallationState.Initializing:
            return "Initializing...";

          case InstallationState.DetectedPresent:
            return "Installed";

          case InstallationState.DetectedNewer:
            return "Newer version installed";

          case InstallationState.DetectedAbsent:
            return "Not installed";

          case InstallationState.Applying:
            switch (this.plannedAction)
            {
              case LaunchAction.Install:
                return "Installing...";

              case LaunchAction.Repair:
                return "Repairing...";

              case LaunchAction.Uninstall:
                return "Uninstalling...";

              default:
                return "Unexpected action state";
            }

          case InstallationState.Applied:
            switch (this.plannedAction)
            {
              case LaunchAction.Install:
                return "Successfully installed";

              case LaunchAction.Repair:
                return "Successfully repaired";

              case LaunchAction.Uninstall:
                return "Successfully uninstalled";

              default:
                return "Unexpected action state";
            }

          case InstallationState.Failed:
            if (this.root.Canceled)
            {
              return "Canceled";
            }
            else if (this.planAttempted)
            {
              switch (this.plannedAction)
              {
                case LaunchAction.Install:
                  return "Failed to install";

                case LaunchAction.Repair:
                  return "Failed to repair";

                case LaunchAction.Uninstall:
                  return "Failed to uninstall";

                default:
                  return "Unexpected action state";
              }
            }
            else
            {
              return "Unexpected failure";
            }

          default:
            return "Unknown view model state";
        }
      }
    }

    /// <summary>
    /// Causes the installation view to re-detect machine state.
    /// </summary>
    public void Refresh()
    {
      // TODO: verify that the engine is in a state that will allow it to do Detect().

      this.root.Canceled = false;
      MpBootstrapperApplication.Model.Engine.Detect();
    }

    /// <summary>
    /// Starts planning the appropriate action.
    /// </summary>
    /// <param name="action">Action to plan.</param>
    private void Plan(LaunchAction action)
    {
      this.planAttempted = true;
      this.plannedAction = action;
      this.hwnd = (MpBootstrapperApplication.View == null) ? IntPtr.Zero : new WindowInteropHelper(MpBootstrapperApplication.View).Handle;

      this.root.Canceled = false;
      MpBootstrapperApplication.Model.Engine.Plan(this.plannedAction);
    }

    private void DetectBegin(object sender, DetectBeginEventArgs e)
    {
      this.root.State = InstallationState.Initializing;
      this.planAttempted = false;
    }

    private void DetectedRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
    {
      if (e.Operation == RelatedOperation.Downgrade)
      {
        this.Downgrade = true;
      }
    }

    private void DetectedPackage(object sender, DetectPackageCompleteEventArgs e)
    {
      if (e.PackageId.Equals("Wix", StringComparison.Ordinal))
      {

        this.root.State = (e.State == PackageState.Present) ? InstallationState.DetectedPresent : InstallationState.DetectedAbsent;
      }
    }

    private void DetectComplete(object sender, DetectCompleteEventArgs e)
    {
      if (LaunchAction.Uninstall == MpBootstrapperApplication.Model.Command.Action)
      {
        MpBootstrapperApplication.Model.Engine.Log(LogLevel.Verbose, "Invoking automatic plan for uninstall");
        MpBootstrapperApplication.Dispatcher.Invoke((Action)delegate()
        {
          this.Plan(LaunchAction.Uninstall);
        }
        );
      }
      else if (Hresult.Succeeded(e.Status))
      {
        if (this.Downgrade)
        {
          // TODO: What behavior do we want for downgrade?
          this.root.State = InstallationState.DetectedNewer;
        }

        if (LaunchAction.Layout == MpBootstrapperApplication.Model.Command.Action)
        {
          PlanLayout();
        }
        else if (MpBootstrapperApplication.Model.Command.Display != Display.Full)
        {
          // If we're not waiting for the user to click install, dispatch plan with the default action.
          MpBootstrapperApplication.Model.Engine.Log(LogLevel.Verbose, "Invoking automatic plan for non-interactive mode.");
          MpBootstrapperApplication.Dispatcher.Invoke((Action)delegate()
          {
            this.Plan(MpBootstrapperApplication.Model.Command.Action);
          }
          );
        }
      }
      else
      {
        this.root.State = InstallationState.Failed;
      }
    }

    private void PlanLayout()
    {
      // Either default or set the layout directory
      if (String.IsNullOrEmpty(MpBootstrapperApplication.Model.Command.LayoutDirectory))
      {
        MpBootstrapperApplication.Model.LayoutDirectory = Directory.GetCurrentDirectory();

        // Ask the user for layout folder if one wasn't provided and we're in full UI mode
        if (MpBootstrapperApplication.Model.Command.Display == Display.Full)
        {
          MpBootstrapperApplication.Dispatcher.Invoke((Action)delegate()
          {
            FolderBrowserDialog browserDialog = new FolderBrowserDialog();
            browserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

            // Default to the current directory.
            browserDialog.SelectedPath = MpBootstrapperApplication.Model.LayoutDirectory;
            DialogResult result = browserDialog.ShowDialog();

            if (DialogResult.OK == result)
            {
              MpBootstrapperApplication.Model.LayoutDirectory = browserDialog.SelectedPath;
              this.Plan(MpBootstrapperApplication.Model.Command.Action);
            }
            else
            {
              MpBootstrapperApplication.View.Close();
            }
          }
          );
        }
      }
      else
      {
        MpBootstrapperApplication.Model.LayoutDirectory = MpBootstrapperApplication.Model.Command.LayoutDirectory;

        MpBootstrapperApplication.Dispatcher.Invoke((Action)delegate()
        {
          this.Plan(MpBootstrapperApplication.Model.Command.Action);
        }
        );
      }
    }

    private void PlanPackageBegin(object sender, PlanPackageBeginEventArgs e)
    {
      if (MpBootstrapperApplication.Model.Engine.StringVariables.Contains("MbaNetfxPackageId") && e.PackageId.Equals(MpBootstrapperApplication.Model.Engine.StringVariables["MbaNetfxPackageId"], StringComparison.Ordinal))
      {
        e.State = RequestState.None;
      }
    }

    private void PlanComplete(object sender, PlanCompleteEventArgs e)
    {
      if (Hresult.Succeeded(e.Status))
      {
        this.root.PreApplyState = this.root.State;
        this.root.State = InstallationState.Applying;
        MpBootstrapperApplication.Model.Engine.Apply(this.hwnd);
      }
      else
      {
        this.root.State = InstallationState.Failed;
      }
    }

    private void ApplyBegin(object sender, ApplyBeginEventArgs e)
    {
      this.downloadRetries.Clear();
    }

    private void CacheAcquireBegin(object sender, CacheAcquireBeginEventArgs e)
    {
      this.cachePackageStart = DateTime.Now;
    }

    private void CacheAcquireComplete(object sender, CacheAcquireCompleteEventArgs e)
    {
      this.AddPackageTelemetry("Cache", e.PackageOrContainerId ?? String.Empty, DateTime.Now.Subtract(this.cachePackageStart).TotalMilliseconds, e.Status);
    }

    private void ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
    {
      this.executePackageStart = e.ShouldExecute ? DateTime.Now : DateTime.MinValue;
    }

    private void ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs e)
    {
      if (DateTime.MinValue < this.executePackageStart)
      {
        this.AddPackageTelemetry("Execute", e.PackageId ?? String.Empty, DateTime.Now.Subtract(this.executePackageStart).TotalMilliseconds, e.Status);
        this.executePackageStart = DateTime.MinValue;
      }
    }

    private void ExecuteError(object sender, ErrorEventArgs e)
    {
      lock (this)
      {
        if (!this.root.Canceled)
        {
          // If the error is a cancel coming from the engine during apply we want to go back to the preapply state.
          if (InstallationState.Applying == this.root.State && (int)Error.UserCancelled == e.ErrorCode)
          {
            this.root.State = this.root.PreApplyState;
          }
          else
          {
            this.Message = e.ErrorMessage;

            if (Display.Full == MpBootstrapperApplication.Model.Command.Display)
            {
              // On HTTP authentication errors, have the engine try to do authentication for us.
              if (ErrorType.HttpServerAuthentication == e.ErrorType || ErrorType.HttpProxyAuthentication == e.ErrorType)
              {
                e.Result = Result.TryAgain;
              }
              else // show an error dialog.
              {
                MessageBoxButton msgbox = MessageBoxButton.OK;
                switch (e.UIHint & 0xF)
                {
                  case 0:
                    msgbox = MessageBoxButton.OK;
                    break;
                  case 1:
                    msgbox = MessageBoxButton.OKCancel;
                    break;
                  // There is no 2! That would have been MB_ABORTRETRYIGNORE.
                  case 3:
                    msgbox = MessageBoxButton.YesNoCancel;
                    break;
                  case 4:
                    msgbox = MessageBoxButton.YesNo;
                    break;
                  // default: stay with MBOK since an exact match is not available.
                }

                MessageBoxResult result = MessageBoxResult.None;
                MpBootstrapperApplication.View.Dispatcher.Invoke((Action)delegate()
                    {
                      result = MessageBox.Show(MpBootstrapperApplication.View, e.ErrorMessage, "WiX Toolset", msgbox, MessageBoxImage.Error);
                    }
                    );

                // If there was a match from the UI hint to the msgbox value, use the result from the
                // message box. Otherwise, we'll ignore it and return the default to Burn.
                if ((e.UIHint & 0xF) == (int)msgbox)
                {
                  e.Result = (Result)result;
                }
              }
            }
          }
        }
        else // canceled, so always return cancel.
        {
          e.Result = Result.Cancel;
        }
      }
    }

    private void ResolveSource(object sender, ResolveSourceEventArgs e)
    {
      int retries = 0;

      this.downloadRetries.TryGetValue(e.PackageOrContainerId, out retries);
      this.downloadRetries[e.PackageOrContainerId] = retries + 1;

      e.Result = retries < 3 && !String.IsNullOrEmpty(e.DownloadSource) ? Result.Download : Result.Ok;
    }

    private void ApplyComplete(object sender, ApplyCompleteEventArgs e)
    {
      MpBootstrapperApplication.Model.Result = e.Status; // remember the final result of the apply.

      // If we're not in Full UI mode, we need to alert the dispatcher to stop and close the window for passive.
      if (Display.Full != MpBootstrapperApplication.Model.Command.Display)
      {
        // If its passive, send a message to the window to close.
        if (Display.Passive == MpBootstrapperApplication.Model.Command.Display)
        {
          MpBootstrapperApplication.Model.Engine.Log(LogLevel.Verbose, "Automatically closing the window for non-interactive install");
          MpBootstrapperApplication.Dispatcher.BeginInvoke((Action)delegate()
          {
            MpBootstrapperApplication.View.Close();
          }
          );
        }
        else
        {
          MpBootstrapperApplication.Dispatcher.InvokeShutdown();
        }
      }

      // Set the state to applied or failed unless the state has already been set back to the preapply state
      // which means we need to show the UI as it was before the apply started.
      if (this.root.State != this.root.PreApplyState)
      {
        this.root.State = Hresult.Succeeded(e.Status) ? InstallationState.Applied : InstallationState.Failed;
      }
    }

    private void AddPackageTelemetry(string prefix, string id, double time, int result)
    {
      lock (this)
      {
        string key = String.Format("{0}Time_{1}", prefix, id);
        string value = time.ToString();
        MpBootstrapperApplication.Model.Telemetry.Add(new KeyValuePair<string, string>(key, value));

        key = String.Format("{0}Result_{1}", prefix, id);
        value = String.Concat("0x", result.ToString("x"));
        MpBootstrapperApplication.Model.Telemetry.Add(new KeyValuePair<string, string>(key, value));
      }
    }
  }
}
