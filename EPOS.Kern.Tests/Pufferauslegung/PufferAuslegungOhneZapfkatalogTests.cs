using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Seiten.Pufferspeicher;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Pufferauslegung ohne geladenen Zapfprofil-Paketteil</b> (Anwenderbefund 06.10.2026):
    /// Fehlt in <c>Tab_TwwParameter_STAMM</c> jede Katalogversion, rechnen Vorbelegung, Speichern
    /// und Übernahme ohne die Nenninhalte des Parametersatzes weiter — ohne eine
    /// <see cref="ParametersatzException"/> „keine Katalogversion" auch nur zu werfen (Visual Studio
    /// hält sonst an ihr an), und die Herkunftszeile der Nenninhalte nennt den Grund. Ein Projekt auf
    /// dem Zapfprofil-Weg lädt beim Vorbelegen den freien Paketteil nach
    /// (<see cref="TwwPaketteilCtrl.Nachladen"/>); das Speichern allein ruft den Generator nicht.
    /// Mit Katalogversion bleibt alles, wie es war. Geleert wird nur die Arbeitskopie der Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PufferAuslegungOhneZapfkatalogTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Projekt mit Zapfprofil-Weg (<c>Tab_TwwProjekt.Weg = GENERATOR</c>) und sein Kombipuffer.</summary>
        private const int P_ZAPF = 1045;
        private const int PUFFER_1045_KOMBI = 1054210;
        /// <summary>Projekt ohne Zapfprofil-Weg (keine Zeile in <c>Tab_TwwProjekt</c>).</summary>
        private const int P_PROZESS = 1041;

        private static void ZapfkatalogLeeren() =>
            DataRepository.ExecuteNonQuery("DELETE FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM);

        /// <summary>
        /// Zählt die auf diesem Thread geworfenen Parametersatz-Ausnahmen, auch gefangene: alle und
        /// die „keine Katalogversion" des Befunds.
        /// </summary>
        private sealed class Ausnahmezaehler : IDisposable
        {
            private readonly int _thread = Environment.CurrentManagedThreadId;
            internal int Alle;
            internal int OhneKatalogversion;

            internal Ausnahmezaehler() => AppDomain.CurrentDomain.FirstChanceException += Gesehen;

            private void Gesehen(object sender, FirstChanceExceptionEventArgs e)
            {
                if (Environment.CurrentManagedThreadId != _thread || e.Exception is not ParametersatzException p) return;
                Alle++;
                if (p.Fehler == ParametersatzFehler.KeineKatalogversion) OhneKatalogversion++;
            }

            public void Dispose() => AppDomain.CurrentDomain.FirstChanceException -= Gesehen;
        }

        private static PufferAuslegungHerkunft NenninhaltHerkunft(PufferAuslegungVorbelegung v) =>
            v.Herkunft.Single(h => h.Feld == nameof(PufferAuslegungEingang.Nenninhalte));

        /// <summary>Der Weg des Befunds: Vorbelegt mit Katalog, gespeichert ohne (Speichern → Grundvorbelegung → Nenninhalte).</summary>
        [Theory]
        [InlineData(P_ZAPF, PUFFER_1045_KOMBI)]
        [InlineData(P_PROZESS, null)]
        public void Ohne_Katalogversion_speichert_die_Auslegung_ohne_Parametersatzausnahme(int idProjekt, int? idPuffer)
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(idProjekt, idPuffer);
            ZapfkatalogLeeren();
            Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());

            using var zaehler = new Ausnahmezaehler();
            int id = PufferAuslegungCtrl.Speichern(idProjekt, idPuffer, v.Eingang, null);

            Assert.True(id > 0, "Speichern fehlgeschlagen.");
            Assert.Equal(0, zaehler.Alle);
            Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());
        }

        [Fact]
        public void Ohne_Zapfprofil_Weg_nennt_die_Vorbelegung_den_fehlenden_Katalog()
        {
            if (!_db.Vorhanden) return;
            ZapfkatalogLeeren();

            using var zaehler = new Ausnahmezaehler();
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_PROZESS, null);

            Assert.Equal(0, zaehler.Alle);
            Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());
            PufferAuslegungHerkunft h = NenninhaltHerkunft(v);
            Assert.Equal(PufferHerkunftsquelle.VORGABE, h.Quelle);
            Assert.Equal("PAUS_HERK_NENNINHALTE_OHNE_KATALOG", h.Baustein.Schluessel);
            Assert.Contains("Zapfprofil-Katalog nicht geladen", h.Text);
            Assert.Null(v.Eingang.Nenninhalte);
        }

        [Fact]
        public void Mit_Zapfprofil_Weg_laedt_die_Vorbelegung_den_freien_Paketteil_nach_statt_zu_werfen()
        {
            if (!_db.Vorhanden) return;
            ZapfkatalogLeeren();

            using var zaehler = new Ausnahmezaehler();
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_KOMBI);

            Assert.Equal(0, zaehler.OhneKatalogversion);
            Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.Contains(v.Herkunft, z => z.Feld == nameof(PufferAuslegungEingang.Zapfprofil));
            Assert.Single(v.Herkunft, z => z.Feld == nameof(PufferAuslegungEingang.Nenninhalte));
        }

        [Fact]
        public void Mit_Katalogversion_bleiben_die_Nenninhalte_aus_dem_Parametersatz()
        {
            if (!_db.Vorhanden) return;
            Assert.NotNull(ZapfprofilCtrl.AktuelleKatalogversion());

            using var zaehler = new Ausnahmezaehler();
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_KOMBI);

            Assert.Equal(0, zaehler.Alle);
            PufferAuslegungHerkunft h = NenninhaltHerkunft(v);
            Assert.Equal(PufferHerkunftsquelle.PARAMETER, h.Quelle);
            Assert.Equal(ZapfprofilCtrl.Nenninhalte(ZapfprofilCtrl.Parameter()).Liste.WerteL, v.Eingang.Nenninhalte);
        }

        [Fact]
        public void Uebernehmen_ohne_Katalogversion_meldet_statt_zu_werfen()
        {
            if (!_db.Vorhanden) return;
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@id", P_PROZESS)));
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " ohne Zapfkatalog");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            ZapfkatalogLeeren();

            using var zaehler = new Ausnahmezaehler();
            var h = new PufferAuslegungHuelle(new PufferAuslegungAuftrag { IdProjekt = kopie, IdPuffer = null });
            PufferAuslegungStartDaten d = h.Start();
            PufferUebernahmeErgebnis r = h.Uebernehmen(d.Eingabe, new PufferUebernahmeDaten(true, "Prozesspuffer ohne Zapfkatalog"));

            Assert.True(r.Erfolg, r.Text);
            Assert.Contains("Übernommen", r.Text);
            Assert.True(r.IdPuffer > 0);
            Assert.Equal(0, zaehler.Alle);
            Assert.Contains(d.Herkunft, z => z.Text.Contains("Zapfprofil-Katalog nicht geladen"));
        }
    }
}
