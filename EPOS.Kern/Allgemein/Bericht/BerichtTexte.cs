using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Berichtssprache = UI-Sprache (Konzept Eckpunkt 10; <see cref="Sprache.Nummer"/>: 0=de, 1=en).
    ///
    /// Übersetzt bekannte Berichtstexte per Wörterbuch: T(text) liefert bei
    /// englischer UI die Übersetzung, sonst den Eingabetext unverändert — unbekannte
    /// (dynamische) Texte laufen unverändert durch. Der WordKontext wendet T() auf
    /// Überschriften, Beschriftungen und fette Tabellenkopf-Zellen an; Kennzahl-
    /// Beschriftungen sind über den KennzahlenKatalog (LabelDe/LabelEn) zweisprachig.
    /// Vollständige Übersetzung der Fließtexte bleibt Übersetzungsarbeit (LIESMICH).
    /// </summary>
    public static class BerichtTexte
    {
        public static bool Englisch
        {
            get { try { return Sprache.Nummer == 1; } catch { return false; } }
        }

        /// <summary>Kultur der Berichtssprache (Zahlen-/Datumsformate).</summary>
        public static CultureInfo Kultur
        {
            get { return KulturFuer(Englisch); }
        }

        /// <summary>Kultur einer ausdrücklich gewählten Berichtssprache: <c>en-US</c> oder <c>de-DE</c>
        /// (Berichtsvorlagen: der Wertesatz bekommt die Sprache übergeben).</summary>
        public static CultureInfo KulturFuer(bool englisch)
        {
            return CultureInfo.GetCultureInfo(englisch ? "en-US" : "de-DE");
        }

        public static string T(string de)
        {
            return T(de, Englisch);
        }

        /// <summary>Wie <see cref="T(string)"/>, aber für eine ausdrücklich gewählte Sprache statt der
        /// Oberflächensprache.</summary>
        public static string T(string de, bool englisch)
        {
            if (!englisch || de == null) return de;
            string en;
            return _en.TryGetValue(de, out en) ? en : de;
        }

        private static readonly Dictionary<string, string> _en = new Dictionary<string, string>
        {
            // Kapitel
            { "Inhalt", "Contents" },
            { "Projektbeschreibung", "Project description" },
            { "Gebäude", "Buildings" },
            // E30 / A12: Gebaeudekennzahlen aus Tab_ErgebnisGebaeude, Ausweis im Berichtskopf
            { "Gebäude (Simulationsergebnis Stamm)", "Buildings (base simulation result)" },
            { "Gebäudemodell", "Building model" },
            { "Rechenweg", "Calculation method" },
            { "Wärmebedarf Heizung", "Space heating demand" },
            { "Spitzenlast (Stundenwert)", "Peak load (hourly value)" },
            { "Spitzenlast (Tagesmittel)", "Peak load (daily mean)" },
            { "Spitzenlast (95-%-Quantil)", "Peak load (95th percentile)" },
            { "Kühlenergie", "Cooling energy" },
            { "Stunden mit Kühlbedarf", "Hours with cooling demand" },
            { "Mittlere Raumtemperatur (Nutzungszeit)", "Mean room temperature (occupancy period)" },
            { "Überhitzungsstunden", "Overheating hours" },
            // Anlagenkopplung AK1 (Konzept 9.4): der Heizkreis je gekoppeltem Gebaeude
            { "Heizkreis und Übergabe (Simulationsergebnis Stamm)", "Heating circuit and heat emission (base simulation result)" },
            { "Übergabe (Auslegung)", "Heat emitter (design)" },
            { "Heizkurve", "Heating curve" },
            { "Vorlauf Mittel [°C]", "Mean flow [°C]" },
            { "Rücklauf Mittel [°C]", "Mean return [°C]" },
            { "Übergabe begrenzt [h/a]", "Emission limited [h/a]" },
            { "fester Vorlauf", "fixed flow temperature" },
            { "Niveau", "Level" },
            { "Steilheit", "Slope" },
            { "Die Mittel gelten für die Stunden mit Heizbetrieb; begrenzt heißt, die Übergabe lieferte weniger, als der Sollwert verlangte.",
              "The means refer to the hours with heating; limited means the heat emitter delivered less than the set point required." },
            // E37: der Kaeltekreis je kuehlgekoppeltem Gebaeude
            { "Kältekreis und Kühlübergabe (Simulationsergebnis Stamm)", "Chilled water circuit and cooling emission (base simulation result)" },
            { "Kühlübergabe (Auslegung)", "Cooling emitter (design)" },
            { "Vorlaufgrenze [°C]", "Flow temperature limit [°C]" },
            { "Kühlvorlauf Mittel [°C]", "Mean chilled water flow [°C]" },
            { "Kühlrücklauf Mittel [°C]", "Mean chilled water return [°C]" },
            { "Kühlübergabe begrenzt [h/a]", "Cooling emission limited [h/a]" },
            { "davon an der Vorlaufgrenze", "of which at the flow temperature limit" },
            { "keine", "none" },
            { "Die Mittel gelten für die Stunden mit Kühlbetrieb; begrenzt heißt, die Kühlübergabe lieferte weniger, als der Kühlsollwert verlangte. Das sind keine Überhitzungsstunden — diese zählen die Stunden über der Maximalraumtemperatur.",
              "The means refer to the hours with cooling; limited means the cooling emitter delivered less than the cooling set point required. These are not overheating hours — those count the hours above the maximum room temperature." },
            { "Die Vorlaufgrenze ist eine Vorgabe, keine gerechnete Taupunktgrenze; die Kühlübergabe rechnet sensibel, ohne Entfeuchtung.",
              "The flow temperature limit is a default, not a calculated dew point limit; the cooling emission is calculated sensibly, without dehumidification." },
            { "Der Tagesbilanz-Weg (Bestandsweg) liefert weder Raumtemperatur noch Kühllast.",
              "The daily balance method (legacy method) provides neither room temperature nor cooling load." },
            { "Energiebedarf (Simulationsergebnis Stamm)", "Energy demand (base simulation result)" },
            { "Komponenten & Varianten", "Components & variants" },
            { "Komponentenübersicht", "Component overview" },
            { "Abweichungen der Varianten gegenüber dem Stamm", "Deviations of the variants from the base" },
            { "Berechnungsergebnisse je Variante", "Results per variant" },
            { "Variantenvergleich", "Variant comparison" },
            { "Abweichung zum Stamm (Schlüsselkennzahlen, in %)", "Deviation from base (key figures, %)" },
            { "Kennzahlen im Vergleich (Diagramme)", "Key figures compared (charts)" },
            { "Deckungsdiagramme", "Coverage charts" },
            { "Erzeuger — Einzelauflistung je Projekt", "Generators — itemised per project" },
            { "Brennstoffmengen", "Fuel quantities" },
            { "Anhang", "Appendix" },
            { "Simulationsstände", "Simulation timestamps" },
            { "Datengrundlage und Methodik", "Data basis and methodology" },
            { "Hinweise dieses Berichtslaufs", "Notes of this report run" },
            { "Variantenvergleich — Energie- und Wärmeversorgung", "Variant comparison — energy and heat supply" },

            // Deckblatt/Eigenschaften
            { "Projekt", "Project" },
            { "Kunde", "Customer" },
            { "Bearbeiter", "Editor" },
            { "Verglichene Varianten", "Compared variants" },
            { "Berichtsdatum", "Report date" },
            { "EPOS-Plan-Version", "EPOS-Plan version" },
            { "Projektname", "Project name" },
            { "Beschreibung", "Description" },
            { "Klimaregion", "Climate region" },
            { "Angelegt", "Created" },
            { "Zuletzt geändert", "Last modified" },
            { "Simulationsstand", "Simulation timestamp" },

            // Tabellenköpfe
            { "Kennzahl (Einheit)", "Key figure (unit)" },
            { "Kennzahl", "Key figure" },
            { "Stamm", "Base" },
            { "Variante", "Variant" },
            { "Merkmal", "Characteristic" },
            { "Gewerk", "Trade" },
            { "Rolle", "Role" },
            { "Simulation vom", "Simulated on" },
            { "Hinweis", "Note" },
            { "Erzeuger", "Generator" },
            { "Wärme [MWh/a]", "Heat [MWh/a]" },
            { "Strom [MWh/a]", "Electricity [MWh/a]" },
            { "Energieträger", "Energy carrier" },
            { "Verbrauch [MWh/a]", "Consumption [MWh/a]" },
            { "Bezeichner", "Identifier" },
            { "Menge", "Quantity" },
            { "Δ (Var. − Stamm)", "Δ (var. − base)" },
            { "(Stammprojekt)", "(base project)" },

            // Kennzahlgruppen
            { "Energiebilanz", "Energy balance" },
            { "Effizienz", "Efficiency" },
            { "Emissionen", "Emissions" },
            { "Kosten", "Costs" },

            // Wirtschaftlichkeit (Phase 6)
            { "Wirtschaftlichkeit", "Economic viability" },
            { "Kapitalwertmethode (DIN EN 17463)", "Net present value method (DIN EN 17463)" },
            { "Kennzahlen im Szenario „Erwartet“", "Key figures, scenario \"Expected\"" },
            { "Szenario", "Scenario" },
            { "Investition I₀ [€]", "Investment I₀ [€]" },
            { "Betriebskosten [€/a]", "Operating cost [€/a]" },
            { "Energiekosten [€/a]", "Energy cost [€/a]" },
            { "Einspeiseerlös [€/a]", "Feed-in revenue [€/a]" },
            { "Restwert (Barwert) [€]", "Residual value (present value) [€]" },
            { "Nettobarwert über T [€]", "Net present value over T [€]" },
            { "Kapitalwert vs. Stamm [€]", "NPV vs. base [€]" },
            { "Annuität des KW [€/a]", "Annuity of NPV [€/a]" },
            { "Amortisation [a]", "Payback [a]" },
            { "Wärmegestehungskosten [€/kWh]", "Levelised cost of heat [€/kWh]" },
            { "KW Worst [€]", "NPV worst [€]" },
            { "KW Erwartet [€]", "NPV expected [€]" },
            { "KW Best [€]", "NPV best [€]" },
            { "Referenz: Stammprojekt · Restwert linear", "Reference: base project · linear residual value" },
            { "Rechenstand", "Calculated" },
            // Kapitalwert-Verlauf (Phase 11)
            { "Kapitalwert-Verlauf (kumulierte Barwerte, ohne Restwert) [€]",
              "NPV progression (cumulative present values, excl. residual value) [€]" },
            { "Jahr", "Year" },
            { "Ohne Restwert — Nettobarwert = Endwert + Restwert-Barwert.",
              "Excl. residual value — net present value equals the final value plus the discounted residual value." },
            { "CO₂-Abgabe BEHG [€/a]", "CO₂ levy (BEHG) [€/a]" },
            { "KWKG-Erlös Jahr 1 [€/a]", "CHP subsidy year 1 [€/a]" },
            { "Interner Zinsfuß [%]", "Internal rate of return [%]" },
            { "Sensitivitätsanalyse (Szenario „Erwartet“)", "Sensitivity analysis (scenario \"Expected\")" },
            { "Parameter", "Parameter" },
            { "KW bei −Δ [€]", "NPV at −Δ [€]" },
            { "KW Basis [€]", "NPV base [€]" },
            { "KW bei +Δ [€]", "NPV at +Δ [€]" },
            { "Stromkosten Tarif [€/a]", "Grid cost (tariff) [€/a]" },
            { "Netzbezug [MWh]", "Grid import [MWh]" },
            { "PV-Einspeisung [MWh]", "PV export [MWh]" },
            { "KWK-Eigenstrom [MWh]", "CHP self-consumption [MWh]" },
            { "KWK-Einspeisung [MWh]", "CHP export [MWh]" },
            { "Emissionsbilanz — gekoppelte vs. getrennte Erzeugung", "Emission balance — combined vs. separate generation" },
            { "Schadstoff", "Pollutant" },
            { "Gekoppelt (System)", "Combined (system)" },
            { "Getrennt (Referenz)", "Separate (reference)" },
            { "Vermeidung", "Avoided" },
            { "CO₂-Vermeidung vs. getrennt [t/a]", "CO₂ avoided vs. separate [t/a]" },

            // Verbindliche Rechenkette je Berichtslauf (15.08.2026)
            { "Wirtschaftlichkeit konnte für diesen Bericht nicht berechnet werden — " +
              "Kostenpositionen und Parameter prüfen.",
              "Economic viability could not be calculated for this report — check cost items and parameters." },
            { "⚠ Die Wirtschaftlichkeitsrechnung dieses Berichtslaufs ist fehlgeschlagen — " +
              "gezeigt wird der zuletzt gespeicherte Stand.",
              "⚠ The economic calculation of this report run failed — the last stored result is shown." },
            { "Für diesen Bericht wurde jedes aufgeführte Projekt neu simuliert " +
              "(stündliche Jahresrechnung) und anschließend wirtschaftlich bewertet; " +
              "die Zahlen aller Kapitel stammen damit aus demselben Rechenlauf.",
              "Every project listed here was simulated anew for this report (hourly annual calculation) " +
              "and then evaluated economically; all chapters therefore share one calculation run." },

            // PAKET E1 (Konzept 4.4) — die drei Bedarfskanäle im Bericht.
            // Die Kanalnamen tragen bewusst den Zusatz „davon" bzw. stehen als
            // Deckungsgrad-Zeilen: „Heizung" allein wäre als Wörterbuchschlüssel zu
            // grob — der Bericht verwendet das Wort auch in Gewerks- und
            // Komponentennamen, und T() ersetzt Text global.
            { "davon Heizung", "of which space heating" },
            { "davon Brauchwasser", "of which domestic hot water" },
            { "davon Prozesswärme", "of which process heat" },
            { "Deckungsgrade je Bedarfsart", "Coverage by demand type" },
            { "Deckungsgrad Heizung", "Coverage space heating" },
            { "Deckungsgrad Brauchwasser", "Coverage domestic hot water" },
            { "Deckungsgrad Prozesswärme", "Coverage process heat" },
            // Die übrigen Zeilen der Eigenschaftstafel „Energiebedarf (Simulationsergebnis Stamm)“ — englisch wie die
            // Beschriftungen ihrer Kennzahlen (KennzahlenKatalog: energie.waermebedarf, .waermelast, .strombedarf, .strommax).
            { "Wärmebedarf gesamt", "Total heat demand" },
            { "Wärmelast max.", "Peak heat load" },
            { "Strombedarf gesamt", "Total electricity demand" },
            { "Strombedarf max.", "Peak electric load" },
            // Die Eigenschaftstafel je Gebäude der Projektbeschreibung — englisch wie die ausführliche Vorlage.
            { "Gebäudeart", "Building type" },
            { "Baualtersklasse", "Construction period" },
            { "Wohn-/Nutzfläche", "Living/usable area" },
            { "Bewohner/Nutzer", "Occupants/users" },
            { "Wärmebedarf", "Heat demand" },
            { "spez. Wärmeverbrauch", "Specific heat consumption" },
            { "Warmwasserbedarf", "Hot water demand" },
            { "Raumhöhe", "Room height" },

            // STUFE KU1 (Kühlkonzept 8.4) — der Kälteabschnitt der Projektbeschreibung. Die
            // Kältezahlen tragen ihre Grenze (K5) als Satz aus MyResource, schon übersetzt.
            { "Kältebedarf und -deckung (Simulationsergebnis Stamm)", "Cooling demand and coverage (base simulation result)" },
            { "Kältebedarf gesamt", "Total cooling demand" },
            { "davon Kühlung", "of which cooling" },
            { "Kältelast max.", "Peak cooling load" },
            { "Vollbenutzungsstunden Kälte", "Full-load hours cooling" },
            { "Kältebedarf ungedeckt", "Uncovered cooling demand" },

            // STUFE KU2 WELLE 3 (Kühlkonzept 6, 8.4; E21, E34) — die Deckung: Kennzahlzeilen,
            // die Kälteerzeugertabelle und die Grenze der Emissionen des Kältestroms.
            { "Deckungsgrad Kühlung", "Coverage cooling" },
            { "Kälteerzeugung Wärmepumpe", "Cooling generation heat pump" },
            { "Kältestrom", "Cooling electricity" },
            { "Jahresarbeitszahl Kälte", "Seasonal EER (cooling)" },
            { "Netzbezug Kältestrom", "Grid import cooling electricity" },
            { "Kosten Kältestrom", "Cost of cooling electricity" },
            { "CO₂ Kältestrom", "CO₂ cooling electricity" },
            { "Kälteerzeuger", "Cooling generators" },
            { "Anlage", "Plant" },
            { "Kälte [MWh/a]", "Cooling [MWh/a]" },
            { "Kältestrom [MWh/a]", "Cooling electricity [MWh/a]" },
            { "aus dem Netz [MWh/a]", "from the grid [MWh/a]" },
            { "Stromträger", "Electricity carrier" },
            { "Die Emissionen des Kältestroms sind betriebsbedingt über den Strom gerechnet; Kältemittelverluste sind nicht enthalten.",
              "Emissions of the cooling electricity are operational, calculated via the electricity; refrigerant losses are not included." },

            // PAKET P2 (Konzept 7.4) — die Speichertemperaturen des Schichtmodells.
            // „Speicher" allein ist als Wörterbuchschlüssel grob genug, dass es nur als
            // Tabellenkopf auftritt; die beiden Temperaturzeilen tragen ihre Einheit mit,
            // damit sie sich nicht mit Zahlenwerten anderer Tabellen kreuzen.
            { "Speichertemperaturen (Schichtmodell)", "Storage temperatures (stratified model)" },
            { "Speicher", "Storage" },
            // Stufe G6a - die Zonen eines Gebaeudes im Gebaeudeblock der Projektbeschreibung.
            { "Zonen", "Zones" },
            { "Zone", "Zone" },
            { "Nutzfläche [m²]", "Usable area [m²]" },
            { "Volumen [m³]", "Volume [m³]" },
            { "H_T [W/K]", "H_T [W/K]" },
            { "H_ve [W/K]", "H_ve [W/K]" },
            { "Bauteile", "Building components" },
            { "Summe", "Total" },
            { "* Volumen aus Nutzfläche × Raumhöhe abgeleitet.", "* Volume derived from usable area × room height." },
            // Stufe G6b - die Zonenzeilen des Laufs (Tab_ErgebnisZone) in derselben Tabelle.
            { "beheizt", "heated" },
            { "ja", "yes" },
            { "nein", "no" },
            { "Heizwärme [MWh/a]", "Heating energy [MWh/a]" },
            { "Spitze [kW]", "Peak [kW]" },
            { "T oben Mittel [°C]", "T top mean [°C]" },
            { "T oben Minimum [°C]", "T top minimum [°C]" },
            { "Speichertemperaturen in charakteristischen Wochen (Winter/Übergang/Sommer)",
              "Storage temperatures in characteristic weeks (winter/transition/summer)" },

            // BERICHTSVORLAGEN BV-E1 (Konzept 4.10) — die Texte der Word-Engine
            // (WordVorlagentexte): Fundorte, Gründe und Meldungen des Füllergebnisses. Die Muster
            // tragen {0}, {1} …; zitierte Platzhalter bleiben in doppelten Klammern stehen.
            { WordVorlagentexte.FUNDORT_RUMPF, "Body, paragraph {0}" },
            { WordVorlagentexte.FUNDORT_KOPF, "Header (section {0}), paragraph {1}" },
            { WordVorlagentexte.FUNDORT_KOPF_ERSTE, "Header (section {0}, first page), paragraph {1}" },
            { WordVorlagentexte.FUNDORT_KOPF_GERADE, "Header (section {0}, even pages), paragraph {1}" },
            { WordVorlagentexte.FUNDORT_KOPF_OHNE, "Header, paragraph {0}" },
            { WordVorlagentexte.FUNDORT_FUSS, "Footer (section {0}), paragraph {1}" },
            { WordVorlagentexte.FUNDORT_FUSS_ERSTE, "Footer (section {0}, first page), paragraph {1}" },
            { WordVorlagentexte.FUNDORT_FUSS_GERADE, "Footer (section {0}, even pages), paragraph {1}" },
            { WordVorlagentexte.FUNDORT_FUSS_OHNE, "Footer, paragraph {0}" },
            { WordVorlagentexte.FUNDORT_FUSSNOTEN, "Footnotes, paragraph {0}" },
            { WordVorlagentexte.FUNDORT_ENDNOTEN, "Endnotes, paragraph {0}" },
            { WordVorlagentexte.FUNDORT_TABELLE, " · table {0}, row {1}, column {2}" },
            { WordVorlagentexte.FUNDORT_TEXTFELD, " · text box" },
            { WordVorlagentexte.FUNDORT_SDT, " · content control" },
            { WordVorlagentexte.FUNDORT_AUSZUG, ": “{0}”" },
            { WordVorlagentexte.GRUND_UNBEKANNT, "unknown key" },
            { WordVorlagentexte.GRUND_NICHT_UNTERSTUETZT, "not yet supported in this version" },
            { WordVorlagentexte.GRUND_FALSCHE_STELLE, "does not fit at this position" },
            { WordVorlagentexte.GRUND_DOPPELT, "chapter already stands at an earlier position" },
            { WordVorlagentexte.GRUND_BLOCK, "block mark without counterpart or at a position not allowed" },
            { WordVorlagentexte.GRUND_KONTEXT, "value per state or building outside its block" },
            { WordVorlagentexte.OHNE_PLATZHALTER,
              "The template contains no placeholder — the chapters are placed at the end of the document ({{bericht.inhalt}})." },
            { WordVorlagentexte.AUSNAHME, "{0}: exception while resolving, “—” inserted ({1})." },
            { WordVorlagentexte.VERKNUEPFTES_BILD, "Linked picture removed: {0}" },
            { WordVorlagentexte.VERKNUEPFUNG, "Link removed ({0}): {1}" },
            { WordVorlagentexte.DOKUMENTVORLAGE, "Reference to the attached template removed: {0}" },
            { WordVorlagentexte.STIL_ANGELEGT, "Style “{0}” created — it was missing in the template." },
            { WordVorlagentexte.KOMMENTARE, "Comments removed from the template: {0}" },
            { WordVorlagentexte.DATUMSFELD, "The field {0} shows the date of opening — use {{bericht.datum}}?" },
            { WordVorlagentexte.DOTX, "The template is a Word template (.dotx); the report is a document (.docx)." },
            { WordVorlagentexte.LEER, "{0}: empty ({1}×)" },
            { WordVorlagentexte.LEER_STAENDE, "{0}: empty for {1} of {2} states" },
            { WordVorlagentexte.LEER_GEBAEUDE, "{0}: empty for {1} of {2} buildings" },
            { WordVorlagentexte.BLOCK_OFFEN,
              "{0}: the block has no matching end on the same level — the mark remains ({1})." },
            { WordVorlagentexte.BLOCK_TIEFE,
              "{0}: blocks are at most two levels deep — this block remains ({1})." },
            { WordVorlagentexte.BLOCK_BEREICH,
              "{0}: unknown repetition range — the block remains ({1})." },
            { WordVorlagentexte.WENN_KEIN_SCHALTER,
              "{0}: the condition names no switch of the catalogue — the range remains with its marks ({1})." },
            { WordVorlagentexte.WENN_KONTEXT,
              "{0}: the switch only applies within its block ({{#je stand}} or {{#je gebaeude}}) — the range remains with its marks ({1})." },
            { WordVorlagentexte.SCHALTER_LEER,
              "{0}: the switch has no value — the condition counts as not met ({1})." },
            { WordVorlagentexte.KAPITEL_ORT,
              "{0}: a chapter must stand alone in a paragraph of the body or of a content control — the placeholder remains ({1})." },
            { WordVorlagentexte.LISTE_ORT,
              "{0}: a list belongs in the body, a table cell or a content control — the placeholder remains ({1})." },
            { WordVorlagentexte.SDT_IM_SATZ,
              "{0}: an inline content control only takes text, number or date — it remains ({1})." },
            { WordVorlagentexte.SDT_ZEILE,
              "{0}: content controls around table rows or cells are not filled — it remains ({1})." },
            { WordVorlagentexte.KAPITEL_DOPPELT,
              "{0}: the chapter already stands at an earlier position of the template — this one remains ({1})." },
            { WordVorlagentexte.BILD_ORT,
              "{0}: a picture belongs in the body, a table cell, a content control, a header or footer or a text box — the picture remains ({1})." },
            { WordVorlagentexte.BILD_OHNE_BILD,
              "{0}: the alternative text belongs to a shape without a picture — it remains ({1})." },
            { WordVorlagentexte.TABELLE_ORT,
              "{0}: a table must stand alone in a paragraph of the body, a table cell or a content control — the placeholder remains ({1})." },
            { WordVorlagentexte.MUSTER_ORT,
              "{0}: the sample table carries the key as alternative text or title of a table, not in the text — the placeholder remains ({1})." },
            { WordVorlagentexte.MUSTER_OHNE_ROLLEN,
              "The sample table names no role (base, group, total, warning) — it was removed, direct formatting applies." },
            { WordVorlagentexte.BILD_ENTFAELLT, "Chart omitted: {0}" },
            { WordVorlagentexte.BILD_OHNE_MODELL, "{0}: no picture — {1}" },
            { WordVorlagentexte.BILD_IM_SATZ,
              "{0}: a picture stands alone in a paragraph or in the alternative text of a picture — the placeholder remains ({1})." },
            { WordVorlagentexte.BILD_VERKLEINERT,
              "{0}: the frame is narrower than the picture is drawn — it is reduced to {1} % ({2})." },
            { WordVorlagentexte.VORLAGE_LEER, "The report template is empty." },
            { WordVorlagentexte.VORLAGE_UNLESBAR, "The report template cannot be opened as a Word document (.docx)." },
            { WordVorlagentexte.VORLAGE_MAKROS, "Templates with macros (.docm, .dotm) are not used — please save as .docx." },
        };
    }
}
