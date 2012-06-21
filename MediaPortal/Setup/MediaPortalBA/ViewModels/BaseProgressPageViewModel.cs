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
      if (this.IsInDesignMode)
      {
        this.StatusText = "This is an example status text.";
        this.ProgressValue = 66;
        return;
      }
    }

    #region Implementation

    #endregion
  }
}