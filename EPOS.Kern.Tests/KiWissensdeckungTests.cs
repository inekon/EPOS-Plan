using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Deckung der Wissensbasis: <b>jeder Bedienbereich des Assistenten hat Inhalt
    /// im EINGEBAUTEN Wissen — oder steht mit Grund in der Ausnahmeliste</b>
    /// (Auftrag KI‑1; Aufgabensteuerungskonzept 7.1).
    ///
    /// <para><b>Warum es diesen Wächter gibt.</b> Anwendermeldung vom 19.09.2026: „Der
    /// KI‑Assistent kann die try Daten noch nicht finden bzw. kennt diese nicht." Der
    /// Grund war keine kaputte Suche, sondern eine LÜCKE: Zur Klimadatenbedienung stand
    /// im eingebauten Wissen kein einziger Abschnitt, und die Wiki-Seite dazu ist bis
    /// zum Sammel-Upload nicht online. So etwas fällt niemandem auf — der Assistent
    /// antwortet ja, nur eben ohne Kenntnis. Der Wächter macht die Lücke sichtbar:
    /// Ein Bereich ohne Inhalt muss AUSDRÜCKLICH als solcher benannt sein.</para>
    ///
    /// <para><b>Was als Inhalt zählt.</b> Ein Abschnitt in
    /// <see cref="HilfeWissen.Abschnitte"/>, dessen <c>Bereich</c> GENAU dem
    /// Bereichsnamen entspricht. Die Rechenwegseiten (<c>Berechnungswissen</c>) tragen
    /// alle den Bereich „Berechnung" und decken deshalb keinen Bedienbereich ab; wo es
    /// zu einem Bereich eine Rechenwegseite gibt, sagt das die Ausnahmeliste. Ein
    /// örtlicher Hilfe-Cache trägt „Online-Hilfe" und zählt ebenfalls nicht mit — beide
    /// Namen stehen nicht in der Positivliste.</para>
    ///
    /// <para><b>Serielle Sammlung und Kulturpinnung</b> (Hausregel
    /// <c>EPOS.Kern/CLAUDE.md</c>): Die Fälle halten deutsche Titel gegen
    /// <c>Contains</c>, und die Kultur ist prozessweit.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KiWissensdeckungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  Die Ausnahmeliste — jeder Eintrag mit Grund
        // =====================================================================

        /// <summary>
        /// Bereiche OHNE eingebautes Wissen, jeder mit seinem Grund. Die Liste ist
        /// keine Ablage: Wer einen Bereich füllt, streicht ihn hier — die Gegenprobe
        /// <see cref="Die_Ausnahmeliste_fuehrt_nur_wirklich_leere_Bereiche"/> fällt
        /// sonst rot aus.
        /// </summary>
        /// <remarks>
        /// „Rechenweg vorhanden" heißt: Zum Thema gibt es eine Seite der Rubrik
        /// <c>Programm Dokumentation/Berechnung</c>, die als Abschnitt im Bereich
        /// „Berechnung" mitläuft. Sie beschreibt, WIE gerechnet wird — nicht, wie der
        /// Dialog bedient wird; deshalb ist der Bedienbereich trotzdem leer.
        /// </remarks>
        private static readonly IReadOnlyDictionary<string, string> Ausnahmen =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { KiChatKontext.B_ADMIN,            "Bedienwissen ausstehend" },
                { KiChatKontext.B_ASSISTENT,        "Bedienwissen ausstehend" },
                { KiChatKontext.B_BERICHT,          "Bedienwissen ausstehend" },
                { KiChatKontext.B_BHKW,             "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_BRAUCHWASSER,     "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_GEBAEUDE,         "Bedienwissen ausstehend" },
                { KiChatKontext.B_HAUPTFENSTER,     "Bedienwissen ausstehend" },
                { KiChatKontext.B_HEIZKESSEL,       "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_KOSTEN,           "Bedienwissen ausstehend" },
                { KiChatKontext.B_LIZENZ,           "Bedienwissen ausstehend" },
                { KiChatKontext.B_PROJEKT,          "Bedienwissen ausstehend" },
                { KiChatKontext.B_PROZESSWAERME,    "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_PUFFERSPEICHER,   "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_SOLARTHERMIE,     "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_STROMVERBRAUCHER, "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_VARIANTEN,        "Bedienwissen ausstehend" },
                { KiChatKontext.B_WAERMEBEDARF,     "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_WIRTSCHAFT,       "Bedienwissen ausstehend" },
                { KiChatKontext.B_QUELLE_ERDREICH,  "Rechenweg vorhanden, Bedienwissen ausstehend" },
                { KiChatKontext.B_SIM_DETAIL,       "Bedienwissen ausstehend" },
            };

        /// <summary>Die Abschnitte eines Bereichs — Gleichheit, keine Teilzeichenkette.</summary>
        private static List<WissensAbschnitt> Abschnitte(string bereich)
            => HilfeWissen.Abschnitte
                          .Where(a => string.Equals(a.Bereich, bereich, StringComparison.Ordinal))
                          .ToList();

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        [Fact]
        public void Jeder_Bereich_hat_Inhalt_oder_eine_benannte_Ausnahme()
        {
            var ohne = new SortedSet<string>(StringComparer.Ordinal);

            foreach (string bereich in KiChatKontext.Bereiche)
            {
                // „Unbekannter Bereich" ist kein Thema, sondern der Ersatzwert.
                if (string.Equals(bereich, KiChatKontext.BEREICH_UNBEKANNT, StringComparison.Ordinal))
                    continue;

                if (Abschnitte(bereich).Count > 0) continue;
                if (Ausnahmen.ContainsKey(bereich)) continue;

                ohne.Add(bereich);
            }

            Assert.True(ohne.Count == 0,
                "Diese Bereiche haben keinen Abschnitt im eingebauten Wissen und stehen auch " +
                "nicht mit Grund in der Ausnahmeliste (KiWissensdeckungTests.Ausnahmen): "
                + string.Join(", ", ohne));
        }

        /// <summary>
        /// <b>Gegenprobe:</b> Die Ausnahmeliste führt nur Bereiche, die es gibt, und nur
        /// solche, die WIRKLICH leer sind — und jeder Eintrag nennt einen Grund.
        /// </summary>
        [Fact]
        public void Die_Ausnahmeliste_fuehrt_nur_wirklich_leere_Bereiche()
        {
            foreach (KeyValuePair<string, string> eintrag in Ausnahmen)
            {
                Assert.Contains(eintrag.Key, KiChatKontext.Bereiche);

                Assert.False(string.IsNullOrWhiteSpace(eintrag.Value),
                             eintrag.Key + ": Ausnahme ohne Grund.");

                Assert.True(Abschnitte(eintrag.Key).Count == 0,
                            eintrag.Key + " hat inzwischen Wissen — den Eintrag aus der " +
                            "Ausnahmeliste streichen.");
            }
        }

        /// <summary>
        /// Der Anlass des Wächters: <b>Klimadaten ist gefüllt</b> und steht in keiner
        /// Ausnahme — drei Abschnitte Bedienwissen und die vier Meldungserklärungen.
        /// </summary>
        [Fact]
        public void Klimadaten_ist_gefuellt_und_keine_Ausnahme()
        {
            Assert.DoesNotContain(KiChatKontext.B_KLIMADATEN, Ausnahmen.Keys);

            List<WissensAbschnitt> klima = Abschnitte(KiChatKontext.B_KLIMADATEN);

            // Drei Abschnitte Bedienwissen (ohne Kennung) + vier Meldungserklärungen.
            Assert.Equal(3, klima.Count(a => a.Kennung.Length == 0));
            Assert.Equal(4, klima.Count(a => a.Kennung.Length > 0));

            foreach (string kennung in new[]
                     {
                         KiMeldungskennung.KLIMA_TRY_KEIN_BEREICH,
                         KiMeldungskennung.KLIMA_TRY_AUSSERHALB,
                         KiMeldungskennung.KLIMA_TRY_FORMATFEHLER,
                         KiMeldungskennung.KLIMA_TRY_STANDORT_UNLESBAR
                     })
            {
                WissensAbschnitt abschnitt = HilfeWissen.AbschnittFuerKennung(kennung);
                Assert.NotNull(abschnitt);
                Assert.Equal(KiChatKontext.B_KLIMADATEN, abschnitt.Bereich);
            }
        }

        // =====================================================================
        //  Die Suchprobe (Auftrag KI‑1, Punkt 4)
        // =====================================================================

        /// <summary>
        /// Die Fragen, mit denen ein Anwender die Klimadaten sucht, führen auf die neuen
        /// Abschnitte — ohne Netz, ohne Modell, ohne Wiki-Upload.
        /// </summary>
        /// <remarks>
        /// <b>Warum keine Frage aus drei Buchstaben.</b> <c>HilfeWissen.Suchen</c>
        /// verwirft Wörter unter vier Zeichen (Füllwörter); „TRY" und „DWD" ALLEIN
        /// tragen deshalb nichts bei, und daran ändert dieser Auftrag nichts — eine
        /// Mindestlänge von drei ließe „die", „der", „wie" mitzählen. Gefunden wird über
        /// die langen Begriffe, die im Titel und im Inhalt stehen: „Klimadaten",
        /// „Testreferenzjahr", „Regionaldaten", „PVGIS", „importieren", „einlesen",
        /// „Datei". Der Kontext ist bewusst <c>BEREICH_UNBEKANNT</c> — der Bereichsbonus
        /// soll das Ergebnis nicht tragen.
        /// </remarks>
        [Theory]
        [InlineData("Wie importiere ich TRY-Daten?")]
        [InlineData("Testreferenzjahr einlesen")]
        [InlineData("Klimadaten aus einer DWD-Datei anlegen")]
        [InlineData("Woher kommen die Regionaldaten der Testreferenzjahre?")]
        public void Die_Suche_fuehrt_auf_das_Klimawissen(string frage)
        {
            List<WissensAbschnitt> treffer =
                HilfeWissen.Suchen(frage, KiChatKontext.BEREICH_UNBEKANNT, 4, "");

            Assert.NotEmpty(treffer);
            Assert.Equal(KiChatKontext.B_KLIMADATEN, treffer[0].Bereich);
        }

        /// <summary>Die Frage nach den Adressen führt auf die Rubrik der Einstellungen.</summary>
        [Fact]
        public void Die_Frage_nach_der_PVGIS_Adresse_fuehrt_auf_die_Einstellungen()
        {
            List<WissensAbschnitt> treffer = HilfeWissen.Suchen(
                "PVGIS Adresse in den Einstellungen", KiChatKontext.BEREICH_UNBEKANNT, 4, "");

            Assert.NotEmpty(treffer);
            Assert.Equal(KiChatKontext.B_KLIMADATEN, treffer[0].Bereich);
            Assert.Contains("Einstellungen", treffer[0].Titel, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Gegenprobe:</b> Das neue Wissen drängt sich nicht vor — eine Frage aus
        /// einem anderen Gewerk findet weiterhin ihren eigenen Abschnitt.
        /// </summary>
        [Fact]
        public void Eine_fremde_Frage_findet_weiterhin_ihren_Abschnitt()
        {
            List<WissensAbschnitt> treffer = HilfeWissen.Suchen(
                "Bivalenzpunkt und Heizstab", KiChatKontext.BEREICH_UNBEKANNT, 4, "");

            Assert.NotEmpty(treffer);
            Assert.NotEqual(KiChatKontext.B_KLIMADATEN, treffer[0].Bereich);
            Assert.Contains("Bivalenzpunkt", treffer[0].Titel, StringComparison.Ordinal);
        }
    }
}
