using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// <b>Die Spalten der zwei IMPORTMASKEN als Katalogfilterprofil</b> (Stufe
/// <b>S3.4</b> des Anwenderentscheids <b>W14a‑E‑10</b> vom 07.09.2026, Frage
/// <b>Q10</b>).
///
/// <para><b>Was hier festgehalten wird.</b> Beide Importprofile liefern ihr
/// <c>Listenprofil</c> fertig aus — dieselben Spalten, die die Maske schon vorher
/// zeigte, nur jetzt als DATEN mit Trichter und Sortierpfeil. Entscheidend sind
/// dabei zwei Dinge, die man leicht falsch macht: Die Größen der gefallenen
/// Zahlenleisten müssen <b>Zahlenspalten</b> sein (sonst verglichen sie „9" &gt;
/// „10" als Zeichenkette und ein <c>Zahlenausdruck</c> träfe nichts), und jeder
/// Katalog braucht seinen <b>eigenen Schlüssel</b> — sonst teilten sich zwei
/// Importe einen Filterstand.</para>
///
/// <para>Ohne Datenbank und ohne Oberfläche: Es sind Daten.</para>
/// </summary>
public class KatalogfilterImportTests
{
    // =====================================================================
    //  KatalogImportProfil - die fünf Ausprägungen des Katalogimports
    // =====================================================================

    /// <summary>
    /// Die vier VDI-Ausprägungen zeigen drei Spalten: Eintrag, Hersteller und die
    /// GEFILTERTE Größe. Die dritte ist neu sichtbar — sie stand vorher nur in der
    /// Filterleiste („Th. Leistung [kW] von: … bis: …"), war also filterbar, ohne
    /// dass man sie sah.
    /// </summary>
    [Theory]
    [InlineData(KatalogImportArt.Heizkessel, "Th. Leistung", "kW")]
    [InlineData(KatalogImportArt.Pufferspeicher, "Volumen", "l")]
    [InlineData(KatalogImportArt.Solarkollektoren, "Aperturfläche", "m²")]
    [InlineData(KatalogImportArt.Waermepumpe, "Th. Leistung", "kW")]
    public void Die_vier_VDI_Auspraegungen_zeigen_Eintrag_Hersteller_und_die_Groesse(
        KatalogImportArt art, string titel, string einheit)
    {
        Katalogfilterprofil p = KatalogImportProfil.Finde(art, Text).Listenprofil;

        Assert.Equal(3, p.Spalten.Count);
        Assert.Equal(KatalogImportProfil.SpalteEintrag, p.Spalten[0].Schluessel);
        Assert.Equal(KatalogImportProfil.FeldFirma, p.Spalten[1].Schluessel);
        Assert.Equal(KatalogImportProfil.SpalteFilterwert, p.Spalten[2].Schluessel);

        // Der Spaltenkopf traegt KEINEN Doppelpunkt - er ist keine Feldbeschriftung.
        Assert.Equal(titel + " [" + einheit + "]", p.Spalten[2].Kopftext);
        Assert.DoesNotContain(":", p.Spalten[2].Kopftext);

        // Und er ist eine ZAHLENspalte, sonst traefe "10..200" nichts.
        Assert.Equal(Katalogspaltenart.Zahl, p.Spalten[2].Art);
        Assert.True(p.Spalten[2].Filterbar);
    }

    /// <summary>
    /// Der Stromspeicher bringt seine eigenen sieben Listenspalten mit; dort werden
    /// die zwei Größen der gefallenen Zahlenleiste — Kapazität und Leistung — zu
    /// Zahlenspalten, alles andere bleibt Text.
    /// </summary>
    [Fact]
    public void Der_Stromspeicher_macht_seine_zwei_Filtergroessen_zu_Zahlenspalten()
    {
        KatalogImportProfil profil = KatalogImportProfil.Finde(
            KatalogImportArt.Stromspeicher, Text);
        Katalogfilterprofil p = profil.Listenprofil;

        // Eintrag plus die sieben Listenspalten.
        Assert.Equal(8, p.Spalten.Count);
        Assert.Equal(KatalogImportProfil.SpalteEintrag, p.Spalten[0].Schluessel);

        Assert.Equal(Katalogspaltenart.Zahl, p.Spalte(profil.Filterspalte).Art);
        Assert.Equal(Katalogspaltenart.Zahl, p.Spalte(profil.Zweitfilterspalte).Art);
        Assert.Equal("ENERGIE", profil.Filterspalte);
        Assert.Equal("LEISTUNG", profil.Zweitfilterspalte);

        Assert.Equal(Katalogspaltenart.Text, p.Spalte(KatalogImportProfil.FeldFirma).Art);
        Assert.Equal(Katalogspaltenart.Text, p.Spalte("TYP").Art);
    }

