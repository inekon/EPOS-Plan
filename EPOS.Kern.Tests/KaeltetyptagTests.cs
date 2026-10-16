using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KBP-4a — Schemaschritt <see cref="KaeltetyptagSchema"/>, <see cref="KaeltetyptagCtrl"/> und der Typtagzweig
    /// von <see cref="ProfilBedarf"/></b> auf einer frisch migrierten Kopie der Testdatenbank (Konzept
    /// Kältebedarfsprofile 8.2, 10): Park-Zusicherungen, Tabellen, Prüfklauseln, Kaskade, Eindeutigkeit,
    /// Wiederholbarkeit; Lesen und Schreiben; die Mitzieher Kopie bei Bearbeitung, Duplizieren und Projektpaket; der
    /// Projektlauf einer Zuordnung mit Eingabeart 'typtag'.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltetyptagTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string BUERO = "Raumkühlung Büro";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // ---- Schema ---------------------------------------------------------------------------------------------------

        [Fact]
        public void Nummer_geparkt_und_noch_nicht_im_Register()
        {
            // KETTE: geparkt auf 236; nach dem Umhaengen hinter 235 WasserpreisTraegerkarteSchema gelten statt der drei
            // Zeilen: SCHRITT == WasserpreisTraegerkarteSchema.SCHRITT + 1, Zielversion == SCHRITT, Stufe in der Paketanhebung.
            Assert.Equal(236, KaeltetyptagSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion < KaeltetyptagSchema.SCHRITT);
            Assert.DoesNotContain(Paketanhebung.Stufen, s => s.Nr == KaeltetyptagSchema.SCHRITT);
            Assert.Equal(2, KaeltetyptagSchema.Tabellen().Count);
        }

        [Fact]
        public void Tabellen_stehen_STRICT_leer_mit_Fremdschluesseln()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaeltetyptagSchema.Vollstaendig());
            foreach (string tab in new[] { KaeltetyptagSchema.TAB_TYPTAG, KaeltetyptagSchema.TAB_MATRIX })
            {
                string ddl = Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", tab);
                Assert.EndsWith("STRICT", ddl.TrimEnd());
                Assert.Contains("REFERENCES \"Tab_Kaeltebedarf\" (\"ID\") ON DELETE CASCADE", ddl);
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tab + "\""));
            }
            Assert.Contains("REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE",
                            Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", KaeltetyptagSchema.TAB_TYPTAG));
            Assert.Contains("REFERENCES \"Tab_Kaeltetyptag\" (\"ID\") ON DELETE CASCADE",
                            Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", KaeltetyptagSchema.TAB_MATRIX));
            Assert.False(DataRepository.TabelleVorhanden(KaeltetyptagSchema.TAB_TYPTAG + "_STAMM"));
        }

        [Fact]
        public void Pruefklauseln_und_Eindeutigkeit_weisen_ab()
        {
            if (!_db.Vorhanden) return;
            int kopie = KopieMitTyptagen();
            long idTag = Zahl("SELECT MIN(ID) FROM Tab_Kaeltetyptag WHERE ID_Kaeltebedarf = ?", kopie);
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag SET Nr = 7 WHERE ID = " + idTag));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag SET Nr = 0 WHERE ID = " + idTag));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag SET Nr = 2 WHERE ID = " + idTag));      // Nr doppelt
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag SET Tagesmenge = -0.1 WHERE ID = " + idTag));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag SET \"5\" = -1 WHERE ID = " + idTag));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag SET Bezeichner = NULL WHERE ID = " + idTag));
            long idZelle = Zahl("SELECT MIN(ID) FROM Tab_Kaeltetyptag_Matrix WHERE ID_Kaeltebedarf = ?", kopie);
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag_Matrix SET Monat = 13 WHERE ID = " + idZelle));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag_Matrix SET Tagesart = 'feiertag' WHERE ID = " + idZelle));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("INSERT INTO Tab_Kaeltetyptag_Matrix (ID_Kaeltebedarf, Monat, Tagesart, ID_Kaeltetyptag) " +
                                                         "SELECT ID_Kaeltebedarf, Monat, Tagesart, ID_Kaeltetyptag FROM Tab_Kaeltetyptag_Matrix WHERE ID = " + idZelle));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltetyptag_Matrix SET ID_Kaeltetyptag = 999999 WHERE ID = " + idZelle));
            // Die Vorgabe der Tagesmenge ist 1.
            Ausfuehren("INSERT INTO Tab_Kaeltetyptag (ID_Projekt, ID_Kaeltebedarf, Nr, Bezeichner) VALUES (" + PROJEKT + ", " + kopie + ", 6, 'sechs')");
            Assert.Equal(1L, Zahl("SELECT Tagesmenge FROM Tab_Kaeltetyptag WHERE ID_Kaeltebedarf = ? AND Nr = 6", kopie));
        }

        [Fact]
        public void Kaskade_vom_Typtag_von_der_Projektkopie_und_vom_Projekt()
        {
            if (!_db.Vorhanden) return;
            int kopie = KopieMitTyptagen();
            Assert.Equal(36L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag_Matrix WHERE ID_Kaeltebedarf = ?", kopie));
            // Typtag 3 weg: seine zwölf Sonntagszellen gehen mit, die Eingabe nennt sie leer.
            Ausfuehren("DELETE FROM Tab_Kaeltetyptag WHERE Nr = 3 AND ID_Kaeltebedarf = " + kopie);
            Assert.Equal(24L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag_Matrix WHERE ID_Kaeltebedarf = ?", kopie));
            Assert.Equal(0, KaeltetyptagCtrl.Lies(kopie).Matrix[0, 2]);
            // Die Projektkopie weg: alles weg.
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, 0));
            Ausfuehren("DELETE FROM Tab_Kaeltebedarf WHERE ID = " + kopie);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag WHERE ID_Kaeltebedarf = ?", kopie));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag_Matrix WHERE ID_Kaeltebedarf = ?", kopie));
            // Das Projekt weg: auch eine zweite Eingabe geht mit.
            int zweite = KopieMitTyptagen();
            Ausfuehren("DELETE FROM Tab_Projekt WHERE ID = " + PROJEKT);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag WHERE ID_Kaeltebedarf = ?", zweite));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag_Matrix WHERE ID_Kaeltebedarf = ?", zweite));
        }

        [Fact]
        public void Wiederholbar()
        {
            if (!_db.Vorhanden) return;
            var bericht = new List<string>();
            Assert.Equal(0, KaeltetyptagSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, b => b.Contains("stehen bereits", StringComparison.Ordinal));
        }

        // ---- Controller -----------------------------------------------------------------------------------------------

        [Fact]
        public void Schreiben_und_Lesen_ergeben_dieselbe_Eingabe()
        {
            if (!_db.Vorhanden) return;
            int kopie = KopieMitTyptagen();
            KaeltetyptagEingabe gelesen = KaeltetyptagCtrl.Lies(kopie);
            PruefeGleich(TyptagschichtTests.Drei(), gelesen);

            // Ein zweites Schreiben ersetzt, statt anzuhängen.
            KaeltetyptagEingabe neu = TyptagschichtTests.Drei();
            neu.Typtage.RemoveAt(2);
            for (int m = 0; m < 12; m++) neu.Matrix[m, 2] = 2;
            Assert.True(KaeltetyptagCtrl.Schreiben(PROJEKT, kopie, neu, out string grund), grund);
            PruefeGleich(neu, KaeltetyptagCtrl.Lies(kopie));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag WHERE ID_Kaeltebedarf = ?", kopie));

            // Benannt abgelehnt: Regelverstoß und fremdes Projekt.
            neu.Matrix[0, 0] = 4;
            Assert.False(KaeltetyptagCtrl.Schreiben(PROJEKT, kopie, neu, out grund));
            Assert.Contains("fehlenden Typtag 4", grund);
            Assert.False(KaeltetyptagCtrl.Schreiben(PROJEKT + 1, kopie, TyptagschichtTests.Drei(), out grund));
            Assert.Contains("gehoert nicht zum Projekt", grund);
            PruefeGleich(Ohne3(), KaeltetyptagCtrl.Lies(kopie));

            Assert.True(KaeltetyptagCtrl.Loeschen(kopie));
            Assert.True(KaeltetyptagCtrl.Lies(kopie).Leer);
        }

        [Fact]
        public void Die_Kopie_bei_Bearbeitung_nimmt_Typtage_und_Matrix_mit()
        {
            if (!_db.Vorhanden) return;
            int kopie = KopieMitTyptagen();
            // Ohne Klammer wie ausserhalb des Assistenten; im Assistenten laeuft derselbe Weg in dessen Vorgang
            // (Vorgangsklammer), und KaeltetyptagCtrl.Schreiben wird dort ein Unterpunkt.
            int eigene = KaeltebedarfStammCtrl.ProjektkopieAbspalten(kopie, PROJEKT);
            Assert.True(eigene > 0 && eigene != kopie);
            PruefeGleich(TyptagschichtTests.Drei(), KaeltetyptagCtrl.Lies(eigene));
            PruefeGleich(TyptagschichtTests.Drei(), KaeltetyptagCtrl.Lies(kopie));
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag WHERE ID_Projekt = ?", PROJEKT));

            // Derselbe Weg in der Klammer des Assistenten: Schreiben wird Unterpunkt des laufenden Vorgangs.
            int dritte;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                    dritte = KaeltebedarfStammCtrl.ProjektkopieAbspalten(kopie, PROJEKT);
                v.Commit();
            }
            Assert.True(dritte > 0 && dritte != eigene);
            PruefeGleich(TyptagschichtTests.Drei(), KaeltetyptagCtrl.Lies(dritte));
        }

        [Fact]
        public void Das_Duplizieren_traegt_Typtage_und_Matrix()
        {
            if (!_db.Vorhanden) return;
            KopieMitTyptagen();
            int ziel = new ProjektDuplizierenCtrl().Duplizieren(Projektname(PROJEKT), "KBP-4a Kopie");
            Assert.True(ziel > 0, "Duplizieren fehlgeschlagen.");
            PruefeZiel(ziel);
        }

        [Fact]
        public void Das_Projektpaket_traegt_Typtage_und_Matrix()
        {
            if (!_db.Vorhanden) return;
            KopieMitTyptagen();
            string ordner = Path.Combine(Path.GetTempPath(), "KBP4a_Paket_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "kbp4a.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Projektname(PROJEKT), paket));
                int neu = io.Importieren(paket, "KBP-4a Paket", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeZiel(neu);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }

        // ---- Rechenweg ------------------------------------------------------------------------------------------------

        [Fact]
        public void Der_Projektlauf_rechnet_Typtage_auf_die_Jahressumme()
        {
            if (!_db.Vorhanden) return;
            int kopie = KopieMitTyptagen();
            double[] jahr = Rechnen();
            Assert.Equal(12000.0, jahr.Sum(), 6);                                     // 'jahr': die Jahressumme 12 MWh

            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_Projekt_Kaeltebedarf SET Eingabeart = ? WHERE ID_Projekt = ?",
                                                  new DbParam("@e", DbWerte.KAELTE_EINGABE_TYPTAG), new DbParam("@p", PROJEKT)));
            double[] typtag = Rechnen();
            Assert.Equal(12000.0, typtag.Sum(), 6);
            Assert.NotEqual(jahr, typtag);

            // Dieselbe Reihe wie die Schicht allein - mit dem Raster und den Monatsgrenzen des Laufs.
            (int[] a, int[] _) = Monate();
            var erwartet = new double[8760];
            Typtagschicht.TyptageZuJahr(TyptagschichtTests.Drei(), 12.0, erwartet, a,
                                        Konditionierungdatenweg.Raster(PROJEKT, ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion())).W0,
                                        KalenderArten(kopie, out double f, out bool kuerzen), f, kuerzen);
            Assert.Equal(erwartet, typtag);

            // Ohne Typtage: Warnung, Anteil 0 - nie still die Woche.
            Assert.True(KaeltetyptagCtrl.Loeschen(kopie));
            Assert.Equal(0.0, Rechnen().Sum());
        }

        [Fact]
        public void Die_Vorschau_rechnet_die_Typtage_eines_Arbeitsstands()
        {
            if (!_db.Vorhanden) return;
            int kopie = KopieMitTyptagen();
            string name = Text("SELECT Bezeichner FROM Tab_Kaeltebedarf WHERE ID = ?", kopie);
            KaeltetyptagEingabe arbeit = TyptagschichtTests.Drei();
            arbeit.Typtag(1).Tagesmenge = 5;
            ProfilQuelle quelle = ProfilQuelle.Kaelte(ProfilQuellmodus.Projektvorschau);
            quelle.Jahressummen = new Dictionary<string, double> { [name] = 7.0 };
            quelle.Arbeitsstaende = new Dictionary<string, ProfilArbeitsstand>
            {
                [name] = new ProfilArbeitsstand { Eingabeart = DbWerte.KAELTE_EINGABE_TYPTAG, Typtage = arbeit }
            };
            (int[] a, int[] e) = Monate();
            var ziel = new double[8760];
            ProfilBedarf.Rechnen(quelle, PROJEKT, new List<string> { name },
                                 ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion()), a, e, ziel);
            Assert.Equal(7000.0, ziel.Sum(), 6);
            var gespeichert = new double[8760];
            quelle.Arbeitsstaende = new Dictionary<string, ProfilArbeitsstand>
            {
                [name] = new ProfilArbeitsstand { Eingabeart = DbWerte.KAELTE_EINGABE_TYPTAG }
            };
            ProfilBedarf.Rechnen(quelle, PROJEKT, new List<string> { name },
                                 ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion()), a, e, gespeichert);
            Assert.Equal(7000.0, gespeichert.Sum(), 6);
            Assert.NotEqual(gespeichert, ziel);                                       // der Arbeitsstand wirkt
        }

        // ---- Helfer ---------------------------------------------------------------------------------------------------

        /// <summary>Eine Zuordnung „Büro“ mit 12 MWh, deren Projektkopie die drei Typtage trägt; Rückgabe = ID der Kopie.</summary>
        private static int KopieMitTyptagen()
        {
            int stamm = (int)Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", BUERO);
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = stamm, Bezeichner = BUERO, Summe = 12 }
            }));
            int kopie = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single().ID_Kaeltebedarf;
            Assert.True(KaeltetyptagCtrl.Schreiben(PROJEKT, kopie, TyptagschichtTests.Drei(), out string grund), grund);
            return kopie;
        }

        private static KaeltetyptagEingabe Ohne3()
        {
            KaeltetyptagEingabe e = TyptagschichtTests.Drei();
            e.Typtage.RemoveAt(2);
            for (int m = 0; m < 12; m++) e.Matrix[m, 2] = 2;
            return e;
        }

        private static void PruefeZiel(int ziel)
        {
            Assert.NotEqual(PROJEKT, ziel);
            int kopie = Z_ProjektKaeltebedarfCtrl.LiesProjekt(ziel).Single().ID_Kaeltebedarf;
            PruefeGleich(TyptagschichtTests.Drei(), KaeltetyptagCtrl.Lies(kopie));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag WHERE ID_Projekt = ?", ziel));
            // Jede Zelle zeigt auf einen Typtag DERSELBEN Projektkopie.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag_Matrix m JOIN Tab_Kaeltetyptag t ON t.ID = m.ID_Kaeltetyptag " +
                                  "WHERE m.ID_Kaeltebedarf = ? AND t.ID_Kaeltebedarf <> m.ID_Kaeltebedarf", kopie));
            Assert.Equal(36L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyptag_Matrix WHERE ID_Kaeltebedarf = ?", kopie));
            PruefeGleich(TyptagschichtTests.Drei(), KaeltetyptagCtrl.Lies(
                Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single().ID_Kaeltebedarf));      // die Quelle unberührt
        }

        private static void PruefeGleich(KaeltetyptagEingabe soll, KaeltetyptagEingabe ist)
        {
            Assert.Equal(soll.Typtage.Count, ist.Typtage.Count);
            foreach (Kaeltetyptag s in soll.Typtage)
            {
                Kaeltetyptag i = ist.Typtag(s.Nr);
                Assert.NotNull(i);
                Assert.Equal(s.Bezeichner, i.Bezeichner);
                Assert.Equal(s.Tagesmenge, i.Tagesmenge);
                Assert.Equal(s.Stunden, i.Stunden);
            }
            Assert.Equal(soll.Matrix, ist.Matrix);
        }

        private static double[] Rechnen()
        {
            (int[] a, int[] e) = Monate();
            var ziel = new double[8760];
            ProfilBedarf.Rechnen(ProfilQuelle.Kaelte(ProfilQuellmodus.Projektrechnung), PROJEKT, null,
                                 ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion()), a, e, ziel);
            return ziel;
        }

        /// <summary>Die Tagesarten des Kalenders der Zuordnung (oder <c>null</c>), wie der Lauf sie bildet.</summary>
        private static byte[] KalenderArten(int kopie, out double ferienfaktor, out bool kuerzen)
        {
            ferienfaktor = 1.0;
            kuerzen = false;
            long id = Zahl("SELECT ID_Betriebskalender FROM Z_Projekt_Kaeltebedarf WHERE ID_Kaeltebedarf = ?", kopie);
            if (id <= 0) return null;
            Betriebskalender k = BetriebskalenderCtrl.Lies((int)id);
            ferienfaktor = k.Ferienfaktor;
            kuerzen = k.FerienKuerzen;
            return k.Tagesarten(Konditionierungdatenweg.Raster(PROJEKT, ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion())));
        }

        private static (int[] anfang, int[] ende) Monate()
        {
            var a = new int[12];
            var e = new int[12];
            int h = 0;
            for (int m = 0; m < 12; m++)
            {
                a[m] = h;
                h += Feiertage.TageJeMonat[m] * 24;
                e[m] = h - 1;
            }
            return (a, e);
        }

        private static int Klimaregion() => (int)Zahl("SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?", PROJEKT);

        private static string Projektname(int id) => Text("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", id);

        private static void Ausfuehren(string sql)
        {
            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren(sql);
            v.Commit();
        }

        private static string Text(string sql, object p)
            => Convert.ToString(DataRepository.ExecuteScalar(sql, new DbParam("@p", p)), CultureInfo.InvariantCulture);

        private static long Zahl(string sql, object p = null)
        {
            object o = p == null ? DataRepository.ExecuteScalar(sql) : DataRepository.ExecuteScalar(sql, new DbParam("@p", p));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
