using System.Globalization;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Der ARBEITSSTAND eines Gebäude-Katalogsatzes samt seinen Regeln</b> — die EINE Fassung,
/// mit der der Katalogeditor (<c>GebaeudeKatalogDialog</c>) und das Stammblatt der
/// Gebäudeverwaltung (<c>GebaeudeAdminDialog</c>, Welle #465) arbeiten.
///
/// <para><b>Eine Wahrheit.</b> Bis #465 standen Prüfung, Ableitungen und die Rechnung der
/// Hülle im Editor allein, und die Verwaltung schrieb über einen eigenen, schmaleren Weg nur
/// fünf Kenndaten. Jetzt bearbeiten beide Dialoge denselben Feldsatz
/// (<see cref="GebaeudeKatalogDaten"/>), prüfen mit <see cref="Pruefen"/>, leiten mit
/// <see cref="Ableiten"/> ab und schreiben über den Weg der Hülle
/// (<c>GebaeudeKatalogHuelle.Schreiben</c>). Nur die Anordnung ist je Dialog eine andere:
/// zwei Reiter im Editor, Gruppen im Stammblatt.</para>
///
/// <para><b>Geschrieben wird im Dialog, nicht hier.</b> Die Klasse kennt weder Datenbank noch
/// Hülle; sie hält den Stand, bis der Dialog ihn über seinen Speicherweg gibt. Der
/// hereingereichte Satz bleibt unberührt — <see cref="Laden"/> arbeitet auf einer tiefen
/// Kopie.</para>
/// </summary>
public sealed class GebaeudeArbeitsstand
{
    // =====================================================================
    //  Der Stand
    // =====================================================================

    /// <summary>Der Feldsatz, den die Felder bearbeiten — eine tiefe Kopie des geladenen Satzes.</summary>
    public GebaeudeKatalogDaten Stand { get; private set; } = new();

    /// <summary>Der Rechenweg beim Laden — der Schalter kehrt dorthin zurück, NULL bleibt NULL.</summary>
    public string? ModellBeimLaden { get; private set; }

    /// <summary>Die Randbedingung beim Laden — Erdreich bleibt NULL, wenn es NULL war.</summary>
    public string? RandBeimLaden { get; private set; }

    /// <summary>Soll die Bauweise vor dem Schreiben aus Bauart und Nutzfläche entstehen?</summary>
    public bool BauweiseNachfuehren { get; private set; }

    /// <summary>Die Felder, deren Text gerade keine gültige Zahl ist (Feldnamen der Meldung).</summary>
    public HashSet<string> Fehlerfelder { get; } = new();

    /// <summary>
    /// Die Ferien als Tag und Monat — die Zerlegung der Jahrestage für die Anzeige; sie
    /// wandern erst in <see cref="Ableiten"/> als Jahrestage in den Stand.
    /// </summary>
    public int?[] BeginnTag { get; } = new int?[4];

    /// <summary>Der Monat des Ferienbeginns je Zeitraum.</summary>
    public int?[] BeginnMonat { get; } = new int?[4];

    /// <summary>Der Tag des Ferienendes je Zeitraum.</summary>
    public int?[] EndeTag { get; } = new int?[4];

    /// <summary>Der Monat des Ferienendes je Zeitraum.</summary>
    public int?[] EndeMonat { get; } = new int?[4];

    /// <summary>
    /// Übernimmt einen Satz als Arbeitsstand — beim Öffnen, beim Satzwechsel und beim
    /// Verwerfen. <paramref name="neu"/>: ein Satz, der erst entsteht; seine Bauweise folgt
    /// dann immer der Bauart.
    /// </summary>
    public void Laden(GebaeudeKatalogDaten? satz, bool neu)
    {
        Stand = (satz ?? new GebaeudeKatalogDaten()).Kopie();
        ModellBeimLaden = Stand.Modell;
        RandBeimLaden = Stand.GrundflaecheRandbedingung;
        ArtBeimLaden = Stand.UebergabeArt;
        KuehlArtBeimLaden = Stand.KuehlUebergabeArt;
        BandBeimLaden = Stand.ReglerProportionalband;
        BandFrei = false;
        _heizkurveVorgeschlagen = false;
        BauweiseNachfuehren = neu || Stand.Bauweise <= 0;
        Fehlerfelder.Clear();
        _uebergabeFehlerfelder.Clear();
        _kuehlFehlerfelder.Clear();
        FerienZerlegen();
        ProfilLesen();
    }

    /// <summary>
    /// Nach einem gelungenen Schreiben: Was geschrieben ist, ist der neue Ausgangspunkt der
    /// Wahlen, die zu ihrem geladenen Wert zurückkehren.
    /// </summary>
    public void Geschrieben()
    {
        ModellBeimLaden = Stand.Modell;
        RandBeimLaden = Stand.GrundflaecheRandbedingung;
        ArtBeimLaden = Stand.UebergabeArt;
        KuehlArtBeimLaden = Stand.KuehlUebergabeArt;
        BandBeimLaden = Stand.ReglerProportionalband;
        _heizkurveVorgeschlagen = false;
    }

    // =====================================================================
    //  Die Zonen eines Projektgebäudes (Gebäudesimulation G3, Welle D2; G6a)
    // =====================================================================
    //
    // Ein Katalogsatz trägt keine Zonen (Softwarearchitektur 2.9); ein Gebäude im Projekt bis zu
    // GebaeudeZonenregeln.Hoechstzahl() (G6a), und der Lauf rechnet sie alle (G6b). Die Zonen gehören
    // zum Arbeitsstand wie die Felder: Übernehmen, Anlegen, Öffnen, Duplizieren, Umordnen und
    // Entfernen ändern nur ihn, geschrieben wird im OK-Weg des Editors (Softwarearchitektur 3.3).
    // JEDE Änderung trifft ihre Zone über die Id — nie über „die erste" (G6a, Risiko 2: ein Ersetzen
    // der ganzen Liste löschte beim OK still alle übrigen Zonen).

    /// <summary>Die Zonen im Arbeitsstand in Listenfolge (= Rang); leer = keine Zone (Klassenweg).</summary>
    public List<ZoneDaten> Zonen { get; private set; } = new();

    /// <summary>Die Zonen beim Laden bzw. nach dem letzten Schreiben — Vergleich für <see cref="ZonenGeaendert"/>.</summary>
    private List<ZoneDaten> _zonenGeschrieben = new();

    /// <summary>
    /// Die kleinste vergebene vorläufige Id. Eine neue Zone bekommt eine Id DARUNTER — nie eine, die
    /// schon einmal vergeben war: −1 trägt die Übernahmezone (<c>GebaeudeZonenabbildung</c>), und
    /// die Hülle hält zu ihr die Spalten der Übernahme fest; eine spätere neue Zone mit derselben Id
    /// erbte sie still.
    /// </summary>
    private int _kleinsteId = -1;

    /// <summary>Führt der Arbeitsstand ein Gebäude im Projekt (mit Zonenweg)? Ein Katalogsatz nicht.</summary>
    public bool MitZonenweg { get; private set; }

    /// <summary>
    /// Die Luftströme zwischen den Zonen im Arbeitsstand (Stufe G6b) — sie gehören zum Arbeitsstand wie
    /// die Zonen: Der Dialog „Luftaustausch" und das Entfernen einer Zone ändern nur ihn.
    /// </summary>
    public List<ZonenluftstromDaten> Luftstroeme { get; private set; } = new();

    /// <summary>Die Luftströme beim Laden bzw. nach dem letzten Schreiben — Vergleich für <see cref="LuftGeaendert"/>.</summary>
    private List<ZonenluftstromDaten> _luftGeschrieben = new();

    /// <summary>Übernimmt die Zonen eines Projektgebäudes — beim Öffnen des Editors in der Betriebsart Projekt.</summary>
    public void ZonenLaden(IReadOnlyList<ZoneDaten>? zonen, bool mitZonenweg, IReadOnlyList<ZonenluftstromDaten>? luftstroeme = null)
    {
        MitZonenweg = mitZonenweg;
        Zonen = (zonen ?? Array.Empty<ZoneDaten>()).Select(z => z.Kopie()).ToList();
        Luftstroeme = (luftstroeme ?? Array.Empty<ZonenluftstromDaten>()).Select(l => l.Kopie()).ToList();
        _kleinsteId = Math.Min(-1, Zonen.Count == 0 ? -1 : Zonen.Min(z => z.Id));
        ZonenGeschrieben();
    }

    /// <summary>Nach einem gelungenen Schreiben der Zonen: der neue Vergleichsstand (samt Luftströmen).</summary>
    public void ZonenGeschrieben()
    {
        _zonenGeschrieben = Zonen.Select(z => z.Kopie()).ToList();
        _luftGeschrieben = Luftstroeme.Select(l => l.Kopie()).ToList();
    }

    /// <summary>
    /// Sind die Zonen seit dem Laden bzw. dem letzten Schreiben geändert (auch umgeordnet) — oder die
    /// Luftströme zwischen ihnen (Stufe G6b)?
    /// </summary>
    public bool ZonenGeaendert
        => Zonen.Count != _zonenGeschrieben.Count
           || Zonen.Where((z, i) => !z.GleicheWerte(_zonenGeschrieben[i])).Any()
           || LuftGeaendert;

    /// <summary>Sind die Luftströme seit dem Laden bzw. dem letzten Schreiben geändert (Stufe G6b)?</summary>
    public bool LuftGeaendert
        => Luftstroeme.Count != _luftGeschrieben.Count
           || Luftstroeme.Where((l, i) => !l.GleicheWerte(_luftGeschrieben[i])).Any();

    /// <summary>Ersetzt die Luftströme — der Rückweg des Dialogs „Luftaustausch" (Stufe G6b).</summary>
    public void LuftstroemeSetzen(IEnumerable<ZonenluftstromDaten> luftstroeme)
        => Luftstroeme = (luftstroeme ?? Enumerable.Empty<ZonenluftstromDaten>()).Select(l => l.Kopie()).ToList();

    /// <summary>
    /// Der Stand, den der OK-Weg prüft und schreibt (Stufe G6b): die Zonen, die Luftströme — beim
    /// Schreiben nur, wenn sie geändert sind (<paramref name="nurGeaenderteLuft"/>; sonst <c>null</c> =
    /// ungeändert) — und der Tagessollwert des Gebäudes.
    /// </summary>
    public ZonenstandDaten Zonenstand(bool nurGeaenderteLuft)
        => new(Zonen, nurGeaenderteLuft && !LuftGeaendert ? null : Luftstroeme, Stand.SollTag);

    /// <summary>Die Zone mit dieser Id; <c>null</c> = keine.</summary>
    public ZoneDaten? ZoneMitId(int id) => Zonen.FirstOrDefault(z => z.Id == id);

    /// <summary>Eine neue vorläufige Id — unter allen bisher vergebenen.</summary>
    private int NeueId() => --_kleinsteId;

    /// <summary>
    /// Eine neue, leere Zone mit einer neuen vorläufigen Id — noch NICHT im Arbeitsstand; sie kommt
    /// erst mit <see cref="ZoneAnlegen"/> hinein (etwa nach dem OK des Zonendialogs).
    /// </summary>
    public ZoneDaten NeueZone(string bezeichner) => new() { Id = NeueId(), Bezeichner = bezeichner ?? "" };

    /// <summary>
    /// Hängt eine Zone an die Liste — die Übernahme (Id −1) oder eine neue Zone. Trägt sie keine
    /// vorläufige Id oder eine, die in der Liste schon steht, bekommt sie eine neue.
    /// </summary>
    public ZoneDaten ZoneAnlegen(ZoneDaten zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (zone.Id >= 0 || ZoneMitId(zone.Id) is not null) zone.Id = NeueId();
        else _kleinsteId = Math.Min(_kleinsteId, zone.Id);
        Zonen.Add(zone);
        return zone;
    }

    /// <summary>Ersetzt die Zone gleicher Id an ihrem Platz (Rückweg des Zonendialogs); <c>false</c> = keine solche Zone.</summary>
    public bool ZoneErsetzen(ZoneDaten zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        int i = Zonen.FindIndex(z => z.Id == zone.Id);
        if (i < 0) return false;
        Zonen[i] = zone;
        return true;
    }

    /// <summary>
    /// Nimmt die Zone mit dieser Id samt Bauteilen aus dem Arbeitsstand; <c>false</c> = keine solche Zone.
    /// <b>Die Bezüge fallen mit</b> (Festlegung 8 des Auftrags G6b): Trennflächen ANDERER Zonen, die auf
    /// sie zeigen, grenzen danach an einen unbeheizten Raum (<c>UNBEHEIZT</c>, ohne Nachbar und
    /// Zuordnung) — sichtbar im Arbeitsstand —, und die Luftströme mit ihr entfallen.
    /// </summary>
    public bool ZoneEntfernen(int id)
    {
        if (Zonen.RemoveAll(z => z.Id == id) == 0) return false;
        foreach (BauteilDaten b in Zonen.SelectMany(z => z.Bauteile).Where(b => b.IdNachbarzone == id))
        {
            b.Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT;
            b.IdNachbarzone = null;
            b.TrennflaecheZuordnung = null;
        }
        Luftstroeme.RemoveAll(l => l.IdZoneA == id || l.IdZoneB == id);
        return true;
    }

    /// <summary>
    /// Was auf die Zone <paramref name="id"/> zeigt (Festlegung 8): die Trennflächen ANDERER Zonen mit
    /// ihr als Nachbar und die Luftströme mit ihr — die Rückfrage vor dem Entfernen nennt beide.
    /// </summary>
    public (IReadOnlyList<(ZoneDaten Zone, BauteilDaten Bauteil)> Trennflaechen, IReadOnlyList<ZonenluftstromDaten> Luftstroeme) Bezuege(int id)
        => (Zonen.Where(z => z.Id != id)
                 .SelectMany(z => z.Bauteile.Where(b => b.IdNachbarzone == id).Select(b => (z, b)))
                 .ToList(),
            Luftstroeme.Where(l => l.IdZoneA == id || l.IdZoneB == id).ToList());

    /// <summary>Die Trennflächen, die die Zone <paramref name="id"/> selbst führt (Randbedingung Nachbarzone) — Festlegung 9.</summary>
    public IReadOnlyList<BauteilDaten> EigeneTrennflaechen(int id)
        => ZoneMitId(id)?.Bauteile.Where(b => b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE).ToList()
           ?? (IReadOnlyList<BauteilDaten>)Array.Empty<BauteilDaten>();

    /// <summary>Der Name der Zone mit dieser Id; „#Id", wenn es sie nicht gibt.</summary>
    public string Zonenname(int? id)
        => id is int i ? ZoneMitId(i)?.Bezeichner ?? "#" + i.ToString(CultureInfo.InvariantCulture) : "—";

    /// <summary>
    /// Die übrigen Zonen zur Wahl als Nachbarzone einer Trennfläche der Zone <paramref name="idZone"/>
    /// (Stufe G6b) — in Listenfolge, auch mit vorläufigen Ids; die Zone selbst nicht.
    /// </summary>
    public IReadOnlyList<NachbarzoneWahl> Nachbarzonen(int idZone)
        => Zonen.Where(z => z.Id != idZone).Select(z => new NachbarzoneWahl(z.Id, z.Bezeichner)).ToList();

    /// <summary>Alle Zonen zur Wahl (Id, Name) in Listenfolge — die Paare des Luftaustauschs (Stufe G6b).</summary>
    public IReadOnlyList<NachbarzoneWahl> Zonenwahl
        => Zonen.Select(z => new NachbarzoneWahl(z.Id, z.Bezeichner)).ToList();

