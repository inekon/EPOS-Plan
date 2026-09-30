using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das gefüllte A100-Paket der Entwicklungsumgebung</b> (Anwenderentscheid 29.09.2026, Regel
    /// ZU24): Die gefüllte Kopie der Paketvorlage <c>Referenzlaeufe/Katalogpaket_Vorlage_A100/</c> mit
    /// den Werten des lizenzierten Normexemplars liegt nur lokal unter
    /// <c>Referenzlaeufe/Normzahlen/Katalogpaket_A100/</c> — ausgeschlossen durch <c>.gitignore</c>
    /// (Wache <see cref="RepositoryOrdnungWacheTests.Normzahlen_stehen_im_gitignore"/>), nie im
    /// Repositorium, nie in der Auslieferung. Liegt der Ordner mit seinen vier Dateien vor, spielt
    /// dieser Fall ihn über den Katalogimport des Anwenders
    /// (<see cref="TwwNutzungsartCtrl.Importieren"/>) in eine leere Tww-Datenbank ein und prüft ihn;
    /// sonst ist er mit benanntem Grund übersprungen — in der CI und in der Cloud liegt er nie.
    ///
    /// <para><b>Keine Normzahl.</b> Weder dieser Fall noch seine Meldungen nennen einen Wert oder
    /// einen Namen des Pakets: gemeldet werden Kennungen, Ids und Zählungen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwNormpaketLokalTests
    {
        /// <summary>Die vier Dateien des Importformats, die die Paketvorlage führt.</summary>
        private static readonly string[] DATEIEN =
        {
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv",
            TwwSchema.TAB_TWW_TAGESGANG_STAMM + ".csv",
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv",
            TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ".csv"
        };

        private readonly ITestOutputHelper _ausgabe;

        public TwwNormpaketLokalTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        [Fact]
        public void Das_lokale_A100_Paket_spielt_ueber_den_Katalogimport_ohne_Ablehnung_ein()
        {
            string ordner = Path.Combine(Wurzel(), "Referenzlaeufe", "Normzahlen", "Katalogpaket_A100");
            string[] fehlend = DATEIEN.Where(d => !File.Exists(Path.Combine(ordner, d))).ToArray();
            if (fehlend.Length > 0)
            {
                _ausgabe.WriteLine("Lokales A100-Paket übersprungen: Referenzlaeufe/Normzahlen/Katalogpaket_A100/ fehlt " +
                                   "oder ist unvollständig (fehlt: " + string.Join(", ", fehlend) + "). Die gefüllte " +
                                   "Kopie der Paketvorlage liegt nur lokal, nie im Repositorium und nie in der CI.");
                return;
            }

            using var db = new TwwTestdatenbank();
            IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(ordner, out ZapfSatz fehler);
            Assert.True(fehler == null, "Das lokale A100-Paket ist nicht lesbar: " + fehler?.Kennung);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien.ToList());
            Assert.True(b.Abbruch == null, "Das lokale A100-Paket ist als Ganzes abgelehnt: " + b.Abbruch?.Kennung);
            Assert.True(b.Abgelehnt == 0, b.Abgelehnt + " Nutzungsart(en) abgelehnt, Gründe: " +
                                          string.Join(", ", b.Zeilen.Where(z => z.Ausgang == TwwImportausgang.Abgelehnt)
                                                                    .Select(z => "Zeile " + z.Zeile + " " + z.Grund?.Kennung)));
            Assert.True(b.Angelegt >= 1, "Das lokale A100-Paket legt keine Nutzungsart an.");

            foreach (int id in b.NeueIds)
            {
                Nutzungsart n = TwwNutzungsartCtrl.Lies(id);
                Assert.True(n != null, "Nutzungsart #" + id + " ist nicht lesbar.");
                Assert.True(n.Tagesgaenge != null && n.Tagesgaenge.Vollstaendig, "Nutzungsart #" + id + ": Tagesgangsatz unvollständig.");
                for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
                {
                    double tag = 0;
                    for (int h = 0; h < Tagesgangsatz.STUNDEN; h++) tag += n.Tagesgaenge.Anteile[t, h];
                    Assert.True(Math.Abs(tag - 1.0) <= 1e-6, "Nutzungsart #" + id + ", Tagtyp " + (t + 1) + ": Summe der Stundenanteile ist nicht 1.");
                }
                Assert.True(Math.Abs(n.Wochenfaktoren.Sum() - 1.0) <= 1e-6, "Nutzungsart #" + id + ": Summe der Wochenanteile ist nicht 1.");
                Assert.True(Math.Abs(n.Monatsfaktoren.Average() - 1.0) <= 1e-6, "Nutzungsart #" + id + ": Mittel der Monatsfaktoren ist nicht 1.");

                IReadOnlyList<Zapfkategorie> k = TwwNutzungsartCtrl.KategorienLesen(id).Kategorien;
                Assert.True(k.Count > 0, "Nutzungsart #" + id + ": ohne Zapfkategorien.");
                Assert.True(Math.Abs(k.Sum(x => x.Anteil) - 1.0) <= 1e-6, "Nutzungsart #" + id + ": Summe der Kategorienanteile ist nicht 1.");
            }
            _ausgabe.WriteLine("Lokales A100-Paket: " + b.Angelegt + " Nutzungsart(en) angelegt, keine abgelehnt, Summenregeln gehalten.");
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
