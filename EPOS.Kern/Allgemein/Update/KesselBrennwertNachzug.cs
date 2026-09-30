using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DAS BRENNWERTKENNZEICHEN DER PROJEKTKESSEL NACHZIEHEN - Etappe E2b des Konzepts
    // Dokumentation/ueberholt/Konzept_Kessel_Kennlinie_EPOS-Plan.md (Abschnitte 6 und 7;
    // Anwenderentscheid B-1 vom 30.09.2026: "Auch Projekte nachziehen").
    //
    // WOZU. Die Normvorgabe des Teillastwirkungsgrads (Konzept 7.1, seit E2 wirksam) richtet sich
    // nach der BAUART des Kessels, gelesen am Schalter Brennwert der PROJEKTKOPIE. Die Nachpflege
    // des Katalogs (E1, Entscheid F2) hat nur Tab_Heizkessel_STAMM gekennzeichnet; die Kopien in
    // Tab_Heizkessel tragen den Schalter nur dort, wo ihn der Katalogsatz beim Aufnehmen schon
    // hatte. Ein Brennwertgeraet rechnete deshalb als Niedertemperaturkessel.
    //
    // WAS. Brennwert = 1 in jeder Projektkopie, deren KATALOGSATZ ein Brennwertkessel ist - nach
    // seinem Schalter oder nach seiner Bauart in der Beschreibung (IstBrennwertkessel; so traegt die
    // Regel auch in einem Anwenderkatalog, den die Nachpflege aus E1 nicht erreicht hat). Den
    // Katalogsatz findet der Schritt auf dem Weg des Programms: ueber den BEZEICHNER
    // (HeizkesselCtrl.CopyFromStamm legt die Kopie unter dem Bezeichner des Katalogsatzes an und
    // findet ihn darueber wieder, DataRepository.GetIdByName). Tragen mehrere Katalogsaetze den
    // Bezeichner und widersprechen sie sich, entscheidet die Nennleistung auf 0,05 kW - die Regel
    // der Katalog-Nachpflege (KesselkatalogNachpflege.LEISTUNG_TOLERANZ_KW). Ohne eindeutigen
    // Katalogsatz gilt die Bauart in der Beschreibung der Kopie, wie beim Import
    // (HeizkesselImportSatz.IstBrennwert: "Brennwert" im Text).
    //
    // NUR SETZEN, NIE LOESCHEN. Ein Schalter, der steht, bleibt stehen - auch wenn der Katalog
    // ihn nicht fuehrt. Kennlinie_Brennwert wird nicht beruehrt (Konzept 3.1: gerechnet wird die
    // Brennwertkennlinie nur auf ausdrueckliche Wahl).
    //
    // WIEDERHOLBAR. Ein zweiter Lauf findet nichts mehr zu setzen. Was ohne Zuordnung bleibt,
    // steht benannt im Bericht.
    //
    // RECHENWIRKUNG. Jeder so gekennzeichnete Brennstoffkessel ohne eigenes eta30 rechnet ab dem
    // naechsten Lauf mit der Normvorgabe des Brennwertkessels (eta100 + 0,06 bis Hs/Hi) - das ist
    // der Zweck. Die Einfrierregel "gesaete Kesseldaten" greift: Die Basis ist neu eingefroren
    // (Referenzlaeufe/LIESMICH.md).
    //
    // VIER LESER: der Schemaschritt der Schale (WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs), die Paketanhebung (Paketanhebung, Stufe mit Umformung), das Werkzeug
    // Werkzeuge/Testdatenbankschema und die Testvorrichtung samt Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// <b>Der Schemaschritt „Brennwertkennzeichen der Projektkessel nachziehen“</b> (Konzept
    /// Kesselkennlinie, Etappe E2b; Anwenderentscheid B-1) — EINE Quelle für Migration,
    /// Paketanhebung, Werkzeug und Testvorrichtung. Anlass und Regel stehen im Kopf der Datei.
    /// </summary>
    public static class KesselBrennwertNachzug
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Sie folgt
        /// lückenlos auf <see cref="KonditionierungsvorlagenSaatSchema.SCHRITT"/>; wird der Schritt
        /// beim Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = KonditionierungsvorlagenSaatSchema.SCHRITT + 1;

        /// <summary>Die Projektkopien.</summary>
        public const string TAB_PROJEKT = SchemaKatalog.TAB_HEIZKESSEL;

        /// <summary>Der Katalog.</summary>
        public const string TAB_STAMM = SchemaKatalog.TAB_HEIZKESSEL_STAMM;

        /// <summary>Der Schalter, den der Schritt setzt.</summary>
        public const string SPALTE = "Brennwert";

        // Die Anweisungen AUSGESCHRIEBEN, damit Werkzeuge/SqlDialektPruefer jede als Ganzes sieht.

        /// <summary>Die Projektkopien ohne Brennwertkennzeichen.</summary>
        internal const string SQL_OHNE_KENNZEICHEN =
            "SELECT ID, ID_Projekt, Bezeichner, Beschreibung, Ptherm FROM Tab_Heizkessel " +
            "WHERE COALESCE(Brennwert, 0) = 0 ORDER BY ID";

        /// <summary>Die Katalogsätze unter einem Bezeichner — der Weg des Programms.</summary>
        internal const string SQL_KATALOG =
            "SELECT ID, Brennwert, Beschreibung, Ptherm FROM Tab_Heizkessel_STAMM WHERE Bezeichner = ? ORDER BY ID";

        /// <summary>Setzt das Kennzeichen einer Projektkopie — nur, wo es noch fehlt.</summary>
        internal const string SQL_SETZEN =
            "UPDATE Tab_Heizkessel SET Brennwert = 1 WHERE ID = ? AND COALESCE(Brennwert, 0) = 0";

        /// <summary>Woher ein Entscheid stammt.</summary>
        public enum Quelle
        {
            /// <summary>Der Katalogsatz unter dem Bezeichner der Kopie.</summary>
            Katalog = 0,

            /// <summary>Die Bauart in der Beschreibung der Kopie — ohne eindeutigen Katalogsatz.</summary>
            Beschreibung = 1
        }

        /// <summary>Ein Katalogsatz unter dem Bezeichner einer Projektkopie.</summary>
        public readonly struct Katalogsatz
        {
            public Katalogsatz(int id, bool brennwert, double? ptherm)
            {
                Id = id;
                Brennwert = brennwert;
                Ptherm = ptherm;
            }

            /// <summary><c>Tab_Heizkessel_STAMM.ID</c>.</summary>
            public int Id { get; }

            /// <summary>Ist der Katalogsatz ein Brennwertkessel (<see cref="IstBrennwertkessel"/>)?</summary>
            public bool Brennwert { get; }

            /// <summary>Nennleistung [kW]; <c>null</c> = leer.</summary>
            public double? Ptherm { get; }
        }

        /// <summary>Der Entscheid über eine Projektkopie.</summary>
        public readonly struct Entscheid
        {
            public Entscheid(bool setzen, Quelle quelle, bool katalogMehrdeutig)
            {
                Setzen = setzen;
                Herkunft = quelle;
                KatalogMehrdeutig = katalogMehrdeutig;
            }

            /// <summary>Wird <c>Brennwert</c> = 1 gesetzt?</summary>
            public bool Setzen { get; }

            /// <summary>Katalog oder Beschreibung.</summary>
            public Quelle Herkunft { get; }

            /// <summary>Gab es Katalogsätze, die sich auch nach der Leistung widersprachen?</summary>
            public bool KatalogMehrdeutig { get; }
        }

        /// <summary>
        /// <b>Die Regel</b> für eine Projektkopie ohne Kennzeichen — rein, ohne Datenbank:
        /// <list type="number">
        /// <item>Katalogsätze unter dem Bezeichner, die sich einig sind: Ihr Kennzeichen gilt.</item>
        /// <item>Widersprechen sie sich: nur die mit der Nennleistung der Kopie (auf
        /// <see cref="KesselkatalogNachpflege.LEISTUNG_TOLERANZ_KW"/>); sind die sich einig, gilt ihr
        /// Kennzeichen.</item>
        /// <item>Sonst — kein oder kein eindeutiger Katalogsatz — die Bauart in der Beschreibung
        /// (<see cref="HeizkesselImportSatz.IstBrennwert"/>).</item>
        /// </list>
        /// </summary>
        public static Entscheid Entscheiden(IReadOnlyList<Katalogsatz> katalog, double? ptherm, string beschreibung)
        {
            bool mehrdeutig = false;
            if (katalog != null && katalog.Count > 0)
            {
                bool? einig = Einig(katalog);
                if (einig.HasValue) return new Entscheid(einig.Value, Quelle.Katalog, false);

                if (ptherm.HasValue)
                {
                    var passend = katalog.Where(k => k.Ptherm.HasValue &&
                        Math.Abs(k.Ptherm.Value - ptherm.Value) <= KesselkatalogNachpflege.LEISTUNG_TOLERANZ_KW).ToList();
                    bool? einigNachLeistung = passend.Count > 0 ? Einig(passend) : null;
                    if (einigNachLeistung.HasValue) return new Entscheid(einigNachLeistung.Value, Quelle.Katalog, false);
                }
                mehrdeutig = true;
            }
            return new Entscheid(HeizkesselImportSatz.IstBrennwert(beschreibung), Quelle.Beschreibung, mehrdeutig);
        }

        /// <summary>Das gemeinsame Kennzeichen der Sätze; <c>null</c>, wenn sie sich widersprechen.</summary>
        private static bool? Einig(IReadOnlyList<Katalogsatz> saetze)
        {
            bool erstes = saetze[0].Brennwert;
            for (int i = 1; i < saetze.Count; i++)
                if (saetze[i].Brennwert != erstes) return null;
            return erstes;
        }

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Geprüfte Projektkopien ohne Kennzeichen.</summary>
            public int Geprueft { get; internal set; }

            /// <summary>Gesetzt, weil der Katalogsatz ein Brennwertkessel ist.</summary>
            public List<string> AusKatalog { get; } = new List<string>();

            /// <summary>Gesetzt nach der Beschreibung — ohne (eindeutigen) Katalogsatz.</summary>
            public List<string> AusBeschreibung { get; } = new List<string>();

            /// <summary>Ohne (eindeutigen) Katalogsatz und ohne Brennwert in der Beschreibung — bleibt.</summary>
            public List<string> OhneZuordnung { get; } = new List<string>();

            /// <summary>Katalogsätze unter demselben Bezeichner widersprechen sich (auch nach der Leistung).</summary>
            public List<string> Mehrdeutig { get; } = new List<string>();

            /// <summary>Zahl der gesetzten Kennzeichen.</summary>
            public int Gesetzt => AusKatalog.Count + AusBeschreibung.Count;

            /// <summary>Die Zeilen für Protokoll und Werkzeug.</summary>
            public IEnumerable<string> Zeilen()
            {
                yield return Gesetzt.ToString(CultureInfo.InvariantCulture) + " von " +
                             Geprueft.ToString(CultureInfo.InvariantCulture) +
                             " Projektkessel(n) ohne Kennzeichen als Brennwertkessel gekennzeichnet (" +
                             AusKatalog.Count.ToString(CultureInfo.InvariantCulture) + " nach dem Katalogsatz, " +
                             AusBeschreibung.Count.ToString(CultureInfo.InvariantCulture) + " nach der Beschreibung)";
                foreach (string s in AusBeschreibung)
                    yield return "nach der Beschreibung gekennzeichnet (kein eindeutiger Katalogsatz): " + s;
                foreach (string s in Mehrdeutig)
                    yield return "Katalogsaetze unter dem Bezeichner widersprechen sich: " + s;
                foreach (string s in OhneZuordnung)
                    yield return "ohne Zuordnung, bleibt ohne Kennzeichen: " + s;
            }
        }

        /// <summary>
        /// Zieht das Kennzeichen in der Datenbank des Anwenders nach (Migration, Werkzeug,
        /// Testvorrichtung). <paramref name="bericht"/> nimmt die Zeilen auf und darf <c>null</c> sein.
        /// Setzt Schritt <see cref="KesselKennlinieSchema.SCHRITT"/> nicht voraus, nur die beiden
        /// Kesseltabellen mit <c>Brennwert</c>. Fehler werfen — der Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
            => Ausfuehren(Umformzugriff.Datenbank, Umformzugriff.Datenbank, bericht);

        /// <summary>
        /// Zieht das Kennzeichen an <paramref name="projekt"/> nach; die Katalogsätze kommen aus
        /// <paramref name="katalog"/>. Beim Anheben eines Pakets ist das die Arbeitsdatenbank des
        /// Pakets bzw. die Datenbank des Ziels — dort sucht das Programm den Katalogsatz nach dem
        /// Einspielen. Fehlt der Katalog, gilt für jede Kopie die Beschreibung.
        /// </summary>
        internal static Bericht Ausfuehren(Umformzugriff projekt, Umformzugriff katalog, IList<string> bericht)
        {
            if (projekt == null) throw new ArgumentNullException(nameof(projekt));
            var b = new Bericht();
            if (!projekt.SpalteVorhanden(TAB_PROJEKT, SPALTE)) return b;

            bool mitKatalog = KatalogLesbar(katalog);

            DataTable kopien = projekt.Lesen(SQL_OHNE_KENNZEICHEN);
            if (kopien == null)
                throw new InvalidOperationException("Die Projektkessel (" + TAB_PROJEKT + ") liessen sich nicht lesen.");

            foreach (DataRow r in kopien.Rows)
            {
                b.Geprueft++;
                int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                string bezeichner = Text(r["Bezeichner"]);
                string beschreibung = Text(r["Beschreibung"]);
                double? ptherm = Zahl(r["Ptherm"]);
                string name = "Projekt " + Text(r["ID_Projekt"]) + ", Kessel " + id.ToString(CultureInfo.InvariantCulture) +
                              " \"" + bezeichner + "\" (" + (string.IsNullOrEmpty(beschreibung) ? "ohne Beschreibung" : beschreibung) + ")";

                IReadOnlyList<Katalogsatz> treffer = mitKatalog ? KatalogLesen(katalog, bezeichner) : Array.Empty<Katalogsatz>();
                Entscheid e = Entscheiden(treffer, ptherm, beschreibung);
                if (e.KatalogMehrdeutig) b.Mehrdeutig.Add(name);

                if (!e.Setzen)
                {
                    if (e.Herkunft == Quelle.Beschreibung) b.OhneZuordnung.Add(name);
                    continue;
                }

                projekt.Ausfuehren(SQL_SETZEN, new DbParam("@id", id));
                if (e.Herkunft == Quelle.Katalog) b.AusKatalog.Add(name);
                else b.AusBeschreibung.Add(name);
            }

            if (bericht != null)
                foreach (string z in b.Zeilen()) bericht.Add(z);
            return b;
        }

        /// <summary>
        /// Steht der Schritt? Keine Projektkopie ohne Kennzeichen, die nach der Regel eines
        /// bekäme — dieselbe Regel, nur lesend.
        /// </summary>
        public static bool Vollstaendig()
        {
            Umformzugriff db = Umformzugriff.Datenbank;
            if (!db.SpalteVorhanden(TAB_PROJEKT, SPALTE)) return false;
            bool mitKatalog = KatalogLesbar(db);
            DataTable kopien = db.Lesen(SQL_OHNE_KENNZEICHEN);
            if (kopien == null) return false;
            foreach (DataRow r in kopien.Rows)
            {
                IReadOnlyList<Katalogsatz> treffer = mitKatalog ? KatalogLesen(db, Text(r["Bezeichner"])) : Array.Empty<Katalogsatz>();
                if (Entscheiden(treffer, Zahl(r["Ptherm"]), Text(r["Beschreibung"])).Setzen) return false;
            }
            return true;
        }

        /// <summary>
        /// Ist ein KATALOGSATZ ein Brennwertkessel? Sein Schalter <c>Brennwert</c> oder seine Bauart in
        /// der Beschreibung (<see cref="HeizkesselImportSatz.IstBrennwert"/>) — dieselbe Regel, mit der
        /// Import und Katalog-Nachpflege den Schalter aus der VDI-Bauart setzen. So findet der Schritt
        /// das Brennwertgerät auch in einem Katalog, den die Nachpflege (E1) noch nicht erreicht hat.
        /// </summary>
        public static bool IstBrennwertkessel(bool schalter, string beschreibung)
            => schalter || HeizkesselImportSatz.IstBrennwert(beschreibung);

        private static bool KatalogLesbar(Umformzugriff katalog)
        {
            if (katalog == null) return false;
            try
            {
                return katalog.SpalteVorhanden(TAB_STAMM, SPALTE) && katalog.SpalteVorhanden(TAB_STAMM, "Ptherm") &&
                       katalog.SpalteVorhanden(TAB_STAMM, "Beschreibung");
            }
            catch (Exception) { return false; }
        }

        private static IReadOnlyList<Katalogsatz> KatalogLesen(Umformzugriff katalog, string bezeichner)
        {
            DataTable dt = katalog.Lesen(SQL_KATALOG, new DbParam("@bez", bezeichner ?? ""));
            if (dt == null || dt.Rows.Count == 0) return Array.Empty<Katalogsatz>();
            var saetze = new List<Katalogsatz>(dt.Rows.Count);
            foreach (DataRow k in dt.Rows)
                saetze.Add(new Katalogsatz(Convert.ToInt32(k["ID"], CultureInfo.InvariantCulture),
                                           IstBrennwertkessel(Wahr(k["Brennwert"]), Text(k["Beschreibung"])),
                                           Zahl(k["Ptherm"])));
            return saetze;
        }

        private static string Text(object o) =>
            o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);

        private static double? Zahl(object o)
        {
            if (o == null || o == DBNull.Value) return null;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch (Exception) { return null; }
        }

        private static bool Wahr(object o)
        {
            if (o == null || o == DBNull.Value) return false;
            if (o is bool b) return b;
            try { return Convert.ToInt64(o, CultureInfo.InvariantCulture) != 0; }
            catch (Exception) { return false; }
        }
    }
}
