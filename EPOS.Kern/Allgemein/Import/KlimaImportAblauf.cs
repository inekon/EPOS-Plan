using System;
using System.Collections.Generic;
using System.Globalization;
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
        Schreibfehler
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
    }

    /// <summary>
    /// Woher die TMY-Stundenwerte kommen. <b>Der einzige Netzzugriff des Programms</b>
    /// hängt an diesem Delegaten (Risiko R-W14c-5): Unter Windows ist es
    /// <c>PVGIS_EPW_Downloader.GetTMY</c>, in der Probe eine eingefrorene Datei.
    /// </summary>
    public delegate Task<List<TmyHourlyData>> ITmyQuelle(double lon, double lat, int azimut);

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
        public static async Task<KlimaImportErgebnis> Laufen(
            KlimaImportAuftrag auftrag,
            ITmyQuelle tmy,
            IOrtsQuelle orte = null,
            IProgress<ImportFortschritt> melder = null,
            CancellationToken abbruch = default)
        {
            var erg = new KlimaImportErgebnis();
            if (auftrag == null || tmy == null) return Abbruch(erg);

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

            // ---- Schritt 2: EIN PVGIS-Abruf (A-10) --------------------------
            Melden(melder, 2, "KLIMA_SCHRITT_ABRUF");

            List<TmyHourlyData> stunden;
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
            return erg;
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
