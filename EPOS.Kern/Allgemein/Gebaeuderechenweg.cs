using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Welcher Rechenweg für ein Gebäude gilt</b> — die Auskunft für Dialog und Hülle
    /// (Stufe G1; Umsetzungskonzept Gebäudesimulation 2.3, 2.7; ADR-006).
    ///
    /// <para><b>Die Anzeige folgt der Rechnung.</b> Die Regel steht einmal, an der Weiche
    /// (<c>SimulationWaermebedarf.RechenwegWaehlen</c>): <c>VDI6007</c> führt auf den
    /// VDI-Weg, <c>TAGESBILANZ</c> und jeder unbekannte Wert auf den Tagesbilanz-Weg, und
    /// NULL folgt <c>SimulationWaermebedarf.MODELL_OHNE_ANGABE</c> — dem VDI-Weg. Diese Klasse
    /// liest dieselbe Konstante; der Dialog zeigt ein Gebäude ohne Angabe deshalb als
    /// „VDI 6007".</para>
    ///
    /// <para>Rein, ohne Datenbank; öffentlich, weil der Katalogeditor in <c>EPOS.UI</c> sie
    /// braucht (dieselbe Lage wie <see cref="Gebaeudebauweise"/>).</para>
    /// </summary>
    public static class Gebaeuderechenweg
    {
        /// <summary>
        /// Der Rechenweg eines Gebäudes ohne Angabe (Spaltenwert NULL):
        /// <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/>.
        /// </summary>
        public static string OhneAngabe => SimulationWaermebedarf.MODELL_OHNE_ANGABE;

        /// <summary>
        /// Der Rechenweg, auf dem ein Gebäude mit dem Spaltenwert <paramref name="modell"/>
        /// tatsächlich rechnet: <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/> oder
        /// <see cref="DbWerte.GEBAEUDE_MODELL_TAGESBILANZ"/>.
        /// </summary>
        public static string Wirksam(string modell)
        {
            string m = modell ?? OhneAngabe;
            return string.Equals(m, DbWerte.GEBAEUDE_MODELL_VDI6007, StringComparison.Ordinal)
                ? DbWerte.GEBAEUDE_MODELL_VDI6007
                : DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        }

        /// <summary>Rechnet ein Gebäude mit diesem Spaltenwert auf dem VDI-Weg?</summary>
        public static bool IstVdi6007(string modell)
            => Wirksam(modell) == DbWerte.GEBAEUDE_MODELL_VDI6007;
    }

    /// <summary>
    /// <b>Die Vorgaben der Modellparameter</b>, die der Kern für eine leere Spalte einsetzt
    /// (<c>GebaeudeFestwerte.VORGABE_*</c>) — öffentlich, damit der Gebäudedialog sie als
    /// Platzhalter „Vorgabe …" zeigt, statt eine zweite Zahl zu führen (Umsetzungskonzept
    /// Gebäudesimulation 2.4). Der Dialog schreibt NULL, nicht diese Werte.
    /// </summary>
    public static class Gebaeudemodellvorgaben
    {
        /// <summary>Rahmenanteil der Fenster [–].</summary>
        public static double Rahmenanteil => GebaeudeFestwerte.VORGABE_RAHMENANTEIL;

        /// <summary>Verschattungsfaktor [–].</summary>
        public static double Verschattungsfaktor => GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR;

        /// <summary>Masseanteil außen [–].</summary>
        public static double MasseanteilAussen => GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN;

        /// <summary>Innenflächenfaktor [–].</summary>
        public static double Innenflaechenfaktor => GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR;

        /// <summary>Strahlungsanteil der Heizung [–].</summary>
        public static double HeizungStrahlungsanteil => GebaeudeFestwerte.VORGABE_HEIZUNG_STRAHLUNGSANTEIL;

        /// <summary>Kellertemperatur [°C] bei Randbedingung Keller.</summary>
        public static double Kellertemperatur => GebaeudeFestwerte.VORGABE_KELLERTEMPERATUR;

        /// <summary>Infiltration [1/h] (Stufe G2).</summary>
        public static double LuftwechselInfiltration => GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION;

        /// <summary>Nutzerlüftung [1/h] (Stufe G2).</summary>
        public static double LuftwechselNutzer => GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER;

        /// <summary>Luftwechsel der Sommerlüftung [1/h] (Stufe G2).</summary>
        public static double LuftwechselSommer => GebaeudeFestwerte.SOMMERLUEFTUNG_LUFTWECHSEL;

        /// <summary>Einschaltschwelle der Sommerlüftung [°C] (Stufe G2).</summary>
        public static double SommerlueftungSchwelle => GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE;

        /// <summary>
        /// <b>Der Luftwechsel, mit dem der VDI-Weg rechnet</b> [1/h] (Stufe G2;
        /// Softwarearchitektur 2.8, Rechenschritte A7) — die eine Stelle der Regel, die
        /// Eingangsbauer und Gebäudedialog teilen:
        /// <list type="number">
        /// <item>ist Infiltration oder Nutzerlüftung gesetzt, gilt ihre Summe, das fehlende
        /// Glied mit seiner Vorgabe (0,3 bzw. 0,4 1/h);</item>
        /// <item>sind beide leer, gilt die <c>Luftwechselrate</c> des Gebäudes, sofern sie größer
        /// null ist;</item>
        /// <item>sonst die Summe der beiden Vorgaben (0,7 1/h).</item>
        /// </list>
        /// Der Rückfall wird nie still überschrieben — <paramref name="herkunft"/> nennt ihn.
        /// </summary>
        public static double WirksamerLuftwechsel(double? luftwechselrate, double? infiltration,
                                                  double? nutzer, out Luftwechselherkunft herkunft)
        {
            if (infiltration.HasValue || nutzer.HasValue)
            {
                herkunft = Luftwechselherkunft.InfiltrationUndNutzer;
                return (infiltration ?? LuftwechselInfiltration) + (nutzer ?? LuftwechselNutzer);
            }
            if (luftwechselrate is double n && n > 0.0 && !double.IsInfinity(n))
            {
                herkunft = Luftwechselherkunft.Luftwechselrate;
                return n;
            }
            herkunft = Luftwechselherkunft.Vorgabe;
            return LuftwechselInfiltration + LuftwechselNutzer;
        }

        /// <summary>Dasselbe ohne Herkunft.</summary>
        public static double WirksamerLuftwechsel(double? luftwechselrate, double? infiltration, double? nutzer)
            => WirksamerLuftwechsel(luftwechselrate, infiltration, nutzer, out _);

        // ---- Kühlung (Stufe KU1; Kühlkonzept 3.2, 8.1) -----------------------------------

        /// <summary>
        /// Mindestabstand des Kühlsollwerts über dem höchsten Heizsollwert [K] — die harte
        /// Prüfregel des Lösers (Q18-Regel des Stundenwegs), die der Gebäudedialog vor dem
        /// Speichern mit derselben Zahl prüft.
        /// </summary>
        public static double KuehlsollwertAbstand => GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K;

        /// <summary>
        /// Kleinster Kühlsollwert, den der Gebäudedialog annimmt [°C] — eine Plausibilitätsgrenze
        /// der Eingabe (Kühlkonzept 8.1), keine Rechenregel.
        /// </summary>
        public const double KUEHLSOLLWERT_MIN = 15.0;

        /// <summary>Größter Kühlsollwert, den der Gebäudedialog annimmt [°C] (siehe <see cref="KUEHLSOLLWERT_MIN"/>).</summary>
        public const double KUEHLSOLLWERT_MAX = 35.0;

        /// <summary>
        /// <b>Der höchste Heizsollwert eines Gebäudes</b> [°C], den der Sollwertfahrplan des
        /// Stundenmodells erreichen kann (<c>GebaeudeModellEingang.Sollwertfahrplan</c>): Tag und
        /// Nacht immer, das Wochenende nur über der Wirksamkeitsschwelle des Fahrplans, die Ferien
        /// nur bei aktivem Ferienfahrplan. Gegen ihn prüft der Gebäudedialog den Kühlsollwert; der
        /// Löser prüft denselben Abstand (<see cref="KuehlsollwertAbstand"/>) am gerechneten
        /// Fahrplan und bricht bei einer Verletzung für dieses Gebäude ab.
        /// </summary>
        public static double HoechsterHeizsollwert(double sollTag, double sollNacht, double sollWochenende,
                                                   double sollFerien, bool ferienAktiv)
        {
            double max = Math.Max(sollTag, sollNacht);
            if (sollWochenende > GebaeudeFestwerte.WOCHENENDE_SOLLWERT_SCHWELLE)
                max = Math.Max(max, sollWochenende);
            if (ferienAktiv && sollFerien >= GebaeudeFestwerte.FERIEN_SOLLWERT_MIN)
                max = Math.Max(max, sollFerien);
            return max;
        }
    }

    /// <summary>
    /// <b>Die Vorgaben und Grenzen der Wärmeübergabe</b> (Anlagenkopplung AK1; Konzept 3.1, 4.4,
    /// 8.1, 9.1; H1, H10, H12, E25) — öffentlich, damit die Gruppe „Wärmeübergabe" des
    /// Gebäudedialogs jede Vorgabe als ZAHL zeigt und mit DERSELBEN Grenze prüft, mit der der
    /// Eingangsbauer des Kerns hart abbricht (<c>GebaeudeModellEingang.KopplungAufloesen</c>).
    /// Der Dialog schreibt NULL, nicht diese Werte; alle Zahlen sind Vorgaben von EPOS-Plan, keine
    /// Normwerte.
    /// </summary>
    public static class Waermeuebergabevorgaben
    {
        /// <summary>Die Übergabearten in Anzeigereihenfolge: ideal (= Kopplung aus), Radiator, Flächenheizung, Konvektor.</summary>
        public static readonly IReadOnlyList<string> Arten = new[]
        {
            DbWerte.UEBERGABE_IDEAL, DbWerte.UEBERGABE_RADIATOR, DbWerte.UEBERGABE_FLAECHE, DbWerte.UEBERGABE_KONVEKTOR
        };

        /// <summary>
        /// Die Kopplungsstufen des Projekts in Anzeigereihenfolge (<c>Tab_Einstellungen.Anlagenkopplung</c>):
        /// aus, AK1, AK2, AK3. Gebaut und damit wählbar sind nur aus und AK1 (<see cref="StufeGebaut"/>).
        /// </summary>
        public static readonly IReadOnlyList<string> Stufen = new[]
        {
            DbWerte.ANLAGENKOPPLUNG_AUS, DbWerte.ANLAGENKOPPLUNG_AK1, DbWerte.ANLAGENKOPPLUNG_AK2, DbWerte.ANLAGENKOPPLUNG_AK3
        };

        /// <summary>
        /// <b>Ist die Stufe gebaut?</b> Nur „aus" (auch NULL) und AK1 — ein Wert, dessen Rechenweg nicht
        /// gebaut ist, wird nicht angeboten (Konzept Anlagenkopplung 9.4, Kühlkonzept K7). Steht AK2 oder
        /// AK3 schon in der Datenbank, rechnet der Lauf AK1 und nennt es.
        /// </summary>
        public static bool StufeGebaut(string stufe)
            => string.IsNullOrEmpty(stufe) || stufe == DbWerte.ANLAGENKOPPLUNG_AUS || stufe == DbWerte.ANLAGENKOPPLUNG_AK1;

        /// <summary>Rechnet diese Art eine Übergabe (Radiator, Flächenheizung, Konvektor)? NULL und „ideal" nicht.</summary>
        public static bool ArtRechnet(string art) => Waermeuebergabe.ArtBekannt(art);

        /// <summary>Exponent der Art [–]; <c>null</c> für ideal oder unbekannt.</summary>
        public static double? Exponent(string art) => Zahl(Waermeuebergabe.VorgabeExponent(art));

        /// <summary>Auslegungsvorlauf der Art [°C]; <c>null</c> für ideal oder unbekannt.</summary>
        public static double? Vorlauf(string art) => Zahl(Waermeuebergabe.VorgabeVorlaufC(art));

        /// <summary>
        /// Der ANZEIGENAME einer Übergabeart (Drei-Schichten-Regel: der Steuerwert bleibt deutsch und
        /// eingefroren, angezeigt wird der Ressourcentext) — für Bedarfsdialog, Bericht und
        /// Variantenvergleich aus EINER Stelle; NULL heißt „ideal", ein unbekannter Wert steht, wie er ist.
        /// </summary>
        public static string Anzeigename(string art)
        {
            switch (art)
            {
                case null:
                case "":
                case DbWerte.UEBERGABE_IDEAL: return Text("GEBK_UEBERGABE_IDEAL", "ideal (keine Übergabe)");
                case DbWerte.UEBERGABE_RADIATOR: return Text("GEBK_UEBERGABE_RADIATOR", "Radiator");
                case DbWerte.UEBERGABE_FLAECHE: return Text("GEBK_UEBERGABE_FLAECHE", "Flächenheizung");
                case DbWerte.UEBERGABE_KONVEKTOR: return Text("GEBK_UEBERGABE_KONVEKTOR", "Konvektor");
                default: return art;
            }
        }

        /// <summary>Der Anzeigename einer Kopplungsstufe (<c>DbWerte.ANLAGENKOPPLUNG_*</c>); NULL heißt „aus".</summary>
        public static string Stufenname(string stufe)
        {
            switch (stufe)
            {
                case null:
                case "":
                case DbWerte.ANLAGENKOPPLUNG_AUS: return Text("SIMKONF_ANLAGENKOPPLUNG_AUS", "aus");
                case DbWerte.ANLAGENKOPPLUNG_AK1: return Text("SIMKONF_ANLAGENKOPPLUNG_AK1", "Heizkreis (AK1)");
                case DbWerte.ANLAGENKOPPLUNG_AK2: return Text("SIMKONF_ANLAGENKOPPLUNG_AK2", "Fahrplan (AK2)");
                case DbWerte.ANLAGENKOPPLUNG_AK3: return Text("SIMKONF_ANLAGENKOPPLUNG_AK3", "geschlossener Kreis (AK3)");
                default: return stufe;
            }
        }

        // ---- Die Kühlübergabe (E37; Anlagenkopplung 7.2, 8.1; A4) — dieselben Zahlen wie im
        //      Eingangsbauer (Kuehluebergabe, GebaeudeModellEingang.KuehlKopplungAufloesen) ----

        /// <summary>Die Kühlübergabearten in Anzeigereihenfolge: ideal (= Kälteseite nicht gekoppelt), Kühldecke, Flächenkühlung, Gebläsekonvektor.</summary>
        public static readonly IReadOnlyList<string> KuehlArten = new[]
        {
            DbWerte.KUEHLUEBERGABE_IDEAL, DbWerte.KUEHLUEBERGABE_KUEHLDECKE,
            DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG, DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR
        };

        /// <summary>Rechnet diese Kühlübergabeart (Kühldecke, Flächenkühlung, Gebläsekonvektor)? NULL und „ideal" nicht.</summary>
        public static bool KuehlArtRechnet(string art) => Kuehluebergabe.ArtBekannt(art);

        /// <summary>Exponent der Kühlübergabeart [–]; <c>null</c> für ideal oder unbekannt.</summary>
        public static double? KuehlExponent(string art) => Zahl(Kuehluebergabe.VorgabeExponent(art));

        /// <summary>Auslegungsvorlauf der Kühlübergabeart [°C]; <c>null</c> für ideal oder unbekannt.</summary>
        public static double? KuehlVorlauf(string art) => Zahl(Kuehluebergabe.VorgabeVorlaufC(art));

        /// <summary>Auslegungsrücklauf der Kühlübergabeart [°C]; <c>null</c> für ideal oder unbekannt.</summary>
        public static double? KuehlRuecklauf(string art) => Zahl(Kuehluebergabe.VorgabeRuecklaufC(art));

        /// <summary>Strahlungsanteil der Kühlübergabeart [–] — eine Vorgabe ohne eigene Spalte; <c>null</c> für ideal.</summary>
        public static double? KuehlStrahlungsanteil(string art) => Zahl(Kuehluebergabe.VorgabeStrahlungsanteil(art));

        /// <summary>
        /// Vorlaufgrenze der Kühlübergabeart [°C] — eine Vorgabe statt einer Taupunktrechnung (7.2);
        /// <c>null</c> heißt „keine Grenze" (Gebläsekonvektor) bzw. ideal oder unbekannt.
        /// </summary>
        public static double? KuehlVorlaufgrenze(string art) => Zahl(Kuehluebergabe.VorgabeVorlaufgrenzeC(art));

        /// <summary>
        /// Der ANZEIGENAME einer Kühlübergabeart (Drei-Schichten-Regel wie <see cref="Anzeigename"/>) —
        /// für Gebäudedialog, Bedarfsdialog, Bericht und Variantenvergleich aus EINER Stelle; NULL heißt
        /// „ideal", ein unbekannter Wert steht, wie er ist.
        /// </summary>
        public static string KuehlAnzeigename(string art)
        {
            switch (art)
            {
                case null:
                case "":
                case DbWerte.KUEHLUEBERGABE_IDEAL: return Text("GEBK_KUEHLUEBERGABE_IDEAL", "ideal (keine Kühlübergabe)");
                case DbWerte.KUEHLUEBERGABE_KUEHLDECKE: return Text("GEBK_KUEHLUEBERGABE_KUEHLDECKE", "Kühldecke");
                case DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG: return Text("GEBK_KUEHLUEBERGABE_FLAECHENKUEHLUNG", "Flächenkühlung");
                case DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR: return Text("GEBK_KUEHLUEBERGABE_GEBLAESEKONVEKTOR", "Gebläsekonvektor");
                default: return art;
            }
        }

        /// <summary>Kleinster Auslegungsvorlauf und kleinste Vorlaufgrenze der Kühlübergabe [°C].</summary>
        public const double KUEHL_VORLAUF_MIN = GebaeudeFestwerte.KUEHL_VORLAUF_MIN;
        /// <summary>Größter Auslegungsvorlauf und größte Vorlaufgrenze der Kühlübergabe [°C].</summary>
        public const double KUEHL_VORLAUF_MAX = GebaeudeFestwerte.KUEHL_VORLAUF_MAX;
        /// <summary>Kleinste Raumtemperatur im Auslegungspunkt der Kühlübergabe [°C].</summary>
        public const double KUEHL_RAUM_MIN = GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MIN;
        /// <summary>Größte Raumtemperatur im Auslegungspunkt der Kühlübergabe [°C].</summary>
        public const double KUEHL_RAUM_MAX = GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MAX;

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>Auslegungsrücklauf der Art [°C]; <c>null</c> für ideal oder unbekannt.</summary>
        public static double? Ruecklauf(string art) => Zahl(Waermeuebergabe.VorgabeRuecklaufC(art));

        /// <summary>Strahlungsanteil der Art [–] (H12: gilt, wenn <c>Heizung_Strahlungsanteil</c> leer ist); <c>null</c> für ideal.</summary>
        public static double? Strahlungsanteil(string art) => Zahl(Waermeuebergabe.VorgabeStrahlungsanteil(art));

        /// <summary>Proportionalband des Raumreglers bei leerem Feld [K] (H1, E25).</summary>
        public static double Proportionalband => GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K;

        /// <summary>Die Schnellwahl des Proportionalbands [K] (E25); jeder andere Wert zeigt „frei".</summary>
        public static readonly IReadOnlyList<double> ProportionalbandSchnellwahl = new[] { 0.5, 1.0, 2.0 };

        /// <summary>Niveau der Heizkurve bei leerem Feld [K].</summary>
        public static double HeizkurveNiveau => GebaeudeFestwerte.VORGABE_HEIZKURVE_NIVEAU_K;

        /// <summary>Steilheit der Heizkurve bei leerem Feld [–] — die Kurve durch den Auslegungspunkt.</summary>
        public static double HeizkurveSteilheit => GebaeudeFestwerte.VORGABE_HEIZKURVE_STEILHEIT;

        // ---- Die Grenzen der Prüfregeln (9.1) — dieselben Zahlen wie im Eingangsbauer ----

        /// <summary>Kleinster Exponent [–].</summary>
        public const double EXPONENT_MIN = GebaeudeFestwerte.UEBERGABE_EXPONENT_MIN;
        /// <summary>Größter Exponent [–].</summary>
        public const double EXPONENT_MAX = GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX;
        /// <summary>Kleinster Auslegungsvorlauf [°C].</summary>
        public const double VORLAUF_MIN = GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MIN;
        /// <summary>Größter Auslegungsvorlauf [°C].</summary>
        public const double VORLAUF_MAX = GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MAX;
        /// <summary>Kleinste Auslegungs-Raumtemperatur [°C].</summary>
        public const double RAUM_MIN = GebaeudeFestwerte.AUSLEGUNG_RAUM_MIN;
        /// <summary>Größte Auslegungs-Raumtemperatur [°C].</summary>
        public const double RAUM_MAX = GebaeudeFestwerte.AUSLEGUNG_RAUM_MAX;
        /// <summary>Kleinste eingegebene Auslegungs-Außentemperatur [°C].</summary>
        public const double AUSSEN_MIN = GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MIN;
        /// <summary>Größte eingegebene Auslegungs-Außentemperatur [°C].</summary>
        public const double AUSSEN_MAX = GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MAX;
        /// <summary>Kleinstes Niveau der Heizkurve [K].</summary>
        public const double NIVEAU_MIN = GebaeudeFestwerte.HEIZKURVE_NIVEAU_MIN;
        /// <summary>Größtes Niveau der Heizkurve [K].</summary>
        public const double NIVEAU_MAX = GebaeudeFestwerte.HEIZKURVE_NIVEAU_MAX;
        /// <summary>Kleinste Steilheit der Heizkurve [–].</summary>
        public const double STEILHEIT_MIN = GebaeudeFestwerte.HEIZKURVE_STEILHEIT_MIN;
        /// <summary>Größte Steilheit der Heizkurve [–].</summary>
        public const double STEILHEIT_MAX = GebaeudeFestwerte.HEIZKURVE_STEILHEIT_MAX;
        /// <summary>Kleinstes Proportionalband [K]; 0 = ideale Regelung mit Grenze.</summary>
        public const double BAND_MIN = GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MIN_K;
        /// <summary>Größtes Proportionalband [K].</summary>
        public const double BAND_MAX = GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MAX_K;
        /// <summary>Kleinster Wert des Sollwert-Zeitprogramms [°C].</summary>
        public const double SOLLWERT_MIN = GebaeudeFestwerte.SOLLWERTPROFIL_MIN_C;
        /// <summary>Größter Wert des Sollwert-Zeitprogramms [°C].</summary>
        public const double SOLLWERT_MAX = GebaeudeFestwerte.SOLLWERTPROFIL_MAX_C;

        private static double? Zahl(double w) => double.IsNaN(w) ? (double?)null : w;

        /// <summary>
        /// <b>Die vier Bestandssollwerte als Woche</b> (Konzept Anlagenkopplung 4.3, 9.2) — 168
        /// Werte, Montag 00:00 zuerst, nach DERSELBEN Regel wie der Sollwertfahrplan des
        /// Stundenmodells ohne Zeitprogramm: Samstag und Sonntag den ganzen Tag der
        /// Wochenendwert, sofern er über der Wirksamkeitsschwelle liegt, sonst wie die Werktage;
        /// an Werktagen in der Nutzungszeit der Tagwert, sonst der Nachtwert. Die Nutzungszeit ist die
        /// des Gebäudes (<paramref name="nachtBeginn"/>/<paramref name="nachtEnde"/>, Entscheid E43;
        /// beide leer = 22 bis 6 Uhr). Die Ferien wirken im Lauf darüber und stehen hier nicht. Das
        /// Wochenraster zeigt diese Woche, solange kein Zeitprogramm gepflegt ist — wer daraus eins
        /// anlegt, bekommt genau das.
        ///
        /// <para>Ein widersprüchliches Paar (<see cref="Nachtzeit.Pruefen"/>) zeigt die Woche der
        /// Vorgabe: Die Woche ist nur der Vorschlag des Rasters; den Fehler benennt die Prüfung des
        /// Editors, und der Lauf bricht an ihm ab (<c>GebaeudeModellFehler.NachtzeitUngueltig</c>).</para>
        /// </summary>
        public static double[] Bestandswoche(double sollTag, double sollNacht, double sollWochenende,
                                             int? nachtBeginn = null, int? nachtEnde = null)
        {
            Nachtzeit nacht = Nachtzeit.Pruefen(nachtBeginn, nachtEnde) == NachtzeitBefund.Gueltig
                ? Nachtzeit.Aus(nachtBeginn, nachtEnde)
                : Nachtzeit.Vorgabe;
            bool weWirksam = sollWochenende > GebaeudeFestwerte.WOCHENENDE_SOLLWERT_SCHWELLE;
            var woche = new double[AnlagenkopplungSchema.WOCHENWERTE];
            for (int i = 0; i < woche.Length; i++)
            {
                int tag = i / 24;   // 0 = Montag … 6 = Sonntag
                if (weWirksam && tag >= 5) woche[i] = sollWochenende;
                else woche[i] = nacht.Nutzungszeit(i) ? sollTag : sollNacht;
            }
            return woche;
        }
    }

    /// <summary>Woher der Luftwechsel des VDI-Wegs kommt (<see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?, out Luftwechselherkunft)"/>).</summary>
    public enum Luftwechselherkunft
    {
        /// <summary>Summe aus Infiltration und Nutzerlüftung (Stufe G2).</summary>
        InfiltrationUndNutzer,

        /// <summary>Beide leer: die Luftwechselrate des Gebäudes.</summary>
        Luftwechselrate,

        /// <summary>Alles leer bzw. null: die Summe der beiden Vorgaben.</summary>
        Vorgabe,
    }
}
