using System.Globalization;
using System.Threading;
using EPOS.UI.Dienste;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Das VORGABEMASS eines Fensters — Anwenderwunsch vom 05.09.2026,
/// „Admin-Menüs sind nicht an Größe Bildschirm angepasst".
///
/// <para><b>Warum dieser Fall hier steht.</b> Die Regel gehört zur
/// Windows-Hülle <c>BlazorDialogForm</c>, die in einem
/// <c>net10.0-windows</c>-Projekt liegt — ein Test, der es referenziert, liefe
/// weder auf ubuntu noch auf macOS. Die RECHNUNG ist deshalb eine
/// plattformfreie statische Methode in <c>EPOS.UI</c>
/// (<see cref="Fenstermass.Vorgabe"/>); die Hülle besorgt nur noch den
/// Arbeitsbereich. Denselben Schnitt macht <c>ParametersatzTests</c>.</para>
///
/// <para>Keine Sprachbindung: geprüft werden ausschließlich Zahlen. Die
/// Kultur wird trotzdem gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public sealed class FenstermassTests
{
    public FenstermassTests() => DeutscheOberflaeche();

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    /// <summary>Ein üblicher Schirm: 1920 × 1080 mit 40 px Taskleiste.</summary>
    private const int ARBEIT_BREITE = 1920;
    private const int ARBEIT_HOEHE = 1040;

    // =====================================================================
    //  Der Befund: eine Fachmaske war so klein wie ihr Wunschmaß
    // =====================================================================

    /// <summary>
    /// Der Katalogdialog „Administration Solarkollektoren" wünscht 760 × 640
    /// (<c>SolarkollektorHuelle.KATALOG_MASS</c>) und blieb bis zum 05.09.2026
    /// auch auf einem 1920er Schirm genau so groß. Seither nimmt er den Anteil.
    /// </summary>
    [Fact]
    public void Eine_Fachmaske_waechst_auf_den_Anteil_des_Arbeitsbereichs()
    {
        (int breite, int hoehe) = Fenstermass.Vorgabe(760, 640, ARBEIT_BREITE, ARBEIT_HOEHE);

        Assert.Equal(1632, breite);      // 85 % von 1920
        Assert.Equal(896, hoehe);        // 90 % von 1040, minus Rahmen und Titelleiste
    }

    /// <summary>
    /// Der Anteil ist eine UNTERGRENZE. Wer mehr wünscht — der Assistent
    /// 1264 × 900, das Simulationsergebnis 1474 × 821 —, behält seinen Wunsch,
    /// solange er unter den Deckel passt.
    /// </summary>
    [Fact]
    public void Ein_groesserer_Wunsch_bleibt_erhalten()
    {
        (int breite, int hoehe) = Fenstermass.Vorgabe(1700, 950, ARBEIT_BREITE, ARBEIT_HOEHE);

        Assert.Equal(1700, breite);      // größer als 1632 und kleiner als der Deckel
        Assert.Equal(916, hoehe);        // Deckel: 92 % von 1040 minus 40
    }

    /// <summary>
    /// Der DECKEL bleibt, wie er am 03.09.2026 eingeführt wurde: 92 % des
    /// Arbeitsbereichs. Ein Fachdialog mit 914 px Breite war auf dem
    /// Anwenderrechner zusammengequetscht, weil er größer war als der Schirm.
    /// </summary>
    [Fact]
    public void Der_Deckel_haelt_das_Fenster_auf_dem_Schirm()
    {
        (int breite, int hoehe) = Fenstermass.Vorgabe(4000, 3000, ARBEIT_BREITE, ARBEIT_HOEHE);

        Assert.Equal(1766, breite);      // 92 % von 1920
        Assert.Equal(916, hoehe);        // 92 % von 1040, minus 40
    }

    // =====================================================================
    //  Die kleinen Masken wachsen NICHT mit
    // =====================================================================

    /// <summary>
    /// Namensabfrage, Erststart, Lizenztext und die zwei KI-Masken bleiben bei
    /// ihrem Wunschmaß — für sie gilt genau das, was vor dem 05.09.2026 für
    /// alle galt.
    /// </summary>
    [Theory]
    [InlineData(520, 360)]      // NamensDialogHuelle.FENSTER
    [InlineData(620, 480)]      // KiEinstellungenHuelle.MASS
    [InlineData(700, 600)]      // KiHinweisHuelle.MASS
    [InlineData(760, 560)]      // ErststartHuelle.MASS
    [InlineData(980, 760)]      // LizenzHuelle.MASS
    public void Eine_kleine_Maske_bleibt_bei_ihrem_Wunschmass(int wunschBreite, int wunschHoehe)
    {
        (int breite, int hoehe) = Fenstermass.Vorgabe(
            wunschBreite, wunschHoehe, ARBEIT_BREITE, ARBEIT_HOEHE, Dialogart.Klein);

        Assert.Equal(wunschBreite, breite);
        Assert.Equal(wunschHoehe, hoehe);
    }

    /// <summary>Auch eine kleine Maske bleibt auf dem Schirm.</summary>
    [Fact]
    public void Auch_eine_kleine_Maske_liegt_unter_dem_Deckel()
    {
        (int breite, int hoehe) = Fenstermass.Vorgabe(
            4000, 3000, ARBEIT_BREITE, ARBEIT_HOEHE, Dialogart.Klein);

        Assert.Equal(1766, breite);
        Assert.Equal(916, hoehe);
    }

    /// <summary>Die Vorgabe ist der Fachdialog — eine Hülle muss nichts bestellen.</summary>
    [Fact]
    public void Ohne_Angabe_gilt_der_Fachdialog()
    {
        Assert.Equal(Fenstermass.Vorgabe(760, 640, ARBEIT_BREITE, ARBEIT_HOEHE, Dialogart.Fachdialog),
                     Fenstermass.Vorgabe(760, 640, ARBEIT_BREITE, ARBEIT_HOEHE));
    }

    // =====================================================================
    //  Die Grenzfälle
    // =====================================================================

    /// <summary>
    /// Auf einem winzigen Schirm bleibt das Kleinstmaß stehen — sonst gäbe es
    /// ein Fenster ohne Dialogkopf. <c>MinimumSize</c> der Hülle trägt dieselben
    /// zwei Zahlen.
    /// </summary>
    [Fact]
    public void Unter_das_Kleinstmass_geht_es_nie()
    {
        (int breite, int hoehe) = Fenstermass.Vorgabe(300, 200, 400, 300);

        Assert.Equal(Fenstermass.MindestBreite, breite);
        Assert.Equal(Fenstermass.MindestHoehe, hoehe);
    }

    /// <summary>
    /// Die drei Anteile stehen in der erwarteten Ordnung: Der Deckel liegt
    /// ÜBER den zwei Anteilen — sonst machte der Anteil ein großes Fenster
    /// kleiner, statt ein kleines größer.
    /// </summary>
    [Fact]
    public void Der_Deckel_liegt_ueber_den_Anteilen()
    {
        Assert.True(Fenstermass.Deckel > Fenstermass.AnteilBreite);
        Assert.True(Fenstermass.Deckel > Fenstermass.AnteilHoehe);
    }

    /// <summary>
    /// <b>Die Zahlen der Windows-Abnahme.</b> Was bei 100 / 125 / 150 %
    /// Skalierung auf einem 1920er Schirm in der WebView ankommt: Geräteixel
    /// geteilt durch die Skalierung. Alle drei liegen über der Umbruchbreite
    /// 900 px des Katalograhmens — der Anwender sieht Liste und Eingabe also
    /// bei jeder der drei Stufen nebeneinander.
    /// </summary>
    [Theory]
    [InlineData(1.00, 1632)]
    [InlineData(1.25, 1305)]
    [InlineData(1.50, 1088)]
    public void Bei_100_125_und_150_Prozent_bleibt_das_Fenster_ueber_der_Umbruchbreite(
        double skalierung, int erwarteteCssBreite)
    {
        (int breite, _) = Fenstermass.Vorgabe(760, 640, ARBEIT_BREITE, ARBEIT_HOEHE);

        Assert.Equal(erwarteteCssBreite, (int)(breite / skalierung));
        Assert.True(erwarteteCssBreite > 900, "unter der Umbruchbreite des Katalograhmens");
    }
}
