using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazLootIni
{
    public class INPC : INotifyPropertyChanged
    {

        #region Standard INotifyPropertyChanged Implementation

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { };
        #endregion

    }
}
