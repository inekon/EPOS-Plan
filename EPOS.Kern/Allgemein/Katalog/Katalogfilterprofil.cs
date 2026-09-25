using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Welcher der drei ZEITREIHENKATALOGE</b> (Stufe S3.2 des
    /// <c>Konzept_Katalogfilter</c>, 2.10 und 4.10).
    ///
    /// <para>Sie sind keine <see cref="Anlagenart"/>: Hinter ihnen steht kein Geraet,
    /// sondern eine Kopftabelle mit einer Wertetabelle daneben. Der Aufzaehlungstyp
    /// steht deshalb hier und nicht in <see cref="ParameterVerwendung"/>, dessen acht
    /// Arten gegen <c>pragma table_info</c> der Stammtabellen gehalten werden.</para>
    /// </summary>
    public enum Zeitreihenart
    {
        /// <summary>Waermebedarf-Lastgang — <c>Tab_Waermebedarf_STAMM</c> (4 Saetze).</summary>
        Waermebedarf,

        /// <summary>Stromganglinie — <c>Tab_Stromganglinie_STAMM</c> (3 Saetze), MIT Zeitintervall.</summary>
        Stromganglinie,

        /// <summary>Solarthermieganglinie — <c>Tab_Solarganglinie_STAMM</c> (1 Satz), MIT Beschreibung.</summary>
        Solarganglinie
    }

    /// <summary>
    /// Was fuer eine Spalte einer Katalogliste (Konzept_Katalogfilter 5.6.3).
    /// </summary>
    public enum Katalogspaltenart
    {
        /// <summary>
        /// Text — Bezeichner, Hersteller, Brennstoff, Technologie, Quelle, Bauart.
        /// Der Filter ist die Teilzeichenkette „enthaelt…", gross/klein egal.
        /// </summary>
        Text,

        /// <summary>
        /// Zahl — P_th, η, P_STC, Volumen, Energie. Der Filter versteht
        /// <c>&gt;10</c>, <c>10..60</c>, <c>=15</c> (<see cref="Zahlenausdruck"/>);
        /// die Spalte steht rechtsbuendig.
        /// </summary>
        Zahl,

        /// <summary>
        /// Kennzeichen — Brennwert, Kuehlen. Die Spalte zeigt „Ja"/„Nein" und traegt
        /// NUR den Sortierpfeil: Ein Feld „enthaelt ja" fuer zwei Werte ist ein
        /// Bedienelement ohne Gewinn (5.6.2).
        /// </summary>
        JaNein
    }

    /// <summary>
    /// <b>Wann eine Spalte einer Katalogliste zu sehen ist</b> (Konzept
    /// Administrationsdialoge, Vorschlag V2 „keine waagerechte Rollleiste: Spalten mit
    /// Rang"). Der Rang sagt die REIHENFOLGE, in der Spalten weichen, wenn die Liste
    /// schmal wird; WANN eine Spalte weicht, rechnet die Liste aus den Breiten ihres
    /// Inhalts (<c>EPOS.UI/Bausteine/Spaltenraenge.cs</c>).
    /// </summary>
    public enum Katalogspaltenrang
    {
        /// <summary>
        /// Immer sichtbar — der Bezeichner und der EINE Hauptkennwert. Das ist auch die
        /// Vorgabe: Ein Profil, das keinen Rang nennt, zeigt alle Spalten wie bisher.
        /// </summary>
        Immer = 1,

        /// <summary>Sichtbar, sobald die Liste Platz hat — Hersteller, Brennstoff, Typ.</summary>
        BeiPlatz = 2,

        /// <summary>
        /// Nur in einer breiten Liste — was man zum Waehlen selten braucht. Weicht als
        /// erste; im Stammblatt (Stufe 3) steht es ohnehin.
        /// </summary>
        Breit = 3
    }

    /// <summary>
    /// <b>Eine Spalte einer Katalogliste</b> — Titel, Einheit, Art und die zwei
    /// Fragen „sortierbar?" und „filterbar?" (Konzept_Katalogfilter Kapitel 4 und
    /// 5.6.5).
    /// </summary>
    /// <remarks>
    /// <para><b>Der Schluessel ist sprachneutral</b> und zugleich der Schluessel, unter
    /// dem <c>…StammCtrl.Katalogfilterzeilen</c> den Wert liefert — dasselbe Muster
    /// wie bei <see cref="BrowserDetailfeld"/>.</para>
    /// <para><b>Filterbar heisst: es gibt einen Trichter.</b> Wo
    /// <see cref="Filterbar"/> <c>false</c> ist, zeichnet der Spaltenkopf keinen —
    /// „kein Delegat, kein Bedienelement" (Hausregel aus
    /// <c>ModulKatalogDialog.Filterbar</c>).</para>
    /// </remarks>
    public sealed class Katalogspalte
    {
        public Katalogspalte(string schluessel, string titel, string einheit = "",
                             Katalogspaltenart art = Katalogspaltenart.Text,
                             bool sortierbar = true, bool filterbar = true,
                             Katalogspaltenrang rang = Katalogspaltenrang.Immer)
        {
            Schluessel = schluessel;
            Titel = titel;
            Einheit = einheit ?? "";
            Art = art;
            Sortierbar = sortierbar;
            Filterbar = filterbar && art != Katalogspaltenart.JaNein;
            Rang = rang;
        }

        /// <summary>
        /// <b>Der Rang der Spalte</b> (Vorschlag V2): in welcher Reihenfolge sie weicht,
        /// wenn die Liste schmal wird. Eine Spalte mit gesetztem Filter oder Sortierung
        /// weicht nie (V7) — das entscheidet die Liste, nicht das Profil.
        /// </summary>
        public Katalogspaltenrang Rang { get; }

        /// <summary>Sprachneutraler ASCII-Schluessel — zugleich der Zugriff auf den Wert.</summary>
        public string Schluessel { get; }

        /// <summary>Spaltenkopf, bereits uebersetzt.</summary>
        public string Titel { get; }

        /// <summary>Einheit; leer, wo es keine gibt.</summary>
        public string Einheit { get; }

        /// <summary>Text, Zahl oder Kennzeichen.</summary>
        public Katalogspaltenart Art { get; }

        /// <summary>Laesst sich die Liste nach dieser Spalte sortieren?</summary>
        public bool Sortierbar { get; }

        /// <summary>Traegt der Spaltenkopf einen Trichter?</summary>
        public bool Filterbar { get; }

        /// <summary>Der vollstaendige Kopftext: „P_th [kW]" bzw. „Hersteller".</summary>
        public string Kopftext =>
            Einheit.Length > 0 ? Titel + " [" + Einheit + "]" : Titel;

        /// <summary>Steht die Spalte rechtsbuendig? Zahlen ja, alles andere nein.</summary>
        public bool Rechtsbuendig => Art == Katalogspaltenart.Zahl;
    }

    /// <summary>
    /// Ein Zellwert: der ANGEZEIGTE Text und — bei einer Zahlenspalte — die Zahl
    /// dahinter.
    /// </summary>
    /// <remarks>
    /// <para><b>Der Filter arbeitet auf dem ANGEZEIGTEN Wert</b> (Konzept 5.6.3).
    /// <c>Tab_Heizkessel_STAMM.Brennstoff</c> ist eine Zahl, in der Spalte steht der
    /// Name aus <c>Tab_Brennstoff_Stamm</c>; „enthaelt Gas" trifft deshalb
    /// Stadtgas, Erdgas und Fluessiggas. Deshalb traegt der Wert den TEXT.</para>
    /// <para>Die ZAHL steht daneben, damit eine Zahlenspalte als Zahl sortiert und
    /// verglichen wird und nicht als Zeichenkette („9" &gt; „10").</para>
    /// </remarks>
    public readonly struct Katalogwert
    {
        public Katalogwert(string text, double? zahl = null)
        {
            Text = text ?? "";
            Zahl = zahl;
        }

        /// <summary>Der Anzeigetext der Zelle.</summary>
        public string Text { get; }

        /// <summary>Die Zahl dahinter; <c>null</c> bei Textspalten und bei Leerwerten.</summary>
        public double? Zahl { get; }

        /// <summary>Ein nicht gepflegter Wert — der Halbgeviertstrich aus W6-E-1.</summary>
        public static readonly Katalogwert Leer = new Katalogwert(ParameterVerwendung.LEER);

        /// <summary>
        /// Ein Textwert; leerer Text wird zum Halbgeviertstrich (W6-E-1: NULL ist etwas
        /// anderes als eine gemessene Null).
        /// </summary>
        public static Katalogwert AusText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? Leer : new Katalogwert(text);
        }

        /// <summary>
        /// Ein Zahlenwert mit fester Nachkommastellenzahl in der Kultur des Anwenders;
        /// <c>null</c> wird zum Halbgeviertstrich.
        /// </summary>
        public static Katalogwert AusZahl(double? zahl, int nachkomma = 1)
        {
            if (zahl == null || double.IsNaN(zahl.Value) || double.IsInfinity(zahl.Value))
                return Leer;

            return new Katalogwert(
                zahl.Value.ToString("N" + nachkomma.ToString(CultureInfo.InvariantCulture),
                                    CultureInfo.CurrentCulture),
                zahl.Value);
        }

        /// <summary>
        /// Ein Kennzeichen: „Ja"/„Nein" aus demselben Ressourcenpaar, das jede
        /// Rueckfrage des Hauses benutzt (wie <c>ParameterUebersichtCtrl.Wahrheit</c>).
        /// Damit sortiert die Spalte die Ja-Saetze zusammen (5.6.2).
        /// </summary>
        public static Katalogwert AusKennzeichen(bool ja)
        {
            return new Katalogwert(ja ? MyResource.Resource.ALLG_BTN_JA
                                      : MyResource.Resource.ALLG_BTN_NEIN,
                                   ja ? 1 : 0);
        }
    }

    /// <summary>
    /// <b>Eine Zeile einer Katalogliste</b> — Primaerschluessel, Bezeichner und je
    /// Spalte ein <see cref="Katalogwert"/>.
    /// </summary>
    /// <remarks>
    /// <para>Sie ersetzt in den acht Verwaltungsdialogen die <c>ID, Bezeichner</c>
    /// der Vorlaeufer: Ohne Parameterspalten nuetzt ein Parameterfilter wenig
    /// (Konzept Befund 1.2/2), und ohne Werte kann die Liste weder sortieren noch
    /// filtern.</para>
    /// <para><b>Abgeleitete Groessen sind gleichberechtigte Spalten</b> (3.2):
    /// Stromkennzahl σ, C-Rate, Modulflaeche und COP bei A2/W35 werden EINMAL beim
    /// Aufbau der Liste gerechnet und sind danach ganz normale Werte.</para>
    /// </remarks>
    public sealed class Katalogfilterzeile
    {
        private readonly Dictionary<string, Katalogwert> _werte;

        public Katalogfilterzeile(int id, string bezeichner,
                                  IReadOnlyDictionary<string, Katalogwert> werte = null)
        {
            Id = id;
            Bezeichner = bezeichner ?? "";
            _werte = new Dictionary<string, Katalogwert>(StringComparer.Ordinal);
            if (werte != null)
                foreach (KeyValuePair<string, Katalogwert> p in werte) _werte[p.Key] = p.Value;
        }

        /// <summary>Primaerschluessel im Katalog.</summary>
        public int Id { get; }

        /// <summary>Der Bezeichner — der Schluessel jeder Aktion des Dialogs.</summary>
        public string Bezeichner { get; }

        /// <summary>
        /// <b>Der Schluessel dieser Zeile</b> — woran die Katalogliste ihre Wahl und
        /// ihre Markierung festmacht. Vorgabe ist der <see cref="Bezeichner"/>; genau
        /// so haben es die fuenfzehn Wirte aus S1 und S2.
        ///
        /// <para><b>Warum er ueberhaupt getrennt ist</b> (Stufe S3.4): Ein
        /// IMPORTKANDIDAT hat keinen Datenbank-Bezeichner — die CEC-Liste bringt
        /// 20 743 Module, darunter Namensdubletten, und der Satz ist erst nach dem
        /// Uebernehmen ein Katalogsatz. Die Importmasken vergeben deshalb eine
        /// laufende Kennung je Kandidat und lassen den Bezeichner das, was er ist:
        /// der ANZEIGEname.</para>
        /// </summary>
        public string Schluessel
        {
            get { return _schluessel.Length > 0 ? _schluessel : Bezeichner; }
            set { _schluessel = value ?? ""; }
        }

        private string _schluessel = "";

        /// <summary>
        /// Traegt der Satz den Schreibschutz der Auslieferung? Nur der BHKW-Browser
        /// zeichnet ihn gedimmt (<c>Form_BHKWAdmin.cs:202-203</c>), die
        /// Waermepumpen-Stammliste ebenso.
        /// </summary>
        public bool Geschuetzt { get; set; }

        /// <summary>Setzt einen Wert und liefert die Zeile zurueck (Baukasten).</summary>
        public Katalogfilterzeile Mit(string schluessel, Katalogwert wert)
        {
            _werte[schluessel] = wert;
            return this;
        }

        /// <summary>Setzt einen Textwert.</summary>
        public Katalogfilterzeile MitText(string schluessel, string text)
        {
            return Mit(schluessel, Katalogwert.AusText(text));
        }

        /// <summary>Setzt einen Zahlenwert.</summary>
        public Katalogfilterzeile MitZahl(string schluessel, double? zahl, int nachkomma = 1)
        {
            return Mit(schluessel, Katalogwert.AusZahl(zahl, nachkomma));
        }

        /// <summary>Setzt ein Kennzeichen („Ja"/„Nein").</summary>
        public Katalogfilterzeile MitKennzeichen(string schluessel, bool ja)
        {
            return Mit(schluessel, Katalogwert.AusKennzeichen(ja));
        }

        /// <summary>Der Wert einer Spalte; eine unbekannte Spalte ist leer.</summary>
        public Katalogwert Wert(string schluessel)
        {
            Katalogwert w;
            return _werte.TryGetValue(schluessel ?? "", out w) ? w : Katalogwert.Leer;
        }

        /// <summary>Der Anzeigetext einer Spalte.</summary>
        public string Text(string schluessel) => Wert(schluessel).Text;

        /// <summary>Die Zahl einer Spalte; <c>null</c>, wo keine steht.</summary>
        public double? Zahl(string schluessel) => Wert(schluessel).Zahl;
    }

    /// <summary>
    /// <b>Die Auspraegung eines Katalogfilters</b> (Anwenderentscheid
    /// <b>W14a-E-10</b> vom 07.09.2026, Konzept_Katalogfilter Kapitel 3 und 4) —
    /// welche Spalten eine Katalogliste zeigt, welche davon sortierbar sind und
    /// welche einen Trichter tragen.
    ///
    /// <para><b>Warum EINER und nicht vierzehn.</b> Es gab drei Filtermechaniken im
    /// Haus (Befund 1.4), und mit dem Wunsch W14a-E-9 kaemen zehn weitere Filter
    /// dazu. Vierzehn handgeschriebene Filterzeilen sind vierzehn Wahrheiten
    /// darueber, was „Alle" heisst und ob <c>*</c> ein Platzhalter ist. Das Haus hat
    /// fuer diese Lage ein Muster, dreimal erprobt — <see cref="KatalogImportProfil"/>,
    /// <see cref="KatalogBrowserProfil"/>, <see cref="ModulKatalogProfil"/>: Der
    /// Bauplan steht einmal als Komponente, die Unterschiede stehen als DATEN im
    /// Kern. Dies ist der vierte Zwilling.</para>
    ///
    /// <para><b>Die Beschriftungen kommen von aussen</b> — <see cref="Finde"/> nimmt
    /// einen Uebersetzer entgegen (Schluessel → Text); der Kern kennt keine
    /// Anzeigetexte.</para>
    ///
    /// <para><b>Sechs bis neun Spalten</b> (5.6.5, im Mockup gemessen): Ueber die
    /// ganze Breite haben bei 1 366 px sechs bis neun Parameterspalten Platz, ohne
    /// dass die Liste waagerecht rollt. Was gemessen fast nie gepflegt ist, faellt
    /// heraus — Vorlauf/Ruecklauf des Kessels (0 von 63), die Bauart der Waermepumpe
    /// (45 von 51 leer).</para>
    /// </summary>
    public sealed class Katalogfilterprofil
    {
        // ==================================================================
        // Die Spaltenschluessel — sprachneutral, wie BrowserDetailfeld
        // ==================================================================

        public const string SpBezeichner = "BEZEICHNER";
        public const string SpHersteller = "HERSTELLER";
        public const string SpBrennstoff = "BRENNSTOFF";
        public const string SpPtherm = "PTHERM";
        public const string SpPel = "PEL";
        public const string SpEta = "ETA";
        public const string SpSigma = "SIGMA";
        public const string SpMotortyp = "MOTORTYP";
        public const string SpBrennwert = "BRENNWERT";
        public const string SpKollektortyp = "KOLLEKTORTYP";
        public const string SpApertur = "APERTUR";
        public const string SpEtaNull = "ETANULL";
        public const string SpK1 = "K1";
        public const string SpSpeichertyp = "SPEICHERTYP";
        public const string SpVolumen = "VOLUMEN";
        public const string SpVerluste = "VERLUSTE";
        public const string SpPstc = "PSTC";
        public const string SpTechnologie = "TECHNOLOGIE";
        public const string SpModulflaeche = "MODULFLAECHE";
        public const string SpTnoct = "TNOCT";
        public const string SpPac = "PAC";
        public const string SpEtaEuro = "ETAEURO";
        public const string SpMppt = "MPPT";
        public const string SpUdcMax = "UDCMAX";
        public const string SpHerkunft = "HERKUNFT";
        public const string SpChemie = "CHEMIE";
        public const string SpEnergie = "ENERGIE";
        public const string SpLeistung = "LEISTUNG";
        public const string SpCrate = "CRATE";
        public const string SpEtaRt = "ETART";
        public const string SpZyklen = "ZYKLEN";
        public const string SpQuelle = "QUELLE";
        public const string SpNennleistung = "NENNLEISTUNG";
        public const string SpVlMin = "VLMIN";
        public const string SpVlMax = "VLMAX";
        public const string SpZuheizung = "ZUHEIZUNG";

        /// <summary>
        /// <b>Kuehlleistung [kW]</b> (<c>Tab_WP_STAMM.Kuehlleistung</c>) — die ZAHL, nicht
        /// mehr das Kennzeichen „Kuehlen" (Anwenderentscheid 16.09.2026).
        ///
        /// <para>Bis dahin stand hier <c>SpKuehlen</c>, ein Ja/Nein aus
        /// <c>Kuehlleistung &gt; 0</c>. Die Zahl sagt dasselbe und mehr: Wer kuehlen will,
        /// will wissen, WIE VIEL. „Zwei Spalten fuer eine Aussage waeren eine zu viel"
        /// (Konzept_Katalogfilter 4.3) — deshalb ERSETZT die Zahlenspalte das Kennzeichen,
        /// statt neben es zu treten; die Waermepumpe behaelt ihre neun Spalten. Das
        /// schnelle „nur mit Kuehlfunktion" traegt der Schalter des
        /// <c>WaermepumpenKatalogDialog</c>, der genau diese Spalte auf <c>&gt;0</c> setzt.</para>
        /// </summary>
        public const string SpKuehlleistung = "KUEHLLEISTUNG";

        /// <summary>
        /// Der Ausdruck, den der Schalter „nur mit Kuehlfunktion" in
        /// <see cref="SpKuehlleistung"/> legt — dieselbe Zeichenkette, die ein Anwender
        /// von Hand in den Trichter schriebe (<see cref="Zahlenausdruck"/>).
        /// </summary>
        public const string AUSDRUCK_MIT_KUEHLUNG = ">0";

        public const string SpCop = "COP";

        /// <summary>
        /// <b>„im Projekt verwendet"</b> (Frage <b>Q12</b>, Stufe S2.3) — die einzige
        /// Spalte, die es NUR in den Projektdialogen gibt. In der Verwaltung waere
        /// sie eine Zaehlung ueber alle Projekte ohne Nutzen fuer die Pflege.
        /// <para>Sie ist ein KENNZEICHEN und traegt deshalb nur den Sortierpfeil
        /// (5.6.2): Ein Feld „enthaelt ja" fuer zwei Werte ist ein Bedienelement ohne
        /// Gewinn — die Sortierung stellt die verwendeten Saetze ohnehin
        /// zusammen.</para>
        /// </summary>
        public const string SpVerwendet = "VERWENDET";

        // ------------------------------------------------------------------
        // Stufe S3 (07.09.2026) - die SECHS weiteren Kataloge
        // ------------------------------------------------------------------

        /// <summary>Die Profilzuordnung der drei Bedarfskataloge (4.9) — <c>Typ</c>.</summary>
        public const string SpTyp = "TYP";

        /// <summary>
        /// Die JAHRESSUMME eines Bedarfssatzes (4.9) — Σ <c>Monat_1…12</c>.
        /// <para><b>Die Monatswerte stehen in MWh</b> (das Einheitenkuerzel neben dem
        /// Feld heisst „MWh" bzw. „MWth", <c>BedarfAdminHuelle.Einheit</c>), und die
        /// Spalte zeigt sie unveraendert: <b>kein Faktor 1000</b> — Einheitenregel
        /// W8-O-5c. Der Schluessel nennt die Einheit im Namen.</para>
        /// </summary>
        public const string SpJahressummeMwh = "JAHRESSUMME";

        /// <summary>
        /// Die Beschreibung (4.9) — einzeilig gekuerzt. Sie ist bei den VDI-6002-Saetzen
        /// der eigentliche SUCHRAUM: „Monatswerte in MWh fuer 1 Person bei 28 l/d
        /// @60 °C (ΔT 50 K)" traegt den Kennwert im Text.
        /// </summary>
        public const string SpBeschreibung = "BESCHREIBUNG";

        /// <summary>
        /// <b>„nur eigene Saetze"</b> (4.9) — der Schreibschutz der Auslieferung
        /// (<c>ReadOnly</c>) als Wert der Bedarfszeile. Eine SPALTE ist er nicht mehr:
        /// Die Liste zeigt ihn als Schloss hinter dem Bezeichner (Konzept
        /// Administrationsdialoge, V10).
        /// </summary>
        public const string SpAuslieferung = "AUSLIEFERUNG";

        /// <summary>Das Zeitraster einer Zeitreihe (4.10) — nur die Stromganglinie fuehrt es.</summary>
        public const string SpZeitintervall = "ZEITINTERVALL";

        /// <summary>
        /// Die JAHRESARBEIT einer Zeitreihe (4.10) in MWh — Σ der Stundenleistungen
        /// ÷ 1 000, gerechnet in der EINEN Gruppenabfrage je Katalog
        /// (<see cref="GanglinienAuswertungCtrl"/>).
        /// </summary>
        public const string SpJahresarbeitMwh = "JAHRESARBEIT";

        /// <summary>
        /// Die SPITZE einer Zeitreihe (4.10) in kW — der Hoechstwert der STUNDENreihe,
        /// also genau die Zahl, die die Grafik der Ganglinien-Dialoge als 100-%-Linie
        /// zeigt (W9-E-3 / W12-E-2).
        /// </summary>
        public const string SpSpitzeKw = "SPITZE";

        // ------------------------------------------------------------------
        // Klimaregionen (Auftrag KL-4) - der neunte Katalog
        //
        //   Die Klimadatenmaske zeigt ihre Regionsliste seit KL-4 als
        //   Katalogliste (Anwenderwunsch 19.09.2026: "das gleiche Schema wie
        //   bei den Modulen"). Die Spalten stehen hier, weil sie DATEN sind -
        //   wie bei den vierzehn anderen Katalogen; das Profil baut
        //   KlimadatenDialog ueber AusSpalten("KLIMAREGION", ...).
        //
        //   Die QUELLE benutzt den bestehenden SpQuelle: Sie ist dieselbe
        //   Frage wie beim Stromspeicher ("woher stammt der Satz"), und ein
        //   zweiter Schluessel gleichen Inhalts waere die Wahrheit doppelt.
        // ------------------------------------------------------------------

        /// <summary>
        /// Der <b>Standort</b> einer Klimaregion (4.11) — der Ortsname aus
        /// <c>Tab_Klimaregion_STAMM.Details</c>, sonst das Koordinatenpaar. Er ist die
        /// Spalte, nach der gesucht wird: Der Bezeichner ist frei gewaehlt, der
        /// Standort sagt, WO die Reihe gemessen ist
        /// (<c>KlimaregionStammCtrl.Standorttext</c>).
        /// </summary>
        public const string SpStandort = "STANDORT";

        /// <summary>Laengengrad einer Klimaregion in Grad Ost (4.11), vier Nachkommastellen.</summary>
        public const string SpLongitude = "LONGITUDE";

        /// <summary>Breitengrad einer Klimaregion in Grad Nord (4.11), vier Nachkommastellen.</summary>
        public const string SpLatitude = "LATITUDE";

        /// <summary>
        /// Der Tag des Imports (4.11) als ISO-Text <c>yyyy-MM-dd</c> aus
        /// <c>Tab_Klimaregion_STAMM.Importdatum</c> (Schemaschritt 95).
        ///
        /// <para><b>Eine TEXTspalte, und zwar mit Absicht</b>: ISO sortiert als
        /// Zeichenkette in derselben Reihenfolge wie als Datum, und das Spaltenmodell
        /// kennt keine Datumsart (<see cref="Katalogspaltenart"/>). Der Trichter
        /// „enthaelt 2026-09" wird damit zum Monatsfilter.</para>
        /// </summary>
        public const string SpImportdatum = "IMPORTDATUM";

        /// <summary>
        /// Der <b>Schreibschutz</b> einer Klimaregion (4.11) — <c>ReadOnly</c>: ein
        /// Satz der Auslieferung laesst sich nicht loeschen. Der Wert steht in der Zeile;
        /// die Liste zeigt ihn als Schloss hinter dem Bezeichner, nicht als Spalte
        /// (Konzept Administrationsdialoge, V10).
        /// </summary>
        public const string SpSchreibschutz = "SCHREIBSCHUTZ";

        // ------------------------------------------------------------------
        // Brauchwasser-Nutzungsarten (Umsetzungskonzept Zapfprofilgenerator
        // 3.2 und 5.4, Stufe Z0, Posten P8). Die Herkunft der Bedarfswerte
        // benutzt den bestehenden SpHerkunft - dieselbe Frage wie beim
        // Wechselrichter ("woher stammt der Satz").
        // ------------------------------------------------------------------

        /// <summary>Die Bezugsart einer Nutzungsart (Personen, Wohneinheiten, Betten …).</summary>
        public const string SpBezugsart = "BEZUGSART";

        /// <summary>Die Kalenderart einer Nutzungsart (Wohnen, Arbeitstage, Schulferien …).</summary>
        public const string SpKalender = "KALENDER";

        /// <summary>
        /// Die <b>Katalogversion</b> — zusammen mit dem Bezeichner der natürliche Schlüssel
        /// einer Nutzungsart (Konzept 3.1): Zwei Versionen tragen denselben Namen.
        /// </summary>
        public const string SpKatalogversion = "KATALOGVERSION";

        /// <summary>Der Status einer Katalogzeile: Auslieferung, eigen oder Import.</summary>
        public const string SpStatus = "STATUS";

        // ------------------------------------------------------------------
        // Stufe 5 der Neuordnung der Administrationsdialoge (V16): die drei
        // SONDERLISTEN - Gebaeude, Gebaeudetypen, Lastgaenge der
        // Lastspitzenkappung - bekommen ein Spaltenprofil wie die uebrigen
        // Kataloge. Die Spalten stehen hier, weil sie DATEN sind.
        // ------------------------------------------------------------------

        /// <summary>
        /// Die <b>Gebaeudeart</b> eines Katalogsatzes (<c>Tab_Gebaeude_STAMM.Gebaeudeart</c>)
        /// — bis Stufe 5 der Vorfilter „Gebaeudeart" ueber der eigenen Tabelle, jetzt ein
        /// Trichter.
        /// </summary>
        public const string SpGebaeudeart = "GEBAEUDEART";

        /// <summary>
        /// Die <b>Verwendung</b> eines Gebaeudes — Wohngebaeude oder Gewerbe und Sonstige
        /// (<c>Wohngebaeude_Nicht_Wohngebaeude</c>), als ANZEIGETEXT: Der Trichter filtert
        /// auf dem, was in der Zelle steht.
        /// </summary>
        public const string SpVerwendung = "VERWENDUNG";

        /// <summary>
        /// Das <b>Baujahr</b> eines Gebaeudes — der Klartext der Baualtersklasse
        /// (<c>GebaeudeStammCtrl.Baualtersklassen</c>), nicht ihr Buchstabe.
        /// </summary>
        public const string SpBaujahr = "BAUJAHR";

        /// <summary>Die Wohn- bzw. Nutzflaeche eines Gebaeudes in m² (<c>Wohnflaeche_gesamt</c>).</summary>
        public const string SpFlaecheM2 = "FLAECHE";

        /// <summary>
        /// Die Zahl der <b>Tageskurven</b> eines Gebaeudetyps — fuenf oder acht zu je 24
        /// Stunden (<c>Tab_DBTagVDaten_STAMM</c>, Zeilen ÷ 24).
        /// </summary>
        public const string SpKurven = "KURVEN";

        /// <summary>
        /// Das <b>Zeitraster</b> eines Lastgangs der Lastspitzenkappung in Minuten (15 oder
        /// 60) — die Zahl hinter „Intervall".
        /// </summary>
        public const string SpIntervallMin = "INTERVALL";

        /// <summary>
        /// Das <b>Jahresmaximum</b> eines Lastgangs in kW — der hoechste abgelegte Wert, also
        /// die Bezugsgroesse der Lastspitzenkappung (Viertelstunden- bzw. Stundenleistung).
        /// Nicht <see cref="SpSpitzeKw"/>: Die ist der Hoechstwert der STUNDENreihe.
        /// </summary>
        public const string SpJahresmaximumKw = "JAHRESMAXIMUM";

        // ---- Gebaeudesimulation G3 (Welle B): Baustoffe und Bauteilaufbauten -------------

        /// <summary>Die Ordnungsgruppe eines Baustoffs (Mauerwerk, Beton, Dämmstoffe, …).</summary>
        public const string SpGruppe = "GRUPPE";

        /// <summary>Die Wärmeleitfähigkeit λ eines Baustoffs in W/(m·K).</summary>
        public const string SpLambda = "LAMBDA";

        /// <summary>Die Rohdichte ρ eines Baustoffs in kg/m³.</summary>
        public const string SpRho = "RHO";

        /// <summary>Die spezifische Wärmekapazität c eines Baustoffs in J/(kg·K).</summary>
        public const string SpCp = "CP";

        /// <summary>Die Bauteilart eines Aufbaus als Persistenzwert (<c>DbWerte.BAUTEILART_*</c>); leer = für jede.</summary>
        public const string SpBauteilart = "BAUTEILART";

        /// <summary>Die Zahl der Schichten eines Aufbaus.</summary>
        public const string SpSchichten = "SCHICHTEN";

        /// <summary>Die Gesamtdicke eines Aufbaus in m.</summary>
        public const string SpDicke = "DICKE";

        /// <summary>Der U-Wert eines Aufbaus in W/(m²·K) — gerechnet aus den Schichten (<c>BauteilaufbauCtrl.Kennwerte</c>).</summary>
        public const string SpUWert = "UWERT";

        /// <summary>Der Wärmedurchlasswiderstand R = Σ d/λ eines Aufbaus in m²·K/W.</summary>
        public const string SpRWert = "RWERT";

        /// <summary>Die flächenbezogene Wärmekapazität Σ ρ·c·d eines Aufbaus in kJ/(m²·K).</summary>
        public const string SpKapazitaet = "KAPAZITAET";

        /// <summary>
        /// <b>Der Ausdruck „ohne Wert"</b> — ein Gleichheitszeichen ohne Operand (Konzept_Katalogfilter
        /// V1: <c>=15</c> heißt „gleich 15", <c>=</c> allein „gleich nichts"). Er trifft genau die
        /// Zeilen, deren Zelle den Leerwert trägt; der Schalter „nur herstellerneutral" der
        /// Baustoffverwaltung setzt ihn auf die Spalte Hersteller — kein zweiter Filterweg,
        /// „Filter zurücksetzen" nimmt ihn mit.
        /// </summary>
        public const string AUSDRUCK_LEER = "=";

        /// <summary>
        /// Welche der acht Anlagenarten. <b>Nur bei den acht Anlagenkatalogen belegt</b>;
        /// die sechs Kataloge der Stufe S3 (Bedarf, Zeitreihen) sind keine Anlagen und
        /// stehen unter ihrem <see cref="Schluessel"/>.
        /// </summary>
        public Anlagenart Art { get; private set; }

        /// <summary>
        /// <b>Der sprachneutrale Schluessel dieses Katalogs</b> — die Kennung, unter der
        /// <see cref="Katalogfilterregister"/> den Filterstand fuehrt (Stufe S3).
        ///
        /// <para>Bis S2 war der Schluessel die <see cref="Anlagenart"/>; mit den sechs
        /// Katalogen aus S3 (drei Bedarfe, drei Zeitreihen) reicht sie nicht mehr, denn
        /// keiner davon ist eine Anlage. Statt einen zweiten Aufzaehlungstyp in
        /// <see cref="ParameterVerwendung"/> zu zwaengen — er beschreibt den
        /// VERWENDUNGSkatalog der Anlagen und wird gegen <c>pragma table_info</c>
        /// gehalten — traegt jedes Profil seinen Schluessel selbst.</para>
        ///
        /// <para>Die acht Anlagenkataloge tragen ihn nicht ein; fuer sie ist er
        /// <c>ANLAGE_</c> plus der Name der <see cref="Art"/> — damit bleibt der
        /// gemeinsame Filterstand aus S2.5 Zeichen fuer Zeichen derselbe.</para>
        /// </summary>
        public string Schluessel
        {
            get { return _schluessel.Length > 0 ? _schluessel : "ANLAGE_" + Art; }
            private set { _schluessel = value ?? ""; }
        }

        private string _schluessel = "";

        /// <summary>Die Spalten in der Reihenfolge der Liste (5 bis 9).</summary>
        public IReadOnlyList<Katalogspalte> Spalten { get; private set; }

        /// <summary>Die Spalte zu einem Schluessel; <c>null</c>, wenn es sie nicht gibt.</summary>
        public Katalogspalte Spalte(string schluessel)
        {
            if (Spalten == null || schluessel == null) return null;
            for (int i = 0; i < Spalten.Count; i++)
                if (Spalten[i].Schluessel == schluessel) return Spalten[i];
            return null;
        }

        // ==================================================================
        // Die acht Auspraegungen
        // ==================================================================

        /// <summary>
        /// Die Auspraegung zu einer Anlagenart. <paramref name="text"/> uebersetzt einen
        /// Beschriftungsschluessel; <c>null</c> liefert den Schluessel selbst zurueck
        /// (fuer Tests und fuer eine Umgebung ohne Katalog) — dasselbe Vorgehen wie
        /// <see cref="KatalogBrowserProfil.Finde"/>.
        /// </summary>
        public static Katalogfilterprofil Finde(Anlagenart art, Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            switch (art)
            {
                // ----------------------------------------------------------
                // 4.1 Heizkessel (63) - SECHS Spalten
                // ----------------------------------------------------------
                case Anlagenart.Heizkessel:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpBezeichner,  t("KFLT_SP_BEZEICHNER")),
                            new Katalogspalte(SpHersteller,  t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpBrennstoff,  t("KFLT_SP_BRENNSTOFF"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpPtherm,      t("KFLT_SP_PTHERM"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEta,         t("KFLT_SP_ETA"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            // Kennzeichen: nur der Sortierpfeil (5.6.2). Befund D-1 -
                            // 6 von 63 Saetzen tragen es, 46 Beschreibungen nennen
                            // "Brennwert"; die SUCHE ueber alle Spalten findet die 46.
                            new Katalogspalte(SpBrennwert,   t("KFLT_SP_BRENNWERT"), "", Katalogspaltenart.JaNein, rang: Katalogspaltenrang.Breit)
                        }
                    };

                // ----------------------------------------------------------
                // 4.2 BHKW (79) - ACHT Spalten, darunter die abgeleitete Stromkennzahl
                // ----------------------------------------------------------
                case Anlagenart.Bhkw:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpBezeichner, t("KFLT_SP_BEZEICHNER")),
                            new Katalogspalte(SpHersteller, t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpBrennstoff, t("KFLT_SP_BRENNSTOFF"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpPel,        t("KFLT_SP_PEL"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpPtherm,     t("KFLT_SP_PTHERM"), "kW", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpSigma,      t("KFLT_SP_SIGMA"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit),
                            new Katalogspalte(SpEta,        t("KFLT_SP_ETA"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            // 45 verschiedene Werte in 79 Saetzen (Befund O-3): als
                            // Spalte mit Feld tauglich, als Klappliste nie gewesen.
                            new Katalogspalte(SpMotortyp,   t("KFLT_SP_MOTORTYP"), rang: Katalogspaltenrang.Breit)
                        }
                    };

                // ----------------------------------------------------------
                // 4.3 Waermepumpe (51) - NEUN Spalten
                // ----------------------------------------------------------
                case Anlagenart.Waermepumpe:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpHersteller,   t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            // Der Bezeichner heisst hier "Modell" - der Vorlaeufer
                            // Form_WpFilterAuswahl nannte ihn so.
                            new Katalogspalte(SpBezeichner,   t("KFLT_SP_MODELL")),
                            new Katalogspalte(SpQuelle,       t("KFLT_SP_QUELLE"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpNennleistung, t("KFLT_SP_NENNLEISTUNG"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpVlMin,        t("KFLT_SP_VLMIN"), "°C", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit),
                            new Katalogspalte(SpVlMax,        t("KFLT_SP_VLMAX"), "°C", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpZuheizung,    t("KFLT_SP_ZUHEIZUNG"), "kW", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit),
                            // 16.09.2026: die ZAHL statt des Kennzeichens „Kuehlen" - sie
                            // sagt dasselbe und nennt die Leistung; das schnelle Ja/Nein
                            // traegt der Schalter „nur mit Kuehlfunktion", der genau
                            // diese Spalte auf ">0" setzt.
                            new Katalogspalte(SpKuehlleistung, t("KFLT_SP_KUEHLLEISTUNG"), "kW", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpCop,          t("KFLT_SP_COP"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit)
                        }
                    };

                // ----------------------------------------------------------
                // 4.4 Solarkollektoren (7) - SECHS Spalten
                // ----------------------------------------------------------
                case Anlagenart.Solarkollektoren:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpBezeichner,   t("KFLT_SP_BEZEICHNER")),
                            new Katalogspalte(SpHersteller,   t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpKollektortyp, t("KFLT_SP_KOLLEKTORTYP"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpApertur,      t("KFLT_SP_APERTUR"), "m²", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEtaNull,      t("KFLT_SP_ETANULL"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpK1,           t("KFLT_SP_K1"), "W/(m²·K)", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz)
                        }
                    };

                // ----------------------------------------------------------
                // 4.5 Pufferspeicher (13) - FUENF Spalten
                // ----------------------------------------------------------
                case Anlagenart.Pufferspeicher:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpBezeichner,  t("KFLT_SP_BEZEICHNER")),
                            new Katalogspalte(SpHersteller,  t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpSpeichertyp, t("KFLT_SP_SPEICHERTYP"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpVolumen,     t("KFLT_SP_VOLUMEN"), "l", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpVerluste,    t("KFLT_SP_VERLUSTE"), "kWh/d", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz)
                        }
                    };

                // ----------------------------------------------------------
                // 4.6 PV-Module (6 -> 20 749) - SIEBEN Spalten
                // ----------------------------------------------------------
                case Anlagenart.Photovoltaik:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpBezeichner,   t("KFLT_SP_BEZEICHNER")),
                            new Katalogspalte(SpHersteller,   t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpPstc,         t("KFLT_SP_PSTC"), "W", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEta,          t("KFLT_SP_ETA"), "%", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpTechnologie,  t("KFLT_SP_TECHNOLOGIE"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpModulflaeche, t("KFLT_SP_MODULFLAECHE"), "m²", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit),
                            new Katalogspalte(SpTnoct,        t("KFLT_SP_TNOCT"), "°C", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit)
                        }
                    };

                // ----------------------------------------------------------
                // 4.7 Wechselrichter (1 -> 2 343) - SIEBEN Spalten
                // ----------------------------------------------------------
                case Anlagenart.Wechselrichter:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpBezeichner, t("KFLT_SP_BEZEICHNER")),
                            new Katalogspalte(SpHersteller, t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpPac,        t("KFLT_SP_PAC"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEtaEuro,    t("KFLT_SP_ETAEURO"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpMppt,       t("KFLT_SP_MPPT"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpUdcMax,     t("KFLT_SP_UDCMAX"), "V", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit),
                            new Katalogspalte(SpHerkunft,   t("KFLT_SP_HERKUNFT"), rang: Katalogspaltenrang.BeiPlatz)
                        }
                    };

                // ----------------------------------------------------------
                // 4.8 Stromspeicher (5 -> 6 658) - ACHT Spalten
                // ----------------------------------------------------------
                case Anlagenart.Stromspeicher:
                    return new Katalogfilterprofil
                    {
                        Art = art,
                        Spalten = new[]
                        {
                            new Katalogspalte(SpBezeichner, t("KFLT_SP_BEZEICHNER")),
                            // Befund D-3: Tab_Stromspeicher_STAMM hat keine Spalte
                            // Firma; der Hersteller kommt aus dem Bezeichnerpraefix
                            // (Text vor dem ersten Doppelpunkt), wie ihn der Import
                            // schreibt. Q7 - die Spalte selbst ist ein Schemaschritt.
                            new Katalogspalte(SpHersteller, t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpChemie,     t("KFLT_SP_CHEMIE"), rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpEnergie,    t("KFLT_SP_ENERGIE"), "kWh", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpLeistung,   t("KFLT_SP_LEISTUNG"), "kW", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpCrate,      t("KFLT_SP_CRATE"), "1/h", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit),
                            new Katalogspalte(SpEtaRt,      t("KFLT_SP_ETART"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz),
                            new Katalogspalte(SpZyklen,     t("KFLT_SP_ZYKLEN"), "", Katalogspaltenart.Zahl, rang: Katalogspaltenrang.Breit)
                        }
                    };
            }

            throw new ArgumentOutOfRangeException(nameof(art));
        }

        // ==================================================================
        // Stufe S3.1 - die drei BEDARFSKATALOGE (Konzept 2.9 und 4.9)
        // ==================================================================

        /// <summary>
        /// <b>Die Auspraegung eines Bedarfskatalogs</b> (Stufe <b>S3.1</b>,
        /// Konzept_Katalogfilter 4.9) — Brauchwasser (16), Prozesswaerme (32) und
        /// Stromverbraucher (41) sind Drillinge DERSELBEN Tabellenform
        /// (<c>Bezeichner</c>, <c>Typ</c>, <c>Beschreibung</c>, <c>Monat_1…12</c>,
        /// <c>ReadOnly</c>), und sie bekommen deshalb DASSELBE Profil.
        ///
        /// <para><b>Vier Spalten:</b> Bezeichner · Typ (die Profilzuordnung — der
        /// Grund, warum zwei gleich grosse Bedarfe verschieden rechnen) · Jahressumme
        /// [MWh] · Beschreibung. Die Beschreibung steht als SPALTE und
        /// nicht nur im Suchraum, weil die VDI-6002-Saetze ihren Kennwert im Text
        /// tragen; ein Trichter „enthaelt 28 l" findet sie.</para>
        ///
        /// <para><b>Die Auslieferung ist keine Spalte mehr</b> (Konzept
        /// Administrationsdialoge, V10; Entscheid AD-Q13): Ein Auslieferungssatz traegt
        /// das Schloss hinter seinem Bezeichner (<see cref="Katalogfilterzeile.Geschuetzt"/>),
        /// die Spalte nahm der Liste nur Breite. Der Wert <see cref="SpAuslieferung"/>
        /// steht weiter in der Zeile.</para>
        /// </summary>
        public static Katalogfilterprofil FuerBedarf(BedarfsArt art, Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            return new Katalogfilterprofil
            {
                Schluessel = "BEDARF_" + art,
                Spalten = new[]
                {
                    new Katalogspalte(SpBezeichner,      t("KFLT_SP_BEZEICHNER")),
                    new Katalogspalte(SpTyp,             t("KFLT_SP_TYP"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpJahressummeMwh,  t("KFLT_SP_JAHRESSUMME"), "MWh", Katalogspaltenart.Zahl),
                    new Katalogspalte(SpBeschreibung,    t("KFLT_SP_BESCHREIBUNG"), rang: Katalogspaltenrang.Breit)
                }
            };
        }

        // ==================================================================
        // Stufe S3.2 - die drei ZEITREIHENKATALOGE (Konzept 2.10 und 4.10)
        // ==================================================================

        /// <summary>
        /// <b>Die Auspraegung eines Zeitreihenkatalogs</b> (Stufe <b>S3.2</b>,
        /// Konzept_Katalogfilter 4.10) — Waermebedarf-Lastgang (4), Stromganglinie (3)
        /// und Solarthermieganglinie (1).
        ///
        /// <para><b>Die drei unterscheiden sich in genau zwei Spalten</b>: Das
        /// Zeitintervall fuehrt nur die Stromganglinie (<c>Tab_Stromganglinie_STAMM.
        /// Zeitinterval</c>), die Beschreibung nur die Solarganglinie — die anderen
        /// zwei Kopftabellen haben die Spalte gar nicht.</para>
        ///
        /// <para><b>Jahresarbeit und Spitze stehen NICHT am Kopfsatz</b>, sondern in der
        /// Wertetabelle (78 840 bzw. 35 040 Zeilen). Sie kommen aus EINER
        /// Gruppenabfrage je Katalog — <see cref="GanglinienAuswertungCtrl.Kennzahlen"/>
        /// —, nicht je Zeile, und sie sind dieselben Zahlen, die die Grafik der
        /// Ganglinien-Dialoge zeigt (W9-E-3 / W12-E-2).</para>
        /// </summary>
        public static Katalogfilterprofil FuerZeitreihe(Zeitreihenart art,
                                                        Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            var spalten = new List<Katalogspalte>
            {
                new Katalogspalte(SpBezeichner, t("KFLT_SP_BEZEICHNER"))
            };

            if (art == Zeitreihenart.Stromganglinie)
                spalten.Add(new Katalogspalte(SpZeitintervall, t("KFLT_SP_ZEITINTERVALL"), "",
                                              Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz));

            if (art == Zeitreihenart.Solarganglinie)
                spalten.Add(new Katalogspalte(SpBeschreibung, t("KFLT_SP_BESCHREIBUNG"),
                                              rang: Katalogspaltenrang.BeiPlatz));

            spalten.Add(new Katalogspalte(SpJahresarbeitMwh, t("KFLT_SP_JAHRESARBEIT"), "MWh",
                                          Katalogspaltenart.Zahl));
            spalten.Add(new Katalogspalte(SpSpitzeKw, t("KFLT_SP_SPITZE"), "kW",
                                          Katalogspaltenart.Zahl, rang: Katalogspaltenrang.BeiPlatz));

            return new Katalogfilterprofil { Schluessel = "ZEITREIHE_" + art, Spalten = spalten };
        }

        // ==================================================================
        // Zapfprofilgenerator - der Katalog der Brauchwasser-Nutzungsarten
        // ==================================================================

        /// <summary>Der Schluessel des Filterstands der Nutzungsarten.</summary>
        public const string SCHLUESSEL_TWW_NUTZUNGSART = "TWW_NUTZUNGSART";

        /// <summary>
        /// <b>Die Auspraegung des Katalogs der Brauchwasser-Nutzungsarten</b>
        /// (Umsetzungskonzept Zapfprofilgenerator 5.4, Stufe Z0, Posten P8) — die
        /// Datenseite der Katalogliste des spaeteren <c>TwwNutzungsartAdminDialog</c>.
        ///
        /// <para><b>Sechs Spalten</b> nach 5.4: Nutzungsart (der Bezeichner) · Bezugsart ·
        /// Kalender · Herkunft der Bedarfswerte · Katalogversion · Status. Kennwerte je
        /// Niveau stehen bewusst NICHT in der Liste, sondern im Stammblatt daneben —
        /// die Liste waehlt, sie vergleicht keine Zahlen.</para>
        ///
        /// <para><b>Rang</b> (Stufe Z4, Katalogprobe N23: ohne Rang rollte die Liste bei
        /// 1 088 px um 58 px, bei 400 px um 350 px quer): Nutzungsart und Bezugsart stehen
        /// immer; Kalender und Katalogversion bei Platz; Herkunft und Status weichen als
        /// erste — das Schloss zeigt die Auslieferung ohnehin, und das Stammblatt nennt
        /// beide.</para>
        ///
        /// <para>Die Zeilen liefert <c>TwwNutzungsartCtrl.Katalogfilterzeilen</c>; ihr
        /// <see cref="Katalogfilterzeile.Schluessel"/> ist die ID, weil der Bezeichner
        /// allein ueber zwei Katalogversionen nicht eindeutig ist.</para>
        /// </summary>
        public static Katalogfilterprofil FuerTwwNutzungsart(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            return new Katalogfilterprofil
            {
                Schluessel = SCHLUESSEL_TWW_NUTZUNGSART,
                Spalten = new[]
                {
                    new Katalogspalte(SpBezeichner,     t("KFLT_SP_NUTZUNGSART")),
                    new Katalogspalte(SpBezugsart,      t("KFLT_SP_BEZUGSART")),
                    new Katalogspalte(SpKalender,       t("KFLT_SP_KALENDER"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpHerkunft,       t("KFLT_SP_HERKUNFT"), rang: Katalogspaltenrang.Breit),
                    new Katalogspalte(SpKatalogversion, t("KFLT_SP_KATALOGVERSION"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpStatus,         t("KFLT_SP_STATUS"), rang: Katalogspaltenrang.Breit)
                }
            };
        }

        // ==================================================================
        // Stufe 5 der Neuordnung (V16) - die drei Sonderlisten
        // ==================================================================

        /// <summary>Der Schluessel des Filterstands der Gebaeudeverwaltung.</summary>
        public const string SCHLUESSEL_GEBAEUDE = "GEBAEUDE";

        /// <summary>Der Schluessel des Filterstands der Gebaeudetypen.</summary>
        public const string SCHLUESSEL_GEBAEUDETYP = "GEBAEUDETYP";

        /// <summary>Der Schluessel des Filterstands der Lastgaenge der Lastspitzenkappung.</summary>
        public const string SCHLUESSEL_LASTGANG = "LASTGANG";

        /// <summary>
        /// <b>Die Gebaeudeverwaltung</b> (Konzept Administrationsdialoge, V16; Bestand A9) —
        /// fuenf Spalten: Name, Gebaeudeart, Verwendung, Baujahr, Flaeche.
        ///
        /// <para><b>Die vier Vorfilter der eigenen Tabelle werden Trichter</b>: Verwendung,
        /// Gebaeudeart und Baujahr sind Spalten mit Trichter, das Suchmuster ist die Suche
        /// ueber alle Spalten. Die Zeilen liefert <c>GebaeudeStammCtrl.Katalogfilterzeilen</c>;
        /// ein Auslieferungssatz traegt das Schloss (<see cref="Katalogfilterzeile.Geschuetzt"/>).</para>
        ///
        /// <para><b>Rang:</b> Name und Flaeche stehen immer; Gebaeudeart und Baujahr bei Platz;
        /// die Verwendung weicht als erste — sie ist die grobe Einteilung, die der Name meist
        /// schon verraet, und eine gefilterte Spalte weicht ohnehin nie.</para>
        /// </summary>
        public static Katalogfilterprofil FuerGebaeude(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            return new Katalogfilterprofil
            {
                Schluessel = SCHLUESSEL_GEBAEUDE,
                Spalten = new[]
                {
                    new Katalogspalte(SpBezeichner, t("KFLT_SP_NAME")),
                    new Katalogspalte(SpGebaeudeart, t("KFLT_SP_GEBAEUDEART"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpVerwendung, t("KFLT_SP_VERWENDUNG"), rang: Katalogspaltenrang.Breit),
                    new Katalogspalte(SpBaujahr, t("KFLT_SP_BAUJAHR"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpFlaecheM2, t("KFLT_SP_FLAECHE"), "m²", Katalogspaltenart.Zahl)
                }
            };
        }

        /// <summary>
        /// <b>Die Gebaeudetypen</b> (V16; Bestand A10) — Name, Zahl der Tageskurven und
        /// Beschreibung. Bis Stufe 5 stand hier eine Typliste mit rundem Wahlknopf und nur dem
        /// Namen; die Zeilen liefert <c>TagVCtrl.Katalogfilterzeilen</c>, ein nicht
        /// veraenderbarer Typ traegt das Schloss.
        /// </summary>
        public static Katalogfilterprofil FuerGebaeudetyp(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            return new Katalogfilterprofil
            {
                Schluessel = SCHLUESSEL_GEBAEUDETYP,
                Spalten = new[]
                {
                    new Katalogspalte(SpBezeichner, t("KFLT_SP_NAME")),
                    new Katalogspalte(SpKurven, t("KFLT_SP_KURVEN"), "", Katalogspaltenart.Zahl),
                    new Katalogspalte(SpBeschreibung, t("KFLT_SP_BESCHREIBUNG"), rang: Katalogspaltenrang.BeiPlatz)
                }
            };
        }

        /// <summary>
        /// <b>Die Lastgaenge der Lastspitzenkappung</b> (V16; Bestand A11) — Lastgang, Quelle
        /// (Stamm, Projekt oder Datei), Intervall in Minuten und Jahresmaximum in kW.
        ///
        /// <para><b>Ein Rechenwerkzeug, kein Katalog:</b> Die Liste ersetzt Optionsgruppe und
        /// Klappliste; eine eingelesene Datei erscheint als Zeile mit der Quelle „Datei" und
        /// wird nicht abgelegt. Die Zeilen der Datenbank liefert
        /// <c>PeakShavingCtrl.Katalogfilterzeilen</c>.</para>
        /// </summary>
        public static Katalogfilterprofil FuerLastgang(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            return new Katalogfilterprofil
            {
                Schluessel = SCHLUESSEL_LASTGANG,
                Spalten = new[]
                {
                    new Katalogspalte(SpBezeichner, t("KFLT_SP_LASTGANG")),
                    new Katalogspalte(SpQuelle, t("KFLT_SP_QUELLE"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpIntervallMin, t("KFLT_SP_INTERVALL"), "min", Katalogspaltenart.Zahl,
                                      rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpJahresmaximumKw, t("KFLT_SP_JAHRESMAXIMUM"), "kW", Katalogspaltenart.Zahl)
                }
            };
        }

        // ==================================================================
        // Gebaeudesimulation G3 (Welle B) - Baustoffe und Bauteilaufbauten
        // ==================================================================

        /// <summary>Der Schluessel des Filterstands des Baustoffkatalogs (Registerschluessel <c>BAUSTOFF</c>).</summary>
        public const string SCHLUESSEL_BAUSTOFF = "BAUSTOFF";

        /// <summary>Der Schluessel des Filterstands des Aufbaukatalogs (Registerschluessel <c>BAUTEILAUFBAU</c>, W21).</summary>
        public const string SCHLUESSEL_BAUTEILAUFBAU = "BAUTEILAUFBAU";

        /// <summary>
        /// <b>Der Baustoffkatalog</b> (Schritt S-A) — sieben Spalten: Name, Gruppe, Hersteller, λ,
        /// ρ, c und Quelle. Die Zeilen liefert <c>BaustoffCtrl.Katalogfilterzeilen</c>; ein Satz
        /// der Auslieferung traegt das Schloss.
        ///
        /// <para><b>Rang:</b> Name und λ stehen immer — λ ist die Zahl, nach der ein Anwender
        /// einen Stoff waehlt; Gruppe, ρ und c bei Platz; Hersteller und Quelle weichen als erste
        /// (herstellerneutral ist die Saat, die Quelle nennt das Stammblatt).</para>
        /// </summary>
        public static Katalogfilterprofil FuerBaustoff(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            return new Katalogfilterprofil
            {
                Schluessel = SCHLUESSEL_BAUSTOFF,
                Spalten = new[]
                {
                    new Katalogspalte(SpBezeichner, t("KFLT_SP_NAME")),
                    new Katalogspalte(SpGruppe, t("KFLT_SP_GRUPPE"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpHersteller, t("KFLT_SP_HERSTELLER"), rang: Katalogspaltenrang.Breit),
                    new Katalogspalte(SpLambda, t("KFLT_SP_LAMBDA"), "W/(m·K)", Katalogspaltenart.Zahl),
                    new Katalogspalte(SpRho, t("KFLT_SP_RHO"), "kg/m³", Katalogspaltenart.Zahl,
                                      rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpCp, t("KFLT_SP_CP"), "J/(kg·K)", Katalogspaltenart.Zahl,
                                      rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpQuelle, t("KFLT_SP_QUELLE"), rang: Katalogspaltenrang.Breit)
                }
            };
        }

        /// <summary>
        /// <b>Der Aufbaukatalog</b> (Schritt S-B, Oberfläche Welle C) — Name, Bauteilart, U, R,
        /// flächenbezogene Kapazität, Zahl der Schichten, Gesamtdicke und Herkunft. Die Zeilen
        /// liefert <c>BauteilaufbauCtrl.Katalogfilterzeilen</c>; U, R und C rechnet er über den
        /// Bauteilweg, dieselbe Rechnung wie der Summenfuß des Stammblatts.
        ///
        /// <para><b>Rang:</b> Name und U stehen immer — der U-Wert ist die Zahl, nach der ein
        /// Aufbau gewählt wird; Bauteilart, R, C und Schichtzahl bei Platz; Dicke und Herkunft
        /// weichen als erste.</para>
        /// </summary>
        public static Katalogfilterprofil FuerBauteilaufbau(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            return new Katalogfilterprofil
            {
                Schluessel = SCHLUESSEL_BAUTEILAUFBAU,
                Spalten = new[]
                {
                    new Katalogspalte(SpBezeichner, t("KFLT_SP_NAME")),
                    new Katalogspalte(SpBauteilart, t("KFLT_SP_BAUTEILART"), rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpUWert, t("KFLT_SP_UWERT"), "W/(m²·K)", Katalogspaltenart.Zahl),
                    new Katalogspalte(SpRWert, t("KFLT_SP_RWERT"), "m²·K/W", Katalogspaltenart.Zahl,
                                      rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpKapazitaet, t("KFLT_SP_KAPAZITAET"), "kJ/(m²·K)", Katalogspaltenart.Zahl,
                                      rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpSchichten, t("KFLT_SP_SCHICHTEN"), "", Katalogspaltenart.Zahl,
                                      rang: Katalogspaltenrang.BeiPlatz),
                    new Katalogspalte(SpDicke, t("KFLT_SP_DICKE"), "m", Katalogspaltenart.Zahl,
                                      rang: Katalogspaltenrang.Breit),
                    new Katalogspalte(SpHerkunft, t("KFLT_SP_HERKUNFT"), rang: Katalogspaltenrang.Breit)
                }
            };
        }

        /// <summary>
        /// <b>Ein Profil aus freien Spalten</b> (Stufe <b>S3.4</b>) — der Weg der zwei
        /// IMPORTMASKEN, deren Spalten schon als Daten dastehen
        /// (<c>KatalogImportProfil.Spalten</c> / <c>ModulImportProfil.Spalten</c>).
        ///
        /// <para><b>Warum keine neunte Auspraegung.</b> Die Importliste zeigt
        /// KANDIDATEN, keine Katalogsaetze: Ihre Spalten haengen an der Quelle (CEC,
        /// PAN, OND, VDI 3805) und stehen bereits im Importprofil. Eine zweite,
        /// handgeschriebene Liste hier waere die vierzehnte Wahrheit darueber, was ein
        /// Katalog zeigt — genau das, was Kapitel 3.1 vermeidet.</para>
        /// </summary>
        public static Katalogfilterprofil AusSpalten(string schluessel,
                                                     IEnumerable<Katalogspalte> spalten)
        {
            return new Katalogfilterprofil
            {
                Schluessel = schluessel ?? "",
                Spalten = new List<Katalogspalte>(spalten ?? new Katalogspalte[0])
            };
        }

        /// <summary>
        /// <b>Dieselbe Auspraegung MIT der Spalte „im Projekt verwendet"</b> (Frage
        /// <b>Q12</b>, Stufe S2.3) — sie haengt hinten an und gilt NUR fuer die sieben
        /// Projektdialoge.
        ///
        /// <para><b>Warum eine zweite Fabrikmethode und kein zweites Profil.</b> Die
        /// Spalten sind dieselben; es kommt EINE dazu. Ein eigener Satz je
        /// Projektdialog waere die vierzehnte Wahrheit darueber, was ein Katalog
        /// zeigt — genau das, was Kapitel 3.1 vermeidet.</para>
        ///
        /// <para><b>Sie steht HINTEN.</b> Vorne stuenden die Parameter, nach denen
        /// gesucht wird, weiter hinten; und der Anwender liest die Liste von links
        /// nach rechts als Geraetebeschreibung. „Verwendet" ist eine Auskunft ueber
        /// das PROJEKT, keine ueber das Geraet.</para>
        /// </summary>
        public static Katalogfilterprofil MitVerwendung(Anlagenart art,
                                                        Func<string, string> text = null)
        {
            return Finde(art, text).MitVerwendungsspalte(text);
        }

        /// <summary>
        /// <b>Dasselbe Profil MIT der Spalte „im Projekt verwendet"</b> — die
        /// Instanzfassung von <see cref="MitVerwendung"/> (Stufe S3.1).
        ///
        /// <para>Sie gibt es, seit auch ein BEDARFSKATALOG einen Projektdialog hat
        /// (<c>BedarfsProfileDialog</c>, Frage <b>Q12</b>): Die Spalte haengt am
        /// Profil, nicht an der <see cref="Anlagenart"/>, und ein Profil, das keine
        /// Anlagenart fuehrt, braucht denselben Weg.</para>
        /// </summary>
        public Katalogfilterprofil MitVerwendungsspalte(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            var spalten = new List<Katalogspalte>(Spalten)
            {
                new Katalogspalte(SpVerwendet, t("KFLT_SP_VERWENDET"), "",
                                  Katalogspaltenart.JaNein)
            };

            return new Katalogfilterprofil
            {
                Art = Art,
                Schluessel = _schluessel,
                Spalten = spalten
            };
        }

        /// <summary>Alle acht Auspraegungen — fuer Stapelpruefungen.</summary>
        public static IEnumerable<Anlagenart> AlleArten
        {
            get
            {
                yield return Anlagenart.Heizkessel;
                yield return Anlagenart.Bhkw;
                yield return Anlagenart.Waermepumpe;
                yield return Anlagenart.Solarkollektoren;
                yield return Anlagenart.Pufferspeicher;
                yield return Anlagenart.Photovoltaik;
                yield return Anlagenart.Wechselrichter;
                yield return Anlagenart.Stromspeicher;
            }
        }
    }
}
