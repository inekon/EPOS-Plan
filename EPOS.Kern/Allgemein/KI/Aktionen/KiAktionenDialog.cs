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
    /// <b>Acht Aktionen, drei Gattungen.</b> <c>dialog_lesen</c>,
    /// <c>dialog_parameter_erklaeren</c> und <c>dialog_oeffnen</c> gehoeren zu
    /// <see cref="Schutzstufe.Lesen"/>: Sie fassen keine Daten an (auch das Oeffnen einer
    /// Maske nicht — die Verantwortung geht mit dem Oeffnen an den Anwender ueber,
    /// Fachkonzept 4.1). <c>feld_setzen</c>, <c>formular_ausfuellen</c>,
    /// <c>reihe_setzen</c> (Zahlenreihen, Welle #458 Stufe 3b) und
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

                    // GEZAEHLT wird, was tatsaechlich gelesen wurde - nicht, was der
                    // Katalog deklariert. Seit es Spalten gibt, sind das zwei Zahlen:
                    // Eine Spaltendeklaration steht einmal im Katalog und liefert so
                    // viele Felder, wie die Maske gerade Zeilen hat.
                    int gelesen = 0;

                    foreach (KiFeldwert w in KiMaskenbruecke.Lesen(maske))
                    {
                        gelesen++;

                        // Eine WAHL zeigt beides (KI-F1b): den Text, damit die Antwort
                        // verstaendlich bleibt, den Schluessel, damit das Modell ihn
                        // zurueckgeben kann - und die Eintraege, damit es nicht raten muss.
                        zeilen.Add(KiHilfe.Zeile(
                            "art", "feld",
                            "name", w.Name,
                            "anzeigename", KiHilfe.Text(w.Anzeigename),
                            "typ", w.Typ.ToString(),
                            "einheit", KiHilfe.Text(w.Einheit),
                            "leer_erlaubt", w.LeerErlaubt,
                            "wert", KiHilfe.Text(w.IstWahl ? Wahlwert(w) : w.Text),
                            "eintraege", KiHilfe.Text(KiWahl.Aufzaehlen(w.Eintraege)),
                            "bedienbar", w.Setzbar,
                            "hinweis", KiHilfe.Text(Lesehinweis(w))));
                    }

                    foreach (KiDialogKnopf k in eintrag.Knoepfe)
                        zeilen.Add(KiHilfe.Zeile(
                            "art", "knopf",
                            "name", k.Name,
                            "anzeigename", KiHilfe.Text(k.Anzeigename),
                            "typ", "",
                            "einheit", "",
                            "leer_erlaubt", false,
                            "wert", "",
                            "eintraege", "",
                            "bedienbar", false,
                            "hinweis", KiHilfe.Text(haken.Speichern != null
                                                        ? MyResource.Resource.KI_AKTION_KNOPF_UEBER_SPEICHERN
                                                        : KiDialogTexte.KnopfSpaeter)));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.Gelesen,
                                      eintrag.Anzeigename,
                                      gelesen, eintrag.Knoepfe.Count),
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
                    string grund = BrueckenGrund(a.Text("maske"), a.Text("feld"));
                    if (grund != null) return grund;

                    // Der Grund nur, wenn das Feld FEHLT (Befund der Welle #458 Stufe 3b):
                    // FeldzugangGrund liefert immer einen Satz, und unbedingt gerufen lehnte
                    // die Vorbedingung jede Erklaerung ab - auch die eines vorhandenen Feldes.
                    string maske = Maskenschluessel(a.Text("maske"));
                    return KiMaskenbruecke.Feldzugang(maske, a.Text("feld")) == null
                        ? FeldzugangGrund(a.Text("maske"), a.Text("feld"))
                        : null;
                },
                ausfuehren: a =>
                {
                    string grund = BrueckenGrund(a.Text("maske"), a.Text("feld"));
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

                    // Bei einer WAHL gehoeren die Eintraege in die Erklaerung (KI-F1b):
                    // „welche Werte nimmt das Feld an?" ist bei ihr die ganze Frage.
                    string auswahl = zugang.IstWahl
                        ? KiWahl.Aufzaehlen(zugang.Wahleintraege())
                        : "";

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "name", feld.Name,
                        "anzeigename", KiHilfe.Text(feld.Anzeigename),
                        "typ", KiHilfe.Text(Typname(feld.Typ)),
                        "einheit", KiHilfe.Text(feld.Einheit),
                        "leer_erlaubt", feld.LeerErlaubt,
                        "leer_regel", KiHilfe.Text(leerregel),
                        "erlaeuterung", KiHilfe.Text(feld.Erlaeuterung),
                        "reihe", KiHilfe.Text(feld.IstReihe ? feld.Reihe.Umfang() : ""),
                        "bereich", KiHilfe.Text(KiFeldwandler.Bereichstext(feld)),
                        "wert", KiHilfe.Text(Feldtext(zugang)),
                        "eintraege", KiHilfe.Text(auswahl),
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

                    // Eine ZAHLENREIHE nennt Laenge und Stellen, jedes Zahlenfeld mit
                    // Grenze seinen Bereich (Welle #458 Stufe 3b) - „welche Werte nimmt
                    // das Feld an?" ist bei beiden die halbe Frage.
                    if (feld.IstReihe)
                        satz += " " + string.Format(CultureInfo.CurrentCulture,
                                                    KiDialogTexte.ReiheUmfang, feld.Reihe.Umfang());
                    if (feld.HatBereich)
                        satz += " " + string.Format(CultureInfo.CurrentCulture,
                                                    KiDialogTexte.Bereich, KiFeldwandler.Bereichstext(feld));

                    if (auswahl.Length > 0)
                        satz += " " + string.Format(CultureInfo.CurrentCulture,
                                                    MyResource.Resource.KI_FELD_WAHL_EINTRAEGE,
                                                    auswahl);

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

                    // DAS ARGUMENT (Anwenderentscheid KI-D-Q8): Der Reiter der
                    // Startseite bzw. das Blatt der Ansicht „Berichte und Kosten".
                    // Ohne Argument bleibt der Ruf wie bisher einstellig - die
                    // Navigationstabellen lesen ihr erstes Argument je Schluessel,
                    // und eine leere Zeichenkette waere dort ein Wunsch, der nichts
                    // benennt.
                    string argument = KiMaskenziele.Argument(eintrag.Maskenname);

                    bool offen;
                    try
                    {
                        offen = argument.Length > 0
                                    ? Dienste.Navigation.OeffneMaske(ziel, argument)
                                    : Dienste.Navigation.OeffneMaske(ziel);
                    }
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
                                            Feldtext(zugang), Neutext(zugang, a.Text("wert")))
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
                        "feld_genannt", KiHilfe.Text(a.Text("feld")),
                        "wert_vorher", KiHilfe.Text(alt),
                        "wert_nachher", KiHilfe.Text(Feldtext(zugang))));

                    KiErgebnis ergebnis = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FeldGesetzt,
                                      zugang.Feld.Anzeigename,
                                      Sichtbar(Feldtext(zugang)), Sichtbar(alt)) +
                        Aufloesung(a.Text("feld"), zugang),
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
                    string aufgeloest = "";

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
                        aufgeloest += Aufloesung(namen[i], zugang);
                        zeilen.Add(KiHilfe.Zeile(
                            "maske", eintrag.Maskenname,
                            "feld", zugang.Name,
                            "feld_genannt", KiHilfe.Text(namen[i]),
                            "wert_vorher", KiHilfe.Text(alt),
                            "wert_nachher", KiHilfe.Text(Feldtext(zugang))));
                    }

                    haken.Auffrischung();

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.FelderGesetzt,
                                      gesetzt, eintrag.Anzeigename) + aufgeloest,
                        zeilen, anzahl: gesetzt);

                    return MitBefund(e.MitMeldungen(meldungen), haken);
                });
        }

        // =====================================================================
        // reihe_setzen  (Welle #458 Stufe 3b)
        // =====================================================================

        /// <summary>
        /// Traegt in eine ZAHLENREIHE der offenen Maske eine Liste von Zahlen ein - die
        /// ganze Reihe oder einen Ausschnitt ab einer Stelle. Andockpunkt
        /// <c>KiFeldzugang.Setzen</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum eine eigene Aktion und kein weiterer Weg durch <c>feld_setzen</c>.</b>
        /// Dort ist <c>wert</c> ein Pflichttext (siehe dort, „leeren oder vergessen"), und
        /// <c>formular_ausfuellen</c> trennt seine Zuweisungen mit dem Strichpunkt. Eine
        /// Zahlenliste als Text haette beide Regeln aufgeweicht - und die Laenge der Reihe
        /// liesse sich nicht mehr vor dem Setzen pruefen. Hier ist die Liste ein
        /// typgeprueftes Zahlenfeld (<see cref="KiParameterTyp.ZahlListe"/>), die Stelle
        /// eine Ganzzahl, und die Form der Reihe steht im Katalog.
        /// </para>
        /// <para>
        /// <b>Ein Block, ein Klick, gekuerzt.</b> Die Vorschau zeigt die geaenderten
        /// Stellen „alt → neu" und ihre Zahl (<see cref="KiFeldBlock.Reihe"/>); die
        /// Protokollzeile traegt die volle Liste als Parameter.
        /// </para>
        /// <para>
        /// <b>Kein Sicherungspunkt</b> - dieselbe Festlegung wie bei <c>feld_setzen</c>:
        /// Die Aktion schreibt in den Stand der Maske; in die Datenbank kommt die Reihe
        /// erst mit <c>dialog_speichern</c> oder dem Knopf des Anwenders.
        /// </para>
        /// </remarks>
        internal static KiAktion ReiheSetzen()
        {
            return new KiAktion(
                name: "reihe_setzen",
                zweck: KiAktionsTexte.ZweckReiheSetzen,
                titel: KiAktionsTexte.TitelReiheSetzen,
                beispiel: KiAktionsTexte.BeispielReiheSetzen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "KiFeldzugang.Setzen",
                formularaktion: true,
                datenbankwirksam: false,
                umkehrbar: true,
                wirkung: KiAktionsTexte.WirkungReiheSetzen,
                parameter: new[] { MaskeParameter(), FeldParameter(), ReihenwerteParameter(), AbParameter() },
                vorbedingung: a => ReihenGrund(a),
                vorschau: a =>
                {
                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));
                    KiFeldumsetzung umsetzung = KiFeldwandler.WandleReihe(zugang, a.ZahlListe("werte"), Ab(a));

                    return KiFeldBlock.Reihe(eintrag.Anzeigename, zugang.Feld.Anzeigename, zugang.Feld.Reihe,
                                             KiFeldwandler.Reihenwerte(zugang),
                                             KiZahlenreihe.Werte(umsetzung.Wert),
                                             CultureInfo.CurrentCulture);
                },
                ausfuehren: a =>
                {
                    string grund = ReihenGrund(a);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    string maske = Maskenschluessel(a.Text("maske"));
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
                    KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
                    KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));
                    double[] werte = a.ZahlListe("werte");
                    int ab = Ab(a);

                    IReadOnlyList<double?> alt = KiFeldwandler.Reihenwerte(zugang);
                    KiFeldumsetzung umsetzung = KiFeldwandler.WandleReihe(zugang, werte, ab);
                    if (!umsetzung.Ok) return KiErgebnis.Abgelehnt(umsetzung.Grund);

                    try { zugang.Setzen(umsetzung.Wert); }
                    catch (Exception ex)
                    {
                        return KiErgebnis.Abgelehnt(
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_FELD_SETZEN_FEHLER,
                                          zugang.Feld.Anzeigename, ex.Message));
                    }

                    haken.Auffrischung();

                    IReadOnlyList<double?> neu = KiFeldwandler.Reihenwerte(zugang);
                    int erste = ab <= 0 ? 1 : ab;
                    int letzte = erste + werte.Length - 1;

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "maske", eintrag.Maskenname,
                        "feld", zugang.Name,
                        "feld_genannt", KiHilfe.Text(a.Text("feld")),
                        "ab", erste,
                        "anzahl", werte.Length,
                        "laenge", zugang.Feld.Reihe.Laenge,
                        "wert_vorher", KiHilfe.Text(KiZahlenreihe.Kurz(alt, CultureInfo.CurrentCulture)),
                        "wert_nachher", KiHilfe.Text(KiZahlenreihe.Kurz(neu, CultureInfo.CurrentCulture))));

                    KiErgebnis ergebnis = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ReiheGesetzt,
                                      zugang.Feld.Anzeigename, werte.Length, zugang.Feld.Reihe.Laenge,
                                      zugang.Feld.Reihe.Stellenname(erste),
                                      zugang.Feld.Reihe.Stellenname(letzte)) +
                        Aufloesung(a.Text("feld"), zugang),
                        zeilen, anzahl: werte.Length);

                    return MitBefund(ergebnis, haken);
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

        /// <summary>
        /// Die Zahlen einer Reihe - ein typgeprueftes Zahlenfeld (Welle #458 Stufe 3b).
        /// </summary>
        /// <remarks>
        /// Grenzen stehen hier NICHT: Sie gehoeren dem Eingabefeld der Maske und stehen im
        /// Katalog (<see cref="KiDialogFeld.Min"/>); die Laenge sagt die Form der Reihe.
        /// </remarks>
        private static KiParameter ReihenwerteParameter()
        {
            return new KiParameter("werte", KiParameterTyp.ZahlListe,
                                   KiAktionsTexte.ErlReihenwerte,
                                   anzeigename: KiAktionsTexte.WerteName);
        }

        /// <summary>Die Stelle des ersten Wertes - optional; ohne sie die ganze Reihe.</summary>
        private static KiParameter AbParameter()
        {
            return new KiParameter("ab", KiParameterTyp.Ganzzahl,
                                   KiAktionsTexte.ErlAb,
                                   pflicht: false,
                                   anzeigename: KiAktionsTexte.AbName,
                                   min: 1, max: KiZahlenreihe.MaxLaenge);
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
        /// <para>
        /// Die Ablehnung NENNT, was es gibt — dieselbe Regel wie beim alten Weg
        /// (Fachkonzept 11.4): Ein „geht nicht" ohne Liste zwaenge das Modell zum Raten.
        /// </para>
        /// <para>
        /// <b>Seit dem 15.09.2026 nennt sie auch den WEG</b> (Anwenderbefund: „setze die
        /// Vorlauftemperatur Heizkessel auf 65°C" im Bereich Heizkessel). Die Absage
        /// lautete bis dahin „Es ist keine steuerbare Maske geoeffnet. Steuerbar sind:
        /// Form_Heizkessel_Bearbeiten, Form_PV, …" - eine Liste von TYPNAMEN, aus der
        /// weder Anwender noch Modell einen Menueweg ableiten koennen. Dabei stand die
        /// Antwort bereit: Der Katalog kennt alle Masken samt Feldern UNABHAENGIG davon,
        /// ob eine offen ist. Wer <c>vorlauf</c> und <c>ruecklauf</c> nennt, meint
        /// erkennbar die Heizkesselmaske - und genau das sagt die Absage jetzt, mitsamt
        /// dem Hinweis auf <c>dialog_oeffnen</c>.
        /// </para>
        /// </remarks>
        /// <param name="genannt">Der genannte Maskenschluessel; leer = die zuletzt angemeldete.</param>
        /// <param name="felder">
        /// Die Feldnamen, die der Aufruf nennt. Aus ihnen wird die gemeinte Maske
        /// erschlossen, wenn keine offen ist; ohne sie bleibt es bei der Liste.
        /// </param>
        private static string BrueckenGrund(string genannt, params string[] felder)
        {
            string gesucht = (genannt ?? "").Trim();

            if (gesucht.Length > 0 && !KiDialoge.Katalog.Kennt(gesucht))
                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.MaskeUnbekannt,
                                     gesucht, Aufzaehlen(Anzeigenamen()));

            string maske = Maskenschluessel(gesucht);

            if (maske.Length == 0 || !KiMaskenbruecke.IstAngemeldet(maske))
            {
                // ZUERST der Weg, dann die Liste: Wenn sich aus den genannten Feldern
                // (oder aus dem genannten Maskennamen) EINE Maske erschliessen laesst,
                // ist die Liste der uebrigen sechs nur Rauschen.
                KiDialog gemeint = GemeinteMaske(gesucht, felder, out IReadOnlyList<KiDialog> kandidaten);
                string weg = gemeint != null
                    ? string.Format(CultureInfo.CurrentCulture, KiDialogTexte.MaskeNichtOffen,
                                    gemeint.Anzeigename, gemeint.Maskenname)
                    : kandidaten.Count > 1
                        ? string.Format(CultureInfo.CurrentCulture, KiDialogTexte.MaskeMehrdeutig,
                                        Aufzaehlen(Beschriftungen(kandidaten)))
                        : null;

                // Welle #458: Kam der Aufruf aus einer Maske der AUSNAHMELISTE, sagt die
                // Absage das zuerst - mit ihrem Grund statt der Liste aller Masken.
                // Nur ohne genannte Maske: Wer eine nennt, meint sie.
                string ausnahme = gesucht.Length == 0 ? AbsageAusDemAufruf() : null;
                if (ausnahme != null) return weg == null ? ausnahme : ausnahme + " " + weg;

                if (weg != null) return weg;

                return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.KeineOffen,
                                     Aufzaehlen(Anzeigenamen()));
            }

            return null;
        }

        /// <summary>
        /// Die Absage für einen Aufruf aus einer Maske, die der Assistent mit Absicht
        /// nicht steuert (<see cref="KiDialogAusnahmen"/>, Welle #458); <c>null</c>, wenn
        /// der Aufruf keine solche Maske trifft.
        /// </summary>
        /// <remarks>
        /// <b>Der Aufruf ist der einzige Kontext, den es dafür gibt.</b> Eine ausgenommene
        /// Maske meldet sich nicht an der Brücke an; was der Assistent über sie weiß, ist
        /// der Hilfeschlüssel ihres Info-Knopfes, mit dem sie ihn gerufen hat
        /// (<see cref="KiChatKontext.Aufruf"/>). Er bleibt stehen, bis das Chatfenster
        /// schließt oder ein neuer Aufruf kommt.
        /// </remarks>
        private static string AbsageAusDemAufruf()
        {
            KiAufrufkontext aufruf = KiChatKontext.Aufruf;
            return aufruf == null ? null : KiDialogAusnahmen.Absage(aufruf.Hilfeschluessel);
        }

        /// <summary>
        /// Die Maske, die der Aufruf ERKENNBAR meint - genannt oder aus den Feldnamen
        /// erschlossen. <c>null</c>, wenn sie sich nicht eindeutig bestimmen laesst.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eindeutig heisst eindeutig.</b> Fuehren zwei Masken ein Feld desselben
        /// Namens (etwa <c>schritt</c>), liefert die Suche <c>null</c> - dann bleibt
        /// es bei der Liste, statt eine der beiden zu raten. Genannt wird nur, was sicher
        /// ist.
        /// </para>
        /// <para>
        /// <b>Mehrdeutig ist nur, was an VERSCHIEDENE Orte fuehrt</b> (Welle #456). Der
        /// Katalogeditor „Heizkessel bearbeiten" ist eine Ueberlagerung der Verwaltung
        /// „Administration Heizkessel", beide fuehren <c>wirkungsgrad_gas</c> - und
        /// beide oeffnet <c>dialog_oeffnen</c> an derselben Stelle. Das ist kein
        /// Raten zwischen zwei Masken, sondern EIN Weg; genannt wird dann die Maske,
        /// die dort aufgeht: die Verwaltung (<see cref="Zielmaske"/>).
        /// </para>
        /// <para>
        /// <b>Gesucht wird im KATALOG, nicht an der Bruecke</b> - und das ist der Punkt:
        /// Zu diesem Zeitpunkt ist gerade KEINE Maske angemeldet, die Bruecke wuesste also
        /// nichts. Der Katalog dagegen fuehrt alle sieben Masken samt Feldern, immer.
        /// </para>
        /// <para>
        /// <b>Intern statt privat</b> fuer den Waechter
        /// <c>EPOS.Kern.Tests/KiKatalogKulturTests</c>: Fuer jeden Feldschluessel des
        /// Katalogs muss die Antwort in jeder Sprache dieselbe sein.
        /// </para>
        /// </remarks>
        internal static KiDialog GemeinteMaske(string genannt, string[] felder)
            => GemeinteMaske(genannt, felder, out _);

        /// <summary>
        /// Wie <see cref="GemeinteMaske(string, string[])"/>; ist die Maske mehrdeutig,
        /// stehen in <paramref name="kandidaten"/> die Masken, zwischen denen nicht geraten
        /// wird (sonst ist die Liste leer).
        /// </summary>
        /// <remarks>
        /// <b>Ueber ALLE Masken entscheidet die beste Stufe der Namensregel</b>
        /// (<see cref="KiWahlstufe"/>, Welle #472). Gesucht wird Stufe fuer Stufe -
        /// exakter Schluessel, gefalteter Schluessel, Anzeigename, Wortanfang, enthaltener
        /// Teil -, und die erste Stufe, auf der IRGENDEINE Maske etwas findet, entscheidet.
        /// Masken, die den Namen erst auf einer schwaecheren Stufe treffen, zaehlen nicht
        /// mehr mit. Vorher genuegte jeder Maske ein eindeutiger Treffer auf IRGENDEINER
        /// Stufe: „With PV surplus" traf in der Waermesenke den Anfang ihres Anzeigenamens
        /// „With PV surplus:" und in der Ansicht „Simulation" den enthaltenen Teil
        /// „PV surplus" - beide zaehlten gleich, beide fuehren an denselben Ort, und die
        /// Absage schickte in die Simulation, die das Feld nicht hat. Findet die
        /// entscheidende Stufe mehrere Masken, die nicht an einen Ort fuehren, wird
        /// benannt abgesagt: mit genau diesen Masken, nicht mit der Liste aller.
        /// </remarks>
        internal static KiDialog GemeinteMaske(string genannt, string[] felder,
                                               out IReadOnlyList<KiDialog> kandidaten)
        {
            kandidaten = Array.Empty<KiDialog>();

            if (genannt.Length > 0)
            {
                KiDialog ausName = KiDialoge.Katalog.Finde(genannt);
                if (ausName != null) return ausName;
            }

            if (felder == null) return null;

            // Stufe fuer Stufe, und der buchstabengetreue Schluessel geht vor (KI-F1b):
            // Ein Name, den GENAU EINE Maske als Schluessel fuehrt, ist eindeutig - auch
            // wenn eine andere Maske einen aehnlichen traegt („bereitschaftsverlust" gegen
            // „bereitschaftsverluste"). Erst wenn keine Maske den Namen auf einer Stufe
            // trifft, wird die naechste befragt; dann meint „vorlauftemperatur" das Feld
            // „vorlauf".
            for (int rang = 1; rang <= RANG_TEIL; rang++)
            {
                List<KiDialog> treffer = Treffer(felder, rang);
                if (treffer.Count > 0) return Gemeint(treffer, felder, out kandidaten);
            }

            return null;
        }

        /// <summary>Der schwaechste Rang von <see cref="Rang"/>.</summary>
        private const int RANG_TEIL = 4;

        /// <summary>
        /// Der Rang einer Stufe im Vergleich UEBER Masken: exakter Schluessel (1) vor
        /// gleichem Namen (2) vor Wortanfang (3) vor enthaltenem Teil (4); 0 = nichts.
        /// </summary>
        /// <remarks>
        /// <b>Gefalteter Schluessel und Anzeigename stehen gleich.</b> Innerhalb EINER Maske
        /// geht der Schluessel vor (<see cref="KiWahl.Treffer"/>); zwischen zwei Masken
        /// aber ist „Preisquelle" als Beschriftung der Ansicht „Simulation" nicht weniger
        /// gemeint als <c>preisquelle</c> als Schluessel der Stromspeicherauslegung - das
        /// ist eine echte Mehrdeutigkeit, und sie wird benannt, nicht zugunsten des
        /// Schluessels entschieden.
        /// </remarks>
        private static int Rang(KiWahlstufe stufe) => stufe switch
        {
            KiWahlstufe.Schluessel => 1,
            KiWahlstufe.SchluesselGefaltet => 2,
            KiWahlstufe.Anzeigetext => 2,
            KiWahlstufe.Anfang => 3,
            KiWahlstufe.Teil => RANG_TEIL,
            _ => 0,
        };

        /// <summary>
        /// Die Masken, in denen einer der genannten Namen auf genau dem Rang
        /// <paramref name="rang"/> (<see cref="Rang"/>) etwas trifft - ein Feld oder mehrere.
        /// </summary>
        /// <remarks>
        /// <b>Auch mehrere Felder in EINER Maske bestimmen die Maske.</b> „Außenwand"
        /// passt im Gebaeude auf die Außenwand und die Außenwand zum Keller; welches Feld
        /// gemeint ist, klaert die offene Maske (<see cref="KiMaskenbruecke.Feldsuche"/>
        /// nennt dann die Kandidaten) - welche MASKE gemeint ist, steht aber fest. Zaehlte
        /// eine solche Maske nicht mit, gewaenne eine schwaechere Stufe einer fremden Maske
        /// (vorher: die Wirtschaftlichkeitsseite).
        /// </remarks>
        private static List<KiDialog> Treffer(string[] felder, int rang)
        {
            var treffer = new List<KiDialog>();

            foreach (string feld in felder)
            {
                if (string.IsNullOrWhiteSpace(feld)) continue;

                foreach (KiDialog d in KiDialoge.Katalog.Alle)
                    if (Rang(d.Feldsuche(feld.Trim()).Stufe) == rang && !treffer.Contains(d)) treffer.Add(d);
            }

            return treffer;
        }

        /// <summary>
        /// Die EINE Maske unter den Treffern der entscheidenden Stufe; <c>null</c> bei
        /// mehrdeutigem Treffer - dann stehen die Treffer in <paramref name="kandidaten"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Treffen die Felder MEHRERE Masken</b>, ist das nur dann eindeutig, wenn
        /// alle auf dasselbe Oeffnungsziel fuehren UND die Maske dieses Ziels selbst
        /// unter den Treffern steht - der Fall Katalogeditor und Verwaltung. Zwei
        /// Projektmasken derselben Startseite (Heizkessel und BHKW im Projekt) fuehren
        /// zwar auch an einen Ort, sind aber zwei Masken; dort wird weiter nicht geraten.
        /// </para>
        /// </remarks>
        private static KiDialog Gemeint(List<KiDialog> treffer, string[] felder,
                                        out IReadOnlyList<KiDialog> kandidaten)
        {
            kandidaten = Array.Empty<KiDialog>();

            if (treffer.Count == 1) return Zielmaske(treffer[0], felder);

            // Mehrere Treffer: EIN Ziel, und dessen Maske ist dabei - sonst mehrdeutig.
            string ziel = Zielschluessel(treffer[0]);
            bool einZiel = true;
            foreach (KiDialog d in treffer)
                if (!string.Equals(Zielschluessel(d), ziel, StringComparison.OrdinalIgnoreCase)) einZiel = false;

            if (einZiel)
                foreach (KiDialog d in treffer)
                    if (string.Equals(d.Maskenname, ziel, StringComparison.OrdinalIgnoreCase)) return d;

            kandidaten = treffer;
            return null;
        }

        /// <summary>„Anzeigename (Maskenschluessel)" je Maske - fuer die benannte Absage.</summary>
        private static IReadOnlyList<string> Beschriftungen(IReadOnlyList<KiDialog> masken)
        {
            var namen = new List<string>(masken.Count);
            foreach (KiDialog d in masken) namen.Add(d.Anzeigename + " (" + d.Maskenname + ")");
            return namen;
        }

        /// <summary>
        /// Das Oeffnungsziel einer Maske; ohne hinterlegtes Ziel ihr eigener Name.
        /// </summary>
        private static string Zielschluessel(KiDialog d)
        {
            string ziel = KiMaskenziele.Ziel(d.Maskenname);
            return ziel.Length > 0 ? ziel : d.Maskenname;
        }

        /// <summary>
        /// <b>Eine Ueberlagerung wird ueber ihre Verwaltung genannt</b> (Welle #456):
        /// Ist das Ziel der gemeinten Maske selbst eine Katalogmaske, und fuehrt sie alle
        /// genannten Felder, dann nennt die Absage DIESE - dort geht <c>dialog_oeffnen</c>
        /// auf, und dort lassen sich die Werte setzen. Fuehrt die Zielmaske die Felder
        /// nicht (die Photovoltaik des Projekts fuehrt auf den Modulkatalog, der ihre
        /// Felder nicht hat), bleibt es bei der gemeinten Maske.
        /// </summary>
        /// <remarks>
        /// <b>Ob die Zielmaske ein Feld fuehrt, entscheiden SCHLUESSEL</b>
        /// (<see cref="ZielFuehrtFeld"/>), nie ihre Anzeigenamen: Die stehen uebersetzt
        /// in <c>MyResource.Resource</c>, und dieselbe Frage bekaeme sonst je Sprache
        /// eine andere Antwort. Genau so nannte die Absage unter <c>en-US</c> fuer
        /// <c>bereitschaftsverlust</c> den Editor statt der Verwaltung - die Verwaltung
        /// fuehrt das Feld als <c>bbverlust</c>, und nur ihre deutsche Beschriftung
        /// („Betriebsbereitschaftsverluste") traf den Namen tolerant.
        /// </remarks>
        private static KiDialog Zielmaske(KiDialog gemeint, string[] felder)
        {
            KiDialog ziel = KiDialoge.Katalog.Finde(KiMaskenziele.Ziel(gemeint.Maskenname));
            if (ziel == null || ReferenceEquals(ziel, gemeint)) return gemeint;

            foreach (string feld in felder)
            {
                if (string.IsNullOrWhiteSpace(feld)) continue;
                if (!ZielFuehrtFeld(gemeint, ziel, feld.Trim())) return gemeint;
            }

            return ziel;
        }

        /// <summary>
        /// Fuehrt die Zielmaske <paramref name="ziel"/> das Feld, das der Aufruf an der
        /// gemeinten Maske <paramref name="gemeint"/> nennt?
        /// </summary>
        /// <remarks>
        /// <para>
        /// Zuerst wird der genannte Name an der GEMEINTEN Maske aufgeloest - buchstabengetreu,
        /// sonst tolerant, dieselbe Nachsicht, mit der sie gefunden wurde. Danach zaehlt
        /// nur noch der Schluessel dieses Feldes: Die Zielmaske fuehrt es unter demselben
        /// Schluessel oder unter dem, den der Katalog als sein Gegenstueck ERKLAERT
        /// (<see cref="KiDialoge.Zielfeldname"/> - der Katalogeditor nennt die
        /// Bereitschaftsverluste <c>bereitschaftsverlust</c>, seine Verwaltung
        /// <c>bbverlust</c>).
        /// </para>
        /// <para>
        /// Kennt die gemeinte Maske den Namen gar nicht, bleibt nur der Schluessel selbst.
        /// </para>
        /// <para>
        /// <b>An der Zielmaske gibt es KEINE Nachsicht</b> (Welle #469): Sie fuehrt den
        /// Schluessel buchstabengetreu oder als erklaertes Gegenstueck - sonst nicht. Die
        /// Frage ist die IDENTITAET zweier Werte, und die folgt nicht aus einem aehnlichen
        /// Namen. Eine Anfangs- oder Teilstringsuche ueber die Schluessel der Zielmaske
        /// (<see cref="KiWahl"/>, Stufen 4 und 5) traf <c>wr_wirkungsgrad</c> des
        /// Wechselrichters auf den <c>wirkungsgrad</c> des Moduls im Modulkatalog,
        /// <c>ladeleistung</c> des Puffers auf die <c>speicher_ladeleistung</c> der
        /// Batterie in der Ansicht „Simulation" und <c>ersatz_fuehren</c> der
        /// Vorlagenposition auf den <c>satz</c> der Kostenverwaltung - jedesmal hiess die
        /// Absage den Anwender eine Maske oeffnen, die das gemeinte Feld nicht hat. Der
        /// einzige Fall, den sie treffen sollte (<c>katalog_breite</c> des Aufklappers
        /// „Alle Daten" steht im Modulkatalog als <c>breite</c>), ist jetzt die erklaerte
        /// Vorsilbe in <see cref="KiDialoge.Zielfeldname"/>. Die Nachsicht fuer den
        /// Wortlaut des MODELLS bleibt, wo sie hingehoert: beim Aufloesen an der gemeinten
        /// Maske (oben) und an der offenen Maske (<see cref="KiMaskenbruecke.Feldsuche"/>).
        /// </para>
        /// </remarks>
        private static bool ZielFuehrtFeld(KiDialog gemeint, KiDialog ziel, string feld)
        {
            KiDialogFeld eigenes = gemeint.FindeFeld(feld) ?? gemeint.FindeFeldTolerant(feld);
            string schluessel = eigenes == null
                ? feld
                : KiDialoge.Zielfeldname(gemeint.Maskenname, eigenes.Name);

            return ziel.KenntFeld(schluessel);
        }

        /// <summary>
        /// Die blossen FELDNAMEN einer Zuweisungsliste („vorlauf=65; ruecklauf=55").
        /// </summary>
        /// <remarks>
        /// Bewusst nachsichtig und ohne Beanstandung: Hier geht es nur darum, die gemeinte
        /// Maske zu erraten, bevor ueberhaupt eine offen ist. Die richtige Zerlegung samt
        /// Fehlermeldung macht <see cref="Zerlegen"/> - aber erst, wenn es eine Maske gibt.
        /// </remarks>
        private static string[] Feldnamen(string werte)
        {
            if (string.IsNullOrWhiteSpace(werte)) return Array.Empty<string>();

            var namen = new List<string>();
            foreach (string teil in werte.Split(ZUWEISUNGSTRENNER))
            {
                int gleich = teil.IndexOf(ZUWEISUNGSZEICHEN);
                string name = (gleich > 0 ? teil.Substring(0, gleich) : teil).Trim();
                if (name.Length > 0) namen.Add(name);
            }
            return namen.ToArray();
        }

        /// <summary>Die Anzeigenamen aller Katalogmasken - fuer Absagen an den ANWENDER.</summary>
        /// <remarks>
        /// Der Typname (<c>Form_PufferSp_Bearbeiten</c>) gehoert in das Protokoll und in
        /// die Parameter des Modells; in einem Satz, den der Anwender liest, hat er nichts
        /// zu suchen.
        /// </remarks>
        private static IReadOnlyList<string> Anzeigenamen()
        {
            var namen = new List<string>();
            foreach (KiDialog d in KiDialoge.Katalog.Alle) namen.Add(d.Anzeigename);
            return namen;
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

        /// <summary>
        /// Klartextgrund fuer ein Feld, das die Bruecke nicht fuehrt - oder das MEHRERE
        /// treffen koennte (KI-F1b, KI-D-Q6).
        /// </summary>
        /// <remarks>
        /// <b>Zwei Absagen, weil es zwei Befunde sind.</b> „Das Feld gibt es nicht, hier
        /// ist die Liste" hilft, wenn der Name danebenlag; steht der Name dagegen fuer
        /// mehrere Felder („lauf" fuer Vorlauf und Ruecklauf), ist die ganze Liste
        /// Rauschen - genannt werden dann die Kandidaten, und geraten wird nicht.
        /// </remarks>
        private static string FeldzugangGrund(string genannt, string feld)
        {
            string maske = Maskenschluessel(genannt);
            KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
            KiFeldtreffer treffer = KiMaskenbruecke.Feldsuche(maske, feld);

            if (treffer.Mehrdeutig)
                return string.Format(CultureInfo.CurrentCulture,
                                     MyResource.Resource.KI_FELD_MEHRDEUTIG,
                                     feld ?? "",
                                     eintrag == null ? maske : eintrag.Anzeigename,
                                     Aufzaehlen(treffer.Kandidaten));

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
            string grund = BrueckenGrund(a.Text("maske"), a.Text("feld"));
            if (grund != null) return grund;

            string maske = Maskenschluessel(a.Text("maske"));

            grund = Lesemodus();
            if (grund != null) return grund;

            KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));
            if (zugang == null) return FeldzugangGrund(maske, a.Text("feld"));

            grund = FeldSetzbar(zugang);
            if (grund != null) return grund;

            grund = Schreibschutz(maske, zugang);
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
            // Die Feldnamen gehen in die Bruecke MIT: Ist keine Maske offen, erschliesst
            // sie daraus die gemeinte und nennt sie - „vorlauf; ruecklauf" heisst
            // erkennbar Heizkessel (Anwenderbefund 15.09.2026).
            string grund = BrueckenGrund(a.Text("maske"), Feldnamen(a.Text("werte")));
            if (grund != null) return grund;

            string maske = Maskenschluessel(a.Text("maske"));

            grund = Lesemodus();
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

                grund = Schreibschutz(maske, zugang);
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

        /// <summary>
        /// Vorbedingung von <c>reihe_setzen</c> (Welle #458 Stufe 3b): Maske angemeldet,
        /// Feld da, eine Zahlenreihe und setzbar, kein Lesemodus, kein Schreibschutz,
        /// Laenge und Grenzen passend, mindestens eine echte Aenderung.
        /// </summary>
        /// <remarks>
        /// Dieselbe Reihenfolge wie bei <see cref="EinzelfeldGrund"/> - die Vorbedingung
        /// laeuft vor der Vorschau, und die Vorschau darf danach ohne weitere Pruefung auf
        /// Katalogeintrag, Zugang und Umsetzung zugreifen.
        /// </remarks>
        private static string ReihenGrund(KiAufruf a)
        {
            string grund = BrueckenGrund(a.Text("maske"), a.Text("feld"));
            if (grund != null) return grund;

            string maske = Maskenschluessel(a.Text("maske"));

            grund = Lesemodus();
            if (grund != null) return grund;

            KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, a.Text("feld"));
            if (zugang == null) return FeldzugangGrund(maske, a.Text("feld"));

            if (!zugang.Feld.IstReihe)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KI_FELD_KEINE_REIHE,
                                     zugang.Feld.Anzeigename);

            grund = FeldSetzbar(zugang);
            if (grund != null) return grund;

            grund = Schreibschutz(maske, zugang);
            if (grund != null) return grund;

            KiFeldumsetzung umsetzung = KiFeldwandler.WandleReihe(zugang, a.ZahlListe("werte"), Ab(a));
            if (!umsetzung.Ok) return umsetzung.Grund;

            // Ohne Aenderung kein Block - dieselbe Regel wie bei formular_ausfuellen.
            IReadOnlyList<double?> alt = KiFeldwandler.Reihenwerte(zugang);
            IReadOnlyList<double?> neu = KiZahlenreihe.Werte(umsetzung.Wert) ?? Array.Empty<double?>();
            for (int i = 0; i < neu.Count; i++)
            {
                double? vorher = i < alt.Count ? alt[i] : null;
                if (vorher != neu[i]) return null;
            }

            return string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ReiheOhneAenderung,
                                 zugang.Feld.Anzeigename);
        }

        /// <summary>Die Stelle des ersten Wertes; <c>0</c> = nicht genannt, die ganze Reihe.</summary>
        private static int Ab(KiAufruf a) => a.Hat("ab") ? a.Id("ab") : 0;

        /// <summary>
        /// Der Hinweis einer Zeile von <c>dialog_lesen</c>: bei einer Zahlenreihe Umfang und
        /// Weg (Welle #458 Stufe 3b), bei einem nicht setzbaren Feld der Grund.
        /// </summary>
        private static string Lesehinweis(KiFeldwert w)
        {
            if (w.Feld.IstReihe)
            {
                string reihe = string.Format(CultureInfo.CurrentCulture, KiDialogTexte.ReiheHinweis,
                                             w.Feld.Reihe.Umfang());
                return w.Setzbar ? reihe : reihe + " " + KiDialogTexte.NichtSetzbar;
            }

            return w.Setzbar ? null : KiDialogTexte.NichtSetzbar;
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
        /// Was gegen das SPEICHERN dieser Maske spricht: Lesemodus der Lizenz (iF30)
        /// und ein schreibgeschuetzter Katalogsatz (<c>ReadOnly</c>, Fachkonzept 4.5).
        /// Das Setzen fragt beides je Feld (<see cref="Lesemodus"/>,
        /// <see cref="Schreibschutz"/>).
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
            string lesemodus = Lesemodus();
            if (lesemodus != null) return lesemodus;

            return KiMaskenbruecke.Haken(maske).IstSchreibgeschuetzt() ? Schutzabsage(maske) : null;
        }

        /// <summary>Der Lesemodus der Lizenz (iF30); <c>null</c> = es darf geschrieben werden.</summary>
        private static string Lesemodus() => SimulationLaufCtrl.LesemodusGrund();

        /// <summary>
        /// Der Schreibschutz des bearbeiteten Satzes, gefragt FUER DIESES FELD;
        /// <c>null</c> = es darf gesetzt werden.
        /// </summary>
        /// <remarks>
        /// <b>Ein Feld, das den SATZ WAEHLT, ist ausgenommen</b>
        /// (<see cref="KiDialogFeld.Satzwahl"/>, Welle #456): Es schreibt nichts in den
        /// geschuetzten Satz, es wechselt nur, welcher bearbeitet wird. Ohne diese
        /// Ausnahme bliebe der Assistent in einer Verwaltung, deren erste Zeile ein
        /// Auslieferungssatz ist, stecken - er koennte den eigenen Satz nicht waehlen.
        /// </remarks>
        private static string Schreibschutz(string maske, KiFeldzugang zugang)
        {
            if (zugang != null && zugang.Feld.Satzwahl) return null;
            return KiMaskenbruecke.Haken(maske).IstSchreibgeschuetzt() ? Schutzabsage(maske) : null;
        }

        /// <summary>
        /// Die Absage an einen geschuetzten Satz - mit dem Grund und dem Weg des
        /// Dialogs, wo er einen anmeldet (<see cref="KiMaskenhaken.Schreibschutzgrund"/>),
        /// sonst die allgemeine.
        /// </summary>
        private static string Schutzabsage(string maske)
        {
            KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(maske);
            string name = eintrag == null ? maske : eintrag.Anzeigename;
            string grund = KiMaskenbruecke.Haken(maske).Schutzgrund();

            return grund.Length > 0
                ? string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.KI_FELD_SATZ_GESCHUETZT_WEG, name, grund)
                : string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.KI_FELD_SATZ_GESCHUETZT, name);
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
        /// Formatiert wird ueber <see cref="KiMaskenbruecke.Feldtext"/> — dieselbe
        /// Schreibweise, in der der Feldblock die Werte an das Modell gibt, und bei einer
        /// WAHL der Text des Eintrags statt seiner Id (KI-F1b). Zwei Fassungen desselben
        /// Wertes waeren genau die Stelle, an der Vorschau und Ergebnis auseinanderliefen.
        /// </remarks>
        private static string Feldtext(KiFeldzugang zugang) => KiMaskenbruecke.Feldtext(zugang);

        /// <summary>
        /// Der Vermerk einer toleranten Namensauflösung („vorlauftemperatur → vorlauf")
        /// - leer, wenn der genannte Name schon der Schluessel war (KI-F1b).
        /// </summary>
        /// <remarks>
        /// Er haengt am Ergebnistext und geht damit in die Protokollzeile
        /// (<c>KiErgebnis.Kurzfassung</c>): Wer spaeter nachliest, welches Feld gesetzt
        /// wurde, soll auch sehen, unter welchem Namen es gemeint war.
        /// </remarks>
        private static string Aufloesung(string genannt, KiFeldzugang zugang)
        {
            string gesucht = (genannt ?? "").Trim();

            if (zugang == null || gesucht.Length == 0 ||
                string.Equals(gesucht, zugang.Name, StringComparison.Ordinal))
                return "";

            return " " + string.Format(CultureInfo.CurrentCulture,
                                       MyResource.Resource.KI_FELD_AUFGELOEST,
                                       gesucht, zugang.Name);
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
        /// <remarks>
        /// <b>Beide Seiten stehen als TEXT</b> (KI-F1b): Bei einer Wahl zeigt der Block
        /// „Energietraeger · Heizoel EL → Erdgas H" und nicht „12 → 3". Bestaetigt wird,
        /// was der Anwender auf der Maske liest (Feldsicherung 11.5).
        /// </remarks>
        private static void Sammle(string maske, string werte, List<KiFeldAenderung> ziel)
        {
            var namen = new List<string>();
            var inhalte = new List<string>();
            if (Zerlegen(werte, namen, inhalte) != null) return;

            for (int i = 0; i < namen.Count; i++)
            {
                KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, namen[i]);
                if (zugang == null) continue;

                var aenderung = new KiFeldAenderung(zugang.Feld.Anzeigename,
                                                    Feldtext(zugang),
                                                    Neutext(zugang, inhalte[i]));
                if (aenderung.IstAenderung) ziel.Add(aenderung);
            }
        }

        /// <summary>
        /// Der NEUE Wert, wie er im Bestaetigungsblock steht: bei einer Wahl der Text des
        /// getroffenen Eintrags, sonst der genannte Wert selbst (KI-F1b).
        /// </summary>
        private static string Neutext(KiFeldzugang zugang, string wert)
        {
            if (zugang == null || !zugang.IstWahl) return wert;

            IReadOnlyList<KiWahleintrag> eintraege = zugang.Wahleintraege();
            KiWahltreffer treffer = KiWahl.Treffer(eintraege, wert);

            return treffer.Eindeutig ? eintraege[treffer.Stelle].Text : wert;
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
                case KiParameterTyp.Aufzaehlung:
                case KiParameterTyp.Wahl: return KiDialogTexte.TypAuswahl;
                case KiParameterTyp.ZahlListe: return KiDialogTexte.TypZahlenreihe;
                default: return KiDialogTexte.TypText;
            }
        }

        /// <summary>Ein leerer Wert wird benannt, nicht verschwiegen.</summary>
        private static string Sichtbar(string text)
        {
            return string.IsNullOrEmpty(text) ? KiDialogTexte.KeinWert : text;
        }

        /// <summary>
        /// Der Stand eines Wahlfeldes fuer <c>dialog_lesen</c>: „Text (Schluessel)"
        /// (KI-F1b, KI-D-Q6).
        /// </summary>
        /// <remarks>
        /// Der Text allein liesse das Modell den Schluessel raten, der Schluessel allein
        /// machte die Antwort unlesbar. Traegt das Feld gar keinen Wert, bleibt es leer -
        /// eine Klammer um nichts ist keine Auskunft.
        /// </remarks>
        private static string Wahlwert(KiFeldwert w)
        {
            if (w == null || w.Schluessel.Length == 0) return "";
            if (string.Equals(w.Text, w.Schluessel, StringComparison.Ordinal)) return w.Text;

            return w.Text + " (" + w.Schluessel + ")";
        }
    }
}
