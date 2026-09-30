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
/// <para><b>Wer die Delegaten trägt.</b> Die Hülle füllt das Bündel in Welle K2
/// (<c>EPOS.UI.Daten/Bedarf/</c>) aus den reinen Funktionen des Kerns, die K2 aus den
/// Datenbankwegen von KP1 herauszieht (Arbeitsstand <c>Konditionierungsarbeit</c>, Entwurf KP2
/// Abschnitt 2); je Delegat nennt sein Kommentar den heutigen Kernweg. Die Übersetzung zwischen den
/// Typen dieser Seite und dem Kern (<c>Vorgabematrix</c>, <c>Matrixzelle</c>,
/// <c>Konditionierungskalender</c>, <c>Kalenderregel</c>, <c>KonditionierungCtrl.Eigner</c>) steht
/// allein in der Hülle. Gebunden wird das Bündel vom Reiter in Welle U1.</para>
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
    /// <para>Kern heute: <c>KonditionierungCtrl.Anlegen</c> über <c>Standardfahrplan.Erzeugen(matrix,
    /// größe, rundlaufPruefen: true)</c>, <c>PersonenNennwertVorschlag</c>,
    /// <c>GeraeteNennwertNachPersonen</c>; K2: „Anlegen" des Arbeitsstands.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungErgebnis>? Anlegen { get; init; }

    /// <summary>
    /// <b>„Verwerfen"</b> (Teilkonzept 3.3): Der angelegte Kalender der Größe fällt samt Perioden, die
    /// Matrix bleibt; danach ist er wieder abgeleitet.
    /// <para>Kern heute: <c>KonditionierungCtrl.Verwerfen</c>; K2: „Verwerfen" des Arbeitsstands.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungErgebnis>? Verwerfen { get; init; }

    /// <summary>
    /// <b>„Matrix erneut anwenden…"</b> (P12 (a)): ersetzt am angelegten Kalender nur den Matrixbereich
    /// — Standardwoche, Ferien- und Saisonperioden —; eigene Perioden und Ausnahmetage bleiben. Die
    /// Rückfrage kommt vorher (<see cref="Rueckfrage"/>).
    /// <para>Kern heute: <c>KonditionierungCtrl.ErneutAnwenden</c> und <c>IstMatrixbereich</c>; K2:
    /// „Matrix erneut anwenden" des Arbeitsstands.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungErgebnis>? MatrixErneut { get; init; }

    /// <summary>
    /// <b>„Aus dem Katalog erneut übernehmen…"</b> (Festlegung 4, nur Projekt): ersetzt die ganze
    /// Gebäudeebene samt Bestandszellen, Nachtzeiten und Ferienzeiträumen; die Zonen bleiben.
    /// <para>Kern heute: <c>GebaeudeStammCtrl.KonditionierungErneutUebernehmen</c> über
    /// <c>Konditionierungskopie.Kopieren</c> (Datenbank, Befund B9); K2: „erneut übernehmen" des
    /// Arbeitsstands.</para>
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

    // ------------------------------------------------------------------ Vorlagen je Größe

    /// <summary>
    /// <b>Die Vorlagen einer Größe</b> für die Auswahlliste der Karte (Teilkonzept 7.4) — die
    /// ausgelieferten zuerst.
    /// <para>Kern heute: <c>KonditionierungsvorlageCtrl.Liste(größe)</c> (bleibt ein Leseweg).</para>
    /// </summary>
    public Func<KonditionierungGroesse, IReadOnlyList<KonditionierungVorlageDaten>>? Vorlagen { get; init; }

    /// <summary>
    /// <b>Eine Vorlage übernehmen</b> (P11, P12): Die Zellen der Vorlage gehen in die Matrixspalte
    /// (eine leere Zelle lässt die des Ziels), der Kalender der Größe wird angelegt — Generator mit den
    /// Ferienzeiträumen des Ziels, darüber Woche und Perioden der Vorlage —, die Herkunft steht in
    /// <see cref="KonditionierungKalender.Vorlage"/>. Dritter Parameter: die Id der Vorlage.
    /// <para>Kern heute: <c>KonditionierungsvorlageCtrl.Uebernehmen(idVorlage, ziel, matrix)</c> mit
    /// dem Zusammenführen Generator + Vorlage + Bestand (Datenbank); K2: „Vorlage übernehmen" des
    /// Arbeitsstands.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, long, KonditionierungErgebnis>? VorlageUebernehmen { get; init; }

    /// <summary>
    /// <b>„Als Vorlage speichern…"</b> (E54): legt aus der Spalte und, falls angelegt, dem Kalender
    /// der Größe eine eigene Vorlage an — ohne Nennwert und Saison. Schreibt sofort (Festlegung 13);
    /// ein Doppelname in der Liste ist eine benannte Ablehnung.
    /// <para>Kern heute: <c>KonditionierungsvorlageCtrl.Speichern(quelle, größe, name, beschreibung,
    /// nutzung, out id)</c> mit der Datenbank als Quelle und <c>Namenspruefung</c>; K2: der
    /// Als-Vorlage-Inhalt aus dem Arbeitsstand.</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungVorlageEingabe, KonditionierungVorlageErgebnis>? AlsVorlageSpeichern { get; init; }

    /// <summary>
    /// <b>Eine eigene Vorlage umbenennen</b> (Vorlagenverwaltung, F4): Id und neuer Name; schreibt
    /// sofort. <para>Kern heute: <c>KonditionierungsvorlageCtrl.Umbenennen</c>.</para>
    /// </summary>
    public Func<long, string, KonditionierungVorlageErgebnis>? VorlageUmbenennen { get; init; }

    /// <summary>
    /// <b>Eine eigene Vorlage löschen</b> (Vorlagenverwaltung, F4); kein Gebäude wird berührt.
    /// <para>Kern heute: <c>KonditionierungsvorlageCtrl.Loeschen</c>.</para>
    /// </summary>
    public Func<long, KonditionierungVorlageErgebnis>? VorlageLoeschen { get; init; }

    /// <summary>
    /// <b>Eine Vorlage duplizieren</b> (Vorlagenverwaltung, F4) — auch eine ausgelieferte; Id und Name
    /// der Kopie. <para>Kern heute: <c>KonditionierungsvorlageCtrl.Duplizieren(id, name, out neueId)</c>.</para>
    /// </summary>
    public Func<long, string, KonditionierungVorlageErgebnis>? VorlageDuplizieren { get; init; }

    // ------------------------------------------------------------------ Werkzeuge der Karte

    /// <summary>
    /// <b>Das Zeitfenster „Tage, von, bis, Wert"</b> (Teilkonzept 3.5, 7.5): setzt den Wert in die
    /// Stunden der Standardwoche und lässt alles andere stehen; der Vermerk kommt in
    /// <see cref="KonditionierungKalender.Vermerk"/>.
    /// <para>Kern: <c>Kalenderwerkzeuge.Zeitfenster(kalender, tage, von, bis, wert)</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungZeitfenster, KonditionierungErgebnis>? Zeitfenster { get; init; }

    /// <summary>
    /// <b>Die Feiertage als Regel</b> (F11): die neun bundeseinheitlichen Feiertage „wie Wochentag X";
    /// dritter Parameter: der Wochentag 1 = Montag … 7 = Sonntag (Vorgabe 7).
    /// <para>Kern: <c>Kalenderwerkzeuge.Feiertagsregeln(kalender, wieWochentag)</c> (rein).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, int, KonditionierungErgebnis>? Feiertage { get; init; }

    /// <summary>
    /// <b>„Zeitstruktur übernehmen"</b> (Festlegung 11): Kühlen, Lüftung oder Geräte „wie Heizung" oder
    /// „wie Anwesenheit"; ersetzt wird nur die Standardwoche.
    /// <para>Kern: neu mit Welle K1 neben <c>Kalenderwerkzeuge</c> (<c>ZeitstrukturTests</c>).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, KonditionierungZeitstruktur, KonditionierungErgebnis>? Zeitstruktur { get; init; }

    // ------------------------------------------------------------------ Befunde, Bilder, Prüfung

    /// <summary>
    /// <b>Der Rückfragebefund VOR dem Schreiben</b> (Festlegung 3): was eine Handlung ersetzt, was
    /// bleibt und welche Zonen es betrifft, mit Namen; <c>null</c> = keine Rückfrage nötig. Ohne
    /// Delegat fragt der Dialog ohne Einzelheiten.
    /// <para>Kern heute: der Befund entsteht erst NACH dem Kopieren (<c>Konditionierungskopie.Befund</c>,
    /// Befund B9); K2: die Rückfragebefunde des Arbeitsstands.</para>
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
    /// <para>Kern: <c>ChartRenderer.StundenprofilModell</c> (die Lücke für „aus" mit Welle K4, Befund
    /// B12).</para>
    /// </summary>
    public Func<KonditionierungGroesse, double[], Zeichenmodell?>? WochenVorschau { get; init; }

    /// <summary>
    /// <b>Das Teppichbild</b> (Teilkonzept 7.5, Festlegung 8): Tage × Stunden des Kalenders, „aus" als
    /// eigene Fläche, die Quelle je Tag am Element; <c>null</c> = kein Bild.
    /// <para>Kern: <c>Konditionierungskalender.Auswerten</c>/<c>Quelle</c> und der Renderer der Welle K4
    /// (<c>Kalenderteppich</c>).</para>
    /// </summary>
    public Func<KonditionierungStand, KonditionierungOrt, Zeichenmodell?>? Teppichbild { get; init; }

    /// <summary>
    /// <b>Die Herleitung der Lasten</b> (P1, Teilkonzept 7.2) für die Nennwertzeile und die Zeile der
    /// Jahresmittel; zweiter Parameter: die Zone (<c>null</c> = Gebäude bzw. Katalogbau).
    /// <para>Kern: <c>KonditionierungCtrl.PersonenNennwertVorschlag</c>, <c>PersonenJahresmittelW</c>,
    /// <c>GeraeteNennwertNachPersonen</c>, <c>Matrixeingang.PERSON_W</c>.</para>
    /// </summary>
    public Func<KonditionierungStand, int?, KonditionierungLasten?>? Lasten { get; init; }

    /// <summary>
    /// <b>Die Prüfregeln des Kerns</b> über die Konditionierung des Stands (Grenzen je Größe, Rundlauf
    /// auf vier Nachkommastellen, eindeutiger Rang, höchstens 64 Perioden) — der OK-Weg fragt sie VOR
    /// dem ersten Schritt; leer = gültig. Kein Delegat = die Prüfung läuft allein im Schreibweg.
    /// <para>Kern: <c>KonditionierungCtrl.ZellePruefen</c>, <c>Kalenderwoche.Rundlauf</c>,
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
    /// die Liste, aus der gewählt wird.
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
            _ => false
        };
    }
}
