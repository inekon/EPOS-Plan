using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// <item><b>Wiederimport</b> (MA-6): Auch eine geänderte Fassung der Datei (andere Prüfsumme, gleiches Format) öffnet das
    /// Blatt. Das erste OK des Importdialogs zeigt die Unterschiede zum Bestand (<see cref="MehrzonenuebernahmeCtrl.Vorschau"/>)
    /// samt der Wahl, entfallende Elemente zu entfernen; das zweite merkt vor.</item>
    /// <item><b>Schreiben</b> im OK-Weg des Gebäudedialogs: <see cref="MehrzonenuebernahmeCtrl.Zusammenfuehren"/> in einem
    /// Vorgang — Ergänzungen von Hand bleiben, Rückweg bei Fehler.</item>
    /// </list>
    /// </summary>
    internal sealed class GebaeudeMehrzonenHuelle
    {
        private readonly Func<int> _gebaeudeId;
        private readonly int _idProjekt;
        private readonly bool _ios;
        private GebaeudeImportHerkunft _ausstehend;
        private string _ausstehendDatei = "";
        private IReadOnlyList<Zonenquellhinweis> _hinweise;
        private bool _entfernen;

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
            // MA-6: eine geänderte Fassung (andere Prüfsumme, gleiches Format) läuft in die Zusammenführung mit Vorschau.
            bool geaendert = p.Zustand == MehrzonenquellZustand.PruefsummeAbweichend && !string.IsNullOrEmpty(p.HashDatei);
            if (!p.Passend && !geaendert) return new GebaeudeMehrzonenstart(p.Meldung, null, pfad ?? "");

            var import = new GebaeudeImportHuelle(_idProjekt, _ios)
            {
                // GD-N: der zuletzt gespeicherte Zonenplan dieses Gebäudes als Vorbelegung des Zonenbaums.
                Planvorlage = (abbild, index, haken) => ZonenplanCtrl.GespeichertJeGebaeude(_gebaeudeId(), p.HashDatei, abbild, index, haken),
                ProjektdateiPruefen = true,
            };
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
                _entfernen = _vorschau?.Entfernen ?? false;
                _hinweise = ModellpruefungHuelle.HinweiseAus(import.Plan);
                return Task.FromResult<string>(null);
            };
            var gaben = new Dictionary<string, object>(import.Gaben(uebernehmen))
            {
                ["Wiederimport"] = new Func<GebaeudeImportErgebnis, GebaeudeWiederimportDaten>(ergebnis => Vorschau(import, ergebnis, geaendert))
            };
            return new GebaeudeMehrzonenstart(null, gaben, pfad);
        }

        private GebaeudeWiederimportDaten _vorschau;

        /// <summary>
        /// MA-6: die Unterschiede des gelesenen Modells zum Bestand des Gebäudes — ohne zu schreiben. <c>null</c> = das Gebäude
        /// hat keine Zonen (nichts zusammenzuführen).
        /// </summary>
        internal GebaeudeWiederimportDaten Vorschau(GebaeudeImportHuelle import, GebaeudeImportErgebnis ergebnis, bool geaendert)
        {
            if (import.SatzAusErgebnis(ergebnis) == null || import.Herkunft?.Vorschlag == null)
                return new GebaeudeWiederimportDaten { Meldung = MyResource.Resource.GEBMZ_MSG_OHNE_ZONEN };
            Wiederimportvorschau v = MehrzonenuebernahmeCtrl.Vorschau(_gebaeudeId(), import.Herkunft);
            if (!v.Ok) return new GebaeudeWiederimportDaten { Meldung = v.Meldung ?? "" };
            if (!v.Elemente.Any(e => e.Stand != Wiederimportstand.Neu)) return _vorschau = null;
            _entfernen = false;
            return _vorschau = new GebaeudeWiederimportDaten
            {
                GeaenderteFassung = geaendert,
                Zeilen = v.Elemente.Select(e => new WiederimportZeile(e.IstZone, e.Zone, e.Bezeichner, e.Stand, e.Felder, e.Hand)).ToList(),
                EntfernenSetzen = wert => _entfernen = wert
            };
        }

        /// <summary>Die vorgemerkte Wahl „entfallende Elemente entfernen“ (Prüfhilfe für die Proben).</summary>
        internal bool EntfallendeEntfernen => _entfernen;

        /// <summary>Die Zeile des vorgemerkten Modells; <c>null</c> = keins.</summary>
        internal string Ausstehend()
            => _ausstehend == null ? null
             : _vorschau == null ? Formatieren(MyResource.Resource.GEBMZ_AUSSTEHEND, _ausstehendDatei, _ausstehend.Vorschlag.Zonen.Count)
             : Formatieren(MyResource.Resource.GEBWI_AUSSTEHEND, _ausstehendDatei, _ausstehend.Vorschlag.Zonen.Count,
                           _entfernen ? MyResource.Resource.GEBWI_AUSSTEHEND_ENTFERNEN : MyResource.Resource.GEBWI_AUSSTEHEND_BEHALTEN);

        /// <summary>Das vorgemerkte Modell (Prüfhilfe für die Proben).</summary>
        internal GebaeudeImportHerkunft Vorgemerkt => _ausstehend;

        /// <summary>OK-Weg: ersetzt in einem Vorgang; <c>null</c> = geschrieben bzw. nichts vorgemerkt, sonst der Grund.</summary>
        internal string Schreiben()
        {
            if (_ausstehend == null) return null;
            Mehrzonenersetzung e = MehrzonenuebernahmeCtrl.Zusammenfuehren(_gebaeudeId(), _ausstehend, _entfernen);
            if (!e.Ok) return e.Meldung;
            // MA-1b: DIN-Nummer und Raumtyp je Zone für das Blatt „Modell prüfen", das der Gebäudedialog danach öffnet.
            ModellpruefungHuelle.HinweiseMerken(_gebaeudeId(), _hinweise);
            _ausstehend = null;
            _vorschau = null;
            _entfernen = false;
            return null;
        }

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
