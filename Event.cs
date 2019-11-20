using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMOD_MIDI_Control
{
    public class Event : INotifyPropertyChanged
    {

        private string eventName;

        private List<Parameter> parameterList = new List<Parameter>();

        public string EventName
        {
            get { return eventName; }
            set { if (eventName != value) { eventName = value; OnPropertyChanged("EventName"); } }
        }

        public List<Parameter> ParameterList
        {
            get
            { return parameterList; }
            set
            {
                if (parameterList != value)
                {
                    parameterList = value; OnPropertyChanged("ParameterList"); ;
                }
            }
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

    public class Parameter : INotifyPropertyChanged
    {
        private string name;

        private double minValue, maxValue;

        private int cc;

        public string Name {
            get { return name; }
            set { if (name != value) { name = value; OnPropertyChanged("Name"); } }
        }

        public double MinValue { get => minValue; set => minValue = value; }
        public double MaxValue { get => maxValue; set => maxValue = value; }
        public int CC {
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
