using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine BHKW-Anlage mit den Jahresgrößen, die die Steuerprüfung braucht
    /// (Etappe E4). Reines Eingabe-DTO — die Werte liest
    /// <see cref="WirtschaftlichkeitCtrl"/> aus Anlagen- und Ergebniszeilen.
    /// </summary>
    public sealed class SteuerAnlage
    {
        /// <summary>Bezeichner der Anlagenzeile — ein Datenwert, kein Anzeigetext.</summary>
        public string Bezeichner = "";

        /// <summary>Elektrische Nennleistung [kW] — Bezugsgröße der 2-MW-Grenze
        /// des § 9 Abs. 1 Nr. 3 StromStG, <b>je Anlage</b> und nie als Projektsumme.</summary>
        public double PelKW;

        /// <summary>Brennstoffeinsatz dieser Anlage [MWh/a], <b>heizwertbezogen</b> —
        /// die Größe, die der Rechenkern führt.</summary>
        public double BrennstoffMWh;

        /// <summary>Stromerzeugung [MWh/a].</summary>
        public double StromMWh;

        /// <summary>Wärmeerzeugung [MWh/a].</summary>
        public double WaermeMWh;

        /// <summary>Katalogschlüssel des vollen Steuersatzes nach § 2 EnergieStG
        /// (<c>ENERGIEST_*</c>); leer = dem Energieträger ist kein Satz zugeordnet.</summary>
        public string SchluesselSatzVoll = "";

        /// <summary>Katalogschlüssel des Teilsatzes nach § 53a Abs. 5 EnergieStG
        /// (<c>ENERGIEST_53A5_*</c>); leer = kein Satz zugeordnet.</summary>
        public string SchluesselSatz53a = "";

        /// <summary>Katalogschlüssel des direkten CO₂-Faktors
        /// (<c>EF_BILANZ_EBEV_*</c>, g/kWh Brennstoff, heizwertbezogen); leer = kein
        /// Faktor zugeordnet.</summary>
        public string SchluesselCo2 = "";

        /// <summary>Heizwert je Abrechnungseinheit [kWh/Einheit] aus
        /// <c>Abfrage_Energietraeger_Effektiv</c> (Projektwert vor Katalogwert);
        /// 0 = nicht gepflegt.</summary>
        public double EffHi;

        /// <summary>Brennwert je Abrechnungseinheit [kWh/Einheit], gleiche Quelle;
        /// 0 = nicht gepflegt.</summary>
        public double EffHs;

        /// <summary>Abrechnungseinheit des Energieträgers (<c>L</c>, <c>kg</c>,
        /// <c>m³</c>, <c>kWh</c>) — entscheidet, ob sich die Menge in die gesetzliche
        /// Einheit des Satzes umrechnen lässt.</summary>
        public string Abrechnungseinheit = "";

        /// <summary>true, wenn der Brennstoff fossil ist — nur dann gilt der
        /// CO₂-Grenzwert des § 2 StromStG.</summary>
        public bool Fossil;

        /// <summary>Klartext „Bezeichner (n kW)" für Meldungen — dieselbe Form wie im
        /// KWKG-Guard.</summary>
        public string Klartext(CultureInfo kultur)
        {
            return Bezeichner + " (" + PelKW.ToString("N0", kultur) + " kW)";
        }
    }

    /// <summary>Eingabesatz einer Jahresrechnung der Steuergutschriften (Etappe E4).</summary>
    public sealed class SteuerEingabe
    {
        /// <summary>Unternehmensart, Steuerwert aus <c>DbWerte.UNTERNEHMENSART_*</c>.</summary>
        public string Unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;

        /// <summary>Räumlicher Zusammenhang bestätigt (§ 12b StromStV).</summary>
        public bool RaeumlicherZusammenhang;

        /// <summary>Hocheffizienz nachgewiesen (§ 2 StromStG).</summary>
        public bool HocheffizienzNachweis;

        /// <summary>Jahresnutzungsgrad [%]; <c>null</c> = nicht gepflegt.</summary>
        public double? JahresnutzungsgradProzent;

        /// <summary>Gewählte Entlastungsnorm, Steuerwert aus
        /// <c>DbWerte.ENERGIESTEUER_WAHL_*</c>.</summary>
        public string EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_KEINE;

        /// <summary>Aufteilungsmethode, Steuerwert aus <c>DbWerte.AUFTEILUNG_*</c>.</summary>
        public string AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

        /// <summary>Die BHKW-Anlagen des Projekts; leer = kein BHKW.</summary>
        public List<SteuerAnlage> Anlagen = new List<SteuerAnlage>();

        /// <summary>
        /// KWK-Eigenverbrauch [MWh/a] aus <c>StromMatrix.KwkEigenGesamtMWh</c>.
        /// <c>null</c> = nicht bestimmbar (keine Stundenreihen im Lauf) — dann gibt es
        /// KEINE Befreiung, statt „alles ist Eigenverbrauch" zu unterstellen.
        /// </summary>
        public double? KwkEigenMWh;

        /// <summary>Netzbezug Strom [MWh/a] — Bemessungsgrundlage des § 9b StromStG.</summary>
        public double NetzbezugMWh;
    }

    /// <summary>Ergebnis EINER Jahresrechnung (Etappe E4).</summary>
    public sealed class SteuerErgebnis
    {
        /// <summary>Energiesteuer-Entlastung [€/a] nach der gewählten Norm.</summary>
        public double EnergiesteuerEur;

        /// <summary>Stromsteuer-Befreiung [€/a] nach § 9 Abs. 1 Nr. 3 StromStG.</summary>
        public double StromsteuerBefreiungEur;

        /// <summary>Stromsteuer-Entlastung [€/a] nach § 9b StromStG.</summary>
        public double StromsteuerEntlastungEur;

        /// <summary>Begründungen für jede NICHT gewährte Gutschrift — nie eine stille Null.</summary>
        public readonly List<string> Begruendungen = new List<string>();

        /// <summary>Herkunft der tatsächlich verwendeten Sätze, je Satz eine Zeile.</summary>
        public readonly List<string> Herkunft = new List<string>();

        public double SummeEur
        {
            get { return EnergiesteuerEur + StromsteuerBefreiungEur + StromsteuerEntlastungEur; }
        }
    }

    /// <summary>
    /// Energiesteuer- und Stromsteuergutschriften einer KWK-Anlage (Etappe E4 aus
    /// <c>Konzept_BHKW_Kosten_Erloese.md</c>, Abschnitt 4.2). Faktenbasis:
    /// <c>Grundlagen_KWKG_Energiesteuer_Stromsteuer.md</c>, Abschnitte 2, 3 und 4.
    ///
    /// <para><b>Reine Funktion über DTOs (Leitentscheidung L9).</b> Die Klasse kennt
    /// keine Datenbank. Die gesetzlichen Sätze kommen über einen Auflöser
    /// <c>Func&lt;Schlüssel, GesetzParameter&gt;</c> herein, den der Aufrufer aus
    /// <see cref="GesetzKatalog"/> bildet — damit ist die Rechnung ohne Datenbank
    /// prüfbar und die Stichtagsauflösung bleibt an EINER Stelle.</para>
    ///
    /// <para><b>Einheitendisziplin (Leitentscheidung L3).</b> Jeder Satz steht in SEINER
    /// gesetzlichen Einheit — €/MWh (Erdgas), €/1.000 l (Heizöl EL), €/1.000 kg
    /// (Schweröl, Flüssiggas), €/GJ (Kohle). Umgerechnet wird ausschließlich über die
    /// gepflegten Heizwerte der Abrechnungseinheit. Lässt sich die Menge nicht
    /// umrechnen — etwa weil ein je Liter abgerechneter Träger nach Kilogramm besteuert
    /// wird und keine Dichte gepflegt ist —, gibt es KEINE Gutschrift und eine
    /// Begründung. Genau die Vermischung dieser Einheiten ist der Öl-Fehler der
    /// Altanwendung (Analyse, Befunde 1 und 2).</para>
    ///
    /// <para><b>Steuersatz und Entlastungssatz getrennt (Leitentscheidung L4).</b> Es
    /// wird nie eine Differenz geraten: Regelsatz (20,50 €/MWh), Entlastungssatz
    /// (20,00 €/MWh) und Sockelbetrag (250 €/a) stehen einzeln im Katalog und werden
    /// einzeln gelesen.</para>
    ///
    /// <para><b>Bedingungen werden ausgewiesen, nicht angenommen.</b> Jede nicht
    /// erfüllte oder nicht erfasste Bedingung führt zu 0 € plus verständlicher
    /// Begründung über denselben Meldungsweg wie die KWKG-Guards.</para>
    ///
    /// <para><b>Je Anlage, nicht je Projektsumme.</b> Die 2-MW-Grenze des
    /// § 9 Abs. 1 Nr. 3 StromStG ist eine <b>Anlagen</b>-Nennleistung; sie wird für jede
    /// Anlage einzeln geprüft, und die Befreiung wird — wie beim KWKG-Guard — über den
    /// Stromanteil der verbleibenden Anlagen bereinigt (Restbefund 3 aus
    /// <c>W4_E2_Vollbenutzungsstunden_Protokoll.md</c>, Nachtrag 1 Abschnitt N7).</para>
    /// </summary>
    public static class SteuerGutschriftRechner
    {
        /// <summary>Umrechnung Megawattstunde → Gigajoule.</summary>
        private const double GJ_JE_MWH = 3.6;

        /// <summary>
        /// Rechnet die drei Gutschriften für EIN Kalenderjahr.
        /// </summary>
        /// <param name="e">Mengen und Projektangaben; <c>null</c> ergibt ein leeres Ergebnis.</param>
        /// <param name="jahr">Kalenderjahr — bestimmt über die Stichtagsregel, welcher
        /// Satz gilt. Daraus entstehen die jahresscharfen Reihen (L1).</param>
        /// <param name="satz">Auflöser Schlüssel → Katalogzeile des Jahres;
        /// <c>null</c> = Schlüssel nicht gepflegt.</param>
        /// <param name="kultur">Zahlenformat der Meldungen.</param>
        public static SteuerErgebnis Rechne(SteuerEingabe e, int jahr,
                                            Func<string, GesetzParameter> satz,
                                            CultureInfo kultur)
        {
            var r = new SteuerErgebnis();
            if (e == null || satz == null) return r;
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            Energiesteuer(e, satz, kultur, r);
            StromsteuerBefreiung(e, satz, kultur, r);
            StromsteuerEntlastung(e, satz, kultur, r);
            return r;
        }

        // =====================================================================
        // Energiesteuer — § 53 bzw. § 53a Abs. 5 EnergieStG
        // =====================================================================

        /// <summary>
        /// Energiesteuer-Entlastung auf den <b>BHKW</b>-Brennstoff — nie auf Kessel:
        /// Die Anlagenliste enthält ausschließlich BHKW, und der Rechenkern führt den
        /// Kesselbrennstoff ohnehin getrennt. Genau diese Abgrenzung verlangt die
        /// Anleitung zum Formular 1131 („nur der Erdgasanteil ist entlastungsfähig, der
        /// für den Prozess der Stromerzeugung eingesetzt wurde").
        /// </summary>
        private static void Energiesteuer(SteuerEingabe e, Func<string, GesetzParameter> satz,
                                          CultureInfo kultur, SteuerErgebnis r)
        {
            bool nach53 = string.Equals(e.EnergiesteuerWahl, DbWerte.ENERGIESTEUER_WAHL_53,
                                        StringComparison.Ordinal);
            bool nach53a = string.Equals(e.EnergiesteuerWahl, DbWerte.ENERGIESTEUER_WAHL_53A,
                                         StringComparison.Ordinal);
            if (!nach53 && !nach53a)
            {
                // Der Regelfall für Bestandsprojekte: nichts gewählt, nichts gerechnet.
                // Die Meldung erscheint nur, wenn überhaupt ein BHKW Brennstoff
                // verbraucht — sonst wäre sie an jedem Wärmepumpenprojekt Rauschen.
                if (BrennstoffGesamt(e) > 0)
                    r.Begruendungen.Add(MyResource.Resource.STEUER_ENERGIEST_NICHT_GEWAEHLT);
                return;
            }

            // § 53a Abs. 5 setzt einen Jahresnutzungsgrad von mindestens 70 % voraus
            // (§ 53a Abs. 1 EnergieStG). Die Schwelle steht im Katalog.
            if (nach53a && !NutzungsgradErfuellt(e, satz, kultur, r)) return;

            double summe = 0;
            foreach (SteuerAnlage a in e.Anlagen)
            {
                if (a == null || a.BrennstoffMWh <= 0) continue;

                string schluessel = nach53 ? a.SchluesselSatzVoll : a.SchluesselSatz53a;
                if (string.IsNullOrEmpty(schluessel))
                {
                    r.Begruendungen.Add(string.Format(kultur,
                        MyResource.Resource.STEUER_ENERGIEST_TRAEGER_UNKLAR, a.Klartext(kultur)));
                    continue;
                }

                GesetzParameter p = satz(schluessel);
                if (p == null || !p.Wert.HasValue)
                {
                    r.Begruendungen.Add(string.Format(kultur,
                        MyResource.Resource.STEUER_ENERGIEST_SATZ_FEHLT, a.Klartext(kultur), schluessel));
                    continue;
                }

                // § 53 entlastet den Brennstoff der Stromerzeugung. Welche Menge das ist,
                // entscheidet die gewählte Aufteilungsmethode; § 53a Abs. 5 bemisst sich
                // immer nach dem GESAMTeinsatz.
                double brennstoffMWh = nach53 ? Stromanteil(e.AufteilungMethode, a) : a.BrennstoffMWh;
                if (brennstoffMWh <= 0)
                {
                    r.Begruendungen.Add(string.Format(kultur,
                        MyResource.Resource.STEUER_ENERGIEST_MENGE_UNKLAR, a.Klartext(kultur)));
                    continue;
                }

                string grund;
                double? menge = MengeInGesetzlicherEinheit(p.Einheit, brennstoffMWh, a, kultur, r, out grund);
                if (!menge.HasValue)
                {
                    r.Begruendungen.Add(grund);
                    continue;
                }

                summe += p.Wert.Value * menge.Value;
                r.Herkunft.Add(Herkunft(p, kultur));
            }
            r.EnergiesteuerEur = summe;
        }

        /// <summary>Summe des BHKW-Brennstoffs [MWh/a] über alle Anlagen.</summary>
        private static double BrennstoffGesamt(SteuerEingabe e)
        {
            double s = 0;
            foreach (SteuerAnlage a in e.Anlagen) if (a != null && a.BrennstoffMWh > 0) s += a.BrennstoffMWh;
            return s;
        }

        /// <summary>Prüft die 70-%-Schwelle des § 53a und begründet den Fehlschlag.</summary>
        private static bool NutzungsgradErfuellt(SteuerEingabe e, Func<string, GesetzParameter> satz,
                                                 CultureInfo kultur, SteuerErgebnis r)
        {
            GesetzParameter schwelle = satz(DbWerte.GESETZ_ENERGIEST_53A_NUTZUNGSGRAD);
            if (schwelle == null || !schwelle.Wert.HasValue)
            {
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_SATZ_FEHLT,
                    DbWerte.GESETZ_ENERGIEST_53A_NUTZUNGSGRAD));
                return false;
            }
            if (!e.JahresnutzungsgradProzent.HasValue)
            {
                r.Begruendungen.Add(string.Format(kultur,
                    MyResource.Resource.STEUER_ENERGIEST_53A_NUTZUNGSGRAD_FEHLT,
                    schwelle.Wert.Value.ToString("N0", kultur)));
                return false;
            }
            if (e.JahresnutzungsgradProzent.Value < schwelle.Wert.Value)
            {
                r.Begruendungen.Add(string.Format(kultur,
                    MyResource.Resource.STEUER_ENERGIEST_53A_NUTZUNGSGRAD,
                    e.JahresnutzungsgradProzent.Value.ToString("N1", kultur),
                    schwelle.Wert.Value.ToString("N0", kultur)));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Der auf die Stromerzeugung entfallende Brennstoffanteil [MWh/a].
        ///
        /// <para><b>Vorgabe <c>VOLLER_BRENNSTOFF</c>: keine Aufteilung.</b> Nach
        /// § 53 Abs. 2 Satz 1 EnergieStG gelten Energieerzeugnisse als zur Stromerzeugung
        /// verwendet, „soweit sie in der Stromerzeugungsanlage unmittelbar am
        /// Energieumwandlungsprozess teilnehmen" — beim Motor-BHKW also der gesamte
        /// zugeführte Brennstoff. Die Dienstvorschrift Energieerzeugung sagt zum
        /// Schaubild des § 53 Abs. 1 ausdrücklich, dass Wärme — genutzt oder ungenutzt —
        /// nicht betrachtet wird. Der „Anteil" des § 53 Abs. 1 Satz 2 betrifft die
        /// MECHANISCHE Energie an der Welle (Generator neben Verdichter), nicht die
        /// Wärmeauskopplung.</para>
        ///
        /// <para><b><c>ENERGETISCH</c>: die konservative Wahl.</b> Brennstoff ×
        /// Strom / (Strom + Wärme). Kein Rechtsverfahren, sondern die Auslegung, von der
        /// die Grundlagen bis zur Recherche vom 19.08.2026 ausgingen; sie zeigt die
        /// Untergrenze der Gutschrift.</para>
        /// </summary>
        private static double Stromanteil(string methode, SteuerAnlage a)
        {
            if (string.Equals(methode, DbWerte.AUFTEILUNG_ENERGETISCH, StringComparison.Ordinal))
            {
                double nenner = a.StromMWh + a.WaermeMWh;
                return nenner > 0 ? a.BrennstoffMWh * a.StromMWh / nenner : 0;
            }
            return a.BrennstoffMWh;   // VOLLER_BRENNSTOFF (auch bei leerer Angabe)
        }

        /// <summary>
        /// Rechnet die Brennstoffmenge in die <b>gesetzliche Einheit des Satzes</b> um
        /// (L3). <c>null</c> = nicht umrechenbar; <paramref name="grund"/> trägt dann die
        /// Begründung.
        /// </summary>
        private static double? MengeInGesetzlicherEinheit(string einheit, double brennstoffMWh,
                                                          SteuerAnlage a, CultureInfo kultur,
                                                          SteuerErgebnis r, out string grund)
        {
            grund = null;

            if (string.Equals(einheit, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.Ordinal))
            {
                // Je MWh besteuert werden ausschließlich gasförmige Energieerzeugnisse
                // (§ 2 Abs. 3 Satz 1 Nr. 4 EnergieStG). Bemessen wird die Erdgasmenge in
                // Deutschland BRENNWERTbezogen; der Rechenkern führt dagegen Heizwerte.
                // Umgerechnet wird über die gepflegten Werte der Abrechnungseinheit
                // (Ho/Hi je m³, Projektwert vor Katalogwert) — nicht über einen
                // pauschalen Faktor.
                if (a.EffHs > 0 && a.EffHi > 0)
                {
                    double faktor = a.EffHs / a.EffHi;
                    r.Herkunft.Add(string.Format(kultur, MyResource.Resource.STEUER_ENERGIEST_HO,
                        faktor.ToString("N4", kultur)));
                    return brennstoffMWh * faktor;
                }
                // Ohne gepflegten Brennwert bleibt nur der Heizwert. Das ist die
                // KONSERVATIVE Richtung — die Entlastung fällt rund 10 % zu niedrig aus.
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_ENERGIEST_HO_FEHLT,
                    a.Klartext(kultur)));
                return brennstoffMWh;
            }

            if (string.Equals(einheit, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.Ordinal))
            {
                if (a.EffHi > 0 && IstEinheit(a.Abrechnungseinheit, "l"))
                    return brennstoffMWh * 1000.0 / a.EffHi / 1000.0;
                grund = EinheitGrund(einheit, a, kultur);
                return null;
            }

            if (string.Equals(einheit, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.Ordinal))
            {
                if (a.EffHi > 0 && IstEinheit(a.Abrechnungseinheit, "kg"))
                    return brennstoffMWh * 1000.0 / a.EffHi / 1000.0;
                // Ein je Liter abgerechneter Träger ließe sich nur über die Dichte in
                // Kilogramm umrechnen — energy_carrier.density ist im Bestand nirgends
                // gepflegt. Lieber keine Gutschrift als eine geratene Dichte (L3).
                grund = EinheitGrund(einheit, a, kultur);
                return null;
            }

            if (string.Equals(einheit, DbWerte.GESETZ_EINHEIT_EUR_GJ, StringComparison.Ordinal))
                return brennstoffMWh * GJ_JE_MWH;

            grund = EinheitGrund(einheit, a, kultur);
            return null;
        }

        /// <summary>Abrechnungseinheit vergleichen — tolerant gegen Groß-/Kleinschreibung
        /// (der Katalog führt „L" und „kg").</summary>
        private static bool IstEinheit(string vorhanden, string erwartet)
        {
            return vorhanden != null &&
                   string.Equals(vorhanden.Trim(), erwartet, StringComparison.OrdinalIgnoreCase);
        }

        private static string EinheitGrund(string einheit, SteuerAnlage a, CultureInfo kultur)
        {
            return string.Format(kultur, MyResource.Resource.STEUER_ENERGIEST_EINHEIT_UNKLAR,
                a.Klartext(kultur), einheit,
                string.IsNullOrEmpty(a.Abrechnungseinheit) ? "?" : a.Abrechnungseinheit,
                a.EffHi.ToString("N2", kultur));
        }

        // =====================================================================
        // Stromsteuer — Befreiung § 9 Abs. 1 Nr. 3 StromStG
        // =====================================================================

        /// <summary>
        /// Befreiung des KWK-Eigenverbrauchs. Vier Bedingungen, jede einzeln geprüft und
        /// begründet: elektrische Nennleistung bis 2 MW <b>je Anlage</b>, Hocheffizienz
        /// nachgewiesen, bei fossilem Betrieb unter 270 g CO₂ je kWh Energieertrag,
        /// räumlicher Zusammenhang.
        ///
        /// <para><b>Warum ohne Stundenreihen keine Befreiung.</b>
        /// <see cref="StromMatrix"/> teilt die BHKW-Erzeugung stundenweise in
        /// Eigenverbrauch und Einspeisung; ohne Bedarfsreihe fällt sie auf „alles ist
        /// Eigenverbrauch" zurück. Für den KWK-Zuschlag ist das eine vertretbare
        /// Näherung, für eine gegenüber dem Hauptzollamt geltend gemachte Steuerbefreiung
        /// nicht — deshalb hier 0 € mit Begründung.</para>
        /// </summary>
        private static void StromsteuerBefreiung(SteuerEingabe e, Func<string, GesetzParameter> satz,
                                                 CultureInfo kultur, SteuerErgebnis r)
        {
            if (e.Anlagen.Count == 0) return;   // kein BHKW — nichts zu befreien, nichts zu melden

            if (!e.HocheffizienzNachweis)
            {
                r.Begruendungen.Add(MyResource.Resource.STEUER_STROMST_HOCHEFFIZIENZ);
                return;
            }

            GesetzParameter radius = satz(DbWerte.GESETZ_STROMST_RADIUS_RAEUMLICH);
            if (!e.RaeumlicherZusammenhang)
            {
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_STROMST_RAEUMLICH,
                    radius != null && radius.Wert.HasValue ? radius.Wert.Value.ToString("N1", kultur) : "?"));
                return;
            }

            if (!e.KwkEigenMWh.HasValue)
            {
                r.Begruendungen.Add(MyResource.Resource.STEUER_STROMST_EIGEN_UNKLAR);
                return;
            }

            GesetzParameter regelsatz = satz(DbWerte.GESETZ_STROMST_REGELSATZ);
            if (regelsatz == null || !regelsatz.Wert.HasValue)
            {
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_SATZ_FEHLT,
                    DbWerte.GESETZ_STROMST_REGELSATZ));
                return;
            }

            GesetzParameter grenze = satz(DbWerte.GESETZ_STROMST_GRENZE_BEFREIUNG);
            GesetzParameter co2Grenze = satz(DbWerte.GESETZ_STROMST_CO2_GRENZWERT);
            if (grenze == null || !grenze.Wert.HasValue || co2Grenze == null || !co2Grenze.Wert.HasValue)
            {
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_SATZ_FEHLT,
                    DbWerte.GESETZ_STROMST_GRENZE_BEFREIUNG + " / " + DbWerte.GESETZ_STROMST_CO2_GRENZWERT));
                return;
            }

            // Je Anlage prüfen und die Befreiung — wie beim KWKG-Guard — über den
            // Stromanteil der verbleibenden Anlagen bereinigen. Eine Anlage, auf die
            // MEHRERE Ausschlussgründe zutreffen, fehlt in den Summen genau einmal.
            var ueberGrenze = new List<string>();
            var ueberCo2 = new List<string>();
            var co2Unklar = new List<string>();
            double stromGesamt = 0, stromBefreit = 0, pelBefreit = 0;

            foreach (SteuerAnlage a in e.Anlagen)
            {
                if (a == null) continue;
                stromGesamt += a.StromMWh;

                bool zuGross = a.PelKW > grenze.Wert.Value;
                bool co2Verletzt = false, unklar = false;

                if (a.Fossil)
                {
                    double? spez = Co2JeEnergieertrag(a, satz);
                    if (!spez.HasValue) unklar = true;
                    else co2Verletzt = spez.Value >= co2Grenze.Wert.Value;
                }

                if (zuGross) ueberGrenze.Add(a.Klartext(kultur));
                else if (co2Verletzt) ueberCo2.Add(a.Klartext(kultur));
                else if (unklar) co2Unklar.Add(a.Klartext(kultur));

                if (!zuGross && !co2Verletzt && !unklar)
                {
                    stromBefreit += a.StromMWh;
                    pelBefreit += a.PelKW;
                }
            }

            string rest = pelBefreit.ToString("N0", kultur);
            if (ueberGrenze.Count > 0)
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_STROMST_LEISTUNG,
                    grenze.Wert.Value.ToString("N0", kultur), string.Join(", ", ueberGrenze.ToArray()), rest));
            if (ueberCo2.Count > 0)
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_STROMST_CO2,
                    co2Grenze.Wert.Value.ToString("N0", kultur), string.Join(", ", ueberCo2.ToArray()), rest));
            if (co2Unklar.Count > 0)
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_STROMST_CO2_UNKLAR,
                    string.Join(", ", co2Unklar.ToArray())));

            double anteil = stromGesamt > 0 ? stromBefreit / stromGesamt : 0;
            if (anteil <= 0) return;   // Begründung steht bereits oben

            r.StromsteuerBefreiungEur = regelsatz.Wert.Value * e.KwkEigenMWh.Value * anteil;
            r.Herkunft.Add(Herkunft(regelsatz, kultur));
        }

        /// <summary>
        /// Direkte CO₂-Emissionen je kWh <b>Energieertrag</b> [g/kWh] — die Größe, auf die
        /// § 2 StromStG abstellt: Brennstoff-CO₂ ÷ (Strom + Wärme), nicht ÷ Brennstoff.
        ///
        /// <para><b>Warum der EBeV-Faktor und nicht Anlage 9 des GModG.</b> Gefragt sind
        /// die tatsächlichen direkten Emissionen, nicht ein Nachweiswert des
        /// Gebäuderechts. Genau dafür trennt Leitentscheidung L11 die Klassen
        /// <c>EF_BILANZ</c> und <c>EF_NACHWEIS</c>; verwendet wird
        /// <c>EF_BILANZ_EBEV_*</c> (EBeV 2030, Anlage 2 Teil 4, heizwertbezogen — dieselbe
        /// Bezugsgröße wie der Brennstoff des Rechenkerns).</para>
        ///
        /// <para><c>null</c> = kein Faktor zugeordnet oder kein Energieertrag im Lauf.</para>
        /// </summary>
        private static double? Co2JeEnergieertrag(SteuerAnlage a, Func<string, GesetzParameter> satz)
        {
            if (string.IsNullOrEmpty(a.SchluesselCo2)) return null;
            double ertrag = a.StromMWh + a.WaermeMWh;
            if (ertrag <= 0 || a.BrennstoffMWh <= 0) return null;
            GesetzParameter f = satz(a.SchluesselCo2);
            if (f == null || !f.Wert.HasValue) return null;
            return f.Wert.Value * a.BrennstoffMWh / ertrag;
        }

        // =====================================================================
        // Stromsteuer — Entlastung § 9b StromStG
        // =====================================================================

        /// <summary>
        /// Entlastung des Netzbezugs: <c>max(0, Entlastungssatz × Netzbezug − Sockel)</c>.
        /// Nur für Unternehmen des produzierenden Gewerbes und Betriebe der Land- und
        /// Forstwirtschaft. Der Sockelbetrag von 250 €/a entspricht bei 20,00 €/MWh
        /// einem Netzbezug von 12,5 MWh/a — darunter gibt es nichts.
        /// </summary>
        private static void StromsteuerEntlastung(SteuerEingabe e, Func<string, GesetzParameter> satz,
                                                  CultureInfo kultur, SteuerErgebnis r)
        {
            bool berechtigt =
                string.Equals(e.Unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE, StringComparison.Ordinal) ||
                string.Equals(e.Unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST, StringComparison.Ordinal);

            if (!berechtigt)
            {
                if (e.NetzbezugMWh > 0)
                    r.Begruendungen.Add(MyResource.Resource.STEUER_STROMST_9B_UNTERNEHMENSART);
                return;
            }

            GesetzParameter entlastung = satz(DbWerte.GESETZ_STROMST_ENTLASTUNG_9B);
            if (entlastung == null || !entlastung.Wert.HasValue)
            {
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_SATZ_FEHLT,
                    DbWerte.GESETZ_STROMST_ENTLASTUNG_9B));
                return;
            }

            GesetzParameter sockelZeile = satz(DbWerte.GESETZ_STROMST_SOCKELBETRAG_9B);
            double sockel = sockelZeile != null && sockelZeile.Wert.HasValue ? sockelZeile.Wert.Value : 0;

            double roh = entlastung.Wert.Value * e.NetzbezugMWh;
            double netto = roh - sockel;
            if (netto <= 0)
            {
                if (e.NetzbezugMWh > 0)
                    r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_STROMST_9B_SOCKEL,
                        roh.ToString("N2", kultur), sockel.ToString("N0", kultur)));
                return;
            }

            r.StromsteuerEntlastungEur = netto;
            r.Herkunft.Add(Herkunft(entlastung, kultur));
            if (sockelZeile != null) r.Herkunft.Add(Herkunft(sockelZeile, kultur));
        }

        // =====================================================================
        // Herkunft
        // =====================================================================

        /// <summary>
        /// Herkunftszeile eines verwendeten Satzes: Schlüssel, Wert, Einheit,
        /// Gültigkeitsjahr, Status und Fundstelle — genau das, was
        /// <see cref="GesetzKatalog.WertMitHerkunft"/> liefert.
        /// </summary>
        public static string Herkunft(GesetzParameter p, CultureInfo kultur)
        {
            if (p == null) return "";
            return string.Format(kultur, MyResource.Resource.STEUER_HERKUNFT_FORMAT,
                p.Schluessel,
                p.Wert.HasValue ? p.Wert.Value.ToString("N2", kultur) : "—",
                p.Einheit,
                p.JahrVon.ToString(CultureInfo.InvariantCulture),
                p.Status,
                p.Quelle);
        }
    }
}
