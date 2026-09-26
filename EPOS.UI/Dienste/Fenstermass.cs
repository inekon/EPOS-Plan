using System;

namespace EPOS.UI.Dienste;

/// <summary>
/// Die zwei Bauarten eines Fensters - die eine waechst mit dem Bildschirm,
/// die andere nicht (Anwenderwunsch 05.09.2026, Entscheid <b>iU8-E-1</b>).
/// </summary>
public enum Dialogart
{
    /// <summary>
    /// Eine FACHMASKE: Katalogverwaltung, Erzeugerdialog, Bedarfsmaske,
    /// Assistent. Sie oeffnet im Anteil des Arbeitsbereichs und nutzt den
    /// Platz, den der Bildschirm hergibt. Vorgabe.
    /// </summary>
    Fachdialog,

    /// <summary>
    /// Eine KLEINE Maske: Namensabfrage, Rueckfrage, Erststart, Lizenztext,
    /// KI-Hinweis. Sie bleibt bei ihrem Wunschmass - ein Fenster mit vier
    /// Feldern ueber den halben Bildschirm zu ziehen macht es nicht besser,
    /// sondern nur leerer.
    /// </summary>
    Klein,

    /// <summary>
    /// Ein Fenster, dessen Wunschmass AUS SEINEM INHALT abgeleitet ist - in
    /// CSS-Pixeln, wie die WebView ihn zeichnet: die Projektwahl („Projekt
    /// öffnen", „Projekt löschen") und „Speichern unter" (<see cref="Fenstermass.Projektdialog"/>).
    /// Es waechst NICHT mit dem Bildschirm - eine Liste mit acht Zeilen und vier
    /// Feldern bekommt auf dem Anteil einer Fachmaske nur leere Flaeche unter und
    /// neben sich -, aber es waechst mit der Skalierung, weil der Inhalt es tut.
    /// </summary>
    Inhaltsmass
}

/// <summary>
/// Das VORGABEMASS eines Fensters beim Oeffnen - die Rechnung hinter
/// <c>BlazorDialogForm</c> (Anwenderwunsch 05.09.2026: „Admin-Menues sind
/// nicht an Groesse Bildschirm angepasst").
///
/// <para><b>Warum die Rechnung hier steht und nicht in der Huelle.</b> Sie ist
/// reine Arithmetik auf vier ganzen Zahlen - kein WinForms, kein
/// <c>System.Drawing</c>, kein Bildschirm. So laesst sie sich in
/// <c>EPOS.UI.Tests</c> auf jedem Betriebssystem pruefen; die Huelle in
/// <c>WindowsFormsApplication1</c> liefert nur noch den Arbeitsbereich und
/// nimmt das Ergebnis entgegen. Fuer eine iOS-Schale ist sie gegenstandslos -
/// dort gibt es kein Fenstermass -, aber sie kostet dort auch nichts.</para>
///
/// <para><b>Die Regel.</b> Bis zum 05.09.2026 galt nur ein DECKEL: Das
/// Wunschmass wurde auf 92 % des Arbeitsbereichs geklemmt (Befund 03.09.2026 -
/// ein Fachdialog mit 914 px Breite war auf dem Anwenderrechner
/// zusammengequetscht). Der Deckel half nur nach oben. Ein Katalogdialog mit
/// dem Wunsch 760 x 640 blieb deshalb auch auf einem 1920er Schirm 760 x 640
/// gross: Liste winzig, Eingabeblock nur ueber den Seitenrollbalken zu
/// erreichen. Seither ist das Vorgabemass das MAXIMUM aus Wunschmass und
/// einem ANTEIL des Arbeitsbereichs (85 % Breite, 90 % Hoehe), wieder auf den
/// Deckel geklemmt. Eine <see cref="Dialogart.Klein"/>e Maske nimmt nur den
/// Deckel - fuer sie gilt genau das, was vorher fuer alle galt. Ein Fenster mit
/// <see cref="Dialogart.Inhaltsmass"/> nimmt sein Wunschmass mal Skalierung,
/// ebenfalls nur gedeckelt.</para>
///
/// <para><b>Einheit.</b> Alle vier Zahlen sind GERAETEPIXEL desselben
/// Bildschirms. Unter „Per Monitor V2" (Entscheid E-6 / iF21) stehen
/// <c>Screen.WorkingArea</c> und <c>Form.ClientSize</c> im selben Raum; es
/// steht deshalb kein Skalierungsfaktor in dieser Rechnung. Was die WebView
/// daraus an CSS-Pixeln macht, ist Geraetepixel geteilt durch die Skalierung -
/// bei 150 % also zwei Drittel. Genau deshalb bricht der Katalograhmen erst
/// bei 900 CSS-Pixeln um und nicht bei 1100: Sonst staende der Anwender mit
/// 150 % auf einem 1920er Schirm (1632 / 1,5 = 1088) wieder untereinander.</para>
/// </summary>
public static class Fenstermass
{
    /// <summary>Kleinstes sinnvolles Innenmass - darunter passt kein Dialogkopf mehr.</summary>
    public const int MindestBreite = 520;

