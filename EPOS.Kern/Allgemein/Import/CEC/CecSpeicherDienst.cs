using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Abruf der CEC Energy Storage System List</b> — Stufe S1 des
    /// <c>Konzept_Stromspeicherimport_EPOS-Plan.md</c> (Anwenderentscheide
    /// <b>W13‑E‑2‑Q2</b> und <b>‑Q8</b> vom 07.09.2026: „Empfehlung", also die
    /// Liste NICHT mitliefern und statt dessen einen Abrufknopf anbieten).
    ///
    /// <para><b>Warum es ihn geben MUSS.</b> Die Nutzungsbedingungen der
    /// California Energy Commission (abgerufen am 07.09.2026,
    /// <c>https://www.energy.ca.gov/conditions-of-use</c>) untersagen die
    /// Verwendung ihrer Materialien „for commercial or profit-making purposes"
    /// ausdrücklich. Anders als bei den CEC-Modul- und -Wechselrichterlisten gibt
    /// es für die Speicherliste keinen NREL-Umweg unter BSD‑3‑Clause
    /// (Kapitel 1.3 des Konzepts: <c>deploy/libraries</c> führt keine
    /// Speicherliste). EPOS-Plan liefert sie deshalb nicht mit — der Anwender
    /// holt sie mit diesem Knopf selbst, so wie er es im Browser auch täte.</para>
    ///
    /// <para><b>Zwilling zu <see cref="CecWechselrichterDienst"/>.</b> Derselbe
    /// Apparat: 45 Sekunden Zeitgrenze, ein 30-Tage-Zwischenspeicher neben dem der
    /// Module und Wechselrichter, ein Fortschrittsmelder mit Abbruch. Zwei Dinge
    /// sind anders, und beide kommen von der Quelle:</para>
    /// <list type="bullet">
    ///   <item><b>Die Quelle liefert XLSX, nicht CSV</b>
    ///     (<c>/Home/DownloadtoExcel?filename=EnergyStorage</c>, 1 338 305 Byte am
    ///     07.09.2026, <c>Content-Disposition: …Energy_Storage_System_List_Data_ADA.xlsx</c>).
    ///     Der Dienst lädt deshalb BYTES und keinen Text — ein
    ///     <c>ReadAsStringAsync</c> zerstörte die Mappe.</item>
    ///   <item><b>Er zerlegt nichts.</b> Das tut
    ///     <see cref="CecSpeicherImport"/>, und der liest Datei ODER Tabelle. Der
    ///     Dienst liefert deshalb den PFAD des Zwischenspeichers zurück; wer die
    ///     Sätze will, gibt ihn dem Zerleger. So gibt es die Erkennung der
    ///     Kopfzeile genau einmal.</item>
    /// </list>
    ///
    /// <para><b>Er ist ohne Netz prüfbar.</b> Der Konstruktor nimmt einen
    /// <see cref="HttpMessageHandler"/> und einen Ablageort entgegen; die Prüfung
    /// stellt beides. Ohne Angabe gilt der Vorgabeweg — dann ist der Dienst
    /// wortgleich zu seinem Wechselrichterzwilling.</para>
    ///
    /// <para><b>Der Kern kennt keine Anzeigetexte.</b> Jede Rückmeldung ist ein
    /// <see cref="SpeicherImportMeldung"/> — Schlüssel und Platzhalterwerte.</para>
    /// </summary>
    public sealed class CecSpeicherDienst
    {
        /// <summary>
        /// Der Abrufweg der Energy Commission. Der Dateiname ist NICHT zu raten —
        /// <c>?filename=Battery</c> antwortet mit HTTP 200 und 0 Byte; den Namen
        /// gibt allein der Verweis auf der Übersichtsseite her (Kapitel 1.2).
        /// </summary>
        internal const string URL =
            "https://solarequipment.energy.ca.gov/Home/DownloadtoExcel?filename=EnergyStorage";

        /// <summary>
        /// Kleiner als das ist keine Speicherliste. Die echte Mappe misst
        /// 1,3 MB; die Grenze fängt die 0-Byte-Antwort und jede Fehlerseite ab,
        /// ohne eine geschrumpfte künftige Auflage auszuschließen.
        /// </summary>
        internal const int MINDESTGROESSE = 10000;

        /// <summary>So alt darf der Zwischenspeicher werden, bevor neu geholt wird.</summary>
        internal const int ZWISCHENSPEICHER_TAGE = 30;

        private readonly string _zwischenspeicher;
        private readonly HttpMessageHandler _handler;

        /// <param name="handler">
        /// Der HTTP-Unterbau; <c>null</c> heißt „der des Systems". Die Prüfung
        /// stellt ihn und braucht damit kein Netz.
        /// </param>
        /// <param name="zwischenspeicher">
        /// Ablageort der geholten Mappe; <c>null</c> heißt „neben den zwei
        /// anderen CEC-Listen". Er liegt in DERSELBEN Ablage und trägt einen
        /// EIGENEN Namen — eine gemeinsame Datei entwertete beim ersten Abruf die
        /// jeweils andere Liste.
        /// </param>
        public CecSpeicherDienst(HttpMessageHandler handler = null, string zwischenspeicher = null)
        {
            _handler = handler;
            _zwischenspeicher = string.IsNullOrWhiteSpace(zwischenspeicher)
                ? Dienste.Pfade.Verbinde(Dienste.Pfade.BenutzerLokalBasis,
                                         "CECModuleImporter", "cec_energy_storage.xlsx")
                : zwischenspeicher;
        }

        /// <summary>Wohin die geholte Mappe gelegt wird.</summary>
        public string Zwischenspeicher => _zwischenspeicher;

        /// <summary>
        /// Holt die Liste — aus dem Zwischenspeicher, wenn er jünger als
        /// 30 Tage ist, sonst über HTTP.
        ///
        /// <para>Bleibt das Netz stumm, aber es liegt ein ÄLTERER Zwischenspeicher
        /// vor, gewinnt der alte Stand: Eine Liste vom vorigen Monat ist besser
        /// als keine, und der Anwender erfährt es über
        /// <c>SPIMP_MSG_CEC_ALT</c>.</para>
        /// </summary>
        /// <returns>
        /// <c>Erfolg</c> und der PFAD der Mappe; bei Misserfolg ein leerer Pfad
        /// und die Meldung, woran es lag.
        /// </returns>
        public async Task<(bool Erfolg, string Pfad, SpeicherImportMeldung Meldung)> LadenAsync(
            IProgress<SpeicherImportMeldung> melder = null,
            CancellationToken abbruch = default)
        {
            melder?.Report(new SpeicherImportMeldung("SPIMP_MSG_CEC_SUCHEN"));

            if (File.Exists(_zwischenspeicher))
            {
                TimeSpan alter = DateTime.Now - File.GetLastWriteTime(_zwischenspeicher);
                if (alter.TotalDays < ZWISCHENSPEICHER_TAGE)
                {
                    melder?.Report(new SpeicherImportMeldung("SPIMP_MSG_CEC_CACHE"));
                    return (true, _zwischenspeicher, new SpeicherImportMeldung("SPIMP_MSG_CEC_CACHE"));
                }
            }

            melder?.Report(new SpeicherImportMeldung("SPIMP_MSG_CEC_VERBINDEN"));

            // Ein GESTELLTER Handler gehoert dem Aufrufer - der HttpClient darf ihn
            // deshalb nicht mit wegwerfen (disposeHandler: false).
            HttpClient http = _handler == null ? new HttpClient() : new HttpClient(_handler, false);
            try
            {
                http.Timeout = TimeSpan.FromSeconds(45);
                http.DefaultRequestHeaders.Add("User-Agent", "CECModuleImporter/1.0");

                abbruch.ThrowIfCancellationRequested();

                try
                {
                    HttpResponseMessage antwort = await http.GetAsync(URL, abbruch).ConfigureAwait(false);
                    if (antwort.IsSuccessStatusCode)
                    {
                        byte[] mappe = await antwort.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                        // Die 0-Byte-Antwort des geratenen Dateinamens kommt mit
                        // HTTP 200 - der Statuscode allein genuegt hier nicht.
                        if (mappe.Length >= MINDESTGROESSE)
                        {
                            string ordner = Path.GetDirectoryName(_zwischenspeicher);
                            if (!string.IsNullOrEmpty(ordner) && !Directory.Exists(ordner))
                                Directory.CreateDirectory(ordner);

                            File.WriteAllBytes(_zwischenspeicher, mappe);

                            var fertig = new SpeicherImportMeldung("SPIMP_MSG_CEC_GEHOLT",
                                mappe.Length.ToString(CultureInfo.InvariantCulture));
                            melder?.Report(fertig);
                            return (true, _zwischenspeicher, fertig);
                        }

                        melder?.Report(new SpeicherImportMeldung("SPIMP_MSG_CEC_LEER",
                            mappe.Length.ToString(CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        melder?.Report(new SpeicherImportMeldung("SPIMP_MSG_CEC_FEHLER",
                            ((int)antwort.StatusCode).ToString(CultureInfo.InvariantCulture)));
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    melder?.Report(new SpeicherImportMeldung("SPIMP_MSG_CEC_FEHLER", ex.Message));
                }
            }
            finally
            {
                http.Dispose();
            }

            if (File.Exists(_zwischenspeicher))
            {
                var alt = new SpeicherImportMeldung("SPIMP_MSG_CEC_ALT");
                melder?.Report(alt);
                return (true, _zwischenspeicher, alt);
            }

            return (false, "", new SpeicherImportMeldung("SPIMP_MSG_CEC_KEINE_QUELLE"));
        }
    }
}
