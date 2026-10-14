using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>DA-6 — Saat der DIN-Nutzungsprofile</b> (<see cref="RaumnutzungDinSaat"/>, <see cref="RaumnutzungDinWerteSchema"/>;
    /// Entscheid E135): die eingebettete Tabelle liest sich vollständig und trägt ihre Prüfsumme, die Migration sät die 45
    /// Profile nur, wo kein Wert steht, wiederholbar, und die Rücknahme der Normwerte stellt die Saat wieder her. Verglichen
    /// wird immer gegen die Konstante, nie gegen Werte der Norm im Test.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungDinWerteSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        private static readonly string WERTE_GESETZT =
            string.Join(" OR ", RaumnutzungDinWerteSchema.Wertspalten.Select(s => "\"" + s + "\" IS NOT NULL"));

        private static long DinKategorie() => Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Art = 'DIN_V_18599_10'");

        /// <summary>Alle Spalten der DIN-Profile als Text — für den Vergleich vor und nach einem Lauf.</summary>
        private static string Bild()
        {
            DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? ORDER BY ID",
                                                      new DbParam("@k", DinKategorie()));
            return string.Join("\n", t.Rows.Cast<DataRow>().Select(r => string.Join("|", r.ItemArray.Select(
                o => Convert.ToString(o, CultureInfo.InvariantCulture)))));
        }

        // =============================================================================
        //  Teil 1 - die eingebettete Tabelle
        // =============================================================================

        [Fact]
        public void Die_Tabelle_liest_45_Profile_ohne_Ablehnung_und_traegt_ihre_Pruefsumme()
        {
            RaumnutzungCsvLesung lesung = RaumnutzungCsv.Lesen(RaumnutzungDinSaat.TABELLE);
            Assert.Null(lesung.Abbruch);
            Assert.Equal(RaumnutzungDinSaat.ANZAHL, lesung.Zeilen.Count);
            Assert.All(lesung.Zeilen, z => Assert.True(z.Uebernehmbar, z.Zeile + ": " + (z.Ablehnung ?? z.Zielablehnung)));
            Assert.Equal(RaumnutzungDinSaat.ANZAHL, RaumnutzungDinSaat.Profile.Count);

            Assert.Equal(RaumnutzungDinSaat.PRUEFSUMME, RaumnutzungDinSaat.Pruefsumme(RaumnutzungDinSaat.TABELLE));
            // Der Zeilenumbruch der Quelltextdatei wirkt nicht (Checkout unter Windows mit CRLF).
            Assert.Equal(RaumnutzungDinSaat.PRUEFSUMME,
                         RaumnutzungDinSaat.Pruefsumme(RaumnutzungDinSaat.TABELLE.Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n"));
            // Gegenprobe: ein geändertes Zeichen ändert die Summe.
            Assert.NotEqual(RaumnutzungDinSaat.PRUEFSUMME, RaumnutzungDinSaat.Pruefsumme(RaumnutzungDinSaat.TABELLE.Insert(RaumnutzungDinSaat.TABELLE.IndexOf('\n') + 1, "x")));
        }

        [Fact]
        public void Die_Tabelle_ordnet_43_Nummern_und_die_zwei_Wohnprofile_zu()
        {
            IReadOnlyList<Raumnutzungsprofil> p = RaumnutzungDinSaat.Profile;
            List<string> nummern = p.Where(x => x.Nummer != null).Select(x => RaumnutzungNormwerte.Nummernschluessel(x.Nummer)).ToList();
            Assert.Equal(43, nummern.Distinct().Count());
            Assert.Equal(Enumerable.Range(1, 43).Select(i => i.ToString(CultureInfo.InvariantCulture)), nummern.OrderBy(int.Parse));
            Assert.Equal(RaumnutzungNormkategorienSchema.WOHNPROFILE.OrderBy(n => n), p.Where(x => x.Nummer == null).Select(x => x.Bezeichner).OrderBy(n => n));
            Assert.Same(p.First(x => x.Nummer == "1"), RaumnutzungDinSaat.Zu("01", "beliebig"));
            Assert.NotNull(RaumnutzungDinSaat.Zu(null, RaumnutzungNormkategorienSchema.WOHNEN_MFH.ToUpperInvariant()));
            // Gegenprobe: unbekannte Nummer, fremder Name ohne Nummer.
            Assert.Null(RaumnutzungDinSaat.Zu("44", null));
            Assert.Null(RaumnutzungDinSaat.Zu(null, "Einzelbüro"));
        }

        [Fact]
        public void Die_Tabelle_traegt_je_Spaltenart_was_die_Norm_hergibt()
        {
            foreach (Raumnutzungsprofil p in RaumnutzungDinSaat.Profile)
            {
                bool wohnen = p.Nummer == null;
                Assert.Equal(p.Nutzung_Von.HasValue, p.Nutzung_Bis.HasValue);
                Assert.True(p.Heiz_Soll.HasValue, p.ToString());
                Assert.True(p.Aussenluft.HasValue, p.ToString());
                Assert.Equal(wohnen ? RaumnutzungSchema.EINHEIT_JE_STUNDE : RaumnutzungSchema.EINHEIT_JE_FLAECHE, p.Aussenluft_Einheit);
                Assert.Equal(wohnen, p.Betrieb_Von.HasValue && p.Betrieb_Bis.HasValue);
                Assert.Equal(wohnen, p.Heiz_Aus_Ausserhalb.HasValue);
                Assert.True(p.Nutzungstage_Woche == null || p.Nutzungstage_Woche == "1111111", p.ToString());
                Assert.Null(p.Aussenluft_Person);                              // die Fassung 2025-10 führt keine
                Assert.Null(p.Feiertage_Wie_Sonntag);
                Assert.True(new object[] { p.Personen_Flaeche, p.Personen_Waerme, p.Personen_Anteil, p.Geraete_Leistung,
                                           p.Beleuchtung_Leistung }.All(w => w == null), p.ToString());
                Assert.StartsWith("DIN/TS 18599-10:" + RaumnutzungDinSaat.FASSUNG, p.Beschreibung, StringComparison.Ordinal);
                Assert.InRange(p.Beschreibung.Length, 1, RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN);
                Assert.Null(RaumnutzungCtrl.Profilpruefung(p.Kopie()));
            }
            Assert.Contains(RaumnutzungDinSaat.Profile, p => p.Heiz_Aus_Ausserhalb == true);
            Assert.Contains(RaumnutzungDinSaat.Profile, p => p.Heiz_Soll_Ausserhalb.HasValue);   // aus der Absenkung
            Assert.Contains(RaumnutzungDinSaat.Profile, p => p.Kuehl_Soll.HasValue);
            Assert.Contains(RaumnutzungDinSaat.Profile, p => p.Nutzung_Von.HasValue);
            Assert.Contains(RaumnutzungDinSaat.Profile, p => p.Nutzungstage_Woche == null);
            // Gegenprobe: eine Zeile ohne Bezeichner lehnt der Leser ab.
            string[] zeilen = RaumnutzungDinSaat.TABELLE.Replace("\r\n", "\n").Split('\n');
            string[] felder = zeilen[1].Split(';');
            felder[1] = "";
            zeilen[1] = string.Join(";", felder);
            string kaputt = string.Join("\n", zeilen);
            Assert.Equal(RaumnutzungDinSaat.ANZAHL - 1, RaumnutzungCsv.Lesen(kaputt).Uebernehmbare.Count());
        }

        // =============================================================================
        //  Teil 2 - der Schritt an der Testdatenbank
        // =============================================================================

        [Fact]
        public void Der_Schritt_haengt_an_der_Kette_und_steht_im_Register()
        {
            Assert.Equal(BauteilkatalogAbschnittSchema.SCHRITT + 1, RaumnutzungDinWerteSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= RaumnutzungDinWerteSchema.SCHRITT);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == RaumnutzungDinWerteSchema.SCHRITT);
        }

        [Fact]
        public void Nach_der_Migration_tragen_die_45_DIN_Profile_die_Werte_der_Saat()
        {
            if (!_db.Vorhanden) return;
            long k = DinKategorie();
            Assert.Equal((long)RaumnutzungDinSaat.ANZAHL, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? AND (" +
                                                               WERTE_GESETZT + ")", new DbParam("@k", k)));
            Assert.Equal(0, RaumnutzungDinWerteSchema.Offen());
            Assert.True(RaumnutzungDinWerteSchema.Vollstaendig());
            Assert.Empty(RaumnutzungDinWerteSchema.Abweichungen());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? AND Werte_Herkunft IS NOT NULL", new DbParam("@k", k)));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszeile z JOIN Tab_Raumnutzungsprofil p ON p.ID = z.ID_Profil WHERE p.ID_Katalog = " + k));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsstunden z JOIN Tab_Raumnutzungsprofil p ON p.ID = z.ID_Profil WHERE p.ID_Katalog = " + k));
            Assert.Equal(RaumnutzungSaat.QUELLE_DIN, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Quellenhinweis FROM Tab_Raumnutzungskatalog WHERE ID = ?", new DbParam("@k", k)), CultureInfo.InvariantCulture));
            Assert.Contains(RaumnutzungDinSaat.FASSUNG, RaumnutzungSaat.QUELLE_DIN, StringComparison.Ordinal);
            // Kein Referenzprojekt nutzt ein Nutzungsprofil.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE COALESCE(Nutzungsprofil, '') <> ''"));

            // Stichproben je Spaltenart über den Leser des Kerns gegen die Konstante.
            List<Raumnutzungsprofil> din = RaumnutzungNormwerte.DinProfile();
            foreach (Raumnutzungsprofil ist in new[]
                     {
                         din.Single(p => p.Nummer == "1"), din.Single(p => p.Bezeichner == RaumnutzungNormkategorienSchema.WOHNEN_EFH),
                         din.Single(p => p.Bezeichner == RaumnutzungNormkategorienSchema.WOHNEN_MFH),
                         din.FirstOrDefault(p => p.Nummer != null && RaumnutzungDinSaat.Zu(p.Nummer, null).Nutzungstage_Woche != null),
                     }.Where(p => p != null))
            {
                Raumnutzungsprofil soll = RaumnutzungDinSaat.Zu(ist.Nummer, ist.Bezeichner);
                Assert.Equal((soll.Nutzung_Von, soll.Nutzung_Bis, soll.Betrieb_Von, soll.Betrieb_Bis, soll.Nutzungstage_Woche),
                             (ist.Nutzung_Von, ist.Nutzung_Bis, ist.Betrieb_Von, ist.Betrieb_Bis, ist.Nutzungstage_Woche));
                Assert.Equal((soll.Heiz_Soll, soll.Heiz_Soll_Ausserhalb, soll.Heiz_Aus_Ausserhalb, soll.Kuehl_Soll),
                             (ist.Heiz_Soll, ist.Heiz_Soll_Ausserhalb, ist.Heiz_Aus_Ausserhalb, ist.Kuehl_Soll));
                Assert.Equal((soll.Aussenluft, soll.Aussenluft_Einheit, soll.Aussenluft_Person), (ist.Aussenluft, ist.Aussenluft_Einheit, ist.Aussenluft_Person));
                Assert.Equal(soll.Beschreibung, ist.Beschreibung);
                Assert.True(ist.Ausgeliefert && ist.Werte_Herkunft == null);
            }
        }

        [Fact]
        public void Ein_zweiter_Lauf_aendert_nichts()
        {
            if (!_db.Vorhanden) return;
            string vorher = Bild();
            var bericht = new List<string>();
            Assert.Equal(0, RaumnutzungDinWerteSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
            Assert.Equal(vorher, Bild());
        }

        [Fact]
        public void Der_Schritt_saet_nur_leere_Profile_und_ein_vorhandener_Wert_bleibt()
        {
            if (!_db.Vorhanden) return;
            long k = DinKategorie();
            // Stand vor dem Schritt: alle DIN-Profile leer, die Texte vor der Saat - nur Profil 2 trägt einen eigenen Wert.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungsprofil SET " +
                string.Join(", ", RaumnutzungDinWerteSchema.Wertspalten.Select(s => "\"" + s + "\" = NULL")) + ", Beschreibung = NULL WHERE ID_Katalog = ?",
                new DbParam("@k", k));
            DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungsprofil SET Heiz_Soll = 12 WHERE ID_Katalog = ? AND Nummer = '2'", new DbParam("@k", k));
            DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungskatalog SET Quellenhinweis = ?, Beschreibung = ? WHERE ID = ?",
                new DbParam("@q", RaumnutzungDinSaat.QUELLENHINWEIS_VOR_SAAT), new DbParam("@b", RaumnutzungDinSaat.BESCHREIBUNG_VOR_SAAT), new DbParam("@k", k));
            Assert.Equal(RaumnutzungDinSaat.ANZAHL - 1 + 1, RaumnutzungDinWerteSchema.Offen());
            Assert.Equal(RaumnutzungDinSaat.ANZAHL, RaumnutzungDinWerteSchema.Abweichungen().Count);

            Assert.Equal(RaumnutzungDinSaat.ANZAHL - 1, RaumnutzungDinWerteSchema.Ausfuehren(null));
            Assert.True(RaumnutzungDinWerteSchema.Vollstaendig());
            Raumnutzungsprofil zwei = RaumnutzungNormwerte.DinProfile().Single(p => p.Nummer == "2");
            Assert.Equal(12.0, zwei.Heiz_Soll);
            Assert.Null(zwei.Kuehl_Soll);
            Assert.Null(zwei.Beschreibung);
            Assert.Equal(new[] { "2 " + zwei.Bezeichner }, RaumnutzungDinWerteSchema.Abweichungen());
            Assert.Equal(RaumnutzungSaat.QUELLE_DIN, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Quellenhinweis FROM Tab_Raumnutzungskatalog WHERE ID = ?", new DbParam("@k", k)), CultureInfo.InvariantCulture));
            Assert.Equal(RaumnutzungSaat.BESCHREIBUNG_DIN, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Beschreibung FROM Tab_Raumnutzungskatalog WHERE ID = ?", new DbParam("@k", k)), CultureInfo.InvariantCulture));
            Assert.True(RaumnutzungDinTsSchema.Vollstaendig());
            Assert.Equal(0, RaumnutzungDinWerteSchema.Ausfuehren(null));
        }

        [Fact]
        public void Die_Ruecknahme_der_Normwerte_stellt_die_Saat_wieder_her()
        {
            if (!_db.Vorhanden) return;
            string vorher = Bild();
            Raumnutzungsprofil eins = RaumnutzungDinSaat.Zu("1", null).Kopie();
            eins.Heiz_Soll = eins.Heiz_Soll.Value - 1.0;
            eins.Aussenluft_Person = 30.0;
            RaumnutzungNormwerte.Vorschau v = RaumnutzungNormwerte.AusCsv(RaumnutzungCsv.SchreibenBytes(new[] { eins }), "probe.csv");
            Assert.True(v.Ok, v.Meldung);
            Assert.Equal(1, RaumnutzungNormwerte.Einlesen(v, true, new DateTime(2026, 10, 14)).Geschrieben);
            Assert.NotEqual(vorher, Bild());

            RaumnutzungNormwerte.Bilanz b = RaumnutzungNormwerte.Entfernen();
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(1, b.Geschrieben);
            Assert.Equal(vorher, Bild());
        }
    }
}
