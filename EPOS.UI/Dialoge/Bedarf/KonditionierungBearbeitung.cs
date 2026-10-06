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
/// <para><b>Die Zonenmatrix</b> (Stufe KP2, Welle U4; Teilkonzept 3.4, 7.3): Mit einer Zone gebaut
/// (<see cref="KonditionierungBearbeitung(GebaeudeArbeitsstand, ZoneDaten, Func{KonditionierungWeg?}, Func{KonditionierungTexte}?, Func{KonditionierungFragetexte}?)"/>)
/// wirkt jede Handlung am Ort der Zone: Die Bestandszellen sind die Felder der Zone (leer = „wie das
/// Gebäude"), die neuen Zellen und Kalender ihre <see cref="ZoneDaten.Konditionierung"/>, alle Zeiten
/// stehen in der Zelle. Das Gebäude liest sie nur — ein Schritt, der das Gebäude ändern wollte (die
/// Aufteilung der Gesamtangabe), wird benannt abgelehnt; geschrieben wird mit dem OK des Wirts. Die
/// Platzhalter nennen, was eine leere Zelle erbt (<see cref="Erbplatzhalter"/>).</para>
/// <para><b>Die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…“</b> (E57; Stufe KP2, Welle U5):
/// <see cref="VorlageAlleWaehlen"/> stellt aus der Wahl eines Namens EINE Rückfrage für alle Größen, und „Ja“
/// übernimmt in jeder Größe mit einer Vorlage dieses Namens über denselben Weg wie „Übernehmen“ der Karte — als
/// EIN Schritt. Kein Satzbegriff (P11 bleibt): Die Vorlagen bleiben je Größe, der Weg je Karte bleibt, wie er
/// ist.</para>
/// </remarks>
public sealed class KonditionierungBearbeitung
{
    private readonly GebaeudeArbeitsstand _arbeit;
    private readonly ZoneDaten? _zone;
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

    /// <summary>
    /// <b>Die Bearbeitung der Zonenmatrix</b> (Stufe KP2, Welle U4): <paramref name="zone"/> ist der
    /// Arbeitsstand des Zonendialogs — ihn ändern die Handlungen; <paramref name="arbeit"/> der
    /// Arbeitsstand des Gebäudeeditors, den sie nur lesen (Gebäude und übrige Zonen).
    /// </summary>
    public KonditionierungBearbeitung(GebaeudeArbeitsstand arbeit, ZoneDaten zone, Func<KonditionierungWeg?> weg,
                                      Func<KonditionierungTexte>? texte = null,
                                      Func<KonditionierungFragetexte>? fragen = null)
        : this(arbeit, weg, texte, fragen)
    {
        _zone = zone ?? throw new ArgumentNullException(nameof(zone));
    }

    /// <summary>Meldet eine benannte Ablehnung oder einen Hinweis an den Dialog (sein Banner).</summary>
    public Action<string, WarnStufe>? Melden { get; set; }

    /// <summary>Der Arbeitsstand.</summary>
    public GebaeudeArbeitsstand Arbeit => _arbeit;

    /// <summary>Der Feldsatz des Arbeitsstands.</summary>
    public GebaeudeKatalogDaten Stand => _arbeit.Stand;

    /// <summary>Die Zone, deren Matrix bearbeitet wird; <c>null</c> = das Gebäude bzw. der Katalogbau.</summary>
    public ZoneDaten? Zone => _zone;

    /// <summary>Wird die Matrix einer Zone bearbeitet?</summary>
    public bool IstZone => _zone is not null;

    /// <summary>Der Ort einer Handlung: die Größe an der Zone bzw. am Gebäude.</summary>
    private KonditionierungOrt Ort(KonditionierungGroesse g) => new(g, _zone?.Id);

    /// <summary>Der Weg; ohne Gaben das leere Bündel.</summary>
    public KonditionierungWeg Weg => _weg() ?? KonditionierungWeg.Keiner;

    /// <summary>Die Texte.</summary>
    public KonditionierungTexte Texte => _texte();

    /// <summary>Die Texte der Rückfragen.</summary>
    public KonditionierungFragetexte Fragen => _fragen();

    /// <summary>
    /// Die Konditionierung des Feldsatzes bzw. der Zone; <c>null</c> ohne Tabellen oder ohne Gaben — an
    /// einer neuen Zone auch, solange sie noch keine Zelle trägt.
    /// </summary>
    public KonditionierungDaten? Daten => _zone is not null ? _zone.Konditionierung : Stand.Konditionierung;

    /// <summary>
    /// Geht die Bearbeitung über den Weg? Nur mit Konditionierung im Feldsatz (an einer Zone: am
    /// Gebäude), dem Delegaten „Zelle setzen" und ohne <see cref="KonditionierungWeg.Sperre"/>.
    /// </summary>
    public bool MitWeg => Stand.Konditionierung is not null && (_zone is not null || Daten is not null)
                          && Weg.Sperre is null && Weg.ZelleSetzen is not null;

    /// <summary>
    /// Warum Kalender, neue Zellen und Handlungen nicht zur Verfügung stehen (Festlegung 6: „ohne
    /// Konditionierungstabellen benannt gesperrt"); <c>null</c> = sie stehen.
    /// </summary>
    public string? Sperrgrund => MitWeg ? null : Weg.Sperre is { Length: > 0 } s ? s : Texte.GrundOhneTabellen;

    /// <summary>
    /// Bietet der Reiter den Knopf dieser Handlung an? („kein Delegat, kein Knopf") „Aus dem Katalog erneut
    /// übernehmen…" gibt es nur am Gebäude, „Vom Gebäude übernehmen und anpassen" nur an einer Zone.
    /// </summary>
    public bool Bietet(KonditionierungHandlung h)
        => MitWeg && Weg.Bietet(h)
           && (IstZone ? h != KonditionierungHandlung.KatalogErneut : h != KonditionierungHandlung.VomGebaeude);

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
        // Am Gebäude stehen die Heiz-Nachtzeiten in den Bestandsspalten (Festlegung 5), an einer Zone in der Zelle.
        if (!IstZone && g == KonditionierungGroesse.Heizen && z == KonditionierungZeile.Nacht)
            return (Stand.NachtBeginn, Stand.NachtEnde);
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

    /// <summary>
    /// Der Wert einer Bestandszelle aus ihrem Feld (<see cref="KonditionierungDaten.Bestandsfeld"/>); an
    /// einer Zone das Feld der Zone (leer = „wie das Gebäude"), die Kühlspalte folgt dem Gebäude (bis KU3).
    /// </summary>
    private double? Bestandswert(KonditionierungGroesse g, KonditionierungZeile z)
        => _zone is ZoneDaten zone ? Zonenwert(zone, g, z) : Gebaeudewert(g, z);

