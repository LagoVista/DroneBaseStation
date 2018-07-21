using LagoVista.Client.Core;
using LagoVista.DroneBaseStation.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.DroneBaseStation
{
    public class ClientAppInfo : IClientAppInfo
    {
        public Type MainViewModel => typeof(MainViewModel);
    }

}