    /// <summary>
    /// Die Trennflächen, die eine ANDERE Zone mit der Zone <paramref name="idZone"/> führt (Stufe G6b) —
    /// die Gegenseiten, die ihr Zonendialog gespiegelt und nur zum Lesen zeigt.
    /// </summary>
    public IReadOnlyList<GegenseiteDaten> Gegenseiten(int idZone)
        => Zonen.Where(z => z.Id != idZone)
                .SelectMany(z => z.Bauteile.Where(b => b.IdNachbarzone == idZone
                                                       && b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE)
                                           .Select(b => new GegenseiteDaten(z.Id, z.Bezeichner, b)))
                .ToList();

    /// <summary>
    /// Dupliziert die Zone mit dieser Id — die Kopie steht direkt hinter ihr. Zone und Bauteile
    /// bekommen neue vorläufige Ids, die Bauteile die Herkunft „manuell" und keine Quellkennung: Die
    /// Importpaarung gehört der Vorlage. <see cref="ZoneDaten.VorlageId"/> nennt die Vorlage, deren
    /// übrige Spalten die Hülle übernimmt (bei einem Duplikat eines Duplikats dessen Vorlage).
    /// </summary>
    /// <returns>Die Kopie; <c>null</c> = keine solche Zone.</returns>
    public ZoneDaten? ZoneDuplizieren(int id, string bezeichner)
    {
        int i = Zonen.FindIndex(z => z.Id == id);
        if (i < 0) return null;
        ZoneDaten quelle = Zonen[i];
        ZoneDaten kopie = quelle.Kopie();
        kopie.Id = NeueId();
        kopie.VorlageId = quelle.VorlageId ?? quelle.Id;
        kopie.Bezeichner = bezeichner ?? "";
        int bauteilId = 0;
        foreach (BauteilDaten b in kopie.Bauteile)
        {
            b.Id = --bauteilId;
            b.Herkunft = DbWerte.HERKUNFT_MANUELL;
            b.Quellkennung = null;
        }
        Zonen.Insert(i + 1, kopie);
        return kopie;
    }

    /// <summary>
    /// Verschiebt die Zone mit dieser Id um einen Platz (<paramref name="richtung"/> −1 nach oben,
    /// +1 nach unten); der Rang folgt beim Schreiben lückenlos der Listenfolge.
    /// </summary>
    /// <returns><c>false</c> = keine solche Zone, oder sie steht schon am Rand.</returns>
    public bool ZoneVerschieben(int id, int richtung)
    {
        int i = Zonen.FindIndex(z => z.Id == id);
        int j = i + Math.Sign(richtung);
        if (i < 0 || richtung == 0 || j < 0 || j >= Zonen.Count) return false;
        (Zonen[i], Zonen[j]) = (Zonen[j], Zonen[i]);
        return true;
    }

    /// <summary>
    /// Die Herleitungszeile „Rechenweg der Hülle" — in BEIDEN Stellungen (Softwarearchitektur 3.2
    /// Regel 3): Klassenweg über die U-Wert-Gruppen und die Bauweise, oder Bauteilweg mit der Zone
    /// (bzw. der Zahl der Zonen) und der Zahl der Bauteile; für einen Katalogsatz der Klassenweg samt
    /// dem Satz, dass er keine Zonen trägt.
    /// </summary>
    public string Huellwegzeile(GebaeudeZonenTexte t)
    {
        if (!MitZonenweg) return t.ZeileKatalog;
        if (Zonen.Count == 0) return t.ZeileKlassenweg;
        CultureInfo c = CultureInfo.CurrentCulture;
        string bauteile = Zonen.Sum(z => z.Bauteile.Count).ToString(c);
        return Zonen.Count == 1
            ? string.Format(c, t.ZeileBauteilweg, Zonen[0].Bezeichner, bauteile)
            : string.Format(c, t.ZeileBauteilwegZonen, Zonen.Count.ToString(c), bauteile);
    }

    /// <summary>Der Luftwechsel, mit dem H_ve gebildet wird [1/h] — der des gewählten Rechenwegs.</summary>
    private double? LuftwechselRechenweg => IstVdi6007 ? WirksamerLuftwechsel : Stand.Luftwechselrate;

    /// <summary>
    /// Die Kennwerte je Zone in Listenfolge (<see cref="WindowsFormsApplication1.Zonenkennwerte"/>, die
    /// EINE Formel des Kerns) mit Nutzfläche, Raumhöhe und Luftwechsel des Arbeitsstands.
    /// </summary>
    public IReadOnlyList<Zonenkennwerte> Kennwerte
        => Zonen.Select(z => Zonensummen.Kennwerte(z, Stand.WohnflaecheGesamt, Stand.Raumhoehe, LuftwechselRechenweg)).ToList();

    /// <summary>
    /// <b>Die Werte des Gebäudes, aus denen eine Zone erbt</b> (<see cref="Gebaeudevorgaben"/>, Stufe G6b)
    /// — aus dem Arbeitsstand, mit denselben Ableitungen wie beim Schreiben (leere Felder als 0, Bewohner
    /// über <see cref="Gebaeudevorgaben.BewohnerAusFlaeche"/>). Die Anzeige „Vorgabe: …" des
    /// Zonendialogs bildet daraus mit <see cref="Zonenvorgaben"/> dieselben Werte wie der Lauf.
    /// </summary>
    public Gebaeudevorgaben Vorgaben
    {
        get
        {
            double flaeche = Stand.WohnflaecheGesamt ?? 0;
            return new Gebaeudevorgaben(flaeche, Stand.Raumhoehe ?? 0,
                Stand.SollTag ?? 0, Stand.NachtAbsenkung ?? 0, Stand.WochenendAbsenkung ?? 0, Stand.SollFerien ?? 0,
                Stand.MaxTemperatur ?? 0, Stand.HeizungStrahlungsanteil, Stand.HeizleistungMax,
                Stand.Luftwechselrate ?? 0, Stand.LuftwechselInfiltration, Stand.LuftwechselNutzer,
                Stand.Waermegewinne ?? 0, Gebaeudevorgaben.BewohnerAusFlaeche(flaeche, Stand.FlaecheNutzer ?? 0),
                Stand.KuehlungAktiv, Stand.KuehlSollwert, Stand.KuehlSollwertNacht, Stand.KuehlleistungMax);
        }
    }

    /// <summary>Die Kennwerte der Zone mit dieser Id; <c>null</c> = keine solche Zone.</summary>
    public Zonenkennwerte? KennwerteVon(int id)
        => ZoneMitId(id) is ZoneDaten z ? Zonensummen.Kennwerte(z, Stand.WohnflaecheGesamt, Stand.Raumhoehe, LuftwechselRechenweg) : null;

    /// <summary>
    /// Die Nutzfläche, mit der das Gebäude rechnet [m²]: mit Zonen Σ ihrer Flächen (leer = die des
    /// Gebäudes), sonst die des Gebäudes; <c>null</c>, wenn eine Fläche fehlt.
    /// </summary>
    public double? NutzflaecheWirksam
    {
        get
        {
            if (Zonen.Count == 0) return Stand.WohnflaecheGesamt;
            double summe = 0.0;
            foreach (Zonenkennwerte k in Kennwerte)
            {
                if (k.Nutzflaeche is not double a) return null;
                summe += a;
            }
            return summe;
        }
    }

    /// <summary>H_T zur Anzeige [W/K]: mit Zonen Σ aus ihren Bauteilen (Summenregel), sonst aus den U-Wert-Gruppen.</summary>
    public double HTAnzeige => Zonen.Count == 0 ? HT : Summe(Kennwerte.Select(k => k.HT));

    /// <summary>H_ve zur Anzeige [W/K]: mit Zonen Σ über ihre Nutzflächen (Flächenschlüssel), sonst wie <see cref="HVe"/>.</summary>
    public double HVeAnzeige => Zonen.Count == 0 ? HVe : Summe(Kennwerte.Select(k => k.HVe));

    /// <summary>Σ in Listenfolge; eine einzige Zone gibt ihren Wert unverändert zurück.</summary>
    private static double Summe(IEnumerable<double> werte)
    {
        double s = 0.0;
        bool erster = true;
        foreach (double w in werte)
        {
            s = erster ? w : s + w;
            erster = false;
        }
        return s;
    }

    /// <summary>
    /// Die Hinweise über die Zonenliste (<c>GebaeudeZonenCtrl.Hinweise</c>, Stufe G6a) — Σ Nutzfläche
    /// gegen die des Gebäudes, Zonen ohne Außenbauteil; leer = nichts zu sagen. Sie halten kein OK an.
    /// </summary>
    public IReadOnlyList<string> ZonenHinweise
        => GebaeudeZonenCtrl.Hinweise(Zonen.Select(z => new GebaeudeZonenCtrl.Zonenangabe(z.Bezeichner, z.Nutzflaeche,
               z.Bauteile.Select(b => (b.Bauteilart, b.Randbedingung!)).ToList())), Stand.WohnflaecheGesamt);

    private void FerienZerlegen()
    {
        for (int n = 0; n < 4; n++)
        {
            (BeginnTag[n], BeginnMonat[n]) = Ferienzeit.TagUndMonat(Stand.Ferienbeginn[n]);
            (EndeTag[n], EndeMonat[n]) = Ferienzeit.TagUndMonat(Stand.Ferienende[n]);
        }
    }

    // =====================================================================
    //  Die Wege der Bedienelemente — für Hand, Stammblatt und Assistent dieselben
    // =====================================================================

    /// <summary>Wählt einen Eintrag einer Namensliste (Gebäudetyp, Gebäudeart); <c>null</c> leert.</summary>
    public static string Waehlen(IReadOnlyList<string> quelle, int? index, string bisher)
    {
        if (index is null) return "";
        return index.Value >= 0 && index.Value < quelle.Count ? quelle[index.Value] : bisher;
    }

    /// <summary>Die Bauartwahl — und mit ihr die BAUWEISE (Entscheid W9‑O‑2).</summary>
    public void BauartWaehlen(int? index)
    {
        Stand.Bauart = index ?? 1;
        BauweiseNachfuehren = true;
        BauweiseBilden();
    }

    /// <summary>Die Nutzfläche — die Bauweise folgt ihr beim Schreiben.</summary>
    public void NutzflaecheSetzen(double? wert)
    {
        Stand.WohnflaecheGesamt = wert;
        BauweiseNachfuehren = true;
    }

    /// <summary><c>Bauweise = Nutzfläche × 20 / 50 / 100</c> aus der BAUART-Auswahl.</summary>
    private void BauweiseBilden()
        => Stand.Bauweise = Gebaeudebauweise.BauweiseAusBauart(Stand.Bauart, Stand.WohnflaecheGesamt ?? 0);

    /// <summary>Die Verwendung über ihren Listenplatz; der Wert ist der STEUERWERT.</summary>
    public void VerwendungWaehlen(IReadOnlyList<string> steuerwerte, int? index)
    {
        if (index is null || index.Value < 0 || index.Value >= steuerwerte.Count) return;
        Stand.Verwendung = steuerwerte[index.Value];
    }

    /// <summary>Der Listenplatz der Verwendung unter den Steuerwerten; <c>null</c> = keiner.</summary>
    public int? Verwendungsindex(IReadOnlyList<string> steuerwerte)
    {
        for (int i = 0; i < steuerwerte.Count; i++)
            if (string.Equals(steuerwerte[i], Stand.Verwendung, StringComparison.Ordinal)) return i;
        return null;
    }

    // ---- Baualtersklasse und Energiestandard (Entscheid E47) --------------------------------

    /// <summary>
    /// Die Klasse, die die Klappliste ZEIGT: die aus dem Baujahr, sonst die gewählte (DAS BAUJAHR FÜHRT,
    /// F2). Gespeichert wird dieselbe (<c>Gebaeudeklassen.IndexWirksam</c> in der Hülle).
    /// </summary>
    public int KlasseWirksam => Gebaeudeklassen.IndexWirksam(Stand.Baujahr, Stand.Baualtersklasse);

    /// <summary>Folgt die Klasse aus dem Baujahr? Dann ist die Klappliste gesperrt.</summary>
    public bool KlasseAusBaujahr => Stand.KlasseAusBaujahr;

    /// <summary>Die Klassenwahl — nur ohne Baujahr wirksam; mit Baujahr bleibt die Klasse aus dem Jahr.</summary>
    public void KlasseWaehlen(int? index)
    {
        if (KlasseAusBaujahr) return;
        Stand.Baualtersklasse = index ?? 0;
    }

    /// <summary>Das Baujahr — die Klasse folgt ihm, wenn es eine ergibt.</summary>
    public void BaujahrSetzen(int? jahr) => Stand.BaujahrUebernehmen(jahr);

    /// <summary>
    /// Die Herleitungszeile unter der Klappliste der Klasse: mit Baujahr „Die Klasse folgt aus dem
    /// Baujahr …", dann die Quelle der Einteilung (IWU 2015, Stein/Loga 2025).
    /// </summary>
    public string KlassenHerleitung
        => KlasseAusBaujahr && Stand.Baujahr is int jahr
            ? Gebaeudeklassen.AusBaujahrText(jahr) + " " + Gebaeudeklassen.Quelle()
            : Gebaeudeklassen.Quelle();

    /// <summary>
    /// Die Einträge der Klappliste Energiestandard (Id = Platz in <c>Energiestandard.CODES</c>): die
    /// Standards, die zur Verwendung passen (F3) — und der gespeicherte, auch wenn er nicht passt, damit
    /// er sichtbar bleibt; die Prüfung meldet ihn beim Speichern. „Keiner" ist der Platzhalter.
    /// </summary>
    public IReadOnlyList<(int Id, string Text)> Energiestandardeintraege()
    {
        var liste = new List<(int Id, string Text)>();
        for (int i = 0; i < Energiestandard.CODES.Count; i++)
        {
            string code = Energiestandard.CODES[i];
            if (Energiestandard.PasstZu(code, Stand.Verwendung) ||
                string.Equals(code, Stand.Energiestandard, StringComparison.Ordinal))
                liste.Add((i, Energiestandard.Text(code)));
        }
        return liste;
    }

    /// <summary>Der Platz des gespeicherten Energiestandards in <c>Energiestandard.CODES</c>; <c>null</c> = keiner.</summary>
    public int? EnergiestandardIndex
        => Energiestandard.Index(Stand.Energiestandard) is int i && i >= 0 ? i : null;

    /// <summary>Die Wahl des Energiestandards über seinen Platz; der Platzhalter (<c>null</c>) heißt „keiner".</summary>
    public void EnergiestandardWaehlen(int? index) => Stand.Energiestandard = Energiestandard.Code(index);

    /// <summary>
    /// Der Schalter „Rechenweg" (0 = VDI 6007, 1 = Tagesbilanz). Kehrt die Wahl zum Weg
    /// zurück, den der geladene Spaltenwert ohnehin nimmt, bleibt der geladene Wert — NULL
    /// bleibt NULL.
    /// </summary>
    public void RechenwegWaehlen(int? index)
    {
        if (index is null) return;
        string gewaehlt = index.Value == 0 ? DbWerte.GEBAEUDE_MODELL_VDI6007 : DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        Stand.Modell = gewaehlt == Gebaeuderechenweg.Wirksam(ModellBeimLaden) ? ModellBeimLaden : gewaehlt;
    }

    /// <summary>Die Randbedingung der Bodenplatte (0 Erdreich, 1 Keller, 2 Außenluft); Erdreich bleibt NULL, wenn es NULL war.</summary>
    public void RandbedingungWaehlen(int? index)
    {
        string gewaehlt = index switch
        {
            1 => DbWerte.GRUND_KELLER,
            2 => DbWerte.GRUND_AUSSENLUFT,
            _ => DbWerte.GRUND_ERDREICH
        };
        string geladen = RandBeimLaden ?? DbWerte.GRUND_ERDREICH;
        Stand.GrundflaecheRandbedingung = gewaehlt == geladen ? RandBeimLaden : gewaehlt;
    }

