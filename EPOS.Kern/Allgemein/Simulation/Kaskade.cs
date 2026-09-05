using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// iU9-W10b.0b — die KASKADENBELEGUNG eines Projekts: vier Plaetze
    /// (<c>Tab_Einstellungen.Tool_1..4</c>) und die beiden Stromplaetze
    /// (<c>Tool_5</c>, <c>Tool_6</c>).
    ///
    /// <para><b>Woher sie kommt.</b> Bis W10b bedienten sechs unsichtbare Auswahlfelder
    /// samt Haken dieses Modell — die Karten der Simulationskonfiguration waren eine
    /// ANSICHT auf sie (<c>Form_Simulation_Config.Karten.cs</c>:412-603). Ohne
    /// WinForms gibt es diese Steuerelemente nicht mehr; die Zuordnung „Platz →
    /// <c>Tool_n</c>" steht deshalb hier, unmittelbar auf dem
    /// <see cref="KonfigurationModel"/> — dem Satz, den <c>KonfigurationCtrl</c>
    /// liest und schreibt. Es bleibt bei EINER Wahrheit ueber die Kaskadenposition;
    /// <c>Ladeordnung.Kaskadenpositionen</c> liest sie als Sortierkriterium der
    /// Ladereihenfolge (Konzept 3.4).</para>
    ///
    /// <para><b>Rein rechnend, ohne Datenbank.</b> Jede Methode nimmt das Modell und
    /// gibt es veraendert zurueck; geschrieben wird ausschliesslich ueber
    /// <c>KonfigurationCtrl.Delete</c>/<c>Insert</c> — genau wie bisher der
    /// Speichern-Knopf.</para>
    /// </summary>
    public static class Kaskade
    {
        /// <summary>Zahl der Waermeerzeuger-Plaetze (Tool_1..4).</summary>
        public const int PLAETZE = 4;

        /// <summary>Der Platz des Stromerzeugers (Tool_5).</summary>
        public const int PLATZ_STROMERZEUGER = 5;

        /// <summary>Der Platz des Energiespeichers (Tool_6).</summary>
        public const int PLATZ_ENERGIESPEICHER = 6;

        // ==================================================================== Lesen

        /// <summary>
        /// Die vier Waermeerzeuger-Plaetze in ihrer Reihenfolge; leerer Eintrag = frei
        /// (Vorlaeufer <c>KaskadeLesen</c>:426-436).
        /// </summary>
        public static List<string> Lesen(KonfigurationModel k)
        {
            List<string> plaetze = new List<string>();
            if (k == null)
            {
                for (int i = 0; i < PLAETZE; i++) plaetze.Add("");
                return plaetze;
            }

            plaetze.Add(k.m_Tool_1 ?? "");
            plaetze.Add(k.m_Tool_2 ?? "");
            plaetze.Add(k.m_Tool_3 ?? "");
            plaetze.Add(k.m_Tool_4 ?? "");
            return plaetze;
        }

        /// <summary>
        /// Schreibt die vier Plaetze zurueck (Vorlaeufer <c>KaskadeSchreiben</c>:439-465).
        /// Eine kuerzere Liste laesst die uebrigen Plaetze leer.
        /// </summary>
        public static void Schreiben(KonfigurationModel k, IList<string> plaetze)
        {
            if (k == null) return;

            k.m_Tool_1 = Platz(plaetze, 0);
            k.m_Tool_2 = Platz(plaetze, 1);
            k.m_Tool_3 = Platz(plaetze, 2);
            k.m_Tool_4 = Platz(plaetze, 3);
        }

        private static string Platz(IList<string> plaetze, int i)
        {
            if (plaetze == null || i >= plaetze.Count) return "";
            return plaetze[i] ?? "";
        }

        /// <summary>Die BELEGTEN Plaetze in ihrer Reihenfolge, ohne Wiederholung.</summary>
        public static List<string> Belegt(KonfigurationModel k)
        {
            List<string> belegt = new List<string>();
            foreach (string wert in Lesen(k))
                if (!string.IsNullOrEmpty(wert) && !belegt.Contains(wert)) belegt.Add(wert);
            return belegt;
        }

        // ==================================================================== Aendern

        /// <summary>
        /// Verschiebt einen Erzeuger um einen Rang (<paramref name="richtung"/> −1 = nach
        /// vorn, +1 = nach hinten); <c>false</c> = nichts geaendert.
        ///
        /// <para><b>Getauscht werden PLATZINHALTE, verdichtet wird nicht</b> (woertlich
        /// <c>KaskadeVerschieben</c>:480-506). <c>Ladeordnung.Kaskadenpositionen</c>
        /// liest die SPALTENNUMMER <c>Tool_1..4</c> als Kaskadenposition. Wuerde beim
        /// Verschieben eine Luecke geschlossen — etwa Tool_1 leer, Tool_2 belegt —,
        /// aenderten sich Positionen, die niemand angefasst hat.</para>
        /// </summary>
        public static bool Verschieben(KonfigurationModel k, string dbWert, int richtung)
        {
            if (k == null || string.IsNullOrEmpty(dbWert)) return false;

            List<string> plaetze = Lesen(k);

            List<int> belegt = new List<int>();
            for (int i = 0; i < plaetze.Count; i++)
                if (!string.IsNullOrEmpty(plaetze[i])) belegt.Add(i);

            int rang = -1;
            for (int i = 0; i < belegt.Count; i++)
                if (string.Equals(plaetze[belegt[i]], dbWert, StringComparison.Ordinal))
                {
                    rang = i;
                    break;
                }

            int ziel = rang + richtung;
            if (rang < 0 || ziel < 0 || ziel >= belegt.Count) return false;

            string merker = plaetze[belegt[rang]];
            plaetze[belegt[rang]] = plaetze[belegt[ziel]];
            plaetze[belegt[ziel]] = merker;

            Schreiben(k, plaetze);
            return true;
        }

        /// <summary>
        /// Nimmt einen Waermeerzeuger in die Simulation auf — das „+ aufnehmen" der
        /// verfuegbaren Karte (woertlich <c>KaskadeAufnehmen</c>:528-551).
        ///
        /// <para>Genommen wird der erste freie Platz HINTER dem letzten belegten; damit
        /// erscheint die Karte am Ende der Kaskade. Erst wenn dort keiner frei ist, wird
        /// eine Luecke weiter vorn gefuellt — vier Plaetze fuer vier Erzeugertypen, es
        /// bleibt also immer einer uebrig.</para>
        /// </summary>
        public static bool Aufnehmen(KonfigurationModel k, string dbWert)
        {
            if (k == null || string.IsNullOrEmpty(dbWert)) return false;

            List<string> plaetze = Lesen(k);
            if (plaetze.Contains(dbWert)) return false;   // schon aufgenommen

            int letzterBelegt = -1;
            for (int i = 0; i < plaetze.Count; i++)
                if (!string.IsNullOrEmpty(plaetze[i])) letzterBelegt = i;

            int ziel = -1;
            for (int i = letzterBelegt + 1; i < plaetze.Count; i++)
                if (string.IsNullOrEmpty(plaetze[i])) { ziel = i; break; }

            if (ziel < 0)
                for (int i = 0; i < plaetze.Count; i++)
                    if (string.IsNullOrEmpty(plaetze[i])) { ziel = i; break; }

            if (ziel < 0) return false;   // alle vier Plaetze belegt

            plaetze[ziel] = dbWert;
            Schreiben(k, plaetze);
            return true;
        }

        /// <summary>
        /// Nimmt einen Waermeerzeuger aus der Simulation — das „×" der aufgenommenen
        /// Karte (woertlich <c>KaskadeEntfernen</c>:559-573). Der Platz wird leer, alle
        /// uebrigen bleiben, wo sie sind (keine Verdichtung).
        /// </summary>
        public static bool Entfernen(KonfigurationModel k, string dbWert)
        {
            if (k == null || string.IsNullOrEmpty(dbWert)) return false;

            List<string> plaetze = Lesen(k);
            bool getroffen = false;
            for (int i = 0; i < plaetze.Count; i++)
                if (string.Equals(plaetze[i], dbWert, StringComparison.Ordinal))
                {
                    plaetze[i] = "";
                    getroffen = true;
                }

            if (getroffen) Schreiben(k, plaetze);
            return getroffen;
        }

        /// <summary>
        /// Setzt den Auswahlplatz der Strom- bzw. Speicherseite (<c>Tool_5</c>,
        /// <c>Tool_6</c>); leerer <paramref name="dbWert"/> = nicht aufnehmen
        /// (woertlich <c>StromAuswahlSetzen</c>:583-603).
        /// </summary>
        public static void StromAuswahl(KonfigurationModel k, int platz, string dbWert)
        {
            if (k == null) return;

            string wert = dbWert ?? "";
            if (platz == PLATZ_STROMERZEUGER) k.m_Tool_5 = wert;
            else if (platz == PLATZ_ENERGIESPEICHER) k.m_Tool_6 = wert;
        }

        /// <summary>Der belegte Wert eines Stromplatzes; "" = frei.</summary>
        public static string StromWert(KonfigurationModel k, int platz)
        {
            if (k == null) return "";
            if (platz == PLATZ_STROMERZEUGER) return k.m_Tool_5 ?? "";
            if (platz == PLATZ_ENERGIESPEICHER) return k.m_Tool_6 ?? "";
            return "";
        }

        // ==================================================================== Ableitung

        /// <summary>
        /// Die Liste der gerechneten Erzeuger — die vier belegten Waermeplaetze, entdoppelt,
        /// und IMMER <c>DbWerte.ERZEUGER_GESAMTSYSTEM</c> am Ende
        /// (woertlich <c>AddErzeuger</c>:457-500).
        ///
        /// <para><b>Befund W10b-B41.</b> Im Vorlaeufer landete diese Liste im Feld
        /// <c>listErzeuger</c>, und dieses Feld hatte KEINEN Leser — die Methode wirkte
        /// allein ueber ihren Nachlauf (den Neuaufbau der Karten). Die Ableitung bleibt
        /// hier trotzdem stehen: Sie ist die dokumentierte Bedeutung von „welche
        /// Technologien rechnet dieses Projekt", und sie ist ohne Oberflaeche
        /// pruefbar.</para>
        /// </summary>
        public static List<string> Erzeugerliste(KonfigurationModel k)
        {
            List<string> liste = new List<string>();
            foreach (string wert in Lesen(k))
                if (!string.IsNullOrEmpty(wert) && !liste.Contains(wert)) liste.Add(wert);

            if (!liste.Contains(DbWerte.ERZEUGER_GESAMTSYSTEM))
                liste.Add(DbWerte.ERZEUGER_GESAMTSYSTEM);

            return liste;
        }

        /// <summary><c>Tab_Energieanlagen.ID_Type</c> zu einem Waermeerzeuger-DB-Wert; 0 = unbekannt.</summary>
        public static int TypZuAnlagentyp(string dbWert)
        {
            switch (dbWert)
            {
                case DbWerte.ERZEUGER_WAERMEPUMPE: return WizardItemClass.WP_TYP;
                case DbWerte.ERZEUGER_HEIZKESSEL: return WizardItemClass.KESSEL_TYP;
                case DbWerte.ERZEUGER_BHKW: return WizardItemClass.BHKW_TYP;
                case DbWerte.ERZEUGER_SOLARTHERMIE: return WizardItemClass.SOLAR_TYP;
                default: return 0;
            }
        }
    }
}
