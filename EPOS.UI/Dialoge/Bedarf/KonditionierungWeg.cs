using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Der Weg der Konditionierung</b> — alles, was der Reiter „Konditionierung" des
/// Gebäude-Katalogeditors, seine Kalenderkarten und die Zonenmatrix vom Kern brauchen, als EIN
/// Parameter (Entwurf KP2 Abschnitt 2, Stufe KP2 Welle U0b; Muster <see cref="GebaeudeZonenweg"/>).
/// </summary>
/// <remarks>
/// <para><b>Kein Delegat, kein Knopf.</b> Je Handlung ein Delegat; fehlt er, bietet der Reiter den
/// Knopf nicht an (<see cref="Bietet"/>). Ein Bündel ohne Delegaten — die Vorgabe, auch ohne Gaben —
/// bietet keinen.</para>
/// <para><b>Arbeitsstand statt Schreiben je Knopf.</b> Jede Handlung bekommt den
/// <see cref="KonditionierungStand"/> und gibt einen NEUEN zurück (<see cref="KonditionierungErgebnis"/>);
/// der Reiter übernimmt ihn in den Arbeitsstand. Die <see cref="KonditionierungDaten.Fassung"/> jeder
/// Ebene, die die Handlung geändert hat — auch einer Zone, deren Kalender „Anlegen" am Gebäude mit
/// anlegt (F2 Regel 2) —, hat die Hülle schon hochgezählt; der Reiter zählt nur, was er selbst ändert. <b>Geschrieben wird allein im OK-Weg des Editors</b> — die Konditionierung reist im Feldsatz
/// (<see cref="GebaeudeKatalogDaten.Konditionierung"/>) und an den Zonen
/// (<see cref="ZoneDaten.Konditionierung"/>) durch <c>Speichern</c> bzw. den Zonenweg; einen eigenen
/// Schreibdelegaten gibt es deshalb nicht. Ausgenommen sind die Vorlagen: „Als Vorlage speichern…"
/// und die Vorlagenverwaltung schreiben sofort mit eigenem OK (Festlegung 13), und eine Zeile unter
/// dem Knopf sagt es.</para>
/// <para><b>Wer die Delegaten trägt.</b> Die Hülle (<c>KonditionierungHuelle.Weg</c> in
/// <c>EPOS.UI.Daten/Bedarf/</c>) füllt das Bündel aus den reinen Schritten des Kerns über dem
/// Arbeitsstand (<c>Konditionierungsarbeit</c>, Entwurf KP2 Abschnitt 2); je Delegat nennt sein
/// Kommentar den Kernweg, über den sie ihn trägt. Die Übersetzung zwischen den Typen dieser Seite und
/// dem Kern (<c>Vorgabematrix</c>, <c>Matrixzelle</c>, <c>Konditionierungskalender</c>,
/// <c>Kalenderregel</c>, <c>KonditionierungCtrl.Eigner</c>) steht allein in der Hülle. Im Projekt
/// rechnet der Arbeitsstand mit dem Bezug des Projekts wie der Lauf — Stufe der Anlagenkopplung,
/// Referenzjahr, Kühlbetrieb (<c>KonditionierungHuelle.Projektbezug</c>) —, im Katalog ohne Kopplung
/// mit dem Bezugsjahr 2025. Gebunden wird das Bündel vom Reiter „Konditionierung" über
/// <see cref="KonditionierungBearbeitung"/>.</para>
/// <para><b>Zusammensetzen statt Sonderwege.</b> Die Vorschau einer Vorlage an den
/// Ferienzeiträumen des Ziels (Teilkonzept 7.4) ist <see cref="WochenVorschau"/> bzw.
/// <see cref="Teppichbild"/> des Stands, den <see cref="VorlageUebernehmen"/> auf einer Kopie
/// liefert; die Woche eines abgeleiteten Kalenders ist die, die <see cref="Anlegen"/> auf einer
/// Kopie liefert. „Zurücknehmen" braucht keinen Delegaten: Der Reiter hält den Stand vor der
/// letzten Handlung.</para>
/// </remarks>
public sealed class KonditionierungWeg
{
    /// <summary>Das leere Bündel: Es bietet keinen Knopf an.</summary>
    public static KonditionierungWeg Keiner { get; } = new();

    // ------------------------------------------------------------------ die Matrix

