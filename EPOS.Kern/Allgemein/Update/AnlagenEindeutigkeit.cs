using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Beschreibt EINE gesperrte Geräteverweis-Spalte in <c>Tab_Energieanlagen</c>:
    /// welche Spalte, auf welche Gerätetabelle sie zeigt und welche Kindtabellen eine
    /// Gerätekopie mitnehmen muss.
    /// </summary>
    public sealed class GeraeteSperre
    {
        /// <summary>Spalte in <c>Tab_Energieanlagen</c>, z. B. <c>ID_WP</c>.</summary>
        public readonly string Spalte;

        /// <summary>Projekttabelle des Geräts, z. B. <c>Tab_WP</c>.</summary>
        public readonly string Tabelle;

        /// <summary>Klartext für das Migrationsprotokoll (deutsch, keine Anzeige).</summary>
        public readonly string Gewerk;

        /// <summary>
        /// Kindtabellen der Gerätetabelle als Paare (Tabelle, Fremdschlüsselspalte).
        /// Eine Gerätekopie ohne ihre Kinder wäre bei der Wärmepumpe eine Kopie OHNE
        /// Kennlinien - rechnerisch wertlos.
        /// </summary>
        public readonly string[][] Kinder;

        public GeraeteSperre(string spalte, string tabelle, string gewerk, string[][] kinder)
        {
            Spalte = spalte;
            Tabelle = tabelle;
            Gewerk = gewerk;
            Kinder = kinder ?? new string[0][];
        }
    }

    /// <summary>
    /// EINE Zeile je Projekt und Gerät - Prüfung beim Anlegen (Teil A), Eindeutigkeits-
    /// index in der Datenbank (Teil B) und Abschlussbericht der Migration (Teil C).
    ///
    /// <para>
    /// <b>Der Leitgedanke.</b> Die Simulation baut ihre Modullisten JE ANLAGENZEILE auf
    /// (<c>SimulationControl.WP_Liste_Laden</c>, <c>SPK_Liste_Laden</c>,
    /// <c>BHKW_Liste_Laden</c> - kein DISTINCT); zeigen zwei Zeilen desselben Projekts auf
    /// dasselbe Gerät, zählt es doppelt. Die Kostenseite zählt seit Commit 605dcb8
    /// dagegen JE GERÄT (<c>TechnikPlanwertCtrl</c>, GROUP BY Verweisspalte). Solange
    /// Doppelzeilen möglich sind, widersprechen sich beide Deutungen. Ist je Projekt und
    /// Gerät nur EINE Zeile erlaubt, sind „je Zeile" und „je Gerät" wieder dasselbe -
    /// genau das stellt diese Klasse her.
    /// </para>
    ///
    /// <para>
    /// <b>Vier Spalten, nicht sieben.</b> Gesperrt werden <c>ID_WP</c>,
    /// <c>ID_Kessel</c>, <c>ID_BHKW</c> und <c>ID_PUFFER</c> - Geräte, die es im Projekt
    /// physisch nur einmal gibt. <c>ID_PV</c> und <c>ID_Solar</c> bleiben FREI: Mehrere
    /// Felder desselben Modultyps mit unterschiedlicher Neigung, Azimut und Stückzahl sind
    /// fachlich richtig (die Engine rechnet dort bewusst je Zeile). <c>ID_SP</c> bleibt
    /// ebenfalls frei - dort ist eine zweite Zeile eine VARIANTE desselben Speichers, kein
    /// zweiter Speicher (Fachkonzept Stromspeicher 7.3).
    /// </para>
    ///
    /// <para>
    /// <b>ARBEITSPAKET S4c/S4d - WER RUFT DIESE KLASSE.</b> Sie ist NICHT nur Zubehör der
    /// Migration, sondern zur SQLite-LAUFZEIT erreichbar: <c>WizardCtrl</c> ruft
    /// <see cref="Aufnehmen"/> (und darüber <see cref="ProjektkopieAnlegen"/>),
    /// <c>PufferSpCtrl</c> ruft <see cref="ZeileVorhanden"/>, <c>Form_PufferSp</c> ruft
    /// <see cref="BereitsInListe"/> und <see cref="ZweitesGeraetBestaetigen"/>. Nur der
    /// DDL-Teil - <see cref="SqlIndex"/> und <see cref="IndexName"/> - hat allein die
    /// eingefrorene <c>SchemaMigration</c> als Aufrufer.
    /// </para>
    ///
    /// <para>
    /// Eine eigene Datenbankverbindung hält die Klasse nicht (und hielt sie nie): Jeder
    /// Zugriff läuft über <see cref="StilleDb"/> und damit über die Zugriffsschicht. Die
    /// <see cref="DbParamTyp"/>-Angaben in <c>Spaltentyp</c> und <c>StilleDb.Par</c> sind
    /// nur noch Datenträger - siehe die Begründung dort.
    /// </para>
    /// </summary>
    public static class AnlagenEindeutigkeit
    {
        // =================================================================================
        // Spalten und Sperren
        // =================================================================================

        // Die vier Spaltennamen stehen seit iU3 bei Anlagenzeilen (Kante K4) - dort
        // braucht sie der Rechenpfad ohne den Dialogteil dieser Klasse. Hier bleiben sie
        // als Weiterleitung stehen, damit alle bestehenden Aufrufer gültig bleiben.
        public const string SPALTE_WP = Anlagenzeilen.SPALTE_WP;
        public const string SPALTE_KESSEL = Anlagenzeilen.SPALTE_KESSEL;
        public const string SPALTE_BHKW = Anlagenzeilen.SPALTE_BHKW;
        public const string SPALTE_PUFFER = Anlagenzeilen.SPALTE_PUFFER;

        /// <summary>Die vier gesperrten Geräteverweise - EINE Wahrheit für Dialog, Index und Bericht.</summary>
        public static readonly GeraeteSperre[] SPERREN =
        {
            // Die Wärmepumpe ist der einzige Fall mit Kindtabellen: ohne Kennlinien
            // liefert SimulationWaermepumpe für die Kopie keinen COP.
            new GeraeteSperre(SPALTE_WP, "Tab_WP", "Wärmepumpe",
                              new[] { new[] { "Tab_Kenndaten", "ID_WP" },
                                      new[] { "Tab_Kenndaten_Kuehlung", "ID_WP" } }),

            new GeraeteSperre(SPALTE_KESSEL, "Tab_Heizkessel", "Heizkessel", null),
            new GeraeteSperre(SPALTE_BHKW, "Tab_BHKW", "BHKW", null),
            new GeraeteSperre(SPALTE_PUFFER, SchemaKatalog.TAB_PUFFERSPEICHER, "Pufferspeicher", null),
        };

        /// <summary>Sperre zu einer Spalte, oder <c>null</c>.</summary>
        public static GeraeteSperre Sperre(string spalte)
        {
            foreach (GeraeteSperre s in SPERREN)
                if (string.Equals(s.Spalte, spalte, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }

        // =================================================================================
        // Teil B - SQL für Index und Dublettenprüfung
        // =================================================================================

        /// <summary>Indexname zur Spalte, z. B. <c>idx_Anlage_ID_WP</c>.</summary>
        public static string IndexName(string spalte)
        {
            return "idx_Anlage_" + spalte;
        }

        /// <summary>
        /// Zusammengesetzter Eindeutigkeitsindex (<c>ID_Projekt</c>, Verweisspalte).
        ///
        /// <para>
        /// ACE/Jet lässt in einem eindeutigen Index MEHRERE NULL zu; die Sperre greift
        /// deshalb nur für Zeilen, die den Verweis tatsächlich führen. Genau darauf ist
        /// <c>WizardCtrl.AnlagenParameter</c> ausgelegt - für nicht passende Anlagentypen
        /// schreibt es durchgehend <see cref="DBNull"/>, nie 0. SQLite verhält sich hier
        /// GLEICH (NULL gilt in einem UNIQUE-Index als von jedem anderen NULL verschieden)
        /// - die Aussage oben bleibt nach der Migration also unverändert gültig.
        /// </para>
        ///
        /// <para>
        /// ARBEITSPAKET S4d - <c>IF NOT EXISTS</c>: Ein zweiter Anlauf auf einen bereits
        /// vorhandenen Index ist damit KEIN Fehler mehr, sondern wirkungslos. Das ist für
        /// diesen Index wesentlich, weil er ausdrücklich NACHZIEHEN können soll, sobald
        /// der Bestand dublettenfrei ist (siehe <c>SchemaMigration.EindeutigkeitAbschluss</c>)
        /// - die Anweisung läuft also an jedem Programmstart erneut. Wer unterscheiden
        /// muss, ob der Index NEU entstanden ist oder schon stand, fragt vorher
        /// <see cref="IndexVorhanden"/>.
        /// </para>
        /// </summary>
        public static string SqlIndex(string spalte)
        {
            return "CREATE UNIQUE INDEX IF NOT EXISTS [" + IndexName(spalte) + "]" +
                   " ON [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] (ID_Projekt, [" + spalte + "])";
        }

        /// <summary>
        /// Vorabprobe zu <see cref="SqlIndex"/>: Steht der Eindeutigkeitsindex dieser
        /// Spalte bereits in der Datenbank?
        ///
        /// <para>
        /// STILL wie der Rest der Klasse: über <see cref="StilleDb"/>, nicht über
        /// <c>DataRepository.IndexListe</c>. Die Probe läuft im Migrationslauf beim
        /// Programmstart; eine MessageBox aus der Auskunft heraus wäre dort genau der
        /// hängende Lauf, den die stille Fassung verhindert.
        /// </para>
        ///
        /// <para>
        /// Ein Aufrufer fehlt heute noch: Angelegt wird der Index ausschließlich von
        /// <c>SchemaMigration.EindeutigkeitAbschluss</c>, und die SchemaMigration ist der
        /// eingefrorene Access-Zweig (Umbau erst mit S6/S8). Bis dahin trägt
        /// <c>IF NOT EXISTS</c> die Idempotenz allein.
        /// </para>
        /// </summary>
        public static bool IndexVorhanden(string spalte)
        {
            if (string.IsNullOrWhiteSpace(spalte)) return false;

            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM sqlite_master " +
                "WHERE type = 'index' AND name = ? AND tbl_name = ?",
                StilleDb.Par("@idx", DbParamTyp.VarWChar, IndexName(spalte)),
                StilleDb.Par("@tab", DbParamTyp.VarWChar, SchemaKatalog.TAB_ENERGIEANLAGEN))) > 0;
        }

        /// <summary>
        /// Die Dublettengruppen einer Spalte: je Projekt und Geräte-ID die Zahl der Zeilen,
        /// sofern mehr als eine.
        ///
        /// <para>
        /// <b>0 zählt mit.</b> Geprüft wird <c>IS NOT NULL</c>, nicht <c>&gt; 0</c> - ein
        /// als 0 geschriebener Platzhalter ist für den Index ein WERT und würde ihn
        /// genauso scheitern lassen wie eine echte Dublette. Die Prüfung muss deshalb
        /// dasselbe sehen wie der Index, sonst meldet sie „sauber" und das
        /// <c>CREATE UNIQUE INDEX</c> scheitert danach doch.
        /// </para>
        /// </summary>
        public static string SqlDublettenGruppen(string spalte)
        {
            return "SELECT ID_Projekt, [" + spalte + "] AS Geraet, COUNT(*) AS Anzahl " +
                   "FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] " +
                   "WHERE [" + spalte + "] IS NOT NULL " +
                   "GROUP BY ID_Projekt, [" + spalte + "] " +
                   "HAVING COUNT(*) > 1 " +
                   "ORDER BY ID_Projekt, [" + spalte + "]";
        }

        /// <summary>
        /// Die EINZELZEILEN aller Dublettengruppen einer Spalte - Grundlage für die
        /// Meldung, die Projekt, Gewerk und betroffene Zeilen benennt.
        /// </summary>
        public static string SqlDublettenZeilen(string spalte)
        {
            string t = "[" + SchemaKatalog.TAB_ENERGIEANLAGEN + "]";

            return "SELECT a.ID_Projekt, a.[" + spalte + "] AS Geraet, a.ID, a.Bezeichner " +
                   "FROM " + t + " AS a " +
                   "WHERE a.[" + spalte + "] IS NOT NULL AND EXISTS (" +
                   "  SELECT 1 FROM " + t + " AS b " +
                   "  WHERE b.ID_Projekt = a.ID_Projekt AND b.[" + spalte + "] = a.[" + spalte + "]" +
                   "  AND b.ID <> a.ID) " +
                   "ORDER BY a.ID_Projekt, a.[" + spalte + "], a.ID";
        }

        // =================================================================================
        // Teil A - Rückfrage und Hinweis
        // =================================================================================

        /// <summary>
        /// Die Rückfrage an den Anwender: Text und Titel hinein, „Ja" heraus.
        ///
        /// <para>
        /// AUSTAUSCHBAR, damit der Prüfstand beide Antworten fahren kann, ohne einen
        /// Dialog zu bedienen - dieselbe Bauform, mit der <c>DataRepository.FehlerMelden</c>
        /// zwischen Dialog und Protokoll unterscheidet. <c>null</c> setzt den
        /// Vorgabeweg wieder ein.
        /// </para>
        /// </summary>
        public static Func<string, string, bool> Frage;

        /// <summary>Reiner Hinweis ohne Entscheidung (PV/Solar, Umbenennung einer Speichervariante).</summary>
        public static Action<string, string> Hinweis;

        /// <summary>
        /// Stellt die Rückfrage. Im Engine-Modus gibt es keinen Anwender: Dort lautet die
        /// Antwort JA und der Vorgang landet im Protokoll.
        ///
        /// <para>
        /// WARUM JA UND NICHT NEIN. „Nein" verwirft die Anlagenzeile - stiller
        /// Datenverlust. „Ja" behält sie und legt eine eigene Gerätekopie an; das Ergebnis
        /// ist vollständig und verletzt den Index nicht. Ein unbeantwortbarer Fall darf
        /// nicht die Variante mit dem größeren Schaden wählen.
        /// </para>
        /// </summary>
        private static bool Fragen(string text, string titel)
        {
            if (DataRepository.EngineModusAktiv)
            {
                Console.WriteLine("Eindeutigkeit der Anlagenzeilen: " + text +
                                  " - ohne Bedienung wird eine eigene Gerätekopie angelegt.");
                return true;
            }

            Func<string, string, bool> f = Frage;
            if (f != null) return f(text, titel);

            return Dienste.Dialog.Frage(text, titel);
        }

        /// <summary>Hinweis anzeigen - im Engine-Modus nur ins Protokoll.</summary>
        private static void Melden(string text, string titel)
        {
            if (DataRepository.EngineModusAktiv)
            {
                Console.WriteLine("Eindeutigkeit der Anlagenzeilen: " + text);
                return;
            }

            Action<string, string> h = Hinweis;
            if (h != null) { h(text, titel); return; }

            Dienste.Dialog.Meldung(text, titel);
        }

        /// <summary>
        /// Die Rückfrage für die OBERFLÄCHE - dieselbe Frage mit demselben Wortlaut, die
        /// der Schreibweg stellen würde.
        ///
        /// <para>
        /// WOZU SIE IM DIALOG STEHT, OBWOHL DER SCHREIBWEG SIE OHNEHIN STELLT: Der
        /// Anwender soll die Meldung sehen, WÄHREND er die Anlage aufnimmt - nicht erst
        /// beim Speichern, wenn er die Liste längst zusammengestellt hat. Wer hier „Ja"
        /// sagt, wird über <c>WErzeugerModel.GeraetekopieErzwingen</c> nicht ein zweites
        /// Mal gefragt.
        /// </para>
        /// </summary>
        public static bool ZweitesGeraetBestaetigen(string bezeichner)
        {
            return Fragen(string.Format(MyResource.Resource.ANL_DUBLETTE_FRAGE, (bezeichner ?? "").Trim()),
                          MyResource.Resource.ANL_DUBLETTE_TITEL);
        }

        /// <summary>
        /// UI-Vorprüfung: Führt die Auswahlliste bereits eine Anlage desselben Typs mit
        /// demselben Bezeichner? Das ist genau der Fall, aus dem im Schreibweg die
        /// Dublette entsteht - <c>CopyFromStamm</c> löst beide Einträge über den
        /// Bezeichner auf dieselbe Projektkopie auf.
        /// </summary>
        public static bool BereitsInListe(IEnumerable<WErzeugerModel> liste, int idType, string bezeichner)
        {
            if (liste == null) return false;

            string gesucht = (bezeichner ?? "").Trim();
            if (gesucht.Length == 0) return false;

            foreach (WErzeugerModel m in liste)
            {
                if (m == null || m.ID_Type != idType) continue;
                if (string.Equals((m.Bezeichner ?? "").Trim(), gesucht, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        // =================================================================================
        // Teil A - Belegung eines Schreibdurchlaufs
        // =================================================================================

        /// <summary>
        /// Merkt sich die in EINEM Schreibdurchlauf bereits vergebenen Geräte-IDs.
        ///
        /// <para>
        /// WARUM NICHT NUR DIE DATENBANK BEFRAGEN. Der Speicherweg aller Erzeuger ist
        /// Löschen + Neuanlegen (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> gefolgt von
        /// <c>Add_WP_Waermeerzeuger</c>). Während der Schleife sind die alten Zeilen
        /// bereits weg und die neuen noch nicht alle da - die Datenbank allein kann die
        /// Dublette also gar nicht sehen. Sie entsteht innerhalb der Liste, und genau dort
        /// wird sie hier auch erkannt.
        /// </para>
        /// </summary>
        public sealed class Belegung
        {
            private readonly Dictionary<string, HashSet<int>> _je =
                new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

            private readonly HashSet<string> _namen =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public bool IstBelegt(string spalte, int idGeraet)
            {
                HashSet<int> m;
                return _je.TryGetValue(spalte, out m) && m.Contains(idGeraet);
            }

            public void Merken(string spalte, int idGeraet)
            {
                HashSet<int> m;
                if (!_je.TryGetValue(spalte, out m)) { m = new HashSet<int>(); _je[spalte] = m; }
                m.Add(idGeraet);
            }

            /// <summary>Anlagen-Bezeichner dieses Durchlaufs - Grundlage der Namensprüfung.</summary>
            public bool NameVergeben(string name)
            {
                return _namen.Contains((name ?? "").Trim());
            }

            public void NameMerken(string name)
            {
                _namen.Add((name ?? "").Trim());
            }
        }

        // =================================================================================
        // Teil A - die Prüfung selbst
        // =================================================================================

        /// <summary>
        /// Nimmt ein Gerät in eine Anlagenzeile auf und hält dabei die Regel „eine Zeile je
        /// Projekt und Gerät" ein.
        /// </summary>
        /// <returns>
        /// Die zu schreibende Geräte-ID; <c>0</c>, wenn die Anlagenzeile NICHT geschrieben
        /// werden soll (Anwender hat verneint oder die Gerätekopie ist gescheitert).
        /// </returns>
        /// <param name="kopieErzwingen">
        /// Vorentscheidung aus der Oberfläche (<c>WErzeugerModel.GeraetekopieErzwingen</c>):
        /// Der Anwender wurde bereits gefragt und hat „Ja" gesagt - dann wird hier NICHT
        /// erneut gefragt. Ohne diese Weitergabe käme dieselbe Frage zweimal.
        /// </param>
        public static int Aufnehmen(GeraeteSperre sperre, int idProjekt, int idGeraet,
                                    WErzeugerModel item, Belegung belegt, bool kopieErzwingen)
        {
            if (sperre == null || idGeraet <= 0) return idGeraet;

            bool doppelt = (belegt != null && belegt.IstBelegt(sperre.Spalte, idGeraet))
                           || ZeileVorhanden(sperre.Spalte, idProjekt, idGeraet);

            if (!doppelt && !kopieErzwingen)
            {
                if (belegt != null) belegt.Merken(sperre.Spalte, idGeraet);
                return idGeraet;
            }

            if (!doppelt)
            {
                // Die Oberfläche hat eine Kopie angekündigt, die Datenbank sieht aber keine
                // Dublette (z. B. weil die Vorgängerzeile inzwischen entfernt wurde). Dann
                // ist die Kopie überflüssig - das Gerät bleibt, wie es ist.
                if (belegt != null) belegt.Merken(sperre.Spalte, idGeraet);
                return idGeraet;
            }

            string bezeichner = (item != null ? item.Bezeichner : null) ?? "";

            if (!kopieErzwingen &&
                !Fragen(string.Format(MyResource.Resource.ANL_DUBLETTE_FRAGE, bezeichner),
                        MyResource.Resource.ANL_DUBLETTE_TITEL))
            {
                Console.WriteLine("Anlagenzeile \"" + bezeichner + "\" (" + sperre.Spalte +
                                  " = " + idGeraet + ") wurde auf Wunsch nicht aufgenommen - " +
                                  "das Gerät ist im Projekt " + idProjekt + " bereits enthalten.");
                return 0;
            }

            string name = EindeutigerBezeichner(sperre.Tabelle, idProjekt, bezeichner, 0);
            int neu = ProjektkopieAnlegen(sperre, idGeraet, idProjekt, name);

            if (neu <= 0)
            {
                Melden(string.Format(MyResource.Resource.ANL_DUBLETTE_KOPIE_FEHLER, bezeichner),
                       MyResource.Resource.ANL_DUBLETTE_TITEL);
                return 0;
            }

            // Der Bezeichner der ANLAGENZEILE muss mitwandern: GetProjektId,
            // Z_ProjektPufferSp.Pufferspeicher, PufferSpCtrl.ProjektWaisenEntfernen und
            // die Rettung der Speichervarianten lösen weiterhin über den Namen auf.
            if (item != null) item.Bezeichner = name;
            if (belegt != null) belegt.Merken(sperre.Spalte, neu);

            Console.WriteLine("Anlagenzeile \"" + bezeichner + "\": eigene Gerätekopie \"" + name +
                              "\" in " + sperre.Tabelle + " angelegt (ID " + neu + "), weil " +
                              sperre.Spalte + " = " + idGeraet + " im Projekt " + idProjekt +
                              " bereits belegt war.");
            return neu;
        }

        /// <summary>
        /// Gibt es im Projekt bereits eine Anlagenzeile auf dieses Gerät?
        /// Weiterleitung auf <see cref="Anlagenzeilen.ZeileVorhanden"/> (Kante K4).
        /// </summary>
        public static bool ZeileVorhanden(string spalte, int idProjekt, int idGeraet)
        {
            return Anlagenzeilen.ZeileVorhanden(spalte, idProjekt, idGeraet);
        }

        // =================================================================================
        // Teil A - eindeutiger Bezeichner und Gerätekopie
        // =================================================================================

        /// <summary>
        /// Ein im Projekt noch nicht vergebener Gerätebezeichner: „PS 800", „PS 800 (2)", …
        ///
        /// Verallgemeinerung von <c>PufferSpCtrl.EindeutigerBezeichner</c> auf alle vier
        /// Gerätetabellen; dort steht die ausführliche Begründung, warum gleiche NAMEN
        /// trotz erlaubter Mehrfachanlage nicht vorkommen dürfen (die bezeichnerbasierten
        /// Altpfade lösen weiterhin über den Namen auf).
        /// </summary>
        /// <param name="idAusnahme">Geräte-ID, die beim Namensvergleich übergangen wird (Ändern).</param>
        public static string EindeutigerBezeichner(string tabelle, int idProjekt, string wunsch, int idAusnahme)
        {
            string basis = (wunsch ?? "").Trim();
            if (basis.Length == 0) basis = "Gerät";

            for (int n = 1; n < 1000; n++)
            {
                string kandidat = (n == 1) ? basis : basis + " (" + n + ")";

                int treffer = StilleDb.Zahl(StilleDb.Scalar(
                    "SELECT COUNT(*) FROM [" + tabelle + "] " +
                    "WHERE ID_Projekt = ? AND Bezeichner = ? AND ID <> ?",
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                    StilleDb.Par("@bez", DbParamTyp.VarWChar, kandidat),
                    StilleDb.Par("@aus", DbParamTyp.Integer, idAusnahme)));

                if (treffer == 0) return kandidat;
            }

            return basis + " (" + DateTime.Now.ToString("HHmmss") + ")";
        }

        /// <summary>
        /// Legt eine ZWEITE Projektkopie eines Geräts an - der Weg, den bisher kein
        /// Gewerk hatte.
        ///
        /// <para>
        /// <b>Warum nicht über <c>CopyFromStamm</c>.</b> Alle vier Gewerke prüfen dort
        /// zuerst <c>GetProjektId(Bezeichner, Projekt)</c> und geben bei einem Treffer die
        /// VORHANDENE ID zurück (<c>PufferSpCtrl.cs:206</c>, <c>WPCtrl.cs:244</c>,
        /// <c>HeizkesselCtrl.cs:188</c>, <c>BHKWCtrl.cs:253</c>) - eine zweite Kopie kann
        /// dieser Weg gar nicht erzeugen. Er scheitert außerdem an jedem Projektgerät,
        /// das im Katalog nicht (mehr) steht.
        /// </para>
        ///
        /// <para>
        /// <b>Kopiert wird deshalb die PROJEKTZEILE, nicht die Katalogzeile.</b> Das ist
        /// auch fachlich das Richtige: Das zweite Gerät soll dem ersten gleichen, und das
        /// erste kann im Projekt längst bearbeitet worden sein (Investitionskosten,
        /// Vor-/Rücklauf, Schwellen des Puffers). Der Katalog wüsste davon nichts.
        /// </para>
        ///
        /// <para>
        /// Die Spaltenliste kommt aus der Quellzeile selbst - kein Gewerk braucht eine
        /// eigene INSERT-Anweisung, und eine später hinzugefügte Spalte wandert
        /// automatisch mit.
        /// </para>
        /// </summary>
        /// <returns>ID der neuen Gerätezeile, -1 bei Fehler.</returns>
        public static int ProjektkopieAnlegen(GeraeteSperre sperre, int idQuelle, int idProjekt, string neuerName)
        {
            if (sperre == null || idQuelle <= 0 || idProjekt <= 0) return -1;

            DataTable quelle = StilleDb.Tabelle(
                "SELECT * FROM [" + sperre.Tabelle + "] WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idQuelle));

            if (quelle == null || quelle.Rows.Count == 0)
            {
                Console.WriteLine("Gerätekopie: die Quellzeile " + idQuelle + " in " +
                                  sperre.Tabelle + " wurde nicht gefunden.");
                return -1;
            }

            int neueId = NaechsteId(sperre.Tabelle);
            if (neueId <= 0) return -1;

            Dictionary<string, object> ersatz = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            ersatz["ID"] = neueId;
            ersatz["ID_Projekt"] = idProjekt;
            ersatz["Bezeichner"] = neuerName;

            if (!ZeileKopieren(sperre.Tabelle, quelle.Rows[0], ersatz)) return -1;

            // Kindtabellen (heute nur die Kennlinien der Wärmepumpe).
            foreach (string[] kind in sperre.Kinder)
            {
                if (kind == null || kind.Length < 2) continue;
                KinderKopieren(kind[0], kind[1], idQuelle, neueId);
            }

            return neueId;
        }

        /// <summary>
        /// Nächste freie ID einer Gerätetabelle. Alle vier führen <c>ID</c> als LONG mit
        /// ausdrücklicher Vergabe über <c>MAX(ID)+1</c> - dieselbe Konvention wie
        /// <c>CopyFromStamm</c> und <c>DataRepository.GetMaxID</c>.
        /// </summary>
        private static int NaechsteId(string tabelle)
        {
            object max = StilleDb.Scalar("SELECT MAX(ID) FROM [" + tabelle + "]");
            return StilleDb.Zahl(max) + 1;
        }

        /// <summary>
        /// Schreibt eine Zeile mit dem Spaltensatz der Quellzeile neu; die Werte aus
        /// <paramref name="ersatz"/> gehen vor.
        /// </summary>
        private static bool ZeileKopieren(string tabelle, DataRow quelle, Dictionary<string, object> ersatz)
        {
            StringBuilder spalten = new StringBuilder();
            StringBuilder platzhalter = new StringBuilder();
            List<DbParam> ps = new List<DbParam>();

            foreach (DataColumn c in quelle.Table.Columns)
            {
                object wert;
                if (!ersatz.TryGetValue(c.ColumnName, out wert))
                    wert = (quelle[c] == DBNull.Value) ? null : quelle[c];

                if (spalten.Length > 0) { spalten.Append(", "); platzhalter.Append(", "); }
                spalten.Append('[').Append(c.ColumnName).Append(']');
                platzhalter.Append('?');

                ps.Add(StilleDb.Par("@p" + ps.Count, Spaltentyp(c, wert), wert));
            }

            int n = StilleDb.NonQuery(
                "INSERT INTO [" + tabelle + "] (" + spalten + ") VALUES (" + platzhalter + ")",
                ps.ToArray());

            if (n >= 0) return true;

            Console.WriteLine("Gerätekopie: die neue Zeile in " + tabelle + " konnte nicht geschrieben werden.");
            return false;
        }

        /// <summary>
        /// Kopiert die Kindzeilen eines Geräts auf die neue Geräte-ID (Kennlinien der
        /// Wärmepumpe). Fehlt die Kindtabelle auf einer fremden Datenbank, bleibt es bei
        /// einer Protokollzeile - die Gerätekopie selbst steht dann bereits.
        /// </summary>
        private static void KinderKopieren(string tabelle, string fkSpalte, int idQuelle, int idNeu)
        {
            DataTable kinder = StilleDb.Tabelle(
                "SELECT * FROM [" + tabelle + "] WHERE [" + fkSpalte + "] = ? ORDER BY ID",
                StilleDb.Par("@fk", DbParamTyp.Integer, idQuelle));

            if (kinder == null || kinder.Rows.Count == 0) return;

            int naechste = NaechsteId(tabelle);

            foreach (DataRow r in kinder.Rows)
            {
                Dictionary<string, object> ersatz =
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                ersatz["ID"] = naechste++;
                ersatz[fkSpalte] = idNeu;

                if (!ZeileKopieren(tabelle, r, ersatz)) return;
            }
        }

        /// <summary>
        /// Spaltentyp für den Parameter. Aus <see cref="DBNull"/> allein leitet der
        /// OLE-DB-Provider keinen Typ ab - dieselbe Begründung wie bei
        /// <c>ProjektPuffer.Par</c>.
        ///
        /// <para>
        /// <b>ARBEITSPAKET S4d - GEPRÜFT, BEWUSST NICHT UMGEBAUT.</b> Die zurückgegebene
        /// <see cref="DbParamTyp"/> wandert über <c>StilleDb.Par</c> in einen
        /// <see cref="DbParam"/> - und den wertet die Zugriffsschicht nicht mehr
        /// aus: <c>DataRepository.UebersetzeParameter</c> baut aus JEDEM Bestandsparameter
        /// einen <c>SqliteParameter</c> allein aus dem WERT (Name und Typangabe werden
        /// verworfen), <c>DataRepository.NormalisiereWert</c> hebt ihn danach auf die
        /// Speicherform der Datei (bool -&gt; 0/1, DateTime -&gt; ISO-Text, Guid -&gt; Text,
        /// decimal -&gt; double). Die Zeilen unten sind damit WIRKUNGSLOS, aber auch
        /// harmlos.
        /// </para>
        ///
        /// <para>
        /// Sie bleiben trotzdem stehen: Der Mapper ist die einzige Stelle, die den
        /// CLR-Typ einer Spalte überhaupt noch benennt, und ein Rückbau brächte nichts
        /// als ein zweites Muster für dieselbe Parameterübergabe. Der Fall MEMO
        /// (&gt; 255 Zeichen -&gt; <c>LongVarWChar</c>) ist unter SQLite ohnehin
        /// gegenstandslos: TEXT kennt keine Längengrenze, ein Abschneiden gibt es nicht
        /// mehr.
        /// </para>
        /// </summary>
        private static DbParamTyp Spaltentyp(DataColumn c, object wert)
        {
            Type t = c.DataType;

            if (t == typeof(string))
            {
                // Ein MEMO-Feld über VarWChar zu schreiben schneidet den Text ab; die
                // Länge des Wertes entscheidet, welche Bindung nötig ist.
                string s = wert as string;
                return (s != null && s.Length > 255) ? DbParamTyp.LongVarWChar : DbParamTyp.VarWChar;
            }
            if (t == typeof(bool)) return DbParamTyp.Boolean;
            if (t == typeof(byte) || t == typeof(short) || t == typeof(int)) return DbParamTyp.Integer;
            if (t == typeof(long)) return DbParamTyp.BigInt;
            if (t == typeof(float) || t == typeof(double)) return DbParamTyp.Double;
            if (t == typeof(decimal)) return DbParamTyp.Decimal;
            if (t == typeof(DateTime)) return DbParamTyp.Date;
            if (t == typeof(Guid)) return DbParamTyp.Guid;
            if (t == typeof(byte[])) return DbParamTyp.VarBinary;

            return DbParamTyp.Variant;
        }

        // =================================================================================
        // Teil A - PV/Solar und Speichervarianten (KEINE Sperre)
        // =================================================================================

        /// <summary>
        /// PV und Solarthermie: mehrere Felder desselben Modultyps sind richtig, ein
        /// VERSEHEN ist nur die exakte Wiederholung. Gemeldet wird deshalb ausschließlich
        /// der Fall „gleiches Gerät UND gleiche Neigung UND gleicher Azimut UND gleiche
        /// Modulanzahl" - und auch der nur als Hinweis, ohne die Aufnahme zu verhindern.
        /// </summary>
        /// <returns>true, wenn ein Hinweis gezeigt wurde.</returns>
        public static bool FeldHinweisPruefen(WErzeugerModel item, List<WErzeugerModel> bereits)
        {
            if (item == null || bereits == null) return false;

            foreach (WErzeugerModel v in bereits)
            {
                if (v == null || ReferenceEquals(v, item)) continue;
                if (v.ID_Type != item.ID_Type) continue;
                if (!string.Equals((v.Bezeichner ?? "").Trim(), (item.Bezeichner ?? "").Trim(),
                                   StringComparison.OrdinalIgnoreCase)) continue;

                if (v.m_Neigung != item.m_Neigung) continue;
                if (v.m_Azimut != item.m_Azimut) continue;
                if (v.Kollektormodulanzahl != item.Kollektormodulanzahl) continue;

                Melden(string.Format(MyResource.Resource.ANL_FELD_HINWEIS,
                                     (item.Bezeichner ?? "").Trim(),
                                     item.m_Neigung, item.m_Azimut, item.Kollektormodulanzahl),
                       MyResource.Resource.ANL_DUBLETTE_TITEL);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stromspeicher: KEINE Gerätesperre - eine zweite Zeile auf denselben Speicher ist
        /// eine weitere VARIANTE. Was auch dort nicht vorkommen darf, sind zwei Varianten
        /// GLEICHEN NAMENS: <c>WizardCtrl.SpVariantenWiederherstellen</c> ordnet die
        /// geretteten Betriebsparameter über den Bezeichner zu und träfe sonst immer
        /// dieselbe Zeile.
        ///
        /// <para>
        /// Die Prüfung stammt aus <c>StromspeicherKontextMenuCtrl.VarianteAnlegen</c>
        /// (dort <c>NameVergeben</c> mit Abbruch). Auf dem Wizard-Weg kann sie NICHT
        /// abbrechen - der Aufruf steht hinter einem bereits ausgeführten DELETE aller
        /// Anlagenzeilen, ein Abbruch wäre Datenverlust. Sie vergibt deshalb ein Suffix
        /// und sagt es dem Anwender.
        /// </para>
        /// </summary>
        /// <returns>Der zu verwendende Name.</returns>
        public static string SpeichervarianteBenennen(string name, Belegung belegt)
        {
            string basis = (name ?? "").Trim();
            if (belegt == null || !belegt.NameVergeben(basis)) return basis;

            for (int n = 2; n < 1000; n++)
            {
                string kandidat = basis + " (" + n + ")";
                if (belegt.NameVergeben(kandidat)) continue;

                Melden(string.Format(MyResource.Resource.ANL_SP_NAME_ANGEPASST, basis, kandidat),
                       MyResource.Resource.ANL_DUBLETTE_TITEL);
                return kandidat;
            }

            return basis;
        }
    }
}
