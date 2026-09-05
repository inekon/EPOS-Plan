using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Hilfe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Rechtshinweises (iU9-W15b.3) — Ersatz für
    /// <c>Views/Help/Form_KiHinweis.cs</c> (280 Z., ohne Designer).
    ///
    /// <para><b>Hier hängt die ganze Schutzzusage.</b> <see cref="Einhaengen"/> wird
    /// <b>einmal</b> in <c>Program.Main</c> gerufen und legt
    /// <see cref="KiEinwilligung.Nachfragen"/> auf <see cref="Einholen"/>. Der
    /// Kommentar des Vorläufers sagt, warum das die Abnahme ist: <i>„Ohne diesen
    /// Aufruf gibt es keinen Weg zu einer Einwilligung — und damit auch keine
    /// Übertragung. Genau darauf baut der Aktionsharnisch: er hängt nichts ein und
    /// weist damit nach, dass ohne Einwilligung nichts gesendet wird."</i> Das gilt
    /// unverändert; nur die Signatur ist seit W15b.0b asynchron (Befund
    /// W15b-B12).</para>
    ///
    /// <para><b>Der Faden-Umweg bleibt.</b> Der Riegel wird aus <c>KiChatService</c>
    /// heraus gezogen, und der läuft aus einem <c>await</c>-Kontext. Läuft er
    /// ausnahmsweise nicht auf dem Oberflächenstrang, muss der Dialog trotzdem dort
    /// erscheinen — <c>Control.Invoke</c> holt ihn hinüber, wörtlich wie in
    /// <c>Form_KiHinweis.Einholen</c> (<c>:70-75</c>).</para>
    ///
    /// <para><b>Die Texte holt die Hülle, nicht die Komponente.</b> Alle 17
    /// <c>KI_HINWEIS_*</c>-Schlüssel liegen zweisprachig vor; die Komponente bekommt
    /// sie als Parameter. Ändert sich einer inhaltlich, muss
    /// <see cref="KiEinwilligung.FASSUNG"/> erhöht werden — die Umstellung auf Razor
    /// hat keinen geändert (Entscheid E-4).</para>
    /// </summary>
    internal static class KiHinweisHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 660 × 560, Mindestmaß 560 × 420).</summary>
        private static readonly Size MASS = new Size(700, 600);

        /// <summary>
        /// Hängt diesen Dialog als Nachfrage der <see cref="KiEinwilligung"/> ein.
        /// Aufruf einmalig beim Programmstart (<c>Program.Main</c>).
        /// </summary>
        /// <remarks>
        /// Ohne diesen Aufruf gibt es keinen Weg zu einer Einwilligung - und damit auch
        /// keine Übertragung.
        /// </remarks>
        public static void Einhaengen()
        {
            KiEinwilligung.Nachfragen = () => Task.FromResult(Einholen());
        }

        /// <summary>
        /// Zeigt den Hinweis zur Bestätigung. Rückgabe <c>true</c> = eingewilligt.
        /// Merkt die Einwilligung NICHT selbst - das tut <see cref="KiEinwilligung"/>.
        /// </summary>
        public static bool Einholen(IWin32Window besitzer = null)
        {
            Control anker = (besitzer as Control) ?? Form.ActiveForm;

            // Der Riegel wird aus dem Dienst heraus gezogen. Läuft der ausnahmsweise
            // nicht auf dem Oberflächenstrang, muss der Dialog trotzdem dort erscheinen.
            if (anker != null && anker.InvokeRequired)
                return (bool)anker.Invoke(new Func<bool>(() => Zeigen(anker, true)));

            return Zeigen(anker, true);
        }

        /// <summary>Zeigt den Hinweis zum Nachlesen; ändert nichts.</summary>
        public static void Anzeigen(IWin32Window besitzer = null)
        {
            Zeigen((besitzer as Control) ?? Form.ActiveForm, false);
        }

        private static bool Zeigen(Control anker, bool mitEinwilligung)
        {
            bool ja = false;
            BlazorDialogForm<KiHinweisDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(mitEinwilligung))
            {
                ["Beantwortet"] = EventCallback.Factory.Create<bool>(new object(), antwort =>
                {
                    ja = antwort;
                    if (dlg != null) dlg.Schliessen(antwort);
                })
            };

            // KLEIN (Anwenderwunsch 05.09.2026): eine Rueckfrage mit Hinweistext.
            dlg = new BlazorDialogForm<KiHinweisDialog>(
                MyResource.Resource.KI_HINWEIS_FENSTER, MASS, werte,
                EPOS.UI.Dienste.Dialogart.Klein);

            using (dlg)
            {
                if (anker != null) dlg.ShowDialog(anker); else dlg.ShowDialog();
            }

            return ja;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Beantwortet</c>. So kann ihn auch
        /// das Chatfenster verwenden, das den Hinweis als ÜBERLAGERUNG zeigt statt als
        /// zweites Fenster.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(bool mitEinwilligung)
        {
            return new Dictionary<string, object>
            {
                ["MitEinwilligung"] = mitEinwilligung,
                ["Titel"] = MyResource.Resource.KI_HINWEIS_TITEL,
                ["Fassungszeile"] = string.Format(MyResource.Resource.KI_HINWEIS_FASSUNG,
                                                  KiEinwilligung.FASSUNG),
                ["Einleitung"] = MyResource.Resource.KI_HINWEIS_EINLEITUNG,
                ["Abschnitte"] = Abschnitte(),
                ["Stand"] = StandText(),
                ["EinverstandenText"] = MyResource.Resource.KI_HINWEIS_OK,
                ["AbbrechenText"] = MyResource.Resource.KI_HINWEIS_ABBRECHEN,
                ["SchliessenText"] = MyResource.Resource.KI_HINWEIS_SCHLIESSEN
            };
        }

        /// <summary>
        /// Die sieben Abschnitte in der Reihenfolge des Bestands
        /// (<c>Form_KiHinweis.TextAufbauen</c>, <c>:209-238</c>): erst was übertragen
        /// wird, dann was im Aktionsbetrieb dazukommt, dann was NICHT hinausgeht, dann
        /// Empfänger, Anwenderpflichten, Verantwortung und der Abschalter.
        /// </summary>
        private static IReadOnlyList<KiHinweisAbschnitt> Abschnitte()
        {
            return new List<KiHinweisAbschnitt>
            {
                new KiHinweisAbschnitt(MyResource.Resource.KI_HINWEIS_UEB_UEBERTRAGEN,
                                       MyResource.Resource.KI_HINWEIS_UEBERTRAGEN),
                new KiHinweisAbschnitt(MyResource.Resource.KI_HINWEIS_UEB_AKTIONEN,
                                       MyResource.Resource.KI_HINWEIS_AKTIONEN),
                new KiHinweisAbschnitt(MyResource.Resource.KI_HINWEIS_UEB_NICHT,
                                       MyResource.Resource.KI_HINWEIS_NICHT),
                new KiHinweisAbschnitt(MyResource.Resource.KI_HINWEIS_UEB_EMPFAENGER,
                                       MyResource.Resource.KI_HINWEIS_EMPFAENGER),
                new KiHinweisAbschnitt(MyResource.Resource.KI_HINWEIS_UEB_BEACHTEN,
                                       MyResource.Resource.KI_HINWEIS_BEACHTEN),
                new KiHinweisAbschnitt(MyResource.Resource.KI_HINWEIS_UEB_VERANTWORTUNG,
                                       MyResource.Resource.KI_HINWEIS_VERANTWORTUNG),
                new KiHinweisAbschnitt(MyResource.Resource.KI_HINWEIS_UEB_ABSCHALTEN,
                                       MyResource.Resource.KI_HINWEIS_ABSCHALTEN)
            };
        }

        /// <summary>
        /// Zeile über den Stand der Einwilligung - auch beim Nachlesen sichtbar.
        /// Drei Fälle: noch keine, ältere Fassung, aktuell.
        /// </summary>
        internal static string StandText()
        {
            int fassung = KiEinwilligung.BestaetigteFassung;
            string am = KiEinwilligung.BestaetigtAm;

            if (fassung <= 0) return MyResource.Resource.KI_HINWEIS_STAND_NEIN;

            if (fassung < KiEinwilligung.FASSUNG)
                return string.Format(MyResource.Resource.KI_HINWEIS_STAND_ALT,
                                     am, fassung, KiEinwilligung.FASSUNG);

            return string.Format(MyResource.Resource.KI_HINWEIS_STAND_JA, am, fassung);
        }
    }
}
