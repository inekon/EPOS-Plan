using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Modellfabrik der Berichtsbilder</b> (Konzept Berichtsvorlagen 5.4, 6.5; Etappe BV-E5): die
    /// Zeichenmodelle der dreizehn Bilder des Wortberichts aus den Daten eines Berichtslaufs — dieselben
    /// Aufrufe für den Bausteinweg (die Bausteine des Berichts) und den Vorlagenweg (die Bildplatzhalter
    /// <c>bild.*</c>, <c>stand.bild.*</c>, <c>stamm.bild.*</c>). Wer ein Berichtsbild ändert, ändert es hier.
    ///
    /// <para><b>Nur aus <see cref="BerichtsDaten"/></b> (<c>EPOS.Kern/CLAUDE.md</c>, „Bericht“): Die Fabrik
    /// liest den Baum und den Wertesatz des Laufs, nie die Datenbank. <c>null</c> heißt „kein Bild“ — der
    /// Lauf führt die Reihen oder Werte dieses Bildes nicht; die Bausteine lassen die Stelle dann aus, die
    /// Engine schreibt einen Hinweis.</para>
    ///
    /// <para><b>Das Zielmaß</b> (<see cref="Bildmass"/>, Stufe 2) reicht jede Methode an den
    /// <see cref="ChartRenderer"/> durch; ohne Maß entsteht das Bild des Bausteinwegs, byte-gleich.</para>
    /// </summary>
    public static class Berichtsbilder
    {
        /// <summary>Die Anzeigebreite der breiten Berichtsbilder im Bausteinweg [px bei 96 dpi].</summary>
        public const int ANZEIGE_BREITE = 620;

        /// <summary>Die Anzeigebreite der Deckungskuchen im Bausteinweg [px].</summary>
        public const int ANZEIGE_BREITE_KUCHEN = 420;

        /// <summary>Die Anzeigehöhe der Deckungskuchen im Bausteinweg [px].</summary>
        public const int ANZEIGE_HOEHE_KUCHEN = 262;

        /// <summary>Die Modellbreite der Deckungskuchen [px] — <see cref="ChartRenderer.KuchenModell"/>.</summary>
        public const int MODELL_BREITE_KUCHEN = 960;

        /// <summary>Die vier Schlüsselkennzahlen der Vergleichsbalken (Konzept Kap. 6.1).</summary>
        public static readonly IReadOnlyList<string> Balkenkennzahlen = new[]
        {
            "energie.brennstoff", "energie.netzbezug", "energie.waermerest", "eff.jaz",
        };

        // =====================================================================
        //  Ganglinien je Stand (Zeitreihensatz des Laufs)
        // =====================================================================

        /// <summary>Wärmeerzeugung im Jahresverlauf; <c>null</c> ohne Reihen.</summary>
        public static Zeichenmodell JahresverlaufWaerme(ZeitreihenSatz z, Bildmass? mass = null)
            => z == null ? null : ChartRenderer.JahresverlaufWaermeModell(z, mass);

        /// <summary>Jahresdauerlinie Wärme; <c>null</c> ohne Wärmebedarf.</summary>
        public static Zeichenmodell DauerlinieWaerme(ZeitreihenSatz z, Bildmass? mass = null)
            => z == null ? null : ChartRenderer.DauerlinieWaermeModell(z, mass);

        /// <summary>Strombilanz im Monatsverlauf; <c>null</c> ohne Strombedarf oder Deckung.</summary>
        public static Zeichenmodell StrombilanzMonate(ZeitreihenSatz z, Bildmass? mass = null)
            => z == null ? null : ChartRenderer.StrombilanzMonateModell(z, mass);

        /// <summary>Speicherverlauf in drei Wochen; <c>null</c> ohne Füllstandsreihe.</summary>
        public static Zeichenmodell Speicherverlauf(ZeitreihenSatz z, Bildmass? mass = null)
            => z == null ? null : ChartRenderer.SpeicherverlaufModell(z, mass);

        /// <summary>Speichertemperaturen in drei Wochen; <c>null</c> ohne Temperaturreihe.</summary>
        public static Zeichenmodell Speichertemperaturen(ZeitreihenSatz z, Bildmass? mass = null)
            => z == null ? null : ChartRenderer.SpeichertemperaturenModell(z, mass);

        // =====================================================================
        //  Deckung je Stand (Kuchen aus den Deckungsgraden der Erzeuger)
        // =====================================================================

        /// <summary>
        /// Der Deckungskuchen eines Ergebnisses — Wärme (Wärmepumpe, BHKW, Spitzenkessel, Solarthermie, Rest)
        /// oder Strom (Photovoltaik, BHKW, Netzbezug), aus dem Bestandsbericht übernommen. <c>null</c> ohne
        /// Ergebnis oder ohne einen Anteil.
        /// </summary>
        public static Zeichenmodell Deckung(ErgebnisModel m, bool waerme, Bildmass? mass = null)
        {
            List<ChartRenderer.Segment> segmente = waerme ? Waermedeckung(m) : Stromdeckung(m);
            if (segmente == null || segmente.Count == 0) return null;
            return ChartRenderer.KuchenModell(waerme ? "Wärmedeckung" : "Stromdeckung", segmente, mass);
        }

        /// <summary>Die Segmente der Wärmedeckung; <c>null</c> ohne Ergebnis.</summary>
        public static List<ChartRenderer.Segment> Waermedeckung(ErgebnisModel m)
        {
            if (m == null) return null;
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
            return segW;
        }

        /// <summary>Die Segmente der Stromdeckung; <c>null</c> ohne Ergebnis.</summary>
        public static List<ChartRenderer.Segment> Stromdeckung(ErgebnisModel m)
        {
            if (m == null) return null;
            var segS = new List<ChartRenderer.Segment>();
            double sumS = 0;
            if (m.Photovoltaik != null && m.Photovoltaik.Strombedarfsdeckung > 0)
            { segS.Add(new ChartRenderer.Segment("Photovoltaik", m.Photovoltaik.Strombedarfsdeckung, ChartRenderer.C_PV)); sumS += m.Photovoltaik.Strombedarfsdeckung; }
            if (m.BHKW != null && m.BHKW.Strombedarfsdeckung > 0)
            { segS.Add(new ChartRenderer.Segment("BHKW", m.BHKW.Strombedarfsdeckung, ChartRenderer.C_BHKW)); sumS += m.BHKW.Strombedarfsdeckung; }
            if (100.0 - sumS > 0.05) segS.Add(new ChartRenderer.Segment("Netzbezug", 100.0 - sumS, ChartRenderer.C_KESSEL));
            return segS;
        }

        // =====================================================================
        //  Vergleich über die Stände
        // =====================================================================

        /// <summary>
        /// Die Balken einer Schlüsselkennzahl je Stand (Stamm hervorgehoben) — die Stände mit Wert, in
        /// Berichtsfolge; weniger als zwei ergeben kein Bild.
        /// </summary>
        public static List<ChartRenderer.Balken> Vergleichsbalken(IEnumerable<VariantenDaten> staende, string schluessel)
        {
            var balken = new List<ChartRenderer.Balken>();
            foreach (VariantenDaten v in staende ?? Enumerable.Empty<VariantenDaten>())
            {
                double? wert = v.Kennzahlen != null && v.Kennzahlen.ContainsKey(schluessel) ? v.Kennzahlen[schluessel] : null;
                if (wert.HasValue)
                    balken.Add(new ChartRenderer.Balken(v.IstStamm ? "Stamm" : v.Anzeige, wert.Value, v.IstStamm));
            }
            return balken;
        }

        /// <summary>
        /// Das Balkenbild einer Schlüsselkennzahl über die Stände; <c>null</c> mit weniger als zwei Werten
        /// oder ohne die Kennzahl im Katalog. Die Anzeigehöhe im Bausteinweg ist
        /// <c>(150 + Balken · 64) / 2</c>.
        /// </summary>
        public static Zeichenmodell Vergleich(IEnumerable<VariantenDaten> staende, IEnumerable<Kennzahl> katalog,
                                              string schluessel, bool englisch, Bildmass? mass = null)
        {
            Kennzahl kz = katalog?.FirstOrDefault(x => x.Schluessel == schluessel);
            if (kz == null) return null;
            List<ChartRenderer.Balken> balken = Vergleichsbalken(staende, schluessel);
            if (balken.Count < 2) return null;
            return ChartRenderer.BalkenHorizontalModell(kz.Label(englisch), kz.Einheit, balken, mass);
        }

        // =====================================================================
        //  Wirtschaftlichkeit (Verlauf, Brücke, Spanne, Zahlungsstrom)
        // =====================================================================

        /// <summary>
        /// Das DREIERBILD: der kumulierte Barwert der Differenz zur Referenz in allen drei Szenarien.
        /// <c>null</c> ohne Verlauf, ohne Erwartungsfall oder wenn der Verlauf leer ist.
        /// </summary>
        public static Zeichenmodell KapitalwertSzenarien(WirtschaftlichkeitVerlaufSzenarien verlauf, Bildmass? mass = null)
        {
            if (!HatErwartung(verlauf) || verlauf.Leer) return null;
            ChartRenderer.VerlaufSzenarienTexte texte = ChartRenderer.VerlaufSzenarienTexte.AusRessourcen();
            return ChartRenderer.KapitalwertSzenarienModell(
                MyResource.Resource.WIRT_VERL_BILD,
                ChartRenderer.VerlaufsReihenSzenarien(verlauf, texte), texte,
                MyResource.Resource.WIRT_VERL_FUSS, mass);
        }

        /// <summary>
        /// Die kumulierten Barwerte je Version im Erwartungsfall (die Stammlinie gestrichelt).
        /// <c>null</c> ohne Verlauf oder Erwartungsfall.
        /// </summary>
        public static Zeichenmodell BarwerteKumuliert(WirtschaftlichkeitVerlaufSzenarien verlauf, Bildmass? mass = null)
        {
            if (!HatErwartung(verlauf)) return null;
            WirtschaftlichkeitVerlauf erwartet = verlauf.Lauf(WirtschaftlichkeitSzenario.ERWARTET);
            return ChartRenderer.KapitalwertVerlaufModell(
                "Kumulierte Barwerte je Version",
                ChartRenderer.VerlaufsReihen(erwartet.Absolut, true, true), null, mass);
        }

        /// <summary>Trägt der Verlauf einen Erwartungsfall mit wenigstens einer kumulierten Reihe?</summary>
        public static bool HatErwartung(WirtschaftlichkeitVerlaufSzenarien verlauf)
        {
            WirtschaftlichkeitVerlauf erwartet = verlauf?.Lauf(WirtschaftlichkeitSzenario.ERWARTET);
            return erwartet != null && !erwartet.Absolut.All(s => s.Kumuliert == null);
        }

        /// <summary>
        /// Das BRÜCKENBILD: die Leitversion gegen die Referenz im Erwartungsfall, je Bestandteil der Beitrag
        /// zur Kapitalwertdifferenz. <c>null</c> ohne Verlauf, Parameter, Bandbreite, Leitversion oder passende
        /// Gliederung — und wenn die Leitversion die Referenz ist.
        /// </summary>
        public static Zeichenmodell Bruecke(BerichtsDaten daten, WirtschaftlichkeitVerlaufSzenarien verlauf,
                                            List<WirtschaftlichkeitErgebnis> alle, WirtschaftlichkeitParameter p,
                                            WirtschaftlichkeitBewertung bewertung, CultureInfo kultur,
                                            Bildmass? mass = null)
        {
            if (daten == null || verlauf == null || p == null || bewertung == null || bewertung.Bandbreite == null) return null;
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(verlauf, p, alle);
            int idReferenz = bewertung.Bandbreite.IdReferenz;
            int leit = Zahlungsgliederungen.Leitversion(alle, daten.Varianten.Select(v => v.IdProjekt), idReferenz);
            string erwartet = WirtschaftlichkeitSzenario.ERWARTET;
            Zahlungsgliederung stand = satz.Von(leit, erwartet), referenz = satz.Von(idReferenz, erwartet);
            if (leit == 0 || leit == idReferenz || stand == null || referenz == null) return null;

            VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == leit);
            string name = v == null ? "" : (v.IstStamm ? "Stamm" : v.Anzeige);
            ChartRenderer.BrueckenTexte texte = ChartRenderer.BrueckenTexte.Fuer(
                name, bewertung.Bandbreite.Referenzname, MyResource.Resource.WIRT_SZEN_ERWARTET, stand, kultur);
            return ChartRenderer.KapitalwertBrueckeModell(ChartRenderer.Brueckenschritt.Aus(stand, referenz), texte, mass);
        }

        /// <summary>
        /// Das SPANNENBILD: die Bandbreite je Version als Balken vom kleinsten bis zum größten Szenariowert.
        /// <c>null</c> ohne Bandbreite oder mit einer leeren (keine Variante gewählt).
        /// </summary>
        public static Zeichenmodell Spanne(WirtschaftlichkeitBandbreite band, Bildmass? mass = null)
        {
            if (band == null || band.Leer) return null;
            return ChartRenderer.KapitalwertSpanneModell(ChartRenderer.Spannenbalken.Aus(band), band.Referenzname,
                                                         ChartRenderer.SpannenTexte.AusRessourcen(), mass);
        }

        /// <summary>
        /// Das ZAHLUNGSSTROMBILD eines Stands aus seiner Mehrjahrestafel (<see cref="Mehrjahresbild"/>).
        /// <c>null</c> ohne Tafel.
        /// </summary>
        public static Zeichenmodell Zahlungsstrom(Mehrjahresbild bild, string standname, CultureInfo kultur,
                                                  Bildmass? mass = null)
        {
            if (bild == null) return null;
            return ChartRenderer.ZahlungsstromModell(
                ChartRenderer.Zahlungsstromreihe.Aus(bild), ChartRenderer.Zahlungsstromreihe.Ersatzjahre(bild),
                ChartRenderer.ZahlungsstromTexte.Fuer(standname, MyResource.Resource.WIRT_SZEN_ERWARTET, kultur), mass);
        }

        /// <summary>Die Mehrjahrestafel eines Stands im Erwartungsfall des Verlaufs; <c>null</c> ohne.</summary>
        public static Mehrjahresbild Mehrjahrestafel(WirtschaftlichkeitVerlaufSzenarien verlauf, int idProjekt)
        {
            WirtschaftlichkeitVerlauf erwartet = verlauf?.Lauf(WirtschaftlichkeitSzenario.ERWARTET);
            if (erwartet == null || erwartet.Absolut.All(s => s.Bild == null)) return null;
            VerlaufSerie serie = erwartet.Absolut.FirstOrDefault(s => s.IdProjekt == idProjekt);
            return Mehrjahresbild.Baue(serie);
        }

        // =====================================================================
        //  Titel
        // =====================================================================

        /// <summary>
        /// Der Titel, den das Modell zeichnet (Marke <c>titel</c>, der erste Text) — nach dem Füllen der
        /// Alternativtext des Bildes (BV-Q14); doppelte Leerzeichen zusammengezogen. Leer ohne Titel.
        /// </summary>
        public static string Titel(Zeichenmodell modell)
        {
            if (modell == null) return "";
            Zeichnung.Text t = ErsterTitel(modell.Befehle);
            if (t == null) return "";
            string text = (t.Inhalt ?? "").Trim();
            while (text.Contains("  ", StringComparison.Ordinal)) text = text.Replace("  ", " ");
            return text;
        }

        private static Zeichnung.Text ErsterTitel(IReadOnlyList<Zeichenbefehl> befehle)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                if (b is Zeichnung.Text t && t.Marke == "titel") return t;
                if (b is Gruppe g && g.Marke != null && g.Marke != "titel") continue;
                if (b is Gruppe gg)
                {
                    Zeichnung.Text drin = ErsterTitel(gg.Befehle);
                    if (drin != null) return drin;
                }
            }
            return null;
        }
    }
}