    private static double? Zonenwert(ZoneDaten zone, KonditionierungGroesse g, KonditionierungZeile z) => (g, z) switch
    {
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Tag) => zone.SollTag,
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht) => zone.SollNacht,
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Wochenende) => zone.SollWochenende,
        (KonditionierungGroesse.Heizen, KonditionierungZeile.Ferien) => zone.SollFerien,
        (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag) => zone.KuehlSollwert,
        (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Nacht) => zone.KuehlSollwertNacht,
        (KonditionierungGroesse.Lueftung, KonditionierungZeile.Nennwert) => zone.LuftwechselInfiltration,
        (KonditionierungGroesse.Lueftung, KonditionierungZeile.Tag) => zone.LuftwechselNutzer,
        (KonditionierungGroesse.Geraete, KonditionierungZeile.Nennwert) => zone.InterneWaermegewinne,
        _ => null
    };

    private double? Gebaeudewert(KonditionierungGroesse g, KonditionierungZeile z) => (g, z) switch
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

    /// <summary>Schreibt den Wert einer Bestandszelle unmittelbar in ihr Feld (an einer Zone in das der Zone).</summary>
    private void BestandswertSetzen(KonditionierungGroesse g, KonditionierungZeile z, double? w)
    {
        if (_zone is ZoneDaten zone)
        {
            switch (g, z)
            {
                case (KonditionierungGroesse.Heizen, KonditionierungZeile.Tag): zone.SollTag = w; break;
                case (KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht): zone.SollNacht = w; break;
                case (KonditionierungGroesse.Heizen, KonditionierungZeile.Wochenende): zone.SollWochenende = w; break;
                case (KonditionierungGroesse.Heizen, KonditionierungZeile.Ferien): zone.SollFerien = w; break;
                case (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag): zone.KuehlSollwert = w; break;
                case (KonditionierungGroesse.Kuehlen, KonditionierungZeile.Nacht): zone.KuehlSollwertNacht = w; break;
                case (KonditionierungGroesse.Lueftung, KonditionierungZeile.Nennwert): zone.LuftwechselInfiltration = w; break;
                case (KonditionierungGroesse.Lueftung, KonditionierungZeile.Tag): zone.LuftwechselNutzer = w; break;
                case (KonditionierungGroesse.Geraete, KonditionierungZeile.Nennwert): zone.InterneWaermegewinne = w; break;
            }
            return;
        }
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
        return Ausfuehren("W|" + g + "|" + z, s => Weg.ZelleSetzen!(s, Ort(g), z, zelle),
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
        // Die Heiz-Nachtzeiten stehen nur an Gebäude und Katalogbau in Bestandsspalten; an einer Zone in der Zelle.
        bool heiznacht = !IstZone && g == KonditionierungGroesse.Heizen && z == KonditionierungZeile.Nacht;
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
        return Ausfuehren("Z|" + g + "|" + z, s => Weg.ZelleSetzen!(s, Ort(g), z, zelle),
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
        return Ausfuehren("D|" + g + "|" + z, s => Weg.ZelleSetzen!(s, Ort(g), z, zelle));
    }

    // =================================================================================
    // Die Kalenderkarten
    // =================================================================================

    /// <summary>Der Kalender einer Größe; <c>null</c> ohne Konditionierung.</summary>
    public KonditionierungKalender? Kalender(KonditionierungGroesse g) => Daten?.Spalte(g).Kalender;

    /// <summary>Ist der Kalender der Größe angelegt?</summary>
    public bool Angelegt(KonditionierungGroesse g) => Kalender(g)?.Zustand == KonditionierungZustand.Angelegt;

    /// <summary>
    /// Die Herkunft des angelegten Kalenders — der Name der zuletzt übernommenen VORLAGE (Teilkonzept 7.2; Lesen von
    /// <c>kond_&lt;größe&gt;_vorlage</c>, Liste „alle Größen“); <c>null</c> = keine. Ein aus einem Nutzungsprofil übernommener
    /// Kalender (<see cref="KonditionierungKalender.HerkunftProfil"/>) hat keine Vorlage, auch wenn eine gleichnamige
    /// existiert — unterschieden über die Herkunftsart, nicht über den Namen (NP2b-5c).
    /// </summary>
    public string? Herkunft(KonditionierungGroesse g)
        => Angelegt(g) && Kalender(g) is { HerkunftProfil: false, Vorlage: { Length: > 0 } v } ? v : null;

    /// <summary>
    /// Die Herkunft als Anzeigetext der Zeile „Vorlage“ der Matrix: der Name der Vorlage, bei einem Nutzungsprofil
    /// „Nutzungsprofil …“ (<see cref="KonditionierungTexte.HerkunftProfil"/>); <c>null</c> = keine.
    /// </summary>
    public string? HerkunftText(KonditionierungGroesse g)
        => !Angelegt(g) || Kalender(g) is not { Vorlage: { Length: > 0 } v } k ? null
         : k.HerkunftProfil ? string.Format(CultureInfo.CurrentCulture, Texte.HerkunftProfil, v) : v;

    /// <summary>
    /// Die Zustandszeile der eingeklappten Karte (Teilkonzept 7.1): „aus der Matrix", „aus Vorlage
    /// Büro", „angelegt, 3 eigene Perioden" oder — an einer Zone — „vom Gebäude".
    /// </summary>
    public string Zustand(KonditionierungGroesse g)
    {
        KonditionierungKalender? k = Kalender(g);
        KonditionierungTexte t = Texte;
        if (VomGebaeude(g)) return t.ZustandGebaeude;
        if (k is null || k.Zustand == KonditionierungZustand.Abgeleitet) return t.ZustandMatrix;
        if (k.Zustand == KonditionierungZustand.VomGebaeude) return IstZone ? t.ZustandMatrix : t.ZustandGebaeude;
        if (!string.IsNullOrEmpty(k.Vorlage))
            return string.Format(CultureInfo.CurrentCulture, k.HerkunftProfil ? t.ZustandProfil : t.ZustandVorlage, k.Vorlage);
        return k.EigenePerioden == 1
            ? t.ZustandAngelegtEine
            : string.Format(CultureInfo.CurrentCulture, t.ZustandAngelegt, k.EigenePerioden);
    }

    // =================================================================================
    // Die Zone: vom Gebäude, übernehmen und anpassen, geerbte Zellen (Stufe KP2, Welle U4)
    // =================================================================================

    /// <summary>
    /// <b>Folgt die Zone dem Kalender des Gebäudes?</b> (Teilkonzept 3.4) — das Gebäude hat den Kalender
    /// der Größe angelegt, die Zone keinen eigenen. Dann sind die Zellen der Zone in dieser Größe ohne
    /// Wirkung, bis „Vom Gebäude übernehmen und anpassen" eine eigene Kopie anlegt.
    /// </summary>
    public bool VomGebaeude(KonditionierungGroesse g)
        => IstZone && !Angelegt(g)
           && Stand.Konditionierung?.Spalte(g).Kalender.Zustand == KonditionierungZustand.Angelegt;

    /// <summary>
    /// Die Zustandszeile einer Größe an der Zone: „vom Gebäude", „eigener Kalender" oder „aus der Matrix".
    /// </summary>
    public string Zonenzustand(KonditionierungGroesse g)
        => VomGebaeude(g) ? Texte.ZustandGebaeude : Angelegt(g) ? Texte.ZustandEigen : Texte.ZustandMatrix;

    /// <summary>
    /// <b>„Vom Gebäude übernehmen und anpassen"</b> — die Zone bekommt eine eigene Kopie des angelegten
    /// Gebäudekalenders (Teilkonzept 3.4, 7.3); danach wirken ihre Zellen.
    /// </summary>
    public bool VomGebaeudeUebernehmen(KonditionierungGroesse g)
        => Bietet(KonditionierungHandlung.VomGebaeude) && VomGebaeude(g)
           && Ausfuehren("U|" + g, s => Weg.VomGebaeude!(s, Ort(g)));

    /// <summary>
    /// Was eine leere Bestandszelle der Zone erbt, wo der Weg kein Erbe liefert (ohne
    /// Konditionierungstabellen, ohne Gaben) — der Zonendialog reicht seine Vorgabenkaskade; <c>null</c> =
    /// „wie Gebäude".
    /// </summary>
    public Func<KonditionierungGroesse, KonditionierungZeile, double?>? Bestandserbe { get; set; }

    /// <summary>Die geerbten Zellen der Zone und die Nutzfläche, für die sie gebildet sind.</summary>
    private (KonditionierungDaten? Daten, double? Nutzflaeche, bool Gebildet) _erbe;

    /// <summary>
    /// <b>Was die Zone vom Gebäude erbt</b> — je Zelle der Wert, den eine leere Zelle gerade gälte
    /// (<see cref="KonditionierungWeg.Geerbt"/>); <c>null</c> am Gebäude, ohne Weg oder ohne Delegat.
    /// Neu gebildet, sobald die Nutzfläche der Zone wechselt (sie schlüsselt die Anteile).
    /// </summary>
    public KonditionierungDaten? Erbe
    {
        get
        {
            if (_zone is null || !MitWeg || Weg.Geerbt is null) return null;
            if (_erbe.Gebildet && _erbe.Nutzflaeche == _zone.Nutzflaeche) return _erbe.Daten;
            KonditionierungDaten? d;
            try { d = Weg.Geerbt(Eingabestand(), _zone.Id); }
            catch (Exception) { d = null; }
            _erbe = (d, _zone.Nutzflaeche, true);
            return d;
        }
    }

    /// <summary>
    /// <b>Der Platzhalter einer leeren Zonenzelle</b> (Teilkonzept 7.3): „Vorgabe {Wert}" aus dem Erbe
    /// („aus" wie in der Zelle, Anteile in %), ohne Angabe „wie Gebäude"; am Gebäude <c>null</c>.
    /// </summary>
    public string? Erbplatzhalter(KonditionierungGroesse g, KonditionierungZeile z)
    {
        if (!IstZone) return null;
        KonditionierungTexte t = Texte;
        KonditionierungZelle? c = Gibt(g, z) ? Erbe?.Spalte(g).Zelle(z) : null;
        if (c is null && Erbe is null && Bestandserbe?.Invoke(g, z) is double b)
            c = new KonditionierungZelle { Wert = b };
        if (c is null || (!c.Aus && c.Wert is null)) return t.PlatzhalterWieGebaeude;
        string wert = c.Aus
            ? t.ZelleAus
            : Zahlen.Anzeigetext(Math.Round(c.Wert!.Value, 4)) + (IstAnteil(g, z) ? " %" : "");
        return string.Format(CultureInfo.CurrentCulture, t.PlatzhalterVorgabe, wert);
    }

    /// <summary>
    /// Das Nachtfenster, das eine Zonenzelle ohne eigene Zeiten erbt (Spalte, sonst Heizspalte des
    /// Gebäudes); (<c>null</c>, <c>null</c>) = die Vorgabe 22–6 Uhr.
    /// </summary>
    public (int? Von, int? Bis) Erbfenster(KonditionierungGroesse g)
    {
        KonditionierungSpalte? s = Erbe?.Spalte(g), h = Erbe?.Spalte(KonditionierungGroesse.Heizen);
        return (s?.Nacht.Von ?? h?.Nacht.Von, s?.Nacht.Bis ?? h?.Nacht.Bis);
    }

    /// <summary>
    /// <b>Ein neuer Anfang</b> — nach einem Satzwechsel, „Verwerfen" oder dem Speichern des Wirts: Der
    /// Schritt für „Zurücknehmen", eine offene Rückfrage und das Erbe gelten nicht mehr.
    /// </summary>
    public void Neubeginn()
    {
        _letzter = null;
        OffeneFrage = null;
        LetzteMeldung = null;
        _erbe = default;
    }

    /// <summary>„Kalender anlegen" — der Generator schreibt den Kalender aus der Matrix in den Arbeitsstand.</summary>
    public bool Anlegen(KonditionierungGroesse g)
        => Bietet(KonditionierungHandlung.Anlegen)
           && Ausfuehren("A|" + g, s => Weg.Anlegen!(s, Ort(g)));

    /// <summary>„Verwerfen" — nach der Rückfrage (Vorgabe Nein) fällt der angelegte Kalender samt Perioden.</summary>
    public void Verwerfen(KonditionierungGroesse g)
    {
        if (!Bietet(KonditionierungHandlung.Verwerfen)) return;
        MitRueckfrage(KonditionierungHandlung.Verwerfen, g, Texte.KnopfVerwerfen,
                      () => Ausfuehren("V|" + g, s => Weg.Verwerfen!(s, Ort(g))));
    }

    /// <summary>„Matrix erneut anwenden…" — nach der Rückfrage ersetzt es nur den Matrixbereich (P12).</summary>
    public void MatrixErneut(KonditionierungGroesse g)
    {
        if (!Bietet(KonditionierungHandlung.MatrixErneut)) return;
        MitRueckfrage(KonditionierungHandlung.MatrixErneut, g, Texte.KnopfMatrixErneut,
                      () => Ausfuehren("M|" + g, s => Weg.MatrixErneut!(s, Ort(g))));
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

    /// <summary>
    /// <b>Die Feiertage als Regel</b> (F11): die neun bundeseinheitlichen Feiertage „wie Wochentag"
    /// (Vorgabe 7 = Sonntag) im Feiertagsband unter den Ferien; eine vorhandene Regel bleibt, wie sie ist.
    /// Der Vermerk kommt in die Herkunft (B8).
    /// </summary>
    public bool FeiertageAnlegen(KonditionierungGroesse g, int wieWochentag = 7)
        => Bietet(KonditionierungHandlung.Feiertage)
           && Ausfuehren("H|" + g, s => Weg.Feiertage!(s, new KonditionierungOrt(g), wieWochentag));

    /// <summary>
    /// <b>„Zeitstruktur übernehmen"</b> (Festlegung 11): Kühlen, Lüftung oder Geräte „wie Heizung" oder
    /// „wie Anwesenheit" — ersetzt nur die Standardwoche; der Vermerk kommt in die Herkunft (B8).
    /// </summary>
    public bool ZeitstrukturUebernehmen(KonditionierungGroesse g, KonditionierungZeitstruktur quelle)
        => Bietet(KonditionierungHandlung.Zeitstruktur)
           && Ausfuehren("Y|" + g, s => Weg.Zeitstruktur!(s, new KonditionierungOrt(g), quelle));

    /// <summary>
    /// <b>Das Teppichbild</b> der Größe (Teilkonzept 7.5, Festlegung 8) — Tage × Stunden des Kalenders, wie
    /// er gilt (angelegt, sonst aus der Matrix), gegen das Bezugsjahr des Wegs; <c>null</c> = keins (ohne
    /// Delegat, ohne Weg oder ohne Kalender). Die Karte ruft es entprellt (Schlüssel
    /// <see cref="Vorschauschluessel"/>).
    /// </summary>
    public WindowsFormsApplication1.Zeichnung.Zeichenmodell? Teppichbild(KonditionierungGroesse g)
    {
        if (!MitWeg || Weg.Teppichbild is null) return null;
        try { return Weg.Teppichbild(Eingabestand(), new KonditionierungOrt(g)); }
        catch (Exception) { return null; }
    }

    /// <summary>
    /// <b>„In den Kalender übernehmen"</b> an der Wärmeübergabe (Teilkonzept 5.5): das Sollwert-Zeitprogramm
    /// wird die Standardwoche des Heizkalenders. Vor einem angelegten Heizkalender fragt der Reiter
    /// (Vorgabe „Nein"); sonst legt der Weg ihn an und setzt die Woche — ein Schritt für „Zurücknehmen".
    /// <c>false</c> = der Weg bietet es nicht.
    /// </summary>
    public bool SollwertprofilUebernehmen()
    {
        if (!Bietet(KonditionierungHandlung.SollwertprofilUebernehmen)) return false;
        Action handlung = () => Ausfuehren("I", s => Weg.SollwertprofilUebernehmen!(s));
        if (Angelegt(KonditionierungGroesse.Heizen))
            OffeneFrage = new Rueckfrage(Knopftext(Texte.KnopfInDenKalender), Fragen.Sollwertprofil, VorgabeNein: true, handlung);
        else
            handlung();
        return true;
    }

    /// <summary>Die neun Feiertagsregeln der Periodenliste (F11); leer ohne Weg.</summary>
    public IReadOnlyList<KonditionierungFeiertag> Feiertagsregeln
        => MitWeg ? Weg.Feiertagsregeln ?? Array.Empty<KonditionierungFeiertag>() : Array.Empty<KonditionierungFeiertag>();

    /// <summary>
    /// <b>Eine eigene Periode anlegen oder ersetzen</b> (Festlegung 15): Zeitraum oder Feiertag;
    /// <paramref name="rang"/> <c>null</c> = neu im Eigenband. Ein Schritt für „Zurücknehmen".
    /// </summary>
    public bool PeriodeSetzen(KonditionierungGroesse g, int? rang, KonditionierungPeriode periode)
        => Bietet(KonditionierungHandlung.PeriodeSetzen)
           && Ausfuehren("P|" + g, s => Weg.PeriodeSetzen!(s, new KonditionierungOrt(g), rang, periode));

    /// <summary><b>Rang ▲▼</b> einer eigenen Periode im Eigenband (Festlegung 15).</summary>
    public bool RangVerschieben(KonditionierungGroesse g, int rang, bool hoeher)
        => Bietet(KonditionierungHandlung.RangVerschieben)
           && Ausfuehren("R|" + g, s => Weg.RangVerschieben!(s, new KonditionierungOrt(g), rang, hoeher));

    /// <summary><b>Eine eigene Periode löschen</b> (Festlegung 15) — „Zurücknehmen" holt sie zurück.</summary>
    public bool PeriodeLoeschen(KonditionierungGroesse g, int rang)
        => Bietet(KonditionierungHandlung.PeriodeLoeschen)
           && Ausfuehren("L|" + g, s => Weg.PeriodeLoeschen!(s, new KonditionierungOrt(g), rang));

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

    /// <summary>
    /// <b>Warum die Vorschau kein Bild hat</b> (Welle U3, offener Punkt aus U2): Geräte und Personen ohne
    /// einen Anteil in Tag, Nacht, Wochenende oder Ferien ergeben keinen Kalender — der Nennwert gilt in
    /// jeder Stunde bzw. die Last steckt in den inneren Wärmegewinnen; sonst der allgemeine Grund.
    /// </summary>
    public string VorschauLeergrund(KonditionierungGroesse g)
        => g is KonditionierungGroesse.Geraete or KonditionierungGroesse.Personen && !Angelegt(g)
           && !Zeilen.Any(z => IstAnteil(g, z) && Wert(g, z).HasValue)
            ? string.Format(CultureInfo.CurrentCulture, Texte.TextVorschauOhneAnteile, Groessenname(g))
            : Texte.TextVorschauLeer;

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
    /// (Umbenennen, Löschen, Duplizieren, Kopieren nach …) — „kein Delegat, kein Knopf".
    /// </summary>
    public bool MitVerwaltung
        => MitWeg && Weg.Vorlagen is not null
           && (Weg.Bietet(KonditionierungHandlung.VorlageUmbenennen) || Weg.Bietet(KonditionierungHandlung.VorlageLoeschen)
               || Weg.Bietet(KonditionierungHandlung.VorlageDuplizieren) || Weg.Bietet(KonditionierungHandlung.VorlageKopieren));

    /// <summary>Eine eigene Vorlage umbenennen — schreibt sofort; eine Ablehnung des Namens kommt mit <c>AmNamen</c>.</summary>
    public KonditionierungVorlageErgebnis VorlageUmbenennen(long id, string name)
        => Vorlagenhandlung(KonditionierungHandlung.VorlageUmbenennen, () => Weg.VorlageUmbenennen!(id, name));

    /// <summary>Eine eigene Vorlage löschen — schreibt sofort; kein Gebäude wird berührt.</summary>
    public KonditionierungVorlageErgebnis VorlageLoeschen(long id)
        => Vorlagenhandlung(KonditionierungHandlung.VorlageLoeschen, () => Weg.VorlageLoeschen!(id));

    /// <summary>Eine Vorlage duplizieren — auch eine ausgelieferte; die Kopie heißt „Name (Kopie)", eindeutig.</summary>
    public KonditionierungVorlageErgebnis VorlageDuplizieren(long id)
        => Vorlagenhandlung(KonditionierungHandlung.VorlageDuplizieren, () => Weg.VorlageDuplizieren!(id, ""));

    private static readonly IReadOnlyList<KonditionierungKopierziel> KEINE_ZIELE = Array.Empty<KonditionierungKopierziel>();

    /// <summary>
    /// Die erlaubten Ziele von „Kopieren nach …" für Vorlagen der Größe <paramref name="quelle"/> — der Kern
    /// entscheidet sie (Heizen → Kühlen, Geräte ↔ Personen); leer = keine, auch ohne Weg.
    /// </summary>
    public IReadOnlyList<KonditionierungKopierziel> Kopierziele(KonditionierungGroesse quelle)
    {
        if (!Bietet(KonditionierungHandlung.VorlageKopieren)) return KEINE_ZIELE;
        try
        {
            return Weg.Kopierziele!(quelle) ?? KEINE_ZIELE;
        }
        catch (Exception)
        {
            return KEINE_ZIELE;
        }
    }

    /// <summary>
    /// „Kopieren nach …" — eine Vorlage als eigene Vorlage einer anderen Größe; schreibt sofort. Eine Ablehnung
    /// des Namens kommt mit <c>AmNamen</c>, eine des Komfortsollwerts mit <c>AmSollwert</c>, eine des
    /// Absenksollwerts mit <c>AmAbsenkwert</c>.
    /// </summary>
    public KonditionierungVorlageErgebnis VorlageKopieren(long id, KonditionierungVorlageKopie kopie)
        => Vorlagenhandlung(KonditionierungHandlung.VorlageKopieren, () => Weg.VorlageKopieren!(id, kopie));

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
    // Die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…“ (E57; Stufe KP2, Welle U5)
    // =================================================================================

    /// <summary>
    /// Ein Eintrag der Liste „alle Größen“ (E57): ein Name aus mindestens einer der fünf Listen — ein Datenwert,
    /// nicht übersetzt (Glossar § 10) — und ob eine Vorlage dieses Namens zur Auslieferung gehört (das Schloss
    /// wie an der Karte).
    /// </summary>
    /// <param name="Name">Der Name, wie ihn die erste Liste führt, die ihn trägt (Heizen zuerst).</param>
    /// <param name="Ausgeliefert">Gehört eine Vorlage dieses Namens in einer der Listen zur Auslieferung?</param>
    public sealed record VorlagennameAlle(string Name, bool Ausgeliefert);

    /// <summary>
    /// <b>Bietet der Reiter die Abkürzung</b> „gleichnamige Vorlage in allen Größen übernehmen…“ (E57)? Mit den
    /// Vorlagen der Karten (<see cref="MitVorlagen"/>) am Gebäude und Katalogbau — die Karten einer Zone bieten
    /// keine Vorlagen (Teilkonzept 3.4), also auch keine Abkürzung.
    /// </summary>
    public bool MitVorlageAlle => MitVorlagen && !IstZone;

    /// <summary>
    /// <b>Die Liste „alle Größen“</b> (E57): jeder Name, der in MINDESTENS EINER der fünf Listen
    /// (<see cref="Vorlagen"/>) steht, ohne Dubletten — verglichen wie die Namensregel des Kerns (getrimmt,
    /// ohne Unterschied der Groß- und Kleinschreibung) — in der Reihenfolge des Kerns: die ausgelieferten
    /// zuerst, dann nach Name wie <c>COLLATE NOCASE</c> (<c>KonditionierungsvorlageCtrl.Vergleichen</c>). Ein
    /// Name steht so an derselben Stelle wie in jeder Karte. Leer ohne die Abkürzung.
    /// </summary>
    public IReadOnlyList<VorlagennameAlle> VorlagenAlle()
    {
        if (!MitVorlageAlle) return Array.Empty<VorlagennameAlle>();
        var namen = new List<VorlagennameAlle>();
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            foreach (KonditionierungVorlageDaten v in Vorlagen(g))
            {
                int i = namen.FindIndex(x => Namensgleich(x.Name, v.Name));
                if (i < 0) namen.Add(new VorlagennameAlle((v.Name ?? "").Trim(), v.Ausgeliefert));
                else if (v.Ausgeliefert && !namen[i].Ausgeliefert) namen[i] = namen[i] with { Ausgeliefert = true };
            }
        return namen.OrderBy(x => x.Ausgeliefert ? 0 : 1)
                    .ThenBy(x => AsciiKlein(x.Name), StringComparer.Ordinal)
                    .ToList();
    }

    /// <summary>
    /// Die Vorlage des Namens <paramref name="name"/> in der Liste der Größe — verglichen wie die Namensregel des
    /// Kerns; <c>null</c> = die Liste führt keine Vorlage dieses Namens.
    /// </summary>
    public KonditionierungVorlageDaten? VorlageGleichenNamens(KonditionierungGroesse g, string? name)
        => Vorlagen(g).FirstOrDefault(v => Namensgleich(v.Name, name));

    /// <summary>
    /// <b>Warum „Übernehmen“ einer Größe gesperrt steht</b> — die Sperre der Karte (<c>Kalenderkarte.UebernehmenSperre</c>),
    /// heute allein die Kühlspalte ohne „Gebäude wird gekühlt“, mit dem Grund, den die Wirte als Kühlsperre reichen
    /// (<see cref="KonditionierungTexte.GrundKuehlenGesperrt"/>; Welle U1: die Kühlspalte sperrt allein „Kühlung
    /// aktiv“). Die Abkürzung übergeht eine gesperrte Größe und nennt den Grund in ihrer Rückfrage; <c>null</c> = frei.
    /// </summary>
    public string? Uebernehmensperre(KonditionierungGroesse g)
        => g == KonditionierungGroesse.Kuehlen && !KuehlungAktivHier ? Texte.GrundKuehlenGesperrt : null;

    /// <summary>
    /// Wird am Ort der Bearbeitung gekühlt? Am Gebäude sein Schalter, an einer Zone ihr eigener
    /// (<see cref="ZoneDaten.KuehlungAktiv"/>, KU3-3), leer = der des Gebäudes.
    /// </summary>
    public bool KuehlungAktivHier => _zone?.KuehlungAktiv ?? Stand.KuehlungAktiv;

    /// <summary>Die Rückfrage der Abkürzung und ihr Name — die Wahl gilt, solange GENAU diese Frage steht.</summary>
    private Rueckfrage? _frageAlle;

    /// <summary>Der gewählte Name der Liste „alle Größen“ (siehe <see cref="GewaehlteVorlageAlle"/>).</summary>
    private string? _nameAlle;

    /// <summary>
    /// Die Wahl der Liste „alle Größen“ — nur, solange ihre Rückfrage steht; nach „Ja“, „Nein“ oder Abbrechen und
    /// nach jedem neuen Anfang wieder „—“. Die Wahl ändert den Arbeitsstand nicht.
    /// </summary>
    public string? GewaehlteVorlageAlle
        => _frageAlle is not null && ReferenceEquals(OffeneFrage, _frageAlle) ? _nameAlle : null;

    /// <summary>
    /// <b>„Gleichnamige Vorlage in allen Größen übernehmen…“</b> (E57): die Wahl eines Namens der Liste „alle
    /// Größen“ in der Zeile „Vorlage“. Sie stellt SOFORT EINE Rückfrage (Vorgabe „Nein“) aus den Befunden aller
    /// Größen — vor dem Schreiben, aus dem Arbeitsstand (<see cref="VorlageAlleFragetext"/>). Weil die Wahl in einer
    /// Liste ein einziger Griff ist, der sonst fünf Kalender schriebe, fragt der Reiter IMMER, auch ohne Befund
    /// (wie <c>immer: true</c>). „Ja“ übernimmt als EIN Schritt (<see cref="VorlageAlleUebernehmen"/>); „Nein“ und
    /// Abbrechen lassen alles, die Wahl steht danach wieder auf „—“. <c>false</c> = keine Abkürzung, ein Name, den
    /// keine Liste führt, oder ein Befund scheiterte (gemeldet).
    /// </summary>
    public bool VorlageAlleWaehlen(string? name)
    {
        if (!MitVorlageAlle || string.IsNullOrWhiteSpace(name)) return false;
        VorlagennameAlle? eintrag = VorlagenAlle().FirstOrDefault(x => Namensgleich(x.Name, name));
        if (eintrag is null) return false;
        if (VorlageAlleFragetext(eintrag.Name) is not string text) return false;
        string gewaehlt = eintrag.Name;
        var frage = new Rueckfrage(Fragen.VorlageAlleTitel, text, VorgabeNein: true, () => VorlageAlleUebernehmen(gewaehlt));
        _nameAlle = gewaehlt;
        _frageAlle = frage;
        OffeneFrage = frage;
        return true;
    }

    /// <summary>
    /// <b>Der Text der einen Rückfrage</b> (E57): der Satz „Die Vorlage „Büro“ in allen Größen übernehmen?“ und
    /// je Größe EINE Zeile — „übernehmen“; am angelegten Kalender, was ersetzt wird und was bleibt (P12, der Befund
    /// des Wegs <see cref="KonditionierungWeg.Rueckfrage"/>); an der Gesamtangabe der Lüftung die Aufteilung (E56
    /// F5 (a), gerechnet wie die Rückfrage „aufteilen“ auf einer Probe); „keine Vorlage dieses Namens — bleibt“;
    /// „gesperrt — Grund“ (<see cref="Uebernehmensperre"/>) —, dazu die betroffenen Zonen mit Namen. Geändert wird
    /// nichts; <c>null</c> = ein Befund scheiterte (gemeldet).
    /// </summary>
    public string? VorlageAlleFragetext(string name)
    {
        CultureInfo c = CultureInfo.CurrentCulture;
        KonditionierungFragetexte f = Fragen;
        var zeilen = new List<string> { string.Format(c, f.VorlageAlle, name) };
        var zonen = new List<string>();
        try
        {
            KonditionierungStand stand = Eingabestand();
            foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            {
                string teil;
                if (Uebernehmensperre(g) is string sperre)
                    teil = string.Format(c, f.VorlageAlleGesperrt, sperre);
                else if (VorlageGleichenNamens(g, name) is not KonditionierungVorlageDaten v)
                    teil = f.VorlageAlleOhne;
                else
                {
                    var teile = new List<string>();
                    KonditionierungRueckfrage? befund = Weg.Rueckfrage?.Invoke(stand, Ort(g), KonditionierungHandlung.VorlageUebernehmen);
                    foreach (string zone in befund?.Zonen ?? Array.Empty<string>())
                        if (!zonen.Contains(zone)) zonen.Add(zone);
                    if (befund is not null && Angelegt(g))
                        teile.Add(string.Format(c, f.VorlageAlleErsetzt, Posten(befund.Ersetzt), Posten(befund.Bleibt)));
                    if (Aufteilung(stand, g, v.Id) is { } aufteilung)
                        teile.Add(string.Format(c, f.VorlageAlleAufteilen, Zahlen.Anzeigetext(aufteilung.Rate),
                                                Zahlen.Anzeigetext(aufteilung.Infiltration), Zahlen.Anzeigetext(aufteilung.Nutzer)));
                    teil = teile.Count > 0 ? string.Join("; ", teile) : f.VorlageAlleUebernehmen;
                }
                zeilen.Add(string.Format(c, f.VorlageAlleZeile, Groessenname(g), teil));
            }
        }
        catch (Exception ex)
        {
            Fehler(ex.Message);
            return null;
        }
        if (zonen.Count > 0) zeilen.Add(string.Format(c, f.Zonen, string.Join(", ", zonen)));
        return string.Join("\n", zeilen);
    }

    /// <summary>
    /// Verlangt „Übernehmen“ der Vorlage <paramref name="id"/> in der Größe zuerst „aufteilen“ (E56 F5 (a))? Dann
    /// die Aufteilung, die der Weg auf einer Probe rechnet — Rate, Infiltration, Nutzerlüftung, wie in der
    /// Rückfrage „aufteilen“ (<see cref="AufteilenFragen"/>); sonst <c>null</c>. Geändert wird nichts.
    /// </summary>
    private (double? Rate, double? Infiltration, double? Nutzer)? Aufteilung(KonditionierungStand stand,
                                                                            KonditionierungGroesse g, long id)
    {
        if (Weg.VorlageUebernehmen is null || Weg.LuftwechselAufteilen is null) return null;
        KonditionierungErgebnis e = Weg.VorlageUebernehmen(stand, Ort(g), id);
        if (e.Ok || e.Rueckfrage is null) return null;
        KonditionierungErgebnis probe = Weg.LuftwechselAufteilen(stand);
        if (!probe.Ok || probe.Stand is null) return null;
        return (stand.Gebaeude.Luftwechselrate, probe.Stand.Gebaeude.LuftwechselInfiltration,
                probe.Stand.Gebaeude.LuftwechselNutzer);
    }

    /// <summary>
    /// <b>„Ja“ der Abkürzung</b> (E57): der Reihe nach Heizen, Kühlen, Lüftung, Geräte, Personen über den Weg
    /// „Übernehmen“ (<see cref="KonditionierungWeg.VorlageUebernehmen"/>) in den Arbeitsstand — eine Größe ohne
    /// gleichnamige Vorlage und eine gesperrte übergeht er; verlangt der Weg „aufteilen“ (E56 F5 (a)), teilt er
    /// zuerst auf (<see cref="KonditionierungWeg.LuftwechselAufteilen"/>), denn die Antwort der einen Rückfrage
    /// deckt das. EIN Schritt für „Zurücknehmen“ (Schlüssel <c>TA|name</c>); der Fehler eines Schritts bricht ab
    /// und meldet, der Arbeitsstand bleibt der von davor. Danach ist auch die Wahl der Karten leer, die übernommen
    /// haben (E56 F2 (a)): Karten und Zeile „Vorlage“ nennen die Herkunft je Größe.
    /// </summary>
    private void VorlageAlleUebernehmen(string name)
    {
        if (!MitVorlageAlle || Weg.VorlageUebernehmen is null) return;
        KonditionierungStand vor = Eingabestand();
        KonditionierungStand s = vor;
        var uebernommen = new List<KonditionierungGroesse>();
        try
        {
            foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            {
                if (Uebernehmensperre(g) is not null || VorlageGleichenNamens(g, name) is not KonditionierungVorlageDaten v)
                    continue;
                KonditionierungErgebnis e = Weg.VorlageUebernehmen(s, Ort(g), v.Id);
                if (!e.Ok && e.Rueckfrage is not null && Weg.LuftwechselAufteilen is not null)
                {
                    // „aufteilen“ (E56 F5 (a)): Die eine Rückfrage hat die Aufteilung genannt - keine zweite Frage.
                    KonditionierungErgebnis a = Weg.LuftwechselAufteilen(s);
                    if (!a.Ok || a.Stand is null)
                    {
                        Fehler(a.Meldung);
                        return;
                    }
                    s = a.Stand;
                    e = Weg.VorlageUebernehmen(s, Ort(g), v.Id);
                }
                if (!e.Ok || e.Stand is null)
                {
                    Fehler(e.Rueckfrage is not null ? Texte.TextPostenLuftwechsel : e.Meldung);
                    return;
                }
                s = e.Stand;
                uebernommen.Add(g);
            }
        }
        catch (Exception ex)
        {
            Fehler(ex.Message);
            return;
        }
        if (uebernommen.Count == 0) return;
        Uebernehmen(vor, s);
        foreach (KonditionierungGroesse g in uebernommen) _gewaehlt.Remove(g);
        Merken("TA|" + name, vor, Eingabestand());
    }

    /// <summary>
    /// <b>Die gemeinsame Herkunft</b> (Lesen von <c>kond_vorlage_alle</c>): der Name, den JEDE ungesperrte Größe
    /// mit einer Vorlage dieses Namens als Herkunft trägt — mindestens eine; <c>null</c> = keine gemeinsame.
    /// </summary>
    public string? HerkunftAlle()
    {
        foreach (VorlagennameAlle n in VorlagenAlle())
        {
            bool getragen = false, abweichend = false;
            foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            {
                if (Uebernehmensperre(g) is not null || VorlageGleichenNamens(g, n.Name) is null) continue;
                if (Namensgleich(Herkunft(g), n.Name)) getragen = true;
                else abweichend = true;
            }
            if (getragen && !abweichend) return n.Name;
        }
        return null;
    }

    /// <summary>Zwei Namen sind gleich wie in der Namensregel des Kerns: getrimmt, ohne Unterschied der Groß- und Kleinschreibung.</summary>
    private static bool Namensgleich(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Faltet nur A … Z auf a … z — wie <c>COLLATE NOCASE</c> in der Reihenfolge der Listen des Kerns.</summary>
    private static string AsciiKlein(string s)
    {
        char[] z = (s ?? "").ToCharArray();
        for (int i = 0; i < z.Length; i++)
            if (z[i] >= 'A' && z[i] <= 'Z') z[i] = (char)(z[i] + ('a' - 'A'));
        return new string(z);
    }

    // =================================================================================
    // Nutzungsprofil übernehmen (Stufe NP3b; Konzept Nutzungsprofile 6.2, NP-F13, NP-F17, NP-F18)
    // =================================================================================

    private static readonly IReadOnlyList<KonditionierungProfilwahl> KEINE_PROFILE = Array.Empty<KonditionierungProfilwahl>();

    /// <summary>Die Liste des Katalogs, einmal gelesen; <c>null</c> = noch nicht.</summary>
    private IReadOnlyList<KonditionierungProfilwahl>? _profile;

    /// <summary>Der Probelauf des gewählten Profils am Arbeitsstand; <c>null</c> = noch keiner.</summary>
    private (long Id, KonditionierungProfilergebnis Ergebnis)? _profilprobe;

    /// <summary>Bietet der Weg „Nutzungsprofil übernehmen…" an („kein Delegat, kein Knopf")?</summary>
    public bool MitProfilen => Bietet(KonditionierungHandlung.ProfilUebernehmen);

    /// <summary>Die Profile des Katalogs in seiner Ordnung — leer ohne den Weg oder bei einem Lesefehler (gemeldet).</summary>
    public IReadOnlyList<KonditionierungProfilwahl> Profile()
    {
        if (!MitProfilen) return KEINE_PROFILE;
        if (_profile is not null) return _profile;
        try
        {
            _profile = Weg.Nutzungsprofile!() ?? KEINE_PROFILE;
        }
        catch (Exception ex)
        {
            Fehler(ex.Message);
            _profile = KEINE_PROFILE;
        }
        return _profile;
    }

    /// <summary>
    /// <b>Die Vorschläge der Nutzung</b> für „Als Vorlage speichern…" (NP-F15): die Namen des Katalogs der Nutzungsprofile,
    /// jeder einmal; <c>null</c> ohne den Katalog — dann bleibt der freie Text.
    /// </summary>
    public IReadOnlyList<string>? Nutzungsvorschlaege()
        => MitProfilen ? Profile().Select(p => p.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList() : null;

    /// <summary>Liest die Liste beim nächsten Zugriff neu — nach dem Blatt „Nutzungsprofile".</summary>
    public void ProfileNeuLaden()
    {
        _profile = null;
        _profilprobe = null;
    }

    /// <summary>Das gewählte Profil der Auswahl; <c>null</c> = keines.</summary>
    public long? GewaehltesProfil { get; private set; }

    /// <summary>Der Eintrag des gewählten Profils; <c>null</c> = keiner.</summary>
    public KonditionierungProfilwahl? Profilwahl
        => GewaehltesProfil is long id ? Profile().FirstOrDefault(p => p.Id == id) : null;

    /// <summary>Wählt ein Profil der Liste (Vorschau und Nennwertzeile folgen); <c>false</c> = nicht in der Liste.</summary>
    public bool ProfilWaehlen(long? id)
    {
        if (id is long w && Profile().All(p => p.Id != w)) return false;
        GewaehltesProfil = id;
        _profilprobe = null;
        return true;
    }

    /// <summary>
    /// <b>Die Fläche des Ziels</b> für die Nennwerte (Q39, Q40): an der Zone ihre Nutzfläche, am Gebäude und Katalogbau
    /// die Nutzfläche des Feldsatzes (<see cref="GebaeudeKatalogDaten.WohnflaecheGesamt"/>); <c>null</c> = ohne.
    /// </summary>
    public double? Zielflaeche => Positiv(_zone is ZoneDaten z ? z.Nutzflaeche : Stand.WohnflaecheGesamt);

    /// <summary>
    /// <b>Die lichte Höhe des Ziels</b> für Außenluft in m³/(h·m²) (NP-F10): an der Zone ihre Raumhöhe, leer die des
    /// Gebäudes, am Gebäude und Katalogbau die Raumhöhe des Feldsatzes; <c>null</c> = ohne — dann setzt die Übernahme die
    /// Lüftung eines flächenbezogenen Profils benannt nicht.
    /// </summary>
    public double? Zielhoehe => Positiv(_zone is ZoneDaten z ? z.Raumhoehe ?? Stand.Raumhoehe : Stand.Raumhoehe);

    private static double? Positiv(double? w) => w is double x && double.IsFinite(x) && x > 0.0 ? x : null;

    /// <summary>Die Anfrage an den Weg für das Profil <paramref name="id"/> am Ort dieser Bearbeitung.</summary>
    public KonditionierungProfilanfrage Profilanfrage(long id) => new(id, _zone?.Id, Zielflaeche, Zielhoehe);

    /// <summary>
    /// <b>Der Probelauf</b> des gewählten Profils am Arbeitsstand — was „Übernehmen" eintrüge, ohne etwas zu ändern;
    /// Grundlage von Vorschau, Nennwertzeile und Rückfrage. <c>null</c> ohne Wahl.
    /// </summary>
    public KonditionierungProfilergebnis? Profilprobe()
    {
        if (!MitProfilen || GewaehltesProfil is not long id) return null;
        if (_profilprobe is { } p && p.Id == id) return p.Ergebnis;
        KonditionierungProfilergebnis e;
        try
        {
            e = Weg.ProfilUebernehmen!(Eingabestand(), Profilanfrage(id)) ?? KonditionierungProfilergebnis.Fehler("");
        }
        catch (Exception ex)
        {
            e = KonditionierungProfilergebnis.Fehler(ex.Message);
        }
        _profilprobe = (id, e);
        return e;
    }

    /// <summary>
    /// <b>Die Nennwertzeilen</b> des Probelaufs (NP-F18): je Größe mit Nennwert „Nennwert Geräte: 8 W/m² × 120 m² =
    /// 960 W". Leer ohne Wahl, ohne Fläche des Ziels oder ohne einen Kennwert dafür.
    /// </summary>
    public IReadOnlyList<string> Profilnennwerte(RaumnutzungTexte t)
        => (Profilprobe() is { Ok: true } e ? e.Posten : Array.Empty<KonditionierungProfilposten>())
           .Where(p => p.Uebernommen && !string.IsNullOrEmpty(p.Nennwert))
           .Select(p => string.Format(CultureInfo.CurrentCulture, t.Nennwertzeile, Groessenname(p.Groesse), p.Nennwert))
           .ToList();

    /// <summary>
    /// <b>Die Nutzungstage am Ziel</b> aus dem Probelauf (E93): abgeleitet aus Wochenmuster und Feiertagen, abzüglich der
    /// Ferientage des Gebäudes („…, abzüglich 30 Ferientage = 222"). Leer ohne Wahl oder für ein Profil ohne Werte.
    /// </summary>
    public string Profilnutzungstage()
        => Profilprobe() is { Ok: true } e ? e.Nutzungstage ?? "" : "";

    /// <summary>Die Hinweise des Probelaufs, jeder einmal (NP-F10: Außenluft ohne Höhe, …).</summary>
    public IReadOnlyList<string> Profilhinweise()
        => (Profilprobe() is { Ok: true } e ? e.Posten : Array.Empty<KonditionierungProfilposten>())
           .Select(p => p.Hinweis).Where(h => !string.IsNullOrEmpty(h)).Distinct().ToList();

    /// <summary>
    /// <b>Warum „Übernehmen" nicht geht</b> (weiche Sperre mit Grund): ohne Wahl; die benannte Ablehnung des Probelaufs;
    /// ein Profil ohne Werte am Gebäude oder Katalogbau (NP-F13 — an einer Zone geht wenigstens der Name ans Ziel).
    /// <c>null</c> = es geht.
    /// </summary>
    public string? Profilsperre(RaumnutzungTexte t)
    {
        if (GewaehltesProfil is null) return t.GrundOhneWahl;
        KonditionierungProfilergebnis? e = Profilprobe();
        if (e is null) return t.GrundOhneWahl;
        if (!e.Ok) return string.IsNullOrEmpty(e.Meldung) ? t.GrundOhneWahl : e.Meldung;
        if (!IstZone && !e.Posten.Any(p => p.Uebernommen))
            return e.OhneWerte ? t.GrundOhneWerte : Profilhinweise().FirstOrDefault() ?? t.GrundOhneWerte;
        return null;
    }

    /// <summary>
    /// <b>Die Woche der Vorschau</b> einer Größe aus dem Probelauf — der Kalender, den „Übernehmen" am Ort anlegte;
    /// <c>null</c> = die Größe bekommt keinen (nicht belegt, übersprungen, ohne Werte).
    /// </summary>
    public double[]? Profilwoche(KonditionierungGroesse g)
    {
        if (Profilprobe() is not { Ok: true, Stand: KonditionierungStand neu } e) return null;
        if (!e.Posten.Any(p => p.Groesse == g && p.Uebernommen)) return null;
        KonditionierungDaten? daten = _zone is ZoneDaten z
            ? neu.Zonen.FirstOrDefault(x => x.Id == z.Id)?.Konditionierung
            : neu.Gebaeude.Konditionierung;
        return Wochenwerte(daten?.Spalte(g).Kalender);
    }

    /// <summary>Das Bild der Wochenvorschau aus dem Probelauf (<see cref="KonditionierungWeg.WochenVorschau"/>); <c>null</c> = keines.</summary>
    public WindowsFormsApplication1.Zeichnung.Zeichenmodell? Profilvorschau(KonditionierungGroesse g) => Bild(g, Profilwoche(g));

    /// <summary>
    /// <b>„Übernehmen"</b> der Auswahl (NP-F17, P12): stellt EINE Rückfrage aus dem Probelauf — je Größe „wird
    /// übernommen", „ersetzt den Matrixbereich …" (Vorgabe „Nein", sobald ein Kalender ersetzt wird), „nicht belegt —
    /// bleibt" oder „unbeheizte Zone — übersprungen", dazu Hinweise und die Nennwertzeilen mit Herleitung (NP-F18).
    /// „Ja" trägt das Profil als EIN Schritt in den Arbeitsstand; geschrieben wird mit dem OK des Editors.
    /// <c>false</c> = gesperrt (<see cref="Profilsperre"/>, gemeldet).
    /// </summary>
    public bool ProfilUebernehmenFragen(RaumnutzungTexte t)
    {
        if (Profilsperre(t) is string sperre)
        {
            Melden?.Invoke(sperre, WarnStufe.Hinweis);
            return false;
        }
        long id = GewaehltesProfil!.Value;
        KonditionierungProfilergebnis e = Profilprobe()!;
        OffeneFrage = new Rueckfrage(t.TitelUebernahme, Profilfragetext(e, t), e.Posten.Any(p => p.Ersetzt),
                                     () => ProfilEintragen(id, t));
        return true;
    }

    /// <summary>Der Text der Rückfrage aus einem Probelauf (siehe <see cref="ProfilUebernehmenFragen"/>).</summary>
    public string Profilfragetext(KonditionierungProfilergebnis e, RaumnutzungTexte t)
    {
        CultureInfo c = CultureInfo.CurrentCulture;
        var zeilen = new List<string> { string.Format(c, t.FrageUebernehmen, e.Profilname) };
        if (e.OhneWerte) zeilen.Add(t.FrageOhneWerte);
        else
        {
            foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            {
                if (e.Posten.FirstOrDefault(p => p.Groesse == g) is not KonditionierungProfilposten p) continue;
                var teile = new List<string>
                {
                    p.Unbeheizt ? t.FrageUnbeheizt
                    : !p.Uebernommen ? t.FrageBleibt
                    : p.Ersetzt ? t.FrageErsetzt
                    : t.FrageUebernimmt,
                };
                if (!string.IsNullOrEmpty(p.Hinweis)) teile.Add(p.Hinweis);
                if (p.Uebernommen && !string.IsNullOrEmpty(p.Nennwert))
                    teile.Add(string.Format(c, t.Nennwertzeile, Groessenname(g), p.Nennwert));
                zeilen.Add(string.Format(c, t.FrageZeile, Groessenname(g), string.Join("; ", teile)));
            }
            if (e.Aufgeteilt) zeilen.Add(t.FrageAufteilen);
        }
        if (IstZone) zeilen.Add(string.Format(c, t.FrageName, e.Profilname));
        return string.Join("\n", zeilen);
    }

    /// <summary>
    /// „Ja" der Rückfrage: das Profil als EIN Schritt in den Arbeitsstand (Schlüssel <c>NP|id</c> für „Zurücknehmen"),
    /// an einer Zone samt ihrem Profilnamen (<see cref="ZoneDaten.Nutzungsprofil"/>); eine Ablehnung des Wegs wird
    /// gemeldet, der Arbeitsstand bleibt.
    /// </summary>
    private void ProfilEintragen(long id, RaumnutzungTexte t)
    {
        KonditionierungStand vor = Eingabestand();
        KonditionierungProfilergebnis e;
        try
        {
            e = Weg.ProfilUebernehmen!(vor, Profilanfrage(id)) ?? KonditionierungProfilergebnis.Fehler("");
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
        Uebernehmen(vor, e.Stand);
        Merken("NP|" + id.ToString(CultureInfo.InvariantCulture), vor, Eingabestand());
        _profilprobe = null;
        Melden?.Invoke(string.Format(CultureInfo.CurrentCulture, t.TextUebernommen, e.Profilname), WarnStufe.Hinweis);
    }

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
                befund = Weg.Rueckfrage(Eingabestand(), Ort(g), h);
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

        if (_zone is ZoneDaten eigene)
        {
            // An einer Zone: nur ihre Felder und ihre Konditionierung.
            ZoneDaten? zv = s.Vorher.Zonen.FirstOrDefault(x => x.Id == eigene.Id);
            ZoneDaten? zn = s.Nachher.Zonen.FirstOrDefault(x => x.Id == eigene.Id);
            if (zv is null || zn is null) return true;
            foreach (Zonenfeld f in ZONENFELDER)
                if (Gleich(f.Lesen(eigene), f.Lesen(zn))) f.Setzen(eigene, f.Lesen(zv));
            eigene.Konditionierung = ZurueckOderLeer(eigene.Konditionierung, zv.Konditionierung, zn.Konditionierung);
            return true;
        }

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

    /// <summary>
    /// Wie <see cref="Zurueck"/>, auch wenn die Zone davor noch keine Konditionierung trug (eine neue Zone
    /// vor ihrer ersten Zelle): dann eine leere mit neuer Fassung — der OK-Weg schreibt sie leer.
    /// </summary>
    private static KonditionierungDaten? ZurueckOderLeer(KonditionierungDaten? jetzt, KonditionierungDaten? vor,
                                                        KonditionierungDaten? nach)
    {
        if (vor is not null || jetzt is null || nach is null || jetzt.Fassung != nach.Fassung) return Zurueck(jetzt, vor, nach);
        return new KonditionierungDaten { Fassung = jetzt.Fassung + 1 };
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
        List<ZoneDaten> zonen = _arbeit.Zonen.Select(z => z.Kopie()).ToList();
        if (_zone is ZoneDaten eigene)
        {
            // An einer Zone: ihr Arbeitsstand an ihrer Stelle - eine neue Zone steht noch nicht in der
            // Liste des Wirts (erst sein OK legt sie an) und kommt ans Ende.
            int i = zonen.FindIndex(z => z.Id == eigene.Id);
            if (i >= 0) zonen[i] = eigene.Kopie();
            else zonen.Add(eigene.Kopie());
        }
        return new KonditionierungStand(g, zonen);
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
        if (IstZone)
        {
            // Die Aufteilung ändert das Gebäude - das liest der Zonendialog nur (Stufe KP2, Welle U4).
            Fehler(Texte.HinweisZoneAufteilen);
            return;
        }
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
        if (_zone is ZoneDaten eigene)
        {
            // An einer Zone: nur sie - das Gebäude und die übrigen Zonen liest der Zonendialog bloß.
            if (neu.Zonen.FirstOrDefault(x => x.Id == eigene.Id) is not ZoneDaten zn) return;
            foreach (Zonenfeld f in ZONENFELDER) f.Setzen(eigene, f.Lesen(zn));
            eigene.Konditionierung = zn.Konditionierung;
            return;
        }
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
        new(z => z.KuehlSollwert, (z, w) => z.KuehlSollwert = (double?)w),
        new(z => z.KuehlSollwertNacht, (z, w) => z.KuehlSollwertNacht = (double?)w),
        new(z => z.LuftwechselInfiltration, (z, w) => z.LuftwechselInfiltration = (double?)w),
        new(z => z.LuftwechselNutzer, (z, w) => z.LuftwechselNutzer = (double?)w),
        new(z => z.InterneWaermegewinne, (z, w) => z.InterneWaermegewinne = (double?)w),
        // NP3b: der Name des übernommenen Nutzungsprofils (Tab_Zone.Nutzungsprofil) reist mit dem Schritt.
        new(z => z.Nutzungsprofil, (z, w) => z.Nutzungsprofil = (string?)w),
    };

    private static bool Gleich(object? a, object? b) => Equals(a, b);

    private static bool Gleich(int[]? a, int[]? b)
        => a is null ? b is null : b is not null && a.SequenceEqual(b);
}
