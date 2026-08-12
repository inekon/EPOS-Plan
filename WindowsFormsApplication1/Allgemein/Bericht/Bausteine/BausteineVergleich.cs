using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Baustein 5: Berechnungsergebnisse je Variante (Phase-2-Basis; die vier
    /// Ganglinien-Diagrammtypen folgen in Phase 3 über den ChartRenderer).
    /// </summary>
    public class ErgebnisseBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_ERGEBNISSE; } }
        public string Titel { get { return "Ergebnisse je Variante"; } }

        // Kernkennzahlen des Variantenkapitels (Katalogschlüssel in Anzeigereihenfolge).
        private static readonly string[] KERN =
        {
            "energie.waermebedarf", "energie.strombedarf",
            "energie.wp_waerme", "energie.bhkw_waerme", "energie.kessel_waerme",
            "energie.solar_waerme", "energie.bhkw_strom", "energie.pv_strom",
            "energie.brennstoff", "energie.netzbezug", "energie.einspeisung",
            "energie.waermerest", "eff.jaz", "eff.autarkie"
        };

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1("Berechnungsergebnisse je Variante");
            List<Kennzahl> katalog = KennzahlenKatalog.Alle();

            foreach (VariantenDaten v in daten.Varianten)
            {
                k.Ueberschrift2((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);
                k.Hinweis("Simulationsstand: " + (v.SimulationsStand.HasValue
                    ? v.SimulationsStand.Value.ToString("dd.MM.yyyy HH:mm", k.Kultur) : "—") +
                    (v.FrischSimuliert ? " (für diesen Bericht neu gerechnet)" : ""));

                if (v.Fehler != null)
                { k.Text("Für dieses Projekt konnten keine Ergebnisse geladen werden: " + v.Fehler); continue; }

                var paare = new List<string>();
                foreach (string schluessel in KERN)
                {
                    Kennzahl kz = katalog.FirstOrDefault(x => x.Schluessel == schluessel);
                    if (kz == null) continue;
                    double? wert = v.Kennzahlen.ContainsKey(schluessel) ? v.Kennzahlen[schluessel] : null;
                    if (!wert.HasValue) continue;   // fehlende Gewerke nicht als Leerzeilen führen
                    paare.Add(kz.Label(BerichtTexte.Englisch));
                    paare.Add(k.FW(wert, kz.Format) + (kz.Einheit == "–" ? "" : " " + kz.Einheit));
                }
                if (paare.Count > 0) k.Eigenschaften(paare.ToArray());
                else k.Text("Keine Kennzahlen verfügbar.");

                // Die vier Ganglinientypen aus der In-Memory-Simulation (Konzept Kap. 6.2).
                if (v.Zeitreihen != null)
                    ZeichneGanglinien(k, v.Zeitreihen);
                else
                    k.Hinweis("Ganglinien nicht verfügbar — sie entstehen nur, wenn für den Bericht " +
                              "frisch simuliert wurde (Baustein „Ergebnisse je Variante“ aktiv).");
            }
        }

        private static void ZeichneGanglinien(WordKontext k, ZeitreihenSatz z)
        {
            byte[] png = Sicher(() => ChartRenderer.JahresverlaufWaerme(z));
            if (png != null)
            {
                k.Bild(png, 620, 280);
                k.Beschriftung("Wärmeerzeugung im Jahresverlauf (gestapelte Erzeuger, Bedarf als Linie, Tagesmittel)");
            }
            png = Sicher(() => ChartRenderer.DauerlinieWaerme(z));
            if (png != null)
            {
                k.Bild(png, 620, 280);
                k.Beschriftung("Jahresdauerlinie Wärme (geordnete Bedarfs- und Erzeugerdauerlinien)");
            }
            png = Sicher(() => ChartRenderer.StrombilanzMonate(z));
            if (png != null)
            {
                k.Bild(png, 620, 280);
                k.Beschriftung("Strombilanz im Monatsverlauf (Deckung gestapelt, Einspeisung separat, Bedarf als Linie)");
            }
            png = Sicher(() => ChartRenderer.Speicherverlauf(z));
            if (png != null)
            {
                k.Bild(png, 620, 260);
                k.Beschriftung("Speicherverlauf in charakteristischen Wochen (Winter/Übergang/Sommer)");
            }
        }

        private static byte[] Sicher(Func<byte[]> f)
        {
            try { return f(); } catch { return null; }   // ein Diagrammfehler kippt nicht den Bericht
        }
    }

    /// <summary>
    /// Baustein 6: Variantenvergleich — Kennzahlentabellen je Gruppe mit Blocksplitting,
    /// kompakte Delta-Tabelle, Erzeuger-Einzellisten und Brennstoffmengen (Konzept Kap. 4–5).
    /// Balkendiagramme folgen in Phase 3.
    /// </summary>
    public class VergleichBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_VERGLEICH; } }
        public string Titel { get { return "Variantenvergleich"; } }

        // Schlüsselkennzahlen der kompakten Delta-Tabelle.
        private static readonly string[] DELTA_KEYS =
        {
            "energie.waermebedarf", "energie.brennstoff", "energie.netzbezug",
            "energie.waermerest", "eff.jaz", "eff.autarkie"
        };

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;
            List<VariantenDaten> varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            List<Kennzahl> katalog = KennzahlenKatalog.Alle();

            k.Ueberschrift1("Variantenvergleich");
            if (varianten.Count == 0)
                k.Hinweis("Es wurden keine Varianten ausgewählt — die Tabellen zeigen nur das Stammprojekt.");

            // ---------------- Kennzahlentabellen je Gruppe ----------------
            foreach (string gruppe in new[] { KennzahlenKatalog.GR_ENERGIE, KennzahlenKatalog.GR_EFFIZIENZ,
                                              KennzahlenKatalog.GR_EMISSION, KennzahlenKatalog.GR_KOSTEN })
            {
                var zeilen = katalog.Where(x => x.Gruppe == gruppe)
                    .Where(x => daten.Varianten.Any(v =>
                        v.Kennzahlen.ContainsKey(x.Schluessel) && v.Kennzahlen[x.Schluessel].HasValue))
                    .ToList();
                if (zeilen.Count == 0) continue;   // Gruppe ohne verfügbare Werte (z. B. Kosten bis Phase 5)

                k.Ueberschrift2(gruppe);
                SchreibeGruppe(k, daten, stamm, varianten, zeilen);
            }

            // ---------------- kompakte Delta-Tabelle ----------------
            if (varianten.Count >= 2)
            {
                k.Ueberschrift2("Abweichung zum Stamm (Schlüsselkennzahlen, in %)");
                SchreibeDeltaTabelle(k, stamm, varianten, katalog);
            }

            // ---------------- Balkendiagramme je Schlüsselkennzahl (Konzept Kap. 6.1) ----------------
            if (daten.Varianten.Count >= 2)
            {
                k.Ueberschrift2("Kennzahlen im Vergleich (Diagramme)");
                foreach (string schluessel in new[] { "energie.brennstoff", "energie.netzbezug",
                                                      "energie.waermerest", "eff.jaz" })
                {
                    Kennzahl kz = katalog.FirstOrDefault(x => x.Schluessel == schluessel);
                    if (kz == null) continue;
                    var balken = new List<ChartRenderer.Balken>();
                    foreach (VariantenDaten v in daten.Varianten)
                    {
                        double? wert = Wert(v, schluessel);
                        if (wert.HasValue)
                            balken.Add(new ChartRenderer.Balken(
                                v.IstStamm ? "Stamm" : v.Anzeige, wert.Value, v.IstStamm));
                    }
                    if (balken.Count < 2) continue;
                    byte[] png = SicherB(() => ChartRenderer.BalkenHorizontal(kz.Label(BerichtTexte.Englisch), kz.Einheit, balken));
                    if (png != null)
                    {
                        int hoehe = (150 + balken.Count * 64) / 2;
                        k.Bild(png, 620, hoehe);
                        k.Beschriftung(kz.Label(BerichtTexte.Englisch) + " je Variante (Stamm hervorgehoben)");
                    }
                }
            }

            // ---------------- Deckungsdiagramme je Projekt (aus dem Bestand übernommen) ----------------
            k.Ueberschrift2("Deckungsdiagramme");
            k.Hinweis("Anteile an der Wärme- bzw. Stromdeckung je Projekt (aus den Deckungsgraden " +
                      "der Erzeuger; der Rest ist ungedeckte Wärme bzw. Netzbezug).");
            foreach (VariantenDaten v in daten.Varianten)
            {
                ErgebnisModel m = v.Ergebnis;
                if (m == null) continue;
                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                var segW = new List<ChartRenderer.Segment>();
                double sumW = 0;
                if (m.Waermepumpe != null && m.Waermepumpe.Waermebedarfsdeckung > 0)
                { segW.Add(new ChartRenderer.Segment("Wärmepumpe", m.Waermepumpe.Waermebedarfsdeckung, ChartRenderer.C_WP)); sumW += m.Waermepumpe.Waermebedarfsdeckung; }
                if (m.BHKW != null && m.BHKW.Waermebedarfsdeckung > 0)
                { segW.Add(new ChartRenderer.Segment("BHKW", m.BHKW.Waermebedarfsdeckung, ChartRenderer.C_BHKW)); sumW += m.BHKW.Waermebedarfsdeckung; }
                if (m.Heizkessel != null && m.Heizkessel.Waermebedarfsdeckung > 0)
                { segW.Add(new ChartRenderer.Segment("Spitzenkessel", m.Heizkessel.Waermebedarfsdeckung, ChartRenderer.C_KESSEL)); sumW += m.Heizkessel.Waermebedarfsdeckung; }
                if (m.Solarthermie != null && m.Solarthermie.Waermebedarfsdeckung > 0)
                { segW.Add(new ChartRenderer.Segment("Solarthermie", m.Solarthermie.Waermebedarfsdeckung, ChartRenderer.C_SOLAR)); sumW += m.Solarthermie.Waermebedarfsdeckung; }
                if (100.0 - sumW > 0.05) segW.Add(new ChartRenderer.Segment("Rest/ungedeckt", 100.0 - sumW, ChartRenderer.C_REST));

                var segS = new List<ChartRenderer.Segment>();
                double sumS = 0;
                if (m.Photovoltaik != null && m.Photovoltaik.Strombedarfsdeckung > 0)
                { segS.Add(new ChartRenderer.Segment("Photovoltaik", m.Photovoltaik.Strombedarfsdeckung, ChartRenderer.C_PV)); sumS += m.Photovoltaik.Strombedarfsdeckung; }
                if (m.BHKW != null && m.BHKW.Strombedarfsdeckung > 0)
                { segS.Add(new ChartRenderer.Segment("BHKW", m.BHKW.Strombedarfsdeckung, ChartRenderer.C_BHKW)); sumS += m.BHKW.Strombedarfsdeckung; }
                if (100.0 - sumS > 0.05) segS.Add(new ChartRenderer.Segment("Netzbezug", 100.0 - sumS, ChartRenderer.C_KESSEL));

                if (segW.Count > 0)
                {
                    byte[] png = SicherB(() => ChartRenderer.Kuchen("Wärmedeckung", segW));
                    if (png != null) k.Bild(png, 420, 262);
                }
                if (segS.Count > 0)
                {
                    byte[] png = SicherB(() => ChartRenderer.Kuchen("Stromdeckung", segS));
                    if (png != null) k.Bild(png, 420, 262);
                }
            }

            // ---------------- Erzeuger-Einzellisten je Projekt ----------------
            k.Ueberschrift2("Erzeuger — Einzelauflistung je Projekt");
            k.Hinweis("Je Projekt eine Zeile pro Gerät (Modul) mit erzeugter Energie; bei BHKW/Kessel " +
                      "der Brennstoff, bei der Wärmepumpe Strom (inkl. Heizstab).");
            foreach (VariantenDaten v in daten.Varianten)
            {
                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);
                if (v.Ergebnis == null) { k.Text("(kein Ergebnis vorhanden)"); continue; }
                SchreibeErzeugerListe(k, v.Ergebnis);
            }

            // ---------------- Brennstoffmengen je Projekt ----------------
            bool hatMengen = daten.Varianten.Any(v => v.Brennstoffmengen != null && v.Brennstoffmengen.Rows.Count > 0);
            if (hatMengen)
            {
                k.Ueberschrift2("Brennstoffmengen");
                k.Hinweis("Aus dem Brennstoffverbrauch über den projektspezifischen effektiven Heizwert " +
                          "in die Abrechnungseinheit umgerechnete Menge.");
                foreach (VariantenDaten v in daten.Varianten)
                {
                    k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);
                    SchreibeBrennstoffmengen(k, v.Brennstoffmengen);
                }
            }
        }

        // ------------------------------------------------------------- Gruppen-Tabellen

        private static void SchreibeGruppe(WordKontext k, BerichtsDaten daten, VariantenDaten stamm,
                                           List<VariantenDaten> varianten, List<Kennzahl> zeilen)
        {
            bool mitDelta = varianten.Count == 1;   // Δ-Spalte nur bei genau einer Variante (Kap. 5.1)

            foreach (List<VariantenDaten> block in k.VariantenBloecke(daten))
            {
                var spalten = new List<VariantenDaten> { stamm };
                spalten.AddRange(block);
                int extra = mitDelta ? 1 : 0;

                int wLabel = 3100;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / (spalten.Count + extra);
                var w = new List<int> { wLabel };
                for (int i = 0; i < spalten.Count + extra; i++) w.Add(wCol);

                Table t = k.NeueTabelle(w.ToArray());
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Kennzahl (Einheit)", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                for (int i = 0; i < spalten.Count; i++)
                    kopf.Append(k.Zelle(spalten[i].IstStamm ? "Stamm" : spalten[i].Anzeige,
                        w[i + 1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                if (mitDelta)
                    kopf.Append(k.Zelle("Δ (Var. − Stamm)", w[w.Count - 1], true,
                        WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                foreach (Kennzahl kz in zeilen)
                {
                    var tr = new TableRow();
                    string label = kz.Label(BerichtTexte.Englisch) + (kz.Einheit == "–" || kz.Einheit.Length == 0 ? "" : " [" + kz.Einheit + "]");
                    tr.Append(k.Zelle(label, w[0], false, null, JustificationValues.Left));

                    for (int i = 0; i < spalten.Count; i++)
                    {
                        double? wert = Wert(spalten[i], kz.Schluessel);
                        string txt = k.FW(wert, kz.Format);
                        tr.Append(k.Zelle(txt, w[i + 1], false,
                            spalten[i].IstStamm ? WordBerichtGenerator.STAMM_FILL : null,
                            txt == "—" ? JustificationValues.Center : JustificationValues.Right));
                    }
                    if (mitDelta)
                    {
                        string d = kz.DeltaAnzeigen
                            ? k.Delta(Wert(stamm, kz.Schluessel), Wert(varianten[0], kz.Schluessel), kz.Format)
                            : "—";
                        tr.Append(k.Zelle(d, w[w.Count - 1], false, null,
                            d == "—" ? JustificationValues.Center : JustificationValues.Right));
                    }
                    t.Append(tr);
                }
                k.Fuege(t);
                k.Beschriftung(" ");
            }
        }

        private static void SchreibeDeltaTabelle(WordKontext k, VariantenDaten stamm,
                                                 List<VariantenDaten> varianten, List<Kennzahl> katalog)
        {
            var keys = DELTA_KEYS.Where(s =>
                Wert(stamm, s).HasValue && katalog.Any(x => x.Schluessel == s)).ToList();
            if (keys.Count == 0) return;

            int wLabel = 2600;
            int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / keys.Count;
            var w = new List<int> { wLabel };
            for (int i = 0; i < keys.Count; i++) w.Add(wCol);

            Table t = k.NeueTabelle(w.ToArray());
            var kopf = new TableRow();
            kopf.Append(k.Zelle("Variante", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            for (int i = 0; i < keys.Count; i++)
            {
                Kennzahl kz = katalog.First(x => x.Schluessel == keys[i]);
                kopf.Append(k.Zelle(kz.Label(BerichtTexte.Englisch), w[i + 1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            }
            t.Append(kopf);

            foreach (VariantenDaten v in varianten)
            {
                var tr = new TableRow();
                tr.Append(k.Zelle(v.Anzeige, w[0], false, null, JustificationValues.Left));
                for (int i = 0; i < keys.Count; i++)
                {
                    string d = k.DeltaProzent(Wert(stamm, keys[i]), Wert(v, keys[i]));
                    tr.Append(k.Zelle(d, w[i + 1], false, null,
                        d == "—" ? JustificationValues.Center : JustificationValues.Right));
                }
                t.Append(tr);
            }
            k.Fuege(t);
        }

        private static double? Wert(VariantenDaten v, string schluessel)
        { return v.Kennzahlen.ContainsKey(schluessel) ? v.Kennzahlen[schluessel] : null; }

        private static byte[] SicherB(Func<byte[]> f)
        {
            try { return f(); } catch { return null; }   // ein Diagrammfehler kippt nicht den Bericht
        }

        // ------------------------------------------------------------- Einzellisten

        // Kompakte Fassung der Erzeuger-Einzelliste aus dem Bestandsbericht
        // (ProjektvergleichBericht.ErzeugerEinzelTabelle).
        private static void SchreibeErzeugerListe(WordKontext k, ErgebnisModel m)
        {
            int[] w = { 2900, 1500, 1500, 1800, 1655 };
            Table t = k.NeueTabelle(w);
            var kopf = new TableRow();
            kopf.Append(k.Zelle("Erzeuger", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("Wärme [MWh/a]", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("Strom [MWh/a]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("Energieträger", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("Verbrauch [MWh/a]", w[4], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            t.Append(kopf);

            Action<string, string, string, string, string> zeile = (name, waerme, strom, traeger, verbrauch) =>
            {
                var tr = new TableRow();
                tr.Append(k.Zelle(name, w[0], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(waerme, w[1], false, null, waerme == "—" ? JustificationValues.Center : JustificationValues.Right));
                tr.Append(k.Zelle(strom, w[2], false, null, strom == "—" ? JustificationValues.Center : JustificationValues.Right));
                tr.Append(k.Zelle(traeger, w[3], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(verbrauch, w[4], false, null, verbrauch == "—" ? JustificationValues.Center : JustificationValues.Right));
                t.Append(tr);
            };

            if (m.Waermepumpe != null)
            {
                var mods = m.Waermepumpe.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisWaermepumpeModulModel mo in mods)
                        zeile(Name(mo.Modul, "Wärmepumpe"), k.F(mo.Waermeproduktion, 0), "—",
                              "Strom", k.F(mo.Stromverbrauch + mo.Heizstab, 0));
                else
                    zeile("Wärmepumpe", k.F(m.Waermepumpe.Waermeproduktion_WP, 0), "—", "Strom",
                          k.F(m.Waermepumpe.Stromverbrauch_WP + m.Waermepumpe.Stromverbrauch_Heizstab, 0));
            }
            if (m.BHKW != null)
            {
                var mods = m.BHKW.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisBHKWModulModel mo in mods)
                        zeile(Name(mo.Modul, "BHKW"), k.F(mo.Waermeproduktion, 0), k.F(mo.Stromproduktion, 0),
                              LeerStrich(mo.Brennstoff), mo.Verbrauch > 0 ? k.F(mo.Verbrauch, 0) : "—");
                else
                    zeile("BHKW", k.F(m.BHKW.Waermeproduktion, 0), k.F(m.BHKW.Stromproduktion, 0), "—", "—");
            }
            if (m.Heizkessel != null)
            {
                var mods = m.Heizkessel.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisHeizkesselModulModel mo in mods)
                        zeile(Name(mo.Modul, "Spitzenkessel"),
                              k.F(mo.Waermeproduktion > 0 ? mo.Waermeproduktion : mo.Waerme_Gas + mo.Waerme_Oel, 0),
                              "—", LeerStrich(mo.Brennstoff), mo.Verbrauch > 0 ? k.F(mo.Verbrauch, 0) : "—");
                else
                    zeile("Spitzenkessel", k.F(m.Heizkessel.Waermeproduktion, 0), "—", "—", "—");
            }
            if (m.Solarthermie != null)
            {
                var mods = m.Solarthermie.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisSolarthermieModulModel mo in mods)
                        zeile(Name(mo.Modul, "Solarthermie"), k.F(mo.Waermeproduktion, 0), "—", "—", "—");
                else
                    zeile("Solarthermie", k.F(m.Solarthermie.Waermeproduktion, 0), "—", "—", "—");
            }
            if (m.Photovoltaik != null)
            {
                var mods = m.Photovoltaik.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisPhotovoltaikModulModel mo in mods)
                        zeile(Name(mo.Modul, "Photovoltaik"), "—", k.F(mo.Stromproduktion, 0), "—", "—");
                else
                    zeile("Photovoltaik", "—", k.F(m.Photovoltaik.Stromproduktion, 0), "—", "—");
            }

            k.Fuege(t);
        }

        private static void SchreibeBrennstoffmengen(WordKontext k, DataTable dt)
        {
            int[] w = { 2900, 3800, 2655 };
            Table t = k.NeueTabelle(w);
            var kopf = new TableRow();
            kopf.Append(k.Zelle("Erzeuger", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("Bezeichner", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("Menge", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            t.Append(kopf);

            if (dt == null || dt.Rows.Count == 0)
            {
                var tr = new TableRow();
                tr.Append(k.Zelle("—", w[0], false, null, JustificationValues.Center));
                tr.Append(k.Zelle("(keine Brennstoffdaten)", w[1], false, null, JustificationValues.Left));
                tr.Append(k.Zelle("—", w[2], false, null, JustificationValues.Center));
                t.Append(tr);
            }
            else
            {
                foreach (DataRow r in dt.Rows)
                {
                    string menge = r["Menge"] != DBNull.Value ? r["Menge"].ToString() : "—";
                    var tr = new TableRow();
                    tr.Append(k.Zelle(r["Erzeuger"] != DBNull.Value ? r["Erzeuger"].ToString() : "", w[0], false, null, JustificationValues.Left));
                    tr.Append(k.Zelle(r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString() : "", w[1], false, null, JustificationValues.Left));
                    tr.Append(k.Zelle(menge, w[2], false, null, menge == "—" ? JustificationValues.Center : JustificationValues.Right));
                    t.Append(tr);
                }
            }
            k.Fuege(t);
        }

        private static string Name(string modul, string fallback)
        { return string.IsNullOrWhiteSpace(modul) ? fallback : modul.Trim(); }

        private static string LeerStrich(string s)
        { return string.IsNullOrWhiteSpace(s) ? "—" : s.Trim(); }
    }
}
