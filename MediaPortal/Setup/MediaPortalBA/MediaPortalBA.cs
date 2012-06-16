using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MediaPortal.InstallerUI.ViewModels;
using MediaPortal.InstallerUI.Views;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MediaPortal.InstallerUI
{
  /// <summary>
  /// The WiX toolset user experience.
  /// </summary>
  public class MediaPortalBA : BootstrapperApplication
  {
    #region Properties

    /// <summary>
    /// Gets the global model.
    /// </summary>
    public static Model Model { get; private set; }

    /// <summary>
    /// Gets the global view.
    /// </summary>
    public static RootView View { get; private set; }

    /// <summary>
    /// Gets the global dispatcher.
    /// </summary>
    public static Dispatcher Dispatcher { get; private set; }

    #endregion

    #region Implementation

    /// <summary>
    /// Thread entry point for WiX Toolset UX.
    /// </summary>
    protected override void Run()
    {
#if Debug
      MessageBox.Show("Attach debugger!");
#endif

      try
      {
        this.Engine.Log(LogLevel.Verbose, "Running the MpBootstrapperApplication.");
        MediaPortalBA.Model = new Model(this);
        MediaPortalBA.Dispatcher = Dispatcher.CurrentDispatcher;
        
        ViewModel viewModel = new ViewModel();
        viewModel.Initialize();
        this.Engine.Log(LogLevel.Verbose, "Wait for Detect to complete");
        //viewModel.WaitForDetectComplete();
        this.Engine.Log(LogLevel.Verbose, "Detect Completed, now create view");
        
        MediaPortalBA.View = new RootView(viewModel);
        MediaPortalBA.View.Run();

        this.Engine.Quit(MediaPortalBA.Model.Result);
      }
      catch (Exception ex)
      {
        for (Exception exception = ex; exception != null; exception = exception.InnerException)
        {
          this.Engine.Log(LogLevel.Error, exception.Message);
          this.Engine.Log(LogLevel.Error, exception.StackTrace);
        }
        this.Engine.Quit(-1);
      }
    }

    /// <summary>
    /// Launches the default web browser to the provided URI.
    /// </summary>
    /// <param name="uri">URI to open the web browser.</param>
    public static void LaunchUrl(string uri)
    {
      // Switch the wait cursor since shellexec can take a second or so.
      Cursor cursor = View.Cursor;
      View.Cursor = Cursors.Wait;

      try
      {
        Process process = new Process();
        process.StartInfo.FileName = uri;
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.Verb = "open";

        process.Start();
      }
      finally
      {
        View.Cursor = cursor; // back to the original cursor.
      }
    }

    #endregion
  }
}
