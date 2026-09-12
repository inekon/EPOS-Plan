using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using KiKern;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Werkzeugrunde über den Prüfkanal — Zeuge T-9, Nachweise P-2 und P-3
    /// (iU9-W15b.0i).
    ///
    /// <para><b>Warum das geprüft wird.</b> <c>KiChatService.Modellkanal</c> ist seit
    /// Etappe 2 gebaut, aber im ganzen Repo hat ihn nie ein Prüfling belegt (Befund
    /// W15b-B25). Damit war die ganze Werkzeugrunde — Absichtserkennung, Riegel,
    /// Bestätigung, Ausführung, Protokollzeile, Rundendeckel — nicht nachrechenbar,
    /// obwohl sie es ohne Netz sein könnte: Der Kanal liefert genau den Antwortrumpf,
    /// den sonst der Anbieter liefert, und ersetzt <b>nur</b> den Transport.</para>
    ///
    /// <para><b>Kein Netz, kein Schlüssel, keine gezählte Anfrage.</b> Ist der Kanal
    /// gesetzt, gilt der Lauf als „eingespeist": Weder wird ein API-Schlüssel verlangt
    /// noch der Tageszähler erhöht. Genau darauf zielt Fachkonzept 8/Etappe 2 — „die
    /// Modellanbindung selbst wird NICHT automatisiert getestet", alles davor und
    /// dahinter schon.</para>
    ///
    /// <para>Die Fälle laufen gegen ein EIGENES Register mit zwei Aktionen (lesend und
    /// schreibend), nicht gegen das der Anwendung: Der Kern soll ohne die Anwendung
    /// prüfbar sein, und die Fälle sollen sich nicht ändern, wenn dem echten Register
    /// eine Aktion zuwächst (dieselbe Begründung wie in <c>KiKern.Tests</c>).</para>
    ///
    /// <para>Die Klasse tauscht <c>Dienste.Einstellungen</c> und steht darum in der
    /// einen seriellen Sammlung „Testdatenbank" (Befund iU5‑O‑1, 06.09.2026; vorher
    /// „Dienste" — zwei verschiedene Sammlungen laufen in xunit nebeneinander).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ModellkanalTests
    {
        // ==================================================================
        //  Prueflinge
        // ==================================================================

        /// <summary>Einstellungen im Arbeitsspeicher, mit erteilter Einwilligung.</summary>
        private sealed class PruefEinstellungen : IEinstellungen
        {
            private readonly Dictionary<string, string> _werte =
                new Dictionary<string, string>(StringComparer.Ordinal);

            internal PruefEinstellungen()
            {
                // Ohne Einwilligung entsteht gar keine Werkzeugrunde (Riegel, T-8).
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

        /// <summary>
        /// Eine Ausführungsschicht ohne Datenbank: Sie merkt sich, was ausgeführt wurde,
        /// und liefert eine feste Vorschau. Die echte
        /// (<c>KiAusfuehrungAdapter</c> über <c>KiAusfuehrer</c>) liegt in der
        /// Windows-Anwendung und hängt an Controllern und Formularen.
        /// </summary>
        private sealed class PruefAusfuehrung : IKiAusfuehrung
        {
            private readonly KiRegister _register;

            internal PruefAusfuehrung(KiRegister register) { _register = register; }

            /// <summary>Die Namen der tatsächlich ausgeführten Aufrufe, in Reihenfolge.</summary>
            internal List<string> Ausgefuehrt { get; } = new List<string>();

            /// <summary>Die Namen der eingelösten Freigaben (bestätigungspflichtiger Weg).</summary>
            internal List<string> MitFreigabe { get; } = new List<string>();

            /// <summary>Die abgewiesenen Aufrufe samt Grund.</summary>
            internal List<string> Abgewiesen { get; } = new List<string>();

            public KiRegister Register => _register;

            public string LetzteProtokollzeile { get; private set; } = "";

            public Task<KiVorbereitung> VorbereitenAsync(KiAufruf aufruf, CancellationToken abbruch)
            {
                KiFreigabe freigabe = KiFreigabe.Erzeuge(
                    aufruf, "Ich würde " + aufruf.Name + " ausführen.");
                return Task.FromResult(new KiVorbereitung(freigabe, null));
            }

            public Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, CancellationToken abbruch)
            {
                Ausgefuehrt.Add(aufruf.Name);
                LetzteProtokollzeile = "PROTOKOLL " + aufruf.Name;
                return Task.FromResult(KiErgebnis.Ok("erledigt: " + aufruf.Name, null, 1));
            }

            public Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, KiFreigabe freigabe,
                                                    CancellationToken abbruch)
            {
                MitFreigabe.Add(aufruf.Name);

                // Der Ausfuehrer loest JEDEN Ausgang ein - auch "abgelehnt" und
                // "verfallen" (Fachkonzept 3.5). Der Pruefling bildet das nach: Ohne
                // erteilte Entscheidung wird nichts ausgefuehrt.
                if (freigabe == null || freigabe.Stand != KiEntscheidung.Erteilt)
                {
                    LetzteProtokollzeile = "PROTOKOLL abgelehnt " + aufruf.Name;
                    return Task.FromResult(KiErgebnis.Abgelehnt("nicht bestätigt"));
                }

                Ausgefuehrt.Add(aufruf.Name);
                LetzteProtokollzeile = "PROTOKOLL " + aufruf.Name;
                return Task.FromResult(KiErgebnis.Ok("erledigt: " + aufruf.Name, null, 1));
            }

            public KiErgebnis AbweisenUndVermerken(KiAufruf aufruf, string grund)
            {
                Abgewiesen.Add((aufruf == null ? "" : aufruf.Name) + ": " + grund);
                LetzteProtokollzeile = "PROTOKOLL abgewiesen";
                return KiErgebnis.Abgelehnt(grund ?? "");
            }

            public void KlarnamenAnmelden(KiPlatzhalter platzhalter, params string[] texte) { }
        }

        // ==================================================================
        //  Register und Antwortruempfe
        // ==================================================================

        private const string LESEN = "projekte_auflisten";
        private const string SCHREIBEN = "projekt_umbenennen";

        private static KiRegister Register()
        {
            return new KiRegister()
                .Aufnehmen(new KiAktion(
                    name: LESEN,
                    zweck: "Listet alle Projekte der Datenbank.",
                    stufe: Schutzstufe.Lesen,
                    andockpunkt: "ProjektCtrl.ReadAll",
                    ausfuehren: _ => KiErgebnis.Ok("2 Projekte", null, 2)))
                .Aufnehmen(new KiAktion(
                    name: SCHREIBEN,
                    zweck: "Benennt ein Projekt um.",
                    stufe: Schutzstufe.Schreiben,
                    andockpunkt: "ProjektCtrl.Rename",
                    parameter: new[]
                    {
                        new KiParameter("projekt_id", KiParameterTyp.Ganzzahl,
                                        "Schlüssel des Projekts.", anzeigename: "Projekt (ID)", min: 1)
                    },
                    ausfuehren: a => KiErgebnis.Ok("umbenannt"),
                    vorschau: a => "Ich würde Projekt " + a.Id("projekt_id") + " umbenennen.",
                    wirkung: "Das Projekt heißt danach anders.",
                    umkehrbar: true));
        }

        /// <summary>Ein Anbieterrumpf mit genau einem Werkzeugaufruf (Weg A).</summary>
        private static string RumpfMitAufruf(string aktion, string argumenteJson)
        {
            var teile = new JsonArray
            {
                new JsonObject
                {
                    ["functionCall"] = new JsonObject
                    {
                        ["name"] = aktion,
                        ["args"] = JsonNode.Parse(argumenteJson)
                    }
                }
            };
            return Rumpf(teile);
        }

        /// <summary>Ein Anbieterrumpf ohne Werkzeugaufruf - reine Auskunft.</summary>
        private static string RumpfNurText(string text)
            => Rumpf(new JsonArray { new JsonObject { ["text"] = text } });

        private static string Rumpf(JsonArray teile)
        {
            var wurzel = new JsonObject
            {
                ["candidates"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["content"] = new JsonObject { ["role"] = "model", ["parts"] = teile },
                        ["finishReason"] = "STOP"
                    }
                }
            };
            return wurzel.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        // ==================================================================
        //  Der Lauf
        // ==================================================================

        /// <summary>
        /// Fährt eine Werkzeugrunde ohne Netz. <paramref name="antworten"/> liefert je
        /// Runde einen Anbieterrumpf; <paramref name="entscheidung"/> ist der
        /// Bestätigungsweg (<c>null</c> = es gibt niemanden zu fragen).
        /// </summary>
        private static async Task<(KiAntwort Antwort, PruefAusfuehrung Schicht, int Runden)> Fahren(
            IReadOnlyList<string> antworten,
            KiEntscheidung? entscheidung,
            bool ohneBestaetigungsweg = false)
        {
            IEinstellungen einstellungenVorher = Dienste.Einstellungen;
            IKiAusfuehrung schichtVorher = KiAusfuehrungsweg.Aktuell;
            Func<string, string, CancellationToken, Task<string>> kanalVorher = KiChatService.Modellkanal;
            KiBestaetigungsfrage wegVorher = KiChatService.Bestaetigungsweg;

            KiRegister register = Register();
            var schicht = new PruefAusfuehrung(register);
            int runden = 0;

            try
            {
                Dienste.Einstellungen = new PruefEinstellungen();
                KiAusfuehrungsweg.Aktuell = schicht;

                KiChatService.Modellkanal = (anfrage, modell, abbruch) =>
                {
                    string rumpf = antworten[Math.Min(runden, antworten.Count - 1)];
                    runden++;
                    return Task.FromResult(rumpf);
                };

                KiChatService.Bestaetigungsweg = ohneBestaetigungsweg
                    ? null
                    : (freigabe, abbruch) => Task.FromResult(entscheidung ?? KiEntscheidung.Abgelehnt);

                KiAntwort antwort = await KiChatService.FrageMitAktionenAsync(
                    "Bitte alle Projekte auflisten.", "Bereich: Projektverwaltung",
                    register: register);

                return (antwort, schicht, runden);
            }
            finally
            {
                KiChatService.Bestaetigungsweg = wegVorher;
                KiChatService.Modellkanal = kanalVorher;
                KiAusfuehrungsweg.Aktuell = schichtVorher;
                Dienste.Einstellungen = einstellungenVorher;
            }
        }

        // ==================================================================
        //  P-2 - die Werkzeugrunde ohne Netz
        // ==================================================================

        /// <summary>
        /// <b>Der Kernnachweis P-2.</b> Eine LESENDE Aktion läuft ganz durch: Der Kanal
        /// liefert den Werkzeugaufruf, die Absichtserkennung findet ihn, der Riegel
        /// lässt Stufe „Lesen" durch, die Ausführungsschicht führt aus, und die
        /// Protokollzeile steht im Schritt. Ohne Netz, ohne Schlüssel.
        /// </summary>
        [Fact]
        public async Task Lesende_Aktion_laeuft_ohne_Netz_ganz_durch()
        {
            var (antwort, schicht, _) = await Fahren(
                new[] { RumpfMitAufruf(LESEN, "{}"), RumpfNurText("Das waren zwei Projekte.") },
                KiEntscheidung.Erteilt);

            Assert.True(antwort.Erfolg, antwort.Fehler);
            Assert.Equal(new[] { LESEN }, schicht.Ausgefuehrt);
            Assert.Single(antwort.Schritte);
            Assert.True(antwort.Schritte[0].Ausgefuehrt);
            Assert.Equal("PROTOKOLL " + LESEN, antwort.Schritte[0].Protokollzeile);

            // Lesen ist nicht bestaetigungspflichtig - es wurde keine Freigabe eingeloest.
            Assert.Empty(schicht.MitFreigabe);
        }

        /// <summary>
        /// Eine reine Auskunft ohne Werkzeugaufruf beendet den Lauf sofort — eine Runde,
        /// keine Ausführung.
        /// </summary>
        [Fact]
        public async Task Reine_Auskunft_fuehrt_nichts_aus()
        {
            var (antwort, schicht, runden) = await Fahren(
                new[] { RumpfNurText("Dazu brauche ich keine Aktion.") },
                KiEntscheidung.Erteilt);

            Assert.True(antwort.Erfolg, antwort.Fehler);
            Assert.Empty(schicht.Ausgefuehrt);
            Assert.Empty(antwort.Schritte);
            Assert.Equal(1, runden);
        }

        /// <summary>
        /// <b>Der Rundendeckel greift</b> (Fachkonzept 3.3, Festlegung 5). Antwortet das
        /// Modell endlos mit Werkzeugaufrufen, endet der Lauf nach drei Runden — sonst
        /// könnte eine Schleife beliebig viele Anfragen und beliebig viele Aktionen
        /// auslösen.
        /// </summary>
        [Fact]
        public async Task Rundendeckel_beendet_eine_Schleife()
        {
            var (antwort, schicht, runden) = await Fahren(
                new[] { RumpfMitAufruf(LESEN, "{}") },   // immer derselbe Aufruf
                KiEntscheidung.Erteilt);

            Assert.True(antwort.Deckel);
            Assert.Equal(3, runden);
            Assert.Equal(3, schicht.Ausgefuehrt.Count);
        }

        // ==================================================================
        //  P-3 - die vier Ausgaenge der Bestaetigung
        // ==================================================================

        /// <summary>
        /// Ausgang 1: <b>Erteilt</b> — die Freigabe wird eingelöst und die Aktion läuft.
        /// </summary>
        [Fact]
        public async Task Bestaetigung_erteilt_fuehrt_aus()
        {
            var (antwort, schicht, _) = await Fahren(
                new[] { RumpfMitAufruf(SCHREIBEN, "{\"projekt_id\":42}"), RumpfNurText("Fertig.") },
                KiEntscheidung.Erteilt);

            Assert.Equal(new[] { SCHREIBEN }, schicht.MitFreigabe);
            Assert.Equal(new[] { SCHREIBEN }, schicht.Ausgefuehrt);
            Assert.Single(antwort.Schritte);
            Assert.True(antwort.Schritte[0].Bestaetigungspflichtig);
            Assert.Equal(KiEntscheidung.Erteilt, antwort.Schritte[0].Entscheidung);
            Assert.True(antwort.Schritte[0].Ausgefuehrt);
        }

        /// <summary>
        /// Ausgänge 2 bis 4: <b>Abgelehnt</b>, <b>Verfallen</b>, <b>Abgebrochen</b> — der
        /// Ausführer wird in jedem Fall gerufen (er protokolliert), führt aber NICHTS
        /// aus. Das ist die Zusage des Fachkonzepts 3.5: „Jeder Ausgang geht durch den
        /// Ausführer", damit keine Entscheidung unprotokolliert bleibt.
        /// </summary>
        [Theory]
        [InlineData(KiEntscheidung.Abgelehnt)]
        [InlineData(KiEntscheidung.Verfallen)]
        [InlineData(KiEntscheidung.Abgebrochen)]
        public async Task Bestaetigung_ohne_Ja_fuehrt_nichts_aus(KiEntscheidung entscheidung)
        {
            var (antwort, schicht, _) = await Fahren(
                new[] { RumpfMitAufruf(SCHREIBEN, "{\"projekt_id\":42}"), RumpfNurText("Fertig.") },
                entscheidung);

            Assert.Equal(new[] { SCHREIBEN }, schicht.MitFreigabe);
            Assert.Empty(schicht.Ausgefuehrt);
            Assert.Single(antwort.Schritte);
            Assert.Equal(entscheidung, antwort.Schritte[0].Entscheidung);
            Assert.False(antwort.Schritte[0].Ausgefuehrt);
        }

        /// <summary>
        /// <b>Ohne Bestätigungsweg läuft KEINE Schreibaktion</b> — kein Chatfenster, kein
        /// Anwender, keine Änderung. Die Vorbereitung wird gar nicht erst angestoßen, es
        /// entsteht also auch kein Sicherungspunkt für etwas, das ohnehin nicht laufen
        /// kann.
        /// </summary>
        [Fact]
        public async Task Ohne_Bestaetigungsweg_laeuft_keine_Schreibaktion()
        {
            var (antwort, schicht, _) = await Fahren(
                new[] { RumpfMitAufruf(SCHREIBEN, "{\"projekt_id\":42}"), RumpfNurText("Fertig.") },
                null, ohneBestaetigungsweg: true);

            Assert.Empty(schicht.MitFreigabe);
            Assert.Empty(schicht.Ausgefuehrt);
            Assert.Single(schicht.Abgewiesen);
            Assert.Single(antwort.Schritte);
            Assert.Equal(KiEntscheidung.Abgelehnt, antwort.Schritte[0].Entscheidung);
            Assert.False(antwort.Schritte[0].Ausgefuehrt);
        }

        // ==================================================================
        //  Was der Kanal NICHT ersetzt
        // ==================================================================

        /// <summary>
        /// Der Kanal ersetzt den Transport, nicht die Prüfung: Ein unbekannter
        /// Aktionsname wird abgelehnt, das Modell bekommt den Klartextgrund zurück und
        /// darf nachbessern — ausgeführt wird nichts.
        /// </summary>
        [Fact]
        public async Task Unbekannte_Aktion_wird_abgelehnt()
        {
            var (antwort, schicht, _) = await Fahren(
                new[] { RumpfMitAufruf("gibt_es_nicht", "{}"), RumpfNurText("Verstanden.") },
                KiEntscheidung.Erteilt);

            Assert.Empty(schicht.Ausgefuehrt);
            Assert.Single(antwort.Schritte);
            Assert.False(antwort.Schritte[0].Ausgefuehrt);
            Assert.False(string.IsNullOrEmpty(antwort.Schritte[0].Grund));
        }

        /// <summary>
        /// <b>Der Riegel bleibt vor dem Kanal</b> (Regel S-4). Ist die KI abgeschaltet,
        /// wird der Prüfkanal nicht einmal gerufen — auch nicht im Prüflauf.
        /// </summary>
        [Fact]
        public async Task Abschalter_erreicht_den_Kanal_nicht()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            Func<string, string, CancellationToken, Task<string>> kanalVorher = KiChatService.Modellkanal;
            int aufrufe = 0;
            try
            {
                var ablage = new PruefEinstellungen();
                ablage.Schreib("KiDeaktiviert", "1");
                Dienste.Einstellungen = ablage;
                KiChatService.Modellkanal = (a, m, t) =>
                {
                    aufrufe++;
                    return Task.FromResult(RumpfNurText("nie"));
                };

                KiAntwort antwort = await KiChatService.FrageMitAktionenAsync(
                    "Bitte alle Projekte auflisten.", "Bereich: Projektverwaltung",
                    register: Register());

                Assert.Equal(0, aufrufe);
                Assert.Equal(Resource.KI_ABSCHALTER_MELDUNG, antwort.Fehler);
            }
            finally
            {
                KiChatService.Modellkanal = kanalVorher;
                Dienste.Einstellungen = vorher;
            }
        }

        /// <summary>
        /// <b>Der Kanal zählt keine Anfrage.</b> Ein Prüflauf darf das Tageskontingent
        /// des Anwenders nicht aufbrauchen.
        /// </summary>
        [Fact]
        public async Task Der_Kanal_erhoeht_den_Tageszaehler_nicht()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            IKiAusfuehrung schichtVorher = KiAusfuehrungsweg.Aktuell;
            Func<string, string, CancellationToken, Task<string>> kanalVorher = KiChatService.Modellkanal;
            try
            {
                var ablage = new PruefEinstellungen();
                Dienste.Einstellungen = ablage;
                KiAusfuehrungsweg.Aktuell = new PruefAusfuehrung(Register());
                KiChatService.Modellkanal = (a, m, t) => Task.FromResult(RumpfNurText("fertig"));

                int vorZaehler = KiChatService.AnfragenHeute;

                await KiChatService.FrageMitAktionenAsync(
                    "Bitte alle Projekte auflisten.", "Bereich: Projektverwaltung",
                    register: Register());

                Assert.Equal(vorZaehler, KiChatService.AnfragenHeute);
            }
            finally
            {
                KiChatService.Modellkanal = kanalVorher;
                KiAusfuehrungsweg.Aktuell = schichtVorher;
                Dienste.Einstellungen = vorher;
            }
        }
    }
}
