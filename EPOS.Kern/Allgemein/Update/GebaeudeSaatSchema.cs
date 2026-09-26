using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DER KATALOGSAETZE M UND A - Schemaschritt zu Entscheid E51 (Konzept
    // Gebaeudesimulation N1.58, Konzept Baualtersklassen 4).
    //
    // WAS. Sechs Saetze in Tab_Gebaeude_STAMM (GebaeudeSaat.cs), je mit ReadOnly = 1, der
    // Baualtersklasse M bzw. A und bei "EFH-GEG-EH55" dem Energiestandard EH55. Reines DML, keine
    // Spalte, keine Tabelle, keine Sicht.
    //
    // SCHLUESSEL IST DER BEZEICHNER. Der eindeutige Index Tab_Gebaeude_STAMM_Gebaeudename haelt je
    // Name EINEN Satz; die Saat legt nur an, was unter seinem Namen fehlt, und ueberschreibt nie -
    // auch keinen gleichnamigen eigenen Satz des Anwenders (das Protokoll nennt ihn). Feste Ids gibt
    // es nicht: Die Ids des Katalogs vergibt AUTOINCREMENT.
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt einen der Saetze, kein Rechenweg liest Klasse oder
    // Vorgabe; der Referenzlauf bleibt byte-gleich (Nachtrag in Referenzlaeufe/LIESMICH.md).
    // ====================================================================================

    /// <summary>
    /// <b>Der Schemaschritt der Katalogsätze M und A</b> (Entscheid E51) — EINE Quelle für die Migration
    /// der Schale, <c>Werkzeuge/Testdatenbankschema</c>, die Testvorrichtung und den Nachweis.
    /// </summary>
    public static class GebaeudeSaatSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Sie folgt lückenlos
        /// auf die Baualtersklassen (<see cref="BaualtersklassenSchema.SCHRITT"/>); wird der Schritt beim
        /// Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = BaualtersklassenSchema.SCHRITT + 1;

        /// <summary>Die Tabelle des Katalogs.</summary>
        public const string TABELLE = GebaeudeSchema.TAB_GEBAEUDE_STAMM;

        /// <summary>Die sechs Sätze (<see cref="GebaeudeSaattabelle.Alle"/>).</summary>
        public static IReadOnlyList<GebaeudeSaat> Saat => GebaeudeSaattabelle.Alle;

        /// <summary>
        /// Schreibt EINEN Satz — alle Werte als <c>?</c>-Parameter in der Reihenfolge von
        /// <see cref="Parameter"/>; fest stehen nur der Ferienfahrplan „keine Ferien" (Wochenende 0,
        /// Ferien 0, Ferienbeginn_1 366, alle übrigen Grenzen 0) und <c>ReadOnly = 1</c>. Der Text steht
        /// am Aufruf, damit der <c>SqlDialektPruefer</c> ihn gegen die Testdatenbank hält.
        /// </summary>
        /// <returns>Zahl der geschriebenen Zeilen (1).</returns>
        public static int Einfuegen(GebaeudeSaat s)
            => DataRepository.ExecuteNonQuery(
            "INSERT INTO \"Tab_Gebaeude_STAMM\" (\"Bezeichner\", \"Typ\", \"Beschreibung\", \"Wohnflaeche_gesamt\", " +
            "\"Bewohner\", \"Flaeche_Nutzer\", \"Interne_Waermegewinne\", \"Bauweise\", \"Fensterflaeche_Sued\", " +
            "\"Fensterflaeche_Ost_West\", \"Fensterflaeche_Nord\", \"Fensterdurchlassgrad\", " +
            "\"Raumsolltemperatur_Nachtabsenkung\", \"Raumsolltemperatur_Tag\", \"Maximaleraumtemperatur\", " +
            "\"k_Wert_Außenwand\", \"k_Wert_Fenster\", \"k_Wert_Dachflaeche\", \"k_Wert_Grundflaeche\", \"k_Wert_Sonstiges\", " +
            "\"Flaeche_Außenwand\", \"gesamte_Fensterflaeche\", \"Dachflaeche\", \"Grundflaeche\", \"Sonstige_Flaechen\", " +
            "\"Nutzflaeche\", \"Raumhoehe\", \"WBVK_Anschluß_Fenster_Wand\", \"WBVK_Anschluß_Wand_Dach\", " +
            "\"WBVK_Anschluß_Außenwand_Kellerdecke\", \"Abmessung_Anschluß_Fenster_Wand\", \"Abmessung_Anschluß_Wand_Dach\", " +
            "\"Abmessung_Anschluß_Außenwand_Kellerdecke\", \"Luftwechselrate\", \"WW_Bedarf\", \"Baualtersklasse\", " +
            "\"Gebaeudeart\", \"Wohngebaeude_Nicht_Wohngebaeude\", \"Energiestandard\", " +
            "\"Wochenende\", \"Ferien\", \"Ferienbeginn_1\", \"Ferienende_1\", \"Ferienbeginn_2\", \"Ferienende_2\", " +
            "\"Ferienbeginn_3\", \"Ferienende_3\", \"Ferienbeginn_4\", \"Ferienende_4\", \"ReadOnly\") " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, ?, ?, 0, 0, 366, 0, 0, 0, 0, 0, 0, 0, 1)",
            Parameter(s));

        /// <summary>Die 39 Parameter eines Satzes in der Reihenfolge der Anweisung von <see cref="Einfuegen"/>.</summary>
        public static DbParam[] Parameter(GebaeudeSaat s)
        {
            object standard = s.Energiestandard == null ? DBNull.Value : (object)s.Energiestandard;
            return new[]
            {
                new DbParam("@bez", s.Bezeichner),
                new DbParam("@typ", GebaeudeSaattabelle.TYP_WOHNGEBAEUDE),
                new DbParam("@besch", s.Beschreibung),
                new DbParam("@wfl", s.Wohnflaeche),
                new DbParam("@bew", s.Bewohner),
                new DbParam("@fjn", GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE),
                new DbParam("@iwg", s.InnereGewinne),
                new DbParam("@bauw", s.Bauweise),
                new DbParam("@fs", s.FensterSued),
                new DbParam("@fow", s.FensterOstWest),
                new DbParam("@fn", s.FensterNord),
                new DbParam("@g", s.GWert),
                new DbParam("@tn", GebaeudeStammCtrl.SOLLTEMPERATUR_NACHT_VORGABE),
                new DbParam("@tt", GebaeudeStammCtrl.SOLLTEMPERATUR_TAG_VORGABE),
                new DbParam("@tmax", GebaeudeSaattabelle.RAUMTEMPERATUR_MAX),
                new DbParam("@uaw", s.UAussenwand),
                new DbParam("@ufe", s.UFenster),
                new DbParam("@uda", s.UDach),
                new DbParam("@ugr", s.UGrund),
                new DbParam("@uso", s.USonstige),
                new DbParam("@aaw", s.FlaecheAussenwand),
                new DbParam("@afe", s.FlaecheFenster),
                new DbParam("@ada", s.FlaecheDach),
                new DbParam("@agr", s.FlaecheGrund),
                new DbParam("@aso", s.FlaecheSonstige),
                new DbParam("@nfl", s.Wohnflaeche),
                new DbParam("@h", s.Raumhoehe),
                new DbParam("@pfw", s.PsiFensterWand),
                new DbParam("@pwd", s.PsiWandDach),
                new DbParam("@pak", s.PsiAussenwandKeller),
                new DbParam("@lfw", s.LaengeFensterWand),
                new DbParam("@lwd", s.LaengeWandDach),
                new DbParam("@lak", s.LaengeAussenwandKeller),
                new DbParam("@lw", s.Luftwechsel),
                new DbParam("@ww", GebaeudeSaattabelle.WW_BEDARF),
                new DbParam("@bak", s.Klasse),
                new DbParam("@art", s.Gebaeudeart),
                new DbParam("@wng", GebaeudeSaattabelle.WOHNGEBAEUDE),
                new DbParam("@es", standard),
            };
        }

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zahl der in diesem Lauf angelegten Sätze.</summary>
            public int Gesaet { get; internal set; }

            /// <summary>Namen, unter denen schon ein Satz stand — übergangen, nicht überschrieben.</summary>
            public List<string> Vorhanden { get; } = new List<string>();

            /// <summary>Namen, unter denen ein EIGENER Satz des Anwenders steht (<c>ReadOnly = 0</c>).</summary>
            public List<string> Eigene { get; } = new List<string>();

            /// <summary>Die Zeile für Protokoll und Werkzeug.</summary>
            public string Zeile()
                => Gesaet.ToString(CultureInfo.InvariantCulture) + " von " + Saat.Count.ToString(CultureInfo.InvariantCulture) +
                   " Katalogsatz/-saetze der Klassen M und A gesaet (ReadOnly = 1), " +
                   Vorhanden.Count.ToString(CultureInfo.InvariantCulture) + " stand(en) bereits";
        }

        /// <summary>Steht jeder Satz unter seinem Namen im Katalog?</summary>
        public static bool Vollstaendig()
        {
            if (!DataRepository.TabelleVorhanden(TABELLE)) return false;
            foreach (GebaeudeSaat s in Saat)
                if (Anzahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\" WHERE \"Bezeichner\" = ?", new DbParam("@b", s.Bezeichner)) == 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Schreibt die fehlenden Sätze. <b>Wiederholbar und nie überschreibend:</b> Ein Name, unter dem
        /// schon ein Satz steht, wird übergangen; ein eigener Satz des Anwenders unter demselben Namen
        /// kommt ins Protokoll (<paramref name="bericht"/>, darf <c>null</c> sein). Setzt die Spalte
        /// <c>Energiestandard</c> voraus (<see cref="BaualtersklassenSchema"/>). Fehler werfen — der
        /// Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            var b = new Bericht();
            foreach (GebaeudeSaat s in Saat)
            {
                if (Anzahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\" WHERE \"Bezeichner\" = ?", new DbParam("@b", s.Bezeichner)) > 0)
                {
                    b.Vorhanden.Add(s.Bezeichner);
                    if (Anzahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\" WHERE \"Bezeichner\" = ? AND \"ReadOnly\" = 0",
                               new DbParam("@b", s.Bezeichner)) > 0)
                    {
                        b.Eigene.Add(s.Bezeichner);
                        bericht?.Add("Katalogsatz \"" + s.Bezeichner + "\" nicht gesaet: ein eigener Satz traegt den Namen");
                    }
                    continue;
                }
                if (Einfuegen(s) == 1) b.Gesaet++;
            }
            bericht?.Add(b.Zeile());
            return b;
        }

        private static long Anzahl(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
