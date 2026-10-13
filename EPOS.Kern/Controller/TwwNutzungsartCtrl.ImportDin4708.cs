using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Katalogimport der DIN-4708-Katalogwerte</b> (Konzept Zapfprofil-Benutzerführung, ZB7):
    /// der dritte Teil von <see cref="TwwNutzungsartCtrl"/>s Katalogimport —
    /// <c>Tab_TwwDin4708Wert_STAMM</c>, die Belegung je Raumzahl (Art <c>BELEGUNG</c>) und die
    /// Ausstattungsklassen (Art <c>AUSSTATTUNG</c>, Wert Σ v·w_v in Wh). Sie sind der einzige
    /// Pflegeweg für diese Werte im Programm; einen Editor gibt es dafür nicht.
    ///
    /// <para><b>Format.</b> Dieselben Formregeln wie die übrigen Dateien des Pakets: Kopfzeile,
    /// Trenner <c>;</c> oder <c>,</c>, Zahlen mit Punkt, eine unbekannte Spalte oder eine fehlende
    /// Pflichtspalte (<c>Art</c>, <c>Schluessel</c>, <c>Wert</c>, <c>Quelle</c>, <c>Version</c>,
    /// <c>Herkunftsart</c>) lehnt das Paket als Ganzes ab. Die Spalte <c>Katalogversion</c> ist
    /// wahlfrei nach derselben Regel wie die übrigen Kopfdateien
    /// (<see cref="TwwNutzungsartCtrl.IMPORT_TABELLEN_MIT_VERSION"/>).</para>
    ///
    /// <para><b>Inhalt je Zeile</b> — ein Fehler lehnt nur diese Zeile ab, die übrigen kommen:
    /// <list type="bullet">
    /// <item><c>Art</c> ist <c>BELEGUNG</c> oder <c>AUSSTATTUNG</c> (Groß-/Kleinschreibung frei,
    /// gespeichert groß) — sonst <c>KATALOGIMPORT_DIN4708_ART</c>.</item>
    /// <item><c>BELEGUNG</c>: Der Schlüssel ist eine Raumzahl über 0 in halben Schritten. Er wird so
    /// gespeichert, wie der Rechenweg ihn sucht — als Raumzahl in invarianter Schreibweise
    /// (<c>2</c>, <c>2.5</c>; „2.0" wird „2") —, sonst <c>KATALOGIMPORT_DIN4708_RAUMZAHL</c>.</item>
    /// <item><c>AUSSTATTUNG</c>: Der Schlüssel ist nicht leer.</item>
    /// <item><c>Wert</c> ist angegeben (sonst <c>KATALOGIMPORT_PFLICHT_FEHLT</c>) und größer als 0
    /// (sonst <c>KATALOGIMPORT_DIN4708_WERT</c>); nicht endliche Zahlen sind schon ein Formfehler.</item>
    /// </list></para>
    ///
    /// <para><b>Ersetzen statt Versionsbildung</b> — wie beim Parameter: Eine Zeile ist über
    /// (<c>Art</c>, <c>Schluessel</c>, <c>Katalogversion</c>) bestimmt. Gleicher Wert —
    /// übersprungen; abweichender Wert — <b>am Platz ersetzt</b>, mit derselben <c>ID</c>, damit
    /// <c>Tab_TwwWohnungstyp.ID_Ausstattung</c> weiter trifft; die Zeile ist danach eine
    /// Anwenderzeile (<c>Status = 'IMPORT'</c>, <c>ReadOnly = 0</c>, ohne <c>Beleg</c>) mit der
    /// Provenienz des Pakets, und der Bericht nennt alten und neuen Wert
    /// (<c>KATALOGIMPORT_DIN4708_ERSETZT</c>). Eine neue Zeile wird angelegt.</para>
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        /// <summary>Die Dateien der Nutzungsarten-Kette — ein Paket mit einer davon braucht die Nutzungsarten.</summary>
        private static readonly string[] NUTZUNGSART_KETTE =
        {
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_TAGESGANG_STAMM,
            TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM
        };

        /// <summary>
        /// <b>Ist das Paket ein Kennwertpaket?</b> Es führt <c>Tab_TwwDin4708Wert_STAMM.csv</c> und
        /// keine Datei der Nutzungsarten-Kette — dann braucht es <c>Tab_TwwNutzungsart_STAMM.csv</c>
        /// nicht. Jedes andere Paket ohne die Nutzungsarten bleibt abgelehnt
        /// (<c>KATALOGIMPORT_KEINE_DATEI</c>).
        /// </summary>
        private static bool Kennwertpaket(IReadOnlyList<TwwPaketdatei> dateien)
        {
            bool Hat(string tabelle) => dateien.Any(d => string.Equals(tabelle + ".csv", d.Name, StringComparison.OrdinalIgnoreCase));
            return Hat(TwwSchema.TAB_TWW_DIN4708_WERT_STAMM) && !NUTZUNGSART_KETTE.Any(Hat);
        }

        /// <summary>Eine Zeile der DIN-4708-Katalogwerte des Pakets und — wenn sie nicht taugt — der Grund.</summary>
        private sealed class PaketDin4708Zeile
        {
            internal int Zeile { get; set; }

            /// <summary>Die Art, groß geschrieben; bei einer unbekannten Art der Text des Pakets.</summary>
            internal string Art { get; set; } = "";

            /// <summary>Der Schlüssel — bei <c>BELEGUNG</c> die normalisierte Raumzahl.</summary>
            internal string Schluessel { get; set; } = "";

            internal string Katalogversion { get; set; } = "";
            internal double Wert { get; set; }
            internal Provenienz Herkunft { get; set; }
            internal ZapfSatz Fehler { get; set; }

            /// <summary>Der Name der Zeile im Bericht: Art und Schlüssel.</summary>
            internal string Name => (Art + " " + Schluessel).Trim();
        }

        /// <summary>
        /// <b>Die Raumzahl als Schlüssel</b>: über 0, endlich, in halben Schritten — dann die
        /// Schreibweise, unter der der Rechenweg sie sucht (<c>Raumzahl.ToString(InvariantCulture)</c>);
        /// sonst <c>null</c>.
        /// </summary>
        internal static string Din4708Raumzahlschluessel(string text)
        {
            string roh = (text ?? "").Trim();
            if (!double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out double r)
                || double.IsNaN(r) || double.IsInfinity(r) || r <= 0.0) return null;
            double halbe = Math.Round(r * 2.0);
            if (Math.Abs(r * 2.0 - halbe) > 1e-9 || halbe < 1.0) return null;
            return (halbe / 2.0).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Die DIN-4708-Zeilen des Pakets; je Zeile die Inhaltsprüfung (Klassenkommentar).</summary>
        private static List<PaketDin4708Zeile> Din4708Zeilen(Dictionary<string, PaketTabelle> tabellen, Paket paket)
        {
            var liste = new List<PaketDin4708Zeile>();
            if (!tabellen.TryGetValue(TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, out PaketTabelle td)) return liste;

            Pflicht(td, "Art", "Schluessel", "Wert", "Quelle", "Version", "Herkunftsart");
            if (paket.Zielversion == null) Pflicht(td, SPALTE_KATALOGVERSION);
            var schluessel = new HashSet<string>(StringComparer.Ordinal);
            foreach (var z in td.Zeilen)
            {
                string art = td.Text(z, "Art");
                var p = new PaketDin4708Zeile
                {
                    Zeile = z.Zeile,
                    Art = art,
                    Schluessel = td.Text(z, "Schluessel"),
                    Katalogversion = paket.Version(td, z)
                };

                // Der Wert wird zuerst gelesen: Eine Zahl mit Komma ist ein Formfehler des Pakets.
                double? wert = td.Zahl(z, "Wert");
                p.Wert = wert ?? 0.0;

                if (string.Equals(art, TwwSchema.DIN4708_ART_BELEGUNG, StringComparison.OrdinalIgnoreCase))
                {
                    p.Art = TwwSchema.DIN4708_ART_BELEGUNG;
                    string raum = Din4708Raumzahlschluessel(p.Schluessel);
                    if (raum == null) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_DIN4708_RAUMZAHL", p.Schluessel);
                    else p.Schluessel = raum;
                }
                else if (string.Equals(art, TwwSchema.DIN4708_ART_AUSSTATTUNG, StringComparison.OrdinalIgnoreCase))
                {
                    p.Art = TwwSchema.DIN4708_ART_AUSSTATTUNG;
                    if (p.Schluessel.Length == 0) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Schluessel");
                }
                else p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_DIN4708_ART", art, TwwSchema.DIN4708_ART_BELEGUNG,
                                            TwwSchema.DIN4708_ART_AUSSTATTUNG);

                if (p.Fehler == null && p.Katalogversion.Length == 0)
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", SPALTE_KATALOGVERSION);
                if (p.Fehler == null && !wert.HasValue) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Wert");
                if (p.Fehler == null && !(p.Wert > 0.0)) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_DIN4708_WERT", p.Name, p.Wert);

                Provenienz h = Herkunft(td, z, "", out ZapfSatz fh);
                p.Herkunft = h;
                if (fh != null) p.Fehler ??= fh;

                if (p.Fehler == null && !schluessel.Add(p.Art + "\u0001" + p.Schluessel + "\u0001" + p.Katalogversion))
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_DOPPELT_IM_PAKET");
                liste.Add(p);
            }
            return liste;
        }

        /// <summary>
        /// Eine DIN-4708-Zeile des Pakets: anlegen, überspringen oder den Wert am Platz ersetzen.
        /// <paramref name="auslieferung"/> sammelt die Namen ersetzter Auslieferungszeilen für den Hinweis.
        /// </summary>
        private static TwwImportzeile Din4708Einspielen(DbVorgang v, PaketDin4708Zeile p, List<string> auslieferung)
        {
            if (p.Fehler != null)
                return new TwwImportzeile(TwwImportausgang.Abgelehnt, p.Name, p.Katalogversion, "", 0, p.Zeile, p.Fehler)
                { Bereich = TwwImportbereich.Din4708 };

            string version = p.Katalogversion.Trim();
            Provenienz h = ImportHerkunft(p.Herkunft);

            DataTable dt = v.Lese("SELECT ID, Wert, Status FROM " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM +
                                  " WHERE Art = ? AND Schluessel = ? AND Katalogversion = ?",
                                  new DbParam("@art", p.Art), new DbParam("@s", p.Schluessel), new DbParam("@k", version));
            if (dt != null && dt.Rows.Count == 1)
            {
                DataRow r = dt.Rows[0];
                int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                double altWert = Convert.ToDouble(r["Wert"], CultureInfo.InvariantCulture);
                if (altWert.Equals(p.Wert))
                    return new TwwImportzeile(TwwImportausgang.Uebersprungen, p.Name, version, p.Name, 0, p.Zeile,
                                              ZapfSatz.Neu("KATALOGIMPORT_GLEICH_VORHANDEN"))
                    { Bereich = TwwImportbereich.Din4708 };

                ZapfKatalogstatus alt = TwwWertemengen.Status(Convert.ToString(r["Status"], CultureInfo.InvariantCulture));
                v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM + " SET Wert = ?, Quelle = ?, Ausgabe = ?, " +
                             "Version = ?, Herkunftsart = ?, Status = ?, Beleg = NULL, ReadOnly = 0 WHERE ID = ?",
                             new DbParam("@w", p.Wert), new DbParam("@q", h.Quelle), new DbParam("@aus", h.Ausgabe),
                             new DbParam("@ver", h.Version), new DbParam("@h", TwwWertemengen.Text(h.Art)),
                             new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Import)), new DbParam("@id", id));
                if (alt == ZapfKatalogstatus.Auslieferung) auslieferung.Add(p.Name);
                return new TwwImportzeile(TwwImportausgang.Ersetzt, p.Name, version, p.Name, id, p.Zeile,
                                          ZapfSatz.Neu("KATALOGIMPORT_DIN4708_ERSETZT", p.Name, altWert, p.Wert))
                { Bereich = TwwImportbereich.Din4708, IdErsetzt = id };
            }

            int neuId = v.EinfuegenUndId(
                "INSERT INTO " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM +
                " (Art, Schluessel, Wert, Katalogversion, Quelle, Ausgabe, Version, Herkunftsart, Status, Beleg, ReadOnly) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, 0)",
                new[]
                {
                    new DbParam("@art", p.Art), new DbParam("@s", p.Schluessel), new DbParam("@w", p.Wert),
                    new DbParam("@k", version), new DbParam("@q", h.Quelle), new DbParam("@aus", h.Ausgabe),
                    new DbParam("@ver", h.Version), new DbParam("@h", TwwWertemengen.Text(h.Art)),
                    new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Import))
                });
            return new TwwImportzeile(TwwImportausgang.Angelegt, p.Name, version, p.Name, neuId, p.Zeile, null)
            { Bereich = TwwImportbereich.Din4708 };
        }
    }
}
