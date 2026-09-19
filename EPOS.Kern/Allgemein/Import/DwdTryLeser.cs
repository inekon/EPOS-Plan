using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was im KOPF einer DWD-TRY-Datei steht — und was der Import davon NICHT übernimmt.
    ///
    /// <para>Der Kopf ist freier Text: 34 Zeilen bei den Testreferenzjahren 2015,
    /// 36 bei den Projektionen 2045, bis zu der Zeile, die getrimmt mit
    /// <c>***</c> beginnt. Gelesen werden daraus nur die Angaben, die als
    /// Herkunftsvermerk in <c>Tab_Klimaregion_STAMM.Details</c> taugen; alles
    /// Weitere bleibt Text.</para>
    /// </summary>
    public sealed class TryKopf
    {
        /// <summary>Zahl der Kopfzeilen VOR der Trennzeile (34 bzw. 36).</summary>
        public int Kopfzeilen;

        /// <summary>Art des Datensatzes, soweit im Kopf genannt (z. B. „Jahr").</summary>
        public string Art = "";

        /// <summary>Bezugszeitraum bzw. Datenbasis, soweit im Kopf genannt.</summary>
        public string Bezugszeitraum = "";

        /// <summary>Lambert-Rechtswert der Station; <c>""</c>, wenn nicht lesbar.</summary>
        public string Rechtswert = "";

        /// <summary>Lambert-Hochwert der Station; <c>""</c>, wenn nicht lesbar.</summary>
        public string Hochwert = "";

        /// <summary>Höhe über NN, soweit im Kopf genannt.</summary>
        public string Hoehe = "";

        /// <summary>
        /// Geographische Länge des Standorts [Grad, Ost positiv], aus
        /// <see cref="Rechtswert"/>/<see cref="Hochwert"/> über
        /// <see cref="LambertKoordinaten"/> gerechnet (Auftrag KL-2).
        /// <c>null</c>, wenn der Kopf keine oder keine plausiblen Lambert-Koordinaten
        /// führt — <b>nie eine stille Null</b>.
        /// </summary>
        public double? Laenge;

        /// <summary>Geographische Breite des Standorts [Grad, Nord positiv]; siehe
        /// <see cref="Laenge"/>.</summary>
        public double? Breite;

        /// <summary>Führt der Kopf einen lesbaren, plausiblen Standort?</summary>
        public bool StandortBekannt => Laenge.HasValue && Breite.HasValue;

        /// <summary>
        /// Die Größen, die die Datei führt und EPOS-Plan BENANNT verwirft (Entscheid des
        /// Anwenders: nur vorhandene Spalten füllen). Sie stehen im Herkunftsvermerk,
        /// damit niemand sie später in der Datenbank sucht.
        /// </summary>
        public IReadOnlyList<string> Verworfen = DwdTryLeser.VERWORFENE_GROESSEN;

        /// <summary>Die verworfenen Größen als ein Text, z. B. „p, WR, WG, …".</summary>
        public string VerworfenText => string.Join(", ", Verworfen);
    }

    /// <summary>
    /// Der LESER einer DWD-Testreferenzjahr-Datei (<c>.dat</c>) — die ZWEITE Klimaquelle
    /// neben PVGIS (Auftrag KL1-B).
    ///
    /// <para><b>Plattformfrei, ohne Netz, ohne Datenbank.</b> Er bekommt Zeilen oder
    /// einen Dateipfad und liefert dieselbe <see cref="TmyHourlyData"/>-Liste, die
    /// <c>PVGIS_EPW_Downloader.GetTMY</c> liefert. <c>KlimaImportAblauf.Rechnen</c>,
    /// <c>SolarCalculator.GetDailyAverages</c>, <c>Tagtypen</c> und
    /// <c>AccessRepository.SaveTmyData</c> bleiben dadurch UNVERÄNDERT.</para>
    ///
    /// <para><b>Das Format.</b> Kopfzeilen bis zur ersten Zeile, die getrimmt mit
    /// <c>***</c> beginnt; danach genau 8 760 Datenzeilen mit 17 weißraumgetrennten
    /// Feldern:</para>
    /// <code>
    /// RW HW MM DD HH t p WR WG N x RF B D A E IL
    /// </code>
    /// <list type="bullet">
    ///   <item><description><c>RW</c>/<c>HW</c> — Lambert-Koordinaten der Station,
    ///     je Datenzeile wiederholt (nicht übernommen; der STANDORT kommt aus dem
    ///     Kopf, siehe <see cref="TryKopf.Laenge"/>).</description></item>
    ///   <item><description><c>MM</c>/<c>DD</c>/<c>HH</c> — Monat, Tag, Stunde
    ///     <b>1…24 MEZ</b>; <c>HH</c> benennt das Intervall, das zu <c>HH:00</c>
    ///     endet.</description></item>
    ///   <item><description><c>t</c> — Lufttemperatur [°C] → <c>Temperature</c>.</description></item>
    ///   <item><description><c>B</c> — Direktstrahlung HORIZONTAL [W/m²],
    ///     <c>D</c> — Diffusstrahlung horizontal [W/m²] →
    ///     <c>GlobalIrradiance = B + D</c>, <c>DiffuseIrradiance = D</c>,
    ///     <c>DirectIrradiance = B</c> (noch horizontal, siehe
    ///     <see cref="DirektNormal"/>).</description></item>
    ///   <item><description><c>A</c> — atmosphärische Gegenstrahlung [W/m²],
    ///     <c>RF</c> — relative Feuchte [%], <c>N</c> — Bedeckungsgrad [Achtel]:
    ///     <b>gespeichert seit Schemaschritt 95</b> (Auftrag KL-3) in
    ///     <c>Tab_Solar(_STAMM).Gegenstrahlung</c>, <c>.Luftfeuchte</c> und
    ///     <c>.Bedeckungsgrad</c> — die Größen, die die Gebäudesimulation nach
    ///     VDI 6007 braucht.</description></item>
    ///   <item><description><c>p WR WG x E IL</c> — Druck, Windrichtung,
    ///     Windgeschwindigkeit, Wasserdampfgehalt, langwellige Ausstrahlung,
    ///     Qualitätsbit: <b>benannt verworfen</b>, weil es in <c>Tab_Solar_STAMM</c>
    ///     keine Spalte dafür gibt (Entscheid des Anwenders: nur vorhandene Spalten
    ///     füllen, nie still übergehen). Die Windgeschwindigkeit bleibt ausdrücklich
    ///     draußen — keine Spalte ohne Leser (Umsetzungskonzept Gebäudesimulation
    ///     F-S3).</description></item>
    /// </list>
    ///
    /// <para><b>Die ZEITBASIS.</b> Die Datei läuft in Ortszeit ohne Sommerzeit: Der
    /// Ortszeitindex einer Zeile ist <c>L = (TagImJahr − 1) · 24 + (HH − 1)</c> im
    /// 365-Tage-Raster (kein Schaltjahr, Wurzel-CLAUDE.md).
    /// <c>Tab_Solar_STAMM</c> führt die Reihe dagegen in UTC-Reihenfolge — das ist der
    /// Vertrag, den <see cref="SolarZeitbasis"/> beim LESEN wieder auflöst. TRY kennt
    /// keine MESZ, der Versatz ist deshalb ganzjährig eine Stunde:</para>
    /// <code>
    /// utc[u] = mez[(u + 1) % 8760]
    /// </code>
    /// <para><b>Was daraus folgt, ausdrücklich festgehalten:</b>
    /// <c>SolarZeitbasis.UtcIndex(L, 2025)</c> holt im WINTER <c>mez[L]</c> zurück
    /// (Versatz 1 h, genau die Drehung) und im SOMMER <c>mez[L − 1]</c> (Versatz 2 h,
    /// eine Stunde zu früh) — weil das Haus die Reihe als UTC liest und die EU-Regel
    /// anwendet, die TRY selbst nicht kennt. Das ist bewusst so: Eine TRY-Reihe
    /// „mitzudrehen" hieße, sie gegen die Bestandsregionen unterschiedlich zu
    /// behandeln.</para>
    ///
    /// <para><b>Der Zeitstempel</b> entsteht aus dem UTC-Index <c>u</c> mit dem
    /// Referenzjahr <see cref="DbWerte.SOLAR_REFERENZJAHR_STANDARD"/>
    /// (<c>Tag = u / 24 + 1</c>, <c>Stunde = u % 24</c>, Minute 0) im Format
    /// <c>yyyyMMdd:HHmm</c> — genau das, was <c>KlimaImportAblauf.Rechnen</c> und
    /// <c>GetDailyAverages</c> parsen.</para>
    /// </summary>
    public static class DwdTryLeser
    {
        /// <summary>Zahl der Felder einer Datenzeile.</summary>
        public const int FELDER = 17;

        /// <summary>Das Jahresraster des Hauses: 8 760 Stunden.</summary>
        public const int STUNDEN_JAHR = SolarZeitbasis.STUNDEN_JAHR;

        /// <summary>Solarkonstante als KLEMME der Direkt-Normal-Rechnung [W/m²].</summary>
        public const double SOLARKONSTANTE = 1367.0;

        /// <summary>
        /// Unter dieser Sonnenhöhe [Grad] wird NICHT auf Direkt-Normal umgerechnet.
        /// Bei kleinem <c>sin alpha</c> liefe <c>B / sin alpha</c> gegen unendlich, und
        /// schon ein Messrauschen von wenigen W/m² würde zu einem vierstelligen
        /// DNI-Wert — dieselbe Überlegung wie die Horizontklemme <c>cos 85°</c> in
        /// <c>SolarCalculator</c>.
        /// </summary>
        public const double MINDESTHOEHE_GRAD = 5.0;

        /// <summary>Die Trennzeile zwischen Kopf und Daten.</summary>
        public const string TRENNZEICHEN = "***";

        /// <summary>
        /// Die sechs Größen der Datei, für die es keine Spalte gibt — benannt verworfen.
        ///
        /// <para><b>Drei sind mit Schemaschritt 95 herausgefallen</b> (Auftrag KL-3):
        /// <c>N</c>, <c>RF</c> und <c>A</c> werden jetzt gespeichert. <c>WG</c> bleibt
        /// hier — keine Spalte ohne Leser.</para>
        /// </summary>
        public static readonly IReadOnlyList<string> VERWORFENE_GROESSEN = new[]
        {
            "p", "WR", "WG", "x", "E", "IL"
        };

        /// <summary>Feldindex des Bedeckungsgrads <c>N</c> [Achtel] in der Datenzeile.</summary>
        public const int FELD_N = 9;

        /// <summary>Feldindex der relativen Feuchte <c>RF</c> [%] in der Datenzeile.</summary>
        public const int FELD_RF = 11;

        /// <summary>Feldindex der atmosphärischen Gegenstrahlung <c>A</c> [W/m²].</summary>
        public const int FELD_A = 14;

        /// <summary>Tage je Monat im 365-Tage-Raster (kein Schaltjahr).</summary>
        private static readonly int[] TAGE_MONAT =
            { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Liest eine TRY-Datei vom Datenträger. Der Kern liest hier eine Datei, deren
        /// Pfad die OBERFLÄCHE gewählt hat — er sucht sie nie selbst.
        /// </summary>
        /// <exception cref="FileNotFoundException">Die Datei gibt es nicht.</exception>
        /// <exception cref="FormatException">Kopf, Spalten- oder Zeilenzahl passen nicht.</exception>
        public static List<TmyHourlyData> LesenDatei(string pfad, out TryKopf kopf,
                                                     bool vollesJahr = true)
        {
            if (string.IsNullOrWhiteSpace(pfad) || !File.Exists(pfad))
                throw new FileNotFoundException(pfad ?? "");

            // ReadAllLines vertraegt CRLF wie LF; die Kultur spielt keine Rolle, weil
            // jedes Feld mit InvariantCulture gelesen wird.
            return Lesen(File.ReadAllLines(pfad), out kopf, vollesJahr);
        }

        /// <summary>
        /// Liest NUR den Kopf einer TRY-Datei — bis zur Trennzeile <c>***</c>, ohne die
        /// 8 760 Datenzeilen zu deuten (Auftrag KL-2).
        ///
        /// <para><b>Wofür:</b> Der Klimadaten-Dialog belegt beim Wählen einer Datei den
        /// Standort vor. Dafür genügen die vier Dutzend Kopfzeilen; die ganze Datei zu
        /// parsen kostete ein Vielfaches und läge in der Oberfläche quer.</para>
        ///
        /// <para><b>Ohne Ausnahme:</b> Fehlt die Datei, ist sie nicht lesbar oder trägt
        /// sie keine Trennzeile, liefert die Methode <c>false</c> und einen leeren
        /// <see cref="TryKopf"/> — der Dialog meldet das, statt zu stürzen.</para>
        /// </summary>
        /// <param name="pfad">Der Pfad, den die OBERFLÄCHE gewählt hat.</param>
        /// <param name="kopf">Was der Kopf hergibt, samt Standort.</param>
        /// <returns><c>true</c>, wenn Kopf und Trennzeile gelesen wurden.</returns>
        public static bool KopfLesen(string pfad, out TryKopf kopf)
        {
            kopf = new TryKopf();

            if (string.IsNullOrWhiteSpace(pfad)) return false;

            try
            {
                if (!File.Exists(pfad)) return false;
                kopf = KopfLesen(File.ReadLines(pfad));
                return kopf.Kopfzeilen > 0 || kopf.StandortBekannt;
            }
            catch (FormatException) { kopf = new TryKopf(); return false; }
            catch (IOException) { kopf = new TryKopf(); return false; }
            catch (UnauthorizedAccessException) { kopf = new TryKopf(); return false; }
        }

        /// <summary>
        /// Liest den Kopf aus einer Zeilenfolge und HÖRT AN DER TRENNZEILE AUF — bei
        /// <c>File.ReadLines</c> heißt das: Der Rest der Datei wird nie gelesen.
        /// </summary>
        /// <exception cref="FormatException">Es gibt keine Trennzeile <c>***</c>.</exception>
        public static TryKopf KopfLesen(IEnumerable<string> zeilen)
        {
            if (zeilen == null) throw new ArgumentNullException(nameof(zeilen));

            var kopf = new TryKopf();
            int nummer = 0;

            foreach (string zeile in zeilen)
            {
                nummer++;
                string z = (zeile ?? "").Trim();

                if (z.StartsWith(TRENNZEICHEN, StringComparison.Ordinal))
                {
                    kopf.Kopfzeilen = nummer - 1;
                    StandortSetzen(kopf);
                    return kopf;
                }

                KopfzeileDeuten(kopf, z);
            }

            throw new FormatException(MyResource.Resource.KLIMA_TRY_KEIN_KOPFENDE);
        }

        /// <summary>
        /// Liest eine TRY-Reihe aus ihren Zeilen.
        /// </summary>
        /// <param name="zeilen">Kopf, Trennzeile und Datenzeilen — Zeilenenden
        /// beliebig, sie sind hier schon getrennt.</param>
        /// <param name="kopf">Was der Kopf hergibt, samt den verworfenen Größen.</param>
        /// <param name="vollesJahr"><c>true</c> (die Vorgabe): Es müssen genau 8 760
        /// Datenzeilen sein, und die Reihe wird von MEZ auf die UTC-Reihenfolge von
        /// <c>Tab_Solar_STAMM</c> gedreht. <c>false</c> ist die PRÜFOPTION für die
        /// synthetische Importprobe (72 Stunden): beliebige Zeilenzahl, keine Drehung,
        /// der Zeitstempel folgt unmittelbar MM/DD/HH.</param>
        /// <exception cref="FormatException">Mit Zeilennummer und Grund.</exception>
        public static List<TmyHourlyData> Lesen(IEnumerable<string> zeilen, out TryKopf kopf,
                                                bool vollesJahr = true)
        {
            if (zeilen == null) throw new ArgumentNullException(nameof(zeilen));

            kopf = new TryKopf();
            var roh = new List<TmyHourlyData>(vollesJahr ? STUNDEN_JAHR : 72);
            var ortszeit = new List<int>(roh.Capacity);

            bool imKopf = true;
            int nummer = 0;                 // Zeilennummer in der DATEI, 1-basiert

            foreach (string zeile in zeilen)
            {
                nummer++;
                string z = (zeile ?? "").Trim();

                if (imKopf)
                {
                    if (z.StartsWith(TRENNZEICHEN, StringComparison.Ordinal))
                    {
                        kopf.Kopfzeilen = nummer - 1;
                        StandortSetzen(kopf);
                        imKopf = false;
                        continue;
                    }
                    KopfzeileDeuten(kopf, z);
                    continue;
                }

                if (z.Length == 0) continue;     // eine leere Schlusszeile ist kein Fehler

                string[] f = z.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length != FELDER)
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_SPALTENZAHL,
                        nummer.ToString(CultureInfo.CurrentCulture),
                        FELDER.ToString(CultureInfo.CurrentCulture),
                        f.Length.ToString(CultureInfo.CurrentCulture)));

                int monat = Ganzzahl(f[2], nummer, "MM");
                int tag = Ganzzahl(f[3], nummer, "DD");
                int stunde = Ganzzahl(f[4], nummer, "HH");
                double t = Zahl(f[5], nummer, "t");
                double b = Zahl(f[12], nummer, "B");
                double d = Zahl(f[13], nummer, "D");

                // Schemaschritt 95 (Auftrag KL-3): die drei Groessen der
                // Gebaeudesimulation. Sie sind PFLICHTFELDER der Datenzeile - eine TRY
                // Datei, die sie nicht als Zahl fuehrt, ist nicht lesbar, und genau das
                // meldet Zahl() mit Zeilennummer und Feldname.
                double n = Zahl(f[FELD_N], nummer, "N");
                double rf = Zahl(f[FELD_RF], nummer, "RF");
                double a = Zahl(f[FELD_A], nummer, "A");

                if (monat < 1 || monat > 12 || tag < 1 || tag > TAGE_MONAT[monat - 1] ||
                    stunde < 1 || stunde > 24)
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_ZEITFELD,
                        nummer.ToString(CultureInfo.CurrentCulture),
                        monat.ToString(CultureInfo.CurrentCulture),
                        tag.ToString(CultureInfo.CurrentCulture),
                        stunde.ToString(CultureInfo.CurrentCulture)));

                roh.Add(new TmyHourlyData
                {
                    Temperature = t,
                    GlobalIrradiance = b + d,
                    DirectIrradiance = b,        // noch HORIZONTAL - siehe DirektNormal
                    DiffuseIrradiance = d,
                    Humidity = rf,               // RF -> Tab_Solar(_STAMM).Luftfeuchte
                    Gegenstrahlung = a,          // A  -> Tab_Solar(_STAMM).Gegenstrahlung
                    Bedeckungsgrad = n,          // N  -> Tab_Solar(_STAMM).Bedeckungsgrad
                    WindSpeed = 0                // WG ist benannt verworfen
                });

                ortszeit.Add((TagImJahr(monat, tag) - 1) * 24 + (stunde - 1));
            }

            if (imKopf)
                throw new FormatException(MyResource.Resource.KLIMA_TRY_KEIN_KOPFENDE);

            if (!vollesJahr)
            {
                // Pruefoption: kein Jahresraster, kein Drehen - der Zeitstempel folgt
                // unmittelbar MM/DD/HH (Ortszeit).
                for (int i = 0; i < roh.Count; i++) roh[i].TimeString = Zeitstempel(ortszeit[i]);
                return roh;
            }

            if (roh.Count != STUNDEN_JAHR)
                throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_ZEILENZAHL,
                    STUNDEN_JAHR.ToString(CultureInfo.CurrentCulture),
                    roh.Count.ToString(CultureInfo.CurrentCulture),
                    nummer.ToString(CultureInfo.CurrentCulture)));

            // Die Datei kommt in Ortszeitreihenfolge; sicherheitshalber wird sie ueber
            // den Ortszeitindex einsortiert, statt der Reihenfolge zu vertrauen.
            var mez = new TmyHourlyData[STUNDEN_JAHR];
            for (int i = 0; i < roh.Count; i++)
            {
                int l = ortszeit[i];
                if (mez[l] != null)
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_DOPPELTE_STUNDE,
                        (i + kopf.Kopfzeilen + 2).ToString(CultureInfo.CurrentCulture)));
                mez[l] = roh[i];
            }

            // MEZ -> UTC: utc[u] = mez[(u + 1) % 8760]. Der Jahresumlauf ist gewollt -
            // die erste MEZ-Stunde des 1.1. ist die LETZTE UTC-Stunde des Jahres.
            var utc = new List<TmyHourlyData>(STUNDEN_JAHR);
            for (int u = 0; u < STUNDEN_JAHR; u++)
            {
                TmyHourlyData s = mez[(u + 1) % STUNDEN_JAHR];
                s.TimeString = Zeitstempel(u);
                utc.Add(s);
            }
            return utc;
        }

        /// <summary>
        /// Rechnet die HORIZONTALE Direktstrahlung der TRY-Datei auf die
        /// DIREKT-NORMAL-Strahlung um, die <c>Tab_Solar_STAMM</c> führt und
        /// <c>SolarCalculator.CalculateHourly</c> erwartet
        /// (<c>direct = dni * cosTheta</c>).
        ///
        /// <code>
        /// alpha &lt; 5°  ->  Direct = 0, Diffuse += B          (B fällt in den Diffusanteil)
        /// sonst        ->  Direct = min(B / sin alpha, 1367)
        /// </code>
        ///
        /// <para><b>Die beiden Regeln, ausdrücklich benannt.</b> (1) Unter
        /// <see cref="MINDESTHOEHE_GRAD"/> wird nicht geteilt: <c>sin alpha</c> geht
        /// dort gegen 0, und die Division machte aus wenigen W/m² Messrauschen einen
        /// vierstelligen Wert. Der Betrag geht nicht verloren, sondern in den
        /// Diffusanteil — so bleibt die GLOBALSTRAHLUNG <c>B + D</c> in jeder Stunde
        /// unverändert, und genau sie ist die Größe, die in die Bilanz eingeht.
        /// (2) Die Klemme auf die Solarkonstante <see cref="SOLARKONSTANTE"/>: Mehr als
        /// die extraterrestrische Normalstrahlung kann am Boden nicht ankommen; ein
        /// höherer Wert wäre ein Rechenartefakt kleiner Sonnenhöhen.</para>
        ///
        /// <para><b>Vor <c>Rechnen</c> aufrufen</b> — danach führt
        /// <c>DirectIrradiance</c> Direkt-Normal, und die vier Fassadenwerte stimmen
        /// mit denen einer PVGIS-Region überein.</para>
        /// </summary>
        public static void DirektNormal(List<TmyHourlyData> stunden, double lon, double lat)
        {
            if (stunden == null) return;

            foreach (TmyHourlyData s in stunden)
            {
                if (string.IsNullOrEmpty(s.TimeString)) continue;

                DateTime dt = DateTime.ParseExact(s.TimeString, "yyyyMMdd:HHmm",
                                                  CultureInfo.InvariantCulture);

                double b = s.DirectIrradiance;          // horizontal, wie gelesen
                double alpha = SolarCalculator.Sonnenhoehe(lon, lat, dt.DayOfYear, dt.Hour);

                if (alpha < MINDESTHOEHE_GRAD)
                {
                    s.DirectIrradiance = 0;
                    s.DiffuseIrradiance += b;           // GHI = B + D bleibt erhalten
                    continue;
                }

                double dni = b / Math.Sin(alpha * Math.PI / 180.0);
                s.DirectIrradiance = dni > SOLARKONSTANTE ? SOLARKONSTANTE : dni;
            }
        }

        // =====================================================================
        //  Intern
        // =====================================================================

        /// <summary>
        /// Der Zeitstempel zu einem Index des Jahresrasters — <c>yyyyMMdd:HHmm</c> mit
        /// dem Referenzjahr des Hauses.
        /// </summary>
        public static string Zeitstempel(int index)
        {
            int tag = index / 24 + 1;               // 1…365
            int stunde = index % 24;                // 0…23

            int monat = 1;
            int rest = tag;
            while (monat < 12 && rest > TAGE_MONAT[monat - 1])
            {
                rest -= TAGE_MONAT[monat - 1];
                monat++;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:D4}{1:D2}{2:D2}:{3:D2}00",
                                 DbWerte.SOLAR_REFERENZJAHR_STANDARD, monat, rest, stunde);
        }

        /// <summary>Tag im 365-Tage-Raster (1…365) aus Monat und Tag.</summary>
        public static int TagImJahr(int monat, int tag)
        {
            int t = tag;
            for (int m = 1; m < monat; m++) t += TAGE_MONAT[m - 1];
            return t;
        }

        /// <summary>
        /// Deutet eine Kopfzeile, soweit sie eine der fünf Angaben trägt. Sie ist FREIER
        /// TEXT — was nicht passt, bleibt liegen; der Kopf ist kein Pflichtteil.
        /// </summary>
        private static void KopfzeileDeuten(TryKopf kopf, string zeile)
        {
            if (zeile.Length == 0) return;

            if (kopf.Rechtswert.Length == 0 && Traegt(zeile, "Rechtswert"))
                kopf.Rechtswert = Wert(zeile);
            else if (kopf.Hochwert.Length == 0 && Traegt(zeile, "Hochwert"))
                kopf.Hochwert = Wert(zeile);
            else if (kopf.Hoehe.Length == 0 && (Traegt(zeile, "Hoehenlage") || Traegt(zeile, "Höhenlage")))
                kopf.Hoehe = Wert(zeile);
            else if (kopf.Art.Length == 0 && Traegt(zeile, "Art des TRY"))
                kopf.Art = Wert(zeile);
            else if (kopf.Bezugszeitraum.Length == 0 && Traegt(zeile, "Bezugszeitraum"))
                kopf.Bezugszeitraum = Wert(zeile);
        }

        /// <summary>
        /// Rechnet <see cref="TryKopf.Rechtswert"/>/<see cref="TryKopf.Hochwert"/> in
        /// Länge und Breite um (Auftrag KL-2). Fehlt eine der beiden Angaben, ist sie
        /// keine Zahl oder liegt der Punkt außerhalb der Plausibilitätsgrenzen von
        /// <see cref="LambertKoordinaten"/>, bleiben beide Felder <c>null</c> —
        /// <b>keine stille Null</b>.
        /// </summary>
        private static void StandortSetzen(TryKopf kopf)
        {
            kopf.Laenge = null;
            kopf.Breite = null;

            if (!Meterwert(kopf.Rechtswert, out double rw)) return;
            if (!Meterwert(kopf.Hochwert, out double hw)) return;

            if (LambertKoordinaten.NachGeographisch(rw, hw, out double lon, out double lat))
            {
                kopf.Laenge = lon;
                kopf.Breite = lat;
            }
        }

        /// <summary>
        /// Die Zahl aus einer Kopfangabe wie „3909500 Meter“: das ERSTE weißraumfreie
        /// Stück, invariant gelesen; ein Dezimalkomma wird zum Punkt.
        /// </summary>
        public static bool Meterwert(string angabe, out double wert)
        {
            wert = 0;
            string a = (angabe ?? "").Trim();
            if (a.Length == 0) return false;

            int ende = a.IndexOfAny(new[] { ' ', '\t' });
            string zahl = ende > 0 ? a.Substring(0, ende) : a;
            zahl = zahl.Replace(',', '.');

            return double.TryParse(zahl, NumberStyles.Float, CultureInfo.InvariantCulture,
                                   out wert);
        }

        private static bool Traegt(string zeile, string wort)
            => zeile.IndexOf(wort, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Alles hinter dem ersten Doppelpunkt, sonst die ganze Zeile.</summary>
        private static string Wert(string zeile)
        {
            int p = zeile.IndexOf(':');
            return (p >= 0 ? zeile.Substring(p + 1) : zeile).Trim();
        }

        private static int Ganzzahl(string feld, int nummer, string name)
        {
            if (int.TryParse(feld, NumberStyles.Integer, CultureInfo.InvariantCulture, out int w))
                return w;
            throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KLIMA_TRY_FELDWERT,
                nummer.ToString(CultureInfo.CurrentCulture), name, feld ?? ""));
        }

        private static double Zahl(string feld, int nummer, string name)
        {
            if (double.TryParse(feld, NumberStyles.Float, CultureInfo.InvariantCulture, out double w))
                return w;
            throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KLIMA_TRY_FELDWERT,
                nummer.ToString(CultureInfo.CurrentCulture), name, feld ?? ""));
        }
    }
}
