using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Seiten.Pufferspeicher;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Hülle der Pufferspeicher-Auslegung</b> (Konzept 6, Stufe P2) auf der Testdatenbank:
    /// Startstand für den Kombipuffer von 1045 (Vorbelegung mit Herkunft, Reihen als Kennzahlen,
    /// Vorlagen, erstes Ergebnis mit beiden Zonen, Warntexte aus den Ressourcen), abweichende
    /// Kriterienschalter, der neue Prozesspuffer von 1041, die Übernahme — Ändern auf einer Kopie
    /// von 1045, Neuanlegen auf einer Kopie von 1041 — mit Nachzug, benannte Ablehnungen (Puffer
    /// fremd, Projekt ohne Reihen) und die Übergabe aus dem Zapfprofil über die Naht des Fensters.
    /// Übernommen wird nie auf ein Referenzprojekt, nur auf Kopien.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PufferAuslegungHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            Pufferauslegungswege.Fenster = null;
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int P_ZAPF = 1045;
        private const int PUFFER_1045_KOMBI = 1054210;
        private const int P_PROZESS = 1041;

        private static object Wert(string sql, params object[] p) =>
            DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        private static double Zahl(string sql, params object[] p) => Convert.ToDouble(Wert(sql, p));

        private static string Projektname(int id) => Convert.ToString(Wert("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", id));

        private static int Kopie(int idProjekt, string zusatz)
        {
            string name = Projektname(idProjekt);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " " + zusatz);
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            return kopie;
        }

        private static int PufferDerKopie(int idKopie, int idQuellpuffer) =>
            Convert.ToInt32(Wert("SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = " +
                                 "(SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?)", idKopie, idQuellpuffer));

        private static PufferAuslegungHuelle Huelle(int idProjekt, int? idPuffer, Action nachzug = null)
            => new PufferAuslegungHuelle(new PufferAuslegungAuftrag { IdProjekt = idProjekt, IdPuffer = idPuffer, Nachzug = nachzug });

        // =============================================================================
        //  Startstand
        // =============================================================================

        [Fact]
        public void Der_Startstand_des_Kombipuffers_traegt_Vorbelegung_Herkunft_Reihen_und_Ergebnis()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungStartDaten d = Huelle(P_ZAPF, PUFFER_1045_KOMBI).Start();

            Assert.Equal("", d.Fehler);
            Assert.Equal(PUFFER_1045_KOMBI, d.IdPuffer);
            Assert.Equal(Convert.ToString(Wert("SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_KOMBI)), d.PufferBezeichner);
            Assert.Equal(Projektname(P_ZAPF), d.Projektname);
            Assert.True(d.Eingabe.KlasseHeizung);
            Assert.True(d.Eingabe.KlasseBrauchwasser);
            Assert.Equal("WP_BIVALENT", d.Eingabe.Vorlage);

            // Herkunft: Vorlage aus der Kaskade, Klassen aus dem Puffer, Marken übersetzt.
            Assert.Equal("Kaskade", d.HerkunftVon("Vorlage")!.Marke);
            Assert.Equal("Puffer", d.HerkunftVon("Klassen")!.Marke);
            Assert.Equal("Zapfprofil", d.HerkunftVon("Zapfprofil")!.Marke);
            Assert.True(d.ZapfVorhanden);
            Assert.NotEmpty(d.Erzeuger);
            Assert.True(d.IstWaermepumpe);

            // Reihen als Kennzahlen: drei Kanäle, Heizung mit Spitze und Jahressumme.
            Assert.Equal("", d.ReihenFehler);
            Assert.Equal(3, d.Reihen.Count);
            Assert.True(d.Reihen[0].SpitzeKw > 0 && d.Reihen[0].JahrKwh > d.Reihen[0].SpitzeKw);

            // Sieben Vorlagen mit je neun Kriterienschaltern, Texte aus den Ressourcen.
            Assert.Equal(7, d.Vorlagen.Count);
            Assert.All(d.Vorlagen, v => Assert.Equal(9, v.Kriterien.Count));
            PufferVorlageDaten wp = d.Vorlagen.Single(v => v.Schluessel == "WP_MONO");
            Assert.Equal("Wärmepumpe", wp.Titel);
            Assert.Equal("monovalent", wp.Untertitel);
            Assert.StartsWith("Faustwert ", wp.Faustwert);
            Assert.Equal(5, d.Uebergabearten.Count);

            // Das erste Ergebnis: beide Zonen, eine Empfehlung, Warntexte aus den Ressourcen.
            PufferAuslegungErgebnisDaten e = d.Ergebnis!;
            Assert.Equal(PufferErgebnisZustand.Gerechnet, e.Zustand);
            Assert.Contains(e.Zonen, z => z.Zone == "Heizung" && z.Name == "Heizzone");
            Assert.Contains(e.Zonen, z => z.Zone == "Brauchwasser" && z.VolumenL > 0);
            Assert.True(e.EmpfehlungL > 0);
            Assert.Contains(": ", e.Bemessend);
            foreach (PufferWarnungDaten w in e.Warnungen)
            {
                string schluessel = w.Code.Replace('-', '_');
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel), w.Text);
                Assert.NotEqual("", w.Klartext);
            }

            // Dieselbe Rechnung wie der Kern.
            PufferAuslegungErgebnis kern = PufferAuslegungCtrl.Rechnen(
                PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_KOMBI, PufferAuslegungCtrl.Reihen(P_ZAPF)).Eingang);
            Assert.Equal(kern.EmpfehlungL, e.EmpfehlungL);
            Assert.Equal(kern.SummeL, e.SummeL, 9);
        }

        [Fact]
        public void Arbeitsstand_und_Eingang_bilden_einander_ab_und_Kriterienschalter_ueberschreiben_die_Vorlage()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungHuelle h = Huelle(P_ZAPF, PUFFER_1045_KOMBI);
            PufferAuslegungStartDaten d = h.Start();

            // Hin und zurück ohne Verlust.
            PufferAuslegungEingang e = h.EingangAus(d.Eingabe);
            PufferAuslegungEingabeDaten zurueck = PufferAuslegungHuelle.EingabeAus(e);
            Assert.Equal(d.Eingabe.Vorlage, zurueck.Vorlage);
            Assert.Equal(d.Eingabe.Sperrprofil, zurueck.Sperrprofil);
            Assert.Equal(d.Eingabe.HeizgrenzeC, zurueck.HeizgrenzeC);
            Assert.Equal(d.Eingabe.DeckungszielProzent, zurueck.DeckungszielProzent);

            // D1 ist in der Vorlage WP_BIVALENT an; abgeschaltet bemisst es nicht mehr mit.
            PufferAuslegungErgebnisDaten ohne = h.Rechnen(d.Eingabe);
            Assert.True(ohne.Kriterium("D1")!.Aktiv);
            PufferAuslegungEingabeDaten mit = d.Eingabe.Kopie();
            mit.Kriterien["D1"] = false;
            Assert.NotEqual(true, h.Rechnen(mit).Kriterium("D1")?.Aktiv);
            // K9 (Festbrennstoff) ist in der Vorlage aus und fehlt; eingeschaltet steht es aktiv im Ergebnis.
            Assert.Null(ohne.Kriterium("K9"));
            mit.Kriterien["K9"] = true;
            Assert.True(h.Rechnen(mit).Kriterium("K9")!.Aktiv);

            // Vorlage, Sperrprofil und Ziele kommen im Eingang an.
            mit.Vorlage = "BHKW";
            mit.Sperrprofil = PufferSperrprofilWert.DreiMalZwei;
            mit.DeckungszielProzent = 90;
            PufferAuslegungEingang e2 = h.EingangAus(mit);
            Assert.Equal(PufferVorlage.BHKW, e2.Vorlage);
            Assert.Equal(3, e2.Sperrfenster.Count);
            Assert.Equal(0.9, e2.Deckungsziel!.Value, 12);
        }

        [Fact]
        public void Der_neue_Prozesspuffer_rechnet_die_Prozesszone()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungStartDaten d = Huelle(P_PROZESS, null).Start();
            Assert.Null(d.IdPuffer);
            Assert.True(d.Eingabe.KlasseProzess);
            Assert.Equal(PufferErgebnisZustand.Gerechnet, d.Ergebnis!.Zustand);
            Assert.Contains(d.Ergebnis.Zonen, z => z.Zone == "Prozess" && z.VolumenL > 0);
            Assert.True(d.Reihen[2].JahrKwh > 0);
        }

        [Fact]
        public void Ein_fremder_Puffer_und_ein_Projekt_ohne_Reihen_werden_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungStartDaten fremd = Huelle(P_PROZESS, PUFFER_1045_KOMBI).Start();
            Assert.Contains("gehört nicht zum Projekt", fremd.Fehler);

            PufferAuslegungHuelle ohne = Huelle(987654, null);
            PufferAuslegungStartDaten d = ohne.Start();
            Assert.True(d.Fehler.Length > 0 || d.ReihenFehler.Length > 0, "kein Grund genannt");
            Assert.Equal(PufferErgebnisZustand.Fehler, ohne.Rechnen(new PufferAuslegungEingabeDaten { KlasseHeizung = true }).Zustand);
            Assert.False(ohne.Uebernehmen(new PufferAuslegungEingabeDaten { KlasseHeizung = true },
                                          new PufferUebernahmeDaten(true, "x")).Erfolg);
        }

        // =============================================================================
        //  Übernahme — nur auf Kopien
        // =============================================================================

        [Fact]
        public void Die_Uebernahme_aendert_den_Kombipuffer_einer_Kopie_und_zieht_den_Nachzug()
        {
            if (!_db.Vorhanden) return;
            double referenz = Zahl("SELECT Gesamtvolumen FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_KOMBI);
            int kopie = Kopie(P_ZAPF, "P2 Aendern");
            int puffer = PufferDerKopie(kopie, PUFFER_1045_KOMBI);
            DataRow alt = DataRepository.GetDataTable("SELECT * FROM Tab_Pufferspeicher WHERE ID = ?", new DbParam("@id", puffer)).Rows[0];

            int nachzug = 0;
            PufferAuslegungHuelle h = Huelle(kopie, puffer, () => nachzug++);
            PufferAuslegungStartDaten d = h.Start();
            PufferUebernahmeErgebnis r = h.Uebernehmen(d.Eingabe, new PufferUebernahmeDaten(false, ""));

            Assert.True(r.Erfolg, r.Text);
            Assert.Equal(puffer, r.IdPuffer);
            Assert.Equal(1, nachzug);
            Assert.Contains("Übernommen", r.Text);
            DataRow neu = DataRepository.GetDataTable("SELECT * FROM Tab_Pufferspeicher WHERE ID = ?", new DbParam("@id", puffer)).Rows[0];
            Assert.Equal(d.Ergebnis!.EmpfehlungL, Convert.ToDouble(neu["Gesamtvolumen"]));
            foreach (string spalte in new[] { "Bezeichner", "Vorlauf", "Ruecklauf", "Schwelle_Ein", "Schwelle_Aus" })
                Assert.True(Equals(alt[spalte], neu[spalte]) || (spalte.StartsWith("Schwelle") && alt[spalte] == DBNull.Value),
                            spalte + ": " + alt[spalte] + " → " + neu[spalte]);

            // Die Auslegung ist am Puffer gespeichert; das Referenzprojekt bleibt, wie es war.
            Assert.Equal(1, Convert.ToInt32(Wert("SELECT COUNT(*) FROM " + PufferAuslegungSchema.TAB +
                                                 " WHERE ID_Projekt = ? AND ID_Pufferspeicher = ?", kopie, puffer)));
            Assert.Equal(referenz, Zahl("SELECT Gesamtvolumen FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_KOMBI));
            Assert.Null(h.Speichern(d.Eingabe));
        }

        [Fact]
        public void Die_Uebernahme_legt_auf_einer_Kopie_des_Prozessprojekts_einen_Puffer_an()
        {
            if (!_db.Vorhanden) return;
            int referenzAnzahl = Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", P_PROZESS));
            int kopie = Kopie(P_PROZESS, "P2 Neu");
            int anzahl = Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", kopie));

            PufferAuslegungHuelle h = Huelle(kopie, null);
            PufferAuslegungStartDaten d = h.Start();
            PufferUebernahmeErgebnis r = h.Uebernehmen(d.Eingabe, new PufferUebernahmeDaten(true, "Prozesspuffer P2"));

            Assert.True(r.Erfolg, r.Text);
            Assert.True(r.IdPuffer > 0);
            Assert.Equal(anzahl + 1, Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", kopie)));
            Assert.Equal("Prozesspuffer P2", Convert.ToString(Wert("SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?", r.IdPuffer)));
            Assert.Equal(d.Ergebnis!.EmpfehlungL, Zahl("SELECT Gesamtvolumen FROM Tab_Pufferspeicher WHERE ID = ?", r.IdPuffer));
            Assert.True(PufferSpCtrl.KlassenSetLesen(r.IdPuffer).Prozess);
            Assert.Equal(1, Convert.ToInt32(Wert("SELECT COUNT(*) FROM " + PufferAuslegungSchema.TAB +
                                                 " WHERE ID_Projekt = ? AND ID_Pufferspeicher = ?", kopie, r.IdPuffer)));

            // Ein zweites „Übernehmen" ändert den angelegten Puffer, statt einen weiteren anzulegen.
            PufferUebernahmeErgebnis r2 = h.Uebernehmen(d.Eingabe, new PufferUebernahmeDaten(false, ""));
            Assert.Equal(r.IdPuffer, r2.IdPuffer);
            Assert.Equal(anzahl + 1, Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", kopie)));
            Assert.Equal(referenzAnzahl, Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", P_PROZESS)));
        }

        // =============================================================================
        //  Einstieg und Übergabe
        // =============================================================================

        [Fact]
        public void Die_Ansicht_holt_den_angemeldeten_Arbeitsgang_genau_einmal_ab()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungHuelle.Anmelden(new PufferAuslegungAuftrag { IdProjekt = P_ZAPF, IdPuffer = PUFFER_1045_KOMBI, Einstieg = "Probe" });
            var erste = (PufferAuslegungStartDaten)PufferAuslegungHuelle.AnsichtGaben(P_ZAPF)["Daten"];
            Assert.Equal(PUFFER_1045_KOMBI, erste.IdPuffer);
            Assert.Equal("Probe", erste.Einstieg);

            var zweite = (PufferAuslegungStartDaten)PufferAuslegungHuelle.AnsichtGaben(P_ZAPF)["Daten"];
            Assert.Null(zweite.IdPuffer);
            Assert.Null(PufferAuslegungHuelle.AnsichtGaben(0));
        }

        [Fact]
        public void Die_Uebergabe_aus_dem_Zapfprofil_waehlt_den_Brauchwasserpuffer_und_oeffnet_im_Fenster()
        {
            if (!_db.Vorhanden) return;
            ZapfprofilStand stand = ZapfprofilCtrl.Lies(P_ZAPF);
            ZapfprofilEingabeDaten zonen = ZapfprofilHuelle.AlsEingabe(stand);

            // Ohne Fenster (iOS): benannt abgelehnt.
            Pufferauslegungswege.Fenster = null;
            string grund = ZapfprofilHuelle.Uebergeben(P_ZAPF, zonen, null, stand, ZapfprofilStufe.Einfach).GetAwaiter().GetResult();
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PAUS_UEBERGABE_NICHT_MOEGLICH, grund);

            // Mit Fenster: der Kombipuffer, Klasse Brauchwasser, das übergebene Ergebnis als Herkunft.
            IReadOnlyDictionary<string, object> gezeigt = null;
            Pufferauslegungswege.Fenster = g => { gezeigt = g; return Task.CompletedTask; };
            Assert.Null(ZapfprofilHuelle.Uebergeben(P_ZAPF, zonen, null, stand, ZapfprofilStufe.Einfach).GetAwaiter().GetResult());
            var d = (PufferAuslegungStartDaten)gezeigt["Daten"];
            Assert.Equal(PUFFER_1045_KOMBI, d.IdPuffer);
            Assert.True(d.Eingabe.KlasseBrauchwasser);
            Assert.Equal("übergeben", d.HerkunftVon("Zapfprofil")!.Marke);
            Assert.True(d.ZapfVorhanden);
            Assert.Contains(d.Ergebnis!.Zonen, z => z.Zone == "Brauchwasser");
            Assert.Contains("Zapfprofil", d.Einstieg);
        }
    }
}
