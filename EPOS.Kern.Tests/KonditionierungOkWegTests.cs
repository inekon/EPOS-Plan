using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Eine Schreibstelle — der OK-Weg der Konditionierung</b> (Stufe KP2, Welle K2, Teilschritt 4;
    /// Entwurf KP2 Abschnitt 2, Befund B10): Katalog neu samt Konditionierung in EINEM Vorgang, im
    /// Projekt Schritt 1 (Gebäude) und Schritt 3 (Zonen samt Konditionierung und Id-Zuordnung). Ein
    /// Fehler mitten im Vorgang rollt alles zurück; ein zweites OK schreibt nichts doppelt; eine neue
    /// Zone mit eigener Zelle steht nach dem OK richtig da.
    ///
    /// <para>Jeder Fall arbeitet auf einer Arbeitskopie der Testdatenbank und schweigt ohne sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KonditionierungOkWegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt Kultur und Arbeitskopie zurück (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar();

        private const int PROJEKT = 1007;

        private static int IdZ => Z_ProjGebCtrl.LiesProjekt(PROJEKT)[0].ID_Z;

        private static int Gebaeude => GebaeudeBedarfCtrl.TabGebaeudeId(IdZ);

        private const Konditionierungsgroesse HEIZ = Konditionierungsgroesse.Heizsoll;

        /// <summary>Die Ebene einer Zone mit eigener Heiz-Nachtzeile (Festlegung 5) und einem Geräteanteil.</summary>
        private static Konditionierungsstand Zonenebene()
            => Konditionierungsstand.Leer(Kalendereigentuemer.Zone, null)
                .MitVorgabe(HEIZ, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.NurZeiten(20, 7))
                .MitVorgabe(Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5));

        /// <summary>Ein Heizkalender, der nicht rund läuft (Wert → Text → Wert nicht bitgleich) — der Schreibweg lehnt ihn ab.</summary>
        private static Konditionierungskalender Unrund()
            => new Konditionierungskalender(HEIZ, Kalenderangabe.AusWert(20.000001), null, Array.Empty<Kalenderregel>());

        private static Konditionierungskalender Heizkalender()
        {
            var woche = new double[168];
            for (int i = 0; i < woche.Length; i++) woche[i] = i % 24 < 6 ? 16.0 : 20.0;
            return new Konditionierungskalender(HEIZ, Kalenderangabe.AusWoche(woche), null,
                new[]
                {
                    Kalenderregel.Zeitraum(400, DbWerte.KOND_ART_ZEITRAUM, "Umbau", 100, 110, Kalenderangabe.AusWert(14.0)),
                });
        }

        // =============================================================================
        //  Schritt 3: Zonen samt Konditionierung
        // =============================================================================

        [Fact]
        public void Eine_neue_Zone_mit_eigener_Zelle_steht_nach_dem_OK_richtig()
        {
            if (!Bereit()) return;
            var zonen = new GebaeudeZonenCtrl();
            Konditionierungsstand ebene = Zonenebene();
            GebaeudeZonenCtrl.Schreibergebnis e = zonen.Schreiben(
                Gebaeude, new List<ZoneModel> { GebaeudeZonenlistePruefregelTests.Zone(-1, "Anbau", 60) }, null,
                new Dictionary<int, Konditionierungsstand> { [-1] = ebene });
            Assert.True(e.Ok, e.Meldung);
            int id = e.Zonen[-1];
            Assert.True(id > 0);
            Assert.Equal(1, e.KonditionierungGeschrieben);
            Assert.True(e.Bauteile[id].All(b => b > 0));

            var kond = new KonditionierungCtrl();
            Konditionierungsstand gelesen = kond.StandLesen(KonditionierungCtrl.Eigner.Zone(Gebaeude, id), out string m);
            Assert.Null(m);
            Assert.True(ebene.Gleich(gelesen, mitBestand: false));

            // Die eigene Nachtzeile macht die Heizspalte der Zone wirksam (Festlegung 5) — aus der Datenbank gelesen.
            Konditionierungsarbeitsstand a = kond.ArbeitsstandLesen(Gebaeude, Gemeinjahrkalender.Kalenderjahr(2025), out _);
            Assert.NotNull(a.GeltenderKalender(HEIZ, id));
            Assert.Null(a.GeltenderKalender(HEIZ, null));
        }

        [Fact]
        public void Ein_zweites_OK_schreibt_nichts_doppelt()
        {
            if (!Bereit()) return;
            var zonen = new GebaeudeZonenCtrl();
            var liste = new List<ZoneModel> { GebaeudeZonenlistePruefregelTests.Zone(-1, "Anbau", 60) };
            GebaeudeZonenCtrl.Schreibergebnis e1 = zonen.Schreiben(Gebaeude, liste, null,
                new Dictionary<int, Konditionierungsstand> { [-1] = Zonenebene() });
            Assert.True(e1.Ok, e1.Meldung);
            int id = e1.Zonen[-1];
            long zonenzahl = Zahl("SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", Gebaeude);
            long vorgaben = Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE + "\"", null);
            long hoechste = Zahl("SELECT MAX(\"ID\") FROM \"" + KonditionierungSchema.TAB_VORGABE + "\"", null);

            // Dasselbe OK noch einmal — die Zone trägt jetzt ihre endgültige Id.
            GebaeudeZonenCtrl.Schreibergebnis e2 = zonen.Schreiben(Gebaeude, liste, null,
                new Dictionary<int, Konditionierungsstand> { [id] = Zonenebene() });
            Assert.True(e2.Ok, e2.Meldung);
            Assert.Empty(e2.Zonen);
            Assert.Equal(0, e2.KonditionierungGeschrieben);
            Assert.Equal(zonenzahl, Zahl("SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", Gebaeude));
            Assert.Equal(vorgaben, Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE + "\"", null));
            Assert.Equal(hoechste, Zahl("SELECT MAX(\"ID\") FROM \"" + KonditionierungSchema.TAB_VORGABE + "\"", null));
        }

        /// <summary>
        /// <b>B10 am Weg des Dialogs</b>: Der Zonenweg liefert die Zuordnung, der Arbeitsstand übernimmt
        /// sie — ein zweites OK (etwa „Speichern" des Assistenten ohne Schließen) mit einer weiteren
        /// Änderung legt die neue Zone samt Bauteil nicht noch einmal an.
        /// </summary>
        [Fact]
        public void B10_Der_Zonenweg_liefert_die_Zuordnung_und_das_zweite_OK_legt_nichts_noch_einmal_an()
        {
            if (!_db.Vorhanden) return;
            GebaeudeZonenweg weg = GebaeudeKatalogHuelle.Zonenweg(PROJEKT, IdZ, Gebaeude);
            var a = new GebaeudeArbeitsstand();
            a.Laden(new GebaeudeKatalogDaten { WohnflaecheGesamt = 110, Raumhoehe = 2.5 }, false);
            a.ZonenLaden(weg.Zonen, true);
            int vorher = a.Zonen.Count;

            ZoneDaten neu = a.NeueZone("Anbauprobe");
            neu.Nutzflaeche = 60;
            neu.Bauteile.Add(new BauteilDaten { Id = -1, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                                Flaeche = 20, Azimut = 180, UWert = 0.3 });
            a.ZoneAnlegen(neu);
            ZonenSchreibergebnis e = weg.Speichern!(a.Zonenstand(true));
            Assert.True(e.Ok, e.Meldung);
            a.IdsUebernehmen(e);
            a.ZonenGeschrieben();
            ZoneDaten geschrieben = a.Zonen.Single(z => z.Bezeichner == "Anbauprobe");
            Assert.True(geschrieben.Id > 0);
            Assert.True(geschrieben.Bauteile[0].Id > 0);

            // Das zweite OK mit einer weiteren Änderung.
            ZoneDaten umbenannt = geschrieben.Kopie();
            umbenannt.Bezeichner = "Anbauprobe neu";
            Assert.True(a.ZoneErsetzen(umbenannt));
            Assert.True(weg.Speichern!(a.Zonenstand(true)).Ok);

            Assert.Equal(vorher + 1, (int)Zahl("SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", Gebaeude));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil\" WHERE \"ID_Zone\" = ?", geschrieben.Id));
            Assert.Equal("Anbauprobe neu", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"Bezeichner\" FROM \"Tab_Zone\" WHERE \"ID\" = ?", new DbParam("@z", geschrieben.Id)),
                CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Ein_Fehler_mitten_im_Vorgang_rollt_Zonen_und_Konditionierung_zurueck()
        {
            if (!Bereit()) return;
            long zonenzahl = Zahl("SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", Gebaeude);
            long kalender = Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER + "\"", null);
            long vorgaben = Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE + "\"", null);

            // Die Vorgabezellen würden geschrieben, der Kalender läuft nicht rund — mitten im Vorgang.
            Konditionierungsstand ebene = Zonenebene().MitKalender(HEIZ, Unrund(), Kalenderherkunft.Keine);
            GebaeudeZonenCtrl.Schreibergebnis e = new GebaeudeZonenCtrl().Schreiben(
                Gebaeude, new List<ZoneModel> { GebaeudeZonenlistePruefregelTests.Zone(-1, "Anbau", 60) }, null,
                new Dictionary<int, Konditionierungsstand> { [-1] = ebene });
            Assert.False(e.Ok);
            Assert.Empty(e.Zonen);

            Assert.Equal(zonenzahl, Zahl("SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", Gebaeude));
            Assert.Equal(kalender, Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER + "\"", null));
            Assert.Equal(vorgaben, Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE + "\"", null));
        }

        // =============================================================================
        //  Katalog neu und Projekt Schritt 1
        // =============================================================================

        [Fact]
        public void Katalog_neu_schreibt_Kopf_und_Konditionierung_in_einem_Vorgang()
        {
            if (!Bereit()) return;
            GebaeudeModel kopf = Kopfsatz("K2 OK-Weg neu");
            Konditionierungsstand stand = Konditionierungsstand.Leer(Kalendereigentuemer.Katalogbau, null)
                .MitVorgabe(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(0.8, 22, 6))
                .MitKalender(HEIZ, Heizkalender(), new Kalenderherkunft("Büro", null));

            GebaeudeStammCtrl.Katalogschreibergebnis e = GebaeudeStammCtrl.KatalogSchreiben(kopf, true, null, stand);
            Assert.True(e.Ok, e.Meldung);
            Assert.True(e.Id > 0);
            Assert.True(e.KonditionierungGeschrieben);
            Konditionierungsstand gelesen = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Katalogbau(e.Id), out string m);
            Assert.Null(m);
            Assert.True(stand.Gleich(gelesen, mitBestand: false));
            Assert.Equal("Büro", gelesen.Herkunft(HEIZ).Vorlage);

            // Ein zweites OK auf denselben Satz schreibt an der Konditionierung nichts.
            GebaeudeStammCtrl.Katalogschreibergebnis e2 = GebaeudeStammCtrl.KatalogSchreiben(kopf, false, "K2 OK-Weg neu", stand);
            Assert.True(e2.Ok, e2.Meldung);
            Assert.Equal(e.Id, e2.Id);
            Assert.False(e2.KonditionierungGeschrieben);
        }

        [Fact]
        public void Katalog_neu_mit_einem_Fehler_in_der_Konditionierung_legt_keinen_Kopf_an()
        {
            if (!Bereit()) return;
            long katalog = Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\"", null);
            Konditionierungsstand stand = Konditionierungsstand.Leer(Kalendereigentuemer.Katalogbau, null)
                .MitKalender(HEIZ, Unrund(), Kalenderherkunft.Keine);
            GebaeudeStammCtrl.Katalogschreibergebnis e = GebaeudeStammCtrl.KatalogSchreiben(Kopfsatz("K2 OK-Weg Fehler"), true, null, stand);
            Assert.False(e.Ok);
            Assert.Equal(katalog, Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\"", null));
            Assert.Null(new GebaeudeStammCtrl().Lies("K2 OK-Weg Fehler"));
        }

        [Fact]
        public void Projekt_Schritt_1_schreibt_das_Gebaeude_samt_Konditionierung_und_ein_zweites_Mal_nichts()
        {
            if (!Bereit()) return;
            GebaeudeModel modell = GebaeudeStammCtrl.LiesProjektkopie(Gebaeude);
            Assert.NotNull(modell);
            Konditionierungsstand stand = Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, null)
                .MitVorgabe(Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5))
                .MitKalender(HEIZ, Heizkalender(), Kalenderherkunft.Keine);
            Assert.True(GebaeudeStammCtrl.ProjektkopieSchreiben(Gebaeude, PROJEKT, modell, stand).Ok);
            var kond = new KonditionierungCtrl();
            Assert.True(stand.Gleich(kond.StandLesen(KonditionierungCtrl.Eigner.Gebaeude(Gebaeude), out _), mitBestand: false));

            long hoechste = Zahl("SELECT MAX(\"ID\") FROM \"" + KonditionierungSchema.TAB_KALENDER + "\"", null);
            Assert.True(GebaeudeStammCtrl.ProjektkopieSchreiben(Gebaeude, PROJEKT, modell, stand).Ok);
            Assert.Equal(hoechste, Zahl("SELECT MAX(\"ID\") FROM \"" + KonditionierungSchema.TAB_KALENDER + "\"", null));
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        /// <summary>Der erste freie Katalogsatz als Kopf unter neuem Namen.</summary>
        private static GebaeudeModel Kopfsatz(string name)
        {
            long stamm = Zahl("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0", null);
            string bezeichner = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"Bezeichner\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = ?", new DbParam("@s", stamm)),
                CultureInfo.InvariantCulture);
            GebaeudeModel m = new GebaeudeStammCtrl().Lies(bezeichner);
            Assert.NotNull(m);
            m.Gebaeudename = name;
            return m;
        }

        private static long Zahl(string sql, long? parameter)
        {
            object o = parameter.HasValue
                ? DataRepository.ExecuteScalar(sql, new DbParam("@p", parameter.Value))
                : DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
