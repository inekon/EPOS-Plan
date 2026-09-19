from geopy.geocoders import Nominatim
from geopy.distance import geodesic

# 1. Datenbank der 15 TRY-Repräsentanzstationen mit ihren Koordinaten
TRY_STATIONS = {
    "01": {"name": "Bremerhaven", "coords": (53.53, 8.58)},
    "02": {"name": "Rostock-Warnemünde", "coords": (54.18, 12.08)},
    "03": {"name": "Hamburg-Fuhlsbüttel", "coords": (53.63, 9.98)},
    "04": {"name": "Potsdam", "coords": (52.38, 13.06)},
    "05": {"name": "Essen", "coords": (51.40, 6.96)},
    "06": {"name": "Bad Marienberg", "coords": (50.65, 7.95)},
    "07": {"name": "Kassel", "coords": (51.30, 9.45)},
    "08": {"name": "Braunlage", "coords": (51.72, 10.60)},
    "09": {"name": "Chemnitz", "coords": (50.79, 12.87)},
    "10": {"name": "Hof", "coords": (50.31, 11.88)},
    "11": {"name": "Fichtelberg", "coords": (50.42, 12.95)},
    "12": {"name": "Mannheim", "coords": (49.47, 8.55)},
    "13": {"name": "Mühldorf/Inn", "coords": (48.24, 12.50)},
    "14": {"name": "Stötten", "coords": (48.66, 9.86)},
    "15": {"name": "Garmisch-Partenkirchen", "coords": (47.48, 11.06)}
}

def finde_region_id(stadt_name):
    geolocator = Nominatim(user_agent="try_region_finder")
    
    # Stadt zu Koordinaten auflösen
    location = geolocator.geocode(stadt_name + ", Germany")
    if not location:
        return None, "Stadt nicht gefunden"
    
    stadt_coords = (location.latitude, location.longitude)
    
    # Nächstgelegene Station finden (Lineare Entfernung)
    beste_region = None
    min_distanz = float('inf')
    
    for region_id, info in TRY_STATIONS.items():
        distanz = geodesic(stadt_coords, info["coords"]).kilometers
        if distanz < min_distanz:
            min_distanz = distanz
            beste_region = region_id
            
    return beste_region, location.address

# Beispielaufruf
stadt = "Dortmund"
region_id, voll_adresse = finde_region_id(stadt)
print(f"Stadt: {voll_adresse}")
print(f"Ermittelte TRY-Region: {region_id} (Repräsentant: {TRY_STATIONS[region_id]['name']})")