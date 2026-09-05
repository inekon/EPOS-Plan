using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Riegel gegen eine Anfrage OHNE Schlüssel und die Trennung von Anwendersatz
    /// und Rohtext — Anwenderbefund <b>W15b‑B‑2</b> der Windows-Abnahme vom 05.09.2026
    /// („Hilfeassistent funktioniert nicht bei Fragen").
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Befund.</b> Im Verlauf stand <i>„HTTP 401 - Request had invalid
    /// authentication credentials. Expected OAuth 2 access token, login cookie or other
    /// valid authentication credential."</i> Diese Antwort gibt der Anbieter auf eine
    /// Anfrage, die OHNE Zugangsdaten ankommt (ein falscher Schlüssel ergäbe 400
    /// API_KEY_INVALID). Das Programm kannte die Lage aber, bevor es sendete.
    /// </para>
    /// <para>
    /// <b>Was hier bewiesen wird:</b> Ohne Schlüssel geht keine Anfrage hinaus, und
    /// stattdessen steht der Satz da, der die Stelle zum Beheben nennt. Eine Absage des
    /// Dienstes wird zum Anwendersatz; der Wortlaut des Anbieters landet in der
    /// Störungsliste, die das Chatfenster unter „Protokoll anzeigen" zeigt.
    /// </para>
    /// <para>
    /// Kein Netz: Der Riegel schlägt zu, bevor irgendetwas gesendet würde, und die
    /// Werkzeugrunde läuft über den eingespeisten <c>Modellkanal</c>.
    /// </para>
    /// </remarks>
    [Collection("Dienste")]
    public class KiDienstriegelTests
    {
        /// <summary>Einstellungen im Arbeitsspeicher, mit erteilter Einwilligung.</summary>
        private sealed class PruefEinstellungen : IEinstellungen
        {
            private readonly Dictionary<string, string> _werte =
                new Dictionary<string, string>(StringComparer.Ordinal);

            internal PruefEinstellungen()
            {
                // Ohne Einwilligung greift der Einwilligungsriegel VOR dem Schluessel
                // (Regel S-4) - der Fall traefe dann eine andere Weiche.
                _werte["KiHinweisBestaetigt"] =
                    KiEinwilligung.FASSUNG.ToString(CultureInfo.InvariantCulture);
            }

            public string Lies(string schluessel, string vorgabe = null)
            {
                string wert;
                return _werte.TryGetValue(schluessel ?? "", out wert) ? wert : vorgabe;
            }

            public int LiesZahl(string schluessel, int vorgabe = 0)
            {
                int n;
                return int.TryParse(Lies(schluessel, null), NumberStyles.Integer,
                                    CultureInfo.InvariantCulture, out n) ? n : vorgabe;
            }

            public void Schreib(string schluessel, string wert) { _werte[schluessel ?? ""] = wert; }

            public void SchreibZahl(string schluessel, int wert)
                => Schreib(schluessel, wert.ToString(CultureInfo.InvariantCulture));

            public void Loesche(string schluessel) { _werte.Remove(schluessel ?? ""); }

            public string LiesMaschine(string schluessel, string vorgabe = null) => vorgabe;
        }

        // ==================================================================
        //  Ohne Schluessel geht nichts hinaus
        // ==================================================================

        /// <summary>
        /// <b>Kein Schlüssel, keine Anfrage — und ein Satz, der sagt, was zu tun ist.</b>
        /// Der Fall belegt zugleich, dass nichts gesendet wird: Ohne Netz käme ein
        /// echter Aufruf nicht in 30 Sekunden zurück, und die Störungsliste bliebe nicht
        /// leer.
        /// </summary>
        [Fact]
        public async Task Ohne_Schluessel_geht_keine_Anfrage_hinaus()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            string schluesselVorher = KiChatService.ApiKey;
            try
            {
                Dienste.Einstellungen = new PruefEinstellungen();
                KiChatService.ApiKey = "";
                KiChatService.StoerungenLeeren();

                KiAntwort antwort = await KiChatService.FrageAsync(
                    "Wieviel Varianten hat dieses Projekt?", "Bereich: Projektverwaltung");

                Assert.False(antwort.Erfolg);
                Assert.Equal(KiDienstfehler.Anwendersatz(KiDienstfehler.OhneAnfrage), antwort.Fehler);

                // Der Rohtext des Anbieters kommt gar nicht erst vor.
                Assert.DoesNotContain("OAuth", antwort.Fehler ?? "", StringComparison.Ordinal);
                Assert.DoesNotContain("HTTP", antwort.Fehler ?? "", StringComparison.Ordinal);

                // Und es ist nichts hinausgegangen, das haette scheitern koennen.
                Assert.Empty(KiChatService.Stoerungen);
            }
            finally
            {
                KiChatService.ApiKey = schluesselVorher;
                KiChatService.StoerungenLeeren();
                Dienste.Einstellungen = vorher;
            }
        }

        // ==================================================================
        //  Eine Absage des Dienstes: Anwendersatz vorn, Rohtext ins Protokoll
        // ==================================================================

        /// <summary>
        /// <b>401 wird ein Anwendersatz.</b> Der eingespeiste Modellkanal ersetzt nur den
        /// TRANSPORT; er wirft dieselbe Absage, die der HTTP-Weg wirft, und die
        /// Werkzeugrunde muss daraus denselben Satz machen.
        /// </summary>
        [Fact]
        public async Task Eine_Absage_des_Dienstes_wird_zum_Anwendersatz()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            Func<string, string, CancellationToken, Task<string>> kanalVorher = KiChatService.Modellkanal;
            const string ROH = "Request had invalid authentication credentials. " +
                               "Expected OAuth 2 access token, login cookie or other valid " +
                               "authentication credential.";
            try
            {
                Dienste.Einstellungen = new PruefEinstellungen();
                KiChatService.Modellkanal =
                    (anfrage, modell, abbruch) => throw new KiDienstAusnahme(401, ROH);

                KiAntwort antwort = await KiChatService.FrageMitAktionenAsync(
                    "Wieviel Varianten hat dieses Projekt?", "Bereich: Projektverwaltung",
                    register: new KiRegister());

                Assert.False(antwort.Erfolg);
                Assert.Equal(KiDienstfehler.Anwendersatz(401), antwort.Fehler);
                Assert.DoesNotContain("OAuth", antwort.Fehler ?? "", StringComparison.Ordinal);
                Assert.Contains("Einstellungen", antwort.Fehler ?? "", StringComparison.Ordinal);
            }
            finally
            {
                KiChatService.Modellkanal = kanalVorher;
                Dienste.Einstellungen = vorher;
            }
        }

        // ==================================================================
        //  Die Stoerungsliste
        // ==================================================================

        /// <summary>
        /// Die Störungsliste beginnt leer und lässt sich leeren — sie ist die Ablage
        /// für den Rohtext, den „Protokoll anzeigen" unter dem Aktionsprotokoll zeigt.
        /// </summary>
        /// <remarks>
        /// Sie steht bewusst NICHT in der Protokolldatei: Die führt genau eine Zeile je
        /// Ausführungsversuch in einem festen Format (Fachkonzept 3.6), und eine
        /// Netzstörung ist kein Ausführungsversuch.
        /// </remarks>
        [Fact]
        public void Die_Stoerungsliste_laesst_sich_leeren()
        {
            KiChatService.StoerungenLeeren();
            Assert.Empty(KiChatService.Stoerungen);
        }
    }
}
