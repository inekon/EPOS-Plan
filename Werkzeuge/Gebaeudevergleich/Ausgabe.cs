using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Konsole und Protokoll</b> — die zwei Ausgabekanäle eines Laufs.
    ///
    /// <para><b>Das Protokoll</b> (<c>protokoll.txt</c>) nimmt alles auf: die eigenen Zeilen
    /// des Werkzeugs und alles, was der Kern auf <see cref="Console.Out"/> oder
    /// <see cref="Console.Error"/> legt — <c>SimulationProtokoll</c> schreibt jede Meldung
    /// dorthin, samt Gebäudenamen. Deshalb werden beide Ströme beim Start hierher umgelenkt,
    /// und jede Zeile läuft durch die <see cref="Namensbereinigung"/>. Zeilen, die vor dem
    /// Lesen der Namen entstehen, werden zurückgehalten und erst bereinigt geschrieben.</para>
    ///
    /// <para><b>Die Konsole</b> bekommt nur die namenlosen Zeilen des Werkzeugs: Unterbefehl und
    /// Zähler, je Gebäude <c>P&lt;ID&gt;/G&lt;ID&gt;: alt ok|FEHLER, neu ok|FEHLER &lt;Code&gt;</c>,
    /// Abbruchgründe und den Exitcode. Auch sie laufen durch die Bereinigung.</para>
    ///
    /// <para>Ein Aufruf aus einem zweiten Faden (die 60-s-Warnung) ist erlaubt; alle
    /// Schreibwege sind gesperrt.</para>
    /// </summary>
    internal sealed class Ausgabe : IDisposable
    {
        private readonly object _sperre = new object();
        private readonly TextWriter _konsole;
        private readonly TextWriter _fehlerkonsole;
        private readonly StreamWriter _datei;
        /// <summary>Zeilen vor dem Scharfschalten: bereinigt wird <c>Zeile</c>, <c>Roh</c> (eine Prüfsumme) nicht.</summary>
        private readonly List<(string Zeile, string Roh)> _zurueckgehalten = new List<(string, string)>();
        private readonly Zeilenschreiber _umlenkung;
        private Namensbereinigung _bereinigung;
        private bool _scharf;
        private bool _entsorgt;

        internal Ausgabe(string protokolldatei)
        {
            _konsole = Console.Out;
            _fehlerkonsole = Console.Error;
            Directory.CreateDirectory(Path.GetDirectoryName(protokolldatei));
            _datei = new StreamWriter(protokolldatei, false, new UTF8Encoding(false)) { AutoFlush = false, NewLine = "\n" };

            _umlenkung = new Zeilenschreiber(ProtokollZeile);
            Console.SetOut(_umlenkung);
            Console.SetError(_umlenkung);
        }

        /// <summary>Die aktuelle Bereinigung (vor dem Scharfschalten leer).</summary>
        internal Namensbereinigung Bereinigung => _bereinigung ?? Namensbereinigung.Leer;

        /// <summary>
        /// Setzt die Bereinigung und schreibt die zurückgehaltenen Zeilen — bereinigt. Mit
        /// <see cref="Namensbereinigung.Leer"/> (<c>--mit-namen</c>) wird nichts ersetzt.
        /// </summary>
        internal void Scharfschalten(Namensbereinigung bereinigung)
        {
            lock (_sperre)
            {
                _bereinigung = bereinigung ?? Namensbereinigung.Leer;
                _scharf = true;
                foreach ((string z, string roh) in _zurueckgehalten) _datei.WriteLine(_bereinigung.Bereinigen(z) + roh);
                _zurueckgehalten.Clear();
                _datei.Flush();
            }
        }

        /// <summary>Eine Zeile nur ins Protokoll.</summary>
        internal void Protokoll(string zeile) => ProtokollZeile(zeile ?? "");

        /// <summary>
        /// Eine Prüfsummenzeile ins Protokoll — OHNE Bereinigung. Nur für Zeilen, die das Werkzeug
        /// allein aus festem Text und einer Prüfsumme (Kleinbuchstaben-Hex) bildet: Ein kurzer
        /// Namenswert wie „de" träfe sonst mitten in eine Prüfsumme und machte sie unlesbar.
        /// </summary>
        internal void ProtokollPruefsumme(string text, string pruefsumme)
        {
            lock (_sperre)
            {
                if (_entsorgt) return;
                if (!_scharf) { _zurueckgehalten.Add((text ?? "", pruefsumme ?? "")); return; }
                _datei.WriteLine(_bereinigung.Bereinigen(text ?? "") + (pruefsumme ?? ""));
            }
        }

        /// <summary>Eine Zeile ins Protokoll, sofort auf die Platte (Beginn eines Gebäudes).</summary>
        internal void ProtokollSofort(string zeile)
        {
            lock (_sperre)
            {
                ProtokollZeile(zeile ?? "");
                if (_scharf) _datei.Flush();
            }
        }

        /// <summary>Eine namenlose Zeile auf die Konsole und ins Protokoll.</summary>
        internal void Konsole(string zeile)
        {
            lock (_sperre)
            {
                try { _konsole.WriteLine(Bereinigung.Bereinigen(zeile ?? "")); _konsole.Flush(); } catch { }
                ProtokollZeile(zeile ?? "");
                if (_scharf) _datei.Flush();
            }
        }

        /// <summary>Ein Abbruchgrund auf die Fehlerkonsole und ins Protokoll.</summary>
        internal void Fehler(string zeile)
        {
            lock (_sperre)
            {
                try { _fehlerkonsole.WriteLine(Bereinigung.Bereinigen(zeile ?? "")); _fehlerkonsole.Flush(); } catch { }
                ProtokollZeile(zeile ?? "");
                if (_scharf) _datei.Flush();
            }
        }

        private void ProtokollZeile(string zeile)
        {
            lock (_sperre)
            {
                if (_entsorgt) return;
                if (!_scharf) { _zurueckgehalten.Add((zeile, "")); return; }
                _datei.WriteLine(_bereinigung.Bereinigen(zeile));
            }
        }

        public void Dispose()
        {
            // Die angefangene letzte Zeile VOR der eigenen Sperre: Der Zeilenschreiber hält
            // seine Sperre, während er in diese Klasse ruft - umgekehrt wäre ein Verklemmen möglich.
            _umlenkung.Rest();
            lock (_sperre)
            {
                if (_entsorgt) return;
                if (!_scharf) Scharfschalten(_bereinigung);
                Console.SetOut(_konsole);
                Console.SetError(_fehlerkonsole);
                _entsorgt = true;
                _datei.Flush();
                _datei.Dispose();
            }
        }

        /// <summary>
        /// Ein <see cref="TextWriter"/>, der Zeichen zu Zeilen sammelt und jede fertige Zeile
        /// weiterreicht — für die Umlenkung von <see cref="Console.Out"/>.
        /// </summary>
        private sealed class Zeilenschreiber : TextWriter
        {
            private readonly Action<string> _zeile;
            private readonly StringBuilder _puffer = new StringBuilder();
            private readonly object _sperre = new object();

            internal Zeilenschreiber(Action<string> zeile) { _zeile = zeile; }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                lock (_sperre)
                {
                    if (value == '\n')
                    {
                        string z = _puffer.ToString().TrimEnd('\r');
                        _puffer.Clear();
                        _zeile(z);
                    }
                    else _puffer.Append(value);
                }
            }

            public override void Write(string value)
            {
                if (value == null) return;
                lock (_sperre) { foreach (char c in value) Write(c); }
            }

            public override void WriteLine(string value)
            {
                lock (_sperre) { Write(value); Write('\n'); }
            }

            /// <summary>Reicht eine angefangene letzte Zeile weiter.</summary>
            internal void Rest()
            {
                lock (_sperre)
                {
                    if (_puffer.Length > 0) { _zeile(_puffer.ToString()); _puffer.Clear(); }
                }
            }
        }
    }
}
