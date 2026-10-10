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
            (KennzahlenKatalog.GR_GEBAEUDE, "gebaeude"),
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
            // PVG: rechnet ein Stand seine Photovoltaik ueber eine PV-Ganglinie, steht vorn die Quelle, und die
            // Merkmale des Modulmodells stehen bei ihm als „entfaellt (Ganglinie)".
            bool ganglinie = gewerk == "Photovoltaik" && daten.Varianten.Any(v => v.Details?.PvGanglinie != null);
            bool vorhanden = ganglinie || daten.Varianten.Any(v => v.Details != null && v.Details.HatGewerk(gewerk));
            var merkmale = tabelle == null ? new List<AbweichungsErmittler.Merkmal>()
                                           : AbweichungsErmittler.Felder.Where(f => f.Tabelle == tabelle).ToList();
            if (!vorhanden || merkmale.Count == 0) return Leer(nameof(R.BV_GRUND_NICHT_VERFUEGBAR), kultur);

            List<VariantenDaten> spalten = Spaltenfolge(daten, stamm);
            var t = new Berichtstabelle().Feste(2600);
            foreach (VariantenDaten v in spalten) t.Spalte(v.IstStamm ? Spaltenart.Stamm : Spaltenart.Stand, 0, v.IdProjekt);
            t.MitKopf(new[] { Zellen.Kopf(BerichtTexte.T("Merkmal", englisch), Tabellenausrichtung.Links) }
                .Concat(spalten.Select(v => Zellen.Kopf(Standkopf(v, englisch)))));

            if (ganglinie)
            {
                var quelle = new List<Tabellenzelle> { Zellen.Text(Grund("PVG_AUSWEIS_MERKMAL_QUELLE", kultur)) };
                foreach (VariantenDaten v in spalten)
                {
                    PvGanglinieAusweis a = v.Details?.PvGanglinie;
                    string wert = a != null ? a.Text(kultur) : Grund("PVG_AUSWEIS_MODULMODELL", kultur);
                    quelle.Add(Zellen.Text(wert, Tabellenausrichtung.Links,
                                           Zellen.RolleWenn(v.IstStamm), h: Zellen.StammWenn(v.IstStamm)));
                }
                t.Zeile(quelle);
            }

            foreach (AbweichungsErmittler.Merkmal f in merkmale)
            {
                var zellen = new List<Tabellenzelle> { Zellen.Text(f.Label) };
                foreach (VariantenDaten v in spalten)
                {
                    ProjektDetails d = v.Details;
                    if (ganglinie && d?.PvGanglinie != null)
                    {
                        zellen.Add(Zellen.Text(PvGanglinieAusweis.Entfaellt(kultur), Tabellenausrichtung.Mitte,
                                               Zellen.RolleWenn(v.IstStamm), h: Zellen.StammWenn(v.IstStamm)));
                        continue;
                    }
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
                    Zellen.Text(AbweichungsErmittler.Gewerkname(a.Gewerk)),
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
            // Anwenderentscheid 29.09.2026: Die Zellen tragen die EINZELZAHL je Stand — wie
            // Kostenseite und Übersicht der App. Wo die Gruppenregel „Strombedarf ohne Verwendung"
            // gewirkt hat, nennt eine Fußzeile unter der Tafel daneben die Zahl mit bepreistem
            // Netzbezug und die Menge: unter der Kostentafel die Energiekosten, unter der
            // Emissionstafel das CO₂. Sie werden hier gesammelt und unten angehängt, damit eine
            // Tafel über mehrere Gruppen (Vergleichsgesamt) beide Sätze in ihrer Folge trägt.
            var fussnoten = new List<string>();
            foreach (string gruppe in gruppen)
            {
                List<Kennzahl> zeilen = Gruppenzeilen(daten, gruppe);
                if (zeilen.Count == 0) continue;
                foreach (string fussnote in daten.StromGruppenregelFussnoten(kultur, gruppe))
                    if (!fussnoten.Contains(fussnote)) fussnoten.Add(fussnote);
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
            foreach (string fussnote in fussnoten) t.Hinweis(fussnote);
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
        /// <b><c>stand.tabelle.heizkessel</c></b> (Katalog v11, Konzept Kesselkennlinie 5) — der Betrieb je Heizkessel eines
        /// Stands: Jahresnutzungsgrad η_eff, Anteil des Brennwertbetriebs nach Stunden und nach Wärme, Starts im Jahr.
        /// </summary>
        /// <remarks>
        /// Gelesen werden die Werte des Laufs im Zeitreihensatz (<see cref="ZeitreihenSatz.Kessel"/>) — Brennwertstunden,
        /// Brennwertwärme und Starts stehen nicht im gespeicherten Ergebnis. Ein Kessel ohne Brennwertkennlinie hat keinen
        /// Brennwertbetrieb: Strich statt 0, wie im Kessel-Reiter. Ohne Zeitreihensatz bleibt die Tabelle mit Grund leer,
        /// ohne Kessel ebenso.
        /// </remarks>
        public static Berichtstabelle Heizkessel(VariantenDaten v, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            if (v.Zeitreihen == null) return Leer(nameof(R.BV_GRUND_KEINE_ZEITREIHEN), kultur);
            List<Kesselbetrieb> kessel = v.Zeitreihen.Kessel ?? new List<Kesselbetrieb>();
            if (kessel.Count == 0) return Leer(nameof(R.BV_GRUND_KEIN_HEIZKESSEL), kultur);

            var t = new Berichtstabelle().Feste(2755, 1700, 1700, 1700, 1500);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Heizkessel", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Jahresnutzungsgrad [%]", englisch)),
                Zellen.Kopf(BerichtTexte.T("Brennwertbetrieb Stunden [%]", englisch)),
                Zellen.Kopf(BerichtTexte.T("Brennwertbetrieb Wärme [%]", englisch)),
                Zellen.Kopf(BerichtTexte.T("Starts [1/a]", englisch)),
            });
            Tabellenzelle strich = Zellen.Zahl(Tabellenzelle.STRICH, null, null);
            foreach (Kesselbetrieb k in kessel.Where(k => k != null))
                t.Zeile(new[]
                {
                    Zellen.Text(string.IsNullOrWhiteSpace(k.Name) ? Tabellenzelle.STRICH : k.Name),
                    Zellen.Zahl(Tabellenformat.F(k.JahresnutzungsgradProzent, 1, kultur), k.JahresnutzungsgradProzent, "N1", einheit: "%"),
                    k.MitBrennwertkennlinie
                        ? Zellen.Zahl(Tabellenformat.F(k.BrennwertStundenProzent, 0, kultur), k.BrennwertStundenProzent, "N0", einheit: "%")
                        : strich,
                    k.MitBrennwertkennlinie
                        ? Zellen.Zahl(Tabellenformat.F(k.BrennwertWaermeProzent, 0, kultur), k.BrennwertWaermeProzent, "N0", einheit: "%")
                        : strich,
                    Zellen.Zahl(Tabellenformat.F(k.Starts, 0, kultur), k.Starts, "N0"),
                });
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.bivalenz</c></b> (Katalog v17, UB‑E4, Fachkonzept Übergabegrenze 7.3) — die Tafel „Bivalenz
        /// und Übergabe“: Höchstvorlauf; Übergabegrenze in kW, als Anteil der Heizlast und ihr Rücklauf; Bivalenzpunkte
        /// berechnet, eingegeben und maßgebend; Stunden und Wärme je Betriebsbereich; Anteil nach § 43 GModG. Darunter
        /// die Hinweiszeilen ohne Feld: Stundenmodell, und nur wenn sie zutreffen die Prüfhinweise Anfahrgrenze und
        /// Mindestrücklauf (UB‑Q10).
        /// </summary>
        /// <remarks>
        /// Die Herleitung (Höchstvorlauf, Übergabe, maßgebender und eingegebener Punkt, Hybrid-Anteil) kommt aus den
        /// Projektdaten (<see cref="VariantenDaten.Bivalenz"/>), die berechneten Bivalenzpunkte und die Bereiche aus dem
        /// gespeicherten Lauf. Ohne beides bleibt die Tafel mit Grund leer.
        /// </remarks>
        public static Berichtstabelle Bivalenz(VariantenDaten v, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            BivalenzBerichtswerte b = v.Bivalenz;
            ErgebnisWaermepumpeModel wp = v.Ergebnis?.Waermepumpe;
            Bereichskennzahlen bereiche = wp?.Bereiche;
            Bereichskennzahlen modul = wp?.Module?.Select(m => m?.Bereiche).FirstOrDefault(x => x != null &&
                (x.Bivalenzpunkt_1.HasValue || x.Bivalenzpunkt_2.HasValue || x.Uebergabe_Max_kW.HasValue));
            if (b == null && bereiche == null) return Leer(nameof(R.BV_GRUND_KEINE_BIVALENZ), kultur);

            var t = new Berichtstabelle().Feste(6655, 2700);
            t.MitKopf(new[]
            {
                Zellen.Kopf(Grund(nameof(R.BER_BIV_GROESSE), kultur), Tabellenausrichtung.Links),
                Zellen.Kopf(Grund(nameof(R.BER_BIV_WERT), kultur)),
            });
            void Zeile(string text, double? wert, int stellen)
            {
                bool da = wert.HasValue && !double.IsNaN(wert.Value) && !double.IsInfinity(wert.Value);
                string format = "N" + stellen;
                t.Zeile(new[]
                {
                    Zellen.Text(text),
                    da ? Zellen.Zahl(Tabellenformat.F(wert.Value, stellen, kultur), wert.Value, format)
                       : Zellen.Zahl(Tabellenzelle.STRICH, null, null),
                });
            }
            Zeile(Grund(nameof(R.BER_BIV_HOECHSTVORLAUF), kultur), b?.HoechstvorlaufC, 1);
            Zeile(Grund(nameof(R.BER_BIV_UEBERGABE_KW), kultur), b != null && b.MitUebergabe ? b.UebergabeKw : modul?.Uebergabe_Max_kW, 1);
            Zeile(Grund(nameof(R.BER_BIV_UEBERGABE_ANTEIL), kultur), b?.UebergabeAnteilProzent, 0);
            Zeile(Grund(nameof(R.BER_BIV_UEBERGABE_RUECKLAUF), kultur), b?.RuecklaufC, 1);
            Zeile(Grund(nameof(R.BER_BIV_PUNKT1), kultur), modul?.Bivalenzpunkt_1 ?? b?.ErsterC, 1);
            Zeile(Grund(nameof(R.BER_BIV_PUNKT2), kultur), modul?.Bivalenzpunkt_2 ?? b?.ZweiterC, 1);
            Zeile(Grund(nameof(R.BER_BIV_PUNKT_EINGEGEBEN), kultur), b?.EingegebenC, 1);
            Zeile(Grund(nameof(R.BER_BIV_PUNKT_MASSGEBEND), kultur), b?.MassgebendC, 1);
            string[] namen =
            {
                nameof(R.SIM_BEREICH_WP_ALLEIN), nameof(R.SIM_BEREICH_PARALLEL),
                nameof(R.SIM_BEREICH_VORWAERMUNG), nameof(R.SIM_BEREICH_NUR_KESSEL),
            };
            for (int i = 0; i < namen.Length; i++)
            {
                string bereich = Grund(namen[i], kultur);
                Zeile(string.Format(kultur, Grund(nameof(R.BER_BIV_STUNDEN), kultur), bereich),
                      bereiche?.Stunden[i] is int h ? h : (double?)null, 0);
                Zeile(string.Format(kultur, Grund(nameof(R.BER_BIV_WAERME), kultur), bereich), bereiche?.Mwh[i], 2);
            }
            Zeile(Grund(nameof(R.BER_BIV_HYBRID), kultur), b?.HybridAnteilProzent, 0);
            Zeile(Grund(nameof(R.BER_BIV_HYBRID_MINDEST), kultur), b?.HybridMindestanteilProzent, 0);

            // FK 7.3: die Hinweiszeilen ohne Feld.
            t.Hinweis(Grund(nameof(R.BER_HINWEIS_STUNDENMODELL), kultur));
            if (b != null && b.HinweisAnfahrgrenze) t.Hinweis(Grund(nameof(R.BER_HINWEIS_ANFAHRGRENZE), kultur));
            if (b != null && b.HinweisMindestruecklauf) t.Hinweis(Grund(nameof(R.BER_HINWEIS_MINDESTRUECKLAUF), kultur));
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.km_teillast</c></b> (Katalog v18, KM3‑E3‑b, Fachkonzept Teillast und Takten 5.4) — die Tafel
        /// „Teillast und Takten der Kältemaschinen“: je Maschine mit Teillastweg eine Spalte mit Weg und Herkunft,
        /// Verdichterregelung, C_d, Taktstunden, Starts, Taktstrom, Teillaststunden, Teillastanteil, mittlerem Lastgrad,
        /// extrapolierten Stunden und Jahres-EER ohne Hilfsstrom; darunter der Hinweis zum Stundenmodell.
        /// </summary>
        /// <remarks>
        /// Die Zahlen kommen aus dem gespeicherten Lauf (<c>Tab_ErgebnisKaeltemaschine</c>), die Lesewerte aus den
        /// Projektkopien (<see cref="VariantenDaten.KaeltemaschineTeillast"/>), die Verdichterstunden des Teillastanteils aus
        /// dem Zeitreihensatz. Ohne Maschine mit Weg bleibt die Tafel mit Grund leer.
        /// </remarks>
        public static Berichtstabelle KaeltemaschineTeillast(VariantenDaten v, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(R.BV_GRUND_KEIN_STAND), kultur);
            List<ErgebnisKaeltemaschineModel> alle = v.Ergebnis?.Kaeltemaschinen ?? new List<ErgebnisKaeltemaschineModel>();
            var km = alle.Select((k, i) => (Platz: i, K: k)).Where(x => KaeltemaschineTeillastKennzahlen.MitWeg(x.K)).ToList();
            if (km.Count == 0) return Leer(nameof(R.BV_GRUND_KEINE_KM_TEILLAST), kultur);

            int breite = Math.Max(1200, 5155 / km.Count);
            var t = new Berichtstabelle().Feste(new[] { 4200 }.Concat(Enumerable.Repeat(breite, km.Count)).ToArray());
            t.MitKopf(new[] { Zellen.Kopf(Grund(nameof(R.BER_BIV_GROESSE), kultur), Tabellenausrichtung.Links) }
                .Concat(km.Select(x => Zellen.Kopf(string.IsNullOrWhiteSpace(x.K.Bezeichner) ? "–" : x.K.Bezeichner))).ToArray());

            KaeltemaschineTeillastLesewerte Lese(ErgebnisKaeltemaschineModel k)
                => k.ID_Kaeltemaschine is int id && v.KaeltemaschineTeillast != null
                   && v.KaeltemaschineTeillast.TryGetValue(id, out KaeltemaschineTeillastLesewerte w) ? w : null;
            void Textzeile(string res, Func<KaeltemaschineTeillastLesewerte, string> text)
                => t.Zeile(new[] { Zellen.Text(Grund(res, kultur)) }
                    .Concat(km.Select(x => Lese(x.K) is { } w ? Zellen.Text(text(w), Tabellenausrichtung.Rechts)
                                                              : Zellen.Zahl(Tabellenzelle.STRICH, null, null))).ToArray());
            void Zeile(string res, Func<(int Platz, ErgebnisKaeltemaschineModel K), double?> wert, int stellen)
                => t.Zeile(new[] { Zellen.Text(Grund(res, kultur)) }
                    .Concat(km.Select(x =>
                    {
                        double? z = wert(x);
                        return z is double d && !double.IsNaN(d) && !double.IsInfinity(d)
                            ? Zellen.Zahl(Tabellenformat.F(d, stellen, kultur), d, "N" + stellen)
                            : Zellen.Zahl(Tabellenzelle.STRICH, null, null);
                    })).ToArray());

            Dictionary<int, int> vs = v.Zeitreihen?.KaeltemaschineVerdichterstunden;
            Textzeile(nameof(R.BER_KMT_WEG), w => w.WegText(kultur));
            Textzeile(nameof(R.BER_KMT_REGELUNG), w => w.RegelungText(kultur));
            Textzeile(nameof(R.BER_KMT_CD), w => w.CdText(kultur));
            Zeile(nameof(R.BER_KMT_TAKTSTUNDEN), x => x.K.Taktstunden, 0);
            Zeile(nameof(R.BER_KMT_STARTS), x => x.K.Starts, 0);
            Zeile(nameof(R.BER_KMT_TAKTSTROM), x => x.K.Taktstrom_MWh * 1000.0, 2);
            Zeile(nameof(R.BER_KMT_TEILLASTSTUNDEN), x => x.K.Teillaststunden, 0);
            Zeile(nameof(R.BER_KMT_TEILLASTANTEIL), x => KaeltemaschineTeillastKennzahlen.TeillastanteilProzent(
                x.K.Teillaststunden, vs != null && vs.TryGetValue(x.Platz, out int n) ? n : (int?)null), 1);
            Zeile(nameof(R.BER_KMT_LASTGRAD), x => x.K.Lastgrad_Mittel, 2);
            Zeile(nameof(R.BER_KMT_EXTRAPOLIERT), x => x.K.Stunden_Extrapoliert, 0);
            Zeile(nameof(R.BER_KMT_JAZ), x => KaeltemaschineTeillastKennzahlen.JazVerdichter(x.K), 2);

            t.Hinweis(Grund(nameof(R.BER_HINWEIS_STUNDENMODELL), kultur));
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
