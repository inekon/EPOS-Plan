using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Schreibwege des Schemaschritts 124</b> (Umsetzungskonzept Zapfprofilgenerator N10 (i)/(j),
    /// N11 (d)/(i)/(j); Stufe Z4): Die Laufangaben der Auslegung — Erzeugerart, Werkstoff, Personen
    /// auto/manuell, Bezug des Füllstands — sind Projektgrößen in <c>Tab_TwwProjekt</c>, der
    /// Konstruktor legt seinen Tag samt Bezugsmenge und Bezugsart ab, die Wertemengen der DDL gelten
    /// auch im Schreibweg, und eine Datenbank vor 124 lehnt eine gesetzte Angabe benannt ab, statt sie
    /// still fallen zu lassen. Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilSchritt124Tests
    {
        private const int PROJEKT = 1006;

        [Fact]
        public void Die_Laufangaben_werden_gespeichert_und_gelesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(TwwSchema.T3Vollstaendig());

            ProjektStand p = ZapfprofilCtrl.ProjektVorgabe() with
            {
                Erzeugerart = ZapfErzeugerart.Waermepumpe,
                UebertragerWerkstoff = ZapfUebertragerwerkstoff.Edelstahl,
                PersonenAuto = false,
                PersonenManuell = 42.5,
                FuellstandBezug = ZapfFuellstandbezug.NenninhaltBand
            };
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], p));

            ProjektStand gelesen = ZapfprofilCtrl.Lies(PROJEKT).Projekt;
            Assert.Equal(ZapfErzeugerart.Waermepumpe, gelesen.Erzeugerart);
            Assert.Equal(ZapfUebertragerwerkstoff.Edelstahl, gelesen.UebertragerWerkstoff);
            Assert.False(gelesen.PersonenAuto);
            Assert.Equal(42.5, gelesen.PersonenManuell);
            Assert.Equal(ZapfFuellstandbezug.NenninhaltBand, gelesen.FuellstandBezug);
            Assert.Equal(2L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT Erzeugerart FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT))));

            // Zurück auf „keine Angabe": NULL bzw. Personen automatisch.
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0],
                gelesen with { Erzeugerart = null, UebertragerWerkstoff = null, PersonenAuto = true, PersonenManuell = null, FuellstandBezug = null }));
            ProjektStand leer = ZapfprofilCtrl.Lies(PROJEKT).Projekt;
            Assert.Null(leer.Erzeugerart);
            Assert.Null(leer.UebertragerWerkstoff);
            Assert.True(leer.PersonenAuto);
            Assert.Null(leer.FuellstandBezug);
        }

        [Fact]
        public void Werte_ausserhalb_der_Wertemenge_werden_benannt_abgelehnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ProjektStand p = ZapfprofilCtrl.ProjektVorgabe();
            var faelle = new (ProjektStand Stand, string Begriff)[]
            {
                (p with { Erzeugerart = (ZapfErzeugerart)3 }, "BEGRIFF_ERZEUGERART"),
                (p with { UebertragerWerkstoff = (ZapfUebertragerwerkstoff)0 }, "BEGRIFF_WERKSTOFF"),
                (p with { FuellstandBezug = (ZapfFuellstandbezug)5 }, "BEGRIFF_FUELLSTAND_BEZUG"),
                (p with { PersonenAuto = false, PersonenManuell = -1.0 }, "BEGRIFF_PERSONEN")
            };
            foreach (var (stand, begriff) in faelle)
            {
                var ex = Assert.Throws<ZapfprofilSpeicherException>(() =>
                    ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], stand)));
                Assert.Equal("SPEICHER_WERTEMENGE", ex.Grund.Kennung);
                Assert.Equal(begriff, Assert.IsType<ZapfSatz>(ex.Grund.Werte[0]).Kennung);
            }
        }

        [Fact]
        public void Vor_Schritt_124_laeuft_der_Schreibweg_ohne_Angabe_und_lehnt_eine_Angabe_benannt_ab()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            TwwTestdatenbank.SchemaAnlegen(mitT2: false);   // Stand vor 115 und vor 124
            Assert.False(TwwSchema.T3Vollstaendig());

            ZapfprofilCtrl.Speichern(1, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], ZapfprofilCtrl.ProjektVorgabe()));
            ProjektStand gelesen = ZapfprofilCtrl.Lies(1).Projekt;
            Assert.True(gelesen.PersonenAuto);
            Assert.Null(gelesen.Erzeugerart);

            var ex = Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(1,
                new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], gelesen with { Erzeugerart = ZapfErzeugerart.Kessel })));
            Assert.Equal("SPEICHER_SPALTE_FEHLT", ex.Grund.Kennung);
            Assert.Null(ZapfprofilCtrl.Lies(1).Projekt.Erzeugerart);
        }

        [Fact]
        public void Der_Konstruktor_legt_seinen_Tag_samt_Bezugsmenge_und_Bezugsart_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen("TEST-1");
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            var zeilen = new[] { new Konstruktorzeile(420, 450, 120.0, 45.0) };

            Assert.Throws<ZapfAuslegungException>(() => ZapfprofilCtrl.BedarfstagKonstruieren(zeilen, "Ohne Art", ps, 10.0, null));
            Assert.Throws<ZapfAuslegungException>(() => ZapfprofilCtrl.BedarfstagKonstruieren(zeilen, "Null", ps, 0.0, ZapfBezugsart.Personen));

            BedarfstagKatalogzeile projekttag = ZapfprofilCtrl.BedarfstagKonstruieren(zeilen, "Projekttag (fiktiv)", ps);
            Assert.Null(projekttag.Bezugsmenge);
            Assert.Null(projekttag.Bezugsart);

            BedarfstagKatalogzeile entwurf = ZapfprofilCtrl.BedarfstagKonstruieren(zeilen, "Personentag (fiktiv)", ps, 10.0,
                                                                                  ZapfBezugsart.Personen);
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null)
                                              { BedarfstagEntwurf = entwurf });
            BedarfstagKatalogzeile gespeichert = Assert.Single(ZapfprofilCtrl.Bedarfstage(), t => t.Bezeichner == "Personentag (fiktiv)");
            Assert.Equal(10.0, gespeichert.Bezugsmenge);
            Assert.Equal(ZapfBezugsart.Personen, gespeichert.Bezugsart);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, gespeichert.QuelleArt);
            Assert.Equal(gespeichert.Id, ZapfprofilCtrl.Lies(PROJEKT).Projekt.IdBedarfstag);

            // Die Ecodesign-Zapfprofile tragen ihre Bezugsart aus dem Paketteil, ohne Bezugsmenge.
            var eco = ZapfprofilCtrl.Bedarfstage().Where(t => t.QuelleArt == ZapfBedarfstagquelle.Ecodesign).ToList();
            Assert.All(eco, t =>
            {
                Assert.Equal(ZapfBezugsart.Wohneinheiten, t.Bezugsart);
                Assert.Null(t.Bezugsmenge);
            });
        }
    }
}
