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

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;

            k.Ueberschrift1("Projektbeschreibung");

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
                        "Baualtersklasse", Oder(ProjektDetails.S(g, "Baualtersklasse"), "—"),
                        "Wohn-/Nutzfläche", Zahl(k, g, "Wohnflaeche_gesamt", "m²", 0),
                        "Bewohner/Nutzer", Zahl(k, g, "Bewohner", "", 0),
                        "Wärmebedarf", Zahl(k, g, "Waermebedarf", "kWh/a", 0),
                        "spez. Wärmeverbrauch", Zahl(k, g, "spez_Waermeverbrauch", "kWh/m²a", 1),
                        "Warmwasserbedarf", Zahl(k, g, "WW_Bedarf", "kWh/a", 0),
                        "Raumhöhe", Zahl(k, g, "Raumhoehe", "m", 2));
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
            }

            // PAKET P2 (Konzept 7.4): die Speichertemperaturen des Schichtmodells.
            SpeichertemperaturenSchreiben(k, stamm);
        }

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

            byte[] png;
            try { png = ChartRenderer.Speichertemperaturen(stamm.Zeitreihen); }
            catch { png = null; }

            if (png != null)
            {
                k.Bild(png, 620, 280);
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

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;

            k.Ueberschrift1("Komponenten & Varianten");

            // ---------------- Matrix Komponenten × Varianten ----------------
            k.Ueberschrift2("Komponentenübersicht");
            foreach (List<VariantenDaten> block in k.VariantenBloecke(daten))
            {
                var spalten = new List<VariantenDaten> { stamm };
                spalten.AddRange(block);

                int wLabel = 2600;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / Math.Max(spalten.Count, 1);
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
                k.Beschriftung(" ");
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
                    int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / Math.Max(spalten.Count, 1);
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
                    k.Beschriftung(" ");
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
