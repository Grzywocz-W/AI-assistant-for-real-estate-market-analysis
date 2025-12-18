from keras.saving import load_model
import skops.io as sio
import numpy as np
model = load_model("model.keras")
unknown_types = sio.get_untrusted_types(file="minmaxscalerx.skops")
scaler_x = sio.load("minmaxscalerx.skops", trusted=unknown_types)
scaler_y = sio.load("minmaxscalery.skops", trusted=unknown_types)

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


# Example usage
example_features = {
    'square_meters': 120,
    'number_of_rooms': 6,
    'number_of_floors': 8,
    'has_yard': 1,
    'has_pool': 0,
    'is_new_built': 0,
    'has_storm_protector': 1,
    'basement_square_meters': 80,
    'year_built': 2010,
    'garage_size': 200,
    'has_storage_room': 1,
    'number_of_guest_rooms': 2,
    'number_of_previous_owners': 4,
    'attic_square_meters': 80
}

print(predict_housing_price(example_features))