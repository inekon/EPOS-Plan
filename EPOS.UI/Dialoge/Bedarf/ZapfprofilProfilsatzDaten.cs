namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die DATENSEITE von „Profil ablegen" im Blatt „Zapfprofil erzeugen" (Konzept Zapfprofil
//  Bereiche und Methoden, ZC6). Nur Daten; die Hülle (EPOS.UI.Daten/Bedarf/
//  ZapfprofilHuelle.Profilsatz.cs) füllt sie.
// =====================================================================================

/// <summary>
/// <b>Die Vorschau des Profilsatzes</b> (ZC6) für die Schlussleiste des Generator-Blatts:
/// Jahres- und Monatssummen, Spitzenstunde der mittleren Woche, die Warnung des Projektwegs,
/// der Hinweis auf den geltenden Rechenweg und die Knopftexte — alles in der Oberflächensprache.
/// </summary>
public sealed class ZapfprofilProfilsatzVorschauDaten
{
    /// <summary>Ist ein Profilsatz abgebildet? Sonst nennen <see cref="Kennung"/> und <see cref="Grund"/> den Grund.</summary>
    public bool Gerechnet { get; set; }
    public string Kennung { get; set; } = "";
    public string Grund { get; set; } = "";

    public bool MitZirkulation { get; set; }
    /// <summary>„Zapfwärme und Zirkulationsverlust" bzw. „nur Zapfwärme".</summary>
    public string Variante { get; set; } = "";

    public double JahressummeMwh { get; set; }
    public List<double> MonatssummenMwh { get; set; } = new List<double>();
    public double SpitzenstundeWocheKw { get; set; }
    /// <summary>Wochenstunde der Spitze, 0 = Montag 0–1 Uhr.</summary>
    public int SpitzenstundeWocheIndex { get; set; }
    /// <summary>Die Spitzenstunde als Text, etwa „Mo 07–08 Uhr: 3,2 kW".</summary>
    public string SpitzenstundeText { get; set; } = "";

    /// <summary>Gilt im Projekt gerade der Generator (sonst die Bestandsprofile)?</summary>
    public bool RechenwegGenerator { get; set; }
    public string HinweisRechenweg { get; set; } = "";
    /// <summary>Die Warnung vor „Zum Projekt hinzufügen".</summary>
    public string WarnungProjekt { get; set; } = "";
    public string KnopfKatalog { get; set; } = "";
    public string KnopfProjekt { get; set; } = "";
    /// <summary>
    /// Rechnet das Projekt auf dem Bestandsweg schon eine feste Zirkulation? Dann zählte ein Profil mit
    /// Zirkulation den Verlust doppelt; der Dialog schlägt „ohne Zirkulation“ vor.
    /// </summary>
    public bool BestandswegHatZirkulation { get; set; }
    /// <summary>Der Hinweis dazu; leer, wenn der Bestandsweg keine Zirkulation führt.</summary>
    public string HinweisZirkulationDoppelt { get; set; } = "";
    /// <summary>Der Name, den „Zum Projekt hinzufügen" vergäbe.</summary>
    public string NameProjekt { get; set; } = "";
}

/// <summary>Das Ergebnis von „Als Katalogsatz speichern…" bzw. „Zum Projekt hinzufügen".</summary>
public sealed class ZapfprofilProfilsatzErgebnis
{
    public bool Erfolg { get; set; }
    /// <summary>Die ID des neuen Katalogsatzes bzw. der neuen Zuordnungszeile des Projekts; 0 bei Ablehnung.</summary>
    public int Id { get; set; }
    /// <summary>Der Name des Satzes bzw. der Zeile.</summary>
    public string Name { get; set; } = "";
    /// <summary>Die Jahressumme [MWh] des Satzes.</summary>
    public double JahressummeMwh { get; set; }
    /// <summary>Der Ressourcenschlüssel der Ablehnung bzw. der Erfolgsmeldung.</summary>
    public string Kennung { get; set; } = "";
    /// <summary>Die Meldung in der Oberflächensprache.</summary>
    public string Text { get; set; } = "";
}
