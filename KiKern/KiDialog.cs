using System;
using System.Collections.Generic;
using System.Globalization;

namespace KiKern
{
    /// <summary>
    /// Ein steuerbares Feld einer freigegebenen Maske (Fachkonzept 11.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eine Deklaration, drei Verwendungen</b> - dasselbe Muster wie bei
    /// <see cref="KiParameter"/>: Aus diesem Objekt entstehen (a) die Feldliste fuer das
    /// Modell (<c>dialog_lesen</c>), (b) die Pruefung des Setzweges im Anwendungsprojekt
    /// und (c) der Klartext der Feldbestaetigung (<see cref="KiFeldBlock"/>). Damit koennen
    /// sie nicht auseinanderlaufen.
    /// </para>
    /// <para>
    /// <b>Warum <see cref="KiParameterTyp"/> und keine zweite Aufzaehlung.</b> Ein
    /// Maskenfeld traegt genau die Arten, die auch ein Aktionsparameter kennt. Eine eigene
    /// Feldtyp-Aufzaehlung waere die naechste Stelle, die sich auseinanderentwickeln kann -
    /// und der Erklaertext („erwartet eine Zahl") soll in beiden Faellen derselbe sein.
    /// </para>
    /// <para>
    /// <b>Der Kern kennt weder Controls noch Datenklassen.</b> <see cref="Eigenschaftspfad"/>
    /// ist reiner Text; aufgeloest wird er erst in der Oberflaeche, und zwar seit Auftrag
    /// #200 (Stufe S2) ueber die EIGENSCHAFT des Daten-Objekts eines Razor-Dialogs
    /// (<c>HeizkesselKatalogDaten.Ptherm</c>) statt ueber einen WinForms-Controlnamen.
    /// Die Controlnamen waren seit iU9 tot: Die vier Masken sind Razor-Komponenten, und
    /// <c>Application.OpenForms</c> fuehrt sie nicht mehr. So bleibt KiKern referenzfrei
    /// (Fachkonzept 3.7) - und die Deklaration bleibt pruefbar, ohne eine Maske zu oeffnen.
    /// </para>
    /// </remarks>
    public sealed class KiDialogFeld
    {
        /// <summary>
        /// Deklariert ein steuerbares Feld.
        /// </summary>
        /// <param name="name">Logischer, sprachneutraler Schluessel (ASCII, wie <see cref="KiName"/>).</param>
        /// <param name="eigenschaftspfad">
        /// Typname und Eigenschaft im Daten-Objekt des Dialogs, z. B.
        /// <c>HeizkesselKatalogDaten.Ptherm</c> (Auftrag #200).
        /// </param>
        /// <param name="anzeigename">Klartextname - genau der Text, den der Anwender auf der Maske liest.</param>
        /// <param name="typ">Feldart; wiederverwendet <see cref="KiParameterTyp"/>.</param>
        /// <param name="erlaeuterung">Ein Satz Klartext fuer <c>dialog_parameter_erklaeren</c>.</param>
        /// <param name="einheit">Einheit fuer die Anzeige, z. B. „kWh"; leer, wenn keine.</param>
        /// <param name="leerErlaubt">Darf das Feld leer bleiben?</param>
        /// <param name="hilfeSlug">Slug des Hilfeartikels (<c>WordPressHelpCatalog.Get</c>); <c>null</c> = keiner.</param>
        public KiDialogFeld(string name,
                            string eigenschaftspfad,
                            string anzeigename,
                            KiParameterTyp typ,
                            string erlaeuterung,
                            string? einheit = null,
                            bool leerErlaubt = false,
                            string? hilfeSlug = null)
        {
            if (!KiName.IstGueltig(name))
                throw new ArgumentException(
                    "Feldname '" + name + "' ist nicht zulaessig (erlaubt: a-z, 0-9, _; hoechstens 64 Zeichen).",
                    nameof(name));
            if (!KiEigenschaftspfad.IstGueltig(eigenschaftspfad))
                throw new ArgumentException(
                    "Das Feld '" + name + "' braucht einen Eigenschaftspfad der Form " +
                    "'Datentyp.Eigenschaft' (geliefert: '" + eigenschaftspfad + "').",
                    nameof(eigenschaftspfad));
            if (string.IsNullOrWhiteSpace(anzeigename))
                throw new ArgumentException(
                    "Das Feld '" + name + "' braucht den Anzeigenamen, der auf der Maske steht.", nameof(anzeigename));

            // PFLICHTTEXT: Ohne Erlaeuterung koennte „dialog_parameter_erklaeren" nur den
            // Anzeigenamen wiederholen - genau die Antwort, die der Anwender schon sieht.
            if (string.IsNullOrWhiteSpace(erlaeuterung))
                throw new ArgumentException(
                    "Das Feld '" + name + "' braucht eine Erlaeuterung in einem Satz.", nameof(erlaeuterung));

            // Eine Zahlenliste hat auf einer Maske kein Control: gesetzt werden Textfeld,
            // Haekchen und Auswahlliste (Fachkonzept 11.4). Was sich nicht setzen laesst,
            // soll sich auch nicht deklarieren lassen - sonst faellt es erst zur Laufzeit auf.
            if (typ == KiParameterTyp.GanzzahlListe)
                throw new ArgumentException(
                    "Das Feld '" + name + "' kann keine Zahlenliste sein; ein Maskenfeld traegt genau einen Wert.",
                    nameof(typ));

            Name = name;
            Eigenschaftspfad = eigenschaftspfad.Trim();
            Anzeigename = anzeigename;
            Typ = typ;
            Erlaeuterung = erlaeuterung;
            Einheit = einheit ?? "";
            LeerErlaubt = leerErlaubt;
            HilfeSlug = string.IsNullOrWhiteSpace(hilfeSlug) ? "" : hilfeSlug!.Trim();
        }

