using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE KOSTENSPALTEN DER INVESTITIONSGEWERKE ENTFALLEN - Welle KB-4 (Anwenderentscheid
    // 13.10.2026: "eine Kostenbasis, keine Doppelhaltung").
    //
    // WOZU. Acht Geraetekataloge und ihre Projektkopien fuehrten je eigene Kostenspalten -
    // Kosten_Modul samt vier Nebenposten und Wartungskosten_kwhel am BHKW,
    // Investitionskosten und Wartungskosten(_Einheit) am Heizkessel, Modulkosten an
    // Waermepumpe, Photovoltaik und Kaeltemaschine, Investitionskosten an Solarkollektoren
    // und Pufferspeicher, Kosten am Wechselrichter. Derselbe Kostensatz stand damit zweimal:
    // am Geraet und in der Position der Anlage, und kein Mechanismus hielt beide zusammen.
    // Welle KB-3 hat die eine Basis gebaut (Satzvorlagen des zentralen Kostenkatalogs, je
    // Katalogsatz eine Vorlage je Art, verwiesen ueber ID_KostenVorlage und
    // ID_KostenVorlageInvestition) und sie aus den Spalten GESAET. Dieser Schritt nimmt die
    // Spalten fort - die Satzvorlage ist danach die einzige Quelle.
    //
    // WAS, IN DIESER REIHENFOLGE:
    //   1. DDL: ID_KostenVorlageInvestition an Tab_Wechselrichter und
    //      Tab_Wechselrichter_STAMM (nullbar, ON DELETE SET NULL wie an den uebrigen acht).
    //      NUR die Investitionsseite: Der Wechselrichter fuehrt mit Kosten genau eine
    //      Kostenspalte, und sie ist eine Investition. Ohne Betriebskostenspalte entsteht
    //      keine Betriebsvorlage, also braucht er auch deren Verweis nicht - eine leere
    //      Spalte waere eine Zusage, die nichts einloest.
    //   2. SAAT des Wechselrichters ueber die Maschinerie von KB-3
    //      (KatalogkostenSaatSchema.Saeen mit der Gewerkliste dieser Welle): Kosten geteilt
    //      durch P_AC_Nenn wird EUR_PRO_KW_ELEKTRISCH - dieselbe Bauform wie bei der
    //      Kaeltemaschine (Konzept Wirtschaftlichkeit 3.2), aber mit der Art, die das Gewerk
    //      der Vorlage (Photovoltaik) kennt: „je kW Leistung" fuehrt die Photovoltaik nicht
    //      (BemessungKatalog.PasstZuGewerk), die Nennleistung des Wechselrichters ist eine
    //      elektrische Leistung. Satz x (P_AC_Nenn x Stueckzahl)
    //      ergibt wieder Stueckpreis x Stueckzahl, also dieselbe Zahl, die
    //      TechnikPlanwertCtrl heute zeigt.
    //   3. SICHERHEITSLAUF der Saat aller Gewerke dieser Welle (idempotent; ein zweiter Lauf
    //      traegt 0 nach) und die WACHE davor: Traegt nach der Saat noch eine Zeile einen
    //      Kostenwert > 0 OHNE Verweis, bricht der Schritt BENANNT ab und laesst die Spalten
    //      stehen. Das trifft auch den Fall OHNE BEZUGSGROESSE (ein Stueckpreis ohne
    //      Modulleistung, Aperturflaeche, Nennkaelteleistung oder P_AC_Nenn): KB-3 laesst
    //      ihn absichtlich ungesaet stehen - hier waere er ein stiller Datenverlust.
    //   4. DROP COLUMN der 32 Kostenspalten an den acht Katalogen und ihren Projektkopien.
    //   5. NEUBILDUNG der Katalogpruefsummen je betroffenem Katalog nach dem Muster von
    //      Schritt 227.
    //
    // INVESTITION_KWEL FAELLT MIT. Der Wert ist seit dem Nutzerentscheid vom 22.08.2026 aus
    // Kosten_Modul und den vier Nebenposten ABGELEITET (BHKWKosten) und deshalb von KB-3
    // bewusst NICHT gesaet - eine zweite Position daraus waere dieselbe Zahl ein zweites Mal.
    // Mit seiner Quelle entfaellt er; abgeleitet wird er kuenftig aus der Satzvorlage.
    //
    // WARTUNGSKOSTEN_EINHEIT FAELLT MIT. Die Spalte trug den Zeitbezug der Kessel-Wartung
    // (EUR/a, EUR/kWh, %/a). Die Saat hat ihn in die BEMESSUNG der Position uebersetzt
    // (JAHRESBETRAG, EUR_PRO_KWH_THERMISCH, PROZENT_INVESTITION); er steht damit weiter
    // geschrieben, nur an der einen Stelle, die ihn rechnet.
    //
    // NICHT IN DIESER WELLE: Tab_Stromspeicher(_STAMM) mit Modulkosten, Leistungskosten,
    // Investition_Fix und Verschleisskosten (Welle KB-5 - sein RECHENWEG liest die Spalten,
    // und die Flotte des Referenzprojekts 1046 haengt daran: neue Basis), und
    // Tab_Rueckkuehlwerk(_STAMM).Modulkosten (von KB-3 nicht gesaet, kein Leser, keine Zeile).
    //
    // KEIN TABELLENNEUBAU. SQLite kann DROP COLUMN seit 3.35. Keine der 32 Spalten steht
    // unter einem Index, in einem Fremdschluessel, einer generierten Spalte, einem Trigger,
    // einer Sicht oder einer Spalten-CHECK-Bedingung; dass Tabellen STRICT sind, aendert
    // daran nichts (Muster BrennstoffpreiseTraegerkarteSchema, KwkgProjektaltspalten).
    //
    // DIE NAMEN STEHEN NUR HIER, UND NUR ALS ARGUMENT. Gegen eine bereits migrierte
    // Datenbank gehalten waere eine ausgeschriebene Anweisung zwangslaeufig "no such column"
    // - und genau so haelt sie Werkzeuge/SqlDialektPruefer (Muster StrompreisAltspalten).
    //
    // DIE DDL FRUEHERER SCHRITTE BLEIBT, WIE SIE IST. Ein Schemaschritt ist Geschichte und
    // gibt den Stand seiner Zeit wieder; eine Datenbank, die sie nacheinander laeuft, legt
    // die Spalten an und entfernt sie wieder - richtig und folgenlos.
    //
    // PRUEFSUMME UND KATALOGFASSUNG. Die Kostenspalten standen in der Fachspaltenliste ihres
    // Katalogs (Katalogfassung) und trugen - auch leer - je eine Zeile zur Pruefsumme bei.
    // Mit ihrem Entfall aendert sich die Pruefsumme jedes ausgelieferten Satzes der acht
    // Kataloge. Damit ein unveraenderter Satz nicht als "vom Anwender geaendert" gilt, bildet
    // der Schritt die Pruefsumme jedes Satzes neu, dessen gespeicherte Summe VOR dem Entfall
    // stimmte; ein geaenderter Satz bleibt als geaendert erkennbar. Die ALTE Spaltenfolge
    // steht dafuer allein hier (ALTE_FOLGE: je Katalog die entfallende Spalte und ihr Platz
    // in der alten Folge) - das Register fuehrt sie nicht mehr.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg der acht Gewerke liest eine dieser Spalten: Investition
    // und Betrieb kommen aus Tab_ProjektWerte (InvestKaskade.Lies). Gelesen haben sie der
    // PLANWERT des Kostendialogs (TechnikPlanwertCtrl - liest jetzt die Satzvorlage), die
    // Katalogdialoge und Steckbriefe, die Feldkataloge der KI und der Vorlagenfeldkatalog der
    // Word-Berichte; die berechnete Word-Katalogfassung bleibt unveraendert, nur die Quelle
    // der Kosten-Platzhalter wechselt. Der Referenzlauf bleibt byte-gleich.
    //
    // NUMMER. 228 - angemeldet fuer KB-4, haengt ueber
    // BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1 an 227.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung
    // in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>KB-4</b> — die Kostenspalten der Investitionsgewerke entfallen: Verweis und Saat für den
    /// Wechselrichter, Sicherheitslauf der Saat, Entfall der 32 Spalten, Neubildung der
    /// Katalogprüfsummen. EINE Quelle für Migration, Werkzeug, Paketanhebung und Nachweis.
    /// </summary>
    public static class GeraetekostenEntfallSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Sie hängt an den
        /// Brennstoffpreisen (<see cref="BrennstoffpreiseTraegerkarteSchema.SCHRITT"/>, KB-2).
        /// </summary>
        public const int SCHRITT = BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1;

        /// <summary>Der Auslieferungskatalog der Wechselrichter.</summary>
        public const string TAB_WR_STAMM = "Tab_Wechselrichter_STAMM";

        /// <summary>Die Projektkopie der Wechselrichter.</summary>
        public const string TAB_WR = "Tab_Wechselrichter";

        /// <summary>Die Kostenspalte des Wechselrichters — ein Stückpreis je Gerät.</summary>
        public const string SPALTE_WR_KOSTEN = "Kosten";

        /// <summary>Die Bezugsgröße EINES Wechselrichters: seine Nennscheinleistung in kW.</summary>
        public const string SPALTE_WR_LEISTUNG = WechselrichterSchema.SPALTE_P_AC_NENN;

        /// <summary>Die abgeleitete Spalte des BHKW, die mit ihrer Quelle entfällt.</summary>
        public const string SPALTE_BHKW_INVESTITION_KWEL = "Investition_kwel";

        /// <summary>Die Kostenkomponente, unter der die Wechselrichter-Satzvorlage steht (Photovoltaik).</summary>
        public const int KOMPONENTE_WECHSELRICHTER = 3;

        /// <summary>
        /// <b>Das Gewerk Wechselrichter</b> — der Nachzug zu den acht Gewerken von KB-3. Die Bemessung
        /// ist die der Kältemaschine: ein Stückpreis, geteilt durch die Baugröße EINES Stücks. Die Art
        /// ist „je kW elektrisch", nicht „je kW Leistung": Die Satzvorlage steht unter der
        /// Photovoltaik, und dort führt allein die elektrische Leistung eine Bezugsgröße — eine
        /// gewerksfremde Art stünde in der Kostenverwaltung ohne Basis da. Die Bezugsgröße des
        /// Satzes bleibt die Nennleistung EINES Geräts (<see cref="SPALTE_WR_LEISTUNG"/>).
        /// </summary>
        public static readonly Katalogkostengewerk GEWERK_WECHSELRICHTER =
            new Katalogkostengewerk(TAB_WR_STAMM, TAB_WR, KOMPONENTE_WECHSELRICHTER, new[]
            {
                new Katalogkostenfeld(SPALTE_WR_KOSTEN, DbWerte.KOSTEN_KATEGORIE_INVESTITION,
                                      DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, BEZEICHNUNG_WECHSELRICHTER,
                                      DbWerte.KOSTENART_KAPITALGEBUNDEN, 10,
                                      bezugsspalte: SPALTE_WR_LEISTUNG),
            });

        /// <summary>
        /// Die gespeicherte Bezeichnung der Wechselrichterposition (Katalogtext, deutsch — wortgleich mit
        /// der Position der Standardvorlage der Photovoltaik, damit der Abgleich sie findet).
        /// </summary>
        public const string BEZEICHNUNG_WECHSELRICHTER = "Wechselrichter";

        /// <summary>
        /// <b>Die acht Gewerke dieser Welle</b>: die sieben von KB-3 ohne den Stromspeicher (Welle KB-5)
        /// und der Wechselrichter. Aus ihnen entstehen Saat, Wache und Entfallliste.
        /// </summary>
        public static readonly IReadOnlyList<Katalogkostengewerk> GEWERKE =
            KatalogkostenSaatSchema.GEWERKE
                .Where(g => !string.Equals(g.Kopie, "Tab_Stromspeicher", StringComparison.Ordinal))
                .Concat(new[] { GEWERK_WECHSELRICHTER })
                .ToArray();

        /// <summary>Die zwei Verweisspalten des DDL-Teils: die Investitionsvorlage an Katalog und Projektkopie.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_WR_STAMM, KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION, Verweistyp()),
            (TAB_WR, KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION, Verweistyp()),
        };

        private static string Verweistyp()
            => "INTEGER REFERENCES \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGE + "\" (\"ID\") ON DELETE SET NULL";

        /// <summary>
        /// <b>Die 32 entfallenden Spalten in fester Folge</b>: je Gewerk zuerst der Katalog, dann die
        /// Projektkopie. Die Liste ist die der Saat (<see cref="Katalogkostengewerk.Felder"/> samt
        /// Einheitspalte) plus die abgeleitete BHKW-Spalte — sie kann deshalb nicht von der Saat
        /// abweichen.
        /// </summary>
        public static IEnumerable<(string Tabelle, string Spalte)> Entfall
        {
            get
            {
                foreach (Katalogkostengewerk g in GEWERKE)
                    foreach (string tabelle in new[] { g.Katalog, g.Kopie })
                    {
                        foreach (string spalte in Kostenspalten(g))
                            yield return (tabelle, spalte);
                    }
            }
        }

        /// <summary>
        /// Die Kostenspalten EINES Gewerks: die Spalte je Saatfeld, die Einheitspalte, die den Zeitbezug
        /// trug, und am BHKW die abgeleitete <c>Investition_kwel</c>.
        /// </summary>
        public static IReadOnlyList<string> Kostenspalten(Katalogkostengewerk g)
        {
            var liste = new List<string>();
            foreach (Katalogkostenfeld f in g.Felder)
            {
                if (!liste.Contains(f.Spalte, StringComparer.Ordinal)) liste.Add(f.Spalte);
                if (!string.IsNullOrEmpty(f.Einheitspalte) &&
                    !liste.Contains(f.Einheitspalte, StringComparer.Ordinal)) liste.Add(f.Einheitspalte);
            }
            if (string.Equals(g.Katalog, "Tab_BHKW_STAMM", StringComparison.Ordinal))
                liste.Add(SPALTE_BHKW_INVESTITION_KWEL);
            return liste;
        }

        /// <summary>
        /// <b>Die alte Fachspaltenfolge je Katalog</b> — je entfallende Spalte ihr Platz in der Folge, mit
        /// der die gespeicherten Prüfsummen gebildet wurden. Das Register führt die Spalten nicht mehr,
        /// deshalb steht die alte Folge allein hier (Kopfblock, Abschnitt „Prüfsumme und Katalogfassung").
        /// Dass die Plätze stimmen, weist die Wache an den gespeicherten Prüfsummen der Testdatenbank nach.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Spalte, int Platz)>> ALTE_FOLGE =
            new Dictionary<string, IReadOnlyList<(string Spalte, int Platz)>>(StringComparer.Ordinal)
            {
                ["Tab_WP_STAMM"] = new[] { ("Modulkosten", 10) },
                ["Tab_Heizkessel_STAMM"] = new[]
                {
                    ("Investitionskosten", 7), ("Wartungskosten", 9), ("Wartungskosten_Einheit", 20),
                },
                ["Tab_BHKW_STAMM"] = new[]
                {
                    (SPALTE_BHKW_INVESTITION_KWEL, 7), ("Wartungskosten_kwhel", 9), ("Kosten_Modul", 18),
                    ("Kosten_Montage", 19), ("Kosten_Lieferung", 20), ("Kosten_Schallschutzhaube", 21),
                    ("Kosten_Abgasreinigung", 22),
                },
                ["Tab_PV_STAMM"] = new[] { ("Modulkosten", 15) },
                ["Tab_Pufferspeicher_STAMM"] = new[] { ("Investitionskosten", 5) },
                ["Tab_Solarkollektoren_STAMM"] = new[] { ("Investitionskosten", 11) },
                [TAB_WR_STAMM] = new[] { (SPALTE_WR_KOSTEN, 23) },
                [KaeltemaschineSchema.TAB_STAMM] = new[] { (KaeltemaschineSchema.SPALTE_MODULKOSTEN, 11) },
            };

        /// <summary>Was ein Lauf getan hat: angelegte Verweisspalten, gesäte Vorlagen, entfernte Spalten, neue Prüfsummen.</summary>
        public readonly record struct Laufergebnis(int Verweisspalten, int Gesaet, int Entfernt, int PruefsummenNeu)
        {
            /// <summary>Die Summe der Handgriffe — die Zahl, die die vier Leser melden.</summary>
            public int Handgriffe => Verweisspalten + Gesaet + Entfernt + PruefsummenNeu;
        }

        /// <summary>Die Tabellen, ohne die der Schritt nicht laufen kann.</summary>
        public static IEnumerable<string> Voraussetzungen()
            => GEWERKE.Select(g => g.Katalog)
                      .Concat(new[] { KatalogkostenSaatSchema.TAB_KOSTENVORLAGE,
                                      KatalogkostenSaatSchema.TAB_KOSTENVORLAGEPOSITION })
                      .Distinct(StringComparer.Ordinal).ToArray();

        /// <summary>Steht diese Spalte noch?</summary>
        public static bool Vorhanden(string tabelle, string spalte) =>
            DataRepository.TabelleVorhanden(tabelle) && DataRepository.SpalteVorhanden(tabelle, spalte);

        /// <summary>Wie viele der 32 Spalten stehen noch? 0 = der Schritt ist gelaufen.</summary>
        public static int Offen() => Entfall.Count(s => Vorhanden(s.Tabelle, s.Spalte));

        /// <summary>Steht der Schritt vollständig (keine Kostenspalte mehr da, die Verweise angelegt)?</summary>
        public static bool Vollstaendig()
            => Offen() == 0 && SPALTEN.All(s => Vorhanden(s.Tabelle, s.Spalte));

        /// <summary>
        /// <b>Die Wache vor dem Entfall</b>: die Zeilen, die einen Kostenwert &gt; 0 tragen und KEINEN
        /// Verweis auf eine Satzvorlage — je Zeile eine Meldung. Leer heißt: Jeder gepflegte Kostenwert
        /// steht in einer Satzvorlage, der Entfall verliert nichts.
        /// </summary>
        public static IReadOnlyList<string> OhneVerweis()
        {
            var offen = new List<string>();
            foreach (Katalogkostengewerk g in GEWERKE)
                foreach (string tabelle in new[] { g.Katalog, g.Kopie })
                    foreach (KatalogkostenSaatSchema.Satzbefund b in KatalogkostenSaatSchema.Befunde(g, tabelle))
                    {
                        if (b.Verweis != 0) continue;
                        if (b.Positionen.Count == 0 && b.OhneBezug.Count == 0) continue;
                        offen.Add(tabelle + " ID " + b.Id.ToString(CultureInfo.InvariantCulture) +
                                  (b.OhneBezug.Count > 0 ? ": " + string.Join(", ", b.OhneBezug)
                                                         : ": Kostenwert ohne Satzvorlage"));
                    }
            return offen;
        }

        /// <summary>
        /// Führt den Schritt aus: Verweisspalten, Saat, Wache, Entfall, Prüfsummen.
        /// Rückgabe: die Zahl der Handgriffe. <b>Wiederholbar.</b>
        /// </summary>
        /// <param name="bericht">Je Handgriff eine Zeile; darf <c>null</c> sein.</param>
        public static int Ausfuehren(IList<string> bericht) => Lauf(bericht).Handgriffe;

        /// <summary>
        /// <see cref="Ausfuehren"/> mit der Aufschlüsselung des Laufs — der Weg des Nachweises. Bricht
        /// <b>benannt</b> ab, wenn nach der Saat noch ein Kostenwert ohne Satzvorlage steht; die Spalten
        /// bleiben dann, wie sie sind.
        /// </summary>
        public static Laufergebnis Lauf(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");

            int verweise = VerweisspaltenAnlegen(bericht);
            int gesaet = KatalogkostenSaatSchema.Saeen(bericht, GEWERKE);

            IReadOnlyList<string> offen = OhneVerweis();
            if (offen.Count > 0)
                throw new InvalidOperationException(
                    "Schemaschritt " + Nr + ": " + offen.Count.ToString(CultureInfo.InvariantCulture) +
                    " Zeile(n) tragen einen Kostenwert ohne Satzvorlage; die Spalten bleiben stehen. " +
                    string.Join("; ", offen.Take(10)) + (offen.Count > 10 ? " …" : ""));

            Dictionary<string, List<long>> stimmig = StimmigeSaetze();
            int entfernt = Entfernen(bericht);
            int summen = PruefsummenNeuBilden(stimmig, bericht);
            bericht?.Add("Rechenweg unberuehrt: Investition und Betrieb kommen aus Tab_ProjektWerte; " +
                         "der Referenzlauf bleibt byte-gleich");
            return new Laufergebnis(verweise, gesaet, entfernt, summen);
        }

        // =================================================================================
        //  1. Die Verweisspalten des Wechselrichters
        // =================================================================================

        /// <summary>Die Anweisung, die eine Verweisspalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        private static int VerweisspaltenAnlegen(IList<string> bericht)
        {
            var offen = SPALTEN.Where(s => DataRepository.TabelleVorhanden(s.Tabelle) &&
                                           !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - der Investitionsverweis am Wechselrichter; nichts zu tun");
                return 0;
            }
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        bericht?.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return offen.Count;
        }

        // =================================================================================
        //  4. Der Entfall der 32 Spalten
        // =================================================================================

        private static int Entfernen(IList<string> bericht)
        {
            int n = 0;
            foreach ((string Tabelle, string Spalte) s in Entfall.ToList())
            {
                if (!Vorhanden(s.Tabelle, s.Spalte)) continue;
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
                n++;
                bericht?.Add(s.Tabelle + "." + s.Spalte + " entfernt");
            }
            if (n == 0) bericht?.Add("keine Kostenspalte mehr da; nichts zu tun");
            return n;
        }

        // =================================================================================
        //  5. Die Pruefsummen der acht Kataloge
        // =================================================================================

        /// <summary>
        /// Die Sicht eines Katalogs mit der ALTEN Spaltenfolge (<see cref="ALTE_FOLGE"/>, soweit die Spalten
        /// noch stehen) — die Folge, mit der die gespeicherten Prüfsummen gebildet wurden.
        /// <c>(null, null)</c>, wenn der Katalog nicht im Register steht oder die Kostenspalten schon fort sind.
        /// </summary>
        internal static (Katalogtabelle Tabelle, List<string> Spalten) AlteSicht(string katalog)
        {
            Katalogtabelle neu = Katalogfassung.Tabelle(katalog);
            if (neu == null || !Katalogfassung.SpaltenVorhanden(katalog)) return (null, null);
            IReadOnlyList<(string Spalte, int Platz)> plaetze;
            if (!ALTE_FOLGE.TryGetValue(katalog, out plaetze)) return (null, null);

            var alt = new List<string>(neu.Fachspalten);
            foreach ((string spalte, int platz) in plaetze.OrderBy(p => p.Platz))
                alt.Insert(Math.Min(platz, alt.Count), spalte);

            List<string> vorhanden = alt.Where(s => DataRepository.SpalteVorhanden(katalog, s)).ToList();
            if (!plaetze.Any(p => DataRepository.SpalteVorhanden(katalog, p.Spalte)))
                return (null, null);                       // die Kostenspalten sind schon fort

            var tAlt = new Katalogtabelle(neu.Tabelle, neu.Kuerzel, vorhanden.ToArray(), neu.Kinder.ToArray())
            {
                Stufe = neu.Stufe,
                Anzeigeschluessel = neu.Anzeigeschluessel,
                Verweise = neu.Verweise,
                Namensspalten = neu.Namensspalten,
            };
            return (tAlt, vorhanden);
        }

        /// <summary>
        /// Die Sätze je Katalog, deren gespeicherte Prüfsumme VOR dem Entfall stimmte — gebildet über
        /// <see cref="ALTE_FOLGE"/>. Nur sie bekommen danach eine neue Summe.
        /// </summary>
        internal static Dictionary<string, List<long>> StimmigeSaetze()
        {
            var je = new Dictionary<string, List<long>>(StringComparer.Ordinal);
            foreach (string katalog in ALTE_FOLGE.Keys)
            {
                var ids = new List<long>();
                (Katalogtabelle tAlt, List<string> alt) = AlteSicht(katalog);
                if (tAlt == null) { je[katalog] = ids; continue; }
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT \"ID\", \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\", " + Katalogfassung.Spaltentext(alt) +
                    " FROM \"" + katalog + "\" WHERE \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" IS NOT NULL");
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (string.Equals(Convert.ToString(r[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture),
                                          Katalogfassung.PruefsummeDerZeile(tAlt, alt, r, Katalogfassung.LeseOhneVorgang),
                                          StringComparison.Ordinal))
                            ids.Add(Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture));
                je[katalog] = ids;
            }
            return je;
        }

        /// <summary>Bildet die Prüfsumme der übergebenen Sätze mit der NEUEN Spaltenfolge neu.</summary>
        private static int PruefsummenNeuBilden(Dictionary<string, List<long>> je, IList<string> bericht)
        {
            int gesamt = 0;
            if (je == null) return 0;
            foreach (KeyValuePair<string, List<long>> paar in je)
            {
                if (paar.Value == null || paar.Value.Count == 0) continue;
                Katalogtabelle t = Katalogfassung.Tabelle(paar.Key);
                if (t == null || !Katalogfassung.SpaltenVorhanden(paar.Key)) continue;
                List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
                int n = 0;
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        Func<string, DbParam[], DataTable> lese = (sql, p) => v.Lese(sql, p);
                        foreach (long id in paar.Value)
                        {
                            DataTable dt = v.Lese("SELECT \"ID\", " + Katalogfassung.Spaltentext(spalten) +
                                                  " FROM \"" + paar.Key + "\" WHERE \"ID\" = ?",
                                                  new[] { new DbParam("@id", id) });
                            if (dt == null || dt.Rows.Count == 0) continue;
                            v.Ausfuehren("UPDATE \"" + paar.Key + "\" SET \"" + Katalogfassung.SPALTE_PRUEFSUMME +
                                         "\" = ? WHERE \"ID\" = ?",
                                         new DbParam("@summe", Katalogfassung.PruefsummeDerZeile(t, spalten, dt.Rows[0], lese)),
                                         new DbParam("@id", id));
                            n++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (n > 0)
                    bericht?.Add(n.ToString(CultureInfo.InvariantCulture) + " Pruefsumme(n) von " + paar.Key +
                                 " neu gebildet");
                gesamt += n;
            }
            return gesamt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
