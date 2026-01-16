#pip install fastapi uvicorn google-generativeai
#pip install keras skops tensorflow
#pip install google-cloud-aiplatform
#pip install google-adk
#Give me a price of 100 square meters flat with 4 rooms
import sys
import os

if os.name == 'nt':## jak tego nie ma to się wywalala, poszukaj czegoś takiego jak null consola, tzw NullWriter
    import ctypes
    try:
        ctypes.windll.kernel32.AllocConsole()
        
        sys.stdout = open("CONOUT$", "w", encoding="utf-8")
        sys.stderr = open("CONOUT$", "w", encoding="utf-8")
        print("Działa?????")
    except Exception as e:
        pass 

import signal
signal.set_wakeup_fd = lambda *a, **k: None#zostawcie to bo się wywali

import nest_asyncio
nest_asyncio.apply()

import google.generativeai as genai
from google.adk.agents import LlmAgent
import os
from keras.saving import load_model
import skops.io as sio
import numpy as np
#dodane importy
import asyncio
from google.adk.sessions import InMemorySessionService
from google.adk.runners import Runner
from google.genai import types




script_dir = os.path.dirname(os.path.abspath(__file__))

parent_dir = os.path.dirname(script_dir)

#dodajcie ścieżkę
#model_path = os.path.join(parent_dir, "model.keras")
#scaler_x_path = os.path.join(parent_dir, "minmaxscalerx.skops")
#scaler_y_path = os.path.join(parent_dir, "minmaxscalery.skops")

#dodajcie swoje

model_path = r"Projekt\Estate Predictor\model.keras"
scaler_x_path = r"Projekt\Estate Predictor\minmaxscalerx.skops"
scaler_y_path = r"Projekt\Estate Predictor\minmaxscalery.skops"

model = load_model(model_path)
unknown_types = sio.get_untrusted_types(file=scaler_x_path)
scaler_x = sio.load(scaler_x_path, trusted=unknown_types)
scaler_y = sio.load(scaler_y_path, trusted=unknown_types)


global_runner = None
global_session_service = None
APP_NAME = "housing_price_prediction_app"
USER_ID = "user_1"
SESSION_ID = "session_1"

def initialize_agent_system(api_key):
    global global_runner, global_session_service
    
    if global_runner is not None:
        return

    os.environ['GOOGLE_API_KEY'] = api_key
    os.environ['GOOGLE_GENAI_USE_VERTEXAT'] = 'False'

    house_price_prediction_agent = LlmAgent(
        model="gemini-2.5-flash",
        name='house_price_prediction_agent',
        description="Answers user questions about house prices in Paris and suggest the best accomodation price for given parameters.",
        instruction="""
    You are an intelligent housing price prediction agent.

    Your goal is to provide the user with an estimated accommodation price in Paris.
    You can call the function `predict_housing_price()` to make predictions.

    - If the user does not explicitly provide all parameters (like square_meters or number_of_rooms),
      **you must intelligently infer reasonable default values**.
    - Always assume the context is about housing in Paris unless stated otherwise.
    - The user usually will not provide 'number' word to ask about number of given feature, e.g. 5 rooms is 5 number_of_rooms
    - When uncertain about missing attributes give three answers:
    - In first answer replace all missing attributes with 0
    - In second answer, make **realistic assumptions**:
        • square_meters: 70
        • number_of_rooms: 3
        • number_of_floors: 1
        • has_yard: 0
        • has_pool: 0
        • year_built: 1990
        • is_new_built: 0
        • has_storage_room: 0
        • number_of_guest_rooms:
    - In third answer set those attributes to 1:
        • has_yard
        • has_pool
        • is_new_built
        • has_storm_protector
        • has_storage_room
        and rest attributes excluding previous_owners and year_built set to randomized number from 10000 to 1000000 differend for each parameter.
    - List those answers in unordered list form with newline character as a separator.
    - Clearly state any assumptions made in your final answer.
    - Return the predicted price and short reasoning.
    - If you provide user with default values for parameters, don't state the actual name of that parameter, but how it would be used in natural language. List those parameters in unordered list form with newline character as a separator.
    - User must provide at least square meters (`square_meters` param) and number of rooms (`number_of_rooms` param).
    - Answer the user naturally, don't show the reasoning behind your answers. Clearly state which price is lowest, average and highest for given params.
    - House prices cannot be negative.
    - Feature is is_new_built means that the house couldn't have been built before 2020. That means that if is_new_built is True then year_built is not important. But if the year is before 2020, then the house is not newly built.
    - Always highest price is greater than average price and average price is greater than lowest price.

    Example query: Show me the house price for 1000 square meters and 5 room flat in paris, 5 levels, no yard.
    Example response: The lowest predicted price for a given params is 10000. The average price for given params is 50000. The highest predicted price for given params is 150000.
    """,#tak i forgot comma
        tools=[predict_housing_price] 
    )

    global_session_service = InMemorySessionService()
    
    global_runner = Runner(
        agent=house_price_prediction_agent,
        app_name=APP_NAME,
        session_service=global_session_service
    )
    print("Agent i SessionService zainicjalizowane globalnie.")




