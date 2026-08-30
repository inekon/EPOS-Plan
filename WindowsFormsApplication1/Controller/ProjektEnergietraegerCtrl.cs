using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die EINE Leseschicht für die Frage „welche Energieträger VERWENDET ein Projekt".
    ///
    /// <para>
    /// <b>Der Fall, den das löst.</b> Auf der Kostenseite von „Berichte &amp; Kosten"
    /// stand bis 22.08.2026 die Trägerliste aus <c>Abfrage_Energietraeger_Effektiv</c>,
    /// also der Inhalt von <c>energy_project_settings</c>. Das sind EINSTELLUNGEN je
    /// Träger (Preis, Heizwert, CO₂-Faktor) — keine Verwendungsliste. Die Variante
    /// „Wöhler - Test2" (Projekt 1024) führt dort acht Träger und zeigte sie alle an,
    /// obwohl das Projekt nur ein BHKW, einen elektrischen Heizkessel, eine Wärmepumpe
    /// und zwei Pufferspeicher hat. Wer eine solche Liste liest, hält sechs Träger für
    /// beteiligt, die nie eine Kilowattstunde liefern.
    /// </para>
    ///
    /// <para>
    /// <b>Verbaut ist, was <c>Tab_Energieanlagen</c> führt.</b> Nicht der
    /// <c>ID_Projekt</c> der Gerätetabellen: Die tragen Katalog-Projektkopien und
    /// Waisen (derselbe Befund wie bei den Komponentenzahlen,
    /// <see cref="ProjektDetails"/>). Maßgeblich ist die Verweisspalte der
    /// Anlagenzeile — dieselbe Bedingung wie in
    /// <see cref="TechnikPlanwertCtrl.Verbaut"/>.
    /// </para>
    ///
    /// <para><b>Welches Gewerk welchen Träger beiträgt</b> (an den Daten der
    /// Kenndaten.accdb vom 22.08.2026 geprüft):</para>
    /// <list type="bullet">
    ///   <item><description><b>BHKW</b> und <b>Heizkessel</b> — ihr eigener Brennstoff
    ///     bzw. Energieträger. Nur diese beiden Gewerke tragen überhaupt je einen
    ///     <c>ID_Carrier</c>: Von den zehn Anlagenzeilen der Datenbank mit
    ///     <c>ID_Carrier &gt; 0</c> sind alle vom Typ <c>BHKW_TYP</c> oder
    ///     <c>KESSEL_TYP</c>.</description></item>
    ///   <item><description><b>Wärmepumpe</b>, <b>Photovoltaik</b>, <b>Stromspeicher</b>
    ///     und ein gesetzter <b>Heizstab</b> — der Stromträger des Projekts
    ///     (<see cref="StromAufschlagCtrl.StromCarrierId"/>). Sie führen keinen eigenen
    ///     Trägerverweis; ihre Energie ist elektrische Energie, und genau die rechnet
    ///     der <c>KostenEmissionRechner</c> über den Netzbezugspfad ab.</description></item>
    ///   <item><description><b>Solarthermie</b> und <b>Pufferspeicher</b> — KEIN
    ///     Energieträger. Solarstrahlung wird nicht beschafft, ein Speicher wandelt
    ///     nicht um.</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>DIE ZWEI WELTEN: wann <c>ID_Carrier</c>, wann <c>Brennstoff</c>.</b> Der
    /// Energieträger eines Brenners steht an zwei Stellen, und die Regel ist die
    /// bereits im Haus geltende (<see cref="WirtschaftlichkeitCtrl"/>,
    /// <c>LiesBhkwAnlagen</c> / <c>BrennstoffId</c>):
    /// </para>
    /// <list type="number">
    ///   <item><description><c>Tab_Energieanlagen.ID_Carrier</c> → <c>energy_carrier</c>.
    ///     Der Träger hängt an der ANLAGE und ist die jüngere, maßgebliche Aussage.
    ///     Die Engine kennt nur ihn: <c>SimulationControl.EnergietraegerZuordnungLesen</c>
    ///     liest ausschließlich <c>ID_Carrier</c> und meldet eine leere Spalte als
    ///     Warnung; die <c>carrier_id</c> der Ergebnismodule, aus der
    ///     <see cref="KostenEmissionRechner"/> Kosten und Emissionen bildet, stammt
    ///     allein daher.</description></item>
    ///   <item><description>sonst <c>Tab_BHKW.Brennstoff</c> bzw.
    ///     <c>Tab_Heizkessel.Brennstoff</c> → <c>Tab_Brennstoff_Stamm.ID</c> →
    ///     der Träger, dessen <c>energy_carrier.id_brennstoff</c> darauf zeigt.
    ///     Der Weg des Altstands.</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>Warum nicht umgekehrt, belegt an den Daten.</b> Beide Felder sind im Bestand
    /// gepflegt und widersprechen sich stellenweise: Die BHKW-Anlage 12326 des Projekts
    /// 1024 trägt <c>ID_Carrier</c> = 56 („Heizöl EL", Brennstoff 9), ihr Katalogmodul
    /// <c>A-Tron_21_F</c> dagegen <c>Brennstoff</c> = 8 („Heizöl L"); die beiden
    /// BHKW-Anlagen des Projekts 1030 tragen Träger 63 („Erdgas E", Brennstoff 3) bei
    /// <c>Brennstoff</c> = 1 („Stadtgas"). <c>Tab_BHKW</c> beschreibt das KATALOGGERÄT
    /// — wechselt der Anwender den Träger der Anlage, bleibt die Gerätezeile stehen.
    /// Umgekehrt ist der Trägerverweis im Bestand überwiegend LEER (13 von 23
    /// Brenner-Anlagenzeilen, darunter alle der Projekte 1019 und 1023); ein Filter
    /// allein auf <c>ID_Carrier</c> leerte deren Liste vollständig. Erst beide Wege
    /// zusammen ergeben eine Aussage für jedes Projekt.
    /// </para>
    ///
    /// <para>
    /// <b>Mehrere Träger auf denselben Brennstoff.</b> Der Katalog führt zu einem
    /// <c>Tab_Brennstoff_Stamm</c>-Eintrag oft mehrere Träger (Brennstoff 13
    /// „Elektrische Energie" → 54 „Strom Variante", 58 „Elektrische Energie 2",
    /// 60 „Elektrische Energie"). Für den BRENNSTOFFWEG hat dann der dem Projekt
    /// ZUGEORDNETE Träger Vorrang, sonst der mit der kleinsten Nummer — dieselbe
    /// Auflösung wie in <c>ErgebnisCtrl.CarrierIdFuerProjekt</c> und dieselbe
    /// Ordnungsregel wie in <see cref="StromAufschlagCtrl.StromCarrierId"/>
    /// (<see cref="Auswahl"/>).
    /// </para>
    ///
    /// <para>
    /// <b>Der STROMTRÄGER folgt seit dem 30.08.2026 einer eigenen, fachlichen
    /// Regel</b> (<see cref="StandardStromTraeger"/>): „kleinste Nummer" traf im
    /// Bestand die umbenannte Variante 54 und damit den falschen Träger für
    /// Wärmepumpe, Photovoltaik und Stromspeicher. Maßgeblich ist jetzt die
    /// Auslieferungskennung <c>energy_carrier.code</c>
    /// (<see cref="DbWerte.ENERGIETRAEGER_CODE_STROM"/>); Anzeige und
    /// Wizard-Automatik lesen dieselbe Funktion.
    /// </para>
    ///
    /// <para>
    /// <b>Gespeicherte Access-Abfragen bleiben unangetastet.</b>
    /// <c>Abfrage_Energietraeger_Effektiv</c> liegt außerhalb des Repos und wird von
    /// vier Stellen gelesen; gefiltert wird deshalb im Code, nicht in der Abfrage.
    /// </para>
    /// </summary>
    internal static class ProjektEnergietraegerCtrl
    {
        /// <summary>Ein im Projekt verwendeter Energieträger mit seiner Begründung.</summary>
        internal sealed class Verwendung
        {
            /// <summary><c>energy_carrier.id</c>.</summary>
            public int CarrierId;

            /// <summary><c>energy_carrier.name</c> (leer, wenn der Katalog schweigt).</summary>
            public string Name = "";

            /// <summary>
            /// true, wenn der Träger dem Projekt in <c>energy_project_settings</c>
            /// zugeordnet ist — nur dann führt <c>Abfrage_Energietraeger_Effektiv</c>
            /// eine Zeile für ihn, nur dann kann er überhaupt angezeigt werden.
            /// </summary>
            public bool Zugeordnet;

            /// <summary>
            /// Die Anlagen, die diesen Träger beitragen, in Klartext
            /// („Heizkessel „eloBLOCK VE 10"") — der Beleg, warum er in der Liste steht.
            /// </summary>
            public readonly List<string> Beitraeger = new List<string>();

            /// <summary>Die Beiträger als eine Zeile.</summary>
            public string BeitraegerText
            { get { return string.Join(", ", Beitraeger.ToArray()); } }
        }

        // ------------------------------------------------------------------ Landkarte

        /// <summary>Eine Katalogzeile <c>energy_carrier</c>.</summary>
        private sealed class Traeger
        {
            public int Id;
            public string Name = "";
            public string Code = "";
            public int IdBrennstoff;
            public string Preismodell = "";
        }

        /// <summary>
        /// Die Energieträger, die das Projekt tatsächlich verwendet — aufsteigend nach
        /// Trägernummer. Leere Liste ist ein gültiges Ergebnis: ein Projekt ohne Erzeuger
        /// (nur Puffer, nur Solarthermie) bezieht keine Energie.
        /// </summary>
        internal static List<Verwendung> Verwendete(int projektID)
        {
            var ergebnis = new List<Verwendung>();
            if (projektID <= 0) return ergebnis;

            List<Traeger> katalog = Katalog();
            HashSet<int> zugeordnet = Zugeordnete(projektID);

            DataTable anlagen = Anlagen(projektID);
            if (anlagen == null) return ergebnis;

            // Brennstoff der Gerätezeile je verwiesenem Gerät — die Rückfallebene, wenn
            // die Anlage keinen Trägerverweis trägt. Eingesammelt über den Verbund mit
            // Tab_Energieanlagen, damit nur die Geräte DIESES Projekts gelesen werden.
            Dictionary<int, int> bhkwBrennstoff = GeraeteBrennstoff(projektID, "ID_BHKW", "Tab_BHKW");
            Dictionary<int, int> kesselBrennstoff = GeraeteBrennstoff(projektID, "ID_Kessel", "Tab_Heizkessel");

            // Der Stromträger wird höchstens einmal gesucht, auch wenn fünf Anlagen ihn
            // beitragen.
            int stromTraeger = -1;

            var gefunden = new Dictionary<int, Verwendung>();

            foreach (DataRow r in anlagen.Rows)
            {
                string bezeichner = Text(r, "Bezeichner");
                int idCarrier = Ganz(r, SchemaKatalog.SPALTE_ID_CARRIER);

                // --- Brenner: eigener Träger, zwei Welten (siehe Klassenkommentar) ---
                int geraet = Ganz(r, "ID_BHKW");
                if (geraet > 0)
                    Trage(gefunden, katalog, zugeordnet,
                          BrennerTraeger(katalog, zugeordnet, idCarrier, bhkwBrennstoff, geraet),
                          DbWerte.ERZEUGER_BHKW, bezeichner);

                geraet = Ganz(r, "ID_Kessel");
                if (geraet > 0)
                    Trage(gefunden, katalog, zugeordnet,
                          BrennerTraeger(katalog, zugeordnet, idCarrier, kesselBrennstoff, geraet),
                          DbWerte.ERZEUGER_HEIZKESSEL, bezeichner);

                // --- Elektrische Gewerke: der Stromträger des Projekts ---
                bool elektrisch = Ganz(r, "ID_WP") > 0 || Ganz(r, "ID_PV") > 0 || Ganz(r, "ID_SP") > 0;
                string gewerk = Ganz(r, "ID_WP") > 0 ? DbWerte.ERZEUGER_WAERMEPUMPE
                              : Ganz(r, "ID_PV") > 0 ? DbWerte.ERZEUGER_PHOTOVOLTAIK
                              : DbWerte.ERZEUGER_STROMSPEICHER;

                // Der Heizstab ist ein Merkmal der Anlagenzeile, kein eigenes Gerät —
                // er hebt eine Anlage aber in jedem Fall in die elektrische Welt.
                bool heizstab = Ja(r, "Heizstab");
                if (heizstab && !elektrisch) gewerk = HEIZSTAB;

                if (elektrisch || heizstab)
                {
                    if (stromTraeger < 0) stromTraeger = StromTraeger(katalog, zugeordnet, projektID);
                    Trage(gefunden, katalog, zugeordnet, stromTraeger,
                          heizstab && elektrisch ? gewerk + " + " + HEIZSTAB : gewerk, bezeichner);
                }

                // Solarthermie (ID_Solar) und Pufferspeicher (ID_PUFFER) tragen bewusst
                // NICHTS bei: keine beschaffte Energie, kein Träger.
            }

            foreach (KeyValuePair<int, Verwendung> kv in gefunden) ergebnis.Add(kv.Value);
            ergebnis.Sort(delegate (Verwendung a, Verwendung b)
                          { return a.CarrierId.CompareTo(b.CarrierId); });
            return ergebnis;
        }

        /// <summary>Klartext für den Heizstab einer Anlagenzeile (kein Gewerk der Kostenlandkarte).</summary>
        private const string HEIZSTAB = "Heizstab";

        // ================================================================= Ä19

        /// <summary>
        /// Ä19: EINE Zeile je Anlagenzeile des Projekts — für die Anlagenliste der
        /// Kosten-Seite (zwei Wärmepumpen = zwei Einträge). Der Träger ist der, den
        /// die Anlage beiträgt (dieselben Auflösungsstufen wie <see cref="Verwendete"/>:
        /// Brenner über Anlagenverweis vor Geräte-Brennstoff, elektrische Gewerke über
        /// den Stromträger des Projekts); 0 = keiner (Solarthermie/Puffer beziehen
        /// keine Energie).
        /// </summary>
        internal sealed class AnlagenEintrag
        {
            /// <summary><c>Tab_Energieanlagen.ID</c>.</summary>
            public int AnlageId;
            public string Bezeichner = "";
            /// <summary>Kostenkomponente (DbWerte-Persistenzwert).</summary>
            public string Komponente = "";
            /// <summary><c>energy_carrier.id</c>; 0 = keiner.</summary>
            public int CarrierId;
        }

        /// <summary>Die Anlagenzeilen des Projekts in Anlagereihenfolge — leere
        /// Liste ist ein gültiges Ergebnis (Projekt ohne Anlagen).</summary>
        internal static List<AnlagenEintrag> AnlagenMitTraeger(int projektID)
        {
            var ergebnis = new List<AnlagenEintrag>();
            if (projektID <= 0) return ergebnis;

            List<Traeger> katalog = Katalog();
            HashSet<int> zugeordnet = Zugeordnete(projektID);
            DataTable anlagen = Anlagen(projektID);
            if (anlagen == null) return ergebnis;

            Dictionary<int, int> bhkwBrennstoff = GeraeteBrennstoff(projektID, "ID_BHKW", "Tab_BHKW");
            Dictionary<int, int> kesselBrennstoff = GeraeteBrennstoff(projektID, "ID_Kessel", "Tab_Heizkessel");
            int stromTraeger = -1;

            foreach (DataRow r in anlagen.Rows)
            {
                var e = new AnlagenEintrag
                {
                    AnlageId = Ganz(r, "ID"),
                    Bezeichner = Text(r, "Bezeichner")
                };
                int idCarrier = Ganz(r, SchemaKatalog.SPALTE_ID_CARRIER);

                int geraet = Ganz(r, "ID_BHKW");
                if (geraet > 0)
                {
                    e.Komponente = DbWerte.ERZEUGER_BHKW;
                    e.CarrierId = BrennerTraeger(katalog, zugeordnet, idCarrier, bhkwBrennstoff, geraet);
                }
                else if ((geraet = Ganz(r, "ID_Kessel")) > 0)
                {
                    e.Komponente = DbWerte.ERZEUGER_HEIZKESSEL;
                    e.CarrierId = BrennerTraeger(katalog, zugeordnet, idCarrier, kesselBrennstoff, geraet);
                }
                else if (Ganz(r, "ID_WP") > 0) e.Komponente = DbWerte.ERZEUGER_WAERMEPUMPE;
                else if (Ganz(r, "ID_PV") > 0) e.Komponente = DbWerte.ERZEUGER_PHOTOVOLTAIK;
                else if (Ganz(r, "ID_SP") > 0) e.Komponente = DbWerte.ERZEUGER_STROMSPEICHER;
                else if (Ganz(r, "ID_Solar") > 0) e.Komponente = DbWerte.ERZEUGER_SOLARTHERMIE;
                else if (Ganz(r, "ID_PUFFER") > 0) e.Komponente = DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER;
                else continue;   // leere Anlagenzeile — nichts anzuzeigen

                bool elektrisch = Ganz(r, "ID_WP") > 0 || Ganz(r, "ID_PV") > 0 || Ganz(r, "ID_SP") > 0;
                if (e.CarrierId <= 0 && (elektrisch || Ja(r, "Heizstab")))
                {
                    if (stromTraeger < 0) stromTraeger = StromTraeger(katalog, zugeordnet, projektID);
                    if (stromTraeger > 0) e.CarrierId = stromTraeger;
                }

                ergebnis.Add(e);
            }
            return ergebnis;
        }

        // ------------------------------------------------------------- Trägerauflösung

        /// <summary>
        /// Der Träger eines Brenners: erst der Verweis der ANLAGE, sonst der Brennstoff
        /// der Gerätezeile. 0 = nicht ermittelbar.
        /// </summary>
        private static int BrennerTraeger(List<Traeger> katalog, HashSet<int> zugeordnet,
                                          int idCarrier, Dictionary<int, int> brennstoffe, int geraet)
        {
            // Stufe 1 — der Trägerverweis der Anlage, aber nur, wenn er im Katalog
            // ankommt. Ein Verweis ins Leere ist keine Aussage.
            if (idCarrier > 0 && Zeile(katalog, idCarrier) != null) return idCarrier;

            // Stufe 2 — der Brennstoff der Gerätezeile.
            int idBrennstoff;
            if (!brennstoffe.TryGetValue(geraet, out idBrennstoff) || idBrennstoff <= 0) return 0;

            return Auswahl(katalog, zugeordnet,
                           delegate (Traeger t) { return t.IdBrennstoff == idBrennstoff; });
        }

        /// <summary>
        /// <b>DIE eine Wahrheit: der Stromträger eines Projekts</b> — Wärmepumpe,
        /// Photovoltaik und Stromspeicher beziehen ihn (Anwenderentscheid 30.08.2026).
        /// 0 = der Katalog führt keinen Stromträger.
        ///
        /// <para>Dieselbe Funktion beantwortet die Frage für die ANZEIGE
        /// (<c>UcBkKosten.LadeTraeger</c>, <see cref="Verwendete"/>,
        /// <see cref="AnlagenMitTraeger"/>) und für die AUTOMATIK
        /// (<c>WizardCtrl.Add_Projekt_Energietraeger</c>). Zwei Fassungen hätten
        /// genau den Befund erzeugt, der zu dieser Änderung führte: Die Anzeige
        /// nannte einen anderen Träger, als die Automatik zugeordnet hätte.</para>
        ///
        /// <para><b>Die Regel, in dieser Reihenfolge:</b></para>
        /// <list type="number">
        ///   <item><description><b>Was das Projekt schon führt.</b> Ist dem Projekt
        ///     in <c>energy_project_settings</c> bereits ein Träger mit
        ///     <c>pricing_model = ELECTRICITY</c> zugeordnet, gilt der
        ///     (<see cref="StromAufschlagCtrl.StromCarrierId"/> — dieselbe Auswahl,
        ///     die Strompreis, Aufschläge und <c>KostenEmissionRechner</c> nutzen).
        ///     Eine Entscheidung des Anwenders wird nicht überstimmt.</description></item>
        ///   <item><description><b>Sonst der Auslieferungsträger des Katalogs</b>
        ///     nach <see cref="KatalogStromTraeger"/>.</description></item>
        /// </list>
        /// </summary>
        internal static int StandardStromTraeger(int projektID)
        {
            return StromTraeger(Katalog(), Zugeordnete(projektID), projektID);
        }

        /// <summary>Innenfassung von <see cref="StandardStromTraeger"/> für die
        /// Aufrufer, die Katalog und Zuordnungsmenge ohnehin schon gelesen haben.</summary>
        private static int StromTraeger(List<Traeger> katalog, HashSet<int> zugeordnet, int projektID)
        {
            // Stufe 1 - der dem Projekt bereits zugeordnete Stromtraeger.
            try
            {
                int id = StromAufschlagCtrl.StromCarrierId(projektID);
                if (id > 0) return id;
            }
            catch { }

            // Stufe 2 - der Auslieferungstraeger des Katalogs. Die Zuordnungsmenge
            // spielt hier bewusst KEINE Rolle mehr: Sie ist in Stufe 1 bereits
            // abschliessend ausgewertet (StromCarrierId liest genau sie).
            return KatalogStromTraeger(katalog);
        }

        /// <summary>
        /// <b>Der Auslieferungs-Stromträger des Katalogs</b>, wenn das Projekt selbst
        /// noch keinen führt. 0 = der Katalog führt keinen.
        ///
        /// <para><b>Warum die frühere Regel „kleinste Nummer" falsch war.</b> Der
        /// Bestand führt DREI Träger mit <c>pricing_model = ELECTRICITY</c>: 54
        /// („Strom Variante"), 58 („Elektrische Energie 2") und 60 („Elektrische
        /// Energie"). Die kleinste Nummer traf damit ausgerechnet die vom Anwender
        /// umbenannte Variante — für eine Wärmepumpe, eine PV-Anlage oder einen
        /// Stromspeicher die falsche Aussage.</para>
        ///
        /// <para><b>Die Kennung, an der erkannt wird.</b> Erhoben wurde, was an
        /// belastbaren Merkmalen überhaupt vorhanden ist: <c>is_active</c> steht bei
        /// allen 27 Katalogzeilen auf TRUE, <c>sort_order</c> ist durchgehend NULL,
        /// ein <c>ReadOnly</c>-/Auslieferungskennzeichen gibt es in
        /// <c>energy_carrier</c> nicht, und <c>id_brennstoff</c> ist bei allen drei
        /// Zeilen 13. Übrig bleibt <c>code</c> — der Verweisanker, den
        /// <c>EnergietraegerKatalogCtrl.Umbenennen</c> nie anfasst; alle drei tragen
        /// „Elektrische Energie" (<see cref="DbWerte.ENERGIETRAEGER_CODE_STROM"/>).
        /// Er benennt die FAMILIE, nicht die einzelne Zeile — deshalb entscheidet ein
        /// zweites, ebenso persistentes Merkmal den Gleichstand.</para>
        ///
        /// <para><b>Rangfolge</b> (höchster Rang gewinnt, bei Gleichstand die
        /// kleinste Nummer):</para>
        /// <list type="bullet">
        ///   <item><description><b>+2 · <c>code</c> = Auslieferungskennung.</b> Die
        ///     Zeile gehört zur Stromfamilie der Auslieferung.</description></item>
        ///   <item><description><b>+1 · <c>name</c> = <c>code</c>.</b> Die Zeile trägt
        ///     noch ihre Auslieferungsbezeichnung, ist also nicht umbenannt worden.
        ///     Das trifft im Bestand genau die 60 — 54 und 58 sind umbenannt. Auch für
        ///     eine über die Oberfläche erzeugte Variante gilt <c>name = code</c>
        ///     (<c>EnergietraegerKatalogCtrl.Variante</c> setzt beide auf „X Variante"),
        ///     sie trägt dann aber einen ANDEREN Code und bleibt mit +1 hinter der
        ///     Auslieferungszeile mit +3.</description></item>
        /// </list>
        ///
        /// <para><b>Rückfall.</b> Führt keine Zeile die Auslieferungskennung (fremd
        /// aufgebauter Katalog), gewinnt unter allen ELECTRICITY-Trägern die nicht
        /// umbenannte, sonst die mit der kleinsten Nummer — dieselbe Ordnungsregel
        /// wie bisher, nur nachrangig statt allein.</para>
        /// </summary>
        private static int KatalogStromTraeger(List<Traeger> katalog)
        {
            int besterRang = -1, bester = 0;
            foreach (Traeger t in katalog)
            {
                if (!string.Equals(t.Preismodell, StromAufschlagCtrl.PRICING_MODEL_STROM,
                                   StringComparison.OrdinalIgnoreCase))
                    continue;

                int rang = 0;
                if (string.Equals(t.Code, DbWerte.ENERGIETRAEGER_CODE_STROM,
                                  StringComparison.OrdinalIgnoreCase))
                    rang += 2;
                if (t.Code.Length > 0 &&
                    string.Equals(t.Name, t.Code, StringComparison.OrdinalIgnoreCase))
                    rang += 1;

                if (rang > besterRang || (rang == besterRang && bester > 0 && t.Id < bester))
                {
                    besterRang = rang;
                    bester = t.Id;
                }
            }
            return bester;
        }

        /// <summary>
        /// Aus allen passenden Katalogträgern der maßgebliche: vorrangig ein dem Projekt
        /// ZUGEORDNETER (nur der hat Preis und Heizwert des Projekts), sonst der mit der
        /// kleinsten Nummer. 0 = keiner passt.
        /// </summary>
        private static int Auswahl(List<Traeger> katalog, HashSet<int> zugeordnet,
                                   Predicate<Traeger> passt)
        {
            int besterZugeordnet = 0, besterKatalog = 0;
            foreach (Traeger t in katalog)
            {
                if (!passt(t)) continue;
                if (besterKatalog == 0 || t.Id < besterKatalog) besterKatalog = t.Id;
                if (zugeordnet.Contains(t.Id) && (besterZugeordnet == 0 || t.Id < besterZugeordnet))
                    besterZugeordnet = t.Id;
            }
            return besterZugeordnet > 0 ? besterZugeordnet : besterKatalog;
        }

        /// <summary>Trägt einen Beitrag ein; <paramref name="carrierId"/> 0 wird übergangen.</summary>
        private static void Trage(Dictionary<int, Verwendung> gefunden, List<Traeger> katalog,
                                  HashSet<int> zugeordnet, int carrierId,
                                  string gewerk, string bezeichner)
        {
            if (carrierId <= 0) return;

            Verwendung v;
            if (!gefunden.TryGetValue(carrierId, out v))
            {
                Traeger t = Zeile(katalog, carrierId);
                v = new Verwendung
                {
                    CarrierId = carrierId,
                    Name = t != null ? t.Name : "",
                    Zugeordnet = zugeordnet.Contains(carrierId)
                };
                gefunden[carrierId] = v;
            }

            string klartext = string.IsNullOrEmpty(bezeichner)
                            ? gewerk : gewerk + " „" + bezeichner + "“";
            if (!v.Beitraeger.Contains(klartext)) v.Beitraeger.Add(klartext);
        }

        private static Traeger Zeile(List<Traeger> katalog, int id)
        {
            foreach (Traeger t in katalog) if (t.Id == id) return t;
            return null;
        }

        // -------------------------------------------------------------------- Lesen

        /// <summary>Der Trägerkatalog. Leere Liste, wenn die Tabelle fehlt (alte Datenbank).</summary>
        private static List<Traeger> Katalog()
        {
            var liste = new List<Traeger>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT id, name, code, id_brennstoff, pricing_model FROM [" +
                    SchemaKatalog.ENERGY_CARRIER + "]");
                if (dt == null) return liste;
                foreach (DataRow r in dt.Rows)
                {
                    int id = Ganz(r, "id");
                    if (id <= 0) continue;
                    liste.Add(new Traeger
                    {
                        Id = id,
                        Name = Text(r, "name"),
                        Code = Text(r, "code"),
                        IdBrennstoff = Ganz(r, "id_brennstoff"),
                        Preismodell = Text(r, "pricing_model")
                    });
                }
            }
            catch { liste.Clear(); }
            return liste;
        }

        /// <summary>Die dem Projekt zugeordneten Träger (<c>energy_project_settings</c>).</summary>
        private static HashSet<int> Zugeordnete(int projektID)
        {
            var menge = new HashSet<int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [ID_Energieträger] FROM [" + SchemaKatalog.ENERGY_PROJECT_SETTINGS +
                    "] WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", (Int32)projektID));
                if (dt == null) return menge;
                foreach (DataRow r in dt.Rows)
                {
                    if (r[0] == DBNull.Value) continue;
                    try { menge.Add(Convert.ToInt32(r[0])); } catch { }
                }
            }
            catch { }
            return menge;
        }

        /// <summary>Die Anlagenzeilen des Projekts. <c>null</c> = Abfrage gescheitert.</summary>
        private static DataTable Anlagen(int projektID)
        {
            try
            {
                return DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, ID_WP, ID_Kessel, ID_BHKW, ID_PV, ID_Solar, " +
                    "ID_SP, ID_PUFFER, [" + SchemaKatalog.SPALTE_ID_CARRIER + "], Heizstab " +
                    "FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] WHERE ID_Projekt = ? " +
                    "ORDER BY ID",
                    new OleDbParameter("@p", (Int32)projektID));
            }
            catch { return null; }
        }

        /// <summary>
        /// Gerät → <c>Brennstoff</c> für die Geräte, auf die die Anlagenzeilen DIESES
        /// Projekts über <paramref name="verweis"/> zeigen. Leere Zuordnung, wenn die
        /// Abfrage scheitert — dann greift die Rückfallebene schlicht nicht.
        /// </summary>
        private static Dictionary<int, int> GeraeteBrennstoff(int projektID, string verweis,
                                                              string tabelle)
        {
            var zuordnung = new Dictionary<int, int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT g.ID, g.Brennstoff FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] AS a " +
                    "INNER JOIN [" + tabelle + "] AS g ON a.[" + verweis + "] = g.ID " +
                    "WHERE a.ID_Projekt = ?",
                    new OleDbParameter("@p", (Int32)projektID));
                if (dt == null) return zuordnung;
                foreach (DataRow r in dt.Rows)
                {
                    int id = Ganz(r, "ID");
                    if (id > 0) zuordnung[id] = Ganz(r, "Brennstoff");
                }
            }
            catch { }
            return zuordnung;
        }

        // ------------------------------------------------------------------- Helfer

        private static int Ganz(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); } catch { return 0; }
        }

        private static string Text(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            return Convert.ToString(r[spalte]).Trim();
        }

        private static bool Ja(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return false;
            try { return Convert.ToBoolean(r[spalte]); } catch { return false; }
        }
    }
}
