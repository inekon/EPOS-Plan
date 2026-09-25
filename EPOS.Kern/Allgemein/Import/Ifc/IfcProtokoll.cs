using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xbim.Common.Step21;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Protokollsenke eines IFC-Lesedurchgangs</b> (Datenaustauschkonzept 4, Ergänzung 3;
    /// 7.1 <c>FehlendeEntitaeten</c>): ein eigener <see cref="ILoggerFactory"/> je Lauf, den der Leser
    /// dem <c>MemoryModel</c> in den Konstruktor gibt — der Parser (<c>XbimP21Scanner</c>) und die
    /// Entitätserzeugung schreiben über die Fabrik des Modells.
    ///
    /// <para><b>Kein globaler Zustand.</b> <c>XbimServices.Current</c> wird NICHT konfiguriert: Eine dort
    /// belegte Senke bliebe für den ganzen Prozess hängen, und zwei Läufe (oder zwei Testklassen)
    /// zählten in dieselbe. Gemessen an xBIM 6.1.605: <c>MemoryModel.OpenReadStep21(Stream, ILogger, …)</c>
    /// reicht den übergebenen Logger NICHT an den Parser weiter, sondern nimmt die Fabrik aus
    /// <c>XbimServices</c> — deshalb öffnet der Leser das Modell über den Konstruktor mit dieser Fabrik
    /// und <c>LoadStep21</c>/<c>LoadXml</c>.</para>
    ///
    /// <para><b>Beide Verlustkanäle werden gezählt</b> — wer nur den ersten zählt, gäbe beim Round-Trip
    /// eine Datei trotz grüner Sperre beschädigt zurück (Befund S, 3.2):</para>
    /// <list type="number">
    /// <item><b>Nicht angelegt:</b> Einträge mit <see cref="LogEventIds.FailedEntity"/> und jede
    /// Meldung der Stufe Fehler (der Parser meldet einen unbekannten Typ als Fehler ohne Ereigniskennung,
    /// „Illegal element in file; cannot find type …").</item>
    /// <item><b>Verweis ins Leere:</b> Warnungen „Entity #… is referenced but could not be
    /// instantiated" — erkannt an der Formatvorlage der Meldung, nicht am formatierten Text.</item>
    /// </list>
    /// </summary>
    internal sealed class IfcProtokoll : ILoggerFactory, ILogger
    {
        /// <summary>Die Formatvorlage des zweiten Verlustkanals, wie xBIM sie schreibt (Kernstück, ohne Platzhalter).</summary>
        internal const string VORLAGE_VERWEIS = "is referenced but could not be instantiated";

        private const int BEISPIELE = 5;

        private readonly List<string> _beispiele = new List<string>();

        /// <summary>Verlustkanal 1: nicht angelegte Entitäten.</summary>
        public int NichtAngelegt { get; private set; }

        /// <summary>Verlustkanal 2: Verweise auf Entitäten, die es nicht gibt.</summary>
        public int VerweiseInsLeere { get; private set; }

        /// <summary>Die Summe beider Kanäle — <c>Tab_Importquelle.FehlendeEntitaeten</c>.</summary>
        public int Summe => NichtAngelegt + VerweiseInsLeere;

        /// <summary>Die ersten Verlustmeldungen im Wortlaut der Bibliothek (für den Beleg).</summary>
        public IReadOnlyList<string> Beispiele => _beispiele;

        /// <summary>Jede Kategorie schreibt in dieselbe Senke.</summary>
        public ILogger CreateLogger(string categoryName) => this;

        /// <summary>Fremde Senken werden nicht angenommen — das Protokoll gehört dem Lauf.</summary>
        public void AddProvider(ILoggerProvider provider)
        {
        }

        /// <summary>Nichts freizugeben.</summary>
        public void Dispose()
        {
        }

        /// <summary>Warnungen und Fehler zählen; Informationen und Ablaufmeldungen nicht.</summary>
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning && logLevel != LogLevel.None;

        /// <summary>Kein Bereich.</summary>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Ordnet eine Meldung einem der beiden Verlustkanäle zu.</summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                                Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            string text = null;
            try { text = formatter?.Invoke(state, exception); }
            catch (Exception) { text = null; }

            string vorlage = Vorlage(state) ?? text ?? "";
            if (vorlage.IndexOf(VORLAGE_VERWEIS, StringComparison.Ordinal) >= 0)
                VerweiseInsLeere++;
            else if (eventId.Id == LogEventIds.FailedEntity.Id && eventId.Name == LogEventIds.FailedEntity.Name
                     || logLevel >= LogLevel.Error)
                NichtAngelegt++;
            else
                return;   // eine Warnung ohne Verlust (Kodierung, erweiterte Parameter …)

            if (_beispiele.Count < BEISPIELE && !string.IsNullOrWhiteSpace(text))
                _beispiele.Add(text.Trim());
        }

        /// <summary>Die Formatvorlage einer strukturierten Meldung (<c>{OriginalFormat}</c>); <c>null</c> = keine.</summary>
        private static string Vorlage<TState>(TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object>> werte)
                foreach (KeyValuePair<string, object> w in werte)
                    if (w.Key == "{OriginalFormat}") return w.Value as string;
            return null;
        }
    }
}
