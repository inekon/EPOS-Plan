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
    /// <remarks>An der Anlage heisst der Leereintrag „(bitte wählen)": Eine fehlende
    /// Anlagenart ist NICHT gepflegt und gilt nicht als Neuanlage — § 8 KWKG leitet ohne
    /// sie kein Kontingent ab (Konzept Wirtschaftlichkeit § 6.3 Nr. 30, Register R‑NR
    /// Nr. 30: „der Dialog zeigt ‚bitte wählen'").</remarks>
    public static IReadOnlyList<Steuerwahl> Anlagenart(bool mitOffen) => Nummeriere(
        new (string Wert, string Text)[]
        {
            ("", mitOffen
                ? BhwTexte.T("BHW_W_OFFEN", "(nicht angegeben)")
                : BhwTexte.T("BHW_W_ART_LEER", "(bitte wählen)")),
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

    /// <summary>ETAPPE B6: die beiden Modi des § 9 Abs. 1 Nr. 3 StromStG. Die
    /// Steuerwerte stehen seit Schemaschritt 88 in <c>DbWerte</c> — sie gehen in
    /// <c>Tab_ProjektWirtschaftlichkeit.Stromst_Befreiung_Modus</c>. AUSWEIS steht
    /// zuerst: Es ist die Vorgabe, und <c>NummerZu</c> faellt auf den ersten Eintrag
    /// zurueck, wenn nichts gepflegt ist.</summary>
    public static IReadOnlyList<Steuerwahl> Befreiungsmodus() => Nummeriere(
        new (string, string)[]
        {
            (DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS,
                BhwTexte.T("BHW_W_MODUS_AUSWEIS", "Ausweis (nicht im Kapitalwert)")),
            (DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES,
                BhwTexte.T("BHW_W_MODUS_ERLOES", "Erlös (im Kapitalwert)"))
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
/// <para><b>Ein Ziel.</b> Der zweite Sprung „Strombezug…" in die Einkaufsseite der
/// Tarifstruktur ist mit Q11 entfallen: Den Zeitzonentarif gibt es nicht mehr, und
/// die Leistungspreis-Staffel pflegt der Stromträger in der Kostenverwaltung.</para>
///
/// <para><b>Warum ein Sprungwunsch und kein Aufruf.</b> Das Ziel ist eine
/// Sicht des Tarifdialogs. Zu Etappe B5b war das eine WinForms-Maske
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
    BhkwTarif
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

/// <summary>
/// Der ARBEITSSTAND einer Anlagenzeile — genau die Felder, die der Dialog pflegt:
/// die elf aus K7 und der Kostenanteil (BK1), dazu seit Etappe E7c Kennzeichen und
/// Stromkennzahl aus der Überlagerung „Sätze und Herkunft".
///
/// <para><b>Warum es ihn gibt.</b> Der Dialog traegt OK und Abbrechen; wer
/// Abbrechen anbietet, darf vorher nichts geschrieben haben. Die Eingaben
/// stehen deshalb bis zum OK hier und nicht in der geladenen Zeile. Erst
/// <see cref="Anwenden"/> legt sie auf die Zeile, und erst danach schreibt der
/// Wirt sie; Abbrechen laesst beides unberuehrt.</para>
///
/// <para><b>Warum elf Felder und keine Kopie der ganzen Zeile.</b> Eine Kopie
/// muesste jedes Feld mitfuehren — auch die, die der Dialog nie anfasst
/// (Bezeichner, Leistung, Brennstoff, Projektzuordnung); ein beim Kopieren
/// vergessenes Feld ginge beim Schreiben verloren. Der Stand fuehrt deshalb
/// ausschliesslich das, was der Dialog pflegt.</para>
/// </summary>
public sealed class BhkwAnlagenstand
{
    /// <summary>Bestell-/Genehmigungsdatum (Feldkarte 1.7).</summary>
    public DateTime? Stichtag;

    /// <summary>Inbetriebnahmedatum (1.8).</summary>
    public DateTime? Inbetriebnahme;

    /// <summary>Anlagenart, Steuerwert <c>DbWerte.KWKG_ANLAGENART_*</c> (1.9).</summary>
    public string Anlagenart = "";

    /// <summary>Tatbestand des § 6 Abs. 3, Steuerwert <c>DbWerte.KWKG_EIGENFALL_*</c> (1.10).</summary>
    public string Eigenfall = "";

    /// <summary>Ueberschreibwert des Einspeisesatzes [ct/kWh] (1.11); <c>null</c> = Projektsatz.</summary>
    public double? SatzEinspCt;

    /// <summary>Ueberschreibwert des Eigenstromsatzes [ct/kWh] (1.12); <c>null</c> = Projektsatz.</summary>
    public double? SatzEigenCt;

    /// <summary>Vbh-Kontingent [h] (1.13); <c>null</c> = Projektwert.</summary>
    public double? VbhKontingent;

    /// <summary>Jahresdeckel-Override [h/a] (1.14); <c>null</c> = Staffel.</summary>
    public double? VbhDeckel;

    /// <summary>ETAPPE BK1 — Anteil an den Neuherstellungskosten dieser Anlage [%]
    /// (§ 8 Abs. 2/3); <c>null</c> oder 0 = nicht gepflegt. Er waehlt zusammen mit
    /// <see cref="Anlagenart"/> die Kontingentstufe der Anlage.</summary>
    public double? Kostenanteil;

    /// <summary>Entlastungsnorm dieser Anlage (1.15); leer = Projektwert.</summary>
    public string EnergiesteuerWahl = "";

    /// <summary>Aufteilungsmethode dieser Anlage (1.16); leer = Projektwert.</summary>
    public string AufteilungMethode = "";

    /// <summary>Hilfsenergieanteil [%] (1.17); 0 = keine Hilfsenergie (BF4).</summary>
    public double? HilfsenergieAnteil;

    /// <summary>ETAPPE E7c (Befund K‑1, Entscheid E7‑Q2) — Kennzeichen „Vorrichtung zur
    /// Abwärmeabfuhr": true = KWK-Strom nach Fall 2 des § 2 Nr. 16 KWKG. Gepflegt in der
    /// Überlagerung „Sätze und Herkunft".</summary>
    public bool Abwaermeabfuhr;

    /// <summary>ETAPPE E7c — die eigene Stromkennzahl σ; <c>null</c> = keine, dann gilt
    /// P_el ÷ P_th der Gerätezeile (Vorschlag aus <c>KwkStromRechner</c>).</summary>
    public double? Stromkennzahl;

    /// <summary>Der Stand, wie die Zeile geladen wurde.</summary>
    public static BhkwAnlagenstand Aus(KwkgAnlagenAngabe a) => new BhkwAnlagenstand
    {
        Stichtag = a.Stichtag,
        Inbetriebnahme = a.Inbetriebnahme,
        Anlagenart = a.Anlagenart ?? "",
        Eigenfall = a.Eigenfall ?? "",
        SatzEinspCt = a.SatzEinspCt,
        SatzEigenCt = a.SatzEigenCt,
        VbhKontingent = a.VbhKontingent,
        VbhDeckel = a.VbhDeckel,
        Kostenanteil = a.Kostenanteil,
        EnergiesteuerWahl = a.EnergiesteuerWahl ?? "",
        AufteilungMethode = a.AufteilungMethode ?? "",
        HilfsenergieAnteil = a.HilfsenergieAnteil,
        Abwaermeabfuhr = a.Abwaermeabfuhr,
        Stromkennzahl = a.Stromkennzahl
    };

    /// <summary>
    /// Trägt die geladene Zeile <paramref name="a"/> noch WERTGLEICH das, was
    /// der Arbeitsstand führt? Dann hat der Anwender an dieser Zeile nichts
    /// geändert, und ein Schreiben wäre folgenlos — schlimmer: In einer
    /// Mehrbenutzerlage überschriebe es die Änderung eines anderen mit dem
    /// eigenen geladenen Stand. Der Sprungknopf fragt deshalb hier, statt
    /// sich auf ein Merkflag zu verlassen: Ein Flag kippt schon bei einem
    /// Fokuswechsel oder einem Neuzeichnen, ein Wertvergleich nicht.
    /// </summary>
    /// <summary>ETAPPE E7c2 (U22): eine Kopie — der Zwischenstand der Überlagerung „Sätze
    /// und Herkunft", der erst mit „Übernehmen" auf den Arbeitsstand geht. Alle Felder
    /// sind Werte oder Zeichenketten, eine flache Kopie genügt.</summary>
    public BhkwAnlagenstand Kopie() => (BhkwAnlagenstand)MemberwiseClone();

    public bool Gleicht(KwkgAnlagenAngabe a)
        => Stichtag == a.Stichtag
        && Inbetriebnahme == a.Inbetriebnahme
        && Anlagenart == (a.Anlagenart ?? "")
        && Eigenfall == (a.Eigenfall ?? "")
        && SatzEinspCt == a.SatzEinspCt
        && SatzEigenCt == a.SatzEigenCt
        && VbhKontingent == a.VbhKontingent
        && VbhDeckel == a.VbhDeckel
        && Kostenanteil == a.Kostenanteil
        && EnergiesteuerWahl == (a.EnergiesteuerWahl ?? "")
        && AufteilungMethode == (a.AufteilungMethode ?? "")
        && HilfsenergieAnteil == a.HilfsenergieAnteil
        && Abwaermeabfuhr == a.Abwaermeabfuhr
        && Stromkennzahl == a.Stromkennzahl;

    /// <summary>Den Stand auf die geladene Zeile legen — NUR im OK-Weg.</summary>
    public void Anwenden(KwkgAnlagenAngabe a)
    {
        a.Abwaermeabfuhr = Abwaermeabfuhr;
        a.Stromkennzahl = Stromkennzahl;
        a.Stichtag = Stichtag;
        a.Inbetriebnahme = Inbetriebnahme;
        a.Anlagenart = Anlagenart;
        a.Eigenfall = Eigenfall;
        a.SatzEinspCt = SatzEinspCt;
        a.SatzEigenCt = SatzEigenCt;
        a.VbhKontingent = VbhKontingent;
        a.VbhDeckel = VbhDeckel;
        a.Kostenanteil = Kostenanteil;
        a.EnergiesteuerWahl = EnergiesteuerWahl;
        a.AufteilungMethode = AufteilungMethode;
        a.HilfsenergieAnteil = HilfsenergieAnteil;
    }
}

/// <summary>
/// Der ARBEITSSTAND der Projektvorgaben — genau die achtzehn Felder der Gruppen
/// 2 bis 4, die der Dialog pflegt.
///
/// <para>Derselbe Grund und dieselbe Bauart wie bei
/// <see cref="BhkwAnlagenstand"/>: Bis zum OK steht die Eingabe hier, danach
/// legt <see cref="Anwenden"/> sie auf den geladenen Parametersatz. Alles
/// Uebrige dieses Satzes — Zins, Betrachtungszeitraum, CO₂-Preis und was sonst
/// andere Masken pflegen — bleibt unveraendert stehen und geht wertgleich in
/// die Zeile zurueck.</para>
/// </summary>
public sealed class BhkwVorgabenstand
{
    /// <summary>
    /// Einspeiseverguetung KWK-Strom [EUR/kWh]; <c>null</c> = nicht gepflegt
    /// (AUFTRAG #325, Anwenderwunsch 17.09.2026).
    ///
    /// <para><b>0 heisst „nicht gepflegt".</b> Die Regel zieht unveraendert mit:
    /// Eine gepflegte 0 waere die Aussage „der eingespeiste KWK-Strom bringt
    /// nichts ein" und laesst sich von einem nie angefassten Feld an dieser Zahl
    /// nicht unterscheiden. Deshalb <c>null</c> — und der Rechenweg sagt es
    /// (<c>StromPreisCtrl.GepflegtCtKwh</c>).</para>
    /// </summary>
    public double? EinspeiseverguetungKwk;

    /// <summary>
    /// ETAPPE E9b (Konzept § 2.11.5, Pflege): die Einspeisevergütung KWK des Szenarios
    /// GÜNSTIG [EUR/kWh] — gepflegt über den ±-Knopf am Feld; <c>null</c> = wie Erwartet.
    /// Sie steht im Szenariosatz des Parametersatzes
    /// (<see cref="SzenarioSatz.EinspeiseverguetungKwk"/>, Schemaschritt 118) und reist wie
    /// die übrigen Vorgaben im Arbeitsstand bis zum OK-Weg.
    /// </summary>
    public double? EinspeiseverguetungKwkBest;

    /// <summary>ETAPPE E9b: dasselbe für das Szenario UNGÜNSTIG.</summary>
    public double? EinspeiseverguetungKwkWorst;

    // ETAPPE BK1a: Die vier KWKG-Rechengrößen des Projekts und die zwei
    // Einordnungen (Tatbestand, Anlagenart) sind mit Schemaschritt 90 entfallen,
    // der Kostenanteil mit Schemaschritt 91 — alle drei gehören der Anlage. Der
    // Rundtrip führt sie deshalb nicht mehr.

    /// <summary>Abschlag Negativstunden [%] (2.5).</summary>
    public double KwkgAbschlagNegativ;

    /// <summary>Pauschale § 9 KWKG (2.9).</summary>
    public bool KwkgPauschalmodus;

    /// <summary>Stichtag, Vorgabe je Anlage (2.10).</summary>
    public DateTime? KwkgStichtag;

    /// <summary>Inbetriebnahme, Vorgabe je Anlage (2.11).</summary>
    public DateTime? KwkgInbetriebnahme;

    /// <summary>Energiesteuerentlastung (3.1).</summary>
    public string EnergiesteuerWahl = "";

    /// <summary>Brennstoff auf Strom/Waerme (3.2).</summary>
    public string AufteilungMethode = "";

    /// <summary>Jahresnutzungsgrad [%] (3.3); <c>null</c> = nicht erfasst.</summary>
    public double? Jahresnutzungsgrad;

    /// <summary>Unternehmensart (4.1).</summary>
    public string Unternehmensart = "";

    /// <summary>Raeumlicher Zusammenhang (4.2).</summary>
    public bool RaeumlicherZusammenhang;

    /// <summary>Hocheffizienz nachgewiesen (4.3).</summary>
    public bool HocheffizienzNachweis;

    /// <summary>Modus § 9 Abs. 1 Nr. 3 StromStG (4.4) — Steuerwert aus
    /// <c>DbWerte.STROMST_BEFREIUNG_MODUS_*</c>; leer heisst wie NULL: AUSWEIS.</summary>
    public string StromsteuerBefreiungModus = "";

    /// <summary>Der Stand, wie der Parametersatz geladen wurde.</summary>
    public static BhkwVorgabenstand Aus(WirtschaftlichkeitParameter p) => new BhkwVorgabenstand
    {
        EinspeiseverguetungKwk = p.EinspeiseverguetungKWK,
        EinspeiseverguetungKwkBest = p.SatzBest?.EinspeiseverguetungKwk,
        EinspeiseverguetungKwkWorst = p.SatzWorst?.EinspeiseverguetungKwk,
        KwkgAbschlagNegativ = p.KwkgAbschlagNegativ,
        KwkgPauschalmodus = p.KwkgPauschalmodus,
        KwkgStichtag = p.KwkgStichtag,
        KwkgInbetriebnahme = p.KwkgInbetriebnahme,
        EnergiesteuerWahl = p.EnergiesteuerWahl ?? "",
        AufteilungMethode = p.AufteilungMethode ?? "",
        Jahresnutzungsgrad = p.Jahresnutzungsgrad,
        Unternehmensart = p.Unternehmensart ?? "",
        RaeumlicherZusammenhang = p.RaeumlicherZusammenhang,
        HocheffizienzNachweis = p.HocheffizienzNachweis,
        StromsteuerBefreiungModus = p.StromsteuerBefreiungModus ?? ""
    };

    /// <summary>
    /// Trägt der geladene Parametersatz <paramref name="p"/> noch WERTGLEICH
    /// das, was der Arbeitsstand führt? Dann hat der Anwender an den Vorgaben
    /// nichts geändert — derselbe Grund wie bei
    /// <see cref="BhkwAnlagenstand.Gleicht"/>: Wer nur nachschlägt, soll keinen
    /// Schreibzugriff auslösen, und ein Schreiben ohne Änderung überschriebe in
    /// einer Mehrbenutzerlage fremde Änderungen mit dem eigenen geladenen Stand.
    /// </summary>
    /// <summary>ETAPPE E7c2 (U22): eine Kopie für den Zwischenstand der Überlagerung.</summary>
    public BhkwVorgabenstand Kopie() => (BhkwVorgabenstand)MemberwiseClone();

    public bool Gleicht(WirtschaftlichkeitParameter p)
        => EinspeiseverguetungKwk == p.EinspeiseverguetungKWK
        && EinspeiseverguetungKwkBest == p.SatzBest?.EinspeiseverguetungKwk
        && EinspeiseverguetungKwkWorst == p.SatzWorst?.EinspeiseverguetungKwk
        && KwkgAbschlagNegativ == p.KwkgAbschlagNegativ
        && KwkgPauschalmodus == p.KwkgPauschalmodus
        && KwkgStichtag == p.KwkgStichtag
        && KwkgInbetriebnahme == p.KwkgInbetriebnahme
        && EnergiesteuerWahl == (p.EnergiesteuerWahl ?? "")
        && AufteilungMethode == (p.AufteilungMethode ?? "")
        && Jahresnutzungsgrad == p.Jahresnutzungsgrad
        && Unternehmensart == (p.Unternehmensart ?? "")
        && RaeumlicherZusammenhang == p.RaeumlicherZusammenhang
        && HocheffizienzNachweis == p.HocheffizienzNachweis
        && StromsteuerBefreiungModus == (p.StromsteuerBefreiungModus ?? "");

    /// <summary>Den Stand auf den geladenen Parametersatz legen — NUR im OK-Weg.</summary>
    public void Anwenden(WirtschaftlichkeitParameter p)
    {
        p.EinspeiseverguetungKWK = EinspeiseverguetungKwk;
        // ETAPPE E9b: das Szenariopaar in die zwei Szenariosätze — die übrigen Felder der
        // Sätze pflegt der Parameterdialog, sie bleiben unberührt.
        if (p.SatzBest is null) p.SatzBest = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);
        if (p.SatzWorst is null) p.SatzWorst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
        p.SatzBest.EinspeiseverguetungKwk = EinspeiseverguetungKwkBest;
        p.SatzWorst.EinspeiseverguetungKwk = EinspeiseverguetungKwkWorst;
        p.KwkgAbschlagNegativ = KwkgAbschlagNegativ;
        p.KwkgPauschalmodus = KwkgPauschalmodus;
        p.KwkgStichtag = KwkgStichtag;
        p.KwkgInbetriebnahme = KwkgInbetriebnahme;
        p.EnergiesteuerWahl = EnergiesteuerWahl;
        p.AufteilungMethode = AufteilungMethode;
        p.Jahresnutzungsgrad = Jahresnutzungsgrad;
        p.Unternehmensart = Unternehmensart;
        p.RaeumlicherZusammenhang = RaeumlicherZusammenhang;
        p.HocheffizienzNachweis = HocheffizienzNachweis;
        // ETAPPE B6: Leer heisst AUSWEIS - dieselbe Regel wie in der Datenbank, damit
        // ein Stand ohne Wahl nicht still auf etwas anderes faellt.
        p.StromsteuerBefreiungModus =
            string.IsNullOrEmpty(StromsteuerBefreiungModus)
                ? DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS
                : StromsteuerBefreiungModus;
    }
}
