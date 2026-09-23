using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    public class KonfigurationCtrl : KonfigurationModel
    {
        public KonfigurationModel model = new KonfigurationModel();
        public int rows;

        public enum Energieerzeuger
        {
            BHKW = 0,
            HEIZKESSEL = 1,
            PHOTOVOLTAIK = 2,
            SOLARTHERMIE = 3,
            WAERMEPUMPE = 4
        }


        public KonfigurationCtrl()
        {
            rows = 0;
        }

        ~KonfigurationCtrl()
        {
            rows = 0;
        }

        /// <summary>
        /// Liest den Einstellungssatz EINES Projekts — die eine Wahrheit dieser
        /// Abfrage (iU9-W10b.0b und iU9-W11a.2, Befund W11-B24).
        ///
        /// <para><b>Warum es diese Methode gibt.</b> Die Zeile
        /// <c>"select * from Tab_Einstellungen where ID_Projekt=" + id</c> stand ACHTMAL
        /// im Bestand — sechsmal in <c>Form_Simulation_Detail</c>, einmal in
        /// <c>Form_Start</c> und einmal im <see cref="SimulationRunner"/>: die
        /// Projektnummer als Zeichenkette in die Anweisung geklebt, also gegen die
        /// Hausregel „Datenzugriff ausschliesslich ueber <c>DataRepository</c> mit
        /// <c>new DbParam(…)</c>". Der Weg ueber einen <see cref="DbParam"/> steht jetzt
        /// an EINER Stelle; an der Ordinalkette der Zeilenauswertung
        /// (<see cref="ZeileUebernehmen"/>) aendert sich nichts.</para>
        ///
        /// <para><b>Zwei Wellen, eine Methode.</b> W10b (Konfigurationsseite) und W11a
        /// (Ergebnisseite) haben sie gleichzeitig gebraucht und unabhaengig gebaut. Beim
        /// Zusammenfuehren ist die Signatur die des Kerns geblieben; wer ein
        /// STEUEROBJEKT fuellen will statt ein frisches Modell zu bekommen, nimmt
        /// <see cref="ProjektLesen"/>.</para>
        ///
        /// <para>Rueckgabe <c>null</c> = kein Satz zum Projekt (neues Projekt). Der
        /// Aufrufer legt dann selbst einen leeren an — genau das taten die
        /// Aufrufstellen bisher ueber <c>rows == 0</c>.</para>
        /// </summary>
        public static KonfigurationModel LiesProjekt(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            KonfigurationModel m = new KonfigurationModel();
            return ZeileUebernehmen(TabelleJeProjekt(idProjekt), m) ? m : null;
        }

        /// <summary>
        /// Dasselbe fuer ein STEUEROBJEKT: fuellt <see cref="model"/> an Ort und Stelle
        /// und setzt <see cref="rows"/> — der wortgleiche Ersatz fuer
        /// <c>ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + id)</c>.
        ///
        /// <para>Bewusst AN ORT UND STELLE und nicht mit einem frischen Modell: Die
        /// Aufrufer reichen <c>ctrl.model</c> weiter (<c>SimulationControl.ctrl_konfig</c>
        /// haelt das Steuerobjekt und liest es waehrend des Laufs). Und bewusst mit
        /// derselben Feldregel wie <see cref="ReadSingle"/>: Ein DBNull laesst den
        /// bisherigen Wert stehen.</para>
        /// </summary>
        public bool ProjektLesen(int idProjekt)
        {
            rows = 0;
            if (idProjekt <= 0) return false;
            if (!ZeileUebernehmen(TabelleJeProjekt(idProjekt), model)) return false;
            rows = 1;
            return true;
        }

        /// <summary>Die eine Abfrage — parametrisiert, nicht konkateniert.</summary>
        private static DataTable TabelleJeProjekt(int idProjekt)
        {
            return DataRepository.GetDataTable(
                "SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", idProjekt));
        }

        /// <summary>
        /// Wie <see cref="ReadSingle(string)"/>, aber mit Parametern statt zusammen-
        /// gesetztem Text (iU9-W10b.0b).
        /// </summary>
        public void ReadSingle(string sql, params DbParam[] parameter)
        {
            ReadZeile(DataRepository.GetDataTable(sql, parameter));
        }

        public void ReadSingle(string sql)
        {
            ReadZeile(DataRepository.GetDataTable(sql));
        }

        private void ReadZeile(DataTable dt)
        {
            rows = 0;

            if (ZeileUebernehmen(dt, model)) rows = 1;
        }

        /// <summary>
        /// Uebernimmt die erste Zeile einer gelesenen Tabelle in ein Modell — die
        /// Abbildung, die <see cref="ReadSingle"/> und <see cref="LiesProjekt"/> teilen.
        /// Rueckgabe <c>false</c>, wenn nichts zu uebernehmen war.
        /// </summary>
        private static bool ZeileUebernehmen(DataTable dt, KonfigurationModel model)
        {
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row[0] != DBNull.Value) model.m_ID = Convert.ToInt32(row[0]);
                if (row[1] != DBNull.Value) model.m_ID_Projekt = Convert.ToInt32(row[1]);
                if (row[2] != DBNull.Value) model.m_BHKW_Grenzleistung = Convert.ToDouble(row[2]);
                if (row[3] != DBNull.Value) model.m_Netzverluste = Convert.ToDouble(row[3]);
                if (row[4] != DBNull.Value) model.m_szNetzverlusteEinheit = row[4].ToString();
                // 16.09.2026 (Schemaschritt 79): Hier stand row[5] = WP_Heizstab, der
                // PROJEKTweite Heizstabschalter. Er ist an die Anlagenzeile gewandert
                // (Tab_Energieanlagen.Heizstab), und die Spalte ist entfernt - die
                // Ordinalkette ist damit um EINE Position nach vorn gerueckt.
                if (row[5] != DBNull.Value) model.m_Kessel_Betriebsbereitschaft = Convert.ToInt32(row[5]);
                if (row[6] != DBNull.Value) model.m_Tool_1 = row[6].ToString();
                if (row[7] != DBNull.Value) model.m_Tool_2 = row[7].ToString();
                if (row[8] != DBNull.Value) model.m_Tool_3 = row[8].ToString();
                if (row[9] != DBNull.Value) model.m_Tool_4 = row[9].ToString();
                if (row[10] != DBNull.Value) model.m_Tool_5 = row[10].ToString();
                if (row[11] != DBNull.Value) model.m_Tool_6 = row[11].ToString();
                if (row[12] != DBNull.Value) model.m_Ladefuellstand_Min = Convert.ToInt32(row[12]);
                if (row[13] != DBNull.Value) model.m_Ladefuellstand_Max = Convert.ToInt32(row[13]);
                if (row[14] != DBNull.Value) model.m_Ladeleistung_Max = Convert.ToInt32(row[14]);
                if (row[15] != DBNull.Value) model.m_Ladefuellstand_Min_Auswahl = row[15].ToString();
                if (row[16] != DBNull.Value) model.m_Ladefuellstand_Max_Auswahl = row[16].ToString();
                if (row[17] != DBNull.Value) model.m_Ladeleistung_Max_Auswahl = row[17].ToString();
                if (row[18] != DBNull.Value) model.m_Ladeschwellwert = Convert.ToDouble(row[18]);
                if (row[19] != DBNull.Value) model.Betriebsart = Convert.ToInt32(row[19]);
                if (row[20] != DBNull.Value) model.Leistungsgrenze = Convert.ToInt32(row[20]);
                if (row[21] != DBNull.Value) model.Pendelspeicher = Convert.ToDouble(row[21]);

                // PAKET L (Aufräumen): Hier stand die namensbasierte Lesung des
                // Feature-Flags Kaskade_Zweikanalig. Sie ist mit dem Feld
                // KonfigurationModel.Kaskade_Zweikanalig entfallen - seit Paket A1 gibt
                // es nur EINEN Rechenweg, und mit diesem Paket auch keinen Leser mehr.
                // Die Ordinalkette row[0..22] ist davon unberührt: Die Lesung war
                // namensbasiert und hing an keiner Position.

                // --- Einstellung Extrapolation_erlaubt (Paket 8, Konzept 13.4) --------
                //
                // NAMENSBASIERT, bewusst NICHT als row[23] an die Ordinalkette angehängt:
                // Die Kette oben ist an die physische Spaltenreihenfolge von
                // Tab_Einstellungen gebunden und damit die brüchigste Stelle des
                // Datenzugriffs - jede weitere Position macht sie nur länger. Über den
                // Spaltennamen ist der Zugriff unabhängig davon, an welcher Position die
                // Migration die Spalte angehängt hat.
                //
                // Der Wert wird in BEIDEN Zweigen gesetzt und nicht nur bei Treffer: ein
                // wiederverwendetes Model dürfte sonst den Stand des zuvor gelesenen
                // Projekts behalten. Anders als beim entfallenen Flag mit
                // UMGEKEHRTER Vorbelegung: Fehlt die Spalte (Datenbank noch nicht auf
                // Schemastand 7) oder steht dort NULL, gilt ERLAUBT. Das ist genau das
                // bisherige Verhalten - die Engine fragte nach, und die Antwort war in
                // jedem dokumentierten Lauf "Ja". Ein "verboten" darf deshalb nur aus
                // einem ausdrücklich gesetzten FALSE kommen, nie aus einer Datenlücke.
                //
                // NACHARBEIT PAKET 8, BEFUND N8 — der nie vorbelegte Zustand.
                // Es reicht nicht, fehlende Spalte und NULL abzufangen: Die Spalte steht
                // seit Paket 1 in SchemaKatalog.Schritt2_Speicher und wird deshalb auch
                // von der stillen Rückfallebene (WaermequelleClass.SchemaSicherstellen)
                // angelegt - mit dem Access-Default FALSE, denn ein Ja/Nein-Feld kennt
                // kein NULL. Auf einer Datenbank, die diese Spalte hat, aber
                // Migrationsschritt 7 noch nicht gelaufen ist, stünde damit überall
                // "verboten", und jeder extrapolierende Wärmepumpenlauf bräche ab. Genau
                // das trifft die Referenzlauf-Suite in Weg B: Der Modus "projekt"
                // migriert nicht. Solange der Schemastand unter 7 liegt, ist das FALSE
                // deshalb kein Anwenderwille, sondern eine Datenlücke - und die bedeutet
                // ERLAUBT, wie überall sonst bei dieser Einstellung.
                model.Extrapolation_erlaubt =
                    !dt.Columns.Contains(SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT) ||
                    row[SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT] == DBNull.Value ||
                    Convert.ToBoolean(row[SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT]) ||
                    ExtrapolationVorbelegungFehlt();

                // --- Kanal-Knappheitsreihenfolge (Paket K2, Konzept 4.3, F10) ---------
                //
                // Zweites Feld nach demselben namensbasierten Muster: Die Ordinalkette
                // oben endet bei row[22], und sie soll dort enden. Fehlt die Spalte
                // (Datenbank noch nicht auf Schemastand 49) oder steht dort NULL bzw.
                // ein Leerwert, gilt DbWerte.KNAPPHEIT_DEFAULT - also genau die
                // Reihenfolge, die die Kaskade vor diesem Paket fest verdrahtet kannte.
                //
                // Wie beim Feld darueber wird der Wert in BEIDEN Zweigen
                // gesetzt und nicht nur bei Treffer: ein wiederverwendetes Model
                // duerfte sonst die Reihenfolge des zuvor gelesenen Projekts behalten.
                model.Kanal_Knappheitsreihenfolge = KnappheitsreihenfolgeOderDefault(
                    dt.Columns.Contains(SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE)
                        ? row[SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE]
                        : null);

                // --- Merkspalte „Kaskade vom Anwender gepflegt" (Schemaschritt 82) -----
                //
                // Drittes Feld nach demselben namensbasierten Muster, und wieder in
                // BEIDEN Zweigen gesetzt: Ein wiederverwendetes Model duerfte die Marke
                // des zuvor gelesenen Projekts nicht behalten - sonst bliebe die
                // Automatik am naechsten Projekt aus, das sie braucht.
                //
                // Fehlende Spalte (Datenbank noch nicht auf Schemastand 82), NULL und
                // ein unlesbarer Wert heissen gleichermassen FALSCH, also „nicht
                // gepflegt". Das ist genau das bisherige Verhalten: Die Automatik greift
                // wie zuvor. Anders als bei Extrapolation_erlaubt braucht es hier keine
                // Markerpruefung (Befund N8) - die Vorbelegung der Spalte IST der
                // bisherige Zustand, eine 0 aus einer Datenluecke sagt dasselbe wie eine
                // 0 aus dem Bestand.
                model.Kaskade_Gepflegt =
                    dt.Columns.Contains(SchemaKatalog.SPALTE_KASKADE_GEPFLEGT) &&
                    WahrOderFalsch(row[SchemaKatalog.SPALTE_KASKADE_GEPFLEGT]);

                // --- Projekteinstellung „Kuehlbetrieb" (Schemaschritt 109, KU-S2, K10) ----
                //
                // Viertes Feld nach demselben namensbasierten Muster, wieder in BEIDEN
                // Zweigen gesetzt. Fehlende Spalte (Datenbank vor Schemastand 109), NULL und
                // ein unlesbarer Wert heissen „aus" - und NIE „wie die Programmeinstellung":
                // Die gilt allein fuer den Anfangswert eines NEU angelegten Projekts und wird
                // nur dort gelesen (KuehlbetriebAnfangswertSetzen, Kuehlkonzept 7.2, E27).
                model.Kuehlbetrieb =
                    dt.Columns.Contains(KuehlungSchema.SPALTE_KUEHLBETRIEB) &&
                    WahrOderFalsch(row[KuehlungSchema.SPALTE_KUEHLBETRIEB]);

                HeizkesselNachziehen(model);

                return true;
            }

            return false;
        }

        // =====================================================================
        // HK-E-1 — DER HEIZKESSEL KOMMT IN DIE KASKADE
        // =====================================================================

        /// <summary>
        /// <b>Ein Heizkessel, den das Projekt fuehrt, bekommt automatisch einen
        /// Kaskadenplatz, wenn er keinen hat</b> (Anwenderentscheid HK-E-1 vom
        /// 15.09.2026: „Umsetzen", Vorgabe NACHRANGIG, Position waehlbar).
        /// </summary>
        /// <remarks>
        /// <para><b>Der Befund, den er schliesst</b> (#190): Die Simulation rechnet
        /// einen Waermeerzeuger ausschliesslich dann, wenn seine Technologie in einem
        /// der vier Plaetze <c>Tab_Einstellungen.Tool_1..4</c> steht. Einen Kessel
        /// anzulegen legte aber keinen Platz an — <see cref="Kaskade.Aufnehmen"/> hatte
        /// als einzigen Aufrufer das „+ aufnehmen" der verfuegbaren Karte. Die Anlage
        /// stand im Projekt und rechnete still nicht mit. Stufe 1 (#190) hat das
        /// sichtbar gemacht und bewusst nichts geaendert; hier kommt die Wirkung.</para>
        ///
        /// <para><b>NACHRANGIG ist die Vorgabe</b>, und sie ist bereits die Semantik von
        /// <see cref="Kaskade.Aufnehmen"/>: erster freier Platz HINTER dem letzten
        /// belegten, die Karte erscheint also am Ende der Kaskade. Ein eigenes Verfahren
        /// waere eine zweite Wahrheit ueber „hinten".</para>
        ///
        /// <para><b>Die Position bleibt WAEHLBAR, und dafuer wird nichts Neues gebaut:</b>
        /// Die Simulationskonfiguration ordnet die Kaskade seit jeher mit den Pfeilen der
        /// Erzeugerkachel um (<c>Kaskade.Verschieben</c>). Beim Verschieben bleibt der
        /// Platz belegt, nur an anderer Stelle — die Bedingung dieser Methode trifft
        /// danach nicht mehr zu, und der Anwender behaelt seine Reihenfolge.</para>
        ///
        /// <para><b>DIE MERKSPALTE HAT VORRANG</b> (Anwenderentscheid vom 16.09.2026):
        /// Steht <c>Tab_Einstellungen.Kaskade_Gepflegt</c> auf 1, steigt diese Methode
        /// aus, BEVOR sie irgendetwas schreibt. Eine vom Anwender gepflegte Kaskade wird
        /// nicht nachgezogen. Die Marke setzt die Simulationskonfiguration bei jedem der
        /// drei Handgriffe — aufnehmen, entfernen, verschieben
        /// (<c>SimulationKonfigHuelle</c>).</para>
        ///
        /// <para><b>Warum hier und warum GESCHRIEBEN wird.</b> Diese Stelle ist der eine
        /// Trichter, durch den jede Lesung der Konfiguration laeuft — Simulationslauf,
        /// Referenz- und CI-Lauf, Speicherauslegung und die Konfigurationsseite. Ein
        /// bloss im Arbeitsspeicher gesetzter Platz waere trotzdem falsch:
        /// <c>Ladeordnung.Kaskadenpositionen</c> liest <c>Tool_1..4</c> waehrend des
        /// Laufs unmittelbar aus der Datenbank. Steht der Platz nur im Modell, rechnete
        /// derselbe Lauf mit zwei verschiedenen Kaskaden. Geschrieben wird deshalb
        /// sofort.</para>
        ///
        /// <para><b>Wie oft sie greift.</b> Solange niemand die Kaskade anfasst, genau
        /// EINMAL je Projekt: Danach traegt ein Platz den Heizkessel, und die erste
        /// Bedingung trifft nicht mehr. Wer ihn aber wieder ENTFERNT, hinterlaesst einen
        /// leeren Platz — und der sieht fuer diese Pruefung aus wie der Zustand vor dem
        /// ersten Lauf. Genau dafuer gibt es die Merkspalte
        /// <c>Kaskade_Gepflegt</c>: Sie unterscheidet „noch nie belegt" von „vom Anwender
        /// herausgenommen". Mit ihr bleibt ein entfernter Kessel draussen.</para>
        ///
        /// <para><b>Schlaegt das Schreiben fehl</b> (schreibgeschuetzte Datenbank), bleibt
        /// auch das Modell unveraendert. Lieber der alte Zustand samt seiner Warnung als
        /// zwei Wahrheiten ueber die Kaskade in einem Lauf.</para>
        ///
        /// <para><b>Nur der Heizkessel.</b> Der Entscheid nennt ihn allein. Waermepumpe,
        /// Solarthermie, BHKW, Photovoltaik und Stromspeicher ohne Platz bleiben
        /// unberuehrt und werden weiterhin nur gemeldet
        /// (<c>SimulationLaufCtrl.ErzeugerOhneKaskadenplatz</c>).</para>
        /// </remarks>
        /// <param name="model">Das eben gefuellte Konfigurationsmodell.</param>
        /// <returns><c>true</c>, wenn ein Platz belegt und geschrieben wurde.</returns>
        private static bool HeizkesselNachziehen(KonfigurationModel model)
        {
            if (model == null || model.m_ID_Projekt <= 0) return false;

            // DER VORRANG IST EINDEUTIG: eine gepflegte Kaskade wird nicht nachgezogen.
            // Die Pruefung steht VOR jeder anderen - sie soll auch kein COUNT(*) auf
            // Tab_Energieanlagen mehr kosten.
            if (model.Kaskade_Gepflegt) return false;

            if (Kaskade.Lesen(model).Contains(DbWerte.ERZEUGER_HEIZKESSEL)) return false;
            if (!HeizkesselImProjekt(model.m_ID_Projekt)) return false;

            // Auf einer KOPIE probieren: Erst wenn das Schreiben durchgeht, gilt der
            // neue Platz auch im Modell.
            KonfigurationModel probe = new KonfigurationModel();
            Kaskade.Schreiben(probe, Kaskade.Lesen(model));
            if (!Kaskade.Aufnehmen(probe, DbWerte.ERZEUGER_HEIZKESSEL)) return false;

            if (StilleDb.NonQuery(
                    "UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = ?, Tool_3 = ?, Tool_4 = ? " +
                    "WHERE ID_Projekt = ?",
                    StilleDb.Par("@t1", DbParamTyp.VarWChar, probe.m_Tool_1 ?? ""),
                    StilleDb.Par("@t2", DbParamTyp.VarWChar, probe.m_Tool_2 ?? ""),
                    StilleDb.Par("@t3", DbParamTyp.VarWChar, probe.m_Tool_3 ?? ""),
                    StilleDb.Par("@t4", DbParamTyp.VarWChar, probe.m_Tool_4 ?? ""),
                    StilleDb.Par("@proj", DbParamTyp.Integer, model.m_ID_Projekt)) <= 0)
                return false;

            Kaskade.Schreiben(model, Kaskade.Lesen(probe));
            return true;
        }

        /// <summary>
        /// Fuehrt das Projekt wenigstens eine Heizkesselanlage
        /// (<c>Tab_Energieanlagen.ID_Type</c> = <see cref="WizardItemClass.KESSEL_TYP"/>)?
        /// Dialogfrei gelesen, weil derselbe Weg im unbeaufsichtigten Referenz- und
        /// CI-Lauf benutzt wird.
        /// </summary>
        private static bool HeizkesselImProjekt(int idProjekt)
        {
            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@typ", DbParamTyp.Integer, WizardItemClass.KESSEL_TYP)), 0) > 0;
        }

        // =====================================================================
        // ENTFALLEN MIT PAKET L (Aufraeumen) - Altlast Kaskade_Zweikanalig
        //
        // Hier standen KaskadeZweikanaligLesen, KaskadeZweikanaligSchreiben und die
        // Automatik KaskadeNotwendig (zwei Ueberladungen) samt ihrer beiden privaten
        // Helfer KaskadeErzeuger und ErzeugerZuTyp. Sie bedienten die
        // Projekteinstellung Tab_Einstellungen.Kaskade_Zweikanalig - bis Paket A1 die
        // WEICHE zwischen zwei Rechenwegen.
        //
        // Mit Paket A1 (Leitentscheidung L1) ist der einkanalige Altpfad ersatzlos
        // entfallen; mit dem Fusszeilenschalter des Konfigurationsdialogs und dem
        // Uebergangshinweis des Senkendialogs verschwand ihr letzter Aufrufer. Paket L
        // schneidet die aufruferfreien Bausteine heraus (A1-O3): Es gibt nur EINEN
        // Rechenweg, es gibt also nichts mehr umzuschalten und nichts zu begruenden.
        //
        // DIE SPALTE BLEIBT. Konzept Kapitel 15 fuehrt
        // Tab_Einstellungen.Kaskade_Zweikanalig als "stillgelegt (Lese-Altlast nach
        // Migration)"; Migrationsschritt 51 setzt sie im Bestand auf WAHR und loescht
        // nichts. Wer sie je wieder braucht, liest sie ueber StilleDb - der Weg dorthin
        // ist eine Zeile, die Namenskonstante steht in
        // SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG.
        // =====================================================================

        /// <summary>
        /// Liest die Einstellung <c>Extrapolation_erlaubt</c> eines Projekts DIALOGFREI
        /// (Paket 8, Konzept 13.4) — für die Oberfläche, die den Schalter anzeigt, ohne
        /// den ganzen Einstellungssatz zu laden.
        ///
        /// Fehlende Spalte, fehlende Zeile und NULL liefern gleichermaßen <c>true</c>;
        /// das ist die Vorbelegung der Einstellung und das bisherige Verhalten.
        /// </summary>
        public static bool ExtrapolationErlaubtLesen(int idProjekt)
        {
            if (idProjekt <= 0) return true;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            if (v == null) return true;
            try { if (Convert.ToBoolean(v)) return true; }
            catch { return true; }

            // Befund N8: ein FALSE aus einer Datenbank ohne Migrationsschritt 7 ist die
            // Vorbelegung von Access, nicht der Wille des Anwenders (Begründung in
            // ReadSingle).
            return ExtrapolationVorbelegungFehlt();
        }

        /// <summary>
        /// true, solange die Datenbank den Migrationsschritt 7 (Vorbelegung
        /// <c>Extrapolation_erlaubt = WAHR</c>) noch nicht hinter sich hat — dann ist ein
        /// gespeichertes FALSE die Access-Vorbelegung einer angehängten YESNO-Spalte und
        /// nicht die Entscheidung des Anwenders (Nacharbeit Paket 8, Befund N8).
        ///
        /// Bewusst LESEND: Die Alternative wäre gewesen, die stille Rückfallebene
        /// <c>WaermequelleClass.SchemaSicherstellen</c> die Spalte nachvorbelegen zu
        /// lassen. Das trägt nicht — sie läuft erst in <c>Do_Simulation</c>, also NACH
        /// dem Lesen der Konfiguration im <c>SimulationRunner</c>, und hätte den
        /// laufenden Lauf nicht mehr erreicht. Ein Leser, der die Datenlücke erkennt,
        /// wirkt sofort und schreibt nichts in eine fremde Datenbank.
        ///
        /// Der erreichte Zielstand wird gemerkt: Auf einer gepflegten Datenbank fällt
        /// genau ein Marker-Lesevorgang je Programmlauf an, danach nichts mehr.
        /// </summary>
        private static bool _schemastand7Erreicht = false;

        private static bool ExtrapolationVorbelegungFehlt()
        {
            if (_schemastand7Erreicht) return false;

            try
            {
                if (ApplikationCtrl.GetSchemaVersion() >= SchemaStand.SCHRITT_7_EXTRAPOLATION)
                {
                    _schemastand7Erreicht = true;
                    return false;
                }
            }
            catch { /* Marker nicht lesbar - dann gilt die Datenlücke */ }

            return true;
        }

        /// <summary>
        /// Schreibt die Einstellung <c>Extrapolation_erlaubt</c> eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> hängen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte würde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// oder die Spalte fehlt.
        /// </summary>
        public static bool ExtrapolationErlaubtSchreiben(int idProjekt, bool wert)
        {
            if (idProjekt <= 0) return false;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" + SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.Boolean, wert),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        // --- Auslegungstemperaturen der PV-Strangpruefung (W6-B-11, 09.09.2026) --------

        /// <summary>
        /// <b>Die zwei Auslegungstemperaturen eines Projekts</b> —
        /// <c>Tab_Einstellungen.Ausleg_T_Kalt</c> und <c>…Ausleg_T_Heiss</c>
        /// (Anwenderentscheid <b>W6‑B‑11</b> vom 09.09.2026, Migrationsschritt 70).
        ///
        /// <para><b><c>null</c> heisst „die Vorgabe"</b> — <c>StrangPlausibilitaet.T_KALT</c>
        /// (−10 °C) bzw. <c>T_HEISS</c> (+70 °C). Fehlende Spalte, fehlende Zeile und
        /// NULL liefern gleichermaßen <c>null</c>: Alle drei bedeuten „nicht
        /// gepflegt", und das ist der Stand von heute.</para>
        ///
        /// <para><b>Dialogfrei und namensbasiert.</b> Dasselbe Muster wie
        /// <see cref="ExtrapolationErlaubtLesen"/>: <c>Tab_Einstellungen</c> wird in
        /// <see cref="ReadSingle(string)"/> ORDINAL gelesen, die zwei Spalten sind
        /// deshalb angehängt und werden hier einzeln geholt. Die Ordinalkette bleibt
        /// unberührt.</para>
        /// </summary>
        public static Auslegungstemperaturen AuslegungstemperaturenLesen(int idProjekt)
        {
            if (idProjekt <= 0) return new Auslegungstemperaturen(null, null);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT [" + SchemaKatalog.SPALTE_AUSLEG_T_KALT + "], [" +
                SchemaKatalog.SPALTE_AUSLEG_T_HEISS + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", idProjekt));

            if (dt == null || dt.Rows.Count == 0) return new Auslegungstemperaturen(null, null);

            DataRow r = dt.Rows[0];
            return new Auslegungstemperaturen(Grad(r, 0), Grad(r, 1));
        }

        /// <summary>Ein Temperaturfeld; <c>DBNull</c> und ein unlesbarer Wert werden <c>null</c>.</summary>
        private static double? Grad(DataRow r, int spalte)
        {
            if (r == null || spalte >= r.Table.Columns.Count) return null;
            if (r[spalte] == null || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); }
            catch { return null; }
        }

        /// <summary>
        /// Schreibt die zwei Auslegungstemperaturen eines Projekts; <c>null</c> setzt
        /// die Spalte auf NULL und damit auf die Vorgabe zurück.
        ///
        /// <para>Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/> — dieselbe Begründung wie bei
        /// <see cref="ExtrapolationErlaubtSchreiben"/>: Auf einer Datenbank ohne die
        /// Spalten würde ein erweitertes UPDATE das Speichern der GESAMTEN
        /// Konfiguration scheitern lassen.</para>
        ///
        /// <para>Rückgabe <c>false</c>, wenn keine Zeile getroffen wurde oder die
        /// Spalten fehlen.</para>
        /// </summary>
        public static bool AuslegungstemperaturenSchreiben(int idProjekt, double? kalt, double? heiss)
        {
            if (idProjekt <= 0) return false;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" + SchemaKatalog.SPALTE_AUSLEG_T_KALT + "] = ?, [" +
                SchemaKatalog.SPALTE_AUSLEG_T_HEISS + "] = ? WHERE ID_Projekt = ?",
                StilleDb.Par("@kalt", DbParamTyp.Double, (object)kalt ?? DBNull.Value),
                StilleDb.Par("@heiss", DbParamTyp.Double, (object)heiss ?? DBNull.Value),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        // --- Kanal-Knappheitsreihenfolge (Paket K2, Konzept 4.3, Entscheidung F10) -----

        /// <summary>
        /// Der Wert eines gelesenen Feldes; <c>null</c>, <c>DBNull</c> und Leerwert
        /// ergeben <see cref="DbWerte.KNAPPHEIT_DEFAULT"/> — die Vorbelegung nach F10
        /// und zugleich die bis Paket K2 fest verdrahtete Reihenfolge.
        ///
        /// <para>Bewusst OHNE inhaltliche Prüfung: Ein unbekanntes Glied oder ein
        /// fehlender Kanal ist kein Grund, den Anwenderwillen zu verwerfen. Die
        /// Auswertung im Rechenkern ist tolerant — sie übergeht, was sie nicht kennt,
        /// und ergänzt fehlende Kanäle hinten in der Reihenfolge des Vorgabewerts.</para>
        /// </summary>
        public static string KnappheitsreihenfolgeOderDefault(object feld)
        {
            if (feld == null || feld == DBNull.Value) return DbWerte.KNAPPHEIT_DEFAULT;

            string wert = (feld.ToString() ?? "").Trim();
            return wert.Length == 0 ? DbWerte.KNAPPHEIT_DEFAULT : wert;
        }

        /// <summary>
        /// Liest die Knappheitsreihenfolge eines Projekts DIALOGFREI — für Aufrufer, die
        /// nicht den ganzen Einstellungssatz laden (Rechenkern, Oberfläche).
        ///
        /// Fehlende Spalte, fehlende Zeile, NULL und Leerwert liefern gleichermaßen
        /// <see cref="DbWerte.KNAPPHEIT_DEFAULT"/>.
        /// </summary>
        public static string KnappheitsreihenfolgeLesen(int idProjekt)
        {
            if (idProjekt <= 0) return DbWerte.KNAPPHEIT_DEFAULT;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return KnappheitsreihenfolgeOderDefault(v);
        }

        /// <summary>
        /// Schreibt die Knappheitsreihenfolge eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/> — dieselbe Begründung wie bei
        /// <see cref="ExtrapolationErlaubtSchreiben"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> hängen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte würde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// Ein leerer Wert wird als <see cref="DbWerte.KNAPPHEIT_DEFAULT"/> geschrieben,
        /// nicht als NULL: Die Spalte soll die geltende Reihenfolge zeigen, auch wenn
        /// sie die Vorgabe ist.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// oder die Spalte fehlt.
        /// </summary>
        public static bool KnappheitsreihenfolgeSchreiben(int idProjekt, string reihenfolge)
        {
            if (idProjekt <= 0) return false;

            string wert = (reihenfolge ?? "").Trim();
            if (wert.Length == 0) wert = DbWerte.KNAPPHEIT_DEFAULT;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" +
                SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.VarWChar, wert),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        // --- Booster-Lesepunkt (Paket B2, Nutzerauftrag 28.08.2026) --------------------

        /// <summary>
        /// Liest den LESEPUNKT der Booster-Quelltemperatur eines Projekts DIALOGFREI —
        /// für den Rechenkern und den Konfigurationsdialog.
        ///
        /// <para>Fehlende Spalte (Datenbank noch nicht auf Schemastand 55), fehlende
        /// Zeile, NULL, Leerwert und jeder unbekannte Wert liefern gleichermaßen
        /// <see cref="DbWerte.BOOSTER_LESEPUNKT_DAVOR"/> — die Vorbelegung des
        /// Nutzerauftrags.</para>
        ///
        /// <para><b>Anders als bei <see cref="ExtrapolationErlaubtLesen"/> braucht es
        /// hier KEINE Markerprüfung</b> (Befund N8): Die Spalte ist ein TEXTfeld, und
        /// eine angehängte Textspalte steht in Access auf NULL — nicht auf einem Wert,
        /// der wie eine Anwenderentscheidung aussieht. Die Datenlücke ist damit von
        /// selbst als solche erkennbar.</para>
        /// </summary>
        public static string BoosterLesepunktLesen(int idProjekt)
        {
            if (idProjekt <= 0) return DbWerte.BOOSTER_LESEPUNKT_DAVOR;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_BOOSTER_LESEPUNKT + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return DbWerte.BoosterLesepunktOderDefault(v);
        }

        /// <summary>
        /// Schreibt den Booster-Lesepunkt eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/> — dieselbe Begründung wie bei
        /// <see cref="KnappheitsreihenfolgeSchreiben"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> hängen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte würde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// Ein unbekannter Wert wird als Vorbelegung geschrieben, nicht als NULL: Die
        /// Spalte soll den geltenden Lesepunkt zeigen, auch wenn er die Vorgabe ist.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// oder die Spalte fehlt.
        /// </summary>
        public static bool BoosterLesepunktSchreiben(int idProjekt, string lesepunkt)
        {
            if (idProjekt <= 0) return false;

            string wert = DbWerte.BoosterLesepunktOderDefault(lesepunkt);

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" +
                SchemaKatalog.SPALTE_BOOSTER_LESEPUNKT + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.VarWChar, wert),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        // --- Merkspalte „Kaskade vom Anwender gepflegt" (Schemaschritt 82) ------------

        /// <summary>
        /// Ein Datenbankwert als Wahrheitswert; NULL, DBNull und alles Unlesbare heissen
        /// FALSCH. Der Weg fuer die 0/1-Spalten, deren Datenluecke dasselbe bedeutet wie
        /// ihre Vorbelegung.
        /// </summary>
        private static bool WahrOderFalsch(object wert)
        {
            if (wert == null || wert == DBNull.Value) return false;
            try { return Convert.ToBoolean(wert); }
            catch { return false; }
        }

        /// <summary>
        /// Hat der Anwender die Kaskade dieses Projekts selbst in die Hand genommen?
        /// DIALOGFREI gelesen, weil derselbe Weg im unbeaufsichtigten Referenz- und
        /// CI-Lauf benutzt wird.
        ///
        /// <para>Fehlende Spalte (Datenbank noch nicht auf Schemastand 82), fehlende
        /// Zeile und NULL liefern gleichermassen <c>false</c> — „nicht gepflegt", also
        /// genau das bisherige Verhalten.</para>
        /// </summary>
        public static bool KaskadeGepflegtLesen(int idProjekt)
        {
            if (idProjekt <= 0) return false;

            return WahrOderFalsch(StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_KASKADE_GEPFLEGT + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt)));
        }

        /// <summary>
        /// Schreibt die Merkspalte eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/> — dieselbe Begruendung wie bei
        /// <see cref="KnappheitsreihenfolgeSchreiben"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> haengen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte wuerde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// <para><b>Warum das in die DATENBANK muss und nicht ins Modell reicht:</b>
        /// dieselbe Begruendung, aus der <see cref="HeizkesselNachziehen"/> den Platz
        /// schreibt — <c>Ladeordnung.Kaskadenpositionen</c> liest <c>Tool_1..4</c>
        /// waehrend des Laufs ein zweites Mal unmittelbar aus der Datenbank. Eine Marke,
        /// die nur im Arbeitsspeicher stuende, waere beim naechsten Lesen der
        /// Konfiguration wieder fort, und die Automatik naehme den eben entfernten
        /// Heizkessel erneut auf.</para>
        ///
        /// <para>Geschrieben wird 0/1 (Hausregel BETRIEB_SQLITE.md Abschnitt 6), nicht
        /// <c>true</c>/<c>false</c>: Die Spalte traegt ein <c>CHECK (… IN (0,1))</c>.</para>
        ///
        /// Dialogfrei (Konzept 13.4). Rueckgabe <c>false</c>, wenn keine Zeile getroffen
        /// wurde oder die Spalte fehlt.
        /// </summary>
        public static bool KaskadeGepflegtSchreiben(int idProjekt, bool gepflegt)
        {
            if (idProjekt <= 0) return false;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" +
                SchemaKatalog.SPALTE_KASKADE_GEPFLEGT + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.Integer, gepflegt ? 1 : 0),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        // --- Projekteinstellung „Kuehlbetrieb" (Schemaschritt 109, KU-S2; K10, E27) -----

        /// <summary>
        /// Wird in diesem Projekt Kaelte gerechnet? DIALOGFREI gelesen, wie die Merkspalte.
        ///
        /// <para><b>Fehlende Zeile, fehlende Spalte und NULL heissen „aus"</b> - ein
        /// Bestandsprojekt ohne Einstellungssatz ist aus, gleich, was die
        /// Programmeinstellung sagt (Kuehlkonzept 7.2: „ausgeschlossen ist, die
        /// Programmeinstellung zur Laufzeit als Rueckfall zu lesen").</para>
        /// </summary>
        public static bool KuehlbetriebLesen(int idProjekt)
        {
            if (idProjekt <= 0) return false;

            return WahrOderFalsch(StilleDb.Scalar(
                "SELECT [" + KuehlungSchema.SPALTE_KUEHLBETRIEB + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt)));
        }

        /// <summary>
        /// Schreibt die Projekteinstellung „Kuehlbetrieb" eines Projekts - die Projektschalter
        /// sind je Projekt in beide Richtungen frei (Kuehlkonzept 7.2, 8.3).
        ///
        /// <para>Ein EIGENES, zielgenaues UPDATE wie bei
        /// <see cref="KaskadeGepflegtSchreiben"/> und aus demselben Grund: Die Spaltenlisten
        /// von <see cref="Insert"/>/<see cref="Update"/> haengen an der Ordinalkette. Das
        /// Speichern der Kaskade legt die Zeile neu an und reicht den Wert danach ueber
        /// diese Methode nach (<c>SimulationKonfigHuelle.Speichern</c>). Geschrieben wird
        /// 0/1; die Spalte traegt ein <c>CHECK (… IN (0,1))</c>.</para>
        ///
        /// Rueckgabe <c>false</c>, wenn keine Zeile getroffen wurde oder die Spalte fehlt.
        /// </summary>
        public static bool KuehlbetriebSchreiben(int idProjekt, bool an)
        {
            if (idProjekt <= 0) return false;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" +
                KuehlungSchema.SPALTE_KUEHLBETRIEB + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.Integer, an ? 1 : 0),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        /// <summary>
        /// <b>DER ANFANGSWERT EINES NEUEN PROJEKTS</b> (Entscheid E27, K10; Kuehlkonzept 7.2)
        /// - die EINE Stelle, an der die Programmeinstellung „Neue Projekte mit Kuehlung
        /// anlegen" in eine Projekteinstellung uebergeht. Gerufen von den beiden
        /// Anlagewegen, unmittelbar nachdem die Projektzeile steht
        /// (<see cref="WizardCtrl.Add_Projekt"/>, <see cref="ProjektCtrl.Insert"/>), und von
        /// nirgends sonst.
        ///
        /// <para><b>Programmeinstellung aus (die Vorgabe):</b> Es gibt nichts zu schreiben.
        /// Ein neues Projekt hat bis zum ersten Speichern der Kaskade keinen
        /// Einstellungssatz; der traegt dann die Spaltenvorgabe 0, und ohne Satz liest
        /// <see cref="KuehlbetriebLesen"/> ebenfalls „aus". Die Anlage bleibt damit genau,
        /// wie sie war.</para>
        ///
        /// <para><b>Programmeinstellung an:</b> Das Projekt bekommt seinen Einstellungssatz
        /// schon beim Anlegen - ueber denselben Einfuegeweg wie beim ersten Speichern der
        /// Kaskade (<see cref="Insert"/> mit leerem Modell samt der drei Vorbelegungen) -
        /// und <c>Kuehlbetrieb = 1</c>. Steht schon ein Satz, wird nur der Schalter gesetzt.
        /// Der Wert haelt, weil das Speichern der Kaskade ihn nach Loeschen und Neuanlegen
        /// nachreicht.</para>
        ///
        /// <para><b>Nie zur Laufzeit.</b> Kein Lesen der Konfiguration, kein Lauf und kein
        /// Schemaschritt fragt die Programmeinstellung; ein Projektduplikat uebernimmt den
        /// Wert seiner Quelle (es ist kopiert, nicht neu angelegt). Wer diese Methode
        /// anderswo ruft, schaltet Bestandsprojekte mit - der Waechter
        /// <c>KuehlbetriebProgrammeinstellungTests</c> haelt die Aufrufer fest.</para>
        /// </summary>
        /// <returns>Der Anfangswert, der jetzt in der Projekteinstellung steht (<c>false</c> auch, wenn das Schreiben scheiterte).</returns>
        public static bool KuehlbetriebAnfangswertSetzen(int idProjekt)
        {
            if (idProjekt <= 0) return false;
            if (!EinstellungenCtrl.NeueProjekteMitKuehlungLesen()) return false;

            bool satzVorhanden = StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt)), 0) > 0;

            // Dasselbe leere Modell, mit dem die Konfigurationsseite ein Projekt ohne Satz
            // oeffnet - der Satz sieht aus, als waere die Kaskade einmal leer gespeichert.
            if (!satzVorhanden && !new KonfigurationCtrl().Insert(idProjekt)) return false;

            return KuehlbetriebSchreiben(idProjekt, true);
        }

        public bool Insert(int ID_Projekt)
        {
            try
            {
                // Umstellung auf sichere Parameter-Marker (?) statt ungesicherter String-Verkettung
                string sql = @"
                    INSERT INTO TAB_Einstellungen 
                    (
                        ID_Projekt, BHKW_Grenzleistung, Netzverluste, NetzverlusteEinheit, 
                        Kessel_Betriebsbereitschaft, 
                        Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6,
                        Ladefuellstand_Min, Ladefuellstand_Max, Ladeleistung_Max,
                        Ladefuellstand_Min_Auswahl, Ladefuellstand_Max_Auswahl, 
                        Ladeleistung_Max_Auswahl, Ladeschwellwert, Betriebsart, Leistungsgrenze, Pendelspeicher
                    ) 
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                // Die Parameter werden als OLEDB-Objekte an dein DataRepository gereicht
                DbParam[] parameters = new DbParam[]
                {
                    new DbParam("?", ID_Projekt),
                    new DbParam("?", model.m_BHKW_Grenzleistung),
                    new DbParam("?", model.m_Netzverluste),
                    new DbParam("?", model.m_szNetzverlusteEinheit ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Kessel_Betriebsbereitschaft),
                    new DbParam("?", model.m_Tool_1 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_2 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_3 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_4 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_5 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_6 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladefuellstand_Min),
                    new DbParam("?", model.m_Ladefuellstand_Max),
                    new DbParam("?", model.m_Ladeleistung_Max),
                    new DbParam("?", model.m_Ladefuellstand_Min_Auswahl ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladefuellstand_Max_Auswahl ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladeleistung_Max_Auswahl ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladeschwellwert),
                    new DbParam("?", model.Betriebsart),
                    new DbParam("?", model.Leistungsgrenze),
                    new DbParam("?", model.Pendelspeicher)
                };

                // Übergabe an das DataRepository
                DataRepository.ExecuteNonQuery(sql, parameters);

                // PAKET 8 (Konzept 13.4): Die Spaltenliste oben bleibt unangetastet -
                // sie gehört zur Ordinalkette von ReadSingle, und auf einer Datenbank
                // ohne Schemastand 7 würde ein erweitertes INSERT das Anlegen der
                // GESAMTEN Konfiguration scheitern lassen. Die Vorbelegung kommt
                // deshalb als eigenes, stilles UPDATE hinterher: Access belegt eine
                // angehängte YESNO-Spalte in einer neuen Zeile mit False - ohne diese
                // Zeile stünde jedes NEUE Projekt auf "Extrapolation verboten" und
                // damit auf anderem Verhalten als der migrierte Bestand.
                ExtrapolationErlaubtSchreiben(ID_Projekt, true);

                // PAKET K2 (F10): dieselbe Nachreichung für die Knappheitsreihenfolge.
                // Anders als beim Ja/Nein-Feld darüber wäre NULL hier kein Fehler - die
                // Leseseite macht daraus ohnehin den Vorgabewert. Geschrieben wird sie
                // trotzdem, damit ein neues Projekt dieselbe Zeile zeigt wie ein
                // migriertes: Die Spalte soll die geltende Reihenfolge nennen, nicht
                // schweigen.
                KnappheitsreihenfolgeSchreiben(ID_Projekt, DbWerte.KNAPPHEIT_DEFAULT);

                // PAKET B2: dieselbe Nachreichung für den Booster-Lesepunkt. Ein neues
                // Projekt soll dieselbe Zeile zeigen wie ein migriertes - die Spalte
                // nennt den geltenden Lesepunkt, statt zu schweigen.
                BoosterLesepunktSchreiben(ID_Projekt, DbWerte.BOOSTER_LESEPUNKT_DAVOR);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Einfügen der Konfiguration: " + ex.Message);
                DataRepository.FehlerMelden("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
        }

        public bool Update(int ID_Projekt)
        {
            try
            {
                // SQL-Update-String mit Positions-Parametern (?)
                string sql = @"
            UPDATE TAB_Einstellungen 
            SET 
                BHKW_Grenzleistung = ?, 
                Netzverluste = ?, 
                NetzverlusteEinheit = ?, 
                Kessel_Betriebsbereitschaft = ?, 
                Tool_1 = ?, 
                Tool_2 = ?, 
                Tool_3 = ?, 
                Tool_4 = ?, 
                Tool_5 = ?, 
                Tool_6 = ?,
                Ladefuellstand_Min = ?, 
                Ladefuellstand_Max = ?, 
                Ladeleistung_Max = ?,
                Ladefuellstand_Min_Auswahl = ?, 
                Ladefuellstand_Max_Auswahl = ?, 
                Ladeleistung_Max_Auswahl = ?, 
                Ladeschwellwert = ?,
                Betriebsart = ?,
                Leistungsgrenze = ?,
                Pendelspeicher = ?
            WHERE ID_Projekt = ?";

                // Die Parameter-Reihenfolge entspricht exakt den Fragezeichen im SQL-String
                DbParam[] parameters = new DbParam[]
                {
            new DbParam("?", model.m_BHKW_Grenzleistung),
            new DbParam("?", model.m_Netzverluste),
            new DbParam("?", model.m_szNetzverlusteEinheit ?? (object)DBNull.Value),
            new DbParam("?", model.m_Kessel_Betriebsbereitschaft),
            new DbParam("?", model.m_Tool_1 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_2 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_3 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_4 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_5 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_6 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladefuellstand_Min),
            new DbParam("?", model.m_Ladefuellstand_Max),
            new DbParam("?", model.m_Ladeleistung_Max),
            new DbParam("?", model.m_Ladefuellstand_Min_Auswahl ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladefuellstand_Max_Auswahl ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladeleistung_Max_Auswahl ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladeschwellwert),
            new DbParam("?", model.Betriebsart),
            new DbParam("?", model.Leistungsgrenze),
            new DbParam("?", model.Pendelspeicher),
            // ID_Projekt steht am Ende, weil das WHERE-Statement ganz unten steht!
            new DbParam("?", ID_Projekt)
                };

                // Übergabe an dein bestehendes DataRepository
                DataRepository.ExecuteNonQuery(sql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Konfiguration: " + ex.Message);
                DataRepository.FehlerMelden("Allgemeiner Fehler beim Speichern: " + ex.Message);
                return false;
            }
        }

        public bool Delete(int ID_Projekt)
        {
            try
            {
                // Sauberes ANSI-SQL für OLEDB ohne das ungültige "DELETE *"
                string sql = "DELETE FROM Tab_Einstellungen WHERE ID_Projekt = ?";
                DbParam parameter = new DbParam("?", ID_Projekt);

                DataRepository.ExecuteNonQuery(sql, parameter);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen der Konfiguration: " + ex.Message);
                return false;
            }
        }
    }
}
