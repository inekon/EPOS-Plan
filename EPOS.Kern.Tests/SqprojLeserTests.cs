using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 33 — Leser und Fassung</b> (Datenaustauschkonzept 16.7): die selbst erzeugte Kleinstdatei kommt vollständig an
    /// (Räume, Zonen, Profile, Kurven, Abschnitte, Übersprungenes), eine fehlende Tabelle ist benannt abgelehnt, eine
    /// Nicht-SQLite-Datei ebenso, und der Leser schreibt nicht: Bytes unverändert, keine Journale, Schreibversuch scheitert.
    /// Dazu der Zusatzschritt im Importablauf (HottCAD-Prüfung, Größengrenze, Arbeitskopie).
    /// </summary>
    public sealed class SqprojLeserTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _pfade = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string p in _pfade)
                try { if (File.Exists(p)) File.Delete(p); } catch (IOException) { }
        }

        private string Probe(SqprojProbenErzeuger e, string name = "probe")
        {
            string p = SqprojProbenErzeuger.TempPfad(name);
            _pfade.Add(p);
            return e.Schreiben(p);
        }

        private static IEnumerable<string> Schluessel(SqprojAbbild a) => a.Meldungen.Select(m => m.Schluessel);

        [Fact]
        public void Die_Standardprobe_kommt_vollstaendig_an()
        {
            SqprojAbbild a = SqprojLeser.Lesen(Probe(SqprojProbenErzeuger.Standard()));
            Assert.False(a.Abgelehnt, a.Ablehnung?.ToString());
            Assert.Equal("16.7", a.Fassung);
            Assert.Equal("Probegebäude", a.Gebaeudename);
            Assert.Equal(new[] { "EG", "OG" }, a.Geschosse.Select(g => g.Name));
            Assert.Equal(new[] { "Raum A", "Raum B", "Raum C", "Raum D", "Raum E" }, a.Raeume.Select(r => r.Name));
            Assert.Equal("OG", a.Raum("R4").GeschossName);
            Assert.Equal(SqprojProbenErzeuger.GID_D, a.Raum("R4").Gid);
            Assert.Equal(1, a.Raum("R3").Raumart);
            Assert.Equal(new[] { "Leer", "Nutzung EG", "Nutzung OG", "Simulation EG", "Simulation OG" }, a.Zonen.Select(z => z.Name));
            SqprojZone nutzungEg = a.Zonen.Single(z => z.Name == "Nutzung EG");
            Assert.Equal(new[] { "R1", "R2" }, nutzungEg.Raeume);
            Assert.Equal(1, nutzungEg.Nutzungsprofil.Profilnummer);
            Assert.Equal(7, nutzungEg.Nutzungsprofil.BetriebVon);
            Assert.Equal(18, nutzungEg.Nutzungsprofil.BetriebBis);
            Assert.Null(nutzungEg.Nutzungsprofil.HeizVon);   // Nullzeit = keine Angabe
            Assert.Equal(21.0, nutzungEg.Nutzungsprofil.Raumtemperatur);
            Assert.Equal(11, nutzungEg.Nutzungsprofil.Betriebsstunden);

            SqprojZone sim = a.Zonen.Single(z => z.Name == "Simulation EG");
            Assert.True(sim.IstSimulationszone);
            Assert.Null(sim.Nutzungsprofil);
            Assert.Equal(SqprojTagesart.Werktage, sim.Gruppe.Tagesart);
            Assert.Equal(new[] { 4, 6, 8, 10 }, sim.Gruppe.Profile.Keys);
            SqprojZeitprofil heizen = sim.Gruppe.Profil(SqprojProfilklasse.HEIZEN);
            Assert.Equal(17.0, heizen.Stunden[0]);
            Assert.Equal(21.0, heizen.Stunden[6]);
            Assert.Equal(40.0, heizen.Stunden[12]);
            SqprojZeitprofil personen = sim.Gruppe.Profil(SqprojProfilklasse.PERSONEN);
            Assert.Equal(4.0, personen.Personen);
            Assert.Equal(80.0, personen.WattJePerson);
            Assert.Equal(2, personen.Betriebsart);
            Assert.Equal(new[] { 1, 182 }, personen.Abschnitte.Select(s => s.Beginn));
            Assert.Equal(new[] { true, true, false, false, false, false, false }, personen.Abschnitte[1].Wochentage);
            Assert.Equal(10.0, sim.Gruppe.Profil(SqprojProfilklasse.GERAETE).GeraeteWm2);
            Assert.Equal(SqprojTagesart.Unbekannt, a.Zonen.Single(z => z.Name == "Leer").Gruppe.Tagesart);

            Assert.Equal(6, a.Zeitprofile);
            Assert.Equal(4, a.Abschnitte);
            Assert.Equal(new Dictionary<int, int> { [2] = 1, [10] = 1 }, a.ZonentypenUebersprungen);
            Assert.Equal(new Dictionary<int, int> { [9] = 1 }, a.KlassenUebersprungen);
            Assert.Equal(new Dictionary<int, int> { [1] = 5, [2] = 2 }, a.Betriebsarten);   // H3 trägt beide
            Assert.Equal(new int?[] { 2, 1 }, new[] { a.Zonen.Single(z => z.Name == "Simulation OG").Gruppe.Profil(SqprojProfilklasse.HEIZEN).Betriebsarten[0],
                                                     a.Zonen.Single(z => z.Name == "Simulation OG").Gruppe.Profil(SqprojProfilklasse.HEIZEN).Betriebsarten[12] });
            Assert.Equal(SqprojTagesart.WerktageSamstag, a.Zonen.Single(z => z.Name == "Simulation OG").Gruppe.Tagesart);
            Assert.Equal(71, a.Zonen.Single(z => z.Name == "Simulation OG").Gruppe.Profilnummer);
            Assert.Equal(4, a.Gebaeudegruppe.Gruppenart);
            Assert.Equal(SqprojTagesart.AlleTage, a.Gebaeudegruppe.Tagesart);
            Assert.Equal(3, Schluessel(a).Count(s => s == SqprojProtokoll.TAGESART_ANNAHME));
            Assert.Contains(SqprojProtokoll.TAGESART_UNBEKANNT, Schluessel(a));
            Assert.Contains(SqprojProtokoll.ABSCHNITTSART_UNBEKANNT, Schluessel(a));
            Assert.Equal(2, Schluessel(a).Count(s => s == SqprojProtokoll.ZONENTYP_UEBERSPRUNGEN));
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojProtokoll.KLASSE_UEBERSPRUNGEN && m.Werte[0] == "9");
        }

        [Fact]
        public void Eine_fehlende_Tabelle_lehnt_benannt_ab()
        {
            SqprojAbbild a = SqprojLeser.Lesen(Probe(SqprojProbenErzeuger.Standard().Ohne("PdProfileTaskSerial")));
            Assert.True(a.Abgelehnt);
            Assert.Equal(SqprojProtokoll.TABELLE_FEHLT, a.Ablehnung.Schluessel);
            Assert.Equal("PdProfileTaskSerial", a.Ablehnung.Werte[0]);
            Assert.Empty(a.Zonen);
        }

        [Fact]
        public void Eine_Datei_ohne_SQLite_Kennung_lehnt_benannt_ab()
        {
            string p = SqprojProbenErzeuger.TempPfad("text");
            _pfade.Add(p);
            File.WriteAllText(p, "keine Projektdatei");
            SqprojAbbild a = SqprojLeser.Lesen(p);
            Assert.Equal(SqprojProtokoll.KEINE_DATEI, a.Ablehnung?.Schluessel);
        }

        [Fact]
        public void Der_Leser_schreibt_nicht()
        {
            string p = Probe(SqprojProbenErzeuger.Standard());
            byte[] vorher = SHA256.HashData(File.ReadAllBytes(p));
            DateTime zeit = File.GetLastWriteTimeUtc(p);
            SqprojLeser.Lesen(p);
            using (SqliteConnection c = SqprojLeser.Oeffnen(p))
            using (SqliteCommand k = c.CreateCommand())
            {
                k.CommandText = "CREATE TABLE Spur (A)";
                Assert.Throws<SqliteException>(() => k.ExecuteNonQuery());
            }
            Assert.Equal(vorher, SHA256.HashData(File.ReadAllBytes(p)));
            Assert.Equal(zeit, File.GetLastWriteTimeUtc(p));
            Assert.False(File.Exists(p + "-journal"));
            Assert.False(File.Exists(p + "-wal"));
        }

        [Fact]
        public void Zwei_Laeufe_lesen_dasselbe()
        {
            string p = Probe(SqprojProbenErzeuger.Standard());
            string Abdruck(SqprojAbbild a) => string.Join("|", a.Raeume.Select(r => r.Uuid)) + "#" +
                string.Join("|", a.Zonen.Select(z => z.Uuid + ":" + string.Join(",", z.Raeume))) + "#" +
                string.Join("|", a.Meldungen.Select(m => m.ToString()));
            Assert.Equal(Abdruck(SqprojLeser.Lesen(p)), Abdruck(SqprojLeser.Lesen(p)));
        }

        [Fact]
        public void Jeder_Meldungsschluessel_hat_Texte_in_beiden_Sprachen()
        {
            List<string> schluessel = typeof(SqprojProtokoll).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Where(s => s.StartsWith(SqprojProtokoll.PRAEFIX, StringComparison.Ordinal) && s != SqprojProtokoll.PRAEFIX || s == SqprojProtokoll.BELEG)
                .ToList();
            Assert.True(schluessel.Count >= 20);
            foreach (string kultur in new[] { "de-DE", "en-US" })
                foreach (string s in schluessel)
                    Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s, CultureInfo.GetCultureInfo(kultur))), s + " " + kultur);
        }

        // ------------------------------------------------------------------
        //  Zusatzschritt im Importablauf
        // ------------------------------------------------------------------

        [Fact]
        public void Der_Ablauf_laedt_nur_zu_einem_HottCAD_Export_dazu()
        {
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            using (FileStream s = File.OpenRead(Probe(SqprojProbenErzeuger.Standard())))
            {
                SqprojStand stand = ablauf.ProjektdateiLesen(s, "projekt.sqproj", 0);
                Assert.Equal(SqprojProtokoll.KEIN_HOTTCAD, stand.Ablehnung?.Schluessel);
            }
            Assert.False(GebaeudeImportAblauf.IstHottcad(ablauf.Abbild, 0));
            Assert.Equal(SqprojProtokoll.NICHT_GELESEN, new GebaeudeImportAblauf().ProjektdateiLesen(Stream.Null, "x.sqproj", 0).Ablehnung?.Schluessel);
        }

        [Fact]
        public void Der_Ablauf_liest_eine_Arbeitskopie_und_raeumt_sie_weg()
        {
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            ablauf.Abbild.Gebaeude[0].Art = "TModelBuilding";
            Assert.True(GebaeudeImportAblauf.IstHottcad(ablauf.Abbild, 0));
            string ordner = Path.Combine(Path.GetTempPath(), "epos-sqproj-test", "arbeit-" + Guid.NewGuid().ToString("N"));
            ablauf.Arbeitsordner = () => ordner;
            string p = Probe(SqprojProbenErzeuger.Standard());
            SqprojStand stand = ablauf.ProjektdateiLesen(p, 0);
            Assert.False(stand.Abgelehnt, stand.Ablehnung?.ToString());
            Assert.Equal(new FileInfo(p).Length, stand.Bytes);
            Assert.Equal(64, stand.Hash.Length);
            Assert.Equal(5, stand.Zonen);
            Assert.Equal(0, stand.Abgeglichen);       // die Räume des Zonenhauses heißen anders
            Assert.Equal(5, stand.NichtAbgeglichen);
            Assert.Empty(Directory.GetFiles(ordner));
            Directory.Delete(ordner);
            Assert.Same(stand, ablauf.Projektdatei);
            Assert.Same(stand, ablauf.Zuordnen(0, null).Projektdatei);
        }

        [Fact]
        public void Die_Projektdatei_hat_ihre_eigene_Grenze_250_und_100_MB()
        {
            Assert.Equal(250L * 1024 * 1024, SqprojProfil.MAX_BYTES);
            Assert.Equal(100L * 1024 * 1024, SqprojProfil.MAX_BYTES_IOS);
            Assert.Equal(SqprojProfil.MAX_BYTES, SqprojProfil.GrenzeFuerPlattform(false));
            Assert.Equal(SqprojProfil.MAX_BYTES_IOS, SqprojProfil.GrenzeFuerPlattform(true));
            Assert.Equal(SqprojProfil.MAX_BYTES, new GebaeudeImportAblauf().ProjektdateiMaxBytes);
            // Gegenprobe: die IFC-Grenze bleibt 50/20 MB.
            Assert.Equal(50L * 1024 * 1024, new IfcImportProfil().GrenzeFuerPlattform(false));
            Assert.Equal(20L * 1024 * 1024, new IfcImportProfil().GrenzeFuerPlattform(true));
        }

        /// <summary>Ablauf mit gelesenem HottCAD-IFC, dessen IFC-Profil kleiner ist als die Projektdatei.</summary>
        private static GebaeudeImportAblauf HottcadAblauf()
        {
            string ifc = Path.Combine(IfcProbenTests.Ordner(), "ifc4_zonen.ifc");
            var ablauf = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(ifc))
                ablauf.Lesen(s, ifc, new IfcImportProfil(new FileInfo(ifc).Length + 100));
            ablauf.Abbild.Gebaeude[0].Art = "TModelBuilding";
            return ablauf;
        }

        [Fact]
        public void Die_Grenze_des_IFC_Profils_gilt_nicht_fuer_die_Projektdatei()
        {
            GebaeudeImportAblauf ablauf = HottcadAblauf();
            string probe = Probe(SqprojProbenErzeuger.Standard());
            Assert.True(new FileInfo(probe).Length > ablauf.Profil.MaxBytes);   // größer als die IFC-Grenze …
            SqprojStand stand = ablauf.ProjektdateiLesen(probe, 0);
            Assert.False(stand.Abgelehnt, stand.Ablehnung?.Schluessel);         // … und doch gelesen
        }

        [Fact]
        public void Zu_gross_nennt_die_wirksame_Grenze_in_MB()
        {
            GebaeudeImportAblauf ablauf = HottcadAblauf();
            ablauf.ProjektdateiMaxBytes = 1024 * 1024 / 10;   // 0,1 MB
            string probe = Probe(SqprojProbenErzeuger.Standard().Raum("RX", new string('x', 200_000), "F1", null, 1.0));
            SqprojStand stand = ablauf.ProjektdateiLesen(probe, 0);
            Assert.Equal(SqprojProtokoll.ZU_GROSS, stand.Ablehnung?.Schluessel);
            Assert.Equal("0.1", stand.Ablehnung.Werte[1]);
            Assert.Equal(SqprojProtokoll.Mb(new FileInfo(probe).Length), stand.Ablehnung.Werte[0]);
            Assert.Contains("0.1 MB", string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.IMP_SQ_PROT_ZU_GROSS, stand.Ablehnung.Werte));
            Assert.Equal("250", SqprojProtokoll.Mb(SqprojProfil.MAX_BYTES));
            Assert.Equal("100", SqprojProtokoll.Mb(SqprojProfil.MAX_BYTES_IOS));
        }
    }
}
