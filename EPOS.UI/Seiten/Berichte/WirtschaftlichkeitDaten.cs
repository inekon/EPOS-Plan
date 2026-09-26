using System;
using System.Collections.Generic;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>Eine Kennzahl-Karte über der Vergleichstabelle (KD6a).</summary>
public sealed class KachelZeile
{
    /// <summary>Überschrift der Karte.</summary>
    public string Titel { get; set; } = "";

    /// <summary>Der fertig formatierte Wert; leer = „—".</summary>
    public string Wert { get; set; } = "";

    /// <summary>Leise Zeile darunter: woher der Wert stammt.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>
    /// ETAPPE E5 (V‑A, Entscheid V‑3): die Einordnung der Kennzahl nach DIN EN 17463
    /// Anhang C — „nachrichtlich (Anhang C)" an Amortisation und internem Zinsfuß; leer
    /// beim Kapitalwert, dem einzigen Maß der Vorteilhaftigkeit, und bei der Annuität
    /// (Empfehlung Q3: sie ist der Kapitalwert als gleichmäßiger Jahresbetrag).
    /// </summary>
    public string Kennzeichen { get; set; } = "";

    /// <summary>
    /// ETAPPE E5 (V‑A, Befund A2): eine Warnung, die den Wert stehen lässt — die
    /// Mehrdeutigkeit des internen Zinsfußes bei mehr als einem Vorzeichenwechsel der
    /// Differenzreihe; leer = keine.
    /// </summary>
    public string Warnung { get; set; } = "";

    /// <summary>
    /// BV-E6 (Konzept Berichtsvorlagen 9.5): der Platzhalter, der genau diesen Wert erzeugt —
    /// die Hülle wählt ihn nach Stand (Stamm oder Variante) bzw. bester Variante; leer = keine
    /// Marke.
    /// </summary>
    public string Vorlagenfeld { get; set; } = "";

    /// <summary>„entspricht“ (Vorgabe) oder „ähnlich im Bericht“.</summary>
    public Vorlagenfeldstufe VorlagenfeldStufe { get; set; } = Vorlagenfeldstufe.Entspricht;

    /// <summary>Bei „ähnlich“: wo der Bericht anders zeigt; sonst leer.</summary>
    public string VorlagenfeldHinweis { get; set; } = "";
}

/// <summary>
/// ETAPPE E5 (U5) — eine <b>Empfehlungskarte</b> je Version: Stufe und
/// Kapitalwertdifferenz zur Referenz im Szenario Erwartet (Mockup Kategorie 8, „Lohnt es
/// sich?"). Die Stufe urteilt über alle drei Szenarien; die Regel steht im Kern
/// (<c>WirtschaftlichkeitEmpfehlung</c>).
/// </summary>
public sealed class EmpfehlungKarte
{
    /// <summary>Sprachneutrale Stufe „empfohlen" — die Stilklasse der Karte.</summary>
    public const string STUFE_JA = "JA";

    /// <summary>Sprachneutrale Stufe „bedingt empfohlen".</summary>
    public const string STUFE_BEDINGT = "BEDINGT";

    /// <summary>Sprachneutrale Stufe „nicht empfohlen".</summary>
    public const string STUFE_NEIN = "NEIN";

    /// <summary>Der Anzeigename der Version.</summary>
    public string Name { get; set; } = "";

    /// <summary>Die Stufe als Schlüssel (<see cref="STUFE_JA"/>, <see cref="STUFE_BEDINGT"/>,
    /// <see cref="STUFE_NEIN"/>).</summary>
    public string Stufe { get; set; } = "";

    /// <summary>Die Stufe als Text (<c>WIRT_EMPF_STUFE_*</c>), bei fehlender Bandbreite
    /// mit dem Zusatz „Bandbreite nicht berechnet".</summary>
    public string StufeText { get; set; } = "";

    /// <summary>ΔKW Erwartet mit Vorzeichen und Einheit („+1.660.205 €").</summary>
    public string Differenz { get; set; } = "";

    /// <summary>Fehlen Worst oder Best? Dann urteilt die Stufe allein nach Erwartet.</summary>
    public bool BandbreiteFehlt { get; set; }
}

/// <summary>
/// Eine Zeile der Vergleichstabelle: der Kennzahltitel und je Version eine
/// Zelle (Vorbild <c>UcWirtschaftlichkeit.Zeile</c>).
/// </summary>
public sealed class MatrixZeile
{
    /// <summary>ETAPPE E5 (U2): Die Zeile steht in der Kennzahltafel („Lohnt es sich?").</summary>
    public const string ABSCHNITT_KENNZAHL = "KENNZAHL";

