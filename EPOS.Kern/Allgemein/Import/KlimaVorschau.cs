using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Kennzahlen eines gelesenen Klimadatensatzes (Auftrag KD-2, Konzept
    /// Klimadatenimport § 4) — gerechnet im Kern, nie auf der Razor-Seite.
    ///
    /// <para><b>Die Tagesmittel</b> entstehen wie die Tageswerte von
    /// <c>Tab_Klimadaten</c> (<see cref="SolarCalculator.GetDailyAverages"/>): Gruppe je
    /// Kalendertag des Zeitstempels <c>yyyyMMdd:HHmm</c>, Mittel über die Stunden der
    /// Gruppe. Die Kennzahlen lesen nur Lufttemperatur und Globalstrahlung — beide
    /// ändert der Import nach dem Lesen nicht mehr.</para>
    /// </summary>
    public sealed class KlimaKennzahlen
    {
        /// <summary>Zahl der gelesenen Stunden (8 760 bei einem vollständigen Jahr).</summary>
        public int Stunden;

        /// <summary>Zahl der Kalendertage mit mindestens einer Stunde.</summary>
        public int Tage;

        /// <summary>Jahresmittel der Lufttemperatur [°C] (Mittel über die Stunden).</summary>
        public double TemperaturMittel;

        /// <summary>Kleinster Stundenwert der Lufttemperatur [°C].</summary>
        public double TemperaturMin;

        /// <summary>Größter Stundenwert der Lufttemperatur [°C].</summary>
        public double TemperaturMax;

        /// <summary>
        /// Jahressumme der Globalstrahlung horizontal [kWh/m²a]: Summe der Stundenwerte
        /// [W/m²] × 1 h / 1 000.
        /// </summary>
        public double GlobalstrahlungKwhM2;

        /// <summary>
        /// Gradtagzahl G20/15 [Kd/a] nach VDI 3807 Blatt 1: Summe von
        /// (20 °C − θm) über die Tage mit Tagesmittel θm unter der Heizgrenze 15 °C.
        /// </summary>
        public double GradtagzahlKdA;

        /// <summary>Zahl der Heiztage (Tagesmittel unter 15 °C) nach derselben Regel.</summary>
        public int Heiztage;

        /// <summary>Monatsmittel der Lufttemperatur [°C], Januar … Dezember; <c>NaN</c> ohne Stunden.</summary>
        public double[] MonatsmittelTemperatur = new double[12];

        /// <summary>Monatssumme der Globalstrahlung [kWh/m²], Januar … Dezember.</summary>
        public double[] MonatssummeGlobalstrahlungKwhM2 = new double[12];

        /// <summary>
        /// Die MONATSREIHEN je Größe des <see cref="KlimaReihenkatalog"/> (Auftrag KD-8 Teil A):
        /// Schlüssel → zwölf Monatswerte, Summen in kWh/m², Mittel in der Einheit der Stunden;
        /// ein Monat ohne Stunde dieser Größe ist <c>NaN</c>. Eine Größe ohne eine einzige Stunde
        /// steht nicht darin. Temperatur und Globalstrahlung sind dieselben Zahlen wie
        /// <see cref="MonatsmittelTemperatur"/> und <see cref="MonatssummeGlobalstrahlungKwhM2"/>.
        /// </summary>
        public Dictionary<string, double[]> Monatsreihen = new Dictionary<string, double[]>();

        /// <summary>
        /// Die Monatsreihe einer Größe; <c>null</c> = der Satz trägt sie nicht. Temperatur und
        /// Globalstrahlung kommen auch ohne <see cref="Monatsreihen"/> aus den Feldern des Bildes.
        /// </summary>
        public double[] Monatsreihe(string schluessel)
        {
            if (schluessel != null && Monatsreihen != null && Monatsreihen.TryGetValue(schluessel, out double[] w)) return w;
            if (schluessel == KlimaReihenkatalog.R_TEMPERATUR && MonatsmittelTemperatur?.Length == 12) return MonatsmittelTemperatur;
            if (schluessel == KlimaReihenkatalog.R_GLOBAL && MonatssummeGlobalstrahlungKwhM2?.Length == 12)
                return MonatssummeGlobalstrahlungKwhM2;
            return null;
        }

        /// <summary>Die Zahl der Stunden mit Wert je Größe (Titel des Chips bei Teilfüllung).</summary>
        public Dictionary<string, int> Reihenstunden = new Dictionary<string, int>();

        /// <summary>Innentemperatur der Gradtagzahl G20/15 [°C].</summary>
        public const double GRADTAG_INNEN = 20.0;

        /// <summary>Heizgrenze der Gradtagzahl G20/15 [°C].</summary>
        public const double GRADTAG_HEIZGRENZE = 15.0;

        /// <summary>
        /// Rechnet die Kennzahlen aus einer Stundenreihe; <c>null</c>, wenn die Reihe
        /// leer ist oder keinen lesbaren Zeitstempel trägt.
        /// </summary>
        /// <param name="stunden">Die Stunden wie gelesen.</param>
        /// <param name="direktNormal">Wahlweise dieselben Stunden mit Direkt-Normalstrahlung
        /// (TRY: nach <c>DwdTryLeser.DirektNormal</c>); <c>null</c> = <paramref name="stunden"/>
        /// führen sie schon (PVGIS).</param>
        public static KlimaKennzahlen Rechnen(IReadOnlyList<TmyHourlyData> stunden,
                                              IReadOnlyList<TmyHourlyData> direktNormal = null)
        {
            if (stunden == null || stunden.Count == 0) return null;

            var k = new KlimaKennzahlen
            {
                TemperaturMin = double.MaxValue,
                TemperaturMax = double.MinValue
            };

            double summeT = 0;
            var monatT = new double[12];
            var monatN = new int[12];

            // Tagesgruppen in der Reihenfolge ihres ersten Auftretens.
            var tagIndex = new Dictionary<int, int>();
            var tagSumme = new List<double>();
            var tagZahl = new List<int>();

            foreach (TmyHourlyData s in stunden)
            {
                if (s == null) continue;
                if (!DateTime.TryParseExact(s.TimeString ?? "", "yyyyMMdd:HHmm",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    continue;

                double t = s.Temperature;
                k.Stunden++;
                summeT += t;
                if (t < k.TemperaturMin) k.TemperaturMin = t;
                if (t > k.TemperaturMax) k.TemperaturMax = t;

                double g = s.GlobalIrradiance / 1000.0;
                k.GlobalstrahlungKwhM2 += g;

                int m = dt.Month - 1;
                monatT[m] += t;
                monatN[m]++;
                k.MonatssummeGlobalstrahlungKwhM2[m] += g;

                int schluessel = dt.Month * 100 + dt.Day;
                if (!tagIndex.TryGetValue(schluessel, out int i))
                {
                    i = tagSumme.Count;
                    tagIndex[schluessel] = i;
                    tagSumme.Add(0);
                    tagZahl.Add(0);
                }
                tagSumme[i] += t;
                tagZahl[i]++;
            }

            if (k.Stunden == 0) return null;

            k.TemperaturMittel = summeT / k.Stunden;
            for (int m = 0; m < 12; m++)
                k.MonatsmittelTemperatur[m] = monatN[m] > 0 ? monatT[m] / monatN[m] : double.NaN;

            MonatsreihenRechnen(k, stunden, direktNormal);

            k.Tage = tagSumme.Count;
            for (int i = 0; i < tagSumme.Count; i++)
            {
                double mittel = tagSumme[i] / tagZahl[i];
                if (mittel >= GRADTAG_HEIZGRENZE) continue;
                k.Heiztage++;
                k.GradtagzahlKdA += GRADTAG_INNEN - mittel;
            }

            return k;
        }

        /// <summary>
        /// Die Monatsreihen je Größe der Vorschau (Konzept § 4a.1): Monatsmittel bzw. Monatssumme
        /// ∑ W/m² · 1 h / 1 000 über die vorhandenen Stunden; Monatsgrenzen aus dem Zeitstempel.
        /// </summary>
        private static void MonatsreihenRechnen(KlimaKennzahlen k, IReadOnlyList<TmyHourlyData> stunden,
                                                IReadOnlyList<TmyHourlyData> direktNormal)
        {
            foreach (KlimaReihe r in KlimaReihenkatalog.Reihen)
            {
                if (!r.InVorschau || r.Herkunft == KlimaReihenherkunft.Rohwert) continue;
                IReadOnlyList<TmyHourlyData> quelle =
                    r.Schluessel == KlimaReihenkatalog.R_DIREKT_NORMAL && direktNormal != null ? direktNormal : stunden;

                var summe = new double[12];
                var zahl = new int[12];
                int gesamt = 0;
                foreach (TmyHourlyData s in quelle)
                {
                    if (s == null) continue;
                    double? w = KlimaReihenkatalog.Stundenwert(s, r.Schluessel);
                    if (!w.HasValue || double.IsNaN(w.Value)) continue;
                    if (!DateTime.TryParseExact(s.TimeString ?? "", "yyyyMMdd:HHmm",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                        continue;
                    int m = dt.Month - 1;
                    summe[m] += w.Value;
                    zahl[m]++;
                    gesamt++;
                }
                if (gesamt == 0) continue;

                var monate = new double[12];
                for (int m = 0; m < 12; m++)
                    monate[m] = zahl[m] == 0 ? double.NaN
                        : r.Monatsregel == KlimaMonatsregel.Summe ? summe[m] / 1000.0 : summe[m] / zahl[m];
                k.Monatsreihen[r.Schluessel] = monate;
                k.Reihenstunden[r.Schluessel] = gesamt;
            }

            // Die beiden Reihen des heutigen Bildes bleiben dieselben Zahlen.
            if (k.Monatsreihen.ContainsKey(KlimaReihenkatalog.R_TEMPERATUR))
                k.Monatsreihen[KlimaReihenkatalog.R_TEMPERATUR] = (double[])k.MonatsmittelTemperatur.Clone();
            if (k.Monatsreihen.ContainsKey(KlimaReihenkatalog.R_GLOBAL))
                k.Monatsreihen[KlimaReihenkatalog.R_GLOBAL] = (double[])k.MonatssummeGlobalstrahlungKwhM2.Clone();
        }
    }

    /// <summary>Die Größen eines Klimadatensatzes (Konzept Klimadatenimport § 2).</summary>
    public enum KlimaParameter
    {
        Lufttemperatur,
        Luftfeuchte,
        Globalstrahlung,
        DirektstrahlungNormal,
        DirektstrahlungHorizontal,
        Diffusstrahlung,
        Gegenstrahlung,
        Bedeckungsgrad,
        Luftdruck,
        Windgeschwindigkeit,
        Windrichtung,
        Wasserdampfgehalt,
        Ausstrahlung,
        QualitaetStrahlung
    }

    /// <summary>Wie eine Quelle eine Größe führt.</summary>
    public enum KlimaParameterStand
    {
        /// <summary>Die Quelle liefert die Größe.</summary>
        Gefuehrt,
        /// <summary>Die Quelle liefert sie nicht unmittelbar; EPOS-Plan rechnet sie aus geführten Größen.</summary>
        Gerechnet,
        /// <summary>Die Quelle führt die Größe nicht (oder der gelesene Satz trägt keinen Wert).</summary>
        Fehlt
    }

    /// <summary>Eine Zeile des Parameterbestands.</summary>
    public sealed class KlimaParameterEintrag
    {
        public KlimaParameter Parameter;
        public KlimaParameterStand Stand;
    }

    /// <summary>
    /// Der PARAMETERBESTAND einer Quelle (Auftrag KD-2): welche Größe die Quelle führt —
    /// nach der Tabelle in § 2 des Konzepts Klimadatenimport. Ob EPOS-Plan die Größe
    /// heute speichert, ist eine andere Frage (Rohdaten erst mit KD-4).
    /// </summary>
    public static class KlimaParameterbestand
    {
        /// <summary>
        /// Der Bestand einer Quelle, geschärft am gelesenen Satz: Führt die Quelle eine
        /// nullbare Größe (Feuchte, Gegenstrahlung, Bedeckungsgrad) dem Format nach, trägt
        /// der Satz aber keinen einzigen Wert, steht sie auf <see cref="KlimaParameterStand.Fehlt"/>.
        /// </summary>
        public static IReadOnlyList<KlimaParameterEintrag> Fuer(KlimaQuelle quelle,
                                                                 IReadOnlyList<TmyHourlyData> stunden = null)
        {
            var liste = new List<KlimaParameterEintrag>();
            foreach (KlimaParameter p in Enum.GetValues(typeof(KlimaParameter)))
            {
                KlimaParameterStand stand = Nennstand(quelle, p);
                if (stand == KlimaParameterStand.Gefuehrt && stunden != null && stunden.Count > 0 &&
                    !WertVorhanden(p, stunden))
                    stand = KlimaParameterStand.Fehlt;

                liste.Add(new KlimaParameterEintrag { Parameter = p, Stand = stand });
            }
            return liste;
        }

        /// <summary>Der Stand dem FORMAT der Quelle nach (Konzept § 2).</summary>
        public static KlimaParameterStand Nennstand(KlimaQuelle quelle, KlimaParameter p)
        {
            if (quelle == KlimaQuelle.PvgisTmy)
            {
                switch (p)
                {
                    case KlimaParameter.DirektstrahlungHorizontal:
                    case KlimaParameter.Bedeckungsgrad:
                    case KlimaParameter.Wasserdampfgehalt:
                    case KlimaParameter.Ausstrahlung:
                    case KlimaParameter.QualitaetStrahlung:
                        return KlimaParameterStand.Fehlt;
                    default:
                        return KlimaParameterStand.Gefuehrt;
                }
            }

            // DWD-TRY (Datei wie Regionalpaket): 17 Felder; die Globalstrahlung ist B + D,
            // die Direktstrahlung normal wird aus B gerechnet.
            switch (p)
            {
                case KlimaParameter.Globalstrahlung:
                case KlimaParameter.DirektstrahlungNormal:
                    return KlimaParameterStand.Gerechnet;
                default:
                    return KlimaParameterStand.Gefuehrt;
            }
        }

        /// <summary>Trägt der gelesene Satz für eine nullbare Größe mindestens einen Wert?</summary>
        private static bool WertVorhanden(KlimaParameter p, IReadOnlyList<TmyHourlyData> stunden)
        {
            Func<TmyHourlyData, double?> wert;
            switch (p)
            {
                case KlimaParameter.Luftfeuchte: wert = s => s.Humidity; break;
                case KlimaParameter.Gegenstrahlung: wert = s => s.Gegenstrahlung; break;
                case KlimaParameter.Bedeckungsgrad: wert = s => s.Bedeckungsgrad; break;
                default: return true;
            }

            foreach (TmyHourlyData s in stunden)
                if (s != null && wert(s).HasValue) return true;
            return false;
        }
    }

    /// <summary>Welcher Art der Bezugspunkt eines Datensatzes ist.</summary>
    public enum KlimaBezugspunktArt
    {
        /// <summary>Die nächste Repräsentanzstation des TRY-Regionalpakets.</summary>
        Repraesentanzstation,
        /// <summary>Der Bezugsort im Kopf einer DWD-TRY-Datei (Station bzw. Rasterpunkt).</summary>
        Dateistandort,
        /// <summary>Der Punkt, für den PVGIS gerechnet hat.</summary>
        PvgisPunkt
    }

    /// <summary>Der Bezugspunkt des gelesenen Datensatzes (Auftrag KD-2).</summary>
    public sealed class KlimaBezugspunkt
    {
        public KlimaBezugspunktArt Art;

        /// <summary>Name der Station bzw. Region („14 Stötten"); leer, wenn die Quelle keinen nennt.</summary>
        public string Name = "";

        /// <summary>Breite [Grad]; <c>null</c>, wenn die Quelle keine nennt.</summary>
        public double? Latitude;

        /// <summary>Länge [Grad]; <c>null</c>, wenn die Quelle keine nennt.</summary>
        public double? Longitude;

        /// <summary>Höhe über NN [m]; <c>null</c>, wenn die Quelle keine nennt.</summary>
        public double? HoeheM;

        /// <summary>Großkreisentfernung zum Standort [km]; <c>null</c> ohne Koordinate.</summary>
        public double? EntfernungKm;
    }

    /// <summary>Der Standort, für den gesucht wurde (Auftrag KD-2).</summary>
    public sealed class KlimaStandort
    {
        public double Latitude;
        public double Longitude;

        /// <summary>Anzeigename der Geokodierung; leer bei Koordinaten von Hand.</summary>
        public string Anzeigename = "";

        /// <summary>Höhe über NN [m]; <c>null</c>, wenn sie nicht bekannt ist.</summary>
        public double? HoeheM;

        /// <summary>Kommen Breite und Länge aus der Geokodierung eines Ortsnamens?</summary>
        public bool AusGeokodierung;
    }

    /// <summary>
    /// Die FESTWERTE des Hinweises „Bezugspunkt nicht ortstypisch" (Entscheide KD-Q1 und
    /// KD-Q9: im Kern, nicht in den Einstellungen).
    /// </summary>
    public static class KlimaVorschauFestwerte
    {
        /// <summary>Ab dieser Entfernung [km] zwischen Standort und Bezugspunkt erscheint der Hinweis.</summary>
        public const double HINWEIS_ENTFERNUNG_KM = 50.0;

        /// <summary>Ab diesem Höhenunterschied [m] (Betrag) erscheint der Hinweis.</summary>
        public const double HINWEIS_HOEHE_M = 200.0;

        /// <summary>
        /// Ist der Bezugspunkt nicht ortstypisch? Ja ab <see cref="HINWEIS_ENTFERNUNG_KM"/>
        /// Entfernung ODER ab <see cref="HINWEIS_HOEHE_M"/> Höhenunterschied (je
        /// einschließlich); eine unbekannte Größe löst nichts aus.
        /// </summary>
        public static bool NichtOrtstypisch(double? entfernungKm, double? hoehenunterschiedM)
        {
            if (entfernungKm.HasValue && entfernungKm.Value >= HINWEIS_ENTFERNUNG_KM) return true;
            if (hoehenunterschiedM.HasValue && Math.Abs(hoehenunterschiedM.Value) >= HINWEIS_HOEHE_M) return true;
            return false;
        }

        private static readonly Regex ZAHL = new Regex(@"-?\d+(?:[.,]\d+)?",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Die Höhe aus dem Kopftext „Höhenlage" einer TRY-Datei („734 Meter über NN",
        /// „250 m"): die erste Zahl, Komma oder Punkt als Dezimalzeichen; <c>null</c>
        /// ohne Zahl.
        /// </summary>
        public static double? HoeheAusText(string text)
        {
            Match m = ZAHL.Match(text ?? "");
            if (!m.Success) return null;
            return double.TryParse(m.Value.Replace(',', '.'), NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out double h) ? h : (double?)null;
        }
    }
}