    /// <summary>Der Listenplatz der Randbedingung: 0 Erdreich, 1 Keller, 2 Außenluft.</summary>
    public int Randindex => Stand.GrundflaecheRandbedingung switch
    {
        DbWerte.GRUND_KELLER => 1,
        DbWerte.GRUND_AUSSENLUFT => 2,
        _ => 0
    };

    /// <summary>Der Kennwert (U bzw. ψ) einer Zeile des Hüll-Rasters.</summary>
    public void KennwertSetzen(Huellbauteil bauteil, double? wert)
    {
        switch (bauteil)
        {
            case Huellbauteil.Aussenwand: Stand.UWertAussenwand = wert; break;
            case Huellbauteil.Fenster: Stand.UWertFenster = wert; break;
            case Huellbauteil.Dach: Stand.UWertDachflaeche = wert; break;
            case Huellbauteil.Bodenplatte: Stand.UWertGrundflaeche = wert; break;
            case Huellbauteil.Sonstiges: Stand.UWertSonstiges = wert; break;
            case Huellbauteil.WaermebrueckeFensterWand: Stand.WbvkFensterWand = wert; break;
            case Huellbauteil.WaermebrueckeAussenwandKeller: Stand.WbvkAussenwandKeller = wert; break;
            case Huellbauteil.WaermebrueckeWandDach: Stand.WbvkWandDach = wert; break;
        }
    }

    /// <summary>Die Größe (A bzw. L) einer Zeile; die Fensterfläche ist gerechnet und hat keine.</summary>
    public void GroesseSetzen(Huellbauteil bauteil, double? wert)
    {
        switch (bauteil)
        {
            case Huellbauteil.Aussenwand: Stand.FlaecheAussenwand = wert; break;
            case Huellbauteil.Dach: Stand.Dachflaeche = wert; break;
            case Huellbauteil.Bodenplatte: Stand.Grundflaeche = wert; break;
            case Huellbauteil.Sonstiges: Stand.SonstigeFlaechen = wert; break;
            case Huellbauteil.WaermebrueckeFensterWand: Stand.AnschlussFensterWand = wert; break;
            case Huellbauteil.WaermebrueckeAussenwandKeller: Stand.AnschlussAussenwandKeller = wert; break;
            case Huellbauteil.WaermebrueckeWandDach: Stand.AnschlussWandDach = wert; break;
        }
    }

    /// <summary>
    /// Der Haken „Gebäude wird gekühlt". Ohne Haken stehen Kühlsollwert, Grenze und die
    /// Kühlübergabe nicht da — ihre Werte bleiben im Stand, eine Fehleingabe in einem
    /// ausgeblendeten Feld hält den Speicherweg aber nicht mehr an.
    /// </summary>
    public void KuehlungSetzen(bool wert, GebaeudeHuelleTexte texte)
    {
        Stand.KuehlungAktiv = wert;
        if (wert) return;
        Fehlerfelder.Remove(Feld(texte.LabelKuehlSollwert));
        Fehlerfelder.Remove(Feld(texte.LabelKuehlleistungMax));
        KuehlFehlerfelderLeeren();
    }

    /// <summary>Ein Eingabefeld meldet, ob sein Text eine gültige Zahl ist.</summary>
    public void FehlerMelden((string Feld, bool Fehlerhaft) e)
    {
        if (e.Fehlerhaft) Fehlerfelder.Add(e.Feld); else Fehlerfelder.Remove(e.Feld);
    }

    // =====================================================================
    //  Die Wärmeübergabe (Stufe AK1; Anlagenkopplung 8.1, 9.1, 9.2; H1, H10, H12, E25)
    // =====================================================================

    /// <summary>Die Übergabeart beim Laden — „ideal" bleibt NULL, wenn es NULL war.</summary>
    public string? ArtBeimLaden { get; private set; }

    /// <summary>Das Proportionalband beim Laden — die Vorgabe bleibt NULL, wenn es NULL war.</summary>
    public double? BandBeimLaden { get; private set; }

    /// <summary>Hat der Anwender in der Schnellwahl „frei" gewählt? Dann steht das freie Feld.</summary>
    public bool BandFrei { get; private set; }

    /// <summary>Die 168 Werte des gespeicherten Zeitprogramms; <c>null</c> ohne gültiges Profil.</summary>
    public double[]? ProfilWerte { get; private set; }

    /// <summary>Was der strenge Leser im gespeicherten Zeitprogramm gefunden hat (H-F10).</summary>
    public AnlagenkopplungSchema.WochenprofilBefund ProfilBefund { get; private set; }

    private int _profilGefunden, _profilStelle;

    /// <summary>Die Fehlerfelder der Gruppe — sie fallen, sobald die Gruppe ihre Felder nicht mehr zeigt.</summary>
    private readonly HashSet<string> _uebergabeFehlerfelder = new();

    /// <summary>Rechnet die gewählte Art eine Übergabe (Radiator, Flächenheizung, Konvektor)?</summary>
    public bool ArtRechnet => Waermeuebergabevorgaben.ArtRechnet(Stand.UebergabeArt);

    /// <summary>Stehen die Felder der Übergabe da? Haken gesetzt UND eine rechnende Art.</summary>
    public bool UebergabeAktiv => Stand.HeizkreisAktiv && ArtRechnet;

    /// <summary>Ein Feld der Gruppe meldet seinen Fehlerzustand — gemerkt für das Ausblenden.</summary>
    public void UebergabeFehlerMelden((string Feld, bool Fehlerhaft) e)
    {
        FehlerMelden(e);
        if (e.Fehlerhaft) _uebergabeFehlerfelder.Add(e.Feld); else _uebergabeFehlerfelder.Remove(e.Feld);
    }

    private void UebergabeFehlerfelderLeeren()
    {
        foreach (string f in _uebergabeFehlerfelder) Fehlerfelder.Remove(f);
        _uebergabeFehlerfelder.Clear();
    }

    /// <summary>
    /// Hat <see cref="HeizkreisSetzen"/> die Heizkurve VORGESCHLAGEN, ohne dass der Anwender den
    /// Haken „Heizkurve fahren" seither angefasst hat? Dann nimmt das Ausschalten des Heizkreises
    /// den Vorschlag zurück.
    /// </summary>
    private bool _heizkurveVorgeschlagen;

    /// <summary>
    /// Der Haken „Übergabe rechnen". Beim ersten Einschalten (noch keine rechnende Art) schlägt
    /// der Dialog die Heizkurve vor (8.1: „die Heizkurve schlägt erst der Dialog vor, sobald jemand
    /// den Heizkreis einschaltet"). Ohne Haken bleiben alle Werte im Stand, die der ANWENDER
    /// gesetzt hat; eine Fehleingabe in einem ausgeblendeten Feld hält den Speicherweg nicht mehr
    /// an.
    ///
    /// <para><b>Ein bloßer Vorschlag fällt mit dem Haken.</b> Ein- und wieder Ausschalten ist keine
    /// Änderung: Hat der Anwender die vorgeschlagene Heizkurve nicht angefasst, steht sie nach dem
    /// Ausschalten wieder so, wie sie geladen wurde — sonst schriebe ein OK
    /// <c>Heizkurve_Aktiv = 1</c> in einen Satz ohne Heizkreis, ohne dass der Anwender es sah
    /// (Befund 25.09.2026 an einer Projektkopie).</para>
    /// </summary>
    public void HeizkreisSetzen(bool wert)
    {
        Stand.HeizkreisAktiv = wert;
        if (wert && !ArtRechnet && !Stand.HeizkurveAktiv)
        {
            Stand.HeizkurveAktiv = true;
            _heizkurveVorgeschlagen = true;
        }
        if (!wert)
        {
            if (_heizkurveVorgeschlagen) Stand.HeizkurveAktiv = false;
            _heizkurveVorgeschlagen = false;
            UebergabeFehlerfelderLeeren();
        }
    }

    /// <summary>
    /// Der Haken „Heizkurve fahren" — die Wahl des Anwenders; ein Vorschlag aus
    /// <see cref="HeizkreisSetzen"/> gilt danach als seine Wahl und bleibt beim Ausschalten stehen.
    /// </summary>
    public void HeizkurveSetzen(bool wert)
    {
        Stand.HeizkurveAktiv = wert;
        _heizkurveVorgeschlagen = false;
    }

    /// <summary>
    /// Die Übergabeart über ihren Listenplatz in <see cref="Waermeuebergabevorgaben.Arten"/>
    /// (0 = ideal). „ideal" und NULL sind dasselbe: War beim Laden NULL, bleibt es NULL.
    /// </summary>
    public void ArtWaehlen(int? index)
    {
        if (index is null || index.Value < 0 || index.Value >= Waermeuebergabevorgaben.Arten.Count) return;
        string gewaehlt = Waermeuebergabevorgaben.Arten[index.Value];
        if (gewaehlt == DbWerte.UEBERGABE_IDEAL)
        {
            bool geladenIdeal = ArtBeimLaden is null || ArtBeimLaden == DbWerte.UEBERGABE_IDEAL;
            Stand.UebergabeArt = geladenIdeal ? ArtBeimLaden : null;
            UebergabeFehlerfelderLeeren();
            return;
        }
        Stand.UebergabeArt = gewaehlt;
    }

    /// <summary>Der Listenplatz der Übergabeart; NULL = ideal (0); eine unbekannte Art = −1.</summary>
    public int Artindex
    {
        get
        {
            if (string.IsNullOrEmpty(Stand.UebergabeArt)) return 0;
            for (int i = 0; i < Waermeuebergabevorgaben.Arten.Count; i++)
                if (Waermeuebergabevorgaben.Arten[i] == Stand.UebergabeArt) return i;
            return -1;
        }
    }

    /// <summary>
    /// Der Platz der Schnellwahl des Proportionalbands (E25): 0 = 0,5 K, 1 = 1 K, 2 = 2 K, 3 =
    /// frei. Leer gilt die Vorgabe (1 K); ein Wert ohne Schnellwahl zeigt „frei".
    /// </summary>
    public int BandIndex
    {
        get
        {
            if (BandFrei) return 3;
            double wert = Stand.ReglerProportionalband ?? Waermeuebergabevorgaben.Proportionalband;
            for (int i = 0; i < Waermeuebergabevorgaben.ProportionalbandSchnellwahl.Count; i++)
                if (Waermeuebergabevorgaben.ProportionalbandSchnellwahl[i] == wert) return i;
            return 3;
        }
    }

    /// <summary>
    /// Die Schnellwahl: 0,5 / 1 / 2 K setzen den Wert — die Vorgabe (1 K) bleibt NULL, wenn es
    /// NULL war —, „frei" zeigt das freie Feld mit dem bisherigen Wert.
    /// </summary>
    public void BandWaehlen(int? index)
    {
        if (index is null) return;
        IReadOnlyList<double> schnell = Waermeuebergabevorgaben.ProportionalbandSchnellwahl;
        if (index.Value < 0 || index.Value >= schnell.Count)
        {
            BandFrei = true;
            return;
        }
        BandFrei = false;
        double wert = schnell[index.Value];
        Stand.ReglerProportionalband = wert == Waermeuebergabevorgaben.Proportionalband && BandBeimLaden is null
            ? null : wert;
    }

    /// <summary>Der freie Wert des Proportionalbands [K]; leer = Vorgabe.</summary>
    public void BandSetzen(double? wert) => Stand.ReglerProportionalband = wert;

    /// <summary>Liest das gespeicherte Zeitprogramm streng (H-F10) — Werte oder ein benannter Befund.</summary>
    private void ProfilLesen()
    {
        AnlagenkopplungSchema.Wochenprofil p = AnlagenkopplungSchema.WochenprofilLesen(Stand.Sollwertprofil);
        ProfilBefund = p.Befund;
        ProfilWerte = p.Befund == AnlagenkopplungSchema.WochenprofilBefund.Gelesen ? p.Werte : null;
        _profilGefunden = p.Gefunden;
        _profilStelle = p.Stelle;
    }

    /// <summary>
    /// Das Zeitprogramm aus dem Wochenraster: 168 Werte werden im Format des Kerns geschrieben
    /// (<see cref="AnlagenkopplungSchema.WochenprofilSchreiben"/>, zwei Nachkommastellen);
    /// <c>null</c> verwirft es — dann gelten wieder die Bestandssollwerte.
    /// </summary>
    public void ProfilSetzen(double[]? werte)
    {
        Stand.Sollwertprofil = werte is null ? null : AnlagenkopplungSchema.WochenprofilSchreiben(werte);
        ProfilLesen();
    }

    /// <summary>Die Bestandssollwerte als Woche — was ohne Zeitprogramm gilt (Kern-Regel), mit der Nachtzeit des Satzes (E43).</summary>
    public double[] Bestandswoche
        => Waermeuebergabevorgaben.Bestandswoche(Stand.SollTag ?? 0, Stand.NachtAbsenkung ?? 0,
                                                 Stand.WochenendAbsenkung ?? 0, Stand.NachtBeginn, Stand.NachtEnde);

    /// <summary>Die Auslegungs-Raumtemperatur, die gilt: das Feld, sonst das Soll am Tag.</summary>
    public double AuslegungRaumWirksam => Stand.AuslegungRaumtemperatur ?? Stand.SollTag ?? 0;

    /// <summary>
    /// Der Stand, wie der Schreibweg ihn ablegte — eine Kopie mit der nachgeführten Bauweise und
    /// der Summe Ost + West. Aus ihm leitet die Hülle Nennleistung und Außentemperatur her.
    /// </summary>
    public GebaeudeKatalogDaten Probestand()
    {
        GebaeudeKatalogDaten k = Stand.Kopie();
        if (BauweiseNachfuehren)
            k.Bauweise = Gebaeudebauweise.BauweiseAusBauart(Stand.Bauart, Stand.WohnflaecheGesamt ?? 0);
        k.FensterflaecheOstWest = SummeOstWest ?? 0;
        return k;
    }

    /// <summary>
    /// Der Abdruck des Stands für die Herleitung — ändert er sich, ist die hergeleitete Zahl
    /// neu zu bilden. Er umfasst jede Eigenschaft des Feldsatzes samt der Ferienfelder.
    /// </summary>
    public string Abdruck()
    {
        var sb = new System.Text.StringBuilder(1024);
        foreach (System.Reflection.PropertyInfo p in AbdruckEigenschaften)
        {
            object? w = p.GetValue(Stand);
            if (w is int[] feld) sb.Append(string.Join(",", feld));
            else if (w is IFormattable f) sb.Append(f.ToString(null, CultureInfo.InvariantCulture));
            else sb.Append(w);
            sb.Append('|');
        }
        sb.Append(BauweiseNachfuehren ? '1' : '0');
        return sb.ToString();
    }

    private static readonly System.Reflection.PropertyInfo[] AbdruckEigenschaften =
        typeof(GebaeudeKatalogDaten).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

    // =====================================================================
    //  Die Kühlübergabe (E37; Anlagenkopplung 7.2, 8.1, 9.1; A1, A2, A4)
    // =====================================================================
    //
    // Der Unterabschnitt „Kühlübergabe" der Gruppe „Kühlung": sichtbar nur mit Kühlung, der
    // Schalter kennt kein NULL (A1), „ideal" hält NULL wie auf der Heizseite, und jede Vorgabe
    // kommt als Zahl aus dem Kern (Waermeuebergabevorgaben.Kuehl*) — mit DENSELBEN Grenzen, an
    // denen der Eingangsbauer hart abbricht.

    /// <summary>Die Kühlübergabeart beim Laden — „ideal" bleibt NULL, wenn es NULL war.</summary>
    public string? KuehlArtBeimLaden { get; private set; }

