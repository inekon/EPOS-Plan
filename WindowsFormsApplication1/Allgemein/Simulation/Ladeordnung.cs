using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

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

        /// <summary>Kaskadenposition einer Anlage, die in <c>Tool_1..4</c> nicht vorkommt.</summary>
        public const int KASKADE_UNBEKANNT = 99;

        /// <summary>Abschaltschwelle eines Puffers ohne eigene Vorgabe [%] (Konzept 5.1).</summary>
        public const double SCHWELLE_AUS_DEFAULT = 95.0;

        /// <summary>Einschaltschwelle eines Puffers ohne eigene Vorgabe [%] (Konzept 5.1).</summary>
        public const double SCHWELLE_EIN_DEFAULT = 10.0;

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

        /// <summary>Anzeigename eines Erzeugertyps (deutsch, wie in der Übersicht).</summary>
        public static string ErzeugerName(int idType)
        {
            switch (idType)
            {
                case ProjektPuffer.TYP_WP: return "Wärmepumpe";
                case ProjektPuffer.TYP_SOLARTHERMIE: return "Solarthermie";
                case ProjektPuffer.TYP_KESSEL: return "Heizkessel";
                case ProjektPuffer.TYP_BHKW: return "BHKW";
                default: return "Erzeuger";
            }
        }

        /// <summary>Erzeuger-Literal, wie es in <c>Tab_Einstellungen.Tool_1..4</c> steht.</summary>
        private static string KaskadenLiteral(int idType)
        {
            switch (idType)
            {
                case ProjektPuffer.TYP_WP: return "Wärmepumpe";
                case ProjektPuffer.TYP_SOLARTHERMIE: return "Solarthermie";
                case ProjektPuffer.TYP_KESSEL: return "Heizkessel";
                case ProjektPuffer.TYP_BHKW: return "BHKW";
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

            /// <summary>true = der Puffer ist die ZWEITsenke dieser Anlage.</summary>
            public bool Zweitsenke;

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
                return Bezeichner + " (" + Erzeuger + ", Prio " + Ladeprio + ")";
            }
        }

        /// <summary>
        /// Alle Anlagen, die den Puffer laden (Haupt- ODER Zweitsenke), in der
        /// WIRKSAMEN Reihenfolge nach Konzept 3.4:
        ///
        ///   Ladepriorität → Kaskadenposition → Tab_Energieanlagen.Prioritaet → Anlagen-ID
        ///
        /// Die Kette ist vollständig deterministisch und nie von der Datenbankreihenfolge
        /// abhängig. Die Obergrenze je Eintrag ist bereits aufgelöst:
        ///
        ///   Obergrenze = WS_Ladegrenze          , wenn gesetzt (&gt; 0)
        ///              = Schwelle_Aus           , wenn die Anlage die vorrangige ist
        ///              = Schwelle_Aus_Nachrang  , sonst
        /// </summary>
        public static List<LadeEintrag> Ladereihenfolge(int idProjekt, int idPuffer)
        {
            List<LadeEintrag> liste = new List<LadeEintrag>();
            if (idProjekt <= 0 || idPuffer <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, Bezeichner, ID_Type, Prioritaet, " +
                "       WS_Ziel, WS_ID_Puffer, WS_Ladeprio, WS_Ladegrenze, WS_Ladeprio_PV, " +
                "       WS_Ziel2, WS_ID_Puffer2, WS_Ladeprio2, WS_Ladegrenze2 " +
                "FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ") " +
                "  AND (WS_ID_Puffer = ? OR WS_ID_Puffer2 = ?)",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                StilleDb.Par("@puf1", OleDbType.Integer, idPuffer),
                StilleDb.Par("@puf2", OleDbType.Integer, idPuffer));

            if (dt == null) return liste;

            Dictionary<int, int> kaskade = Kaskadenpositionen(idProjekt);

            foreach (DataRow r in dt.Rows)
            {
                int idAnlage = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                int idType = StilleDb.Zahl(StilleDb.Feld(r, "ID_Type"));
                string bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                int anlagenprio = StilleDb.Zahl(StilleDb.Feld(r, "Prioritaet"));
                if (anlagenprio <= 0) anlagenprio = ANLAGENPRIO_UNGEPFLEGT;

                int kaskadenpos;
                if (!kaskade.TryGetValue(idType, out kaskadenpos)) kaskadenpos = KASKADE_UNBEKANNT;

                // ZIEL MIT PRÜFEN, nicht nur die ID: Altdaten können eine WS_ID_Puffer
                // tragen und trotzdem auf den Heizkreis zeigen (die Senke wurde
                // zurückgenommen, die ID blieb stehen; die Oberfläche schreibt seit
                // Paket 2 NULL, ältere Stände taten das nicht). Solche Reste zählten
                // sonst als ladende Anlage — die Anzeige „lädt als n. von m" und die
                // Entladeprio-Automatik lägen daneben, und die Engine wird ab Paket 4
                // ausschließlich über WS_Ziel entscheiden. Anzeige und Engine bleiben so
                // deckungsgleich.
                if (StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer")) == idPuffer &&
                    WaermesenkeClass.IstPufferZiel(StilleDb.Text(StilleDb.Feld(r, "WS_Ziel"))))
                {
                    liste.Add(Eintrag(idAnlage, bezeichner, idType, false,
                                      StilleDb.Zahl(StilleDb.Feld(r, "WS_Ladeprio")),
                                      StilleDb.Kommazahl(StilleDb.Feld(r, "WS_Ladegrenze")),
                                      StilleDb.Zahl(StilleDb.Feld(r, "WS_Ladeprio_PV")),
                                      kaskadenpos, anlagenprio));
                }

                if (StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer2")) == idPuffer &&
                    WaermesenkeClass.IstPufferZiel(StilleDb.Text(StilleDb.Feld(r, "WS_Ziel2"))))
                {
                    liste.Add(Eintrag(idAnlage, bezeichner, idType, true,
                                      StilleDb.Zahl(StilleDb.Feld(r, "WS_Ladeprio2")),
                                      StilleDb.Kommazahl(StilleDb.Feld(r, "WS_Ladegrenze2")),
                                      0, // die PV-Sonderregel hängt an der Hauptsenke
                                      kaskadenpos, anlagenprio));
                }
            }

            Sortieren(liste);
            ObergrenzenAufloesen(liste, idPuffer);
            return liste;
        }

        private static LadeEintrag Eintrag(int idAnlage, string bezeichner, int idType,
                                           bool zweitsenke, int ladeprioRoh, double ladegrenze,
                                           int ladeprioPV, int kaskadenpos, int anlagenprio)
        {
            LadeEintrag e = new LadeEintrag();
            e.ID_Anlage = idAnlage;
            e.Bezeichner = bezeichner;
            e.ID_Type = idType;
            e.Erzeuger = ErzeugerName(idType);
            e.Zweitsenke = zweitsenke;
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
        ///   Anlagen-ID → Hauptsenke vor Zweitsenke
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
                return a.Zweitsenke.CompareTo(b.Zweitsenke);
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
                StilleDb.Par("@id", OleDbType.Integer, idPuffer));
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
                StilleDb.Par("@id", OleDbType.Integer, idAnlage)));
            if (anlagenprio <= 0) anlagenprio = ANLAGENPRIO_UNGEPFLEGT;

            string bezeichner = StilleDb.Text(StilleDb.Scalar(
                "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idAnlage)));

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
                return Bezeichner + " (Prio " + Prio + (Manuell ? ", manuell" : ", automatisch") + ")";
            }
        }

        /// <summary>
        /// Automatikwert der Entladepriorität eines Speichers: die BESTE (kleinste)
        /// Ladepriorität, die an ihm anliegt (Konzept 3.6). Lädt ihn niemand, bleibt es
        /// bei <see cref="PRIO_SONSTIGE"/> — er wird dann zuletzt herangezogen.
        /// </summary>
        public static int EntladeprioAutomatik(int idProjekt, int idPuffer)
        {
            List<LadeEintrag> laden = Ladereihenfolge(idProjekt, idPuffer);
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
        /// </summary>
        public static List<EntladeEintrag> Entladereihenfolge(int idProjekt, string verwendung)
        {
            List<EntladeEintrag> liste = new List<EntladeEintrag>();
            if (idProjekt <= 0) return liste;

            string kanal = string.IsNullOrEmpty(verwendung)
                ? WaermesenkeClass.VERWENDUNG_HEIZUNG : verwendung;

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(idProjekt, kanal))
            {
                EntladeEintrag e = new EntladeEintrag();
                e.ID_Puffer = p.ID;
                e.Bezeichner = p.Bezeichner;
                e.Verwendung = WaermesenkeClass.WirksameVerwendung(p);

                int manuell = p.Entladeprio;
                e.Manuell = manuell >= PRIO_MIN && manuell <= PRIO_MAX;
                e.Prio = e.Manuell ? manuell : EntladeprioAutomatik(idProjekt, e.ID_Puffer);

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
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
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
