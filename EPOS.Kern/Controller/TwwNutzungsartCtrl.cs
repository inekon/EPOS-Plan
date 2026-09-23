using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

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
        Fehlgeschlagen = 8
    }

    /// <summary>Ergebnis einer Pflegeaktion: Ausgang und die ID der betroffenen (bzw. neuen) Zeile.</summary>
    internal sealed record TwwKatalogErgebnis(TwwKatalogAusgang Ausgang, int Id)
    {
        /// <summary>Ist die Aktion geschrieben?</summary>
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
    /// auf den Vorgänger, Provenienz und interner Beleg übernommen. Eine freie Zeile (eigen,
    /// unbenutzt) lässt sich an Ort und Stelle ändern; ihr Vier-Augen-Vermerk entfällt dabei,
    /// weil er für die alten Werte galt.</para>
    ///
    /// <para><b>Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern</b>,
    /// Prüfung und Schreiben je Aktion in EINEM <see cref="DbVorgang"/>.</para>
    /// </summary>
    internal static class TwwNutzungsartCtrl
    {
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
            "ID_Tagesgangsatz = ?, Freigabe = NULL WHERE ID = ?";

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

        /// <summary>Gehört die Zeile zur Auslieferung (<c>ReadOnly</c>)? Eine fehlende Zeile ist es nicht.</summary>
        internal static bool IstReadOnly(int id)
        {
            if (!TabellenVorhanden()) return false;
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                new DbParam("@id", id));
            return v != null && v != DBNull.Value && Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0;
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

        /// <summary>„Neu": eine eigene Zeile (Status <c>EIGEN</c>, ohne Vorlage, ohne Beleg).</summary>
        internal static TwwKatalogErgebnis Neu(TwwNutzungsartEntwurf e) => Anlegen(e, null);

        /// <summary>
        /// „Speichern unter": die Werte des Entwurfs als NEUE Zeile — Status <c>EIGEN</c>,
        /// <c>ReadOnly = 0</c>, <c>ID_Vorlage</c> auf <paramref name="idVorlage"/>, die Provenienz
        /// je Wertgruppe aus dem Entwurf (mit <see cref="TwwNutzungsartEntwurf.Aus"/> die der
        /// Vorlage) und der interne Beleg der Vorlage. Der einzige Weg, eine gesperrte Zeile zu
        /// „ändern"; die Vorlage bleibt unberührt. (Bezeichner, Katalogversion) muss frei sein.
        /// </summary>
        internal static TwwKatalogErgebnis SpeichernUnter(int idVorlage, TwwNutzungsartEntwurf e) => Anlegen(e, idVorlage);

        /// <summary>
        /// „Ändern" an Ort und Stelle — nur für eine Zeile, die weder zur Auslieferung gehört
        /// noch von einer Zone benutzt wird; sonst benannt <see cref="TwwKatalogAusgang.ReadOnlyGesperrt"/>
        /// bzw. <see cref="TwwKatalogAusgang.BenutztGesperrt"/>, und der Aufrufer bietet
        /// <see cref="SpeichernUnter"/>. Status und Vorlage bleiben, der Freigabevermerk entfällt.
        /// </summary>
        internal static TwwKatalogErgebnis Aendern(int id, TwwNutzungsartEntwurf e)
        {
            if (!TabellenVorhanden()) return new TwwKatalogErgebnis(TwwKatalogAusgang.TabellenFehlen, id);
            if (!Vollstaendig(e)) return new TwwKatalogErgebnis(TwwKatalogAusgang.EntwurfUnvollstaendig, id);

            return Ausfuehren(id, v =>
            {
                TwwKatalogAusgang sperre = Sperre(v, id);
                if (sperre != TwwKatalogAusgang.Ausgefuehrt) return sperre;
                if (!TagesgangsatzVorhanden(v, e.IdTagesgangsatz)) return TwwKatalogAusgang.TagesgangsatzFehlt;
                if (NameVergeben(v, e.Bezeichner, e.Katalogversion, id)) return TwwKatalogAusgang.NameBelegt;

                var p = Fachwerte(e);
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

        private static TwwKatalogErgebnis Anlegen(TwwNutzungsartEntwurf e, int? idVorlage)
        {
            if (!TabellenVorhanden()) return new TwwKatalogErgebnis(TwwKatalogAusgang.TabellenFehlen, 0);
            if (!Vollstaendig(e)) return new TwwKatalogErgebnis(TwwKatalogAusgang.EntwurfUnvollstaendig, 0);

            int neu = 0;
            TwwKatalogErgebnis erg = Ausfuehren(0, v =>
            {
                if (idVorlage.HasValue && !ZeileVorhanden(v, idVorlage.Value)) return TwwKatalogAusgang.NichtGefunden;
                if (!TagesgangsatzVorhanden(v, e.IdTagesgangsatz)) return TwwKatalogAusgang.TagesgangsatzFehlt;
                if (NameVergeben(v, e.Bezeichner, e.Katalogversion, null)) return TwwKatalogAusgang.NameBelegt;

                var p = Fachwerte(e);
                p.Add(new DbParam("@vorlage", idVorlage.HasValue ? (object)idVorlage.Value : null));
                p.Add(new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)));
                p.Add(new DbParam("@beleg", idVorlage.HasValue ? (object)idVorlage.Value : null));
                neu = v.EinfuegenUndId(SQL_INSERT, p.ToArray());
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

        /// <summary>Die Sperre einer vorhandenen Zeile: NichtGefunden, ReadOnlyGesperrt, BenutztGesperrt oder frei.</summary>
        private static TwwKatalogAusgang Sperre(DbVorgang v, int id)
        {
            object ro = v.Skalar("SELECT ReadOnly FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                                 new DbParam("@id", id));
            if (ro == null) return TwwKatalogAusgang.NichtGefunden;
            if (Convert.ToInt64(ro, CultureInfo.InvariantCulture) != 0) return TwwKatalogAusgang.ReadOnlyGesperrt;

            object n = v.Skalar("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Nutzungsart = ?",
                                new DbParam("@id", id));
            if (n != null && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0) return TwwKatalogAusgang.BenutztGesperrt;

            return TwwKatalogAusgang.Ausgefuehrt;
        }

        private static bool ZeileVorhanden(DbVorgang v, int id)
            => v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                        new DbParam("@id", id)) != null;

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
