using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Admin;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Logos</b> in der Rubrik „Bericht" der Programmeinstellungen (Etappe BV-E2,
    /// Anwenderentscheid BV-E2-1, Lesart b): Die Gaben tragen <c>Logo</c>, <c>LogoChanged</c>,
    /// <c>LogoWaehler</c> und <c>LogoVorhanden</c> passend zu den Parametern des Dialogs; gelesen und
    /// geschrieben wird die Einstellung <c>BerichtLogo</c> (Dateipfad) allein über den Controller
    /// (<see cref="BerichtsvorlagenCtrl.LogoPfad"/>, <see cref="BerichtsvorlagenCtrl.SchreibeLogo"/>), auch zu
    /// einer Datei, die es nicht gibt; leer entfernt die Einstellung; die Dateiwahl geht über
    /// <c>Dienste.Datei</c> mit dem Bildfilter.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EinstellungenBerichtLogoHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _wurzel = Probevorlagen.TempOrdner("epos-bv-logo");
        private readonly FluechtigeEinstellungen _einstellungen = new FluechtigeEinstellungen();
        private readonly Probepfade _pfade;
        private readonly BerichtsvorlagenCtrl _vorlagen;
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;

        public EinstellungenBerichtLogoHuelleTests()
        {
            string dokumente = Directory.CreateDirectory(Path.Combine(_wurzel, "Dokumente")).FullName;
            string app = Directory.CreateDirectory(Path.Combine(_wurzel, "App", "Vorlagen")).FullName;
            _pfade = new Probepfade(dokumente, app);
            _vorlagen = new BerichtsvorlagenCtrl(_pfade, _einstellungen, () => "Probe GmbH");
        }

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_wurzel);
        }

        private IReadOnlyDictionary<string, object> Gaben()
        {
            return EinstellungenBerichtGaben.Gaben(_vorlagen, new Berichtsvorlagenwege { OrdnerWaehlbar = true });
        }

        /// <summary>Die vier Gaben des Logos treffen die Parameter des Dialogs; der Anfangswert ist die Einstellung des Controllers.</summary>
        [Fact]
        public void Die_Gaben_des_Logos_treffen_die_Parameter_des_Dialogs()
        {
            _einstellungen.Schreib(BerichtsvorlagenCtrl.EINSTELLUNG_LOGO, @"C:\Logos\firma.png");
            IReadOnlyDictionary<string, object> gaben = Gaben();

            Dictionary<string, Type> parameter = typeof(EinstellungenDialog).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
                .ToDictionary(p => p.Name, p => p.PropertyType);
            foreach (string schluessel in new[] { "Logo", "LogoChanged", "LogoWaehler", "LogoVorhanden" })
            {
                Assert.True(gaben.ContainsKey(schluessel), "Es fehlt " + schluessel);
                Assert.True(parameter.ContainsKey(schluessel), "kein [Parameter] " + schluessel);
                Assert.True(parameter[schluessel].IsInstanceOfType(gaben[schluessel]), schluessel);
            }
            foreach (KeyValuePair<string, object> g in gaben)
                Assert.True(parameter.ContainsKey(g.Key), "kein [Parameter] " + g.Key);

            Assert.Equal(@"C:\Logos\firma.png", gaben["Logo"]);

            var ohne = new BerichtsvorlagenCtrl(_pfade, new FluechtigeEinstellungen(), () => "Probe GmbH");
            Assert.Equal("", EinstellungenBerichtGaben.Gaben(ohne)["Logo"]);
        }

        /// <summary>
        /// Der Rückweg schreibt über den Controller den Pfad getrimmt — auch zu einer Datei, die es nicht
        /// gibt; leer heißt „ohne Logo" und entfernt die Einstellung.
        /// </summary>
        [Fact]
        public async Task Der_Rueckweg_schreibt_die_Einstellung_auch_ohne_Datei()
        {
            var logo = (EventCallback<string>)Gaben()["LogoChanged"];

            string fehlt = Path.Combine(_wurzel, "gibt-es-nicht.png");
            await logo.InvokeAsync("  " + fehlt + " ");
            Assert.Equal(fehlt, _einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_LOGO, null));
            Assert.Equal(fehlt, _vorlagen.LogoPfad);
            Assert.Equal(fehlt, Gaben()["Logo"]);

            await logo.InvokeAsync("");
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_LOGO, null));
            Assert.Null(_vorlagen.LogoPfad);
            Assert.Equal("", Gaben()["Logo"]);
        }

        /// <summary>Die Dateiwahl geht über <c>Dienste.Datei</c> mit Titel und Bildfilter; abgebrochen = leer.</summary>
        [Fact]
        public async Task Die_Dateiwahl_nimmt_Dienste_Datei_mit_dem_Bildfilter()
        {
            var probe = new Dateiprobe { Antwort = @"D:\Bilder\logo.jpg" };
            Dienste.Datei = probe;
            var waehler = (Func<string, Task<string>>)Gaben()["LogoWaehler"];

            Assert.Equal(@"D:\Bilder\logo.jpg", await waehler(""));
            Assert.Equal(R.EIN_BERICHT_DLG_LOGO, probe.Titel);
            Assert.Equal(R.EIN_BERICHT_LOGO_FILTER, probe.Filter);
            Assert.Contains("*.png", probe.Filter);
            Assert.Contains("*.jpg", probe.Filter);
            Assert.Contains("*.jpeg", probe.Filter);

            probe.Antwort = "";
            Assert.Equal("", await waehler(""));
        }

        [Fact]
        public void Die_Pruefung_findet_nur_vorhandene_Dateien()
        {
            string da = Path.Combine(_wurzel, "logo.png");
            File.WriteAllBytes(da, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            var vorhanden = (Func<string, bool>)Gaben()["LogoVorhanden"];

            Assert.True(vorhanden(da));
            Assert.True(vorhanden(" " + da + " "));
            Assert.False(vorhanden(Path.Combine(_wurzel, "fehlt.png")));
            Assert.False(vorhanden(""));
            Assert.False(vorhanden("<:>|?"));
        }

        private sealed class Probepfade : StandardPfade
        {
            private readonly string _dokumente;
            private readonly string _vorlagen;

            public Probepfade(string dokumente, string vorlagen)
            {
                _dokumente = dokumente;
                _vorlagen = vorlagen;
            }

            public override string Dokumente { get { return _dokumente; } }

            public override string Berichtsvorlagen { get { return _vorlagen; } }
        }

        /// <summary>Eine Dateiwahl-Attrappe: feste Antwort, Titel und Filter gemerkt.</summary>
        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Antwort = "";
            internal string Titel = "";
            internal string Filter = "";

            public string DateiOeffnen(string titel, string filter, string startOrdner)
            {
                Titel = titel ?? "";
                Filter = filter ?? "";
                return Antwort;
            }

            public string DateiSpeichern(string titel, string filter, string vorschlag) => "";

            public string OrdnerWaehlen(string titel, string startOrdner) => "";

            public bool MitSystemOeffnen(string pfad) => false;
        }
    }
}