    /// <inheritdoc cref="MindestBreite" />
    public const int MindestHoehe = 360;

    /// <summary>Anteil des Arbeitsbereichs, den eine Fachmaske in der BREITE nimmt.</summary>
    public const double AnteilBreite = 0.85;

    /// <summary>Anteil des Arbeitsbereichs, den eine Fachmaske in der HOEHE nimmt.</summary>
    public const double AnteilHoehe = 0.90;

    /// <summary>
    /// Der Deckel: mehr als das nimmt kein Fenster, auch wenn es mehr wuenscht
    /// (Befund 03.09.2026). Er liegt ueber den Anteilen, damit eine Maske mit
    /// einem sehr grossen Wunschmass - der Assistent will 1264 x 900 - nicht
    /// vom Anteil KLEINER gemacht wird, aber trotzdem auf den Schirm passt.
    /// </summary>
    public const double Deckel = 0.92;

    /// <summary>
    /// Rahmen und Titelleiste, die in der Hoehe zum Innenmass hinzukommen.
    /// Ohne diesen Abzug steht die Schlussleiste unter der Taskleiste.
    /// </summary>
    public const int Fensterrahmen = 40;

    // ---------------------------------------------------------------------
    //  Das Inhaltsmass der Projektdialoge (Projekt öffnen, Projekt löschen,
    //  Speichern unter). Alle Zahlen in CSS-Pixeln bei 100 %.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Breite der Projektdialoge: die Höchstbreite eines Dialogs im Stilblatt
    /// (<c>.epos-dialog { max-width: 1160px }</c>) und 10 px Luft je Seite. Bei
    /// 1 180 px stehen in „Speichern unter" Liste und Felder nebeneinander - der
    /// Umbruch des Rasters liegt bei 1 100 px.
    /// </summary>
    public const int ProjektdialogBreite = 1180;

    /// <summary>Zeilenhöhe der Projektliste (Wahlspalte mit 44-px-Knopf und Polster).</summary>
    public const int ProjektlisteZeile = 53;

    /// <summary>So viele Projektzeilen zeigt die Liste beim Öffnen, ohne zu rollen.</summary>
    public const int ProjektlisteSichtbareZeilen = 8;

    /// <summary>
    /// Alles an „Speichern unter" außer den Listenzeilen, gemessen an der Maske
    /// (26.09.2026, 100 %): Polster 2 × 16, Kopf 44, Gruppenkopf 40 + 15, Suche 66,
    /// Listenkopf samt Rahmen 30, Zählzeile 18, Knopfleiste 44 + 10, Lücken
    /// dazwischen. Die Projektwahl hat keinen Gruppenkopf - ihre Liste bekommt die
    /// 55 px zusätzlich, weil die Liste im Fenster die freie Höhe füllt.
    /// </summary>
    public const int ProjektdialogUmfeld = 342;

    /// <summary>
    /// Das Wunschmaß der Projektdialoge in CSS-Pixeln, abgeleitet aus dem Inhalt:
    /// 1 180 × 766 (8 Zeilen à 53 px und das Umfeld). Es geht mit
    /// <see cref="Dialogart.Inhaltsmass"/> an <see cref="Vorgabe"/> - als Fachdialog
    /// öffneten die Projektdialoge auf dem Anteil des Arbeitsbereichs (1 632 × 896 auf
    /// einem 1920er Schirm), und der Inhalt stand im oberen Drittel.
    /// </summary>
    public static (int Breite, int Hoehe) Projektdialog
        => (ProjektdialogBreite,
            ProjektdialogUmfeld + ProjektlisteSichtbareZeilen * ProjektlisteZeile);

