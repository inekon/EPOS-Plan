using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Womit ein neu angelegter Strang anfängt</b> — Befund <b>W6‑B‑4</b> der
    /// Windows-Abnahme vom 07.09.2026: „Strang 1s1: 0 Module in Reihe, 1 parallel ·
    /// Werte fehlen: Module in Reihe …".
    ///
    /// <para><b>Der Nachweis geht bis zur Ampel.</b> Eine Vorbelegung, die nur schöne
    /// Zahlen setzt, wäre nichts wert; sie muss dazu führen, dass
    /// <see cref="StrangPlausibilitaet"/> danach GRÜN meldet — insbesondere P8, die
    /// Modulsumme gegen die „Anzahl Module" der Anlage. Genau das steht am Ende dieser
    /// Datei.</para>
    ///
    /// <para><b>Ohne Datenbank, ohne Oberfläche, ohne Sprachbindung</b> — geprüft
    /// werden Zahlen. Die zwei Fälle, die einen SATZ lesen, pinnen die Kultur.</para>
    /// </summary>
    public class StrangvorbelegungTests
    {
        // =================================================================================
        // 1 - Der erste Strang ist die ganze Anlage
        // =================================================================================

        /// <summary>
        /// <b>Der erste Strang trägt die Modulzahl der Anlage</b>, einen Strang parallel,
        /// Tracker 1 und Gerät 1 — der Regelfall einer Anlage an einem Wechselrichter.
        /// </summary>
        [Fact]
        public void Der_erste_Strang_traegt_die_Modulzahl_der_Anlage()
        {
            Strangvorbelegung.Vorschlag v = Strangvorbelegung.FuerNeuenStrang(
                anzahlModuleAnlage: 12, bestehendeStraenge: 0, belegteModule: 0,
                hoechsterMppt: 0, trackerDesGeraets: 1);

            Assert.Equal(12, v.ModuleReihe);
            Assert.Equal(1, v.StraengeParallel);
            Assert.Equal(1, v.Mppt);
            Assert.Equal(1, v.Geraetenummer);
        }

        /// <summary>
        /// <b>Eine gebrochene Modulzahl wird kaufmännisch gerundet</b> — dieselbe
        /// Rundung, mit der P8 die Summe vergleicht
        /// (<c>Math.Round(..., MidpointRounding.AwayFromZero)</c>). Zwei verschiedene
        /// Rundungen an derselben Grösse wären ein stiller Widerspruch.
        /// </summary>
        [Theory]
        [InlineData(11.4, 11)]
        [InlineData(11.5, 12)]
        [InlineData(12.6, 13)]
        public void Eine_gebrochene_Modulzahl_wird_wie_bei_P8_gerundet(double anlage, int erwartet)
        {
            Strangvorbelegung.Vorschlag v = Strangvorbelegung.FuerNeuenStrang(
                anzahlModuleAnlage: anlage, bestehendeStraenge: 0, belegteModule: 0,
                hoechsterMppt: 0, trackerDesGeraets: null);

            Assert.Equal(erwartet, v.ModuleReihe);
        }

        /// <summary>
        /// <b>Ohne bekannte Modulzahl steht EINS da</b>, nicht 0. Ein Strang mit null
        /// Modulen ist kein Strang — und genau die 0 war der Befund.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(-3.0)]
        public void Ohne_Modulzahl_faengt_der_Strang_bei_eins_an(double anlage)
        {
            Strangvorbelegung.Vorschlag v = Strangvorbelegung.FuerNeuenStrang(
                anzahlModuleAnlage: anlage, bestehendeStraenge: 0, belegteModule: 0,
                hoechsterMppt: 0, trackerDesGeraets: null);

            Assert.Equal(Strangvorbelegung.MINDESTREIHE, v.ModuleReihe);
            Assert.Equal(1, v.ModuleReihe);
        }

        // =================================================================================
        // 2 - Jeder weitere Strang bekommt den Rest
        // =================================================================================

        /// <summary>
        /// <b>Der zweite Strang bekommt, was noch nicht zugeordnet ist.</b> 12 Module,
        /// 6 im ersten Strang — der zweite bringt die anderen 6.
        /// </summary>
        [Fact]
        public void Der_zweite_Strang_bekommt_die_noch_freien_Module()
        {
            Strangvorbelegung.Vorschlag v = Strangvorbelegung.FuerNeuenStrang(
                anzahlModuleAnlage: 12, bestehendeStraenge: 1, belegteModule: 6,
                hoechsterMppt: 1, trackerDesGeraets: 2);

            Assert.Equal(6, v.ModuleReihe);
            Assert.Equal(1, v.StraengeParallel);
            Assert.Equal(2, v.Mppt);
        }

        /// <summary>
        /// <b>Ist die Anlage voll belegt, bleibt EINS übrig</b> — der Planer wollte
        /// offensichtlich einen Strang, und P8 sagt ihm sofort, dass die Summe nun
        /// grösser ist als die Anlage. Ein Vorschlag von 0 oder −4 wäre keiner.
        /// </summary>
        [Theory]
        [InlineData(12, 12)]
        [InlineData(12, 16)]
        public void Eine_volle_Anlage_gibt_dem_naechsten_Strang_ein_Modul(int anlage, int belegt)
        {
            Strangvorbelegung.Vorschlag v = Strangvorbelegung.FuerNeuenStrang(
                anzahlModuleAnlage: anlage, bestehendeStraenge: 2, belegteModule: belegt,
                hoechsterMppt: 2, trackerDesGeraets: 2);

            Assert.Equal(1, v.ModuleReihe);
        }

        // =================================================================================
        // 3 - Der MPP-Tracker
        // =================================================================================

        /// <summary>
        /// <b>Der nächste freie Tracker</b> — eine Nummer weiter, gedeckelt auf das,
        /// was das Gerät führt. Ohne bekannte Trackerzahl EINS (W6‑O‑2: die CEC-Liste
        /// führt <c>Anzahl_Mppt</c> nicht, und die Prüfung rechnet dann ohnehin auf
        /// einem Tracker).
        /// </summary>
        [Theory]
        [InlineData(0, 2, 1)]      // erster Strang am Zweitracker-Geraet
        [InlineData(1, 2, 2)]      // zweiter Strang -> Tracker 2
        [InlineData(2, 2, 2)]      // dritter Strang -> mehr hat das Geraet nicht
        [InlineData(1, 1, 1)]      // Eintracker-Geraet
        [InlineData(3, null, 1)]   // Trackerzahl unbekannt
        [InlineData(0, 0, 1)]      // unsinnige Trackerzahl im Katalog
        public void Der_naechste_Tracker_bleibt_im_Geraet(int hoechster, int? amGeraet, int erwartet)
        {
            Assert.Equal(erwartet, Strangvorbelegung.NaechsterTracker(hoechster, amGeraet));
        }

        // =================================================================================
        // 4 - Der Nachweis bis zur Ampel: P8 bleibt gruen
        // =================================================================================

        /// <summary>
        /// <b>Zwei vorbelegte Stränge ergeben genau die „Anzahl Module" der Anlage</b> —
        /// P8 (<c>Modulsumme</c>) bleibt damit still, und die Ampel meldet keinen
        /// fehlenden Wert mehr. Das ist der eigentliche Zweck der Vorbelegung: Der
        /// Planer sieht nach dem Anlegen einen Zustand, über den die Prüfung nichts zu
        /// beanstanden hat.
        /// </summary>
        [Fact]
        public void Zwei_vorbelegte_Straenge_lassen_P8_gruen()
        {
            const int ANLAGE = 12;

            Strangvorbelegung.Vorschlag erster = Strangvorbelegung.FuerNeuenStrang(
                ANLAGE, bestehendeStraenge: 0, belegteModule: 0,
                hoechsterMppt: 0, trackerDesGeraets: 2);

            // Der Planer teilt den ersten Strang auf sechs Module herunter …
            var s1 = new AnlageStrangModel
            {
                Rang = 1, ID_Wechselrichter = GERAET, Module_Reihe = 6,
                Straenge_Parallel = erster.StraengeParallel, Mppt = erster.Mppt,
                Geraetenummer = erster.Geraetenummer
            };

            // … und legt den zweiten an: er bekommt die restlichen sechs.
            Strangvorbelegung.Vorschlag zweiter = Strangvorbelegung.FuerNeuenStrang(
                ANLAGE, bestehendeStraenge: 1, belegteModule: s1.Modulzahl,
                hoechsterMppt: s1.MpptOderEins, trackerDesGeraets: 2);

            var s2 = new AnlageStrangModel
            {
                Rang = 2, ID_Wechselrichter = GERAET, Module_Reihe = zweiter.ModuleReihe,
                Straenge_Parallel = zweiter.StraengeParallel, Mppt = zweiter.Mppt,
                Geraetenummer = zweiter.Geraetenummer
            };

            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                new StrangPlausibilitaet.Gaben
                {
                    Straenge = new List<AnlageStrangModel> { s1, s2 },
                    Modul = Modul(),
                    Geraete = new Dictionary<int, WechselrichterModel> { { GERAET, Geraet() } },
                    AnzahlModuleAnlage = ANLAGE
                });

            Assert.Equal(6, zweiter.ModuleReihe);
            Assert.Equal(2, zweiter.Mppt);
            Assert.Equal(ANLAGE, b.Modulsumme);
            Assert.True(b.ModulsummeStimmt);
        }

        /// <summary>
        /// <b>Der EINE vorbelegte Strang ebenso</b> — der Regelfall, den der Anwender
        /// vor sich hatte: Anlage mit zwölf Modulen, ein Strang, Ampel ohne „Werte
        /// fehlen: Module in Reihe".
        /// </summary>
        [Fact]
        public void Ein_vorbelegter_Strang_traegt_die_ganze_Anlage()
        {
            const int ANLAGE = 12;

            Strangvorbelegung.Vorschlag v = Strangvorbelegung.FuerNeuenStrang(
                ANLAGE, bestehendeStraenge: 0, belegteModule: 0,
                hoechsterMppt: 0, trackerDesGeraets: 2);

            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                new StrangPlausibilitaet.Gaben
                {
                    Straenge = new List<AnlageStrangModel>
                    {
                        new AnlageStrangModel
                        {
                            Rang = 1, ID_Wechselrichter = GERAET,
                            Module_Reihe = v.ModuleReihe,
                            Straenge_Parallel = v.StraengeParallel,
                            Mppt = v.Mppt, Geraetenummer = v.Geraetenummer
                        }
                    },
                    Modul = Modul(),
                    Geraete = new Dictionary<int, WechselrichterModel> { { GERAET, Geraet() } },
                    AnzahlModuleAnlage = ANLAGE
                });

            Assert.Equal(ANLAGE, b.Modulsumme);
            Assert.True(b.ModulsummeStimmt);
        }

        // =================================================================================
        // Der Prüfaufbau — dieselben Zahlen wie in StrangPlausibilitaetTests (Anhang A)
        // =================================================================================

        private const int GERAET = 4711;

        private static PhotovoltaikModel Modul()
            => new PhotovoltaikModel
            {
                m_szName = "Ablytek 6MN6A275",
                m_Leistung = 275.19,
                m_U_Leerlauf = 38.4,
                m_U_Mpp = 31.4,
                m_I_Kurzschluss = 9.34,
                m_beta_OC = -0.118,
                m_alpha_SC = 0.0047
            };

        private static WechselrichterModel Geraet()
            => new WechselrichterModel
            {
                m_ID = GERAET,
                m_szName = "Muster 2500TL",
                m_P_AC_Nenn = 2.5,
                m_U_Mpp_Min = 80.0,
                m_U_Mpp_Max = 500.0,
                m_U_Dc_Max = 600.0,
                m_I_Dc_Max = 12.0,
                m_Anzahl_Mppt = 2
            };
    }
}
