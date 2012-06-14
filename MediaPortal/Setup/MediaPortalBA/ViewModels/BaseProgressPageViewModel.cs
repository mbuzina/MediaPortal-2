using System;
using System.ComponentModel;

namespace MediaPortal.InstallerUI.ViewModels
{
  public class BaseProgressPageViewModel : BaseViewModel
  {
    #region Members

    private string _statusText = string.Empty;
    private int _progressValue;

    #endregion

    #region Properties

    public int ProgressValue
    {
      get
      {
        return this._progressValue;
      }
      set
      {
        this._progressValue = value;
        base.OnPropertyChanged("ProgressValue");
      }
    }

    public string StatusText
    {
      get
      {
        return this._statusText;
      }
      set
      {
        this._statusText = value;
        base.OnPropertyChanged("StatusText");
      }
    }

    #endregion

    public BaseProgressPageViewModel()
    {
    }

    #region Implementation

    #endregion
  }
}