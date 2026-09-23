using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // FREMDE ERGEBNISVERWEISE DER WIRTSCHAFTLICHKEIT WERDEN LEER - Migrationsschritt 106
    //
    // WOZU. Tab_ErgebnisWirtschaftlichkeit.ID_Ergebnis nennt den Simulationslauf
    // (Tab_Ergebnis.ID), auf dem eine gespeicherte Wirtschaftlichkeit beruht; ob sie zum
    // Simulationsstand passt, fragt WirtschaftlichkeitCtrl.ErgebnisAktuell genau daran ab.
    // Das Duplizieren eines Projekts (ProjektDuplizierenCtrl, auch der Weg jeder Variante,
    // VariantenCtrl.AnlegenAusStamm) kopierte die Zeilen samt UNVERSETZTEM Verweis: Die
    // Spalte hat keine deklarierte Beziehung und stand in keiner Zuordnung des Kopierlaufs.
    // Jede Kopie zeigte damit auf den Lauf des QUELLprojekts - in der Anwenderdatenbank die
    // Projekte 1066/1067 auf den Lauf 234 von 1065, in der Testdatenbank 21 Zeilen der
    // Projekte 1028, 1029, 1040, 1041 (auf 167 von 1026) und 1043, 1044 (auf 206 von 1042).
    //
    // WAS DER SCHRITT TUT. Er setzt jeden Verweis auf NULL, der auf keinen Lauf DESSELBEN
    // Projekts zeigt - der Anwenderentscheid vom 23.09.2026 ("Ergebnisverweise werden nicht
    // mitkopiert; die Kopie hat noch kein Ergebnis, die Wirtschaftlichkeit rechnet nach dem
    // ersten Lauf neu"). Die Zeilen selbst BLEIBEN: Sie gelten danach als "passt nicht zum
    // Simulationsstand", wie es jede Kopie ab jetzt von Anfang an tut
    // (ProjektDuplizierenCtrl.ERGEBNISVERWEISE_LEEREN). Ein Verweis auf einen Lauf, den es
    // gar nicht mehr gibt, zeigt ebenso auf keinen eigenen Lauf und wird mit leer.
    //
    // NUR DER FREMDE VERWEIS. Getroffen wird genau ein gesetzter Verweis (> 0) ohne eigenen
    // Lauf; ein Verweis auf den eigenen Lauf bleibt, NULL und 0 bleiben, und jede andere
    // Spalte der Zeile bleibt.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest den Verweis - er entscheidet allein die Anzeige
    // "aktuell/veraltet" einer gespeicherten Wirtschaftlichkeit, und die ist fuer einen
    // fremden Verweis schon vorher "veraltet" gewesen (die Id ist nie die des juengsten
    // eigenen Laufs). Die Wirtschaftlichkeit steht nicht im Export des Referenzlaufs.
    //
    // WIEDERHOLBAR. Ein zweiter Lauf findet keinen fremden Verweis mehr (Offen() = 0) und
    // fasst nichts an. Fehlt die Tabelle (sie entsteht erst mit dem ersten
    // Wirtschaftlichkeitslauf, WirtschaftlichkeitCtrl.StelleTabellenSicher), tut er nichts.
    //
    // EINE QUELLE fuer drei Leser: den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests (TestDatenbank
    // zieht jede Arbeitskopie damit nach).
    // ====================================================================================

    /// <summary>
    /// Schemaschritt 106 — Verweise der gespeicherten Wirtschaftlichkeit auf den
    /// Simulationslauf eines FREMDEN Projekts werden NULL (Anwenderentscheid 23.09.2026).
    /// EINE Quelle für Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class WirtschaftlichkeitFremdverweis
    {
        /// <summary>Die Tabelle der gespeicherten Wirtschaftlichkeitsergebnisse.</summary>
        public const string TABELLE = WirtschaftlichkeitCtrl.TAB_ERGEBNIS;

        /// <summary>Die Spalte, deren fremder Verweis NULL wird.</summary>
        public const string SPALTE = "ID_Ergebnis";

        /// <summary>Die Tabelle der Simulationsläufe, auf die der Verweis zeigt.</summary>
        public const string TAB_LAUF = ErgebnisCtrl.TAB_KOPF;

        /// <summary>
        /// Die Bedingung aller drei Anweisungen: ein GESETZTER Verweis, zu dem kein Lauf
        /// DESSELBEN Projekts gehört — ein Lauf eines anderen Projekts oder keiner.
        /// </summary>
        private const string BEDINGUNG =
            "[" + SPALTE + "] > 0 AND NOT EXISTS (SELECT 1 FROM [" + TAB_LAUF + "] e " +
            "WHERE e.[ID] = [" + TABELLE + "].[" + SPALTE + "] " +
            "AND e.[ID_Projekt] = [" + TABELLE + "].[ID_Projekt])";

        /// <summary>Die Anweisung des Schrittes.</summary>
        public const string SQL_SETZEN =
            "UPDATE [" + TABELLE + "] SET [" + SPALTE + "] = NULL WHERE " + BEDINGUNG;

        /// <summary>Die Zählung — dieselbe Bedingung wie die Anweisung.</summary>
        public const string SQL_ZAEHLEN =
            "SELECT COUNT(*) FROM [" + TABELLE + "] WHERE " + BEDINGUNG;

        /// <summary>Die betroffenen Zeilen mit Id, Projekt, Verweis und Szenario — für das
        /// Protokoll des Schrittes, damit es sagt, WELCHE Zeilen er angefasst hat.</summary>
        public const string SQL_BETROFFENE =
            "SELECT [ID], [ID_Projekt], [" + SPALTE + "], [Szenario] FROM [" + TABELLE +
            "] WHERE " + BEDINGUNG + " ORDER BY [ID]";

        /// <summary>
        /// Die Anweisungen — eine, und nur solange sie etwas trifft. Auf einer bereits
        /// bereinigten Datenbank bleibt die Folge leer; der zweite Lauf fasst nichts an.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!Vorhanden()) yield break;
                if (Offen() <= 0) yield break;
                yield return new KeyValuePair<string, string>(
                    "Fremde Ergebnisverweise in " + TABELLE + " auf NULL", SQL_SETZEN);
            }
        }

        /// <summary>
        /// Führt den Schritt aus und liefert die Zahl der Zeilen, deren Verweis er auf NULL
        /// gesetzt hat (0 = nichts zu tun). Für Werkzeug und Nachweis; die Migration ruft die
        /// <see cref="Anweisungen"/> selbst, um einen Fehler benannt melden zu können.
        /// </summary>
        public static int Ausfuehren()
        {
            int vorher = Offen();
            foreach (KeyValuePair<string, string> a in Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
            return vorher - Offen();
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Stehen Verweisspalte und Laufkopf samt Projektbezug? Ohne sie tut der Schritt
        /// nichts — dieselbe tolerante Haltung wie bei jedem DML-Schritt.
        /// </summary>
        public static bool Vorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SPALTE) &&
                   DataRepository.SpalteVorhanden(TABELLE, "ID_Projekt") &&
                   DataRepository.SpalteVorhanden(TAB_LAUF, "ID_Projekt");
        }

        /// <summary>Wie viele Zeilen verweisen noch auf einen fremden Lauf? Genau so viele
        /// setzt der Schritt auf NULL. 0 = nichts zu tun, er ist gelaufen.</summary>
        public static int Offen()
        {
            if (!Vorhanden()) return 0;
            try
            {
                object o = DataRepository.ExecuteScalar(SQL_ZAEHLEN);
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        /// <summary>
        /// Die Zeilen, die der Schritt anfassen wird, als Protokollzeilen
        /// „Id 16, Projekt 1028: Lauf 167 (Erwartet)" — vor dem Schreiben gelesen, danach
        /// findet die Abfrage nichts mehr.
        /// </summary>
        public static List<string> Betroffene()
        {
            var zeilen = new List<string>();
            if (!Vorhanden()) return zeilen;
            DataTable t = DataRepository.GetDataTable(SQL_BETROFFENE);
            if (t == null) return zeilen;
            foreach (DataRow r in t.Rows)
                zeilen.Add("Id " + Convert.ToString(r["ID"], CultureInfo.InvariantCulture) +
                           ", Projekt " + Convert.ToString(r["ID_Projekt"], CultureInfo.InvariantCulture) +
                           ": Lauf " + Convert.ToString(r[SPALTE], CultureInfo.InvariantCulture) +
                           " (" + Convert.ToString(r["Szenario"], CultureInfo.InvariantCulture) + ")");
            return zeilen;
        }
    }
}