    /// <summary>ETAPPE E5 (U2): Die Zeile gliedert den Kapitalwert („Woraus entsteht die
    /// Zahl?") — Vorgabe.</summary>
    public const string ABSCHNITT_GLIEDERUNG = "GLIEDERUNG";

    /// <summary>ETAPPE E5 (U2): eine Hinweiszeile (Fehlgrund, Hinweis, „nicht
    /// berechnet") — sie steht unter der Gliederung und speist das Warnband.</summary>
    public const string ABSCHNITT_HINWEIS = "HINWEIS";

    /// <summary>Der Kennzahltitel (erste Spalte, fett).</summary>
    public string Titel { get; set; } = "";

    /// <summary>Je Version eine fertig formatierte Zelle.</summary>
    public IReadOnlyList<string> Zellen { get; set; } = Array.Empty<string>();

    /// <summary>
    /// ETAPPE E5 (U2): in welchen Abschnitt der Seite die Zeile gehört
    /// (<see cref="ABSCHNITT_KENNZAHL"/>, <see cref="ABSCHNITT_GLIEDERUNG"/>,
    /// <see cref="ABSCHNITT_HINWEIS"/>). Die Hülle setzt es aus der Zeilendefinition des
    /// Kerns; die Seite teilt danach, sie urteilt nicht selbst.
    /// </summary>
    public string Abschnitt { get; set; } = ABSCHNITT_GLIEDERUNG;

    /// <summary>
    /// ETAPPE E5 (V‑A, V‑3): das Label am Titel — „nachrichtlich (Anhang C)" an
    /// Amortisation und Zinsfuß; leer = keines.
    /// </summary>
    public string Kennzeichen { get; set; } = "";

    /// <summary>
    /// ETAPPE E5 (V‑A, Befund A2): je Zelle eine Warnung, die den Wert stehen lässt (der
    /// mehrdeutige Zinsfuß); leer oder kürzer als <see cref="Zellen"/> = keine. Nicht zu
    /// verwechseln mit <see cref="ErgebnisMatrix.Warnungen"/>, den Warnzellen des Bandes.
    /// </summary>
    public IReadOnlyList<string> Zellwarnungen { get; set; } = Array.Empty<string>();

    /// <summary>Die Warnung der Zelle <paramref name="spalte"/>; <c>""</c> = keine.</summary>
    public string Zellwarnung(int spalte)
        => Zellwarnungen is not null && spalte >= 0 && spalte < Zellwarnungen.Count
           ? Zellwarnungen[spalte] ?? "" : "";

    /// <summary>
    /// ETAPPE E8a: eine <b>Summenzeile</b> — die Zeilen „Summe nominal" und „Barwert" unter
    /// den Zahlungsreihen (Block 2), der Nettobarwert der Gliederung. Die Seite setzt sie
    /// ab; gerechnet ist sie in der Hülle.
    /// </summary>
    public bool IstSumme { get; set; }

    /// <summary>
    /// ETAPPE E8a (U46): je Zelle eine leise zweite Zeile unter dem Wert — in der Gliederung
    /// des Kapitalwerts die Nominalsumme unter dem Barwert („nominal 48.000"). Leer oder
    /// kürzer als <see cref="Zellen"/> = keine.
    /// </summary>
    public IReadOnlyList<string> Unterwerte { get; set; } = Array.Empty<string>();

    /// <summary>Die zweite Zeile der Zelle <paramref name="spalte"/>; <c>""</c> = keine.</summary>
    public string Unterwert(int spalte)
        => Unterwerte is not null && spalte >= 0 && spalte < Unterwerte.Count
           ? Unterwerte[spalte] ?? "" : "";
}

/// <summary>
/// ETAPPE E8a (ValERI-Block 2 „Zahlungsreihen", DIN EN 17463 6.1 bis 6.4) — die
/// Zahlungsreihen EINES Standes in EINEM Szenario: je Jahr eine Zeile mit den sechs
/// Bestandteilen der Gliederung (Investition, Betriebskosten, Energiekosten, Erlöse,
/// Ersatzbeschaffungen, Restwert), dem Netto und dem Barwert; darunter die Summe nominal
/// und die Barwerte. Die Hülle baut sie aus dem Zahlungsbild des Laufs, fertig formatiert.
/// </summary>
public sealed class ZahlungsreihenTafel
{
    /// <summary>Der Stand (<c>Tab_Projekt.ID</c>).</summary>
    public int IdStand { get; set; }

    /// <summary>Das Szenario als Nummer der Szenario-Klappliste
    /// (<see cref="WirtschaftlichkeitStand.Szenarien"/>, 0 = Erwartet).</summary>
    public int Szenario { get; set; }

