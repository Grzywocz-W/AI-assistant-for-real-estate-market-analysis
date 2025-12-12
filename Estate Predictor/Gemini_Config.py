#pip install fastapi uvicorn google-generativeai
import google.generativeai as genai
import os

def generate_text(api_key, user_message):
    try:
        if not api_key:
            return "Błąd Python: Brak klucza API."
        genai.configure(api_key=api_key)
        model = genai.GenerativeModel('gemini-2.5-flash-lite')
        
        response = model.generate_content(user_message)
        
        return response.text
        
    except Exception as e:
        return f"Błąd {str(e)}"
