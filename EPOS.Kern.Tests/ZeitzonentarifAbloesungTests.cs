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
    /// zweistufige Leistungspreis-Staffel zieht in die Kostenverwaltung) und die alten
    /// Tarife werden verworfen (Entscheid E7b‑Q4, Anwender 23.09.2026: „alte Tarife
    /// verwerfen, nicht mehr relevant").
    ///
    /// <para>Geprüft wird: der Zielstand und die drei Staffelspalten; die nachgezogene
    /// Arbeitskopie (die vier Zonenzeilen der Strommatrix von 1018 und 1031 sind je eine
    /// Jahreszeile mit denselben Summen, kein Satz steht im Zonenmodell); und der
    /// Datenteil an gebauten Sätzen — die Staffel geht an den Stromträger jeder Version
    /// der Gruppe, nur aus einem Satz, in dem sie rechnete, und nie über eine schon
    /// gepflegte Staffel; ein Satz ohne Stromträgerzeile wird benannt; jeder Satz des
    /// Zonenmodells wird gelöscht, aktiv oder nicht, ein Rollentarif nicht; ein
    /// gespeicherter Lauf, der mit einem Zonentarif rechnete, wird samt Sensitivität und
    /// Matrix verworfen, ein Flat- und ein Rollenergebnis bleiben; ein zweiter Lauf
    /// findet nichts mehr.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZeitzonentarifAbloesungTests
    {
        /// <summary>Stamm „Wöhler" mit den Versionen 1023 und 1024 — alle drei führen
        /// den Stromträger 60 („Elektrische Energie") in energy_project_settings.</summary>
        private const int STAMM = 1019;
        private const int STROM = 60;

        /// <summary>„BHKW Test München" mit der Version 1031 — beide führen nur Erdgas,
        /// keinen Stromträger.</summary>
        private const int STAMM_OHNE_STROM = 1018;

        /// <summary>Stamm mit den Versionen 1027 und 1029; hier mit einem INAKTIVEN
        /// Satz des Zonenmodells.</summary>
        private const int STAMM_INAKTIV = 1026;

        /// <summary>Ein Satz im Rollenmodell — er bleibt, samt seinen Ergebnissen.</summary>
        private const int STAMM_ROLLEN = 1039;

        private const string TAB_ERGEBNIS = "Tab_ErgebnisWirtschaftlichkeit";
        private const string TAB_SENS = "Tab_ErgebnisWirtSensitivitaet";
        private const string TAB_MATRIX = "Tab_ErgebnisStromMatrix";

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

            // Die Kennzeichnung eines mit einem Tarif gerechneten Ergebnisses.
            Assert.Equal("StromkostenTarif", ZeitzonentarifAbloesung.SPALTE_STROMKOSTEN_TARIF);
            Assert.Contains("[StromkostenTarif] IS NOT NULL", ZeitzonentarifAbloesung.SQL_TARIFERGEBNISSE_ZAEHLEN,
                            StringComparison.Ordinal);
        }

        /// <summary>
        /// Die nachgezogene Arbeitskopie (<see cref="TestDatenbank"/>): Die Staffelspalten
        /// stehen, kein Satz steht im Zonenmodell, und die Altbestände der Strommatrix
        /// (1018, 1031: je vier Zonenzeilen vom 21.08.2026) sind je EINE Jahreszeile — mit
        /// den Summen, die der Leser schon vorher bildete. Kein gespeichertes Ergebnis der
        /// Testdatenbank rechnete mit einem Tarif; verworfen wird deshalb nichts.
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

            // Die gespeicherten Ergebnisse der Testdatenbank stehen alle noch da.
            Assert.Equal(3, Zeilen(TAB_ERGEBNIS, 1018));
            Assert.Equal(3, Zeilen(TAB_ERGEBNIS, 1019));
        }

        /// <summary>
        /// Der Datenteil an gebauten Sätzen — Übernahme, Löschen, Zusammenfassen — in einem
        /// Lauf, und ein zweiter Lauf findet nichts mehr.
        /// </summary>
        [Fact]
        public void Der_Schritt_uebernimmt_die_Staffel_loescht_die_Zonensaetze_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SaetzeAnlegen();

            // Die Strommatrix eines Projekts im alten Zonenformat (vier Zeilen, die erste
            // mit der höchsten Stundenlast und dem jüngsten Zeitstempel).
            Zonenmatrix(1040, 99001);

            Assert.Equal(4, ZeitzonentarifAbloesung.OffeneZonensaetze());      // (a) bis (d)
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

            // (d) Der inaktive Satz rechnete nie: keine Übernahme an 1026.
            Assert.DoesNotContain(b.Uebernahmen, u => u.IdStamm == STAMM_INAKTIV);

            // Gelöscht sind genau die vier Sätze des Zonenmodells, aktiv oder nicht; der
            // Rollensatz (e) steht weiter und bleibt aktiv.
            Assert.Equal(new[] { 1018, 1019, 1026, 1030 }, b.Geloescht.OrderBy(x => x).ToArray());
            foreach (int projekt in new[] { STAMM, STAMM_OHNE_STROM, 1030, STAMM_INAKTIV })
                Assert.False(SatzVorhanden(projekt), "Der Zonensatz von " + projekt + " steht noch.");
            Assert.True(SatzVorhanden(STAMM_ROLLEN));
            Assert.True(Aktiv(STAMM_ROLLEN));

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
            Assert.Contains("Tarifsaetze im Zonenmodell geloescht: 4", text, StringComparison.Ordinal);

            // WIEDERHOLBAR: nichts offen, nichts geschrieben, nichts gelöscht.
            Assert.Equal(0, ZeitzonentarifAbloesung.OffeneZonensaetze());
            Assert.Equal(0, ZeitzonentarifAbloesung.OffeneZonenzeilen());
            ZeitzonentarifAbloesung.Bericht zweiter = ZeitzonentarifAbloesung.Ausfuehren();
            Assert.Equal(0, zweiter.StaffelnGeschrieben);
            Assert.Empty(zweiter.Uebernahmen);
            Assert.Empty(zweiter.Geloescht);
            Assert.Empty(zweiter.ErgebnisseVerworfen);
            Assert.Empty(zweiter.MatrixProjekte);
            Assert.Equal(new double?[] { 500, 80, 110 }, StaffelVon(STAMM, STROM));
            Assert.True(SatzVorhanden(STAMM_ROLLEN));
        }

        /// <summary>
        /// Entscheid E7b‑Q4: Gespeicherte Ergebnisse, die mit einem Zonentarif gerechnet
        /// wurden, werden verworfen. <b>Die Kennzeichnung</b>: eine Ergebniszeile mit
        /// gefüllter Spalte <c>StromkostenTarif</c> in der Gruppe eines Satzes im
        /// Zonenmodell — gleich, ob der Satz noch aktiv ist. Verworfen wird der ganze
        /// gespeicherte Lauf des Projekts (alle Szenarien, Sensitivität, Matrix). Ein
        /// Flat-Ergebnis derselben Gruppe (ohne <c>StromkostenTarif</c>) und ein
        /// Rollenergebnis (Gruppe eines Rollensatzes) bleiben.
        /// </summary>
        [Fact]
        public void Der_Schritt_verwirft_die_mit_einem_Zonentarif_gerechneten_Ergebnisse()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SaetzeAnlegen();

            // Vorbestand der Testdatenbank: je drei Ergebniszeilen, keine mit Tarif.
            foreach (int projekt in new[] { 1018, 1019, 1023, 1024, 1026, 1027 })
                Assert.Equal(3, Zeilen(TAB_ERGEBNIS, projekt));

            // Mit dem Zonentarif gerechnet: 1019 und 1024 (Gruppe des aktiven Satzes (a)),
            // 1026 (der Satz (d) ist inzwischen inaktiv, der Lauf rechnete mit ihm).
            foreach (int projekt in new[] { 1019, 1024, STAMM_INAKTIV })
                Tarifkosten(projekt, 12345.6);

            // Mit dem Rollentarif gerechnet: 1039 — ein Lauf mit drei Szenarien.
            for (int i = 0; i < 3; i++)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO [" + TAB_ERGEBNIS + "] ([ID], [ID_Projekt], [Szenario], [StromkostenTarif]) " +
                    "VALUES (?, ?, ?, ?)",
                    new DbParam("@id", 98001 + i), new DbParam("@p", STAMM_ROLLEN),
                    new DbParam("@s", WirtschaftlichkeitSzenario.Alle[i]), new DbParam("@t", 4321.0));

            // Sensitivität und Matrix gehören zum gespeicherten Lauf.
            Sensitivitaet(98101, STAMM);
            Sensitivitaet(98102, STAMM);
            Sensitivitaet(98103, STAMM_ROLLEN);
            Zonenmatrix(STAMM, 99011);

            ZeitzonentarifAbloesung.Bericht b = ZeitzonentarifAbloesung.Ausfuehren();

            Assert.Equal(new[] { 1019, 1024, 1026 }, b.ErgebnisseVerworfen.ToArray());
            Assert.Equal(9, b.ErgebniszeilenVerworfen);
            foreach (int projekt in new[] { 1019, 1024, STAMM_INAKTIV })
                Assert.Equal(0, Zeilen(TAB_ERGEBNIS, projekt));

            // Flat-Ergebnisse derselben Gruppen bleiben — ebenso der Stamm ohne Stromträger.
            Assert.Equal(3, Zeilen(TAB_ERGEBNIS, 1023));
            Assert.Equal(3, Zeilen(TAB_ERGEBNIS, 1027));
            Assert.Equal(3, Zeilen(TAB_ERGEBNIS, 1018));

            // Das Rollenergebnis bleibt, samt Sensitivität.
            Assert.Equal(3, Zeilen(TAB_ERGEBNIS, STAMM_ROLLEN));
            Assert.Equal(1, Zeilen(TAB_SENS, STAMM_ROLLEN));

            // Der verworfene Lauf verliert Sensitivität und Matrix — keine Jahreszeile.
            Assert.Equal(0, Zeilen(TAB_SENS, STAMM));
            Assert.Equal(0, Zeilen(TAB_MATRIX, STAMM));
            Assert.DoesNotContain(STAMM, b.MatrixProjekte);
            Assert.Equal(0, ZeitzonentarifAbloesung.OffeneZonenzeilen());

            Assert.Contains("mit Zonentarif gerechnete Ergebnisse verworfen: 9 Zeile(n) in 3 Projekt(en)",
                            b.Text(), StringComparison.Ordinal);

            // Wiederholbar: Ohne Zonensatz gibt es keine Gruppe mehr, deren Ergebnis fiele.
            ZeitzonentarifAbloesung.Bericht zweiter = ZeitzonentarifAbloesung.Ausfuehren();
            Assert.Empty(zweiter.ErgebnisseVerworfen);
            Assert.Equal(3, Zeilen(TAB_ERGEBNIS, STAMM_ROLLEN));
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        /// <summary>
        /// Die fünf Sätze der Fälle: (a) 1019 aktiv im Zonenmodell mit Zonenpreisen und
        /// Staffel (rechnete, Übernahme an alle drei Versionen; 1023 führt schon eine
        /// eigene Staffel); (b) 1018 ohne Stromträger, aktiv, Modus leer (Bestand vor
        /// Schritt 21); (c) 1030 aktiv ohne Bezugspreis (die Rechnung fiel schon damals auf
        /// die Flat-Preise zurück); (d) 1026 inaktiv im Zonenmodell mit Staffel; (e) 1039
        /// aktiv im Rollenmodell.
        /// </summary>
        private static void SaetzeAnlegen()
        {
            new WirtschaftlichkeitCtrl().LadeTarif(STAMM);          // stellt die Tabellen sicher
            DataRepository.ExecuteNonQuery("DELETE FROM [Tab_ProjektTarif]");

            Tarif(9101, STAMM, aktiv: true, modus: DbWerte.TARIF_MODUS_ZONEN, bezug: 0.30,
                  grenze: 500, preis1: 80, preis2: 110);
            Staffel(1023, STROM, 999, 1, 2);
            Tarif(9102, STAMM_OHNE_STROM, aktiv: true, modus: null, bezug: 0.25,
                  grenze: 100, preis1: 50, preis2: 60);
            Tarif(9103, 1030, aktiv: true, modus: DbWerte.TARIF_MODUS_ZONEN, bezug: 0,
                  grenze: 1500, preis1: 60, preis2: 90);
            Tarif(9104, STAMM_INAKTIV, aktiv: false, modus: DbWerte.TARIF_MODUS_ZONEN, bezug: 0.3,
                  grenze: 40, preis1: 55, preis2: 70);
            Tarif(9105, STAMM_ROLLEN, aktiv: true, modus: DbWerte.TARIF_MODUS_ROLLEN, bezug: 0.3,
                  grenze: 40, preis1: 55, preis2: 70);
        }

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

        /// <summary>Kennzeichnet den gespeicherten Lauf eines Projekts als mit einem
        /// Tarif gerechnet — so, wie der Tarifweg ihn schrieb.</summary>
        private static void Tarifkosten(int projekt, double betrag)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [" + TAB_ERGEBNIS + "] SET [StromkostenTarif] = ? WHERE [ID_Projekt] = ?",
                new DbParam("@t", betrag), new DbParam("@p", projekt));
        }

        private static void Sensitivitaet(int id, int projekt)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO [" + TAB_SENS + "] ([ID], [ID_Projekt], [Parameter]) VALUES (?, ?, ?)",
                new DbParam("@id", id), new DbParam("@p", projekt), new DbParam("@par", "Zinssatz"));
        }

        /// <summary>Vier Zonenzeilen im Format von vor E7b; die erste mit der höchsten
        /// Stundenlast und dem jüngsten Zeitstempel.</summary>
        private static void Zonenmatrix(int projekt, int ersteId)
        {
            DataRepository.ExecuteNonQuery("DELETE FROM [" + TAB_MATRIX + "] WHERE [ID_Projekt] = ?",
                                           new DbParam("@p", projekt));
            for (int i = 0; i < ZeitzonentarifAbloesung.ZONEN.Length; i++)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO [" + TAB_MATRIX + "] ([ID], [ID_Projekt], [Zone], [BezugMWh], [EinspPvMWh], " +
                    "[KwkEigenMWh], [KwkEinspMWh], [MaxBezugKW], [BedarfMWh], [Zeitstempel]) " +
                    "VALUES (?, ?, ?, ?, ?, 0, 0, ?, ?, ?)",
                    new DbParam("@id", ersteId + i), new DbParam("@p", projekt),
                    new DbParam("@z", ZeitzonentarifAbloesung.ZONEN[i]),
                    new DbParam("@b", 1.25), new DbParam("@pv", 0.5), new DbParam("@mx", i == 0 ? 8.4 : 5.0),
                    new DbParam("@bd", 2.0), new DbParam("@t", i == 0 ? "2026-09-22 10:00:00" : "2026-09-20 10:00:00"));
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

        private static bool SatzVorhanden(int projekt) =>
            Zahl("SELECT COUNT(*) FROM [Tab_ProjektTarif] WHERE [ID_Projekt] = ?", projekt) > 0;

        private static bool Aktiv(int projekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [Aktiv] FROM [Tab_ProjektTarif] WHERE [ID_Projekt] = ?", new DbParam("@p", projekt));
            return o != null && o != DBNull.Value && Convert.ToInt32(o) == 1;
        }

        private static int Zeilen(string tabelle, int projekt) =>
            Zahl("SELECT COUNT(*) FROM [" + tabelle + "] WHERE [ID_Projekt] = ?", projekt);

        private static int Zahl(string sql, int projekt)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@p", projekt));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
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
                "SELECT * FROM [" + TAB_MATRIX + "] WHERE [ID_Projekt] = ? ORDER BY [ID]",
                new DbParam("@p", projekt));
            Assert.NotNull(t);
            return t;
        }
    }
}
