using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ladepriorität, Ladeobergrenzen und Entladereihenfolge der Projekt-Pufferspeicher
    /// (Konzept 3.4, 3.5 und 3.6).
    ///
    /// Die Klasse rechnet ausschließlich AUS den gespeicherten Feldern und schreibt
    /// nichts. Sie ist damit die eine Stelle, aus der sowohl die Anzeige „Lädt als n.
    /// von m" im Senkendialog (4.2) als auch die Tabelle „Ladereihenfolge dieses
    /// Speichers" im Puffer-Dialog (4.3) gespeist werden — und später die Engine
    /// (Paket 4/6). Genau das verlangt Konzept 3.4: „Die Anzeige ist die maßgebliche
    /// Kontrollinstanz; sie wird aus denselben Daten berechnet, die die Simulation
    /// verwendet."
    ///
    /// Dialogfrei (nur <see cref="StilleDb"/>), damit sie auch aus einem headless
    /// laufenden Prüfprogramm heraus benutzbar ist.
    /// </summary>
    public static class Ladeordnung
    {
        // --- Vorgabe-Rangfolge nach Grenzkosten (Konzept 3.4) -------------------------

        /// <summary>Solarthermie: Grenzkosten ≈ 0, nicht genutzte Einstrahlung ist verloren.</summary>
        public const int PRIO_SOLARTHERMIE = 10;

        /// <summary>Wärmepumpe: Grenzkosten = Strompreis / JAZ, profitiert vom leeren Speicher.</summary>
        public const int PRIO_WAERMEPUMPE = 20;

        /// <summary>BHKW: Wärme ist Koppelprodukt, Bewertung hängt an der Stromgutschrift.</summary>
        public const int PRIO_BHKW = 30;

        /// <summary>Heizkessel: höchste Grenzkosten, soll nur nachheizen.</summary>
        public const int PRIO_HEIZKESSEL = 40;

        /// <summary>Alles, was keinem der vier Erzeugertypen entspricht — hinter dem Kessel.</summary>
        public const int PRIO_SONSTIGE = 50;

        /// <summary>Kleinste bzw. größte manuell zulässige Ladepriorität (Konzept 3.4).</summary>
        public const int PRIO_MIN = 1;
        public const int PRIO_MAX = 99;

        /// <summary>
        /// Ersatzwert für eine nicht gepflegte <c>Tab_Energieanlagen.Prioritaet</c> im
        /// Gleichstandsfall. 0 bedeutet dort „nicht gesetzt"; ohne diese Abbildung
        /// stünde eine ungepflegte Anlage vor jeder gepflegten (1 = zuerst).
        /// </summary>
        public const int ANLAGENPRIO_UNGEPFLEGT = 99;

        /// <summary>
        /// Dieselbe Regel als SQL-Ausdruck, für jeden Leser, der Anlagen direkt in der
        /// Datenbank nach <c>Tab_Energieanlagen.Prioritaet</c> ordnet.
        ///
        /// <para><b>Warum das nötig ist.</b> In der Datenbank steht die nicht gepflegte
        /// Priorität als NULL oder 0, und ACE sortiert beides VOR die 1. Ein
        /// <c>ORDER BY Prioritaet, ID</c> stellt damit jede frisch angelegte Anlage vor
        /// die konfigurierte — genau umgekehrt zu dem, was
        /// <see cref="ANLAGENPRIO_UNGEPFLEGT"/> für die Ladeordnung längst festlegt
        /// (siehe <see cref="SortierenNachLadeprio"/>, dritte Stufe der Kette). Auf den
        /// Erzeugerkarten erbt die vorderste Karte eines Typs außerdem die Pfeil- und
        /// Entfernen-Knöpfe; die ungepflegte Anlage nahm sie der konfigurierten weg.</para>
        ///
        /// <para>Der Ausdruck steht hier und nicht in den Abfragen, damit die Regel EINE
        /// bleibt: Zahl und Bedingung stehen genau einmal da, direkt neben der Konstante,
        /// aus der sie kommen.</para>
        ///
        /// <paramref name="alias"/> ist der Tabellen-Alias der Abfrage (z. B. <c>"a"</c>);
        /// leer oder <c>null</c> für eine Abfrage ohne Alias.
        /// </summary>
        public static string SqlAnlagenprio(string alias)
        {
            string t = string.IsNullOrEmpty(alias) ? "" : alias + ".";
            return "IIF(" + t + "Prioritaet IS NULL OR " + t + "Prioritaet = 0, " +
                   ANLAGENPRIO_UNGEPFLEGT.ToString(CultureInfo.InvariantCulture) + ", " +
                   t + "Prioritaet)";
        }

        /// <summary>Kaskadenposition einer Anlage, die in <c>Tool_1..4</c> nicht vorkommt.</summary>
        public const int KASKADE_UNBEKANNT = 99;

        /// <summary>Abschaltschwelle eines Puffers ohne eigene Vorgabe [%] (Konzept 5.1).</summary>
        public const double SCHWELLE_AUS_DEFAULT = 95.0;

        /// <summary>Einschaltschwelle eines Puffers ohne eigene Vorgabe [%] (Konzept 5.1).</summary>
        public const double SCHWELLE_EIN_DEFAULT = 10.0;

        /// <summary>
        /// Mindestfüllstand/Notreserve eines Puffers ohne eigene Vorgabe [%] (Paket
        /// BHKW-Regulär, Entscheidung des Anwenders 17.08.2026, Punkt 3).
        ///
        /// Sie wirkt AUSSCHLIESSLICH auf die Entladung im BHKW-Pfad — alle anderen
        /// Erzeuger entladen unverändert bis 0. Der Wert ist derselbe, den
        /// Migrationsschritt 13 in den Bestand schreibt; die Konstante trägt ihn für
        /// Datenbanken, in denen die Spalte noch NULL ist (Migration nicht gelaufen, Spalte
        /// nur über die Rückfallebene <c>WaermequelleClass.SchemaSicherstellen</c> angelegt).
        /// </summary>
        public const double SCHWELLE_RESERVE_DEFAULT = 10.0;

        /// <summary>Vorgabe-Ladepriorität nach Erzeugertyp (<c>WS_Ladeprio = 0</c>).</summary>
        public static int VorgabeLadeprio(int idType)
        {
            switch (idType)
            {
                case ProjektPuffer.TYP_SOLARTHERMIE: return PRIO_SOLARTHERMIE;
                case ProjektPuffer.TYP_WP: return PRIO_WAERMEPUMPE;
                case ProjektPuffer.TYP_BHKW: return PRIO_BHKW;
                case ProjektPuffer.TYP_KESSEL: return PRIO_HEIZKESSEL;
                default: return PRIO_SONSTIGE;
            }
        }

        /// <summary>
        /// Wirksame Ladepriorität: der manuelle Wert 1…99, sonst die Vorgabe nach
        /// Erzeugertyp. Werte außerhalb 1…99 gelten als „nicht gesetzt".
        /// </summary>
        public static int WirksameLadeprio(int idType, int ladeprio)
        {
            if (ladeprio >= PRIO_MIN && ladeprio <= PRIO_MAX) return ladeprio;
            return VorgabeLadeprio(idType);
        }

        /// <summary>
        /// Wirksame Ladepriorität in einer Stunde MIT PV-Überschuss (Konzept 3.5):
        /// <c>WS_Ladeprio_PV</c> übersteuert, aber nur bei Betriebsmodus PV.
        /// Steht hier, damit Anzeige und Engine dieselbe Regel benutzen.
        /// </summary>
        public static int WirksameLadeprioPV(int idType, int ladeprio, int ladeprioPV,
                                             string bmTyp, bool pvUeberschuss)
        {
            if (pvUeberschuss &&
                string.Equals(bmTyp, WaermequelleClass.MODUS_PV, StringComparison.Ordinal) &&
                ladeprioPV >= PRIO_MIN && ladeprioPV <= PRIO_MAX)
                return ladeprioPV;

            return WirksameLadeprio(idType, ladeprio);
        }

        /// <summary>
        /// ANZEIGEname eines Erzeugertyps — lokalisiert (Paket 9 / L6).
        ///
        /// Nicht zu verwechseln mit <see cref="KaskadenLiteral"/> direkt darunter: Das
        /// liefert für dieselben Typen die deutschen PERSISTENZWERTE, die in
        /// <c>Tab_Einstellungen.Tool_1..4</c> stehen. Bis Paket 9 waren beide
        /// Zeichenketten identisch, was die Verwechslung leicht machte; seitdem ist der
        /// Unterschied auch am Rückgabewert sichtbar.
        /// </summary>
        public static string ErzeugerName(int idType)
        {
            switch (idType)
            {
                case ProjektPuffer.TYP_WP: return MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE;
                case ProjektPuffer.TYP_SOLARTHERMIE: return MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE;
                case ProjektPuffer.TYP_KESSEL: return MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL;
                case ProjektPuffer.TYP_BHKW: return MyResource.Resource.SIM_ERZEUGERNAME_BHKW;
                default: return MyResource.Resource.SIM_ERZEUGERNAME_ALLGEMEIN;
            }
        }

        /// <summary>Erzeuger-Literal, wie es in <c>Tab_Einstellungen.Tool_1..4</c> steht.</summary>
        private static string KaskadenLiteral(int idType)
        {
            switch (idType)
            {
                case ProjektPuffer.TYP_WP: return DbWerte.ERZEUGER_WAERMEPUMPE;
                case ProjektPuffer.TYP_SOLARTHERMIE: return DbWerte.ERZEUGER_SOLARTHERMIE;
                case ProjektPuffer.TYP_KESSEL: return DbWerte.ERZEUGER_HEIZKESSEL;
                case ProjektPuffer.TYP_BHKW: return DbWerte.ERZEUGER_BHKW;
                default: return null;
            }
        }

        // --- Eine ladende Anlage an einem Puffer --------------------------------------

        /// <summary>Eine Zeile der Ladereihenfolge eines Pufferspeichers.</summary>
        public sealed class LadeEintrag
        {
            /// <summary>Tab_Energieanlagen.ID der ladenden Anlage.</summary>
            public int ID_Anlage;

            public string Bezeichner = "";

            /// <summary>ID_Type der Anlage (1 WP, 2 Solar, 10 Kessel, 11 BHKW).</summary>
            public int ID_Type;

            /// <summary>Anzeigename des Erzeugertyps.</summary>
            public string Erzeuger = "";

            /// <summary>
            /// RANG der Senkenzeile (Paket S1, Konzept 5.1) — 1 = die bisherige
            /// Hauptsenke, 2 = die bisherige Zweitsenke, darüber die mit S1 neu möglichen
            /// weiteren Senken.
            /// </summary>
            public int Rang = 1;

            /// <summary>
            /// true = der Puffer ist NICHT die erstrangige Senke dieser Anlage.
            /// ABGELEITET aus <see cref="Rang"/> (Paket S1); bleibt für Anzeigen und für
            /// <see cref="Ladeordnung.Position(List{LadeEintrag}, int, bool)"/> erhalten.
            /// </summary>
            public bool Zweitsenke
            {
                get { return Rang > 1; }
            }

            /// <summary>Wirksame Ladepriorität (manuell oder Vorgabe).</summary>
            public int Ladeprio;

            /// <summary>true = die Priorität ist manuell gesetzt, nicht aus der Vorgabe.</summary>
            public bool PrioManuell;

            /// <summary>Sonderpriorität bei PV-Überschuss; 0 = keine (Konzept 3.5).</summary>
            public int LadeprioPV;

            /// <summary>Eigene Ladeobergrenze der Anlage [%]; 0 = nicht gesetzt.</summary>
            public double Ladegrenze;

            /// <summary>Aufgelöste Obergrenze [%] nach der Regel aus Konzept 3.4.</summary>
            public double Obergrenze;

            /// <summary>true = die Obergrenze stammt aus der eigenen Anlagengrenze.</summary>
            public bool ObergrenzeEigen;

            /// <summary>
            /// true = VORRANGIGE Anlage an diesem Puffer, d. h. ihre Ladepriorität ist
            /// die kleinste, die an diesem Speicher anliegt (Konzept 3.4). Bei
            /// Gleichstand trifft das auf MEHRERE Anlagen zu — der Vorrang ist über die
            /// Zahl definiert, nicht über den Listenplatz.
            /// </summary>
            public bool Vorrangig;

            /// <summary>Position in Tab_Einstellungen.Tool_1..4 (1…4), sonst 99.</summary>
            public int Kaskadenposition = KASKADE_UNBEKANNT;

            /// <summary>Tab_Energieanlagen.Prioritaet (0 = nicht gesetzt → 99).</summary>
            public int Anlagenprioritaet = ANLAGENPRIO_UNGEPFLEGT;

            public override string ToString()
            {
                return string.Format(MyResource.Resource.SIM_LADEEINTRAG_ANZEIGE,
                                     Bezeichner, Erzeuger, Ladeprio);
            }
        }

        /// <summary>
        /// Alle Anlagen, die den Puffer laden — über ALLE Senkenränge —, in der
        /// WIRKSAMEN Reihenfolge nach Konzept 3.4:
        ///
        ///   Ladepriorität → Kaskadenposition → Tab_Energieanlagen.Prioritaet → Anlagen-ID
        ///
        /// Die Kette ist vollständig deterministisch und nie von der Datenbankreihenfolge
        /// abhängig. Die Obergrenze je Eintrag ist bereits aufgelöst:
        ///
        ///   Obergrenze = eigene Ladegrenze der Senkenzeile, wenn gesetzt (&gt; 0)
        ///              = Schwelle_Aus                      , wenn die Anlage die vorrangige ist
        ///              = Schwelle_Aus_Nachrang             , sonst
        ///
        /// <para><b>PAKET A1 — die Anzeigefassung liest die SENKENLISTE.</b> Bis dahin
        /// fragte sie die Altspalten <c>WS_ID_Puffer</c>/<c>WS_ID_Puffer2</c> und kannte
        /// deshalb genau zwei Senkenplätze je Anlage: Für eine Senke ab Rang 3 lieferte
        /// sie 0, und die Kreisziffer „lädt als n von m" fehlte an Karte und Schemakante
        /// (S2-O6). Sie holt sich die Listen jetzt selbst — STILL, ohne Protokollzeilen
        /// (<c>WaermesenkeClass.SenkenlistenLadenStill</c>), weil sie aus Dialogen heraus
        /// läuft. Wer die Listen ohnehin schon hat (die Engine), reicht sie über die
        /// Überladung herein und spart die Abfrage.</para>
        /// </summary>
        public static List<LadeEintrag> Ladereihenfolge(int idProjekt, int idPuffer)
        {
            if (idProjekt <= 0 || idPuffer <= 0) return new List<LadeEintrag>();

            return Ladereihenfolge(idProjekt, idPuffer,
                                   WaermesenkeClass.SenkenlistenLadenStill(idProjekt));
        }

        /// <summary>
        /// Dieselbe Ordnung mit BEREITS GELESENEN Senkenlisten (Paket S1, Konzept 5.1) —
        /// die Fassung, die die Engine ruft: Sie hält die Listen des Laufs ohnehin und
        /// spart damit die Abfrage je Puffer.
        ///
        /// <para><b>Was gleich bleibt.</b> Sortierregel (Ladepriorität → Kaskadenposition
        /// → Anlagenpriorität → Anlagen-ID → Rang) und Obergrenzen-Auflösung sind
        /// dieselben wie bei der parameterlosen Fassung; beide laufen durch DIESEN
        /// Rumpf.</para>
        ///
        /// <paramref name="senken"/> <c>null</c> = die Listen werden still nachgeladen.
        /// </summary>
        public static List<LadeEintrag> Ladereihenfolge(int idProjekt, int idPuffer,
                                                        List<Senkenliste> senken)
        {
            if (senken == null) senken = WaermesenkeClass.SenkenlistenLadenStill(idProjekt);

            List<LadeEintrag> liste = new List<LadeEintrag>();
            if (idProjekt <= 0 || idPuffer <= 0) return liste;

            Dictionary<int, int> kaskade = Kaskadenpositionen(idProjekt);
            Dictionary<int, Anlagenkopf> koepfe = Anlagenkoepfe(idProjekt);

            foreach (Senkenliste s in senken)
            {
                if (s == null) continue;

                Anlagenkopf kopf;
                if (!koepfe.TryGetValue(s.AnlagenID, out kopf)) continue;   // fremde/gelöschte Anlage

                int kaskadenpos;
                if (!kaskade.TryGetValue(kopf.ID_Type, out kaskadenpos)) kaskadenpos = KASKADE_UNBEKANNT;

                foreach (Senkenzeile z in s.Zeilen)
                {
                    if (z == null || !z.IstPuffersenke) continue;
                    if (z.IDPuffer != idPuffer) continue;

                    liste.Add(Eintrag(s.AnlagenID, kopf.Bezeichner, kopf.ID_Type, z.Rang,
                                      z.Ladeprio, z.LadegrenzeProzent, z.LadeprioPV,
                                      kaskadenpos, kopf.Anlagenprioritaet));
                }
            }

            Sortieren(liste);
            ObergrenzenAufloesen(liste, idPuffer);
            return liste;
        }

        /// <summary>Die Anlagenfelder, die eine Senkenzeile nicht trägt (Paket S1).</summary>
        private sealed class Anlagenkopf
        {
            public int ID_Type;
            public string Bezeichner = "";
            public int Anlagenprioritaet = ANLAGENPRIO_UNGEPFLEGT;
        }

        /// <summary>
        /// Bezeichner, Typ und Anlagenpriorität aller Wärmeerzeuger eines Projekts —
        /// EINE Abfrage für die ganze Ladeordnung (Paket S1).
        ///
        /// <c>Z_AnlageSenke</c> trägt nur die Senke, nicht die Anlage; die drei Felder
        /// stecken weiter in <c>Tab_Energieanlagen</c>. Ohne diese Sammelabfrage käme je
        /// Senkenzeile ein Nachschlag dazu — ein N+1 mitten im Kontextaufbau.
        /// </summary>
        private static Dictionary<int, Anlagenkopf> Anlagenkoepfe(int idProjekt)
        {
            Dictionary<int, Anlagenkopf> map = new Dictionary<int, Anlagenkopf>();
            if (idProjekt <= 0) return map;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, Bezeichner, ID_Type, Prioritaet FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ")",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null) return map;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (id <= 0) continue;

                Anlagenkopf k = new Anlagenkopf();
                k.ID_Type = StilleDb.Zahl(StilleDb.Feld(r, "ID_Type"));
                k.Bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));

                int prio = StilleDb.Zahl(StilleDb.Feld(r, "Prioritaet"));
                k.Anlagenprioritaet = prio > 0 ? prio : ANLAGENPRIO_UNGEPFLEGT;

                map[id] = k;
            }

            return map;
        }

        private static LadeEintrag Eintrag(int idAnlage, string bezeichner, int idType,
                                           bool zweitsenke, int ladeprioRoh, double ladegrenze,
                                           int ladeprioPV, int kaskadenpos, int anlagenprio)
        {
            return Eintrag(idAnlage, bezeichner, idType, zweitsenke ? 2 : 1, ladeprioRoh,
                           ladegrenze, ladeprioPV, kaskadenpos, anlagenprio);
        }

        /// <summary>Dieselbe Bildung mit ausdrücklichem RANG (Paket S1).</summary>
        private static LadeEintrag Eintrag(int idAnlage, string bezeichner, int idType,
                                           int rang, int ladeprioRoh, double ladegrenze,
                                           int ladeprioPV, int kaskadenpos, int anlagenprio)
        {
            LadeEintrag e = new LadeEintrag();
            e.ID_Anlage = idAnlage;
            e.Bezeichner = bezeichner;
            e.ID_Type = idType;
            e.Erzeuger = ErzeugerName(idType);
            e.Rang = rang > 0 ? rang : 1;
            e.PrioManuell = ladeprioRoh >= PRIO_MIN && ladeprioRoh <= PRIO_MAX;
            e.Ladeprio = WirksameLadeprio(idType, ladeprioRoh);
            e.LadeprioPV = (ladeprioPV >= PRIO_MIN && ladeprioPV <= PRIO_MAX) ? ladeprioPV : 0;
            e.Ladegrenze = ladegrenze > 0 ? ladegrenze : 0;
            e.Kaskadenposition = kaskadenpos;
            e.Anlagenprioritaet = anlagenprio;
            return e;
        }

        /// <summary>
        /// Sortierung nach Konzept 3.4. Die Zweitsenke steht bei sonst gleichen Werten
        /// hinter der Hauptsenke derselben Anlage — sie verwertet nur Überschuss.
        /// </summary>
        private static void Sortieren(List<LadeEintrag> liste)
        {
            SortierenNachLadeprio(liste, PrioEintrag);
        }

        /// <summary>Priorität eines Eintrags ohne Stundenbezug — die gespeicherte Ladeprio.</summary>
        private static int PrioEintrag(LadeEintrag e)
        {
            return e.Ladeprio;
        }

        /// <summary>
        /// Die Ordnungsregel aus Konzept 3.4 als EINE Implementierung für Anzeige und
        /// Engine:
        ///
        ///   Ladepriorität → Kaskadenposition → Tab_Energieanlagen.Prioritaet →
        ///   Anlagen-ID → RANG der Senkenzeile
        ///
        /// Das letzte Glied hieß bis Paket S1 „Hauptsenke vor Zweitsenke" und ist
        /// wertgleich: Mit zwei Senkenplätzen war Rang 1 die Hauptsenke und Rang 2 die
        /// Zweitsenke, und <c>false &lt; true</c> ist dieselbe Ordnung wie
        /// <c>1 &lt; 2</c>. Mit n Senken ordnet der Rang jetzt auch Rang 3 und darüber.
        ///
        /// Die Priorität kommt aus <paramref name="prio"/> statt fest aus
        /// <see cref="LadeEintrag.Ladeprio"/>, weil sie ab Konzept 3.5 ZEITABHÄNGIG ist:
        /// Die Engine reicht in Stunden mit PV-Überschuss die Auflösung nach
        /// <see cref="WirksameLadeprioPV"/> herein, die Anzeige die gespeicherte. Alles
        /// Übrige — und damit die Deterministik der Reihenfolge — ist dasselbe.
        ///
        /// Die Sortierung ist stabil im Ergebnis, nicht im Verfahren: Die Kette endet mit
        /// der Anlagen-ID und dem Senkenrang, ist also für zwei verschiedene Aufträge nie
        /// unentschieden und damit nie von der Datenbank- oder Listenreihenfolge abhängig.
        /// </summary>
        public static void SortierenNachLadeprio(List<LadeEintrag> liste,
                                                 Converter<LadeEintrag, int> prio)
        {
            if (liste == null || prio == null) return;

            liste.Sort(delegate (LadeEintrag a, LadeEintrag b)
            {
                int c = prio(a).CompareTo(prio(b));
                if (c != 0) return c;
                c = a.Kaskadenposition.CompareTo(b.Kaskadenposition);
                if (c != 0) return c;
                c = a.Anlagenprioritaet.CompareTo(b.Anlagenprioritaet);
                if (c != 0) return c;
                c = a.ID_Anlage.CompareTo(b.ID_Anlage);
                if (c != 0) return c;
                return a.Rang.CompareTo(b.Rang);
            });
        }

        /// <summary>
        /// Auflösungsregel der Ladeobergrenzen (Konzept 3.4).
        ///
        /// VORRANG BEI GLEICHSTAND: Vorrangig ist jede Anlage mit der KLEINSTEN
        /// Ladepriorität der Liste — nicht nur die erste Zeile. Konzept 3.4 definiert den
        /// Vorrang über die Zahl; die weitere Sortierung (Kaskade, Anlagenprio, ID)
        /// ordnet nur innerhalb desselben Rangs und darf keinen Rangunterschied
        /// erfinden. Zwei Solarfelder mit Ladeprio 10 laden deshalb BEIDE bis
        /// <c>Schwelle_Aus</c>; die Reservezone <c>Schwelle_Aus_Nachrang</c> bleibt für
        /// die echten Nachrangigen (Wärmepumpe, Kessel …) frei. Vorher bekam das zweite
        /// Solarfeld die Nachrangschwelle — ein Rangunterschied, den niemand eingestellt
        /// hatte.
        /// </summary>
        private static void ObergrenzenAufloesen(List<LadeEintrag> liste, int idPuffer)
        {
            ObergrenzenAufloesen(liste, idPuffer, PrioEintrag);
        }

        /// <summary>
        /// Dieselbe Auflösung mit einer FREI WÄHLBAREN Priorität — die zeitabhängige
        /// Fassung (Konzept 3.5).
        ///
        /// Der Vorrang an einem Puffer entscheidet über die Obergrenze (Schwelle_Aus
        /// gegen Schwelle_Aus_Nachrang). In Stunden mit PV-Überschuss gilt aber eine
        /// ANDERE Priorität als die gespeicherte (<see cref="WirksameLadeprioPV"/>) —
        /// also kann auch eine andere Anlage die vorrangige sein. Würde die Engine die
        /// Reihenfolge nach der PV-Priorität bilden, die Obergrenzen aber nach der
        /// gespeicherten, bekäme die in dieser Stunde vorrangige Anlage die Reservezone
        /// nicht, für die sie gerade nach vorn gezogen wurde. Anzeige und Engine
        /// benutzen deshalb dieselbe Funktion, nur mit der jeweils gültigen Priorität.
        ///
        /// Der Vorrang wird hier über das MINIMUM von <paramref name="prio"/> bestimmt
        /// und nicht über den ersten Listenplatz: Die Liste kann nach einer anderen
        /// Priorität sortiert sein als der, nach der aufgelöst wird.
        /// </summary>
        public static void ObergrenzenAufloesen(List<LadeEintrag> liste, int idPuffer,
                                                Converter<LadeEintrag, int> prio)
        {
            if (liste == null || liste.Count == 0 || prio == null) return;

            double schwelleAus, schwelleAusNachrang;
            SchwellenLesen(idPuffer, out schwelleAus, out schwelleAusNachrang);

            int besteLadeprio = prio(liste[0]);
            for (int i = 1; i < liste.Count; i++)
            {
                int p = prio(liste[i]);
                if (p < besteLadeprio) besteLadeprio = p;
            }

            for (int i = 0; i < liste.Count; i++)
            {
                LadeEintrag e = liste[i];
                e.Vorrangig = (prio(e) == besteLadeprio);

                if (e.Ladegrenze > 0)
                {
                    e.Obergrenze = e.Ladegrenze;
                    e.ObergrenzeEigen = true;
                }
                else
                {
                    e.Obergrenze = e.Vorrangig ? schwelleAus : schwelleAusNachrang;
                    e.ObergrenzeEigen = false;
                }
            }
        }

        /// <summary>
        /// Abschaltschwelle und Abschaltschwelle-für-Nachrangige eines Puffers [%].
        /// Fehlt <c>Schwelle_Aus_Nachrang</c>, gilt <c>Schwelle_Aus</c> — das ist die
        /// verhaltensneutrale Vorbelegung aus Konzept 3.4 (keine Reservezone).
        /// </summary>
        public static void SchwellenLesen(int idPuffer, out double schwelleAus,
                                          out double schwelleAusNachrang)
        {
            schwelleAus = SCHWELLE_AUS_DEFAULT;
            schwelleAusNachrang = SCHWELLE_AUS_DEFAULT;
            if (idPuffer <= 0) return;

            DataTable dt = StilleDb.Tabelle(
                "SELECT Schwelle_Aus, Schwelle_Aus_Nachrang FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idPuffer));
            if (dt == null || dt.Rows.Count == 0) return;

            double aus = StilleDb.Kommazahl(StilleDb.Feld(dt.Rows[0], "Schwelle_Aus"));
            if (aus > 0) schwelleAus = aus;

            double nachrang = StilleDb.Kommazahl(StilleDb.Feld(dt.Rows[0], "Schwelle_Aus_Nachrang"));
            schwelleAusNachrang = nachrang > 0 ? nachrang : schwelleAus;
        }

        /// <summary>
        /// Ladereihenfolge mit einer noch NICHT gespeicherten Einstellung — die Grundlage
        /// der Anzeige „Lädt als n. von m" im Senkendialog (Konzept 4.2).
        ///
        /// Der Dialog zeigt damit die Wirkung der gerade gewählten Priorität, nicht die
        /// des zuletzt gespeicherten Stands. Der eigene Eintrag der Anlage wird aus der
        /// gespeicherten Liste entfernt und durch den übergebenen ersetzt; sortiert und
        /// aufgelöst wird danach exakt wie in <see cref="Ladereihenfolge"/>.
        /// </summary>
        public static List<LadeEintrag> LadereihenfolgeVorschau(int idProjekt, int idPuffer,
                                                               int idAnlage, int idType,
                                                               bool zweitsenke, int ladeprioRoh,
                                                               double ladegrenze, int ladeprioPV)
        {
            List<LadeEintrag> liste = Ladereihenfolge(idProjekt, idPuffer);
            if (idPuffer <= 0 || idAnlage <= 0) return liste;

            liste.RemoveAll(delegate (LadeEintrag e)
            {
                return e.ID_Anlage == idAnlage && e.Zweitsenke == zweitsenke;
            });

            int kaskadenpos = KASKADE_UNBEKANNT;
            Dictionary<int, int> kaskade = Kaskadenpositionen(idProjekt);
            if (!kaskade.TryGetValue(idType, out kaskadenpos)) kaskadenpos = KASKADE_UNBEKANNT;

            int anlagenprio = StilleDb.Zahl(StilleDb.Scalar(
                "SELECT Prioritaet FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idAnlage)));
            if (anlagenprio <= 0) anlagenprio = ANLAGENPRIO_UNGEPFLEGT;

            string bezeichner = StilleDb.Text(StilleDb.Scalar(
                "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idAnlage)));

            liste.Add(Eintrag(idAnlage, bezeichner, idType, zweitsenke, ladeprioRoh, ladegrenze,
                              ladeprioPV, kaskadenpos, anlagenprio));

            Sortieren(liste);
            ObergrenzenAufloesen(liste, idPuffer);
            return liste;
        }

        /// <summary>
        /// Position einer Anlage in der Ladereihenfolge (1-basiert); 0, wenn sie nicht
        /// vorkommt. Grundlage der Anzeige „Lädt als n. von m" (Konzept 4.2).
        /// </summary>
        public static int Position(List<LadeEintrag> liste, int idAnlage, bool zweitsenke)
        {
            if (liste == null) return 0;
            for (int i = 0; i < liste.Count; i++)
                if (liste[i].ID_Anlage == idAnlage && liste[i].Zweitsenke == zweitsenke)
                    return i + 1;
            return 0;
        }

        // --- Entladereihenfolge (Konzept 3.6) ----------------------------------------

        /// <summary>Ein Puffer in der Entladereihenfolge seines Kanals.</summary>
        public sealed class EntladeEintrag
        {
            public int ID_Puffer;
            public string Bezeichner = "";
            public string Verwendung = "";

            /// <summary>Wirksame Entladepriorität (manuell oder Automatik).</summary>
            public int Prio;

            /// <summary>true = <c>Entladeprio</c> ist manuell gesetzt.</summary>
            public bool Manuell;

            public override string ToString()
            {
                return string.Format(Manuell
                                         ? MyResource.Resource.SIM_ENTLADEEINTRAG_MANUELL
                                         : MyResource.Resource.SIM_ENTLADEEINTRAG_AUTOMATISCH,
                                     Bezeichner, Prio);
            }
        }

        /// <summary>
        /// Automatikwert der Entladepriorität eines Speichers: die BESTE (kleinste)
        /// Ladepriorität, die an ihm anliegt (Konzept 3.6). Lädt ihn niemand, bleibt es
        /// bei <see cref="PRIO_SONSTIGE"/> — er wird dann zuletzt herangezogen.
        /// </summary>
        public static int EntladeprioAutomatik(int idProjekt, int idPuffer)
        {
            return EntladeprioAutomatik(idProjekt, idPuffer,
                                        WaermesenkeClass.SenkenlistenLadenStill(idProjekt));
        }

        /// <summary>
        /// Dieselbe Automatik mit BEREITS GELESENEN Senkenlisten (Paket S1) — nötig, weil
        /// ein Puffer ab S1 auch die Senke einer Zeile mit Rang 3 sein kann.
        /// <c>null</c> = die Listen werden still nachgeladen.
        /// </summary>
        public static int EntladeprioAutomatik(int idProjekt, int idPuffer,
                                               List<Senkenliste> senken)
        {
            List<LadeEintrag> laden = Ladereihenfolge(idProjekt, idPuffer, senken);
            if (laden.Count == 0) return PRIO_SONSTIGE;
            return laden[0].Ladeprio;
        }

        /// <summary>
        /// Alle Puffer eines Kanals (<paramref name="verwendung"/>) in der wirksamen
        /// Entladereihenfolge: kleinere Zahl = früher entladen; bei Gleichstand
        /// entscheidet die Puffer-ID (Konzept 3.6).
        ///
        /// LEERE <c>Verwendung</c> AM PUFFER ZÄHLT ALS „HEIZUNG". Die Auswahl läuft
        /// deshalb über <see cref="WaermesenkeClass.ProjektPufferListe"/> und NICHT über
        /// ein <c>WHERE Verwendung = ?</c>: Puffer aus dem früheren impliziten
        /// <c>CopyFromStamm</c> haben keine Verwendung, ein Gleichheitsvergleich in SQL
        /// ließe sie durchfallen. Sie sind aber im Senkendialog wählbar
        /// (<c>ProjektPufferListe</c>) und bestehen die Validierung
        /// (<c>PufferPasst</c> über <c>WirksameVerwendung</c>) — dann müssen sie auch in
        /// der Entladereihenfolge ihres Kanals stehen. Sonst zeigte die Verwaltung für
        /// einen Alt-Puffer „(keine Angabe)", obwohl ihn Anlagen laden.
        ///
        /// Eine leere <paramref name="verwendung"/> wird wie „Heizung" behandelt — der
        /// Kanal ist die Frage, und einen namenlosen Kanal gibt es nicht.
        ///
        /// ETAPPE D5a: Gefragt ist hier ausdrücklich „welche Speicher BEDIENEN diesen
        /// Kanal", nicht „welcher Puffer passt zu diesem Senkenziel". Ein KOMBISPEICHER
        /// bedient beide Kanäle aus EINEM Vorrat und steht deshalb in BEIDEN
        /// Entladereihenfolgen — je Kanal an der Stelle seiner Entladepriorität. Die
        /// Kanalsicht liefert <c>ProjektPufferListe(…, kanalSicht: true)</c>; ohne
        /// Kombispeicher ist sie dieselbe Liste wie zuvor.
        /// </summary>
        public static List<EntladeEintrag> Entladereihenfolge(int idProjekt, string verwendung)
        {
            if (idProjekt <= 0) return new List<EntladeEintrag>();

            // EINMAL laden und in die Schleife hineinreichen: Die Automatik je Puffer
            // braucht die Senkenlisten des ganzen Projekts, und ein Nachladen je
            // Listeneintrag wäre auf einem Projekt mit vielen Pufferkopien ein N+1.
            return Entladereihenfolge(idProjekt, verwendung,
                                      WaermesenkeClass.SenkenlistenLadenStill(idProjekt));
        }

        /// <summary>
        /// Dieselbe Entladereihenfolge mit BEREITS GELESENEN Senkenlisten als Grundlage
        /// der Entladeprio-Automatik (Paket S1). Die ENGINE reicht die Listen des Laufs
        /// herein, damit ein Puffer, den nur eine höherrangige Senkenzeile lädt, seine
        /// Automatik-Priorität bekommt statt <see cref="PRIO_SONSTIGE"/>.
        /// <c>null</c> = die Listen werden still nachgeladen.
        /// </summary>
        public static List<EntladeEintrag> Entladereihenfolge(int idProjekt, string verwendung,
                                                              List<Senkenliste> senken)
        {
            List<EntladeEintrag> liste = new List<EntladeEintrag>();
            if (idProjekt <= 0) return liste;

            if (senken == null) senken = WaermesenkeClass.SenkenlistenLadenStill(idProjekt);

            string kanal = string.IsNullOrEmpty(verwendung)
                ? WaermesenkeClass.VERWENDUNG_HEIZUNG : verwendung;

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(idProjekt, kanal, true))
            {
                EntladeEintrag e = new EntladeEintrag();
                e.ID_Puffer = p.ID;
                e.Bezeichner = p.Bezeichner;
                e.Verwendung = WaermesenkeClass.WirksameVerwendung(p);

                int manuell = p.Entladeprio;
                e.Manuell = manuell >= PRIO_MIN && manuell <= PRIO_MAX;
                e.Prio = e.Manuell ? manuell : EntladeprioAutomatik(idProjekt, e.ID_Puffer, senken);

                liste.Add(e);
            }

            liste.Sort(delegate (EntladeEintrag a, EntladeEintrag b)
            {
                int c = a.Prio.CompareTo(b.Prio);
                if (c != 0) return c;
                return a.ID_Puffer.CompareTo(b.ID_Puffer);
            });

            return liste;
        }

        /// <summary>Position eines Puffers in der Entladereihenfolge (1-basiert); 0 = nicht enthalten.</summary>
        public static int Position(List<EntladeEintrag> liste, int idPuffer)
        {
            if (liste == null) return 0;
            for (int i = 0; i < liste.Count; i++)
                if (liste[i].ID_Puffer == idPuffer) return i + 1;
            return 0;
        }

        // --- Kaskadenposition aus Tab_Einstellungen.Tool_1..4 -------------------------

        /// <summary>
        /// Abbildung ID_Type → Kaskadenposition (1…4) aus <c>Tab_Einstellungen.Tool_1..4</c>.
        /// Erzeugertypen, die nicht in der Kaskade stehen, fehlen in der Abbildung und
        /// bekommen im Gleichstandsfall <see cref="KASKADE_UNBEKANNT"/>.
        /// </summary>
        public static Dictionary<int, int> Kaskadenpositionen(int idProjekt)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            if (idProjekt <= 0) return map;

            DataTable dt = StilleDb.Tabelle(
                "SELECT Tool_1, Tool_2, Tool_3, Tool_4 FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null || dt.Rows.Count == 0) return map;

            DataRow r = dt.Rows[0];
            int[] typen =
            {
                ProjektPuffer.TYP_WP, ProjektPuffer.TYP_SOLARTHERMIE,
                ProjektPuffer.TYP_KESSEL, ProjektPuffer.TYP_BHKW
            };

            for (int spalte = 1; spalte <= 4; spalte++)
            {
                string wert = StilleDb.Text(StilleDb.Feld(r, "Tool_" + spalte));
                if (wert.Length == 0) continue;

                foreach (int typ in typen)
                {
                    if (map.ContainsKey(typ)) continue;
                    if (string.Equals(wert, KaskadenLiteral(typ), StringComparison.Ordinal))
                    {
                        map[typ] = spalte;
                        break;
                    }
                }
            }

            return map;
        }
    }
}
