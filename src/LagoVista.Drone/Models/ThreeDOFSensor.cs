using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LagoVista.Drone.Models
{
    public class ThreeDOFSensor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        private bool Set<T>(ref T storage, T value, string columnName = null, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            if (PropertyChanged != null && !String.IsNullOrEmpty(propertyName))
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            return true;
        }

        private Int16 _x;
        public Int16 X
        {
            get { return _x; }
            set { Set(ref _x, value); }
        }

        private Int16 _y;
        public Int16 Y
        {
            get { return _y; }
            set { Set(ref _y, value); }
        }

        private Int16 _z;
        public Int16 Z
        {
            get { return _z; }
            set { Set(ref _z, value); }
        }
    }
}
