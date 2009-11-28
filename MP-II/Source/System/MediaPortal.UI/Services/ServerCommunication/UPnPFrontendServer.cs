#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using UPnP.Infrastructure.Dv;

namespace MediaPortal.UI.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP frontend server device.
  /// </summary>
  public class UPnPFrontendServer : UPnPServer
  {
    public const int SSDP_ADVERTISMENT_INTERVAL = 1800;

    public UPnPFrontendServer(string frontendServerSystemId)
    {
      AddRootDevice(new MP2FrontendServerDevice(frontendServerSystemId));
      // TODO: add UPnP standard MediaRenderer device: it's not implemented yet
      //AddRootDevice(new UPnPMediaRendererDevice(...));
    }

    public void Start()
    {
      Bind(SSDP_ADVERTISMENT_INTERVAL);
    }

    public void Stop()
    {
      Close();
    }
  }
}