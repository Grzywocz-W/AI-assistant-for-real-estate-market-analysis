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

        public ChatMessage(string text, bool isU) { 
            Text = text;
            IsUser = isU;
        }
    }
}
