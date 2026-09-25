using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Die Ausgabe von <c>vergleich</c></b> — alles nach <c>--ziel</c>, außerhalb des
    /// Repositoriums: <c>gebaeude.csv</c>, <c>gebaeude_monate.csv</c>, <c>projekte.csv</c>,
    /// <c>stunden/*.csv</c> (nur mit <c>--stundenreihen</c>), <c>bericht.html</c> und
    /// <c>zusammenfassung.md</c>.
    ///
    /// <para><b>Wiederholbar.</b> Zwei Läufe auf derselben Datenbank ergeben byte-gleiche Dateien:
    /// keine Zeitstempel und keine Rechenzeiten außerhalb von <c>protokoll.txt</c>, feste
    /// Reihenfolge (Projekt-ID, Gebäude-ID), invariante Zahlen in den CSV.</para>
    ///
    /// <para><b>Namen.</b> Ohne <c>--mit-namen</c> steht kein Name in einer Datei: Die
    /// Namensspalten fehlen, und jeder Text (Meldungen, Einheit, Gebäudeart …) läuft durch die
    /// Bereinigung. <c>zusammenfassung.md</c> trägt nie einen Namen und nie eine ID — sie ist
    /// der einzige Teil, der in Papiere übernommen wird.</para>
    /// </summary>
    internal sealed class Berichtsschreiber
    {
        internal const string GEBAEUDE_CSV = "gebaeude.csv";
        internal const string MONATE_CSV = "gebaeude_monate.csv";
        internal const string PROJEKTE_CSV = "projekte.csv";
        internal const string STUNDEN_ORDNER = "stunden";
        internal const string BERICHT_HTML = "bericht.html";
        internal const string ZUSAMMENFASSUNG_MD = "zusammenfassung.md";

        /// <summary>Die Bänder der Jahresabweichung [%] (Grenzen: unten eingeschlossen; das letzte Band ist „&gt; 50").</summary>
        internal static readonly (string Name, double Unten, double Oben)[] BAENDER =
        {
            ("< 0", double.NegativeInfinity, 0.0),
            ("0–5", 0.0, 5.0),
            ("5–15", 5.0, 15.0),
            ("15–30", 15.0, 30.0),
            ("30–50", 30.0, 50.0),
            ("> 50", 50.0, double.PositiveInfinity),
        };

        private const string SPITZENHINWEIS = "Spitzen der Stundenreihe, keine Normheizlast";

        private readonly Argumente _arg;
        private readonly Ausgabe _aus;
        private readonly List<Projektzeile> _projekte;
        private readonly List<Gebaeudezeile> _gebaeude;
        private readonly int _stand;
        private readonly Namensbereinigung _b;

        internal Berichtsschreiber(Argumente arg, Ausgabe aus, List<Projektzeile> projekte, int stand)
        {
            _arg = arg;
            _aus = aus;
            _projekte = projekte;
            _gebaeude = projekte.SelectMany(p => p.Gebaeude).ToList();
            _stand = stand;
            _b = aus.Bereinigung;
        }

        internal void Schreiben()
        {
            Datei(GEBAEUDE_CSV, GebaeudeCsv());
            Datei(MONATE_CSV, MonateCsv());
            Datei(PROJEKTE_CSV, ProjekteCsv());
            if (_arg.Stundenreihen) StundenSchreiben();
            Datei(BERICHT_HTML, BerichtHtml());
            Datei(ZUSAMMENFASSUNG_MD, Zusammenfassung());
        }

        private void Datei(string name, string inhalt)
        {
            string pfad = Path.Combine(_arg.Ziel, name);
            File.WriteAllText(pfad, inhalt, new UTF8Encoding(false));
            _aus.Protokoll("geschrieben: " + name);
        }

        private string T(string s) => _b.Bereinigen(s ?? "");

        // =================================================================================
        //  gebaeude.csv
        // =================================================================================

        private sealed class Spalte
        {
            internal string Name;
            internal Func<Gebaeudezeile, string> Wert;
            internal Spalte(string name, Func<Gebaeudezeile, string> wert) { Name = name; Wert = wert; }
        }

        private List<Spalte> GebaeudeSpalten()
        {
            var s = new List<Spalte>
            {
                new Spalte("Projekt", z => Formate.Csv(z.IdProjekt)),
                new Spalte("Gebaeude", z => Formate.Csv(z.IdGebaeude)),
            };
            if (_arg.MitNamen)
            {
                s.Add(new Spalte("Projektname", z => Formate.CsvText(z.Projektname)));
                s.Add(new Spalte("Gebaeudename", z => Formate.CsvText(z.Gebaeudename)));
            }
            s.AddRange(new[]
            {
                new Spalte("Ampel", z => Formate.CsvText(Ursachenregeln.Text(z.Befund.Ampel))),
                new Spalte("Vermerk", z => Formate.CsvText(z.Befund.Vermerk)),
                new Spalte("Regeln", z => Formate.CsvText(string.Join(" ", z.Befund.Codes))),
                new Spalte("Bauform", z => Formate.CsvText(z.Merkmale.Bauform)),
                new Spalte("Flaechenangabe", z => Formate.Csv(z.Merkmale.IstFlaeche)),
                new Spalte("Einheit", z => Formate.CsvText(T(z.Merkmale.Einheit))),
                new Spalte("Bezugswert", z => Formate.Csv(z.Merkmale.Bezugswert)),
                new Spalte("spez_kWh_m2a", z => Formate.Csv(z.Merkmale.SpezWaermeverbrauch)),
                new Spalte("Verbrauch_angegeben_kWh", z => Formate.Csv(z.Merkmale.IstFlaeche ? (double?)null : z.Merkmale.VerbrauchNeuKwh)),
                new Spalte("Nutzflaeche_m2", z => Formate.Csv(z.Merkmale.Nutzflaeche)),
                new Spalte("Skalierung", z => Formate.Csv(z.Merkmale.Skalierung)),
                new Spalte("Bauweise_Wh_K", z => Formate.Csv(z.Merkmale.Bauweise)),
                new Spalte("Bauweise_Wh_m2K", z => Formate.Csv(z.Merkmale.BauweiseJeM2)),
                new Spalte("Fensteranteil_proz", z => Formate.Csv(z.Merkmale.FensteranteilProzent)),
                new Spalte("Suedanteil_proz", z => Formate.Csv(z.Merkmale.SuedanteilProzent)),
                new Spalte("Innere_Gewinne_W", z => Formate.Csv(z.Merkmale.InnereGewinneW)),
                new Spalte("Innere_Gewinne_W_m2", z => Formate.Csv(z.Merkmale.InnereGewinneWm2)),
                new Spalte("WG_NWG", z => Formate.CsvText(T(z.Merkmale.WgNwg))),
                new Spalte("Gebaeudeart", z => Formate.CsvText(T(z.Merkmale.Gebaeudeart))),
                new Spalte("Baualtersklasse", z => Formate.CsvText(T(z.Merkmale.Baualtersklasse))),
                new Spalte("Soll_Tag_C", z => Formate.Csv(z.Merkmale.SollTagC)),
                new Spalte("Soll_Nacht_C", z => Formate.Csv(z.Merkmale.SollNachtC)),
                new Spalte("Absenkung_K", z => Formate.Csv(z.Merkmale.AbsenkungK)),
                new Spalte("Luftwechsel_1_h", z => Formate.Csv(z.Merkmale.Luftwechsel)),
                new Spalte("Zone", z => Formate.Csv(z.Merkmale.Zone)),
                new Spalte("Heizkreis_Aktiv", z => Formate.Csv(z.Merkmale.HeizkreisAktiv)),
                new Spalte("Feste_Nennleistung", z => Formate.Csv(z.Merkmale.FesteNennleistung)),
                new Spalte("Kuehlung_wirksam", z => Formate.Csv(z.Merkmale.KuehlungWirksam)),
                new Spalte("Gekoppelt", z => Formate.Csv(z.Merkmale.Gekoppelt)),
                new Spalte("Spalte_Modell", z => Formate.CsvText(z.Merkmale.SpalteModell)),
                new Spalte("Alt_ok", z => Formate.Csv(z.Alt.Ok)),
                new Spalte("Neu_ok", z => Formate.Csv(z.Neu.Ok)),
                new Spalte("Alt_Fehlercode", z => Formate.CsvText(z.Alt.Fehlercode)),
                new Spalte("Neu_Fehlercode", z => Formate.CsvText(z.Neu.Fehlercode)),
            });

            void Kennzahl(string name, Func<Wegergebnis, double> wert)
            {
                s.Add(new Spalte(name + "_alt", z => Formate.Csv(wert(z.Alt))));
                s.Add(new Spalte(name + "_neu", z => Formate.Csv(wert(z.Neu))));
                s.Add(new Spalte(name + "_delta", z => Formate.Csv(Kennzahlen.Delta(wert(z.Alt), wert(z.Neu)))));
                s.Add(new Spalte(name + "_delta_proz", z => Formate.Csv(Kennzahlen.DeltaProzent(wert(z.Alt), wert(z.Neu)))));
            }
            Kennzahl("Jahr_MWh", w => w.JahrMwh);
            Kennzahl("Spitze_Stunde_kW", w => w.SpitzeKw);
            Kennzahl("Spitze_24h_Mittel_kW", w => w.TagesmittelKw);
            Kennzahl("Q95_kW", w => w.Q95Kw);
            Kennzahl("Vollbenutzungsstunden_h", w => w.VollbenutzungsstundenH);
            Kennzahl("Nachtanteil_proz", w => w.NachtanteilProzent);
            Kennzahl("Heizstunden_h", w => w.Heizstunden);

            s.AddRange(new[]
            {
                new Spalte("Katalogtreffer_alt_proz", z => Formate.Csv(z.Alt.KatalogtrefferProzent)),
                new Spalte("Katalogtreffer_neu_proz", z => Formate.Csv(z.Neu.KatalogtrefferProzent)),
                new Spalte("Katalogtreffer", z => Formate.CsvText(Katalogtext(z))),
                new Spalte("Verbrauchstreffer_alt_proz", z => Formate.Csv(z.Alt.VerbrauchstrefferProzent)),
                new Spalte("Verbrauchstreffer_neu_proz", z => Formate.Csv(z.Neu.VerbrauchstrefferProzent)),
                new Spalte("Raumtemperatur_Heizzeit_neu_C", z => Formate.Csv(z.Neu.RaumtemperaturMittelC)),
                new Spalte("Ueberhitzungsstunden_neu_h", z => Formate.Csv(z.Neu.Ueberhitzungsstunden)),
                new Spalte("Kaelte_neu_MWh", z => Formate.Csv(z.Neu.KaelteMwh)),
                new Spalte("Kaeltespitze_neu_kW", z => Formate.Csv(z.Neu.KaeltespitzeKw)),
                new Spalte("Fehler_alt", z => Formate.CsvText(z.Alt.Fehlertext)),
                new Spalte("Fehler_neu", z => Formate.CsvText(z.Neu.Fehlertext)),
                new Spalte("Meldungen_alt", z => Formate.CsvText(z.Alt.Meldungen)),
                new Spalte("Meldungen_neu", z => Formate.CsvText(z.Neu.Meldungen)),
            });
            return s;
        }

        /// <summary>„kein Katalogwert", „nicht bestimmbar (Verbrauchsangabe)" oder leer (Treffer steht in den Zahlenspalten).</summary>
        internal static string Katalogtext(Gebaeudezeile z)
        {
            if (!z.Merkmale.IstFlaeche) return "nicht bestimmbar (Verbrauchsangabe)";
            if (!(z.Merkmale.SpezWaermeverbrauch > 0.0)) return "kein Katalogwert";
            return "";
        }

        private string GebaeudeCsv()
        {
            List<Spalte> spalten = GebaeudeSpalten();
            var sb = new StringBuilder();
            sb.Append(string.Join(";", spalten.Select(s => s.Name))).Append('\n');
            foreach (Gebaeudezeile z in _gebaeude)
                sb.Append(string.Join(";", spalten.Select(s => s.Wert(z)))).Append('\n');
            return sb.ToString();
        }

        // =================================================================================
        //  gebaeude_monate.csv, projekte.csv, stunden/
        // =================================================================================

        private string MonateCsv()
        {
            var sb = new StringBuilder("Projekt;Gebaeude;Monat;alt_MWh;neu_MWh;delta_MWh;delta_proz\n");
            foreach (Gebaeudezeile z in _gebaeude)
                for (int m = 0; m < 12; m++)
                {
                    double a = z.Alt.Ok ? z.Alt.MonateMwh[m] : double.NaN;
                    double n = z.Neu.Ok ? z.Neu.MonateMwh[m] : double.NaN;
                    sb.Append(Formate.Csv(z.IdProjekt)).Append(';').Append(Formate.Csv(z.IdGebaeude)).Append(';')
                      .Append(Formate.Csv(m + 1)).Append(';').Append(Formate.Csv(a)).Append(';').Append(Formate.Csv(n)).Append(';')
                      .Append(Formate.Csv(Kennzahlen.Delta(a, n))).Append(';').Append(Formate.Csv(Kennzahlen.DeltaProzent(a, n)))
                      .Append('\n');
                }
            return sb.ToString();
        }

        private string ProjekteCsv()
        {
            var sb = new StringBuilder();
            sb.Append("Projekt;");
            if (_arg.MitNamen) sb.Append("Projektname;");
            sb.Append("Status;Grund;Gebaeude_gesamt;Gebaeude_gerechnet;Jahr_alt_MWh;Jahr_neu_MWh;Jahr_delta_MWh;" +
                      "Jahr_delta_proz;Spitze_alt_kW;Spitze_neu_kW;Spitze_delta_kW;Spitze_delta_proz;Ampel\n");

            foreach (Projektzeile p in _projekte)
            {
                sb.Append(Formate.Csv(p.Id)).Append(';');
                if (_arg.MitNamen) sb.Append(Formate.CsvText(p.Name)).Append(';');
                if (p.Uebersprungen)
                {
                    sb.Append("übersprungen;").Append(Formate.CsvText(T(p.Grund))).Append(';')
                      .Append(Formate.Csv(p.GebaeudeZugeordnet)).Append(";0;;;;;;;;;\n");
                    continue;
                }
                sb.Append("gerechnet;;").Append(Formate.Csv(p.Gebaeude.Count)).Append(';').Append(Formate.Csv(p.GebaeudeGerechnet)).Append(';')
                  .Append(Formate.Csv(p.SummeAltMwh)).Append(';').Append(Formate.Csv(p.SummeNeuMwh)).Append(';')
                  .Append(Formate.Csv(Kennzahlen.Delta(p.SummeAltMwh, p.SummeNeuMwh))).Append(';')
                  .Append(Formate.Csv(Kennzahlen.DeltaProzent(p.SummeAltMwh, p.SummeNeuMwh))).Append(';')
                  .Append(Formate.Csv(p.SpitzeAltKw)).Append(';').Append(Formate.Csv(p.SpitzeNeuKw)).Append(';')
                  .Append(Formate.Csv(Kennzahlen.Delta(p.SpitzeAltKw, p.SpitzeNeuKw))).Append(';')
                  .Append(Formate.Csv(Kennzahlen.DeltaProzent(p.SpitzeAltKw, p.SpitzeNeuKw))).Append(';')
                  .Append(Formate.CsvText(Ursachenregeln.Text(p.Ampel))).Append('\n');
            }

            // Die Summenzeile: gerechnete Projekte, Gebäude und Jahreswärme; keine Summenspitze
            // (die Projekte stehen nicht an einem Netz).
            List<Projektzeile> gerechnet = _projekte.Where(p => !p.Uebersprungen).ToList();
            double alt = gerechnet.Where(p => p.GebaeudeGerechnet > 0).Sum(p => p.SummeAltMwh);
            double neu = gerechnet.Where(p => p.GebaeudeGerechnet > 0).Sum(p => p.SummeNeuMwh);
            sb.Append("Summe;");
            if (_arg.MitNamen) sb.Append(';');
            sb.Append(Formate.Csv(gerechnet.Count)).Append(" gerechnet;")
              .Append(Formate.Csv(_projekte.Count - gerechnet.Count)).Append(" übersprungen;")
              .Append(Formate.Csv(_gebaeude.Count)).Append(';').Append(Formate.Csv(gerechnet.Sum(p => p.GebaeudeGerechnet))).Append(';')
              .Append(Formate.Csv(alt)).Append(';').Append(Formate.Csv(neu)).Append(';')
              .Append(Formate.Csv(Kennzahlen.Delta(alt, neu))).Append(';').Append(Formate.Csv(Kennzahlen.DeltaProzent(alt, neu)))
              .Append(";;;;;").Append(Formate.CsvText(Ursachenregeln.Text(SchlechtesteAmpel()))).Append('\n');
            return sb.ToString();
        }

        private Ampel SchlechtesteAmpel() => _gebaeude.Count == 0 ? Ampel.Erklaert : _gebaeude.Max(z => z.Befund.Ampel);

        private void StundenSchreiben()
        {
            string ordner = Path.Combine(_arg.Ziel, STUNDEN_ORDNER);
            Directory.CreateDirectory(ordner);
            foreach (Gebaeudezeile z in _gebaeude)
            {
                var sb = new StringBuilder("Stunde;alt_kW;neu_kW\n");
                for (int h = 0; h < 8760; h++)
                {
                    double a = z.Alt.StundenKw != null ? z.Alt.StundenKw[h] : double.NaN;
                    double n = z.Neu.StundenKw != null ? z.Neu.StundenKw[h] : double.NaN;
                    sb.Append(Formate.Csv(h + 1)).Append(';').Append(Formate.Csv(a)).Append(';').Append(Formate.Csv(n)).Append('\n');
                }
                string name = "P" + Formate.Csv(z.IdProjekt) + "_G" + Formate.Csv(z.IdGebaeude) + ".csv";
                File.WriteAllText(Path.Combine(ordner, name), sb.ToString(), new UTF8Encoding(false));
            }
            _aus.Protokoll("geschrieben: " + STUNDEN_ORDNER + "/ (" + Formate.Csv(_gebaeude.Count) + " Dateien)");
        }

        // =================================================================================
        //  Auswertung für Bericht und Zusammenfassung
        // =================================================================================

        /// <summary>Die Verteilung einer Menge von Abweichungen [%].</summary>
        internal sealed class Verteilung
        {
            internal int N;
            internal double Min = double.NaN, Q1 = double.NaN, Median = double.NaN, Q3 = double.NaN, Max = double.NaN;
            internal int[] Baender = new int[BAENDER.Length];

            internal static Verteilung Aus(IEnumerable<double> werte)
            {
                List<double> s = Formate.Sortiert(werte);
                var v = new Verteilung { N = s.Count };
                if (s.Count == 0) return v;
                v.Min = s[0];
                v.Q1 = Formate.Quantil(s, 0.25);
                v.Median = Formate.Quantil(s, 0.5);
                v.Q3 = Formate.Quantil(s, 0.75);
                v.Max = s[s.Count - 1];
                foreach (double w in s) v.Baender[Band(w)]++;
                return v;
            }
        }

        /// <summary>Das Band einer Abweichung: unten eingeschlossen, oben ausgeschlossen; 50 zählt zu „30–50".</summary>
        internal static int Band(double w)
        {
            if (w < 0.0) return 0;
            if (w < 5.0) return 1;
            if (w < 15.0) return 2;
            if (w < 30.0) return 3;
            if (w <= 50.0) return 4;
            return 5;
        }

        private IEnumerable<Gebaeudezeile> Gerechnet(bool flaeche)
            => _gebaeude.Where(z => z.BeideOk && z.Merkmale.IstFlaeche == flaeche);

        private static double DeltaJahr(Gebaeudezeile z) => Kennzahlen.DeltaProzent(z.Alt.JahrMwh, z.Neu.JahrMwh);

        /// <summary>Je Bauform-Gruppe (und Angabeart) der Median der Abweichung ihrer Gebäude.</summary>
        private IEnumerable<double> JeGruppe(bool flaeche)
            => Gerechnet(flaeche).GroupBy(z => z.Merkmale.Bauform)
                                 .Select(g => Formate.Quantil(Formate.Sortiert(g.Select(DeltaJahr)), 0.5));

        // =================================================================================
        //  zusammenfassung.md — ohne Namen und ohne IDs
        // =================================================================================

        private string Zusammenfassung()
        {
            var sb = new StringBuilder();
            void Z(string s = "") => sb.Append(s).Append('\n');

            List<Projektzeile> gerechnet = _projekte.Where(p => !p.Uebersprungen).ToList();
            int gebaeudeOk = _gebaeude.Count(z => z.BeideOk);
            int gebaeudeFehler = _gebaeude.Count(z => !z.BeideOk);

            Z("# Gebäudevergleich alt/neu — Zusammenfassung");
            Z();
            Z("Tagesbilanz-Weg (alt) gegen VDI-6007-Weg (neu), je Gebäude beide Wege erzwungen. " +
              "Namenlos und ohne IDs; die Einzelzeilen stehen in gebaeude.csv und projekte.csv.");
            Z();
            Z("- Werkzeug: Gebaeudevergleich " + Einstieg.Werkzeugversion);
            Z("- Schemastand der Datenbank: " + Formate.De(_stand) + ", Zielversion: " + Formate.De(SchemaStand.Zielversion));
            Z("- Auswahl: " + (_arg.Projekte == null ? "alle Projekte" : "Projektauswahl (" + Formate.De(_arg.Projekte.Count) + ")"));
            Z();
            Z("## Anzahlen");
            Z();
            Z("| Größe | Anzahl |");
            Z("|---|---:|");
            Z("| Projekte gesamt | " + Formate.De(_projekte.Count) + " |");
            Z("| Projekte gerechnet | " + Formate.De(gerechnet.Count) + " |");
            Z("| Projekte übersprungen | " + Formate.De(_projekte.Count - gerechnet.Count) + " |");
            Z("| Gebäude gerechnet (beide Wege) | " + Formate.De(gebaeudeOk) + " |");
            Z("| Gebäude mit Fehler | " + Formate.De(gebaeudeFehler) + " |");
            Z();

            var gruende = _projekte.Where(p => p.Uebersprungen)
                                   .GroupBy(p => OhneZahlen(p.Grund))
                                   .OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
            if (gruende.Count > 0)
            {
                Z("Übersprungen, nach Grund:");
                Z();
                Z("| Grund | Projekte |");
                Z("|---|---:|");
                foreach (var g in gruende) Z("| " + Md(g.Key) + " | " + Formate.De(g.Count()) + " |");
                Z();
            }

            Z("## Ampel");
            Z();
            Z("| erklärt | zu prüfen | Fehler |");
            Z("|---:|---:|---:|");
            Z("| " + Formate.De(_gebaeude.Count(z => z.Befund.Ampel == Ampel.Erklaert)) + " | " +
              Formate.De(_gebaeude.Count(z => z.Befund.Ampel == Ampel.ZuPruefen)) + " | " +
              Formate.De(_gebaeude.Count(z => z.Befund.Ampel == Ampel.Fehler)) + " |");
            int datenfehler = _gebaeude.Count(z => z.Befund.Vermerk == Ursachenregeln.VERMERK_DATENFEHLER);
            if (datenfehler > 0) Z();
            if (datenfehler > 0) Z("Davon mit Vermerk „Datenfehler“ (U-BW): " + Formate.De(datenfehler) + ".");
            Z();

            Z("## Jahresabweichung Δ = neu / alt − 1 [%]");
            Z();
            Z("### Je Gebäude");
            Z();
            VerteilungstabelleMd(sb, Verteilung.Aus(Gerechnet(true).Select(DeltaJahr)), Verteilung.Aus(Gerechnet(false).Select(DeltaJahr)));
            Z("### Je Bauform-Gruppe (Median der Gebäude einer Gruppe)");
            Z();
            VerteilungstabelleMd(sb, Verteilung.Aus(JeGruppe(true)), Verteilung.Aus(JeGruppe(false)));

            Z("## Katalogtreffer (Flächenangabe mit Katalogwert > 0)");
            Z();
            List<Gebaeudezeile> mitKatalog = _gebaeude.Where(z => z.BeideOk && z.Neu.KatalogtrefferProzent.HasValue).ToList();
            if (mitKatalog.Count > 0)
            {
                Z("- neu: " + Formate.De(mitKatalog.Min(z => z.Neu.KatalogtrefferProzent)) + " bis " +
                  Formate.De(mitKatalog.Max(z => z.Neu.KatalogtrefferProzent)) + " % (" + Formate.De(mitKatalog.Count) + " Gebäude)");
                Z("- alt: " + Formate.De(mitKatalog.Min(z => z.Alt.KatalogtrefferProzent)) + " bis " +
                  Formate.De(mitKatalog.Max(z => z.Alt.KatalogtrefferProzent)) + " %");
                Z("- neu im Band 90–115 %: " + Formate.De(mitKatalog.Count(z => z.Befund.Codes.Contains(Ursachenregeln.U_KAT))));
            }
            else Z("- kein Gebäude mit Katalogwert gerechnet");
            Z("- kein Katalogwert (Flächenangabe, spez = 0): " + Formate.De(_gebaeude.Count(z => z.Merkmale.IstFlaeche && !(z.Merkmale.SpezWaermeverbrauch > 0.0))));
            Z("- nicht bestimmbar (Verbrauchsangabe): " + Formate.De(_gebaeude.Count(z => !z.Merkmale.IstFlaeche)));
            Z();

            List<Gebaeudezeile> verbrauch = _gebaeude.Where(z => z.BeideOk && !z.Merkmale.IstFlaeche).ToList();
            if (verbrauch.Count > 0)
            {
                Z("## Verbrauchstreffer (Verbrauchsangabe)");
                Z();
                Z("- alt: " + Formate.De(verbrauch.Min(z => z.Alt.VerbrauchstrefferProzent), 2) + " bis " +
                  Formate.De(verbrauch.Max(z => z.Alt.VerbrauchstrefferProzent), 2) + " %");
                Z("- neu: " + Formate.De(verbrauch.Min(z => z.Neu.VerbrauchstrefferProzent), 2) + " bis " +
                  Formate.De(verbrauch.Max(z => z.Neu.VerbrauchstrefferProzent), 2) + " %");
                Z("- Band E8: |Δ Jahr| ≤ " + Formate.De(Ursachenregeln.BAND_E8_PROZENT, 1) + " %");
                Z();
            }

            Z("## Regeln");
            Z();
            Z("| Regel | Gebäude | Vorschlag |");
            Z("|---|---:|---|");
            foreach (string code in Ursachenregeln.ALLE)
            {
                int n = _gebaeude.Count(z => z.Befund.Codes.Contains(code));
                Z("| " + code + " | " + Formate.De(n) + " | " + Md(Ursachenregeln.Vorschlag(code)) + " |");
            }
            Z();

            Z("## Fehlercodes");
            Z();
            var codes = _gebaeude.SelectMany(z => new[] { ("alt", z.Alt), ("neu", z.Neu) })
                                 .Where(t => !t.Item2.Ok)
                                 .GroupBy(t => t.Item1 + ": " + t.Item2.Fehlercode)
                                 .OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
            if (codes.Count == 0) Z("Keiner.");
            foreach (var g in codes) Z("- " + Md(g.Key) + " — " + Formate.De(g.Count()));
            Z();
            Z("Die Spitzen in gebaeude.csv und bericht.html sind " + SPITZENHINWEIS + ".");
            return sb.ToString();
        }

        private static void VerteilungstabelleMd(StringBuilder sb, Verteilung flaeche, Verteilung verbrauch)
        {
            void Z(string s = "") => sb.Append(s).Append('\n');
            Z("| Angabe | n | Min | Q1 | Median | Q3 | Max |");
            Z("|---|---:|---:|---:|---:|---:|---:|");
            foreach ((string name, Verteilung v) in new[] { ("Fläche", flaeche), ("Verbrauch", verbrauch) })
                Z("| " + name + " | " + Formate.De(v.N) + " | " + Formate.De(v.Min) + " | " + Formate.De(v.Q1) + " | " +
                  Formate.De(v.Median) + " | " + Formate.De(v.Q3) + " | " + Formate.De(v.Max) + " |");
            Z();
            Z("| Angabe | " + string.Join(" | ", BAENDER.Select(b => b.Name + " %")) + " |");
            Z("|---|" + string.Concat(BAENDER.Select(_ => "---:|")));
            foreach ((string name, Verteilung v) in new[] { ("Fläche", flaeche), ("Verbrauch", verbrauch) })
                Z("| " + name + " | " + string.Join(" | ", v.Baender.Select(n => Formate.De(n))) + " |");
            Z();
        }

        /// <summary>Markdown-Zelle: senkrechte Striche maskieren.</summary>
        private static string Md(string s) => (s ?? "").Replace("|", "\\|");

        /// <summary>Ein Grund ohne Ziffern — die Zusammenfassung trägt keine ID (auch keine Klimaregion).</summary>
        private static string OhneZahlen(string s) => new string((s ?? "").Where(c => !char.IsDigit(c)).ToArray()).Replace("  ", " ");

        // =================================================================================
        //  bericht.html — eigenständig, ohne Fremdressourcen
        // =================================================================================

        private string BerichtHtml()
        {
            var sb = new StringBuilder();
            void Z(string s) => sb.Append(s).Append('\n');

            Z("<!DOCTYPE html>");
            Z("<html lang=\"de\"><head><meta charset=\"utf-8\"><title>Gebäudevergleich alt/neu</title>");
            Z("<style>");
            Z("body{font-family:system-ui,Segoe UI,Arial,sans-serif;margin:16px;color:#1b1b1b;background:#fff}");
            Z("h1{font-size:1.4em}h2{font-size:1.15em;margin-top:1.6em}");
            Z("table{border-collapse:collapse;font-size:.85em;margin:.5em 0}th,td{border:1px solid #bbb;padding:3px 6px;text-align:right;vertical-align:top}");
            Z("th{background:#eee}td.t{text-align:left}.rot{background:#f6c9c9}.gelb{background:#fbeeb8}.gruen{background:#cfeccf}");
            Z(".hinweis{color:#555;font-size:.9em}");
            Z("@media (prefers-color-scheme:dark){body{background:#161616;color:#e6e6e6}th{background:#2a2a2a}th,td{border-color:#444}" +
              ".rot{background:#5a2323}.gelb{background:#4d4318}.gruen{background:#1f4020}.hinweis{color:#aaa}}");
            Z("</style></head><body>");
            Z("<h1>Gebäudevergleich alt/neu</h1>");
            Z("<p>Tagesbilanz-Weg (alt) gegen VDI-6007-Weg (neu), je Gebäude beide Wege erzwungen. Werkzeug " +
              Formate.Html(Einstieg.Werkzeugversion) + ", Schemastand " + Formate.De(_stand) + " (Zielversion " +
              Formate.De(SchemaStand.Zielversion) + ").</p>");
            Z("<p class=\"hinweis\">Die Spitzen sind " + SPITZENHINWEIS + ".</p>");

            Z("<h2>Ampel</h2><table><tr><th>erklärt</th><th>zu prüfen</th><th>Fehler</th></tr><tr>" +
              "<td class=\"gruen\">" + Formate.De(_gebaeude.Count(z => z.Befund.Ampel == Ampel.Erklaert)) + "</td>" +
              "<td class=\"gelb\">" + Formate.De(_gebaeude.Count(z => z.Befund.Ampel == Ampel.ZuPruefen)) + "</td>" +
              "<td class=\"rot\">" + Formate.De(_gebaeude.Count(z => z.Befund.Ampel == Ampel.Fehler)) + "</td></tr></table>");

            Z("<h2>Gebäude</h2>");
            Z("<table><tr><th>Projekt</th><th>Gebäude</th>" + (_arg.MitNamen ? "<th>Name</th>" : "") +
              "<th>Ampel</th><th>Regeln</th><th>Angabe</th><th>Jahr alt MWh</th><th>Jahr neu MWh</th><th>Δ %</th>" +
              "<th>Spitze alt kW</th><th>Spitze neu kW</th><th>Δ %</th><th>Nacht alt %</th><th>Nacht neu %</th>" +
              "<th>Katalog neu %</th><th>Kälte neu MWh</th><th>Fehler</th></tr>");
            foreach (Gebaeudezeile z in _gebaeude)
            {
                string klasse = z.Befund.Ampel == Ampel.Fehler ? "rot" : z.Befund.Ampel == Ampel.ZuPruefen ? "gelb" : "gruen";
                string fehler = string.Join(" ", new[] { z.Alt.Ok ? "" : "alt: " + z.Alt.Fehlercode, z.Neu.Ok ? "" : "neu: " + z.Neu.Fehlercode, z.Befund.Vermerk }
                                                  .Where(s => s.Length > 0));
                string katalog = z.Neu.KatalogtrefferProzent.HasValue ? Formate.De(z.Neu.KatalogtrefferProzent) : Katalogtext(z);
                Z("<tr><td>" + z.IdProjekt.ToString(CultureInfo.InvariantCulture) + "</td><td>" +
                  z.IdGebaeude.ToString(CultureInfo.InvariantCulture) + "</td>" +
                  (_arg.MitNamen ? "<td class=\"t\">" + Formate.Html(z.Gebaeudename) + "</td>" : "") +
                  "<td class=\"t " + klasse + "\">" + Formate.Html(Ursachenregeln.Text(z.Befund.Ampel)) + "</td>" +
                  "<td class=\"t\">" + Formate.Html(string.Join(" ", z.Befund.Codes)) + "</td>" +
                  "<td class=\"t\">" + (z.Merkmale.IstFlaeche ? "Fläche" : "Verbrauch") + "</td>" +
                  "<td>" + Formate.De(z.Alt.JahrMwh, 2) + "</td><td>" + Formate.De(z.Neu.JahrMwh, 2) + "</td>" +
                  "<td>" + Formate.De(Kennzahlen.DeltaProzent(z.Alt.JahrMwh, z.Neu.JahrMwh)) + "</td>" +
                  "<td>" + Formate.De(z.Alt.SpitzeKw) + "</td><td>" + Formate.De(z.Neu.SpitzeKw) + "</td>" +
                  "<td>" + Formate.De(Kennzahlen.DeltaProzent(z.Alt.SpitzeKw, z.Neu.SpitzeKw)) + "</td>" +
                  "<td>" + Formate.De(z.Alt.NachtanteilProzent) + "</td><td>" + Formate.De(z.Neu.NachtanteilProzent) + "</td>" +
                  "<td class=\"t\">" + Formate.Html(katalog) + "</td><td>" + Formate.De(z.Neu.KaelteMwh, 2) + "</td>" +
                  "<td class=\"t\">" + Formate.Html(fehler) + "</td></tr>");
            }
            Z("</table>");

            Z("<h2>Projekte</h2>");
            Z("<table><tr><th>Projekt</th>" + (_arg.MitNamen ? "<th>Name</th>" : "") +
              "<th>Status</th><th>Gebäude gerechnet/gesamt</th><th>Jahr alt MWh</th><th>Jahr neu MWh</th><th>Δ %</th>" +
              "<th>Spitze alt kW</th><th>Spitze neu kW</th><th>Δ %</th><th>Ampel</th></tr>");
            foreach (Projektzeile p in _projekte)
            {
                string name = _arg.MitNamen ? "<td class=\"t\">" + Formate.Html(p.Name) + "</td>" : "";
                if (p.Uebersprungen)
                {
                    Z("<tr><td>" + p.Id.ToString(CultureInfo.InvariantCulture) + "</td>" + name +
                      "<td class=\"t\" colspan=\"9\">übersprungen: " + Formate.Html(T(p.Grund)) + "</td></tr>");
                    continue;
                }
                string klasse = p.Ampel == Ampel.Fehler ? "rot" : p.Ampel == Ampel.ZuPruefen ? "gelb" : "gruen";
                Z("<tr><td>" + p.Id.ToString(CultureInfo.InvariantCulture) + "</td>" + name + "<td class=\"t\">gerechnet</td>" +
                  "<td>" + Formate.De(p.GebaeudeGerechnet) + "/" + Formate.De(p.Gebaeude.Count) + "</td>" +
                  "<td>" + Formate.De(p.SummeAltMwh, 2) + "</td><td>" + Formate.De(p.SummeNeuMwh, 2) + "</td>" +
                  "<td>" + Formate.De(Kennzahlen.DeltaProzent(p.SummeAltMwh, p.SummeNeuMwh)) + "</td>" +
                  "<td>" + Formate.De(p.SpitzeAltKw) + "</td><td>" + Formate.De(p.SpitzeNeuKw) + "</td>" +
                  "<td>" + Formate.De(Kennzahlen.DeltaProzent(p.SpitzeAltKw, p.SpitzeNeuKw)) + "</td>" +
                  "<td class=\"t " + klasse + "\">" + Formate.Html(Ursachenregeln.Text(p.Ampel)) + "</td></tr>");
            }
            Z("</table>");

            Z("<h2>Regeln</h2><table><tr><th>Regel</th><th>Gebäude</th><th>Vorschlag</th></tr>");
            foreach (string code in Ursachenregeln.ALLE)
                Z("<tr><td class=\"t\">" + code + "</td><td>" + Formate.De(_gebaeude.Count(z => z.Befund.Codes.Contains(code))) +
                  "</td><td class=\"t\">" + Formate.Html(Ursachenregeln.Vorschlag(code)) + "</td></tr>");
            Z("</table>");
            Z("</body></html>");
            return sb.ToString();
        }
    }
}
