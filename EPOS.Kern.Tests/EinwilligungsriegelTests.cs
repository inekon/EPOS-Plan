using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Einwilligungsriegel nach iU9-W15b.0b — Zeuge T-8, Nachweis P-1.
    ///
    /// <para><b>Was hier bewiesen wird.</b> „Ohne Einwilligung geht nichts hinaus."
    /// Das ist die Zusage des Rechtshinweises, und sie ist bisher nirgends
    /// nachgerechnet worden — die ganze Schutzkette hing an einem <c>Func</c>, den
    /// kein Test je gesetzt hat (Befund W15b-B25). Der Riegel steht in
    /// <c>KiChatService.EinwilligungsriegelAsync</c> VOR allem anderen: vor Cache,
    /// Tageslimit, Schlüsselprüfung und auch vor dem eingespeisten
    /// <c>Modellkanal</c> (Regel S-4). Genau deshalb lässt er sich OHNE NETZ prüfen:
    /// Ist der Prüfkanal gesetzt und wird trotzdem nicht gerufen, hat der Riegel
    /// gehalten.</para>
    ///
    /// <para><b>Warum die Sammlung „Dienste".</b> <see cref="Dienste.Einstellungen"/>
    /// ist prozessweiter Zustand, und <see cref="KiEinwilligung"/> liest jede Angabe
    /// von dort. Ohne die Sammlung führe xunit diese Klasse neben
    /// <c>DiensteTests</c> und <c>EnergieeinheitTests</c>, die denselben Dienst
    /// tauschen.</para>
    ///
    /// <para>Jeder Fall stellt Dienst, Haken, Prüfkanal und Abschalter im
    /// <c>finally</c> wieder her.</para>
    /// </summary>
    [Collection("Dienste")]
    public class EinwilligungsriegelTests
    {
        /// <summary>
        /// Flüchtige Einstellungen, deren MASCHINENWEITE Sicht sich setzen lässt.
        /// <see cref="FluechtigeEinstellungen"/> liefert dort immer die Vorgabe — ein
        /// maschinenweiter Abschalter wäre damit nicht darstellbar.
        /// </summary>
        private sealed class PruefEinstellungen : IEinstellungen
        {
            private readonly Dictionary<string, string> _werte =
                new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>Die maschinenweite Sicht (HKLM); <c>null</c> = nichts gesetzt.</summary>
            public string Maschine { get; set; }

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

            public string LiesMaschine(string schluessel, string vorgabe = null)
                => Maschine ?? vorgabe;
        }

        /// <summary>
        /// Führt einen Fall mit eigener Ablage, eigenem Haken und gesetztem Prüfkanal
        /// aus und stellt danach alles zurück. Der Zähler sagt, wie oft das „Modell"
        /// gerufen wurde — im Betrieb wäre das ein HTTPS-Aufruf.
        /// </summary>
        private static async Task<(KiAntwort Antwort, int Modellaufrufe)> Fahren(
            PruefEinstellungen ablage, Func<Task<bool>> nachfragen)
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            Func<Task<bool>> hakenVorher = KiEinwilligung.Nachfragen;
            Func<string, string, CancellationToken, Task<string>> kanalVorher =
                KiChatService.Modellkanal;

            int aufrufe = 0;
            try
            {
                Dienste.Einstellungen = ablage;
                KiEinwilligung.Nachfragen = nachfragen;
                KiChatService.Modellkanal = (prompt, modell, abbruch) =>
                {
                    aufrufe++;
                    return Task.FromResult("{}");
                };

                KiAntwort antwort = await KiChatService.FrageAsync("Wie lege ich ein Projekt an?",
                                                                   "Bereich: Projekt");
                return (antwort, aufrufe);
            }
            finally
            {
                KiChatService.Modellkanal = kanalVorher;
                KiEinwilligung.Nachfragen = hakenVorher;
                Dienste.Einstellungen = vorher;
            }
        }

        // ==================================================================
        //  P-1 — ohne Einwilligung kein Modellaufruf
        // ==================================================================

        /// <summary>
        /// <b>Der Kernnachweis.</b> Kein Haken eingehängt — genau die Lage des
        /// Aktionsharnischs, des Referenzlaufs und jedes Konsolenwerkzeugs. Der Riegel
        /// hält, der Prüfkanal bleibt unberührt.
        /// </summary>
        [Fact]
        public async Task Ohne_eingehaengte_Nachfrage_entsteht_kein_Modellaufruf()
        {
            var (antwort, aufrufe) = await Fahren(new PruefEinstellungen(), null);

            Assert.Equal(0, aufrufe);
            Assert.False(antwort.Erfolg);
            Assert.Equal(Resource.KI_HINWEIS_ABGELEHNT, antwort.Fehler);
        }

        /// <summary>Lehnt der Anwender ab, geht ebenfalls nichts hinaus.</summary>
        [Fact]
        public async Task Abgelehnte_Nachfrage_entsteht_kein_Modellaufruf()
        {
            var (antwort, aufrufe) = await Fahren(new PruefEinstellungen(),
                                                  () => Task.FromResult(false));

            Assert.Equal(0, aufrufe);
            Assert.Equal(Resource.KI_HINWEIS_ABGELEHNT, antwort.Fehler);
        }

        /// <summary>
        /// Wirft der Haken, gilt das als Ablehnung — nicht als Zustimmung. Eine
        /// Ausnahme in der Oberfläche darf nie zu einer Übertragung führen.
        /// </summary>
        [Fact]
        public async Task Werfende_Nachfrage_gilt_als_Ablehnung()
        {
            var (antwort, aufrufe) = await Fahren(
                new PruefEinstellungen(),
                () => throw new InvalidOperationException("Fenster ging nicht auf"));

            Assert.Equal(0, aufrufe);
            Assert.Equal(Resource.KI_HINWEIS_ABGELEHNT, antwort.Fehler);
        }

        /// <summary>
        /// Sagt der Anwender ja, wird die Einwilligung GEMERKT (Fassung und Zeitpunkt)
        /// und der Riegel lässt durch — er ist keine Dauersperre.
        /// </summary>
        /// <remarks>
        /// Dass die Frage danach am FEHLENDEN SCHLÜSSEL scheitert, ist der Beweis, dass
        /// der Riegel passiert wurde: Die Prüfung auf <c>IstEingerichtet</c> steht
        /// unmittelbar dahinter (<c>KiChatService.FrageAsync</c>). Der Prüfkanal wird im
        /// Hilfefall bewusst NICHT als Ersatz für einen Schlüssel gewertet — nur die
        /// Werkzeugrunde (<c>FrageMitAktionenAsync</c>) lässt ihn dort einspringen,
        /// damit sich der Aktionsbetrieb ohne Netz durchspielen lässt (T-9).
        /// </remarks>
        [Fact]
        public async Task Erteilte_Einwilligung_wird_gemerkt_und_laesst_durch()
        {
            var ablage = new PruefEinstellungen();
            int gefragt = 0;

            var (antwort, aufrufe) = await Fahren(ablage, () =>
            {
                gefragt++;
                return Task.FromResult(true);
            });

            Assert.Equal(1, gefragt);
            Assert.Equal(0, aufrufe);
            Assert.NotEqual(Resource.KI_HINWEIS_ABGELEHNT, antwort.Fehler);
            Assert.NotEqual(Resource.KI_ABSCHALTER_MELDUNG, antwort.Fehler);
            Assert.Equal(KiEinwilligung.FASSUNG.ToString(CultureInfo.InvariantCulture),
                         ablage.Lies("KiHinweisBestaetigt"));
            Assert.NotEmpty(ablage.Lies("KiHinweisBestaetigtAm", ""));
        }

        // ==================================================================
        //  Der Abschalter — HKCU und HKLM
        // ==================================================================

        /// <summary>
        /// Der benutzerbezogene Abschalter steht VOR der Einwilligung: Es wird gar
        /// nicht erst gefragt, und die Meldung ist die des Abschalters.
        /// </summary>
        [Fact]
        public async Task Benutzerabschalter_fragt_nicht_einmal()
        {
            var ablage = new PruefEinstellungen();
            ablage.Schreib("KiDeaktiviert", "1");
            int gefragt = 0;

            var (antwort, aufrufe) = await Fahren(ablage, () =>
            {
                gefragt++;
                return Task.FromResult(true);
            });

            Assert.Equal(0, gefragt);
            Assert.Equal(0, aufrufe);
            Assert.Equal(Resource.KI_ABSCHALTER_MELDUNG, antwort.Fehler);
        }

        /// <summary>
        /// Der MASCHINENWEITE Abschalter überstimmt selbst eine gültige Einwilligung —
        /// er ist aus der Anwendung heraus nicht zu lösen.
        /// </summary>
        [Fact]
        public async Task Maschinenabschalter_ueberstimmt_die_Einwilligung()
        {
            var ablage = new PruefEinstellungen { Maschine = "1" };
            ablage.Schreib("KiHinweisBestaetigt",
                           KiEinwilligung.FASSUNG.ToString(CultureInfo.InvariantCulture));

            var (antwort, aufrufe) = await Fahren(ablage, () => Task.FromResult(true));

            Assert.Equal(0, aufrufe);
            Assert.Equal(Resource.KI_ABSCHALTER_MELDUNG, antwort.Fehler);
        }

        // ==================================================================
        //  Die Fassung — eine alte Einwilligung genuegt nicht
        // ==================================================================

        /// <summary>
        /// Fassung 1 &lt; 2: Wer den ALTEN Hinweis bestätigt hat, wird erneut gefragt.
        /// Das ist der Grund, warum <c>FASSUNG</c> überhaupt existiert — die Fassung 1
        /// sprach von „ausschließlich lesenden Aktionen".
        /// </summary>
        [Fact]
        public async Task Alte_Fassung_wird_erneut_gefragt()
        {
            var ablage = new PruefEinstellungen();
            ablage.Schreib("KiHinweisBestaetigt", "1");
            int gefragt = 0;

            await Fahren(ablage, () =>
            {
                gefragt++;
                return Task.FromResult(false);
            });

            Assert.Equal(2, KiEinwilligung.FASSUNG);
            Assert.Equal(1, gefragt);
        }

        /// <summary>
        /// Die aktuelle Fassung wird NICHT erneut gefragt — der Hinweis erscheint
        /// einmal, nicht bei jeder Frage.
        /// </summary>
        [Fact]
        public async Task Aktuelle_Fassung_wird_nicht_erneut_gefragt()
        {
            var ablage = new PruefEinstellungen();
            ablage.Schreib("KiHinweisBestaetigt",
                           KiEinwilligung.FASSUNG.ToString(CultureInfo.InvariantCulture));
            int gefragt = 0;

            var (antwort, _) = await Fahren(ablage, () =>
            {
                gefragt++;
                return Task.FromResult(true);
            });

            Assert.Equal(0, gefragt);
            Assert.NotEqual(Resource.KI_HINWEIS_ABGELEHNT, antwort.Fehler);
        }

        // ==================================================================
        //  Die synchrone Fassade (R-W15b-5)
        // ==================================================================

        /// <summary>
        /// <see cref="KiEinwilligung.Sicherstellen"/> fragt seit iU9-W15b.0b NICHT mehr
        /// nach: Sie liefert <c>false</c>, wenn nur der asynchrone Haken gesetzt ist.
        /// Ein blockierendes Warten würde in einer WebView den Renderer verklemmen, der
        /// die Überlagerung erst noch zeichnen müsste.
        /// </summary>
        [Fact]
        public void Synchrone_Fassade_fragt_nicht_nach()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            Func<Task<bool>> hakenVorher = KiEinwilligung.Nachfragen;
            int gefragt = 0;
            try
            {
                Dienste.Einstellungen = new PruefEinstellungen();
                KiEinwilligung.Nachfragen = () =>
                {
                    gefragt++;
                    return Task.FromResult(true);
                };

                Assert.False(KiEinwilligung.Sicherstellen());
                Assert.Equal(0, gefragt);
            }
            finally
            {
                KiEinwilligung.Nachfragen = hakenVorher;
                Dienste.Einstellungen = vorher;
            }
        }

        /// <summary>Liegt die Einwilligung schon vor, sagt die Fassade sofort ja.</summary>
        [Fact]
        public void Synchrone_Fassade_erkennt_die_vorhandene_Einwilligung()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                var ablage = new PruefEinstellungen();
                ablage.Schreib("KiHinweisBestaetigt",
                               KiEinwilligung.FASSUNG.ToString(CultureInfo.InvariantCulture));
                Dienste.Einstellungen = ablage;

                Assert.True(KiEinwilligung.Sicherstellen());
            }
            finally { Dienste.Einstellungen = vorher; }
        }
    }
}
