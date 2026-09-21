namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Der ARBEITSSTAND der Überlagerung „Anlagenwerte" — und zugleich das Daten-Objekt,
/// das <c>PvStraengeFelder</c> für diese Maske beim Hilfe-Assistenten anmeldet
/// (Katalogschlüssel <c>Form_PV_Anlagenwerte</c>, Welle KI‑F7, Anwenderentscheid
/// 21.09.2026).
///
/// <para><b>Ein Fenster, ein Arbeitsstand.</b> Die vier Kennwerte des Wechselrichters
/// werden beim Öffnen aus der Anlage geholt, im Fenster bearbeitet und erst mit „OK" in
/// die Anlage übernommen; „Abbrechen" verwirft sie. Genau diese vier Zahlen stehen als
/// Eigenschaften hier — dieselben, an denen auch die vier Zahlenfelder der Überlagerung
/// hängen. Der Assistent liest und schreibt damit, was der Anwender vor sich hat, und
/// nicht den Stand der Anlage dahinter; die Entscheidung bleibt beim OK-Knopf.</para>
///
/// <para><b>Warum nicht einfach die Anlagenzeile.</b> Der Katalog hätte an
/// <c>ErzeugerZeile</c> binden können — dann setzte der Assistent an der Überlagerung
/// vorbei in die Anlage, während der Anwender im offenen Fenster noch andere Zahlen
/// sieht, und sein „Abbrechen" verwürfe die Assistentensetzung nicht. Das wäre genau die
/// stille Setzung, die Fachkonzept 11.6 meint.</para>
///
/// <para><b>Null und 0 heißen dasselbe: „nicht bekannt".</b> Das Fenster übernimmt nur
/// positive Werte in die Anlage (<see cref="Uebernehmen"/>); eine 0 wird dort zu
/// <c>null</c> — so hält es die Maske seit jeher.</para>
/// </summary>
public sealed class PvAnlagenwerteKiSicht
{
    /// <summary>AC-Nennleistung des Wechselrichters [kW]; <c>null</c> = nicht bekannt.</summary>
    public double? Nennleistung { get; set; }

    /// <summary>Wirkungsgrad bei 10 % Last als Faktor (0…1).</summary>
    public double? Eta10 { get; set; }

    /// <summary>Wirkungsgrad bei 50 % Last als Faktor (0…1).</summary>
    public double? Eta50 { get; set; }

    /// <summary>Wirkungsgrad bei 100 % Last als Faktor (0…1).</summary>
    public double? Eta100 { get; set; }

    /// <summary>Holt die vier Werte der Anlage in den Arbeitsstand (Fenster öffnen).</summary>
    public void Laden(ErzeugerZeile? zeile)
    {
        Nennleistung = zeile?.WrNennleistungKw;
        Eta10 = zeile?.WrEta10;
        Eta50 = zeile?.WrEta50;
        Eta100 = zeile?.WrEta100;
    }

    /// <summary>
    /// Schreibt den Arbeitsstand in die Anlage („OK"): 0 und <c>null</c> heißen beide
    /// „nicht bekannt" und landen als <c>null</c>.
    /// </summary>
    public void Uebernehmen(ErzeugerZeile? zeile)
    {
        if (zeile is null) return;

        zeile.WrNennleistungKw = Positiv(Nennleistung);
        zeile.WrEta10 = Positiv(Eta10);
        zeile.WrEta50 = Positiv(Eta50);
        zeile.WrEta100 = Positiv(Eta100);
    }

    private static double? Positiv(double? wert)
        => wert.HasValue && wert.Value > 0.0 ? wert : null;
}
