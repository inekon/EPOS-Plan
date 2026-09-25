using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Admin;
using Microsoft.AspNetCore.Components;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Abschnitt „Bericht" der Programmeinstellungen</b> (Etappe BV-E1, Konzept
    /// Berichtsvorlagen 10.3) — plattformfrei: Firma für <c>{{ersteller.firma}}</c> und der
    /// Vorlagenordner der eigenen Berichtsvorlagen, gelesen und geschrieben über
    /// <see cref="BerichtsvorlagenCtrl"/>. Die Schale mischt diese Gaben in den Parametersatz
    /// des <see cref="EinstellungenDialog"/> (Windows: <c>EinstellungenHuelle</c>).
    ///
    /// <para><b>Hinaus geht nur, was der Dialog im OK-Weg meldet</b> — nach dem gelungenen
    /// Speichern des Wertesatzes, je Wert einmal und nur geändert. Die Firma schreibt
    /// <see cref="BerichtsvorlagenCtrl.SchreibeFirma"/>; ist sie die Firma der Lizenz, fällt die
    /// Einstellung weg, damit die Vorbelegung der Lizenz folgt. Der Ordner geht über
    /// <see cref="BerichtsvorlagenCtrl.SetzeVorlagenordner"/>, der nur einen bestehenden,
    /// erreichbaren Ordner übernimmt; sein Befund „nicht erreichbar" kommt als Meldung.</para>
    ///
    /// <para><b>Ohne Ordnerwahl der Plattform</b> (iOS: <c>OrdnerWaehlen</c> liefert dort immer
    /// <c>""</c>, <see cref="Berichtsvorlagenwege.OrdnerWaehlbar"/> fehlt) ist das Feld nur lesbar,
    /// und der Grund steht darunter — benannt, nicht still.</para>
    /// </summary>
    internal static class EinstellungenBerichtGaben
    {
        /// <summary>Hilfeschlüssel des Abschnitts (Muster <c>&lt;Maske&gt;.btn_Help_&lt;Teil&gt;</c>).</summary>
        internal const string HILFE_BERICHT = "Form_AdminSettings.btn_Help_Bericht";

        /// <summary>
        /// Die Gaben des Abschnitts „Bericht": Firma samt Vorgabe und Rückweg, Vorlagenordner samt
        /// Vorgabe, Rückweg und — ohne Ordnerwahl der Plattform — dem Sperrgrund, die Texte und der
        /// Hilfeschlüssel. <paramref name="ctrl"/> und <paramref name="wege"/> reicht ein Prüfstand
        /// herein; <c>null</c> = die der Plattform.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(BerichtsvorlagenCtrl ctrl = null,
                                                                 Berichtsvorlagenwege wege = null)
        {
            BerichtsvorlagenCtrl vorlagen = ctrl ?? new BerichtsvorlagenCtrl();
            Berichtsvorlagenwege plattform = wege ?? Berichtsvorlagenwege.Plattform ?? new Berichtsvorlagenwege();

            var gaben = new Dictionary<string, object>
            {
                ["Firma"] = Lies(() => vorlagen.Ersteller().Firma),
                ["FirmaVorgabe"] = Lies(vorlagen.FirmaAusLizenz),
                ["FirmaChanged"] = EventCallback.Factory.Create<string>(new object(), text => FirmaSchreiben(vorlagen, text)),
                ["Vorlagenordner"] = Lies(() => vorlagen.Vorlagenordner),
                ["VorlagenordnerVorgabe"] = Lies(() => vorlagen.Vorgabeordner),
                ["VorlagenordnerChanged"] = EventCallback.Factory.Create<string>(new object(),
                    ordner => OrdnerSetzen(vorlagen, ordner)),
                ["BerichtTexte"] = new EinstellungenBerichtTexte(),
                ["HilfeSchluesselBericht"] = HILFE_BERICHT
            };
            if (!plattform.OrdnerWaehlbar) gaben["VorlagenordnerGesperrtGrund"] = R.EIN_BERICHT_ORDNER_FEST;
            return gaben;
        }

        /// <summary>
        /// Schreibt die Firma. Dieselbe wie die der Lizenz heißt „keine eigene Angabe": Die
        /// Einstellung fällt weg, und die Vorbelegung folgt einer neuen Lizenz. Leer bleibt
        /// ausdrücklich leer.
        /// </summary>
        internal static void FirmaSchreiben(BerichtsvorlagenCtrl vorlagen, string text)
        {
            string firma = (text ?? "").Trim();
            string lizenz = Lies(vorlagen.FirmaAusLizenz);
            if (lizenz.Length > 0 && string.Equals(firma, lizenz, StringComparison.Ordinal)) vorlagen.SchreibeFirma(null);
            else vorlagen.SchreibeFirma(firma);
        }

        /// <summary>
        /// Setzt den Vorlagenordner; nimmt der Controller ihn nicht (nicht erreichbar, ungültig), sagt
        /// es eine Meldung — der Dialog ist dann schon gespeichert und geschlossen.
        /// </summary>
        internal static async Task OrdnerSetzen(BerichtsvorlagenCtrl vorlagen, string ordner)
        {
            Ordnerbefund befund;
            try { befund = vorlagen.SetzeVorlagenordner(ordner); }
            catch (Exception ex)
            {
                await Dienste.Dialog.WarnungAsync(ex.Message, R.ADM_SET_TITEL);
                return;
            }
            if (!befund.Erfolg) await Dienste.Dialog.WarnungAsync(befund.Meldung, R.ADM_SET_TITEL);
        }

        private static string Lies(Func<string> quelle)
        {
            try { return quelle() ?? ""; }
            catch (Exception) { return ""; }
        }
    }
}
