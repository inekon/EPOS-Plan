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