    /// <summary>
    /// <b>Eine Zelle setzen</b> (Entwurf KP2 Abschnitt 2 „Zelle setzen"): die Weiche des Zellenorts
    /// (Bestandsfeld des Feldsatzes oder neue Zelle, <see cref="KonditionierungDaten.Bestandsfeld"/>),
    /// Nachtfenster und Merker (Festlegung 5), die Folgeregel eines angelegten, nicht von Hand
    /// geänderten Kalenders (F2, E56) und — nach der Rückfrage — die Aufteilung der Gesamtangabe
    /// <c>Luftwechselrate</c> (F5: Infiltration = min(0,3; Rate), Nutzerlüftung = Rest). Die Zelle
    /// trägt den gewollten Zustand samt Wert, auch den einer Bestandszelle.
    /// <para>Kern: <c>Konditionierungsarbeit.ZelleSetzen</c> (rein; Weiche <c>Matrixzellenort</c>), getragen
    /// von der Hülle <c>KonditionierungHuelle.Weg</c>; der Datenbankweg <c>KonditionierungCtrl.Vorgabe</c>
    /// ist eine dünne Hülle darüber. Die Rückfrage „aufteilen" kommt als
    /// <see cref="KonditionierungErgebnis.Rueckfrage"/>.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungZeile, KonditionierungZelle, KonditionierungErgebnis>? ZelleSetzen { get; init; }

    /// <summary>
    /// <b>„Kalender anlegen"</b> (Teilkonzept 3.3): Der Generator macht aus der wirksamen Matrix den
    /// Kalender der Größe — mit dem Rundlauf, den Kalendern der Zonen mit eigenen Zellen (F2 Regel 2,
    /// Befund B4) und dem Geräte-Nennwert nach Personen (P1, Befund B5).
    /// <para>Kern: <c>Konditionierungsarbeit.Anlegen</c> (rein) über <c>Standardfahrplan.Erzeugen</c> mit
    /// Rundlauf, <c>PersonenNennwertVorschlag</c> und <c>GeraeteNennwertNachPersonen</c>; der
    /// Datenbankweg <c>KonditionierungCtrl.Anlegen</c> ist eine dünne Hülle darüber.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungErgebnis>? Anlegen { get; init; }

    /// <summary>
    /// <b>„Verwerfen"</b> (Teilkonzept 3.3): Der angelegte Kalender der Größe fällt samt Perioden, die
    /// Matrix bleibt; danach ist er wieder abgeleitet.
    /// <para>Kern: <c>Konditionierungsarbeit.Verwerfen</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungErgebnis>? Verwerfen { get; init; }

    /// <summary>
    /// <b>„Matrix erneut anwenden…"</b> (P12 (a)): ersetzt am angelegten Kalender nur den Matrixbereich
    /// — Standardwoche, Ferien- und Saisonperioden —; eigene Perioden und Ausnahmetage bleiben. Die
    /// Rückfrage kommt vorher (<see cref="Rueckfrage"/>).
    /// <para>Kern: <c>Konditionierungsarbeit.MatrixErneut</c> (rein; <c>IstMatrixbereich</c>).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungErgebnis>? MatrixErneut { get; init; }

    /// <summary>
    /// <b>„Aus dem Katalog erneut übernehmen…"</b> (Festlegung 4, nur Projekt): ersetzt die ganze
    /// Gebäudeebene samt Bestandszellen, Nachtzeiten und Ferienzeiträumen; die Zonen bleiben.
    /// <para>Kern: <c>Konditionierungsarbeit.KatalogErneut</c> (rein) mit der Katalogebene aus
    /// <c>GebaeudeStammCtrl.KatalogebeneDerKopie</c> (ein Leseweg); nur im Projekt belegt.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungErgebnis>? KatalogErneut { get; init; }

    /// <summary>
    /// <b>„Aufteilen"</b> (E56 F5 (a), Befund B3) — die Antwort auf die Rückfrage, die
    /// <see cref="ZelleSetzen"/> oder <see cref="VorlageUebernehmen"/> als
    /// <see cref="KonditionierungErgebnis.Rueckfrage"/> meldet: Die Gesamtangabe <c>Luftwechselrate</c> des
    /// Gebäudes wird Infiltration = min(0,3 1/h; Rate) und Nutzerlüftung = Rest; der wirksame Luftwechsel
    /// bleibt. Danach wiederholt der Reiter die Handlung.
    /// <para>Kern: <c>Konditionierungsarbeit.LuftwechselAufteilen</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungErgebnis>? LuftwechselAufteilen { get; init; }

