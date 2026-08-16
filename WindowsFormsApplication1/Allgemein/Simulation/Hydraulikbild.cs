using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE D4 — die hydraulische Verschaltung eines Projekts, EINMAL aus der
    /// Datenbank gelesen und für alle Auswerter dieselbe.
    ///
    /// <b>Warum diese Klasse entsteht.</b> Die Ableitung „welche Anlage lädt welchen
    /// Puffer, welche Anlage bezieht aus welchem Puffer" stand bis D5b als lokale
    /// Rechnung IN <c>WaermesenkeClass.RingMeldung</c>. Die Schema-Ansicht braucht
    /// genau diese Abbildung ein zweites Mal, und der Restpunkt 2 aus
    /// <c>D5b_DialogFreischaltung_Protokoll.md</c> hält ausdrücklich fest, dass sie
    /// „sich für die Kettenbildung wiederverwenden lässt, statt sie ein drittes Mal zu
    /// schreiben". Also wandert sie hierher; <see cref="WaermesenkeClass.RingMeldung"/>
    /// ruft sie auf und rechnet nichts mehr selbst.
    ///
    /// <b>Verhalten unverändert.</b> Abfrage, Reihenfolge der Zeilen, die Bedingung
    /// „lädt" (Puffer-ID auf einem Senkenfeld UND ein Puffer-Ziel dazu), die
    /// Einschränkung auf Wärmepumpe und Heizkessel (Befund E-K2-2) und die
    /// Ebenen-Relaxation sind Zeile für Zeile die aus D5b. Die Abfrage holt nur
    /// zusätzliche SPALTEN — dieselben Zeilen in derselben Ordnung.
    ///
    /// <b>Zwei Auflösungen der Quellidentität, mit Absicht.</b>
    /// <list type="bullet">
    ///   <item><description><see cref="QuelleJeAnlage"/> — ausschließlich der
    ///     Fremdschlüssel <c>WQ_ID_Puffer</c>. Das ist die ENGINE-Wahrheit:
    ///     <c>SimulationControl.QuellbezuegeAufbauen</c> verlangt ihn &gt; 0, sonst
    ///     entsteht gar kein Quellbezug. Ring- und Ebenenrechnung laufen darüber, damit
    ///     der Dialog exakt das prüft, woran die Engine später scheitern würde.</description></item>
    ///   <item><description><see cref="QuellpufferAnzeige"/> — Fremdschlüssel, sonst der
    ///     Alt-Bezeichner gegen die Projekt-Puffer (kleinste ID, wie
    ///     <see cref="WaermesenkeClass.QuellPufferDerAnlage"/>). Das ist die
    ///     ANZEIGE-Wahrheit der Erzeugerkarte; Liste und Schema müssen dasselbe zeigen.
    ///     <see cref="NurBezeichner"/> macht den Unterschied sichtbar.</description></item>
    /// </list>
    ///
    /// <b>Invariante S-1</b> (Konzept Abschnitt 5): Gefragt wird ausschließlich
    /// <c>Tab_Energieanlagen</c>. Quell- und Senkenbezüge existieren nur dort, nie an
    /// <c>Tab_Pufferspeicher</c> — ein Speicher kann in dieser Abbildung strukturell
    /// weder Lader noch Quellnutzer eines anderen Speichers sein.
    /// </summary>
    public sealed class Hydraulikbild
    {
        /// <summary>Eine Wärmeerzeuger-Anlage des Projekts mit ihren Quell- und Senkenfeldern.</summary>
        public sealed class AnlagenEintrag
        {
            /// <summary>Tab_Energieanlagen.ID.</summary>
            public int ID;

            /// <summary>1 WP, 2 Solarthermie, 10 Heizkessel, 11 BHKW.</summary>
            public int ID_Type;

            public string Bezeichner = "";

            /// <summary>Einsatzreihenfolge innerhalb der Erzeugerart (0 = nicht gesetzt).</summary>
            public int Prioritaet;

            /// <summary>WQ_Typ — Steuerwert der Wärmequelle (<c>WaermequelleClass.TYP_*</c>).</summary>
            public string WQ_Typ = "";

            /// <summary>WQ_Temp — konstante Quelltemperatur [°C].</summary>
            public double WQ_Temp;

            /// <summary>WQ_ID_Puffer — Fremdschlüssel des Quellpuffers, roh; 0 = leer.</summary>
            public int WQ_ID_Puffer;

            /// <summary>WQ_Puffer — Alt-Bezeichner des Quellpuffers; "" = leer.</summary>
            public string WQ_Puffer = "";

            /// <summary>Bauart der Wärmepumpe (Tab_WP.Typ); "" bei jeder anderen Art.</summary>
            public string WpTyp = "";

            /// <summary>Auslegungstemperaturen der ANLAGE; 0 = nicht gepflegt.</summary>
            public int Vorlauf;
            public int Ruecklauf;

            /// <summary>Haupt- und Zweitsenke (Konzept 5.3), bereits normalisiert.</summary>
            public WaermesenkeClass.SenkeDaten Senke = new WaermesenkeClass.SenkeDaten();

            /// <summary>true = diese Erzeugerart darf überhaupt eine Wärmequelle wählen.</summary>
            public bool QuellenwahlMoeglich
            {
                get { return WaermequelleClass.QuellenwahlMoeglich(ID_Type); }
            }
        }

        /// <summary>Alle Wärmeerzeuger-Anlagen des Projekts, sortiert wie die Datenbank sie liefert.</summary>
        public readonly List<AnlagenEintrag> Anlagen = new List<AnlagenEintrag>();

        /// <summary>Anlage je ID — der Zugriff, den die Auswerter brauchen.</summary>
        public readonly Dictionary<int, AnlagenEintrag> JeId = new Dictionary<int, AnlagenEintrag>();

        /// <summary>Anlagenname je ID (kann leer sein — dann steht die ID als Ersatz da).</summary>
        public readonly Dictionary<int, string> NameJeAnlage = new Dictionary<int, string>();

        /// <summary>
        /// Anlage → Quellpuffer, ausschließlich über den Fremdschlüssel und nur für
        /// Arten mit Quellenwahl. Das ist die Menge, mit der die Engine rechnet.
        /// </summary>
        public readonly Dictionary<int, int> QuelleJeAnlage = new Dictionary<int, int>();

        /// <summary>
        /// Puffer → Anlagen, die ihn LADEN. Bedingung wie in
        /// <c>Ladeordnung.Ladereihenfolge</c>: Puffer-ID auf einem der beiden
        /// Senkenfelder UND ein Puffer-Ziel dazu.
        /// </summary>
        public readonly Dictionary<int, List<int>> LaderJePuffer = new Dictionary<int, List<int>>();

        /// <summary>Projekt, aus dem das Bild stammt.</summary>
        public int ID_Projekt { get; private set; }

        // --- Lesen --------------------------------------------------------------------

        /// <summary>
        /// Liest die Verschaltung eines Projekts; <c>null</c>, wenn die Abfrage
        /// scheitert (dieselbe Rückgabe wie die Vorgängerrechnung in
        /// <c>RingMeldung</c>, damit ihr Aufrufer unverändert bleibt).
        /// </summary>
        public static Hydraulikbild Lesen(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            // Spaltenauswahl und Sortierung wie in Form_Simulation_Config.AnlagenImProjekt:
            // Die Rücklaufspalte trägt in Tab_Energieanlagen den UMLAUT (Befund B0-4,
            // siehe ProjektPuffer.SQL_SYSTEM_RUECKLAUF), der LEFT JOIN auf Tab_WP liefert
            // die Bauart für die Quellenanzeige der Wärmepumpe.
            DataTable dt = StilleDb.Tabelle(
                "SELECT a.ID, a.ID_Type, a.Bezeichner, a.Prioritaet, " +
                "       a.Vorlauf, a.[Rücklauf] AS Ruecklauf, " +
                "       a.WQ_Typ, a.WQ_Temp, a.WQ_ID_Puffer, a.WQ_Puffer, " +
                "       a.WS_Ziel, a.WS_ID_Puffer, a.WS_Typ, a.WS_Ladeprio, a.WS_Ladegrenze, a.WS_Ladeprio_PV, " +
                "       a.WS_Ziel2, a.WS_ID_Puffer2, a.WS_Ladeprio2, a.WS_Ladegrenze2, " +
                "       w.Typ AS WPTyp " +
                "FROM Tab_Energieanlagen AS a LEFT JOIN Tab_WP AS w ON a.ID_WP = w.ID " +
                "WHERE a.ID_Projekt = ? AND a.ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ") " +
                "ORDER BY a.Prioritaet, a.ID",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return null;

            Hydraulikbild bild = new Hydraulikbild();
            bild.ID_Projekt = idProjekt;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (id <= 0) continue;

                AnlagenEintrag a = new AnlagenEintrag();
                a.ID = id;
                a.ID_Type = StilleDb.Zahl(StilleDb.Feld(r, "ID_Type"));
                a.Bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                a.Prioritaet = StilleDb.Zahl(StilleDb.Feld(r, "Prioritaet"));
                a.Vorlauf = StilleDb.Zahl(StilleDb.Feld(r, "Vorlauf"));
                a.Ruecklauf = StilleDb.Zahl(StilleDb.Feld(r, "Ruecklauf"));
                a.WpTyp = StilleDb.Text(StilleDb.Feld(r, "WPTyp"));
                a.WQ_Typ = StilleDb.Text(StilleDb.Feld(r, "WQ_Typ"));
                a.WQ_Temp = StilleDb.Kommazahl(StilleDb.Feld(r, "WQ_Temp"));
                a.WQ_ID_Puffer = StilleDb.Zahl(StilleDb.Feld(r, "WQ_ID_Puffer"));
                a.WQ_Puffer = StilleDb.Text(StilleDb.Feld(r, "WQ_Puffer"));
                a.Senke = WaermesenkeClass.AusDatenzeile(r);

                bild.Anlagen.Add(a);
                bild.JeId[id] = a;
                bild.NameJeAnlage[id] = a.Bezeichner;

                // Quellbezug NUR über den Fremdschlüssel und nur bei WP/Kessel — die
                // Grenze, die die Engine seit der D5a-Nacharbeit zieht (E-K2-2).
                if (a.QuellenwahlMoeglich &&
                    string.Equals(a.WQ_Typ, WaermequelleClass.TYP_PUFFER, StringComparison.Ordinal) &&
                    a.WQ_ID_Puffer > 0)
                    bild.QuelleJeAnlage[id] = a.WQ_ID_Puffer;

                bild.LaderEintragen(id, a.Senke.Ziel, a.Senke.ID_Puffer);
                bild.LaderEintragen(id, a.Senke.Ziel2, a.Senke.ID_Puffer2);
            }

            return bild;
        }

        /// <summary>Trägt eine Anlage als LADER eines Puffers ein (Bedingung wie Ladeordnung).</summary>
        private void LaderEintragen(int idAnlage, string ziel, int idPuffer)
        {
            if (idPuffer <= 0 || !WaermesenkeClass.IstPufferZiel(ziel)) return;

            List<int> lader;
            if (!LaderJePuffer.TryGetValue(idPuffer, out lader))
            {
                lader = new List<int>();
                LaderJePuffer[idPuffer] = lader;
            }
            if (!lader.Contains(idAnlage)) lader.Add(idAnlage);
        }

        /// <summary>true, wenn die Anlage zu diesem Projekt gehört.</summary>
        public bool KenntAnlage(int idAnlage)
        {
            return NameJeAnlage.ContainsKey(idAnlage);
        }

        /// <summary>Anlagenname; die ID als Ersatz, wenn kein Bezeichner gepflegt ist.</summary>
        public string Name(int idAnlage)
        {
            string name;
            if (NameJeAnlage.TryGetValue(idAnlage, out name) && name.Length > 0) return name;
            return idAnlage.ToString();
        }

        /// <summary>Die Anlagen, die einen Puffer laden — nie <c>null</c>.</summary>
        public List<int> Lader(int idPuffer)
        {
            List<int> lader;
            if (LaderJePuffer.TryGetValue(idPuffer, out lader)) return lader;
            return new List<int>();
        }

        // --- Quellidentität für die ANZEIGE -------------------------------------------

        /// <summary>
        /// Quellpuffer einer Anlage für die ANZEIGE: Fremdschlüssel, sonst der
        /// Alt-Bezeichner gegen die übergebene Projekt-Pufferliste (kleinste ID, wie
        /// <see cref="WaermesenkeClass.QuellPufferDerAnlage"/>). 0 = kein Quellpuffer.
        ///
        /// <paramref name="projektPuffer"/> ist die ohnehin geladene Pufferliste des
        /// Projekts — die Auflösung kostet damit keine zusätzliche Abfrage.
        /// </summary>
        public int QuellpufferAnzeige(int idAnlage, List<WaermesenkeClass.PufferInfo> projektPuffer)
        {
            AnlagenEintrag a;
            if (!JeId.TryGetValue(idAnlage, out a)) return 0;
            if (!string.Equals(a.WQ_Typ, WaermequelleClass.TYP_PUFFER, StringComparison.Ordinal)) return 0;
            if (a.WQ_ID_Puffer > 0) return a.WQ_ID_Puffer;
            if (a.WQ_Puffer.Length == 0 || projektPuffer == null) return 0;

            int treffer = 0;
            foreach (WaermesenkeClass.PufferInfo p in projektPuffer)
            {
                if (p == null) continue;
                if (!string.Equals(p.Bezeichner, a.WQ_Puffer, StringComparison.OrdinalIgnoreCase)) continue;
                if (treffer == 0 || p.ID < treffer) treffer = p.ID;
            }
            return treffer;
        }

        /// <summary>
        /// true, wenn der Quellpuffer dieser Anlage NUR über den Alt-Bezeichner
        /// auflösbar ist — die Engine baut dann keinen Quellbezug auf (E0, D5b 1c).
        /// </summary>
        public bool NurBezeichner(int idAnlage)
        {
            AnlagenEintrag a;
            if (!JeId.TryGetValue(idAnlage, out a)) return false;
            return string.Equals(a.WQ_Typ, WaermequelleClass.TYP_PUFFER, StringComparison.Ordinal) &&
                   a.WQ_ID_Puffer <= 0 && a.WQ_Puffer.Length > 0;
        }

        // --- Ebenen-Relaxation (aus WaermesenkeClass.RingMeldung übernommen) -----------

        /// <summary>
        /// Rechenebenen der Kaskade nach der Relaxation aus
        /// <c>Kaskadenschleife.EbenenRelaxieren</c>:
        /// <c>Ebene(A) = 1 + max{ Ebene(L) : L lädt den Quellpuffer von A }</c>, iterativ.
        /// Was nach so vielen Runden noch wächst, wie es Anlagen gibt, kann nur ein Ring
        /// sein — dann steht <paramref name="ring"/> auf true.
        ///
        /// <paramref name="idAnlageErsatz"/>/<paramref name="idQuellPufferErsatz"/> setzen
        /// EINEN Quellbezug probeweise, bevor gerechnet wird: So prüft der Dialog den
        /// Zustand NACH dem Speichern. Beide 0 = der gespeicherte Bestand.
        ///
        /// Der Selbstbezug (die Anlage lädt ihren eigenen Quellpuffer) ist übersprungen —
        /// das ist der Kurzschluss aus Konzept 4.6, den
        /// <see cref="WaermesenkeClass.KurzschlussMeldung"/> mit eigenem Text abfängt.
        /// </summary>
        public Dictionary<int, int> Ebenen(int idAnlageErsatz, int idQuellPufferErsatz, out bool ring)
        {
            ring = false;

            Dictionary<int, int> quellen = new Dictionary<int, int>(QuelleJeAnlage);
            if (idAnlageErsatz > 0 && idQuellPufferErsatz > 0)
                quellen[idAnlageErsatz] = idQuellPufferErsatz;

            Dictionary<int, int> ebene = new Dictionary<int, int>();
            foreach (int id in NameJeAnlage.Keys) ebene[id] = 0;

            for (int runde = 0; runde <= ebene.Count; runde++)
            {
                bool geaendert = false;

                foreach (KeyValuePair<int, int> bezug in quellen)
                {
                    if (!ebene.ContainsKey(bezug.Key)) continue;

                    List<int> lader;
                    if (!LaderJePuffer.TryGetValue(bezug.Value, out lader)) continue;

                    int soll = 0;
                    foreach (int idLader in lader)
                    {
                        if (idLader == bezug.Key) continue;      // Kurzschluss, nicht Ring

                        int e;
                        if (!ebene.TryGetValue(idLader, out e)) continue;
                        if (e + 1 > soll) soll = e + 1;
                    }

                    if (soll > ebene[bezug.Key]) { ebene[bezug.Key] = soll; geaendert = true; }
                }

                if (!geaendert) return ebene;                    // zyklenfrei
            }

            ring = true;
            return ebene;
        }

        /// <summary>
        /// Die Anlagen, die im Ring stecken, als lesbare Aufzählung — dieselbe Auswahl
        /// wie in <c>Kaskadenschleife.ZyklusMeldung</c>: die mit der höchsten erreichten
        /// Ebene, denn nur die sind unbegrenzt gewachsen.
        /// </summary>
        public string RingBeteiligte(Dictionary<int, int> ebene,
                                     int idAnlageErsatz, int idQuellPufferErsatz)
        {
            Dictionary<int, int> quellen = new Dictionary<int, int>(QuelleJeAnlage);
            if (idAnlageErsatz > 0 && idQuellPufferErsatz > 0)
                quellen[idAnlageErsatz] = idQuellPufferErsatz;

            int hoechste = 0;
            foreach (KeyValuePair<int, int> e in ebene)
                if (e.Value > hoechste) hoechste = e.Value;

            List<string> beteiligt = new List<string>();
            foreach (KeyValuePair<int, int> bezug in quellen)
            {
                int stufe;
                if (!ebene.TryGetValue(bezug.Key, out stufe) || stufe < hoechste) continue;
                if (!LaderJePuffer.ContainsKey(bezug.Value)) continue;

                string puffer = WaermesenkeClass.PufferName(bezug.Value);
                if (puffer.Length == 0) puffer = MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER;

                beteiligt.Add(string.Format(MyResource.Resource.SIM_QUELLE_BETEILIGT,
                                            Name(bezug.Key), puffer));
            }

            return beteiligt.Count > 0 ? string.Join(" · ", beteiligt.ToArray()) : "–";
        }
    }
}
