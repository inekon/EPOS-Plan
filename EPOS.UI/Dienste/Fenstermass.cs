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
    Klein
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
/// Deckel - fuer sie gilt genau das, was vorher fuer alle galt.</para>
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

    /// <summary>
    /// Das Innenmass, mit dem ein Fenster oeffnet.
    /// </summary>
    /// <param name="wunschBreite">Wunschmass der Huelle (<c>MASS</c>), Breite.</param>
    /// <param name="wunschHoehe">Wunschmass der Huelle (<c>MASS</c>), Hoehe.</param>
    /// <param name="arbeitBreite">Arbeitsbereich des Bildschirms, Breite.</param>
    /// <param name="arbeitHoehe">Arbeitsbereich des Bildschirms, Hoehe.</param>
    /// <param name="art">Fachmaske oder kleine Maske.</param>
    /// <returns>Breite und Hoehe des Innenmasses.</returns>
    public static (int Breite, int Hoehe) Vorgabe(
        int wunschBreite, int wunschHoehe,
        int arbeitBreite, int arbeitHoehe,
        Dialogart art = Dialogart.Fachdialog)
    {
        int deckelBreite = Math.Max(MindestBreite, (int)(arbeitBreite * Deckel));
        int deckelHoehe = Math.Max(MindestHoehe, (int)(arbeitHoehe * Deckel) - Fensterrahmen);

        int breite = wunschBreite;
        int hoehe = wunschHoehe;

        if (art == Dialogart.Fachdialog)
        {
            // Der Anteil ist eine UNTERGRENZE, keine Vorschrift: Wer mehr
            // wuenscht (Assistent, Simulationsergebnis), behaelt seinen Wunsch.
            breite = Math.Max(breite, (int)(arbeitBreite * AnteilBreite));
            hoehe = Math.Max(hoehe, (int)(arbeitHoehe * AnteilHoehe) - Fensterrahmen);
        }

        return (Math.Max(MindestBreite, Math.Min(breite, deckelBreite)),
                Math.Max(MindestHoehe, Math.Min(hoehe, deckelHoehe)));
    }
}
