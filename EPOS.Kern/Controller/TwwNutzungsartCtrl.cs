using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>Wie eine Pflegeaktion am Katalog der Nutzungsarten ausgegangen ist — die benannte Ablehnung.</summary>
    internal enum TwwKatalogAusgang
    {
        /// <summary>Die Aktion ist geschrieben.</summary>
        Ausgefuehrt = 0,

        /// <summary>Die Zeile gibt es nicht (mehr).</summary>
        NichtGefunden = 1,

        /// <summary>Die Zeile gehört zur Auslieferung (<c>ReadOnly</c>) — nur „Speichern unter".</summary>
        ReadOnlyGesperrt = 2,

        /// <summary>Eine Zone benutzt die Zeile — sie ist unveränderlich (3.2), nur „Speichern unter".</summary>
        BenutztGesperrt = 3,

        /// <summary>(Bezeichner, Katalogversion) ist schon vergeben — es wurde nichts geschrieben.</summary>
        NameBelegt = 4,

        /// <summary>Dem Entwurf fehlt eine Pflichtangabe oder ein Raster hat die falsche Länge.</summary>
        EntwurfUnvollstaendig = 5,

        /// <summary>Der genannte Tagesgangsatz steht nicht im Katalog.</summary>
        TagesgangsatzFehlt = 6,

        /// <summary>Die Tww-Tabellen fehlen in dieser Datenbank.</summary>
        TabellenFehlen = 7,

        /// <summary>Die Datenbank hat die Anweisung nicht angenommen.</summary>
        Fehlgeschlagen = 8,

        /// <summary>
        /// Ein Raster verletzt die Regeln aus Konzept 3.1: ein Wert negativ oder nicht endlich,
        /// Wochenfaktoren bzw. ein Tagesgang nicht Σ 1, Monatsfaktoren nicht im Mittel 1.
        /// </summary>
        RasterUngueltig = 9
    }

    /// <summary>Ergebnis einer Pflegeaktion: Ausgang und die ID der betroffenen (bzw. neuen) Zeile.</summary>
    internal sealed record TwwKatalogErgebnis(TwwKatalogAusgang Ausgang, int Id)
    {
        /// <summary>Ist die Aktion geschrieben?</summary>
        public bool Ok => Ausgang == TwwKatalogAusgang.Ausgefuehrt;
    }

    /// <summary>
    /// Ergebnis von <see cref="TwwNutzungsartCtrl.TagesgangSpeichern"/>: Ausgang, die Nutzungsart,
    /// die jetzt die Werte trägt (dieselbe oder die per „Speichern unter" neue), und ihr
    /// Tagesgangsatz (derselbe oder ein neuer); bei einer Ablehnung 0.
    /// </summary>
    internal sealed record TwwTagesgangErgebnis(TwwKatalogAusgang Ausgang, int IdNutzungsart, int IdTagesgangsatz)
    {
        /// <summary>Ist die Aktion geschrieben (oder war nichts zu schreiben)?</summary>
        public bool Ok => Ausgang == TwwKatalogAusgang.Ausgefuehrt;
    }

    /// <summary>Kurzform einer Nutzungsart für die Katalogliste: ohne Werte, mit Verwendung.</summary>
    internal sealed record TwwNutzungsartZeile(
        int Id,
        string Bezeichner,
        string Katalogversion,
        ZapfBezugsart Bezug,
        ZapfKalenderart Kalender,
        Herkunftsart BedarfHerkunft,
        ZapfKatalogstatus Status,
        bool ReadOnly,
        bool Benutzt);

    /// <summary>
    /// <b>Die schreibbaren Werte einer Nutzungsart</b> — was „Neu", „Ändern" und „Speichern
    /// unter" in <c>Tab_TwwNutzungsart_STAMM</c> schreiben. Status, ReadOnly, Vorlage, Beleg
    /// und Freigabe setzt der Controller, nie der Entwurf. <see cref="Aus"/> übernimmt Werte
    /// und Provenienz einer gelesenen Zeile; geändert wird mit <c>with</c>.
    /// </summary>
    internal sealed record TwwNutzungsartEntwurf
    {
        public string Bezeichner { get; init; } = "";
        public string Katalogversion { get; init; } = "";
        public ZapfBezugsart Bezug { get; init; }

        /// <summary>kWh je Einheit und Tag je Niveau (niedrig, mittel, hoch).</summary>
        public double[] Bedarf { get; init; } = new double[NutzungsartRaster.NIVEAUS];

        public double?[] BedarfMin { get; init; } = new double?[NutzungsartRaster.NIVEAUS];
        public double?[] BedarfMax { get; init; } = new double?[NutzungsartRaster.NIVEAUS];
        public Provenienz BedarfHerkunft { get; init; }
        public Temperaturbezug Bezugstemperaturen { get; init; }
        public ZapfBilanzgrenze Grenze { get; init; }
        public ZapfKalenderart Kalender { get; init; }
        public double? Ferienfaktor { get; init; }
        public double[] Monatsfaktoren { get; init; } = new double[NutzungsartRaster.MONATE];
        public Provenienz JahresgangHerkunft { get; init; }
        public double[] Wochenfaktoren { get; init; } = new double[NutzungsartRaster.WOCHENTAGE];
        public Provenienz WochengangHerkunft { get; init; }
        public int IdTagesgangsatz { get; init; }

        /// <summary>Werte und Provenienz einer Katalogzeile — Kopien der Raster, keine Verweise.</summary>
        internal static TwwNutzungsartEntwurf Aus(Nutzungsart n) => new TwwNutzungsartEntwurf
        {
            Bezeichner = n.Name,
            Katalogversion = n.Katalogversion,
            Bezug = n.Bezug,
            Bedarf = (double[])n.BedarfJeNiveauKwhJeEinheitTag.Clone(),
            BedarfMin = (double?[])n.Herkunft.Bandbreite.Min.Clone(),
            BedarfMax = (double?[])n.Herkunft.Bandbreite.Max.Clone(),
            BedarfHerkunft = n.Herkunft.Bedarf,
            Bezugstemperaturen = n.Bezugstemperaturen,
            Grenze = n.Grenze,
            Kalender = n.Kalender,
            Ferienfaktor = n.Ferienfaktor,
            Monatsfaktoren = (double[])n.Monatsfaktoren.Clone(),
            JahresgangHerkunft = n.Herkunft.Jahresgang,
            Wochenfaktoren = (double[])n.Wochenfaktoren.Clone(),
            WochengangHerkunft = n.Herkunft.Wochengang,
            IdTagesgangsatz = n.Tagesgaenge?.Id ?? 0
        };
    }

    /// <summary>
    /// <b>Die Katalogpflege der Brauchwasser-Nutzungsarten</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.2, 3.3; Stufe Z0, Posten P7) — Datenseite des späteren
    /// <c>TwwNutzungsartAdminDialog</c> (5.4).
    ///
    /// <para><b>Unveränderliche Katalogversionen statt Projektkopie (3.2).</b> Eine Zone
    /// verweist unmittelbar auf ihre Katalogzeile. Damit eine Katalogänderung kein
    /// Projektergebnis rückwirkend ändert, ist eine <b>benutzte</b> Zeile (eine Zeile in
    /// <c>Tab_TwwZone</c> verweist auf sie) ebenso wie eine Zeile der <b>Auslieferung</b>
    /// (<c>ReadOnly</c>) gesperrt: <see cref="Aendern"/> und <see cref="Loeschen"/> lehnen
    /// benannt ab. Jede Änderung einer gesperrten Zeile ergibt über
    /// <see cref="SpeichernUnter"/> eine NEUE Zeile — Status <c>EIGEN</c>, <c>ID_Vorlage</c>
    /// auf den Vorgänger. Eine freie Zeile (eigen, unbenutzt) lässt sich an Ort und Stelle
    /// ändern; ihr Vier-Augen-Vermerk entfällt dabei, weil er für die alten Werte galt.
    /// Dasselbe gilt für den Tagesgangsatz: <see cref="TagesgangSpeichern"/> schreibt einen
    /// benutzten oder ausgelieferten Satz nur als neue Zeile. Die Zapfkategorien (Schemaschritt
    /// T2) gehören zur Katalogversion ihrer Nutzungsart: Eine ausgelieferte Kategorie
    /// (<c>ReadOnly</c>) sperrt die Nutzungsart, und jede neue Zeile aus „Speichern unter" oder
    /// <see cref="TagesgangSpeichern"/> trägt die Kategorien ihrer Vorlage (Status <c>EIGEN</c>,
    /// <c>ReadOnly = 0</c>).</para>
    ///
    /// <para><b>Provenienz je Wertgruppe (3.1).</b> Beim Ändern und bei „Speichern unter"
    /// vergleicht der Controller jede Wertgruppe mit dem Bezug (gespeicherte Zeile bzw.
    /// Vorlage): Eine geänderte Gruppe trägt die neue Katalogversion und, solange der Entwurf
    /// die Provenienz des Bezugs unverändert mitbringt, Herkunftsart
    /// <c>EIGENKONSTRUKTION</c> mit neutraler Quelle (<see cref="QUELLE_EIGENKONSTRUKTION"/>);
    /// der interne Beleg gilt nur, solange keine Gruppe geändert ist.</para>
    ///
    /// <para><b>Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern</b>,
    /// Prüfung und Schreiben je Aktion in EINEM <see cref="DbVorgang"/>.</para>
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        /// <summary>
        /// Die Quelle einer vom Anwender gesetzten Wertgruppe — neutral, ohne Norm, Ausgabe oder
        /// Hersteller (Konzept 3.1, 6 (e)); ein Persistenzwert, kein Anzeigetext.
        /// </summary>
        internal const string QUELLE_EIGENKONSTRUKTION = "Eigenkonstruktion";

        /// <summary>Toleranz der Rasterregeln (Σ 1, Mittel 1).</summary>
        internal const double TOLERANZ = 1e-9;

        private const string SQL_INSERT =
            "INSERT INTO " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " (" +
            "Bezeichner, Katalogversion, Bezugsart, Bedarf_Niedrig, Bedarf_Mittel, Bedarf_Hoch, Bedarf_Niedrig_Min, Bedarf_Niedrig_Max, " +
            "Bedarf_Mittel_Min, Bedarf_Mittel_Max, Bedarf_Hoch_Min, Bedarf_Hoch_Max, Bedarf_Quelle, Bedarf_Ausgabe, Bedarf_Version, Bedarf_Herkunftsart, " +
            "Bezug_Zapftemperatur, Bezug_Kaltwasser, Bilanzgrenze, Kalenderart, Ferienfaktor, Monat_1, Monat_2, Monat_3, " +
            "Monat_4, Monat_5, Monat_6, Monat_7, Monat_8, Monat_9, Monat_10, Monat_11, " +
            "Monat_12, Jahresgang_Quelle, Jahresgang_Ausgabe, Jahresgang_Version, Jahresgang_Herkunftsart, Woche_1, Woche_2, Woche_3, " +
            "Woche_4, Woche_5, Woche_6, Woche_7, Wochengang_Quelle, Wochengang_Ausgabe, Wochengang_Version, Wochengang_Herkunftsart, " +
            "ID_Tagesgangsatz, ID_Vorlage, Status, Beleg, ReadOnly) VALUES (" +
            "?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, ?, ?, ?, ?, ?, " +
            "?, ?, (SELECT Beleg FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?), 0)";

        private const string SQL_UPDATE =
            "UPDATE " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " SET " +
            "Bezeichner = ?, Katalogversion = ?, Bezugsart = ?, Bedarf_Niedrig = ?, Bedarf_Mittel = ?, Bedarf_Hoch = ?, " +
            "Bedarf_Niedrig_Min = ?, Bedarf_Niedrig_Max = ?, Bedarf_Mittel_Min = ?, Bedarf_Mittel_Max = ?, Bedarf_Hoch_Min = ?, Bedarf_Hoch_Max = ?, " +
            "Bedarf_Quelle = ?, Bedarf_Ausgabe = ?, Bedarf_Version = ?, Bedarf_Herkunftsart = ?, Bezug_Zapftemperatur = ?, Bezug_Kaltwasser = ?, " +
            "Bilanzgrenze = ?, Kalenderart = ?, Ferienfaktor = ?, Monat_1 = ?, Monat_2 = ?, Monat_3 = ?, " +
            "Monat_4 = ?, Monat_5 = ?, Monat_6 = ?, Monat_7 = ?, Monat_8 = ?, Monat_9 = ?, " +
            "Monat_10 = ?, Monat_11 = ?, Monat_12 = ?, Jahresgang_Quelle = ?, Jahresgang_Ausgabe = ?, Jahresgang_Version = ?, " +
            "Jahresgang_Herkunftsart = ?, Woche_1 = ?, Woche_2 = ?, Woche_3 = ?, Woche_4 = ?, Woche_5 = ?, " +
            "Woche_6 = ?, Woche_7 = ?, Wochengang_Quelle = ?, Wochengang_Ausgabe = ?, Wochengang_Version = ?, Wochengang_Herkunftsart = ?, " +
            "ID_Tagesgangsatz = ?, Beleg = CASE WHEN ? = 1 THEN Beleg ELSE NULL END, Freigabe = NULL WHERE ID = ?";

        /// <summary>
        /// Ein Tagesgang: Satz, Tagtyp, die 24 Anteile und die Provenienz. Die Spaltennamen
        /// entstehen aus einer Schleife (<see cref="ZapfprofilCtrl.AnteilSpalte"/>), nie aus einer
        /// Eingabe (Konzept 3.1).
        /// </summary>
        private static readonly string SQL_INSERT_TAGESGANG = TagesgangInsert();

        private static string TagesgangInsert()
        {
            var spalten = new StringBuilder("ID_Tagesgangsatz, Tagtyp");
            var platz = new StringBuilder("?, ?");
            for (int h = 1; h <= Tagesgangsatz.STUNDEN; h++)
            {
                spalten.Append(", ").Append(ZapfprofilCtrl.AnteilSpalte(h));
                platz.Append(", ?");
            }
            return "INSERT INTO " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " (" + spalten +
                   ", Quelle, Ausgabe, Version, Herkunftsart) VALUES (" + platz + ", ?, ?, ?, ?)";
        }

        // =================================================================================
        // Lesen
        // =================================================================================

        /// <summary>
        /// Die Katalogliste in EINER Abfrage — je Zeile Verwaltungsangaben und ob eine Zone sie
        /// benutzt. Ohne Tabellen leer.
        /// </summary>
        internal static IReadOnlyList<TwwNutzungsartZeile> Liste()
        {
            var liste = new List<TwwNutzungsartZeile>();
            if (!TabellenVorhanden()) return liste;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT n.ID, n.Bezeichner, n.Katalogversion, n.Bezugsart, n.Kalenderart, n.Bedarf_Herkunftsart, " +
                "n.Status, n.ReadOnly, EXISTS (SELECT 1 FROM " + TwwSchema.TAB_TWW_ZONE + " AS z WHERE z.ID_Nutzungsart = n.ID) AS Benutzt " +
                "FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " AS n ORDER BY n.Bezeichner, n.Katalogversion, n.ID");
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
                liste.Add(new TwwNutzungsartZeile(
                    ZapfprofilCtrl.Ganz(r, "ID"),
                    ZapfprofilCtrl.Text(r, "Bezeichner"),
                    ZapfprofilCtrl.Text(r, "Katalogversion"),
                    (ZapfBezugsart)ZapfprofilCtrl.Ganz(r, "Bezugsart"),
                    (ZapfKalenderart)ZapfprofilCtrl.Ganz(r, "Kalenderart"),
                    TwwWertemengen.Herkunft(ZapfprofilCtrl.Text(r, "Bedarf_Herkunftsart")),
                    TwwWertemengen.Status(ZapfprofilCtrl.Text(r, "Status")),
                    ZapfprofilCtrl.Wahr(r, "ReadOnly"),
                    ZapfprofilCtrl.Wahr(r, "Benutzt")));
            return liste;
        }

        /// <summary>
        /// Die Zeilen der Katalogliste nach <see cref="Katalogfilterprofil.FuerTwwNutzungsart"/>
        /// (Konzept 5.4), aus <see cref="Liste"/> — EINE Abfrage. Der
        /// <see cref="Katalogfilterzeile.Schluessel"/> ist die ID: Der Bezeichner allein ist
        /// über zwei Katalogversionen nicht eindeutig. Geschützt ist eine ausgelieferte
        /// Zeile.
        ///
        /// <para><paramref name="text"/> übersetzt die Wertwörter der Aufzählungen; sein
        /// Schlüssel ist <c>ZPG_BEZUGSART_</c>, <c>ZPG_KALENDER_</c>, <c>ZPG_HERKUNFT_</c>
        /// bzw. <c>ZPG_STATUS_</c> plus der Name des Werts. Ohne Übersetzer (Tests, Kern
        /// ohne Oberfläche) steht der Name selbst da.</para>
        /// </summary>
        internal static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen(Func<string, string> text = null)
        {
            Func<string, string, string> t = (praefix, name) =>
            {
                string s = text?.Invoke(praefix + name);
                return string.IsNullOrEmpty(s) ? name : s;
            };

            var zeilen = new List<Katalogfilterzeile>();
            foreach (TwwNutzungsartZeile z in Liste())
            {
                var zeile = new Katalogfilterzeile(z.Id, z.Bezeichner)
                {
                    Schluessel = z.Id.ToString(CultureInfo.InvariantCulture),
                    Geschuetzt = z.ReadOnly
                };
                zeilen.Add(zeile
                    .MitText(Katalogfilterprofil.SpBezeichner, z.Bezeichner)
                    .MitText(Katalogfilterprofil.SpBezugsart, t("ZPG_BEZUGSART_", z.Bezug.ToString()))
                    .MitText(Katalogfilterprofil.SpKalender, t("ZPG_KALENDER_", z.Kalender.ToString()))
                    .MitText(Katalogfilterprofil.SpHerkunft, t("ZPG_HERKUNFT_", z.BedarfHerkunft.ToString()))
                    .MitText(Katalogfilterprofil.SpKatalogversion, z.Katalogversion)
                    .MitText(Katalogfilterprofil.SpStatus, t("ZPG_STATUS_", z.Status.ToString())));
            }
            return zeilen;
        }

        /// <summary>Eine Nutzungsart samt Tagesgangsatz; <c>null</c>, wenn es sie nicht gibt.</summary>
        internal static Nutzungsart Lies(int id) => ZapfprofilCtrl.LiesNutzungsart(id);

        /// <summary>
        /// Gehört die Zeile zur Auslieferung (<c>ReadOnly</c> an ihr oder an einer ihrer
        /// Zapfkategorien)? Eine fehlende Zeile ist es nicht.
        /// </summary>
        internal static bool IstReadOnly(int id)
        {
            if (!TabellenVorhanden()) return false;
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                new DbParam("@id", id));
            if (v == null || v == DBNull.Value) return false;
            if (Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0) return true;
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM)) return false;
            object k = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE ID_Nutzungsart = ? AND ReadOnly <> 0",
                new DbParam("@id", id));
            return k != null && k != DBNull.Value && Convert.ToInt64(k, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Benutzt eine Zone (<c>Tab_TwwZone.ID_Nutzungsart</c>) die Zeile?</summary>
        internal static bool IstBenutzt(int id)
        {
            if (!TabellenVorhanden()) return false;
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Nutzungsart = ?",
                new DbParam("@id", id));
            return v != null && v != DBNull.Value && Convert.ToInt64(v, CultureInfo.InvariantCulture) > 0;
        }

        // =================================================================================
        // Schreiben
        // =================================================================================

        /// <summary>„Neu": eine eigene Zeile (Status <c>EIGEN</c>, ohne Vorlage, ohne Beleg); die Provenienz steht im Entwurf.</summary>
        internal static TwwKatalogErgebnis Neu(TwwNutzungsartEntwurf e) => Anlegen(e, null);

        /// <summary>
        /// „Speichern unter": die Werte des Entwurfs als NEUE Zeile — Status <c>EIGEN</c>,
        /// <c>ReadOnly = 0</c>, <c>ID_Vorlage</c> auf <paramref name="idVorlage"/>. Die Provenienz
        /// führt der Controller je Wertgruppe gegen die Vorlage nach
        /// (<see cref="Nachgefuehrt"/>): eine unveränderte Gruppe behält ihre, eine geänderte
        /// trägt die neue Katalogversion und — solange der Entwurf die Provenienz der Vorlage
        /// unverändert mitbringt — Herkunftsart <c>EIGENKONSTRUKTION</c> mit neutraler Quelle.
        /// Der interne Beleg der Vorlage kommt nur mit, wenn keine Gruppe geändert ist. Der
        /// einzige Weg, eine gesperrte Zeile zu „ändern"; die Vorlage bleibt unberührt.
        /// (Bezeichner, Katalogversion) muss frei sein. Die Zapfkategorien der Vorlage kommen
        /// mit (Status <c>EIGEN</c>, <c>ReadOnly = 0</c>, Werte und Provenienz unverändert).
        /// </summary>
        internal static TwwKatalogErgebnis SpeichernUnter(int idVorlage, TwwNutzungsartEntwurf e) => Anlegen(e, idVorlage);

        /// <summary>
        /// „Ändern" an Ort und Stelle — nur für eine Zeile, die weder zur Auslieferung gehört
        /// noch von einer Zone benutzt wird; sonst benannt <see cref="TwwKatalogAusgang.ReadOnlyGesperrt"/>
        /// (Vorrang) bzw. <see cref="TwwKatalogAusgang.BenutztGesperrt"/>, und der Aufrufer bietet
        /// <see cref="SpeichernUnter"/>. Status und Vorlage bleiben, der Freigabevermerk entfällt;
        /// die Provenienz führt der Controller wie bei <see cref="SpeichernUnter"/> gegen den
        /// gespeicherten Stand nach, und der interne Beleg entfällt, sobald eine Gruppe geändert ist.
        /// </summary>
        internal static TwwKatalogErgebnis Aendern(int id, TwwNutzungsartEntwurf e)
        {
            if (!TabellenVorhanden()) return new TwwKatalogErgebnis(TwwKatalogAusgang.TabellenFehlen, id);
            if (!Vollstaendig(e)) return new TwwKatalogErgebnis(TwwKatalogAusgang.EntwurfUnvollstaendig, id);
            if (!RasterGueltig(e)) return new TwwKatalogErgebnis(TwwKatalogAusgang.RasterUngueltig, id);

            return Ausfuehren(id, v =>
            {
                TwwKatalogAusgang sperre = Sperre(v, id);
                if (sperre != TwwKatalogAusgang.Ausgefuehrt) return sperre;
                if (!TagesgangsatzVorhanden(v, e.IdTagesgangsatz)) return TwwKatalogAusgang.TagesgangsatzFehlt;
                if (NameVergeben(v, e.Bezeichner, e.Katalogversion, id)) return TwwKatalogAusgang.NameBelegt;

                TwwNutzungsartEntwurf bezug = Bezugszeile(v, id);
                if (bezug == null) return TwwKatalogAusgang.NichtGefunden;
                TwwNutzungsartEntwurf nachgefuehrt = ProvenienzNachfuehren(e, bezug, out bool geaendert);

                var p = Fachwerte(nachgefuehrt);
                p.Add(new DbParam("@beleg_behalten", geaendert ? 0 : 1));
                p.Add(new DbParam("@id", id));
                v.Ausfuehren(SQL_UPDATE, p.ToArray());
                return TwwKatalogAusgang.Ausgefuehrt;
            });
        }

        /// <summary>
        /// „Löschen" — nur eine Zeile, die weder zur Auslieferung gehört noch benutzt wird.
        /// Eine Nachfolgeversion, die auf sie als Vorlage zeigt, verliert nur den Verweis
        /// (<c>ON DELETE SET NULL</c>).
        /// </summary>
        internal static TwwKatalogErgebnis Loeschen(int id)
        {
            if (!TabellenVorhanden()) return new TwwKatalogErgebnis(TwwKatalogAusgang.TabellenFehlen, id);

            return Ausfuehren(id, v =>
            {
                TwwKatalogAusgang sperre = Sperre(v, id);
                if (sperre != TwwKatalogAusgang.Ausgefuehrt) return sperre;

                v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                             new DbParam("@id", id));
                return TwwKatalogAusgang.Ausgefuehrt;
            });
        }

        // =================================================================================
        // Tagesgang (Knopf „Tagesgang…", Konzept 5.4)
        // =================================================================================

        /// <summary>Gehört der Tagesgangsatz zur Auslieferung (<c>ReadOnly</c>)? Ein fehlender Satz ist es nicht.</summary>
        internal static bool TagesgangsatzIstReadOnly(int idSatz)
        {
            if (!TabellenVorhanden()) return false;
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " WHERE ID = ?",
                new DbParam("@id", idSatz));
            return v != null && v != DBNull.Value && Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0;
        }

        /// <summary>
        /// Benutzt eine Nutzungsart (Vorgabesatz, <c>Tab_TwwNutzungsart_STAMM.ID_Tagesgangsatz</c>)
        /// oder eine Zone (Expertenwahl, <c>Tab_TwwZone.ID_Tagesgangsatz</c>) den Satz? Dieselben
        /// Fundstellen wie die Verwendungsprüfung der Registry (<c>TWW_TAGESGANGSATZ</c>).
        /// </summary>
        internal static bool TagesgangsatzIstBenutzt(int idSatz)
        {
            if (!TabellenVorhanden()) return false;
            object v = DataRepository.ExecuteScalar(
                "SELECT (SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID_Tagesgangsatz = ?) + " +
                "(SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Tagesgangsatz = ?)",
                new DbParam("@n", idSatz), new DbParam("@z", idSatz));
            return v != null && v != DBNull.Value && Convert.ToInt64(v, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// <b>„Tagesgang…"</b> (Konzept 3.3, 5.4): die vier Tagesgänge des Satzes einer
        /// Nutzungsart und ihre Wochenfaktoren in EINEM Vorgang.
        ///
        /// <para><paramref name="tagesgaenge"/> trägt vier Reihen (Tagtyp 1 … 4: Werktag,
        /// Samstag, Sonn-/Feiertag, Ruhetag) zu je 24 Stundenanteilen, jede Reihe Σ 1;
        /// <paramref name="wochenfaktoren"/> sieben Werte Mo–So, Σ 1; alle Werte endlich und
        /// nicht negativ — sonst <see cref="TwwKatalogAusgang.RasterUngueltig"/>.</para>
        ///
        /// <para><b>Die Sperre (3.2).</b> Der <b>Satz</b> ist gesperrt, wenn er zur Auslieferung
        /// gehört, eine Zone ihn wählt, eine ANDERE Nutzungsart ihn trägt oder die Nutzungsart
        /// selbst gesperrt ist; dann entsteht bei geändertem Tagesgang ein NEUER Satz
        /// (Bezeichner des alten, Katalogversion <paramref name="katalogversion"/>, Status
        /// <c>EIGEN</c>) samt vier Tagesgängen — die unveränderten mit ihrer Provenienz, die
        /// geänderten als Eigenkonstruktion, außer eine geänderte Reihe ist bitgleich (Toleranz
        /// <see cref="GLEICH_TOLERANZ"/>) einer Reihe des Satzes <paramref name="idVorlageSatz"/>
        /// (Editor „Vorlage laden") — dann trägt sie dessen Provenienz. Ist die <b>Nutzungsart</b> gesperrt (ReadOnly oder
        /// von einer Zone benutzt), entsteht sie per „Speichern unter" neu (Katalogversion
        /// <paramref name="katalogversion"/>, <c>ID_Vorlage</c> auf die alte) und trägt Satz,
        /// Wochenfaktoren und die Zapfkategorien der alten (Status <c>EIGEN</c>, <c>ReadOnly = 0</c>);
        /// die alte bleibt unberührt. Sonst wird an Ort und Stelle geschrieben
        /// und der Freigabevermerk entfällt. <paramref name="katalogversion"/> ist nur Pflicht,
        /// wenn eine neue Zeile entsteht (sonst <see cref="TwwKatalogAusgang.EntwurfUnvollstaendig"/>);
        /// ist ihr natürlicher Schlüssel vergeben, <see cref="TwwKatalogAusgang.NameBelegt"/>.</para>
        ///
        /// <para>Ist nichts geändert, wird nichts geschrieben; das Ergebnis nennt die
        /// bisherigen IDs.</para>
        /// </summary>
        internal static TwwTagesgangErgebnis TagesgangSpeichern(int idNutzungsart, IReadOnlyList<double[]> tagesgaenge,
                                                                double[] wochenfaktoren, string katalogversion = null,
                                                                int? idVorlageSatz = null)
        {
            if (!TabellenVorhanden() || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANG_STAMM))
                return new TwwTagesgangErgebnis(TwwKatalogAusgang.TabellenFehlen, idNutzungsart, 0);
            if (!TagesgaengeGueltig(tagesgaenge) || !SummeEins(wochenfaktoren, NutzungsartRaster.WOCHENTAGE))
                return new TwwTagesgangErgebnis(TwwKatalogAusgang.RasterUngueltig, idNutzungsart, 0);

            string neueVersion = string.IsNullOrWhiteSpace(katalogversion) ? null : katalogversion.Trim();
            int idNeu = idNutzungsart, satzNeu = 0;

            TwwKatalogErgebnis erg = Ausfuehren(idNutzungsart, v =>
            {
                TwwNutzungsartEntwurf bezug = Bezugszeile(v, idNutzungsart);
                if (bezug == null) return TwwKatalogAusgang.NichtGefunden;
                int satz = bezug.IdTagesgangsatz;
                satzNeu = satz;

                DataTable kopf = v.Lese("SELECT Bezeichner, Katalogversion, ReadOnly FROM " +
                                        TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " WHERE ID = ?", new DbParam("@id", satz));
                if (kopf == null || kopf.Rows.Count == 0) return TwwKatalogAusgang.TagesgangsatzFehlt;
                string satzName = ZapfprofilCtrl.Text(kopf.Rows[0], "Bezeichner");
                string satzVersion = ZapfprofilCtrl.Text(kopf.Rows[0], "Katalogversion");

                // Die gespeicherten Tagesgänge je Tagtyp (fehlt einer, gilt er als geändert).
                (double[][] alt, Provenienz[] altHerkunft) = TagesgangZeilenLesen(v, satz);

                // Die Vorlage, aus der der Editor geladen haben könnte („Vorlage laden") — nur zur
                // Herkunftszuordnung einer geänderten Reihe, die ihr bitgleich ist.
                double[][] vorlageWerte = null;
                Provenienz[] vorlageHerkunft = null;
                if (idVorlageSatz.HasValue && idVorlageSatz.Value != satz && idVorlageSatz.Value > 0)
                    (vorlageWerte, vorlageHerkunft) = TagesgangZeilenLesen(v, idVorlageSatz.Value);

                var tagGeaendert = new bool[Tagesgangsatz.TAGTYPEN];
                bool tagesgangGeaendert = false;
                for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
                {
                    tagGeaendert[t] = alt[t] == null || !Gleich(alt[t], tagesgaenge[t]);
                    tagesgangGeaendert |= tagGeaendert[t];
                }
                bool wocheGeaendert = !Gleich(bezug.Wochenfaktoren, wochenfaktoren);
                if (!tagesgangGeaendert && !wocheGeaendert) return TwwKatalogAusgang.Ausgefuehrt;

                // Die Herkunft einer geänderten Reihe: die Vorlage, wenn die Reihe ihr bitgleich
                // ist, sonst Eigenkonstruktion.
                Provenienz HerkunftGeaendert(int t, string version)
                    => vorlageWerte != null && vorlageWerte[t] != null && Gleich(vorlageWerte[t], tagesgaenge[t])
                        ? vorlageHerkunft[t]
                        : Eigenkonstruktion(version);

                bool nutzungsartGesperrt = Sperre(v, idNutzungsart) != TwwKatalogAusgang.Ausgefuehrt;
                bool satzGesperrt = nutzungsartGesperrt
                    || ZapfprofilCtrl.Wahr(kopf.Rows[0], "ReadOnly")
                    || Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Tagesgangsatz = ?", satz) > 0
                    || Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                                 " WHERE ID_Tagesgangsatz = ? AND ID <> ?", satz, idNutzungsart) > 0;

                bool neuerSatz = tagesgangGeaendert && satzGesperrt;
                if ((neuerSatz || nutzungsartGesperrt) && neueVersion == null) return TwwKatalogAusgang.EntwurfUnvollstaendig;

                // --- Tagesgänge -----------------------------------------------------------
                if (neuerSatz)
                {
                    if (v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                                 " WHERE Bezeichner = ? AND Katalogversion = ?",
                                 new DbParam("@b", satzName), new DbParam("@k", neueVersion)) != null)
                        return TwwKatalogAusgang.NameBelegt;

                    satzNeu = v.EinfuegenUndId(
                        "INSERT INTO " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                        " (Bezeichner, Katalogversion, Status, Beleg, ReadOnly) VALUES (?, ?, ?, NULL, 0)",
                        new[] { new DbParam("@b", satzName), new DbParam("@k", neueVersion),
                                new DbParam("@s", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)) });
                    for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
                        TagesgangEinfuegen(v, satzNeu, t + 1, tagesgaenge[t],
                                           tagGeaendert[t] ? HerkunftGeaendert(t, neueVersion) : altHerkunft[t]);
                }
                else if (tagesgangGeaendert)
                {
                    for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
                    {
                        if (!tagGeaendert[t]) continue;
                        if (alt[t] != null)
                            v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                                         " WHERE ID_Tagesgangsatz = ? AND Tagtyp = ?",
                                         new DbParam("@id", satz), new DbParam("@t", t + 1));
                        TagesgangEinfuegen(v, satz, t + 1, tagesgaenge[t], HerkunftGeaendert(t, satzVersion));
                    }
                    v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " SET Beleg = NULL WHERE ID = ?",
                                 new DbParam("@id", satz));
                }

                // --- Nutzungsart ----------------------------------------------------------
                string version = nutzungsartGesperrt ? neueVersion : bezug.Katalogversion;
                TwwNutzungsartEntwurf e = bezug with
                {
                    Katalogversion = version,
                    Wochenfaktoren = (double[])wochenfaktoren.Clone(),
                    WochengangHerkunft = Nachgefuehrt(bezug.WochengangHerkunft, bezug.WochengangHerkunft, wocheGeaendert, version),
                    IdTagesgangsatz = satzNeu
                };

                if (nutzungsartGesperrt)
                {
                    if (NameVergeben(v, e.Bezeichner, e.Katalogversion, null)) return TwwKatalogAusgang.NameBelegt;
                    var p = Fachwerte(e);
                    p.Add(new DbParam("@vorlage", idNutzungsart));
                    p.Add(new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)));
                    p.Add(new DbParam("@beleg", wocheGeaendert ? null : (object)idNutzungsart));
                    idNeu = v.EinfuegenUndId(SQL_INSERT, p.ToArray());
                    // Die neue Version rechnet stochastisch wie die alte: ihre Zapfkategorien kommen mit.
                    KategorienKopieren(v, idNutzungsart, idNeu);
                }
                else
                {
                    var p = Fachwerte(e);
                    p.Add(new DbParam("@beleg_behalten", wocheGeaendert ? 0 : 1));
                    p.Add(new DbParam("@id", idNutzungsart));
                    v.Ausfuehren(SQL_UPDATE, p.ToArray());
                }
                return TwwKatalogAusgang.Ausgefuehrt;
            });

            return erg.Ok
                ? new TwwTagesgangErgebnis(TwwKatalogAusgang.Ausgefuehrt, idNeu, satzNeu)
                : new TwwTagesgangErgebnis(erg.Ausgang, idNutzungsart, 0);
        }

        // =================================================================================

        private static TwwKatalogErgebnis Anlegen(TwwNutzungsartEntwurf e, int? idVorlage)
        {
            if (!TabellenVorhanden()) return new TwwKatalogErgebnis(TwwKatalogAusgang.TabellenFehlen, 0);
            if (!Vollstaendig(e)) return new TwwKatalogErgebnis(TwwKatalogAusgang.EntwurfUnvollstaendig, 0);
            if (!RasterGueltig(e)) return new TwwKatalogErgebnis(TwwKatalogAusgang.RasterUngueltig, 0);

            int neu = 0;
            TwwKatalogErgebnis erg = Ausfuehren(0, v =>
            {
                TwwNutzungsartEntwurf zuSchreiben = e;
                bool geaendert = true;
                if (idVorlage.HasValue)
                {
                    TwwNutzungsartEntwurf bezug = Bezugszeile(v, idVorlage.Value);
                    if (bezug == null) return TwwKatalogAusgang.NichtGefunden;
                    zuSchreiben = ProvenienzNachfuehren(e, bezug, out geaendert);
                }
                if (!TagesgangsatzVorhanden(v, e.IdTagesgangsatz)) return TwwKatalogAusgang.TagesgangsatzFehlt;
                if (NameVergeben(v, e.Bezeichner, e.Katalogversion, null)) return TwwKatalogAusgang.NameBelegt;

                var p = Fachwerte(zuSchreiben);
                p.Add(new DbParam("@vorlage", idVorlage.HasValue ? (object)idVorlage.Value : null));
                p.Add(new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)));
                // Der interne Beleg der Vorlage gilt nur für ihre Werte - mit, solange keine Gruppe geändert ist.
                p.Add(new DbParam("@beleg", idVorlage.HasValue && !geaendert ? (object)idVorlage.Value : null));
                neu = v.EinfuegenUndId(SQL_INSERT, p.ToArray());
                // „Speichern unter" nimmt die Zapfkategorien der Vorlage mit (Status EIGEN, ReadOnly 0).
                if (idVorlage.HasValue) KategorienKopieren(v, idVorlage.Value, neu);
                return TwwKatalogAusgang.Ausgefuehrt;
            });
            return erg.Ok ? new TwwKatalogErgebnis(TwwKatalogAusgang.Ausgefuehrt, neu) : erg;
        }

        /// <summary>
        /// Eine Aktion in EINEM Vorgang: Prüfung und Schreiben sehen denselben Stand. Eine
        /// Ablehnung rollt zurück; ein Wurf der Datenbank wird zu
        /// <see cref="TwwKatalogAusgang.Fehlgeschlagen"/> — außer der Lesemodus-Sperre der
        /// Lizenz, die ihre eigene benannte Meldung trägt.
        /// </summary>
        private static TwwKatalogErgebnis Ausfuehren(int id, Func<DbVorgang, TwwKatalogAusgang> aktion)
        {
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    TwwKatalogAusgang a = aktion(v);
                    if (a == TwwKatalogAusgang.Ausgefuehrt) v.Commit();
                    else v.Rollback();
                    return new TwwKatalogErgebnis(a, id);
                }
            }
            catch (Exception ex) when (ex is not LesemodusException)
            {
                Console.WriteLine("Fehler in der Pflege der Brauchwasser-Nutzungsarten: " + ex.Message);
                return new TwwKatalogErgebnis(TwwKatalogAusgang.Fehlgeschlagen, id);
            }
        }

        /// <summary>
        /// Die Sperre einer vorhandenen Zeile: NichtGefunden, ReadOnlyGesperrt (Vorrang), BenutztGesperrt oder frei.
        /// Eine ausgelieferte Zapfkategorie (Schemaschritt T2, <c>ReadOnly</c>) sperrt ihre Nutzungsart wie
        /// deren eigene Spalte — dieselbe Regel wie <c>KatalogBereinigung.Sperrgrund</c>.
        /// </summary>
        private static TwwKatalogAusgang Sperre(DbVorgang v, int id)
        {
            object ro = v.Skalar("SELECT ReadOnly FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                                 new DbParam("@id", id));
            if (ro == null) return TwwKatalogAusgang.NichtGefunden;
            if (Convert.ToInt64(ro, CultureInfo.InvariantCulture) != 0) return TwwKatalogAusgang.ReadOnlyGesperrt;
            if (KategorientabelleDa(v)
                && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM +
                             " WHERE ID_Nutzungsart = ? AND ReadOnly <> 0", id) > 0)
                return TwwKatalogAusgang.ReadOnlyGesperrt;

            if (Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Nutzungsart = ?", id) > 0)
                return TwwKatalogAusgang.BenutztGesperrt;

            return TwwKatalogAusgang.Ausgefuehrt;
        }

        /// <summary>Führt die Datenbank die Zapfkategorien (Schemaschritt T2)? Im laufenden Vorgang gefragt.</summary>
        private static bool KategorientabelleDa(DbVorgang v)
        {
            object n = v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                                new DbParam("@tabelle", TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM));
            return n != null && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// Die Zapfkategorien der Nutzungsart <paramref name="von"/> als Kategorien der neuen Zeile
        /// <paramref name="nach"/> (Kopierstellen „Speichern unter" und neue Version aus
        /// <see cref="TagesgangSpeichern"/>): Werte, Reihenfolge, Provenienz und interner Beleg wie
        /// im Original — die Werte sind unverändert —, Status wie die Kopie (<c>EIGEN</c>),
        /// <c>ReadOnly = 0</c>. Ohne Tabelle (Stand vor Schritt 115) nichts.
        /// </summary>
        private static void KategorienKopieren(DbVorgang v, int von, int nach)
        {
            if (!KategorientabelleDa(v)) return;
            v.Ausfuehren(
                "INSERT INTO " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " (ID_Nutzungsart, Kategorie, Reihenfolge, " +
                "Volumenstrom_l_min, Dauer_min, Anteil, Sigma, Kappung_l_min, Quelle, Ausgabe, Version, Herkunftsart, " +
                "Status, Beleg, ReadOnly) SELECT ?, Kategorie, Reihenfolge, Volumenstrom_l_min, Dauer_min, Anteil, Sigma, " +
                "Kappung_l_min, Quelle, Ausgabe, Version, Herkunftsart, ?, Beleg, 0 FROM " +
                TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE ID_Nutzungsart = ? ORDER BY Reihenfolge, ID",
                new DbParam("@nach", nach), new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)),
                new DbParam("@von", von));
        }

        /// <summary>Die vier Tagesgänge eines Satzes samt Provenienz je Tagtyp — fehlt einer, bleibt sein Feld <c>null</c>.</summary>
        private static (double[][] Werte, Provenienz[] Herkunft) TagesgangZeilenLesen(DbVorgang v, int idSatz)
        {
            var werte = new double[Tagesgangsatz.TAGTYPEN][];
            var herkunft = new Provenienz[Tagesgangsatz.TAGTYPEN];
            DataTable gaenge = v.Lese("SELECT * FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                                      " WHERE ID_Tagesgangsatz = ? ORDER BY Tagtyp", new DbParam("@id", idSatz));
            foreach (DataRow r in gaenge.Rows)
            {
                int t = ZapfprofilCtrl.Ganz(r, "Tagtyp") - 1;
                if (t < 0 || t >= Tagesgangsatz.TAGTYPEN) continue;
                werte[t] = new double[Tagesgangsatz.STUNDEN];
                for (int h = 0; h < Tagesgangsatz.STUNDEN; h++)
                    werte[t][h] = ZapfprofilCtrl.Zahl(r, ZapfprofilCtrl.AnteilSpalte(h + 1));
                herkunft[t] = ZapfprofilCtrl.Herkunft(r, "");
            }
            return (werte, herkunft);
        }

        private static long Anzahl(DbVorgang v, string sql, params int[] werte)
        {
            var p = new DbParam[werte.Length];
            for (int i = 0; i < werte.Length; i++) p[i] = new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), werte[i]);
            object n = v.Skalar(sql, p);
            return n == null ? 0 : Convert.ToInt64(n, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die gespeicherte Zeile als Entwurf — im laufenden Vorgang gelesen, damit der
        /// Vergleich der Wertgruppen denselben Stand sieht wie das Schreiben. <c>null</c>, wenn
        /// es die Zeile nicht gibt.
        /// </summary>
        private static TwwNutzungsartEntwurf Bezugszeile(DbVorgang v, int id)
        {
            DataTable dt = v.Lese("SELECT * FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                                  new DbParam("@id", id));
            if (dt == null || dt.Rows.Count == 0) return null;
            Nutzungsart n = ZapfprofilCtrl.NutzungsartenAus(dt, new Dictionary<int, Tagesgangsatz>())[0];
            return TwwNutzungsartEntwurf.Aus(n) with { IdTagesgangsatz = ZapfprofilCtrl.Ganz(dt.Rows[0], "ID_Tagesgangsatz") };
        }

        /// <summary>
        /// <b>Die Provenienz je Wertgruppe gegen den Bezug nachführen</b> (Konzept 3.1:
        /// <c>G_Version</c> ist die Katalogversion, in der die Gruppe zuletzt gesetzt wurde).
        /// Die Gruppen: <b>Bedarf</b> — Bezugsart, Bedarf je Niveau samt Bandbreite,
        /// Bezugstemperaturen und Bilanzgrenze (alles, was bestimmt, was die Bedarfszahl
        /// bedeutet); <b>Jahresgang</b> — Kalenderart, Ferienfaktor, Monatsfaktoren;
        /// <b>Wochengang</b> — Wochenfaktoren. <paramref name="irgendeineGeaendert"/> sagt, ob
        /// überhaupt eine Gruppe vom Bezug abweicht.
        /// </summary>
        private static TwwNutzungsartEntwurf ProvenienzNachfuehren(TwwNutzungsartEntwurf e, TwwNutzungsartEntwurf bezug,
                                                                   out bool irgendeineGeaendert)
        {
            string version = e.Katalogversion.Trim();

            bool bedarf = e.Bezug != bezug.Bezug
                          || !Gleich(e.Bedarf, bezug.Bedarf)
                          || !Gleich(e.BedarfMin, bezug.BedarfMin)
                          || !Gleich(e.BedarfMax, bezug.BedarfMax)
                          || !Equals(e.Bezugstemperaturen, bezug.Bezugstemperaturen)
                          || e.Grenze != bezug.Grenze;
            bool jahresgang = e.Kalender != bezug.Kalender
                              || !Nullable.Equals(e.Ferienfaktor, bezug.Ferienfaktor)
                              || !Gleich(e.Monatsfaktoren, bezug.Monatsfaktoren);
            bool wochengang = !Gleich(e.Wochenfaktoren, bezug.Wochenfaktoren);

            irgendeineGeaendert = bedarf || jahresgang || wochengang;
            return e with
            {
                BedarfHerkunft = Nachgefuehrt(e.BedarfHerkunft, bezug.BedarfHerkunft, bedarf, version),
                JahresgangHerkunft = Nachgefuehrt(e.JahresgangHerkunft, bezug.JahresgangHerkunft, jahresgang, version),
                WochengangHerkunft = Nachgefuehrt(e.WochengangHerkunft, bezug.WochengangHerkunft, wochengang, version)
            };
        }

        /// <summary>
        /// Die Provenienz einer Wertgruppe nach dem Schreiben: unverändert, wenn die Werte der
        /// Gruppe gleich geblieben sind. Sind sie geändert, trägt die Gruppe die neue
        /// Katalogversion; bringt der Entwurf dabei noch die Provenienz des Bezugs mit (Quelle,
        /// Ausgabe, Herkunftsart gleich), ist sie <c>EIGENKONSTRUKTION</c> mit neutraler Quelle —
        /// ein vom Anwender geänderter Wert trägt nie die Norm oder das Verfahren der Vorlage
        /// als Herkunft. Eine im Entwurf ausdrücklich gesetzte andere Provenienz bleibt.
        /// </summary>
        private static Provenienz Nachgefuehrt(Provenienz entwurf, Provenienz bezug, bool geaendert, string version)
        {
            if (!geaendert) return entwurf;
            bool unberuehrt = entwurf.Art == bezug.Art
                              && string.Equals(entwurf.Quelle, bezug.Quelle, StringComparison.Ordinal)
                              && string.Equals(entwurf.Ausgabe, bezug.Ausgabe, StringComparison.Ordinal);
            return unberuehrt ? Eigenkonstruktion(version) : entwurf with { Version = version };
        }

        /// <summary>Die Provenienz einer vom Anwender gesetzten Gruppe: neutrale Quelle, keine Ausgabe, EIGENKONSTRUKTION.</summary>
        private static Provenienz Eigenkonstruktion(string version)
            => new Provenienz(QUELLE_EIGENKONSTRUKTION, null, version, Herkunftsart.Eigenkonstruktion);

        private static void TagesgangEinfuegen(DbVorgang v, int idSatz, int tagtyp, double[] anteile, Provenienz h)
        {
            var p = new List<DbParam>(Tagesgangsatz.STUNDEN + 6)
            {
                new DbParam("@satz", idSatz),
                new DbParam("@tagtyp", tagtyp)
            };
            for (int s = 0; s < Tagesgangsatz.STUNDEN; s++)
                p.Add(new DbParam("@a" + s.ToString(CultureInfo.InvariantCulture), anteile[s]));
            p.Add(new DbParam("@quelle", h.Quelle));
            p.Add(new DbParam("@ausgabe", h.Ausgabe));
            p.Add(new DbParam("@version", h.Version));
            p.Add(new DbParam("@art", TwwWertemengen.Text(h.Art)));
            v.Ausfuehren(SQL_INSERT_TAGESGANG, p.ToArray());
        }

        /// <summary>Die Toleranz von <see cref="Gleich(double[], double[])"/> je Wert — ein Runden über Prozent
        /// und zurück darf eine unveränderte Reihe nicht als geändert erscheinen lassen (Z4, Gruppe 2b Punkt 1).</summary>
        private const double GLEICH_TOLERANZ = 1e-12;

        private static bool Gleich(double[] a, double[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (Math.Abs(a[i] - b[i]) > GLEICH_TOLERANZ) return false;
            return true;
        }

        private static bool Gleich(double?[] a, double?[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (!Nullable.Equals(a[i], b[i])) return false;
            return true;
        }

        private static bool TagesgangsatzVorhanden(DbVorgang v, int id)
            => v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " WHERE ID = ?",
                        new DbParam("@id", id)) != null;

        /// <summary>Ist (Bezeichner, Katalogversion) an eine ANDERE Zeile als <paramref name="ausser"/> vergeben?</summary>
        private static bool NameVergeben(DbVorgang v, string bezeichner, string version, int? ausser)
        {
            object id = v.Skalar(
                "SELECT ID FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", bezeichner.Trim()), new DbParam("@k", version.Trim()));
            return id != null && (!ausser.HasValue || Convert.ToInt32(id, CultureInfo.InvariantCulture) != ausser.Value);
        }

        /// <summary>Pflichtangaben und Rasterlängen — was die DDL als NOT NULL führt, fehlt nicht.</summary>
        private static bool Vollstaendig(TwwNutzungsartEntwurf e)
        {
            if (e == null) return false;
            if (string.IsNullOrWhiteSpace(e.Bezeichner) || string.IsNullOrWhiteSpace(e.Katalogversion)) return false;
            if (!Enum.IsDefined(typeof(ZapfBezugsart), e.Bezug)
                || !Enum.IsDefined(typeof(ZapfBilanzgrenze), e.Grenze)
                || !Enum.IsDefined(typeof(ZapfKalenderart), e.Kalender)) return false;
            if (e.Bedarf == null || e.Bedarf.Length != NutzungsartRaster.NIVEAUS) return false;
            if (e.BedarfMin == null || e.BedarfMin.Length != NutzungsartRaster.NIVEAUS) return false;
            if (e.BedarfMax == null || e.BedarfMax.Length != NutzungsartRaster.NIVEAUS) return false;
            if (e.Monatsfaktoren == null || e.Monatsfaktoren.Length != NutzungsartRaster.MONATE) return false;
            if (e.Wochenfaktoren == null || e.Wochenfaktoren.Length != NutzungsartRaster.WOCHENTAGE) return false;
            if (e.Bezugstemperaturen == null) return false;
            return Belegt(e.BedarfHerkunft) && Belegt(e.JahresgangHerkunft) && Belegt(e.WochengangHerkunft);
        }

        /// <summary>
        /// Die Rasterregeln aus Konzept 3.1: Bedarf und Faktoren endlich und nicht negativ,
        /// Monatsfaktoren im Mittel 1, Wochenfaktoren Σ 1 (Toleranz <see cref="TOLERANZ"/>).
        /// </summary>
        private static bool RasterGueltig(TwwNutzungsartEntwurf e)
        {
            foreach (double b in e.Bedarf) if (!NichtNegativ(b)) return false;
            if (!SummeEins(e.Wochenfaktoren, NutzungsartRaster.WOCHENTAGE)) return false;
            double summe = 0.0;
            foreach (double m in e.Monatsfaktoren)
            {
                if (!NichtNegativ(m)) return false;
                summe += m;
            }
            return Math.Abs(summe / NutzungsartRaster.MONATE - 1.0) <= TOLERANZ;
        }

        /// <summary>Vier Tagesgänge zu je 24 Anteilen, jeder endlich, nicht negativ und Σ 1.</summary>
        private static bool TagesgaengeGueltig(IReadOnlyList<double[]> tagesgaenge)
        {
            if (tagesgaenge == null || tagesgaenge.Count != Tagesgangsatz.TAGTYPEN) return false;
            foreach (double[] t in tagesgaenge)
                if (!SummeEins(t, Tagesgangsatz.STUNDEN)) return false;
            return true;
        }

        private static bool SummeEins(double[] werte, int laenge)
        {
            if (werte == null || werte.Length != laenge) return false;
            double summe = 0.0;
            foreach (double w in werte)
            {
                if (!NichtNegativ(w)) return false;
                summe += w;
            }
            return Math.Abs(summe - 1.0) <= TOLERANZ;
        }

        private static bool NichtNegativ(double w) => !double.IsNaN(w) && !double.IsInfinity(w) && w >= 0.0;

        private static bool Belegt(Provenienz p)
            => p != null && !string.IsNullOrWhiteSpace(p.Quelle) && !string.IsNullOrWhiteSpace(p.Version)
               && Enum.IsDefined(typeof(Herkunftsart), p.Art);

        /// <summary>Die 49 Fachwerte in der Spaltenreihenfolge von <see cref="SQL_INSERT"/> und <see cref="SQL_UPDATE"/>.</summary>
        private static List<DbParam> Fachwerte(TwwNutzungsartEntwurf e)
        {
            var p = new List<DbParam>(52)
            {
                new DbParam("@bezeichner", e.Bezeichner.Trim()),
                new DbParam("@version", e.Katalogversion.Trim()),
                new DbParam("@bezugsart", (int)e.Bezug)
            };
            for (int n = 0; n < NutzungsartRaster.NIVEAUS; n++)
                p.Add(new DbParam("@bedarf" + n, e.Bedarf[n]));
            for (int n = 0; n < NutzungsartRaster.NIVEAUS; n++)
            {
                p.Add(new DbParam("@min" + n, e.BedarfMin[n]));
                p.Add(new DbParam("@max" + n, e.BedarfMax[n]));
            }
            Provenienzwerte(p, "bedarf", e.BedarfHerkunft);
            p.Add(new DbParam("@zapf", e.Bezugstemperaturen.ZapftemperaturC));
            p.Add(new DbParam("@kalt", e.Bezugstemperaturen.KaltwasserC));
            p.Add(new DbParam("@grenze", (int)e.Grenze));
            p.Add(new DbParam("@kalender", (int)e.Kalender));
            p.Add(new DbParam("@ferien", e.Ferienfaktor));
            for (int m = 0; m < NutzungsartRaster.MONATE; m++)
                p.Add(new DbParam("@monat" + m, e.Monatsfaktoren[m]));
            Provenienzwerte(p, "jahr", e.JahresgangHerkunft);
            for (int w = 0; w < NutzungsartRaster.WOCHENTAGE; w++)
                p.Add(new DbParam("@woche" + w, e.Wochenfaktoren[w]));
            Provenienzwerte(p, "woche", e.WochengangHerkunft);
            p.Add(new DbParam("@satz", e.IdTagesgangsatz));
            return p;
        }

        private static void Provenienzwerte(List<DbParam> p, string gruppe, Provenienz h)
        {
            p.Add(new DbParam("@" + gruppe + "_quelle", h.Quelle));
            p.Add(new DbParam("@" + gruppe + "_ausgabe", h.Ausgabe));
            p.Add(new DbParam("@" + gruppe + "_version", h.Version));
            p.Add(new DbParam("@" + gruppe + "_art", TwwWertemengen.Text(h.Art)));
        }

        private static bool TabellenVorhanden()
            => DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)
               && DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM)
               && DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZONE);
    }
}
