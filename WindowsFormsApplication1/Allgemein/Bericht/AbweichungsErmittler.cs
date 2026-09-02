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
            public string Tabelle;     // Quelltabelle (projektbezogen, siehe ProjektDetails)
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
        /// Die Texte der Stufe 1 als Konstanten, nicht als Literale an zwei Stellen:
        /// Die Seite „Übersicht" zeigt dieselben Bestandszeilen inzwischen auch OHNE
        /// Vergleichspartner (Gegenüberstellung Stamm ↔ Varianten). Zwei Schreibweisen
        /// derselben Kennzahl wären für den Leser zwei Kennzahlen.
        /// </summary>
        public const string MERKMAL_BESTAND = "Bestand";
        public const string MERKMAL_ANZAHL = "Anzahl Komponenten";
        public const string BESTAND_VORHANDEN = "vorhanden";
        public const string BESTAND_FEHLT = "nicht vorhanden";

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
            // Paket A des PV-Ertragsmodells (Stufe E1.3), Migrationsschritt 62.
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_WrWirkungsgrad",  "Wechselrichter-Wirkungsgrad", "", 2),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_Systemverluste",  "Systemverluste", "%", 1),
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
        /// Hinweis: verglichen wird je Gewerk die erste Komponente des Projekts
        /// (erste Anlagenzeile — <c>ProjektDetails.LadeGewerk</c>); unterschiedliche
        /// Einträge-Anzahlen werden als eigene Abweichung gemeldet.
        /// </summary>
        public static List<Abweichung> Vergleiche(ProjektDetails stamm, ProjektDetails variante)
        {
            var liste = new List<Abweichung>();
            if (stamm == null || variante == null) return liste;

            // Stufe 1: Gewerk vorhanden / nicht vorhanden + Anzahl.
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                int nS = Anzahl(stamm, g.Key);
                int nV = Anzahl(variante, g.Key);
                if ((nS > 0) != (nV > 0))
                    liste.Add(new Abweichung
                    {
                        Gewerk = g.Key, Merkmal = MERKMAL_BESTAND,
                        WertStamm = nS > 0 ? BESTAND_VORHANDEN : BESTAND_FEHLT,
                        WertVariante = nV > 0 ? BESTAND_VORHANDEN : BESTAND_FEHLT
                    });
                else if (nS != nV)
                    liste.Add(new Abweichung
                    {
                        Gewerk = g.Key, Merkmal = MERKMAL_ANZAHL,
                        WertStamm = nS.ToString(DE), WertVariante = nV.ToString(DE)
                    });
            }

            // Stufe 2/3: Merkmalsvergleich über die deklarative Feldliste.
            // Artefakt-Guard (Nutzerbefund 28.08.2026): Der Anlage-Block vergleicht
            // die jeweils erste ECHTE Anlagenzeile — führen Stamm und Variante dort
            // verschiedene Gewerke, entfallen seine Merkmalszeilen (siehe
            // AnlagenVergleichbar); Referenzanlagen zählen nie (ErsteEchteAnlage).
            bool anlagenVergleichbar = AnlagenVergleichbar(stamm, variante);
            foreach (Merkmal f in Felder)
            {
                if (f.Tabelle == "Tab_Energieanlagen" && !anlagenVergleichbar) continue;
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
        /// Anzahl der Komponenten EINES GEWERKS im Projekt — die Kennzahl der
        /// Stufe-1-Zeile „Anzahl Komponenten". Quelle ist
        /// <see cref="ProjektDetails.KomponentenAnzahl"/>, also der über
        /// <c>Tab_Energieanlagen</c> ermittelte VERBAUTE Bestand
        /// (<c>ProjektDetails.LadeGewerk</c>) — NICHT der rohe Zeilenbestand der
        /// Gerätetabelle, der auch Altkopien führt, auf die keine Anlage mehr zeigt.
        /// Öffentlich, damit die Gegenüberstellung der Seite „Übersicht" dieselbe
        /// Kennzahl aus derselben Quelle liest wie die Unterschiedsanzeige.
        /// </summary>
        public static int Anzahl(ProjektDetails d, string gewerk)
        {
            return (d != null && gewerk != null && d.KomponentenAnzahl.ContainsKey(gewerk))
                ? d.KomponentenAnzahl[gewerk] : 0;
        }

        /// <summary>
        /// Anzeigetext der Anzahlzeile: die Zahl — bei 0 das „nicht vorhanden" der
        /// Bestandszeile. Eine „0" allein wäre in einer Gegenüberstellung ohne
        /// Vergleichspartner nicht von einer ungezählten Zelle zu unterscheiden.
        /// </summary>
        public static string AnzahlText(int n)
        {
            return n > 0 ? n.ToString(DE) : BESTAND_FEHLT;
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
                return ErsteEchteAnlage(d);
            if (f.Tabelle == "Tab_Gebaeude")
                return (d.Gebaeude != null && d.Gebaeude.Rows.Count > 0) ? d.Gebaeude.Rows[0] : null;
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                if (g.Value == f.Tabelle)
                    return d.Komponenten.ContainsKey(g.Key) ? d.Komponenten[g.Key] : null;
            return null;
        }

        /// <summary>Die n-te Komponentenzeile eines Gewerks (null wenn nicht vorhanden) —
        /// Grundlage der „eine Zeile je Komponente"-Gegenüberstellung (28.08.2026).</summary>
        public static DataRow KomponenteZeile(ProjektDetails d, string gewerk, int index)
        {
            if (d == null || gewerk == null || index < 0) return null;
            DataTable dt;
            if (!d.KomponentenAlle.TryGetValue(gewerk, out dt) || dt == null) return null;
            return index < dt.Rows.Count ? dt.Rows[index] : null;
        }

        /// <summary>Das Bezeichner-Merkmal eines Gewerks (Label „Komponente" der
        /// deklarativen Feldliste) — liefert der Komponentenzeile ihren Namen.</summary>
        public static Merkmal BezeichnerMerkmal(string gewerk)
        {
            foreach (Merkmal f in Felder)
                if (f.Gewerk == gewerk && f.Spalte == "Bezeichner") return f;
            return null;
        }

        /// <summary>
        /// Merkmalstext einer Komponentenzeile über die deklarative Feldliste ihres
        /// Gewerks — je belegtem Merkmal „Label: Wert"; das Bezeichner-Merkmal bleibt
        /// außen vor (es steht bereits in der Zelle). Eine Wahrheit für Mouse-over
        /// und Auswahlanzeige der Gegenüberstellung (Nutzerauftrag 28.08.2026).
        /// </summary>
        public static string MerkmaleText(DataRow r, string gewerk, string trenner)
        {
            if (r == null) return "";
            var teile = new List<string>();
            foreach (Merkmal f in Felder)
            {
                if (f.Gewerk != gewerk || f.Spalte == "Bezeichner") continue;
                if (!r.Table.Columns.Contains(f.Spalte)) continue;
                string w = Formatiere(r, f);
                if (string.IsNullOrEmpty(w) || w == "—") continue;
                teile.Add(f.Label + ": " + w);
            }
            return string.Join(trenner, teile);
        }

        /// <summary>
        /// Die erste ECHTE Anlagenzeile eines Projekts — Referenzanlagen
        /// (<c>WizardItemClass.REF_KESSEL_TYP</c>…<c>REF_PV_TYP</c>) bleiben außen
        /// vor: Sie sind im Bereich Energieerzeuger nicht als Projektanlage
        /// angelegt und lieferten dem Anlage-Block sonst Artefaktwerte
        /// (Nutzerbefund 28.08.2026).
        /// </summary>
        public static DataRow ErsteEchteAnlage(ProjektDetails d)
        {
            if (d == null || d.Anlagen == null) return null;
            foreach (DataRow r in d.Anlagen.Rows)
            {
                int typ = (int)(ProjektDetails.D(r, "ID_Type") ?? 0);
                if (typ < WizardItemClass.REF_KESSEL_TYP || typ > WizardItemClass.REF_PV_TYP)
                    return r;
            }
            return null;
        }

        /// <summary>
        /// true, wenn die Anlage-Merkmale zweier Projekte vergleichbar sind: Beide
        /// führen eine echte Anlagenzeile DESSELBEN Gewerks (ID_Type). Ein
        /// WP-Stamm gegen eine BHKW-Variante verglich sonst Äpfel mit Birnen —
        /// die Tabelle zeigte „Anlage"-Unterschiede einer Anlage, die es im
        /// Bereich Energieerzeuger des Partners gar nicht gibt; der
        /// Systemunterschied steht bereits in den Bestandszeilen der Stufe 1.
        /// </summary>
        public static bool AnlagenVergleichbar(ProjektDetails a, ProjektDetails b)
        {
            DataRow ra = ErsteEchteAnlage(a), rb = ErsteEchteAnlage(b);
            if (ra == null || rb == null) return false;
            return (int)(ProjektDetails.D(ra, "ID_Type") ?? 0) ==
                   (int)(ProjektDetails.D(rb, "ID_Type") ?? 0);
        }

        /// <summary>
        /// true, wenn ALLE Versionen mit echter Anlagenzeile dasselbe Gewerk
        /// führen (für die Gegenüberstellung der Übersicht — sonst stünden Werte
        /// verschiedener Gewerke nebeneinander in einer „Anlage"-Zeile).
        /// </summary>
        public static bool AnlagenEinheitlich(IEnumerable<ProjektDetails> versionen)
        {
            if (versionen == null) return false;
            int typ = 0;
            foreach (ProjektDetails d in versionen)
            {
                DataRow r = ErsteEchteAnlage(d);
                if (r == null) continue;
                int t = (int)(ProjektDetails.D(r, "ID_Type") ?? 0);
                if (typ == 0) typ = t;
                else if (typ != t) return false;
            }
            return typ != 0;
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