    // ------------------------------------------------------------------ Zonen (Stufe KP2, Welle U4)

    /// <summary>
    /// <b>„Vom Gebäude übernehmen und anpassen"</b> (Teilkonzept 3.4, 7.3) — nur an einer Zone
    /// (<see cref="KonditionierungOrt.Zone"/>): Sie folgt dem angelegten Kalender des Gebäudes; die
    /// Handlung legt ihr eine eigene Kopie an, samt Woche, Perioden und Herkunft, ein Anteilskalender
    /// mit dem Nennwert der Zone (eigener Wert oder Flächenanteil).
    /// <para>Kern: <c>Konditionierungsarbeit.VomGebaeudeUebernehmen</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungErgebnis>? VomGebaeude { get; init; }

    /// <summary>
    /// <b>Was eine Zone vom Gebäude erbt</b> (Teilkonzept 3.4, 7.3) — die Zellen, die eine leere Zelle
    /// der Zone gerade gälte: jede Zelle mit ihrem Wert, AUCH eine Bestandszelle (Heizen Tag … Ferien,
    /// Infiltration, Nutzerlüftung, innere Gewinne im Flächenanteil), dazu das Nachtfenster und je
    /// Größe der Zustand „vom Gebäude" oder „aus der Matrix". Zweiter Parameter: die Id der Zone im
    /// Arbeitsstand. Die Platzhalter „Vorgabe …" der Zonenmatrix; <c>null</c> = keine Angabe (dann
    /// „wie Gebäude").
    /// <para>Kern: <c>Konditionierungsarbeitsstand.Erbmatrix</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, int, KonditionierungDaten?>? Geerbt { get; init; }

    // ------------------------------------------------------------------ Vorlagen je Größe

    /// <summary>
    /// <b>Die Vorlagen einer Größe</b> für die Auswahlliste der Karte (Teilkonzept 7.4) — die
    /// ausgelieferten zuerst.
    /// <para>Kern: <c>KonditionierungsvorlageCtrl.Liste(größe)</c> (ein Leseweg).</para>
    /// </summary>
    public Func<KonditionierungGroesse, IReadOnlyList<KonditionierungVorlageDaten>>? Vorlagen { get; init; }

    /// <summary>
    /// <b>Eine Vorlage übernehmen</b> (P11, P12): Die Zellen der Vorlage gehen in die Matrixspalte
    /// (eine leere Zelle lässt die des Ziels), der Kalender der Größe wird angelegt — Generator mit den
    /// Ferienzeiträumen des Ziels, darüber Woche und Perioden der Vorlage —, die Herkunft steht in
    /// <see cref="KonditionierungKalender.Vorlage"/>. Dritter Parameter: die Id der Vorlage.
    /// <para>Kern: <c>Konditionierungsarbeit.VorlageUebernehmen</c> (rein: Generator + Vorlage +
    /// Bestand) mit der Vorlage aus <c>KonditionierungsvorlageCtrl.Lesen</c> und
    /// <c>KonditionierungCtrl.StandLesen</c> (Lesewege).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, long, KonditionierungErgebnis>? VorlageUebernehmen { get; init; }

    /// <summary>
    /// <b>„Als Vorlage speichern…"</b> (E54): legt aus der Spalte und, falls angelegt, dem Kalender
    /// der Größe eine eigene Vorlage an — ohne Nennwert und Saison. Schreibt sofort (Festlegung 13);
    /// ein Doppelname in der Liste ist eine benannte Ablehnung.
    /// <para>Kern: der Inhalt aus dem Arbeitsstand (<c>Konditionierungsarbeit.AlsVorlage</c>, rein),
    /// geschrieben über <c>KonditionierungsvorlageCtrl.SpeichernAus</c> mit <c>Namenspruefung</c>.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungVorlageEingabe, KonditionierungVorlageErgebnis>? AlsVorlageSpeichern { get; init; }

    /// <summary>
    /// <b>Eine eigene Vorlage umbenennen</b> (Vorlagenverwaltung, F4): Id und neuer Name; schreibt
    /// sofort. <para>Kern: <c>KonditionierungsvorlageCtrl.Umbenennen</c>.</para>
    /// </summary>
    public Func<long, string, KonditionierungVorlageErgebnis>? VorlageUmbenennen { get; init; }

