using System.Globalization;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Bearbeitung der Konditionierung im Arbeitsstand des Katalogeditors</b> (Stufe KP2, Welle U1;
/// Entwurf KP2 Abschnitt 2, Festlegungen 1, 3, 4, 5; Teilkonzept 3.3, 7.2) — was der Reiter
/// „Konditionierung", seine Matrix, seine Kalenderkarten und der Assistent tun, an EINER Stelle und
/// ohne Oberfläche.
/// </summary>
/// <remarks>
/// <para><b>Eine Wahrheit.</b> Die neun Bestandszellen, das Nachtfenster der Heizspalte, die
/// Ferienzeiträume und die Merker stehen in den Feldern von <see cref="GebaeudeKatalogDaten"/>, die
/// neuen Zellen und die Kalender in <see cref="GebaeudeKatalogDaten.Konditionierung"/>. Diese Klasse
/// liest beides, rechnet aber nichts selbst: Jede Handlung geht über ihren Delegaten im
/// <see cref="KonditionierungWeg"/> (Kern: <c>Konditionierungsarbeit</c>), und der neue Stand kommt
/// feldweise in den Arbeitsstand zurück. Geschrieben wird allein im OK-Weg des Editors.</para>
/// <para><b>Ohne Weg</b> (ohne Gaben, ohne Konditionierungstabellen) bleiben die Bestandszellen
/// bedienbar — sie stehen nach E56 F3 (a) nur noch in diesem Reiter — und schreiben ihr Feld
/// unmittelbar; neue Zellen, Kalender und Handlungen gibt es dann nicht („kein Delegat, kein
/// Knopf"), und <see cref="Sperrgrund"/> nennt den Grund.</para>
/// <para><b>Rückfragen</b> entstehen VOR der Handlung aus dem Befund des Wegs (Festlegung 3), mit
/// Vorgabe „Nein", wo ersetzt wird; die offene Frage steht in <see cref="OffeneFrage"/>, der Reiter
/// zeichnet sie und reicht die Antwort an <see cref="Beantworten"/>. Die Rückfrage „aufteilen"
/// (E56 F5 (a)) meldet der Weg selbst; nach „Ja" teilt der Weg auf und wiederholt die Handlung — ein
/// Schritt.</para>
/// <para><b>„Zurücknehmen"</b> geht eine Stufe zurück (Festlegung 1): Jede Handlung merkt sich den
/// Stand davor und danach; Eingaben in dieselbe Zelle hintereinander sind EIN Schritt. Zurückgesetzt
/// wird je Feld nur, was noch so steht, wie die Handlung es hinterließ — eine spätere Eingabe an einem
/// anderen Feld bleibt.</para>
/// </remarks>
public sealed class KonditionierungBearbeitung
{
    private readonly GebaeudeArbeitsstand _arbeit;
    private readonly Func<KonditionierungWeg?> _weg;
    private readonly Func<KonditionierungTexte> _texte;
    private readonly Func<KonditionierungFragetexte> _fragen;

    /// <summary>Die Bearbeitung über dem Arbeitsstand <paramref name="arbeit"/>.</summary>
    /// <param name="arbeit">Der Arbeitsstand des Editors (Feldsatz und Zonen).</param>
    /// <param name="weg">Der Weg der Konditionierung — als Delegat, denn der Parameter des Dialogs kann wechseln.</param>
    /// <param name="texte">Das Textbündel; <c>null</c> = die deutschen Rückfälle.</param>
    /// <param name="fragen">Das Bündel der Rückfragen; <c>null</c> = die deutschen Rückfälle.</param>
    public KonditionierungBearbeitung(GebaeudeArbeitsstand arbeit, Func<KonditionierungWeg?> weg,
                                      Func<KonditionierungTexte>? texte = null,
                                      Func<KonditionierungFragetexte>? fragen = null)
    {
        _arbeit = arbeit ?? throw new ArgumentNullException(nameof(arbeit));
        _weg = weg ?? (() => null);
        var t = new KonditionierungTexte();
        var f = new KonditionierungFragetexte();
        _texte = texte ?? (() => t);
        _fragen = fragen ?? (() => f);
    }

    /// <summary>Meldet eine benannte Ablehnung oder einen Hinweis an den Dialog (sein Banner).</summary>
    public Action<string, WarnStufe>? Melden { get; set; }

    /// <summary>Der Arbeitsstand.</summary>
    public GebaeudeArbeitsstand Arbeit => _arbeit;

    /// <summary>Der Feldsatz des Arbeitsstands.</summary>
    public GebaeudeKatalogDaten Stand => _arbeit.Stand;

    /// <summary>Der Weg; ohne Gaben das leere Bündel.</summary>
    public KonditionierungWeg Weg => _weg() ?? KonditionierungWeg.Keiner;

    /// <summary>Die Texte.</summary>
    public KonditionierungTexte Texte => _texte();

    /// <summary>Die Texte der Rückfragen.</summary>
    public KonditionierungFragetexte Fragen => _fragen();

    /// <summary>Die Konditionierung des Feldsatzes; <c>null</c> ohne Tabellen oder ohne Gaben.</summary>
    public KonditionierungDaten? Daten => Stand.Konditionierung;

    /// <summary>
    /// Geht die Bearbeitung über den Weg? Nur mit Konditionierung im Feldsatz, dem Delegaten
    /// „Zelle setzen" und ohne <see cref="KonditionierungWeg.Sperre"/>.
    /// </summary>
    public bool MitWeg => Daten is not null && Weg.Sperre is null && Weg.ZelleSetzen is not null;

    /// <summary>
    /// Warum Kalender, neue Zellen und Handlungen nicht zur Verfügung stehen (Festlegung 6: „ohne
    /// Konditionierungstabellen benannt gesperrt"); <c>null</c> = sie stehen.
    /// </summary>
    public string? Sperrgrund => MitWeg ? null : Weg.Sperre is { Length: > 0 } s ? s : Texte.GrundOhneTabellen;

    /// <summary>Bietet der Reiter den Knopf dieser Handlung an? („kein Delegat, kein Knopf")</summary>
    public bool Bietet(KonditionierungHandlung h) => MitWeg && Weg.Bietet(h);

    // =================================================================================
    // Die Zellen der Matrix
    // =================================================================================

    /// <summary>Die sechs Zeilen der Matrix in ihrer Reihenfolge.</summary>
    public static IReadOnlyList<KonditionierungZeile> Zeilen { get; } = new[]
    {
        KonditionierungZeile.Nennwert, KonditionierungZeile.Tag, KonditionierungZeile.Nacht,
        KonditionierungZeile.Wochenende, KonditionierungZeile.Ferien, KonditionierungZeile.Saison
    };

    /// <summary>
    /// Gibt es die Zelle? Den Nennwert führen Lüftung (Infiltration), Geräte und Personen, die Saison
    /// nur Heizen und Kühlen (Teilkonzept 3.3).
    /// </summary>
    public static bool Gibt(KonditionierungGroesse g, KonditionierungZeile z) => z switch
    {
        KonditionierungZeile.Nennwert => g is KonditionierungGroesse.Lueftung or KonditionierungGroesse.Geraete
                                             or KonditionierungGroesse.Personen,
        KonditionierungZeile.Saison => g is KonditionierungGroesse.Heizen or KonditionierungGroesse.Kuehlen,
        _ => true
    };

    /// <summary>Kennt die Zelle den Zustand „aus"? Nur Heizen und Kühlen (P2, Teilkonzept 7.2).</summary>
    public static bool MitAus(KonditionierungGroesse g, KonditionierungZeile z)
        => g is KonditionierungGroesse.Heizen or KonditionierungGroesse.Kuehlen
           && z is not (KonditionierungZeile.Nennwert or KonditionierungZeile.Saison);

    /// <summary>Trägt die Zelle einen Anteil in Prozent (Geräte und Personen außer dem Nennwert)?</summary>
    public static bool IstAnteil(KonditionierungGroesse g, KonditionierungZeile z)
        => g is KonditionierungGroesse.Geraete or KonditionierungGroesse.Personen && z != KonditionierungZeile.Nennwert;

    /// <summary>
    /// Liest der Tagesbilanz-Weg die Zelle? Nur die Heizspalte (Tag, Nacht, Wochenende, Ferien) und die
    /// inneren Wärmegewinne (Geräte, Nennwert) — Teilkonzept 2.2: für diese Gebäude zeigt die Matrix nur
    /// die Felder des Altwegs.
    /// </summary>
    public static bool LiestAltweg(KonditionierungGroesse g, KonditionierungZeile z)
        => (g == KonditionierungGroesse.Heizen && z is not (KonditionierungZeile.Nennwert or KonditionierungZeile.Saison))
           || (g == KonditionierungGroesse.Geraete && z == KonditionierungZeile.Nennwert);

    /// <summary>
    /// Der Name des Felds einer Zelle — Beschriftung für die Sprachausgabe und die Sammelmeldung
    /// („Heizen · Tag"); die Heizspalte trägt an Wochenende und Ferien die Namen des Bestands
    /// (Teilkonzept 7.2), die Lüftung die Namen ihrer Zeilen (Infiltration, Nutzerlüftung,
    /// Nachtauskühlung).
    /// </summary>
    public static string Feldname(KonditionierungTexte t, KonditionierungGroesse g, KonditionierungZeile z)
    {
        if (g == KonditionierungGroesse.Heizen && z == KonditionierungZeile.Wochenende) return t.FeldHeizenWochenende;
        if (g == KonditionierungGroesse.Heizen && z == KonditionierungZeile.Ferien) return t.FeldHeizenFerien;
        string zeile = (g, z) switch
        {
            (KonditionierungGroesse.Lueftung, KonditionierungZeile.Nennwert) => t.LabelInfiltration,
            (KonditionierungGroesse.Lueftung, KonditionierungZeile.Tag) => t.LabelNutzerlueftung,
            (KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht) => t.LabelNachtauskuehlung,
            (_, KonditionierungZeile.Nennwert) => t.ZeileNennwert,
            (_, KonditionierungZeile.Tag) => t.ZeileTag,
            (_, KonditionierungZeile.Nacht) => t.ZeileNacht,
            (_, KonditionierungZeile.Wochenende) => t.ZeileWochenende,
            (_, KonditionierungZeile.Ferien) => t.ZeileFerien,
            _ => t.ZeileSaison
        };
        return Groessenname(t, g) + " · " + zeile;
    }

    /// <summary>Der Name des Nachtfenster-Felds einer Spalte („Heizen · Nachtfenster").</summary>
    public static string Fensterfeldname(KonditionierungTexte t, KonditionierungGroesse g)
        => Groessenname(t, g) + " · " + t.LabelNachtfenster;

    /// <summary>Der Name des Felds ΔT der Nachtauskühlung („Lüftung · ΔT Außenluft").</summary>
    public static string DeltaTFeldname(KonditionierungTexte t) => t.GroesseLueftung + " · " + t.LabelAussenabstand;

    /// <summary>Die Namen von Start und Ende der Saison einer Spalte.</summary>
    public static (string Start, string Ende) Saisonfeldnamen(KonditionierungTexte t, KonditionierungGroesse g)
    {
        string feld = Feldname(t, g, KonditionierungZeile.Saison);
        return (feld + " · " + t.LabelSaisonStart, feld + " · " + t.LabelSaisonEnde);
    }

    /// <summary>Der Name einer Größe aus einem Bündel.</summary>
    public static string Groessenname(KonditionierungTexte t, KonditionierungGroesse g) => g switch
    {
        KonditionierungGroesse.Heizen => t.GroesseHeizen,
        KonditionierungGroesse.Kuehlen => t.GroesseKuehlen,
        KonditionierungGroesse.Lueftung => t.GroesseLueftung,
        KonditionierungGroesse.Geraete => t.GroesseGeraete,
        _ => t.GroessePersonen
    };

    /// <summary>
    /// Steht ein Feld dieses Namens in der Matrix? Die Prüfung des Arbeitsstands springt dann bei einer
    /// Fehleingabe auf den Reiter „Konditionierung" statt auf den ersten.
    /// </summary>
    public bool IstMatrixfeld(string feld)
    {
        KonditionierungTexte t = Texte;
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            if (feld == Fensterfeldname(t, g)) return true;
            foreach (KonditionierungZeile z in Zeilen)
                if (Gibt(g, z) && feld == Feldname(t, g, z)) return true;
            if (Gibt(g, KonditionierungZeile.Saison))
            {
                (string start, string ende) = Saisonfeldnamen(t, g);
                if (feld == start || feld == ende) return true;
            }
        }
        return feld == DeltaTFeldname(t);
    }

    /// <summary>Die Zelle der Konditionierung; <c>null</c> ohne Konditionierung oder ohne Zelle.</summary>
    public KonditionierungZelle? Zelle(KonditionierungGroesse g, KonditionierungZeile z)
        => Gibt(g, z) ? Daten?.Spalte(g).Zelle(z) : null;

    /// <summary>
    /// Der Wert der Zelle, wie das Zahlenfeld ihn zeigt: <see cref="double.NaN"/> = „aus", <c>null</c> =
    /// leer; eine Bestandszelle aus ihrem Feld, eine neue aus der Konditionierung.
    /// </summary>
    public double? Wert(KonditionierungGroesse g, KonditionierungZeile z)
    {
        if (!Gibt(g, z)) return null;
        KonditionierungZelle? c = Zelle(g, z);
        if (c?.Aus == true && MitAus(g, z)) return double.NaN;
        return KonditionierungDaten.IstBestandszelle(g, z) ? Bestandswert(g, z) : c?.Wert;
    }

    /// <summary>Die Zeiten der Zelle: das Nachtfenster (Stunden) bzw. die Saison (Jahrestage).</summary>
    public (int? Von, int? Bis) Zeiten(KonditionierungGroesse g, KonditionierungZeile z)
    {
        if (g == KonditionierungGroesse.Heizen && z == KonditionierungZeile.Nacht) return (Stand.NachtBeginn, Stand.NachtEnde);
        KonditionierungZelle? c = Zelle(g, z);
        return (c?.Von, c?.Bis);
    }

    /// <summary>ΔT der Nachtauskühlung [K]; <c>null</c> = die Vorgabe 2 K.</summary>
    public double? DeltaT => Zelle(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht)?.DeltaT;

    /// <summary>
    /// Braucht die Lüftung ΔT? Erst, wenn der Nachtwert über dem Tagwert liegt — dann ist er
    /// Nachtauskühlung (Teilkonzept 3.7) — oder wenn ΔT schon gesetzt ist.
    /// </summary>
    public bool Nachtauskuehlung
    {
        get
        {
            if (DeltaT.HasValue) return true;
            if (Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht) is not double nacht || !double.IsFinite(nacht))
                return false;
            double tag = Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Tag) is double t && double.IsFinite(t) ? t : 0.0;
            return nacht > tag;
        }
    }

    /// <summary>Der Wert einer Bestandszelle aus ihrem Feld (<see cref="KonditionierungDaten.Bestandsfeld"/>).</summary>
    private double? Bestandswert(KonditionierungGroesse g, KonditionierungZeile z) => (g, z) switch
    {
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Tag) => Stand.SollTag,
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht) => Stand.NachtAbsenkung,
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Wochenende) => Stand.WochenendAbsenkung,
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Ferien) => Stand.SollFerien,
        (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag) => Stand.KuehlSollwert,
        (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Nacht) => Stand.KuehlSollwertNacht,
        (KonditionierungGroesse.Lueftung, KonditionierungZeile.Nennwert) => Stand.LuftwechselInfiltration,
        (KonditionierungGroesse.Lueftung, KonditionierungZeile.Tag) => Stand.LuftwechselNutzer,
        (KonditionierungGroesse.Geraete, KonditionierungZeile.Nennwert) => Stand.Waermegewinne,
        _ => null
    };

    /// <summary>Schreibt den Wert einer Bestandszelle unmittelbar in ihr Feld.</summary>
    private void BestandswertSetzen(KonditionierungGroesse g, KonditionierungZeile z, double? w)
    {
        switch (g, z)
        {
            case (KonditionierungGroesse.Heizen, KonditionierungZeile.Tag): Stand.SollTag = w; break;
            case (KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht): Stand.NachtAbsenkung = w; break;
            case (KonditionierungGroesse.Heizen, KonditionierungZeile.Wochenende): Stand.WochenendAbsenkung = w; break;
            case (KonditionierungGroesse.Heizen, KonditionierungZeile.Ferien): Stand.SollFerien = w; break;
            case (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag): Stand.KuehlSollwert = w; break;
            case (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Nacht): Stand.KuehlSollwertNacht = w; break;
            case (KonditionierungGroesse.Lueftung, KonditionierungZeile.Nennwert): Stand.LuftwechselInfiltration = w; break;
            case (KonditionierungGroesse.Lueftung, KonditionierungZeile.Tag): Stand.LuftwechselNutzer = w; break;
            case (KonditionierungGroesse.Geraete, KonditionierungZeile.Nennwert): Stand.Waermegewinne = w; break;
        }
    }

    /// <summary>Darf die Zelle bearbeitet werden? Ohne Weg nur die Bestandszellen.</summary>
    public bool Bearbeitbar(KonditionierungGroesse g, KonditionierungZeile z)
        => Gibt(g, z) && (MitWeg || KonditionierungDaten.IstBestandszelle(g, z));

    /// <summary>
    /// <b>Setzt den Wert einer Zelle</b> — eine Zahl, <c>null</c> (leer) oder <see cref="double.NaN"/>
    /// („aus", nur Heizen und Kühlen). Mit Weg über „Zelle setzen" (Zellenort, Merker, Folgeregel F2,
    /// Rückfrage „aufteilen" F5); eine geleerte Bestandszelle leert danach ihr Feld — „—" heißt leer
    /// (Teilkonzept 3.3), der Kern lässt die Spalte an Gebäude und Katalogbau sonst stehen.
    /// </summary>
    public bool WertSetzen(KonditionierungGroesse g, KonditionierungZeile z, double? wert)
    {
        if (!Bearbeitbar(g, z)) return false;
        bool aus = wert is double a && double.IsNaN(a);
        if (aus && !MitAus(g, z)) return false;
        double? zahl = wert is double w && double.IsFinite(w) ? w : null;
        bool bestand = KonditionierungDaten.IstBestandszelle(g, z);

        if (!MitWeg)
        {
            if (bestand && !aus) BestandswertSetzen(g, z, zahl);
            return true;
        }

        KonditionierungZelle alt = Zelle(g, z) ?? new KonditionierungZelle();
        (int? von, int? bis) = Zeiten(g, z);
        var zelle = new KonditionierungZelle { Wert = zahl, Aus = aus, Von = von, Bis = bis, DeltaT = alt.DeltaT };
        bool leeren = bestand && zahl is null && !aus;
        return Ausfuehren("W|" + g + "|" + z, s => Weg.ZelleSetzen!(s, new KonditionierungOrt(g), z, zelle),
                          leeren ? () => BestandswertSetzen(g, z, null) : null);
    }

    /// <summary>
    /// Setzt die Zeiten einer Zelle: das Nachtfenster (Stunden 0 … 23) bzw. Start und Ende der Saison
    /// (Jahrestage 1 … 365). Das Nachtfenster der Heizspalte steht in <c>NachtBeginn</c>/<c>NachtEnde</c>
    /// (Festlegung 5); leer heißt dort die Vorgabe 22–6 Uhr.
    /// </summary>
    public bool ZeitenSetzen(KonditionierungGroesse g, KonditionierungZeile z, int? von, int? bis)
    {
        if (!Gibt(g, z) || z is not (KonditionierungZeile.Nacht or KonditionierungZeile.Saison)) return false;
        bool heiznacht = g == KonditionierungGroesse.Heizen && z == KonditionierungZeile.Nacht;
        if (!MitWeg)
        {
            if (!heiznacht) return false;
            Stand.NachtBeginn = von;
            Stand.NachtEnde = bis;
            return true;
        }

        KonditionierungZelle alt = Zelle(g, z) ?? new KonditionierungZelle();
        double? wert = Wert(g, z);
        var zelle = new KonditionierungZelle
        {
            Wert = wert is double w && double.IsFinite(w) ? w : null,
            Aus = alt.Aus,
            Von = von,
            Bis = bis,
            DeltaT = alt.DeltaT
        };
        // Leer am Nachtfenster der Heizspalte: Der Kern lässt eine leere Zeit stehen; hier heißt leer die
        // Vorgabe - die Spalten werden danach NULL.
        bool leeren = heiznacht && !von.HasValue && !bis.HasValue;
        return Ausfuehren("Z|" + g + "|" + z, s => Weg.ZelleSetzen!(s, new KonditionierungOrt(g), z, zelle),
                          leeren ? () => { Stand.NachtBeginn = null; Stand.NachtEnde = null; } : null);
    }

    /// <summary>Setzt ΔT der Nachtauskühlung [K]; <c>null</c> = die Vorgabe 2 K.</summary>
    public bool DeltaTSetzen(double? k)
    {
        if (!MitWeg) return false;
        const KonditionierungGroesse g = KonditionierungGroesse.Lueftung;
        const KonditionierungZeile z = KonditionierungZeile.Nacht;
        KonditionierungZelle alt = Zelle(g, z) ?? new KonditionierungZelle();
        var zelle = alt.Kopie();
        zelle.DeltaT = k is double w && double.IsFinite(w) ? w : null;
        return Ausfuehren("D|" + g + "|" + z, s => Weg.ZelleSetzen!(s, new KonditionierungOrt(g), z, zelle));
    }

    // =================================================================================
    // Die Kalenderkarten
    // =================================================================================

    /// <summary>Der Kalender einer Größe; <c>null</c> ohne Konditionierung.</summary>
    public KonditionierungKalender? Kalender(KonditionierungGroesse g) => Daten?.Spalte(g).Kalender;

    /// <summary>Ist der Kalender der Größe angelegt?</summary>
    public bool Angelegt(KonditionierungGroesse g) => Kalender(g)?.Zustand == KonditionierungZustand.Angelegt;

    /// <summary>
    /// Die Herkunft des angelegten Kalenders — der Name der zuletzt übernommenen Vorlage (Zeile „Vorlage"
    /// der Matrix, Teilkonzept 7.2); <c>null</c> = keine.
    /// </summary>
    public string? Herkunft(KonditionierungGroesse g)
        => Angelegt(g) && Kalender(g)?.Vorlage is { Length: > 0 } v ? v : null;

    /// <summary>
    /// Die Zustandszeile der eingeklappten Karte (Teilkonzept 7.1): „aus der Matrix", „aus Vorlage
    /// Büro", „angelegt, 3 eigene Perioden" oder — an einer Zone — „vom Gebäude".
    /// </summary>
    public string Zustand(KonditionierungGroesse g)
    {
        KonditionierungKalender? k = Kalender(g);
        KonditionierungTexte t = Texte;
        if (k is null || k.Zustand == KonditionierungZustand.Abgeleitet) return t.ZustandMatrix;
        if (k.Zustand == KonditionierungZustand.VomGebaeude) return t.ZustandGebaeude;
        if (!string.IsNullOrEmpty(k.Vorlage)) return string.Format(CultureInfo.CurrentCulture, t.ZustandVorlage, k.Vorlage);
        return k.EigenePerioden == 1
            ? t.ZustandAngelegtEine
            : string.Format(CultureInfo.CurrentCulture, t.ZustandAngelegt, k.EigenePerioden);
    }

    /// <summary>„Kalender anlegen" — der Generator schreibt den Kalender aus der Matrix in den Arbeitsstand.</summary>
    public bool Anlegen(KonditionierungGroesse g)
        => Bietet(KonditionierungHandlung.Anlegen)
           && Ausfuehren("A|" + g, s => Weg.Anlegen!(s, new KonditionierungOrt(g)));

    /// <summary>„Verwerfen" — nach der Rückfrage (Vorgabe Nein) fällt der angelegte Kalender samt Perioden.</summary>
    public void Verwerfen(KonditionierungGroesse g)
    {
        if (!Bietet(KonditionierungHandlung.Verwerfen)) return;
        MitRueckfrage(KonditionierungHandlung.Verwerfen, g, Texte.KnopfVerwerfen,
                      () => Ausfuehren("V|" + g, s => Weg.Verwerfen!(s, new KonditionierungOrt(g))));
    }

    /// <summary>„Matrix erneut anwenden…" — nach der Rückfrage ersetzt es nur den Matrixbereich (P12).</summary>
    public void MatrixErneut(KonditionierungGroesse g)
    {
        if (!Bietet(KonditionierungHandlung.MatrixErneut)) return;
        MitRueckfrage(KonditionierungHandlung.MatrixErneut, g, Texte.KnopfMatrixErneut,
                      () => Ausfuehren("M|" + g, s => Weg.MatrixErneut!(s, new KonditionierungOrt(g))));
    }

    /// <summary>
    /// „Aus dem Katalog erneut übernehmen…" (nur Projekt, Festlegung 4) — immer mit Rückfrage und Vorgabe
    /// Nein; ersetzt die ganze Gebäudeebene, die Zonen bleiben.
    /// </summary>
    public void KatalogErneut()
    {
        if (!Bietet(KonditionierungHandlung.KatalogErneut)) return;
        MitRueckfrage(KonditionierungHandlung.KatalogErneut, KonditionierungGroesse.Heizen, Texte.KnopfKatalogErneut,
                      () => Ausfuehren("K", s => Weg.KatalogErneut!(s)), immer: true);
    }

    // =================================================================================
    // Die Karte im Einzelnen (Stufe KP2, Welle U3; Teilkonzept 3.2, 7.5)
    // =================================================================================

    /// <summary>
    /// Warum die Handlungen der aufgeklappten Karte weich gesperrt stehen — ohne angelegten Kalender gibt
    /// es nichts zu ändern (der Kern lehnt es benannt ab, statt still einen anzulegen); <c>null</c> = frei.
    /// </summary>
    public string? Kartensperre(KonditionierungGroesse g) => Angelegt(g) ? null : Texte.GrundNichtAngelegt;

    /// <summary>
    /// Die 168 Werte, die ohne Standardwoche gelten — die Grundangabe als Raster (Wert oder „aus");
    /// <c>null</c>, wenn der Kalender eine Woche führt oder nicht angelegt ist. Das Wochenraster zeigt sie
    /// gesperrt, und „Standardwoche anlegen" macht daraus die Woche.
    /// </summary>
    public double[]? Grundangabewoche(KonditionierungGroesse g)
    {
        KonditionierungKalender? k = Kalender(g);
        if (k is null || k.Zustand != KonditionierungZustand.Angelegt || k.Angabe == KonditionierungAngabe.Woche) return null;
        return Wochenwerte(k);
    }

    /// <summary>
    /// <b>Die Grundangabe</b> (Ebene 1) — ein Wert oder <c>null</c> = „aus"; eine Standardwoche fällt
    /// dabei. Ein Schritt für „Zurücknehmen".
    /// </summary>
    public bool GrundangabeSetzen(KonditionierungGroesse g, double? wert)
        => Bietet(KonditionierungHandlung.Grundangabe)
           && Ausfuehren("G|" + g, s => Weg.Grundangabe!(s, new KonditionierungOrt(g), wert));

    /// <summary>
    /// <b>Die Standardwoche</b> (Ebene 2) aus dem Wochenraster — 168 Werte, NaN = „aus"; <c>null</c>
    /// verwirft sie zugunsten der Grundangabe. Jede Eingabe ist ein Schritt für „Zurücknehmen".
    /// </summary>
    public bool StandardwocheSetzen(KonditionierungGroesse g, double[]? woche)
        => Bietet(KonditionierungHandlung.Standardwoche)
           && Ausfuehren("S|" + g, s => Weg.Standardwoche!(s, new KonditionierungOrt(g), woche));

    /// <summary>
    /// <b>Das Zeitfenster „Tage, von, bis, Wert"</b> (Teilkonzept 3.5): setzt den Wert (oder „aus") in die
    /// Stunden der Standardwoche und lässt alles andere stehen; der Vermerk kommt in die Herkunft (B8).
    /// </summary>
    public bool ZeitfensterAnwenden(KonditionierungGroesse g, KonditionierungZeitfenster fenster)
        => Bietet(KonditionierungHandlung.Zeitfenster)
           && Ausfuehren("F|" + g, s => Weg.Zeitfenster!(s, new KonditionierungOrt(g), fenster));

    // =================================================================================
    // Vorlagen je Größe (Stufe KP2, Welle U2; Teilkonzept 3.5, 7.4)
    // =================================================================================

    private static readonly IReadOnlyList<KonditionierungVorlageDaten> KEINE_VORLAGEN = Array.Empty<KonditionierungVorlageDaten>();

    /// <summary>Die Listen je Größe, einmal gelesen — ein Zeichenlauf fragt die Datenbank nicht.</summary>
    private readonly Dictionary<KonditionierungGroesse, IReadOnlyList<KonditionierungVorlageDaten>> _vorlagen = new();

    /// <summary>Die Wahl der Auswahlliste je Größe (noch nicht übernommen).</summary>
    private readonly Dictionary<KonditionierungGroesse, long> _gewaehlt = new();

    /// <summary>
    /// Führt die Karte die Auswahlliste? Nur mit „Übernehmen" samt Liste im Weg („kein Delegat, kein
    /// Knopf", <see cref="KonditionierungWeg.Bietet"/>).
    /// </summary>
    public bool MitVorlagen => Bietet(KonditionierungHandlung.VorlageUebernehmen);

    /// <summary>
    /// <b>Die Vorlagen einer Größe</b> in der Reihenfolge des Kerns — die ausgelieferten zuerst, dann nach
    /// Name; gleiche Namen stehen so in jeder Liste an derselben Stelle. Gelesen wird einmal je Größe,
    /// bis <see cref="VorlagenNeuLaden"/> die Listen verwirft.
    /// </summary>
    public IReadOnlyList<KonditionierungVorlageDaten> Vorlagen(KonditionierungGroesse g)
    {
        if (!MitWeg || Weg.Vorlagen is null) return KEINE_VORLAGEN;
        if (_vorlagen.TryGetValue(g, out IReadOnlyList<KonditionierungVorlageDaten>? liste)) return liste;
        try
        {
            liste = Weg.Vorlagen(g) ?? KEINE_VORLAGEN;
        }
        catch (Exception ex)
        {
            Fehler(ex.Message);
            liste = KEINE_VORLAGEN;
        }
        _vorlagen[g] = liste;
        return liste;
    }

    /// <summary>
    /// Verwirft die gelesenen Listen — nach „Als Vorlage speichern…" und nach jeder Handlung der
    /// Verwaltung; eine Wahl, deren Vorlage es nicht mehr gibt, fällt.
    /// </summary>
    public void VorlagenNeuLaden()
    {
        _vorlagen.Clear();
        foreach (KonditionierungGroesse g in _gewaehlt.Keys.ToList())
            if (!Vorlagen(g).Any(v => v.Id == _gewaehlt[g])) _gewaehlt.Remove(g);
    }

    /// <summary>Die gewählte, noch nicht übernommene Vorlage einer Größe; <c>null</c> = keine.</summary>
    public KonditionierungVorlageDaten? GewaehlteVorlage(KonditionierungGroesse g)
        => _gewaehlt.TryGetValue(g, out long id) ? Vorlagen(g).FirstOrDefault(v => v.Id == id) : null;

    /// <summary>
    /// Wählt eine Vorlage der Liste (<c>null</c> = keine). Nur eine Vorlage DIESER Liste — eine andere
    /// Id wird abgelehnt (<c>false</c>). Die Wahl ändert den Arbeitsstand nicht; erst
    /// <see cref="VorlageUebernehmen"/> tut es.
    /// </summary>
    public bool VorlageWaehlen(KonditionierungGroesse g, long? id)
    {
        if (id is not long v)
        {
            _gewaehlt.Remove(g);
            return true;
        }
        if (!Vorlagen(g).Any(x => x.Id == v)) return false;
        _gewaehlt[g] = v;
        return true;
    }

    /// <summary>
    /// <b>„Übernehmen"</b> (P11, P12): die gewählte Vorlage in die Matrixspalte und den Kalender der
    /// Größe — über den Weg des Kerns in den Arbeitsstand; geschrieben wird mit dem OK des Editors.
    /// Trägt das Ziel schon einen angelegten Kalender dieser Größe, fragt der Reiter VORHER aus dem
    /// Befund (was ersetzt wird, was bleibt, Vorgabe „Nein"); steht die Lüftung als Gesamtangabe, kommt
    /// die Rückfrage „aufteilen" (F5). Danach ist die Wahl leer: Die Karte zeigt den Kalender, der der
    /// Matrix folgt (E56 F2 (a)). <c>false</c> = keine Wahl oder kein Weg.
    /// </summary>
    public bool VorlageUebernehmen(KonditionierungGroesse g)
    {
        if (!MitVorlagen || GewaehlteVorlage(g) is not KonditionierungVorlageDaten v) return false;
        long id = v.Id;
        Action handlung = () => Ausfuehren("T|" + g, s => Weg.VorlageUebernehmen!(s, new KonditionierungOrt(g), id),
                                           () => _gewaehlt.Remove(g));
        if (Angelegt(g))
            MitRueckfrage(KonditionierungHandlung.VorlageUebernehmen, g, Texte.KnopfUebernehmen, handlung, vorlage: v.Name);
        else
            handlung();
        return true;
    }

    /// <summary>
    /// <b>„Als Vorlage speichern…"</b> (E54, Festlegung 13): legt aus der Spalte und, falls angelegt, dem
    /// Kalender der Größe eine eigene Vorlage an — ohne Nennwert und Saison. Schreibt SOFORT mit eigenem
    /// OK, nicht mit dem des Editors, und ändert den Arbeitsstand nicht; danach sind die Listen neu
    /// gelesen. Eine Ablehnung des Namens kommt mit <see cref="KonditionierungVorlageErgebnis.AmNamen"/>.
    /// </summary>
    public KonditionierungVorlageErgebnis AlsVorlageSpeichern(KonditionierungGroesse g, KonditionierungVorlageEingabe eingabe)
    {
        if (!Bietet(KonditionierungHandlung.AlsVorlageSpeichern))
            return new KonditionierungVorlageErgebnis(false, Sperrgrund ?? Texte.GrundOhneTabellen, null);
        KonditionierungVorlageErgebnis e;
        try
        {
            e = Weg.AlsVorlageSpeichern!(Eingabestand(), new KonditionierungOrt(g), eingabe);
        }
        catch (Exception ex)
        {
            e = new KonditionierungVorlageErgebnis(false, ex.Message, null);
        }
        if (e.Ok) VorlagenNeuLaden();
        return e;
    }

    /// <summary>
    /// <b>Die Woche der Vorschau</b> einer Karte (Teilkonzept 7.4, 7.5) — 168 Werte in der Einheit der
    /// Spalte, <see cref="double.NaN"/> = „aus". Mit gewählter Vorlage die Woche, die „Übernehmen" auf
    /// einer KOPIE des Arbeitsstands ergäbe (an den Ferienzeiträumen des Ziels; an einer Gesamtangabe
    /// der Lüftung auf der aufgeteilten Probe); sonst die des angelegten Kalenders, beim abgeleiteten
    /// die, die „Kalender anlegen" auf einer Kopie ergäbe. Geändert wird nichts; <c>null</c> = keine.
    /// </summary>
    public double[]? Vorschauwoche(KonditionierungGroesse g) => Vorschauwoche(g, GewaehlteVorlage(g)?.Id);

    /// <summary>
    /// Die Woche der Vorschau mit der Vorlage <paramref name="vorlage"/> (<c>null</c> = der Kalender, wie
    /// er gilt) — die Vorschau der Vorlagenverwaltung, die die Wahl der Karte nicht anfasst.
    /// </summary>
    public double[]? Vorschauwoche(KonditionierungGroesse g, long? vorlage)
    {
        if (!MitWeg) return null;
        try
        {
            var ort = new KonditionierungOrt(g);
            KonditionierungStand s = Eingabestand();
            Func<KonditionierungStand, KonditionierungErgebnis>? probe = null;
            if (vorlage is long id && Weg.VorlageUebernehmen is not null)
                probe = x => Weg.VorlageUebernehmen(x, ort, id);
            else if (Angelegt(g))
                return Wochenwerte(Kalender(g));
            else if (Weg.Anlegen is not null)
                probe = x => Weg.Anlegen(x, ort);
            if (probe is null) return null;

            KonditionierungErgebnis e = probe(s);
            if (!e.Ok && e.Rueckfrage is not null && Weg.LuftwechselAufteilen is not null
                && Weg.LuftwechselAufteilen(s) is { Ok: true, Stand: KonditionierungStand geteilt })
                e = probe(geteilt);
            return e is { Ok: true, Stand: KonditionierungStand neu }
                ? Wochenwerte(neu.Gebaeude.Konditionierung?.Spalte(g).Kalender)
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Das Bild der Vorschau (<see cref="KonditionierungWeg.WochenVorschau"/>); <c>null</c> = keines.</summary>
    public WindowsFormsApplication1.Zeichnung.Zeichenmodell? Vorschau(KonditionierungGroesse g)
        => Bild(g, Vorschauwoche(g));

    /// <summary>Das Bild der Vorschau mit der Vorlage <paramref name="vorlage"/> — die Vorlagenverwaltung.</summary>
    public WindowsFormsApplication1.Zeichnung.Zeichenmodell? Vorschau(KonditionierungGroesse g, long vorlage)
        => Bild(g, Vorschauwoche(g, vorlage));

    private WindowsFormsApplication1.Zeichnung.Zeichenmodell? Bild(KonditionierungGroesse g, double[]? woche)
    {
        if (Weg.WochenVorschau is null || woche is null) return null;
        try { return Weg.WochenVorschau(g, woche); }
        catch (Exception) { return null; }
    }

    // ---- Die Vorlagenverwaltung (F4 (a), Festlegung 13): jede Handlung schreibt SOFORT ----

    /// <summary>
    /// Bietet der Weg die Vorlagenverwaltung? Mit der Liste und mindestens einer ihrer Handlungen
    /// (Umbenennen, Löschen, Duplizieren) — „kein Delegat, kein Knopf".
    /// </summary>
    public bool MitVerwaltung
        => MitWeg && Weg.Vorlagen is not null
           && (Weg.Bietet(KonditionierungHandlung.VorlageUmbenennen) || Weg.Bietet(KonditionierungHandlung.VorlageLoeschen)
               || Weg.Bietet(KonditionierungHandlung.VorlageDuplizieren));

    /// <summary>Eine eigene Vorlage umbenennen — schreibt sofort; eine Ablehnung des Namens kommt mit <c>AmNamen</c>.</summary>
    public KonditionierungVorlageErgebnis VorlageUmbenennen(long id, string name)
        => Vorlagenhandlung(KonditionierungHandlung.VorlageUmbenennen, () => Weg.VorlageUmbenennen!(id, name));

    /// <summary>Eine eigene Vorlage löschen — schreibt sofort; kein Gebäude wird berührt.</summary>
    public KonditionierungVorlageErgebnis VorlageLoeschen(long id)
        => Vorlagenhandlung(KonditionierungHandlung.VorlageLoeschen, () => Weg.VorlageLoeschen!(id));

    /// <summary>Eine Vorlage duplizieren — auch eine ausgelieferte; die Kopie heißt „Name (Kopie)", eindeutig.</summary>
    public KonditionierungVorlageErgebnis VorlageDuplizieren(long id)
        => Vorlagenhandlung(KonditionierungHandlung.VorlageDuplizieren, () => Weg.VorlageDuplizieren!(id, ""));

    private KonditionierungVorlageErgebnis Vorlagenhandlung(KonditionierungHandlung h, Func<KonditionierungVorlageErgebnis> handlung)
    {
        if (!Bietet(h)) return new KonditionierungVorlageErgebnis(false, Sperrgrund ?? Texte.GrundOhneTabellen, null);
        KonditionierungVorlageErgebnis e;
        try
        {
            e = handlung();
        }
        catch (Exception ex)
        {
            e = new KonditionierungVorlageErgebnis(false, ex.Message, null);
        }
        if (e.Ok) VorlagenNeuLaden();
        return e;
    }

    /// <summary>
    /// Der Schlüssel der Vorschau: Er ändert sich, wenn sich etwas ändert, das die Woche ändern kann — die
    /// Fassung der Konditionierung (jede Handlung, auch eine Bestandszelle über den Weg), die Wahl, die
    /// Kühlung und die Kopplung der Heizspalte. Die Karte rechnet nur bei neuem Schlüssel, entprellt.
    /// </summary>
    public string Vorschauschluessel(KonditionierungGroesse g)
        => string.Join("|", (Daten?.Fassung ?? -1).ToString(CultureInfo.InvariantCulture),
                       GewaehlteVorlage(g)?.Id.ToString(CultureInfo.InvariantCulture) ?? "-",
                       Stand.KuehlungAktiv ? "k" : "-", Stand.HeizkreisAktiv ? "h" : "-",
                       Stand.UebergabeArt ?? "", Stand.Sollwertprofil ?? "");

    /// <summary>Die 168 Werte eines Kalenders: Woche, Wert oder „aus"; <c>null</c> ohne Kalender.</summary>
    private static double[]? Wochenwerte(KonditionierungKalender? k)
    {
        if (k is null || k.Zustand != KonditionierungZustand.Angelegt) return null;
        switch (k.Angabe)
        {
            case KonditionierungAngabe.Woche:
                return k.Woche is { Length: WOCHENWERTE } w ? (double[])w.Clone() : null;
            case KonditionierungAngabe.Wert:
                return Enumerable.Repeat(k.Wert ?? double.NaN, WOCHENWERTE).ToArray();
            case KonditionierungAngabe.Aus:
                return Enumerable.Repeat(double.NaN, WOCHENWERTE).ToArray();
            default:
                return null;
        }
    }

    /// <summary>Die Zahl der Stunden einer Woche.</summary>
    private const int WOCHENWERTE = 168;

    // =================================================================================
    // Rückfragen
    // =================================================================================

    /// <summary>Eine offene Rückfrage: Titel, Text, Vorgabe „Nein" und was „Ja" tut.</summary>
    public sealed record Rueckfrage(string Titel, string Text, bool VorgabeNein, Action Ja);

    /// <summary>Die offene Rückfrage; <c>null</c> = keine.</summary>
    public Rueckfrage? OffeneFrage { get; private set; }

    /// <summary>Die Antwort: „Ja" führt die Handlung aus, „Nein" und Abbrechen lassen alles.</summary>
    public void Beantworten(bool? antwort)
    {
        Rueckfrage? f = OffeneFrage;
        OffeneFrage = null;
        if (antwort == true) f?.Ja();
    }

    /// <summary>
    /// Fragt VOR der Handlung aus dem Befund des Wegs (Festlegung 3); ohne Befund (nichts, was verloren
    /// ginge) geht es gleich weiter — außer <paramref name="immer"/>. Ohne Delegat fragt der Reiter ohne
    /// Einzelheiten.
    /// </summary>
    private void MitRueckfrage(KonditionierungHandlung h, KonditionierungGroesse g, string titel, Action handlung,
                               bool immer = false, string? vorlage = null)
    {
        string text;
        if (Weg.Rueckfrage is null)
            text = string.Format(CultureInfo.CurrentCulture, Fragen.OhneEinzelheiten, Knopftext(titel));
        else
        {
            KonditionierungRueckfrage? befund;
            try
            {
                befund = Weg.Rueckfrage(Eingabestand(), new KonditionierungOrt(g), h);
            }
            catch (Exception ex)
            {
                Fehler(ex.Message);
                return;
            }
            if (befund is null && !immer)
            {
                handlung();
                return;
            }
            text = Fragetext(h, g, befund, vorlage);
        }
        OffeneFrage = new Rueckfrage(Knopftext(titel), text, VorgabeNein: true, handlung);
    }

    /// <summary>
    /// Der Text einer Rückfrage aus ihrem Befund: was ersetzt wird, was bleibt, die Zonen mit Namen;
    /// <paramref name="vorlage"/> nennt beim Übernehmen die Vorlage.
    /// </summary>
    public string Fragetext(KonditionierungHandlung h, KonditionierungGroesse g, KonditionierungRueckfrage? befund,
                            string? vorlage = null)
    {
        CultureInfo c = CultureInfo.CurrentCulture;
        string ersetzt = Posten(befund?.Ersetzt);
        string bleibt = Posten(befund?.Bleibt);
        string text = h switch
        {
            KonditionierungHandlung.Verwerfen => string.Format(c, Fragen.Verwerfen, Groessenname(g), ersetzt, bleibt),
            KonditionierungHandlung.MatrixErneut => string.Format(c, Fragen.MatrixErneut, Groessenname(g), ersetzt, bleibt),
            KonditionierungHandlung.KatalogErneut => string.Format(c, Fragen.KatalogErneut, ersetzt, bleibt),
            KonditionierungHandlung.VorlageUebernehmen
                => string.Format(c, Fragen.VorlageUebernehmen, vorlage ?? "", Groessenname(g), ersetzt, bleibt),
            _ => string.Format(c, Fragen.OhneEinzelheiten, Groessenname(g))
        };
        if (befund is { Zonen.Count: > 0 })
            text += " " + string.Format(c, Fragen.Zonen, string.Join(", ", befund.Zonen));
        return text;
    }

    /// <summary>Die Posten eines Befunds als Liste („1 Kalender, 3 eigene Perioden"); leer = „nichts".</summary>
    public string Posten(IReadOnlyList<KonditionierungPosten>? posten)
    {
        KonditionierungTexte t = Texte;
        CultureInfo c = CultureInfo.CurrentCulture;
        var teile = new List<string>();
        foreach (KonditionierungPosten p in posten ?? Array.Empty<KonditionierungPosten>())
        {
            if (p.Anzahl <= 0) continue;
            string muster = p.Art switch
            {
                KonditionierungPostenart.Matrixzellen => t.TextPostenMatrixzellen,
                KonditionierungPostenart.Kalender => t.TextPostenKalender,
                KonditionierungPostenart.Standardwoche => t.TextPostenStandardwoche,
                KonditionierungPostenart.Ferienperioden => t.TextPostenFerienperioden,
                KonditionierungPostenart.Saisonperioden => t.TextPostenSaison,
                KonditionierungPostenart.EigenePerioden => t.TextPostenEigenePerioden,
                KonditionierungPostenart.Feiertage => t.TextPostenFeiertage,
                KonditionierungPostenart.Nachtzeiten => t.TextPostenNachtzeiten,
                KonditionierungPostenart.Ferienzeitraeume => t.TextPostenFerienzeitraeume,
                KonditionierungPostenart.Luftwechsel => t.TextPostenLuftwechsel,
                KonditionierungPostenart.Zonenkalender => t.TextPostenZonenkalender,
                _ => t.TextPostenBauteile
            };
            teile.Add(string.Format(c, muster, p.Anzahl));
        }
        return teile.Count == 0 ? t.TextNichts : string.Join(", ", teile);
    }

    /// <summary>
    /// <b>Die Frage von „Speichern unter" im Projekt</b> (Festlegung 3): Zonen, Bauteile und
    /// Konditionierung zusammen, mit den Namen der Zonen; <c>null</c> = der Weg kennt die Frage nicht
    /// (dann fragt der Dialog wie bisher) oder es bleibt nichts zurück.
    /// </summary>
    public string? SpeichernUnterFrage()
    {
        if (!MitWeg || Weg.SpeichernUnterRueckfrage is null) return null;
        KonditionierungRueckfrage? befund;
        try
        {
            befund = Weg.SpeichernUnterRueckfrage(Eingabestand());
        }
        catch (Exception)
        {
            return null;
        }
        if (befund is null) return null;
        string text = string.Format(CultureInfo.CurrentCulture, Fragen.SpeichernUnter, Posten(befund.Bleibt));
        if (befund.Zonen.Count > 0)
            text += " " + string.Format(CultureInfo.CurrentCulture, Fragen.Zonen, string.Join(", ", befund.Zonen));
        return text;
    }

    /// <summary>Der Name einer Größe aus dem Bündel.</summary>
    public string Groessenname(KonditionierungGroesse g) => Groessenname(Texte, g);

    /// <summary>Ein Knopftext ohne Auslassungspunkte — als Titel einer Rückfrage.</summary>
    private static string Knopftext(string knopf) => (knopf ?? "").TrimEnd('…', '.', ' ');

    // =================================================================================
    // „Zurücknehmen" (Festlegung 1: eine Stufe)
    // =================================================================================

    /// <summary>Der letzte Schritt: sein Schlüssel und der Stand davor und danach.</summary>
    private sealed record Schritt(string Schluessel, KonditionierungStand Vorher, KonditionierungStand Nachher);

    private Schritt? _letzter;

    /// <summary>Gibt es einen Schritt, den „Zurücknehmen" zurücknimmt?</summary>
    public bool KannZuruecknehmen => _letzter is not null;

    /// <summary>
    /// <b>„Zurücknehmen"</b> — der letzte Schritt fällt: Jedes Feld, das noch so steht, wie der Schritt
    /// es hinterließ, bekommt seinen Wert von davor; die Konditionierung des Gebäudes und der Zonen
    /// ebenso (mit neuer Fassung, damit das OK sie schreibt). Eine Stufe — danach ist nichts mehr
    /// zurückzunehmen.
    /// </summary>
    public bool Zuruecknehmen()
    {
        Schritt? s = _letzter;
        if (s is null)
        {
            Melden?.Invoke(Texte.GrundNichtsZurueck, WarnStufe.Hinweis);
            return false;
        }
        _letzter = null;
        OffeneFrage = null;

        GebaeudeKatalogDaten jetzt = Stand, vor = s.Vorher.Gebaeude, nach = s.Nachher.Gebaeude;
        foreach (Feld f in FELDER)
            if (Gleich(f.Lesen(jetzt), f.Lesen(nach))) f.Setzen(jetzt, f.Lesen(vor));
        if (Gleich(_arbeit.Ferienbeginne(), nach.Ferienbeginn) && Gleich(_arbeit.Ferienenden(), nach.Ferienende)
            && !(Gleich(nach.Ferienbeginn, vor.Ferienbeginn) && Gleich(nach.Ferienende, vor.Ferienende)))
            _arbeit.FerienUebernehmen(vor.Ferienbeginn, vor.Ferienende);
        jetzt.Konditionierung = Zurueck(jetzt.Konditionierung, vor.Konditionierung, nach.Konditionierung);

        foreach (ZoneDaten z in _arbeit.Zonen)
        {
            ZoneDaten? zv = s.Vorher.Zonen.FirstOrDefault(x => x.Id == z.Id);
            ZoneDaten? zn = s.Nachher.Zonen.FirstOrDefault(x => x.Id == z.Id);
            if (zv is null || zn is null) continue;
            foreach (Zonenfeld f in ZONENFELDER)
                if (Gleich(f.Lesen(z), f.Lesen(zn))) f.Setzen(z, f.Lesen(zv));
            z.Konditionierung = Zurueck(z.Konditionierung, zv.Konditionierung, zn.Konditionierung);
        }
        return true;
    }

    /// <summary>Die Konditionierung von davor, wenn sie noch die von danach ist — mit neuer Fassung.</summary>
    private static KonditionierungDaten? Zurueck(KonditionierungDaten? jetzt, KonditionierungDaten? vor, KonditionierungDaten? nach)
    {
        if (jetzt is null || vor is null || nach is null || jetzt.Fassung != nach.Fassung) return jetzt;
        KonditionierungDaten k = vor.Kopie();
        k.Fassung = Math.Max(jetzt.Fassung, vor.Fassung) + 1;
        return k;
    }

    // =================================================================================
    // Ausführen: Stand hin, neuer Stand zurück
    // =================================================================================

    /// <summary>
    /// <b>Der Stand, den eine Handlung bekommt</b> — eine Kopie des Feldsatzes mit den Ferienzeiträumen
    /// aus Tag und Monat der Felder (sie stehen bis zum OK nur dort) und die Zonen. Die Hülle ändert
    /// ihn nicht; sie gibt einen neuen zurück.
    /// </summary>
    public KonditionierungStand Eingabestand()
    {
        GebaeudeKatalogDaten g = Stand.Kopie();
        g.Ferienbeginn = _arbeit.Ferienbeginne();
        g.Ferienende = _arbeit.Ferienenden();
        return new KonditionierungStand(g, _arbeit.Zonen.Select(z => z.Kopie()).ToList());
    }

    /// <summary>
    /// Führt eine Handlung über den Weg aus und übernimmt den neuen Stand. Fragt der Weg „aufteilen"
    /// (F5), steht danach die Rückfrage; nach „Ja" teilt der Weg auf, und die Handlung läuft auf dem
    /// aufgeteilten Stand — ein Schritt für „Zurücknehmen".
    /// </summary>
    private bool Ausfuehren(string schluessel, Func<KonditionierungStand, KonditionierungErgebnis> handlung, Action? danach = null)
    {
        KonditionierungStand vor = Eingabestand();
        KonditionierungErgebnis e;
        try
        {
            e = handlung(vor);
        }
        catch (Exception ex)
        {
            Fehler(ex.Message);
            return false;
        }

        if (!e.Ok && e.Rueckfrage is not null)
        {
            AufteilenFragen(schluessel, handlung, danach, vor);
            return false;
        }
        return Abschliessen(schluessel, vor, e, danach);
    }

    private bool Abschliessen(string schluessel, KonditionierungStand vor, KonditionierungErgebnis e, Action? danach)
    {
        if (!e.Ok || e.Stand is null)
        {
            Fehler(e.Meldung);
            return false;
        }
        Uebernehmen(vor, e.Stand);
        danach?.Invoke();
        Merken(schluessel, vor, Eingabestand());
        return true;
    }

    /// <summary>
    /// Die Rückfrage „aufteilen" (E56 F5 (a)): Die Frage nennt Rate und Aufteilung — gerechnet vom Weg
    /// selbst auf einer Probe; nach „Ja" teilt er auf, und die Handlung läuft auf dem neuen Stand.
    /// </summary>
    private void AufteilenFragen(string schluessel, Func<KonditionierungStand, KonditionierungErgebnis> handlung,
                                 Action? danach, KonditionierungStand vor)
    {
        if (Weg.LuftwechselAufteilen is null)
        {
            Fehler(Texte.TextPostenLuftwechsel);
            return;
        }
        KonditionierungErgebnis probe = Weg.LuftwechselAufteilen(vor);
        if (!probe.Ok || probe.Stand is null)
        {
            Fehler(probe.Meldung);
            return;
        }
        CultureInfo c = CultureInfo.CurrentCulture;
        string text = string.Format(c, Fragen.Aufteilen, Zahlen.Anzeigetext(vor.Gebaeude.Luftwechselrate),
                                    Zahlen.Anzeigetext(probe.Stand.Gebaeude.LuftwechselInfiltration),
                                    Zahlen.Anzeigetext(probe.Stand.Gebaeude.LuftwechselNutzer));
        OffeneFrage = new Rueckfrage(Texte.GroesseLueftung, text, VorgabeNein: false, () =>
        {
            KonditionierungStand jetzt = Eingabestand();
            KonditionierungErgebnis a = Weg.LuftwechselAufteilen(jetzt);
            if (!a.Ok || a.Stand is null)
            {
                Fehler(a.Meldung);
                return;
            }
            KonditionierungErgebnis e;
            try
            {
                e = handlung(a.Stand);
            }
            catch (Exception ex)
            {
                Fehler(ex.Message);
                return;
            }
            if (!e.Ok || e.Stand is null)
            {
                Fehler(e.Meldung);
                return;
            }
            Uebernehmen(jetzt, e.Stand);
            danach?.Invoke();
            _letzter = null;   // ein neuer Schritt, nicht die Fortsetzung einer Eingabe
            Merken(schluessel, jetzt, Eingabestand());
        });
    }

    private void Fehler(string meldung)
    {
        if (string.IsNullOrEmpty(meldung)) return;
        LetzteMeldung = meldung;
        Melden?.Invoke(meldung, WarnStufe.Warnung);
    }

    /// <summary>Die letzte benannte Ablehnung des Wegs — der Assistent nennt sie als Grund.</summary>
    public string? LetzteMeldung { get; private set; }

    /// <summary>Merkt den Schritt; Eingaben in dieselbe Zelle hintereinander sind EIN Schritt.</summary>
    private void Merken(string schluessel, KonditionierungStand vor, KonditionierungStand nach)
    {
        bool fortsetzung = _letzter is not null && schluessel.Length > 2
                           && (schluessel[0] is 'W' or 'Z' or 'D') && _letzter.Schluessel == schluessel;
        _letzter = new Schritt(schluessel, fortsetzung ? _letzter!.Vorher : vor, nach);
    }

    /// <summary>
    /// Übernimmt den neuen Stand feldweise in den Arbeitsstand: die Felder der Konditionierung, die
    /// Ferienzeiträume nur, wenn der Schritt sie geändert hat, die Konditionierung des Gebäudes und je
    /// Zone (über die Id) Felder und Konditionierung.
    /// </summary>
    private void Uebernehmen(KonditionierungStand vor, KonditionierungStand neu)
    {
        GebaeudeKatalogDaten ziel = Stand, n = neu.Gebaeude;
        foreach (Feld f in FELDER) f.Setzen(ziel, f.Lesen(n));
        if (!Gleich(n.Ferienbeginn, vor.Gebaeude.Ferienbeginn) || !Gleich(n.Ferienende, vor.Gebaeude.Ferienende))
            _arbeit.FerienUebernehmen(n.Ferienbeginn, n.Ferienende);
        ziel.Konditionierung = n.Konditionierung;

        foreach (ZoneDaten zn in neu.Zonen)
        {
            ZoneDaten? z = _arbeit.ZoneMitId(zn.Id);
            if (z is null) continue;
            foreach (Zonenfeld f in ZONENFELDER) f.Setzen(z, f.Lesen(zn));
            z.Konditionierung = zn.Konditionierung;
        }
    }

    // =================================================================================
    // Die Felder, die ein Schritt ändern kann
    // =================================================================================

    private sealed record Feld(Func<GebaeudeKatalogDaten, object?> Lesen, Action<GebaeudeKatalogDaten, object?> Setzen);

    private sealed record Zonenfeld(Func<ZoneDaten, object?> Lesen, Action<ZoneDaten, object?> Setzen);

    /// <summary>
    /// Die Felder des Feldsatzes, die die Konditionierung liest und ein Schritt ändern kann: die neun
    /// Bestandszellen, die Gesamtangabe des Luftwechsels, das Nachtfenster und die Merker.
    /// </summary>
    private static readonly Feld[] FELDER =
    {
        new(d => d.SollTag, (d, w) => d.SollTag = (double?)w),
        new(d => d.NachtAbsenkung, (d, w) => d.NachtAbsenkung = (double?)w),
        new(d => d.WochenendAbsenkung, (d, w) => d.WochenendAbsenkung = (double?)w),
        new(d => d.SollFerien, (d, w) => d.SollFerien = (double?)w),
        new(d => d.KuehlSollwert, (d, w) => d.KuehlSollwert = (double?)w),
        new(d => d.KuehlSollwertNacht, (d, w) => d.KuehlSollwertNacht = (double?)w),
        new(d => d.LuftwechselInfiltration, (d, w) => d.LuftwechselInfiltration = (double?)w),
        new(d => d.LuftwechselNutzer, (d, w) => d.LuftwechselNutzer = (double?)w),
        new(d => d.Luftwechselrate, (d, w) => d.Luftwechselrate = (double?)w),
        new(d => d.Waermegewinne, (d, w) => d.Waermegewinne = (double?)w),
        new(d => d.NachtBeginn, (d, w) => d.NachtBeginn = (int?)w),
        new(d => d.NachtEnde, (d, w) => d.NachtEnde = (int?)w),
        new(d => d.Wochenende, (d, w) => d.Wochenende = (double)w!),
        new(d => d.Ferien, (d, w) => d.Ferien = (double)w!),
    };

    /// <summary>Die Felder einer Zone, die ein Schritt ändern kann.</summary>
    private static readonly Zonenfeld[] ZONENFELDER =
    {
        new(z => z.SollTag, (z, w) => z.SollTag = (double?)w),
        new(z => z.SollNacht, (z, w) => z.SollNacht = (double?)w),
        new(z => z.SollWochenende, (z, w) => z.SollWochenende = (double?)w),
        new(z => z.SollFerien, (z, w) => z.SollFerien = (double?)w),
        new(z => z.LuftwechselInfiltration, (z, w) => z.LuftwechselInfiltration = (double?)w),
        new(z => z.LuftwechselNutzer, (z, w) => z.LuftwechselNutzer = (double?)w),
        new(z => z.InterneWaermegewinne, (z, w) => z.InterneWaermegewinne = (double?)w),
    };

    private static bool Gleich(object? a, object? b) => Equals(a, b);

    private static bool Gleich(int[]? a, int[]? b)
        => a is null ? b is null : b is not null && a.SequenceEqual(b);
}
