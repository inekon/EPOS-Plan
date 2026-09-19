using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>Wie ein Klimaimport ausgegangen ist.</summary>
    public enum KlimaImportAusgang
    {
        /// <summary>Die Region steht samt Stunden- und Tageswerten.</summary>
        Erfolg,
        /// <summary>Der Anwender hat abgebrochen — stiller Ausstieg.</summary>
        Abgebrochen,
        /// <summary>Eine Eingabe fehlt oder ist keine Zahl.</summary>
        Eingabefehler,
        /// <summary>Es gibt schon eine Region dieses Namens.</summary>
        Dublette,
        /// <summary>Der Ortsname war nicht aufzulösen.</summary>
        OrtUnbekannt,
        /// <summary>Der Datenabruf ist gescheitert.</summary>
        Netzfehler,
        /// <summary>Das Schreiben ist gescheitert; es wurde zurückgerollt.</summary>
        Schreibfehler,
        /// <summary>
        /// Eine gewählte Datei ist nicht lesbar — TRY-Datei oder Regionalpaket
        /// (Auftrag KL1-B). <b>Am ENDE der Aufzählung</b>, damit die Zahlenwerte der
        /// bestehenden Ausgänge sich nicht verschieben.
        /// </summary>
        Dateifehler
    }

    /// <summary>Das Ergebnis eines Klimaimports.</summary>
    public sealed class KlimaImportErgebnis
    {
        public KlimaImportAusgang Ausgang = KlimaImportAusgang.Abgebrochen;

        /// <summary>Der angelegte Regionsname; leer, wenn nichts angelegt wurde.</summary>
        public string Bezeichner = "";

        /// <summary>Die Stamm-Id der neuen Region; 0, wenn nichts angelegt wurde.</summary>
        public int Id;

        /// <summary>Zahl der geschriebenen Stundenwerte.</summary>
        public int Stundenwerte;

        /// <summary>Zahl der geschriebenen Tageswerte.</summary>
        public int Tageswerte;

        /// <summary>Der fertige Meldungstext; leer heißt: nichts zu melden.</summary>
        public string Meldung = "";

        public bool Erfolgreich => Ausgang == KlimaImportAusgang.Erfolg;
    }

    /// <summary>
    /// Was die REGIONSVORSCHAU zurückgibt (Auftrag KL-3, Anwenderentscheid 19.09.2026
    /// Punkt b): welche TRY-Region der eingetragene Standort trifft — <b>bevor</b>
    /// eingelesen wird.
    /// </summary>
    public sealed class KlimaVorschauErgebnis
    {
        /// <summary>Wie die Vorschau ausgegangen ist; die Ausgänge des Imports.</summary>
        public KlimaImportAusgang Ausgang = KlimaImportAusgang.Abgebrochen;

        /// <summary>Die fertige Ergebnis- oder Fehlerzeile; leer heißt: nichts zu melden.</summary>
        public string Meldung = "";

        /// <summary>Die getroffene Region; <c>null</c>, wenn keine ermittelt wurde.</summary>
        public TryRegion Region;

        /// <summary>Was das Paket für diese Region führt — Jahr und Szenario.</summary>
        public IReadOnlyList<TryAusfuehrung> Ausfuehrungen = new List<TryAusfuehrung>();

        public bool Erfolgreich => Ausgang == KlimaImportAusgang.Erfolg;
    }

    /// <summary>
    /// Woher die Koordinaten kommen — die zwei Zweige des Vorläufers.
    /// </summary>
    public enum KlimaImportArt
    {
        /// <summary>Aus einem Ortsnamen (Geokodierung bei Nominatim).</summary>
        AusOrtsname,
        /// <summary>Aus Longitude, Latitude und einer Bezeichnung von Hand.</summary>
        AusKoordinaten
    }

    /// <summary>
    /// WOHER die Stundenwerte kommen (Auftrag KL1-B). Die Vorgabe
    /// <see cref="PvgisTmy"/> ist der Bestand — jeder Auftrag ohne Angabe verhält sich
    /// wie bisher.
    /// </summary>
    public enum KlimaQuelle
    {
        /// <summary>PVGIS-TMY über das Netz (der Bestand, weltweit).</summary>
        PvgisTmy,
        /// <summary>Eine DWD-TRY-Datei (<c>.dat</c>) vom Rechner des Anwenders.</summary>
        TryDatei,
        /// <summary>Die offenen TRY-Regionaldaten (Deutschland) aus <c>data.zip</c>.</summary>
        TryRegional
    }

    /// <summary>
    /// Die drei TRY-Ausprägungen eines Bezugsjahres. Vorgabe ist das
    /// <see cref="MittleresJahr"/> — so heißt im Paket die Datei <c>…_Jahr.dat</c>.
    /// </summary>
    public enum TrySzenario
    {
        /// <summary>Mittleres Jahr (<c>Jahr</c>).</summary>
        MittleresJahr,
        /// <summary>Sommerwarm (<c>Somm</c>).</summary>
        Sommerwarm,
        /// <summary>Winterkalt (<c>Wint</c>).</summary>
        Winterkalt
    }

    /// <summary>
    /// Die Eingaben eines Klimaimports.
    /// </summary>
    public sealed class KlimaImportAuftrag
    {
        public KlimaImportArt Art = KlimaImportArt.AusOrtsname;

        /// <summary>Der Ortsname (Ausprägung <see cref="KlimaImportArt.AusOrtsname"/>).</summary>
        public string Ortsname = "";

        /// <summary>Die Bezeichnung (Ausprägung <see cref="KlimaImportArt.AusKoordinaten"/>).</summary>
        public string Bezeichnung = "";

        public double Longitude;
        public double Latitude;

        /// <summary>
        /// Woher die Stundenwerte kommen. <b>Vorgabe <see cref="KlimaQuelle.PvgisTmy"/></b> —
        /// jeder Auftrag, der nichts sagt, läuft wie vor KL1-B.
        /// </summary>
        public KlimaQuelle Quelle = KlimaQuelle.PvgisTmy;

        /// <summary>Pfad der DWD-TRY-Datei (<see cref="KlimaQuelle.TryDatei"/>).</summary>
        public string TryPfad = "";

        /// <summary>
        /// Pfad einer LOKALEN <c>data.zip</c> (<see cref="KlimaQuelle.TryRegional"/>);
        /// leer heißt: über die eingestellte Adresse abrufen.
        /// </summary>
        public string TryPaketPfad = "";

        /// <summary>Das Szenario der Regionaldaten; Vorgabe mittleres Jahr.</summary>
        public TrySzenario Szenario = TrySzenario.MittleresJahr;

        /// <summary>Das Bezugsjahr der Regionaldaten (2015 oder 2045); Vorgabe 2015.</summary>
        public int TryJahr = TRY_JAHR_VORGABE;

        /// <summary>Das vorgegebene Bezugsjahr der Regionaldaten.</summary>
        public const int TRY_JAHR_VORGABE = 2015;

        /// <summary>Das zweite wählbare Bezugsjahr (Projektion).</summary>
        public const int TRY_JAHR_PROJEKTION = 2045;
    }

    /// <summary>
    /// Woher die TMY-Stundenwerte kommen. <b>Der Netzzugriff des Programms</b> hängt an
    /// diesem Delegaten (Risiko R-W14c-5): Unter Windows ist es
    /// <c>PVGIS_EPW_Downloader.GetTMY</c>, in der Probe eine eingefrorene Datei.
    ///
    /// <para><b>Seit KL1-B gibt es einen ZWEITEN Netzweg</b> — <see cref="INetzbereich"/>
    /// für die TRY-Regionaldaten. Beide sind Delegaten, beide belegt die SCHALE; der
    /// Kern kennt weder <c>HttpClient</c> noch eine Adresse, und ohne Delegat gibt es
    /// den Weg schlicht nicht. Die DWD-TRY-DATEI braucht gar kein Netz.</para>
    /// </summary>
    public delegate Task<List<TmyHourlyData>> ITmyQuelle(double lon, double lat, int azimut);

    /// <summary>
    /// Ein BEREICHSABRUF über das Netz — <c>HTTP Range</c> (Auftrag KL1-B).
    ///
    /// <para><b>Wozu.</b> Das TRY-Regionalpaket <c>data.zip</c> ist 892 MB groß;
    /// gebraucht werden daraus das Zentralverzeichnis am Dateiende und EINE
    /// <c>.dat</c>-Datei von rund 155 KB. <see cref="BereichStream"/> setzt daraus einen
    /// suchfähigen Lesestrom zusammen, auf dem <c>ZipArchive</c> arbeitet.</para>
    ///
    /// <para><b>Der Vertrag.</b> <paramref name="laenge"/> kleiner 0 heißt „bis zum
    /// Ende". Zurück kommen die gelesenen Bytes und die GESAMTLÄNGE der Datei — unter
    /// Windows aus dem Kopf <c>Content-Range</c>. Kann die Adresse keine Teilabrufe
    /// (Gesamtlänge ≤ 0 oder mehr Bytes als angefordert), wird der Lauf BENANNT
    /// abgebrochen; ein stiller Volldownload findet nie statt.</para>
    /// </summary>
    public delegate Task<(byte[] Daten, long Gesamtlaenge)> INetzbereich(
        string adresse, long von, long laenge, CancellationToken abbruch);

    /// <summary>
    /// Woher die Koordinaten zu einem Ortsnamen kommen (Nominatim bzw. eine Probe).
    /// </summary>
    public delegate Task<(bool Success, double Lat, double Lon, string DisplayName)>
        IOrtsQuelle(string ortsname);

    /// <summary>
    /// Der Klimaimport als ABLAUF (iU9-W14c.0e) — Geokodierung, Abruf, Rechnung,
    /// EINE Transaktion.
    ///
    /// <para><b>Warum es das gibt.</b> Er stand als 177-Zeilen-Handler
    /// (<c>Form_Klimadaten.btn_Import_Click</c>) in der Oberfläche: Netzabruf,
    /// Sonnenstandsrechnung und drei Schreibschritte in einer Transaktion — alles
    /// Fachweg, nichts davon Anzeige. Muster ist
    /// <see cref="GanglinienImportAblauf"/> aus W12.0d.</para>
    ///
    /// <para><b>EIN PVGIS-Abruf statt VIER</b> (Befund W14c-B28, A-10): Der Vorläufer
    /// startete vier Abrufe (Süd 0°, Ost −90°, West 90°, Nord 180°) gleichzeitig,
    /// wartete auf alle vier und verwertete <b>nur den Süd-Abruf</b>; der Kommentar
    /// nannte das ausdrücklich. Die vier Fassadenwerte rechnet
    /// <see cref="SolarCalculator.CalculateHourly"/> ohnehin selbst aus DERSELBEN
    /// Stundenreihe — die drei anderen Abrufe waren umsonst. <b>Kein gespeichertes
    /// Byte ändert sich dadurch.</b></para>
    ///
    /// <para><b>Der Sonnenwinkel kommt als RÜCKGABEWERT</b> (Befund W14c-B29): Der
    /// Vorläufer las ihn aus dem statischen Feld
    /// <c>SolarCalculator.sonnenwinkel</c>, das die Schleife je Stunde neu setzte —
    /// ein Rückgabekanal einer Methode, die schon einen Rückgabewert hat. Hier steht
    /// die Zuweisung unmittelbar nach dem Süd-Aufruf, in derselben Reihenfolge, in der
    /// sie der Vorläufer las.</para>
    ///
    /// <para><b>Die Tageswerte tragen den <c>Listbezeichner</c></b> (Befund W14c-B31,
    /// A-11): Schritt E schrieb sie mit <c>comboBox_Ort.Text</c> — im
    /// Handeingabe-Zweig war das Feld leer.</para>
    ///
    /// <para><b>Die Dublettenprüfung fragt die DATENBANK</b> (Befund W14c-B26, A-9):
    /// Der Vorläufer prüfte mit <c>listBoxKlimreg.FindString(ort)</c> — einer
    /// Präfixsuche in der ANZEIGE, die „Berlin" auch auf „Berlin_2024" treffen liess —
    /// und kehrte dann STILL zurück. Für die Handeingabe gab es gar keine Prüfung.</para>
    /// </summary>
    public static class KlimaImportAblauf
    {
        /// <summary>Die Ausrichtung, mit der PVGIS abgerufen wird (Süd).</summary>
        public const int AZIMUT_SUED = 0;

        /// <summary>Neigung der gerechneten Fassaden — 90° (senkrecht).</summary>
        private const int NEIGUNG_FASSADE = 90;

        /// <summary>Die vier Fassadenrichtungen: Süd, Ost, Nord, West.</summary>
        private const int AZ_SUED = 0, AZ_OST = -90, AZ_NORD = 180, AZ_WEST = 90;

        /// <summary>Die sieben Schritte des Ablaufs — der Balken des Vorläufers zählte bis 7.</summary>
        private const int SCHRITTE = 7;

        /// <summary>
        /// Führt den Import aus. <b>Er zeigt nie etwas an</b> — Fortschritt geht über
        /// <paramref name="melder"/>, das Ergebnis über die Rückgabe.
        /// </summary>
        /// <param name="auftrag">Was importiert werden soll.</param>
        /// <param name="tmy">Die TMY-Quelle; ohne sie bricht der Ablauf ab.</param>
        /// <param name="orte">Die Ortsauflösung; nur für <c>AusOrtsname</c> nötig.</param>
        /// <param name="melder">Fortschritt der sieben Schritte.</param>
        /// <param name="abbruch">Abbruchmarke (A-4) — der Vorläufer hatte keine.</param>
        /// <param name="bereich">Der Bereichsabruf der TRY-Regionaldaten (KL1-B).
        /// <b>Als LETZTER Parameter mit Vorgabe</b>, damit jeder bestehende Aufruf
        /// unverändert gültig bleibt; ohne ihn steht nur der Weg über eine lokale
        /// <c>data.zip</c> offen.</param>
        public static async Task<KlimaImportErgebnis> Laufen(
            KlimaImportAuftrag auftrag,
            ITmyQuelle tmy,
            IOrtsQuelle orte = null,
            IProgress<ImportFortschritt> melder = null,
            CancellationToken abbruch = default,
            INetzbereich bereich = null)
        {
            var erg = new KlimaImportErgebnis();
            if (auftrag == null) return Abbruch(erg);
            if (tmy == null && auftrag.Quelle == KlimaQuelle.PvgisTmy) return Abbruch(erg);

            // ---- Schritt 1: Koordinaten -------------------------------------
            Melden(melder, 1, "KLIMA_SCHRITT_KOORDINATEN");

            double lon, lat;
            string details;
            string bezeichner;

            if (auftrag.Art == KlimaImportArt.AusOrtsname)
            {
                string ort = (auftrag.Ortsname ?? "").Trim();
                if (ort.Length == 0 || orte == null) return Fehler(erg,
                    KlimaImportAusgang.Eingabefehler, MyResource.Resource.KLIMA_MSG_EINGABEN_PRUEFEN);

                var antwort = await orte(ort).ConfigureAwait(false);
                if (!antwort.Success)
                    return Fehler(erg, KlimaImportAusgang.OrtUnbekannt,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_ORT_UNBEKANNT,
                                      ort, antwort.DisplayName ?? ""));

                lat = antwort.Lat;
                lon = antwort.Lon;
                details = antwort.DisplayName ?? "";
                bezeichner = ort;
            }
            else
            {
                bezeichner = (auftrag.Bezeichnung ?? "").Trim();
                if (bezeichner.Length == 0) return Fehler(erg,
                    KlimaImportAusgang.Eingabefehler, MyResource.Resource.KLIMA_MSG_EINGABEN_PRUEFEN);

                lon = auftrag.Longitude;
                lat = auftrag.Latitude;
                details = "";
            }

            // A-9 (Befund W14c-B26): Die Dublettenpruefung fragt die DATENBANK und
            // MELDET, statt still zurueckzukehren.
            if (new KlimaregionStammCtrl().GetStammId(bezeichner) > 0)
                return Fehler(erg, KlimaImportAusgang.Dublette,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_MSG_SCHON_VORHANDEN, bezeichner));

            if (abbruch.IsCancellationRequested) return Abbruch(erg);

            // ---- Schritt 2: die Stundenwerte (eine der DREI Quellen) --------
            List<TmyHourlyData> stunden;
            string herkunft = "";          // der Herkunftsvermerk der TRY-Quellen

            // Schemaschritt 97 (Auftrag KL-6): WELCHES Wetterjahr die Reihe
            // beschreibt. Leer bzw. null heisst "sagt nichts dazu" - so bleibt es
            // bei PVGIS, das keine TRY-Szenarien kennt.
            string szenario = "";
            int? bezugsjahr = null;

            if (auftrag.Quelle == KlimaQuelle.PvgisTmy)
            {
                // ---- PVGIS: EIN Abruf (A-10), unveraendert ------------------
                Melden(melder, 2, "KLIMA_SCHRITT_ABRUF");

                try
                {
                    stunden = await tmy(lon, lat, AZIMUT_SUED).ConfigureAwait(false)
                              ?? new List<TmyHourlyData>();
                }
                catch (ArgumentException ex)
                {
                    return Fehler(erg, KlimaImportAusgang.Netzfehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_PVGIS_EINGABE, ex.Message));
                }
                catch (Exception ex)
                {
                    return Fehler(erg, KlimaImportAusgang.Netzfehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_DOWNLOAD_FEHLER, ex.Message));
                }
            }
            else if (auftrag.Quelle == KlimaQuelle.TryDatei)
            {
                // ---- DWD-TRY-Datei: KEIN Netz ------------------------------
                Melden(melder, 2, "KLIMA_SCHRITT_DATEI");

                string pfad = (auftrag.TryPfad ?? "").Trim();
                if (pfad.Length == 0)
                    return Fehler(erg, KlimaImportAusgang.Eingabefehler,
                        MyResource.Resource.KLIMA_MSG_EINGABEN_PRUEFEN);

                TryKopf kopf;
                try
                {
                    stunden = DwdTryLeser.LesenDatei(pfad, out kopf);
                }
                catch (FileNotFoundException)
                {
                    return Fehler(erg, KlimaImportAusgang.Eingabefehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_TRY_DATEI_FEHLT, pfad));
                }
                catch (FormatException ex)
                {
                    return Fehler(erg, KlimaImportAusgang.Dateifehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_TRY_FORMATFEHLER, ex.Message));
                }
                catch (Exception ex)
                {
                    return Fehler(erg, KlimaImportAusgang.Dateifehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_TRY_FORMATFEHLER, ex.Message));
                }

                // ---- der STANDORT (Auftrag KL-2) ---------------------------
                // Der Auftrag hat Vorrang: Was der Anwender eingetragen oder ueber
                // den Ortsnamen geholt hat, bleibt stehen. Erst wenn er KEINE
                // Koordinaten mitbringt, kommt der Standort aus dem Dateikopf
                // (Lambert -> Laenge/Breite). Beides wird BENANNT vermerkt.
                bool ausKopf = false;
                if (auftrag.Art == KlimaImportArt.AusKoordinaten && lon == 0 && lat == 0)
                {
                    if (!kopf.StandortBekannt)
                        return Fehler(erg, KlimaImportAusgang.Eingabefehler,
                            MyResource.Resource.KLIMA_TRY_STANDORT_FEHLT);

                    lon = kopf.Laenge!.Value;
                    lat = kopf.Breite!.Value;
                    ausKopf = true;
                }

                string standort = ausKopf
                    ? string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_STANDORT_KOPF,
                        kopf.Rechtswert, kopf.Hochwert,
                        lon.ToString("F4", CultureInfo.CurrentCulture),
                        lat.ToString("F4", CultureInfo.CurrentCulture))
                    : MyResource.Resource.KLIMA_TRY_STANDORT_ANWENDER;

                herkunft = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_DETAILS_DATEI,
                    Path.GetFileName(pfad),
                    DateTime.Now.ToString("d", CultureInfo.CurrentCulture),
                    MyResource.Resource.KLIMA_TRY_LIZENZ,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_VERWORFEN, kopf.VerworfenText))
                    + " · " + standort;

                // Schemaschritt 97: Das SZENARIO steht im Dateikopf ("Art des TRY"),
                // das Bezugsjahr NICHT - der Kopf nennt einen Bezugszeitraum
                // ("1995-2012"), und daraus ein Bezugsjahr dieses Hauses zu machen
                // waere eine Behauptung. Also Szenario, wenn der Kopf es hergibt,
                // und sonst nichts.
                szenario = SzenarioschluesselAusKopf(kopf.Art);

                DwdTryLeser.DirektNormal(stunden, lon, lat);
            }
            else
            {
                // ---- TRY-Regionaldaten: Bereichsabruf ODER lokale data.zip --
                Melden(melder, 2, "KLIMA_SCHRITT_PAKET");

                string paket = (auftrag.TryPaketPfad ?? "").Trim();
                string quelle;
                TryPaketErgebnis paketErg;

                try
                {
                    if (paket.Length > 0)
                    {
                        quelle = Path.GetFileName(paket);
                        paketErg = TryPaketLeser.LesenAusDatei(
                            paket, lon, lat, auftrag.TryJahr, auftrag.Szenario);
                    }
                    else
                    {
                        if (bereich == null)
                            return Fehler(erg, KlimaImportAusgang.Eingabefehler,
                                MyResource.Resource.KLIMA_TRY_KEIN_BEREICH);

                        quelle = Adresse();
                        paketErg = await TryPaketLeser.LesenAusNetzAsync(
                            bereich, quelle, lon, lat, auftrag.TryJahr, auftrag.Szenario,
                            abbruch).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { return Abbruch(erg); }
                catch (FileNotFoundException)
                {
                    return Fehler(erg, KlimaImportAusgang.Eingabefehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_TRY_DATEI_FEHLT, paket));
                }
                catch (FormatException ex)
                {
                    return Fehler(erg, KlimaImportAusgang.Dateifehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_TRY_FORMATFEHLER, ex.Message));
                }
                catch (InvalidOperationException ex)
                {
                    // Keine Teilabrufe, keine Region, kein Szenario - alles BENANNT.
                    return Fehler(erg, KlimaImportAusgang.Dateifehler, ex.Message);
                }
                catch (Exception ex)
                {
                    return Fehler(erg, KlimaImportAusgang.Netzfehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_DOWNLOAD_FEHLER, ex.Message));
                }

                stunden = paketErg.Stunden;

                // Die Region wird mit NAMEN genannt (Auftrag KL-3): "14 Stoetten"
                // statt "14" - die Nummer allein sagt einem Anwender nichts. Die
                // ZUORDNUNG bleibt der naechste Stationsmittelpunkt aus dem Paket.
                herkunft = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_DETAILS_REGIONAL,
                    paketErg.Region.Bezeichnung,
                    paketErg.Region.Koordinatentext,
                    paketErg.Region.EntfernungKm.ToString("F0", CultureInfo.CurrentCulture),
                    SzenarioText(auftrag.Szenario),
                    auftrag.TryJahr.ToString(CultureInfo.CurrentCulture),
                    quelle,
                    DateTime.Now.ToString("d", CultureInfo.CurrentCulture),
                    MyResource.Resource.KLIMA_TRY_LIZENZ,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_VERWORFEN,
                                  paketErg.Kopf.VerworfenText));

                // Schemaschritt 97: Bei den Regionaldaten steht beides im AUFTRAG -
                // der Anwender hat Jahr und Szenario gewaehlt, und genau danach ist
                // das Paket gelesen worden.
                szenario = Szenarioschluessel(auftrag.Szenario);
                bezugsjahr = auftrag.TryJahr;

                DwdTryLeser.DirektNormal(stunden, lon, lat);
            }

            if (herkunft.Length > 0)
                details = details.Length > 0 ? details + " · " + herkunft : herkunft;

            if (abbruch.IsCancellationRequested) return Abbruch(erg);

            // ---- Schritt 3: die Sonnenstaende ------------------------------
            Melden(melder, 3, "KLIMA_SCHRITT_RECHNEN");

            try
            {
                Rechnen(stunden, lon, lat, abbruch);
            }
            catch (OperationCanceledException) { return Abbruch(erg); }

            if (abbruch.IsCancellationRequested) return Abbruch(erg);

            // ---- Schritte 4 bis 6: EINE Transaktion ------------------------
            var ctrl = new KlimaregionStammCtrl();
            var repo = new AccessRepository();
            List<TmyHourlyData> tage;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    Melden(melder, 4, "KLIMA_SCHRITT_REGION");
                    if (!ctrl.Add(bezeichner, lon, lat, details,
                                  Quellenschluessel(auftrag.Quelle), Heute(),
                                  szenario, bezugsjahr, v))
                    {
                        v.Rollback();
                        return Fehler(erg, KlimaImportAusgang.Schreibfehler, "");
                    }

                    object gefunden = v.Skalar(
                        "SELECT ID_Klimaregion FROM Tab_Klimaregion_STAMM WHERE Name = ?",
                        new DbParam("@name", bezeichner));
                    int id = gefunden != null && gefunden != DBNull.Value
                        ? Convert.ToInt32(gefunden, CultureInfo.InvariantCulture) : 0;
                    if (id == 0) throw new Exception(MyResource.Resource.KLIMA_MSG_ID_FEHLT);

                    Melden(melder, 5, "KLIMA_SCHRITT_STUNDEN");
                    repo.SaveTmyData(stunden, bezeichner, "Tab_Solar_STAMM", id, v);

                    Melden(melder, 6, "KLIMA_SCHRITT_TAGE");
                    tage = SolarCalculator.GetDailyAverages(stunden);
                    Tagtypen(tage);

                    // A-11 (Befund W14c-B31): der Listbezeichner, nicht comboBox_Ort.Text.
                    repo.SaveTmyData(tage, bezeichner, "Tab_Klimadaten_STAMM", id, v);

                    v.Commit();
                    erg.Id = id;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { /* der Rollback darf nicht scheitern */ }
                    return Fehler(erg, KlimaImportAusgang.Schreibfehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_IMPORT_FEHLER, ex.Message));
                }
            }

            // ---- Schritt 7: fertig -----------------------------------------
            Melden(melder, SCHRITTE, "KLIMA_SCHRITT_FERTIG");

            erg.Ausgang = KlimaImportAusgang.Erfolg;
            erg.Bezeichner = bezeichner;
            erg.Stundenwerte = stunden.Count;
            erg.Tageswerte = tage.Count;
            erg.Meldung = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KLIMA_MSG_IMPORT_FERTIG, bezeichner,
                stunden.Count.ToString(CultureInfo.CurrentCulture),
                tage.Count.ToString(CultureInfo.CurrentCulture));

            // Die HERKUNFT steht in der Meldung UND in Details (Entscheid des Anwenders:
            // Lizenzvermerk, Region und die verworfenen Groessen bleiben sichtbar).
            if (herkunft.Length > 0)
                erg.Meldung += " " + string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_MSG_HERKUNFT, herkunft);

            return erg;
        }

        // =====================================================================
        //  Die REGIONSVORSCHAU (Auftrag KL-3, Anwenderentscheid 19.09.2026 b)
        // =====================================================================

        /// <summary>
        /// Sagt VOR dem Import, welche TRY-Region der eingetragene Standort trifft.
        ///
        /// <para><b>Wozu.</b> Die Regionaldaten ordnen einen Ort der nächstgelegenen
        /// Repräsentanzstation zu. Welche das ist, stand bisher erst NACH dem Einlesen
        /// im Herkunftsvermerk — wer 8 760 Stunden einliest, um zu erfahren, dass es
        /// die falsche Region war, hat sie umsonst eingelesen.</para>
        ///
        /// <para><b>Ohne einen Eintrag zu entpacken.</b> Gelesen wird allein das
        /// Zentralverzeichnis des Pakets (<see cref="TryPaketLeser.RegionErmitteln"/>);
        /// die Stundenreihe bleibt liegen. Der spätere Import liest das Paket ein
        /// ZWEITES Mal — ein zweiter Bereichsabruf über einige hundert Kilobyte, vom
        /// Anwender angenommen. Der Lauf nimmt dieselbe Regionswahl, weil beide
        /// dieselbe Funktion rufen.</para>
        ///
        /// <para><b>Die Geokodierung läuft wie im Import</b>: Ein Ortsname wird über
        /// <paramref name="orte"/> aufgelöst, Koordinaten gelten unverändert.</para>
        /// </summary>
        /// <param name="auftrag">Standort, Bezugsjahr und — wenn vorhanden — der Pfad
        /// einer lokalen <c>data.zip</c>.</param>
        /// <param name="orte">Die Ortsauflösung; nur für <c>AusOrtsname</c> nötig.</param>
        /// <param name="abbruch">Abbruchmarke.</param>
        /// <param name="bereich">Der Bereichsabruf; ohne ihn steht nur der Weg über
        /// eine lokale <c>data.zip</c> offen.</param>
        public static async Task<KlimaVorschauErgebnis> RegionErmittelnAsync(
            KlimaImportAuftrag auftrag,
            IOrtsQuelle orte = null,
            CancellationToken abbruch = default,
            INetzbereich bereich = null)
        {
            var erg = new KlimaVorschauErgebnis();
            if (auftrag == null) return VorschauFehler(erg, KlimaImportAusgang.Abgebrochen, "");

            double lon, lat;

            if (auftrag.Art == KlimaImportArt.AusOrtsname)
            {
                string ort = (auftrag.Ortsname ?? "").Trim();
                if (ort.Length == 0 || orte == null)
                    return VorschauFehler(erg, KlimaImportAusgang.Eingabefehler,
                        MyResource.Resource.KLIMA_MSG_EINGABEN_PRUEFEN);

                var antwort = await orte(ort).ConfigureAwait(false);
                if (!antwort.Success)
                    return VorschauFehler(erg, KlimaImportAusgang.OrtUnbekannt,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_ORT_UNBEKANNT,
                                      ort, antwort.DisplayName ?? ""));

                lon = antwort.Lon;
                lat = antwort.Lat;
            }
            else
            {
                lon = auftrag.Longitude;
                lat = auftrag.Latitude;
            }

            if (abbruch.IsCancellationRequested)
                return VorschauFehler(erg, KlimaImportAusgang.Abgebrochen,
                                      MyResource.Resource.KLIMA_MSG_ABGEBROCHEN);

            string paket = (auftrag.TryPaketPfad ?? "").Trim();
            TryRegionVorschau vorschau;

            try
            {
                if (paket.Length > 0)
                {
                    vorschau = TryPaketLeser.RegionErmittelnAusDatei(paket, lon, lat, auftrag.TryJahr);
                }
                else
                {
                    if (bereich == null)
                        return VorschauFehler(erg, KlimaImportAusgang.Eingabefehler,
                            MyResource.Resource.KLIMA_TRY_KEIN_BEREICH);

                    vorschau = await TryPaketLeser.RegionErmittelnAusNetzAsync(
                        bereich, Adresse(), lon, lat, auftrag.TryJahr, abbruch).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return VorschauFehler(erg, KlimaImportAusgang.Abgebrochen,
                                      MyResource.Resource.KLIMA_MSG_ABGEBROCHEN);
            }
            catch (FileNotFoundException)
            {
                return VorschauFehler(erg, KlimaImportAusgang.Eingabefehler,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_DATEI_FEHLT, paket));
            }
            catch (InvalidOperationException ex)
            {
                // Keine Teilabrufe, keine Region fuer das Jahr, ausserhalb 300 km -
                // alles BENANNT, mit dem Text des Kerns.
                return VorschauFehler(erg, KlimaImportAusgang.Dateifehler, ex.Message);
            }
            catch (FormatException ex)
            {
                return VorschauFehler(erg, KlimaImportAusgang.Dateifehler,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_FORMATFEHLER, ex.Message));
            }
            catch (Exception ex)
            {
                return VorschauFehler(erg, KlimaImportAusgang.Netzfehler,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_MSG_DOWNLOAD_FEHLER, ex.Message));
            }

            erg.Ausgang = KlimaImportAusgang.Erfolg;
            erg.Region = vorschau.Region;
            erg.Ausfuehrungen = vorschau.Ausfuehrungen;
            erg.Meldung = Vorschauzeile(vorschau);
            return erg;
        }

        /// <summary>
        /// Die Ergebniszeile der Vorschau: „Region 14 Stötten, Station 48,6600 / 9,8600,
        /// Entfernung 12 km" — und daran, was das Paket für diese Region führt.
        /// </summary>
        public static string Vorschauzeile(TryRegionVorschau vorschau)
        {
            if (vorschau == null || vorschau.Region == null) return "";

            string zeile = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KLIMA_TRY_VORSCHAU,
                vorschau.Region.Bezeichnung,
                vorschau.Region.Koordinatentext,
                vorschau.Region.EntfernungKm.ToString("F0", CultureInfo.CurrentCulture));

            string bestand = Bestandstext(vorschau.Ausfuehrungen);
            if (bestand.Length > 0)
                zeile += " · " + string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_VORSCHAU_BESTAND, bestand);

            return zeile;
        }

        /// <summary>Jahr und Szenario jeder Ausführung, z. B. „2015 mittleres Jahr,
        /// 2015 sommerwarm, …"; leer, wenn das Paket keine führt.</summary>
        private static string Bestandstext(IReadOnlyList<TryAusfuehrung> ausfuehrungen)
        {
            if (ausfuehrungen == null || ausfuehrungen.Count == 0) return "";

            var teile = new List<string>(ausfuehrungen.Count);
            foreach (TryAusfuehrung a in ausfuehrungen)
                teile.Add(a.Jahr.ToString(CultureInfo.CurrentCulture) + " " + SzenarioText(a.Szenario));

            return string.Join(", ", teile);
        }

        private static KlimaVorschauErgebnis VorschauFehler(KlimaVorschauErgebnis erg,
                                                            KlimaImportAusgang ausgang, string meldung)
        {
            erg.Ausgang = ausgang;
            erg.Meldung = meldung ?? "";
            return erg;
        }

        /// <summary>
        /// Der sprachneutrale Schlüssel der Quelle für
        /// <c>Tab_Klimaregion(_STAMM).Quelle</c> (Schemaschritt 95, Auftrag KL-3).
        ///
        /// <para><b>Nie ein Anzeigetext.</b> In der Spalte steht, WOHER die Reihe
        /// stammt; wie das heißt, entscheidet die Sprache der Oberfläche — und die darf
        /// den gespeicherten Wert nicht ändern (Drei-Schichten-Regel).</para>
        /// </summary>
        public static string Quellenschluessel(KlimaQuelle quelle)
        {
            switch (quelle)
            {
                case KlimaQuelle.TryDatei: return DbWerte.KLIMA_QUELLE_TRY_DATEI;
                case KlimaQuelle.TryRegional: return DbWerte.KLIMA_QUELLE_TRY_REGIONAL;
                default: return DbWerte.KLIMA_QUELLE_PVGIS;
            }
        }

        /// <summary>
        /// Der sprachneutrale Schlüssel des Szenarios für
        /// <c>Tab_Klimaregion(_STAMM).Szenario</c> (Schemaschritt 97, Auftrag KL-6) —
        /// die eins-zu-eins-Abbildung von <see cref="TrySzenario"/>.
        ///
        /// <para><b>Nie ein Anzeigetext</b> (Drei-Schichten-Regel); den liefert
        /// <see cref="SzenarioText"/>.</para>
        /// </summary>
        public static string Szenarioschluessel(TrySzenario szenario)
        {
            switch (szenario)
            {
                case TrySzenario.Sommerwarm: return DbWerte.KLIMA_SZENARIO_SOMMERWARM;
                case TrySzenario.Winterkalt: return DbWerte.KLIMA_SZENARIO_WINTERKALT;
                default: return DbWerte.KLIMA_SZENARIO_MITTEL;
            }
        }

        /// <summary>
        /// Das Szenario aus der Kopfzeile „Art des TRY" einer DWD-Datei —
        /// <c>""</c>, wenn der Kopf nichts nennt oder etwas Unbekanntes nennt
        /// (Schemaschritt 97, Auftrag KL-6).
        ///
        /// <para><b>Warum hier geraten werden DARF.</b> „Art des TRY" IST das
        /// Szenario; der Kopf sagt es mit eigenen Worten („mittleres Jahr", „Sommer
        /// warm", „Winter kalt"). Gemessen wird deshalb gegen die tragenden Wortteile
        /// und ohne Rücksicht auf Groß-/Kleinschreibung, Bindestriche und
        /// Zwischenräume — und was danach nicht zuzuordnen ist, bleibt LEER statt
        /// „mittleres Jahr" zu heißen: Eine Vorgabe wäre hier eine Behauptung über
        /// die Datei.</para>
        ///
        /// <para><b>Das Bezugsjahr steht NICHT im Kopf.</b> Er nennt einen
        /// Bezugszeitraum („1995-2012", „2031-2060"); das Bezugsjahr dieses Hauses
        /// (2015 bzw. 2045) ist etwas anderes und bleibt bei dieser Quelle NULL.</para>
        /// </summary>
        public static string SzenarioschluesselAusKopf(string art)
        {
            string a = (art ?? "").Trim();
            if (a.Length == 0) return "";

            a = a.Replace("-", "").Replace("\u2011", "").Replace(" ", "").ToUpperInvariant();

            if (a.Contains("SOMMER") && a.Contains("WARM")) return DbWerte.KLIMA_SZENARIO_SOMMERWARM;
            if (a.Contains("WINTER") && a.Contains("KALT")) return DbWerte.KLIMA_SZENARIO_WINTERKALT;
            if (a.Contains("MITTL") || a.Contains("NORMAL")) return DbWerte.KLIMA_SZENARIO_MITTEL;

            return "";
        }

        /// <summary>
        /// Der heutige Tag als ISO-Text <c>yyyy-MM-dd</c> — das Format der Spalte
        /// <c>Importdatum</c>: sortierbar und kulturunabhängig. Der Herkunftsvermerk in
        /// <c>Details</c> nennt daneben weiterhin das Datum in der Landesschreibweise;
        /// er ist Text für den Leser, diese Spalte eine Angabe für das Programm.
        /// </summary>
        private static string Heute()
        {
            return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die Adresse des TRY-Regionalpakets: der Einstellwert, sonst die Vorgabe.
        /// Der Kern liest ihn über <c>Dienste.Einstellungen</c> — er kennt die Ablage nicht.
        /// </summary>
        public static string Adresse()
        {
            string wert = "";
            try { wert = Dienste.Einstellungen.Lies(TryPaketLeser.SCHLUESSEL_ADRESSE) ?? ""; }
            catch { wert = ""; }

            return string.IsNullOrWhiteSpace(wert) ? TryPaketLeser.ADRESSE_VORGABE : wert.Trim();
        }

        /// <summary>Der Anzeigetext eines Szenarios.</summary>
        public static string SzenarioText(TrySzenario szenario)
        {
            switch (szenario)
            {
                case TrySzenario.Sommerwarm: return MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM;
                case TrySzenario.Winterkalt: return MyResource.Resource.KLIMA_TRY_SZ_WINTERKALT;
                default: return MyResource.Resource.KLIMA_TRY_SZ_MITTEL;
            }
        }

        /// <summary>
        /// Die vier Fassadenwerte und der Sonnenwinkel je Stunde — wörtlich in der
        /// Reihenfolge des Vorläufers: Süd, Ost, Nord, West, danach der Winkel.
        ///
        /// <para><b>Öffentlich, weil die Probe genau das nachrechnet</b>: Sie ist die
        /// einzige Fachrechnung des Imports.</para>
        /// </summary>
        public static void Rechnen(List<TmyHourlyData> stunden, double lon, double lat,
                                   CancellationToken abbruch = default)
        {
            if (stunden == null) return;

            for (int i = 0; i < stunden.Count; i++)
            {
                abbruch.ThrowIfCancellationRequested();

                TmyHourlyData s = stunden[i];
                double ghi = s.GlobalIrradiance, dni = s.DirectIrradiance;
                double dhi = s.DiffuseIrradiance, t2m = s.Temperature;

                DateTime dt = DateTime.ParseExact(s.TimeString, "yyyyMMdd:HHmm",
                                                  CultureInfo.InvariantCulture);

                s.Sol_sued = SolarCalculator.CalculateHourly(lon, lat, NEIGUNG_FASSADE, AZ_SUED,
                    ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);

                // Befund W14c-B29: Der Winkel ist eine Eigenschaft der SONNENSTELLUNG,
                // nicht der Fassade - er wird nach dem ERSTEN Aufruf gelesen, wie im
                // Vorlaeufer nach dem letzten (alle vier setzen denselben Wert).
                double winkel = SolarCalculator.sonnenwinkel;

                s.Sol_ost = SolarCalculator.CalculateHourly(lon, lat, NEIGUNG_FASSADE, AZ_OST,
                    ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);
                s.Sol_nord = SolarCalculator.CalculateHourly(lon, lat, NEIGUNG_FASSADE, AZ_NORD,
                    ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);
                s.Sol_west = SolarCalculator.CalculateHourly(lon, lat, NEIGUNG_FASSADE, AZ_WEST,
                    ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);

                s.Sonnenwinkel = winkel;
            }
        }

        /// <summary>
        /// Die drei Tagtypen je Tageswert — wörtlich: Wochenende = Sa/So,
        /// <c>TagTyp_W</c> = 2, wenn der Diffusanteil über der Hälfte der
        /// Globalstrahlung liegt, sonst 1; <c>TagTyp_NW</c> aus Quartal und Wochenende.
        /// </summary>
        public static void Tagtypen(List<TmyHourlyData> tage)
        {
            if (tage == null) return;

            foreach (TmyHourlyData t in tage)
            {
                DateTime datum = DateTime.ParseExact(t.TimeString, "dd.MM.yyyy",
                                                     CultureInfo.InvariantCulture);
                t.TagTyp_NW = Jahreszeitwert(datum);
                t.WE = datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday;
                t.TagTyp_W = t.DiffuseIrradiance > 0.5 * t.GlobalIrradiance ? 2 : 1;
            }
        }

        /// <summary>
        /// Quartal × Wochenende → 1…8 (<c>GetSeasonalValue</c> des Vorläufers).
        /// Q1 Werktag 1, Q1 Wochenende 2, Q2 3/4, Q3 5/6, Q4 7/8.
        /// </summary>
        public static int Jahreszeitwert(DateTime datum)
        {
            bool we = datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday;
            int quartal = (datum.Month - 1) / 3 + 1;

            switch (quartal)
            {
                case 1: return we ? 2 : 1;
                case 2: return we ? 4 : 3;
                case 3: return we ? 6 : 5;
                case 4: return we ? 8 : 7;
            }
            return 0;
        }

        private static void Melden(IProgress<ImportFortschritt> melder, int schritt, string schluessel)
        {
            melder?.Report(new ImportFortschritt((double)schritt / SCHRITTE, schluessel));
        }

        private static KlimaImportErgebnis Abbruch(KlimaImportErgebnis erg)
        {
            erg.Ausgang = KlimaImportAusgang.Abgebrochen;
            erg.Meldung = MyResource.Resource.KLIMA_MSG_ABGEBROCHEN;
            return erg;
        }

        private static KlimaImportErgebnis Fehler(KlimaImportErgebnis erg,
                                                  KlimaImportAusgang ausgang, string meldung)
        {
            erg.Ausgang = ausgang;
            erg.Meldung = meldung ?? "";
            return erg;
        }
    }
}
