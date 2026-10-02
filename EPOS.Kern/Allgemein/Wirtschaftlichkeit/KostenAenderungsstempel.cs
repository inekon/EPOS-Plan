using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Warum ein gespeichertes Ergebnis der Wirtschaftlichkeit nicht mehr aktuell ist</b> — die
    /// Antwort von <see cref="WirtschaftlichkeitCtrl.Veraltung"/>, hinter der
    /// <see cref="WirtschaftlichkeitCtrl.ErgebnisAktuell"/> steht.
    ///
    /// <para><b>Die Werte sind nach Vorrang geordnet</b> (<see cref="KostenAenderungsstempel.Vorrang"/>):
    /// Ein jüngerer Simulationslauf schlägt eine Kostenänderung, die Kostenänderung der Gruppe
    /// schlägt den Katalog. Das Band nennt immer den gewichtigsten Grund.</para>
    /// </summary>
    public enum Ergebnisveraltung
    {
        /// <summary>Das Ergebnis ist aktuell — oder es gibt keines (dann fehlt es, veraltet ist nichts).</summary>
        Keine = 0,

        /// <summary>Der Kostenkatalog (Energieträger, Emissionen, Gesetzesparameter, Nutzungsdauern …)
        /// wurde nach der Rechnung geändert.</summary>
        Katalog = 1,

        /// <summary>Kosten, Preise oder Wirtschaftlichkeitsparameter eines Projekts der
        /// Vergleichsgruppe wurden nach der Rechnung geändert.</summary>
        Kosten = 2,

        /// <summary>Das Ergebnis passt nicht zum jüngsten Simulationslauf seines Projekts.</summary>
        Simulation = 3,
    }

    /// <summary>
    /// <b>Der Vergleich der Änderungsstempel</b> mit dem Zeitstempel eines gespeicherten Ergebnisses.
    /// Die Stempel setzt die Datenbank selbst (Trigger aus <see cref="KostenStempelSchema"/>); diese
    /// Klasse liest sie und urteilt.
    ///
    /// <para><b>Verglichen wird die Vergleichsgruppe:</b> Stamm und Varianten
    /// (<c>Tab_Variante.ID_ProjektRef</c>) rechnen zusammen — Differenzkennzahlen, Referenz,
    /// Tarif und Parameter des Stamms gelten für alle. Der höchste Projektstempel der Gruppe und
    /// der Katalogstempel werden gegen den Zeitstempel des Ergebnisses gehalten; ein Stempel
    /// <b>strikt jünger</b> als das Ergebnis macht es veraltet.</para>
    ///
    /// <para><b>Dieselbe Sekunde gilt als davor.</b> Stempel und Ergebnis-Zeitstempel sind
    /// sekundengenaue Ortszeit („yyyy-MM-dd HH:mm:ss"). Eine Änderung in derselben Sekunde, in der
    /// das Ergebnis gespeichert wurde, ist von einer Änderung kurz davor nicht zu unterscheiden; sie
    /// zählt als davor — hinnehmbar, und nur so stempelt sich eine Rechnung nicht selbst, deren
    /// letzter Schreibvorgang in derselben Sekunde liegt wie ihr Ergebnis
    /// (<c>WirtschaftlichkeitCtrl.Persistiere</c> setzt den Zeitstempel erst am Ende). In der
    /// Stunde der Zeitumstellung im Herbst ist die Ortszeit doppeldeutig; dort kann eine Änderung
    /// übersehen werden.</para>
    ///
    /// <para><b>Ohne Ergebnis kein Urteil:</b> Fehlt der Zeitstempel, ist nichts veraltet — das
    /// Ergebnis fehlt, und das sagen die Seiten selbst.</para>
    /// </summary>
    public static class KostenAenderungsstempel
    {
        /// <summary>
        /// Der Gruppenstempel und der Katalogstempel in EINER Abfrage. Die Gruppe ist der Stamm
        /// des Projekts (bei einer Variante <c>ID_ProjektRef</c>, sonst das Projekt selbst) mit
        /// allen seinen Varianten. Parameter: zweimal die Projekt-Id.
        /// </summary>
        internal const string SQL_STEMPEL =
            "SELECT " +
            "(SELECT MAX(p.\"" + KostenStempelSchema.SPALTE_PROJEKT + "\") FROM \"" + KostenStempelSchema.TAB_PROJEKT + "\" AS p " +
            "WHERE p.\"ID\" = s.Stamm OR p.\"ID\" IN " +
            "(SELECT v.\"ID_Projekt\" FROM \"" + KostenStempelSchema.TAB_VARIANTE + "\" AS v WHERE v.\"ID_ProjektRef\" = s.Stamm)) AS Gruppe, " +
            "(SELECT MAX(a.\"" + KostenStempelSchema.SPALTE_KATALOG + "\") FROM \"" + KostenStempelSchema.TAB_APPLIKATION + "\" AS a) AS Katalog " +
            "FROM (SELECT COALESCE((SELECT v.\"ID_ProjektRef\" FROM \"" + KostenStempelSchema.TAB_VARIANTE + "\" AS v " +
            "WHERE v.\"ID_Projekt\" = ? LIMIT 1), ?) AS Stamm) AS s";

        /// <summary>Die zwei Stempel, die für ein Projekt gelten; <c>null</c> = keiner gesetzt.</summary>
        public sealed class Stempel
        {
            /// <summary>Der jüngste Projektstempel der Vergleichsgruppe.</summary>
            public DateTime? Gruppe { get; internal set; }

            /// <summary>Der Stempel des Kostenkatalogs.</summary>
            public DateTime? Katalog { get; internal set; }
        }

        /// <summary>
        /// Das Urteil für ein gespeichertes Ergebnis des Projekts mit diesem Zeitstempel:
        /// <see cref="Ergebnisveraltung.Kosten"/>, <see cref="Ergebnisveraltung.Katalog"/> oder
        /// <see cref="Ergebnisveraltung.Keine"/> (auch ohne Zeitstempel). Den Simulationslauf
        /// prüft <see cref="WirtschaftlichkeitCtrl.Veraltung"/> davor.
        /// </summary>
        public static Ergebnisveraltung Pruefe(int idProjekt, DateTime? ergebnisZeit)
        {
            if (idProjekt <= 0 || !ergebnisZeit.HasValue) return Ergebnisveraltung.Keine;
            return Urteil(Lies(idProjekt), ergebnisZeit.Value);
        }

        /// <summary>
        /// Das Urteil aus gelesenen Stempeln — ohne Datenbank, für Aufrufer, die mehrere Ergebnisse
        /// derselben Gruppe prüfen. Strikt jünger auf die Sekunde; der Gruppenstempel geht vor.
        /// </summary>
        public static Ergebnisveraltung Urteil(Stempel stempel, DateTime ergebnisZeit)
        {
            if (stempel == null) return Ergebnisveraltung.Keine;
            DateTime ergebnis = Sekunde(ergebnisZeit);
            if (stempel.Gruppe.HasValue && stempel.Gruppe.Value > ergebnis) return Ergebnisveraltung.Kosten;
            if (stempel.Katalog.HasValue && stempel.Katalog.Value > ergebnis) return Ergebnisveraltung.Katalog;
            return Ergebnisveraltung.Keine;
        }

        /// <summary>
        /// Liest Gruppen- und Katalogstempel des Projekts — still und ohne Dialog. Eine Datenbank
        /// vor dem Schemaschritt <see cref="KostenStempelSchema.SCHRITT"/> kennt die Spalten nicht;
        /// dann gilt „kein Stempel" (das Urteil bleibt beim Simulationslauf), der Grund steht in
        /// <see cref="Lesefehler"/>.
        /// </summary>
        public static Stempel Lies(int idProjekt)
        {
            var s = new Stempel();
            Lesefehler = null;
            if (idProjekt <= 0) return s;
            try
            {
                DataTable dt = StilleDb.TabelleStreng(SQL_STEMPEL,
                    new DbParam("@p", idProjekt), new DbParam("@p2", idProjekt));
                if (dt != null && dt.Rows.Count > 0)
                {
                    s.Gruppe = Zeitpunkt(dt.Rows[0]["Gruppe"]);
                    s.Katalog = Zeitpunkt(dt.Rows[0]["Katalog"]);
                }
            }
            catch (Exception ex)
            {
                // Benannt statt verschluckt: Der Grund ist abrufbar; ein unlesbarer Stempel macht
                // kein Ergebnis veraltet - die Frage nach dem Simulationslauf bleibt davon unberührt.
                Lesefehler = Fehlergrund.Text(ex);
            }
            return s;
        }

        /// <summary>Der Grund, aus dem das letzte <see cref="Lies"/> keine Stempel lesen konnte; <c>null</c> = keiner.</summary>
        public static string Lesefehler { get; private set; }

        /// <summary>
        /// Der gewichtigere zweier Gründe (Simulation vor Kosten vor Katalog) — für Seiten, die
        /// mehrere Ergebnisse zu EINEM Band zusammenfassen.
        /// </summary>
        public static Ergebnisveraltung Vorrang(Ergebnisveraltung a, Ergebnisveraltung b)
        {
            return (int)a >= (int)b ? a : b;
        }

        /// <summary>Ein Zeitpunkt auf die volle Sekunde gekürzt — die Auflösung der Stempel.</summary>
        public static DateTime Sekunde(DateTime zeit)
        {
            return new DateTime(zeit.Ticks - zeit.Ticks % TimeSpan.TicksPerSecond, zeit.Kind);
        }

        /// <summary>Ein Stempeltext im Format <see cref="KostenStempelSchema.FORMAT"/>; sonst <c>null</c>.</summary>
        internal static DateTime? Zeitpunkt(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            if (wert is DateTime d) return Sekunde(d);
            string text = Convert.ToString(wert, CultureInfo.InvariantCulture);
            if (DateTime.TryParseExact(text, KostenStempelSchema.FORMAT, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out DateTime t))
                return t;
            return null;
        }
    }
}
