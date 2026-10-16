using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE KOSTENSPALTEN DES STROMSPEICHERS ENTFALLEN - Welle KB-5 (Anwenderentscheid
    // 13.10.2026: "eine Kostenbasis, keine Doppelhaltung").
    //
    // WOZU. Tab_Stromspeicher_STAMM und seine Projektkopie Tab_Stromspeicher fuehrten vier
    // Kostenspalten: Modulkosten [EUR/kWh Nennkapazitaet], Leistungskosten [EUR/kW],
    // Investition_Fix [EUR] und Verschleisskosten [EUR/(kWh Nennkapazitaet * Vollzyklus)].
    // Welle KB-3 hat sie in Satzvorlagen gesaet (Komponente 5): die Investitionsvorlage mit
    // EUR_PRO_KWH_KAPAZITAET, EUR_PRO_KW_LEISTUNG und BETRAG, die Betriebsvorlage mit
    // EUR_PRO_KWH_ELEKTRISCH. Ein Vollzyklus entlaedt die Nennkapazitaet - EUR je kWh
    // Nennkapazitaet und Vollzyklus ist dieselbe Groesse wie EUR je entladener kWh.
    // KB-4 liess den Stromspeicher aus, weil sein RECHENWEG die Spalten las; seit KB-5 liest
    // er die vier Saetze ueber KatalogkostensatzCtrl.Stromspeicher aus Position und Vorlage.
    //
    // WAS, IN DIESER REIHENFOLGE:
    //   1. SICHERHEITSLAUF der Saat (KatalogkostenSaatSchema.Saeen nur mit dem Gewerk
    //      Stromspeicher; idempotent - nur wo ein Kostenwert > 0 und kein Verweis steht).
    //      Die Verweisspalten stehen seit 208/209 (Katalog) und 226 (Projektkopie).
    //   2. WACHE: Traegt danach noch eine Zeile einen Kostenwert > 0 ohne Verweis, bricht
    //      der Schritt BENANNT ab und laesst die Spalten stehen.
    //   3. DROP COLUMN der acht Spalten (vier an Katalog und Projektkopie).
    //   4. NEUBILDUNG der Pruefsummen des Katalogs nach dem Muster von 228 (ALTE_FOLGE).
    //
    // ERGEBNISGLEICH. Der Rechenweg liest dieselben Zahlen aus der Satzvorlage, die die
    // Saat aus den Spalten uebernommen hat (Rangfolge: Kostenposition der Anlage, Vorlage
    // der Projektkopie, Vorlage des Katalogsatzes). Der Rueckfall c_ver 0,025 greift wie
    // vorher dort, wo kein Verschleisssatz gepflegt ist.
    //
    // DIE NAMEN STEHEN NUR HIER, UND NUR ALS ARGUMENT (Muster GeraetekostenEntfallSchema).
    //
    // NUMMER. 229 - haengt an 228 GeraetekostenEntfallSchema.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung
    // in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>KB-5</b> — die Kostenspalten des Stromspeichers entfallen: Sicherheitslauf der Saat, Wache,
    /// Entfall der acht Spalten, Neubildung der Katalogprüfsummen.
    /// </summary>
    public static class StromspeicherKostenSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — 229, hängt an 228
        /// (<see cref="GeraetekostenEntfallSchema.SCHRITT"/>).
        /// </summary>
        public const int SCHRITT = GeraetekostenEntfallSchema.SCHRITT + 1;

        /// <summary>Der Auslieferungskatalog der Stromspeicher.</summary>
        public const string TAB_STAMM = "Tab_Stromspeicher_STAMM";

        /// <summary>Die Projektkopie der Stromspeicher.</summary>
        public const string TAB_KOPIE = "Tab_Stromspeicher";

        /// <summary>Die Kostenkomponente des Stromspeichers.</summary>
        public const int KOMPONENTE = 5;

        /// <summary>Das Gewerk Stromspeicher aus der Saat von KB-3 — die EINE Feldliste.</summary>
        public static Katalogkostengewerk GEWERK =>
            KatalogkostenSaatSchema.GEWERKE.First(g => string.Equals(g.Kopie, TAB_KOPIE, StringComparison.Ordinal));

        /// <summary>Die Gewerkliste dieses Schritts (ein Gewerk).</summary>
        public static IReadOnlyList<Katalogkostengewerk> GEWERKE => new[] { GEWERK };

        /// <summary>Die vier entfallenden Spalten je Tabelle.</summary>
        public static IReadOnlyList<string> Kostenspalten => GEWERK.Felder.Select(f => f.Spalte).ToArray();

        /// <summary>Die acht entfallenden Spalten: zuerst der Katalog, dann die Projektkopie.</summary>
        public static IEnumerable<(string Tabelle, string Spalte)> Entfall
        {
            get
            {
                foreach (string tabelle in new[] { TAB_STAMM, TAB_KOPIE })
                    foreach (string spalte in Kostenspalten)
                        yield return (tabelle, spalte);
            }
        }

        /// <summary>
        /// <b>Die alte Fachspaltenfolge des Katalogs</b> — je entfallende Spalte ihr Platz in der Folge, mit
        /// der die gespeicherten Prüfsummen gebildet wurden (Katalogfassung vor KB-5).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Spalte, int Platz)>> ALTE_FOLGE =
            new Dictionary<string, IReadOnlyList<(string Spalte, int Platz)>>(StringComparer.Ordinal)
            {
                [TAB_STAMM] = new[]
                {
                    ("Modulkosten", 6), ("Verschleisskosten", 9), ("Leistungskosten", 10), ("Investition_Fix", 11),
                },
            };

        /// <summary>Was ein Lauf getan hat: gesäte Vorlagen, entfernte Spalten, neue Prüfsummen.</summary>
        /// <param name="NutzungsdauerNachgetragen">Gesäte Investitionspositionen, die ihre Nutzungsdauerzeile erst hier
        /// bekommen haben (<see cref="NutzungsdauerNachtragen"/>).</param>
        public readonly record struct Laufergebnis(int Gesaet, int Entfernt, int PruefsummenNeu, int NutzungsdauerNachgetragen = 0)
        {
            /// <summary>Die Summe der Handgriffe.</summary>
            public int Handgriffe => Gesaet + Entfernt + PruefsummenNeu + NutzungsdauerNachgetragen;
        }

        /// <summary>Die Tabellen, ohne die der Schritt nicht laufen kann.</summary>
        public static IEnumerable<string> Voraussetzungen()
            => new[] { TAB_STAMM, TAB_KOPIE, KatalogkostenSaatSchema.TAB_KOSTENVORLAGE,
                       KatalogkostenSaatSchema.TAB_KOSTENVORLAGEPOSITION };

        /// <summary>Steht diese Spalte noch?</summary>
        public static bool Vorhanden(string tabelle, string spalte) =>
            DataRepository.TabelleVorhanden(tabelle) && DataRepository.SpalteVorhanden(tabelle, spalte);

        /// <summary>Wie viele der acht Spalten stehen noch? 0 = der Schritt ist gelaufen.</summary>
        public static int Offen() => Entfall.Count(s => Vorhanden(s.Tabelle, s.Spalte));

        /// <summary>Steht der Schritt vollständig (keine Kostenspalte mehr da)?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// <b>Die Wache vor dem Entfall</b>: die Zeilen mit einem Kostenwert &gt; 0 OHNE Verweis auf eine
        /// Satzvorlage — je Zeile eine Meldung. Leer heißt: Der Entfall verliert nichts.
        /// </summary>
        public static IReadOnlyList<string> OhneVerweis()
        {
            var offen = new List<string>();
            Katalogkostengewerk g = GEWERK;
            foreach (string tabelle in new[] { g.Katalog, g.Kopie })
                foreach (KatalogkostenSaatSchema.Satzbefund b in KatalogkostenSaatSchema.Befunde(g, tabelle))
                {
                    if (b.Verweis != 0) continue;
                    if (b.Positionen.Count == 0 && b.OhneBezug.Count == 0) continue;
                    offen.Add(tabelle + " ID " + b.Id.ToString(CultureInfo.InvariantCulture) +
                              ": Kostenwert ohne Satzvorlage");
                }
            return offen;
        }

        /// <summary>Führt den Schritt aus; Rückgabe: die Zahl der Handgriffe. <b>Wiederholbar.</b></summary>
        public static int Ausfuehren(IList<string> bericht) => Lauf(bericht).Handgriffe;

        /// <summary>
        /// <see cref="Ausfuehren"/> mit Aufschlüsselung. Bricht <b>benannt</b> ab, wenn nach der Saat noch ein
        /// Kostenwert ohne Satzvorlage steht; die Spalten bleiben dann stehen.
        /// </summary>
        public static Laufergebnis Lauf(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");

            int gesaet = KatalogkostenSaatSchema.Saeen(bericht, GEWERKE);
            int nutzungsdauer = NutzungsdauerNachtragen(bericht);

            IReadOnlyList<string> offen = OhneVerweis();
            if (offen.Count > 0)
                throw new InvalidOperationException(
                    "Schemaschritt " + Nr + ": " + offen.Count.ToString(CultureInfo.InvariantCulture) +
                    " Zeile(n) tragen einen Kostenwert ohne Satzvorlage; die Spalten bleiben stehen. " +
                    string.Join("; ", offen.Take(10)) + (offen.Count > 10 ? " …" : ""));

            Dictionary<string, List<long>> stimmig = GeraetekostenEntfallSchema.StimmigeSaetze(ALTE_FOLGE);
            int entfernt = Entfernen(bericht);
            int summen = GeraetekostenEntfallSchema.PruefsummenNeuBilden(stimmig, bericht);
            bericht?.Add("Rechenweg: die vier Speichersaetze kommen aus Position und Satzvorlage; " +
                         "dieselben Zahlen wie aus den Spalten");
            return new Laufergebnis(gesaet, entfernt, summen, nutzungsdauer);
        }

        /// <summary>
        /// <b>Nachzug Nutzungsdauer</b> (Welle KB-5): Die Saat der Schritte 226 und 228 und die eigene Saat dieses Schritts
        /// legen Investitionspositionen ohne <c>NutzungsdauerID</c> an; Schritt 75 ordnet sie nur zu, wenn er in DEMSELBEN
        /// Lauf nach der Saat läuft. Hier läuft dieselbe Regel (<see cref="NutzungsdauerSchema.ZuordnungSchreiben"/>:
        /// Bezeichnung und Technik der Vorlage, nur Investitionsvorlagen, nur wo der Verweis leer ist) noch einmal —
        /// wiederholbar und nie überschreibend; eine vom Anwender gepflegte Zuordnung bleibt. Ohne Nutzungsdauertabelle
        /// (Schritt 75 nicht gelaufen) tut der Nachzug nichts.
        /// </summary>
        /// <returns>Zahl der gesetzten Verweise.</returns>
        public static int NutzungsdauerNachtragen(IList<string> bericht)
        {
            if (!DataRepository.TabelleVorhanden(NutzungsdauerSchema.TABELLE) ||
                !DataRepository.SpalteVorhanden(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, NutzungsdauerSchema.SPALTE_VERWEIS))
                return 0;
            int n = NutzungsdauerSchema.ZuordnungSchreiben();
            bericht?.Add(n > 0
                ? "Nutzungsdauer an " + n.ToString(CultureInfo.InvariantCulture) + " gesaeter Investitionsposition(en) nachgetragen"
                : "Nutzungsdauer der gesaeten Investitionspositionen: nichts nachzutragen");
            return n;
        }

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
            if (n == 0) bericht?.Add("keine Kostenspalte des Stromspeichers mehr da; nichts zu tun");
            return n;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