        /// <summary>Logischer, sprachneutraler Schluessel des Feldes.</summary>
        public string Name { get; }

        /// <summary>
        /// Typname und Eigenschaft im Daten-Objekt des Dialogs
        /// (<c>HeizkesselKatalogDaten.Ptherm</c>) - aufgeloest wird er in der Oberflaeche.
        /// </summary>
        public string Eigenschaftspfad { get; }

        /// <summary>Der Typname vor dem Punkt - das Daten-Objekt, an dem die Eigenschaft haengt.</summary>
        public string Datentyp => KiEigenschaftspfad.Datentyp(Eigenschaftspfad);

        /// <summary>Der Eigenschaftsname hinter dem Punkt.</summary>
        public string Eigenschaft => KiEigenschaftspfad.Eigenschaft(Eigenschaftspfad);

        /// <summary>Klartextname, wie er auf der Maske steht.</summary>
        public string Anzeigename { get; }

        /// <summary>Feldart.</summary>
        public KiParameterTyp Typ { get; }

        /// <summary>Ein Satz Klartext fuer die Erklaerung.</summary>
        public string Erlaeuterung { get; }

        /// <summary>Einheit fuer die Anzeige; leer, wenn keine.</summary>
        public string Einheit { get; }

        /// <summary>Darf das Feld leer bleiben?</summary>
        public bool LeerErlaubt { get; }

        /// <summary>Slug des Hilfeartikels; leer, wenn keiner deklariert ist.</summary>
        public string HilfeSlug { get; }

        /// <summary>Ist ein Hilfeartikel deklariert?</summary>
        public bool HatHilfe => HilfeSlug.Length > 0;

        /// <inheritdoc/>
        public override string ToString() => Name + " (" + Eigenschaftspfad + ")";
    }