    /// <summary>Die Tafel: Spalten Jahr · sechs Bestandteile · Netto nominal · Barwert.</summary>
    public ErgebnisMatrix Tafel { get; set; } = new();

    /// <summary>Die Zeile unter der Tafel: nominal, Vorzeichen, Zins und Nettobarwert.</summary>
    public string Unterzeile { get; set; } = "";

    /// <summary>
    /// ETAPPE E8a (U42) — das <b>Zahlungsstrombild</b> desselben Standes im selben Szenario
    /// (<c>ChartRenderer.ZahlungsstromModell</c>): die Spalten der Mehrjahrestafel als
    /// gestapelte Jahresbalken, Ausgaben nach unten, Ersatzjahre markiert. <c>null</c> = keines.
    /// </summary>
    public Zeichenmodell? Bild { get; set; }
}

/// <summary>
/// Die Vergleichstabelle als Matrix.
///
/// <para><b>Warum kein <c>Raster</c>.</b> Ein <c>QuickGrid</c> braucht
/// Spalten, die zur Übersetzungszeit feststehen; hier entstehen sie zur
/// Laufzeit — eine je Version der Gruppe. Der Vorläufer baute dafür
/// <c>grid.Columns.Add</c> je Ergebniszeile. Die Blazor-Fassung schreibt eine
/// gewöhnliche Tabelle mit der Hausklasse <c>epos-raster</c>: dieselbe Optik,
/// ohne dem Baustein eine Fähigkeit anzudichten, die er nicht hat.</para>
/// </summary>
public sealed class ErgebnisMatrix
{
    /// <summary>Die Spaltenköpfe: „Kennzahl" und je Version einer.</summary>
    public IReadOnlyList<string> Spalten { get; set; } = Array.Empty<string>();

    /// <summary>Die Zeilen in Anzeigereihenfolge.</summary>
    public IReadOnlyList<MatrixZeile> Zeilen { get; set; } = Array.Empty<MatrixZeile>();

    /// <summary>
    /// ETAPPE E5 (U2): dieselbe Matrix OHNE die Zeilen eines Abschnitts — die Seite
    /// zeichnet in „Woraus entsteht die Zahl?" die Gliederung und die Hinweise, nicht die
    /// Kennzahlen, die schon in „Lohnt es sich?" stehen. Spalten bleiben, wie sie sind.
    /// </summary>
    public ErgebnisMatrix Ohne(string abschnitt)
    {
        var zeilen = new List<MatrixZeile>();
        foreach (MatrixZeile z in Zeilen)
            if (z is not null && !string.Equals(z.Abschnitt, abschnitt, StringComparison.Ordinal))
                zeilen.Add(z);
        return new ErgebnisMatrix { Spalten = Spalten, Zeilen = zeilen };
    }

    /// <summary>
    /// Das Zeichen, mit dem eine WARNZELLE der Matrix beginnt (Hinweis, Fehlgrund,
    /// „nicht berechnet"). Die Hülle schreibt es seit W3 vor jede solche Zelle; seit
    /// Auftrag #267 steht es hier als Konstante, statt an drei Stellen als Literal.
    /// </summary>
    public const string WARN_PRAEFIX = "⚠ ";

    /// <summary>
    /// <b>Die Warnungen der Tabelle, jede einmal</b> — Grundlage des Warnbandes über
    /// der Seite (Auftrag #267, Anwenderbefund 14.09.2026).
    ///
    /// <para><b>Warum aus der Matrix und nicht aus einem eigenen Feld.</b> Die
    /// Warnungen stehen längst in der Tabelle: Der Kern liefert je Ergebnis
    /// <c>Hinweis</c> und <c>Fehlgrund</c>, die Hülle setzt sie als Zeile „Hinweis"
    /// mit <see cref="WARN_PRAEFIX"/> davor. Ein zweites Feld daneben wäre eine
    /// zweite Wahrheit, die auseinanderlaufen kann — und die ganz unten in einer
    /// langen Tabelle stehende Zeile hat der Anwender schlicht nicht gesehen: Er las
    /// „Energiekosten —" und schloss auf einen Programmfehler.</para>
    ///
    /// <para>Reihenfolge der Tabelle, Dubletten fallen weg (dieselbe Meldung steht in
    /// jeder Spalte).</para>
    /// </summary>
    public IReadOnlyList<string> Warnungen()
    {
        var treffer = new List<string>();
        foreach (MatrixZeile z in Zeilen)
        {
            if (z?.Zellen == null) continue;
            foreach (string zelle in z.Zellen)
            {
                if (string.IsNullOrEmpty(zelle) || !zelle.StartsWith(WARN_PRAEFIX, StringComparison.Ordinal))
                    continue;

                // Der Kern hängt mehrere Hinweise mit " | " aneinander
                // (WirtschaftlichkeitCtrl.Anhaengen). Im Band wird daraus je eine
                // eigene Zeile — ein Band mit drei Sätzen hintereinander liest
                // niemand zu Ende.
                foreach (string teil in zelle.Substring(WARN_PRAEFIX.Length).Split('|'))
                {
                    string text = teil.Trim();
                    if (text.Length > 0 && !treffer.Contains(text)) treffer.Add(text);
                }
            }
        }
        return treffer;
    }
}

