using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>Baustein 3: Projektbeschreibung (Stamm vollständig; Konzept Kap. 4).</summary>
    public class ProjektbeschreibungBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_PROJEKT; } }
        public string Titel { get { return "Projektbeschreibung"; } }

        /// <summary>Die Überschrift des Kapitels (Überschrift 1) — Schlüssel der Übersetzung in
        /// <see cref="BerichtTexte"/>; dieselbe Quelle hat <see cref="Berichtskapitel.Ueberschrift"/>.</summary>
        public const string UEBERSCHRIFT = "Projektbeschreibung";

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;

            k.Ueberschrift1(UEBERSCHRIFT);

            ProjektModel p = stamm.Projekt;
            k.Eigenschaften(
                "Projektname", p != null ? p.m_szProjektname : daten.Stammprojektname,
                "Kunde", p != null ? p.m_szKunde : "",
                "Bearbeiter", p != null ? p.m_szBearbeiter : "",
                "Beschreibung", p != null ? p.m_szBeschreibung : "",
                "Klimaregion", stamm.Details != null && stamm.Details.KlimaregionName.Length > 0
                    ? stamm.Details.KlimaregionName : "—",
                "Angelegt", p != null ? p.m_Erstelldatum.ToString("dd.MM.yyyy", k.Kultur) : "—",
                "Zuletzt geändert", p != null ? p.m_Aenderungsdatum.ToString("dd.MM.yyyy", k.Kultur) : "—",
                "Simulationsstand", stamm.SimulationsStand.HasValue
                    ? stamm.SimulationsStand.Value.ToString("dd.MM.yyyy HH:mm", k.Kultur) : "—");

            // Gebäude des Stammprojekts.
            if (stamm.Details != null && stamm.Details.Gebaeude != null && stamm.Details.Gebaeude.Rows.Count > 0)
            {
                k.Ueberschrift2("Gebäude");
                foreach (DataRow g in stamm.Details.Gebaeude.Rows)
                {
                    k.Ueberschrift3(ProjektDetails.S(g, "Gebaeudename"));
                    k.Eigenschaften(
                        "Gebäudeart", Oder(ProjektDetails.S(g, "Gebaeudeart"), ProjektDetails.S(g, "Typ")),
                        // E47: der Klartext der Klasse (Bauzeitraum), nicht ihr Buchstabe.
                        "Baualtersklasse", Oder(Gebaeudeklassen.Text(ProjektDetails.S(g, "Baualtersklasse")), "—"),
                        "Wohn-/Nutzfläche", Zahl(k, g, "Wohnflaeche_gesamt", "m²", 0),
                        "Bewohner/Nutzer", Zahl(k, g, "Bewohner", "", 0),
                        "Wärmebedarf", Zahl(k, g, "Waermebedarf", "kWh/a", 0),
                        "spez. Wärmeverbrauch", Zahl(k, g, "spez_Waermeverbrauch", "kWh/m²a", 1),
                        "Warmwasserbedarf", Zahl(k, g, "WW_Bedarf", "kWh/a", 0),
                        "Raumhöhe", Zahl(k, g, "Raumhoehe", "m", 2));

                    // Stufe G6a: die Zonen dieses Gebaeudes - der Abschnitt entfaellt ohne Zonen;
                    // Stufe G6b: mit den Zonenzeilen des Laufs (Tab_ErgebnisZone, E30).
                    ZonentabelleSchreiben(k, stamm.Details, g,
                                          ErgebnisZonen(stamm, (int)(ProjektDetails.D(g, "ID") ?? 0)));
                }
            }

            // Bedarfe aus der Simulation (Stamm).
            ErgebnisEnergiebedarfModel e = stamm.Ergebnis != null ? stamm.Ergebnis.Energiebedarf : null;
            if (e != null)
            {
                k.Ueberschrift2("Energiebedarf (Simulationsergebnis Stamm)");
                // PAKET E1 (Konzept 4.4): Der Wärmebedarf steht mit seinen drei Kanälen
                // da — die drei „davon"-Zeilen addieren sich zur Zeile darüber. Sie
                // stehen unmittelbar hinter der Summe und vor der Wärmelast, damit die
                // Zerlegung als solche lesbar bleibt.
                k.Eigenschaften(
                    "Wärmebedarf gesamt", k.F(e.Waermebedarf_Gesamt, 0) + " MWh/a",
                    "davon Heizung", KanalWert(k, e, Kanal.HEIZUNG),
                    "davon Brauchwasser", KanalWert(k, e, Kanal.BRAUCHWASSER),
                    "davon Prozesswärme", KanalWert(k, e, Kanal.PROZESS),
                    "Wärmelast max.", k.F(e.Waermelast_Max, 0) + " kW",
                    "Strombedarf gesamt", k.F(e.Strombedarf_Gesamt, 0) + " MWh/a",
                    "Strombedarf max.", k.F(e.Strombedarf_Max, 0) + " kW");

                // Deckungsgrade je Bedarfsart (Konzept 4.4). Sie beantworten, was der
                // Gesamtdeckungsgrad verdeckt: ob die Auslegung Warmwasser und Prozess
                // ebenso trägt wie die Heizung. Ein Kanal ohne Bedarf erscheint als „—" —
                // ein Deckungsgrad ohne Bedarf ist keine 0, sondern undefiniert.
                k.Ueberschrift2("Deckungsgrade je Bedarfsart");
                k.Eigenschaften(
                    "Deckungsgrad Heizung", DeckungWert(k, stamm, "energie.deckung_heizung"),
                    "Deckungsgrad Brauchwasser", DeckungWert(k, stamm, "energie.deckung_brauchwasser"),
                    "Deckungsgrad Prozesswärme", DeckungWert(k, stamm, "energie.deckung_prozess"));

                // STUFE KU1 (Kuehlkonzept 8.4; E21, K5, K18): der Kaeltebedarf des Stamms.
                // BV-E3: Die Namen der Kuehltraeger kommen aus dem Wertesatz des Laufs
                // (BerichtsDaten.Wirtschaft) — beim Schreiben ohne Datenbank.
                KaelteSchreiben(k, stamm, id => WirtschaftsBerichtswerte.Von(daten).Traegername(id));
            }

            // ENTSCHEID E30: die Kennzahlen je Gebaeude aus Tab_ErgebnisGebaeude.
            GebaeudeErgebnisseSchreiben(k, stamm);

            // PAKET P2 (Konzept 7.4): die Speichertemperaturen des Schichtmodells.
            SpeichertemperaturenSchreiben(k, stamm);
        }

        /// <summary>Überschrift des Abschnitts (E30) — zugleich Schlüssel der Übersetzung in <see cref="BerichtTexte"/>.</summary>
        internal const string UEBERSCHRIFT_GEBAEUDE_ERGEBNIS = "Gebäude (Simulationsergebnis Stamm)";

        /// <summary>Die Zeile über der Zonentabelle eines Gebäudes (G6a) — zugleich Schlüssel der Übersetzung.</summary>
        internal const string UEBERSCHRIFT_ZONEN = "Zonen";

        /// <summary>Der Hinweis unter der Zonentabelle, wenn ein Volumen abgeleitet ist — zugleich Schlüssel der Übersetzung.</summary>
        internal const string HINWEIS_ZONENVOLUMEN = "* Volumen aus Nutzfläche × Raumhöhe abgeleitet.";

        /// <summary>
        /// Die Zonenzeilen des Laufs zu einem Gebäude (Stufe G6b, <c>Tab_ErgebnisZone</c>, E30) —
        /// gefunden über die Gebäudezeile; leer ohne Lauf und bei höchstens einer Zone.
        /// </summary>
        private static List<ErgebnisZoneModel> ErgebnisZonen(VariantenDaten stamm, int idGebaeude)
        {
            ErgebnisGebaeudeModel g = GebaeudeZeilen(stamm).FirstOrDefault(x => x.ID_Gebaeude == idGebaeude);
            return g?.Zonen ?? new List<ErgebnisZoneModel>();
        }

        /// <summary>
        /// <b>Die Zonen eines Gebäudes</b> (Stufe G6a; Mehrzonenkonzept 7 und 9) — Zone, Nutzfläche,
        /// Volumen, H_T, H_ve und Bauteile je Zone samt Summenzeile, Bauform wie die
        /// Speichertemperaturen. Die Werte kommen nur aus der einen Formel (<see cref="Zonenkennwerte"/>,
        /// über <see cref="ProjektDetails.Kennwerte"/>); ein abgeleitetes Volumen trägt einen Stern und
        /// den Hinweis darunter. <b>Stufe G6b:</b> Trägt der Lauf Zonenzeilen
        /// (<paramref name="ergebnis"/>, aus <c>Tab_ErgebnisZone</c> — der Bericht liest nur
        /// Gespeichertes, E30), kommen „beheizt", Heizwärme und Spitze dazu, über die Zone gefunden; eine
        /// unbeheizte Zone und eine Zone ohne Zeile zeigen „—". Die Summenzeile summiert die Heizwärme,
        /// die Spitzen nicht (sie treten nicht gleichzeitig auf). Ohne Zonenzeilen bleibt die Tabelle,
        /// wie sie ist. Der Abschnitt entfällt ohne Zonen.
        /// </summary>
        private static void ZonentabelleSchreiben(WordKontext k, ProjektDetails details, DataRow gebaeude,
                                                  List<ErgebnisZoneModel> ergebnis)
        {
            if (details == null || gebaeude == null) return;
            List<ZoneModel> zonen = details.ZonenVon((int)(ProjektDetails.D(gebaeude, "ID") ?? 0));
            if (zonen.Count == 0) return;
            bool mitErgebnis = ergebnis != null && ergebnis.Count > 0;

            k.Text(UEBERSCHRIFT_ZONEN);
            int[] w = mitErgebnis
                ? new[] { 1755, 1000, 950, 950, 950, 850, 850, 1100, 950 }
                : new[] { 2355, 1500, 1500, 1400, 1400, 1200 };
            Table t = k.NeueTabelle(w);
            var kopf = new TableRow();
            kopf.Append(k.Zelle("Zone", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("Nutzfläche [m²]", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("Volumen [m³]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("H_T [W/K]", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("H_ve [W/K]", w[4], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("Bauteile", w[5], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            if (mitErgebnis)
            {
                kopf.Append(k.Zelle("beheizt", w[6], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("Heizwärme [MWh/a]", w[7], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("Spitze [kW]", w[8], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            }
            t.Append(kopf);

            double heizwaerme = 0.0;
            bool heizwaermeDa = false;
            double flaeche = 0.0, volumen = 0.0, ht = 0.0, hve = 0.0;
            bool flaecheBekannt = true, volumenBekannt = true, abgeleitet = false;
            int bauteile = 0;
            foreach (ZoneModel z in zonen)
            {
                Zonenkennwerte kw = details.Kennwerte(z, gebaeude);
                var tr = new TableRow();
                tr.Append(k.Zelle(string.IsNullOrWhiteSpace(z.Bezeichner) ? "—" : z.Bezeichner, w[0], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(kw.Nutzflaeche.HasValue ? k.F(kw.Nutzflaeche.Value, 1) : "—", w[1], false, null, JustificationValues.Right));
                string vol = kw.Volumen.HasValue ? k.F(kw.Volumen.Value, 0) + (kw.VolumenAbgeleitet ? " *" : "") : "—";
                tr.Append(k.Zelle(vol, w[2], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(k.F(kw.HT, 1), w[3], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(k.F(kw.HVe, 1), w[4], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(kw.Bauteile.ToString(k.Kultur), w[5], false, null, JustificationValues.Right));
                if (mitErgebnis)
                {
                    ErgebnisZoneModel ez = ergebnis.FirstOrDefault(e => e.ID_Zone == z.ID);
                    tr.Append(k.Zelle(ez == null ? "—" : ez.IstBeheizt ? "ja" : "nein", w[6], false, null, JustificationValues.Center));
                    tr.Append(k.Zelle(ez?.HeizwaermeMwh is double q ? k.F(q, 1) : "—", w[7], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(ez?.SpitzeKw is double p ? k.F(p, 1) : "—", w[8], false, null, JustificationValues.Right));
                    if (ez?.HeizwaermeMwh is double s) { heizwaerme += s; heizwaermeDa = true; }
                }
                t.Append(tr);

                if (kw.Nutzflaeche is double a) flaeche += a; else flaecheBekannt = false;
                if (kw.Volumen is double v) volumen += v; else volumenBekannt = false;
                abgeleitet |= kw.Volumen.HasValue && kw.VolumenAbgeleitet;
                ht += kw.HT;
                hve += kw.HVe;
                bauteile += kw.Bauteile;
            }

            var summe = new TableRow();
            summe.Append(k.Zelle("Summe", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            summe.Append(k.Zelle(flaecheBekannt ? k.F(flaeche, 1) : "—", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Right));
            summe.Append(k.Zelle(volumenBekannt ? k.F(volumen, 0) : "—", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Right));
            summe.Append(k.Zelle(k.F(ht, 1), w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Right));
            summe.Append(k.Zelle(k.F(hve, 1), w[4], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Right));
            summe.Append(k.Zelle(bauteile.ToString(k.Kultur), w[5], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Right));
            if (mitErgebnis)
            {
                summe.Append(k.Zelle("", w[6], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                summe.Append(k.Zelle(heizwaermeDa ? k.F(heizwaerme, 1) : "—", w[7], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Right));
                summe.Append(k.Zelle("—", w[8], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Right));
            }
            t.Append(summe);
            k.Fuege(t);
            if (abgeleitet) k.Hinweis(HINWEIS_ZONENVOLUMEN);
        }

        /// <summary>
        /// Überschrift des Kälteabschnitts — seit Stufe KU2 Welle 3 „Kältebedarf und -deckung"
        /// (Kühlkonzept 8.4) — zugleich Schlüssel der Übersetzung.
        /// </summary>
        internal const string UEBERSCHRIFT_KAELTE = "Kältebedarf und -deckung (Simulationsergebnis Stamm)";

        /// <summary>Überschrift der Kälteerzeugertabelle (Stufe KU2 Welle 3) — zugleich Schlüssel der Übersetzung.</summary>
        internal const string UEBERSCHRIFT_KAELTEERZEUGER = "Kälteerzeuger";

        /// <summary>
        /// Die Grenze der Emissionen des Kältestroms (Kühlkonzept 6.3, Kapitel 14): betriebsbedingt
        /// über den Strom, ohne Kältemittelverluste — zugleich Schlüssel der Übersetzung.
        /// </summary>
        internal const string HINWEIS_KAELTEMITTEL =
            "Die Emissionen des Kältestroms sind betriebsbedingt über den Strom gerechnet; Kältemittelverluste sind nicht enthalten.";

        /// <summary>
        /// <b>STUFE KU1 — der Kältebedarf des Stamms</b> (Kühlkonzept 8.4; E21, K5, K18): nach dem
        /// Muster des Wärmebedarfs darüber — die Summe, die Kanalzeile „davon Kühlung", die
        /// Kältespitze, die Stunden mit Kühlbedarf (wenn der Bericht sie am Kanalvektor zählen
        /// konnte), die ungedeckte Kälte; darunter, wer sie deckt, und die Grenze der Zahl als
        /// Satz neben den Zahlen, nicht als Fußnote (3.6).
        ///
        /// <para><b>Der Abschnitt entfällt vollständig</b>, wenn der Lauf keine Kälte ERHOBEN hat
        /// (Projektschalter aus, jedes Bestands- und Referenzprojekt): „Eine Tabelle voller ‚—'
        /// wäre keine Aussage, sondern eine Frage." Gerechnet wird hier nichts — die Zahlen sind
        /// die Ergebnisspalten des Laufs (KU-S4) und die Kennzahlen des Katalogs.</para>
        /// </summary>
        private static void KaelteSchreiben(WordKontext k, VariantenDaten stamm, Func<int, string> traegername)
        {
            ErgebnisEnergiebedarfModel e = stamm?.Ergebnis?.Energiebedarf;
            if (e == null || !e.KaelteErhoben) return;

            double jahr = e.Kaeltebedarf_Gesamt ?? 0.0;
            double spitze = e.Kaeltelast_Max ?? 0.0;
            var paare = new List<string>
            {
                "Kältebedarf gesamt", k.F(jahr, 1) + " MWh/a",
                "davon Kühlung", KanalWertKaelte(k, e),
                "Kältelast max.", k.F(spitze, 1) + " kW",
            };
            double? stunden = stamm.Kennzahlen.TryGetValue(KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN, out double? s) ? s : null;
            if (stunden.HasValue)
            {
                paare.Add("Stunden mit Kühlbedarf");
                paare.Add(k.F(stunden.Value, 0) + " h/a");
            }
            if (spitze > 0)
            {
                paare.Add("Vollbenutzungsstunden Kälte");
                paare.Add(k.F(jahr * 1000.0 / spitze, 0) + " h/a");
            }
            paare.Add("Kältebedarf ungedeckt");
            paare.Add(k.F(e.Kaelterestbedarf ?? 0.0, 1) + " MWh/a");

            // STUFE KU2 WELLE 3 (Kühlkonzept 6.2–6.4, 8.4; E21, E34): die DECKUNG — nach dem
            // Muster der Wärmeseite (Deckungsgrad, Erzeuger), dazu der Kältestrom in der
            // Strombilanz mit seinem Netzbezug, seinen Kosten und Emissionen. Nur mit gerechneter
            // Kälteerzeugung; ohne Kälteerzeuger steht allein der ungedeckte Bedarf.
            ErgebnisWaermepumpeModel wp = stamm.Ergebnis.Waermepumpe;
            bool mitErzeugung = wp != null && wp.Kaelteproduktion_WP.HasValue;
            if (mitErzeugung)
            {
                paare.Add("Deckungsgrad Kühlung");
                paare.Add(KennzahlWert(k, stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD, 1, "%"));
                paare.Add("Kälteerzeugung Wärmepumpe");
                paare.Add(k.F(wp.Kaelteproduktion_WP.Value, 1) + " MWh/a");
                paare.Add("Kältestrom");
                paare.Add(Wert(k, wp.Stromverbrauch_Kuehlung, 2, "MWh/a"));
                paare.Add("Jahresarbeitszahl Kälte");
                paare.Add(KennzahlWert(k, stamm, KennzahlenKatalog.SCHLUESSEL_KAELTE_JAZ, 2, ""));
                paare.Add("Netzbezug Kältestrom");
                paare.Add(Wert(k, stamm.KaeltestromNetzbezugMWh, 2, "MWh/a"));
                paare.Add("Kosten Kältestrom");
                paare.Add(Wert(k, stamm.KaeltestromKosten, 0, "€/a"));
                paare.Add("CO₂ Kältestrom");
                paare.Add(Wert(k, stamm.KaeltestromCO2t, 2, "t/a"));
            }

            k.Ueberschrift2(UEBERSCHRIFT_KAELTE);
            k.Eigenschaften(paare.ToArray());
            if (mitErzeugung) KaelteerzeugerSchreiben(k, wp, traegername);
            k.HinweisRoh(!(jahr > 0) ? MyResource.Resource.SIMERG_HRL_KAELTE_LEER
                         : mitErzeugung ? string.Format(k.Kultur, MyResource.Resource.SIMERG_HRL_KAELTE_GEDECKT,
                                                        k.F(wp.Kaelteproduktion_WP.Value, 2),
                                                        k.F(SimulationRunner.DeckungProzent(wp.Kaelteproduktion_WP.Value, jahr), 1))
                         : MyResource.Resource.SIMERG_HRL_KAELTE_UNGEDECKT);
            k.HinweisRoh(SimulationKaeltebedarf.GrenzeFeuchte);
            if (mitErzeugung) k.Hinweis(HINWEIS_KAELTEMITTEL);
        }

        /// <summary>
        /// Die Kälteerzeugertabelle (Kühlkonzept 8.4, #32) — je Wärmepumpe im Kühlbetrieb Kälte,
        /// Kältestrom, EER-Jahreswert, ihr Netzbezug und der Stromträger, der ihn bepreist (E34).
        /// Aus den Modulzeilen des Ergebnisses (Schemaschritt 119); eine Wärmepumpe ohne Kälte steht
        /// nicht darin.
        /// </summary>
        private static void KaelteerzeugerSchreiben(WordKontext k, ErgebnisWaermepumpeModel wp,
                                                    Func<int, string> traegername)
        {
            var zeilen = wp.Module.Where(m => m != null && m.Kaelteproduktion.HasValue && m.Kaelteproduktion.Value > 0).ToList();
            if (zeilen.Count == 0) return;

            int[] b = { 2600, 1200, 1300, 1000, 1300, k.Inhaltsbreite - 7400 };
            Table t = k.NeueTabelle(b);
            var kopf = new TableRow();
            string[] titel = { "Anlage", "Kälte [MWh/a]", "Kältestrom [MWh/a]", "EER", "aus dem Netz [MWh/a]", "Stromträger" };
            for (int i = 0; i < titel.Length; i++)
                kopf.Append(k.Zelle(titel[i], b[i], true, WordBerichtGenerator.STAMM_FILL, JustificationValues.Left));
            t.Append(kopf);

            foreach (ErgebnisWaermepumpeModulModel m in zeilen)
            {
                double strom = m.Stromverbrauch_Kuehlung ?? 0.0;
                var tr = new TableRow();
                tr.Append(k.Zelle(string.IsNullOrEmpty(m.Modul) ? "—" : m.Modul, b[0], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(k.F(m.Kaelteproduktion.Value, 2), b[1], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(k.F(strom, 2), b[2], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(strom > 0 ? k.F(m.Kaelteproduktion.Value / strom, 2) : "—", b[3], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(m.Kaeltestrom_Netzbezug.HasValue ? k.F(m.Kaeltestrom_Netzbezug.Value, 2) : "—", b[4], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(KuehltraegerText(m, traegername), b[5], false, null, JustificationValues.Left));
                t.Append(tr);
            }

            k.Ueberschrift3(UEBERSCHRIFT_KAELTEERZEUGER);
            k.Fuege(t);
        }

        /// <summary>Der Stromträger des Kältestroms einer Modulzeile: der des Projekts, oder ein abweichender samt Abrechnungsart (E34).</summary>
        internal static string KuehltraegerText(ErgebnisWaermepumpeModulModel m)
        {
            return KuehltraegerText(m, Emissionsquelle.TraegerName);
        }

        /// <summary>
        /// Derselbe Text mit dem Namen des Trägers aus einer übergebenen Quelle — im Bericht aus dem
        /// Wertesatz des Laufs (<see cref="WirtschaftsBerichtswerte.Traegername"/>, BV-E3).
        /// </summary>
        internal static string KuehltraegerText(ErgebnisWaermepumpeModulModel m, Func<int, string> traegername)
        {
            if (m == null || !m.Kuehl_CarrierId.HasValue || m.Kuehl_CarrierId.Value <= 0)
                return MyResource.Resource.BER_KAELTE_TRAEGER_PROJEKT;
            string name = (traegername ?? Emissionsquelle.TraegerName)(m.Kuehl_CarrierId.Value);
            return string.Format(m.Kuehl_EigenerZaehler == true ? MyResource.Resource.BER_KAELTE_TRAEGER_ZAEHLER
                                                               : MyResource.Resource.BER_KAELTE_TRAEGER_ANTEILIG, name);
        }

        /// <summary>Ein Kennzahlwert der Variante aus dem Katalog — null wird „—".</summary>
        private static string KennzahlWert(WordKontext k, VariantenDaten v, string schluessel, int dez, string einheit)
        {
            double? w = v.Kennzahlen.TryGetValue(schluessel, out double? x) ? x : null;
            return w.HasValue ? k.F(w.Value, dez) + (einheit.Length > 0 ? " " + einheit : "") : "—";
        }

        /// <summary>Die Kanalzeile „davon Kühlung" — der vierte Eintrag des Kanalfelds (KU-S4, Spalte <c>Waermebedarf_Kuehlung</c>).</summary>
        private static string KanalWertKaelte(WordKontext k, ErgebnisEnergiebedarfModel e)
        {
            if (e.Waermebedarf_Kanal == null || Kanal.KUEHLUNG >= e.Waermebedarf_Kanal.Length) return "—";
            return k.F(e.Waermebedarf_Kanal[Kanal.KUEHLUNG], 1) + " MWh/a";
        }

        /// <summary>
        /// <b>Die Gebäudezeilen, die der Abschnitt zeigt</b> (E30) — die des letzten Laufs des
        /// Stamms, wie <c>ErgebnisCtrl.Load</c> sie aus <c>Tab_ErgebnisGebaeude</c> gelesen hat.
        ///
        /// <para><b>Der Abschnitt entfällt</b>, wenn die Liste leer ist: Das Projekt hat kein
        /// Gebäude, es gibt noch kein Ergebnis, oder die Datenbank steht vor Schemaschritt 107.
        /// Eine Überschrift ohne Zeilen wäre keine Aussage. Gerechnet wird hier nichts — der
        /// Bericht zeigt die Zahlen des Laufs.</para>
        /// </summary>
        internal static List<ErgebnisGebaeudeModel> GebaeudeZeilen(VariantenDaten v)
        {
            if (v == null || v.Ergebnis == null || v.Ergebnis.Gebaeude == null) return new List<ErgebnisGebaeudeModel>();
            return v.Ergebnis.Gebaeude.Where(g => g != null).OrderBy(g => g.Merkplatz).ToList();
        }

        /// <summary>
        /// ENTSCHEID E30 — je Gebäude Rechenweg, Wärmebedarf, die drei Spitzenwerte und, auf
        /// dem VDI-Weg, die Kühl- und Raumkennzahlen. Ein Gebäude auf dem Tagesbilanz-Weg trägt
        /// die Zeile „Tagesbilanz (Bestandsweg)" (E20, E23) und keine Kühlzeilen — dieser Weg
        /// liefert keine Kühllast (E21). Der Abschnitt entfällt nach
        /// <see cref="GebaeudeZeilen"/>.
        /// </summary>
        private static void GebaeudeErgebnisseSchreiben(WordKontext k, VariantenDaten stamm)
        {
            List<ErgebnisGebaeudeModel> zeilen = GebaeudeZeilen(stamm);
            if (zeilen.Count == 0) return;

            k.Ueberschrift2(UEBERSCHRIFT_GEBAEUDE_ERGEBNIS);
            foreach (ErgebnisGebaeudeModel g in zeilen)
            {
                k.Ueberschrift3Roh(string.IsNullOrWhiteSpace(g.Gebaeudename) ? "—" : g.Gebaeudename);

                var paare = new List<string>
                {
                    "Rechenweg", Rechenwegtext(g),
                    "Wärmebedarf Heizung", k.F(g.HeizwaermeMwh, 1) + " MWh/a",
                    "Spitzenlast (Stundenwert)", k.F(g.SpitzeKw, 1) + " kW",
                    "Spitzenlast (Tagesmittel)", k.F(g.SpitzeTagesmittelKw, 1) + " kW",
                    "Spitzenlast (95-%-Quantil)", k.F(g.Spitze95Kw, 1) + " kW",
                };
                if (g.IstVdi6007)
                {
                    // Stufe KU1 (Kuehlkonzept 6.4): Der Zusatz „(informativ)" ist entfallen - mit
                    // eingeschalteter Kuehlung ist die Kuehlenergie der Kaeltebedarf des Gebaeudes;
                    // ein Gebaeude ohne wirksame Kuehlung laeuft frei und zeigt „—" (E32, K18).
                    paare.Add("Kühlenergie"); paare.Add(Wert(k, g.KuehlenergieMwh, 1, "MWh/a"));
                    paare.Add("Stunden mit Kühlbedarf"); paare.Add(Wert(k, g.KuehlstundenH, "h/a"));
                    paare.Add("Mittlere Raumtemperatur (Nutzungszeit)"); paare.Add(Wert(k, g.MittlereRaumtemperaturC, 1, "°C"));
                    paare.Add("Überhitzungsstunden"); paare.Add(Wert(k, g.UeberhitzungsstundenH, "h/a"));
                }
                k.Eigenschaften(paare.ToArray());
            }

            if (zeilen.Any(g => !g.IstVdi6007))
                k.Hinweis("Der Tagesbilanz-Weg (Bestandsweg) liefert weder Raumtemperatur noch Kühllast.");

            // K5 (E31): Die Grenze steht an JEDER Kaeltezahl - auch an der Kuehlenergie je Gebaeude.
            if (zeilen.Any(g => g.IstVdi6007 && g.KuehlenergieMwh.HasValue))
                k.HinweisRoh(SimulationKaeltebedarf.GrenzeFeuchte);

            // ANLAGENKOPPLUNG AK1 (Konzept 9.4): der Heizkreis der gekoppelt gerechneten Gebaeude.
            HeizkreisSchreiben(k, stamm, zeilen);
            // E37: der Kaeltekreis der kuehlgekoppelt gerechneten Gebaeude.
            KuehlkreisSchreiben(k, stamm, zeilen);
        }

        /// <summary>
        /// Der Rechenweg eines Gebäudes als Ausweis (E20, E23; Anlagenkopplung 9.4): auf dem VDI-Weg
        /// mit wirksamer Kopplung einer Seite (Heiz- oder Kälteseite, E37) „VDI 6007, gekoppelt
        /// (AK1)" — dieselbe Zeile wie im Bedarfsdialog.
        /// </summary>
        internal static string Rechenwegtext(ErgebnisGebaeudeModel g)
        {
            if (!g.IstVdi6007) return MyResource.Resource.GEB_RECHENWEG_TAGESBILANZ;
            return g.IstGekoppelt || g.IstKuehlgekoppelt
                ? MyResource.Resource.GEB_RECHENWEG_VDI6007 + ", " + MyResource.Resource.GEB_RECHENWEG_GEKOPPELT
                : MyResource.Resource.GEB_RECHENWEG_VDI6007;
        }

        /// <summary>Überschrift des Abschnitts Heizkreis (Anlagenkopplung AK1) — zugleich Schlüssel der Übersetzung.</summary>
        internal const string UEBERSCHRIFT_HEIZKREIS = "Heizkreis und Übergabe (Simulationsergebnis Stamm)";

        /// <summary>Der Satz unter der Tabelle des Heizkreises — zugleich Schlüssel der Übersetzung.</summary>
        internal const string HINWEIS_HEIZKREIS =
            "Die Mittel gelten für die Stunden mit Heizbetrieb; begrenzt heißt, die Übergabe lieferte weniger, als der Sollwert verlangte.";

        /// <summary>
        /// <b>ANLAGENKOPPLUNG AK1 — der Heizkreis je gekoppeltem Gebäude</b> (Konzept 9.4): Übergabeart
        /// samt Auslegungspunkt, Heizkurve, die beiden Temperaturmittel über die Heizstunden und die
        /// Stunden mit begrenzter Übergabe. Die Zahlen sind die des Laufs (<c>Tab_ErgebnisGebaeude</c>,
        /// Schritt 128) — gerechnet wird hier nichts; Auslegungspunkt und Heizkurve sind die Eingaben
        /// des Gebäudes, ein leeres Feld die Vorgabe der Art.
        ///
        /// <para><b>Der Abschnitt entfällt</b>, wenn kein Gebäude gekoppelt gerechnet hat — jedes
        /// Bestands- und Referenzprojekt.</para>
        /// </summary>
        private static void HeizkreisSchreiben(WordKontext k, VariantenDaten stamm, List<ErgebnisGebaeudeModel> zeilen)
        {
            List<ErgebnisGebaeudeModel> gekoppelt = zeilen.Where(g => g.IstGekoppelt).ToList();
            if (gekoppelt.Count == 0) return;

            int[] b = { 2300, 2000, 2000, 1200, 1200, k.Inhaltsbreite - 8700 };
            Table t = k.NeueTabelle(b);
            var kopf = new TableRow();
            string[] titel = { "Gebäude", "Übergabe (Auslegung)", "Heizkurve", "Vorlauf Mittel [°C]",
                               "Rücklauf Mittel [°C]", "Übergabe begrenzt [h/a]" };
            for (int i = 0; i < titel.Length; i++)
                kopf.Append(k.Zelle(titel[i], b[i], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            t.Append(kopf);

            foreach (ErgebnisGebaeudeModel g in gekoppelt)
            {
                DataRow eingabe = Gebaeudeeingabe(stamm, g.ID_Gebaeude);
                var tr = new TableRow();
                tr.Append(k.Zelle(string.IsNullOrWhiteSpace(g.Gebaeudename) ? "—" : g.Gebaeudename, b[0], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(Uebergabetext(k, g.UebergabeArt, eingabe), b[1], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(Heizkurventext(k, eingabe), b[2], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(g.VorlaufMittelC.HasValue ? k.F(g.VorlaufMittelC.Value, 1) : "—", b[3], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(g.RuecklaufMittelC.HasValue ? k.F(g.RuecklaufMittelC.Value, 1) : "—", b[4], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(g.UebergabeBegrenztStundenH.HasValue ? k.F(g.UebergabeBegrenztStundenH.Value, 0) : "—", b[5], false, null, JustificationValues.Right));
                t.Append(tr);
            }

            k.Ueberschrift2(UEBERSCHRIFT_HEIZKREIS);
            k.Fuege(t);
            k.Hinweis(HINWEIS_HEIZKREIS);
            k.HinweisRoh(MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG);
        }

        /// <summary>Überschrift des Abschnitts Kältekreis (E37) — zugleich Schlüssel der Übersetzung.</summary>
        internal const string UEBERSCHRIFT_KUEHLKREIS = "Kältekreis und Kühlübergabe (Simulationsergebnis Stamm)";

        /// <summary>
        /// Der Satz unter der Tabelle des Kältekreises — zugleich Schlüssel der Übersetzung. Er trennt
        /// die begrenzten Stunden (gegen den Kühlsollwert) von den Überhitzungsstunden (gegen die
        /// Maximalraumtemperatur, E32).
        /// </summary>
        internal const string HINWEIS_KUEHLKREIS =
            "Die Mittel gelten für die Stunden mit Kühlbetrieb; begrenzt heißt, die Kühlübergabe lieferte weniger, als der Kühlsollwert verlangte. Das sind keine Überhitzungsstunden — diese zählen die Stunden über der Maximalraumtemperatur.";

        /// <summary>Der Satz zur Grenze der Zahl (K5) — zugleich Schlüssel der Übersetzung.</summary>
        internal const string HINWEIS_KUEHLKREIS_GRENZE =
            "Die Vorlaufgrenze ist eine Vorgabe, keine gerechnete Taupunktgrenze; die Kühlübergabe rechnet sensibel, ohne Entfeuchtung.";

        /// <summary>
        /// <b>E37 — der Kältekreis je kühlgekoppeltem Gebäude</b> (Anlagenkopplung 9.4, 10.5):
        /// Kühlübergabeart samt Auslegungspunkt, die Vorlaufgrenze, die beiden Temperaturmittel über
        /// die Kühlstunden und die Stunden mit begrenzter Kühlübergabe samt dem Anteil an der
        /// Vorlaufgrenze. Die Zahlen sind die des Laufs (<c>Tab_ErgebnisGebaeude</c>, Schritt 136);
        /// Auslegungspunkt und Grenze sind die Eingaben des Gebäudes, ein leeres Feld die Vorgabe der
        /// Art. Die Grenze der Zahl (K5) steht darunter.
        ///
        /// <para><b>Der Abschnitt entfällt</b>, wenn kein Gebäude kühlgekoppelt gerechnet hat.</para>
        /// </summary>
        private static void KuehlkreisSchreiben(WordKontext k, VariantenDaten stamm, List<ErgebnisGebaeudeModel> zeilen)
        {
            List<ErgebnisGebaeudeModel> gekoppelt = zeilen.Where(g => g.IstKuehlgekoppelt).ToList();
            if (gekoppelt.Count == 0) return;

            int[] b = { 2300, 2000, 1500, 1300, 1300, k.Inhaltsbreite - 8400 };
            Table t = k.NeueTabelle(b);
            var kopf = new TableRow();
            string[] titel = { "Gebäude", "Kühlübergabe (Auslegung)", "Vorlaufgrenze [°C]", "Kühlvorlauf Mittel [°C]",
                               "Kühlrücklauf Mittel [°C]", "Kühlübergabe begrenzt [h/a]" };
            for (int i = 0; i < titel.Length; i++)
                kopf.Append(k.Zelle(titel[i], b[i], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            t.Append(kopf);

            foreach (ErgebnisGebaeudeModel g in gekoppelt)
            {
                DataRow eingabe = Gebaeudeeingabe(stamm, g.ID_Gebaeude);
                var tr = new TableRow();
                tr.Append(k.Zelle(string.IsNullOrWhiteSpace(g.Gebaeudename) ? "—" : g.Gebaeudename, b[0], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(Kuehluebergabetext(k, g.KuehlUebergabeArt, eingabe), b[1], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(Vorlaufgrenzetext(k, g.KuehlUebergabeArt, eingabe), b[2], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(g.KuehlVorlaufMittelC.HasValue ? k.F(g.KuehlVorlaufMittelC.Value, 1) : "—", b[3], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(g.KuehlRuecklaufMittelC.HasValue ? k.F(g.KuehlRuecklaufMittelC.Value, 1) : "—", b[4], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(Begrenzttext(k, g), b[5], false, null, JustificationValues.Right));
                t.Append(tr);
            }

            k.Ueberschrift2(UEBERSCHRIFT_KUEHLKREIS);
            k.Fuege(t);
            k.Hinweis(HINWEIS_KUEHLKREIS);
            k.Hinweis(HINWEIS_KUEHLKREIS_GRENZE);
            k.HinweisRoh(MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG);
        }

        /// <summary>„Kühldecke, 16/19 °C" — die Kühlübergabeart mit dem Auslegungspunkt (Eingabe, sonst Vorgabe der Art).</summary>
        internal static string Kuehluebergabetext(WordKontext k, string art, DataRow eingabe)
        {
            double? vorlauf = ProjektDetails.D(eingabe, "Kuehl_Auslegung_Vorlauf") ?? Waermeuebergabevorgaben.KuehlVorlauf(art);
            double? ruecklauf = ProjektDetails.D(eingabe, "Kuehl_Auslegung_Ruecklauf") ?? Waermeuebergabevorgaben.KuehlRuecklauf(art);
            string name = Waermeuebergabevorgaben.KuehlAnzeigename(art);
            return vorlauf.HasValue && ruecklauf.HasValue
                ? name + ", " + k.F(vorlauf.Value, 0) + "/" + k.F(ruecklauf.Value, 0) + " °C"
                : name;
        }

        /// <summary>Die Vorlaufgrenze (Eingabe, sonst Vorgabe der Art); „keine" ohne Grenze.</summary>
        internal static string Vorlaufgrenzetext(WordKontext k, string art, DataRow eingabe)
        {
            double? grenze = ProjektDetails.D(eingabe, "Kuehl_Vorlaufgrenze") ?? Waermeuebergabevorgaben.KuehlVorlaufgrenze(art);
            return grenze.HasValue ? k.F(grenze.Value, 0) : BerichtTexte.T("keine");
        }

        /// <summary>„12 (davon 3 an der Vorlaufgrenze)" — die begrenzten Stunden samt Anteil an der Grenze.</summary>
        private static string Begrenzttext(WordKontext k, ErgebnisGebaeudeModel g)
        {
            if (!g.KuehlUebergabeBegrenztStundenH.HasValue) return "—";
            string text = k.F(g.KuehlUebergabeBegrenztStundenH.Value, 0);
            return g.KuehlVorlaufgrenzeStundenH is double grenze && grenze > 0
                ? text + " (" + BerichtTexte.T("davon an der Vorlaufgrenze") + " " + k.F(grenze, 0) + ")"
                : text;
        }

        /// <summary>Die Eingabezeile eines Gebäudes aus den Projektdetails (<c>Tab_Gebaeude</c>); <c>null</c> ohne.</summary>
        private static DataRow Gebaeudeeingabe(VariantenDaten v, int idGebaeude)
        {
            DataTable dt = v?.Details?.Gebaeude;
            if (dt == null || !dt.Columns.Contains("ID")) return null;
            foreach (DataRow r in dt.Rows)
                if ((int)(ProjektDetails.D(r, "ID") ?? 0) == idGebaeude) return r;
            return null;
        }

        /// <summary>„Radiator, 55/45 °C" — die Übergabeart mit dem Auslegungspunkt (Eingabe, sonst Vorgabe der Art).</summary>
        internal static string Uebergabetext(WordKontext k, string art, DataRow eingabe)
        {
            double? vorlauf = ProjektDetails.D(eingabe, "Auslegung_Vorlauf") ?? Waermeuebergabevorgaben.Vorlauf(art);
            double? ruecklauf = ProjektDetails.D(eingabe, "Auslegung_Ruecklauf") ?? Waermeuebergabevorgaben.Ruecklauf(art);
            string name = Waermeuebergabevorgaben.Anzeigename(art);
            return vorlauf.HasValue && ruecklauf.HasValue
                ? name + ", " + k.F(vorlauf.Value, 0) + "/" + k.F(ruecklauf.Value, 0) + " °C"
                : name;
        }

        /// <summary>„Niveau 0 K, Steilheit 1,00" mit Heizkurve (leere Felder = Vorgabe), sonst „fester Vorlauf".</summary>
        internal static string Heizkurventext(WordKontext k, DataRow eingabe)
        {
            if (eingabe == null) return "—";
            if (ProjektDetails.B(eingabe, "Heizkurve_Aktiv") != true) return BerichtTexte.T("fester Vorlauf");
            double niveau = ProjektDetails.D(eingabe, "Heizkurve_Niveau") ?? Waermeuebergabevorgaben.HeizkurveNiveau;
            double steilheit = ProjektDetails.D(eingabe, "Heizkurve_Steilheit") ?? Waermeuebergabevorgaben.HeizkurveSteilheit;
            return BerichtTexte.T("Niveau") + " " + k.F(niveau, 1) + " K, " + BerichtTexte.T("Steilheit") + " " + k.F(steilheit, 2);
        }

        private static string Wert(WordKontext k, double? w, int dez, string einheit)
        { return w.HasValue ? k.F(w.Value, dez) + " " + einheit : "—"; }

        private static string Wert(WordKontext k, int? w, string einheit)
        { return w.HasValue ? k.F(w.Value, 0) + " " + einheit : "—"; }

        /// <summary>
        /// PAKET P2 — Temperaturen der obersten Schicht je Speicher, dazu die Ganglinie
        /// (Konzept 7.4, offener Punkt P1-O5).
        ///
        /// <para><b>Der Abschnitt entfällt vollständig, wenn kein Speicher einen Wert
        /// trägt</b> — also bei jedem Projekt ohne Senkenspeicher und bei jedem
        /// Ergebnis, das vor Paket P1 gerechnet wurde. Eine Tabelle voller „—" wäre
        /// keine Aussage, sondern eine Frage.</para>
        ///
        /// <para>Die Kennzahl <c>T_oben_Mittel</c>/<c>_Min</c> steht in
        /// <c>Tab_ErgebnisPufferspeicher</c> (Schritt 52, gefüllt seit P1); der
        /// <see cref="KennzahlenKatalog"/> führt daneben die beiden Projektwerte
        /// (Mittel über die Speicher, kleinstes Minimum) für den Variantenvergleich.
        /// Hier steht die AUFSCHLÜSSELUNG je Speicher — dieselbe Arbeitsteilung wie bei
        /// Bedarf und Deckungsgraden darüber.</para>
        /// </summary>
        private static void SpeichertemperaturenSchreiben(WordKontext k, VariantenDaten stamm)
        {
            if (stamm.Ergebnis == null || stamm.Ergebnis.Pufferspeicher == null) return;

            List<ErgebnisPufferspeicherModel> mitWert = stamm.Ergebnis.Pufferspeicher
                .Where(p => p != null && p.T_oben_Mittel.HasValue).ToList();
            if (mitWert.Count == 0) return;

            k.Ueberschrift2("Speichertemperaturen (Schichtmodell)");

            int[] w = { 4155, 2600, 2600 };
            Table t = k.NeueTabelle(w);

            var kopf = new TableRow();
            kopf.Append(k.Zelle("Speicher", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("T oben Mittel [°C]", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("T oben Minimum [°C]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            t.Append(kopf);

            foreach (ErgebnisPufferspeicherModel p in mitWert)
            {
                var tr = new TableRow();
                tr.Append(k.Zelle(string.IsNullOrWhiteSpace(p.Bezeichner) ? "—" : p.Bezeichner,
                                  w[0], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(k.F(p.T_oben_Mittel.Value, 1), w[1], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(p.T_oben_Min.HasValue ? k.F(p.T_oben_Min.Value, 1) : "—",
                                  w[2], false, null,
                                  p.T_oben_Min.HasValue ? JustificationValues.Right : JustificationValues.Center));
                t.Append(tr);
            }
            k.Fuege(t);

            // Die Ganglinie entsteht wie die vier Bestandsdiagramme aus dem
            // Zeitreihensatz des Laufs — sie gibt es also nur, wenn für diesen Bericht
            // frisch simuliert wurde. Ein Diagrammfehler kippt den Bericht nicht.
            if (stamm.Zeitreihen == null) return;

            // DG-E3 (Gruppe d): ueber das ZEICHENMODELL — das Bild steht als SVG mit
            // PNG-Rueckfall im Dokument (Entscheid DG-E3-8).
            Zeichnung.Zeichenmodell bild;
            try { bild = ChartRenderer.SpeichertemperaturenModell(stamm.Zeitreihen); }
            catch { bild = null; }

            if (bild != null)
            {
                k.Bild(bild, 620, 280);
                k.Beschriftung("Speichertemperaturen in charakteristischen Wochen (Winter/Übergang/Sommer)");
            }
        }

        private static string Oder(string a, string b) { return string.IsNullOrWhiteSpace(a) ? b : a; }

        /// <summary>PAKET E1: Bedarf eines Kanals [MWh/a]; „—", wenn die Zeile ihn nicht führt.</summary>
        private static string KanalWert(WordKontext k, ErgebnisEnergiebedarfModel e, int kanal)
        {
            if (e.Waermebedarf_Kanal == null || kanal >= e.Waermebedarf_Kanal.Length) return "—";
            return k.F(e.Waermebedarf_Kanal[kanal], 0) + " MWh/a";
        }

        /// <summary>
        /// PAKET E1: Deckungsgrad eines Kanals [%] aus dem Kennzahlen-Dictionary der
        /// Variante — die Umrechnung auf den Kanalbedarf steht EINMAL im
        /// <see cref="KennzahlenKatalog"/> und wird hier nur abgeholt, nicht nachgebaut.
        /// </summary>
        private static string DeckungWert(WordKontext k, VariantenDaten v, string schluessel)
        {
            double? d;
            if (v.Kennzahlen == null || !v.Kennzahlen.TryGetValue(schluessel, out d) || !d.HasValue)
                return "—";
            return k.F(d.Value, 1) + " %";
        }

        private static string Zahl(WordKontext k, DataRow r, string spalte, string einheit, int dez)
        {
            double? d = ProjektDetails.D(r, spalte);
            if (!d.HasValue) return "—";
            string t = k.F(d.Value, dez);
            return einheit.Length == 0 ? t : t + " " + einheit;
        }
    }

    /// <summary>
    /// Baustein 4: Komponenten & Varianten — Matrix, Kenndaten je Gewerk,
    /// Abweichungstabellen je Variante (Konzept Kap. 4/4.3; Blocksplitting Kap. 5.1).
    /// </summary>
    public class KomponentenBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_KOMPONENTEN; } }
        public string Titel { get { return "Komponenten & Varianten"; } }

        /// <summary>Die Überschrift des Kapitels (Überschrift 1) — Schlüssel der Übersetzung in
        /// <see cref="BerichtTexte"/>; dieselbe Quelle hat <see cref="Berichtskapitel.Ueberschrift"/>.</summary>
        public const string UEBERSCHRIFT = "Komponenten & Varianten";

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;

            k.Ueberschrift1(UEBERSCHRIFT);

            // ---------------- Matrix Komponenten × Varianten ----------------
            k.Ueberschrift2("Komponentenübersicht");
            foreach (List<VariantenDaten> block in k.VariantenBloecke(daten))
            {
                var spalten = new List<VariantenDaten> { stamm };
                spalten.AddRange(block);

                int wLabel = 2600;
                int wCol = (k.Inhaltsbreite - wLabel) / Math.Max(spalten.Count, 1);
                var w = new List<int> { wLabel };
                for (int i = 0; i < spalten.Count; i++) w.Add(wCol);

                Table t = k.NeueTabelle(w.ToArray());
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Gewerk", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                for (int i = 0; i < spalten.Count; i++)
                    kopf.Append(k.Zelle(spalten[i].IstStamm ? "Stamm" : spalten[i].Anzeige,
                        w[i + 1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                {
                    var tr = new TableRow();
                    tr.Append(k.Zelle(g.Key, w[0], false, null, JustificationValues.Left));
                    for (int i = 0; i < spalten.Count; i++)
                    {
                        ProjektDetails d = spalten[i].Details;
                        int n = (d != null && d.KomponentenAnzahl.ContainsKey(g.Key)) ? d.KomponentenAnzahl[g.Key] : 0;
                        string zelle = n == 0 ? "—" : (n == 1 ? "✓" : "✓ (" + n + ")");
                        tr.Append(k.Zelle(zelle, w[i + 1], false,
                            spalten[i].IstStamm ? WordBerichtGenerator.STAMM_FILL : null, JustificationValues.Center));
                    }
                    t.Append(tr);
                }
                k.Fuege(t);
                k.Abstand();
            }

            // ---------------- Kenndaten je Gewerk (deklarative Feldliste) ----------------
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                // Gewerk in mindestens einem Projekt vorhanden?
                bool vorhanden = daten.Varianten.Any(v => v.Details != null && v.Details.HatGewerk(g.Key));
                if (!vorhanden) continue;

                var merkmale = AbweichungsErmittler.Felder.Where(f => f.Tabelle == g.Value).ToList();
                if (merkmale.Count == 0) continue;

                k.Ueberschrift2(g.Key);
                foreach (List<VariantenDaten> block in k.VariantenBloecke(daten))
                {
                    var spalten = new List<VariantenDaten> { stamm };
                    spalten.AddRange(block);

                    int wLabel = 2600;
                    int wCol = (k.Inhaltsbreite - wLabel) / Math.Max(spalten.Count, 1);
                    var w = new List<int> { wLabel };
                    for (int i = 0; i < spalten.Count; i++) w.Add(wCol);

                    Table t = k.NeueTabelle(w.ToArray());
                    var kopf = new TableRow();
                    kopf.Append(k.Zelle("Merkmal", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                    for (int i = 0; i < spalten.Count; i++)
                        kopf.Append(k.Zelle(spalten[i].IstStamm ? "Stamm" : spalten[i].Anzeige,
                            w[i + 1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                    t.Append(kopf);

                    foreach (AbweichungsErmittler.Merkmal f in merkmale)
                    {
                        var tr = new TableRow();
                        tr.Append(k.Zelle(f.Label, w[0], false, null, JustificationValues.Left));
                        for (int i = 0; i < spalten.Count; i++)
                        {
                            ProjektDetails d = spalten[i].Details;
                            DataRow zeile = (d != null && d.Komponenten.ContainsKey(g.Key)) ? d.Komponenten[g.Key] : null;
                            string wert = zeile == null ? "—" : AbweichungsErmittler.Formatiere(zeile, f);
                            tr.Append(k.Zelle(wert, w[i + 1], false,
                                spalten[i].IstStamm ? WordBerichtGenerator.STAMM_FILL : null,
                                wert == "—" ? JustificationValues.Center : JustificationValues.Right));
                        }
                        t.Append(tr);
                    }
                    k.Fuege(t);
                    k.Abstand();
                }
            }

            // ---------------- Abweichungen je Variante ----------------
            var variantenMitDaten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            if (variantenMitDaten.Count > 0)
            {
                k.Ueberschrift2("Abweichungen der Varianten gegenüber dem Stamm");
                foreach (VariantenDaten v in variantenMitDaten)
                {
                    k.Ueberschrift3(v.Anzeige);
                    if (v.Abweichungen == null || v.Abweichungen.Count == 0)
                    {
                        k.Text("Keine Abweichungen in der verglichenen Anlagen- und Gebäudekonfiguration.");
                        continue;
                    }

                    int[] w = { 1900, 2600, 2400, 2455 };
                    Table t = k.NeueTabelle(w);
                    var kopf = new TableRow();
                    kopf.Append(k.Zelle("Gewerk", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                    kopf.Append(k.Zelle("Merkmal", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                    kopf.Append(k.Zelle("Stamm", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                    kopf.Append(k.Zelle(v.Anzeige, w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                    t.Append(kopf);
                    foreach (Abweichung a in v.Abweichungen)
                    {
                        var tr = new TableRow();
                        tr.Append(k.Zelle(a.Gewerk, w[0], false, null, JustificationValues.Left));
                        tr.Append(k.Zelle(a.Merkmal, w[1], false, null, JustificationValues.Left));
                        tr.Append(k.Zelle(a.WertStamm, w[2], false, WordBerichtGenerator.STAMM_FILL, JustificationValues.Center));
                        tr.Append(k.Zelle(a.WertVariante, w[3], false, null, JustificationValues.Center));
                        t.Append(tr);
                    }
                    k.Fuege(t);
                }
            }
        }
    }
}
