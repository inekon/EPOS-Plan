using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER ZEITZONENTARIF WIRD ABGELÖST - Migrationsschritt 104
    //
    // WOZU. Entscheid Q11 (Anwender 22.09.2026, "kein HT/NT"; Register R-Q, Q11): Der
    // Zeitzonentarif Winter/Sommer x HT/NT wird nicht mehr gefuehrt. Die zweistufige
    // Leistungspreis-Staffel, die bis dahin im Tarifsatz stand (Tab_ProjektTarif) und
    // nur im Zonenmodell rechnete, zieht in die Kostenverwaltung neben die
    // Energiepreisstruktur (Weg 2 aus Nach #291) - an die Projektuebersteuerung des
    // Stromtraegers (energy_project_settings, Spalten aus
    // SchemaKatalog.Schritt104_LeistungspreisStaffel). Entscheid E7b-Q4 (Anwender
    // 23.09.2026, "alte Tarife verwerfen, nicht mehr relevant"): Die Saetze des
    // Zonenmodells und die mit ihnen gerechneten Ergebnisse werden verworfen.
    //
    // WAS EIN SATZ DES ZONENMODELLS IST. Jeder Satz, dessen Tarif_Modus (getrimmt) nicht
    // ROLLEN heisst - leer, NULL, ZONEN oder ein unbekannter Wert. Genau diese Saetze
    // rechnete der Kern bis E7b im Zonenpfad (TarifParameter.RollenModus vergleicht mit
    // ROLLEN, alles andere war Zonenmodell). Fehlt die Modusspalte (Bestand vor
    // Schritt 21), ist jeder Satz einer.
    //
    // WAS DER SCHRITT TUT, in dieser Reihenfolge und in EINER Transaktion:
    //  (1) STAFFEL UEBERNEHMEN - aus jedem Satz des Zonenmodells, in dem sie RECHNETE:
    //      aktiv, mindestens ein Zonen-Bezugspreis gepflegt (sonst fiel die Rechnung
    //      schon damals auf die Flat-Preise zurueck) und mindestens ein Staffelpreis
    //      gepflegt. Der Satz galt fuer die ganze Vergleichsgruppe; uebernommen wird
    //      deshalb an den Stromtraeger JEDER Version (Stamm und Varianten aus
    //      Tab_Variante), an die Zeile, die den Netzbezug bepreist
    //      (Emissionsquelle.StromTraeger). Geschrieben wird nur, wo die drei neuen
    //      Spalten noch leer sind - eine schon gepflegte Staffel bleibt. Fehlt der Version
    //      eine zugeordnete Stromtraegerzeile, wird NICHTS angelegt (eine neue Zeile
    //      machte einen Rueckfalltraeger still zum zugeordneten) - die Version steht dann
    //      benannt im Bericht.
    //  (2) ZONENSAETZE LOESCHEN - jeder Satz des Zonenmodells, aktiv oder nicht, wird aus
    //      Tab_ProjektTarif geloescht. Ein Satz im Rollenmodell bleibt unberuehrt.
    //  (3) ERGEBNISSE MIT ZONENTARIF VERWERFEN - die KENNZEICHNUNG: ein gespeichertes
    //      Ergebnis (Tab_ErgebnisWirtschaftlichkeit) mit gefuellter Spalte
    //      StromkostenTarif, dessen Projekt zur Gruppe eines Satzes im Zonenmodell gehoert.
    //      StromkostenTarif fuellt nur der Tarifweg: im Zonenmodell mit den
    //      Zonen-Bezugskosten samt Staffel, im Rollenmodell mit dem Reststrom - deshalb
    //      die Gruppenbedingung, die ein Rollenergebnis ausschliesst. Ein Ergebnis ohne
    //      StromkostenTarif rechnete mit den Flat-Preisen und bleibt. Verworfen wird der
    //      GANZE gespeicherte Lauf des Projekts, so wie Persistiere ihn als Einheit
    //      schreibt: Ergebnis (alle Szenarien), Sensitivitaet und Strommatrix.
    //  (4) STROMMATRIX ZUSAMMENFASSEN - die gespeicherten Zeilen der vier Tarifzonen je
    //      Projekt werden zu EINER Jahreszeile (StromMatrix.ZEILE_JAHR): Summen der
    //      Mengen, Maximum der Stundenlast, der juengste Zeitstempel. Der Leser
    //      (WirtschaftlichkeitCtrl.LadeStromMatrix) summierte sie ohnehin; so bleiben sie
    //      nicht als Zonen stehen. Projekte aus (3) haben keine Matrix mehr.
    //
    // ERGEBNIS. In der Testdatenbank traegt kein Projekt einen Tarifsatz - (1) bis (3)
    // finden nichts; (4) fasst die Altzeilen der Projekte 1018 und 1031 zusammen. Wo ein
    // Satz rechnete, rechnet der naechste Lauf mit dem Arbeitspreis des Stromtraegers
    // statt der Zonenpreise, mit der Staffel an der Viertelstunden- statt an der
    // Stundenspitze und mit der Einspeiseverguetung der Parameter statt der
    // Zonen-Einspeisepreise. Der Bericht des Schritts nennt jeden Satz und jedes Projekt.
    //
    // WIEDERHOLBAR. Ein zweiter Lauf findet keinen Satz des Zonenmodells und keine
    // Zonenzeile mehr und fasst nichts an. Eine Datenbank VOR dem Schritt meldet einen
    // aktiven Zonensatz weiter am Ergebnis (WIRT_HINWEIS_ZEITZONENTARIF) - der Waechter
    // fuer einen nicht migrierten Stand.
    //
    // EINE QUELLE fuer drei Leser: den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests (TestDatenbank
    // zieht jede Arbeitskopie damit nach).
    // ====================================================================================

    /// <summary>
    /// Schemaschritt 104 — der Zeitzonentarif HT/NT wird abgelöst (Entscheide Q11 und
    /// E7b‑Q4): die Leistungspreis-Staffel zieht an den Stromträger der Kostenverwaltung,
    /// die Tarifsätze des Zonenmodells und die mit ihnen gerechneten gespeicherten
    /// Ergebnisse werden verworfen, die Zonenzeilen der Strommatrix werden eine
    /// Jahreszeile. EINE Quelle für Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class ZeitzonentarifAbloesung
    {
        /// <summary>Der Tarifsatz je Stammprojekt.</summary>
        public const string TAB_TARIF = SchemaKatalog.TAB_PROJEKTTARIF;

        /// <summary>Die gespeicherten Ergebnisse der Wirtschaftlichkeit.</summary>
        public const string TAB_ERGEBNIS = WirtschaftlichkeitCtrl.TAB_ERGEBNIS;

        /// <summary>Die gespeicherte Sensitivität.</summary>
        public const string TAB_SENS = WirtschaftlichkeitCtrl.TAB_SENS;

        /// <summary>Die gespeicherte Strommatrix.</summary>
        public const string TAB_MATRIX = WirtschaftlichkeitCtrl.TAB_MATRIX;

        /// <summary>Die Projektübersteuerung der Energieträger.</summary>
        public const string TAB_EINSTELLUNG = SchemaKatalog.ENERGY_PROJECT_SETTINGS;

        /// <summary>Die Spalte des Tarifmodus (Schritt 21); fehlt sie, ist jeder Satz ein
        /// Satz des Zonenmodells.</summary>
        public const string SPALTE_MODUS = SchemaKatalog.SPALTE_TARIF_MODUS;

        /// <summary>Die Kennzeichnung eines mit einem Tarif gerechneten Ergebnisses.</summary>
        public const string SPALTE_STROMKOSTEN_TARIF = "StromkostenTarif";

        /// <summary>
        /// Die vier Schlüssel, unter denen die Strommatrix bis E7b je Tarifzone
        /// gespeichert wurde — wortgleich zu den gelöschten Konstanten
        /// <c>StromMatrix.Z_WINTER_HT</c> … <c>Z_SOMMER_NT</c>.
        /// </summary>
        public static readonly string[] ZONEN = { "Winter HT", "Winter NT", "Sommer HT", "Sommer NT" };

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Bedingung „Satz im Zonenmodell": jeder Satz, der nicht im Rollenmodell steht —
        /// dieselbe Grenze wie <c>TarifParameter.RollenModus</c>. Ohne Modusspalte jeder Satz.
        /// </summary>
        private static string Zonenbedingung(bool mitModus)
        {
            return mitModus
                ? "TRIM(COALESCE([" + SPALTE_MODUS + "], '')) <> ?"
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

        /// <summary>Alle Sätze des Zonenmodells, aktiv oder nicht (Zählung und Protokoll).</summary>
        private static string SqlZonensaetze(bool mitModus)
        {
            return "SELECT [ID_Projekt] FROM [" + TAB_TARIF + "] WHERE " +
                   Zonenbedingung(mitModus) + " ORDER BY [ID_Projekt]";
        }

        /// <summary>Löscht alle Sätze des Zonenmodells; ein Rollensatz bleibt.</summary>
        private static string SqlZonensaetzeLoeschen(bool mitModus)
        {
            return "DELETE FROM [" + TAB_TARIF + "] WHERE " + Zonenbedingung(mitModus);
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

        /// <summary>Wie viele gespeicherte Ergebniszeilen eines Projekts rechneten mit einem Tarif?</summary>
        public const string SQL_TARIFERGEBNISSE_ZAEHLEN =
            "SELECT COUNT(*) FROM [" + TAB_ERGEBNIS + "] WHERE [ID_Projekt] = ? AND [" +
            SPALTE_STROMKOSTEN_TARIF + "] IS NOT NULL";

        /// <summary>Der gespeicherte Lauf eines Projekts — Ergebnis, Sensitivität, Matrix.</summary>
        public const string SQL_ERGEBNIS_LOESCHEN = "DELETE FROM [" + TAB_ERGEBNIS + "] WHERE [ID_Projekt] = ?";

        /// <inheritdoc cref="SQL_ERGEBNIS_LOESCHEN"/>
        public const string SQL_SENS_LOESCHEN = "DELETE FROM [" + TAB_SENS + "] WHERE [ID_Projekt] = ?";

        /// <inheritdoc cref="SQL_ERGEBNIS_LOESCHEN"/>
        public const string SQL_MATRIX_LOESCHEN = "DELETE FROM [" + TAB_MATRIX + "] WHERE [ID_Projekt] = ?";

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
                ? new[] { new DbParam("@m", DbWerte.TARIF_MODUS_ROLLEN) }
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

        /// <summary>Gibt es gespeicherte Ergebnisse mit der Kennzeichnung
        /// <see cref="SPALTE_STROMKOSTEN_TARIF"/>? Ohne die Spalte rechnete nie ein Tarif.</summary>
        public static bool TarifergebnisseLesbar()
        {
            return DataRepository.TabelleVorhanden(TAB_ERGEBNIS) &&
                   DataRepository.SpalteVorhanden(TAB_ERGEBNIS, SPALTE_STROMKOSTEN_TARIF);
        }

        /// <summary>Stehen die drei Staffelspalten (DDL-Teil des Schrittes)?</summary>
        public static bool StaffelspaltenVorhanden()
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt104_LeistungspreisStaffel)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
            return true;
        }

        private static bool MitModus()
        {
            return DataRepository.SpalteVorhanden(TAB_TARIF, SPALTE_MODUS);
        }

        /// <summary>Wie viele Tarifsätze stehen noch im Zonenmodell, aktiv oder nicht? Genau
        /// so viele löscht der Schritt. 0 = nichts zu tun.</summary>
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

        /// <summary>Stamm und Versionen einer Vergleichsgruppe (Tab_Variante).</summary>
        public static List<int> Gruppe(int stamm)
        {
            var gruppe = new List<int> { stamm };
            if (!DataRepository.TabelleVorhanden(SchemaKatalog.TAB_VARIANTE)) return gruppe;
            DataTable versionen = DataRepository.GetDataTable(SQL_VERSIONEN, new DbParam("@s", stamm));
            if (versionen != null)
                foreach (DataRow v in versionen.Rows)
                {
                    int id = Convert.ToInt32(v["ID_Projekt"], CultureInfo.InvariantCulture);
                    if (id > 0 && !gruppe.Contains(id)) gruppe.Add(id);
                }
            return gruppe;
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

                foreach (int id in Gruppe(stamm))
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

        /// <summary>Die Stammprojekte aller Sätze im Zonenmodell, aktiv oder nicht.</summary>
        public static List<int> Zonensaetze()
        {
            var liste = new List<int>();
            if (!TarifVorhanden()) return liste;
            bool m = MitModus();
            DataTable t = DataRepository.GetDataTable(SqlZonensaetze(m), Modusparameter(m));
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
            {
                int id = Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture);
                if (!liste.Contains(id)) liste.Add(id);
            }
            return liste;
        }

        /// <summary>
        /// Die Projekte, deren gespeicherter Lauf mit einem Zonentarif gerechnet wurde:
        /// Projekte der Gruppen der <paramref name="zonensaetze"/> mit mindestens einer
        /// Ergebniszeile, die <see cref="SPALTE_STROMKOSTEN_TARIF"/> trägt.
        /// </summary>
        public static List<int> Zonenergebnisse(List<int> zonensaetze)
        {
            var liste = new List<int>();
            if (zonensaetze == null || zonensaetze.Count == 0 || !TarifergebnisseLesbar()) return liste;
            foreach (int stamm in zonensaetze)
                foreach (int id in Gruppe(stamm))
                    if (!liste.Contains(id) && Anzahl(SQL_TARIFERGEBNISSE_ZAEHLEN, new DbParam("@p", id)) > 0)
                        liste.Add(id);
            liste.Sort();
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
            /// <summary>Die Stammprojekte der gelöschten Sätze des Zonenmodells.</summary>
            public List<int> Geloescht = new List<int>();
            /// <summary>Die Projekte, deren mit einem Zonentarif gerechneter Lauf verworfen wurde.</summary>
            public List<int> ErgebnisseVerworfen = new List<int>();
            /// <summary>Die dabei gelöschten Ergebniszeilen (alle Szenarien).</summary>
            public int ErgebniszeilenVerworfen;
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
                teile.Add("Tarifsaetze im Zonenmodell geloescht: " +
                          Geloescht.Count.ToString(CultureInfo.InvariantCulture) +
                          (Geloescht.Count > 0 ? " (Projekt " + string.Join(", ", Geloescht) + ")" : ""));
                teile.Add("mit Zonentarif gerechnete Ergebnisse verworfen: " +
                          ErgebniszeilenVerworfen.ToString(CultureInfo.InvariantCulture) + " Zeile(n) in " +
                          ErgebnisseVerworfen.Count.ToString(CultureInfo.InvariantCulture) + " Projekt(en)" +
                          (ErgebnisseVerworfen.Count > 0 ? " (Projekt " + string.Join(", ", ErgebnisseVerworfen) + ")" : ""));
                teile.Add("Strommatrix: " + MatrixZeilenEntfallen.ToString(CultureInfo.InvariantCulture) +
                          " Zonenzeile(n) in " + MatrixProjekte.Count.ToString(CultureInfo.InvariantCulture) +
                          " Projekt(en) zu je einer Jahreszeile zusammengefasst" +
                          (MatrixProjekte.Count > 0 ? " (Projekt " + string.Join(", ", MatrixProjekte) + ")" : ""));
                return string.Join("; ", teile);
            }
        }

        /// <summary>
        /// Führt den Datenteil des Schrittes aus — Übernahme, Löschen der Zonensätze,
        /// Verwerfen der mit ihnen gerechneten Ergebnisse, Zusammenfassen der Matrix — in
        /// EINER Transaktion; die DDL (die drei Staffelspalten) muss vorher stehen. Wirft
        /// bei einem Fehler, und dann bleibt nichts halb geschrieben.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            b.Uebernahmen = Uebernahmen();                 // lesen, bevor geschrieben wird
            b.Geloescht = Zonensaetze();
            b.ErgebnisseVerworfen = Zonenergebnisse(b.Geloescht);

            var matrix = new List<DataRow>();
            if (MatrixVorhanden())
            {
                DataTable t = DataRepository.GetDataTable(SQL_ZONENZEILEN_SUMMEN, Zonenparameter());
                if (t != null)
                    foreach (DataRow r in t.Rows)
                    {
                        // Ein verworfener Lauf verliert auch seine Matrix — keine Jahreszeile.
                        int projekt = Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture);
                        if (!b.ErgebnisseVerworfen.Contains(projekt)) matrix.Add(r);
                    }
            }

            if (b.Uebernahmen.Count == 0 && b.Geloescht.Count == 0 && matrix.Count == 0)
                return b;                                   // nichts zu tun - wiederholbar

            bool mitModus = TarifVorhanden() && MitModus();
            bool mitSens = DataRepository.TabelleVorhanden(TAB_SENS);
            bool mitMatrix = MatrixVorhanden();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // (1) Die Staffel an die Stromträger der Gruppe.
                    foreach (Uebernahme u in b.Uebernahmen)
                    {
                        if (u.Grund != null) continue;
                        int n = v.Ausfuehren(SQL_STAFFEL_SETZEN,
                            new DbParam("@g", u.Grenze), new DbParam("@p1", u.Preis1), new DbParam("@p2", u.Preis2),
                            new DbParam("@p", u.IdProjekt), new DbParam("@c", u.IdTraeger));
                        if (n > 0) b.StaffelnGeschrieben += n;
                        else u.Grund = "die Stromtraegerzeile fuehrt schon eine Staffel";
                    }

                    // (2) Die Sätze des Zonenmodells löschen — ein Rollensatz bleibt.
                    if (b.Geloescht.Count > 0)
                        v.Ausfuehren(SqlZonensaetzeLoeschen(mitModus), Modusparameter(mitModus));

                    // (3) Den mit einem Zonentarif gerechneten Lauf verwerfen — als Einheit.
                    foreach (int projekt in b.ErgebnisseVerworfen)
                    {
                        b.ErgebniszeilenVerworfen += v.Ausfuehren(SQL_ERGEBNIS_LOESCHEN, new DbParam("@p", projekt));
                        if (mitSens) v.Ausfuehren(SQL_SENS_LOESCHEN, new DbParam("@p", projekt));
                        if (mitMatrix) v.Ausfuehren(SQL_MATRIX_LOESCHEN, new DbParam("@p", projekt));
                    }

                    // (4) Die übrigen Zonenzeilen der Matrix zu je einer Jahreszeile.
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
