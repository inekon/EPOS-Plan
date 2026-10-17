namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Der Arbeitsstand eines Bauteilaufbaus</b> — Kopf und Schichten, wie sie Stammblatt,
/// „Neu…" und der Assistent bearbeiten, bis „Speichern" bzw. OK sie als EIN Aggregat schreibt
/// (Softwarearchitektur Gebäudesimulation 3.3; Vorbild <c>GebaeudeArbeitsstand</c>).
/// </summary>
/// <remarks>
/// <para><b>Ein Stand, drei Bedienwege.</b> Die Felder des Stammblatts, das Schichtenraster
/// (<see cref="BauteilschichtenFelder"/>) und die Sicht des Assistenten schreiben in DIESELBEN
/// Objekte; was geändert ist, zählt <see cref="GeaenderteFelder"/> gegen den gespeicherten
/// Stand. Die Objekte, die der Wirt hereingereicht hat, bleiben unangetastet — gearbeitet wird
/// an einer Kopie.</para>
/// <para><b>Die Schichtoperationen stehen hier und nicht im Raster:</b> Anlegen, Verschieben,
/// Entfernen und die Wahl des Baustoffs samt Wertekopie — so bedient der Assistent dieselben
/// Wege wie die Knöpfe. Jede Änderung zählt <see cref="Fassung"/> hoch; daran erkennt der Wirt,
/// dass der Summenfuß neu zu holen ist.</para>
/// </remarks>
public sealed class BauteilaufbauArbeitsstand
{
    /// <summary>Beginnt einen Arbeitsstand auf einer Kopie von <paramref name="gespeichert"/>.</summary>
    public BauteilaufbauArbeitsstand(BauteilaufbauDaten? gespeichert = null)
    {
        Gespeichert = gespeichert ?? new BauteilaufbauDaten();
        Arbeit = Gespeichert.Kopie();
    }

    /// <summary>Der gespeicherte Stand — die Vergleichsbasis von „geändert".</summary>
    public BauteilaufbauDaten Gespeichert { get; private set; }

    /// <summary>Der Arbeitsstand — die Felder schreiben hierhin.</summary>
    public BauteilaufbauDaten Arbeit { get; private set; }

    /// <summary>Zählt jede Änderung — der Wirt holt den Summenfuß neu, wenn sie wechselt.</summary>
    public int Fassung { get; private set; }

    /// <summary>Die Schichten des Arbeitsstands, innen → außen.</summary>
    public IReadOnlyList<BauteilschichtDaten> Schichten => Arbeit.Schichten;

    /// <summary>Ist etwas geändert?</summary>
    public bool Geaendert => GeaenderteFelder > 0;

    /// <summary>
    /// Wie viele Angaben vom gespeicherten Stand abweichen: je Kopffeld eins (Name,
    /// Beschreibung, Bauteilart, Quelle), je Schicht, die anders ist oder anders liegt, eins,
    /// dazu der Unterschied der Schichtzahl.
    /// </summary>
    public int GeaenderteFelder
    {
        get
        {
            int n = 0;
            if (!Gleich(Arbeit.Bezeichner, Gespeichert.Bezeichner)) n++;
            if (!Gleich(Arbeit.Beschreibung, Gespeichert.Beschreibung)) n++;
            if (!Gleich(Arbeit.Bauteilart, Gespeichert.Bauteilart)) n++;
            if (!Gleich(Arbeit.Quelle, Gespeichert.Quelle)) n++;
            if (!Gleich(Arbeit.GruppeCode, Gespeichert.GruppeCode)) n++;
            if (!Gleich(Arbeit.KlasseCode, Gespeichert.KlasseCode)) n++;
            n += Arbeit.FensterUnterschiede(Gespeichert);
            n += Arbeit.StrahlungUnterschiede(Gespeichert);
            int gemeinsam = Math.Min(Arbeit.Schichten.Count, Gespeichert.Schichten.Count);
            for (int i = 0; i < gemeinsam; i++)
                if (!Arbeit.Schichten[i].GleicheWerte(Gespeichert.Schichten[i])) n++;
            return n + Math.Abs(Arbeit.Schichten.Count - Gespeichert.Schichten.Count);
        }
    }

    /// <summary>Der Arbeitsstand wird wieder der gespeicherte („Verwerfen").</summary>
    public void Zuruecksetzen()
    {
        Arbeit = Gespeichert.Kopie();
        Fassung++;
    }

    /// <summary>Nach dem Speichern: der geschriebene Stand wird die neue Vergleichsbasis.</summary>
    public void Uebernehmen(BauteilaufbauDaten gespeichert)
    {
        Gespeichert = gespeichert ?? new BauteilaufbauDaten();
        Arbeit = Gespeichert.Kopie();
        Fassung++;
    }

    /// <summary>Meldet eine Eingabe in ein Feld — Kopf oder Schicht.</summary>
    public void Eingabe() => Fassung++;

    /// <summary>„+ Neue Schicht …": hängt eine leere Schicht außen an und liefert sie.</summary>
    public BauteilschichtDaten SchichtAnlegen()
    {
        var s = new BauteilschichtDaten();
        Arbeit.Schichten.Add(s);
        Fassung++;
        return s;
    }

    /// <summary>
    /// Verschiebt die Schicht an <paramref name="index"/> um eine Stelle nach innen
    /// (<paramref name="richtung"/> −1) oder außen (+1); am Rand geschieht nichts.
    /// </summary>
    public bool Verschieben(int index, int richtung)
    {
        int ziel = index + Math.Sign(richtung);
        if (index < 0 || index >= Arbeit.Schichten.Count || ziel < 0 || ziel >= Arbeit.Schichten.Count) return false;
        (Arbeit.Schichten[index], Arbeit.Schichten[ziel]) = (Arbeit.Schichten[ziel], Arbeit.Schichten[index]);
        Fassung++;
        return true;
    }