    /// <summary>
    /// Ein ausloesbarer Knopf einer freigegebenen Maske - die Positivliste des
    /// Katalogeintrags (Fachkonzept 11.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Loeschknoepfe gibt es hier per Bauart nicht.</b> Die Verbotsliste (Fachkonzept
    /// 1.2, Grenzen 11.7) schliesst Loeschen aus. Eine Positivliste allein truege das
    /// nur so lange, wie niemand einen Eintrag zuviel schreibt; deshalb weist schon
    /// DIESER Konstruktor jeden Namen und jeden Controlpfad ab, der nach Loeschen
    /// aussieht (<see cref="IstLoeschbezeichnung"/>). Ein Loeschknopf ist damit nicht
    /// „nicht deklariert", sondern nicht deklarierBAR.
    /// </para>
    /// <para>
    /// <b>Der Fehlalarm ist gewollt.</b> Die Regel trifft auch einen harmlosen Knopf, der
    /// zufaellig „loesch" im Namen fuehrt. Das ist der guenstigere Fehler: Er faellt beim
    /// ersten Testlauf auf und kostet eine Umbenennung im Katalog - der umgekehrte Fehler
    /// kostet Daten.
    /// </para>
    /// </remarks>
    public sealed class KiDialogKnopf
    {
        /// <summary>
        /// Wortstaemme, die einen Knopf unwiderruflich vom Katalog ausschliessen.
        /// Verglichen wird ohne Ruecksicht auf Gross- und Kleinschreibung.
        /// </summary>
        public static readonly IReadOnlyList<string> Loeschwoerter =
            new[] { "loesch", "lösch", "delete" };

        /// <summary>
        /// Deklariert einen ausloesbaren Knopf.
        /// </summary>
        /// <param name="name">Logischer, sprachneutraler Schluessel (ASCII, wie <see cref="KiName"/>).</param>
        /// <param name="controlpfad">Pfad des Knopfes in der Maske, z. B. <c>btn_Speichern</c>.</param>
        /// <param name="anzeigename">Beschriftung, wie sie der Anwender liest, z. B. „Speichern".</param>
        public KiDialogKnopf(string name, string controlpfad, string anzeigename)
        {
            if (!KiName.IstGueltig(name))
                throw new ArgumentException(
                    "Knopfname '" + name + "' ist nicht zulaessig (erlaubt: a-z, 0-9, _; hoechstens 64 Zeichen).",
                    nameof(name));
            if (!KiControlpfad.IstGueltig(controlpfad))
                throw new ArgumentException(
                    "Der Knopf '" + name + "' braucht einen Controlpfad ohne Leerzeichen und ohne leere Stufe " +
                    "(geliefert: '" + controlpfad + "').", nameof(controlpfad));
            if (string.IsNullOrWhiteSpace(anzeigename))
                throw new ArgumentException(
                    "Der Knopf '" + name + "' braucht seine Beschriftung im Klartext.", nameof(anzeigename));

            if (IstLoeschbezeichnung(name))
                throw new ArgumentException(
                    "Loeschknoepfe sind nicht deklarierbar (Fachkonzept 1.2/11.7); '" + name + "' sieht nach Loeschen aus.",
                    nameof(name));
            if (IstLoeschbezeichnung(controlpfad))
                throw new ArgumentException(
                    "Loeschknoepfe sind nicht deklarierbar (Fachkonzept 1.2/11.7); '" + controlpfad +
                    "' sieht nach Loeschen aus.", nameof(controlpfad));

            Name = name;
            Controlpfad = controlpfad.Trim();
            Anzeigename = anzeigename;
        }

        /// <summary>Logischer, sprachneutraler Schluessel des Knopfes.</summary>
        public string Name { get; }

        /// <summary>Pfad des Knopfes in der Maske.</summary>
        public string Controlpfad { get; }

        /// <summary>Beschriftung im Klartext.</summary>
        public string Anzeigename { get; }

