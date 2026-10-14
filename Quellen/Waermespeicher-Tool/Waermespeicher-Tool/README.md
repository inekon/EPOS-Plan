# Wärmespeicher-Tool

Auslegungstool für **Trinkwarmwasser-Speicher** und **Heizungs-Pufferspeicher**
in Wärmepumpen-Anlagen (EFH/MFH, Wohnungswirtschaft und GHD).
Bedienung über eine Streamlit-Oberfläche im Browser; die Berechnung läuft
vollständig lokal, es werden keine Daten übertragen.

---

## 1. Installation (Windows)

1. **Python 3.10 oder neuer** von <https://www.python.org/downloads/> installieren.
   Bei der Installation den Haken **„Add Python to PATH"** setzen.
2. Den Projektordner an einen Ort mit Schreibrechten kopieren
   (z. B. `C:\Tools\Waermespeicher-Tool`).
3. **`start_tool.bat` doppelklicken.**
   Beim ersten Start legt das Skript eine virtuelle Umgebung `.venv` an und
   installiert die Pakete aus `requirements.txt` (dauert einige Minuten,
   Internetverbindung erforderlich). Danach startet es die Oberfläche.

Der Browser öffnet sich automatisch unter <http://localhost:8501>.
Zum Beenden das schwarze Konsolenfenster schließen.

**Pakete aktualisieren:** `start_tool.bat update` (in der Eingabeaufforderung)
erzwingt eine Neuinstallation der Abhängigkeiten.

### Installation ohne Batch-Datei (Linux/macOS oder manuell)

```bash
python3 -m venv .venv
source .venv/bin/activate          # Windows: .venv\Scripts\activate
pip install -r requirements.txt
streamlit run app.py
```

### Tests

```bash
python -m pytest -q
```

---

## 2. Arbeitsablauf (die fünf Tabs)

In der **Seitenleiste** stehen der Projektname sowie
*Projekt speichern* (JSON-Download mit allen Eingaben) und
*Projekt laden* (JSON-Upload, stellt alle Eingaben wieder her).
Das Lastprofil wird bewusst **nicht** in der Projektdatei gespeichert — nach dem
Laden ist es mit einem Klick neu zu erzeugen.

### 1 · Gebäude & Profil

Zwei Datenquellen zur Auswahl:

* **Synthetisch** — Gebäudetabelle (Name, Kategorie EFH/MFH/GHD, Wohneinheiten,
  Personen je WE, Jahreswärme Heizung/TWW, Anzahl identischer Gebäude,
  Gleichzeitigkeits-Sigma, BDEW-Branche und TWW-Anteil für GHD),
  TRY-Klimaregion (DWD 2010, 1–15), Jahr und Auflösung (1 h oder 15 min).
  *Schaltjahre werden nicht angeboten* — demandlib 0.2.2 kann für VDI 4655
  keine 366-Tage-Jahre rechnen.
* **Gemessen** — Upload einer CSV-/XLSX-/XLSM-Datei, Vorschau der ersten Zeilen,
  Zuordnung von Zeitstempel- und Wertespalte, Einheit (kW oder kWh je Intervall),
  Dezimaltrennzeichen und CSV-Trennzeichen. Liegt nur die Gesamtwärme vor, trennt
  der **Sommer-Baseline-Split** den TWW-Anteil ab (Monate und Korrekturfaktor
  einstellbar). Import- und Lückenreport werden angezeigt.

Nach *Profil erzeugen*: Jahressummen-Metriken, Jahresgang (Tagesmittelwerte) und
Beispielwoche (Woche mit dem lastintensivsten Werktag) sowie die Soll-/Ist-Bilanz
der Jahressummen.

### 2 · Analyse

Jahresdauerlinie mit Markierungslinien für die Leistung, die 90 / 95 / 99 % der
Jahreswärmearbeit deckt (Bivalenzpunkt-Hilfe), Top-20-Lastspitzen, mittlere
Tagesprofile je Monat und Tagtyp (Werktag / Samstag / Sonntag, Feiertage zählen
als Sonntag), Wochenlastgang-Heatmap und die Kennzahlentabelle
(Jahressummen, Spitzenlasten, Vollbenutzungsstunden, TWW-Anteil,
Gleichzeitigkeitsfaktor nach DIN 4708).

### 3 · TWW-Speicher

Eingaben: Speicher- und Kaltwassertemperatur, nutzbarer Volumenanteil,
Ladeleistung der WP im TWW-Betrieb, Zirkulationsverlust, Sicherheitszuschlag.
Wohneinheiten und Belegung werden bei synthetischer Quelle aus der Gebäudetabelle
vorbelegt (Summe WE, personengewichtete Belegung), sonst manuell eingegeben.

Ergebnis: drei Verfahren nebeneinander (DIN 4708, profilbasiert, Faustwert),
das maßgebende Verfahren ist markiert, darunter die auf marktübliche Baugrößen
gerundete Empfehlung sowie Warnungen (z. B. zu kleine Ladeleistung) und der
Hinweis zur Trinkwasserhygiene.

### 4 · Pufferspeicher

