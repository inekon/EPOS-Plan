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
    /// Was die VORSCHAU zurückgibt (Auftrag KL-3, erweitert in KD-2 nach Entscheid
    /// KD-Q3): welchen Datensatz der eingetragene Standort trifft — <b>gelesen</b>, bevor
    /// eingelesen wird, für alle drei Quellen. Der Import übernimmt den gelesenen Satz
    /// ohne zweiten Abruf und ohne zweite Geokodierung, solange
    /// <see cref="PasstZu"/> gilt.
    /// </summary>
    public sealed class KlimaVorschauErgebnis
    {
        /// <summary>Wie die Vorschau ausgegangen ist; die Ausgänge des Imports.</summary>
        public KlimaImportAusgang Ausgang = KlimaImportAusgang.Abgebrochen;

        /// <summary>Die fertige Ergebnis- oder Fehlerzeile; leer heißt: nichts zu melden.</summary>
        public string Meldung = "";

        /// <summary>Die getroffene TRY-Region (nur Regionalweg); sonst <c>null</c>.</summary>
        public TryRegion Region;

        /// <summary>Was das Paket für diese Region führt — Jahr und Szenario (nur Regionalweg).</summary>
        public IReadOnlyList<TryAusfuehrung> Ausfuehrungen = new List<TryAusfuehrung>();

        /// <summary>Die Quelle, aus der gelesen wurde.</summary>
        public KlimaQuelle Quelle;

        /// <summary>Bezugsjahr der gewählten Ausführung (Regionalweg) bzw. aus dem Dateikopf; sonst <c>null</c>.</summary>
        public int? Bezugsjahr;

        /// <summary>Szenario der gewählten Ausführung (nur Regionalweg).</summary>
        public TrySzenario Szenario;

        /// <summary>
        /// Gibt es die GEWÄHLTE Ausführung? Im Regionalweg: Jahr und Szenario im Paket
        /// für die getroffene Region. Bei Datei und PVGIS: Der Satz trägt Stunden.
        /// Nein heißt: <see cref="Grund"/> sagt warum, im Regionalweg stehen die
        /// vorhandenen Ausführungen in <see cref="Ausfuehrungen"/>.
        /// </summary>
        public bool AusfuehrungGefunden;

        /// <summary>Der benannte Grund, wenn <see cref="AusfuehrungGefunden"/> nein ist.</summary>
        public string Grund = "";

        /// <summary>Der gesuchte Standort: Koordinate, Anzeigename der Geokodierung, Höhe.</summary>
        public KlimaStandort Standort;

        /// <summary>Der Bezugspunkt des Datensatzes: Station, Dateistandort oder PVGIS-Punkt.</summary>
        public KlimaBezugspunkt Bezugspunkt;

        /// <summary>Höhe des Bezugspunkts minus Höhe des Standorts [m]; <c>null</c>, wenn eine fehlt.</summary>
        public double? HoehenunterschiedM;

        /// <summary>Die Kennzahlen des gelesenen Satzes; <c>null</c> ohne Stunden.</summary>
        public KlimaKennzahlen Kennzahlen;

        /// <summary>Welche Größen die Quelle führt.</summary>
        public IReadOnlyList<KlimaParameterEintrag> Parameterbestand = new List<KlimaParameterEintrag>();

        /// <summary>Regionalweg: „nächste Repräsentanzstation, nicht die amtliche Klimaregion" (KD-Q1).</summary>
        public bool Repraesentanzhinweis;

        /// <summary>
        /// Bezugspunkt nicht ortstypisch — ab <see cref="KlimaVorschauFestwerte.HINWEIS_ENTFERNUNG_KM"/>
        /// oder <see cref="KlimaVorschauFestwerte.HINWEIS_HOEHE_M"/> (KD-Q1, KD-Q9).
        /// </summary>
        public bool Ortshinweis;

        /// <summary>Die Hinweistexte in Anzeigereihenfolge (leise Zeilen, kein Banner).</summary>
        public IReadOnlyList<string> Hinweise = new List<string>();

        /// <summary>Der gelesene Satz für den Import — nie in der Oberfläche.</summary>
        internal KlimaImportAblauf.Lesesatz Satz;

        /// <summary>Der Schlüssel der Eingaben, aus denen die Vorschau gelesen hat.</summary>
        internal string Schluessel = "";

        public bool Erfolgreich => Ausgang == KlimaImportAusgang.Erfolg;

        /// <summary>Kann ein Import diesen Satz übernehmen?</summary>
        public bool Importierbar => Erfolgreich && AusfuehrungGefunden && Satz != null &&
                                    Satz.Stunden != null && Satz.Stunden.Count > 0;

        /// <summary>
        /// Gehört die Vorschau zu diesem Auftrag? Nein, sobald Ort, Koordinaten, Quelle,
        /// Datei, Jahr oder Szenario anders sind — dann ist sie VERFALLEN, und der Import
        /// liest selbst. Koordinaten von Hand passen auch, wenn sie genau den
        /// geokodierten Standort der Vorschau tragen (die Felder sind damit befüllt).
        /// </summary>
        public bool PasstZu(KlimaImportAuftrag auftrag)
        {
            if (auftrag == null || !Erfolgreich || Schluessel.Length == 0) return false;
            if (Schluessel == KlimaImportAblauf.Vorschauschluessel(auftrag)) return true;

            return auftrag.Art == KlimaImportArt.AusKoordinaten && Standort != null &&
                   Schluessel.EndsWith(KlimaImportAblauf.Quellenteil(auftrag), StringComparison.Ordinal) &&
                   auftrag.Longitude == Standort.Longitude && auftrag.Latitude == Standort.Latitude;
        }
    }

    /// <summary>
    /// Ein PVGIS-Abruf SAMT KOPF (Auftrag KD-2) — die Naht der Vorschau. Unter Windows
    /// <c>PVGIS_EPW_Downloader.GetTmyAbruf</c>, in der Probe die eingefrorene Antwort.
    /// </summary>
    public delegate Task<PvgisAbruf> IPvgisAbruf(double lon, double lat, int azimut);

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
        /// Höhe des Standorts über NN [m], wenn der Anwender sie kennt (Auftrag KD-2);
        /// <c>null</c> heißt unbekannt. Sie wirkt nur auf den Höhenhinweis der Vorschau.
        /// </summary>
        public double? StandortHoeheM;

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
        /// <param name="vorschau">Eine Vorschau (Auftrag KD-2, Entscheid KD-Q3): Passt sie
        /// zum Auftrag (<see cref="KlimaVorschauErgebnis.PasstZu"/>) und ist sie
        /// importierbar, übernimmt der Import ihren gelesenen Satz — <b>ohne zweiten Abruf
        /// und ohne zweite Geokodierung</b>. Eine verfallene Vorschau wird übergangen.</param>
        public static async Task<KlimaImportErgebnis> Laufen(
            KlimaImportAuftrag auftrag,
            ITmyQuelle tmy,
            IOrtsQuelle orte = null,
            IProgress<ImportFortschritt> melder = null,
            CancellationToken abbruch = default,
            INetzbereich bereich = null,
            KlimaVorschauErgebnis vorschau = null)
        {
            var erg = new KlimaImportErgebnis();
            if (auftrag == null) return Abbruch(erg);

            bool mitVorschau = vorschau != null && vorschau.Importierbar && vorschau.PasstZu(auftrag);
            if (!mitVorschau && tmy == null && auftrag.Quelle == KlimaQuelle.PvgisTmy) return Abbruch(erg);

            // ---- Schritt 1: Koordinaten -------------------------------------
            Melden(melder, 1, "KLIMA_SCHRITT_KOORDINATEN");

            double lon, lat;
            string details;
            string bezeichner;

            if (auftrag.Art == KlimaImportArt.AusOrtsname)
            {
                string ort = (auftrag.Ortsname ?? "").Trim();
                if (mitVorschau)
                {
                    // KD-Q3: KEINE zweite Geokodierung - der Ort ist der der Vorschau.
                    lon = vorschau.Satz.Lon;
                    lat = vorschau.Satz.Lat;
                    details = vorschau.Satz.Anzeigename;
                }
                else
                {
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
                }
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
            Lesesatz satz;
            if (mitVorschau)
            {
                Melden(melder, 2, Schrittschluessel(auftrag.Quelle));
                satz = vorschau.Satz;
            }
            else
            {
                IPvgisAbruf pvgis = tmy == null ? null : AusTmyQuelle(tmy);
                satz = await DatensatzLesen(auftrag, lon, lat, pvgis, bereich, melder, abbruch)
                       .ConfigureAwait(false);

                if (satz.Ausgang == KlimaImportAusgang.Abgebrochen) return Abbruch(erg);
                if (satz.Ausgang != KlimaImportAusgang.Erfolg) return Fehler(erg, satz.Ausgang, satz.Meldung);

                // Fehlt die gewaehlte Ausfuehrung, bricht der Import BENANNT ab - mit
                // demselben Grund, den die Vorschau zeigt.
                // (Nur im Regionalweg: Eine leere PVGIS-Antwort laeuft wie bisher weiter.)
                if (!satz.Gefunden && auftrag.Quelle == KlimaQuelle.TryRegional)
                    return Fehler(erg, KlimaImportAusgang.Dateifehler, satz.Grund);
            }

            // Der Import rechnet auf einer KOPIE: Die Vorschau behaelt ihren Satz, wie
            // er gelesen wurde (DirektNormal und Sonnenstaende aendern die Stunden).
            List<TmyHourlyData> stunden = new List<TmyHourlyData>(satz.Stunden.Count);
            foreach (TmyHourlyData h in satz.Stunden) stunden.Add(h.Kopie());

            lon = satz.Lon;
            lat = satz.Lat;
            string herkunft = satz.Herkunft;

            // Schemaschritt 97 (Auftrag KL-6): WELCHES Wetterjahr die Reihe
            // beschreibt. Leer bzw. null heisst "sagt nichts dazu" - so bleibt es
            // bei PVGIS, das keine TRY-Szenarien kennt.
            string szenario = satz.Szenario;
            int? bezugsjahr = satz.Bezugsjahr;

            if (auftrag.Quelle != KlimaQuelle.PvgisTmy)
                DwdTryLeser.DirektNormal(stunden, lon, lat);

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
        //  Der gemeinsame LESEWEG von Vorschau und Import (Auftrag KD-2)
        // =====================================================================

        /// <summary>
        /// Ein gelesener Datensatz — die Stunden WIE GELESEN (vor <c>DirektNormal</c> und
        /// den Sonnenständen), dazu Standort, Herkunft und Wetterjahr. Vorschau und Import
        /// lesen über <see cref="DatensatzLesen"/>; eine Vorschau trägt ihren Satz zum
        /// Import (Entscheid KD-Q3).
        /// </summary>
        internal sealed class Lesesatz
        {
            public KlimaImportAusgang Ausgang = KlimaImportAusgang.Erfolg;
            public string Meldung = "";

            /// <summary>Der Rechenstandort — bei einer TRY-Datei ohne Koordinaten aus dem Kopf.</summary>
            public double Lon, Lat;

            /// <summary>Anzeigename der Geokodierung; leer bei Koordinaten von Hand.</summary>
            public string Anzeigename = "";

            /// <summary>Kommt der Standort aus dem Dateikopf (TRY-Datei ohne Koordinaten)?</summary>
            public bool StandortAusKopf;

            public List<TmyHourlyData> Stunden = new List<TmyHourlyData>();
            public string Herkunft = "";
            public string Szenario = "";
            public int? Bezugsjahr;

            /// <summary>Gibt es die gewählte Ausführung? Nein: <see cref="Grund"/>.</summary>
            public bool Gefunden = true;
            public string Grund = "";

            /// <summary>Kopf der TRY-Datei bzw. des Paketeintrags; <c>null</c> bei PVGIS.</summary>
            public TryKopf Kopf;

            /// <summary>Das Ergebnis des Paketlesers (nur Regionalweg).</summary>
            public TryPaketErgebnis Paket;

            /// <summary>Der PVGIS-Abruf samt Kopf (nur PVGIS).</summary>
            public PvgisAbruf Pvgis;

            /// <summary>Dateiname der TRY-Datei (nur Datei).</summary>
            public string Dateiname = "";
        }

        /// <summary>Der Fortschrittsschlüssel des zweiten Schritts je Quelle.</summary>
        private static string Schrittschluessel(KlimaQuelle quelle)
        {
            switch (quelle)
            {
                case KlimaQuelle.TryDatei: return "KLIMA_SCHRITT_DATEI";
                case KlimaQuelle.TryRegional: return "KLIMA_SCHRITT_PAKET";
                default: return "KLIMA_SCHRITT_ABRUF";
            }
        }

        /// <summary>Die Naht des Imports (<see cref="ITmyQuelle"/>) als Abruf ohne Kopf.</summary>
        public static IPvgisAbruf AusTmyQuelle(ITmyQuelle tmy)
        {
            if (tmy == null) return null;
            return async (lon, lat, azimut) => new PvgisAbruf
            {
                Stunden = await tmy(lon, lat, azimut).ConfigureAwait(false) ?? new List<TmyHourlyData>()
            };
        }

        /// <summary>
        /// Liest den Datensatz einer der drei Quellen — <b>der eine Leseweg</b> von
        /// Vorschau und Import. Fehler kommen BENANNT im Satz zurück, wie sie der Import
        /// schon immer meldete; eine fehlende Ausführung des Regionalpakets ist kein
        /// Fehler, sondern <see cref="Lesesatz.Gefunden"/> = nein mit Grund.
        /// </summary>
        internal static async Task<Lesesatz> DatensatzLesen(
            KlimaImportAuftrag auftrag, double lon, double lat,
            IPvgisAbruf pvgis, INetzbereich bereich,
            IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            var satz = new Lesesatz { Lon = lon, Lat = lat };

            if (auftrag.Quelle == KlimaQuelle.PvgisTmy)
            {
                // ---- PVGIS: EIN Abruf (A-10), unveraendert ------------------
                Melden(melder, 2, "KLIMA_SCHRITT_ABRUF");

                if (pvgis == null) return SatzFehler(satz, KlimaImportAusgang.Abgebrochen,
                                                     MyResource.Resource.KLIMA_MSG_ABGEBROCHEN);
                try
                {
                    satz.Pvgis = await pvgis(lon, lat, AZIMUT_SUED).ConfigureAwait(false) ?? new PvgisAbruf();
                    satz.Stunden = satz.Pvgis.Stunden ?? new List<TmyHourlyData>();
                }
                catch (ArgumentException ex)
                {
                    return SatzFehler(satz, KlimaImportAusgang.Netzfehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_PVGIS_EINGABE, ex.Message));
                }
                catch (Exception ex)
                {
                    return SatzFehler(satz, KlimaImportAusgang.Netzfehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_MSG_DOWNLOAD_FEHLER, ex.Message));
                }

                if (satz.Stunden.Count == 0)
                {
                    satz.Gefunden = false;
                    satz.Grund = MyResource.Resource.KLIMA_VORSCHAU_LEER;
                }
                return satz;
            }

            if (auftrag.Quelle == KlimaQuelle.TryDatei)
            {
                // ---- DWD-TRY-Datei: KEIN Netz ------------------------------
                Melden(melder, 2, "KLIMA_SCHRITT_DATEI");

                string pfad = (auftrag.TryPfad ?? "").Trim();
                if (pfad.Length == 0)
                    return SatzFehler(satz, KlimaImportAusgang.Eingabefehler,
                        MyResource.Resource.KLIMA_MSG_EINGABEN_PRUEFEN);

                TryKopf kopf;
                try
                {
                    satz.Stunden = DwdTryLeser.LesenDatei(pfad, out kopf);
                }
                catch (FileNotFoundException)
                {
                    return SatzFehler(satz, KlimaImportAusgang.Eingabefehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_TRY_DATEI_FEHLT, pfad));
                }
                catch (Exception ex)
                {
                    return SatzFehler(satz, KlimaImportAusgang.Dateifehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KLIMA_TRY_FORMATFEHLER, ex.Message));
                }

                satz.Kopf = kopf;
                satz.Dateiname = Path.GetFileName(pfad);

                // ---- der STANDORT (Auftrag KL-2) ---------------------------
                // Der Auftrag hat Vorrang: Was der Anwender eingetragen oder ueber
                // den Ortsnamen geholt hat, bleibt stehen. Erst wenn er KEINE
                // Koordinaten mitbringt, kommt der Standort aus dem Dateikopf
                // (Lambert -> Laenge/Breite). Beides wird BENANNT vermerkt.
                if (auftrag.Art == KlimaImportArt.AusKoordinaten && lon == 0 && lat == 0)
                {
                    if (!kopf.StandortBekannt)
                        return SatzFehler(satz, KlimaImportAusgang.Eingabefehler,
                            MyResource.Resource.KLIMA_TRY_STANDORT_FEHLT);

                    satz.Lon = kopf.Laenge!.Value;
                    satz.Lat = kopf.Breite!.Value;
                    satz.StandortAusKopf = true;
                }

                string standort = satz.StandortAusKopf
                    ? string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_STANDORT_KOPF,
                        kopf.Rechtswert, kopf.Hochwert,
                        satz.Lon.ToString("F4", CultureInfo.CurrentCulture),
                        satz.Lat.ToString("F4", CultureInfo.CurrentCulture))
                    : MyResource.Resource.KLIMA_TRY_STANDORT_ANWENDER;

                satz.Herkunft = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KLIMA_TRY_DETAILS_DATEI,
                    satz.Dateiname,
                    DateTime.Now.ToString("d", CultureInfo.CurrentCulture),
                    MyResource.Resource.KLIMA_TRY_LIZENZ,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_VERWORFEN, kopf.VerworfenText))
                    + " · " + standort;

                // Der Bezugszeitraum WOERTLICH im Herkunftsvermerk: Das Bezugsjahr
                // unten ist daraus abgeleitet, also bleibt die Herleitung in der
                // Zeile nachlesbar (Auftrag KL-7).
                string zeitraum = (kopf.Bezugszeitraum ?? "").Trim();
                if (zeitraum.Length > 0)
                    satz.Herkunft += " · " + string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KLIMA_TRY_BEZUGSZEITRAUM, zeitraum);

                // Schemaschritt 97: Das SZENARIO steht im Dateikopf ("Art des TRY"),
                // das Bezugsjahr steht dort als ZEITRAUM ("1995-2012", "2031-2060").
                // Die Zuordnung Zeitraum -> Jahr ist die DWD-Konvention und eine
                // benannte Tabelle (Anwenderentscheid vom 20.09.2026); ein Zeitraum,
                // der nicht darin steht, ergibt kein Jahr - nie die Vorgabe.
                satz.Szenario = SzenarioschluesselAusKopf(kopf.Art);
                satz.Bezugsjahr = BezugsjahrAusZeitraum(kopf.Bezugszeitraum);
                return satz;
            }

            // ---- TRY-Regionaldaten: Bereichsabruf ODER lokale data.zip --------
            Melden(melder, 2, "KLIMA_SCHRITT_PAKET");

            string paket = (auftrag.TryPaketPfad ?? "").Trim();
            string quelle;
            TryPaketErgebnis paketErg;

            try
            {
                if (paket.Length > 0)
                {
                    quelle = Path.GetFileName(paket);
                    paketErg = TryPaketLeser.VorschauAusDatei(
                        paket, lon, lat, auftrag.TryJahr, auftrag.Szenario);
                }
                else
                {
                    if (bereich == null)
                        return SatzFehler(satz, KlimaImportAusgang.Eingabefehler,
                            MyResource.Resource.KLIMA_TRY_KEIN_BEREICH);

                    quelle = Adresse();
                    paketErg = await TryPaketLeser.VorschauAusNetzAsync(
                        bereich, quelle, lon, lat, auftrag.TryJahr, auftrag.Szenario,
                        abbruch).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return SatzFehler(satz, KlimaImportAusgang.Abgebrochen,
                                  MyResource.Resource.KLIMA_MSG_ABGEBROCHEN);
            }
            catch (FileNotFoundException)
            {
                return SatzFehler(satz, KlimaImportAusgang.Eingabefehler,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_DATEI_FEHLT, paket));
            }
            catch (FormatException ex)
            {
                return SatzFehler(satz, KlimaImportAusgang.Dateifehler,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_TRY_FORMATFEHLER, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                // Keine Teilabrufe, keine Region, ausserhalb 300 km - alles BENANNT.
                return SatzFehler(satz, KlimaImportAusgang.Dateifehler, ex.Message);
            }
            catch (Exception ex)
            {
                return SatzFehler(satz, KlimaImportAusgang.Netzfehler,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KLIMA_MSG_DOWNLOAD_FEHLER, ex.Message));
            }

            satz.Paket = paketErg;
            satz.Gefunden = paketErg.AusfuehrungGefunden;
            satz.Grund = paketErg.Grund ?? "";
            if (!satz.Gefunden) return satz;

            satz.Stunden = paketErg.Stunden;
            satz.Kopf = paketErg.Kopf;

            // Die Region wird mit NAMEN genannt (Auftrag KL-3): "14 Stoetten"
            // statt "14" - die Nummer allein sagt einem Anwender nichts. Die
            // ZUORDNUNG bleibt der naechste Stationsmittelpunkt aus dem Paket.
            satz.Herkunft = string.Format(CultureInfo.CurrentCulture,
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
            satz.Szenario = Szenarioschluessel(auftrag.Szenario);
            satz.Bezugsjahr = auftrag.TryJahr;
            return satz;
        }

        private static Lesesatz SatzFehler(Lesesatz satz, KlimaImportAusgang ausgang, string meldung)
        {
            satz.Ausgang = ausgang;
            satz.Meldung = meldung ?? "";
            return satz;
        }

        /// <summary>
        /// Der Teil des Vorschauschlüssels, der die QUELLE beschreibt: Quelle, Datei bzw.
        /// Paket, im Regionalweg Jahr und Szenario.
        /// </summary>
        internal static string Quellenteil(KlimaImportAuftrag a)
        {
            switch (a.Quelle)
            {
                case KlimaQuelle.TryDatei:
                    return "|Q=TryDatei|" + (a.TryPfad ?? "").Trim();
                case KlimaQuelle.TryRegional:
                    return "|Q=TryRegional|" + (a.TryPaketPfad ?? "").Trim() + "|" +
                           a.TryJahr.ToString(CultureInfo.InvariantCulture) + "|" + a.Szenario;
                default:
                    return "|Q=Pvgis";
            }
        }

        /// <summary>
        /// Der Schlüssel der Eingaben, aus denen eine Vorschau liest — Ort bzw.
        /// Koordinaten und <see cref="Quellenteil"/>. Ändert sich einer davon, verfällt
        /// die Vorschau. Bezeichnung und Standorthöhe gehören nicht dazu: Sie ändern den
        /// gelesenen Satz nicht.
        /// </summary>
        internal static string Vorschauschluessel(KlimaImportAuftrag a)
        {
            string ort = a.Art == KlimaImportArt.AusOrtsname
                ? "O=" + (a.Ortsname ?? "").Trim()
                : "K=" + a.Longitude.ToString("R", CultureInfo.InvariantCulture) + ";" +
                         a.Latitude.ToString("R", CultureInfo.InvariantCulture);
            return ort + Quellenteil(a);
        }

        // =====================================================================
        //  Die VORSCHAU (Auftrag KL-3, gelesen seit KD-2)
        // =====================================================================

        /// <summary>
        /// Sucht VOR dem Import den Datensatz des eingetragenen Standorts und LIEST ihn —
        /// für alle drei Quellen (Auftrag KD-2, Entscheid KD-Q3): PVGIS ruft die API, die
        /// TRY-Datei liest die Datei, der Regionalweg liest einen Eintrag des Pakets
        /// (rund 155 KB). <b>Nichts wird gespeichert.</b>
        ///
        /// <para>Zurück kommen Standort (Koordinate, Anzeigename, Höhe), Bezugspunkt
        /// (Station, Dateistandort oder PVGIS-Punkt mit Höhe und Entfernung),
        /// <see cref="KlimaVorschauErgebnis.AusfuehrungGefunden"/>, Kennzahlen,
        /// Parameterbestand und die Hinweise nach KD-Q1. Der gelesene Satz wandert mit;
        /// <see cref="Laufen"/> übernimmt ihn, solange
        /// <see cref="KlimaVorschauErgebnis.PasstZu"/> gilt.</para>
        /// </summary>
        /// <param name="auftrag">Standort, Quelle, bei TRY Datei bzw. Paket, Jahr und Szenario.</param>
        /// <param name="orte">Die Ortsauflösung; nur für <c>AusOrtsname</c> nötig.</param>
        /// <param name="abbruch">Abbruchmarke.</param>
        /// <param name="bereich">Der Bereichsabruf; ohne ihn steht im Regionalweg nur
        /// eine lokale <c>data.zip</c> offen.</param>
        /// <param name="pvgis">Der PVGIS-Abruf samt Kopf; ohne ihn bricht die Vorschau
        /// einer PVGIS-Quelle ab.</param>
        public static async Task<KlimaVorschauErgebnis> RegionErmittelnAsync(
            KlimaImportAuftrag auftrag,
            IOrtsQuelle orte = null,
            CancellationToken abbruch = default,
            INetzbereich bereich = null,
            IPvgisAbruf pvgis = null)
        {
            var erg = new KlimaVorschauErgebnis();
            if (auftrag == null) return VorschauFehler(erg, KlimaImportAusgang.Abgebrochen, "");

            erg.Quelle = auftrag.Quelle;
            double lon, lat;
            string anzeigename = "";
            bool geokodiert = false;

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
                anzeigename = antwort.DisplayName ?? "";
                geokodiert = true;
            }
            else
            {
                lon = auftrag.Longitude;
                lat = auftrag.Latitude;
            }

            if (abbruch.IsCancellationRequested)
                return VorschauFehler(erg, KlimaImportAusgang.Abgebrochen,
                                      MyResource.Resource.KLIMA_MSG_ABGEBROCHEN);

            Lesesatz satz = await DatensatzLesen(auftrag, lon, lat, pvgis, bereich, null, abbruch)
                            .ConfigureAwait(false);
            if (satz.Ausgang != KlimaImportAusgang.Erfolg)
                return VorschauFehler(erg, satz.Ausgang, satz.Meldung);

            satz.Anzeigename = anzeigename;

            erg.Ausgang = KlimaImportAusgang.Erfolg;
            erg.Satz = satz;
            erg.Schluessel = Vorschauschluessel(auftrag);
            erg.AusfuehrungGefunden = satz.Gefunden;
            erg.Grund = satz.Grund;
            erg.Szenario = auftrag.Szenario;
            erg.Bezugsjahr = satz.Bezugsjahr;

            // ---- Standort ----------------------------------------------------
            erg.Standort = new KlimaStandort
            {
                Longitude = satz.Lon,
                Latitude = satz.Lat,
                Anzeigename = anzeigename,
                AusGeokodierung = geokodiert,
                HoeheM = auftrag.StandortHoeheM
            };

            // ---- Bezugspunkt -------------------------------------------------
            var bezug = new KlimaBezugspunkt();
            double? kopfHoehe = satz.Kopf != null ? KlimaVorschauFestwerte.HoeheAusText(satz.Kopf.Hoehe) : null;

            if (auftrag.Quelle == KlimaQuelle.TryRegional)
            {
                TryRegion region = satz.Paket.Region;
                erg.Region = region;
                erg.Ausfuehrungen = satz.Paket.Ausfuehrungen;
                bezug.Art = KlimaBezugspunktArt.Repraesentanzstation;
                bezug.Name = region.Bezeichnung;
                bezug.Latitude = region.Latitude;
                bezug.Longitude = region.Longitude;
                bezug.EntfernungKm = region.EntfernungKm;
                bezug.HoeheM = kopfHoehe;
            }
            else if (auftrag.Quelle == KlimaQuelle.TryDatei)
            {
                bezug.Art = KlimaBezugspunktArt.Dateistandort;
                bezug.Name = satz.Dateiname;
                bezug.Latitude = satz.Kopf.Breite;
                bezug.Longitude = satz.Kopf.Laenge;
                bezug.HoeheM = kopfHoehe;
                if (satz.StandortAusKopf && !erg.Standort.HoeheM.HasValue)
                    erg.Standort.HoeheM = kopfHoehe;
            }
            else
            {
                PvgisOrt ort = satz.Pvgis?.Ort;
                bezug.Art = KlimaBezugspunktArt.PvgisPunkt;
                bezug.Name = (satz.Pvgis?.MeteoDatenbank ?? "").Trim(' ', '-');
                bezug.Latitude = ort?.Latitude ?? satz.Lat;
                bezug.Longitude = ort?.Longitude ?? satz.Lon;
                bezug.HoeheM = ort?.Elevation;
                // PVGIS rechnet AM Standort: Seine Hoehe ist die des Standorts.
                if (!erg.Standort.HoeheM.HasValue) erg.Standort.HoeheM = ort?.Elevation;
            }

            if (!bezug.EntfernungKm.HasValue && bezug.Latitude.HasValue && bezug.Longitude.HasValue)
                bezug.EntfernungKm = TryPaketLeser.EntfernungKm(
                    satz.Lat, satz.Lon, bezug.Latitude.Value, bezug.Longitude.Value);

            erg.Bezugspunkt = bezug;
            erg.HoehenunterschiedM = bezug.HoeheM.HasValue && erg.Standort.HoeheM.HasValue
                ? bezug.HoeheM.Value - erg.Standort.HoeheM.Value
                : (double?)null;

            // ---- Kennzahlen und Parameterbestand -----------------------------
            // KD-8: Die TRY-Datei führt die Direktstrahlung horizontal; die Monatsreihe
            // „Direkt normal“ rechnet auf einer Kopie wie der Import (DirektNormal).
            List<TmyHourlyData> direktNormal = null;
            if (auftrag.Quelle != KlimaQuelle.PvgisTmy && satz.Stunden != null)
            {
                direktNormal = new List<TmyHourlyData>(satz.Stunden.Count);
                foreach (TmyHourlyData h in satz.Stunden) direktNormal.Add(h.Kopie());
                DwdTryLeser.DirektNormal(direktNormal, satz.Lon, satz.Lat);
            }
            erg.Kennzahlen = KlimaKennzahlen.Rechnen(satz.Stunden, direktNormal);
            erg.Parameterbestand = KlimaParameterbestand.Fuer(auftrag.Quelle, satz.Stunden);

            // ---- Hinweise (KD-Q1, KD-Q9) -------------------------------------
            var hinweise = new List<string>();
            erg.Repraesentanzhinweis = auftrag.Quelle == KlimaQuelle.TryRegional;
            if (erg.Repraesentanzhinweis) hinweise.Add(MyResource.Resource.KLIMA_VORSCHAU_REPRAESENTANZ);

            erg.Ortshinweis = KlimaVorschauFestwerte.NichtOrtstypisch(bezug.EntfernungKm, erg.HoehenunterschiedM);
            if (erg.Ortshinweis) hinweise.Add(MyResource.Resource.KLIMA_VORSCHAU_NICHT_ORTSTYPISCH);
            erg.Hinweise = hinweise;

            // ---- die Zeile ---------------------------------------------------
            string zeile;
            if (auftrag.Quelle == KlimaQuelle.TryRegional)
                zeile = Vorschauzeile(new TryRegionVorschau
                {
                    Region = erg.Region,
                    Ausfuehrungen = erg.Ausfuehrungen
                });
            else
                zeile = satz.Herkunft;

            var teile = new List<string>();
            if (!string.IsNullOrEmpty(zeile)) teile.Add(zeile);
            if (!erg.AusfuehrungGefunden && erg.Grund.Length > 0) teile.Add(erg.Grund);
            teile.AddRange(hinweise);
            erg.Meldung = string.Join(" · ", teile);
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
        /// <para><b>Das Bezugsjahr steht als ZEITRAUM im Kopf</b> („1995-2012",
        /// „2031-2060"); es kommt aus <see cref="BezugsjahrAusZeitraum"/>.</para>
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
        /// Die BENANNTE Zuordnung Bezugszeitraum → Bezugsjahr (DWD-Konvention,
        /// Anwenderentscheid vom 20.09.2026). Eine Tabelle, keine Schwelle: Was hier
        /// nicht steht, hat kein Bezugsjahr.
        /// </summary>
        private static readonly (int Von, int Bis, int Jahr)[] BEZUGSZEITRAEUME =
        {
            (1995, 2012, KlimaImportAuftrag.TRY_JAHR_VORGABE),
            (2031, 2060, KlimaImportAuftrag.TRY_JAHR_PROJEKTION)
        };

        /// <summary>
        /// Das Bezugsjahr aus der Kopfzeile „Bezugszeitraum" einer DWD-TRY-Datei —
        /// <c>null</c>, wenn der Kopf keinen oder einen unbekannten Zeitraum nennt
        /// (Auftrag KL-7).
        ///
        /// <para><b>Die Zuordnung ist die DWD-Konvention und eine Tabelle</b>
        /// (<see cref="BEZUGSZEITRAEUME"/>): 1995-2012 ist das Testreferenzjahr 2015,
        /// 2031-2060 die Projektion 2045. Jeder andere Zeitraum — ein einzelnes Jahr,
        /// „1961-1990", gar nichts — ergibt <c>null</c> und NIE die Vorgabe: Ein
        /// Bezugsjahr, das die Datei nicht deckt, wäre eine Behauptung über sie.</para>
        ///
        /// <para>Gelesen werden die ersten beiden vierstelligen Jahreszahlen des
        /// Textes, gleich womit sie verbunden sind (Bindestrich, Gedankenstrich,
        /// „bis", Zwischenräume).</para>
        /// </summary>
        public static int? BezugsjahrAusZeitraum(string zeitraum)
        {
            if (!Jahrespaar(zeitraum, out int von, out int bis)) return null;

            foreach ((int Von, int Bis, int Jahr) z in BEZUGSZEITRAEUME)
                if (z.Von == von && z.Bis == bis) return z.Jahr;

            return null;
        }

        /// <summary>
        /// Die ersten beiden VIERSTELLIGEN Jahreszahlen eines freien Textes. Eine
        /// Ziffernfolge anderer Länge ist keine Jahreszahl und wird übergangen;
        /// findet sich keine zweite, ist das Ergebnis <c>false</c>.
        /// </summary>
        private static bool Jahrespaar(string text, out int von, out int bis)
        {
            von = 0;
            bis = 0;

            string t = text ?? "";
            int gefunden = 0;

            for (int i = 0; i < t.Length; )
            {
                // NUR die Ziffern 0-9: char.IsDigit nimmt auch fremde Ziffernsaetze,
                // an denen int.Parse dann scheitern wuerde.
                if (!Ziffer(t[i])) { i++; continue; }

                int start = i;
                while (i < t.Length && Ziffer(t[i])) i++;
                if (i - start != 4) continue;

                int jahr = int.Parse(t.Substring(start, 4), CultureInfo.InvariantCulture);
                if (gefunden == 0) von = jahr; else bis = jahr;
                if (++gefunden == 2) return true;
            }

            return false;
        }

        /// <summary>Eine Ziffer 0-9 — und nur die.</summary>
        private static bool Ziffer(char c) => c >= '0' && c <= '9';

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
