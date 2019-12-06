using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using Midi;
using System.Windows.Controls;
using System.Collections;
using UnityCoroutines;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Globalization;
using System.ComponentModel;

namespace FMOD_MIDI_Control
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        static NetworkStream networkStream = null;
        static Socket socket = null;
        static IAsyncResult socketConnection = null;

        bool learning = false;

        Midi.Control ccValue;

        public static string Ip { get; set; } = "127.0.0.1";
        public static string Port { get; set; } = "3663";

        public InputDevice _InputDevice { get; set; }

        public int RecordCC { get; set; }

        private string evtName;
        public string EventName
        {

            get { return evtName; }
            set { if (evtName != value) { evtName = value; OnPropertyChanged("EventName"); } }

        }

        public ReadOnlyCollection<InputDevice> InputDevices
        {
            get
            {
                return InputDevice.InstalledDevices;
            }

        }

        public ObservableCollection<Bus> ObservableBusses
        {
            get
            {
                return this.Busses;
            }
        }

        public ObservableCollection<Bus> Busses = new ObservableCollection<Bus>();

        public ObservableCollection<Event> ObservableEvents
        {
            get
            {
                return this.Events;
            }
        }

        public ObservableCollection<Event> Events = new ObservableCollection<Event>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            CoroutineManager.Instance.Run();
        }

        private void GetEvent()
        {
            if (_InputDevice == null)
            {
                System.Windows.MessageBox.Show("Please select a valid Midi Device first", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (Events.Count > 0)
            {
                Events.Clear();
            }

            SendScriptCommand("var event = studio.window.browserCurrent();");

            var eventName = (string)GetScriptOutput("event.name");

            if (eventName == null)
            {
                System.Windows.MessageBox.Show("Couldn't get the FMOD Event. Please select a valid FMOD Event in the Event Hierarchy", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            Event evt = new Event();

            evt.EventName = eventName;
            EventName = eventName;

            var eventParameters = Int32.Parse(GetScriptOutput("var param = event.getParameterPresets(); param.length"));

            for (int i = 0; i < eventParameters; i++)
            {
                Parameter param = new Parameter();

                param.Name = (string)GetScriptOutput("param" + "[" + i + "]" + ".presetOwner.name");
                param.MinValue = Double.Parse(GetScriptOutput("param" + "[" + i + "]" + ".minimum"));
                param.MaxValue = Double.Parse(GetScriptOutput("param" + "[" + i + "]" + ".maximum"));

                evt.ParameterList.Add(param);
            }

            Events.Add(evt);

            this.DataGridEvent.DataContext = evt.ParameterList;
        }

        private void GetMixerBusses()
        {

            if (_InputDevice == null)
            {
                System.Windows.MessageBox.Show("Please select a valid Midi Device first", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            SendScriptCommand("var mixerBusses = studio.project.model.MixerGroup.findInstances();");
            var mixerBusses = Int32.Parse(GetScriptOutput("mixerBusses.length"));

            if (Busses.Count > 0)
            {
                Busses.Clear();
            }

            for (int i = 0; i < mixerBusses; i++)
            {

                Busses.Add(new Bus()
                {
                    Name = GetScriptOutput("mixerBusses[" + i + "]" + ".name"),
                    CC = 0,
                });
            }
            this.DataGrid.DataContext = this.Busses;
        }

        private void OpenMidiDevice(Midi.InputDevice _InputDevice)
        {
            if (!_InputDevice.IsOpen)
            {

                _InputDevice.Open();
                _InputDevice.ControlChange += new InputDevice.ControlChangeHandler(NoteCC);
                _InputDevice.StartReceiving(null); // Note events will be received in another thread Console.ReadKey(); // This thread waits for a keypress ... 
            }
        }

        private void CloseMidiDevice(Midi.InputDevice _InputDevice)
        {
            _InputDevice.StopReceiving();
            _InputDevice.ControlChange -= NoteCC;
            _InputDevice.Close();
        }

        public void NoteCC(ControlChangeMessage msg)
        {

            if (Busses.Count > 0)
            {
                for (int i = 0; i < Busses.Count; i++)
                {

                    if (msg.Control == (Midi.Control)Busses[i].CC && !learning)
                    {
                        SendScriptCommand("mixerBusses[" + i + "].volume = " + ConvertRange(0, 127, -80, 10, msg.Value));
                    }

                    if (learning)
                    {
                        ccValue = msg.Control;
                    }
                }

            }

            if (Events.Count > 0)
            {

                for (int i = 0; i < Events[0].ParameterList.Count; i++)
                {

                    if (msg.Control == (Midi.Control)Events[0].ParameterList[i].CC && !learning)
                    {
                        SendScriptCommand("event.setCursorPosition(" + "param" + "[" + i + "], " + ConvertRangeDouble((double)0, (double)127, Events[0].ParameterList[i].MinValue, Events[0].ParameterList[i].MaxValue, (double)msg.Value).ToString(CultureInfo.InvariantCulture) + ")");
                    }

                    if (learning)
                    {
                        ccValue = msg.Control;
                    }
                }

                if (msg.Control == (Midi.Control)RecordCC)
                {
                    SendScriptCommand("if (typeof session === 'undefined') { var currentEvent = studio.window.browserCurrent(); var session = studio.project.create('ProfilerSession'); studio.window.navigateTo(session); session.toggleRecording(); studio.window.navigateTo(currentEvent); } else { if (session.isRecording() == true) { studio.window.navigateTo(session); session.toggleRecording(); studio.project.save(); } var folderName = session.id; console.log(folderName); var fileName = 'capture-0000.wav'; var projectPath = studio.project.filePath; projectPath = projectPath.substr(0, projectPath.lastIndexOf('/')); var filePath = projectPath + '/.user/Profiler/Session/' + folderName + '/' + fileName; console.log(filePath); var newEvent = studio.project.create('Event'); var d = new Date(); newEvent.name = 'Recording' + d.getTime(); var file = studio.system.getFile(filePath); var filePath2 = (projectPath + '/.user/Profiler/Session/' + folderName + '/' + newEvent.name + '.wav').toString(); file.copy(filePath2); var track = newEvent.addGroupTrack(); var sound = track.addSound(newEvent.timeline, 'SingleSound', 0, 10); var asset = studio.project.importAudioFile(filePath2); sound.audioFile = asset; sound.length = asset.length; studio.window.navigateTo(currentEvent); currentEvent.play(); d = undefined; newEvent = undefined; sound = undefined; track = undefined; asset = undefined; currentEvent = undefined; session = undefined; filePath = undefined; folderName = undefined; asset = undefined; file = undefined; filePath2 = undefined; }");
                }
            }
        }

        public static int ConvertRange(
            int originalStart, int originalEnd, // original range
            int newStart, int newEnd, // desired range
            int value) // value to convert
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (int)(newStart + ((value - originalStart) * scale));
        }

        public static double ConvertRangeDouble(
           double originalStart, double originalEnd, // original range
           double newStart, double newEnd, // desired range
          double value) // value to convert
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (double)(newStart + ((value - originalStart) * scale));
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Bus>));

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML file (*.xml)|*.xml";

            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter wr = new StreamWriter(saveFileDialog.FileName))
                {
                    xs.Serialize(wr, Busses);
                }
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Bus>));

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML file (*.xml)|*.xml";

            if (openFileDialog.ShowDialog() == true)
            {
                using (StreamReader rd = new StreamReader(openFileDialog.FileName))
                {
                    Busses = xs.Deserialize(rd) as ObservableCollection<Bus>;
                    this.DataGrid.DataContext = this.Busses;
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnectedToStudio())
            {
                GetMixerBusses();
            }
            else
            {              
                    System.Windows.MessageBox.Show("Can't connect to FMOD Studio, make sure a FMOD project is currently open.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
            }
        }

        private void DeviceList_DropDownClosed(object sender, EventArgs e)
        {
            if (_InputDevice != null)
            {
                var oldDevice = _InputDevice;
                CloseMidiDevice(oldDevice);
            }

            ComboBox cb = sender as ComboBox;

            if (cb.SelectedIndex == -1)
            {
                System.Windows.MessageBox.Show("Select a valid Midi Device", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            _InputDevice = cb.SelectedItem as InputDevice;

            OpenMidiDevice(_InputDevice);
        }

        private void CC_Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.Changes.Count > 0)
            {
                TextBox ccText = sender as TextBox;

                if (!ccText.IsFocused)
                    return;

                Bus selectedBus = (Bus)DataGrid.SelectedItem;

                //Bus selectedBus = new Bus();
                //selectedBus = DataGrid.SelectedItem as Bus;

                if (ccText.Text == "")
                    return;

                selectedBus.CC = Int32.Parse(ccText.Text);
            }
        }

        private void Button_MIDI_Click(object sender, RoutedEventArgs e)
        {


            Bus selectedBus = (Bus)DataGrid.SelectedItem;
            CoroutineManager.Instance.StartCoroutine(LearnCC(selectedBus));
        }

        public IEnumerator LearnCC(Bus selectedBus)
        {
            learning = true;
            while ((int)ccValue == 0)
            {
                yield return 0;
            }
            selectedBus.CC = (int)ccValue;

            ccValue = 0;

            learning = false;
        }

        public IEnumerator LearnParameterCC(Parameter selectedParameter)
        {
            learning = true;
            while ((int)ccValue == 0)
            {
                yield return 0;
            }
            selectedParameter.CC = (int)ccValue;

            ccValue = 0;

            learning = false;
        }

        private void btnEventRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnectedToStudio())
            {            
            GetEvent();
            }
            else
            {
                System.Windows.MessageBox.Show("Can't connect to FMOD Studio, make sure a FMOD project is currently open.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
        }

        private void CC_Parameter_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (e.Changes.Count > 0)
            {


                TextBox ccText = sender as TextBox;

                if (!ccText.IsFocused)
                    return;

                Parameter param = (Parameter)DataGridEvent.SelectedItem;

                if (ccText.Text == "")
                    return;

                param.CC = Int32.Parse(ccText.Text);
            }
        }

        private void Button_Event_MIDI_Click(object sender, RoutedEventArgs e)
        {
            Parameter param = (Parameter)DataGridEvent.SelectedItem;

            CoroutineManager.Instance.StartCoroutine(LearnParameterCC(param));
        }

        private void IP_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox ip = sender as TextBox;
            if (e.Changes.Count > 0)
            {
                Ip = ip.Text;
            }
        }

        private void Port_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox port = sender as TextBox;
            if (e.Changes.Count > 0)
            {
                Port = port.Text;
            }
        }

        private void CC_Record_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox record = sender as TextBox;
            if (e.Changes.Count > 0)
            {
                RecordCC = Int32.Parse(record.Text);
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

        static NetworkStream ScriptStream
        {
            get
            {
                if (networkStream == null)
                {
                    try
                    {
                        if (socket == null)
                        {
                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        }

                        if (!socket.Connected)
                        {
                            socketConnection = socket.BeginConnect(Ip, Int32.Parse(Port), null, null);
                            socketConnection.AsyncWaitHandle.WaitOne();
                            socket.EndConnect(socketConnection);
                            socketConnection = null;
                        }

                        networkStream = new NetworkStream(socket);

                        byte[] headerBytes = new byte[128];
                        int read = ScriptStream.Read(headerBytes, 0, 128);
                        string header = Encoding.UTF8.GetString(headerBytes, 0, read - 1);
                        if (header.StartsWith("log():"))
                        {
                            Trace.WriteLine("FMOD Studio: Script Client returned " + header.Substring(6));
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("FMOD Studio: Script Client failed to connect - Check FMOD Studio is running");

                        socketConnection = null;
                        socket = null;
                        networkStream = null;

                        throw e;
                    }
                }
                return networkStream;
            }
        }

        public static bool SendScriptCommand(string command)
        {
            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            try
            {
                ScriptStream.Write(commandBytes, 0, commandBytes.Length);
                byte[] commandReturnBytes = new byte[128];
                int read = ScriptStream.Read(commandReturnBytes, 0, 128);
                string result = Encoding.UTF8.GetString(commandReturnBytes, 0, read - 1);
                return (result.Contains("true"));
            }
            catch (Exception)
            {
                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream = null;
                }
                return false;
            }
        }

        public static string GetScriptOutput(string command)
        {
            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            try
            {
                ScriptStream.Write(commandBytes, 0, commandBytes.Length);
                byte[] commandReturnBytes = new byte[2048];
                int read = ScriptStream.Read(commandReturnBytes, 0, commandReturnBytes.Length);
                string result = Encoding.UTF8.GetString(commandReturnBytes, 0, read - 1);
                if (result.StartsWith("out():"))
                {
                    return result.Substring(6).Trim();
                }
                return null;
            }
            catch (Exception)
            {
                networkStream.Close();
                networkStream = null;
                return null;
            }
        }

        private static void AsyncConnectCallback(IAsyncResult result)
        {
            try
            {
                socket.EndConnect(result);
            }
            catch (Exception)
            {
            }
            finally
            {
                socketConnection = null;
            }
        }

        public static bool IsConnectedToStudio()
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    if (SendScriptCommand("true"))
                    {
                        return true;
                    }
                }

                if (socketConnection == null)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketConnection = socket.BeginConnect(Ip, Int32.Parse(Port), AsyncConnectCallback, null);
                }

                return false;

            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Can't connect to FMOD Studio, please check IP and Port settings. Make sure FMOD Studio is open.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }
    }
}

