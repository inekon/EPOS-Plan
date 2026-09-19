using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine TRY-REGION des Regionalpakets: ihre Nummer, die Koordinate ihrer
    /// Bezugsstation und die Entfernung zum gesuchten Ort.
    /// </summary>
    public sealed class TryRegion
    {
        /// <summary>Regionsnummer 1…15 (der Ordner unter <c>1_raw-data/</c>).</summary>
        public int Nummer;

        /// <summary>Breitengrad der Bezugsstation [Grad].</summary>
        public double Latitude;

        /// <summary>Längengrad der Bezugsstation [Grad].</summary>
        public double Longitude;

        /// <summary>Entfernung des gesuchten Ortes zur Bezugsstation [km].</summary>
        public double EntfernungKm;

        /// <summary>Die zwölf Ziffern des Dateinamens (<c>lat·1e4</c> + <c>lon·1e4</c>).</summary>
        public string Ziffern = "";

        /// <summary>Koordinate als Text für den Herkunftsvermerk, z. B. „53,5591 / 8,5872".</summary>
        public string Koordinatentext => string.Format(CultureInfo.CurrentCulture,
            "{0:F4} / {1:F4}", Latitude, Longitude);
    }

    /// <summary>Was ein Lauf des <see cref="TryPaketLeser"/> zurückgibt.</summary>
    public sealed class TryPaketErgebnis
    {
        /// <summary>Die 8 760 Stundenwerte in der UTC-Reihenfolge von <c>Tab_Solar_STAMM</c>.</summary>
        public List<TmyHourlyData> Stunden = new List<TmyHourlyData>();

        /// <summary>Der Kopf der gelesenen Datei.</summary>
        public TryKopf Kopf = new TryKopf();

        /// <summary>Die gewählte Region.</summary>
        public TryRegion Region = new TryRegion();

        /// <summary>Der Eintragsname im Paket, z. B. <c>1_raw-data/1/TRY2015_…_Jahr.dat</c>.</summary>
        public string Eintrag = "";

        /// <summary>Zahl der Bereichsabrufe (0 bei einer lokalen Datei).</summary>
        public long Abrufe;

        /// <summary>Übertragene Bytes (0 bei einer lokalen Datei).</summary>
        public long UebertrageneBytes;
    }

    /// <summary>
    /// Der LESER des offenen TRY-REGIONALPAKETS (RE-Lab-Projects,
    /// <c>TRY_DE_2015_2045</c>, <c>data.zip</c>) — die DRITTE Klimaquelle (Auftrag KL1-B).
    ///
    /// <para><b>Das Paket ist 892 MB groß; heruntergeladen wird es nie.</b> Gebraucht
    /// werden zwei Dinge: das Zentralverzeichnis des ZIP am Dateiende und EINE
    /// <c>.dat</c>-Datei von rund 155 KB (deflate). Beides holt
    /// <see cref="BereichStream"/> über HTTP-Bereichsabrufe — einige hundert Kilobyte
    /// statt 892 MB. Kann die Adresse keine Teilabrufe, wird das BENANNT abgelehnt
    /// (<c>KLIMA_TRY_KEIN_BEREICH</c>); ein stiller Volldownload wäre die schlechtere
    /// Überraschung.</para>
    ///
    /// <para><b>Das Zentralverzeichnis IST die Regionsliste</b> — im Code steht keine
    /// Tabelle der 15 Regionen. Die Einträge heißen
    /// <c>1_raw-data/&lt;1…15&gt;/TRY&lt;2015|2045&gt;_&lt;12 Ziffern&gt;_&lt;Jahr|Somm|Wint&gt;.dat</c>;
    /// die zwölf Ziffern sind <c>lat·1e4</c> (sechs Stellen) und <c>lon·1e4</c> (sechs
    /// Stellen), <c>TRY2015_535591085872_Jahr.dat</c> also 53,5591 / 8,5872. Gewählt
    /// wird die Region mit der kleinsten Entfernung (Haversine); liegt auch die weiter
    /// als <see cref="MAX_ENTFERNUNG_KM"/> weg, bricht der Lauf mit Grund ab — das
    /// Paket deckt nur Deutschland ab.</para>
    ///
    /// <para><b>Ohne Netz und ohne Datenbank testbar:</b> Die zweite Quelle ist eine
    /// lokale <c>data.zip</c>, und der Prüfstand legt ein ZIP im Speicher an.</para>
    /// </summary>
    public static class TryPaketLeser
    {
        /// <summary>Die Adresse, wenn in den Einstellungen keine steht.</summary>
        public const string ADRESSE_VORGABE =
            "https://github.com/RE-Lab-Projects/TRY_DE_2015_2045/releases/download/v1.4.0/data.zip";

        /// <summary>Der Einstellungsschlüssel der Adresse.</summary>
        public const string SCHLUESSEL_ADRESSE = "TRYRegionalUrl";

        /// <summary>Weiter weg als das rechnet der Import nicht — das Paket endet an der Grenze.</summary>
        public const double MAX_ENTFERNUNG_KM = 300.0;

        /// <summary>Der Ordner der Rohdaten im Paket.</summary>
        public const string ORDNER_ROH = "1_raw-data/";

        /// <summary>Erdradius der Haversine-Formel [km].</summary>
        private const double ERDRADIUS_KM = 6371.0;

        /// <summary>
        /// <c>1_raw-data/&lt;n&gt;/TRY&lt;jahr&gt;_&lt;12 Ziffern&gt;_&lt;kuerzel&gt;.dat</c>.
        /// </summary>
        private static readonly Regex EINTRAG = new Regex(
            @"^1_raw-data/(?<n>\d+)/TRY(?<jahr>\d{4})_(?<ziffern>\d{12})_(?<sz>[A-Za-z]+)\.dat$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>Das Namenskürzel eines Szenarios im Paket.</summary>
        public static string Kuerzel(TrySzenario szenario)
        {
            switch (szenario)
            {
                case TrySzenario.Sommerwarm: return "Somm";
                case TrySzenario.Winterkalt: return "Wint";
                default: return "Jahr";
            }
        }

        /// <summary>
        /// Liest die nächstgelegene Region aus dem Paket im NETZ — über Bereichsabrufe.
        /// </summary>
        /// <exception cref="InvalidOperationException">Die Adresse kann keine Teilabrufe,
        /// oder der nächste Regionsmittelpunkt liegt zu weit weg.</exception>
        public static async Task<TryPaketErgebnis> LesenAusNetzAsync(
            INetzbereich bereich, string adresse, double lon, double lat,
            int jahr, TrySzenario szenario, CancellationToken abbruch = default)
        {
            if (bereich == null) throw new ArgumentNullException(nameof(bereich));
            if (string.IsNullOrWhiteSpace(adresse)) adresse = ADRESSE_VORGABE;

            using (BereichStream strom = await BereichStream.OeffnenAsync(
                       bereich, adresse, abbruch).ConfigureAwait(false))
            {
                TryPaketErgebnis erg = AusStrom(strom, lon, lat, jahr, szenario);
                erg.Abrufe = strom.Abrufe;
                erg.UebertrageneBytes = strom.UebertrageneBytes;
                return erg;
            }
        }

        /// <summary>Liest die nächstgelegene Region aus einer LOKALEN <c>data.zip</c>.</summary>
        public static TryPaketErgebnis LesenAusDatei(string zipPfad, double lon, double lat,
                                                     int jahr, TrySzenario szenario)
        {
            if (string.IsNullOrWhiteSpace(zipPfad) || !File.Exists(zipPfad))
                throw new FileNotFoundException(zipPfad ?? "");

            using (FileStream fs = File.OpenRead(zipPfad))
            {
                return AusStrom(fs, lon, lat, jahr, szenario);
            }
        }

        /// <summary>
        /// Der gemeinsame Weg: Zentralverzeichnis lesen, Region wählen, EINEN Eintrag
        /// entpacken, Zeilen an <see cref="DwdTryLeser.Lesen"/>.
        /// </summary>
        public static TryPaketErgebnis AusStrom(Stream strom, double lon, double lat,
                                                int jahr, TrySzenario szenario)
        {
            if (strom == null) throw new ArgumentNullException(nameof(strom));

            var erg = new TryPaketErgebnis();

            using (var archiv = new ZipArchive(strom, ZipArchiveMode.Read, leaveOpen: true))
            {
                TryRegion beste = null;
                var kandidaten = new Dictionary<int, ZipArchiveEntry>();

                foreach (ZipArchiveEntry eintrag in archiv.Entries)
                {
                    Match m = EINTRAG.Match(eintrag.FullName.Replace('\\', '/'));
                    if (!m.Success) continue;
                    if (m.Groups["jahr"].Value != jahr.ToString(CultureInfo.InvariantCulture)) continue;

                    string ziffern = m.Groups["ziffern"].Value;
                    int nummer = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);

                    double stationLat = double.Parse(ziffern.Substring(0, 6),
                        CultureInfo.InvariantCulture) / 10000.0;
                    double stationLon = double.Parse(ziffern.Substring(6, 6),
                        CultureInfo.InvariantCulture) / 10000.0;

                    double km = EntfernungKm(lat, lon, stationLat, stationLon);

                    if (beste == null || km < beste.EntfernungKm)
                    {
                        beste = new TryRegion
                        {
                            Nummer = nummer,
                            Latitude = stationLat,
                            Longitude = stationLon,
                            EntfernungKm = km,
                            Ziffern = ziffern
                        };
                    }

                    if (m.Groups["sz"].Value.Equals(Kuerzel(szenario), StringComparison.OrdinalIgnoreCase)
                        && !kandidaten.ContainsKey(nummer))
                        kandidaten[nummer] = eintrag;
                }

                if (beste == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_KEINE_REGION,
                        jahr.ToString(CultureInfo.CurrentCulture)));

                if (beste.EntfernungKm > MAX_ENTFERNUNG_KM)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_AUSSERHALB,
                        beste.EntfernungKm.ToString("F0", CultureInfo.CurrentCulture),
                        MAX_ENTFERNUNG_KM.ToString("F0", CultureInfo.CurrentCulture)));

                if (!kandidaten.TryGetValue(beste.Nummer, out ZipArchiveEntry datei))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_KEIN_SZENARIO,
                        Kuerzel(szenario),
                        beste.Nummer.ToString(CultureInfo.CurrentCulture)));

                erg.Region = beste;
                erg.Eintrag = datei.FullName;
                erg.Stunden = DwdTryLeser.Lesen(Zeilen(datei), out TryKopf kopf);
                erg.Kopf = kopf;
            }

            return erg;
        }

        /// <summary>Die Zeilen eines Paketeintrags — entpackt, nie als Ganzes im Speicher.</summary>
        private static IEnumerable<string> Zeilen(ZipArchiveEntry eintrag)
        {
            using (Stream s = eintrag.Open())
            using (var leser = new StreamReader(s))
            {
                string zeile;
                while ((zeile = leser.ReadLine()) != null) yield return zeile;
            }
        }

        /// <summary>Großkreisentfernung zweier Koordinaten [km] (Haversine).</summary>
        public static double EntfernungKm(double lat1, double lon1, double lat2, double lon2)
        {
            double r = Math.PI / 180.0;
            double dLat = (lat2 - lat1) * r;
            double dLon = (lon2 - lon1) * r;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * r) * Math.Cos(lat2 * r) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return 2 * ERDRADIUS_KM * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
        }
    }

    /// <summary>
    /// Ein LESESTROM über HTTP-BEREICHSABRUFE — die 892 MB des Regionalpakets bleiben
    /// dort, wo sie liegen.
    ///
    /// <para><b>Warum ein eigener Stream.</b> <see cref="ZipArchive"/> im Lesemodus
    /// braucht einen Strom, der SUCHEN kann: Er springt ans Dateiende zum
    /// Zentralverzeichnis und von dort an den Anfang des gewünschten Eintrags. Genau
    /// das leistet diese Klasse mit Bereichsabrufen — sie holt je Sprung einen Block
    /// von <see cref="BLOCK"/> Byte und bedient alle Lesewünsche daraus.</para>
    ///
    /// <para><b>Der Prüfpunkt der Adresse.</b> Der erste Abruf liefert neben den Daten
    /// die Gesamtlänge (aus <c>Content-Range</c>). Ist sie <c>&lt;= 0</c> oder kommen
    /// MEHR Bytes zurück als angefordert, dann beantwortet die Adresse Bereiche nicht,
    /// sondern schickt die ganze Datei — der Lauf bricht dann BENANNT ab
    /// (<c>KLIMA_TRY_KEIN_BEREICH</c>), statt 892 MB zu ziehen.</para>
    ///
    /// <para><b>Synchron über einen asynchronen Delegaten.</b> <see cref="ZipArchive"/>
    /// liest synchron; die Bereichsabrufe sind asynchron. Der erste Abruf läuft deshalb
    /// in der Fabrik <see cref="OeffnenAsync"/>, jeder weitere blockiert in
    /// <see cref="Read(byte[], int, int)"/>. Der ganze Import läuft ohnehin in einem
    /// <c>Task.Run</c> der Hülle (A-4), nie auf dem Anzeigefaden.</para>
    /// </summary>
    public sealed class BereichStream : Stream
    {
        /// <summary>Leseblock eines Bereichsabrufs [Byte].</summary>
        public const int BLOCK = 256 * 1024;

        private readonly INetzbereich _bereich;
        private readonly string _adresse;
        private readonly CancellationToken _abbruch;

        private long _laenge;
        private long _pos;
        private byte[] _puffer = Array.Empty<byte>();
        private long _pufferVon = -1;

        private BereichStream(INetzbereich bereich, string adresse, CancellationToken abbruch)
        {
            _bereich = bereich;
            _adresse = adresse;
            _abbruch = abbruch;
        }

        /// <summary>Zahl der bisherigen Bereichsabrufe (Messgröße des Prüfstands).</summary>
        public long Abrufe { get; private set; }

        /// <summary>Summe der übertragenen Bytes (Messgröße des Prüfstands).</summary>
        public long UebertrageneBytes { get; private set; }

        /// <summary>
        /// Öffnet den Strom und holt dabei den ERSTEN Block — er entscheidet, ob die
        /// Adresse Bereichsabrufe kann.
        /// </summary>
        public static async Task<BereichStream> OeffnenAsync(
            INetzbereich bereich, string adresse, CancellationToken abbruch = default)
        {
            if (bereich == null) throw new ArgumentNullException(nameof(bereich));

            var s = new BereichStream(bereich, adresse ?? "", abbruch);
            await s.HolenAsync(0).ConfigureAwait(false);
            return s;
        }

        private async Task HolenAsync(long von)
        {
            (byte[] Daten, long Gesamtlaenge) antwort =
                await _bereich(_adresse, von, BLOCK, _abbruch).ConfigureAwait(false);

            byte[] daten = antwort.Daten ?? Array.Empty<byte>();

            // Der Prüfpunkt: keine Gesamtlänge (kein Content-Range) oder mehr Bytes als
            // angefordert heisst „die Adresse kann keine Teilabrufe".
            if (antwort.Gesamtlaenge <= 0 || daten.LongLength > BLOCK)
                throw new InvalidOperationException(MyResource.Resource.KLIMA_TRY_KEIN_BEREICH);

            _laenge = antwort.Gesamtlaenge;
            _puffer = daten;
            _pufferVon = von;

            Abrufe++;
            UebertrageneBytes += daten.LongLength;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _laenge;

        public override long Position
        {
            get => _pos;
            set => _pos = value < 0 ? 0 : value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (count <= 0 || _pos >= _laenge) return 0;

            if (_pufferVon < 0 || _pos < _pufferVon || _pos >= _pufferVon + _puffer.LongLength)
                HolenAsync(_pos).GetAwaiter().GetResult();

            int imPuffer = (int)(_pos - _pufferVon);
            int n = Math.Min(count, _puffer.Length - imPuffer);
            if (n <= 0) return 0;

            Array.Copy(_puffer, imPuffer, buffer, offset, n);
            _pos += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long ziel;
            switch (origin)
            {
                case SeekOrigin.Begin: ziel = offset; break;
                case SeekOrigin.Current: ziel = _pos + offset; break;
                default: ziel = _laenge + offset; break;
            }
            if (ziel < 0) ziel = 0;
            _pos = ziel;
            return _pos;
        }

        public override void Flush() { }

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();
    }
}