/// <summary>
/// Was ein Szenariowechsel neu zeigt (Vorbild
/// <c>UcWirtschaftlichkeit.ZeigeErgebnisse</c>): die vier Kennzahl-Karten und
/// die Vergleichstabelle. Gerechnet wird dabei nichts — die Hülle liest den
/// Lauf, der schon im Speicher liegt.
/// </summary>
public sealed class ErgebnisAnsicht
{
    /// <summary>
    /// Die vier Kennzahl-Karten (Kapitalwert, Annuität, Amortisation, IRR). ETAPPE E5
    /// Teil b: Sie stehen in „Lohnt es sich?" über der Szenario-Klappliste und zeigen
    /// deshalb den Erwartungsfall — die Klappliste steuert nur die Tafeln darunter.
    /// </summary>
    public IReadOnlyList<KachelZeile> Kacheln { get; set; } = Array.Empty<KachelZeile>();

    /// <summary>
    /// BV-E6: der Platzhalter der Gliederungstafel im gezeigten Szenario
    /// (<c>tabelle.wirtschaft.kennzahlen</c>, <c>.guenstig</c>, <c>.unguenstig</c>); leer = keine Marke.
    /// </summary>
    public string GliederungVorlagenfeld { get; set; } = "";

    /// <summary>
    /// Die Vergleichstabelle des GEWÄHLTEN Szenarios — alle Zeilen der Definition, je mit
    /// ihrem <see cref="MatrixZeile.Abschnitt"/>. Die Seite zeichnet daraus „Woraus entsteht
    /// die Zahl?" (<see cref="ErgebnisMatrix.Ohne"/> ohne die Kennzahlen); das Warnband
    /// liest ihre Hinweiszeilen.
    /// </summary>
    public ErgebnisMatrix Matrix { get; set; } = new();

    /// <summary>
    /// ETAPPE E5 (U2, Mockup „Lohnt es sich?" — „Die Kennzahlen dazu"): die
    /// Kennzahltafel im Szenario ERWARTET — Kapitalwertdifferenz, Annuität, Amortisation,
    /// Zinsfuß, Wärmegestehungskosten, Nettobarwert, mit Label und Zellwarnung.
    /// </summary>
    public ErgebnisMatrix Kennzahltafel { get; set; } = new();

    /// <summary>
    /// ETAPPE E5 (V‑A, V‑G6): die Sensitivitätstafel (Szenario Erwartet) — Spalten
    /// Version · Einflussgröße · bei −Δ · Basis · bei +Δ · Steigung; der Name der Version
    /// steht an ihrer ersten Zeile. Leer = keine Sensitivität gerechnet.
    /// </summary>
    public ErgebnisMatrix Sensitivitaet { get; set; } = new();

    /// <summary>
    /// ETAPPE E5 (Konzept § 6.3 Nr. 31): „‹Stände›: Nachweis liegt mit der nächsten
    /// Rechnung vor" — leer, wenn jede gezeigte Ergebniszeile ihren Nachweis trägt.
    /// </summary>
    public string Nachweiszeile { get; set; } = "";

    /// <summary>
    /// ETAPPE E5 (V‑1, ValERI-Block 1 „Gegenstand und Rahmen"): Maßnahme, Referenz,
    /// Betrachtungszeitraum und Kalkulationszins — je Zeile der Titel und EINE Zelle.
    /// </summary>
    public ErgebnisMatrix Rahmen { get; set; } = new();

    /// <summary>
    /// ETAPPE W5‑B‑9 (Anwenderentscheid 09.09.2026): die Statuszeile des GEWÄHLTEN
    /// Szenarios — mit welchem Parametersatz es rechnet und ob der aus Vorgaben oder
    /// aus gepflegten Werten besteht.
    ///
    /// <para>Sie steht an der <b>Ansicht</b> und nicht am Stand, weil sie mit der
    /// Szenariowahl wechselt: Der Wechsel tauscht genau dieses Objekt aus, und die
    /// Zeile zieht damit von selbst mit.</para>
    ///
    /// <para>Leer = keine Zeile (Szenario Erwartet ohne besondere Angabe, oder die
    /// Parameter waren nicht lesbar).</para>
    /// </summary>
    public string Szenariozeile { get; set; } = "";