    /// <summary>
    /// <b>Jeder Import trägt seinen eigenen Filterschlüssel.</b> Zwei Kataloge unter
    /// einem Schlüssel teilten sich einen Filterstand — beim Import wäre das
    /// besonders unerklärlich, weil er Kandidaten aus einer DATEI zeigt.
    /// </summary>
    [Fact]
    public void Jeder_Katalogimport_fuehrt_seinen_eigenen_Schluessel()
    {
        string[] schluessel = KatalogImportProfil.AlleArten
            .Select(a => KatalogImportProfil.Finde(a, Text).Listenprofil.Schluessel)
            .ToArray();

        Assert.Equal(5, schluessel.Length);
        Assert.Equal(schluessel.Length, schluessel.Distinct().Count());
        Assert.All(schluessel, s => Assert.StartsWith("IMPORT_", s));
    }

    // =====================================================================
    //  ModulImportProfil - Module und Wechselrichter
    // =====================================================================

    /// <summary>
    /// Der Modulimport behält seine zehn Spalten; Leistung und Effizienz — die zwei
    /// Größen der gefallenen Zahlenleiste — werden Zahlenspalten.
    /// </summary>
    [Fact]
    public void Der_Modulimport_traegt_zehn_Spalten_mit_zwei_Zahlengroessen()
    {
        ModulImportProfil profil = ModulImportProfil.Finde(ModulImportArt.Photovoltaik, Text);
        Katalogfilterprofil p = profil.Listenprofil;

        Assert.Equal(profil.Spalten.Count, p.Spalten.Count);
        Assert.Equal(10, p.Spalten.Count);

        Assert.Equal(new[] { ModulImportProfil.SpaltePmp, ModulImportProfil.SpalteEffizienz },
                     profil.Zahlspalten);
        Assert.Equal(Katalogspaltenart.Zahl, p.Spalte(ModulImportProfil.SpaltePmp).Art);
        Assert.Equal(Katalogspaltenart.Zahl, p.Spalte(ModulImportProfil.SpalteEffizienz).Art);
        Assert.Equal(Katalogspaltenart.Text, p.Spalte(ModulImportProfil.SpalteHersteller).Art);
    }

    /// <summary>
    /// Der Wechselrichterimport hat sieben Spalten und EINE Zahlengröße — er führte
    /// auch nur einen Zahlenbereich.
    /// </summary>
    [Fact]
    public void Der_Wechselrichterimport_traegt_sieben_Spalten_mit_einer_Zahlengroesse()
    {
        ModulImportProfil profil = ModulImportProfil.Finde(ModulImportArt.Wechselrichter, Text);
        Katalogfilterprofil p = profil.Listenprofil;

        Assert.Equal(7, p.Spalten.Count);
        Assert.Equal(new[] { ModulImportProfil.SpaltePAc }, profil.Zahlspalten);
        Assert.Equal(Katalogspaltenart.Zahl, p.Spalte(ModulImportProfil.SpaltePAc).Art);

        // Der Wechselrichter fuehrt KEINE Technologie - die Spalte gibt es nicht.
        Assert.Null(p.Spalte(ModulImportProfil.SpalteTechnologie));
    }

    /// <summary>Auch die zwei Geräteimporte trennen ihre Filterstände.</summary>
    [Fact]
    public void Die_zwei_Geraeteimporte_fuehren_getrennte_Schluessel()
    {
        string pv = ModulImportProfil.Finde(ModulImportArt.Photovoltaik, Text)
                                     .Listenprofil.Schluessel;
        string wr = ModulImportProfil.Finde(ModulImportArt.Wechselrichter, Text)
                                     .Listenprofil.Schluessel;

        Assert.NotEqual(pv, wr);
        Assert.StartsWith("IMPORT_", pv);
        Assert.StartsWith("IMPORT_", wr);
    }

    /// <summary>
    /// Der Übersetzer der Prüfstände: der Ressourcentext, damit die Kopftexte oben so
    /// dastehen, wie der Anwender sie sieht. Ein unbekannter Schlüssel bleibt stehen —
    /// dieselbe Regel wie in <c>EPOS.UI.Dialoge.Import.Texte.Zu</c>.
    /// </summary>
    private static string Text(string schluessel)
        => WindowsFormsApplication1.MyResource.Resource.ResourceManager
               .GetString(schluessel, System.Globalization.CultureInfo.GetCultureInfo("de-DE"))
           ?? schluessel;
}
