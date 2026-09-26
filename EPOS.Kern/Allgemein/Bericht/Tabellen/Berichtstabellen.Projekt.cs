using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RR = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>Die Strukturtabellen der Projektbeschreibung (Anhang A: Kälteerzeuger, Speichertemperaturen).</summary>
    public static partial class Berichtstabellen
    {
        /// <summary>
        /// <b><c>tabelle.kaelteerzeuger</c></b> — je Wärmepumpe im Kühlbetrieb Kälte, Kältestrom, EER-Jahreswert, ihr
        /// Netzbezug und der Stromträger (Kühlkonzept 8.4) aus den Modulzeilen des Ergebnisses des Stamms. Die Namen der
        /// Träger kommen aus dem Wertesatz (<paramref name="traegername"/>).
        /// </summary>
        public static Berichtstabelle Kaelteerzeuger(ErgebnisWaermepumpeModel wp, Func<int, string> traegername,
                                                     bool englisch, CultureInfo kultur)
        {
            var zeilen = (wp?.Module ?? new List<ErgebnisWaermepumpeModulModel>())
                .Where(m => m != null && m.Kaelteproduktion.HasValue && m.Kaelteproduktion.Value > 0).ToList();
            if (zeilen.Count == 0) return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(2600, 1200, 1300, 1000, 1300, 0);
            string[] titel = { "Anlage", "Kälte [MWh/a]", "Kältestrom [MWh/a]", "EER", "aus dem Netz [MWh/a]", "Stromträger" };
            t.MitKopf(titel.Select(x => new Tabellenzelle
            {
                Text = BerichtTexte.T(x, englisch), Fett = true, Hinterlegung = Tabellenhinterlegung.Stamm,
                Ausrichtung = Tabellenausrichtung.Links,
            }));

            Func<double?, string, Tabellenzelle> zahl = (w, einheit) => new Tabellenzelle
            {
                Text = w.HasValue ? Tabellenformat.F(w.Value, 2, kultur) : Tabellenzelle.STRICH,
                Zahl = w, Format = "N2", Einheit = einheit, Ausrichtung = Tabellenausrichtung.Rechts,
            };
            foreach (ErgebnisWaermepumpeModulModel m in zeilen)
            {
                double strom = m.Stromverbrauch_Kuehlung ?? 0.0;
                t.Zeile(new[]
                {
                    Zellen.Text(string.IsNullOrEmpty(m.Modul) ? Tabellenzelle.STRICH : m.Modul),
                    zahl(m.Kaelteproduktion.Value, "MWh/a"),
                    zahl(strom, "MWh/a"),
                    zahl(strom > 0 ? m.Kaelteproduktion.Value / strom : (double?)null, null),
                    zahl(m.Kaeltestrom_Netzbezug, "MWh/a"),
                    Zellen.Text(ProjektbeschreibungBaustein.KuehltraegerText(m, traegername)),
                });
            }
            return t;
        }

        /// <summary>
        /// Die Kennzahlen EINES Gebäudes aus dem Ergebnis des Laufs (Entscheid E30) als Beschriftung · Wert — Rechenweg,
        /// Wärmebedarf, die drei Spitzenwerte und auf dem VDI-Weg die Kühl- und Raumkennzahlen; die Tafel, die das
        /// Kapitel „Projektbeschreibung“ je Gebäude schreibt.
        /// </summary>
        public static Berichtstabelle Gebaeudeergebnis(ErgebnisGebaeudeModel g, bool englisch, CultureInfo kultur)
        {
            if (g == null) return Leer(nameof(RR.BV_GRUND_KEIN_GEBAEUDE), kultur);
            return Eigenschaftstabelle(Gebaeudepaare(g, kultur), englisch);
        }

        /// <summary>
        /// <b><c>tabelle.gebaeude.ergebnis</c></b> — die Kennzahlen aller Gebäude des Stamms in einer Tafel: je Gebäude eine
        /// Gruppenzeile mit seinem Namen, darunter seine Zeilen wie in <see cref="Gebaeudeergebnis"/>.
        /// </summary>
        public static Berichtstabelle Gebaeudeergebnisse(VariantenDaten stamm, bool englisch, CultureInfo kultur)
        {
            if (stamm == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAMM), kultur);
            List<ErgebnisGebaeudeModel> zeilen = ProjektbeschreibungBaustein.GebaeudeZeilen(stamm);
            if (zeilen.Count == 0) return Leer(nameof(RR.BV_GRUND_KEIN_GEBAEUDE), kultur);

            var t = new Berichtstabelle().Feste(2800, 0);
            foreach (ErgebnisGebaeudeModel g in zeilen)
            {
                t.Zeile(new[]
                {
                    Zellen.Text(string.IsNullOrWhiteSpace(g.Gebaeudename) ? Tabellenzelle.STRICH : g.Gebaeudename,
                                rolle: Tabellenrolle.Gruppe, fett: true, h: Tabellenhinterlegung.Kopf),
                    Zellen.Text("", rolle: Tabellenrolle.Gruppe, fett: true, h: Tabellenhinterlegung.Kopf),
                }, Tabellenrolle.Gruppe);
                foreach (Tabellenzeile z in Gebaeudeergebnis(g, englisch, kultur).Zeilen) t.Zeile(z.Zellen, z.Rolle);
            }
            return t;
        }

        private static IEnumerable<(string, Tabellenzelle)> Gebaeudepaare(ErgebnisGebaeudeModel g, CultureInfo kultur)
        {
            Tabellenzelle Zahl(double? w, int dez, string einheit) => new Tabellenzelle
            {
                Text = w.HasValue ? Tabellenformat.F(w.Value, dez, kultur) + " " + einheit : Tabellenzelle.STRICH,
                Zahl = w, Format = "N" + dez, Einheit = einheit,
            };
            yield return ("Rechenweg", Zellen.Text(ProjektbeschreibungBaustein.Rechenwegtext(g)));
            yield return ("Wärmebedarf Heizung", Zahl(g.HeizwaermeMwh, 1, "MWh/a"));
            yield return ("Spitzenlast (Stundenwert)", Zahl(g.SpitzeKw, 1, "kW"));
            yield return ("Spitzenlast (Tagesmittel)", Zahl(g.SpitzeTagesmittelKw, 1, "kW"));
            yield return ("Spitzenlast (95-%-Quantil)", Zahl(g.Spitze95Kw, 1, "kW"));
            if (!g.IstVdi6007) yield break;
            yield return ("Kühlenergie", Zahl(g.KuehlenergieMwh, 1, "MWh/a"));
            yield return ("Stunden mit Kühlbedarf", Zahl(g.KuehlstundenH, 0, "h/a"));
            yield return ("Mittlere Raumtemperatur (Nutzungszeit)", Zahl(g.MittlereRaumtemperaturC, 1, "°C"));
            yield return ("Überhitzungsstunden", Zahl(g.UeberhitzungsstundenH, 0, "h/a"));
        }

        /// <summary>
        /// <b><c>tabelle.anhang_e.checkliste</c></b> — die Checkliste nach DIN EN 17463 Anhang E: Nr., Thema, Anforderung,
        /// Stelle im Bericht, Stand und Beurteilung, je Gruppe eine Gruppenzeile. Die Stelle nennt die Überschrift vor dem
        /// Kapitel in DIESEM Bericht (<paramref name="kapitelstellen"/>), ohne sie die eigene Überschrift.
        /// </summary>
        public static Berichtstabelle AnhangE(List<ChecklistenPunkt> punkte, CultureInfo kultur)
        {
            if (punkte == null || punkte.Count == 0) return Leer(nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), kultur);
            var t = new Berichtstabelle { Schmal = true }.Feste(600, 1500, 2455, 2300, 1900, 600);
            string[] titel =
            {
                RR.WIRT_AE_SP_NR, RR.WIRT_AE_SP_THEMA, RR.WIRT_AE_SP_ANFORDERUNG,
                RR.WIRT_AE_SP_STELLE, RR.WIRT_AE_SP_STAND, RR.WIRT_AE_SP_NOTE
            };
            t.MitKopf(titel.Select(x => Zellen.Kopf(x, Tabellenausrichtung.Links)));
            string gruppe = null;
            foreach (ChecklistenPunkt pkt in punkte)
            {
                if (!string.Equals(gruppe, pkt.Gruppe, StringComparison.Ordinal))
                {
                    gruppe = pkt.Gruppe;
                    t.Zeile(Enumerable.Range(0, 6).Select(i => Zellen.Text(i == 0 ? gruppe : "", rolle: Tabellenrolle.Gruppe,
                                                                           fett: true, h: Tabellenhinterlegung.Stamm)),
                            Tabellenrolle.Gruppe);
                }
                t.Zeile(new[] { pkt.Nummer, pkt.Thema, pkt.Anforderung, pkt.Stelle, pkt.StandZeile, "" }.Select(w => Zellen.Text(w)));
            }
            return t;
        }

        /// <summary>
        /// <b><c>tabelle.speichertemperaturen</c></b> — je Speicher des Stamms mit Wert die mittlere und die kleinste
        /// Temperatur oben im Schichtmodell.
        /// </summary>
        public static Berichtstabelle Speichertemperaturen(VariantenDaten stamm, bool englisch, CultureInfo kultur)
        {
            if (stamm == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAMM), kultur);
            if (stamm.Ergebnis == null || stamm.Ergebnis.Pufferspeicher == null) return Leer(nameof(RR.BV_GRUND_KEIN_ERGEBNIS), kultur);
            List<ErgebnisPufferspeicherModel> mitWert = stamm.Ergebnis.Pufferspeicher
                .Where(p => p != null && p.T_oben_Mittel.HasValue).ToList();
            if (mitWert.Count == 0) return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(4155, 2600, 2600);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Speicher", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("T oben Mittel [°C]", englisch)),
                Zellen.Kopf(BerichtTexte.T("T oben Minimum [°C]", englisch)),
            });
            foreach (ErgebnisPufferspeicherModel p in mitWert)
                t.Zeile(new[]
                {
                    Zellen.Text(string.IsNullOrWhiteSpace(p.Bezeichner) ? Tabellenzelle.STRICH : p.Bezeichner),
                    new Tabellenzelle { Text = Tabellenformat.F(p.T_oben_Mittel.Value, 1, kultur), Zahl = p.T_oben_Mittel,
                                        Format = "N1", Einheit = "°C", Ausrichtung = Tabellenausrichtung.Rechts },
                    Zellen.Zahl(p.T_oben_Min.HasValue ? Tabellenformat.F(p.T_oben_Min.Value, 1, kultur) : Tabellenzelle.STRICH,
                                p.T_oben_Min, "N1", einheit: "°C"),
                });
            return t;
        }
    }
}
