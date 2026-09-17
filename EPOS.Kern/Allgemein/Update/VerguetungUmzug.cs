using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE EINSPEISEVERGUETUNG ZIEHT VON DER TRAEGERKARTE ZU DEN WIRTSCHAFTLICHKEITS-
    // PARAMETERN - Migrationsschritt 84 (Anwenderentscheid SP-E-5 (a) vom 17.09.2026).
    //
    // WOZU. Die Traegerkarte des Stromtraegers trug einen Block "Verguetung fuer
    // eingespeisten Strom" mit v_pv (energy_project_settings.Verguetung_PV) und v_bhkw
    // (Verguetung_BHKW). Gelesen hat ihn NUR die Speicherwelt. Die Wirtschaftlichkeit
    // rechnete von jeher mit Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung und
    // Einspeiseverguetung_KWK und hat den Block nie angesehen. Damit gab es zwei
    // Wahrheiten fuer denselben eingespeisten Strom, und welche galt, hing vom Rechenweg
    // ab. Der Entscheid nimmt den Block von der Karte; die Parameter sind ab hier die
    // eine Quelle - fuer die Wirtschaftlichkeit wie fuer den Verkaufspreis der
    // Speicherwelt (StromPreisCtrl.VerguetungenBauen).
    //
    // WAS DER SCHRITT TUT. Er rettet die gepflegten Kartenwerte hinueber, damit kein
    // Projekt eine Zahl verliert, die jemand eingetragen hat:
    //
    //     Einspeiseverguetung      := Verguetung_PV   / 100,  wenn nicht gepflegt
    //     Einspeiseverguetung_KWK  := Verguetung_BHKW / 100,  wenn NULL
    //
    // EINHEITEN. Die Karte fuehrte ct/kWh, die Parameter fuehren EUR/kWh (so steht es an
    // WirtschaftlichkeitParameter.Einspeiseverguetung und so rechnet
    // WirtschaftlichkeitCtrl damit). Aus 5,0 ct/kWh werden 0,05 EUR/kWh - dieselbe Zahl,
    // andere Einheit. Liest die Speicherwelt sie zurueck, rechnet sie wieder 5,0 ct/kWh;
    // genau darum bleibt der Referenzlauf byte-gleich.
    //
    // 0 HEISST "NICHT GEPFLEGT". Einspeiseverguetung ist eine REAL-Spalte ohne
    // DDL-DEFAULT, aber jeder Parametersatz, den die Maske angelegt hat, traegt dort
    // eine 0. Eine Einspeisung, die nichts einbringt, und ein Feld, das niemand
    // angefasst hat, sind an dieser Zahl nicht zu unterscheiden. Der Schritt entscheidet
    // sich fuer "nicht gepflegt" und ueberschreibt die 0 - sonst verloere genau das
    // Projekt seinen Kartenwert, das schon einen Parametersatz hat. Bei
    // Einspeiseverguetung_KWK ist die Frage nicht zu stellen: Die Spalte ist nullbar,
    // und eine gepflegte 0 hat dort seit jeher die Bedeutung "kein eigener KWK-Satz".
    //
    // GEPFLEGTE PARAMETER GEWINNEN. Steht in Einspeiseverguetung schon ein Wert
    // ungleich 0, bleibt er stehen - er ist die juengere und die fachlich fuehrende
    // Angabe. Dasselbe gilt fuer einen nicht-NULLen KWK-Satz.
    //
    // EIN PROJEKT KANN MEHRERE STROMTRAEGER HABEN. Die Karte haengt an (Projekt,
    // Traeger), der Parametersatz am Projekt. Genommen wird der erste gepflegte Wert in
    // der Reihenfolge der Traeger-Id; weichen zwei Traeger desselben Projekts
    // voneinander ab, sagt das Protokoll es. Erfunden wird nichts - ein Mittelwert waere
    // eine Zahl, die niemand eingetragen hat.
    //
    // WIEDERHOLBAR. Nach dem Lauf sind die Parameter gepflegt; ein zweiter Lauf findet
    // nichts mehr und ZaehlungUmzug() liefert 0. Die Kartenspalten bleiben unberuehrt
    // stehen (kein DROP, kein Leeren) - eine aeltere Programmfassung auf derselben Datei
    // soll nicht auf einen fehlenden Namen laufen.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // NutzungsdauerSchema (75) bis StrompreisZerlegung (83): Die Anweisungen brauchen
    // drei Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Der Umzug der Einspeiseverguetung von der Traegerkarte in die
    /// Wirtschaftlichkeitsparameter (Schemaschritt 84) - EINE Quelle fuer Migration,
    /// Werkzeug und Nachweis.
    /// </summary>
    public static class VerguetungUmzug
    {
        /// <summary>Die Projekteinstellungen je (Projekt, Energietraeger).</summary>
        public const string TABELLE_KARTE = SchemaKatalog.ENERGY_PROJECT_SETTINGS;

        /// <summary>Der Traegerkatalog - er traegt das Preismodell.</summary>
        public const string TABELLE_TRAEGER = "energy_carrier";

        /// <summary>Die Wirtschaftlichkeitsparameter je Projekt.</summary>
        public const string TABELLE_PARAMETER = WirtschaftlichkeitCtrl.TAB_PARAMETER;

        /// <summary>Preismodell des Stromtraegers.</summary>
        public const string PREISMODELL_STROM = "ELECTRICITY";

        /// <summary>ct/kWh je EUR/kWh.</summary>
        private const double CT_JE_EUR = 100.0;

        /// <summary>
        /// Ein Projekt, wie der Schritt es vorfindet: die geretteten Kartenwerte und der
        /// Stand seines Parametersatzes.
        /// </summary>
        public sealed class Zeile
        {
            public int Projekt;

            /// <summary>Kartenwert v_pv [ct/kWh]; 0 = keiner gepflegt.</summary>
            public double KartePvCtKwh;

            /// <summary>Kartenwert v_bhkw [ct/kWh]; 0 = keiner gepflegt.</summary>
            public double KarteBhkwCtKwh;

            /// <summary>Traeger, dessen Kartenwerte genommen wurden.</summary>
            public int Traeger;

            /// <summary>true, wenn Traeger desselben Projekts verschiedene Werte tragen.</summary>
            public bool TraegerUneinig;

            /// <summary>true, wenn das Projekt ueberhaupt einen Parametersatz hat.</summary>
            public bool ParametersatzVorhanden;

            /// <summary>Gepflegte Einspeiseverguetung [EUR/kWh] oder <c>null</c>.</summary>
            public double? ParameterEvEurKwh;

            /// <summary>Gepflegter KWK-Satz [EUR/kWh] oder <c>null</c> (Spalte ist NULL).</summary>
            public double? ParameterEvKwkEurKwh;

            /// <summary>Ist am Projekt noch etwas zu tun?</summary>
            public bool ZuTun
            {
                get
                {
                    return (KartePvCtKwh != 0.0 && !ParameterEvEurKwh.HasValue)
                        || (KarteBhkwCtKwh != 0.0 && !ParameterEvKwkEurKwh.HasValue);
                }
            }
        }

        // =================================================================
        //  Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Projekte zieht der Schritt um? 0 = nichts zu tun (er ist gelaufen,
        /// oder es gab nie einen Kartenwert).
        /// </summary>
        public static int ZaehlungUmzug()
        {
            int n = 0;
            foreach (Zeile z in Zeilen())
                if (z.ZuTun) n++;
            return n;
        }

        /// <summary>
        /// Jedes Projekt mit mindestens einem gepflegten Kartenwert, samt dem Stand
        /// seines Parametersatzes - die Grundlage des Umzugs und seines Protokolls.
        /// </summary>
        public static IReadOnlyList<Zeile> Zeilen()
        {
            List<Zeile> liste = new List<Zeile>();
            Dictionary<int, Zeile> nachProjekt = new Dictionary<int, Zeile>();

            DataTable karte = DataRepository.GetDataTable(
                "SELECT eps.ID_Projekt, eps.[ID_Energieträger], " +
                "eps.[" + SchemaKatalog.SPALTE_VERGUETUNG_PV + "] AS vpv, " +
                "eps.[" + SchemaKatalog.SPALTE_VERGUETUNG_BHKW + "] AS vbhkw " +
                "FROM [" + TABELLE_KARTE + "] AS eps " +
                "INNER JOIN " + TABELLE_TRAEGER + " AS ec ON eps.[ID_Energieträger] = ec.id " +
                "WHERE ec.pricing_model = ? " +
                "ORDER BY eps.ID_Projekt, eps.[ID_Energieträger]",
                new DbParam("@pm", DbParamTyp.VarWChar) { Wert = PREISMODELL_STROM });

            if (karte == null) return liste;

            foreach (DataRow r in karte.Rows)
            {
                double pv = Zahl(karte, r, "vpv") ?? 0.0;
                double bhkw = Zahl(karte, r, "vbhkw") ?? 0.0;
                if (pv == 0.0 && bhkw == 0.0) continue;

                int projekt = Ganzzahl(karte, r, "ID_Projekt");
                int traeger = Ganzzahl(karte, r, "ID_Energieträger");

                Zeile z;
                if (nachProjekt.TryGetValue(projekt, out z))
                {
                    // Der erste Traeger gewinnt; ein abweichender zweiter wird gemeldet.
                    if (z.KartePvCtKwh != pv || z.KarteBhkwCtKwh != bhkw)
                        z.TraegerUneinig = true;
                    continue;
                }

                z = new Zeile
                {
                    Projekt = projekt,
                    Traeger = traeger,
                    KartePvCtKwh = pv,
                    KarteBhkwCtKwh = bhkw
                };
                nachProjekt[projekt] = z;
                liste.Add(z);
            }

            foreach (Zeile z in liste) ParameterstandLesen(z);
            return liste;
        }

        // =================================================================
        //  Der Umzug
        // =================================================================

        /// <summary>
        /// Zieht die gepflegten Kartenwerte in die Wirtschaftlichkeitsparameter um.
        ///
        /// <para><b>Wiederholbar.</b> Umgezogen wird nur, was drueben noch nicht
        /// gepflegt ist; nach dem Lauf liefert <see cref="ZaehlungUmzug"/> 0.</para>
        /// </summary>
        /// <returns>Das Protokoll - eine Zeile je angefasstem Projekt, leer wenn nichts war.</returns>
        public static IReadOnlyList<string> Umziehen()
        {
            List<string> protokoll = new List<string>();
            CultureInfo k = CultureInfo.InvariantCulture;

            foreach (Zeile z in Zeilen())
            {
                if (z.TraegerUneinig)
                    protokoll.Add("Projekt " + z.Projekt + ": mehrere Stromtraeger mit " +
                                  "verschiedenen Kartenwerten - genommen wird Traeger " +
                                  z.Traeger + " (v_pv " + z.KartePvCtKwh.ToString("0.###", k) +
                                  ", v_bhkw " + z.KarteBhkwCtKwh.ToString("0.###", k) +
                                  " ct/kWh). Ein Mittelwert waere eine Zahl, die niemand " +
                                  "eingetragen hat.");

                if (!z.ZuTun) continue;

                double? neuEv = (z.KartePvCtKwh != 0.0 && !z.ParameterEvEurKwh.HasValue)
                    ? (double?)(z.KartePvCtKwh / CT_JE_EUR) : null;
                double? neuEvKwk = (z.KarteBhkwCtKwh != 0.0 && !z.ParameterEvKwkEurKwh.HasValue)
                    ? (double?)(z.KarteBhkwCtKwh / CT_JE_EUR) : null;

                if (z.ParametersatzVorhanden) ParameterAendern(z, neuEv, neuEvKwk);
                else ParametersatzAnlegen(z, neuEv, neuEvKwk);

                protokoll.Add("Projekt " + z.Projekt + ": Einspeiseverguetung " +
                              (neuEv.HasValue
                                  ? z.KartePvCtKwh.ToString("0.###", k) + " ct/kWh -> " +
                                    neuEv.Value.ToString("0.#####", k) + " EUR/kWh"
                                  : "bleibt gepflegt") +
                              ", KWK-Satz " +
                              (neuEvKwk.HasValue
                                  ? z.KarteBhkwCtKwh.ToString("0.###", k) + " ct/kWh -> " +
                                    neuEvKwk.Value.ToString("0.#####", k) + " EUR/kWh"
                                  : "bleibt gepflegt") +
                              " (Parametersatz " +
                              (z.ParametersatzVorhanden ? "ergaenzt" : "angelegt") + ").");
            }

            return protokoll;
        }

        // =================================================================
        //  Die einzelnen Handgriffe
        // =================================================================

        /// <summary>Liest den Stand des Parametersatzes in die Zeile.</summary>
        private static void ParameterstandLesen(Zeile z)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Einspeiseverguetung AS ev, [" +
                SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "] AS evkwk " +
                "FROM [" + TABELLE_PARAMETER + "] WHERE ID_Projekt = ?",
                new DbParam("@p", DbParamTyp.Integer) { Wert = z.Projekt });

            if (dt == null || dt.Rows.Count == 0) return;

            z.ParametersatzVorhanden = true;
            DataRow r = dt.Rows[0];

            // 0 heisst "nicht gepflegt" - Begruendung im Kopfkommentar.
            double? ev = Zahl(dt, r, "ev");
            z.ParameterEvEurKwh = (ev.HasValue && ev.Value != 0.0) ? ev : null;

            // Beim KWK-Satz zaehlt allein NULL als "nicht gepflegt": Die Spalte ist
            // nullbar, und eine gepflegte 0 bedeutet dort "kein eigener KWK-Satz".
            z.ParameterEvKwkEurKwh = Zahl(dt, r, "evkwk");
        }

        /// <summary>Ergaenzt den vorhandenen Parametersatz - nur die leeren Felder.</summary>
        private static void ParameterAendern(Zeile z, double? ev, double? evKwk)
        {
            if (ev.HasValue)
                DataRepository.ExecuteNonQuery(
                    "UPDATE [" + TABELLE_PARAMETER + "] SET Einspeiseverguetung = ? " +
                    "WHERE ID_Projekt = ?",
                    new DbParam("@ev", DbParamTyp.Double) { Wert = ev.Value },
                    new DbParam("@p", DbParamTyp.Integer) { Wert = z.Projekt });

            if (evKwk.HasValue)
                DataRepository.ExecuteNonQuery(
                    "UPDATE [" + TABELLE_PARAMETER + "] SET [" +
                    SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "] = ? WHERE ID_Projekt = ?",
                    new DbParam("@vk", DbParamTyp.Double) { Wert = evKwk.Value },
                    new DbParam("@p", DbParamTyp.Integer) { Wert = z.Projekt });
        }

        /// <summary>
        /// Legt einen Parametersatz an, der NUR die Verguetung traegt.
        ///
        /// <para><b>Alle uebrigen Spalten bleiben NULL</b>, und das ist Absicht:
        /// <c>WirtschaftlichkeitCtrl.LadeParameter</c> setzt fuer jede leere Spalte den
        /// Vorgabewert der Klasse ein - genau den, den das Projekt ohne Parametersatz
        /// auch bekommen haette. Eine abgeschriebene Vorgabeliste waere die zweite
        /// Stelle fuer dieselben Zahlen und wuerde beim ersten Vorgabewechsel falsch.</para>
        /// </summary>
        private static void ParametersatzAnlegen(Zeile z, double? ev, double? evKwk)
        {
            int id = DataRepository.GetMaxID(TABELLE_PARAMETER, "ID") + 1;

            DataRepository.ExecuteNonQuery(
                "INSERT INTO [" + TABELLE_PARAMETER + "] " +
                "(ID, ID_Projekt, Einspeiseverguetung, [" +
                SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "], GeaendertAm) " +
                "VALUES (?,?,?,?,?)",
                new DbParam("@id", DbParamTyp.Integer) { Wert = id },
                new DbParam("@p", DbParamTyp.Integer) { Wert = z.Projekt },
                new DbParam("@ev", DbParamTyp.Double)
                { Wert = ev.HasValue ? (object)ev.Value : DBNull.Value },
                new DbParam("@vk", DbParamTyp.Double)
                { Wert = evKwk.HasValue ? (object)evKwk.Value : DBNull.Value },
                new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now });
        }

        // =================================================================
        //  Kleinigkeiten
        // =================================================================

        private static double? Zahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return null;
            object v = r[spalte];
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDouble(v);
        }

        private static int Ganzzahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return 0;
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }
    }
}
