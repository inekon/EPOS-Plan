using System;
using System.Data;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Der Controller der Pufferspeicher-Auslegung</b> (Konzept 3.1, 3.5, 4, 5) auf der Testdatenbank:
    /// Vorbelegung aus Puffer, Kaskade, Erzeugern, Gebäuden und Einstellungen samt Herkunft, die drei
    /// Bedarfsreihen über den Weg des Laufs, die Brauchwasserzone aus dem Zapfprofil (1045), die
    /// Prozesszone (1041), Speichern und erneutes Vorbelegen, die Übernahme in den Projektpuffer — nur auf
    /// einer Projektkopie —, das Nutzungsprofil je Testprojekt, das Projektduplikat und der Projekttransfer
    /// mit der Auslegungszeile, Determinismus.
    ///
    /// <para>Die Fixture arbeitet auf einer Arbeitskopie der Testdatenbank; Referenzprojekte werden nur
    /// gelesen, geschrieben wird auf ihre Auslegungszeile (eine eigene Tabelle) und auf Kopien.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferAuslegungCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Wärmepumpe an Rang 1 mit Kessel, Zapfprofilgenerator (Zone Speicher).</summary>
        private const int P_ZAPF = 1045;
        /// <summary>Der Heizungspuffer von 1045 (Temperaturpaar und Schwellen gepflegt).</summary>
        private const int PUFFER_1045_HEIZUNG = 1054212;
        /// <summary>Der Kombipuffer von 1045 (Heizung und Brauchwasser).</summary>
        private const int PUFFER_1045_KOMBI = 1054210;
        /// <summary>BHKW an Rang 1, Heizungspuffer.</summary>
        private const int P_BHKW = 1030;
        private const int PUFFER_1030 = 1054170;
        /// <summary>Prozesswärme im Projekt, Wärmepumpe an Rang 1.</summary>
        private const int P_PROZESS = 1041;
        /// <summary>Das einzige Testprojekt mit gepflegter Übergabeart.</summary>
        private const int P_UEBERGABE = 1047;

        private static object Wert(string sql, params object[] p) =>
            DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        /// <summary>NULL der Datenbank — <c>ExecuteScalar</c> liefert dafür <c>null</c> oder <see cref="DBNull"/>.</summary>
        private static bool Leer(object v) => v == null || v == DBNull.Value;

        private static double Zahl(string sql, params object[] p) => Convert.ToDouble(Wert(sql, p));

        private static string Projektname(int id) => Convert.ToString(Wert("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", id));

        private static int PufferDerKopie(int idKopie, int idQuellpuffer) =>
            Convert.ToInt32(Wert("SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = " +
                                 "(SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?)", idKopie, idQuellpuffer));

        private static int Zeilen(int idProjekt) =>
            Convert.ToInt32(Wert("SELECT COUNT(*) FROM " + PufferAuslegungSchema.TAB + " WHERE ID_Projekt = ?", idProjekt));

        // =============================================================================
        //  Vorbelegen
        // =============================================================================

        [Fact]
        public void Vorbelegen_liest_Puffer_Kaskade_Erzeuger_und_Einstellungen_wie_die_Datenbank()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_HEIZUNG);
            PufferAuslegungEingang e = v.Eingang;

            // Wärmepumpe an Rang 1 (Tool_1), Kessel im Projekt → bivalent.
            Assert.Equal("Wärmepumpe", Convert.ToString(Wert("SELECT Tool_1 FROM Tab_Einstellungen WHERE ID_Projekt = ?", P_ZAPF)));
            Assert.Equal(PufferVorlage.WP_BIVALENT, e.Vorlage);
            Assert.True(e.Erzeuger.IstWaermepumpe);
            double nenn = Zahl("SELECT SUM(w.Nennleistung) FROM Tab_Energieanlagen a JOIN Tab_WP w ON w.ID = a.ID_WP " +
                               "WHERE a.ID_Projekt = ? AND a.ID_Type = 1", P_ZAPF);
            Assert.Equal(nenn, e.Erzeuger.NennleistungKw, 9);
            double kessel = Zahl("SELECT SUM(k.Ptherm) FROM Tab_Energieanlagen a JOIN Tab_Heizkessel k ON k.ID = a.ID_Kessel " +
                                 "WHERE a.ID_Projekt = ? AND a.ID_Type = 10", P_ZAPF);
            Assert.Equal(kessel, e.Erzeuger.ZweiterzeugerKw, 9);
            Assert.Equal(PufferHerkunftsquelle.KASKADE, v.Quelle(nameof(PufferAuslegungEingang.Vorlage)));

            // Puffer: Klassen-Set, Temperaturpaar und Schwellen (Datenbank in %, Eingang als Anteil).
            Assert.True(e.KlasseHeizung);
            Assert.False(e.KlasseBrauchwasser);
            Assert.False(e.KlasseProzess);
            Assert.Equal(Zahl("SELECT Vorlauf FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_HEIZUNG), e.VorlaufC);
            Assert.Equal(Zahl("SELECT Ruecklauf FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_HEIZUNG), e.RuecklaufC);
            Assert.Equal(Zahl("SELECT Schwelle_Ein FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_HEIZUNG) / 100.0, e.SchwelleEin.Value, 12);
            Assert.Equal(Zahl("SELECT Schwelle_Aus FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_HEIZUNG) / 100.0, e.SchwelleAus.Value, 12);
            Assert.Equal(PufferHerkunftsquelle.PUFFER, v.Quelle(nameof(PufferAuslegungEingang.VorlaufC)));

            // Übergabeart NULL → ideal (Vorgabe), Heizgrenze leer → 15 °C.
            Assert.True(Leer(Wert("SELECT MAX(Uebergabe_Art) FROM Tab_Gebaeude WHERE ID_Projekt = ?", P_ZAPF)));
            Assert.Null(e.Uebergabeart);
            Assert.Equal(PufferHerkunftsquelle.VORGABE, v.Quelle(nameof(PufferAuslegungEingang.Uebergabeart)));
            Assert.True(Leer(Wert("SELECT Kessel_Heizgrenze FROM Tab_Einstellungen WHERE ID_Projekt = ?", P_ZAPF)));
            Assert.Equal(SimulationSPK.HEIZGRENZE_VORGABE_C, e.HeizgrenzeC);

            // Parameter, Katalog und Herkunft.
            Assert.NotNull(e.Parameter);
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher_STAMM WHERE Speichertyp IN ('Pufferspeicher', 'Kombispeicher')"),
                         e.Katalog.Count);
            Assert.False(v.Gespeichert);
            Assert.All(v.Herkunft, x => Assert.False(string.IsNullOrEmpty(x.Quelle)));
        }

        [Fact]
        public void Vorbelegen_erkennt_BHKW_Uebergabeart_und_lehnt_fremden_Puffer_ab()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungEingang bhkw = PufferAuslegungCtrl.Vorbelegen(P_BHKW, PUFFER_1030).Eingang;
            Assert.Equal(PufferVorlage.BHKW, bhkw.Vorlage);
            Assert.False(bhkw.Erzeuger.IstWaermepumpe);
            Assert.Equal(Zahl("SELECT SUM(b.Ptherm) FROM Tab_Energieanlagen a JOIN Tab_BHKW b ON b.ID = a.ID_BHKW " +
                              "WHERE a.ID_Projekt = ? AND a.ID_Type = 11", P_BHKW), bhkw.Erzeuger.NennleistungKw, 9);
            Assert.Equal(85, bhkw.VorlaufC);
            Assert.Equal(60, bhkw.RuecklaufC);

            PufferAuslegungVorbelegung ue = PufferAuslegungCtrl.Vorbelegen(P_UEBERGABE, null);
            Assert.Equal(Convert.ToString(Wert("SELECT MAX(Uebergabe_Art) FROM Tab_Gebaeude WHERE ID_Projekt = ?", P_UEBERGABE)),
                         ue.Eingang.Uebergabeart);
            Assert.Equal(PufferHerkunftsquelle.GEBAEUDE, ue.Quelle(nameof(PufferAuslegungEingang.Uebergabeart)));
            Assert.True(ue.Eingang.KlasseHeizung);

            Assert.Throws<ArgumentException>(() => PufferAuslegungCtrl.Vorbelegen(P_BHKW, PUFFER_1045_HEIZUNG));
        }

        [Fact]
        public void Nutzungsprofil_je_Testprojekt()
        {
            if (!_db.Vorhanden) return;
            PufferNutzungsprofilAbleitung zapf = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, null).Nutzungsprofil;
            Assert.Equal(PufferNutzungsprofil.WOHNEN, zapf.Profil);
            Assert.False(zapf.Vorgabe);
            Assert.StartsWith("Zapf-Nutzungsart", zapf.Herkunft);

            PufferAuslegungVorbelegung prozess = PufferAuslegungCtrl.Vorbelegen(P_PROZESS, null);
            Assert.Equal(PufferNutzungsprofil.GEWERBE, prozess.Nutzungsprofil.Profil);
            Assert.Equal(PufferNutzungsprofil.GEWERBE, prozess.Eingang.Nutzungsprofil);

            PufferNutzungsprofilAbleitung vorgabe = PufferAuslegungCtrl.Vorbelegen(P_BHKW, PUFFER_1030).Nutzungsprofil;
            Assert.Equal(PufferNutzungsprofil.WOHNEN, vorgabe.Profil);
            Assert.True(vorgabe.Vorgabe);
        }

        [Fact]
        public void Vorlagen_liefern_sieben_Saetze_aus_der_Vorgabetabelle()
        {
            if (!_db.Vorhanden) return;
            var l = PufferAuslegungCtrl.Vorlagen();
            Assert.Equal(7, l.Count);
            Assert.Equal(Enum.GetValues(typeof(PufferVorlage)).Cast<PufferVorlage>(), l.Select(x => x.Vorlage));
            PufferVorlagenbeschreibung wp = l.Single(x => x.Vorlage == PufferVorlage.WP_MONO);
            Assert.True(wp.Kriterien["K4"]);
            Assert.False(wp.Kriterien["K9"]);
            Assert.Equal(Zahl("SELECT Wert FROM " + PufferAuslegungSchema.TAB_PARAMETER + " WHERE Schluessel = ?",
                              PufferAuslegungVorgaben.VorlageSchluessel("WP_MONO", "Startziel_je_Tag")), wp.StartzielJeTag);
            Assert.Equal("l/m²", l.Single(x => x.Vorlage == PufferVorlage.SOLAR).FaustwertEinheit);
        }

        // =============================================================================
        //  Reihen und Rechnen
        // =============================================================================

        [Fact]
        public void Reihen_liefern_drei_Jahresreihen_ohne_Fehlertext()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungReihen r = PufferAuslegungCtrl.Reihen(P_BHKW, PufferAuslegungCtrl.Klimaregion(P_BHKW));
            Assert.Null(r.Fehlertext);
            Assert.True(r.Vorhanden);
            Assert.Equal(8760, r.Heizung.Length);
            Assert.Equal(8760, r.Brauchwasser.Length);
            Assert.Equal(8760, r.Prozess.Length);
            Assert.True(r.Heizung.Sum() > 0);
            Assert.All(r.Heizung, w => Assert.True(double.IsFinite(w)));

            PufferAuslegungReihen ohne = PufferAuslegungCtrl.Reihen(P_BHKW, 0);
            Assert.False(ohne.Vorhanden);
            Assert.NotNull(ohne.Fehlertext);
        }

        [Fact]
        public void Brauchwasserzone_aus_dem_Zapfprofil_und_Determinismus()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungReihen r = PufferAuslegungCtrl.Reihen(P_ZAPF);
            Assert.True(r.Vorhanden, r.Fehlertext);
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_KOMBI, r);
            Assert.True(v.Eingang.KlasseHeizung);
            Assert.True(v.Eingang.KlasseBrauchwasser);
            Assert.NotNull(v.Eingang.Zapfprofil);
            Assert.Contains(v.Herkunft, h => h.Feld == nameof(PufferAuslegungEingang.Zapfprofil) && h.Quelle == PufferHerkunftsquelle.ZAPFPROFIL);

            PufferAuslegungErgebnis a = PufferAuslegungCtrl.Rechnen(v.Eingang);
            PufferZonenergebnis bw = a.Zone(PufferZone.Brauchwasser);
            Assert.NotNull(bw);
            Assert.True(bw.VolumenL > 0, "Brauchwasserzone " + bw.VolumenL);
            Assert.NotNull(a.Zone(PufferZone.Heizung));
            Assert.True(a.EmpfehlungL >= a.SummeL || a.AnPraxisgrenze);
            Assert.NotNull(a.Kennzahlen.ZonenanteilHeizung);

            // Determinismus: dieselbe Vorbelegung, dieselbe Rechnung.
            PufferAuslegungVorbelegung v2 = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_KOMBI, r);
            Assert.Equal(v.Eingang.Zapfprofil, v2.Eingang.Zapfprofil);
            Assert.Equal(v.Eingang.Erzeuger, v2.Eingang.Erzeuger);
            PufferAuslegungErgebnis b = PufferAuslegungCtrl.Rechnen(v2.Eingang);
            Assert.Equal(a.SummeL, b.SummeL);
            Assert.Equal(a.EmpfehlungL, b.EmpfehlungL);
            Assert.Equal(a.Bemessend, b.Bemessend);
            Assert.Equal(a.Warnungen.Select(w => w.Code), b.Warnungen.Select(w => w.Code));

            // Das Ergebnis landet in der Auslegungszeile.
            int id = PufferAuslegungCtrl.Speichern(P_ZAPF, PUFFER_1045_KOMBI, v.Eingang, a);
            Assert.True(id > 0);
            Assert.Equal(a.EmpfehlungL, Zahl("SELECT Volumen_Empfehlung_l FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id));
            Assert.Equal(bw.VolumenL, Zahl("SELECT Volumen_B_l FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id), 9);
            Assert.Equal(a.Bemessend, Convert.ToString(Wert("SELECT Bemessend FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$",
                           Convert.ToString(Wert("SELECT Berechnet_am FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));
        }

        [Fact]
        public void Prozesszone_fuer_das_Projekt_mit_Prozesswaerme()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungErgebnis a = PufferAuslegungCtrl.Durchrechnen(P_PROZESS, null, out PufferAuslegungVorbelegung v, out string fehler);
            Assert.True(a != null, fehler);
            Assert.True(v.Eingang.KlasseProzess);
            Assert.True(v.Eingang.ReiheProzess.Sum() > 0);
            PufferZonenergebnis pz = a.Zone(PufferZone.Prozess);
            Assert.NotNull(pz);
            Assert.True(pz.VolumenL > 0, "Prozesszone " + pz.VolumenL);
        }

        // =============================================================================
        //  Speichern
        // =============================================================================

        [Fact]
        public void Speichern_und_erneut_Vorbelegen_liefert_die_gespeicherten_Werte_in_einer_Zeile()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_HEIZUNG);
            PufferAuslegungEingang geaendert = v.Eingang with
            {
                Vorlage = PufferVorlage.WP_MONO,
                StartzielJeTag = 4,
                AnlagenvolumenL = 300,
                Sperrfenster = PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI"),
                KlasseProzess = true,
                Erzeuger = v.Eingang.Erzeuger with { ZweiterzeugerFrei = true }
            };
            int id = PufferAuslegungCtrl.Speichern(P_ZAPF, PUFFER_1045_HEIZUNG, geaendert, null);
            Assert.True(id > 0);

            PufferAuslegungVorbelegung w = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_HEIZUNG);
            Assert.True(w.Gespeichert);
            Assert.Equal(id, w.IdZeile);
            Assert.Equal(PufferVorlage.WP_MONO, w.Eingang.Vorlage);
            Assert.Equal(4, w.Eingang.StartzielJeTag);
            Assert.Equal(300, w.Eingang.AnlagenvolumenL);
            Assert.Equal(PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI"), w.Eingang.Sperrfenster);
            Assert.True(w.Eingang.KlasseProzess);
            Assert.True(w.Eingang.Erzeuger.ZweiterzeugerFrei);
            Assert.Equal(PufferHerkunftsquelle.GESPEICHERT, w.Quelle(nameof(PufferAuslegungEingang.Vorlage)));
            // Nicht geänderte Werte bleiben Vorbelegung und stehen als NULL.
            Assert.Equal(v.Eingang.Deckungsziel, w.Eingang.Deckungsziel);
            Assert.Equal(v.Eingang.Nutzungsprofil, w.Eingang.Nutzungsprofil);
            Assert.True(Leer(Wert("SELECT Deckungsziel FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));
            Assert.True(Leer(Wert("SELECT Nutzungsprofil FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));
            Assert.True(Leer(Wert("SELECT WP_Geregelt FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));
            Assert.Equal("WP_MONO", Wert("SELECT Vorlage FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id));

            // Zweimal speichern legt keine zweite Zeile an; zurück auf die Vorbelegung setzt die Spalte NULL.
            int zeilen = Zeilen(P_ZAPF);
            Assert.Equal(id, PufferAuslegungCtrl.Speichern(P_ZAPF, PUFFER_1045_HEIZUNG, geaendert with { StartzielJeTag = null }, null));
            Assert.Equal(zeilen, Zeilen(P_ZAPF));
            Assert.Equal(1, Convert.ToInt32(Wert("SELECT COUNT(*) FROM " + PufferAuslegungSchema.TAB +
                                                 " WHERE ID_Projekt = ? AND ID_Pufferspeicher = ?", P_ZAPF, PUFFER_1045_HEIZUNG)));
            Assert.True(Leer(Wert("SELECT Startziel_je_Tag FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));

            // „Neu anlegen“ (ohne Puffer) ist eine eigene Zeile.
            int neu = PufferAuslegungCtrl.Speichern(P_ZAPF, null, PufferAuslegungCtrl.Vorbelegen(P_ZAPF, null).Eingang, null);
            Assert.True(neu > 0 && neu != id);
            Assert.Equal(neu, PufferAuslegungCtrl.Speichern(P_ZAPF, null, PufferAuslegungCtrl.Vorbelegen(P_ZAPF, null).Eingang, null));
            Assert.Equal(zeilen + 1, Zeilen(P_ZAPF));
        }

        // =============================================================================
        //  Übernehmen (nur auf einer Projektkopie)
        // =============================================================================

        private static PufferAuslegungErgebnis Ergebnis(double empfehlungL, bool kombi) => new PufferAuslegungErgebnis
        {
            Zonen = kombi
                ? new[] { new PufferZonenergebnis { Zone = PufferZone.Heizung, VolumenL = 900 },
                          new PufferZonenergebnis { Zone = PufferZone.Brauchwasser, VolumenL = 600 } }
                : new[] { new PufferZonenergebnis { Zone = PufferZone.Heizung, VolumenL = empfehlungL - 100 } },
            SummeL = empfehlungL - 100,
            EmpfehlungL = empfehlungL,
            Kennzahlen = new PufferKennzahlen
            {
                Verlust = new PufferVerlust { VolumenL = empfehlungL, KwhJeTag = 2.5 },
                ZonenanteilHeizung = kombi ? 0.6 : (double?)null,
                SchichtenMindest = kombi ? 2 : 1
            }
        };

        [Fact]
        public void Uebernehmen_auf_einer_Projektkopie_aendert_Volumen_und_laesst_Schwellen_stehen()
        {
            if (!_db.Vorhanden) return;
            string name = Projektname(P_ZAPF);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Pufferauslegung");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            int puffer = PufferDerKopie(kopie, PUFFER_1045_HEIZUNG);
            Assert.True(puffer > 0 && puffer != PUFFER_1045_HEIZUNG);
            DataTable vorher = DataRepository.GetDataTable("SELECT * FROM Tab_Pufferspeicher WHERE ID = ?", new DbParam("@id", puffer));

            Assert.Equal(puffer, PufferAuslegungCtrl.Uebernehmen(kopie, puffer, Ergebnis(1500, false), null));
            DataRow r = DataRepository.GetDataTable("SELECT * FROM Tab_Pufferspeicher WHERE ID = ?", new DbParam("@id", puffer)).Rows[0];
            DataRow alt = vorher.Rows[0];
            Assert.Equal(1500, Convert.ToDouble(r["Gesamtvolumen"]));
            Assert.Equal(2.5, Convert.ToDouble(r["Bereitschaftsverluste"]), 9);
            foreach (string spalte in new[] { "Bezeichner", "Vorlauf", "Ruecklauf", "Schwelle_Ein", "Schwelle_Aus", "Schwelle_Aus_Nachrang",
                                              "Entladeprio", "Schwelle_Reserve", "Hersteller", "Speichertyp", "Investitionskosten" })
                Assert.True(Equals(alt[spalte], r[spalte]), spalte + ": " + alt[spalte] + " → " + r[spalte]);
            Assert.Equal(1, Convert.ToInt32(r["Nutzung_Heizung"]));
            Assert.Equal(0, Convert.ToInt32(r["Nutzung_Brauchwasser"]));

            // Das Referenzprojekt bleibt, wie es war.
            Assert.Equal(Convert.ToDouble(alt["Gesamtvolumen"]),
                         Zahl("SELECT Gesamtvolumen FROM Tab_Pufferspeicher WHERE ID = ?", PUFFER_1045_HEIZUNG));

            // Kombipuffer: Klassen-Set, mindestens zwei Schichten, Zonenanteil als Entnahmehöhe.
            Assert.Equal(puffer, PufferAuslegungCtrl.Uebernehmen(kopie, puffer, Ergebnis(1500, true), null));
            PufferSpCtrl.KlassenSet ks = PufferSpCtrl.KlassenSetLesen(puffer);
            Assert.True(ks.Heizung && ks.Brauchwasser && !ks.Prozess);
            PufferSpCtrl.Schichtdaten s = PufferSpCtrl.SchichtdatenLesen(puffer);
            Assert.True(s.Schichten >= 2);
            Assert.Equal(0.6, s.EntnahmeHeizung.Value, 12);
            Assert.Equal(1.0, s.EntnahmeBW.Value, 12);

            // Neu anlegen: ein weiterer Puffer, die Zeile „neu“ hängt danach an ihm.
            Assert.True(PufferAuslegungCtrl.Speichern(kopie, null, PufferAuslegungCtrl.Vorbelegen(kopie, null).Eingang, null) > 0);
            int anzahl = Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", kopie));
            int neu = PufferAuslegungCtrl.Uebernehmen(kopie, null, Ergebnis(800, false), "Puffer aus Auslegung");
            Assert.True(neu > 0);
            Assert.Equal(anzahl + 1, Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", kopie)));
            Assert.Equal(800, Zahl("SELECT Gesamtvolumen FROM Tab_Pufferspeicher WHERE ID = ?", neu));
            Assert.Equal(ProjektPuffer.SCHWELLE_EIN_DEFAULT, Zahl("SELECT Schwelle_Ein FROM Tab_Pufferspeicher WHERE ID = ?", neu));
            Assert.Equal(ProjektPuffer.SCHWELLE_AUS_DEFAULT, Zahl("SELECT Schwelle_Aus FROM Tab_Pufferspeicher WHERE ID = ?", neu));
            Assert.Equal(1, Convert.ToInt32(Wert("SELECT COUNT(*) FROM " + PufferAuslegungSchema.TAB +
                                                 " WHERE ID_Projekt = ? AND ID_Pufferspeicher = ?", kopie, neu)));
            Assert.Equal(0, Convert.ToInt32(Wert("SELECT COUNT(*) FROM " + PufferAuslegungSchema.TAB +
                                                 " WHERE ID_Projekt = ? AND ID_Pufferspeicher IS NULL", kopie)));

            // Ohne Empfehlung und mit fremdem Puffer: nichts übernommen.
            Assert.Equal(-1, PufferAuslegungCtrl.Uebernehmen(kopie, puffer, new PufferAuslegungErgebnis(), null));
            Assert.Equal(-1, PufferAuslegungCtrl.Uebernehmen(kopie, PUFFER_1045_HEIZUNG, Ergebnis(1500, false), null));
        }

        // =============================================================================
        //  Projektduplikat und Projekttransfer tragen die Auslegungszeile
        // =============================================================================

        [Fact]
        public void Duplikat_und_Transfer_tragen_die_Auslegungszeile_mit_umgesetztem_Puffer()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungEingang e = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_HEIZUNG).Eingang with { StartzielJeTag = 5 };
            Assert.True(PufferAuslegungCtrl.Speichern(P_ZAPF, PUFFER_1045_HEIZUNG, e, null) > 0);
            string name = Projektname(P_ZAPF);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Auslegungszeile");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            int puffer = PufferDerKopie(kopie, PUFFER_1045_HEIZUNG);
            Assert.Equal(5, PufferAuslegungCtrl.Vorbelegen(kopie, puffer).Eingang.StartzielJeTag);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-pufferauslegung-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(name, paket));
                int neu = io.Importieren(paket, "Transfer Pufferauslegung", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                int pufferNeu = PufferDerKopie(neu, PUFFER_1045_HEIZUNG);
                Assert.Equal(5, PufferAuslegungCtrl.Vorbelegen(neu, pufferNeu).Eingang.StartzielJeTag);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Hilfen ohne Datenbank
        // =============================================================================

        [Fact]
        public void DmaxTag_ist_das_groesste_Tagesdefizit_gegen_das_Tagesmittel()
        {
            var r = new double[48];
            r[7] = 12;   // Tag 1: 12 kWh in einer Stunde → Defizit 12 − 0,5 = 11,5
            r[30] = 6;   // Tag 2: 6 kWh → 5,75
            (double dmax, double tag) = PufferAuslegungCtrl.DmaxTag(r);
            Assert.Equal(11.5, dmax, 12);
            Assert.Equal(12, tag, 12);
            Assert.Equal((0.0, 0.0), PufferAuslegungCtrl.DmaxTag(new double[24]));
        }

        // =============================================================================
        //  Vorbelegung aus den Teillastfeldern (Welle M4, Stufe P4a)
        // =============================================================================

        private static int Ausfuehren(string sql, params object[] p) =>
            DataRepository.ExecuteNonQuery(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        /// <summary>Die Wärmepumpe (Projektgerät <c>Tab_WP</c>) an der Anlage einer Projektkopie.</summary>
        private static int WpDerKopie(int idKopie) =>
            Convert.ToInt32(Wert("SELECT ID_WP FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = 1", idKopie));

        private static PufferAuslegungHerkunft Teillastzeile(PufferAuslegungVorbelegung v, string quelle) =>
            v.Herkunft.LastOrDefault(h => h.Quelle == quelle && h.Baustein?.Schluessel != null &&
                                          h.Baustein.Schluessel.Contains("_WP_MINDEST", StringComparison.Ordinal));

        [Fact]
        public void Teillastfeld_der_Waermepumpe_belegt_Mindestleistung_und_Regelung_vor()
        {
            if (!_db.Vorhanden) return;
            string name = Projektname(P_ZAPF);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Teillast WP");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            int wp = WpDerKopie(kopie);
            Assert.True(wp > 0);
            Assert.Equal(0, Convert.ToInt32(Wert("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_WP = ? AND ID_Projekt <> ?", wp, kopie)));
            double nenn = Zahl("SELECT Nennleistung FROM Tab_WP WHERE ID = ?", wp);

            // Ein/Aus-Gerät ohne Teillastfeld: wie bisher — keine Mindestleistung, nicht geregelt, keine Teillastzeile.
            Ausfuehren("UPDATE Tab_WP SET Regelung = 'Ein/Aus', Mindestleistung_kW = NULL WHERE ID = ?", wp);
            PufferAuslegungVorbelegung leer = PufferAuslegungCtrl.Vorbelegen(kopie, null);
            Assert.Null(leer.Eingang.Erzeuger.MindestleistungKw);
            Assert.False(leer.Eingang.Erzeuger.Geregelt);
            Assert.DoesNotContain(leer.Herkunft, h => h.Quelle == PufferHerkunftsquelle.TEILLAST);

            // Teillastfeld der Anlage (Projektgerät): Wert übernommen, unter der Nennleistung → geregelt, Herkunft benannt.
            double pmin = Math.Round(nenn * 0.3, 1);
            Ausfuehren("UPDATE Tab_WP SET Mindestleistung_kW = ? WHERE ID = ?", pmin, wp);
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(kopie, null);
            Assert.Equal(pmin, v.Eingang.Erzeuger.MindestleistungKw.Value, 9);
            Assert.True(v.Eingang.Erzeuger.Geregelt);
            PufferAuslegungHerkunft h = Teillastzeile(v, PufferHerkunftsquelle.TEILLAST);
            Assert.NotNull(h);
            Assert.Equal(nameof(PufferAuslegungEingang.Erzeuger), h.Feld);
            Assert.Equal("PAUS_HERK_TEILLAST_WP_MINDEST", h.Baustein.Schluessel);
            Assert.Contains("Mindestleistung_kW", h.Text);
            Assert.Equal(PufferHerkunftsquelle.TEILLAST, v.Quelle(nameof(PufferAuslegungEingang.Erzeuger)));

            // Mindestleistung = Nennleistung: übernommen, aber kein Modulationsbereich → nicht geregelt.
            Ausfuehren("UPDATE Tab_WP SET Mindestleistung_kW = ? WHERE ID = ?", nenn, wp);
            PufferAuslegungVorbelegung voll = PufferAuslegungCtrl.Vorbelegen(kopie, null);
            Assert.Equal(nenn, voll.Eingang.Erzeuger.MindestleistungKw.Value, 9);
            Assert.False(voll.Eingang.Erzeuger.Geregelt);

            // Das Referenzprojekt bleibt unberührt.
            Assert.True(Leer(Wert("SELECT w.Mindestleistung_kW FROM Tab_Energieanlagen a JOIN Tab_WP w ON w.ID = a.ID_WP " +
                                  "WHERE a.ID_Projekt = ? AND a.ID_Type = 1", P_ZAPF)));
            Assert.Null(PufferAuslegungCtrl.Vorbelegen(P_ZAPF, null).Eingang.Erzeuger.MindestleistungKw);
        }

        [Fact]
        public void Katalogsatz_der_Waermepumpe_ist_der_Rueckfall_des_Teillastfelds()
        {
            if (!_db.Vorhanden) return;
            string name = Projektname(P_ZAPF);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Teillast Katalog");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            int wp = WpDerKopie(kopie);
            double nenn = Zahl("SELECT Nennleistung FROM Tab_WP WHERE ID = ?", wp);

            // Ein Katalogsatz, auf den kein Projektgerät verweist — nur die Kopie wird auf ihn umgehängt.
            int stamm = Convert.ToInt32(Wert("SELECT MIN(s.ID) FROM Tab_WP_STAMM s WHERE NOT EXISTS " +
                                             "(SELECT 1 FROM Tab_WP w WHERE w.ID_Stamm = s.ID)"));
            Assert.True(stamm > 0);
            double pmin = Math.Round(nenn * 0.25, 1);
            Ausfuehren("UPDATE Tab_WP_STAMM SET Mindestleistung_kW = ? WHERE ID = ?", pmin, stamm);
            Ausfuehren("UPDATE Tab_WP SET ID_Stamm = ?, Mindestleistung_kW = NULL, Regelung = 'Ein/Aus' WHERE ID = ?", stamm, wp);

            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(kopie, null);
            Assert.Equal(pmin, v.Eingang.Erzeuger.MindestleistungKw.Value, 9);
            Assert.True(v.Eingang.Erzeuger.Geregelt);
            PufferAuslegungHerkunft h = Teillastzeile(v, PufferHerkunftsquelle.KATALOG);
            Assert.NotNull(h);
            Assert.Equal("PAUS_HERK_KATALOG_WP_MINDEST", h.Baustein.Schluessel);
            Assert.Contains("Tab_WP_STAMM", h.Text);

            // Das Projektgerät schlägt den Katalog.
            Ausfuehren("UPDATE Tab_WP SET Mindestleistung_kW = ? WHERE ID = ?", pmin * 2, wp);
            PufferAuslegungVorbelegung g = PufferAuslegungCtrl.Vorbelegen(kopie, null);
            Assert.Equal(pmin * 2, g.Eingang.Erzeuger.MindestleistungKw.Value, 9);
            Assert.NotNull(Teillastzeile(g, PufferHerkunftsquelle.TEILLAST));
            Assert.Null(Teillastzeile(g, PufferHerkunftsquelle.KATALOG));
        }

        [Fact]
        public void Mindestlaufzeit_des_BHKW_aus_dem_Teillastfeld()
        {
            if (!_db.Vorhanden) return;
            string name = Projektname(P_BHKW);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Teillast BHKW");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            int puffer = PufferDerKopie(kopie, PUFFER_1030);

            // Leer: wie bisher — die Mindestlaufzeit bleibt offen, K3 rechnet mit der Vorlage BHKW.
            PufferAuslegungVorbelegung leer = PufferAuslegungCtrl.Vorbelegen(kopie, puffer);
            Assert.Equal(PufferVorlage.BHKW, leer.Eingang.Vorlage);
            Assert.Null(leer.Eingang.MindestlaufzeitMin);
            Assert.Null(leer.Quelle(nameof(PufferAuslegungEingang.MindestlaufzeitMin)));
            Assert.Null(leer.Eingang.Erzeuger.MindestleistungKw);

            Assert.True(Ausfuehren("UPDATE Tab_BHKW SET Mindestlaufzeit_min = 20 WHERE ID IN " +
                                   "(SELECT ID_BHKW FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = 11)", kopie) > 0);
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(kopie, puffer);
            Assert.Equal(20, v.Eingang.MindestlaufzeitMin);
            Assert.Equal(PufferHerkunftsquelle.TEILLAST, v.Quelle(nameof(PufferAuslegungEingang.MindestlaufzeitMin)));
            PufferAuslegungHerkunft h = v.Herkunft.Last(x => x.Feld == nameof(PufferAuslegungEingang.MindestlaufzeitMin));
            Assert.Equal("PAUS_HERK_TEILLAST_BHKW_LAUFZEIT", h.Baustein.Schluessel);
            Assert.Contains("Tab_BHKW.Mindestlaufzeit_min = 20 min", h.Text);
            Assert.Null(v.Eingang.Erzeuger.MindestleistungKw);       // ein BHKW-Teillastfeld der Mindestleistung gibt es nicht

            // K3 rechnet mit der Mindestlaufzeit des Teillastfelds.
            PufferKriterium k3 = PufferAuslegungCtrl.Rechnen(v.Eingang with { ReiheHeizung = PufferAuslegungCtrl.Reihen(kopie).Heizung })
                                                    .Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K3);
            Assert.NotNull(k3);
            Assert.Contains(" · 20 min / ", k3.Rechenweg);

            // Das Referenzprojekt bleibt unberührt.
            Assert.Null(PufferAuslegungCtrl.Vorbelegen(P_BHKW, PUFFER_1030).Eingang.MindestlaufzeitMin);
        }

        // =============================================================================
        //  Sitzungseingaben (Welle P4c)
        // =============================================================================

        /// <summary>
        /// Kriterienschalter, Expertenweg, Heizlast, Wohneinheiten und Anzeigestufe: speichern, neu
        /// vorbelegen, gleich; die Vorgabe steht als NULL. Der Bericht rechnet mit den gespeicherten Schaltern.
        /// </summary>
        [Fact]
        public void Sitzungseingaben_speichern_vorbelegen_gleich()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_HEIZUNG);
            PufferAuslegungEingang e = v.Eingang;
            string typ = e.Vorlage.ToString();
            PufferAuslegungParameter p = e.Parameter ?? PufferAuslegungParameter.Vorgabe();
            // Zwei Schalter umgedreht: K2 und KV.
            var kriterien = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["K2"] = !p.VorlageAn(typ, "K2"),
                ["KV"] = !p.VorlageAn(typ, "KV")
            };
            PufferAuslegungEingang geaendert = e with
            {
                Parameter = PufferAuslegungCtrl.ParameterMitKriterien(p, e.Vorlage, kriterien),
                SperrzeitExpertenweg = !e.SperrzeitExpertenweg,
                AuslegungsheizlastKw = 17.5,
                Wohneinheiten = 6
            };
            int id = PufferAuslegungCtrl.Speichern(P_ZAPF, PUFFER_1045_HEIZUNG, geaendert, null, "EXPERTE");
            Assert.True(id > 0);
            Assert.Equal((long)PufferAuslegungCtrl.KriterienMaske(geaendert.Parameter, e.Vorlage),
                         Convert.ToInt64(Wert("SELECT Kriterien_Aktiv FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));
            Assert.Equal(6L, Convert.ToInt64(Wert("SELECT Wohneinheiten FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)));
            Assert.Equal("EXPERTE", Wert("SELECT Anzeigestufe FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id));

            PufferAuslegungVorbelegung w = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_HEIZUNG);
            Assert.Equal(kriterien.OrderBy(k => k.Key), w.Kriterien.OrderBy(k => k.Key));
            foreach (string k in PufferAuslegungVorgaben.VORLAGE_SCHALTER)
                Assert.Equal(geaendert.Parameter.VorlageAn(typ, k), w.Eingang.Parameter.VorlageAn(typ, k));
            Assert.Equal(p.VorlageAn(typ, "K2"), w.KriterienBasis.VorlageAn(typ, "K2"));
            Assert.Equal(geaendert.SperrzeitExpertenweg, w.Eingang.SperrzeitExpertenweg);
            Assert.Equal(17.5, w.Eingang.AuslegungsheizlastKw);
            Assert.Equal(6.0, w.Eingang.Wohneinheiten);
            Assert.Equal("EXPERTE", w.Anzeigestufe);
            Assert.Equal(PufferHerkunftsquelle.GESPEICHERT, w.Quelle("Kriterien"));

            // Der Bericht rechnet mit den gespeicherten Schaltern nach.
            PufferAuslegungGespeichert g = PufferAuslegungCtrl.Gespeichert(P_ZAPF).Single(x => x.IdZeile == id);
            PufferAuslegungErgebnis r = PufferAuslegungCtrl.Rechnen(w.Eingang);
            Assert.Equal(r.EmpfehlungL, g.NachgerechnetL);

            // Zurück auf die Vorgabe: jede Spalte NULL, keine Abweichung mehr.
            Assert.Equal(id, PufferAuslegungCtrl.Speichern(P_ZAPF, PUFFER_1045_HEIZUNG, e, null));
            foreach (string sp in new[] { "Kriterien_Aktiv", "Sperrzeit_Expertenweg", "Auslegungsheizlast_kW", "Wohneinheiten", "Anzeigestufe" })
                Assert.True(Leer(Wert("SELECT " + sp + " FROM " + PufferAuslegungSchema.TAB + " WHERE ID = ?", id)), sp);
            Assert.Empty(PufferAuslegungCtrl.Vorbelegen(P_ZAPF, PUFFER_1045_HEIZUNG).Kriterien);
        }

        [Fact]
        public void Kriterienmaske_hin_und_zurueck()
        {
            for (int m = 0; m <= PufferAuslegungErgaenzungSchema.MASKE_MAX; m += 37)
            {
                var p = PufferAuslegungCtrl.ParameterMitKriterien(PufferAuslegungParameter.Vorgabe(), PufferVorlage.BHKW,
                                                                  PufferAuslegungCtrl.KriterienAusMaske(m));
                Assert.Equal(m, PufferAuslegungCtrl.KriterienMaske(p, PufferVorlage.BHKW));
            }
        }

        // =============================================================================
        //  Konditionierungsvorlage am Gebäude, Katalogverweis am Puffer (Welle P4c)
        // =============================================================================

        /// <summary>
        /// Übernimmt die Auslegung den Katalogsatz, merkt der Puffer ihn (<c>ID_Stamm</c>) und nennt ihn als
        /// Herkunft; ohne den Schalter wird der Verweis leer. Nur auf einer Projektkopie von 1045.
        /// </summary>
        [Fact]
        public void Uebernehmen_merkt_den_Katalogsatz_am_Puffer()
        {
            if (!_db.Vorhanden) return;
            string name = Projektname(P_ZAPF);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Katalogverweis");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            int puffer = PufferDerKopie(kopie, PUFFER_1045_HEIZUNG);
            PufferKatalogsatz satz = PufferAuslegungCtrl.Katalog().First();
            PufferAuslegungErgebnis r = Ergebnis(1500, false) with { Katalogvorschlag = satz };

            Assert.Equal(puffer, PufferAuslegungCtrl.Uebernehmen(kopie, puffer, r, null));
            Assert.Equal((long)satz.Id, Convert.ToInt64(Wert("SELECT ID_Stamm FROM Tab_Pufferspeicher WHERE ID = ?", puffer)));
            Assert.Equal(satz.Bezeichner, PufferSpCtrl.Katalogherkunft(puffer));

            Assert.Equal(puffer, PufferAuslegungCtrl.Uebernehmen(kopie, puffer, r, null, katalogsatz: false));
            Assert.True(Leer(Wert("SELECT ID_Stamm FROM Tab_Pufferspeicher WHERE ID = ?", puffer)));
            Assert.Null(PufferSpCtrl.Katalogherkunft(puffer));

            // Das Referenzprojekt bleibt ohne Verweis.
            Assert.Equal(0L, Convert.ToInt64(Wert("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND ID_Stamm IS NOT NULL", P_ZAPF)));
        }
    }
}