    /// <summary>Die Fehlerfelder des Unterabschnitts — sie fallen, sobald er seine Felder nicht mehr zeigt.</summary>
    private readonly HashSet<string> _kuehlFehlerfelder = new();

    /// <summary>Rechnet die gewählte Kühlübergabeart (Kühldecke, Flächenkühlung, Gebläsekonvektor)?</summary>
    public bool KuehlArtRechnet => Waermeuebergabevorgaben.KuehlArtRechnet(Stand.KuehlUebergabeArt);

    /// <summary>Steht der Unterabschnitt da? Nur mit dem Haken „Gebäude wird gekühlt".</summary>
    public bool KuehluebergabeSichtbar => Stand.KuehlungAktiv;

    /// <summary>Stehen die Felder der Kühlübergabe da? Kühlung, Haken „Kühlübergabe rechnen" UND eine rechnende Art.</summary>
    public bool KuehlUebergabeAktiv => Stand.KuehlungAktiv && Stand.KuehluebergabeAktiv && KuehlArtRechnet;

    /// <summary>Ein Feld des Unterabschnitts meldet seinen Fehlerzustand — gemerkt für das Ausblenden.</summary>
    public void KuehlFehlerMelden((string Feld, bool Fehlerhaft) e)
    {
        FehlerMelden(e);
        if (e.Fehlerhaft) _kuehlFehlerfelder.Add(e.Feld); else _kuehlFehlerfelder.Remove(e.Feld);
    }

    private void KuehlFehlerfelderLeeren()
    {
        foreach (string f in _kuehlFehlerfelder) Fehlerfelder.Remove(f);
        _kuehlFehlerfelder.Clear();
    }

    /// <summary>
    /// Der Haken „Kühlübergabe rechnen" (A1). Ohne Haken bleiben alle Werte im Stand — auch die
    /// Art (A1: die Art bleibt beim Abschalten); eine Fehleingabe in einem ausgeblendeten Feld
    /// hält den Speicherweg nicht mehr an.
    /// </summary>
    public void KuehluebergabeSetzen(bool wert)
    {
        Stand.KuehluebergabeAktiv = wert;
        if (!wert) KuehlFehlerfelderLeeren();
    }

    /// <summary>
    /// Die Kühlübergabeart über ihren Listenplatz in <see cref="Waermeuebergabevorgaben.KuehlArten"/>
    /// (0 = ideal). „ideal" und NULL sind dasselbe: War beim Laden NULL, bleibt es NULL.
    /// </summary>
    public void KuehlArtWaehlen(int? index)
    {
        if (index is null || index.Value < 0 || index.Value >= Waermeuebergabevorgaben.KuehlArten.Count) return;
        string gewaehlt = Waermeuebergabevorgaben.KuehlArten[index.Value];
        if (gewaehlt == DbWerte.KUEHLUEBERGABE_IDEAL)
        {
            bool geladenIdeal = KuehlArtBeimLaden is null || KuehlArtBeimLaden == DbWerte.KUEHLUEBERGABE_IDEAL;
            Stand.KuehlUebergabeArt = geladenIdeal ? KuehlArtBeimLaden : null;
            KuehlFehlerfelderLeeren();
            return;
        }
        Stand.KuehlUebergabeArt = gewaehlt;
    }

    /// <summary>Der Listenplatz der Kühlübergabeart; NULL = ideal (0); eine unbekannte Art = −1.</summary>
    public int KuehlArtindex
    {
        get
        {
            if (string.IsNullOrEmpty(Stand.KuehlUebergabeArt)) return 0;
            for (int i = 0; i < Waermeuebergabevorgaben.KuehlArten.Count; i++)
                if (Waermeuebergabevorgaben.KuehlArten[i] == Stand.KuehlUebergabeArt) return i;
            return -1;
        }
    }

    /// <summary>Die Einträge der Kühlübergabeart: die vier Arten — eine unbekannte gespeicherte Art vorangestellt.</summary>
    public IReadOnlyList<(int Id, string Text)> KuehlArteintraege(GebaeudeHuelleTexte t)
    {
        var l = new List<(int, string)>();
        if (KuehlArtindex < 0) l.Add((-1, Stand.KuehlUebergabeArt ?? ""));
        for (int i = 0; i < Waermeuebergabevorgaben.KuehlArten.Count; i++)
            l.Add((i, t.Kuehluebergabe.Artname(Waermeuebergabevorgaben.KuehlArten[i])));
        return l;
    }

    /// <summary>Die Raumtemperatur im Auslegungspunkt der Kühlung, die gilt: das Feld, sonst der Kühlsollwert.</summary>
    public double? KuehlAuslegungRaumWirksam => Stand.KuehlAuslegungRaumtemperatur ?? Stand.KuehlSollwert;

    /// <summary>Der Auslegungsvorlauf der Kühlung, der gilt: das Feld, sonst die Vorgabe der Art.</summary>
    public double? KuehlAuslegungVorlaufWirksam
        => Stand.KuehlAuslegungVorlauf ?? Waermeuebergabevorgaben.KuehlVorlauf(Stand.KuehlUebergabeArt);

    /// <summary>Die Vorlaufgrenze, die gilt: das Feld, sonst die Vorgabe der Art; <c>null</c> = keine Grenze.</summary>
    public double? KuehlVorlaufgrenzeWirksam
        => Stand.KuehlVorlaufgrenze ?? Waermeuebergabevorgaben.KuehlVorlaufgrenze(Stand.KuehlUebergabeArt);

    // ---- Herleitungszeilen der Kühlübergabe (9.1: „Jede Vorgabe steht als Zahl") ----

    /// <summary>Die Zeile unter Haken und Art: ohne Haken, „ideal" oder die Vorgaben der Art mit Zahlen.</summary>
    public string KuehlArtZeile(GebaeudeHuelleTexte t)
    {
        KuehluebergabeTexte k = t.Kuehluebergabe;
        if (!Stand.KuehluebergabeAktiv) return k.ZeileAus;
        if (!KuehlArtRechnet) return k.ZeileIdeal;
        string art = Stand.KuehlUebergabeArt!;
        double? grenze = Waermeuebergabevorgaben.KuehlVorlaufgrenze(art);
        return string.Format(k.ZeileArt, k.Artname(art),
                             Zahl(Waermeuebergabevorgaben.KuehlExponent(art), 2),
                             Zahl(Waermeuebergabevorgaben.KuehlVorlauf(art), 0),
                             Zahl(Waermeuebergabevorgaben.KuehlRuecklauf(art), 0),
                             Zahl(Waermeuebergabevorgaben.KuehlStrahlungsanteil(art), 2),
                             grenze.HasValue ? Zahl(grenze, 0) + " °C" : k.GrenzeKeine);
    }

    /// <summary>
    /// Die Wirksamkeit samt Grund (Stufe, Kühlung, Art): ohne Kühlsollwert nicht wirksam; mit einem
    /// geöffneten Projekt, ob es koppelt und kühlt; sonst die Regel.
    /// </summary>
    public string KuehlWirksamZeile(GebaeudeHuelleTexte t, UebergabeHerleitungDaten? h)
    {
        KuehluebergabeTexte k = t.Kuehluebergabe;
        if (Stand.KuehlSollwert is null) return k.ZeileOhneSollwert;
        KuehluebergabeHerleitungDaten? kh = h?.Kuehlung;
        if (kh is null) return k.ZeileProjekt;
        if (!kh.ProjektKoppelt) return k.ZeileProjektOhneStufe;
        if (!kh.ProjektKuehlt) return k.ZeileProjektOhneKaelte;
        return k.ZeileWirksam;
    }

    /// <summary>Die Zeile der Auslegungs-Raumtemperatur: leer gilt der Kühlsollwert.</summary>
    public string KuehlRaumZeile(GebaeudeHuelleTexte t)
        => string.Format(t.Kuehluebergabe.ZeileRaum, Zahl(Stand.KuehlSollwert, 1));

    /// <summary>
    /// Die Zeile der Nennleistung — die Kühllast des Auslegungstags mit Tag und Tagesmittel, sonst
    /// die Regel ohne Zahl bzw. der Grund, warum der Kern keine Zahl liefert (A2).
    /// </summary>
    public string KuehlNennleistungZeile(GebaeudeHuelleTexte t, UebergabeHerleitungDaten? h)
    {
        KuehluebergabeTexte k = t.Kuehluebergabe;
        KuehluebergabeHerleitungDaten? kh = h?.Kuehlung;
        if (kh is not null && !string.IsNullOrEmpty(kh.Befund)) return string.Format(k.ZeileHerleitungBefund, kh.Befund);
        if (kh?.AuslegungskuehllastKw is double kw)
            return string.Format(k.ZeileNennleistung, Zahl(kw, 1), kh.AuslegungstagText, Zahl(kh.AuslegungstagMittelC, 1));
        return k.ZeileNennleistungOhne;
    }

    /// <summary>Die Zeile des Kaltwasser-Vorlaufs: aus der Anlage, hochgemischt oder die Auslegung; ohne Projekt die Regel.</summary>
    public string KuehlVorlaufZeile(GebaeudeHuelleTexte t, UebergabeHerleitungDaten? h)
    {
        KuehluebergabeTexte k = t.Kuehluebergabe;
        KuehluebergabeHerleitungDaten? kh = h?.Kuehlung;
        if (kh?.VorlaufC is not double v || !string.IsNullOrEmpty(kh.Befund)) return k.ZeileVorlaufOhne;
        if (!kh.VorlaufAusAnlage) return string.Format(k.ZeileVorlaufAuslegung, Zahl(v, 1));
        return kh.Gekappt
            ? string.Format(k.ZeileVorlaufGemischt, Zahl(kh.VorlaufQuelleC, 1), Zahl(v, 1))
            : string.Format(k.ZeileVorlaufAnlage, Zahl(v, 1));
    }

    /// <summary>
    /// Der leise Hinweis, dass die Vorlaufgrenze über dem Auslegungsvorlauf liegt (die Nennleistung
    /// wird nie erreicht); leer, wenn nicht.
    /// </summary>
    public string KuehlGrenzeHinweis(GebaeudeHuelleTexte t)
        => KuehlVorlaufgrenzeWirksam is double g && KuehlAuslegungVorlaufWirksam is double v && g > v
            ? string.Format(t.Kuehluebergabe.ZeileGrenzeUeberAuslegung, Zahl(g, 1), Zahl(v, 1))
            : "";

    /// <summary>Der Platzhalter der Vorlaufgrenze: die Vorgabe der Art, sonst „keine Grenze".</summary>
    public string KuehlGrenzePlatzhalter(GebaeudeHuelleTexte t)
        => Waermeuebergabevorgaben.KuehlVorlaufgrenze(Stand.KuehlUebergabeArt) is double g
            ? Vorgabe(g, t)
            : t.Kuehluebergabe.VorgabeKeineGrenze;

    /// <summary>Der Platzhalter der Nennleistung: die Kühllast des Auslegungstags, sonst „Vorgabe: hergeleitet".</summary>
    public static string KuehlNennleistungPlatzhalter(UebergabeHerleitungDaten? h, GebaeudeHuelleTexte t)
        => h?.Kuehlung?.AuslegungskuehllastKw is double kw && string.IsNullOrEmpty(h.Kuehlung.Befund)
            ? Vorgabe(kw, t)
            : t.Kuehluebergabe.VorgabeHergeleitet;

    // =====================================================================
    //  Abgeleitet — gerechnet im Kern, hier nur zusammengesetzt
    // =====================================================================

    /// <summary>Rechnet der Stand auf dem VDI-Weg? (Anzeige = Rechnung, ADR-006)</summary>
    public bool IstVdi6007 => Gebaeuderechenweg.IstVdi6007(Stand.Modell);

    /// <summary>Summe Ost + West: die zwei Felder, sonst das Bestandsfeld; <c>null</c> = nur eines gesetzt.</summary>
    public double? SummeOstWest => Gebaeudehuellbilanz.FensterOstWest(
        Stand.FensterflaecheOst, Stand.FensterflaecheWest, Stand.FensterflaecheOstWest);

    /// <summary>Die gesamte Fensterfläche = Süd + Ost + West + Nord (gerechnet, nie eingegeben).</summary>
    public double Fenstergesamt
        => (Stand.FensterflaecheSued ?? 0) + (SummeOstWest ?? 0) + (Stand.FensterflaecheNord ?? 0);

    /// <summary>Die acht Zeilen des Hüll-Rasters.</summary>
    public IReadOnlyList<Huellzeile> Huellzeilen => Gebaeudehuellbilanz.Zeilen(
        Stand.UWertAussenwand, Stand.FlaecheAussenwand, Stand.UWertFenster, Fenstergesamt,
        Stand.UWertDachflaeche, Stand.Dachflaeche, Stand.UWertGrundflaeche, Stand.Grundflaeche,
        Stand.UWertSonstiges, Stand.SonstigeFlaechen,
        Stand.WbvkFensterWand, Stand.AnschlussFensterWand,
        Stand.WbvkAussenwandKeller, Stand.AnschlussAussenwandKeller,
        Stand.WbvkWandDach, Stand.AnschlussWandDach);

    /// <summary>H_T [W/K].</summary>
    public double HT => Gebaeudehuellbilanz.TransmissionWK(Huellzeilen);

    /// <summary>
    /// H_ve [W/K] mit dem Luftwechsel des gewählten Rechenwegs: auf dem VDI-Weg der wirksame
    /// (Infiltration + Nutzerlüftung, sonst Luftwechselrate), auf dem Tagesbilanz-Weg die
    /// Luftwechselrate.
    /// </summary>
    public double HVe => Gebaeudehuellbilanz.LueftungWK(
        IstVdi6007 ? WirksamerLuftwechsel : Stand.Luftwechselrate, Stand.WohnflaecheGesamt, Stand.Raumhoehe);

    /// <summary>Der Luftwechsel des VDI-Wegs [1/h] — dieselbe Regel wie im Eingangsbauer des Kerns.</summary>
    public double WirksamerLuftwechsel => Gebaeudemodellvorgaben.WirksamerLuftwechsel(
        Stand.Luftwechselrate, Stand.LuftwechselInfiltration, Stand.LuftwechselNutzer);

    /// <summary>
    /// Der höchste Heizsollwert des Stands [°C] — über dieselbe Regel des Kerns, die der
    /// Sollwertfahrplan des Stundenmodells kennt; leere Felder gelten wie beim Schreiben als 0.
    /// </summary>
    public double HoechsterHeizsollwert => Gebaeudemodellvorgaben.HoechsterHeizsollwert(
        Stand.SollTag ?? 0, Stand.NachtAbsenkung ?? 0, Stand.WochenendAbsenkung ?? 0,
        Stand.SollFerien ?? 0, FerienAktiv);

    /// <summary>Gilt der Ferienfahrplan? Feriensollwert gesetzt und mindestens ein Zeitraum eingetragen.</summary>
    public bool FerienAktiv
    {
        get
        {
            if (!((Stand.SollFerien ?? 0) > 0)) return false;
            int[] beginn = Ferienbeginne(), ende = Ferienenden();
            for (int n = 0; n < 4; n++)
                if (beginn[n] > 0 && beginn[n] < 366 && ende[n] > 0 && ende[n] < 366) return true;
            return false;
        }
    }

    /// <summary>Die vier Ferienbeginne als Jahrestage (aus Tag und Monat der Felder).</summary>
    public int[] Ferienbeginne()
    {
        var b = new int[4];
        for (int n = 0; n < 4; n++) b[n] = Ferienzeit.Jahrestag(BeginnMonat[n], BeginnTag[n]);
        return b;
    }

    /// <summary>Die vier Ferienenden als Jahrestage.</summary>
    public int[] Ferienenden()
    {
        var e = new int[4];
        for (int n = 0; n < 4; n++) e[n] = Ferienzeit.Jahrestag(EndeMonat[n], EndeTag[n]);
        return e;
    }