Eingaben: WP-Heizleistung und minimale Modulationsleistung, zweiter Erzeuger,
nutzbare Spreizung, Mindestlaufzeit, spezifisches Abtauvolumen, Ziel-Deckungsgrad
und bis zu drei täglich wiederkehrende EVU-Sperrfenster (Start/Dauer).

Ergebnis: vier Kriterien (Abtauung, Taktung, EVU-Sperrzeit, Lastgang-Simulation)
mit Kennzeichnung des maßgebenden, Empfehlung, Deckungsgrad und Unterdeckung der
Simulation sowie der Speicherfüllstands-Verlauf (SOC) in der lastintensivsten
Woche.

### 5 · Export

Erzeugt den Excel-Bericht (Eingaben, Lastgang, JDL, Tagesprofile,
TWW-/Puffer-Auslegung, Kennzahlen mit nativen Excel-Diagrammen) und bietet ihn
zum Download an; der Dateiname enthält Projektname und Datum.

---

## 3. Datenquellen und Methodik (Kurzfassung)

Ausführliche Herleitung, Formeln und Parameterdefaults: **`DESIGN.md`**.

| Baustein | Grundlage |
|---|---|
| Lastprofile Wohnen | VDI 4655 Referenzlastprofile über `demandlib.vdi` |
| Lastprofile GHD | BDEW-Standardlastprofile (Gas/Wärme) über `demandlib.bdew` |
| Wetter | DWD **TRY2010**, 15 Klimaregionen (`vendor/try_weather/`) |
| Messdaten | eigener Import mit Auflösungserkennung, Lückenreport, Sommer-Baseline-Split |
| TWW-Speicher | DIN 4708 (Bedarfskennzahl N, W_z), profilbasierte SOC-Defizitrechnung, Faustwert 30–45 l/(Person·d) nach DIN V 18599-10 / VDI-Praxis |
| Trinkwasserhygiene | DVGW W 551 / VDI 6023 (Großanlage > 400 l), FriWa-Empfehlung bei WP |
| Pufferspeicher | Abtauung (≈ 20 l/kW), Taktung (Mindestlaufzeit), EVU-Sperrzeit, Lastgang-Simulation mit Binärsuche über die Kapazität (Logik analog SWSG-Lastgangauswertung) |
| Umrechnung | c_w = 1,163 Wh/(l·K); V [l] = Q [kWh] · 1000 / (1,163 · ΔT [K]) |

Empfehlungen werden auf marktübliche Speichergrößen
(100/150/200/300/400/500/800/1000/1500/2000/3000/5000/8000/10000 l) aufgerundet.

**Grenzen der Ergebnisse:** Die Bedarfskennzahl N wird vereinfacht
(v = w = 1, Einheitswohnung p = 3,5) bestimmt; die Speichersimulation ist eine
reine Energiebilanz ohne Schichtungs-/Temperaturmodell. Die Ergebnisse ersetzen
keine Anlagenplanung, sondern sind eine normbasierte Vorbemessung.

---

## 4. Fremdcode und Lizenzen

Das Tool nutzt zwei Open-Source-Projekte, beide unter **MIT-Lizenz**:

* **lpagg** — Load Profile Aggregator, J. Nettelstroth / Joris Zimmermann,
  <https://github.com/jnettels/lpagg>, MIT.
  Übernommen (vendored): `vendor/din4708.py` (DIN-4708-Kennwerte NL-Zahl,
  Gleichzeitigkeitsfaktor) und `vendor/try_weather/` (DWD-TRY2010-Wetterdateien).
  Lizenztext: `vendor/LICENSE_lpagg.txt`.
* **demandlib** — oemof developer group,
  <https://github.com/oemof/demandlib>, MIT (PyPI: `demandlib==0.2.2`).
  Liefert die VDI-4655- und BDEW-Profilgeneratoren.

Weitere Abhängigkeiten: streamlit (Apache-2.0), pandas (BSD-3-Clause),
numpy (BSD-3-Clause), plotly (MIT), openpyxl (MIT), holidays (MIT).

Die DWD-Testreferenzjahre 2010 stammen vom Deutschen Wetterdienst und werden
unverändert weiterverwendet. Normtexte (DIN 4708, DIN V 18599-10, VDI 4655,
VDI 6002, DVGW W 551) sind **nicht** Bestandteil dieses Projekts — es werden
lediglich die daraus abgeleiteten Rechenverfahren implementiert.

---

## 5. Bekannte Einschränkungen

* Schaltjahre sind für synthetische Profile gesperrt (Begrenzung von
  demandlib 0.2.2 / VDI 4655).
* Die 15-min-Auflösung erhöht Rechenzeit und Speicherbedarf deutlich; für die
  Speicherauslegung reicht in der Regel die Stundenauflösung.
* Der Excel-Export benötigt das Modul `wsp/export_excel.py`; fehlt es, zeigt der
  Export-Tab eine Fehlermeldung an, alle übrigen Funktionen bleiben nutzbar.
* Gemessene Lastgänge werden stets auf 1-h-Werte umgerechnet.
