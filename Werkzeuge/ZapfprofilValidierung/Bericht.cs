using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Die Berichte</b> — je Objekt eine Markdown-Datei und eine CSV-Datei, dazu der
    /// Sammelbericht in beiden Formaten. Geschrieben werden <b>nur Verhältniszahlen, Anteile und
    /// Zählungen</b>; kein Absolutwert der Messung, keine Einheit einer Menge oder Leistung, kein
    /// Name außer der anonymen Kennung (Konzept Kapitel 9 K5). <see cref="Berichtswache"/> hält
    /// jeden Text dagegen, bevor er auf die Platte kommt.
    ///
    /// <para><b>Markdown für den Menschen, CSV für die Reihe.</b> Die CSV-Dateien sind gedacht, um
    /// mehrere Läufe (andere Seeds, mehr Realisierungen, ein anderer Katalog) gegeneinander zu
    /// halten; sie führen dieselben Zahlen wie die Markdown-Tabellen, mit Punkt als Dezimalzeichen
    /// und <c>;</c> als Trenner.</para>
    /// </summary>
    internal static class Bericht
    {
        private static readonly CultureInfo K = CultureInfo.InvariantCulture;

        /// <summary>Die Kopfzeile der Sammel-CSV — auch der Spaltenname ist Teil des Formats.</summary>
        internal const string SAMMEL_KOPF =
            "Kennung;Nutzungsart;Bezugsart;Einheiten;Bilanzgrenze;Stochastisch;Realisierungen;Seed;"
            + "AufloesungMin;Messtage;Lueckenanteil;Jahresanteil;Teiljahr;"
            + "EnergieVerhaeltnis;EnergieAbweichung;Spitzenverhaeltnis;BandUnten;BandOben;Lage;"
            + "Streubreite;Skalierungsmass;WurzelNVerhaeltnis;Formmass;Formschwelle;"
            + "MonatsabweichungGroesste;MonatGroesster;Kalibrierfaktor;EnergieResiduum;"
            + "Zirkulationsanteil;Ampel;AmpelBand;AmpelForm;AmpelEnergie;BezugsmengeHerkunft;"
            + "MessspitzePerzentil;EnsembleUnten;EnsembleOben;EnsembleAnteilDarunter";

        // =============================================================================
        //  Je Objekt
        // =============================================================================

        /// <summary>Der Bericht eines Objekts als Markdown.</summary>
        internal static string ObjektMarkdown(Objektbefund b)
        {
            var s = new StringBuilder();
            s.Append("# Validierung ").Append(b.Kennung).Append(" — ").Append(Ampeltext(b.Gesamt)).AppendLine();
            s.AppendLine();
            s.AppendLine("Rechennachweis des Zapfprofilgenerators gegen eine gemessene Reihe (Umsetzungskonzept");
            s.AppendLine("Zapfprofilgenerator, Kapitel 7 Stufe Z5). **Der Bericht führt nur Verhältniszahlen,");
            s.AppendLine("Anteile und Zählungen** — keine gemessene Menge, keine gemessene Leistung (Kapitel 9 K5).");
            s.AppendLine();

            if (b.Abbruch != null)
            {
                s.AppendLine("## Nicht ausgewertet");
                s.AppendLine();
                s.Append("> ").AppendLine(b.Abbruch);
                s.AppendLine();
                Hinweise(s, b);
                return s.ToString();
            }

            s.AppendLine("## Eingang");
            s.AppendLine();
            s.AppendLine("| Angabe | Wert |");
            s.AppendLine("|---|---|");
            Zeile(s, "Nutzungsart des Katalogs", b.Nutzungsart);
            Zeile(s, "Bezugsart", b.Bezugsart.ToString());
            Zeile(s, "Einheiten N", Ganz(b.Einheiten));
            Zeile(s, "Herkunft der Bezugsmenge", Herkunfttext(b.Herkunft));
            Zeile(s, "Kalenderart", b.Kalenderart.ToString());
            Zeile(s, "Bilanzgrenze des Zählers", b.Grenze.ToString());
            Zeile(s, "Rechenweg der Jahresreihe", b.Stochastisch ? "stochastisch" : "deterministisch");
            Zeile(s, "Realisierungen / Seed", Ganz(b.Realisierungen) + " / " + Ganz(b.Seed));
            Zeile(s, "Auflösung der Messreihe [min]", Ganz(b.AufloesungMin));
            Zeile(s, "Länge der Messreihe [d]", Zahl(b.MesstageGesamt, 1));
            Zeile(s, "Lückenanteil [-]", Zahl(b.Lueckenanteil, 4));
            Zeile(s, "Schalttage in der Reihe", Ganz(b.Schalttage));
            Zeile(s, "Abgedeckter Jahresanteil [-]", Zahl(b.Jahresanteil, 4));
            Zeile(s, "Teiljahr (Jahreswert hochgerechnet)", b.Teiljahr ? "ja" : "nein");
            s.AppendLine();

            s.AppendLine("## Abnahme nach Kapitel 7, Zeile Z5");
            s.AppendLine();
            s.AppendLine("| Kriterium | Ampel | Maß | Schranke | Bemerkung |");
            s.AppendLine("|---|---|---|---|---|");
            foreach (Kriterium k in b.Kriterien)
                s.Append("| ").Append(k.Name).Append(" | ").Append(Ampeltext(k.Ampel))
                 .Append(" | ").Append(Zahl(k.Mass, 6)).Append(" | ").Append(k.Schranke)
                 .Append(" | ").Append(k.Satz).AppendLine(" |");
            s.AppendLine();

            s.AppendLine("## Die fünf Kennzahlen");
            s.AppendLine();
            s.AppendLine("| Kennzahl | Wert |");
            s.AppendLine("|---|---|");
            Zeile(s, "(a) Energieverhältnis Messung/Rechnung [-]", Zahl(b.EnergieVerhaeltnis, 6));
            Zeile(s, "(a) Abweichung [-]", Zahl(b.EnergieAbweichung, 6));
            Zeile(s, "(b) Spitzenverhältnis Messung/Rechnung [-]", Zahl(b.Spitzenverhaeltnis, 6));
            Zeile(s, "(b) Band der Dauerlinie [-]", Zahl(b.BandUnten, 6) + " … " + Zahl(b.BandOben, 6));
            Zeile(s, "(b) Quantile des Bands [-]", Zahl(b.PerzentilUnten, 4) + " / " + Zahl(b.PerzentilOben, 4));
            Zeile(s, "(b) Werte der Dauerlinie", Ganz(b.Dauerlinienwerte));
            Zeile(s, "(b) Lage der Messspitze", b.Lage.ToString());
            Zeile(s, "Analyse: Perzentil der Messspitze in der Dauerlinie [-]", Zahl(b.MessspitzePerzentil, 4));
            Zeile(s, "Analyse: Ensemblespitzen je verglichener Realisierung [-]",
                  Zahl(b.EnsembleUnten, 4) + " … " + Zahl(b.EnsembleOben, 4));
            Zeile(s, "Analyse: Anteil der Ensemblespitzen unter der Messspitze [-]", Zahl(b.EnsembleAnteilDarunter, 4));
            Zeile(s, "Spitzenstreuung des Ensembles [-]", Zahl(b.StreuungUnten, 6) + " … " + Zahl(b.StreuungOben, 6));
            Zeile(s, "Streubreite oben/unten [-]", Zahl(b.Streubreite, 6));
            Zeile(s, "Realisierungen der Streuung", Ganz(b.StreuungRealisierungen));
            Zeile(s, "(c) 1/√N [-]", Zahl(b.WurzelNVerhaeltnis, 6));
            Zeile(s, "(c) Skalierungsmaß [-]", Zahl(b.Skalierungsmass, 6));
            Zeile(s, "(d) Formmaß [-]", Zahl(b.Formmass, 6));
            Zeile(s, "(d) Formschwelle [-]", Zahl(b.Formschwelle, 6));
            Zeile(s, "(e) größte Monatsabweichung [-]", Zahl(b.MonateGroessteAbweichung, 6));
            Zeile(s, "(e) Monat der größten Abweichung", Ganz(b.MonateGroessterMonat));
            Zeile(s, "Kalibrierfaktor [-]", Zahl(b.Kalibrierfaktor, 6));
            Zeile(s, "Residuum der Energie nach Kalibrierung [-]", Zahl(b.EnergieResiduum, 12));
            Zeile(s, "Zirkulationsanteil der Rechnung [-]", Zahl(b.Zirkulationsanteil, 6));
            Zeile(s, "Verglichen gegen die kalibrierte Reihe", b.GegenKalibrierteReihe ? "ja" : "nein");
            s.AppendLine();

            if (b.Form.Count > 0)
            {
                s.AppendLine("## (d) Form je Tagtyp");
                s.AppendLine();
                s.AppendLine("| Tagtyp | Tage gemessen | Tage gerechnet | mittlere Abweichung [-] "
                             + "| verschobener Anteil [-] | im Rahmen |");
                s.AppendLine("|---|---|---|---|---|---|");
                foreach (Formzeile f in b.Form)
                    s.Append("| ").Append(f.Tagtyp).Append(" | ").Append(Ganz(f.TageGemessen))
                     .Append(" | ").Append(Ganz(f.TageGerechnet)).Append(" | ").Append(Zahl(f.MittlereAbweichung, 6))
                     .Append(" | ").Append(Zahl(f.VerschobenerAnteil, 6)).Append(" | ")
                     .Append(f.ImRahmen ? "ja" : "nein").AppendLine(" |");
                s.AppendLine();
            }

            if (b.MonatsanteileGemessen.Count > 0)
            {
                s.AppendLine("## (e) Monatsanteile");
                s.AppendLine();
                s.AppendLine("| Monat | gemessen [-] | gerechnet [-] | Abweichung [-] |");
                s.AppendLine("|---|---|---|---|");
                for (int m = 0; m < b.MonatsanteileGemessen.Count; m++)
                {
                    double gem = b.MonatsanteileGemessen[m];
                    double ger = m < b.MonatsanteileGerechnet.Count ? b.MonatsanteileGerechnet[m] : 0.0;
                    s.Append("| ").Append(Ganz(m + 1)).Append(" | ").Append(Zahl(gem, 6))
                     .Append(" | ").Append(Zahl(ger, 6)).Append(" | ").Append(Zahl(gem - ger, 6)).AppendLine(" |");
                }
                s.AppendLine();
            }

            if (b.Vorschlag is { } v)
            {
                s.AppendLine("## Kalibriervorschlag für die Nichtwohn-Nutzungsart");
                s.AppendLine();
                s.AppendLine("Der Vorschlag ist **nicht** gespeichert; er gehört in eine eigene Katalogkopie des");
                s.AppendLine("Anwenders (Status „eigen\"). Der **Tagesbedarf steht als Verhältnis** zur Rechnung, nicht");
                s.AppendLine("als Betrag — ein Betrag wäre eine gemessene Menge und gehört nicht in diesen Bericht;");
                s.AppendLine("der Anwender erhält ihn durch Multiplikation mit dem gerechneten Tagesbedarf je Einheit.");
                s.AppendLine();
                s.AppendLine("| Angabe | Wert |");
                s.AppendLine("|---|---|");
                Zeile(s, "Tagesbedarf je Einheit, Messung/Rechnung [-]", Zahl(v.TagesbedarfVerhaeltnis, 6));
                Zeile(s, "vollständige Messtage", Ganz(v.VolleTage));
                Zeile(s, "Wochenfaktoren Mo … So [-]",
                      string.Join(" / ", v.Wochenfaktoren.Select(w => Zahl(w, 4))));
                s.AppendLine();
                if (v.Tagesgaenge.Count > 0)
                {
                    s.AppendLine("| Tagtyp des Vorschlags | Tage im Mittel |");
                    s.AppendLine("|---|---|");
                    foreach (Formzeile f in v.Tagesgaenge)
                        s.Append("| ").Append(f.Tagtyp).Append(" | ").Append(Ganz(f.TageGemessen)).AppendLine(" |");
                    s.AppendLine();
                }
            }

            Hinweise(s, b);
            return s.ToString();
        }

        private static void Hinweise(StringBuilder s, Objektbefund b)
        {
            s.AppendLine("## Hinweise des Rechenwegs");
            s.AppendLine();
            if (b.Hinweise.Count == 0) { s.AppendLine("Keine."); s.AppendLine(); return; }
            foreach (string h in b.Hinweise) s.Append("- ").AppendLine(h);
            s.AppendLine();
        }

        /// <summary>Der Bericht eines Objekts als CSV — Name, Wert, je Zeile eine Größe.</summary>
        internal static string ObjektCsv(Objektbefund b)
        {
            var s = new StringBuilder();
            s.AppendLine("Groesse;Wert");
            Csv(s, "Kennung", b.Kennung);
            Csv(s, "Ampel", Ampeltext(b.Gesamt));
            if (b.Abbruch != null) { Csv(s, "Abbruch", b.Abbruch); return s.ToString(); }
            Csv(s, "Nutzungsart", b.Nutzungsart);
            Csv(s, "Bezugsart", b.Bezugsart.ToString());
            Csv(s, "Einheiten", Ganz(b.Einheiten));
            Csv(s, "BezugsmengeHerkunft", Herkunfttext(b.Herkunft));
            Csv(s, "MessspitzePerzentil", Zahl(b.MessspitzePerzentil, 8));
            Csv(s, "EnsembleUnten", Zahl(b.EnsembleUnten, 8));
            Csv(s, "EnsembleOben", Zahl(b.EnsembleOben, 8));
            Csv(s, "EnsembleAnteilDarunter", Zahl(b.EnsembleAnteilDarunter, 8));
            Csv(s, "Bilanzgrenze", b.Grenze.ToString());
            Csv(s, "Stochastisch", b.Stochastisch ? "1" : "0");
            Csv(s, "Realisierungen", Ganz(b.Realisierungen));
            Csv(s, "Seed", Ganz(b.Seed));
            Csv(s, "AufloesungMin", Ganz(b.AufloesungMin));
            Csv(s, "Messtage", Zahl(b.MesstageGesamt, 4));
            Csv(s, "Lueckenanteil", Zahl(b.Lueckenanteil, 6));
            Csv(s, "Jahresanteil", Zahl(b.Jahresanteil, 6));
            Csv(s, "EnergieVerhaeltnis", Zahl(b.EnergieVerhaeltnis, 8));
            Csv(s, "EnergieAbweichung", Zahl(b.EnergieAbweichung, 8));
            Csv(s, "Spitzenverhaeltnis", Zahl(b.Spitzenverhaeltnis, 8));
            Csv(s, "BandUnten", Zahl(b.BandUnten, 8));
            Csv(s, "BandOben", Zahl(b.BandOben, 8));
            Csv(s, "Lage", b.Lage.ToString());
            Csv(s, "StreuungUnten", Zahl(b.StreuungUnten, 8));
            Csv(s, "StreuungOben", Zahl(b.StreuungOben, 8));
            Csv(s, "Streubreite", Zahl(b.Streubreite, 8));
            Csv(s, "WurzelNVerhaeltnis", Zahl(b.WurzelNVerhaeltnis, 8));
            Csv(s, "Skalierungsmass", Zahl(b.Skalierungsmass, 8));
            Csv(s, "Formmass", Zahl(b.Formmass, 8));
            Csv(s, "Formschwelle", Zahl(b.Formschwelle, 8));
            Csv(s, "MonatsabweichungGroesste", Zahl(b.MonateGroessteAbweichung, 8));
            Csv(s, "MonatGroesster", Ganz(b.MonateGroessterMonat));
            Csv(s, "Kalibrierfaktor", Zahl(b.Kalibrierfaktor, 8));
            Csv(s, "EnergieResiduum", Zahl(b.EnergieResiduum, 12));
            Csv(s, "Zirkulationsanteil", Zahl(b.Zirkulationsanteil, 8));
            Csv(s, "GegenKalibrierteReihe", b.GegenKalibrierteReihe ? "1" : "0");
            foreach (Formzeile f in b.Form)
                Csv(s, "Form." + f.Tagtyp, Zahl(f.MittlereAbweichung, 8));
            for (int m = 0; m < b.MonatsanteileGemessen.Count; m++)
                Csv(s, "Monatsanteil.gemessen." + (m + 1).ToString(K), Zahl(b.MonatsanteileGemessen[m], 8));
            for (int m = 0; m < b.MonatsanteileGerechnet.Count; m++)
                Csv(s, "Monatsanteil.gerechnet." + (m + 1).ToString(K), Zahl(b.MonatsanteileGerechnet[m], 8));
            if (b.Vorschlag is { } v)
            {
                Csv(s, "Vorschlag.TagesbedarfVerhaeltnis", Zahl(v.TagesbedarfVerhaeltnis, 8));
                Csv(s, "Vorschlag.VolleTage", Ganz(v.VolleTage));
                for (int i = 0; i < v.Wochenfaktoren.Count; i++)
                    Csv(s, "Vorschlag.Wochenfaktor." + (i + 1).ToString(K), Zahl(v.Wochenfaktoren[i], 8));
            }
            return s.ToString();
        }

        // =============================================================================
        //  Der Sammelbericht
        // =============================================================================

        /// <summary>Der Sammelbericht als Markdown: eine Zeile je Objekt und die Zählung der Ampeln.</summary>
        internal static string SammelMarkdown(IReadOnlyList<Objektbefund> befunde, string katalog, string quellen)
        {
            var s = new StringBuilder();
            s.AppendLine("# Validierung des Zapfprofilgenerators gegen Messreihen");
            s.AppendLine();
            s.AppendLine("Sammelbericht des Werkzeugs `Werkzeuge/ZapfprofilValidierung` (Umsetzungskonzept");
            s.AppendLine("Zapfprofilgenerator, Kapitel 7 Stufe Z5, offener Punkt K5). **Nur Verhältniszahlen,");
            s.AppendLine("Anteile und Zählungen** — keine gemessene Menge, keine gemessene Leistung, kein");
            s.AppendLine("Objektname außer der anonymen Kennung.");
            s.AppendLine();
            s.Append("Katalogquelle: `").Append(katalog).AppendLine("`.");
            if (!string.IsNullOrWhiteSpace(quellen))
            {
                s.AppendLine();
                s.AppendLine(quellen);
            }
            s.AppendLine();

            s.AppendLine("## Ampel je Objekt");
            s.AppendLine();
            s.AppendLine("| Kennung | Nutzungsart | N | Herkunft N | Ampel | Band | Form | Energie |");
            s.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (Objektbefund b in befunde)
                s.Append("| ").Append(b.Kennung).Append(" | ").Append(b.Nutzungsart).Append(" | ")
                 .Append(Ganz(b.Einheiten)).Append(" | ").Append(Herkunfttext(b.Herkunft)).Append(" | ").Append(Ampeltext(b.Gesamt)).Append(" | ")
                 .Append(Kurz(b, 0)).Append(" | ").Append(Kurz(b, 1)).Append(" | ").Append(Kurz(b, 2))
                 .AppendLine(" |");
            s.AppendLine();

            Kriterium wn = Sammelkriterium.Bilden(befunde);
            s.AppendLine("## Vierte Abnahme: die Wurzel-N-Skalierung über alle Objekte");
            s.AppendLine();
            s.AppendLine("| Kriterium | Ampel | Maß | Schranke | Bemerkung |");
            s.AppendLine("|---|---|---|---|---|");
            s.Append("| ").Append(wn.Name).Append(" | ").Append(Ampeltext(wn.Ampel)).Append(" | ")
             .Append(Zahl(wn.Mass, 4)).Append(" | ").Append(wn.Schranke).Append(" | ")
             .Append(wn.Satz).AppendLine(" |");
            s.AppendLine();
            s.AppendLine("| Kennung | N | Spitzenverhältnis [-] | 1/√N [-] | Skalierungsmaß [-] |");
            s.AppendLine("|---|---|---|---|---|");
            foreach (Objektbefund b in befunde)
                s.Append("| ").Append(b.Kennung).Append(" | ").Append(Ganz(b.Einheiten)).Append(" | ")
                 .Append(Zahl(b.Spitzenverhaeltnis, 4)).Append(" | ").Append(Zahl(b.WurzelNVerhaeltnis, 4))
                 .Append(" | ").Append(Zahl(b.Skalierungsmass, 4)).AppendLine(" |");
            s.AppendLine();
            (double? alle, int alleObjekte) = Sammelkriterium.SteigungAlle(befunde);
            s.Append("Zum Vergleich die Steigung über alle ").Append(Ganz(alleObjekte))
             .Append(" auswertbaren Objekte, auch mit Platzhalter- oder unbekannter Bezugsmenge: ")
             .Append(Zahl(alle, 4)).AppendLine(" (keine Ampel).");
            s.AppendLine();

            s.Append(Bandanalyse.Markdown(befunde));

            s.AppendLine("## Kennzahlen je Objekt");
            s.AppendLine();
            s.AppendLine("| Kennung | Energie­verhältnis | Spitzen­verhältnis | Band | Lage | Skalierungs­maß "
                         + "| Formmaß (Schwelle) | Monats­abweichung | Kalibrier­faktor | Residuum |");
            s.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (Objektbefund b in befunde)
                s.Append("| ").Append(b.Kennung).Append(" | ").Append(Zahl(b.EnergieVerhaeltnis, 4))
                 .Append(" | ").Append(Zahl(b.Spitzenverhaeltnis, 4))
                 .Append(" | ").Append(Zahl(b.BandUnten, 3)).Append(" … ").Append(Zahl(b.BandOben, 3))
                 .Append(" | ").Append(b.Abbruch == null ? b.Lage.ToString() : "—")
                 .Append(" | ").Append(Zahl(b.Skalierungsmass, 4))
                 .Append(" | ").Append(Zahl(b.Formmass, 4)).Append(" (").Append(Zahl(b.Formschwelle, 4)).Append(")")
                 .Append(" | ").Append(Zahl(b.MonateGroessteAbweichung, 4))
                 .Append(" | ").Append(Zahl(b.Kalibrierfaktor, 4))
                 .Append(" | ").Append(Zahl(b.EnergieResiduum, 12)).AppendLine(" |");
            s.AppendLine();

            s.AppendLine("## Zählung");
            s.AppendLine();
            s.AppendLine("| Ampel | Objekte |");
            s.AppendLine("|---|---|");
            foreach (Ampel a in new[] { Ampel.Gruen, Ampel.Gelb, Ampel.Rot })
                s.Append("| ").Append(Ampeltext(a)).Append(" | ")
                 .Append(Ganz(befunde.Count(b => b.Gesamt == a))).AppendLine(" |");
            s.AppendLine();

            var mitAbbruch = befunde.Where(b => b.Abbruch != null).ToList();
            if (mitAbbruch.Count > 0)
            {
                s.AppendLine("## Nicht ausgewertete Objekte");
                s.AppendLine();
                foreach (Objektbefund b in mitAbbruch)
                    s.Append("- **").Append(b.Kennung).Append("**: ").AppendLine(b.Abbruch);
                s.AppendLine();
            }
            return s.ToString();
        }

        /// <summary>Der Sammelbericht als CSV: eine Zeile je Objekt, Kopfzeile <see cref="SAMMEL_KOPF"/>.</summary>
        internal static string SammelCsv(IReadOnlyList<Objektbefund> befunde)
        {
            var s = new StringBuilder();
            s.AppendLine(SAMMEL_KOPF);
            foreach (Objektbefund b in befunde)
            {
                s.Append(Feld(b.Kennung)).Append(';').Append(Feld(b.Nutzungsart)).Append(';')
                 .Append(b.Bezugsart).Append(';').Append(Ganz(b.Einheiten)).Append(';')
                 .Append(b.Grenze).Append(';').Append(b.Stochastisch ? "1" : "0").Append(';')
                 .Append(Ganz(b.Realisierungen)).Append(';').Append(Ganz(b.Seed)).Append(';')
                 .Append(Ganz(b.AufloesungMin)).Append(';').Append(Zahl(b.MesstageGesamt, 4)).Append(';')
                 .Append(Zahl(b.Lueckenanteil, 6)).Append(';').Append(Zahl(b.Jahresanteil, 6)).Append(';')
                 .Append(b.Teiljahr ? "1" : "0").Append(';')
                 .Append(Zahl(b.EnergieVerhaeltnis, 8)).Append(';').Append(Zahl(b.EnergieAbweichung, 8)).Append(';')
                 .Append(Zahl(b.Spitzenverhaeltnis, 8)).Append(';').Append(Zahl(b.BandUnten, 8)).Append(';')
                 .Append(Zahl(b.BandOben, 8)).Append(';').Append(b.Lage).Append(';')
                 .Append(Zahl(b.Streubreite, 8)).Append(';').Append(Zahl(b.Skalierungsmass, 8)).Append(';')
                 .Append(Zahl(b.WurzelNVerhaeltnis, 8)).Append(';').Append(Zahl(b.Formmass, 8)).Append(';')
                 .Append(Zahl(b.Formschwelle, 8)).Append(';')
                 .Append(Zahl(b.MonateGroessteAbweichung, 8)).Append(';').Append(Ganz(b.MonateGroessterMonat)).Append(';')
                 .Append(Zahl(b.Kalibrierfaktor, 8)).Append(';').Append(Zahl(b.EnergieResiduum, 12)).Append(';')
                 .Append(Zahl(b.Zirkulationsanteil, 8)).Append(';').Append(Ampeltext(b.Gesamt)).Append(';')
                 .Append(Kurz(b, 0)).Append(';').Append(Kurz(b, 1)).Append(';')
                 .Append(Kurz(b, 2)).Append(';').Append(Herkunfttext(b.Herkunft)).Append(';')
                 .Append(Zahl(b.MessspitzePerzentil, 8)).Append(';').Append(Zahl(b.EnsembleUnten, 8)).Append(';')
                 .Append(Zahl(b.EnsembleOben, 8)).Append(';').Append(Zahl(b.EnsembleAnteilDarunter, 8)).AppendLine();
            }
            return s.ToString();
        }

        // =============================================================================
        //  Formate
        // =============================================================================

        /// <summary>Die Herkunft der Bezugsmenge als Wort; ohne Angabe ein Strich.</summary>
        internal static string Herkunfttext(Bezugsmengenherkunft? h)
            => h switch
            {
                Bezugsmengenherkunft.Veroeffentlichung => "belegt",
                Bezugsmengenherkunft.Abgeleitet => "abgeleitet",
                Bezugsmengenherkunft.Platzhalter => "Platzhalter",
                Bezugsmengenherkunft.Unbekannt => "unbekannt",
                _ => "—"
            };

        internal static string Ampeltext(Ampel a)
            => a == Ampel.Gruen ? "gruen" : a == Ampel.Gelb ? "gelb" : "rot";

        private static string Kurz(Objektbefund b, int index)
            => index < b.Kriterien.Count ? Ampeltext(b.Kriterien[index].Ampel) : "—";

        private static void Zeile(StringBuilder s, string name, string wert)
            => s.Append("| ").Append(name).Append(" | ").Append(wert).AppendLine(" |");

        private static void Csv(StringBuilder s, string name, string wert)
            => s.Append(name).Append(';').AppendLine(Feld(wert));

        private static string Feld(string w)
            => w == null ? "" : w.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");

        internal static string Ganz(int w) => w.ToString(K);

        private static string Zahl(double w, int stellen) => Zahl((double?)w, stellen);

        /// <summary>Eine Zahl mit höchstens <paramref name="stellen"/> Nachkommastellen; <c>null</c> = Strich.</summary>
        internal static string Zahl(double? w, int stellen)
        {
            if (w == null || double.IsNaN(w.Value) || double.IsInfinity(w.Value)) return "—";
            return w.Value.ToString("0." + new string('#', Math.Max(1, stellen)), K);
        }
    }
}