    // =====================================================================
    //  Herleitungszeilen und Platzhalter — dieselben Sätze in Editor und Stammblatt
    // =====================================================================

    /// <summary>
    /// Die Herleitungszeile unter dem Schalter „Rechenweg": der Weg, der rechnet, und — ohne
    /// Angabe — dass die Vorgabe des Programms gilt (ADR-006).
    /// </summary>
    public string Rechenwegzeile(GebaeudeHuelleTexte t)
    {
        string zeile = IstVdi6007 ? t.ZeileRechenwegVdi6007 : t.ZeileRechenwegTagesbilanz;
        if (Stand.Modell is not null) return zeile;
        string vorgabe = Gebaeuderechenweg.IstVdi6007(null) ? t.RechenwegVdi6007 : t.RechenwegTagesbilanz;
        return zeile + " " + t.ZeileRechenwegVorgabe.Replace("{0}", vorgabe);
    }

    /// <summary>
    /// Die Herleitungszeile der Gruppe „Kühlung" (Kühlkonzept 8.1) — sie nennt den Rückfall in
    /// BEIDEN Stellungen des Hakens: mit Haken die Prüfregel samt höchstem Heizsollwert, ohne
    /// Haken die Maximalraumtemperatur, an der die Überhitzung des frei laufenden Gebäudes
    /// gezählt wird (Entscheid E32).
    /// </summary>
    public string Kuehlungszeile(GebaeudeHuelleTexte t)
    {
        double maxTemperatur = Stand.MaxTemperatur ?? 0;
        if (maxTemperatur < 1) maxTemperatur = 24;   // dieselbe Ableitung wie beim Schreiben
        if (!Stand.KuehlungAktiv)
            return string.Format(t.ZeileKuehlungAus, Zahl(maxTemperatur, 1));
        return string.Format(t.ZeileKuehlungAn,
                             Zahl(Gebaeudemodellvorgaben.KuehlsollwertAbstand, 0),
                             Zahl(HoechsterHeizsollwert, 1),
                             Zahl(maxTemperatur, 1));
    }

    // ---- Wärmeübergabe (9.1: „Jede Vorgabe steht als Zahl in der Herleitungszeile") ----

    /// <summary>Die Zeile unter Haken und Art: ohne Haken, „ideal" oder die Vorgaben der Art mit Zahlen.</summary>
    public string UebergabeArtZeile(GebaeudeHuelleTexte t)
    {
        WaermeuebergabeTexte u = t.Uebergabe;
        if (!Stand.HeizkreisAktiv) return u.ZeileAus;
        if (!ArtRechnet) return u.ZeileIdeal;
        string art = Stand.UebergabeArt!;
        return string.Format(u.ZeileArt, u.Artname(art),
                             Zahl(Waermeuebergabevorgaben.Exponent(art), 2),
                             Zahl(Waermeuebergabevorgaben.Vorlauf(art), 0),
                             Zahl(Waermeuebergabevorgaben.Ruecklauf(art), 0),
                             Zahl(Waermeuebergabevorgaben.Strahlungsanteil(art), 2));
    }

    /// <summary>Die Zeile der Auslegungs-Raumtemperatur: leer gilt das Soll am Tag.</summary>
    public string UebergabeRaumZeile(GebaeudeHuelleTexte t)
        => string.Format(t.Uebergabe.ZeileRaum, Zahl(Stand.SollTag ?? 0, 1));

    /// <summary>Die Zeile der Auslegungs-Außentemperatur — mit der hergeleiteten Zahl, wo es eine gibt.</summary>
    public string UebergabeAussenZeile(GebaeudeHuelleTexte t, UebergabeHerleitungDaten? h)
        => h?.AuslegungAussenC is double a
            ? string.Format(t.Uebergabe.ZeileAussen, Zahl(a, 0))
            : t.Uebergabe.ZeileAussenOhne;

    /// <summary>
    /// Die Zeile der Nennleistung — die hergeleitete Auslegungsheizlast mit ihrem Auslegungspunkt,
    /// sonst die Regel ohne Zahl bzw. der Grund, warum der Kern keine Zahl liefert.
    /// </summary>
    public string UebergabeNennleistungZeile(GebaeudeHuelleTexte t, UebergabeHerleitungDaten? h)
    {
        WaermeuebergabeTexte u = t.Uebergabe;
        if (h is not null && !string.IsNullOrEmpty(h.Befund)) return string.Format(u.ZeileHerleitungBefund, h.Befund);
        if (h?.AuslegungsheizlastKw is double kw)
        {
            double aussen = Stand.AuslegungAussentemperatur ?? h.AuslegungAussenC ?? double.NaN;
            return string.Format(u.ZeileNennleistung, Zahl(kw, 1), Zahl(aussen, 0), Zahl(AuslegungRaumWirksam, 1));
        }
        return u.ZeileNennleistungOhne;
    }

    /// <summary>Die Zeile der Heizkurve: an mit dem Auslegungspunkt, aus mit dem festen Vorlauf.</summary>
    public string UebergabeHeizkurveZeile(GebaeudeHuelleTexte t, UebergabeHerleitungDaten? h)
    {
        WaermeuebergabeTexte u = t.Uebergabe;
        if (!Stand.HeizkurveAktiv) return u.ZeileHeizkurveAus;
        double vorlauf = Stand.AuslegungVorlauf ?? Waermeuebergabevorgaben.Vorlauf(Stand.UebergabeArt) ?? double.NaN;
        double? aussen = Stand.AuslegungAussentemperatur ?? h?.AuslegungAussenC;
        return string.Format(u.ZeileHeizkurveAn, Zahl(vorlauf, 0),
                             aussen.HasValue ? Zahl(aussen.Value, 0) : u.VorgabeHergeleitet,
                             Zahl(Waermeuebergabevorgaben.HeizkurveNiveau, 1),
                             Zahl(Waermeuebergabevorgaben.HeizkurveSteilheit, 2));
    }

    /// <summary>Die Zeile des Proportionalbands (E25).</summary>
    public static string UebergabeBandZeile(GebaeudeHuelleTexte t)
        => string.Format(t.Uebergabe.ZeileBand, Zahl(Waermeuebergabevorgaben.Proportionalband, 1),
                         Zahl(Waermeuebergabevorgaben.BAND_MAX, 0));

    /// <summary>Die Zeile des Zeitprogramms: mit oder ohne Profil.</summary>
    public string UebergabeProfilZeile(GebaeudeHuelleTexte t)
        => ProfilWerte is not null ? t.Uebergabe.ZeileProfilMit : t.Uebergabe.ZeileProfilOhne;

    /// <summary>Der Platzhalter eines Feldes mit einer Vorgabe der Art (Exponent, Vorlauf, Rücklauf).</summary>
    public static string UebergabeVorgabe(double? wert, GebaeudeHuelleTexte t)
        => wert.HasValue ? Vorgabe(wert.Value, t) : "";

    /// <summary>Der Platzhalter eines hergeleiteten Feldes: die Zahl, sonst „Vorgabe: hergeleitet".</summary>
    public static string UebergabeHergeleitet(double? wert, GebaeudeHuelleTexte t)
        => wert.HasValue ? Vorgabe(wert.Value, t) : t.Uebergabe.VorgabeHergeleitet;

    /// <summary>Die Einträge der Übergabeart: die vier Arten — eine unbekannte gespeicherte Art vorangestellt.</summary>
    public IReadOnlyList<(int Id, string Text)> Arteintraege(GebaeudeHuelleTexte t)
    {
        var l = new List<(int, string)>();
        if (Artindex < 0) l.Add((-1, Stand.UebergabeArt ?? ""));
        for (int i = 0; i < Waermeuebergabevorgaben.Arten.Count; i++)
            l.Add((i, t.Uebergabe.Artname(Waermeuebergabevorgaben.Arten[i])));
        return l;
    }

    /// <summary>Die vier Einträge der Schnellwahl des Proportionalbands.</summary>
    public static IReadOnlyList<(int Id, string Text)> Bandeintraege(GebaeudeHuelleTexte t)
    {
        var l = new List<(int, string)>();
        IReadOnlyList<double> schnell = Waermeuebergabevorgaben.ProportionalbandSchnellwahl;
        for (int i = 0; i < schnell.Count; i++)
            l.Add((i, schnell[i].ToString("0.0##", CultureInfo.CurrentCulture) + " K"));
        l.Add((schnell.Count, t.Uebergabe.BandFrei));
        return l;
    }

    /// <summary>Die Herleitungszeile des Luftwechsels: Wert und Herkunft.</summary>
    public string Luftwechselzeile(GebaeudeHuelleTexte t)
    {
        double n = Gebaeudemodellvorgaben.WirksamerLuftwechsel(Stand.Luftwechselrate,
            Stand.LuftwechselInfiltration, Stand.LuftwechselNutzer, out Luftwechselherkunft herkunft);
        string quelle = herkunft switch
        {
            Luftwechselherkunft.InfiltrationUndNutzer => t.HerkunftInfiltrationNutzer,
            Luftwechselherkunft.Luftwechselrate => t.HerkunftLuftwechselrate,
            _ => t.HerkunftVorgabe
        };
        return string.Format(t.HinweisLuftwechsel, Zahl(n, 2), quelle);
    }

    /// <summary>Die Herleitungszeile der Sommerlüftungsregel.</summary>
    public static string Sommerlueftungszeile(GebaeudeHuelleTexte t)
        => string.Format(t.HinweisSommerlueftung,
                         Zahl(Gebaeudemodellvorgaben.SommerlueftungSchwelle, 0),
                         Zahl(Gebaeudemodellvorgaben.LuftwechselSommer, 1));

    /// <summary>
    /// Platzhalter der beiden Lüftungsfelder: Sind beide leer, rechnet der VDI-Weg mit der
    /// Luftwechselrate — dann steht nichts da; sonst die Vorgabe des leeren Glieds.
    /// </summary>
    public string LuftwechselPlatzhalter(double vorgabe, GebaeudeHuelleTexte t)
        => !Stand.LuftwechselInfiltration.HasValue && !Stand.LuftwechselNutzer.HasValue
           && Stand.Luftwechselrate is double n && n > 0
            ? ""
            : Vorgabe(vorgabe, t);

    /// <summary>Der Platzhalter von Ost und West: die Hälfte des Bestandsfelds Ost + West.</summary>
    public string OstWestPlatzhalter(GebaeudeHuelleTexte t)
        => Stand.FensterflaecheOstWest is double ow && !Stand.FensterflaecheOst.HasValue
                                                    && !Stand.FensterflaecheWest.HasValue
            ? Vorgabe(0.5 * ow, t)
            : "";

    /// <summary>„Vorgabe 0,3" — der Platzhalter eines leeren Feldes, das NULL speichert.</summary>
    public static string Vorgabe(double wert, GebaeudeHuelleTexte t)
        => t.VorgabeFormat.Replace("{0}", wert.ToString("0.###", CultureInfo.CurrentCulture));

    // =====================================================================
    //  Prüfen und Ableiten — der EINE Schreibweg (Konzept 4.8; E27/U1)
    // =====================================================================