    /// <summary>
    /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, VALERI-Lücke G9): der
    /// <b>Vorschlag zur Entscheidung</b> — welche Variante gegenüber dem Stammprojekt
    /// empfohlen wird und wie belastbar das über die Bandbreite ist.
    ///
    /// <para>Sie steht an der <b>Ansicht</b> und nicht am Stand, obwohl sie vom
    /// gewählten Szenario unabhängig ist: Sie hängt an der VERGLEICHSWAHL, und die
    /// tauscht — wie der Szenariowechsel — genau dieses Objekt aus. Am Stand würde
    /// sie einem Haken erst beim nächsten vollen Laden folgen.</para>
    ///
    /// <para>Leer = keine Variante mit Erwartet-Ergebnis gegenüber dem Stamm; dann
    /// wird die Zeile gar nicht erst gezeichnet.</para>
    ///
    /// <para><b>ETAPPE E5 (U5):</b> Der Satz entsteht aus denselben Einstufungen wie
    /// <see cref="Empfehlungen"/> und nennt die Referenz beim Namen.</para>
    /// </summary>
    public string Empfehlungszeile { get; set; } = "";

    /// <summary>
    /// ETAPPE E5 (U5): die Empfehlungskarten der gewählten Versionen — je Version mit
    /// Erwartet-Ergebnis gegen die Referenz eine Karte, in der Reihenfolge der Gruppe.
    /// Sie hängen wie der Vorschlagssatz an der Vergleichswahl, nicht am gezeigten
    /// Szenario.
    /// </summary>
    public IReadOnlyList<EmpfehlungKarte> Empfehlungen { get; set; } = Array.Empty<EmpfehlungKarte>();

    /// <summary>
    /// ETAPPE E5 (U4): die <b>Bandbreite</b> der Kapitalwertdifferenz über die drei
    /// Szenarien nebeneinander — Spalten Version · Ungünstig · Erwartet · Günstig ·
    /// Spanne · Einstufung, als erste Zeile die Referenz. Sie folgt der Vergleichswahl,
    /// nicht der Szenario-Klappliste. Leer (keine Zeilen) = nichts gerechnet.
    /// </summary>
    public ErgebnisMatrix Bandbreite { get; set; } = new();

    /// <summary>
    /// ETAPPE E5 (U4): der Fußtext der Bandbreite — was ΔKW heißt, gegen welche Referenz
    /// gerechnet ist und wie die Spanne entsteht (<c>WIRT_SZ_DELTA_FUSS</c>, derselbe wie
    /// im Bericht). Leer ohne Bandbreite.
    /// </summary>
    public string Bandbreitenfuss { get; set; } = "";

    /// <summary>
    /// ETAPPE E6 (Nachtrag E5b, Anwenderentscheid 22.09.2026 zu Frage (4), Mockup
    /// <c>valeri-f2</c>): das <b>Spannenbild</b> — die Bandbreite je Version als Balken
    /// (Ungünstig bis Günstig, Erwartet als Punkt, die Referenz als Nulllinie), gebaut vom
    /// Renderer des Kerns aus DEMSELBEN Modell wie <see cref="Bandbreite"/>.
    /// <c>null</c> = keine Bandbreite, dann steht kein Bild.
    /// </summary>
    public Zeichenmodell? Spannenbild { get; set; }

    // =====================================================================
    // ETAPPE E8a — Gliederung des Kapitalwerts (U46) und Zahlungsreihen (Block 2)
    // =====================================================================

    /// <summary>
    /// ETAPPE E8a (U46, Mockup „Woraus entsteht die Zahl?"): die <b>Gliederung des
    /// Kapitalwerts</b> im GEWÄHLTEN Szenario — je Bestandteil (Investition, Betriebskosten,
    /// Energiekosten, Erlöse, Ersatzbeschaffungen, Restwert) und Stand der Barwert, darunter
    /// die Nominalsumme (<see cref="MatrixZeile.Unterwerte"/>), als letzte Spalte
    /// „Differenz ‹Leitversion› − Referenz", als letzte Zeile der Nettobarwert; die
    /// Differenzspalte geht in der Kapitalwertdifferenz auf. Leer = keine Jahresreihen.
    /// </summary>
    public ErgebnisMatrix Bestandteile { get; set; } = new();

    /// <summary>Die Überschrift der Gliederung („Gliederung des Kapitalwerts — Szenario Erwartet").</summary>
    public string BestandteileTitel { get; set; } = "";

    /// <summary>Die Zeile darunter: Barwert und Nominalsumme, Zins und Zeitraum.</summary>
    public string BestandteileUnterzeile { get; set; } = "";

    /// <summary>
    /// ETAPPE E8a (U41, Mockup „Von der Investition zur Kapitalwertdifferenz"): das
    /// <b>Brückenbild</b> der Leitversion gegen die Referenz im gewählten Szenario — je
    /// Bestandteil eine Säule, die Ergebnissäule ist die Kapitalwertdifferenz; dieselben Zahlen
    /// wie die Differenzspalte von <see cref="Bestandteile"/>, gebaut vom Renderer des Kerns.
    /// <c>null</c> = keine Leitversion oder keine Jahresreihen, dann steht kein Bild.
    /// </summary>
    public Zeichenmodell? Bruecke { get; set; }

    /// <summary>
    /// ETAPPE E8a (U47, Mockup „Was ist angenommen?"): die Tafel „Was daraus im Lauf wird" —
    /// je Szenario (Ungünstig · Erwartet · Günstig) die Wirkung auf die Leitversion:
    /// Investition I₀, die Jahre der fälligen Ersatzbeschaffungen, der Restwert am Ende. Die
    /// Spalten sind die Szenarioläufe der Bandbreite. Sie hängt an der Vergleichswahl, nicht
    /// an der Szenario-Klappliste. Leer = keine Jahresreihen.
    /// </summary>
    public ErgebnisMatrix Laufwirkung { get; set; } = new();

    /// <summary>
    /// ETAPPE E8a (U48): die Fußzeile von „Was ist angenommen?" — wie viele Szenarien
    /// gerechnet sind und woher ihre Annahmen kommen („Drei Szenarien gerechnet · Annahmen aus
    /// Vorgaben, nichts gepflegt"). Leer = keine Zeile.
    /// </summary>
    public string Szenariofuss { get; set; } = "";

    /// <summary>
    /// ETAPPE E8a (ValERI-Block 2): die Zahlungsreihen je Stand und Szenario — aus den
    /// Zahlungsbildern des Laufs, der in dieser Sitzung gerechnet ist, und nur, wo sie zum
    /// gespeicherten Ergebnis passen. Leer = keine Jahresreihen (dann steht
    /// <see cref="Zahlungshinweis"/> bzw. die Hinweiszeile des Blocks). Sie hängen an der
    /// Vergleichswahl, nicht an der Szenario-Klappliste: Block 2 wählt selbst.
    /// </summary>
    public IReadOnlyList<ZahlungsreihenTafel> Zahlungsreihen { get; set; } = Array.Empty<ZahlungsreihenTafel>();

    /// <summary>Die Stände, für die es Zahlungsreihen gibt (Id, Name) — die Auswahl in Block 2.</summary>
    public IReadOnlyList<(int Id, string Text)> Zahlungsstaende { get; set; } = Array.Empty<(int, string)>();

    /// <summary>
    /// ETAPPE E8a: die <b>Leitversion</b> — der Stand, dessen Differenz zur Referenz
    /// Differenzspalte, Brückenbild und „Was daraus im Lauf wird" zeigen (die größte
    /// Kapitalwertdifferenz im Erwartungsfall, in Sicht 2 B); Block 2 zeigt ihn zuerst.
    /// 0 = keiner.
    /// </summary>
    public int Leitversion { get; set; }

    /// <summary>
    /// ETAPPE E8a: warum Jahresreihen fehlen — kein Lauf in dieser Sitzung, oder die Reihen
    /// eines Standes passen nicht zu den gespeicherten Ergebnissen. Leer = alle da.
    /// </summary>
    public string Zahlungshinweis { get; set; } = "";
}

