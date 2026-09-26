using System;
using System.Collections.Generic;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Katalog v4, Teil Tabellen</b> (<see cref="Vorlagenfeldkatalog.KATALOGFASSUNG"/> 4, Etappe BV-E5, Konzept
    /// Berichtsvorlagen 5.4, 6.4 Nr. 2, Anhang A) — die Strukturtabellen:
    /// <list type="bullet">
    /// <item><c>tabelle.*</c> (Kontext Bericht, Stamm oder Gruppe, gültig überall) und <c>stand.tabelle.*</c> (Kontext
    /// Stand, im Block <c>je stand</c>) — je Tabelle ein Bauweg aus <see cref="Berichtstabellen"/>, derselbe, über den
    /// der Baustein seine Tabelle schreibt; <c>tabelle.komponenten.kenndaten.&lt;gewerk&gt;</c> und
    /// <c>tabelle.vergleich.&lt;gruppe&gt;</c> aus den Gewerken bzw. Kennzahlgruppen erzeugt;</item>
    /// <item><c>muster.tabelle</c> — der Alternativtext der Mustertabelle, aus der der Vorlagenweg die Rollen liest;</item>
    /// <item>je Tabelle der Schalter <c>hat.tabelle.&lt;name&gt;</c> (<c>stand.</c> entfällt im Namen): trägt die Tabelle
    /// eine Zeile — im Block <c>je stand</c> die des laufenden Stands, außerhalb die irgendeines Stands.</item>
    /// </list>
    /// <para>Die Quelle liefert eine <see cref="Berichtstabelle"/>; eine leere trägt ihren Grund, die Engine schreibt
    /// dann den Leertext (Konzept 4.10). Gerechnet wird nichts, gelesen nur der Wertesatz.</para>
    /// </summary>
    public static partial class Vorlagenfeldkatalog
    {
        /// <summary>Die Fassung der Tabellen (Etappe BV-E5).</summary>
        private const int FASSUNG_TABELLEN = 4;

        /// <summary>Die Fassung der drei Tabellen mit reiner Excel-Quelle (Etappe BV-E9, Katalog v7).</summary>
        private const int FASSUNG_EXCEL_TABELLEN = 7;

        /// <summary>Der Alternativtext der Mustertabelle (Konzept 6.4 Nr. 2).</summary>
        public const string MUSTER_TABELLE = "muster.tabelle";

        /// <summary>Musterschlüssel der Kenndatentafeln je Gewerk.</summary>
        public const string MUSTER_TABELLE_KENNDATEN = "tabelle.komponenten.kenndaten.<gewerk>";

        /// <summary>Musterschlüssel der Kennzahltafeln je Gruppe des Vergleichs.</summary>
        public const string MUSTER_TABELLE_VERGLEICH = "tabelle.vergleich.<gruppe>";

        /// <summary>Musterschlüssel der Schalter je Tabelle.</summary>
        public const string MUSTER_HAT_TABELLE = "hat.tabelle.<name>";

        /// <summary>Vorsilbe der Schalter je Tabelle.</summary>
        public const string PRAEFIX_TABELLENSCHALTER = "hat.tabelle.";

        /// <summary>Vorsilbe der Tabellen je Stand.</summary>
        private const string STAND_TABELLE = "stand.tabelle.";

        /// <summary>Eine Tabelle des Katalogs: Schlüssel, Kontext, Bauweg und Bedarf.</summary>
        private sealed class Tabellenquelle
        {
            internal string Schluessel;
            internal Vorlagenfeldkontext Kontext;
            internal Func<Berichtswerte, Berichtstabelle> Bau;
            internal Vorlagenbedarf Bedarf;
            internal Vorlagenfeldableitung Ableitung;
            internal int Seit = FASSUNG_TABELLEN;
            internal Vorlagenausgabe Ausgaben = Vorlagenausgabe.Beide;
        }

        /// <summary>Ist der Schlüssel eine Tabelle je Stand?</summary>
        internal static bool IstStandtabelle(string schluessel)
        {
            return schluessel != null && schluessel.StartsWith(STAND_TABELLE, StringComparison.Ordinal);
        }

        /// <summary>Der Name des Schalters einer Tabelle: <c>hat.</c> + Schlüssel ohne <c>stand.</c>.</summary>
        public static string SchalterDerTabelle(string schluessel)
        {
            string name = IstStandtabelle(schluessel) ? schluessel.Substring(STAND.Length) : schluessel;
            return "hat." + name;
        }

        /// <summary>Die Tabellen, die Mustertabelle und die Schalter je Tabelle (Katalog v4).</summary>
        private static IEnumerable<Vorlagenfeld> Tabellen()
        {
            List<Tabellenquelle> quellen = Tabellenquellen().ToList();
            foreach (Tabellenquelle q in quellen)
            {
                Func<Berichtswerte, Berichtstabelle> bau = q.Bau;
                yield return new Vorlagenfeld(q.Schluessel, Vorlagenfeldart.Tabelle, q.Kontext, w => bau(w))
                {
                    Seit = q.Seit,
                    Leerwert = Vorlagenfeld.STRICH,
                    Bedarf = q.Bedarf,
                    Ableitung = q.Ableitung,
                    Ausgaben = q.Ausgaben,
                };
            }

            yield return new Vorlagenfeld(MUSTER_TABELLE, Vorlagenfeldart.Tabelle, Vorlagenfeldkontext.Bericht, w => null)
            {
                Seit = FASSUNG_TABELLEN,
                Leerwert = Vorlagenfeld.STRICH,
                Ausgaben = Vorlagenausgabe.Word,
            };

            // Die Schalter je Tabelle nur für Tabellen mit Ausgabe Word: Excel kennt keine Schalter (Konzept 4.4).
            foreach (Tabellenquelle q in quellen.Where(q => (q.Ausgaben & Vorlagenausgabe.Word) != 0))
            {
                Tabellenquelle quelle = q;
                bool jeStand = q.Kontext == Vorlagenfeldkontext.Stand;
                string tabelle = q.Schluessel;
                yield return new Vorlagenfeld(SchalterDerTabelle(q.Schluessel), Vorlagenfeldart.Schalter, Vorlagenfeldkontext.Gruppe,
                    w => jeStand ? Hat(w, v => HatZeilen(quelle.Bau(w.MitStand(v)))) : (object)HatZeilen(quelle.Bau(w)))
                {
                    Seit = FASSUNG_TABELLEN,
                    Bedarf = q.Bedarf,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_HAT_TABELLE, nameof(R.VF_MUSTER_HAT_TABELLE), tabelle)
                    {
                        Bezeichnung = k => "{{" + tabelle + "}}",
                    },
                };
            }
        }

        private static bool HatZeilen(Berichtstabelle t) { return t != null && !t.IstLeer; }

        /// <summary>Die Tabellen des Katalogs in Katalogreihenfolge.</summary>
        private static IEnumerable<Tabellenquelle> Tabellenquellen()
        {
            const Vorlagenfeldkontext B = Vorlagenfeldkontext.Bericht;
            const Vorlagenfeldkontext ST = Vorlagenfeldkontext.Stamm;
            const Vorlagenfeldkontext G = Vorlagenfeldkontext.Gruppe;
            const Vorlagenfeldkontext S = Vorlagenfeldkontext.Stand;
            Tabellenquelle Q(string schluessel, Vorlagenfeldkontext kontext, Func<Berichtswerte, Berichtstabelle> bau,
                             Vorlagenbedarf bedarf = Vorlagenbedarf.Keiner)
                => new Tabellenquelle { Schluessel = schluessel, Kontext = kontext, Bau = bau, Bedarf = bedarf };
            Func<Berichtswerte, Berichtstabelle> jeStand(Func<Berichtswerte, VariantenDaten, Berichtstabelle> bau)
                => w => w.LaufenderStand == null ? Berichtstabellen.Leer(nameof(R.BV_GRUND_KEIN_STAND), w.Kultur) : bau(w, w.LaufenderStand);

            // ---------------- Bericht, Komponenten, Projekt ----------------
            yield return Q("tabelle.varianten", B, w => Berichtstabellen.Varianten(w.Daten, v => Stromspeichertext(w, v), w.Englisch, w.Kultur));
            yield return Q("tabelle.komponenten.matrix", B, w => Berichtstabellen.Komponentenmatrix(w.Daten, w.Englisch, w.Kultur));
            foreach ((string gewerk, string name) in Berichtstabellen.Gewerke)
            {
                string g = gewerk;
                Tabellenquelle q = Q("tabelle.komponenten.kenndaten." + name, B, w => Berichtstabellen.Kenndaten(w.Daten, g, w.Englisch, w.Kultur));
                q.Ableitung = new Vorlagenfeldableitung(MUSTER_TABELLE_KENNDATEN, nameof(R.VF_MUSTER_TABELLE_KENNDATEN), name)
                {
                    Bezeichnung = k => BerichtTexte.T(g, k.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase)),
                };
                yield return q;
            }
            yield return Q("tabelle.kaelteerzeuger", ST, w => Berichtstabellen.Kaelteerzeuger(w.Stamm?.Ergebnis?.Waermepumpe,
                id => w.Wirtschaft.Traegername(id), w.Englisch, w.Kultur));
            yield return Q("tabelle.speichertemperaturen", ST, w => Berichtstabellen.Speichertemperaturen(w.Stamm, w.Englisch, w.Kultur));
            yield return Q("tabelle.gebaeude.ergebnis", ST, w => Berichtstabellen.Gebaeudeergebnisse(w.Stamm, w.Englisch, w.Kultur));

            // ---------------- Variantenvergleich ----------------
            yield return Q("tabelle.vergleich", G, w => Berichtstabellen.Vergleichsgesamt(w.Daten, w.Englisch, w.Kultur));
            foreach ((string gruppe, string name) in Berichtstabellen.Vergleichsgruppen)
            {
                string gr = gruppe;
                Tabellenquelle q = Q("tabelle.vergleich." + name, G, w => Berichtstabellen.Vergleichsgruppe(w.Daten, gr, w.Englisch, w.Kultur));
                q.Ableitung = new Vorlagenfeldableitung(MUSTER_TABELLE_VERGLEICH, nameof(R.VF_MUSTER_TABELLE_VERGLEICH), name)
                {
                    Bezeichnung = k => BerichtTexte.T(gr, k.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase)),
                };
                yield return q;
            }
            yield return Q("tabelle.vergleich.delta_prozent", G, w => Berichtstabellen.DeltaProzent(w.Daten, w.Englisch, w.Kultur));

            // ---------------- Wirtschaftlichkeit (Gruppe) ----------------
            yield return Q("tabelle.wirtschaft.kennzahlen", G, w => Berichtstabellen.Wirtschaftskennzahlen(
                w.Daten, w.Wirtschaft, WirtschaftlichkeitSzenario.ERWARTET, w.Englisch, w.Kultur));
            yield return Q("tabelle.wirtschaft.kennzahlen.guenstig", G, w => Berichtstabellen.Wirtschaftskennzahlen(
                w.Daten, w.Wirtschaft, WirtschaftlichkeitSzenario.BEST, w.Englisch, w.Kultur));
            yield return Q("tabelle.wirtschaft.kennzahlen.unguenstig", G, w => Berichtstabellen.Wirtschaftskennzahlen(
                w.Daten, w.Wirtschaft, WirtschaftlichkeitSzenario.WORST, w.Englisch, w.Kultur));
            yield return Q("tabelle.wirtschaft.szenarien", G, w => w.Wirtschaft.Ergebnisse.Count == 0
                ? Berichtstabellen.Leer(nameof(R.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), w.Kultur)
                : Berichtstabellen.Szenarien(w.Wirtschaft.Bewertung, w.Englisch, w.Kultur));
            yield return Q("tabelle.wirtschaft.nicht_monetaer", G, w => w.Wirtschaft.Ergebnisse.Count == 0
                ? Berichtstabellen.Leer(nameof(R.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), w.Kultur)
                : Berichtstabellen.NichtMonetaer(w.Wirtschaft.Bewertung?.Wirkungen, w.Kultur));

            // ---------------- Anhang ----------------
            yield return Q("tabelle.anhang.simulationsstaende", B, w => Berichtstabellen.Simulationsstaende(w.Daten, w.Englisch, w.Kultur));
            yield return Q("tabelle.anhang_e.checkliste", B, w => w.Wirtschaft.Ergebnisse.Count == 0
                ? Berichtstabellen.Leer(nameof(R.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), w.Kultur)
                : Berichtstabellen.AnhangE(AnhangECheckliste.AusBericht(w.Daten, w.Kapitelstellen), w.Kultur));

            // ---------------- je Stand ----------------
            yield return Q(STAND_TABELLE + "kennzahlen", S, jeStand((w, v) => Berichtstabellen.Standkennzahlen(v, w.Englisch, w.Kultur)));
            yield return Q(STAND_TABELLE + "abweichungen", S, jeStand((w, v) => Berichtstabellen.Abweichungen(v, w.Englisch, w.Kultur)));
            yield return Q(STAND_TABELLE + "erzeuger", S, jeStand((w, v) => Berichtstabellen.Erzeuger(v, w.Englisch, w.Kultur)));
            yield return Q(STAND_TABELLE + "brennstoffmengen", S, jeStand((w, v) => Berichtstabellen.Brennstoffmengen(v, false, w.Englisch, w.Kultur)));
            yield return Q(STAND_TABELLE + "kwkg_module", S, jeStand((w, v) => Berichtstabellen.KwkgModule(v, w.Wirtschaft.Ergebnisse, w.Kultur)));
            yield return Q(STAND_TABELLE + "betriebskosten", S, jeStand((w, v) => Berichtstabellen.Betriebskosten(v, w.Wirtschaft.Ergebnisse, w.Kultur)));
            yield return Q(STAND_TABELLE + "mehrjahres", S, jeStand((w, v) => Berichtstabellen.Mehrjahres(v, w.Wirtschaft, w.Kultur)),
                           Vorlagenbedarf.Verlauf);
            yield return Q(STAND_TABELLE + "vermiedene_kosten", S, jeStand((w, v) => Berichtstabellen.VermiedeneKosten(v, w.Wirtschaft.Ergebnisse, w.Kultur)));
            yield return Q(STAND_TABELLE + "sensitivitaet", S, jeStand((w, v) => Berichtstabellen.Sensitivitaet(
                v, w.Wirtschaft.Bewertung?.Sensitivitaet, w.Englisch, w.Kultur)));
            yield return Q(STAND_TABELLE + "strommengen", S, jeStand((w, v) => Berichtstabellen.Strommengen(
                v, w.Wirtschaft.Ergebnisse.Count == 0 ? null : w.Wirtschaft.Strommatrizen, w.Englisch, w.Kultur)));
            yield return Q(STAND_TABELLE + "emissionsbilanz", S, jeStand((w, v) => Berichtstabellen.Emissionsbilanz(v, w.Wirtschaft, w.Englisch, w.Kultur)),
                           Vorlagenbedarf.Emissionsbilanz);

            // ---------------- nur Excel (Katalog v7, BV-E9): die drei Tabellen mit reiner Excel-Quelle ----------------
            Tabellenquelle X(Tabellenquelle q) { q.Seit = FASSUNG_EXCEL_TABELLEN; q.Ausgaben = Vorlagenausgabe.Excel; return q; }
            yield return X(Q("tabelle.wirtschaft.parameter", G, w => Berichtstabellen.Parameter(w.Daten, w.Wirtschaft, w.Kultur)));
            yield return X(Q("tabelle.wirtschaft.verlauf", G, w => Berichtstabellen.Verlauf(w.Wirtschaft, w.Kultur), Vorlagenbedarf.Verlauf));
            yield return X(Q(STAND_TABELLE + "monatswerte", S, jeStand((w, v) => Berichtstabellen.Monatswerte(v, w.Kultur)),
                             Vorlagenbedarf.Zeitreihen));
        }

        /// <summary>Der Stromspeicher eines Stands wie <c>stand.stromspeicher</c> (die Zeile <c>SPEICHER_KONTEXT</c>).</summary>
        private static string Stromspeichertext(Berichtswerte w, VariantenDaten v)
        {
            WirtschaftsBerichtswerte werte = w.Wirtschaft;
            if (werte.Ergebnisse.Count == 0) return null;
            WirtZeile z = Zeile(werte, "SPEICHER_KONTEXT");
            WirtschaftlichkeitErgebnis e = werte.Erwartet(v.IdProjekt);
            return z == null || e == null ? null : z.Text(e);
        }
    }
}
