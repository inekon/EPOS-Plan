using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Schreib- und Prüfschicht für die Kostenpositionen eines Projekts
    /// (<c>Tab_ProjektWerte</c> + Katalog <c>Tab_Kostenfaktor</c>).
    ///
    /// <para>
    /// <b>Warum getrennt von <see cref="Form_Kosten"/>.</b> Dieselben Regeln brauchen drei
    /// Aufrufer: die Kostenverwaltung (Anlegen und Übernehmen), die Kostenseite von
    /// „Berichte &amp; Kosten" (nur Abweichungsanzeige) und der Übernahmedialog
    /// <see cref="Form_PlanwertUebernahme"/>. Eine zweite Kopie der Regeln wäre genau die
    /// Sorte Doppelpflege, an der die Kostenseite schon einmal auseinandergelaufen ist
    /// (Befund D1).
    /// </para>
    ///
    /// <para>
    /// <b>Unterpositionen brauchen keine Schemaänderung.</b> <c>Tab_ProjektWerte</c> trägt
    /// keinen eigenen Bezeichnungstext — der Name einer Position steht in
    /// <c>Tab_Kostenfaktor.Bezeichnung</c>, die Zugehörigkeit zur Komponente in
    /// <c>KomponentenID</c>, die Rolle in <c>Tab_Kostenfaktor.IsMainComponent</c> und die
    /// Bündelung in <c>Tab_ProjektWerte.Gruppe</c>. Eine Nebenkostenzeile ist deshalb
    /// schlicht eine weitere Zeile mit derselben <c>KomponentenID</c>, derselben
    /// <c>Gruppe</c> und einem eigenen Katalogeintrag mit
    /// <c>IsMainComponent = False</c>. <c>Tab_Kostenfaktor.StammID</c> ist ein AutoWert,
    /// fehlende Katalogeinträge entstehen darum beim ersten Bedarf — dasselbe
    /// „Lern"-Muster, das <c>Form_Kosten.AddKostenItem</c> für
    /// <c>Tab_KostenGruppenKatalog</c> schon verwendet.
    /// </para>
    /// </summary>
    internal static class KostenPositionCtrl
    {
        /// <summary>Toleranz des Betragsvergleichs — ein halber Cent.</summary>
        private const double EPS = 0.005;

        // ------------------------------------------------------------------- Katalog

        /// <summary>
        /// <c>StammID</c> der Hauptposition einer Komponente.
        /// </summary>
        /// <remarks>
        /// Bewusst <c>MIN(StammID)</c> über die Bezeichnung statt einer festen ID: Der
        /// Katalog führt für „Solarthermie" zwei Hauptpositionszeilen (82 und 84), und
        /// Bestandsprojekte verwenden beide (Befund D4).
        /// </remarks>
        internal static int StammIdHaupt(string komponente)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(StammID) FROM Tab_Kostenfaktor " +
                "WHERE Bezeichnung = ? AND IsMainComponent = True",
                new OleDbParameter("@b", komponente ?? ""));
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 0;
        }

        /// <summary>
        /// <c>StammID</c> einer Nebenposition; legt den Katalogeintrag an, wenn er fehlt.
        /// 0 = konnte weder gefunden noch angelegt werden.
        /// </summary>
        internal static int StammIdNeben(string bezeichnung)
        {
            if (string.IsNullOrEmpty(bezeichnung)) return 0;

            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(StammID) FROM Tab_Kostenfaktor " +
                "WHERE Bezeichnung = ? AND IsMainComponent = False",
                new OleDbParameter("@b", bezeichnung));
            if (o != null && o != DBNull.Value) return Convert.ToInt32(o);

            try
            {
                // StammID ist ein AutoWert — nur Bezeichnung und Rolle setzen.
                DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Kostenfaktor (Bezeichnung, IsMainComponent) VALUES (?, ?)",
                    new OleDbParameter("@b", bezeichnung),
                    new OleDbParameter("@m", OleDbType.Boolean) { Value = false });
            }
            catch { return 0; }

            o = DataRepository.ExecuteScalar(
                "SELECT MIN(StammID) FROM Tab_Kostenfaktor " +
                "WHERE Bezeichnung = ? AND IsMainComponent = False",
                new OleDbParameter("@b", bezeichnung));
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 0;
        }

        /// <summary>
        /// Nimmt eine Gruppe in <c>Tab_KostenGruppenKatalog</c> auf, falls sie fehlt.
        /// Ohne diesen Eintrag verliert <c>Abfrage_ProjektKostenInvestBetrieb</c> die
        /// Zeile (INNER JOIN über den Gruppennamen).
        /// </summary>
        internal static void GruppeSichern(string gruppe)
        {
            if (string.IsNullOrEmpty(gruppe)) return;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_KostenGruppenKatalog WHERE GruppenName = ?",
                    new OleDbParameter("@g", gruppe));
                if (o != null && o != DBNull.Value && Convert.ToInt32(o) > 0) return;

                DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_KostenGruppenKatalog (GruppenName) VALUES (?)",
                    new OleDbParameter("@g", gruppe));
            }
            catch { }
        }

        // ------------------------------------------------------------- Projektwerte

        /// <summary>
        /// ID der HAUPTPOSITION einer Komponente, oder 0.
        /// </summary>
        /// <remarks>
        /// Gesucht wird über <c>Tab_Kostenfaktor.Bezeichnung</c> + <c>IsMainComponent</c>,
        /// nicht über eine feste <c>StammID</c> — aus demselben Grund wie bei
        /// <see cref="StammIdHaupt"/>: „Solarthermie" hat zwei Hauptpositionszeilen im
        /// Katalog (82 und 84), und Bestandsprojekte verwenden beide. Ein Vergleich gegen
        /// nur eine der beiden würde für die andere Hälfte der Projekte eine zweite
        /// Hauptposition anlegen (Befund D4).
        /// </remarks>
        internal static int FindeHauptposition(int projektID, int kategorieID, int komponentenID,
                                               string komponente)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(w.ID) FROM Tab_ProjektWerte AS w " +
                "     INNER JOIN Tab_Kostenfaktor AS f ON w.StammID = f.StammID " +
                "WHERE w.ProjektID = ? AND w.KategorieID = ? AND w.KomponentenID = ? " +
                "      AND f.IsMainComponent = True AND f.Bezeichnung = ?",
                new OleDbParameter("@p", projektID),
                new OleDbParameter("@k", kategorieID),
                new OleDbParameter("@c", komponentenID),
                new OleDbParameter("@b", komponente ?? ""));
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 0;
        }

        /// <summary>Setzt den Betrag einer vorhandenen Position (Primärschlüssel).</summary>
        internal static bool SetzeBetragNachId(int positionsID, double betrag)
        {
            if (positionsID <= 0) return false;
            return DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = ? WHERE ID = ?",
                new OleDbParameter("@v", betrag),
                new OleDbParameter("@id", positionsID));
        }

        /// <summary>ID der Projektposition, oder 0.</summary>
        internal static int FindePosition(int projektID, int kategorieID, int komponentenID, int stammID)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KategorieID = ? AND KomponentenID = ? AND StammID = ?",
                new OleDbParameter("@p", projektID),
                new OleDbParameter("@k", kategorieID),
                new OleDbParameter("@c", komponentenID),
                new OleDbParameter("@s", stammID));
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 0;
        }

        /// <summary>Aktuell erfasster Betrag einer Position (0, wenn sie fehlt).</summary>
        internal static double LiesBetrag(int positionsID)
        {
            if (positionsID <= 0) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT EingegebenerWert FROM Tab_ProjektWerte WHERE ID = ?",
                new OleDbParameter("@id", positionsID));
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }

        /// <summary>
        /// Legt eine Position an oder setzt ihren Betrag. Rückgabe: ID der Position, 0 bei
        /// Fehlschlag. Angelegt wird nur, wenn <paramref name="anlegenWennFehlt"/> gilt —
        /// so entsteht keine Zeile für einen Nebenposten, den es nicht (mehr) gibt.
        /// </summary>
        internal static int SetzeBetrag(int projektID, int kategorieID, int komponentenID,
                                        int stammID, double betrag, string gruppe,
                                        bool anlegenWennFehlt)
        {
            if (stammID <= 0 || komponentenID <= 0) return 0;

            int id = FindePosition(projektID, kategorieID, komponentenID, stammID);
            if (id > 0)
            {
                // Aktualisieren über den Primärschlüssel — nie über die Merkmalskombination
                // (mehrere Zeilen desselben Faktors sind im Bestand möglich).
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET EingegebenerWert = ? WHERE ID = ?",
                    new OleDbParameter("@v", betrag),
                    new OleDbParameter("@id", id));
                return id;
            }

            if (!anlegenWennFehlt) return 0;

            GruppeSichern(gruppe);
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, " +
                "EingegebenerWert, Nutzungsdauer, Einheit, Gruppe) VALUES (?, ?, ?, ?, ?, 0, ?, ?)",
                new OleDbParameter("@p", projektID),
                new OleDbParameter("@s", stammID),
                new OleDbParameter("@c", komponentenID),
                new OleDbParameter("@k", kategorieID),
                new OleDbParameter("@v", betrag),
                new OleDbParameter("@e", DbWerte.KOSTEN_EINHEIT_EURO),
                new OleDbParameter("@g", gruppe ?? DbWerte.KOSTEN_GRUPPE_ALLGEMEIN));

            return FindePosition(projektID, kategorieID, komponentenID, stammID);
        }

        /// <summary>Wie <see cref="SchreibeNebenkosten"/> mit vorhandenen Zeilen umgeht.</summary>
        internal enum Nebenmodus
        {
            /// <summary>
            /// Fehlende Zeilen anlegen, vorhandene UNBERÜHRT lassen — der Modus des
            /// bloßen Anwählens einer Komponente. So entstehen beim zweiten Öffnen weder
            /// Dubletten noch überschriebene Anwenderwerte.
            /// </summary>
            NurAnlegen,

            /// <summary>
            /// Fehlende anlegen UND vorhandene auf den Technikwert setzen — nur auf
            /// ausdrückliche Handlung („Planwert übernehmen…").
            /// </summary>
            Abgleichen
        }

        /// <summary>
        /// Schreibt die Nebenkostenzeilen einer Komponente: je Posten mit Wert &gt; 0 eine
        /// eigene Zeile in derselben Gruppe. Rückgabe = Anzahl der berührten Zeilen.
        /// </summary>
        /// <remarks>
        /// Posten ohne Wert entstehen nicht (Nutzerentscheidung 2) — eine bereits
        /// vorhandene Zeile wird aber auch nie gelöscht. Löschen bleibt der ausdrücklichen
        /// Handlung des Anwenders vorbehalten; still verschwindende Positionen wären
        /// derselbe Vertrauensbruch wie stilles Überschreiben.
        /// </remarks>
        internal static int SchreibeNebenkosten(int projektID, int kategorieID, int komponentenID,
                                                List<TechnikPlanwertCtrl.Nebenposten> posten,
                                                string gruppe, Nebenmodus modus)
        {
            int n = 0;
            if (posten == null) return 0;

            foreach (TechnikPlanwertCtrl.Nebenposten p in posten)
            {
                if (p.Betrag <= 0) continue;
                int stamm = StammIdNeben(p.Bezeichnung);
                if (stamm <= 0) continue;

                int vorhanden = FindePosition(projektID, kategorieID, komponentenID, stamm);
                if (vorhanden > 0)
                {
                    if (modus == Nebenmodus.NurAnlegen) continue;
                    if (SetzeBetragNachId(vorhanden, p.Betrag)) n++;
                    continue;
                }

                if (SetzeBetrag(projektID, kategorieID, komponentenID, stamm, p.Betrag,
                                gruppe, true) > 0) n++;
            }
            return n;
        }

        // -------------------------------------------------------------- Abweichungen

        /// <summary>Abweichung einer Komponente zwischen erfasster Position und Technik.</summary>
        internal sealed class Abweichung
        {
            /// <summary>Die Komponente führt überhaupt Technikdaten.</summary>
            public bool TechnikVorhanden;

            /// <summary>Mindestens eine Anlage bietet zwei Kostenbasen an.</summary>
            public bool AuswahlOffen;

            /// <summary>Erfasster Betrag der Hauptposition.</summary>
            public double Erfasst;

            /// <summary>Technikwert, mit dem verglichen wurde (bei Mehrdeutigkeit der größte).</summary>
            public double Technik;

            /// <summary>Erfasster Wert passt zu keiner der angebotenen Kostenbasen.</summary>
            public bool Abweichend;

            /// <summary>Klartext für Kachel/Tooltip (leer, wenn nichts zu melden ist).</summary>
            public string Text = "";
        }

        /// <summary>
        /// Vergleicht die erfasste Hauptposition mit den Technik-Planwerten.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der Vergleich fragt „passt der erfasste Wert zu IRGENDEINER angebotenen
        /// Kostenbasis?"</b>, nicht „passt er zu der einen richtigen". Das ist der Grund,
        /// warum die getroffene Auswahl NICHT gespeichert werden muss: Hat der Anwender
        /// beim BHKW den Modulpreis gewählt, stimmt die Position mit genau dieser Summe
        /// überein und gilt als angeglichen; wählt er später den spezifischen Preis, gilt
        /// sie ebenso. Erst wenn die Technik sich ändert (Katalogpflege, Modulwechsel über
        /// <see cref="KomponentenUebernahmeCtrl"/>) oder der Anwender den Betrag von Hand
        /// gesetzt hat, passt keine Summe mehr — und genau dann soll die Abweichung
        /// erscheinen. Eine gespeicherte Auswahl hätte dafür eine neue Spalte und damit
        /// einen Migrationsschritt gebraucht.
        /// </para>
        /// <para>
        /// Der Fall „Position 15.000 €, Technikwert 0 € (Vitocrossal in Projekt 1018, im
        /// Katalog inzwischen 12.000 €)" fällt darunter: ohne gepflegten Projektwert gibt
        /// es keine Kostenbasis, der erfasste Wert bleibt stehen und wird als Abweichung
        /// gemeldet. Überschrieben wird nie.
        /// </para>
        /// </remarks>
        internal static Abweichung Pruefe(int projektID, string komponente, int kategorieID,
                                          int komponentenID)
        {
            var a = new Abweichung();
            if (kategorieID != Form_Kosten.KATEGORIE_INVESTITION) return a;
            if (!TechnikPlanwertCtrl.Bekannt(komponente)) return a;

            List<TechnikPlanwertCtrl.Anlage> anlagen =
                TechnikPlanwertCtrl.LiesAnlagen(projektID, komponente);

            a.Erfasst = LiesBetrag(FindeHauptposition(projektID, kategorieID, komponentenID, komponente));

            // Alle Summen, die der Anwender über den Übernahmedialog erzeugen könnte.
            List<double> moeglich = MoeglicheSummen(anlagen);
            a.TechnikVorhanden = moeglich.Count > 0 && Groesste(moeglich) > 0;
            a.AuswahlOffen = TechnikPlanwertCtrl.Mehrdeutig(anlagen);
            a.Technik = Groesste(moeglich);

            foreach (double m in moeglich)
                if (Math.Abs(m - a.Erfasst) <= EPS) return a;      // passt zu einer Basis

            if (!a.TechnikVorhanden && Math.Abs(a.Erfasst) <= EPS) return a;  // beides leer

            a.Abweichend = true;
            a.Text = a.AuswahlOffen
                ? MyResource.Resource.KOSTEN_ABWEICHUNG_AUSWAHL
                : string.Format(MyResource.Resource.KOSTEN_ABWEICHUNG,
                                a.Erfasst.ToString("N2", BerichtTexte.Kultur),
                                a.Technik.ToString("N2", BerichtTexte.Kultur));
            return a;
        }

        /// <summary>
        /// Die Hauptpositionssummen, die aus den Kostenbasen der Anlagen entstehen können.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bei mehreren mehrdeutigen Anlagen wird nicht kombinatorisch aufgezählt, sondern
        /// es werden die beiden Ränder gebildet: einmal überall der Modulpreis, einmal
        /// überall der spezifische Preis. Anlagen mit nur einer gepflegten Basis steuern in
        /// beiden Fällen denselben Wert bei. Mehr braucht die Frage „passt der erfasste Wert
        /// überhaupt zur Technik?" nicht.
        /// </para>
        /// <para>
        /// <b>Der Zustand „noch nichts gewählt" gehört ausdrücklich NICHT dazu.</b> Solange
        /// eine Anlage zwei Kostenbasen anbietet, trägt sie in der automatischen Vorbelegung
        /// 0 bei — würde diese Summe hier mitzählen, gälte die unausgefüllte Hauptposition
        /// als abgeglichen, und der Anwender bekäme nie den Hinweis, dass er noch entscheiden
        /// muss. Genau das ist der Fall des Beispiel-BHKW „2G 250kw.el Gas".
        /// </para>
        /// </remarks>
        private static List<double> MoeglicheSummen(List<TechnikPlanwertCtrl.Anlage> anlagen)
        {
            var summen = new List<double>();
            if (anlagen == null || anlagen.Count == 0) return summen;

            string[] varianten =
            {
                TechnikPlanwertCtrl.BASIS_MODULPREIS,
                TechnikPlanwertCtrl.BASIS_SPEZIFISCH
            };

            foreach (string v in varianten)
            {
                double s = 0;
                foreach (TechnikPlanwertCtrl.Anlage an in anlagen)
                {
                    TechnikPlanwertCtrl.Basiswert b = an.Basis(v);
                    s += (b != null) ? b.Betrag : an.EindeutigerWert;
                }
                summen.Add(s);
            }
            return summen;
        }

        private static double Groesste(List<double> werte)
        {
            double max = 0;
            foreach (double w in werte) if (w > max) max = w;
            return max;
        }
    }
}
