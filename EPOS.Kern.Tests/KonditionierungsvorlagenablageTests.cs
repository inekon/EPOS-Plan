using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorlagen ohne Datenbank</b> (Stufe KP2, Welle U2) — die Ablage
    /// <see cref="Konditionierungsvorlagenablage"/> folgt denselben Regeln wie
    /// <see cref="KonditionierungsvorlageCtrl"/>: die 14 ausgelieferten Vorlagen aus der Saattabelle,
    /// Kopf für Kopf und Inhalt für Inhalt gleich der gesäten Testdatenbank; dieselbe Reihenfolge der
    /// Auswahlliste (die ausgelieferten zuerst, gleiche Namen an derselben Stelle jeder Liste); dieselbe
    /// Namensregel, dasselbe Schloss, dieselbe „Kopie".
    /// </summary>
    /// <remarks>Eigene Arbeitskopie je Fall (ein Fall schreibt); die Kultur ist auf de-DE gepinnt.</remarks>
    [Collection("Testdatenbank")]
    public class KonditionierungsvorlagenablageTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private bool Bereit()
            => _db.Vorhanden && KonditionierungSchema.Lesbar() && KonditionierungVorlagenSchema.Lesbar();

        [Fact]
        public void Die_Ablage_aus_der_Saat_gleicht_der_gesaeten_Testdatenbank_Kopf_und_Inhalt()
        {
            if (!Bereit()) return;
            var ctrl = new KonditionierungsvorlageCtrl();
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            Assert.Equal(KonditionierungsvorlagenSaattabelle.VORLAGEN, ablage.Anzahl);

            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                List<KonditionierungsvorlageCtrl.Vorlage> db = ctrl.Liste(g), ohne = ablage.Liste(g);
                Assert.Equal(db.Select(v => (v.Bezeichner, v.Beschreibung, v.Nutzung, v.Ausgeliefert)),
                             ohne.Select(v => (v.Bezeichner, v.Beschreibung, v.Nutzung, v.Ausgeliefert)));
                for (int i = 0; i < db.Count; i++)
                {
                    Konditionierungsvorlage a = ctrl.Inhalt(db[i].Id, out string m1);
                    Konditionierungsvorlage b = ablage.Inhalt(ohne[i].Id, out string m2);
                    Assert.Null(m1);
                    Assert.Null(m2);
                    Assert.True(a.Inhalt.Gleich(b.Inhalt, mitBestand: false), g + "/" + db[i].Bezeichner + ": Inhalt verschieden");
                    Assert.Equal(a.Name, b.Name);
                    Assert.Equal(a.Groesse, b.Groesse);
                }
            }
        }

        [Fact]
        public void Gleiche_Namen_stehen_in_jeder_Liste_an_derselben_Stelle_die_eigenen_hinten()
        {
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            ablage.Hinzufuegen(Konditionierungsgroesse.Heizsoll, "aula", null, null, ausgeliefert: false, null);
            ablage.Hinzufuegen(Konditionierungsgroesse.Heizsoll, "Zeichensaal", null, null, ausgeliefert: false, null);
            ablage.Hinzufuegen(Konditionierungsgroesse.Heizsoll, "Büro alt", null, null, ausgeliefert: false, null);

            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                Assert.Equal("Büro", ablage.Liste(g)[0].Bezeichner);
            Assert.Equal(new[] { "Büro", "Schule", "Wohnen", "aula", "Büro alt", "Zeichensaal" },
                         ablage.Liste(Konditionierungsgroesse.Heizsoll).Select(v => v.Bezeichner));
        }

        [Fact]
        public void Die_Reihenfolge_ohne_Datenbank_ist_die_der_Datenbank_auch_mit_eigenen_Vorlagen()
        {
            if (!Bereit()) return;
            var ctrl = new KonditionierungsvorlageCtrl();
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            Konditionierungsstand inhalt = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(19));
            foreach (string name in new[] { "aula", "Zeichensaal", "Büro alt", "Ärztehaus" })
            {
                Assert.True(ctrl.SpeichernAus(inhalt, Konditionierungsgroesse.Heizsoll, name, null, null, out _).Ok);
                Assert.True(ablage.SpeichernAus(inhalt, Konditionierungsgroesse.Heizsoll, name, null, null, out _).Ok);
            }
            Assert.Equal(ctrl.Liste(Konditionierungsgroesse.Heizsoll).Select(v => v.Bezeichner),
                         ablage.Liste(Konditionierungsgroesse.Heizsoll).Select(v => v.Bezeichner));
        }

        [Fact]
        public void Namensregel_Schloss_und_Kopie_folgen_dem_Controller()
        {
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            KonditionierungsvorlageCtrl.Vorlage buero = ablage.Liste(Konditionierungsgroesse.Heizsoll)[0];
            Konditionierungsstand inhalt = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);

            // Doppelname ohne Unterschied der Schreibweise, am Namen benannt.
            string doppelt = ablage.NamePruefen(Konditionierungsgroesse.Heizsoll, "  BÜRO ", 0);
            Assert.NotNull(doppelt);
            Assert.Contains("„BÜRO“", doppelt);
            Assert.False(ablage.SpeichernAus(inhalt, Konditionierungsgroesse.Heizsoll, "büro", null, null, out _).Ok);
            Assert.Null(ablage.NamePruefen(Konditionierungsgroesse.Lueftung, "Wohnen", 0));   // eine andere Liste
            Assert.NotNull(ablage.NamePruefen(Konditionierungsgroesse.Heizsoll, "", 0));
            Assert.NotNull(ablage.NamePruefen(Konditionierungsgroesse.Heizsoll, new string('x', 81), 0));
            Assert.False(ablage.SpeichernAus(inhalt, Konditionierungsgroesse.Heizsoll, "Neu", null, "KANTINE", out _).Ok);

            // Das Schloss: eine ausgelieferte lässt sich nur duplizieren.
            Assert.False(ablage.Umbenennen(buero.Id, "Kontor").Ok);
            Assert.False(ablage.Loeschen(buero.Id).Ok);
            Assert.True(ablage.Duplizieren(buero.Id, "", out long kopie).Ok);
            Assert.Equal("Büro (Kopie)", ablage.Lesen(kopie).Bezeichner);
            Assert.False(ablage.Lesen(kopie).Ausgeliefert);
            Assert.True(ablage.Duplizieren(buero.Id, "", out long zweite).Ok);
            Assert.Equal("Büro (Kopie) (2)", ablage.Lesen(zweite).Bezeichner);
            Assert.True(ablage.Inhalt(kopie, out _).Inhalt.Gleich(ablage.Inhalt(buero.Id, out _).Inhalt, mitBestand: false));

            // Eine eigene lässt sich umbenennen (nicht auf einen belegten Namen) und löschen.
            Assert.False(ablage.Umbenennen(kopie, "Schule").Ok);
            Assert.True(ablage.Umbenennen(kopie, "Kontor").Ok);
            Assert.Equal("Kontor", ablage.Lesen(kopie).Bezeichner);
            Assert.True(ablage.Umbenennen(kopie, "KONTOR").Ok);   // der eigene Name zählt nicht
            Assert.True(ablage.Loeschen(kopie).Ok);
            Assert.Null(ablage.Lesen(kopie));
            Assert.Null(ablage.Inhalt(kopie, out string fehlt));
            Assert.NotNull(fehlt);
        }

        [Fact]
        public void Die_Kopie_heisst_in_Datenbank_und_Ablage_gleich()
        {
            if (!Bereit()) return;
            var ctrl = new KonditionierungsvorlageCtrl();
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            long db = ctrl.Liste(Konditionierungsgroesse.Personen)[0].Id;
            long ohne = ablage.Liste(Konditionierungsgroesse.Personen)[0].Id;
            for (int i = 0; i < 2; i++)
            {
                Assert.True(ctrl.Duplizieren(db, null, out long a).Ok);
                Assert.True(ablage.Duplizieren(ohne, null, out long b).Ok);
                Assert.Equal(ctrl.Lesen(a).Bezeichner, ablage.Lesen(b).Bezeichner);
            }
        }
    }
}
