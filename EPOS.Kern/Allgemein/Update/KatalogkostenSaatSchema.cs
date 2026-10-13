using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SAAT DER KATALOGKOSTEN-SATZVORLAGEN - Welle KB-3 (Anwenderentscheid 13.10.2026:
    // "fuer alle Anlagen die zentrale Verwaltung der Energie-, Betriebs- und Investitionskosten,
    // keine Doppelhaltung der Geraetekosten; die Kosten einer Anlage sollen mit dem zentralen
    // Kostenkatalog abgeglichen werden koennen - keine mehrfache Kostenposition im Katalogsatz").
    //
    // WAS. Zwei Teile in einem Schritt:
    //   a. DDL: ID_KostenVorlage und ID_KostenVorlageInvestition an den ACHT PROJEKTKOPIEN der
    //      Kataloge mit Kosten (Tab_BHKW, Tab_Heizkessel, Tab_WP, Tab_PV, Tab_Solarkollektoren,
    //      Tab_Pufferspeicher, Tab_Kaeltemaschine, Tab_Stromspeicher). Die acht KATALOGE tragen
    //      beide Spalten seit den Schritten 208/209 (KatalogkostenUrsprungSchema,
    //      KatalogkostenInvestitionSchema); die Kopien brauchen sie, weil ihre Kostenspalten mit
    //      Welle KB-4 entfallen und ihr Ursprung meist unbekannt ist (ID_Stamm leer, Schritt 208
    //      traegt ihn bewusst nicht ueber den Namen nach).
    //   b. DML (Saat): Je Satz mit mindestens einem Kostenwert > 0 und OHNE Verweis entsteht
    //      GENAU EINE Satzvorlage je Art - eine Investitionsvorlage (KategorieID 1) und eine
    //      Betriebsvorlage (KategorieID 2) -, in ihr je gepflegter Kostenspalte eine Position mit
    //      Bemessung nach Konzept Wirtschaftlichkeit 3.2 und dem Spaltenwert als Satz. Der Satz
    //      verweist hierueber (ID_KostenVorlageInvestition bzw. ID_KostenVorlage); mehr als ein
    //      Verweis je Art kann nicht entstehen.
    //
    // DIE BEMESSUNG IST DIE DES RECHENWEGS, nicht eine neue. Sie steht je Spalte in FELDER und
    // trifft die Zahl, die TechnikPlanwertCtrl.BasenFuellen heute als Planwert zeigt:
    //   - Ein absoluter Geraetepreis wird BETRAG (BHKW-Modul und seine vier Nebenposten,
    //     Heizkessel- und Pufferspeicher-Investitionskosten, Waermepumpen-Modulkosten,
    //     Stromspeicher Investition_Fix).
    //   - Ein STUECKPREIS (Preis je Modul, im Projekt mit der Stueckzahl der Anlage
    //     multipliziert) wird zum spezifischen Satz, indem er durch die Baugroesse EINES Stuecks
    //     geteilt wird: Photovoltaik Modulkosten / (Leistung [W] / 1000) -> EUR_PRO_KWP,
    //     Solarthermie Investitionskosten / Aperturflaeche -> EUR_PRO_M2_KOLLEKTOR,
    //     Kaeltemaschine Modulkosten / Nennkaelteleistung -> EUR_PRO_KW_LEISTUNG. Satz x
    //     Bezugsgroesse ergibt damit wieder Stueckpreis x Stueckzahl - dieselbe Zahl, eine Basis.
    //   - Ein spezifischer Satz bleibt er: Stromspeicher Modulkosten [EUR/kWh] ->
    //     EUR_PRO_KWH_KAPAZITAET, Leistungskosten [EUR/kW] -> EUR_PRO_KW_LEISTUNG,
    //     BHKW Wartungskosten_kwhel -> EUR_PRO_KWH_ELEKTRISCH, Stromspeicher Verschleisskosten
    //     (je entladener kWh) -> EUR_PRO_KWH_ELEKTRISCH.
    //   - Die Kessel-Wartung traegt ihren Zeitbezug in Wartungskosten_Einheit: EUR/a ->
    //     JAHRESBETRAG, EUR/kWh -> EUR_PRO_KWH_THERMISCH, %/a -> PROZENT_INVESTITION.
    //
    // NICHT GESAET wird Tab_BHKW(_STAMM).Investition_kwel: Der Wert ist seit dem Nutzerentscheid
    // vom 22.08.2026 aus Kosten_Modul und den vier Nebenposten ABGELEITET (BHKWKosten) - eine
    // zweite Position daraus waere dieselbe Zahl ein zweites Mal.
    //
    // OHNE BEZUGSGROESSE KEINE POSITION. Fehlt einem Stueckpreis seine Baugroesse (Modulleistung,
    // Aperturflaeche, Nennkaelteleistung <= 0), entsteht KEINE Position mit Fantasiesatz; die
    // Zeile steht benannt im Bericht und behaelt ihren Kostenwert, bis die Baugroesse gepflegt ist.
    //
    // IDEMPOTENT. Schluessel ist der Verweis des Satzes: Ein zweiter Lauf findet ihn gesetzt und
    // ruehrt nichts an - auch keine vom Anwender geaenderte Vorlage. Ein Satz ohne Kosten bekommt
    // keine leere Vorlage. Je Satz und Art entsteht nie mehr als eine Vorlage.
    //
    // GESPEICHERTER KATALOGTEXT, DEUTSCH. Name, Bemerkung und Positionsbezeichnung sind
    // Katalogdaten wie bei den ausgelieferten Standardvorlagen (Muster KaeltetypSaat), nicht
    // Oberflaechentexte: Zwei Laeufe in zwei Sprachen duerfen nicht zwei Kataloge ergeben. Die
    // Bezeichnungen sind wortgleich mit den Positionen der Standardvorlage des Gewerks, damit der
    // Abgleich (Katalogkostenabgleich) Projekt- und Katalogposition findet.
    //
    // ERGEBNISNEUTRAL. Der Rechenweg liest Investition und Betrieb allein aus Tab_ProjektWerte
    // (InvestKaskade.Lies); eine Satzvorlage wirkt erst, wenn der Anwender sie uebernimmt oder
    // abgleicht. Keine Katalogpruefsumme aendert sich: ID_KostenVorlage und
    // ID_KostenVorlageInvestition sind Metaspalten der Katalogfassung, und Tab_KostenVorlage
    // reist in keinem Katalogpaket. Der Referenzlauf bleibt byte-gleich.
    //
    // NUMMER. 226 - angemeldet fuer KB-3. Sie haengt an 225 (Sitzung Gebaeudesimulation, DA-2),
    // sobald 225 auf origin liegt; bis dahin rechnet sie sich aus der hoechsten Klasse im Baum
    // (BauteilkatalogSchema, 221) + 5. Eingetragen in SchemaStand.Zielversion, im Register der
    // Paketanhebung, in der SchemaMigration der Schale, in Werkzeuge/Testdatenbankschema und in
    // EPOS.Kern.Tests/TestDatenbank.
    // ====================================================================================

    /// <summary>Eine Kostenspalte eines Geraetekatalogs und die Position, die aus ihr entsteht.</summary>
    public sealed class Katalogkostenfeld
    {
        public Katalogkostenfeld(string spalte, int kategorie, string bemessung, string bezeichnung,
                                 string kostenart, int sortierung, string bezugsspalte = null,
                                 double bezugsteiler = 1.0, string einheitspalte = null)
        {
            Spalte = spalte;
            Kategorie = kategorie;
            Bemessung = bemessung;
            Bezeichnung = bezeichnung;
            Kostenart = kostenart;
            Sortierung = sortierung;
            Bezugsspalte = bezugsspalte;
            Bezugsteiler = bezugsteiler;
            Einheitspalte = einheitspalte;
        }

        /// <summary>Die Kostenspalte am Geraet.</summary>
        public string Spalte { get; }

        /// <summary>1 = Investition, 2 = Betrieb (<c>DbWerte.KOSTEN_KATEGORIE_*</c>).</summary>
        public int Kategorie { get; }

        /// <summary>Die Bemessungsart der Position (<c>DbWerte.BEMESSUNG_*</c>).</summary>
        public string Bemessung { get; }

        /// <summary>Die gespeicherte Bezeichnung der Position (Katalogtext, deutsch).</summary>
        public string Bezeichnung { get; }

        /// <summary>Die Kostenart nach VDI 2067 (<c>DbWerte.KOSTENART_*</c>).</summary>
        public string Kostenart { get; }

        /// <summary>Sortierung innerhalb der Vorlage.</summary>
        public int Sortierung { get; }

        /// <summary>Die Baugroesse EINES Stuecks, durch die ein Stueckpreis geteilt wird; leer = der
        /// Spaltenwert IST der Satz.</summary>
        public string Bezugsspalte { get; }

        /// <summary>Teiler der Bezugsgroesse (Photovoltaik: 1000 W je kWp).</summary>
        public double Bezugsteiler { get; }

        /// <summary>Die Spalte, die den Zeitbezug traegt (Kessel-Wartung); leer = feste Bemessung.</summary>
        public string Einheitspalte { get; }
    }

    /// <summary>Ein Gewerk der Saat: Katalog, Projektkopie, Kostenkomponente und seine Kostenspalten.</summary>
    public sealed class Katalogkostengewerk
    {
        public Katalogkostengewerk(string katalog, string kopie, int komponentenId,
                                   IReadOnlyList<Katalogkostenfeld> felder)
        {
            Katalog = katalog;
            Kopie = kopie;
            KomponentenId = komponentenId;
            Felder = felder;
        }

        /// <summary>Die Katalogtabelle (<c>_STAMM</c>).</summary>
        public string Katalog { get; }

        /// <summary>Die Projektkopie.</summary>
        public string Kopie { get; }

        /// <summary>Die Kostenkomponente (<c>Tab_KostenKomponente.ID</c>).</summary>
        public int KomponentenId { get; }

        /// <summary>Die Kostenspalten des Gewerks.</summary>
        public IReadOnlyList<Katalogkostenfeld> Felder { get; }
    }

    /// <summary>
    /// <b>Saat der Katalogkosten-Satzvorlagen</b> — je Katalogsatz mit Kosten genau eine Satzvorlage
    /// je Art, Verweis über <c>ID_KostenVorlage</c>/<c>ID_KostenVorlageInvestition</c>. Anlass,
    /// Bemessung und Grenzen im Kopf der Datei.
    /// </summary>
    public static class KatalogkostenSaatSchema
    {
        /// <summary>
        /// Die Nummer des Schritts: <b>226</b>. Sie hängt an 225 (Sitzung Gebäudesimulation, DA-2),
        /// sobald 225 auf origin liegt; bis dahin rechnet sie sich aus der höchsten Klasse im Baum
        /// (<see cref="BauteilkatalogSchema"/>, 221) + 5.
        /// </summary>
        public const int SCHRITT = BauteilkatalogSchema.SCHRITT + 5;

        /// <summary>Die Tabelle der Kostenvorlagen.</summary>
        public const string TAB_KOSTENVORLAGE = KatalogkostenUrsprungSchema.TAB_KOSTENVORLAGE;

        /// <summary>Die Tabelle der Vorlagenpositionen.</summary>
        public const string TAB_KOSTENVORLAGEPOSITION = "Tab_KostenVorlagePosition";

        private const int K_INVEST = DbWerte.KOSTEN_KATEGORIE_INVESTITION;
        private const int K_BETRIEB = DbWerte.KOSTEN_KATEGORIE_BETRIEB;

        /// <summary>Die acht Gewerke mit Kostenspalten und die Position je Spalte.</summary>
        public static readonly IReadOnlyList<Katalogkostengewerk> GEWERKE = new[]
        {
            new Katalogkostengewerk("Tab_WP_STAMM", "Tab_WP", 1, new[]
            {
                new Katalogkostenfeld("Modulkosten", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      "Wärmepumpe (Aggregat)", DbWerte.KOSTENART_KAPITALGEBUNDEN, 10),
            }),
            new Katalogkostengewerk("Tab_Heizkessel_STAMM", "Tab_Heizkessel", 2, new[]
            {
                new Katalogkostenfeld("Investitionskosten", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      "Wärmeerzeuger (Kessel)", DbWerte.KOSTENART_KAPITALGEBUNDEN, 10),
                new Katalogkostenfeld("Wartungskosten", K_BETRIEB, DbWerte.BEMESSUNG_JAHRESBETRAG,
                                      "Vollwartung / Wartung Kessel", DbWerte.KOSTENART_BETRIEBSGEBUNDEN, 10,
                                      einheitspalte: "Wartungskosten_Einheit"),
            }),
            new Katalogkostengewerk("Tab_PV_STAMM", "Tab_PV", 3, new[]
            {
                new Katalogkostenfeld("Modulkosten", K_INVEST, DbWerte.BEMESSUNG_EUR_PRO_KWP,
                                      "PV-Module", DbWerte.KOSTENART_KAPITALGEBUNDEN, 10,
                                      bezugsspalte: "Leistung", bezugsteiler: 1000.0),
            }),
            new Katalogkostengewerk("Tab_Solarkollektoren_STAMM", "Tab_Solarkollektoren", 4, new[]
            {
                new Katalogkostenfeld("Investitionskosten", K_INVEST, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR,
                                      "Sonnenkollektoren", DbWerte.KOSTENART_KAPITALGEBUNDEN, 10,
                                      bezugsspalte: "Aperturflaeche"),
            }),
            new Katalogkostengewerk("Tab_Stromspeicher_STAMM", "Tab_Stromspeicher", 5, new[]
            {
                new Katalogkostenfeld("Modulkosten", K_INVEST, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                                      "Speicher", DbWerte.KOSTENART_KAPITALGEBUNDEN, 10),
                new Katalogkostenfeld("Leistungskosten", K_INVEST, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                                      "Leistungsteil (Wechselrichter)", DbWerte.KOSTENART_KAPITALGEBUNDEN, 20),
                new Katalogkostenfeld("Investition_Fix", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      "Sonstiges", DbWerte.KOSTENART_KAPITALGEBUNDEN, 30),
                new Katalogkostenfeld("Verschleisskosten", K_BETRIEB, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                                      "Verschleiß (je entladener kWh)", DbWerte.KOSTENART_BETRIEBSGEBUNDEN, 10),
            }),
            new Katalogkostengewerk("Tab_Pufferspeicher_STAMM", "Tab_Pufferspeicher", 6, new[]
            {
                new Katalogkostenfeld("Investitionskosten", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      "Speicher", DbWerte.KOSTENART_KAPITALGEBUNDEN, 10),
            }),
            new Katalogkostengewerk("Tab_BHKW_STAMM", "Tab_BHKW", 7, new[]
            {
                new Katalogkostenfeld("Kosten_Modul", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      "BHKW-Modul (Kompaktaggregat)", DbWerte.KOSTENART_KAPITALGEBUNDEN, 10),
                new Katalogkostenfeld("Kosten_Montage", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      DbWerte.KOSTENPOSTEN_MONTAGE, DbWerte.KOSTENART_KAPITALGEBUNDEN, 20),
                new Katalogkostenfeld("Kosten_Lieferung", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      DbWerte.KOSTENPOSTEN_LIEFERUNG, DbWerte.KOSTENART_KAPITALGEBUNDEN, 30),
                new Katalogkostenfeld("Kosten_Schallschutzhaube", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      DbWerte.KOSTENPOSTEN_SCHALLSCHUTZHAUBE, DbWerte.KOSTENART_KAPITALGEBUNDEN, 40),
                new Katalogkostenfeld("Kosten_Abgasreinigung", K_INVEST, DbWerte.BEMESSUNG_BETRAG,
                                      DbWerte.KOSTENPOSTEN_ABGASREINIGUNG, DbWerte.KOSTENART_KAPITALGEBUNDEN, 50),
                new Katalogkostenfeld("Wartungskosten_kwhel", K_BETRIEB, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                                      "Wartung BHKW", DbWerte.KOSTENART_BETRIEBSGEBUNDEN, 10),
            }),
            new Katalogkostengewerk(KaeltemaschineSchema.TAB_STAMM, KaeltemaschineSchema.TAB_PROJEKT,
                                    KaeltemaschineAnlageSchema.KOMPONENTE_KAELTEMASCHINE, new[]
            {
                new Katalogkostenfeld(KaeltemaschineSchema.SPALTE_MODULKOSTEN, K_INVEST,
                                      DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, "Kältemaschine (Aggregat)",
                                      DbWerte.KOSTENART_KAPITALGEBUNDEN, 10,
                                      bezugsspalte: KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG),
            }),
        };

        /// <summary>Die sechzehn Spalten des DDL-Teils: zwei Verweise an jeder der acht Projektkopien.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            GEWERKE.SelectMany(g => new[]
            {
                (g.Kopie, KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE, Verweistyp()),
                (g.Kopie, KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION, Verweistyp()),
            }).ToArray();

        private static string Verweistyp()
            => "INTEGER REFERENCES \"" + TAB_KOSTENVORLAGE + "\" (\"ID\") ON DELETE SET NULL";

        /// <summary>Die Tabellen, die stehen müssen, bevor der Schritt läuft.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => GEWERKE.Select(g => g.Katalog).Concat(GEWERKE.Select(g => g.Kopie))
                      .Concat(new[] { TAB_KOSTENVORLAGE, TAB_KOSTENVORLAGEPOSITION })
                      .Distinct(StringComparer.Ordinal).ToArray();

        /// <summary>Die Verweisspalte je Kategorie.</summary>
        public static string Verweisspalte(int kategorie)
            => kategorie == K_INVEST
                ? KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION
                : KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE;

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen alle Spalten des DDL-Teils?</summary>
        public static bool SpaltenStehen()
            => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Steht der Schritt vollständig — Spalten da und keine Zeile mehr offen?</summary>
        public static bool Vollstaendig() => SpaltenStehen() && Offen() == 0;

        /// <summary>
        /// Die Zahl der Zeilen, die eine Satzvorlage tragen müssten und keinen Verweis haben —
        /// die Messlatte der Wache und der Vollständigkeitsprüfung.
        /// </summary>
        public static int Offen() => Offen(GEWERKE);

        /// <summary>
        /// <see cref="Offen()"/> für eine Auswahl der Gewerke — der Weg, auf dem eine spätere Welle
        /// dieselbe Maschinerie für ihr eigenes Gewerk fährt (<see cref="GeraetekostenEntfallSchema"/>).
        /// </summary>
        public static int Offen(IReadOnlyList<Katalogkostengewerk> gewerke)
        {
            int offen = 0;
            foreach (Katalogkostengewerk g in gewerke ?? GEWERKE)
                foreach (string tabelle in new[] { g.Katalog, g.Kopie })
                    foreach (Satzbefund b in Befunde(g, tabelle))
                        if (b.Verweis == 0 && b.Positionen.Count > 0) offen++;
            return offen;
        }

        /// <summary>
        /// Legt die fehlenden Verweisspalten an und sät die fehlenden Satzvorlagen — <b>wiederholbar</b>;
        /// ein zweiter Lauf ändert nichts. Gibt die Zahl der Handgriffe zurück (Spalten + Vorlagen).
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            int handgriffe = SpaltenAnlegen(bericht);
            handgriffe += Saeen(bericht, GEWERKE);
            bericht?.Add("Rechenweg unberuehrt: Investition und Betrieb kommen aus Tab_ProjektWerte; " +
                         "der Referenzlauf bleibt byte-gleich");
            return handgriffe;
        }

        // ------------------------------------------------------------------- DDL ---

        private static int SpaltenAnlegen(IList<string> bericht)
        {
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - die zwei Verweise an den acht Projektkopien; nichts zu tun");
                return 0;
            }
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            if (bericht != null) foreach (string z in zeilen) bericht.Add(z);
            return offen.Count;
        }

        // ------------------------------------------------------------------ Saat ---

        /// <summary>Eine Position, wie sie aus einer Kostenspalte entsteht.</summary>
        internal sealed class Positionssaat
        {
            internal Katalogkostenfeld Feld;
            internal string Bemessung;
            internal double Satz;
        }

        /// <summary>Was an einem Satz zu tun ist: Verweis, Name und die Positionen je Kategorie.</summary>
        internal sealed class Satzbefund
        {
            internal int Id;
            internal string Name = "";
            internal int Kategorie;
            internal int Verweis;
            internal List<Positionssaat> Positionen = new List<Positionssaat>();
            internal List<string> OhneBezug = new List<string>();
        }

        /// <summary>
        /// Sät die fehlenden Satzvorlagen der übergebenen Gewerke — <b>wiederholbar</b>; ein zweiter Lauf
        /// ändert nichts. Eine spätere Welle fährt damit dieselbe Maschinerie für ihr eigenes Gewerk,
        /// statt die Saat ein zweites Mal zu bauen (<see cref="GeraetekostenEntfallSchema"/>).
        /// </summary>
        public static int Saeen(IList<string> bericht, IReadOnlyList<Katalogkostengewerk> gewerke)
        {
            int vorlagen = 0, positionen = 0;
            string heute = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var zielspalten = new HashSet<string>(DataRepository.SpaltenVonTabelle(TAB_KOSTENVORLAGEPOSITION),
                                                  StringComparer.OrdinalIgnoreCase);
            foreach (Katalogkostengewerk g in gewerke ?? GEWERKE)
                foreach (string tabelle in new[] { g.Katalog, g.Kopie })
                {
                    var offen = Befunde(g, tabelle)
                                .Where(b => b.Verweis == 0 && b.Positionen.Count > 0).ToList();
                    foreach (Satzbefund b in offen.Where(x => x.OhneBezug.Count > 0))
                        bericht?.Add(tabelle + " ID " + b.Id + ": " + string.Join(", ", b.OhneBezug));
                    if (offen.Count == 0) continue;
                    using (DbVorgang v = DataRepository.Vorgang())
                    {
                        try
                        {
                            foreach (Satzbefund b in offen)
                            {
                                int idVorlage = VorlageAnlegen(v, g, b, tabelle, heute);
                                foreach (Positionssaat p in b.Positionen)
                                    positionen += PositionAnlegen(v, idVorlage, p, zielspalten);
                                v.Ausfuehren("UPDATE \"" + tabelle + "\" SET \"" + Verweisspalte(b.Kategorie) +
                                             "\" = ? WHERE \"ID\" = ?",
                                             new DbParam("@v", idVorlage), new DbParam("@id", b.Id));
                                vorlagen++;
                            }
                            v.Commit();
                        }
                        catch
                        {
                            v.Rollback();
                            throw;
                        }
                    }
                    bericht?.Add(tabelle + ": " + offen.Count + " Satzvorlage(n) gesaet");
                }
            if (vorlagen == 0) bericht?.Add("Saat: kein Satz mit Kosten ohne Verweis; nichts zu tun");
            else bericht?.Add("Saat: " + vorlagen + " Satzvorlage(n) mit " + positionen + " Position(en)");
            return vorlagen;
        }

        private static int VorlageAnlegen(DbVorgang v, Katalogkostengewerk g, Satzbefund b,
                                          string tabelle, string heute)
        {
            string name = b.Name.Length == 0 ? tabelle + " " + b.Id : b.Name;
            if (name.Length > Katalogrueckweg.NAME_VORLAGE_MAX)
                name = name.Substring(0, Katalogrueckweg.NAME_VORLAGE_MAX);
            return v.EinfuegenUndId(
                "INSERT INTO \"" + TAB_KOSTENVORLAGE + "\" (\"KomponentenID\", \"KategorieID\", \"Name\", " +
                "\"IstStandard\", \"ReadOnly\", \"Bemerkung\", \"GeaendertAm\") VALUES (?, ?, ?, 0, 0, ?, ?)",
                new[]
                {
                    new DbParam("@c", g.KomponentenId), new DbParam("@k", b.Kategorie),
                    new DbParam("@n", name), new DbParam("@b", BEMERKUNG + " " + heute),
                    new DbParam("@g", heute),
                });
        }

        /// <summary>Die Herkunft, die jede gesäte Vorlage trägt (Katalogtext, deutsch).</summary>
        public const string BEMERKUNG = "Aus den Kostenspalten des Katalogsatzes übernommen am";

        private static int PositionAnlegen(DbVorgang v, int idVorlage, Positionssaat p,
                                           HashSet<string> zielspalten)
        {
            var werte = new List<(string Spalte, object Wert)>
            {
                ("VorlageID", idVorlage),
                ("Bezeichnung", p.Feld.Bezeichnung),
                ("Kostenart", p.Feld.Kostenart),
                ("Bemessung", p.Bemessung),
                ("Satz", p.Satz),
                ("IstErloes", 0),
                ("Sortierung", p.Feld.Sortierung),
                ("IstPflicht", 0),
            };
            werte = werte.Where(w => zielspalten.Contains(w.Spalte)).ToList();
            v.Ausfuehren("INSERT INTO \"" + TAB_KOSTENVORLAGEPOSITION + "\" (" +
                         string.Join(", ", werte.Select(w => "\"" + w.Spalte + "\"")) + ") VALUES (" +
                         string.Join(", ", werte.Select(_ => "?")) + ")",
                         werte.Select((w, i) => new DbParam("@w" + i, w.Wert)).ToArray());
            return 1;
        }

        // --------------------------------------------------------------- Befunde ---

        /// <summary>
        /// Was an jeder Zeile der Tabelle zu tun wäre — je Satz und Kategorie ein Befund. Fehlt eine
        /// Spalte, zählt sie nicht; fehlt die Verweisspalte, liefert die Tabelle keinen Befund.
        /// </summary>
        internal static IReadOnlyList<Satzbefund> Befunde(Katalogkostengewerk g, string tabelle)
        {
            var leer = new List<Satzbefund>();
            if (!DataRepository.TabelleVorhanden(tabelle)) return leer;
            var felder = g.Felder.Where(f => DataRepository.SpalteVorhanden(tabelle, f.Spalte)).ToList();
            if (felder.Count == 0) return leer;
            var kategorien = felder.Select(f => f.Kategorie).Distinct()
                                   .Where(k => DataRepository.SpalteVorhanden(tabelle, Verweisspalte(k))).ToList();
            if (kategorien.Count == 0) return leer;

            var spalten = new List<string> { "ID" };
            if (DataRepository.SpalteVorhanden(tabelle, "Bezeichner")) spalten.Add("Bezeichner");
            foreach (Katalogkostenfeld f in felder)
            {
                spalten.Add(f.Spalte);
                if (!string.IsNullOrEmpty(f.Bezugsspalte) && DataRepository.SpalteVorhanden(tabelle, f.Bezugsspalte))
                    spalten.Add(f.Bezugsspalte);
                if (!string.IsNullOrEmpty(f.Einheitspalte) && DataRepository.SpalteVorhanden(tabelle, f.Einheitspalte))
                    spalten.Add(f.Einheitspalte);
            }
            foreach (int k in kategorien) spalten.Add(Verweisspalte(k));

            DataTable dt = DataRepository.GetDataTable(
                "SELECT " + string.Join(", ", spalten.Distinct(StringComparer.Ordinal)
                                                     .Select(s => "\"" + s + "\"")) +
                " FROM \"" + tabelle + "\"");
            if (dt == null) return leer;

            var liste = new List<Satzbefund>();
            foreach (DataRow r in dt.Rows)
            {
                foreach (int kategorie in kategorien)
                {
                    var b = new Satzbefund
                    {
                        Id = Ganz(r, "ID"),
                        Name = Text(r, "Bezeichner"),
                        Kategorie = kategorie,
                        Verweis = Ganz(r, Verweisspalte(kategorie)),
                    };
                    foreach (Katalogkostenfeld f in felder.Where(x => x.Kategorie == kategorie)
                                                          .OrderBy(x => x.Sortierung))
                    {
                        double? wert = Zahl(r, f.Spalte);
                        if (!wert.HasValue || wert.Value <= 0) continue;
                        double satz = wert.Value;
                        if (!string.IsNullOrEmpty(f.Bezugsspalte))
                        {
                            double? bezug = Zahl(r, f.Bezugsspalte);
                            double groesse = bezug.HasValue ? bezug.Value / f.Bezugsteiler : 0;
                            if (groesse <= 0)
                            {
                                b.OhneBezug.Add(f.Spalte + " ohne " + f.Bezugsspalte +
                                                " - keine Position, der Kostenwert bleibt stehen");
                                continue;
                            }
                            satz = wert.Value / groesse;
                        }
                        b.Positionen.Add(new Positionssaat
                        {
                            Feld = f,
                            Bemessung = BemessungVon(f, r),
                            Satz = satz,
                        });
                    }
                    if (b.Positionen.Count > 0 || b.OhneBezug.Count > 0) liste.Add(b);
                }
            }
            return liste;
        }

        /// <summary>
        /// Die Bemessung einer Position — fest, oder (Kessel-Wartung) nach dem Zeitbezug der
        /// Einheitspalte: <c>€/a</c> Jahresbetrag, <c>€/kWh</c> je kWh thermisch, <c>%/a</c> Prozent
        /// der Investition.
        /// </summary>
        private static string BemessungVon(Katalogkostenfeld f, DataRow r)
        {
            if (string.IsNullOrEmpty(f.Einheitspalte)) return f.Bemessung;
            string einheit = Text(r, f.Einheitspalte);
            if (string.Equals(einheit, DbWerte.KESSEL_WARTUNG_EINHEIT_ARBEIT, StringComparison.Ordinal))
                return DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH;
            if (string.Equals(einheit, DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT, StringComparison.Ordinal))
                return DbWerte.BEMESSUNG_PROZENT_INVESTITION;
            return f.Bemessung;
        }

        private static bool Hat(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value;

        private static int Ganz(DataRow r, string spalte)
            => Hat(r, spalte) ? Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture) : 0;

        private static double? Zahl(DataRow r, string spalte)
        {
            if (!Hat(r, spalte)) return null;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        private static string Text(DataRow r, string spalte)
            => Hat(r, spalte) ? (Convert.ToString(r[spalte], CultureInfo.InvariantCulture) ?? "") : "";

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
