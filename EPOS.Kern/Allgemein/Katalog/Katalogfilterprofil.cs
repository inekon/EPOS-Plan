using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
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
                             bool sortierbar = true, bool filterbar = true)
        {
            Schluessel = schluessel;
            Titel = titel;
            Einheit = einheit ?? "";
            Art = art;
            Sortierbar = sortierbar;
            Filterbar = filterbar && art != Katalogspaltenart.JaNein;
        }

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
        public const string SpKuehlen = "KUEHLEN";
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

        /// <summary>Welche der acht Anlagenarten.</summary>
        public Anlagenart Art { get; private set; }

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
                            new Katalogspalte(SpHersteller,  t("KFLT_SP_HERSTELLER")),
                            new Katalogspalte(SpBrennstoff,  t("KFLT_SP_BRENNSTOFF")),
                            new Katalogspalte(SpPtherm,      t("KFLT_SP_PTHERM"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEta,         t("KFLT_SP_ETA"), "", Katalogspaltenart.Zahl),
                            // Kennzeichen: nur der Sortierpfeil (5.6.2). Befund D-1 -
                            // 6 von 63 Saetzen tragen es, 46 Beschreibungen nennen
                            // "Brennwert"; die SUCHE ueber alle Spalten findet die 46.
                            new Katalogspalte(SpBrennwert,   t("KFLT_SP_BRENNWERT"), "", Katalogspaltenart.JaNein)
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
                            new Katalogspalte(SpHersteller, t("KFLT_SP_HERSTELLER")),
                            new Katalogspalte(SpBrennstoff, t("KFLT_SP_BRENNSTOFF")),
                            new Katalogspalte(SpPel,        t("KFLT_SP_PEL"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpPtherm,     t("KFLT_SP_PTHERM"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpSigma,      t("KFLT_SP_SIGMA"), "", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEta,        t("KFLT_SP_ETA"), "", Katalogspaltenart.Zahl),
                            // 45 verschiedene Werte in 79 Saetzen (Befund O-3): als
                            // Spalte mit Feld tauglich, als Klappliste nie gewesen.
                            new Katalogspalte(SpMotortyp,   t("KFLT_SP_MOTORTYP"))
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
                            new Katalogspalte(SpHersteller,   t("KFLT_SP_HERSTELLER")),
                            // Der Bezeichner heisst hier "Modell" - der Vorlaeufer
                            // Form_WpFilterAuswahl nannte ihn so.
                            new Katalogspalte(SpBezeichner,   t("KFLT_SP_MODELL")),
                            new Katalogspalte(SpQuelle,       t("KFLT_SP_QUELLE")),
                            new Katalogspalte(SpNennleistung, t("KFLT_SP_NENNLEISTUNG"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpVlMin,        t("KFLT_SP_VLMIN"), "°C", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpVlMax,        t("KFLT_SP_VLMAX"), "°C", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpZuheizung,    t("KFLT_SP_ZUHEIZUNG"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpKuehlen,      t("KFLT_SP_KUEHLEN"), "", Katalogspaltenart.JaNein),
                            new Katalogspalte(SpCop,          t("KFLT_SP_COP"), "", Katalogspaltenart.Zahl)
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
                            new Katalogspalte(SpHersteller,   t("KFLT_SP_HERSTELLER")),
                            new Katalogspalte(SpKollektortyp, t("KFLT_SP_KOLLEKTORTYP")),
                            new Katalogspalte(SpApertur,      t("KFLT_SP_APERTUR"), "m²", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEtaNull,      t("KFLT_SP_ETANULL"), "", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpK1,           t("KFLT_SP_K1"), "W/(m²·K)", Katalogspaltenart.Zahl)
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
                            new Katalogspalte(SpHersteller,  t("KFLT_SP_HERSTELLER")),
                            new Katalogspalte(SpSpeichertyp, t("KFLT_SP_SPEICHERTYP")),
                            new Katalogspalte(SpVolumen,     t("KFLT_SP_VOLUMEN"), "l", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpVerluste,    t("KFLT_SP_VERLUSTE"), "kWh/d", Katalogspaltenart.Zahl)
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
                            new Katalogspalte(SpHersteller,   t("KFLT_SP_HERSTELLER")),
                            new Katalogspalte(SpPstc,         t("KFLT_SP_PSTC"), "W", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEta,          t("KFLT_SP_ETA"), "%", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpTechnologie,  t("KFLT_SP_TECHNOLOGIE")),
                            new Katalogspalte(SpModulflaeche, t("KFLT_SP_MODULFLAECHE"), "m²", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpTnoct,        t("KFLT_SP_TNOCT"), "°C", Katalogspaltenart.Zahl)
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
                            new Katalogspalte(SpHersteller, t("KFLT_SP_HERSTELLER")),
                            new Katalogspalte(SpPac,        t("KFLT_SP_PAC"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEtaEuro,    t("KFLT_SP_ETAEURO"), "", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpMppt,       t("KFLT_SP_MPPT"), "", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpUdcMax,     t("KFLT_SP_UDCMAX"), "V", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpHerkunft,   t("KFLT_SP_HERKUNFT"))
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
                            new Katalogspalte(SpHersteller, t("KFLT_SP_HERSTELLER")),
                            new Katalogspalte(SpChemie,     t("KFLT_SP_CHEMIE")),
                            new Katalogspalte(SpEnergie,    t("KFLT_SP_ENERGIE"), "kWh", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpLeistung,   t("KFLT_SP_LEISTUNG"), "kW", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpCrate,      t("KFLT_SP_CRATE"), "1/h", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpEtaRt,      t("KFLT_SP_ETART"), "", Katalogspaltenart.Zahl),
                            new Katalogspalte(SpZyklen,     t("KFLT_SP_ZYKLEN"), "", Katalogspaltenart.Zahl)
                        }
                    };
            }

            throw new ArgumentOutOfRangeException(nameof(art));
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
            Func<string, string> t = text ?? (s => s);

            Katalogfilterprofil grund = Finde(art, text);
            var spalten = new List<Katalogspalte>(grund.Spalten)
            {
                new Katalogspalte(SpVerwendet, t("KFLT_SP_VERWENDET"), "",
                                  Katalogspaltenart.JaNein)
            };

            return new Katalogfilterprofil { Art = art, Spalten = spalten };
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
