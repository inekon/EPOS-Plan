using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>ETAPPE E7c — Schritt E: Ersatz und Restwert je Position entkoppelt</b>
    /// (Entscheid A6 vom 20.09.2026, Mockup U39, Konzept Wirtschaftlichkeit § 2.13 (3),
    /// Schemaschritt 111).
    ///
    /// <para><b>Was die zwei Kennzeichen sagen.</b> Jede Investitionsposition
    /// (<c>Tab_ProjektWerte</c>) und jede Vorlagenposition
    /// (<c>Tab_KostenVorlagePosition</c>) trägt <c>ErsatzFuehren</c> und
    /// <c>RestwertAnsetzen</c> — dreiwertig: <b>leer (NULL) = wie bisher</b> (ersetzt
    /// wird, sobald die Nutzungsdauer vor dem Ende des Betrachtungszeitraums abläuft;
    /// der Restwert steht linear am Ende), <b>ja</b> (dasselbe, ausdrücklich gewählt),
    /// <b>nein</b> (abgeschaltet). Oft ist das eine ohne das andere gewollt: Eine
    /// Planungsleistung wird nicht ersetzt, trägt aber auch keinen Restwert; ein
    /// Gebäudeanteil wird nicht ersetzt und behält seinen Restwert.</para>
    ///
    /// <para><b>Dieser Controller ist der eine Lese- und Schreibweg</b> der Kennzeichen
    /// für Dialog, Vorlagenübernahme und Kostenwelt. Gerechnet wird mit ihnen allein
    /// in <see cref="KapitalwertRechner.Ersatz"/>; hier entstehen keine Zahlen.</para>
    ///
    /// <para><b>Tolerant gegen eine nie migrierte Datenbank.</b> Jeder Leser fragt
    /// <see cref="SpaltenVorhanden"/>; fehlen die Spalten, liest er nichts und rechnet
    /// den Weg vor Schritt 111 — Zeichen für Zeichen. Der gemerkte Spaltenstand gilt je
    /// Datenbankpfad; die Migration vergisst ihn nach dem Anlegen
    /// (<see cref="SpaltenStandVergessen"/>).</para>
    /// </summary>
    internal static class ErsatzRestwertKennzeichen
    {
        /// <summary>Eintrag „ja" der Dreiwerte-Auswahl. Der dritte Wert (leer) ist
        /// der Platzhalter der Klappliste, kein Eintrag.</summary>
        internal const int JA = 1;

        /// <summary>Eintrag „nein" der Dreiwerte-Auswahl.</summary>
        internal const int NEIN = 0;

        /// <summary>Die zwei Kennzeichen einer Zeile; beide <c>null</c> = wie bisher.</summary>
        internal struct Paar
        {
            public bool? ErsatzFuehren;
            public bool? RestwertAnsetzen;

            /// <summary>true, wenn wenigstens eines der zwei Kennzeichen gesetzt ist.</summary>
            public bool Gesetzt => ErsatzFuehren.HasValue || RestwertAnsetzen.HasValue;
        }

        // =====================================================================
        //  Spaltenstand
        // =====================================================================

        private static readonly object _sperre = new object();
        private static string _pfad;
        private static readonly Dictionary<string, bool> _da =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Trägt <paramref name="tabelle"/> beide Kennzeichenspalten? Gemerkt je
        /// Datenbankpfad — eine Testkopie, ein Referenzlauf und die Datenbank des
        /// Anwenders bekommen je ihre eigene Antwort.
        /// </summary>
        internal static bool SpaltenVorhanden(string tabelle)
        {
            if (string.IsNullOrEmpty(tabelle)) return false;
            string pfad = Pfad();
            lock (_sperre)
            {
                if (!string.Equals(pfad, _pfad, StringComparison.OrdinalIgnoreCase))
                {
                    _da.Clear();
                    _pfad = pfad;
                }
                bool da;
                if (_da.TryGetValue(tabelle, out da)) return da;
                da = false;
                try
                {
                    da = DataRepository.SpalteVorhanden(tabelle, SchemaKatalog.SPALTE_PW_ERSATZ_FUEHREN) &&
                         DataRepository.SpalteVorhanden(tabelle, SchemaKatalog.SPALTE_PW_RESTWERT_ANSETZEN);
                }
                catch { da = false; }
                _da[tabelle] = da;
                return da;
            }
        }

        /// <summary>Vergisst den gemerkten Spaltenstand — gerufen von Schritt 111
        /// der Migration, nachdem er die Spalten angelegt hat.</summary>
        internal static void SpaltenStandVergessen()
        {
            lock (_sperre)
            {
                _da.Clear();
                _pfad = null;
            }
        }

        private static string Pfad()
        {
            try { return DataRepository.GetDBPath() ?? ""; }
            catch { return ""; }
        }

        // =====================================================================
        //  Lesen und Schreiben
        // =====================================================================

        /// <summary>
        /// Ein Kennzeichen aus einer gelesenen Zeile: <c>null</c>, wenn die Spalte
        /// fehlt oder NULL trägt; sonst 0 = nein, alles andere = ja.
        /// </summary>
        internal static bool? Wert(DataRow r, string spalte)
        {
            try
            {
                if (r == null || !r.Table.Columns.Contains(spalte)) return null;
                object o = r[spalte];
                if (o == null || o == DBNull.Value) return null;
                return Convert.ToInt64(o, CultureInfo.InvariantCulture) != 0;
            }
            catch { return null; }
        }

        /// <summary>Die Kennzeichen aller Zeilen EINES Projekts, geschlüsselt nach
        /// <c>Tab_ProjektWerte.ID</c>. Leer ohne Spalten; Zeilen mit zwei leeren
        /// Kennzeichen fehlen in der Karte.</summary>
        internal static Dictionary<int, Paar> LiesProjekt(int projektId)
        {
            return LiesKarte(SchemaKatalog.TAB_PROJEKTWERTE, "ProjektID", projektId);
        }

        /// <summary>Die Kennzeichen aller Positionen EINER Vorlage, geschlüsselt nach
        /// <c>Tab_KostenVorlagePosition.ID</c>.</summary>
        internal static Dictionary<int, Paar> LiesVorlage(int vorlageId)
        {
            return LiesKarte(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION,
                             SchemaKatalog.SPALTE_KVP_VORLAGEID, vorlageId);
        }

        private static Dictionary<int, Paar> LiesKarte(string tabelle, string schluesselSpalte, int schluessel)
        {
            var karte = new Dictionary<int, Paar>();
            if (schluessel <= 0 || !SpaltenVorhanden(tabelle)) return karte;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [ID], [" + SchemaKatalog.SPALTE_PW_ERSATZ_FUEHREN + "], [" +
                    SchemaKatalog.SPALTE_PW_RESTWERT_ANSETZEN + "] FROM [" + tabelle +
                    "] WHERE [" + schluesselSpalte + "] = ?",
                    new DbParam("@k", schluessel));
                if (dt == null) return karte;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["ID"] == DBNull.Value) continue;
                    var p = new Paar
                    {
                        ErsatzFuehren = Wert(r, SchemaKatalog.SPALTE_PW_ERSATZ_FUEHREN),
                        RestwertAnsetzen = Wert(r, SchemaKatalog.SPALTE_PW_RESTWERT_ANSETZEN)
                    };
                    if (p.Gesetzt) karte[Convert.ToInt32(r["ID"])] = p;
                }
            }
            catch { }
            return karte;
        }

        /// <summary>
        /// Schreibt die zwei Kennzeichen EINER Zeile — <c>null</c> schreibt NULL
        /// („wie bisher"). Rückgabe <c>false</c>, wenn die Spalten fehlen oder keine
        /// Zeile getroffen wurde; eine nie migrierte Datenbank bleibt so still auf dem
        /// Weg vor Schritt 111.
        /// </summary>
        /// <param name="tabelle"><see cref="SchemaKatalog.TAB_PROJEKTWERTE"/> oder
        /// <see cref="SchemaKatalog.TAB_KOSTENVORLAGEPOSITION"/>.</param>
        internal static bool Schreibe(string tabelle, int id, bool? ersatzFuehren, bool? restwertAnsetzen)
        {
            if (id <= 0) return false;
            if (!string.Equals(tabelle, SchemaKatalog.TAB_PROJEKTWERTE, StringComparison.Ordinal) &&
                !string.Equals(tabelle, SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, StringComparison.Ordinal))
                return false;
            if (!SpaltenVorhanden(tabelle)) return false;

            int n = DataRepository.ExecuteNonQuery(
                "UPDATE [" + tabelle + "] SET [" + SchemaKatalog.SPALTE_PW_ERSATZ_FUEHREN + "] = ?, [" +
                SchemaKatalog.SPALTE_PW_RESTWERT_ANSETZEN + "] = ? WHERE [ID] = ?",
                Parameter("@e", ersatzFuehren),
                Parameter("@r", restwertAnsetzen),
                new DbParam("@id", id));
            return n == 1;
        }

        private static DbParam Parameter(string name, bool? wert)
        {
            var p = new DbParam(name, DbParamTyp.Integer);
            p.Wert = wert.HasValue ? (object)(wert.Value ? 1 : 0) : DBNull.Value;
            return p;
        }

        // =====================================================================
        //  Die Dreiwerte-Auswahl des Dialogs
        // =====================================================================

        /// <summary>Die Einträge der Klappliste — „ja" und „nein"; leer ist der
        /// Platzhalter (<see cref="LeerText"/>).</summary>
        internal static IReadOnlyList<(int Id, string Text)> Eintraege()
        {
            return new List<(int, string)>
            {
                (JA, MyResource.Resource.ERK_JA),
                (NEIN, MyResource.Resource.ERK_NEIN)
            };
        }

        /// <summary>Der Platzhalter der Klappliste: leer = wie bisher.</summary>
        internal static string LeerText => MyResource.Resource.ERK_LEER;

        /// <summary>Kennzeichen → Auswahl der Klappliste (<c>null</c> = Platzhalter).</summary>
        internal static int? AlsAuswahl(bool? wert)
        {
            return wert.HasValue ? (wert.Value ? JA : NEIN) : (int?)null;
        }

        /// <summary>Auswahl der Klappliste → Kennzeichen (<c>null</c> = leer).</summary>
        internal static bool? AusAuswahl(int? auswahl)
        {
            return auswahl.HasValue ? auswahl.Value != NEIN : (bool?)null;
        }

        // =====================================================================
        //  Die Herleitungszeile je Position
        // =====================================================================

        /// <summary>
        /// Die Herleitungszeile einer Position: „Kennzeichen der Position —
        /// Ersatzbeschaffung: nein · Restwert: wie bisher." Leer, wenn beide Kennzeichen
        /// leer sind — dann rechnet die Position wie bisher, und es gibt nichts zu
        /// erklären.
        /// </summary>
        internal static string Herleitung(bool? ersatzFuehren, bool? restwertAnsetzen, CultureInfo kultur)
        {
            if (!ersatzFuehren.HasValue && !restwertAnsetzen.HasValue) return "";
            return string.Format(kultur ?? CultureInfo.CurrentCulture, MyResource.Resource.ERK_HERLEITUNG,
                                 Klartext(ersatzFuehren), Klartext(restwertAnsetzen));
        }

        private static string Klartext(bool? wert)
        {
            return !wert.HasValue ? MyResource.Resource.ERK_WIE_BISHER
                 : wert.Value ? MyResource.Resource.ERK_JA
                 : MyResource.Resource.ERK_NEIN;
        }
    }
}
