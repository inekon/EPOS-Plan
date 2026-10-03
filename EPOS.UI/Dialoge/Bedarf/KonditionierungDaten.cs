namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  DIE DATENSEITE DER KONDITIONIERUNG (Stufe KP2, Welle U0b; Entwurf KP2 Abschnitt 2,
//  Teilkonzept Konditionierungsprofile 3.2 bis 3.5 und 7). Nur Daten: keine Datenbank,
//  keine Fachklasse des Kerns. Die Hülle in EPOS.UI.Daten (Welle K2) übersetzt zwischen
//  diesen Typen und dem Kern (Vorgabematrix, Matrixzelle, Konditionierungskalender,
//  Kalenderregel), der Reiter „Konditionierung" (Welle U1) bindet sie. Der Weg dazu ist
//  das Delegatenbündel KonditionierungWeg.
// =====================================================================================

/// <summary>
/// <b>Die fünf Größen der Konditionierung</b> — die Spalten der Vorgabe-Matrix und die
/// Kalenderkarten (Teilkonzept 3.1). Eine Aufzählung der Oberfläche; die Hülle bildet sie auf
/// <c>Konditionierungsgroesse</c> des Kerns ab — dieselbe Reihenfolge wie
/// <c>Konditionierungsgroessen.Alle</c> und <c>DbWerte.KOND_GROESSEN</c>.
/// </summary>
public enum KonditionierungGroesse
{
    /// <summary>Der Heizsollwert [°C]; „aus" heißt: ohne Heizung.</summary>
    Heizen = 0,

    /// <summary>Der Kühlsollwert [°C]; „aus" heißt: ohne Kühlung.</summary>
    Kuehlen = 1,

    /// <summary>Die Nutzerlüftung [1/h], absolut; „aus" heißt 0 1/h.</summary>
    Lueftung = 2,

    /// <summary>Geräte und Anlage als Anteil eines Nennwerts [W]; „aus" heißt 0 %.</summary>
    Geraete = 3,

    /// <summary>Die Anwesenheit der Personen als Anteil eines Nennwerts [W]; „aus" heißt 0 %.</summary>
    Personen = 4
}

/// <summary>
/// <b>Die sechs Zeilen der Vorgabe-Matrix</b> (Teilkonzept 3.3) — dieselbe Reihenfolge wie
/// <c>DbWerte.KOND_ZEILEN</c>. Die Zeile „Vorlage" der Skizze (7.2) ist keine Zelle, sie nennt die
/// Herkunft des Kalenders (<see cref="KonditionierungKalender.Vorlage"/>).
/// </summary>
public enum KonditionierungZeile
{
    /// <summary>Nennwert: Infiltration [1/h] bei der Lüftung, Nennleistung [W] bei Geräten und Personen.</summary>
    Nennwert = 0,

    /// <summary>Tag: der Werktag außerhalb des Nachtfensters.</summary>
    Tag = 1,

    /// <summary>Nacht: Wert und Nachtfenster von–bis (Stunde 0 … 23), an der Lüftung dazu ΔT.</summary>
    Nacht = 2,

    /// <summary>Wochenende: Samstag und Sonntag ganztägig.</summary>
    Wochenende = 3,

    /// <summary>Ferien: der Wert in den Ferienzeiträumen; die Zeiträume stehen am Gebäude.</summary>
    Ferien = 4,

    /// <summary>Saison: Start und Ende der Heiz- bzw. Kühlperiode als Tag im Gemeinjahr (E53).</summary>
    Saison = 5
}

/// <summary>Der Zustand eines Kalenders (Teilkonzept 3.3, 3.4) — die eine Zeile der eingeklappten Karte.</summary>
public enum KonditionierungZustand
{
    /// <summary>Keine Kalenderzeile: Der Lauf erzeugt den Kalender aus der Matrix („aus der Matrix").</summary>
    Abgeleitet = 0,

    /// <summary>Der Kalender ist angelegt; die Matrix ist dann nur Vorgabe.</summary>
    Angelegt = 1,

    /// <summary>Nur an einer Zone: Sie folgt dem angelegten Kalender des Gebäudes („vom Gebäude").</summary>
    VomGebaeude = 2
}

/// <summary>
/// Die Art einer Periode (Teilkonzept 3.2) — sie ordnet und benennt, gerechnet wird mit ihr nicht.
/// Dieselbe Reihenfolge wie <c>DbWerte.KOND_ARTEN</c>.
/// </summary>
public enum KonditionierungPeriodenart
{
    /// <summary>Ein eigener Zeitraum des Anwenders.</summary>
    Zeitraum = 0,

    /// <summary>Ein Ferienzeitraum — vom Generator aus den Ferienzeiträumen des Gebäudes (Matrixbereich).</summary>
    Ferien = 1,

    /// <summary>Eine Feiertagsregel ohne Datum (F11).</summary>
    Feiertag = 2,

    /// <summary>Die Tage außerhalb der Saison — vom Generator (Matrixbereich, E53).</summary>
    Betriebspause = 3
}

/// <summary>
/// Welcher Art eine Angabe ist (Teilkonzept 3.2) — dieselbe Reihenfolge wie <c>Angabeart</c> des
/// Kerns. Die Grundangabe eines Kalenders ist <see cref="Wert"/>, <see cref="Aus"/> oder
/// <see cref="Woche"/>; nur eine Periode kennt <see cref="WieWochentag"/>.
/// </summary>
public enum KonditionierungAngabe
{
    /// <summary>Ein Wert für alle Stunden.</summary>
    Wert = 0,

    /// <summary>„aus" — die Größe wirkt nicht.</summary>
    Aus = 1,

    /// <summary>Eine Woche mit 168 Werten, <see cref="double.NaN"/> für „aus".</summary>
    Woche = 2,

    /// <summary>„wie Wochentag X" (1 = Montag … 7 = Sonntag) — die Stunden dieses Tags aus der Standardwoche.</summary>
    WieWochentag = 3
}

/// <summary>Die Nutzung einer Vorlage (<c>DbWerte.KOND_NUTZUNGEN</c>; <see cref="Keine"/> = ohne Angabe).</summary>
public enum KonditionierungNutzung
{
    /// <summary>Ohne Angabe (Spalte leer).</summary>
    Keine = 0,

    /// <summary>Wohnen.</summary>
    Wohnen = 1,

    /// <summary>Büro.</summary>
    Buero = 2,

    /// <summary>Schule.</summary>
    Schule = 3,

    /// <summary>Sonstige.</summary>
    Sonstige = 4
}

/// <summary>Die Quelle des Werkzeugs „Zeitstruktur übernehmen" (Entwurf KP2, Festlegung 11).</summary>
public enum KonditionierungZeitstruktur
{
    /// <summary>„wie Heizung": die Stunden mit endlichem Wert ≥ Tagwert der Heizspalte.</summary>
    WieHeizung = 0,

    /// <summary>„wie Anwesenheit": die Stunden mit einem Personenanteil &gt; 0.</summary>
    WieAnwesenheit = 1
}

/// <summary>
/// <b>Die Handlungen der Konditionierung</b> — je Handlung ein Delegat im
/// <see cref="KonditionierungWeg"/> („kein Delegat, kein Knopf", <see cref="KonditionierungWeg.Bietet"/>)
/// und der Anlass eines Rückfragebefunds (<see cref="KonditionierungWeg.Rueckfrage"/>).
/// „Zurücknehmen" steht nicht hier: Der Reiter hält den Stand vor der letzten Handlung selbst.
/// </summary>
public enum KonditionierungHandlung
{
    /// <summary>Eine Zelle der Matrix setzen (samt Merker, Folgeregel F2 und Aufteilung des Luftwechsels F5).</summary>
    ZelleSetzen = 0,

    /// <summary>„Kalender anlegen" aus der Matrix.</summary>
    Anlegen = 1,

    /// <summary>„Verwerfen" — zurück zur Matrix.</summary>
    Verwerfen = 2,

    /// <summary>„Matrix erneut anwenden…" — ersetzt nur den Matrixbereich (P12).</summary>
    MatrixErneut = 3,

    /// <summary>„Übernehmen" einer Vorlage der Größe (P11, P12).</summary>
    VorlageUebernehmen = 4,

    /// <summary>„Als Vorlage speichern…" — schreibt sofort mit eigenem OK (Festlegung 13).</summary>
    AlsVorlageSpeichern = 5,

    /// <summary>Eine eigene Vorlage umbenennen (Vorlagenverwaltung, F4) — schreibt sofort.</summary>
    VorlageUmbenennen = 6,

    /// <summary>Eine eigene Vorlage löschen (Vorlagenverwaltung, F4) — schreibt sofort.</summary>
    VorlageLoeschen = 7,

    /// <summary>Eine Vorlage duplizieren (Vorlagenverwaltung, F4) — schreibt sofort.</summary>
    VorlageDuplizieren = 8,

    /// <summary>Das Werkzeug „Zeitfenster eintragen" (Tage, von, bis, Wert).</summary>
    Zeitfenster = 9,

    /// <summary>Das Werkzeug „Feiertage als Regel anlegen" (F11).</summary>
    Feiertage = 10,

    /// <summary>Das Werkzeug „Zeitstruktur übernehmen" (Festlegung 11).</summary>
    Zeitstruktur = 11,

    /// <summary>„Aus dem Katalog erneut übernehmen…" — nur im Projekt (Festlegung 4).</summary>
    KatalogErneut = 12,

    /// <summary>
    /// „Aufteilen" — die Antwort auf die Rückfrage nach E56 F5 (a): die Gesamtangabe <c>Luftwechselrate</c>
    /// wird Infiltration = min(0,3 1/h; Rate) und Nutzerlüftung = Rest; die Summe bleibt.
    /// </summary>
    LuftwechselAufteilen = 13,

    /// <summary>
    /// „Vom Gebäude übernehmen und anpassen" — nur an einer Zone (Teilkonzept 3.4, 7.3): legt eine eigene
    /// Kopie des angelegten Gebäudekalenders an (Stufe KP2, Welle U4).
    /// </summary>
    VomGebaeude = 14,

    /// <summary>Die Grundangabe der Karte setzen — ein Wert oder „aus"; eine Standardwoche fällt (Welle U3).</summary>
    Grundangabe = 15,

    /// <summary>Die Standardwoche der Karte setzen (Wochenraster) oder verwerfen (Welle U3).</summary>
    Standardwoche = 16,

    /// <summary>Eine eigene Periode anlegen oder ersetzen — Zeitraum oder Feiertag (Festlegung 15, Welle U3).</summary>
    PeriodeSetzen = 17,

    /// <summary>Rang ▲▼ einer eigenen Periode im Eigenband (Festlegung 15, Welle U3).</summary>
    RangVerschieben = 18,

    /// <summary>Eine eigene Periode löschen (Festlegung 15, Welle U3).</summary>
    PeriodeLoeschen = 19,

    /// <summary>
    /// „In den Kalender übernehmen" an der Wärmeübergabe (Teilkonzept 5.5, Welle U3): das Sollwert-Zeitprogramm
    /// wird die Standardwoche des Heizkalenders.
    /// </summary>
    SollwertprofilUebernehmen = 20,

    /// <summary>
    /// „Kopieren nach …" (Vorlagenverwaltung, Teilkonzept 3.5, 7.4): eine Vorlage als eigene Vorlage einer
    /// anderen Größe — Heizen → Kühlen, Geräte ↔ Personen; schreibt sofort.
    /// </summary>
    VorlageKopieren = 21
}

/// <summary>
/// <b>Eine Zelle der Vorgabe-Matrix</b> (Teilkonzept 3.3, 5.6) — wie die Vorgabezeile des Kerns
/// (<c>Matrixzelle</c>): Wert oder „aus", dazu Zeiten und ΔT. Jedes Feld darf leer sein und heißt
/// dann „wie die Ebene darüber" (Zone → Gebäude → Vorgabe des Programms, 3.4).
/// </summary>
/// <remarks>
/// <para><b>Eine Wahrheit.</b> Die neun Bestandszellen (<see cref="KonditionierungDaten.Bestandsfeld"/>)
/// tragen ihren Wert im Feld von <see cref="GebaeudeKatalogDaten"/>, nicht hier; ihr <see cref="Wert"/>
/// bleibt im gespeicherten Stand leer, nur „aus", Zeiten und ΔT stehen hier (Konzept 5.6: die
/// Vorgabezeile einer Bestandszelle trägt nur „aus", die Zeiten und <c>Bedingt_K</c>). Ebenso stehen
/// die Nachtzeiten der Heizspalte an Gebäude und Katalogbau in
/// <see cref="GebaeudeKatalogDaten.NachtBeginn"/>/<see cref="GebaeudeKatalogDaten.NachtEnde"/>
/// (<see cref="KonditionierungDaten.Bestandszeiten"/>, Entwurf KP2 Festlegung 5).</para>
/// <para>Als ANFRAGE an <see cref="KonditionierungWeg.ZelleSetzen"/> trägt die Zelle dagegen den Wert
/// auch einer Bestandszelle — die Hülle legt ihn dann in das Feld des Feldsatzes.</para>
/// </remarks>
public sealed class KonditionierungZelle
{
    /// <summary>Der Wert in der Einheit der Spalte (Anteile in Prozent); <c>null</c> = leer.</summary>
    public double? Wert { get; set; }

    /// <summary>Steht die Zelle auf „aus" (P2)? Dann gilt <see cref="Wert"/> nicht.</summary>
    public bool Aus { get; set; }

    /// <summary>
    /// Zeile Nacht: die erste Stunde 0 … 23 des Nachtfensters; Zeile Saison: der Starttag 1 … 365
    /// im Gemeinjahr; sonst <c>null</c>. Leer heißt beim Nachtfenster „das der Heizspalte" (F19).
    /// </summary>
    public int? Von { get; set; }

    /// <summary>
    /// Zeile Nacht: die Stunde 0 … 23, vor der das Nachtfenster endet; Zeile Saison: der Endtag
    /// 1 … 365 (nach dem Start heißt über den Jahreswechsel); sonst <c>null</c>.
    /// </summary>
    public int? Bis { get; set; }

    /// <summary>Nur Lüftung/Nacht: ΔT der Nachtauskühlung [K]; <c>null</c> = 2 K (P9).</summary>
    public double? DeltaT { get; set; }

    /// <summary>Ist die Zelle in jedem Feld leer — „wie die Ebene darüber"?</summary>
    public bool Leer => !Wert.HasValue && !Aus && !Von.HasValue && !Bis.HasValue && !DeltaT.HasValue;

    /// <summary>Eine entkoppelte Kopie.</summary>
    public KonditionierungZelle Kopie() => (KonditionierungZelle)MemberwiseClone();
}

/// <summary>
/// <b>Eine Periode eines Kalenders</b> (Teilkonzept 3.2, 5.1) — ein Zeitraum mit Datum im Gemeinjahr
/// oder eine Feiertagsregel, Rang und Art, dazu genau eine Angabe. Perioden gelten ganze Tage; je
/// Stunde gewinnt die ranghöchste, die den Tag enthält.
/// </summary>
public sealed class KonditionierungPeriode
{
    /// <summary>Der Rang 1 … 999, je Kalender eindeutig; die größere Zahl gewinnt.</summary>
    public int Rang { get; set; }

    /// <summary>Die Art — sie ordnet und benennt.</summary>
    public KonditionierungPeriodenart Art { get; set; }

    /// <summary>Der Name, den die Vorschau als Quelle nennt („Quelle: Sommerferien").</summary>
    public string Name { get; set; } = "";

    /// <summary>Der erste Tag 1 … 365 im Gemeinjahr; <c>null</c> bei einer Feiertagsregel.</summary>
    public int? Von { get; set; }

    /// <summary>Der letzte Tag 1 … 365; nach <see cref="Von"/> heißt über den Jahreswechsel; <c>null</c> bei einer Feiertagsregel.</summary>
    public int? Bis { get; set; }

    /// <summary>Die Feiertagsregel als Persistenzwert (<c>DbWerte.KOND_FEIERTAG_*</c>), nie ein Anzeigetext; <c>null</c> bei einem Zeitraum.</summary>
    public string? Feiertagsregel { get; set; }

    /// <summary>Welche Angabe die Periode trägt.</summary>
    public KonditionierungAngabe Angabe { get; set; }

    /// <summary>Der Wert bei <see cref="KonditionierungAngabe.Wert"/> (Anteile in Prozent), sonst <c>null</c>.</summary>
    public double? Wert { get; set; }

    /// <summary>
    /// Die eigene Woche bei <see cref="KonditionierungAngabe.Woche"/>: 168 Werte ab Montag 00:00 in der
    /// Einheit der Spalte, <see cref="double.NaN"/> = „aus"; sonst <c>null</c>.
    /// </summary>
    public double[]? Woche { get; set; }

    /// <summary>Der Wochentag 1 = Montag … 7 = Sonntag bei <see cref="KonditionierungAngabe.WieWochentag"/>, sonst <c>null</c>.</summary>
    public int? WieWochentag { get; set; }

    /// <summary>
    /// Gehört die Periode zum Matrixbereich (Ferien und Saison, P12)? Gesetzt von der Hülle nach
    /// <c>KonditionierungCtrl.IstMatrixbereich</c> — die Periodenliste zeigt sie nur lesbar
    /// (Festlegung 15); „Matrix erneut anwenden" ersetzt sie.
    /// </summary>
    public bool Matrixbereich { get; set; }

    /// <summary>
    /// Steht die Periode im Band der eigenen Perioden (Rang 310 … 899)? Gesetzt von der Hülle nach
    /// <c>Kalenderwerkzeuge.ImEigenband</c> — nur dort verschiebt ▲▼ den Rang (Festlegung 15); eine
    /// Feiertagsregel im Band 100 … 108 behält ihn.
    /// </summary>
    public bool Eigenband { get; set; }

    /// <summary>Eine entkoppelte Kopie samt Woche.</summary>
    public KonditionierungPeriode Kopie()
    {
        var k = (KonditionierungPeriode)MemberwiseClone();
        k.Woche = (double[]?)Woche?.Clone();
        return k;
    }
}

/// <summary>
/// <b>Der Kalender einer Größe</b> (Teilkonzept 3.2) — Grundangabe (Wert oder „aus") oder
/// Standardwoche, darüber die Perioden; dazu Nennwert und Herkunft. Abgeleitet heißt: nichts ist
/// angelegt, die Felder sind leer — der Lauf erzeugt den Kalender aus der Matrix.
/// </summary>
public sealed class KonditionierungKalender
{
    /// <summary>Abgeleitet, angelegt oder (an einer Zone) vom Gebäude.</summary>
    public KonditionierungZustand Zustand { get; set; } = KonditionierungZustand.Abgeleitet;

    /// <summary>Die Grundangabe: <see cref="KonditionierungAngabe.Wert"/>, <see cref="KonditionierungAngabe.Aus"/> oder <see cref="KonditionierungAngabe.Woche"/>.</summary>
    public KonditionierungAngabe Angabe { get; set; }

    /// <summary>Der Wert der Grundangabe (Anteile in Prozent); <c>null</c> = keiner.</summary>
    public double? Wert { get; set; }

    /// <summary>
    /// Die Standardwoche: 168 Werte ab Montag 00:00 in der Einheit der Spalte (Anteile in Prozent),
    /// <see cref="double.NaN"/> = „aus"; <c>null</c> = keine.
    /// </summary>
    public double[]? Woche { get; set; }

    /// <summary>Der Nennwert [W] bei 100 % — nur Geräte und Personen; <c>null</c> = keiner (Geräte: <c>Interne_Waermegewinne</c>).</summary>
    public double? Nennwert { get; set; }

    /// <summary>
    /// Die Herkunft: der Name der zuletzt übernommenen Vorlage („aus Vorlage Büro", Zeile „Vorlage"
    /// der Matrix); <c>null</c> = keine. Mit <see cref="Vermerk"/> bildet sie die Spalte
    /// <c>Bemerkung</c> — „Herkunft · letzter Werkzeugvermerk" (Festlegung 5, Befund B8).
    /// </summary>
    public string? Vorlage { get; set; }

    /// <summary>Der Vermerk des zuletzt angewandten Werkzeugs; <c>null</c> = keiner.</summary>
    public string? Vermerk { get; set; }

    /// <summary>Die Perioden in Rangfolge, die ranghöchste zuerst.</summary>
    public List<KonditionierungPeriode> Perioden { get; set; } = new();

    /// <summary>Die eigenen Perioden — alle außerhalb des Matrixbereichs (der Kartenzustand zählt sie).</summary>
    public int EigenePerioden => Perioden.Count(p => !p.Matrixbereich);

    /// <summary>Eine entkoppelte Kopie samt Woche und Perioden.</summary>
    public KonditionierungKalender Kopie()
    {
        var k = (KonditionierungKalender)MemberwiseClone();
        k.Woche = (double[]?)Woche?.Clone();
        k.Perioden = Perioden.Select(p => p.Kopie()).ToList();
        return k;
    }
}

/// <summary>
/// <b>Eine Spalte der Vorgabe-Matrix samt dem Kalender ihrer Größe</b> — die sechs neuen Zellen
/// (Teilkonzept 3.3) und die Kalenderkarte (7.5).
/// </summary>
public sealed class KonditionierungSpalte
{
    /// <summary>Eine leere Spalte der Größe <paramref name="groesse"/>.</summary>
    public KonditionierungSpalte(KonditionierungGroesse groesse) => Groesse = groesse;

    /// <summary>Die Größe dieser Spalte.</summary>
    public KonditionierungGroesse Groesse { get; }

    /// <summary>Zeile Nennwert.</summary>
    public KonditionierungZelle Nennwert { get; set; } = new();

    /// <summary>Zeile Tag.</summary>
    public KonditionierungZelle Tag { get; set; } = new();

    /// <summary>Zeile Nacht — mit Nachtfenster und (Lüftung) ΔT.</summary>
    public KonditionierungZelle Nacht { get; set; } = new();

    /// <summary>Zeile Wochenende.</summary>
    public KonditionierungZelle Wochenende { get; set; } = new();

    /// <summary>Zeile Ferien.</summary>
    public KonditionierungZelle Ferien { get; set; } = new();

    /// <summary>Zeile Saison — Start und Ende als Tag im Gemeinjahr (nur Heizen und Kühlen).</summary>
    public KonditionierungZelle Saison { get; set; } = new();

    /// <summary>Der Kalender der Größe.</summary>
    public KonditionierungKalender Kalender { get; set; } = new();

    /// <summary>Die Zelle einer Zeile.</summary>
    public KonditionierungZelle Zelle(KonditionierungZeile zeile) => zeile switch
    {
        KonditionierungZeile.Nennwert => Nennwert,
        KonditionierungZeile.Tag => Tag,
        KonditionierungZeile.Nacht => Nacht,
        KonditionierungZeile.Wochenende => Wochenende,
        KonditionierungZeile.Ferien => Ferien,
        KonditionierungZeile.Saison => Saison,
        _ => throw new ArgumentOutOfRangeException(nameof(zeile))
    };

    /// <summary>Eine entkoppelte Kopie samt Zellen und Kalender.</summary>
    public KonditionierungSpalte Kopie() => new(Groesse)
    {
        Nennwert = Nennwert.Kopie(),
        Tag = Tag.Kopie(),
        Nacht = Nacht.Kopie(),
        Wochenende = Wochenende.Kopie(),
        Ferien = Ferien.Kopie(),
        Saison = Saison.Kopie(),
        Kalender = Kalender.Kopie()
    };
}

/// <summary>
/// <b>Die Konditionierung eines Gebäudes, eines Katalogbaus oder einer Zone im Arbeitsstand</b>
/// (Entwurf KP2 Abschnitt 2; Teilkonzept 3, 7) — je Größe die neuen Zellen der Matrix und der
/// Kalender, dazu eine <see cref="Fassung"/>. Ohne Kerntypen.
/// </summary>
/// <remarks>
/// <para><b>Wo sie steht.</b> Am Gebäude und am Katalogbau in <see cref="GebaeudeKatalogDaten.Konditionierung"/>,
/// an einer Zone in <see cref="ZoneDaten.Konditionierung"/> — so reist sie mit dem Feldsatz durch
/// den EINEN Schreibweg des Editors (<c>Speichern</c>, im Projekt Schritt 1 und 3), und Abbrechen
/// schreibt nichts. <c>null</c> heißt: Die Hülle reicht keine (ohne Konditionierungstabellen, ohne
/// Gaben); der Reiter nennt dann seinen Grund.</para>
/// <para><b>Die Fassung.</b> Jede Änderung zählt sie hoch (<see cref="Weiterzaehlen"/>): der Reiter,
/// wenn er einen neuen Stand aus einem Delegaten übernimmt oder selbst etwas ändert. Sie geht in den
/// Abdruck des Arbeitsstands ein (<c>GebaeudeArbeitsstand.Abdruck</c>, Befund B10 — der Abdruck
/// sieht die Listen dieser Klasse nicht) und in den Vergleich der Zonen
/// (<see cref="ZoneDaten.GleicheWerte"/>); so schreibt ein zweites OK nichts doppelt, und eine
/// geänderte Konditionierung wird geschrieben.</para>
/// </remarks>
public sealed class KonditionierungDaten
{
    private readonly KonditionierungSpalte[] _spalten;

    private static readonly KonditionierungGroesse[] GROESSEN =
    {
        KonditionierungGroesse.Heizen, KonditionierungGroesse.Kuehlen, KonditionierungGroesse.Lueftung,
        KonditionierungGroesse.Geraete, KonditionierungGroesse.Personen
    };

    /// <summary>Eine leere Konditionierung: fünf leere Spalten, alle Kalender abgeleitet.</summary>
    public KonditionierungDaten()
    {
        _spalten = new KonditionierungSpalte[GROESSEN.Length];
        foreach (KonditionierungGroesse g in GROESSEN) _spalten[(int)g] = new KonditionierungSpalte(g);
    }

    /// <summary>Die fünf Größen in ihrer festen Reihenfolge — der Index ist der Wert der Aufzählung.</summary>
    public static IReadOnlyList<KonditionierungGroesse> Alle => GROESSEN;

    /// <summary>Der Zähler der Änderungen; er geht in den Abdruck des Arbeitsstands ein.</summary>
    public int Fassung { get; set; }

    /// <summary>Die fünf Spalten in der Reihenfolge von <see cref="Alle"/>.</summary>
    public IReadOnlyList<KonditionierungSpalte> Spalten => _spalten;

    /// <summary>Die Spalte einer Größe.</summary>
    public KonditionierungSpalte Spalte(KonditionierungGroesse groesse) => _spalten[(int)groesse];

    /// <summary>Zählt die <see cref="Fassung"/> um eins hoch — nach jeder Änderung.</summary>
    public void Weiterzaehlen() => Fassung++;

    /// <summary>Eine TIEFE Kopie — Zellen, Kalender, Wochen und Perioden sind entkoppelt, die Fassung bleibt.</summary>
    public KonditionierungDaten Kopie()
    {
        var k = new KonditionierungDaten { Fassung = Fassung };
        for (int i = 0; i < _spalten.Length; i++) k._spalten[i] = _spalten[i].Kopie();
        return k;
    }

    /// <summary>
    /// <b>Das Feld von <see cref="GebaeudeKatalogDaten"/>, das den Wert einer Bestandszelle trägt</b>
    /// — <c>null</c>, wenn die Zelle keine Bestandsspalte hat und ihr Wert hier steht. Die neun
    /// Bestandszellen sind die des Kerns (<c>Matrixzellenort</c>, Teilkonzept 3.3 und 5.6):
    /// Heizen Tag, Nacht, Wochenende und Ferien, Kühlen Tag und Nacht, Lüftung Nennwert
    /// (Infiltration) und Tag (Nutzerlüftung), Geräte Nennwert (innere Wärmegewinne).
    /// </summary>
    public static string? Bestandsfeld(KonditionierungGroesse groesse, KonditionierungZeile zeile)
        => (groesse, zeile) switch
        {
            (KonditionierungGroesse.Heizen, KonditionierungZeile.Tag) => nameof(GebaeudeKatalogDaten.SollTag),
            (KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht) => nameof(GebaeudeKatalogDaten.NachtAbsenkung),
            (KonditionierungGroesse.Heizen, KonditionierungZeile.Wochenende) => nameof(GebaeudeKatalogDaten.WochenendAbsenkung),
            (KonditionierungGroesse.Heizen, KonditionierungZeile.Ferien) => nameof(GebaeudeKatalogDaten.SollFerien),
            (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag) => nameof(GebaeudeKatalogDaten.KuehlSollwert),
            (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Nacht) => nameof(GebaeudeKatalogDaten.KuehlSollwertNacht),
            (KonditionierungGroesse.Lueftung, KonditionierungZeile.Nennwert) => nameof(GebaeudeKatalogDaten.LuftwechselInfiltration),
            (KonditionierungGroesse.Lueftung, KonditionierungZeile.Tag) => nameof(GebaeudeKatalogDaten.LuftwechselNutzer),
            (KonditionierungGroesse.Geraete, KonditionierungZeile.Nennwert) => nameof(GebaeudeKatalogDaten.Waermegewinne),
            _ => null
        };

    /// <summary>Hat die Zelle eine Bestandsspalte (<see cref="Bestandsfeld"/>)?</summary>
    public static bool IstBestandszelle(KonditionierungGroesse groesse, KonditionierungZeile zeile)
        => Bestandsfeld(groesse, zeile) is not null;

    /// <summary>
    /// Die Felder von <see cref="GebaeudeKatalogDaten"/>, die an Gebäude und Katalogbau die Zeiten
    /// einer Zelle tragen — allein die Nachtzeile der Heizspalte (<c>Nachtabsenkung_Beginn/_Ende</c>,
    /// Festlegung 5); <c>null</c> sonst. An einer Zone stehen alle Zeiten in der Zelle.
    /// </summary>
    public static (string Von, string Bis)? Bestandszeiten(KonditionierungGroesse groesse, KonditionierungZeile zeile)
        => groesse == KonditionierungGroesse.Heizen && zeile == KonditionierungZeile.Nacht
            ? (nameof(GebaeudeKatalogDaten.NachtBeginn), nameof(GebaeudeKatalogDaten.NachtEnde))
            : null;
}

// =====================================================================================
//  Anfrage und Ergebnis der Handlungen (KonditionierungWeg)
// =====================================================================================

/// <summary>
/// <b>Der Teil des Arbeitsstands, den eine Handlung der Konditionierung liest und ändert</b> — der
/// Feldsatz des Gebäudes bzw. Katalogbaus (mit den neun Bestandszellen, den Nachtzeiten der
/// Heizspalte, den Ferienzeiträumen und <see cref="GebaeudeKatalogDaten.Konditionierung"/>) und die
/// Zonen (jede mit ihrer <see cref="ZoneDaten.Konditionierung"/>; ein Katalogsatz trägt keine).
/// </summary>
/// <remarks>
/// Ein Delegat ändert nie, was er bekommt: Er gibt einen NEUEN Stand zurück, und der Reiter übernimmt
/// ihn in den Arbeitsstand (Hausregel „Geschrieben wird im OK-Weg"). So lässt sich jede Handlung auch
/// zur Vorschau auf einer Kopie fahren, etwa „Vorlage übernehmen" für die Vorschau einer Vorlage an
/// den Ferienzeiträumen des Ziels (Teilkonzept 7.4).
/// </remarks>
/// <param name="Gebaeude">Der Feldsatz des Gebäudes bzw. Katalogbaus.</param>
/// <param name="Zonen">Die Zonen in Listenfolge; leer an einem Katalogsatz.</param>
public sealed record KonditionierungStand(GebaeudeKatalogDaten Gebaeude, IReadOnlyList<ZoneDaten> Zonen)
{
    /// <summary>Eine tiefe Kopie — Feldsatz und Zonen samt ihrer Konditionierung.</summary>
    public KonditionierungStand Kopie() => new(Gebaeude.Kopie(), Zonen.Select(z => z.Kopie()).ToList());
}

/// <summary>Wo eine Handlung wirkt: die Größe und das Gebäude bzw. der Katalogbau oder eine Zone.</summary>
/// <param name="Groesse">Die Größe (Spalte, Kalenderkarte).</param>
/// <param name="Zone">Die Id der Zone im Arbeitsstand (≤ 0 = vorläufig); <c>null</c> = Gebäude bzw. Katalogbau.</param>
public sealed record KonditionierungOrt(KonditionierungGroesse Groesse, int? Zone = null);

/// <summary>
/// Was eine Handlung ergeben hat — der neue Stand oder die benannte Ablehnung. Ein Fehlschlag
/// ändert nichts: <see cref="Stand"/> ist dann <c>null</c>.
/// </summary>
/// <param name="Ok">Hat die Handlung gegriffen?</param>
/// <param name="Meldung">Die benannte Ablehnung; leer im guten Fall.</param>
/// <param name="Stand">Der neue Stand; <c>null</c> im Fehlerfall.</param>
public sealed record KonditionierungErgebnis(bool Ok, string Meldung, KonditionierungStand? Stand)
{
    /// <summary>
    /// <b>Die Rückfrage, ohne die die Handlung nicht weitergeht</b> (Stufe KP2, Welle K2) — etwa
    /// „aufteilen" nach E56 F5 (a), wenn die Lüftung eine Vorgabe bekommt und das Ziel nur die
    /// Gesamtangabe <c>Luftwechselrate</c> trägt. Dann ist <see cref="Ok"/> <c>false</c>, der Stand
    /// <c>null</c> und nichts geändert; nach „Ja" ruft der Reiter den Delegaten der Antwort
    /// (<see cref="KonditionierungWeg.LuftwechselAufteilen"/>) und wiederholt die Handlung.
    /// </summary>
    public KonditionierungRueckfrage? Rueckfrage { get; init; }

    /// <summary>Der gute Fall.</summary>
    public static KonditionierungErgebnis Gut(KonditionierungStand stand) => new(true, "", stand);

    /// <summary>Der benannte Fehlschlag.</summary>
    public static KonditionierungErgebnis Fehler(string meldung) => new(false, meldung ?? "", null);

    /// <summary>Die Rückfrage vor der Handlung — nichts geändert.</summary>
    public static KonditionierungErgebnis Frage(KonditionierungRueckfrage rueckfrage)
        => new(false, "", null) { Rueckfrage = rueckfrage };
}

/// <summary>Eine Vorlage der Auswahlliste einer Größe (Teilkonzept 3.5, 7.4).</summary>
/// <param name="Id">Die Id der Vorlage.</param>
/// <param name="Groesse">Die eine Größe der Vorlage (P11).</param>
/// <param name="Name">Der Name, eindeutig je Größe; Daten, nicht übersetzt (Glossar § 10).</param>
/// <param name="Beschreibung">Die Beschreibung; leer = ohne.</param>
/// <param name="Nutzung">Die Nutzung.</param>
/// <param name="Ausgeliefert">Gehört die Vorlage zur Auslieferung (Schloss)?</param>
public sealed record KonditionierungVorlageDaten(long Id, KonditionierungGroesse Groesse, string Name,
                                                 string Beschreibung, KonditionierungNutzung Nutzung,
                                                 bool Ausgeliefert);

/// <summary>
/// Eine der neun bundeseinheitlichen Feiertagsregeln für die Periodenliste (F11): der Persistenzwert
/// (<c>DbWerte.KOND_FEIERTAGE</c>, nie übersetzt) und ihr Anzeigename.
/// </summary>
/// <param name="Regel">Der Persistenzwert der Regel.</param>
/// <param name="Name">Der Anzeigename („Neujahr").</param>
public sealed record KonditionierungFeiertag(string Regel, string Name);

/// <summary>Was „Als Vorlage speichern…" erfragt (Teilkonzept 7.4): Name, Beschreibung, Nutzung.</summary>
public sealed record KonditionierungVorlageEingabe(string Name, string Beschreibung, KonditionierungNutzung Nutzung);

/// <summary>
/// Ein erlaubtes Ziel von „Kopieren nach …" (Teilkonzept 3.5, 7.4) — der Dialog zeigt nur diese. Wo die
/// Richtung neue Sollwerte braucht (Heizen → Kühlen), tragen sie die Vorgaben des Komfort- und des
/// Absenksollwerts und die Grenzen beider Felder.
/// </summary>
/// <param name="Ziel">Die Zielgröße.</param>
/// <param name="Komfortsollwert">
/// Die Vorgabe des Komfortsollwerts [°C]; <c>null</c> = Werte und Zeitstruktur reisen unverändert (Geräte ↔
/// Personen), es gibt kein Sollwertfeld.
/// </param>
/// <param name="Min">Der kleinste Komfortsollwert [°C] (die Grenzen der Kühlspalte); ohne Sollwertfeld ohne Bedeutung.</param>
/// <param name="Max">Der größte Komfortsollwert [°C].</param>
public sealed record KonditionierungKopierziel(KonditionierungGroesse Ziel, double? Komfortsollwert, double Min, double Max)
{
    /// <summary>
    /// Die Vorgabe des Absenksollwerts [°C] für die Absenkzeiten (Heizsollwert unter dem Tagwert der Vorlage);
    /// <c>null</c> ohne Sollwertfeld. Grenzen wie beim Komfortsollwert (<see cref="Min"/>, <see cref="Max"/>);
    /// im Feld wählbar ist dazu „aus".
    /// </summary>
    public double? Absenksollwert { get; init; }
}

/// <summary>
/// Was „Kopieren nach …" erfragt (Teilkonzept 7.4): Zielgröße, Name der Kopie, bei Heizen → Kühlen den Komfort-
/// und den Absenksollwert.
/// </summary>
/// <param name="Ziel">Die Zielgröße — eines der erlaubten Ziele der Quelle.</param>
/// <param name="Name">Der Name der Kopie in der Liste der Zielgröße.</param>
/// <param name="Komfortsollwert">Der Komfortsollwert [°C]; nur bei Heizen → Kühlen, sonst <c>null</c>.</param>
public sealed record KonditionierungVorlageKopie(KonditionierungGroesse Ziel, string Name, double? Komfortsollwert)
{
    /// <summary>
    /// Der Absenksollwert [°C] der Absenkzeiten, <see cref="double.NaN"/> = „aus"; nur bei Heizen → Kühlen,
    /// sonst <c>null</c>.
    /// </summary>
    public double? Absenksollwert { get; init; }
}

/// <summary>
/// Was eine Handlung an einer Vorlage ergeben hat (speichern, umbenennen, löschen, duplizieren, kopieren
/// nach …) — sie schreiben sofort (Festlegung 13).
/// </summary>
/// <param name="Ok">Wurde geschrieben?</param>
/// <param name="Meldung">Die benannte Ablehnung (etwa ein Doppelname in der Liste); leer im guten Fall.</param>
/// <param name="Vorlage">Die neue bzw. geänderte Vorlage; <c>null</c> nach dem Löschen und im Fehlerfall.</param>
public sealed record KonditionierungVorlageErgebnis(bool Ok, string Meldung, KonditionierungVorlageDaten? Vorlage)
{
    /// <summary>
    /// Betrifft die Ablehnung den NAMEN (leer, zu lang, Doppelname in der Liste)? Dann nennt der Dialog
    /// sie am Namensfeld, nicht im Banner (Teilkonzept 7.4; Stufe KP2, Welle U2).
    /// </summary>
    public bool AmNamen { get; init; }

    /// <summary>
    /// Betrifft die Ablehnung den KOMFORTSOLLWERT von „Kopieren nach …" (fehlt, außerhalb der Grenzen der
    /// Kühlspalte)? Dann nennt der Dialog sie am Sollwertfeld.
    /// </summary>
    public bool AmSollwert { get; init; }

    /// <summary>
    /// Betrifft die Ablehnung den ABSENKSOLLWERT von „Kopieren nach …" (fehlt, außerhalb der Grenzen der
    /// Kühlspalte, unter dem Komfortsollwert)? Dann nennt der Dialog sie am Absenkfeld.
    /// </summary>
    public bool AmAbsenkwert { get; init; }
}

/// <summary>Die Angaben des Werkzeugs „Zeitfenster eintragen" (Teilkonzept 3.5, 7.5).</summary>
/// <param name="Tage">Die Tage 0 = Montag … 6 = Sonntag; leer = alle sieben.</param>
/// <param name="Von">Die erste Stunde 0 … 23 (einschließlich).</param>
/// <param name="Bis">Die Stunde 1 … 24, vor der das Fenster endet; unter <paramref name="Von"/> = über Mitternacht.</param>
/// <param name="Wert">Der Wert in der Einheit der Spalte; <c>null</c> = „aus".</param>
public sealed record KonditionierungZeitfenster(IReadOnlyList<int> Tage, int Von, int Bis, double? Wert);

/// <summary>Was ein Rückfragebefund zählt (Entwurf KP2 Festlegung 3; Teilkonzept 3.3, 3.5).</summary>
public enum KonditionierungPostenart
{
    /// <summary>Belegte Zellen der Matrix.</summary>
    Matrixzellen = 0,

    /// <summary>Angelegte Kalender.</summary>
    Kalender = 1,

    /// <summary>Die Standardwoche eines Kalenders.</summary>
    Standardwoche = 2,

    /// <summary>Perioden der Art Ferien (Matrixbereich).</summary>
    Ferienperioden = 3,

    /// <summary>Perioden der Art Betriebspause — die Saison (Matrixbereich).</summary>
    Saisonperioden = 4,

    /// <summary>Eigene Perioden der Art Zeitraum (Ausnahmetage).</summary>
    EigenePerioden = 5,

    /// <summary>Feiertagsregeln.</summary>
    Feiertage = 6,

    /// <summary>Die Nachtzeiten des Gebäudes.</summary>
    Nachtzeiten = 7,

    /// <summary>Die Ferienzeiträume des Gebäudes.</summary>
    Ferienzeitraeume = 8,

    /// <summary>Die Gesamtangabe des Luftwechsels, aufgeteilt in Infiltration und Nutzerlüftung (F5).</summary>
    Luftwechsel = 9,

    /// <summary>Kalender der Zonen, die ein Schritt mit anlegt (F2) oder zurücklässt.</summary>
    Zonenkalender = 10,

    /// <summary>Bauteile der Zonen — „Speichern unter" im Projekt nimmt sie nicht mit (Festlegung 3).</summary>
    Bauteile = 11
}

/// <summary>Ein Posten eines Rückfragebefunds: was und wie viel.</summary>
public sealed record KonditionierungPosten(KonditionierungPostenart Art, int Anzahl);

/// <summary>
/// <b>Der Befund einer Rückfrage VOR dem Schreiben</b> (Entwurf KP2 Festlegung 3) — was ersetzt
/// wird, was bleibt und welche Zonen es betrifft, mit Namen. Den Fragetext setzt der Dialog aus
/// seinen Texten zusammen; eine Frage je Handlung.
/// </summary>
/// <param name="Ersetzt">Was die Handlung ersetzt.</param>
/// <param name="Bleibt">Was stehen bleibt (eigene Perioden, Ausnahmetage, Zonen).</param>
/// <param name="Zonen">Die Namen der betroffenen Zonen; leer = keine.</param>
public sealed record KonditionierungRueckfrage(IReadOnlyList<KonditionierungPosten> Ersetzt,
                                               IReadOnlyList<KonditionierungPosten> Bleibt,
                                               IReadOnlyList<string> Zonen);

/// <summary>
/// Die Herleitung der Lasten (P1, Teilkonzept 3.1 und 7.2): Personen = Zahl × 70 W, Geräte =
/// Gesamtwert − Personenmittel, dazu die Jahresmittel in W und W/m² für die Zeile unter der Matrix.
/// </summary>
/// <param name="Personenzahl">Die Personenzahl (aus <c>Bewohner</c>, sonst Nutzfläche ÷ Fläche je Nutzer); <c>null</c> = keine.</param>
/// <param name="LeistungJePersonW">Die Wärme je Person [W] (EPOS-Vorgabe 70 W).</param>
/// <param name="PersonenNennwertW">Der Nennwert der Personen [W].</param>
/// <param name="PersonenJahresmittelW">Das Jahresmittel der Personenwärme [W].</param>
/// <param name="GeraeteNennwertW">Der Nennwert der Geräte [W] (nach P1).</param>
/// <param name="GeraeteJahresmittelW">Das Jahresmittel der Geräte [W].</param>
/// <param name="NutzflaecheM2">Die Nutzfläche [m²] für die Werte je m²; <c>null</c> = keine.</param>
public sealed record KonditionierungLasten(double? Personenzahl, double LeistungJePersonW, double PersonenNennwertW,
                                           double PersonenJahresmittelW, double GeraeteNennwertW,
                                           double GeraeteJahresmittelW, double? NutzflaecheM2);
