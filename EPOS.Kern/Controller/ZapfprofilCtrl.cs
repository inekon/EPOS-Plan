using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Datenseite des Zapfprofilgenerators</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.3, Stufe Z0: lesend).
    ///
    /// <para><b>Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern.</b>
    /// Tabellen- und Spaltennamen stammen aus <see cref="TwwSchema"/> bzw. aus Schleifen über
    /// feste Zahlen, nie aus einer Eingabe.</para>
    /// </summary>
    internal static partial class ZapfprofilCtrl
    {
        // =================================================================================
        // Parameter (Posten P5)
        // =================================================================================

        /// <summary>
        /// Die aktuelle Katalogversion der Parameter: die Version der zuletzt angelegten
        /// Parameterzeile (höchste <c>ID</c>). Die IDs sind <c>AUTOINCREMENT</c> und werden nie
        /// wieder vergeben (<see cref="TwwSchema"/>); eine neue Auslieferungsversion kommt als
        /// neue Zeilen (Konzept 3.2) und trägt deshalb die höchsten IDs. Die Textform der
        /// Version wird bewusst nicht geordnet — „V10" gegen „V9" hätte keine sichere Regel.
        /// <c>null</c>, wenn die Tabelle fehlt oder leer ist.
        /// </summary>
        internal static string AktuelleKatalogversion()
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PARAMETER_STAMM)) return null;

            object v = DataRepository.ExecuteScalar(
                "SELECT Katalogversion FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " " +
                "ORDER BY ID DESC LIMIT 1");
            return v == null || v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die gekapselten Parameter der aktuellen Katalogversion
        /// (<see cref="AktuelleKatalogversion"/>). Gibt es keine, die benannte Ablehnung
        /// <see cref="ParametersatzFehler.KeineKatalogversion"/> — kein Rückfallwert.
        /// </summary>
        internal static Parametersatz Parameter()
        {
            string version = AktuelleKatalogversion();
            if (version == null)
                throw new ParametersatzException(ParametersatzFehler.KeineKatalogversion, "", "",
                    ZapfSatz.Neu("PARAMETER_TABELLE_OHNE_VERSION", ZapfSatz.Tabelle(TwwSchema.TAB_TWW_PARAMETER_STAMM)));
            return Parameter(version);
        }

        /// <summary>
        /// Die Parameter einer bestimmten Katalogversion. Trägt die Version keine Zeile, die
        /// benannte Ablehnung <see cref="ParametersatzFehler.KatalogversionFehlt"/>.
        /// </summary>
        internal static Parametersatz Parameter(string katalogversion)
        {
            if (string.IsNullOrEmpty(katalogversion))
                throw new ParametersatzException(ParametersatzFehler.KeineKatalogversion, "", "",
                    ZapfSatz.Neu("PARAMETER_KEINE_VERSION_GENANNT"));
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PARAMETER_STAMM))
                throw new ParametersatzException(ParametersatzFehler.KeineKatalogversion, katalogversion, "",
                    ZapfSatz.Neu("PARAMETER_TABELLE_FEHLT", ZapfSatz.Tabelle(TwwSchema.TAB_TWW_PARAMETER_STAMM)));

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Schluessel, Wert, Einheit, Quelle, Ausgabe, Version, Herkunftsart " +
                "FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " WHERE Katalogversion = ? ORDER BY Schluessel",
                new DbParam("@version", katalogversion));

            var zeilen = new List<ZapfParameterwert>();
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    zeilen.Add(new ZapfParameterwert(
                        Text(r, "Schluessel"),
                        Zahl(r, "Wert"),
                        TextOderNull(r, "Einheit"),
                        Herkunft(r, "")));

            // Leere Menge -> KatalogversionFehlt (Parametersatz.Aus).
            return Parametersatz.Aus(katalogversion, zeilen);
        }

        // =================================================================================
        // Weiche und Verfügbarkeit (Posten P6)
        // =================================================================================

        /// <summary>
        /// Die Weiche eines Projekts (Konzept 2.2): <see cref="BrauchwasserWeg.Generator"/> nur,
        /// wenn <c>Tab_TwwProjekt</c> für das Projekt eine Zeile mit <c>Weg = 'GENERATOR'</c>
        /// trägt; ohne Tabelle, ohne Zeile oder mit <c>BESTAND</c> der Bestandsweg.
        /// </summary>
        internal static BrauchwasserWeg Weg(int idProjekt)
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PROJEKT)) return BrauchwasserWeg.Bestand;

            object v = DataRepository.ExecuteScalar(
                "SELECT Weg FROM " + TwwSchema.TAB_TWW_PROJEKT + " WHERE ID_Projekt = ?",
                new DbParam("@projekt", idProjekt));
            return WegAus(v == null || v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Kann der Generator in dieser Datenbank laufen? Benannt: alle zehn Tabellen aus
        /// <see cref="TwwSchema"/> vorhanden (ein älterer iOS-Seed trägt sie nicht, 3.2) und
        /// eine Katalogversion der Parameter vorhanden (<see cref="AktuelleKatalogversion"/>).
        /// </summary>
        internal static ZapfVerfuegbarkeit Verfuegbar()
        {
            var fehlend = new List<string>();
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) fehlend.Add(a.Key);
            if (fehlend.Count > 0)
                return new ZapfVerfuegbarkeit(false, ZapfVerfuegbarkeitsgrund.TabellenFehlen,
                    ZapfSatz.Neu("VERFUEGBAR_TABELLEN_FEHLEN", (object)fehlend.Select(ZapfSatz.Tabelle).ToArray()));

            string version = AktuelleKatalogversion();
            if (version == null)
                return new ZapfVerfuegbarkeit(false, ZapfVerfuegbarkeitsgrund.KeineKatalogversion,
                    ZapfSatz.Neu("VERFUEGBAR_KEINE_KATALOGVERSION", ZapfSatz.Tabelle(TwwSchema.TAB_TWW_PARAMETER_STAMM)));

            return new ZapfVerfuegbarkeit(true, ZapfVerfuegbarkeitsgrund.Verfuegbar,
                ZapfSatz.Neu("VERFUEGBAR_JA", version));
        }

        // =================================================================================
        // Katalog (Posten P6) — eine Abfrage je Tabelle
        // =================================================================================

        /// <summary>
        /// Alle Nutzungsarten des Katalogs samt Tagesgangsatz, Provenienz und Status, geordnet
        /// nach Bezeichner und Katalogversion. Ohne Tabellen eine leere Liste.
        /// </summary>
        internal static IReadOnlyList<Nutzungsart> Katalog()
        {
            if (!KatalogtabellenVorhanden()) return new Nutzungsart[0];

            Dictionary<int, Tagesgangsatz> saetze = SaetzeNachId(null);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " ORDER BY Bezeichner, Katalogversion, ID");
            return NutzungsartenAus(dt, saetze);
        }

        /// <summary>Eine Nutzungsart samt Tagesgangsatz; <c>null</c>, wenn es sie (oder die Tabellen) nicht gibt.</summary>
        internal static Nutzungsart LiesNutzungsart(int id)
        {
            if (!KatalogtabellenVorhanden()) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                new DbParam("@id", id));
            if (dt == null || dt.Rows.Count == 0) return null;

            Dictionary<int, Tagesgangsatz> saetze = SaetzeNachId(Ganz(dt.Rows[0], "ID_Tagesgangsatz"));
            IReadOnlyList<Nutzungsart> eine = NutzungsartenAus(dt, saetze);
            return eine.Count == 1 ? eine[0] : null;
        }

        /// <summary>Alle Tagesgangsätze des Katalogs, geordnet nach Bezeichner und Katalogversion.</summary>
        internal static IReadOnlyList<Tagesgangsatz> Tagesgangsaetze()
        {
            if (!KatalogtabellenVorhanden()) return new Tagesgangsatz[0];

            var liste = new List<Tagesgangsatz>(SaetzeNachId(null).Values);
            liste.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.Bezeichner, b.Bezeichner);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.Katalogversion, b.Katalogversion);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            return liste;
        }

        // =================================================================================
        // Projektdaten (Posten P6)
        // =================================================================================

        /// <summary>
        /// Der Arbeitsstand eines Projekts: Weg, Zonen in ihrer Reihenfolge samt
        /// Wohnungstabelle, die Projektzeile (<c>null</c>, wenn es keine gibt) und die Zeilen des
        /// Bedarfstag-Konstruktors am Auslegungssatz (Schemaschritt T5, ZU25). Ohne
        /// Tabellen der Bestandsweg ohne Zonen.
        /// </summary>
        internal static ZapfprofilStand Lies(int idProjekt)
        {
            BrauchwasserWeg weg = Weg(idProjekt);
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZONE)
                || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_WOHNUNGSTYP)
                || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PROJEKT))
                return new ZapfprofilStand(weg, new ZonenStand[0], null);

            // Die Wohnungstabellen aller Zonen des Projekts in EINER Abfrage.
            var wohnungen = new Dictionary<int, List<WohnungstypStand>>();
            DataTable dw = DataRepository.GetDataTable(
                "SELECT w.* FROM " + TwwSchema.TAB_TWW_WOHNUNGSTYP + " AS w INNER JOIN " + TwwSchema.TAB_TWW_ZONE +
                " AS z ON z.ID = w.ID_Zone WHERE z.ID_Projekt = ? ORDER BY w.ID_Zone, w.Reihenfolge, w.ID",
                new DbParam("@projekt", idProjekt));
            if (dw != null)
                foreach (DataRow r in dw.Rows)
                {
                    int zone = Ganz(r, "ID_Zone");
                    if (!wohnungen.TryGetValue(zone, out List<WohnungstypStand> l))
                        wohnungen[zone] = l = new List<WohnungstypStand>();
                    l.Add(new WohnungstypStand
                    {
                        Id = Ganz(r, "ID"),
                        Anzahl = Ganz(r, "Anzahl"),
                        Raumzahl = ZahlOderNull(r, "Raumzahl"),
                        Personen = ZahlOderNull(r, "Personen"),
                        IdAusstattung = GanzOderNull(r, "ID_Ausstattung"),
                        Reihenfolge = Ganz(r, "Reihenfolge")
                    });
                }

            var zonen = new List<ZonenStand>();
            DataTable dz = DataRepository.GetDataTable(
                "SELECT * FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Projekt = ? ORDER BY Reihenfolge, ID",
                new DbParam("@projekt", idProjekt));
            if (dz != null)
                foreach (DataRow r in dz.Rows)
                    zonen.Add(ZoneAus(r, wohnungen));

            ProjektStand projekt = null;
            DataTable dp = DataRepository.GetDataTable(
                "SELECT * FROM " + TwwSchema.TAB_TWW_PROJEKT + " WHERE ID_Projekt = ?",
                new DbParam("@projekt", idProjekt));
            if (dp != null && dp.Rows.Count > 0) projekt = ProjektAus(dp.Rows[0]);

            // Die Zeilen des Konstruktors überdauern das Speichern (Schritt 145, ZU25): Ein erneut
            // geöffneter Konstruktor beginnt mit ihnen, auch in einer neuen Sitzung.
            // Mit ihnen der Bezug des gespeicherten Tags (Folge (a) aus N21): Er steht an dessen
            // Katalogzeile, nicht am Auslegungssatz — ein erneutes OK soll denselben Tag bauen.
            return new ZapfprofilStand(weg, zonen, projekt)
            {
                Konstruktorzeilen = Konstruktorzeilen(projekt?.Id ?? 0),
                KonstruktorBezug = DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM)
                    ? KonstruktorBezug(projekt, DataRepository.GetDataTable) : null
            };
        }

        /// <summary>
        /// <b>Der Bezug eines gespeicherten Konstruktortags</b> (Folge (a) aus N21): Bezugsmenge und
        /// — ab Schritt 124 — Bezugsart der Katalogzeile, auf die <paramref name="projekt"/> mit der
        /// Quelle Konstruktor zeigt. <c>null</c>, wenn die Projektzeile keinen gespeicherten
        /// Konstruktortag nennt; steht die Katalogzeile nicht mehr, ein Stand mit
        /// <see cref="KonstruktorBezugStand.TagGefunden"/> <c>false</c> (nie still „ohne Bezug").
        /// <paramref name="lese"/> ist der Leseweg — allein oder im Vorgang des Schreibwegs; die
        /// Katalogtabelle muss stehen (der Aufrufer prüft es).
        /// </summary>
        internal static KonstruktorBezugStand KonstruktorBezug(ProjektStand projekt, Func<string, DbParam[], DataTable> lese)
        {
            if (projekt?.BedarfstagQuelle != ZapfBedarfstagquelle.Konstruktor || projekt.IdBedarfstag is not int id || id <= 0)
                return null;
            DataTable d = lese("SELECT * FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " WHERE ID = ?",
                               new[] { new DbParam("@id", id) });
            if (d == null || d.Rows.Count == 0) return new KonstruktorBezugStand(false, null, null);
            DataRow r = d.Rows[0];
            int? art = SpalteDa(r, TwwSchema.SPALTE_BEZUGSART) ? GanzOderNull(r, TwwSchema.SPALTE_BEZUGSART) : null;
            return new KonstruktorBezugStand(true, ZahlOderNull(r, "Bezugsmenge"),
                art.HasValue && Enum.IsDefined(typeof(ZapfBezugsart), art.Value) ? (ZapfBezugsart)art.Value : (ZapfBezugsart?)null);
        }

        /// <summary>
        /// <b>Die Zeilen des Bedarfstag-Konstruktors</b> eines Auslegungssatzes
        /// (<c>Tab_TwwKonstruktorzeile</c>, Schemaschritt T5, Anwenderentscheid ZU25) in ihrer
        /// Reihenfolge. Ohne Satz, ohne Tabelle oder ohne Zeile eine leere Liste — der
        /// Konstruktor beginnt dann mit einer Zeile wie beim ersten Mal.
        /// </summary>
        internal static IReadOnlyList<KonstruktorzeileStand> Konstruktorzeilen(int idAuslegung)
        {
            if (idAuslegung <= 0 || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_KONSTRUKTORZEILE))
                return new KonstruktorzeileStand[0];
            DataTable d = DataRepository.GetDataTable(
                "SELECT * FROM " + TwwSchema.TAB_TWW_KONSTRUKTORZEILE +
                " WHERE ID_TwwProjekt = ? ORDER BY Reihenfolge, ID",
                new DbParam("@auslegung", idAuslegung));
            if (d == null || d.Rows.Count == 0) return new KonstruktorzeileStand[0];
            var liste = new List<KonstruktorzeileStand>(d.Rows.Count);
            foreach (DataRow r in d.Rows)
                liste.Add(new KonstruktorzeileStand(ZahlOderNull(r, "Beginn_h"), ZahlOderNull(r, "Ende_h"),
                                                    Text(r, "Regel"), ZahlOderNull(r, "Anzahl"),
                                                    ZahlOderNull(r, "Volumen_l"), ZahlOderNull(r, "Zapftemperatur_C"),
                                                    Text(r, "Verbraucher")));
            return liste.AsReadOnly();
        }

        // =================================================================================
        // Abbildung Zeile -> Kerntyp
        // =================================================================================

        private static bool KatalogtabellenVorhanden()
            => DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)
               && DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM)
               && DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANG_STAMM);

        private static BrauchwasserWeg WegAus(string text)
            => string.Equals(text, TwwSchema.WEG_GENERATOR, StringComparison.Ordinal)
                ? BrauchwasserWeg.Generator : BrauchwasserWeg.Bestand;

        /// <summary>Tagesgangsätze je ID — alle, oder nur der eine mit <paramref name="nurId"/>.</summary>
        private static Dictionary<int, Tagesgangsatz> SaetzeNachId(int? nurId)
        {
            DataTable koepfe = nurId.HasValue
                ? DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Katalogversion, Status, ReadOnly FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                    " WHERE ID = ?", new DbParam("@id", nurId.Value))
                : DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Katalogversion, Status, ReadOnly FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM);
            DataTable gaenge = nurId.HasValue
                ? DataRepository.GetDataTable(
                    "SELECT * FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " WHERE ID_Tagesgangsatz = ? ORDER BY Tagtyp",
                    new DbParam("@id", nurId.Value))
                : DataRepository.GetDataTable(
                    "SELECT * FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " ORDER BY ID_Tagesgangsatz, Tagtyp");

            var anteile = new Dictionary<int, double[,]>();
            var herkunft = new Dictionary<int, Provenienz[]>();
            if (gaenge != null)
                foreach (DataRow r in gaenge.Rows)
                {
                    int satz = Ganz(r, "ID_Tagesgangsatz");
                    int t = Ganz(r, "Tagtyp") - 1;
                    if (t < 0 || t >= Tagesgangsatz.TAGTYPEN) continue;   // CHECK der DDL haelt 1..4
                    if (!anteile.TryGetValue(satz, out double[,] a))
                    {
                        anteile[satz] = a = new double[Tagesgangsatz.TAGTYPEN, Tagesgangsatz.STUNDEN];
                        herkunft[satz] = new Provenienz[Tagesgangsatz.TAGTYPEN];
                    }
                    for (int h = 0; h < Tagesgangsatz.STUNDEN; h++)
                        a[t, h] = Zahl(r, AnteilSpalte(h + 1));
                    herkunft[satz][t] = Herkunft(r, "");
                }

            var saetze = new Dictionary<int, Tagesgangsatz>();
            if (koepfe != null)
                foreach (DataRow r in koepfe.Rows)
                {
                    int id = Ganz(r, "ID");
                    saetze[id] = new Tagesgangsatz(
                        id,
                        anteile.TryGetValue(id, out double[,] a) ? a : new double[Tagesgangsatz.TAGTYPEN, Tagesgangsatz.STUNDEN],
                        herkunft.TryGetValue(id, out Provenienz[] p) ? p : new Provenienz[Tagesgangsatz.TAGTYPEN])
                    {
                        Bezeichner = Text(r, "Bezeichner"),
                        Katalogversion = Text(r, "Katalogversion"),
                        Status = TwwWertemengen.Status(Text(r, "Status")),
                        ReadOnly = Wahr(r, "ReadOnly")
                    };
                }
            return saetze;
        }

        /// <summary>Der Spaltenname der Stunde <paramref name="stunde"/> (1 … 24) — aus einer Schleife, nie aus einer Eingabe.</summary>
        internal static string AnteilSpalte(int stunde)
            => "Anteil_" + stunde.ToString("00", CultureInfo.InvariantCulture);

        /// <summary>
        /// Die Nutzungsarten aus den Zeilen von <c>Tab_TwwNutzungsart_STAMM</c>; ein Tagesgangsatz,
        /// der in <paramref name="saetze"/> fehlt, bleibt <c>null</c>. Auch der Weg der Katalogpflege
        /// (<c>TwwNutzungsartCtrl</c>), die ihre Bezugszeile im laufenden Vorgang liest.
        /// </summary>
        internal static IReadOnlyList<Nutzungsart> NutzungsartenAus(DataTable dt, Dictionary<int, Tagesgangsatz> saetze)
        {
            var liste = new List<Nutzungsart>();
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                var bedarf = new double[NutzungsartRaster.NIVEAUS];
                var min = new double?[NutzungsartRaster.NIVEAUS];
                var max = new double?[NutzungsartRaster.NIVEAUS];
                for (int n = 0; n < NutzungsartRaster.NIVEAUS; n++)
                {
                    string spalte = NutzungsartRaster.Niveauspalten[n];
                    bedarf[n] = Zahl(r, spalte);
                    min[n] = ZahlOderNull(r, spalte + "_Min");
                    max[n] = ZahlOderNull(r, spalte + "_Max");
                }

                var monate = new double[NutzungsartRaster.MONATE];
                for (int m = 0; m < NutzungsartRaster.MONATE; m++)
                    monate[m] = Zahl(r, "Monat_" + (m + 1).ToString(CultureInfo.InvariantCulture));

                var woche = new double[NutzungsartRaster.WOCHENTAGE];
                for (int w = 0; w < NutzungsartRaster.WOCHENTAGE; w++)
                    woche[w] = Zahl(r, "Woche_" + (w + 1).ToString(CultureInfo.InvariantCulture));

                saetze.TryGetValue(Ganz(r, "ID_Tagesgangsatz"), out Tagesgangsatz satz);

                var herkunft = new Katalogherkunft(
                    Herkunft(r, "Bedarf_"),
                    new Bedarfsbandbreite(min, max),
                    Herkunft(r, "Jahresgang_"),
                    Herkunft(r, "Wochengang_"),
                    satz?.JeTagtyp ?? new Provenienz[Tagesgangsatz.TAGTYPEN]);

                liste.Add(new Nutzungsart(
                    Ganz(r, "ID"),
                    Text(r, "Bezeichner"),
                    (ZapfBezugsart)Ganz(r, "Bezugsart"),
                    bedarf,
                    new Temperaturbezug(Zahl(r, "Bezug_Zapftemperatur"), Zahl(r, "Bezug_Kaltwasser")),
                    (ZapfBilanzgrenze)Ganz(r, "Bilanzgrenze"),
                    (ZapfKalenderart)Ganz(r, "Kalenderart"),
                    ZahlOderNull(r, "Ferienfaktor"),
                    monate,
                    woche,
                    satz,
                    herkunft)
                {
                    Katalogversion = Text(r, "Katalogversion"),
                    Status = TwwWertemengen.Status(Text(r, "Status")),
                    ReadOnly = Wahr(r, "ReadOnly"),
                    IdVorlage = GanzOderNull(r, "ID_Vorlage"),
                    Freigabe = TextOderNull(r, "Freigabe")
                });
            }
            return liste;
        }

        private static ZonenStand ZoneAus(DataRow r, Dictionary<int, List<WohnungstypStand>> wohnungen)
        {
            var beginn = new int?[4];
            var ende = new int?[4];
            for (int i = 0; i < 4; i++)
            {
                string n = (i + 1).ToString(CultureInfo.InvariantCulture);
                beginn[i] = GanzOderNull(r, "Ferienbeginn_" + n);
                ende[i] = GanzOderNull(r, "Ferienende_" + n);
            }

            var auslastung = new double?[NutzungsartRaster.MONATE];
            for (int m = 0; m < NutzungsartRaster.MONATE; m++)
                auslastung[m] = ZahlOderNull(r, "Auslastung_" + (m + 1).ToString("00", CultureInfo.InvariantCulture));

            int id = Ganz(r, "ID");
            int? einheit = GanzOderNull(r, "Jahresmesswert_Einheit");
            int? grenze = GanzOderNull(r, "Jahresmesswert_Bilanzgrenze");

            return new ZonenStand
            {
                Id = id,
                IdNutzungsart = Ganz(r, "ID_Nutzungsart"),
                IdTagesgangsatz = GanzOderNull(r, "ID_Tagesgangsatz"),
                IdGebaeude = GanzOderNull(r, "ID_Gebaeude"),
                Reihenfolge = Ganz(r, "Reihenfolge"),
                Name = Text(r, "Name"),
                Bezugsmenge = Zahl(r, "Bezugsmenge"),
                Niveau = (ZapfNiveau)Ganz(r, "Niveau"),
                PersonenJeWe = ZahlOderNull(r, "Personen_je_WE"),
                WohnflaecheJeWeM2 = ZahlOderNull(r, "Wohnflaeche_je_WE"),
                Topologie = (ZapfTopologie)Ganz(r, "Topologie"),
                Zirkulation = Wahr(r, "Zirkulation"),
                Ferienbeginn = beginn,
                Ferienende = ende,
                Jahresmesswert = ZahlOderNull(r, "Jahresmesswert"),
                JahresmesswertEinheit = einheit.HasValue ? (ZapfMesswerteinheit)einheit.Value : (ZapfMesswerteinheit?)null,
                JahresmesswertBilanzgrenze = grenze.HasValue ? (ZapfBilanzgrenze)grenze.Value : (ZapfBilanzgrenze?)null,
                JahresmesswertQuelle = TextOderNull(r, "Jahresmesswert_Quelle"),
                JahresmesswertZeitraum = TextOderNull(r, "Jahresmesswert_Zeitraum"),
                SpeicherverlustKwhJeJahr = ZahlOderNull(r, "Speicherverlust_Kwh_a"),
                TagesbedarfAuto = Wahr(r, "Tagesbedarf_Auto"),
                TagesbedarfManuellKwh = ZahlOderNull(r, "Tagesbedarf_Manuell_Kwh"),
                BedarfSpezKwhJeEinheitTag = ZahlOderNull(r, "Bedarf_Spez"),
                ZapftemperaturC = ZahlOderNull(r, "Zapftemperatur"),
                KaltwasserMittelC = ZahlOderNull(r, "Kaltwasser_Mittel"),
                KaltwasserAmplitudeK = ZahlOderNull(r, "Kaltwasser_Amplitude"),
                Auslastung = auslastung,
                Wohnungen = wohnungen.TryGetValue(id, out List<WohnungstypStand> w)
                    ? w : (IReadOnlyList<WohnungstypStand>)new WohnungstypStand[0]
            };
        }

        private static ProjektStand ProjektAus(DataRow r)
        {
            int? lage = GanzOderNull(r, "Zirk_Lage");
            int? quelle = GanzOderNull(r, "Bedarfstag_Quelle");
            // Schritt 124 (T3): Vor dem Schritt fehlen die Spalten — dann gelten die DDL-Vorgaben.
            int? erzeuger = SpalteDa(r, TwwSchema.SPALTE_ERZEUGERART) ? GanzOderNull(r, TwwSchema.SPALTE_ERZEUGERART) : null;
            int? werkstoff = SpalteDa(r, TwwSchema.SPALTE_UEBERTRAGER_WERKSTOFF)
                ? GanzOderNull(r, TwwSchema.SPALTE_UEBERTRAGER_WERKSTOFF) : null;
            int? bezug = SpalteDa(r, TwwSchema.SPALTE_FUELLSTAND_BEZUG) ? GanzOderNull(r, TwwSchema.SPALTE_FUELLSTAND_BEZUG) : null;
            // Schritt 131 (T3 „Typtage"): die Wahl des Typtagwegs - vor dem Schritt fehlen die
            // Spalten, dann gelten die DDL-Vorgaben (aus, keine Angabe).
            int? typtagzone = SpalteDa(r, TwwSchema.SPALTE_TYPTAGE_KLIMAZONE)
                ? GanzOderNull(r, TwwSchema.SPALTE_TYPTAGE_KLIMAZONE) : null;

            return new ProjektStand
            {
                Id = Ganz(r, "ID"),
                Weg = WegAus(Text(r, "Weg")),
                JahresreiheStochastisch = Wahr(r, "Jahresreihe_Stochastisch"),
                Seed = Ganz(r, "Seed"),
                Realisierungen = Ganz(r, "Realisierungen"),
                RealisierungenAuslegung = GanzOderNull(r, "Realisierungen_Auslegung"),
                Perzentil = Ganz(r, "Perzentil"),
                ZirkAuto = Wahr(r, "Zirk_Auto"),
                ZirkMethode = (ZapfZirkulationsmethode)Ganz(r, "Zirk_Methode"),
                ZirkLage = lage.HasValue ? (ZapfLeitungslage)lage.Value : (ZapfLeitungslage?)null,
                ZirkLaengeM = ZahlOderNull(r, "Zirk_Laenge_m"),
                ZirkVerlustWJeM = ZahlOderNull(r, "Zirk_Verlust_W_m"),
                ZirkAnteil = ZahlOderNull(r, "Zirk_Anteil"),
                ZirkKennwert = ZahlOderNull(r, "Zirk_Kennwert"),
                ZirkFlaecheM2 = ZahlOderNull(r, "Zirk_Flaeche_m2"),
                ZirkLaufzeitH = ZahlOderNull(r, "Zirk_Laufzeit_h"),
                ZirkManuellKw = ZahlOderNull(r, "Zirk_Manuell_Kw"),
                LeitungsinhaltL = ZahlOderNull(r, "Leitungsinhalt_l"),
                LadeAuto = Wahr(r, "Lade_Auto"),
                LadefensterH = ZahlOderNull(r, "Ladefenster_h"),
                LadefensterBeginnH = ZahlOderNull(r, "Ladefenster_Beginn_h"),
                LadeManuellKw = ZahlOderNull(r, "Lade_Manuell_Kw"),
                SpeicherC = ZahlOderNull(r, "Speicher_C"),
                KaltwasserAuslegungC = ZahlOderNull(r, "Kaltwasser_Auslegung_C"),
                ErzeugerKw = ZahlOderNull(r, "Erzeuger_Kw"),
                UebertragerKw = ZahlOderNull(r, "Uebertrager_Kw"),
                UebertragerUaWJeK = ZahlOderNull(r, "Uebertrager_UA_W_K"),
                UebertragerFlaecheM2 = ZahlOderNull(r, "Uebertrager_Flaeche_m2"),
                Speicherart = (ZapfSpeicherart)Ganz(r, "Speicherart"),
                SensorhoeheAnteil = ZahlOderNull(r, "Sensorhoehe_Anteil"),
                NachweisVolumenL = ZahlOderNull(r, "Nachweis_Volumen_l"),
                SpeicherverlustW = ZahlOderNull(r, "Speicherverlust_W"),
                Nutzanteil = ZahlOderNull(r, "Nutzanteil"),
                Zuschlag = ZahlOderNull(r, "Zuschlag"),
                BedarfstagQuelle = quelle.HasValue ? (ZapfBedarfstagquelle)quelle.Value : (ZapfBedarfstagquelle?)null,
                IdBedarfstag = GanzOderNull(r, "ID_Bedarfstag"),
                AuslegungVolumenL = ZahlOderNull(r, "Auslegung_Volumen_l"),
                AuslegungLeistungKw = ZahlOderNull(r, "Auslegung_Leistung_Kw"),
                Aenderungsdatum = TextOderNull(r, "Aenderungsdatum"),
                Erzeugerart = erzeuger.HasValue ? (ZapfErzeugerart)erzeuger.Value : (ZapfErzeugerart?)null,
                UebertragerWerkstoff = werkstoff.HasValue ? (ZapfUebertragerwerkstoff)werkstoff.Value : (ZapfUebertragerwerkstoff?)null,
                PersonenAuto = !SpalteDa(r, TwwSchema.SPALTE_PERSONEN_AUTO) || Wahr(r, TwwSchema.SPALTE_PERSONEN_AUTO),
                PersonenManuell = SpalteDa(r, TwwSchema.SPALTE_PERSONEN_MANUELL) ? ZahlOderNull(r, TwwSchema.SPALTE_PERSONEN_MANUELL) : null,
                FuellstandBezug = bezug.HasValue ? (ZapfFuellstandbezug)bezug.Value : (ZapfFuellstandbezug?)null,
                TyptageAktiv = SpalteDa(r, TwwSchema.SPALTE_TYPTAGE_AKTIV) && Wahr(r, TwwSchema.SPALTE_TYPTAGE_AKTIV),
                TyptageKlimazone = typtagzone,
                TyptageGebaeudeart = SpalteDa(r, TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART)
                    ? TextOderNull(r, TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART) : null
            };
        }

        /// <summary>Führt die gelesene Zeile die Spalte (Stand nach dem Schemaschritt)?</summary>
        internal static bool SpalteDa(DataRow r, string spalte) => r?.Table != null && r.Table.Columns.Contains(spalte);

        // =================================================================================
        // Lesehilfen — die DataTable liefert je nach Spalte long, int, bool oder double
        // (SqliteDatenzugriff.LadeTabelle, Regeln D9); hier wird einmal umgesetzt.
        // =================================================================================

        /// <summary>Die Provenienz einer Wertgruppe; <paramref name="praefix"/> etwa „Bedarf_" oder leer.</summary>
        internal static Provenienz Herkunft(DataRow r, string praefix)
            => new Provenienz(
                Text(r, praefix + "Quelle"),
                TextOderNull(r, praefix + "Ausgabe"),
                Text(r, praefix + "Version"),
                TwwWertemengen.Herkunft(Text(r, praefix + "Herkunftsart")));

        internal static string Text(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? "" : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        internal static string TextOderNull(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        internal static double Zahl(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? 0.0 : Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        internal static double? ZahlOderNull(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? (double?)null : Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        internal static int Ganz(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        internal static int? GanzOderNull(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        internal static bool Wahr(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v != null && v != DBNull.Value && Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0;
        }
    }
}
