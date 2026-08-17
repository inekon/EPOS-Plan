using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpeicherEngine
{
    /// <summary>
    /// Deklarierte Einheit der eingelesenen Rohwerte (Fachkonzept 3.2, AP5).
    /// </summary>
    public enum GanglinienEinheit
    {
        /// <summary>Leistung [kW], konstant ueber das Intervall. Zielkonvention der Kette.</summary>
        Kilowatt = 0,

        /// <summary>Arbeit [kWh] je Intervall. Wird ueber die Intervalldauer in kW umgerechnet.</summary>
        KilowattstundeJeIntervall = 1
    }

    /// <summary>
    /// Zeitraster einer Ganglinie. Der Zahlenwert ist zugleich das DB-Feld
    /// <c>Tab_Stromganglinie.Zeitinterval</c> (Intervalle je Stunde).
    /// </summary>
    public enum GanglinienRaster
    {
        /// <summary>Nicht bestimmbar.</summary>
        Unbekannt = 0,

        /// <summary>Stundenwerte, 8.760 (bzw. 8.784 im Schaltjahr).</summary>
        Stunde = 1,

        /// <summary>Viertelstundenwerte, 35.040 (bzw. 35.136 im Schaltjahr).</summary>
        Viertelstunde = 4,

        /// <summary>Minutenwerte, 525.600 (bzw. 527.040 im Schaltjahr).</summary>
        Minute = 60
    }

    /// <summary>
    /// Zeitstempelkonvention einer Reihe: Bezeichnet der Zeitstempel den Anfang
    /// oder das Ende des Intervalls?
    /// </summary>
    public enum IntervallKonvention
    {
        /// <summary>Aus der Reihe erkennen (Vorgabe).</summary>
        Automatisch = 0,

        /// <summary>Zeitstempel = Intervallanfang (Hauskonvention).</summary>
        Anfang = 1,

        /// <summary>Zeitstempel = Intervallende; wird intern um ein Intervall zurueckgesetzt.</summary>
        Ende = 2
    }

    /// <summary>Schwere einer Protokollmeldung der Ganglinienpruefung.</summary>
    public enum PruefStufe
    {
        /// <summary>Reine Nachricht ueber einen erkannten oder vorgenommenen Schritt.</summary>
        Info = 0,

        /// <summary>Auffaelligkeit; der Import darf trotzdem laufen.</summary>
        Warnung = 1,

        /// <summary>Abbruchgrund; die Reihe darf nicht gespeichert werden.</summary>
        Fehler = 2
    }

    /// <summary>
    /// Eine Zeile des Validierungsprotokolls. Die Engine ist sprachneutral: sie
    /// liefert einen <see cref="Schluessel"/> (zugleich der Name des
    /// Ressourcenschluessels <c>IMPORT_PROT_*</c> im Hauptprojekt) und die
    /// eingesetzten <see cref="Werte"/>; den Text holt erst die Oberflaeche aus
    /// <c>MyResource.Resource</c> (Drei-Schichten-Regel der Projekt-CLAUDE.md).
    /// </summary>
    public sealed class PruefMeldung
    {
        /// <summary>Erzeugt eine Meldung.</summary>
        /// <param name="stufe">Schwere.</param>
        /// <param name="schluessel">Sprachneutraler Schluessel, z. B. <c>IMPORT_PROT_LUECKE</c>.</param>
        /// <param name="werte">Platzhalterwerte, bereits invariant formatiert.</param>
        public PruefMeldung(PruefStufe stufe, string schluessel, params string[] werte)
        {
            Stufe = stufe;
            Schluessel = schluessel ?? string.Empty;
            Werte = werte ?? Array.Empty<string>();
        }

        /// <summary>Schwere der Meldung.</summary>
        public PruefStufe Stufe { get; }

        /// <summary>Sprachneutraler Schluessel; Name des Ressourcenschluessels in der Oberflaeche.</summary>
        public string Schluessel { get; }

        /// <summary>
        /// Platzhalterwerte in der Reihenfolge <c>{0}</c>, <c>{1}</c>, ... Bereits
        /// mit <see cref="CultureInfo.InvariantCulture"/> formatiert, damit der
        /// Kulturtest (de-DE / en-US) identische Protokolle liefert.
        /// </summary>
        public string[] Werte { get; }

        /// <summary>
        /// Sprachunabhaengige Kurzfassung fuer Protokolldateien und Testvergleiche:
        /// <c>SCHLUESSEL: wert1; wert2</c>.
        /// </summary>
        public override string ToString()
        {
            return Werte.Length == 0
                ? Schluessel
                : Schluessel + ": " + string.Join("; ", Werte);
        }
    }

    /// <summary>
    /// Eingang der Ganglinienpruefung: bereits geparste Rohwerte samt Metadaten.
    /// Das Einlesen der Datei (Trennzeichen, Dezimaltrenner, Excel) gehoert
    /// bewusst nicht hierher, sondern in die Leseschicht des Hauptprojekts
    /// (<c>Allgemein\Import\GanglinienDatei.cs</c>).
    /// </summary>
    public sealed class GanglinienPruefEingang
    {
        /// <summary>Rohwerte in Dateireihenfolge. Pflichtangabe.</summary>
        public double[] Rohwerte { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Zeitstempel je Rohwert, oder <c>null</c>, wenn die Datei keine Zeitspalte hat.
        /// Gleiche Laenge wie <see cref="Rohwerte"/>.
        /// </summary>
        public DateTime[]? Zeitstempel { get; set; }

        /// <summary>Deklarierte Einheit der Rohwerte. Vorgabe: kW.</summary>
        public GanglinienEinheit Einheit { get; set; } = GanglinienEinheit.Kilowatt;

        /// <summary>
        /// Vom Anwender deklariertes Raster, oder <see cref="GanglinienRaster.Unbekannt"/>
        /// fuer die automatische Erkennung. Weicht die Deklaration vom erkannten
        /// Raster ab, gewinnt die Erkennung (mit Warnung).
        /// </summary>
        public GanglinienRaster DeklariertesRaster { get; set; } = GanglinienRaster.Unbekannt;

        /// <summary>Zeitstempelkonvention; Vorgabe automatisch.</summary>
        public IntervallKonvention Konvention { get; set; } = IntervallKonvention.Automatisch;

        /// <summary>
        /// Ab welchem Vielfachen des Medians ein Wert als Ausreisser gemeldet wird.
        /// Vorgabe 20; Werte &lt;= 0 schalten die Pruefung ab.
        /// </summary>
        public double AusreisserFaktor { get; set; } = 20.0;
    }

    /// <summary>
    /// Ergebnis der Ganglinienpruefung: validierte Reihe im festen Jahresraster
    /// plus Protokoll.
    /// </summary>
    public sealed class GanglinienPruefErgebnis
    {
        internal GanglinienPruefErgebnis(
            double[] werte,
            GanglinienRaster zielraster,
            IReadOnlyList<PruefMeldung> protokoll,
            bool schaltjahrNormalisiert,
            bool gemittelt,
            bool sommerzeitBehandelt)
        {
            Werte = werte;
            Zielraster = zielraster;
            Protokoll = protokoll;
            SchaltjahrNormalisiert = schaltjahrNormalisiert;
            Gemittelt = gemittelt;
            SommerzeitBehandelt = sommerzeitBehandelt;
        }

        /// <summary>
        /// Validierte Reihe in <b>kW</b>: 8.760 Stunden- oder 35.040
        /// Viertelstundenwerte. Bei <see cref="Erfolgreich"/> == <c>false</c> leer.
        /// </summary>
        public double[] Werte { get; }

        /// <summary>Raster der <see cref="Werte"/>: <see cref="GanglinienRaster.Stunde"/> oder <see cref="GanglinienRaster.Viertelstunde"/>.</summary>
        public GanglinienRaster Zielraster { get; }

        /// <summary>
        /// Wert fuer das DB-Feld <c>Zeitinterval</c>: 1 bei Stunden-, 4 bei
        /// Viertelstundenreihen. Minutenreihen landen nach der Mittelung als 4.
        /// </summary>
        public int Zeitinterval => (int)Zielraster;

        /// <summary>Vollstaendiges Protokoll in Entstehungsreihenfolge.</summary>
        public IReadOnlyList<PruefMeldung> Protokoll { get; }

        /// <summary>Kein Eintrag der Stufe <see cref="PruefStufe.Fehler"/> im Protokoll.</summary>
        public bool Erfolgreich
        {
            get
            {
                for (int i = 0; i < Protokoll.Count; i++)
                    if (Protokoll[i].Stufe == PruefStufe.Fehler) return false;
                return true;
            }
        }

        /// <summary>Es gibt mindestens eine Warnung.</summary>
        public bool HatWarnungen
        {
            get
            {
                for (int i = 0; i < Protokoll.Count; i++)
                    if (Protokoll[i].Stufe == PruefStufe.Warnung) return true;
                return false;
            }
        }

        /// <summary>Der 29.02. wurde ausgelassen (8.784/35.136 -&gt; 8.760/35.040).</summary>
        public bool SchaltjahrNormalisiert { get; }

        /// <summary>Minutenwerte wurden auf 15 Minuten gemittelt.</summary>
        public bool Gemittelt { get; }

        /// <summary>Eine Sommerzeitumstellung wurde behandelt (Luecke gefuellt oder Dublette gemittelt).</summary>
        public bool SommerzeitBehandelt { get; }

        /// <summary>
        /// <c>true</c>, wenn der Anwender das Protokoll bestaetigen soll, weil an der
        /// Reihe etwas veraendert wurde oder Auffaelligkeiten anliegen.
        /// </summary>
        public bool BestaetigungNoetig =>
            HatWarnungen || SchaltjahrNormalisiert || Gemittelt || SommerzeitBehandelt;
    }

    /// <summary>
    /// Regelwerk des erweiterten Lastgangimports (AP5, Fachkonzept 3.2) - die
    /// testbare Pruef- und Normalisierungslogik, streng getrennt vom Datei-I/O.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Aufgabe.</b> Aus geparsten Rohwerten (plus optionalen Zeitstempeln) wird
    /// eine Reihe im festen Jahresraster des Hauses: 8.760 Stunden- oder 35.040
    /// Viertelstundenwerte, Einheit kW. Genau dieses Raster verlangen der
    /// Rechenkern (<c>BhkwPlan</c>, feste Feldgroessen) und die Engine
    /// (<see cref="RasterAdapter"/>); die Ablage in
    /// <c>Tab_StromganglinieDaten(_STAMM)</c> kennt keinen Zeitstempel, die
    /// Reihenfolge ist die Zeitachse.
    /// </para>
    /// <para><b>Verarbeitungsreihenfolge</b> (jeder Schritt protokolliert):</para>
    /// <list type="number">
    ///   <item><description>Grundpruefung: leere Reihe, NaN/Unendlich, Laenge der Zeitstempelspalte.</description></item>
    ///   <item><description>Rastererkennung aus dem Zeitstempelabstand und aus der Wertanzahl; Widerspruch ist ein Fehler.</description></item>
    ///   <item><description>Zeitachse normalisieren: Monotonie, Dubletten, Luecken - mit den beiden Sommerzeitausnahmen.</description></item>
    ///   <item><description>Einheit: kWh je Intervall -&gt; kW (Faktor = Intervalle je Stunde).</description></item>
    ///   <item><description>Minutenwerte -&gt; arithmetisches Mittel ueber je 15 Werte (Entscheid Fachkonzept 3.2 / offener Punkt 7).</description></item>
    ///   <item><description>Schaltjahr: 8.784/35.136 -&gt; 8.760/35.040 durch Auslassen des 29.02.</description></item>
    ///   <item><description>Plausibilitaet: negative Werte, Nullserien, Ausreisser.</description></item>
    /// </list>
    /// <para>
    /// <b>Sommerzeit.</b> Die Reihe wird als Ortszeitreihe gelesen. Nach EU-Regel
    /// (seit 1996) fehlt am letzten Maerzsonntag die Ortsstunde 02:00 und der
    /// letzte Oktobersonntag hat sie doppelt. Beides ist <i>kein</i> Fehler: die
    /// Dublette wird arithmetisch gemittelt, die Luecke mit dem Wert des
    /// Vorintervalls aufgefuellt (Wertwiederholung wie
    /// <c>Stundenwerte_zu_viertelstunden</c>, keine Interpolation). Ergebnis ist
    /// eine glatte Ortszeitachse ohne Zeitsprung, also genau die 8.760/35.040
    /// Faecher der Ablage. Jede andere Luecke oder Dublette bleibt ein Fehler.
    /// Absichtlich <b>ohne</b> <c>TimeZoneInfo</c>: die Regel ist fest verdrahtet,
    /// damit das Ergebnis nicht von der Zeitzonentabelle des Rechners abhaengt.
    /// </para>
    /// <para>
    /// <b>Schaltjahr.</b> Die Bestandskette rechnet im festen 8.760/35.040-Raster
    /// (Wurzel-CLAUDE.md), <see cref="RasterAdapter"/> lehnt 35.136 ab. Der
    /// Schaltjahreseingang wird deshalb hier normalisiert statt durchgereicht: der
    /// 29.02. entfaellt (mit Zeitstempeln datumsgenau, ohne Zeitstempel ueber die
    /// Position - Tag 60 des Jahres). Der Vorgang steht als Info im Protokoll und
    /// erzwingt in der Oberflaeche einen Bestaetigungsschritt. Die verlustfreie
    /// Originalablage ist die Kopie der Quelldatei im Anwenderordner
    /// <c>...\Strom</c>, die der Importdialog unveraendert anlegt.
    /// </para>
    /// <para>
    /// <b>Kultur.</b> Die Klasse formatiert saemtliche Protokollwerte mit
    /// <see cref="CultureInfo.InvariantCulture"/> und liest selbst nichts aus Text -
    /// der Kulturtest (de-DE / en-US) liefert daher bitgleiche Reihen und
    /// zeichengleiche Protokolle.
    /// </para>
    /// </remarks>
    public static class GanglinienPruefung
    {
        /// <summary>Stundenwerte eines Normaljahres.</summary>
        public const int StundenJahr = 8760;

        /// <summary>Stundenwerte eines Schaltjahres.</summary>
        public const int StundenSchaltjahr = 8784;

        /// <summary>Viertelstundenwerte eines Normaljahres.</summary>
        public const int ViertelstundenJahr = 35040;

        /// <summary>Viertelstundenwerte eines Schaltjahres.</summary>
        public const int ViertelstundenSchaltjahr = 35136;

        /// <summary>Minutenwerte eines Normaljahres.</summary>
        public const int MinutenJahr = 525600;

        /// <summary>Minutenwerte eines Schaltjahres.</summary>
        public const int MinutenSchaltjahr = 527040;

        /// <summary>Zusammenfassungsbreite der Minutenmittelung: 15 Minuten je Viertelstunde.</summary>
        public const int MinutenJeViertelstunde = 15;

        /// <summary>Tage vor dem 29.02. in einem Schaltjahr (Jan 1 ... Feb 28).</summary>
        private const int TageVorSchalttag = 59;

        // ------------------------------------------------------------------
        // Protokollschluessel. Gleichlautend als IMPORT_PROT_* in MyResource.
        // ------------------------------------------------------------------

        /// <summary>Kein einziger Wert im Eingang.</summary>
        public const string SchluesselKeineWerte = "IMPORT_PROT_KEINE_WERTE";

        /// <summary>Nicht darstellbarer Zahlenwert (NaN / Unendlich). {0} = Zeilennummer.</summary>
        public const string SchluesselUngueltigerWert = "IMPORT_PROT_UNGUELTIGER_WERT";

        /// <summary>Zeitstempelspalte hat eine andere Laenge als die Wertspalte. {0} = Werte, {1} = Zeitstempel.</summary>
        public const string SchluesselZeitstempelAnzahl = "IMPORT_PROT_ZEITSTEMPEL_ANZAHL";

        /// <summary>Raster erkannt. {0} = Anzahl Werte, {1} = Intervalle je Stunde.</summary>
        public const string SchluesselRasterErkannt = "IMPORT_PROT_RASTER_ERKANNT";

        /// <summary>Anzahl passt zu keinem bekannten Jahresraster. {0} = Anzahl.</summary>
        public const string SchluesselRasterUnbekannt = "IMPORT_PROT_RASTER_UNBEKANNT";

        /// <summary>Deklariertes Raster weicht vom erkannten ab. {0} = deklariert, {1} = erkannt.</summary>
        public const string SchluesselRasterAbweichend = "IMPORT_PROT_RASTER_ABWEICHEND";

        /// <summary>Raster aus dem Zeitstempelabstand. {0} = Minuten, {1} = Intervalle je Stunde.</summary>
        public const string SchluesselRasterAusZeit = "IMPORT_PROT_RASTER_AUS_ZEIT";

        /// <summary>Zeitstempelabstand und Wertanzahl widersprechen sich. {0} = aus Zeit, {1} = aus Anzahl.</summary>
        public const string SchluesselRasterWiderspruch = "IMPORT_PROT_RASTER_WIDERSPRUCH";

        /// <summary>Zeitstempelabstand nicht bestimmbar oder unbekanntes Raster. {0} = Minuten.</summary>
        public const string SchluesselZeitschrittUnbekannt = "IMPORT_PROT_ZEITSCHRITT_UNBEKANNT";

        /// <summary>Zeitstempel = Intervallanfang (Hauskonvention).</summary>
        public const string SchluesselKonventionAnfang = "IMPORT_PROT_KONVENTION_ANFANG";

        /// <summary>Zeitstempel = Intervallende; um ein Intervall zurueckgesetzt. {0} = erster Zeitstempel.</summary>
        public const string SchluesselKonventionEnde = "IMPORT_PROT_KONVENTION_ENDE";

        /// <summary>Die Reihe beginnt nicht am 01.01. um 00:00. {0} = erster Zeitstempel.</summary>
        public const string SchluesselJahresanfang = "IMPORT_PROT_JAHRESANFANG";

        /// <summary>Zeitstempel laufen rueckwaerts. {0} = Zeitpunkt, {1} = Zeilennummer.</summary>
        public const string SchluesselNichtMonoton = "IMPORT_PROT_NICHT_MONOTON";

        /// <summary>Doppelter Zeitstempel ausserhalb der Zeitumstellung. {0} = Zeitpunkt, {1} = Anzahl.</summary>
        public const string SchluesselDublette = "IMPORT_PROT_DUBLETTE";

        /// <summary>Fehlende Intervalle ausserhalb der Zeitumstellung. {0} = Zeitpunkt, {1} = Anzahl.</summary>
        public const string SchluesselLuecke = "IMPORT_PROT_LUECKE";

        /// <summary>Herbstumstellung: doppelte Ortsstunde gemittelt. {0} = Zeitpunkt, {1} = Anzahl Werte.</summary>
        public const string SchluesselSommerzeitDublette = "IMPORT_PROT_SOMMERZEIT_DUBLETTE";

        /// <summary>Fruehjahrsumstellung: fehlende Ortsstunde aufgefuellt. {0} = Zeitpunkt, {1} = Anzahl Intervalle.</summary>
        public const string SchluesselSommerzeitLuecke = "IMPORT_PROT_SOMMERZEIT_LUECKE";

        /// <summary>Einheit umgerechnet kWh je Intervall -&gt; kW. {0} = Faktor.</summary>
        public const string SchluesselEinheitUmgerechnet = "IMPORT_PROT_EINHEIT_UMGERECHNET";

        /// <summary>Minutenwerte auf 15 Minuten gemittelt. {0} = Anzahl vorher, {1} = Anzahl nachher.</summary>
        public const string SchluesselMinutenGemittelt = "IMPORT_PROT_MINUTEN_GEMITTELT";

        /// <summary>Minutenanzahl nicht durch 15 teilbar. {0} = Anzahl.</summary>
        public const string SchluesselMinutenRest = "IMPORT_PROT_MINUTEN_REST";

        /// <summary>Schaltjahr normalisiert. {0} = Anzahl vorher, {1} = Anzahl nachher, {2} = ausgelassene Werte.</summary>
        public const string SchluesselSchaltjahr = "IMPORT_PROT_SCHALTJAHR";

        /// <summary>Schaltjahr ohne Zeitstempel: 29.02. ueber die Position ausgelassen. {0} = erste, {1} = letzte Zeilennummer.</summary>
        public const string SchluesselSchaltjahrPosition = "IMPORT_PROT_SCHALTJAHR_POSITION";

        /// <summary>Endgueltige Laenge passt zu keinem Jahresraster. {0} = Anzahl, {1} = erwartet.</summary>
        public const string SchluesselLaengeFalsch = "IMPORT_PROT_LAENGE_FALSCH";

        /// <summary>Negative Werte in der Reihe. {0} = Anzahl, {1} = kleinster Wert.</summary>
        public const string SchluesselNegativeWerte = "IMPORT_PROT_NEGATIVE_WERTE";

        /// <summary>Laengere Nullserie. {0} = Zeilennummer, {1} = Laenge in Intervallen.</summary>
        public const string SchluesselNullserie = "IMPORT_PROT_NULLSERIE";

        /// <summary>Die ganze Reihe ist null.</summary>
        public const string SchluesselAlleNull = "IMPORT_PROT_ALLE_NULL";

        /// <summary>Ausreisser oberhalb des Medianvielfachen. {0} = Anzahl, {1} = groesster Wert, {2} = Median, {3} = Faktor.</summary>
        public const string SchluesselAusreisser = "IMPORT_PROT_AUSREISSER";

        /// <summary>Ergebnisuebersicht. {0} = Anzahl Werte, {1} = Intervalle je Stunde, {2} = Jahresarbeit in kWh.</summary>
        public const string SchluesselErgebnis = "IMPORT_PROT_ERGEBNIS";

        /// <summary>
        /// Prueft und normalisiert eine eingelesene Ganglinie.
        /// </summary>
        /// <param name="eingang">Rohwerte samt Metadaten.</param>
        /// <returns>
        /// Immer ein Ergebnisobjekt - Fehler stehen im Protokoll, es fliegt keine
        /// Ausnahme. Bei <see cref="GanglinienPruefErgebnis.Erfolgreich"/> ==
        /// <c>false</c> ist <see cref="GanglinienPruefErgebnis.Werte"/> leer.
        /// </returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="eingang"/> <c>null</c> ist.</exception>
        public static GanglinienPruefErgebnis Pruefe(GanglinienPruefEingang eingang)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));

            var protokoll = new List<PruefMeldung>();
            double[] werte = eingang.Rohwerte ?? Array.Empty<double>();
            DateTime[]? zeit = eingang.Zeitstempel;

            // --- 1. Grundpruefung ------------------------------------------
            if (werte.Length == 0)
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselKeineWerte));
                return Abbruch(protokoll);
            }

            for (int i = 0; i < werte.Length; i++)
            {
                if (double.IsNaN(werte[i]) || double.IsInfinity(werte[i]))
                {
                    protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselUngueltigerWert, Zahl(i + 1)));
                    return Abbruch(protokoll);
                }
            }

            if (zeit != null && zeit.Length != werte.Length)
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselZeitstempelAnzahl,
                    Zahl(werte.Length), Zahl(zeit.Length)));
                return Abbruch(protokoll);
            }

            // --- 2. Raster bestimmen ---------------------------------------
            GanglinienRaster rasterAusAnzahl = RasterAusAnzahl(werte.Length);
            GanglinienRaster raster = rasterAusAnzahl;
            bool sommerzeitBehandelt = false;

            if (zeit != null && zeit.Length > 1)
            {
                int schrittMinuten = SchrittMinuten(zeit);
                GanglinienRaster rasterAusZeit = RasterAusMinuten(schrittMinuten);

                if (rasterAusZeit == GanglinienRaster.Unbekannt)
                {
                    protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselZeitschrittUnbekannt,
                        Zahl(schrittMinuten)));
                    return Abbruch(protokoll);
                }

                protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselRasterAusZeit,
                    Zahl(schrittMinuten), Zahl((int)rasterAusZeit)));

                if (rasterAusAnzahl != GanglinienRaster.Unbekannt && rasterAusAnzahl != rasterAusZeit)
                {
                    protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselRasterWiderspruch,
                        Zahl((int)rasterAusZeit), Zahl((int)rasterAusAnzahl)));
                    return Abbruch(protokoll);
                }

                raster = rasterAusZeit;

                // Intervallkonvention aufloesen, dann Zeitachse normalisieren.
                zeit = KonventionAnwenden(zeit, schrittMinuten, eingang.Konvention, protokoll);

                if (!ZeitachseNormalisieren(ref werte, ref zeit, schrittMinuten, protokoll, ref sommerzeitBehandelt))
                    return Abbruch(protokoll);
            }
            else if (raster == GanglinienRaster.Unbekannt)
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselRasterUnbekannt, Zahl(werte.Length)));
                return Abbruch(protokoll);
            }

            // Nach der Zeitachsennormalisierung muss die Anzahl zum Jahresraster passen.
            if (RasterAusAnzahl(werte.Length) != raster)
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselLaengeFalsch,
                    Zahl(werte.Length), Zahl(NormaljahrAnzahl(raster))));
                return Abbruch(protokoll);
            }

            protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselRasterErkannt,
                Zahl(werte.Length), Zahl((int)raster)));

            if (eingang.DeklariertesRaster != GanglinienRaster.Unbekannt &&
                eingang.DeklariertesRaster != raster)
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Warnung, SchluesselRasterAbweichend,
                    Zahl((int)eingang.DeklariertesRaster), Zahl((int)raster)));
            }

            // --- 3. Einheit ------------------------------------------------
            if (eingang.Einheit == GanglinienEinheit.KilowattstundeJeIntervall)
            {
                int faktor = (int)raster;   // Intervalle je Stunde
                if (faktor != 1)
                {
                    double[] umgerechnet = new double[werte.Length];
                    for (int i = 0; i < werte.Length; i++) umgerechnet[i] = werte[i] * faktor;
                    werte = umgerechnet;
                }
                protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselEinheitUmgerechnet, Zahl(faktor)));
            }

            // --- 4. Minutenwerte auf 15 Minuten mitteln --------------------
            bool gemittelt = false;
            if (raster == GanglinienRaster.Minute)
            {
                if (werte.Length % MinutenJeViertelstunde != 0)
                {
                    protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselMinutenRest, Zahl(werte.Length)));
                    return Abbruch(protokoll);
                }

                int vorher = werte.Length;
                werte = Mittelwerte(werte, MinutenJeViertelstunde);
                zeit = JedenNten(zeit, MinutenJeViertelstunde);
                raster = GanglinienRaster.Viertelstunde;
                gemittelt = true;

                protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselMinutenGemittelt,
                    Zahl(vorher), Zahl(werte.Length)));
            }

            // --- 5. Schaltjahr normalisieren -------------------------------
            bool schaltjahr = false;
            int intervalleJeTag = (int)raster * 24;
            if (werte.Length == SchaltjahrAnzahl(raster))
            {
                int vorher = werte.Length;

                if (zeit != null)
                {
                    var behaltenW = new List<double>(NormaljahrAnzahl(raster));
                    var behaltenZ = new List<DateTime>(NormaljahrAnzahl(raster));
                    for (int i = 0; i < werte.Length; i++)
                    {
                        if (zeit[i].Month == 2 && zeit[i].Day == 29) continue;
                        behaltenW.Add(werte[i]);
                        behaltenZ.Add(zeit[i]);
                    }
                    werte = behaltenW.ToArray();
                    zeit = behaltenZ.ToArray();
                }
                else
                {
                    // Ohne Zeitstempel gilt die Annahme "Reihe beginnt am 01.01. 00:00":
                    // der 29.02. ist Tag 60, also der Block ab Index 59 * Intervalle-je-Tag.
                    int start = TageVorSchalttag * intervalleJeTag;
                    var behalten = new List<double>(NormaljahrAnzahl(raster));
                    for (int i = 0; i < werte.Length; i++)
                    {
                        if (i >= start && i < start + intervalleJeTag) continue;
                        behalten.Add(werte[i]);
                    }
                    werte = behalten.ToArray();
                    protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselSchaltjahrPosition,
                        Zahl(start + 1), Zahl(start + intervalleJeTag)));
                }

                schaltjahr = true;
                protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselSchaltjahr,
                    Zahl(vorher), Zahl(werte.Length), Zahl(intervalleJeTag)));
            }

            if (werte.Length != NormaljahrAnzahl(raster))
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselLaengeFalsch,
                    Zahl(werte.Length), Zahl(NormaljahrAnzahl(raster))));
                return Abbruch(protokoll);
            }

            // --- 6. Plausibilitaet -----------------------------------------
            Plausibilitaet(werte, intervalleJeTag, eingang.AusreisserFaktor, protokoll);

            protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselErgebnis,
                Zahl(werte.Length), Zahl((int)raster), Zahl(Jahresarbeit(werte, raster))));

            return new GanglinienPruefErgebnis(werte, raster, protokoll, schaltjahr, gemittelt, sommerzeitBehandelt);
        }

        // ==================================================================
        // Raster
        // ==================================================================

        /// <summary>
        /// Bestimmt das Jahresraster aus der Wertanzahl (Normal- und Schaltjahr).
        /// </summary>
        /// <param name="anzahl">Anzahl der Werte.</param>
        /// <returns>Erkanntes Raster oder <see cref="GanglinienRaster.Unbekannt"/>.</returns>
        public static GanglinienRaster RasterAusAnzahl(int anzahl)
        {
            switch (anzahl)
            {
                case StundenJahr:
                case StundenSchaltjahr:
                    return GanglinienRaster.Stunde;
                case ViertelstundenJahr:
                case ViertelstundenSchaltjahr:
                    return GanglinienRaster.Viertelstunde;
                case MinutenJahr:
                case MinutenSchaltjahr:
                    return GanglinienRaster.Minute;
                default:
                    return GanglinienRaster.Unbekannt;
            }
        }

        /// <summary>Raster aus dem Zeitstempelabstand in Minuten (60 / 15 / 1).</summary>
        /// <param name="minuten">Abstand zweier Zeitstempel.</param>
        /// <returns>Erkanntes Raster oder <see cref="GanglinienRaster.Unbekannt"/>.</returns>
        public static GanglinienRaster RasterAusMinuten(int minuten)
        {
            switch (minuten)
            {
                case 60: return GanglinienRaster.Stunde;
                case 15: return GanglinienRaster.Viertelstunde;
                case 1: return GanglinienRaster.Minute;
                default: return GanglinienRaster.Unbekannt;
            }
        }

        /// <summary>Wertanzahl eines Normaljahres im angegebenen Raster.</summary>
        /// <param name="raster">Raster.</param>
        /// <returns>8.760, 35.040, 525.600 oder 0.</returns>
        public static int NormaljahrAnzahl(GanglinienRaster raster)
        {
            switch (raster)
            {
                case GanglinienRaster.Stunde: return StundenJahr;
                case GanglinienRaster.Viertelstunde: return ViertelstundenJahr;
                case GanglinienRaster.Minute: return MinutenJahr;
                default: return 0;
            }
        }

        /// <summary>Wertanzahl eines Schaltjahres im angegebenen Raster.</summary>
        /// <param name="raster">Raster.</param>
        /// <returns>8.784, 35.136, 527.040 oder 0.</returns>
        public static int SchaltjahrAnzahl(GanglinienRaster raster)
        {
            switch (raster)
            {
                case GanglinienRaster.Stunde: return StundenSchaltjahr;
                case GanglinienRaster.Viertelstunde: return ViertelstundenSchaltjahr;
                case GanglinienRaster.Minute: return MinutenSchaltjahr;
                default: return 0;
            }
        }

        /// <summary>
        /// Haeufigster positiver Abstand zweier aufeinanderfolgender Zeitstempel in
        /// ganzen Minuten. Der Modus statt des ersten Abstands, damit die
        /// Zeitumstellung (Sprung von 60 auf 120 Minuten) das Raster nicht kippt.
        /// </summary>
        /// <param name="zeit">Zeitstempelreihe.</param>
        /// <returns>Abstand in Minuten; 0, wenn keiner bestimmbar ist.</returns>
        public static int SchrittMinuten(DateTime[] zeit)
        {
            if (zeit == null || zeit.Length < 2) return 0;

            var haeufigkeit = new Dictionary<int, int>();
            int obergrenze = Math.Min(zeit.Length, 2000);   // die ersten Zeilen genuegen
            for (int i = 1; i < obergrenze; i++)
            {
                double m = (zeit[i] - zeit[i - 1]).TotalMinutes;
                if (m <= 0 || m > int.MaxValue) continue;
                int min = (int)Math.Round(m, MidpointRounding.AwayFromZero);
                if (min <= 0) continue;
                haeufigkeit.TryGetValue(min, out int n);
                haeufigkeit[min] = n + 1;
            }

            int besterWert = 0;
            int besteAnzahl = 0;
            foreach (var paar in haeufigkeit)
            {
                // Bei Gleichstand gewinnt der kleinere Abstand - deterministisch.
                if (paar.Value > besteAnzahl || (paar.Value == besteAnzahl && paar.Key < besterWert))
                {
                    besterWert = paar.Key;
                    besteAnzahl = paar.Value;
                }
            }
            return besterWert;
        }

        // ==================================================================
        // Zeitachse
        // ==================================================================

        /// <summary>
        /// Loest die Intervallkonvention auf. Bei <see cref="IntervallKonvention.Ende"/>
        /// werden alle Zeitstempel um ein Intervall zurueckgesetzt, damit die weitere
        /// Pruefung durchgaengig mit Intervallanfaengen arbeitet.
        /// </summary>
        private static DateTime[] KonventionAnwenden(
            DateTime[] zeit, int schrittMinuten, IntervallKonvention gewuenscht, List<PruefMeldung> protokoll)
        {
            bool ende;
            if (gewuenscht == IntervallKonvention.Ende) ende = true;
            else if (gewuenscht == IntervallKonvention.Anfang) ende = false;
            else
            {
                // Automatik: eine Reihe mit Intervallende beginnt genau ein Intervall
                // nach Mitternacht des 01.01.
                DateTime erst = zeit[0];
                ende = erst.Month == 1 && erst.Day == 1 &&
                       Math.Abs((erst - erst.Date).TotalMinutes - schrittMinuten) < 0.001;
            }

            if (!ende)
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselKonventionAnfang));
            }
            else
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselKonventionEnde, Zeitpunkt(zeit[0])));
                var verschoben = new DateTime[zeit.Length];
                for (int i = 0; i < zeit.Length; i++) verschoben[i] = zeit[i].AddMinutes(-schrittMinuten);
                zeit = verschoben;
            }

            DateTime start = zeit[0];
            if (!(start.Month == 1 && start.Day == 1 && start.TimeOfDay == TimeSpan.Zero))
                protokoll.Add(new PruefMeldung(PruefStufe.Warnung, SchluesselJahresanfang, Zeitpunkt(start)));

            return zeit;
        }

        /// <summary>
        /// Prueft Monotonie, mittelt Dubletten und fuellt Luecken. Die beiden
        /// Sommerzeitfaelle sind Info, alles andere Fehler.
        /// </summary>
        /// <returns><c>false</c>, sobald ein Fehler protokolliert wurde.</returns>
        private static bool ZeitachseNormalisieren(
            ref double[] werte, ref DateTime[]? zeit, int schrittMinuten,
            List<PruefMeldung> protokoll, ref bool sommerzeitBehandelt)
        {
            DateTime[] quelleZeit = zeit!;
            double[] quelleWerte = werte;
            int n = quelleWerte.Length;

            var zielWerte = new List<double>(n);
            var zielZeit = new List<DateTime>(n);

            int i = 0;
            while (i < n)
            {
                DateTime t = quelleZeit[i];

                // Block gleicher Zeitstempel zusammenfassen.
                int j = i;
                double summe = 0.0;
                while (j < n && quelleZeit[j] == t) { summe += quelleWerte[j]; j++; }
                int anzahl = j - i;

                if (anzahl > 1)
                {
                    if (IstHerbstumstellung(t))
                    {
                        protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselSommerzeitDublette,
                            Zeitpunkt(t), Zahl(anzahl)));
                        sommerzeitBehandelt = true;
                    }
                    else
                    {
                        protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselDublette,
                            Zeitpunkt(t), Zahl(anzahl)));
                        return false;
                    }
                }

                zielWerte.Add(summe / anzahl);
                zielZeit.Add(t);

                if (j < n)
                {
                    DateTime naechste = quelleZeit[j];
                    if (naechste < t)
                    {
                        protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselNichtMonoton,
                            Zeitpunkt(naechste), Zahl(j + 1)));
                        return false;
                    }

                    double schritte = (naechste - t).TotalMinutes / schrittMinuten;
                    int fehlend = (int)Math.Round(schritte, MidpointRounding.AwayFromZero) - 1;
                    if (fehlend > 0)
                    {
                        DateTime luecke = t.AddMinutes(schrittMinuten);
                        if (IstFruehjahrsumstellung(luecke, fehlend, schrittMinuten))
                        {
                            protokoll.Add(new PruefMeldung(PruefStufe.Info, SchluesselSommerzeitLuecke,
                                Zeitpunkt(luecke), Zahl(fehlend)));
                            sommerzeitBehandelt = true;

                            // Wertwiederholung des Vorintervalls - Hauskonvention
                            // (Stundenwerte_zu_viertelstunden), keine Interpolation.
                            double letzter = zielWerte[zielWerte.Count - 1];
                            for (int k = 0; k < fehlend; k++)
                            {
                                zielWerte.Add(letzter);
                                zielZeit.Add(t.AddMinutes(schrittMinuten * (k + 1)));
                            }
                        }
                        else
                        {
                            protokoll.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselLuecke,
                                Zeitpunkt(luecke), Zahl(fehlend)));
                            return false;
                        }
                    }
                }

                i = j;
            }

            werte = zielWerte.ToArray();
            zeit = zielZeit.ToArray();
            return true;
        }

        /// <summary>
        /// Letzter Sonntag eines Monats - Grundlage der EU-Zeitumstellungsregel
        /// (Richtlinie 2000/84/EG, gueltig seit 1996).
        /// </summary>
        /// <param name="jahr">Jahr.</param>
        /// <param name="monat">Monat (3 = Maerz, 10 = Oktober).</param>
        /// <returns>Datum des letzten Sonntags (00:00).</returns>
        public static DateTime LetzterSonntag(int jahr, int monat)
        {
            DateTime letzter = new DateTime(jahr, monat, DateTime.DaysInMonth(jahr, monat));
            int abstand = (int)letzter.DayOfWeek;     // Sonntag = 0
            return letzter.AddDays(-abstand);
        }

        /// <summary>
        /// Faellt <paramref name="t"/> in die Ortsstunde 02:00 des letzten
        /// Oktobersonntags (die bei der Rueckstellung doppelt auftritt)?
        /// </summary>
        /// <param name="t">Zu pruefender Zeitpunkt.</param>
        /// <returns><c>true</c>, wenn es die doppelte Herbststunde ist.</returns>
        public static bool IstHerbstumstellung(DateTime t)
        {
            if (t.Month != 10) return false;
            if (t.Date != LetzterSonntag(t.Year, 10)) return false;
            return t.Hour == 2;
        }

        /// <summary>
        /// Ist die Luecke ab <paramref name="beginn"/> genau die uebersprungene
        /// Ortsstunde 02:00 des letzten Maerzsonntags?
        /// </summary>
        /// <param name="beginn">Erster fehlender Zeitstempel.</param>
        /// <param name="fehlend">Anzahl fehlender Intervalle.</param>
        /// <param name="schrittMinuten">Rasterweite in Minuten.</param>
        /// <returns><c>true</c>, wenn die Luecke exakt eine Stunde am Umstelltag ist.</returns>
        public static bool IstFruehjahrsumstellung(DateTime beginn, int fehlend, int schrittMinuten)
        {
            if (beginn.Month != 3) return false;
            if (beginn.Date != LetzterSonntag(beginn.Year, 3)) return false;
            if (beginn.Hour != 2 || beginn.Minute != 0) return false;
            return fehlend * schrittMinuten == 60;
        }

        // ==================================================================
        // Umformungen
        // ==================================================================

        /// <summary>
        /// Arithmetisches Mittel ueber je <paramref name="breite"/> aufeinanderfolgende
        /// Werte - die Minutenmittelung des Fachkonzepts 3.2.
        /// </summary>
        /// <param name="werte">Eingangsreihe; Laenge muss durch <paramref name="breite"/> teilbar sein.</param>
        /// <param name="breite">Gruppenbreite.</param>
        /// <returns>Neue Reihe der Laenge <c>werte.Length / breite</c>.</returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="werte"/> <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Bei ungueltiger Breite oder Restlaenge.</exception>
        public static double[] Mittelwerte(double[] werte, int breite)
        {
            if (werte == null) throw new ArgumentNullException(nameof(werte));
            if (breite <= 0) throw new ArgumentException("Die Gruppenbreite muss positiv sein.", nameof(breite));
            if (werte.Length % breite != 0)
                throw new ArgumentException("Die Reihenlaenge muss durch die Gruppenbreite teilbar sein.", nameof(werte));

            double[] ziel = new double[werte.Length / breite];
            for (int g = 0; g < ziel.Length; g++)
            {
                double summe = 0.0;
                int b = g * breite;
                for (int k = 0; k < breite; k++) summe += werte[b + k];
                ziel[g] = summe / breite;
            }
            return ziel;
        }

        /// <summary>Jeden n-ten Zeitstempel behalten (Gruppenanfang der Mittelung).</summary>
        private static DateTime[]? JedenNten(DateTime[]? zeit, int breite)
        {
            if (zeit == null) return null;
            var ziel = new DateTime[zeit.Length / breite];
            for (int g = 0; g < ziel.Length; g++) ziel[g] = zeit[g * breite];
            return ziel;
        }

        /// <summary>Jahresarbeit einer kW-Reihe in kWh.</summary>
        private static double Jahresarbeit(double[] werte, GanglinienRaster raster)
        {
            double summe = 0.0;
            for (int i = 0; i < werte.Length; i++) summe += werte[i];
            return summe / (int)raster;
        }

        // ==================================================================
        // Plausibilitaet
        // ==================================================================

        private static void Plausibilitaet(
            double[] werte, int intervalleJeTag, double ausreisserFaktor, List<PruefMeldung> protokoll)
        {
            int negativ = 0;
            double minimum = double.MaxValue;
            double maximum = double.MinValue;
            int laufNull = 0, besterLaufNull = 0, besterStartNull = 0;
            bool allesNull = true;

            for (int i = 0; i < werte.Length; i++)
            {
                double w = werte[i];
                if (w < 0) { negativ++; }
                if (w < minimum) minimum = w;
                if (w > maximum) maximum = w;
                if (w != 0.0) allesNull = false;

                if (w == 0.0)
                {
                    laufNull++;
                    if (laufNull > besterLaufNull)
                    {
                        besterLaufNull = laufNull;
                        besterStartNull = i - laufNull + 2;   // 1-basierte Zeilennummer
                    }
                }
                else laufNull = 0;
            }

            if (negativ > 0)
                protokoll.Add(new PruefMeldung(PruefStufe.Warnung, SchluesselNegativeWerte,
                    Zahl(negativ), Zahl(minimum)));

            if (allesNull)
            {
                protokoll.Add(new PruefMeldung(PruefStufe.Warnung, SchluesselAlleNull));
                return;
            }

            if (besterLaufNull >= intervalleJeTag)
                protokoll.Add(new PruefMeldung(PruefStufe.Warnung, SchluesselNullserie,
                    Zahl(besterStartNull), Zahl(besterLaufNull)));

            if (ausreisserFaktor > 0)
            {
                double median = Median(werte);
                if (median > 0)
                {
                    double grenze = median * ausreisserFaktor;
                    int ueber = 0;
                    for (int i = 0; i < werte.Length; i++) if (werte[i] > grenze) ueber++;
                    if (ueber > 0)
                        protokoll.Add(new PruefMeldung(PruefStufe.Warnung, SchluesselAusreisser,
                            Zahl(ueber), Zahl(maximum), Zahl(median), Zahl(ausreisserFaktor)));
                }
            }
        }

        /// <summary>
        /// Median einer Reihe (unteres Mittel bei gerader Anzahl - deterministisch,
        /// ohne Mittelung der beiden mittleren Werte).
        /// </summary>
        /// <param name="werte">Eingangsreihe.</param>
        /// <returns>Median; 0 bei leerer Reihe.</returns>
        public static double Median(double[] werte)
        {
            if (werte == null || werte.Length == 0) return 0.0;
            double[] kopie = (double[])werte.Clone();
            Array.Sort(kopie);
            return kopie[kopie.Length / 2];
        }

        // ==================================================================
        // Hilfsmittel
        // ==================================================================

        private static GanglinienPruefErgebnis Abbruch(List<PruefMeldung> protokoll)
        {
            return new GanglinienPruefErgebnis(
                Array.Empty<double>(), GanglinienRaster.Unbekannt, protokoll, false, false, false);
        }

        /// <summary>Invariante Formatierung einer ganzen Zahl.</summary>
        private static string Zahl(int wert)
        {
            return wert.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Invariante Formatierung einer Gleitkommazahl (max. drei Nachkommastellen).</summary>
        private static string Zahl(double wert)
        {
            return wert.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>Invariante Formatierung eines Zeitpunkts im Hausformat.</summary>
        private static string Zeitpunkt(DateTime wert)
        {
            return wert.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