    /// <summary>
    /// <b>Eine eigene Vorlage löschen</b> (Vorlagenverwaltung, F4); kein Gebäude wird berührt.
    /// <para>Kern: <c>KonditionierungsvorlageCtrl.Loeschen</c>.</para>
    /// </summary>
    public Func<long, KonditionierungVorlageErgebnis>? VorlageLoeschen { get; init; }

    /// <summary>
    /// <b>Eine Vorlage duplizieren</b> (Vorlagenverwaltung, F4) — auch eine ausgelieferte; Id und Name
    /// der Kopie. <para>Kern: <c>KonditionierungsvorlageCtrl.Duplizieren(id, name, out neueId)</c>.</para>
    /// </summary>
    public Func<long, string, KonditionierungVorlageErgebnis>? VorlageDuplizieren { get; init; }

    /// <summary>
    /// <b>„Kopieren nach …"</b> (Vorlagenverwaltung, Teilkonzept 3.5, 7.4): legt aus einer Vorlage — auch einer
    /// ausgelieferten — eine eigene Vorlage einer anderen Größe an: Geräte ↔ Personen unverändert, Heizen →
    /// Kühlen mit Zeitstruktur und Aus-Zeiten, jede Zelle mit Sollwert in Höhe des Tagwerts auf dem
    /// Komfortsollwert, jede Absenkzeit (Heizsollwert unter dem Tagwert) auf dem Absenksollwert bzw. „aus".
    /// Erster Parameter: die Id der Quelle. Schreibt sofort (Festlegung 13); ein Doppelname in der Zielliste
    /// kommt mit <see cref="KonditionierungVorlageErgebnis.AmNamen"/>, ein ungültiger Komfortsollwert mit
    /// <see cref="KonditionierungVorlageErgebnis.AmSollwert"/>, ein ungültiger Absenksollwert mit
    /// <see cref="KonditionierungVorlageErgebnis.AmAbsenkwert"/>.
    /// <para>Kern: <c>KonditionierungsvorlageCtrl.KopierenNach</c> über die reine Regel
    /// <c>Vorlagenkopierregel.Umsetzen</c>; Namensregel, Komfort- und Absenksollwert vorher
    /// (<c>NamePruefen</c>, <c>Vorlagenkopierregel.KomfortsollwertPruefen</c>,
    /// <c>Vorlagenkopierregel.AbsenksollwertPruefen</c>).</para>
    /// </summary>
    public Func<long, KonditionierungVorlageKopie, KonditionierungVorlageErgebnis>? VorlageKopieren { get; init; }

    /// <summary>
    /// <b>Die erlaubten Ziele von „Kopieren nach …"</b> je Quellgröße — nur diese zeigt der Dialog; leer = die
    /// Größe lässt sich in keine andere kopieren (Kühlen, Lüftung). Bei Heizen → Kühlen samt Vorgaben des
    /// Komfort- und des Absenksollwerts und ihren Grenzen.
    /// <para>Kern: <c>Vorlagenkopierregel.Ziele</c>, <c>KOMFORTSOLLWERT_VORGABE</c>,
    /// <c>ABSENKSOLLWERT_VORGABE</c>, <c>KomfortsollwertMin</c>/<c>KomfortsollwertMax</c>.</para>
    /// </summary>
    public Func<KonditionierungGroesse, IReadOnlyList<KonditionierungKopierziel>>? Kopierziele { get; init; }

    // ------------------------------------------------------------------ Werkzeuge der Karte

    /// <summary>
    /// <b>Die Grundangabe</b> der aufgeklappten Karte (Teilkonzept 3.2 Ebene 1, 7.5; Welle U3): ein Wert
    /// in der Einheit der Spalte (Anteile in Prozent) oder <c>null</c> = „aus". Eine Standardwoche fällt
    /// dabei — Grundangabe und Woche stehen nie zugleich. Eine direkte Eingabe: die Herkunft bleibt.
    /// <para>Kern: <c>Konditionierungsarbeit.Grundangabe</c> über <c>Kalenderwerkzeuge.Grundangabe</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, double?, KonditionierungErgebnis>? Grundangabe { get; init; }

