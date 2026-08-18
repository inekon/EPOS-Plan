using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Abweichungserkennung Variante vs. Stamm (Konzept Kap. 4, Baustein 4):
    /// dreistufig — Gewerk vorhanden/nicht vorhanden, andere Komponente,
    /// geänderte Auslegung/Betriebsparameter. Die Feldliste ist deklarativ:
    /// eine Zeile je Vergleichsmerkmal; neue Merkmale kosten genau eine Zeile.
    /// Dieselbe Liste speist die Kenndaten-Tabellen des Komponenten-Bausteins.
    /// </summary>
    public static class AbweichungsErmittler
    {
        /// <summary>Ein Vergleichsmerkmal (Spalte einer Eingabetabelle).</summary>
        public class Merkmal
        {
            public string Gewerk;      // Anzeigegruppe ("Anlage", "Wärmepumpe", …)
            public string Tabelle;     // Quelltabelle (ID_Projekt-gefiltert)
            public string Spalte;      // Spaltenname (tolerant — fehlt sie, wird übersprungen)
            public string Label;       // Anzeigename
            public string Einheit;     // "" wenn keine
            public int Dez;            // Nachkommastellen (-1 = Text, -2 = Ja/Nein)
            public Merkmal(string gewerk, string tabelle, string spalte, string label, string einheit, int dez)
            { Gewerk = gewerk; Tabelle = tabelle; Spalte = spalte; Label = label; Einheit = einheit; Dez = dez; }
        }

        public const int TEXT = -1;
        public const int JN = -2;

        /// <summary>
        /// Deklarative Feldliste (Spaltennamen gegen Kenndaten.accdb verifiziert, 11.08.2026).
        /// </summary>
        public static readonly List<Merkmal> Felder = new List<Merkmal>
        {
            // Anlagenkonfiguration (Tab_Energieanlagen — Anker der Konfiguration)
            new Merkmal("Anlage", "Tab_Energieanlagen", "Betriebsart",        "Betriebsart", "", TEXT),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Vorlauf",            "Vorlauftemperatur", "°C", 0),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Rücklauf",           "Rücklauftemperatur", "°C", 0),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Bivalenter_Betrieb", "Bivalenter Betrieb", "", JN),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Abschaltpunkt",      "Abschaltpunkt", "°C", 1),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Heizstab",           "Heizstab", "", JN),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Grenzleistung",      "Grenzleistung", "kW", 1),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_Leistung",        "PV-Leistung", "kWp", 1),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Neigung",            "Neigung", "°", 0),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Azimut",             "Azimut", "°", 0),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Kollektormodulanzahl","Kollektormodulanzahl", "", 0),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Solaranteil",        "Solaranteil", "%", 0),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Volumen",            "Speichervolumen (Anlage)", "l", 0),
            new Merkmal("Anlage", "Tab_Energieanlagen", "WQ_Typ",             "Wärmequelle", "", TEXT),

            // Wärmepumpe (Tab_WP)
            new Merkmal("Wärmepumpe", "Tab_WP", "Bezeichner",   "Komponente", "", TEXT),
            new Merkmal("Wärmepumpe", "Tab_WP", "Firma",        "Hersteller", "", TEXT),
            new Merkmal("Wärmepumpe", "Tab_WP", "Typ",          "Typ", "", TEXT),
            new Merkmal("Wärmepumpe", "Tab_WP", "Bauart",       "Bauart", "", TEXT),
            new Merkmal("Wärmepumpe", "Tab_WP", "Nennleistung", "Nennleistung", "kW", 0),
            new Merkmal("Wärmepumpe", "Tab_WP", "maxPtherm",    "max. therm. Leistung", "kW", 0),
            new Merkmal("Wärmepumpe", "Tab_WP", "Kuehlleistung","Kühlleistung", "kW", 1),
            new Merkmal("Wärmepumpe", "Tab_WP", "Regelung",     "Regelung", "", TEXT),

            // BHKW (Tab_BHKW)
            new Merkmal("BHKW", "Tab_BHKW", "Bezeichner", "Komponente", "", TEXT),
            new Merkmal("BHKW", "Tab_BHKW", "Firma",      "Hersteller", "", TEXT),
            new Merkmal("BHKW", "Tab_BHKW", "Motortyp",   "Motortyp", "", TEXT),
            new Merkmal("BHKW", "Tab_BHKW", "Ptherm",     "therm. Leistung", "kW", 1),
            new Merkmal("BHKW", "Tab_BHKW", "Pel",        "el. Leistung", "kW", 1),
            new Merkmal("BHKW", "Tab_BHKW", "Wirkungsgrad","Wirkungsgrad", "%", 1),
            new Merkmal("BHKW", "Tab_BHKW", "Vorlauf",    "Vorlauf", "°C", 0),
            new Merkmal("BHKW", "Tab_BHKW", "Ruecklauf",  "Rücklauf", "°C", 0),

            // Spitzenkessel (Tab_Heizkessel)
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Bezeichner",       "Komponente", "", TEXT),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Firma",            "Hersteller", "", TEXT),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Ptherm",           "therm. Leistung", "kW", 1),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Wirkungsgrad_Gas", "Wirkungsgrad Gas", "%", 1),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Wirkungsgrad_Öl",  "Wirkungsgrad Öl", "%", 1),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Brennwert",        "Brennwertnutzung", "", JN),

            // Solarthermie (Tab_Solarkollektoren)
            new Merkmal("Solarthermie", "Tab_Solarkollektoren", "Bezeichner",    "Komponente", "", TEXT),
            new Merkmal("Solarthermie", "Tab_Solarkollektoren", "Kollektortyp",  "Kollektortyp", "", TEXT),
            new Merkmal("Solarthermie", "Tab_Solarkollektoren", "Aperturflaeche","Aperturfläche", "m²", 2),

            // Photovoltaik (Tab_PV)
            new Merkmal("Photovoltaik", "Tab_PV", "Bezeichner",  "Komponente", "", TEXT),
            new Merkmal("Photovoltaik", "Tab_PV", "Firma",       "Hersteller", "", TEXT),
            new Merkmal("Photovoltaik", "Tab_PV", "Leistung",    "Modulleistung", "W", 0),
            new Merkmal("Photovoltaik", "Tab_PV", "Wirkungsgrad","Wirkungsgrad", "%", 1),

            // Pufferspeicher (Tab_Pufferspeicher)
            new Merkmal("Pufferspeicher", "Tab_Pufferspeicher", "Bezeichner",   "Komponente", "", TEXT),
            new Merkmal("Pufferspeicher", "Tab_Pufferspeicher", "Speichertyp",  "Speichertyp", "", TEXT),
            new Merkmal("Pufferspeicher", "Tab_Pufferspeicher", "Gesamtvolumen","Gesamtvolumen", "l", 0),

            // Stromspeicher (Tab_Stromspeicher)
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Bezeichner", "Komponente", "", TEXT),
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Typ",        "Typ", "", TEXT),
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Leistung",   "Leistung", "kW", 1),
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Energie",    "Kapazität", "kWh", 1),

            // Gebäude (Tab_Gebaeude — erstes Gebäude)
            new Merkmal("Gebäude", "Tab_Gebaeude", "Waermebedarf",       "Wärmebedarf", "kWh/a", 0),
            new Merkmal("Gebäude", "Tab_Gebaeude", "Wohnflaeche_gesamt", "Wohn-/Nutzfläche", "m²", 0),
            new Merkmal("Gebäude", "Tab_Gebaeude", "WW_Bedarf",          "Warmwasserbedarf", "kWh/a", 0),
            new Merkmal("Gebäude", "Tab_Gebaeude", "Luftwechselrate",    "Luftwechselrate", "1/h", 2),
        };

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>
        /// Vergleicht die Konfiguration einer Variante gegen den Stamm.
        /// Hinweis: verglichen wird je Tabelle die erste Zeile (ORDER BY ID);
        /// unterschiedliche Einträge-Anzahlen werden als eigene Abweichung gemeldet.
        /// </summary>
        public static List<Abweichung> Vergleiche(ProjektDetails stamm, ProjektDetails variante)
        {
            var liste = new List<Abweichung>();
            if (stamm == null || variante == null) return liste;

            // Stufe 1: Gewerk vorhanden / nicht vorhanden + Anzahl.
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                int nS = stamm.KomponentenAnzahl.ContainsKey(g.Key) ? stamm.KomponentenAnzahl[g.Key] : 0;
                int nV = variante.KomponentenAnzahl.ContainsKey(g.Key) ? variante.KomponentenAnzahl[g.Key] : 0;
                if ((nS > 0) != (nV > 0))
                    liste.Add(new Abweichung
                    {
                        Gewerk = g.Key, Merkmal = "Bestand",
                        WertStamm = nS > 0 ? "vorhanden" : "nicht vorhanden",
                        WertVariante = nV > 0 ? "vorhanden" : "nicht vorhanden"
                    });
                else if (nS != nV)
                    liste.Add(new Abweichung
                    {
                        Gewerk = g.Key, Merkmal = "Anzahl Komponenten",
                        WertStamm = nS.ToString(DE), WertVariante = nV.ToString(DE)
                    });
            }

            // Stufe 2/3: Merkmalsvergleich über die deklarative Feldliste.
            foreach (Merkmal f in Felder)
            {
                DataRow rS = ZeileFuer(stamm, f);
                DataRow rV = ZeileFuer(variante, f);
                if (rS == null && rV == null) continue;            // Gewerk in beiden nicht vorhanden
                if (rS == null || rV == null) continue;            // Bestand bereits in Stufe 1 gemeldet

                string wS = Formatiere(rS, f);
                string wV = Formatiere(rV, f);
                if (!WerteGleich(rS, rV, f))
                    liste.Add(new Abweichung { Gewerk = f.Gewerk, Merkmal = f.Label, WertStamm = wS, WertVariante = wV });
            }

            return liste;
        }

        /// <summary>
        /// Die Datenzeile eines Projekts, aus der ein Merkmal gelesen wird
        /// (null = Gewerk im Projekt nicht vorhanden). Öffentlich, damit die Seite
        /// „Übersicht" des Reiters „Berichte &amp; Kosten" die Komponenten des
        /// Stammprojekts mit derselben Feldliste anzeigen kann wie der Bericht.
        /// </summary>
        public static DataRow ZeileFuer(ProjektDetails d, Merkmal f)
        {
            if (f.Tabelle == "Tab_Energieanlagen")
                return (d.Anlagen != null && d.Anlagen.Rows.Count > 0) ? d.Anlagen.Rows[0] : null;
            if (f.Tabelle == "Tab_Gebaeude")
                return (d.Gebaeude != null && d.Gebaeude.Rows.Count > 0) ? d.Gebaeude.Rows[0] : null;
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                if (g.Value == f.Tabelle)
                    return d.Komponenten.ContainsKey(g.Key) ? d.Komponenten[g.Key] : null;
            return null;
        }

        private static bool WerteGleich(DataRow a, DataRow b, Merkmal f)
        {
            if (f.Dez == TEXT)
                return string.Equals(ProjektDetails.S(a, f.Spalte).Trim(),
                                     ProjektDetails.S(b, f.Spalte).Trim(),
                                     StringComparison.OrdinalIgnoreCase);
            if (f.Dez == JN)
            {
                bool? x = ProjektDetails.B(a, f.Spalte), y = ProjektDetails.B(b, f.Spalte);
                return (x ?? false) == (y ?? false);
            }
            double? u = ProjektDetails.D(a, f.Spalte), v = ProjektDetails.D(b, f.Spalte);
            if (!u.HasValue && !v.HasValue) return true;
            if (!u.HasValue || !v.HasValue) return false;
            double toleranz = Math.Pow(10, -Math.Max(f.Dez, 0)) / 2.0;   // halbe Anzeigestelle
            return Math.Abs(u.Value - v.Value) <= toleranz;
        }

        /// <summary>Formatiert einen Merkmalswert für Tabellen (Abweichung/Kenndaten). null/leer → „—".</summary>
        public static string Formatiere(DataRow r, Merkmal f)
        {
            if (f.Dez == TEXT)
            {
                string s = ProjektDetails.S(r, f.Spalte).Trim();
                return s.Length == 0 ? "—" : s;
            }
            if (f.Dez == JN)
            {
                bool? b = ProjektDetails.B(r, f.Spalte);
                return !b.HasValue ? "—" : (b.Value ? "Ja" : "Nein");
            }
            double? d = ProjektDetails.D(r, f.Spalte);
            if (!d.HasValue) return "—";
            string txt = d.Value.ToString("N" + f.Dez, DE);
            return string.IsNullOrEmpty(f.Einheit) ? txt : txt + " " + f.Einheit;
        }
    }
}
