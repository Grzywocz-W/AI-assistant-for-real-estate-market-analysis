using Estate_Predictor.Helpers;
using Estate_Predictor.Model;
using System.ComponentModel;
using Estate_Predictor.View;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Estate_Predictor.Helpers;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Estate_Predictor.ViewModel
{
    public class MainWindow_VM : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        public GeminiConnect _agent = new GeminiConnect();



        public ObservableCollection<ChatSession> Sessions { get; set; }

        private ChatSession _selectedSession;
        public ChatSession SelectedSession
        {
            get => _selectedSession;
            set
            {
                _selectedSession = value;
                OnChanged();
                OnChanged(nameof(ChatHistory));
            }
        }
        public ObservableCollection<ChatMessage> ChatHistory => SelectedSession?.Messages;
        public ICommand CanSend { get; }
        public ICommand NewChatCommand { get; }
        public ICommand RenameChatCommand { get; }
        public ICommand DeleteChatCommand { get; }
        public MainWindow_VM() 
        {
            Sessions = new ObservableCollection<ChatSession>();
            CreateNewChat();

            CanSend = new RelayCommand(Btn_send_clicked, Can_send);
            //ChatHistory = new ObservableCollection<ChatMessage>();

            NewChatCommand = new RelayCommand(CreateNewChat);
            RenameChatCommand = new RelayParamCommand(RenameChat);
            DeleteChatCommand = new RelayParamCommand(DeleteChat);
        }



        private string _message;
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnChanged();
            }
        }



        public async void Btn_send_clicked()
        {
            if (string.IsNullOrWhiteSpace(Message) || SelectedSession == null) return;

            string userMessageText = Message;

            if (SelectedSession.Messages.Count <= 1)
            {
                string newTitle = userMessageText.Length > 25
                    ? userMessageText.Substring(0, 25) + "..."
                    : userMessageText;
                SelectedSession.Name = newTitle;
            }


            SelectedSession.Messages.Add(new ChatMessage(userMessageText, true));

            Message = string.Empty;

            string response = await Task.Run(() => _agent.send_and_retrive(userMessageText));

            SelectedSession.Messages.Add(new ChatMessage(response, false));
        }

        private bool Can_send()
        {
            return !string.IsNullOrWhiteSpace(_message);
        }

        private void CreateNewChat()
        {
            var newSession = new ChatSession();
            newSession.Messages.Add(new ChatMessage("Click here, to open new session", false));

            Sessions.Insert(0, newSession);
            SelectedSession = newSession;
        }


        private void RenameChat(object parameter)
        {
            if (parameter is ChatSession sessionToRename)
            {
                var vm = new RenameWindow_VM(sessionToRename.Name);

                var window = new RenameWindow
                {
                    DataContext = vm,
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    sessionToRename.Name = vm.NewName;
                }
            }
        }

        private void DeleteChat(object parameter)
        {
            if (parameter is ChatSession sessionToDelete)
            {
                Sessions.Remove(sessionToDelete);

                if (SelectedSession == sessionToDelete)
                {
                    if (Sessions.Count > 0)
                        SelectedSession = Sessions[0];
                    else
                        CreateNewChat();
                }
            }
        }

    }
}
