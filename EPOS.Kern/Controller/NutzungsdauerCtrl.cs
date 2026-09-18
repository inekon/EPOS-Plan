using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Zeile der Nutzungsdauertabelle (<c>Tab_Nutzungsdauer</c>, Konzept 2.2).
    /// <c>null</c> heisst durchgaengig „nicht gepflegt", nie 0.
    /// </summary>
    public sealed class NutzungsdauerZeile
    {
        public int Id;

        /// <summary>Technik; <c>null</c> = technikuebergreifend (Montage, Planung).</summary>
        public int? KomponentenId;

        /// <summary>Anzeigename der Technik; leer bei den technikuebergreifenden Zeilen.</summary>
        public string Technik = "";

        public string Positionsart = "";

        /// <summary>Standardzeile ihrer Technik — der Rueckfall ohne Positionsart.</summary>
        public bool IstStandard;

        /// <summary>Rechnerische Nutzungsdauer [a]; <c>null</c> = wie die Standardzeile.</summary>
        public double? Nutzungsdauer;

        /// <summary>Steuerliche Nutzungsdauer [a] — nur Anzeige (ND-Q1).</summary>
        public double? AfaSteuerlich;

        /// <summary>Instandsetzungssatz [%] — angelegt mit S1, sichtbar ab S3 (ND-Q6).</summary>
        public double? InstandsetzungProzent;

        /// <summary>Wartungssatz [%] — angelegt mit S1, sichtbar ab S3 (ND-Q6).</summary>
        public double? WartungProzent;

        public string Quelle = "";

        /// <summary>Auslieferungszeile: Wert editierbar, Zeile NICHT loeschbar (ND-Q5).</summary>
        public bool NurLesen;

        public int Sortierung;
    }

    /// <summary>
    /// Die aufgeloeste Vorgabe zu einer Position: der Wert, der Herleitungstext und die
    /// Zeile, aus der er stammt (Konzept 2.4.1). <c>Wert</c> ist <c>null</c>, wenn die
    /// Technik keine Standardzeile hat — dann wird NICHTS vorbelegt.
    /// </summary>
    public sealed class NutzungsdauerVorgabe
    {
        public double? Wert;

        /// <summary>Der Satz unter dem Raster — leer, wenn es nichts herzuleiten gibt.</summary>
        public string Herleitung = "";

        /// <summary>Die Zeile, aus der der Wert kommt; <c>null</c> = keine gefunden.</summary>
        public NutzungsdauerZeile Quellzeile;
    }

    /// <summary>
    /// <b>Die Nutzungsdauern (AfA)</b> — Lesen, Pflegen und Aufloesen der Tabelle
    /// <c>Tab_Nutzungsdauer</c> (Konzept „Nutzungsdauer je Technik und Positionsart aus
    /// einer AfA-Tabelle", Stufe S1; Anwenderentscheide ND-Q1 bis ND-Q8 vom 14.09.2026).
    ///
    /// <para><b>Der Dialog rechnet und schreibt nicht selbst</b> (Hausmuster): Pruef- und
    /// Schreiblogik liegen hier, oberflaechenfrei und testbar. Die Huelle in
    /// <c>EPOS.UI.Daten</c> wandelt zwischen diesen Klassen und den Anzeigezeilen der
    /// Razor-Komponente.</para>
    ///
    /// <para><b>Zwei Regeln, die hier und nur hier stehen.</b> Erstens: Eine
    /// AUSLIEFERUNGSZEILE (<c>ReadOnly</c>) ist im WERT aenderbar, aber nicht loeschbar
    /// (ND-Q5) — „die Tabelle ist die Tabelle des Anwenders", und
    /// <see cref="AuslieferungWiederherstellen"/> holt die Saatwerte zurueck. Zweitens:
    /// Eine Nutzungsdauer <c>NULL</c> heisst „wie die Standardzeile der Technik der
    /// Position" und wird in <see cref="Vorgabe"/> aufgeloest, nicht in der
    /// Datenbank.</para>
    ///
    /// <para><b>Tolerant gegenueber einer nicht migrierten Datenbank</b> (Muster
    /// <c>KostenVorlagenCtrl.PflichtSpalteVorhanden</c>): Fehlt die Tabelle, liefern die
    /// Lesewege leere Listen und <see cref="Vorgabe"/> eine Vorgabe ohne Wert — dann
    /// belegt niemand etwas vor, und nichts bricht.</para>
    /// </summary>
    public static class NutzungsdauerCtrl
    {
        // ------------------------------------------------------------------ Lesen ---

        /// <summary>Gibt es die Tabelle? Die Antwort wird je Prozess gemerkt.</summary>
        private static bool? _tabelleDa;

        /// <summary>
        /// Probe auf <c>Tab_Nutzungsdauer</c>. Sie wird gemerkt, weil jede Vorbelegung
        /// sie stellt; <see cref="ProbeVergessen"/> setzt sie im Test zurueck.
        /// </summary>
        public static bool TabelleVorhanden()
        {
            if (_tabelleDa.HasValue) return _tabelleDa.Value;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                    new DbParam("@t", NutzungsdauerSchema.TABELLE));
                _tabelleDa = o != null && o != DBNull.Value &&
                             Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0;
            }
            catch { _tabelleDa = false; }
            return _tabelleDa.Value;
        }

        /// <summary>Vergisst die Tabellenprobe — fuer Tests, die die Datenbank wechseln.</summary>
        public static void ProbeVergessen()
        {
            _tabelleDa = null;
            _verweisSpalte.Clear();
        }

        /// <summary>Ergebnis der Spaltenprobe je Tabelle, je Prozess gemerkt.</summary>
        private static readonly Dictionary<string, bool> _verweisSpalte =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Traegt <paramref name="tabelle"/> die Verweisspalte <c>NutzungsdauerID</c>?
        /// Muster <c>KostenVorlagenCtrl.PflichtSpalteVorhanden</c>: Eine nie migrierte
        /// Datenbank liefert sonst einen Abfragefehler.
        /// </summary>
        public static bool VerweisSpalteVorhanden(string tabelle)
        {
            if (string.IsNullOrEmpty(tabelle)) return false;
            bool da;
            if (_verweisSpalte.TryGetValue(tabelle, out da)) return da;

            da = WirtschaftlichkeitCtrl.SpalteVorhanden(tabelle, NutzungsdauerSchema.SPALTE_VERWEIS);
            _verweisSpalte[tabelle] = da;
            return da;
        }

        /// <summary>
        /// Alle Zeilen, <b>nach Technik gruppiert und Standard zuerst</b>: die
        /// technikbezogenen Zeilen in der Reihenfolge der Komponenten, danach die
        /// technikuebergreifenden.
        /// </summary>
        public static IList<NutzungsdauerZeile> Alle()
        {
            var liste = new List<NutzungsdauerZeile>();
            if (!TabelleVorhanden()) return liste;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT n.[ID], n.[" + NutzungsdauerSchema.SPALTE_KOMPONENTENID + "], k.[" +
                SchemaKatalog.SPALTE_KK_KOMPONENTE + "], n.[" +
                NutzungsdauerSchema.SPALTE_POSITIONSART + "], n.[" +
                NutzungsdauerSchema.SPALTE_IST_STANDARD + "], n.[" +
                NutzungsdauerSchema.SPALTE_NUTZUNGSDAUER + "], n.[" +
                NutzungsdauerSchema.SPALTE_AFA + "], n.[" +
                NutzungsdauerSchema.SPALTE_INSTANDSETZUNG + "], n.[" +
                NutzungsdauerSchema.SPALTE_WARTUNG + "], n.[" +
                NutzungsdauerSchema.SPALTE_QUELLE + "], n.[" +
                NutzungsdauerSchema.SPALTE_READONLY + "], n.[" +
                NutzungsdauerSchema.SPALTE_SORTIERUNG + "] " +
                "FROM [" + NutzungsdauerSchema.TABELLE + "] n LEFT JOIN [" +
                SchemaKatalog.TAB_KOSTENKOMPONENTE + "] k ON k.[ID] = n.[" +
                NutzungsdauerSchema.SPALTE_KOMPONENTENID + "] " +
                // Die technikuebergreifenden Zeilen ganz nach hinten, innerhalb einer
                // Technik der Standard zuerst. Die Reihenfolge einer Boolean-Spalte
                // wird AUSGEDRUECKT, nicht der Kodierung ueberlassen
                // (BETRIEB_SQLITE.md 6.3).
                "ORDER BY CASE WHEN n.[" + NutzungsdauerSchema.SPALTE_KOMPONENTENID +
                "] IS NULL THEN 1 ELSE 0 END, n.[" + NutzungsdauerSchema.SPALTE_KOMPONENTENID +
                "], CASE WHEN n.[" + NutzungsdauerSchema.SPALTE_IST_STANDARD +
                "] = 1 THEN 0 ELSE 1 END, n.[" + NutzungsdauerSchema.SPALTE_SORTIERUNG + "], n.[ID]");

            foreach (DataRow r in dt.Rows) liste.Add(AusZeile(r));
            return liste;
        }

        /// <summary>Eine Zeile ueber ihre Id; <c>null</c>, wenn es sie nicht gibt.</summary>
        public static NutzungsdauerZeile Zeile(int id)
        {
            if (id <= 0 || !TabelleVorhanden()) return null;
            foreach (NutzungsdauerZeile z in Alle()) if (z.Id == id) return z;
            return null;
        }

        /// <summary>
        /// Die Standardzeile einer Technik — der Rueckfall, wenn eine Position keine
        /// Positionsart traegt. <c>null</c>, wenn die Technik keine hat.
        /// </summary>
        public static NutzungsdauerZeile Standard(int komponentenId)
        {
            if (komponentenId <= 0 || !TabelleVorhanden()) return null;
            foreach (NutzungsdauerZeile z in Alle())
                if (z.IstStandard && z.KomponentenId.HasValue && z.KomponentenId.Value == komponentenId)
                    return z;
            return null;
        }

        /// <summary>
        /// <b>Die Aufloesung</b> (Konzept 2.4.1): der Wert, mit dem eine Position der
        /// Technik <paramref name="komponentenId"/> vorbelegt wird.
        ///
        /// <list type="number">
        ///   <item>Traegt die Position einen Verweis und hat dessen Zeile einen Wert, gilt
        ///   dieser.</item>
        ///   <item>Traegt sie einen Verweis auf eine Zeile OHNE Wert — „Montage",
        ///   „Planung / Baunebenkosten" —, gilt die Standardzeile ihrer Technik; die
        ///   Herleitung nennt beide.</item>
        ///   <item>Ohne Verweis gilt die Standardzeile.</item>
        ///   <item>Gibt es keine Standardzeile, bleibt der Wert <c>null</c> — es wird
        ///   NICHTS vorbelegt und nichts erfunden.</item>
        /// </list>
        /// </summary>
        public static NutzungsdauerVorgabe Vorgabe(int komponentenId, int? nutzungsdauerId)
        {
            var v = new NutzungsdauerVorgabe();
            if (!TabelleVorhanden()) return v;

            NutzungsdauerZeile gewaehlt = nutzungsdauerId.HasValue && nutzungsdauerId.Value > 0
                ? Zeile(nutzungsdauerId.Value) : null;

            if (gewaehlt != null && gewaehlt.Nutzungsdauer.HasValue)
            {
                v.Wert = gewaehlt.Nutzungsdauer;
                v.Quellzeile = gewaehlt;
                v.Herleitung = Herleitung(gewaehlt, gewaehlt);
                return v;
            }

            NutzungsdauerZeile standard = Standard(komponentenId);
            if (standard == null) return v;

            v.Wert = standard.Nutzungsdauer;
            v.Quellzeile = standard;
            v.Herleitung = Herleitung(gewaehlt, standard);
            return v;
        }

        /// <summary>
        /// Der Satz unter dem Raster. <paramref name="gewaehlt"/> ist die Zeile der
        /// Positionsart (darf <c>null</c> sein), <paramref name="wirksam"/> die, deren
        /// Wert gilt.
        /// </summary>
        private static string Herleitung(NutzungsdauerZeile gewaehlt, NutzungsdauerZeile wirksam)
        {
            if (wirksam == null || !wirksam.Nutzungsdauer.HasValue) return "";

            string wert = wirksam.Nutzungsdauer.Value.ToString("0.###", CultureInfo.CurrentCulture);
            string technik = string.IsNullOrEmpty(wirksam.Technik)
                ? Text("ND_TECHNIKUEBERGREIFEND", "technikübergreifend") : wirksam.Technik;

            if (gewaehlt != null && gewaehlt.Id != wirksam.Id)
                return string.Format(CultureInfo.CurrentCulture,
                    Text("ND_HERLEITUNG_STANDARD",
                         "Nutzungsdauer aus Nutzungsdauern (AfA): {0} → wie Standardzeile {1} · {2} {3} a"),
                    gewaehlt.Positionsart, technik, wirksam.Positionsart, wert);

            return string.Format(CultureInfo.CurrentCulture,
                Text("ND_HERLEITUNG",
                     "Nutzungsdauer aus Nutzungsdauern (AfA): {0} · {1} {2} a"),
                technik, wirksam.Positionsart, wert);
        }

        /// <summary>
        /// <b>STUFE S2 — WOHER die Nutzungsdauer einer Position stammt</b>, als
        /// fertiger Satz: „Vorgabe der Technik", „AfA-Tabelle: ‹Positionsart›",
        /// „eigener Wert" oder „keine Dauer gepflegt".
        ///
        /// <para>Gemessen wird am Wert, nicht an einer Merkspalte: Der gepflegte Wert
        /// gilt als übernommen, solange er der Vorgabe entspricht — sobald der Anwender
        /// ihn ändert, ist er ein eigener. Eine zusätzliche Spalte „woher" wäre eine
        /// zweite Wahrheit, die beim ersten Tippen falsch würde.</para>
        /// </summary>
        /// <param name="komponentenId">Die Technik der Position.</param>
        /// <param name="nutzungsdauerId">Die Positionsart; <c>null</c> = keine.</param>
        /// <param name="wert">Die gepflegte Nutzungsdauer [a]; <c>null</c> bzw.
        /// <c>&lt; 1</c> heißt „keine" (dieselbe Schwelle wie im Rechenkern).</param>
        public static string Herkunft(int komponentenId, int? nutzungsdauerId, double? wert)
        {
            if (!wert.HasValue || wert.Value < 1.0)
                return Text("ND_HERK_KEINE", "keine Dauer gepflegt");

            NutzungsdauerVorgabe v = Vorgabe(komponentenId, nutzungsdauerId);
            if (!v.Wert.HasValue || Math.Abs(v.Wert.Value - wert.Value) > 1e-9)
                return Text("ND_QUELLE_EIGEN", "eigener Wert");

            NutzungsdauerZeile gewaehlt = nutzungsdauerId.HasValue && nutzungsdauerId.Value > 0
                ? Zeile(nutzungsdauerId.Value) : null;

            if (gewaehlt != null && gewaehlt.Nutzungsdauer.HasValue)
                return string.Format(CultureInfo.CurrentCulture,
                    Text("ND_HERK_ART", "AfA-Tabelle: {0}"), gewaehlt.Positionsart);

            return Text("ND_HERK_TECHNIK", "Vorgabe der Technik");
        }

        /// <summary>
        /// STUFE S2: Die HERLEITUNGSZEILE unter dem Nutzungsdauerfeld des Rasters —
        /// „15 a · Vorgabe der Technik". Leer, wo keine Dauer gepflegt ist: Dort steht
        /// ein leeres Feld, und der Grund gehört unter die Tafel „Ersatz und Restwert",
        /// nicht in jede einzelne Zeile.
        /// </summary>
        public static string Herleitungszeile(int komponentenId, int? nutzungsdauerId,
                                              double? wert)
        {
            if (!wert.HasValue || wert.Value < 1.0) return "";
            return string.Format(CultureInfo.CurrentCulture,
                Text("ND_ZEILE_HERLEITUNG", "{0} a · {1}"),
                wert.Value.ToString("0.###", CultureInfo.CurrentCulture),
                Herkunft(komponentenId, nutzungsdauerId, wert));
        }

        /// <summary>
        /// STUFE S2: Die wählbaren POSITIONSARTEN einer Technik für den Zeileneditor —
        /// die Zeilen dieser Technik, danach die technikübergreifenden (Konzept 2.4.3).
        /// Leere Liste, solange es die Tabelle nicht gibt.
        /// </summary>
        public static IList<NutzungsdauerZeile> Arten(int komponentenId)
        {
            var liste = new List<NutzungsdauerZeile>();
            foreach (NutzungsdauerZeile z in Alle())
                if (z.KomponentenId.HasValue && z.KomponentenId.Value == komponentenId)
                    liste.Add(z);
            foreach (NutzungsdauerZeile z in Alle())
                if (!z.KomponentenId.HasValue) liste.Add(z);
            return liste;
        }

        // -------------------------------------------------------------- Schreiben ---

        /// <summary>
        /// Legt eine EIGENE Zeile an (nie eine Auslieferungszeile). Rueckgabe: Id oder 0;
        /// <paramref name="grund"/> nennt die Ablehnung im Klartext.
        /// </summary>
        public static int Neu(int? komponentenId, string positionsart, double? nutzungsdauer,
                              double? afa, string quelle, out string grund)
        {
            grund = "";
            if (!TabelleVorhanden())
            {
                grund = Text("ND_MELD_KEINE_TABELLE",
                             "Die Nutzungsdauertabelle gibt es in dieser Datenbank noch nicht.");
                return 0;
            }
            if (string.IsNullOrWhiteSpace(positionsart))
            {
                grund = Text("ND_MELD_ART_LEER", "Die Positionsart darf nicht leer sein.");
                return 0;
            }

            string art = positionsart.Trim();
            if (NutzungsdauerSchema.ZeileZu(komponentenId, art) > 0)
            {
                grund = string.Format(CultureInfo.CurrentCulture,
                    Text("ND_MELD_DOPPELT", "Für diese Technik gibt es die Positionsart „{0}“ bereits."),
                    art);
                return 0;
            }

            int id = MaxId() + 1;
            int n = DataRepository.ExecuteNonQuery(
                "INSERT INTO [" + NutzungsdauerSchema.TABELLE + "] ([ID], [" +
                NutzungsdauerSchema.SPALTE_KOMPONENTENID + "], [" +
                NutzungsdauerSchema.SPALTE_POSITIONSART + "], [" +
                NutzungsdauerSchema.SPALTE_IST_STANDARD + "], [" +
                NutzungsdauerSchema.SPALTE_NUTZUNGSDAUER + "], [" +
                NutzungsdauerSchema.SPALTE_AFA + "], [" +
                NutzungsdauerSchema.SPALTE_QUELLE + "], [" +
                NutzungsdauerSchema.SPALTE_READONLY + "], [" +
                NutzungsdauerSchema.SPALTE_SORTIERUNG + "]) VALUES (?, ?, ?, 0, ?, ?, ?, 0, ?)",
                new DbParam("@id", id),
                NutzungsdauerSchema.Ganz("@kid", komponentenId),
                new DbParam("@art", art),
                NutzungsdauerSchema.Wert("@nd", nutzungsdauer),
                NutzungsdauerSchema.Wert("@afa", afa),
                new DbParam("@q", quelle ?? ""),
                new DbParam("@so", NaechsteSortierung(komponentenId)));
            return n == 1 ? id : 0;
        }

        /// <summary>
        /// Schreibt die Fachfelder einer Zeile. <b>Auch eine Auslieferungszeile</b> —
        /// ihr WERT ist aenderbar (ND-Q5); <c>IstStandard</c>, <c>ReadOnly</c> und der
        /// Schluessel (Technik, Positionsart) bleiben, was sie sind.
        /// </summary>
        public static bool Speichern(NutzungsdauerZeile z, out string grund)
        {
            grund = "";
            if (z == null || z.Id <= 0 || !TabelleVorhanden()) return false;

            NutzungsdauerZeile alt = Zeile(z.Id);
            if (alt == null) return false;

            // Die Positionsart einer AUSLIEFERUNGSzeile bleibt: An ihr haengen die
            // Verweise der Auslieferungspositionen und die Wiederherstellung.
            string art = alt.NurLesen || string.IsNullOrWhiteSpace(z.Positionsart)
                ? alt.Positionsart : z.Positionsart.Trim();

            if (!string.Equals(art, alt.Positionsart, StringComparison.Ordinal) &&
                NutzungsdauerSchema.ZeileZu(alt.KomponentenId, art) > 0)
            {
                grund = string.Format(CultureInfo.CurrentCulture,
                    Text("ND_MELD_DOPPELT", "Für diese Technik gibt es die Positionsart „{0}“ bereits."),
                    art);
                return false;
            }

            return DataRepository.ExecuteNonQuery(
                "UPDATE [" + NutzungsdauerSchema.TABELLE + "] SET [" +
                NutzungsdauerSchema.SPALTE_POSITIONSART + "] = ?, [" +
                NutzungsdauerSchema.SPALTE_NUTZUNGSDAUER + "] = ?, [" +
                NutzungsdauerSchema.SPALTE_AFA + "] = ?, [" +
                NutzungsdauerSchema.SPALTE_INSTANDSETZUNG + "] = ?, [" +
                NutzungsdauerSchema.SPALTE_WARTUNG + "] = ?, [" +
                NutzungsdauerSchema.SPALTE_QUELLE + "] = ? WHERE [ID] = ?",
                new DbParam("@art", art),
                NutzungsdauerSchema.Wert("@nd", z.Nutzungsdauer),
                NutzungsdauerSchema.Wert("@afa", z.AfaSteuerlich),
                NutzungsdauerSchema.Wert("@in", z.InstandsetzungProzent),
                NutzungsdauerSchema.Wert("@wa", z.WartungProzent),
                new DbParam("@q", z.Quelle ?? ""),
                new DbParam("@id", z.Id)) == 1;
        }

        /// <summary>
        /// Loescht eine EIGENE Zeile. Eine Auslieferungszeile wird <b>benannt
        /// abgelehnt</b> (ND-Q5) — sie bleibt, und ihr Wert laesst sich stattdessen
        /// aendern oder zuruecksetzen.
        /// </summary>
        public static bool Loeschen(int id, out string grund)
        {
            grund = "";
            if (id <= 0 || !TabelleVorhanden()) return false;

            NutzungsdauerZeile z = Zeile(id);
            if (z == null) return false;

            if (z.NurLesen)
            {
                grund = Text("ND_MELD_READONLY",
                             "Auslieferungszeilen werden nicht gelöscht. Ihr Wert lässt sich ändern; " +
                             "„Auslieferungswerte wiederherstellen“ setzt ihn zurück.");
                return false;
            }

            // Eine geloeschte Zeile darf keinen Verweis hinterlassen, der ins Leere
            // zeigt: Positionen, die auf sie zeigten, fallen auf den Technik-Standard
            // zurueck - genau das druecken sie mit NULL aus.
            VerweiseLoesen(id);

            return DataRepository.ExecuteNonQuery(
                "DELETE FROM [" + NutzungsdauerSchema.TABELLE + "] WHERE [ID] = ?",
                new DbParam("@id", id)) == 1;
        }

        /// <summary>
        /// Setzt die Werte der AUSLIEFERUNGSZEILEN auf die Saat zurueck und legt
        /// fehlende Saatzeilen wieder an (ND-Q5). Eigene Zeilen bleiben unberuehrt.
        /// </summary>
        /// <returns>Zahl der zurueckgesetzten und neu angelegten Zeilen.</returns>
        public static int AuslieferungWiederherstellen()
        {
            if (!TabelleVorhanden()) return 0;

            int beruehrt = NutzungsdauerSchema.SaatSchreiben();

            foreach (NutzungsdauerSaat s in NutzungsdauerSchema.Saat)
            {
                int id = NutzungsdauerSchema.ZeileZu(s.KomponentenId, s.Positionsart);
                if (id <= 0) continue;

                beruehrt += DataRepository.ExecuteNonQuery(
                    "UPDATE [" + NutzungsdauerSchema.TABELLE + "] SET [" +
                    NutzungsdauerSchema.SPALTE_NUTZUNGSDAUER + "] = ?, [" +
                    NutzungsdauerSchema.SPALTE_AFA + "] = ?, [" +
                    NutzungsdauerSchema.SPALTE_QUELLE + "] = ?, [" +
                    NutzungsdauerSchema.SPALTE_IST_STANDARD + "] = ?, [" +
                    NutzungsdauerSchema.SPALTE_READONLY + "] = 1 WHERE [ID] = ?",
                    NutzungsdauerSchema.Wert("@nd", s.Nutzungsdauer),
                    NutzungsdauerSchema.Wert("@afa", s.AfaSteuerlich),
                    new DbParam("@q", s.Quelle),
                    new DbParam("@std", s.IstStandard ? 1 : 0),
                    new DbParam("@id", id));
            }
            return beruehrt;
        }

        // ----------------------------------------------------------------- intern ---

        private static NutzungsdauerZeile AusZeile(DataRow r)
        {
            return new NutzungsdauerZeile
            {
                Id = Convert.ToInt32(r[0], CultureInfo.InvariantCulture),
                KomponentenId = GanzOderNull(r[1]),
                Technik = r[2] == DBNull.Value ? "" : (Convert.ToString(r[2]) ?? ""),
                Positionsart = Convert.ToString(r[3]) ?? "",
                IstStandard = r[4] != DBNull.Value && Convert.ToBoolean(r[4]),
                Nutzungsdauer = WertOderNull(r[5]),
                AfaSteuerlich = WertOderNull(r[6]),
                InstandsetzungProzent = WertOderNull(r[7]),
                WartungProzent = WertOderNull(r[8]),
                Quelle = r[9] == DBNull.Value ? "" : (Convert.ToString(r[9]) ?? ""),
                NurLesen = r[10] != DBNull.Value && Convert.ToBoolean(r[10]),
                Sortierung = r[11] == DBNull.Value ? 0 : Convert.ToInt32(r[11], CultureInfo.InvariantCulture),
            };
        }

        /// <summary>Loest die Verweise auf eine Zeile, die gleich verschwindet.</summary>
        private static void VerweiseLoesen(int id)
        {
            foreach (string tabelle in new[]
                     {
                         SchemaKatalog.TAB_KOSTENVORLAGEPOSITION,
                         SchemaKatalog.TAB_PROJEKTWERTE
                     })
            {
                try
                {
                    DataRepository.ExecuteNonQuery(
                        "UPDATE [" + tabelle + "] SET [" + NutzungsdauerSchema.SPALTE_VERWEIS +
                        "] = NULL WHERE [" + NutzungsdauerSchema.SPALTE_VERWEIS + "] = ?",
                        new DbParam("@id", id));
                }
                catch { }   // eine nie migrierte Datenbank kennt die Spalte nicht
            }
        }

        private static int MaxId()
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MAX([ID]) FROM [" + NutzungsdauerSchema.TABELLE + "]");
            return (o == null || o == DBNull.Value)
                ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static int NaechsteSortierung(int? komponentenId)
        {
            object o = komponentenId.HasValue
                ? DataRepository.ExecuteScalar(
                    "SELECT MAX([" + NutzungsdauerSchema.SPALTE_SORTIERUNG + "]) FROM [" +
                    NutzungsdauerSchema.TABELLE + "] WHERE [" +
                    NutzungsdauerSchema.SPALTE_KOMPONENTENID + "] = ?",
                    new DbParam("@kid", komponentenId.Value))
                : DataRepository.ExecuteScalar(
                    "SELECT MAX([" + NutzungsdauerSchema.SPALTE_SORTIERUNG + "]) FROM [" +
                    NutzungsdauerSchema.TABELLE + "] WHERE [" +
                    NutzungsdauerSchema.SPALTE_KOMPONENTENID + "] IS NULL");

            int max = (o == null || o == DBNull.Value)
                ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            return max + 10;
        }

        private static double? WertOderNull(object o)
        {
            return (o == null || o == DBNull.Value)
                ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        private static int? GanzOderNull(object o)
        {
            return (o == null || o == DBNull.Value)
                ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Ressourcentext mit deutschem Rueckfall (Hausmuster der Huellen).</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