    /// <summary>
    /// <b>Die Standardwoche</b> aus dem Wochenraster der Karte (Teilkonzept 3.2 Ebene 2, 7.5; Welle U3):
    /// 168 Werte ab Montag 00:00 in der Einheit der Spalte, <see cref="double.NaN"/> = „aus"; <c>null</c>
    /// verwirft die Woche, an ihre Stelle tritt die Grundangabe mit dem häufigsten Wert der Woche.
    /// <para>Kern: <c>Konditionierungsarbeit.Standardwoche</c> über <c>Kalenderwerkzeuge.Standardwoche</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, double[]?, KonditionierungErgebnis>? Standardwoche { get; init; }

    /// <summary>
    /// <b>Eine eigene Periode anlegen oder ersetzen</b> (Periodenliste, Festlegung 15; Welle U3): Zeitraum
    /// (Tag 1 … 365 im Gemeinjahr) oder Feiertag (eine der neun Regeln), mit Name und Angabe — Wert, „aus"
    /// oder „wie Wochentag". Dritter Parameter: der Rang der Periode, die ersetzt wird; <c>null</c> = neu
    /// über der ranghöchsten eigenen Periode im Eigenband. Der Matrixbereich (Ferien, Saison) ist nur lesbar.
    /// <para>Kern: <c>Konditionierungsarbeit.PeriodeSetzen</c> über <c>Kalenderwerkzeuge.PeriodeSetzen</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, int?, KonditionierungPeriode, KonditionierungErgebnis>? PeriodeSetzen { get; init; }

    /// <summary>
    /// <b>Rang ▲▼</b> einer eigenen Periode im Eigenband (Festlegung 15): tauscht den Rang mit der
    /// nächsten eigenen Periode darüber (<c>true</c>) bzw. darunter.
    /// <para>Kern: <c>Konditionierungsarbeit.RangVerschieben</c> über <c>Kalenderwerkzeuge.RangVerschieben</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, int, bool, KonditionierungErgebnis>? RangVerschieben { get; init; }

    /// <summary>
    /// <b>Eine eigene Periode löschen</b> (Festlegung 15) — über ihren Rang; der Matrixbereich wird benannt abgelehnt.
    /// <para>Kern: <c>Konditionierungsarbeit.PeriodeLoeschen</c> über <c>Kalenderwerkzeuge.PeriodeLoeschen</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, int, KonditionierungErgebnis>? PeriodeLoeschen { get; init; }

    /// <summary>
    /// Die neun Feiertagsregeln der Periodenliste (Persistenzwert und Anzeigename, F11) — die Auswahl der
    /// Art „Feiertag"; <c>null</c> = keine (dann bietet die Liste nur Zeiträume an).
    /// <para>Kern: <c>DbWerte.KOND_FEIERTAGE</c> und <c>Kalenderwerkzeuge.Feiertagsnamen</c>.</para>
    /// </summary>
    public IReadOnlyList<KonditionierungFeiertag>? Feiertagsregeln { get; init; }

    /// <summary>
    /// <b>„In den Kalender übernehmen"</b> an der Wärmeübergabe (Teilkonzept 5.5; Welle U3): das
    /// Sollwert-Zeitprogramm (<c>Sollwertprofil</c>) wird die Standardwoche des Heizkalenders — ist er nicht
    /// angelegt, wird er zuerst angelegt; Perioden und Herkunft eines angelegten bleiben. Die Rückfrage vor
    /// einem angelegten Kalender stellt der Reiter.
    /// <para>Kern: <c>Konditionierungsarbeit.SollwertprofilUebernehmen</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungErgebnis>? SollwertprofilUebernehmen { get; init; }

    /// <summary>
    /// <b>Das Zeitfenster „Tage, von, bis, Wert"</b> (Teilkonzept 3.5, 7.5): setzt den Wert in die
    /// Stunden der Standardwoche und lässt alles andere stehen; der Vermerk kommt in
    /// <see cref="KonditionierungKalender.Vermerk"/>.
    /// <para>Kern: <c>Konditionierungsarbeit.Zeitfenster</c> über
    /// <c>Kalenderwerkzeuge.Zeitfenster(kalender, tage, von, bis, wert)</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungZeitfenster, KonditionierungErgebnis>? Zeitfenster { get; init; }

    /// <summary>
    /// <b>Die Feiertage als Regel</b> (F11): die neun bundeseinheitlichen Feiertage „wie Wochentag X";
    /// dritter Parameter: der Wochentag 1 = Montag … 7 = Sonntag (Vorgabe 7).
    /// <para>Kern: <c>Konditionierungsarbeit.Feiertage</c> über
    /// <c>Kalenderwerkzeuge.Feiertagsregeln(kalender, wieWochentag)</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, int, KonditionierungErgebnis>? Feiertage { get; init; }

