using System;
using System.Globalization;
using System.Linq;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Aufschlag in der Projekteinstellung</b> (Entwurf KP3, Welle O1b; E59 (2), Festlegungen 35, 36; P16) —
    /// die Hüllenseite hinter den zwei Feldern „Aufschlag (h)" und „Aufschlag (%)".
    ///
    /// <para><b>Geprüft wird:</b> Die Angaben der Herleitungszeile nehmen n' aus der Formel des Kerns
    /// (<see cref="Aufheizoptimierung.MitAufschlag(int, Aufheizvorgabe)"/>) mit n = t_auf,max + 1, nur für bemessene
    /// Gebäude ohne manuelle Aufheizzeit und nur, wenn der Aufschlag die Rampe verlängert (n &gt; 1, Deckel 48);
    /// die Naht der Ergebnishülle schreibt und liest den Aufschlag und nennt n' in der Zeile je Gebäude, während
    /// t_auf,max die bemessene Zeit bleibt; das Speichern der Kaskade (Delete + Insert in
    /// <c>SimulationKonfigHuelle</c>) trägt den Aufschlag mit.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizAufschlagEinstellungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly IEinstellungen _vorher = Dienste.Einstellungen;

        public AufheizAufschlagEinstellungTests()
        {
            Dienste.Einstellungen = new FluechtigeEinstellungen();
        }

        public void Dispose()
        {
            Dienste.Einstellungen = _vorher;
            _db.Dispose();
        }

        /// <summary>Das Hotel (Referenzprojekt 1018): ein Gebäude nach VDI 6007 mit Einstellungssatz.</summary>
        private const int HOTEL = 1018;

        /// <summary>Schalter an, Vorgaben, Aufschlag 2 h und 50 %.</summary>
        private static readonly Aufheizvorgabe MIT_AUFSCHLAG = new Aufheizvorgabe(true, null, null, null, null, 2, 50.0);

        private static Aufheizauskunft Bemessen(int tAufMax, int? manuell = null)
            => new Aufheizauskunft
            {
                ID_Gebaeude = 7, Gebaeudename = "Haus", Zustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN,
                Bemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, AufheizzeitMaxH = tAufMax, AussenC = -10.0,
                LeistungKw = 30.0, LeistungUnskaliertKw = 30.0, Skalierungsfaktor = 1.0, Quelle = DbWerte.AUFHEIZ_QUELLE_ZIEL,
                AufheizzeitManuellH = manuell,
            };

        /// <summary>
        /// n' nach Festlegung 35 aus dem Kern: n = t_auf,max + 1; Stunden, Prozent (aufgerundet), das Maximum beider,
        /// der Deckel 48 — und keine Angabe ohne Wirkung.
        /// </summary>
        [Theory]
        [InlineData(5, 2, 0.0, 6, 8)]
        [InlineData(5, 0, 50.0, 6, 9)]      // ⌈6 · 0,5⌉ = 3
        [InlineData(5, 2, 50.0, 6, 9)]      // max(2, 3)
        [InlineData(2, 0, 10.0, 3, 4)]      // ⌈3 · 0,1⌉ = 1
        [InlineData(5, 24, 100.0, 6, 30)]
        [InlineData(40, 24, 0.0, 41, 48)]   // Deckel
        public void Die_Angaben_nennen_n_und_n_strich_aus_dem_Kern(int tAufMax, int h, double prozent, int n, int nStrich)
        {
            var vorgabe = new Aufheizvorgabe(true, null, null, null, null, h, prozent);
            AufheizHerleitungsdaten d = AufheizHerleitungszeile.Aus(Bemessen(tAufMax), vorgabe);
            Assert.Equal(n, d.RampeN);
            Assert.Equal(nStrich, d.RampeNAufschlag);
            Assert.Equal(Aufheizoptimierung.MitAufschlag(n, vorgabe), d.RampeNAufschlag);
            Assert.Equal(tAufMax, d.AufheizzeitMaxH);
        }

        /// <summary>
        /// Keine Angabe: ohne Aufschlag (auch 0/0 und „aus" ohne Werte), ohne Vorgabe, bei n = 1 (P16), bei manueller
        /// Aufheizzeit, UNERREICHBAR, GEKOPPELT und auf dem Tagesbilanz-Weg; die übrigen Angaben wie ohne Vorgabe.
        /// </summary>
        [Fact]
        public void Ohne_Wirkung_nennen_die_Angaben_kein_n_strich()
        {
            Aufheizauskunft a = Bemessen(5);
            Assert.Equal(AufheizHerleitungszeile.Aus(a), AufheizHerleitungszeile.Aus(a, new Aufheizvorgabe(true, null, null, null, null, 0, 0.0)));
            Assert.Equal(AufheizHerleitungszeile.Aus(a), AufheizHerleitungszeile.Aus(a, null));
            Assert.Equal(AufheizHerleitungszeile.Aus(a), AufheizHerleitungszeile.Aus(a, Aufheizvorgabe.Aus));

            Assert.Null(AufheizHerleitungszeile.Aus(Bemessen(0), MIT_AUFSCHLAG).RampeNAufschlag);
            Assert.Null(AufheizHerleitungszeile.Aus(Bemessen(5, manuell: 8), MIT_AUFSCHLAG).RampeNAufschlag);
            Assert.Null(AufheizHerleitungszeile.Aus(a with { Zustand = DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, AufheizzeitMaxH = null },
                                                    MIT_AUFSCHLAG).RampeNAufschlag);
            Assert.Null(AufheizHerleitungszeile.Aus(a with { Zustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT }, MIT_AUFSCHLAG).RampeN);
            Assert.Null(AufheizHerleitungszeile.Aus(new Aufheizauskunft { Gebaeudename = "EFH", Tagesbilanz = true }, MIT_AUFSCHLAG).RampeN);
        }

        /// <summary>
        /// Die Naht der Ergebnishülle mit der Testdatenbank: Der Aufschlag geht über <c>AufheizvorgabeSchreiben</c> in
        /// die zwei Spalten und kommt mit <c>Laden</c> zurück; die Herleitungszeile des Hotels nennt n' aus dem Kern
        /// neben dem unveränderten t_auf,max, deutsch und englisch; ohne Aufschlag steht n' nicht da.
        /// </summary>
        [Fact]
        public void Die_Huelle_nennt_n_strich_in_der_Herleitungszeile()
        {
            if (!_db.Vorhanden) return;
            SimulationParameterDienste wege = SimulationErgebnisHuelle.Erzeugen(null, HOTEL, new BedarfsZustand()).ParameterGaben();
            var p = new ProjektCtrl();
            p.ReadSingle(HOTEL);

            Assert.True(wege.AufheizvorgabeSchreiben(new Aufheizvorgabe(true, null, null, null, null)));
            Aufheizauskunft ohne = GebaeudeBedarfCtrl.Aufheizbemessung(HOTEL, p.m_ID_Klimaregion).Single();
            string zeileOhne;
            using (new Kulturvorrichtung("de-DE"))
            {
                zeileOhne = Assert.Single(wege.AufheizHerleitung());
                Assert.DoesNotContain("n′", zeileOhne, StringComparison.Ordinal);
            }

            Assert.True(wege.AufheizvorgabeSchreiben(MIT_AUFSCHLAG));
            Assert.Equal(MIT_AUFSCHLAG, KonfigurationCtrl.AufheizvorgabeLesen(HOTEL));
            Assert.Equal(MIT_AUFSCHLAG, wege.Laden().Aufheizung);

            Aufheizauskunft mit = GebaeudeBedarfCtrl.Aufheizbemessung(HOTEL, p.m_ID_Klimaregion).Single();
            Assert.Equal(ohne.AufheizzeitMaxH, mit.AufheizzeitMaxH);   // t_auf,max bleibt die bemessene Zeit
            int t = mit.AufheizzeitMaxH ?? -1;
            Assert.True(t >= 1, "Das Hotel muss eine Rampe mit n > 1 bemessen, sonst prüft der Fall nichts (t = " + t + ").");
            int nStrich = Aufheizoptimierung.MitAufschlag(t + 1, MIT_AUFSCHLAG);
            Assert.True(nStrich > t + 1);

            using (new Kulturvorrichtung("de-DE"))
            {
                string zeile = Assert.Single(wege.AufheizHerleitung());
                Assert.Equal(zeileOhne + " · Aufschlag: längste Rampe n′ = " + nStrich.ToString(CultureInfo.CurrentCulture) +
                             " statt " + (t + 1).ToString(CultureInfo.CurrentCulture) + " Stufen", zeile);
            }
            using (new Kulturvorrichtung("en-US"))
            {
                string zeile = Assert.Single(wege.AufheizHerleitung());
                Assert.EndsWith(" · surcharge: longest ramp n′ = " + nStrich + " instead of " + (t + 1) + " steps", zeile,
                                StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Das Speichern der Kaskade (Delete + Insert der Einstellungszeile) trägt den Aufschlag mit — auch bei
        /// Schalter aus (Festlegung 36).
        /// </summary>
        [Fact]
        public void Das_Speichern_der_Kaskade_traegt_den_Aufschlag_mit()
        {
            if (!_db.Vorhanden) return;
            foreach (Aufheizvorgabe vorgabe in new[] { MIT_AUFSCHLAG, new Aufheizvorgabe(false, null, null, 0.3, null, 4, 12.5) })
            {
                Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(HOTEL, vorgabe));
                var dienste = (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(HOTEL).Gaben()["Dienste"];
                Assert.True(dienste.Speichern());
                Assert.Equal(vorgabe, KonfigurationCtrl.AufheizvorgabeLesen(HOTEL));
            }
        }
    }
}