    /// <summary>
    /// <b>Die Prüfregeln — genau einmal</b>, für OK, „Speichern unter", „Speichern" im
    /// Stammblatt und den Assistenten. Liefert die erste verletzte Regel, sonst <c>null</c>.
    /// </summary>
    /// <param name="mitName">Muss der Name gesetzt sein? (Anlegen)</param>
    /// <param name="p">Feldnamen und Meldungen der Prüfung.</param>
    /// <param name="t">Das Textbündel der VDI-Struktur (Meldungen und Zeilennamen).</param>
    public GebaeudePruefbefund? Pruefen(bool mitName, GebaeudePrueftexte p, GebaeudeHuelleTexte t)
    {
        if (mitName && string.IsNullOrWhiteSpace(Stand.Name))
            return Huelle(p.MeldungNameFehlt);

        foreach (string feld in Fehlerfelder)
            return Huelle(t.MeldungUngueltig.Replace("{0}", feld));

        // Das Baujahr (G4a): leer ist erlaubt (unbekannt), sonst gilt der Bereich der Spalte -
        // dieselbe Grenze, an der die Datenbank mit ihrem CHECK abweist.
        if (Stand.Baujahr is int jahr && (jahr < GebaeudeSchema.BAUJAHR_MIN || jahr > GebaeudeSchema.BAUJAHR_MAX))
            return Huelle(string.Format(CultureInfo.CurrentCulture, p.MeldungBaujahr,
                                        GebaeudeSchema.BAUJAHR_MIN, GebaeudeSchema.BAUJAHR_MAX));

        // Der Energiestandard (E47, F3): Effizienzhaus 115/100 und 85 gibt es nur für Wohngebäude -
        // die Klappliste bietet sie einem Nichtwohngebäude nicht an; ein gespeicherter bleibt sichtbar
        // und wird hier benannt, statt still zu verschwinden.
        if (!Energiestandard.PasstZu(Stand.Energiestandard, Stand.Verwendung))
            return Huelle(p.MeldungEnergiestandardWohnen.Replace("{0}", Energiestandard.Text(Stand.Energiestandard)));

        // Die Nachtzeit (E43): beide leer (die Vorgabe 22 bis 6 Uhr) oder beide gesetzt, je 0 bis 23
        // und verschieden - DIESELBE Regel, an der der Eingangsbauer des Stundenmodells abbricht
        // (Nachtzeit.Pruefen). Die Felder stehen auf dem zweiten Reiter.
        switch (Nachtzeit.Pruefen(Stand.NachtBeginn, Stand.NachtEnde))
        {
            case NachtzeitBefund.NurEineGesetzt:
                return Temperaturen(string.Format(CultureInfo.CurrentCulture, p.MeldungNachtzeitNurEine,
                                                  Nachtzeit.VORGABE_BEGINN, Nachtzeit.VORGABE_ENDE));
            case NachtzeitBefund.AusserhalbDesTages:
                return Temperaturen(string.Format(CultureInfo.CurrentCulture, p.MeldungNachtzeitBereich,
                                                  Nachtzeit.STUNDE_MIN, Nachtzeit.STUNDE_MAX));
            case NachtzeitBefund.BeginnGleichEnde:
                return Temperaturen(p.MeldungNachtzeitGleich);
        }

        (double? wert, string name)[] pflicht =
        {
            (Stand.WohnflaecheGesamt, p.FeldWohnflaeche),
            (Stand.FlaecheNutzer, p.FeldFlaecheNutzer),
            (Stand.Waermegewinne, p.FeldWaermegewinne),
            (Stand.Fensterdurchlassgrad, p.FeldFensterdurchlassgrad),
            (Stand.Raumhoehe, p.FeldRaumhoehe),
            (Stand.Luftwechselrate, p.FeldLuftwechsel),
            (Stand.UWertAussenwand, p.FeldUAussenwand),
            (Stand.FlaecheAussenwand, p.FeldFlaecheAussenwand),
            (Stand.UWertFenster, p.FeldUFenster),
            (Stand.UWertDachflaeche, p.FeldUDachflaeche),
            (Stand.Dachflaeche, p.FeldDachflaeche),
            (Stand.UWertGrundflaeche, p.FeldUGrundflaeche),
            (Stand.Grundflaeche, p.FeldGrundflaeche),
            (Stand.UWertSonstiges, p.FeldUSonstiges),
            (Stand.SonstigeFlaechen, p.FeldSonstigeFlaechen),
            (Stand.FensterflaecheNord, p.FeldFFNord),
            (Stand.FensterflaecheSued, p.FeldFFSued)
        };
        foreach ((double? wert, string name) in pflicht)
            if (wert is null) return Huelle(string.Format(p.MeldungZahlFehlt, name));

        if (!(Stand.WohnflaecheGesamt > 0)) return Huelle(t.MeldungNutzflaeche);
        if (!(Stand.FlaecheNutzer > 0)) return Huelle(t.MeldungFlaecheNutzer);
        if (!(Stand.Raumhoehe > 0)) return Huelle(t.MeldungRaumhoehe);
        if (!(Stand.Luftwechselrate > 0)) return Huelle(t.MeldungLuftwechsel);
        if (!(Stand.Fensterdurchlassgrad > 0 && Stand.Fensterdurchlassgrad <= 1))
            return Huelle(t.MeldungGWert);

        // U-Werte nur fuer Bauteile, die eine Flaeche haben - ein Bauteil ohne Flaeche
        // traegt keinen Waermestrom.
        foreach (Huellzeile z in Huellzeilen)
        {
            if (z.IstWaermebruecke || !((z.Groesse ?? 0) > 0)) continue;
            double u = z.Kennwert ?? 0;
            if (u < Gebaeudehuellbilanz.U_MIN || u > Gebaeudehuellbilanz.U_MAX)
                return Huelle(t.MeldungUBereich.Replace("{0}", t.Zeile(z.Bauteil)));
        }

        if (Gebaeudehuellbilanz.MittleresUOpak(Huellzeilen) is double uMittel
            && uMittel >= Gebaeudehuellbilanz.U_OPAK_MITTEL_MAX)
            return Huelle(t.MeldungRRest);

        if (SummeOstWest is null) return Huelle(t.MeldungOstWest);

        double bauweise = BauweiseNachfuehren
            ? Gebaeudebauweise.BauweiseAusBauart(Stand.Bauart, Stand.WohnflaecheGesamt ?? 0)
            : Stand.Bauweise;
        double spezifisch = bauweise / (Stand.WohnflaecheGesamt ?? 1);
        if (spezifisch < Gebaeudehuellbilanz.BAUWEISE_JE_M2_MIN || spezifisch > Gebaeudehuellbilanz.BAUWEISE_JE_M2_MAX)
            return Huelle(t.MeldungBauweise);

        // Stufe KU1 (Kuehlkonzept 8.1): die Regeln der Gruppe „Kuehlung" - nur mit Haken. Der
        // Abstand zum hoechsten Heizsollwert ist DIESELBE Zahl, mit der der Loeser abbricht
        // (Gebaeudemodellvorgaben.KuehlsollwertAbstand); ein leerer Sollwert heisst „Kuehlung
        // aus" und ist erlaubt.
        if (Stand.KuehlungAktiv)
        {
            if (Stand.KuehlSollwert is double soll)
            {
                if (soll < Gebaeudemodellvorgaben.KUEHLSOLLWERT_MIN || soll > Gebaeudemodellvorgaben.KUEHLSOLLWERT_MAX)
                    return Kuehlung(string.Format(t.MeldungKuehlsollwertBereich,
                                                Zahl(Gebaeudemodellvorgaben.KUEHLSOLLWERT_MIN, 0),
                                                Zahl(Gebaeudemodellvorgaben.KUEHLSOLLWERT_MAX, 0)));
                double heizMax = HoechsterHeizsollwert;
                if (soll < heizMax + Gebaeudemodellvorgaben.KuehlsollwertAbstand)
                    return Kuehlung(string.Format(t.MeldungKuehlsollwertHeizung, Zahl(soll, 1), Zahl(heizMax, 1),
                                                Zahl(Gebaeudemodellvorgaben.KuehlsollwertAbstand, 0)));
            }
            if (Stand.KuehlleistungMax is double grenze && !(grenze > 0))
                return Kuehlung(t.MeldungKuehlleistung);

            // E37: die Regeln des Unterabschnitts „Kühlübergabe" - nur mit Haken und rechnender Art.
            GebaeudePruefbefund? kuehluebergabe = KuehluebergabePruefen(t);
            if (kuehluebergabe is not null) return kuehluebergabe;
        }

        // Stufe AK1 (Anlagenkopplung 9.1, 9.5): die Regeln der Gruppe „Wärmeübergabe" - nur mit
        // Haken und rechnender Art, mit DENSELBEN Grenzen, an denen der Eingangsbauer des Kerns
        // hart abbricht (Waermeuebergabevorgaben). Ein leeres Feld ist erlaubt: Es gilt die
        // Vorgabe, und die Zusammenhangsregeln rechnen mit ihr.
        GebaeudePruefbefund? uebergabe = UebergabePruefen(t);
        if (uebergabe is not null) return uebergabe;

        string? regel = Ferienzeit.Pruefen(Ferienbeginne(), Ferienenden());
        if (regel is not null) return new GebaeudePruefbefund(p.Ferienmeldung(regel), GebaeudePruefbereich.Ferien);

        return null;
    }

    private static GebaeudePruefbefund Huelle(string meldung) => new(meldung, GebaeudePruefbereich.Huelle);

    private static GebaeudePruefbefund Kuehlung(string meldung) => new(meldung, GebaeudePruefbereich.Kuehlung);

    /// <summary>Eine Regel des zweiten Reiters (Raumtemperaturen, Nachtzeit, Ferien).</summary>
    private static GebaeudePruefbefund Temperaturen(string meldung) => new(meldung, GebaeudePruefbereich.Ferien);

    private static GebaeudePruefbefund Uebergabe(string meldung) => new(meldung, GebaeudePruefbereich.Waermeuebergabe);

    /// <summary>
    /// Die Regeln der Gruppe „Wärmeübergabe" (9.1, 9.5): Bereiche der Felder, die Zusammenhänge
    /// des Auslegungspunkts (Vorlauf über Raum, Rücklauf dazwischen, außen unter Raum) mit den
    /// Werten, die gelten — leeres Feld = Vorgabe der Art bzw. Soll am Tag —, und das
    /// Zeitprogramm streng (H-F10). Ohne Haken oder mit „ideal" gilt keine.
    /// </summary>
    private GebaeudePruefbefund? UebergabePruefen(GebaeudeHuelleTexte t)
    {
        if (!Stand.HeizkreisAktiv) return null;
        WaermeuebergabeTexte u = t.Uebergabe;
        if (Artindex < 0) return Uebergabe(string.Format(u.MeldungArtUnbekannt, Stand.UebergabeArt));
        if (!ArtRechnet) return null;

        GebaeudePruefbefund? Bereich(double? wert, string label, double min, double max)
            => wert is double w && (w < min || w > max)
                ? Uebergabe(string.Format(u.MeldungBereich, Feld(label), Zahl(min, 1), Zahl(max, 1)))
                : null;

        GebaeudePruefbefund? b =
            Bereich(Stand.UebergabeExponent, u.LabelExponent, Waermeuebergabevorgaben.EXPONENT_MIN, Waermeuebergabevorgaben.EXPONENT_MAX)
            ?? Bereich(Stand.AuslegungVorlauf, u.LabelAuslegungVorlauf, Waermeuebergabevorgaben.VORLAUF_MIN, Waermeuebergabevorgaben.VORLAUF_MAX)
            ?? Bereich(Stand.AuslegungRaumtemperatur, u.LabelAuslegungRaum, Waermeuebergabevorgaben.RAUM_MIN, Waermeuebergabevorgaben.RAUM_MAX)
            ?? Bereich(Stand.AuslegungAussentemperatur, u.LabelAuslegungAussen, Waermeuebergabevorgaben.AUSSEN_MIN, Waermeuebergabevorgaben.AUSSEN_MAX)
            ?? Bereich(Stand.ReglerProportionalband, u.LabelProportionalband, Waermeuebergabevorgaben.BAND_MIN, Waermeuebergabevorgaben.BAND_MAX);
        if (b is not null) return b;
        if (Stand.HeizkurveAktiv)
        {
            b = Bereich(Stand.HeizkurveNiveau, u.LabelHeizkurveNiveau, Waermeuebergabevorgaben.NIVEAU_MIN, Waermeuebergabevorgaben.NIVEAU_MAX)
                ?? Bereich(Stand.HeizkurveSteilheit, u.LabelHeizkurveSteilheit, Waermeuebergabevorgaben.STEILHEIT_MIN, Waermeuebergabevorgaben.STEILHEIT_MAX);
            if (b is not null) return b;
        }
        if (Stand.UebergabeLeistungNennKw is double nenn && !(nenn > 0)) return Uebergabe(u.MeldungNennleistung);

        string art = Stand.UebergabeArt!;
        double vorlauf = Stand.AuslegungVorlauf ?? Waermeuebergabevorgaben.Vorlauf(art)!.Value;
        double ruecklauf = Stand.AuslegungRuecklauf ?? Waermeuebergabevorgaben.Ruecklauf(art)!.Value;
        double raum = AuslegungRaumWirksam;
        if (!(vorlauf > raum)) return Uebergabe(string.Format(u.MeldungVorlaufRaum, Zahl(vorlauf, 1), Zahl(raum, 1)));
        if (!(ruecklauf > raum) || !(ruecklauf < vorlauf))
            return Uebergabe(string.Format(u.MeldungRuecklauf, Zahl(ruecklauf, 1), Zahl(raum, 1), Zahl(vorlauf, 1)));
        if (Stand.AuslegungAussentemperatur is double aussen && !(aussen < raum))
            return Uebergabe(string.Format(u.MeldungAussenRaum, Zahl(aussen, 1), Zahl(raum, 1)));

        switch (ProfilBefund)
        {
            case AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl:
                return Uebergabe(string.Format(u.MeldungProfilWertzahl, _profilGefunden, AnlagenkopplungSchema.WOCHENWERTE));
            case AnlagenkopplungSchema.WochenprofilBefund.KeineZahl:
                return Uebergabe(string.Format(u.MeldungProfilKeineZahl, _profilStelle));
        }
        if (ProfilWerte is not null)
            for (int i = 0; i < ProfilWerte.Length; i++)
                if (ProfilWerte[i] < Waermeuebergabevorgaben.SOLLWERT_MIN || ProfilWerte[i] > Waermeuebergabevorgaben.SOLLWERT_MAX)
                    return Uebergabe(string.Format(u.MeldungProfilWert, i + 1, Zahl(ProfilWerte[i], 1),
                                                   Zahl(Waermeuebergabevorgaben.SOLLWERT_MIN, 0),
                                                   Zahl(Waermeuebergabevorgaben.SOLLWERT_MAX, 0)));
        return null;
    }

    /// <summary>
    /// Die Regeln des Unterabschnitts „Kühlübergabe" (E37; 7.2, 9.1): Bereiche der Felder mit den
    /// Grenzen des Eingangsbauers, die Nennleistung größer null und die Reihenfolge des
    /// Auslegungspunkts (Vorlauf unter Rücklauf unter Raum) mit den Werten, die gelten — leeres
    /// Feld = Vorgabe der Art bzw. Kühlsollwert. Ohne Haken oder mit „ideal" gilt keine; eine
    /// Vorlaufgrenze über dem Auslegungsvorlauf ist erlaubt (ein Hinweis, keine Regel).
    /// </summary>
    private GebaeudePruefbefund? KuehluebergabePruefen(GebaeudeHuelleTexte t)
    {
        if (!Stand.KuehlungAktiv || !Stand.KuehluebergabeAktiv) return null;
        KuehluebergabeTexte k = t.Kuehluebergabe;
        if (KuehlArtindex < 0) return Kuehlung(string.Format(k.MeldungArtUnbekannt, Stand.KuehlUebergabeArt));
        if (!KuehlArtRechnet) return null;

        GebaeudePruefbefund? Bereich(double? wert, string label, double min, double max)
            => wert is double w && (w < min || w > max)
                ? Kuehlung(string.Format(k.MeldungBereich, Feld(label), Zahl(min, 1), Zahl(max, 1)))
                : null;

        GebaeudePruefbefund? b =
            Bereich(Stand.KuehlUebergabeExponent, k.LabelExponent, Waermeuebergabevorgaben.EXPONENT_MIN, Waermeuebergabevorgaben.EXPONENT_MAX)
            ?? Bereich(Stand.KuehlAuslegungVorlauf, k.LabelAuslegungVorlauf, Waermeuebergabevorgaben.KUEHL_VORLAUF_MIN, Waermeuebergabevorgaben.KUEHL_VORLAUF_MAX)
            ?? Bereich(Stand.KuehlAuslegungRaumtemperatur, k.LabelAuslegungRaum, Waermeuebergabevorgaben.KUEHL_RAUM_MIN, Waermeuebergabevorgaben.KUEHL_RAUM_MAX)
            ?? Bereich(Stand.KuehlVorlaufgrenze, k.LabelVorlaufgrenze, Waermeuebergabevorgaben.KUEHL_VORLAUF_MIN, Waermeuebergabevorgaben.KUEHL_VORLAUF_MAX);
        if (b is not null) return b;
        if (Stand.KuehlUebergabeLeistungNennKw is double nenn && !(nenn > 0)) return Kuehlung(k.MeldungNennleistung);

        string art = Stand.KuehlUebergabeArt!;
        double vorlauf = Stand.KuehlAuslegungVorlauf ?? Waermeuebergabevorgaben.KuehlVorlauf(art)!.Value;
        double ruecklauf = Stand.KuehlAuslegungRuecklauf ?? Waermeuebergabevorgaben.KuehlRuecklauf(art)!.Value;
        // Ohne Kühlsollwert und ohne Feld ruht die Kälteseite - dann gibt es keinen Raum zu prüfen.
        if (KuehlAuslegungRaumWirksam is double raum)
        {
            if (!(vorlauf < ruecklauf) || !(ruecklauf < raum))
                return Kuehlung(string.Format(k.MeldungReihenfolge, Zahl(vorlauf, 1), Zahl(ruecklauf, 1), Zahl(raum, 1)));
        }
        else if (!(vorlauf < ruecklauf))
            return Kuehlung(string.Format(k.MeldungReihenfolge, Zahl(vorlauf, 1), Zahl(ruecklauf, 1), "—"));
        return null;
    }

    /// <summary>
    /// <b>Die Ableitungen des Vorläufers</b> (<c>btn_Speichern_Click</c> der zweiten Maske) und
    /// der Hülle — unmittelbar vor dem Schreiben: leere Felder der Temperaturen gelten als 0,
    /// Maximaltemperatur &lt; 1 → 24, die Flags Wochenende und Ferien, Winterferienbeginn 0 → 366;
    /// dazu die Summe Ost + West und die Bauweise.
    ///
    /// <para><b>Der Warmwasserbedarf bleibt stehen.</b> Der Vorläufer setzte <c>WW_Bedarf</c> beim
    /// Übernehmen seiner zweiten Maske auf 0 — nur wer sie öffnete und bestätigte. Als Ableitung im
    /// OK-Weg hätte das JEDES Speichern getan (Katalogeditor, Stammblatt, „Hülle und Zonen…") und
    /// den Wert gelöscht, den kein Feld dieses Dialogs zeigt (Befund 25.09.2026: 700 → 0). Der
    /// Stand trägt ihn unverändert vom Laden bis zum Schreiben.</para>
    /// </summary>
    public void Ableiten()
    {
        if (BauweiseNachfuehren) BauweiseBilden();

        // E47 (F2): DAS BAUJAHR FUEHRT - geschrieben wird die Klasse, die die Klappliste zeigt.
        Stand.Baualtersklasse = KlasseWirksam;

        Stand.FensterflaecheOstWest = SummeOstWest ?? 0;

        Stand.SollTag ??= 0;
        Stand.NachtAbsenkung ??= 0;
        double max = Stand.MaxTemperatur ?? 0;
        Stand.MaxTemperatur = max < 1 ? 24 : max;
        Stand.WochenendAbsenkung ??= 0;
        Stand.Wochenende = (Stand.WochenendAbsenkung ?? 0) > 0 ? 1 : 0;
        Stand.SollFerien ??= 0;
        Stand.Ferien = (Stand.SollFerien ?? 0) > 0 ? 1 : 0;

        Stand.WbvkFensterWand ??= 0;
        Stand.WbvkAussenwandKeller ??= 0;
        Stand.WbvkWandDach ??= 0;
        Stand.AnschlussFensterWand ??= 0;
        Stand.AnschlussWandDach ??= 0;
        Stand.AnschlussAussenwandKeller ??= 0;

        int[] beginn = Ferienbeginne();
        beginn[0] = Ferienzeit.WinterbeginnGehoben(beginn[0]);
        Stand.Ferienbeginn = beginn;
        Stand.Ferienende = Ferienenden();
    }

