using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der benannte Grund einer Ausnahme beim Dateiimport (<see cref="Importfang"/>).
    /// </summary>
    /// <param name="Datei">Der Dateiname ohne Ordner.</param>
    /// <param name="Ursache">Die Meldung der Ausnahme.</param>
    /// <param name="Text">Der fertige Satz für den Warnbanner
    /// (<c>IMP_MSG_UNERWARTET</c>: „Die Datei „x.csv“ konnte nicht eingelesen werden: …“).</param>
    internal sealed record Importablehnung(string Datei, string Ursache, string Text);

    /// <summary>
    /// <b>Die gemeinsame Fangstelle der Dateiimporte in den Hüllen</b> (IM-1). Die Leser des
    /// Kerns liefern bei falschem Format ein Ergebnis mit Befund; was trotzdem als Ausnahme
    /// entsteht (Datenbank, Datei gesperrt, ein Fehler im Leser), fängt diese EINE Stelle statt
    /// eines try/catch in jedem Dialog: Sie startet die Arbeit über
    /// <see cref="Kulturweitergabe"/>, vermerkt die Ausnahme im <see cref="Ausnahmeprotokoll"/> und
    /// gibt das Ergebnis der benannten Ablehnung zurück — der Dialog zeigt es in seinem Warnbanner
    /// und bleibt bedienbar.
    ///
    /// <para><b>Warum hier:</b> Eine Ausnahme, die aus der Hülle in den Ereignishandler einer
    /// Razor-Komponente läuft, endet in der Fehlerschranke der Wurzel (<c>Wurzel&lt;T&gt;</c>), die
    /// den ganzen Dialog gegen die Fehlansicht tauscht — für den Anwender ein Absturz.</para>
    ///
    /// <para>Ein Abbruch (<see cref="OperationCanceledException"/>) ist keine Ablehnung und läuft
    /// weiter wie bisher.</para>
    /// </summary>
    internal static class Importfang
    {
        /// <summary>Startet <paramref name="arbeit"/> nebenher; eine Ausnahme wird zu <paramref name="ablehnung"/>.</summary>
        internal static async Task<T> Starten<T>(string pfad, Func<T> arbeit, Func<Importablehnung, T> ablehnung)
        {
            try { return await Kulturweitergabe.Starten(arbeit); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return ablehnung(Vermerken(pfad, ex)); }
        }

        /// <summary>Wie <see cref="Starten{T}"/> für eine asynchrone Kette (Importablauf mit Rückrufen).</summary>
        internal static async Task<T> StartenAsync<T>(string pfad, Func<Task<T>> arbeit, Func<Importablehnung, T> ablehnung)
        {
            try { return await Kulturweitergabe.StartenAsync(arbeit); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return ablehnung(Vermerken(pfad, ex)); }
        }

        /// <summary>Vermerkt die Ausnahme im Ausnahmeprotokoll und baut den Grund.</summary>
        internal static Importablehnung Vermerken(string pfad, Exception ex)
        {
            string datei = Path.GetFileName(pfad ?? "") ?? "";
            Ausnahmeprotokoll.Vermerken("Import \u201e" + datei + "\u201c abgelehnt: "
                + ex.GetType().FullName + ": " + ex.Message);
            string text = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_MSG_UNERWARTET,
                                        datei, ex.Message);
            return new Importablehnung(datei, ex.Message, text);
        }

        /// <summary>Die Ablehnung als Ergebnis der Ganglinien-Importkette (Klasse A).</summary>
        internal static GanglinienImportErgebnis AlsGanglinienimport(Importablehnung a)
        {
            var erg = new GanglinienImportErgebnis
            {
                Ausgang = ImportAusgang.Fehler,
                Meldung = a.Text,
                MeldungStufe = PruefStufe.Fehler
            };
            erg.Protokoll.Add(new PruefMeldung(PruefStufe.Fehler, GanglinienDatei.SchluesselLesefehler, a.Ursache));
            return erg;
        }

        /// <summary>Die Ablehnung als Vorschau des Optionendialogs: nicht lesbar, mit Grund.</summary>
        internal static GanglinienVorschau AlsVorschau(Importablehnung a)
        {
            var v = new GanglinienVorschau();
            v.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, GanglinienDatei.SchluesselLesefehler, a.Ursache));
            return v;
        }
    }
}
