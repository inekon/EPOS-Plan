using System;
using System.Collections.Generic;

namespace KiKern
{
    /// <summary>
    /// Das Ergebnis EINES Ausfuehrungsversuchs - Grundlage fuer Chatantwort, Protokollzeile
    /// und (ab Etappe 2) die Rueckmeldung an das Modell (Fachkonzept 3.6).
    /// </summary>
    /// <remarks>
    /// <see cref="Zeilen"/> traegt tabellarische Nutzdaten in genau der Form, in der die
    /// Oberflaeche sie anzeigen kann: eine Liste von Feld/Wert-Zuordnungen. Der Kern
    /// formatiert sie NICHT - die Anzeige entscheidet ueber Kultur und Spaltenwahl.
    /// </remarks>
    public sealed class KiErgebnis
    {
        private readonly List<string> _meldungen = new List<string>();

        private KiErgebnis(KiStatus status, string text)
        {
            Status = status;
            Text = text ?? "";
            Zeilen = Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        /// <summary>Ausgang des Versuchs.</summary>
        public KiStatus Status { get; }

        /// <summary>true, wenn die Aktion gelaufen ist und ein Ergebnis geliefert hat.</summary>
        public bool Erfolg => Status == KiStatus.Ausgefuehrt;

        /// <summary>Kurzer Klartext fuer den Chat; bei Fehlern der Grund.</summary>
        public string Text { get; }

        /// <summary>Tabellarische Nutzdaten (kann leer sein).</summary>
        public IReadOnlyList<IReadOnlyDictionary<string, object?>> Zeilen { get; private set; }

        /// <summary>
        /// Zahl der betroffenen Datensaetze - im Protokoll die Kurzfassung des Ergebnisses.
        /// Vorbelegt mit der Zeilenzahl, kann von der Aktion ueberschrieben werden.
        /// </summary>
        public int Anzahl { get; private set; }

        /// <summary>Laufzeit; wird vom Ausfuehrer gesetzt.</summary>
        public TimeSpan Dauer { get; set; }

        /// <summary>
        /// Zusatzmeldungen: die still gesammelten Datenbankfehler aus
        /// <c>DataRepository.StilleFehlerAbholen()</c> und Hinweise der Aktion selbst.
        /// </summary>
        public IReadOnlyList<string> Meldungen => _meldungen;

        // ---------------------------------------------------------------- Erzeuger

        /// <summary>Erfolgreiche Ausfuehrung mit Klartext und optionalen Nutzdaten.</summary>
        public static KiErgebnis Ok(string text,
                                    IReadOnlyList<IReadOnlyDictionary<string, object?>>? zeilen = null,
                                    int? anzahl = null)
        {
            var e = new KiErgebnis(KiStatus.Ausgefuehrt, text);
            if (zeilen != null) e.Zeilen = zeilen;
            e.Anzahl = anzahl ?? e.Zeilen.Count;
            return e;
        }

        /// <summary>
        /// Die Aktion wurde gar nicht erst ausgefuehrt - Vorbedingung nicht erfuellt,
        /// Parameter fehlerhaft, Sperre belegt oder Anwender hat abgelehnt.
        /// </summary>
        public static KiErgebnis Abgelehnt(string grund)
            => new KiErgebnis(KiStatus.Abgelehnt, grund);

        /// <summary>Der Lauf wurde abgebrochen.</summary>
        public static KiErgebnis Abgebrochen(string text)
            => new KiErgebnis(KiStatus.Abgebrochen, text);

        /// <summary>Die Aktion lief an und endete in einem Fehler.</summary>
        public static KiErgebnis Fehlgeschlagen(string grund)
            => new KiErgebnis(KiStatus.Fehlgeschlagen, grund);

        // ---------------------------------------------------------------- Anreichern

        /// <summary>Haengt Meldungen an (still gesammelte Datenbankfehler, Hinweise).</summary>
        public KiErgebnis MitMeldungen(IEnumerable<string>? meldungen)
        {
            if (meldungen == null) return this;
            foreach (string m in meldungen)
                if (!string.IsNullOrWhiteSpace(m)) _meldungen.Add(m.Trim());
            return this;
        }

        /// <summary>Setzt die Laufzeit und liefert dasselbe Ergebnis zurueck.</summary>
        public KiErgebnis MitDauer(TimeSpan dauer)
        {
            Dauer = dauer;
            return this;
        }

        /// <summary>Kurzfassung des Ergebnisses fuer das Protokollfeld „Ergebnis".</summary>
        public string Kurzfassung()
        {
            string text = Text.Length > 0 ? Text : SchutzstufeText.Schluessel(Status);
            if (Zeilen.Count > 0 || Anzahl > 0) text = Anzahl + "x; " + text;
            return text;
        }
    }
}
