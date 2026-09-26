using System;
using System.Collections.Generic;
using System.IO;
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
    ///
    /// <para><b>BV-E2 (Anwenderentscheid BV-E2-1, Lesart b): das Firmenlogo.</b> Der Pfad einer PNG-
    /// oder JPEG-Datei (leer = ohne Logo), den der Berichtslauf für die Kopfzeile liest — gelesen und
    /// geschrieben allein über den Controller (<see cref="BerichtsvorlagenCtrl.LogoPfad"/>,
    /// <see cref="BerichtsvorlagenCtrl.SchreibeLogo"/>; leer entfernt die Einstellung). Gewählt wird über
    /// <c>Dienste.Datei</c> (auf iOS kopiert die Dateiwahl die Datei in die Sandbox); ob die Datei da
    /// ist, sagt der Dialog als Hinweis unter dem Feld, geschrieben wird der Pfad trotzdem.</para>
    /// </summary>
    internal static class EinstellungenBerichtGaben
    {
        /// <summary>Hilfeschlüssel des Abschnitts (Muster <c>&lt;Maske&gt;.btn_Help_&lt;Teil&gt;</c>).</summary>
        internal const string HILFE_BERICHT = "Form_AdminSettings.btn_Help_Bericht";

        /// <summary>
        /// Die Gaben des Abschnitts „Bericht": Firma samt Vorgabe und Rückweg, Vorlagenordner samt
        /// Vorgabe, Rückweg und — ohne Ordnerwahl der Plattform — dem Sperrgrund, die Texte und der
        /// Hilfeschlüssel — dazu (BV-E2) das Logo samt Rückweg, Dateiwahl und Prüfung.
        /// <paramref name="ctrl"/> und <paramref name="wege"/> reicht ein Prüfstand herein; <c>null</c> =
        /// die der Plattform.
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
                ["HilfeSchluesselBericht"] = HILFE_BERICHT,

                // BV-E2 (Entscheid BV-E2-1): das Firmenlogo der Kopfzeile.
                ["Logo"] = Lies(() => vorlagen.LogoPfad),
                ["LogoChanged"] = EventCallback.Factory.Create<string>(new object(), pfad => vorlagen.SchreibeLogo(pfad)),
                ["LogoWaehler"] = new Func<string, Task<string>>(LogoWaehlen),
                ["LogoVorhanden"] = new Func<string, bool>(LogoVorhanden)
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
            if (!befund.Erfolg)
            {
                await Dienste.Dialog.WarnungAsync(befund.Meldung, R.ADM_SET_TITEL);
                return;
            }

            // BV-E7-6: Die Muster (Unterordner „Mitgeliefert“) folgen dem Ordner - im Hintergrund, der
            // Dialog ist schon zu; ein Schreibfehler im neuen Ordner kommt als Meldung (eine fehlende
            // Quelle der Installation nennt das Startprotokoll).
            Musterbefund muster;
            try { muster = await SpeicherEngine.Kulturweitergabe.Starten(() => vorlagen.MusterBereitstellen()); }
            catch (Exception ex)
            {
                await Dienste.Dialog.WarnungAsync(ex.Message, R.ADM_SET_TITEL);
                return;
            }
            if (muster.Schreibfehler) await Dienste.Dialog.WarnungAsync(muster.Meldung, R.ADM_SET_TITEL);
        }

        /// <summary>
        /// BV-E2: die Dateiwahl des Logos über <c>Dienste.Datei</c> (Bilder PNG und JPEG); <c>""</c> =
        /// abgebrochen. Den Filter der Komponente nimmt sie, wenn einer kommt.
        /// </summary>
        internal static async Task<string> LogoWaehlen(string filter)
        {
            string start = "";
            try { start = Dienste.Pfade.Dokumente ?? ""; } catch (Exception) { start = ""; }
            string muster = string.IsNullOrWhiteSpace(filter) ? R.EIN_BERICHT_LOGO_FILTER : filter;
            try { return await Dienste.Datei.DateiOeffnenAsync(R.EIN_BERICHT_DLG_LOGO, muster, start) ?? ""; }
            catch (Exception) { return ""; }
        }

        /// <summary>
        /// BV-E2: Gibt es die Datei im Feld? Ein ungültiger Pfad ist keine Datei. Geprüft wird der Wert des
        /// Felds vor dem Speichern — nicht die Einstellung wie <see cref="BerichtsvorlagenCtrl.LogoVorhanden"/>,
        /// dessen strengere Probe (PNG oder JPEG, Größe) der Hinweis „Datei nicht gefunden" nicht trüge;
        /// was der Lauf nicht laden kann, nennt seine Laufmeldung.
        /// </summary>
        internal static bool LogoVorhanden(string pfad)
        {
            try { return !string.IsNullOrWhiteSpace(pfad) && File.Exists(pfad.Trim()); }
            catch (Exception) { return false; }
        }

        private static string Lies(Func<string> quelle)
        {
            try { return quelle() ?? ""; }
            catch (Exception) { return ""; }
        }
    }
}
