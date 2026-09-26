using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        /// <summary>
        /// Vorsilbe der Ressourcen mit dem Titel eines Häkchens: <c>BK_BER_BAUSTEIN_</c> + Schlüssel in
        /// Großbuchstaben, etwa <c>BK_BER_BAUSTEIN_ERGEBNISSE</c> (Konzept Berichtsvorlagen 5.5).
        /// </summary>
        public const string PRAEFIX_TITEL = "BK_BER_BAUSTEIN_";

        /// <summary>Ein wählbarer Berichtsbaustein (Reihenfolge = Berichtsreihenfolge).</summary>
        public class BausteinDef
        {
            public string Schluessel;

            /// <summary>Der deutsche Titel des Häkchens — bleibt, wie er war; zweisprachig über <see cref="TitelIn"/>.</summary>
            public string Titel;
            public bool Standard;      // im Neuzustand angehakt?
            public bool NurWord;       // bei reiner Excel-Ausgabe ohne Wirkung
            public BausteinDef(string schluessel, string titel, bool standard, bool nurWord)
            { Schluessel = schluessel; Titel = titel; Standard = standard; NurWord = nurWord; }

            /// <summary>Der Ressourcenschlüssel des Titels (<see cref="PRAEFIX_TITEL"/> + Schlüssel in Großbuchstaben).</summary>
            public string TitelId { get { return PRAEFIX_TITEL + (Schluessel ?? "").ToUpperInvariant(); } }

            /// <summary>
            /// Der Titel des Häkchens in der gewählten Sprache aus <c>MyResource</c>
            /// (<see cref="TitelId"/>); fehlt die Ressource, der deutsche <see cref="Titel"/>.
            /// </summary>
            public string TitelIn(bool englisch)
            {
                string text = null;
                try { text = MyResource.Resource.ResourceManager.GetString(TitelId, BerichtTexte.KulturFuer(englisch)); }
                catch (Exception) { text = null; }
                return string.IsNullOrEmpty(text) ? Titel : text;
            }
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

        /// <summary>
        /// Der Titel des Häkchens <paramref name="schluessel"/> in der gewählten Sprache
        /// (<see cref="BausteinDef.TitelIn"/>); ein unbekannter Schlüssel steht für sich.
        /// </summary>
        public static string Titel(string schluessel, bool englisch)
        {
            foreach (BausteinDef b in AlleBausteine)
                if (string.Equals(b.Schluessel, schluessel, StringComparison.Ordinal)) return b.TitelIn(englisch);
            return schluessel ?? "";
        }

        // --- gespeicherte Auswahl ---

        /// <summary>Projekt-IDs der gewählten Varianten (ohne Stamm — der ist immer dabei).</summary>
        public List<int> VariantenIds { get; set; } = new List<int>();

        /// <summary>Schlüssel der aktiven Bausteine.</summary>
        public List<string> AktiveBausteine { get; set; } = new List<string>();

        /// <summary>
        /// Ohne Wirkung seit 15.08.2026: jeder Berichtslauf simuliert alle gewählten
        /// Projekte neu und rechnet danach die Wirtschaftlichkeit
        /// (BerichtsDatenSammler.SammleFuerBericht). Das Feld bleibt nur erhalten,
        /// damit bereits gespeicherte Konfigurations-JSONs weiter lesbar sind.
        /// </summary>
        public bool NeuRechnen { get; set; } = true;

        /// <summary>"Word" | "Excel" | "Beide".</summary>
        public string Ausgabe { get; set; } = "Word";

        /// <summary>Zielordner der Ausgabedateien (leer = Dokumente-Ordner).</summary>
        public string ZielOrdner { get; set; } = "";

        // --- Vorlagenwahl je Stammprojekt (Konzept Berichtsvorlagen 10.3, Zeile „Abweichung“) ---

        /// <summary>Quelle der Word-Vorlage: die mitgelieferte Standardvorlage.</summary>
        public const string VORLAGE_QUELLE_STANDARD = "standard";

        /// <summary>Quelle der Word-Vorlage: eine eigene Vorlage im Vorlagenordner.</summary>
        public const string VORLAGE_QUELLE_EIGEN = "eigen";

        /// <summary>
        /// Abweichende Word-Vorlage dieses Stammprojekts: <see cref="VORLAGE_QUELLE_STANDARD"/> oder
        /// <see cref="VORLAGE_QUELLE_EIGEN"/>; <c>null</c> = keine Abweichung, es gilt die Vorgabe der
        /// Installation (<c>BerichtsvorlagenCtrl</c>). Eine Datei, keine Datenbankzeile: Wählen,
        /// Auflösen und der Rückfall bei fehlender Datei stehen im <c>BerichtsvorlagenCtrl</c>.
        ///
        /// <para><b>Tolerant gelesen:</b> Ein Wert anderer Art (Zahl, Objekt) setzt nicht die ganze
        /// Konfiguration auf den Standard zurück, sondern wird zu Text bzw. <c>null</c>;
        /// ohne Abweichung wird das Feld nicht geschrieben — ältere Fassungen lesen das JSON
        /// unverändert.</para>
        /// </summary>
        [JsonConverter(typeof(TolerantTextKonverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string VorlageWordQuelle { get; set; }

        /// <summary>
        /// Dateiname der eigenen Word-Vorlage im Vorlagenordner (ohne Pfad), wenn
        /// <see cref="VorlageWordQuelle"/> <see cref="VORLAGE_QUELLE_EIGEN"/> ist; sonst <c>null</c>.
        /// </summary>
        [JsonConverter(typeof(TolerantTextKonverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string VorlageWordDatei { get; set; }

        /// <summary>Quelle der Excel-Vorlage: ausdrücklich ohne Vorlage — die heutige Mappe aus dem Code (Konzept 7.1).</summary>
        public const string VORLAGE_QUELLE_OHNE = "ohne";

        /// <summary>
        /// Abweichende Excel-Vorlage dieses Stammprojekts (Konzept Berichtsvorlagen 10.3, Etappe BV-E7):
        /// <see cref="VORLAGE_QUELLE_OHNE"/> oder <see cref="VORLAGE_QUELLE_EIGEN"/>; <c>null</c> = keine
        /// Abweichung, es gilt die Vorgabe der Installation (Einstellung <c>BerichtVorlageExcel</c>). Tolerant
        /// gelesen und ohne Abweichung nicht geschrieben wie <see cref="VorlageWordQuelle"/>.
        /// </summary>
        [JsonConverter(typeof(TolerantTextKonverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string VorlageExcelQuelle { get; set; }

        /// <summary>Dateiname der eigenen Excel-Vorlage im Vorlagenordner (ohne Pfad), wenn
        /// <see cref="VorlageExcelQuelle"/> <see cref="VORLAGE_QUELLE_EIGEN"/> ist; sonst <c>null</c>.</summary>
        [JsonConverter(typeof(TolerantTextKonverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string VorlageExcelDatei { get; set; }

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

        /// <summary>
        /// Liest ein Textfeld duldsam: Text bleibt Text, eine Zahl oder ein Wahrheitswert wird zu Text,
        /// ein Objekt oder eine Liste wird übersprungen und zu <c>null</c> — ein falsch geschriebenes
        /// neues Feld kostet so nicht die übrige Auswahl.
        /// </summary>
        private sealed class TolerantTextKonverter : JsonConverter<string>
        {
            public override bool HandleNull { get { return true; } }

            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return reader.GetString();
                    case JsonTokenType.Number:
                        return reader.TryGetInt64(out long ganz)
                            ? ganz.ToString(CultureInfo.InvariantCulture)
                            : reader.GetDouble().ToString("R", CultureInfo.InvariantCulture);
                    case JsonTokenType.True:
                        return "true";
                    case JsonTokenType.False:
                        return "false";
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        reader.Skip();
                        return null;
                    default:
                        return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                if (value == null) writer.WriteNullValue();
                else writer.WriteStringValue(value);
            }
        }
    }
}
