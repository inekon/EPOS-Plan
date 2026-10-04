using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein Ort je Zelle</b> (Stufe KP1b, Konzept Konditionierungsprofile 5.6, Befund NB9): Die
    /// Karte <see cref="Matrixzellenort"/> sagt je Eigentümerart, Größe und Zeile, ob die Zelle
    /// eine Bestandsspalte hat. Die Fälle schreiben jede Zelle über
    /// <see cref="KonditionierungCtrl.Vorgabe"/> und prüfen zweierlei: dass der Wert an GENAU
    /// einem Ort steht und dass die <see cref="Vorgabematrix"/> ihn dort wieder herausliest.
    ///
    /// <para>Alle drei Tabellen sind im Bestand leer — die Fälle schreiben ausschließlich in eine
    /// Arbeitskopie (<see cref="TestDatenbank"/>) und lassen den Referenzlauf unberührt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class MatrixzellenortTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungCtrl _ctrl = new KonditionierungCtrl();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Die Karte selbst — ohne Datenbank
        // =============================================================================

        [Fact]
        public void Die_Karte_nennt_neun_Bestandsspalten_und_an_der_Vorlage_keine()
        {
            var gefunden = new List<string>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                foreach (string z in DbWerte.KOND_ZEILEN)
                {
                    string spalte = Matrixzellenort.Bestandsspalte(g, z);
                    if (spalte != null) gefunden.Add(Konditionierungsgroessen.Kennwort(g) + "|" + z + "=" + spalte);

                    // Gebäude, Zone und Katalogbau führen dieselben Spalten in ihrer eigenen Tabelle.
                    foreach (Kalendereigentuemer art in new[]
                             {
                                 Kalendereigentuemer.Gebaeude, Kalendereigentuemer.Zone,
                                 Kalendereigentuemer.Katalogbau
                             })
                    {
                        Matrixzellenort.Ort ort = Matrixzellenort.Fuer(art, g, z);
                        Assert.Equal(spalte != null, ort.IstBestandsspalte);
                        if (spalte != null)
                        {
                            Assert.Equal(spalte, ort.Spalte);
                            Assert.Equal(Matrixzellenort.Tabelle(art), ort.Tabelle);
                        }
                    }

                    // Eine Vorlage trägt jede Zelle in der Vorgabetabelle (Konzept 5.7).
                    Assert.False(Matrixzellenort.HatBestandsspalte(Kalendereigentuemer.Vorlage, g, z));
                }

            Assert.Equal(9, gefunden.Count);
            Assert.Null(Matrixzellenort.Tabelle(Kalendereigentuemer.Vorlage));

            // Die Saison und die ganze Personenspalte sind neu — nirgends eine Bestandsspalte.
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                Assert.Null(Matrixzellenort.Bestandsspalte(g, DbWerte.KOND_ZEILE_SAISON));
            foreach (string z in DbWerte.KOND_ZEILEN)
                Assert.Null(Matrixzellenort.Bestandsspalte(Konditionierungsgroesse.Personen, z));
        }

        [Fact]
        public void Die_drei_Tabellen_sind_im_Bestand_leer_die_Weiche_bleibt_byte_neutral()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;

            // Die Weiche ändert den Leser nur dort, wo eine Vorgabezeile steht. Solange die drei
            // Tabellen keine Zeile eines Gebäudes, einer Zone oder eines Katalogbaus tragen, rechnet
            // jeder Referenzlauf wörtlich den Bestandszweig — der Nachweis der Byte-Gleichheit steht
            // damit in der Datenbank, nicht in einer Meinung. Die Saat der ausgelieferten Vorlagen
            // (Schritt 157) zählt nicht: Vorlagenzeilen tragen kein ID_Gebaeude, und der Lauf liest
            // allein über ID_Gebaeude.
            foreach (string sql in new[]
                     {
                         // Ausgenommen das Zonenprojekt 1052 (G6d): seine zwei Zonenkalender stehen ausserhalb der Basis;
                         // ebenso die Konditionierung des Referenzprojekts 1051 und seines Referenzkatalogbaus (KP3, RP1).
                         "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Vorlage\" IS NULL AND " + Zonenbestand.NICHT_1052 + " AND " + Konditionierungsbestand.NICHT_1051,
                         "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Vorlage\" IS NULL AND " + Zonenbestand.NICHT_1052 + " AND " + Konditionierungsbestand.NICHT_1051,
                         "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" NOT IN " +
                         "(SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Vorlage\" IS NOT NULL) " +
                         "AND \"ID_Kalender\" NOT IN (" + Zonenbestand.KALENDER_1052 + ") AND \"ID_Kalender\" NOT IN (" + Konditionierungsbestand.KALENDER_1051 + ")",
                     })
            {
                object o = DataRepository.ExecuteScalar(sql);
                Assert.Equal(0L, Convert.ToInt64(o, CultureInfo.InvariantCulture));
            }
        }

        // =============================================================================
        //  Schreiben und Lesen: jede Größe × jede Zeile × drei Eigentümer
        // =============================================================================

        [Fact]
        public void Jede_Zelle_landet_an_ihrem_einen_Ort_und_kommt_dort_wieder_heraus()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;

            long idGebaeude = EinGebaeude();
            long idStamm = EinFreierKatalogbau();
            if (idGebaeude == 0 || idStamm == 0) return;
            long idZone = ZoneAnlegen(idGebaeude);

            var eigner = new[]
            {
                KonditionierungCtrl.Eigner.Gebaeude(idGebaeude),
                KonditionierungCtrl.Eigner.Zone(idGebaeude, idZone),
                KonditionierungCtrl.Eigner.Katalogbau(idStamm),
            };

            foreach (KonditionierungCtrl.Eigner e in eigner)
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                    foreach (string zeile in DbWerte.KOND_ZEILEN)
                    {
                        // Die Saisonzeile trägt keinen Wert, nur Tage (E53) — sie wird eigens geprüft.
                        if (string.Equals(zeile, DbWerte.KOND_ZEILE_SAISON, StringComparison.Ordinal)) continue;

                        double wert = Probewert(g, zeile);
                        string wer = e.Art + "/" + Konditionierungsgroessen.Kennwort(g) + "/" + zeile;

                        KonditionierungCtrl.Ergebnis erg =
                            _ctrl.Vorgabe(e, g, zeile, Matrixzelle.AusWert(wert));
                        Assert.True(erg.Ok, wer + ": " + erg.Meldung);

                        Matrixzellenort.Ort ort = Matrixzellenort.Fuer(e.Art, g, zeile);
                        if (ort.IstBestandsspalte)
                        {
                            Assert.Equal(wert, Spaltenwert(ort, Traeger(e)) ?? double.NaN, 9);
                            // KEIN zweiter Ort: Die Vorgabezeile trägt für diese Zelle keinen Wert.
                            Assert.Null(Vorgabewert(e, g, zeile));
                        }
                        else
                        {
                            Assert.Equal(wert, Vorgabewert(e, g, zeile) ?? double.NaN, 9);
                        }

                        // Der Leser folgt derselben Karte.
                        Vorgabematrix m = Vorgabematrix.Bilden(Bestand(e), _ctrl.Vorgaben(e), e.Art);
                        Matrixzelle zelle = m.Spalte(g).Zeile(zeile);
                        Assert.True(zelle.Belegt, wer + ": die Zelle ist nicht belegt.");
                        Assert.False(zelle.Aus, wer);
                        Assert.Equal(wert, zelle.Wert, 9);
                    }
        }

        [Fact]
        public void Ein_aus_bleibt_in_der_Vorgabezeile_und_schlaegt_die_Bestandsspalte()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;

            long idGebaeude = EinGebaeude();
            if (idGebaeude == 0) return;
            KonditionierungCtrl.Eigner e = KonditionierungCtrl.Eigner.Gebaeude(idGebaeude);

            // Erst ein Wert: Er steht in der Bestandsspalte.
            Assert.True(_ctrl.Vorgabe(e, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG,
                                      Matrixzelle.AusWert(21.0)).Ok);
            Matrixzellenort.Ort ort = Matrixzellenort.Fuer(e.Art, Konditionierungsgroesse.Heizsoll,
                                                           DbWerte.KOND_ZEILE_TAG);
            Assert.Equal(21.0, Spaltenwert(ort, idGebaeude) ?? double.NaN, 9);

            // Dann „aus": Die Bestandsspalte bleibt, die Vorgabezeile trägt das „aus" (Konzept 5.6).
            Assert.True(_ctrl.Vorgabe(e, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG,
                                      Matrixzelle.Abgeschaltet()).Ok);
            Assert.Equal(21.0, Spaltenwert(ort, idGebaeude) ?? double.NaN, 9);

            Vorgabematrix m = Vorgabematrix.Bilden(Bestand(e), _ctrl.Vorgaben(e), e.Art);
            Assert.True(m.Heizsoll.Tag.Belegt);
            Assert.True(m.Heizsoll.Tag.Aus);
        }

        [Fact]
        public void An_einer_Vorlage_traegt_die_Vorgabezeile_jede_Zelle()
        {
            // Ohne Datenbank: Eine Vorlage hat keine Bestandsspalten, also gilt ihr Wert.
            var vorgaben = new List<Vorgabezeile>
            {
                new Vorgabezeile
                {
                    IdVorlage = 7, Groesse = DbWerte.KOND_GROESSE_HEIZSOLL,
                    Zeile = DbWerte.KOND_ZEILE_TAG, Wert = 19.5,
                },
            };
            var leer = new Matrixeingang();

            Vorgabematrix alsVorlage = Vorgabematrix.Bilden(leer, vorgaben, Kalendereigentuemer.Vorlage);
            Assert.True(alsVorlage.Heizsoll.Tag.Belegt);
            Assert.Equal(19.5, alsVorlage.Heizsoll.Tag.Wert, 9);

            // Dieselbe Zeile an einem Gebäude: Die Bestandsspalte gilt — sie ist hier leer.
            Vorgabematrix alsGebaeude = Vorgabematrix.Bilden(leer, vorgaben, Kalendereigentuemer.Gebaeude);
            Assert.False(alsGebaeude.Heizsoll.Tag.Belegt);
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        private static double Probewert(Konditionierungsgroesse g, string zeile)
        {
            if (string.Equals(zeile, DbWerte.KOND_ZEILE_NENNWERT, StringComparison.Ordinal)
                && Konditionierungsgroessen.HatNennwert(g))
                return g == Konditionierungsgroesse.Geraete ? 500.0 : 700.0;    // Watt

            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll: return 19.0;
                case Konditionierungsgroesse.Kuehlsoll: return 26.0;
                case Konditionierungsgroesse.Lueftung: return 0.4;
                default: return 0.6;                                            // Anteil 0 … 1
            }
        }

        private static long EinGebaeude()
        {
            object o = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM \"Tab_Gebaeude\"");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Ein Katalogbau OHNE Schloss — ein gesperrter lehnt jeden Schreibweg ab.</summary>
        private static long EinFreierKatalogbau()
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Eine Zone für den Fall — die Testdatenbank führt keine.</summary>
        private static long ZoneAnlegen(long idGebaeude)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\") " +
                "VALUES (?, ?, ?, 1)",
                new DbParam("@g", idGebaeude), new DbParam("@r", 1), new DbParam("@b", "Zellenortprobe"));
            object o = DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static long Traeger(KonditionierungCtrl.Eigner e)
        {
            switch (e.Art)
            {
                case Kalendereigentuemer.Gebaeude: return e.IdGebaeude;
                case Kalendereigentuemer.Zone: return e.IdZone.Value;
                case Kalendereigentuemer.Katalogbau: return e.IdStamm;
                default: return e.IdVorlage;
            }
        }

        private static double? Spaltenwert(Matrixzellenort.Ort ort, long id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT \"" + ort.Spalte + "\" FROM \"" + ort.Tabelle + "\" WHERE \"ID\" = ?",
                new DbParam("@id", id));
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        private double? Vorgabewert(KonditionierungCtrl.Eigner e, Konditionierungsgroesse g, string zeile)
        {
            foreach (Vorgabezeile z in _ctrl.Vorgaben(e))
                if (string.Equals(z.Groesse, Konditionierungsgroessen.Kennwort(g), StringComparison.Ordinal)
                    && string.Equals(z.Zeile, zeile, StringComparison.Ordinal))
                    return z.Wert;
            return null;
        }

        /// <summary>Die Bestandsspalten des Eigentümers aus SEINER Tabelle.</summary>
        private static Matrixeingang Bestand(KonditionierungCtrl.Eigner e)
        {
            string tabelle = Matrixzellenort.Tabelle(e.Art);
            DataTable t = DataRepository.GetDataTable(
                "SELECT * FROM \"" + tabelle + "\" WHERE \"ID\" = ?", new DbParam("@id", Traeger(e)));
            DataRow r = t.Rows[0];
            return new Matrixeingang
            {
                SollTag = Zahl(r, "Raumsolltemperatur_Tag"),
                SollNacht = Zahl(r, "Raumsolltemperatur_Nachtabsenkung"),
                SollWochenende = Zahl(r, "Raumsolltemperatur_Wochenende"),
                SollFerien = Zahl(r, "Raumsolltemperatur_Ferien"),
                KuehlSollwert = Zahl(r, "Kuehl_Sollwert"),
                KuehlSollwertNacht = Zahl(r, "Kuehl_Sollwert_Nacht"),
                LuftwechselInfiltration = Zahl(r, "Luftwechsel_Infiltration"),
                LuftwechselNutzer = Zahl(r, "Luftwechsel_Nutzer"),
                InterneWaermegewinne = Zahl(r, "Interne_Waermegewinne"),
            };
        }

        private static double? Zahl(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture)
                : (double?)null;
    }
}
