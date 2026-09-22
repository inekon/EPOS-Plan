using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE B7P (Anwenderentscheid B7-E-1, 18.09.2026) — der <b>Nachweisumschlag</b>
    /// eines Wirtschaftlichkeitslaufs: EINE Quelle für Schreib- und Leseweg der Zeilen,
    /// die es bis B7P nur im frisch gerechneten Lauf gab.
    ///
    /// <para><b>Der Befund, der ihn erzwang.</b> Modulnachweis, Energiekosten je Anlage,
    /// Betriebskostenpositionen und Kohärenzzeilen entstanden im Lauf und starben mit
    /// ihm. Wer das Programm schloss und den gebuchten Stand wieder aufschlug, sah die
    /// Summen — aber keine einzige Herleitung darunter: Der Reiter ließ die Unterzeilen
    /// weg, Word und Excel ihre Modultabelle, und die Gruppe 5 des BHKW-Dialogs sagte
    /// „ohne Lauf". Die Zahlen waren gespeichert, ihre Begründung nicht.</para>
    ///
    /// <para><b>Warum ein Umschlag und keine Tabellen.</b> Es sind vier LISTEN mit je
    /// einem Dutzend Feldern, deren Länge an der Zahl der Anlagen und Kostenpositionen
    /// hängt. Als Tabellen wären das vier Schemata, vier Schreibwege, vier Lesewege und
    /// vier Migrationen — für Daten, die reiner AUSWEIS sind und aus denen nichts
    /// gerechnet wird. Der Umschlag steht in EINER Spalte
    /// (<c>WirtschaftlichkeitCtrl.SPALTE_NACHWEIS_JSON</c>), und diese Klasse ist die
    /// einzige Stelle, die sein Format kennt.</para>
    ///
    /// <para><b>Keine Kompression.</b> Anders als beim Auslegungsprofil
    /// (<c>SpeicherAuslegungCtrl</c>, Präfix <c>gz1:</c>) sind das ein paar Dutzend
    /// Zeilen, nicht Jahresreihen. Der Klartext bleibt lesbar — wer eine Datenbank in
    /// der Hand hat, kann nachsehen, was ein Lauf angesetzt hat.</para>
    ///
    /// <para><b>Was eingefroren wird.</b> Die Herleitungstexte
    /// (<see cref="KwkgModulNachweis.HerleitungEigen"/>,
    /// <see cref="KwkgModulNachweis.HerleitungEinspeisung"/>,
    /// <see cref="KohaerenzHinweis.Text"/>) sind fertig formatierte Sätze in der Sprache
    /// des Laufs. Ein gespeicherter Lauf zeigt sie deshalb in der Sprache, in der er
    /// gerechnet wurde — nicht in der gerade eingestellten. Das ist hingenommen: Die
    /// Alternative wäre, Schlüssel und Argumente einzeln zu führen, also die
    /// Formatierung ein zweites Mal zu schreiben.</para>
    /// </summary>
    public sealed class ErgebnisNachweisUmschlag
    {
        /// <summary>Formatkennung am Anfang des Textes. Ohne sie ist der Inhalt fremd
        /// und wird nicht gelesen (dieselbe Wache wie <c>gz1:</c>).</summary>
        public const string PRAEFIX = "nw1:";

        /// <summary>Fassung des Umschlags. Eine HÖHERE Fassung wird NICHT gelesen —
        /// ein halb verstandener Nachweis wäre schlimmer als keiner. Eine ÄLTERE
        /// dagegen schon: Ihre Felder sind eine echte Teilmenge, die fehlenden bleiben
        /// auf ihrer Vorgabe (siehe <see cref="Lesen"/>).</summary>
        public const int FASSUNG = 5;

        /// <summary>Die älteste Fassung, die noch gelesen wird. Darunter gab es keinen
        /// Umschlag.</summary>
        public const int FASSUNG_MINDESTENS = 1;

        /// <summary>
        /// Längenwächter [Byte]. Der Umschlag ist Ausweis, kein Rechenwert: Lieber
        /// verliert ein Ausreißerlauf seine Unterzeilen, als dass eine Ergebniszeile
        /// nicht gespeichert wird. Vier MiB fassen einige tausend Anlagen- und
        /// Kostenzeilen; alles darüber ist kein Bericht mehr.
        /// </summary>
        public const int GRENZE_BYTE = 4 * 1024 * 1024;

        /// <summary>Fassung DIESES Umschlags; <see cref="Lesen"/> nimmt sie von
        /// <see cref="FASSUNG_MINDESTENS"/> bis <see cref="FASSUNG"/> an.</summary>
        public int Version = FASSUNG;

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.KwkgModule"/>
        public List<KwkgModulNachweis> KwkgModule = new List<KwkgModulNachweis>();

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.EnergiekostenJeAnlage"/>
        public List<EnergieAnlageNachweis> EnergiekostenJeAnlage = new List<EnergieAnlageNachweis>();

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.Betriebskosten"/>
        public List<KostenPositionNachweis> Betriebskosten = new List<KostenPositionNachweis>();

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.KohaerenzHinweise"/>
        public List<KohaerenzHinweis> KohaerenzHinweise = new List<KohaerenzHinweis>();

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.VermiedenMengeMWh"/>
        public double VermiedenMengeMWh;

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.VermiedenEntlastung9bJahr"/>
        public double VermiedenEntlastung9bJahr;

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.ProduzierendesGewerbe"/>
        public bool ProduzierendesGewerbe;

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.BezugsspitzeKW"/>
        public double? BezugsspitzeKW;

        /// <summary>
        /// AUFTRAG U17 (Fassung 2) — die pauschale Vorauszahlung nach § 9 KWKG [€],
        /// einmalig im Jahr 0. 0 = sie greift nicht; ein Umschlag der Fassung 1 kennt
        /// das Feld nicht und liest sich deshalb mit 0 — genau die Aussage, die der
        /// gebuchte Stand von damals trägt.
        /// </summary>
        public double KwkgPauschaleEur;

        /// <summary>
        /// KONZEPT § 2.16 (Fassung 3) — die HERKUNFT der PV-Vergütung: <c>true</c> =
        /// dieser Stand rechnet mit der Vergütung seines Stammprojekts.
        ///
        /// <para>Ein Umschlag der Fassung 1 oder 2 kennt das Feld nicht und liest sich
        /// mit <c>false</c> — „eigene Werte". Genau das ist die Aussage, die ein vor
        /// Schemaschritt 93 gebuchter Stand trägt: Damals bekam eine Variante die
        /// Dialogangaben nur über eine eigene Zeile.</para>
        /// </summary>
        public bool PvVerguetungUebernommen;

        /// <summary>Name des Stammprojekts bei Übernahme; leer bei eigenen Werten
        /// (Fassung 3). <c>WhenWritingNull</c> lässt ihn dann aus dem JSON.</summary>
        public string PvVerguetungQuelle;

        // ---- AUFTRAG U7 (Fassung 4) — die Energiesteuer in zwei Beträgen ----
        //
        // Die Ergebnisspalte führt weiterhin die SUMME; getrennt sind die beiden
        // Paragrafenbeträge nur hier. Ein Umschlag der Fassung 1 bis 3 kennt die
        // Felder nicht, liest sich mit 0 — und sagt über
        // WirtschaftlichkeitErgebnis.EnergiesteuerAufgeteilt = false, dass es eben
        // KEINE Aufteilung gibt. Genau das trifft auf einen damals gebuchten Lauf zu.

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.Energiesteuer53Jahr1"/>
        public double Energiesteuer53Jahr1;

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.Energiesteuer54Jahr1"/>
        public double Energiesteuer54Jahr1;

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.Energiesteuer54SockelJahr1"/>
        public double Energiesteuer54SockelJahr1;

        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.EnergiesteuerNachweise"/>
        public List<EnergiesteuerNachweis> EnergiesteuerNachweise =
            new List<EnergiesteuerNachweis>();

        /// <summary>
        /// AUFTRAG 9d (Fassung 5) — die Begründung je Position der Erlösrubrik.
        /// Einem Umschlag der Fassung 1 bis 4 fehlt sie; die Rubrik nennt dann wie
        /// zuvor nur die Bedingung der Position.
        /// </summary>
        /// <inheritdoc cref="WirtschaftlichkeitErgebnis.PositionsGruende"/>
        public Dictionary<string, string> PositionsGruende =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// <c>IncludeFields</c> ist Pflicht: Alle vier Nachweistypen führen ausschließlich
        /// FELDER. Ohne die Option schriebe der Serialisierer leere Objekte — und läse
        /// sie auch wieder ein, ohne zu klagen.
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptionen = new JsonSerializerOptions
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>
        /// Baut den Umschlag eines Ergebnisses und gibt ihn als Text mit Präfix zurück.
        /// <c>null</c> = nichts zu speichern oder zu groß; dann steht in
        /// <paramref name="grund"/> der fertige Hinweistext für den Lauf, und die Spalte
        /// bekommt <c>DBNull</c>.
        ///
        /// <para><b>Wirft nie.</b> Der Schreibweg läuft in der Transaktion, die auch die
        /// Ergebniszeilen schreibt; ein Wurf hier verlöre den ganzen Lauf.</para>
        /// </summary>
        public static string Schreiben(WirtschaftlichkeitErgebnis e, out string grund)
        {
            grund = null;
            if (e == null) return null;
            try
            {
                var u = new ErgebnisNachweisUmschlag
                {
                    KwkgModule = e.KwkgModule ?? new List<KwkgModulNachweis>(),
                    EnergiekostenJeAnlage = e.EnergiekostenJeAnlage ?? new List<EnergieAnlageNachweis>(),
                    Betriebskosten = e.Betriebskosten ?? new List<KostenPositionNachweis>(),
                    KohaerenzHinweise = e.KohaerenzHinweise ?? new List<KohaerenzHinweis>(),
                    VermiedenMengeMWh = e.VermiedenMengeMWh,
                    VermiedenEntlastung9bJahr = e.VermiedenEntlastung9bJahr,
                    ProduzierendesGewerbe = e.ProduzierendesGewerbe,
                    BezugsspitzeKW = e.BezugsspitzeKW,
                    KwkgPauschaleEur = e.KwkgPauschaleEur,
                    PvVerguetungUebernommen = e.PvVerguetungUebernommen,
                    PvVerguetungQuelle = string.IsNullOrEmpty(e.PvVerguetungQuelle)
                                       ? null : e.PvVerguetungQuelle,
                    Energiesteuer53Jahr1 = e.Energiesteuer53Jahr1,
                    Energiesteuer54Jahr1 = e.Energiesteuer54Jahr1,
                    Energiesteuer54SockelJahr1 = e.Energiesteuer54SockelJahr1,
                    EnergiesteuerNachweise = e.EnergiesteuerNachweise
                                           ?? new List<EnergiesteuerNachweis>(),
                    PositionsGruende = e.PositionsGruende
                                     ?? new Dictionary<string, string>(StringComparer.Ordinal)
                };

                byte[] roh = JsonSerializer.SerializeToUtf8Bytes(u, JsonOptionen);
                if (roh.Length > GRENZE_BYTE)
                {
                    grund = MyResource.Resource.WIRT_NACHWEIS_ZU_GROSS;
                    return null;
                }
                return PRAEFIX + Encoding.UTF8.GetString(roh);
            }
            catch
            {
                // Ein Serialisierungsfehler kostet die Nachweise, nie den Lauf — und er
                // sagt es mit demselben Satz wie der Längenwächter: Für den Anwender ist
                // beides dasselbe, nämlich „diesmal nur im frischen Lauf".
                grund = MyResource.Resource.WIRT_NACHWEIS_ZU_GROSS;
                return null;
            }
        }

        /// <summary>
        /// Liest einen Umschlag. <c>null</c> = kein Text, fehlendes Präfix, kaputtes JSON
        /// oder fremde Fassung. <b>Wirft nie</b> — der Leseweg der Ergebnisse hat EINEN
        /// Fang um die ganze Projektschleife; eine Ausnahme von hier verschluckte alle
        /// Ergebniszeilen aller Projekte.
        /// </summary>
        public static ErgebnisNachweisUmschlag Lesen(string daten)
        {
            try
            {
                if (string.IsNullOrEmpty(daten)) return null;
                if (!daten.StartsWith(PRAEFIX, StringComparison.Ordinal)) return null;

                ErgebnisNachweisUmschlag u = JsonSerializer.Deserialize<ErgebnisNachweisUmschlag>(
                    daten.Substring(PRAEFIX.Length), JsonOptionen);
                // AUFTRAG U17: Eine ÄLTERE Fassung wird weiter gelesen. Der Umschlag
                // reist mit dem GEBUCHTEN Stand; würde die Fassungserhöhung ihn
                // verwerfen, verlöre jeder vor U17 gerechnete Lauf seine Unterzeilen,
                // seine Modultabelle und seine Kohärenzhinweise — für ein Feld, das er
                // gar nicht führen kann. Die Felder der Fassung 1 sind eine echte
                // Teilmenge derer der Fassung 2; was fehlt, bleibt auf seiner Vorgabe.
                // Eine HÖHERE Fassung bleibt verworfen: Was sie bedeutet, weiß dieser
                // Stand nicht. Fassung 3 (Konzept § 2.16) trägt die Herkunft der
                // PV-Vergütung nach; sie fehlt einem älteren Umschlag und liest sich
                // dort als „eigene Werte" — die Aussage, die ein damals gebuchter Lauf
                // tatsächlich trägt.
                if (u == null || u.Version < FASSUNG_MINDESTENS || u.Version > FASSUNG)
                    return null;

                // Ein Umschlag, dem eine Liste fehlt, ist lesbar — leer ist die richtige
                // Antwort, null wäre eine Falle für jeden Leser.
                if (u.KwkgModule == null) u.KwkgModule = new List<KwkgModulNachweis>();
                if (u.EnergiekostenJeAnlage == null) u.EnergiekostenJeAnlage = new List<EnergieAnlageNachweis>();
                if (u.Betriebskosten == null) u.Betriebskosten = new List<KostenPositionNachweis>();
                if (u.KohaerenzHinweise == null) u.KohaerenzHinweise = new List<KohaerenzHinweis>();
                if (u.EnergiesteuerNachweise == null)
                    u.EnergiesteuerNachweise = new List<EnergiesteuerNachweis>();
                if (u.PositionsGruende == null)
                    u.PositionsGruende = new Dictionary<string, string>(StringComparer.Ordinal);
                return u;
            }
            catch { return null; }
        }

        /// <summary>Schreibt den Inhalt des Umschlags in ein geladenes Ergebnis zurück —
        /// die Gegenrichtung zu <see cref="Schreiben"/>, in derselben Klasse, damit ein
        /// neues Feld nicht an einer der beiden Seiten vergessen wird.</summary>
        public void Uebernimm(WirtschaftlichkeitErgebnis e)
        {
            if (e == null) return;
            e.KwkgModule = KwkgModule;
            e.EnergiekostenJeAnlage = EnergiekostenJeAnlage;
            e.Betriebskosten = Betriebskosten;
            e.KohaerenzHinweise = KohaerenzHinweise;
            e.VermiedenMengeMWh = VermiedenMengeMWh;
            e.VermiedenEntlastung9bJahr = VermiedenEntlastung9bJahr;
            e.ProduzierendesGewerbe = ProduzierendesGewerbe;
            e.BezugsspitzeKW = BezugsspitzeKW;
            e.KwkgPauschaleEur = KwkgPauschaleEur;
            e.PvVerguetungUebernommen = PvVerguetungUebernommen;
            e.PvVerguetungQuelle = PvVerguetungQuelle ?? "";

            // AUFTRAG U7 — die Aufteilung gibt es erst ab Fassung 4. Ein älterer
            // Umschlag trägt 0/0, was zusammen mit EnergiesteuerJahr1 > 0 eine
            // Behauptung wäre; der Merker sagt deshalb, dass es sie NICHT gibt, und
            // die Rubrik bleibt für diesen Stand bei der einen Zeile.
            e.EnergiesteuerAufgeteilt = Version >= 4;
            e.Energiesteuer53Jahr1 = Energiesteuer53Jahr1;
            e.Energiesteuer54Jahr1 = Energiesteuer54Jahr1;
            e.Energiesteuer54SockelJahr1 = Energiesteuer54SockelJahr1;
            e.EnergiesteuerNachweise = EnergiesteuerNachweise
                                     ?? new List<EnergiesteuerNachweis>();
            e.PositionsGruende = PositionsGruende
                               ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
