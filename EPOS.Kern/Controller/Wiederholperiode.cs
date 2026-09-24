using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>ETAPPE E16 — die Wiederholperiode je Kostenposition</b> (Konzept Wirtschaftlichkeit
    /// § 2.11.2 V‑G3, DIN EN 17463 6.3.1 „alle n Jahre"; Schemaschritt
    /// <see cref="WiederholperiodeSchema.SCHRITT"/>).
    ///
    /// <para><b>Was die Periode sagt.</b> Eine Betriebsposition (<c>Tab_ProjektWerte</c>,
    /// Kategorie 2) und eine Vorlagenposition (<c>Tab_KostenVorlagePosition</c>) tragen
    /// <c>Wiederholperiode_a</c>: <b>leer, 0 oder 1 = jährlich</b> (der Weg vor dem Schritt),
    /// <b>n ≥ 2</b> = die Position zahlt ab ihrem Startjahr s nur in den Jahren s, s + n,
    /// s + 2n … ≤ T (E16‑Q1 a, <see cref="KapitalwertRechner.ZahltImJahr"/>). Beispiel: eine
    /// Dichtheitsprüfung alle 2 Jahre.</para>
    ///
    /// <para><b>Nur Betriebspositionen</b> (E16‑Q2 a). Eine Investition „alle n Jahre" ist
    /// die Ersatzkette über die Nutzungsdauer (<see cref="KapitalwertRechner.Ersatz"/>) und
    /// bekommt keine zweite Regel; Dialog und Leseschleife fragen die Periode deshalb nur
    /// auf der Betriebsseite.</para>
    ///
    /// <para><b>Dieser Controller ist der eine Lese- und Schreibweg</b> der Periode für
    /// Dialog, Vorlagenübernahme und Kostenwelt; gerechnet wird mit ihr allein im
    /// <see cref="KapitalwertRechner"/>. <b>Tolerant gegen eine nie migrierte Datenbank:</b>
    /// Jeder Leser fragt <see cref="WiederholperiodeSchema.SpalteVorhanden"/>; fehlt die
    /// Spalte, liest er nichts, und die Position zahlt jährlich — Zeichen für Zeichen der Weg
    /// vor dem Schritt.</para>
    /// </summary>
    internal static class Wiederholperiode
    {
        /// <summary>Die kleinste Periode, die NICHT jährlich ist.</summary>
        internal const int MIN_WIEDERHOLT = 2;

        /// <summary>Obergrenze der Eingabe [a] — länger als jeder Betrachtungszeitraum; eine
        /// Position, deren zweite Zahlung jenseits von T läge, zahlt einmal im Startjahr.</summary>
        internal const int MAX = 99;

        /// <summary>
        /// Die wirksame Periode einer Angabe: <c>null</c> (jährlich) für leer, 0, 1 oder eine
        /// negative Zahl; sonst die Zahl, auf <see cref="MAX"/> geklemmt.
        /// </summary>
        internal static int? Normiert(int? wert)
        {
            if (!wert.HasValue || wert.Value < MIN_WIEDERHOLT) return null;
            return Math.Min(wert.Value, MAX);
        }

        /// <summary>
        /// Die Periode einer gelesenen Zeile: ≥ 2 = alle n Jahre, sonst 1 (jährlich) — auch
        /// wenn die Spalte fehlt, NULL trägt oder keine Zahl ist.
        /// </summary>
        internal static int DerZeile(DataRow r)
        {
            try
            {
                if (r == null || !r.Table.Columns.Contains(WiederholperiodeSchema.SPALTE)) return 1;
                object o = r[WiederholperiodeSchema.SPALTE];
                if (o == null || o == DBNull.Value) return 1;
                int? n = Normiert(Convert.ToInt32(o, CultureInfo.InvariantCulture));
                return n ?? 1;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException ||
                                       ex is OverflowException || ex is ArgumentException)
            {
                // Benannt: keine Zahl heißt „jährlich", wie eine leere Zelle.
                return 1;
            }
        }

        /// <summary>Die Perioden aller Zeilen EINES Projekts, geschlüsselt nach
        /// <c>Tab_ProjektWerte.ID</c>; nur Zeilen mit n ≥ 2. Leer ohne Spalte.</summary>
        internal static Dictionary<int, int> LiesProjekt(int projektId)
        {
            return LiesKarte(SchemaKatalog.TAB_PROJEKTWERTE, "ProjektID", projektId);
        }

        /// <summary>Die Perioden aller Positionen EINER Vorlage, geschlüsselt nach
        /// <c>Tab_KostenVorlagePosition.ID</c>; nur Positionen mit n ≥ 2.</summary>
        internal static Dictionary<int, int> LiesVorlage(int vorlageId)
        {
            return LiesKarte(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION,
                             SchemaKatalog.SPALTE_KVP_VORLAGEID, vorlageId);
        }

        private static Dictionary<int, int> LiesKarte(string tabelle, string schluesselSpalte, int schluessel)
        {
            var karte = new Dictionary<int, int>();
            if (schluessel <= 0 || !WiederholperiodeSchema.SpalteVorhanden(tabelle)) return karte;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [ID], [" + WiederholperiodeSchema.SPALTE + "] FROM [" + tabelle +
                    "] WHERE [" + schluesselSpalte + "] = ? AND [" + WiederholperiodeSchema.SPALTE +
                    "] >= ?",
                    new DbParam("@k", schluessel),
                    new DbParam("@n", MIN_WIEDERHOLT));
                if (dt == null) return karte;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["ID"] == DBNull.Value) continue;
                    int n = DerZeile(r);
                    if (n >= MIN_WIEDERHOLT) karte[Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture)] = n;
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException ||
                                       ex is OverflowException || ex is ArgumentException)
            {
                // Benannt: eine unlesbare Zeile zählt als jährlich; die Karte bleibt, soweit gelesen.
            }
            return karte;
        }

        /// <summary>
        /// Schreibt die Periode EINER Zeile — <c>null</c>, 0 und 1 schreiben NULL
        /// („jährlich"). Rückgabe <c>false</c>, wenn die Spalte fehlt oder keine Zeile
        /// getroffen wurde; eine nie migrierte Datenbank bleibt so still beim jährlichen Weg.
        /// </summary>
        /// <param name="tabelle"><see cref="SchemaKatalog.TAB_PROJEKTWERTE"/> oder
        /// <see cref="SchemaKatalog.TAB_KOSTENVORLAGEPOSITION"/>.</param>
        internal static bool Schreibe(string tabelle, int id, int? periode)
        {
            if (id <= 0) return false;
            if (!string.Equals(tabelle, SchemaKatalog.TAB_PROJEKTWERTE, StringComparison.Ordinal) &&
                !string.Equals(tabelle, SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, StringComparison.Ordinal))
                return false;
            if (!WiederholperiodeSchema.SpalteVorhanden(tabelle)) return false;

            int? n = Normiert(periode);
            var p = new DbParam("@n", DbParamTyp.Integer);
            p.Wert = n.HasValue ? (object)n.Value : DBNull.Value;
            int treffer = DataRepository.ExecuteNonQuery(
                "UPDATE [" + tabelle + "] SET [" + WiederholperiodeSchema.SPALTE + "] = ? WHERE [ID] = ?",
                p, new DbParam("@id", id));
            return treffer == 1;
        }

        // =====================================================================
        //  Die Herleitung
        // =====================================================================

        /// <summary>
        /// „alle n Jahre ab Jahr X" — leer, wenn die Position jährlich zahlt. X ist das
        /// Startjahr, ohne Startjahr (≤ 1) das Jahr 1. Dieselbe Zeile stehen Kostendialog,
        /// Wort- und Tabellenbericht und die Herleitungsspalte der Formelmappe.
        /// </summary>
        internal static string Herleitung(int? periode, int? startJahr, CultureInfo kultur)
        {
            int? n = Normiert(periode);
            if (!n.HasValue) return "";
            int s = startJahr.HasValue && startJahr.Value > 1 ? startJahr.Value : 1;
            return string.Format(kultur ?? CultureInfo.CurrentCulture,
                                 MyResource.Resource.WIRT_BK_ALLE_N_JAHRE, n.Value, s);
        }

        /// <summary>
        /// Die Zahlungsjahre einer Position im Zeitraum 1…T — für Herleitung, Tests und die
        /// Gegenprobe; dieselbe Regel wie der Rechenkern (<see cref="KapitalwertRechner.ZahltImJahr"/>).
        /// </summary>
        internal static List<int> Zahlungsjahre(int? periode, int? startJahr, int jahre)
        {
            var liste = new List<int>();
            int n = Normiert(periode) ?? 1;
            int s = startJahr.HasValue && startJahr.Value > 1 ? startJahr.Value : 1;
            for (int t = 1; t <= jahre; t++)
                if (KapitalwertRechner.ZahltImJahr(s, n, t)) liste.Add(t);
            return liste;
        }
    }
}
