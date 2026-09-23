using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER ZEITZONENTARIF WIRD ABGELÖST - Migrationsschritt 103
    //
    // WOZU. Entscheid Q11 (Anwender 22.09.2026, "kein HT/NT"; Register R-Q, Q11): Der
    // Zeitzonentarif Winter/Sommer x HT/NT wird nicht mehr gefuehrt. Die zweistufige
    // Leistungspreis-Staffel, die bis dahin im Tarifsatz stand (Tab_ProjektTarif) und
    // nur im Zonenmodell rechnete, zieht in die Kostenverwaltung neben die
    // Energiepreisstruktur (Weg 2 aus Nach #291) - an die Projektuebersteuerung des
    // Stromtraegers (energy_project_settings, Spalten aus
    // SchemaKatalog.Schritt103_LeistungspreisStaffel).
    //
    // WAS DER SCHRITT TUT, in dieser Reihenfolge:
    //  (1) STAFFEL UEBERNEHMEN - aus jedem Tarifsatz, in dem sie RECHNETE: aktiv, im
    //      Zonenmodell (Tarif_Modus leer oder ZONEN), mindestens ein Zonen-Bezugspreis
    //      gepflegt (sonst fiel die Rechnung schon damals auf die Flat-Preise zurueck)
    //      und mindestens ein Staffelpreis gepflegt. Der Satz galt fuer die ganze
    //      Vergleichsgruppe; uebernommen wird deshalb an den Stromtraeger JEDER Version
    //      (Stamm und Varianten aus Tab_Variante), an die Zeile, die den Netzbezug
    //      bepreist (Emissionsquelle.StromTraeger). Geschrieben wird nur, wo die drei
    //      neuen Spalten noch leer sind - eine schon gepflegte Staffel bleibt. Fehlt der
    //      Version eine zugeordnete Stromtraegerzeile, wird NICHTS angelegt (eine neue
    //      Zeile machte einen Rueckfalltraeger still zum zugeordneten) - die Version
    //      steht dann benannt im Bericht.
    //  (2) ZONENSAETZE ABSCHALTEN - jeder aktive Satz im Zonenmodell bekommt Aktiv = 0.
    //      Er rechnet ohnehin nicht mehr (WirtschaftlichkeitCtrl.BaueEingabe); ohne den
    //      Schalter hinge an seinem Projekt fuer immer der Hinweis
    //      WIRT_HINWEIS_ZEITZONENTARIF, und fuer ein Waermepumpenprojekt ohne BHKW und
    //      ohne Photovoltaik gaebe es keinen Weg mehr, ihn abzuschalten. Die Zeile
    //      selbst bleibt (kein DDL, die Tabelle raeumt ein spaeterer Schritt auf).
    //  (3) STROMMATRIX ZUSAMMENFASSEN - die gespeicherten Zeilen der vier Tarifzonen je
    //      Projekt werden zu EINER Jahreszeile (StromMatrix.ZEILE_JAHR): Summen der
    //      Mengen, Maximum der Stundenlast, der juengste Zeitstempel. Der Leser
    //      (WirtschaftlichkeitCtrl.LadeStromMatrix) summierte sie ohnehin; so bleiben sie
    //      nicht als Zonen stehen.
    //
    // ERGEBNIS. In der Testdatenbank traegt kein Projekt einen Tarifsatz - (1) und (2)
    // finden nichts; (3) fasst die Altzeilen der Projekte 1018 und 1031 zusammen. Wo ein
    // Satz rechnete, aendert sich die Rechnung erst mit dem naechsten Lauf: der
    // Arbeitspreis des Stromtraegers statt der Zonenpreise, die Staffel an der
    // Viertelstunden- statt an der Stundenspitze, die Einspeiseverguetung der Parameter
    // statt der Zonen-Einspeisepreise. Der Bericht des Schritts nennt jeden Satz.
    //
    // WIEDERHOLBAR. Ein zweiter Lauf findet keinen aktiven Zonensatz und keine
    // Zonenzeile mehr und fasst nichts an.
    //
    // EINE QUELLE fuer drei Leser: den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests (TestDatenbank
    // zieht jede Arbeitskopie damit nach).
    // ====================================================================================

    /// <summary>
    /// Schemaschritt 103 — der Zeitzonentarif HT/NT wird abgelöst (Entscheid Q11): die
    /// Leistungspreis-Staffel zieht an den Stromträger der Kostenverwaltung, die
    /// Tarifsätze des Zonenmodells werden abgeschaltet, die Zonenzeilen der Strommatrix
    /// werden eine Jahreszeile. EINE Quelle für Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class ZeitzonentarifAbloesung
    {
        /// <summary>Der Tarifsatz je Stammprojekt.</summary>
        public const string TAB_TARIF = SchemaKatalog.TAB_PROJEKTTARIF;

        /// <summary>Die gespeicherte Strommatrix.</summary>
        public const string TAB_MATRIX = WirtschaftlichkeitCtrl.TAB_MATRIX;

        /// <summary>Die Projektübersteuerung der Energieträger.</summary>
        public const string TAB_EINSTELLUNG = SchemaKatalog.ENERGY_PROJECT_SETTINGS;

        /// <summary>Die Spalte des Tarifmodus (Schritt 21); fehlt sie, ist jeder Satz ein
        /// Satz des Zonenmodells.</summary>
        public const string SPALTE_MODUS = SchemaKatalog.SPALTE_TARIF_MODUS;

        /// <summary>
        /// Die vier Schlüssel, unter denen die Strommatrix bis E7b je Tarifzone
        /// gespeichert wurde — wortgleich zu den gelöschten Konstanten
        /// <c>StromMatrix.Z_WINTER_HT</c> … <c>Z_SOMMER_NT</c>.
        /// </summary>
        public static readonly string[] ZONEN = { "Winter HT", "Winter NT", "Sommer HT", "Sommer NT" };

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>Bedingung „Satz im Zonenmodell" — ohne Modusspalte jeder Satz.</summary>
        private static string Zonenbedingung(bool mitModus)
        {
            return mitModus
                ? "COALESCE([" + SPALTE_MODUS + "], '') IN ('', ?)"
                : "1 = 1";
        }

        /// <summary>Die Sätze, deren Staffel rechnete: aktiv, Zonenmodell, ein
        /// Zonen-Bezugspreis und ein Staffelpreis gepflegt.</summary>
        private static string SqlStaffelquellen(bool mitModus)
        {
            return "SELECT [ID_Projekt], [Staffel_Grenze], [Staffel_Preis1], [Staffel_Preis2] " +
                   "FROM [" + TAB_TARIF + "] WHERE [Aktiv] = 1 AND " + Zonenbedingung(mitModus) +
                   " AND (COALESCE([Bezug_W_HT], 0) > 0 OR COALESCE([Bezug_W_NT], 0) > 0 OR " +
                   "COALESCE([Bezug_S_HT], 0) > 0 OR COALESCE([Bezug_S_NT], 0) > 0) " +
                   "AND (COALESCE([Staffel_Preis1], 0) > 0 OR COALESCE([Staffel_Preis2], 0) > 0) " +
                   "ORDER BY [ID_Projekt]";
        }

        /// <summary>Die aktiven Sätze des Zonenmodells (Zählung und Protokoll).</summary>
        private static string SqlZonensaetze(bool mitModus)
        {
            return "SELECT [ID_Projekt] FROM [" + TAB_TARIF + "] WHERE [Aktiv] = 1 AND " +
                   Zonenbedingung(mitModus) + " ORDER BY [ID_Projekt]";
        }

        /// <summary>Schaltet die aktiven Sätze des Zonenmodells ab.</summary>
        private static string SqlZonensaetzeAbschalten(bool mitModus)
        {
            return "UPDATE [" + TAB_TARIF + "] SET [Aktiv] = 0 WHERE [Aktiv] = 1 AND " +
                   Zonenbedingung(mitModus);
        }

        /// <summary>Die Versionen einer Vergleichsgruppe neben dem Stamm.</summary>
        public const string SQL_VERSIONEN =
            "SELECT [ID_Projekt] FROM [" + SchemaKatalog.TAB_VARIANTE + "] WHERE [ID_ProjektRef] = ? " +
            "ORDER BY [ID_Projekt]";

        /// <summary>Steht die Stromträgerzeile der Version, und sind ihre Staffelspalten leer?</summary>
        public const string SQL_ZIEL_ZAEHLEN =
            "SELECT COUNT(*) FROM [" + TAB_EINSTELLUNG + "] WHERE [ID_Projekt] = ? AND [ID_Energieträger] = ?";

        /// <summary>Schreibt die Staffel — nur, wo alle drei Spalten noch leer sind.</summary>
        public const string SQL_STAFFEL_SETZEN =
            "UPDATE [" + TAB_EINSTELLUNG + "] SET [" + SchemaKatalog.SPALTE_LP_STAFFEL_GRENZE + "] = ?, [" +
            SchemaKatalog.SPALTE_LP_STAFFEL_PREIS1 + "] = ?, [" + SchemaKatalog.SPALTE_LP_STAFFEL_PREIS2 + "] = ? " +
            "WHERE [ID_Projekt] = ? AND [ID_Energieträger] = ? AND [" + SchemaKatalog.SPALTE_LP_STAFFEL_GRENZE +
            "] IS NULL AND [" + SchemaKatalog.SPALTE_LP_STAFFEL_PREIS1 + "] IS NULL AND [" +
            SchemaKatalog.SPALTE_LP_STAFFEL_PREIS2 + "] IS NULL";

        /// <summary>Wie viele Zeilen der Strommatrix tragen noch einen Zonenschlüssel?</summary>
        public const string SQL_ZONENZEILEN_ZAEHLEN =
            "SELECT COUNT(*) FROM [" + TAB_MATRIX + "] WHERE [Zone] IN (?, ?, ?, ?)";

        /// <summary>Die Jahressummen der Zonenzeilen je Projekt.</summary>
        public const string SQL_ZONENZEILEN_SUMMEN =
            "SELECT [ID_Projekt], SUM([BezugMWh]) AS Bezug, SUM([EinspPvMWh]) AS EinspPv, " +
            "SUM([KwkEigenMWh]) AS KwkEigen, SUM([KwkEinspMWh]) AS KwkEinsp, " +
            "MAX([MaxBezugKW]) AS MaxBezug, SUM([BedarfMWh]) AS Bedarf, MAX([Zeitstempel]) AS Zeit, " +
            "COUNT(*) AS Zeilen FROM [" + TAB_MATRIX + "] WHERE [Zone] IN (?, ?, ?, ?) " +
            "GROUP BY [ID_Projekt] ORDER BY [ID_Projekt]";

        /// <summary>Die neue Jahreszeile eines Projekts.</summary>
        public const string SQL_JAHRESZEILE =
            "INSERT INTO [" + TAB_MATRIX + "] ([ID], [ID_Projekt], [Zone], [BezugMWh], [EinspPvMWh], " +
            "[KwkEigenMWh], [KwkEinspMWh], [MaxBezugKW], [BedarfMWh], [Zeitstempel]) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

        /// <summary>Die Zonenzeilen eines Projekts entfernen.</summary>
        public const string SQL_ZONENZEILEN_LOESCHEN =
            "DELETE FROM [" + TAB_MATRIX + "] WHERE [ID_Projekt] = ? AND [Zone] IN (?, ?, ?, ?)";

        /// <summary>Die nächste freie Kennung der Strommatrix.</summary>
        public const string SQL_MATRIX_MAX_ID = "SELECT MAX([ID]) FROM [" + TAB_MATRIX + "]";

        /// <summary>Die vier Zonenschlüssel als gebundene Werte.</summary>
        private static DbParam[] Zonenparameter(params DbParam[] davor)
        {
            var l = new List<DbParam>(davor);
            for (int i = 0; i < ZONEN.Length; i++) l.Add(new DbParam("@z" + i, ZONEN[i]));
            return l.ToArray();
        }

        private static DbParam[] Modusparameter(bool mitModus)
        {
            return mitModus
                ? new[] { new DbParam("@m", DbWerte.TARIF_MODUS_ZONEN) }
                : new DbParam[0];
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Gibt es den Tarifsatz überhaupt? Er entsteht erst mit dem ersten
        /// Öffnen der Wirtschaftlichkeit (Selbst-DDL des Controllers).</summary>
        public static bool TarifVorhanden()
        {
            return DataRepository.TabelleVorhanden(TAB_TARIF);
        }

        /// <summary>Gibt es die gespeicherte Strommatrix?</summary>
        public static bool MatrixVorhanden()
        {
            return DataRepository.TabelleVorhanden(TAB_MATRIX);
        }

        /// <summary>Stehen die drei Staffelspalten (DDL-Teil des Schrittes)?</summary>
        public static bool StaffelspaltenVorhanden()
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt103_LeistungspreisStaffel)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
            return true;
        }

        private static bool MitModus()
        {
            return DataRepository.SpalteVorhanden(TAB_TARIF, SPALTE_MODUS);
        }

        /// <summary>Wie viele Tarifsätze stehen noch AKTIV im Zonenmodell? Genau so viele
        /// schaltet der Schritt ab. 0 = nichts zu tun.</summary>
        public static int OffeneZonensaetze()
        {
            if (!TarifVorhanden()) return 0;
            try
            {
                bool m = MitModus();
                DataTable t = DataRepository.GetDataTable(SqlZonensaetze(m), Modusparameter(m));
                return t == null ? 0 : t.Rows.Count;
            }
            catch { return 0; }
        }

        /// <summary>Wie viele Zeilen der Strommatrix tragen noch einen Zonenschlüssel?
        /// 0 = nichts zu tun.</summary>
        public static int OffeneZonenzeilen()
        {
            if (!MatrixVorhanden()) return 0;
            try
            {
                object o = DataRepository.ExecuteScalar(SQL_ZONENZEILEN_ZAEHLEN, Zonenparameter());
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        /// <summary>Eine Staffel, die der Schritt übernimmt: aus dem Satz des Stammes an
        /// den Stromträger einer Version.</summary>
        public sealed class Uebernahme
        {
            /// <summary>Das Stammprojekt, dessen Tarifsatz die Staffel trug.</summary>
            public int IdStamm;
            /// <summary>Die Version, an deren Stromträger sie geht (auch der Stamm).</summary>
            public int IdProjekt;
            /// <summary>Der Stromträger der Version; 0 = keiner zugeordnet.</summary>
            public int IdTraeger;
            /// <summary>Staffelgrenze [kW].</summary>
            public double Grenze;
            /// <summary>Preis bis zur Grenze [€/(kW·a)].</summary>
            public double Preis1;
            /// <summary>Preis über der Grenze [€/(kW·a)].</summary>
            public double Preis2;
            /// <summary>Warum nicht übernommen wird; <c>null</c> = wird übernommen.</summary>
            public string Grund;

            /// <summary>Die Protokollzeile.</summary>
            public override string ToString()
            {
                string staffel = "Grenze " + Grenze.ToString("0.###", CultureInfo.InvariantCulture) +
                                 " kW, " + Preis1.ToString("0.###", CultureInfo.InvariantCulture) + " / " +
                                 Preis2.ToString("0.###", CultureInfo.InvariantCulture) + " EUR/(kW*a)";
                return "Projekt " + IdProjekt.ToString(CultureInfo.InvariantCulture) +
                       (IdProjekt != IdStamm ? " (Version von " + IdStamm.ToString(CultureInfo.InvariantCulture) + ")" : "") +
                       ", Stromtraeger " + IdTraeger.ToString(CultureInfo.InvariantCulture) + ": " + staffel +
                       (Grund == null ? "" : " - NICHT uebernommen: " + Grund);
            }
        }

        /// <summary>
        /// Die geplanten Übernahmen — gelesen, bevor der Schritt schreibt. Je Satz, in dem
        /// die Staffel rechnete, eine Zeile je Version der Gruppe; eine Zeile mit
        /// <see cref="Uebernahme.Grund"/> wird nicht geschrieben.
        /// </summary>
        public static List<Uebernahme> Uebernahmen()
        {
            var liste = new List<Uebernahme>();
            if (!TarifVorhanden()) return liste;

            bool m = MitModus();
            DataTable quellen = DataRepository.GetDataTable(SqlStaffelquellen(m), Modusparameter(m));
            if (quellen == null) return liste;

            bool spalten = StaffelspaltenVorhanden();
            foreach (DataRow r in quellen.Rows)
            {
                int stamm = Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture);
                double grenze = Zahl(r, "Staffel_Grenze");
                double p1 = Zahl(r, "Staffel_Preis1");
                double p2 = Zahl(r, "Staffel_Preis2");

                var gruppe = new List<int> { stamm };
                DataTable versionen = DataRepository.GetDataTable(SQL_VERSIONEN, new DbParam("@s", stamm));
                if (versionen != null)
                    foreach (DataRow v in versionen.Rows)
                    {
                        int id = Convert.ToInt32(v["ID_Projekt"], CultureInfo.InvariantCulture);
                        if (id > 0 && !gruppe.Contains(id)) gruppe.Add(id);
                    }

                foreach (int id in gruppe)
                {
                    var u = new Uebernahme
                    {
                        IdStamm = stamm, IdProjekt = id, Grenze = grenze, Preis1 = p1, Preis2 = p2,
                        IdTraeger = Emissionsquelle.StromTraeger(id)
                    };
                    if (!spalten)
                        u.Grund = "die Staffelspalten fehlen";
                    else if (u.IdTraeger <= 0)
                        u.Grund = "kein Stromtraeger zugeordnet";
                    else if (Anzahl(SQL_ZIEL_ZAEHLEN, new DbParam("@p", id), new DbParam("@c", u.IdTraeger)) <= 0)
                        u.Grund = "keine Zeile des Stromtraegers in " + TAB_EINSTELLUNG;
                    liste.Add(u);
                }
            }
            return liste;
        }

        /// <summary>Die Projekte der aktiven Sätze im Zonenmodell (Protokoll).</summary>
        public static List<int> Zonensaetze()
        {
            var liste = new List<int>();
            if (!TarifVorhanden()) return liste;
            bool m = MitModus();
            DataTable t = DataRepository.GetDataTable(SqlZonensaetze(m), Modusparameter(m));
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture));
            return liste;
        }

        // =================================================================
        //  Der Schritt
        // =================================================================

        /// <summary>Was ein Lauf getan hat — für Migrationsprotokoll, Werkzeug und Nachweis.</summary>
        public sealed class Bericht
        {
            /// <summary>Die Übernahmen samt der nicht geschriebenen (mit Grund).</summary>
            public List<Uebernahme> Uebernahmen = new List<Uebernahme>();
            /// <summary>Zeilen der Stromträger, die eine Staffel bekommen haben.</summary>
            public int StaffelnGeschrieben;
            /// <summary>Die Stammprojekte der abgeschalteten Zonensätze.</summary>
            public List<int> Abgeschaltet = new List<int>();
            /// <summary>Projekte, deren Zonenzeilen zu einer Jahreszeile wurden.</summary>
            public List<int> MatrixProjekte = new List<int>();
            /// <summary>Zonenzeilen, die dabei entfielen.</summary>
            public int MatrixZeilenEntfallen;

            /// <summary>Die Notiz des Schrittes, ohne Umbrüche.</summary>
            public string Text()
            {
                var teile = new List<string>();
                int nicht = 0;
                foreach (Uebernahme u in Uebernahmen) if (u.Grund != null) nicht++;
                teile.Add("Leistungspreis-Staffel: " + StaffelnGeschrieben.ToString(CultureInfo.InvariantCulture) +
                          " Stromtraegerzeile(n) uebernommen" +
                          (nicht > 0 ? ", " + nicht.ToString(CultureInfo.InvariantCulture) + " nicht" : ""));
                foreach (Uebernahme u in Uebernahmen) teile.Add(u.ToString());
                teile.Add("Tarifsaetze im Zonenmodell abgeschaltet: " +
                          Abgeschaltet.Count.ToString(CultureInfo.InvariantCulture) +
                          (Abgeschaltet.Count > 0 ? " (Projekt " + string.Join(", ", Abgeschaltet) + ")" : ""));
                teile.Add("Strommatrix: " + MatrixZeilenEntfallen.ToString(CultureInfo.InvariantCulture) +
                          " Zonenzeile(n) in " + MatrixProjekte.Count.ToString(CultureInfo.InvariantCulture) +
                          " Projekt(en) zu je einer Jahreszeile zusammengefasst" +
                          (MatrixProjekte.Count > 0 ? " (Projekt " + string.Join(", ", MatrixProjekte) + ")" : ""));
                return string.Join("; ", teile);
            }
        }

        /// <summary>
        /// Führt den Datenteil des Schrittes aus — Übernahme, Abschalten, Zusammenfassen —
        /// in EINER Transaktion; die DDL (die drei Staffelspalten) muss vorher stehen. Wirft
        /// bei einem Fehler, und dann bleibt nichts halb geschrieben.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            b.Uebernahmen = Uebernahmen();                 // lesen, bevor geschrieben wird
            b.Abgeschaltet = Zonensaetze();

            var matrix = new List<DataRow>();
            if (MatrixVorhanden())
            {
                DataTable t = DataRepository.GetDataTable(SQL_ZONENZEILEN_SUMMEN, Zonenparameter());
                if (t != null) foreach (DataRow r in t.Rows) matrix.Add(r);
            }

            if (b.Uebernahmen.Count == 0 && b.Abgeschaltet.Count == 0 && matrix.Count == 0)
                return b;                                   // nichts zu tun - wiederholbar

            bool mitModus = TarifVorhanden() && MitModus();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (Uebernahme u in b.Uebernahmen)
                    {
                        if (u.Grund != null) continue;
                        int n = v.Ausfuehren(SQL_STAFFEL_SETZEN,
                            new DbParam("@g", u.Grenze), new DbParam("@p1", u.Preis1), new DbParam("@p2", u.Preis2),
                            new DbParam("@p", u.IdProjekt), new DbParam("@c", u.IdTraeger));
                        if (n > 0) b.StaffelnGeschrieben += n;
                        else u.Grund = "die Stromtraegerzeile fuehrt schon eine Staffel";
                    }

                    if (b.Abgeschaltet.Count > 0)
                        v.Ausfuehren(SqlZonensaetzeAbschalten(mitModus), Modusparameter(mitModus));

                    if (matrix.Count > 0)
                    {
                        object o = v.Skalar(SQL_MATRIX_MAX_ID);
                        int id = (o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture)) + 1;
                        foreach (DataRow r in matrix)
                        {
                            int projekt = Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture);
                            v.Ausfuehren(SQL_JAHRESZEILE,
                                new DbParam("@id", id),
                                new DbParam("@p", projekt),
                                new DbParam("@z", StromMatrix.ZEILE_JAHR),
                                new DbParam("@b", Math.Round(Zahl(r, "Bezug"), 3)),
                                new DbParam("@pv", Math.Round(Zahl(r, "EinspPv"), 3)),
                                new DbParam("@ke", Math.Round(Zahl(r, "KwkEigen"), 3)),
                                new DbParam("@ki", Math.Round(Zahl(r, "KwkEinsp"), 3)),
                                new DbParam("@mx", Math.Round(Zahl(r, "MaxBezug"), 1)),
                                new DbParam("@bd", Math.Round(Zahl(r, "Bedarf"), 3)),
                                new DbParam("@zeit", r["Zeit"] == DBNull.Value ? (object)DBNull.Value
                                                                                : Convert.ToString(r["Zeit"], CultureInfo.InvariantCulture)));
                            id++;
                            b.MatrixZeilenEntfallen += v.Ausfuehren(SQL_ZONENZEILEN_LOESCHEN,
                                Zonenparameter(new DbParam("@p", projekt)));
                            b.MatrixProjekte.Add(projekt);
                        }
                    }

                    v.Commit();
                }
                catch
                {
                    try { v.Rollback(); } catch { }
                    throw;
                }
            }
            return b;
        }

        // =================================================================
        //  Kleinwerkzeug
        // =================================================================

        private static double Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static int Anzahl(string sql, params DbParam[] p)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql, p);
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }
    }
}
