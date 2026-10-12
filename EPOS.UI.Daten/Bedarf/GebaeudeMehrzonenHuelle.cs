using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>„Als Mehrzonenmodell übernehmen…“ im Gebäudedialog</b> (Entwurf Gebäudedialog 2.4, Welle GD-4; Entscheid E130 F7) —
    /// die Datenseite eines Projektgebäudes:
    /// <list type="number">
    /// <item><b>Öffnen</b>: die gespeicherte Importquelle (<see cref="MehrzonenuebernahmeCtrl.Quelle"/>), die Dateiwahl mit ihrem
    /// Namen über <c>Dienste.Datei</c> (erwartet, nie synchron) und die Prüfsumme der Datei gegen die Quelle
    /// (<see cref="MehrzonenuebernahmeCtrl.QuellePruefen"/>, im Arbeitsfaden). Weicht sie ab oder fehlt die Datei, steht eine
    /// benannte Meldung — kein stiller Rückfall; eine andere Datei ist ein neuer Import.</item>
    /// <item><b>Blatt</b>: der Parametersatz des Importdialogs aus einer frischen <see cref="GebaeudeImportHuelle"/> — derselbe
    /// Zonenbaum samt Raumplatzierung (GD-3) und Vorbesetzung (GD-5). Sein OK merkt die Herkunft vor, schreibt nichts;
    /// Abbrechen merkt nichts vor.</item>
    /// <item><b>Schreiben</b> im OK-Weg des Gebäudedialogs: <see cref="MehrzonenuebernahmeCtrl.Ersetzen"/> in einem Vorgang.</item>
    /// </list>
    /// </summary>
    internal sealed class GebaeudeMehrzonenHuelle
    {
        private readonly Func<int> _gebaeudeId;
        private readonly int _idProjekt;
        private readonly bool _ios;
        private GebaeudeImportHerkunft _ausstehend;
        private string _ausstehendDatei = "";

        internal GebaeudeMehrzonenHuelle(Func<int> gebaeudeId, int idProjekt, bool? ios = null)
        {
            _gebaeudeId = gebaeudeId ?? (() => 0);
            _idProjekt = idProjekt;
            _ios = ios ?? OperatingSystem.IsIOS();
        }

        /// <summary>Der Weg für den Gebäudedialog; ohne gespeicherte Quelle benannt gesperrt.</summary>
        internal static GebaeudeMehrzonenweg Weg(Func<int> gebaeudeId, int idProjekt, bool? ios = null)
        {
            ImportquelleModel q = MehrzonenuebernahmeCtrl.Quelle(gebaeudeId?.Invoke() ?? 0);
            if (q == null) return new GebaeudeMehrzonenweg { Sperre = MyResource.Resource.GEBMZ_SPERRE_KEINE_QUELLE };
            var h = new GebaeudeMehrzonenHuelle(gebaeudeId, idProjekt, ios);
            return h.AlsWeg(GebaeudeNeulesenHuelle.Angabe(q)?.Quellzeile ?? "");
        }

        /// <summary>Die Delegaten dieser Hülle als Weg — eigener Einstieg für die Proben.</summary>
        internal GebaeudeMehrzonenweg AlsWeg(string quellzeile)
            => new GebaeudeMehrzonenweg
            {
                Quelle = quellzeile ?? "",
                Oeffnen = OeffnenAsync,
                Ausstehend = Ausstehend,
                Verwerfen = () => _ausstehend = null,
                Schreiben = Schreiben
            };

        /// <summary>Dateiwahl mit dem Namen der Quelle, dann <see cref="StartAsync"/>; <c>null</c> = Wahl abgebrochen.</summary>
        internal async Task<GebaeudeMehrzonenstart> OeffnenAsync()
        {
            ImportquelleModel q = MehrzonenuebernahmeCtrl.Quelle(_gebaeudeId());
            if (q == null) return new GebaeudeMehrzonenstart(MyResource.Resource.GEBMZ_MSG_KEINE_QUELLE, null, "");
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                Formatieren(MyResource.Resource.GEB_NL_DATEI_TITEL, q.Dateiname),
                new GebaeudeNeulesenHuelle(_ios).Filter(q.Dateiname), null);
            if (string.IsNullOrEmpty(pfad)) return null;
            return await StartAsync(pfad, q);
        }

        /// <summary>
        /// Der Teil nach der Dateiwahl: Prüfsumme gegen die Quelle, bei Gleichheit der Parametersatz des Importdialogs mit dem
        /// vormerkenden <c>Uebernehmen</c>. Eigener Einstieg für die Proben.
        /// </summary>
        internal async Task<GebaeudeMehrzonenstart> StartAsync(string pfad, ImportquelleModel q)
        {
            Mehrzonenquellpruefung p = await Kulturweitergabe.Starten(() => MehrzonenuebernahmeCtrl.QuellePruefen(pfad, q));
            if (!p.Passend) return new GebaeudeMehrzonenstart(p.Meldung, null, pfad ?? "");

            var import = new GebaeudeImportHuelle(_idProjekt, _ios);
            string datei = q.Dateiname ?? "";
            Func<GebaeudeImportErgebnis, Task<string>> uebernehmen = ergebnis =>
            {
                if (import.SatzAusErgebnis(ergebnis) == null || import.Quelle == null)
                    return Task.FromResult(MyResource.Resource.GIMP_DLG_NICHT_GELESEN);
                // Gelesen ist, was geprüft wurde: dieselbe Prüfsumme wie beim Öffnen - sonst kein stiller Rückfall.
                if (!string.Equals(import.Quelle.Hash ?? "", p.HashDatei, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(Formatieren(MyResource.Resource.GEBMZ_MSG_PRUEFSUMME, datei,
                                                       import.Quelle.Hash ?? "", p.HashDatei));
                GebaeudeImportHerkunft h = import.Herkunft;
                if (h?.Vorschlag == null) return Task.FromResult(MyResource.Resource.GEBMZ_MSG_OHNE_ZONEN);
                _ausstehend = h;
                _ausstehendDatei = datei;
                return Task.FromResult<string>(null);
            };
            return new GebaeudeMehrzonenstart(null, import.Gaben(uebernehmen), pfad);
        }

        /// <summary>Die Zeile des vorgemerkten Modells; <c>null</c> = keins.</summary>
        internal string Ausstehend()
            => _ausstehend == null ? null
             : Formatieren(MyResource.Resource.GEBMZ_AUSSTEHEND, _ausstehendDatei, _ausstehend.Vorschlag.Zonen.Count);

        /// <summary>Das vorgemerkte Modell (Prüfhilfe für die Proben).</summary>
        internal GebaeudeImportHerkunft Vorgemerkt => _ausstehend;

        /// <summary>OK-Weg: ersetzt in einem Vorgang; <c>null</c> = geschrieben bzw. nichts vorgemerkt, sonst der Grund.</summary>
        internal string Schreiben()
        {
            if (_ausstehend == null) return null;
            Mehrzonenersetzung e = MehrzonenuebernahmeCtrl.Ersetzen(_gebaeudeId(), _ausstehend);
            if (!e.Ok) return e.Meldung;
            _ausstehend = null;
            return null;
        }

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
