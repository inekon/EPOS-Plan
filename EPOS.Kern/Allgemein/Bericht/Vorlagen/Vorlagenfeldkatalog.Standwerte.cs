using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Katalog v3</b> (<see cref="Vorlagenfeldkatalog.KATALOGFASSUNG"/> 3, Etappe BV-E4, Konzept Berichtsvorlagen
    /// 4.5, 4.7, 4.10, 4.11, 5.2, 9.5, Anhang A) — die Werte je Stand, je Gebäude und über die Gruppe:
    /// <list type="bullet">
    /// <item><c>stand.*</c> (Kontext Stand, Quelle <see cref="Berichtswerte.LaufenderStand"/>): Stammdaten und Anzeige,
    /// Standschalter und Fehlertext, <c>stand.kennzahl.&lt;k&gt;</c>, <c>stand.delta(_prozent).&lt;k&gt;</c>,
    /// <c>stand.wirtschaft.&lt;zeile&gt;</c> je Szenario samt <c>.grund</c> und Warnliste, <c>stand.bandbreite.*</c>;</item>
    /// <item><c>stamm.wirtschaft.&lt;zeile&gt;</c> (Kontext Stamm) und <c>wirtschaft.beste.&lt;zeile&gt;</c> über
    /// <see cref="BesteVariante.Waehle"/>, dazu <c>wirtschaft.*</c> der Gruppe: Warnungen, Hinweise, Methodik,
    /// Parameter, Szenarien;</item>
    /// <item><c>vergleich.spanne|minimum|maximum.&lt;k&gt;</c>, der Alias <c>vergleich.beste_variante</c>, <c>gebaeude.*</c> (Kontext Gebäude) und die Datenschalter <c>hat.*</c>
    /// (im Block für den laufenden Stand, sonst für die Gruppe);</item>
    /// <item>je Eintrag des Kontexts Stand sein Zwilling <c>stand.a.*</c> und <c>stand.b.*</c> der Paarsicht
    /// (<see cref="Berichtswerte.StandA"/>, <see cref="Berichtswerte.StandB"/>), gültig überall.</item>
    /// </list>
    ///
    /// <para><b>Die Quellen lesen nur den Wertesatz</b> — den Berichtsbaum und
    /// <see cref="Berichtswerte.Wirtschaft"/>. Die Wirtschaftlichkeitszeilen sind die der Kennzahltafel
    /// (<see cref="WirtschaftsBerichtswerte.Zeilen"/> gegen <see cref="WirtschaftsBerichtswerte.IdReferenzTafel"/>),
    /// der Wert einer Zelle ist <see cref="WirtZeile.ExcelWert"/> — Word und Excel tragen dieselbe Zahl. Ein Leerwert
    /// ist nie 0; er trägt den Grund der Zelle (<see cref="WirtZeile.Grund"/>) oder einen der Gründe des Katalogs.</para>
    /// </summary>
    public static partial class Vorlagenfeldkatalog
    {
        /// <summary>Die Fassung der Standwerte (Etappe BV-E4).</summary>
        private const int FASSUNG_STAND = 3;

        /// <summary>Musterschlüssel der Kennzahlen des laufenden Stands.</summary>
        public const string MUSTER_STAND_KENNZAHL = "stand.kennzahl.<k>";

        /// <summary>Musterschlüssel der Abweichung gegenüber dem Stamm.</summary>
        public const string MUSTER_STAND_DELTA = "stand.delta.<k>";

        /// <summary>Musterschlüssel der Abweichung in Prozent.</summary>
        public const string MUSTER_STAND_DELTA_PROZENT = "stand.delta_prozent.<k>";

        /// <summary>Musterschlüssel der Spanne einer Kennzahl über die Stände.</summary>
        public const string MUSTER_VERGLEICH_SPANNE = "vergleich.spanne.<k>";

        /// <summary>Musterschlüssel des kleinsten Werts einer Kennzahl über die Stände.</summary>
        public const string MUSTER_VERGLEICH_MINIMUM = "vergleich.minimum.<k>";

        /// <summary>Musterschlüssel des größten Werts einer Kennzahl über die Stände.</summary>
        public const string MUSTER_VERGLEICH_MAXIMUM = "vergleich.maximum.<k>";

        /// <summary>Alias von <c>wirtschaft.beste.anzeige</c> (Anhang A, Anwenderentscheid BV-E4-2).</summary>
        public const string ALIAS_BESTE_VARIANTE = "vergleich.beste_variante";

        /// <summary>Musterschlüssel der Wirtschaftlichkeitszeilen des Stammprojekts.</summary>
        public const string MUSTER_STAMM_WIRTSCHAFT = "stamm.wirtschaft.<zeile>";

        /// <summary>Musterschlüssel der Wirtschaftlichkeitszeilen des laufenden Stands.</summary>
        public const string MUSTER_STAND_WIRTSCHAFT = "stand.wirtschaft.<zeile>";

        /// <summary>Musterschlüssel des Grunds einer leeren Wirtschaftlichkeitszelle.</summary>
        public const string MUSTER_STAND_WIRTSCHAFT_GRUND = "stand.wirtschaft.<zeile>.grund";

        /// <summary>Musterschlüssel der Wirtschaftlichkeitszeilen der besten Variante.</summary>
        public const string MUSTER_BESTE_WIRTSCHAFT = "wirtschaft.beste.<zeile>";

        /// <summary>Musterschlüssel der Parameter der Wirtschaftlichkeit.</summary>
        public const string MUSTER_WIRTSCHAFT_PARAMETER = "wirtschaft.parameter.<name>";

        /// <summary>Musterschlüssel der Szenarioangaben.</summary>
        public const string MUSTER_SZENARIO = "wirtschaft.szenario.<s>.<angabe>";

        /// <summary>Musterschlüssel des Paarvergleichs, Stand A.</summary>
        public const string MUSTER_STAND_A = "stand.a.<schluessel>";

        /// <summary>Musterschlüssel des Paarvergleichs, Stand B.</summary>
        public const string MUSTER_STAND_B = "stand.b.<schluessel>";

        /// <summary>Vorsilbe der Standwerte.</summary>
        private const string STAND = "stand.";

        private const string FORMAT_EURO = "N0";

        // =====================================================================
        //  Die Zeilen der Wirtschaftlichkeit (Konzept 4.5, 5.2)
        // =====================================================================

        /// <summary>
        /// Eine Zeile der Kennzahltafel, wie der Katalog sie führt: der Zeilenschlüssel von
        /// <see cref="WirtschaftlichkeitZeilen"/> (Großschreibung, im Katalog klein), der Titel als
        /// Ressourcenname (Beschreibung), Textzeile oder Zahl, Format und Einheit wie am Titel.
        /// </summary>
        internal sealed class Wirtschaftszeile
        {
            internal Wirtschaftszeile(string zeile, string titel, string einheit, string format = FORMAT_EURO,
                                      bool text = false, string komponente = null)
            {
                Zeile = zeile;
                Titel = titel;
                Einheit = einheit;
                Format = format;
                Text = text;
                Komponente = komponente;
            }

            /// <summary>Der Zeilenschlüssel der Tafel (<see cref="WirtZeile.Schluessel"/>).</summary>
            internal string Zeile { get; }

            /// <summary>Der Katalogteil: der Zeilenschlüssel klein.</summary>
            internal string Schluessel { get { return Zeile.ToLowerInvariant(); } }

            /// <summary>Der Ressourcenname des Titels.</summary>
            internal string Titel { get; }

            internal string Einheit { get; }

            internal string Format { get; }

            internal bool Text { get; }

            /// <summary>Die Komponente einer zur Laufzeit gebildeten Zeile (<c>{0}</c> im Titel); sonst <c>null</c>.</summary>
            internal string Komponente { get; }

            /// <summary>Der Titel in einer Kultur, bei Komponentenzeilen mit dem Namen der Komponente.</summary>
            internal string Bezeichnung(CultureInfo kultur)
            {
                string titel = Ressource(Titel, kultur);
                if (Komponente == null) return titel;
                string name = Ressource(Komponente == WirtZeile.KOMPONENTE_BHKW ? nameof(R.WIRT_ERL_K_BHKW)
                                      : Komponente == WirtZeile.KOMPONENTE_PV ? nameof(R.WIRT_ERL_K_PV)
                                      : Komponente == WirtZeile.KOMPONENTE_KESSEL ? nameof(R.WIRT_ERL_K_KESSEL)
                                      : nameof(R.WIRT_ERL_PROJEKTWEIT), kultur);
                try { return string.Format(kultur, titel, name); }
                catch (FormatException) { return titel; }
            }
        }

        /// <summary>
        /// Die Zeilen der Wirtschaftlichkeit mit Wert — alle Zeilen der Definition außer den Überschriften der Blöcke
        /// und Komponenten (sie tragen keinen Wert), den Kohärenzzeilen (<c>KOHAERENZ_n</c>, nach Zahl der Hinweise
        /// gebildet) und den Anlagenzeilen der Energiekosten (<c>ENERGIEKOSTEN_ANLAGE_&lt;name&gt;</c>, nach dem
        /// Anlagennamen gebildet). Die zur Laufzeit je Komponente gebildeten Zeilen sind aufgezählt (Konzept 4.5): die
        /// Teilsummen des Blocks A je <see cref="WirtZeile.Komponentenfolge"/>, die vermiedenen Kosten des Blocks B je
        /// Anlage (BHKW, PV, Kessel).
        /// </summary>
        internal static readonly IReadOnlyList<Wirtschaftszeile> Wirtschaftszeilen = BildeWirtschaftszeilen();

        private static List<Wirtschaftszeile> BildeWirtschaftszeilen()
        {
            const string EUR = "€", EUR_A = "€/a";
            var l = new List<Wirtschaftszeile>
            {
                new Wirtschaftszeile("SPEICHER_KONTEXT", nameof(R.WIRT_ZEILE_SPEICHER), null, text: true),
                new Wirtschaftszeile("INVESTITION", nameof(R.WIRT_ZEILE_INVESTITION), EUR),
                new Wirtschaftszeile("ZUSCHUSS", nameof(R.WIRT_ZEILE_ZUSCHUSS), EUR),
                new Wirtschaftszeile("BETRIEBSKOSTEN", nameof(R.WIRT_ZEILE_BETRIEBSKOSTEN), EUR_A),
                new Wirtschaftszeile("ENERGIEKOSTEN", nameof(R.WIRT_ZEILE_ENERGIEKOSTEN), EUR_A),
                new Wirtschaftszeile("STROMKOSTEN_TARIF", nameof(R.WIRT_ZEILE_STROMKOSTEN_RESTSTROM), EUR_A),
                new Wirtschaftszeile("BEZUGSSPITZE_STROM", nameof(R.WIRT_ZEILE_BEZUGSSPITZE_STROM), "kW"),
                new Wirtschaftszeile("CO2_BEHG", nameof(R.WIRT_ZEILE_CO2_BEHG), EUR_A),
                new Wirtschaftszeile("ERL_A_KWKG", nameof(R.WIRT_ERL_A_KWKG), EUR_A),
                new Wirtschaftszeile("ERL_A_KWKG_GRUND", nameof(R.WIRT_ERL_HERLEITUNG), null, text: true),
                new Wirtschaftszeile("ERL_A1_EINSPEISUNG", nameof(R.WIRT_ERL_A1_EINSPEISUNG), EUR_A),
                new Wirtschaftszeile("ERL_A1_SATZ", nameof(R.WIRT_ERL_A1_SATZ), null, text: true),
                new Wirtschaftszeile("ERL_A2_EIGEN", nameof(R.WIRT_ERL_A2_EIGEN), EUR_A),
                new Wirtschaftszeile("ERL_A2_SATZ", nameof(R.WIRT_ERL_A2_SATZ), null, text: true),
                new Wirtschaftszeile("VBH_ELEKTRISCH", nameof(R.WIRT_ZEILE_VBH_ELEKTRISCH), "h/a"),
                new Wirtschaftszeile("ERL_A_KWKG_PAUSCHALE", nameof(R.WIRT_ERL_A_KWKG_PAUSCHALE), EUR),
                new Wirtschaftszeile("ERL_A_ENERGIESTEUER", nameof(R.WIRT_ERL_A_ENERGIESTEUER), EUR_A),
                new Wirtschaftszeile("ERL_A_ENERGIESTEUER_SATZ", nameof(R.WIRT_ERL_HERLEITUNG), null, text: true),
                new Wirtschaftszeile("ERL_A_ENERGIESTEUER_54", nameof(R.WIRT_ERL_ENERGIEST_54), EUR_A),
                new Wirtschaftszeile("ERL_A_ENERGIESTEUER_54_SATZ", nameof(R.WIRT_ERL_HERLEITUNG), null, text: true),
                new Wirtschaftszeile("ERL_A_STROMST_ENTLASTUNG", nameof(R.WIRT_ERL_A_STROMST_ENTLASTUNG), EUR_A),
                new Wirtschaftszeile("ERL_A_STROMST_ENTLASTUNG_GRUND", nameof(R.WIRT_ERL_HERLEITUNG), null, text: true),
                new Wirtschaftszeile("ERL_A_STROMST_BEFREIUNG", nameof(R.WIRT_ERL_A_STROMST_BEFREIUNG), EUR_A),
                new Wirtschaftszeile("EINSPEISEERLOES", nameof(R.WIRT_ERL_A_EINSPEISUNG), EUR_A),
                new Wirtschaftszeile("EINSPEISEERLOES_GRUND", nameof(R.WIRT_ERL_HERLEITUNG), null, text: true),
                new Wirtschaftszeile("EINSPEISEERLOES_PV", nameof(R.WIRT_ZEILE_EINSPEISEERLOES_PV), EUR_A),
                new Wirtschaftszeile("EINSPEISEERLOES_KWK", nameof(R.WIRT_ZEILE_EINSPEISEERLOES_KWK), EUR_A),
                new Wirtschaftszeile("PV_FORM", nameof(R.WIRT_ZEILE_PV_FORM), null, text: true),
                new Wirtschaftszeile("PV_AW", nameof(R.WIRT_ZEILE_PV_AW), "ct/kWh", "N2"),
                new Wirtschaftszeile("PV_HERKUNFT", nameof(R.WIRT_ZEILE_PV_HERKUNFT), null, text: true),
                new Wirtschaftszeile("PV_MARKTPRAEMIE", nameof(R.WIRT_ZEILE_PV_MARKTPRAEMIE), EUR_A),
                new Wirtschaftszeile("PV_51A", nameof(R.WIRT_ZEILE_PV_51A), EUR),
            };
            foreach (string k in WirtZeile.Komponentenfolge)
                l.Add(new Wirtschaftszeile("ERL_A_TEIL_" + Kennform(k), nameof(R.WIRT_ERL_TEILSUMME), EUR_A, komponente: k));
            l.AddRange(new[]
            {
                new Wirtschaftszeile("ERL_A_SUMME", nameof(R.WIRT_ERL_A_SUMME), EUR_A),
                new Wirtschaftszeile("ERL_B_STROMST_BEFREIUNG", nameof(R.WIRT_ZEILE_STROMST_BEFREIUNG_AUSWEIS), EUR_A),
            });
            foreach (string k in WirtZeile.Komponentenfolge.Where(k => k != WirtZeile.KOMPONENTE_PROJEKTWEIT))
            {
                l.Add(new Wirtschaftszeile("VERMIEDEN_BRUTTO_" + Kennform(k), nameof(R.WIRT_ERL_B1_BRUTTO), EUR_A, komponente: k));
                l.Add(new Wirtschaftszeile("VERMIEDEN_HERLEITUNG_" + Kennform(k), nameof(R.WIRT_ERL_HERLEITUNG), null, text: true, komponente: k));
                l.Add(new Wirtschaftszeile("ERL_B1_ABZUG_9B_" + Kennform(k), nameof(R.WIRT_ERL_B1_ABZUG_9B), EUR_A, komponente: k));
                l.Add(new Wirtschaftszeile("ERL_B1_EFFEKTIV_" + Kennform(k), nameof(R.WIRT_ERL_B1_WIRKSAM), EUR_A, komponente: k));
            }
            l.AddRange(new[]
            {
                new Wirtschaftszeile("VERMIEDEN_LEISTUNG", nameof(R.WIRT_ZEILE_VERMIEDEN_LEISTUNG), EUR_A),
                new Wirtschaftszeile("VERMIEDEN_GESAMT", nameof(R.WIRT_ERL_B1_BRUTTO), EUR_A),
                new Wirtschaftszeile("VERMIEDEN_ARBEIT", nameof(R.WIRT_ZEILE_VERMIEDEN_ARBEIT), EUR_A),
                new Wirtschaftszeile("ERL_B1_ABZUG_9B", nameof(R.WIRT_ERL_B1_ABZUG_9B), EUR_A),
                new Wirtschaftszeile("ERL_B1_EFFEKTIV", nameof(R.WIRT_ERL_B1_EFFEKTIV), EUR_A),
                new Wirtschaftszeile("PV_VERMIEDEN", nameof(R.WIRT_ZEILE_PV_VERMIEDEN), EUR_A),
                new Wirtschaftszeile("PV_AUSFALL_KWH", nameof(R.WIRT_ZEILE_PV_AUSFALL_KWH), "kWh/a"),
                new Wirtschaftszeile("PV_AUSFALL_EUR", nameof(R.WIRT_ZEILE_PV_AUSFALL_EUR), EUR_A),
                new Wirtschaftszeile("PV_KAPPUNG", nameof(R.WIRT_ZEILE_PV_KAPPUNG), "kWh/a"),
                new Wirtschaftszeile("ERSATZ", nameof(R.WIRT_ZEILE_ERSATZ), EUR),
                new Wirtschaftszeile("RESTWERT", nameof(R.WIRT_ZEILE_RESTWERT), EUR),
                new Wirtschaftszeile("KAPITALWERT_DIFF", nameof(R.WIRT_ZEILE_KAPITALWERT_DIFF), EUR),
                new Wirtschaftszeile("ANNUITAET", nameof(R.WIRT_ZEILE_ANNUITAET), EUR_A),
                new Wirtschaftszeile("AMORTISATION", nameof(R.WIRT_ZEILE_AMORTISATION), "a", "N1"),
                new Wirtschaftszeile("IRR", nameof(R.WIRT_ZEILE_IRR), "%", "N1"),
                new Wirtschaftszeile("GESTEHUNGSKOSTEN", nameof(R.WIRT_ZEILE_GESTEHUNGSKOSTEN), "€/kWh", "N3"),
                new Wirtschaftszeile("NETTOBARWERT", nameof(R.WIRT_ZEILE_NETTOBARWERT), EUR),
                new Wirtschaftszeile("STEUER_HERKUNFT", nameof(R.WIRT_ZEILE_STEUER_HERKUNFT), null, text: true),
            });
            return l;
        }

        /// <summary>Die Kennung einer Komponente im Zeilenschlüssel — wie in <see cref="WirtschaftlichkeitZeilen"/>.</summary>
        private static string Kennform(string komponente)
        {
            return string.IsNullOrEmpty(komponente) ? "PROJEKTWEIT" : komponente;
        }

        /// <summary>Die drei Szenarien: Anhang des Schlüssels, Szenario des Kerns, Ressource des Namens.</summary>
        private static readonly (string Anhang, string Szenario, string Name, string Schluessel)[] Szenarien =
        {
            ("", WirtschaftlichkeitSzenario.ERWARTET, nameof(R.WIRT_SZEN_ERWARTET), "erwartet"),
            (".guenstig", WirtschaftlichkeitSzenario.BEST, nameof(R.WIRT_SZEN_BEST), "guenstig"),
            (".unguenstig", WirtschaftlichkeitSzenario.WORST, nameof(R.WIRT_SZEN_WORST), "unguenstig"),
        };

        // =====================================================================
        //  Die Einträge
        // =====================================================================

        /// <summary>Alle Einträge der Fassung 3 außer den Zwillingen der Paarsicht (<see cref="Paarsicht"/>).</summary>
        private static IEnumerable<Vorlagenfeld> Standwerte(List<Kennzahl> kennzahlen)
        {
            var l = new List<Vorlagenfeld>();
            l.AddRange(StandHandgepflegt());
            l.AddRange(StandKennzahlen(kennzahlen));
            l.AddRange(WirtschaftszeilenFelder());
            l.AddRange(Gruppenwerte());
            l.AddRange(VergleichKennzahlen(kennzahlen));
            l.AddRange(Gebaeudewerte());
            l.AddRange(Datenschalter());
            return l;
        }

        private static Vorlagenfeld Neu(string schluessel, Vorlagenfeldart art, Vorlagenfeldkontext kontext,
                                        Func<Berichtswerte, object> quelle)
        {
            return new Vorlagenfeld(schluessel, art, kontext, quelle) { Seit = FASSUNG_STAND };
        }

        private static IEnumerable<Vorlagenfeld> StandHandgepflegt()
        {
            const Vorlagenfeldkontext S = Vorlagenfeldkontext.Stand;
            return new[]
            {
                Neu("stand.rolle", Vorlagenfeldart.Text, S,
                    w => MitStand(w, v => BerichtTexte.T(v.IstStamm ? "Stamm" : "Variante", w.Englisch))),
                new Vorlagenfeld("stand.bezeichner", Vorlagenfeldart.Text, S,
                    w => MitStand(w, v => v.IstStamm ? BerichtTexte.T("(Stammprojekt)", w.Englisch) : v.Variantenname))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                Neu("stand.projektname", Vorlagenfeldart.Text, S, w => MitStand(w, v => v.Projektname)),
                new Vorlagenfeld("stand.hinweis", Vorlagenfeldart.Text, S, w => MitStand(w, v => Standhinweis(w, v)))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                new Vorlagenfeld("stand.beschreibung", Vorlagenfeldart.Text, S, w => StandProjekt(w, p => p.m_szBeschreibung))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                new Vorlagenfeld("stand.kunde", Vorlagenfeldart.Text, S, w => StandProjekt(w, p => p.m_szKunde))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                new Vorlagenfeld("stand.bearbeiter", Vorlagenfeldart.Text, S, w => StandProjekt(w, p => p.m_szBearbeiter))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                new Vorlagenfeld("stand.stromspeicher", Vorlagenfeldart.Text, S, Stromspeicher)
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                new Vorlagenfeld("stand.simulationsstand", Vorlagenfeldart.Datum, S, w => MitStand(w, v =>
                        v.SimulationsStand.HasValue ? (object)v.SimulationsStand.Value : Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS))))
                    { Seit = FASSUNG_STAND, Format = FORMAT_DATUM_ZEIT },
                Neu("stand.hat_fehler", Vorlagenfeldart.Schalter, S, w => MitStand(w, v => !string.IsNullOrEmpty(v.Fehler))),
                Neu("stand.veraltet", Vorlagenfeldart.Schalter, S, w => MitStand(w, v => v.ErgebnisVeraltet)),
                Neu("stand.frisch", Vorlagenfeldart.Schalter, S, w => MitStand(w, v => v.FrischSimuliert)),
                new Vorlagenfeld("stand.hat_zeitreihen", Vorlagenfeldart.Schalter, S, w => MitStand(w, v => v.Zeitreihen != null))
                    { Seit = FASSUNG_STAND, Bedarf = Vorlagenbedarf.Zeitreihen },
                new Vorlagenfeld("stand.fehler", Vorlagenfeldart.Text, S, w => MitStand(w, v => v.Fehler))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                Neu("stand.wirtschaft.warnungen", Vorlagenfeldart.Liste, S,
                    w => MitStand(w, v => Wirtschaftswarnungen(w, v))),
                Bandbreite("stand.bandbreite.unguenstig", z => z.Worst, FORMAT_EURO, "€"),
                Bandbreite("stand.bandbreite.erwartet", z => z.Erwartet, FORMAT_EURO, "€"),
                Bandbreite("stand.bandbreite.guenstig", z => z.Best, FORMAT_EURO, "€"),
                Bandbreite("stand.bandbreite.spanne", z => z.Spanne, FORMAT_EURO, "€"),
                Bandbreite("stand.bandbreite.amortisation", z => z.AmortisationJahre, "N1", "a"),
                Neu("stand.bandbreite.einstufung", Vorlagenfeldart.Text, S,
                    w => MitStand(w, v => Bandbreitenwert(w, v, z => z.Urteil == null ? null : z.Urteil.StufeText))),
            };
        }

        private static Vorlagenfeld Bandbreite(string schluessel, Func<BandbreitenZeile, double?> wert, string format, string einheit)
        {
            return new Vorlagenfeld(schluessel, Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stand,
                w => MitStand(w, v => Bandbreitenwert(w, v, z => wert(z))))
            {
                Seit = FASSUNG_STAND,
                Format = format,
                Einheit = einheit,
            };
        }

        /// <summary>Die erzeugten Kennzahlwerte des laufenden Stands: Wert, Abweichung, Abweichung in Prozent.</summary>
        private static IEnumerable<Vorlagenfeld> StandKennzahlen(List<Kennzahl> kennzahlen)
        {
            foreach (Kennzahl k in kennzahlen)
            {
                string s = k.Schluessel;
                Vorlagenbedarf bedarf = s == KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN ? Vorlagenbedarf.Zeitreihen : Vorlagenbedarf.Keiner;
                yield return new Vorlagenfeld("stand.kennzahl." + s, Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stand,
                    w => MitStand(w, v => Kennzahlwert(w, v, s)))
                {
                    Seit = FASSUNG_STAND,
                    Format = k.Format,
                    Einheit = k.Einheit,
                    Bedarf = bedarf,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_STAND_KENNZAHL, nameof(R.VF_MUSTER_STAND_KENNZAHL), s),
                };
            }
            foreach (Kennzahl k in kennzahlen.Where(k => k.DeltaAnzeigen))
            {
                string s = k.Schluessel;
                Vorlagenbedarf bedarf = s == KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN ? Vorlagenbedarf.Zeitreihen : Vorlagenbedarf.Keiner;
                yield return new Vorlagenfeld("stand.delta." + s, Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stand,
                    w => MitStand(w, v => Abweichung(w, v, s, false)))
                {
                    Seit = FASSUNG_STAND,
                    Format = k.Format,
                    Einheit = k.Einheit,
                    Bedarf = bedarf,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_STAND_DELTA, nameof(R.VF_MUSTER_STAND_DELTA), s),
                };
                yield return new Vorlagenfeld("stand.delta_prozent." + s, Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stand,
                    w => MitStand(w, v => Abweichung(w, v, s, true)))
                {
                    Seit = FASSUNG_STAND,
                    Format = "N1",
                    Einheit = "%",
                    Bedarf = bedarf,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_STAND_DELTA_PROZENT, nameof(R.VF_MUSTER_STAND_DELTA_PROZENT), s),
                };
            }
        }

        /// <summary>
        /// Je Kennzahl über die Stände (Kontext Gruppe): die Spanne, der kleinste und der größte Wert — neutral, ohne
        /// Richtung „besser“ (Anwenderentscheid BV-E4-2). Alle drei lesen dieselbe Wertemenge (<see cref="Kennzahlreihe"/>)
        /// und sind mit weniger als zwei Werten leer mit demselben Grund.
        /// </summary>
        private static IEnumerable<Vorlagenfeld> VergleichKennzahlen(List<Kennzahl> kennzahlen)
        {
            var arten = new (string Vorsilbe, string Muster, string MusterId, Func<List<double>, double> Wert)[]
            {
                ("vergleich.spanne.", MUSTER_VERGLEICH_SPANNE, nameof(R.VF_MUSTER_VERGLEICH_SPANNE), x => x.Max() - x.Min()),
                ("vergleich.minimum.", MUSTER_VERGLEICH_MINIMUM, nameof(R.VF_MUSTER_VERGLEICH_MINIMUM), x => x.Min()),
                ("vergleich.maximum.", MUSTER_VERGLEICH_MAXIMUM, nameof(R.VF_MUSTER_VERGLEICH_MAXIMUM), x => x.Max()),
            };
            foreach (var a in arten)
                foreach (Kennzahl k in kennzahlen)
                {
                    string s = k.Schluessel;
                    Func<List<double>, double> wert = a.Wert;
                    yield return new Vorlagenfeld(a.Vorsilbe + s, Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Gruppe,
                        w => UeberStaende(w, s, wert))
                    {
                        Seit = FASSUNG_STAND,
                        Format = k.Format,
                        Einheit = k.Einheit,
                        Bedarf = s == KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN ? Vorlagenbedarf.Zeitreihen : Vorlagenbedarf.Keiner,
                        Ableitung = new Vorlagenfeldableitung(a.Muster, a.MusterId, s),
                    };
                }
        }

        /// <summary>
        /// Je Zeile der Wirtschaftlichkeit und Szenario <c>stamm.wirtschaft.*</c>, <c>stand.wirtschaft.*</c> und
        /// <c>wirtschaft.beste.*</c>; je Zahlzeile der Grund <c>stand.wirtschaft.&lt;zeile&gt;.grund</c> (Erwartungsfall).
        /// </summary>
        private static IEnumerable<Vorlagenfeld> WirtschaftszeilenFelder()
        {
            var bereiche = new (string Vorsilbe, Vorlagenfeldkontext Kontext, string Muster, string MusterId,
                                Func<Berichtswerte, string, WirtschaftsZiel> Ziel)[]
            {
                ("stamm.wirtschaft.", Vorlagenfeldkontext.Stamm, MUSTER_STAMM_WIRTSCHAFT, nameof(R.VF_MUSTER_STAMM_WIRTSCHAFT), ZielStamm),
                ("stand.wirtschaft.", Vorlagenfeldkontext.Stand, MUSTER_STAND_WIRTSCHAFT, nameof(R.VF_MUSTER_STAND_WIRTSCHAFT), ZielStand),
                ("wirtschaft.beste.", Vorlagenfeldkontext.Gruppe, MUSTER_BESTE_WIRTSCHAFT, nameof(R.VF_MUSTER_BESTE_WIRTSCHAFT), ZielBeste),
            };
            foreach (var b in bereiche)
                foreach (Wirtschaftszeile z in Wirtschaftszeilen)
                    foreach (var sz in Szenarien)
                    {
                        Wirtschaftszeile zeile = z;
                        var bereich = b;
                        string szenario = sz.Szenario, name = sz.Name;
                        yield return new Vorlagenfeld(b.Vorsilbe + z.Schluessel + sz.Anhang,
                            z.Text ? Vorlagenfeldart.Text : Vorlagenfeldart.Zahl, b.Kontext,
                            w => Wirtschaftswert(bereich.Ziel(w, szenario), zeile.Zeile))
                        {
                            Seit = FASSUNG_STAND,
                            Format = z.Text ? null : z.Format,
                            Einheit = z.Einheit,
                            Leerwert = z.Text ? "" : null,
                            Ableitung = new Vorlagenfeldableitung(b.Muster, b.MusterId, z.Schluessel)
                            {
                                Bezeichnung = zeile.Bezeichnung,
                                Zusatz = k => Ressource(name, k),
                            },
                        };
                    }
            foreach (Wirtschaftszeile z in Wirtschaftszeilen.Where(z => !z.Text))
            {
                Wirtschaftszeile zeile = z;
                yield return new Vorlagenfeld("stand.wirtschaft." + z.Schluessel + ".grund", Vorlagenfeldart.Text,
                    Vorlagenfeldkontext.Stand, w => Zellgrund(ZielStand(w, WirtschaftlichkeitSzenario.ERWARTET), zeile.Zeile))
                {
                    Seit = FASSUNG_STAND,
                    Leerwert = "",
                    Ableitung = new Vorlagenfeldableitung(MUSTER_STAND_WIRTSCHAFT_GRUND, nameof(R.VF_MUSTER_STAND_WIRTSCHAFT_GRUND), z.Schluessel)
                    {
                        Bezeichnung = zeile.Bezeichnung,
                    },
                };
            }
        }

        /// <summary>Die Werte der Gruppe: Warnungen, Hinweise, Methodik, beste Variante, Parameter, Szenarien.</summary>
        private static IEnumerable<Vorlagenfeld> Gruppenwerte()
        {
            const Vorlagenfeldkontext G = Vorlagenfeldkontext.Gruppe;
            var l = new List<Vorlagenfeld>
            {
                Neu("wirtschaft.warnungen", Vorlagenfeldart.Liste, G, w => Wirtschaftswarnungen(w, null)),
                Neu("wirtschaft.hinweise", Vorlagenfeldart.Liste, G, w => MitErgebnissen(w, werte =>
                    WirtschaftlichkeitBaustein.Rechnungszeilen(w.Daten, werte.Ergebnisse, null, false, true)
                        .Select(t => BerichtTexte.T(t, w.Englisch)).ToList())),
                Neu("wirtschaft.referenzname", Vorlagenfeldart.Text, G, w => MitErgebnissen(w, werte =>
                    werte.Bewertung?.Bandbreite?.Referenzname)),
                new Vorlagenfeld("wirtschaft.rechenstand", Vorlagenfeldart.Datum, G, w => MitErgebnissen(w, werte =>
                        (object)werte.Ergebnisse[0].Zeitstempel))
                    { Seit = FASSUNG_STAND, Format = FORMAT_DATUM_ZEIT },
                Neu("wirtschaft.methodik", Vorlagenfeldart.Text, G,
                    w => BerichtTexte.T(WirtschaftlichkeitBaustein.METHODIK, w.Englisch)),
                Neu("wirtschaft.parameternachweis", Vorlagenfeldart.Text, G, w => MitErgebnissen(w, werte =>
                    BerichtTexte.T(WirtschaftlichkeitBaustein.Parameterzeile(werte, w.Kultur), w.Englisch))),
                new Vorlagenfeld("wirtschaft.vorschlag", Vorlagenfeldart.Text, G, w => MitErgebnissen(w, werte =>
                        werte.Bewertung?.Vorschlagstext))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                new Vorlagenfeld("wirtschaft.szenarioabdeckung", Vorlagenfeldart.Text, G, w => MitErgebnissen(w, werte =>
                        werte.Bewertung?.Szenarioabdeckung))
                    { Seit = FASSUNG_STAND, Leerwert = "" },
                Neu("wirtschaft.valeri_hinweise", Vorlagenfeldart.Liste, G, w => MitErgebnissen(w, werte =>
                    WirtschaftlichkeitBaustein.Valerizeilen(werte, werte.Bewertung))),
                Neu("wirtschaft.deklarationen", Vorlagenfeldart.Liste, G, w => MitErgebnissen(w, werte =>
                    WirtschaftlichkeitBaustein.Deklarationszeilen(werte.Bewertung))),
                new Vorlagenfeld("wirtschaft.beste.anzeige", Vorlagenfeldart.Text, G, BesteAnzeige)
                    { Seit = FASSUNG_STAND, Aliasse = new[] { ALIAS_BESTE_VARIANTE } },
                Neu("wirtschaft.beste.ist_stamm", Vorlagenfeldart.Schalter, G,
                    w => w.Wirtschaft.Ergebnisse.Count > 0 && w.Beste.Grund == BesteVariante.Auswahlgrund.StammOhneVarianten),
                new Vorlagenfeld("wirtschaft.beste.kapitalwert", Vorlagenfeldart.Zahl, G, BesteKapitalwert)
                    { Seit = FASSUNG_STAND, Format = FORMAT_EURO, Einheit = "€" },
            };

            // Die Parameter je Szenario — dieselben Größen wie der Parameterblock der Formelmappe
            // (ExcelFormelmappe.Parameterblock), Sätze in Prozent.
            var parameter = new (string Name, string Titel, string Format, string Einheit)[]
            {
                ("zins", nameof(R.WIRT_FM_PARAM_ZINS), "N2", "%"),
                ("zins_basis", nameof(R.WIRT_FM_PARAM_ZINS), "N2", "%"),
                ("zeitraum", nameof(R.WIRT_FM_PARAM_ZEITRAUM), "N0", "a"),
                ("p_e", nameof(R.WIRT_FM_PARAM_PREIS_E), "N2", "%"),
                ("p_b", nameof(R.WIRT_FM_PARAM_PREIS_B), "N2", "%"),
                ("p_i", nameof(R.WIRT_FM_PARAM_PREIS_I), "N2", "%"),
                ("risiko_zuschlag", nameof(R.WIRT_FM_PARAM_RISIKO_ZUSCHLAG), "N2", "%"),
                ("risiko_verlust", nameof(R.WIRT_FM_PARAM_RISIKO_VERLUST), FORMAT_EURO, "€"),
                ("risiko_p", nameof(R.WIRT_FM_PARAM_RISIKO_P), "N2", "%"),
                ("risiko_abzug", nameof(R.WIRT_FM_PARAM_RISIKO_ABZUG), "N2", "€"),
            };
            foreach (var p in parameter)
                foreach (var sz in Szenarien)
                {
                    string name = p.Name, titel = p.Titel, szenario = sz.Szenario, szName = sz.Name;
                    l.Add(new Vorlagenfeld("wirtschaft.parameter." + p.Name + sz.Anhang, Vorlagenfeldart.Zahl, G,
                        w => Parameterwert(w, name, szenario))
                    {
                        Seit = FASSUNG_STAND,
                        Format = p.Format,
                        Einheit = p.Einheit,
                        Ableitung = new Vorlagenfeldableitung(MUSTER_WIRTSCHAFT_PARAMETER, nameof(R.VF_MUSTER_WIRTSCHAFT_PARAMETER), p.Name)
                        {
                            Bezeichnung = k => Ressource(titel, k),
                            Zusatz = k => Ressource(szName, k),
                        },
                    });
                }

            // Die Szenarien: Name, Annahmen, gepflegte Trägerpreise.
            foreach (var sz in Szenarien)
            {
                string szenario = sz.Szenario, szName = sz.Name;
                Func<CultureInfo, string> bezeichnung = k => Ressource(szName, k);
                string vorsilbe = "wirtschaft.szenario." + sz.Schluessel + ".";
                l.Add(new Vorlagenfeld(vorsilbe + "name", Vorlagenfeldart.Text, G, w => w.Text(szName))
                {
                    Seit = FASSUNG_STAND,
                    Ableitung = new Vorlagenfeldableitung(MUSTER_SZENARIO, nameof(R.VF_MUSTER_SZENARIO_NAME), sz.Schluessel)
                    { Bezeichnung = bezeichnung },
                });
                l.Add(new Vorlagenfeld(vorsilbe + "annahmen", Vorlagenfeldart.Text, G, w => Annahmen(w, szenario))
                {
                    Seit = FASSUNG_STAND,
                    Leerwert = "",
                    Ableitung = new Vorlagenfeldableitung(MUSTER_SZENARIO, nameof(R.VF_MUSTER_SZENARIO_ANNAHMEN), sz.Schluessel)
                    { Bezeichnung = bezeichnung },
                });
                l.Add(new Vorlagenfeld(vorsilbe + "traegerpreise", Vorlagenfeldart.Text, G, w => Traegerpreise(w, szenario))
                {
                    Seit = FASSUNG_STAND,
                    Leerwert = "",
                    Ableitung = new Vorlagenfeldableitung(MUSTER_SZENARIO, nameof(R.VF_MUSTER_SZENARIO_TRAEGERPREISE), sz.Schluessel)
                    { Bezeichnung = bezeichnung },
                });
            }
            return l;
        }

        /// <summary>Die Werte des laufenden Gebäudes (<see cref="Berichtswerte.LaufendesGebaeude"/>) — Eingaben wie
        /// in der Projektbeschreibung, Ergebnisse wie im Abschnitt „Gebäude (Simulationsergebnis)“.</summary>
        private static IEnumerable<Vorlagenfeld> Gebaeudewerte()
        {
            const Vorlagenfeldkontext GB = Vorlagenfeldkontext.Gebaeude;
            Vorlagenfeld Zahl(string schluessel, Func<Berichtswerte, object> quelle, string format, string einheit) =>
                new Vorlagenfeld(schluessel, Vorlagenfeldart.Zahl, GB, quelle) { Seit = FASSUNG_STAND, Format = format, Einheit = einheit };

            return new[]
            {
                Neu("gebaeude.art", Vorlagenfeldart.Text, GB, w => MitGebaeude(w, g =>
                    Oder(ProjektDetails.S(g, "Gebaeudeart"), ProjektDetails.S(g, "Typ")))),
                // Der Klartext der Klasse (Bauzeitraum) wie im Kapitel „Projekt“, nicht ihr gespeicherter Kennbuchstabe (E47).
                Neu("gebaeude.baualtersklasse", Vorlagenfeldart.Text, GB, w => MitGebaeude(w, g =>
                    Gebaeudeklassen.Text(ProjektDetails.S(g, "Baualtersklasse"), w.Kultur))),
                Zahl("gebaeude.flaeche", w => MitGebaeude(w, g => ProjektDetails.D(g, "Wohnflaeche_gesamt")), "N0", "m²"),
                Zahl("gebaeude.nutzer", w => MitGebaeude(w, g => ProjektDetails.D(g, "Bewohner")), "N0", null),
                Zahl("gebaeude.waermebedarf", w => MitGebaeude(w, g => ProjektDetails.D(g, "Waermebedarf")), "N0", "kWh/a"),
                Zahl("gebaeude.spez_waermeverbrauch", w => MitGebaeude(w, g => ProjektDetails.D(g, "spez_Waermeverbrauch")), "N1", "kWh/m²a"),
                Zahl("gebaeude.ww_bedarf", w => MitGebaeude(w, g => ProjektDetails.D(g, "WW_Bedarf")), "N0", "kWh/a"),
                Zahl("gebaeude.raumhoehe", w => MitGebaeude(w, g => ProjektDetails.D(g, "Raumhoehe")), "N2", "m"),
                Neu("gebaeude.ergebnis.rechenweg", Vorlagenfeldart.Text, GB, w => MitGebaeudeergebnis(w, false, e =>
                    ProjektbeschreibungBaustein.Rechenwegtext(e))),
                Zahl("gebaeude.ergebnis.heizwaerme", w => MitGebaeudeergebnis(w, false, e => e.HeizwaermeMwh), "N1", "MWh/a"),
                Zahl("gebaeude.ergebnis.spitze", w => MitGebaeudeergebnis(w, false, e => e.SpitzeKw), "N1", "kW"),
                Zahl("gebaeude.ergebnis.spitze_tagesmittel", w => MitGebaeudeergebnis(w, false, e => e.SpitzeTagesmittelKw), "N1", "kW"),
                Zahl("gebaeude.ergebnis.spitze_95", w => MitGebaeudeergebnis(w, false, e => e.Spitze95Kw), "N1", "kW"),
                Zahl("gebaeude.ergebnis.kuehlenergie", w => MitGebaeudeergebnis(w, true, e => e.KuehlenergieMwh), "N1", "MWh/a"),
                Zahl("gebaeude.ergebnis.kuehlstunden", w => MitGebaeudeergebnis(w, true, e => e.KuehlstundenH), "N0", "h/a"),
                Zahl("gebaeude.ergebnis.raumtemperatur", w => MitGebaeudeergebnis(w, true, e => e.MittlereRaumtemperaturC), "N1", "°C"),
                Zahl("gebaeude.ergebnis.ueberhitzungsstunden", w => MitGebaeudeergebnis(w, true, e => e.UeberhitzungsstundenH), "N0", "h/a"),
            };
        }

        /// <summary>
        /// Die Datenschalter <c>hat.*</c> (Konzept 4.5, 4.7): im Block <c>je stand</c> für den laufenden Stand, außerhalb
        /// für die Gruppe — wahr, sobald irgendein Stand die Bedingung erfüllt. Kontext Gruppe: gültig überall.
        /// </summary>
        private static IEnumerable<Vorlagenfeld> Datenschalter()
        {
            const Vorlagenfeldkontext G = Vorlagenfeldkontext.Gruppe;
            return new[]
            {
                Neu("hat.ergebnis", Vorlagenfeldart.Schalter, G, w => Hat(w, v => v.Ergebnis != null)),
                new Vorlagenfeld("hat.zeitreihen", Vorlagenfeldart.Schalter, G, w => Hat(w, v => v.Zeitreihen != null))
                    { Seit = FASSUNG_STAND, Bedarf = Vorlagenbedarf.Zeitreihen },
                Neu("hat.kaelte", Vorlagenfeldart.Schalter, G, w => Hat(w, v => RechnetKaelte(w, v))),
                Neu("hat.wirtschaft", Vorlagenfeldart.Schalter, G, w => Hat(w, v => w.Wirtschaft.Erwartet(v.IdProjekt) != null)),
                Neu("hat.fehler", Vorlagenfeldart.Schalter, G, w => Hat(w, v => !string.IsNullOrEmpty(v.Fehler))),
                Neu("hat.veraltet", Vorlagenfeldart.Schalter, G, w => Hat(w, v => v.ErgebnisVeraltet)),
                Neu("hat.gebaeude", Vorlagenfeldart.Schalter, G, w => Hat(w, v => v.Details?.Gebaeude != null && v.Details.Gebaeude.Rows.Count > 0)),
                Neu("hat.emissionsbilanz", Vorlagenfeldart.Schalter, G, w => Hat(w, v => HatEmissionsbilanz(w, v))),
                Neu("hat.sensitivitaet", Vorlagenfeldart.Schalter, G, w => Hat(w, v => HatSensitivitaet(w, v))),
                Neu("hat.emissionsmodus_gwp", Vorlagenfeldart.Schalter, G, w => Hat(w, v => EmissionsAusweis.IstAequivalent(v.EmissionsModus))),
            };
        }

        /// <summary>
        /// Je Eintrag des Kontexts Stand der Zwilling der Paarsicht (Konzept 4.7): <c>stand.a.&lt;rest&gt;</c> löst den
        /// Eintrag auf <see cref="Berichtswerte.StandA"/> auf, <c>stand.b.&lt;rest&gt;</c> auf
        /// <see cref="Berichtswerte.StandB"/> — Kontext Gruppe, gültig auch außerhalb der Blöcke; ohne Paar leer mit
        /// Grund. Art, Format, Einheit, Leerwert und Bedarf wie das Vorbild.
        /// </summary>
        private static IEnumerable<Vorlagenfeld> Paarsicht(IEnumerable<Vorlagenfeld> alle)
        {
            List<Vorlagenfeld> vorbilder = alle.Where(f => f.Kontext == Vorlagenfeldkontext.Stand &&
                                                           f.Schluessel.StartsWith(STAND, StringComparison.Ordinal)).ToList();
            foreach (bool a in new[] { true, false })
                foreach (Vorlagenfeld f in vorbilder)
                {
                    Vorlagenfeld vorbild = f;
                    bool standA = a;
                    yield return new Vorlagenfeld(STAND + (a ? "a." : "b.") + f.Schluessel.Substring(STAND.Length), f.Art,
                        Vorlagenfeldkontext.Gruppe, w => ImPaar(w, vorbild, standA))
                    {
                        Seit = FASSUNG_STAND,
                        Format = f.Format,
                        Einheit = f.Einheit,
                        Leerwert = f.Leerwert,
                        Ausgaben = f.Ausgaben,
                        Bedarf = f.Bedarf,
                        Ableitung = new Vorlagenfeldableitung(a ? MUSTER_STAND_A : MUSTER_STAND_B,
                            a ? nameof(R.VF_MUSTER_STAND_A) : nameof(R.VF_MUSTER_STAND_B), f.Schluessel)
                        {
                            Bezeichnung = k => Beschreibung(vorbild, k.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase)),
                        },
                    };
                }
        }

        // =====================================================================
        //  Quellen der Fassung 3 — sie lesen nur den Wertesatz
        // =====================================================================

        /// <summary>Wertet am laufenden Stand aus; ohne ihn der Grund „kein laufender Stand“.</summary>
        private static object MitStand(Berichtswerte w, Func<VariantenDaten, object> wert)
        {
            VariantenDaten v = w.LaufenderStand;
            return v == null ? Grund(w, nameof(R.BV_GRUND_KEIN_STAND)) : wert(v);
        }

        private static object ImPaar(Berichtswerte w, Vorlagenfeld vorbild, bool standA)
        {
            VariantenDaten v = standA ? w.StandA : w.StandB;
            if (v == null) return Grund(w, nameof(R.BV_GRUND_KEIN_PAAR));
            return vorbild.Quelle(w.MitStand(v));
        }

        private static object StandProjekt(Berichtswerte w, Func<ProjektModel, object> feld)
        {
            return MitStand(w, v => v.Projekt == null ? Grund(w, nameof(R.BV_GRUND_KEINE_PROJEKTDATEN)) : feld(v.Projekt));
        }

        /// <summary>Der Hinweis der Tafel „Simulationsstände“ des Anhangs (Fehler, neu gerechnet, veraltet).</summary>
        private static object Standhinweis(Berichtswerte w, VariantenDaten v)
        {
            if (v.Fehler != null) return BerichtTexte.T("Fehler: ", w.Englisch) + v.Fehler;
            if (v.FrischSimuliert) return BerichtTexte.T("für diesen Bericht neu gerechnet", w.Englisch);
            if (v.ErgebnisVeraltet) return BerichtTexte.T("älter als letzte Projektänderung", w.Englisch);
            return null;
        }

        /// <summary>Der Stromspeicher des Stands: die Zeile „Stromspeicher“ der Kennzahltafel (im Sammler erhoben).</summary>
        private static object Stromspeicher(Berichtswerte w)
        {
            return MitStand(w, v =>
            {
                WirtschaftsBerichtswerte werte = w.Wirtschaft;
                WirtZeile z = Zeile(werte, "SPEICHER_KONTEXT");
                if (z == null) return null;
                WirtschaftlichkeitErgebnis e = werte.Erwartet(v.IdProjekt);
                return e == null ? null : z.Text(e);
            });
        }

        /// <summary>Eine Kennzahl des Stands aus <see cref="VariantenDaten.Kennzahlen"/> — dieselbe Quelle wie die Kapitel.</summary>
        private static object Kennzahlwert(Berichtswerte w, VariantenDaten v, string schluessel)
        {
            if (v.Kennzahlen != null && v.Kennzahlen.TryGetValue(schluessel, out double? wert) && wert.HasValue)
                return wert.Value;
            if (!string.IsNullOrEmpty(v.Fehler)) return Grund(w, nameof(R.BV_GRUND_LAUF_FEHLGESCHLAGEN));
            if (v.Ergebnis == null) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
            return Grund(w, nameof(R.BV_GRUND_NICHT_VERFUEGBAR));
        }

        /// <summary>
        /// Die Abweichung einer Kennzahl gegenüber dem Stamm — wie die Δ-Spalte des Vergleichs (Stand − Stamm) bzw. die
        /// Δ-%-Tafel ((Stand − Stamm) / |Stamm| · 100, ohne Stammwert nahe 0).
        /// </summary>
        private static object Abweichung(Berichtswerte w, VariantenDaten v, string schluessel, bool prozent)
        {
            if (v.IstStamm) return Grund(w, nameof(R.BV_GRUND_IST_STAMM));
            if (w.Stamm == null) return Grund(w, nameof(R.BV_GRUND_KEIN_STAMM));
            object stand = Kennzahlwert(w, v, schluessel);
            if (!(stand is double s)) return stand;
            object stamm = Kennzahlwert(w, w.Stamm, schluessel);
            if (!(stamm is double b)) return stamm;
            if (!prozent) return s - b;
            if (Math.Abs(b) <= 1e-9) return Grund(w, nameof(R.BV_GRUND_KEIN_DELTA));
            return (s - b) / Math.Abs(b) * 100.0;
        }

        /// <summary>Die endlichen Werte einer Kennzahl über alle Stände mit Wert.</summary>
        private static List<double> Kennzahlreihe(Berichtswerte w, string schluessel)
        {
            var werte = new List<double>();
            foreach (VariantenDaten v in w.Staende)
                if (v.Kennzahlen != null && v.Kennzahlen.TryGetValue(schluessel, out double? x) && x.HasValue &&
                    !double.IsNaN(x.Value) && !double.IsInfinity(x.Value))
                    werte.Add(x.Value);
            return werte;
        }

        /// <summary>
        /// Ein Wert einer Kennzahl über alle Stände mit Wert — Spanne (größter − kleinster), Minimum oder Maximum; mit
        /// weniger als zwei Werten der Grund „zu wenige Stände“.
        /// </summary>
        private static object UeberStaende(Berichtswerte w, string schluessel, Func<List<double>, double> wert)
        {
            List<double> werte = Kennzahlreihe(w, schluessel);
            if (werte.Count < 2) return Grund(w, nameof(R.BV_GRUND_ZU_WENIG_STAENDE));
            return wert(werte);
        }

        /// <summary>Wertet nur mit Ergebnissen der Wirtschaftlichkeit aus; sonst der Grund „keine Wirtschaftlichkeit berechnet“.</summary>
        private static object MitErgebnissen(Berichtswerte w, Func<WirtschaftsBerichtswerte, object> wert)
        {
            WirtschaftsBerichtswerte werte = w.Wirtschaft;
            return werte.Ergebnisse.Count == 0 ? Grund(w, nameof(R.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT)) : wert(werte);
        }

        /// <summary>Ein Wert der Szenarientafel für den Stand (<see cref="WirtschaftlichkeitBandbreite"/> der Bewertung);
        /// die Referenz führt keine Zeile.</summary>
        private static object Bandbreitenwert(Berichtswerte w, VariantenDaten v, Func<BandbreitenZeile, object> wert)
        {
            return MitErgebnissen(w, werte =>
            {
                WirtschaftlichkeitBandbreite band = werte.Bewertung?.Bandbreite;
                if (band == null || band.Leer) return Grund(w, nameof(R.BV_GRUND_NUR_STAMM));
                if (v.IdProjekt == band.IdReferenz || (band.IdReferenz <= 0 && v.IstStamm)) return Grund(w, nameof(R.BV_GRUND_REFERENZ));
                BandbreitenZeile z = band.Zeile(v.IdProjekt);
                return z == null ? Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS)) : wert(z);
            });
        }

        /// <summary>Wohin ein Wirtschaftlichkeitswert zielt: der Stand (Kennung, Fehler) und das Szenario — oder der
        /// Grund, warum es keinen Stand gibt.</summary>
        private sealed class WirtschaftsZiel
        {
            internal Berichtswerte Werte;
            internal int IdProjekt;
            internal string Fehler;
            internal string Szenario;
            internal bool Beste;
            internal Leergrund Ohne;
        }

        private static WirtschaftsZiel ZielStand(Berichtswerte w, string szenario)
        {
            VariantenDaten v = w.LaufenderStand;
            return v == null
                ? new WirtschaftsZiel { Werte = w, Ohne = Grund(w, nameof(R.BV_GRUND_KEIN_STAND)) }
                : new WirtschaftsZiel { Werte = w, IdProjekt = v.IdProjekt, Fehler = v.Fehler, Szenario = szenario };
        }

        private static WirtschaftsZiel ZielStamm(Berichtswerte w, string szenario)
        {
            VariantenDaten v = w.Stamm;
            return v == null
                ? new WirtschaftsZiel { Werte = w, Ohne = Grund(w, nameof(R.BV_GRUND_KEIN_STAMM)) }
                : new WirtschaftsZiel { Werte = w, IdProjekt = v.IdProjekt, Fehler = v.Fehler, Szenario = szenario };
        }

        /// <summary>Die beste Variante (<see cref="Berichtswerte.Beste"/>); die Szenarien Günstig und Ungünstig zeigen
        /// denselben Stand — gewählt wird im Erwartungsfall.</summary>
        private static WirtschaftsZiel ZielBeste(Berichtswerte w, string szenario)
        {
            if (w.Wirtschaft.Ergebnisse.Count == 0)
                return new WirtschaftsZiel { Werte = w, Ohne = Grund(w, nameof(R.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT)) };
            BesteVariante.Auswahl beste = w.Beste;
            if (beste.Grund == BesteVariante.Auswahlgrund.KeinErgebnis)
                return new WirtschaftsZiel { Werte = w, Ohne = Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS)) };
            return new WirtschaftsZiel
            {
                Werte = w,
                IdProjekt = beste.IdProjekt,
                Fehler = w.FindeStand(beste.IdProjekt)?.Fehler,
                Szenario = szenario,
                Beste = true,
            };
        }

        /// <summary>Die Zeile der Kennzahltafel gegen die Referenz der Tafel; <c>null</c> = im Lauf nicht geführt.</summary>
        private static WirtZeile Zeile(WirtschaftsBerichtswerte werte, string zeile)
        {
            return werte.Zeilen(werte.IdReferenzTafel).FirstOrDefault(z => string.Equals(z.Schluessel, zeile, StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Der Wert einer Zelle der Wirtschaftlichkeit</b> (Konzept 4.10, 5.2): die Zahl, die Excel schreibt
        /// (<see cref="WirtZeile.ExcelWert"/>), bzw. der Text einer Textzeile — ohne Wert der Grund der Zelle
        /// (<see cref="WirtZeile.Grund"/>), bei der Referenz einer Differenzkennzahl „Referenz der Gruppe“ (bei der
        /// besten Variante im Stammfall „nur Stammprojekt gerechnet“). Nie 0 statt eines fehlenden Werts.
        /// </summary>
        private static object Wirtschaftswert(WirtschaftsZiel ziel, string zeile)
        {
            Berichtswerte w = ziel.Werte;
            if (ziel.Ohne != null) return ziel.Ohne;
            WirtschaftsBerichtswerte werte = w.Wirtschaft;
            if (werte.Ergebnisse.Count == 0) return Grund(w, nameof(R.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT));
            WirtschaftlichkeitErgebnis e = werte.Ergebnisse.FirstOrDefault(x =>
                x.IdProjekt == ziel.IdProjekt && string.Equals(x.Szenario, ziel.Szenario, StringComparison.Ordinal));
            if (e == null)
                return Grund(w, string.IsNullOrEmpty(ziel.Fehler) ? nameof(R.BV_GRUND_KEIN_ERGEBNIS) : nameof(R.BV_GRUND_LAUF_FEHLGESCHLAGEN));
            WirtZeile z = Zeile(werte, zeile);
            if (z == null || (!z.IstText && z.Wert == null)) return Grund(w, nameof(R.BV_GRUND_ZEILE_FEHLT));
            if (z.IstText)
            {
                string t = z.Text(e);
                return string.IsNullOrWhiteSpace(t) ? null : t;
            }
            if (z.IstReferenz(e) && z.StammAnzeige != null)
                return Grund(w, ziel.Beste ? nameof(R.BV_GRUND_NUR_STAMM) : nameof(R.BV_GRUND_REFERENZ));
            string grund = z.Grund(e);
            if (grund != null) return grund.Length == 0 ? null : new Leergrund(grund);
            return z.Wert(e);
        }

        /// <summary>Der Grund einer Zelle ohne Wert als Text; leer, wenn die Zelle einen Wert trägt.</summary>
        private static object Zellgrund(WirtschaftsZiel ziel, string zeile)
        {
            if (ziel.Ohne != null) return ziel.Ohne;
            object wert = Wirtschaftswert(ziel, zeile);
            return wert is Leergrund leer ? leer.Grund : null;
        }

        /// <summary>
        /// Die Gültigkeitshinweise der Wirtschaftlichkeit (Konzept 4.11) — dieselben Sätze wie im Kapitel: keine
        /// Rechnung, Rückfall auf den gespeicherten Stand, veraltete Ergebnisse, Fehlgründe, Warnungen der Zellen der
        /// Kennzahltafel. <paramref name="nur"/> schränkt auf einen Stand ein (<c>stand.wirtschaft.warnungen</c>).
        /// </summary>
        private static object Wirtschaftswarnungen(Berichtswerte w, VariantenDaten nur)
        {
            WirtschaftsBerichtswerte werte = w.Wirtschaft;
            var zeilen = new List<string>();
            List<WirtschaftlichkeitErgebnis> alle = werte.Ergebnisse;
            if (alle.Count == 0)
            {
                zeilen.Add(BerichtTexte.T(WirtschaftlichkeitBaustein.TextOhneErgebnis(w.Daten), w.Englisch));
                return zeilen;
            }
            if (!werte.AusDiesemLauf)
                zeilen.Add(BerichtTexte.T(WirtschaftlichkeitBaustein.TextRueckfall(w.Daten), w.Englisch));
            string veraltet = WirtschaftlichkeitBaustein.TextVeraltet(w.Daten, werte, nur);
            if (veraltet != null) zeilen.Add(veraltet);
            zeilen.AddRange(WirtschaftlichkeitBaustein.Rechnungszeilen(w.Daten, alle, nur, true, false)
                                                    .Select(t => BerichtTexte.T(t, w.Englisch)));

            List<WirtZeile> tafel = WirtschaftlichkeitZeilen.Sichtbare(werte.Zeilen(werte.IdReferenzTafel), alle);
            foreach (int id in w.Staendefolge)
            {
                if (nur != null && id != nur.IdProjekt) continue;
                VariantenDaten v = w.FindeStand(id);
                WirtschaftlichkeitErgebnis e = werte.Erwartet(id);
                if (v == null || e == null) continue;
                foreach (WirtZeile z in tafel)
                {
                    string warnung = z.Warnung(e);
                    if (!string.IsNullOrEmpty(warnung)) zeilen.Add(WirtschaftlichkeitBaustein.Zellwarnung(v, z, warnung));
                }
            }
            return zeilen;
        }

        /// <summary>Der Name der besten Variante — beim Stamm der Name des Stammprojekts (Konzept 4.5).</summary>
        private static object BesteAnzeige(Berichtswerte w)
        {
            return MitErgebnissen(w, werte =>
            {
                BesteVariante.Auswahl beste = w.Beste;
                if (beste.Grund == BesteVariante.Auswahlgrund.KeinErgebnis) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
                VariantenDaten v = w.FindeStand(beste.IdProjekt);
                if (v == null) return beste.Ergebnis?.Anzeige;
                if (v.IstStamm)
                {
                    string name = v.Projekt?.m_szProjektname;
                    return string.IsNullOrWhiteSpace(name) ? w.Daten.Stammprojektname : name;
                }
                return v.Anzeige;
            });
        }

        /// <summary>
        /// Der Wert der Karte „Kapitalwert ggü. Stamm“ (Konzept 9.5): die Kapitalwertdifferenz der besten Variante,
        /// im Stammfall der Nettobarwert des Stamms — genau die Zahl der Karte in beiden Fällen.
        /// </summary>
        private static object BesteKapitalwert(Berichtswerte w)
        {
            return MitErgebnissen(w, werte =>
            {
                BesteVariante.Auswahl beste = w.Beste;
                WirtschaftlichkeitErgebnis e = beste.Ergebnis;
                if (beste.Grund == BesteVariante.Auswahlgrund.KeinErgebnis || e == null) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
                double? wert = beste.Grund == BesteVariante.Auswahlgrund.BestesKriterium ? e.KapitalwertDiff : e.Kapitalwert;
                return wert.HasValue ? (object)wert.Value : Grund(w, nameof(R.BV_GRUND_NICHT_VERFUEGBAR));
            });
        }

        /// <summary>
        /// Ein Parameter der Wirtschaftlichkeit je Szenario — dieselben Ausdrücke wie der Parameterblock der Formelmappe
        /// (<see cref="ExcelFormelmappe"/>), die Sätze in Prozent statt als Dezimalzahl.
        /// </summary>
        private static object Parameterwert(Berichtswerte w, string name, string szenario)
        {
            return MitErgebnissen(w, werte =>
            {
                WirtschaftlichkeitParameter p = werte.Parameter;
                if (p == null) return Grund(w, nameof(R.BV_GRUND_NICHT_VERFUEGBAR));
                bool erwartet = szenario == WirtschaftlichkeitSzenario.ERWARTET;
                SzenarioSatz satz = p.SatzFuer(szenario) ?? SzenarioSatz.Vorgabe(szenario);
                bool zinsRisiko = RisikoModul.ZinsAktiv(p), abzug = RisikoModul.AbzugAktiv(p);
                switch (name)
                {
                    case "zins":
                        if (zinsRisiko) return p.FuerSzenario(szenario).Zinssatz;
                        return erwartet ? p.Zinssatz : satz.ZinsWirksam(p.Zinssatz);
                    case "zins_basis":
                        if (!zinsRisiko) return Grund(w, nameof(R.BV_GRUND_KEIN_RISIKO));
                        return erwartet ? p.Zinssatz : satz.ZinsWirksam(p.Zinssatz);
                    case "zeitraum":
                        return erwartet ? (double)p.Betrachtungszeitraum : (double)satz.ZeitraumWirksam(p.Betrachtungszeitraum);
                    case "p_e":
                        return erwartet ? p.PreissteigerungEnergie : satz.PreisEnergieWirksam(p.PreissteigerungEnergie);
                    case "p_b":
                        return erwartet ? p.PreissteigerungBetrieb : satz.PreisBetriebWirksam(p.PreissteigerungBetrieb);
                    case "p_i":
                        return erwartet ? p.PreisInvestWirksam : satz.PreisInvestWirksam(p.PreisInvestWirksam);
                    case "risiko_zuschlag":
                        return zinsRisiko ? (object)RisikoModul.Zinszuschlag(p) : Grund(w, nameof(R.BV_GRUND_KEIN_RISIKO));
                    case "risiko_verlust":
                        return abzug ? (object)p.RisikoVerlust.Value : Grund(w, nameof(R.BV_GRUND_KEIN_RISIKO));
                    case "risiko_p":
                        return abzug ? (object)RisikoModul.Wahrscheinlichkeit(p) : Grund(w, nameof(R.BV_GRUND_KEIN_RISIKO));
                    case "risiko_abzug":
                        return abzug ? (object)RisikoModul.AbzugJeJahr(p) : Grund(w, nameof(R.BV_GRUND_KEIN_RISIKO));
                    default:
                        return Grund(w, nameof(R.BV_GRUND_NICHT_VERFUEGBAR));
                }
            });
        }

        /// <summary>Die Annahmen eines Szenarios: Günstig und Ungünstig die Zeile der Szenarientafel, Erwartet der
        /// Parameternachweis (er IST der Projektsatz).</summary>
        private static object Annahmen(Berichtswerte w, string szenario)
        {
            return MitErgebnissen(w, werte =>
                szenario == WirtschaftlichkeitSzenario.ERWARTET
                    ? werte.Parameternachweis(w.Kultur)
                    : WirtschaftlichkeitBaustein.Annahmenzeile(werte.Parameter, szenario, w.Kultur));
        }

        /// <summary>Die gepflegten Trägerpreise eines Szenarios; im Erwartungsfall keine eigene Zeile.</summary>
        private static object Traegerpreise(Berichtswerte w, string szenario)
        {
            if (szenario == WirtschaftlichkeitSzenario.ERWARTET) return null;
            return MitErgebnissen(w, werte => werte.Traegerpreiszeile(szenario, w.Kultur));
        }

        private static object MitGebaeude(Berichtswerte w, Func<DataRow, object> wert)
        {
            DataRow g = w.LaufendesGebaeude;
            if (g == null) return Grund(w, nameof(R.BV_GRUND_KEIN_GEBAEUDE));
            object o = wert(g);
            return o is string s && string.IsNullOrWhiteSpace(s) ? null : o;
        }

        /// <summary>Das Ergebnis des laufenden Gebäudes im Lauf des Stands (im Block der laufende, sonst der Stamm);
        /// Kühlwerte nur auf dem Weg nach VDI 6007.</summary>
        private static object MitGebaeudeergebnis(Berichtswerte w, bool nurVdi6007, Func<ErgebnisGebaeudeModel, object> wert)
        {
            return MitGebaeude(w, g =>
            {
                VariantenDaten v = w.LaufenderStand ?? w.Stamm;
                if (v == null) return Grund(w, nameof(R.BV_GRUND_KEIN_STAMM));
                int id = (int)(ProjektDetails.D(g, "ID") ?? 0);
                ErgebnisGebaeudeModel e = v.Ergebnis?.Gebaeude?.FirstOrDefault(x => x != null && x.ID_Gebaeude == id);
                if (e == null) return Grund(w, nameof(R.BV_GRUND_KEIN_ERGEBNIS));
                if (nurVdi6007 && !e.IstVdi6007) return Grund(w, nameof(R.BV_GRUND_KEIN_VDI6007));
                return wert(e);
            });
        }

        private static string Oder(string a, string b)
        {
            return string.IsNullOrWhiteSpace(a) ? b : a;
        }

        /// <summary>Ein Datenschalter: im Standblock für den laufenden Stand, sonst für irgendeinen Stand.</summary>
        private static object Hat(Berichtswerte w, Func<VariantenDaten, bool> bedingung)
        {
            if (w.LaufenderStand != null) return bedingung(w.LaufenderStand);
            return w.Staende.Any(bedingung);
        }

        private static bool RechnetKaelte(Berichtswerte w, VariantenDaten v)
        {
            if (v.KaeltestromNetzbezugMWh.HasValue) return true;
            if (v.Kennzahlen == null) return false;
            return w.Kennzahlen.Where(k => k.Gruppe == KennzahlenKatalog.GR_KAELTE)
                .Any(k => v.Kennzahlen.TryGetValue(k.Schluessel, out double? x) && x.HasValue);
        }

        private static bool HatEmissionsbilanz(Berichtswerte w, VariantenDaten v)
        {
            WirtschaftsBerichtswerte werte = w.Wirtschaft;
            if (werte.Ergebnisse.Count == 0 || werte.Parameter == null || werte.Parameter.IdKraftwerkspark <= 0) return false;
            WirtschaftlichkeitErgebnis e = werte.Erwartet(v.IdProjekt);
            return e != null && werte.ErgebnisAktuell(e);
        }

        private static bool HatSensitivitaet(Berichtswerte w, VariantenDaten v)
        {
            WirtschaftsBerichtswerte werte = w.Wirtschaft;
            if (werte.Ergebnisse.Count == 0) return false;
            List<SensitivitaetZeile> sens = werte.Bewertung?.Sensitivitaet;
            return sens != null && sens.Any(z => z != null && z.IdProjekt == v.IdProjekt);
        }
    }
}
