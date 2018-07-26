using LagoVista.Core.Models;
using System;

namespace LagoVista.Drone.Models
{
    public class ServoOutput : ModelBase
    {
        public UInt16 _value;
        public UInt16 Value
        {
            get { return _value; }
            set { Set(ref _value, value); }
        }
    }
}
