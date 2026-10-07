using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DER BDEW-STANDARDLASTPROFILE STROM 2025 - Schemaschritt der Welle SLP25
    // (Befund SLP25-A, Entscheide E-SLP1 bis E-SLP8 nach Empfehlung).
    //
    // WAS. Drei Katalogsaetze der "Datenbank Strombedarf" (StandardlastprofilSaat.cs): je ein Kopf in
    // Tab_Stromverbraucher_STAMM (zwoelf Monatswerte in MWh, Summe 1.000 MWh) und ein Typprofil in
    // Tab_Stromverbrauchertyp_STAMM (168 Wochenstunden), verknuepft ueber den Namen (Typ = Typname),
    // beide mit ReadOnly = 1. Reines DML, keine Spalte, keine Tabelle, keine Sicht.
    //
    // SCHLUESSEL IST DER NAME. Die eindeutigen Indizes Tab_Stromverbraucher_STAMM_Bezeichner und
    // Tab_Stromverbrauchertyp_STAMM_Typname halten je Name EINEN Satz; der Schritt legt nur an, was
    // unter seinem Namen fehlt, und ueberschreibt nie. Je Profil steht das Typprofil vor dem Kopf. Traegt
    // ein EIGENER Satz des Anwenders (ReadOnly = 0) den Typnamen, uebergeht der Schritt das ganze Profil -
    // ein gelieferter Kopf zeigte sonst auf ein fremdes Wochenprofil -; traegt er den Bezeichner, bleibt
    // er stehen. Beides nennt das Protokoll. Feste Ids gibt es nicht (AUTOINCREMENT).
    //
    // KATALOGSCHLUESSEL. Danach belegt KatalogSchluesselSaat Schluessel und Pruefsumme der gesperrten
    // Saetze beider Tabellen, die noch keinen tragen - Verweisziel (SVT) vor Verweiser (SV), wie die
    // Katalogfassung Stufe 2 (KatalogfassungStufe2Schema) es fuer den Bestand getan hat. Damit fuehren
    // Auslieferungsvorlage, Katalogpaket und Katalogabgleich die drei Saetze wie jeden gelieferten Satz
    // (Konzept Setup 6.5.5: Katalog-DML-Schritte schreiben die Pruefsumme gesperrter Saetze mit).
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt einen der Saetze - die Projekte rechnen mit ihren
    // Projektkopien; neue Katalogzeilen verschieben allein die Ids des Katalogs.
    //
    // NUMMER. 193 = TypaufbauSchema.SCHRITT + 1, hinter 191 (RaumgrundrissSchema) und 192 (TypaufbauSchema)
    // der Sitzung IFC. Eingetragen in SchemaStand.Zielversion, im Register der Paketanhebung (Art Katalog),
    // in der SchemaMigration der Schale, in Werkzeuge/Testdatenbankschema und in EPOS.Kern.Tests/TestDatenbank.
    // ====================================================================================

    /// <summary>
    /// <b>Der Schemaschritt der BDEW-Standardlastprofile Strom 2025</b> (H25, G25, L25) — EINE Quelle für
    /// die Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c>, die Testvorrichtung und den Nachweis.
    /// </summary>
    public static class StandardlastprofilSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Wird der Schritt beim
        /// Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = TypaufbauSchema.SCHRITT + 1;

        /// <summary>Der Katalog der Köpfe („Datenbank Strombedarf").</summary>
        public const string TAB_KOPF = "Tab_Stromverbraucher_STAMM";

        /// <summary>Der Katalog der Typprofile (Wochenprofile).</summary>
        public const string TAB_TYP = "Tab_Stromverbrauchertyp_STAMM";

        /// <summary>Stunden eines Wochenprofils.</summary>
        public const int WOCHENSTUNDEN = 168;

        /// <summary>Monatswerte eines Kopfes.</summary>
        public const int MONATE = 12;

        /// <summary>Die drei Sätze (<see cref="StandardlastprofilSaattabelle.Alle"/>).</summary>
        public static IReadOnlyList<StandardlastprofilSaat> Saat => StandardlastprofilSaattabelle.Alle;

        /// <summary>Die Tabellen, die der Schritt voraussetzt (samt Katalogspalten der Stufe 2).</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return TAB_TYP;
            yield return TAB_KOPF;
        }

        /// <summary>Die beiden Katalogtabellen des Registers, Verweisziel vor Verweiser (SVT, SV).</summary>
        public static IReadOnlyList<Katalogtabelle> Katalogtabellen()
            => new[] { Katalogfassung.Tabelle(TAB_TYP), Katalogfassung.Tabelle(TAB_KOPF) };

        /// <summary>Die Spalten „1" bis „168", je in Anführungszeichen.</summary>
        private static readonly string WOCHENSPALTEN = string.Join(", ",
            Enumerable.Range(1, WOCHENSTUNDEN).Select(i => "\"" + i.ToString(CultureInfo.InvariantCulture) + "\""));

        /// <summary>168 Platzhalter.</summary>
        private static readonly string WOCHENPLAETZE = string.Join(", ", Enumerable.Repeat("?", WOCHENSTUNDEN));

        /// <summary>
        /// Die Anweisung für EIN Typprofil — Typname, Beschreibung und die 168 Stunden als <c>?</c>-Parameter
        /// (<see cref="TypParameter"/>), fest <c>ReadOnly = 1</c>. Die Spaltenliste entsteht aus den Zahlen
        /// 1 bis 168, Werte stehen nie im Text.
        /// </summary>
        public static string TypAnweisung()
            => "INSERT INTO \"Tab_Stromverbrauchertyp_STAMM\" (\"Typname\", \"Beschreibung\", " + WOCHENSPALTEN +
               ", \"ReadOnly\") VALUES (?, ?, " + WOCHENPLAETZE + ", 1)";

        /// <summary>
        /// Die Anweisung für EINEN Kopf — Bezeichner, Typ, Beschreibung und die zwölf Monatswerte als
        /// <c>?</c>-Parameter (<see cref="KopfParameter"/>), fest <c>ReadOnly = 1</c>.
        /// </summary>
        public const string KOPF_ANWEISUNG =
            "INSERT INTO \"Tab_Stromverbraucher_STAMM\" (\"Bezeichner\", \"Typ\", \"Beschreibung\", \"Monat_1\", " +
            "\"Monat_2\", \"Monat_3\", \"Monat_4\", \"Monat_5\", \"Monat_6\", \"Monat_7\", \"Monat_8\", \"Monat_9\", " +
            "\"Monat_10\", \"Monat_11\", \"Monat_12\", \"ReadOnly\") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1)";

        /// <summary>Die 170 Parameter eines Typprofils in der Folge von <see cref="TypAnweisung"/>.</summary>
        public static DbParam[] TypParameter(StandardlastprofilSaat s)
        {
            var p = new List<DbParam>(WOCHENSTUNDEN + 2)
            {
                new DbParam("@typname", s.Typname),
                new DbParam("@besch", s.Typbeschreibung),
            };
            for (int h = 0; h < WOCHENSTUNDEN; h++)
                p.Add(new DbParam("@h" + (h + 1).ToString("D3", CultureInfo.InvariantCulture), s.Wochenwerte[h]));
            return p.ToArray();
        }

        /// <summary>Die 15 Parameter eines Kopfes in der Folge von <see cref="KOPF_ANWEISUNG"/>.</summary>
        public static DbParam[] KopfParameter(StandardlastprofilSaat s)
        {
            var p = new List<DbParam>(MONATE + 3)
            {
                new DbParam("@bez", s.Bezeichner),
                new DbParam("@typ", s.Typname),
                new DbParam("@besch", s.Beschreibung),
            };
            for (int m = 0; m < MONATE; m++)
                p.Add(new DbParam("@m" + (m + 1).ToString("D2", CultureInfo.InvariantCulture), s.Monatswerte[m]));
            return p.ToArray();
        }

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zahl der in diesem Lauf angelegten Köpfe.</summary>
            public int KoepfeGesaet { get; internal set; }

            /// <summary>Zahl der in diesem Lauf angelegten Typprofile.</summary>
            public int TypenGesaet { get; internal set; }

            /// <summary>Bezeichner, unter denen schon ein Kopf stand — übergangen, nicht überschrieben.</summary>
            public List<string> Vorhanden { get; } = new List<string>();

            /// <summary>Namen (Bezeichner oder Typname), unter denen ein EIGENER Satz des Anwenders steht.</summary>
            public List<string> Eigene { get; } = new List<string>();

            /// <summary>Zahl der Sätze, die in diesem Lauf Katalogschlüssel und Prüfsumme bekamen.</summary>
            public int Schluessel { get; internal set; }

            /// <summary>Die Zeile für Protokoll und Werkzeug.</summary>
            public string Zeile()
                => KoepfeGesaet.ToString(CultureInfo.InvariantCulture) + " von " + Saat.Count.ToString(CultureInfo.InvariantCulture) +
                   " BDEW-Standardlastprofil(en) gesaet (ReadOnly = 1), " + TypenGesaet.ToString(CultureInfo.InvariantCulture) +
                   " Typprofil(e), " + Schluessel.ToString(CultureInfo.InvariantCulture) + " Satz/Saetze mit Katalogschluessel, " +
                   Vorhanden.Count.ToString(CultureInfo.InvariantCulture) + " stand(en) bereits";
        }

        /// <summary>
        /// Steht jedes Profil unter seinen Namen im Katalog — Typprofil und Kopf, oder das Typprofil als eigener
        /// Satz des Anwenders (dann ist das Profil benannt übergangen) —, und trägt jeder gesperrte Satz beider
        /// Tabellen Schlüssel und Prüfsumme?
        /// </summary>
        public static bool Vollstaendig()
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t)) return false;
            foreach (StandardlastprofilSaat s in Saat)
            {
                if (Anzahl(null, "SELECT COUNT(*) FROM \"Tab_Stromverbrauchertyp_STAMM\" WHERE \"Typname\" = ?",
                           new DbParam("@t", s.Typname)) == 0)
                    return false;
                if (Anzahl(null, "SELECT COUNT(*) FROM \"Tab_Stromverbraucher_STAMM\" WHERE \"Bezeichner\" = ?",
                           new DbParam("@b", s.Bezeichner)) == 0 && !TypEigen(null, s))
                    return false;
            }
            return KatalogSchluesselSaat.OffeneSaetze(Katalogtabellen()) == 0;
        }

        /// <summary>
        /// Schreibt die fehlenden Sätze, je Profil in EINEM Vorgang (Typprofil, dann Kopf), und belegt danach
        /// Schlüssel und Prüfsumme. <b>Wiederholbar und nie überschreibend</b>; eigene Sätze des Anwenders unter
        /// einem der Namen kommen ins Protokoll (<paramref name="bericht"/>, darf <c>null</c> sein). Setzt die
        /// Katalogspalten der Stufe 2 voraus (<see cref="KatalogfassungStufe2Schema"/>). Fehler werfen — der
        /// Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            var b = new Bericht();
            foreach (StandardlastprofilSaat s in Saat)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        bool kopfDa = Anzahl(v, "SELECT COUNT(*) FROM \"Tab_Stromverbraucher_STAMM\" WHERE \"Bezeichner\" = ?",
                                             new DbParam("@b", s.Bezeichner)) > 0;
                        bool kopfEigen = kopfDa && Anzahl(v,
                            "SELECT COUNT(*) FROM \"Tab_Stromverbraucher_STAMM\" WHERE \"Bezeichner\" = ? AND \"ReadOnly\" = 0",
                            new DbParam("@b", s.Bezeichner)) > 0;
                        if (kopfDa)
                        {
                            b.Vorhanden.Add(s.Bezeichner);
                            if (kopfEigen)
                            {
                                b.Eigene.Add(s.Bezeichner);
                                bericht?.Add("Standardlastprofil \"" + s.Bezeichner + "\" nicht gesaet: ein eigener Satz traegt den Namen");
                            }
                        }

                        if (TypEigen(v, s))
                        {
                            b.Eigene.Add(s.Typname);
                            if (!kopfDa)
                                bericht?.Add("Standardlastprofil \"" + s.Bezeichner + "\" nicht gesaet: ein eigenes Typprofil traegt den Namen \"" +
                                             s.Typname + "\"");
                            v.Commit();
                            continue;
                        }

                        if (Anzahl(v, "SELECT COUNT(*) FROM \"Tab_Stromverbrauchertyp_STAMM\" WHERE \"Typname\" = ?",
                                   new DbParam("@t", s.Typname)) == 0)
                        {
                            v.Ausfuehren(TypAnweisung(), TypParameter(s));
                            b.TypenGesaet++;
                        }
                        if (!kopfDa)
                        {
                            v.Ausfuehren(KOPF_ANWEISUNG, KopfParameter(s));
                            b.KoepfeGesaet++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            b.Schluessel = KatalogSchluesselSaat.Ausfuehren(bericht, Katalogtabellen());
            bericht?.Add(b.Zeile());
            return b;
        }

        /// <summary>Trägt ein eigener Satz des Anwenders (<c>ReadOnly = 0</c>) den Typnamen des Profils?</summary>
        private static bool TypEigen(DbVorgang v, StandardlastprofilSaat s)
            => Anzahl(v, "SELECT COUNT(*) FROM \"Tab_Stromverbrauchertyp_STAMM\" WHERE \"Typname\" = ? AND \"ReadOnly\" = 0",
                      new DbParam("@t", s.Typname)) > 0;

        private static long Anzahl(DbVorgang v, string sql, params DbParam[] p)
        {
            object o = v != null ? v.Skalar(sql, p) : DataRepository.ExecuteScalar(sql, p);
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
