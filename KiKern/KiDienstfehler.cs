using System;
using System.Globalization;

namespace KiKern
{
    /// <summary>
    /// Was der Anwender liest, wenn der Modelldienst nicht antwortet - Anwenderbefund
    /// <b>W15b-B-2</b> der Windows-Abnahme vom 05.09.2026
    /// („Hilfeassistent funktioniert nicht bei Fragen").
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Befund.</b> Im Chatfenster stand woertlich: <i>„Die Anfrage konnte nicht
    /// beantwortet werden: HTTP 401 - Request had invalid authentication credentials.
    /// Expected OAuth 2 access token, login cookie or other valid authentication
    /// credential. See https://developers.google.com/identity/sign-in/web/devconsole-project."</i>
    /// Das ist der Rohtext des Anbieters, mitsamt einer Adresse, die in eine
    /// Entwicklerkonsole fuehrt. Er sagt dem Anwender nicht, was er tun soll - und er
    /// nennt nicht die eine Stelle, an der es zu tun waere.
    /// </para>
    /// <para>
    /// <b>Zwei Dinge trennt diese Klasse.</b> Der ANWENDERSATZ sagt in einem Satz, was
    /// los ist und wo es zu beheben ist; der ROHTEXT bleibt vollstaendig erhalten und
    /// geht ins Protokoll („Protokoll anzeigen"). Nichts geht verloren, aber nichts
    /// Rohes steht mehr im Verlauf.
    /// </para>
    /// <para>
    /// <b>Der Sonderfall 0 ist der wichtigste.</b> Er heisst: Es ging gar keine Anfrage
    /// hinaus, weil kein Zugangsschluessel vorliegt. Genau diese Lage beantwortete der
    /// Anbieter bisher mit 401 („Expected OAuth 2 access token" - die Antwort auf eine
    /// Anfrage OHNE Schluessel; ein FALSCHER Schluessel ergaebe 400), und der Anwender
    /// bekam einen Netzfehler zu lesen, obwohl das Programm die Lage kannte, bevor es
    /// sendete. Der Riegel dagegen steht in <c>KiChatService.SendenAsync</c> - der EINEN
    /// Stelle, durch die jede Anfrage geht.
    /// </para>
    /// <para>
    /// Die Texte stehen in <see cref="KiTexte"/> und kommen ueber dessen Lieferanten
    /// zweisprachig aus <c>MyResource</c>; der Kern selbst kennt keine Ressourcendatei.
    /// </para>
    /// </remarks>
    public static class KiDienstfehler
    {
        /// <summary>Kennzahl fuer „es ging gar keine Anfrage hinaus".</summary>
        public const int OhneAnfrage = 0;

        /// <summary>
        /// Der Satz, den der Anwender im Verlauf liest.
        /// </summary>
        /// <param name="status">HTTP-Kennzahl; <see cref="OhneAnfrage"/> = kein Schluessel.</param>
        public static string Anwendersatz(int status)
        {
            if (status == OhneAnfrage) return KiTexte.DienstKeinSchluessel;

            string vorlage;
            if (status == 400) vorlage = KiTexte.DienstSchluesselUngueltig;
            else if (status == 401 || status == 403) vorlage = KiTexte.DienstAbgelehnt;
            else if (status == 429) vorlage = KiTexte.DienstKontingent;
            else if (status >= 500 && status <= 599) vorlage = KiTexte.DienstGestoert;
            else vorlage = KiTexte.DienstUnbekannt;

            return Fuellen(vorlage, status.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Die Protokollzeile mit dem ROHTEXT des Anbieters - vollstaendig und
        /// ungekuerzt. Sie steht unter „Protokoll anzeigen", nicht im Verlauf.
        /// </summary>
        public static string Protokollzeile(int status, string? rohtext)
        {
            string kennzahl = status == OhneAnfrage
                ? "-"
                : status.ToString(CultureInfo.InvariantCulture);

            string vorlage = KiTexte.DienstProtokollzeile;
            string text = (rohtext ?? "").Trim();

            try
            {
                return string.Format(CultureInfo.InvariantCulture, vorlage, kennzahl, text);
            }
            catch (FormatException)
            {
                return kennzahl + ": " + text;
            }
        }

        /// <summary>
        /// Setzt die Kennzahl in eine Vorlage ein. Eine Vorlage OHNE Platzhalter bleibt
        /// unveraendert - eine Uebersetzung, die ihn weglaesst, soll keinen Fehler
        /// ausloesen (dieselbe Milde wie <see cref="KiTexte.Hole"/>).
        /// </summary>
        private static string Fuellen(string vorlage, string kennzahl)
        {
            try
            {
                return string.Format(CultureInfo.InvariantCulture, vorlage, kennzahl);
            }
            catch (FormatException)
            {
                return vorlage;
            }
        }
    }

    /// <summary>
    /// Eine Absage des Modelldienstes: der Anwendersatz als <see cref="Exception.Message"/>,
    /// die Kennzahl und der Rohtext daneben (Anwenderbefund <b>W15b-B-2</b>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum eine eigene Ausnahme.</b> Der Dienst warf bisher eine
    /// <see cref="Exception"/> mit dem Text <c>"HTTP 401 - " + Rohtext</c>; der Aufrufer
    /// setzte diesen Text unbesehen in den Verlauf. Damit gab es keinen Ort, an dem sich
    /// Anwendersatz und Rohtext noch trennen liessen. Jetzt traegt die Ausnahme beides,
    /// und der Aufrufer entscheidet, was wohin geht.
    /// </para>
    /// <para>
    /// <b><see cref="Rohtext"/> bleibt vollstaendig.</b> Er ist der Nachweis dafuer, was
    /// der Anbieter tatsaechlich geantwortet hat - er wird weder gekuerzt noch geschoent.
    /// </para>
    /// </remarks>
    public sealed class KiDienstAusnahme : Exception
    {
        /// <summary>Erzeugt die Absage.</summary>
        /// <param name="status">HTTP-Kennzahl; <see cref="KiDienstfehler.OhneAnfrage"/> = nicht gesendet.</param>
        /// <param name="rohtext">Der Wortlaut des Anbieters.</param>
        public KiDienstAusnahme(int status, string? rohtext)
            : base(KiDienstfehler.Anwendersatz(status))
        {
            Status = status;
            Rohtext = rohtext ?? "";
        }

        /// <summary>HTTP-Kennzahl; 0 = es ging keine Anfrage hinaus.</summary>
        public int Status { get; }

        /// <summary>Der ungekuerzte Wortlaut des Anbieters.</summary>
        public string Rohtext { get; }

        /// <summary>Die Zeile fuer das Protokoll.</summary>
        public string Protokollzeile() => KiDienstfehler.Protokollzeile(Status, Rohtext);
    }
}
