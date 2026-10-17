using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Kein Fall hängt an der Anwenderdatenbank des Rechners</b> (<see cref="OhneAnwenderdatenbank"/>): Die
/// Zugriffsschicht sieht KEINE Datei — auch dann nicht, wenn unter <c>%ProgramData%\EPOS_PLAN</c> eine liegt. Der
/// Nutzungskatalog des Zonenplans ist dann die Vorgabe im Code (WOHNEN, BUERO, SCHULE), nicht die Profile einer
/// fremden Datenbank (<c>#1</c>, <c>#2</c>).
///
/// <para><b>Befund.</b> Auf dem Windows-Arbeitsrechner fielen vier Fälle von <c>GebaeudeImportZonenDialogTests</c>
/// und <c>GebaeudeImportProjektdateiDialogTests</c> mit den Katalogschlüsseln statt WOHNEN/BUERO, auf ubuntu waren
/// sie grün. Mit einer Katalogdatenbank am Standardpfad fielen sie unter Linux ebenso.</para>
/// </summary>
public sealed class OhneAnwenderdatenbankTests
{
    [Fact]
    public void Die_Zugriffsschicht_sieht_keine_Datenbank()
    {
        string pfad = DataRepository.GetDBPath();
        Assert.False(File.Exists(pfad), "Am Pfad der Zugriffsschicht liegt eine Datei: " + pfad);
        Assert.False(DataRepository.DatenbankVorhanden());
    }

    [Fact]
    public void Der_Zonenplan_rechnet_mit_der_Vorgabe_im_Code()
    {
        Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
        Assert.False(v.MitKatalog, "Die Zugriffsschicht sieht einen Nutzungskatalog: " + DataRepository.GetDBPath());
        Assert.Equal(Zonenplan.NUTZUNGEN, v.Auswahl().Select(a => a.Schluessel));
    }

    /// <summary>Der Pfad ist der des Initialisierers — nicht der der Einstellungen oder von <c>CommonApplicationData</c>.</summary>
    [Fact]
    public void Der_Pfad_der_Zugriffsschicht_ist_der_des_Initialisierers()
    {
        Assert.Equal(OhneAnwenderdatenbank.Pfad, DataRepository.PfadUeberschreibung);
        Assert.Equal(OhneAnwenderdatenbank.Pfad, DataRepository.GetDBPath());
        Assert.False(Directory.Exists(Path.GetDirectoryName(OhneAnwenderdatenbank.Pfad)));
    }
}