    // =====================================================================
    //  Geändert? — der Vergleich mit dem geladenen Satz (Stammblatt)
    // =====================================================================

    /// <summary>
    /// <b>Wie viele Felder vom geladenen Satz abweichen</b> — jedes Eingabefeld zählt einmal,
    /// die sechzehn Ferienzahlen je Zahl; ein Feld mit ungültigem Text zählt als geändert
    /// (sonst ginge es beim Satzwechsel still verloren). Abgeleitete Größen (Bauweise,
    /// Summen) zählen nicht.
    /// </summary>
    public int Abweichungen(GebaeudeKatalogDaten? geladen)
    {
        if (geladen is null) return 0;
        GebaeudeKatalogDaten a = Stand, g = geladen;
        int n = Fehlerfelder.Count;

        void T(string? x, string? y) { if (!string.Equals(x ?? "", y ?? "", StringComparison.Ordinal)) n++; }
        void Z(double? x, double? y) { if (x != y) n++; }
        void I(int? x, int? y) { if (x != y) n++; }
        void B(bool x, bool y) { if (x != y) n++; }

        T(a.Typ, g.Typ); T(a.Beschreibung, g.Beschreibung); T(a.Gebaeudeart, g.Gebaeudeart);
        T(a.Verwendung, g.Verwendung); I(a.Baualtersklasse, g.Baualtersklasse); I(a.Bauart, g.Bauart);
        I(a.Baujahr, g.Baujahr); T(a.Energiestandard, g.Energiestandard);
        I(a.NachtBeginn, g.NachtBeginn); I(a.NachtEnde, g.NachtEnde);

        Z(a.WohnflaecheGesamt, g.WohnflaecheGesamt); Z(a.FlaecheNutzer, g.FlaecheNutzer);
        Z(a.Waermegewinne, g.Waermegewinne); Z(a.Fensterdurchlassgrad, g.Fensterdurchlassgrad);
        Z(a.Raumhoehe, g.Raumhoehe); Z(a.Luftwechselrate, g.Luftwechselrate);

        Z(a.FensterflaecheNord, g.FensterflaecheNord); Z(a.FensterflaecheSued, g.FensterflaecheSued);
        Z(a.FensterflaecheOst, g.FensterflaecheOst); Z(a.FensterflaecheWest, g.FensterflaecheWest);
        Z(a.FlaecheAussenwand, g.FlaecheAussenwand); Z(a.Dachflaeche, g.Dachflaeche);
        Z(a.Grundflaeche, g.Grundflaeche); Z(a.SonstigeFlaechen, g.SonstigeFlaechen);

        Z(a.UWertAussenwand, g.UWertAussenwand); Z(a.UWertFenster, g.UWertFenster);
        Z(a.UWertDachflaeche, g.UWertDachflaeche); Z(a.UWertGrundflaeche, g.UWertGrundflaeche);
        Z(a.UWertSonstiges, g.UWertSonstiges);

        Z(a.WbvkFensterWand, g.WbvkFensterWand); Z(a.WbvkAussenwandKeller, g.WbvkAussenwandKeller);
        Z(a.WbvkWandDach, g.WbvkWandDach); Z(a.AnschlussFensterWand, g.AnschlussFensterWand);
        Z(a.AnschlussWandDach, g.AnschlussWandDach); Z(a.AnschlussAussenwandKeller, g.AnschlussAussenwandKeller);

        Z(a.SollTag, g.SollTag); Z(a.NachtAbsenkung, g.NachtAbsenkung); Z(a.MaxTemperatur, g.MaxTemperatur);
        Z(a.WochenendAbsenkung, g.WochenendAbsenkung); Z(a.SollFerien, g.SollFerien);

        T(a.Modell, g.Modell); T(a.GrundflaecheRandbedingung, g.GrundflaecheRandbedingung);
        Z(a.Kellertemperatur, g.Kellertemperatur);
        Z(a.Rahmenanteil, g.Rahmenanteil); Z(a.Verschattungsfaktor, g.Verschattungsfaktor);
        Z(a.MasseanteilAussen, g.MasseanteilAussen); Z(a.Innenflaechenfaktor, g.Innenflaechenfaktor);
        Z(a.HeizungStrahlungsanteil, g.HeizungStrahlungsanteil); Z(a.HeizleistungMax, g.HeizleistungMax);
        B(a.AussenbauteileStrahlung, g.AussenbauteileStrahlung);
        Z(a.LuftwechselInfiltration, g.LuftwechselInfiltration); Z(a.LuftwechselNutzer, g.LuftwechselNutzer);
        B(a.Sommerlueftung, g.Sommerlueftung);
        B(a.KuehlungAktiv, g.KuehlungAktiv); Z(a.KuehlSollwert, g.KuehlSollwert);
        Z(a.KuehlleistungMax, g.KuehlleistungMax);

        // Stufe AK1: die dreizehn Felder der Wärmeübergabe (das Zeitprogramm als EIN Feld).
        B(a.HeizkreisAktiv, g.HeizkreisAktiv); T(a.UebergabeArt, g.UebergabeArt);
        Z(a.UebergabeExponent, g.UebergabeExponent); Z(a.UebergabeLeistungNennKw, g.UebergabeLeistungNennKw);
        Z(a.AuslegungVorlauf, g.AuslegungVorlauf); Z(a.AuslegungRuecklauf, g.AuslegungRuecklauf);
        Z(a.AuslegungRaumtemperatur, g.AuslegungRaumtemperatur); Z(a.AuslegungAussentemperatur, g.AuslegungAussentemperatur);
        B(a.HeizkurveAktiv, g.HeizkurveAktiv); Z(a.HeizkurveNiveau, g.HeizkurveNiveau);
        Z(a.HeizkurveSteilheit, g.HeizkurveSteilheit); Z(a.ReglerProportionalband, g.ReglerProportionalband);
        T(a.Sollwertprofil, g.Sollwertprofil);

        // E37: die acht Felder der Kühlübergabe.
        B(a.KuehluebergabeAktiv, g.KuehluebergabeAktiv); T(a.KuehlUebergabeArt, g.KuehlUebergabeArt);
        Z(a.KuehlUebergabeExponent, g.KuehlUebergabeExponent); Z(a.KuehlUebergabeLeistungNennKw, g.KuehlUebergabeLeistungNennKw);
        Z(a.KuehlAuslegungVorlauf, g.KuehlAuslegungVorlauf); Z(a.KuehlAuslegungRuecklauf, g.KuehlAuslegungRuecklauf);
        Z(a.KuehlAuslegungRaumtemperatur, g.KuehlAuslegungRaumtemperatur); Z(a.KuehlVorlaufgrenze, g.KuehlVorlaufgrenze);

        for (int i = 0; i < 4; i++)
        {
            (int? bt, int? bm) = Ferienzeit.TagUndMonat(g.Ferienbeginn[i]);
            (int? et, int? em) = Ferienzeit.TagUndMonat(g.Ferienende[i]);
            I(BeginnTag[i], bt); I(BeginnMonat[i], bm); I(EndeTag[i], et); I(EndeMonat[i], em);
        }
        return n;
    }

    // =====================================================================
    //  Der Hilfe-Assistent — EINE Sicht für Editor und Stammblatt
    // =====================================================================

    /// <summary>
    /// <b>Die Sicht des Assistenten auf diesen Stand</b> (Welle KI‑F3; #465). Jeder Weg geht
    /// über den LEBENDEN Stand und über dieselben Methoden wie die Bedienelemente — die
    /// Bauart zieht die Bauweise nach, die Nutzfläche ebenso, die Randbedingung hält NULL.
    /// Was nur der Dialog kennt (Name, Betriebsart, die Einträge der Klapplisten, die
    /// Satzwahl der Verwaltung), kommt über <paramref name="wege"/> herein.
    /// </summary>
    /// <summary>Die Zonen als Zeilen des Assistenten — je Zugriff neu über dem Arbeitsstand (G6a).</summary>
    private IReadOnlyList<GebaeudeZoneKiZeile> KiZonen()
        => Zonen.Zip(Kennwerte, (z, k) => (z, k))
                .Select((x, i) => new GebaeudeZoneKiZeile((i + 1).ToString(CultureInfo.InvariantCulture), x.z.Bezeichner,
                                                          x.k.Nutzflaeche, x.k.HT, x.k.Bauteile))
                .ToList();

    public GebaeudeKatalogKiSicht KiSicht(GebaeudeKiWege wege)
    {
        return new GebaeudeKatalogKiSicht
        {
            StandLesen = () => Stand,

            NameLesen = wege.NameLesen,
            NameSetzen = wege.NameSetzen,
            BetriebsartLesen = wege.BetriebsartLesen,

            SatzLesen = wege.SatzLesen,
            SatzSetzen = wege.SatzSetzen,
            SatzEintraege = wege.SatzEintraege,

            BauartSetzen = BauartWaehlen,
            WohnflaecheSetzen = NutzflaecheSetzen,
            BaujahrSetzen = BaujahrSetzen,

            TypEintraege = wege.TypEintraege,
            GebaeudeartEintraege = wege.GebaeudeartEintraege,
            BaualtersklasseEintraege = wege.BaualtersklasseEintraege,
            BauartEintraege = wege.BauartEintraege,
            VerwendungEintraege = wege.VerwendungEintraege,

            SollTagLesen = () => Stand.SollTag,
            SollTagSetzen = w => Stand.SollTag = w,
            NachtabsenkungLesen = () => Stand.NachtAbsenkung,
            NachtabsenkungSetzen = w => Stand.NachtAbsenkung = w,
            NachtBeginnLesen = () => Stand.NachtBeginn,
            NachtBeginnSetzen = w => Stand.NachtBeginn = w,
            NachtEndeLesen = () => Stand.NachtEnde,
            NachtEndeSetzen = w => Stand.NachtEnde = w,
            MaxTemperaturLesen = () => Stand.MaxTemperatur,
            MaxTemperaturSetzen = w => Stand.MaxTemperatur = w,
            WochenendabsenkungLesen = () => Stand.WochenendAbsenkung,
            WochenendabsenkungSetzen = w => Stand.WochenendAbsenkung = w,
            SollFerienLesen = () => Stand.SollFerien,
            SollFerienSetzen = w => Stand.SollFerien = w,

            WbvkFensterWandLesen = () => Stand.WbvkFensterWand,
            WbvkFensterWandSetzen = w => Stand.WbvkFensterWand = w,
            WbvkWandDachLesen = () => Stand.WbvkWandDach,
            WbvkWandDachSetzen = w => Stand.WbvkWandDach = w,
            WbvkAussenwandKellerLesen = () => Stand.WbvkAussenwandKeller,
            WbvkAussenwandKellerSetzen = w => Stand.WbvkAussenwandKeller = w,

            AnschlussFensterWandLesen = () => Stand.AnschlussFensterWand,
            AnschlussFensterWandSetzen = w => Stand.AnschlussFensterWand = w,
            AnschlussWandDachLesen = () => Stand.AnschlussWandDach,
            AnschlussWandDachSetzen = w => Stand.AnschlussWandDach = w,
            AnschlussAussenwandKellerLesen = () => Stand.AnschlussAussenwandKeller,
            AnschlussAussenwandKellerSetzen = w => Stand.AnschlussAussenwandKeller = w,

            LuftwechselrateLesen = () => Stand.Luftwechselrate,
            LuftwechselrateSetzen = w => Stand.Luftwechselrate = w,

            RandbedingungLesen = () => Randindex,
            RandbedingungSetzen = RandbedingungWaehlen,
            RandbedingungEintraege = wege.RandbedingungEintraege,
            FerienLesen = () => _kiFerien ??= KiFerien(wege.Ferienname),
            // Stufe G6a: die Zonenliste zum Lesen, Werte aus der EINEN Formel des Kerns.
            ZonenLesen = KiZonen,

            // Stufe AK1: die Gruppe „Wärmeübergabe" über die Wege der Bedienelemente.
            HeizkreisSetzen = HeizkreisSetzen,
            HeizkurveSetzen = HeizkurveSetzen,
            UebergabeArtSetzen = w => KiArtSetzen(w, wege.Texte ?? new GebaeudeHuelleTexte()),
            UebergabeArtEintraege = () => KiArteintraege(wege.Texte ?? new GebaeudeHuelleTexte()),
            SollwertprofilSetzen = w => KiProfilSetzen(w, wege.Texte ?? new GebaeudeHuelleTexte()),

            // E37: der Unterabschnitt „Kühlübergabe" über dieselben Wege.
            KuehluebergabeSetzen = KuehluebergabeSetzen,
            KuehlUebergabeArtSetzen = w => KiKuehlArtSetzen(w, wege.Texte ?? new GebaeudeHuelleTexte()),
            KuehlUebergabeArtEintraege = () => KiKuehlArteintraege(wege.Texte ?? new GebaeudeHuelleTexte())
        };
    }

    /// <summary>Die Kühlübergabeart des Assistenten: der Steuerwert über denselben Weg wie die Klappliste.</summary>
    private string? KiKuehlArtSetzen(string wert, GebaeudeHuelleTexte t)
    {
        string gesucht = (wert ?? "").Trim();
        if (gesucht.Length == 0) gesucht = DbWerte.KUEHLUEBERGABE_IDEAL;
        for (int i = 0; i < Waermeuebergabevorgaben.KuehlArten.Count; i++)
            if (string.Equals(Waermeuebergabevorgaben.KuehlArten[i], gesucht, StringComparison.OrdinalIgnoreCase))
            {
                KuehlArtWaehlen(i);
                return null;
            }
        return string.Format(t.Kuehluebergabe.MeldungArtUnbekannt, gesucht);
    }

    private static IReadOnlyList<KiWahleintrag> KiKuehlArteintraege(GebaeudeHuelleTexte t)
        => Waermeuebergabevorgaben.KuehlArten.Select(a => new KiWahleintrag(a, t.Kuehluebergabe.Artname(a))).ToList();

    /// <summary>Die Übergabeart des Assistenten: der Steuerwert über denselben Weg wie die Klappliste.</summary>
    private string? KiArtSetzen(string wert, GebaeudeHuelleTexte t)
    {
        string gesucht = (wert ?? "").Trim();
        if (gesucht.Length == 0) gesucht = DbWerte.UEBERGABE_IDEAL;
        for (int i = 0; i < Waermeuebergabevorgaben.Arten.Count; i++)
            if (string.Equals(Waermeuebergabevorgaben.Arten[i], gesucht, StringComparison.OrdinalIgnoreCase))
            {
                ArtWaehlen(i);
                return null;
            }
        return string.Format(t.Uebergabe.MeldungArtUnbekannt, gesucht);
    }

    private static IReadOnlyList<KiWahleintrag> KiArteintraege(GebaeudeHuelleTexte t)
        => Waermeuebergabevorgaben.Arten.Select(a => new KiWahleintrag(a, t.Uebergabe.Artname(a))).ToList();

