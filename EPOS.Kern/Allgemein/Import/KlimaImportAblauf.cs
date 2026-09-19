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

                herkunft = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_DETAILS_DATEI,
                    Path.GetFileName(pfad),
                    DateTime.Now.ToString("d", CultureInfo.CurrentCulture),
                    MyResource.Resource.KLIMA_TRY_LIZENZ,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_VERWORFEN, kopf.VerworfenText));

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

                herkunft = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_DETAILS_REGIONAL,
                    paketErg.Region.Nummer.ToString(CultureInfo.CurrentCulture),
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
                    if (!ctrl.Add(bezeichner, lon, lat, details, v))
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
