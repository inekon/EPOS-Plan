using System.Globalization;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Anzeigetexte des Dialogs „BHKW-Wirtschaftlichkeit" (Etappe B5b).
///
/// <para><b>Warum ein Schluesselzugriff und kein <c>@Resource.BHW_TITEL</c>.</b> Die
/// 98 Schluessel <c>BHW_*</c> stehen seit Etappe B5 in
/// <c>EPOS.Kern/MyResource/Resource.resx</c> und <c>Resource.en-US.resx</c> — aber
/// NICHT in der erzeugten <c>Resource.Designer.cs</c>. Stark typisierte
/// Eigenschaften gibt es fuer sie deshalb nicht; sie entstehen erst, wenn Visual
/// Studio die Designer-Datei neu erzeugt. Bis dahin ist der Weg ueber den
/// <see cref="System.Resources.ResourceManager"/> derselbe, den auch die erzeugten
/// Eigenschaften nehmen (<c>GetString(name, resourceCulture)</c>) — mit demselben
/// Katalog, denselben Schluesseln und denselben Satellitendateien.</para>
///
/// <para><b>Der deutsche Rueckfall bleibt</b> (Konzept § 6.4, Rueckfallmuster der
/// Etappe B5): Fehlt ein Schluessel, steht der deutsche Wortlaut. Genau so hat es
/// die WinForms-Fassung gehalten; ein neuer Text im Blazor-Layout ist damit
/// sofort lesbar und wird beim naechsten resx-Sammelnachtrag zweisprachig.</para>
/// </summary>
public static class BhwTexte
{
    /// <summary>Anzeigetext zu einem Schluessel; fehlt er, gilt der deutsche Rueckfall.</summary>
    public static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel, Resource.Culture); }
        catch { /* ein fehlender Katalog darf keinen Dialog mitreissen */ }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>Zahlenformat der Anzeige — dieselbe Kultur wie Bericht und Reiter.</summary>
    public static CultureInfo Kultur
    {
        get
        {
            try { return BerichtTexte.Kultur; }
            catch { return CultureInfo.CurrentCulture; }
        }
    }

    /// <summary>Kurzdatum wie in der Anlagentabelle; <c>null</c> wird zum Gedankenstrich.</summary>
    public static string Kurzdatum(DateOnly? d) =>
        d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue).ToString("d", Kultur) : "—";
}

/// <summary>
/// Ein Eintrag einer Auswahlliste: sprachneutraler Steuerwert fuer die Datenbank,
/// Anzeigetext fuer den Bildschirm (Drei-Schichten-Regel).
///
/// <para><see cref="Nummer"/> ist die Stellung in der Liste. Das
/// <see cref="EPOS.UI.Standards.Auswahlfeld"/> des Hauses fuehrt seine Eintraege
/// ueber eine ganzzahlige Id; der Steuerwert ist aber Text. Die Nummer ist die
/// Bruecke zwischen beidem — sie steht nirgends in der Datenbank.</para>
/// </summary>
/// <param name="Nummer">Stellung in der Liste (die Id des Auswahlfeldes).</param>
/// <param name="Wert">Steuerwert aus <c>DbWerte</c>; leer = „kein eigener Wert".</param>
/// <param name="Text">Anzeigetext.</param>
public sealed record Steuerwahl(int Nummer, string Wert, string Text);

/// <summary>
/// Die Auswahllisten des Dialogs — wortgleich aus der WinForms-Fassung
/// <c>Views/Wirtschaftlichkeit/Form_BhkwWirtschaftlichkeit.cs</c> uebernommen
/// (Steuerwerte aus <c>DbWerte</c>, Anzeigetexte ueber <see cref="BhwTexte.T"/>).
/// </summary>
public static class BhkwWahlen
{
    /// <param name="mitOffen">true = der erste Eintrag heisst „(nicht angegeben)";
    /// der Steuerwert ist in beiden Faellen LEER — das ist der Zustand jeder
    /// Bestandszeile.</param>
    public static IReadOnlyList<Steuerwahl> Anlagenart(bool mitOffen) => Nummeriere(
        new (string Wert, string Text)[]
        {
            ("", mitOffen
                ? BhwTexte.T("BHW_W_OFFEN", "(nicht angegeben)")
                : BhwTexte.T("BHW_W_ART_LEER", "(nicht erfasst — gilt als Neuanlage)")),
            (DbWerte.KWKG_ANLAGENART_NEU,
                BhwTexte.T("BHW_W_ART_NEU", "neue Anlage (§ 8 Abs. 1)")),
            (DbWerte.KWKG_ANLAGENART_MODERNISIERT,
                BhwTexte.T("BHW_W_ART_MOD", "modernisiert (§ 8 Abs. 2)")),
            (DbWerte.KWKG_ANLAGENART_NACHGERUESTET,
                BhwTexte.T("BHW_W_ART_NACH", "nachgerüstet (§ 8 Abs. 3)"))
        });

