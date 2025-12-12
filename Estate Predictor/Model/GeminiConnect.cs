using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using static System.Net.WebRequestMethods;
using System.Data;
using Python.Runtime;

namespace Estate_Predictor.Model
{
    public class GeminiConnect
    {
        private readonly string key = Environment.GetEnvironmentVariable("Gemini_API_Key");
        private static readonly HttpClient client = new HttpClient();
        private bool is_Connected { get; set; }
        //private string url { get; set; }
        public GeminiConnect() 
        {
            is_Connected = false;
            //string url = "http://127.0.0.1:8000/chat";
            //connect();
            InitializePython();
        }


        private void InitializePython()
        {
            try
            {                           //podajcie swoją scieżkę
                string pythonDllPath = @"    \Python\Python312\python312.dll";

                if (!System.IO.File.Exists(pythonDllPath))
                {
                    System.Windows.MessageBox.Show($"Nie znaleziono DLL: {pythonDllPath}");
                    is_Connected = false;
                    return;
                }

                Runtime.PythonDLL = pythonDllPath;

                if (!PythonEngine.IsInitialized)
                {
                    PythonEngine.Initialize();
                    PythonEngine.BeginAllowThreads();
                }

                is_Connected = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"{ex.Message}");
                is_Connected = false;
            }
        }

        public string send_and_retrive(string input)
        {
            if (!is_Connected)
            {
                return "Python nie jest połączony.";
            }

            if (string.IsNullOrEmpty(key))
            {
                return "Brak klucza";
            }
            using (Py.GIL())
            {
                try
                {
                    dynamic sys = Py.Import("sys");

                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    sys.path.append(currentDir);

                    dynamic pythonScript = Py.Import("Gemini_Config");
                    // jak nie działa to zróbcie (prawy na skrypt -> własciwości i zamieńcie na końcu "Zawsze kopiuj do katalogu wyjściowego")
                    string result = pythonScript.generate_text(key, input);

                    return result;
                }
                catch (PythonException pyEx)
                {
                    return $"Błąd skryptu: {pyEx.Message}";
                }
                catch (Exception ex)
                {
                    return $"Błąd C#: {ex.Message}";
                }
            }
        }

    }
}
