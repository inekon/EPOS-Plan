using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Verrechnung der Kennzahlgruppen „Emissionen" und „Kosten (einfach)"
    /// (Konzept Kap. 5; Phase 5) — Vorstufe zur Wirtschaftlichkeit.
    ///
    /// Grundlage: die mit Befund B1 eingeführte carrier_id an den Ergebnis-Modulen
    /// (Verbrauch je Erzeuger-Modul in MWh/a) plus das Preis-/Faktorgerüst
    /// energy_project_settings / energy_carrier / Abfrage_Energietraeger_Effektiv.
    ///
    /// Regeln:
    ///  - Fehlt für einen Träger MIT Verbrauch der Preis bzw. der CO₂-Faktor,
    ///    bleibt die betroffene Kennzahl null („—") — keine stillen Teilsummen.
    ///  - Emissionsfaktoren-Quelle: seit Etappe E5 EINE Kette für beide Rechner in
    ///    <see cref="EmissionsFaktorLader"/> — PROJEKTWERT (energy_project_settings.co2)
    ///    → aktive emissionswert-Zeile des Trägers → Tab_Brennstoff_Stamm.CO2 (über
    ///    energy_carrier.id_brennstoff) → energy_carrier.co2. Bis E4 fehlte die zweite
    ///    Stufe; die Reihenfolge der übrigen ist die Vorgabe vom 11.08.2026.
    ///  - BERECHNUNGSMODUS (Konzept F7): CO2Gesamt und CO2Spezifisch führen im Modus
    ///    CO2E das CO₂-Äquivalent der ausgewählten Arten statt des reinen CO₂ — der
    ///    Netzstrom-Anteil eingeschlossen. CO2Brennstoff (BEHG) bleibt in BEIDEN Modi
    ///    reines CO₂: Abgabepflichtig ist nach EBeV 2030 das Kohlendioxid, nicht sein
    ///    Äquivalent.
    ///  - Einheit VERIFIZIERT (Kenndaten.accdb, 11.08.2026): die Faktoren stehen in
    ///    g/kWh (= kg/MWh) — Tab_Brennstoff_Stamm z. B. Erdgas 240, Heizöl 310,
    ///    Strom 560. t/a = MWh/a × Faktor / 1000.
    ///  - Netzbezug: Faktor des projektzugeordneten Strom-Trägers über dieselbe
    ///    Kette (Projektwert → Tab_Brennstoff_Stamm → energy_carrier); erst wenn
    ///    dort nichts gepflegt ist, greift STROMMIX_CO2_G_JE_KWH als Vorgabewert.
    ///  - STROMBEDARF OHNE VERWENDUNG (Anwenderentscheide 22.09.2026): Führt das Projekt
    ///    keinen Erzeuger, der Strom verwendet
    ///    (<see cref="ProjektEnergietraegerCtrl.StromOhneVerwendung"/>), gehen
    ///    Stromkosten und Emissionen des Netzbezugs mit 0 ein — unabhängig von
    ///    Trägerzuordnung und Preis; ein Hinweis nennt die ausgelassene Menge.
    ///    GRUPPENREGEL: Im Vergleich einer Gruppe, in der ein anderer Stand Strom
    ///    verwendet, setzt die Wirtschaftlichkeit auf ihrer Kopie
    ///    <see cref="VariantenDaten.StromImVergleichBepreisen"/> — dann wird der
    ///    Netzbezug bepreist und bewertet, ohne Zuordnung mit dem Auslieferungsträger.
    ///  - CO2Brennstoff (BEHG-Basis, Phase 7/W2): nur ABGABEPFLICHTIGE Träger —
    ///    Brennstoff-Kategorien Gas/Öl/Koks/Kohle/Sonstige (Tab_BrennstoffKategorien),
    ///    ausgenommen „Biogas“. Näherung: Bio-Heizöl-Blends zählen voll als fossil,
    ///    unbekannte Träger gelten als pflichtig (konservativ); Quoten erst mit W3.
    ///  - LEITENTSCHEIDUNG L13: Zusätzlich werden die MENGEN biogener Träger geführt
    ///    (BiogenMengeMWh, BiogenBehgMengeMWh). Bewusst Mengen und keine Emissionen —
    ///    ob biogenes Verbrennungs-CO₂ angesetzt wird und ob der Nullansatz des § 8
    ///    EBeV 2030 zulässig ist, entscheidet die gewählte Konvention, nicht dieser
    ///    Rechner. Er bleibt dadurch unverändert in dem, was er bisher lieferte.
    /// </summary>
    public static class KostenEmissionRechner
    {
        /// <summary>
        /// CO₂-Faktor des Netzstroms [g/kWh] (deutscher Strommix, Vorgabewert). Er
        /// greift NUR, wenn dem Projekt kein Stromträger zugeordnet ist — sonst gilt
        /// der Faktor dieses Trägers.
        ///
        /// <para><b>435 statt bisher 380</b> (Nutzerentscheid 29.08.2026, Etappe E5):
        /// BAFA, Informationsblatt CO₂-Faktoren EEW, Zeile „El. Strom
        /// (Effizienzmaßnahme)" — 0,435 tCO₂/MWh. Der Wert ersetzt den alten
        /// Strommix-Vorgabewert und folgt damit demselben Beschluss wie die Saat der
        /// Stromträger aus Etappe E1 (<c>Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md</c>
        /// § 2.2/§ 3): Sonst rechnete dieselbe Anwendung je nach Datenlage mit 380
        /// oder 435.</para>
        /// </summary>
        /// <remarks>
        /// <b>W14a-E-8-B1 / W11a-O-2 (07.09.2026):</b> Die Zahl steht seither in
        /// <see cref="Emissionsquelle.NETZSTROM_RUECKFALL_G_JE_KWH"/> und wird hier nur
        /// noch unter ihrem gewachsenen Namen weitergereicht. Grund: Dieselbe Größe
        /// braucht seit W11a-O-2 auch die Autarkie-Kachel, und die hatte bis dahin ein
        /// EIGENES Literal (0,42 kg/kWh) — zwei Vorgabewerte für denselben Netzstrom.
        /// Alle bisherigen Leser (<c>BerichtsDatenSammler</c>, <c>SchemaMigration</c>,
        /// <c>WizardCtrl</c>) bleiben unberührt.
        /// </remarks>
        public const double STROMMIX_CO2_G_JE_KWH = Emissionsquelle.NETZSTROM_RUECKFALL_G_JE_KWH;

        // =====================================================================
        // DIE GRÜNDE, AUS DENEN DIE ENERGIEKOSTEN UNBESTIMMBAR BLEIBEN
        // (Anwenderbefund 14.09.2026, Auftrag #267)
        // =====================================================================
        //
        // Jeder Ausgang, der null liefert, benennt sich hier selbst — mit dem
        // AUSWEG in derselben Zeile. Bis dahin lieferten alle Ausgänge dasselbe
        // stumme null, und die Seite machte daraus „—" plus den pauschalen Satz
        // „Arbeitspreise/Träger prüfen": Ein Anwender mit gepflegten Preisen und
        // einer Wärmepumpe ohne Trägerzuordnung las damit genau das Gegenteil
        // dessen, was zu tun war.
        //
        // DIE TEXTE STEHEN HIER, NICHT IN DER OBERFLÄCHE: Wer den Grund kennt,
        // ist dieser Rechner. Die Oberfläche zeigt ihn nur an (Razor-Warnbanner),
        // die Wirtschaftlichkeit reicht ihn als Fehlgrund durch.
        //
        // JEDER TEXT KOMMT AUS MyResource (beide Sprachen), der deutsche Wortlaut
        // daneben ist der RÜCKFALL für eine Ressourcendatei ohne den Schlüssel —
        // dasselbe Muster wie WirtschaftlichkeitCtrl.T und KohaerenzPruefung.T.
        // Eine Eigenschaft statt einer Konstanten, weil die Sprache erst zur
        // LAUFZEIT feststeht: Eine Konstante wäre in der Sprache eingefroren, die
        // beim Übersetzen galt.

        /// <summary>Der Netzbezug ist nicht bepreisbar, weil dem Projekt überhaupt
        /// kein Stromträger zuzuordnen ist (auch der Katalog führt keinen).</summary>
        internal static string GRUND_KEIN_STROMTRAEGER
        {
            get
            {
                return T("WIRT_GRUND_KEIN_STROMTRAEGER",
                    "Energiekosten nicht bestimmbar: Der elektrischen Erzeugung (Wärmepumpe, " +
                    "Photovoltaik, Stromspeicher, Heizstab, Elektrokessel, BHKW, Hilfsenergie) " +
                    "ist kein Energieträger zugeordnet. " +
                    "Ausweg: unter „Berichte & Kosten › Energieträger“ einen Stromträger zuordnen.");
            }
        }

        /// <summary>Der Stromträger steht, aber sein Arbeitspreis ist nirgends gepflegt.</summary>
        internal static string GRUND_STROMPREIS_FEHLT
        {
            get
            {
                return T("WIRT_GRUND_STROMPREIS_FEHLT",
                    "Energiekosten nicht bestimmbar: Für den Stromträger „{0}“ ist kein " +
                    "Arbeitspreis gepflegt. Ausweg: den Arbeitspreis unter „Berichte & Kosten › " +
                    "Energieträger“ eintragen.");
            }
        }

        /// <summary>Ein verbrauchender Träger ohne gepflegten Arbeitspreis.</summary>
        internal static string GRUND_BRENNSTOFFPREIS_FEHLT
        {
            get
            {
                return T("WIRT_GRUND_BRENNSTOFFPREIS_FEHLT",
                    "Energiekosten nicht bestimmbar: Für {0} ist kein Arbeitspreis gepflegt. " +
                    "Ausweg: den Arbeitspreis unter „Berichte & Kosten › Energieträger“ eintragen.");
            }
        }

        /// <summary>Verbrauch, der keinem Energieträger zugeordnet ist.</summary>
        internal static string GRUND_VERBRAUCH_OHNE_TRAEGER
        {
            get
            {
                return T("WIRT_GRUND_VERBRAUCH_OHNE_TRAEGER",
                    "Energiekosten nicht bestimmbar: Ein Teil des Brennstoffverbrauchs " +
                    "({0} MWh/a) gehört zu keinem Energieträger. Ausweg: den betroffenen " +
                    "Erzeugern unter „Anlagen“ einen Energieträger zuordnen.");
            }
        }

        /// <summary>Weder Brennstoffverbrauch noch Netzbezug im Simulationsergebnis —
        /// es gibt nichts zu bepreisen.</summary>
        internal static string GRUND_KEIN_VERBRAUCH
        {
            get
            {
                return T("WIRT_GRUND_KEIN_VERBRAUCH",
                    "Energiekosten nicht bestimmbar: Das Simulationsergebnis weist weder " +
                    "Brennstoffverbrauch noch Netzbezug aus. Ausweg: Simulation prüfen und " +
                    "erneut rechnen.");
            }
        }

        /// <summary>
        /// Der Kältestrom einer Wärmepumpe trägt einen abweichenden Kühlträger (E34), und diesem
        /// fehlt der Arbeitspreis — die Energiekosten bleiben aus, statt den Kältestrom still mit
        /// dem Stromträger des Projekts zu bepreisen.
        /// </summary>
        internal static string GRUND_KUEHLTRAEGER_PREIS_FEHLT
        {
            get
            {
                return T("WIRT_GRUND_KUEHLTRAEGER_PREIS_FEHLT",
                    "Energiekosten nicht bestimmbar: Der Stromträger der Kühlung „{0}“ trägt keinen " +
                    "Arbeitspreis. Ausweg: unter „Berichte & Kosten › Energieträger“ den Arbeitspreis " +
                    "pflegen oder im Wärmepumpendialog den Stromträger der Kühlung auf „wie " +
                    "Heizbetrieb“ stellen.");
            }
        }

        /// <summary>Setzt die Ausweisfelder des Kältestroms auf „nicht gerechnet" (KU2 Welle 3, E34).</summary>
        private static void KaeltestromZuruecksetzen(VariantenDaten v)
        {
            v.KaeltestromNetzbezugMWh = null;
            v.KaeltestromKosten = null;
            v.KaeltestromCO2t = null;
            v.StromkostenKuehltraeger = 0.0;
            v.NetzbezugKuehltraegerMWh = 0.0;
            v.KuehlzaehlerMWh = 0.0;
        }

        /// <summary>Die Rechnung selbst ist gescheitert (Fangzaun in <see cref="Berechne"/>).</summary>
        internal static string GRUND_RECHENFEHLER
        {
            get
            {
                return T("WIRT_GRUND_RECHENFEHLER",
                    "Energiekosten nicht bestimmbar: Die Kostenrechnung ist abgebrochen. " +
                    "Ausweg: Preise und Heizwerte der Energieträger prüfen.");
            }
        }

        /// <summary>Der Netzbezug wurde mit dem AUSLIEFERUNGSträger bepreist, weil das
        /// Projekt selbst keinen zugeordnet hat — dieselbe Wahl, die die Kostenseite
        /// anzeigt und der Assistent zuordnen würde.</summary>
        internal static string HINWEIS_STROMTRAEGER_RUECKFALL
        {
            get
            {
                return T("WIRT_STROMTRAEGER_RUECKFALL",
                    "Netzbezug mit dem Energieträger „{0}“ bepreist — dem Projekt ist kein " +
                    "Stromträger zugeordnet. Ausweg: unter „Berichte & Kosten › Energieträger“ " +
                    "zuordnen.");
            }
        }

        /// <summary>Der Netzbezug wurde mit dem CO₂-Faktor des AUSLIEFERUNGSträgers
        /// gerechnet, weil das Projekt keinen zugeordnet hat — die Herleitungszeile zum
        /// GELIEHENEN Emissionsfaktor (Anwenderentscheid 15.09.2026). Sie steht neben,
        /// nicht anstelle der Kostenzeile: Beide Seiten können denselben Träger geliehen
        /// haben, und der Anwender behebt beides mit demselben Griff.</summary>
        internal static string HINWEIS_CO2_TRAEGER_RUECKFALL
        {
            get
            {
                return T("WIRT_CO2_TRAEGER_RUECKFALL",
                    "CO₂-Bilanz: Netzbezug mit dem Emissionsfaktor des Energieträgers „{0}“ " +
                    "gerechnet — dem Projekt ist kein Stromträger zugeordnet. Ausweg: unter " +
                    "„Berichte & Kosten › Energieträger“ zuordnen.");
            }
        }

        /// <summary>
        /// <b>Strombedarf ohne Verwendung</b> (Anwenderentscheide 22.09.2026): Das Projekt
        /// führt einen Netzbezug, aber keinen Erzeuger, der Strom verwendet
        /// (<see cref="ProjektEnergietraegerCtrl.StromOhneVerwendung"/>). Energiekosten
        /// UND Emissionen entstehen dann OHNE diesen Strom — und das wird gesagt, statt
        /// still zu wirken.
        /// </summary>
        internal static string HINWEIS_STROMBEDARF_OHNE_VERWENDUNG
        {
            get
            {
                return T("WIRT_HINWEIS_STROMBEDARF_OHNE_VERWENDUNG",
                    "Strombedarf ohne Verwendung: Das Projekt führt einen Strombedarf von " +
                    "{0} MWh/a, aber keinen Erzeuger, der Strom verwendet. Energiekosten und " +
                    "Emissionen sind ohne diesen Strom bestimmt.");
            }
        }

        /// <summary>
        /// <b>Die Gruppenregel</b> zu „Strombedarf ohne Verwendung": Im Vergleich einer
        /// Gruppe, in der ein anderer Stand Strom verwendet, wird der Netzbezug dieses
        /// Standes bepreist und bewertet (<see cref="VariantenDaten.StromImVergleichBepreisen"/>).
        /// {0} = Stand, {1} = die Stände mit Stromverwendung, {2} = Menge [MWh/a].
        /// </summary>
        internal static string HINWEIS_STROM_GRUPPENREGEL
        {
            get
            {
                return T("WIRT_HINWEIS_STROM_GRUPPENREGEL",
                    "Strombedarf ohne Verwendung im Stand „{0}“: Im Vergleich mit {1} wird der " +
                    "Netzbezug von {2} MWh/a bepreist und bewertet (Gruppenregel).");
            }
        }

        /// <summary>
        /// MyResource mit deutschem Rückfall (Drei-Schichten-Regel) — dasselbe Muster
        /// wie <c>WirtschaftlichkeitCtrl.T</c> und <c>KohaerenzPruefung.T</c>. Der
        /// Rückfall greift auf einer Ressourcendatei ohne den Schlüssel.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

        public static void Berechne(VariantenDaten v)
        {
            Berechne(v, null);
        }

        /// <summary>
        /// ETAPPE E9a (Schritt C, E9a‑Q3 Lesart a) — dieselbe Rechnung mit den
        /// <b>Trägerpreisen eines Szenarios</b>: Für BEST und WORST ersetzt ein gepflegter
        /// Szenariopreis (<see cref="TraegerpreisSzenario"/>) Arbeits-, Grund- bzw.
        /// Leistungspreis des Trägers als Ganzes; ohne Pflege, für ERWARTET und für
        /// <c>null</c> ist die Rechnung Zeichen für Zeichen die von <see cref="Berechne(VariantenDaten)"/>.
        /// Aufgerufen von der Wirtschaftlichkeit auf einer KOPIE der Variante
        /// (<c>WirtschaftlichkeitCtrl.Szenariodaten</c>) — das Original trägt weiter die
        /// Erwartet-Zahlen.
        /// </summary>
        public static void Berechne(VariantenDaten v, string szenario)
        {
            if (v == null || v.Ergebnis == null) return;
            try { BerechneIntern(v, szenario); }
            catch
            {
                v.SzenarioStrompreisGepflegt = false;              // E9a
                v.SzenarioLeistungspreisOhneWirkung = new List<string>();
                v.Energiekosten = null; v.StromkostenNetz = null;
                v.EnergieLeistungsanteil = null;
                v.CO2Gesamt = null; v.CO2Spezifisch = null; v.CO2Brennstoff = null;
                v.BiogenMengeMWh = 0; v.BiogenBehgMengeMWh = 0;
                v.EmissionsModus = DbWerte.EMISSION_MODUS_CO2;
                v.CO2StrommixRueckfall = false;
                v.KesselVerbrauchFehlt = false;                  // B-1/N1
                v.KesselOhneVerbrauch = new List<string>();
                v.StromTraegerRueckfall = null;
                v.CO2TraegerRueckfall = null;
                v.StrombedarfOhneVerwendungMWh = null;
                v.StromGruppenregelMWh = null;
                v.BezugsspitzeKW = null;
                v.LeistungspreisOhneSpitze = null;
                KaeltestromZuruecksetzen(v);
                v.EnergiekostenGrund = GRUND_RECHENFEHLER;
            }
        }

        private static void BerechneIntern(VariantenDaten v, string szenario)
        {
            ErgebnisModel m = v.Ergebnis;
            v.EnergiekostenGrund = null;         // Auftrag #267 — frischer Lauf
            v.SzenarioStrompreisGepflegt = false;                          // E9a
            v.SzenarioLeistungspreisOhneWirkung = new List<string>();     // E9a
            v.StromTraegerRueckfall = null;
            v.CO2TraegerRueckfall = null;        // Auftrag #293
            v.StrombedarfOhneVerwendungMWh = null;   // Anwenderentscheid 22.09.2026
            v.StromGruppenregelMWh = null;           // Gruppenregel (nur im Vergleich gesetzt)
            v.LeistungspreisOhneSpitze = null;
            KaeltestromZuruecksetzen(v);              // KU2 Welle 3, E34

            // Die Bezugsspitze ist eine HERLEITUNG des Laufs, kein Preisergebnis: Sie
            // steht auch dann an der Variante, wenn kein Leistungspreis gepflegt ist —
            // dann trägt sie nichts zu den Kosten bei und bleibt reine Auskunft
            // (Vergleichszeile „Bezugsspitze Strom").
            v.BezugsspitzeKW = (v.Zeitreihen != null && v.Zeitreihen.Bezugsspitze != null &&
                                v.Zeitreihen.Bezugsspitze.JahrKW > 0)
                               ? (double?)v.Zeitreihen.Bezugsspitze.JahrKW : null;

            // BERECHNUNGSMODUS (F7) - EINMAL je Lauf gelesen und am Ergebnis vermerkt.
            // Der Vermerk ist der Grund, weshalb ein Bericht die Zahl richtig
            // beschriften kann: Er nennt den Modus, in dem sie ENTSTANDEN ist, und
            // nicht den, der beim Drucken gerade eingestellt sein mag.
            string modus = EmissionenCtrl.ModusFuerRechenlauf(v.IdProjekt);
            v.EmissionsModus = modus;

            // ---------------- Verbrauch je Energieträger einsammeln (MWh/a) ----------------
            var verbrauchJeTraeger = new Dictionary<int, double>();   // carrier_id -> MWh
            double verbrauchOhneTraeger = 0;                          // Module ohne carrier_id

            Action<int, double> add = (carrier, mwh) =>
            {
                if (mwh <= 0) return;
                if (carrier <= 0) { verbrauchOhneTraeger += mwh; return; }
                if (!verbrauchJeTraeger.ContainsKey(carrier)) verbrauchJeTraeger[carrier] = 0;
                verbrauchJeTraeger[carrier] += mwh;
            };

            if (m.BHKW != null && m.BHKW.Module != null)
                foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module) add(mo.CarrierId, mo.Verbrauch);

            // BEFUND B-1 (Anwenderentscheid 18.09.2026: „Verbrauch aus dem Lauf
            // nachziehen"): DIE WARNUNG BLEIBT, SIE SCHWEIGT JETZT NUR.
            //
            // Ein Kessel, der Wärme erzeugt hat (Waerme_Gas + Waerme_Oel > 0), aber
            // keinen Brennstoffverbrauch ausweist, fällt unten in `add` durch die
            // Klemme „mwh <= 0" — und zwar BEVOR die Trägerprüfung greift. Sein
            // Brennstoff fehlte dann still in Energiekosten, CO₂-Bilanz und
            // BEHG-Menge (Folgebefund N1: `kostenVollstaendig` blieb dabei true).
            // Seit B-1 füllt der SimulationRunner die Spalte, der Fall tritt also im
            // frischen Lauf nicht mehr ein; die Fahne bleibt als Wächter stehen für
            // den Fall, dass die Kette doch einmal reißt, und für gespeicherte Läufe
            // von vor B-1, die ihre 0 behalten.
            //
            // Weiterhin gilt: NUR MELDEN, NICHT ABLEITEN. Es wird WEDER eine
            // Ersatzmenge gebildet (die Steuerseite tut das über den
            // Jahresnutzungsgrad; das ist eine Rechtsvorschrift und keine Vorlage für
            // die Kostenkette) NOCH `kostenVollstaendig` gekippt — das nähme jedem
            // betroffenen Kesselprojekt den Kapitalwert.
            //
            // DER ELEKTROKESSEL IST AUSGENOMMEN. Er bucht seinen Einsatz auf den
            // Stromzähler (SimulationSPK, Bilanz_und_Nutzungsgrad) und steht über den
            // Reststrombedarf im Netzbezug, den diese Rechnung eigens bepreist; seine
            // Modulzeile führt deshalb BEWUSST keinen Brennstoffverbrauch. Ohne diese
            // Ausnahme stünde bei jedem Projekt mit Elektrokessel dauerhaft eine
            // Warnung über einen Brennstoff, den es dort nicht gibt.
            var stromkessel = StromKesselNamen(v.IdProjekt);
            var kesselOhneVerbrauch = new List<string>();
            if (m.Heizkessel != null && m.Heizkessel.Module != null)
                foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                {
                    add(mo.CarrierId, mo.Verbrauch);
                    // Der Modulname ist der Anlagen-Bezeichner des Anwenders (Datenwert,
                    // kein Anzeigetext); ohne Namen bleibt das sprachneutrale "?" —
                    // dafür lohnt kein Ressourcenschlüssel. JE NAME EINMAL: Eine
                    // Kaskade aus sechs baugleichen Kesseln führt sechs Modulzeilen
                    // desselben Namens; die Meldung nennt den Kessel dann einmal
                    // statt sechsmal (die Reihenfolge des Ergebnisses bleibt).
                    if (mo.Verbrauch <= 0 && (mo.Waerme_Gas + mo.Waerme_Oel) > 0)
                    {
                        string name = string.IsNullOrEmpty(mo.Modul) ? "?" : mo.Modul;
                        if (stromkessel.Contains(name.Trim())) continue;
                        if (!kesselOhneVerbrauch.Contains(name)) kesselOhneVerbrauch.Add(name);
                    }
                }
            v.KesselOhneVerbrauch = kesselOhneVerbrauch;
            v.KesselVerbrauchFehlt = kesselOhneVerbrauch.Count > 0;

            // ---------------- Brennstoffe: Kosten + CO₂ ----------------
            double brennstoffKosten = 0, brennstoffCO2t = 0, behgCO2t = 0;
            double biogenMWh = 0, biogenBehgMWh = 0;                  // L13
            bool kostenVollstaendig = verbrauchOhneTraeger <= 0;
            bool co2Vollstaendig = verbrauchOhneTraeger <= 0;

            // F7: Die BEHG-Menge hat ihre EIGENE Vollständigkeit. Im Modus CO2 sind
            // beide Fahnen deckungsgleich (wirksamer Faktor = reines CO₂); im Modus
            // CO2E kann ein Träger ein Äquivalent führen, ohne dass sein reines CO₂
            // gepflegt wäre - dann ist die Kennzahl bestimmbar und die Abgabemenge
            // nicht. Eine gemeinsame Fahne machte aus dem einen Loch zwei.
            bool behgVollstaendig = verbrauchOhneTraeger <= 0;

            // KD4/FK6: Leistungsanteil der BRENNSTOFFträger — Basis ist die
            // vorgehaltene Anschlussleistung aus den Gerätedaten. Der STROMträger
            // rechnet weiter unten mit seiner eigenen Basis (der gemessenen
            // Bezugsspitze); beide Anteile laufen in dieselbe Summe.
            double leistungsAnteil = 0;
            bool leistungGepflegt = false;
            int stromCarrierId = Emissionsquelle.StromTraeger(v.IdProjekt);

            // AUFTRAG #267 — der Träger, mit dem der NETZBEZUG BEPREIST wird.
            //
            // BEFUND (Anwenderbefund 14.09.2026, Projekt „Beispiel WP WG 1"). Eine
            // Wärmepumpe trägt keinen eigenen ID_Carrier, und wer sie außerhalb des
            // Assistenten anlegt, bekommt auch keine Zeile in energy_project_settings.
            // Emissionsquelle.StromTraeger lieferte dann 0, stromKosten blieb null und
            // damit die ganzen Energiekosten — obwohl die KOSTENSEITE dem Anwender
            // längst einen Stromträger anzeigt und der Assistent genau diesen zuordnen
            // würde. Drei Stellen, zwei Antworten.
            //
            // DIE REGEL IST JETZT DIESELBE WIE DORT: erst der zugeordnete bzw. an der
            // Anlage gewählte Träger (Emissionsquelle.StromTraeger), sonst der
            // Auslieferungsträger des Katalogs (ProjektEnergietraegerCtrl.
            // StandardStromTraeger — die Fassung, die Anzeige und Automatik lesen).
            // Der Rückfall wird an der Variante VERMERKT, nie stillschweigend
            // verrechnet.
            //
            // NUR DIE KOSTEN. Der CO₂-Faktor unten bleibt am ZUGEORDNETEN Träger: Er
            // hat mit STROMMIX_CO2_G_JE_KWH einen benannten Vorgabewert, der gerade
            // deshalb gemeldet wird (v.CO2StrommixRueckfall) — und „kein Träger
            // zugeordnet" ist genau die Aussage, die diese Fahne trägt. Die Kosten
            // haben keinen solchen Vorgabewert und dürfen keinen erfinden; sie fragen
            // deshalb den Träger, den die Kostenseite ohnehin nennt.
            int stromCarrierKosten = stromCarrierId;
            bool stromAusRueckfall = false;
            if (stromCarrierKosten <= 0)
            {
                int rueckfall = StandardStromTraeger(v.IdProjekt);
                // GRUPPENREGEL „Strombedarf ohne Verwendung": Ein Stand ohne eigene
                // Stromverwendung, dessen Netzbezug im Vergleich bepreist wird, bekommt
                // denselben Auslieferungsträger — ohne die Vorbedingung der elektrischen
                // Welt, die ihn sonst ausschließt. Der Rückfall wird vermerkt wie jeder.
                if (rueckfall <= 0 && v.StromImVergleichBepreisen)
                    rueckfall = ProjektEnergietraegerCtrl.StromTraegerImVergleich(v.IdProjekt);
                if (rueckfall > 0) { stromCarrierKosten = rueckfall; stromAusRueckfall = true; }
            }

            // Träger MIT Verbrauch, aber OHNE Arbeitspreis — für den Grundtext unten.
            var ohnePreis = new List<string>();

            foreach (KeyValuePair<int, double> kv in verbrauchJeTraeger)
            {
                // ETAPPE E9a (Schritt C): im Szenariolauf mit den wirksamen Szenariopreisen.
                TraegerInfo info = LadeTraeger(v.IdProjekt, kv.Key, szenario);

                // L13: die MENGE biogener Träger — unabhängig davon, ob ein Faktor
                // gepflegt ist. Die Konventionsfrage entscheidet der Aufrufer.
                if (info.Biogen)
                {
                    biogenMWh += kv.Value;
                    if (info.BehgBiogen) biogenBehgMWh += kv.Value;
                }

                // Kosten: mengenbasiert (Heizwert vorhanden) oder Direktabrechnung je kWh.
                if (info.PreisArbeit.HasValue)
                {
                    double kosten;
                    if (info.EffHi.HasValue && info.EffHi.Value > 0)
                    {
                        double menge = kv.Value * 1000.0 / info.EffHi.Value;   // Abrechnungseinheit
                        kosten = menge * info.PreisArbeit.Value;
                    }
                    else
                        kosten = kv.Value * 1000.0 * info.PreisArbeit.Value;   // €/kWh direkt
                    if (info.Grundpreis.HasValue) kosten += info.Grundpreis.Value;   // je Träger einmal p. a.
                    brennstoffKosten += kosten;
                }
                else
                {
                    kostenVollstaendig = false;
                    string name = TraegerName(kv.Key);   // #267: den Träger beim Namen nennen
                    if (!ohnePreis.Contains(name)) ohnePreis.Add(name);
                }

                // Leistungspreis der BRENNSTOFFträger (Etappe KD4/FK6): Basis ist die
                // VORGEHALTENE Anschlussleistung aus den Gerätedaten (§ 7.1-Umsetzung);
                // Modus JAHR = Satz × kW, MONAT = Satz × kW × 12. Eine gepflegte
                // Saisonreihe (FK6a) gilt vor dem konstanten Satz: Summe der zwölf
                // Monatssätze × kW. Fehlt die Basis (keine plausible
                // Geräteleistung), entsteht bewusst KEIN Anteil — ein Fantasiewert
                // wäre schlimmer als ein fehlender.
                //
                // DER STROMTRÄGER BLEIBT HIER AUSSEN VOR — nicht, weil er keinen
                // Leistungspreis hätte, sondern weil seine BASIS eine andere ist:
                // Ein Stromanschluss wird nicht nach vorgehaltener Anlagenleistung
                // abgerechnet, sondern nach der gemessenen Bezugsspitze. Sein Anteil
                // entsteht deshalb im Netzbezugsblock weiter unten.
                // ETAPPE E9a (E9a‑Q3): Eine gepflegte Saisonreihe geht dem konstanten Satz vor
                // — ein Szenario-Leistungspreis bleibt dann ohne Wirkung und wird benannt.
                if (info.LeistungSzenarioGepflegt && info.ReihenSummeJeKW.HasValue &&
                    kv.Key != stromCarrierId)
                    SzenarioLeistungOhneWirkung(v, kv.Key);

                if ((info.ReihenSummeJeKW.HasValue || info.PreisLeistung.HasValue) &&
                    kv.Key != stromCarrierId)
                {
                    double kw = AnschlussleistungKW(v.IdProjekt, kv.Key);
                    if (kw > 0)
                    {
                        double anteil = info.ReihenSummeJeKW.HasValue
                            ? info.ReihenSummeJeKW.Value * kw
                            : (string.Equals(info.LeistungsModus,
                                   DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal)
                                ? info.PreisLeistung.Value * kw * 12.0
                                : info.PreisLeistung.Value * kw);
                        brennstoffKosten += anteil;
                        leistungsAnteil += anteil;
                        leistungGepflegt = true;
                    }
                }

                // CO₂ (g/kWh, s. Klassenkommentar). Die ausgewiesene Kennzahl folgt dem
                // MODUS (F7), die BEHG-Basis bleibt reines CO₂.
                double? wirksam = info.Faktoren.Wirksam(modus);
                if (wirksam.HasValue && wirksam.Value > 0)
                    brennstoffCO2t += kv.Value * wirksam.Value / 1000.0;
                else
                    co2Vollstaendig = false;

                if (info.CO2.HasValue && info.CO2.Value > 0)
                {
                    if (info.BehgPflichtig)
                        behgCO2t += kv.Value * info.CO2.Value / 1000.0;   // BEHG-Basis (Phase 7/W2)
                }
                else
                    behgVollstaendig = false;
            }

            // ---------------- Netzbezug Strom ----------------
            double netzbezugMWh = m.Energiebedarf != null ? m.Energiebedarf.Stromrestbedarf : 0;

            // ---- STROMBEDARF OHNE VERWENDUNG (Anwenderentscheide 22.09.2026) ----
            //
            // „Energiekosten (Strom, Gas, …) sollen nur anfallen, falls sie auch
            // Verwendung finden." Führt das Projekt einen Netzbezug, aber keinen
            // Erzeuger, der Strom verwendet (Wärmepumpe, Photovoltaik, Stromspeicher,
            // Heizstab, Elektrokessel, BHKW, Hilfsenergie einer Brenneranlage), dann
            // bleibt dieser Strom in Energiekosten UND Emissionen außen vor — gleich, ob
            // dem Projekt ein Stromträger zugeordnet ist oder der Auslieferungsträger
            // einen Katalogpreis trägt.
            //
            // DIE FRAGE STELLT DIE EINE FASSUNG (ProjektEnergietraegerCtrl.
            // StromOhneVerwendung) — dieselbe, aus der Stromträger-Automatik, Rückfallträger
            // und Kohärenzprüfung ihre Antwort nehmen. Hier wird sie EINMAL gestellt, und
            // alles, was den Netzbezug bewertet, hängt an ihrem Ergebnis:
            //  - KOSTEN: Der Preisträger wird gar nicht erst geladen; StromkostenNetz bleibt
            //    null (nicht 0), damit auch der Rollentarif der
            //    Wirtschaftlichkeit, der StromkostenNetz ersetzt, keinen Strom
            //    nachträglich bepreisen. Kein Rückfallvermerk — bepreist wurde nichts.
            //  - EMISSIONEN: kein Netzstromfaktor, netzCO2t = 0 (CO₂ bzw. im Modus CO2E
            //    das Äquivalent aller Arten — weitere Schadstoffe leitet dieser Rechner
            //    aus dem Netzbezug nicht ab); weder Strommix- noch Trägerrückfall.
            //  - ANLAGENZEILE „Netzbezug" der Energiekosten je Anlage: entfällt.
            //  - HINWEIS: v.StrombedarfOhneVerwendungMWh trägt die Menge; die
            //    Wirtschaftlichkeit macht daraus die Warnzeile (kein Fehlgrund).
            //
            // DIE GRUPPENREGEL: Im VERGLEICH einer Gruppe, in der ein anderer Stand Strom
            // verwendet (ProjektEnergietraegerCtrl.GruppeVerwendetStrom), setzt die
            // Wirtschaftlichkeit auf ihrer Kopie der Variante StromImVergleichBepreisen.
            // Dann wird der Netzbezug bepreist und bewertet wie bei jedem Stand mit
            // Stromverwendung; v.StromGruppenregelMWh trägt die Menge für den Hinweis.
            // Die Einzelbetrachtung setzt das Feld nie — dort gilt die Regel je Stand.
            bool ohneVerwendungImStand =
                ProjektEnergietraegerCtrl.StromOhneVerwendung(v.IdProjekt, netzbezugMWh);
            bool stromOhneVerwendung = ohneVerwendungImStand && !v.StromImVergleichBepreisen;
            if (stromOhneVerwendung) v.StrombedarfOhneVerwendungMWh = netzbezugMWh;
            else if (ohneVerwendungImStand) v.StromGruppenregelMWh = netzbezugMWh;

            // Der Netzbezug, den Kosten- UND CO₂-Seite bewerten. Ohne Verwendung ist er
            // null — damit greift unten derselbe Zweig wie bei einem Projekt ganz ohne
            // Netzbezug: Energiekosten = Brennstoffkosten, kein Fehlgrund.
            double netzbezugBewertet = stromOhneVerwendung ? 0.0 : netzbezugMWh;

            // ---- DER KÄLTESTROM EINES ABWEICHENDEN KÜHLTRÄGERS (Entscheid E34, KU2 Welle 3) ----
            //
            // Trägt eine Wärmepumpe für die Kühlung einen anderen Stromträger als das Projekt, hat
            // der Lauf die Menge je Anlage gespeichert (Modulspalte Kaeltestrom_Netzbezug samt
            // Kühlträger und Abrechnungsart, Schemaschritt 119). HIER — und nur hier — wird sie
            // bepreist und bewertet:
            //  - ANTEILIG AM NETZBEZUG (Vorgabe): die Menge ist ein TEIL von Stromrestbedarf. Sie
            //    trägt Arbeitspreis und CO₂-Faktor des Kühlträgers; der Stromträger des Projekts
            //    bepreist den Rest — samt Grund- und Leistungspreis, die Bezugsspitze bleibt die
            //    des ganzen Anschlusses.
            //  - EIGENER ZÄHLER: die Menge liegt NEBEN Stromrestbedarf und trägt ganz den
            //    Kühlträger — Arbeitspreis und CO₂-Faktor, dazu (Entscheid E35, Konzept
            //    Gebäudesimulation N1.40) Grund- und Leistungspreis des Kühlträgers: je Zähler (je
            //    Anlage) einen Grundpreis und den Leistungspreis auf die EIGENE Spitze des
            //    Kältestroms dieser Anlage, nach derselben Regel wie beim Projektträger.
            // Anteilig setzt die Regel Grund- und Leistungspreis des Kühlträgers nicht an (E34: sie
            // bleiben beim Projektträger). Ohne abweichenden Kühlträger sind beide Listen leer, und
            // jede Zahl unten entsteht Zeichen für Zeichen wie ohne diesen Block.
            List<Kaeltestromabrechnung.Anteil> kuehlAnteile = stromOhneVerwendung
                ? new List<Kaeltestromabrechnung.Anteil>() : Kaeltestromabrechnung.Anteile(m);
            double kuehlAnteiligMWh = 0.0, kuehlZaehlerMWh = 0.0;
            foreach (Kaeltestromabrechnung.Anteil a in kuehlAnteile)
            {
                if (a.EigenerZaehler) kuehlZaehlerMWh += a.MengeMwh;
                else kuehlAnteiligMWh += a.MengeMwh;
            }
            // Der Teil des Netzbezugs, den der Stromträger des Projekts trägt.
            double netzbezugProjektMWh = kuehlAnteiligMWh > 0
                ? Math.Max(0.0, netzbezugMWh - kuehlAnteiligMWh) : netzbezugMWh;
            double netzbezugProjektBewertet = stromOhneVerwendung ? 0.0 : netzbezugProjektMWh;

            double? stromKosten = null;
            double stromCO2 = STROMMIX_CO2_G_JE_KWH;   // Vorgabewert, falls kein Träger gepflegt
            int stromCarrier = stromCarrierId;   // bereits vor der Brennstoffschleife bestimmt (KD4)
            string stromPreisTraeger = null;     // #267: Name des bepreisenden Trägers

            // BEFUND 30.08.2026: Der Vorgabewert greift STILL. Er wird jetzt festgehalten
            // (v.CO2StrommixRueckfall) - siehe Feldkommentar in VariantenDaten.
            bool strommixRueckfall = true;

            // #267: DIE KOSTENSEITE fragt den Träger mit Rückfall (stromCarrierKosten),
            // die CO₂-Seite den ZUGEORDNETEN (stromCarrier). Sind beide gleich — der
            // Regelfall —, wird auch nur EINMAL geladen. Ohne Verwendung gar nicht.
            double? projektArbeitspreis = null;   // KU2 Welle 3: für den Ausweis des Kältestroms
            if (!stromOhneVerwendung && stromCarrierKosten > 0)
            {
                // ETAPPE E9a (Schritt C): im Szenariolauf mit den wirksamen Szenariopreisen
                // des Stromträgers; ob einer davon gepflegt war, wird vermerkt — das
                // Rollenmodell ersetzt den Stromanteil und meldet dann, dass der
                // Szenario-Strompreis nicht wirkt (E9a‑Q7).
                TraegerInfo preistraeger = LadeTraeger(v.IdProjekt, stromCarrierKosten, szenario);
                v.SzenarioStrompreisGepflegt = preistraeger.SzenarioGepflegt;
                if (preistraeger.LeistungSzenarioGepflegt &&
                    (preistraeger.Staffel.Gepflegt || preistraeger.ReiheJeKW != null))
                    SzenarioLeistungOhneWirkung(v, stromCarrierKosten);
                stromPreisTraeger = TraegerName(stromCarrierKosten);
                projektArbeitspreis = preistraeger.PreisArbeit;
                if (preistraeger.PreisArbeit.HasValue)
                {
                    // E34: ohne den Anteil der abweichenden Kühlträger (sonst der ganze Netzbezug).
                    stromKosten = netzbezugProjektMWh * 1000.0 * preistraeger.PreisArbeit.Value;
                    if (preistraeger.Grundpreis.HasValue) stromKosten += preistraeger.Grundpreis.Value;

                    // ---- DER LEISTUNGSPREIS DES STROMTRÄGERS (Anwenderentscheid
                    // 17.09.2026, SP-E-1 a / Q1 Viertelstunde) ----
                    //
                    // Der Strompreis setzt sich aus Arbeits-, Grund- UND
                    // Leistungspreis zusammen. Ohne den dritten Bestandteil ging der
                    // Effekt der Lastspitzenkappung im Variantenvergleich verloren:
                    // Ein Speicher, der die Spitze halbiert, senkte die Energiekosten
                    // um keinen Cent, weil nur die ARBEIT bepreist wurde — und die
                    // ändert der Speicher kaum.
                    //
                    // BASIS IST DIE GEMESSENE BEZUGSSPITZE im Viertelstundenraster
                    // (v.Zeitreihen.Bezugsspitze, gebildet aus derselben Reihe, die
                    // der Speicher kappt), nicht die Anschlussleistung der Geräte:
                    // Der Netzbetreiber misst die Viertelstundenleistung, und ein
                    // Stundenmittel (StromMatrix.MaxBezugKW) fiele regelmäßig zu
                    // niedrig aus.
                    //
                    // MODUS JAHR: Satz [€/(kW·a)] × Jahresspitze.
                    // MODUS MONAT: Σ über 12 Monate (Monatsspitze × Satz [€/(kW·Mon)]).
                    // SAISONREIHE (FK6a) geht vor, wie im Brennstoffzweig: Σ über
                    // 12 Monate (Monatssatz × Monatsspitze).
                    //
                    // OHNE SPITZE KEIN ANTEIL, aber auch kein Schweigen: Führt der
                    // Lauf keine Zeitreihen, wird der gepflegte Leistungspreis
                    // benannt (v.LeistungspreisOhneSpitze) statt still zu entfallen.
                    //
                    // KEINE ZWEITE WAHRHEIT: Ein aktiver Rollentarif ersetzt den
                    // ganzen Stromanteil (WirtschaftlichkeitCtrl rechnet
                    // v.StromkostenNetz heraus) — dieser Anteil ist deshalb
                    // ausdrücklich TEIL von StromkostenNetz und fällt dort mit heraus.
                    //
                    // DIE ZWEISTUFIGE STAFFEL (Q11, Schemaschritt 104): Sie stand im
                    // Tarifsatz und rechnete nur im Zonenmodell; jetzt steht sie am
                    // Stromträger und geht dessen Leistungspreis und Saisonreihe VOR —
                    // dieselbe Rangfolge wie damals, als der Tarif den ganzen
                    // Stromanteil ersetzte. Bemessen an der JAHRESspitze der
                    // Viertelstundenreihe, in €/(kW·a), gleich welcher Modus am Träger
                    // steht: min(S, Grenze) × Preis 1 + max(0, S − Grenze) × Preis 2.
                    //
                    // E35: Dieselbe Regel bepreist die eigene Spitze eines Kältestromzählers
                    // (LeistungsanteilStrom, unten beim Kältestrom) — eine Regel, zwei Spitzen.
                    if (LeistungspreisStrom(preistraeger))
                    {
                        Netzbezugsspitze spitze = v.Zeitreihen != null ? v.Zeitreihen.Bezugsspitze : null;
                        if (spitze != null && spitze.JahrKW > 0)
                        {
                            double anteilStrom = LeistungsanteilStrom(preistraeger, spitze);

                            stromKosten += anteilStrom;
                            leistungsAnteil += anteilStrom;
                            leistungGepflegt = true;
                        }
                        else
                            LeistungspreisOhneSpitzeVermerken(v, stromCarrierKosten);
                    }

                    // Der Vermerk steht NUR, wenn der Rückfall auch wirklich einen
                    // Betrag getragen hat — sonst behauptete die Hinweiszeile eine
                    // Bepreisung, die gar nicht stattgefunden hat.
                    if (stromAusRueckfall) v.StromTraegerRueckfall = TraegerName(stromCarrierKosten);
                }
            }

            // AUFTRAG #293 (Anwenderentscheid 15.09.2026: „bereits zugewiesene
            // CO₂-Zahlen nicht überschreiben").
            //
            // Der Emissionsfaktor des Netzbezugs kommt aus EINER Stelle —
            // Emissionsquelle.Netzstrom —, derselben, aus der auch die Autarkie-Kachel
            // liest: zugeordneter Stromträger über die Lesekette (EmissionsFaktorLader,
            // im MODUS des Laufs, F7) → nur wenn KEINER zugeordnet ist der
            // Auslieferungsträger des Katalogs → sonst der Vorgabewert.
            //
            // DIE ZWEITE STUFE FÜLLT AUSSCHLIESSLICH LÜCKEN: Wo ein Träger zugeordnet
            // ist, bleibt sein Faktor unangetastet — auch ein Träger, dessen Kette
            // nichts hergibt, rechnet weiter mit dem Vorgabewert und meldet das über
            // CO2StrommixRueckfall. Verändert wird nur die Lage, in der bis hierher
            // gar keine Projektzahl stand: kein Stromträger, anonymer Vorgabewert.
            //
            // OHNE VERWENDUNG wird kein Faktor gezogen: Der Netzbezug geht mit 0 MWh in
            // die Bilanz, und ein Faktor dafür wäre eine Herleitung ohne Rechnung.
            //
            // GRUPPENREGEL: Ein Stand ohne eigene Stromverwendung und ohne zugeordneten
            // Stromträger bekommt von Emissionsquelle.Netzstrom keinen Rückfallträger (die
            // Vorbedingung der elektrischen Welt schließt ihn aus). Im Vergleich nimmt er
            // deshalb den Träger, der auch seinen Netzbezug bepreist — geliehen und vermerkt.
            int co2Traeger = stromCarrier;
            bool co2ImVergleichGeliehen = false;
            if (co2Traeger <= 0 && v.StromGruppenregelMWh.HasValue && stromCarrierKosten > 0)
            {
                co2Traeger = stromCarrierKosten;
                co2ImVergleichGeliehen = true;
            }
            Emissionsfaktoren netz = stromOhneVerwendung
                ? null : Emissionsquelle.Netzstrom(v.IdProjekt, co2Traeger, modus);
            if (netz != null && netz.Co2Gepflegt && netz.Co2GKwh > 0)
            {
                stromCO2 = netz.Co2GKwh;
                strommixRueckfall = false;

                // DIE HERLEITUNG. Sie steht nur, wenn der geliehene Faktor auch wirklich
                // eine Menge getragen hat — sonst behauptete die Zeile eine Rechnung,
                // die gar nicht stattgefunden hat (dieselbe Klemme wie beim Preisträger).
                if (netz.RueckfallTraegerId > 0 && netzbezugBewertet > 0)
                    v.CO2TraegerRueckfall = TraegerName(netz.RueckfallTraegerId);
                else if (co2ImVergleichGeliehen && netzbezugBewertet > 0)
                    v.CO2TraegerRueckfall = TraegerName(co2Traeger);
            }
            // Ohne (bewerteten) Netzbezug ändert der Vorgabewert nichts - dann ist er kein
            // Rückfall, sondern eine Zahl, die mit 0 MWh multipliziert wird.
            v.CO2StrommixRueckfall = strommixRueckfall && netzbezugProjektBewertet > 0;

            // E34: der Stromträger des Projekts trägt den Netzbezug ohne den Anteil der
            // abweichenden Kühlträger; die tragen ihre Menge mit IHREM Faktor (dieselbe Stufenfolge,
            // Emissionsquelle.Netzstrom - ein Kühlträger ohne Faktor rechnet mit dem Vorgabewert
            // und meldet das wie der Träger des Projekts).
            double netzCO2t = netzbezugProjektBewertet * stromCO2 / 1000.0;

            double kuehlKosten = 0.0, kuehlCO2t = 0.0;
            var kuehlOhnePreis = new List<string>();
            foreach (Kaeltestromabrechnung.Anteil a in kuehlAnteile)
            {
                // E9a: im Szenariolauf mit dem wirksamen Szenariopreis des Kühlträgers - derselbe
                // Leseweg wie für den Stromträger des Projekts.
                TraegerInfo kt = LadeTraeger(v.IdProjekt, a.Traeger, szenario);
                if (kt.PreisArbeit.HasValue) kuehlKosten += a.MengeMwh * 1000.0 * kt.PreisArbeit.Value;
                else
                {
                    string name = TraegerName(a.Traeger);
                    if (!kuehlOhnePreis.Contains(name)) kuehlOhnePreis.Add(name);
                }

                Emissionsfaktoren kf = Emissionsquelle.Netzstrom(v.IdProjekt, a.Traeger, modus);
                double faktor = kf != null && kf.Co2GKwh > 0 ? kf.Co2GKwh : STROMMIX_CO2_G_JE_KWH;
                if (kf == null || !kf.Co2Gepflegt || !(kf.Co2GKwh > 0)) v.CO2StrommixRueckfall = true;
                kuehlCO2t += a.MengeMwh * faktor / 1000.0;
            }
            netzCO2t += kuehlCO2t;

            // ---- ENTSCHEID E35: GRUND- UND LEISTUNGSPREIS DES EIGENEN ZÄHLERS ----
            //
            // Je Zähler — je Anlage mit abweichendem Kühlträger und eigenem Zähler, auch ohne
            // Kältestrom im Jahr — der Grundpreis des Kühlträgers, und, wenn der Träger einen
            // Leistungspreis führt, dieser auf die eigene Spitze des Kältestroms der Anlage
            // (Zeitreihen.Kaeltestromspitzen, Viertelstundenraster) — Staffel, Saisonreihe, Satz je
            // Jahr oder je Monat, wie beim Projektträger (LeistungsanteilStrom). Fehlt die Spitze,
            // weil der Lauf keine Zeitreihen geführt hat, wird der Träger benannt statt still
            // übergangen (dieselbe Fahne wie beim Projektträger). E9a: im Szenariolauf mit den
            // wirksamen Szenariopreisen des Kühlträgers; ein Szenario-Leistungspreis neben Staffel
            // oder Saisonreihe bleibt ohne Wirkung und wird benannt.
            List<Kaeltestromabrechnung.Zaehler> kuehlZaehler = stromOhneVerwendung
                ? new List<Kaeltestromabrechnung.Zaehler>() : Kaeltestromabrechnung.EigeneZaehler(m);
            var zaehlerZeilen = new List<EnergieAnlageNachweis>();
            foreach (Kaeltestromabrechnung.Zaehler z in kuehlZaehler)
            {
                TraegerInfo kt = LadeTraeger(v.IdProjekt, z.Traeger, szenario);
                if (kt.LeistungSzenarioGepflegt && (kt.Staffel.Gepflegt || kt.ReiheJeKW != null))
                    SzenarioLeistungOhneWirkung(v, z.Traeger);

                if (kt.Grundpreis.HasValue && kt.Grundpreis.Value > 0)
                {
                    kuehlKosten += kt.Grundpreis.Value;
                    zaehlerZeilen.Add(new EnergieAnlageNachweis
                    {
                        Anlage = string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_ENK_KAELTESTROM_ZAEHLER_GRUND, z.Anlage),
                        Traeger = TraegerName(z.Traeger),
                        MengeAbrechnung = 1.0,
                        Einheit = "a",
                        PreisJeEinheit = kt.Grundpreis.Value,
                        KostenEur = kt.Grundpreis.Value
                    });
                }

                if (!LeistungspreisStrom(kt)) continue;
                Netzbezugsspitze eigene = null;
                if (v.Zeitreihen == null || v.Zeitreihen.Kaeltestromspitzen == null ||
                    !v.Zeitreihen.Kaeltestromspitzen.TryGetValue(z.Modulindex, out eigene) || eigene == null)
                {
                    LeistungspreisOhneSpitzeVermerken(v, z.Traeger);
                    continue;
                }
                if (!(eigene.JahrKW > 0)) continue;      // kein Kältestrom - keine Spitze, kein Anteil

                double anteil = LeistungsanteilStrom(kt, eigene);
                kuehlKosten += anteil;
                leistungsAnteil += anteil;
                leistungGepflegt = true;
                double basis = LeistungsbasisKW(kt, eigene);
                zaehlerZeilen.Add(new EnergieAnlageNachweis
                {
                    Anlage = string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_ENK_KAELTESTROM_ZAEHLER_LEISTUNG, z.Anlage),
                    Traeger = TraegerName(z.Traeger),
                    MengeAbrechnung = basis,
                    Einheit = "kW",
                    PreisJeEinheit = basis > 0 ? anteil / basis : 0.0,
                    KostenEur = anteil
                });
            }

            v.StromkostenKuehltraeger = kuehlOhnePreis.Count == 0 ? kuehlKosten : 0.0;
            v.NetzbezugKuehltraegerMWh = kuehlAnteiligMWh;
            v.KuehlzaehlerMWh = kuehlZaehlerMWh;

            // DER AUSWEIS DES KÄLTESTROMS (Kühlkonzept 6.2, 6.3): sein Netzbezug — über alle
            // Anlagen, gleich welcher Träger ihn bepreist —, seine Arbeitskosten und seine
            // Emissionen. Die Anteile der abweichenden Kühlträger stehen oben; der Rest trägt
            // Arbeitspreis und Faktor des Projekts. Nur ein Ausweis: In Energiekosten und
            // CO2Gesamt steht der Kältestrom genau einmal (als Teil des Netzbezugs bzw. als
            // Menge eines Kühlträgers).
            double? kaelteNetzbezug = stromOhneVerwendung ? null : Kaeltestromabrechnung.NetzbezugKaeltestromMwh(m);
            if (kaelteNetzbezug.HasValue)
            {
                double restMWh = Math.Max(0.0, kaelteNetzbezug.Value - kuehlAnteiligMWh - kuehlZaehlerMWh);
                v.KaeltestromNetzbezugMWh = kaelteNetzbezug.Value;
                v.KaeltestromKosten = kuehlOhnePreis.Count > 0 || (restMWh > 0 && !projektArbeitspreis.HasValue)
                    ? (double?)null
                    : kuehlKosten + (restMWh > 0 ? restMWh * 1000.0 * projektArbeitspreis.Value : 0.0);
                v.KaeltestromCO2t = kuehlCO2t + restMWh * stromCO2 / 1000.0;
            }

            // ---------------- ETAPPE B7: Energiekosten JE ANLAGE (Konzept § 3.5) ----
            //
            // Dieselben Mengen und dieselben Preise wie oben, nur nicht je TRÄGER
            // zusammengefasst, sondern je ANLAGE aufgeschlüsselt. Die Zeilen sind
            // reiner Ausweis: Ihre Summe IST der Brennstoffanteil oben (ohne
            // Grundpreis, der einmal je Träger anfällt und keiner einzelnen Anlage
            // gehört). Fehlt einem Träger der Preis, entsteht auch keine Zeile — eine
            // Zeile ohne Preis wäre eine Herleitung ohne Rechnung.
            var jeAnlage = new List<EnergieAnlageNachweis>();
            if (m.BHKW != null && m.BHKW.Module != null)
                foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module)
                    AnlageZeile(jeAnlage, v.IdProjekt, mo.Modul, mo.CarrierId, mo.Verbrauch, szenario);
            if (m.Heizkessel != null && m.Heizkessel.Module != null)
                foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                    AnlageZeile(jeAnlage, v.IdProjekt, mo.Modul, mo.CarrierId, mo.Verbrauch, szenario);
            // Die Stromseite als EINE Zeile: Netzbezug × Arbeitspreis. Sie steht für
            // alles, was Strom bezieht (Wärmepumpe, Hilfsenergie, Gebäude) — der
            // Rechenkern führt den Restbezug als eine Menge, und eine Aufteilung nach
            // Verbrauchern gäbe es nur als Schätzung. Ohne Verwendung keine Zeile.
            // E34: Die Zeile „Netzbezug" trägt den Teil des Stromträgers des Projekts; der Kältestrom
            // eines abweichenden Kühlträgers steht als eigene Zeile daneben.
            if (netzbezugProjektBewertet > 0 && stromCarrierKosten > 0)
                AnlageZeile(jeAnlage, v.IdProjekt, MyResource.Resource.WIRT_ENK_NETZBEZUG,
                            stromCarrierKosten, netzbezugProjektBewertet, szenario);
            foreach (Kaeltestromabrechnung.Anteil a in kuehlAnteile)
                AnlageZeile(jeAnlage, v.IdProjekt,
                            string.Format(BerichtTexte.Kultur,
                                a.EigenerZaehler ? MyResource.Resource.WIRT_ENK_KAELTESTROM_ZAEHLER
                                                 : MyResource.Resource.WIRT_ENK_KAELTESTROM,
                                string.Join(", ", a.Anlagen)),
                            a.Traeger, a.MengeMwh, szenario);
            // E35: Grund- und Leistungspreis eines eigenen Zählers gehören genau einer Anlage — sie
            // stehen deshalb (anders als die trägerweiten Fixbeträge des Projekts) je Zähler als
            // eigene Zeile, mit der Herleitung „1 a × Grundpreis" bzw. „Spitze × Satz".
            jeAnlage.AddRange(zaehlerZeilen);
            v.EnergiekostenJeAnlage = jeAnlage;

            // ---------------- Kennzahlen setzen ----------------
            v.BiogenMengeMWh = biogenMWh;             // L13 — reine Mengen, keine Wertung
            v.BiogenBehgMengeMWh = biogenBehgMWh;
            v.StromkostenNetz = stromKosten;

            // Ohne Verwendung ist netzbezugBewertet null (Regel oben, beim Netzbezug):
            // Die Energiekosten sind dann die Brennstoffkosten, ohne Fehlgrund. E34: Der Kältestrom
            // eines eigenen Zählers ist Verbrauch auch ohne Netzbezug am Hauptanschluss, und die
            // Kosten der abweichenden Kühlträger kommen hinzu — fehlt einem der Arbeitspreis, bleibt
            // die Summe aus (Fehlgrund unten).
            // E35: Ein eigener Zähler trägt auch ohne Kältestrom seinen Grundpreis.
            double? energie = (kostenVollstaendig && stromKosten.HasValue)
                ? (double?)(brennstoffKosten + stromKosten.Value)
                : (kostenVollstaendig && (verbrauchJeTraeger.Count > 0 || kuehlZaehlerMWh > 0 || kuehlZaehler.Count > 0) &&
                   netzbezugBewertet <= 0
                    ? (double?)brennstoffKosten : null);
            if (energie.HasValue && (kuehlAnteile.Count > 0 || kuehlZaehler.Count > 0))
                energie = kuehlOhnePreis.Count > 0 ? (double?)null : energie.Value + kuehlKosten;
            v.Energiekosten = energie;

            // AUFTRAG #267 — KEIN STILLES NULL. Bleibt die Zahl aus, steht ab hier im
            // Klartext, WORAN es liegt und WAS zu tun ist. Die Reihenfolge ist die der
            // Behebung: erst der fehlende Träger (ohne ihn hilft kein Preis), dann der
            // fehlende Preis, zuletzt der Verbrauch ohne Trägerzuordnung.
            if (!v.Energiekosten.HasValue)
            {
                if (verbrauchJeTraeger.Count == 0 && verbrauchOhneTraeger <= 0 && netzbezugBewertet <= 0 &&
                    kuehlZaehlerMWh <= 0)
                    v.EnergiekostenGrund = GRUND_KEIN_VERBRAUCH;
                else if (verbrauchOhneTraeger > 0)
                    v.EnergiekostenGrund = string.Format(GRUND_VERBRAUCH_OHNE_TRAEGER,
                        verbrauchOhneTraeger.ToString("N1", BerichtTexte.Kultur));
                // Kein Träger — oder nur der aus dem Rückfall, und auch der trägt
                // keinen Preis. Beides ist DIESELBE Aufgabe für den Anwender: erst
                // zuordnen, dann bepreisen. Einen Preis „für Elektrische Energie"
                // zu verlangen, den man mangels Zuordnung gar nicht eintragen kann,
                // wäre eine Sackgasse.
                // OHNE NETZBEZUG kein Stromgrund: Ein reines Kesselprojekt ohne
                // Strombezug hat kein Stromloch, sondern ein Preisloch — der Zweig
                // darunter nennt dann den Brennstoff beim Namen.
                else if (netzbezugBewertet > 0 && !stromKosten.HasValue &&
                         (stromCarrierKosten <= 0 || stromAusRueckfall))
                    v.EnergiekostenGrund = GRUND_KEIN_STROMTRAEGER;
                else if (netzbezugBewertet > 0 && !stromKosten.HasValue)
                    v.EnergiekostenGrund = string.Format(GRUND_STROMPREIS_FEHLT,
                        stromPreisTraeger ?? "?");
                else if (ohnePreis.Count > 0)
                    v.EnergiekostenGrund = string.Format(GRUND_BRENNSTOFFPREIS_FEHLT,
                        string.Join(", ", ohnePreis));
                // E34: Der Kühlträger einer Wärmepumpe trägt keinen Arbeitspreis — beim Namen genannt.
                else if (kuehlOhnePreis.Count > 0)
                    v.EnergiekostenGrund = string.Format(GRUND_KUEHLTRAEGER_PREIS_FEHLT,
                        string.Join(", ", kuehlOhnePreis));
                else
                    v.EnergiekostenGrund = GRUND_RECHENFEHLER;
            }

            // KD4/FK6: Leistungsanteil getrennt ausweisen (in Energiekosten enthalten).
            v.EnergieLeistungsanteil = leistungGepflegt ? (double?)leistungsAnteil : null;

            bool hatBrennstoff = verbrauchJeTraeger.Count > 0 || verbrauchOhneTraeger > 0;
            if (!hatBrennstoff)
            {
                v.CO2Gesamt = netzCO2t;                     // reine Strom-Systeme
                v.CO2Brennstoff = 0.0;
            }
            else
            {
                v.CO2Gesamt = co2Vollstaendig ? (double?)(brennstoffCO2t + netzCO2t) : null;
                v.CO2Brennstoff = behgVollstaendig ? (double?)behgCO2t : null;   // nur abgabepflichtige Träger
            }

            double waermeMWh = m.Energiebedarf != null ? m.Energiebedarf.Waermebedarf_Gesamt : 0;
            v.CO2Spezifisch = (v.CO2Gesamt.HasValue && waermeMWh > 0)
                ? (double?)(v.CO2Gesamt.Value * 1000.0 / waermeMWh)    // t/a → g/kWh Wärme
                : null;
        }

        // ------------------------------------------------------------- Träger-Daten

        /// <summary>
        /// ETAPPE B7 — eine Anlagenzeile der Energiekosten-Aufschlüsselung, mit genau
        /// derselben Rechnung wie die Trägersumme: mengenbasiert über den Heizwert,
        /// sonst direkt je kWh. Ohne Träger, ohne Menge oder ohne Arbeitspreis entsteht
        /// KEINE Zeile — die Aufschlüsselung soll erklären, nicht behaupten.
        /// </summary>
        // ------------------------------------------- Der Leistungspreis eines Stromträgers

        /// <summary>
        /// Führt ein Stromträger einen Leistungspreis — zweistufige Staffel (Q11), Saisonreihe (FK6a)
        /// oder Satz (Projektwert vor Katalogwert, 0 = nicht gepflegt)? Dieselbe Frage für den
        /// Projektträger (Bezugsspitze des Anschlusses) und für den Kühlträger eines eigenen Zählers
        /// (E35, seine eigene Spitze).
        /// </summary>
        private static bool LeistungspreisStrom(TraegerInfo t)
        {
            return t != null && (t.Staffel.Gepflegt || t.ReiheJeKW != null || t.PreisLeistung.HasValue);
        }

        /// <summary>
        /// <b>Die Leistungspreisregel eines Stromträgers</b> [€/a] auf eine Spitze im
        /// Viertelstundenraster — EINE Regel für den Netzbezug des Anschlusses (SP-E-1, Q11, FK6a) und
        /// für den eigenen Zähler des Kältestroms (E35): Die Staffel geht vor und bemisst sich an der
        /// Jahresspitze; dann die Saisonreihe (Monatssatz × Monatsspitze); dann der Satz je Monat
        /// (× Summe der Monatsspitzen) oder je Jahr (× Jahresspitze). Nur für einen Träger mit
        /// <see cref="LeistungspreisStrom"/>.
        /// </summary>
        private static double LeistungsanteilStrom(TraegerInfo t, Netzbezugsspitze spitze)
        {
            if (t.Staffel.Gepflegt)
                return t.Staffel.Betrag(spitze.JahrKW);
            if (t.ReiheJeKW != null)
            {
                double anteil = 0;
                for (int mo = 0; mo < 12; mo++)
                    anteil += t.ReiheJeKW[mo] * spitze.MonatKW[mo];
                return anteil;
            }
            if (string.Equals(t.LeistungsModus, DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal))
                return t.PreisLeistung.Value * spitze.MonatssummeKW;
            return t.PreisLeistung.Value * spitze.JahrKW;
        }

        /// <summary>
        /// Die Bemessungsgröße derselben Regel [kW] — für den Ausweis „Spitze × Satz": die
        /// Jahresspitze bei Staffel und Satz je Jahr, die Summe der Monatsspitzen bei Saisonreihe und
        /// Satz je Monat.
        /// </summary>
        private static double LeistungsbasisKW(TraegerInfo t, Netzbezugsspitze spitze)
        {
            if (t.Staffel.Gepflegt) return spitze.JahrKW;
            if (t.ReiheJeKW != null ||
                string.Equals(t.LeistungsModus, DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal))
                return spitze.MonatssummeKW;
            return spitze.JahrKW;
        }

        /// <summary>
        /// Vermerkt einen Stromträger, dessen Leistungspreis mangels Spitze nicht gerechnet werden
        /// konnte (<see cref="VariantenDaten.LeistungspreisOhneSpitze"/>) — je Name einmal, mehrere
        /// durch Komma getrennt (Projektträger und Kühlträger eines eigenen Zählers, E35).
        /// </summary>
        private static void LeistungspreisOhneSpitzeVermerken(VariantenDaten v, int carrierId)
        {
            string name = TraegerName(carrierId);
            if (string.IsNullOrEmpty(name)) return;
            if (string.IsNullOrEmpty(v.LeistungspreisOhneSpitze)) { v.LeistungspreisOhneSpitze = name; return; }
            foreach (string vorhanden in v.LeistungspreisOhneSpitze.Split(new[] { ", " }, StringSplitOptions.None))
                if (string.Equals(vorhanden, name, StringComparison.Ordinal)) return;
            v.LeistungspreisOhneSpitze += ", " + name;
        }

        private static void AnlageZeile(List<EnergieAnlageNachweis> ziel, int idProjekt,
                                        string anlage, int carrierId, double mengeMWh, string szenario)
        {
            if (ziel == null || carrierId <= 0 || mengeMWh <= 0) return;
            TraegerInfo info = LadeTraeger(idProjekt, carrierId, szenario);   // E9a: wie die Summe
            if (info == null || !info.PreisArbeit.HasValue) return;

            bool ueberHeizwert = info.EffHi.HasValue && info.EffHi.Value > 0;
            double menge = ueberHeizwert ? mengeMWh * 1000.0 / info.EffHi.Value
                                         : mengeMWh * 1000.0;
            ziel.Add(new EnergieAnlageNachweis
            {
                Anlage = string.IsNullOrEmpty(anlage) ? "?" : anlage,
                Traeger = TraegerName(carrierId),
                MengeMWh = mengeMWh,
                MengeAbrechnung = menge,
                Einheit = ueberHeizwert ? info.Abrechnungseinheit : "kWh",
                PreisJeEinheit = info.PreisArbeit.Value,
                KostenEur = menge * info.PreisArbeit.Value
            });
        }

        private class TraegerInfo
        {
            public double? PreisArbeit;   // € je Abrechnungseinheit bzw. €/kWh (Direktabrechnung)
            public double? Grundpreis;    // €/a

            /// <summary>Der Faktorsatz des Trägers aus der EINEN Lesekette
            /// (<see cref="EmissionsFaktorLader"/>, Etappe E5): reines CO₂,
            /// CO₂-Äquivalent nach F6 und die Luftschadstoffe.</summary>
            public EmissionsFaktorSatz Faktoren = new EmissionsFaktorSatz();

            /// <summary>Reines CO₂ [g/kWh] — die Größe der BEHG-Abgabemenge; NIE
            /// modusabhängig (Kurzzugriff auf <see cref="Faktoren"/>).</summary>
            public double? CO2 { get { return Faktoren.Co2GKwh; } }

            public double? EffHi;         // kWh je Abrechnungseinheit (null/0 = Direktabrechnung)

            /// <summary>ETAPPE B7 — die Abrechnungseinheit des Traegers (L, kg, m3,
            /// kWh) fuer die Herleitungszeile „Menge x Preis"; leer = nicht gepflegt.</summary>
            public string Abrechnungseinheit = "";
            public bool BehgPflichtig = true;   // fossiler Brennstoff (Phase 7/W2)

            /// <summary>L13 — biogener Träger (Holz, Pellets, Rapsöl, Tierische Fette, Biogas).</summary>
            public bool Biogen;

            /// <summary>L13 — biogener Träger, der zugleich BEHG-Brennstoff ist
            /// (flüssige Biomasse). Nur hier wirkt ein fehlender Nachhaltigkeitsnachweis.</summary>
            public bool BehgBiogen;

            /// <summary>Leistungspreis (Etappe KD4/FK6): Projektwert vor Katalogwert,
            /// 0 zählt wie beim Arbeitspreis als NICHT GEPFLEGT (Befund D5).
            /// Einheit je <see cref="LeistungsModus"/>: €/(kW·a) bzw. €/(kW·Monat).</summary>
            public double? PreisLeistung;

            /// <summary><c>energy_carrier.price_power_modus</c> — JAHR (Vorgabe) oder
            /// MONAT; der Modus ist Katalogsache je Träger (FK6), keine Projektgröße.</summary>
            public string LeistungsModus = DbWerte.LEISTUNGSPREIS_MODUS_JAHR;

            /// <summary>FK6a — Summe der 12 Monatssätze der saisonalen
            /// Leistungspreis-Reihe [€/(kW·a)-äquivalent]; null = keine Reihe
            /// gepflegt. Eine gepflegte Reihe gilt VOR dem konstanten Satz
            /// (§ 7.1); Projektreihe vor Stammreihe löst
            /// <see cref="PreisreiheCtrl.ReadTraegerReihe"/> auf.</summary>
            public double? ReihenSummeJeKW;

            /// <summary>FK6a — die ZWÖLF Monatssätze derselben Reihe [€/(kW·Monat)];
            /// null = keine Reihe gepflegt. Der BRENNSTOFFzweig rechnet mit
            /// <see cref="ReihenSummeJeKW"/>, weil seine Basis über das Jahr konstant
            /// ist (die vorgehaltene Anschlussleistung); der STROMzweig braucht die
            /// Sätze einzeln, weil jeder Monat seine eigene Bezugsspitze hat.</summary>
            public double[] ReiheJeKW;

            /// <summary>
            /// Q11 (Schemaschritt 104) — die zweistufige Leistungspreis-Staffel der
            /// Projektübersteuerung; gelesen nur im STROMzweig. Gepflegt geht sie dem
            /// konstanten Satz und der Saisonreihe vor. Leer, wenn nicht gepflegt — nie
            /// <c>null</c>.
            /// </summary>
            public LeistungspreisStaffel Staffel = new LeistungspreisStaffel();

            /// <summary>ETAPPE E9a (Schritt C): mindestens einer der drei Preise dieses
            /// Trägers kommt aus einem gepflegten Szenariopreis.</summary>
            public bool SzenarioGepflegt;

            /// <summary>ETAPPE E9a (Schritt C): der Leistungspreis kommt aus einem
            /// gepflegten Szenariopreis (er wirkt nur ohne Staffel und Saisonreihe).</summary>
            public bool LeistungSzenarioGepflegt;
        }

        /// <summary>ETAPPE E9a: Ist das ein Szenario mit eigenem Preissatz (BEST oder WORST)?</summary>
        private static bool IstSzenario(string szenario)
        {
            return string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal) ||
                   string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal);
        }

        /// <summary>ETAPPE E9a (E9a‑Q3): vermerkt einen Träger, dessen Szenario-Leistungspreis
        /// neben Staffel oder Saisonreihe ohne Wirkung bleibt — je Name einmal.</summary>
        private static void SzenarioLeistungOhneWirkung(VariantenDaten v, int carrierId)
        {
            string name = TraegerName(carrierId);
            if (v.SzenarioLeistungspreisOhneWirkung == null)
                v.SzenarioLeistungspreisOhneWirkung = new List<string>();
            if (!string.IsNullOrEmpty(name) && !v.SzenarioLeistungspreisOhneWirkung.Contains(name))
                v.SzenarioLeistungspreisOhneWirkung.Add(name);
        }

        /// <summary>
        /// ETAPPE E9a (Schritt C, E9a‑Q3 Lesart a) — die Szenariopreise auf den WIRKSAMEN
        /// Erwartet-Satz legen: nach der ganzen Rückfallkette (Projektwert → Preisstand →
        /// Katalog) ersetzt ein gepflegter Szenariowert den Preis als Ganzes
        /// (<see cref="TraegerpreisSzenario.Wirksam"/>, die EINE Regel). Für ERWARTET und ohne
        /// gepflegten Wert bleibt der Satz, wie er ist.
        /// </summary>
        private static void SzenarioPreiseAnwenden(TraegerInfo info, int idProjekt, int carrierId,
                                                   string szenario)
        {
            if (info == null || !IstSzenario(szenario)) return;
            TraegerpreisSzenario sz = EnergietraegerPreisCtrl.SzenarioLesen(idProjekt, carrierId);
            if (sz.Leer) return;

            bool arbeit, grund, leistung;
            info.PreisArbeit = TraegerpreisSzenario.Wirksam(info.PreisArbeit, sz.Arbeitspreis(szenario), out arbeit);
            info.Grundpreis = TraegerpreisSzenario.Wirksam(info.Grundpreis, sz.Grundpreis(szenario), out grund);
            info.PreisLeistung = TraegerpreisSzenario.Wirksam(info.PreisLeistung, sz.Leistungspreis(szenario), out leistung);
            info.SzenarioGepflegt = arbeit || grund || leistung;
            info.LeistungSzenarioGepflegt = leistung;
        }

        /// <summary>
        /// Vorgehaltene Anschlussleistung eines Trägers [kW] aus den GERÄTEDATEN der
        /// Projektanlagen (Etappe KD4, Konzept Kostendialoge § 7.1): BHKW
        /// (Pel + Ptherm) / η_gesamt, Kessel Ptherm / η. Bewusst Gerätedaten statt
        /// Simulationszeitreihe: Der Gas-Leistungspreis bepreist die VORGEHALTENE
        /// Leistung des Anschlusses, und Ergebnis-Zeitreihen werden hausregelkonform
        /// nicht persistiert. Wirkungsgrade werden nach dem Parser-Muster normiert
        /// (Wert &gt; 1,5 = Prozentangabe ÷ 100); außerhalb (0; 1,5] wird die Anlage
        /// übersprungen — Basis fehlt statt Fantasiewert.
        /// </summary>
        internal static double AnschlussleistungKW(int idProjekt, int carrierId)
        {
            double summe = 0;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT b.Pel AS BhkwPel, b.Ptherm AS BhkwPth, b.Wirkungsgrad AS BhkwEta, " +
                    "h.Ptherm AS KesselPth, h.Wirkungsgrad_Gas AS KesselEtaGas, " +
                    "h.[Wirkungsgrad_Öl] AS KesselEtaOel " +
                    "FROM (Tab_Energieanlagen AS e LEFT JOIN Tab_BHKW AS b ON e.ID_BHKW = b.ID) " +
                    "LEFT JOIN Tab_Heizkessel AS h ON e.ID_Kessel = h.ID " +
                    "WHERE e.ID_Projekt = ? AND e.ID_Carrier = ?",
                    new DbParam("@p", idProjekt), new DbParam("@c", carrierId));
                if (dt == null) return 0;

                foreach (DataRow r in dt.Rows)
                {
                    double? bhkwPel = W(r, "BhkwPel"), bhkwPth = W(r, "BhkwPth");
                    if (bhkwPel.HasValue || bhkwPth.HasValue)
                    {
                        double eta = EtaNormiert(W(r, "BhkwEta"));
                        if (eta > 0)
                            summe += ((bhkwPel ?? 0) + (bhkwPth ?? 0)) / eta;
                        continue;
                    }

                    double? kesselPth = W(r, "KesselPth");
                    if (kesselPth.HasValue && kesselPth.Value > 0)
                    {
                        double etaGas = EtaNormiert(W(r, "KesselEtaGas"));
                        double etaOel = EtaNormiert(W(r, "KesselEtaOel"));
                        double eta = etaGas > 0 ? etaGas : etaOel;
                        if (eta > 0) summe += kesselPth.Value / eta;
                    }
                }
            }
            catch { }
            return summe;
        }

        /// <summary>Wirkungsgrad-Normierung (Parser-Muster): &gt; 1,5 gilt als
        /// Prozentangabe; außerhalb (0; 1,5] bleibt 0 („Basis fehlt").</summary>
        private static double EtaNormiert(double? eta)
        {
            if (!eta.HasValue || eta.Value <= 0) return 0;
            double e = eta.Value;
            if (e > 1.5) e /= 100.0;
            return (e > 0 && e <= 1.5) ? e : 0;
        }

        /// <summary>
        /// Der ARBEITSPREIS aus der PREISHISTORIE des Projekts
        /// (<c>energy_price.arbeitspreis</c>, je Abrechnungseinheit) — <c>null</c>, wenn
        /// dort nichts Gepflegtes steht (Auftrag #267).
        ///
        /// <para><b>Die Stichtagsregel ist die des Kerns</b>
        /// (<see cref="StromPreisCtrl.Stichtag"/>, Fachkonzept 4.1): die jüngste Version
        /// mit <c>valid_from ≤ Stichtag</c>, sonst die ÄLTESTE überhaupt — besser ein
        /// späterer Preis als gar keiner. Auf <c>valid_to</c> wird nicht gefiltert; die
        /// Spalte ist im ganzen Bestand NULL (Begründung bei
        /// <see cref="StromPreisCtrl"/>).</para>
        ///
        /// <para><b>0 zählt wie überall als NICHT GEPFLEGT</b> (Befund D5): Die Zeilen,
        /// die <c>WizardCtrl.TraegerSatzAnlegen</c> bei der Zuordnung schreibt, tragen
        /// den Stammwert — und der ist im Bestand durchweg 0.</para>
        /// </summary>
        private static double? Historienpreis(int idProjekt, int carrierId)
        {
            if (idProjekt <= 0 || carrierId <= 0) return null;
            try
            {
                DateTime stichtag = StromPreisCtrl.Stichtag(null, 0);
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT arbeitspreis FROM energy_price " +
                    "WHERE carrier_id = ? AND id_projekt = ? AND valid_from <= ? " +
                    "ORDER BY valid_from DESC LIMIT 1",
                    new DbParam("@c", DbParamTyp.Integer) { Wert = carrierId },
                    new DbParam("@p", DbParamTyp.Integer) { Wert = idProjekt },
                    new DbParam("@d", DbParamTyp.Date) { Wert = stichtag });

                if (dt == null || dt.Rows.Count == 0)
                    dt = DataRepository.GetDataTable(
                        "SELECT arbeitspreis FROM energy_price " +
                        "WHERE carrier_id = ? AND id_projekt = ? ORDER BY valid_from ASC LIMIT 1",
                        new DbParam("@c", DbParamTyp.Integer) { Wert = carrierId },
                        new DbParam("@p", DbParamTyp.Integer) { Wert = idProjekt });

                if (dt == null || dt.Rows.Count == 0) return null;
                double? preis = W(dt.Rows[0], "arbeitspreis");
                return (preis.HasValue && preis.Value > 0) ? preis : null;
            }
            catch { return null; }
        }

        /// <summary>Der Anzeigename eines Trägers — <see cref="Emissionsquelle.TraegerName"/>,
        /// nicht eine zweite Abfrage (Auftrag #267).</summary>
        private static string TraegerName(int carrierId)
        {
            return Emissionsquelle.TraegerName(carrierId);
        }

        /// <summary>
        /// Die Bezeichner der ELEKTROKESSEL eines Projekts (<c>Tab_Heizkessel.Brennstoff</c>
        /// = <see cref="SimulationSPK.BRENNSTOFF_STROM"/>); leer, wenn es keinen gibt.
        ///
        /// <para><b>Wofür.</b> Die Warnung <c>WIRT_KESSELBRENNSTOFF_FEHLT</c> meint einen
        /// Kessel, dessen BRENNSTOFF im Ergebnis fehlt. Ein Elektrokessel hat keinen: Sein
        /// Einsatz steht über den Reststrombedarf im Netzbezug, und seine Modulzeile führt
        /// deshalb bewusst keinen Verbrauch (Befund B-1). Ohne diese Liste stünde die
        /// Warnung bei jedem solchen Projekt dauerhaft.</para>
        ///
        /// <para><b>Warum die ANLAGENzeile und nicht die Ergebniszeile gefragt wird.</b>
        /// Der Brennstoff des Kessels ist die einzige Angabe, die den Fall sicher
        /// beantwortet — auch für eine gespeicherte Modulzeile ohne zugeordneten
        /// Energieträger, und auch für Läufe von vor B-1. Der <c>carrier_id</c> der
        /// Modulzeile trägt die Auskunft nur, wenn dem Kessel überhaupt ein Träger
        /// zugeordnet ist; im Bestand ist er oft leer.</para>
        ///
        /// <para>Verbunden wird über den Bezeichner — dieselbe Spalte, aus der
        /// <c>SimulationControl.SPK_Liste_Laden</c> die Modulnamen zieht.</para>
        /// </summary>
        private static HashSet<string> StromKesselNamen(int idProjekt)
        {
            var namen = new HashSet<string>(StringComparer.Ordinal);
            if (idProjekt <= 0) return namen;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT Bezeichner FROM Tab_Heizkessel WHERE ID_Projekt = ? AND Brennstoff = ?",
                    new DbParam("@p", idProjekt),
                    new DbParam("@b", SimulationSPK.BRENNSTOFF_STROM));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                    {
                        string s = r["Bezeichner"] == DBNull.Value
                                 ? "" : Convert.ToString(r["Bezeichner"]);
                        if (!string.IsNullOrEmpty(s)) namen.Add(s.Trim());
                    }
            }
            catch { }
            return namen;
        }

        /// <summary>
        /// Der AUSLIEFERUNGS-Stromträger eines Projekts, wenn ihm keiner zugeordnet ist —
        /// <see cref="ProjektEnergietraegerCtrl.StandardStromTraeger"/>, also genau die
        /// Fassung, die auch die Kostenseite anzeigt und der Assistent zuordnet
        /// (Auftrag #267). 0 = das Projekt führt keine elektrische Erzeugung, oder der
        /// Katalog führt keinen Stromträger.
        ///
        /// <para><b>Nur für Projekte mit elektrischer Welt.</b> Ein reines Kesselprojekt
        /// ohne Wärmepumpe, PV, Stromspeicher oder Heizstab bekommt keinen Rückfall:
        /// Sein Netzbezug ist Haushaltsstrom der Bedarfsseite und keine Anlagengröße,
        /// und ein Träger, den niemand zugeordnet hat, wäre dort eine Erfindung.</para>
        ///
        /// <para>Der Rumpf steht seit dem Anwenderentscheid vom 15.09.2026 in
        /// <see cref="Emissionsquelle.KatalogStromTraeger"/> — die CO₂-Seite braucht
        /// dieselbe Wahl, und zwei Fassungen wären zwei Antworten.</para>
        /// </summary>
        private static int StandardStromTraeger(int idProjekt)
        {
            return Emissionsquelle.KatalogStromTraeger(idProjekt);
        }

        /// <summary>
        /// <b>Trägt der bepreisende Stromträger dieses Projekts einen Leistungspreis?</b>
        /// (zweistufige Staffel, konstanter Satz oder Saisonreihe, Projektwert vor
        /// Katalogwert, 0 zählt wie überall als nicht gepflegt).
        ///
        /// <para><b>Wozu die Schale das braucht.</b> Der Leistungsanteil des Stroms
        /// bemisst sich an der Bezugsspitze, und die gibt es nur aus einem frischen
        /// Lauf mit eingesammelten Zeitreihen. Wer die Wirtschaftlichkeit rechnet, muss
        /// also VORHER wissen, ob dieser Lauf gebraucht wird — dieselbe Frage, die
        /// heute schon der Rollentarif und der KWKG-Bonus stellen.</para>
        /// </summary>
        public static bool StromLeistungspreisGepflegt(int idProjekt)
        {
            try
            {
                int traeger = Emissionsquelle.StromTraeger(idProjekt);
                if (traeger <= 0) traeger = StandardStromTraeger(idProjekt);
                if (traeger > 0 && LeistungspreisMitSzenario(idProjekt, traeger)) return true;

                // ENTSCHEID E35 (Konzept Gebäudesimulation N1.40): Der Kühlträger eines eigenen
                // Zählers bepreist die eigene Spitze des Kältestroms — auch sie gibt es nur aus
                // dem frischen Lauf. Gefragt wird an der Anlagenkonfiguration, vor jedem Lauf.
                foreach (int kuehltraeger in KuehltraegerEigenerZaehler(idProjekt, traeger))
                    if (LeistungspreisMitSzenario(idProjekt, kuehltraeger)) return true;
                return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// Trägt der Träger einen Leistungspreis — gepflegt (Staffel, Saisonreihe, Satz) oder allein
        /// in einem Szenario (ETAPPE E9a, Schritt C: sonst fehlte er im Szenariolauf still)?
        /// </summary>
        private static bool LeistungspreisMitSzenario(int idProjekt, int traeger)
        {
            TraegerInfo info = LadeTraeger(idProjekt, traeger);
            // Q11: Auch die zweistufige Staffel bemisst sich an der Bezugsspitze.
            if (LeistungspreisStrom(info)) return true;
            TraegerpreisSzenario sz = EnergietraegerPreisCtrl.SzenarioLesen(idProjekt, traeger);
            return (sz.LeistungspreisBest ?? 0) > 0 || (sz.LeistungspreisWorst ?? 0) > 0;
        }

        /// <summary>
        /// Die Kühlträger der Anlagen mit eigenem Zähler (E34, E35) — nur Wärmepumpen im Kühlbetrieb
        /// und nur ein vom Projektträger abweichender Träger (sonst wirkt die Abrechnungsart nicht).
        /// </summary>
        private static List<int> KuehltraegerEigenerZaehler(int idProjekt, int projekttraeger)
        {
            var liste = new List<int>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT DISTINCT ea.Kuehl_ID_Carrier AS traeger FROM Tab_Energieanlagen AS ea " +
                "INNER JOIN Tab_WP AS wp ON wp.ID = ea.ID_WP " +
                "WHERE ea.ID_Projekt = ? AND ea.Kuehl_EigenerZaehler = 1 AND wp.Kuehlbetrieb = 1 " +
                "AND ea.Kuehl_ID_Carrier IS NOT NULL ORDER BY ea.Kuehl_ID_Carrier",
                new DbParam("@p", idProjekt));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                if (r["traeger"] == DBNull.Value) continue;
                int k = Convert.ToInt32(r["traeger"], System.Globalization.CultureInfo.InvariantCulture);
                if (Kaeltestromabrechnung.Abweichend(k, projekttraeger)) liste.Add(k);
            }
            return liste;
        }

        /// <summary>
        /// Dieselbe Frage für eine ganze VERGLEICHSGRUPPE (Entscheid LS-E-2, Auftrag VF-1):
        /// Führt der Stamm <b>oder eine ihrer Versionen</b> einen Strom-Leistungspreis?
        ///
        /// <para><b>Warum nicht der Stamm allein.</b> Der Leistungspreis ist eine
        /// Projektübersteuerung; eine Variante kann ihn führen, ohne dass der Stamm es tut.
        /// Entschied allein der Stamm, rechnete die Wirtschaftlichkeit die Gruppe ohne
        /// Zeitreihen — und der Variante fiel der Leistungsanteil ihrer Energiekosten
        /// stillschweigend weg, weil ihre Bezugsspitze nie eingesammelt wurde.</para>
        ///
        /// <para>Die Frage wird je Projekt gestellt und beim ersten Treffer beendet; der
        /// Stamm kommt zuerst, weil er der häufigste Träger ist.</para>
        /// </summary>
        /// <param name="idStamm">Das Stammprojekt der Gruppe.</param>
        /// <param name="versionen">
        /// Die übrigen Versionen der Gruppe (der Stamm darf darin stehen); <c>null</c> =
        /// nur der Stamm.
        /// </param>
        public static bool StromLeistungspreisGepflegt(int idStamm, IEnumerable<int> versionen)
        {
            if (StromLeistungspreisGepflegt(idStamm)) return true;
            if (versionen == null) return false;
            foreach (int id in versionen)
                if (id > 0 && id != idStamm && StromLeistungspreisGepflegt(id)) return true;
            return false;
        }

        private static TraegerInfo LadeTraeger(int idProjekt, int carrierId)
        {
            return LadeTraeger(idProjekt, carrierId, null);
        }

        /// <summary>
        /// ETAPPE E9a: derselbe Träger mit den Preisen eines SZENARIOS — die Rückfallkette
        /// darunter ist unverändert; erst ihr Ergebnis bekommt die gepflegten Szenariopreise
        /// (<see cref="SzenarioPreiseAnwenden"/>). <paramref name="szenario"/> = <c>null</c> oder
        /// ERWARTET ist der Weg von vor E9a.
        /// </summary>
        private static TraegerInfo LadeTraeger(int idProjekt, int carrierId, string szenario)
        {
            var info = new TraegerInfo();
            try
            {
                DataTable eff = DataRepository.GetDataTable(
                    "SELECT eff_hi, billing_unit FROM Abfrage_Energietraeger_Effektiv " +
                    "WHERE ID_Projekt = ? AND carrier_id = ?",
                    new DbParam("@p", idProjekt), new DbParam("@c", carrierId));
                if (eff != null && eff.Rows.Count > 0)
                {
                    if (eff.Rows[0]["eff_hi"] != DBNull.Value)
                        info.EffHi = Convert.ToDouble(eff.Rows[0]["eff_hi"]);
                    if (eff.Rows[0]["billing_unit"] != DBNull.Value)
                        info.Abrechnungseinheit = eff.Rows[0]["billing_unit"].ToString();
                }
            }
            catch { }

            // Emissionsfaktoren: EINE Kette für beide Rechner (Etappe E5).
            info.Faktoren = EmissionsFaktorLader.Lade(idProjekt, carrierId);

            double? sPreis = null, sGrund = null, sLeistung = null;
            try
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_work, custom_price_base, custom_price_power " +
                    "FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", idProjekt), new DbParam("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    sPreis = W(s.Rows[0], "custom_price_work");
                    sGrund = W(s.Rows[0], "custom_price_base");
                    sLeistung = W(s.Rows[0], "custom_price_power");
                }
            }
            catch { }

            double? kPreis = null, kGrund = null, kLeistung = null;
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_work, price_base, price_power, price_power_modus " +
                    "FROM energy_carrier WHERE id = ?",
                    new DbParam("@c", carrierId));
                if (k != null && k.Rows.Count > 0)
                {
                    kPreis = W(k.Rows[0], "price_work");
                    kGrund = W(k.Rows[0], "price_base");
                    kLeistung = W(k.Rows[0], "price_power");

                    object modus = k.Rows[0]["price_power_modus"];
                    if (modus != null && modus != DBNull.Value &&
                        string.Equals(Convert.ToString(modus),
                            DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal))
                        info.LeistungsModus = DbWerte.LEISTUNGSPREIS_MODUS_MONAT;
                }
            }
            catch { }

            // Leistungspreis: Projektwert vor Katalogwert, 0 = nicht gepflegt
            // (dieselbe Regel wie beim Arbeitspreis, Befund D5).
            if (sLeistung.HasValue && sLeistung.Value > 0) info.PreisLeistung = sLeistung;
            else if (kLeistung.HasValue && kLeistung.Value > 0) info.PreisLeistung = kLeistung;

            // Q11 (Schemaschritt 104): die zweistufige Leistungspreis-Staffel der
            // Projektübersteuerung — eigene Abfrage über den Controller der Trägerkarte,
            // damit eine Datenbank ohne die Spalten die Preise oben nicht verliert.
            info.Staffel = EnergietraegerPreisCtrl.StaffelLesen(idProjekt, carrierId);

            // FK6a: saisonale Leistungspreis-Reihe (12 Monatssätze). Sie gilt vor dem
            // konstanten Satz; die Ebenen (Projekt vor Stamm) löst der Controller auf.
            try
            {
                PreisreiheCtrl prc = new PreisreiheCtrl();
                PreisreiheModel reihe = prc.ReadTraegerReihe(idProjekt, carrierId);
                if (reihe != null && string.Equals(reihe.Einheit,
                        DbWerte.PREISREIHE_EINHEIT_EUR_KW_MONAT, StringComparison.Ordinal))
                {
                    double[] werte = prc.ReadWerte(reihe.ID);
                    if (werte != null && werte.Length > 0)
                    {
                        double summe = 0;
                        foreach (double wert in werte) summe += wert;
                        if (summe > 0)
                        {
                            info.ReihenSummeJeKW = summe;

                            // Dieselbe Reihe, monatsscharf — für den Stromzweig, der
                            // jeden Satz mit SEINER Monatsspitze multipliziert. Eine
                            // kürzere Reihe wird auf zwölf aufgefüllt (fehlende Monate
                            // = 0), eine längere abgeschnitten; die Summe oben bleibt
                            // unangetastet.
                            var jeMonat = new double[12];
                            for (int mo = 0; mo < 12 && mo < werte.Length; mo++)
                                jeMonat[mo] = werte[mo];
                            info.ReiheJeKW = jeMonat;
                        }
                    }
                }
            }
            catch { }

            // BEHG-Einstufung und Biogen-Kennzeichen aus dem Brennstoff-Katalog
            // (Tab_Brennstoff_Stamm über energy_carrier.id_brennstoff). Der
            // EMISSIONSFAKTOR kommt seit Etappe E5 nicht mehr von hier, sondern aus
            // der einen Lesekette (EmissionsFaktorLader) - diese Abfrage klärt nur
            // noch die EINSTUFUNG des Trägers.
            try
            {
                DataTable b = DataRepository.GetDataTable(
                    "SELECT bs.ID_Kategorie, bs.Bezeichner FROM energy_carrier AS ec " +
                    "INNER JOIN Tab_Brennstoff_Stamm AS bs ON ec.id_brennstoff = bs.ID " +
                    "WHERE ec.id = ?",
                    new DbParam("@c", carrierId));
                if (b != null && b.Rows.Count > 0)
                {
                    // BEHG-pflichtig: Kategorien 1 Gas / 2 Öl / 3 Koks / 4 Kohle /
                    // 11 Sonstige (Tab_BrennstoffKategorien); Biogas ausgenommen.
                    // Holz/Pellets/Rapsöl/Tier. Fette/Strom/Fernwärme/Wasserstoff frei.
                    double? kat = W(b.Rows[0], "ID_Kategorie");
                    string bez = b.Rows[0]["Bezeichner"] != DBNull.Value
                                 ? b.Rows[0]["Bezeichner"].ToString() : "";
                    if (kat.HasValue)
                    {
                        int k2 = (int)kat.Value;
                        info.BehgPflichtig = (k2 == 1 || k2 == 2 || k2 == 3 || k2 == 4 || k2 == 11)
                                             && !bez.Trim().Equals("Biogas", StringComparison.OrdinalIgnoreCase);

                        // L13 — dieselbe Kategorieregel, EINE Stelle für beide Rechner
                        // (BilanzKonvention). Die Einstufung ist reine Auskunft; sie
                        // ändert an dieser Rechnung nichts.
                        info.Biogen = BilanzKonvention.IstBiogen(k2, bez);
                        info.BehgBiogen = info.Biogen && BilanzKonvention.IstBehgBiogen(k2);
                    }
                }
            }
            catch { }

            // Arbeitspreis: 0 zählt als NICHT GEPFLEGT (Befund D5, 18.08.2026).
            //
            // Ein Arbeitspreis von 0 kam bisher als gültiger Preis durch: Die Spalten
            // custom_price_work / price_work sind numerisch und selten NULL, W() liefert
            // deshalb 0.0 statt null. Folge: kostenVollstaendig blieb true, die
            // Energiekosten wurden zu 0,00 €/a und die Wirtschaftlichkeitsrechnung
            // speicherte einen Kapitalwert OHNE Fehlgrund — nachgewiesen an Projekt 1018
            // („Erdgas E", beide Preisspalten 0): Kapitalwert −80.464,51 € auf einer
            // Datenbasis ohne jeden Energiepreis, während Projekt 1024 korrekt
            // „Energiekosten nicht bestimmbar" meldete.
            //
            // Abgrenzung: Die Regel gilt nur für den ARBEITSPREIS und nur für Träger, die
            // ein verbrauchendes Modul überhaupt anfährt. Ein legitim kostenloser Träger
            // existiert in diesem Datenmodell nicht — energy_carrier führt ausschließlich
            // beschaffte Energie (pricing_model ANIMAL_FAT, ELECTRICITY, GASEOUS_FUEL,
            // HEAT, LIQUID_FUEL, SOLID_FUEL), jeweils mit Abrechnungseinheit und
            // Heizwert. Umweltwärme der Wärmepumpe und PV-Eigenstrom sind KEINE
            // Energieträger: In verbrauchJeTraeger landen nur BHKW- und Heizkesselmodule,
            // der Strombezug läuft separat über den Netzbezugspfad. Ein Arbeitspreis 0 ist
            // hier also immer „noch nicht erfasst", nie „kostenlos".
            //
            // Der GRUNDPREIS bleibt bewusst unangetastet: 0 €/a ist dort ein üblicher und
            // gültiger Vertragswert.
            //
            // Vorrangkette: Projektwert → PREISHISTORIE zum Stichtag → Katalogwert → null.
            //
            // AUFTRAG #267 — DIE HISTORIE FEHLTE HIER (Anwenderbefund 14.09.2026).
            // Die Trägerkarte schreibt jeden Preisstand nach energy_price
            // (EnergietraegerPreisCtrl.HistorieSchreiben), und der Strompreis-Weg der
            // Speicherrechnung LIEST ihn seit AP4 auch von dort
            // (StromPreisCtrl.ArbeitspreisCtKwh). Diese Kette kannte nur
            // energy_project_settings und den Katalog: Ein Projekt, dessen Preis
            // ausschließlich als Preisstand gepflegt ist, galt hier als „kein Preis" —
            // die Energiekosten blieben „—", während dieselbe Datenbank an anderer
            // Stelle mit dem Preis rechnete. Jetzt ist es EINE Wahrheit.
            //
            // ERGEBNISNEUTRAL, WO SCHON EIN PREIS STAND: Die Historie wird nur
            // befragt, wenn Stufe 1 nichts liefert, und sie kommt VOR dem Katalog —
            // der Projektstand ist die speziellere Angabe.
            info.PreisArbeit = (sPreis.HasValue && sPreis.Value > 0) ? sPreis
                             : (Historienpreis(idProjekt, carrierId)
                                ?? ((kPreis.HasValue && kPreis.Value > 0) ? kPreis : null));
            info.Grundpreis = sGrund ?? kGrund;

            // ETAPPE E9a (Schritt C): die EINE Stelle, an der die Energiekosten einen
            // Szenariopreis lesen — nach der vollständigen Rückfallkette.
            SzenarioPreiseAnwenden(info, idProjekt, carrierId, szenario);
            return info;
        }

        // ================================================================= ETAPPE H2
        // Zwei schmale Zugänge für den Endenergie-Auflöser der Betriebskosten
        // (EndenergieAufloeser, Konzept_BHKW_Wirtschaftlichkeit § 4.5). Sie nutzen
        // DIESELBEN Bausteine wie die Kostenschleife oben (LadeTraeger,
        // Emissionsquelle.StromTraeger) — der Arbeitspreis bleibt damit EINE Wahrheit. Die in E3
        // gegen die Referenz gestellte Schleife selbst bleibt unangetastet
        // (Rechenweg-Disziplin); ihre Kostenformel „Verbrauch × 1000 / eff_hi ×
        // Preis" ist mit „Verbrauch × 1000 × ArbeitspreisJeKwh" algebraisch gleich.

        /// <summary>
        /// Arbeitspreis eines Trägers in €/kWh — bei Direktabrechnung der gepflegte
        /// Satz, sonst über den effektiven Heizwert (kWh je Abrechnungseinheit)
        /// umgerechnet; null = kein Preis gepflegt. Grund- und Leistungspreis gehören
        /// ausdrücklich NICHT dazu: Die anlagenscharfe Endenergie bemisst sich am
        /// Arbeitsanteil („Verbrauch des Moduls × Trägerpreis", Konzept § 4.5) —
        /// trägerweite Fixbeträge lassen sich keiner Anlage zurechnen.
        /// </summary>
        internal static double? ArbeitspreisJeKwh(int idProjekt, int carrierId)
        {
            return ArbeitspreisJeKwh(idProjekt, carrierId, null);
        }

        /// <summary>ETAPPE E9a: derselbe Arbeitspreis [€/kWh] im Szenario — mit dem
        /// wirksamen Szenariopreis des Trägers (der Endenergie-Auflöser der Betriebskosten
        /// liest ihn im Szenariolauf, die „E7c-Wege" B‑4).</summary>
        internal static double? ArbeitspreisJeKwh(int idProjekt, int carrierId, string szenario)
        {
            if (carrierId <= 0) return null;
            TraegerInfo info = LadeTraeger(idProjekt, carrierId, szenario);
            if (!info.PreisArbeit.HasValue) return null;
            return (info.EffHi.HasValue && info.EffHi.Value > 0)
                ? info.PreisArbeit.Value / info.EffHi.Value
                : info.PreisArbeit.Value;
        }

        /// <summary>
        /// Arbeits- und Leistungspreis EINES Trägers, wie dieser Rechner sie sieht:
        /// je ABRECHNUNGSEINHEIT (der Arbeitspreis also NICHT auf kWh umgerechnet),
        /// <c>null</c> = nicht gepflegt.
        ///
        /// <para><b>Nur ein zweiter Leser, kein zweiter Rechenweg.</b> Die
        /// Vorrangkette — Projektwert → Preisstand → Katalogwert, jeweils „0 zählt als
        /// nicht gepflegt" — steht genau einmal, in <see cref="LadeTraeger"/>. Der
        /// Übernahmeweg der Trägerkarte
        /// (<see cref="EnergietraegerRueckfall"/>) fragt hier, statt sich dieselbe
        /// Kette ein zweites Mal zu schreiben: Was die Karte als Lücke zeigt, muss
        /// dasselbe sein, was die Wirtschaftlichkeit als Lücke meldet.</para>
        /// </summary>
        internal static void PreiseDesTraegers(int idProjekt, int carrierId,
                                               out double? arbeitspreis,
                                               out double? leistungspreis)
        {
            arbeitspreis = null;
            leistungspreis = null;
            if (carrierId <= 0) return;

            TraegerInfo info = LadeTraeger(idProjekt, carrierId);
            arbeitspreis = info.PreisArbeit;
            leistungspreis = info.PreisLeistung;
        }

        /// <summary>
        /// ETAPPE E9a — die drei WIRKSAMEN Preise eines Trägers in einem Szenario (Arbeit je
        /// Abrechnungseinheit, Grund in €/a, Leistung je Modus), genau so, wie die
        /// Energiekosten sie ansetzen: Rückfallkette, dann die Szenariopreise. Nur ein zweiter
        /// Leser (Parameterblock des Berichts), kein zweiter Rechenweg.
        /// </summary>
        internal static void PreisSatz(int idProjekt, int carrierId, string szenario,
                                       out double? arbeit, out double? grund, out double? leistung)
        {
            arbeit = null; grund = null; leistung = null;
            if (carrierId <= 0) return;
            TraegerInfo info = LadeTraeger(idProjekt, carrierId, szenario);
            arbeit = info.PreisArbeit;
            grund = info.Grundpreis;
            leistung = info.PreisLeistung;
        }

        /// <summary><c>energy_carrier.id</c> des Stromträgers des Projekts
        /// (<c>pricing_model = 'ELECTRICITY'</c>); 0 = keiner gepflegt.</summary>
        internal static int StromTraegerId(int idProjekt)
        {
            // W14a-E-8-B1: Die Abfrage stand hier als private FindeStromTraeger und ist
            // in Emissionsquelle gezogen - die Autarkie-Kachel braucht denselben Träger.
            return Emissionsquelle.StromTraeger(idProjekt);
        }

        private static double? W(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[col]); } catch { return null; }
        }
    }
}