    /// <summary>
    /// <b>BK-5: „Abschnitt daneben"</b> — macht die Schicht an <paramref name="index"/> zum Abschnitt ihrer Ebene (eine
    /// homogene Schicht wird Abschnitt 1) und legt hinter der Ebene eine Schicht mit der nächsten Abschnittsnummer an, gleich
    /// dick. In einer Ebene stehen die Abschnitte aufsteigend nebeneinander (Kernregel <c>InhomogeneSchichten.Ebenen</c>).
    /// </summary>
    public BauteilschichtDaten? AbschnittAnlegen(int index)
    {
        if (index < 0 || index >= Arbeit.Schichten.Count) return null;
        BauteilschichtDaten s = Arbeit.Schichten[index];
        s.Abschnitt ??= 1;
        int ende = index;
        while (ende + 1 < Arbeit.Schichten.Count && Arbeit.Schichten[ende + 1].Abschnitt is int b && b > Arbeit.Schichten[ende].Abschnitt)
            ende++;
        int nr = Arbeit.Schichten[ende].Abschnitt!.Value + 1;
        var neu = new BauteilschichtDaten { DickeMm = s.DickeMm, Abschnitt = nr, Flaechenanteil = AnteilVon(nr) };
        Arbeit.Schichten.Insert(ende + 1, neu);
        Fassung++;
        return neu;
    }

    /// <summary>BK-5: Der Flächenanteil eines Abschnitts gilt für alle seine Schichten — gesetzt an jeder.</summary>
    public void AnteilSetzen(int index, double? anteil)
    {
        if (index < 0 || index >= Arbeit.Schichten.Count || Arbeit.Schichten[index].Abschnitt is not int nr) return;
        foreach (BauteilschichtDaten s in Arbeit.Schichten.Where(x => x.Abschnitt == nr)) s.Flaechenanteil = anteil;
        Fassung++;
    }

    /// <summary>BK-5: Die Abschnittsnummer einer Schicht; <c>null</c> macht sie homogen. Der Anteil folgt dem Abschnitt.</summary>
    public void AbschnittSetzen(int index, int? nummer)
    {
        if (index < 0 || index >= Arbeit.Schichten.Count) return;
        BauteilschichtDaten s = Arbeit.Schichten[index];
        s.Abschnitt = nummer is > 0 ? nummer : null;
        s.Flaechenanteil = s.Abschnitt is int nr ? AnteilVon(nr) ?? s.Flaechenanteil : null;
        Fassung++;
    }

    /// <summary>
    /// <b>BK-5: Die Summe der Flächenanteile einer inhomogenen Schichtebene</b> — nur an der LETZTEN Schicht der Ebene, sonst
    /// <c>null</c>. Die Ebene folgt der Kernregel (<c>InhomogeneSchichten.Ebenen</c>): Schichten mit Abschnitt, die mit
    /// aufsteigender Nummer aufeinander folgen. Gezählt wird je Abschnitt EIN Anteil; ein fehlender zählt null.
    /// <c>InOrdnung</c> heißt 1 ± <see cref="WindowsFormsApplication1.InhomogeneSchichten.ANTEIL_TOLERANZ"/>. Eine Eingabeprüfung, keine Rechnung.
    /// </summary>
    public (double Summe, bool InOrdnung)? Ebenensumme(int index)
    {
        var s = Arbeit.Schichten;
        if (index < 0 || index >= s.Count || s[index].Abschnitt is not int nr) return null;
        if (index + 1 < s.Count && s[index + 1].Abschnitt is int nach && nach > nr) return null;
        int anfang = index;
        while (anfang > 0 && s[anfang - 1].Abschnitt is int vor && vor < s[anfang].Abschnitt) anfang--;
        double summe = 0.0;
        for (int i = anfang; i <= index; i++) summe += s[i].Flaechenanteil ?? 0.0;
        return (summe, Math.Abs(summe - 1.0) <= WindowsFormsApplication1.InhomogeneSchichten.ANTEIL_TOLERANZ + 1e-12);
    }

    private double? AnteilVon(int nummer)
        => Arbeit.Schichten.FirstOrDefault(x => x.Abschnitt == nummer && x.Flaechenanteil.HasValue)?.Flaechenanteil;

    /// <summary>Nimmt die Schicht an <paramref name="index"/> heraus.</summary>
    public bool Entfernen(int index)
    {
        if (index < 0 || index >= Arbeit.Schichten.Count) return false;
        Arbeit.Schichten.RemoveAt(index);
        Fassung++;
        return true;
    }

    /// <summary>
    /// <b>Die Wahl des Baustoffs einer Schicht</b>: Die Schicht übernimmt λ, ρ und c_p des
    /// Stoffes als KOPIE (danach überschreibbar); <c>null</c> oder ein unbekannter Stoff heißt
    /// freie Eingabe — die Werte bleiben stehen, die Zuordnung fällt.
    /// </summary>
    public void StoffWaehlen(int index, int? idBaustoff, IReadOnlyList<BaustoffWahl>? baustoffe)
    {
        if (index < 0 || index >= Arbeit.Schichten.Count) return;
        BauteilschichtDaten s = Arbeit.Schichten[index];
        BaustoffWahl? stoff = idBaustoff is int id ? baustoffe?.FirstOrDefault(b => b.Id == id) : null;
        if (stoff is null)
        {
            s.IdBaustoff = null;
        }
        else
        {
            s.IdBaustoff = stoff.Id;
            s.Lambda = stoff.Lambda;
            s.Rho = stoff.Rho;
            s.Cp = stoff.Cp;
        }
        Fassung++;
    }

    private static bool Gleich(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.Ordinal);
}
