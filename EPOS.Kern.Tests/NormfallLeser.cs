using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die EINE Lesestelle der Normfälle (Umsetzungskonzept 1.9): liest die zwölf
    /// Validierungsmodelle der AixLib (<c>TestCase1.mo</c> … <c>TestCase12.mo</c>) aus
    /// <c>Referenzlaeufe/Normzahlen/aixlib/</c> und bildet sie auf den 2-K-Löser ab. Die
    /// Dateien liegen nur lokal (<c>Referenzlaeufe/Normzahlen/LIESMICH.md</c>); in diesem
    /// Quelltext steht keine einzige Zahl der Richtlinie — alles wird zur Laufzeit gelesen.
    ///
    /// <para><b>Das erwartete Format.</b> Je Testfall ein Modelica-Modell mit
    /// <list type="bullet">
    ///   <item>der Zone <c>RC.TwoElements</c> (Parameter <c>RExt</c>, <c>RExtRem</c>,
    ///         <c>CExt</c>, <c>RInt</c>, <c>CInt</c>, <c>AExt</c>, <c>AInt</c>, <c>AWin</c>,
    ///         <c>ATransparent</c>, <c>hConExt</c>, <c>hConInt</c>, <c>hRad</c>,
    ///         <c>gWin</c>, <c>ratioWinConRad</c>, <c>T_start</c>; je eine Kapazität);</item>
    ///   <item>dem äußeren Übergang (<c>Convection</c> an <c>extWall</c>, Leitwert aus einem
    ///         <c>Constant</c>) und einer Temperaturquelle (<c>FixedTemperature</c> oder
    ///         <c>PrescribedTemperature</c>, gespeist aus Außentemperatur oder dem Block
    ///         <c>EquivalentAirTemperature.VDI6007</c>);</item>
    ///   <item>inneren Lasten über <c>PrescribedHeatFlow</c> an <c>intGainsConv</c> bzw.
    ///         <c>intGainsRad</c>, Solar über <c>solRad</c>, Lüftung über
    ///         <c>MassFlowSource_T</c> an <c>ports</c>;</item>
    ///   <item>der Regelung — ideal über <c>PrescribedTemperature</c> hinter einem
    ///         <c>HeatFlowSensor</c> oder über <c>LimPID</c> mit Verstärkung als
    ///         Leistungsgrenze;</item>
    ///   <item>den Eingangs- und Referenztabellen als <c>CombiTimeTable</c> mit
    ///         <c>table=[…]</c> im Modell. Die Referenztabelle heißt <c>reference</c>; ihre Zeile
    ///         zur Zeit n·3600 s ist das Blockmittel der n-ten Stunde. Temperaturen stehen dort
    ///         in °C (das <c>offset</c> macht in AixLib Kelvin daraus).</item>
    /// </list></para>
    ///
    /// <para><b>Die Abbildung</b> folgt Befund E, Abschnitt 3: R_Rest = <c>RExtRem</c> plus
    /// Kehrwert des äußeren Übergangsleitwerts; Strahlungslasten innen flächenproportional
    /// auf Außen- und Innenbauteile; Fenstersolar zum Anteil <c>ratioWinConRad</c> an die
    /// Luft, der Rest über die Aufteilung der AixLib (bestrahlte Orientierung ausgenommen);
    /// Strahlungsaustausch über die kleinere der beiden Flächen. Das <b>Vorzeichen</b> der
    /// Last ergibt sich aus der Messkette zum Mittelwertbildner (Richtung des
    /// <c>HeatFlowSensor</c>, Verstärkungen, Summen) — so wird die gedrehte Reihe eines Falls
    /// erkannt, ohne ihn beim Namen zu nennen. Die <b>Referenzspalte</b> ist die, die das
    /// Modell auf den Prüfblock <c>assEqu</c> führt. Die Messkette eines Falls, die
    /// 120 s nach jedem Umschalten auf eine Ersatzspalte der Referenz wechselt, wird nicht
    /// nachgebildet: gelesen wird der Rechenzweig.</para>
    ///
    /// <para><b>Eingänge je Stunde</b> sind die Blockmittel der Modellsignale, abgetastet mit
    /// <see cref="ABTASTUNGEN_JE_STUNDE"/> Stützstellen je Stunde (Mittelpunktregel). Die
    /// Tabellen springen auf Stundengrenzen; so bleibt das Mittel exakt.</para>
    ///
    /// <para><b>Lizenz:</b> Die gelesenen Dateien stehen unter der Lizenz der AixLib
    /// (<c>AixLib/UsersGuide/License.mo</c>, Copyright RWTH Aachen University, E.ON Energy
    /// Research Center, EBC); sie werden weder versioniert noch ausgeliefert.</para>
    /// </summary>
    internal static class NormfallLeser
    {
        /// <summary>Nullpunkt der Celsius-Skala [K].</summary>
        internal const double KELVIN = 273.15;

        /// <summary>
        /// Spezifische Wärmekapazität des Mediums <c>Modelica.Media.Air.SimpleAir</c>
        /// (<c>cp_const</c> der Modelica-Standardbibliothek) [J/(kg·K)] — für den Leitwert
        /// eines Lüftungsmassenstroms.
        /// </summary>
        internal const double CP_SIMPLEAIR = 1005.45;

        /// <summary>Stützstellen je Stunde für die Blockmittel der Eingänge.</summary>
        internal const int ABTASTUNGEN_JE_STUNDE = 360;

        /// <summary>Liest und bildet den Testfall <paramref name="nummer"/> ab.</summary>
        internal static Normfall Lesen(Normzahlen n, int nummer)
        {
            string datei = n.Testfalldatei(nummer);
            if (datei == null)
                throw new NormfallLeserException("TestCase" + nummer + ".mo fehlt unter " + n.AixlibOrdner +
                                                 " (Referenzlaeufe/Normzahlen/LIESMICH.md).");
            ModelicaModell m = ModelicaModell.Lesen(File.ReadAllText(datei, Encoding.UTF8));
            return new Abbildung(m, nummer).Normfall();
        }

        /// <summary>Die Abbildung eines gelesenen Modells auf Parametersatz, Ränder und Reihen.</summary>
        private sealed class Abbildung
        {
            private readonly ModelicaModell _m;
            private readonly Signalnetz _s;
            private readonly int _nummer;
            private readonly string _zone;

            internal Abbildung(ModelicaModell m, int nummer)
            {
                _m = m;
                _s = new Signalnetz(m);
                _nummer = nummer;
                _zone = m.Komponenten.Values.SingleOrDefault(k => k.Typ.EndsWith("TwoElements", StringComparison.Ordinal))?.Name
                        ?? throw Fehler("keine Zone RC.TwoElements");
            }

            private NormfallLeserException Fehler(string text) => new NormfallLeserException("TestCase" + _nummer + ": " + text);

            internal Normfall Normfall()
            {
                Komponente z = _m.Komponenten[_zone];
                if (z.Zahl("nExt", 1) != 1 || z.Zahl("nInt", 1) != 1)
                    throw Fehler("nur je eine Kapazität für Außen- und Innenbauteile ist abgebildet");
                double[] aExt = z.Feld("AExt");
                double[] aWin = z.Feld("AWin");
                double[] aTrans = z.Feld("ATransparent");
                if (aWin.Any(a => a != 0.0))
                    throw Fehler("Fenster mit eigener Oberfläche (AWin > 0) sind nicht abgebildet");
                double aAW = aExt.Sum();
                double aIW = z.Zahl("AInt");
                double aTot = aAW + aIW;

                // Äußerer Übergang: Convection an extWall, Leitwert aus dem Signal an Gc.
                string konv = Komponentenname(_m.Partner(_zone + ".extWall").Single());
                double gAussen = _s.Wert(_s.Quelle(konv + ".Gc"), 0.0);
                string temperaturquelle = Komponentenname(_m.Partner(konv + ".fluid").Single());

                var p = new ErsatzparameterRC(
                    c_AW_Jk: z.Feld("CExt")[0],
                    c_IW_Jk: z.Feld("CInt")[0],
                    r_1_AW_KW: z.Feld("RExt")[0],
                    r_Rest_AW_KW: z.Zahl("RExtRem") + 1.0 / gAussen,
                    r_1_IW_KW: z.Feld("RInt")[0],
                    r_conv_AW_KW: 1.0 / (z.Zahl("hConExt") * aAW),
                    r_conv_IW_KW: 1.0 / (z.Zahl("hConInt") * aIW),
                    r_rad_KW: 1.0 / (z.Zahl("hRad") * Math.Min(aAW, aIW)),
                    r_ext_KW: double.PositiveInfinity,
                    a_AW_opak_M2: aAW,
                    a_IW_M2: aIW,
                    summeUA_opak_WK: 0.0,
                    // Äußerer Übergang als Bedingung von Gl. (28a); der Rest enthält ihn
                    // bereits, also greift stets der Regelfall.
                    r_alphaAussen_KW: 1.0 / gAussen);

                // Innere Lasten: PrescribedHeatFlow unmittelbar an der Zone.
                List<string> konvektiv = Quellen(_zone + ".intGainsConv");
                List<string> strahlend = Quellen(_zone + ".intGainsRad");
                double anteilAWInnen = aAW / aTot, anteilIWInnen = aIW / aTot;

                // Solar durch Fenster.
                int nOri = (int)z.Zahl("nOrientations", 1);
                double gWin = z.Zahl("gWin", 1.0), ratio = z.Zahl("ratioWinConRad", 0.0);
                var solar = new string[nOri];
                for (int i = 0; i < nOri; i++) solar[i] = _s.Quelle(_zone + ".solRad[" + (i + 1) + "]");

                // Lüftung über Massenstromquellen an den Fluidanschlüssen.
                var zuluft = new List<(string Strom, string Temperatur)>();
                foreach (string partner in _m.Komponenten.Keys
                                              .Where(k => _m.Komponenten[k].Typ.EndsWith("MassFlowSource_T", StringComparison.Ordinal)))
                {
                    Komponente q = _m.Komponenten[partner];
                    bool mitTemperatur = q.Wahr("use_T_in");
                    zuluft.Add((_s.Quelle(partner + ".m_flow_in"), mitTemperatur ? _s.Quelle(partner + ".T_in") : null));
                }

                Regelung regelung = RegelungLesen();
                (NormGroesse groesse, double vorzeichen) = Messkette(_s.Quelle(Mittelwertbildner() + ".u"));
                List<Normreihe> reihen = Referenz(groesse, vorzeichen);

                int stunden = reihen.Max(r => r.Tag) * 24;
                var raender = new Stundenrand[stunden];
                int nAbtast = ABTASTUNGEN_JE_STUNDE;
                for (int h = 0; h < stunden; h++)
                {
                    double eq = 0, conv = 0, radAW = 0, radIW = 0, soll = 0, gVent = 0, gVentT = 0;
                    for (int j = 0; j < nAbtast; j++)
                    {
                        double t = (h + (j + 0.5) / nAbtast) * Zonenmodell2K.STUNDE_S;
                        eq += Temperatur(temperaturquelle, t) - KELVIN;
                        foreach (string q in konvektiv) conv += Last(q, t);
                        foreach (string q in strahlend)
                        {
                            double w = Last(q, t);
                            radAW += anteilAWInnen * w;
                            radIW += anteilIWInnen * w;
                        }
                        for (int i = 0; i < nOri; i++)
                        {
                            double qSol = _s.Wert(solar[i], t) * gWin * aTrans[i];
                            conv += ratio * qSol;
                            double nenner = aTot - aExt[i];
                            radAW += (1.0 - ratio) * qSol * (aAW - aExt[i]) / nenner;
                            radIW += (1.0 - ratio) * qSol * aIW / nenner;
                        }
                        foreach ((string strom, string temp) in zuluft)
                        {
                            double m = _s.Wert(strom, t);
                            if (m <= 0.0 || temp == null) continue;
                            gVent += m * CP_SIMPLEAIR;
                            gVentT += m * CP_SIMPLEAIR * (_s.Wert(temp, t) - KELVIN);
                        }
                        if (regelung != null) soll += _s.Wert(regelung.Sollwert, t) - KELVIN;
                    }
                    eq /= nAbtast; conv /= nAbtast; radAW /= nAbtast; radIW /= nAbtast; soll /= nAbtast;
                    double gMittel = gVent / nAbtast;
                    double tZuluft = gVent > 0.0 ? gVentT / gVent : eq;

                    raender[h] = regelung == null
                        ? new Stundenrand(tZuluft, eq, double.NaN, double.NaN, radAW, radIW, conv, zusatzleitwertWK: gMittel)
                        : new Stundenrand(tZuluft, eq, soll, soll, radAW, radIW, conv,
                                          regelung.HeizMaxW, regelung.KuehlMaxW, 0.0, regelung.KuehlungInnen,
                                          zusatzleitwertWK: gMittel);
                }

                return new Normfall(_nummer, p, z.Zahl("T_start") - KELVIN, raender, reihen);
            }

            /// <summary>Temperatur einer Quelle [K] zur Zeit t.</summary>
            private double Temperatur(string quelle, double t)
            {
                Komponente k = _m.Komponenten[quelle];
                if (k.Typ.EndsWith("FixedTemperature", StringComparison.Ordinal)) return k.Zahl("T");
                if (k.Typ.EndsWith("PrescribedTemperature", StringComparison.Ordinal)) return _s.Wert(_s.Quelle(quelle + ".T"), t);
                throw Fehler("Temperaturquelle " + quelle + " (" + k.Typ + ") ist nicht abgebildet");
            }

            private double Last(string quelle, double t) => _s.Wert(_s.Quelle(quelle + ".Q_flow"), t);

            /// <summary>Alle PrescribedHeatFlow, deren Anschluss unmittelbar an <paramref name="port"/> liegt.</summary>
            private List<string> Quellen(string port)
                => _m.Partner(port)
                     .Select(Komponentenname)
                     .Where(k => _m.Komponenten[k].Typ.EndsWith("PrescribedHeatFlow", StringComparison.Ordinal))
                     .ToList();

            private string Mittelwertbildner()
                => _m.Komponenten.Values.Single(k => k.Typ.EndsWith(".Mean", StringComparison.Ordinal)).Name;

            /// <summary>Größe und Vorzeichen der gemessenen Reihe: Messwert = Vorzeichen · (Heizen − Kühlen).</summary>
            private (NormGroesse, double) Messkette(string signal)
            {
                (string komp, string port, _) = Signalnetz.Zerlegen(signal);
                if (komp == _zone && port == "TAir") return (NormGroesse.Lufttemperatur, 1.0);
                Komponente k = _m.Komponenten[komp];
                string typ = Typname(k);
                switch (typ)
                {
                    case "HeatFlowSensor":
                    {
                        bool bAnZone = _m.Partner(komp + ".port_b").Any(IstZonenanschluss);
                        bool aAnZone = _m.Partner(komp + ".port_a").Any(IstZonenanschluss);
                        if (bAnZone == aAnZone) throw Fehler("Richtung des Sensors " + komp + " unklar");
                        return (NormGroesse.Last, bAnZone ? 1.0 : -1.0);
                    }
                    case "Gain":
                    {
                        (NormGroesse g, double v) = Messkette(_s.Quelle(komp + ".u"));
                        return (g, v * Math.Sign(k.Zahl("k")));
                    }
                    case "Add":
                    {
                        (NormGroesse g1, double v1) = Messkette(_s.Quelle(komp + ".u1"));
                        (NormGroesse g2, double v2) = Messkette(_s.Quelle(komp + ".u2"));
                        double s1 = v1 * Math.Sign(k.Zahl("k1", 1.0)), s2 = v2 * Math.Sign(k.Zahl("k2", 1.0));
                        if (g1 != g2 || s1 != s2) throw Fehler("Summe " + komp + " mischt Richtungen");
                        return (g1, s1);
                    }
                    case "Switch":
                    {
                        // Der Zweig, der NICHT aus der Referenztabelle kommt, ist der Rechenzweig.
                        string u1 = _s.Quelle(komp + ".u1"), u3 = _s.Quelle(komp + ".u3");
                        bool u1Referenz = Signalnetz.Zerlegen(u1).Komponente == "reference";
                        bool u3Referenz = Signalnetz.Zerlegen(u3).Komponente == "reference";
                        if (u1Referenz == u3Referenz) throw Fehler("Umschalter " + komp + " in der Messkette ist nicht abgebildet");
                        return Messkette(u1Referenz ? u3 : u1);
                    }
                    default:
                        throw Fehler("Messkette über " + komp + " (" + k.Typ + ") ist nicht abgebildet");
                }
            }

            private bool IstZonenanschluss(string anschluss) => Signalnetz.Zerlegen(anschluss).Komponente == _zone;

            /// <summary>Die Referenzreihen: Spalte am Prüfblock, Zeilen ab n·3600 s, Vorzeichen der Richtlinie.</summary>
            private List<Normreihe> Referenz(NormGroesse groesse, double vorzeichen)
            {
                Komponente r = _m.Komponenten["reference"];
                string pruef = _m.Komponenten.Values.Single(k => k.Typ.EndsWith("VerifyDifferenceThreePeriods", StringComparison.Ordinal)).Name;
                string ausgang = new[] { pruef + ".u1", pruef + ".u2" }
                    .Select(e => _s.Quelle(e))
                    .Single(q => Signalnetz.Zerlegen(q).Komponente == "reference");
                int index = Signalnetz.Zerlegen(ausgang).Index;
                int spalte = (int)r.Feld("columns")[index - 1];
                double[][] tafel = r.Tafel("table");

                var werte = new SortedDictionary<int, double>();
                foreach (double[] zeile in tafel)
                {
                    double stunde = zeile[0] / Zonenmodell2K.STUNDE_S;
                    int k = (int)Math.Round(stunde);
                    if (Math.Abs(stunde - k) > 1e-9) throw Fehler("Referenzzeile nicht auf voller Stunde");
                    if (k < 1) continue;
                    werte[k] = vorzeichen * zeile[spalte - 1];
                }

                var reihen = new List<Normreihe>();
                foreach (IGrouping<int, KeyValuePair<int, double>> tag in werte.GroupBy(w => (w.Key - 1) / 24 + 1))
                {
                    double[] p = new double[24];
                    int gefunden = 0;
                    foreach (KeyValuePair<int, double> w in tag)
                    {
                        p[(w.Key - 1) % 24] = w.Value;
                        gefunden++;
                    }
                    if (gefunden != 24) throw Fehler("Tag " + tag.Key + " der Referenz ist unvollständig");
                    // Die AixLib führt eine Programmspalte; beide Bandgrenzen daraus.
                    reihen.Add(new Normreihe(groesse, tag.Key, p, (double[])p.Clone()));
                }
                if (reihen.Count == 0) throw Fehler("keine Referenzzeilen");
                return reihen;
            }

            /// <summary>Sollwert, Leistungsgrenzen und Übergabeort der Regelung — oder null.</summary>
            private Regelung RegelungLesen()
            {
                // Ideal: PrescribedTemperature hinter einem HeatFlowSensor an der Zone.
                foreach (Komponente sensor in _m.Komponenten.Values.Where(k => Typname(k) == "HeatFlowSensor"))
                {
                    foreach (string seite in new[] { ".port_a", ".port_b" })
                    {
                        string gegen = seite == ".port_a" ? ".port_b" : ".port_a";
                        if (!_m.Partner(sensor.Name + seite).Any(IstZonenanschluss)) continue;
                        foreach (string geraet in _m.Partner(sensor.Name + gegen).Select(Komponentenname))
                        {
                            if (Typname(_m.Komponenten[geraet]) == "PrescribedTemperature")
                            {
                                string port = Signalnetz.Zerlegen(_m.Partner(sensor.Name + seite).Single(IstZonenanschluss)).Port;
                                if (port != "intGainsConv") throw Fehler("ideale Regelung an " + port + " ist nicht abgebildet");
                                return new Regelung(_s.Quelle(geraet + ".T"), double.NaN, double.NaN, 0.0);
                            }
                        }
                    }
                }

                Komponente pid = _m.Komponenten.Values.SingleOrDefault(k => Typname(k) == "LimPID");
                if (pid == null) return null;
                double yMax = pid.Zahl("yMax", 1.0), yMin = pid.Zahl("yMin", -yMax);
                double heizMax = double.NaN, kuehlMax = double.NaN, kuehlInnen = 0.0;
                bool heizt = false, kuehlt = false;

                foreach (Komponente geraet in _m.Komponenten.Values.Where(k => Typname(k) == "PrescribedHeatFlow"))
                {
                    string zonenport = Zonenport(geraet.Name + ".port");
                    if (zonenport == null) continue;   // innere Last, schon verbucht

                    string q = _s.Quelle(geraet.Name + ".Q_flow");
                    (string gk, _, _) = Signalnetz.Zerlegen(q);
                    Komponente gain = _m.Komponenten[gk];
                    if (Typname(gain) != "Gain") throw Fehler("Stellgröße von " + geraet.Name + " ist nicht abgebildet");
                    double k = gain.Zahl("k");
                    (string vk, _, _) = Signalnetz.Zerlegen(_s.Quelle(gk + ".u"));
                    bool richtungHeizen, richtungKuehlen;
                    if (vk == pid.Name) { richtungHeizen = richtungKuehlen = true; }
                    else if (Typname(_m.Komponenten[vk]) == "Switch")
                    {
                        // Wahr-Zweig (u1) = PID, wenn der Umschalter bei positiver Stellgröße wahr ist.
                        bool u1Pid = Signalnetz.Zerlegen(_s.Quelle(vk + ".u1")).Komponente == pid.Name;
                        bool u3Pid = Signalnetz.Zerlegen(_s.Quelle(vk + ".u3")).Komponente == pid.Name;
                        if (u1Pid == u3Pid) throw Fehler("Umschalter " + vk + " ist nicht abgebildet");
                        richtungHeizen = u1Pid;
                        richtungKuehlen = u3Pid;
                    }
                    else throw Fehler("Stellweg von " + geraet.Name + " ist nicht abgebildet");

                    if (richtungHeizen)
                    {
                        if (zonenport != "intGainsConv") throw Fehler("Heizen an " + zonenport + " ist nicht abgebildet");
                        heizMax = k * yMax;
                        heizt = true;
                    }
                    if (richtungKuehlen)
                    {
                        if (zonenport == "intWallIndoorSurface") kuehlInnen = 1.0;
                        else if (zonenport != "intGainsConv") throw Fehler("Kühlen an " + zonenport + " ist nicht abgebildet");
                        kuehlMax = k * Math.Abs(yMin);
                        kuehlt = true;
                    }
                }
                if (!heizt || !kuehlt) throw Fehler("Regelung ohne Heiz- und Kühlweg ist nicht abgebildet");
                return new Regelung(_s.Quelle(pid.Name + ".u_s"), heizMax, kuehlMax, kuehlInnen);
            }

            /// <summary>Der Zonenanschluss eines Geräteanschlusses — unmittelbar oder über einen Sensor.</summary>
            private string Zonenport(string anschluss)
            {
                foreach (string partner in _m.Partner(anschluss))
                {
                    string komp = Komponentenname(partner);
                    if (komp == _zone) return null;   // unmittelbar an der Zone: innere Last
                    if (Typname(_m.Komponenten[komp]) != "HeatFlowSensor") continue;
                    string andere = partner.EndsWith(".port_a", StringComparison.Ordinal) ? komp + ".port_b" : komp + ".port_a";
                    string zone = _m.Partner(andere).FirstOrDefault(IstZonenanschluss);
                    if (zone != null) return Signalnetz.Zerlegen(zone).Port;
                }
                return null;
            }

            private static string Komponentenname(string anschluss) => Signalnetz.Zerlegen(anschluss).Komponente;

            private static string Typname(Komponente k)
            {
                int punkt = k.Typ.LastIndexOf('.');
                return punkt < 0 ? k.Typ : k.Typ.Substring(punkt + 1);
            }
        }

        private sealed class Regelung
        {
            internal Regelung(string sollwert, double heizMaxW, double kuehlMaxW, double kuehlungInnen)
            {
                Sollwert = sollwert;
                HeizMaxW = heizMaxW;
                KuehlMaxW = kuehlMaxW;
                KuehlungInnen = kuehlungInnen;
            }

            /// <summary>Das Signal des Sollwerts [K].</summary>
            internal string Sollwert { get; }
            internal double HeizMaxW { get; }
            internal double KuehlMaxW { get; }
            internal double KuehlungInnen { get; }
        }
    }

    /// <summary>Ein benannter Lesefehler der Normfälle — die Datei passt nicht zur Abbildung.</summary>
    internal sealed class NormfallLeserException : Exception
    {
        internal NormfallLeserException(string meldung) : base(meldung) { }
    }

    /// <summary>Eine Komponente eines Modelica-Modells: Typ, Name und die einfachen Modifikatoren.</summary>
    internal sealed class Komponente
    {
        internal Komponente(string typ, string name, Dictionary<string, string> modifikatoren)
        {
            Typ = typ;
            Name = name;
            Modifikatoren = modifikatoren;
        }

        internal string Typ { get; }
        internal string Name { get; }
        internal Dictionary<string, string> Modifikatoren { get; }

        private readonly Dictionary<string, double> _zahlen = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double[]> _felder = new(StringComparer.Ordinal);

        internal double Zahl(string schluessel)
        {
            if (_zahlen.TryGetValue(schluessel, out double z)) return z;
            if (!Modifikatoren.TryGetValue(schluessel, out string w))
                throw new NormfallLeserException(Name + ": Parameter " + schluessel + " fehlt.");
            return _zahlen[schluessel] = ModelicaModell.Ausdruck(w);
        }

        internal double Zahl(string schluessel, double vorgabe)
            => Modifikatoren.ContainsKey(schluessel) ? Zahl(schluessel) : vorgabe;

        internal bool Wahr(string schluessel)
            => Modifikatoren.TryGetValue(schluessel, out string w) && w.Trim() == "true";

        internal double[] Feld(string schluessel)
        {
            if (_felder.TryGetValue(schluessel, out double[] f)) return f;
            return _felder[schluessel] = FeldLesen(schluessel);
        }

        private double[] FeldLesen(string schluessel)
        {
            if (!Modifikatoren.TryGetValue(schluessel, out string w))
                throw new NormfallLeserException(Name + ": Feld " + schluessel + " fehlt.");
            string t = w.Trim();
            if (!t.StartsWith("{", StringComparison.Ordinal) || !t.EndsWith("}", StringComparison.Ordinal))
                throw new NormfallLeserException(Name + ": " + schluessel + " ist kein Feld.");
            return t.Substring(1, t.Length - 2).Split(',').Select(ModelicaModell.Ausdruck).ToArray();
        }

        internal double[][] Tafel(string schluessel)
        {
            if (!Modifikatoren.TryGetValue(schluessel, out string w))
                throw new NormfallLeserException(Name + ": Tabelle " + schluessel + " fehlt.");
            string t = w.Trim();
            if (!t.StartsWith("[", StringComparison.Ordinal) || !t.EndsWith("]", StringComparison.Ordinal))
                throw new NormfallLeserException(Name + ": " + schluessel + " ist keine Tabelle.");
            return t.Substring(1, t.Length - 2)
                    .Split(';')
                    .Select(z => z.Split(',').Select(ModelicaModell.Ausdruck).ToArray())
                    .ToArray();
        }
    }

    /// <summary>
    /// Ein bewusst schmaler Leser für die Validierungsmodelle: Komponentendeklarationen mit
    /// ihren einfachen Modifikatoren (<c>name = wert</c>) und die <c>connect</c>-Anweisungen.
    /// Zeichenketten, Kommentare und <c>annotation(…)</c> werden vorher entfernt.
    /// </summary>
    internal sealed class ModelicaModell
    {
        private ModelicaModell(Dictionary<string, Komponente> komponenten, List<(string A, string B)> verbindungen)
        {
            Komponenten = komponenten;
            Verbindungen = verbindungen;
        }

        internal Dictionary<string, Komponente> Komponenten { get; }
        internal List<(string A, string B)> Verbindungen { get; }

        /// <summary>Alle Gegenstellen eines Anschlusses.</summary>
        internal IEnumerable<string> Partner(string anschluss)
        {
            foreach ((string a, string b) in Verbindungen)
            {
                if (a == anschluss) yield return b;
                else if (b == anschluss) yield return a;
            }
        }

        internal static ModelicaModell Lesen(string text)
        {
            string t = Regex.Replace(text, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
            t = Regex.Replace(t, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            t = Regex.Replace(t, @"//[^\n]*", " ");
            t = OhneAnnotationen(t);

            int gleichung = Regex.Match(t, @"\bequation\b").Index;
            if (gleichung <= 0) throw new NormfallLeserException("Abschnitt 'equation' fehlt.");
            // Kopf und Rahmen: "within …;" und "model Name" tragen keine Komponente.
            string deklarationen = Regex.Replace(t.Substring(0, gleichung), @"^\s*within[^;]*;", "");
            deklarationen = Regex.Replace(deklarationen, @"\bmodel\s+\w+\s*(?:"""")?", " ");
            string gleichungen = t.Substring(gleichung);

            var komponenten = new Dictionary<string, Komponente>(StringComparer.Ordinal);
            foreach (string anweisung in Teilen(deklarationen, ';'))
            {
                Match m = Regex.Match(anweisung.Trim(), @"^(?<typ>[A-Za-z_][\w.]*)\s+(?<name>[A-Za-z_]\w*)\s*(?<rest>.*)$", RegexOptions.Singleline);
                if (!m.Success) continue;
                string typ = m.Groups["typ"].Value;
                if (typ is "within" or "model" or "extends" or "replaceable" or "parameter" or "constant") continue;
                string rest = m.Groups["rest"].Value.Trim();
                var mods = new Dictionary<string, string>(StringComparer.Ordinal);
                if (rest.StartsWith("(", StringComparison.Ordinal))
                {
                    string innen = Klammerinhalt(rest, 0);
                    foreach (string mod in Teilen(innen, ','))
                    {
                        int gleich = GleichheitszeichenAufEbeneNull(mod);
                        if (gleich < 0) continue;
                        string schluessel = mod.Substring(0, gleich).Trim();
                        if (!Regex.IsMatch(schluessel, @"^\w+$")) continue;
                        mods[schluessel] = mod.Substring(gleich + 1).Trim();
                    }
                }
                komponenten[m.Groups["name"].Value] = new Komponente(typ, m.Groups["name"].Value, mods);
            }

            var verbindungen = new List<(string, string)>();
            foreach (Match c in Regex.Matches(gleichungen, @"connect\s*\(\s*([^,()]+?)\s*,\s*([^,()]+?)\s*\)"))
                verbindungen.Add((Regex.Replace(c.Groups[1].Value, @"\s+", ""), Regex.Replace(c.Groups[2].Value, @"\s+", "")));

            return new ModelicaModell(komponenten, verbindungen);
        }

        /// <summary>Wertet eine Zahl oder ein einfaches Produkt/Quotienten aus (<c>25*10.5</c>, <c>1/3600</c>).</summary>
        internal static double Ausdruck(string w)
        {
            string t = w.Trim();
            if (t.StartsWith("(", StringComparison.Ordinal) && t.EndsWith(")", StringComparison.Ordinal)) t = t.Substring(1, t.Length - 2);
            MatchCollection teile = Regex.Matches(t, @"([*/]?)\s*([-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?)");
            if (teile.Count == 0 || Regex.Replace(t, @"[\d.eE+\-*/\s]", "").Length > 0)
                throw new NormfallLeserException("Ausdruck '" + w + "' ist keine Zahl.");
            double wert = 1.0;
            bool erster = true;
            foreach (Match m in teile)
            {
                double z = double.Parse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (erster) { wert = z; erster = false; }
                else if (m.Groups[1].Value == "/") wert /= z;
                else wert *= z;
            }
            return wert;
        }

        private static string OhneAnnotationen(string t)
        {
            var sb = new StringBuilder(t.Length);
            int i = 0;
            while (i < t.Length)
            {
                if (i + 10 <= t.Length && string.CompareOrdinal(t, i, "annotation", 0, 10) == 0
                    && (i == 0 || !char.IsLetterOrDigit(t[i - 1])))
                {
                    int k = i + 10;
                    while (k < t.Length && char.IsWhiteSpace(t[k])) k++;
                    if (k < t.Length && t[k] == '(')
                    {
                        Klammerinhalt(t, k, out int ende);
                        i = ende + 1;
                        continue;
                    }
                }
                sb.Append(t[i]);
                i++;
            }
            return sb.ToString();
        }

        private static string Klammerinhalt(string t, int start) => Klammerinhalt(t, start, out _);

        private static string Klammerinhalt(string t, int start, out int ende)
        {
            int tiefe = 0;
            for (int i = start; i < t.Length; i++)
            {
                char c = t[i];
                if (c == '(' || c == '{' || c == '[') tiefe++;
                else if (c == ')' || c == '}' || c == ']')
                {
                    tiefe--;
                    if (tiefe == 0)
                    {
                        ende = i;
                        return t.Substring(start + 1, i - start - 1);
                    }
                }
            }
            throw new NormfallLeserException("Klammer ohne Ende.");
        }

        private static IEnumerable<string> Teilen(string t, char trenner)
        {
            int tiefe = 0, anfang = 0;
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if (c == '(' || c == '{' || c == '[') tiefe++;
                else if (c == ')' || c == '}' || c == ']') tiefe--;
                else if (c == trenner && tiefe == 0)
                {
                    yield return t.Substring(anfang, i - anfang);
                    anfang = i + 1;
                }
            }
            if (anfang < t.Length) yield return t.Substring(anfang);
        }

        private static int GleichheitszeichenAufEbeneNull(string t)
        {
            int tiefe = 0;
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if (c == '(' || c == '{' || c == '[') tiefe++;
                else if (c == ')' || c == '}' || c == ']') tiefe--;
                else if (c == '=' && tiefe == 0) return i;
            }
            return -1;
        }
    }

    /// <summary>
    /// Der Signalweg der Blockbibliothek, soweit die Validierungsmodelle ihn brauchen:
    /// Zeittafeln, Konstanten, Verstärkung, Summe, Produkt, Umschalter, Schwelle, Wurzel,
    /// °C→K und die äquivalente Außentemperatur nach VDI 6007 in der Fassung der AixLib
    /// (Befund E, Abschnitt 3).
    /// </summary>
    internal sealed class Signalnetz
    {
        private readonly ModelicaModell _m;
        private readonly Dictionary<string, Zeittafel> _tafeln = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _quellen = new(StringComparer.Ordinal);

        internal Signalnetz(ModelicaModell m)
        {
            _m = m;
        }

        /// <summary>Zerlegt <c>komp.port[i]</c>.</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string, string, int)> Zerlegt = new(StringComparer.Ordinal);

        internal static (string Komponente, string Port, int Index) Zerlegen(string signal)
            => Zerlegt.GetOrAdd(signal, ZerlegenOhnePuffer);

        private static (string, string, int) ZerlegenOhnePuffer(string signal)
        {
            Match m = Regex.Match(signal, @"^(?<k>\w+)\.(?<p>\w+)(?:\[(?<i>\d+)\])?$");
            if (!m.Success) throw new NormfallLeserException("Anschluss '" + signal + "' ist nicht lesbar.");
            int index = m.Groups["i"].Success ? int.Parse(m.Groups["i"].Value, CultureInfo.InvariantCulture) : 0;
            return (m.Groups["k"].Value, m.Groups["p"].Value, index);
        }

        /// <summary>Der Ausgang, der den Eingang <paramref name="eingang"/> speist.</summary>
        internal string Quelle(string eingang)
        {
            if (_quellen.TryGetValue(eingang, out string q)) return q;
            q = Ausgaenge(_m.Partner(eingang)).SingleOrDefault();
            if (q == null)
            {
                (string k, string p, int i) = Zerlegen(eingang);
                if (i > 0)
                {
                    string ganz = Ausgaenge(_m.Partner(k + "." + p)).SingleOrDefault();
                    if (ganz != null && Zerlegen(ganz).Index == 0) q = ganz + "[" + i + "]";
                }
            }
            if (q == null) throw new NormfallLeserException("Eingang '" + eingang + "' ist nicht verbunden.");
            _quellen[eingang] = q;
            return q;
        }

        /// <summary>
        /// Nur die Ausgänge unter den Gegenstellen — ein Eingang kann zugleich mit einem
        /// weiteren Eingang verbunden sein (derselbe Wert geht an zwei Blöcke).
        /// </summary>
        private IEnumerable<string> Ausgaenge(IEnumerable<string> gegenstellen)
        {
            foreach (string g in gegenstellen)
            {
                (string k, string p, _) = Zerlegen(g);
                bool ausgang = p is "y" or "TEqAir" or "TAir"
                    || (p == "Q_flow" && _m.Komponenten.TryGetValue(k, out Komponente komp)
                        && komp.Typ.EndsWith("HeatFlowSensor", StringComparison.Ordinal));
                if (ausgang) yield return g;
            }
        }

        /// <summary>Der Wert eines Ausgangs zur Zeit <paramref name="t"/> [s].</summary>
        internal double Wert(string ausgang, double t)
        {
            (string name, string port, int index) = Zerlegen(ausgang);
            if (!_m.Komponenten.TryGetValue(name, out Komponente k))
                throw new NormfallLeserException("Komponente '" + name + "' fehlt.");
            string typ = k.Typ.Substring(k.Typ.LastIndexOf('.') + 1);
            switch (typ)
            {
                case "CombiTimeTable":
                    if (!_tafeln.TryGetValue(name, out Zeittafel z)) _tafeln[name] = z = new Zeittafel(k);
                    return z.Wert(t, index == 0 ? 1 : index);
                case "Constant":
                    return k.Zahl("k");
                case "Gain":
                    return k.Zahl("k") * Wert(Quelle(name + ".u"), t);
                case "Add":
                    return k.Zahl("k1", 1.0) * Wert(Quelle(name + ".u1"), t) + k.Zahl("k2", 1.0) * Wert(Quelle(name + ".u2"), t);
                case "Product":
                    return Wert(Quelle(name + ".u1"), t) * Wert(Quelle(name + ".u2"), t);
                case "Switch":
                    return Wert(Quelle(name + ".u2"), t) > 0.5 ? Wert(Quelle(name + ".u1"), t) : Wert(Quelle(name + ".u3"), t);
                case "GreaterThreshold":
                    return Wert(Quelle(name + ".u"), t) > k.Zahl("threshold", 0.0) ? 1.0 : 0.0;
                case "Sqrt":
                    return Math.Sqrt(Wert(Quelle(name + ".u"), t));
                case "From_degC":
                    return Wert(Quelle(name + ".u"), t) + NormfallLeser.KELVIN;
                case "VDI6007":
                    return AequivalenteAussentemperatur(k, t);
                default:
                    throw new NormfallLeserException("Block " + name + " (" + k.Typ + ") ist nicht abgebildet.");
            }
        }

        /// <summary>
        /// Äquivalente Außentemperatur [K] des Blocks <c>EquivalentAirTemperature.VDI6007</c>:
        /// langwelliger Anteil (T_Himmel − T_Luft)·h_rad/(h_rad + h_a), kurzwelliger H_sol·a/(h_rad + h_a),
        /// Wand = Luft + beide, Fenster = Luft + langwellig·(1 − Sonnenschutzsignal), gewichtet
        /// mit wfWall, wfWin und wfGro·TGro. Ohne langwelligen Anteil trägt das Fenster die Lufttemperatur.
        /// </summary>
        private double AequivalenteAussentemperatur(Komponente k, double t)
        {
            int n = (int)k.Zahl("n", 1);
            double hRad = k.Zahl("hRad"), hA = k.Zahl("hConWallOut"), a = k.Zahl("aExt");
            double[] wfWall = k.Feld("wfWall"), wfWin = k.Feld("wfWin");
            double wfGro = k.Zahl("wfGro", 0.0);
            double tGro = wfGro != 0.0 ? k.Zahl("TGro") : 0.0;
            bool langwellig = !k.Modifikatoren.TryGetValue("withLongwave", out string lw) || lw.Trim() == "true";

            double tLuft = Wert(Quelle(k.Name + ".TDryBul"), t);
            double dLw = langwellig ? (Wert(Quelle(k.Name + ".TBlaSky"), t) - tLuft) * hRad / (hRad + hA) : 0.0;
            double summe = wfGro * tGro;
            for (int i = 1; i <= n; i++)
            {
                double hSol = Wert(Quelle(k.Name + ".HSol[" + i + "]"), t);
                double dSw = hSol * a / (hRad + hA);
                double sonnenschutz = Wert(Quelle(k.Name + ".sunblind[" + i + "]"), t);
                double wand = tLuft + dLw + dSw;
                double fenster = langwellig ? tLuft + dLw * (1.0 - sonnenschutz) : tLuft;
                summe += wfWall[i - 1] * wand + wfWin[i - 1] * fenster;
            }
            return summe;
        }
    }

    /// <summary>
    /// Eine <c>CombiTimeTable</c>: Zeitspalte und Datenspalten, lineare oder stufige Segmente,
    /// periodische oder haltende Fortsetzung. Doppelte Zeitpunkte sind Sprünge (rechtsstetig).
    /// </summary>
    internal sealed class Zeittafel
    {
        private readonly double[] _zeit;
        private readonly double[][] _zeilen;
        private readonly int[] _spalten;
        private readonly double[] _versatz;
        private readonly bool _periodisch;
        private readonly bool _stufig;

        internal Zeittafel(Komponente k)
        {
            _zeilen = k.Tafel("table");
            _zeit = _zeilen.Select(z => z[0]).ToArray();
            _spalten = k.Modifikatoren.ContainsKey("columns") ? k.Feld("columns").Select(c => (int)c).ToArray() : new[] { 2 };
            _versatz = k.Modifikatoren.ContainsKey("offset") ? k.Feld("offset") : new[] { 0.0 };
            string fort = k.Modifikatoren.TryGetValue("extrapolation", out string e) ? e : "";
            _periodisch = fort.EndsWith("Periodic", StringComparison.Ordinal);
            if (!_periodisch && !fort.EndsWith("HoldLastPoint", StringComparison.Ordinal))
                throw new NormfallLeserException(k.Name + ": Fortsetzung '" + fort + "' ist nicht abgebildet.");
            string glatt = k.Modifikatoren.TryGetValue("smoothness", out string s) ? s : "LinearSegments";
            _stufig = glatt.EndsWith("ConstantSegments", StringComparison.Ordinal);
            if (!_stufig && !glatt.EndsWith("LinearSegments", StringComparison.Ordinal))
                throw new NormfallLeserException(k.Name + ": Glättung '" + glatt + "' ist nicht abgebildet.");
        }

        /// <summary>Der Wert des Ausgangs <paramref name="ausgang"/> (ab 1) zur Zeit t.</summary>
        internal double Wert(double t, int ausgang)
        {
            int spalte = _spalten[ausgang - 1] - 1;
            double versatz = _versatz.Length == 1 ? _versatz[0] : _versatz[ausgang - 1];
            double t0 = _zeit[0], t1 = _zeit[_zeit.Length - 1];
            if (_periodisch && t1 > t0)
            {
                double periode = t1 - t0;
                t = t0 + ((t - t0) % periode + periode) % periode;
            }
            if (t <= t0) return _zeilen[0][spalte] + versatz;
            if (t >= t1) return _zeilen[_zeilen.Length - 1][spalte] + versatz;

            // Letzte Zeile mit Zeit ≤ t (bei doppelten Zeiten die spätere: rechtsstetig).
            int lo = 0, hi = _zeit.Length - 1;
            while (hi - lo > 1)
            {
                int mitte = (lo + hi) / 2;
                if (_zeit[mitte] <= t) lo = mitte;
                else hi = mitte;
            }
            double y0 = _zeilen[lo][spalte];
            if (_stufig) return y0 + versatz;
            double y1 = _zeilen[hi][spalte];
            double dt = _zeit[hi] - _zeit[lo];
            return (dt > 0.0 ? y0 + (y1 - y0) * (t - _zeit[lo]) / dt : y1) + versatz;
        }
    }
}
