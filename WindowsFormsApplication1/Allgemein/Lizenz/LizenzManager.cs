using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>Gesamtzustand der Lizenz auf diesem Arbeitsplatz.</summary>
    public enum LizenzStatus
    {
        NichtAktiviert,     // kein (gültiges) Token vorhanden
        Gueltig,            // alles in Ordnung
        NachpruefungFaellig,// Offline-Leine abgelaufen, Karenzzeit läuft
        Kulanz,             // Lizenz abgelaufen, Kulanzfenster läuft
        Lesemodus,          // abgelaufen/Karenz überschritten: nur noch lesen
        UhrManipuliert,     // Systemuhr steht vor dem letzten bekannten Zeitpunkt
    }

    /// <summary>
    /// Zentrale Lizenzlogik des Clients (vgl. Konzept "Zeitlich beschränkte
    /// Lizenzierung", Kap. 4): signiertes Token mit zwei Fristen
    /// (gueltig_bis = Lizenzende, token_bis = Offline-Leine), verschlüsselte
    /// Ablage per DPAPI, monotoner Zeitanker gegen Zurückstellen der Uhr,
    /// stille Nachprüfung im Hintergrund und Lesemodus statt harter Sperre.
    /// </summary>
    public static class LizenzManager
    {
        /// <summary>Karenzzeit in Tagen nach Ablauf der Offline-Leine (token_bis).</summary>
        public const int KARENZ_TAGE = 14;

        /// <summary>Hintergrund-Nachprüfung, sobald die letzte Prüfung älter ist (Tage).</summary>
        public const int NACHPRUEFUNG_ALLE_TAGE = 14;

        /// <summary>Adresse des Lizenzportals für Hinweise an den Benutzer.</summary>
        public const string PORTAL_URL = "https://epos-plan.de/lizenzportal/";

        private static readonly object _sperre = new object();
        private static LizenzToken _token;
        private static bool _geladen;

        // ------------------------------------------------------------------
        //  Ablageorte
        // ------------------------------------------------------------------

        /// <summary>
        /// <c>%APPDATA%\wp-plan</c>, angelegt falls noetig — unveraendert derselbe
        /// Ordner wie vor iU5, nur ueber <c>Dienste.Pfade</c> gebildet.
        /// </summary>
        private static string Verzeichnis()
        {
            return Dienste.Pfade.Unterordner(Dienste.Pfade.Anwendungsdaten);
        }

        /// <summary>Name der Ablage mit dem signierten Lizenztoken.</summary>
        private const string TOKEN_ABLAGE = "lizenz.dat";

        /// <summary>Name der Ablage mit dem Datumsanker (Schutz gegen Zurueckdrehen der Uhr).</summary>
        private const string ANKER_ABLAGE = "lizenz-zeit.dat";

        /// <summary>Name des zweiten Datumsankers in den Einstellungen.</summary>
        private const string REGISTRY_ANKER = "LizenzAnker";

        // ------------------------------------------------------------------
        //  Öffentliche Sicht
        // ------------------------------------------------------------------

        /// <summary>Das aktuell gespeicherte (signaturgeprüfte) Token, sonst null.</summary>
        public static LizenzToken Token
        {
            get { TokenLaden(); return _token; }
        }

        /// <summary>
        /// Lizenzstatus bestimmen (rein offline, ohne Serverkontakt).
        /// Wird beim Programmstart und vor lizenzpflichtigen Aktionen gerufen.
        /// </summary>
        public static LizenzStatus Pruefe()
        {
            TokenLaden();
            DateTime heute = DateTime.UtcNow.Date;

            // Uhr-Manipulationsschutz: Systemzeit darf nicht vor dem höchsten
            // je gesehenen Zeitpunkt liegen (1 Tag Toleranz für Zeitzonen u. Ä.).
            // Beim allerersten Start existiert noch kein Anker (DateTime.MinValue) —
            // dann entfällt die Prüfung; AddDays auf "heute" statt auf dem Anker,
            // damit MinValue.AddDays(-1) keine ArgumentOutOfRangeException wirft.
            DateTime anker = AnkerLesen();
            if (anker > DateTime.MinValue && heute.AddDays(1) < anker)
                return LizenzStatus.UhrManipuliert;
            AnkerSchreiben(heute);

            if (_token == null)
                return LizenzStatus.NichtAktiviert;

            // Gerätebindung
            if (!string.Equals(_token.GeraeteId, GeraeteId.Ermitteln(), StringComparison.Ordinal))
                return LizenzStatus.NichtAktiviert;

            // Lizenzlaufzeit
            if (_token.GueltigBis.HasValue && heute > _token.GueltigBis.Value)
            {
                DateTime kulanzEnde = _token.GueltigBis.Value.AddDays(Math.Max(0, _token.KulanzTage));
                return heute <= kulanzEnde ? LizenzStatus.Kulanz : LizenzStatus.Lesemodus;
            }

            // Offline-Leine
            if (_token.TokenBis.HasValue && heute > _token.TokenBis.Value)
            {
                DateTime karenzEnde = _token.TokenBis.Value.AddDays(KARENZ_TAGE);
                return heute <= karenzEnde ? LizenzStatus.NachpruefungFaellig : LizenzStatus.Lesemodus;
            }

            return LizenzStatus.Gueltig;
        }

        /// <summary>Kurztext zum Status für Titelzeile, Dialoge und Hinweise.</summary>
        public static string StatusText()
        {
            LizenzStatus status = Pruefe();
            LizenzToken t = _token;
            switch (status)
            {
                case LizenzStatus.Gueltig:
                    return t.TypText() + " · gültig bis " + Datum(t.GueltigBis);
                case LizenzStatus.Kulanz:
                    return "Lizenz am " + Datum(t.GueltigBis) + " abgelaufen — Kulanzfenster läuft, bitte verlängern.";
                case LizenzStatus.NachpruefungFaellig:
                    return "Online-Nachprüfung fällig — bitte einmal mit Internetverbindung starten.";
                case LizenzStatus.Lesemodus:
                    return "Lizenz abgelaufen — Lesemodus (Projekte ansehen und exportieren).";
                case LizenzStatus.UhrManipuliert:
                    return "Die Systemuhr wurde zurückgestellt — bitte Uhrzeit korrigieren oder online nachprüfen.";
                default:
                    return "Nicht aktiviert — Testversion oder Lizenzschlüssel unter Administration → Lizenz.";
            }
        }

        /// <summary>
        /// Dürfen neue Arbeitsergebnisse erzeugt werden (Simulation, neue
        /// Projekte, Änderungen)? Im Lesemodus bleibt Ansehen/Exportieren möglich.
        /// </summary>
        public static bool DarfSchreiben()
        {
            LizenzStatus s = Pruefe();
            return s == LizenzStatus.Gueltig
                || s == LizenzStatus.Kulanz
                || s == LizenzStatus.NachpruefungFaellig;
        }

        // ------------------------------------------------------------------
        //  Aktivierung / Nachprüfung / Freigabe
        // ------------------------------------------------------------------

        /// <summary>Mit Lizenzschlüssel und E-Mail online aktivieren.</summary>
        public static async Task<LizenzServerAntwort> Aktivieren(string schluessel, string email)
        {
            LizenzServerAntwort antwort = await new LizenzServerClient().Aktivieren(schluessel, email).ConfigureAwait(false);
            if (antwort.Ok && antwort.TokenJson != null)
            {
                string fehler;
                LizenzToken token = LizenzToken.Laden(antwort.TokenJson, out fehler);
                if (token == null)
                {
                    antwort.Ok = false;
                    antwort.Meldung = fehler;
                    return antwort;
                }
                TokenSpeichern(token);
            }
            return antwort;
        }

        /// <summary>
        /// Lizenzschlüssel und E-Mail aus einer Lizenzdatei (.lic) lesen.
        /// Der Dialog übernimmt die Werte in die Eingabefelder; die eigentliche
        /// Aktivierung läuft anschließend online über <see cref="Aktivieren"/>.
        /// </summary>
        public static void LicDateiLesen(string licPfad, out string schluessel, out string email)
        {
            schluessel = null; email = null;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(licPfad, Encoding.UTF8));
                JsonElement wurzel = doc.RootElement;
                if (wurzel.TryGetProperty("format", out JsonElement fmt) && fmt.GetString() == "epos-signiert-1")
                {
                    byte[] nutzdaten = Convert.FromBase64String(wurzel.GetProperty("nutzdaten").GetString() ?? "");
                    using JsonDocument innen = JsonDocument.Parse(Encoding.UTF8.GetString(nutzdaten));
                    JsonElement lic = innen.RootElement;
                    if (lic.TryGetProperty("schluessel", out JsonElement s)) schluessel = s.GetString();
                    if (lic.TryGetProperty("email", out JsonElement m)) email = m.GetString();
                }
            }
            catch { /* Dialog zeigt eine Meldung, wenn nichts gefunden wurde */ }
        }

        /// <summary>
        /// Stille Nachprüfung im Hintergrund (Aufruf z. B. beim Programmstart).
        /// Erneuert das Token, wenn die letzte Prüfung länger zurückliegt;
        /// Fehler bleiben bewusst folgenlos, solange die Karenzzeit läuft.
        /// </summary>
        public static async Task NachpruefungImHintergrund()
        {
            try
            {
                TokenLaden();
                if (_token == null || _token.TokenId == null) return;

                bool faellig = true;
                if (_token.Ausgestellt.HasValue)
                    faellig = (DateTimeOffset.UtcNow - _token.Ausgestellt.Value).TotalDays >= NACHPRUEFUNG_ALLE_TAGE;
                if (!faellig) return;

                LizenzServerAntwort antwort = await new LizenzServerClient().Nachpruefen(_token.TokenId).ConfigureAwait(false);
                if (antwort.Ok && antwort.TokenJson != null)
                {
                    string fehler;
                    LizenzToken frisch = LizenzToken.Laden(antwort.TokenJson, out fehler);
                    if (frisch != null) TokenSpeichern(frisch);
                }
                else if (!antwort.Ok && !antwort.NetzwerkFehler)
                {
                    // Der Server hat das Token ausdrücklich abgelehnt
                    // (Lizenz gesperrt, Gerät deaktiviert, Benutzer entfernt):
                    // lokales Token verwerfen.
                    TokenLoeschen();
                }
            }
            catch { /* Hintergrundlauf darf die Anwendung nie stören */ }
        }

        /// <summary>Dieses Gerät von der Lizenz lösen (Platz freigeben).</summary>
        public static async Task<LizenzServerAntwort> Freigeben()
        {
            TokenLaden();
            if (_token == null)
                return new LizenzServerAntwort { Ok = true, Meldung = "Es ist keine Lizenz gespeichert." };

            LizenzServerAntwort antwort = await new LizenzServerClient().Deaktivieren(_token.TokenId).ConfigureAwait(false);
            if (antwort.Ok || !antwort.NetzwerkFehler)
                TokenLoeschen();
            return antwort;
        }

        // ------------------------------------------------------------------
        //  Ablage (DPAPI) und Zeitanker
        // ------------------------------------------------------------------

        private static void TokenLaden()
        {
            lock (_sperre)
            {
                if (_geladen) return;
                _geladen = true;
                try
                {
                    // nurDiesesGeraet: true = Geraetebereich (DPAPI LocalMachine). NUR so
                    // gilt eine einmal aktivierte Lizenz fuer alle Windows-Konten
                    // desselben Rechners. Wird der Bereich je umgestellt, ist jede
                    // installierte Lizenz entwertet - der Inhalt ist dann nicht mehr zu
                    // entschluesseln.
                    byte[] klartext = Dienste.Lizenzablage.Lesen(TOKEN_ABLAGE, true);
                    if (klartext == null) return;

                    string fehler;
                    _token = LizenzToken.Laden(Encoding.UTF8.GetString(klartext), out fehler);
                }
                catch { _token = null; }
            }
        }

        private static void TokenSpeichern(LizenzToken token)
        {
            lock (_sperre)
            {
                byte[] klartext = Encoding.UTF8.GetBytes(token.RohJson);
                Dienste.Lizenzablage.Schreiben(TOKEN_ABLAGE, klartext, true);
                _token = token;
                _geladen = true;
            }
        }

        private static void TokenLoeschen()
        {
            lock (_sperre)
            {
                try { Dienste.Lizenzablage.Loeschen(TOKEN_ABLAGE); } catch { }
                _token = null;
                _geladen = true;
            }
        }

        /// <summary>Höchsten je gesehenen Tag lesen (Datei und Registry, Maximum).</summary>
        private static DateTime AnkerLesen()
        {
            DateTime wert = DateTime.MinValue;
            try
            {
                byte[] roh = Dienste.Lizenzablage.Lesen(ANKER_ABLAGE, true);
                if (roh != null && long.TryParse(Encoding.UTF8.GetString(roh), out long ticks))
                    wert = new DateTime(ticks, DateTimeKind.Utc).Date;
            }
            catch { }
            try
            {
                if (long.TryParse(Dienste.Einstellungen.Lies(REGISTRY_ANKER), out long ticks2))
                {
                    DateTime reg = new DateTime(ticks2, DateTimeKind.Utc).Date;
                    if (reg > wert) wert = reg;
                }
            }
            catch { }
            return wert;
        }

        private static void AnkerSchreiben(DateTime heuteUtc)
        {
            if (heuteUtc <= AnkerLesen()) return; // monoton: nie zurückschreiben
            try
            {
                byte[] roh = Encoding.UTF8.GetBytes(heuteUtc.Ticks.ToString());
                Dienste.Lizenzablage.Schreiben(ANKER_ABLAGE, roh, true);
            }
            catch { }
            try
            {
                Dienste.Einstellungen.Schreib(REGISTRY_ANKER, heuteUtc.Ticks.ToString());
            }
            catch { }
        }

        private static string Datum(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy") : "-";
    }
}
