using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Parameter des Zapfprofilgenerators — eine Zeile aus
    /// <c>Tab_TwwParameter_STAMM</c> ohne die interne Spalte <c>Beleg</c>.
    ///
    /// <para><b>Name mit Präfix</b>: <c>Parameterwert</c> ist im gemeinsamen Namensraum
    /// schon vergeben (<c>ParameterUebersichtCtrl.cs</c>); deshalb <c>ZapfParameterwert</c>.</para>
    /// </summary>
    internal sealed record ZapfParameterwert(string Schluessel, double Wert, string Einheit, Provenienz Herkunft);

    /// <summary>Warum ein Parameter nicht zur Verfügung steht.</summary>
    internal enum ParametersatzFehler
    {
        /// <summary>Die Tabelle fehlt oder trägt keine einzige Zeile — es gibt keine Katalogversion.</summary>
        KeineKatalogversion = 1,

        /// <summary>Die verlangte Katalogversion trägt keine Parameterzeile.</summary>
        KatalogversionFehlt = 2,

        /// <summary>Der verlangte Schlüssel steht nicht in der Katalogversion des Satzes.</summary>
        ParameterFehlt = 3
    }

    /// <summary>
    /// Die benannte Ablehnung „nicht rechenbar — Parameter fehlt" (Konzept 2.1): Es gibt
    /// keinen Rückfallwert, keine Vorgabe im Quelltext und keine stille Null. Der Grund
    /// steht als <see cref="Fehler"/> (für die Oberfläche) und als Klartext in der
    /// Meldung (für Protokoll und Test).
    /// </summary>
    internal sealed class ParametersatzException : Exception
    {
        internal ParametersatzException(ParametersatzFehler fehler, string katalogversion, string schluessel,
                                        string meldung)
            : base(meldung)
        {
            Fehler = fehler;
            Katalogversion = katalogversion ?? "";
            Schluessel = schluessel ?? "";
        }

        /// <summary>Der Grund der Ablehnung.</summary>
        internal ParametersatzFehler Fehler { get; }

        /// <summary>Die betroffene Katalogversion; leer, wenn es keine gibt.</summary>
        internal string Katalogversion { get; }

        /// <summary>Der fehlende Schlüssel; leer, wenn nicht ein Schlüssel, sondern die Version fehlt.</summary>
        internal string Schluessel { get; }
    }

    /// <summary>
    /// <b>Die gekapselten Normkonstanten, Regelwerksgrenzen und INEKON-Setzungen</b> einer
    /// Katalogversion (Umsetzungskonzept Zapfprofilgenerator 2.1, 3.3, Kapitel 6 (a)).
    ///
    /// <para><b>Die Schlüssel stehen im Code, die Werte nie.</b> Der Satz kommt aus
    /// <c>Tab_TwwParameter_STAMM</c> über <see cref="ZapfprofilCtrl.Parameter()"/>; diese
    /// Klasse kennt weder <see cref="DataRepository"/> noch eine Oberfläche. Fehlt ein
    /// Schlüssel, wirft <see cref="Wert"/> die benannte <see cref="ParametersatzException"/>
    /// statt einen Rückfallwert zu liefern.</para>
    ///
    /// <para><b>Unveränderlich.</b> Die Werte liegen nach dem Bau in einem eingefrorenen
    /// Wörterbuch; die Quelle, aus der der Satz gebaut wurde, kann ihn nicht mehr ändern.
    /// Schlüssel werden ordinal verglichen — <c>a.b</c> und <c>A.b</c> sind zwei Parameter.</para>
    /// </summary>
    internal sealed class Parametersatz
    {
        private readonly FrozenDictionary<string, ZapfParameterwert> _werte;

        private Parametersatz(string katalogversion, FrozenDictionary<string, ZapfParameterwert> werte)
        {
            Katalogversion = katalogversion;
            _werte = werte;
        }

        /// <summary>Die Katalogversion, aus der alle Werte dieses Satzes stammen.</summary>
        internal string Katalogversion { get; }

        /// <summary>Alle Parameter des Satzes, je Schlüssel — nur lesbar.</summary>
        internal IReadOnlyDictionary<string, ZapfParameterwert> Werte => _werte;

        /// <summary>Wie viele Parameter der Satz trägt.</summary>
        internal int Anzahl => _werte.Count;

        /// <summary>
        /// Baut einen Satz aus den Zeilen EINER Katalogversion. Leere Zeilenmenge, eine Zeile
        /// einer anderen Version oder ein doppelter Schlüssel werden benannt abgelehnt — ein
        /// Satz ohne Werte oder mit zwei Wahrheiten je Schlüssel entsteht nicht.
        /// </summary>
        internal static Parametersatz Aus(string katalogversion, IEnumerable<ZapfParameterwert> zeilen)
        {
            if (string.IsNullOrEmpty(katalogversion))
                throw new ParametersatzException(ParametersatzFehler.KeineKatalogversion, "", "",
                    "Nicht rechenbar — es gibt keine Katalogversion der Brauchwasserparameter.");

            var werte = new Dictionary<string, ZapfParameterwert>(StringComparer.Ordinal);
            if (zeilen != null)
                foreach (ZapfParameterwert p in zeilen)
                {
                    if (p == null || string.IsNullOrEmpty(p.Schluessel))
                        throw new ArgumentException("Eine Parameterzeile ohne Schlüssel.", nameof(zeilen));
                    // Die Provenienzspalte „Version" nennt, in welcher Katalogversion der
                    // WERT zuletzt gesetzt wurde; sie darf von der Katalogversion der Zeile
                    // abweichen und wird hier nicht gegen sie gehalten.
                    if (!werte.TryAdd(p.Schluessel, p))
                        throw new ArgumentException("Der Parameter „" + p.Schluessel + "“ steht in der Katalogversion „"
                                                    + katalogversion + "“ zweimal.", nameof(zeilen));
                }

            if (werte.Count == 0)
                throw new ParametersatzException(ParametersatzFehler.KatalogversionFehlt, katalogversion, "",
                    "Nicht rechenbar — die Katalogversion „" + katalogversion + "“ trägt keine Brauchwasserparameter.");

            return new Parametersatz(katalogversion, werte.ToFrozenDictionary(StringComparer.Ordinal));
        }

        /// <summary>Steht der Schlüssel im Satz?</summary>
        internal bool Enthaelt(string schluessel) => schluessel != null && _werte.ContainsKey(schluessel);

        /// <summary>
        /// Der Parameter zu einem Schlüssel samt Einheit und Provenienz; fehlt er, die benannte
        /// Ablehnung <see cref="ParametersatzFehler.ParameterFehlt"/>.
        /// </summary>
        internal ZapfParameterwert Lies(string schluessel)
        {
            if (schluessel != null && _werte.TryGetValue(schluessel, out ZapfParameterwert p)) return p;
            throw new ParametersatzException(ParametersatzFehler.ParameterFehlt, Katalogversion, schluessel,
                "Nicht rechenbar — Parameter fehlt: „" + (schluessel ?? "") + "“ steht nicht in der Katalogversion „"
                + Katalogversion + "“ der Brauchwasserparameter.");
        }

        /// <summary>Der Zahlenwert zu einem Schlüssel; fehlt er, die benannte Ablehnung.</summary>
        internal double Wert(string schluessel) => Lies(schluessel).Wert;
    }
}