/// <summary>
/// Der Anzeigestand der Wirtschaftlichkeitsseite — was der Vorläufer in
/// <c>LadeDaten</c>, <c>AktualisiereListe</c>, <c>ZeigeParameterzeile</c> und
/// <c>BauePhotovoltaikKnopf</c> verteilt zusammentrug, in einer Antwort.
/// </summary>
public sealed class WirtschaftlichkeitStand
{
    /// <summary>ETAPPE E5 (U2): Darstellung „Kennzahlen" — die vier Abschnitte (Vorgabe).</summary>
    public const int DARSTELLUNG_KENNZAHLEN = 0;

    /// <summary>ETAPPE E5 (U2): Darstellung „ValERI-Bewertung" — die Blöcke der Norm.</summary>
    public const int DARSTELLUNG_VALERI = 1;

    /// <summary>
    /// ETAPPE E5 (U2, Entscheid V‑1/K8): der Zustand des Umschalters „Kennzahlen /
    /// ValERI-Bewertung" — eine Sitzungswahl der Hülle (Muster Vergleichsauswahl), Vorgabe
    /// <see cref="DARSTELLUNG_KENNZAHLEN"/>.
    /// </summary>
    public int Darstellung { get; set; }

    /// <summary>
    /// ETAPPE E5 (U2, Mockup „Was ist angenommen?"): die Annahmentafel — Spalten Größe ·
    /// Ungünstig · Erwartet · Günstig · Herkunft, je Größe eine Zeile mit den WIRKSAMEN
    /// Werten. Sie hängt am Parametersatz, nicht an der Wahl.
    /// </summary>
    public ErgebnisMatrix Annahmen { get; set; } = new();

