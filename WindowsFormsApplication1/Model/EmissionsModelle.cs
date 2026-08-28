using System;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Datenhaltung des Emissionsarten-Katalogs (Konzept_Emissionsarten_CO2-
    // Aequivalent_EPOS-Plan.md, § 3; Tabellen aus Migrationsschritt 57).
    //
    // Drei Modelle in EINER Datei, weil sie nur miteinander Sinn ergeben: eine ART
    // (emissionsart), ein WERT dazu (emissionswert — Katalogvorlage ODER Trägerwert)
    // und die ZEILE, die der Emissions-Tab daraus zeigt. Gelesen und geschrieben
    // werden sie ausschließlich über EmissionskatalogCtrl und EmissionenCtrl.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Eine Emissionsart des Katalogs (Konzept F1): CO₂, SO₂, NOx sind keine
    /// Spaltennamen mehr, sondern Zeilen der Tabelle <c>emissionsart</c>.
    /// </summary>
    public class EmissionsartModel
    {
        /// <summary>Primärschlüssel (AUTOINCREMENT der Tabelle).</summary>
        public int ID;

        /// <summary>Fachlicher Schlüssel, eindeutig — <c>CO2</c>, <c>SO2</c>,
        /// <c>NOX</c>, <c>CH4_FOSSIL</c> … (Werte aus <see cref="DbWerte"/>).</summary>
        public string Kuerzel = "";

        /// <summary>Anzeigename („Methan (fossil)").</summary>
        public string Name = "";

        /// <summary>Anzeigeeinheit (Konzept F4): <see cref="DbWerte.EMISSION_EINHEIT_G_KWH"/>
        /// bei CO₂, sonst <see cref="DbWerte.EMISSION_EINHEIT_MG_KWH"/>.</summary>
        public string Einheit = DbWerte.EMISSION_EINHEIT_MG_KWH;

        /// <summary>Treibhauspotenzial GWP₁₀₀ (Konzept F2). Bei CO₂ fest 1; SO₂,
        /// NOx, Staub und CO tragen 0 — sie sind keine Treibhausgase.</summary>
        public double Co2Aequivalent;

        /// <summary>Fundstelle des Äquivalenzfaktors („IPCC AR6, GWP100").</summary>
        public string AequivalentQuelle = "";

        /// <summary>Pflichtart — nur CO₂ (Konzept F1): nicht abwählbar, nicht löschbar.</summary>
        public bool IstPflicht;

        /// <summary>Steuert Feldliste UND Summe, global je Art (Konzept F5).</summary>
        public bool Ausgewaehlt;

        /// <summary>Mitgelieferte Art — abwählbar, aber nicht löschbar.</summary>
        public bool IstAuslieferung;

        /// <summary>Reihenfolge im Emissions-Tab und im Katalog-Dialog.</summary>
        public int Sortierung;

        /// <summary>true, wenn die Art in mg/kWh geführt wird — dann geht ihr Wert
        /// für die CO₂e-Summe durch 1000 (Konzept F4/F6).</summary>
        public bool IstMilligramm
        {
            get
            {
                return string.Equals(Einheit, DbWerte.EMISSION_EINHEIT_MG_KWH,
                                     StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Der Wert dieser Art, normiert auf g/kWh (Konzept F6).</summary>
        public double NormiertGKwh(double wert)
        {
            return IstMilligramm ? wert / 1000.0 : wert;
        }
    }

    /// <summary>
    /// Eine Zeile aus <c>emissionswert</c> — Katalogvorlage ODER Trägerwert. Der
    /// Unterschied ist allein <see cref="CarrierId"/>: NULL heißt
    /// „trägerunabhängige Vorlage" (Konzept § 3).
    /// </summary>
    public class EmissionswertModel
    {
        /// <summary>Primärschlüssel (AUTOINCREMENT der Tabelle).</summary>
        public int ID;

        /// <summary>Verweis auf <see cref="EmissionsartModel.ID"/>.</summary>
        public int EmissionsartId;

        /// <summary>Träger oder <c>null</c> = Vorlage für alle Träger.</summary>
        public int? CarrierId;

        /// <summary>Quellkennung, Werte aus <see cref="DbWerte"/>
        /// (<c>BAFA_EEW</c>, <c>EBEV_2030</c>, <c>EIGENER_WERT</c> …).</summary>
        public string Quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT;

        /// <summary>Anzeigetext mit Stand („BAFA EEW 3.4, 2026").</summary>
        public string QuelleText = "";

        /// <summary>Zahlenwert in der Einheit der Art; <c>null</c> = nicht gepflegt.</summary>
        public double? Wert;

        /// <summary>Der Wert IST bereits ein CO₂-Äquivalent (Konzept F3) — dann wird
        /// für die Summe nichts mehr aufaddiert.</summary>
        public bool IstCo2e;

        /// <summary>Der für den Träger geltende Wert; je Träger und Art höchstens einer.</summary>
        public bool IstAktiv;

        /// <summary>Bei kopierten Werten der Katalogeintrag, aus dem kopiert wurde
        /// (Konzept F8) — die Katalogänderung wirkt NICHT rückwirkend.</summary>
        public int? HerkunftId;

        /// <summary>Ausgelieferte Katalogzeile — unveränderlich und nicht löschbar.</summary>
        public bool IstAuslieferung;

        /// <summary>Fortschreibungsdatum (Muster Jahreszeilen).</summary>
        public DateTime? GueltigAb;

        /// <summary>Anzeigetext der Herkunft; leer bleibt nie stehen.</summary>
        public string Herkunftstext
        {
            get
            {
                if (!string.IsNullOrEmpty(QuelleText)) return QuelleText;
                return string.IsNullOrEmpty(Quelle) ? "—" : Quelle;
            }
        }
    }

    /// <summary>
    /// Eine Zeile des Emissions-Tabs (Konzept § 4.1): die Art, ihr geltender Wert
    /// und dessen Herkunft — samt Bearbeitungsstand.
    ///
    /// <para><b>Deferred-Semantik (Ä12/Ä14):</b> <see cref="Geaendert"/> markiert
    /// eine Änderung, die NUR im Objekt lebt. Erst „Speichern" schreibt sie;
    /// Abbrechen und Trägerwechsel übernehmen nichts.</para>
    /// </summary>
    public class EmissionsZeile
    {
        /// <summary>Die Art, zu der die Zeile gehört.</summary>
        public EmissionsartModel Art;

        /// <summary><c>emissionswert.id</c> der aktiven Trägerzeile; 0 = es gibt
        /// noch keine — dann legt das Speichern eine an.</summary>
        public int WertId;

        /// <summary>Angezeigter/bearbeiteter Wert; <c>null</c> = nicht gepflegt
        /// (leeres Feld) — er trägt dann nichts zur Summe bei (Konzept F5).</summary>
        public double? Wert;

        /// <summary>Quellkennung des geltenden Wertes.</summary>
        public string Quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT;

        /// <summary>Herkunftstext, wie er am Feld steht (Konzept F8).</summary>
        public string QuelleText = "";

        /// <summary>Der Wert ist bereits ein Äquivalent (Konzept F3).</summary>
        public bool IstCo2e;

        /// <summary>Verweis auf die Katalogvorlage, aus der übernommen wurde.</summary>
        public int? HerkunftId;

        /// <summary>Die Zeile wurde in dieser Sitzung geändert und ist zu speichern.</summary>
        public bool Geaendert;

        /// <summary>Im Projektkontext sind nur die drei Kernarten editierbar; die
        /// übrigen erscheinen lesend mit ihrem Katalogwert.</summary>
        public bool NurLesend;

        /// <summary>Kürzel der Art — Kurzform für die Suche in Listen.</summary>
        public string Kuerzel
        {
            get { return Art != null ? Art.Kuerzel : ""; }
        }

        /// <summary>Der Beitrag dieser Zeile zur CO₂e-Summe in g/kWh (Konzept F6).</summary>
        public double BeitragGKwh
        {
            get
            {
                if (Art == null || !Wert.HasValue) return 0.0;
                return Art.NormiertGKwh(Wert.Value) * Art.Co2Aequivalent;
            }
        }
    }

    /// <summary>
    /// Ein Schritt des Speicherplans (Konzept § 4.1, Kontext-Regel). Der Plan wird
    /// GEBILDET, bevor er ausgeführt wird — damit ist im Prüfstand nachlesbar, was
    /// das Speichern täte, ohne dass etwas geschrieben wird.
    /// </summary>
    public class EmissionsSpeicherschritt
    {
        /// <summary>Kennung eines <see cref="EmissionenCtrl"/>-Schritts:
        /// <c>INSERT</c>/<c>UPDATE</c> (Tabelle <c>emissionswert</c>),
        /// <c>SPIEGEL</c> (Altspalten <c>energy_carrier</c>, Konzept F9),
        /// <c>PROJEKT</c> (Bestandsweg <c>energy_project_settings</c>) oder
        /// <c>MODUS</c> (Berechnungsmodus, Konzept F7).</summary>
        public string Art = "";

        /// <summary>Kürzel der betroffenen Emissionsart; leer beim Modusschritt.</summary>
        public string Kuerzel = "";

        /// <summary>Zu schreibender Wert; <c>null</c> = nicht gepflegt.</summary>
        public double? Wert;

        /// <summary>Quellkennung, die der Schritt setzt (Konzept F8).</summary>
        public string Quelle = "";

        /// <summary>Herkunftstext, den der Schritt setzt.</summary>
        public string QuelleText = "";

        /// <summary>Kennzeichen „Wert ist bereits Äquivalent" (Konzept F3).</summary>
        public bool IstCo2e;

        /// <summary>Verweis auf die Katalogvorlage bei einer Übernahme.</summary>
        public int? HerkunftId;

        /// <summary>Betroffene <c>emissionswert.id</c>; 0 bei INSERT.</summary>
        public int WertId;

        /// <summary>Klartextzeile für Protokoll und Prüfstand.</summary>
        public string Klartext = "";

        public override string ToString()
        {
            return Klartext;
        }
    }
}
