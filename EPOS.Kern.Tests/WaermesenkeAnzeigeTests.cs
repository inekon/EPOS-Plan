using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="WaermesenkeClass.SenkeAnzeige"/> und <see cref="WaermesenkeClass.IstPufferZiel"/>
    /// nach iU9-W10a.0a — die beiden Anzeige- bzw. Zielfragen, die bis dahin in
    /// <c>Form_Waermesenke</c> standen.
    ///
    /// <para><b>Warum sie umgezogen sind (Befund W10-B22/B23).</b> <c>SenkeAnzeige</c> war
    /// eine STATISCHE Methode auf einem Formular, und fuenf fremde Stellen riefen sie von
    /// dort — die Erzeugerkarten, die Uebersicht und das Schemamodell. Mit dem Port der
    /// Maske nach Blazor waere der Bau gebrochen. <c>IstPufferZiel</c> stand doppelt da:
    /// die Kernfassung und eine Formularfassung mit einem zusaetzlichen Oder-Zweig auf
    /// <c>PufferProzess</c>, den die Kernfassung seit Paket S1 selbst fuehrt.</para>
    ///
    /// <para><b>Keine Datenbank noetig, solange kein Puffer haengt.</b> Nur der Zweig mit
    /// <c>ID_Puffer &gt; 0</c> fragt <c>Tab_Pufferspeicher</c>; die Faelle hier bleiben
    /// darunter bzw. pruefen nur, dass ohne Puffer die reine Kurzform steht.</para>
    /// </summary>
    public class WaermesenkeAnzeigeTests
    {
        /// <summary>
        /// Der Windows-Laeufer steht auf en-US (Befund aus Welle 8) — jeder Fall, der
        /// einen Ressourcentext woertlich vergleicht, legt die Oberflaechensprache fuer
        /// seine Dauer fest und stellt sie danach zurueck.
        /// </summary>
        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly System.Globalization.CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo("de-DE");
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorher;
            }
        }

        // ================================================================= IstPufferZiel

        [Theory]
        [InlineData(DbWerte.WS_ZIEL_PUFFER_HEIZUNG)]
        [InlineData(DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER)]
        [InlineData(DbWerte.WS_ZIEL_PUFFER_KOMBI)]
        [InlineData(DbWerte.WS_ZIEL_PUFFER_PROZESS)]
        public void IstPufferZiel_kennt_alle_vier_Ladeziele(string ziel)
        {
            Assert.True(WaermesenkeClass.IstPufferZiel(ziel));
        }

        /// <summary>
        /// Die beiden DIREKTsenken sind kein Puffer-Ziel — <c>Prozesswaerme</c>
        /// ausdruecklich nicht, sonst ginge sie in der Altspaltenpflege als Ladeziel durch.
        /// </summary>
        [Theory]
        [InlineData(DbWerte.WS_ZIEL_HEIZKREIS)]
        [InlineData(DbWerte.WS_ZIEL_PROZESS)]
        [InlineData("")]
        [InlineData(null)]
        public void IstPufferZiel_verneint_Direktsenken(string ziel)
        {
            Assert.False(WaermesenkeClass.IstPufferZiel(ziel));
        }

        // ================================================================== SenkeAnzeige

        [Fact]
        public void SenkeAnzeige_ohne_Zeile_liefert_den_Gedankenstrich()
        {
            Assert.Equal(WaermesenkeClass.SENKE_LEER, WaermesenkeClass.SenkeAnzeige(null));
            Assert.Equal("–", WaermesenkeClass.SENKE_LEER);
        }

        /// <summary>
        /// Ladeziel OHNE zugeordneten Speicher: die reine Kurzform, kein Doppelpunkt.
        /// </summary>
        [Fact]
        public void SenkeAnzeige_beim_Ladeziel_ohne_Puffer_nur_die_Kurzform()
        {
            using var _ = new DeutscheOberflaeche();
            var z = new Z_AnlageSenkeModel
            {
                Ziel = DbWerte.WS_ZIEL_PUFFER_HEIZUNG,
                ID_Puffer = 0
            };

            Assert.Equal("Puffer Heizung", WaermesenkeClass.SenkeAnzeige(z));
        }

        [Fact]
        public void SenkeAnzeige_beim_Prozesskanal_nennt_den_Kanal()
        {
            using var _ = new DeutscheOberflaeche();
            var z = new Z_AnlageSenkeModel { Ziel = DbWerte.WS_ZIEL_PROZESS };

            Assert.Equal("Prozesswärme",
                         WaermesenkeClass.SenkeAnzeige(z));
        }

        /// <summary>
        /// Beim Heizkreis entscheidet die BEDARFSART ueber den Zusatz (Konzept 3.1) —
        /// die Feinsteuerung, die der Vorlaeufer aus zwei aelteren Methoden geerbt hat.
        /// </summary>
        [Fact]
        public void SenkeAnzeige_beim_Heizkreis_unterscheidet_die_Bedarfsart()
        {
            using var _ = new DeutscheOberflaeche();

            var beides = new Z_AnlageSenkeModel
            {
                Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                Bedarfsart = WaermequelleClass.SENKE_BEIDES
            };
            var warmwasser = new Z_AnlageSenkeModel
            {
                Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                Bedarfsart = WaermequelleClass.SENKE_WARMWASSER
            };
            var heizung = new Z_AnlageSenkeModel
            {
                Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                Bedarfsart = WaermequelleClass.SENKE_HEIZUNG
            };

            Assert.Equal("Heizkreis (beides)", WaermesenkeClass.SenkeAnzeige(beides));
            Assert.Equal("Heizkreis (nur Warmwasser)", WaermesenkeClass.SenkeAnzeige(warmwasser));
            Assert.Equal("Heizkreis (nur Heizwärme)",
                         WaermesenkeClass.SenkeAnzeige(heizung));
        }

        /// <summary>
        /// Ein unbekannter Bedarfsart-Wert faellt auf „beides" zurueck — der
        /// <c>default</c>-Zweig des Vorlaeufers, woertlich uebernommen.
        /// </summary>
        [Fact]
        public void SenkeAnzeige_faellt_bei_unbekannter_Bedarfsart_auf_beides_zurueck()
        {
            using var _ = new DeutscheOberflaeche();
            var z = new Z_AnlageSenkeModel
            {
                Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                Bedarfsart = "gibt-es-nicht"
            };

            Assert.Equal("Heizkreis (beides)", WaermesenkeClass.SenkeAnzeige(z));
        }
    }
}
