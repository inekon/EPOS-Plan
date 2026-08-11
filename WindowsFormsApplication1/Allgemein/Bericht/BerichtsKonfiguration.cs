using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auswahl und Optionen eines Berichtslaufs (Konzept Kap. 3/8.4).
    /// Wird als JSON in der Tabelle Berichtskonfiguration je Stammprojekt gespeichert
    /// (BerichtCtrl) und beim nächsten Öffnen des Dialogs vorbelegt.
    /// </summary>
    public class BerichtsKonfiguration
    {
        // --- Baustein-Schlüssel (stabil, nicht mehr ändern — sie stehen in der DB) ---
        public const string B_DECKBLATT = "deckblatt";
        public const string B_INHALT = "inhaltsverzeichnis";
        public const string B_PROJEKT = "projektbeschreibung";
        public const string B_KOMPONENTEN = "komponenten";        // inkl. Abweichungen je Variante
        public const string B_ERGEBNISSE = "ergebnisse";          // je Variante, inkl. Ganglinien
        public const string B_VERGLEICH = "vergleich";            // Kennzahlen + Balkendiagramme
        public const string B_WIRTSCHAFT = "wirtschaftlichkeit";
        public const string B_ANHANG = "anhang";

        /// <summary>Ein wählbarer Berichtsbaustein (Reihenfolge = Berichtsreihenfolge).</summary>
        public class BausteinDef
        {
            public string Schluessel;
            public string Titel;
            public bool Standard;      // im Neuzustand angehakt?
            public bool NurWord;       // bei reiner Excel-Ausgabe ohne Wirkung
            public BausteinDef(string schluessel, string titel, bool standard, bool nurWord)
            { Schluessel = schluessel; Titel = titel; Standard = standard; NurWord = nurWord; }
        }

        /// <summary>Katalog aller Bausteine in Berichtsreihenfolge (Konzept Kap. 4).</summary>
        public static readonly BausteinDef[] AlleBausteine = new BausteinDef[]
        {
            new BausteinDef(B_DECKBLATT,   "Deckblatt",                          true,  true),
            new BausteinDef(B_INHALT,      "Inhaltsverzeichnis",                 true,  true),
            new BausteinDef(B_PROJEKT,     "Projektbeschreibung",                true,  false),
            new BausteinDef(B_KOMPONENTEN, "Komponenten & Varianten",            true,  false),
            new BausteinDef(B_ERGEBNISSE,  "Ergebnisse je Variante",             true,  false),
            new BausteinDef(B_VERGLEICH,   "Variantenvergleich",                 true,  false),
            new BausteinDef(B_WIRTSCHAFT,  "Wirtschaftlichkeit",                 false, false),
            new BausteinDef(B_ANHANG,      "Anhang",                             true,  true),
        };

        // --- gespeicherte Auswahl ---

        /// <summary>Projekt-IDs der gewählten Varianten (ohne Stamm — der ist immer dabei).</summary>
        public List<int> VariantenIds { get; set; } = new List<int>();

        /// <summary>Schlüssel der aktiven Bausteine.</summary>
        public List<string> AktiveBausteine { get; set; } = new List<string>();

        /// <summary>Vor Ausgabe neu rechnen (für Ganglinien ohnehin erforderlich, Kap. 6.2).</summary>
        public bool NeuRechnen { get; set; } = true;

        /// <summary>"Word" | "Excel" | "Beide".</summary>
        public string Ausgabe { get; set; } = "Word";

        /// <summary>Zielordner der Ausgabedateien (leer = Dokumente-Ordner).</summary>
        public string ZielOrdner { get; set; } = "";

        /// <summary>Standardkonfiguration (Bausteine laut Katalog-Standard).</summary>
        public static BerichtsKonfiguration Standard()
        {
            BerichtsKonfiguration k = new BerichtsKonfiguration();
            foreach (BausteinDef b in AlleBausteine)
                if (b.Standard) k.AktiveBausteine.Add(b.Schluessel);
            return k;
        }

        public bool IstAktiv(string schluessel)
        { return AktiveBausteine != null && AktiveBausteine.Contains(schluessel); }

        // --- (De-)Serialisierung ---

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        { WriteIndented = false };

        public string NachJson()
        { return JsonSerializer.Serialize(this, _json); }

        /// <summary>Tolerant: ungültiges/leeres JSON liefert die Standardkonfiguration.</summary>
        public static BerichtsKonfiguration AusJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Standard();
            try
            {
                BerichtsKonfiguration k = JsonSerializer.Deserialize<BerichtsKonfiguration>(json, _json);
                return k ?? Standard();
            }
            catch { return Standard(); }
        }
    }
}
