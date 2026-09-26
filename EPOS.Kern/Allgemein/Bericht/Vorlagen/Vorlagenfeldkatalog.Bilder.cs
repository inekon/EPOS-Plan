using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Katalog v4 — die Bildplatzhalter</b> (<see cref="Vorlagenfeldkatalog.KATALOGFASSUNG"/> 4, Etappe BV-E5,
    /// Konzept Berichtsvorlagen 4.2, 4.6 BV-P5, 5.4, 6.5, Anhang A): die dreizehn Bilder des Wortberichts als
    /// Schlüssel — je Stand <c>stand.bild.*</c> (Kontext Stand, im Block <c>je stand</c>), für das Stammprojekt
    /// <c>stamm.bild.speichertemperaturen</c>, über die Gruppe <c>bild.vergleich.balken.&lt;k&gt;</c> und
    /// <c>bild.wirtschaft.*</c> — und je Bild der Schalter <c>hat.bild.&lt;name&gt;</c> („das Bild hat ein
    /// Modell“; im Block <c>je stand</c> für den laufenden Stand, sonst für den ganzen Bericht).
    ///
    /// <para><b>Die Quelle liefert ein <see cref="Diagrammbild"/></b>, kein fertiges Bild: Die Engine zeichnet
    /// es im Zielmaß des Rahmens (Stufe 2). Die Modelle baut <see cref="Berichtsbilder"/> — dieselben Aufrufe wie
    /// die Bausteine, nur aus <see cref="BerichtsDaten"/>. Die Zeitreihen- und Verlaufsbilder tragen ihren
    /// <see cref="Vorlagenfeld.Bedarf"/>, damit <see cref="Berichtsbedarf.AusVorlage"/> die Reihen erhebt; hat
    /// der Lauf sie nicht erhoben, holt ein Bild sie nicht nach, sondern bleibt mit Grund leer.</para>
    ///
    /// <para><b>Vorgemerkt</b> bleiben die App-Diagramme ohne Berichtsziel (Anhang A, Konzept 5.4): Sie stehen
    /// nicht im Katalog, sondern in <see cref="VorgemerkteBilder"/> — der Prüfer meldet sie als „erst in einer
    /// späteren Programmfassung“, statt sie als unbekannt zu behandeln.</para>
    /// </summary>
    public static partial class Vorlagenfeldkatalog
    {
        /// <summary>Die Fassung der Bildplatzhalter (Etappe BV-E5, Katalog v4).</summary>
        private const int FASSUNG_BILDER = 4;

        /// <summary>Vorsilbe der Bildschalter.</summary>
        public const string PRAEFIX_BILDSCHALTER = "hat.bild.";

        /// <summary>Musterschlüssel der Vergleichsbalken.</summary>
        public const string MUSTER_BILD_VERGLEICH_BALKEN = "bild.vergleich.balken.<k>";

        /// <summary>Musterschlüssel der Schalter der Vergleichsbalken.</summary>
        public const string MUSTER_HAT_BILD_VERGLEICH_BALKEN = "hat.bild.vergleich.balken.<k>";

        /// <summary>Bildpunkte des Modells je Anzeigepunkt bei den breiten Bildern (1240 → 620).</summary>
        private const double FAKTOR_BREIT = 2.0;

        /// <summary>
        /// Die vorgemerkten App-Diagramme ohne Berichtsziel (Anhang A): Schlüssel, die eine spätere Fassung füllt.
        /// Sie sind keine Katalogeinträge.
        /// </summary>
        public static readonly IReadOnlyList<string> VorgemerkteBilder = new[]
        {
            "bild.kosten.profil", "bild.klimadaten.jahresgang", "bild.waermequelle.jahresgang",
            "bild.waermepumpe.kennlinie.cop", "bild.waermepumpe.kennlinie.leistung",
            "bild.speicherflotte.optimierungsraster", "bild.speicherflotte.schnittkurve",
            "bild.speicherflotte.stueckzahlkurve", "bild.speicherflotte.jahresprojektion",
            "bild.zapfprofil.tagesgang", "bild.zapfprofil.wochenprofil", "bild.zapfprofil.jahresgang",
            "bild.zapfprofil.dauerlinie", "bild.gebaeude.raumtemperatur",
            "bild.ergebnis.erzeugerstapel", "bild.ergebnis.streuwolke", "bild.ergebnis.temperaturverlauf",
            "bild.ergebnis.jahresverlauf", "bild.ergebnis.ganglinie_normiert", "bild.ergebnis.monatsstapel",
            "bild.ergebnis.stundenprofil", "bild.peakshaving.lastgang",
        };

        /// <summary>Je Bildschlüssel die Bildpunkte je Anzeigepunkt und die Mindestbreite (für den Prüfer).</summary>
        private static readonly Dictionary<string, (double Faktor, int Mindestbreite)> _bildgroessen =
            new Dictionary<string, (double, int)>(StringComparer.Ordinal);

        /// <summary>
        /// <b>Die Stufe 2 am Rahmen</b> (Konzept 6.5, 6.8): der Anteil der Rahmenbreite an der Breite, in der das
        /// Bild gezeichnet wird — 1, solange der Renderer in der Zielgröße zeichnet; darunter (der Rahmen ist
        /// schmaler als die Mindestbreite des Bildes) greift Stufe 1, und die Schrift schrumpft um diesen Anteil.
        /// <c>null</c> für einen Schlüssel ohne Diagramm (etwa das Logo) oder ohne Rahmenbreite.
        /// </summary>
        public static double? Rahmenanteil(string schluessel, long rahmenBreiteEmu)
        {
            Vorlagenfeld feld = Finde(schluessel);
            if (feld == null || rahmenBreiteEmu <= 0) return null;
            if (!_bildgroessen.TryGetValue(feld.Schluessel, out var g)) return null;
            double ziel = rahmenBreiteEmu / (double)Wordbilder.EMU_JE_PIXEL * g.Faktor;
            return Math.Min(1.0, ziel / Math.Max(ziel, g.Mindestbreite));
        }

        /// <summary>Ist der Schlüssel ein vorgemerktes App-Diagramm (<see cref="VorgemerkteBilder"/>)?</summary>
        public static bool IstVorgemerktesBild(string schluessel)
        {
            string normiert = Platzhaltersyntax.NormiereSchluessel(schluessel);
            return VorgemerkteBilder.Contains(normiert, StringComparer.Ordinal);
        }

        // =====================================================================
        //  Die Einträge
        // =====================================================================

        /// <summary>Die Bildplatzhalter und ihre Schalter der Fassung 4.</summary>
        private static IEnumerable<Vorlagenfeld> Bilder(List<Kennzahl> kennzahlen)
        {
            const Vorlagenfeldkontext S = Vorlagenfeldkontext.Stand;
            var l = new List<Vorlagenfeld>();

            // ---- je Stand: Ganglinien, Deckung, Zahlungsstrom ----
            l.AddRange(Bild("stand.bild.waerme_jahresverlauf", S, Vorlagenbedarf.Zeitreihen, true,
                w => MitStand(w, v => Zeitreihenbild(w, v, Berichtsbilder.JahresverlaufWaerme))));
            l.AddRange(Bild("stand.bild.waerme_dauerlinie", S, Vorlagenbedarf.Zeitreihen, true,
                w => MitStand(w, v => Zeitreihenbild(w, v, Berichtsbilder.DauerlinieWaerme))));
            l.AddRange(Bild("stand.bild.strombilanz_monate", S, Vorlagenbedarf.Zeitreihen, true,
                w => MitStand(w, v => Zeitreihenbild(w, v, Berichtsbilder.StrombilanzMonate))));
            l.AddRange(Bild("stand.bild.speicherverlauf", S, Vorlagenbedarf.Zeitreihen, true,
                w => MitStand(w, v => Zeitreihenbild(w, v, Berichtsbilder.Speicherverlauf))));
            l.AddRange(Bild("stand.bild.deckung_waerme", S, Vorlagenbedarf.Keiner, true,
                w => MitStand(w, v => Deckungsbild(w, v, true))));
            l.AddRange(Bild("stand.bild.deckung_strom", S, Vorlagenbedarf.Keiner, true,
                w => MitStand(w, v => Deckungsbild(w, v, false))));
            l.AddRange(Bild("stand.bild.zahlungsstrom", S, Vorlagenbedarf.Verlauf, true,
                w => MitStand(w, v => Zahlungsstrombild(w, v))));

            // ---- Stammprojekt ----
            l.AddRange(Bild("stamm.bild.speichertemperaturen", Vorlagenfeldkontext.Stamm, Vorlagenbedarf.Zeitreihen, false,
                w => w.Stamm == null ? Grund(w, nameof(R.BV_GRUND_KEIN_STAMM))
                                     : Zeitreihenbild(w, w.Stamm, Berichtsbilder.Speichertemperaturen)));

            // ---- über die Gruppe: Vergleichsbalken je Schlüsselkennzahl ----
            foreach (string k in Berichtsbilder.Balkenkennzahlen)
            {
                if (!kennzahlen.Any(x => x.Schluessel == k)) continue;
                string schluessel = k;
                Func<Berichtswerte, object> quelle = w => Vergleichsbild(w, schluessel);
                l.Add(new Vorlagenfeld("bild.vergleich.balken." + k, Vorlagenfeldart.Bild, Vorlagenfeldkontext.Gruppe, quelle)
                {
                    Seit = FASSUNG_BILDER,
                    Leerwert = "",
                    Ausgaben = Vorlagenausgabe.Word,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_BILD_VERGLEICH_BALKEN, nameof(R.VF_MUSTER_BILD_VERGLEICH_BALKEN), k),
                });
                l.Add(new Vorlagenfeld(PRAEFIX_BILDSCHALTER + "vergleich.balken." + k, Vorlagenfeldart.Schalter,
                                       Vorlagenfeldkontext.Gruppe, w => HatModell(quelle(w)))
                {
                    Seit = FASSUNG_BILDER,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_HAT_BILD_VERGLEICH_BALKEN, nameof(R.VF_MUSTER_HAT_BILD_VERGLEICH_BALKEN), k),
                });
                _bildgroessen["bild.vergleich.balken." + k] = (FAKTOR_BREIT, Bildmass.MIN_BREITE);
            }

            // ---- über die Gruppe: Wirtschaftlichkeit ----
            const Vorlagenfeldkontext G = Vorlagenfeldkontext.Gruppe;
            l.AddRange(Bild("bild.wirtschaft.kapitalwert_szenarien", G, Vorlagenbedarf.Verlauf, false,
                w => Wirtschaftsbild(w, true, ChartRenderer.SZENARIEN_MIN_BREITE, nameof(R.BV_GRUND_BILD_OHNE_DATEN),
                                     (werte, verlauf, m) => Berichtsbilder.KapitalwertSzenarien(verlauf, m))));
            l.AddRange(Bild("bild.wirtschaft.barwerte_kumuliert", G, Vorlagenbedarf.Verlauf, false,
                w => Wirtschaftsbild(w, true, Bildmass.MIN_BREITE, nameof(R.BV_GRUND_BILD_OHNE_DATEN),
                                     (werte, verlauf, m) => Berichtsbilder.BarwerteKumuliert(verlauf, m))));
            l.AddRange(Bild("bild.wirtschaft.bruecke", G, Vorlagenbedarf.Verlauf, false,
                w => Wirtschaftsbild(w, true, ChartRenderer.BRUECKE_MIN_BREITE, nameof(R.BV_GRUND_KEINE_LEITVERSION),
                                     (werte, verlauf, m) => Berichtsbilder.Bruecke(w.Daten, verlauf, werte.Ergebnisse,
                                         werte.Parameter, werte.Bewertung, w.Kultur, m))));
            l.AddRange(Bild("bild.wirtschaft.spanne", G, Vorlagenbedarf.Keiner, false, Spannenbild));

            // Die Größen der handgepflegten Bilder für den Prüfer.
            foreach (string schluessel in new[]
                     {
                         "stand.bild.waerme_jahresverlauf", "stand.bild.waerme_dauerlinie", "stand.bild.strombilanz_monate",
                         "stand.bild.speicherverlauf", "stand.bild.zahlungsstrom", "stamm.bild.speichertemperaturen",
                         "bild.wirtschaft.barwerte_kumuliert",
                     })
                _bildgroessen[schluessel] = (FAKTOR_BREIT, Bildmass.MIN_BREITE);
            double faktorKuchen = Berichtsbilder.MODELL_BREITE_KUCHEN / (double)Berichtsbilder.ANZEIGE_BREITE_KUCHEN;
            _bildgroessen["stand.bild.deckung_waerme"] = (faktorKuchen, Bildmass.MIN_BREITE);
            _bildgroessen["stand.bild.deckung_strom"] = (faktorKuchen, Bildmass.MIN_BREITE);
            _bildgroessen["bild.wirtschaft.kapitalwert_szenarien"] = (FAKTOR_BREIT, ChartRenderer.SZENARIEN_MIN_BREITE);
            _bildgroessen["bild.wirtschaft.bruecke"] = (FAKTOR_BREIT, ChartRenderer.BRUECKE_MIN_BREITE);
            _bildgroessen["bild.wirtschaft.spanne"] = (FAKTOR_BREIT, ChartRenderer.SPANNE_MIN_BREITE);
            return l;
        }

        /// <summary>
        /// Ein handgepflegter Bildschlüssel und sein Schalter <c>hat.bild.&lt;name&gt;</c> (der Schlüssel ohne
        /// <c>stand.</c>/<c>stamm.</c> und ohne <c>bild.</c>). Der Schalter eines Bildes je Stand
        /// (<paramref name="jeStand"/>) gilt im Standblock für den laufenden Stand, sonst für irgendeinen.
        /// </summary>
        private static IEnumerable<Vorlagenfeld> Bild(string schluessel, Vorlagenfeldkontext kontext, Vorlagenbedarf bedarf,
                                                      bool jeStand, Func<Berichtswerte, object> quelle)
        {
            yield return new Vorlagenfeld(schluessel, Vorlagenfeldart.Bild, kontext, quelle)
            {
                Seit = FASSUNG_BILDER,
                Leerwert = "",
                Ausgaben = Vorlagenausgabe.Word,
                Bedarf = bedarf,
            };
            Func<Berichtswerte, object> schalter = jeStand
                ? (Func<Berichtswerte, object>)(w => Hat(w, v => HatModell(quelle(w.MitStand(v)))))
                : (w => HatModell(quelle(w)));
            yield return new Vorlagenfeld(SchalterDesBildes(schluessel), Vorlagenfeldart.Schalter, Vorlagenfeldkontext.Gruppe, schalter)
            {
                Seit = FASSUNG_BILDER,
                Bedarf = bedarf,
            };
        }

        /// <summary>
        /// Der Name des Schalters eines Bildes: <c>hat.bild.</c> + der Schlüssel ohne <c>stand.</c>/<c>stamm.</c> und ohne
        /// <c>bild.</c> (<c>stand.bild.deckung_waerme</c> → <c>hat.bild.deckung_waerme</c>).
        /// </summary>
        public static string SchalterDesBildes(string schluessel)
        {
            int i = schluessel.IndexOf("bild.", StringComparison.Ordinal);
            return PRAEFIX_BILDSCHALTER + (i < 0 ? schluessel : schluessel.Substring(i + "bild.".Length));
        }

        /// <summary>Hat der Wert einer Bildquelle ein Modell (Schalter <c>hat.bild.*</c>)? Ein Fehler beim Bauen heißt nein.</summary>
        private static bool HatModell(object roh)
        {
            if (!(roh is Diagrammbild d)) return false;
            try { return d.Baue(null) != null; }
            catch (Exception) { return false; }
        }

        // =====================================================================
        //  Quellen — sie lesen nur den Wertesatz, nie die Datenbank
        // =====================================================================

        /// <summary>
        /// Ein Bild aus dem Zeitreihensatz eines Stands. Ohne Reihen der Grund „keine Stundenreihen“ — das Bild
        /// holt sie nicht nach (Konzept 8.5: der Bedarf des Laufs entscheidet).
        /// </summary>
        private static object Zeitreihenbild(Berichtswerte w, VariantenDaten v,
                                             Func<ZeitreihenSatz, Bildmass?, Zeichenmodell> bau)
        {
            ZeitreihenSatz z = v.Zeitreihen;
            if (z == null) return Grund(w, nameof(R.BV_GRUND_KEINE_ZEITREIHEN));
            return new Diagrammbild(m => bau(z, m), FAKTOR_BREIT, Bildmass.MIN_BREITE,
                                    w.Text(nameof(R.BV_GRUND_BILD_OHNE_DATEN)));
        }

        /// <summary>Der Deckungskuchen eines Stands (Wärme oder Strom); ohne Ergebnis oder Anteil mit Grund.</summary>
        private static object Deckungsbild(Berichtswerte w, VariantenDaten v, bool waerme)
        {
            ErgebnisModel e = v.Ergebnis;
            if (e == null) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            List<ChartRenderer.Segment> segmente = waerme ? Berichtsbilder.Waermedeckung(e) : Berichtsbilder.Stromdeckung(e);
            if (segmente == null || segmente.Count == 0) return Grund(w, nameof(R.BV_GRUND_BILD_OHNE_DATEN));
            return new Diagrammbild(m => Berichtsbilder.Deckung(e, waerme, m),
                                    Berichtsbilder.MODELL_BREITE_KUCHEN / (double)Berichtsbilder.ANZEIGE_BREITE_KUCHEN,
                                    Bildmass.MIN_BREITE, w.Text(nameof(R.BV_GRUND_BILD_OHNE_DATEN)));
        }

        /// <summary>Das Balkenbild einer Schlüsselkennzahl über die Stände; mit weniger als zwei Werten der Grund.</summary>
        private static object Vergleichsbild(Berichtswerte w, string schluessel)
        {
            List<VariantenDaten> staende = w.Daten.Varianten ?? new List<VariantenDaten>();
            if (Berichtsbilder.Vergleichsbalken(staende, schluessel).Count < 2)
                return Grund(w, nameof(R.BV_GRUND_ZU_WENIG_STAENDE));
            IReadOnlyList<Kennzahl> katalog = w.Kennzahlen;
            bool englisch = w.Englisch;
            return new Diagrammbild(m => Berichtsbilder.Vergleich(staende, katalog, schluessel, englisch, m),
                                    FAKTOR_BREIT, Bildmass.MIN_BREITE, w.Text(nameof(R.BV_GRUND_BILD_OHNE_DATEN)));
        }

        /// <summary>
        /// Ein Bild der Wirtschaftlichkeit. Mit <paramref name="verlaufNoetig"/> braucht es den Kapitalwertverlauf
        /// des Laufs: Hat der Sammler ihn nicht erhoben (Bedarf), wird er NICHT nachgeholt — das Bild bleibt mit
        /// Grund leer; braucht er Stundenreihen, die fehlen, oder scheiterte die Rechnung, ebenso.
        /// </summary>
        private static object Wirtschaftsbild(Berichtswerte w, bool verlaufNoetig, int mindestbreite, string grundOhneModell,
            Func<WirtschaftsBerichtswerte, WirtschaftlichkeitVerlaufSzenarien, Bildmass?, Zeichenmodell> bau)
        {
            return MitErgebnissen(w, werte =>
            {
                WirtschaftlichkeitVerlaufSzenarien verlauf = null;
                if (verlaufNoetig)
                {
                    if (werte.Gesammelt && (werte.Bedarf == null || !werte.Bedarf.Verlauf))
                        return Grund(w, nameof(R.BV_GRUND_VERLAUF_NICHT_ERHOBEN));
                    if (werte.VerlaufEntfaellt) return Grund(w, nameof(R.BV_GRUND_VERLAUF_ENTFAELLT));
                    verlauf = werte.Verlauf;
                    if (verlauf == null) return Grund(w, nameof(R.BV_GRUND_KEIN_VERLAUF));
                }
                WirtschaftlichkeitVerlaufSzenarien v = verlauf;
                return new Diagrammbild(m => bau(werte, v, m), FAKTOR_BREIT, mindestbreite, w.Text(grundOhneModell));
            });
        }

        /// <summary>Das Spannenbild der Bewertung; ohne Varianten (leere Bandbreite) der Grund „nur Stamm“.</summary>
        private static object Spannenbild(Berichtswerte w)
        {
            return MitErgebnissen(w, werte =>
            {
                WirtschaftlichkeitBandbreite band = werte.Bewertung?.Bandbreite;
                if (band == null || band.Leer) return Grund(w, nameof(R.BV_GRUND_NUR_STAMM));
                return new Diagrammbild(m => Berichtsbilder.Spanne(band, m), FAKTOR_BREIT, ChartRenderer.SPANNE_MIN_BREITE,
                                        w.Text(nameof(R.BV_GRUND_BILD_OHNE_DATEN)));
            });
        }

        /// <summary>Das Zahlungsstrombild eines Stands aus seiner Mehrjahrestafel im Erwartungsfall des Verlaufs.</summary>
        private static object Zahlungsstrombild(Berichtswerte w, VariantenDaten v)
        {
            return Wirtschaftsbild(w, true, Bildmass.MIN_BREITE, nameof(R.BV_GRUND_BILD_OHNE_DATEN),
                                   (werte, verlauf, m) => Berichtsbilder.Zahlungsstrom(
                                       Berichtsbilder.Mehrjahrestafel(verlauf, v.IdProjekt), v.Anzeige, w.Kultur, m));
        }
    }
}
