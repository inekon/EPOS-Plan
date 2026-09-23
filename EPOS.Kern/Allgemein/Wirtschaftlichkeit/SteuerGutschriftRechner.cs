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

        /// <summary>ETAPPE K6 — Katalogschlüssel des Teilsatzes nach § 54 EnergieStG
        /// (<c>ENERGIEST_54_*</c>); leer = kein Satz zugeordnet.</summary>
        public string SchluesselSatz54 = "";

        /// <summary>Katalogschlüssel des direkten CO₂-Faktors
        /// (<c>EF_BILANZ_EBEV_*</c>, g/kWh Brennstoff, heizwertbezogen); leer = kein
        /// Faktor zugeordnet. Der Grenzwert des § 2 StromStG wird dennoch
        /// BRENNWERTbezogen geprüft (Konzept § 6.3 Nr. 29) — den Ho-Faktor bildet
        /// <see cref="SteuerGutschriftRechner.Co2JeEnergieertrag"/>.</summary>
        public string SchluesselCo2 = "";

        /// <summary>Heizwert je Abrechnungseinheit [kWh/Einheit] aus
        /// <c>Abfrage_Energietraeger_Effektiv</c> (Projektwert vor Katalogwert);
        /// 0 = nicht gepflegt. Mit <see cref="EffHs"/> die Umrechnung Hi → Ho der
        /// Energiesteuer und der CO₂-Grenzwertprüfung.</summary>
        public double EffHi;

        /// <summary>Brennwert je Abrechnungseinheit [kWh/Einheit], gleiche Quelle;
        /// 0 = nicht gepflegt.</summary>
        public double EffHs;

        /// <summary>Abrechnungseinheit des Energieträgers (<c>L</c>, <c>kg</c>,
        /// <c>m³</c>, <c>kWh</c>) — entscheidet, ob sich die Menge in die gesetzliche
        /// Einheit des Satzes umrechnen lässt.</summary>
        public string Abrechnungseinheit = "";

        /// <summary>
        /// ETAPPE B2 — <c>energy_carrier.id</c> des tatsächlich gerechneten Trägers
        /// (0 = nicht bestimmbar). <b>Nur Ausweis:</b> Die Steuerrechnung liest das Feld
        /// nicht; es verbindet die Anlage mit der Zeile in
        /// <c>energy_project_settings</c>, deren Preiszerlegung die Kohärenzprüfung
        /// (<see cref="KohaerenzPruefung"/>) braucht. Ohne dieses Feld müsste die
        /// Prüfung die Zuordnung Anlage → Träger ein zweites Mal auflösen — eine zweite
        /// Wahrheit über dieselbe Frage.
        /// </summary>
        public int CarrierId;

        /// <summary>true, wenn der Brennstoff fossil ist — nur dann gilt der
        /// CO₂-Grenzwert des § 2 StromStG.</summary>
        public bool Fossil;

        /// <summary>
        /// ETAPPE B3 Paket a (BF6) — die für DIESE Anlage gewählte Entlastungsnorm,
        /// Steuerwert aus <c>DbWerte.ENERGIESTEUER_WAHL_*</c>.
        /// <b><c>null</c> oder leer = kein eigener Wert; dann gilt
        /// <see cref="SteuerEingabe.EnergiesteuerWahl"/>.</b> Genau dieser Rückfall macht
        /// die Etappe für Bestandsprojekte ergebnisneutral: Solange niemand
        /// <c>Tab_Energieanlagen.Energiesteuer_Wahl</c> pflegt, rechnet jede Anlage mit
        /// der Projektwahl — also Zeile für Zeile wie vorher.
        /// </summary>
        public string EnergiesteuerWahl;

        /// <summary>
        /// ETAPPE B3 Paket a — Aufteilungsmethode DIESER Anlage für § 53,
        /// Steuerwert aus <c>DbWerte.AUFTEILUNG_*</c>; <c>null</c> oder leer =
        /// <see cref="SteuerEingabe.AufteilungMethode"/>.
        /// </summary>
        public string AufteilungMethode;

        /// <summary>
        /// ETAPPE B3 Paket a — true, wenn diese Anlage Strom erzeugt (BHKW).
        ///
        /// <para><b>Wozu die Unterscheidung.</b> § 53 und § 53a Abs. 5 EnergieStG
        /// entlasten Energieerzeugnisse, die zur STROMerzeugung verwendet werden; ein
        /// Heizkessel erfüllt den Tatbestand nie. § 54 dagegen hängt an keiner
        /// Stromerzeugung, sondern am produzierenden Gewerbe — er trifft Kessel und BHKW
        /// gleichermaßen (Entscheidung BF5). Eine Kesselanlage mit der Wahl § 53 oder
        /// § 53a rechnet deshalb 0 und bekommt eine Begründung, statt still zu zählen.</para>
        ///
        /// <para><b>Vorgabe true</b>, damit jede vorhandene Konstruktion dieser Klasse
        /// (bis B3 gab es ausschließlich BHKW-Zeilen) unverändert weiterrechnet.</para>
        /// </summary>
        public bool Stromerzeuger = true;

        /// <summary>
        /// Klartext „Bezeichner (n kW)" für Meldungen — dieselbe Form wie im
        /// KWKG-Guard.
        ///
        /// <para><b>ETAPPE E2 (Befund S-3):</b> Die Klammer nennt die ELEKTRISCHE
        /// Nennleistung. Eine Anlage ohne Stromerzeugung führt dort 0, und die Meldung
        /// las sich als „Heizkessel (0 kW)" — eine Angabe, die es an einem Kessel gar
        /// nicht gibt. Sie entfällt deshalb bei <see cref="Stromerzeuger"/> = false;
        /// der Bezeichner steht dann allein.</para>
        /// </summary>
        public string Klartext(CultureInfo kultur)
        {
            if (!Stromerzeuger) return Bezeichner;
            return Bezeichner + " (" + PelKW.ToString("N0", kultur) + " kW)";
        }
    }

    /// <summary>
    /// AUFTRAG 9d — die Positionen, denen der Steuerrechner eine Begründung
    /// zuordnet. Sprachneutrale ASCII-Kennungen; sie stehen in
    /// <see cref="SteuerErgebnis.PositionsGruende"/> und reisen so bis in die
    /// Herleitungszeile der Erlösrubrik.
    /// </summary>
    public static class SteuerPosition
    {
        /// <summary>Energiesteuer nach § 53 bzw. § 53a Abs. 5 EnergieStG.</summary>
        public const string ENERGIEST_53 = "ENERGIEST_53";

        /// <summary>Energiesteuer nach § 54 EnergieStG.</summary>
        public const string ENERGIEST_54 = "ENERGIEST_54";

        /// <summary>Stromsteuer-Befreiung nach § 9 Abs. 1 Nr. 3 StromStG.</summary>
        public const string STROMST_BEFREIUNG = "STROMST_BEFREIUNG";

        /// <summary>Stromsteuer-Entlastung nach § 9b StromStG.</summary>
        public const string STROMST_ENTLASTUNG = "STROMST_ENTLASTUNG";
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

        /// <summary>Gewählte Entlastungsnorm des PROJEKTS, Steuerwert aus
        /// <c>DbWerte.ENERGIESTEUER_WAHL_*</c>. Seit B3 ist sie der RÜCKFALL: Trägt eine
        /// Anlage eine eigene Wahl (<see cref="SteuerAnlage.EnergiesteuerWahl"/>), gilt
        /// diese; sonst gilt der Projektwert.</summary>
        public string EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_KEINE;

        /// <summary>Aufteilungsmethode des PROJEKTS, Steuerwert aus
        /// <c>DbWerte.AUFTEILUNG_*</c>; seit B3 Rückfall zu
        /// <see cref="SteuerAnlage.AufteilungMethode"/>.</summary>
        public string AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

        /// <summary>
        /// Die Anlagen des Projekts, für die eine Energiesteuerentlastung überhaupt in
        /// Betracht kommt; leer = keine.
        ///
        /// <para><b>Seit B3 stehen hier auch HEIZKESSEL</b> (BF5) — erkennbar an
        /// <see cref="SteuerAnlage.Stromerzeuger"/> <c>= false</c>. Die Stromsteuerpfade
        /// (§ 9 Abs. 1 Nr. 3) bleiben davon unberührt: Eine Kesselzeile führt weder
        /// Nennleistung noch Stromerzeugung, sie kann die Bezugsgrößen dort also weder
        /// heben noch senken.</para>
        /// </summary>
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
        /// <summary>
        /// AUFTRAG U7 (Befund B7‑1) — Energiesteuer-Entlastung nach <b>§ 53 bzw.
        /// § 53a Abs. 5 EnergieStG</b> [€/a]: der Teil, der am Brennstoff der
        /// STROMERZEUGUNG hängt und deshalb zum Blockheizkraftwerk gehört.
        ///
        /// <para><b>Warum getrennt.</b> Bis U7 gab der Rechner EINE Summe zurück
        /// (<see cref="EnergiesteuerEur"/>), und die Erlösrubrik konnte beide
        /// Vorschriften nur gemeinsam ausweisen — eine Zeile, deren Titel zwei
        /// Rechtsgrundlagen nannte und die bei zwei Anlagenarten nicht zuzuordnen war.
        /// Getrennt sind es zwei Größen, die je bei ihrer Anlage stehen können.</para>
        ///
        /// <para><b>Kein Sockelbetrag.</b> § 53 und § 53a Abs. 5 haben keinen
        /// (Grundlagen, Abschnitt 4) — der Sockel gehört allein zu § 54.</para>
        /// </summary>
        public double Energiesteuer53Eur;

        /// <summary>
        /// AUFTRAG U7 — Energiesteuer-Entlastung nach <b>§ 54 EnergieStG</b> [€/a],
        /// <b>nach</b> Abzug des Sockelbetrags: der Teil, der am Heizstoff des
        /// produzierenden Gewerbes hängt und deshalb auch einen Kessel trifft.
        /// </summary>
        public double Energiesteuer54Eur;

        /// <summary>
        /// AUFTRAG U7 — der abgezogene Sockelbetrag des § 54 [€/a]; 0 = keine
        /// § 54-Position im Lauf oder kein Sockel im Katalog. Er ist ein
        /// Kalenderjahresbetrag des Antragstellers, keine Anlagengröße, und steht
        /// deshalb neben den Anlagenzeilen statt in ihnen.
        /// </summary>
        public double Energiesteuer54SockelEur;

        /// <summary>
        /// Energiesteuer-Entlastung GESAMT [€/a] — die Summe der beiden
        /// Paragrafenbeträge.
        ///
        /// <para><b>Sie wird nicht mehr gesetzt, sondern gerechnet.</b> Damit ist
        /// zahlengleich, was vor U7 eine einzelne Größe war: Die Erlösreihe des
        /// Kapitalwerts, die Ergebnisspalte und jeder Anker lesen weiterhin diese
        /// Summe, und sie kann sich von den zwei Zeilen darüber nicht lösen.</para>
        /// </summary>
        public double EnergiesteuerEur
        {
            get { return Energiesteuer53Eur + Energiesteuer54Eur; }
        }

        /// <summary>
        /// AUFTRAG U7 — je gerechneter Anlagenposition ein Nachweis mit Paragraf,
        /// Menge in der gesetzlichen Einheit, Satz und Betrag. Leer = keine
        /// Entlastung gerechnet (der Grund steht dann in <see cref="Begruendungen"/>).
        /// </summary>
        public readonly List<EnergiesteuerNachweis> EnergiesteuerNachweise =
            new List<EnergiesteuerNachweis>();

        /// <summary>Stromsteuer-Befreiung [€/a] nach § 9 Abs. 1 Nr. 3 StromStG.</summary>
        public double StromsteuerBefreiungEur;

        /// <summary>Stromsteuer-Entlastung [€/a] nach § 9b StromStG.</summary>
        public double StromsteuerEntlastungEur;

        /// <summary>Begründungen für jede NICHT gewährte Gutschrift — nie eine stille Null.</summary>
        public readonly List<string> Begruendungen = new List<string>();

        /// <summary>
        /// AUFTRAG 9d (Konzept § 6.3, Punkt B7-4) — dieselben Begründungen, aber
        /// <b>ihrer Position zugeordnet</b>: Schlüssel ist eine Kennung aus
        /// <see cref="SteuerPosition"/>, Wert der fertige Satz.
        ///
        /// <para><b>Der Befund, der das erzwang.</b> <see cref="Begruendungen"/> ist
        /// eine flache Liste über alle Vorschriften; im Ergebnis steht sie als EIN mit
        /// „ | " verbundener Text. Welche Zeile der Erlösrubrik zu welchem Satz
        /// gehört, war daraus nicht mehr zu lesen — die Rubrik konnte an einer
        /// Nullzeile nur die BEDINGUNG der Position nennen („nur produzierendes
        /// Gewerbe"), nicht die Feststellung des Laufs. Sie einer Position
        /// nachträglich zuzuordnen hieße, einen Parser zu erfinden; deshalb entsteht
        /// die Zuordnung hier, wo der Grund entsteht.</para>
        ///
        /// <para><b>Der ERSTE Grund je Position gilt.</b> Er ist der, an dem die
        /// Rechnung ausgestiegen ist; alles danach ist Folge. Mehrere Anlagen
        /// derselben Vorschrift melden deshalb nicht mehrere Gründe an dieselbe
        /// Zeile — die vollständige Aufzählung bleibt in
        /// <see cref="Begruendungen"/>.</para>
        /// </summary>
        public readonly Dictionary<string, string> PositionsGruende =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Herkunft der tatsächlich verwendeten Sätze, je Satz eine Zeile.</summary>
        public readonly List<string> Herkunft = new List<string>();

        public double SummeEur
        {
            get { return EnergiesteuerEur + StromsteuerBefreiungEur + StromsteuerEntlastungEur; }
        }
    }

    /// <summary>
    /// KONZEPT § 6.3 Nr. 29 — worauf der CO₂-Faktor bezogen ist, mit dem der Grenzwert
    /// des § 2 StromStG geprüft wird.
    /// </summary>
    internal enum Co2Bezug
    {
        /// <summary>Brennwertbezogener Katalogfaktor (Erdgas: EBeV 181,4 g/kWh H_s).</summary>
        KatalogBrennwert,

        /// <summary>Heizwertbezogener Katalogfaktor, über <c>H_i / H_s</c> des Trägers
        /// auf den Brennwert umgerechnet.</summary>
        Umgerechnet,

        /// <summary>Rückfall ohne gepflegten Brennwert: der heizwertbezogene Faktor,
        /// unverändert — konservativ.</summary>
        Heizwert
    }

    /// <summary>
    /// KONZEPT § 6.3 Nr. 29 — die CO₂-Emissionen einer Anlage je kWh Energieertrag
    /// samt ihrer Herleitung. <b>Nur Rechenweg und Ausweis</b> der Grenzwertprüfung des
    /// § 9 Abs. 1 Nr. 3 StromStG; die Bilanz- und BEHG-Rechnung liest diesen Wert nicht.
    /// </summary>
    internal sealed class Co2Energieertrag
    {
        /// <summary>Worauf der angesetzte Faktor bezogen ist.</summary>
        public Co2Bezug Bezug { get; }

        /// <summary>Der gelesene Katalogschlüssel (<c>EF_BILANZ_EBEV_*</c>).</summary>
        public string Schluessel { get; }

        /// <summary>Der Katalogwert des gelesenen Schlüssels [g CO₂ je kWh Brennstoff].</summary>
        public double KatalogfaktorGJeKwh { get; }

        /// <summary><c>H_i / H_s</c> des Trägers — nur bei <see cref="Co2Bezug.Umgerechnet"/>.</summary>
        public double? QuotientHiHs { get; }

        /// <summary>Der angesetzte Faktor [g CO₂ je kWh Brennstoff] — brennwertbezogen,
        /// außer im Rückfall <see cref="Co2Bezug.Heizwert"/>.</summary>
        public double FaktorGJeKwh { get; }

        /// <summary>Brennstoff-CO₂ je kWh Energieertrag (Strom + Wärme) [g/kWh].</summary>
        public double GrammJeKwh { get; }

        public Co2Energieertrag(Co2Bezug bezug, string schluessel, double katalogfaktor,
                                double? quotientHiHs, double faktor,
                                double brennstoffMWh, double ertragMWh)
        {
            Bezug = bezug;
            Schluessel = schluessel ?? "";
            KatalogfaktorGJeKwh = katalogfaktor;
            QuotientHiHs = quotientHiHs;
            FaktorGJeKwh = faktor;
            GrammJeKwh = ertragMWh > 0 ? faktor * brennstoffMWh / ertragMWh : 0;
        }

        /// <summary>Die Herleitung für Begründung und Herkunftszeile, etwa
        /// „BHKW (300 kW): 218,6 g/kWh (EBeV 181,4 g/kWh, brennwertbezogen)".</summary>
        public string Herleitung(string anlage, CultureInfo kultur)
        {
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            string wert = GrammJeKwh.ToString("N1", kultur);
            string katalog = KatalogfaktorGJeKwh.ToString("N1", kultur);
            switch (Bezug)
            {
                case Co2Bezug.Umgerechnet:
                    return string.Format(kultur, MyResource.Resource.STEUER_STROMST_CO2_FAKTOR_UMGERECHNET,
                        anlage, wert, katalog, (QuotientHiHs ?? 1.0).ToString("N4", kultur));
                case Co2Bezug.Heizwert:
                    return string.Format(kultur, MyResource.Resource.STEUER_STROMST_CO2_FAKTOR_HI,
                        anlage, wert, katalog);
                default:
                    return string.Format(kultur, MyResource.Resource.STEUER_STROMST_CO2_FAKTOR_HO,
                        anlage, wert, katalog);
            }
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
        /// Energiesteuer-Entlastung, <b>je Anlage</b> geprüft und begründet
        /// (Etappe B3 Paket a, Konzept § 4.2, Entscheidungen BF5 und BF6).
        ///
        /// <para><b>Was sich gegenüber E4/K6 geändert hat.</b> Bis B3 galt EINE Wahl für
        /// das ganze Projekt: Sie wurde einmal vor der Anlagenschleife ausgewertet, und
        /// eine gescheiterte Bedingung (Nutzungsgrad, Unternehmensart) brach die ganze
        /// Rechnung ab. Jetzt löst jede Anlage ihre Wahl selbst auf
        /// (<c>Anlagenwert ?? Projektwert</c>), und eine Anlage, die an einer Bedingung
        /// scheitert, nimmt die übrigen nicht mit. Ohne gepflegte Anlagenwahlen ist das
        /// Zeile für Zeile die alte Rechnung — jede Anlage löst dann denselben
        /// Projektwert auf.</para>
        ///
        /// <para><b>§ 53 und § 53a nur für Stromerzeuger.</b> Beide entlasten
        /// Energieerzeugnisse, die zur Stromerzeugung verwendet werden — die Anleitung zum
        /// Formular 1131 sagt es wörtlich („nur der Erdgasanteil ist entlastungsfähig, der
        /// für den Prozess der Stromerzeugung eingesetzt wurde"). Ein Heizkessel
        /// (<see cref="SteuerAnlage.Stromerzeuger"/> <c>= false</c>) erfüllt den Tatbestand
        /// nie und rechnet mit diesen Wahlen 0 plus Begründung. § 54 dagegen hängt an
        /// keiner Stromerzeugung, sondern am produzierenden Gewerbe (BF5) und trifft
        /// beide Anlagenarten.</para>
        ///
        /// <para><b>Der Sockelbetrag bleibt EINMAL je Lauf</b> — er ist ein
        /// Kalenderjahresbetrag des Antragstellers, keine Anlagengröße. Abgezogen wird er
        /// vom § 54-TEIL der Summe; ein § 53-Betrag derselben Rechnung wird davon nicht
        /// berührt (sonst zahlte eine Anlage den Sockel einer anderen).</para>
        /// </summary>
        private static void Energiesteuer(SteuerEingabe e, Func<string, GesetzParameter> satz,
                                          CultureInfo kultur, SteuerErgebnis r)
        {
            // Trägt KEINE Anlage eine Wahl — weder eigene noch über den Projektrückfall —,
            // ist der Regelfall für Bestandsprojekte erreicht: nichts gewählt, nichts
            // gerechnet. Die Meldung erscheint nur, wenn überhaupt Brennstoff im Spiel
            // ist; sonst wäre sie an jedem Wärmepumpenprojekt Rauschen.
            if (!IrgendeineWahl(e))
            {
                // AUFTRAG 9d: Dieser Satz gilt BEIDEN Paragrafenzeilen — es ist
                // überhaupt nichts gewählt, also fehlt beiden die Grundlage.
                if (BrennstoffGesamt(e) > 0)
                {
                    Grund(r, SteuerPosition.ENERGIEST_53,
                          MyResource.Resource.STEUER_ENERGIEST_NICHT_GEWAEHLT);
                    r.PositionsGruende[SteuerPosition.ENERGIEST_54] =
                          MyResource.Resource.STEUER_ENERGIEST_NICHT_GEWAEHLT;
                }
                return;
            }

            // Die 70-%-Prüfung des § 53a bleibt eine PROJEKTgröße (der Jahresnutzungsgrad
            // steht in Tab_ProjektWirtschaftlichkeit). Sie wird deshalb höchstens EINMAL
            // ausgeführt und ihre Begründung höchstens einmal angehängt — mehrere
            // § 53a-Anlagen würden sonst denselben Satz mehrfach melden. Neu ist allein,
            // dass sie den Lauf nicht mehr abbricht: Anlagen mit anderer Wahl rechnen
            // weiter.
            bool? nutzungsgradOk = null;

            // AUFTRAG U7 — zwei Töpfe statt einer Summe. Bis U7 lief eine Gesamtsumme
            // mit, von der am Ende der Sockel abgezogen wurde; der § 54-Teil war nur
            // ein Hilfswert dafür. Jetzt sind beide Paragrafen das ERGEBNIS, und die
            // Gesamtsumme entsteht erst aus ihnen (SteuerErgebnis.EnergiesteuerEur).
            double summe53 = 0;          // § 53 und § 53a Abs. 5 — ohne Sockelbetrag
            double summe54 = 0;          // § 54 — der Teil, den der Sockel mindert

            foreach (SteuerAnlage a in e.Anlagen)
            {
                if (a == null || a.BrennstoffMWh <= 0) continue;

                string wahl = Wahl(a, e);
                bool nach53 = string.Equals(wahl, DbWerte.ENERGIESTEUER_WAHL_53, StringComparison.Ordinal);
                bool nach53a = string.Equals(wahl, DbWerte.ENERGIESTEUER_WAHL_53A, StringComparison.Ordinal);
                // ETAPPE K6 — § 54 EnergieStG als dritte Wahl. Er hat, anders als § 53
                // und § 53a, zwei zusätzliche Bedingungen: die Unternehmensart und einen
                // Sockelbetrag von 250 €/a.
                bool nach54 = string.Equals(wahl, DbWerte.ENERGIESTEUER_WAHL_54, StringComparison.Ordinal);
                if (!nach53 && !nach53a && !nach54) continue;   // diese Anlage: keine Wahl

                // ETAPPE B3 — § 53/§ 53a setzen Stromerzeugung voraus.
                if (!a.Stromerzeuger && !nach54)
                {
                    Grund(r, SteuerPosition.ENERGIEST_53, string.Format(kultur,
                        T("STEUER_ENERGIEST_NUR_54",
                          "{0}: § 53 und § 53a Abs. 5 EnergieStG entlasten nur Anlagen mit " +
                          "Stromerzeugung. Für diese Anlage kommt allein § 54 EnergieStG in Betracht."),
                        a.Klartext(kultur)));
                    continue;
                }

                if (nach53a)
                {
                    if (!nutzungsgradOk.HasValue)
                        nutzungsgradOk = NutzungsgradErfuellt(e, satz, kultur, r);
                    if (!nutzungsgradOk.Value) continue;
                }

                // § 54 setzt ein Unternehmen des produzierenden Gewerbes bzw. einen
                // Betrieb der Land- und Forstwirtschaft voraus — dieselbe Bedingung wie
                // § 9b. Die Begründung wird über die Deduplizierung in
                // WirtschaftlichkeitCtrl.BaueSteuerReihen ohnehin nur einmal ausgegeben.
                //
                // ENTFALLEN mit B3: die Pauschalzeile STEUER_ENERGIEST_54_BEMESSUNG
                // („die Bemessungsgrundlage ist eine bewusste Lücke"). Sie war richtig,
                // solange § 54 nur den BHKW-Brennstoff sah; jetzt steht der
                // Kesselbrennstoff in der Anlagenliste, und die Lücke ist geschlossen.
                // Der Ressourcenschlüssel bleibt in beiden .resx stehen — er wird nur
                // nicht mehr erzeugt.
                if (nach54 && !ProduzierendesGewerbe(e))
                {
                    Grund(r, SteuerPosition.ENERGIEST_54,
                          MyResource.Resource.STEUER_ENERGIEST_54_UNTERNEHMENSART);
                    continue;
                }

                // AUFTRAG 9d — ab hier hängt die Position an der Wahl DIESER Anlage:
                // Was unter § 54 scheitert, steht in der § 54-Zeile, alles übrige in
                // der § 53/§ 53a-Zeile.
                string position = nach54 ? SteuerPosition.ENERGIEST_54
                                         : SteuerPosition.ENERGIEST_53;

                string schluessel = nach53 ? a.SchluesselSatzVoll
                                           : (nach54 ? a.SchluesselSatz54 : a.SchluesselSatz53a);
                if (string.IsNullOrEmpty(schluessel))
                {
                    Grund(r, position, string.Format(kultur,
                        MyResource.Resource.STEUER_ENERGIEST_TRAEGER_UNKLAR, a.Klartext(kultur)));
                    continue;
                }

                GesetzParameter p = satz(schluessel);
                if (p == null || !p.Wert.HasValue)
                {
                    Grund(r, position, string.Format(kultur,
                        MyResource.Resource.STEUER_ENERGIEST_SATZ_FEHLT, a.Klartext(kultur), schluessel));
                    continue;
                }

                // § 53 entlastet den Brennstoff der Stromerzeugung. Welche Menge das ist,
                // entscheidet die gewählte Aufteilungsmethode — seit B3 die dieser Anlage,
                // ersatzweise die des Projekts; § 53a Abs. 5 und § 54 bemessen sich immer
                // nach dem GESAMTeinsatz.
                double brennstoffMWh = nach53 ? Stromanteil(Methode(a, e), a) : a.BrennstoffMWh;
                if (brennstoffMWh <= 0)
                {
                    Grund(r, position, string.Format(kultur,
                        MyResource.Resource.STEUER_ENERGIEST_MENGE_UNKLAR, a.Klartext(kultur)));
                    continue;
                }

                string grund;
                double? menge = MengeInGesetzlicherEinheit(p.Einheit, brennstoffMWh, a, kultur, r, out grund);
                if (!menge.HasValue)
                {
                    Grund(r, position, grund);
                    continue;
                }

                double betrag = p.Wert.Value * menge.Value;
                if (nach54) summe54 += betrag; else summe53 += betrag;

                // AUFTRAG U7 — der Nachweis DIESER Position: Paragraf, Menge in der
                // gesetzlichen Einheit des Satzes, Satz und Betrag. Er ist der
                // einzige Ort, an dem die drei Zahlen zusammen stehen; die Rubrik
                // schreibt daraus ihre Herleitungszeile, statt sie nachzurechnen.
                r.EnergiesteuerNachweise.Add(new EnergiesteuerNachweis
                {
                    Anlage = a.Bezeichner,
                    Paragraf = nach54 ? EnergiesteuerNachweis.PARAGRAF_54
                             : (nach53a ? EnergiesteuerNachweis.PARAGRAF_53A
                                        : EnergiesteuerNachweis.PARAGRAF_53),
                    Menge = menge.Value,
                    Einheit = p.Einheit,
                    SatzEur = p.Wert.Value,
                    BetragEur = betrag
                });
                r.Herkunft.Add(Herkunft(p, kultur));
            }

            // ETAPPE K6 — Sockelbetrag. Nur § 54 hat einen (250 €/Kalenderjahr);
            // § 53 und § 53a haben keinen (Grundlagen, Abschnitt 4). Er wird VOR dem
            // Ausweis abgezogen — dieselbe Mechanik wie bei § 9b StromStG.
            //
            // ETAPPE B3: Bezugsgröße ist der § 54-TEIL. Bei einem reinen § 54-Lauf (dem
            // einzigen, den es bis B3 geben konnte) ist summe54 == summeGesamt, die
            // Rechnung also zeilengleich der bisherigen. Deckt der Sockel den § 54-Teil,
            // entfällt genau dieser Teil — ein § 53-Betrag derselben Rechnung bleibt.
            if (summe54 > 0)
            {
                GesetzParameter sockelZeile = satz(DbWerte.GESETZ_ENERGIEST_54_SOCKELBETRAG);
                double sockel = sockelZeile != null && sockelZeile.Wert.HasValue
                              ? sockelZeile.Wert.Value : 0;
                double netto = summe54 - sockel;
                if (netto <= 0)
                {
                    Grund(r, SteuerPosition.ENERGIEST_54, string.Format(kultur,
                        MyResource.Resource.STEUER_ENERGIEST_54_SOCKEL,
                        summe54.ToString("N2", kultur), sockel.ToString("N0", kultur)));
                    r.Energiesteuer54SockelEur = sockel;
                    summe54 = 0;                  // 0 € mit Hinweis, nie eine stille Null
                }
                else
                {
                    r.Energiesteuer54SockelEur = sockel;
                    summe54 = netto;
                    if (sockelZeile != null) r.Herkunft.Add(Herkunft(sockelZeile, kultur));
                }
            }

            r.Energiesteuer53Eur = summe53;
            r.Energiesteuer54Eur = summe54;
        }

        /// <summary>
        /// Die für eine Anlage geltende Entlastungsnorm: ihr eigener Wert, ersatzweise
        /// der Projektwert (B3, BF6). Leer und <c>null</c> heißen beide „kein eigener
        /// Wert" — das Rückfallmuster der E6-Textspalten.
        /// </summary>
        private static string Wahl(SteuerAnlage a, SteuerEingabe e)
        {
            string eigen = a.EnergiesteuerWahl == null ? null : a.EnergiesteuerWahl.Trim();
            return string.IsNullOrEmpty(eigen) ? e.EnergiesteuerWahl : eigen;
        }

        /// <summary>Die für eine Anlage geltende Aufteilungsmethode — Rückfall wie bei
        /// <see cref="Wahl"/>.</summary>
        private static string Methode(SteuerAnlage a, SteuerEingabe e)
        {
            string eigen = a.AufteilungMethode == null ? null : a.AufteilungMethode.Trim();
            return string.IsNullOrEmpty(eigen) ? e.AufteilungMethode : eigen;
        }

        /// <summary>
        /// true, sobald IRGENDEINE Anlage eine der drei Normen auflöst. Nur dann ist
        /// überhaupt etwas gewählt — und nur dann darf die Meldung „keine Entlastung
        /// gewählt" ausbleiben.
        /// </summary>
        private static bool IrgendeineWahl(SteuerEingabe e)
        {
            foreach (SteuerAnlage a in e.Anlagen)
            {
                if (a == null) continue;
                string w = Wahl(a, e);
                if (string.Equals(w, DbWerte.ENERGIESTEUER_WAHL_53, StringComparison.Ordinal) ||
                    string.Equals(w, DbWerte.ENERGIESTEUER_WAHL_53A, StringComparison.Ordinal) ||
                    string.Equals(w, DbWerte.ENERGIESTEUER_WAHL_54, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Anzeigetext über den Ressourcenschlüssel, mit deutschem Rückfall — dasselbe
        /// Muster wie in <see cref="KohaerenzPruefung"/>. Es hält neue Texte aus
        /// <c>MyResource/Resource.Designer.cs</c> heraus, die Visual Studio selbst
        /// regeneriert.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

        /// <summary>
        /// AUFTRAG 9d — eine Begründung EINMAL schreiben, an ZWEI Stellen: in die
        /// flache Liste <see cref="SteuerErgebnis.Begruendungen"/>, die unverändert
        /// ins Hinweisfeld des Laufs wandert, und unter ihre Position in
        /// <see cref="SteuerErgebnis.PositionsGruende"/>, aus der die Erlösrubrik
        /// ihre Herleitungszeile schreibt.
        ///
        /// <para>Die Liste bleibt dabei wortgleich zu vorher — auch Doppelungen: Sie
        /// werden erst in <c>WirtschaftlichkeitCtrl.BaueSteuerReihen</c> entfernt,
        /// und diese Arbeitsteilung wird hier nicht verschoben. In der Zuordnung
        /// gewinnt dagegen der ERSTE Grund; er ist der, an dem die Rechnung
        /// ausgestiegen ist.</para>
        /// </summary>
        private static void Grund(SteuerErgebnis r, string position, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            r.Begruendungen.Add(text);
            if (!r.PositionsGruende.ContainsKey(position)) r.PositionsGruende[position] = text;
        }

        /// <summary>Produzierendes Gewerbe oder Land- und Forstwirtschaft — die
        /// gemeinsame Voraussetzung von § 9b StromStG und § 54 EnergieStG (K6).</summary>
        /// <summary>
        /// Unternehmensart mit Entlastungsanspruch — produzierendes Gewerbe oder Land-
        /// und Forstwirtschaft. <b>Seit B7 oeffentlich:</b> Die Erloesrubrik (Konzept
        /// § 2.6) kennzeichnet damit die Zeilen A5, A6 und B1 als „nur produzierendes
        /// Gewerbe" und rechnet die § 9b-Korrektur des Ausweises nur dort. Eine zweite
        /// Fassung derselben Pruefung waere eine zweite Antwort auf dieselbe Frage.
        /// </summary>
        public static bool ProduzierendesGewerbe(SteuerEingabe e)
        {
            return string.Equals(e.Unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                                 StringComparison.Ordinal) ||
                   string.Equals(e.Unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST,
                                 StringComparison.Ordinal);
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
                Grund(r, SteuerPosition.ENERGIEST_53, string.Format(kultur,
                    MyResource.Resource.STEUER_SATZ_FEHLT,
                    DbWerte.GESETZ_ENERGIEST_53A_NUTZUNGSGRAD));
                return false;
            }
            if (!e.JahresnutzungsgradProzent.HasValue)
            {
                Grund(r, SteuerPosition.ENERGIEST_53, string.Format(kultur,
                    MyResource.Resource.STEUER_ENERGIEST_53A_NUTZUNGSGRAD_FEHLT,
                    schwelle.Wert.Value.ToString("N0", kultur)));
                return false;
            }
            if (e.JahresnutzungsgradProzent.Value < schwelle.Wert.Value)
            {
                Grund(r, SteuerPosition.ENERGIEST_53, string.Format(kultur,
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
                Grund(r, SteuerPosition.STROMST_BEFREIUNG,
                      MyResource.Resource.STEUER_STROMST_HOCHEFFIZIENZ);
                return;
            }

            GesetzParameter radius = satz(DbWerte.GESETZ_STROMST_RADIUS_RAEUMLICH);
            if (!e.RaeumlicherZusammenhang)
            {
                Grund(r, SteuerPosition.STROMST_BEFREIUNG, string.Format(kultur,
                      MyResource.Resource.STEUER_STROMST_RAEUMLICH,
                    radius != null && radius.Wert.HasValue ? radius.Wert.Value.ToString("N1", kultur) : "?"));
                return;
            }

            if (!e.KwkEigenMWh.HasValue)
            {
                Grund(r, SteuerPosition.STROMST_BEFREIUNG,
                      MyResource.Resource.STEUER_STROMST_EIGEN_UNKLAR);
                return;
            }

            GesetzParameter regelsatz = satz(DbWerte.GESETZ_STROMST_REGELSATZ);
            if (regelsatz == null || !regelsatz.Wert.HasValue)
            {
                Grund(r, SteuerPosition.STROMST_BEFREIUNG, string.Format(kultur,
                      MyResource.Resource.STEUER_SATZ_FEHLT, DbWerte.GESETZ_STROMST_REGELSATZ));
                return;
            }

            GesetzParameter grenze = satz(DbWerte.GESETZ_STROMST_GRENZE_BEFREIUNG);
            GesetzParameter co2Grenze = satz(DbWerte.GESETZ_STROMST_CO2_GRENZWERT);
            if (grenze == null || !grenze.Wert.HasValue || co2Grenze == null || !co2Grenze.Wert.HasValue)
            {
                Grund(r, SteuerPosition.STROMST_BEFREIUNG, string.Format(kultur,
                      MyResource.Resource.STEUER_SATZ_FEHLT,
                      DbWerte.GESETZ_STROMST_GRENZE_BEFREIUNG + " / " + DbWerte.GESETZ_STROMST_CO2_GRENZWERT));
                return;
            }

            // Je Anlage prüfen und die Befreiung — wie beim KWKG-Guard — über den
            // Stromanteil der verbleibenden Anlagen bereinigen. Eine Anlage, auf die
            // MEHRERE Ausschlussgründe zutreffen, fehlt in den Summen genau einmal.
            var ueberGrenze = new List<string>();
            var ueberCo2 = new List<string>();
            var co2Unklar = new List<string>();
            // KONZEPT § 6.3 Nr. 29 — die Herleitung des CO₂-Werts je Anlage (brennwert-
            // bezogen) und die Anlagen, für die kein Brennwert gepflegt ist.
            var co2Werte = new List<string>();
            var co2Heizwert = new List<string>();
            double stromGesamt = 0, stromBefreit = 0, pelBefreit = 0;

            foreach (SteuerAnlage a in e.Anlagen)
            {
                if (a == null) continue;
                stromGesamt += a.StromMWh;

                bool zuGross = a.PelKW > grenze.Wert.Value;
                bool co2Verletzt = false, unklar = false;
                string co2Text = null;

                if (a.Fossil)
                {
                    Co2Energieertrag co2 = Co2JeEnergieertrag(a, satz);
                    if (co2 == null) unklar = true;
                    else
                    {
                        co2Verletzt = co2.GrammJeKwh >= co2Grenze.Wert.Value;
                        co2Text = co2.Herleitung(a.Klartext(kultur), kultur);
                        co2Werte.Add(co2Text);
                        if (co2.Bezug == Co2Bezug.Heizwert) co2Heizwert.Add(a.Klartext(kultur));
                    }
                }

                if (zuGross) ueberGrenze.Add(a.Klartext(kultur));
                else if (co2Verletzt) ueberCo2.Add(co2Text);
                else if (unklar) co2Unklar.Add(a.Klartext(kultur));

                if (!zuGross && !co2Verletzt && !unklar)
                {
                    stromBefreit += a.StromMWh;
                    pelBefreit += a.PelKW;
                }
            }

            string rest = pelBefreit.ToString("N0", kultur);
            if (ueberGrenze.Count > 0)
                Grund(r, SteuerPosition.STROMST_BEFREIUNG, string.Format(kultur,
                      MyResource.Resource.STEUER_STROMST_LEISTUNG,
                    grenze.Wert.Value.ToString("N0", kultur), string.Join(", ", ueberGrenze.ToArray()), rest));
            if (ueberCo2.Count > 0)
                Grund(r, SteuerPosition.STROMST_BEFREIUNG, string.Format(kultur,
                      MyResource.Resource.STEUER_STROMST_CO2,
                    co2Grenze.Wert.Value.ToString("N0", kultur), string.Join("; ", ueberCo2.ToArray()), rest));
            if (co2Unklar.Count > 0)
                Grund(r, SteuerPosition.STROMST_BEFREIUNG, string.Format(kultur,
                      MyResource.Resource.STEUER_STROMST_CO2_UNKLAR,
                    string.Join(", ", co2Unklar.ToArray())));

            // Nr. 29: Ohne gepflegten Brennwert bleibt nur der heizwertbezogene Faktor —
            // die KONSERVATIVE Richtung, wie bei der Brennwertmenge der Energiesteuer.
            // Ein Hinweis, kein Grund einer Nullzeile: Er steht deshalb nur unter den
            // Begründungen, nicht in PositionsGruende.
            if (co2Heizwert.Count > 0)
                r.Begruendungen.Add(string.Format(kultur, MyResource.Resource.STEUER_STROMST_CO2_HEIZWERT,
                    string.Join(", ", co2Heizwert.ToArray())));

            double anteil = stromGesamt > 0 ? stromBefreit / stromGesamt : 0;
            if (anteil <= 0) return;   // Begründung steht bereits oben

            r.StromsteuerBefreiungEur = regelsatz.Wert.Value * e.KwkEigenMWh.Value * anteil;
            r.Herkunft.Add(Herkunft(regelsatz, kultur));

            // Nr. 29: die Herleitung der CO₂-Prüfung — je fossiler Anlage der
            // brennwertbezogene Wert und der Faktor, aus dem er entstand.
            if (co2Werte.Count > 0)
                r.Herkunft.Add(string.Format(kultur, MyResource.Resource.STEUER_STROMST_CO2_HERLEITUNG,
                    string.Join("; ", co2Werte.ToArray()), co2Grenze.Wert.Value.ToString("N0", kultur)));
        }

        /// <summary>
        /// Direkte CO₂-Emissionen je kWh <b>Energieertrag</b> [g/kWh] — die Größe, auf die
        /// § 2 StromStG abstellt: Brennstoff-CO₂ ÷ (Strom + Wärme), nicht ÷ Brennstoff.
        ///
        /// <para><b>Warum der EBeV-Faktor und nicht Anlage 9 des GModG.</b> Gefragt sind
        /// die tatsächlichen direkten Emissionen, nicht ein Nachweiswert des
        /// Gebäuderechts. Genau dafür trennt Leitentscheidung L11 die Klassen
        /// <c>EF_BILANZ</c> und <c>EF_NACHWEIS</c>; verwendet wird
        /// <c>EF_BILANZ_EBEV_*</c> (EBeV 2030, Anlage 2 Teil 4).</para>
        ///
        /// <para><b>ES GILT IMMER DER BRENNWERT</b> (Konzept § 6.3 Nr. 29, Register R‑NR
        /// Nr. 29, Anwender 22.09.2026). Der Grenzwert <c>STROMST_CO2_GRENZWERT</c> =
        /// 270 g/kWh wird brennwertbezogen geprüft: Der Zähler nimmt den Ho-Faktor —
        /// den brennwertbezogenen Katalogwert, wo der Katalog einen führt (Erdgas:
        /// <c>EF_BILANZ_EBEV_ERDGAS_HO</c>, 181,4 g/kWh, statt
        /// <c>EF_BILANZ_EBEV_ERDGAS_HI</c>, 200,9 g/kWh), und sonst den heizwertbezogenen
        /// Katalogwert, umgerechnet Hi → Ho über die gepflegten Werte des Energieträgers
        /// (<c>H_i / H_s</c>, Projektwert vor Katalogwert) — dieselbe Umrechnung wie die
        /// Brennwertmenge der Energiesteuer und wie dort nicht über einen pauschalen
        /// Faktor. Der Brennstoff im Zähler bleibt die heizwertbezogene Menge des
        /// Rechenkerns; der Faktor allein wechselt die Bezugsgröße. Ein heizwertbezogener
        /// Zähler fiele rund 10 % zu hoch aus, und die Befreiung entfiele in Grenzfällen
        /// zu Unrecht.</para>
        ///
        /// <para><b>Ohne gepflegten Brennwert</b> bleibt der heizwertbezogene Faktor
        /// (<see cref="Co2Bezug.Heizwert"/>) — die konservative Richtung; die Befreiung
        /// kann dadurch höchstens zu Unrecht entfallen, nie zu Unrecht gewährt werden.
        /// Die Begründung nennt den Fall.</para>
        ///
        /// <para><c>null</c> = kein Faktor zugeordnet oder kein Energieertrag im Lauf.</para>
        /// </summary>
        internal static Co2Energieertrag Co2JeEnergieertrag(SteuerAnlage a,
                                                             Func<string, GesetzParameter> satz)
        {
            if (a == null || satz == null || string.IsNullOrEmpty(a.SchluesselCo2)) return null;
            double ertrag = a.StromMWh + a.WaermeMWh;
            if (ertrag <= 0 || a.BrennstoffMWh <= 0) return null;

            // (1) Der Schlüssel ist selbst brennwertbezogen, oder der Katalog führt zum
            //     heizwertbezogenen Schlüssel einen brennwertbezogenen: der Ho-Faktor.
            string schluesselHo = IstBrennwertbezogen(a.SchluesselCo2)
                                ? a.SchluesselCo2
                                : Co2SchluesselBrennwert(a.SchluesselCo2);
            if (schluesselHo != null)
            {
                GesetzParameter ho = satz(schluesselHo);
                if (ho != null && ho.Wert.HasValue)
                    return new Co2Energieertrag(Co2Bezug.KatalogBrennwert, schluesselHo,
                                                ho.Wert.Value, null, ho.Wert.Value,
                                                a.BrennstoffMWh, ertrag);
            }

            // (2) Der heizwertbezogene Katalogwert …
            GesetzParameter hi = satz(a.SchluesselCo2);
            if (hi == null || !hi.Wert.HasValue) return null;

            //     … umgerechnet Hi → Ho über die gepflegten Werte des Trägers,
            if (a.EffHi > 0 && a.EffHs > 0)
            {
                double quotient = a.EffHi / a.EffHs;
                return new Co2Energieertrag(Co2Bezug.Umgerechnet, a.SchluesselCo2,
                                            hi.Wert.Value, quotient, hi.Wert.Value * quotient,
                                            a.BrennstoffMWh, ertrag);
            }

            //     … oder, ohne gepflegten Brennwert, unverändert (konservativ).
            return new Co2Energieertrag(Co2Bezug.Heizwert, a.SchluesselCo2,
                                        hi.Wert.Value, null, hi.Wert.Value,
                                        a.BrennstoffMWh, ertrag);
        }

        /// <summary>
        /// Der Katalogschlüssel des <b>brennwertbezogenen</b> EBeV-Faktors zu einem
        /// heizwertbezogenen — heute allein Erdgas (EBeV 2030, Anlage 2 Teil 4:
        /// 181,4 g/kWh H_s). <c>null</c> = der Katalog führt keinen; dann wird über die
        /// Heizwerte des Trägers umgerechnet (Konzept § 6.3 Nr. 29).
        /// </summary>
        internal static string Co2SchluesselBrennwert(string schluesselHeizwert)
        {
            return string.Equals(schluesselHeizwert, DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI,
                                 StringComparison.Ordinal)
                 ? DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO
                 : null;
        }

        /// <summary>true, wenn der Schlüssel selbst schon brennwertbezogen ist.</summary>
        private static bool IstBrennwertbezogen(string schluessel)
        {
            return string.Equals(schluessel, DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO,
                                 StringComparison.Ordinal);
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
            bool berechtigt = ProduzierendesGewerbe(e);

            if (!berechtigt)
            {
                if (e.NetzbezugMWh > 0)
                    Grund(r, SteuerPosition.STROMST_ENTLASTUNG,
                          MyResource.Resource.STEUER_STROMST_9B_UNTERNEHMENSART);
                return;
            }

            GesetzParameter entlastung = satz(DbWerte.GESETZ_STROMST_ENTLASTUNG_9B);
            if (entlastung == null || !entlastung.Wert.HasValue)
            {
                Grund(r, SteuerPosition.STROMST_ENTLASTUNG, string.Format(kultur,
                      MyResource.Resource.STEUER_SATZ_FEHLT, DbWerte.GESETZ_STROMST_ENTLASTUNG_9B));
                return;
            }

            GesetzParameter sockelZeile = satz(DbWerte.GESETZ_STROMST_SOCKELBETRAG_9B);
            double sockel = sockelZeile != null && sockelZeile.Wert.HasValue ? sockelZeile.Wert.Value : 0;

            double roh = entlastung.Wert.Value * e.NetzbezugMWh;
            double netto = roh - sockel;
            if (netto <= 0)
            {
                if (e.NetzbezugMWh > 0)
                    Grund(r, SteuerPosition.STROMST_ENTLASTUNG, string.Format(kultur,
                          MyResource.Resource.STEUER_STROMST_9B_SOCKEL,
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
