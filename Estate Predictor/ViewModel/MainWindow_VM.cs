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
using System.IO;
using System.Text.Json; 
using System.Text.Encodings.Web;
using System.Linq;

namespace Estate_Predictor.ViewModel
{
    public class MainWindow_VM : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        public GeminiConnect _agent = new GeminiConnect();

        private string _saveFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SavedChats");


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

            if (!Directory.Exists(_saveFolderPath))
            {
                Directory.CreateDirectory(_saveFolderPath);
            }

            LoadSessionsFromDisk();


            if (Sessions.Count > 0)
            {
                SelectedSession = Sessions[0];
            }
            else
            {
                SelectedSession = null;
            }

            CanSend = new RelayCommand(Btn_send_clicked, Can_send);
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

            SelectedSession.Messages.Add(new ChatMessage(userMessageText, true));

            Message = string.Empty;

            string response = await Task.Run(() => _agent.send_and_retrive(userMessageText));

            SelectedSession.Messages.Add(new ChatMessage(response, false));

            //zapisujemy po odpowiedzi AI
            SaveSessionToDisk(SelectedSession);
        }

        private bool Can_send()
        {
            return !string.IsNullOrWhiteSpace(_message);
        }

        private void CreateNewChat()
        {
            var vm = new RenameWindow_VM("New Valuation " + DateTime.Now.ToShortDateString());

            var window = new RenameWindow
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow,
                Title = "New chat"
            };

            if (window.ShowDialog() == true)
            {
                string validName = NameValidation(vm.NewName);

                string potentialPath = Path.Combine(_saveFolderPath, validName + ".json");
                if (File.Exists(potentialPath))
                {
                    MessageBox.Show("Valuation with this name already exists. Choose another.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newSession = new ChatSession { Name = validName };

                newSession.Messages.Add(new ChatMessage("# Welcome to EstateAI! \n\n" +
                                                        "This application uses AI to predict property prices in **Paris**. \n\n" +
                                                        "### How to use: \n" +
                                                        "* **Describe the property**: Provide details like square meters, number of rooms, floor, and year built. \n" +
                                                        "* **Answer AI questions**: The model will ask for specific parameters if they are missing. \n" +
                                                        "* **Get Valuation**: Based on your data, the AI will estimate the lowest, average, and highest market price. \n\n" +
                                                        "--- \n" +
                                                        "*Click the send button or press Enter to start your first valuation.*", 
                                                        false));

                Sessions.Insert(0, newSession);
                SelectedSession = newSession;

                SaveSessionToDisk(newSession);
            }
        }


        private void RenameChat(object parameter)
        {
            if (parameter is ChatSession sessionToRename)
            {
                string oldName = sessionToRename.Name;
                var vm = new RenameWindow_VM(oldName);

                var window = new RenameWindow
                {
                    DataContext = vm,
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    string newName = NameValidation(vm.NewName);

                    if (oldName == newName) return;//program musi być idioto odporny
                    string oldPath = Path.Combine(_saveFolderPath, oldName + ".json");
                    string newPath = Path.Combine(_saveFolderPath, newName + ".json");

                    if (File.Exists(newPath))
                    {
                        MessageBox.Show("Such name is occupied", "Error");
                        return;
                    }

                    sessionToRename.Name = newName;

                    
                    if (File.Exists(oldPath))
                    {
                        File.Move(oldPath, newPath);
                    }
                    else
                    {
                        SaveSessionToDisk(sessionToRename);
                    }
                }
            }
        }

        private void DeleteChat(object parameter)
        {
            if (parameter is ChatSession sessionToDelete)
            {
                string path = Path.Combine(_saveFolderPath, sessionToDelete.Name + ".json");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                Sessions.Remove(sessionToDelete);

                if (SelectedSession == sessionToDelete)
                {
                    if (Sessions.Count > 0)
                        SelectedSession = Sessions[0];
                    else
                        SelectedSession = null;
                }
            }
        }


        private void SaveSessionToDisk(ChatSession session)
        {
            try
            {
                string safeName = NameValidation(session.Name);
                string filePath = Path.Combine(_saveFolderPath, safeName + ".json");
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping//dla polskich znaków, bo czemu nie
                };

                string json = JsonSerializer.Serialize(session, options);
                File.WriteAllText(filePath, json);

            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd zapisu: {e.Message}");
            }
        }


        private void LoadSessionsFromDisk()
        {
            try
            {
                var files = Directory.GetFiles(_saveFolderPath, "*.json");//* - oznacza wszystkie znaki
                var sortedFiles = files.OrderByDescending(f => File.GetLastWriteTime(f));

                foreach (var file in sortedFiles)
                {
                    string jsonFile = File.ReadAllText(file);
                    var session = JsonSerializer.Deserialize<ChatSession>(jsonFile);

                    if (session != null)
                    {
                        session.Name = Path.GetFileNameWithoutExtension(file);
                        Sessions.Add(session);
                    }
                }
                 
            }
            catch (Exception e)
            {
                MessageBox.Show($"Deserialize erroe: {e.Message}");
            }
        }

        private string NameValidation(string name)
        {   
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string invalidRe = string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(invalidChars));
            return System.Text.RegularExpressions.Regex.Replace(name, invalidRe, "_");
        }
    }
}
