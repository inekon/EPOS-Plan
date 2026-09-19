import os
import pandas as pd
from pyproj import Transformer

# --- KONFIGURATION ---
STADT_NAME = "Stuttgart"
LAT, LON = 48.7758, 9.1829  # Koordinaten der Stadt
REGION_ID = "12"           # Region für Südwestdeutschland
PARAM = "air_temperature_2m"
TRY_FILE = f"./TRY_Region_{REGION_ID}/TRY_{REGION_ID}_{PARAM}_2015.dat"

# DWD LCC Projektion (Standard für TRY-Rasterdaten)
DWD_PROJ = '+proj=lcc +lat_1=35 +lat_2=65 +lat_0=52 +lon_0=10 +x_0=0 +y_0=0 +ellps=WGS84 +units=m +no_defs'

def get_pixel_value(lat, lon, file_path):
    # 1. Koordinaten umrechnen (WGS84 -> DWD LCC)
    transformer = Transformer.from_crs("epsg:4326", DWD_PROJ, always_xy=True)
    x_target, y_target = transformer.transform(lon, lat)

    # 2. Header der .dat Datei lesen
    with open(file_path, 'r') as f:
        header = {}
        for _ in range(6):
            line = f.readline().split()
            header[line[0].lower()] = float(line[1])
        
        # 3. Raster-Position berechnen
        col = int((x_target - header['xllcorner']) / header['cellsize'])
        row = int((header['yllcorner'] + (header['nrows'] * header['cellsize']) - y_target) / header['cellsize'])

        # Prüfen, ob Punkt im Raster liegt
        if not (0 <= col < header['ncols'] and 0 <= row < header['nrows']):
            return "Koordinaten liegen außerhalb der Region!"

        # 4. Daten extrahieren
        # Überspringe Header + Zeilen bis zur Ziel-Row
        # Jede Zeile im Raster entspricht einem Pixel mit 8760 Werten
        target_line_idx = row * int(header['ncols']) + col
        
        # Wir nutzen pandas für schnelles Lesen der großen Datei ab der Zielzeile
        # (Alternativ: f.readlines() nutzen, aber das braucht viel RAM)
        data = pd.read_csv(file_path, skiprows=6 + target_line_idx, nrows=1, sep=' ', header=None)
        return data.iloc[0].dropna().values

if __name__ == "__main__":
    if os.path.exists(TRY_FILE):
        werte = get_pixel_value(LAT, LON, TRY_FILE)
        
        # Ergebnisse anzeigen (erste 5 Stunden)
        df_stadt = pd.DataFrame(werte, columns=[PARAM])
        df_stadt.index.name = "Stunde"
        print(f"\nDaten für {STADT_NAME}:")
        print(df_stadt.head())
        
        # Als CSV speichern
        df_stadt.to_csv(f"{STADT_NAME}_{PARAM}_TRY.csv")
    else:
        print(f"Datei {TRY_FILE} nicht gefunden. Bitte zuerst das Download-Skript ausführen.")