using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Estate_Predictor.Model
{
    public class ChatMessage
    {
        public string Text { get; set; }
        public bool IsUser {  get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ChatMessage() { 
            // wywaliło mi błąd i się dowiedziałem, że do deserialziacji musi być pusty konstruktor
        }   // dlatego należy stworzyć drugi konstruktor
            //przeciążenie konstruktora
        public ChatMessage(string text, bool isUser)
        {
            Text = text;
            IsUser = isUser;
        }
    }
}
