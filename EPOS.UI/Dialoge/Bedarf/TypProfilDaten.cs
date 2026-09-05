using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das Wochen-Stundenprofil eines Typs (iU9-W8.3) — 7 × 24 Werte samt Beschreibung, wie
/// sie <c>Tab_Stromverbrauchertyp_STAMM</c>, <c>Tab_Prozesstyp_STAMM</c> und
/// <c>Tab_Brauchwassertyp_STAMM</c> in den Spalten <c>[1]</c>…<c>[168]</c> führen.
///
/// <para><b>Warum ein eigener Typ.</b> Die Komponente kennt die Fachklassen des Kerns
/// nicht; die Hülle bildet zwischen <see cref="TypProfilCtrl"/> und diesem Satz ab.</para>
/// </summary>
public sealed class TypProfilDaten
{
    /// <summary>Sieben Tage.</summary>
    public const int TAGE = 7;

    /// <summary>Vierundzwanzig Stunden je Tag — zusammen die 168 Wochenwerte.</summary>
    public const int STUNDEN = 24;

    /// <summary>Welches der drei Blätter. Sie bestimmt Beschriftungen UND Zieltabelle.</summary>
    public BedarfsArt Art { get; set; } = BedarfsArt.Stromverbraucher;

    /// <summary>Der Name des geladenen Typs; leer, solange keiner gewählt ist.</summary>
    public string Typ { get; set; } = "";

    /// <summary>Freitext des Typs (Spalte <c>Beschreibung</c>).</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Die 7 × 24 ÜBERNOMMENEN Werte — der Stand, den „Speichern" schreibt.</summary>
    public double[,] Werte { get; set; } = new double[TAGE, STUNDEN];

    /// <summary>Eine Kopie der Werte eines Tages.</summary>
    public double[] Tag(int tag)
    {
        var w = new double[STUNDEN];
        if (tag < 0 || tag >= Werte.GetLength(0)) return w;
        for (int s = 0; s < STUNDEN; s++) w[s] = Werte[tag, s];
        return w;
    }

    /// <summary>Die 168 Werte in einer Reihe — die Form, die das Bild braucht.</summary>
    public double[] Wochenreihe()
    {
        var w = new double[TAGE * STUNDEN];
        for (int t = 0; t < TAGE; t++)
            for (int s = 0; s < STUNDEN; s++) w[t * STUNDEN + s] = Werte[t, s];
        return w;
    }
}
