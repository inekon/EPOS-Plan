using System;
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
/// Kultur wird trotzdem gepinnt (Hausregel seit iU9‑W8) — hier über ein
/// <see cref="Kulturvorrichtung"/>-Feld statt Erbschaft, weil die Klasse keine
/// <c>BunitContext</c> ist (Auftrag #168).</para>
/// </summary>
public sealed class FenstermassTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new();

    public void Dispose() => _kultur.Dispose();

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
    /// Namensabfrage, Lizenztext und die zwei KI-Masken bleiben bei ihrem
    /// Wunschmaß — für sie gilt genau das, was vor dem 05.09.2026 für alle galt.
    ///
    /// <para>Der fünfte Fall (760 × 560, <c>ErststartHuelle.MASS</c>) ist mit
    /// W3 entfallen: Der Erststart-Assistent ist gefallen, die Datenbank einer
    /// Neuinstallation entsteht ohne Oberfläche aus der ausgelieferten
    /// Vorlage.</para>
    /// </summary>
    [Theory]
    [InlineData(520, 360)]      // NamensDialogHuelle.FENSTER
    [InlineData(620, 480)]      // KiEinstellungenHuelle.MASS
    [InlineData(700, 600)]      // KiHinweisHuelle.MASS
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

    // =====================================================================
    //  Ein Fenster, das einen Import als Überlagerung trägt (Konzept 7.1 d)
    // =====================================================================

    /// <summary>
    /// <b>Der Stromspeicherimport im Modulkatalog.</b> Der Modulkatalog wünscht 860 × 780
    /// (<c>ModulKatalogHuelle.MASS</c>), der Stromspeicherimport als eigenes Fenster
    /// 1 180 × 700 (<c>KatalogImportHuelle.MASS_STROMSPEICHER</c>). Das Fenster, das ihn als
    /// Überlagerung trägt, wünscht deshalb 1 180 × 780 — je Richtung das größere.
    /// </summary>
    [Fact]
    public void Ein_Fenster_mit_Import_wuenscht_mindestens_dessen_Mass()
    {
        Assert.Equal((1180, 780), Fenstermass.MitUeberlagerung(860, 780, 1180, 700));
        Assert.Equal((1240, 800), Fenstermass.MitUeberlagerung(860, 780, 1240, 800));   // PV/WR: Modulimport
        Assert.Equal((900, 700), Fenstermass.MitUeberlagerung(900, 700, 900, 640));     // Katalogbrowser: gleich breit
        Assert.Equal((860, 780), Fenstermass.MitUeberlagerung(860, 780, 0, 0));         // ohne Import
    }

    /// <summary>
    /// <b>Wo es sich zeigt: auf einem kleinen Schirm.</b> Auf 1 280 × 1 024 (Arbeitsbereich
    /// 1 280 × 984) nahm der Modulkatalog nur den Anteil, 1 088 px — so breit wird dann
    /// auch die Import-Überlagerung höchstens. Mit dem Wunsch des Imports öffnet er so breit,
    /// wie der Deckel erlaubt (92 % = 1 177 px), und damit so breit wie der Import als
    /// eigenes Fenster. Auf dem 1920er Schirm ändert sich nichts: Dort nimmt ohnehin jedes
    /// Fenster den Anteil (1 632 px).
    /// </summary>
    [Fact]
    public void Auf_einem_kleinen_Schirm_oeffnet_der_Modulkatalog_so_breit_wie_sein_Import()
    {
        (int ohneBreite, _) = Fenstermass.Vorgabe(860, 780, 1280, 984);
        (int wunschBreite, int wunschHoehe) = Fenstermass.MitUeberlagerung(860, 780, 1180, 700);
        (int mitBreite, _) = Fenstermass.Vorgabe(wunschBreite, wunschHoehe, 1280, 984);
        (int importBreite, _) = Fenstermass.Vorgabe(1180, 700, 1280, 984);

        Assert.Equal(1088, ohneBreite);
        Assert.Equal(1177, mitBreite);
        Assert.Equal(importBreite, mitBreite);

        (int grossBreite, _) = Fenstermass.Vorgabe(wunschBreite, wunschHoehe, ARBEIT_BREITE, ARBEIT_HOEHE);
        Assert.Equal(1632, grossBreite);
    }

    // =====================================================================
    //  Anwenderbefund 26.09.2026: „Dialoggröße nicht angepasst — Kachel
    //  ‚Projekt öffnen' und ‚Speichern unter'". Als Fachdialog öffneten beide
    //  auf dem Anteil (1 632 × 896), der Inhalt stand im oberen Drittel.
    // =====================================================================

    /// <summary>
    /// Das Inhaltsmaß der Projektdialoge: 1 180 breit, acht Listenzeilen à 53 px
    /// plus Umfeld (Kopf, Suche, Listenkopf, Zählzeile, Knopfleiste) = 766 hoch.
    /// </summary>
    [Fact]
    public void Das_Inhaltsmass_der_Projektdialoge_kommt_aus_dem_Layout()
    {
        (int breite, int hoehe) = Fenstermass.Projektdialog;

        Assert.Equal(1180, breite);
        Assert.Equal(Fenstermass.ProjektdialogUmfeld
                     + Fenstermass.ProjektlisteSichtbareZeilen * Fenstermass.ProjektlisteZeile, hoehe);
        Assert.Equal(766, hoehe);
    }

    /// <summary>
    /// Der Befund selbst: Auf dem 1920er Schirm öffnen die Projektdialoge bei 100 %
    /// mit ihrem Inhaltsmaß, NICHT mit dem Anteil einer Fachmaske — und auf einem
    /// größeren Schirm genauso.
    /// </summary>
    [Theory]
    [InlineData(1920, 1040)]
    [InlineData(2560, 1400)]
    [InlineData(3840, 2120)]
    public void Die_Projektdialoge_wachsen_nicht_mit_dem_Bildschirm(int arbeitBreite, int arbeitHoehe)
    {
        (int wunschBreite, int wunschHoehe) = Fenstermass.Projektdialog;

        (int breite, int hoehe) = Fenstermass.Vorgabe(wunschBreite, wunschHoehe,
            arbeitBreite, arbeitHoehe, Dialogart.Inhaltsmass);

        Assert.Equal((1180, 766), (breite, hoehe));
        Assert.NotEqual(Fenstermass.Vorgabe(wunschBreite, wunschHoehe, arbeitBreite, arbeitHoehe),
                        (breite, hoehe));
    }

    /// <summary>
    /// Das Inhaltsmaß ist in CSS-Pixeln gemessen und wächst mit der Skalierung — bei
    /// 125 % auf dem 2560er Schirm 1 475 × 958. Auf dem 1920er Schirm mit 150 % hält
    /// der Deckel es auf dem Schirm (92 % = 1 766 × 916).
    /// </summary>
    [Fact]
    public void Das_Inhaltsmass_waechst_mit_der_Skalierung_bis_zum_Deckel()
    {
        (int wunschBreite, int wunschHoehe) = Fenstermass.Projektdialog;

        Assert.Equal((1475, 958), Fenstermass.Vorgabe(wunschBreite, wunschHoehe,
            2560, 1400, Dialogart.Inhaltsmass, 1.25));
        Assert.Equal((1766, 916), Fenstermass.Vorgabe(wunschBreite, wunschHoehe,
            ARBEIT_BREITE, ARBEIT_HOEHE, Dialogart.Inhaltsmass, 1.5));
    }

    /// <summary>
    /// Die Skalierung wirkt nur beim Inhaltsmaß; ein Unsinnswert zählt als 100 %.
    /// </summary>
    [Fact]
    public void Die_Skalierung_wirkt_nur_beim_Inhaltsmass()
    {
        Assert.Equal((700, 600), Fenstermass.Vorgabe(700, 600, ARBEIT_BREITE, ARBEIT_HOEHE,
            Dialogart.Klein, 1.5));
        Assert.Equal((1632, 896), Fenstermass.Vorgabe(760, 640, ARBEIT_BREITE, ARBEIT_HOEHE,
            Dialogart.Fachdialog, 1.5));
        Assert.Equal((1180, 766), Fenstermass.Vorgabe(1180, 766, ARBEIT_BREITE, ARBEIT_HOEHE,
            Dialogart.Inhaltsmass, double.NaN));
        Assert.Equal((1180, 766), Fenstermass.Vorgabe(1180, 766, ARBEIT_BREITE, ARBEIT_HOEHE,
            Dialogart.Inhaltsmass, 0.5));
    }

    /// <summary>
    /// WACHE über die Windows-Hüllen (sie liegen in einem <c>net10.0-windows</c>-Projekt
    /// und sind hier nur als Text greifbar): Beide Projektfenster wünschen
    /// <see cref="Fenstermass.Projektdialog"/> und öffnen als
    /// <see cref="Dialogart.Inhaltsmass"/>. Ohne die Art fiele das Fenster stumm auf
    /// den Fachdialog zurück — genau der Befund.
    /// </summary>
    [Theory]
    [InlineData("ProjektWahlHuelle.cs")]
    [InlineData("ProjektKopieFenster.cs")]
    public void Beide_Projektfenster_oeffnen_mit_dem_Inhaltsmass(string datei)
    {
        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Wurzel(), "WindowsFormsApplication1", "Views", "Projekt", datei));

        Assert.Contains("Fenstermass.Projektdialog.Breite", text);
        Assert.Contains("Fenstermass.Projektdialog.Hoehe", text);
        Assert.Contains("Dialogart.Inhaltsmass", text);
    }

    private static string Wurzel()
    {
        System.IO.DirectoryInfo? d = new(AppContext.BaseDirectory);
        while (d is not null && !System.IO.File.Exists(
                   System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }
}
