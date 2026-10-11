using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>G5-N — die Hülle der Nordrichtung im Zuordnungsdialog</b> (N1–N3) an einer echten Importprobe: was die Datei nennt,
/// die wirksame Richtung der Planoberseite samt Herkunft, die Schnellwahl, das Setzen mit Neu-Lesen und das Neu-Zuordnen
/// mit gesetztem Winkel — die Azimute des Vorschlags drehen genau um den Unterschied.
/// </summary>
public sealed class GebaeudeImportNordrichtungHuelleTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

    public void Dispose() => _kultur.Dispose();

    private static string Probe(string name)
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return Path.Combine(d!.FullName, "Referenzlaeufe", "Importproben", name);
    }

    /// <summary>Die Azimute der Bauteilzeilen des Stands (Anzeigetext „123,4°“ in de-DE); <c>null</c> = keiner.</summary>
    private static List<double?> Azimute(GebaeudeImportStand stand)
        => stand.Bauteile!.Zeilen.Select(z => double.TryParse((z.Azimut ?? "").TrimEnd('°'), System.Globalization.NumberStyles.Float,
                                                               System.Globalization.CultureInfo.GetCultureInfo("de-DE"), out double w)
                                                  ? w : (double?)null).ToList();

    [Fact]
    public async Task Datei_Eingabe_Schnellwahl_und_Neu_Zuordnen_mit_gesetztem_Winkel()
    {
        var huelle = new GebaeudeImportHuelle();
        Assert.Null(huelle.NordrichtungDaten());
        Assert.False(huelle.NordrichtungSetzen(90.0).Gelesen);

        GebaeudeLesestand stand = await huelle.LesenAsync(Probe("ifc4_g5_wand_extrusion.ifc"), null!, CancellationToken.None);
        Assert.True(stand.Gelesen);
        GebaeudeNordrichtungDaten datei = huelle.NordrichtungDaten()!;
        Assert.Equal("DATEI", datei.Herkunft);
        Assert.Equal("aus der Datei", datei.HerkunftText);
        Assert.NotNull(datei.DateiPlanoberseiteGrad);
        Assert.Equal(datei.DateiPlanoberseiteGrad!.Value, datei.PlanoberseiteGrad, 9);
        Assert.Equal(Nordrichtung.NordwinkelAusPlanoberseite(datei.PlanoberseiteGrad)!.Value, datei.NordwinkelGrad, 9);
        Assert.StartsWith("Die Datei nennt die Nordrichtung", datei.DateiText);
        Assert.Equal("Planoberseite zeigt nach", datei.Beschriftung);
        Assert.Equal(new[] { "Nord", "Nordost", "Ost", "Südost", "Süd", "Südwest", "West", "Nordwest" }, datei.Schnellwahl.Select(s => s.Name));
        Assert.Equal(new[] { "N", "NO", "O", "SO", "S", "SW", "W", "NW" }, datei.Schnellwahl.Select(s => s.Kuerzel));

        var anfrage = new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>());
        GebaeudeImportStand standNord = huelle.ZuordnenMitNordrichtung(anfrage, 0.0);
        Assert.NotEmpty(standNord.Raeume);
        List<double?> nord = Azimute(standNord);
        Assert.Contains(nord, a => a.HasValue);
        Assert.Equal("EINGABE", huelle.NordrichtungDaten()!.Herkunft);

        // Planoberseite nach Süd: jeder Azimut dreht um 180°, ein fehlender bleibt fehlend.
        GebaeudeImportStand standSued = huelle.ZuordnenMitNordrichtung(anfrage, 180.0);
        Assert.NotEmpty(standSued.Raeume);
        List<double?> sued = Azimute(standSued);
        Assert.Equal(nord.Count, sued.Count);
        for (int i = 0; i < nord.Count; i++)
            if (nord[i] is double a) Assert.True(Math.Abs(Math.IEEERemainder(a + 180.0 - sued[i]!.Value, 360.0)) <= 0.11, a + " → " + sued[i]);
            else Assert.Null(sued[i]);
        GebaeudeNordrichtungDaten eingabe = huelle.NordrichtungDaten()!;
        Assert.Equal(180.0, eingabe.PlanoberseiteGrad, 9);
        Assert.Equal(180.0, eingabe.NordwinkelGrad, 9);
        Assert.Equal("eingegeben", eingabe.HerkunftText);
        Assert.Equal(datei.DateiPlanoberseiteGrad, eingabe.DateiPlanoberseiteGrad);   // der Dateiwert bleibt als Angabe

        // Ost über den Arbeitsfaden, dann zurück auf den Dateiwert.
        Assert.True((await huelle.NordrichtungSetzenAsync(90.0, CancellationToken.None)).Gelesen);
        Assert.Equal(270.0, huelle.NordrichtungDaten()!.NordwinkelGrad, 9);
        Assert.True(huelle.NordrichtungSetzen(null).Gelesen);
        Assert.Equal("DATEI", huelle.NordrichtungDaten()!.Herkunft);
    }

    [Fact]
    public async Task Ohne_Nordrichtung_der_Datei_gilt_die_Annahme()
    {
        var huelle = new GebaeudeImportHuelle();
        Assert.True((await huelle.LesenAsync(Probe("gbxml_haus_si.xml"), null!, CancellationToken.None)).Gelesen);
        GebaeudeNordrichtungDaten d = huelle.NordrichtungDaten()!;
        Assert.Null(d.DateiPlanoberseiteGrad);
        Assert.Equal("Die Datei nennt keine Nordrichtung.", d.DateiText);
        Assert.Equal("ANNAHME", d.Herkunft);
        Assert.Equal("angenommen (Planoberseite = Nord)", d.HerkunftText);
        Assert.Equal(0.0, d.PlanoberseiteGrad, 9);
        Assert.Equal(0.0, d.NordwinkelGrad, 9);
    }
}
