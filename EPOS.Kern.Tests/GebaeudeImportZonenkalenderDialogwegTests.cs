using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Export;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Nachzug N7 — die Zonenkalender auf dem Weg des Zuordnungsdialogs</b> (Windows-Probe N4): Ein Gebäude des Projekts 1045
    /// mit Zonen, oberer Raumtemperatur 26 °C, einer Feiertagsperiode mit Verweis auf eine benannte Woche und einer eingebetteten
    /// Feiertagsperiode wird über die Exporthülle als gbXML und als IFC geschrieben und über die Hüllenfolge des Gebäudedialogs
    /// wieder eingelesen — Lesen, Zuordnen, Prüfen, Übernehmen, Editor speichern, Aufnehmen, Gebäudeliste speichern — ohne
    /// Planvorlage und ohne Planschritte. Die Zonenperioden kommen an, der Verweis zeigt auf die benannte Woche, die eingebettete
    /// Periode bleibt eingebettet. Gegenprobe: Eine Zone mit Herkunft MANUELL bleibt beim Wiederimport unverändert.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeImportZonenkalenderDialogwegTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private const int KLASSE_E = 4;
        private const double OBERE_RAUMTEMPERATUR = 26.0;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly string _ordner = Path.Combine(Path.GetTempPath(), "epos-n7-" + Guid.NewGuid().ToString("N"));

        public GebaeudeImportZonenkalenderDialogwegTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            _kultur.Dispose();
            _db.Dispose();
            try { Directory.Delete(_ordner, recursive: true); } catch (IOException) { }
        }

        private static IReadOnlyDictionary<string, bool> Keine => new Dictionary<string, bool>();

        private static readonly double[] RUHE = Woche(16.0, 15.0);
        private static readonly double[] MAI = Woche(18.0, 16.0);

        private static double[] Woche(double tag, double nacht)
            => Enumerable.Range(0, 168).Select(h => h % 24 < 6 ? double.NaN : h % 24 < 18 ? tag : nacht).ToArray();

        /// <summary>Die Dateiwahl der Exporthülle: antwortet mit einem festen Pfad.</summary>
        private sealed class Dateiwahl : IDateiDienst
        {
            internal string Antwort = "";
            public string DateiOeffnen(string titel, string filter, string startOrdner) => "";
            public string DateiSpeichern(string titel, string filter, string vorschlag) => Antwort;
            public string OrdnerWaehlen(string titel, string startOrdner) => "";
            public bool MitSystemOeffnen(string pfad) => true;
        }

        /// <summary>
        /// Das Ausgangsgebäude: das erste Gebäude des Projekts 1045 mit den Zonen des Probenhauses (Zonenplan), an jedem beheizten
        /// Raum der Kalender mit Neujahr (Verweis auf „Betriebsruhe“) und Tag der Arbeit (eingebettet, ranghöher), dazu die obere
        /// Raumtemperatur. Liefert Gebäude und Zuordnung.
        /// </summary>
        private static (int IdGebaeude, int IdZ) Ausgangsgebaeude()
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            ProjektGebaeudeModel g = ctrl.items[0];
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            var kalender = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, new[]
            {
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AusWoche(RUHE)),
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG + 1, "Tag der Arbeit", DbWerte.KOND_FEIERTAG_ERSTER_MAI, Kalenderangabe.AusWoche(MAI)),
            });
            var k = new AbbildKonditionierung();
            k.Kalender.Add(new AbbildKalender(kalender, null));
            var rest = new AbbildZonenrest { Zone = "EG" };
            rest.Wochen.Add(new BenannteWoche(0, Konditionierungsgroesse.Heizsoll, "Betriebsruhe", RUHE));
            rest.Periodenwochen.Add(new AbbildPeriodenwoche(Konditionierungsgroesse.Heizsoll, 0, 0, "Neujahr", "Betriebsruhe",
                                                            DbWerte.KOND_FEIERTAG_NEUJAHR));
            foreach (AbbildRaum r in ablauf.Abbild.Gebaeude[0].Raeume.Where(r => r.Beheizt))
            {
                r.Konditionierung = k;
                r.Rest = rest;
            }
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, zonierung: Zonenplan.Vorschlag(ablauf.Abbild, 0).Zonieren());
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Gebaeude SET Maximaleraumtemperatur = ? WHERE ID = ?",
                                           new DbParam("@t", DbParamTyp.Double) { Wert = OBERE_RAUMTEMPERATUR }, new DbParam("@g", g.ID_Gebaeude));
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?",
                                                                    new DbParam("@g", g.ID_Gebaeude)), CultureInfo.InvariantCulture);
            return (g.ID_Gebaeude, idZ);
        }

        /// <summary>Schreibt das Gebäude über die Exporthülle in eine Datei des Formats.</summary>
        private async Task<(string Pfad, GebaeudeExportAnsicht Vorschau)> Exportieren(int idZ, string format)
        {
            string pfad = Path.Combine(_ordner, "kalenderhaus" + (format == GebaeudeQuelle.FORMAT_IFC ? ".ifc" : ".xml"));
            Dienste.Datei = new Dateiwahl { Antwort = pfad };
            var h = new GebaeudeExportHuelle(PROJEKT, idZ, "Kalenderhaus", ios: false, profil: () => GbxmlExportProbe.Profil());
            GebaeudeExportAnsicht vorschau = await h.Vorbereiten("", format);
            Assert.False(vorschau.Abgelehnt, vorschau.Ablehnung);
            GebaeudeExportErgebnis e = await h.Speichern("", format);
            Assert.True(e.Gespeichert, e.Text);
            Assert.True(File.Exists(pfad));
            return (pfad, vorschau);
        }

        /// <summary>Die Zonenregel des Formats: IFC nach den Zonen der Datei (Z1), gbXML je Raum (X3; X1 bietet die Datei nicht an).</summary>
        private static string Dateizonen(string pfad)
            => pfad.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase) ? IfcImportProfil.ZONENREGEL_Z1 : GebaeudeImportProfil.ZONENREGEL_X3;

        /// <summary>Die Hüllenfolge des Gebäudedialogs ohne Planvorlage und Planschritte; liefert die neue Projektkopie.</summary>
        private static async Task<int> Importieren(string pfad, string name)
            => (await Importieren(pfad, name, Dateizonen(pfad), einzonig: false)).Kopie;

        /// <summary>
        /// Die Hüllenfolge mit einer Zonenregel; <paramref name="einzonig"/> = die Regel ergibt den Einzonenweg. Liefert die neue
        /// Projektkopie und den Stand der Zuordnung (allgemeine Meldungsliste, Zonierung, Bauteile).
        /// </summary>
        private static async Task<(int Kopie, GebaeudeImportStand Stand)> Importieren(string pfad, string name, string regel, bool einzonig)
        {
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            GebaeudeImportweg weg = ((Func<GebaeudeImportweg>)gaben["ImportGaben"])();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)weg.Gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(pfad, null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)weg.Gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, KLASSE_E, Keine, Zonenregel: regel));
            Assert.True(stand.Zonierung!.Einzonig == einzonig, "Regel " + regel + " ergibt " + stand.Zonierung.Regel + " von " +
                                                    string.Join(", ", stand.Zonierung.Regeln.Select(r => r.Schluessel)));
            Assert.True(stand.Bauteile!.Moeglich, stand.Bauteile.Ablehnung);

            var ergebnis = new GebaeudeImportErgebnis(0, KLASSE_E, name, Keine, stand.Zeilen.ToList(), AlsZone: true,
                                                      Zonenregel: regel);
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)weg.Gaben["Pruefen"];
            Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == EPOS.UI.Bausteine.WarnStufe.Fehler);
            Assert.Null(await ((Func<GebaeudeImportErgebnis, Task<string>>)weg.Gaben["Uebernehmen"])(ergebnis));

            IReadOnlyDictionary<string, object> editor = weg.EditorGaben();
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden((GebaeudeKatalogDaten)editor["Daten"], neu: true);
            GebaeudePruefbefund befund = arbeit.Pruefen(true, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());
            Assert.True(befund == null, befund?.Meldung);
            arbeit.Ableiten();
            var speichern = (Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>)editor["Speichern"];
            Assert.True(speichern(arbeit.Stand, true, arbeit.Stand.Name).Erfolg);
            GebaeudeProjektZeile zeile = weg.Aufnehmen();
            Assert.NotNull(zeile);
            ((List<GebaeudeProjektZeile>)gaben["Zeilen"]).Add(zeile);
            ((Action)gaben["Geaendert"])();
            Z_ProjGebModel neu = modelle.Single(m => m.Gebaeudename == name);
            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            return (Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ? AND ID_Projekt = ?",
                new DbParam("@z", neu.ID_Z), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture), stand);
        }

        /// <summary>Die Kalender- und Periodenzeilen einer Zone samt Kennungen — ändert sich bei jedem Neuschreiben.</summary>
        private static string Kalenderbild(int idZone)
            => string.Join(";", DataRepository.GetDataTable(
                "SELECT k.ID, k.Groesse, p.ID, p.Rang, COALESCE(p.ID_Woche, 0) FROM Tab_Konditionierungskalender k " +
                "LEFT JOIN Tab_Konditionierungsperiode p ON p.ID_Kalender = k.ID WHERE k.ID_Zone = ? ORDER BY k.ID, p.ID",
                new DbParam("@z", idZone)).Rows.Cast<DataRow>()
                .Select(r => string.Join(",", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))));

        /// <summary>Prüft an jeder beheizten Zone: Verweis auf „Betriebsruhe“ am Neujahr, Tag der Arbeit eingebettet.</summary>
        private static void KalenderAngekommen(int idGebaeude, string format)
        {
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude).Where(z => z.IstBeheizt).ToList();
            Assert.NotEmpty(zonen);
            foreach (ZoneModel z in zonen)
            {
                Konditionierungsstand stand = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Zone(idGebaeude, z.ID), out string meldung);
                Assert.True(stand != null, meldung);
                Konditionierungskalender heiz = stand.Kalender(Konditionierungsgroesse.Heizsoll);
                Assert.True(heiz != null, format + ": Zone „" + z.Bezeichner + "“ (" + z.ID + ", " + z.Herkunft + ") ohne Heizkalender; Zonen " +
                                          string.Join(", ", zonen.Select(x => x.ID + "/" + x.Bezeichner + "/" + x.Herkunft)));
                IReadOnlyList<Kalenderregel> perioden = heiz.Perioden;
                Assert.True(perioden.Count > 0, format + ": Zone „" + z.Bezeichner + "“ ohne Zonenperiode.");
                BenannteWoche w = Assert.Single(stand.Wochen, x => x.Name == "Betriebsruhe");
                Kalenderregel neujahr = Assert.Single(perioden, p => p.Feiertagsregel == DbWerte.KOND_FEIERTAG_NEUJAHR);
                Kalenderregel mai = Assert.Single(perioden, p => p.Feiertagsregel == DbWerte.KOND_FEIERTAG_ERSTER_MAI);
                Assert.Equal(w.Id, neujahr.Angabe.IdWoche);
                Assert.Equal(RUHE, neujahr.Angabe.Woche.ToArray());
                Assert.Null(mai.Angabe.IdWoche);
                Assert.Equal(MAI, mai.Angabe.Woche.ToArray());
            }
        }

        private static double ObereRaumtemperatur(int idGebaeude)
            => Convert.ToDouble(DataRepository.ExecuteScalar("SELECT Maximaleraumtemperatur FROM Tab_Gebaeude WHERE ID = ?",
                                                             new DbParam("@g", idGebaeude)), CultureInfo.InvariantCulture);

        [Theory]
        [InlineData(GebaeudeQuelle.FORMAT_GBXML)]
        [InlineData(GebaeudeQuelle.FORMAT_IFC)]
        public async Task Zonenkalender_kommen_auf_dem_Dialogweg_ohne_Plan_an(string format)
        {
            if (!_db.Vorhanden) return;
            (int _, int idZ) = Ausgangsgebaeude();
            (string pfad, GebaeudeExportAnsicht export) = await Exportieren(idZ, format);
            // gbXML: der eine Tagtyp „Holiday“ trägt nur das ranghöchste Feiertagsbild — benannt, nicht still (Formatgrenze).
            bool benannt = export.Meldungen.Any(m => m.Schluessel?.EndsWith(GbxmlSchreiber.FEIERTAGE_EIN_BILD, StringComparison.Ordinal) == true);
            Assert.Equal(format == GebaeudeQuelle.FORMAT_GBXML, benannt);

            int kopie = await Importieren(pfad, "Kalenderhaus N7 " + format);
            Assert.Equal(OBERE_RAUMTEMPERATUR, ObereRaumtemperatur(kopie));
            KalenderAngekommen(kopie, format);
        }

        /// <summary>
        /// Der Einzonenweg (N8, E160): Tragen alle beheizten Räume denselben Kalender, nimmt die eine Zone ihn ohne Warnung; trägt ein
        /// Raum einen anderen, nimmt sie keinen — benannt (Warnung an der einen Zone). Gegenprobe: ohne Kalender keine Meldung.
        /// </summary>
        [Fact]
        public void Der_Einzonenweg_benennt_die_nicht_uebernommenen_Zeitplaene()
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportAblauf ohne = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            Assert.DoesNotContain(GebaeudeBauteilvorschlag.Bilden(ohne, 0, null).Meldungen,
                                  m => m.Schluessel == GebaeudeBauteilvorschlag.EINZONE_ZEITPLAENE);
            static AbbildKonditionierung Satz(double wert)
            {
                var k = new AbbildKonditionierung();
                k.Kalender.Add(new AbbildKalender(new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(wert), null,
                                                                               Array.Empty<Kalenderregel>()), null));
                return k;
            }
            GebaeudeImportAblauf gleich = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            foreach (AbbildRaum r in gleich.Abbild.Gebaeude[0].Raeume.Where(r => r.Beheizt)) r.Konditionierung = Satz(20.0);
            GebaeudeBauteilvorschlag vg = GebaeudeBauteilvorschlag.Bilden(gleich, 0, null);
            Assert.DoesNotContain(vg.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.EINZONE_ZEITPLAENE);
            Assert.NotNull(Assert.Single(vg.Zonenkonditionierungen));

            GebaeudeImportAblauf mit = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            List<AbbildRaum> warm = mit.Abbild.Gebaeude[0].Raeume.Where(r => r.Beheizt).ToList();
            Assert.True(warm.Count >= 2, "Die Probe braucht zwei beheizte Räume.");
            foreach (AbbildRaum r in warm) r.Konditionierung = Satz(20.0);
            warm[^1].Konditionierung = Satz(21.0);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(mit, 0, null);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.EINZONE_ZEITPLAENE);
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Null(Assert.Single(v.Zonenkonditionierungen));
        }

        [Fact]
        public async Task Eine_Zone_mit_Herkunft_MANUELL_bleibt_beim_Wiederimport_unveraendert()
        {
            if (!_db.Vorhanden) return;
            (int _, int idZ) = Ausgangsgebaeude();
            (string pfad, _) = await Exportieren(idZ, GebaeudeQuelle.FORMAT_IFC);
            int kopie = await Importieren(pfad, "Kalenderhaus N7 Wiederimport");
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(kopie).Where(z => z.IstBeheizt).ToList();
            Assert.True(zonen.Count >= 2, "Die Probe braucht zwei beheizte Zonen.");
            ZoneModel hand = zonen[0], datei = zonen[1];
            DataRepository.ExecuteNonQuery("UPDATE Tab_Zone SET Herkunft = ? WHERE ID = ?",
                                           new DbParam("@h", DbWerte.HERKUNFT_MANUELL), new DbParam("@z", hand.ID));
            string handVorher = Kalenderbild(hand.ID), dateiVorher = Kalenderbild(datei.ID);

            var mz = new GebaeudeMehrzonenHuelle(() => kopie, PROJEKT, ios: false);
            GebaeudeMehrzonenstart s = await mz.StartAsync(pfad, MehrzonenuebernahmeCtrl.Quelle(kopie));
            Assert.Null(s.Meldung);
            IReadOnlyDictionary<string, object> gaben = s.Gaben!;
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            Assert.True((await lesen(pfad, null!, CancellationToken.None)).Gelesen);
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, KLASSE_E, Keine, Zonenregel: IfcImportProfil.ZONENREGEL_Z1));
            var ergebnis = new GebaeudeImportErgebnis(0, KLASSE_E, "Probe", Keine, stand.Zeilen.ToList(), AlsZone: true,
                                                      Zonenregel: IfcImportProfil.ZONENREGEL_Z1);
            GebaeudeWiederimportDaten daten = ((Func<GebaeudeImportErgebnis, GebaeudeWiederimportDaten>)gaben["Wiederimport"])(ergebnis);
            Assert.NotNull(daten);
            Assert.Equal("", daten.Meldung);
            Assert.Null(await ((Func<GebaeudeImportErgebnis, Task<string>>)gaben["Uebernehmen"])(ergebnis));
            Assert.Null(mz.Schreiben());

            Dictionary<int, ZoneModel> nachher = new GebaeudeZonenCtrl().LesenJeGebaeude(kopie).ToDictionary(z => z.ID);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, nachher[hand.ID].Herkunft);
            Assert.Equal(handVorher, Kalenderbild(hand.ID));
            // Gegenprobe: Die Zone ohne Marke nimmt die Kalender der Datei neu — mit den Perioden der Datei.
            Assert.NotEqual(dateiVorher, Kalenderbild(datei.ID));
            KalenderAngekommen(kopie, GebaeudeQuelle.FORMAT_IFC);
        }

        // ==================================================================
        //  N8 — Einzonenweg (E160), Regel X1 der eigenen gbXML-Datei (E161), Meldung in der allgemeinen Liste
        // ==================================================================

        /// <summary>Die Einzonenregel des Formats: gbXML X4, IFC Z5.</summary>
        private static string Einzone(string format)
            => format == GebaeudeQuelle.FORMAT_IFC ? IfcImportProfil.ZONENREGEL_Z5 : GebaeudeImportProfil.ZONENREGEL_X4;

        /// <summary>Nimmt einer beheizten Zone des Ausgangsgebäudes die Perioden des Heizkalenders — ihr Kalender weicht dann ab.</summary>
        private static void EineZoneAbweichend(int idGebaeude)
        {
            ZoneModel z = new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude).First(x => x.IstBeheizt);
            DataRepository.ExecuteNonQuery(
                "DELETE FROM Tab_Konditionierungsperiode WHERE ID_Kalender IN (SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Zone = ?)",
                new DbParam("@z", z.ID));
        }

        private static bool Gewarnt(IEnumerable<GebaeudeImportMeldung> meldungen)
            => meldungen.Any(m => m.Kennung == GebaeudeBauteilvorschlag.EINZONE_ZEITPLAENE);

        /// <summary>
        /// E160: Tragen alle Räume der Datei denselben Kalender, nimmt die eine Zone ihn — Perioden, Feiertagsverweis auf die benannte
        /// Woche, eingebettete Feiertagsperiode; keine Warnung.
        /// </summary>
        [Theory]
        [InlineData(GebaeudeQuelle.FORMAT_GBXML)]
        [InlineData(GebaeudeQuelle.FORMAT_IFC)]
        public async Task Der_Einzonenweg_nimmt_den_gemeinsamen_Kalender_der_Raeume(string format)
        {
            if (!_db.Vorhanden) return;
            (int _, int idZ) = Ausgangsgebaeude();
            (string pfad, _) = await Exportieren(idZ, format);
            (int kopie, GebaeudeImportStand stand) = await Importieren(pfad, "Kalenderhaus N8 eine Zone " + format, Einzone(format), einzonig: true);
            Assert.False(Gewarnt(stand.Meldungen));
            Assert.False(Gewarnt(stand.Bauteile!.Meldungen));
            Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(kopie));
            KalenderAngekommen(kopie, format);
        }

        /// <summary>
        /// E160, Gegenprobe: Weicht der Kalender eines Raums ab, nimmt die eine Zone keinen Kalender der Datei — die Warnung steht im
        /// Vorschlag und in der allgemeinen Meldungsliste des Imports.
        /// </summary>
        [Theory]
        [InlineData(GebaeudeQuelle.FORMAT_GBXML)]
        [InlineData(GebaeudeQuelle.FORMAT_IFC)]
        public async Task Der_Einzonenweg_warnt_bei_abweichenden_Kalendern_und_nimmt_keinen(string format)
        {
            if (!_db.Vorhanden) return;
            (int idGebaeude, int idZ) = Ausgangsgebaeude();
            EineZoneAbweichend(idGebaeude);
            (string pfad, _) = await Exportieren(idZ, format);
            (int kopie, GebaeudeImportStand stand) = await Importieren(pfad, "Kalenderhaus N8 abweichend " + format, Einzone(format), einzonig: true);
            Assert.True(Gewarnt(stand.Bauteile!.Meldungen));
            GebaeudeImportMeldung m = Assert.Single(stand.Meldungen, x => x.Kennung == GebaeudeBauteilvorschlag.EINZONE_ZEITPLAENE);
            Assert.Equal(EPOS.UI.Bausteine.WarnStufe.Warnung, m.Stufe);
            ZoneModel zone = Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(kopie));
            Konditionierungsstand s = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Zone(kopie, zone.ID), out string meldung);
            Assert.True(s != null, meldung);
            Assert.DoesNotContain(Konditionierungsgroessen.Alle.Select(s.Kalender).Where(k => k != null).SelectMany(k => k.Perioden),
                                  p => p.Feiertagsregel == DbWerte.KOND_FEIERTAG_NEUJAHR);
            Assert.DoesNotContain(s.Wochen, w => w.Name == "Betriebsruhe");
        }

        /// <summary>
        /// E161: Die von EPOS-Plan geschriebene gbXML-Datei (je Zone ein Raum mit <c>zoneIdRef</c>) bietet die Regel X1 an; X1 bildet
        /// die Zonen der Datei mit ihren Kalendern.
        /// </summary>
        [Fact]
        public async Task Die_eigene_gbXML_Datei_bietet_X1_an_und_X1_bildet_die_Zonen_der_Datei()
        {
            if (!_db.Vorhanden) return;
            (int idGebaeude, int idZ) = Ausgangsgebaeude();
            int quellzonen = new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude).Count;
            (string pfad, _) = await Exportieren(idZ, GebaeudeQuelle.FORMAT_GBXML);
            (int kopie, GebaeudeImportStand stand) = await Importieren(pfad, "Kalenderhaus N8 X1", GebaeudeImportProfil.ZONENREGEL_X1, einzonig: false);
            Assert.Equal(GebaeudeImportProfil.ZONENREGEL_X1, stand.Zonierung!.Regel);
            Assert.Contains(stand.Zonierung.Regeln, r => r.Schluessel == GebaeudeImportProfil.ZONENREGEL_X1);
            Assert.Equal(quellzonen, new GebaeudeZonenCtrl().LesenJeGebaeude(kopie).Count);
            KalenderAngekommen(kopie, GebaeudeQuelle.FORMAT_GBXML);
        }
    }
}
