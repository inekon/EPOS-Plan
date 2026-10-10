using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE D4 (Konzept_KonfigUI_Hydraulik, Abschnitt 3 „Ansicht Schema" und
    /// Mockup-Abschnitt 1/2) — das ZEICHENMODELL der Hydraulikübersicht: Knoten,
    /// Kanten und Kaskadenkette eines Projekts.
    ///
    /// <b>Ohne Oberfläche.</b> Hier steht kein <c>System.Windows.Forms</c> und kein
    /// <c>System.Drawing</c>: Was gezeichnet wird, entscheidet dieses Modell, WIE es
    /// gezeichnet wird, entscheidet <c>SchemaAnsicht</c>. Damit ist die Aussage des
    /// Schemas headless prüfbar — Knoten- und Kantenliste gegen die Datenbank, statt
    /// Pixel gegen ein Bild (Verifikationsvorgabe D4).
    ///
    /// <b>Eine Ableitung, keine zweite.</b> Die Verschaltung kommt aus
    /// <see cref="Hydraulikbild"/> — derselben Abbildung, mit der die Dialogprüfung aus
    /// D5b den Ring sucht. Ladepositionen kommen aus <see cref="Ladeordnung"/>,
    /// Speicherstammdaten aus <see cref="WaermesenkeClass.ProjektPufferListe"/>. Das
    /// Modell rechnet nichts nach, was eine dieser Stellen schon weiß.
    ///
    /// <b>Invariante S-1</b> (Konzept Abschnitt 5): Zwischen zwei Speicherknoten steht
    /// IMMER ein Erzeugerknoten. Strukturell garantiert — eine Kante entsteht hier nur
    /// aus einem Quell- oder Senkenbezug, und die gibt es ausschließlich an
    /// <c>Tab_Energieanlagen</c>. <see cref="Pruefen"/> prüft es zusätzlich nach, damit
    /// eine künftige Erweiterung nicht still dagegen verstößt.
    /// </summary>
    public sealed class SchemaModell
    {
        // --- Sprachneutrale Schlüssel (Drei-Schichten-Regel, Schicht „Schlüssel") -----

        public const string PRAEFIX_QUELLE = "QUELLE_";
        public const string PRAEFIX_ERZEUGER = "ERZEUGER_";
        public const string PRAEFIX_SPEICHER = "SPEICHER_";
        public const string ABNEHMER_HEIZKREIS = "ABNEHMER_HEIZKREIS";
        public const string ABNEHMER_WARMWASSER = "ABNEHMER_WARMWASSER";

        /// <summary>
        /// PAKET S2 — der dritte Abnehmerknoten (Konzept 10). Prozesswärme ist seit
        /// Paket K1/K2 ein eigener Kanal und seit S1 ein eigenes Senkenziel
        /// (<c>Prozesswaerme</c>, <c>PufferProzess</c>); bis S2 fehlte sie in der
        /// Abnehmerspalte, weil das Schema die Senke aus den beiden Altspalten las und
        /// die Prozess-Ziele dort gar nicht ausdrückbar sind.
        /// </summary>
        public const string ABNEHMER_PROZESS = "ABNEHMER_PROZESS";

        /// <summary>
        /// STUFE KU2 der Kühlung (Kühlkonzept 4.3 #33) — der vierte Abnehmerknoten: der
        /// KÄLTEKREIS. Bedient wird er von den Wärmepumpen im Kühlbetrieb eines Projekts, das
        /// Kälte rechnet; ohne sie und mit Kältebedarf steht er mit Warnzeichen da wie jeder
        /// Abnehmer ohne Versorger. Kein Speicher bedient ihn — einen Kältespeicher gibt es erst
        /// mit KU3 (K7).
        /// </summary>
        public const string ABNEHMER_KAELTEKREIS = "ABNEHMER_KAELTEKREIS";

        // --- Auftrag KS: die KÄLTEBAHN unter der Wärmebahn ------------------------------
        //
        // Eigene Schlüsselpräfixe, weil eine Wärmepumpe im Kühlbetrieb in BEIDEN Bahnen
        // steht (oben als Wärmeerzeuger, unten als Kälteerzeuger) und ein Schlüssel genau
        // einen Kasten benennt. Der Kältekreis behält seinen Schlüssel und steht unten.

        /// <summary>Rückkühlung bzw. Quelle eines Kälteerzeugers (Spalte 0 der Kältebahn).</summary>
        public const string PRAEFIX_KAELTE_QUELLE = "KQUELLE_";

        /// <summary>Kälteerzeuger: Kältemaschine oder Wärmepumpe im Kühlbetrieb (Spalte 1).</summary>
        public const string PRAEFIX_KAELTE_ERZEUGER = "KERZEUGER_";

        /// <summary>Kältespeicher: Projektpuffer mit der Verwendung Kälte (Spalte 2).</summary>
        public const string PRAEFIX_KAELTE_SPEICHER = "KSPEICHER_";

        /// <summary>Bahn eines Knotens: oben die Wärme, darunter — nur mit Kälte — die Kälte.</summary>
        public enum Bahn
        {
            /// <summary>Die Wärmebahn (Quelle → Erzeuger → Puffer → Heizkreis, Warmwasser, Prozess).</summary>
            Waerme,

            /// <summary>Die Kältebahn (Rückkühlung → Kälteerzeuger → Kältespeicher → Kältekreis).</summary>
            Kaelte
        }

        /// <summary>Spalte des Schemas — die vier Rubriken des Mockups.</summary>
        public enum Knotenart
        {
            /// <summary>Spalte 0: Wärmequelle (Außenluft, Erdsonde, Brennstoff …).</summary>
            Quelle,

            /// <summary>Spalte 1: Wärmeerzeuger.</summary>
            Erzeuger,

            /// <summary>Spalte 2: Pufferspeicher.</summary>
            Speicher,

            /// <summary>Spalte 3: Abnehmer (Heizkreis, Warmwasser).</summary>
            Abnehmer
        }

        /// <summary>Farbsprache der Verbindungen (Mockup, Legende).</summary>
        public enum Kantenart
        {
            /// <summary>Blau: Quellseite (Quelle → Erzeuger).</summary>
            Quelle,

            /// <summary>Blau gestrichelt: Kaskade (Puffer → nachgeschalteter Erzeuger).</summary>
            Kaskade,

            /// <summary>Koralle: Ladung (Erzeuger → Puffer), Kreis = wirksame Priorität.</summary>
            Ladung,

            /// <summary>Grün: Versorgung / Entladung (Puffer bzw. Erzeuger → Abnehmer).</summary>
            Versorgung,

            /// <summary>
            /// PAKET E1 (Befund S2-O7, Konzept § 10): Violett — Versorgung des
            /// PROZESS-Abnehmers.
            ///
            /// <para>Bis hierher trug die Prozesskante dieselbe Farbe wie Heizkreis und
            /// Warmwasser; der dritte Kanal war im Bild nicht von den beiden anderen zu
            /// unterscheiden. Fachlich ist er es sehr wohl — er hat eigene
            /// Temperaturanforderungen, eigene Senken und eigene Speicher.</para>
            ///
            /// <para><b>Eine eigene ART, keine eigene Kantenlogik:</b> Sie verhält sich in
            /// jeder Hinsicht wie <see cref="Versorgung"/> (durchgezogen, Pfeil,
            /// Prioritätskreis) und unterscheidet sich allein in der Farbe — genau die
            /// Trennung, die Kapitel 10 „eigene Kantenfarbe" verlangt.</para>
            /// </summary>
            Prozess
        }

        /// <summary>Ein Kasten im Schema.</summary>
        public sealed class Knoten
        {
            /// <summary>Sprachneutraler Schlüssel, z. B. <c>ERZEUGER_11203</c>.</summary>
            public string Schluessel = "";

            public Knotenart Art;

            /// <summary>Tab_Energieanlagen.ID bzw. Tab_Pufferspeicher.ID; 0 sonst.</summary>
            public int ID;

            /// <summary>ID_Type der Anlage (nur bei <see cref="Knotenart.Erzeuger"/>).</summary>
            public int ID_Type;

            /// <summary>Kaskadenrang als Text; "" = kein Rang.</summary>
            public string Rang = "";

            public string Titel = "";

            /// <summary>Zusatzzeilen unter dem Titel (Temperaturpaar, Volumen, Senke …).</summary>
            public List<string> Zeilen = new List<string>();

            /// <summary>Verwendungs-Badges eines Speichers (Heizung / Warmwasser).</summary>
            public List<string> Badges = new List<string>();

            /// <summary>Mouseover-Text; die Konfigurationsseite ersetzt ihn durch die Kartenkurzinfo.</summary>
            public string Hinweis = "";

            /// <summary>Temperatur-Warnregel (Konzept Abschnitt 5) — amber gezeichnet.</summary>
            public bool Warnung;

            /// <summary>Warnungstext (nur gesetzt, wenn <see cref="Warnung"/>).</summary>
            public string Warntext = "";

            /// <summary>Kaskadenbezug — blau gestrichelter Rahmen (Mockup: „Spitzenkessel").</summary>
            public bool Kaskade;

            /// <summary>Auftrag KS: die Bahn des Knotens — Wärme (Vorgabe) oder Kälte.</summary>
            public Bahn Bahn = Bahn.Waerme;
        }

        /// <summary>Eine Verbindung zwischen zwei Knoten.</summary>
        public sealed class Kante
        {
            public string Von = "";
            public string Nach = "";
            public Kantenart Art;

            /// <summary>Wirksame Ladepriorität für den Kreis an der Kante; 0 = keiner.</summary>
            public int Prioritaet;

            public string Hinweis = "";
        }

        /// <summary>Ein Glied der Kaskadenkette (Pillen-Band unter dem Schema).</summary>
        public sealed class Kettenglied
        {
            /// <summary>Knotenschlüssel, auf den das Glied zeigt; "" = kein Knoten.</summary>
            public string Schluessel = "";

            public string Text = "";
            public Knotenart Art;

            /// <summary>Farbe des Pfeils VOR diesem Glied; beim ersten Glied ohne Bedeutung.</summary>
            public Kantenart PfeilDavor;
        }

        public readonly List<Knoten> Knotenliste = new List<Knoten>();
        public readonly List<Kante> Kantenliste = new List<Kante>();

        /// <summary>Die abgeleiteten Kaskadenketten; leer, wenn das Projekt keine führt.</summary>
        public readonly List<List<Kettenglied>> Ketten = new List<List<Kettenglied>>();

        /// <summary>
        /// Auftrag KS: die Kälteerzeuger in der Reihenfolge der Kälte-Kaskade (Bezeichner) —
        /// dieselbe Reihenfolge, in der der Lauf sie fragt: erst die Wärmepumpen im Kühlbetrieb,
        /// dann die Kältemaschinen, je in der Folge ihrer Anlagenzeilen. Leer ohne Kälteerzeuger.
        /// </summary>
        public readonly List<string> KaelteKette = new List<string>();

        /// <summary>true, wenn mindestens ein Knoten in der Kältebahn steht.</summary>
        public bool HatKaelte
        {
            get
            {
                foreach (Knoten k in Knotenliste)
                    if (k.Bahn == Bahn.Kaelte) return true;
                return false;
            }
        }

        /// <summary>Projekt, aus dem das Modell stammt.</summary>
        public int ID_Projekt { get; private set; }

        /// <summary>
        /// true, wenn MINDESTENS EIN gezeichneter Erzeuger seine Wärme aus einem
        /// Pufferspeicher bezieht — das Projekt führt also eine Kaskade.
        ///
        /// <para><b>Anwenderbefund W10b-B-1 (05.09.2026).</b> Bis hierher las die Anzeige
        /// diese Aussage aus <see cref="Ketten"/>: leeres Band = „Keine Kaskade im
        /// Projekt — kein Erzeuger bezieht seine Wärme aus einem Pufferspeicher". Das
        /// Band ist aber die abgeleitete KETTE und nicht die Tatsache: Es entstand nur,
        /// wenn ein Erzeuger OHNE Quellpuffer den Quellpuffer eines anderen auf RANG 1
        /// lud. Projekt 1042 der Testdatenbank lädt ihn auf Rang 2 — zwei Erzeugerkästen
        /// trugen „Quelle: Puffer 3000Ltr · Kaskade", und darunter stand, es gebe keine.
        /// Die Tatsache steht deshalb hier, an derselben Stelle, aus der auch
        /// <see cref="Knoten.Kaskade"/> und die Kaskadenkanten kommen.</para>
        /// </summary>
        public bool HatKaskade { get; private set; }

        // --- PAKET S2: die Senkenlisten und die Klassen-Sets --------------------------
        //
        // Bis S1 las das Schema die Senke einer Anlage aus WaermesenkeClass.SenkeDaten,
        // also aus den beiden gespiegelten Altspalten. Zwei Dinge gehen darin verloren:
        // die Ränge ab 3 (sie haben keine Altspalte) und die beiden Prozess-Ziele (sie
        // spiegeln sich als „Heizung"). Deshalb liest das Modell die geordneten
        // Senkenlisten; mit Paket A1 ist der Rückfall auf die Altspalten entfallen
        // (Begründung in SenkenlistenLesen).
        //
        // STILL gelesen: Die laute Fassung WaermesenkeClass.SenkenlistenLaden schreibt
        // Protokollzeilen in SimulationProtokoll.Aktuell (Rang-1-Invariante, Puffer-Ziel
        // ohne Puffer). Aus einem Konfigurationsdialog heraus gerufen, landeten sie im
        // Protokoll des NÄCHSTEN Laufs. Die Zeilen kommen deshalb direkt über
        // Z_AnlageSenkeCtrl, die Ladeordnung über SenkenlistenLadenStill - dieselbe
        // Quelle, kein Nebeneffekt.

        /// <summary>Senkenzeilen je Anlagen-ID, in Rangfolge.</summary>
        private readonly Dictionary<int, List<Z_AnlageSenkeModel>> _senken =
            new Dictionary<int, List<Z_AnlageSenkeModel>>();

        /// <summary>Klassen-Set je Puffer-ID (Konzept 6.1).</summary>
        private readonly Dictionary<int, PufferSpCtrl.KlassenSet> _sets =
            new Dictionary<int, PufferSpCtrl.KlassenSet>();

        /// <summary>Die Senkenzeilen einer Anlage in Rangfolge; nie <c>null</c>.</summary>
        private List<Z_AnlageSenkeModel> Senken(int idAnlage)
        {
            List<Z_AnlageSenkeModel> kette;
            return _senken.TryGetValue(idAnlage, out kette)
                ? kette : new List<Z_AnlageSenkeModel>();
        }

        /// <summary>Die Senkenzeile eines Rangs (0-basiert); <c>null</c>, wenn es sie nicht gibt.</summary>
        private Z_AnlageSenkeModel SenkeAufRang(int idAnlage, int index)
        {
            List<Z_AnlageSenkeModel> kette = Senken(idAnlage);
            return index >= 0 && index < kette.Count ? kette[index] : null;
        }

        /// <summary>Bedient der Puffer diesen Kanal (Klassen-Set)? Unbekannte ID: nein.</summary>
        private bool PufferBedient(int idPuffer, int kanal)
        {
            PufferSpCtrl.KlassenSet set;
            if (idPuffer <= 0 || !_sets.TryGetValue(idPuffer, out set) || set == null) return false;

            // Kühlkonzept 4.3 #26: Kein Speicher bedient den Kühlkanal (Kältespeicher erst mit
            // KU3, K7) - ausdrücklich, nicht über den Rückfall auf Heizung darunter.
            if (Kanal.IstKaelte(kanal)) return false;

            switch (kanal)
            {
                case Kanal.BRAUCHWASSER: return set.Brauchwasser;
                case Kanal.PROZESS: return set.Prozess;
                default: return set.Heizung;
            }
        }

        /// <summary>
        /// Die Kanäle, die eine DIREKTSENKE bedient — das Anzeige-Gegenstück zu
        /// <c>Kaskadenschleife.SenkenMaske</c>:
        /// <c>Prozesswaerme</c> → {P}; <c>Heizkreis</c> je Bedarfsart
        /// <c>Beides</c> → {H, B}, <c>Warmwasser</c> → {B}, <c>Heizung</c> → {H};
        /// Puffer-Ziele haben keinen direkten Abnehmer.
        /// </summary>
        private static bool DirektsenkeBedient(Z_AnlageSenkeModel z, int kanal)
        {
            if (z == null || WaermesenkeClass.IstPufferZiel(z.Ziel)) return false;

            // Kühlkonzept 4.3 #26: Der Kältekreis bedient allein die Kälte, und eine
            // Wärmesenke bedient nie einen Kältekanal - ausdrücklich, nicht über das
            // abschließende „return true" (Beides).
            if (WaermesenkeClass.IstKaelteZiel(z.Ziel)) return Kanal.IstKaelte(kanal);
            if (Kanal.IstKaelte(kanal)) return false;

            if (string.Equals(z.Ziel, DbWerte.WS_ZIEL_PROZESS, StringComparison.Ordinal))
                return kanal == Kanal.PROZESS;

            if (kanal == Kanal.PROZESS) return false;

            if (string.Equals(z.Bedarfsart, WaermequelleClass.SENKE_WARMWASSER, StringComparison.Ordinal))
                return kanal == Kanal.BRAUCHWASSER;
            if (string.Equals(z.Bedarfsart, WaermequelleClass.SENKE_HEIZUNG, StringComparison.Ordinal))
                return kanal == Kanal.HEIZUNG;

            return true;                                  // Beides
        }

        /// <summary>true, wenn kein einziger Erzeuger- oder Speicherknoten entstanden ist.</summary>
        public bool IstLeer
        {
            get
            {
                foreach (Knoten k in Knotenliste)
                    if (k.Art == Knotenart.Erzeuger || k.Art == Knotenart.Speicher) return false;
                return true;
            }
        }

        public Knoten Finden(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;
            foreach (Knoten k in Knotenliste)
                if (string.Equals(k.Schluessel, schluessel, StringComparison.Ordinal)) return k;
            return null;
        }

        /// <summary>Alle Knoten einer Spalte EINER Bahn, in Aufbaureihenfolge (Auftrag KS).</summary>
        public List<Knoten> Spalte(Knotenart art, Bahn bahn)
        {
            List<Knoten> liste = new List<Knoten>();
            foreach (Knoten k in Knotenliste)
                if (k.Art == art && k.Bahn == bahn) liste.Add(k);
            return liste;
        }

        /// <summary>Alle Knoten einer Spalte, in Aufbaureihenfolge.</summary>
        public List<Knoten> Spalte(Knotenart art)
        {
            List<Knoten> liste = new List<Knoten>();
            foreach (Knoten k in Knotenliste)
                if (k.Art == art) liste.Add(k);
            return liste;
        }

        // --- Aufbau -------------------------------------------------------------------

        /// <summary>
        /// Baut das Schema eines Projekts.
        /// </summary>
        /// <param name="idProjekt">Projekt; ≤ 0 liefert ein leeres Modell</param>
        /// <param name="kaskade">
        /// Die AUFGENOMMENEN Wärmeerzeuger in Kaskadenreihenfolge (die DB-Werte aus
        /// <c>Tab_Einstellungen.Tool_1..4</c>, wie sie die Kartenansicht liest). Gezeichnet
        /// wird genau das, was gerechnet wird; <c>null</c> = alle Anlagen des Projekts.
        /// </param>
        public static SchemaModell Aufbauen(int idProjekt, IList<string> kaskade)
        {
            return Aufbauen(idProjekt, kaskade, null);
        }

        /// <summary>
        /// Dasselbe Schema, dazu der JAHRESBEDARF JE KANAL [MWh/a] für die Abnehmer ohne
        /// Versorger: Ein Kanal mit Bedarf, den keine Senke bedient, bekommt seinen
        /// Abnehmerknoten trotzdem — mit Warnzeichen und dem Satz
        /// <see cref="Warnkriterien.KanalOhneVersorgerText"/> im Hinweis.
        /// </summary>
        /// <param name="kanalBedarfMwh">
        /// Kanalbedarf wie <c>SimulationRunner.BedarfJeKanal</c>; <c>null</c> = nicht
        /// gerechnet. Dann gilt für Warmwasser und Prozesswärme die Zuordnungsfrage
        /// (Profil zugeordnet ja/nein) als Bedarf, der Satz steht ohne Menge, und für den
        /// Heizkreis entsteht ohne Versorger kein Knoten — ob dort Bedarf liegt, weiß
        /// ohne Rechnung niemand.
        /// </param>
        public static SchemaModell Aufbauen(int idProjekt, IList<string> kaskade,
                                            double[] kanalBedarfMwh)
        {
            SchemaModell m = new SchemaModell();
            m.ID_Projekt = idProjekt;
            if (idProjekt <= 0) return m;

            Hydraulikbild bild = Hydraulikbild.Lesen(idProjekt);
            if (bild == null) return m;

            List<WaermesenkeClass.PufferInfo> puffer =
                WaermesenkeClass.ProjektPufferListe(idProjekt, null);
            if (puffer == null) puffer = new List<WaermesenkeClass.PufferInfo>();

            Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId =
                new Dictionary<int, WaermesenkeClass.PufferInfo>();
            foreach (WaermesenkeClass.PufferInfo p in puffer)
                if (p != null && !pufferJeId.ContainsKey(p.ID)) pufferJeId[p.ID] = p;

            // Rang je Erzeugerart aus der Kaskadenreihenfolge; ohne Vorgabe kein Rang.
            Dictionary<int, int> rangJeTyp = new Dictionary<int, int>();
            if (kaskade != null)
                for (int i = 0; i < kaskade.Count; i++)
                {
                    int typ = TypZuDbWert(kaskade[i]);
                    if (typ > 0 && !rangJeTyp.ContainsKey(typ)) rangJeTyp[typ] = i + 1;
                }

            bool hatBrauchwasser = WaermesenkeClass.ProjektHatBrauchwasser(idProjekt);
            bool hatProzess = ProjektHatProzesswaerme(idProjekt);

            // Anlagen, die gezeichnet werden: die aufgenommenen Arten (oder alle).
            List<Hydraulikbild.AnlagenEintrag> anlagen = new List<Hydraulikbild.AnlagenEintrag>();
            foreach (Hydraulikbild.AnlagenEintrag a in bild.Anlagen)
            {
                if (kaskade != null && !rangJeTyp.ContainsKey(a.ID_Type)) continue;
                anlagen.Add(a);
            }

            // PAKET S2: Senkenlisten (alle Ränge, beide Prozess-Ziele) und Klassen-Sets.
            m.SenkenlistenLesen(idProjekt, anlagen);
            m.KlassenSetsLesen(idProjekt);
            m.KaelteerzeugerLesen(idProjekt, anlagen);

            // Quellpuffer je Anlage in der ANZEIGE-Auflösung (Karte und Schema gleich).
            //
            // OHNE Einschränkung auf die Wärmepumpe: Der HEIZKESSEL kann seit Etappe D5a
            // ebenso einen Pufferspeicher als Wärmequelle führen (Konzept 8.4), und die
            // Kette „… → Puffer → Kessel → …" wird hier deshalb genauso gezeichnet wie
            // bei der Wärmepumpe. Die Auswahl trifft Hydraulikbild.QuellpufferAnzeige
            // über WQ_Typ — sie kennt keinen Anlagentyp.
            Dictionary<int, int> quellpuffer = new Dictionary<int, int>();
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                int q = bild.QuellpufferAnzeige(a.ID, puffer);
                if (q > 0 && pufferJeId.ContainsKey(q)) quellpuffer[a.ID] = q;
            }

            // W10b-B-1: Die Kaskade ist eine Tatsache des Projekts, keine Ableitung aus
            // dem Band — sie steht hier fest, bevor eine Kette gesucht wird.
            m.HatKaskade = quellpuffer.Count > 0;

            m.SpeicherKnotenAnlegen(anlagen, puffer, quellpuffer, hatBrauchwasser, hatProzess,
                                    kanalBedarfMwh);
            m.ErzeugerKnotenAnlegen(bild, anlagen, pufferJeId, quellpuffer, rangJeTyp);
            m.KantenAnlegen(idProjekt, anlagen, quellpuffer, hatBrauchwasser, hatProzess);
            m.KettenAbleiten(anlagen, pufferJeId, quellpuffer, hatBrauchwasser);
            m.KaelteBahnAnlegen(idProjekt, anlagen, kanalBedarfMwh);

            return m;
        }

        /// <summary>
        /// true, wenn dem Projekt mindestens ein Prozesswärme-Anteil zugeordnet ist —
        /// das Gegenstück zu <c>WaermesenkeClass.ProjektHatBrauchwasser</c> und dieselbe
        /// Vorsichtsregel: Ist die Abfrage nicht auswertbar (fehlende Tabelle), gilt
        /// „vorhanden", damit kein Knoten aus Unkenntnis verschwindet.
        /// </summary>
        private static bool ProjektHatProzesswaerme(int idProjekt)
        {
            object v = StilleDb.Scalar(
                "SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            if (v == null) return true;
            return StilleDb.Zahl(v) > 0;
        }

        /// <summary>
        /// Liest die geordneten Senkenlisten des Projekts aus <c>Z_AnlageSenke</c>.
        ///
        /// <para><b>PAKET A1:</b> Der Rückfall auf die beiden Altslots aus
        /// <see cref="Hydraulikbild"/> ist entfallen — dieselbe Entscheidung wie in
        /// <c>WaermesenkeClass.SenkenlistenLaden</c>. Eine Anlage ohne Zeile bekommt die
        /// RANG-1-VORBELEGUNG <c>Heizkreis/Beides</c>, also genau das, was auch die Engine
        /// für sie rechnet; das Schema zeigt damit keinen Speicher mehr an, den kein Lauf
        /// mehr lädt.</para>
        /// </summary>
        private void SenkenlistenLesen(int idProjekt,
                                       List<Hydraulikbild.AnlagenEintrag> anlagen)
        {
            foreach (Z_AnlageSenkeModel z in new Z_AnlageSenkeCtrl().LesenJeProjekt(idProjekt))
            {
                if (z == null || z.ID_Anlage <= 0) continue;

                List<Z_AnlageSenkeModel> kette;
                if (!_senken.TryGetValue(z.ID_Anlage, out kette))
                {
                    kette = new List<Z_AnlageSenkeModel>();
                    _senken[z.ID_Anlage] = kette;
                }
                kette.Add(z);
            }

            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                if (_senken.ContainsKey(a.ID)) continue;

                List<Z_AnlageSenkeModel> kette = new List<Z_AnlageSenkeModel>();
                kette.Add(new Z_AnlageSenkeModel
                {
                    ID_Anlage = a.ID,
                    Rang = 1,
                    Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                    Bedarfsart = WaermequelleClass.SENKE_BEIDES
                });

                _senken[a.ID] = kette;
            }
        }

        /// <summary>Klassen-Set je Projekt-Puffer (Konzept 6.1) — EINE Abfrage.</summary>
        private void KlassenSetsLesen(int idProjekt)
        {
            foreach (KeyValuePair<int, PufferSpCtrl.KlassenSet> e in
                     PufferSpCtrl.KlassenSetsJeProjekt(idProjekt))
                _sets[e.Key] = e.Value;
        }

        /// <summary><c>Tab_Energieanlagen.ID_Type</c> zu einem Erzeuger-DB-Wert; 0 = unbekannt.</summary>
        private static int TypZuDbWert(string dbWert)
        {
            switch (dbWert)
            {
                case DbWerte.ERZEUGER_WAERMEPUMPE: return ProjektPuffer.TYP_WP;
                case DbWerte.ERZEUGER_HEIZKESSEL: return ProjektPuffer.TYP_KESSEL;
                case DbWerte.ERZEUGER_BHKW: return ProjektPuffer.TYP_BHKW;
                case DbWerte.ERZEUGER_SOLARTHERMIE: return ProjektPuffer.TYP_SOLARTHERMIE;
                default: return 0;
            }
        }

        // --- Knoten -------------------------------------------------------------------

        /// <summary>
        /// Speicherknoten: NUR Puffer, die im Schema wirklich vorkommen — geladen,
        /// als Quelle genutzt oder Zweitsenke.
        ///
        /// Der Filter ist nötig und nicht kosmetisch: Projekt 1023 der Arbeitskopie führt
        /// 79 Puffer-Zeilen, von denen genau EINE an der Hydraulik teilnimmt. Ein Schema
        /// mit 79 Kästen wäre unlesbar und würde 78 Speicher behaupten, die kein Erzeuger
        /// bedient.
        /// </summary>
        private void SpeicherKnotenAnlegen(List<Hydraulikbild.AnlagenEintrag> anlagen,
                                           List<WaermesenkeClass.PufferInfo> puffer,
                                           Dictionary<int, int> quellpuffer,
                                           bool hatBrauchwasser, bool hatProzess,
                                           double[] kanalBedarfMwh)
        {
            // PAKET S2: über ALLE Senkenzeilen, nicht mehr über die zwei Altslots — ein
            // Speicher, den erst Rang 3 lädt, fehlte bis hierher im Schema.
            HashSet<int> beteiligt = new HashSet<int>();
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
                foreach (Z_AnlageSenkeModel z in Senken(a.ID))
                    if (z != null && z.ID_Puffer > 0 && WaermesenkeClass.IstPufferZiel(z.Ziel))
                        beteiligt.Add(z.ID_Puffer);

            foreach (KeyValuePair<int, int> q in quellpuffer) beteiligt.Add(q.Value);

            foreach (WaermesenkeClass.PufferInfo p in puffer)
            {
                if (p == null || !beteiligt.Contains(p.ID)) continue;

                Knoten k = new Knoten();
                k.Schluessel = PRAEFIX_SPEICHER + p.ID;
                k.Art = Knotenart.Speicher;
                k.ID = p.ID;
                k.Titel = p.Bezeichner.Length > 0
                    ? p.Bezeichner : MyResource.Resource.PSP_BEZEICHNER_ERSATZ;

                if (p.Gesamtvolumen > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_VOLUMEN, p.Gesamtvolumen));
                if (p.Vorlauf > 0 && p.Ruecklauf > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                               p.Vorlauf, p.Ruecklauf));

                // PAKET S2: EIN BADGE JE KANAL DES KLASSEN-SETS (Konzept 10) statt eines
                // Badges je Verwendung. Ein Kombispeicher trägt damit weiterhin beide
                // Badges (Mockup „Puffer Kombi"), ein {H, P}-Speicher jetzt aber auch
                // seine Prozess-Kennzeichnung - die Verwendung kennt sie nicht.
                if (PufferBedient(p.ID, Kanal.HEIZUNG))
                    k.Badges.Add(MyResource.Resource.PSP_VERWENDUNG_HEIZUNG_ANZEIGE);
                if (PufferBedient(p.ID, Kanal.BRAUCHWASSER))
                    k.Badges.Add(MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER);
                if (PufferBedient(p.ID, Kanal.PROZESS))
                    k.Badges.Add(MyResource.Resource.KANAL_PROZESS_ANZEIGE);

                PufferSpCtrl.KlassenSet set;
                _sets.TryGetValue(p.ID, out set);
                k.Hinweis = string.Format(MyResource.Resource.PSP_KARTE_VERSORGT,
                                          Warnkriterien.KlassenSetAnzeige(set));
                Knotenliste.Add(k);
            }

            // Abnehmerknoten: die, die auch bedient werden (siehe KantenAnlegen) — und,
            // MIT WARNZEICHEN, der Abnehmer eines Kanals mit Bedarf, den keine Senke
            // bedient. Bis hierher verschwand er still aus dem Bild, und mit ihm die
            // Auskunft, dass sein Bedarf ungedeckt bleibt.
            if (BedientKanal(anlagen, Kanal.HEIZUNG))
                Knotenliste.Add(new Knoten
                {
                    Schluessel = ABNEHMER_HEIZKREIS,
                    Art = Knotenart.Abnehmer,
                    Titel = MyResource.Resource.SIM_HEIZKREIS,
                    Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER
                });
            else
                OhneVersorgerAnlegen(ABNEHMER_HEIZKREIS, MyResource.Resource.SIM_HEIZKREIS,
                                     Kanal.HEIZUNG, false, kanalBedarfMwh);

            bool brauchwasserBedient = BedientKanal(anlagen, Kanal.BRAUCHWASSER);
            if (hatBrauchwasser && brauchwasserBedient)
                Knotenliste.Add(new Knoten
                {
                    Schluessel = ABNEHMER_WARMWASSER,
                    Art = Knotenart.Abnehmer,
                    Titel = MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER,
                    Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER
                });
            else if (!brauchwasserBedient)
                OhneVersorgerAnlegen(ABNEHMER_WARMWASSER,
                                     MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER,
                                     Kanal.BRAUCHWASSER, hatBrauchwasser, kanalBedarfMwh);

            bool prozessBedient = BedientKanal(anlagen, Kanal.PROZESS);
            if (hatProzess && prozessBedient)
                Knotenliste.Add(new Knoten
                {
                    Schluessel = ABNEHMER_PROZESS,
                    Art = Knotenart.Abnehmer,
                    Titel = MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                    Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER
                });
            else if (!prozessBedient)
                OhneVersorgerAnlegen(ABNEHMER_PROZESS, MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                                     Kanal.PROZESS, hatProzess, kanalBedarfMwh);

            // Der Kältekreis steht seit Auftrag KS in der Kältebahn (KaelteBahnAnlegen).
        }

        /// <summary>
        /// Anlagen, die im Schema die Kälte bedienen (Stufe KU2): die Wärmepumpen des Projekts,
        /// deren Gerät auf Kühlbetrieb steht — nur, wenn das Projekt Kälte rechnet
        /// (<c>Tab_Einstellungen.Kuehlbetrieb</c>). Dieselbe Auswahl, mit der der Lauf seine
        /// Kälteerzeuger beginnt; die Sperrgründe prüft erst der Lauf.
        /// </summary>
        private readonly HashSet<int> _kaelteerzeuger = new HashSet<int>();

        private void KaelteerzeugerLesen(int idProjekt, List<Hydraulikbild.AnlagenEintrag> anlagen)
        {
            _kaelteerzeuger.Clear();
            if (!KonfigurationCtrl.KuehlbetriebLesen(idProjekt)) return;

            DataTable dt = StilleDb.Tabelle(
                "SELECT a.ID FROM Tab_Energieanlagen a JOIN Tab_WP w ON w.ID = a.ID_WP " +
                "WHERE a.ID_Projekt = ? AND a.ID_Type = ? AND w.Kuehlbetrieb = 1",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@typ", DbParamTyp.Integer, WizardItemClass.WP_TYP));
            if (dt == null) return;

            HashSet<int> gezeichnet = new HashSet<int>(anlagen.Select(a => a.ID));
            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (gezeichnet.Contains(id)) _kaelteerzeuger.Add(id);
            }
        }

        // --- Auftrag KS: die Kältebahn -------------------------------------------------

        /// <summary>
        /// Legt die KÄLTEBAHN an (Auftrag KS) — vier Spalten unter der Wärmebahn, mit derselben
        /// Kantensprache: Rückkühlung bzw. Quelle → Kälteerzeuger (Quellseite), Kälteerzeuger →
        /// Kältespeicher (Ladung, Kreis = Platz in der Kälte-Kaskade), Kältespeicher → Kältekreis
        /// (Versorgung), ohne Kältespeicher Kälteerzeuger → Kältekreis.
        ///
        /// <para><b>Wer Kälte erzeugt</b>, entscheidet dieselbe Auswahl wie im Lauf: die
        /// Wärmepumpen mit Kühlbetrieb am Gerät, solange das Projekt Kälte rechnet
        /// (<see cref="KaelteerzeugerLesen"/>), und jede Kältemaschinen-Anlage
        /// (<see cref="KaeltemaschineAnlageCtrl.ListeStill"/>). Eine Kältemaschine steht auch
        /// bei ausgeschaltetem Projektschalter da — sie ist ein Kälteerzeuger von Natur —, der
        /// Kältekreis sagt dann, dass das Projekt keine Kälte rechnet. Kältespeicher sind die
        /// Projektpuffer mit der Verwendung Kälte; sie haben keine Senkenzeile, denn alle
        /// Kälteerzeuger laden den einen Kältekanal (<c>KaeltespeicherLesen</c> im Lauf).</para>
        ///
        /// <para>Ohne Kälteerzeuger, Kältespeicher und Kältebedarf entsteht kein Knoten, und das
        /// Schema bleibt, wie es ohne Kälte war.</para>
        /// </summary>
        private void KaelteBahnAnlegen(int idProjekt, List<Hydraulikbild.AnlagenEintrag> anlagen,
                                       double[] kanalBedarfMwh)
        {
            bool projektKaelte = KonfigurationCtrl.KuehlbetriebLesen(idProjekt);
            List<string> erzeuger = new List<string>();

            // 1. Wärmepumpen im Kühlbetrieb - in der Folge ihrer Module (KB-A: die Regel der Kaeltefolge).
            List<int> module = SimulationControl.WaermepumpenanlagenLesen(idProjekt) ?? new List<int>();
            List<Hydraulikbild.AnlagenEintrag> wpKaelte = Kaeltefolge.ErzeugerOrdnen(
                anlagen.Where(a => a != null && _kaelteerzeuger.Contains(a.ID)),
                a => KaeltefolgeStufe.Waermepumpe,
                a => module.IndexOf(a.ID) >= 0 ? module.IndexOf(a.ID) : int.MaxValue);
            foreach (Hydraulikbild.AnlagenEintrag a in wpKaelte)
            {

                Knoten k = new Knoten
                {
                    Schluessel = PRAEFIX_KAELTE_ERZEUGER + a.ID,
                    Art = Knotenart.Erzeuger,
                    Bahn = Bahn.Kaelte,
                    ID = a.ID,
                    ID_Type = a.ID_Type,
                    Titel = a.Bezeichner.Length > 0 ? a.Bezeichner : Ladeordnung.ErzeugerName(a.ID_Type)
                };
                k.Zeilen.Add(MyResource.Resource.KONF_KS_WP_KUEHLBETRIEB);
                double? vorlauf = WpKuehlVorlauf(a.ID);
                if (vorlauf.HasValue)
                    k.Zeilen.Add(string.Format(MyResource.Resource.KONF_KS_KUEHLVORLAUF, vorlauf.Value));
                k.Hinweis = string.Join(Environment.NewLine, k.Zeilen.ToArray());
                Knotenliste.Add(k);
                erzeuger.Add(k.Schluessel);
                KaelteKette.Add(k.Titel);

                // Die Quelle der Wärmepumpe nimmt im Kühlbetrieb die Abwärme auf.
                string quelle = Quelltext(a);
                if (quelle.Length > 0)
                    Knotenliste.Add(new Knoten
                    {
                        Schluessel = PRAEFIX_KAELTE_QUELLE + a.ID,
                        Art = Knotenart.Quelle,
                        Bahn = Bahn.Kaelte,
                        ID = a.ID,
                        ID_Type = a.ID_Type,
                        Titel = quelle,
                        Hinweis = string.Format(MyResource.Resource.KONF_KS_ABWAERME, quelle)
                    });
            }

            // 2. Kältemaschinen - in der Folge ihrer Anlagenzeilen.
            foreach (KaeltemaschineAnlageModel a in Kaeltefolge.KaeltemaschinenOrdnen(KaeltemaschineAnlageCtrl.ListeStill(idProjekt)))
            {
                if (a == null || a.AnlagenId <= 0) continue;

                KaeltemaschineModel m = a.IdKaeltemaschine.HasValue
                    ? KaeltemaschineCtrl.LadenStill(a.IdKaeltemaschine.Value) : null;

                Knoten k = new Knoten
                {
                    Schluessel = PRAEFIX_KAELTE_ERZEUGER + a.AnlagenId,
                    Art = Knotenart.Erzeuger,
                    Bahn = Bahn.Kaelte,
                    ID = a.AnlagenId,
                    ID_Type = WizardItemClass.KM_TYP,
                    Titel = !string.IsNullOrWhiteSpace(a.Bezeichner) ? a.Bezeichner
                          : m != null && !string.IsNullOrWhiteSpace(m.Bezeichner) ? m.Bezeichner
                          : MyResource.Resource.KONF_KS_KAELTEMASCHINE
                };
                k.Zeilen.Add(MyResource.Resource.KONF_KS_KAELTEMASCHINE);
                if (m != null && m.Nennkaelteleistung_kW.HasValue && m.Nennkaelteleistung_kW.Value > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.KONF_KS_ANZAHL_LEISTUNG,
                                               Math.Max(1, a.Anzahl), m.Nennkaelteleistung_kW.Value));
                double vorlauf = m != null ? Kaltwasservorlauf(m) : double.NaN;
                if (!double.IsNaN(vorlauf))
                    k.Zeilen.Add(string.Format(MyResource.Resource.KONF_KS_KALTWASSERVORLAUF, vorlauf));
                k.Hinweis = string.Join(Environment.NewLine, k.Zeilen.ToArray());
                Knotenliste.Add(k);
                erzeuger.Add(k.Schluessel);
                KaelteKette.Add(k.Titel);

                string rueckkuehlung = m != null ? KaeltemaschineStammCtrl.RueckkuehlartText(m.Rueckkuehlart) : "";
                if (string.IsNullOrEmpty(rueckkuehlung)) rueckkuehlung = MyResource.Resource.KM_RUECKKUEHLART_KEINE;
                Knotenliste.Add(new Knoten
                {
                    Schluessel = PRAEFIX_KAELTE_QUELLE + a.AnlagenId,
                    Art = Knotenart.Quelle,
                    Bahn = Bahn.Kaelte,
                    ID = a.AnlagenId,
                    ID_Type = WizardItemClass.KM_TYP,
                    Titel = rueckkuehlung,
                    Hinweis = string.Format(MyResource.Resource.KONF_KS_RUECKKUEHLUNG, rueckkuehlung)
                });
            }

            // 3. Kältespeicher - in der Reihenfolge des Laufs (Entladepriorität, 0 hinten).
            List<WaermesenkeClass.PufferInfo> speicher = Kaeltefolge.KaeltespeicherOrdnen(
                (WaermesenkeClass.ProjektPufferListe(idProjekt, WaermesenkeClass.VERWENDUNG_KAELTE)
                 ?? new List<WaermesenkeClass.PufferInfo>()).Where(p => p != null),
                p => p.Entladeprio);

            foreach (WaermesenkeClass.PufferInfo p in speicher)
            {
                Knoten k = new Knoten
                {
                    Schluessel = PRAEFIX_KAELTE_SPEICHER + p.ID,
                    Art = Knotenart.Speicher,
                    Bahn = Bahn.Kaelte,
                    ID = p.ID,
                    Titel = p.Bezeichner.Length > 0 ? p.Bezeichner : MyResource.Resource.PSP_BEZEICHNER_ERSATZ
                };
                if (p.Gesamtvolumen > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_VOLUMEN, p.Gesamtvolumen));
                if (p.Vorlauf > 0 && p.Ruecklauf > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR, p.Vorlauf, p.Ruecklauf));
                k.Badges.Add(MyResource.Resource.KONF_KS_KAELTE);
                k.Hinweis = string.Join(Environment.NewLine, k.Zeilen.ToArray());
                Knotenliste.Add(k);
            }

            // 4. Der Kältekreis - bedient, sonst mit Warnzeichen, wenn der Lauf Kältebedarf kennt.
            if (erzeuger.Count > 0 || speicher.Count > 0)
            {
                Knoten k = new Knoten
                {
                    Schluessel = ABNEHMER_KAELTEKREIS,
                    Art = Knotenart.Abnehmer,
                    Bahn = Bahn.Kaelte,
                    Titel = MyResource.Resource.SIM_ZIEL_KAELTEKREIS,
                    Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER
                };
                KuehluebergabeZeilen(idProjekt, k.Zeilen);
                if (!projektKaelte) k.Zeilen.Add(MyResource.Resource.KONF_KS_PROJEKT_AUS);
                if (k.Zeilen.Count > 0)
                    k.Hinweis = k.Hinweis + Environment.NewLine + string.Join(Environment.NewLine, k.Zeilen.ToArray());
                Knotenliste.Add(k);
            }
            else
            {
                OhneVersorgerAnlegen(ABNEHMER_KAELTEKREIS, MyResource.Resource.SIM_ZIEL_KAELTEKREIS,
                                     Kanal.KUEHLUNG, false, kanalBedarfMwh);
                Knoten ohne = Finden(ABNEHMER_KAELTEKREIS);
                if (ohne != null) ohne.Bahn = Bahn.Kaelte;
            }

            // 5. Kanten - dieselbe Sprache wie in der Wärmebahn.
            for (int i = 0; i < erzeuger.Count; i++)
            {
                Knoten e = Finden(erzeuger[i]);
                Verbinden(PRAEFIX_KAELTE_QUELLE + e.ID, e.Schluessel, Kantenart.Quelle, 0, "");

                if (speicher.Count == 0)
                {
                    Verbinden(e.Schluessel, ABNEHMER_KAELTEKREIS, Kantenart.Versorgung, 0, "");
                    continue;
                }

                string platz = string.Format(MyResource.Resource.KONF_KS_PLATZ, i + 1, erzeuger.Count);
                foreach (WaermesenkeClass.PufferInfo p in speicher)
                    Verbinden(e.Schluessel, PRAEFIX_KAELTE_SPEICHER + p.ID, Kantenart.Ladung, i + 1, platz);
            }

            foreach (WaermesenkeClass.PufferInfo p in speicher)
                Verbinden(PRAEFIX_KAELTE_SPEICHER + p.ID, ABNEHMER_KAELTEKREIS, Kantenart.Versorgung, 0, "");

            // Der Platz in der Kälte-Kaskade steht auch am Kasten (wie der Kaskadenrang der Wärme).
            if (erzeuger.Count > 1)
                for (int i = 0; i < erzeuger.Count; i++)
                    Finden(erzeuger[i]).Rang = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Kühlvorlauf des Projektgeräts einer Wärmepumpen-Anlage [°C]; <c>null</c> = nicht gepflegt.</summary>
        private static double? WpKuehlVorlauf(int idAnlage)
        {
            object v = StilleDb.Scalar(
                "SELECT w.Kuehl_Vorlauf FROM Tab_Energieanlagen a JOIN Tab_WP w ON w.ID = a.ID_WP WHERE a.ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idAnlage));
            if (v == null || v is DBNull) return null;
            double d = StilleDb.Kommazahl(v, double.NaN);
            return double.IsNaN(d) || d <= 0 ? (double?)null : d;
        }

        /// <summary>
        /// Der Kaltwasservorlauf einer Kältemaschine [°C] — derselbe Wert, mit dem der Lauf sie
        /// baut (<see cref="Kaeltemaschine.AusModell"/>); NaN, wenn er nicht bestimmbar ist.
        /// </summary>
        private static double Kaltwasservorlauf(KaeltemaschineModel m)
        {
            try
            {
                double kw = Kaeltemaschine.AusModell(m, out bool _).Kaltwassertemperatur;
                return double.IsNaN(kw) || double.IsInfinity(kw) ? double.NaN : kw;
            }
            catch (Exception) { return double.NaN; }
        }

        /// <summary>
        /// Die Kühlübergabe der Gebäude des Projekts als Zeilen des Kältekreises: je Übergabeart
        /// eine Zeile (Anzeigename wie im Gebäudedialog), dazu die Zahl der Zonen, wenn ein
        /// Gebäude mit Kühlübergabe in mehr als einer Zone rechnet.
        /// </summary>
        private static void KuehluebergabeZeilen(int idProjekt, List<string> zeilen)
        {
            DataTable dt = StilleDb.Tabelle(
                "SELECT g.Kuehl_Uebergabe_Art, " +
                "(SELECT COUNT(*) FROM Tab_Zone z WHERE z.ID_Gebaeude = g.ID) AS Zonen " +
                "FROM Tab_Gebaeude g WHERE g.ID_Projekt = ? AND g.Kuehluebergabe_Aktiv = 1 ORDER BY g.ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null) return;

            int zonen = 0;
            HashSet<string> arten = new HashSet<string>(StringComparer.Ordinal);
            foreach (DataRow r in dt.Rows)
            {
                object art = StilleDb.Feld(r, "Kuehl_Uebergabe_Art");
                string name = Waermeuebergabevorgaben.KuehlAnzeigename(art == null || art is DBNull ? null : art.ToString());
                if (!string.IsNullOrEmpty(name) && arten.Add(name))
                    zeilen.Add(string.Format(MyResource.Resource.KONF_KS_KUEHLUEBERGABE, name));

                int n = StilleDb.Zahl(StilleDb.Feld(r, "Zonen"));
                if (n > 1) zonen += n;
            }

            if (zonen > 0) zeilen.Add(string.Format(MyResource.Resource.KONF_KS_ZONEN, zonen));
        }

        /// <summary>
        /// Der Abnehmerknoten eines Kanals OHNE Versorger — nur, wenn der Kanal Bedarf
        /// hat: nach dem gerechneten Kanalbedarf, sonst nach
        /// <paramref name="bedarfOhneMenge"/> (Profil zugeordnet). Er trägt das
        /// Warnzeichen und als Warntext denselben Satz wie Laufprotokoll und
        /// Ergebnisübersicht (<see cref="Warnkriterien.KanalOhneVersorgerText"/>).
        /// Kanten bekommt er keine — es gibt keine Senke, aus der eine käme.
        /// </summary>
        private void OhneVersorgerAnlegen(string schluessel, string titel, int kanal,
                                          bool bedarfOhneMenge, double[] kanalBedarfMwh)
        {
            double? mwh = null;
            bool bedarf = bedarfOhneMenge;

            if (kanalBedarfMwh != null && kanal < kanalBedarfMwh.Length)
            {
                mwh = kanalBedarfMwh[kanal];
                bedarf = mwh.Value >= Warnkriterien.KANAL_BEDARF_SCHWELLE_MWH;
            }

            if (!bedarf) return;

            Knotenliste.Add(new Knoten
            {
                Schluessel = schluessel,
                Art = Knotenart.Abnehmer,
                Titel = titel,
                Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER,
                Warnung = true,
                Warntext = Warnkriterien.KanalOhneVersorgerText(kanal, mwh)
            });
        }

        /// <summary>
        /// Wird dieser KANAL im Projekt überhaupt bedient — unmittelbar durch eine
        /// Direktsenke oder mittelbar über einen Speicher, dessen Klassen-Set ihn führt?
        ///
        /// <para>PAKET S2: Eine Regel für alle drei Kanäle statt zweier Sonderfassungen
        /// für Heizkreis und Warmwasser. Die Aussage ist für Heizung und Warmwasser
        /// dieselbe wie zuvor — nur dass sie jetzt über ALLE Senkenränge läuft und den
        /// Speicher über sein Klassen-Set statt über die Alt-Verwendung fragt.</para>
        /// </summary>
        private bool BedientKanal(List<Hydraulikbild.AnlagenEintrag> anlagen, int kanal)
        {
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
                foreach (Z_AnlageSenkeModel z in Senken(a.ID))
                {
                    if (z == null) continue;

                    if (WaermesenkeClass.IstPufferZiel(z.Ziel))
                    {
                        if (PufferBedient(z.ID_Puffer, kanal)) return true;
                    }
                    else if (DirektsenkeBedient(z, kanal)) return true;
                }

            return false;
        }

        /// <summary>
        /// Erzeugerknoten und — sofern die Quelle KEIN Puffer ist — der zugehörige
        /// Quellknoten. Bei Puffer-Quelle entsteht kein eigener Quellkasten: Die Kaskade
        /// kommt sichtbar aus dem Speicher (Invariante S-1, Mockup „aus Puffer Heizung").
        /// </summary>
        private void ErzeugerKnotenAnlegen(Hydraulikbild bild,
                                           List<Hydraulikbild.AnlagenEintrag> anlagen,
                                           Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                           Dictionary<int, int> quellpuffer,
                                           Dictionary<int, int> rangJeTyp)
        {
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                Knoten k = new Knoten();
                k.Schluessel = PRAEFIX_ERZEUGER + a.ID;
                k.Art = Knotenart.Erzeuger;
                k.ID = a.ID;
                k.ID_Type = a.ID_Type;
                k.Titel = a.Bezeichner.Length > 0 ? a.Bezeichner : Ladeordnung.ErzeugerName(a.ID_Type);

                int rang;
                if (rangJeTyp.TryGetValue(a.ID_Type, out rang)) k.Rang = rang.ToString();

                k.Zeilen.Add(Ladeordnung.ErzeugerName(a.ID_Type));
                if (a.Vorlauf > 0 && a.Ruecklauf > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                               a.Vorlauf, a.Ruecklauf));
                // PAKET A1: Ziel und Zielspeicher kommen aus der SENKENLISTE (Rang 1) und
                // nicht mehr aus der gespiegelten Altspaltensicht — nur so stehen auch die
                // beiden Prozess-Ziele richtig da (sie spiegelten sich als „Heizung").
                Z_AnlageSenkeModel rang1 = SenkeAufRang(a.ID, 0);
                k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_SENKE,
                                           WaermesenkeClass.SenkeAnzeige(rang1)));

                // Temperatur-Warnregel (Konzept Abschnitt 5), Wort für Wort die Regel der
                // Erzeugerkarte: Erzeuger-Vorlauf < Puffer-Vorlauf der Hauptsenke.
                WaermesenkeClass.PufferInfo senke = null;
                if (rang1 != null && WaermesenkeClass.IstPufferZiel(rang1.Ziel) && rang1.ID_Puffer > 0)
                    pufferJeId.TryGetValue(rang1.ID_Puffer, out senke);

                if (senke != null && senke.Vorlauf > 0 && a.Vorlauf > 0 && a.Vorlauf < senke.Vorlauf)
                {
                    k.Warnung = true;
                    k.Warntext = string.Format(
                        MyResource.Resource.SIM_KARTE_TIP_TEMPERATUR_WARNUNG,
                        a.Vorlauf, senke.Bezeichner, senke.Vorlauf);
                }

                int idQuelle;
                bool kaskade = quellpuffer.TryGetValue(a.ID, out idQuelle);
                k.Kaskade = kaskade;
                if (kaskade)
                    k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_QUELLE_KASKADE,
                                               PufferTitel(idQuelle, pufferJeId)));

                k.Hinweis = string.Join(Environment.NewLine, k.Zeilen.ToArray());
                Knotenliste.Add(k);

                if (kaskade) continue;   // die Quelle ist der Speicherknoten

                string quelltext = Quelltext(a);
                if (quelltext.Length == 0) continue;

                Knotenliste.Add(new Knoten
                {
                    Schluessel = PRAEFIX_QUELLE + a.ID,
                    Art = Knotenart.Quelle,
                    ID = a.ID,
                    ID_Type = a.ID_Type,
                    Titel = quelltext,
                    Hinweis = string.Format(MyResource.Resource.SIM_KARTE_QUELLE, quelltext)
                });
            }
        }

        /// <summary>
        /// Text des Quellkastens je Erzeugerart.
        ///
        /// Für die Wärmepumpe ist das WORTGLEICH die Quellenanzeige der Karte
        /// (<see cref="WaermequelleClass.QuelleAnzeige"/>) — Liste und Schema dürfen die
        /// Quelle nicht verschieden benennen. Die übrigen Arten haben keine wählbare
        /// Quelle (Konzept Anforderung 5); für sie steht dort, woher die Wärme physikalisch
        /// kommt: Solarstrahlung, Brennstoff, Systemrücklauf.
        /// </summary>
        private string Quelltext(Hydraulikbild.AnlagenEintrag a)
        {
            switch (a.ID_Type)
            {
                case ProjektPuffer.TYP_WP:
                    return WaermequelleClass.QuelleAnzeige(ID_Projekt, a.ID, a.WpTyp, a.WQ_Typ, a.WQ_Temp);

                case ProjektPuffer.TYP_SOLARTHERMIE:
                    return MyResource.Resource.SIM_SCHEMA_QUELLE_SOLARSTRAHLUNG;

                case ProjektPuffer.TYP_KESSEL:
                    return MyResource.Resource.SIMQ_QUELLE_SYSTEMRUECKLAUF;

                case ProjektPuffer.TYP_BHKW:
                    return MyResource.Resource.SIM_SCHEMA_QUELLE_BRENNSTOFF;

                default:
                    return "";
            }
        }

        private static string PufferTitel(int idPuffer,
                                          Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId)
        {
            WaermesenkeClass.PufferInfo p;
            if (idPuffer > 0 && pufferJeId.TryGetValue(idPuffer, out p) && p.Bezeichner.Length > 0)
                return p.Bezeichner;
            return MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER;
        }

        // --- Kanten -------------------------------------------------------------------

        private void KantenAnlegen(int idProjekt,
                                   List<Hydraulikbild.AnlagenEintrag> anlagen,
                                   Dictionary<int, int> quellpuffer,
                                   bool hatBrauchwasser, bool hatProzess)
        {
            // Ladepositionen EINMAL je beteiligtem Puffer holen - Ladereihenfolge fragt je
            // Aufruf Anlagen und Kaskadenplätze neu ab (Begründung wie in
            // Form_Simulation_Config.SpeicherKarteDaten).
            //
            // PAKET A1: Die Ladeordnung liest die Senkenlisten (ab Rang 3 trägt die Kante
            // erst dadurch ihre Kreisziffer, S2-O6). Sie werden hier EINMAL still geholt
            // und in jeden Aufruf hineingereicht - sonst läse jeder Speicherknoten sie
            // erneut.
            List<Senkenliste> senken = WaermesenkeClass.SenkenlistenLadenStill(idProjekt);

            Dictionary<int, List<Ladeordnung.LadeEintrag>> ordnung =
                new Dictionary<int, List<Ladeordnung.LadeEintrag>>();

            foreach (Knoten k in Knotenliste)
            {
                if (k.Art != Knotenart.Speicher || ordnung.ContainsKey(k.ID)) continue;
                ordnung[k.ID] = Ladeordnung.Ladereihenfolge(idProjekt, k.ID, senken);
            }

            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                string erzeuger = PRAEFIX_ERZEUGER + a.ID;

                // Quellseite
                int idQuelle;
                if (quellpuffer.TryGetValue(a.ID, out idQuelle))
                    Verbinden(PRAEFIX_SPEICHER + idQuelle, erzeuger, Kantenart.Kaskade, 0,
                              MyResource.Resource.SIM_KARTE_TIP_KASKADE);
                else
                    Verbinden(PRAEFIX_QUELLE + a.ID, erzeuger, Kantenart.Quelle, 0, "");

                // PAKET S2: die SENKENKETTE statt Haupt-/Zweitsenke — jede Zeile bekommt
                // ihre Kante. Rang 1 zeichnet wie bisher die Hauptsenke, alles darüber
                // gilt als Zweitsenke im Sinn der Ladeordnung (dort trennt ein einzelnes
                // Kennzeichen nur „erste" von „weitere").
                List<Z_AnlageSenkeModel> kette = Senken(a.ID);
                for (int i = 0; i < kette.Count; i++)
                {
                    Z_AnlageSenkeModel z = kette[i];
                    if (z == null) continue;

                    if (WaermesenkeClass.IstPufferZiel(z.Ziel) && z.ID_Puffer > 0)
                        LadekanteAnlegen(erzeuger, z.ID_Puffer, a.ID, i > 0, ordnung);
                    else
                        DirektkanteAnlegen(erzeuger, z, hatBrauchwasser, hatProzess);
                }
            }

            // Versorgung: jeder Speicher bedient die Kanäle seines KLASSEN-SETS.
            foreach (Knoten k in Knotenliste)
            {
                if (k.Art != Knotenart.Speicher) continue;

                if (PufferBedient(k.ID, Kanal.HEIZUNG))
                    Verbinden(k.Schluessel, ABNEHMER_HEIZKREIS, Kantenart.Versorgung, 0, "");
                if (PufferBedient(k.ID, Kanal.BRAUCHWASSER))
                    Verbinden(k.Schluessel, ABNEHMER_WARMWASSER, Kantenart.Versorgung, 0, "");
                // PAKET E1 (S2-O7): eigene Kantenart für den Prozessabnehmer.
                if (PufferBedient(k.ID, Kanal.PROZESS))
                    Verbinden(k.Schluessel, ABNEHMER_PROZESS, Kantenart.Prozess, 0, "");
            }
        }

        private void LadekanteAnlegen(string erzeuger, int idPuffer, int idAnlage, bool zweitsenke,
                                      Dictionary<int, List<Ladeordnung.LadeEintrag>> ordnung)
        {
            List<Ladeordnung.LadeEintrag> liste;
            int position = 0;
            if (ordnung.TryGetValue(idPuffer, out liste) && liste != null)
                position = Ladeordnung.Position(liste, idAnlage, zweitsenke);

            string hinweis = "";
            if (position > 0 && liste != null)
                hinweis = string.Format(MyResource.Resource.SIM_POSITION_LAEDT_ALS,
                                        position, liste.Count);
            if (zweitsenke)
                hinweis = (hinweis.Length > 0 ? hinweis + " · " : "") +
                          MyResource.Resource.SIM_ROLLE_ZWEITSENKE;

            Verbinden(erzeuger, PRAEFIX_SPEICHER + idPuffer, Kantenart.Ladung, position, hinweis);
        }

        /// <summary>
        /// Direkte Deckung: Erzeuger → Abnehmer, je nach Ziel und Bedarfsart der
        /// Senkenzeile (PAKET S2 — das dritte Ziel <c>Prozesswaerme</c> ist dazugekommen).
        /// </summary>
        private void DirektkanteAnlegen(string erzeuger, Z_AnlageSenkeModel z,
                                        bool hatBrauchwasser, bool hatProzess)
        {
            if (DirektsenkeBedient(z, Kanal.HEIZUNG))
                Verbinden(erzeuger, ABNEHMER_HEIZKREIS, Kantenart.Versorgung, 0, "");

            if (hatBrauchwasser && DirektsenkeBedient(z, Kanal.BRAUCHWASSER))
                Verbinden(erzeuger, ABNEHMER_WARMWASSER, Kantenart.Versorgung, 0, "");

            // PAKET E1 (S2-O7): eigene Kantenart für den Prozessabnehmer.
            if (hatProzess && DirektsenkeBedient(z, Kanal.PROZESS))
                Verbinden(erzeuger, ABNEHMER_PROZESS, Kantenart.Prozess, 0, "");
        }

        /// <summary>
        /// Legt eine Kante an — aber nur, wenn BEIDE Knoten existieren. Ein Bezug auf
        /// einen Puffer, den es im Projekt nicht (mehr) gibt, darf keine Linie ins Leere
        /// zeichnen; die Karte weist denselben Fall als Rückfall auf den Heizkreis aus.
        /// </summary>
        private void Verbinden(string von, string nach, Kantenart art, int prio, string hinweis)
        {
            if (Finden(von) == null || Finden(nach) == null) return;

            foreach (Kante vorhanden in Kantenliste)
                if (string.Equals(vorhanden.Von, von, StringComparison.Ordinal) &&
                    string.Equals(vorhanden.Nach, nach, StringComparison.Ordinal) &&
                    vorhanden.Art == art)
                    return;

            Kantenliste.Add(new Kante
            {
                Von = von, Nach = nach, Art = art, Prioritaet = prio, Hinweis = hinweis ?? ""
            });
        }

        // --- Kaskadenkette ------------------------------------------------------------

        /// <summary>Höchstzahl gezeigter Ketten — mehr wäre kein Band mehr, sondern eine Liste.</summary>
        private const int MAX_KETTEN = 6;

        /// <summary>
        /// Leitet die Kaskadenketten ab (Konzept Abschnitt 3, Mockup Abschnitt 2):
        /// „Erdsonde → WP 1 → Puffer 1 → WP 2 Booster → Puffer 2 → Warmwasser".
        ///
        /// Eine Kette beginnt bei einem Erzeuger OHNE Quellpuffer, der — auf IRGENDEINEM
        /// Rang seiner Senkenkette — einen Speicher lädt, aus dem ein anderer Erzeuger
        /// seine Quellwärme bezieht; sie folgt dem Weg Erzeuger → Speicher → Erzeuger → …
        /// bis zum letzten Speicher und endet beim Abnehmer. Zwischen zwei Speichern steht
        /// damit immer ein Erzeuger — genau die Darstellungsvorgabe der Invariante S-1.
        ///
        /// Findet sich zu einem Quellpuffer kein solcher Erzeuger, beginnt die Kette beim
        /// SPEICHER. Damit gilt <c>HatKaskade ⇔ Ketten.Count &gt; 0</c>, und das Band sagt
        /// dasselbe wie die Erzeugerkarten (Anwenderbefund W10b-B-1).
        ///
        /// Ohne Quellbezug im Projekt entsteht keine Kette (das ist der Regelfall).
        /// </summary>
        private void KettenAbleiten(List<Hydraulikbild.AnlagenEintrag> anlagen,
                                    Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                    Dictionary<int, int> quellpuffer,
                                    bool hatBrauchwasser)
        {
            if (quellpuffer.Count == 0) return;

            // Puffer -> Erzeuger, die ihn als Quelle nutzen.
            Dictionary<int, List<Hydraulikbild.AnlagenEintrag>> nutzer =
                new Dictionary<int, List<Hydraulikbild.AnlagenEintrag>>();
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                int q;
                if (!quellpuffer.TryGetValue(a.ID, out q)) continue;
                if (!nutzer.ContainsKey(q)) nutzer[q] = new List<Hydraulikbild.AnlagenEintrag>();
                nutzer[q].Add(a);
            }

            // W10b-B-1: Die Puffer, an denen schon eine Kette hängt.
            HashSet<int> begonnen = new HashSet<int>();

            foreach (Hydraulikbild.AnlagenEintrag start in anlagen)
            {
                if (quellpuffer.ContainsKey(start.ID)) continue;          // kein Kettenanfang

                // W10b-B-1: ÜBER ALLE RÄNGE statt nur über Rang 1. Ein Erzeuger, der den
                // Quellpuffer eines anderen auf Rang 2 lädt, ist genauso ein Kettenanfang
                // wie einer, der ihn auf Rang 1 lädt — die Kaskade hängt am Quellbezug,
                // nicht an der Ladeposition (Projekt 1042 der Testdatenbank).
                int idPuffer = KettenPuffer(start, nutzer);
                if (idPuffer <= 0) continue;

                begonnen.Add(idPuffer);

                List<Kettenglied> kette = new List<Kettenglied>();

                string quelltext = Quelltext(start);
                if (quelltext.Length > 0)
                    kette.Add(new Kettenglied
                    {
                        Schluessel = PRAEFIX_QUELLE + start.ID,
                        Text = quelltext,
                        Art = Knotenart.Quelle
                    });

                kette.Add(Glied(start, Kantenart.Quelle));
                KetteFortsetzen(kette, idPuffer, nutzer, pufferJeId, quellpuffer,
                                hatBrauchwasser, new HashSet<int>());
            }

            // W10b-B-1: Ein Quellpuffer, den KEIN kettenfähiger Erzeuger lädt — weil sein
            // Lader selbst aus einem Puffer speist oder weil er als Art gar nicht
            // aufgenommen ist —, bekommt trotzdem seine Kette; sie beginnt dann beim
            // Speicher. Erst damit gilt: Kaskade im Projekt ⇔ Band im Bild.
            List<int> offen = new List<int>(nutzer.Keys);
            offen.Sort();                        // damit zwei Läufe dieselbe Reihenfolge geben

            foreach (int idPuffer in offen)
            {
                if (begonnen.Contains(idPuffer)) continue;

                KetteFortsetzen(new List<Kettenglied>(), idPuffer, nutzer, pufferJeId,
                                quellpuffer, hatBrauchwasser, new HashSet<int>());
            }
        }

        /// <summary>
        /// Der erste Puffer der Senkenkette einer Anlage, aus dem ein ANDERER Erzeuger
        /// seine Wärme bezieht — der Puffer also, an dem die Kaskade weitergeht;
        /// 0, wenn die Anlage keinen solchen lädt.
        /// </summary>
        private int KettenPuffer(Hydraulikbild.AnlagenEintrag a,
                                 Dictionary<int, List<Hydraulikbild.AnlagenEintrag>> nutzer)
        {
            foreach (Z_AnlageSenkeModel z in Senken(a.ID))
            {
                if (z == null || z.ID_Puffer <= 0) continue;
                if (!WaermesenkeClass.IstPufferZiel(z.Ziel)) continue;
                if (nutzer.ContainsKey(z.ID_Puffer)) return z.ID_Puffer;
            }

            return 0;
        }

        private void KetteFortsetzen(List<Kettenglied> kette, int idPuffer,
                                     Dictionary<int, List<Hydraulikbild.AnlagenEintrag>> nutzer,
                                     Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                     Dictionary<int, int> quellpuffer,
                                     bool hatBrauchwasser,
                                     HashSet<int> besucht)
        {
            if (Ketten.Count >= MAX_KETTEN) return;

            // Ringschutz: derselbe Speicher darf in einer Kette nur einmal vorkommen. Die
            // Engine bricht bei einem Ring ab und der Dialog verhindert ihn (D5b) - hier
            // geht es allein darum, dass die ANZEIGE eines Altbestands nicht endlos läuft.
            if (!besucht.Add(idPuffer)) { Ketten.Add(kette); return; }

            kette.Add(new Kettenglied
            {
                Schluessel = PRAEFIX_SPEICHER + idPuffer,
                Text = PufferTitel(idPuffer, pufferJeId),
                Art = Knotenart.Speicher,
                PfeilDavor = Kantenart.Ladung
            });

            List<Hydraulikbild.AnlagenEintrag> folger;
            if (!nutzer.TryGetValue(idPuffer, out folger) || folger.Count == 0)
            {
                KetteAbschliessen(kette, idPuffer, pufferJeId, hatBrauchwasser);
                return;
            }

            for (int i = 0; i < folger.Count; i++)
            {
                Hydraulikbild.AnlagenEintrag b = folger[i];

                // Jeder Zweig bekommt eine eigene Kopie - sonst wüchse die erste Kette
                // um die Glieder aller Geschwister.
                List<Kettenglied> zweig = new List<Kettenglied>(kette);
                zweig.Add(Glied(b, Kantenart.Kaskade));

                // W10b-B-1: erst der Puffer, an dem die Kaskade WEITERGEHT (jeder Rang),
                // sonst wie bisher die Hauptsenke — dort endet die Kette am Abnehmer.
                int weiter = KettenPuffer(b, nutzer);
                if (weiter <= 0) weiter = HauptsenkePuffer(b);
                if (weiter > 0)
                    KetteFortsetzen(zweig, weiter, nutzer, pufferJeId, quellpuffer,
                                    hatBrauchwasser, new HashSet<int>(besucht));
                else
                    DirektAbschliessen(zweig, b, hatBrauchwasser);
            }
        }

        private void KetteAbschliessen(List<Kettenglied> kette, int idPuffer,
                                       Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                       bool hatBrauchwasser)
        {
            // PAKET S2: über das KLASSEN-SET statt über die Alt-Verwendung; die Kette
            // endet am ERSTEN Kanal, den der Speicher bedient (das Band zeigt einen Weg,
            // keine Verzweigung).
            if (PufferBedient(idPuffer, Kanal.HEIZUNG))
                kette.Add(new Kettenglied
                {
                    Schluessel = ABNEHMER_HEIZKREIS,
                    Text = MyResource.Resource.SIM_HEIZKREIS,
                    Art = Knotenart.Abnehmer,
                    PfeilDavor = Kantenart.Versorgung
                });
            else if (hatBrauchwasser && PufferBedient(idPuffer, Kanal.BRAUCHWASSER))
                kette.Add(new Kettenglied
                {
                    Schluessel = ABNEHMER_WARMWASSER,
                    Text = MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER,
                    Art = Knotenart.Abnehmer,
                    PfeilDavor = Kantenart.Versorgung
                });
            else if (PufferBedient(idPuffer, Kanal.PROZESS))
                kette.Add(new Kettenglied
                {
                    Schluessel = ABNEHMER_PROZESS,
                    Text = MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                    Art = Knotenart.Abnehmer,
                    // PAKET E1 (S2-O7): eigene Kantenart, auch im Kaskadenband.
                    PfeilDavor = Kantenart.Prozess
                });

            Ketten.Add(kette);
        }

        private void DirektAbschliessen(List<Kettenglied> kette,
                                        Hydraulikbild.AnlagenEintrag a, bool hatBrauchwasser)
        {
            bool warmwasser = hatBrauchwasser &&
                              string.Equals(a.Senke.Bedarfsart, WaermequelleClass.SENKE_WARMWASSER,
                                            StringComparison.Ordinal);

            kette.Add(new Kettenglied
            {
                Schluessel = warmwasser ? ABNEHMER_WARMWASSER : ABNEHMER_HEIZKREIS,
                Text = warmwasser
                    ? MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER
                    : MyResource.Resource.SIM_HEIZKREIS,
                Art = Knotenart.Abnehmer,
                PfeilDavor = Kantenart.Versorgung
            });

            Ketten.Add(kette);
        }

        private static Kettenglied Glied(Hydraulikbild.AnlagenEintrag a, Kantenart pfeil)
        {
            return new Kettenglied
            {
                Schluessel = PRAEFIX_ERZEUGER + a.ID,
                Text = a.Bezeichner.Length > 0 ? a.Bezeichner : Ladeordnung.ErzeugerName(a.ID_Type),
                Art = Knotenart.Erzeuger,
                PfeilDavor = pfeil
            };
        }

        /// <summary>
        /// Der Speicher, den die Anlage auf RANG 1 lädt; 0, wenn Rang 1 eine Direktsenke
        /// ist. PAKET S2: aus der Senkenliste statt aus der Altspalte — sonst hinge die
        /// Kaskadenkette an einer Spiegelung, die die Prozess-Ziele nicht abbilden kann.
        /// </summary>
        private int HauptsenkePuffer(Hydraulikbild.AnlagenEintrag a)
        {
            List<Z_AnlageSenkeModel> kette = Senken(a.ID);
            if (kette.Count == 0) return 0;

            Z_AnlageSenkeModel z = kette[0];
            return WaermesenkeClass.IstPufferZiel(z.Ziel) ? z.ID_Puffer : 0;
        }

        // --- Selbstprüfung ------------------------------------------------------------

        /// <summary>
        /// Prüft das Modell gegen die Entwurfsregeln; nie <c>null</c>, leer = in Ordnung.
        ///
        /// Zweck ist das Prüfprogramm der Etappe (Modell testen, nicht Pixel):
        /// <list type="number">
        ///   <item><description>Invariante S-1 — keine Kante Speicher → Speicher.</description></item>
        ///   <item><description>Jede Kante hat beide Endknoten.</description></item>
        ///   <item><description>Kein Knotenschlüssel doppelt.</description></item>
        ///   <item><description>Zwischen zwei Speichergliedern einer Kaskadenkette steht ein
        ///     Erzeugerglied.</description></item>
        /// </list>
        /// </summary>
        public List<string> Pruefen()
        {
            List<string> fehler = new List<string>();

            HashSet<string> gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Knoten k in Knotenliste)
                if (!gesehen.Add(k.Schluessel)) fehler.Add("Knoten doppelt: " + k.Schluessel);

            foreach (Kante e in Kantenliste)
            {
                Knoten von = Finden(e.Von);
                Knoten nach = Finden(e.Nach);
                if (von == null) { fehler.Add("Kante ohne Startknoten: " + e.Von); continue; }
                if (nach == null) { fehler.Add("Kante ohne Zielknoten: " + e.Nach); continue; }

                if (von.Art == Knotenart.Speicher && nach.Art == Knotenart.Speicher)
                    fehler.Add("Invariante S-1 verletzt: " + e.Von + " -> " + e.Nach);
            }

            foreach (List<Kettenglied> kette in Ketten)
                for (int i = 1; i < kette.Count; i++)
                    if (kette[i].Art == Knotenart.Speicher && kette[i - 1].Art == Knotenart.Speicher)
                        fehler.Add("Invariante S-1 verletzt (Kette): " +
                                   kette[i - 1].Schluessel + " -> " + kette[i].Schluessel);

            return fehler;
        }
    }
}
