using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.KonditionierungsarbeitTests;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Aus dem Katalog erneut übernehmen" und die Rückfragen VOR dem Schreiben</b> (Stufe KP2,
    /// Welle K2, Teilschritt 3; Entwurf KP2 Festlegung 3 und 4, Befund B9): Die erneute Übernahme
    /// ersetzt die ganze Gebäudeebene samt neun Bestandszellen, Nachtzeiten und Ferienzeiträumen, die
    /// Zonen bleiben; der Befund entsteht aus dem Arbeitsstand, bevor geschrieben wird, und nennt die
    /// Zonen mit Namen — eine Frage je Handlung.
    ///
    /// <para>Die reinen Fälle rechnen ohne Datenbank; die Fälle des Datenbankwegs arbeiten auf einer
    /// Arbeitskopie der Testdatenbank und schweigen ohne sie. Geprüft werden Zahlen und Namen, keine
    /// Ressourcentexte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErneutUebernehmenMatrixTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private const Konditionierungsgroesse HEIZ = Konditionierungsgroesse.Heizsoll;

        // =============================================================================
        //  Rein: die erneute Übernahme
        // =============================================================================

        /// <summary>Ein Katalogbau mit anderen Sollwerten, anderer Nachtzeit, anderen Ferien und einer Personenspalte.</summary>
        private static Konditionierungsstand Katalog()
        {
            Matrixeingang k = Bestand();
            k.SollTag = 19.0;
            k.SollNacht = 15.0;
            k.NachtBeginn = 23;
            k.NachtEnde = 5;
            k.Ferienbeginn[0] = 180.0;
            k.Ferienende[0] = 190.0;
            k.Ferienmerker = 0.0;
            k.InterneWaermegewinne = 300.0;
            k.Bewohner = 9.0;
            return Konditionierungsstand.Leer(Kalendereigentuemer.Katalogbau, k)
                .MitVorgabe(Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.4))
                .MitVorgabe(Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(210.0));
        }

        /// <summary>Ein Projektgebäude mit Lüftungsnacht, eigener Nachtzeit, Heizkalender samt Feiertagen und zwei Zonen.</summary>
        private static Konditionierungsarbeitsstand Projekt()
        {
            Konditionierungsarbeitsstand a = Stand(Zone(-1, "Anbau", b => b.SollTag = 22.0), Zone(-2, "Halle"));
            a = Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(Konditionierungsgroesse.Lueftung), DbWerte.KOND_ZEILE_NACHT,
                                                       Matrixzelle.AusWert(1.5)));
            a = Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(HEIZ), DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(17.0, 21, 5)));
            a = Gut(Konditionierungsarbeit.Anlegen(a, Ort(HEIZ)));
            a = Gut(Konditionierungsarbeit.Feiertage(a, Ort(HEIZ), 7));
            return Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(Konditionierungsgroesse.Geraete, -1), DbWerte.KOND_ZEILE_TAG,
                                                          Matrixzelle.AusWert(0.5)));
        }

        [Fact]
        public void Erneut_uebernehmen_ersetzt_die_ganze_Gebaeudeebene_und_laesst_die_Zonen()
        {
            Konditionierungsarbeitsstand a = Projekt();
            Konditionierungsstand katalog = Katalog();

            Konditionierungsbilanz frage = Konditionierungsarbeit.Rueckfrage(a, null, Konditionierungshandlung.KatalogErneut, katalog);
            Konditionierungsschritt s = Konditionierungsarbeit.KatalogErneut(a, katalog);
            Konditionierungsarbeitsstand b = Gut(s);

            // Die Gebäudeebene ist die des Katalogbaus — Bestandszellen, Nachtzeit, Ferien, Tabellen.
            Matrixeingang g = b.Gebaeude.Bestand;
            Assert.Equal(19.0, g.SollTag);
            Assert.Equal(15.0, g.SollNacht);
            Assert.Equal(23, g.NachtBeginn);
            Assert.Equal(5, g.NachtEnde);
            Assert.Equal(180.0, g.Ferienbeginn[0]);
            Assert.Equal(190.0, g.Ferienende[0]);
            Assert.Equal(0.0, g.Ferienmerker);
            Assert.Equal(300.0, g.InterneWaermegewinne);
            Assert.Equal(5.0, g.Bewohner);                      // keine Konditionierungszelle: bleibt
            Assert.Equal(Kalendereigentuemer.Gebaeude, b.Gebaeude.Art);
            Assert.Equal(0, b.Gebaeude.KalenderAnzahl);
            Assert.False(Konditionierungsstand.Traegt(b.Gebaeude.Vorgabe(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT)));
            Assert.Equal(0.4, b.Gebaeude.Vorgabe(Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG).Wert);

            // Die Zonen bleiben, wie sie waren.
            Assert.True(a.Zone(-1).Stand.Gleich(b.Zone(-1).Stand));
            Assert.True(a.Zone(-2).Stand.Gleich(b.Zone(-2).Stand));

            // Der Befund entsteht VORHER und gleicht dem des Schritts.
            Assert.NotNull(frage);
            Assert.Equal(frage.ToString(), s.Bilanz.ToString());
            Assert.Equal(1, frage.ErsetztAnzahl(Konditionierungspostenart.Kalender));
            Assert.Equal(9, frage.ErsetztAnzahl(Konditionierungspostenart.Feiertage));
            Assert.Equal(1, frage.ErsetztAnzahl(Konditionierungspostenart.Nachtzeiten));
            Assert.Equal(1, frage.ErsetztAnzahl(Konditionierungspostenart.Ferienzeitraeume));
            Assert.True(frage.ErsetztAnzahl(Konditionierungspostenart.Matrixzellen) >= 5);
            Assert.Equal(1, frage.BleibtAnzahl(Konditionierungspostenart.Zonenkalender));   // der mit angelegte der Zone „Anbau"
            Assert.Equal(new[] { "Anbau" }, frage.Zonen);
        }

        [Fact]
        public void Ohne_Katalogbau_wird_benannt_abgelehnt_und_nicht_gefragt()
        {
            Konditionierungsarbeitsstand a = Projekt();
            Assert.False(Konditionierungsarbeit.KatalogErneut(a, null).Ok);
            Assert.Null(Konditionierungsarbeit.Rueckfrage(a, null, Konditionierungshandlung.KatalogErneut, null));
        }

        // =============================================================================
        //  Rein: eine Frage je Handlung
        // =============================================================================

        [Fact]
        public void Ohne_angelegten_Kalender_fragen_Matrix_erneut_und_Verwerfen_nicht()
        {
            Konditionierungsarbeitsstand a = Stand(Zone(-1, "Anbau", b => b.SollTag = 22.0), Zone(-2, "Halle"));
            Assert.Null(Konditionierungsarbeit.Rueckfrage(a, Ort(HEIZ), Konditionierungshandlung.MatrixErneut));
            Assert.Null(Konditionierungsarbeit.Rueckfrage(a, Ort(HEIZ), Konditionierungshandlung.Verwerfen));

            // Vorlage übernehmen ersetzt die Zellen der Spalte; die Zone mit eigenem Sollwert bekäme ihren Kalender.
            Konditionierungsbilanz v = Konditionierungsarbeit.Rueckfrage(a, Ort(HEIZ), Konditionierungshandlung.VorlageUebernehmen);
            Assert.NotNull(v);
            Assert.Equal(4, v.ErsetztAnzahl(Konditionierungspostenart.Matrixzellen));
            Assert.Equal(new[] { "Anbau" }, v.Zonen);
            Assert.Null(Konditionierungsarbeit.Rueckfrage(a, Ort(Konditionierungsgroesse.Personen), Konditionierungshandlung.VorlageUebernehmen));
        }

        [Fact]
        public void Matrix_erneut_und_Verwerfen_nennen_Ersetztes_Bleibendes_und_die_folgenden_Zonen()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.Anlegen(
                Stand(Zone(-1, "Anbau", b => b.SollTag = 22.0), Zone(-2, "Halle")), Ort(HEIZ)));
            a = Gut(Konditionierungsarbeit.Zeitfenster(a, Ort(HEIZ), new[] { 0 }, 6, 8, 22.0));    // von Hand geändert
            a = MitEigenerPeriode(a);

            Konditionierungsbilanz m = Konditionierungsarbeit.Rueckfrage(a, Ort(HEIZ), Konditionierungshandlung.MatrixErneut);
            Assert.NotNull(m);
            Assert.Equal(1, m.ErsetztAnzahl(Konditionierungspostenart.Standardwoche));
            Assert.Equal(1, m.ErsetztAnzahl(Konditionierungspostenart.Ferienperioden));
            Assert.Equal(1, m.BleibtAnzahl(Konditionierungspostenart.EigenePerioden));
            Assert.Equal(1, m.BleibtAnzahl(Konditionierungspostenart.Feiertage));
            Assert.Equal(new[] { "Halle" }, m.Zonen);          // „Anbau" führt seinen eigenen Kalender

            Konditionierungsbilanz v = Konditionierungsarbeit.Rueckfrage(a, Ort(HEIZ), Konditionierungshandlung.Verwerfen);
            Assert.Equal(1, v.ErsetztAnzahl(Konditionierungspostenart.Kalender));
            Assert.Equal(1, v.ErsetztAnzahl(Konditionierungspostenart.EigenePerioden));
            Assert.Equal(1, v.ErsetztAnzahl(Konditionierungspostenart.Feiertage));
            Assert.Equal(new[] { "Halle" }, v.Zonen);

            // Die Rückfrage schreibt nichts — der Stand ist derselbe.
            Assert.Equal(Konditionierungsarbeit.Abdruck(a), Konditionierungsarbeit.Abdruck(a));
        }

        [Fact]
        public void Speichern_unter_im_Projekt_nennt_Zonen_Bauteile_und_Konditionierung_zusammen()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.ZelleSetzen(
                Stand(Zone(-1, "Anbau"), Zone(-2, "Halle")), Ort(Konditionierungsgroesse.Geraete, -1),
                DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5)));
            Konditionierungsbilanz b = Konditionierungsarbeit.RueckfrageSpeichernUnter(a, 3);
            Assert.NotNull(b);
            Assert.Equal(new[] { "Anbau", "Halle" }, b.Zonen);
            Assert.Equal(3, b.BleibtAnzahl(Konditionierungspostenart.Bauteile));
            Assert.Equal(1, b.BleibtAnzahl(Konditionierungspostenart.Zonenkalender));
            Assert.Null(Konditionierungsarbeit.RueckfrageSpeichernUnter(Stand(), 0));
        }

        // =============================================================================
        //  Der Datenbankweg: Befund vorher, dann EIN Vorgang
        // =============================================================================

        private static readonly string[] SPALTEN =
        {
            "Raumsolltemperatur_Tag", "Nachtabsenkung_Beginn", "Nachtabsenkung_Ende", "Ferienbeginn_2", "Ferienende_2",
            "Interne_Waermegewinne",
        };

        [Fact]
        public void Die_erneute_Uebernahme_ersetzt_auch_Bestandszellen_Nachtzeiten_und_Ferienzeitraeume()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;
            long stamm = Id("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0");
            long projekt = Id("SELECT MIN(ID) FROM \"Tab_Projekt\"");
            if (stamm == 0 || projekt == 0) return;
            int neu = Uebernehmen(stamm, projekt);
            Assert.True(neu > 0);

            // Das Projektgebäude weicht von Hand ab; eine Zone trägt einen eigenen Sollwert.
            DataRepository.ExecuteSQL(
                "UPDATE \"Tab_Gebaeude\" SET \"Raumsolltemperatur_Tag\" = 23.5, \"Nachtabsenkung_Beginn\" = 20, " +
                "\"Nachtabsenkung_Ende\" = 4, \"Ferienbeginn_2\" = 50, \"Ferienende_2\" = 60, " +
                "\"Interne_Waermegewinne\" = 999 WHERE \"ID\" = ?", new DbParam("@g", neu));
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\", " +
                "\"Raumsolltemperatur_Tag\") VALUES (?, 1, ?, 1, 22.0)",
                new DbParam("@g", neu), new DbParam("@b", "Anbauprobe"));
            long zone = Id("SELECT MAX(ID) FROM \"Tab_Zone\"");

            // Der Befund entsteht VORHER — gelesen, nicht geschrieben.
            Konditionierungsbilanz frage = GebaeudeStammCtrl.KonditionierungErneutUebernehmenRueckfrage(neu);
            Assert.NotNull(frage);
            Assert.Equal(1, frage.ErsetztAnzahl(Konditionierungspostenart.Nachtzeiten));
            Assert.Equal(1, frage.ErsetztAnzahl(Konditionierungspostenart.Ferienzeitraeume));
            Assert.Contains("Anbauprobe", frage.Zonen);
            Assert.Equal("23.5", Wert("Tab_Gebaeude", neu, "Raumsolltemperatur_Tag"));

            Konditionierungskopie.Befund befund = GebaeudeStammCtrl.KonditionierungErneutUebernehmen(neu);
            Assert.True(befund.Ok, befund.Meldung);

            foreach (string spalte in SPALTEN)
                Assert.Equal(Wert("Tab_Gebaeude_STAMM", stamm, spalte), Wert("Tab_Gebaeude", neu, spalte));
            Assert.Equal("22", Wert("Tab_Zone", zone, "Raumsolltemperatur_Tag"));
        }

        /// <summary>Katalogbau → Projekt über den einzigen Weg, mit frischer Zuordnungszeile.</summary>
        private static int Uebernehmen(long stamm, long projekt)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Z_ProjektGebaeude\" (\"ID_Projekt\", \"Wohnflaeche_Waermebedarf\", " +
                "\"Einheit_Waermebedarf_Wohnflaeche\", \"Jahresnutzungsgrad\", \"dezWarmwasserbereitung\") " +
                "VALUES (?, 100.0, ?, 0.9, 0)",
                new DbParam("@p", projekt), new DbParam("@e", "m2"));
            long z = Id("SELECT MAX(ID) FROM \"Z_ProjektGebaeude\"");
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"Bezeichner\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = ?", new DbParam("@s", stamm)),
                CultureInfo.InvariantCulture);
            return new GebaeudeStammCtrl().CopyFromStamm((int)stamm, name, (int)projekt, (int)z);
        }

        /// <summary>Ein Spaltenwert als invarianter Text; leer = NULL.</summary>
        private static string Wert(string tabelle, long id, string spalte)
        {
            object o = DataRepository.ExecuteScalar("SELECT \"" + spalte + "\" FROM \"" + tabelle + "\" WHERE \"ID\" = ?",
                                                    new DbParam("@id", id));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static long Id(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
