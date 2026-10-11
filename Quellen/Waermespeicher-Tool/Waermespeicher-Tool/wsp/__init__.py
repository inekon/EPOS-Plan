"""Wärmespeicher-Tool — Berechnungspaket."""
import os
import sys

# vendor/ (lpagg-DIN4708 u. a.) importierbar machen
_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if _ROOT not in sys.path:
    sys.path.insert(0, _ROOT)

TRY_WEATHER_DIR = os.path.join(_ROOT, "vendor", "try_weather")