    /// <summary>
    /// Das Innenmass, mit dem ein Fenster oeffnet.
    /// </summary>
    /// <param name="wunschBreite">Wunschmass der Huelle (<c>MASS</c>), Breite.</param>
    /// <param name="wunschHoehe">Wunschmass der Huelle (<c>MASS</c>), Hoehe.</param>
    /// <param name="arbeitBreite">Arbeitsbereich des Bildschirms, Breite.</param>
    /// <param name="arbeitHoehe">Arbeitsbereich des Bildschirms, Hoehe.</param>
    /// <param name="art">Fachmaske, kleine Maske oder Fenster mit Inhaltsmaß.</param>
    /// <param name="skalierung">Anzeigeskalierung des Bildschirms (1,0 = 100 %, 1,5 = 150 %).
    /// Sie wirkt NUR bei <see cref="Dialogart.Inhaltsmass"/>: Dort ist das Wunschmaß in
    /// CSS-Pixeln gemessen und wird hier in Gerätepixel umgerechnet. Ein Wert unter 1
    /// oder ungültig zählt als 1.</param>
    /// <returns>Breite und Hoehe des Innenmasses.</returns>
    public static (int Breite, int Hoehe) Vorgabe(
        int wunschBreite, int wunschHoehe,
        int arbeitBreite, int arbeitHoehe,
        Dialogart art = Dialogart.Fachdialog,
        double skalierung = 1.0)
    {
        int deckelBreite = Math.Max(MindestBreite, (int)(arbeitBreite * Deckel));
        int deckelHoehe = Math.Max(MindestHoehe, (int)(arbeitHoehe * Deckel) - Fensterrahmen);

        int breite = wunschBreite;
        int hoehe = wunschHoehe;

        if (art == Dialogart.Inhaltsmass)
        {
            // Das Inhaltsmass waechst mit der Skalierung, nicht mit dem Schirm.
            double faktor = double.IsFinite(skalierung) && skalierung > 1.0 ? skalierung : 1.0;
            breite = (int)Math.Round(breite * faktor);
            hoehe = (int)Math.Round(hoehe * faktor);
        }
        else if (art == Dialogart.Fachdialog)
        {
            // Der Anteil ist eine UNTERGRENZE, keine Vorschrift: Wer mehr
            // wuenscht (Assistent, Simulationsergebnis), behaelt seinen Wunsch.
            breite = Math.Max(breite, (int)(arbeitBreite * AnteilBreite));
            hoehe = Math.Max(hoehe, (int)(arbeitHoehe * AnteilHoehe) - Fensterrahmen);
        }

        return (Math.Max(MindestBreite, Math.Min(breite, deckelBreite)),
                Math.Max(MindestHoehe, Math.Min(hoehe, deckelHoehe)));
    }

    /// <summary>
    /// <b>Das Wunschmaß eines Fensters, das einen anderen Dialog als Überlagerung trägt</b>
    /// (Konzept Administrationsdialoge 7.1 d): mindestens das Wunschmaß dieses Dialogs,
    /// je Richtung. Eine <c>Ueberlagerung</c> kann nicht breiter werden als ihr Fenster —
    /// die breite nimmt 96 % davon —, und der Dialog darin hat sein Wunschmaß nicht aus
    /// Gewohnheit: Der Stromspeicherimport wünscht 1 180 px, weil seine Liste acht Spalten
    /// und seine Filterleiste vier Felder trägt. Im Modulkatalog mit 860 px bekam er als
    /// Überlagerung nur 826 px.
    /// </summary>
    /// <param name="wunschBreite">Wunschmaß des Fensters selbst, Breite.</param>
    /// <param name="wunschHoehe">Wunschmaß des Fensters selbst, Höhe.</param>
    /// <param name="ueberlagerungBreite">Wunschmaß des getragenen Dialogs als eigenes Fenster, Breite; 0 = keiner.</param>
    /// <param name="ueberlagerungHoehe">Wunschmaß des getragenen Dialogs als eigenes Fenster, Höhe; 0 = keiner.</param>
    /// <returns>Das Wunschmaß, das an <see cref="Vorgabe"/> geht.</returns>
    public static (int Breite, int Hoehe) MitUeberlagerung(
        int wunschBreite, int wunschHoehe, int ueberlagerungBreite, int ueberlagerungHoehe)
        => (Math.Max(wunschBreite, ueberlagerungBreite), Math.Max(wunschHoehe, ueberlagerungHoehe));
}
