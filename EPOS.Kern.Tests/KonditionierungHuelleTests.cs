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
    /// <b>Die Hülle bindet den Vertrag</b> (Stufe KP2, Welle K2, Teilschritt 5): Die Hülle füllt
    /// <see cref="KonditionierungDaten"/> beim Lesen, trägt den <see cref="KonditionierungWeg"/> aus den
    /// reinen Schritten und schreibt im OK-Weg. Die Fälle gehen den Weg des Dialogs — Gaben lesen, eine
    /// Handlung über den Delegaten, OK über den Schreibdelegaten, erneut lesen: alles da; ohne OK
    /// (Abbrechen) bleibt alles, wie es war; ohne Änderung schreibt der OK-Weg an der Konditionierung nichts.
    ///
    /// <para>Jeder Fall arbeitet auf einer Arbeitskopie der Testdatenbank und schweigt ohne sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KonditionierungHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt Kultur und Arbeitskopie zurück.</summary>
        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar();

        private const int PROJEKT = 1007;

        private static int IdZ => Z_ProjGebCtrl.LiesProjekt(PROJEKT)[0].ID_Z;

        private static int Gebaeude => GebaeudeBedarfCtrl.TabGebaeudeId(IdZ);

        private static string FreierKatalogsatz()
            => Convert.ToString(DataRepository.ExecuteScalar(
                   "SELECT \"Bezeichner\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0 ORDER BY \"ID\" LIMIT 1"),
                   CultureInfo.InvariantCulture);

        private static (GebaeudeKatalogDaten Daten, KonditionierungWeg Weg) Katalog(string name)
        {
            IReadOnlyDictionary<string, object> gaben = GebaeudeKatalogHuelle.Gaben(name, GebaeudeKatalogModus.Bearbeiten);
            return ((GebaeudeKatalogDaten)gaben["Daten"], (KonditionierungWeg)gaben["Konditionierung"]);
        }

        private static KonditionierungStand Stand(GebaeudeKatalogDaten d, IReadOnlyList<ZoneDaten> zonen = null)
            => new KonditionierungStand(d, zonen ?? Array.Empty<ZoneDaten>());

        private static KonditionierungStand Gut(KonditionierungErgebnis e)
        {
            Assert.True(e.Ok, e.Meldung);
            Assert.NotNull(e.Stand);
            return e.Stand;
        }

        private static long Hoechste(string tabelle)
        {
            object o = DataRepository.ExecuteScalar("SELECT COALESCE(MAX(\"ID\"), 0) FROM \"" + tabelle + "\"");
            return Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        // =============================================================================
        //  Katalog: lesen, Zelle über den Delegaten, OK, erneut lesen
        // =============================================================================

        [Fact]
        public void Katalog_lesen_Zelle_setzen_OK_erneut_lesen_alles_da()
        {
            if (!Bereit()) return;
            string name = FreierKatalogsatz();
            (GebaeudeKatalogDaten daten, KonditionierungWeg weg) = Katalog(name);
            Assert.NotNull(daten.Konditionierung);
            Assert.Equal(0, daten.Konditionierung.Fassung);
            Assert.Null(weg.Sperre);
            Assert.True(weg.Bietet(KonditionierungHandlung.ZelleSetzen));
            Assert.False(weg.Bietet(KonditionierungHandlung.KatalogErneut));    // nur im Projekt (Festlegung 4)

            // Heizsaison 1.10. bis 30.4. und ein Personenanteil von 50 % (P1: Nennwert und Geräte folgen).
            KonditionierungStand s = Gut(weg.ZelleSetzen!(Stand(daten), new KonditionierungOrt(KonditionierungGroesse.Heizen),
                                                          KonditionierungZeile.Saison, new KonditionierungZelle { Von = 274, Bis = 120 }));
            double? gewinneVorher = s.Gebaeude.Waermegewinne;
            s = Gut(weg.ZelleSetzen!(s, new KonditionierungOrt(KonditionierungGroesse.Personen),
                                     KonditionierungZeile.Tag, new KonditionierungZelle { Wert = 50.0 }));
            Assert.True(s.Gebaeude.Konditionierung.Fassung >= 2);
            Assert.Equal(50.0, s.Gebaeude.Konditionierung.Spalte(KonditionierungGroesse.Personen).Tag.Wert);
            Assert.True(s.Gebaeude.Konditionierung.Spalte(KonditionierungGroesse.Personen).Nennwert.Wert > 0.0);
            Assert.True(s.Gebaeude.Waermegewinne < gewinneVorher);

            // OK — der eine Schreibweg des Editors.
            GebaeudeKatalogErgebnis ok = GebaeudeKatalogHuelle.Schreiben(s.Gebaeude, false, name);
            Assert.True(ok.Erfolg, ok.Meldung);

            (GebaeudeKatalogDaten neu, _) = Katalog(name);
            KonditionierungSpalte heiz = neu.Konditionierung.Spalte(KonditionierungGroesse.Heizen);
            Assert.Equal(274, heiz.Saison.Von);
            Assert.Equal(120, heiz.Saison.Bis);
            KonditionierungSpalte personen = neu.Konditionierung.Spalte(KonditionierungGroesse.Personen);
            Assert.Equal(50.0, personen.Tag.Wert);
            Assert.Equal(s.Gebaeude.Konditionierung.Spalte(KonditionierungGroesse.Personen).Nennwert.Wert, personen.Nennwert.Wert);
            Assert.Equal(s.Gebaeude.Waermegewinne.Value, neu.Waermegewinne.Value, 6);
        }

        [Fact]
        public void Abbrechen_und_ein_OK_ohne_Aenderung_lassen_die_Konditionierung_stehen()
        {
            if (!Bereit()) return;
            string name = FreierKatalogsatz();
            (GebaeudeKatalogDaten daten, KonditionierungWeg weg) = Katalog(name);
            long vorgaben = Hoechste(KonditionierungSchema.TAB_VORGABE);
            long kalender = Hoechste(KonditionierungSchema.TAB_KALENDER);

            // Eine Handlung ohne OK — Abbrechen schreibt nichts.
            Gut(weg.ZelleSetzen!(Stand(daten), new KonditionierungOrt(KonditionierungGroesse.Heizen),
                                 KonditionierungZeile.Saison, new KonditionierungZelle { Von = 274, Bis = 120 }));
            (GebaeudeKatalogDaten wieder, _) = Katalog(name);
            Assert.True(wieder.Konditionierung.Spalte(KonditionierungGroesse.Heizen).Saison.Leer);

            // OK ohne Änderung an der Konditionierung — sie bleibt, wie sie ist (Fassung 0).
            Assert.True(GebaeudeKatalogHuelle.Schreiben(wieder, false, name).Erfolg);
            Assert.Equal(vorgaben, Hoechste(KonditionierungSchema.TAB_VORGABE));
            Assert.Equal(kalender, Hoechste(KonditionierungSchema.TAB_KALENDER));
        }

        [Fact]
        public void Die_Gesamtangabe_fragt_nach_dem_Aufteilen_und_die_Summe_bleibt_bis_in_die_Datenbank()
        {
            if (!Bereit()) return;
            string name = FreierKatalogsatz();
            (GebaeudeKatalogDaten daten, KonditionierungWeg weg) = Katalog(name);
            daten.LuftwechselInfiltration = null;
            daten.LuftwechselNutzer = null;
            daten.Luftwechselrate = 0.7;

            var ort = new KonditionierungOrt(KonditionierungGroesse.Lueftung);
            var nacht = new KonditionierungZelle { Wert = 1.5, Von = 22, Bis = 6 };
            KonditionierungErgebnis frage = weg.ZelleSetzen!(Stand(daten), ort, KonditionierungZeile.Nacht, nacht);
            Assert.False(frage.Ok);
            Assert.NotNull(frage.Rueckfrage);
            Assert.Contains(frage.Rueckfrage.Ersetzt, p => p.Art == KonditionierungPostenart.Luftwechsel);

            KonditionierungStand s = Gut(weg.LuftwechselAufteilen!(Stand(daten)));
            Assert.Equal(0.3, s.Gebaeude.LuftwechselInfiltration.Value, 12);
            Assert.Equal(0.4, s.Gebaeude.LuftwechselNutzer.Value, 12);
            s = Gut(weg.ZelleSetzen!(s, ort, KonditionierungZeile.Nacht, nacht));
            Assert.True(GebaeudeKatalogHuelle.Schreiben(s.Gebaeude, false, name).Erfolg);

            GebaeudeModel m = new GebaeudeStammCtrl().Lies(name);
            Assert.Equal(0.7, Gebaeudemodellvorgaben.WirksamerLuftwechsel(m.Luftwechselrate, m.Luftwechsel_Infiltration,
                                                                          m.Luftwechsel_Nutzer), 12);
            (GebaeudeKatalogDaten neu, _) = Katalog(name);
            Assert.Equal(1.5, neu.Konditionierung.Spalte(KonditionierungGroesse.Lueftung).Nacht.Wert);
        }

        // =============================================================================
        //  Projekt: Gebäude und Zonen über den OK-Weg
        // =============================================================================

        [Fact]
        public void Projekt_Anlegen_und_eine_neue_Zone_mit_eigener_Zelle_stehen_nach_dem_OK_richtig()
        {
            if (!Bereit()) return;
            IReadOnlyDictionary<string, object> gaben = GebaeudeKatalogHuelle.ProjektGaben(PROJEKT, IdZ);
            var daten = (GebaeudeKatalogDaten)gaben["Daten"];
            var weg = (KonditionierungWeg)gaben["Konditionierung"];
            var zonenweg = (GebaeudeZonenweg)gaben["Zonen"];
            Assert.NotNull(daten.Konditionierung);
            Assert.True(weg.Bietet(KonditionierungHandlung.KatalogErneut));

            var a = new GebaeudeArbeitsstand();
            a.Laden(daten, false);
            a.ZonenLaden(zonenweg.Zonen, true);
            ZoneDaten neu = a.NeueZone("Konditionierungsprobe");
            neu.Nutzflaeche = 60;
            neu.Bauteile.Add(new BauteilDaten { Id = -1, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                                Flaeche = 20, Azimut = 180, UWert = 0.3 });
            a.ZoneAnlegen(neu);

            // Die Zone bekommt eine eigene Heiz-Nachtzeile (Festlegung 5) — über den Delegaten.
            KonditionierungStand s = Gut(weg.ZelleSetzen!(new KonditionierungStand(a.Stand, a.Zonen),
                new KonditionierungOrt(KonditionierungGroesse.Heizen, neu.Id), KonditionierungZeile.Nacht,
                new KonditionierungZelle { Von = 20, Bis = 7 }));
            ZoneDaten zone = s.Zonen.Single(z => z.Id == neu.Id);
            Assert.Equal(1, zone.Konditionierung.Fassung);
            Assert.True(a.ZoneErsetzen(zone));

            // OK, Schritt 1 (Gebäude) und Schritt 3 (Zonen samt Konditionierung und Id-Zuordnung).
            Assert.True(GebaeudeKatalogHuelle.ProjektSchreiben(PROJEKT, Gebaeude, a.Stand).Erfolg);
            ZonenSchreibergebnis e = zonenweg.Speichern!(a.Zonenstand(true));
            Assert.True(e.Ok, e.Meldung);
            a.IdsUebernehmen(e);
            a.ZonenGeschrieben();
            int id = a.Zonen.Single(z => z.Bezeichner == "Konditionierungsprobe").Id;
            Assert.True(id > 0);

            // Erneut lesen: alles da.
            IReadOnlyDictionary<string, object> wieder = GebaeudeKatalogHuelle.ProjektGaben(PROJEKT, IdZ);
            ZoneDaten gelesen = ((GebaeudeZonenweg)wieder["Zonen"]).Zonen.Single(z => z.Id == id);
            Assert.Equal(20, gelesen.Konditionierung.Spalte(KonditionierungGroesse.Heizen).Nacht.Von);
            Assert.Equal(7, gelesen.Konditionierung.Spalte(KonditionierungGroesse.Heizen).Nacht.Bis);

            // Ein zweites OK schreibt die Zone nicht noch einmal.
            long zonen = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", Gebaeude)), CultureInfo.InvariantCulture);
            ZoneDaten umbenannt = a.Zonen.Single(z => z.Id == id).Kopie();
            umbenannt.Bezeichner = "Konditionierungsprobe 2";
            Assert.True(a.ZoneErsetzen(umbenannt));
            Assert.True(zonenweg.Speichern!(a.Zonenstand(true)).Ok);
            Assert.Equal(zonen, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", Gebaeude)), CultureInfo.InvariantCulture));
            Assert.Equal(20, new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Zone(Gebaeude, id), out _)
                                 .Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NACHT).Von);
        }

        [Fact]
        public void Der_Rueckfragebefund_und_die_Vorlagenliste_kommen_ueber_den_Weg()
        {
            if (!Bereit()) return;
            (GebaeudeKatalogDaten daten, KonditionierungWeg weg) = Katalog(FreierKatalogsatz());
            KonditionierungStand s = Stand(daten);
            Assert.Null(weg.Rueckfrage!(s, new KonditionierungOrt(KonditionierungGroesse.Heizen), KonditionierungHandlung.MatrixErneut));
            KonditionierungRueckfrage v = weg.Rueckfrage!(s, new KonditionierungOrt(KonditionierungGroesse.Heizen),
                                                          KonditionierungHandlung.VorlageUebernehmen);
            Assert.NotNull(v);
            Assert.Contains(v.Ersetzt, p => p.Art == KonditionierungPostenart.Matrixzellen);

            IReadOnlyList<KonditionierungVorlageDaten> vorlagen = weg.Vorlagen!(KonditionierungGroesse.Heizen);
            Assert.NotEmpty(vorlagen);                        // die Saat der ausgelieferten Vorlagen
            KonditionierungStand mit = Gut(weg.VorlageUebernehmen!(s, new KonditionierungOrt(KonditionierungGroesse.Heizen),
                                                                   vorlagen[0].Id));
            KonditionierungKalender k = mit.Gebaeude.Konditionierung.Spalte(KonditionierungGroesse.Heizen).Kalender;
            Assert.Equal(KonditionierungZustand.Angelegt, k.Zustand);
            Assert.Equal(vorlagen[0].Name, k.Vorlage);
            Assert.Equal("", weg.Pruefen!(mit));
        }

        // =============================================================================
        //  Der Bezug des Projekts (Stufe KP2, Welle U1, Teilschritt 4 (c))
        // =============================================================================

        /// <summary>
        /// <b>Im Projekt rechnet der Weg mit den Daten des Projekts</b> — wie der Lauf
        /// (<c>Vdi6007Rechenweg</c>: <c>Waermeuebergabe.KopplungWirksamFuer</c> mit der Stufe aus
        /// <c>Tab_Einstellungen.Anlagenkopplung</c>, das Referenzjahr des Laufs): Im gekoppelten Projekt
        /// 1047 (AK1, Heizkreis an, Radiator) ersetzt das Zeitprogramm <c>Sollwertprofil</c> die Woche der
        /// Heizspalte, und „Kalender anlegen" schreibt genau diese Woche — „abgeleitet bis angelegt,
        /// bitgleich" (Teilkonzept 3.3). Der Katalog kennt kein Projekt: keine Kopplung, Bezugsjahr 2025.
        /// </summary>
        [Fact]
        public void Im_Projekt_gelten_Anlagenkopplung_und_Referenzjahr_des_Projekts()
        {
            if (!Bereit()) return;
            const int AK1 = 1047;
            int idZ = Z_ProjGebCtrl.LiesProjekt(AK1)[0].ID_Z;
            IReadOnlyDictionary<string, object> gaben = GebaeudeKatalogHuelle.ProjektGaben(AK1, idZ);
            var daten = (GebaeudeKatalogDaten)gaben["Daten"];
            var weg = (KonditionierungWeg)gaben["Konditionierung"];
            Assert.True(daten.HeizkreisAktiv);

            // Ein Zeitprogramm der Kopplung - nur im Arbeitsstand, die Datenbank bleibt, wie sie ist.
            double[] woche = Enumerable.Range(0, 168).Select(h => h % 24 >= 6 && h % 24 < 21 ? 21.0 : 16.0).ToArray();
            daten.Sollwertprofil = AnlagenkopplungSchema.WochenprofilSchreiben(woche);

            KonditionierungStand s = Gut(weg.Anlegen!(Stand(daten), new KonditionierungOrt(KonditionierungGroesse.Heizen)));
            KonditionierungKalender k = s.Gebaeude.Konditionierung.Spalte(KonditionierungGroesse.Heizen).Kalender;
            Assert.Equal(KonditionierungZustand.Angelegt, k.Zustand);
            Assert.NotNull(k.Woche);
            Assert.Equal(woche, k.Woche);

            // Der Bezug selbst: Stufe und Raster aus dem Projekt (E115: w₀ der Klimaregion, ohne Preisreihe kein Jahr),
            // im Katalog das Rückfallraster; der Arbeitsstand rechnet im Raster des Bezugs.
            KonditionierungHuelle.Bezug projekt = KonditionierungHuelle.Projektbezug(AK1);
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK1, projekt.Stufe);
            Assert.Equal(Konditionierungdatenweg.Raster(AK1), projekt.Raster);
            Assert.Equal(SolardatenCtrl.Preisreihenjahr(AK1) ?? 0, projekt.Wirksam.Jahr);
            Assert.Equal(projekt.Wirksam,
                         KonditionierungHuelle.Arbeitsstand(Stand(daten), Kalendereigentuemer.Gebaeude, projekt).Kalender);
            Assert.Equal(2024, KonditionierungHuelle.Arbeitsstand(Stand(daten), Kalendereigentuemer.Gebaeude,
                                                                   projekt with { Raster = Gemeinjahrkalender.Kalenderjahr(2024) })
                                                    .Feiertagsjahr);
            KonditionierungHuelle.Bezug katalog = KonditionierungHuelle.Projektbezug(0);
            Assert.Null(katalog.Stufe);
            Assert.Equal(Konditionierungdatenweg.Rueckfallraster,
                         KonditionierungHuelle.Arbeitsstand(Stand(daten), Kalendereigentuemer.Katalogbau, katalog).Kalender);
        }

        /// <summary>
        /// <b>Die Wochenvorschau trägt den Namen ihrer Größe</b> (Stufe KP2, Welle U1, Teilschritt 4 (d)):
        /// „Vorschau: Woche · Kühlen" statt des Titels des Sollwert-Zeitprogramms der Wärmeübergabe — fünf
        /// Größen, fünf Titel, jede mit ihrer Einheit.
        /// </summary>
        [Fact]
        public void Die_Wochenvorschau_traegt_den_Titel_ihrer_Groesse()
        {
            if (!Bereit()) return;
            (_, KonditionierungWeg weg) = Katalog(FreierKatalogsatz());
            double[] woche = Enumerable.Repeat(24.0, 168).ToArray();

            foreach ((KonditionierungGroesse g, string name) in new[]
                     {
                         (KonditionierungGroesse.Heizen, "Heizen"), (KonditionierungGroesse.Kuehlen, "Kühlen"),
                         (KonditionierungGroesse.Lueftung, "Lüftung"), (KonditionierungGroesse.Geraete, "Geräte"),
                         (KonditionierungGroesse.Personen, "Personen")
                     })
            {
                WindowsFormsApplication1.Zeichnung.Zeichenmodell m = weg.WochenVorschau!(g, woche);
                Assert.NotNull(m);
                string svg = WindowsFormsApplication1.Zeichnung.SvgSchreiber.Text(m);
                Assert.Contains("Vorschau: Woche · " + name, svg);
                Assert.DoesNotContain(GebaeudeKatalogHuelle.UebergabeTexte().Raster.BildTitel, svg);
            }
        }

        // =============================================================================
        //  Der Reiter über den echten Weg (Stufe KP2, Welle U1, Teilschritt 3)
        // =============================================================================

        /// <summary>
        /// <b>Die Handlungen der Karte gehen über die Delegaten des Wegs</b> — dieselbe
        /// <see cref="KonditionierungBearbeitung"/>, die der Reiter bedient: „Kalender anlegen",
        /// „Verwerfen" mit der Rückfrage aus dem Befund VOR dem Schreiben (Vorgabe Nein), „Zurücknehmen"
        /// (eine Stufe), die Rückfrage „aufteilen" (E56 F5 (a)) als EIN Schritt — und geschrieben wird
        /// erst im OK-Weg: Bis dahin steht in der Datenbank nichts.
        /// </summary>
        [Fact]
        public void Die_Bearbeitung_des_Reiters_geht_ueber_den_echten_Weg_und_schreibt_erst_mit_OK()
        {
            if (!Bereit()) return;
            string name = FreierKatalogsatz();
            (GebaeudeKatalogDaten daten, KonditionierungWeg weg) = Katalog(name);
            daten.LuftwechselInfiltration = null;
            daten.LuftwechselNutzer = null;
            daten.Luftwechselrate = 0.7;
            long kalender = Hoechste(KonditionierungSchema.TAB_KALENDER);

            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(daten, neu: false);
            var meldungen = new List<string>();
            var b = new KonditionierungBearbeitung(arbeit, () => weg) { Melden = (m, _) => meldungen.Add(m) };
            Assert.True(b.MitWeg);
            Assert.Null(b.Sperrgrund);

            // Kalender anlegen - der Generator schreibt ihn in den Arbeitsstand.
            Assert.True(b.Anlegen(KonditionierungGroesse.Heizen));
            Assert.True(b.Angelegt(KonditionierungGroesse.Heizen));
            Assert.StartsWith("angelegt", b.Zustand(KonditionierungGroesse.Heizen));

            // Verwerfen fragt aus dem Befund, Vorgabe „Nein"; „Nein" lässt alles.
            b.Verwerfen(KonditionierungGroesse.Heizen);
            Assert.NotNull(b.OffeneFrage);
            Assert.True(b.OffeneFrage!.VorgabeNein);
            Assert.StartsWith("Den angelegten Kalender „Heizen“ verwerfen? Es fällt: 1 Kalender", b.OffeneFrage.Text);
            b.Beantworten(false);
            Assert.True(b.Angelegt(KonditionierungGroesse.Heizen));

            b.Verwerfen(KonditionierungGroesse.Heizen);
            b.Beantworten(true);
            Assert.False(b.Angelegt(KonditionierungGroesse.Heizen));

            // Zurücknehmen: eine Stufe - der Kalender ist wieder da, danach nichts mehr.
            Assert.True(b.Zuruecknehmen());
            Assert.True(b.Angelegt(KonditionierungGroesse.Heizen));
            Assert.False(b.Zuruecknehmen());
            Assert.Single(meldungen);

            // F5 (a): die Lüftung an einem Gebäude mit Gesamtangabe - die Frage nennt Rate und Aufteilung.
            Assert.False(b.WertSetzen(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht, 1.5));
            Assert.NotNull(b.OffeneFrage);
            Assert.False(b.OffeneFrage!.VorgabeNein);
            Assert.Contains("Luftwechselrate 0,7 1/h", b.OffeneFrage.Text);
            Assert.Contains("Infiltration 0,3 1/h und Nutzerlüftung 0,4 1/h", b.OffeneFrage.Text);
            b.Beantworten(true);
            Assert.Equal(0.3, arbeit.Stand.LuftwechselInfiltration!.Value, 12);
            Assert.Equal(0.4, arbeit.Stand.LuftwechselNutzer!.Value, 12);
            Assert.Equal(1.5, b.Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht));

            // Bis hierher hat nichts geschrieben.
            Assert.Equal(kalender, Hoechste(KonditionierungSchema.TAB_KALENDER));

            // Ein Schritt: „Zurücknehmen" stellt Gesamtangabe und leere Zelle wieder her.
            Assert.True(b.Zuruecknehmen());
            Assert.Equal(0.7, arbeit.Stand.Luftwechselrate);
            Assert.Null(arbeit.Stand.LuftwechselInfiltration);
            Assert.Null(b.Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht));
            Assert.True(b.Angelegt(KonditionierungGroesse.Heizen));

            // OK - der Kalender steht danach in der Datenbank.
            Assert.True(GebaeudeKatalogHuelle.Schreiben(arbeit.Stand, false, name).Erfolg);
            (GebaeudeKatalogDaten neu, _) = Katalog(name);
            Assert.Equal(KonditionierungZustand.Angelegt, neu.Konditionierung.Spalte(KonditionierungGroesse.Heizen).Kalender.Zustand);
        }
    }
}