        /// <summary>
        /// Sieht dieser Text nach Loeschen aus? Grundlage der Bauartsperre - eine Stelle,
        /// damit Knopf und Katalog dieselbe Regel pruefen.
        /// </summary>
        public static bool IstLoeschbezeichnung(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            foreach (string wort in Loeschwoerter)
                if (text!.IndexOf(wort, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        /// <inheritdoc/>
        public override string ToString() => Name + " (" + Controlpfad + ")";
    }

    /// <summary>
    /// Abweichende Ruheposition des Aufrufknopfs auf einer Maske (Fachkonzept 11.8).
    /// </summary>
    /// <remarks>
    /// Der Regelplatz ist oben rechts im Client-Bereich. Fuehrt eine Maske dort schon
    /// Bedienelemente, haelt IHR Katalogeintrag die Ausnahme fest - je Maske deklariert,
    /// nicht je Aufruf improvisiert. Die Abstaende sind blosse Zahlen: der Kern kennt
    /// weder <c>Point</c> noch <c>Anchor</c> (Fachkonzept 3.7).
    /// </remarks>
    public sealed class KiKnopfposition
    {
        /// <summary>Legt eine Position aus den Abstaenden zur oberen rechten Ecke an.</summary>
        /// <param name="abstandRechts">Abstand zum rechten Rand in Bildpunkten.</param>
        /// <param name="abstandOben">Abstand zum oberen Rand in Bildpunkten.</param>
        public KiKnopfposition(int abstandRechts, int abstandOben)
        {
            if (abstandRechts < 0)
                throw new ArgumentOutOfRangeException(nameof(abstandRechts), "Der Abstand darf nicht negativ sein.");
            if (abstandOben < 0)
                throw new ArgumentOutOfRangeException(nameof(abstandOben), "Der Abstand darf nicht negativ sein.");

            AbstandRechts = abstandRechts;
            AbstandOben = abstandOben;
        }

        /// <summary>Abstand zum rechten Rand in Bildpunkten.</summary>
        public int AbstandRechts { get; }

        /// <summary>Abstand zum oberen Rand in Bildpunkten.</summary>
        public int AbstandOben { get; }

        /// <inheritdoc/>
        public override string ToString()
            => AbstandRechts.ToString(CultureInfo.InvariantCulture) + "/" +
               AbstandOben.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Der Katalogeintrag EINER freigegebenen Maske (Fachkonzept 11.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Gesteuert wird nur, was deklariert ist.</b> Was hier nicht steht, gibt es fuer
    /// den Assistenten nicht - weder als Feld noch als Knopf. Das ist dieselbe Regel wie
    /// beim Aktionsregister (Fachkonzept 3.2) und der Grund, warum eine „unbekannte" Maske
    /// keine Rueckfrage ausloest, sondern eine Ablehnung im Klartext.
    /// </para>
    /// <para>
    /// <b>Der Maskenname ist der Typname der Form</b> (z. B. <c>Form_Heizkessel_Bearbeiten</c>),
    /// nicht ihr Fenstertitel: Der Titel wechselt mit dem Datensatz, der Typname nicht -
    /// und ueber ihn findet das Anwendungsprojekt die offene Maske in
    /// <c>Application.OpenForms</c> wieder.
    /// </para>
    /// </remarks>
    public sealed class KiDialog
    {
        /// <summary>Hoechstlaenge eines Maskennamens.</summary>
        public const int MaxMaskenname = 128;

        private readonly List<KiDialogFeld> _felder = new List<KiDialogFeld>();
        private readonly List<KiDialogKnopf> _knoepfe = new List<KiDialogKnopf>();

        /// <summary>
        /// Deklariert eine steuerbare Maske.
        /// </summary>
        /// <param name="maskenname">Typname der Form, z. B. <c>Form_PV</c>.</param>
        /// <param name="anzeigename">Klartextname der Maske fuer Chat und Bestaetigung.</param>
        /// <param name="felder">Steuerbare Felder in Anzeigereihenfolge.</param>
        /// <param name="knoepfe">Ausloesbare Knoepfe als Positivliste - nie ein Loeschknopf.</param>
        /// <param name="knopfposition">Abweichende Position des Aufrufknopfs; <c>null</c> = Regelplatz.</param>
        public KiDialog(string maskenname,
                        string anzeigename,
                        IReadOnlyList<KiDialogFeld>? felder = null,
                        IReadOnlyList<KiDialogKnopf>? knoepfe = null,
                        KiKnopfposition? knopfposition = null)
        {
            if (!IstGueltigerMaskenname(maskenname))
                throw new ArgumentException(
                    "Maskenname '" + maskenname + "' ist kein Typname einer Form.", nameof(maskenname));
            if (string.IsNullOrWhiteSpace(anzeigename))
                throw new ArgumentException(
                    "Die Maske '" + maskenname + "' braucht einen Anzeigenamen im Klartext.", nameof(anzeigename));

            Maskenname = maskenname;
            Anzeigename = anzeigename;
            Knopfposition = knopfposition;

            // Doppelte LOGISCHE Namen sind ein Programmierfehler wie beim Aktionsparameter
            // (KiAktion): das Modell koennte den zweiten nie erreichen.
            var namen = new HashSet<string>(StringComparer.Ordinal);

            // ZWEI Pfadmengen seit Auftrag #200, nicht mehr eine. Ein Feld traegt jetzt
            // einen EIGENSCHAFTSpfad ("HeizkesselKatalogDaten.Ptherm"), ein Knopf
            // weiterhin seinen Controlnamen ("btn_Speichern"): Das sind zwei
            // Namensraeume, die einander nicht mehr treffen koennen. Eine gemeinsame
            // Menge behauptete eine Kollision, die es nicht gibt - und verschwiege im
            // Fehlerfall, WELCHE Art Pfad doppelt steht. Verglichen wird weiterhin ohne
            // Ruecksicht auf Gross-/Kleinschreibung: Die Aufloesung (Reflection bzw.
            // Controlsuche) tut es ebenso, zwei solche Eintraege zeigten also auf
            // dasselbe Ziel.
            var feldpfade = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var knopfpfade = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (felder != null)
                foreach (KiDialogFeld f in felder)
                {
                    if (f == null)
                        throw new ArgumentException(
                            "Die Maske '" + maskenname + "' fuehrt einen leeren Feldeintrag.", nameof(felder));
                    if (!namen.Add(f.Name))
                        throw new ArgumentException(
                            "Das Feld '" + f.Name + "' ist in '" + maskenname + "' doppelt deklariert.", nameof(felder));
                    if (!feldpfade.Add(f.Eigenschaftspfad))
                        throw new ArgumentException(
                            "Der Eigenschaftspfad '" + f.Eigenschaftspfad + "' ist in '" + maskenname +
                            "' doppelt deklariert.", nameof(felder));
                    _felder.Add(f);
                }

            var knopfnamen = new HashSet<string>(StringComparer.Ordinal);

            if (knoepfe != null)
                foreach (KiDialogKnopf k in knoepfe)
                {
                    if (k == null)
                        throw new ArgumentException(
                            "Die Maske '" + maskenname + "' fuehrt einen leeren Knopfeintrag.", nameof(knoepfe));
                    if (!knopfnamen.Add(k.Name))
                        throw new ArgumentException(
                            "Der Knopf '" + k.Name + "' ist in '" + maskenname + "' doppelt deklariert.", nameof(knoepfe));
                    if (!knopfpfade.Add(k.Controlpfad))
                        throw new ArgumentException(
                            "Der Controlpfad '" + k.Controlpfad + "' ist in '" + maskenname + "' doppelt deklariert.",
                            nameof(knoepfe));
                    _knoepfe.Add(k);
                }
        }

        /// <summary>Typname der Form - der Schluessel im Katalog.</summary>
        public string Maskenname { get; }

        /// <summary>Klartextname der Maske.</summary>
        public string Anzeigename { get; }

        /// <summary>Steuerbare Felder in Deklarationsreihenfolge.</summary>
        public IReadOnlyList<KiDialogFeld> Felder => _felder;

        /// <summary>Ausloesbare Knoepfe in Deklarationsreihenfolge (Positivliste).</summary>
        public IReadOnlyList<KiDialogKnopf> Knoepfe => _knoepfe;

        /// <summary>Abweichende Position des Aufrufknopfs; <c>null</c> = Regelplatz oben rechts.</summary>
        public KiKnopfposition? Knopfposition { get; }

        /// <summary>Weicht der Aufrufknopf dieser Maske vom Regelplatz ab?</summary>
        public bool HatKnopfposition => Knopfposition != null;

        /// <summary>Liefert das Feld, oder <c>null</c>.</summary>
        public KiDialogFeld? FindeFeld(string? name)
        {
            if (name == null) return null;
            foreach (KiDialogFeld f in _felder)
                if (string.Equals(f.Name, name, StringComparison.Ordinal)) return f;
            return null;
        }

        /// <summary>Liefert den Knopf, oder <c>null</c>.</summary>
        public KiDialogKnopf? FindeKnopf(string? name)
        {
            if (name == null) return null;
            foreach (KiDialogKnopf k in _knoepfe)
                if (string.Equals(k.Name, name, StringComparison.Ordinal)) return k;
            return null;
        }

        /// <summary>Kennt die Maske dieses Feld?</summary>
        public bool KenntFeld(string? name) => FindeFeld(name) != null;

        /// <summary>Kennt die Maske diesen Knopf?</summary>
        public bool KenntKnopf(string? name) => FindeKnopf(name) != null;

        /// <summary>
        /// Die Namen aller Felder, alphabetisch - fuer die Klartext-Ablehnung eines nicht
        /// deklarierten Feldes (Fachkonzept 11.4: Ablehnung nennt, was es gibt).
        /// </summary>
        public IReadOnlyList<string> Feldnamen() => Sortiert(_felder.ConvertAll(f => f.Name));

        /// <summary>Die Namen aller Knoepfe, alphabetisch.</summary>
        public IReadOnlyList<string> Knopfnamen() => Sortiert(_knoepfe.ConvertAll(k => k.Name));

        /// <summary>
        /// Ist das ein Typname einer Form? Erlaubt sind Buchstaben, Ziffern und
        /// Unterstrich; das erste Zeichen ist kein Ziffernzeichen.
        /// </summary>
        /// <remarks>
        /// Bewusst NICHT die ASCII-Regel des Werkzeugkatalogs (<see cref="KiName"/>): Der
        /// Maskenname ist ein C#-Typname aus dem Bestand („Form_PV") und traegt
        /// Grossbuchstaben - er geht auch nicht als Aktionsname auf die Leitung, sondern
        /// als Parameterwert.
        /// </remarks>
        public static bool IstGueltigerMaskenname(string? name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name!.Length > MaxMaskenname) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != '_') return false;

            return true;
        }

        private static IReadOnlyList<string> Sortiert(List<string> namen)
        {
            namen.Sort(StringComparer.Ordinal);
            return namen;
        }

        /// <inheritdoc/>
        public override string ToString()
            => Maskenname + " (" + _felder.Count + " Felder, " + _knoepfe.Count + " Knoepfe)";
    }

    /// <summary>
    /// Namensregel fuer Controlpfade. Sie steht hier an EINER Stelle, damit Feld und Knopf
    /// dieselbe pruefen (Vorbild <see cref="KiName"/>).
    /// </summary>
    /// <remarks>
    /// Ein Controlpfad ist die mit Punkten verkettete Folge von <c>Control.Name</c>-Werten
    /// des Bestands. Diese Namen sind Bezeichner aus der Designer-Datei; sie fuehren
    /// durchaus Nicht-ASCII (<c>tb_Wirkungsgrad_Öl</c>), aber nie Leerzeichen und nie eine
    /// leere Stufe. Genau das wird hier geprueft - mehr kann der Kern nicht wissen, denn
    /// ob der Pfad wirklich aufloest, weist erst der Katalogtest des Anwendungsprojekts
    /// nach (Fachkonzept 11.3).
    /// </remarks>
    public static class KiControlpfad
    {
        /// <summary>Trennzeichen der Pfadstufen.</summary>
        public const char Trenner = '.';

        /// <summary>Ist das ein brauchbarer Controlpfad?</summary>
        public static bool IstGueltig(string? pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad)) return false;

            string p = pfad!.Trim();
            foreach (char c in p)
                if (char.IsWhiteSpace(c)) return false;

            foreach (string stufe in p.Split(Trenner))
                if (stufe.Length == 0) return false;

            return true;
        }
    }

