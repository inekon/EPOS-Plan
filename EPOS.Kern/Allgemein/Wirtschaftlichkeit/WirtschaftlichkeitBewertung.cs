using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // ETAPPE E5 — ERGEBNISANSICHT UND V‑A, Teil a (Konzept § 2.11, § 2.13; Mockup
    // Dialog_Formel_Zahlenprobe.html, Kategorie 8, vom Anwender abgenommen 22.09.2026).
    //
    // Hier stehen die Modelle, die die Ergebnisseite und der Wort- und Tabellenbericht
    // GEMEINSAM lesen: die Bandbreite dreier Szenarien (U4) mit der Einstufung je Version
    // (U5), an der Stelle des Hinweistexts (U10) seit E9b der Ausweis „n von m Parametern
    // szenariert", die Deklarationen (V‑A), die Nutzungsdauer-Hinweise (U39) und das
    // Kennzeichen „Nachweis liegt mit der nächsten Rechnung vor" (Nr. 31).
    //
    // ALLES IN DIESER DATEI IST AUSGABE. Kein Kapitalwert, keine Reihe und keine
    // Kennzahl ändert sich dadurch; die Modelle lesen fertige Ergebnisse. Die Seite
    // bekommt sie über ihre Hülle (WirtschaftlichkeitSeiteGaben), der Bericht über den
    // Berichtsdatensammler (BerichtsDaten.Bewertung) — dieselbe Definition, einmal.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// ETAPPE E5 (U4) — eine Zeile der <b>Bandbreite</b>: die Kapitalwertdifferenz eines
    /// Standes zur Referenz in den drei Szenarien, die Spanne dazwischen, die Amortisation
    /// des Erwartungsfalls und die Einstufung.
    /// </summary>
    public sealed class BandbreitenZeile
    {
        /// <summary><c>Tab_Projekt.ID</c> des Standes.</summary>
        public int IdProjekt;

        /// <summary>Anzeigename des Standes, wie ihn der Aufrufer der Gruppe gibt.</summary>
        public string Anzeige = "";

        /// <summary>Ist der Stand das Stammprojekt? (Er kann eine Zeile sein, wenn eine
        /// Variante die Referenz ist.)</summary>
        public bool IstStamm;

        /// <summary>ΔKW im Szenario Worst (ungünstig) [€]; <c>null</c> = nicht gerechnet.</summary>
        public double? Worst;

        /// <summary>ΔKW im Szenario Erwartet [€]; <c>null</c> = nicht gerechnet.</summary>
        public double? Erwartet;

        /// <summary>ΔKW im Szenario Best (günstig) [€]; <c>null</c> = nicht gerechnet.</summary>
        public double? Best;

        /// <summary>
        /// Die <b>Spanne</b> [€] — ETAPPE E5 (Empfehlung Q4, 22.09.2026): der BETRAG
        /// zwischen dem größten und dem kleinsten der drei Szenariowerte, nicht mehr
        /// Best − Worst. Die Etiketten „ungünstig" und „günstig" beschreiben die Sätze,
        /// nicht ihre Wirkung: Ein höherer Zins kann eine Variante mit kleinerer
        /// Investition als die Referenz BESSER stellen, und dann wäre Best − Worst negativ.
        /// Eine reine Ableitung der drei Nachbarwerte, gerechnet wird nichts. Fehlt Worst
        /// oder Best, gibt es keine Spanne: Eine Spanne aus einer Zahl gibt es nicht.
        /// </summary>
        public double? Spanne
        {
            get
            {
                if (!Worst.HasValue || !Best.HasValue) return null;
                double klein = Math.Min(Worst.Value, Best.Value);
                double gross = Math.Max(Worst.Value, Best.Value);
                if (Erwartet.HasValue)
                {
                    klein = Math.Min(klein, Erwartet.Value);
                    gross = Math.Max(gross, Erwartet.Value);
                }
                return gross - klein;
            }
        }

        /// <summary>Dynamische Amortisation des Erwartungsfalls [a]; <c>null</c> = keine.</summary>
        public double? AmortisationJahre;

        /// <summary>
        /// Die Einstufung des Standes (U5) — dieselbe Regel wie der Vorschlag
        /// (<see cref="WirtschaftlichkeitEmpfehlung.Einstufungen"/>); <c>null</c> = keine
        /// (kein Erwartet-Ergebnis gegen die Referenz, oder der Stamm).
        /// </summary>
        public VariantenEmpfehlung Urteil;
    }

    /// <summary>
    /// ETAPPE E5 (U4, U5) — die <b>Bandbreite der Kapitalwertdifferenz</b> über die drei
    /// Szenarien, je Stand eine Zeile, darüber die Referenz, gegen die jede Zahl
    /// gerechnet ist.
    ///
    /// <para><b>Drei vollständige Läufe.</b> Jedes Szenario rechnet den ganzen Lauf mit
    /// seinem eigenen Parametersatz (<see cref="WirtschaftlichkeitCtrl.Berechne(BerichtsDaten, WirtschaftlichkeitParameter, int, bool)"/>
    /// rechnet alle drei); die Bandbreite ist kein Zuschlag auf das Ergebnis. Wer sie ohne
    /// gespeicherten Lauf braucht, nimmt <see cref="WirtschaftlichkeitCtrl.BerechneBandbreite"/>
    /// — dieselbe Rechnung, OHNE zu persistieren (Muster: Sicht 2 aus § 2.15).</para>
    ///
    /// <para><b>Dieselbe Definition wie der Bericht seit E2 (G8):</b> Zeilen sind die Stände
    /// der Gruppe außer der Referenz in Listenreihenfolge, die Zellen
    /// <see cref="WirtschaftlichkeitErgebnis.KapitalwertDiff"/> je Szenario, die Spanne
    /// Best − Worst, dazu die Amortisation des Erwartungsfalls und die Einstufung. Der
    /// Bericht liest künftig dieses Modell, statt die Tafel selbst zu bilden.</para>
    /// </summary>
    public sealed class WirtschaftlichkeitBandbreite
    {
        /// <summary>Die wirksame Referenz (<c>Tab_Projekt.ID</c>); 0 = unbekannt.</summary>
        public int IdReferenz;

        /// <summary>Ihr Anzeigename — der Name der Referenzzeile.</summary>
        public string Referenzname = "";

        /// <summary>Je Stand außer der Referenz eine Zeile, in Listenreihenfolge.</summary>
        public List<BandbreitenZeile> Zeilen = new List<BandbreitenZeile>();

        /// <summary>
        /// Die Einstufungen der Stände (U5) — die Grundlage der Empfehlungskarten und des
        /// Vorschlagssatzes (<see cref="WirtschaftlichkeitEmpfehlung.Vorschlagstext(List{VariantenEmpfehlung}, CultureInfo, string)"/>).
        /// </summary>
        public List<VariantenEmpfehlung> Urteile = new List<VariantenEmpfehlung>();

        /// <summary>Keine Zeile — dann gibt es nichts zu zeigen.</summary>
        public bool Leer { get { return Zeilen.Count == 0; } }

        /// <summary>Die Zeile eines Standes; <c>null</c> = nicht in der Bandbreite.</summary>
        public BandbreitenZeile Zeile(int idProjekt)
        {
            foreach (BandbreitenZeile z in Zeilen) if (z.IdProjekt == idProjekt) return z;
            return null;
        }

        /// <summary>
        /// Bildet die Bandbreite aus fertigen Ergebnissen.
        /// </summary>
        /// <param name="staende">Die Stände der Gruppe in Listenreihenfolge (Id,
        /// Anzeigename), die Referenz eingeschlossen — sie wird zur Referenzzeile.</param>
        /// <param name="alle">Die Ergebnisse aller Szenarien (ein Lauf).</param>
        /// <param name="idReferenz">Die wirksame Referenz, gegen die <c>KapitalwertDiff</c>
        /// gerechnet ist.</param>
        /// <param name="referenzname">Ihr Anzeigename.</param>
        public static WirtschaftlichkeitBandbreite Bilde(IEnumerable<KeyValuePair<int, string>> staende,
                                                         IEnumerable<WirtschaftlichkeitErgebnis> alle,
                                                         int idReferenz, string referenzname)
        {
            var b = new WirtschaftlichkeitBandbreite
            {
                IdReferenz = idReferenz,
                Referenzname = referenzname ?? ""
            };
            if (staende == null) return b;

            var liste = new List<WirtschaftlichkeitErgebnis>();
            if (alle != null)
                foreach (WirtschaftlichkeitErgebnis e in alle)
                    if (e != null) liste.Add(e);

            var gruppe = new List<KeyValuePair<int, string>>();
            var ids = new HashSet<int>();
            foreach (KeyValuePair<int, string> s in staende)
                if (ids.Add(s.Key)) gruppe.Add(s);

            // U5: Die Einstufung urteilt über ALLE drei Szenarien der Stände — dieselbe
            // Menge, aus der der Vorschlagssatz entsteht. Die Referenz trägt keine
            // Differenz und damit kein Urteil. ETAPPE E5 (Q7): JEDER andere Stand bekommt
            // eines — ist eine Variante die Referenz, auch der Stamm.
            var menge = new List<WirtschaftlichkeitErgebnis>();
            foreach (WirtschaftlichkeitErgebnis e in liste)
                if (ids.Contains(e.IdProjekt)) menge.Add(e);
            b.Urteile = WirtschaftlichkeitEmpfehlung.Einstufungen(menge, idReferenz);

            foreach (KeyValuePair<int, string> s in gruppe)
            {
                if (s.Key == idReferenz) continue;
                WirtschaftlichkeitErgebnis erwartet = Finde(liste, s.Key, WirtschaftlichkeitSzenario.ERWARTET);
                var z = new BandbreitenZeile
                {
                    IdProjekt = s.Key,
                    Anzeige = s.Value ?? "",
                    Worst = Diff(liste, s.Key, WirtschaftlichkeitSzenario.WORST),
                    Erwartet = erwartet != null ? erwartet.KapitalwertDiff : null,
                    Best = Diff(liste, s.Key, WirtschaftlichkeitSzenario.BEST),
                    AmortisationJahre = erwartet != null ? erwartet.AmortisationJahre : null
                };
                WirtschaftlichkeitErgebnis irgendeins = erwartet
                    ?? Finde(liste, s.Key, WirtschaftlichkeitSzenario.WORST)
                    ?? Finde(liste, s.Key, WirtschaftlichkeitSzenario.BEST);
                z.IstStamm = irgendeins != null && irgendeins.IstStamm;
                foreach (VariantenEmpfehlung u in b.Urteile)
                    if (u.IdProjekt == s.Key) { z.Urteil = u; break; }
                b.Zeilen.Add(z);
            }
            return b;
        }

        /// <summary>
        /// Die Bandbreite eines BERICHTSLAUFS — die Referenz so aufgelöst, wie Wort- und
        /// Tabellenbericht sie seit E2 auflösen: in Sicht 2 der Stand A, sonst die
        /// Referenz der Gruppe, ohne Wahl der Stamm.
        /// </summary>
        public static WirtschaftlichkeitBandbreite Bilde(BerichtsDaten daten,
                                                         IEnumerable<WirtschaftlichkeitErgebnis> alle)
        {
            if (daten == null) return new WirtschaftlichkeitBandbreite();
            int idGewaehlt = daten.Sicht != null && daten.Sicht.IstPaar
                           ? daten.Sicht.IdA : daten.IdGruppenreferenz;
            return Bilde(daten, alle, idGewaehlt);
        }

        /// <summary>
        /// Dieselbe Bandbreite mit AUSDRÜCKLICH gewählter Referenz (0 = Stamm); die
        /// Auflösung samt benanntem Rückfall steht in <see cref="Referenzwahl"/>.
        /// </summary>
        public static WirtschaftlichkeitBandbreite Bilde(BerichtsDaten daten,
                                                         IEnumerable<WirtschaftlichkeitErgebnis> alle,
                                                         int idGewaehlt)
        {
            if (daten == null) return new WirtschaftlichkeitBandbreite();
            Referenzwahl referenz = Referenzwahl.Bestimme(daten, idGewaehlt);
            var staende = new List<KeyValuePair<int, string>>();
            foreach (VariantenDaten v in daten.Varianten)
                if (v != null) staende.Add(new KeyValuePair<int, string>(v.IdProjekt, v.Anzeige));
            return Bilde(staende, alle, referenz.IdReferenz, referenz.Anzeige);
        }

        private static WirtschaftlichkeitErgebnis Finde(List<WirtschaftlichkeitErgebnis> alle,
                                                        int idProjekt, string szenario)
        {
            foreach (WirtschaftlichkeitErgebnis e in alle)
                if (e.IdProjekt == idProjekt &&
                    string.Equals(e.Szenario, szenario, StringComparison.Ordinal)) return e;
            return null;
        }

        private static double? Diff(List<WirtschaftlichkeitErgebnis> alle, int idProjekt, string szenario)
        {
            WirtschaftlichkeitErgebnis e = Finde(alle, idProjekt, szenario);
            return e == null ? null : e.KapitalwertDiff;
        }
    }

    /// <summary>
    /// ETAPPE E5 (Konzept § 6.3 Nr. 31) — ein Stand, dessen gezeigte Ergebniszeilen keinen
    /// Nachweisumschlag tragen, mit dem Kennzeichen „Nachweis liegt mit der nächsten
    /// Rechnung vor".
    /// </summary>
    public sealed class OhneNachweisStand
    {
        /// <summary><c>Tab_Projekt.ID</c> des Standes.</summary>
        public int IdProjekt;

        /// <summary>Anzeigename des Standes.</summary>
        public string Anzeige = "";

        /// <summary>Der Kennzeichentext (<see cref="ValeriAusweis.NachweisKennzeichen"/>).</summary>
        public string Kennzeichen = "";
    }

    /// <summary>
    /// ETAPPE E5 — <b>die Bewertung einer Vergleichsgruppe</b>, wie Ergebnisseite und
    /// Bericht sie ausweisen: Bandbreite mit Einstufungen und Vorschlagssatz (U4, U5),
    /// Ausweis der Szenarioabdeckung an der Stelle des Hinweistexts (U10, seit E9b),
    /// Deklarationen (V‑A), Nutzungsdauer-Hinweise (U39) und die Stände ohne Nachweis
    /// (Nr. 31).
    ///
    /// <para>Der Berichtsdatensammler legt sie an <see cref="BerichtsDaten.Bewertung"/>;
    /// die Hülle der Seite bildet dieselben Teile aus denselben Kernmethoden. Eine Zahl
    /// entsteht hier nicht.</para>
    /// </summary>
    public sealed class WirtschaftlichkeitBewertung
    {
        /// <summary>U4/U5 — Bandbreite, Einstufungen und Referenz.</summary>
        public WirtschaftlichkeitBandbreite Bandbreite = new WirtschaftlichkeitBandbreite();

        /// <summary>U5 — der Vorschlag zur Entscheidung (<c>WIRT_EMPF_SATZ</c> bzw.
        /// <c>WIRT_EMPF_KEINE</c>), mit der Referenz beim Namen; leer ohne Grundlage.</summary>
        public string Vorschlagstext = "";

        /// <summary>
        /// U10 — an der Stelle des früheren Hinweistexts unter der Annahmentafel steht seit
        /// ETAPPE E9b (Konzept § 2.11.7: „Der Hinweis entfällt mit der Etappe, die ihn
        /// überflüssig macht"; E9b‑Q3, Lesart a) der AUSWEIS der Szenarioabdeckung:
        /// „n von m Parametern szenariert" samt der gepflegten Größen
        /// (<see cref="SzenarioAbdeckung.Satz"/>). Leer ohne Parametersatz.
        /// </summary>
        public string Szenarioabdeckung = "";

        /// <summary>ETAPPE E9b: die Zählung hinter <see cref="Szenarioabdeckung"/> — dieselbe,
        /// die Seite und Checkliste lesen.</summary>
        public SzenarioAbdeckung Abdeckung = new SzenarioAbdeckung();

        /// <summary>V‑A — die Deklarationszeilen (<see cref="ValeriAusweis.Deklarationen"/>).</summary>
        public IReadOnlyList<ValeriDeklaration> Deklarationen = new List<ValeriDeklaration>();

        /// <summary>
        /// ETAPPE E17 (V‑G11, DIN EN 17463 6.1 und 8.2) — die <b>nicht monetarisierbaren
        /// Wirkungen</b> des Stammprojekts (<see cref="ProjektWirkungCtrl.Laden"/>). Wort- und
        /// Tabellenbericht zeigen sie als Tabelle, die Anhang-E-Checkliste liest daraus
        /// „erfasst" und „beurteilt", die Deklaration „benannt". <c>null</c> = nicht gelesen
        /// (Bewertung ohne Berichtslauf) — dann gilt der Freitext wie bis E17.
        /// <b>Keine Rechenwirkung.</b>
        /// </summary>
        public List<ProjektWirkung> Wirkungen;

        /// <summary>U39 — Zeitraumzeile und „k von n Positionen ohne Nutzungsdauer".</summary>
        public NutzungsdauerHinweise Nutzungsdauer = new NutzungsdauerHinweise();

        /// <summary>Nr. 31 — die Stände, deren Ergebniszeilen keinen Nachweis tragen.</summary>
        public List<OhneNachweisStand> OhneNachweis = new List<OhneNachweisStand>();

        /// <summary>
        /// ETAPPE E5 (V‑A, V‑G6) — die <b>Sensitivitätszeilen</b> des Laufs (Szenario
        /// Erwartet, je Stand außer der Referenz), jede mit ihrer Steigung
        /// (<see cref="SensitivitaetZeile.Steigung"/>). Wort- und Tabellenbericht lesen die
        /// Tafel hier statt aus eigener Bildung.
        /// </summary>
        public List<SensitivitaetZeile> Sensitivitaet = new List<SensitivitaetZeile>();

        /// <summary>
        /// ETAPPE E5 (Nr. 31) — die Zeile „‹Stände›: Nachweis liegt mit der nächsten
        /// Rechnung vor" für Seite und Bericht; leer, wenn jeder Stand seinen Nachweis
        /// trägt. Dieselbe Form wie die Nutzungsdauer-Hinweise: die Namen vorn, der Satz
        /// einmal.
        /// </summary>
        public static string Nachweiszeile(IEnumerable<OhneNachweisStand> staende)
        {
            if (staende == null) return "";
            var namen = new List<string>();
            foreach (OhneNachweisStand s in staende)
                if (s != null && !string.IsNullOrEmpty(s.Anzeige) && !namen.Contains(s.Anzeige))
                    namen.Add(s.Anzeige);
            if (namen.Count == 0) return "";
            return string.Join(", ", namen.ToArray()) + ": " +
                   MyResource.Resource.WIRT_NACHWEIS_NAECHSTE_RECHNUNG;
        }

        /// <summary>
        /// Die Stände ohne Nachweisumschlag unter den Ergebnissen — je Stand einmal, in
        /// Listenreihenfolge der Gruppe (Nr. 31).
        /// </summary>
        public static List<OhneNachweisStand> StaendeOhneNachweis(
            IEnumerable<KeyValuePair<int, string>> staende, IEnumerable<WirtschaftlichkeitErgebnis> alle)
        {
            var liste = new List<OhneNachweisStand>();
            if (staende == null || alle == null) return liste;
            var ohne = new Dictionary<int, WirtschaftlichkeitErgebnis>();
            foreach (WirtschaftlichkeitErgebnis e in alle)
                if (e != null && e.OhneNachweis && !ohne.ContainsKey(e.IdProjekt)) ohne[e.IdProjekt] = e;

            var gesehen = new HashSet<int>();
            foreach (KeyValuePair<int, string> s in staende)
            {
                WirtschaftlichkeitErgebnis e;
                if (!gesehen.Add(s.Key) || !ohne.TryGetValue(s.Key, out e)) continue;
                liste.Add(new OhneNachweisStand
                {
                    IdProjekt = s.Key,
                    Anzeige = s.Value ?? "",
                    Kennzeichen = ValeriAusweis.NachweisKennzeichen(e)
                });
            }
            return liste;
        }

        /// <summary>
        /// Die Bewertung eines BERICHTSLAUFS: die Ergebnisse dieses Laufs (oder, wenn die
        /// Rechnung scheiterte, der gespeicherte Stand), der Parametersatz der Gruppe und
        /// die Stände aus <paramref name="daten"/>. Die Referenz folgt der Sicht wie in
        /// Wort- und Tabellenbericht (<see cref="WirtschaftlichkeitBandbreite.Bilde(BerichtsDaten, IEnumerable{WirtschaftlichkeitErgebnis})"/>).
        /// </summary>
        /// <param name="kultur">Die Berichtskultur (<see cref="BerichtTexte.Kultur"/>).</param>
        public static WirtschaftlichkeitBewertung FuerBericht(BerichtsDaten daten,
                                                              IEnumerable<WirtschaftlichkeitErgebnis> alle,
                                                              WirtschaftlichkeitParameter p,
                                                              CultureInfo kultur)
        {
            return FuerBericht(daten, alle, p, kultur, null);
        }

        /// <summary>
        /// Dieselbe Bewertung mit den Sensitivitätszeilen DES LAUFS, der die Ergebnisse
        /// geliefert hat (ETAPPE E5: in Sicht 2 der Lauf gegen A, der nichts speichert).
        /// </summary>
        /// <param name="sensitivitaet">Die Zeilen des Laufs; <c>null</c> = die gespeicherten
        /// der Stände (<see cref="WirtschaftlichkeitCtrl.LadeSensitivitaet"/>) — der
        /// Rückfall des Berichts, wenn die Rechnung scheiterte.</param>
        public static WirtschaftlichkeitBewertung FuerBericht(BerichtsDaten daten,
                                                              IEnumerable<WirtschaftlichkeitErgebnis> alle,
                                                              WirtschaftlichkeitParameter p,
                                                              CultureInfo kultur,
                                                              IEnumerable<SensitivitaetZeile> sensitivitaet)
        {
            var b = new WirtschaftlichkeitBewertung();
            if (daten == null) return b;
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            var staende = new List<KeyValuePair<int, string>>();
            foreach (VariantenDaten v in daten.Varianten)
                if (v != null) staende.Add(new KeyValuePair<int, string>(v.IdProjekt, v.Anzeige));

            b.Bandbreite = WirtschaftlichkeitBandbreite.Bilde(daten, alle);
            b.Vorschlagstext = WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                b.Bandbreite.Urteile, kultur, b.Bandbreite.Referenzname);
            // ETAPPE E9b (U10, E9b‑Q3): der Ausweis „n von m Parametern szenariert" an der
            // Stelle des Hinweistexts — ein Lesefehler kostet die Zeile, nie den Bericht.
            try { b.Abdeckung = SzenarioAbdeckung.Lesen(p, staende); }
            catch { b.Abdeckung = new SzenarioAbdeckung(); }
            b.Szenarioabdeckung = b.Abdeckung.Satz(kultur);
            // ETAPPE E17 (V‑G11): die Wirkungsliste des Stammprojekts - ein Lesefehler kostet
            // die Tabelle, nie den Bericht (leere Liste). „benannt" folgt der Liste.
            try { b.Wirkungen = new ProjektWirkungCtrl().Laden(daten.IdStamm); }
            catch { b.Wirkungen = new List<ProjektWirkung>(); }
            // ETAPPE E15 (V‑G7): die Risikozeile nennt ein gepflegtes Risiko.
            b.Deklarationen = ValeriAusweis.Deklarationen(NichtMonetaereWirkungen.Kurztext(b.Wirkungen), p);
            b.OhneNachweis = StaendeOhneNachweis(staende, alle);
            b.Sensitivitaet = Sensitivitaetszeilen(staende, sensitivitaet, b.Bandbreite.IdReferenz);
            try
            {
                b.Nutzungsdauer = NutzungsdauerHinweisCtrl.Bilde(
                    p != null ? p.Betrachtungszeitraum : 0, staende, kultur);
            }
            catch { b.Nutzungsdauer = new NutzungsdauerHinweise(); }
            return b;
        }

        /// <summary>
        /// ETAPPE E5 (V‑A) — die Sensitivitätszeilen in der Reihenfolge der Stände, ohne
        /// die Referenz (sie trägt keine Differenz). Ohne Zeilen des Laufs die
        /// gespeicherten; ein Lesefehler kostet die Tafel, nie den Bericht.
        /// </summary>
        public static List<SensitivitaetZeile> Sensitivitaetszeilen(
            IEnumerable<KeyValuePair<int, string>> staende,
            IEnumerable<SensitivitaetZeile> zeilen, int idReferenz)
        {
            var liste = new List<SensitivitaetZeile>();
            if (staende == null) return liste;
            var ids = new List<int>();
            foreach (KeyValuePair<int, string> s in staende)
                if (s.Key > 0 && s.Key != idReferenz && !ids.Contains(s.Key)) ids.Add(s.Key);
            if (ids.Count == 0) return liste;

            IEnumerable<SensitivitaetZeile> quelle = zeilen;
            if (quelle == null)
            {
                try { quelle = new WirtschaftlichkeitCtrl().LadeSensitivitaet(new List<int>(ids)); }
                catch { quelle = new List<SensitivitaetZeile>(); }
            }
            foreach (int id in ids)
                foreach (SensitivitaetZeile z in quelle)
                    if (z != null && z.IdProjekt == id) liste.Add(z);
            return liste;
        }
    }
}
