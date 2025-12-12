using Estate_Predictor.Helpers;
using Estate_Predictor.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Estate_Predictor.ViewModel
{
    public class MainWindow_VM : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        public GeminiConnect _agent = new GeminiConnect();


        public ICommand CanSend { get; }
        public MainWindow_VM() 
        {

            CanSend = new RelayCommand(Btn_send_clicked, Can_send);
        }

        private string _message;
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnChanged(nameof(Message));
            }
        }



        public async void Btn_send_clicked()
        {


            string response = await Task.Run(() => _agent.send_and_retrive(_message));

            MessageBox.Show(response);

        }

        private bool Can_send()
        {
            return !string.IsNullOrWhiteSpace(_message);
        }




    }
}
