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
    /// ETAPPE E7b — Schemaschritt <b>103</b>: Der Zeitzonentarif HT/NT wird abgelöst
    /// (Entscheid Q11, Anwender 22.09.2026: „kein HT/NT"; der Rest nach Empfehlung: die
    /// zweistufige Leistungspreis-Staffel zieht in die Kostenverwaltung).
    ///
    /// <para>Geprüft wird: der Zielstand und die drei Staffelspalten; die nachgezogene
    /// Arbeitskopie (die vier Zonenzeilen der Strommatrix von 1018 und 1031 sind je eine
    /// Jahreszeile mit denselben Summen, kein Satz steht aktiv im Zonenmodell); und der
    /// Datenteil an gebauten Sätzen — die Staffel geht an den Stromträger jeder Version
    /// der Gruppe, nur aus einem Satz, in dem sie rechnete, und nie über eine schon
    /// gepflegte Staffel; ein Satz ohne Stromträgerzeile wird benannt; der Satz des
    /// Zonenmodells wird abgeschaltet, ein Rollentarif nicht; ein zweiter Lauf findet
    /// nichts mehr.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZeitzonentarifAbloesungTests
    {
        /// <summary>Stamm „Wöhler" mit den Versionen 1023 und 1024 — alle drei führen
        /// den Stromträger 60 („Elektrische Energie") in energy_project_settings.</summary>
        private const int STAMM = 1019;
        private static readonly int[] GRUPPE = { 1019, 1023, 1024 };
        private const int STROM = 60;

        /// <summary>„BHKW Test München" mit der Version 1031 — beide führen nur Erdgas,
        /// keinen Stromträger.</summary>
        private const int STAMM_OHNE_STROM = 1018;

        [Fact]
        public void Der_Zielstand_ist_103_und_die_Staffel_hat_drei_Spalten_am_Stromtraeger()
        {
            Assert.True(SchemaStand.Zielversion >= 103,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 103.");

            Assert.Equal(3, SchemaKatalog.Schritt103_LeistungspreisStaffel.Length);
            foreach (SchemaSpalte s in SchemaKatalog.Schritt103_LeistungspreisStaffel)
            {
                Assert.Equal("energy_project_settings", s.Tabelle);
                Assert.Equal("DOUBLE", s.TypDefinition);
            }
            Assert.Equal(new[] { "Leistungspreis_Staffelgrenze", "Leistungspreis_Staffel1", "Leistungspreis_Staffel2" },
                         SchemaKatalog.Schritt103_LeistungspreisStaffel.Select(s => s.Name).ToArray());

            // Die Übernahme schreibt nur in leere Spalten — eine gepflegte Staffel bleibt.
            Assert.Contains("[Leistungspreis_Staffelgrenze] IS NULL", ZeitzonentarifAbloesung.SQL_STAFFEL_SETZEN,
                            StringComparison.Ordinal);
            Assert.Equal(new[] { "Winter HT", "Winter NT", "Sommer HT", "Sommer NT" }, ZeitzonentarifAbloesung.ZONEN);
        }

        /// <summary>
        /// Die nachgezogene Arbeitskopie (<see cref="TestDatenbank"/>): Die Staffelspalten
        /// stehen, kein Satz steht aktiv im Zonenmodell, und die Altbestände der
        /// Strommatrix (1018, 1031: je vier Zonenzeilen vom 21.08.2026) sind je EINE
        /// Jahreszeile — mit den Summen, die der Leser schon vorher bildete.
        /// </summary>
        [Fact]
        public void Die_Arbeitskopie_fuehrt_je_Projekt_eine_Jahreszeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ZeitzonentarifAbloesung.StaffelspaltenVorhanden());
            Assert.Equal(0, ZeitzonentarifAbloesung.OffeneZonensaetze());
            Assert.Equal(0, ZeitzonentarifAbloesung.OffeneZonenzeilen());

            // 1018: -7,718 - 6,510 - 0,184 - 0,059 = -14,471 MWh Bezug, gespiegelt in der
            // KWK-Einspeisung; 1031: -7,820 - 6,644 - 0,186 - 0,073 = -14,723 MWh.
            foreach (var (projekt, summe) in new[] { (1018, 14.471), (1031, 14.723) })
            {
                DataTable t = Matrixzeilen(projekt);
                Assert.Single(t.Rows);
                Assert.Equal(StromMatrix.ZEILE_JAHR, Convert.ToString(t.Rows[0]["Zone"]));
                Assert.Equal(-summe, Convert.ToDouble(t.Rows[0]["BezugMWh"]), 6);
                Assert.Equal(summe, Convert.ToDouble(t.Rows[0]["KwkEinspMWh"]), 6);
                Assert.Equal(new DateTime(2026, 8, 21, 13, 6, 32), Stempel(t.Rows[0]["Zeitstempel"]));

                StromMatrix m = new WirtschaftlichkeitCtrl().LadeStromMatrix(new List<int> { projekt })[projekt];
                Assert.Equal(-summe, m.BezugGesamtMWh, 6);
                Assert.Equal(summe, m.KwkEinspeisungGesamtMWh, 6);
            }
        }

        /// <summary>
        /// Der Datenteil an gebauten Sätzen — alle Fälle des Schrittes in einem Lauf.
        /// </summary>
        [Fact]
        public void Der_Schritt_uebernimmt_die_Staffel_schaltet_ab_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            new WirtschaftlichkeitCtrl().LadeTarif(STAMM);          // stellt die Tabelle sicher
            DataRepository.ExecuteNonQuery("DELETE FROM [Tab_ProjektTarif]");

            // (a) Stamm 1019, AKTIV im Zonenmodell mit Zonenpreisen und Staffel — die
            //     Staffel rechnete und wird übernommen, an alle drei Versionen.
            Tarif(9101, STAMM, aktiv: true, modus: DbWerte.TARIF_MODUS_ZONEN, bezug: 0.30,
                  grenze: 500, preis1: 80, preis2: 110);
            // Version 1023 führt schon eine eigene Staffel: sie bleibt.
            Staffel(1023, STROM, 999, 1, 2);

            // (b) Stamm 1018 ohne Stromträger, aktiv im Zonenmodell (Modus leer = Bestand
            //     vor Schritt 21): wird abgeschaltet, die Staffel hat kein Ziel.
            Tarif(9102, STAMM_OHNE_STROM, aktiv: true, modus: null, bezug: 0.25,
                  grenze: 100, preis1: 50, preis2: 60);

            // (c) 1030 AKTIV im Zonenmodell, aber ohne Bezugspreis: Die Rechnung fiel schon
            //     damals auf die Flat-Preise zurück — die Staffel rechnete nie und wird
            //     nicht übernommen; abgeschaltet wird der Satz trotzdem.
            Tarif(9103, 1030, aktiv: true, modus: DbWerte.TARIF_MODUS_ZONEN, bezug: 0,
                  grenze: 1500, preis1: 60, preis2: 90);

            // (d) 1026 INAKTIV im Zonenmodell mit Staffel: rechnete nie, bleibt, wie er ist.
            Tarif(9104, 1026, aktiv: false, modus: DbWerte.TARIF_MODUS_ZONEN, bezug: 0.3,
                  grenze: 40, preis1: 55, preis2: 70);

            // (e) 1039 AKTIV im Rollenmodell: kein Zonensatz — bleibt aktiv, keine Übernahme.
            Tarif(9105, 1039, aktiv: true, modus: DbWerte.TARIF_MODUS_ROLLEN, bezug: 0.3,
                  grenze: 40, preis1: 55, preis2: 70);

            // Die Strommatrix eines Projekts im alten Zonenformat (vier Zeilen, die erste
            // mit der höchsten Stundenlast und dem jüngsten Zeitstempel).
            DataRepository.ExecuteNonQuery("DELETE FROM [Tab_ErgebnisStromMatrix] WHERE [ID_Projekt] = 1040");
            for (int i = 0; i < ZeitzonentarifAbloesung.ZONEN.Length; i++)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO [Tab_ErgebnisStromMatrix] ([ID], [ID_Projekt], [Zone], [BezugMWh], [EinspPvMWh], " +
                    "[KwkEigenMWh], [KwkEinspMWh], [MaxBezugKW], [BedarfMWh], [Zeitstempel]) VALUES (?, 1040, ?, ?, ?, 0, 0, ?, ?, ?)",
                    new DbParam("@id", 99001 + i), new DbParam("@z", ZeitzonentarifAbloesung.ZONEN[i]),
                    new DbParam("@b", 1.25), new DbParam("@pv", 0.5), new DbParam("@mx", i == 0 ? 8.4 : 5.0),
                    new DbParam("@bd", 2.0), new DbParam("@t", i == 0 ? "2026-09-22 10:00:00" : "2026-09-20 10:00:00"));

            Assert.Equal(3, ZeitzonentarifAbloesung.OffeneZonensaetze());      // (a), (b), (c)
            Assert.Equal(4, ZeitzonentarifAbloesung.OffeneZonenzeilen());

            ZeitzonentarifAbloesung.Bericht b = ZeitzonentarifAbloesung.Ausfuehren();

            // (a) Übernahme an 1019 und 1024; 1023 behält seine eigene Staffel.
            Assert.Equal(2, b.StaffelnGeschrieben);
            Assert.Equal(new double?[] { 500, 80, 110 }, StaffelVon(STAMM, STROM));
            Assert.Equal(new double?[] { 500, 80, 110 }, StaffelVon(1024, STROM));
            Assert.Equal(new double?[] { 999, 1, 2 }, StaffelVon(1023, STROM));
            Assert.Contains(b.Uebernahmen, u => u.IdProjekt == 1023 && u.Grund != null &&
                                                u.Grund.Contains("schon eine Staffel"));

            // (b) Ohne Stromträger: benannt, nicht angelegt.
            Assert.Contains(b.Uebernahmen, u => u.IdStamm == STAMM_OHNE_STROM && u.IdProjekt == 1018 &&
                                                u.Grund == "kein Stromtraeger zugeordnet");
            Assert.Contains(b.Uebernahmen, u => u.IdStamm == STAMM_OHNE_STROM && u.IdProjekt == 1031);

            // (c) Kein Zonen-Bezugspreis: keine Übernahme an 1030.
            Assert.DoesNotContain(b.Uebernahmen, u => u.IdStamm == 1030);
            Assert.Equal(new double?[] { null, null, null }, StaffelVon(1030, STROM));

            // Abgeschaltet sind genau die drei aktiven Zonensätze; (d) und (e) bleiben.
            Assert.Equal(new[] { 1018, 1019, 1030 }, b.Abgeschaltet.OrderBy(x => x).ToArray());
            Assert.False(Aktiv(STAMM));
            Assert.False(Aktiv(STAMM_OHNE_STROM));
            Assert.False(Aktiv(1030));
            Assert.False(Aktiv(1026));
            Assert.True(Aktiv(1039));

            // Die Matrix von 1040 ist EINE Jahreszeile: Summen, Maximum, jüngster Stempel.
            DataTable m = Matrixzeilen(1040);
            Assert.Single(m.Rows);
            Assert.Equal(StromMatrix.ZEILE_JAHR, Convert.ToString(m.Rows[0]["Zone"]));
            Assert.Equal(5.0, Convert.ToDouble(m.Rows[0]["BezugMWh"]), 6);
            Assert.Equal(2.0, Convert.ToDouble(m.Rows[0]["EinspPvMWh"]), 6);
            Assert.Equal(8.4, Convert.ToDouble(m.Rows[0]["MaxBezugKW"]), 6);
            Assert.Equal(8.0, Convert.ToDouble(m.Rows[0]["BedarfMWh"]), 6);
            Assert.Equal(new DateTime(2026, 9, 22, 10, 0, 0), Stempel(m.Rows[0]["Zeitstempel"]));
            Assert.Contains(1040, b.MatrixProjekte);
            Assert.Equal(4, b.MatrixZeilenEntfallen);

            // Der Bericht nennt alles in einer Zeile ohne Umbruch.
            string text = b.Text();
            Assert.DoesNotContain("\n", text, StringComparison.Ordinal);
            Assert.Contains("2 Stromtraegerzeile(n) uebernommen", text, StringComparison.Ordinal);

            // WIEDERHOLBAR: nichts offen, nichts geschrieben, nichts abgeschaltet.
            Assert.Equal(0, ZeitzonentarifAbloesung.OffeneZonensaetze());
            Assert.Equal(0, ZeitzonentarifAbloesung.OffeneZonenzeilen());
            ZeitzonentarifAbloesung.Bericht zweiter = ZeitzonentarifAbloesung.Ausfuehren();
            Assert.Equal(0, zweiter.StaffelnGeschrieben);
            Assert.Empty(zweiter.Uebernahmen);
            Assert.Empty(zweiter.Abgeschaltet);
            Assert.Empty(zweiter.MatrixProjekte);
            Assert.Equal(new double?[] { 500, 80, 110 }, StaffelVon(STAMM, STROM));
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        private static void Tarif(int id, int projekt, bool aktiv, string modus, double bezug,
                                  double grenze, double preis1, double preis2)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO [Tab_ProjektTarif] ([ID], [ID_Projekt], [Aktiv], [Winter_Von], [Winter_Bis], " +
                "[HT_Von], [HT_Bis], [Bezug_W_HT], [Bezug_W_NT], [Bezug_S_HT], [Bezug_S_NT], " +
                "[Staffel_Grenze], [Staffel_Preis1], [Staffel_Preis2], [Tarif_Modus]) " +
                "VALUES (?, ?, ?, 10, 3, 6, 22, ?, ?, ?, ?, ?, ?, ?, ?)",
                new DbParam("@id", id), new DbParam("@p", projekt),
                new DbParam("@a", DbParamTyp.Boolean) { Wert = aktiv },
                new DbParam("@b1", bezug), new DbParam("@b2", bezug), new DbParam("@b3", bezug), new DbParam("@b4", bezug),
                new DbParam("@g", grenze), new DbParam("@s1", preis1), new DbParam("@s2", preis2),
                new DbParam("@m", DbParamTyp.VarWChar, 12) { Wert = (object)modus ?? DBNull.Value });
        }

        private static void Staffel(int projekt, int traeger, double grenze, double preis1, double preis2)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [energy_project_settings] SET [Leistungspreis_Staffelgrenze] = ?, " +
                "[Leistungspreis_Staffel1] = ?, [Leistungspreis_Staffel2] = ? " +
                "WHERE [ID_Projekt] = ? AND [ID_Energieträger] = ?",
                new DbParam("@g", grenze), new DbParam("@p1", preis1), new DbParam("@p2", preis2),
                new DbParam("@p", projekt), new DbParam("@c", traeger));
        }

        private static double?[] StaffelVon(int projekt, int traeger)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT [Leistungspreis_Staffelgrenze] AS g, [Leistungspreis_Staffel1] AS p1, " +
                "[Leistungspreis_Staffel2] AS p2 FROM [energy_project_settings] " +
                "WHERE [ID_Projekt] = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", projekt), new DbParam("@c", traeger));
            Assert.NotNull(t);
            Assert.Single(t.Rows);
            DataRow r = t.Rows[0];
            return new[] { Wert(r, "g"), Wert(r, "p1"), Wert(r, "p2") };
        }

        private static double? Wert(DataRow r, string spalte) =>
            r[spalte] == DBNull.Value ? (double?)null : Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);

        private static bool Aktiv(int projekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [Aktiv] FROM [Tab_ProjektTarif] WHERE [ID_Projekt] = ?", new DbParam("@p", projekt));
            return o != null && o != DBNull.Value && Convert.ToInt32(o) == 1;
        }

        /// <summary>
        /// Der Zeitstempel einer Matrixzeile als Zeitpunkt — die Datenschicht liefert ihn
        /// als <see cref="DateTime"/>; eine Zeichenkette hinge an der Kultur des Läufers.
        /// </summary>
        private static DateTime Stempel(object wert)
            => wert is DateTime d
                ? d
                : DateTime.Parse(Convert.ToString(wert, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        private static DataTable Matrixzeilen(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT * FROM [Tab_ErgebnisStromMatrix] WHERE [ID_Projekt] = ? ORDER BY [ID]",
                new DbParam("@p", projekt));
            Assert.NotNull(t);
            return t;
        }
    }
}