    /// <summary>
    /// Das Zeitprogramm des Assistenten — DERSELBE strenge Leser wie der Kern (H-F10): 168 Werte
    /// oder eine benannte Ablehnung; leer verwirft das Zeitprogramm.
    /// </summary>
    private string? KiProfilSetzen(string text, GebaeudeHuelleTexte t)
    {
        WaermeuebergabeTexte u = t.Uebergabe;
        if (string.IsNullOrWhiteSpace(text)) { ProfilSetzen(null); return null; }

        AnlagenkopplungSchema.Wochenprofil p = AnlagenkopplungSchema.WochenprofilLesen(text);
        switch (p.Befund)
        {
            case AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl:
                return string.Format(u.MeldungProfilWertzahl, p.Gefunden, AnlagenkopplungSchema.WOCHENWERTE);
            case AnlagenkopplungSchema.WochenprofilBefund.KeineZahl:
                return string.Format(u.MeldungProfilKeineZahl, p.Stelle);
        }
        for (int i = 0; i < p.Werte.Length; i++)
            if (p.Werte[i] < Waermeuebergabevorgaben.SOLLWERT_MIN || p.Werte[i] > Waermeuebergabevorgaben.SOLLWERT_MAX)
                return string.Format(u.MeldungProfilWert, i + 1, Zahl(p.Werte[i], 1),
                                     Zahl(Waermeuebergabevorgaben.SOLLWERT_MIN, 0), Zahl(Waermeuebergabevorgaben.SOLLWERT_MAX, 0));
        ProfilSetzen(p.Werte);
        return null;
    }

    private IReadOnlyList<GebaeudeFerienKiZeile>? _kiFerien;

    /// <summary>
    /// Die vier Ferienzeiträume als Zeilen (Welle #458 Stufe 3b) — einmal gebaut, denn die
    /// vier Felderpaare bleiben dieselben; jede Zelle geht über ihren Delegaten auf das Feld,
    /// an dem die Eingabe von Hand hängt.
    /// </summary>
    private IReadOnlyList<GebaeudeFerienKiZeile> KiFerien(Func<int, string>? name)
    {
        return Enumerable.Range(0, 4).Select(i =>
        {
            int n = i;
            return new GebaeudeFerienKiZeile
            {
                ZeitraumLesen = () => name?.Invoke(n) ?? "",
                BeginnTagLesen = () => BeginnTag[n],
                BeginnTagSetzen = w => BeginnTag[n] = w,
                BeginnMonatLesen = () => BeginnMonat[n],
                BeginnMonatSetzen = w => BeginnMonat[n] = w,
                EndeTagLesen = () => EndeTag[n],
                EndeTagSetzen = w => EndeTag[n] = w,
                EndeMonatLesen = () => EndeMonat[n],
                EndeMonatSetzen = w => EndeMonat[n] = w
            };
        }).ToList();
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>Die Beschriftung ohne den nachgestellten Doppelpunkt — als Feldname der Meldung.</summary>
    public static string Feld(string? beschriftung) => (beschriftung ?? "").TrimEnd(' ', ':');

    /// <summary>Eine Zahl in der laufenden Kultur; <c>null</c> wird „—".</summary>
    public static string Zahl(double? wert, int stellen)
        => wert.HasValue ? wert.Value.ToString("N" + stellen, CultureInfo.CurrentCulture) : "—";
}

/// <summary>
/// Die erste verletzte Regel von <see cref="GebaeudeArbeitsstand.Pruefen"/>.
/// </summary>
/// <param name="Meldung">Der Text, den der Dialog meldet.</param>
/// <param name="Bereich">Wo das Feld der Regel steht — der Dialog zeigt es dort.</param>
public sealed record GebaeudePruefbefund(string Meldung, GebaeudePruefbereich Bereich)
{
    /// <summary>Hängt die Regel am zweiten Reiter des Editors (Temperaturen und Ferien)?</summary>
    public bool Temperaturen => Bereich == GebaeudePruefbereich.Ferien;
}

/// <summary>
/// Wo das Feld einer verletzten Regel steht: Der Editor springt auf den Reiter, das Stammblatt
/// der Verwaltung klappt „Alle Daten" auf.
/// </summary>
public enum GebaeudePruefbereich
{
    /// <summary>Name, Kenngrößen, Hülle und Fenster — und ein Feld mit ungültigem Text.</summary>
    Huelle,

    /// <summary>Die Gruppe „Kühlung" (erster Reiter des Editors, „Alle Daten" des Stammblatts).</summary>
    Kuehlung,

    /// <summary>Die Gruppe „Wärmeübergabe" (erster Reiter des Editors, „Alle Daten" des Stammblatts).</summary>
    Waermeuebergabe,

    /// <summary>Raumtemperaturen, Nachtzeit und Ferien (zweiter Reiter des Editors, „Alle Daten" des Stammblatts).</summary>
    Ferien
}

/// <summary>
/// Was der Dialog der Sicht des Assistenten beisteuert, das der Arbeitsstand nicht kennt —
/// Name, Betriebsart, die Einträge der Klapplisten und (in der Verwaltung) die Satzwahl.
/// </summary>
public sealed class GebaeudeKiWege
{
    /// <summary>Liest den Namen des Satzes.</summary>
    public Func<string>? NameLesen { get; init; }

    /// <summary>Setzt den Namen; <c>null</c> = der Name ist hier nicht setzbar.</summary>
    public Action<string>? NameSetzen { get; init; }

    /// <summary>Die Betriebsart des Editors (Bearbeiten, Neu, Admin).</summary>
    public Func<string>? BetriebsartLesen { get; init; }

    /// <summary>Die Satzwahl der Verwaltung — der Bezeichner der Fokuszeile.</summary>
    public Func<string>? SatzLesen { get; init; }

    /// <summary>Wählt einen Satz; Rückgabe: der Grund einer Ablehnung, sonst <c>null</c>.</summary>
    public Func<string, string?>? SatzSetzen { get; init; }

    /// <summary>Die Sätze der Liste als Einträge des Wahlfeldes <c>satz</c>.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    /// <summary>Die Gebäudetypen der Klappliste.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? TypEintraege { get; init; }

    /// <summary>Die Gebäudearten der Klappliste.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? GebaeudeartEintraege { get; init; }

    /// <summary>Die Baualtersklassen (Schlüssel: Listenplatz).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BaualtersklasseEintraege { get; init; }

    /// <summary>Die Bauarten (Schlüssel: Listenplatz).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BauartEintraege { get; init; }

    /// <summary>Die Verwendungen (Schlüssel: Steuerwert).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? VerwendungEintraege { get; init; }

    /// <summary>Die drei Randbedingungen der Bodenplatte (Schlüssel: Listenplatz).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? RandbedingungEintraege { get; init; }

    /// <summary>Der Name des Ferienzeitraums je Zeile (Winter, Ostern, Sommer, Herbst).</summary>
    public Func<int, string>? Ferienname { get; init; }

    /// <summary>Die Texte der VDI-Struktur — für die Namen der Übergabearten und die Ablehnungen der Wärmeübergabe.</summary>
    public GebaeudeHuelleTexte? Texte { get; init; }
}

/// <summary>
/// <b>Die Feldnamen und Meldungen der Prüfung</b> (<see cref="GebaeudeArbeitsstand.Pruefen"/>),
/// die nicht im Bündel <see cref="GebaeudeHuelleTexte"/> stehen — der Rückfall ist der
/// deutsche Text des Katalogeditors; die Hülle füllt es aus den Ressourcen
/// (<c>GebaeudeKatalogHuelle.Prueftexte</c>).
/// </summary>
public sealed class GebaeudePrueftexte
{
    /// <summary><c>GEBK_MSG_NAME_LEER</c>.</summary>
    public string MeldungNameFehlt { get; set; } = "Gebäudenamen eingeben!";

    /// <summary><c>GEBK_MSG_ZAHL</c> — <c>{0}</c> ist der Feldname.</summary>
    public string MeldungZahlFehlt { get; set; } = "Bitte {0} als Zahl eingeben.";

    /// <summary><c>GEBK_MSG_FERIEN_WINTER</c>.</summary>
    public string MeldungFerienWinter { get; set; } = "Die Ferien müssen über die Jahresgrenze gehen!";

    /// <summary><c>GEBK_MSG_FERIEN_OSTERN</c>.</summary>
    public string MeldungFerienOstern { get; set; } = "Fehler: Bei der Eingabe der Osterferien!";

    /// <summary><c>GEBK_MSG_FERIEN_SOMMER</c>.</summary>
    public string MeldungFerienSommer { get; set; } = "Fehler: Bei der Eingabe der Sommerferien!";

    /// <summary><c>GEBK_MSG_FERIEN_HERBST</c>.</summary>
    public string MeldungFerienHerbst { get; set; } = "Fehler: Bei der Eingabe der Herbstferien!";

    /// <summary><c>GEBK_MSG_BAUJAHR</c> — <c>{0}</c> und <c>{1}</c> sind die Grenzen der Spalte.</summary>
    public string MeldungBaujahr { get; set; } = "Das Baujahr muss zwischen {0} und {1} liegen.";

    /// <summary>Das Baujahr — die Beschriftung <c>GEBK_LBL_BAUJAHR</c> ohne Doppelpunkt (Feldname der Fehleingabe).</summary>
    public string FeldBaujahr { get; set; } = "Baujahr";

    /// <summary><c>GEBK_MSG_ENERGIESTANDARD_WOHNEN</c> — <c>{0}</c> ist der Text des Standards (E47).</summary>
    public string MeldungEnergiestandardWohnen { get; set; }
        = "Der Energiestandard „{0}“ gilt nur für Wohngebäude – bitte einen anderen wählen oder die Verwendung ändern.";

    /// <summary><c>GEBK_MSG_NACHTZEIT_NUR_EINE</c> — <c>{0}</c> und <c>{1}</c> sind Beginn und Ende der Vorgabe.</summary>
    public string MeldungNachtzeitNurEine { get; set; }
        = "Bitte Beginn und Ende der Nachtabsenkung beide eingeben oder beide leer lassen (leer = {0} bis {1} Uhr).";

    /// <summary><c>GEBK_MSG_NACHTZEIT_GLEICH</c>.</summary>
    public string MeldungNachtzeitGleich { get; set; } = "Beginn und Ende der Nachtabsenkung dürfen nicht gleich sein.";

    /// <summary><c>GEBK_MSG_NACHTZEIT_BEREICH</c> — <c>{0}</c> und <c>{1}</c> sind die Grenzen der Stunde.</summary>
    public string MeldungNachtzeitBereich { get; set; }
        = "Die Nachtabsenkung beginnt und endet zu einer vollen Stunde von {0} bis {1}.";

    /// <summary>Beginn der Nachtabsenkung — <c>GEBK_LBL_NACHT_BEGINN</c> ohne Doppelpunkt (Feldname der Fehleingabe).</summary>
    public string FeldNachtBeginn { get; set; } = "Nachtabsenkung von";

    /// <summary>Ende der Nachtabsenkung — <c>GEBK_LBL_NACHT_ENDE</c> ohne Doppelpunkt (Feldname der Fehleingabe).</summary>
    public string FeldNachtEnde { get; set; } = "Nachtabsenkung bis";

    /// <summary><c>GEBK_FELD_WOHNFLAECHE</c>.</summary>
    public string FeldWohnflaeche { get; set; } = "Nutzfläche";

    /// <summary><c>GEBK_FELD_FLAECHE_NUTZER</c>.</summary>
    public string FeldFlaecheNutzer { get; set; } = "Fläche / Nutzer";

    /// <summary><c>GEBK_FELD_WAERMEGEWINNE</c>.</summary>
    public string FeldWaermegewinne { get; set; } = "Interne Wärmegewinne";

    /// <summary><c>GEBK_FELD_FENSTERDURCHLASS</c>.</summary>
    public string FeldFensterdurchlassgrad { get; set; } = "Fensterdurchlaßgrad";

    /// <summary><c>GEBK_FELD_RAUMHOEHE</c>.</summary>
    public string FeldRaumhoehe { get; set; } = "Raumhöhe";

    /// <summary>Die Luftwechselrate — die Beschriftung <c>GEBK_LBL_LUFTWECHSEL</c> ohne Doppelpunkt.</summary>
    public string FeldLuftwechsel { get; set; } = "Luftwechselrate";

    /// <summary><c>GEBK_FELD_FF_SUED</c>.</summary>
    public string FeldFFSued { get; set; } = "Fensterfläche Süd";

    /// <summary><c>GEBK_FELD_FF_NORD</c>.</summary>
    public string FeldFFNord { get; set; } = "Fensterfläche Nord";

    /// <summary><c>GEBK_FELD_FL_AUSSENWAND</c>.</summary>
    public string FeldFlaecheAussenwand { get; set; } = "Fläche Außenwand";

    /// <summary><c>GEBK_FELD_DACHFLAECHE</c>.</summary>
    public string FeldDachflaeche { get; set; } = "Gebäude Dachfläche";

    /// <summary><c>GEBK_FELD_GRUNDFLAECHE</c>.</summary>
    public string FeldGrundflaeche { get; set; } = "Gebäude Grundfläche";

    /// <summary><c>GEBK_FELD_SONST_FLAECHEN</c>.</summary>
    public string FeldSonstigeFlaechen { get; set; } = "sonstige Flächen";

    /// <summary><c>GEBK_FELD_U_AUSSENWAND</c>.</summary>
    public string FeldUAussenwand { get; set; } = "U-Wert Außenwand";

    /// <summary><c>GEBK_FELD_U_FENSTER</c>.</summary>
    public string FeldUFenster { get; set; } = "U-Wert Fenster";

    /// <summary><c>GEBK_FELD_U_DACHFLAECHE</c>.</summary>
    public string FeldUDachflaeche { get; set; } = "U-Wert Dachfläche";

    /// <summary><c>GEBK_FELD_U_GRUNDFLAECHE</c>.</summary>
    public string FeldUGrundflaeche { get; set; } = "U-Wert Grundfläche";

    /// <summary><c>GEBK_FELD_U_SONSTIGES</c>.</summary>
    public string FeldUSonstiges { get; set; } = "U-Wert Sonstiges";

    /// <summary>Die Meldung zu einem Schlüssel aus <see cref="Ferienzeit.Pruefen"/>.</summary>
    public string Ferienmeldung(string schluessel) => schluessel switch
    {
        Ferienzeit.MELDUNG_WINTER => MeldungFerienWinter,
        Ferienzeit.MELDUNG_OSTERN => MeldungFerienOstern,
        Ferienzeit.MELDUNG_SOMMER => MeldungFerienSommer,
        _ => MeldungFerienHerbst
    };

    /// <summary>
    /// Der Name, unter dem ein Kennwert des Hüll-Rasters in der Meldung steht — bei U-Werten
    /// die Pflichtnamen, bei den Wärmebrücken Zeile + ψ.
    /// </summary>
    public string Kennwertname(Huellbauteil b, GebaeudeHuelleTexte t) => b switch
    {
        Huellbauteil.Aussenwand => FeldUAussenwand,
        Huellbauteil.Fenster => FeldUFenster,
        Huellbauteil.Dach => FeldUDachflaeche,
        Huellbauteil.Bodenplatte => FeldUGrundflaeche,
        Huellbauteil.Sonstiges => FeldUSonstiges,
        _ => t.Zeile(b) + " ψ"
    };

    /// <summary>Der Name einer Größe des Hüll-Rasters in der Meldung (Fläche bzw. Zeile + L).</summary>
    public string Groessenname(Huellbauteil b, GebaeudeHuelleTexte t) => b switch
    {
        Huellbauteil.Aussenwand => FeldFlaecheAussenwand,
        Huellbauteil.Dach => FeldDachflaeche,
        Huellbauteil.Bodenplatte => FeldGrundflaeche,
        Huellbauteil.Sonstiges => FeldSonstigeFlaechen,
        _ => t.Zeile(b) + " L"
    };
}
