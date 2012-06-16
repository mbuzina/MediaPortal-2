using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using MediaPortal.InstallerUI.ViewModels;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MediaPortal.InstallerUI.Views
{
  /// <summary>
  /// Interaction logic for View.xaml
  /// </summary>
  public partial class RootView : Window
  {
    public delegate void ChangeScreenCallback(Screens newScreen);

    #region Members

    public Screens CurrentScreen { get; private set; }
    private Screens PreviousScreen { get; set; }

    #endregion

    /// <summary>
    /// Creates the view populated with it's model.
    /// </summary>
    /// <param name="viewModel">Model for the view.</param>
    public RootView(ViewModel viewModel)
    {
      this.DataContext = viewModel;

      this.Closing += new CancelEventHandler(Window_Closing);
      this.Loaded += new RoutedEventHandler(Window_Loaded);
      this.Closed += new EventHandler(Window_Closed);
      this.InitializeComponent();
      this.InputBindings.Add((InputBinding)new KeyBinding(viewModel.CloseCommand, new KeyGesture(Key.Escape, ModifierKeys.None)));
      this.PageFrame.Navigated += new NavigatedEventHandler(this.OnFrameNavigated);
    }

    #region Implementation

    /// <summary>
    /// Show user interface
    /// </summary>
    public void Run()
    {
      this.Show();
      Dispatcher.Run();
    }

    /// <summary>
    /// Minimize user interface to taskbar
    /// </summary>
    public void Minimize()
    {
      this.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// public Method to jump to specific screen
    /// </summary>
    /// <param name="newScreen"></param>
    public void GotoScreen(Screens newScreen)
    {
      if (base.Dispatcher == null || base.Dispatcher.HasShutdownFinished || base.Dispatcher.HasShutdownStarted)
        return;

      MediaPortalBA.Model.Engine.Log(LogLevel.Verbose, "ScreenNavigation: GoTo " + newScreen);
      base.Dispatcher.Invoke(new ChangeScreenCallback(this.ChangeScreen),
                             new object[] { newScreen });
    }

    /// <summary>
    /// public method to go to previous screen
    /// </summary>
    public void GoToPreviousScreen()
    {
      if (PreviousScreen == Screens.NotDefined) return;

      MediaPortalBA.Model.Engine.Log(LogLevel.Verbose, "ScreenNavigation: GoToPreviousScreen");
      GotoScreen(PreviousScreen);
    }

    /// <summary>
    /// This method does the actual screen changing.
    /// </summary>
    /// <param name="newScreen"></param>
    private void ChangeScreen(Screens newScreen)
    {
      if (CurrentScreen == newScreen) return;

      if (newScreen == Screens.CancelPage)
        PreviousScreen = CurrentScreen;
      else
        PreviousScreen = Screens.NotDefined;

      MediaPortalBA.Model.Engine.Log(LogLevel.Verbose, "ScreenNavigation: Screen changed to " + newScreen);

      Uri uri = (Uri)null;
      uri = new Uri(String.Format("/MediaPortalBA;component/Views/{0}.xaml", newScreen), UriKind.Relative);

      this.PageFrame.Navigate(uri, newScreen);
    }

    #endregion

    #region Control implementation

    /// <summary>
    /// Is being called when RootWindow is loaded.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      MediaPortalBA.Model.Engine.CloseSplashScreen(); // when window is being displayed, close splashscreen

      // todo: remove combobox handling later, as it is only for testing
      comboBox1.Items.Clear();
      foreach (Screens screen in Enum.GetValues(typeof(Screens)))
      {
        if (screen == Screens.NotDefined) continue;

        comboBox1.Items.Add(screen);
      }
    }

    /// <summary>
    /// Is being call when the window close was called.
    /// Before closing ask the user again, if really wants to cancel the installation.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param
    void Window_Closing(object sender, CancelEventArgs e)
    {
      if (CurrentScreen == Screens.NotDefined  || CurrentScreen == Screens.CancelPage)
        return;

      // TODO: chefkoch, add handling when closing is not allowed (during installation...)
      e.Cancel = true;
      GotoScreen(Screens.CancelPage);
    }

    /// <summary>
    /// When window is closed, shutdown the thread.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    private void Window_Closed(object sender, EventArgs eventArgs)
    {
      this.Dispatcher.InvokeShutdown();
    }

    /// <summary>
    /// Reset Current screen after screen has changed by navigation itself.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
      Screens screens = e.ExtraData is Screens ? (Screens) e.ExtraData : Screens.NotDefined;
      CurrentScreen = screens;
    }

    /// <summary>
    /// Allows the user to drag the window around by grabbing the background rectangle.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Background_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      this.DragMove();
    }

    /// <summary>
    /// Change page after selection has been made.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // TODO: chefkoch, remove later as it is only for testing
      Screens screen = (Screens)comboBox1.SelectedValue;

      GotoScreen(screen);
    }

    #endregion
  }
}
