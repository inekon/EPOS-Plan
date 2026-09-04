using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// iU9-W10b.0b — die drei Abfragen, mit denen die Simulationskonfiguration ihre
    /// Erzeugerkarten fuellt.
    ///
    /// <para><b>Warum sie hier stehen (Befund W10-B35).</b> Alle drei standen als
    /// zusammengesetztes inline-SQL in der ANZEIGESCHICHT
    /// (<c>Form_Simulation_Config.Karten.cs</c>:1226-1246 und :2081-2111,
    /// <c>…Uebersicht.cs</c>:371-430). Der SQL-Dialektpruefer sieht sie dort zwar auch,
    /// aber eine Razor-Komponente darf keine Datenbank kennen - und eine Huelle, die
    /// SQL zusammensetzt, waere nur die Verschiebung des Problems um eine Datei.</para>
    ///
    /// <para><b>Die Umlaut-Falle bleibt woertlich.</b>
    /// <c>Tab_Energieanlagen.[Rücklauf]</c> traegt an der Datenbank den Umlaut (Befund
    /// B0-4, siehe <c>ProjektPuffer.SQL_SYSTEM_RUECKLAUF</c>) - anders als
    /// <c>Tab_Pufferspeicher</c>. Der Alias auf den umlautfreien Namen bleibt, damit der
    /// Lesecode nicht von der Schreibweise abhaengt.</para>
    /// </summary>
    partial class WErzeugerCtrl : WErzeugerModel
    {
        /// <summary>
        /// Bezeichner aller Projektanlagen eines Typs, OHNE Wiederholungen
        /// (Vorlaeufer <c>Form_Simulation_Config.Karten.AnlagenNamen</c>:1226-1246).
        ///
        /// <para>Entdoppelt wird bewusst: Im Bestand stehen regelmaessig mehrere Zeilen
        /// desselben Moduls (Projekt 1011: vier Batteriezeilen, davon drei namensgleich).
        /// Eine Kopfzeile „Stromspeicher · BYD B-Box HVM 11.0 · BYD B-Box HVM 11.0 · …"
        /// sagt nichts, was die entdoppelte Fassung nicht auch sagt.</para>
        ///
        /// <para>Ungepflegte Prioritaet ans ENDE
        /// (<c>Ladeordnung.SqlAnlagenprio</c>), sonst stuende eine frisch angelegte
        /// Anlage in der Kopfzeile vor der konfigurierten.</para>
        /// </summary>
        public static List<string> AnlagenNamen(int idProjekt, int idType)
        {
            List<string> namen = new List<string>();
            if (idProjekt <= 0 || idType <= 0) return namen;

            DataTable dt = StilleDb.Tabelle(
                "SELECT Bezeichner FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type = ? " +
                "ORDER BY " + Ladeordnung.SqlAnlagenprio(null) + ", ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@typ", DbParamTyp.Integer, idType));
            if (dt == null) return namen;

            foreach (DataRow r in dt.Rows)
            {
                string name = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                if (name.Length > 0 && !namen.Contains(name)) namen.Add(name);
            }
            return namen;
        }

        /// <summary>
        /// Alle WAERMEERZEUGER des Projekts mit ID und Bezeichner, in Ladereihenfolge
        /// (Vorlaeufer <c>QuellnutzerSammeln</c>:2081-2111).
        ///
        /// <para>Wer davon einen Puffer als Waermequelle nutzt, entscheidet der
        /// Aufrufer ueber <c>WaermesenkeClass.QuellPufferDerAnlage</c> — dieselbe
        /// Rangfolge (Fremdschluessel vor Bezeichner), die Engine und Erzeugerkarte
        /// benutzen. Ein zweiter Vergleich hier koennte bei Altbestand eine andere
        /// Antwort geben.</para>
        /// </summary>
        public static List<AnlagenKurz> Quellnutzer(int idProjekt)
        {
            List<AnlagenKurz> liste = new List<AnlagenKurz>();
            if (idProjekt <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, Bezeichner FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ") " +
                "ORDER BY " + Ladeordnung.SqlAnlagenprio(null) + ", ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (id <= 0) continue;

                liste.Add(new AnlagenKurz
                {
                    ID = id,
                    Bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"))
                });
            }
            return liste;
        }

        /// <summary>
        /// Alle Projektanlagen eines Typs samt WP-Bauart und Senkenkette
        /// (Vorlaeufer <c>AnlagenImProjekt</c>:371-430).
        ///
        /// <para>Die Senken kommen aus <c>Z_AnlageSenke</c> — EINE Abfrage fuer das
        /// ganze Projekt, je Anlage zugeteilt. Ohne eigene Zeile steht die
        /// Rang-1-Vorbelegung <c>Heizkreis/Beides</c> da, dieselbe, mit der die Engine
        /// rechnet.</para>
        ///
        /// <para><b>Anlagen ohne Bezeichner fallen weg</b> (:426-427) — woertlich
        /// uebernommen: Eine namenlose Zeile ist im Bestand eine halb angelegte Anlage,
        /// und eine Karte ohne Titel waere nicht bedienbar.</para>
        /// </summary>
        public static List<AnlagenInfo> AnlagenMitWp(int idProjekt, int idType)
        {
            List<AnlagenInfo> anlagen = new List<AnlagenInfo>();
            if (idProjekt <= 0 || idType <= 0) return anlagen;

            DataTable dt = StilleDb.Tabelle(
                "SELECT a.ID, a.Bezeichner, a.Prioritaet, a.WQ_Typ, a.WQ_Temp, a.BM_Typ, " +
                "       a.Vorlauf, a.[Rücklauf] AS Ruecklauf, " +
                "       w.Typ AS WPTyp " +
                "FROM Tab_Energieanlagen AS a LEFT JOIN Tab_WP AS w ON a.ID_WP = w.ID " +
                "WHERE a.ID_Projekt = ? AND a.ID_Type = ? " +
                "ORDER BY " + Ladeordnung.SqlAnlagenprio("a") + ", a.ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@typ", DbParamTyp.Integer, idType));
            if (dt == null) return anlagen;

            Dictionary<int, List<Z_AnlageSenkeModel>> senken = SenkenJeAnlage(idProjekt);

            foreach (DataRow r in dt.Rows)
            {
                AnlagenInfo info = new AnlagenInfo();
                info.ID_Type = idType;
                info.ID = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                info.Bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                info.Prioritaet = StilleDb.Zahl(StilleDb.Feld(r, "Prioritaet"));
                info.Vorlauf = StilleDb.Zahl(StilleDb.Feld(r, "Vorlauf"));
                info.Ruecklauf = StilleDb.Zahl(StilleDb.Feld(r, "Ruecklauf"));
                info.WpTyp = StilleDb.Text(StilleDb.Feld(r, "WPTyp"));
                info.WQ_Typ = StilleDb.Text(StilleDb.Feld(r, "WQ_Typ"));
                info.WQ_Temp = StilleDb.Kommazahl(StilleDb.Feld(r, "WQ_Temp"));
                info.BM_Typ = StilleDb.Text(StilleDb.Feld(r, "BM_Typ"));

                List<Z_AnlageSenkeModel> kette;
                info.Senken = senken.TryGetValue(info.ID, out kette) && kette.Count > 0
                    ? kette : VorbelegungRang1(info.ID);

                if (!string.IsNullOrEmpty(info.Bezeichner)) anlagen.Add(info);
            }

            return anlagen;
        }

        /// <summary>
        /// Die Senkenzeilen ALLER Anlagen des Projekts, nach Anlagen-ID gebuendelt und in
        /// Rangfolge — EINE Abfrage auf <c>Z_AnlageSenke</c>. Nie <c>null</c>.
        ///
        /// <para>Fehlt die Tabelle (Migration nicht durchgekommen), bleibt die Sammlung
        /// leer; der Aufrufer setzt dann die Rang-1-Vorbelegung. Dass die
        /// Konfigurationsseite auf einem solchen Schema ueberhaupt aufgeht, verhindert
        /// bereits <c>SchemaMigration.SimulationGesperrt</c>.</para>
        /// </summary>
        internal static Dictionary<int, List<Z_AnlageSenkeModel>> SenkenJeAnlage(int idProjekt)
        {
            Dictionary<int, List<Z_AnlageSenkeModel>> map =
                new Dictionary<int, List<Z_AnlageSenkeModel>>();
            if (idProjekt <= 0 || !Z_AnlageSenkeCtrl.SpalteVorhanden()) return map;

            foreach (Z_AnlageSenkeModel z in new Z_AnlageSenkeCtrl().LesenJeProjekt(idProjekt))
            {
                if (z == null || z.ID_Anlage <= 0) continue;

                List<Z_AnlageSenkeModel> kette;
                if (!map.TryGetValue(z.ID_Anlage, out kette))
                {
                    kette = new List<Z_AnlageSenkeModel>();
                    map[z.ID_Anlage] = kette;
                }
                kette.Add(z);
            }

            return map;
        }

        /// <summary>
        /// Die RANG-1-INVARIANTE als Liste (Konzept 5.1): <c>Heizkreis/Beides</c> — genau
        /// das, was die Engine fuer eine Anlage ohne Senkenzeile rechnet.
        /// </summary>
        internal static List<Z_AnlageSenkeModel> VorbelegungRang1(int idAnlage)
        {
            List<Z_AnlageSenkeModel> kette = new List<Z_AnlageSenkeModel>();
            kette.Add(new Z_AnlageSenkeModel
            {
                ID_Anlage = idAnlage,
                Rang = 1,
                Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                Bedarfsart = WaermequelleClass.SENKE_BEIDES
            });
            return kette;
        }
    }
}
