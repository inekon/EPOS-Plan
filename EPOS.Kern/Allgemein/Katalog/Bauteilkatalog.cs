using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BAUTEILKATALOG - Gliederung, Kennzeichen und Zusatzfelder (BK-2, Entscheid E132).
    //
    // GLIEDERUNG. Baustoffe und Aufbauten stehen zweistufig in Gruppe und Klasse (Codes aus BauteilkatalogSchema). Die
    // Vorgabe der Abbildung liegt hier:
    //   - Baustoff: die Stoffgruppe des Katalogs (Baustoffgruppen, Text) -> Gruppencode; der Stoffgruppencode einer
    //     Fremdquelle (Entwurf 2.3 Regel 5) -> Gruppencode; unbekannt oder "gemischt" -> SONSTIGE.
    //   - Aufbau: die Bauteilart (Entwurf 2.3 Regel 6 bildet die Quelle erst auf sie ab) -> Gruppe und Klasse; ein Dach
    //     nimmt seine Klasse aus dem Typaufbau (Flach- oder Steildach), sonst bleibt sie leer.
    //
    // KENNZEICHEN "fuer die Gebaeudesimulation meist nicht geeignet" (F3): eine Schicht ohne Luftschicht und ohne
    // lambda > 0, oder ein Aufbau ohne Schicht - ein Fenster, eine Vorhangfassade oder eine Tuer mit U der Quelle
    // ausgenommen. Baustoffe werden nicht nach lambda gekennzeichnet (F3).
    //
    // ZUSATZFELDER. Die Felder der Quelle (BauteilkatalogSchema.*_SPALTEN) reisen als Wertetabelle am Modell; die
    // Controller lesen und schreiben sie nur, wenn der Schritt steht (BauteilkatalogSchema.Lesbar). Der Rechenweg liest
    // keines davon.
    // ====================================================================================

    /// <summary>
    /// <b>Gliederung, Kennzeichen und Zusatzfelder des Baustoff- und Bauteilkatalogs</b> (BK-2). Ohne Oberfläche; die
    /// Datenbank nur über die Controller und den Schemaschritt.
    /// </summary>
    public static class Bauteilkatalog
    {
        /// <summary>Die Bauteilarten des Aufbaukatalogs (Wertliste von <c>Bauteilart</c>).</summary>
        public static readonly IReadOnlyList<string> BAUTEILARTEN = new[]
        {
            DbWerte.BAUTEILART_AUSSENWAND, DbWerte.BAUTEILART_DACH, DbWerte.BAUTEILART_BODENPLATTE, DbWerte.BAUTEILART_FENSTER,
            DbWerte.BAUTEILART_TUER, DbWerte.BAUTEILART_INNENWAND, DbWerte.BAUTEILART_DECKE, DbWerte.BAUTEILART_VORHANGFASSADE,
            DbWerte.BAUTEILART_SONSTIGES
        };

        /// <summary>Die Öffnungsarten, die ohne Schicht mit einem U der Quelle nicht gekennzeichnet werden.</summary>
        public static readonly IReadOnlyList<string> OEFFNUNGSARTEN = new[]
        {
            DbWerte.BAUTEILART_FENSTER, DbWerte.BAUTEILART_VORHANGFASSADE, DbWerte.BAUTEILART_TUER
        };

        /// <summary>Bauteilart → (Gruppe, Klasse) des Aufbaus; eine Klasse <c>null</c> ist unklar.</summary>
        private static readonly IReadOnlyDictionary<string, (string Gruppe, string Klasse)> AUS_BAUTEILART =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal)
            {
                [DbWerte.BAUTEILART_AUSSENWAND] = ("WAENDE", "AUSSENWAND"),
                [DbWerte.BAUTEILART_INNENWAND] = ("WAENDE", "INNENWAND"),
                [DbWerte.BAUTEILART_DECKE] = ("DECKEN", null),
                [DbWerte.BAUTEILART_BODENPLATTE] = ("DECKEN", "BODENPLATTE"),
                [DbWerte.BAUTEILART_DACH] = ("DAECHER", null),
                [DbWerte.BAUTEILART_FENSTER] = ("OEFFNUNGEN", "FENSTER"),
                [DbWerte.BAUTEILART_VORHANGFASSADE] = ("OEFFNUNGEN", "VORHANGFASSADE"),
                [DbWerte.BAUTEILART_TUER] = ("OEFFNUNGEN", "TUER"),
                [DbWerte.BAUTEILART_SONSTIGES] = ("SONSTIGE", "SONSTIGES")
            };

        /// <summary>(Bauteilart, Typaufbau) → Klasse, wo der Typaufbau die Klasse klärt.</summary>
        public static readonly IReadOnlyDictionary<(string Bauteilart, string Typaufbau), string> KLASSE_AUS_TYPAUFBAU =
            new Dictionary<(string, string), string>
            {
                [(DbWerte.BAUTEILART_DACH, "DA_STAHLBETON_GEDAEMMT")] = "FLACHDACH",
                [(DbWerte.BAUTEILART_DACH, "DA_SPARRENDACH")] = "STEILDACH"
            };

        /// <summary>
        /// Stoffgruppencode einer Fremdquelle (Entwurf 2.3 Regel 5; belegt sind 2, 4 und 5 aus der Projektdatei) →
        /// Gruppencode. Ein unbelegter Code fällt auf <c>SONSTIGE</c>; BK-3/BK-4 ergänzen die Tafel aus der Zählung der Quelle.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, string> GRUPPE_AUS_QUELLCODE = new Dictionary<int, string>
        {
            [2] = "BETON",
            [4] = "MAUERWERK",
            [5] = "DAEMMSTOFFE"
        };

        /// <summary>Der Gruppencode „Sonstige“ beider Kataloge.</summary>
        public const string GRUPPE_SONSTIGE = "SONSTIGE";

        /// <summary>Der Gruppencode einer Stoffgruppe des Katalogs (Text); <c>null</c>, wenn sie keine feste Gruppe ist.</summary>
        public static string GruppeAusStoffgruppe(string stoffgruppe)
        {
            Baustoffgruppe g = Baustoffgruppen.Finden(stoffgruppe);
            return g == null ? null : g.Ressource.Substring("BST_GRUPPE_".Length);
        }

        /// <summary>Der Stoffgruppentext (<see cref="Baustoffgruppen"/>) eines Gruppencodes; <c>null</c> ohne Treffer.</summary>
        public static string StoffgruppeAusGruppe(string code) =>
            code == null ? null : Baustoffgruppen.Alle.FirstOrDefault(g => g.Ressource == "BST_GRUPPE_" + code)?.Wert;

        /// <summary>Der Gruppencode eines Stoffgruppencodes einer Fremdquelle; leer oder unbekannt → <c>SONSTIGE</c>.</summary>
        public static string GruppeAusQuellcode(int? code) =>
            code.HasValue && GRUPPE_AUS_QUELLCODE.TryGetValue(code.Value, out string g) ? g : GRUPPE_SONSTIGE;

        /// <summary>(Gruppe, Klasse) eines Aufbaus aus Bauteilart und Typaufbau; beide <c>null</c> ohne Bauteilart.</summary>
        public static (string Gruppe, string Klasse) GliederungAusBauteilart(string bauteilart, string typaufbau)
        {
            if (bauteilart == null || !AUS_BAUTEILART.TryGetValue(bauteilart, out (string Gruppe, string Klasse) g)) return (null, null);
            if (typaufbau != null && KLASSE_AUS_TYPAUFBAU.TryGetValue((bauteilart, typaufbau), out string k)) return (g.Gruppe, k);
            return g;
        }

        /// <summary>Die Gruppe einer Baustoffklasse; <c>null</c> ohne Treffer.</summary>
        public static string GruppeDerBaustoffklasse(string klasse) =>
            BauteilkatalogSchema.BAUSTOFF_KLASSEN.FirstOrDefault(k => k.Klasse == klasse).Gruppe;

        /// <summary>Die Gruppe einer Aufbauklasse; <c>null</c> ohne Treffer.</summary>
        public static string GruppeDerAufbauklasse(string klasse) =>
            BauteilkatalogSchema.AUFBAU_KLASSEN.FirstOrDefault(k => k.Klasse == klasse).Gruppe;

        /// <summary>
        /// Prüft Gruppe und Klasse: beide aus der Liste, und die Klasse gehört zur Gruppe. <c>null</c> = in Ordnung, sonst
        /// der Grund (Kerntext für Protokoll und Import).
        /// </summary>
        public static string GliederungPruefen(bool baustoff, string gruppe, string klasse)
        {
            IReadOnlyList<string> gruppen = baustoff ? BauteilkatalogSchema.BAUSTOFF_GRUPPEN : BauteilkatalogSchema.AUFBAU_GRUPPEN;
            if (gruppe != null && !gruppen.Contains(gruppe)) return "Gruppe " + gruppe + " unbekannt";
            if (klasse == null) return null;
            string zu = baustoff ? GruppeDerBaustoffklasse(klasse) : GruppeDerAufbauklasse(klasse);
            if (zu == null) return "Klasse " + klasse + " unbekannt";
            if (gruppe != null && gruppe != zu) return "Klasse " + klasse + " gehoert zur Gruppe " + zu;
            return null;
        }

        /// <summary>
        /// <b>Das Kennzeichen „für die Gebäudesimulation meist nicht geeignet“</b>: eine Schicht ohne Luftschicht und ohne
        /// λ > 0, oder ein Aufbau ohne Schicht — ein Fenster, eine Vorhangfassade oder eine Tür mit U der Quelle
        /// ausgenommen. Der Schemaschritt rechnet dieselbe Regel in SQL (<see cref="BauteilkatalogSchema.KennzeichenSetzen"/>).
        /// </summary>
        public static bool MeistUngeeignet(BauteilaufbauModel m)
        {
            if (m == null) return true;
            List<BauteilschichtModel> schichten = (m.Schichten ?? new List<BauteilschichtModel>()).Where(s => s != null).ToList();
            if (schichten.Any(s => !s.IstLuftschicht && !(s.Lambda > 0))) return true;
            if (schichten.Count > 0) return false;
            return !(m.Bauteilart != null && OEFFNUNGSARTEN.Contains(m.Bauteilart) && Zahl(m.Zusatz, BauteilkatalogSchema.SPALTE_U_QUELLE).HasValue);
        }

        // ---- Zusatzfelder ----------------------------------------------------------------

        /// <summary>Die Zusatzfelder (Felder der Quelle) eines Kopfs oder einer Schicht — ohne Gliederung, Kennzeichen, Verweis.</summary>
        public static IReadOnlyList<BauteilkatalogSchema.Spalte> Zusatzspalten(string tabelle) =>
            BauteilkatalogSchema.SpaltenVon(tabelle)
                .Where(s => (s.Art != BauteilkatalogSchema.Spaltenart.Code || IstSchicht(tabelle)) &&
                            s.Art != BauteilkatalogSchema.Spaltenart.Kennzeichen && s.Art != BauteilkatalogSchema.Spaltenart.Verweis)
                .ToList();

        private static bool IstSchicht(string tabelle) =>
            tabelle == BauteilkatalogSchema.TAB_SCHICHT || tabelle == BauteilkatalogSchema.TAB_SCHICHT_STAMM;

        /// <summary>Die Zusatzfelder einer gelesenen Zeile (fehlt eine Spalte, fehlt der Eintrag; NULL bleibt <c>null</c>).</summary>
        public static Dictionary<string, object> ZusatzLesen(DataRow r, string tabelle)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            if (r == null) return d;
            foreach (BauteilkatalogSchema.Spalte s in Zusatzspalten(tabelle))
                if (r.Table.Columns.Contains(s.Name))
                    d[s.Name] = r[s.Name] == DBNull.Value ? null : r[s.Name];
            return d;
        }

        /// <summary>Eine Zahl der Zusatzfelder; <c>null</c>, wenn sie fehlt.</summary>
        public static double? Zahl(IReadOnlyDictionary<string, object> zusatz, string spalte) =>
            zusatz != null && zusatz.TryGetValue(spalte, out object w) && w != null && w != DBNull.Value
                ? Convert.ToDouble(w, CultureInfo.InvariantCulture) : (double?)null;

        /// <summary>
        /// Schreibt Gliederung, Kennzeichen (nur Aufbau) und Zusatzfelder einer Zeile im Vorgang des Aufrufers — nur, wenn
        /// <paramref name="lesbar"/> (der Schritt steht). Ein Feld, das das Modell nicht trägt, wird leer.
        /// </summary>
        internal static void Schreiben(DbVorgang v, string tabelle, long id, string gruppe, string klasse, bool? ungeeignet,
                                       IReadOnlyDictionary<string, object> zusatz, bool lesbar)
        {
            if (!lesbar) return;
            var sets = new List<string>();
            var p = new List<DbParam>();
            bool kopf = !IstSchicht(tabelle);
            if (kopf)
            {
                sets.Add("\"" + BauteilkatalogSchema.SPALTE_GRUPPE + "\" = ?");
                p.Add(Textwert("@g", gruppe));
                sets.Add("\"" + BauteilkatalogSchema.SPALTE_KLASSE + "\" = ?");
                p.Add(Textwert("@k", klasse));
            }
            if (ungeeignet.HasValue)
            {
                sets.Add("\"" + BauteilkatalogSchema.SPALTE_UNGEEIGNET + "\" = ?");
                p.Add(new DbParam("@u", ungeeignet.Value ? 1 : 0));
            }
            foreach (BauteilkatalogSchema.Spalte s in Zusatzspalten(tabelle))
            {
                sets.Add("\"" + s.Name + "\" = ?");
                object w = zusatz != null && zusatz.TryGetValue(s.Name, out object x) ? x : null;
                p.Add(new DbParam("@" + s.Name, w ?? DBNull.Value));
            }
            if (sets.Count == 0) return;
            p.Add(new DbParam("@id", id));
            v.Ausfuehren("UPDATE \"" + tabelle + "\" SET " + string.Join(", ", sets) + " WHERE \"ID\" = ?", p.ToArray());
        }

        /// <summary>
        /// Überträgt Gliederung, Kennzeichen und Zusatzfelder einer Katalogzeile auf ihre Projektkopie (im Vorgang des
        /// Aufrufers; nur, wenn der Schritt steht). Der Abschnittsverweis reist nicht mit (BK-5).
        /// </summary>
        internal static void Uebertragen(DbVorgang v, string quelle, long quelleId, string ziel, long zielId, bool lesbar)
        {
            if (!lesbar) return;
            List<string> spalten = BauteilkatalogSchema.SpaltenVon(ziel)
                .Where(s => s.Art != BauteilkatalogSchema.Spaltenart.Verweis).Select(s => "\"" + s.Name + "\"").ToList();
            if (spalten.Count == 0) return;
            v.Ausfuehren("UPDATE \"" + ziel + "\" SET (" + string.Join(", ", spalten) + ") = (SELECT " + string.Join(", ", spalten) +
                         " FROM \"" + quelle + "\" WHERE \"ID\" = ?) WHERE \"ID\" = ?",
                         new DbParam("@q", quelleId), new DbParam("@z", zielId));
        }

        private static DbParam Textwert(string name, string wert) =>
            new DbParam(name, DbParamTyp.VarWChar) { Wert = string.IsNullOrWhiteSpace(wert) ? DBNull.Value : (object)wert.Trim() };
    }
}