    /// <summary>
    /// Namensregel fuer EIGENSCHAFTSpfade der Maskenfelder (Auftrag #200, Stufe S2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ein Eigenschaftspfad hat GENAU ZWEI Stufen: den Typnamen des Daten-Objekts und
    /// den Namen der Eigenschaft darin (<c>HeizkesselKatalogDaten.Ptherm</c>). Beide
    /// sind C#-Bezeichner - Buchstabe oder Unterstrich am Anfang, danach Buchstaben,
    /// Ziffern und Unterstriche.
    /// </para>
    /// <para>
    /// <b>Warum genau zwei und nicht beliebig viele.</b> Der Getter der Maskenbruecke
    /// liest die Eigenschaft per Reflection an EINEM Objekt; ein tieferer Pfad
    /// (<c>Daten.Flotte.Optionen.Betriebsziel</c>) haette ueber eine Kette von
    /// <c>null</c>-Stellen zu laufen, und jede davon waere eine stille Fehlerquelle.
    /// Wo eine Maske eine Tiefe braucht, bekommt sie ein flaches SICHTMODELL, das die
    /// Kette EINMAL an einer benannten Stelle aufloest (so die Stromspeicher-Ansicht).
    /// </para>
    /// <para>
    /// <b>Der Typname steht mit im Pfad</b>, obwohl er zum Aufloesen nicht noetig waere:
    /// Er ist die Probe, dass Katalog und Daten-Objekt zusammengehoeren. Meldet ein
    /// Dialog ein Objekt anderen Typs an, faellt es beim Anmelden auf und nicht erst,
    /// wenn eine Eigenschaft zufaellig gleich heisst.
    /// </para>
    /// </remarks>
    public static class KiEigenschaftspfad
    {
        /// <summary>Trennzeichen zwischen Typname und Eigenschaft.</summary>
        public const char Trenner = '.';

