using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Datenschicht der Peak-Shaving-Maske, seit iU9-W12.0a im Kern
    /// (<see cref="PeakShavingCtrl"/>, Befund W12-B23).
    ///
    /// <para><b>Der Anlass ist Befund W12-B25.</b> <c>LeseVorbelegung</c> fing bis
    /// hierher <c>OleDbException</c> ab. Seit der SQLite-Umstellung (<c>6486c36</c>)
    /// wirft der Zugriff <c>SqliteException</c> — der Rueckfall auf die Vorgaben des
    /// Fachkonzepts griff also gar nicht mehr, und die Maske waere bei einer nicht
    /// migrierten Datenbank mit einer unbehandelten Ausnahme stehen geblieben. Der
    /// Fall <see cref="Vorbelegung_ohne_Projekt_nimmt_die_Vorgaben_der_Modelle"/>
    /// und der Fall mit unbekannter Projekt-Id halten fest, dass die Maske OHNE
    /// Speicherprojekt arbeitsfaehig bleibt (Fachkonzept 6.4, Abgrenzung Rev. 4).</para>
    ///
    /// <para>Die datenbanklosen Faelle laufen immer; die uebrigen schweigen ohne
    /// Testdatenbank. EINE Arbeitskopie fuer die ganze Klasse — hier wird nur
    /// gelesen (Regel seit iU9-W11a).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PeakShavingCtrlTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public PeakShavingCtrlTests(TestDatenbank db) { _db = db; }

        private const int PROJEKT = 1030;

        // ------------------------------------------------------- ohne Datenbank

        /// <summary>
        /// Projekt-Id 0 heisst „kein Projekt geoeffnet". Dann wird gar nicht gelesen —
        /// die Vorgaben der Modelle stehen bereits im frisch gebauten Objekt.
        /// </summary>
        [Fact]
        public void Vorbelegung_ohne_Projekt_nimmt_die_Vorgaben_der_Modelle()
        {
            PeakShavingVorbelegung v = PeakShavingCtrl.LeseVorbelegung(0);

            Assert.False(v.AusProjekt);
            Assert.Equal("", v.Bezeichner);
            Assert.Equal(100.0, v.PKw);
            Assert.Equal(200.0, v.KapazitaetKwh);
            Assert.Equal(StromspeicherVarianteModel.SOC_MIN_VORGABE, v.SoCMinProzent);
            Assert.Equal(StromspeicherVarianteModel.SOC_MAX_VORGABE, v.SoCMaxProzent);
            Assert.Equal(StromspeicherVarianteModel.SOC_MIN_VORGABE, v.StartSoCProzent);
            Assert.Equal(StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE, v.WirkungsgradRt);
            Assert.Equal(StromspeicherVarianteModel.KAPITALZINS_VORGABE, v.KapitalzinsProzent);
            Assert.Equal(StromspeicherVarianteModel.NUTZUNGSDAUER_VORGABE, v.NutzungsdauerA);

            // Leistungspreis und Bezugspreis bleiben 0 — ein erfundener Vorgabewert
            // wuerde die Monetarisierung unbemerkt verfaelschen (Fachkonzept, offener
            // Punkt 3).
            Assert.Equal(0.0, v.LeistungspreisEurProKwA);
            Assert.Equal(0.0, v.BezugspreisMittelCtKwh);
            Assert.False(v.Kompatibilitaetsmodus);
        }

        /// <summary>
        /// Ein importierter Eintrag traegt seine Werte selbst; die Datenbank wird gar
        /// nicht angefasst.
        /// </summary>
        [Fact]
        public void LeseWerte_gibt_die_importierte_Reihe_unveraendert_zurueck()
        {
            double[] reihe = new double[] { 1.5, 2.5, 3.5 };
            GanglinienEintrag e = new GanglinienEintrag
            {
                Bezeichner = "aus Datei",
                Zeitinterval = 4,
                ImportWerte = reihe
            };

            Assert.True(e.IstImport);
            Assert.Same(reihe, PeakShavingCtrl.LeseWerte(e));
        }

        // ------------------------------------------------------- mit Datenbank

        /// <summary>
        /// Eine Projekt-Id, die es nicht gibt: gelesen wird, gefunden nichts — und die
        /// Vorgaben stehen weiter. Das ist der Weg, den Befund W12-B25 offengelegt hat.
        /// </summary>
        [Fact]
        public void Vorbelegung_mit_unbekanntem_Projekt_bleibt_bei_den_Vorgaben()
        {
            if (!_db.Vorhanden) return;

            PeakShavingVorbelegung v = PeakShavingCtrl.LeseVorbelegung(999999);

            Assert.False(v.AusProjekt);
            Assert.Equal(100.0, v.PKw);
            Assert.Equal(200.0, v.KapazitaetKwh);
        }

        /// <summary>
        /// Ohne Projekt bleiben die Stammganglinien — die Maske ist ausdruecklich auch
        /// ohne geoeffnetes Projekt nutzbar.
        /// </summary>
        [Fact]
        public void LeseGanglinien_ohne_Projekt_liefert_nur_Stammganglinien()
        {
            if (!_db.Vorhanden) return;

            List<GanglinienEintrag> liste = PeakShavingCtrl.LeseGanglinien(0);

            Assert.NotNull(liste);
            foreach (GanglinienEintrag e in liste)
            {
                Assert.True(e.AusStamm);
                Assert.NotEqual(0, e.Id);
                Assert.False(e.IstImport);
            }
        }

        /// <summary>
        /// Mit Projekt stehen die Projektganglinien VOR den Stammganglinien — die
        /// Reihenfolge fuellt die Auswahlliste der Maske und ist damit sichtbar.
        /// </summary>
        [Fact]
        public void LeseGanglinien_mit_Projekt_stellt_die_Projektganglinien_voran()
        {
            if (!_db.Vorhanden) return;

            List<GanglinienEintrag> liste = PeakShavingCtrl.LeseGanglinien(PROJEKT);
            Assert.NotNull(liste);

            bool stammBegonnen = false;
            foreach (GanglinienEintrag e in liste)
            {
                if (e.AusStamm) stammBegonnen = true;
                else Assert.False(stammBegonnen, "Eine Projektganglinie steht hinter einer Stammganglinie.");
            }

            // Ohne Projekt ist die Liste hoechstens so lang wie mit.
            Assert.True(PeakShavingCtrl.LeseGanglinien(0).Count <= liste.Count);
        }

        /// <summary>
        /// Stundenwerte werden durch WERTWIEDERHOLUNG auf Viertelstunden gelegt
        /// (<c>v[i*4+0..3] = w[i]</c>), nicht interpoliert — die Hauskonvention der
        /// Engine. Geprueft an der ersten Ganglinie, die die Testdatenbank hergibt.
        /// </summary>
        [Fact]
        public void LeseWerte_legt_Stundenwerte_durch_Wiederholung_auf_Viertelstunden()
        {
            if (!_db.Vorhanden) return;

            GanglinienEintrag stunden = null;
            foreach (GanglinienEintrag e in PeakShavingCtrl.LeseGanglinien(PROJEKT))
                if (e.Zeitinterval == 1) { stunden = e; break; }
            if (stunden == null) return;      // die Testdatenbank fuehrt keine Stundenreihe

            double[] werte = PeakShavingCtrl.LeseWerte(stunden);
            if (werte == null) return;

            Assert.Equal(0, werte.Length % 4);
            for (int i = 0; i + 3 < werte.Length; i += 4)
            {
                Assert.Equal(werte[i], werte[i + 1]);
                Assert.Equal(werte[i], werte[i + 2]);
                Assert.Equal(werte[i], werte[i + 3]);
            }
        }
    }
}
