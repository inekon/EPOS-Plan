using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Aktionen am OFFENEN DIALOG (Fachkonzept 11.4, Konzept „Der Hilfe-Assistent im
    /// Dialog" 3.4 / Weg 5, Etappe S3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sieben Aktionen, drei Gattungen.</b> <c>dialog_lesen</c>,
    /// <c>dialog_parameter_erklaeren</c> und <c>dialog_oeffnen</c> gehoeren zu
    /// <see cref="Schutzstufe.Lesen"/>: Sie fassen keine Daten an (auch das Oeffnen einer
    /// Maske nicht — die Verantwortung geht mit dem Oeffnen an den Anwender ueber,
    /// Fachkonzept 4.1). <c>feld_setzen</c>, <c>formular_ausfuellen</c> und
    /// <c>dialog_aktion_ausfuehren</c> sind FORMULARAKTIONEN der Stufe 2: Sie wirken in
    /// die offene Maske. <c>dialog_speichern</c> ist die einzige Aktion dieser Datei, die
    /// in die DATENBANK wirkt — sie traegt deshalb den Sicherungspunkt (KI‑D‑Q4).
    /// </para>
    /// <para>
    /// <b>Seit Auftrag #201 laeuft ALLES ueber die MASKENBRUECKE.</b> Bis dahin loeste
    /// <c>KiDialogZugriff</c> Maskennamen ueber <c>Application.OpenForms</c> und Felder
    /// ueber <c>FindControlRecursive</c> auf. Der Weg war seit iU9 tot: Die vier Masken
    /// sind Razor-Komponenten und stehen in keiner <c>Form</c> mehr. An seiner Stelle
    /// meldet der offene Dialog seine Feldzugaenge selbst an
    /// (<see cref="KiMaskenbruecke"/>, Stufe S2) — und seit S3 dazu seine
    /// <see cref="KiMaskenhaken"/>: Auffrischen, Plausibilitaetspruefung, Schreibschutz,
    /// Speicherweg. Damit steht derselbe Weg auf Windows UND auf iOS.
    /// </para>
    /// <para>
    /// <b>Keine zweite Eingabepruefung — aber eine ECHTE.</b> Gesetzt wird ueber dieselbe
    /// Eigenschaft, an der auch das Eingabefeld haengt; die Umsetzung Text → Wert macht
    /// <see cref="KiFeldwandler"/>, und den fachlichen Befund liefert danach der DIALOG
    /// (<see cref="KiMaskenhaken.Pruefen"/>). Der Assistent ersetzt die Pruefung des
    /// Bestands nicht, er loest sie aus (Umsetzungskonzept 3b, Punkt 4).
    /// </para>
    /// <para>
    /// <b>Die Vorschau kommt aus dem Kern.</b> Der Bestaetigungsblock entsteht in
    /// <see cref="KiFeldBlock"/> aus Katalogtexten und den gelesenen Werten — nie aus
    /// Modelltext. Waere er Modelltext, bestaetigte der Anwender eine Beschreibung, die
    /// mit dem tatsaechlichen Eingriff nichts zu tun haben muss.
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
        /// Nennt Felder, aktuelle Werte und die Wege der offenen Maske. Andockpunkt
        /// <c>KiMaskenbruecke.Lesen</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das ist das „Finden" der Parameter</b> (Auftrag vom 20.08.2026). Ohne diese
        /// Aktion muesste das Modell Feldnamen raten; mit ihr bekommt es genau die Liste,
        /// die auch die Pruefung und die Bestaetigung verwenden - eine Deklaration, drei
        /// Verwendungen.
        /// </para>
        /// <para>
        /// <b>Die KNOEPFE der Deklaration bleiben unbedienbar</b>, und das ist seit
        /// Auftrag #201 kein Zwischenstand mehr, sondern der Befund: Ein Razor-Dialog hat
        /// keinen <c>Button</c>, den man von aussen druecken koennte. Was hinter dem
        /// Speicherknopf steht, meldet der Dialog stattdessen als HAKEN an — und die
        /// Zeile nennt dem Modell den Weg dorthin (<c>dialog_speichern</c>), statt
        /// „geht nicht" ohne Ausweg zu sagen.
        /// </para>
        /// </remarks>
        internal static KiAktion DialogLesen()
        {
            return new KiAktion(
                name: "dialog_lesen",
                zweck: KiAktionsTexte.ZweckDialogLesen,
                titel: KiAktionsTexte.TitelDialogLesen,
                beispiel: KiAktionsTexte.BeispielDialogLesen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KiMaskenbruecke.Lesen",
                parameter: new[] { MaskeParameter() },
                vorbedingung: a => BrueckenGrund(a.Text("maske")),
                ausfuehren: a =>
                {
                    string grund = BrueckenGrund(a.Text("maske"));
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
                    var zeilen = KiHilfe.Liste();

                    foreach (KiFeldwert w in KiMaskenbruecke.Lesen(maske))
                        zeilen.Add(KiHilfe.Zeile(
                            "art", "feld",
                            "name", w.Name,
                            "anzeigename", KiHilfe.Text(w.Anzeigename),
                            "typ", w.Typ.ToString(),
                            "einheit", KiHilfe.Text(w.Einheit),
                            "leer_erlaubt", w.LeerErlaubt,
                            "wert", KiHilfe.Text(w.Text),
                            "bedienbar", w.Setzbar,
                            "hinweis", KiHilfe.Text(w.Setzbar ? null : KiDialogTexte.NichtSetzbar)));

                    foreach (KiDialogKnopf k in eintrag.Knoepfe)
                        zeilen.Add(KiHilfe.Zeile(
                            "art", "knopf",
                            "name", k.Name,
                            "anzeigename", KiHilfe.Text(k.Anzeigename),
                            "typ", "",
                            "einheit", "",
                            "leer_erlaubt", false,
                            "wert", "",
                            "bedienbar", false,
                            "hinweis", KiHilfe.Text(haken.Speichern != null
                                                        ? MyResource.Resource.KI_AKTION_KNOPF_UEBER_SPEICHERN
                                                        : KiDialogTexte.KnopfSpaeter)));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.Gelesen,
                                      eintrag.Anzeigename,
                                      eintrag.Felder.Count, eintrag.Knoepfe.Count),
                        zeilen);
                });
        }

        // =====================================================================
        // dialog_parameter_erklaeren
        // =====================================================================

        /// <summary>
        /// Erklaert ein Feld: Anzeigename, Art, Einheit, Leer-Regel, Erlaeuterung - und,
        /// wo ein Hilfe-Slug deklariert ist, den Hilfetext der Plattform
        /// (<see cref="KiFeldhilfe"/>).
        /// </summary>
        /// <remarks>
        /// <b>Der Hilfetext ist ein ZUSATZ, keine Bedingung.</b> Heute deklariert kein
        /// Katalogfeld einen Slug (Begruendung in <see cref="KiDialoge"/>: die Zuordnung
        /// haengt an <c>help_mapping.txt</c>, die es im Baum nicht gibt). Die Aktion
        /// antwortet deshalb aus der Katalog-Erlaeuterung - und liefert den Hilfeartikel
        /// zusaetzlich, sobald ein Slug dazukommt.
        /// </remarks>
        internal static KiAktion DialogParameterErklaeren()
        {
            return new KiAktion(
                name: "dialog_parameter_erklaeren",
                zweck: KiAktionsTexte.ZweckDialogErklaeren,
                titel: KiAktionsTexte.TitelDialogErklaeren,
                beispiel: KiAktionsTexte.BeispielDialogErklaeren,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KiDialogKatalog / KiFeldhilfe",
                parameter: new[] { MaskeParameter(), FeldParameter() },
                vorbedingung: a =>
                {
                    string grund = BrueckenGrund(a.Text("maske"));
                    if (grund != null) return grund;
                    return FeldzugangGrund(a.Text("maske"), a.Text("feld"));
                },
                ausfuehren: a =>
                {
                    string grund = BrueckenGrund(a.Text("maske"));
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    string maske = Maskenschluessel(a.Text("maske"));
                    KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));
                    if (zugang == null)
                        return KiErgebnis.Abgelehnt(FeldzugangGrund(a.Text("maske"), a.Text("feld")));

                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiDialogFeld feld = zugang.Feld;
                    KiFeldhilfeEintrag hilfe = KiFeldhilfe.Fuer(feld.HilfeSlug);
                    string leerregel = feld.LeerErlaubt
                        ? KiDialogTexte.LeerErlaubt
                        : KiDialogTexte.LeerPflicht;

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "name", feld.Name,
                        "anzeigename", KiHilfe.Text(feld.Anzeigename),
                        "typ", KiHilfe.Text(Typname(feld.Typ)),
                        "einheit", KiHilfe.Text(feld.Einheit),
                        "leer_erlaubt", feld.LeerErlaubt,
                        "leer_regel", KiHilfe.Text(leerregel),
                        "erlaeuterung", KiHilfe.Text(feld.Erlaeuterung),
                        "wert", KiHilfe.Text(Feldtext(zugang)),
                        "bedienbar", zugang.Setzbar,
                        "hilfe_slug", KiHilfe.Text(feld.HilfeSlug),
                        "hilfe_tooltip", KiHilfe.Text(hilfe != null ? hilfe.Kurztext : ""),
                        "hilfe_url", KiHilfe.Text(hilfe != null ? hilfe.Adresse : "")));

                    string satz =
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.Erklaert,
                                      feld.Anzeigename, eintrag.Anzeigename) +
                        " (" + Typname(feld.Typ) +
                        (feld.Einheit.Length > 0 ? ", " + feld.Einheit : "") + ") " +
                        feld.Erlaeuterung + " " + leerregel;

                    if (hilfe != null && hilfe.Kurztext.Length > 0)
                        satz += " " + hilfe.Kurztext;

                    return KiErgebnis.Ok(satz, zeilen, anzahl: 1);
                });
        }

        // =====================================================================
        // dialog_oeffnen  (Auftrag #201, Punkt 3)
        // =====================================================================

        /// <summary>
        /// Oeffnet eine Maske des Dialogkatalogs ueber <c>Dienste.Navigation</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Stufe 1, obwohl der Anwender danach alles aendern kann</b> — so steht es im
        /// Fachkonzept 4.1: „Die Verantwortung geht mit dem Oeffnen an ihn ueber." Was die
        /// Maske beim Oeffnen SELBST schriebe, waere der Ausschlussgrund; keine der fuenf
        /// Katalogmasken tut das.
        /// </para>
        /// <para>
        /// <b>Die Zuordnung ist DATEN</b> (<see cref="KiMaskenziele"/>) und keine
        /// Fallunterscheidung; ein Waechter haelt sie gegen den Katalog. Wohin der
        /// Schluessel dann fuehrt, entscheidet die Plattform: Unter Windows die
        /// Navigationstabelle der Huelle, auf iOS die <c>AppWurzel</c>.
        /// </para>
        /// <para>
        /// <b>Ein offener modaler Dialog sperrt</b> — das erledigt der Ausfuehrer fuer
        /// jede Aktion, die keine Formularaktion ist (Fachkonzept 3.4, Pflicht 2). Diese
        /// hier ist ausdruecklich KEINE: Sie oeffnet ein Fenster, und genau davor schuetzt
        /// die Sperre.
        /// </para>
        /// </remarks>
        internal static KiAktion DialogOeffnen()
        {
            return new KiAktion(
                name: "dialog_oeffnen",
                zweck: KiAktionsTexte.ZweckDialogOeffnen,
                titel: KiAktionsTexte.TitelDialogOeffnen,
                beispiel: KiAktionsTexte.BeispielDialogOeffnen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "Dienste.Navigation.OeffneMaske",
                parameter: new[] { MaskePflichtParameter() },
                vorbedingung: a => ZielGrund(a.Text("maske")),
                ausfuehren: a =>
                {
                    string grund = ZielGrund(a.Text("maske"));
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    KiDialog eintrag = KiDialoge.Katalog.Finde(a.Text("maske").Trim());
                    string ziel = KiMaskenziele.Ziel(eintrag.Maskenname);

                    bool offen;
                    try { offen = Dienste.Navigation.OeffneMaske(ziel); }
                    catch (Exception ex)
                    {
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AKTION_OEFFNEN_FEHLER,
                                          eintrag.Anzeigename, ex.Message));
                    }

                    if (!offen)
                        return KiErgebnis.Abgelehnt(
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AKTION_OEFFNEN_KEIN_WEG,
                                          eintrag.Anzeigename, ziel));

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "maske", eintrag.Maskenname,
                        "anzeigename", KiHilfe.Text(eintrag.Anzeigename),
                        "ziel", ziel));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_AKTION_OEFFNEN_OK, eintrag.Anzeigename),
                        zeilen, anzahl: 1);
                });
        }

        // =====================================================================
        // feld_setzen
        // =====================================================================

        /// <summary>
        /// Traegt in GENAU EIN Feld der offenen Maske einen Wert ein. Andockpunkt
        /// <c>KiFeldzugang.Setzen</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// UMKEHRBAR: Der bisherige Wert wird VOR der Aenderung gelesen und steht in der
        /// Vorschau („Feld · alt → neu") und im Ergebnis. Damit laesst er sich als neuer,
        /// ebenfalls bestaetigungspflichtiger Aufruf zurueckschreiben (Fachkonzept 4.4,
        /// Punkt 3).
        /// </para>
        /// <para>
        /// <b>Kein Sicherungspunkt</b> (Festlegung Paket F4): Die Aktion schreibt in das
        /// Daten-Objekt des offenen Dialogs und beruehrt die Datenbank nie. In die
        /// Datenbank kommt der Wert erst durch <c>dialog_speichern</c> — und die Aktion
        /// bringt ihren Sicherungspunkt selbst mit.
        /// </para>
        /// <para>
        /// <b>Warum <c>wert</c> Pflicht ist und ein Feld sich nicht leeren laesst.</b>
        /// <see cref="KiPruefung"/> behandelt einen leeren Text wie einen FEHLENDEN
        /// Parameter (<c>KiKern\KiPruefung.cs:90</c>). Ein optionaler <c>wert</c> koennte
        /// deshalb „ausdruecklich leeren" und „vergessen anzugeben" nicht unterscheiden.
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
                andockpunkt: "KiFeldzugang.Setzen",
                formularaktion: true,
                datenbankwirksam: false,
                umkehrbar: true,
                wirkung: KiAktionsTexte.WirkungFeldSetzen,
                parameter: new[] { MaskeParameter(), FeldParameter(), WertParameter() },
                vorbedingung: a => EinzelfeldGrund(a),
                vorschau: a =>
                {
                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));

                    return KiFeldBlock.Felder(eintrag.Anzeigename, new[]
                    {
                        new KiFeldAenderung(zugang.Feld.Anzeigename,
                                            Feldtext(zugang), a.Text("wert"))
                    });
                },
                ausfuehren: a =>
                {
                    string grund = EinzelfeldGrund(a);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
                    KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));

                    string alt = Feldtext(zugang);
                    string hindernis = Setze(zugang, a.Text("wert"));
                    if (hindernis != null) return KiErgebnis.Abgelehnt(hindernis);

                    haken.Auffrischung();

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "maske", eintrag.Maskenname,
                        "feld", zugang.Name,
                        "wert_vorher", KiHilfe.Text(alt),
                        "wert_nachher", KiHilfe.Text(Feldtext(zugang))));

                    KiErgebnis ergebnis = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FeldGesetzt,
                                      zugang.Feld.Anzeigename, Sichtbar(a.Text("wert")), Sichtbar(alt)),
                        zeilen, anzahl: 1);

                    return MitBefund(ergebnis, haken);
                });
        }

        // =====================================================================
        // formular_ausfuellen
        // =====================================================================

        /// <summary>
        /// Traegt in mehrere Felder Werte ein - als EIN bestaetigter Block. Andockpunkt
        /// <c>KiFeldzugang.Setzen</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ein Textparameter „feld=wert; feld=wert" und kein Objekt.</b>
        /// <see cref="KiParameterTyp"/> laesst nur Primitive und Zahlenlisten zu
        /// (Fachkonzept 3.2); ein Objektparameter waere eine Erweiterung des Kerns fuer
        /// genau eine Aktion. Das ist zugleich der Weg, auf dem MEHRERE Felder in EINER
        /// Bestaetigung gesetzt werden (Auftrag #201, Punkt 2).
        /// </para>
        /// <para>
        /// <b>Ein Block, ein Klick.</b> Alle Aenderungen stehen in EINEM Vorschaublock und
        /// werden mit EINER Bestaetigung freigegeben. Gesetzt wird danach in der genannten
        /// Reihenfolge; scheitert ein Feld, melden Ergebnis und Meldungsliste, welche
        /// Felder standen und welches nicht. Ein Zuruecksetzen der bereits gesetzten
        /// Felder gibt es NICHT: Die Maske ist kein Transaktionsraum, und ein halb
        /// gefuelltes Formular ist fuer den Anwender sichtbar - anders als ein halb
        /// geschriebener Datensatz.
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
                andockpunkt: "KiFeldzugang.Setzen",
                formularaktion: true,
                datenbankwirksam: false,
                umkehrbar: true,
                wirkung: KiAktionsTexte.WirkungFormularAusfuellen,
                parameter: new[] { MaskeParameter(), WerteParameter() },
                vorbedingung: a => MehrfeldGrund(a),
                vorschau: a =>
                {
                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    var aenderungen = new List<KiFeldAenderung>();
                    Sammle(maske, a.Text("werte"), aenderungen);
                    return KiFeldBlock.Felder(eintrag.Anzeigename, aenderungen);
                },
                ausfuehren: a =>
                {
                    string vorab = MehrfeldGrund(a);
                    if (vorab != null) return KiErgebnis.Abgelehnt(vorab);

                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);

                    var namen = new List<string>();
                    var werte = new List<string>();
                    string grund = Zerlegen(a.Text("werte"), namen, werte);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    var zeilen = KiHilfe.Liste();
                    var meldungen = new List<string>();
                    int gesetzt = 0;

                    for (int i = 0; i < namen.Count; i++)
                    {
                        KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, namen[i]);
                        if (zugang == null)
                        {
                            meldungen.Add(FeldzugangGrund(maske, namen[i]));
                            continue;
                        }

                        string alt = Feldtext(zugang);
                        string hindernis = Setze(zugang, werte[i]);
                        if (hindernis != null)
                        {
                            meldungen.Add(hindernis);
                            continue;
                        }

                        gesetzt++;
                        zeilen.Add(KiHilfe.Zeile(
                            "maske", eintrag.Maskenname,
                            "feld", zugang.Name,
                            "wert_vorher", KiHilfe.Text(alt),
                            "wert_nachher", KiHilfe.Text(Feldtext(zugang))));
                    }

                    haken.Auffrischung();

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FelderGesetzt,
                                      gesetzt, eintrag.Anzeigename),
                        zeilen, anzahl: gesetzt);

                    return MitBefund(e.MitMeldungen(meldungen), haken);
                });
        }

        // =====================================================================
        // dialog_speichern  (Auftrag #201, Punkt 4 - Anwenderentscheid KI-D-Q4)
        // =====================================================================

        /// <summary>
        /// Ruft den Speicherweg der offenen Maske - mit Bestaetigung UND Sicherungspunkt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Anwenderentscheid KI‑D‑Q4:</b> „Beides, Speichern nur mit Bestaetigung und
        /// Sicherungspunkt (datenbankwirksam, Aufgabensteuerung 4.4)." Deshalb ist diese
        /// Aktion ausdruecklich <c>datenbankwirksam</c> — sie ist die EINZIGE der
        /// Dialogaktionen, vor der eine Datenbankkopie entsteht.
        /// </para>
        /// <para>
        /// <b>Sie ist eine FORMULARAKTION</b>, obwohl sie schreibt: Sie verlangt die
        /// offene Maske und muss deshalb an der Modalitaetssperre vorbei (die
        /// Katalogeditoren stehen als Ueberlagerung). Am Riegel aendert das nichts — sie
        /// gehoert zu <see cref="Schutzstufe.Schreiben"/> und braucht denselben Klick wie
        /// jede Schreibaktion.
        /// </para>
        /// <para>
        /// <b>Was sie NICHT tut: die Eingaben pruefen.</b> Das tut der Dialog in seinem
        /// eigenen Speicherweg, genau wie beim Knopfdruck; sein Befund ist das Ergebnis.
        /// Ein zweiter Pruefweg im Kern waere die zweite Wahrheit, die das Konzept
        /// ausschliesst.
        /// </para>
        /// </remarks>
        internal static KiAktion DialogSpeichern()
        {
            return new KiAktion(
                name: "dialog_speichern",
                zweck: KiAktionsTexte.ZweckDialogSpeichern,
                titel: KiAktionsTexte.TitelDialogSpeichern,
                beispiel: KiAktionsTexte.BeispielDialogSpeichern,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "KiMaskenhaken.Speichern",
                formularaktion: true,
                datenbankwirksam: true,
                umkehrbar: false,
                wirkung: KiAktionsTexte.WirkungDialogSpeichern,
                parameter: new[] { MaskeParameter() },
                vorbedingung: a => SpeicherGrund(a),
                vorschau: a =>
                {
                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    return string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.KI_AKTION_SPEICHERN_VORSCHAU,
                                         eintrag.Anzeigename, eintrag.Felder.Count);
                },
                ausfuehren: a =>
                {
                    string grund = SpeicherGrund(a);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);

                    // Der Speicherweg des Dialogs ist asynchron (er schliesst die
                    // Ueberlagerung und meldet an seinen Wirt). Gewartet wird HIER, auf
                    // dem Oberflaechenfaden, auf dem der Ausfuehrer jede Aktion laufen
                    // laesst - eine Fortsetzung auf einem anderen Faden waere genau der
                    // Fall, den DataRepository mit seinem prozessweiten Modus verbietet.
                    KiErgebnis ergebnis;
                    try { ergebnis = haken.Speichern().GetAwaiter().GetResult(); }
                    catch (Exception ex)
                    {
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AKTION_SPEICHERN_FEHLER,
                                          eintrag.Anzeigename, ex.Message));
                    }

                    if (ergebnis == null)
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AKTION_SPEICHERN_FEHLER,
                                          eintrag.Anzeigename, ""));

                    // Der Sicherungspfad gehoert in das Ergebnis (Auftrag #201, Punkt 4):
                    // Der Anwender soll nach dem Schreiben wissen, wohin der Vorzustand
                    // gesichert ist - nicht nur vorher in der Bestaetigung.
                    string pfad = KiSicherungspunkt.Pfad;
                    if (ergebnis.Erfolg && !string.IsNullOrEmpty(pfad))
                        ergebnis = ergebnis.MitMeldungen(new[]
                        {
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AKTION_SPEICHERN_SICHERUNG, pfad)
                        });

                    return ergebnis;
                });
        }

        // =====================================================================
        // dialog_aktion_ausfuehren
        // =====================================================================

        /// <summary>
        /// Loest einen Knopf der Positivliste aus - seit Auftrag #201 die eine Aktion des
        /// Registers OHNE Weg.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum sie bleibt, obwohl sie ablehnt.</b> Aus dem Register wird nichts
        /// geloescht (Fachkonzept 5.4 nennt, was NIE hineingehoert - nicht, was wieder
        /// hinaus soll): Der Werkzeugvertrag des Modells ist stabil, und ein Aufruf, der
        /// ins Leere liefe, waere schlechter als eine benannte Ablehnung, die den Weg
        /// nennt.
        /// </para>
        /// <para>
        /// <b>Warum sie ablehnt.</b> Ihr Andockpunkt war <c>Button.PerformClick</c> - ein
        /// WinForms-Steuerelement. Die fuenf Katalogmasken sind Razor-Komponenten; einen
        /// Knopf „von aussen zu druecken" gibt es dort nicht, und es soll ihn nicht geben
        /// (das waere die Reflexion, die Fachkonzept 3.2 ausschliesst). Was hinter dem
        /// Speicherknopf steht, meldet der Dialog als HAKEN an, und dafuer gibt es
        /// <c>dialog_speichern</c> - mit Bestaetigung UND Sicherungspunkt.
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
                andockpunkt: "KiMaskenhaken.Speichern (abgeloest)",
                formularaktion: true,
                datenbankwirksam: true,
                umkehrbar: false,
                wirkung: KiAktionsTexte.WirkungDialogAktion,
                parameter: new[] { MaskeParameter(), KnopfParameter() },
                vorbedingung: a => KnopfGrundVoll(a),
                vorschau: a =>
                {
                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiDialogKnopf knopf = eintrag?.FindeKnopf(a.Text("knopf"));
                    return KiFeldBlock.Knopf(eintrag == null ? "" : eintrag.Anzeigename,
                                             knopf == null ? "" : knopf.Anzeigename);
                },
                ausfuehren: a => KiErgebnis.Abgelehnt(KnopfGrundVoll(a)
                                                      ?? MyResource.Resource.KI_AKTION_KNOPF_KEIN_WEG));
        }

        // =====================================================================
        // Parameter
        // =====================================================================

        /// <summary>
        /// Der Maskenparameter - bei den Dialogaktionen OPTIONAL: ohne Angabe gilt die
        /// zuletzt angemeldete Maske (Fachkonzept 11.4).
        /// </summary>
        private static KiParameter MaskeParameter()
        {
            return new KiParameter("maske", KiParameterTyp.Text,
                                   KiAktionsTexte.ErlMaske,
                                   pflicht: false,
                                   anzeigename: KiAktionsTexte.MaskeName,
                                   maxLaenge: KiDialog.MaxMaskenname);
        }

        /// <summary>
        /// Der Maskenparameter von <c>dialog_oeffnen</c> - hier PFLICHT: Eine Maske zu
        /// oeffnen, die man nicht genannt hat, ergaebe keinen Sinn.
        /// </summary>
        private static KiParameter MaskePflichtParameter()
        {
            return new KiParameter("maske", KiParameterTyp.Text,
                                   KiAktionsTexte.ErlMaskeOeffnen,
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
        /// Der Maskenschluessel eines Aufrufs: der genannte, sonst die zuletzt
        /// angemeldete Maske.
        /// </summary>
        private static string Maskenschluessel(string genannt)
        {
            string gesucht = (genannt ?? "").Trim();
            return gesucht.Length > 0 ? gesucht : KiMaskenbruecke.AktiveMaske();
        }

        /// <summary>
        /// Warum die Bruecke diese Maske nicht liefern kann; <c>null</c> = sie kann.
        /// </summary>
        /// <remarks>
        /// Die Ablehnung NENNT, was es gibt — dieselbe Regel wie beim alten Weg
        /// (Fachkonzept 11.4): Ein „geht nicht" ohne Liste zwaenge das Modell zum Raten.
        /// </remarks>
        private static string BrueckenGrund(string genannt)
        {
            string gesucht = (genannt ?? "").Trim();

            if (gesucht.Length > 0 && !KiDialoge.Katalog.Kennt(gesucht))
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.MaskeUnbekannt,
                                     gesucht, Aufzaehlen(KiDialoge.Katalog.Maskennamen()));

            string maske = Maskenschluessel(gesucht);

            if (maske.Length == 0 || !KiMaskenbruecke.IstAngemeldet(maske))
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.KeineOffen,
                                     Aufzaehlen(KiDialoge.Katalog.Maskennamen()));

            return null;
        }

        /// <summary>Warum <c>dialog_oeffnen</c> diese Maske nicht kennt; <c>null</c> = es geht.</summary>
        private static string ZielGrund(string genannt)
        {
            string gesucht = (genannt ?? "").Trim();

            if (gesucht.Length == 0 || !KiDialoge.Katalog.Kennt(gesucht))
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.MaskeUnbekannt,
                                     gesucht, Aufzaehlen(KiDialoge.Katalog.Maskennamen()));

            KiDialog eintrag = KiDialoge.Katalog.Finde(gesucht);
            if (!KiMaskenziele.Kennt(eintrag.Maskenname))
                return string.Format(CultureInfo.CurrentCulture,
                                     MyResource.Resource.KI_AKTION_OEFFNEN_OHNE_ZIEL,
                                     eintrag.Anzeigename);

            return null;
        }

        /// <summary>Klartextgrund fuer ein Feld, das die Bruecke nicht fuehrt.</summary>
        private static string FeldzugangGrund(string genannt, string feld)
        {
            string maske = Maskenschluessel(genannt);
            KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);

            return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FeldUnbekannt,
                                 feld ?? "",
                                 eintrag == null ? maske : eintrag.Anzeigename,
                                 Aufzaehlen(eintrag == null
                                                ? Array.Empty<string>()
                                                : eintrag.Feldnamen()));
        }

        /// <summary>
        /// Vorbedingung von <c>feld_setzen</c>: Maske angemeldet, Feld da und setzbar,
        /// kein Lesemodus, kein Schreibschutz, Wert zum Feld passend.
        /// </summary>
        /// <remarks>
        /// Die Vorbedingung laeuft VOR der Vorschau (<c>KiAusfuehrung.VorschauLauf</c>).
        /// Deshalb darf die Vorschau anschliessend ohne weitere Pruefung auf Katalogeintrag
        /// und Feldzugang zugreifen - das ist kein blindes Vertrauen, sondern die
        /// Reihenfolge der Bestaetigungsschicht (Fachkonzept 3.5).
        /// </remarks>
        private static string EinzelfeldGrund(KiAufruf a)
        {
            string grund = BrueckenGrund(a.Text("maske"));
            if (grund != null) return grund;

            string maske = Maskenschluessel(a.Text("maske"));

            grund = SetzbarkeitAllgemein(maske);
            if (grund != null) return grund;

            KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));
            if (zugang == null) return FeldzugangGrund(maske, a.Text("feld"));

            grund = FeldSetzbar(zugang);
            if (grund != null) return grund;

            KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, a.Text("wert"));
            return umsetzung.Ok ? null : umsetzung.Grund;
        }

        /// <summary>
        /// Vorbedingung von <c>formular_ausfuellen</c>: Maske bedienbar, Zuweisungen
        /// lesbar, jedes Feld setzbar, mindestens eine echte Aenderung darunter.
        /// </summary>
        private static string MehrfeldGrund(KiAufruf a)
        {
            string grund = BrueckenGrund(a.Text("maske"));
            if (grund != null) return grund;

            string maske = Maskenschluessel(a.Text("maske"));

            grund = SetzbarkeitAllgemein(maske);
            if (grund != null) return grund;

            var namen = new List<string>();
            var werte = new List<string>();
            grund = Zerlegen(a.Text("werte"), namen, werte);
            if (grund != null) return grund;

            for (int i = 0; i < namen.Count; i++)
            {
                KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, namen[i]);
                if (zugang == null) return FeldzugangGrund(maske, namen[i]);

                grund = FeldSetzbar(zugang);
                if (grund != null) return grund;

                KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, werte[i]);
                if (!umsetzung.Ok) return umsetzung.Grund;
            }

            var aenderungen = new List<KiFeldAenderung>();
            Sammle(maske, a.Text("werte"), aenderungen);
            if (aenderungen.Count == 0)
            {
                KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.OhneAenderung,
                                     eintrag == null ? maske : eintrag.Anzeigename);
            }

            return null;
        }

        /// <summary>Vorbedingung von <c>dialog_speichern</c>.</summary>
        private static string SpeicherGrund(KiAufruf a)
        {
            string grund = BrueckenGrund(a.Text("maske"));
            if (grund != null) return grund;

            string maske = Maskenschluessel(a.Text("maske"));

            grund = SetzbarkeitAllgemein(maske);
            if (grund != null) return grund;

            if (KiMaskenbruecke.Haken(maske).Speichern == null)
            {
                KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                return string.Format(CultureInfo.CurrentCulture,
                                     MyResource.Resource.KI_AKTION_SPEICHERN_KEIN_WEG,
                                     eintrag == null ? maske : eintrag.Anzeigename);
            }

            return null;
        }

        /// <summary>Vorbedingung von <c>dialog_aktion_ausfuehren</c> - sie lehnt immer ab.</summary>
        private static string KnopfGrundVoll(KiAufruf a)
        {
            string grund = BrueckenGrund(a.Text("maske"));
            if (grund != null) return grund;

            string maske = Maskenschluessel(a.Text("maske"));
            KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
            KiDialogKnopf knopf = eintrag?.FindeKnopf(a.Text("knopf"));

            if (knopf == null)
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.KnopfUnbekannt,
                                     a.Text("knopf") ?? "",
                                     eintrag == null ? maske : eintrag.Anzeigename,
                                     Aufzaehlen(eintrag == null
                                                    ? Array.Empty<string>()
                                                    : eintrag.Knopfnamen()));

            return MyResource.Resource.KI_AKTION_KNOPF_KEIN_WEG;
        }

        /// <summary>
        /// Was gegen JEDES Setzen in dieser Maske spricht: Lesemodus der Lizenz (iF30)
        /// und ein schreibgeschuetzter Katalogsatz (<c>ReadOnly</c>, Fachkonzept 4.5).
        /// </summary>
        /// <remarks>
        /// <b>Der Lesemodus wird HIER ein zweites Mal gefragt.</b> Der Ausfuehrer prueft
        /// ihn ohnehin vor jeder Schreibaktion (<c>Schreibvorbedingung</c>); diese Frage
        /// steht trotzdem, weil sie die BESSERE Meldung gibt: Sie nennt die Maske und das
        /// Feld, waehrend die des Ausfuehrers den Lizenzstatus nennt. Beide sind richtig,
        /// und die naeher am Anwender kommt zuerst.
        /// </remarks>
        private static string SetzbarkeitAllgemein(string maske)
        {
            string lesemodus = SimulationLaufCtrl.LesemodusGrund();
            if (lesemodus != null) return lesemodus;

            if (KiMaskenbruecke.Haken(maske).IstSchreibgeschuetzt())
            {
                KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                return string.Format(CultureInfo.CurrentCulture,
                                     MyResource.Resource.KI_FELD_SATZ_GESCHUETZT,
                                     eintrag == null ? maske : eintrag.Anzeigename);
            }

            return null;
        }

        /// <summary>Klartextgrund, warum DIESES Feld nicht setzbar ist; <c>null</c> = es ist es.</summary>
        private static string FeldSetzbar(KiFeldzugang zugang)
        {
            if (zugang.Setzbar) return null;

            return string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.KI_FELD_NICHT_SETZBAR,
                                 zugang.Feld.Anzeigename);
        }

        // =====================================================================
        // Hilfen
        // =====================================================================

        /// <summary>
        /// Setzt EIN Feld ueber die Bruecke. Rueckgabe: Klartextgrund oder <c>null</c>.
        /// </summary>
        private static string Setze(KiFeldzugang zugang, string wert)
        {
            string grund = FeldSetzbar(zugang);
            if (grund != null) return grund;

            KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, wert);
            if (!umsetzung.Ok) return umsetzung.Grund;

            try
            {
                zugang.Setzen(umsetzung.Wert);
                return null;
            }
            catch (Exception ex)
            {
                return string.Format(CultureInfo.CurrentCulture,
                                     MyResource.Resource.KI_FELD_SETZEN_FEHLER,
                                     zugang.Feld.Anzeigename, ex.Message);
            }
        }

        /// <summary>
        /// Haengt den Befund der Dialogpruefung an das Ergebnis - er ist das Urteil DES
        /// DIALOGS ueber den gesetzten Wert (Auftrag #201, Punkt 2).
        /// </summary>
        private static KiErgebnis MitBefund(KiErgebnis ergebnis, KiMaskenhaken haken)
        {
            string befund = haken.Befund();
            if (befund.Length == 0) return ergebnis;

            return ergebnis.MitMeldungen(new[]
            {
                string.Format(CultureInfo.CurrentCulture,
                              MyResource.Resource.KI_FELD_DIALOGBEFUND, befund)
            });
        }

        /// <summary>Der Wert eines Feldes als Anzeigetext; ein werfender Getter ergibt leer.</summary>
        /// <remarks>
        /// Formatiert wird ueber <see cref="KiMaskenbruecke.Anzeigetext"/> — dieselbe
        /// Schreibweise, in der der Feldblock die Werte an das Modell gibt. Zwei
        /// Fassungen desselben Wertes waeren genau die Stelle, an der Vorschau und
        /// Ergebnis auseinanderliefen.
        /// </remarks>
        private static string Feldtext(KiFeldzugang zugang)
        {
            if (zugang == null) return "";

            try { return KiMaskenbruecke.Anzeigetext(zugang.Lesen()); }
            catch (Exception) { return ""; }
        }

        /// <summary>
        /// Zerlegt „feld=wert; feld=wert" in zwei gleichlange Listen.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>.</returns>
        /// <remarks>
        /// Getrennt wird beim ERSTEN Gleichheitszeichen einer Zuweisung; alles danach ist
        /// Wert. Ein Wert darf damit „=" enthalten, aber kein Semikolon - das trennt die
        /// Zuweisungen. Ein Feldinhalt mit Semikolon gehoert in <c>feld_setzen</c>, das
        /// den Wert unzerlegt entgegennimmt.
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
        private static void Sammle(string maske, string werte, List<KiFeldAenderung> ziel)
        {
            var namen = new List<string>();
            var inhalte = new List<string>();
            if (Zerlegen(werte, namen, inhalte) != null) return;

            IReadOnlyList<KiFeldwert> gelesen = KiMaskenbruecke.Lesen(maske);

            for (int i = 0; i < namen.Count; i++)
            {
                KiFeldwert stand = null;
                foreach (KiFeldwert w in gelesen)
                    if (string.Equals(w.Name, namen[i], StringComparison.Ordinal)) { stand = w; break; }

                if (stand == null) continue;

                var aenderung = new KiFeldAenderung(stand.Anzeigename, stand.Text, inhalte[i]);
                if (aenderung.IstAenderung) ziel.Add(aenderung);
            }
        }

        /// <summary>Zaehlt Namen lesbar auf; lange Listen werden gekuerzt.</summary>
        /// <remarks>
        /// Wortgleich aus <c>KiDialogZugriff.Aufzaehlen</c> uebernommen (Auftrag #201) —
        /// jene Klasse ist mit dem Controlweg gefallen, die Klartextregel „nenne, was es
        /// gibt" bleibt.
        /// </remarks>
        internal static string Aufzaehlen(IReadOnlyList<string> namen)
        {
            const int HOECHSTENS = 20;

            if (namen == null || namen.Count == 0) return "";

            var teile = new List<string>();
            for (int i = 0; i < namen.Count && i < HOECHSTENS; i++) teile.Add(namen[i]);

            string text = string.Join(", ", teile);
            if (namen.Count > HOECHSTENS) text += ", ... (" + (namen.Count - HOECHSTENS) + ")";
            return text;
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
