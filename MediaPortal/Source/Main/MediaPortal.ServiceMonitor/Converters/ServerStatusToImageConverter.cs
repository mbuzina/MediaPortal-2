﻿#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using MediaPortal.ServiceMonitor.Model;

namespace MediaPortal.ServiceMonitor.Converters
{
  [ValueConversion(typeof(string), typeof(string))]
  public class ServerStatusToImageConverter : MarkupExtension, IValueConverter
  {
    #region Overrides of MarkupExtension
    private static ServerStatusToImageConverter _converter;
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return _converter ?? (_converter = new ServerStatusToImageConverter());
    }

    #endregion

    #region Implementation of IValueConverter

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var serverStatus = (string) value;
      if (!string.IsNullOrEmpty(serverStatus))
        switch (serverStatus)
        {
      	  case "Attached to Server":
      	  case "Connected to Server":
        		return "../../Resources/Images/connected.png";
         case "Detached from Server":
      	  case "Disconnected from Server":
      		  return "../../Resources/Images/disconnected.png";
      	}
       
      return "../../Resources/Images/Info.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
