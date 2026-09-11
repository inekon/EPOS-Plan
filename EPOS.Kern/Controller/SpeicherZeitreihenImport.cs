using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NReco.Csv;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Fachliche Rolle einer Zeitreihe fuer die Speicheroptimierung.</summary>
    public enum SpeicherZeitreihenRolle
    {
        Last = 0,
        Pv = 1,
        Bezug = 2
    }

    /// <summary>Einheit der Werte in der Quelldatei.</summary>
    public enum SpeicherZeitreihenEinheit
    {
        Kilowatt = 0,
        Megawatt = 1,
        KilowattstundeJeIntervall = 2,
        EuroJeKilowattstunde = 3,
        CentJeKilowattstunde = 4,
        EuroJeMegawattstunde = 5
    }

    /// <summary>Unterstuetzte Zeichenkodierungen der CSV-Datei.</summary>
    public enum SpeicherZeitreihenEncoding
    {
        Utf8 = 0,
        Windows1252 = 1
    }

    /// <summary>
    /// Explizite, JSON-serialisierbare Leseregeln. Der Import raet weder Kultur
    /// noch Spalten oder Zeitformat aus der Umgebung des Rechners.
    /// </summary>
    public sealed class SpeicherZeitreihenOptionen
    {
        public SpeicherZeitreihenRolle Rolle { get; set; } = SpeicherZeitreihenRolle.Last;
        public char Trennzeichen { get; set; } = ';';
        public char Dezimaltrenner { get; set; } = ',';
        public SpeicherZeitreihenEncoding Encoding { get; set; } = SpeicherZeitreihenEncoding.Utf8;
        public bool Kopfzeile { get; set; } = true;
        public int ZuUeberspringendeZeilen { get; set; }
        public int WertSpalte { get; set; } = 1;
        public int ZeitstempelSpalte { get; set; } = 0;
        public int DatumSpalte { get; set; } = -1;
        public int UhrzeitSpalte { get; set; } = -1;
        public string ZeitstempelFormat { get; set; } = "";
        public string DatumFormat { get; set; } = "";
        public string UhrzeitFormat { get; set; } = "";
        public string ZeitzoneId { get; set; } = "Europe/Berlin";
        public IntervallKonvention Konvention { get; set; } = IntervallKonvention.Anfang;
        public SpeicherZeitreihenEinheit Einheit { get; set; } = SpeicherZeitreihenEinheit.Kilowatt;
    }

    /// <summary>Begrenzte, bereits nach CSV-Regeln zerlegte Dateivorschau.</summary>
    public sealed class SpeicherZeitreihenVorschau
    {
        internal SpeicherZeitreihenVorschau(IReadOnlyList<string[]> zeilen, int spaltenzahl)
        {
            Zeilen = zeilen;
            Spaltenzahl = spaltenzahl;
        }

        public IReadOnlyList<string[]> Zeilen { get; }
        public int Spaltenzahl { get; }
    }

    /// <summary>
    /// Importierte Reihe. Last und PV stehen in kW, Bezugspreise in EUR/kWh;
    /// jeder Zeitstempel bezeichnet den Anfang eines 15-Minuten-Intervalls in UTC.
    /// </summary>
    public sealed class SpeicherZeitreihe
    {
        public string QuelleName { get; set; } = "";
        public SpeicherZeitreihenRolle Rolle { get; set; }
        public DateTimeOffset[] ZeitstempelUtc { get; set; } = Array.Empty<DateTimeOffset>();
        public double[] Werte { get; set; } = Array.Empty<double>();
        public string SHA256 { get; set; } = "";
        public SpeicherZeitreihenOptionen Optionen { get; set; } = new SpeicherZeitreihenOptionen();
    }

    /// <summary>
    /// Plattformfreier CSV-Import fuer Last, PV und Bezugspreis. Er erhaelt die
    /// wirkliche Zeitachse einschliesslich des 29. Februar und normalisiert nur
    /// das Zeitraster auf explizite UTC-Viertelstunden.
    /// </summary>
    public static class SpeicherZeitreihenImport
    {
        public const int MaximaleDateigroesse = 64 * 1024 * 1024;
        public const int MaximaleDatensaetze = 600000;
        public const int VorschauZeilen = 20;

        private static readonly string[] ZeitformateMitOffset =
        {
            "yyyy-MM-dd'T'HH:mmzzz",
            "yyyy-MM-dd'T'HH:mm:sszzz",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
            "yyyy-MM-dd HH:mmzzz",
            "yyyy-MM-dd HH:mm:sszzz",
            "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",
            "yyyy-MM-dd'T'HH:mm'Z'",
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
        };

        private static readonly string[] ZeitformateOhneOffset =
        {
            "yyyy-MM-dd'T'HH:mm",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "dd.MM.yyyy HH:mm",
            "dd.MM.yyyy HH:mm:ss",
            "dd.MM.yyyy HH:mm:ss.FFFFFFF"
        };

        private static readonly string[] Datumsformate =
        {
            "yyyy-MM-dd", "dd.MM.yyyy", "yyyyMMdd"
        };

        private static readonly string[] Uhrzeitformate =
        {
            "HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF", "HHmm"
        };

        /// <summary>Zerlegt hoechstens die ersten 20 logischen CSV-Zeilen.</summary>
        public static SpeicherZeitreihenVorschau Vorschau(
            byte[] inhalt, SpeicherZeitreihenOptionen optionen)
        {
            PruefeGrundangaben(inhalt, optionen, false);
            List<string[]> zeilen = LeseCsv(inhalt, optionen, VorschauZeilen);
            int spaltenzahl = 0;
            for (int i = 0; i < zeilen.Count; i++)
                spaltenzahl = Math.Max(spaltenzahl, zeilen[i].Length);
            return new SpeicherZeitreihenVorschau(zeilen, spaltenzahl);
        }

        /// <summary>
        /// Liest, prueft und normalisiert eine CSV-Datei auf 15-Minuten-Intervalle.
        /// Format- und Fachfehler werden als klar bezeichnete Ausnahmen gemeldet.
        /// </summary>
        public static SpeicherZeitreihe Lesen(
            byte[] inhalt, string dateiname, SpeicherZeitreihenOptionen optionen)
        {
            PruefeGrundangaben(inhalt, optionen, true);
            List<string[]> zeilen = LeseCsv(inhalt, optionen, MaximaleDatensaetze +
                optionen.ZuUeberspringendeZeilen + (optionen.Kopfzeile ? 1 : 0) + 1);

            int beginn = optionen.ZuUeberspringendeZeilen + (optionen.Kopfzeile ? 1 : 0);
            if (zeilen.Count <= beginn)
                throw new FormatException("Die CSV-Datei enthaelt keine Datensaetze.");
            if (zeilen.Count - beginn > MaximaleDatensaetze)
                throw new FormatException("Die CSV-Datei enthaelt mehr als " +
                    MaximaleDatensaetze.ToString(CultureInfo.InvariantCulture) + " Datensaetze.");

            TimeZoneInfo zeitzone = null;

            List<DateTimeOffset> zeit = new List<DateTimeOffset>(zeilen.Count - beginn);
            List<double> werte = new List<double>(zeilen.Count - beginn);
            for (int i = beginn; i < zeilen.Count; i++)
            {
                string[] felder = zeilen[i];
                int zeilennummer = i + 1;
                PruefeSpalten(felder, optionen, zeilennummer);
                double wert = ParseZahl(felder[optionen.WertSpalte], optionen.Dezimaltrenner,
                    zeilennummer);
                if (optionen.Rolle != SpeicherZeitreihenRolle.Bezug && wert < 0.0)
                    throw new FormatException("Zeile " + zeilennummer.ToString(CultureInfo.InvariantCulture) +
                        ": Last- und PV-Werte duerfen nicht negativ sein.");

                zeit.Add(ParseZeit(felder, optionen, ref zeitzone, zeilennummer));
                werte.Add(wert);
            }

            if (zeit.Count < 2)
                throw new FormatException("Die Zeitreihe muss mindestens zwei Datensaetze enthalten.");

            TimeSpan quellintervall = ErmittleUndPruefeRaster(zeit);
            if (optionen.Konvention == IntervallKonvention.Ende)
                for (int i = 0; i < zeit.Count; i++) zeit[i] = zeit[i] - quellintervall;

            PruefeEinheit(optionen.Rolle, optionen.Einheit);
            Normalisiere(zeit, werte, quellintervall, optionen.Rolle, optionen.Einheit,
                out DateTimeOffset[] zielZeit, out double[] zielWerte);

            return new SpeicherZeitreihe
            {
                QuelleName = dateiname ?? "",
                Rolle = optionen.Rolle,
                ZeitstempelUtc = zielZeit,
                Werte = zielWerte,
                SHA256 = Convert.ToHexString(SHA256.HashData(inhalt)),
                Optionen = Kopiere(optionen)
            };
        }

        internal static void PruefeGrundangaben(byte[] inhalt, SpeicherZeitreihenOptionen o,
            bool mitSpalten)
        {
            if (inhalt == null) throw new ArgumentNullException(nameof(inhalt));
            if (o == null) throw new ArgumentNullException(nameof(o));
            if (inhalt.Length == 0) throw new FormatException("Die CSV-Datei ist leer.");
            if (inhalt.Length > MaximaleDateigroesse)
                throw new ArgumentException("Die CSV-Datei ist groesser als 64 MiB.", nameof(inhalt));
            if (o.Trennzeichen != ',' && o.Trennzeichen != ';' && o.Trennzeichen != '\t')
                throw new ArgumentException("Als Trennzeichen sind Komma, Semikolon und Tabulator erlaubt.", nameof(o));
            if (o.Dezimaltrenner != ',' && o.Dezimaltrenner != '.')
                throw new ArgumentException("Der Dezimaltrenner muss Komma oder Punkt sein.", nameof(o));
            if (o.ZuUeberspringendeZeilen < 0)
                throw new ArgumentException("Die Anzahl zu ueberspringender Zeilen darf nicht negativ sein.", nameof(o));
            if (o.ZuUeberspringendeZeilen > MaximaleDatensaetze)
                throw new ArgumentException("Es koennen hoechstens " +
                    MaximaleDatensaetze.ToString(CultureInfo.InvariantCulture) +
                    " Zeilen uebersprungen werden.", nameof(o));
            if (!mitSpalten) return;
            if (!Enum.IsDefined(typeof(SpeicherZeitreihenRolle), o.Rolle))
                throw new ArgumentException("Unbekannte Rolle der Zeitreihe.", nameof(o));
            if (!Enum.IsDefined(typeof(SpeicherZeitreihenEinheit), o.Einheit))
                throw new ArgumentException("Unbekannte Einheit der Zeitreihe.", nameof(o));
            if (o.WertSpalte < 0)
                throw new ArgumentException("Die Wertspalte muss angegeben sein.", nameof(o));
            bool kombiniert = o.ZeitstempelSpalte >= 0;
            bool getrennt = o.DatumSpalte >= 0 && o.UhrzeitSpalte >= 0;
            if (!kombiniert && !getrennt)
                throw new ArgumentException("Es muss eine Zeitstempelspalte oder je eine Datums- und Uhrzeitspalte angegeben sein.", nameof(o));
            if (o.Konvention != IntervallKonvention.Anfang &&
                o.Konvention != IntervallKonvention.Ende)
                throw new ArgumentException("Die Zeitstempelkonvention muss Anfang oder Ende sein.", nameof(o));
        }

        internal static List<string[]> LeseCsv(byte[] inhalt, SpeicherZeitreihenOptionen o, int maximal)
        {
            Encoding kodierung = HoleEncoding(o.Encoding);
            List<string[]> ergebnis = new List<string[]>();
            try
            {
                using MemoryStream ms = new MemoryStream(inhalt, false);
                using StreamReader sr = new StreamReader(ms, kodierung, true, 65536, false);
                CsvReader csv = new CsvReader(sr, o.Trennzeichen.ToString())
                {
                    BufferSize = 1024 * 1024,
                    TrimFields = true
                };
                while (ergebnis.Count < maximal && csv.Read())
                {
                    string[] felder = new string[csv.FieldsCount];
                    for (int i = 0; i < felder.Length; i++) felder[i] = csv[i] ?? "";
                    ergebnis.Add(felder);
                }
            }
            catch (DecoderFallbackException ex)
            {
                throw new FormatException("Die Datei enthaelt ungueltige Zeichen fuer die gewaehlte Kodierung.", ex);
            }
            catch (InvalidDataException ex)
            {
                throw new FormatException("Die CSV-Struktur ist ungueltig: " + ex.Message, ex);
            }
            return ergebnis;
        }

        private static Encoding HoleEncoding(SpeicherZeitreihenEncoding encoding)
        {
            if (encoding == SpeicherZeitreihenEncoding.Utf8)
                return new UTF8Encoding(false, true);
            if (encoding == SpeicherZeitreihenEncoding.Windows1252)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback,
                    DecoderFallback.ExceptionFallback);
            }
            throw new ArgumentException("Unbekannte Zeichenkodierung.", nameof(encoding));
        }

        private static void PruefeSpalten(string[] felder, SpeicherZeitreihenOptionen o,
            int zeilennummer)
        {
            int hoechste = Math.Max(o.WertSpalte, o.ZeitstempelSpalte);
            if (o.ZeitstempelSpalte < 0)
                hoechste = Math.Max(hoechste, Math.Max(o.DatumSpalte, o.UhrzeitSpalte));
            if (felder.Length <= hoechste)
                throw new FormatException("Zeile " + zeilennummer.ToString(CultureInfo.InvariantCulture) +
                    " hat nur " + felder.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten; benoetigt wird Spalte " +
                    (hoechste + 1).ToString(CultureInfo.InvariantCulture) + ".");
        }

        internal static double ParseZahl(string text, char dezimaltrenner, int zeilennummer)
        {
            string normalisiert = (text ?? "").Trim();
            if (dezimaltrenner == ',') normalisiert = normalisiert.Replace(',', '.');
            const NumberStyles stil = NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            if (!double.TryParse(normalisiert, stil, CultureInfo.InvariantCulture, out double wert) ||
                double.IsNaN(wert) || double.IsInfinity(wert))
                throw new FormatException("Zeile " + zeilennummer.ToString(CultureInfo.InvariantCulture) +
                    ": '" + text + "' ist keine endliche Zahl mit dem gewaehlten Dezimaltrenner.");
            return wert;
        }

        internal static DateTimeOffset ParseZeit(string[] felder, SpeicherZeitreihenOptionen o,
            ref TimeZoneInfo zeitzone, int zeilennummer)
        {
            if (o.ZeitstempelSpalte >= 0)
            {
                string text = felder[o.ZeitstempelSpalte].Trim();
                if (HatExplizitenOffset(text, o.ZeitstempelFormat))
                {
                    string[] formate = string.IsNullOrWhiteSpace(o.ZeitstempelFormat)
                        ? ZeitformateMitOffset : new[] { o.ZeitstempelFormat };
                    DateTimeStyles stil = text.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
                        ? DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                        : DateTimeStyles.None;
                    if (DateTimeOffset.TryParseExact(text, formate, CultureInfo.InvariantCulture,
                        stil, out DateTimeOffset dto)) return dto.ToUniversalTime();
                }
                else
                {
                    string[] formate = string.IsNullOrWhiteSpace(o.ZeitstempelFormat)
                        ? ZeitformateOhneOffset : new[] { o.ZeitstempelFormat };
                    if (DateTime.TryParseExact(text, formate, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime lokal))
                    {
                        zeitzone ??= FindeZeitzone(o.ZeitzoneId);
                        return LokalZuUtc(DateTime.SpecifyKind(lokal, DateTimeKind.Unspecified),
                            zeitzone, zeilennummer);
                    }
                }
                throw Zeitfehler(zeilennummer, text);
            }

            string datumText = felder[o.DatumSpalte].Trim();
            string uhrzeitText = felder[o.UhrzeitSpalte].Trim();
            string[] datumsformate = string.IsNullOrWhiteSpace(o.DatumFormat)
                ? Datumsformate : new[] { o.DatumFormat };
            string[] uhrzeitformate = string.IsNullOrWhiteSpace(o.UhrzeitFormat)
                ? Uhrzeitformate : new[] { o.UhrzeitFormat };
            if (!DateTime.TryParseExact(datumText, datumsformate, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime datum) ||
                !DateTime.TryParseExact(uhrzeitText, uhrzeitformate, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime uhrzeit))
                throw Zeitfehler(zeilennummer, datumText + " " + uhrzeitText);
            DateTime kombiniert = DateTime.SpecifyKind(datum.Date + uhrzeit.TimeOfDay,
                DateTimeKind.Unspecified);
            zeitzone ??= FindeZeitzone(o.ZeitzoneId);
            return LokalZuUtc(kombiniert, zeitzone, zeilennummer);
        }

        private static bool HatExplizitenOffset(string text, string format)
        {
            if (!string.IsNullOrWhiteSpace(format))
                return format.IndexOf('z') >= 0 || format.IndexOf('K') >= 0 ||
                    format.IndexOf("'Z'", StringComparison.Ordinal) >= 0;
            if (text.EndsWith("Z", StringComparison.OrdinalIgnoreCase)) return true;
            int t = Math.Max(text.LastIndexOf('T'), text.LastIndexOf(' '));
            int plus = text.LastIndexOf('+');
            int minus = text.LastIndexOf('-');
            return plus > t || minus > t;
        }

        private static FormatException Zeitfehler(int zeile, string text)
            => new FormatException("Zeile " + zeile.ToString(CultureInfo.InvariantCulture) +
                ": Zeitstempel '" + text + "' passt zu keinem zugelassenen Format.");

        private static DateTimeOffset LokalZuUtc(DateTime lokal, TimeZoneInfo zone, int zeile)
        {
            if (zone.IsInvalidTime(lokal))
                throw new FormatException("Zeile " + zeile.ToString(CultureInfo.InvariantCulture) +
                    ": Die Ortszeit " + lokal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                    " existiert wegen der Sommerzeitumstellung nicht.");
            if (zone.IsAmbiguousTime(lokal))
                throw new FormatException("Zeile " + zeile.ToString(CultureInfo.InvariantCulture) +
                    ": Die Ortszeit " + lokal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                    " ist wegen der Sommerzeitumstellung mehrdeutig; bitte ISO-Zeitstempel mit Offset verwenden.");
            TimeSpan offset = zone.GetUtcOffset(lokal);
            return new DateTimeOffset(lokal, offset).ToUniversalTime();
        }

        internal static TimeZoneInfo FindeZeitzone(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Fuer Zeitstempel ohne Offset muss eine Zeitzone angegeben sein.", nameof(id));
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) when (id == "Europe/Berlin")
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); }
                catch (TimeZoneNotFoundException) { }
            }
            catch (InvalidTimeZoneException ex)
            {
                throw new ArgumentException("Die Zeitzone '" + id + "' ist ungueltig.", nameof(id), ex);
            }
            throw new ArgumentException("Die Zeitzone '" + id + "' wurde nicht gefunden.", nameof(id));
        }

        internal static TimeSpan ErmittleUndPruefeRaster(IReadOnlyList<DateTimeOffset> zeit)
        {
            TimeSpan raster = zeit[1] - zeit[0];
            if (raster <= TimeSpan.Zero)
                throw new FormatException("Doppelter oder rueckwaerts laufender Zeitstempel bei Datensatz 2.");
            if (raster != TimeSpan.FromMinutes(15) && raster != TimeSpan.FromHours(1))
                throw new FormatException("Unterstuetzt werden Zeitraster von 15 oder 60 Minuten; erkannt wurden " +
                    raster.TotalMinutes.ToString("0.########", CultureInfo.InvariantCulture) + " Minuten.");
            for (int i = 1; i < zeit.Count; i++)
            {
                TimeSpan abstand = zeit[i] - zeit[i - 1];
                if (abstand <= TimeSpan.Zero)
                    throw new FormatException("Doppelter oder rueckwaerts laufender Zeitstempel bei Datensatz " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + ".");
                if (abstand != raster)
                    throw new FormatException("Fehlendes oder doppeltes Intervall vor Datensatz " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + ": erwartet " +
                        raster.TotalMinutes.ToString("0", CultureInfo.InvariantCulture) +
                        " Minuten, gefunden " + abstand.TotalMinutes.ToString("0.########", CultureInfo.InvariantCulture) + ".");
            }
            return raster;
        }

        internal static void PruefeEinheit(SpeicherZeitreihenRolle rolle,
            SpeicherZeitreihenEinheit einheit)
        {
            bool preis = einheit == SpeicherZeitreihenEinheit.EuroJeKilowattstunde ||
                einheit == SpeicherZeitreihenEinheit.CentJeKilowattstunde ||
                einheit == SpeicherZeitreihenEinheit.EuroJeMegawattstunde;
            if (rolle == SpeicherZeitreihenRolle.Bezug && !preis)
                throw new ArgumentException("Fuer Bezugspreise muss eine Preiseinheit gewaehlt werden.", nameof(einheit));
            if (rolle != SpeicherZeitreihenRolle.Bezug && preis)
                throw new ArgumentException("Fuer Last und PV muss kW, MW oder kWh je Intervall gewaehlt werden.", nameof(einheit));
        }

        internal static void Normalisiere(IReadOnlyList<DateTimeOffset> zeit,
            IReadOnlyList<double> werte, TimeSpan raster, SpeicherZeitreihenRolle rolle,
            SpeicherZeitreihenEinheit einheit, out DateTimeOffset[] zielZeit,
            out double[] zielWerte)
        {
            int faktor = raster == TimeSpan.FromHours(1) ? 4 : 1;
            zielZeit = new DateTimeOffset[zeit.Count * faktor];
            zielWerte = new double[werte.Count * faktor];
            double umrechnung;
            if (rolle == SpeicherZeitreihenRolle.Bezug)
            {
                umrechnung = einheit == SpeicherZeitreihenEinheit.CentJeKilowattstunde ? 0.01 :
                    einheit == SpeicherZeitreihenEinheit.EuroJeMegawattstunde ? 0.001 : 1.0;
            }
            else
            {
                umrechnung = einheit == SpeicherZeitreihenEinheit.Megawatt ? 1000.0 :
                    einheit == SpeicherZeitreihenEinheit.KilowattstundeJeIntervall
                        ? 1.0 / raster.TotalHours : 1.0;
            }

            int z = 0;
            for (int i = 0; i < zeit.Count; i++)
            {
                double wert = werte[i] * umrechnung;
                if (double.IsNaN(wert) || double.IsInfinity(wert))
                    throw new FormatException("Die Einheitenumrechnung erzeugt bei Datensatz " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " einen nicht endlichen Wert.");
                for (int q = 0; q < faktor; q++)
                {
                    zielZeit[z] = zeit[i].ToUniversalTime().AddMinutes(q * 15);
                    zielWerte[z] = wert;
                    z++;
                }
            }
        }

        private static SpeicherZeitreihenOptionen Kopiere(SpeicherZeitreihenOptionen o)
            => new SpeicherZeitreihenOptionen
            {
                Rolle = o.Rolle,
                Trennzeichen = o.Trennzeichen,
                Dezimaltrenner = o.Dezimaltrenner,
                Encoding = o.Encoding,
                Kopfzeile = o.Kopfzeile,
                ZuUeberspringendeZeilen = o.ZuUeberspringendeZeilen,
                WertSpalte = o.WertSpalte,
                ZeitstempelSpalte = o.ZeitstempelSpalte,
                DatumSpalte = o.DatumSpalte,
                UhrzeitSpalte = o.UhrzeitSpalte,
                ZeitstempelFormat = o.ZeitstempelFormat ?? "",
                DatumFormat = o.DatumFormat ?? "",
                UhrzeitFormat = o.UhrzeitFormat ?? "",
                ZeitzoneId = o.ZeitzoneId ?? "",
                Konvention = o.Konvention,
                Einheit = o.Einheit
            };
    }
}