def predict_housing_price(features: dict) -> float:
    """
    Predicts the housing price based on the provided features.

    features: A dictionary containing the following keys (all integers, with defaults if not provided):
        - square_meters: Area of the house in square meters (default: 0)
        - number_of_rooms: Number of rooms in the house (default: 0)
        - has_yard: Whether the house has a yard (1 for yes, 0 for no, default: 0)
        - has_pool: Whether the house has a pool (1 for yes, 0 for no, default: 0)
        - number_of_floors: Number of floors in the house (default: 1)
        - number_of_previous_owners: Number of previous owners (default: 1)
        - year_built: Year the house was built (default: 1950)
        - is_new_built: Whether the house is newly built (1 for yes, 0 for no, default: 0)
        - has_storm_protector: Whether the house has a storm protector (1 for yes, 0 for no, default: 0)
        - basement_square_meters: Area of the basement in square meters (default: 50)
        - attic_square_meters: Area of the attic in square meters (default: 0)
        - garage_size: Size of the garage in square meters (default: 0)
        - has_storage_room: Whether the house has a storage room (1 for yes, 0 for no, default: 0)
        - number_of_guest_rooms: Number of guest rooms (default: 0)

    Returns: Predicted price as a float.
    """
    # Extract values with defaults
    square_meters = features.get('square_meters', 0)
    number_of_rooms = features.get('number_of_rooms', 0)
    has_yard = features.get('has_yard', 0)
    has_pool = features.get('has_pool', 0)
    number_of_floors = features.get('number_of_floors', 1)
    number_of_previous_owners = features.get('number_of_previous_owners', 1)
    year_built = features.get('year_built', 1950)
    is_new_built = features.get('is_new_built', 0)
    has_storm_protector = features.get('has_storm_protector', 0)
    basement_square_meters = features.get('basement_square_meters', 50)
    attic_square_meters = features.get('attic_square_meters', 0)
    garage_size = features.get('garage_size', 0)
    has_storage_room = features.get('has_storage_room', 0)
    number_of_guest_rooms = features.get('number_of_guest_rooms', 0)

    # Build input array in the same order as training features
    x_new = np.array([
        square_meters,
        number_of_rooms,
        has_yard,
        has_pool,
        number_of_floors,
        number_of_previous_owners,
        year_built,
        is_new_built,
        has_storm_protector,
        basement_square_meters,
        attic_square_meters,
        garage_size,
        has_storage_room,
        number_of_guest_rooms
    ])

    x_new_scaled = scaler_x.transform(x_new.reshape(1, -1))
    y_pred_scaled = model.predict(x_new_scaled)
    y_pred = scaler_y.inverse_transform(y_pred_scaled)
    return y_pred[0][0].item() # Return scalar float


async def run_agent_process(api_key, user_message):
    global global_runner, global_session_service, USER_ID, SESSION_ID, APP_NAME

    if global_runner is None:
        initialize_agent_system(api_key)

    try:
        await global_session_service.create_session(app_name=APP_NAME, user_id=USER_ID, session_id=SESSION_ID)
    except Exception:
        pass

    content = types.Content(role='user', parts=[types.Part(text=user_message)])
    
    final_response_text = "No response generated."

    async for event in global_runner.run_async(user_id=USER_ID, session_id=SESSION_ID, new_message=content):
        if event.is_final_response():
            found_response = False
            if event.content and event.content.parts:
                for part in event.content.parts:
                    if part.text:
                        final_response_text = part.text
                        found_response = True
                        break
            else:
                final_response_text = f"Agent escalated: {event.error_message or 'No specific error message'}"
                found_response = True
            
            if found_response:
                break
            
    return final_response_text


def generate_text(api_key, user_message):
    try:
        if not api_key:
            return "Błąd Python: Brak klucza API."
        try:
            loop = asyncio.get_event_loop()
        except RuntimeError:
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)
            
        return loop.run_until_complete(run_agent_process(api_key, user_message))
        
    except Exception as e:
        return f"Błąd krytyczny: {str(e)}"