    /// <summary>
    /// <b>„Zeitstruktur übernehmen"</b> (Festlegung 11): Kühlen, Lüftung oder Geräte „wie Heizung" oder
    /// „wie Anwesenheit"; ersetzt wird nur die Standardwoche.
    /// <para>Kern: <c>Konditionierungsarbeit.Zeitstruktur</c> (rein; <c>ZeitstrukturTests</c>).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungZeitstruktur, KonditionierungErgebnis>? Zeitstruktur { get; init; }

    // ------------------------------------------------------------------ Befunde, Bilder, Prüfung

    /// <summary>
    /// <b>Der Rückfragebefund VOR dem Schreiben</b> (Festlegung 3): was eine Handlung ersetzt, was
    /// bleibt und welche Zonen es betrifft, mit Namen; <c>null</c> = keine Rückfrage nötig. Ohne
    /// Delegat fragt der Dialog ohne Einzelheiten.
    /// <para>Kern: <c>Konditionierungsarbeit.Rueckfrage</c> (rein) — der Befund entsteht am Arbeitsstand,
    /// VOR dem Schreiben.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungHandlung, KonditionierungRueckfrage?>? Rueckfrage { get; init; }

    /// <summary>
    /// <b>Die Rückfrage von „Speichern unter" im Projekt</b> (Festlegung 3): Der neue Katalogsatz nimmt nur
    /// die Gebäudeebene mit — die Frage nennt Zonen, ihre Bauteile und ihre Konditionierung zusammen
    /// (<see cref="KonditionierungRueckfrage.Bleibt"/>, <see cref="KonditionierungPostenart.Bauteile"/>);
    /// <c>null</c> = keine Rückfrage nötig. Ohne Delegat fragt der Dialog ohne Einzelheiten.
    /// <para>Kern: <c>Konditionierungsarbeit.RueckfrageSpeichernUnter</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungRueckfrage?>? SpeichernUnterRueckfrage { get; init; }

    /// <summary>
    /// <b>Die Vorschau der Woche</b> (Teilkonzept 7.5): 168 Werte einer Größe in der Einheit ihrer
    /// Spalte, <see cref="double.NaN"/> = „aus" (als Lücke) → Zeichenmodell für <c>DiagrammSvg</c>;
    /// <c>null</c> = kein Bild. Der Reiter reicht ihn als <c>Vorschau</c> an das Wochenraster.
    /// <para>Kern: <c>ChartRenderer.StundenprofilModell</c> mit dem Titel der Größe („Vorschau: Woche ·
    /// Kühlen") und der Lücke für „aus" (Befund B12).</para>
    /// </summary>
    public Func<KonditionierungGroesse, double[], Zeichenmodell?>? WochenVorschau { get; init; }

    /// <summary>
    /// <b>Das Teppichbild</b> (Teilkonzept 7.5, Festlegung 8): Tage × Stunden des Kalenders, „aus" als
    /// eigene Fläche, die Quelle je Tag am Element; <c>null</c> = kein Bild.
    /// <para>Kern: <c>Konditionierungskalender.Auswerten</c>/<c>Quelle</c> und der Renderer
    /// <c>Kalenderteppich</c> mit dem Bezugsjahr <c>Konditionierungdatenweg.Bezugsjahr</c>; die Hülle
    /// belegt den Delegaten mit dem Karteninhalt (Welle U3).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, Zeichenmodell?>? Teppichbild { get; init; }

    /// <summary>
    /// Das Bezugsjahr, gegen das Vorschau und Teppichbild Wochentage und Feiertage auflösen — im Projekt das
    /// des Laufs, im Katalog das der Vorgabe (Festlegung 8); die Karte nennt es unter dem Teppichbild.
    /// <c>null</c> = keins.
    /// <para>Kern: <c>Konditionierungsarbeitsstand.Referenzjahr</c> über den Bezug der Hülle.</para>
    /// </summary>
    public int? Bezugsjahr { get; init; }

