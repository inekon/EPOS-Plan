using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Parametersatz der Pufferspeicher-Auslegung (Konzept 4.1, 5): die Werte der Vorgabetabelle
    /// <c>Tab_PufferAuslegungParameter_STAMM</c>, für jeden fehlenden Schlüssel der eingebaute Wert aus
    /// <see cref="PufferAuslegungVorgaben"/> — derselben Liste, aus der die Saat stammt. Der Rechenkern
    /// bekommt den Satz fertig im Eingang und liest selbst keine Datenbank. Ein Projekt liest seine
    /// Projektkopie (<see cref="ProjektPufferparameter"/>, <see cref="Lesen(int)"/>).
    /// </summary>
    public sealed class PufferAuslegungParameter
    {
        /// <summary>Die Leseanweisung — mit Parameter, nie zusammengesetzt.</summary>
        public const string SQL_LESEN =
            "SELECT Schluessel, Wert, Quelle FROM " + PufferAuslegungSchema.TAB_PARAMETER + " WHERE Schluessel LIKE ?";

        private readonly Dictionary<string, double> _werte;
        private readonly Dictionary<string, string> _quellen;
        private readonly HashSet<string> _ausTabelle;

        private PufferAuslegungParameter(Dictionary<string, double> werte, Dictionary<string, string> quellen,
                                         HashSet<string> ausTabelle)
        {
            _werte = werte;
            _quellen = quellen;
            _ausTabelle = ausTabelle;
        }

        /// <summary>Alle Werte, Schlüssel mit Präfix.</summary>
        public IReadOnlyDictionary<string, double> Werte => _werte;

        /// <summary>Allein die eingebauten Vorgaben — ohne Datenbank (Tests, Rückfall).</summary>
        public static PufferAuslegungParameter Vorgabe() => Mit(null);

        /// <summary>
        /// Die eingebauten Vorgaben, einzelne Werte überschrieben (Schlüssel mit oder ohne Präfix) —
        /// ohne Datenbank. Ein nicht endlicher Wert wird benannt abgelehnt.
        /// </summary>
        public static PufferAuslegungParameter Mit(IReadOnlyDictionary<string, double> ueberschreibungen)
        {
            var werte = new Dictionary<string, double>(StringComparer.Ordinal);
            var quellen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (PufferVorgabe v in PufferAuslegungVorgaben.EINTRAEGE)
            {
                werte[v.Schluessel] = v.Wert;
                quellen[v.Schluessel] = v.Quelle;
            }
            var aus = new HashSet<string>(StringComparer.Ordinal);
            if (ueberschreibungen != null)
                foreach (KeyValuePair<string, double> kv in ueberschreibungen)
                {
                    if (double.IsNaN(kv.Value) || double.IsInfinity(kv.Value))
                        throw new ArgumentException("Der Parameter " + kv.Key + " ist nicht endlich.");
                    string s = Voll(kv.Key);
                    werte[s] = kv.Value;
                    aus.Add(s);
                }
            return new PufferAuslegungParameter(werte, quellen, aus);
        }

        /// <summary>
        /// Liest die Vorgabetabelle über <see cref="DataRepository"/>; fehlt sie, gelten die eingebauten
        /// Werte. Ein Schlüssel, der nicht in der Tabelle steht, fällt auf die Vorgabe zurück.
        /// </summary>
        public static PufferAuslegungParameter Lesen()
        {
            PufferAuslegungParameter p = Vorgabe();
            if (!DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB_PARAMETER)) return p;
            DataTable t = DataRepository.GetDataTable(SQL_LESEN, new DbParam("@s", PufferAuslegungVorgaben.PRAEFIX + "%"));
            if (t == null) return p;
            foreach (DataRow r in t.Rows)
            {
                if (!(r["Schluessel"] is string s) || r["Wert"] == DBNull.Value) continue;
                double w = Convert.ToDouble(r["Wert"], CultureInfo.InvariantCulture);
                if (double.IsNaN(w) || double.IsInfinity(w)) continue;
                p._werte[s] = w;
                if (r["Quelle"] is string q && q.Length > 0) p._quellen[s] = q;
                p._ausTabelle.Add(s);
            }
            return p;
        }

        /// <summary>
        /// Liest den Satz eines Projekts: eingebaute Vorgaben, darüber die Vorgabetabelle, darüber die
        /// Projektkopie (<see cref="ProjektPufferparameter"/>), sobald das Projekt eine führt. Ohne
        /// Projekt (<paramref name="idProjekt"/> ≤ 0) wie <see cref="Lesen()"/>.
        /// </summary>
        public static PufferAuslegungParameter Lesen(int idProjekt)
        {
            PufferAuslegungParameter p = Lesen();
            if (idProjekt <= 0 || !ProjektPufferparameter.Vorhanden()) return p;
            DataTable t = DataRepository.GetDataTable(ProjektPufferparameter.SQL_LESEN, new DbParam("@p", idProjekt),
                                                      new DbParam("@s", PufferAuslegungVorgaben.PRAEFIX + "%"));
            if (t == null) return p;
            foreach (DataRow r in t.Rows)
            {
                if (!(r["Schluessel"] is string s) || r["Wert"] == DBNull.Value) continue;
                double w = Convert.ToDouble(r["Wert"], CultureInfo.InvariantCulture);
                if (double.IsNaN(w) || double.IsInfinity(w)) continue;
                p._werte[s] = w;
                if (r["Quelle"] is string q && q.Length > 0) p._quellen[s] = q;
                p._ausTabelle.Add(s);
            }
            return p;
        }

        private static string Voll(string glied) =>
            glied.StartsWith(PufferAuslegungVorgaben.PRAEFIX, StringComparison.Ordinal) ? glied : PufferAuslegungVorgaben.PRAEFIX + glied;

        /// <summary>Der Wert zum Schlüssel (mit oder ohne Präfix); ein unbekannter Schlüssel wird benannt abgelehnt.</summary>
        public double Wert(string glied)
        {
            string s = Voll(glied);
            if (_werte.TryGetValue(s, out double w)) return w;
            throw new KeyNotFoundException("Der Parameter " + s + " ist weder gepflegt noch vorgegeben.");
        }

        /// <summary>Der Wert oder <paramref name="ersatz"/>, wenn der Schlüssel fehlt.</summary>
        public double WertOder(string glied, double ersatz) => _werte.TryGetValue(Voll(glied), out double w) ? w : ersatz;

        /// <summary>Gibt es den Schlüssel?</summary>
        public bool Hat(string glied) => _werte.ContainsKey(Voll(glied));

        /// <summary>Die Quelle (Zitat) zum Schlüssel; leer, wenn keine bekannt ist.</summary>
        public string Quelle(string glied) => _quellen.TryGetValue(Voll(glied), out string q) ? q : "";

        /// <summary>Kam der Wert aus der Tabelle bzw. einer Überschreibung (sonst: eingebaute Vorgabe)?</summary>
        public bool AusTabelle(string glied) => _ausTabelle.Contains(Voll(glied));

        /// <summary>Ein Kriterienschalter der Vorlage (<c>Vorlage.&lt;Typ&gt;.&lt;Kriterium&gt;</c>); fehlt er: aus.</summary>
        public bool VorlageAn(string typ, string kriterium) =>
            WertOder(PufferAuslegungVorgaben.VorlageSchluessel(typ, kriterium), 0) >= 0.5;

        /// <summary>Der Vorlagenschalter des Aufheizkriteriums K12 für das Nutzungsprofil; ohne Profil oder Schlüssel: aus.</summary>
        public bool AufheizAn(PufferNutzungsprofil? profil) =>
            profil.HasValue && WertOder(PufferAuslegungVorgaben.AUFHEIZ_NUTZUNG + profil.Value, 0) >= 0.5;

        /// <summary>Ein Beispielwert der Vorlage; fehlt er: <paramref name="ersatz"/>.</summary>
        public double VorlageWert(string typ, string glied, double ersatz) =>
            WertOder(PufferAuslegungVorgaben.VorlageSchluessel(typ, glied), ersatz);
    }
}
