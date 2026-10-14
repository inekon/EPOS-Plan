using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Modellprüfung</b> (Welle MA-1a, Entwurf Geführter Modellaufbau 2.2–2.4, E137) gegen die Arbeitskopie der
    /// Testdatenbank: die Referenzprojekte 1007 (Einzonen), 1052 und 1054 (Zonen) sind nie rot; je Gruppe ein synthetischer
    /// Fall mit Vorschlag, Übernahme und Rücknahme. Die Arbeitskopie fällt nach jedem Test — die Referenzprojekte der Datei
    /// bleiben unberührt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ModellpruefungDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public ModellpruefungDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static int Gebaeude(int projekt)
            => Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT g.\"ID\" FROM \"Tab_Gebaeude\" g INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE z.\"ID_Projekt\" = ? ORDER BY g.\"ID\"", new DbParam("@p", projekt)).Rows[0][0], CultureInfo.InvariantCulture);

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.GetDataTable(sql, p).Rows[0][0], CultureInfo.InvariantCulture);

        private void Ausgeben(int projekt, Modellpruefergebnis e)
        {
            _aus.WriteLine(projekt + ": " + e.Status + " (" + (e.Mehrzonen ? "Zonen" : "Einzone") + ")");
            foreach (Pruefgruppenergebnis g in e.Gruppen)
                _aus.WriteLine("  " + (int)g.Gruppe + " " + g.Ampel + " " + g.Anzahl + " Vorschläge " + g.Vorschlaege + " — "
                               + string.Join(", ", g.JeCode.Select(x => x.Key + "×" + x.Value)));
        }

        [Theory]
        [InlineData(1007)]
        [InlineData(1052)]
        [InlineData(1054)]
        public void Referenzprojekte_sind_nie_rot(int projekt)
        {
            if (!_db.Vorhanden) return;
            Modellpruefergebnis e = new ModellpruefungCtrl(Gebaeude(projekt)).Pruefen();
            Assert.NotNull(e);
            Ausgeben(projekt, e);
            Assert.NotEqual(Rechenstatus.NichtRechenfaehig, e.Status);
            Assert.Equal(5, e.Gruppen.Count);
            Assert.Equal(projekt != 1007, e.Mehrzonen);
            Assert.Equal(Pruefampel.Gruen, e.Gruppe(Pruefgruppe.Rechenfaehigkeit).Ampel);
        }

        [Fact]
        public void Nur_U_Bauteil_bekommt_Katalog_oder_Typaufbau_mit_U_Naehe_und_Ruecknahme_stellt_zurueck()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1052);
            var ctrl = new ModellpruefungCtrl(g);
            Modellpruefergebnis vor = ctrl.Pruefen();
            List<Modellbefund> nurU = vor.Befunde.Where(b => b.Code == "NUR_U_WERT").ToList();
            Assert.NotEmpty(nurU);
            foreach (Modellbefund b in nurU)
            {
                Assert.NotNull(b.Vorschlag);
                Assert.Contains(b.Vorschlag.Herkunft, new[] { Vorschlagsherkunft.Katalogaufbau, Vorschlagsherkunft.Typaufbau });
                if (b.Vorschlag.Herkunft == Vorschlagsherkunft.Katalogaufbau)
                {
                    double u = Zahl1("SELECT \"U_Wert\" FROM \"Tab_Bauteil\" WHERE \"ID\" = ?", b.IdBauteil.Value);
                    Assert.True(Math.Abs(b.Vorschlag.U.Value - u) <= Modellvorschlagsregeln.KATALOG_U_BAND * u + 1e-9);
                }
            }
            const string OHNE = "SELECT COUNT(*) FROM \"Tab_Bauteil\" b INNER JOIN \"Tab_Zone\" z ON z.\"ID\" = b.\"ID_Zone\" WHERE z.\"ID_Gebaeude\" = ? AND b.\"ID_Aufbau\" IS NULL";
            long ohneVor = Zahl(OHNE, new DbParam("@g", g));
            long aufbautenVor = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau\"");

            Uebernahmeergebnis u1 = ctrl.Uebernehmen(Pruefgruppe.AufbautenStoffwerte);
            Assert.True(u1.Ok, u1.Meldung);
            Assert.Equal(nurU.Count, u1.Uebernommen);
            Assert.True(ctrl.KannZuruecknehmen(Pruefgruppe.AufbautenStoffwerte));
            Assert.DoesNotContain(u1.Neu.Befunde, b => b.Code == "NUR_U_WERT");
            Assert.Equal(ohneVor - nurU.Count, Zahl(OHNE, new DbParam("@g", g)));
            Assert.NotEqual(Rechenstatus.NichtRechenfaehig, u1.Neu.Status);
            // Das U der Datei bleibt (E95 Nr. 1).
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil\" b INNER JOIN \"Tab_Zone\" z ON z.\"ID\" = b.\"ID_Zone\" WHERE z.\"ID_Gebaeude\" = ? AND b.\"U_Wert\" IS NULL",
                                 new DbParam("@g", g)));

            Uebernahmeergebnis r = ctrl.Zuruecknehmen(Pruefgruppe.AufbautenStoffwerte);
            Assert.True(r.Ok, r.Meldung);
            Assert.False(ctrl.KannZuruecknehmen(Pruefgruppe.AufbautenStoffwerte));
            Assert.Equal(ohneVor, Zahl(OHNE, new DbParam("@g", g)));
            Assert.Equal(aufbautenVor, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau\""));
            Assert.Equal(nurU.Count, r.Neu.Befunde.Count(b => b.Code == "NUR_U_WERT"));
        }

        private static double Zahl1(string sql, int id)
            => Convert.ToDouble(DataRepository.GetDataTable(sql, new DbParam("@i", id)).Rows[0][0], CultureInfo.InvariantCulture);

        [Fact]
        public void Fehlendes_Aussenbauteil_bekommt_den_Projektaufbau_mit_der_meisten_Flaeche()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1052);
            var ctrl = new ModellpruefungCtrl(g);
            Assert.True(ctrl.Uebernehmen(Pruefgruppe.AufbautenStoffwerte).Ok);
            int zone = Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"Rang\", \"ID\"", new DbParam("@g", g)).Rows[0][0], CultureInfo.InvariantCulture);
            // Erwartet: der Aufbau der Außenwände an Außenluft mit der meisten Fläche im Gebäude.
            DataTable t = DataRepository.GetDataTable(
                "SELECT b.\"ID_Aufbau\", SUM(b.\"Flaeche\") AS f FROM \"Tab_Bauteil\" b INNER JOIN \"Tab_Zone\" z ON z.\"ID\" = b.\"ID_Zone\" " +
                "WHERE z.\"ID_Gebaeude\" = ? AND b.\"Bauteilart\" = 'AUSSENWAND' AND b.\"Randbedingung\" = 'AUSSENLUFT' AND b.\"ID_Aufbau\" IS NOT NULL " +
                "GROUP BY b.\"ID_Aufbau\" ORDER BY f DESC, b.\"ID_Aufbau\"", new DbParam("@g", g));
            int erwartet = Convert.ToInt32(t.Rows[0][0], CultureInfo.InvariantCulture);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("INSERT INTO \"Tab_Bauteil\" (\"ID_Zone\", \"Rang\", \"Bezeichner\", \"Bauteilart\", \"Flaeche\", \"Neigung\", \"Azimut\", \"Randbedingung\") " +
                             "VALUES (?, 99, 'Neue Wand', 'AUSSENWAND', 12.5, 90, 180, 'AUSSENLUFT')", new DbParam("@z", zone));
                v.Commit();
            }
            var ctrl2 = new ModellpruefungCtrl(g);
            Modellpruefergebnis e = ctrl2.Pruefen();
            Assert.Equal(Rechenstatus.NichtRechenfaehig, e.Status);
            Modellbefund b = Assert.Single(e.Befunde, x => x.Code == "OHNE_U_OHNE_AUFBAU");
            Assert.Equal(Pruefampel.Rot, b.Ampel);
            Assert.Equal(Vorschlagsherkunft.Projektaufbau, b.Vorschlag.Herkunft);
            Assert.Equal(erwartet, b.Vorschlag.IdAufbau);
            Uebernahmeergebnis u = ctrl2.Uebernehmen(Pruefgruppe.AufbautenStoffwerte);
            Assert.True(u.Ok, u.Meldung);
            Assert.NotEqual(Rechenstatus.NichtRechenfaehig, u.Neu.Status);
            Assert.Equal(erwartet, Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT \"ID_Aufbau\" FROM \"Tab_Bauteil\" WHERE \"Bezeichner\" = 'Neue Wand'").Rows[0][0], CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Zone_ohne_Profil_bekommt_Profil_nach_Regel_und_Ruecknahme_entfernt_die_Kalender()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1052);
            int zone = Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ? AND \"IstBeheizt\" = 1 ORDER BY \"Rang\", \"ID\"", new DbParam("@g", g)).Rows[0][0],
                CultureInfo.InvariantCulture);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("DELETE FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone\" = ?", new DbParam("@z", zone));
                v.Ausfuehren("DELETE FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Zone\" = ?", new DbParam("@z", zone));
                v.Ausfuehren("UPDATE \"Tab_Zone\" SET \"Nutzungsprofil\" = NULL WHERE \"ID\" = ?", new DbParam("@z", zone));
                v.Commit();
            }
            var ctrl = new ModellpruefungCtrl(g);
            Modellbefund b = Assert.Single(ctrl.Pruefen().Befunde, x => x.Code == "ZONE_OHNE_PROFIL");
            Assert.Equal(zone, b.IdZone);
            Assert.NotNull(b.Vorschlag);
            Assert.Equal(Vorschlagsart.Profil, b.Vorschlag.Art);
            // Hotel: keine DIN-Nummer, kein Raumtyp, keine Vorgabe der Gebäudeart → EPOS-Muster.
            Assert.Equal(Vorschlagsherkunft.EposMuster, b.Vorschlag.Herkunft);

            Uebernahmeergebnis u = ctrl.Uebernehmen(Pruefgruppe.NutzungKonditionierung);
            Assert.True(u.Ok, u.Meldung);
            Assert.DoesNotContain(u.Neu.Befunde, x => x.Code == "ZONE_OHNE_PROFIL");
            const string KAL = "SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone\" = ?";
            const string PROFIL = "SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID\" = ? AND \"Nutzungsprofil\" IS NOT NULL";
            // Die Zone trägt das Profil (Kalender nur für Größen mit Kennwert, NP-F13).
            Assert.Equal(1, Zahl(PROFIL, new DbParam("@z", zone)));

            Uebernahmeergebnis r = ctrl.Zuruecknehmen(Pruefgruppe.NutzungKonditionierung);
            Assert.True(r.Ok, r.Meldung);
            Assert.Equal(0, Zahl(KAL, new DbParam("@z", zone)));
            Assert.Equal(0, Zahl(PROFIL, new DbParam("@z", zone)));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Zone\" = ?", new DbParam("@z", zone)));
            Assert.Single(r.Neu.Befunde, x => x.Code == "ZONE_OHNE_PROFIL");
        }

        [Fact]
        public void Alle_uebernehmen_macht_das_Zonenmodell_gruen_oder_gelb_und_baut_keinen_Vorschlag_doppelt()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            var ctrl = new ModellpruefungCtrl(g);
            Modellpruefergebnis vor = ctrl.Pruefen();
            Uebernahmeergebnis u = ctrl.AlleUebernehmen();
            Assert.True(u.Ok, u.Meldung);
            Ausgeben(1054, u.Neu);
            Assert.NotEqual(Rechenstatus.NichtRechenfaehig, u.Neu.Status);
            Assert.Equal(vor.Befunde.Count(b => b.Vorschlag != null), u.Uebernommen);
            Assert.Equal(0, u.Neu.Befunde.Count(b => b.Vorschlag != null));
            Assert.Contains(Pruefgruppe.AufbautenStoffwerte, u.Gruppen);
            // Ein Typaufbau je Bauart und U — nicht je Bauteil.
            long typ = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau\" WHERE \"ID_Projekt\" = 1054 AND \"Typaufbau\" IS NOT NULL");
            Assert.True(typ <= vor.Befunde.Count(b => b.Vorschlag?.Herkunft == Vorschlagsherkunft.Typaufbau));
        }
    }

    /// <summary>Die Modellprüfung ohne Datenbank: Regeln, Gruppen und Ressourcen.</summary>
    public sealed class ModellpruefungRegelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Modellpruefstand Stand()
        {
            var s = new Modellpruefstand
            {
                IdGebaeude = 1, IdProjekt = 1, Nutzflaeche = 100, Raumhoehe = 2.5, FlaecheNutzer = 30, Luftwechselrate = 0.5, Bauweise = 50,
                KlimaregionVorhanden = true, Gebaeudeart = "Einfamilienhaus",
            };
            var z = new ZoneModel { ID = 11, ID_Gebaeude = 1, Bezeichner = "Wohnen", Nutzflaeche = 100, Raumsolltemperatur_Tag = 20,
                                    Luftwechsel_Infiltration = 0.3 };
            z.Bauteile.Add(new BauteilModel { ID = 101, ID_Zone = 11, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 50,
                                              U_Wert = 0.3, Neigung = 90, Azimut = 180, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT, ID_Aufbau = 7 });
            z.Bauteile.Add(new BauteilModel { ID = 102, ID_Zone = 11, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 40,
                                              U_Wert = 0.2, Neigung = 0, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT, ID_Aufbau = 8 });
            z.Bauteile.Add(new BauteilModel { ID = 103, ID_Zone = 11, Bezeichner = "Boden", Bauteilart = DbWerte.BAUTEILART_BODENPLATTE, Flaeche = 40,
                                              U_Wert = 0.3, Neigung = 180, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH, ID_Aufbau = 9 });
            s.Zonen.Add(z);
            s.Kalender[11] = new HashSet<string> { DbWerte.KOND_GROESSE_HEIZSOLL };
            s.Kalendernutzung[11] = DbWerte.KOND_NUTZUNG_WOHNEN;
            s.Gebaeudevorgaben.Add(DbWerte.KOND_ZEILE_FERIEN);
            s.Gebaeudevorgaben.Add(DbWerte.KOND_ZEILE_SAISON);
            s.Feiertagsland = "BY";
            s.Projektaufbauten[7] = new Aufbaukennung(7, "AW Projekt", DbWerte.BAUTEILART_AUSSENWAND, false, 0.3, 200, false, 0);
            s.Projektaufbauten[8] = new Aufbaukennung(8, "Dach Projekt", DbWerte.BAUTEILART_DACH, false, 0.2, 100, false, 1);
            s.Projektaufbauten[9] = new Aufbaukennung(9, "Boden Projekt", DbWerte.BAUTEILART_BODENPLATTE, false, 0.3, 150, false, 2);
            s.Katalogaufbauten.Add(new Aufbaukennung(51, "AW Katalog 0,5", DbWerte.BAUTEILART_AUSSENWAND, false, 0.5, 220, false, 0));
            s.Katalogaufbauten.Add(new Aufbaukennung(52, "AW Katalog 1,2", DbWerte.BAUTEILART_AUSSENWAND, false, 1.2, 300, false, 1));
            s.Katalogaufbauten.Add(new Aufbaukennung(53, "AW ungeeignet 1,0", DbWerte.BAUTEILART_AUSSENWAND, true, 1.0, 300, false, 2));
            return s;
        }

        [Fact]
        public void Ein_vollstaendiges_Modell_ist_gruen()
        {
            Modellpruefergebnis e = Modellpruefung.Pruefen(Stand());
            Assert.Equal(Rechenstatus.Vollstaendig, e.Status);
            Assert.All(e.Gruppen, g => Assert.Equal(Pruefampel.Gruen, g.Ampel));
        }

        [Fact]
        public void Jede_Gruppe_meldet_ihren_Befund_mit_der_richtigen_Ampel()
        {
            Modellpruefstand s = Stand();
            s.Zonen[0].Bauteile[0].Flaeche = 0;                 // G1 rot
            s.Zonen[0].Bauteile[1].ID_Aufbau = null;            // G2 gelb (nur U)
            s.Gebaeudevorgaben.Remove(DbWerte.KOND_ZEILE_SAISON); // G3 gelb
            s.KlimaregionVorhanden = false;                     // G4 rot
            s.FlaecheNutzer = null;                             // G5 rot
            Modellpruefergebnis e = Modellpruefung.Pruefen(s);
            Assert.Equal(Rechenstatus.NichtRechenfaehig, e.Status);
            Assert.Contains(e.Gruppe(Pruefgruppe.GeometrieBauteile).Befunde, b => b.Code == Modellpruefung.FLAECHE_FEHLT && b.Ampel == Pruefampel.Rot);
            Modellbefund nurU = Assert.Single(e.Gruppe(Pruefgruppe.AufbautenStoffwerte).Befunde);
            Assert.Equal(Modellpruefung.NUR_U_WERT, nurU.Code);
            Assert.Equal(Pruefampel.Gelb, nurU.Ampel);
            // Kein anderes Dach trägt einen Projektaufbau, der Katalog keinen Dachaufbau → Typaufbau, auf U 0,2 abgeglichen.
            Assert.Equal(Vorschlagsherkunft.Typaufbau, nurU.Vorschlag.Herkunft);
            Assert.Equal(0.2, nurU.Vorschlag.U);
            Assert.Contains(e.Gruppe(Pruefgruppe.NutzungKonditionierung).Befunde, b => b.Code == Modellpruefung.HEIZPERIODE_FEHLT);
            Assert.Equal(Pruefampel.Rot, e.Gruppe(Pruefgruppe.OrtLage).Ampel);
            Assert.Contains(e.Gruppe(Pruefgruppe.Rechenfaehigkeit).Befunde, b => b.Code == Modellpruefung.PFLICHTGROESSE_FEHLT);
            Assert.Equal(Pruefsprungziel.Aufbau, e.Gruppe(Pruefgruppe.AufbautenStoffwerte).Sprungziel);
        }

        [Fact]
        public void Aufbauregel_Projekt_vor_Katalog_im_U_Band_vor_Typaufbau()
        {
            Modellpruefstand s = Stand();
            var neu = new BauteilModel { ID = 104, ID_Zone = 11, Bezeichner = "Wand Nord", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 20,
                                         U_Wert = 1.1, Neigung = 90, Azimut = 0, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT };
            // (1) Projektaufbau gleicher Art und Randbedingung.
            Assert.Equal(Vorschlagsherkunft.Projektaufbau, Modellvorschlagsregeln.Aufbau(s, neu).Herkunft);
            // (2) Ohne Projektaufbau: Katalog im U-Band (1,2 liegt bei U 1,1 im Band; der ungeeignete 1,0 zählt nicht).
            s.Zonen[0].Bauteile[0].ID_Aufbau = null;
            Pruefvorschlag k = Modellvorschlagsregeln.Aufbau(s, neu);
            Assert.Equal(Vorschlagsherkunft.Katalogaufbau, k.Herkunft);
            Assert.Equal(52, k.IdAufbau);
            // (3) Kein Katalogaufbau im Band → Typaufbau, auf das U abgeglichen.
            neu.U_Wert = 2.5;
            Pruefvorschlag t = Modellvorschlagsregeln.Aufbau(s, neu);
            Assert.Equal(Vorschlagsherkunft.Typaufbau, t.Herkunft);
            Assert.False(string.IsNullOrEmpty(t.Typaufbau));
            // Ohne U: Katalogfolge, ohne Band.
            neu.U_Wert = null;
            Assert.Equal(51, Modellvorschlagsregeln.Aufbau(s, neu).IdAufbau);
        }

        [Fact]
        public void Profilregel_DIN_vor_Raumtyp_vor_Gebaeudeart_vor_EPOS_Muster()
        {
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Vorgabe;
            Assert.Equal(Vorschlagsherkunft.DinNummer, Modellvorschlagsregeln.ProfilNachRegel(v, new Zonenquelle(1, null, null), "Hotel", null).Herkunft);
            Assert.Equal(Vorschlagsherkunft.Gebaeudeart, Modellvorschlagsregeln.ProfilNachRegel(v, null, "Einfamilienhaus", null).Herkunft);
            (Planprofil p, Vorschlagsherkunft h) = Modellvorschlagsregeln.ProfilNachRegel(v, null, "Hotel", "Wohngebaeude");
            Assert.Equal(Vorschlagsherkunft.EposMuster, h);
            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, p.Name);
            Assert.Equal(DbWerte.KOND_NUTZUNG_SONSTIGE, Modellvorschlagsregeln.ProfilNachRegel(v, null, "Hotel", "Nicht Wohngebaeude").Profil.Name);
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Jeder_Befundcode_Status_und_Gruppenname_hat_einen_Text(string kultur)
        {
            CultureInfo c = CultureInfo.GetCultureInfo(kultur);
            foreach (string code in Modellpruefung.CODES.Values.SelectMany(x => x))
                Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("MP_BEFUND_" + code, c)), code);
            foreach (Vorschlagsherkunft h in new[] { Vorschlagsherkunft.DinNummer, Vorschlagsherkunft.Raumtyp, Vorschlagsherkunft.Gebaeudeart, Vorschlagsherkunft.EposMuster })
                Assert.NotNull(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("MP_VORSCHLAG_PROFIL_" + h.ToString().ToUpperInvariant(), c));
            foreach (Pruefgruppe g in Enum.GetValues(typeof(Pruefgruppe)))
                Assert.NotNull(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("MP_GRUPPE_" + g.ToString().ToUpperInvariant(), c));
            foreach (Rechenstatus st in Enum.GetValues(typeof(Rechenstatus)))
                Assert.NotNull(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("MP_STATUS_" + st.ToString().ToUpperInvariant(), c));
        }
    }
}
