using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle der Überlagerung „Auslegung"</b> (<c>EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Auslegung.cs</c>;
    /// Umsetzungskonzept Zapfprofilgenerator 4.5, 4.7, 5.1, 5.5; Stufe Z2, Gruppe 2).
    ///
    /// <para><b>Ohne Datenbank:</b> die Abbildung Eingaben ↔ Projektgrößen samt Entwurf, der
    /// Arbeitsstand ändert die Projektgrößen nur, wenn die Auslegung berührt wurde, die Warnliste
    /// mit Titel und Satz des Kerns — und als Wachen, dass jede Hinweiskennung und jede Ablehnung
    /// der Auslegung ihren Text in beiden Sprachen hat.</para>
    ///
    /// <para><b>Auf der Arbeitskopie der Testdatenbank</b> (Projekt 1006, fiktiver Katalog TEST-1
    /// samt Auslegungsparametern aus <see cref="AuslegungTestbau.ParameterEinspielen"/>): der Stand
    /// beim Öffnen, der Konstruktor, die Auslegung mit Entwurf und der Schreibweg im gemeinsamen
    /// Vorgang. Ohne Testdatenbank schweigen diese Fälle. Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilAuslegungHuelleTests : IDisposable
    {
        private const int PROJEKT = 1006;
        private const string VERSION = "TEST-1";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        // =================================================================================
        // Wachen: Texte der Auslegung in beiden Sprachen
        // =================================================================================

        /// <summary>
        /// Jede Hinweiskennung, die der Rechenweg der Auslegung vergibt — gelesen aus dem QUELLTEXT
        /// (Literale in <c>new Auslegungshinweis("…"</c>) samt den benannten Konstanten —, steht in
        /// <see cref="ZapfprofilHuelle.AUSLEGUNGSHINWEISE"/> und hat ihren Titel in beiden Sprachen.
        /// </summary>
        [Fact]
        public void Jede_Hinweiskennung_der_Auslegung_hat_ihren_Titel_in_beiden_Sprachen()
        {
            var kennungen = new SortedSet<string>(StringComparer.Ordinal);
            var muster = new Regex(@"new Auslegungshinweis\(\s*""(?<k>[A-Z][A-Z0-9_]+)""");
            foreach (string datei in new[] { Pfad("EPOS.Kern", "Allgemein", "Zapfprofil"), Pfad("EPOS.Kern", "Controller") }
                                     .SelectMany(o => Directory.GetFiles(o, "*.cs")))
                foreach (Match m in muster.Matches(File.ReadAllText(datei)))
                    kennungen.Add(m.Groups["k"].Value);
            Assert.True(kennungen.Count >= 20, "Nur " + kennungen.Count + " Kennungen gefunden: " + string.Join(", ", kennungen));

            string[] ungelistet = kennungen.Where(k => !ZapfprofilHuelle.AUSLEGUNGSHINWEISE.Contains(k)).ToArray();
            Assert.True(ungelistet.Length == 0, "Nicht in AUSLEGUNGSHINWEISE: " + string.Join(", ", ungelistet));

            string[] fehlend = ZapfprofilHuelle.AUSLEGUNGSHINWEISE.Select(ZapfprofilHuelle.AuslegungsHinweisSchluessel)
                .Where(k => string.IsNullOrEmpty(Text(k, DE)) || string.IsNullOrEmpty(Text(k, EN))).ToArray();
            Assert.True(fehlend.Length == 0, "Ohne Titel: " + string.Join(", ", fehlend));
            Assert.Equal(ZapfprofilHuelle.AUSLEGUNGSHINWEISE.Length, ZapfprofilHuelle.AUSLEGUNGSHINWEISE.Distinct().Count());
        }

        [Fact]
        public void Jede_Topologie_und_jedes_Verfahren_der_Auslegung_hat_seinen_Text()
        {
            var schluessel = new List<string>();
            schluessel.AddRange(Enum.GetValues(typeof(ZapfTopologie)).Cast<ZapfTopologie>()
                                    .Select(t => "ZPG_AUS_TOPOLOGIE_" + ZapfprofilHuelle.Gross(t.ToString())));
            schluessel.AddRange(Enum.GetValues(typeof(ZapfSpeicherverfahren)).Cast<ZapfSpeicherverfahren>()
                                    .Select(v => "ZPG_AUS_VERFAHREN_" + ZapfprofilHuelle.Gross(v.ToString())));
            Assert.Equal(4 + 4, schluessel.Count);
            Assert.Contains("ZPG_AUS_VERFAHREN_DIN4708", schluessel);

            string[] fehlend = schluessel.Where(k => string.IsNullOrEmpty(Text(k, DE)) || string.IsNullOrEmpty(Text(k, EN))).ToArray();
            Assert.True(fehlend.Length == 0, "Ohne Text in beiden Sprachen: " + string.Join(", ", fehlend));
        }

        [Fact]
        public void Die_Warnliste_nennt_Titel_und_Satz_des_Kerns()
        {
            ZapfprofilWarnDaten w = ZapfprofilHuelle.Warnung(new Auslegungshinweis("MEHRSPEICHER",
                ZapfSatz.Neu("AUSHINWEIS_MEHRSPEICHER_PUNKT", 1500.0, 1000.0), true));
            Assert.Equal("ZPG_AUSHINW_MEHRSPEICHER", w.Kennung);
            Assert.Equal("Mehrspeicheranlage", w.Titel);
            Assert.Contains("1500", w.Text);
            Assert.DoesNotContain("AUSHINWEIS_", w.Text);
            Assert.Equal(ZapfprofilWarnstufe.Warnung, w.Stufe);

            // Ein Befund der Bilanz in der Warnliste der Auslegung: Titel und Kennung der Bilanz, Stufe wie dort.
            foreach (string code in new[] { Mengengeruest.HINWEIS_BANDBREITE, Mengengeruest.HINWEIS_WOHNUNGSTABELLE, "TAGESGANG_SUMME",
                                            "ZIRKULATION_OHNE_FLAECHE", "ZIRKULATION_ZONE_OHNE_FLAECHE", "ZIRKULATION_OHNE_ZONE",
                                            "ZIRKULATION_NICHT_IN_Z1" })
            {
                ZapfprofilWarnDaten b = ZapfprofilHuelle.Warnung(new Auslegungshinweis(code, ZapfSatz.Neu("UNBEKANNT_NEU"), true));
                Assert.Equal("ZPG_WARN_" + code, b.Kennung);
                Assert.Equal(Text("ZPG_WARN_" + code, DE), b.Titel);
                Assert.NotEqual("Hinweis", b.Titel);
                Assert.Equal(ZapfprofilWarnstufe.Warnung, b.Stufe);
            }

            // Eine Kennung ohne Titel bekommt den allgemeinen — benannt statt still.
            ZapfprofilWarnDaten u = ZapfprofilHuelle.Warnung(new Auslegungshinweis("UNBEKANNT_NEU", ZapfSatz.Neu("UNBEKANNT_NEU")));
            Assert.Equal("Hinweis", u.Titel);
            Assert.Equal(ZapfprofilWarnstufe.Hinweis, u.Stufe);
        }

        [Fact]
        public void Das_Textbuendel_der_Auslegung_steht_in_der_Oberflaechensprache()
        {
            ZapfprofilAuslegungTexte de = ZapfprofilHuelle.AuslegungTexte();
            Assert.Equal("Auslegung Brauchwasser", de.Titel);
            Assert.Equal("Schnellauslegung", de.Schnellauslegung);
            Assert.Equal("An Speicherauslegung übergeben…", de.KnopfUebergeben);

            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = EN;
                ZapfprofilAuslegungTexte en = ZapfprofilHuelle.AuslegungTexte();
                Assert.Equal("Domestic hot water design", en.Titel);
                Assert.Equal("Quick design", en.Schnellauslegung);
                Assert.Equal("Cumulative curve of the design day", ZapfprofilHuelle.AuslegungBildtexte().TitelSummenlinie);
            }
            finally { CultureInfo.CurrentUICulture = vorher; }
        }

        // =================================================================================
        // Abbildung Eingaben <-> Projektgrößen
        // =================================================================================

        private static ZapfprofilBedarfstagDaten Entwurf() => new ZapfprofilBedarfstagDaten
        {
            Bezeichner = "Probetag (fiktiv)",
            Quelle = ZapfprofilBedarfstagquelle.Konstruktor,
            Katalogversion = VERSION,
            Ereignisse = { new ZapfprofilEreignisDaten(420, 30, 4.0), new ZapfprofilEreignisDaten(1100, 60, 6.0) }
        };

        [Fact]
        public void Die_Eingaben_gehen_in_die_Projektgroessen_und_zurueck()
        {
            var basis = new ProjektStand { Id = 3, Seed = 1, Realisierungen = 10, Perzentil = 99, UebertragerUaWJeK = 800.0 };
            var a = new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.A100Referenz,
                IdBedarfstag = 17,
                SpeicherC = 58.0,
                ErzeugerKw = 20.0,
                UebertragerKw = 15.0,
                Speicherart = ZapfprofilSpeicherart.GemischterSpeicher,
                SensorhoeheAnteil = 0.6,
                Erzeugerart = ZapfprofilErzeugerart.Waermepumpe,
                Werkstoff = ZapfprofilWerkstoff.Edelstahl,
                PunktVolumenL = 370.0,
                PunktLeistungKw = 15.0
            };

            ProjektStand p = ZapfprofilHuelle.MitAuslegung(basis, a);
            Assert.Equal(ZapfBedarfstagquelle.A100Referenz, p.BedarfstagQuelle);
            Assert.Equal(17, p.IdBedarfstag);
            Assert.Equal(58.0, p.SpeicherC);
            Assert.Equal(20.0, p.ErzeugerKw);
            Assert.Equal(15.0, p.UebertragerKw);
            Assert.Equal(800.0, p.UebertragerUaWJeK);                     // was die Überlagerung nicht führt, bleibt
            Assert.Equal(ZapfSpeicherart.GemischterSpeicher, p.Speicherart);
            Assert.Equal(0.6, p.SensorhoeheAnteil);
            Assert.Equal(370.0, p.AuslegungVolumenL);
            Assert.Equal(15.0, p.AuslegungLeistungKw);
            Assert.Equal(1, p.Seed);

            ZapfprofilAuslegungEingabeDaten zurueck = ZapfprofilHuelle.AuslegungAusStand(new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], p));
            Assert.Equal(ZapfprofilBedarfstagquelle.A100Referenz, zurueck.Quelle);
            Assert.Equal(17, zurueck.IdBedarfstag);
            Assert.Equal(58.0, zurueck.SpeicherC);
            Assert.Equal(ZapfprofilSpeicherart.GemischterSpeicher, zurueck.Speicherart);
            Assert.Equal(370.0, zurueck.PunktVolumenL);
            // Erzeugerart und Werkstoff sind Projektgrößen (Schritt 124) — sie kommen zurück.
            Assert.Equal(ZapfErzeugerart.Waermepumpe, p.Erzeugerart);
            Assert.Equal(ZapfUebertragerwerkstoff.Edelstahl, p.UebertragerWerkstoff);
            Assert.Equal(ZapfprofilErzeugerart.Waermepumpe, zurueck.Erzeugerart);
            Assert.Equal(ZapfprofilWerkstoff.Edelstahl, zurueck.Werkstoff);

            // Vorgaberegel und Stundenprofil tragen keinen Katalogtag.
            Assert.Null(ZapfprofilHuelle.MitAuslegung(basis, new ZapfprofilAuslegungEingabeDaten { IdBedarfstag = 17 }).BedarfstagQuelle);
            Assert.Null(ZapfprofilHuelle.MitAuslegung(basis, new ZapfprofilAuslegungEingabeDaten { IdBedarfstag = 17 }).IdBedarfstag);
            ProjektStand stunde = ZapfprofilHuelle.MitAuslegung(basis, new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.Stundenprofil, IdBedarfstag = 17
            });
            Assert.Equal(ZapfBedarfstagquelle.Stundenprofil, stunde.BedarfstagQuelle);
            Assert.Null(stunde.IdBedarfstag);
            Assert.Null(ZapfprofilHuelle.MitAuslegung(null, a));
        }

        [Fact]
        public void Der_Entwurf_geht_als_Katalogzeile_des_Kerns_und_zurueck()
        {
            var a = new ZapfprofilAuslegungEingabeDaten { Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = Entwurf(), IdBedarfstag = 5 };
            BedarfstagKatalogzeile t = ZapfprofilHuelle.EntwurfAus(a);
            Assert.Equal(ZapfprofilCtrl.ENTWURF_ID, t.Id);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, t.QuelleArt);
            Assert.Equal(ZapfKatalogstatus.Eigen, t.Status);
            Assert.Equal(Herkunftsart.Eigenkonstruktion, t.Herkunft.Art);
            Assert.Equal(new[] { 420, 1100 }, t.Ereignisse.Select(e => e.MinuteBeginn).ToArray());
            Assert.Null(ZapfprofilHuelle.MitAuslegung(new ProjektStand(), a).IdBedarfstag);       // die Id entsteht beim Schreiben
            Assert.Null(ZapfprofilHuelle.EntwurfAus(new ZapfprofilAuslegungEingabeDaten { Entwurf = Entwurf() }));

            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null) { BedarfstagEntwurf = t };
            ZapfprofilAuslegungEingabeDaten zurueck = ZapfprofilHuelle.AuslegungAusStand(stand);
            Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, zurueck.Quelle);
            Assert.Null(zurueck.IdBedarfstag);
            Assert.Equal("Probetag (fiktiv)", zurueck.Entwurf.Bezeichner);
            Assert.Equal(0, zurueck.Entwurf.Id);
            Assert.Equal(2, zurueck.Entwurf.Ereignisse.Count);
            Assert.Equal(10.0, zurueck.Entwurf.TagessummeKwh, 9);
        }

        [Fact]
        public void Der_Arbeitsstand_aendert_die_Projektgroessen_nur_mit_beruehrter_Auslegung()
        {
            var projekt = new ProjektStand { Id = 3, Seed = 7, SpeicherC = 55.0 };
            var basis = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], projekt);
            var e = new ZapfprofilEingabeDaten();

            ZapfprofilStand ohne = ZapfprofilHuelle.AlsStand(e, basis);
            Assert.Same(projekt, ohne.Projekt);
            Assert.Null(ohne.BedarfstagEntwurf);

            e.Auslegung = new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = Entwurf(), SpeicherC = 60.0, PunktVolumenL = 300.0
            };
            ZapfprofilStand mit = ZapfprofilHuelle.AlsStand(e, basis);
            Assert.Equal(60.0, mit.Projekt.SpeicherC);
            Assert.Equal(300.0, mit.Projekt.AuslegungVolumenL);
            Assert.Equal(7, mit.Projekt.Seed);
            Assert.Equal("Probetag (fiktiv)", mit.BedarfstagEntwurf.Bezeichner);

            // Die Übernahme des Dialogs trägt die Auslegung mit; die Kopie ist unabhängig.
            ZapfprofilStand uebernommen = ZapfprofilHuelle.Uebernahme(new ZapfprofilErgebnisDaten(e), basis);
            Assert.Equal(BrauchwasserWeg.Generator, uebernommen.Weg);
            Assert.Equal(60.0, uebernommen.Projekt.SpeicherC);
            ZapfprofilEingabeDaten k = e.Kopie();
            k.Auslegung.SpeicherC = 50.0;
            k.Auslegung.Entwurf.Bezeichner = "anders";
            Assert.Equal(60.0, e.Auslegung.SpeicherC);
            Assert.Equal("Probetag (fiktiv)", e.Auslegung.Entwurf.Bezeichner);
        }

        [Fact]
        public void Ein_ueberholter_Punkt_wird_verworfen_und_der_Rest_bleibt()
        {
            var projekt = new ProjektStand { Id = 3, Seed = 7, SpeicherC = 55.0, AuslegungVolumenL = 400.0, AuslegungLeistungKw = 20.0 };
            var basis = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], projekt);

            // Der Punkt des Stands beim Öffnen bleibt, solange keine Zone die Auslegung geändert hat …
            Assert.Equal(400.0, ZapfprofilHuelle.AlsStand(new ZapfprofilEingabeDaten(), basis).Projekt.AuslegungVolumenL);

            // … und fällt, sobald der Dialog ihn als überholt meldet; die übrigen Größen bleiben.
            ProjektStand ohne = ZapfprofilHuelle.AlsStand(new ZapfprofilEingabeDaten { PunktUeberholt = true }, basis).Projekt;
            Assert.Null(ohne.AuslegungVolumenL);
            Assert.Null(ohne.AuslegungLeistungKw);
            Assert.Equal(55.0, ohne.SpeicherC);
            Assert.Equal(7, ohne.Seed);

            // Ebenso der Punkt, den der Arbeitsstand mit OK der Überlagerung trägt.
            var e = new ZapfprofilEingabeDaten
            {
                PunktUeberholt = true,
                Auslegung = new ZapfprofilAuslegungEingabeDaten { SpeicherC = 60.0, PunktVolumenL = 300.0, PunktLeistungKw = 25.0 }
            };
            ProjektStand mit = ZapfprofilHuelle.AlsStand(e, basis).Projekt;
            Assert.Null(mit.AuslegungVolumenL);
            Assert.Null(mit.AuslegungLeistungKw);
            Assert.Equal(60.0, mit.SpeicherC);
            Assert.Null(ZapfprofilHuelle.Uebernahme(new ZapfprofilErgebnisDaten(e), basis).Projekt.AuslegungVolumenL);
            Assert.True(e.Kopie().PunktUeberholt);
            Assert.Null(ZapfprofilHuelle.OhnePunkt(null));
        }

        [Fact]
        public void Ohne_Zone_rechnet_die_Auslegung_nicht_und_sagt_es()
        {
            ZapfprofilAuslegungDaten d = ZapfprofilHuelle.Auslegung(PROJEKT, new ZapfprofilEingabeDaten(),
                new ZapfprofilAuslegungEingabeDaten(), null, ZapfprofilStufe.Einfach);
            Assert.Equal(ZapfprofilAuslegungZustand.NichtGerechnet, d.Zustand);
            Assert.Empty(d.Gruppen);
            Assert.Null(d.Punktgruppe);
            Assert.Equal("ZPG_AUS_MSG_KEINE_ZONE", Assert.Single(d.Meldungen).Kennung);
            Assert.StartsWith("Keine Auslegung — ", d.Status);
        }

        // =================================================================================
        // Auf der Testdatenbank
        // =================================================================================

        private static ZapfprofilEingabeDaten Zonen() => new ZapfprofilEingabeDaten
        {
            Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = Nutzungsart("Testnutzung A (fiktiv)"), Bezugsmenge = 8 } }
        };

        [Fact]
        public void Der_Stand_beim_Oeffnen_nennt_Bedarfstage_Regeln_und_ohne_Tag_den_Konstruktor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            RegelAnlegen();

            ZapfprofilAuslegungStartDaten s = ZapfprofilHuelle.AuslegungStart(PROJEKT, Zonen(), null, ZapfprofilStufe.Einfach);

            Assert.True(s.Verfuegbar, s.Sperrgrund);
            Assert.Equal(ZapfprofilBedarfstagquelle.Vorgaberegel, s.Eingabe.Quelle);
            Assert.NotEmpty(s.Bedarfstage);
            Assert.All(s.Bedarfstage, t => Assert.True(t.Id > 0));
            Assert.All(s.Bedarfstage.Where(t => t.Quelle == ZapfprofilBedarfstagquelle.Din4708Profil), t => Assert.False(t.Waehlbar));
            Assert.Contains(s.Regeln, r => r.Name == "Probebrause" && r.VolumenJeVorgangL == 40.0);
            Assert.Equal("Eigener Bedarfstag", s.NameVorschlag);
            Assert.Contains("1 Zonen", s.Kontext);

            ZapfprofilAuslegungDaten e = s.Ergebnis;
            Assert.Equal(ZapfprofilAuslegungZustand.Gerechnet, e.Zustand);
            ZapfprofilAuslegungsgruppeDaten g = Assert.Single(e.Gruppen);
            Assert.True(g.Speicher);
            Assert.Equal("Speicher", g.Topologie);
            Assert.True(g.KonstruktorOeffnen);
            Assert.False(g.Empfehlung.Rechenbar);
            Assert.Equal(ZapfprofilKartenstand.NichtGerechnet, g.Perzentil.Stand);
            Assert.Contains(g.Warnliste, w => w.Kennung == "ZPG_AUSHINW_KONSTRUKTOR_OEFFNEN" && w.Titel == "Bedarfstag fehlt");
            Assert.Null(e.Punktgruppe);
        }

        /// <summary>
        /// Die neun Ecodesign-Zapfprofile (Quelle 5, N11 (e), N26): Führt der Katalog seine Zeilen —
        /// die Testdatenbank trägt die Lastprofile XXS bis 4XL der Verordnung (EU) Nr. 814/2013 —,
        /// stehen sie als wählbare Katalogtage in der Wahl, und die Sperrzeile des Dialogs entfällt,
        /// denn sie hängt allein daran, ob eine Zeile dieser Art da ist. Ohne die Zeilen ist sie
        /// wieder da.
        /// </summary>
        [Fact]
        public void Das_Ecodesign_Zapfprofil_steht_als_Katalogtag_und_die_Sperre_faellt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);

            ZapfprofilAuslegungStartDaten s = ZapfprofilHuelle.AuslegungStart(PROJEKT, Zonen(), null, ZapfprofilStufe.Einfach);
            Assert.True(s.Verfuegbar, s.Sperrgrund);
            Assert.Equal(9, s.Bedarfstage.Count(t => t.Quelle == ZapfprofilBedarfstagquelle.Ecodesign));
            ZapfprofilBedarfstagDaten eco = Assert.Single(s.Bedarfstage,
                t => t.Quelle == ZapfprofilBedarfstagquelle.Ecodesign && t.Bezeichner == "Ecodesign-Zapfprofil L");
            Assert.True(eco.Waehlbar, eco.Sperrgrund);
            Assert.True(eco.Id > 0);
            Assert.Equal(ZapfprofilCtrl.Bedarfstage().Single(t => t.QuelleArt == ZapfBedarfstagquelle.Ecodesign
                                                                   && t.Bezeichner == "Ecodesign-Zapfprofil L")
                                         .Ereignisse.Sum(e => e.EnergieKwh), eco.TagessummeKwh, 9);

            // Die Sperre hängt allein an der Katalogzeile: ohne sie kein Ecodesign-Tag in der Wahl.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwBedarfstag_STAMM WHERE Quelle_Art = ?",
                                           new DbParam("@art", (int)ZapfBedarfstagquelle.Ecodesign));
            ZapfprofilAuslegungStartDaten ohne = ZapfprofilHuelle.AuslegungStart(PROJEKT, Zonen(), null, ZapfprofilStufe.Einfach);
            Assert.DoesNotContain(ohne.Bedarfstage, t => t.Quelle == ZapfprofilBedarfstagquelle.Ecodesign);
        }

        /// <summary>
        /// Hausregel „Ein Parametersatz aus einer Hülle trifft nur [Parameter]": Die Schlüssel von
        /// <c>Gaben</c> sind Parameter des Zapfprofil-Dialogs, die des Delegaten
        /// <c>AuslegungGaben</c> Parameter der Überlagerung — je mit passendem Typ.
        /// </summary>
        [Fact]
        public void Die_Parametersaetze_treffen_nur_Parameter_der_Komponenten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);

            IReadOnlyDictionary<string, object> gaben = ZapfprofilHuelle.Gaben(PROJEKT, null);
            ParameterPruefen(typeof(ZapfprofilDialog), gaben);

            var auslegungGaben = (Func<ZapfprofilEingabeDaten, bool, IReadOnlyDictionary<string, object>>)gaben["AuslegungGaben"];
            IReadOnlyDictionary<string, object> ausl = auslegungGaben(Zonen(), false);
            ParameterPruefen(typeof(ZapfprofilAuslegungDialog), ausl);
            Assert.True(((ZapfprofilAuslegungStartDaten)ausl["Daten"]).Verfuegbar);
            Assert.IsType<ZapfprofilAuslegungTexte>(ausl["Texte"]);
            // Mit dem Schalter (Fußknopf „Stochastisch rechnen") dieselben Parameter.
            ParameterPruefen(typeof(ZapfprofilAuslegungDialog), auslegungGaben(Zonen(), true));
        }

        private static void ParameterPruefen(Type komponente, IReadOnlyDictionary<string, object> gaben)
        {
            foreach (KeyValuePair<string, object> g in gaben)
            {
                System.Reflection.PropertyInfo p = komponente.GetProperty(g.Key);
                Assert.True(p != null, komponente.Name + " kennt keinen Parameter „" + g.Key + "“.");
                Assert.True(p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.ParameterAttribute), true).Length == 1,
                            komponente.Name + "." + g.Key + " ist kein [Parameter].");
                Assert.True(g.Value == null || p.PropertyType.IsInstanceOfType(g.Value),
                            komponente.Name + "." + g.Key + ": " + g.Value?.GetType().Name + " passt nicht zu " + p.PropertyType.Name);
            }
        }

        [Fact]
        public void Der_Konstruktor_der_Huelle_prueft_benannt_und_liefert_den_Entwurf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            RegelAnlegen();

            // Benannte Ablehnungen: ohne Namen, leeres Fenster, fehlende Menge, unbekannte Regel.
            ZapfprofilKonstruktorErgebnis falsch = ZapfprofilHuelle.BedarfstagKonstruieren(new[]
            {
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 8, EndeH = 7, VolumenL = 10, ZapftemperaturC = 45 },
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 8 },
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 8, Regel = "Unbekannt", Anzahl = 2 }
            }, " ");
            Assert.Null(falsch.Tag);
            Assert.Equal(new[] { "ZPG_AUS_KON_OHNE_NAME", "ZPG_AUS_KON_ZEILE", "ZPG_AUS_KON_ZEILE", "ZPG_AUS_KON_ZEILE" },
                         falsch.Meldungen.Select(m => m.Kennung).ToArray());
            Assert.StartsWith("Zeile 1: Beginn und Ende", falsch.Meldungen[1].Text);
            Assert.StartsWith("Zeile 2: Anzahl bzw. Volumen", falsch.Meldungen[2].Text);
            Assert.Equal("Zeile 3: Die Zapfregel „Unbekannt“ steht nicht im Katalog.", falsch.Meldungen[3].Text);
            Assert.Equal("ZPG_AUS_KON_OHNE_ZEILE",
                         Assert.Single(ZapfprofilHuelle.BedarfstagKonstruieren(new ZapfprofilKonstruktorZeileDaten[0], "Name").Meldungen).Kennung);

            // Eine Regel mit Anzahl und eine Zeile mit Volumen: der Entwurf.
            ZapfprofilKonstruktorErgebnis gut = ZapfprofilHuelle.BedarfstagKonstruieren(new[]
            {
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 7.5, Regel = "Probebrause", Anzahl = 3, Verbraucher = "Bad" },
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 18, EndeH = 19, VolumenL = 100, ZapftemperaturC = 52 }
            }, "Probetag (fiktiv)");
            Assert.Empty(gut.Meldungen);
            ZapfprofilBedarfstagDaten t = gut.Tag;
            Assert.Equal(0, t.Id);
            Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, t.Quelle);
            Assert.Equal(VERSION, t.Katalogversion);
            Assert.Equal(new[] { 420, 1080 }, t.Ereignisse.Select(e => e.MinuteBeginn).ToArray());
            Assert.Equal(new[] { 30, 60 }, t.Ereignisse.Select(e => e.DauerMin).ToArray());
            double cw = Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K;
            Assert.Equal(3 * 40.0 * cw * (40.0 - 12.0) / 1000.0, t.Ereignisse[0].EnergieKwh, 9);   // θ_KW,A des Katalogs: 12 °C
            Assert.Equal(t.Ereignisse.Sum(e => e.EnergieKwh), t.TagessummeKwh, 9);
            Assert.StartsWith("Eigenkonstruktion", t.Herkunft);

            // Der Entwurf trägt die Zeilen, aus denen er entstand — ein erneutes Öffnen beginnt mit ihnen.
            Assert.Equal(new[] { "Probebrause", "" }, t.Konstruktorzeilen.Select(z => z.Regel).ToArray());
            Assert.Equal(100.0, t.Konstruktorzeilen[1].VolumenL);
            Assert.Equal(2, t.Kopie().Konstruktorzeilen.Count);
        }

        [Fact]
        public void Mit_Entwurf_und_Werkstoff_rechnet_die_Auslegung_den_einen_Punkt_samt_Vergleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);

            ZapfprofilBedarfstagDaten tag = ZapfprofilHuelle.BedarfstagKonstruieren(new[]
            {
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 7.5, VolumenL = 120, ZapftemperaturC = 45 },
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 18.5, EndeH = 19.5, VolumenL = 200, ZapftemperaturC = 45 }
            }, "Probetag (fiktiv)").Tag;
            var a = new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = tag, Werkstoff = ZapfprofilWerkstoff.Stahl
            };

            ZapfprofilAuslegungDaten d = ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(), a, null, ZapfprofilStufe.Einfach);

            Assert.Equal(ZapfprofilAuslegungZustand.Gerechnet, d.Zustand);
            ZapfprofilAuslegungsgruppeDaten g = Assert.Single(d.Gruppen);
            Assert.Same(g, d.Punktgruppe);
            Assert.Equal("Probetag (fiktiv)", g.Bedarfstag);
            Assert.True(g.Empfehlung.Rechenbar, g.Empfehlung.Grund);
            Assert.True(g.Empfehlung.Speicher);
            Assert.True(g.Empfehlung.VolumenL > 0);
            Assert.True(g.Empfehlung.Schnellauslegung);                       // Stufe Einfach
            Assert.Contains(AuslegungTestbau.NenninhalteDerDatenbank(), n => n == g.Empfehlung.NenninhaltL);
            Assert.True(g.Hauptwert.Empfohlen);
            Assert.False(g.Perzentil.Empfohlen);
            Assert.False(g.Normvergleich.Empfohlen);
            Assert.Equal(ZapfprofilKartenstand.NichtRechenbar, g.Normvergleich.Stand);     // ohne Wohnungstabelle nie geschätzt
            Assert.NotNull(g.SummenlinieModell);
            Assert.True(g.LadezeitH > 0);
            Assert.NotEqual("", g.SpeicherCHerkunft);

            ZapfprofilVergleichDaten v = g.Vergleich;
            Assert.NotNull(v);
            Assert.Equal(new[] { "profilbasiert", "DIN 4708", "Faustwert mit Gleichzeitigkeit", "klassischer Faustwert" },
                         v.Verfahren.Select(x => x.Verfahren).ToArray());
            Assert.True(v.Verfahren[3].Nachrichtlich);
            Assert.False(v.Verfahren[3].ImBand);
            Assert.NotNull(v.WochenModell);
            Assert.Equal(0.8, v.Nutzanteil);                                  // Vorlage V4, freier Paketteil (N27)
            Assert.Equal(0.15, v.Zuschlag);
            Assert.True(v.ProfilbasiertVorhanden);                            // Ladefenster 22–6 Uhr, Zapfung am Tag
            Assert.StartsWith("Maßgebender Zeitpunkt der Stundenbilanz: ", v.Zeitpunkt);

            // Die Erzeugerart kommt aus dem Anlagenbestand des Projekts (oder bleibt benannt offen).
            Assert.NotEqual("", d.ErzeugerartHerkunft);

            // Ohne Werkstoff: kein Punkt, der Grund steht an der Empfehlung.
            ZapfprofilAuslegungDaten ohne = ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(),
                new ZapfprofilAuslegungEingabeDaten { Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = tag },
                null, ZapfprofilStufe.Einfach);
            ZapfprofilAuslegungsgruppeDaten go = Assert.Single(ohne.Gruppen);
            Assert.False(go.Empfehlung.Rechenbar);
            Assert.Contains("Werkstoff", go.Empfehlung.Grund);
        }

        /// <summary>
        /// Der übernommene Punkt ist Ergebnis, nicht Eingabe: Ein alter Punkt — im Arbeitsstand
        /// und im gespeicherten Stand —, der als Speichervolumen weit über der Schwelle der
        /// Großanlage läge, ändert weder Punkt noch Speichertemperatur noch Warnliste.
        /// </summary>
        [Fact]
        public void Die_Auslegung_rechnet_unabhaengig_vom_alten_Punkt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);

            ZapfprofilBedarfstagDaten tag = ZapfprofilHuelle.BedarfstagKonstruieren(new[]
            {
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 7.5, VolumenL = 120, ZapftemperaturC = 45 },
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 18.5, EndeH = 19.5, VolumenL = 200, ZapftemperaturC = 45 }
            }, "Probetag (fiktiv)").Tag;
            ZapfprofilAuslegungEingabeDaten Eingaben(double? punktL) => new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = tag.Kopie(), Werkstoff = ZapfprofilWerkstoff.Stahl,
                PunktVolumenL = punktL, PunktLeistungKw = punktL.HasValue ? 1.0 : null
            };

            double schwelle = ZapfprofilCtrl.Parameter().Wert(ZapfAuslegungParameter.W551_GROSS_VOLUMEN);
            double alterPunktL = schwelle * 1000.0;
            var basis = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0],
                ZapfprofilCtrl.ProjektVorgabe() with { AuslegungVolumenL = alterPunktL, AuslegungLeistungKw = 1.0 });

            ZapfprofilAuslegungDaten frei = ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(), Eingaben(null), null, ZapfprofilStufe.Einfach);
            ZapfprofilAuslegungDaten alt = ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(), Eingaben(alterPunktL), basis, ZapfprofilStufe.Einfach);

            ZapfprofilAuslegungsgruppeDaten g = Assert.Single(frei.Gruppen);
            Assert.True(g.Empfehlung.Rechenbar, g.Empfehlung.Grund);
            Assert.True(g.Empfehlung.VolumenL < schwelle, "Der Probetag darf selbst keine Großanlage sein.");
            Assert.Equal(Kurzbild(frei), Kurzbild(alt));
            Assert.DoesNotContain(Assert.Single(alt.Gruppen).Warnliste,
                                  w => w.Kennung == ZapfprofilHuelle.AuslegungsHinweisSchluessel(Grossanlage.HINWEIS_GROSSANLAGE));
        }

        /// <summary>Punkt, Nenninhalt, Speichertemperatur samt Herkunft und Warnliste je Gruppe als Text.</summary>
        private static string Kurzbild(ZapfprofilAuslegungDaten d)
            => string.Join(" || ", d.Gruppen.Select(g => string.Join(" | ",
                   g.Empfehlung.VolumenL?.ToString("R", CultureInfo.InvariantCulture),
                   g.Empfehlung.LeistungKw?.ToString("R", CultureInfo.InvariantCulture),
                   g.Empfehlung.NenninhaltL?.ToString("R", CultureInfo.InvariantCulture),
                   g.SpeicherC?.ToString("R", CultureInfo.InvariantCulture), g.SpeicherCHerkunft,
                   string.Join(", ", g.Warnliste.Select(w => w.Kennung + ": " + w.Text)))));

        [Fact]
        public void Der_Schreibweg_legt_im_gemeinsamen_Vorgang_den_Tag_an_und_haengt_Punkt_und_Projekt_daran()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            int vorher = ZapfprofilCtrl.Bedarfstage().Count;

            ZapfprofilBedarfstagDaten tag = ZapfprofilHuelle.BedarfstagKonstruieren(new[]
            {
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 7.5, VolumenL = 120, ZapftemperaturC = 45 }
            }, "Probetag (fiktiv)").Tag;
            ZapfprofilEingabeDaten e = Zonen();
            e.Auslegung = new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = tag, SpeicherC = 60.0,
                PunktVolumenL = 250.0, PunktLeistungKw = 9.5
            };

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            ZapfprofilEinstieg einstieg = ZapfprofilHuelle.Einstieg(PROJEKT, behaelter.Wege());
            einstieg.Uebernommen(new ZapfprofilErgebnisDaten(e));
            Assert.NotNull(behaelter.Arbeitsstand.BedarfstagEntwurf);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilSpeicherergebnis ok = behaelter.Schreiben(v);
                Assert.True(ok.Erfolg, ok.Meldung?.Text);
                v.Commit();
            }
            behaelter.Geschrieben();

            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(BrauchwasserWeg.Generator, gelesen.Weg);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, gelesen.Projekt.BedarfstagQuelle);
            Assert.NotNull(gelesen.Projekt.IdBedarfstag);
            Assert.Equal(60.0, gelesen.Projekt.SpeicherC);
            Assert.Equal(250.0, gelesen.Projekt.AuslegungVolumenL);
            Assert.Equal(9.5, gelesen.Projekt.AuslegungLeistungKw);
            Assert.Equal(vorher + 1, ZapfprofilCtrl.Bedarfstage().Count);
            BedarfstagKatalogzeile t = ZapfprofilCtrl.Bedarfstage().Single(x => x.Id == gelesen.Projekt.IdBedarfstag.Value);
            Assert.Equal(ZapfKatalogstatus.Eigen, t.Status);
            Assert.Equal(Herkunftsart.Eigenkonstruktion, t.Herkunft.Art);

            // Wieder geöffnet: die Eingaben stehen auf dem gespeicherten Tag, nicht mehr auf einem Entwurf.
            ZapfprofilAuslegungEingabeDaten wieder = ZapfprofilHuelle.AuslegungAusStand(gelesen);
            Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, wieder.Quelle);
            Assert.Equal(t.Id, wieder.IdBedarfstag);
            Assert.Null(wieder.Entwurf);
        }

        [Fact]
        public void Ein_belegter_Name_kommt_benannt_zurueck_und_der_Vorgang_rollt_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            BedarfstagKatalogzeile vorhanden = ZapfprofilCtrl.Bedarfstage().First(t => t.Katalogversion == VERSION);
            var zeilen = new[] { new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 7.5, VolumenL = 120, ZapftemperaturC = 45 } };

            // Der Konstruktor prüft den Namen schon lesend und nennt einen freien.
            ZapfprofilKonstruktorErgebnis belegt = ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, vorhanden.Bezeichner);
            Assert.Null(belegt.Tag);
            ZapfprofilMeldung m = Assert.Single(belegt.Meldungen);
            Assert.Equal("ZPG_AUS_KON_NAME_BELEGT", m.Kennung);
            string frei = ZapfprofilCtrl.FreierBedarfstagname(vorhanden.Bezeichner, VERSION);
            Assert.NotEqual(vorhanden.Bezeichner, frei);
            Assert.Equal("Der Name „" + vorhanden.Bezeichner + "“ ist in der Katalogversion „" + VERSION
                         + "“ schon vergeben — frei ist etwa „" + frei + "“.", m.Text);

            // Wird der Name erst nach dem Konstruktor belegt, lehnt der Schreibweg ab.
            ZapfprofilBedarfstagDaten tag = ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, frei).Tag;
            Assert.NotNull(tag);
            tag.Bezeichner = vorhanden.Bezeichner;
            ZapfprofilEingabeDaten e = Zonen();
            e.Auslegung = new ZapfprofilAuslegungEingabeDaten { Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = tag };
            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            ZapfprofilHuelle.Einstieg(PROJEKT, behaelter.Wege()).Uebernommen(new ZapfprofilErgebnisDaten(e));

            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilSpeicherergebnis nein = behaelter.Schreiben(v);
                Assert.False(nein.Erfolg);
                Assert.Equal("ZPG_SATZ_SPEICHER_ENTWURF_NAME_BELEGT", nein.Meldung.Kennung);
                Assert.StartsWith("Das Zapfprofil wurde nicht gespeichert — ", nein.Meldung.Text);
                v.Rollback();
            }
            Assert.Empty(ZapfprofilCtrl.Lies(PROJEKT).Zonen);
            Assert.True(behaelter.Geaendert);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>Eine erfundene Zapfregel des Konstruktors: 10 l/min · 4 min bei 40 °C.</summary>
        private static void RegelAnlegen()
        {
            TwwTestdatenbank.ParameterAnlegen("Konstruktor.Regel.Probebrause.Volumenstrom", 10.0, VERSION);
            TwwTestdatenbank.ParameterAnlegen("Konstruktor.Regel.Probebrause.Dauer", 4.0, VERSION);
            TwwTestdatenbank.ParameterAnlegen("Konstruktor.Regel.Probebrause.Temperatur", 40.0, VERSION);
        }

        private static int Nutzungsart(string name)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", name), new DbParam("@k", VERSION)));

        private static string Text(string schluessel, CultureInfo kultur)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, kultur);

        private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
