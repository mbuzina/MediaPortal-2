using System.ComponentModel;
using System.Windows;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class BaseViewModel : PropertyNotifyBase
  {
    public bool IsInDesignMode
    {
      get { return ((bool) (DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof (DependencyObject)).DefaultValue)); }
    }
  }
}