    /// <summary>Stamm und Varianten der Vergleichsgruppe, Stamm zuerst.</summary>
    public IReadOnlyList<VarianteZeile> Varianten { get; set; } = Array.Empty<VarianteZeile>();

    /// <summary>Die Ids der angehakten Versionen (der Stamm ist immer dabei).</summary>
    public IReadOnlyList<int> GewaehlteVarianten { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Die wählbaren Szenarien. Die PERSISTENZWERTE
    /// (<c>Tab_ErgebnisWirtschaftlichkeit.Szenario</c>) kennt nur die Hülle;
    /// hier stehen Nummer und Anzeigetext (Drei-Schichten-Regel).
    /// </summary>
    public IReadOnlyList<(int Id, string Text)> Szenarien { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Das vorgewählte Szenario (0 = „Erwartet").</summary>
    public int SzenarioId { get; set; }

    /// <summary>Der Parameternachweis als eine Zeile (L12/L13).</summary>
    public string Parameterzeile { get; set; } = "";

    /// <summary>
    /// ETAPPE W5‑B‑11 (VALERI-Lücke G7): der Betrachtungszeitraum gegen die
    /// Nutzungsdauern — längste und kürzeste gepflegte Dauer und daraus, ob ein
    /// Restwert am Ende steht und ob zwischendurch ersetzt wird. Leer = kein
    /// Zeitraum lesbar.
    /// </summary>
    public string Zeitraumzeile { get; set; } = "";

    /// <summary>
    /// ETAPPE W5‑B‑11 (VALERI-Lücken G10 und G1/G3/G5): die Herleitung der
    /// Eigennutzung (nur mit Photovoltaik in der Gruppe) und die offengelegten
    /// Vereinfachungen. Eine Vereinfachung, die dasteht, ist eine Annahme; eine, die
    /// nicht dasteht, ist ein Fehler.
    /// </summary>
    public string Vereinfachungszeile { get; set; } = "";

    /// <summary>
    /// ETAPPE E5 (U39, Konzept § 2.13 (3)): die Hinweiszeilen „k von n Positionen ohne
    /// Nutzungsdauer" — je Satz eine Zeile, nur wo der Betrachtungszeitraum über der
    /// Vorgabe der Technik liegt. Ein Prüfauftrag, keine Fehlermeldung; leer = nichts
    /// zu prüfen.
    /// </summary>
    public IReadOnlyList<string> Nutzungsdauerhinweise { get; set; } = Array.Empty<string>();

    /// <summary>
    /// ETAPPE E9b (U10, Konzept § 2.11.5 und § 2.11.7; E9b‑Q3): der AUSWEIS unter der
    /// Annahmentafel — „n von m Parametern szenariert" samt der gepflegten Größen, an der
    /// Stelle des früheren Hinweistexts (<c>SzenarioAbdeckung.Satz</c>). Leer = kein
    /// Ausweis (kein Parametersatz lesbar).
    /// </summary>
    public string Szenarioabdeckung { get; set; } = "";

    /// <summary>
    /// ETAPPE E5 (V‑A): die Deklarationszeilen der Bewertung nach DIN EN 17463 — nominal ·
    /// Steuern · Restwert · Risiko, in dieser Reihenfolge.
    /// </summary>
    public IReadOnlyList<string> Deklarationen { get; set; } = Array.Empty<string>();

    /// <summary>
    /// AUFTRAG #325 (Anwenderwunsch 17.09.2026): der GEPFLEGTE Text der nicht
    /// monetären Wirkungen — <c>WirtschaftlichkeitParameter.NichtMonetaer</c>, roh
    /// und unformatiert. Er füllt den Bewertungsblock unter der Kennzahltabelle,
    /// der ihn seit diesem Auftrag auch entgegennimmt.
    ///
    /// <para>AUFTRAG #328: Dieser Wert ist auf der Seite die EINZIGE Darstellung des
    /// Textes. Zugeklappt weist der Kopf des Bewertungsblocks ihn aus, aufgeklappt
    /// steht er im Feld darunter; eine zweite, fertig formulierte Zeile im
    /// Nachweisblock gibt es nicht mehr. Der Bericht ist davon unberührt — Word und
    /// Excel lesen <c>WirtschaftlichkeitParameter.NichtMonetaer</c> selbst.</para>
    ///
    /// <para><b>ETAPPE E17 (V‑G11): das ALTFELD.</b> Die Wirkungen stehen seither als Liste
    /// (<see cref="Wirkungen"/>); der Freitext bleibt lesbar und wird auf der Seite nur noch
    /// gekennzeichnet angezeigt („Altfeld"), nicht mehr geschrieben. Schemaschritt 127 hat
    /// ihn als eine Wirkung der Kategorie „sonstig" in die Liste übernommen.</para>
    /// </summary>
    public string NichtMonetaer { get; set; } = "";

    /// <summary>
    /// ETAPPE E17 (V‑G11, DIN EN 17463 6.1 und 8.2): die nicht monetarisierbaren Wirkungen
    /// des Stammprojekts — je Wirkung Kategorie, Beschreibung, Dauer und die drei
    /// Wirkungsgrade (<c>ProjektWirkungCtrl.Laden</c>). Die Beurteilung rechnet die Zeile
    /// selbst (<c>ProjektWirkung.Beurteilung</c>). Keine Rechenwirkung.
    /// </summary>
    public List<WindowsFormsApplication1.ProjektWirkung> Wirkungen { get; set; } = new();

    /// <summary>Die Kennzahlen und die Tabelle des vorgewählten Szenarios.</summary>
    public ErgebnisAnsicht Ansicht { get; set; } = new();

    /// <summary>Führt die Gruppe Photovoltaik? Dann erscheint „Photovoltaik…".</summary>
    public bool MitPhotovoltaik { get; set; }

    /// <summary>Führt die Gruppe ein BHKW? Dann erscheint „BHKW-Wirtschaftlichkeit…".</summary>
    public bool MitBhkw { get; set; }

    /// <summary>Die Statuszeile beim Aufbau (gespeicherter Stand, veraltet, keiner).</summary>
    public string Statuszeile { get; set; } = "";

    // =====================================================================
    // KONZEPT § 2.9 und § 2.15 — Referenz und Vergleichssicht
    // =====================================================================

    /// <summary>
    /// KONZEPT § 2.9 — die REFERENZ DER GRUPPE (<c>Tab_Projekt.ID</c>): der Stand, gegen
    /// den alle Differenzkennzahlen rechnen. Sie ist in der Vergleichsgruppen-Liste
    /// wählbar und gespeichert; die Vorgabe ist das Stammprojekt.
    /// </summary>
    public int IdReferenz { get; set; }

    /// <summary>
    /// KONZEPT § 2.15 — die Sicht der Vergleichstafeln: 0 = alle Varianten gegen die
    /// Referenz (Vorgabe), 1 = zwei Stände A und B. Sitzungswahl, keine Spalte.
    /// </summary>
    public int Sicht { get; set; }

    /// <summary>Der Stand A der Sicht 2 — in dieser Sicht die Referenz.</summary>
    public int SichtA { get; set; }

    /// <summary>Der Stand B der Sicht 2 — der Stand, dessen Differenz gezeigt wird.</summary>
    public int SichtB { get; set; }

    /// <summary>
    /// Trägt die Gruppe zwei Stände? Sonst ist Sicht 2 gesperrt, und der Werkzeugtipp
    /// nennt den Grund („mindestens zwei Stände").
    /// </summary>
    public bool PaarMoeglich { get; set; }

    /// <summary>
    /// Die ERKLÄRZEILE unter der Optionsgruppe: in Sicht 1 „Referenz: ‹Name›", in
    /// Sicht 2 „Referenz dieser Sicht: ‹A› · Referenz der Gruppe: ‹Referenz›", je mit
    /// der Vorzeichenregel. Leer = keine Zeile.
    /// </summary>
    public string Referenzzeile { get; set; } = "";

    /// <summary>
    /// ALLE Stände der Vergleichsgruppe (Id und Anzeigename), Stamm zuerst — die
    /// Auswahlliste der Referenz (§ 2.9). Die Klapplisten A und B führen davon die
    /// angehakten (<see cref="GewaehlteVarianten"/>).
    /// </summary>
    public IReadOnlyList<(int Id, string Text)> Staende { get; set; }
        = Array.Empty<(int, string)>();
}
