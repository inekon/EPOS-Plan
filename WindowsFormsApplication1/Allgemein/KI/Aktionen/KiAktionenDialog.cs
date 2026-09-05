using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die fuenf Aktionen der Formularsteuerung (Fachkonzept 11.4, Umsetzungskonzept
    /// Etappe 3b, Paket F3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zwei lesende, drei schreibende.</b> <c>dialog_lesen</c> und
    /// <c>dialog_parameter_erklaeren</c> gehoeren zu <see cref="Schutzstufe.Lesen"/>: Sie
    /// fassen nichts an. <c>feld_setzen</c>, <c>formular_ausfuellen</c> und
    /// <c>dialog_aktion_ausfuehren</c> gehoeren zu <see cref="Schutzstufe.Schreiben"/> und
    /// tragen zusaetzlich <see cref="KiAktion.Formularaktion"/> - die „Stufe 2F" des
    /// Fachkonzepts.
    /// </para>
    /// <para>
    /// <b>Was das Kennzeichen 2F NICHT ist: eine Ausnahme vom Riegel.</b>
    /// <c>KiRiegel.BrauchtBestaetigung</c> haengt allein an der Stufe; diese drei Aktionen
    /// brauchen deshalb dieselbe Bestaetigung wie jede andere Schreibaktion. Das
    /// Kennzeichen entscheidet ueber genau zwei Dinge im Anwendungsprojekt: die
    /// Modalitaetsweiche im <see cref="KiAusfuehrer"/> (eine Formularaktion VERLANGT die
    /// offene Zielmaske, waehrend alle uebrigen Aktionen bei offenem modalem Dialog
    /// abgewiesen werden) und - seit Paket F4 - die abschaltbare Feldsicherung
    /// (<see cref="KiBestaetigungspflicht"/>).
    /// </para>
    /// <para>
    /// <b>Der Sicherungspunkt haengt NICHT am Kennzeichen 2F, sondern an
    /// <see cref="KiAktion.Datenbankwirksam"/></b> (Festlegung Paket F4).
    /// <c>feld_setzen</c> und <c>formular_ausfuellen</c> fuellen nur Eingabefelder und
    /// brauchen keine Datenbankkopie; <c>dialog_aktion_ausfuehren</c> behaelt sie, weil der
    /// ausgeloeste Knopf ueber den Bestand in die Datenbank schreibt. Die Begruendung steht
    /// bei jeder der drei Deklarationen.
    /// </para>
    /// <para>
    /// <b>Keine zweite Eingabepruefung.</b> Gesetzt wird TEXT, geprueft wird am Knopf der
    /// Maske (<c>Program.ZahlPruefen</c>/<c>GanzzahlPruefen</c>, Fachkonzept 11.2). „abc"
    /// laesst sich deshalb in ein Zahlenfeld eintragen und scheitert erst bei
    /// <c>dialog_aktion_ausfuehren</c> - mit der Meldung des BESTANDS. Genau das fordert
    /// die Abnahme (Umsetzungskonzept 3b, Punkt 4): Der Assistent ersetzt die Pruefung
    /// nicht, er loest sie aus.
    /// </para>
    /// <para>
    /// <b>Die Vorschau kommt aus dem Kern.</b> Der Bestaetigungsblock entsteht in
    /// <see cref="KiFeldBlock"/> aus Katalogtexten und den auf dem UI-Thread gelesenen
    /// Werten - nie aus Modelltext. Waere er Modelltext, bestaetigte der Anwender eine
    /// Beschreibung, die mit dem tatsaechlichen Eingriff nichts zu tun haben muss.
    /// </para>
    /// </remarks>
    internal static class KiAktionenDialog
    {
        /// <summary>Trennt die Zuweisungen in <c>formular_ausfuellen</c>.</summary>
        private const char ZUWEISUNGSTRENNER = ';';

        /// <summary>Trennt Feldname und Wert innerhalb einer Zuweisung.</summary>
        private const char ZUWEISUNGSZEICHEN = '=';

        // =====================================================================
        // dialog_lesen
        // =====================================================================

        /// <summary>
        /// Nennt Felder, aktuelle Werte und auslösbare Knoepfe der offenen Katalogmaske.
        /// Andockpunkt <c>KiDialogZugriff.Aufloesen</c> / <c>LiesText</c>.
        /// </summary>
        /// <remarks>
        /// <b>Das ist das „Finden" der Parameter</b> (Auftrag vom 20.08.2026). Ohne diese
        /// Aktion muesste das Modell Feldnamen raten; mit ihr bekommt es genau die Liste,
        /// die auch die Pruefung und die Bestaetigung verwenden - eine Deklaration, drei
        /// Verwendungen.
        /// </remarks>
        internal static KiAktion DialogLesen()
        {
            return new KiAktion(
                name: "dialog_lesen",
                zweck: KiAktionsTexte.ZweckDialogLesen,
                titel: KiAktionsTexte.TitelDialogLesen,
                beispiel: KiAktionsTexte.BeispielDialogLesen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KiDialogZugriff.Aufloesen / LiesText",
                parameter: new[] { MaskeParameter() },
                vorbedingung: a => KiDialogZugriff.Aufloesen(a.Text("maske"), false).Grund,
                ausfuehren: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), false);
                    if (!bezug.Ok) return KiErgebnis.Abgelehnt(bezug.Grund);

                    var zeilen = KiHilfe.Liste();

                    foreach (KiDialogFeld f in bezug.Eintrag.Felder)
                    {
                        Control c = KiDialogZugriff.Aufloesen(bezug.Maske, f.Controlpfad);
                        string hindernis = KiDialogZugriff.PruefeSetzbar(f, c);

                        zeilen.Add(KiHilfe.Zeile(
                            "art", "feld",
                            "name", f.Name,
                            "anzeigename", KiHilfe.Text(f.Anzeigename),
                            "typ", f.Typ.ToString(),
                            "einheit", KiHilfe.Text(f.Einheit),
                            "leer_erlaubt", f.LeerErlaubt,
                            "wert", KiHilfe.Text(KiDialogZugriff.LiesText(c)),
                            "bedienbar", hindernis == null,
                            "hinweis", KiHilfe.Text(hindernis)));
                    }

                    foreach (KiDialogKnopf k in bezug.Eintrag.Knoepfe)
                    {
                        Control c = KiDialogZugriff.Aufloesen(bezug.Maske, k.Controlpfad);
                        string hindernis = KiDialogZugriff.PruefeKnopf(k, c);

                        zeilen.Add(KiHilfe.Zeile(
                            "art", "knopf",
                            "name", k.Name,
                            "anzeigename", KiHilfe.Text(k.Anzeigename),
                            "typ", "",
                            "einheit", "",
                            "leer_erlaubt", false,
                            "wert", "",
                            "bedienbar", hindernis == null,
                            "hinweis", KiHilfe.Text(hindernis)));
                    }

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.Gelesen,
                                      bezug.Eintrag.Anzeigename,
                                      bezug.Eintrag.Felder.Count, bezug.Eintrag.Knoepfe.Count),
                        zeilen);
                });
        }

        // =====================================================================
        // dialog_parameter_erklaeren
        // =====================================================================

        /// <summary>
        /// Erklaert ein Feld: Anzeigename, Art, Einheit, Leer-Regel, Erlaeuterung - und,
        /// wo ein Hilfe-Slug deklariert ist, den Hilfetext aus
        /// <c>WikiHelpCatalog.Get</c> (<c>Allgemein\Hilfe\HelpCatalog.cs:194</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der Hilfetext ist ein ZUSATZ, keine Bedingung.</b> Heute deklariert kein
        /// Katalogfeld einen Slug (Begruendung in <see cref="KiDialoge"/>: die Zuordnung
        /// haengt an <c>help_mapping.txt</c>, die es im Baum nicht gibt). Die Aktion
        /// antwortet deshalb aus der Katalog-Erlaeuterung - und liefert den Hilfeartikel
        /// zusaetzlich, sobald ein Slug dazukommt. Genau deswegen ist die Erlaeuterung im
        /// Katalog Pflichttext (<see cref="KiDialogFeld"/>): Sonst haetten Felder ohne Slug
        /// gar keine Antwort.
        /// </para>
        /// <para>
        /// <b>Der Hilfekatalog darf fehlen.</b> <c>WikiHelpCatalog.Aktueller</c> wird erst
        /// in <c>Program.Main</c> belegt; im Aktionsharnisch und in Prueflaeufen gibt es ihn
        /// nicht. Ein fehlender Hilfetext ist ein Schoenheitsfehler und kein Grund, die
        /// Erklaerung scheitern zu lassen.
        /// </para>
        /// </remarks>
        internal static KiAktion DialogParameterErklaeren()
        {
            return new KiAktion(
                name: "dialog_parameter_erklaeren",
                zweck: KiAktionsTexte.ZweckDialogErklaeren,
                titel: KiAktionsTexte.TitelDialogErklaeren,
                beispiel: KiAktionsTexte.BeispielDialogErklaeren,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KiDialogKatalog / WikiHelpCatalog.Get",
                parameter: new[] { MaskeParameter(), FeldParameter() },
                vorbedingung: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), false);
                    if (!bezug.Ok) return bezug.Grund;
                    return FeldGrund(bezug, a.Text("feld"));
                },
                ausfuehren: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), false);
                    if (!bezug.Ok) return KiErgebnis.Abgelehnt(bezug.Grund);

                    KiDialogFeld feld = bezug.Eintrag.FindeFeld(a.Text("feld"));
                    if (feld == null)
                        return KiErgebnis.Abgelehnt(FeldGrund(bezug, a.Text("feld")));

                    HelpEntry hilfe = Hilfetext(feld);
                    string leerregel = feld.LeerErlaubt
                        ? KiDialogTexte.LeerErlaubt
                        : KiDialogTexte.LeerPflicht;

                    Control c = KiDialogZugriff.Aufloesen(bezug.Maske, feld.Controlpfad);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "name", feld.Name,
                        "anzeigename", KiHilfe.Text(feld.Anzeigename),
                        "typ", KiHilfe.Text(Typname(feld.Typ)),
                        "einheit", KiHilfe.Text(feld.Einheit),
                        "leer_erlaubt", feld.LeerErlaubt,
                        "leer_regel", KiHilfe.Text(leerregel),
                        "erlaeuterung", KiHilfe.Text(feld.Erlaeuterung),
                        "wert", KiHilfe.Text(KiDialogZugriff.LiesText(c)),
                        "hilfe_slug", KiHilfe.Text(feld.HilfeSlug),
                        "hilfe_tooltip", KiHilfe.Text(hilfe != null ? hilfe.Tooltip : ""),
                        "hilfe_url", KiHilfe.Text(hilfe != null ? hilfe.Url : "")));

                    string satz =
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.Erklaert,
                                      feld.Anzeigename, bezug.Eintrag.Anzeigename) +
                        " (" + Typname(feld.Typ) +
                        (feld.Einheit.Length > 0 ? ", " + feld.Einheit : "") + ") " +
                        feld.Erlaeuterung + " " + leerregel;

                    if (hilfe != null && hilfe.Tooltip.Length > 0)
                        satz += " " + hilfe.Tooltip;

                    return KiErgebnis.Ok(satz, zeilen, anzahl: 1);
                });
        }

        // =====================================================================
        // feld_setzen
        // =====================================================================

        /// <summary>
        /// Traegt in GENAU EIN Feld einen Wert ein. Andockpunkt
        /// <c>KiDialogZugriff.Setze</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// UMKEHRBAR: Der bisherige Text wird VOR der Aenderung auf dem UI-Thread gelesen
        /// und steht in der Vorschau („Feld · alt → neu") und im Ergebnis. Damit laesst er
        /// sich als neuer, ebenfalls bestaetigungspflichtiger Aufruf zurueckschreiben
        /// (Fachkonzept 4.4, Punkt 3).
        /// </para>
        /// <para>
        /// <b>Warum <c>wert</c> Pflicht ist und ein Feld sich nicht leeren laesst.</b>
        /// <c>KiPruefung</c> behandelt einen leeren Text wie einen FEHLENDEN Parameter
        /// (<c>KiKern\KiPruefung.cs:90</c>). Ein optionaler <c>wert</c> koennte deshalb
        /// „ausdruecklich leeren" und „vergessen anzugeben" nicht unterscheiden - ein
        /// vergessener Parameter wuerde stillschweigend zum Loeschen des Feldinhalts. Bis
        /// es dafuer einen eigenen, sichtbar benannten Weg gibt, wird nur gesetzt.
        /// </para>
        /// </remarks>
        internal static KiAktion FeldSetzen()
        {
            return new KiAktion(
                name: "feld_setzen",
                zweck: KiAktionsTexte.ZweckFeldSetzen,
                titel: KiAktionsTexte.TitelFeldSetzen,
                beispiel: KiAktionsTexte.BeispielFeldSetzen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "KiDialogZugriff.Setze",
                formularaktion: true,
                // KEIN Sicherungspunkt (Festlegung Paket F4): Diese Aktion setzt
                // TextBox.Text bzw. CheckBox.Checked und beruehrt die Datenbank nicht.
                // Eine 90-MB-Kopie sicherte hier einen Zustand, den die Aktion gar nicht
                // verlassen kann; in die Datenbank kommt der Wert erst durch den
                // Aktionsknopf der Maske - und dessen Aktion bringt ihren Sicherungspunkt
                // selbst mit (siehe dialog_aktion_ausfuehren).
                datenbankwirksam: false,
                umkehrbar: true,
                wirkung: KiAktionsTexte.WirkungFeldSetzen,
                parameter: new[] { MaskeParameter(), FeldParameter(), WertParameter() },
                vorbedingung: a => EinzelfeldGrund(a),
                vorschau: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
                    KiDialogFeld feld = bezug.Eintrag.FindeFeld(a.Text("feld"));
                    Control c = KiDialogZugriff.Aufloesen(bezug.Maske, feld.Controlpfad);

                    return KiFeldBlock.Felder(bezug.Eintrag.Anzeigename, new[]
                    {
                        new KiFeldAenderung(feld.Anzeigename,
                                            KiDialogZugriff.LiesText(c), a.Text("wert"))
                    });
                },
                ausfuehren: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
                    if (!bezug.Ok) return KiErgebnis.Abgelehnt(bezug.Grund);

                    KiDialogFeld feld = bezug.Eintrag.FindeFeld(a.Text("feld"));
                    if (feld == null)
                        return KiErgebnis.Abgelehnt(FeldGrund(bezug, a.Text("feld")));

                    Control c = KiDialogZugriff.Aufloesen(bezug.Maske, feld.Controlpfad);
                    string neu = a.Text("wert");
                    string alt = KiDialogZugriff.LiesText(c);

                    string grund = KiDialogZugriff.Setze(feld, c, neu);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "maske", bezug.Eintrag.Maskenname,
                        "feld", feld.Name,
                        "wert_vorher", KiHilfe.Text(alt),
                        "wert_nachher", KiHilfe.Text(KiDialogZugriff.LiesText(c))));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FeldGesetzt,
                                      feld.Anzeigename, Sichtbar(neu), Sichtbar(alt)),
                        zeilen, anzahl: 1);
                });
        }

        // =====================================================================
        // formular_ausfuellen
        // =====================================================================

        /// <summary>
        /// Traegt in mehrere Felder Werte ein - als EIN bestaetigter Block. Andockpunkt
        /// <c>KiDialogZugriff.Setze</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ein Textparameter „feld=wert; feld=wert" und kein Objekt.</b>
        /// <see cref="KiParameterTyp"/> laesst nur Primitive und Zahlenlisten zu
        /// (Fachkonzept 3.2: „Alles Zusammengesetzte gehoert nicht in einen Modellaufruf");
        /// ein Objektparameter waere eine Erweiterung des Kerns fuer genau eine Aktion. Der
        /// Bestand loest dieselbe Aufgabe schon so: <c>KiHilfe.ProjektIds</c> nimmt mehrere
        /// Projekte als eine mit Semikolon getrennte Aufzaehlung entgegen
        /// (<c>KiAktionen.cs:243</c>). Diese Aktion folgt demselben Muster.
        /// </para>
        /// <para>
        /// <b>Ein Block, ein Klick.</b> Alle Aenderungen stehen in EINEM Vorschaublock und
        /// werden mit EINER Bestaetigung freigegeben - sonst waere Ausfuellen unbenutzbar
        /// (Fachkonzept 11.5). Gesetzt wird danach in der genannten Reihenfolge; scheitert
        /// ein Feld, melden das Ergebnis und die Meldungsliste, welche Felder standen und
        /// welches nicht. Ein Zuruecksetzen der bereits gesetzten Felder gibt es NICHT: Die
        /// Maske ist kein Transaktionsraum, und ein halb gefuelltes Formular ist fuer den
        /// Anwender sichtbar - anders als ein halb geschriebener Datensatz.
        /// </para>
        /// <para>
        /// <b>Felder, die sich nicht aendern, fallen heraus</b> - der Bestaetigungsblock
        /// soll nur zeigen, was wirklich anders wird. Aendert sich gar nichts, wird schon
        /// die Vorbedingung im Klartext ablehnen: <see cref="KiFeldBlock"/> laesst einen
        /// leeren Block ausdruecklich nicht zu.
        /// </para>
        /// </remarks>
        internal static KiAktion FormularAusfuellen()
        {
            return new KiAktion(
                name: "formular_ausfuellen",
                zweck: KiAktionsTexte.ZweckFormularAusfuellen,
                titel: KiAktionsTexte.TitelFormularAusfuellen,
                beispiel: KiAktionsTexte.BeispielFormularAusfuellen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "KiDialogZugriff.Setze",
                formularaktion: true,
                // KEIN Sicherungspunkt - Begruendung wie bei feld_setzen: Es werden
                // ausschliesslich Eingabefelder der offenen Maske gefuellt.
                datenbankwirksam: false,
                umkehrbar: true,
                wirkung: KiAktionsTexte.WirkungFormularAusfuellen,
                parameter: new[] { MaskeParameter(), WerteParameter() },
                vorbedingung: a => MehrfeldGrund(a),
                vorschau: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
                    var aenderungen = new List<KiFeldAenderung>();
                    Sammle(bezug, a.Text("werte"), aenderungen);
                    return KiFeldBlock.Felder(bezug.Eintrag.Anzeigename, aenderungen);
                },
                ausfuehren: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
                    if (!bezug.Ok) return KiErgebnis.Abgelehnt(bezug.Grund);

                    var namen = new List<string>();
                    var werte = new List<string>();
                    string grund = Zerlegen(a.Text("werte"), namen, werte);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    var zeilen = KiHilfe.Liste();
                    var meldungen = new List<string>();
                    int gesetzt = 0;

                    for (int i = 0; i < namen.Count; i++)
                    {
                        KiDialogFeld feld = bezug.Eintrag.FindeFeld(namen[i]);
                        if (feld == null)
                        {
                            meldungen.Add(FeldGrund(bezug, namen[i]));
                            continue;
                        }

                        Control c = KiDialogZugriff.Aufloesen(bezug.Maske, feld.Controlpfad);
                        string alt = KiDialogZugriff.LiesText(c);

                        string hindernis = KiDialogZugriff.Setze(feld, c, werte[i]);
                        if (hindernis != null)
                        {
                            meldungen.Add(hindernis);
                            continue;
                        }

                        gesetzt++;
                        zeilen.Add(KiHilfe.Zeile(
                            "maske", bezug.Eintrag.Maskenname,
                            "feld", feld.Name,
                            "wert_vorher", KiHilfe.Text(alt),
                            "wert_nachher", KiHilfe.Text(KiDialogZugriff.LiesText(c))));
                    }

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FelderGesetzt,
                                      gesetzt, bezug.Eintrag.Anzeigename),
                        zeilen, anzahl: gesetzt);

                    return e.MitMeldungen(meldungen);
                });
        }

        // =====================================================================
        // dialog_aktion_ausfuehren
        // =====================================================================

        /// <summary>
        /// Loest einen Knopf der Positivliste aus. Andockpunkt
        /// <c>KiDialogZugriff.Ausloesen</c> (<c>Button.PerformClick</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// NICHT umkehrbar: Was der Knopf ausloest, gehoert der Maske - ein Speichern
        /// schreibt in die Datenbank, ein Abbrechen schliesst das Fenster. Der Assistent
        /// kennt weder den Vorzustand noch einen Weg zurueck. Das sagt die Bestaetigung so.
        /// </para>
        /// <para>
        /// <b>Das Ergebnis ist die Meldung des Bestands - soweit sie greifbar ist.</b>
        /// Laeuft die Aktion in <c>DataRepository.EngineModus()</c> (das tut sie, der
        /// Ausfuehrer klammert jeden Lauf so ein), landen Datenbankmeldungen in der stillen
        /// Sammlung und holt sie <c>StilleFehlerAbholen</c> unmittelbar danach ins
        /// Ergebnis (<c>KiAusfuehrer.cs:599</c>). Die Eingabepruefung der Masken meldet
        /// aber ueber <c>MessageBox</c> und nicht ueber das <c>DataRepository</c> - sie
        /// erscheint also als eigener Dialog vor dem Anwender. Das Ergebnis sagt deshalb
        /// ausdruecklich, dass die Maske selbst meldet, statt Vollstaendigkeit
        /// vorzutaeuschen.
        /// </para>
        /// </remarks>
        internal static KiAktion DialogAktionAusfuehren()
        {
            return new KiAktion(
                name: "dialog_aktion_ausfuehren",
                zweck: KiAktionsTexte.ZweckDialogAktion,
                titel: KiAktionsTexte.TitelDialogAktion,
                beispiel: KiAktionsTexte.BeispielDialogAktion,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "Button.PerformClick",
                formularaktion: true,
                // MIT Sicherungspunkt (Festlegung Paket F4) - ausdruecklich gesetzt, obwohl
                // es die Vorgabe ist: Diese Aktion ist der Grund, warum das Kennzeichen
                // ueberhaupt je Aktion und nicht pauschal fuer alle Formularaktionen
                // entschieden wird. Der ausgeloeste Knopf laeuft durch die Bestandslogik der
                // Maske und schreibt dabei in die Datenbank (btn_Speichern ->
                // InitDatensatzUpdate -> Insert/Update). Vor genau dem gehoert die Kopie.
                datenbankwirksam: true,
                umkehrbar: false,
                wirkung: KiAktionsTexte.WirkungDialogAktion,
                parameter: new[] { MaskeParameter(), KnopfParameter() },
                vorbedingung: a => KnopfGrundVoll(a),
                vorschau: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
                    KiDialogKnopf knopf = bezug.Eintrag.FindeKnopf(a.Text("knopf"));
                    return KiFeldBlock.Knopf(bezug.Eintrag.Anzeigename, knopf.Anzeigename);
                },
                ausfuehren: a =>
                {
                    KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
                    if (!bezug.Ok) return KiErgebnis.Abgelehnt(bezug.Grund);

                    KiDialogKnopf knopf = bezug.Eintrag.FindeKnopf(a.Text("knopf"));
                    if (knopf == null)
                        return KiErgebnis.Abgelehnt(KnopfGrund(bezug, a.Text("knopf")));

                    // Anzeigenamen VOR dem Klick festhalten: „Abbrechen" schliesst die
                    // Maske, danach ist ueber sie nichts mehr zu erfahren.
                    string maskenname = bezug.Eintrag.Anzeigename;

                    Control c = KiDialogZugriff.Aufloesen(bezug.Maske, knopf.Controlpfad);
                    string grund = KiDialogZugriff.Ausloesen(knopf, c);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "maske", bezug.Eintrag.Maskenname,
                        "knopf", knopf.Name));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.KnopfAusgeloest,
                                      knopf.Anzeigename, maskenname),
                        zeilen, anzahl: 1)
                        .MitMeldungen(new[] { KiDialogTexte.KnopfHinweis });
                });
        }

        // =====================================================================
        // Parameter
        // =====================================================================

        /// <summary>
        /// Der Maskenparameter - bei allen fuenf Aktionen OPTIONAL: ohne Angabe gilt die
        /// gerade offene Katalogmaske (Fachkonzept 11.4).
        /// </summary>
        private static KiParameter MaskeParameter()
        {
            return new KiParameter("maske", KiParameterTyp.Text,
                                   KiAktionsTexte.ErlMaske,
                                   pflicht: false,
                                   anzeigename: KiAktionsTexte.MaskeName,
                                   maxLaenge: KiDialog.MaxMaskenname);
        }

        private static KiParameter FeldParameter()
        {
            return new KiParameter("feld", KiParameterTyp.Text,
                                   KiAktionsTexte.ErlFeld,
                                   anzeigename: KiAktionsTexte.FeldName,
                                   maxLaenge: KiName.MaxLaenge);
        }

        private static KiParameter WertParameter()
        {
            return new KiParameter("wert", KiParameterTyp.Text,
                                   KiAktionsTexte.ErlWert,
                                   anzeigename: KiAktionsTexte.WertName,
                                   maxLaenge: 400);
        }

        private static KiParameter WerteParameter()
        {
            return new KiParameter("werte", KiParameterTyp.Text,
                                   KiAktionsTexte.ErlWerte,
                                   anzeigename: KiAktionsTexte.WerteName,
                                   maxLaenge: 2000);
        }

        private static KiParameter KnopfParameter()
        {
            return new KiParameter("knopf", KiParameterTyp.Text,
                                   KiAktionsTexte.ErlKnopf,
                                   anzeigename: KiAktionsTexte.KnopfName,
                                   maxLaenge: KiName.MaxLaenge);
        }

        // =====================================================================
        // Vorbedingungen
        // =====================================================================

        /// <summary>
        /// Vorbedingung von <c>feld_setzen</c>: Maske bedienbar, Feld deklariert, Control
        /// da und setzbar, Wert zum Feld passend.
        /// </summary>
        /// <remarks>
        /// Die Vorbedingung laeuft VOR der Vorschau (<c>KiAusfuehrer.VorschauLauf</c>).
        /// Deshalb darf die Vorschau anschliessend ohne weitere Pruefung auf Katalogeintrag
        /// und Control zugreifen - das ist kein blindes Vertrauen, sondern die Reihenfolge
        /// der Bestaetigungsschicht (Fachkonzept 3.5).
        /// </remarks>
        private static string EinzelfeldGrund(KiAufruf a)
        {
            KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
            if (!bezug.Ok) return bezug.Grund;

            KiDialogFeld feld = bezug.Eintrag.FindeFeld(a.Text("feld"));
            if (feld == null) return FeldGrund(bezug, a.Text("feld"));

            Control c = KiDialogZugriff.Aufloesen(bezug.Maske, feld.Controlpfad);
            string grund = KiDialogZugriff.PruefeSetzbar(feld, c);
            if (grund != null) return grund;

            return KiDialogZugriff.PruefeWert(feld, c, a.Text("wert"));
        }

        /// <summary>
        /// Vorbedingung von <c>formular_ausfuellen</c>: Maske bedienbar, Zuweisungen
        /// lesbar, mindestens eine echte Aenderung darunter.
        /// </summary>
        private static string MehrfeldGrund(KiAufruf a)
        {
            KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
            if (!bezug.Ok) return bezug.Grund;

            var namen = new List<string>();
            var werte = new List<string>();
            string grund = Zerlegen(a.Text("werte"), namen, werte);
            if (grund != null) return grund;

            for (int i = 0; i < namen.Count; i++)
            {
                KiDialogFeld feld = bezug.Eintrag.FindeFeld(namen[i]);
                if (feld == null) return FeldGrund(bezug, namen[i]);

                Control c = KiDialogZugriff.Aufloesen(bezug.Maske, feld.Controlpfad);
                grund = KiDialogZugriff.PruefeSetzbar(feld, c);
                if (grund != null) return grund;

                grund = KiDialogZugriff.PruefeWert(feld, c, werte[i]);
                if (grund != null) return grund;
            }

            var aenderungen = new List<KiFeldAenderung>();
            Sammle(bezug, a.Text("werte"), aenderungen);
            if (aenderungen.Count == 0)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.OhneAenderung,
                                     bezug.Eintrag.Anzeigename);

            return null;
        }

        /// <summary>Vorbedingung von <c>dialog_aktion_ausfuehren</c>.</summary>
        private static string KnopfGrundVoll(KiAufruf a)
        {
            KiDialogZugriff.Bezug bezug = KiDialogZugriff.Aufloesen(a.Text("maske"), true);
            if (!bezug.Ok) return bezug.Grund;

            KiDialogKnopf knopf = bezug.Eintrag.FindeKnopf(a.Text("knopf"));
            if (knopf == null) return KnopfGrund(bezug, a.Text("knopf"));

            Control c = KiDialogZugriff.Aufloesen(bezug.Maske, knopf.Controlpfad);
            return KiDialogZugriff.PruefeKnopf(knopf, c);
        }

        // =====================================================================
        // Hilfen
        // =====================================================================

        /// <summary>
        /// Zerlegt „feld=wert; feld=wert" in zwei gleichlange Listen.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>.</returns>
        /// <remarks>
        /// Getrennt wird beim ERSTEN Gleichheitszeichen einer Zuweisung; alles danach ist
        /// Wert. Ein Wert darf damit „=" enthalten, aber kein Semikolon - das trennt die
        /// Zuweisungen. Fuer Zahlen- und Bezeichnungsfelder der vier Startmasken reicht
        /// das; ein Feldinhalt mit Semikolon gehoert in <c>feld_setzen</c>, das den Wert
        /// unzerlegt entgegennimmt.
        /// </remarks>
        private static string Zerlegen(string werte, List<string> namen, List<string> inhalte)
        {
            foreach (string teil in (werte ?? "").Split(ZUWEISUNGSTRENNER))
            {
                string zuweisung = teil.Trim();
                if (zuweisung.Length == 0) continue;

                int gleich = zuweisung.IndexOf(ZUWEISUNGSZEICHEN);
                if (gleich <= 0)
                    return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.WerteFormat,
                                         zuweisung);

                string name = zuweisung.Substring(0, gleich).Trim();
                string inhalt = zuweisung.Substring(gleich + 1).Trim();

                if (namen.Contains(name))
                    return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.WerteDoppelt, name);

                namen.Add(name);
                inhalte.Add(inhalt);
            }

            if (namen.Count == 0) return KiDialogTexte.WerteLeer;
            return null;
        }

        /// <summary>
        /// Sammelt die ECHTEN Aenderungen fuer den Vorschaublock - Felder, die den Wert
        /// schon tragen, bleiben draussen.
        /// </summary>
        private static void Sammle(KiDialogZugriff.Bezug bezug, string werte,
                                   List<KiFeldAenderung> ziel)
        {
            var namen = new List<string>();
            var inhalte = new List<string>();
            if (Zerlegen(werte, namen, inhalte) != null) return;

            for (int i = 0; i < namen.Count; i++)
            {
                KiDialogFeld feld = bezug.Eintrag.FindeFeld(namen[i]);
                if (feld == null) continue;

                Control c = KiDialogZugriff.Aufloesen(bezug.Maske, feld.Controlpfad);
                var aenderung = new KiFeldAenderung(feld.Anzeigename,
                                                    KiDialogZugriff.LiesText(c), inhalte[i]);
                if (aenderung.IstAenderung) ziel.Add(aenderung);
            }
        }

        /// <summary>Klartextgrund fuer ein nicht deklariertes Feld - nennt, was es gibt.</summary>
        private static string FeldGrund(KiDialogZugriff.Bezug bezug, string feld)
        {
            return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FeldUnbekannt,
                                 feld ?? "", bezug.Eintrag.Anzeigename,
                                 KiDialogZugriff.Aufzaehlen(bezug.Eintrag.Feldnamen()));
        }

        /// <summary>Klartextgrund fuer einen nicht freigegebenen Knopf.</summary>
        private static string KnopfGrund(KiDialogZugriff.Bezug bezug, string knopf)
        {
            return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.KnopfUnbekannt,
                                 knopf ?? "", bezug.Eintrag.Anzeigename,
                                 KiDialogZugriff.Aufzaehlen(bezug.Eintrag.Knopfnamen()));
        }

        /// <summary>Der Hilfeeintrag zum Slug des Feldes; <c>null</c>, wenn keiner da ist.</summary>
        private static HelpEntry Hilfetext(KiDialogFeld feld)
        {
            if (!feld.HatHilfe) return null;
            try
            {
                WikiHelpCatalog katalog = WikiHelpCatalog.Aktueller;
                return katalog != null ? katalog.Get(feld.HilfeSlug) : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Die Feldart im Klartext.</summary>
        private static string Typname(KiParameterTyp typ)
        {
            switch (typ)
            {
                case KiParameterTyp.Ganzzahl: return KiDialogTexte.TypGanzzahl;
                case KiParameterTyp.Zahl: return KiDialogTexte.TypZahl;
                case KiParameterTyp.Wahrheitswert: return KiDialogTexte.TypWahrheit;
                case KiParameterTyp.Aufzaehlung: return KiDialogTexte.TypAuswahl;
                default: return KiDialogTexte.TypText;
            }
        }

        /// <summary>Ein leerer Wert wird benannt, nicht verschwiegen.</summary>
        private static string Sichtbar(string text)
        {
            return string.IsNullOrEmpty(text) ? KiDialogTexte.KeinWert : text;
        }
    }
}