    public static IReadOnlyList<Steuerwahl> Eigenfall(bool mitOffen)
    {
        var l = new List<(string, string)>();
        if (mitOffen) l.Add(("", BhwTexte.T("BHW_W_OFFEN", "(nicht angegeben)")));
        l.Add((DbWerte.KWKG_EIGENFALL_KEINER,
               BhwTexte.T("BHW_W_FALL_KEINER", "kein Tatbestand (kein Eigenstromzuschlag)")));
        l.Add((DbWerte.KWKG_EIGENFALL_NR1,
               BhwTexte.T("BHW_W_FALL_NR1", "Nr. 1 — Anlage bis 100 kW")));
        l.Add((DbWerte.KWKG_EIGENFALL_NR2,
               BhwTexte.T("BHW_W_FALL_NR2", "Nr. 2 — Kundenanlage / geschl. Netz")));
        l.Add((DbWerte.KWKG_EIGENFALL_NR3,
               BhwTexte.T("BHW_W_FALL_NR3", "Nr. 3 — stromkostenintensiv")));
        return Nummeriere(l.ToArray());
    }

    /// <param name="jeAnlage">true = mit dem ersten Eintrag „(Projektwert)"; an der
    /// Anlage heisst leer „kein eigener Wert" (B3a).</param>
    public static IReadOnlyList<Steuerwahl> Energiesteuer(bool jeAnlage)
    {
        var l = new List<(string, string)>();
        if (jeAnlage) l.Add(("", BhwTexte.T("BHW_W_PROJEKTWERT", "(Projektwert)")));
        l.Add((DbWerte.ENERGIESTEUER_WAHL_KEINE, BhwTexte.T("BHW_W_ES_KEINE", "keine")));
        l.Add((DbWerte.ENERGIESTEUER_WAHL_53,
               BhwTexte.T("BHW_W_ES_53", "§ 53 EnergieStG (Formular 1131)")));
        l.Add((DbWerte.ENERGIESTEUER_WAHL_53A,
               BhwTexte.T("BHW_W_ES_53A", "§ 53a Abs. 5 EnergieStG (1135)")));
        l.Add((DbWerte.ENERGIESTEUER_WAHL_54,
               BhwTexte.T("BHW_W_ES_54", "§ 54 EnergieStG (Formular 1450)")));
        return Nummeriere(l.ToArray());
    }

    public static IReadOnlyList<Steuerwahl> Aufteilung(bool jeAnlage)
    {
        var l = new List<(string, string)>();
        if (jeAnlage) l.Add(("", BhwTexte.T("BHW_W_PROJEKTWERT", "(Projektwert)")));
        l.Add((DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF,
               BhwTexte.T("BHW_W_AUF_VOLL", "voller BHKW-Brennstoff (§ 53 Abs. 2)")));
        l.Add((DbWerte.AUFTEILUNG_ENERGETISCH,
               BhwTexte.T("BHW_W_AUF_ENERGETISCH", "energetisch (konservativ)")));
        return Nummeriere(l.ToArray());
    }

    public static IReadOnlyList<Steuerwahl> Unternehmensart() => Nummeriere(
        new (string, string)[]
        {
            (DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE,
                BhwTexte.T("BHW_W_UA_KEIN", "kein produzierendes Gewerbe")),
            (DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                BhwTexte.T("BHW_W_UA_PROD", "produzierendes Gewerbe")),
            (DbWerte.UNTERNEHMENSART_LAND_FORST,
                BhwTexte.T("BHW_W_UA_LAND", "Land- und Forstwirtschaft"))
        });

    /// <summary>K3 = a: die beiden Modi des § 9 Abs. 1 Nr. 3 — reine ANZEIGE ohne
    /// Persistenz. Die Steuerwerte stehen bewusst nicht in <c>DbWerte</c>: Es gibt
    /// bis B6 keine Spalte, in die sie geschrieben wuerden, und <c>DbWerte</c> sammelt
    /// ausschliesslich Werte, die wirklich in der Datenbank stehen.</summary>
    public static IReadOnlyList<Steuerwahl> Befreiungsmodus() => Nummeriere(
        new (string, string)[]
        {
            ("AUSWEIS", BhwTexte.T("BHW_W_MODUS_AUSWEIS", "Ausweis (nicht im Kapitalwert)")),
            ("ERLOES", BhwTexte.T("BHW_W_MODUS_ERLOES", "Erlös (im Kapitalwert)"))
        });

