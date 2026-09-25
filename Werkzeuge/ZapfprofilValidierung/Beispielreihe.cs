using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Die synthetische Messreihe des Beispiels</b> — der wiederholbare Weg, mit dem
    /// <c>Beispiel/&lt;Kennung&gt;/messreihe.csv</c> entstanden ist (Schalter <c>--beispielreihe</c>).
    ///
    /// <para><b>Sie stammt aus der Rechnung selbst</b>, nicht aus einer Messung: die
    /// deterministische Jahresreihe des Objekts, mal einem <b>festen</b> Rauschen
    /// (Kongruenzgenerator mit fester Saat, ± 15 % je Stunde), mit <b>unveränderter Jahresenergie</b>
    /// und mit einer Spitze, die in die <b>Mitte des P85–P95-Bands</b> der gerechneten Dauerlinie
    /// gekappt ist. Damit ist das Beispiel grün <b>durch Konstruktion</b>; es prüft den Weg des
    /// Werkzeugs, nicht den Rechenweg des Generators. Echte Messreihen liegen beim Anwender (K5).</para>
    ///
    /// <para><b>Warum gekappt und nicht gestreckt.</b> Der Vergleich läuft gegen die
    /// <b>kalibrierte</b> Reihe (<see cref="Objektlauf"/>); ein Streckfaktor auf die ganze Reihe
    /// verschwände dort wieder. Was die Spitze wirklich senkt, ist eine <b>flachere Gestalt</b> bei
    /// gleicher Energie — und genau das sagt die Lehre, an der das Band hängt: Die echte Messspitze
    /// liegt nicht am Maximum der Rechnung, sondern bei etwa P90 ihrer Dauerlinie (Konzept 3.6).
    /// Gekappt wird deshalb auf die Bandmitte, und die abgeschnittene Energie wird auf die übrigen
    /// Stunden verteilt, gewichtet mit ihrem eigenen Wert — die Tagesgestalt bleibt dabei erhalten,
    /// nur die Spitzen werden abgetragen (das Formmaß bleibt weit unter seiner Schwelle).</para>
    ///
    /// <para><b>Das Band kommt aus einem Probelauf des Kern-Vergleichs</b>, nicht aus einem eigenen
    /// Quantil: So prüft später dieselbe Rechnung, die hier die Grenzen gesetzt hat.</para>
    ///
    /// <para><b>Das Jahr</b> ist das erste Nichtschaltjahr ab 2013, dessen 1. Januar auf den
    /// Wochentag der Beschreibung fällt; die Zeitstempel sind Normalzeit (keine Umstellung), damit
    /// die 365 Tage der Rechnung und die Kalendertage der Reihe Tag für Tag zusammenfallen.</para>
    /// </summary>
    internal static class Beispielreihe
    {
        /// <summary>Die Saat des Rauschens — fest, damit die Datei wiederholbar entsteht.</summary>
        internal const long RAUSCHSAAT = 2026_0926;

        /// <summary>Die Spannweite des Rauschens [-]: Werte zwischen 1 − s und 1 + s.</summary>
        internal const double RAUSCHEN = 0.15;

        /// <summary>Das früheste Jahr, das die Reihe tragen darf.</summary>
        internal const int JAHR_AB = 2013;

        /// <summary>Höchstzahl der Kappungsrunden — jede Runde senkt die Spitze, keine erhöht sie.</summary>
        internal const int RUNDEN = 200;

        /// <summary>Die Kopfzeile: die Namen, die <see cref="Messreihenleser"/> erkennt.</summary>
        internal const string KOPF = "Zeitstempel;Wert (kWh)";

        /// <summary>
        /// Schreibt die synthetische Reihe des Objekts. Ergebnis ist der Grund bei Misserfolg,
        /// <c>null</c> = geschrieben.
        /// </summary>
        internal static string Schreiben(string objektordner, Objektbeschreibung o, Katalog katalog, bool trocken,
                                        out string vermerk)
        {
            vermerk = null;
            Nutzungsart art = katalog.Suchen(o.Nutzungsart);
            if (art == null) return "Der Katalog fuehrt keine Nutzungsart \"" + o.Nutzungsart + "\".";

            ZapfprofilErgebnis e;
            try
            {
                e = ZapfprofilRechner.Rechnen(Eingang(o, katalog), katalog.Arten);
            }
            catch (Exception ex) when (ex is ZapfprofilEingabeException || ex is ParametersatzException)
            {
                return "Die Rechnung lehnt ab: " + ex.Message;
            }
            if (!e.Vollstaendig)
                return "Die Rechnung lehnt ab: " + string.Join(" | ", e.Ablehnungen.Select(a => a.Klartext));

            // Dieselbe Grenze wie der spaetere Lauf: an der Zapfstelle allein die Zapfung.
            ZapfBilanzgrenze grenze = o.GrenzeWert ?? art.Grenze;
            Bilanzreihe gerechnet = Bilanzreihe.Summe(
                new[] { e.Zapfung, grenze == ZapfBilanzgrenze.Zapfstelle ? null : e.Zirkulation }
                    .Where(r => r != null));
            int jahr = Jahr(o.Kalender.WochentagJan1);
            var beginn = new DateTime(jahr, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            double[] werte = Verrauscht(gerechnet.StundenKwh, gerechnet.JahressummeKwh);
            double mitte = Bandmitte(werte, gerechnet, o, katalog, beginn, out string grund);
            if (grund != null) return grund;
            double kappe = mitte * gerechnet.GroessterStundenwertKw;
            int runden = Kappen(werte, kappe);

            vermerk = "Jahr " + jahr.ToString(CultureInfo.InvariantCulture) + ", Bandmitte "
                      + mitte.ToString("0.####", CultureInfo.InvariantCulture) + ", Rauschen ± "
                      + (RAUSCHEN * 100.0).ToString("0", CultureInfo.InvariantCulture) + " %, "
                      + runden.ToString(CultureInfo.InvariantCulture) + " Kappungsrunden";
            if (trocken) return null;

            var s = new StringBuilder(werte.Length * 32);
            s.Append(KOPF).Append("\r\n");
            for (int i = 0; i < werte.Length; i++)
                s.Append(beginn.AddHours(i).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture))
                 .Append(';').Append(werte[i].ToString("0.######", CultureInfo.InvariantCulture)).Append("\r\n");
            try
            {
                File.WriteAllText(Path.Combine(objektordner, o.Messdatei), s.ToString(),
                                  new UTF8Encoding(false));
                return null;
            }
            catch (IOException ex) { return "Die Reihe ist nicht schreibbar: " + ex.Message; }
        }

        /// <summary>Das erste Nichtschaltjahr ab <see cref="JAHR_AB"/> mit diesem Wochentag am 1. Januar.</summary>
        internal static int Jahr(int wochentagJan1)
        {
            for (int j = JAHR_AB; j < JAHR_AB + 100; j++)
            {
                if (DateTime.IsLeapYear(j)) continue;
                int wt = ((int)new DateTime(j, 1, 1).DayOfWeek + 6) % 7;   // Montag = 0
                if (wt == wochentagJan1) return j;
            }
            return JAHR_AB;
        }

        /// <summary>
        /// Die Reihe mal dem festen Rauschen, auf die <b>gleiche Jahresenergie</b> gebracht.
        /// Kongruenzgenerator (32 Bit) — er muss nicht gut sein, nur überall gleich;
        /// <see cref="ZapfZufall"/> bleibt dem Rechenweg vorbehalten.
        /// </summary>
        internal static double[] Verrauscht(IReadOnlyList<double> reihe, double summeSoll)
        {
            var werte = new double[reihe.Count];
            long z = RAUSCHSAAT;
            double summe = 0.0;
            for (int i = 0; i < reihe.Count; i++)
            {
                z = (1664525L * z + 1013904223L) & 0xFFFFFFFFL;
                double u = z / 4294967296.0;                      // [0, 1)
                werte[i] = reihe[i] * (1.0 - RAUSCHEN + 2.0 * RAUSCHEN * u);
                summe += werte[i];
            }
            if (summe > 0.0 && summeSoll > 0.0)
            {
                double f = summeSoll / summe;
                for (int i = 0; i < werte.Length; i++) werte[i] *= f;
            }
            return werte;
        }

        /// <summary>
        /// <b>Kappt die Reihe auf <paramref name="kappe"/></b> und verteilt die abgeschnittene
        /// Energie auf die Stunden darunter, gewichtet mit ihrem Wert — die Jahresenergie bleibt
        /// erhalten. Wiederholt, bis nichts mehr über der Kappe liegt; liefert die Zahl der Runden.
        /// </summary>
        internal static int Kappen(double[] werte, double kappe)
        {
            if (!(kappe > 0.0)) return 0;
            for (int runde = 1; runde <= RUNDEN; runde++)
            {
                double ueber = 0.0, unten = 0.0;
                for (int i = 0; i < werte.Length; i++)
                {
                    if (werte[i] > kappe) ueber += werte[i] - kappe;
                    else unten += werte[i];
                }
                if (ueber <= 0.0 || unten <= 0.0) return runde - 1;
                double f = 1.0 + ueber / unten;
                for (int i = 0; i < werte.Length; i++)
                    werte[i] = werte[i] > kappe ? kappe : werte[i] * f;
                if (ueber / unten < 1e-12) return runde;
            }
            return RUNDEN;
        }

        /// <summary>
        /// Die Mitte des Bands aus einem Probelauf des Kern-Vergleichs — dieselben Grenzen, die den
        /// späteren Lauf entscheiden.
        /// </summary>
        private static double Bandmitte(double[] werte, Bilanzreihe gerechnet, Objektbeschreibung o,
                                        Katalog katalog, DateTime beginn, out string grund)
        {
            grund = null;
            var probe = new Messreihe(o.Kennung, ZapfMessgroesse.Energie, 60, beginn, werte, "Probe");
            var eingang = new Messvergleichseingang
            {
                Reihe = probe,
                SpreizungK = 0.0,
                Gerechnet = gerechnet,
                Kalender = Zapfkalender.Bilden(o.Kalender.WochentagJan1, o.WeBilden(), null),
                Einheiten = (int)Math.Round(o.Bezugsmenge, MidpointRounding.AwayFromZero)
            };
            eingang = Messvergleich.AusParametern(eingang, katalog.Parameter);
            Messvergleichsergebnis v = Messvergleich.Vergleichen(eingang);
            if (!v.Ok) { grund = "Der Probevergleich lehnt ab: " + (v.Abbruch?.Klartext ?? "ohne Grund"); return 0.0; }
            if (v.Band?.BandUnten == null || v.Band.BandOben == null)
            {
                grund = "Der Probevergleich liefert kein Band - ohne Band keine Kappung.";
                return 0.0;
            }
            return 0.5 * (v.Band.BandUnten.Value + v.Band.BandOben.Value);
        }

        /// <summary>Der deterministische Eingang — die Reihe soll reproduzierbar sein, nicht gezogen.</summary>
        private static Zapfprofileingang Eingang(Objektbeschreibung o, Katalog katalog)
        {
            var zone = new ZonenStand
            {
                Id = 1, Name = "Beispiel", IdNutzungsart = katalog.Suchen(o.Nutzungsart).Id,
                Bezugsmenge = o.Bezugsmenge, Niveau = o.NiveauWert, Reihenfolge = 1,
                Zirkulation = o.Zirkulation, ZapftemperaturC = o.ZapftemperaturC,
                KaltwasserMittelC = o.KaltwasserMittelC, KaltwasserAmplitudeK = o.KaltwasserAmplitudeK
            };
            return new Zapfprofileingang
            {
                Zonen = new[] { zone },
                Projekt = new ProjektStand
                {
                    Id = 1, Weg = BrauchwasserWeg.Generator, ZirkAuto = true,
                    ZirkMethode = ZapfZirkulationsmethode.Anteil, Speicherart = ZapfSpeicherart.Ladespeicher,
                    LadeAuto = true, Perzentil = 99, Seed = 1, Realisierungen = 0,
                    JahresreiheStochastisch = false
                },
                WochentagJan1 = o.Kalender.WochentagJan1,
                We = o.WeBilden(),
                Parameter = katalog.Parameter,
                Tagesgangsaetze = katalog.Saetze,
                Zapfkategorien = katalog.Kategorien
            };
        }
    }
}
