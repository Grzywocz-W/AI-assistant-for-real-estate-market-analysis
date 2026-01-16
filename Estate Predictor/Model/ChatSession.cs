using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Estate_Predictor.Model
{
    public class ChatSession:INotifyPropertyChanged
    {
        private string _name {  get; set; }
        public string Name
        {
            get => _name;
            set 
            { 
                _name = value;
                OnPropertyChanged();
            }  
        }

        public ObservableCollection<ChatMessage> Messages { get; set; }
        public ChatSession()
        {
            Name = "New prediction";
            Messages = new ObservableCollection<ChatMessage>();
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