    /// <summary>Die Nummer eines Steuerwertes in einer Liste; 0, wenn er fehlt —
    /// wie <c>Waehle(ComboBox, wert)</c> der WinForms-Fassung, die dann auf den
    /// ersten Eintrag zurueckfiel.</summary>
    public static int NummerZu(IReadOnlyList<Steuerwahl> liste, string? wert)
    {
        foreach (Steuerwahl w in liste)
            if (string.Equals(w.Wert, wert ?? "", StringComparison.Ordinal)) return w.Nummer;
        return 0;
    }

    /// <summary>Der Steuerwert zu einer Nummer; leer, wenn die Nummer unbekannt ist.</summary>
    public static string WertZu(IReadOnlyList<Steuerwahl> liste, int? nummer)
    {
        if (!nummer.HasValue) return "";
        foreach (Steuerwahl w in liste)
            if (w.Nummer == nummer.Value) return w.Wert;
        return "";
    }

    /// <summary>Der Anzeigetext zu einem Steuerwert (Spalte „Anlagenart" der Tabelle).</summary>
    public static string TextZu(IReadOnlyList<Steuerwahl> liste, string? wert)
    {
        foreach (Steuerwahl w in liste)
            if (string.Equals(w.Wert, wert ?? "", StringComparison.Ordinal)) return w.Text;
        return "";
    }

    /// <summary>Die Eintraege eines <see cref="EPOS.UI.Standards.Auswahlfeld"/> zu einer Liste.</summary>
    public static IReadOnlyList<(int Id, string Text)> Eintraege(IReadOnlyList<Steuerwahl> liste)
    {
        var l = new List<(int, string)>(liste.Count);
        foreach (Steuerwahl w in liste) l.Add((w.Nummer, w.Text));
        return l;
    }

    private static IReadOnlyList<Steuerwahl> Nummeriere((string Wert, string Text)[] roh)
    {
        var l = new List<Steuerwahl>(roh.Length);
        for (int i = 0; i < roh.Length; i++) l.Add(new Steuerwahl(i, roh[i].Wert, roh[i].Text));
        return l;
    }
}

/// <summary>
/// Wohin der Anwender aus dem Dialog springen wollte.
///
/// <para><b>Warum ein Sprungwunsch und kein Aufruf.</b> Beide Ziele sind
/// Sichten desselben Tarifdialogs. Zu Etappe B5b war das eine WinForms-Maske
/// (<c>Form_Tarifstruktur</c>), fuer die es kein Muster gab, sie aus einem
/// Blazor-Dialog heraus zu oeffnen; seit iU9-W2.2 gibt es dafuer die
/// <c>Sprungbruecke</c> — der Tarifdialog ist mit iU9-W2.3 aber SELBST eine
/// Razor-Komponente geworden, und zwei WebViews uebereinander sind Risiko R2
/// des Wellenplans. Der Sprung bleibt deshalb NACHGELAGERT: Die Komponente
/// meldet den Wunsch im Ergebnis; die Huelle oeffnet das Ziel, nachdem der
/// Dialog geschlossen ist, und bringt ihn danach zurueck. Ein Fenster wird
/// daraus erst mit dem Baustein <c>Ueberlagerung</c> (Welle 4).</para>
/// </summary>
public enum BhkwSprung
{
    /// <summary>Kein Sprung — der Dialog wurde einfach geschlossen.</summary>
    Keiner,

    /// <summary>BHKW-Sicht der Tarifstruktur (<c>TarifSicht.Bhkw</c>).</summary>
    BhkwTarif,

    /// <summary>Einkaufsseite der Tarifstruktur (<c>TarifSicht.Strombezug</c>).</summary>
    Strombezug
}

/// <summary>
/// Was der Dialog beim Schliessen meldet.
/// </summary>
/// <param name="Gespeichert">true, wenn mindestens einmal gespeichert wurde — dann
/// rechnet die Wirtschaftlichkeitsseite neu (Bestandsverhalten von
/// <c>Form_BhkwWirtschaftlichkeit.Gespeichert</c>).</param>
/// <param name="Sprung">Das gewuenschte Folgefenster; <see cref="BhkwSprung.Keiner"/>,
/// wenn keines gewuenscht ist.</param>
public sealed record BhkwWirtschaftlichkeitErgebnis(bool Gespeichert, BhkwSprung Sprung);