        /// <summary>Ist das ein brauchbarer Eigenschaftspfad <c>Typ.Eigenschaft</c>?</summary>
        public static bool IstGueltig(string? pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad)) return false;

            string[] stufen = pfad!.Trim().Split(Trenner);
            if (stufen.Length != 2) return false;

            foreach (string stufe in stufen)
                if (!IstBezeichner(stufe)) return false;

            return true;
        }

        /// <summary>Der Typname vor dem Punkt; leer, wenn der Pfad nicht gueltig ist.</summary>
        public static string Datentyp(string? pfad) => Stufe(pfad, 0);

        /// <summary>Der Eigenschaftsname hinter dem Punkt; leer, wenn der Pfad nicht gueltig ist.</summary>
        public static string Eigenschaft(string? pfad) => Stufe(pfad, 1);

        private static string Stufe(string? pfad, int nummer)
        {
            if (!IstGueltig(pfad)) return "";
            return pfad!.Trim().Split(Trenner)[nummer];
        }

        /// <summary>Ist das ein C#-Bezeichner?</summary>
        private static bool IstBezeichner(string? stufe)
        {
            if (string.IsNullOrEmpty(stufe)) return false;
            if (!char.IsLetter(stufe![0]) && stufe[0] != '_') return false;

            foreach (char c in stufe)
                if (!char.IsLetterOrDigit(c) && c != '_') return false;

            return true;
        }
    }
}
