using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Gebäudeimports</b> (<c>EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs</c>,
    /// Stufe G4c Welle 2) — ohne Datenbank: Parametersatz gegen die Parameter der Komponente,
    /// Lesen im Arbeitsfaden samt Fortschritt, Zuordnen, Prüfen mit Handänderungen und Namen, die
    /// Größenablehnung vor dem Lesen, die Quelle für die spätere Persistenz und die Abbildung
    /// <see cref="GebaeudeImportHuelle.NachKatalogdaten"/> auf die Felder des Gebäudeeditors am
    /// Probenhaus <c>Referenzlaeufe/Importproben/gbxml_haus_si.xml</c>.
    /// </summary>
    [Collection("Testdatenbank")]   // ein Fall tauscht Dienste.Datei — prozessweiter Zustand
    public sealed class GebaeudeImportHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        private static Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>> Lesen(
            IReadOnlyDictionary<string, object> gaben)
            => (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];

        private static Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> Zuordnen(IReadOnlyDictionary<string, object> gaben)
            => (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];

        private static Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>> Pruefen(IReadOnlyDictionary<string, object> gaben)
            => (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];

        /// <summary>Liest das Probenhaus über den Parametersatz und ordnet es mit Klasse F (1969 bis 1978, E47) zu.</summary>
        private static async Task<(GebaeudeImportHuelle Huelle, IReadOnlyDictionary<string, object> Gaben, GebaeudeImportStand Stand)> Probenhaus()
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            GebaeudeLesestand gelesen = await Lesen(gaben)(GbxmlImportTests.Probe("gbxml_haus_si.xml"), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            GebaeudeImportStand stand = Zuordnen(gaben)(new GebaeudeZuordnungsanfrage(0, 5, Keine));
            return (h, gaben, stand);
        }

        /// <summary>
        /// Nacharbeit G4b: Die Bauteilliste zeigt den Azimut auf eine Nachkommastelle, das Bauteil des Vorschlags
        /// behält den Wert der Datei — am IFC-Probenhaus, dessen Lageplan gegen Nord gedreht ist.
        /// </summary>
        [Fact]
        public async Task Die_Bauteilliste_rundet_den_Azimut_nur_in_der_Anzeige()
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            GebaeudeLesestand gelesen = await Lesen(gaben)(GbxmlImportTests.Probe("ifc4_haus.ifc"), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            GebaeudeImportStand stand = Zuordnen(gaben)(new GebaeudeZuordnungsanfrage(0, null, Keine));

            Dictionary<string, string> anzeige = stand.Bauteile!.Zeilen.ToDictionary(d => d.Kennung, d => d.Azimut, StringComparer.Ordinal);
            List<double> fein = new();
            foreach (GebaeudeBauteilzeile z in h.Vorschlag.Zeilen)
            {
                if (z.Bauteil.Azimut is not double w) continue;
                Assert.Equal(GebaeudeImportHuelle.AzimutText(w), anzeige[z.Kennung]);
                if (Math.Round(w, 1) != w) fein.Add(w);
            }
            Assert.NotEmpty(fein);
            Assert.Contains(fein, w => Math.Round(w, 3) != Math.Round(w, 1));   // mehr als eine Stelle im Bauteil
        }

        private static GebaeudeImportErgebnis Ergebnis(GebaeudeImportStand stand, IEnumerable<GebaeudeFeldzeileDaten> zeilen = null,
                                                       string name = null, int? klasse = 5)   // E47: F (1969 bis 1978)
            => new GebaeudeImportErgebnis(0, klasse, name ?? stand.Vorschlagsname, Keine, (zeilen ?? stand.Zeilen).ToList());

        // =================================================================================
        //  Parametersatz
        // =================================================================================

        [Fact]
        public void Der_Parametersatz_trifft_nur_Parameter_der_Komponente()
        {
            var parameter = new HashSet<string>(typeof(GebaeudeImportDialog).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null).Select(p => p.Name));
            var h = new GebaeudeImportHuelle(new GbxmlImportProfil());   // ein festes Profil

            IReadOnlyDictionary<string, object> ohne = h.Gaben();
            Assert.All(ohne.Keys, k => Assert.Contains(k, parameter));
            Assert.False(ohne.ContainsKey("Uebernehmen"));   // kein Delegat des Wirts — OK weich gesperrt
            foreach (KeyValuePair<string, object> e in ohne)
                Assert.True(typeof(GebaeudeImportDialog).GetProperty(e.Key)!.PropertyType.IsInstanceOfType(e.Value), e.Key);

            IReadOnlyDictionary<string, object> mit = h.Gaben(_ => Task.FromResult<string>(null));
            Assert.True(typeof(GebaeudeImportDialog).GetProperty("Uebernehmen")!.PropertyType.IsInstanceOfType(mit["Uebernehmen"]));

            var profil = (GebaeudeImportProfilDaten)ohne["Profil"];
            Assert.Equal("gbXML", profil.Formatname);
            Assert.Equal(GbxmlImportProfil.DATEIFILTER, profil.Dateifilter);
            Assert.Equal("25 MB", profil.Groessengrenze);   // der Prüfstand ist nicht iOS
            Assert.Equal(new[] { "X4 – eine Zone je Gebäude" }, profil.Zonierungsregeln);
            Assert.Equal(GbxmlImportProfil.HILFESCHLUESSEL, profil.HilfeSchluessel);
            Assert.Equal(13, ((IReadOnlyList<string>)ohne["Baualtersklassen"]).Count);   // E47: A bis M
            Assert.Equal(GbxmlImportProfil.MAX_BYTES_WINDOWS, h.Profil.MaxBytes);

            // Die Herkunftsschlüssel, die die Komponente selbst setzt, sind die des Kerns.
            Assert.Equal(ImportherkunftWerte.MANUELL, GebaeudeHerkunftSchluessel.Manuell);
            Assert.Equal(GebaeudeZuordnungsModell.HERKUNFT_LEER, GebaeudeHerkunftSchluessel.Leer);
        }

        // =================================================================================
        //  Profil nach Dateiwahl (Stufe G4, Welle 4)
        // =================================================================================

        [Fact]
        public void Ohne_festes_Profil_bietet_die_Huelle_beide_Formate_an()
        {
            var h = new GebaeudeImportHuelle();
            Assert.True(h.ProfilNachDatei);
            Assert.Null(h.Profil);                                  // entschieden wird an der Datei
            Assert.Equal(GebaeudeImportProfil.DATEIFILTER_ALLE, h.Dateifilter);

            var profil = (GebaeudeImportProfilDaten)h.Gaben()["Profil"];
            Assert.Equal("gbXML, IFC", profil.Formatname);
            Assert.Equal(GebaeudeImportProfil.DATEIFILTER_ALLE, profil.Dateifilter);
            Assert.Equal("gbXML 25 MB · IFC 50 MB", profil.Groessengrenze);   // der Prüfstand ist nicht iOS
            Assert.Equal(new[] { "X4 – eine Zone je Gebäude", GebaeudeZuordnungsModell.ZonenregelText(IfcImportProfil.ZONENREGEL_Z5) },
                         profil.Zonierungsregeln);
            Assert.Equal(GebaeudeImportProfil.HILFE_ZUORDNUNG, profil.HilfeSchluessel);
        }

        [Fact]
        public async Task Die_Dateiwahl_waehlt_das_Profil_nach_der_Endung()
        {
            IDateiDienst vorher = Dienste.Datei;
            try
            {
                var probe = new Dateiprobe { Antwort = GbxmlImportTests.Probe("gbxml_haus_si.xml") };
                Dienste.Datei = probe;
                var h = new GebaeudeImportHuelle();
                var waehlen = (Func<string, Task<GebaeudeDateiwahl>>)h.Gaben()["DateiWaehlen"];

                Assert.Null((await waehlen("")).Ablehnung);
                Assert.Equal(GebaeudeImportProfil.DATEIFILTER_ALLE, probe.Filter);   // EINE Dateiwahl für beide

                probe.Antwort = Path.Combine(IfcProbenTests.Ordner(), "ifc4_haus.ifc");
                GebaeudeDateiwahl ifc = await waehlen("");
                Assert.Null(ifc.Ablehnung);
                Assert.Equal("ifc4_haus.ifc", ifc.Dateiname);

                probe.Antwort = Path.Combine(Path.GetTempPath(), "haus-probe.txt");
                GebaeudeDateiwahl txt = await waehlen("");
                Assert.Equal("Dateiart nicht unterstützt: „haus-probe.txt“. Gelesen werden gbXML-Dateien (.xml, .gbxml) " +
                             "und IFC-Dateien (.ifc, .ifcxml, .ifczip).", txt.Ablehnung);
            }
            finally
            {
                Dienste.Datei = vorher;
            }
        }

        [Fact]
        public async Task Lesen_nimmt_das_Profil_der_Datei()
        {
            var h = new GebaeudeImportHuelle();
            var lesen = Lesen(h.Gaben());

            GebaeudeLesestand ifc = await lesen(Path.Combine(IfcProbenTests.Ordner(), "ifc4_haus.ifc"), null, CancellationToken.None);
            Assert.True(ifc.Gelesen, string.Join(" | ", ifc.Meldungen.Select(m => m.Text)));
            Assert.Equal("IFC", ifc.Kopf!.Format);
            Assert.IsType<IfcImportProfil>(h.Profil);
            Assert.Equal(IfcImportProfil.MAX_BYTES_WINDOWS, h.Profil.MaxBytes);
            Assert.Equal(DbWerte.IMPORT_FORMAT_IFC, h.Quelle.Format);
            Assert.Equal("", ifc.SchonImportiert);                     // ohne Projekt kein Hinweis

            GebaeudeLesestand gbxml = await lesen(GbxmlImportTests.Probe("gbxml_haus_si.xml"), null, CancellationToken.None);
            Assert.True(gbxml.Gelesen);
            Assert.Equal("gbXML", gbxml.Kopf!.Format);
            Assert.IsType<GbxmlImportProfil>(h.Profil);

            GebaeudeLesestand txt = await lesen(Path.Combine(Path.GetTempPath(), "haus-probe.txt"), null, CancellationToken.None);
            Assert.False(txt.Gelesen);
            GebaeudeImportMeldung m = Assert.Single(txt.Meldungen);
            Assert.Equal("GIMP_DLG_DATEIART", m.Kennung);
            Assert.Equal(WarnStufe.Fehler, m.Stufe);
        }

        [Fact]
        public async Task Nach_der_Pruefung_stehen_Herkunft_und_Vorbelegung_bereit()
        {
            Assert.Null(new GebaeudeImportHuelle().Herkunft);         // ohne Lauf keine Herkunft
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportStand stand) = await Probenhaus();
            Pruefen(gaben)(Ergebnis(stand, name: "Neubau A"));

            GebaeudeImportHerkunft herkunft = h.Herkunft;
            Assert.NotNull(herkunft);
            Assert.Same(h.Quelle, herkunft.Quelle);
            GebaeudeQuellzuordnung p = Assert.Single(herkunft.Paarungen);
            Assert.Equal("Building", p.Quelltyp);
            Assert.Equal("geb-1", p.Quellkennung);

            GebaeudeVorbelegung v = h.Vorbelegung(Ergebnis(stand, name: "Neubau A"));
            // Die Herleitungszeile nennt Datei und Format und danach jede übernommene Vorgabe.
            Assert.Equal("Vorbelegt aus dem Import: Datei gbxml_haus_si.xml, Format gbXML. Vorgaben, nicht aus der Datei: "
                         + "Interne Wärmegewinne 600 W; ψ Anschluss Fenster–Wand 0,09 W/(mK); ψ Anschluss Wand–Dach 0,3 W/(mK); "
                         + "ψ Anschluss Außenwand–Keller 0,6 W/(mK); Luftwechselrate 0,7 1/h; Heizsollwert in der Nacht 18 °C; "
                         + "Nachtabsenkung von 22 h; Nachtabsenkung bis 6 h.", v.Herleitung);
            Assert.Equal("Neubau A", v.Daten.Name);
            Assert.Equal(120.0, v.Daten.WohnflaecheGesamt);
            // Was nicht aus der Datei kommt, steht wie im Modus Neu des Editors.
            GebaeudeKatalogDaten neu = GebaeudeKatalogHuelle.AusModell(new GebaeudeModel());
            Assert.Equal(neu.Verwendung, v.Daten.Verwendung);
            Assert.Equal(neu.MaxTemperatur, v.Daten.MaxTemperatur);
            Assert.Equal(neu.Ferienbeginn, v.Daten.Ferienbeginn);
        }

        [Fact]
        public void Der_Zeitpunkt_erscheint_in_der_Anzeigekultur()
        {
            Assert.Equal("25.09.2026 10:00", GebaeudeImportHuelle.Zeitpunkttext("2026-09-25T10:00:00+02:00"));
            Assert.Equal("kein Zeitpunkt", GebaeudeImportHuelle.Zeitpunkttext("kein Zeitpunkt"));
        }

        [Fact]
        public async Task Ohne_Projekt_fragt_die_Huelle_keine_Datenbank()
        {
            // Der Wirt der Rasterprobe stellt den Dialog OHNE Datenbank (Proben/Rasterprobe, Seite
            // /gebaeudeimport): Vom Dateiwähler bis zur Vorbelegung des Editors erreicht kein Schritt
            // die Zugriffsschicht - auch der Hinweis „schon importiert" nicht, der erst mit einem
            // Projekt fragt.
            IDatenzugriff vorherZugriff = DataRepository.Zugriff;
            IDateiDienst vorherDatei = Dienste.Datei;
            var zugriffe = new Zaehlzugriff(vorherZugriff);
            try
            {
                DataRepository.Zugriff = zugriffe;
                Dienste.Datei = new Dateiprobe { Antwort = GbxmlImportTests.Probe("gbxml_haus_si.xml") };
                var h = new GebaeudeImportHuelle();
                IReadOnlyDictionary<string, object> gaben = h.Gaben(_ => Task.FromResult<string>(null));

                GebaeudeDateiwahl wahl = await ((Func<string, Task<GebaeudeDateiwahl>>)gaben["DateiWaehlen"])("");
                Assert.Null(wahl.Ablehnung);
                GebaeudeLesestand gelesen = await Lesen(gaben)(wahl.Pfad, null, CancellationToken.None);
                Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
                Assert.Equal("", gelesen.SchonImportiert);
                GebaeudeImportStand stand = Zuordnen(gaben)(new GebaeudeZuordnungsanfrage(0, 5, Keine));
                GebaeudeImportErgebnis ergebnis = Ergebnis(stand, name: "Probe ohne Datenbank");
                Pruefen(gaben)(ergebnis);
                Assert.Equal("Probe ohne Datenbank", h.Vorbelegung(ergebnis).Daten.Name);

                Assert.Equal(0, zugriffe.Gesamt);
            }
            finally
            {
                DataRepository.Zugriff = vorherZugriff;
                Dienste.Datei = vorherDatei;
            }
        }

        [Fact]
        public async Task Die_Plattform_der_Groessengrenze_laesst_sich_einstellen()
        {
            // Die Schalen lassen sie weg (die laufende Plattform); ein Prüfstand zeigt die Grenze von iOS.
            var ios = new GebaeudeImportHuelle(ios: true);
            Assert.Equal("gbXML 25 MB · IFC 20 MB", ((GebaeudeImportProfilDaten)ios.Gaben()["Profil"]).Groessengrenze);
            GebaeudeLesestand ifc = await Lesen(ios.Gaben())(Path.Combine(IfcProbenTests.Ordner(), "ifc4_haus.ifc"), null, CancellationToken.None);
            Assert.True(ifc.Gelesen, string.Join(" | ", ifc.Meldungen.Select(m => m.Text)));
            Assert.Equal(IfcImportProfil.MAX_BYTES_IOS, ios.Profil.MaxBytes);

            Assert.Equal(IfcImportProfil.MAX_BYTES_IOS, new GebaeudeImportHuelle(new IfcImportProfil(), ios: true).Profil.MaxBytes);
            Assert.Equal(IfcImportProfil.MAX_BYTES_WINDOWS, new GebaeudeImportHuelle(new IfcImportProfil(), ios: false).Profil.MaxBytes);
            Assert.Equal("gbXML 25 MB · IFC 50 MB",
                         ((GebaeudeImportProfilDaten)new GebaeudeImportHuelle(ios: false).Gaben()["Profil"]).Groessengrenze);
        }

        // =================================================================================
        //  Lesen, Zuordnen, Prüfen
        // =================================================================================

        [Fact]
        public async Task Lesen_liefert_Kopf_Gebaeude_und_Fortschritt()
        {
            var h = new GebaeudeImportHuelle();
            var schritte = new Sammler();
            GebaeudeLesestand gelesen = await Lesen(h.Gaben())(GbxmlImportTests.Probe("gbxml_haus_si.xml"), schritte, CancellationToken.None);

            Assert.True(gelesen.Gelesen);
            Assert.Equal(new[] { "Haus 1" }, gelesen.Gebaeude);
            Assert.Equal("gbxml_haus_si.xml", gelesen.Kopf!.Dateiname);
            Assert.Equal("gbXML", gelesen.Kopf.Format);
            Assert.Equal("gbXML-Version 0.37", gelesen.Kopf.Schema);
            Assert.Equal("20,1 KB", gelesen.Kopf.Groesse);
            Assert.Equal("X4 – eine Zone je Gebäude", gelesen.Kopf.Zonenregel);
            Assert.Contains(schritte.Texte, t => t == "Die Gebäudedatei gbxml_haus_si.xml wird gelesen …");
            Assert.Contains(schritte.Texte, t => t == "1 Gebäude gelesen.");

            // Die Quelle steht für die spätere Persistenz bereit.
            Assert.Equal("gbxml_haus_si.xml", h.Quelle.Dateiname);
            Assert.Equal(64, h.Quelle.Hash.Length);
            Assert.Empty(h.Quellzuordnungen);   // vor der Zuordnung
        }

        [Fact]
        public async Task Zuordnen_baut_Zeilen_Raeume_und_Meldungen_mit_den_Texten_des_Modells()
        {
            (GebaeudeImportHuelle h, _, GebaeudeImportStand stand) = await Probenhaus();

            Assert.Equal("Haus 1", stand.Vorschlagsname);
            Assert.Equal("Manuell", stand.ManuellHerkunftText);
            Assert.StartsWith("gbxml_haus_si.xml · Gebäude „Haus 1“ · Baualtersklasse F", stand.Kopftext);
            Assert.Equal(GebaeudeZielfelder.Alle.Count, stand.Zeilen.Count);

            GebaeudeFeldzeileDaten nutz = stand.Zeilen.Single(z => z.Zielfeld == GebaeudeZielfelder.NUTZFLAECHE);
            Assert.Equal(120.0, nutz.Wert);
            Assert.Equal("Nutzfläche", nutz.Feld);
            Assert.Equal("Kenngrößen", nutz.Gruppe);
            Assert.Equal("gbXML-Datei", nutz.HerkunftText);
            Assert.Equal(ImportherkunftWerte.GBXML, nutz.HerkunftSchluessel);
            Assert.Equal("Summe aus 3 beheizten Räumen", nutz.Beleg);
            Assert.True(nutz.Eingebbar && nutz.Haken);

            GebaeudeFeldzeileDaten psi = stand.Zeilen.Single(z => z.Zielfeld == GebaeudeZielfelder.PSI_FENSTER_WAND);
            Assert.Equal("Vorgabe", psi.HerkunftText);
            Assert.Equal("0,09", psi.Vorgabe);
            Assert.StartsWith("Baualtersklasse F, Median aus", psi.VorgabeBeleg);

            GebaeudeFeldzeileDaten bauart = stand.Zeilen.Single(z => z.Zielfeld == GebaeudeZielfelder.BAUART);
            Assert.False(bauart.Eingebbar);
            Assert.Equal("Schwere Bauart", bauart.WertText);
            Assert.Equal(GebaeudeZielfelder.BAUART_SCHWER, bauart.Textwert);
            Assert.False(stand.Zeilen.Single(z => z.Zielfeld == GebaeudeZielfelder.FENSTER_GESAMT).HakenSetzbar);

            Assert.Equal(4, stand.Raeume.Count);
            GebaeudeRaumzeileDaten keller = stand.Raeume.Single(r => r.Kennung == "raum-keller");
            Assert.False(keller.Beheizt);
            Assert.Equal("60 m²", keller.Flaeche);
            Assert.Equal("Zustandsangabe der Datei: Unconditioned", keller.Grund);

            Assert.Contains(stand.Meldungen, m => m.Stufe == WarnStufe.Warnung && m.Kennung == "IMP_GBXML_PROT_KEIN_NORDEN");
            Assert.Contains(h.Quellzuordnungen, q => q.Quelltyp == "Building" && q.Quellkennung == "geb-1");
        }

        [Fact]
        public async Task Pruefen_legt_Handaenderungen_und_Haken_auf_den_Satz_und_fragt_den_Kern()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportStand stand) = await Probenhaus();

            Assert.DoesNotContain(Pruefen(gaben)(Ergebnis(stand)), m => m.Stufe == WarnStufe.Fehler);

            // Raumhöhe von Hand geleert, U-Wert Dach von Hand gesetzt, Name leer.
            var zeilen = stand.Zeilen.Select(z => z.Zielfeld switch
            {
                GebaeudeZielfelder.RAUMHOEHE => z with { Wert = null, HerkunftSchluessel = GebaeudeHerkunftSchluessel.Manuell },
                GebaeudeZielfelder.U_DACH => z with { Wert = 0.2, HerkunftSchluessel = GebaeudeHerkunftSchluessel.Manuell },
                _ => z,
            }).ToList();
            IReadOnlyList<GebaeudeImportMeldung> befund = Pruefen(gaben)(Ergebnis(stand, zeilen, name: " "));

            Assert.Contains(befund, m => m.Stufe == WarnStufe.Fehler && m.Kennung == "IMP_GEB_PROT_PFLICHT_FEHLT" && m.Text.StartsWith("Raumhöhe fehlt"));
            Assert.Contains(befund, m => m.Stufe == WarnStufe.Fehler && m.Kennung == "IMP_GEB_PROT_NAME_FEHLT");
            GebaeudeFeldzeile dach = h.Satz.Zeile(GebaeudeZielfelder.U_DACH);
            Assert.Equal(0.2, dach.Wert);
            Assert.Equal(Importherkunft.Manuell, dach.Herkunft);

            // Ein abgehakter Pflichtwert sperrt ebenso — dieselbe Regel wie im Kern.
            var ohneHaken = stand.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.NUTZFLAECHE ? z with { Haken = false } : z).ToList();
            Assert.Contains(Pruefen(gaben)(Ergebnis(stand, ohneHaken)), m => m.Kennung == "IMP_GEB_PROT_PFLICHT_FEHLT");
            Assert.False(h.Satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Uebernehmen);
        }

        [Fact]
        public async Task Eine_zu_grosse_Datei_liest_der_Ablauf_nicht()
        {
            var h = new GebaeudeImportHuelle(new GbxmlImportProfil());
            h.Profil.MaxBytes = 1000;
            GebaeudeLesestand gelesen = await Lesen(h.Gaben())(GbxmlImportTests.Probe("gbxml_haus_si.xml"), null, CancellationToken.None);

            Assert.False(gelesen.Gelesen);
            GebaeudeImportMeldung m = Assert.Single(gelesen.Meldungen);
            Assert.Equal("IMP_GBXML_PROT_ZU_GROSS", m.Kennung);
            Assert.Equal(WarnStufe.Fehler, m.Stufe);
            Assert.Null(h.Quelle);

            GebaeudeLesestand fehlt = await Lesen(h.Gaben())(Path.Combine(Path.GetTempPath(), "epos-gibt-es-nicht-" + Guid.NewGuid() + ".xml"),
                                                          null, CancellationToken.None);
            Assert.False(fehlt.Gelesen);
            Assert.Equal("IMP_GBXML_PROT_LESEFEHLER", Assert.Single(fehlt.Meldungen).Kennung);
        }

        [Fact]
        public async Task Die_Dateiwahl_lehnt_eine_zu_grosse_Datei_vor_dem_Lesen_ab()
        {
            IDateiDienst vorher = Dienste.Datei;
            try
            {
                var probe = new Dateiprobe { Antwort = GbxmlImportTests.Probe("gbxml_haus_si.xml") };
                Dienste.Datei = probe;
                var h = new GebaeudeImportHuelle(new GbxmlImportProfil());
                var waehlen = (Func<string, Task<GebaeudeDateiwahl>>)h.Gaben()["DateiWaehlen"];

                GebaeudeDateiwahl frei = await waehlen(GbxmlImportProfil.DATEIFILTER);
                Assert.Null(frei.Ablehnung);
                Assert.Equal("gbxml_haus_si.xml", frei.Dateiname);
                Assert.Equal(new FileInfo(probe.Antwort).Length, frei.Groesse);
                Assert.Equal("Gebäudedatei öffnen", probe.Titel);
                Assert.Equal(GbxmlImportProfil.DATEIFILTER, probe.Filter);

                h.Profil.MaxBytes = 1000;
                GebaeudeDateiwahl gross = await waehlen("");
                Assert.NotNull(gross.Ablehnung);
                Assert.Contains("größer als die Grenze von 1000 Byte", gross.Ablehnung);
                Assert.Equal(GbxmlImportProfil.DATEIFILTER, probe.Filter);   // leerer Filter: der des Profils

                probe.Antwort = "";
                Assert.Null(await waehlen(""));                               // abgebrochen
            }
            finally
            {
                Dienste.Datei = vorher;
            }
        }

        // =================================================================================
        //  Abbildung auf den Gebäudeeditor
        // =================================================================================

        [Fact]
        public async Task NachKatalogdaten_setzt_die_Felder_des_Gebaeudeeditors()
        {
            (_, _, GebaeudeImportStand stand) = await Probenhaus();
            var grundlage = new GebaeudeKatalogDaten { Name = "Alt", AnschlussWandDach = 10, Verwendung = "Wohngebaeude" };

            // Die leere Anschlusslänge mit Haken wird null — „leer → null", nicht geraten.
            var zeilen = stand.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.LAENGE_WAND_DACH ? z with { Haken = true } : z).ToList();
            GebaeudeKatalogDaten d = GebaeudeImportHuelle.NachKatalogdaten(grundlage, Ergebnis(stand, zeilen));

            Assert.Equal("Haus 1", d.Name);
            Assert.Equal(120.0, d.WohnflaecheGesamt);
            Assert.Equal(2.5, d.Raumhoehe);
            Assert.Equal(24.0, d.FlaecheNutzer);
            Assert.Equal(600.0, d.Waermegewinne);                 // innere Gewinne: Vorgabe 5 W/m² × 120 m² (E43), der Vorschlag nur im Beleg
            Assert.Equal(5, d.Baualtersklasse);                   // F
            Assert.Equal(Gebaeudebauweise.SCHWER, d.Bauart);
            Assert.Equal(120.0 * 50, d.Bauweise);                 // Nutzfläche × 50 — die Rechnung des Editors
            Assert.Equal(145.0, d.FlaecheAussenwand);
            Assert.Equal(0.4, d.UWertAussenwand);
            Assert.Equal(4.0, d.FensterflaecheNord);
            Assert.Equal(2.0, d.FensterflaecheOst);
            Assert.Equal(14.0, d.FensterflaecheSued);
            Assert.Equal(3.0, d.FensterflaecheWest);
            Assert.Equal(5.0, d.FensterflaecheOstWest);           // Ost + West
            Assert.Equal(1.1, d.UWertFenster!.Value, 12);
            Assert.Equal(0.6, d.Fensterdurchlassgrad!.Value, 12);
            Assert.Equal(60.0, d.Dachflaeche);
            Assert.Equal(0.25, d.UWertDachflaeche);
            Assert.Equal(60.0, d.Grundflaeche);
            Assert.Equal(0.4, d.UWertGrundflaeche);
            Assert.Equal(DbWerte.GRUND_KELLER, d.GrundflaecheRandbedingung);
            Assert.Equal(2.0, d.SonstigeFlaechen);
            Assert.Equal(1.8, d.UWertSonstiges);
            Assert.Equal(0.09, d.WbvkFensterWand);                // ψ aus der Vorgabe der Klasse F (U15)
            Assert.Equal(0.3, d.WbvkWandDach);
            Assert.Equal(0.6, d.WbvkAussenwandKeller);
            Assert.Null(d.AnschlussFensterWand);
            Assert.Null(d.AnschlussWandDach);                     // mit Haken: aus 10 wird null
            Assert.Null(d.AnschlussAussenwandKeller);
            Assert.Equal(0.5, d.LuftwechselInfiltration);
            Assert.Null(d.LuftwechselNutzer);                     // D12
            Assert.Equal(GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION + GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER,
                         d.Luftwechselrate);                      // die Pflichtangabe des Editors: Vorgabe 0,7 1/h
            Assert.Equal(20.0, d.SollTag);
            Assert.Equal(18.0, d.NachtAbsenkung);                 // Vorgabe 18 °C (E43)
            Assert.Equal(22, d.NachtBeginn);                      // Nachtzeit 22 bis 6 Uhr als ausdrückliche Werte
            Assert.Equal(6, d.NachtEnde);
            Assert.Equal("Wohngebaeude", d.Verwendung);           // kein Zielfeld — bleibt

            // Die Grundlage bleibt unberührt.
            Assert.Equal("Alt", grundlage.Name);
            Assert.Equal(10.0, grundlage.AnschlussWandDach);
            Assert.Null(grundlage.WohnflaecheGesamt);
        }

        [Fact]
        public async Task Ohne_Haken_bleibt_die_Grundlage_stehen()
        {
            (_, _, GebaeudeImportStand stand) = await Probenhaus();
            var grundlage = new GebaeudeKatalogDaten
            {
                Name = "Alt", WohnflaecheGesamt = 99, UWertAussenwand = 0.9, FensterflaecheOst = 1, FensterflaecheWest = 1,
                FensterflaecheOstWest = 2, Bauart = Gebaeudebauweise.LEICHT, Bauweise = 1980, Baualtersklasse = 7,
                GrundflaecheRandbedingung = null,
            };
            var ohneHaken = stand.Zeilen.Select(z => z with { Haken = false }).ToList();

            GebaeudeKatalogDaten d = GebaeudeImportHuelle.NachKatalogdaten(grundlage, Ergebnis(stand, ohneHaken, name: ""));

            Assert.Equal("Alt", d.Name);                           // ein leerer Name lässt den alten
            Assert.Equal(99.0, d.WohnflaecheGesamt);
            Assert.Equal(0.9, d.UWertAussenwand);
            Assert.Equal(2.0, d.FensterflaecheOstWest);
            Assert.Equal(Gebaeudebauweise.LEICHT, d.Bauart);
            Assert.Equal(1980.0, d.Bauweise);
            Assert.Equal(7, d.Baualtersklasse);
            Assert.Null(d.GrundflaecheRandbedingung);
            Assert.NotSame(grundlage, d);

            // Ohne Ergebnis: eine Kopie der Grundlage; ohne Grundlage: ein neuer Satz.
            Assert.Equal("Alt", GebaeudeImportHuelle.NachKatalogdaten(grundlage, null).Name);
            Assert.Equal(120.0, GebaeudeImportHuelle.NachKatalogdaten(null, Ergebnis(stand)).WohnflaecheGesamt);
        }

        // =================================================================================
        //  Texte der Oberfläche in beiden Sprachen
        // =================================================================================

        [Fact]
        public void Jeder_Ressourcenschluessel_des_Dialogs_steht_in_beiden_Sprachen()
        {
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");
            string wurzel = Wurzel();
            var dateien = new[]
            {
                Path.Combine(wurzel, "EPOS.UI", "Dialoge", "Import", "GebaeudeImportDialog.razor"),
                Path.Combine(wurzel, "EPOS.UI", "Dialoge", "Import", "GebaeudeImportDaten.cs"),
                Path.Combine(wurzel, "EPOS.UI.Daten", "Bedarf", "GebaeudeImportHuelle.cs"),
            };
            var schluessel = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string datei in dateien)
                foreach (Match m in Regex.Matches(File.ReadAllText(datei), @"\bResource\.([A-Z][A-Z0-9_]+)\b"))
                    schluessel.Add(m.Groups[1].Value);

            Assert.True(schluessel.Count >= 40, "Nur " + schluessel.Count + " Schlüssel gefunden.");
            var funde = new List<string>();
            foreach (string k in schluessel)
            {
                if (!de.TryGetValue(k, out string d) || d.Trim().Length == 0) funde.Add(k + ": fehlt in Resource.resx");
                if (!en.TryGetValue(k, out string e) || e.Trim().Length == 0) funde.Add(k + ": fehlt in Resource.en-US.resx");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        // =================================================================================
        //  Prüfstand
        // =================================================================================

        /// <summary>Ein Fortschrittsmelder, der synchron sammelt (der Arbeitsfaden meldet).</summary>
        private sealed class Sammler : IProgress<GebaeudeImportFortschritt>
        {
            private readonly List<string> _texte = new();

            public IReadOnlyList<string> Texte { get { lock (_texte) return _texte.ToList(); } }

            public void Report(GebaeudeImportFortschritt wert) { lock (_texte) _texte.Add(wert.Text); }
        }

        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Titel = "";
            internal string Filter = "";
            internal string Antwort = "";

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

        private static Dictionary<string, string> Resx(string datei)
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.Kern", "MyResource", datei));
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                d[m.Groups["k"].Value] = System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value);
            return d;
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
