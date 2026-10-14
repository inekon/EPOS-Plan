using System;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Ausnahmeprotokoll</b> (Anwenderauftrag 30.09.2026): Eintrag mit Typ, Meldung,
    /// inneren Ausnahmen und Stapel; Drosselung je Sekunde samt Nachtrag; Umlauf über die
    /// Höchstgröße; Rückfall auf den Temp-Ordner. Das prozessweite <c>Einschalten</c> bleibt
    /// hier aus — es hinge sich an jede Ausnahme des Testlaufs.
    /// </summary>
    public class AusnahmeprotokollTests : IDisposable
    {
        private readonly string _ordner = Path.Combine(Path.GetTempPath(), "AusnahmeprotokollTests_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { }
        }

        private static Exception Geworfen()
        {
            try { throw new InvalidOperationException("außen", new ArgumentException("innen")); }
            catch (Exception ex) { return ex; }
        }

        [Fact]
        public void Ein_Eintrag_nennt_Typ_Meldung_innere_Ausnahme_und_Stapel()
        {
            var p = new Ausnahmeprotokoll(() => _ordner);

            p.Schreiben("AUSGELOEST", Geworfen(), "   bei Beispiel.Stelle()");

            string text = File.ReadAllText(Path.Combine(_ordner, Ausnahmeprotokoll.DATEI));
            Assert.Contains("[AUSGELOEST]", text);
            Assert.Contains("System.InvalidOperationException: außen", text);
            Assert.Contains("innen: System.ArgumentException: innen", text);
            Assert.Contains("bei Beispiel.Stelle()", text);
        }

        [Fact]
        public void Ohne_eigenen_Stapel_gilt_der_der_Ausnahme()
        {
            var p = new Ausnahmeprotokoll(() => _ordner);

            p.Schreiben("UNBEHANDELT", Geworfen(), null);

            Assert.Contains(nameof(Geworfen), File.ReadAllText(p.Datei));
        }

        [Fact]
        public void Nichts_wird_geschrieben_ohne_Ausnahme()
        {
            var p = new Ausnahmeprotokoll(() => _ordner);

            p.Schreiben("AUSGELOEST", null, null);

            Assert.False(File.Exists(Path.Combine(_ordner, Ausnahmeprotokoll.DATEI)));
        }

        [Fact]
        public void Ein_Vermerk_steht_ohne_Ausnahme_im_Protokoll_und_ein_leerer_nicht()
        {
            var p = new Ausnahmeprotokoll(() => _ordner);

            p.Vermerk("");
            Assert.False(File.Exists(p.Datei));

            p.Vermerk("Ansicht KAELTEMASCHINE_ANLAGE ohne Wurzel");
            string text = File.ReadAllText(p.Datei);
            Assert.Contains("[VERMERK] Ansicht KAELTEMASCHINE_ANLAGE ohne Wurzel", text);
        }

        [Fact]
        public void Die_Drosselung_zaehlt_und_nennt_die_ausgelassenen_Eintraege()
        {
            DateTime jetzt = new DateTime(2026, 9, 30, 17, 50, 9);
            var p = new Ausnahmeprotokoll(() => _ordner, () => jetzt);
            Exception ex = Geworfen();

            for (int i = 0; i < Ausnahmeprotokoll.HOECHSTE_JE_SEKUNDE + 5; i++)
                p.Schreiben("AUSGELOEST", ex, "s");
            jetzt = jetzt.AddSeconds(1);
            p.Schreiben("AUSGELOEST", ex, "s");

            string text = File.ReadAllText(p.Datei);
            int eintraege = text.Split('\n').Count(z => z.Contains("[AUSGELOEST]"));
            Assert.Equal(Ausnahmeprotokoll.HOECHSTE_JE_SEKUNDE + 1, eintraege);
            Assert.Contains("(5 weitere Ausnahmen gedrosselt)", text);
        }

        [Fact]
        public void Ueber_der_Hoechstgroesse_laeuft_die_Datei_um()
        {
            DateTime jetzt = new DateTime(2026, 9, 30, 0, 0, 0);
            var p = new Ausnahmeprotokoll(() => _ordner, () => jetzt);
            string gross = new string('x', 4096);

            for (int i = 0; i < 80; i++)
            {
                jetzt = jetzt.AddSeconds(1);              // keine Drosselung
                p.Schreiben("AUSGELOEST", new Exception(gross), "s");
            }

            Assert.True(File.Exists(Path.Combine(_ordner, Ausnahmeprotokoll.DATEI_ALT)));
            Assert.True(new FileInfo(p.Datei).Length <= Ausnahmeprotokoll.HOECHSTGROESSE + 8192);
        }

        [Fact]
        public void Ohne_brauchbaren_Ordner_schreibt_es_in_den_Temp_Ordner()
        {
            var p = new Ausnahmeprotokoll(() => throw new InvalidOperationException("keine Einstellungen"));

            Assert.StartsWith(Path.GetTempPath(), p.Datei);
        }

        [Fact]
        public void Die_Kopfzeile_trennt_die_Laeufe()
        {
            var p = new Ausnahmeprotokoll(() => _ordner);

            p.Kopf("Start EPOS_Plan 1.2.0.6");

            Assert.Contains("Start EPOS_Plan 1.2.0.6 ===", File.ReadAllText(p.Datei));
        }
    }
}
