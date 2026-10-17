using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Datenbankweg = reiner Kern, Zelle für Zelle</b> (Stufe KP2, Welle K2, Teilschritt 4; Entwurf
    /// KP2 Abschnitt 2): Jeder Schreibweg von KP1 ist eine dünne Hülle — Lesen → reiner Schritt der
    /// <see cref="Konditionierungsarbeit"/> → Schreiben. Die Probe liest die Ebene vorher, rechnet den
    /// reinen Schritt, fährt den Datenbankweg und liest nachher: Beide Stände gleichen sich in allen 30
    /// Zellen, jedem Kalender samt Perioden und Herkunft und in den Bestandsfeldern.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KonditionierungParitaetTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly KonditionierungCtrl _ctrl = new KonditionierungCtrl();

        /// <summary>Stellt Kultur und Arbeitskopie zurück.</summary>
        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar();

        private const Konditionierungsgroesse HEIZ = Konditionierungsgroesse.Heizsoll;

        private static KonditionierungCtrl.Eigner Gebaeude()
            => KonditionierungCtrl.Eigner.Gebaeude(Id("SELECT MIN(ID) FROM \"Tab_Gebaeude\""));

        private Konditionierungsstand Lesen(KonditionierungCtrl.Eigner e)
        {
            Konditionierungsstand s = _ctrl.StandLesen(e, out string m);
            Assert.Null(m);
            return s;
        }

        private void Gleich(Konditionierungsstand rein, KonditionierungCtrl.Eigner e)
        {
            Konditionierungsstand db = Lesen(e);
            Assert.True(rein.Gleich(db, mitBestand: true),
                        "rein " + rein + " / Datenbank " + db);
            Assert.Equal(Konditionierungsarbeit.Abdruck(new Konditionierungsarbeitsstand(rein, null)),
                         Konditionierungsarbeit.Abdruck(new Konditionierungsarbeitsstand(db, null)));
        }

        [Fact]
        public void Zelle_setzen_samt_Nachtzeit_und_Merker()
        {
            if (!Bereit()) return;
            KonditionierungCtrl.Eigner e = Gebaeude();
            foreach ((Konditionierungsgroesse g, string zeile, Matrixzelle zelle) in new[]
                     {
                         (HEIZ, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(17.0, 21, 5)),
                         (HEIZ, DbWerte.KOND_ZEILE_FERIEN, Matrixzelle.AusWert(15.0)),
                         (HEIZ, DbWerte.KOND_ZEILE_WOCHENENDE, Matrixzelle.Abgeschaltet()),
                         (Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(1.2, 22, 6, 3.0)),
                         (Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.4)),
                         (HEIZ, DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120)),
                     })
            {
                Konditionierungsstand vor = Lesen(e);
                Ebenenergebnis rein = Konditionierungsarbeit.Eintragen(vor, g, zeile, zelle);
                Assert.True(rein.Ok, rein.Meldung);
                KonditionierungCtrl.Ergebnis db = _ctrl.Vorgabe(e, g, zeile, zelle);
                Assert.True(db.Ok, db.Meldung);
                Gleich(rein.Stand, e);
            }
        }

        [Fact]
        public void Anlegen_Werkzeuge_Matrix_erneut_und_Verwerfen()
        {
            if (!Bereit()) return;
            KonditionierungCtrl.Eigner e = Gebaeude();
            Assert.True(_ctrl.Vorgabe(e, HEIZ, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(16.0, 22, 6)).Ok);

            Konditionierungsstand vor = Lesen(e);
            Vorgabematrix matrix = vor.Matrix();
            Ebenenergebnis rein = Konditionierungsarbeit.KalenderAnlegen(vor, matrix, HEIZ);
            Assert.True(_ctrl.Anlegen(e, matrix, HEIZ).Ok);
            Gleich(rein.Stand, e);

            var vorlagen = new KonditionierungsvorlageCtrl();
            vor = Lesen(e);
            rein = Konditionierungsarbeit.Werkzeug(vor, HEIZ, k => Kalenderwerkzeuge.Zeitfenster(k, new[] { 0, 1 }, 6, 8, 22.0));
            Assert.True(vorlagen.Zeitfenster(e, HEIZ, new[] { 0, 1 }, 6, 8, 22.0).Ok);
            Gleich(rein.Stand, e);

            vor = Lesen(e);
            rein = Konditionierungsarbeit.Werkzeug(vor, HEIZ, k => Kalenderwerkzeuge.Feiertagsregeln(k, 7));
            Assert.True(vorlagen.Feiertagsregeln(e, HEIZ, 7).Ok);
            Gleich(rein.Stand, e);

            vor = Lesen(e);
            matrix = vor.Matrix();
            rein = Konditionierungsarbeit.MatrixbereichErsetzen(vor, matrix, HEIZ);
            Assert.True(_ctrl.ErneutAnwenden(e, matrix, HEIZ).Ok);
            Gleich(rein.Stand, e);

            vor = Lesen(e);
            Konditionierungsstand verworfen = Konditionierungsarbeit.KalenderVerwerfen(vor, HEIZ);
            Assert.True(_ctrl.Verwerfen(e, HEIZ).Ok);
            Gleich(verworfen, e);
        }

        [Fact]
        public void Vorlage_uebernehmen()
        {
            if (!Bereit()) return;
            KonditionierungCtrl.Eigner quelle = Gebaeude();
            Assert.True(_ctrl.Vorgabe(quelle, HEIZ, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(16.0, 18, 7)).Ok);
            Assert.True(_ctrl.Vorgabe(quelle, HEIZ, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(21.0)).Ok);
            var vorlagen = new KonditionierungsvorlageCtrl();
            Assert.True(vorlagen.Speichern(quelle, HEIZ, "K2 Paritaet", null, null, out long id).Ok);

            KonditionierungCtrl.Eigner ziel = KonditionierungCtrl.Eigner.Katalogbau(
                Id("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0"));
            Konditionierungsstand vor = Lesen(ziel);
            Vorgabematrix matrix = vor.Matrix();
            var vorlage = new Konditionierungsvorlage(id, "K2 Paritaet", HEIZ, Lesen(KonditionierungCtrl.Eigner.Vorlage(id)));
            Ebenenergebnis rein = Konditionierungsarbeit.VorlageEintragen(vor, matrix, vorlage);
            Assert.True(rein.Ok, rein.Meldung);
            KonditionierungCtrl.Ergebnis db = vorlagen.Uebernehmen(id, ziel, matrix);
            Assert.True(db.Ok, db.Meldung);
            Gleich(rein.Stand, ziel);
            Assert.Equal("K2 Paritaet", Lesen(ziel).Herkunft(HEIZ).Vorlage);
        }

        private static long Id(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
