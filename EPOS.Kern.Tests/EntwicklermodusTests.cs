using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Entwicklermodus (Konzept Administration und Support 3.1/3.3, Handbuch Abschnitt 1):
    /// Sperre vor Umgebung vor Startargument vor Einstellung; Ein- und Ausschalten über die
    /// Einstellung, benannte Absage bei Umgebung und Startargument.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EntwicklermodusTests : IDisposable
    {
        /// <summary>Flüchtige Einstellungen mit setzbarem maschinenweitem Wert.</summary>
        private sealed class MaschinenEinstellungen : IEinstellungen
        {
            private readonly FluechtigeEinstellungen _innen = new FluechtigeEinstellungen();
            public string Maschine { get; set; }
            public string Lies(string schluessel, string vorgabe = null) => _innen.Lies(schluessel, vorgabe);
            public int LiesZahl(string schluessel, int vorgabe = 0) => _innen.LiesZahl(schluessel, vorgabe);
            public void Schreib(string schluessel, string wert) => _innen.Schreib(schluessel, wert);
            public void SchreibZahl(string schluessel, int wert) => _innen.SchreibZahl(schluessel, wert);
            public void Loesche(string schluessel) => _innen.Loesche(schluessel);
            public string LiesMaschine(string schluessel, string vorgabe = null)
                => schluessel == Entwicklermodus.SCHLUESSEL_SPERRE && Maschine != null ? Maschine : vorgabe;
        }

        private static readonly string[] OHNE = new string[0];
        private static readonly string[] MIT_ARGUMENT = { "--projekt", "1030", Entwicklermodus.STARTARGUMENT };

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly IEinstellungen _vorher = Dienste.Einstellungen;
        private readonly MaschinenEinstellungen _einstellungen = new MaschinenEinstellungen();

        public EntwicklermodusTests()
        {
            Dienste.Einstellungen = _einstellungen;
        }

        public void Dispose()
        {
            Dienste.Einstellungen = _vorher;
            Entwicklermodus.Ermitteln(OHNE, _ => null);
            _kultur.Dispose();
        }

        private static Func<string, string> Umgebung(string wert)
            => name => name == Entwicklermodus.UMGEBUNGSVARIABLE ? wert : null;

        [Fact]
        public void Ohne_alles_Anwender_ohne_Quelle()
        {
            Entwicklermodus.Ermitteln(OHNE, Umgebung(null));
            Assert.Equal(Freigabestufe.Anwender, Entwicklermodus.Stufe);
            Assert.False(Entwicklermodus.Aktiv);
            Assert.Equal(Entwicklermodusquelle.Keine, Entwicklermodus.Quelle);
            Assert.False(Entwicklermodus.Gesperrt);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ENTW_QUELLE_KEINE, Entwicklermodus.QuelleText);
        }

        [Fact]
        public void Sperre_schlaegt_Umgebung_Argument_und_Einstellung()
        {
            _einstellungen.SchreibZahl(Entwicklermodus.SCHLUESSEL, 1);
            _einstellungen.Maschine = "1";
            Entwicklermodus.Ermitteln(MIT_ARGUMENT, Umgebung("1"));
            Assert.True(Entwicklermodus.Gesperrt);
            Assert.Equal(Freigabestufe.Anwender, Entwicklermodus.Stufe);
            Assert.Equal(Entwicklermodusquelle.Keine, Entwicklermodus.Quelle);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ENTW_QUELLE_GESPERRT, Entwicklermodus.QuelleText);

            Assert.False(Entwicklermodus.Einschalten());
            Assert.False(Entwicklermodus.Aktiv);
        }

        [Fact]
        public void Sperre_nur_bei_Wert_1()
        {
            _einstellungen.Maschine = "0";
            Entwicklermodus.Ermitteln(MIT_ARGUMENT, Umgebung(null));
            Assert.False(Entwicklermodus.Gesperrt);
            Assert.Equal(Entwicklermodusquelle.Startargument, Entwicklermodus.Quelle);
        }

        [Fact]
        public void Rangfolge_Umgebung_vor_Argument_vor_Einstellung()
        {
            _einstellungen.SchreibZahl(Entwicklermodus.SCHLUESSEL, 1);

            Entwicklermodus.Ermitteln(MIT_ARGUMENT, Umgebung("1"));
            Assert.Equal(Entwicklermodusquelle.Umgebung, Entwicklermodus.Quelle);
            Assert.Equal(Freigabestufe.Entwickler, Entwicklermodus.Stufe);

            Entwicklermodus.Ermitteln(MIT_ARGUMENT, Umgebung("0"));
            Assert.Equal(Entwicklermodusquelle.Startargument, Entwicklermodus.Quelle);

            Entwicklermodus.Ermitteln(OHNE, Umgebung(null));
            Assert.Equal(Entwicklermodusquelle.Einstellung, Entwicklermodus.Quelle);
            Assert.True(Entwicklermodus.Aktiv);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ENTW_QUELLE_EINSTELLUNG, Entwicklermodus.QuelleText);
        }

        [Fact]
        public void Einschalten_schreibt_die_Einstellung()
        {
            Entwicklermodus.Ermitteln(OHNE, Umgebung(null));
            Assert.True(Entwicklermodus.Einschalten());
            Assert.Equal(1, _einstellungen.LiesZahl(Entwicklermodus.SCHLUESSEL));
            Assert.Equal(Entwicklermodusquelle.Einstellung, Entwicklermodus.Quelle);
            Assert.True(Entwicklermodus.Aktiv);

            // Der nächste Start findet sie wieder.
            Entwicklermodus.Ermitteln(OHNE, Umgebung(null));
            Assert.Equal(Entwicklermodusquelle.Einstellung, Entwicklermodus.Quelle);
        }

        [Fact]
        public void Ausschalten_loescht_die_Einstellung()
        {
            _einstellungen.SchreibZahl(Entwicklermodus.SCHLUESSEL, 1);
            Entwicklermodus.Ermitteln(OHNE, Umgebung(null));

            Assert.True(Entwicklermodus.Ausschalten(out string grund));
            Assert.Equal("", grund);
            Assert.Null(_einstellungen.Lies(Entwicklermodus.SCHLUESSEL));
            Assert.Equal(Freigabestufe.Anwender, Entwicklermodus.Stufe);
            Assert.Equal(Entwicklermodusquelle.Keine, Entwicklermodus.Quelle);
        }

        [Fact]
        public void Ausschalten_bei_Umgebung_oder_Argument_liefert_Grund()
        {
            _einstellungen.SchreibZahl(Entwicklermodus.SCHLUESSEL, 1);

            Entwicklermodus.Ermitteln(OHNE, Umgebung("1"));
            Assert.False(Entwicklermodus.Ausschalten(out string grundUmgebung));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ENTW_AUS_UMGEBUNG, grundUmgebung);
            Assert.Contains(Entwicklermodus.UMGEBUNGSVARIABLE, grundUmgebung);
            Assert.True(Entwicklermodus.Aktiv);

            Entwicklermodus.Ermitteln(MIT_ARGUMENT, Umgebung(null));
            Assert.False(Entwicklermodus.Ausschalten(out string grundArgument));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ENTW_AUS_STARTARGUMENT, grundArgument);
            Assert.Contains(Entwicklermodus.STARTARGUMENT, grundArgument);
            Assert.True(Entwicklermodus.Aktiv);

            // Die Einstellung bleibt unberührt.
            Assert.Equal(1, _einstellungen.LiesZahl(Entwicklermodus.SCHLUESSEL));
        }

        [Fact]
        public void Ausschalten_ohne_Modus_liefert_Grund()
        {
            Entwicklermodus.Ermitteln(OHNE, Umgebung(null));
            Assert.False(Entwicklermodus.Ausschalten(out string grund));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ENTW_AUS_NICHT_AKTIV, grund);
            Assert.False(string.IsNullOrWhiteSpace(grund));
        }

        [Fact]
        public void Werfende_Quellen_heissen_nicht_gesetzt()
        {
            Entwicklermodus.Ermitteln(null, _ => throw new InvalidOperationException("Probe"));
            Assert.Equal(Entwicklermodusquelle.Keine, Entwicklermodus.Quelle);
        }
    }
}
