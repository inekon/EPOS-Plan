using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Export;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Gebäudeexports</b> (<c>EPOS.UI.Daten/Bedarf/GebaeudeExportHuelle.cs</c>, Stufe G7a
    /// Welle W3): der Satz wird einmal gelesen, der Plan folgt der Postleitzahl, Grundcodes erscheinen als
    /// Text, eine Ablehnung fragt keine Dateiwahl, eine abgebrochene Dateiwahl schreibt nichts, geschrieben
    /// wird eine ganze Datei mit Pfad in der Rückmeldung, auf iOS mit Teilen und Gebäude-ID im Namen. Der
    /// Satz kommt aus <see cref="ExportSatzProbe"/> — ohne Datenbank; nur die Verdrahtung im Gebäudedialog
    /// liest die Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]   // die Fälle tauschen Dienste.Datei — prozessweiter Zustand
    public sealed class GebaeudeExportHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly string _ordner = Path.Combine(Path.GetTempPath(), "epos-gexp-" + Guid.NewGuid().ToString("N"));

        public GebaeudeExportHuelleTests()
        {
            Directory.CreateDirectory(_ordner);
        }

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            _kultur.Dispose();
            try { Directory.Delete(_ordner, recursive: true); } catch (IOException) { }
        }

        // =====================================================================
        //  Vorrichtung
        // =====================================================================

        private static GebaeudeExportSatz Haus(params BauteilModel[] weitere)
        {
            ZoneModel zone = ExportSatzProbe.UWertHaus();
            zone.Bauteile.AddRange(weitere);
            return ExportSatzProbe.Satz(zone, plz: null);
        }

        private static GebaeudeExportHuelle Huelle(GebaeudeExportSatz satz, bool ios = false, Action gelesen = null)
            => new GebaeudeExportHuelle(1, 1, "Probe/Haus: Nord", ios,
                                        (p, z) => { gelesen?.Invoke(); return satz; },
                                        () => GbxmlExportProbe.Profil());

        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Antwort = "";
            internal string Titel = "";
            internal string Filter = "";
            internal string Vorschlag = "";
            internal int Wahlen;
            internal bool TeilenAntwort = true;
            internal readonly List<string> Geteilt = new();

            public string DateiOeffnen(string titel, string filter, string startOrdner) => "";

            public string DateiSpeichern(string titel, string filter, string vorschlag)
            {
                Wahlen++;
                Titel = titel ?? "";
                Filter = filter ?? "";
                Vorschlag = vorschlag ?? "";
                return Antwort;
            }

            public string OrdnerWaehlen(string titel, string startOrdner) => "";

            public bool MitSystemOeffnen(string pfad)
            {
                Geteilt.Add(pfad);
                return TeilenAntwort;
            }
        }

        // =====================================================================
        //  Parametersatz und Texte
        // =====================================================================

        [Fact]
        public void Der_Parametersatz_trifft_die_Parameter_des_Exportdialogs()
        {
            IReadOnlyDictionary<string, object> gaben = Huelle(Haus()).Gaben(geaendert: true);
            foreach (string schluessel in gaben.Keys)
            {
                var eigenschaft = typeof(GebaeudeExportDialog).GetProperty(schluessel);
                Assert.True(eigenschaft != null && Attribute.IsDefined(eigenschaft,
                                typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                            "GebaeudeExportDialog führt keinen [Parameter] " + schluessel);
            }
            Assert.True((bool)gaben["GespeicherterStand"]);
        }

        [Fact]
        public void Auf_iOS_heissen_Knopf_und_Speichern_mit_teilen()
        {
            Assert.Equal(R.GEXP_BTN_EXPORT_IOS, GebaeudeExportHuelle.Knopftext(ios: true));
            Assert.Equal(R.GEXP_BTN_EXPORT, GebaeudeExportHuelle.Knopftext(ios: false));
            Assert.Equal(R.GEXP_BTN_SPEICHERN_IOS, GebaeudeExportHuelle.Texte(ios: true).Speichern);
            Assert.Equal(R.GEXP_BTN_SPEICHERN, GebaeudeExportHuelle.Texte(ios: false).Speichern);
            Assert.Contains("teilen", R.GEXP_BTN_EXPORT_IOS, StringComparison.Ordinal);
        }

        [Fact]
        public void Eine_Zeile_ohne_Projektkopie_bekommt_keinen_Parametersatz()
        {
            var ungespeichert = new GebaeudeProjektZeile { IdZ = GebaeudeHuelle.STARTINDEX, Name = "Neu", HatProjektkopie = false };
            Assert.Null(GebaeudeExportHuelle.Gaben(1045, ungespeichert, geaendert: false));

            // Auch mit gesetztem Kennzeichen: eine vorläufige Id hat keine Projektkopie.
            var vorlaeufig = new GebaeudeProjektZeile { IdZ = GebaeudeHuelle.STARTINDEX + 3, Name = "Neu", HatProjektkopie = true };
            Assert.Null(GebaeudeExportHuelle.Gaben(1045, vorlaeufig, geaendert: false));
            Assert.Null(GebaeudeExportHuelle.Gaben(0, new GebaeudeProjektZeile { IdZ = 7, HatProjektkopie = true }, false));
        }

        [Fact]
        public void Der_Dateivorschlag_ist_ein_gueltiger_Name_und_traegt_auf_iOS_die_Gebaeude_ID()
        {
            Assert.Equal("Probe_Haus_ Nord.xml", GebaeudeExportHuelle.Dateivorschlag("Probe/Haus: Nord", 501, ios: false));
            Assert.Equal("Probe_Haus_ Nord_501.xml", GebaeudeExportHuelle.Dateivorschlag("Probe/Haus: Nord", 501, ios: true));
            Assert.Equal(GebaeudeExportProfil.PROGRAMMNAME + ".xml", GebaeudeExportHuelle.Dateivorschlag("  ", 0, ios: true));
        }

        // =====================================================================
        //  Vorbereiten
        // =====================================================================

        [Fact]
        public async Task Der_Satz_wird_einmal_gelesen_und_der_Plan_folgt_der_Postleitzahl()
        {
            int gelesen = 0;
            GebaeudeExportHuelle h = Huelle(Haus(), gelesen: () => gelesen++);

            GebaeudeExportAnsicht ohne = await h.Vorbereiten("");
            GebaeudeExportAnsicht mit = await h.Vorbereiten(" 01067 ");

            Assert.Equal(1, gelesen);
            Assert.Equal(1, h.Lesungen);
            Assert.False(ohne.Abgelehnt);
            Assert.Contains(ohne.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.OHNE_ORT);
            Assert.DoesNotContain(mit.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.OHNE_ORT);
            GebaeudeExportMeldung ort = ohne.Meldungen.First(m => m.Schluessel == GebaeudeExportAblauf.OHNE_ORT);
            Assert.Equal(R.GEXP_PROT_OHNE_ORT, ort.Text);
            Assert.Equal(WarnStufe.Hinweis, ort.Stufe);
            Assert.Equal(R.IMPORT_STUFE_INFO, ort.Stufentext);
        }

        [Fact]
        public async Task Ein_Grundcode_erscheint_als_Text_und_nicht_als_Kennung()
        {
            BauteilModel dach = ExportSatzProbe.B(2099, DbWerte.BAUTEILART_DACH, 20.0, DbWerte.RANDBEDINGUNG_UNBEHEIZT,
                                                  u: 0.3, neigung: 0.0);
            GebaeudeExportAnsicht a = await Huelle(Haus(dach)).Vorbereiten("");

            GebaeudeExportMeldung wechsel = Assert.Single(a.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.WECHSEL);
            Assert.Contains(R.GEXP_GRUND_DACH_ALS_DECKE, wechsel.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(GbxmlUmkehrung.GRUND_DACH_ALS_DECKE, wechsel.Text, StringComparison.Ordinal);

            // Ein gewöhnlicher Wert bleibt, wie er ist - auch ein Wort in Großbuchstaben ohne Text.
            Assert.Equal("Bauteil 7", GebaeudeExportHuelle.Grundtext("Bauteil 7"));
            Assert.Equal("KEIN_SOLCHER_CODE", GebaeudeExportHuelle.Grundtext("KEIN_SOLCHER_CODE"));
        }

        [Fact]
        public async Task Eine_Ablehnung_nennt_den_Grund_und_fragt_keine_Dateiwahl()
        {
            var datei = new Dateiprobe { Antwort = Path.Combine(_ordner, "nie.xml") };
            Dienste.Datei = datei;
            GebaeudeExportHuelle h = Huelle(new GebaeudeExportSatz { IdProjekt = 1, IdZ = 1 });

            GebaeudeExportAnsicht a = await h.Vorbereiten("");
            Assert.True(a.Abgelehnt);
            Assert.Equal(R.GEXP_PROT_KEIN_GEBAEUDE, a.Ablehnung);

            GebaeudeExportErgebnis e = await h.Speichern("");
            Assert.False(e.Gespeichert);
            Assert.False(e.Abgebrochen);
            Assert.Equal(0, datei.Wahlen);
            Assert.False(File.Exists(datei.Antwort));
        }

        [Fact]
        public async Task Ein_Lesefehler_wird_zur_benannten_Ablehnung()
        {
            var h = new GebaeudeExportHuelle(1, 1, "X", ios: false,
                                             (p, z) => throw new InvalidOperationException("Probe"),
                                             () => GbxmlExportProbe.Profil());
            GebaeudeExportAnsicht a = await h.Vorbereiten("");
            Assert.True(a.Abgelehnt);
            Assert.Contains("Probe", a.Ablehnung, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Speichern
        // =====================================================================

        [Fact]
        public async Task Eine_abgebrochene_Dateiwahl_schreibt_nichts()
        {
            var datei = new Dateiprobe { Antwort = "" };
            Dienste.Datei = datei;

            GebaeudeExportErgebnis e = await Huelle(Haus()).Speichern("");

            Assert.True(e.Abgebrochen);
            Assert.False(e.Gespeichert);
            Assert.Equal(1, datei.Wahlen);
            Assert.Equal(R.GEXP_DATEIDIALOG_TITEL, datei.Titel);
            Assert.Equal(R.GEXP_DATEIFILTER, datei.Filter);
            Assert.Empty(Directory.GetFiles(_ordner));
        }

        [Fact]
        public async Task Unter_Windows_wird_die_ganze_Datei_geschrieben_und_nicht_geteilt()
        {
            string ziel = Path.Combine(_ordner, "haus.xml");
            var datei = new Dateiprobe { Antwort = ziel };
            Dienste.Datei = datei;

            GebaeudeExportErgebnis e = await Huelle(Haus()).Speichern("01067");

            Assert.True(e.Gespeichert, e.Text);
            Assert.Contains(ziel, e.Text, StringComparison.Ordinal);
            Assert.Equal("Probe_Haus_ Nord.xml", datei.Vorschlag);
            Assert.Empty(datei.Geteilt);
            long laenge = new FileInfo(ziel).Length;
            Assert.True(laenge > 0);
            Assert.Contains(laenge.ToString(System.Globalization.CultureInfo.CurrentCulture), e.Text, StringComparison.Ordinal);
            XDocument doc = XDocument.Load(ziel);
            Assert.Equal("gbXML", doc.Root!.Name.LocalName);
            Assert.NotNull(doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Location"));
        }

        [Fact]
        public async Task Auf_iOS_folgt_das_Teilen_und_ein_Fehlschlag_nennt_den_Pfad()
        {
            string ziel = Path.Combine(_ordner, "haus_ios.xml");
            var datei = new Dateiprobe { Antwort = ziel };
            Dienste.Datei = datei;

            GebaeudeExportErgebnis e = await Huelle(Haus(), ios: true).Speichern("");
            Assert.True(e.Gespeichert, e.Text);
            Assert.Equal(ziel, Assert.Single(datei.Geteilt));
            Assert.EndsWith("_" + ExportSatzProbe.GEB + ".xml", datei.Vorschlag, StringComparison.Ordinal);

            datei.TeilenAntwort = false;
            GebaeudeExportErgebnis f = await Huelle(Haus(), ios: true).Speichern("");
            Assert.True(f.Gespeichert);
            Assert.Equal(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.GEXP_MSG_TEILEN_FEHLER, ziel), f.Text);
        }

        [Fact]
        public async Task Ein_Schreibfehler_wird_gemeldet()
        {
            string ziel = Path.Combine(_ordner, "fehlt", "unter", "haus.xml");   // Ordner gibt es nicht
            Dienste.Datei = new Dateiprobe { Antwort = ziel };

            GebaeudeExportErgebnis e = await Huelle(Haus()).Speichern("");

            Assert.False(e.Gespeichert);
            Assert.False(e.Abgebrochen);
            Assert.StartsWith(R.GEXP_MSG_FEHLER.Substring(0, R.GEXP_MSG_FEHLER.IndexOf('{')), e.Text, StringComparison.Ordinal);
            Assert.False(File.Exists(ziel));
        }

        // =====================================================================
        //  Die Verdrahtung im Gebäudedialog (Testdatenbank)
        // =====================================================================

        /// <summary>
        /// Bei angeschaltetem Freigabeschalter reicht die Hülle des Gebäudedialogs den Delegaten; eine Zeile
        /// mit Projektkopie bekommt einen Parametersatz, eine vorläufige keinen.
        /// </summary>
        [Fact]
        public void Der_Gebaeudedialog_bekommt_den_Export_nur_mit_Freigabeschalter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int PROJEKT = 1045;
            IReadOnlyDictionary<string, object> gaben =
                GebaeudeHuelle.Gaben(PROJEKT, "", Z_ProjGebCtrl.LiesProjekt(PROJEKT), wizard: false);

            Assert.Equal(GebaeudeExportRegeln.GbxmlExportFreigegeben, gaben.ContainsKey("ExportGaben"));
            if (!GebaeudeExportRegeln.GbxmlExportFreigegeben) return;

            var export = (Func<GebaeudeProjektZeile, bool, IReadOnlyDictionary<string, object>>)gaben["ExportGaben"];
            var zeilen = (List<GebaeudeProjektZeile>)gaben["Zeilen"];
            GebaeudeProjektZeile mitKopie = zeilen.First(z => z.HatProjektkopie);
            Assert.NotNull(export(mitKopie, false));
            Assert.Null(export(new GebaeudeProjektZeile { IdZ = GebaeudeHuelle.STARTINDEX }, false));
            Assert.Equal(GebaeudeExportHuelle.Knopftext(), (string)gaben["BtnExportText"]);
        }
    }
}
