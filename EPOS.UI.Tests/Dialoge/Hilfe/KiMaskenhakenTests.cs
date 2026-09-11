using System;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// DIE HAKEN DER FÜNF MASKEN (Auftrag #201, Stufe S3).
///
/// <para><b>Was hier bewiesen wird.</b> Jede der fünf Masken meldet beim Öffnen nicht nur
/// ihre Felder an, sondern auch das, was der Assistent darüber hinaus braucht: einen Weg,
/// die Maske neu zu zeichnen, und — wo die Maske einen hat — ihren Speicherweg. Fehlt ein
/// Haken, lehnt die zugehörige Aktion benannt ab; das ist kein Fehler, aber es soll eine
/// ENTSCHEIDUNG sein und kein Versehen.</para>
///
/// <para><b>Monotone Aussagen, wie in <see cref="KiFeldwerteTests"/>.</b> Die
/// Maskenbrücke ist prozessweiter Zustand, und xunit fährt Testklassen nebeneinander.
/// Geprüft wird deshalb nur, was nach dem Zeichnen DA ist — nicht, dass nichts anderes da
/// wäre.</para>
/// </summary>
public class KiMaskenhakenTests : EposBunitContext
{
    public KiMaskenhakenTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Seit Auftrag #211 (Restpunkt aus Bericht #201) meldet auch der Heizkesseleditor
    /// den Schreibschutz — <c>HeizkesselKatalogDaten.NurLesen</c> trägt das
    /// <c>ReadOnly</c> des geladenen Auslieferungssatzes.
    /// </summary>
    [Fact]
    public void Der_Heizkesseleditor_meldet_Auffrischen_Pruefen_Schreibschutz_und_Speichern()
    {
        Render<HeizkesselKatalogDialog>(p => p.Add(x => x.Daten, new HeizkesselKatalogDaten()));

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.HEIZKESSEL);

        Assert.NotNull(haken.Auffrischen);
        Assert.NotNull(haken.Pruefen);
        Assert.NotNull(haken.Schreibgeschuetzt);
        Assert.NotNull(haken.Speichern);
    }

    /// <summary>
    /// Seit Auftrag #211 meldet auch der Pufferspeichereditor den Schreibschutz —
    /// <c>PufferSpKatalogDaten.NurLesen</c> trägt das <c>ReadOnly</c> des geladenen
    /// Auslieferungssatzes.
    /// </summary>
    [Fact]
    public void Der_Pufferspeichereditor_meldet_Auffrischen_Pruefen_Schreibschutz_und_Speichern()
    {
        Render<PufferSpKatalogDialog>(p => p.Add(x => x.Daten, new PufferSpKatalogDaten()));

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.PUFFERSPEICHER);

        Assert.NotNull(haken.Auffrischen);
        Assert.NotNull(haken.Pruefen);
        Assert.NotNull(haken.Schreibgeschuetzt);
        Assert.NotNull(haken.Speichern);
    }

    /// <summary>
    /// Der Photovoltaik-Dialog schreibt keinen Katalog, er ÜBERNIMMT die gewählte Zeile
    /// in das Modell — das ist sein Speicherweg, und eine Knopfprüfung hat er nicht. Seit
    /// Auftrag #211 meldet er dazu den Schreibschutz des GERÄTS der gewählten Zeile
    /// (<c>ErzeugerZeile.NurLesen</c>).
    /// </summary>
    [Fact]
    public void Der_Photovoltaikdialog_meldet_Auffrischen_Schreibschutz_und_Uebernehmen()
    {
        Render<PhotovoltaikDialog>();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.PHOTOVOLTAIK);

        Assert.NotNull(haken.Auffrischen);
        Assert.NotNull(haken.Schreibgeschuetzt);
        Assert.NotNull(haken.Speichern);
    }

    /// <summary>
    /// Die Wärmepumpenverwaltung war bis Auftrag #211 die EINZIGE der fünf, die den
    /// Schreibschutz des Auslieferungskatalogs kannte
    /// (<c>WaermepumpeStammDaten.NurLesen</c>) — seither melden ihn auch Heizkessel,
    /// Photovoltaik und Pufferspeicher (siehe die drei Fälle oben).
    /// </summary>
    [Fact]
    public void Die_Waermepumpenverwaltung_meldet_auch_den_Schreibschutz()
    {
        Render<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDialog>();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.WAERMEPUMPE);

        Assert.NotNull(haken.Auffrischen);
        Assert.NotNull(haken.Schreibgeschuetzt);
        Assert.NotNull(haken.Speichern);
    }

    /// <summary>
    /// Die Stromspeicher-Ansicht ist die EINZIGE mit Rechenwegen — und ihr Schlüssel ist
    /// der Aktionsname, kein eigener Katalog.
    /// </summary>
    [Fact]
    public void Die_Stromspeicheransicht_meldet_ihre_zwei_Rechenwege()
    {
        Render<EPOS.UI.Seiten.Strom.StromspeicherAuslegungSeite>();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.STROMSPEICHER_AUSLEGUNG);

        Assert.NotNull(haken.Auffrischen);
        Assert.NotNull(haken.Speichern);
        Assert.NotNull(haken.FindeRechenweg("flotte_bewerten"));
        Assert.NotNull(haken.FindeRechenweg("peak_ziel_bestimmen"));
        Assert.Null(haken.FindeRechenweg("gibt_es_nicht"));
    }

    /// <summary>Eine unbekannte Maske liefert den LEEREN Hakensatz und nicht <c>null</c>.</summary>
    [Fact]
    public void Eine_unbekannte_Maske_liefert_den_leeren_Hakensatz()
    {
        KiMaskenhaken haken = KiMaskenbruecke.Haken("Form_GibtEsNicht");

        Assert.NotNull(haken);
        Assert.Null(haken.Speichern);
        Assert.Equal("", haken.Befund());
        Assert.False(haken.IstSchreibgeschuetzt());
    }
}
