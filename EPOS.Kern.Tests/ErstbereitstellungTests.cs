using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="Erstbereitstellung"/> — der Nachweis des Anwenderentscheids
    /// <b>#157‑E‑1 / W3</b> vom 09.09.2026: Die Datenbank einer Neuinstallation entsteht
    /// aus der ausgelieferten Vorlage.
    /// </summary>
    /// <remarks>
    /// <para><b>Ohne die Testdatenbank.</b> <see cref="Erstbereitstellung.Sicherstellen"/>
    /// ist ein reiner Pfad-zu-Pfad-Vorgang und kennt <c>DataRepository.GetDBPath()</c>
    /// nicht — jeder Fall baut sich seine eigene, winzige Vorlage in einem eigenen
    /// Temp-Ordner. Dieselbe Bauart wie <see cref="DatenbanksicherungTests"/>, und aus
    /// demselben Grund braucht es keine <c>[Collection("Testdatenbank")]</c>.</para>
    ///
    /// <para><b>Was hier NICHT geprüft werden kann.</b> Der Auftrag verlangte einen Fall
    /// „Vorlage mit älterem Schemastand → nach <c>SchemaMigration</c> auf
    /// <c>SchemaStand.Zielversion</c>". <c>SchemaMigration</c> liegt in
    /// <c>WindowsFormsApplication1</c> (<c>net10.0-windows</c>, Access-Zweig mit
    /// <c>System.Data.OleDb</c>) und ist von <c>EPOS.Kern.Tests</c> aus nicht erreichbar.
    /// Geprüft wird deshalb die KERN-Hälfte der Zusicherung: Eine Vorlage mit ÄLTEREM
    /// Schemastand wird angenommen und ihr Stand gemeldet — die Bereitstellung verlangt
    /// nur, dass der Marker überhaupt da ist. Das Anheben besorgt danach
    /// <c>SchemaMigration.Ausfuehren</c> im selben Programmstart
    /// (<c>Program.Main</c>, unmittelbar nach der Bereitstellung).</para>
    /// </remarks>
    public sealed class ErstbereitstellungTests : IDisposable
    {
        private readonly string _ordner;

        public ErstbereitstellungTests()
        {
            _ordner = Path.Combine(Path.GetTempPath(),
                                   "epos-erstbereitstellung-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
        }

        public void Dispose()
        {
            // Derselbe Grund wie in DatenbanksicherungTests.Dispose: der Verbindungspool
            // haelt Dateien offen, bis er geleert wird.
            try { SqliteConnection.ClearAllPools(); } catch { /* Aufraeumen darf nicht scheitern */ }
            try { Directory.Delete(_ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
        }

        /// <summary>
        /// Baut eine winzige, gültige Auslieferungsvorlage: <c>Tab_Applikation</c> mit dem
        /// Schemamarker. Mehr prüft die Bereitstellung nicht.
        /// </summary>
        private string NeueVorlage(int schemastand = 72, string dateiname = "Kenndaten.sqlite")
        {
            string pfad = Path.Combine(_ordner, "vorlage", dateiname);
            Directory.CreateDirectory(Path.GetDirectoryName(pfad));

            using (SqliteConnection verbindung = new SqliteConnection($"Data Source={pfad};Pooling=False"))
            {
                verbindung.Open();
                using (SqliteCommand cmd = verbindung.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE Tab_Applikation (ID INTEGER PRIMARY KEY, " +
                                      "SchemaVersion INTEGER); " +
                                      "INSERT INTO Tab_Applikation (ID, SchemaVersion) VALUES (1, " +
                                      schemastand.ToString(CultureInfo.InvariantCulture) + ");";
                    cmd.ExecuteNonQuery();
                }
            }
            return pfad;
        }

        private string Zielpfad(string unterordner = "daten")
        {
            return Path.Combine(_ordner, unterordner, "Kenndaten.sqlite");
        }

        private static bool ByteGleich(string a, string b)
        {
            byte[] x = File.ReadAllBytes(a);
            byte[] y = File.ReadAllBytes(b);
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }


        // =====================================================================
        //  Der Normalfall: die Vorlage wird kopiert
        // =====================================================================

        [Fact]
        public void Fehlt_die_Datenbank_wird_die_Vorlage_kopiert()
        {
            string vorlage = NeueVorlage();
            string ziel = Zielpfad();

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.Kopiert, erg.Lage);
            Assert.True(erg.Bereit);
            Assert.True(File.Exists(ziel));
            Assert.Equal(ziel, erg.Zielpfad);
            Assert.Equal(vorlage, erg.Vorlagepfad);
        }

        [Fact]
        public void Die_Kopie_ist_byte_gleich_zur_Vorlage()
        {
            string vorlage = NeueVorlage();
            string ziel = Zielpfad();

            Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.True(ByteGleich(vorlage, ziel));
        }

        /// <summary>
        /// Der Zielordner muss nicht vorher da sein — beim Anwender legt ihn zwar das
        /// Setup an, aber ein verstellter Datenbankpfad (<c>DBPath</c>) zeigt womöglich
        /// woandershin.
        /// </summary>
        [Fact]
        public void Der_Zielordner_wird_bei_Bedarf_angelegt()
        {
            string vorlage = NeueVorlage();
            string ziel = Path.Combine(_ordner, "neu", "tiefer", "Kenndaten.sqlite");

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.Kopiert, erg.Lage);
            Assert.True(File.Exists(ziel));
        }

        /// <summary>
        /// Die Vorlage liegt beim Anwender unter <c>%ProgramFiles%</c> und ist deshalb
        /// schreibgeschützt; <c>File.Copy</c> nimmt dieses Merkmal mit. Die
        /// Arbeitsdatenbank muss beschreibbar sein.
        /// </summary>
        [Fact]
        public void Die_Kopie_ist_nicht_schreibgeschuetzt()
        {
            string vorlage = NeueVorlage();
            new FileInfo(vorlage).IsReadOnly = true;

            string ziel = Zielpfad();
            Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.False(new FileInfo(ziel).IsReadOnly);
        }

        /// <summary>
        /// Ein liegengebliebenes <c>-wal</c> aus einem abgebrochenen Vorlauf würde beim
        /// ersten Öffnen in die frisch kopierte Datei eingespielt — sie wäre danach weder
        /// der Auslieferungsstand noch ein gültiger Stand.
        /// </summary>
        [Fact]
        public void Reste_eines_abgebrochenen_Vorlaufs_werden_entfernt()
        {
            string vorlage = NeueVorlage();
            string ziel = Zielpfad();
            Directory.CreateDirectory(Path.GetDirectoryName(ziel));
            File.WriteAllText(ziel + "-wal", "Rest eines abgebrochenen Vorlaufs");
            File.WriteAllText(ziel + "-shm", "Rest eines abgebrochenen Vorlaufs");

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.Kopiert, erg.Lage);
            Assert.False(File.Exists(ziel + "-wal"));
            Assert.False(File.Exists(ziel + "-shm"));
        }


        // =====================================================================
        //  Nie überschreiben
        // =====================================================================

        [Fact]
        public void Eine_vorhandene_Datenbank_bleibt_unberuehrt()
        {
            string vorlage = NeueVorlage();
            string ziel = Zielpfad();
            Directory.CreateDirectory(Path.GetDirectoryName(ziel));
            File.WriteAllText(ziel, "Die Projekte des Anwenders.");

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.VorhandenBelassen, erg.Lage);
            Assert.True(erg.Bereit);
            Assert.Equal("Die Projekte des Anwenders.", File.ReadAllText(ziel));
        }

        /// <summary>
        /// <b>Auch eine kaputte Zieldatei wird nicht ersetzt.</b> Der Anwender hat dort
        /// seine Projekte; eine „Reparatur" durch Überschreiben wäre Datenverlust. Die
        /// Bereitstellung prüft deshalb bewusst nur das VORHANDENSEIN — was danach zu tun
        /// ist, entscheidet die Startprüfung <c>DataRepository.DatenbankVorhanden()</c>.
        /// </summary>
        [Fact]
        public void Auch_eine_kaputte_Datenbank_wird_nicht_ersetzt()
        {
            string vorlage = NeueVorlage();
            string ziel = Zielpfad();
            Directory.CreateDirectory(Path.GetDirectoryName(ziel));
            File.WriteAllText(ziel, "kein gueltiges SQLite");

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.VorhandenBelassen, erg.Lage);
            Assert.Equal("kein gueltiges SQLite", File.ReadAllText(ziel));
        }


        // =====================================================================
        //  Fehlende Vorlage
        // =====================================================================

        [Fact]
        public void Fehlt_die_Vorlage_entsteht_keine_Zieldatei()
        {
            string vorlage = Path.Combine(_ordner, "gibtsnicht", "Kenndaten.sqlite");
            string ziel = Zielpfad();

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.VorlageFehlt, erg.Lage);
            Assert.False(erg.Bereit);
            Assert.False(File.Exists(ziel));
        }

        /// <summary>
        /// Die Startmeldung nennt den erwarteten Vorlagenpfad — dafür muss das Ergebnis
        /// ihn führen, auch wenn es die Datei nicht gibt.
        /// </summary>
        [Fact]
        public void Das_Ergebnis_nennt_den_erwarteten_Vorlagenpfad()
        {
            string vorlage = Path.Combine(_ordner, "gibtsnicht", "Kenndaten.sqlite");

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(Zielpfad(), vorlage);

            Assert.Equal(vorlage, erg.Vorlagepfad);
            Assert.Contains(vorlage, erg.Meldung, StringComparison.Ordinal);
        }

        [Fact]
        public void Ein_leerer_Vorlagenpfad_ist_eine_fehlende_Vorlage()
        {
            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(Zielpfad(), "");

            Assert.Equal(Erstbereitstellungslage.VorlageFehlt, erg.Lage);
            Assert.False(File.Exists(Zielpfad()));
        }

        [Fact]
        public void Ohne_Zielpfad_gibt_es_einen_Fehler()
        {
            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen("", NeueVorlage());

            Assert.Equal(Erstbereitstellungslage.Fehler, erg.Lage);
            Assert.False(erg.Bereit);
        }


        // =====================================================================
        //  Nie halb liegen lassen
        // =====================================================================

        [Fact]
        public void Eine_kaputte_Vorlage_hinterlaesst_keine_Zieldatei()
        {
            string vorlage = Path.Combine(_ordner, "vorlage", "Kenndaten.sqlite");
            Directory.CreateDirectory(Path.GetDirectoryName(vorlage));
            File.WriteAllText(vorlage, "Das ist keine SQLite-Datei, sondern Text.");

            string ziel = Zielpfad();
            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.Fehler, erg.Lage);
            Assert.False(erg.Bereit);
            Assert.False(File.Exists(ziel));
        }

        /// <summary>
        /// Eine gültige SQLite-Datei OHNE <c>Tab_Applikation</c> ist keine
        /// EPOS-Plan-Datenbank. <c>PRAGMA integrity_check</c> allein fiele darauf herein —
        /// deshalb die zweite Prüfung.
        /// </summary>
        [Fact]
        public void Eine_Vorlage_ohne_Schemamarker_wird_abgewiesen()
        {
            string vorlage = Path.Combine(_ordner, "vorlage", "Kenndaten.sqlite");
            Directory.CreateDirectory(Path.GetDirectoryName(vorlage));
            using (SqliteConnection verbindung = new SqliteConnection($"Data Source={vorlage};Pooling=False"))
            {
                verbindung.Open();
                using (SqliteCommand cmd = verbindung.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE Irgendwas (Wert TEXT)";
                    cmd.ExecuteNonQuery();
                }
            }

            string ziel = Zielpfad();
            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            Assert.Equal(Erstbereitstellungslage.Fehler, erg.Lage);
            Assert.False(File.Exists(ziel));
            Assert.Contains("Tab_Applikation", erg.Meldung, StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Fehlerfall darf die VORLAGE nicht anfassen — sie gehört der Auslieferung.
        /// </summary>
        [Fact]
        public void Der_Fehlerfall_laesst_die_Vorlage_liegen()
        {
            string vorlage = Path.Combine(_ordner, "vorlage", "Kenndaten.sqlite");
            Directory.CreateDirectory(Path.GetDirectoryName(vorlage));
            File.WriteAllText(vorlage, "kaputt");

            Erstbereitstellung.Sicherstellen(Zielpfad(), vorlage);

            Assert.True(File.Exists(vorlage));
            Assert.Equal("kaputt", File.ReadAllText(vorlage));
        }


        // =====================================================================
        //  Der Schemastand
        // =====================================================================

        [Fact]
        public void Der_Schemastand_der_Vorlage_wird_gemeldet()
        {
            string vorlage = NeueVorlage(SchemaStand.Zielversion);

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(Zielpfad(), vorlage);

            Assert.Equal(SchemaStand.Zielversion, erg.Schemastand);
        }

        /// <summary>
        /// <b>Eine ÄLTERE Vorlage ist zulässig.</b> Sie wird beim Bauen des Setups
        /// eingefroren; bis zur Auslieferung können Schemaschritte dazugekommen sein.
        /// Angehoben wird sie beim selben Start von <c>SchemaMigration.Ausfuehren</c> —
        /// das ist der Windows-Teil und hier nicht erreichbar (siehe Klassenkopf).
        /// </summary>
        [Fact]
        public void Eine_aeltere_Vorlage_wird_angenommen()
        {
            int aelter = SchemaStand.Zielversion - 3;
            string vorlage = NeueVorlage(aelter);

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(Zielpfad(), vorlage);

            Assert.Equal(Erstbereitstellungslage.Kopiert, erg.Lage);
            Assert.Equal(aelter, erg.Schemastand);
            Assert.True(erg.Schemastand < SchemaStand.Zielversion);
        }


        // =====================================================================
        //  Das Protokoll
        // =====================================================================

        [Fact]
        public void Der_Lauf_meldet_Vorlage_und_Ziel()
        {
            string vorlage = NeueVorlage();
            string ziel = Zielpfad();
            var zeilen = new System.Collections.Generic.List<string>();

            Erstbereitstellung.Sicherstellen(ziel, vorlage, z => zeilen.Add(z));

            string alles = string.Join(Environment.NewLine, zeilen);
            Assert.Contains(vorlage, alles, StringComparison.Ordinal);
            Assert.Contains(ziel, alles, StringComparison.Ordinal);
        }

        [Fact]
        public void Ein_werfender_Protokollempfaenger_kippt_den_Ablauf_nicht()
        {
            string vorlage = NeueVorlage();
            string ziel = Zielpfad();

            Erstbereitstellungsergebnis erg = Erstbereitstellung.Sicherstellen(
                ziel, vorlage, z => throw new InvalidOperationException("Empfaenger streikt"));

            Assert.Equal(Erstbereitstellungslage.Kopiert, erg.Lage);
            Assert.True(File.Exists(ziel));
        }


        // =====================================================================
        //  Der Pfad zur Vorlage (IPfade.Auslieferungsvorlage)
        // =====================================================================

        /// <summary>
        /// Die Eigenschaft liefert IMMER einen Pfad — auch dann, wenn es die Datei nicht
        /// gibt. Nur so kann die Startmeldung sagen, wo gesucht wurde.
        /// </summary>
        [Fact]
        public void Der_Vorlagenpfad_ist_nie_leer()
        {
            string pfad = new StandardPfade().Auslieferungsvorlage;

            Assert.False(string.IsNullOrWhiteSpace(pfad));
            Assert.True(Path.IsPathRooted(pfad));
        }

        [Fact]
        public void Der_Vorlagenpfad_endet_auf_Vorlage_Kenndaten_sqlite()
        {
            string pfad = new StandardPfade().Auslieferungsvorlage;

            Assert.Equal("Kenndaten.sqlite", Path.GetFileName(pfad));
            Assert.Equal("Vorlage", Path.GetFileName(Path.GetDirectoryName(pfad)));
        }
    }
}