    /// <summary>
    /// <b>Die Herleitung der Lasten</b> (P1, Teilkonzept 7.2) für die Nennwertzeile und die Zeile der
    /// Jahresmittel; zweiter Parameter: die Zone (<c>null</c> = Gebäude bzw. Katalogbau).
    /// <para>Kern: <c>Konditionierungsarbeit.PersonenNennwertVorschlag</c>, <c>PersonenJahresmittelW</c>,
    /// <c>Matrixeingang.PERSON_W</c> — gegen Referenzjahr und w₀ des Arbeitsstands.</para>
    /// </summary>
    public Func<KonditionierungStand, int?, KonditionierungLasten?>? Lasten { get; init; }

    /// <summary>
    /// <b>Die Prüfregeln des Kerns</b> über die Konditionierung des Stands (Grenzen je Größe, Rundlauf
    /// auf vier Nachkommastellen, eindeutiger Rang, höchstens 64 Perioden) — der OK-Weg fragt sie VOR
    /// dem ersten Schritt; leer = gültig. Kein Delegat = die Prüfung läuft allein im Schreibweg.
    /// <para>Kern: <c>Konditionierungsarbeit.Zellenpruefung</c>, <c>Kalenderleser.Rundlaeuft</c>,
    /// <c>Kalenderwerkzeuge.Rangpruefung</c>, <c>Kalenderregel.PERIODEN_MAX</c>.</para>
    /// </summary>
    public Func<KonditionierungStand, string>? Pruefen { get; init; }

    /// <summary>
    /// Warum die Konditionierung nicht zur Verfügung steht — die Datenbank trägt ihre Tabellen nicht
    /// (<c>KonditionierungSchema.Lesbar()</c>; Text <c>KOND_TXT_GRUND_OHNE_TABELLEN</c>); <c>null</c> =
    /// sie steht. Dann ist der Reiter benannt gesperrt (Festlegung 6), auch wenn Delegaten da sind.
    /// </summary>
    public string? Sperre { get; init; }

    /// <summary>
    /// <b>Bietet der Reiter den Knopf dieser Handlung an?</b> Nur, wenn ihr Delegat da ist und keine
    /// <see cref="Sperre"/> steht („kein Delegat, kein Knopf"). „Übernehmen" einer Vorlage braucht dazu
    /// die Liste, aus der gewählt wird, „Kopieren nach …" die erlaubten Ziele.
    /// </summary>
    public bool Bietet(KonditionierungHandlung handlung)
    {
        if (Sperre is not null) return false;
        return handlung switch
        {
            KonditionierungHandlung.ZelleSetzen => ZelleSetzen is not null,
            KonditionierungHandlung.Anlegen => Anlegen is not null,
            KonditionierungHandlung.Verwerfen => Verwerfen is not null,
            KonditionierungHandlung.MatrixErneut => MatrixErneut is not null,
            KonditionierungHandlung.VorlageUebernehmen => VorlageUebernehmen is not null && Vorlagen is not null,
            KonditionierungHandlung.AlsVorlageSpeichern => AlsVorlageSpeichern is not null,
            KonditionierungHandlung.VorlageUmbenennen => VorlageUmbenennen is not null,
            KonditionierungHandlung.VorlageLoeschen => VorlageLoeschen is not null,
            KonditionierungHandlung.VorlageDuplizieren => VorlageDuplizieren is not null,
            KonditionierungHandlung.Zeitfenster => Zeitfenster is not null,
            KonditionierungHandlung.Feiertage => Feiertage is not null,
            KonditionierungHandlung.Zeitstruktur => Zeitstruktur is not null,
            KonditionierungHandlung.KatalogErneut => KatalogErneut is not null,
            KonditionierungHandlung.LuftwechselAufteilen => LuftwechselAufteilen is not null,
            KonditionierungHandlung.VomGebaeude => VomGebaeude is not null,
            KonditionierungHandlung.Grundangabe => Grundangabe is not null,
            KonditionierungHandlung.Standardwoche => Standardwoche is not null,
            KonditionierungHandlung.PeriodeSetzen => PeriodeSetzen is not null,
            KonditionierungHandlung.RangVerschieben => RangVerschieben is not null,
            KonditionierungHandlung.PeriodeLoeschen => PeriodeLoeschen is not null,
            KonditionierungHandlung.SollwertprofilUebernehmen => SollwertprofilUebernehmen is not null,
            KonditionierungHandlung.VorlageKopieren => VorlageKopieren is not null && Kopierziele is not null,
            _ => false
        };
    }
}
