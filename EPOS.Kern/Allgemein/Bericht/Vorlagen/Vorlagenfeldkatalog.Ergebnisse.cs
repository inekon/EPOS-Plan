using System;
using System.Collections.Generic;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Katalog v10 — die Stellen des Simulationsergebnisses</b> (Nachzug zu BV-E6, Konzept Berichtsvorlagen 9.5): was
    /// die Reiter des Simulationsergebnisses zeigen und bis Fassung 9 keinen Schlüssel hatte.
    /// <list type="bullet">
    /// <item>die <b>Ergebnisbilder je Stand</b> <c>stand.bild.&lt;name&gt;</c> (<see cref="Berichtsbilder.Ergebnisbilder"/>:
    /// Bedarf Wärme, Strom und Kälte, Wärmepumpe mit Stromverbrauch und Streuwolke, Heizkessel, Solarthermie, BHKW,
    /// Photovoltaik) samt Schalter <c>hat.bild.&lt;name&gt;</c> — Word als Bild, Excel als Diagramm; die Streuwolke nur
    /// Word (keine Punktwolke in den Excel-Diagrammen);</item>
    /// <item>die <b>Kennwerte des Speicherlaufs</b> <c>stand.speicher.*</c> der Einzelanlage (die Kacheln des Reiters
    /// „Stromspeicher“) und ihr Schalter <c>stand.hat_speicherlauf</c>;</item>
    /// <item>die <b>solare Deckung</b> <c>stand.solarthermie.deckung</c> (die Solarthermie-Kachel der Autarkieanalyse).</item>
    /// </list>
    /// <b>Katalog v12</b> ergänzt zwei Ergebnisbilder (<see cref="Berichtsbilder.ErgebnisbilderFassung12"/>): die Stromlast
    /// des BHKW (<c>stand.bild.bhkw_strom</c>) und die Kälteproduktion (<c>stand.bild.kaelte_produktion</c>, leer ohne
    /// Kälte), Word und Excel.
    /// <b>Katalog v13</b> ergänzt die Kennzahlen der freien Kühlung der Wärmepumpe über die Wärmequelle
    /// (<c>kaelte.wp.frei</c>, <c>kaelte.wp.frei_stunden</c>; KU3-6) mit ihren erzeugten Einträgen.
    /// <b>Katalog v14</b> ergänzt die Stunden der Wärmepumpe außerhalb der Kennlinienstützstellen am gerechneten Vorlauf
    /// (<c>wp.vorlauf.darunter_stunden</c>, <c>wp.vorlauf.darueber_stunden</c>; VW1b, E88) mit ihren erzeugten Einträgen.
    /// Alle im Kontext Stand, ohne Zwilling der Paarsicht; die Positionsform <c>stand.&lt;n&gt;.*</c> gilt für sie wie
    /// für jeden Standwert. Die Quellen lesen nur den Wertesatz — das gespeicherte Ergebnis und den Zeitreihensatz des
    /// Laufs —, nie die Datenbank.
    /// </summary>
    public static partial class Vorlagenfeldkatalog
    {
        /// <summary>Die Fassung der Ergebnisstellen (Katalog v10).</summary>
        internal const int FASSUNG_ERGEBNISSE = 10;

        /// <summary>Die Fassung der Stromlast des BHKW und der Kälteproduktion (Katalog v12).</summary>
        internal const int FASSUNG_STROM_KAELTE = 12;

        /// <summary>Die Fassung der freien Kühlung der Wärmepumpe über die Wärmequelle (Katalog v13, KU3-6).</summary>
        internal const int FASSUNG_FREIE_KUEHLUNG_WP = 13;

        /// <summary>Die Fassung des Ausweises der Vorlaufwahl der Wärmepumpe (Katalog v14, VW1b).</summary>
        internal const int FASSUNG_VORLAUFWAHL_WP = 14;

        /// <summary>
        /// Die Kennzahlen der Kältemaschine als Anlage (Schemaschritt 183) kamen mit Katalog v12 — ihre erzeugten
        /// Einträge (Stammzahl, Beschriftung, Einheit) stehen erst ab dieser Fassung, frühere Listen bleiben, wie sie
        /// ausgeliefert sind.
        /// </summary>
        private static readonly HashSet<string> KennzahlenStromKaelte = new(StringComparer.Ordinal)
        {
            KennzahlenKatalog.SCHLUESSEL_KM_ERZEUGUNG, KennzahlenKatalog.SCHLUESSEL_KM_STROM,
            KennzahlenKatalog.SCHLUESSEL_KM_HILFSSTROM, KennzahlenKatalog.SCHLUESSEL_KM_JAZ,
            KennzahlenKatalog.SCHLUESSEL_KM_FREI, KennzahlenKatalog.SCHLUESSEL_KM_FREI_STUNDEN,
            KennzahlenKatalog.SCHLUESSEL_KM_TAKT,
        };

        /// <summary>
        /// Die Kennzahlen der freien Kühlung der Wärmepumpe über die Wärmequelle (Schemaschritt 187, KU3-6) kamen
        /// mit Katalog v13 — ihre erzeugten Einträge stehen erst ab dieser Fassung.
        /// </summary>
        private static readonly HashSet<string> KennzahlenFreieKuehlungWp = new(StringComparer.Ordinal)
        {
            KennzahlenKatalog.SCHLUESSEL_WP_FREI, KennzahlenKatalog.SCHLUESSEL_WP_FREI_STUNDEN,
        };

        /// <summary>
        /// Die Kennzahlen der Vorlaufwahl der Wärmepumpe (Schemaschritt 188, VW1b) kamen mit Katalog v14 — ihre
        /// erzeugten Einträge stehen erst ab dieser Fassung.
        /// </summary>
        private static readonly HashSet<string> KennzahlenVorlaufwahlWp = new(StringComparer.Ordinal)
        {
            KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUNTER, KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUEBER,
        };

        /// <summary>Die Fassung, seit der die erzeugten Einträge einer Kennzahl im Katalog stehen.</summary>
        internal static int SeitDerKennzahl(string schluessel) => Math.Max(
            KennzahlenVorlaufwahlWp.Contains(schluessel) ? FASSUNG_VORLAUFWAHL_WP
            : KennzahlenFreieKuehlungWp.Contains(schluessel) ? FASSUNG_FREIE_KUEHLUNG_WP
            : KennzahlenStromKaelte.Contains(schluessel) ? FASSUNG_STROM_KAELTE : 1,
            schluessel != null && SeitJeKennzahl.TryGetValue(schluessel, out int seit) ? seit : 1);

        /// <summary>
        /// Das <see cref="Kennzahl.Seit"/> jeder Kennzahl über 1 (Gruppe „Gebäude“: Katalog v16, KP3 Welle O3b) — so liefert auch
        /// der Weg über den Schlüssel dieselbe Fassung wie der über die Kennzahl. Eine Eigenschaft statt eines Feldinitialisierers:
        /// Die Reihenfolge der statischen Initialisierer über die Teildateien ist nicht festgelegt.
        /// </summary>
        private static Dictionary<string, int> SeitJeKennzahl => _seitJeKennzahl ??=
            KennzahlenKatalog.Alle().Where(k => k.Seit > 1).ToDictionary(k => k.Schluessel, k => k.Seit, StringComparer.Ordinal);

        private static Dictionary<string, int> _seitJeKennzahl;

        /// <summary>
        /// Die Fassung, seit der die erzeugten Einträge einer Kennzahl im Katalog stehen: ihr <see cref="Kennzahl.Seit"/>
        /// (Gruppe „Gebäude“: Katalog v16, KP3 Welle O3b) oder die Fassung ihrer Schlüsselgruppe, die spätere gilt.
        /// </summary>
        internal static int SeitDerKennzahl(Kennzahl k) => k == null ? 1 : Math.Max(k.Seit, SeitDerKennzahl(k.Schluessel));

        /// <summary>Die Einträge der Fassung 10 in Katalogfolge.</summary>
        private static IEnumerable<Vorlagenfeld> Ergebnisse()
        {
            const Vorlagenfeldkontext S = Vorlagenfeldkontext.Stand;
            var l = new List<Vorlagenfeld>();

            // ---- die Ergebnisbilder je Stand ----
            foreach (string name in Berichtsbilder.Ergebnisbilder)
            {
                string bild = name;
                string schluessel = "stand.bild." + bild;
                int seit = Berichtsbilder.ErgebnisbilderFassung12.Contains(bild, StringComparer.Ordinal)
                    ? FASSUNG_STROM_KAELTE : FASSUNG_ERGEBNISSE;
                l.AddRange(Bild(schluessel, S, Vorlagenbedarf.Zeitreihen, true,
                                w => MitStand(w, v => Ergebnisbild(w, v, bild)), seit));
                _bildgroessen[schluessel] = (FAKTOR_BREIT, Bildmass.MIN_BREITE);
            }

            // ---- der Speicherlauf der Einzelanlage ----
            l.Add(new Vorlagenfeld("stand.hat_speicherlauf", Vorlagenfeldart.Schalter, S,
                                   w => MitStand(w, v => Speicherlauf(v) != null))
                { Seit = FASSUNG_ERGEBNISSE });
            l.Add(new Vorlagenfeld("stand.speicher.betriebsart", Vorlagenfeldart.Text, S,
                                   w => Speicherwert(w, s => Textwert(s.Betriebsart)))
                { Seit = FASSUNG_ERGEBNISSE });
            l.Add(new Vorlagenfeld("stand.speicher.berechnungsart", Vorlagenfeldart.Text, S,
                                   w => Speicherwert(w, s => Textwert(s.Berechnungsart)))
                { Seit = FASSUNG_ERGEBNISSE });
            l.Add(Speicherzahl("stand.speicher.ertrag", s => s.Ertrag_Aequivalent, "N2", "€/a"));
            l.Add(Speicherzahl("stand.speicher.ueberschuss", s => s.Jahresueberschuss, "N2", "€/a"));
            l.Add(new Vorlagenfeld("stand.speicher.amortisation", Vorlagenfeldart.Zahl, S,
                                   w => Speicherwert(w, s => s.Investition > 0.0 && IstEndlich(s.Amortisation_Statisch)
                                       ? (object)s.Amortisation_Statisch : Grund(w, nameof(R.BV_GRUND_KEINE_SPEICHERAMORTISATION))))
                { Seit = FASSUNG_ERGEBNISSE, Format = "N1", Einheit = "a" });
            l.Add(Speicherzahl("stand.speicher.vollzyklen", s => s.Vollzyklen, "N1", ""));
            l.Add(Speicherzahl("stand.speicher.eigenverbrauch", s => s.Eigenverbrauchsquote, "N1", "%"));
            l.Add(Speicherzahl("stand.speicher.autarkie", s => s.Autarkiegrad, "N1", "%"));

            // ---- die solare Deckung ----
            l.Add(new Vorlagenfeld("stand.solarthermie.deckung", Vorlagenfeldart.Zahl, S,
                                   w => MitStand(w, v => Solardeckung(w, v)))
                { Seit = FASSUNG_ERGEBNISSE, Format = "N2", Einheit = "%" });
            return l;
        }

        // =====================================================================
        //  Quellen — sie lesen nur den Wertesatz
        // =====================================================================

        /// <summary>Ein Ergebnisbild des Stands aus seinem Zeitreihensatz; ohne Reihen mit Grund.</summary>
        private static object Ergebnisbild(Berichtswerte w, VariantenDaten v, string name)
        {
            ZeitreihenSatz z = v.Zeitreihen;
            if (z == null) return Grund(w, nameof(R.BV_GRUND_KEINE_ZEITREIHEN));
            if (Berichtsbilder.ErgebnisbildPlan(name, z) == null) return Grund(w, nameof(R.BV_GRUND_BILD_OHNE_DATEN));
            return new Diagrammbild(m => Berichtsbilder.Ergebnisbild(name, z), FAKTOR_BREIT, Bildmass.MIN_BREITE,
                                    w.Text(nameof(R.BV_GRUND_BILD_OHNE_DATEN)));
        }

        /// <summary>Der gespeicherte Lauf der Einzelanlage des Stands (der erste Stromspeicher des Ergebnisses); <c>null</c> ohne.</summary>
        internal static ErgebnisStromspeicherModel Speicherlauf(VariantenDaten v)
        {
            return v?.Ergebnis?.Stromspeicher?.FirstOrDefault(s => s != null);
        }

        private static object Speicherwert(Berichtswerte w, Func<ErgebnisStromspeicherModel, object> wert)
        {
            return MitStand(w, v =>
            {
                if (v.Ergebnis == null) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
                ErgebnisStromspeicherModel s = Speicherlauf(v);
                return s == null ? Grund(w, nameof(R.BV_GRUND_KEIN_SPEICHERLAUF)) : wert(s);
            });
        }

        private static Vorlagenfeld Speicherzahl(string schluessel, Func<ErgebnisStromspeicherModel, double> wert,
                                                 string format, string einheit)
        {
            return new Vorlagenfeld(schluessel, Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stand,
                                    w => Speicherwert(w, s => IstEndlich(wert(s)) ? (object)wert(s)
                                                                               : Grund(w, nameof(R.BV_GRUND_NICHT_VERFUEGBAR))))
            {
                Seit = FASSUNG_ERGEBNISSE,
                Format = format,
                Einheit = einheit,
            };
        }

        /// <summary>Die Deckung des Wärmebedarfs durch die Solarthermie aus dem gespeicherten Ergebnis.</summary>
        private static object Solardeckung(Berichtswerte w, VariantenDaten v)
        {
            if (v.Ergebnis == null) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            ErgebnisSolarthermieModel s = v.Ergebnis.Solarthermie;
            if (s == null || !v.Ergebnis.Sim_Solarthermie) return Grund(w, nameof(R.BV_GRUND_KEINE_SOLARTHERMIE));
            return IstEndlich(s.Waermebedarfsdeckung) ? (object)s.Waermebedarfsdeckung : Grund(w, nameof(R.BV_GRUND_NICHT_VERFUEGBAR));
        }

        private static object Textwert(string wert) => string.IsNullOrWhiteSpace(wert) ? null : wert.Trim();

        private static bool IstEndlich(double wert) => !double.IsNaN(wert) && !double.IsInfinity(wert);
    }
}
