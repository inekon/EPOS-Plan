using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Übernahme des KOMPONENTENBESTANDS eines Gewerks aus einer anderen Version
    /// desselben Stammprojekts (Stufe-1-Zeilen „Bestand" / „Anzahl Komponenten" der
    /// Unterschiedsanzeige auf der Seite „Übersicht").
    ///
    /// <para>
    /// WAS „ÜBERNEHMEN" HIER HEISST: Der Bestand des Gewerks im ZIEL wird durch den der
    /// QUELLE ersetzt — fehlende Komponenten entstehen, überzählige fallen weg,
    /// beidseitig vorhandene werden gleichgezogen. Umgesetzt als vollständiger Austausch
    /// der Kette, nicht als Feldabgleich: eine halb abgeglichene Kette (Gerät neu,
    /// Anlagenzeile alt) wäre der Zustand, den niemand mehr auseinanderhält.
    /// </para>
    ///
    /// <para>
    /// DIE KETTE JE GEWERK (<see cref="GewerkPlan"/>): die Gerätezeile(n) der
    /// Gerätetabelle (Projektkopie mit NEUER ID nach dem <c>MAX(ID)+1</c>-Hausmuster —
    /// Quell-IDs werden nie übernommen), deren gewerkspezifische Kindtabellen
    /// (Wärmepumpe: <c>Tab_Kenndaten</c> und <c>Tab_Kenndaten_Kuehlung</c>), die
    /// zugehörigen Zeilen in <c>Tab_Energieanlagen</c> über
    /// <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> mit
    /// <see cref="AnlagenSql.AnlagenParameter"/> — dieselbe eine Einfügeanweisung wie
    /// Wizard, Karten und Kontextmenüs — und beim Stromspeicher zusätzlich die
    /// Betriebsführung in <c>Tab_StromspeicherVariante</c>.
    /// </para>
    ///
    /// <para>
    /// PUFFERSPEICHER SIND DER SONDERFALL. Auf <c>Tab_Pufferspeicher</c> zeigen nicht nur
    /// die eigenen Anlagenzeilen (<c>ID_PUFFER</c>), sondern über das Quellen-/Senken-Modell
    /// auch die FREMDER Gewerke (<c>WS_ID_Puffer</c>, <c>WS_ID_Puffer2</c>,
    /// <c>WQ_ID_Puffer</c>) — und seit Migrationsschritt 50 ebenso die SENKENLISTE
    /// (<c>Z_AnlageSenke.ID_Puffer</c>, restriktive Beziehung
    /// <c>FK_AnlageSenke_Puffer</c>; Befund L-B1). Alle diese Beziehungen sind erzwungen:
    /// Ein Löschen scheitert, solange irgendeine Anlagen- oder Senkenzeile den Speicher
    /// noch führt. Der Ablauf löst diese Verweise deshalb ZUERST (gemerkt als Bezeichner),
    /// löscht dann, legt neu an und stellt die Verweise über den Bezeichner wieder her;
    /// was sich nicht auflösen lässt, bleibt leer und wird gemeldet — nie geraten.
    /// </para>
    ///
    /// <para>
    /// TRANSAKTION. Löschen und Anlegen laufen in EINER Transaktion
    /// (<see cref="DataRepository.Vorgang"/>): ein Abbruch in der Mitte ließe
    /// sonst ein Projekt ohne Gewerk zurück. Die Betriebsführung des Stromspeichers
    /// entsteht bewusst NACH dem Commit über
    /// <see cref="StromspeicherVarianteCtrl.Insert"/> und
    /// <see cref="StromspeicherVarianteCtrl.SetzeAktiv"/> — die Anlagen-ID ist ein
    /// AutoWert und steht erst danach fest, und die Zusage „genau eine aktive Variante je
    /// Projekt" hat dort ihre EINE Schreibstelle (dieselbe Begründung wie in
    /// <c>StromspeicherKontextMenuCtrl</c>).
    /// </para>
    /// </summary>
    public class KomponentenUebernahmeCtrl
    {
        // ------------------------------------------------------------------ Landkarte

        /// <summary>Die vollständige Tabellenkette eines Gewerks.</summary>
        public class GewerkPlan
        {
            /// <summary>Gewerk-Schlüssel wie in <see cref="ProjektDetails.GewerkTabellen"/>.</summary>
            public string Gewerk;

            /// <summary>Gerätetabelle des Projekts (Projektkopie), z. B. <c>Tab_WP</c>.</summary>
            public string Geraetetabelle;

            /// <summary>Fremdschlüsselspalte in <c>Tab_Energieanlagen</c>, z. B. <c>ID_WP</c>.</summary>
            public string AnlagenFk;

            /// <summary>Anlagentypen des Gewerks (Normal- und Referenztyp).</summary>
            public int[] AnlagenTypen;

            /// <summary>Kindtabellen der Gerätezeile (Fremdschlüssel <see cref="KindFk"/>).</summary>
            public string[] Kindtabellen;

            /// <summary>Fremdschlüsselspalte der Kindtabellen auf die Gerätezeile.</summary>
            public string KindFk;
        }

        private const string TAB_ANLAGEN = "Tab_Energieanlagen";
        private const string TAB_PUFFER = "Tab_Pufferspeicher";
        private const string GEWERK_PUFFER = "Pufferspeicher";
        private const string SPALTE_ID = "ID";
        private const string SPALTE_ID_PROJEKT = "ID_Projekt";
        private const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>
        /// Die vier Spalten, über die eine Anlagenzeile auf einen Pufferspeicher zeigt.
        ///
        /// <para>
        /// ÖFFENTLICH, weil <c>GeraeteWaisen</c> dieselbe Liste braucht: Ob eine
        /// Speicherzeile noch verbaut ist, entscheidet sich nicht an <c>ID_PUFFER</c>
        /// allein, sondern an allen vieren - der Senkenspeicher einer Wärmepumpe steht in
        /// <c>WS_ID_Puffer</c> IHRER Anlagenzeile, nicht in einer Puffer-Zeile. Eine
        /// zweite Liste danebenzustellen hieße, die nächste Spalte an einer von zwei
        /// Stellen zu vergessen.
        /// </para>
        /// </summary>
        public static readonly string[] PUFFER_VERWEISE =
            { "ID_PUFFER", "WQ_ID_Puffer", "WS_ID_Puffer", "WS_ID_Puffer2" };

        /// <summary>
        /// Unterstützte Gewerke. Was hier nicht steht, ist auf der Oberfläche gesperrt
        /// (ehrlicher Hinweis statt halber Kette) — die Liste deckt alle Gewerke ab, die
        /// <see cref="ProjektDetails.GewerkTabellen"/> führt.
        /// </summary>
        public static readonly Dictionary<string, GewerkPlan> Plaene =
            new Dictionary<string, GewerkPlan>(StringComparer.OrdinalIgnoreCase)
        {
            { "Wärmepumpe", new GewerkPlan {
                Gewerk = "Wärmepumpe", Geraetetabelle = "Tab_WP", AnlagenFk = "ID_WP",
                AnlagenTypen = new[] { WizardItemClass.WP_TYP, WizardItemClass.REF_WP_TYP },
                Kindtabellen = new[] { "Tab_Kenndaten", "Tab_Kenndaten_Kuehlung" }, KindFk = "ID_WP" } },

            { "BHKW", new GewerkPlan {
                Gewerk = "BHKW", Geraetetabelle = "Tab_BHKW", AnlagenFk = "ID_BHKW",
                AnlagenTypen = new[] { WizardItemClass.BHKW_TYP },
                Kindtabellen = new string[0], KindFk = null } },

            { "Spitzenkessel", new GewerkPlan {
                Gewerk = "Spitzenkessel", Geraetetabelle = "Tab_Heizkessel", AnlagenFk = "ID_Kessel",
                AnlagenTypen = new[] { WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP },
                Kindtabellen = new string[0], KindFk = null } },

            { "Solarthermie", new GewerkPlan {
                Gewerk = "Solarthermie", Geraetetabelle = "Tab_Solarkollektoren", AnlagenFk = "ID_Solar",
                AnlagenTypen = new[] { WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP },
                Kindtabellen = new string[0], KindFk = null } },

            { "Photovoltaik", new GewerkPlan {
                Gewerk = "Photovoltaik", Geraetetabelle = "Tab_PV", AnlagenFk = "ID_PV",
                AnlagenTypen = new[] { WizardItemClass.PV_TYP, WizardItemClass.REF_PV_TYP },
                Kindtabellen = new string[0], KindFk = null } },

            { "Pufferspeicher", new GewerkPlan {
                Gewerk = "Pufferspeicher", Geraetetabelle = TAB_PUFFER, AnlagenFk = "ID_PUFFER",
                AnlagenTypen = new[] { WizardItemClass.PUFFER_TYP },
                Kindtabellen = new string[0], KindFk = null } },

            { "Stromspeicher", new GewerkPlan {
                Gewerk = "Stromspeicher", Geraetetabelle = "Tab_Stromspeicher", AnlagenFk = "ID_SP",
                AnlagenTypen = new[] { WizardItemClass.SP_TYP, WizardItemClass.REF_SP_TYP },
                Kindtabellen = new string[0], KindFk = null } }
        };

        /// <summary>Ist die Komponenten-Übernahme für dieses Gewerk umgesetzt?</summary>
        public static bool Unterstuetzt(string gewerk)
        {
            return !string.IsNullOrEmpty(gewerk) && Plaene.ContainsKey(gewerk);
        }

        // ------------------------------------------------------------------ Vorschau

        /// <summary>Klartext-Vorschau für den Bestätigungsdialog. Schreibt nichts.</summary>
        public class Vorschau
        {
            public bool Moeglich;
            public string Grund = "";

            /// <summary>Quelle und Ziel führen bereits denselben Bestand.</summary>
            public bool NichtsZuTun;

            public List<string> Anlegen = new List<string>();
            public List<string> Entfernen = new List<string>();
            public List<string> Gleichziehen = new List<string>();

            /// <summary>Mehrzeilige Zusammenfassung für den Dialog.</summary>
            public string Klartext = "";
        }

        /// <summary>
        /// Ermittelt, was die Übernahme des Gewerks von <paramref name="idQuelle"/> nach
        /// <paramref name="idZiel"/> anlegen, ersetzen und entfernen würde.
        /// </summary>
        public Vorschau Planen(int idQuelle, int idZiel, string gewerk)
        {
            var v = new Vorschau();

            GewerkPlan plan;
            if (!Plaene.TryGetValue(gewerk ?? "", out plan))
            { v.Grund = string.Format(MyResource.Resource.BK_MSG_KOMP_GEWERK_UNBEKANNT, gewerk); return v; }

            if (idQuelle <= 0 || idZiel <= 0 || idQuelle == idZiel)
            { v.Grund = MyResource.Resource.BK_MSG_UEB_KEINE_QUELLE; return v; }

            DataTable q = VerbauteGeraete(plan, idQuelle);
            DataTable z = VerbauteGeraete(plan, idZiel);
            if (q == null || z == null)
            { v.Grund = string.Format(MyResource.Resource.BK_MSG_KOMP_GEWERK_UNBEKANNT, gewerk); return v; }

            List<string> qNamen = Namen(q);
            List<string> zNamen = Namen(z);

            var offen = new List<string>(zNamen);
            foreach (string n in qNamen)
            {
                if (offen.Remove(n)) v.Gleichziehen.Add(n);
                else v.Anlegen.Add(n);
            }
            v.Entfernen.AddRange(offen);

            v.NichtsZuTun = BestandGleich(plan, q, z, idQuelle, idZiel);
            v.Moeglich = !v.NichtsZuTun;
            v.Klartext = Klartext(plan, v, q.Rows.Count, z.Rows.Count);
            if (v.NichtsZuTun) v.Grund = MyResource.Resource.BK_MSG_KOMP_NICHTS_ZU_TUN;
            return v;
        }

        private string Klartext(GewerkPlan plan, Vorschau v, int nQuelle, int nZiel)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format(MyResource.Resource.BK_KOMP_ZUS_KOPF, plan.Gewerk, nZiel, nQuelle));
            if (v.NichtsZuTun) { sb.AppendLine(MyResource.Resource.BK_MSG_KOMP_NICHTS_ZU_TUN); return sb.ToString(); }

            foreach (string n in v.Anlegen)
                sb.AppendLine(string.Format(MyResource.Resource.BK_KOMP_ZUS_ANLEGEN, n));
            foreach (string n in v.Gleichziehen)
                sb.AppendLine(string.Format(MyResource.Resource.BK_KOMP_ZUS_ERSETZEN, n));
            foreach (string n in v.Entfernen)
                sb.AppendLine(string.Format(MyResource.Resource.BK_KOMP_ZUS_ENTFERNEN, n));

            sb.AppendLine();
            sb.AppendLine(MyResource.Resource.BK_KOMP_ZUS_KETTE);
            sb.AppendLine("  · " + plan.Geraetetabelle);
            foreach (string k in plan.Kindtabellen) sb.AppendLine("  · " + k);
            sb.AppendLine("  · " + TAB_ANLAGEN);
            if (IstStromspeicher(plan)) sb.AppendLine("  · " + StromspeicherVarianteCtrl.TABLE);
            return sb.ToString();
        }

        // ------------------------------------------------------------------ Ausführen

        /// <summary>
        /// Ersetzt den Komponentenbestand des Gewerks im Zielprojekt durch den der Quelle.
        /// </summary>
        /// <param name="hinweise">
        /// Nicht auflösbare Pufferverweise und ähnliche Nebenbefunde — leer, wenn alles glatt lief.
        /// </param>
        public bool Uebernehmen(int idQuelle, int idZiel, string gewerk, out string fehler, out string hinweise)
        {
            fehler = null; hinweise = "";

            GewerkPlan plan;
            if (!Plaene.TryGetValue(gewerk ?? "", out plan))
            { fehler = string.Format(MyResource.Resource.BK_MSG_KOMP_GEWERK_UNBEKANNT, gewerk); return false; }
            if (idQuelle <= 0 || idZiel <= 0 || idQuelle == idZiel)
            { fehler = MyResource.Resource.BK_MSG_UEB_KEINE_QUELLE; return false; }

            // --- 1) Quelle vollständig lesen (vor der Transaktion, rein lesend) --------
            DataTable quellGeraete = VerbauteGeraete(plan, idQuelle);
            List<int> quellAnlagenIds = AnlagenIds(idQuelle, plan.AnlagenTypen);
            var quellAnlagen = new List<WErzeugerCtrl>();
            foreach (int id in quellAnlagenIds)
            {
                WErzeugerCtrl a = AnlageLesen(id);
                if (a != null) quellAnlagen.Add(a);
            }

            // A1-O2: Die SENKENLISTE (Z_AnlageSenke) jeder Quell-Anlage. Sie hängt an der
            // Anlage, nicht am Gerät, und wird deshalb weder von der Gerätekopie noch von
            // der Anlagenkopie miterfasst — bis Paket L startete jede übernommene
            // Komponente mit der Rang-1-Vorbelegung (Heizkreis/Beides) statt mit der
            // Senkenkette der Quelle. Gelesen VOR der Transaktion, geschrieben NACH dem
            // Commit (die Anlagen-ID ist ein AutoWert, Muster VariantenNachziehen).
            var quellSenken = new Dictionary<int, List<Z_AnlageSenkeModel>>();
            {
                var senkeCtrl = new Z_AnlageSenkeCtrl();
                foreach (int id in quellAnlagenIds)
                    quellSenken[id] = senkeCtrl.LesenJeAnlage(id);
            }

            // Betriebsführung der Quell-Speichervarianten (nur Stromspeicher).
            var quellVarianten = new Dictionary<int, StromspeicherVarianteModel>();
            int aktiveQuellAnlage = 0;
            if (IstStromspeicher(plan))
            {
                var spCtrl = new StromspeicherVarianteCtrl();
                foreach (int id in quellAnlagenIds)
                {
                    StromspeicherVarianteModel m = spCtrl.ReadByEnergieanlage(id);
                    if (m == null) continue;
                    quellVarianten[id] = m;
                    if (m.Aktiv && aktiveQuellAnlage == 0) aktiveQuellAnlage = id;
                }
            }

            // --- 2) Zielzustand lesen -------------------------------------------------
            // BEWUSST über Geraete() und nicht über VerbauteGeraete(): Diese Liste dient
            // dem Aufräumen der Kindtabellen vor dem DELETE … WHERE ID_Projekt, und das
            // räumt die Gerätetabelle ohnehin ganz — auch den Altbestand ohne
            // Anlagenzeile. Würde hier gefiltert, blieben dessen Kindzeilen zurück.
            DataTable zielGeraete = Geraete(plan.Geraetetabelle, idZiel);
            var zielGeraeteIds = new List<int>();
            foreach (DataRow r in zielGeraete.Rows) zielGeraeteIds.Add(Ganz(r, SPALTE_ID));

            // Pufferverweise ALLER Anlagenzeilen des Ziels als Bezeichner sichern — sie
            // müssen den Austausch überleben (siehe Klassenkopf).
            List<Pufferbezug> zielPufferbezuege = PufferbezuegeSichern(idZiel);

            // L-B1: dazu die Puffer-Verweise der SENKENLISTE des Ziels — dieselbe
            // erzwungene Beziehung, derselbe Grund.
            List<Senkenbezug> zielSenkenbezuege = SenkenbezuegeSichern(idZiel);

            var warnungen = new List<string>();
            // ARBEITSPAKET S4e: Der Vorgang (Verbindung + Transaktion) wird bewusst
            // INNERHALB des try geoeffnet - genau dort stand bisher der Aufruf, der das
            // Verbindungstupel holte. Ein Fehler beim Verbindungsaufbau soll in denselben
            // catch laufen; deshalb kein using, sondern Dispose im finally.
            DbVorgang v = null;
            var neueGeraeteIds = new List<int>();     // Reihenfolge = quellGeraete
            var neuePufferNachName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // A1-O2: Die Puffer-Abbildung „Quelle -> Ziel" entsteht in der Transaktion,
            // gebraucht wird sie auch danach (Senkenlisten). Deshalb hier deklariert.
            Dictionary<int, int> pufferAbbildungNachher = new Dictionary<int, int>();

            try
            {
                v = DataRepository.Vorgang();

                // --- 3) Verweise auf die zu löschenden Zielspeicher lösen --------------
                // Nur nötig, wenn genau dieses Gewerk der Pufferspeicher ist: sonst
                // bleibt der Speicherbestand des Ziels unangetastet.
                if (IstPuffer(plan))
                {
                    PufferverweiseLoesen(v, idZiel);
                    SenkenverweiseLoesen(v, idZiel);        // L-B1
                }

                // --- 4) Alten Bestand entfernen (Anlagen vor Geräten vor Kindern) ------
                Ausfuehren(v,
                    "DELETE FROM [" + TAB_ANLAGEN + "] WHERE ID_Projekt = ? AND " + TypFilter(plan),
                    new DbParam("@p", idZiel));

                foreach (int alt in zielGeraeteIds)
                {
                    foreach (string kind in plan.Kindtabellen)
                        VersucheAusfuehren(v,
                            "DELETE FROM [" + kind + "] WHERE [" + plan.KindFk + "] = ?",
                            new DbParam("@fk", alt));

                    if (IstPuffer(plan))
                    {
                        VersucheAusfuehren(v,
                            "DELETE FROM [Z_ProjektPufferSp] WHERE ID_Pufferspeicher = ?",
                            new DbParam("@fk", alt));
                        VersucheAusfuehren(v,
                            "DELETE FROM [Z_AnlagePufferVerbund] WHERE ID_Puffer = ?",
                            new DbParam("@fk", alt));
                    }
                }

                Ausfuehren(v,
                    "DELETE FROM [" + plan.Geraetetabelle + "] WHERE ID_Projekt = ?",
                    new DbParam("@p", idZiel));

                // --- 5) Gerätezeilen der Quelle als Projektkopie anlegen ---------------
                int naechsteId = MaxId(v, plan.Geraetetabelle) + 1;
                foreach (DataRow q in quellGeraete.Rows)
                {
                    ZeileKopieren(v, plan.Geraetetabelle, q, naechsteId, idZiel, null, null);
                    neueGeraeteIds.Add(naechsteId);

                    if (IstPuffer(plan))
                        neuePufferNachName[Text(q, SPALTE_BEZEICHNER)] = naechsteId;

                    // Kindtabellen der Gerätezeile mit dem neuen Fremdschlüssel.
                    int quellGeraetId = Ganz(q, SPALTE_ID);
                    foreach (string kind in plan.Kindtabellen)
                        KindtabelleKopieren(v, kind, plan.KindFk,
                                            quellGeraetId, naechsteId, idZiel, warnungen);

                    naechsteId++;
                }

                // --- 6) Gelöste Pufferverweise wiederherstellen ------------------------
                if (IstPuffer(plan))
                {
                    PufferverweiseWiederherstellen(v, zielPufferbezuege, neuePufferNachName, warnungen);
                    SenkenverweiseWiederherstellen(v, zielSenkenbezuege, neuePufferNachName, warnungen);   // L-B1
                }

                // --- 7) Anlagenzeilen anlegen — dieselbe Anweisung wie überall ---------
                Dictionary<int, int> pufferAbbildung = PufferAbbildung(plan, idQuelle, idZiel,
                                                                       quellGeraete, neueGeraeteIds);
                pufferAbbildungNachher = pufferAbbildung;
                Dictionary<int, bool> pufferCache = PufferCache(idZiel, pufferAbbildung, plan, neueGeraeteIds);

                for (int i = 0; i < quellAnlagen.Count; i++)
                {
                    WErzeugerCtrl a = quellAnlagen[i];

                    // REIHENFOLGE IST HIER WESENTLICH. Beim Gewerk Pufferspeicher ist der
                    // Geraete-Fremdschluessel DIESELBE Spalte wie einer der vier
                    // Pufferverweise (ID_PUFFER). Wer ihn zuerst umsetzt, laesst die
                    // anschliessende Verweis-Abbildung auf einer bereits gesetzten
                    // ZIEL-ID nachschlagen - die steht nicht in der Quell-Abbildung, und
                    // der Verweis fiele als "nicht aufloesbar" weg. Deshalb: Ziel-ID aus
                    // dem UNVERAENDERTEN Modell bestimmen, dann die Verweise abbilden,
                    // und den Geraete-Fremdschluessel zuletzt setzen.
                    int fkZiel = GeraetefkZiel(plan, a, quellGeraete, neueGeraeteIds);
                    PufferverweiseUmschreiben(a, pufferAbbildung, warnungen);
                    GeraetefkSetzen(plan, a, fkZiel);

                    Ausfuehren(v, AnlagenSql.SQL_ANLAGE_INSERT,
                               AnlagenSql.AnlagenParameter(idZiel, a, pufferCache));
                }

                v.Commit();
            }
            catch (Exception ex)
            {
                if (v != null) { try { v.Rollback(); } catch { } }
                fehler = ex.Message;
                return false;
            }
            finally
            {
                if (v != null) { try { v.Dispose(); } catch { } }
            }

            // --- 8) Senkenlisten der Quelle nachziehen (nach dem Commit) --------------
            // A1-O2. NACH dem Commit aus demselben Grund wie die Betriebsführung unten:
            // Die Anlagen-ID ist ein AutoWert und steht erst danach fest, und
            // Z_AnlageSenkeCtrl.SchreibenJeAnlage führt seine eigene Transaktion.
            SenkenNachziehen(idZiel, plan, quellAnlagen, quellAnlagenIds, quellSenken,
                             pufferAbbildungNachher, warnungen);

            // --- 9) Betriebsführung des Stromspeichers (nach dem Commit) --------------
            if (IstStromspeicher(plan))
                VariantenNachziehen(idZiel, quellAnlagen, quellAnlagenIds, quellVarianten,
                                    aktiveQuellAnlage, warnungen);

            MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idZiel);

            // --- 10) Kostenpositionen: absichtlich NICHT angefasst, aber gemeldet ------
            // Der Bestandsaustausch tauscht die Gerätezeile; die Kostenposition des
            // Zielprojekts bleibt auf ihrem alten Betrag stehen. Das ist richtig so
            // (Nutzerentscheidung 4 vom 18.08.2026: niemals automatisch überschreiben) —
            // aber es darf nicht STILL geschehen. Deshalb wird hier gezählt, welche
            // Komponenten dadurch vom Technik-Planwert abweichen; angeglichen wird
            // ausschließlich in der Kostenverwaltung über „Planwert übernehmen…".
            KostenabweichungMelden(idZiel, plan.Gewerk, warnungen);

            hinweise = string.Join("\r\n", warnungen.ToArray());
            return true;
        }

        /// <summary>
        /// Vermerkt, ob die (unangetastete) Kostenposition des Gewerks nach dem Austausch
        /// noch zum Technik-Planwert passt.
        /// </summary>
        private static void KostenabweichungMelden(int idZiel, string gewerk, List<string> warnungen)
        {
            try
            {
                // Das Gewerk „Spitzenkessel" heißt als Kostenkomponente „Heizkessel";
                // alle übrigen tragen denselben Namen (Tab_KostenKomponente).
                string komponente = string.Equals(gewerk, "Spitzenkessel", StringComparison.OrdinalIgnoreCase)
                    ? DbWerte.ERZEUGER_HEIZKESSEL : gewerk;

                object o = DataRepository.ExecuteScalar(
                    "SELECT MIN(ID) FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new DbParam("@k", komponente ?? ""));
                if (o == null || o == DBNull.Value) return;

                KostenPositionCtrl.Abweichung ab = KostenPositionCtrl.Pruefe(
                    idZiel, komponente, DbWerte.KOSTEN_KATEGORIE_INVESTITION, Convert.ToInt32(o));

                if (ab != null && ab.Abweichend)
                    warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_KOSTEN, komponente));
            }
            catch { }
        }

        // ------------------------------------------------------------------ Bausteine

        private static bool IstPuffer(GewerkPlan p)
        { return string.Equals(p.Geraetetabelle, TAB_PUFFER, StringComparison.OrdinalIgnoreCase); }

        private static bool IstStromspeicher(GewerkPlan p)
        { return string.Equals(p.Geraetetabelle, "Tab_Stromspeicher", StringComparison.OrdinalIgnoreCase); }

        /// <summary>
        /// <c>ID_Type IN (…)</c> fest im SQL statt als Parameter — dieselbe Begründung wie
        /// bei <c>WizardCtrl.SP_TYPEN</c>: OleDb bindet nach Position, und die Werte sind
        /// Konstanten des Programms, keine Anwendereingabe.
        /// </summary>
        private static string TypFilter(GewerkPlan plan)
        {
            return TypFilter(plan, null);
        }

        /// <summary>
        /// Wie <see cref="TypFilter(GewerkPlan)"/>, aber mit Tabellenkürzel — nötig,
        /// sobald die Abfrage zwei Tabellen führt.
        /// </summary>
        private static string TypFilter(GewerkPlan plan, string alias)
        {
            var teile = new List<string>();
            foreach (int t in plan.AnlagenTypen) teile.Add(t.ToString(CultureInfo.InvariantCulture));
            return (string.IsNullOrEmpty(alias) ? "" : alias + ".") +
                   "ID_Type IN (" + string.Join(", ", teile.ToArray()) + ")";
        }

        /// <summary>
        /// Der ROHE Zeilenbestand der Gerätetabelle zum Projekt — einschließlich der
        /// Zeilen, auf die keine Anlagenzeile mehr zeigt (siehe
        /// <see cref="VerbauteGeraete"/>). Nur zum Aufräumen brauchbar, NICHT als
        /// Komponentenbestand des Projekts.
        /// </summary>
        private static DataTable Geraete(string tabelle, int idProjekt)
        {
            try
            {
                return DataRepository.GetDataTable(
                    "SELECT * FROM [" + tabelle + "] WHERE ID_Projekt = ? ORDER BY ID",
                    new DbParam("@p", idProjekt));
            }
            catch { return null; }
        }

        /// <summary>
        /// Die Gerätezeilen, die im Projekt VERBAUT sind: je Gerätezeile eine, auf die
        /// mindestens eine Anlagenzeile des Gewerks zeigt.
        ///
        /// <para>
        /// WARUM NICHT <see cref="Geraete"/> (also <c>WHERE ID_Projekt = ?</c> auf der
        /// Gerätetabelle): Die Gerätetabellen führen PROJEKTKOPIEN eines Katalogsatzes
        /// (<c>KatalogRegistry</c>, „Kopiersemantik“). Jeder Speichervorgang legt über
        /// <c>CopyFromStamm</c> eine NEUE Kopie an, während <c>WErzeugerCtrl.Delete</c>
        /// nur die Anlagenzeilen räumt — in gewachsenen Projekten steht dort deshalb
        /// Altbestand, auf den nichts mehr zeigt, und eine Projektkopie erbt ihn mit.
        /// Zum Projekt gehört ausschließlich, was <c>Tab_Energieanlagen</c> führt; das
        /// ist auch die Liste „ausgewählt im Projekt“ der Verwaltungsdialoge und die
        /// Grundlage der Simulation.
        /// </para>
        ///
        /// <para>
        /// JE GERÄTEZEILE EINMAL — das ist der Satz Zeilen, den der Kopierweg anlegt.
        /// Zeigen zwei Anlagenzeilen auf dasselbe Gerät, entsteht auch im Ziel nur eine
        /// Kopie, auf die dann beide Anlagenzeilen zeigen. Für die ANZEIGE des Bestands
        /// zählt dagegen die Anlagenzeile (<see cref="GeraeteJeAnlagenzeile"/>).
        /// </para>
        /// </summary>
        public static DataTable VerbauteGeraete(GewerkPlan plan, int idProjekt)
        {
            if (plan == null) return null;
            try
            {
                return DataRepository.GetDataTable(
                    "SELECT * FROM [" + plan.Geraetetabelle + "] WHERE [" + SPALTE_ID + "] IN (" +
                    "SELECT [" + plan.AnlagenFk + "] FROM [" + TAB_ANLAGEN + "] " +
                    "WHERE [" + SPALTE_ID_PROJEKT + "] = ? AND " + TypFilter(plan) + ") " +
                    "ORDER BY [" + SPALTE_ID + "]",
                    new DbParam("@p", idProjekt));
            }
            catch { return null; }
        }

        /// <summary>
        /// Dieselbe Auswahl wie <see cref="VerbauteGeraete"/>, aber JE ANLAGENZEILE eine
        /// Gerätezeile und in deren Reihenfolge — der Bestand, wie ihn die
        /// Verwaltungsdialoge unter „ausgewählt im Projekt“ auflisten. Zeigen zwei
        /// Anlagenzeilen auf dasselbe Gerät, steht es zweimal in der Liste — dort stehen
        /// dann auch zwei Einträge.
        /// </summary>
        public static DataTable GeraeteJeAnlagenzeile(GewerkPlan plan, int idProjekt)
        {
            if (plan == null) return null;
            try
            {
                return DataRepository.GetDataTable(
                    "SELECT g.* FROM [" + plan.Geraetetabelle + "] g " +
                    "INNER JOIN [" + TAB_ANLAGEN + "] a ON g.[" + SPALTE_ID + "] = a.[" + plan.AnlagenFk + "] " +
                    "WHERE a.[" + SPALTE_ID_PROJEKT + "] = ? AND " + TypFilter(plan, "a") + " " +
                    "ORDER BY a.[" + SPALTE_ID + "]",
                    new DbParam("@p", idProjekt));
            }
            catch { return null; }
        }

        private static List<int> AnlagenIds(int idProjekt, int[] typen)
        {
            var liste = new List<int>();
            var plan = new GewerkPlan { AnlagenTypen = typen };
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM [" + TAB_ANLAGEN + "] WHERE ID_Projekt = ? AND " + TypFilter(plan) + " ORDER BY ID",
                new DbParam("@p", idProjekt));
            if (dt != null) foreach (DataRow r in dt.Rows) liste.Add(Convert.ToInt32(r[0]));
            return liste;
        }

        /// <summary>
        /// Eine Anlagenzeile als Modell. Bewusst über <c>WErzeugerCtrl.ReadSingle</c>:
        /// dort liegt die EINE Leseabbildung, die zu <see cref="AnlagenSql.AnlagenParameter"/>
        /// symmetrisch ist. Die ID stammt aus der Datenbank, nicht aus einer Eingabe.
        /// </summary>
        private static WErzeugerCtrl AnlageLesen(int idAnlage)
        {
            var c = new WErzeugerCtrl();
            c.ReadSingle("SELECT * FROM " + TAB_ANLAGEN + " WHERE ID = " +
                         idAnlage.ToString(CultureInfo.InvariantCulture));
            return c.ID == idAnlage ? c : null;
        }

        private static List<string> Namen(DataTable dt)
        {
            var liste = new List<string>();
            if (dt != null) foreach (DataRow r in dt.Rows) liste.Add(Text(r, SPALTE_BEZEICHNER));
            return liste;
        }

        /// <summary>
        /// Bestandsgleichheit: gleiche Gerätezeilen (alle Spalten außer <c>ID</c> und
        /// <c>ID_Projekt</c>) UND gleiche Anlagenzeilen nach (Typ, Bezeichner).
        /// Bewusst OHNE die Quellen-/Senken-Konfiguration der Anlagenzeilen: deren
        /// Pufferverweise werden beim Kopieren projektbezogen aufgelöst und können sich
        /// deshalb legitim unterscheiden. Ergebnis ist eine wiederholbare Übernahme, die
        /// beim zweiten Mal ehrlich „nichts zu tun" meldet.
        /// </summary>
        private bool BestandGleich(GewerkPlan plan, DataTable q, DataTable z, int idQuelle, int idZiel)
        {
            if (q.Rows.Count != z.Rows.Count) return false;

            for (int i = 0; i < q.Rows.Count; i++)
            {
                DataRow a = q.Rows[i], b = z.Rows[i];
                foreach (DataColumn c in q.Columns)
                {
                    if (string.Equals(c.ColumnName, SPALTE_ID, StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(c.ColumnName, SPALTE_ID_PROJEKT, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!z.Columns.Contains(c.ColumnName)) return false;
                    if (!Gleich(a[c.ColumnName], b[c.ColumnName])) return false;
                }
            }

            List<string> qa = AnlagenKennungen(idQuelle, plan);
            List<string> za = AnlagenKennungen(idZiel, plan);
            if (qa.Count != za.Count) return false;
            qa.Sort(StringComparer.OrdinalIgnoreCase);
            za.Sort(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < qa.Count; i++)
                if (!string.Equals(qa[i], za[i], StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }

        private static List<string> AnlagenKennungen(int idProjekt, GewerkPlan plan)
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_Type, Bezeichner FROM [" + TAB_ANLAGEN + "] WHERE ID_Projekt = ? AND " +
                TypFilter(plan) + " ORDER BY ID",
                new DbParam("@p", idProjekt));
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    liste.Add(Ganz(r, "ID_Type").ToString(CultureInfo.InvariantCulture) + "|" +
                              Text(r, SPALTE_BEZEICHNER));
            return liste;
        }

        private static bool Gleich(object a, object b)
        {
            bool na = a == null || a == DBNull.Value, nb = b == null || b == DBNull.Value;
            if (na || nb) return na && nb;
            if (a is double || a is float || a is decimal || b is double || b is float || b is decimal)
                return Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < 1e-9;
            return string.Equals(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }

        // --------------------------------------------------------- Kopieren von Zeilen

        /// <summary>
        /// Kopiert eine Zeile spaltenweise in dieselbe Tabelle — mit NEUER <c>ID</c> und
        /// neuem <c>ID_Projekt</c>. Der Spaltensatz kommt aus dem Schema der geladenen
        /// Zeile, es gibt hier also keine zweite, pflegebedürftige Spaltenliste je Gewerk.
        /// Quell-IDs werden nie übernommen.
        /// </summary>
        private static void ZeileKopieren(DbVorgang v, string tabelle,
                                          DataRow quelle, int neueId, int idZiel,
                                          string fkSpalte, int? fkWert)
        {
            var spalten = new List<string>();
            var platzhalter = new List<string>();
            var ps = new List<DbParam>();

            foreach (DataColumn c in quelle.Table.Columns)
            {
                string name = c.ColumnName;
                spalten.Add("[" + name + "]");
                platzhalter.Add("?");

                if (string.Equals(name, SPALTE_ID, StringComparison.OrdinalIgnoreCase))
                    ps.Add(new DbParam("@p" + ps.Count, DbParamTyp.Integer) { Wert = neueId });
                else if (string.Equals(name, SPALTE_ID_PROJEKT, StringComparison.OrdinalIgnoreCase))
                    ps.Add(new DbParam("@p" + ps.Count, DbParamTyp.Integer) { Wert = idZiel });
                else if (fkSpalte != null && string.Equals(name, fkSpalte, StringComparison.OrdinalIgnoreCase))
                    ps.Add(new DbParam("@p" + ps.Count, DbParamTyp.Integer)
                    { Wert = fkWert.HasValue ? (object)fkWert.Value : DBNull.Value });
                else
                    ps.Add(Wert("@p" + ps.Count, c.DataType, quelle[name]));
            }

            Ausfuehren(v,
                "INSERT INTO [" + tabelle + "] (" + string.Join(", ", spalten.ToArray()) + ") VALUES (" +
                string.Join(", ", platzhalter.ToArray()) + ")",
                ps.ToArray());
        }

        // Alle Zeilen einer Kindtabelle eines Geraets kopieren (z. B. 165 Kennlinienpunkte
        // einer Waermepumpe). Fehlt die Tabelle auf einer aelteren Datenbank, wird das
        // vermerkt statt den ganzen Vorgang abzubrechen.
        private static void KindtabelleKopieren(DbVorgang v,
                                                string tabelle, string fkSpalte,
                                                int quellGeraetId, int neuesGeraet, int idZiel,
                                                List<string> warnungen)
        {
            DataTable dt;
            try
            {
                dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + tabelle + "] WHERE [" + fkSpalte + "] = ? ORDER BY ID",
                    new DbParam("@fk", quellGeraetId));
            }
            catch { warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_KINDTABELLE, tabelle)); return; }

            if (dt == null || dt.Rows.Count == 0) return;

            int naechste = MaxId(v, tabelle) + 1;
            foreach (DataRow r in dt.Rows)
            {
                ZeileKopieren(v, tabelle, r, naechste, idZiel, fkSpalte, neuesGeraet);
                naechste++;
            }
        }

        // ------------------------------------------------------------- Pufferverweise

        /// <summary>Ein gesicherter Pufferverweis einer Anlagenzeile (als Bezeichner).</summary>
        private class Pufferbezug
        {
            public int IdAnlage;
            public string Spalte;
            public string Bezeichner;
        }

        // Alle Verweise der Anlagenzeilen eines Projekts auf dessen eigene Pufferspeicher,
        // aufgeloest als Bezeichner - der ueberlebt den Austausch, die ID nicht.
        private static List<Pufferbezug> PufferbezuegeSichern(int idProjekt)
        {
            var liste = new List<Pufferbezug>();
            try
            {
                DataTable puffer = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner FROM [" + TAB_PUFFER + "] WHERE ID_Projekt = ?",
                    new DbParam("@p", idProjekt));
                var nachId = new Dictionary<int, string>();
                if (puffer != null)
                    foreach (DataRow r in puffer.Rows) nachId[Ganz(r, SPALTE_ID)] = Text(r, SPALTE_BEZEICHNER);
                if (nachId.Count == 0) return liste;

                DataTable anlagen = DataRepository.GetDataTable(
                    "SELECT * FROM [" + TAB_ANLAGEN + "] WHERE ID_Projekt = ?",
                    new DbParam("@p", idProjekt));
                if (anlagen == null) return liste;

                foreach (DataRow a in anlagen.Rows)
                    foreach (string sp in PUFFER_VERWEISE)
                    {
                        if (!anlagen.Columns.Contains(sp) || a[sp] == DBNull.Value) continue;
                        int id = Convert.ToInt32(a[sp]);
                        if (id <= 0 || !nachId.ContainsKey(id)) continue;
                        liste.Add(new Pufferbezug
                        { IdAnlage = Ganz(a, SPALTE_ID), Spalte = sp, Bezeichner = nachId[id] });
                    }
            }
            catch { }
            return liste;
        }

        // Verweise auf die Projektspeicher leeren, damit die Speicherzeilen ueberhaupt
        // geloescht werden koennen (erzwungene Beziehung, kein CASCADE).
        private static void PufferverweiseLoesen(DbVorgang v, int idProjekt)
        {
            foreach (string sp in PUFFER_VERWEISE)
                VersucheAusfuehren(v,
                    "UPDATE [" + TAB_ANLAGEN + "] SET [" + sp + "] = NULL " +
                    "WHERE ID_Projekt = ? AND [" + sp + "] IN " +
                    "(SELECT ID FROM [" + TAB_PUFFER + "] WHERE ID_Projekt = ?)",
                    new DbParam("@p1", idProjekt), new DbParam("@p2", idProjekt));
        }

        private static void PufferverweiseWiederherstellen(DbVorgang v,
                                                           List<Pufferbezug> bezuege,
                                                           Dictionary<string, int> neueNachName,
                                                           List<string> warnungen)
        {
            int verloren = 0;
            foreach (Pufferbezug b in bezuege)
            {
                int neu;
                if (!neueNachName.TryGetValue(b.Bezeichner ?? "", out neu)) { verloren++; continue; }

                VersucheAusfuehren(v,
                    "UPDATE [" + TAB_ANLAGEN + "] SET [" + b.Spalte + "] = ? WHERE ID = ?",
                    new DbParam("@neu", neu), new DbParam("@id", b.IdAnlage));
            }
            if (verloren > 0)
                warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_PUFFERVERWEIS, verloren));
        }

        /// <summary>Ein gesicherter Puffer-Verweis einer Senkenzeile (als Bezeichner).</summary>
        private class Senkenbezug
        {
            public int IdSenke;
            public string Bezeichner;
        }

        // L-B1: Auch die SENKENLISTE zeigt auf Tab_Pufferspeicher - Z_AnlageSenke.ID_Puffer
        // ist seit Schritt 50 eine RESTRIKTIVE Beziehung (FK_AnlageSenke_Puffer). Ohne
        // Sichern/Loesen/Wiederherstellen scheiterte das Loeschen der Zielspeicher an jeder
        // Senkenzeile, die einen von ihnen fuehrt - dieselbe Behandlung wie die vier
        // Anlagenspalten (PufferbezuegeSichern), aus demselben Grund. Der Bezeichner
        // ueberlebt den Austausch, die ID nicht. Die Senkenzeile selbst gehoert einer
        // Anlage eines ANDEREN Gewerks und bleibt stehen; hinge sie doch an einer
        // geloeschten Anlage des Tauschgewerks, naehme die Loeschweitergabe von
        // FK_AnlageSenke_Anlage sie mit, und das Wiederherstellen traefe still 0 Zeilen.
        private static List<Senkenbezug> SenkenbezuegeSichern(int idProjekt)
        {
            var liste = new List<Senkenbezug>();
            if (!Z_AnlageSenkeCtrl.SpalteVorhanden()) return liste;   // vor Schritt 50: nichts zu sichern
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT s.ID, p.Bezeichner FROM [" + Z_AnlageSenkeCtrl.TABLE + "] s " +
                    "INNER JOIN [" + TAB_PUFFER + "] p ON s.ID_Puffer = p.ID " +
                    "WHERE p.ID_Projekt = ?",
                    new DbParam("@p", idProjekt));
                if (dt == null) return liste;

                foreach (DataRow r in dt.Rows)
                    liste.Add(new Senkenbezug
                    { IdSenke = Ganz(r, SPALTE_ID), Bezeichner = Text(r, SPALTE_BEZEICHNER) });
            }
            catch { }
            return liste;
        }

        // Senken-Verweise auf die Projektspeicher leeren (Gegenstueck zu
        // PufferverweiseLoesen). VersucheAusfuehren, weil die Tabelle auf einer
        // Datenbank vor Schritt 50 fehlen kann.
        private static void SenkenverweiseLoesen(DbVorgang v, int idProjekt)
        {
            VersucheAusfuehren(v,
                "UPDATE [" + Z_AnlageSenkeCtrl.TABLE + "] SET ID_Puffer = NULL " +
                "WHERE ID_Puffer IN (SELECT ID FROM [" + TAB_PUFFER + "] WHERE ID_Projekt = ?)",
                new DbParam("@p", idProjekt));
        }

        private static void SenkenverweiseWiederherstellen(DbVorgang v,
                                                           List<Senkenbezug> bezuege,
                                                           Dictionary<string, int> neueNachName,
                                                           List<string> warnungen)
        {
            int verloren = 0;
            foreach (Senkenbezug b in bezuege)
            {
                int neu;
                if (!neueNachName.TryGetValue(b.Bezeichner ?? "", out neu)) { verloren++; continue; }

                VersucheAusfuehren(v,
                    "UPDATE [" + Z_AnlageSenkeCtrl.TABLE + "] SET ID_Puffer = ? WHERE ID = ?",
                    new DbParam("@neu", neu), new DbParam("@id", b.IdSenke));
            }
            if (verloren > 0)
                warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_PUFFERVERWEIS, verloren));
        }

        /// <summary>
        /// Abbildung „Pufferspeicher der QUELLE → Pufferspeicher des ZIELS".
        /// Beim Gewerk Pufferspeicher ist das die soeben angelegte Kopie (ID → neue ID),
        /// sonst die Auflösung über den Bezeichner im unveränderten Zielbestand.
        /// </summary>
        private static Dictionary<int, int> PufferAbbildung(GewerkPlan plan, int idQuelle, int idZiel,
                                                            DataTable quellGeraete, List<int> neueIds)
        {
            var map = new Dictionary<int, int>();

            if (IstPuffer(plan))
            {
                for (int i = 0; i < quellGeraete.Rows.Count && i < neueIds.Count; i++)
                    map[Ganz(quellGeraete.Rows[i], SPALTE_ID)] = neueIds[i];
                return map;
            }

            GewerkPlan pufferplan = Plaene[GEWERK_PUFFER];
            DataTable q = VerbauteGeraete(pufferplan, idQuelle);
            DataTable z = VerbauteGeraete(pufferplan, idZiel);
            if (q == null || z == null) return map;

            var zielNachName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in z.Rows) zielNachName[Text(r, SPALTE_BEZEICHNER)] = Ganz(r, SPALTE_ID);

            foreach (DataRow r in q.Rows)
            {
                int ziel;
                if (zielNachName.TryGetValue(Text(r, SPALTE_BEZEICHNER), out ziel))
                    map[Ganz(r, SPALTE_ID)] = ziel;
            }
            return map;
        }

        /// <summary>
        /// Vorbelegter Existenz-Zwischenspeicher für <see cref="AnlagenSql.AnlagenParameter"/>.
        /// NÖTIG, WEIL DIE PRÜFUNG AUSSERHALB DER TRANSAKTION LÄUFT: die eben angelegten
        /// Speicherzeilen sind für sie noch unsichtbar, sie würde die Verweise als
        /// verwaist leeren. Alle Verweise sind zu diesem Zeitpunkt bereits auf gültige
        /// Ziel-IDs abgebildet — der Zwischenspeicher bestätigt genau diese.
        /// </summary>
        private static Dictionary<int, bool> PufferCache(int idZiel, Dictionary<int, int> abbildung,
                                                         GewerkPlan plan, List<int> neueIds)
        {
            var cache = new Dictionary<int, bool>();
            foreach (KeyValuePair<int, int> kv in abbildung) cache[kv.Value] = true;
            if (IstPuffer(plan)) foreach (int id in neueIds) cache[id] = true;

            DataTable z = Geraete(TAB_PUFFER, idZiel);
            if (z != null && !IstPuffer(plan))
                foreach (DataRow r in z.Rows) cache[Ganz(r, SPALTE_ID)] = true;

            return cache;
        }

        // Verweise der zu kopierenden Anlagenzeile auf Zielspeicher umschreiben;
        // was sich nicht abbilden laesst, bleibt leer (nie eine Quell-ID uebernehmen).
        private static void PufferverweiseUmschreiben(WErzeugerCtrl a, Dictionary<int, int> abbildung,
                                                      List<string> warnungen)
        {
            int verloren = 0;

            a.ID_PUFFER = Abbilden(a.ID_PUFFER, abbildung, ref verloren) ?? 0;
            a.WQ_ID_Puffer = Abbilden(a.WQ_ID_Puffer, abbildung, ref verloren);
            a.WS_ID_Puffer = Abbilden(a.WS_ID_Puffer, abbildung, ref verloren);
            a.WS_ID_Puffer2 = Abbilden(a.WS_ID_Puffer2, abbildung, ref verloren);

            if (verloren > 0)
                warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_PUFFERVERWEIS, verloren));
        }

        private static int? Abbilden(int? quelle, Dictionary<int, int> abbildung, ref int verloren)
        {
            if (!quelle.HasValue || quelle.Value <= 0) return null;
            int ziel;
            if (abbildung.TryGetValue(quelle.Value, out ziel)) return ziel;
            verloren++;
            return null;
        }

        // ---------------------------------------------------------- Geraete-FK setzen

        // Die Geraete-ID, die die kopierte Anlagenzeile im Ziel fuehren muss.
        private static int GeraetefkZiel(GewerkPlan plan, WErzeugerCtrl a,
                                         DataTable quellGeraete, List<int> neueIds)
        {
            int alt = GeraetefkLesen(plan, a);
            for (int i = 0; i < quellGeraete.Rows.Count && i < neueIds.Count; i++)
                if (Ganz(quellGeraete.Rows[i], SPALTE_ID) == alt) return neueIds[i];
            return 0;
        }

        private static int GeraetefkLesen(GewerkPlan plan, WErzeugerCtrl a)
        {
            switch (plan.AnlagenFk)
            {
                case "ID_WP": return a.ID_WP;
                case "ID_BHKW": return a.ID_BHKW;
                case "ID_Kessel": return a.ID_Kessel;
                case "ID_Solar": return a.ID_Solar;
                case "ID_PV": return a.ID_PV;
                case "ID_SP": return a.ID_SP;
                case "ID_PUFFER": return a.ID_PUFFER;
                default: return 0;
            }
        }

        private static void GeraetefkSetzen(GewerkPlan plan, WErzeugerCtrl a, int neu)
        {
            switch (plan.AnlagenFk)
            {
                case "ID_WP": a.ID_WP = neu; break;
                case "ID_BHKW": a.ID_BHKW = neu; break;
                case "ID_Kessel": a.ID_Kessel = neu; break;
                case "ID_Solar": a.ID_Solar = neu; break;
                case "ID_PV": a.ID_PV = neu; break;
                case "ID_SP": a.ID_SP = neu; break;
                case "ID_PUFFER": a.ID_PUFFER = neu; break;
            }
        }

        // ------------------------------------------------------------ Senkenlisten

        /// <summary>
        /// A1-O2: Legt zu den neuen Anlagenzeilen die SENKENLISTE der Quelle an
        /// (<c>Z_AnlageSenke</c>, Muster <see cref="VariantenNachziehen"/> und
        /// <c>ProjektDuplizierenCtrl</c>).
        ///
        /// <para><b>Warum es das braucht.</b> Die Senkenliste hängt an der ANLAGE, nicht
        /// am Gerät. Der Bestandsaustausch löscht die Zielanlagen (die Löschweitergabe
        /// von <c>FK_AnlageSenke_Anlage</c> nimmt ihre Senken mit) und legt sie aus der
        /// Quelle neu an — ohne diesen Schritt startete jede übernommene Komponente mit
        /// der Rang-1-Vorbelegung Heizkreis/Beides statt mit der Senkenkette, die sie in
        /// der Quelle hatte.</para>
        ///
        /// <para><b>Die Anlagen-Abbildung ist POSITIONELL.</b> Schritt 4 hat ALLE
        /// Anlagenzeilen der Gewerktypen im Ziel gelöscht, Schritt 7 hat genau
        /// <paramref name="quellAnlagen"/> viele in dieser Reihenfolge neu angelegt; die
        /// ID ist ein aufsteigender AutoWert. Aufsteigend sortiert stehen die neuen IDs
        /// deshalb Zeile für Zeile in derselben Reihenfolge wie die Quelle. Stimmt die
        /// Zahl wider Erwarten nicht, fällt die Zuordnung auf
        /// <see cref="AnlageFinden"/> zurück — dieselbe (Typ, Bezeichner)-Auflösung wie
        /// bei den Speichervarianten, mit deren dokumentierter Grenze (S1-O1).</para>
        ///
        /// <para><b>Puffer-Verweise:</b> über dieselbe Abbildung wie
        /// <see cref="PufferverweiseUmschreiben"/>. Was sich nicht abbilden lässt, wird
        /// GELEERT — nie eine Quell-ID übernehmen. Das Ziel der Zeile bleibt dabei
        /// unangetastet, genau wie bei den <c>WS_</c>-Spalten: Die Engine normalisiert
        /// ein Puffer-Ziel ohne Puffer beim Lesen und meldet es
        /// (<c>WaermesenkeClass.AusZuordnungstabelle</c>).</para>
        ///
        /// <para>Fehlt die Tabelle (Datenbank vor Schritt 50), sind Lesen und Schreiben
        /// still wirkungslos — <c>Z_AnlageSenkeCtrl.SpalteVorhanden</c> fängt beides ab.</para>
        /// </summary>
        private static void SenkenNachziehen(int idZiel, GewerkPlan plan,
                                             List<WErzeugerCtrl> quellAnlagen,
                                             List<int> quellAnlagenIds,
                                             Dictionary<int, List<Z_AnlageSenkeModel>> quellSenken,
                                             Dictionary<int, int> pufferAbbildung,
                                             List<string> warnungen)
        {
            if (quellAnlagen == null || quellAnlagen.Count == 0) return;
            if (!Z_AnlageSenkeCtrl.SpalteVorhanden()) return;

            List<int> neueIds = NeueAnlagenIds(idZiel, plan);
            bool positionell = (neueIds.Count == quellAnlagen.Count);

            var ctrl = new Z_AnlageSenkeCtrl();
            int verloren = 0;

            for (int i = 0; i < quellAnlagen.Count; i++)
            {
                List<Z_AnlageSenkeModel> vorlage;
                if (!quellSenken.TryGetValue(quellAnlagenIds[i], out vorlage) ||
                    vorlage == null || vorlage.Count == 0) continue;

                int neueAnlage = positionell
                    ? neueIds[i]
                    : AnlageFinden(idZiel, quellAnlagen[i].ID_Type, quellAnlagen[i].Bezeichner);
                if (neueAnlage <= 0) continue;

                var zeilen = new List<Z_AnlageSenkeModel>();
                foreach (Z_AnlageSenkeModel q in vorlage)
                {
                    if (q == null) continue;
                    zeilen.Add(new Z_AnlageSenkeModel
                    {
                        // ID und ID_Anlage vergibt der Schreibweg; Rang ebenfalls
                        // (lueckenlos ab 1 in Listenreihenfolge - die Reihenfolge steht
                        // schon, LesenJeAnlage sortiert nach Rang).
                        Ziel = q.Ziel,
                        Bedarfsart = q.Bedarfsart,
                        ID_Puffer = Abbilden(q.ID_Puffer > 0 ? (int?)q.ID_Puffer : null,
                                             pufferAbbildung, ref verloren) ?? 0,
                        Ladeprio = q.Ladeprio,
                        Ladeprio_PV = q.Ladeprio_PV,
                        Ladegrenze = q.Ladegrenze,
                        Anschlusshoehe = q.Anschlusshoehe
                    });
                }

                ctrl.SchreibenJeAnlage(neueAnlage, zeilen);
            }

            if (verloren > 0)
                warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_PUFFERVERWEIS, verloren));
        }

        /// <summary>
        /// Die Anlagen-IDs des Gewerks im Zielprojekt, AUFSTEIGEND — nach dem Commit
        /// genau die soeben angelegten Zeilen in Anlegereihenfolge.
        /// </summary>
        private static List<int> NeueAnlagenIds(int idZiel, GewerkPlan plan)
        {
            var liste = new List<int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID FROM [" + TAB_ANLAGEN + "] " +
                    "WHERE ID_Projekt = ? AND " + TypFilter(plan) + " ORDER BY ID",
                    new DbParam("@p", idZiel));
                if (dt != null)
                    foreach (DataRow r in dt.Rows) liste.Add(Ganz(r, SPALTE_ID));
            }
            catch { }
            return liste;
        }

        // ------------------------------------------------------- Speichervarianten

        /// <summary>
        /// Legt zu den neuen Speicher-Anlagenzeilen die Betriebsführung an und stellt die
        /// Invariante „genau eine aktive Variante je Projekt" über
        /// <see cref="StromspeicherVarianteCtrl.SetzeAktiv"/> her — der einzigen
        /// Schreibstelle für <c>Aktiv</c>.
        /// Die Zuordnung läuft über (Typ, Bezeichner): die Anlagen-ID ist ein AutoWert und
        /// steht erst nach dem Commit fest; der Bezeichner IST der Variantenname.
        /// </summary>
        private static void VariantenNachziehen(int idZiel, List<WErzeugerCtrl> quellAnlagen,
                                                List<int> quellAnlagenIds,
                                                Dictionary<int, StromspeicherVarianteModel> quellVarianten,
                                                int aktiveQuellAnlage, List<string> warnungen)
        {
            var ctrl = new StromspeicherVarianteCtrl();
            int idAktiv = 0;

            for (int i = 0; i < quellAnlagen.Count; i++)
            {
                WErzeugerCtrl a = quellAnlagen[i];
                int neueAnlage = AnlageFinden(idZiel, a.ID_Type, a.Bezeichner);
                if (neueAnlage <= 0)
                { warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_VARIANTE, a.Bezeichner)); continue; }

                StromspeicherVarianteModel vorlage;
                StromspeicherVarianteModel neu = new StromspeicherVarianteModel();
                if (quellVarianten.TryGetValue(quellAnlagenIds[i], out vorlage) && vorlage != null)
                    neu = ParameterUebernehmen(vorlage);

                neu.ID_Energieanlage = neueAnlage;
                neu.Aktiv = false;                       // SetzeAktiv ist die einzige Schreibstelle
                if (ctrl.Insert(neu) <= 0)
                { warnungen.Add(string.Format(MyResource.Resource.BK_KOMP_HINW_VARIANTE, a.Bezeichner)); continue; }

                if (idAktiv <= 0) idAktiv = neu.ID;                                   // Rückfall: die erste
                if (aktiveQuellAnlage > 0 && quellAnlagenIds[i] == aktiveQuellAnlage) idAktiv = neu.ID;
            }

            if (idAktiv > 0) ctrl.SetzeAktiv(idZiel, idAktiv);
        }

        private static int AnlageFinden(int idProjekt, int idType, string bezeichner)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT ID FROM [" + TAB_ANLAGEN + "] " +
                "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ? ORDER BY ID DESC LIMIT 1",
                new DbParam("@p", idProjekt),
                new DbParam("@t", idType),
                new DbParam("@b", bezeichner ?? ""));
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 0;
        }

        // Betriebsparameter der Vorlage OHNE ID/Anlagenbezug und OHNE Aktiv-Kennzeichen -
        // dieselbe Aufteilung wie StromspeicherKontextMenuCtrl.ParameterUebernehmen.
        private static StromspeicherVarianteModel ParameterUebernehmen(StromspeicherVarianteModel v)
        {
            return new StromspeicherVarianteModel
            {
                Betriebsart = v.Betriebsart,
                Berechnungsart = v.Berechnungsart,
                Preisquelle = v.Preisquelle,
                PV_Zulaessig = v.PV_Zulaessig,
                BHKW_Ueberschuss_Zulaessig = v.BHKW_Ueberschuss_Zulaessig,
                BHKW_Stromgefuehrt = v.BHKW_Stromgefuehrt,
                Netzentladung = v.Netzentladung,
                Kompatibilitaetsmodus = v.Kompatibilitaetsmodus,
                SoC_Min_Prozent = v.SoC_Min_Prozent,
                SoC_Max_Prozent = v.SoC_Max_Prozent,
                Kapitalzins = v.Kapitalzins,
                Nutzungsdauer = v.Nutzungsdauer,
                L_P = v.L_P,
                A_Netzlade = v.A_Netzlade,
                Ladeschwellwert = v.Ladeschwellwert,
                ID_Preisreihe = v.ID_Preisreihe,
                ID_Kostenprofil = v.ID_Kostenprofil,
                Aufschlag_Anwenden = v.Aufschlag_Anwenden
            };
        }

        // ------------------------------------------------------------------ Helfer

        private static void Ausfuehren(DbVorgang v, string sql, params DbParam[] ps)
        {
            v.Ausfuehren(sql, ps);
        }

        /// <summary>
        /// Wie <see cref="Ausfuehren"/>, aber ein Fehlschlag bricht den Vorgang nicht ab —
        /// für Tabellen und Spalten, die auf einer älteren Datenbank fehlen können.
        /// </summary>
        private static void VersucheAusfuehren(DbVorgang v, string sql, params DbParam[] ps)
        {
            try { Ausfuehren(v, sql, ps); }
            catch (Exception ex) { Console.WriteLine("Komponenten-Übernahme (übergangen): " + ex.Message); }
        }

        private static int MaxId(DbVorgang v, string tabelle)
        {
            object o = v.Skalar("SELECT MAX(ID) FROM [" + tabelle + "]");
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 0;
        }

        private static DbParam Wert(string name, Type t, object v)
        {
            DbParamTyp typ = DbParamTyp.Variant;
            if (t == typeof(string)) typ = DbParamTyp.VarWChar;
            else if (t == typeof(bool)) typ = DbParamTyp.Boolean;
            else if (t == typeof(byte) || t == typeof(short) || t == typeof(int) || t == typeof(long))
                typ = DbParamTyp.Integer;
            else if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
                typ = DbParamTyp.Double;
            else if (t == typeof(DateTime)) typ = DbParamTyp.Date;

            return new DbParam(name, typ) { Wert = (v == null) ? DBNull.Value : v };
        }

        private static int Ganz(DataRow r, string spalte)
        {
            return (r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value)
                ? Convert.ToInt32(r[spalte]) : 0;
        }

        private static string Text(DataRow r, string spalte)
        {
            return (r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value)
                ? r[spalte].ToString().Trim() : "";
        }
    }
}
