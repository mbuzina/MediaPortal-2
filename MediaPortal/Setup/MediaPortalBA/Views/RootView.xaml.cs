using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MediaPortal.InstallerUI.ViewModels;

namespace MediaPortal.InstallerUI.Views
{
  /// <summary>
  /// Interaction logic for View.xaml
  /// </summary>
  public partial class RootView : Window
  {
    /// <summary>
    /// Creates the view populated with it's model.
    /// </summary>
    /// <param name="viewModel">Model for the view.</param>
    public RootView(ViewModel viewModel)
    {
      this.DataContext = viewModel;

      this.Loaded += new RoutedEventHandler(Window_Loaded);
      this.Closed += new EventHandler(Window_Closed);
      this.InitializeComponent();
      this.InputBindings.Add((InputBinding)new KeyBinding(viewModel.CancelPromptCommand, new KeyGesture(Key.Escape, ModifierKeys.None)));
    }

    #region Implementation

    public void Run()
    {
      this.Show();
      Dispatcher.Run();
    }

    public void Minimize()
    {
      this.WindowState = WindowState.Minimized;
    }

    #endregion

    #region Control implementation

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      MediaPortalBA.Model.Engine.CloseSplashScreen();

      comboBox1.Items.Clear();
      foreach (var screen in Enum.GetValues(typeof(Screens)))
      {
        comboBox1.Items.Add(screen);
      }
    }

    private void Window_Closed(object sender, EventArgs eventArgs)
    {
      this.Dispatcher.InvokeShutdown();
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

    private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      Screens screen = (Screens)comboBox1.SelectedValue;

      Uri uri = (Uri)null;
      uri = new Uri(String.Format("/MediaPortalBA;component/Views/{0}.xaml", screen), UriKind.Relative);
      
      PageFrame.Navigate(uri);
    }

    #endregion
  }
}
