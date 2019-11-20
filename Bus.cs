using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMOD_MIDI_Control
{
    public class Bus : INotifyPropertyChanged
    {

        private string name;
    //    private float volume;
        private int cc;

        public string Name
        {
            get { return name; }
            set { if (name != value) {name = value; OnPropertyChanged("Name"); } }
        }

        //public float Volume
        //{
        //    get { return volume; }
        //    set { if (volume != value) {volume = value; OnPropertyChanged("Volume"); } }
        //}

        public int CC
        {
            get { return cc; }
            set { if (cc != value) { cc = value; OnPropertyChanged("CC"); } }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


    }
}
