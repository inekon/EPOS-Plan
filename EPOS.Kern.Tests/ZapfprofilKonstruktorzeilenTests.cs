using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zeilen des Bedarfstag-Konstruktors in der Datenbank</b> (Schemaschritt T5,
    /// Anwenderentscheid ZU25; Umsetzungskonzept Zapfprofilgenerator 4.5 Quelle (4), Nachtrag N21):
    /// Der Schreibweg legt sie ersetzend am Auslegungssatz ab, <c>Lies</c> gibt sie in ihrer
    /// Reihenfolge zurück, sie reisen mit Projektkopie und <c>.wpx</c>-Paket, und die Hülle führt
    /// sie an den Konstruktor — auch dann, wenn der Tag schon gespeichert ist und es keinen Entwurf
    /// mehr gibt. Eine Datenbank vor dem Schritt lehnt gegebene Zeilen benannt ab, statt sie still
    /// fallen zu lassen.
    ///
    /// <para>Alle Werte sind erfunden (Konzept Kapitel 6 (a)): runde Stunden, runde Liter, ein
    /// neutraler Verbraucher. Projekt 1006 ist keines der Referenzprojekte der CI.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilKonstruktorzeilenTests
    {
        /// <summary>Ein Projekt der Testdatenbank (Id 1006) — die Auslegung hängt daran.</summary>
        private const int PROJEKT = 1006;

        /// <summary>Derselbe Satz mit seinem Namen — Kopie und Paket sprechen Namen.</summary>
        private const string PROJEKTNAME = "Stromspeicher mit Wärmepumpe";

        /// <summary>Zwei erfundene Zeilen: eine mit freiem Volumen, eine über eine Zapfregel.</summary>
        private static IReadOnlyList<KonstruktorzeileStand> Zeilen()
            => new[]
            {
                new KonstruktorzeileStand(6.0, 7.5, "", null, 50.0, 40.0, "Kueche (erfunden)"),
                new KonstruktorzeileStand(18.0, 19.0, "Dusche (erfunden)", 3.0, null, null, "")
            };

        private static ZapfprofilStand Stand(IReadOnlyList<KonstruktorzeileStand> zeilen)
            => new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], ZapfprofilCtrl.ProjektVorgabe())
            {
                Konstruktorzeilen = zeilen
            };

        // =================================================================================
        //  Schreiben und Lesen
        // =================================================================================

        /// <summary>
        /// Gespeichert wird am Auslegungssatz, gelesen in der Reihenfolge der Tabelle — Feld für
        /// Feld dasselbe, samt leerer Regel und leerem Verbraucher. Der zurückgegebene Stand trägt
        /// die Zeilen ebenfalls (der Dialog arbeitet mit ihm weiter).
        /// </summary>
        [Fact]
        public void Die_Zeilen_werden_am_Auslegungssatz_gespeichert_und_gelesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_KONSTRUKTORZEILE));

            ZapfprofilStand geschrieben = ZapfprofilCtrl.Speichern(PROJEKT, Stand(Zeilen()));
            Assert.Equal(2, geschrieben.Konstruktorzeilen.Count);

            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(Zeilen(), gelesen.Konstruktorzeilen);

            // Sie hängen am Auslegungssatz, nicht am Projekt: dieselbe Id, aufsteigende Reihenfolge.
            int idAuslegung = gelesen.Projekt.Id;
            Assert.Equal(2L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_KONSTRUKTORZEILE + " WHERE ID_TwwProjekt = ?",
                new DbParam("@a", idAuslegung))));
            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT MIN(Reihenfolge) FROM " + TwwSchema.TAB_TWW_KONSTRUKTORZEILE + " WHERE ID_TwwProjekt = ?",
                new DbParam("@a", idAuslegung))));
        }

        /// <summary>
        /// <b>Ersetzend:</b> Ein zweites Speichern schreibt die Zeilen des neuen Stands und nichts
        /// sonst — weder verdoppelt es sie noch behält es eine weggenommene. Ein leerer Stand räumt
        /// ab; der Konstruktor beginnt danach mit einer Zeile wie beim ersten Mal.
        /// </summary>
        [Fact]
        public void Ein_zweites_Speichern_ersetzt_die_Zeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilCtrl.Speichern(PROJEKT, Stand(Zeilen()));
            ZapfprofilCtrl.Speichern(PROJEKT, Stand(Zeilen()));
            Assert.Equal(2, ZapfprofilCtrl.Lies(PROJEKT).Konstruktorzeilen.Count);

            var eine = new[] { new KonstruktorzeileStand(8.0, 8.25, "", null, 20.0, 45.0, "Waschtisch (erfunden)") };
            ZapfprofilCtrl.Speichern(PROJEKT, Stand(eine));
            Assert.Equal(eine, ZapfprofilCtrl.Lies(PROJEKT).Konstruktorzeilen);

            ZapfprofilCtrl.Speichern(PROJEKT, Stand(new KonstruktorzeileStand[0]));
            Assert.Empty(ZapfprofilCtrl.Lies(PROJEKT).Konstruktorzeilen);
        }

        /// <summary>
        /// <b>Vor dem Schemaschritt</b> fehlt die Tabelle: Ohne Zeilen läuft das Speichern durch wie
        /// zuvor, mit Zeilen wird es benannt abgelehnt (<see cref="ZapfSpeicherfehler.TabellenFehlen"/>)
        /// — nie still übergangen. Gelesen wird dann eine leere Liste.
        /// </summary>
        [Fact]
        public void Ohne_die_Tabelle_wird_eine_gegebene_Zeile_benannt_abgelehnt()
        {
            using var db = new TwwTestdatenbank();
            DataRepository.ExecuteNonQuery("DROP TABLE IF EXISTS \"" + TwwSchema.TAB_TWW_KONSTRUKTORZEILE + "\"");
            Assert.False(DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_KONSTRUKTORZEILE));

            // Ohne Zeilen: unverändert.
            ZapfprofilStand ohne = ZapfprofilCtrl.Speichern(1, Stand(new KonstruktorzeileStand[0]));
            Assert.NotNull(ohne.Projekt);
            Assert.Empty(ZapfprofilCtrl.Lies(1).Konstruktorzeilen);

            // Mit Zeilen: benannte Ablehnung, kein stiller Verlust.
            var ex = Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(1, Stand(Zeilen())));
            Assert.Equal(ZapfSpeicherfehler.TabellenFehlen, ex.Fehler);
            Assert.Equal("SPEICHER_TABELLE_FEHLT", ex.Grund.Kennung);
            // Der Satz nennt die Tabelle als Begriff der Oberflaechensprache, nicht als Tabellennamen.
            using var __ = new Kulturvorrichtung("de-DE");
            Assert.Contains("Zeilen des Bedarfstag-Konstruktors", ex.Grund.Klartext, StringComparison.Ordinal);
        }

        // =================================================================================
        //  Kopie und Paket
        // =================================================================================

        /// <summary>
        /// <b>Die Zeilen reisen mit dem Projekt</b> (Kopie und <c>.wpx</c>): Sie hängen am
        /// Auslegungssatz und stehen deshalb im Transferplan; nach Kopie und Rundreise stehen
        /// dieselben Zeilen am Ziel — und die Kopie ist unabhängig.
        /// </summary>
        [Fact]
        public void Die_Zeilen_reisen_mit_Kopie_und_Paket()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            ZapfprofilCtrl.Speichern(PROJEKT, Stand(Zeilen()));

            var dup = new ProjektDuplizierenCtrl();
            Assert.Contains(TwwSchema.TAB_TWW_KONSTRUKTORZEILE, dup.ErmittlePlan().Select(s => s.Tabelle));
            Assert.Equal(PROJEKT, dup.GetProjektId(PROJEKTNAME));
            int kopie = dup.Duplizieren(PROJEKTNAME, "Konstruktorzeilen Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(Zeilen(), ZapfprofilCtrl.Lies(kopie).Konstruktorzeilen);

            // Unabhaengig: Die Zeilen der Quelle wegzunehmen laesst die der Kopie stehen.
            ZapfprofilCtrl.Speichern(PROJEKT, Stand(new KonstruktorzeileStand[0]));
            Assert.Equal(Zeilen(), ZapfprofilCtrl.Lies(kopie).Konstruktorzeilen);

            var io = new ProjektExportImportCtrl();
            Assert.Contains(TwwSchema.TAB_TWW_KONSTRUKTORZEILE, io.Transferplan().Select(s => s.Tabelle));
            string paket = ordner.Datei("konstruktorzeilen.wpx");
            Assert.True(io.Exportieren("Konstruktorzeilen Kopie", paket));
            int neu = io.Importieren(paket, "Konstruktorzeilen Transfer", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string grund);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + grund);
            Assert.Equal(Zeilen(), ZapfprofilCtrl.Lies(neu).Konstruktorzeilen);
        }

        // =================================================================================
        //  Die Huelle: der Konstruktor oeffnet mit den gespeicherten Zeilen
        // =================================================================================

        /// <summary>
        /// <b>Die Hülle führt die gespeicherten Zeilen an den Konstruktor</b> (ZU25): Ein
        /// gespeicherter Konstruktortag hat keinen Entwurf mehr — die Startzeilen der Überlagerung
        /// kommen dann aus der Datenbank. Und wer die Auslegung speichert, ohne den Konstruktor zu
        /// öffnen, behält sie: Der Schreibweg der Hülle gibt dieselben Zeilen wieder her.
        /// </summary>
        [Fact]
        public void Die_Huelle_fuehrt_die_gespeicherten_Zeilen_an_den_Konstruktor()
        {
            // Die Projektvorgaben kommen aus der DDL der Datenbank: eine eigene leere Tww-Datenbank,
            // nie die des Rechners (auf dem CI-Laeufer gibt es keine; ProjektVorgabe() waere null).
            using var db = new TwwTestdatenbank();
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0],
                                            ZapfprofilCtrl.ProjektVorgabe() with
                                            {
                                                Id = 7,
                                                BedarfstagQuelle = ZapfBedarfstagquelle.Konstruktor,
                                                IdBedarfstag = 99
                                            })
            {
                Konstruktorzeilen = Zeilen()
            };

            ZapfprofilAuslegungEingabeDaten a = ZapfprofilHuelle.AuslegungAusStand(stand);
            Assert.Null(a.Entwurf);                                   // der Tag ist gespeichert
            Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, a.Quelle);
            Assert.Equal(2, a.Konstruktorzeilen.Count);
            Assert.Equal(6.0, a.Konstruktorzeilen[0].BeginnH);
            Assert.Equal("Kueche (erfunden)", a.Konstruktorzeilen[0].Verbraucher);
            Assert.Equal("Dusche (erfunden)", a.Konstruktorzeilen[1].Regel);
            Assert.Equal(3.0, a.Konstruktorzeilen[1].Anzahl);

            // Zurueck in den Stand: dieselben Zeilen, auch ohne Entwurf.
            Assert.Equal(Zeilen(), ZapfprofilHuelle.Konstruktorzeilen(a));

            // Eine andere Quelle wirft den konstruierten Tag weg - und mit ihm seine Zeilen.
            a.Quelle = ZapfprofilBedarfstagquelle.Stundenprofil;
            Assert.Empty(ZapfprofilHuelle.Konstruktorzeilen(a));
        }

        // =================================================================================
        //  Der Bezug des gespeicherten Tags (Folge (a) aus N21)
        // =================================================================================

        /// <summary>
        /// <b>Speichern → Laden → dieselbe Auslegung:</b> Ein Konstruktortag mit Bezug (Personen,
        /// 4) wird gespeichert; der geladene Stand trägt Bezugsart und Bezugsmenge seiner
        /// Katalogzeile, die Hülle gibt sie an den Konstruktor, und ein erneutes OK mit den
        /// geladenen Zeilen und dem geladenen Bezug baut einen Tag, dessen Auslegung Wert für Wert
        /// gleich der vor dem Schließen ist. Ohne den Bezug (der frühere Fehler) wiche sie ab. Der
        /// Bezug reist mit Kopie und Paket (er steht am Katalogkopf).
        /// </summary>
        [Fact]
        public void Der_Bezug_des_gespeicherten_Tags_kommt_beim_Laden_mit_und_die_Auslegung_bleibt_gleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();
            AuslegungTestbau.ParameterEinspielen("TEST-1");
            Assert.True(ZapfprofilCtrl.KalenderLesen(PROJEKT, out int jan1, out bool[] we));
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            var lauf = new Auslegungslauf(ZapfErzeugerart.Waermepumpe, ZapfUebertragerwerkstoff.Stahl);

            int nutzung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", "Testnutzung A (fiktiv)"), new DbParam("@k", "TEST-1")));
            var zone = new ZonenStand { Name = "Zone Probe", IdNutzungsart = nutzung, Bezugsmenge = 8.0 };
            var kernzeilen = new[] { new Konstruktorzeile(420, 450, 120.0, 45.0), new Konstruktorzeile(1110, 1170, 200.0, 45.0) };
            var zeilen = new[]
            {
                new KonstruktorzeileStand(7.0, 7.5, "", null, 120.0, 45.0, ""),
                new KonstruktorzeileStand(18.5, 19.5, "", null, 200.0, 45.0, "")
            };
            string name = ZapfprofilCtrl.FreierBedarfstagname("Bezugstag (fiktiv)", ps.Katalogversion);
            BedarfstagKatalogzeile entwurf = ZapfprofilCtrl.BedarfstagKonstruieren(kernzeilen, name, ps, 4.0, ZapfBezugsart.Personen);
            var vorher = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, null)
            {
                BedarfstagEntwurf = entwurf,
                Konstruktorzeilen = zeilen
            };
            Auslegungsempfehlung e0 = Empfehlung(ZapfprofilCtrl.Auslegung(PROJEKT, vorher, jan1, we, lauf));
            Assert.True(e0.Rechenbar, e0.Grund?.Klartext);

            // Speichern: Der zurückgegebene Stand trägt den Bezug schon (der Dialog arbeitet mit ihm weiter).
            ZapfprofilStand geschrieben = ZapfprofilCtrl.Speichern(PROJEKT, vorher);
            Assert.Equal(new KonstruktorBezugStand(true, 4.0, ZapfBezugsart.Personen), geschrieben.KonstruktorBezug);

            // Laden: Bezugsart und Bezugsmenge der Katalogzeile.
            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Null(gelesen.BedarfstagEntwurf);
            Assert.Equal(new KonstruktorBezugStand(true, 4.0, ZapfBezugsart.Personen), gelesen.KonstruktorBezug);
            Assert.Equal(e0, Empfehlung(ZapfprofilCtrl.Auslegung(PROJEKT, gelesen, jan1, we, lauf)), Gleich);

            // Die Hülle gibt den Bezug an den Konstruktor — ohne Hinweis.
            ZapfprofilAuslegungEingabeDaten a = ZapfprofilHuelle.AuslegungAusStand(gelesen);
            Assert.Null(a.Entwurf);
            Assert.Equal((int)ZapfBezugsart.Personen, a.KonstruktorBezugsart);
            Assert.Equal(4.0, a.KonstruktorBezugsmenge);
            Assert.Equal("", a.KonstruktorBezugHinweis);

            // Ein erneutes OK mit den geladenen Zeilen und dem geladenen Bezug: dieselbe Auslegung.
            BedarfstagKatalogzeile neu = ZapfprofilCtrl.BedarfstagKonstruieren(kernzeilen, name + " 2", ps,
                a.KonstruktorBezugsmenge, (ZapfBezugsart?)a.KonstruktorBezugsart);
            Assert.Equal(e0, Empfehlung(ZapfprofilCtrl.Auslegung(PROJEKT, gelesen with { BedarfstagEntwurf = neu }, jan1, we, lauf)), Gleich);
            // Gegenprobe: Ohne Bezug („ohne Bezug", der frühere Stand) rechnet die Auslegung anders.
            BedarfstagKatalogzeile ohne = ZapfprofilCtrl.BedarfstagKonstruieren(kernzeilen, name + " 3", ps);
            Assert.NotEqual(e0.VolumenL, Empfehlung(ZapfprofilCtrl.Auslegung(PROJEKT, gelesen with { BedarfstagEntwurf = ohne }, jan1, we, lauf)).VolumenL);

            // Kopie und Paket: Der Bezug steht am Katalogkopf und kommt am Ziel ebenso mit.
            var dup = new ProjektDuplizierenCtrl();
            int kopie = dup.Duplizieren(PROJEKTNAME, "Konstruktorbezug Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(gelesen.KonstruktorBezug, ZapfprofilCtrl.Lies(kopie).KonstruktorBezug);
            var io = new ProjektExportImportCtrl();
            string paket = ordner.Datei("konstruktorbezug.wpx");
            Assert.True(io.Exportieren("Konstruktorbezug Kopie", paket));
            int importiert = io.Importieren(paket, "Konstruktorbezug Transfer", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                            null, out string grund);
            Assert.True(importiert > 0, "Import fehlgeschlagen: " + grund);
            Assert.Equal(gelesen.KonstruktorBezug, ZapfprofilCtrl.Lies(importiert).KonstruktorBezug);
        }

        /// <summary>
        /// <b>Ein alter Datensatz ohne Bezug</b> (Katalogzeile ohne Bezugsart und Bezugsmenge, wie
        /// vor Schritt 124 oder bei einem Tag „ohne Bezug"): Der Konstruktor beginnt ohne Bezug,
        /// aber benannt — die Hülle setzt den Hinweis. Ebenso, wenn die Katalogzeile nicht mehr
        /// steht. Eine andere Quelle oder ein Entwurf setzen keinen Hinweis.
        /// </summary>
        [Fact]
        public void Ein_gespeicherter_Tag_ohne_Bezug_beginnt_ohne_Bezug_mit_benanntem_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen("TEST-1");
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            BedarfstagKatalogzeile entwurf = ZapfprofilCtrl.BedarfstagKonstruieren(
                new[] { new Konstruktorzeile(420, 450, 120.0, 45.0) },
                ZapfprofilCtrl.FreierBedarfstagname("Alttag (fiktiv)", ps.Katalogversion), ps, 4.0, ZapfBezugsart.Personen);
            ZapfprofilStand geschrieben = ZapfprofilCtrl.Speichern(PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null) { BedarfstagEntwurf = entwurf });
            // Der alte Datensatz: die Katalogzeile ohne Bezug.
            DataRepository.ExecuteNonQuery(
                "UPDATE " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " SET Bezugsmenge = NULL, " + TwwSchema.SPALTE_BEZUGSART + " = NULL WHERE ID = ?",
                new DbParam("@id", geschrieben.Projekt.IdBedarfstag.Value));

            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(new KonstruktorBezugStand(true, null, null), gelesen.KonstruktorBezug);
            ZapfprofilAuslegungEingabeDaten a = ZapfprofilHuelle.AuslegungAusStand(gelesen);
            Assert.Null(a.KonstruktorBezugsart);
            Assert.Null(a.KonstruktorBezugsmenge);
            Assert.NotEqual("", a.KonstruktorBezugHinweis);

            // Die Katalogzeile steht nicht mehr: ein anderer Hinweis, ebenfalls benannt.
            ZapfprofilAuslegungEingabeDaten fehlt = ZapfprofilHuelle.AuslegungAusStand(
                gelesen with { KonstruktorBezug = new KonstruktorBezugStand(false, null, null) });
            Assert.NotEqual("", fehlt.KonstruktorBezugHinweis);
            Assert.NotEqual(a.KonstruktorBezugHinweis, fehlt.KonstruktorBezugHinweis);

            // Eine andere Quelle: kein Bezug, kein Hinweis.
            ZapfprofilAuslegungEingabeDaten andere = a.Kopie();
            andere.Quelle = ZapfprofilBedarfstagquelle.Stundenprofil;
            ZapfprofilHuelle.KonstruktorBezugSetzen(andere, gelesen.KonstruktorBezug);
            Assert.Equal("", andere.KonstruktorBezugHinweis);
            Assert.Null(andere.KonstruktorBezugsart);
        }

        private static Auslegungsempfehlung Empfehlung(Auslegungsrechnung r)
            => Assert.Single(r.Ergebnis.Gruppen).Empfehlung;

        /// <summary>Wert für Wert gleich — Verfahren, Volumen, Leistung, Nenninhalt (exakt, ohne Toleranz; die Vermerke nennen den Tag beim Namen).</summary>
        private static readonly IEqualityComparer<Auslegungsempfehlung> Gleich = new EmpfehlungGleich();

        private sealed class EmpfehlungGleich : IEqualityComparer<Auslegungsempfehlung>
        {
            public bool Equals(Auslegungsempfehlung x, Auslegungsempfehlung y)
                => x.Verfahren == y.Verfahren && x.Rechenbar == y.Rechenbar && Nullable.Equals(x.VolumenL, y.VolumenL)
                   && Nullable.Equals(x.LeistungKw, y.LeistungKw) && Nullable.Equals(x.NenninhaltL, y.NenninhaltL);

            public int GetHashCode(Auslegungsempfehlung e) => 0;
        }

        /// <summary>Ein Wegwerfordner für das Paket — wie bei den Nachbarn des Transfers.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            public Arbeitsordner()
            {
                Pfad = Path.Combine(Path.GetTempPath(),
                                    "epos-konstruktorzeilen-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(Pfad);
            }

            public string Pfad { get; }

            public string Datei(string name) => Path.Combine(Pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(Pfad, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }
    }
}
