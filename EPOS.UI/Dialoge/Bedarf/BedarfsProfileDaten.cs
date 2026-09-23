namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EINE Zuordnung „Bedarfsprofil ↔ Projekt" (iU9-W9.5) — das plattformfreie Abbild von
/// <c>Z_ProjektProzesswaermeModel</c>, <c>Z_ProjektStromverbraucherModel</c> bzw.
/// <c>Z_ProjektBrauchwasserModel</c>. Die drei Modelle tragen dieselben fünf Felder unter
/// drei Namen; hier steht EIN Satz.
///
/// <para><b><see cref="IdZ"/> ist der Schlüssel.</b> Alle drei Vorläufer suchen die zu
/// entfernende Zeile über Name UND <c>ID_Z</c> — der Name allein ist nicht eindeutig,
/// dasselbe Profil darf einem Projekt mehrfach zugeordnet sein.</para>
/// </summary>
public sealed class BedarfsProfilZeile
{
    /// <summary>Der Schlüssel der Zuordnung.</summary>
    public int IdZ { get; set; }

    /// <summary>Die Stamm-Id des Profils.</summary>
    public int IdStamm { get; set; }

    /// <summary>Der Bezeichner des Profils.</summary>
    public string Name { get; set; } = "";

    /// <summary>Der Jahresverbrauch dieser Zuordnung.</summary>
    public double Summe { get; set; }
}

/// <summary>
/// Der Infoblock zu einem Profil (iU9-W9.5) — <c>SetProzessInfo</c> der drei Vorläufer.
/// </summary>
/// <param name="Name">Der Bezeichner.</param>
/// <param name="Beschreibung">Die Beschreibung aus dem Kopfsatz.</param>
/// <param name="Typ">Der Profiltyp aus dem Kopfsatz.</param>
public sealed record BedarfsProfilInfo(string Name, string Beschreibung, string Typ);

/// <summary>
/// Die Beschriftungen des Zapfprofil-Einstiegs im Bedarfsprofil-Dialog der Ausprägung
/// Brauchwasser (Umsetzungskonzept Zapfprofilgenerator 5.2, ZU4, ZU6) — EIN Parameter statt
/// neun. Der Vorgabewert ist der deutsche Rückfall und gleicht dem Wert der neutralen
/// Ressource; die Hülle füllt das Bündel in der Oberflächensprache.
/// </summary>
public sealed class ZapfprofilEinstiegTexte
{
    /// <summary><c>BPF_BTN_ZAPFPROFIL_BW</c></summary>
    public string Knopf { get; set; } = "Zapfprofil erzeugen…";

    /// <summary><c>ZPG_TITEL</c> — der Titel der Überlagerung.</summary>
    public string Titel { get; set; } = "Brauchwasser-Zapfprofil";

    /// <summary><c>BPF_LBL_RECHENWEG_BW</c></summary>
    public string LabelRechenweg { get; set; } = "Rechenweg Brauchwasser";

    /// <summary><c>BPF_OPT_BESTANDSPROFILE</c></summary>
    public string OptionBestand { get; set; } = "Bestandsprofile";

    /// <summary><c>BPF_OPT_ZAPFPROFIL</c></summary>
    public string OptionZapfprofil { get; set; } = "Zapfprofil";

    /// <summary><c>BPF_HINW_RECHENWEG_BW</c> — die eine Stelle, die „Trinkwarmwasser" ausschreibt.</summary>
    public string HinweisRechenweg { get; set; }
        = "Der Rechenweg bestimmt, woraus der Wärmebedarf für Brauchwasser (Trinkwarmwasser) entsteht: aus den Bestandsprofilen dieser Liste oder aus dem Zapfprofil der Nutzungszonen.";

    /// <summary><c>BPF_HINW_ZAPFPROFILWEG</c></summary>
    public string HinweisZapfprofilweg { get; set; }
        = "Die Bestandsprofile rechnen nicht mit, solange der Rechenweg auf Zapfprofil steht.";

    /// <summary><c>BPF_HINW_ZAPFPROFIL_OHNE_ZONEN</c></summary>
    public string HinweisOhneZonen { get; set; }
        = "Es gibt noch kein Zapfprofil – „Zapfprofil erzeugen…“ legt es an.";

    /// <summary><c>BPF_LBL_LEISTE_ZAPFPROFIL</c></summary>
    public string LeisteZapfprofil { get; set; } = "rechnet den Zapfprofilweg";
}
