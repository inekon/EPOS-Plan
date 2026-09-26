using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Strukturtabellen des Berichts</b> (Konzept Berichtsvorlagen 5.4, Anhang A; Etappe BV-E5) — je Tabelle
    /// ein Bauweg aus DENSELBEN Zeilenquellen, die die Bausteine lesen: Die Bausteine schreiben ihre Tabellen über
    /// diese Bauwege (<see cref="WordTabellenschreiber.Direkt"/>), der Vorlagenweg setzt sie an <c>{{tabelle.…}}</c>.
    /// Gerechnet wird hier nichts; gelesen wird nur der Berichtsbaum (<see cref="BerichtsDaten"/>) samt Wertesatz der
    /// Wirtschaftlichkeit — keine Datenbank.
    ///
    /// <para><b>Sprache.</b> Jeder Bauweg nimmt Sprache und Kultur des Berichts; Kopftexte laufen durch
    /// <see cref="BerichtTexte.T(string, bool)"/> wie bisher in <see cref="WordKontext.Zelle(string, int, bool, string, DocumentFormat.OpenXml.Wordprocessing.JustificationValues)"/>,
    /// Datenzellen bleiben, wie die Bausteine sie schreiben.</para>
    ///
    /// <para><b>Leer.</b> Ein Bauweg liefert immer eine Tabelle; hat sie keine Zeile, trägt sie ihren
    /// <see cref="Berichtstabelle.Leergrund"/> (Konzept 4.10). Wo der Baustein heute statt einer Tabelle einen Satz
    /// schreibt, schreibt er ihn weiter.</para>
    /// </summary>
    public static partial class Berichtstabellen
    {
        /// <summary>Die Schlüsselkennzahlen der kompakten Δ-%-Tafel des Variantenvergleichs.</summary>
        public static readonly IReadOnlyList<string> DeltaSchluessel = new[]
        {
            "energie.waermebedarf", "energie.brennstoff", "energie.netzbezug",
            "energie.waermerest", "eff.jaz", "eff.autarkie"
        };

        /// <summary>Die Kernkennzahlen des Kapitels „Ergebnisse je Variante“ in Anzeigereihenfolge.</summary>
        public static readonly IReadOnlyList<string> Kernkennzahlen = new[]
        {
            "energie.waermebedarf", "energie.strombedarf",
            "energie.wp_waerme", "energie.bhkw_waerme", "energie.kessel_waerme",
            "energie.solar_waerme", "energie.bhkw_strom", "energie.pv_strom",
            "energie.brennstoff", "energie.netzbezug", "energie.einspeisung",
            "energie.waermerest", "eff.jaz", "eff.autarkie"
        };

        /// <summary>Die Kennzahlgruppen des Vergleichs (<see cref="KennzahlenKatalog.GRUPPEN"/>) und ihre Schlüssel im Katalog.</summary>
        public static readonly IReadOnlyList<(string Gruppe, string Schluessel)> Vergleichsgruppen = new[]
        {
            (KennzahlenKatalog.GR_ENERGIE, "energiebilanz"),
            (KennzahlenKatalog.GR_EFFIZIENZ, "effizienz"),
            (KennzahlenKatalog.GR_KAELTE, "kaelte"),
            (KennzahlenKatalog.GR_EMISSION, "emissionen"),
            (KennzahlenKatalog.GR_KOSTEN, "kosten"),
        };

        /// <summary>Die Gewerke der Kenndatentafeln (<see cref="ProjektDetails.GewerkTabellen"/>) und ihre Schlüssel im Katalog.</summary>
        public static IReadOnlyList<(string Gewerk, string Schluessel)> Gewerke
        {
            get
            {
                return ProjektDetails.GewerkTabellen
                    .Select(g => (g.Key, Platzhaltersyntax.NormiereSchluessel(g.Key)))
                    .ToList();
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Ein Grund aus <c>MyResource</c> in der Kultur des Berichts.</summary>
        internal static string Grund(string ressource, CultureInfo kultur)
        {
            try { return R.ResourceManager.GetString(ressource, kultur) ?? ressource; }
            catch { return ressource; }
        }

        /// <summary>Eine leere Tabelle mit Grund.</summary>
        internal static Berichtstabelle Leer(string ressource, CultureInfo kultur)
        {
            return new Berichtstabelle { Leergrund = Grund(ressource, kultur) };
        }

        /// <summary>Die Kopfbeschriftung eines Stands — „Stamm“ oder sein Anzeigename — übersetzt wie im Baustein.</summary>
        private static string Standkopf(VariantenDaten v, bool englisch)
        {
            return BerichtTexte.T(v.IstStamm ? "Stamm" : v.Anzeige, englisch);
        }

        private static double? Wert(VariantenDaten v, string schluessel)
        {
            return v != null && v.Kennzahlen != null && v.Kennzahlen.TryGetValue(schluessel, out double? w) ? w : null;
        }

        /// <summary>
        /// Die Eigenschaftstabelle Beschriftung · Wert (<see cref="WordKontext.Eigenschaften"/>): Beschriftung 2800 DXA
        /// fett und grau hinterlegt, Wert über den Rest, ohne Kopf.
        /// </summary>
        internal static Berichtstabelle Eigenschaftstabelle(IEnumerable<(string Beschriftung, Tabellenzelle Wert)> paare, bool englisch)
        {
            var t = new Berichtstabelle().Feste(2800, 0);
            foreach ((string beschriftung, Tabellenzelle wert) in paare)
                t.Zeile(new[]
                {
                    Zellen.Text(BerichtTexte.T(beschriftung, englisch), fett: true, h: Tabellenhinterlegung.Stamm),
                    wert,
                });
            return t;
        }

        // =====================================================================
        //  Variantenliste (tabelle.varianten)
        // =====================================================================

        /// <summary>
        /// <b><c>tabelle.varianten</c></b> — die Variantenliste mit sechs Spalten: Rolle, Bezeichner, Projektname,
        /// Simulation vom, Hinweis und Stromspeicher (Konzept 5.4). Die ersten fünf wie die Übersicht der Mappe,
        /// der Stromspeicher aus dem Wertesatz des Laufs (<paramref name="stromspeicher"/>, die Zeile
        /// <c>SPEICHER_KONTEXT</c>, im Sammler über <c>SpeicherKontextText</c> erhoben). Listentauglich.
        /// </summary>
        public static Berichtstabelle Varianten(BerichtsDaten daten, Func<VariantenDaten, string> stromspeicher,
                                                bool englisch, CultureInfo kultur)
        {
            var t = new Berichtstabelle().Feste(1100, 1700, 1900, 1500, 0, 1500);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Rolle", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Bezeichner", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Projektname", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Simulation vom", englisch)),
                Zellen.Kopf(BerichtTexte.T("Hinweis", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Stromspeicher", englisch), Tabellenausrichtung.Links),
            });
            foreach (VariantenDaten v in (daten?.Varianten ?? new List<VariantenDaten>()).Where(v => v != null))
            {
                Tabellenrolle rolle = Zellen.RolleWenn(v.IstStamm);
                Tabellenhinterlegung h = Zellen.StammWenn(v.IstStamm);
                string speicher = null;
                try { speicher = stromspeicher?.Invoke(v); }
                catch { speicher = null; }
                t.Zeile(new[]
                {
                    Zellen.Text(BerichtTexte.T(v.IstStamm ? "Stamm" : "Variante", englisch), rolle: rolle, h: h),
                    Zellen.Text(v.IstStamm ? BerichtTexte.T("(Stammprojekt)", englisch) : v.Variantenname, rolle: rolle, h: h),
                    Zellen.Text(v.Projektname, rolle: rolle, h: h),
                    Zellen.Text(Simulationsstand(v, kultur), Tabellenausrichtung.Mitte, rolle, h: h),
                    Zellen.Text(Standhinweis(v, englisch), rolle: rolle, h: h),
                    Zellen.Text(string.IsNullOrWhiteSpace(speicher) ? Tabellenzelle.STRICH : speicher,
                                string.IsNullOrWhiteSpace(speicher) ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Links, rolle, h: h),
                });
            }
            if (t.IstLeer) t.Leergrund = Grund(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            return t;
        }

        private static string Simulationsstand(VariantenDaten v, CultureInfo kultur)
        {
            return v.SimulationsStand.HasValue ? v.SimulationsStand.Value.ToString("dd.MM.yyyy HH:mm", kultur) : Tabellenzelle.STRICH;
        }

        /// <summary>Der Hinweis eines Stands wie im Anhang (Fehler, neu gerechnet, veraltet), übersetzt.</summary>
        private static string Standhinweis(VariantenDaten v, bool englisch)
        {
            if (v.Fehler != null) return BerichtTexte.T("Fehler: ", englisch) + v.Fehler;
            if (v.FrischSimuliert) return BerichtTexte.T("für diesen Bericht neu gerechnet", englisch);
            if (v.ErgebnisVeraltet) return BerichtTexte.T("älter als letzte Projektänderung", englisch);
            return "";
        }

        // =====================================================================
        //  Anhang: Simulationsstände (tabelle.anhang.simulationsstaende)
        // =====================================================================

        /// <summary><b><c>tabelle.anhang.simulationsstaende</c></b> — die Tafel des Anhangs: Projekt, Rolle, Simulation vom, Hinweis.</summary>
        public static Berichtstabelle Simulationsstaende(BerichtsDaten daten, bool englisch, CultureInfo kultur)
        {
            var t = new Berichtstabelle().Feste(3200, 1400, 2400, 2355);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Projekt", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Rolle", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Simulation vom", englisch)),
                Zellen.Kopf(BerichtTexte.T("Hinweis", englisch), Tabellenausrichtung.Links),
            });
            foreach (VariantenDaten v in (daten?.Varianten ?? new List<VariantenDaten>()))
            {
                string hinweis = v.Fehler != null ? "Fehler: " + v.Fehler
                    : v.FrischSimuliert ? "für diesen Bericht neu gerechnet"
                    : v.ErgebnisVeraltet ? "älter als letzte Projektänderung" : "";
                t.Zeile(new[]
                {
                    Zellen.Text(v.Projektname, rolle: Zellen.RolleWenn(v.IstStamm), h: Zellen.StammWenn(v.IstStamm)),
                    Zellen.Text(v.IstStamm ? "Stamm" : "Variante"),
                    Zellen.Text(Simulationsstand(v, kultur), Tabellenausrichtung.Mitte),
                    Zellen.Text(hinweis),
                });
            }
            if (t.IstLeer) t.Leergrund = Grund(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            return t;
        }

        // =====================================================================
        //  Komponenten (tabelle.komponenten.*, stand.tabelle.abweichungen)
        // =====================================================================

        /// <summary><b><c>tabelle.komponenten.matrix</c></b> — Gewerk × Stand: „✓“, „✓ (n)“ oder „—“; Spalten je Stand.</summary>
        public static Berichtstabelle Komponentenmatrix(BerichtsDaten daten, bool englisch, CultureInfo kultur)
        {
            VariantenDaten stamm = daten?.Varianten?.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return Leer(nameof(R.BV_GRUND_KEIN_STAMM), kultur);
            List<VariantenDaten> spalten = Spaltenfolge(daten, stamm);

            var t = new Berichtstabelle().Feste(2600);
            foreach (VariantenDaten v in spalten) t.Spalte(v.IstStamm ? Spaltenart.Stamm : Spaltenart.Stand, 0, v.IdProjekt);
            t.MitKopf(new[] { Zellen.Kopf(BerichtTexte.T("Gewerk", englisch), Tabellenausrichtung.Links) }
                .Concat(spalten.Select(v => Zellen.Kopf(Standkopf(v, englisch)))));

            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                var zellen = new List<Tabellenzelle> { Zellen.Text(g.Key) };
                foreach (VariantenDaten v in spalten)
                {
                    ProjektDetails d = v.Details;
                    int n = (d != null && d.KomponentenAnzahl.ContainsKey(g.Key)) ? d.KomponentenAnzahl[g.Key] : 0;
                    string text = n == 0 ? Tabellenzelle.STRICH : (n == 1 ? "✓" : "✓ (" + n + ")");
                    zellen.Add(new Tabellenzelle
                    {
                        Text = text, Zahl = n, Format = "N0", Ausrichtung = Tabellenausrichtung.Mitte,
                        Rolle = Zellen.RolleWenn(v.IstStamm), Hinterlegung = Zellen.StammWenn(v.IstStamm),
                    });
                }
                t.Zeile(zellen);
            }
            return t;
        }

        /// <summary>
        /// <b><c>tabelle.komponenten.kenndaten.&lt;gewerk&gt;</c></b> — die Merkmale eines Gewerks
        /// (<see cref="AbweichungsErmittler.Felder"/>) je Stand; gezeigt wird das erste Gerät des Gewerks. Leer, wenn
        /// kein Stand das Gewerk hat.
        /// </summary>
        public static Berichtstabelle Kenndaten(BerichtsDaten daten, string gewerk, bool englisch, CultureInfo kultur)
        {
            VariantenDaten stamm = daten?.Varianten?.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return Leer(nameof(R.BV_GRUND_KEIN_STAMM), kultur);
            string tabelle = ProjektDetails.GewerkTabellen.FirstOrDefault(g => g.Key == gewerk).Value;
            bool vorhanden = daten.Varianten.Any(v => v.Details != null && v.Details.HatGewerk(gewerk));
            var merkmale = tabelle == null ? new List<AbweichungsErmittler.Merkmal>()
                                           : AbweichungsErmittler.Felder.Where(f => f.Tabelle == tabelle).ToList();
            if (!vorhanden || merkmale.Count == 0) return Leer(nameof(R.BV_GRUND_NICHT_VERFUEGBAR), kultur);

            List<VariantenDaten> spalten = Spaltenfolge(daten, stamm);
            var t = new Berichtstabelle().Feste(2600);
            foreach (VariantenDaten v in spalten) t.Spalte(v.IstStamm ? Spaltenart.Stamm : Spaltenart.Stand, 0, v.IdProjekt);
            t.MitKopf(new[] { Zellen.Kopf(BerichtTexte.T("Merkmal", englisch), Tabellenausrichtung.Links) }
                .Concat(spalten.Select(v => Zellen.Kopf(Standkopf(v, englisch)))));

            foreach (AbweichungsErmittler.Merkmal f in merkmale)
            {
                var zellen = new List<Tabellenzelle> { Zellen.Text(f.Label) };
                foreach (VariantenDaten v in spalten)
                {
                    ProjektDetails d = v.Details;
                    DataRow zeile = (d != null && d.Komponenten.ContainsKey(gewerk)) ? d.Komponenten[gewerk] : null;
                    string wert = zeile == null ? Tabellenzelle.STRICH : AbweichungsErmittler.Formatiere(zeile, f);
                    zellen.Add(Zellen.Text(wert, wert == Tabellenzelle.STRICH ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Rechts,
                                           Zellen.RolleWenn(v.IstStamm), h: Zellen.StammWenn(v.IstStamm)));
                }
                t.Zeile(zellen);
            }
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.abweichungen</c></b> — die Abweichungen eines Stands gegenüber dem Stamm: Gewerk, Merkmal,
        /// Stamm, Variante (<see cref="VariantenDaten.Abweichungen"/>, im Sammler erhoben).
        /// </summary>
        public static Berichtstabelle Abweichungen(VariantenDaten v, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            if (v.IstStamm) return Leer(nameof(R.BV_GRUND_IST_STAMM), kultur);
            if (v.Abweichungen == null || v.Abweichungen.Count == 0) return Leer(nameof(R.BV_GRUND_KEIN_DELTA), kultur);

            var t = new Berichtstabelle().Feste(1900, 2600, 2400, 2455);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Gewerk", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Merkmal", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Stamm", englisch)),
                Zellen.Kopf(BerichtTexte.T(v.Anzeige, englisch)),
            });
            foreach (Abweichung a in v.Abweichungen)
                t.Zeile(new[]
                {
                    Zellen.Text(a.Gewerk),
                    Zellen.Text(a.Merkmal),
                    Zellen.Text(a.WertStamm, Tabellenausrichtung.Mitte, Tabellenrolle.Stamm, h: Tabellenhinterlegung.Stamm),
                    Zellen.Text(a.WertVariante, Tabellenausrichtung.Mitte),
                });
            return t;
        }

        /// <summary>Der Stamm vor den Varianten — die Spalten der Tafeln je Stand.</summary>
        private static List<VariantenDaten> Spaltenfolge(BerichtsDaten daten, VariantenDaten stamm)
        {
            var spalten = new List<VariantenDaten> { stamm };
            spalten.AddRange(daten.Varianten.Where(v => !v.IstStamm));
            return spalten;
        }

        // =====================================================================
        //  Ergebnisse je Variante (stand.tabelle.kennzahlen)
        // =====================================================================

        /// <summary>
        /// <b><c>stand.tabelle.kennzahlen</c></b> — die 14 Kernkennzahlen eines Stands als Beschriftung · Wert
        /// (<see cref="Kernkennzahlen"/>); fehlende Gewerke stehen nicht als Leerzeilen da.
        /// </summary>
        public static Berichtstabelle Standkennzahlen(VariantenDaten v, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            if (v.Fehler != null) return Leer(nameof(R.BV_GRUND_LAUF_FEHLGESCHLAGEN), kultur);

            List<Kennzahl> katalog = KennzahlenKatalog.Alle();
            var paare = new List<(string, Tabellenzelle)>();
            foreach (string schluessel in Kernkennzahlen)
            {
                Kennzahl kz = katalog.FirstOrDefault(x => x.Schluessel == schluessel);
                if (kz == null) continue;
                double? wert = v.Kennzahlen.ContainsKey(schluessel) ? v.Kennzahlen[schluessel] : null;
                if (!wert.HasValue) continue;
                string einheit = kz.Einheit == "–" ? null : kz.Einheit;
                paare.Add((kz.Label(englisch), new Tabellenzelle
                {
                    Text = Tabellenformat.FW(wert, kz.Format, kultur) + (einheit == null ? "" : " " + einheit),
                    Zahl = wert, Format = kz.Format, Einheit = einheit,
                }));
            }
            Berichtstabelle t = Eigenschaftstabelle(paare, englisch);
            if (t.IstLeer) t.Leergrund = Grund(v.Ergebnis == null ? nameof(R.BV_GRUND_KEIN_ERGEBNIS) : nameof(R.BV_GRUND_NICHT_VERFUEGBAR), kultur);
            return t;
        }

        // =====================================================================
        //  Variantenvergleich (tabelle.vergleich.*, stand.tabelle.erzeuger, .brennstoffmengen)
        // =====================================================================

        /// <summary>
        /// Die Kennzahlen einer Gruppe, die mindestens ein Stand führt — die Zeilen der Gruppentafel. Der Katalog ist
        /// nach dem Modus des Laufs beschriftet.
        /// </summary>
        public static List<Kennzahl> Gruppenzeilen(BerichtsDaten daten, string gruppe)
        {
            List<Kennzahl> katalog = KennzahlenKatalog.Alle(EmissionsAusweis.ModusAusVarianten(daten.Varianten));
            return katalog.Where(x => x.Gruppe == gruppe)
                .Where(x => daten.Varianten.Any(v => v.Kennzahlen.ContainsKey(x.Schluessel) && v.Kennzahlen[x.Schluessel].HasValue))
                .ToList();
        }

        /// <summary>
        /// <b><c>tabelle.vergleich.&lt;gruppe&gt;</c></b> — die Kennzahltafel einer Gruppe: Kennzahl (Einheit), Stamm,
        /// je Variante eine Spalte, bei genau einer Variante die Δ-Spalte. Blockteilung zu drei Varianten mit
        /// wiederholter Stammspalte (<see cref="Berichtstabelle.Bloecke"/>).
        /// </summary>
        public static Berichtstabelle Vergleichsgruppe(BerichtsDaten daten, string gruppe, bool englisch, CultureInfo kultur)
        {
            return Vergleich(daten, new[] { gruppe }, false, englisch, kultur);
        }

        /// <summary>
        /// <b><c>tabelle.vergleich</c></b> — alle Gruppen in einer Tafel, je Gruppe eine Gruppenzeile (Rolle Gruppe).
        /// </summary>
        public static Berichtstabelle Vergleichsgesamt(BerichtsDaten daten, bool englisch, CultureInfo kultur)
        {
            return Vergleich(daten, KennzahlenKatalog.GRUPPEN, true, englisch, kultur);
        }

        private static Berichtstabelle Vergleich(BerichtsDaten daten, IEnumerable<string> gruppen, bool mitGruppenzeilen,
                                                 bool englisch, CultureInfo kultur)
        {
            VariantenDaten stamm = daten?.Varianten?.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return Leer(nameof(R.BV_GRUND_KEIN_STAMM), kultur);
            List<VariantenDaten> varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            bool mitDelta = varianten.Count == 1;   // Δ-Spalte nur bei genau einer Variante (Konzept 5.4)

            var t = new Berichtstabelle().Feste(3100);
            t.Spalte(Spaltenart.Stamm, 0, stamm.IdProjekt);
            foreach (VariantenDaten v in varianten) t.Spalte(Spaltenart.Stand, 0, v.IdProjekt);
            if (mitDelta) t.Spalte(Spaltenart.Nach, 0);

            var kopf = new List<Tabellenzelle> { Zellen.Kopf(BerichtTexte.T("Kennzahl (Einheit)", englisch), Tabellenausrichtung.Links) };
            kopf.Add(Zellen.Kopf(Standkopf(stamm, englisch)));
            kopf.AddRange(varianten.Select(v => Zellen.Kopf(Standkopf(v, englisch))));
            if (mitDelta) kopf.Add(Zellen.Kopf(BerichtTexte.T("Δ (Var. − Stamm)", englisch)));
            t.MitKopf(kopf);

            int breite = kopf.Count;
            foreach (string gruppe in gruppen)
            {
                List<Kennzahl> zeilen = Gruppenzeilen(daten, gruppe);
                if (zeilen.Count == 0) continue;
                if (mitGruppenzeilen)
                {
                    var gz = new List<Tabellenzelle>
                    {
                        Zellen.Text(BerichtTexte.T(gruppe, englisch), rolle: Tabellenrolle.Gruppe, fett: true, h: Tabellenhinterlegung.Kopf),
                    };
                    for (int i = 1; i < breite; i++)
                        gz.Add(Zellen.Text("", rolle: Tabellenrolle.Gruppe, fett: true, h: Tabellenhinterlegung.Kopf));
                    t.Zeile(gz, Tabellenrolle.Gruppe);
                }
                foreach (Kennzahl kz in zeilen)
                {
                    string label = kz.Label(englisch) + (kz.Einheit == "–" || kz.Einheit.Length == 0 ? "" : " [" + kz.Einheit + "]");
                    var zellen = new List<Tabellenzelle> { Zellen.Text(label) };
                    foreach (VariantenDaten v in new[] { stamm }.Concat(varianten))
                    {
                        double? wert = Wert(v, kz.Schluessel);
                        zellen.Add(Zellen.Zahl(Tabellenformat.FW(wert, kz.Format, kultur), wert, kz.Format,
                                               Zellen.RolleWenn(v.IstStamm), h: Zellen.StammWenn(v.IstStamm)));
                    }
                    if (mitDelta)
                    {
                        double? s = Wert(stamm, kz.Schluessel), b = Wert(varianten[0], kz.Schluessel);
                        string d = kz.DeltaAnzeigen ? Tabellenformat.Delta(s, b, kz.Format, kultur) : Tabellenzelle.STRICH;
                        zellen.Add(Zellen.Zahl(d, s.HasValue && b.HasValue ? b.Value - s.Value : (double?)null, kz.Format));
                    }
                    t.Zeile(zellen);
                }
            }
            if (t.IstLeer) t.Leergrund = Grund(nameof(R.BV_GRUND_NICHT_VERFUEGBAR), kultur);
            return t;
        }

        /// <summary>
        /// <b><c>tabelle.vergleich.delta_prozent</c></b> — die kompakte Tafel „Abweichung zum Stamm“ in Prozent:
        /// je Variante eine Zeile, je Schlüsselkennzahl (<see cref="DeltaSchluessel"/>) mit Stammwert eine Spalte.
        /// Ab zwei Varianten.
        /// </summary>
        public static Berichtstabelle DeltaProzent(BerichtsDaten daten, bool englisch, CultureInfo kultur)
        {
            VariantenDaten stamm = daten?.Varianten?.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return Leer(nameof(R.BV_GRUND_KEIN_STAMM), kultur);
            List<VariantenDaten> varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            if (varianten.Count < 2) return Leer(nameof(R.BV_GRUND_ZU_WENIG_STAENDE), kultur);

            List<Kennzahl> katalog = KennzahlenKatalog.Alle(EmissionsAusweis.ModusAusVarianten(daten.Varianten));
            List<string> keys = DeltaSchluessel.Where(s => Wert(stamm, s).HasValue && katalog.Any(x => x.Schluessel == s)).ToList();
            if (keys.Count == 0) return Leer(nameof(R.BV_GRUND_NICHT_VERFUEGBAR), kultur);

            var t = new Berichtstabelle().Feste(2600);
            foreach (string _ in keys) t.Spalte(Spaltenart.Fest, 0);
            t.MitKopf(new[] { Zellen.Kopf(BerichtTexte.T("Variante", englisch), Tabellenausrichtung.Links) }
                .Concat(keys.Select(s => Zellen.Kopf(BerichtTexte.T(katalog.First(x => x.Schluessel == s).Label(englisch), englisch)))));
            foreach (VariantenDaten v in varianten)
            {
                var zellen = new List<Tabellenzelle> { Zellen.Text(v.Anzeige) };
                foreach (string s in keys)
                {
                    string d = Tabellenformat.DeltaProzent(Wert(stamm, s), Wert(v, s), kultur);
                    zellen.Add(Zellen.Zahl(d, Tabellenformat.DeltaProzentWert(Wert(stamm, s), Wert(v, s)), "N1", einheit: "%"));
                }
                t.Zeile(zellen);
            }
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.erzeuger</c></b> — die Erzeuger-Einzelliste eines Stands: je Gerät (Modul) Wärme, Strom,
        /// Energieträger und Verbrauch in MWh/a.
        /// </summary>
        public static Berichtstabelle Erzeuger(VariantenDaten v, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            ErgebnisModel m = v.Ergebnis;
            if (m == null) return Leer(nameof(R.BV_GRUND_KEIN_ERGEBNIS), kultur);

            var t = new Berichtstabelle().Feste(2900, 1500, 1500, 1800, 1655);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Erzeuger", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Wärme [MWh/a]", englisch)),
                Zellen.Kopf(BerichtTexte.T("Strom [MWh/a]", englisch)),
                Zellen.Kopf(BerichtTexte.T("Energieträger", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Verbrauch [MWh/a]", englisch)),
            });

            Func<double, Tabellenzelle> z = x => Zellen.Zahl(Tabellenformat.F(x, 0, kultur), x, "N0");
            Tabellenzelle strich = Zellen.Zahl(Tabellenzelle.STRICH, null, null);
            Action<string, Tabellenzelle, Tabellenzelle, string, Tabellenzelle> zeile = (name, waerme, strom, traeger, verbrauch) =>
                t.Zeile(new[] { Zellen.Text(name), waerme, strom, Zellen.Text(traeger), verbrauch });

            if (m.Waermepumpe != null)
            {
                var mods = m.Waermepumpe.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisWaermepumpeModulModel mo in mods)
                        zeile(Name(mo.Modul, "Wärmepumpe"), z(mo.Waermeproduktion), strich, "Strom", z(mo.Stromverbrauch + mo.Heizstab));
                else
                    zeile("Wärmepumpe", z(m.Waermepumpe.Waermeproduktion_WP), strich, "Strom",
                          z(m.Waermepumpe.Stromverbrauch_WP + m.Waermepumpe.Stromverbrauch_Heizstab));
            }
            if (m.BHKW != null)
            {
                var mods = m.BHKW.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisBHKWModulModel mo in mods)
                        zeile(Name(mo.Modul, "BHKW"), z(mo.Waermeproduktion), z(mo.Stromproduktion),
                              LeerStrich(mo.Brennstoff), mo.Verbrauch > 0 ? z(mo.Verbrauch) : strich);
                else
                    zeile("BHKW", z(m.BHKW.Waermeproduktion), z(m.BHKW.Stromproduktion), Tabellenzelle.STRICH, strich);
            }
            if (m.Heizkessel != null)
            {
                var mods = m.Heizkessel.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisHeizkesselModulModel mo in mods)
                        zeile(Name(mo.Modul, "Spitzenkessel"),
                              z(mo.Waermeproduktion > 0 ? mo.Waermeproduktion : mo.Waerme_Gas + mo.Waerme_Oel),
                              strich, LeerStrich(mo.Brennstoff), mo.Verbrauch > 0 ? z(mo.Verbrauch) : strich);
                else
                    zeile("Spitzenkessel", z(m.Heizkessel.Waermeproduktion), strich, Tabellenzelle.STRICH, strich);
            }
            if (m.Solarthermie != null)
            {
                var mods = m.Solarthermie.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisSolarthermieModulModel mo in mods)
                        zeile(Name(mo.Modul, "Solarthermie"), z(mo.Waermeproduktion), strich, Tabellenzelle.STRICH, strich);
                else
                    zeile("Solarthermie", z(m.Solarthermie.Waermeproduktion), strich, Tabellenzelle.STRICH, strich);
            }
            if (m.Photovoltaik != null)
            {
                var mods = m.Photovoltaik.Module;
                if (mods != null && mods.Count > 0)
                    foreach (ErgebnisPhotovoltaikModulModel mo in mods)
                        zeile(Name(mo.Modul, "Photovoltaik"), strich, z(mo.Stromproduktion), Tabellenzelle.STRICH, strich);
                else
                    zeile("Photovoltaik", strich, z(m.Photovoltaik.Stromproduktion), Tabellenzelle.STRICH, strich);
            }
            if (t.IstLeer) t.Leergrund = Grund(nameof(R.BV_GRUND_NICHT_VERFUEGBAR), kultur);
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.brennstoffmengen</c></b> — Erzeuger, Bezeichner und Menge in der Abrechnungseinheit
        /// (<see cref="VariantenDaten.Brennstoffmengen"/>, im Sammler erhoben). <paramref name="mitLeerzeile"/>: ohne
        /// Mengen die Zeile „(keine Brennstoffdaten)“ wie im Kapitel; sonst bleibt die Tabelle leer.
        /// </summary>
        public static Berichtstabelle Brennstoffmengen(VariantenDaten v, bool mitLeerzeile, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            DataTable dt = v.Brennstoffmengen;
            var t = new Berichtstabelle().Feste(2900, 3800, 2655);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Erzeuger", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Bezeichner", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Menge", englisch)),
            });
            if (dt == null || dt.Rows.Count == 0)
            {
                if (mitLeerzeile)
                    t.Zeile(new[]
                    {
                        Zellen.Text(Tabellenzelle.STRICH, Tabellenausrichtung.Mitte),
                        Zellen.Text("(keine Brennstoffdaten)"),
                        Zellen.Text(Tabellenzelle.STRICH, Tabellenausrichtung.Mitte),
                    });
                else
                    t.Leergrund = Grund(nameof(R.BV_GRUND_NICHT_VERFUEGBAR), kultur);
                return t;
            }
            foreach (DataRow r in dt.Rows)
            {
                object roh = dt.Columns.Contains("Menge") ? r["Menge"] : DBNull.Value;
                string menge = roh != DBNull.Value ? roh.ToString() : Tabellenzelle.STRICH;
                double? zahl = roh is double d ? d : roh is float f ? f : roh is decimal m ? (double)m : (double?)null;
                t.Zeile(new[]
                {
                    Zellen.Text(r["Erzeuger"] != DBNull.Value ? r["Erzeuger"].ToString() : ""),
                    Zellen.Text(r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString() : ""),
                    new Tabellenzelle
                    {
                        Text = menge, Zahl = zahl,
                        Ausrichtung = menge == Tabellenzelle.STRICH ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Rechts,
                    },
                });
            }
            return t;
        }

        private static string Name(string modul, string fallback)
        { return string.IsNullOrWhiteSpace(modul) ? fallback : modul.Trim(); }

        private static string LeerStrich(string s)
        { return string.IsNullOrWhiteSpace(s) ? Tabellenzelle.STRICH : s.Trim(); }
    }
}
