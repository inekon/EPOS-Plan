using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE BRENNSTOFFPREISE WANDERN IN DIE TRAEGERKARTE - Welle KB-2 (Anwenderentscheid
    // 13.10.2026: "eine Kostenbasis, keine Doppelhaltung").
    //
    // WOZU. Tab_Brennstoff_Stamm und seine Projektkopie Tab_Brennstoff fuehrten je drei
    // Preisspalten: Standard_Arbeitspreis, Standard_Grundpreis, Standard_Leistungspreis.
    // KEIN Rechenweg liest sie. Ihr einziger Weg in die Welt war die SAAT einer
    // Energietraeger-Variante (EnergietraegerVarianteCtrl.Anlegen, WizardCtrl.TraegerSatzAnlegen):
    // Von dort trugen sie in energy_carrier, energy_price und energy_project_settings. Gerechnet
    // wird allein aus der Traegerwelt (KostenEmissionRechner: Projektwert custom_price_* vor
    // Katalogwert energy_carrier.price_*). Ein Preis am Stoff war damit eine zweite, stumme
    // Kostenbasis neben der Karte des Traegers - genau die Doppelhaltung, die der Entscheid
    // aufloest. Die Karte des Traegers ist kuenftig die einzige Quelle der Saat.
    //
    // WAS. Zwei Teile in einem Schritt:
    //   a. UEBERTRAG (DML): Je Traeger mit ID_Brennstoff > 0 und noch leerem Preis uebernimmt
    //      energy_carrier.price_work/price_base/price_power den Standardwert des verknuepften
    //      KATALOGBRENNSTOFFS, wenn dieser groesser als 0 ist. Ein gepflegter Traegerwert bleibt
    //      unangetastet. Mit einem uebertragenen Leistungspreis bekommt ein leerer
    //      price_power_modus den Zeitbezug JAHR (DbWerte.LEISTUNGSPREIS_MODUS_JAHR) - dieselbe
    //      Vorgabe, die der Rechenweg einem leeren Modus ohnehin gibt; so steht sie nach dem
    //      Uebertrag auch geschrieben.
    //   b. DROP COLUMN (DDL): Die drei Spalten fallen an Tab_Brennstoff_Stamm und Tab_Brennstoff.
    //
    // NUR DER KATALOG IST QUELLE, NIE DIE PROJEKTKOPIE. energy_carrier ist ein Katalog ohne
    // Projektbezug (keine Spalte ID_Projekt; die Projektebene des Traegers ist
    // energy_project_settings). Ein Katalog kann deshalb nur aus einem Katalog erben. Die Werte
    // der Projektkopien gehen NICHT in energy_project_settings.custom_price_*: Diese Spalte ist
    // die OBERSTE Ebene der Lesekette: Ein Uebertrag dorthin wuerde Kosten eines jeden Projekts
    // mit abweichender Kopie verschieben - eine Saat darf nie ein Ergebnis bewegen (dieselbe
    // Begruendung wie beim Emissionsentscheid vom 30.08.2026 in WizardCtrl).
    //
    // IDEMPOTENT. Der Uebertrag greift nur, wo der Traegerpreis 0 oder leer ist; nach dem Lauf
    // steht dort der uebernommene Wert, der zweite Lauf findet nichts mehr. Danach sind die
    // Spalten fort, und der Schritt hat nichts mehr zu tun (Offen() = 0).
    //
    // PRUEFSUMME UND KATALOGFASSUNG. Die drei Spalten standen in der Fachspaltenliste des
    // Brennstoffkatalogs (Katalogfassung, Register "BRS") und trugen - auch mit dem Wert 0 - je
    // eine Zeile zur Pruefsumme bei. Mit ihrem Entfall aendert sich die Pruefsumme JEDES
    // ausgelieferten Brennstoffs. Damit ein unveraenderter Satz nicht als "vom Anwender
    // geaendert" gilt, bildet der Schritt die Pruefsumme jedes Satzes neu, dessen gespeicherte
    // Summe VOR dem Entfall stimmte (Muster KaelteKatalogfelderSchema, Schritt 211) - ein
    // geaenderter Satz bleibt als geaendert erkennbar. Die ALTE Spaltenfolge steht dafuer allein
    // hier (ALTSPALTEN): Das Register fuehrt sie nicht mehr. Die naechste Auslieferung traegt
    // damit eine neue Katalogfassung (Setup-Kette, --katalogfassung als JJJJMMTTnn) - die
    // Pruefsummen des Pakets sind nicht die der Vorgaengerfassung.
    //
    // KEIN TABELLENNEUBAU. SQLite kann DROP COLUMN seit 3.35. Keine der sechs Spalten steht
    // unter einem Index, in einem Fremdschluessel, einer generierten Spalte, einem Trigger, einer
    // Sicht oder einer Spalten-CHECK-Bedingung; beide Tabellen sind STRICT, das aendert daran
    // nichts (Muster KwkgProjektaltspalten, SolarkollektorTemperaturen).
    //
    // DIE NAMEN STEHEN NUR HIER, UND NUR ALS ARGUMENT. Gegen eine bereits migrierte Datenbank
    // gehalten waere eine ausgeschriebene Anweisung zwangslaeufig "no such column" - und genau so
    // haelt sie Werkzeuge/SqlDialektPruefer (Muster StrompreisAltspalten, Schritt 85).
    //
    // DIE DDL DES SCHRITTS 175 BLEIBT, WIE SIE IST. ProjektkopienKatalogeSchema legt
    // Tab_Brennstoff mit den drei Spalten an; ein Schemaschritt ist Geschichte und gibt den Stand
    // seiner Zeit wieder. Eine Datenbank, die 175 und 227 nacheinander laeuft, legt sie an und
    // entfernt sie wieder - richtig und folgenlos.
    //
    // ERGEBNISNEUTRAL. Der Referenzlauf rechnet keine Kostengroesse; in der Testdatenbank sind
    // alle sechs Spalten durchweg 0 (25 Katalogsaetze, 1 100 Projektkopien), ebenso price_work,
    // price_base und price_power aller 27 Traeger - der Uebertrag ist dort leer.
    //
    // NUMMER. 227 - angemeldet fuer KB-2, haengt ueber KatalogkostenSaatSchema.SCHRITT + 1 an 226.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>KB-2</b> — die Preisspalten der Brennstoffe wandern in die Trägerkarte: Übertrag nach
    /// <c>energy_carrier.price_*</c>, dann Entfall der drei Spalten an Katalog und Projektkopie.
    /// EINE Quelle für Migration, Werkzeug, Paketanhebung und Nachweis.
    /// </summary>
    public static class BrennstoffpreiseTraegerkarteSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Sie hängt an der
        /// Saat der Katalogkosten (<see cref="KatalogkostenSaatSchema.SCHRITT"/>, KB-3).
        /// </summary>
        public const int SCHRITT = KatalogkostenSaatSchema.SCHRITT + 1;

        /// <summary>Der Auslieferungskatalog der Brennstoffe.</summary>
        public const string TAB_STAMM = "Tab_Brennstoff_Stamm";

        /// <summary>Die Projektkopie der Brennstoffe (Schemaschritt 175).</summary>
        public const string TAB_PROJEKT = ProjektkopienKatalogeSchema.TAB_BRENNSTOFF;

        /// <summary>Der zentrale Trägerkatalog — das Ziel des Übertrags.</summary>
        public const string TAB_TRAEGER = "energy_carrier";

        /// <summary>Die Spalte des Trägers, die den Zeitbezug des Leistungspreises trägt.</summary>
        public const string SPALTE_MODUS = "price_power_modus";

        /// <summary>Der Verweis des Trägers auf die Brennstoffart.</summary>
        public const string SPALTE_VERWEIS = "ID_Brennstoff";

        /// <summary>
        /// Die drei Preispaare in fester Folge: die Spalte der Trägerkarte und die entfallende
        /// Standardspalte des Brennstoffs. Aus ihr entstehen Übertrag, Zählung und Nachweis.
        /// </summary>
        public static readonly IReadOnlyList<(string Traegerspalte, string Standardspalte)> PAARE = new[]
        {
            ("price_work", "Standard_Arbeitspreis"),
            ("price_base", "Standard_Grundpreis"),
            ("price_power", "Standard_Leistungspreis"),
        };

        /// <summary>
        /// Die ALTE Fachspaltenfolge des Brennstoffkatalogs — die Folge, mit der die gespeicherten
        /// Prüfsummen gebildet wurden. Das Register führt die drei Preisspalten nicht mehr, deshalb
        /// steht die alte Folge allein hier (Kopfblock, Abschnitt „Prüfsumme und Katalogfassung").
        /// </summary>
        public static readonly IReadOnlyList<string> ALTSPALTEN = new[]
        {
            "ID_Kategorie", "Bezeichner", "Einheit", "PreisEinheit", "Hi", "Hs", "CO2", "SO2", "NOx", "Staub",
            "PE_Faktor", "Standard_Grundpreis", "Standard_Arbeitspreis", "Standard_Leistungspreis"
        };

        /// <summary>Was ein Lauf getan hat: übertragene Preise, entfernte Spalten, neu gebildete Prüfsummen.</summary>
        public readonly record struct Laufergebnis(int Uebertragen, int Entfernt, int PruefsummenNeu)
        {
            /// <summary>Die Summe der Handgriffe — die Zahl, die die vier Leser melden.</summary>
            public int Handgriffe => Uebertragen + Entfernt + PruefsummenNeu;
        }

        /// <summary>Die Tabellen, ohne die der Schritt nicht laufen kann.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return TAB_STAMM;
            yield return TAB_TRAEGER;
        }

        /// <summary>
        /// Die sechs Spalten in fester Folge: Tabelle und Name. Zuerst der Katalog, dann die
        /// Projektkopie — dieselbe Liste gehen Anweisung, Zählung und Nachweis durch.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Spalten
        {
            get
            {
                foreach ((string _, string standardspalte) in PAARE)
                    yield return new KeyValuePair<string, string>(TAB_STAMM, standardspalte);
                foreach ((string _, string standardspalte) in PAARE)
                    yield return new KeyValuePair<string, string>(TAB_PROJEKT, standardspalte);
            }
        }

        /// <summary>Steht diese Spalte noch?</summary>
        public static bool Vorhanden(string tabelle, string spalte) =>
            DataRepository.TabelleVorhanden(tabelle) && DataRepository.SpalteVorhanden(tabelle, spalte);

        /// <summary>Wie viele der sechs Spalten stehen noch? 0 = der Schritt ist gelaufen.</summary>
        public static int Offen()
        {
            int n = 0;
            foreach (KeyValuePair<string, string> s in Spalten)
                if (Vorhanden(s.Key, s.Value)) n++;
            return n;
        }

        /// <summary>Steht der Schritt vollständig (keine der sechs Spalten mehr da)?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Wie viele Preise würde der Übertrag jetzt setzen? Je Paar ein Zähler, aufsummiert; 0,
        /// sobald die Standardspalten fort sind.
        /// </summary>
        public static int Uebertragbar()
        {
            if (!Vorhanden(TAB_STAMM, PAARE[0].Standardspalte) && !Vorhanden(TAB_STAMM, PAARE[1].Standardspalte)
                && !Vorhanden(TAB_STAMM, PAARE[2].Standardspalte)) return 0;
            int n = 0;
            foreach ((string traegerspalte, string standardspalte) in PAARE)
            {
                if (!Vorhanden(TAB_STAMM, standardspalte)) continue;
                object o = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + Q(TAB_TRAEGER) + " WHERE " +
                                                        Bedingung(traegerspalte, standardspalte));
                if (o != null && o != DBNull.Value) n += Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            return n;
        }

        /// <summary>
        /// Führt den Schritt aus: Übertrag, Entfall der Spalten, Neubildung der Prüfsummen.
        /// Rückgabe: die Zahl der Handgriffe. <b>Wiederholbar.</b>
        /// </summary>
        /// <param name="bericht">Je Handgriff eine Zeile; darf <c>null</c> sein.</param>
        public static int Ausfuehren(IList<string> bericht) => Lauf(bericht).Handgriffe;

        /// <summary>
        /// <see cref="Ausfuehren"/> mit der Aufschlüsselung des Laufs — der Weg des Nachweises.
        /// </summary>
        public static Laufergebnis Lauf(IList<string> bericht)
        {
            int uebertragen = Uebertragen(bericht);
            List<long> stimmig = StimmigeSaetze();
            int entfernt = Entfernen(bericht);
            int summen = PruefsummenNeuBilden(stimmig);
            if (bericht != null && summen > 0)
                bericht.Add(summen.ToString(CultureInfo.InvariantCulture) + " Pruefsumme(n) des Brennstoffkatalogs neu gebildet");
            return new Laufergebnis(uebertragen, entfernt, summen);
        }

        // =================================================================================
        //  a. Der Uebertrag in die Traegerkarte
        // =================================================================================

        /// <summary>
        /// Die Bedingung des Übertrags: ein Träger mit Brennstoffverweis, leerem Preis und einem
        /// Katalogwert über 0.
        /// </summary>
        private static string Bedingung(string traegerspalte, string standardspalte) =>
            "IFNULL(" + Q(SPALTE_VERWEIS) + ", 0) > 0 AND IFNULL(" + Q(traegerspalte) + ", 0) = 0 " +
            "AND IFNULL(" + Unterabfrage(standardspalte) + ", 0) > 0";

        /// <summary>Der Standardwert des verknüpften Katalogbrennstoffs als Unterabfrage.</summary>
        private static string Unterabfrage(string standardspalte) =>
            "(SELECT b." + Q(standardspalte) + " FROM " + Q(TAB_STAMM) + " AS b WHERE b." + Q("ID") + " = " +
            Q(TAB_TRAEGER) + "." + Q(SPALTE_VERWEIS) + ")";

        private static int Uebertragen(IList<string> bericht)
        {
            int n = 0;
            foreach ((string traegerspalte, string standardspalte) in PAARE)
            {
                if (!Vorhanden(TAB_STAMM, standardspalte)) continue;
                bool mitModus = string.Equals(traegerspalte, PAARE[2].Traegerspalte, StringComparison.Ordinal) &&
                                DataRepository.SpalteVorhanden(TAB_TRAEGER, SPALTE_MODUS);
                string sql = "UPDATE " + Q(TAB_TRAEGER) + " SET " + Q(traegerspalte) + " = " + Unterabfrage(standardspalte) +
                             (mitModus
                                 ? ", " + Q(SPALTE_MODUS) + " = IIF(IFNULL(" + Q(SPALTE_MODUS) + ", '') = '', ?, " + Q(SPALTE_MODUS) + ")"
                                 : "") +
                             " WHERE " + Bedingung(traegerspalte, standardspalte);
                int k = mitModus
                    ? DataRepository.ExecuteNonQuery(sql, new DbParam("@modus", DbWerte.LEISTUNGSPREIS_MODUS_JAHR))
                    : DataRepository.ExecuteNonQuery(sql);
                if (k <= 0) continue;
                n += k;
                if (bericht != null)
                    bericht.Add(k.ToString(CultureInfo.InvariantCulture) + " x " + TAB_TRAEGER + "." + traegerspalte +
                                " aus " + TAB_STAMM + "." + standardspalte + " uebernommen");
            }
            return n;
        }

        // =================================================================================
        //  b. Der Entfall der sechs Spalten
        // =================================================================================

        private static int Entfernen(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> s in new List<KeyValuePair<string, string>>(Spalten))
            {
                if (!Vorhanden(s.Key, s.Value)) continue;
                DataRepository.ExecuteNonQuery("ALTER TABLE " + Q(s.Key) + " DROP COLUMN " + Q(s.Value));
                n++;
                if (bericht != null) bericht.Add(s.Key + "." + s.Value + " entfernt");
            }
            return n;
        }

        // =================================================================================
        //  Die Pruefsummen des Brennstoffkatalogs
        // =================================================================================

        /// <summary>
        /// Die Sicht des Brennstoffkatalogs mit der ALTEN Spaltenfolge (<see cref="ALTSPALTEN"/>, soweit die
        /// Spalten noch stehen) — die Folge, mit der die gespeicherten Prüfsummen gebildet wurden.
        /// <c>(null, null)</c>, wenn der Katalog nicht im Register steht oder die Preisspalten schon fort sind.
        /// </summary>
        private static (Katalogtabelle Tabelle, List<string> Spalten) AlteSicht()
        {
            Katalogtabelle neu = Katalogfassung.Tabelle(TAB_STAMM);
            if (neu == null || !Katalogfassung.SpaltenVorhanden(TAB_STAMM)) return (null, null);
            List<string> alt = ALTSPALTEN.Where(s => DataRepository.SpalteVorhanden(TAB_STAMM, s)).ToList();
            if (alt.Count == ALTSPALTEN.Count - PAARE.Count) return (null, null);   // die Preisspalten sind schon fort
            var tAlt = new Katalogtabelle(neu.Tabelle, neu.Kuerzel, alt.ToArray())
            {
                Stufe = neu.Stufe,
                Anzeigeschluessel = neu.Anzeigeschluessel,
                Verweise = neu.Verweise,
                Namensspalten = neu.Namensspalten,
            };
            return (tAlt, alt);
        }

        /// <summary>
        /// Die Prüfsumme eines Katalogsatzes mit der ALTEN Spaltenfolge — die EINE Stelle, an der die Welle sie
        /// noch bilden kann (Nachweis und Vergleich des Schritts). <c>null</c>, sobald die Preisspalten fort sind.
        /// </summary>
        internal static string AltePruefsumme(long id)
        {
            (Katalogtabelle t, List<string> alt) = AlteSicht();
            if (t == null) return null;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT " + Q("ID") + ", " + Katalogfassung.Spaltentext(alt) + " FROM " + Q(TAB_STAMM) +
                " WHERE " + Q("ID") + " = ?", new DbParam("@id", id));
            if (dt == null || dt.Rows.Count == 0) return null;
            return Katalogfassung.PruefsummeDerZeile(t, alt, dt.Rows[0], Katalogfassung.LeseOhneVorgang);
        }

        /// <summary>
        /// Die Sätze, deren gespeicherte Prüfsumme VOR dem Entfall stimmte — gebildet über
        /// <see cref="ALTSPALTEN"/>. Nur sie bekommen danach eine neue Summe.
        /// </summary>
        private static List<long> StimmigeSaetze()
        {
            var ids = new List<long>();
            (Katalogtabelle tAlt, List<string> alt) = AlteSicht();
            if (tAlt == null) return ids;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT " + Q("ID") + ", " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + ", " + Katalogfassung.Spaltentext(alt) +
                " FROM " + Q(TAB_STAMM) + " WHERE " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + " IS NOT NULL");
            if (dt == null) return ids;
            foreach (DataRow r in dt.Rows)
                if (string.Equals(Convert.ToString(r[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture),
                                  Katalogfassung.PruefsummeDerZeile(tAlt, alt, r, Katalogfassung.LeseOhneVorgang),
                                  StringComparison.Ordinal))
                    ids.Add(Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        /// <summary>Bildet die Prüfsumme der übergebenen Sätze mit der NEUEN Spaltenfolge neu.</summary>
        private static int PruefsummenNeuBilden(List<long> ids)
        {
            if (ids == null || ids.Count == 0) return 0;
            Katalogtabelle t = Katalogfassung.Tabelle(TAB_STAMM);
            if (t == null || !Katalogfassung.SpaltenVorhanden(TAB_STAMM)) return 0;
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            int n = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    Func<string, DbParam[], DataTable> lese = (sql, p) => v.Lese(sql, p);
                    foreach (long id in ids)
                    {
                        DataTable dt = v.Lese("SELECT " + Q("ID") + ", " + Katalogfassung.Spaltentext(spalten) +
                                              " FROM " + Q(TAB_STAMM) + " WHERE " + Q("ID") + " = ?",
                                              new[] { new DbParam("@id", id) });
                        if (dt == null || dt.Rows.Count == 0) continue;
                        v.Ausfuehren("UPDATE " + Q(TAB_STAMM) + " SET " + Q(Katalogfassung.SPALTE_PRUEFSUMME) +
                                     " = ? WHERE " + Q("ID") + " = ?",
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
            return n;
        }

        private static string Q(string name) => "\"" + name + "\"";
    }
}
