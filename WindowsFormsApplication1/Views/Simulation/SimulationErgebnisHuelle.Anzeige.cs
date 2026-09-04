using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using SkiaSharp;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die ANZEIGESEITE der Ergebnishülle (iU9-W11b.13): Was aus einem gerechneten
    /// <c>SimulationControl</c> auf den zehn Blättern steht, und welches Bild dazu
    /// gehört.
    ///
    /// <para>Getrennt von <c>SimulationErgebnisHuelle.cs</c>, weil dort der ABLAUF
    /// steht — Öffnen, Lauf, Schreibwege — und hier die ABBILDUNG. Beide Hälften
    /// zusammen ersetzen 11 031 Zeilen aus sechs Masken.</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        // =================================================================
        // Bedarf und Übersicht
        // =================================================================

        private BedarfDaten BedarfDaten(SimulationErgebnisCtrl.BedarfErgebnis b)
        {
            var kanalDa = new bool[Kanal.ANZAHL];
            for (int k = 0; k < Kanal.ANZAHL; k++)
                kanalDa[k] = k < b.KanalMwh.Count && b.KanalMwh[k] > 0;

            return new BedarfDaten
            {
                WaermelastMaxKw = b.WaermelastMaxKw,
                WaermebedarfGesamtMwh = b.WaermebedarfGesamtMwh,
                StrombedarfMaxKw = b.StrombedarfMaxKw,
                StrombedarfGesamtMwh = b.StrombedarfGesamtMwh,
                KanalMwh = b.KanalMwh,
                Kanalnamen = KANALNAMEN,
                KanalDa = kanalDa
            };
        }

        private static string[] KANALNAMEN => new[]
        {
            MyResource.Resource.KANAL_HEIZUNG_ANZEIGE,
            MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE,
            MyResource.Resource.KANAL_PROZESS_ANZEIGE
        };

        /// <summary>
        /// Präsenz, Ringmittelwerte und das Eigenanteilsraster — das, was
        /// <c>NavigatorUebersicht</c> in 148 Zeilen GDI zeichnete.
        /// </summary>
        private UebersichtDaten UebersichtDaten(SimulationErgebnisCtrl.UebersichtKennzahlen k,
                                                ErgebnisPraesenz p)
        {
            double wbGesamt = _waermebedarf.Waermebedarf_Gesamt;
            double sbGesamt = StrombedarfGesamt();

            var d = new UebersichtDaten
            {
                Waermepumpe = p.Waermepumpe,
                Heizstab = p.Heizstab,
                Heizkessel = p.Heizkessel,
                Solarthermie = p.Solarthermie,
                Bhkw = p.BHKW,
                Photovoltaik = p.Photovoltaik,
                Stromspeicher = p.Stromspeicher,

                WaermebedarfVorhanden = wbGesamt > 0,
                StrombedarfVorhanden = sbGesamt > 0,

                // Befund W11-B36: Ohne Bedarf setzte der Vorläufer den Mittelwert HART
                // auf 100 — hier bleibt er 0, und die Seite zeigt statt des Rings den
                // Satz, dass sich ohne Bedarf keine Deckung ausweisen lässt.
                WaermedeckungProzent = wbGesamt > 0 ? k.WaermeGesamtMwh * 100.0 / wbGesamt : 0.0,
                StromdeckungProzent = sbGesamt > 0 ? StromgedecktMwh() * 100.0 / sbGesamt : 0.0,

                ReststromMwh = k.ReststromMwh,
                RestwaermeMwh = k.RestwaermebedarfMwh,

                EigenanteilSpalten = EigenanteilSpalten(),
                Eigenanteil = Eigenanteil(k, p)
            };

            return d;
        }

        /// <summary>
        /// Der Nenner des Strom-Rings — wörtlich aus <c>NavigatorUebersicht</c>
        /// :355-359: der Projektstrombedarf PLUS die Eigenverbräuche der Wärmeerzeuger.
        /// </summary>
        private double StrombedarfGesamt()
        {
            return _strombedarf.Strombedarf_gesamt
                   + sim.simulation_wp.WP_Strombedarf_gesamt / 1000.0
                   + sim.simulation_wp.Heizstab_gesamt / 1000.0
                   + sim.simulation_spk.Stromverbrauch_Spk;
        }

        /// <summary>Die gedeckte Strommenge: PV, BHKW und die Speicherentladung.</summary>
        private double StromgedecktMwh()
        {
            double gedeckt = sim.simulation_pv.Stromproduktion_gesamt / 1000.0
                             + sim.simulation_bhkw.Stromproduktion_BHKW_MWh;

            if (sim.Speicherergebnis != null)
                gedeckt += sim.Speicherergebnis.EntladeenergieKwh / 1000.0;

            return gedeckt;
        }

        private static string[] EigenanteilSpalten()
        {
            var spalten = new List<string>
            {
                MyResource.Resource.SIM_SPALTE_ENERGIE_ERZEUGER,
                MyResource.Resource.SIM_SPALTE_ERGEBNIS_MWH
            };
            foreach (string kanal in KANALNAMEN)
                spalten.Add(string.Format(MyResource.Resource.SIM_SPALTE_DECKUNG_KANAL, kanal));
            return spalten.ToArray();
        }

        /// <summary>
        /// Der Eigenanteil je Erzeuger und Bedarfskanal — wörtlich aus
        /// <c>NavigatorUebersicht.FillTableWithData</c> :122-150, samt der eigenen Zeile
        /// für den Heizstab (Begründung dort :132-136: In der Ergebnispersistenz gehört
        /// er zur Wärmepumpe, auf dem Bildschirm bekommt er seine eigene Zeile — die
        /// Summe der beiden ist der gespeicherte WP-Eigenanteil).
        /// </summary>
        private List<Rasterzeile> Eigenanteil(SimulationErgebnisCtrl.UebersichtKennzahlen k,
                                              ErgebnisPraesenz p)
        {
            var zeilen = new List<Rasterzeile>();

            if (p.Waermepumpe)
                zeilen.Add(Eigenanteilzeile(MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE,
                    k.WaermeWpMwh,
                    SimulationRunner.Summiere(sim.simulation_wp.Direktdeckung_Kanal,
                                              sim.simulation_wp.Speicherentladung_Kanal)));

            if (p.Heizstab)
                zeilen.Add(Eigenanteilzeile(MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                    k.WaermeHeizstabMwh,
                    SimulationRunner.Summiere(sim.simulation_wp.Heizstab_Kanal)));

            if (p.Solarthermie)
                zeilen.Add(Eigenanteilzeile(MyResource.Resource.SIM_SOLARTHERMIE_ANLAGE,
                    k.WaermeSolarMwh,
                    SimulationRunner.Summiere(sim.simulation_solarthermie.Direktdeckung_Kanal,
                                              sim.simulation_solarthermie.Speicherentladung_Kanal)));

            if (p.Heizkessel)
                zeilen.Add(Eigenanteilzeile(MyResource.Resource.SIM_TABELLE_HEIZKESSEL,
                    k.WaermeKesselMwh,
                    SimulationRunner.Summiere(sim.simulation_spk.Direktdeckung_Kanal,
                                              sim.simulation_spk.Speicherentladung_Kanal)));

            if (p.BHKW)
                zeilen.Add(Eigenanteilzeile(MyResource.Resource.SIM_ERZEUGERNAME_BHKW,
                    k.WaermeBhkwMwh,
                    SimulationRunner.Summiere(sim.simulation_bhkw.Direktdeckung_Kanal,
                                              sim.simulation_bhkw.Speicherentladung_Kanal)));

            return zeilen;
        }

        /// <summary>
        /// Eine Ergebniszeile: Erzeuger, Produktion [MWh/a] und der Eigenanteil je Kanal
        /// (Paket E1). Der Kanalvektor kommt in kWh aus der Engine-Buchführung und wird
        /// hier auf MWh gebracht — wörtlich <c>NavigatorUebersicht.Zeile</c> :159-168.
        /// </summary>
        private static Rasterzeile Eigenanteilzeile(string name, double produktionMwh,
                                                    double[] eigenanteilKanalKwh)
        {
            var zellen = new List<string>
            {
                name,
                produktionMwh.ToString("F2", CultureInfo.CurrentCulture)
            };
            for (int k = 0; k < Kanal.ANZAHL; k++)
                zellen.Add((eigenanteilKanalKwh[k] / 1000.0).ToString("F2", CultureInfo.CurrentCulture));

            return new Rasterzeile(zellen);
        }

        // =================================================================
        // Brennstoffzeilen
        // =================================================================

        /// <summary>
        /// Die Brennstoff-Kennungen je Kesselzeile. Sie spiegeln die Buchung der Engine
        /// (<c>SimulationSPK.Bilanz_und_Nutzungsgrad</c>) — wörtlich aus
        /// <c>KesselBrennstoffZeilenVorbereiten</c> :1093-1105. Ändert sich die Buchung
        /// dort, gehört diese Tabelle nachgezogen (offener Punkt des Vorläufers).
        /// </summary>
        private static readonly int[][] KESSEL_BRENNSTOFF_IDS =
        {
            new[] { 1, 2, 3, 4, 5, 14 },                    // Gas (14 = Biogas)
            new[] { 6, 7, 8, 9, 18, 19, 20, 21, 22 },       // Öl
            new[] { 10 },                                   // Koks
            new[] { 11 },                                   // Kohle
            new[] { 12 },                                   // Holz
            new[] { 15 },                                   // Pellets
            new[] { 16 },                                   // Rapsöl
            new[] { 13 },                                   // Elektrowärme
            new[] { 17 },                                   // Tierische Fette
            new int[0]                                      // Sonstige: alles Übrige
        };

        private List<Brennstoffzeile> Kesselbrennstoffe()
        {
            var zeilen = new List<Brennstoffzeile>();
            if (sim.simulation_spk == null) return zeilen;

            string[] namen =
            {
                MyResource.Resource.SIM_LABEL_GASVERBRAUCH,
                MyResource.Resource.SIM_LABEL_OELVERBRAUCH,
                MyResource.Resource.SIM_LABEL_KOKS,
                MyResource.Resource.SIM_LABEL_KOHLE,
                MyResource.Resource.SIM_LABEL_HOLZVERBRAUCH,
                MyResource.Resource.SIM_LABEL_PELLETS,
                MyResource.Resource.SIM_LABEL_RAPSOEL,
                MyResource.Resource.SIMERG_LBL_STROMVERBRAUCH,   // Elektrowärme
                MyResource.Resource.SIM_LABEL_TIERISCHE_FETTE,
                MyResource.Resource.SIM_LABEL_SONSTIGE
            };

            double[] werte =
            {
                sim.simulation_spk.Gasverbrauch_SPK,
                sim.simulation_spk.Oelverbrauch_SPK,
                sim.simulation_spk.Koks_SPK,
                sim.simulation_spk.Kohle_SPK,
                sim.simulation_spk.Holzverbrauch_SPK,
                sim.simulation_spk.Pellets_SPK,
                sim.simulation_spk.Rapsoelverbrauch_SPK,
                sim.simulation_spk.Stromverbrauch_Spk,
                sim.simulation_spk.TierischeFette_SPK,
                sim.simulation_spk.Sonstigverbrauch_SPK
            };

            HashSet<int> arten = HeizkesselStammCtrl.BrennstoffartenJeProjekt(m_ID_Projekt);

            // „Sonstige" fängt jede Kennung auf, die keine der übrigen Zeilen führt -
            // dieselbe else-Verzweigung wie in der Engine.
            HashSet<int> bekannt = new HashSet<int>();
            foreach (int[] ids in KESSEL_BRENNSTOFF_IDS) foreach (int id in ids) bekannt.Add(id);

            // RÜCKFALL, wörtlich :1148-1153: Kennt die Datenbank keinen Brennstoff UND
            // trägt kein Feld einen Wert, bleibt der Block vollständig stehen. Ein
            // leerer Block wäre die schlechtere Auskunft.
            bool nichtsBekannt = arten.Count == 0;
            if (nichtsBekannt)
                foreach (double w in werte) if (w > 0) { nichtsBekannt = false; break; }

            for (int i = 0; i < werte.Length; i++)
            {
                bool sichtbar;
                if (nichtsBekannt)
                {
                    sichtbar = true;
                }
                else if (KESSEL_BRENNSTOFF_IDS[i].Length == 0)
                {
                    sichtbar = werte[i] > 0;
                    if (!sichtbar)
                        foreach (int a in arten) if (!bekannt.Contains(a)) { sichtbar = true; break; }
                }
                else
                {
                    sichtbar = SimulationErgebnisCtrl.BrennstoffZeileSichtbar(
                        werte[i], KESSEL_BRENNSTOFF_IDS[i][0], null);
                    if (!sichtbar)
                        foreach (int id in KESSEL_BRENNSTOFF_IDS[i])
                            if (arten.Contains(id)) { sichtbar = true; break; }
                }

                zeilen.Add(new Brennstoffzeile(namen[i], werte[i], sichtbar));
            }

            return zeilen;
        }

        /// <summary>
        /// Die Brennstoffzeilen des BHKW — nur die mit Verbrauch &gt; 0 (wörtlich
        /// <c>AktualisiereBrennstoffAnzeige</c> :7566-7612, samt Reihenfolge).
        /// </summary>
        private List<Brennstoffzeile> Bhkwbrennstoffe(SimulationErgebnisCtrl.BhkwErgebnis _)
        {
            var zeilen = new List<Brennstoffzeile>();
            SimulationBHKW b = sim.simulation_bhkw;
            if (b == null) return zeilen;

            void Zeile(string name, double wert)
            {
                if (wert > 0) zeilen.Add(new Brennstoffzeile(name, wert, true));
            }

            Zeile(MyResource.Resource.SIM_LABEL_GASVERBRAUCH, b.Gasverbrauch_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_OELVERBRAUCH, b.Oelverbrauch_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_HOLZVERBRAUCH, b.Holzmenge_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_PELLETS, b.Pellets_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_RAPSOEL, b.Rapsoelverbrauch_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_TIERISCHE_FETTE, b.TierischeFette_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_KOKS, b.Koks_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_KOHLE, b.Kohle_BHKW);
            Zeile(MyResource.Resource.SIM_LABEL_SONSTIGE, b.Sonstigemenge_BHKW);

            return zeilen;
        }

        // =================================================================
        // Der Stromspeicher-Reiter
        // =================================================================

        private SpeicherErgebnisDaten SpeicherDaten()
        {
            var d = new SpeicherErgebnisDaten();

            SpeicherErgebnis erg = sim.Speicherergebnis;
            if (erg == null || !sim.bSimulationSSP)
            {
                d.LaufVorhanden = false;
                d.Kopf = MyResource.Resource.SP_ERG_KEIN_LAUF;
                return d;
            }

            StromspeicherLaufKontext kontext = sim.Speicherkontext;
            ErgebnisStromspeicherModel k = StromspeicherSimCtrl.AlsErgebnismodell(erg, kontext);

            SpeicherErgebnis vergleich = kontext != null ? kontext.Vergleichsergebnis : null;
            ErgebnisStromspeicherModel kv = vergleich != null
                ? StromspeicherSimCtrl.AlsErgebnismodell(vergleich, kontext)
                : null;

            d.LaufVorhanden = true;
            d.Kopf = string.Format(MyResource.Resource.SP_ERG_KOPF_VARIANTE,
                                   k.Bezeichner, k.Betriebsart, k.Berechnungsart);
            d.MitVergleich = vergleich != null;
            d.Kacheln = Kacheln(k, erg, kontext);
            d.Kennzahlen = SpeicherKennzahlenBlock.Zeilen(k, erg, kontext, kv, vergleich);
            Ampel(d, k, kontext);

            // Warnzeile für einen Lauf ohne jede Erzeugung (Abnahmebefund 2). Bedingung
            // ist die Erzeugung des LAUFS, nicht das PV-Modulflag.
            d.Erzeugungshinweis = erg.Kennzahlen.ErzeugungKwh > 0.0
                ? "" : MyResource.Resource.SP_ERG_OHNE_ERZEUGUNG;

            d.MehrereVarianten =
                WErzeugerCtrl.AnlagenJeTyp(m_ID_Projekt, WizardItemClass.SP_TYP).Count > 1;

            return d;
        }

        /// <summary>
        /// Die zwölf Kacheln des Kernblocks — wörtlich aus <c>SpKernblockFuellen</c>
        /// :7027-7076, samt „–" für alles, was dieser Lauf nicht kennt.
        /// </summary>
        private static List<(string, string)> Kacheln(ErgebnisStromspeicherModel k,
                                                      SpeicherErgebnis erg,
                                                      StromspeicherLaufKontext kontext)
        {
            CultureInfo kultur = CultureInfo.CurrentCulture;
            SpeicherParameter p = kontext != null ? kontext.Parameter : null;
            string un = SpeicherKennzahlenBlock.UNBESTIMMT;

            string bereich(double a, double b, string format)
                => string.Format(MyResource.Resource.SP_ERG_KERN_BEREICH,
                                 a.ToString(format, kultur), b.ToString(format, kultur));

            return new List<(string, string)>
            {
                (MyResource.Resource.SP_ERG_KERN_KAPAZITAET,
                 p != null ? p.CNomKwh.ToString("N1", kultur) : un),
                (MyResource.Resource.SP_ERG_KERN_LEISTUNG,
                 p != null ? p.PKw.ToString("N1", kultur) : un),
                (MyResource.Resource.SP_ERG_KERN_SOC_PROZENT,
                 p != null && p.CNomKwh > 0.0
                     ? bereich(p.SoCMinKwh / p.CNomKwh * 100.0, p.SoCMaxKwh / p.CNomKwh * 100.0, "N0")
                     : un),
                (MyResource.Resource.SP_ERG_KERN_SOC_KWH,
                 p != null ? bereich(p.SoCMinKwh, p.SoCMaxKwh, "N1") : un),
                (MyResource.Resource.SP_ERG_KERN_BETRIEBSART,
                 string.IsNullOrEmpty(k.Betriebsart) ? un : k.Betriebsart),
                (MyResource.Resource.SP_ERG_KERN_BERECHNUNGSART,
                 string.IsNullOrEmpty(k.Berechnungsart) ? un : k.Berechnungsart),
                (MyResource.Resource.SP_ERG_KERN_ERTRAG, k.Ertrag_Aequivalent.ToString("N2", kultur)),
                (MyResource.Resource.SP_ERG_KERN_UEBERSCHUSS, k.Jahresueberschuss.ToString("N2", kultur)),
                (MyResource.Resource.SP_ERG_KERN_AMORTISATION,
                 SpeicherAnzeigeCtrl.AmortisationText(erg.Wirtschaftlichkeit.StatischeAmortisation)),
                (MyResource.Resource.SP_ERG_KERN_VOLLZYKLEN, k.Vollzyklen.ToString("N1", kultur)),
                // Ohne Erzeugung ist die Eigenverbrauchsquote unbestimmt (0/0), nicht null.
                (MyResource.Resource.SP_ERG_KERN_EIGENVERBRAUCH,
                 erg.Kennzahlen.ErzeugungKwh > 0.0 ? k.Eigenverbrauchsquote.ToString("N1", kultur) : un),
                (MyResource.Resource.SP_ERG_KERN_AUTARKIE, k.Autarkiegrad.ToString("N1", kultur))
            };
        }

        /// <summary>Die Zyklenampel — wörtlich aus <c>SpZyklenampelSetzen</c> :7391-7439.</summary>
        private static void Ampel(SpeicherErgebnisDaten d, ErgebnisStromspeicherModel k,
                                  StromspeicherLaufKontext kontext)
        {
            double budget = kontext != null ? kontext.ZyklenZugesichert : 0.0;

            if (budget <= 0.0)
            {
                d.Ampel = MyResource.Resource.SP_ERG_AMPEL_OHNE_ANGABE;
                d.AmpelWarnung = false;
            }
            else if (k.Zyklen_Hochrechnung > budget)
            {
                d.Ampel = string.Format(MyResource.Resource.SP_ERG_AMPEL_UEBERSCHRITTEN,
                                        k.Zyklen_Hochrechnung, budget);
                d.AmpelWarnung = true;
            }
            else if (k.Zyklen_Hochrechnung > budget * 0.9)
            {
                d.Ampel = string.Format(MyResource.Resource.SP_ERG_AMPEL_KNAPP,
                                        k.Zyklen_Hochrechnung, budget);
                d.AmpelWarnung = true;
            }
            else
            {
                d.Ampel = string.Format(MyResource.Resource.SP_ERG_AMPEL_OK,
                                        k.Zyklen_Hochrechnung, budget);
                d.AmpelWarnung = false;
            }

            // Ein vorzeitig aufgebrauchtes Zyklenbudget erklärt, warum die Preissteuerung
            // ab einem bestimmten Tag nichts mehr geplant hat (AP10, Fachkonzept 6.5).
            if (kontext != null && kontext.Arbitrageergebnis != null
                && kontext.Arbitrageergebnis.Kennzahlen.BudgetErschoepft)
            {
                d.Ampel = MyResource.Resource.ARB_ERG_AMPEL_ERSCHOEPFT + Environment.NewLine + d.Ampel;
                d.AmpelWarnung = true;
            }

            // Der Kompatibilitätsmodus liefert bewusst kein Produktivergebnis - das darf
            // auf der Seite nicht untergehen (Fachkonzept 5.2).
            if (kontext != null && kontext.Kompatibilitaetsmodus)
            {
                d.Ampel = MyResource.Resource.SP_ERG_KOMPATIBILITAET_AKTIV + Environment.NewLine + d.Ampel;
                d.AmpelWarnung = true;
            }
        }

        // =================================================================
        // Die Autarkie-Analyse (DashboardForm)
        // =================================================================

        private double AutarkieKapazitaet()
        {
            if (_autarkieGesetzt) return _autarkieKwh;

            _autarkieKwh = StromspeicherStammCtrl.KapazitaetJeProjekt(m_ID_Projekt);
            _autarkieGesetzt = true;
            return _autarkieKwh;
        }

        /// <summary>
        /// Rechnet die Autarkiekacheln zu einer Was-wäre-wenn-Kapazität — wörtlich aus
        /// <c>DashboardForm.UpdateSimulationData</c> :314-390 samt
        /// <c>RechneSpeicher</c> :414-423.
        ///
        /// <para><b>Die Kapazität wird NIE zurückgeschrieben</b> (Befund W11-B32): Sie ist
        /// eine Was-wäre-wenn-Größe, und die Seite sagt das jetzt auch.</para>
        /// </summary>
        private AutarkieDaten AutarkieRechnen(double kwh)
        {
            _autarkieKwh = kwh;
            _autarkieGesetzt = true;

            var d = new AutarkieDaten { SpeicherKwh = kwh };
            if (!_ergebnisGueltig) return d;

            ErgebnisPraesenz p = ErgebnisPraesenz.Ermitteln(sim);
            d.HatPv = p.Photovoltaik;
            d.HatSolarthermie = p.Solarthermie;

            float[] pvProd = sim.simulation_pv.pvPotentialGesamt_stuendlich;
            float[] stromBedarf = sim.simulation_pv.Strombedarf_stuendlich;

            float[] stProd = new float[Kanalsatz.STUNDEN_JAHR];
            for (int i = 0; i < Kanalsatz.STUNDEN_JAHR; i++)
                stProd[i] = (float)(sim.simulation_solarthermie.Waermeproduktion[i]
                                    + sim.simulation_solarthermie.Ueberschuss[i]);

            float[] waermeBedarf = Array.ConvertAll<double, float>(
                sim.simulation_solarthermie.Waermebedarf, x => (float)x);

            SpeicherErgebnis speicher = AutarkieSpeicher(stromBedarf, pvProd, kwh);

            double gesWaerme = waermeBedarf.Sum();
            double stPotenzial = stProd.Sum();

            double lastKwh = speicher.Kennzahlen.LastKwh;
            double pvDirekt = speicher.Kennzahlen.DirektverbrauchKwh;
            double pvSpeicher = speicher.EntladeenergieKwh;

            double stGenutzt = 0;
            for (int i = 0; i < Kanalsatz.STUNDEN_JAHR; i++)
                stGenutzt += Math.Min(stProd[i], waermeBedarf[i]);

            d.AutarkiePvProzent = lastKwh > 0 ? (pvDirekt + pvSpeicher) / lastKwh * 100.0 : 0.0;
            d.DeckungStProzent = gesWaerme > 0 ? stGenutzt / gesWaerme * 100.0 : 0.0;
            d.DeckungStBekannt = d.DeckungStProzent > 0;
            d.NutzungsgradStProzent = stPotenzial > 0 ? stGenutzt / stPotenzial * 100.0 : 0.0;

            // Die beiden Substitutionsfaktoren stehen seit iU9-W11a.5 im Kern
            // (EmissionsVorgaben, Befund W11-B31) - Werte unverändert.
            d.Co2ErsparnisKg = EmissionsVorgaben.Co2ErsparnisKg(pvDirekt + pvSpeicher, stGenutzt);
            d.SpeichernutzenKwh = pvSpeicher;

            _autarkieLast = RasterAdapter.ZuViertelstundenDouble(stromBedarf);
            _autarkiePv = RasterAdapter.ZuViertelstundenDouble(pvProd);
            _autarkieSpeicher = speicher;

            return d;
        }

        private double[] _autarkieLast;
        private double[] _autarkiePv;
        private SpeicherErgebnis _autarkieSpeicher;

        /// <summary>
        /// Die Speicherwirkung der eingestellten Kapazität über dieselbe Engine, die auch
        /// die Simulationskette rechnet (wörtlich <c>RechneSpeicher</c> :414-423).
        /// </summary>
        private static SpeicherErgebnis AutarkieSpeicher(float[] last, float[] pv, double kwh)
        {
            double[] lastKw = RasterAdapter.ZuViertelstundenDouble(last);
            double[] pvKw = RasterAdapter.ZuViertelstundenDouble(pv);

            SpeicherEingang eingang = new SpeicherEingang(
                lastKw, pvKw,
                SpeicherEingang.KonstanteReihe(StromspeicherSimCtrl.FIXPREIS_BEZUG_CT_KWH, lastKw.Length));

            return new Dauernutzung(SpeicherModus.Energetisch)
                       .Berechne(eingang, StromspeicherSimCtrl.StandardParameter(kwh, kwh));
        }

        // =================================================================
        // Die beiden Ganglinien-Reiter
        // =================================================================

        private WaermegangDaten WaermegangDaten(ErgebnisPraesenz p)
        {
            var d = new WaermegangDaten
            {
                Erzeuger = new[]
                {
                    new Ganglinienreihe("WAERMEPUMPE", MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, p.Waermepumpe),
                    new Ganglinienreihe("HEIZSTAB", MyResource.Resource.CHART_SEGMENT_HEIZSTAB, p.Heizstab),
                    new Ganglinienreihe("HEIZKESSEL", MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, p.Heizkessel),
                    new Ganglinienreihe("SOLARTHERMIE", MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE, p.Solarthermie),
                    new Ganglinienreihe("BHKW_WAERME", MyResource.Resource.SIM_ERZEUGERNAME_BHKW, p.BHKW)
                },
                Speicher = Speicherreihen(),
                Bedarfsarten = Bedarfsarten()
            };
            return d;
        }

        /// <summary>Je Speicher eine Füllstandsreihe — Schlüssel wie im Vorläufer.</summary>
        private List<Ganglinienreihe> Speicherreihen()
        {
            var liste = new List<Ganglinienreihe>();
            List<SimulationPufferspeicher> speicher = sim.AlleSpeicher();

            for (int i = 0; i < speicher.Count; i++)
            {
                SimulationPufferspeicher sp = speicher[i];
                if (sp == null) continue;
                liste.Add(new Ganglinienreihe(sp.Schluessel(i), sp.BezeichnerAnzeige(), true));
            }
            return liste;
        }

        /// <summary>
        /// „Gesamt" immer, ein Kanal nur mit Jahressumme &gt; 0 — wörtlich
        /// <c>AktualisiereBedarfsartAuswahl</c> :460-491.
        /// </summary>
        private List<(int, string)> Bedarfsarten()
        {
            var liste = new List<(int, string)> { (-1, MyResource.Resource.CHART_LEGENDE_GESAMT) };

            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                float[] werte = SimulationControl.BedarfKanalStuendlich(_waermebedarf, k);
                if (werte == null || Jahressumme(werte) <= 0) continue;
                liste.Add((k, KANALNAMEN[k]));
            }
            return liste;
        }

        private StromgangDaten StromgangDaten(ErgebnisPraesenz p)
        {
            return new StromgangDaten
            {
                Reihen = new[]
                {
                    new Ganglinienreihe("GESAMT", MyResource.Resource.CHART_LEGENDE_GESAMT, true),
                    new Ganglinienreihe("PROFIL_LASTGANG", MyResource.Resource.CHART_LEGENDE_PROFIL_LASTGANG, true),
                    new Ganglinienreihe("WAERMEPUMPE", MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, p.Waermepumpe),
                    new Ganglinienreihe("HEIZSTAB", MyResource.Resource.CHART_SEGMENT_HEIZSTAB, p.Heizstab),
                    new Ganglinienreihe("HEIZKESSEL", MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, p.Heizkessel),
                    new Ganglinienreihe("BHKW_STROM", MyResource.Resource.SIM_ERZEUGERNAME_BHKW, p.BHKW),
                    new Ganglinienreihe("PV", MyResource.Resource.SIM_PHOTOVOLTAIK, p.Photovoltaik)
                }
            };
        }

        // =================================================================
        // Die Temperaturreihen (Paket P2)
        // =================================================================

        /// <summary>Eine Reihe der Speichertemperatur-Seite.</summary>
        private sealed class Temperaturreihe
        {
            internal string Legende;
            internal float[] Werte;
            internal SKColor Farbe;
            internal bool Gestrichelt;
            internal string Schluessel;
        }

        /// <summary>Die vier Farben der Speicherpaare (wörtlich <c>TEMP_FARBEN</c> :124-131).</summary>
        private static readonly SKColor[] TEMP_FARBEN =
        {
            new SKColor(0xC0, 0x39, 0x2B), new SKColor(0x28, 0x80, 0xB9),
            new SKColor(0x1D, 0x9E, 0x75), new SKColor(0x8E, 0x44, 0xAD)
        };

        private static readonly SKColor TEMP_FARBE_QUELLE = new SKColor(0xD8, 0x5A, 0x30);

        /// <summary>
        /// Die Temperaturreihen des Laufs — je Senkenspeicher zwei (oben, unten
        /// gestrichelt), je temperaturgekoppeltem Erzeuger eine Quelltemperatur
        /// (wörtlich <c>Temperaturreihen</c> :2490-2554).
        /// </summary>
        private List<Temperaturreihe> Temperaturreihen()
        {
            var liste = new List<Temperaturreihe>();
            if (!_ergebnisGueltig) return liste;

            List<SimulationPufferspeicher> speicher = sim.AlleSpeicher();
            int nummer = 0;

            for (int i = 0; i < speicher.Count; i++)
            {
                SimulationPufferspeicher sp = speicher[i];
                if (sp == null || sp.IstQuelle || !sp.T_oben_Mittel.HasValue) continue;
                if (sp.T_oben_stuendlich == null || sp.T_unten_stuendlich == null) continue;

                string schluessel = sp.Schluessel(i);
                SKColor farbe = TEMP_FARBEN[nummer % TEMP_FARBEN.Length];
                nummer++;

                liste.Add(new Temperaturreihe
                {
                    Schluessel = schluessel + ZeitreihenSatz.SUFFIX_T_OBEN,
                    Legende = sp.BezeichnerAnzeige() + " " + MyResource.Resource.SIM_REIHE_T_OBEN,
                    Werte = sp.T_oben_stuendlich,
                    Farbe = farbe
                });
                liste.Add(new Temperaturreihe
                {
                    Schluessel = schluessel + ZeitreihenSatz.SUFFIX_T_UNTEN,
                    Legende = sp.BezeichnerAnzeige() + " " + MyResource.Resource.SIM_REIHE_T_UNTEN,
                    Werte = sp.T_unten_stuendlich,
                    Farbe = farbe,
                    Gestrichelt = true
                });
            }

            if (sim.bSimulationWP && sim.simulation_wp != null)
            {
                var profile = sim.simulation_wp.Quelltemperaturen;
                var anlagen = sim.simulation_wp.wp_list;

                for (int i = 0; i < profile.Count && i < anlagen.Count; i++)
                {
                    if (!sim.simulation_wp.QuelleGekoppelt(i) || profile[i] == null) continue;
                    Quellreihe(liste, anlagen[i], profile[i], sim.simulation_wp.WP_Modul[i]);
                }
            }

            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                var anlagen = sim.simulation_spk.spk_anlagen_ids;
                for (int i = 0; i < anlagen.Count; i++)
                {
                    float[] reihe = sim.simulation_spk.Quelltemperaturen(i);
                    if (reihe == null) continue;
                    Quellreihe(liste, anlagen[i], reihe, sim.simulation_spk.KesselName(i));
                }
            }

            return liste;
        }

        /// <summary>Hängt eine Quelltemperatur-Reihe an; doppelte Anlagen-IDs übergeht sie.</summary>
        private static void Quellreihe(List<Temperaturreihe> liste, int idAnlage,
                                       float[] werte, string bezeichner)
        {
            if (idAnlage <= 0 || werte == null) return;

            string schluessel = ZeitreihenSatz.QUELLTEMP_PRAEFIX + idAnlage;
            foreach (Temperaturreihe r in liste)
                if (string.Equals(r.Schluessel, schluessel, StringComparison.Ordinal)) return;

            liste.Add(new Temperaturreihe
            {
                Schluessel = schluessel,
                Legende = (string.IsNullOrEmpty(bezeichner) ? schluessel : bezeichner) +
                          " " + MyResource.Resource.SIM_REIHE_QUELLTEMPERATUR,
                Werte = werte,
                Farbe = TEMP_FARBE_QUELLE
            });
        }
    }
}
