using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Netz unter der Mehrzonen-Rechnung (Stufe G6b, Welle W0)</b> — die Einzonenreihen des
    /// Bauteilwegs, eingefroren als SHA-256 je Reihe, als Kern-Probe ohne Datenbank.
    ///
    /// <para><b>Warum:</b> Der Referenzlauf berührt den Bauteilweg nie — die Testdatenbank führt
    /// keine Zone. Die Wellen W3 und W4 lösen den Klimaweg aus dem Eingangsbauer
    /// (<see cref="GebaeudeModellEingang.Bauen"/>) und erweitern den Stundenschritt; beides muss
    /// für ein Gebäude mit genau einer Zone <b>bitgleich</b> bleiben (Auftrag G6b, Risiko 1). Dieses
    /// Netz hält die Reihen dafür fest: den Eingang des Lösers (θ_eq, Φ_sol, Φ_rad, Φ_conv,
    /// Sollwert, obere Grenze), das Ergebnis je Stunde (Heizlast, Raum- und operative Temperatur,
    /// Kühlbedarf, Heizsollwert, Vor- und Rücklauf der Kreise) und die Kennzahlen des Jahres.</para>
    ///
    /// <para><b>Die Fälle</b> rechnen am Probegebäude aus <see cref="Vdi6007Probe"/> mit der
    /// synthetischen Klimareihe (Kühlung und Sommerlüftung an der um 5 K wärmeren) und einer Zone
    /// mit geschichteten Bauteilen
    /// (<see cref="BauteilwegLaufProbe.Geschichtet"/>): ideal, AK1 Heizseite, AK1 Kälteseite,
    /// Kühlung ideal, Rand <c>UNBEHEIZT</c>, Sommerlüftung, Leistungsgrenze greift — und die
    /// Einzelzone mit gesetztem <c>Volumen</c> und <c>Raumhoehe</c>, der Import-Fall
    /// (<c>GebaeudeBauteilvorschlag</c>). Sie läuft über die Zeile
    /// (<see cref="GebaeudeZonenabbildung.AlsZonensatz"/>); seit der Lauf Zonenwerte auch bei
    /// einer Zone liest (Anwenderentscheid A5 = a, Welle W3), rechnet er mit Volumen und Raumhöhe
    /// der Zone — seine Heizlast- und Temperaturreihen sind dafür benannt neu erfasst, die Reihen
    /// des Eingangs blieben. Jeder Fall prüft vorab, dass er tut, was sein Name sagt.</para>
    ///
    /// <para><b>Die Regel der Prüfsumme.</b> Gehasht werden die Bits jeder Reihe (little-endian,
    /// NaN auf ein Bitmuster gebracht). <b>Streng</b> — gleiche Prüfsumme — gilt auf dem Rechner, auf
    /// dem die Abdrücke erfasst sind: Windows x64 außerhalb der CI. Auf anderen Plattformen und in
    /// der CI gilt ersatzweise die <b>Momentprobe</b>: Zahl der nicht endlichen Werte gleich, Σx, Σ|x|
    /// und Σx·(h+1)/n relativ 1e-9 gleich. Grund: Die Mathematikbibliothek der Laufzeit (Sinus,
    /// Exponentialfunktion) darf je Plattform im letzten Bit abweichen — dieselbe Regel wie beim
    /// Referenzlauf, dessen Byte-Vergleich in der CI nur Information ist.</para>
    ///
    /// <para><b>Ändert sich ein Abdruck mit Absicht</b> (A5, Welle W3), nennt die Meldung die neuen
    /// Zeilen der Tafel <see cref="Erwartet"/>; der Wechsel wird im Commit benannt.</para>
    ///
    /// <para><b>Probe 12a</b> (Mehrzonenkonzept 8.1) steht mit hier: Die Bezugsfläche des inneren
    /// Strahlungsaustauschs ist schon die Fallunterscheidung nach Gl. (29)/(31)
    /// (<c>ErsatzparameterRC</c>, Klassenweg und Bauteilweg: A_rad = min(A_AW,ges, A_IW)). Die
    /// Probe bestätigt nur; ein Einfrierschritt entfällt.</para>
    /// </summary>
    public class GebaeudeEinzonennetzTests
    {
        private readonly ITestOutputHelper _aus;

        public GebaeudeEinzonennetzTests(ITestOutputHelper aus) { _aus = aus; }

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        /// <summary>
        /// Die warme Klimareihe der Fälle mit Kühlung und Sommerlüftung: der Jahresgang um
        /// <see cref="WAERMER_K"/> angehoben — an der Reihe <see cref="Klima"/> bleibt die Zone mit
        /// geschichteten Bauteilen im Sommer unter 23 °C, und weder Kühlung noch Sommerlüftung griffen.
        /// </summary>
        private static readonly SolardatenModel[] Warm = Vdi6007Probe.Klima(h => Vdi6007Probe.Jahresgang(h) + WAERMER_K);

        /// <summary>Die Anhebung der warmen Klimareihe [K].</summary>
        internal const double WAERMER_K = 5.0;

        // =====================================================================
        //  Die Fälle
        // =====================================================================

        internal const string IDEAL = "ideal";
        internal const string AK1_HEIZSEITE = "AK1 Heizseite";
        internal const string AK1_KAELTESEITE = "AK1 Kälteseite";
        internal const string KUEHLUNG_IDEAL = "Kühlung ideal";
        internal const string RAND_UNBEHEIZT = "Rand UNBEHEIZT";
        internal const string SOMMERLUEFTUNG = "Sommerlüftung";
        internal const string LEISTUNGSGRENZE = "Leistungsgrenze";
        internal const string VOLUMEN_RAUMHOEHE = "Volumen und Raumhöhe";

        internal static readonly string[] Faelle =
        {
            IDEAL, AK1_HEIZSEITE, AK1_KAELTESEITE, KUEHLUNG_IDEAL, RAND_UNBEHEIZT, SOMMERLUEFTUNG, LEISTUNGSGRENZE, VOLUMEN_RAUMHOEHE,
        };

        public static IEnumerable<object[]> FallDaten() => Faelle.Select(f => new object[] { f });

        /// <summary>Die Heizleistungsgrenze des Falls <see cref="LEISTUNGSGRENZE"/> [kW] — sie greift an den kalten Tagen.</summary>
        internal const double GRENZE_KW = 8.0;

        /// <summary>Die Zone mit geschichteten Bauteilen, der Rand der Bodenplatte wahlweise unbeheizt.</summary>
        private static GebaeudeZonensatz Geschichtet(ProjektGebaeudeModel g, bool bodenUnbeheizt)
        {
            GebaeudeZonensatz z = BauteilwegLaufProbe.Geschichtet(g);
            if (!bodenUnbeheizt) return z;
            List<BauteilEingang> b = z.Bauteile
                .Select(x => x.Art == Bauteilart.Bodenplatte
                    ? new BauteilEingang(x.Bezeichnung, x.Art, x.Flaeche_M2, Bauteilrand.Unbeheizt, schichten: x.Schichten)
                    : x)
                .ToList();
            return new GebaeudeZonensatz(z.ZonenId, z.Bezeichnung, b);
        }

        /// <summary>
        /// Die Zone des Import-Falls über die Zeile: die Übernahme „Gebäude als eine Zone" als
        /// <see cref="ZoneModel"/>, dazu <c>Volumen</c> und <c>Raumhoehe</c> abweichend vom Gebäude
        /// (640 m³ und 3,2 m; das Gebäude: 552,75 m³ und 2,75 m), zurück über <see cref="GebaeudeZonenabbildung.AlsZonensatz"/>.
        /// </summary>
        internal static ZoneModel ImportZeile(ProjektGebaeudeModel g)
        {
            ZoneModel zeile = GebaeudeZonenabbildung.AlsZoneModel(GebaeudeZonenuebernahme.AlsEineZone(g));
            zeile.ID = 17;
            zeile.Bezeichner = "Import";
            zeile.Volumen = 640.0;
            zeile.Raumhoehe = 3.2;
            return zeile;
        }

        /// <summary>Der Eingang des Falls <paramref name="fall"/>.</summary>
        internal static GebaeudeModellEingang Eingang(string fall)
        {
            ProjektGebaeudeModel g = Aufbau(fall, out SolardatenModel[] klima, out bool kuehlbetrieb, out string stufe);
            return GebaeudeModellEingang.Bauen(g, klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                               GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb, stufe);
        }

        /// <summary>Das Gebäude des Falls <paramref name="fall"/> samt Klimareihe, Kühlbetrieb und Kopplungsstufe des Projekts.</summary>
        internal static ProjektGebaeudeModel Aufbau(string fall, out SolardatenModel[] klima, out bool kuehlbetrieb, out string stufe)
        {
            ProjektGebaeudeModel g;
            kuehlbetrieb = false;
            stufe = null;
            klima = fall == AK1_KAELTESEITE || fall == KUEHLUNG_IDEAL || fall == SOMMERLUEFTUNG ? Warm : Klima;
            switch (fall)
            {
                case IDEAL:
                    g = Vdi6007Probe.Gebaeude();
                    g.Zonen = new[] { Geschichtet(g, false) };
                    break;
                case AK1_HEIZSEITE:
                    g = Vdi6007Probe.Gebaeude();
                    g.Heizkreis_Aktiv = true;
                    g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
                    g.Heizkurve_Aktiv = true;
                    g.Zonen = new[] { Geschichtet(g, false) };
                    stufe = DbWerte.ANLAGENKOPPLUNG_AK1;
                    break;
                case AK1_KAELTESEITE:
                    g = Vdi6007Probe.Gekuehlt(24.0);
                    g.Kuehluebergabe_Aktiv = true;
                    g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
                    g.Zonen = new[] { Geschichtet(g, false) };
                    kuehlbetrieb = true;
                    stufe = DbWerte.ANLAGENKOPPLUNG_AK1;
                    break;
                case KUEHLUNG_IDEAL:
                    g = Vdi6007Probe.Gekuehlt(24.0);
                    g.Zonen = new[] { Geschichtet(g, false) };
                    kuehlbetrieb = true;
                    break;
                case RAND_UNBEHEIZT:
                    g = Vdi6007Probe.Gebaeude();
                    g.Kellertemperatur = 8.0;
                    g.Zonen = new[] { Geschichtet(g, true) };
                    break;
                case SOMMERLUEFTUNG:
                    g = Vdi6007Probe.Gebaeude();
                    g.Sommerlueftung = true;
                    g.Zonen = new[] { Geschichtet(g, false) };
                    break;
                case LEISTUNGSGRENZE:
                    g = Vdi6007Probe.Gebaeude();
                    g.Heizleistung_Max = GRENZE_KW;
                    g.Zonen = new[] { Geschichtet(g, false) };
                    break;
                case VOLUMEN_RAUMHOEHE:
                    g = Vdi6007Probe.Gebaeude();
                    g.Zonen = new[] { GebaeudeZonenabbildung.AlsZonensatz(ImportZeile(g), new Dictionary<int, BauteilaufbauModel>()) };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fall), fall, "Unbekannter Fall des Netzes.");
            }
            return g;
        }

        /// <summary>
        /// Die Reihen eines Falls, in fester Reihenfolge: der Eingang des Lösers, das Ergebnis je
        /// Stunde, die Kreise (soweit gerechnet) und die Kennzahlen des Jahres als eine Reihe.
        /// </summary>
        internal static List<(string Name, double[] Werte)> Reihen(GebaeudeModellEingang e, GebaeudeModellErgebnis r)
        {
            var z = new List<(string, double[])>
            {
                ("Eingang.ThetaEq", e.ThetaEq),
                ("Eingang.PhiSolar", e.PhiSolar),
                ("Eingang.PhiRadAW", e.PhiRadAW),
                ("Eingang.PhiRadIW", e.PhiRadIW),
                ("Eingang.PhiConv", e.PhiConv),
                ("Eingang.ThetaSoll", e.ThetaSoll),
                ("Eingang.ThetaMax", e.ThetaMax),
                ("HeizlastW", r.HeizlastW),
                ("Raumtemperatur", r.Raumtemperatur),
                ("OperativeTemperatur", r.OperativeTemperatur),
                ("KuehlbedarfKwh", r.KuehlbedarfKwh),
                ("Heizsollwert", r.Heizsollwert),
            };
            if (r.Heizkreis != null)
            {
                z.Add(("Heizkreis.VorlaufC", r.Heizkreis.VorlaufC));
                z.Add(("Heizkreis.RuecklaufC", r.Heizkreis.RuecklaufC));
                z.Add(("Heizkreis.UebergabeBegrenztAnteil", r.Heizkreis.UebergabeBegrenztAnteil));
            }
            if (r.Kuehlkreis != null)
            {
                z.Add(("Kuehlkreis.VorlaufC", r.Kuehlkreis.VorlaufC));
                z.Add(("Kuehlkreis.RuecklaufC", r.Kuehlkreis.RuecklaufC));
                z.Add(("Kuehlkreis.UebergabeBegrenztAnteil", r.Kuehlkreis.UebergabeBegrenztAnteil));
            }
            z.Add(("Kennzahlen", Kennzahlen(r)));
            return z;
        }

        /// <summary>Die Kennzahlen des Jahres als eine Reihe (<c>null</c> = NaN), in fester Reihenfolge.</summary>
        private static double[] Kennzahlen(GebaeudeModellErgebnis r)
        {
            var k = new List<double>
            {
                r.JahresheizwaermeMwh, r.SpitzeKw, r.SpitzeTagesmittelKw, r.Spitze95Kw,
                r.KuehlenergieMwh ?? double.NaN, r.StundenMitKuehlbedarf ?? double.NaN,
                r.MittlereRaumtemperaturHeizzeit, r.Ueberhitzungsstunden, r.VerbrauchAltKwh, r.Skalierungsfaktor,
                r.ThetaMax, r.StundenMitUmschaltung, r.StundenHeizenUndKuehlen, r.StundenMitSommerlueftung,
                r.KuehlSollwert ?? double.NaN,
            };
            if (r.Heizkreis != null)
            {
                HeizkreisErgebnis h = r.Heizkreis;
                k.AddRange(new[]
                {
                    h.Bedarfsstunden, h.VorlaufMittelC, h.RuecklaufMittelC, h.UebergabeBegrenztStundenH, h.UebergabeNennKw,
                    h.AuslegungVorlaufC, h.AuslegungRuecklaufC, h.HeizleistungMaxStundenH, h.HeizgrenzeStundenH,
                    h.GroessteUnterschreitungK, h.AuslegungsheizlastKw, h.AuslegungAussenC,
                });
            }
            if (r.Kuehlkreis != null)
            {
                KuehlkreisErgebnis c = r.Kuehlkreis;
                k.AddRange(new[]
                {
                    c.Bedarfsstunden, c.VorlaufMittelC, c.RuecklaufMittelC, c.UebergabeBegrenztStundenH, c.UebergabeNennKw,
                    c.KuehlleistungMaxStundenH, c.KeineKaelteStundenH, c.VorlaufgrenzeStundenH, c.GroessteUeberschreitungK,
                    c.AuslegungskuehllastKw, c.AuslegungstagKuehlung, c.VorlaufQuelleC,
                });
            }
            return k.ToArray();
        }

        // =====================================================================
        //  Der Abdruck einer Reihe
        // =====================================================================

        /// <summary>Das eine Bitmuster, auf das jedes NaN vor dem Hashen gebracht wird.</summary>
        private const long NAN_BITS = 0x7FF8000000000000L;

        /// <summary>Der Abdruck einer Reihe, die der Lauf nicht bildet (<c>null</c>, etwa der Kühlbedarf ohne Kühlung).</summary>
        internal const string KEINE_REIHE = "keine";

        /// <summary>
        /// Der Abdruck einer Reihe: SHA-256 über die Bits (little-endian), die Zahl der nicht
        /// endlichen Werte und drei Momente über die endlichen — Σx, Σ|x|, Σx·(h+1)/n. Eine Reihe,
        /// die der Lauf nicht bildet, trägt <see cref="KEINE_REIHE"/>.
        /// </summary>
        internal readonly record struct Abdruck(string Sha256, int NichtEndlich, double Summe, double Betrag, double Moment);

        internal static Abdruck Bilden(double[] reihe)
        {
            if (reihe == null) return new Abdruck(KEINE_REIHE, 0, 0.0, 0.0, 0.0);
            var bytes = new byte[8 * reihe.Length];
            int nichtEndlich = 0;
            double summe = 0.0, betrag = 0.0, moment = 0.0;
            for (int h = 0; h < reihe.Length; h++)
            {
                double x = reihe[h];
                long bits = double.IsNaN(x) ? NAN_BITS : BitConverter.DoubleToInt64Bits(x);
                BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(8 * h, 8), bits);
                if (!double.IsFinite(x)) { nichtEndlich++; continue; }
                summe += x;
                betrag += Math.Abs(x);
                moment += x * (h + 1);
            }
            if (reihe.Length > 0) moment /= reihe.Length;
            return new Abdruck(Convert.ToHexStringLower(SHA256.HashData(bytes)), nichtEndlich, summe, betrag, moment);
        }

        /// <summary>
        /// Gilt die strenge Regel (gleiche Prüfsumme)? Auf Windows x64 außerhalb der CI — dort sind
        /// die Abdrücke erfasst (Klassenkopf).
        /// </summary>
        internal static bool Streng
            => OperatingSystem.IsWindows()
               && RuntimeInformation.ProcessArchitecture == Architecture.X64
               && !string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>Die Momentprobe: nicht endliche Werte gleich, Momente relativ 1e-9 gleich.</summary>
        internal static bool MomenteGleich(Abdruck erwartet, Abdruck ist)
        {
            if (erwartet.NichtEndlich != ist.NichtEndlich) return false;
            double band = 1e-9 * Math.Max(1.0, erwartet.Betrag);
            return Math.Abs(erwartet.Summe - ist.Summe) <= band
                && Math.Abs(erwartet.Betrag - ist.Betrag) <= band
                && Math.Abs(erwartet.Moment - ist.Moment) <= band;
        }

        private static string R(double x) => x.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Die Zeile der Tafel <see cref="Erwartet"/> zu einem Abdruck.</summary>
        internal static string Tafelzeile(string fall, string reihe, Abdruck a)
            => "            [\"" + fall + "|" + reihe + "\"] = new(\"" + a.Sha256 + "\", " + a.NichtEndlich.ToString(CultureInfo.InvariantCulture)
               + ", " + R(a.Summe) + ", " + R(a.Betrag) + ", " + R(a.Moment) + "),";

        // =====================================================================
        //  Die Proben
        // =====================================================================

        [Theory]
        [MemberData(nameof(FallDaten))]
        public void Die_Einzonenreihen_des_Bauteilwegs_bleiben_bitgleich(string fall)
        {
            GebaeudeModellEingang e = Eingang(fall);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Sinnprobe(fall, e, r);

            var abweichend = new List<string>();
            var neu = new StringBuilder();
            foreach ((string name, double[] werte) in Reihen(e, r))
            {
                Abdruck ist = Bilden(werte);
                string zeile = Tafelzeile(fall, name, ist);
                neu.AppendLine(zeile);
                if (!Erwartet.TryGetValue(fall + "|" + name, out Abdruck soll))
                {
                    abweichend.Add(name + " (nicht erfasst)");
                    continue;
                }
                if (string.Equals(soll.Sha256, ist.Sha256, StringComparison.Ordinal)) continue;
                if (!Streng && MomenteGleich(soll, ist))
                {
                    _aus.WriteLine(fall + " | " + name + ": Prüfsumme abweichend, Momente im Band (Plattformregel).");
                    continue;
                }
                abweichend.Add(name);
            }

            if (abweichend.Count > 0)
                _aus.WriteLine("Neue Zeilen der Tafel für „" + fall + "“:" + Environment.NewLine + neu);
            Assert.True(abweichend.Count == 0,
                "Fall „" + fall + "“: " + abweichend.Count.ToString(CultureInfo.InvariantCulture) + " Reihe(n) nicht bitgleich — "
                + string.Join(", ", abweichend) + (Streng ? " (streng)" : " (Momentprobe)")
                + ". Ein gewollter Wechsel wird benannt; neue Zeilen:" + Environment.NewLine + neu);
        }

        [Fact]
        public void Die_Tafel_fuehrt_genau_die_Reihen_der_Faelle()
        {
            var schluessel = new HashSet<string>(StringComparer.Ordinal);
            foreach (string fall in Faelle)
            {
                GebaeudeModellEingang e = Eingang(fall);
                GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
                foreach ((string name, _) in Reihen(e, r)) Assert.True(schluessel.Add(fall + "|" + name));
            }
            Assert.Equal(schluessel.OrderBy(x => x, StringComparer.Ordinal), Erwartet.Keys.OrderBy(x => x, StringComparer.Ordinal));
        }

        [Fact]
        public void Zwei_Laeufe_desselben_Falls_sind_bitgleich()
        {
            foreach (string fall in new[] { AK1_HEIZSEITE, AK1_KAELTESEITE })
            {
                GebaeudeModellEingang e1 = Eingang(fall), e2 = Eingang(fall);
                List<(string Name, double[] Werte)> a = Reihen(e1, Vdi6007Rechenweg.Laufen(e1, 0, 1));
                List<(string Name, double[] Werte)> b = Reihen(e2, Vdi6007Rechenweg.Laufen(e2, 0, 1));
                Assert.Equal(a.Select(x => x.Name), b.Select(x => x.Name));
                for (int i = 0; i < a.Count; i++)
                    Assert.Equal(Bilden(a[i].Werte).Sha256, Bilden(b[i].Werte).Sha256);
            }
        }

        [Fact]
        public void Der_Abdruck_unterscheidet_das_letzte_Bit_und_traegt_NaN_einheitlich()
        {
            double[] a = { 1.0, 2.0, double.NaN, double.PositiveInfinity };
            double[] b = { 1.0, Math.BitIncrement(2.0), double.NaN, double.PositiveInfinity };
            double[] c = { 1.0, 2.0, -double.NaN, double.PositiveInfinity };
            Abdruck x = Bilden(a), y = Bilden(b), z = Bilden(c);
            Assert.NotEqual(x.Sha256, y.Sha256);
            Assert.True(MomenteGleich(x, y));
            Assert.Equal(x.Sha256, z.Sha256);
            Assert.Equal(2, x.NichtEndlich);
            Assert.Equal(3.0, x.Summe);
            Assert.Equal((1.0 * 1 + 2.0 * 2) / 4.0, x.Moment);
            Assert.False(MomenteGleich(x, Bilden(new[] { 1.0, 2.001, double.NaN, double.PositiveInfinity })));
        }

        /// <summary>Jeder Fall tut, was sein Name sagt — sonst hielte das Netz den falschen Weg fest.</summary>
        private static void Sinnprobe(string fall, GebaeudeModellEingang e, GebaeudeModellErgebnis r)
        {
            Assert.True(e.Bauteilweg, fall + ": Der Fall rechnet nicht den Bauteilweg.");
            Assert.Equal(1.0, r.Skalierungsfaktor);
            Assert.True(r.VerbrauchAltKwh > 0.0, fall + ": keine Heizwärme.");
            switch (fall)
            {
                case IDEAL:
                    Assert.False(e.KopplungWirksam || e.KuehlKopplungWirksam || e.KuehlungWirksam || e.Sommerlueftung);
                    Assert.Equal(Gruppenweg.Bauteilweg, e.Parameter.WegAussen);
                    break;
                case AK1_HEIZSEITE:
                    Assert.True(e.KopplungWirksam);
                    Assert.False(e.KuehlKopplungWirksam);
                    Assert.NotNull(r.Heizkreis);
                    break;
                case AK1_KAELTESEITE:
                    Assert.True(e.KuehlKopplungWirksam);
                    Assert.False(e.KopplungWirksam);
                    Assert.NotNull(r.Kuehlkreis);
                    Assert.True(r.KuehlenergieMwh > 0.0);
                    break;
                case KUEHLUNG_IDEAL:
                    Assert.True(e.KuehlungWirksam);
                    Assert.False(e.KuehlKopplungWirksam);
                    Assert.True(r.KuehlenergieMwh > 0.0);
                    break;
                case RAND_UNBEHEIZT:
                    Assert.Contains(e.Bauteile, b => b.Rand == Bauteilrand.Unbeheizt);
                    Assert.Equal(8.0, e.Kellertemperatur);
                    break;
                case SOMMERLUEFTUNG:
                    Assert.True(e.Sommerlueftung);
                    Assert.True(r.StundenMitSommerlueftung > 0);
                    break;
                case LEISTUNGSGRENZE:
                    Assert.Equal(1000.0 * GRENZE_KW, e.HeizleistungMaxW);
                    Assert.True(r.HeizlastW.Max() <= e.HeizleistungMaxW + 1e-9);
                    Assert.True(r.HeizlastW.Count(w => w >= e.HeizleistungMaxW - 1e-6) > 0, fall + ": Die Grenze greift nie.");
                    break;
                case VOLUMEN_RAUMHOEHE:
                    ZoneModel zeile = ImportZeile(Vdi6007Probe.Gebaeude());
                    Assert.NotEqual(Vdi6007Probe.Gebaeude().Raumhoehe, zeile.Raumhoehe);
                    Assert.NotEqual(Vdi6007Probe.Gebaeude().Nutzflaeche * Vdi6007Probe.Gebaeude().Raumhoehe, zeile.Volumen);
                    Assert.Equal(17, e.Zone.ZonenId);
                    // A5 (a), Welle W3: Volumen und Raumhöhe der Zone wirken - H_ve = n·V·c·ρ.
                    Assert.Equal(640.0, e.Luftvolumen_M3);
                    Assert.Equal(3.2, e.Raumhoehe_M);
                    Assert.Equal(e.Luftwechselrate_h * 640.0 * GebaeudeFestwerte.C_RHO_LUFT, e.Lueftungsleitwert_WK);
                    break;
            }
        }

        // =====================================================================
        //  Probe 12a (Mehrzonenkonzept 8.1)
        // =====================================================================

        [Fact]
        public void Probe12a_Die_Bezugsflaeche_des_Strahlungsaustauschs_folgt_schon_Gl29_und_Gl31()
        {
            // Klassenweg: A_IW = f_IW · A_f. Mit f_IW = 3 liegt A_IW über A_AW,ges — Gl. (29) greift,
            // die feste Bezugsfläche A_AW,ges gibt dasselbe Bit. Mit der Vorgabe 2,5 liegt A_IW darunter — Gl. (31).
            ProjektGebaeudeModel gross = Vdi6007Probe.Gebaeude();
            gross.Innenflaechenfaktor = 3.0;
            Bezugsflaeche(Vdi6007Probe.Eingang(gross, Klima).Parameter, true);
            Bezugsflaeche(Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima).Parameter, false);

            // Bauteilweg: A_IW aus den Innenbauteilen der Zone.
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz z = BauteilwegLaufProbe.Geschichtet(g);
            GebaeudeZonensatz mehrInnen = new GebaeudeZonensatz(z.ZonenId, z.Bezeichnung,
                z.Bauteile.Select(b => b.Art == Bauteilart.Innenwand
                    ? new BauteilEingang(b.Bezeichnung, b.Art, 4.0 * g.Nutzflaeche, b.Rand, schichten: b.Schichten)
                    : b).ToList());
            Bezugsflaeche(Vdi6007Probe.Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), mehrInnen), Klima).Parameter, true);
            Bezugsflaeche(Vdi6007Probe.Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), z), Klima).Parameter, false);
        }

        /// <summary>R_rad des Satzes gegen die feste Bezugsfläche der Gleichung, die greifen muss — bitgleich.</summary>
        private void Bezugsflaeche(ErsatzparameterRC p, bool gl29)
        {
            double aGes = p.A_AW_opak_M2 + p.A_Fenster_M2;
            Assert.Equal(gl29, p.A_IW_M2 >= aGes);
            double fest = 1.0 / (GebaeudeFestwerte.ALPHA_STR_INNEN * (gl29 ? aGes : p.A_IW_M2));
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "A_AW,ges = {0:F2} m², A_IW = {1:F2} m², Gl. ({2}), R_rad = {3:E6} K/W",
                                         aGes, p.A_IW_M2, gl29 ? 29 : 31, p.R_rad_KW));
            Assert.Equal(BitConverter.DoubleToInt64Bits(fest), BitConverter.DoubleToInt64Bits(p.R_rad_KW));
        }

        // =====================================================================
        //  Die Tafel der Abdrücke (erfasst unter Windows x64)
        //  Alle acht Fälle tragen die Prüfsummen und Momente des Rechenwegs mit Erdreichwiderstand nach
        //  DIN EN ISO 13370, erfasst unter Windows x64 (dort gilt die strenge Regel).
        // =====================================================================

        private static readonly Dictionary<string, Abdruck> Erwartet = new Dictionary<string, Abdruck>(StringComparer.Ordinal)
        {
            ["ideal|Eingang.ThetaEq"] = new("37ad3bcad3eed4445cfee6af832f36e4df23466e1c9de3194274f5df217ec629", 0, 87599.99999999999, 93382.8138492988, 48211.254213536864),
            ["ideal|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["ideal|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["ideal|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["ideal|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["ideal|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["ideal|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["ideal|HeizlastW"] = new("459589944c231e666dcba95be1d625d5c65b534721a0b2388433fe1056c21f00", 0, 67622712.94363424, 67622712.94363424, 29959996.7823614),
            ["ideal|Raumtemperatur"] = new("4302cdb2b5b8e77955a1cb37254a9ab9b91892d6482688dd61d0e6460e9210a9", 0, 175225.98699319613, 175225.98699319613, 87901.52778382532),
            ["ideal|OperativeTemperatur"] = new("69235aa9023d2305fa14ac61f3cf47cc8730a2c78b2308fd09c952bb5b3612ef", 0, 166826.16793420762, 166826.16793420762, 84175.55756219033),
            ["ideal|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["ideal|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["ideal|Kennzahlen"] = new("37294d3090e69a5ae508aad5ed3e8960685fcb8210bc857d43a8f226110dd3f7", 3, 67843.81372143622, 67843.81372143622, 40657.31429221988),
            ["AK1 Heizseite|Eingang.ThetaEq"] = new("37ad3bcad3eed4445cfee6af832f36e4df23466e1c9de3194274f5df217ec629", 0, 87599.99999999999, 93382.8138492988, 48211.254213536864),
            ["AK1 Heizseite|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["AK1 Heizseite|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["AK1 Heizseite|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["AK1 Heizseite|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["AK1 Heizseite|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Heizseite|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["AK1 Heizseite|HeizlastW"] = new("03b070cd3973550b92ee767ed6232b4f81c16abb4ac567c18f82f80dd4141dcd", 0, 63153197.26439214, 63153197.26439214, 27855709.362628035),
            ["AK1 Heizseite|Raumtemperatur"] = new("fd30434241efe2f1bcc5679a9d9c7a8fd116c89be43125da2a8d8953c56db09f", 0, 170250.7190685451, 170250.7190685451, 85566.0052500082),
            ["AK1 Heizseite|OperativeTemperatur"] = new("185fa000b8e6daeeeaabb7bd83e205389c2638a04b38fee20e5f2dc06ec1e1d5", 0, 162391.3694347422, 162391.3694347422, 82094.73003498293),
            ["AK1 Heizseite|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["AK1 Heizseite|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Heizseite|Heizkreis.VorlaufC"] = new("9abe39a812af20f1f2a04fe2471d1c775334926c8e723f78d34ec3a2b5e75afd", 1793, 281784.5099453338, 281784.5099453338, 133425.4203340356),
            ["AK1 Heizseite|Heizkreis.RuecklaufC"] = new("705bafcb4b2eb8a83f6ad51564ab86fae3c70e8596a149c794ae20f915ef4987", 1793, 249586.71756249072, 249586.71756249072, 119223.56855795131),
            ["AK1 Heizseite|Heizkreis.UebergabeBegrenztAnteil"] = new("a4bce8c95ef4ab3023a1a82a11f8724a020e1080899cfa5ad2916035a3513061", 0, 54.856775681081395, 54.856775681081395, 3.3366555276961214),
            ["AK1 Heizseite|Kennzahlen"] = new("b677950ada33130f261ffb716dd08f56dbd559c21aadbcc395b7c6a11184d0da", 3, 69996.42596198776, 70000.42596198776, 25093.333675687125),
            ["AK1 Kälteseite|Eingang.ThetaEq"] = new("7c8d645760c6fd4ac3e8746f8e2b337c0fcb7ef22dafd317820daef67b4f9ac4", 0, 131400.00000000006, 131400.00000000006, 70113.75421353652),
            ["AK1 Kälteseite|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["AK1 Kälteseite|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["AK1 Kälteseite|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["AK1 Kälteseite|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["AK1 Kälteseite|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Kälteseite|Eingang.ThetaMax"] = new("4ff151175f20e602e5dabe5a68e2c528c4873bc8e6c1bc249a4654b377417e98", 0, 210240, 210240, 105132),
            ["AK1 Kälteseite|HeizlastW"] = new("38fe061a26a68ade0c58cda1bdd572ce0266d92c5561c0cbbf1842978ac55d3f", 0, 41733061.21221403, 41733061.21221403, 17621326.031766407),
            ["AK1 Kälteseite|Raumtemperatur"] = new("dc19604a1f36dab07fe89293f063ca99ae6fd0fe3caaadeb1d540e4f790a668d", 0, 184749.54078935174, 184749.54078935174, 93114.6009145582),
            ["AK1 Kälteseite|OperativeTemperatur"] = new("f344dbdabc5dbf1cddc0d61c9c9d464ffdba93bb8300a9f848503f2b90d991c7", 0, 179889.8658829153, 179889.8658829153, 91105.03584667324),
            ["AK1 Kälteseite|KuehlbedarfKwh"] = new("dab6ab72b3c48aecad0df04326ff2391878c6bfb428e210d3080324f5833cdaa", 0, 5252.338101858456, 5252.338101858456, 2867.1640424974094),
            ["AK1 Kälteseite|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Kälteseite|Kuehlkreis.VorlaufC"] = new("781bddfbe97d27f94d8e8a4f6d918bc8e15f8079845e47444865b893ca53df44", 0, 140160, 140160, 70088),
            ["AK1 Kälteseite|Kuehlkreis.RuecklaufC"] = new("1d9410fdee74adb38fec333333f3302e7aca3a5656f4ec6f30aacdf92a5dfe33", 0, 143108.35941840993, 143108.35941840993, 71697.46038600088),
            ["AK1 Kälteseite|Kuehlkreis.UebergabeBegrenztAnteil"] = new("552174f2b8c10e2690611955d8f47cb536cc985d4b9e562e8cd03bf083bcd137", 0, 0, 0, 0),
            ["AK1 Kälteseite|Kennzahlen"] = new("2577ea22f85bd523e01eaf2b54050c6f7d7a96b91c3262ef52b25b8c7ea8f0a7", 0, 48353.01102366196, 48353.01102366196, 16543.0116117176),
            ["Kühlung ideal|Eingang.ThetaEq"] = new("7c8d645760c6fd4ac3e8746f8e2b337c0fcb7ef22dafd317820daef67b4f9ac4", 0, 131400.00000000006, 131400.00000000006, 70113.75421353652),
            ["Kühlung ideal|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Kühlung ideal|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Kühlung ideal|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Kühlung ideal|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Kühlung ideal|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Kühlung ideal|Eingang.ThetaMax"] = new("4ff151175f20e602e5dabe5a68e2c528c4873bc8e6c1bc249a4654b377417e98", 0, 210240, 210240, 105132),
            ["Kühlung ideal|HeizlastW"] = new("0f441d5feff19620559fe1f4772358639427a9d51e66971332e7a8e85fc0412b", 0, 41733061.21209458, 41733061.21209458, 17621326.03167751),
            ["Kühlung ideal|Raumtemperatur"] = new("f349e3e6bf98e78fc6c515f1ceb770d88d0aa29455b3aaa1c90140395aeff2ab", 0, 183830.49124228177, 183830.49124228177, 92613.01794990801),
            ["Kühlung ideal|OperativeTemperatur"] = new("301600d8434dbda69b3665b6ae26991da7fb435963431121ecc86f336d902eb2", 0, 179526.69044259295, 179526.69044259295, 90907.00208307868),
            ["Kühlung ideal|KuehlbedarfKwh"] = new("3ab9eae4a35f0df8e826310734a80148bbd5f1a99b8fc61a19ce9f575f278fb1", 0, 5207.774749783581, 5207.774749783581, 2843.2899451161143),
            ["Kühlung ideal|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Kühlung ideal|Kennzahlen"] = new("6bf95851b6fed29f0cd41f4d1a32e173b0945910f2512c42e3bec4073bc5ce90", 0, 45800.87665423494, 45800.87665423494, 26902.444974306614),
            ["Rand UNBEHEIZT|Eingang.ThetaEq"] = new("151f33e55fa3b7f75a7ba4f929652e0ca8826a90b5059a9297783b19f7e574d1", 0, 86249.04892148646, 90657.29198682142, 47085.94770755286),
            ["Rand UNBEHEIZT|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Rand UNBEHEIZT|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Rand UNBEHEIZT|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Rand UNBEHEIZT|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Rand UNBEHEIZT|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Rand UNBEHEIZT|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Rand UNBEHEIZT|HeizlastW"] = new("2197a00b6d0c1c7047e028492e73f3bba02a20161f422be2669d2ca885e52338", 0, 69398349.50477923, 69398349.50477923, 31026319.373400006),
            ["Rand UNBEHEIZT|Raumtemperatur"] = new("73f676143140ddbfa769290c672a32d9d3a8a1cf2eb73a3aab6321b5ec6626ad", 0, 173732.3224067787, 173732.3224067787, 87075.97896635297),
            ["Rand UNBEHEIZT|OperativeTemperatur"] = new("f3091630e3ed649b675bc5e8b715ed08769ae59c367f2388b9bc3bd6bb6cde9b", 0, 164998.08545472648, 164998.08545472648, 83151.64513773868),
            ["Rand UNBEHEIZT|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Rand UNBEHEIZT|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Rand UNBEHEIZT|Kennzahlen"] = new("967ba73c5ee0c51776f976286ba66bb6f0f3c6f845206b58db4f14ee0c751c42", 3, 69627.33707353407, 69627.33707353407, 41727.59564463499),
            ["Sommerlüftung|Eingang.ThetaEq"] = new("7c8d645760c6fd4ac3e8746f8e2b337c0fcb7ef22dafd317820daef67b4f9ac4", 0, 131400.00000000006, 131400.00000000006, 70113.75421353652),
            ["Sommerlüftung|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Sommerlüftung|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Sommerlüftung|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Sommerlüftung|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Sommerlüftung|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Sommerlüftung|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Sommerlüftung|HeizlastW"] = new("e523de998a1c07563c27fcd34d9edddab0cd26f0758acbf390e73a631aeb8648", 0, 41733061.350583576, 41733061.350583576, 17621326.13475496),
            ["Sommerlüftung|Raumtemperatur"] = new("104ce2f1652f9c44c688dac0ece90a3970aded3beaba56e598251e107e23b22c", 0, 189403.80327591364, 189403.80327591364, 95658.96357974387),
            ["Sommerlüftung|OperativeTemperatur"] = new("42e8ebf2c0fa309485b61dd235bf20d45f641049626cb82f6f9c192592e82f4a", 0, 184256.21784668026, 184256.21784668026, 93494.29112705369),
            ["Sommerlüftung|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Sommerlüftung|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Sommerlüftung|Kennzahlen"] = new("d4e1723e43936ab6d9b2044718e9ff6122d2d0fbcb8a4d5a458e595ab43bbf19", 3, 44584.35540888553, 44584.35540888553, 26988.229448186663),
            ["Leistungsgrenze|Eingang.ThetaEq"] = new("37ad3bcad3eed4445cfee6af832f36e4df23466e1c9de3194274f5df217ec629", 0, 87599.99999999999, 93382.8138492988, 48211.254213536864),
            ["Leistungsgrenze|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Leistungsgrenze|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Leistungsgrenze|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Leistungsgrenze|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Leistungsgrenze|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Leistungsgrenze|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Leistungsgrenze|HeizlastW"] = new("3a54a88269cdae347ba9b6d0863f3a5ee69e38b24ca7c9ae02fb84ba9dad0476", 0, 42060300.60856697, 42060300.60856697, 19749004.651921045),
            ["Leistungsgrenze|Raumtemperatur"] = new("38ddf039753d367b5a024f7606445994f1b0ebf4c50a46a750dd6df242d1f2dd", 0, 146771.03587703904, 146771.03587703904, 76695.82760379232),
            ["Leistungsgrenze|OperativeTemperatur"] = new("8b88e0fb04f4c0c2d763b9a6ae2430fbf8ce067afda7afb2b146c76a20915d9e", 0, 141462.31287691614, 141462.31287691614, 74210.1502506416),
            ["Leistungsgrenze|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Leistungsgrenze|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Leistungsgrenze|Kennzahlen"] = new("b382f5bbd05d33362d7b878ae3d71dc7ef9f565ee41a7f5831985cf9d81b6ebf", 3, 42253.35647376729, 42253.35647376729, 25337.98231532357),
            ["Volumen und Raumhöhe|Eingang.ThetaEq"] = new("a20fff505a7dd311ad564bacbb6d818be2ba0bb7877bb2b79d17bc68af43f837", 0, 87599.99999999997, 93391.53001598471, 48209.746419504605),
            ["Volumen und Raumhöhe|Eingang.PhiSolar"] = new("b8e0aba0bc68b18867c6f19f5e7d582c5625098a6bdda5f1a662dc43bfcbea47", 0, 16722752.434895622, 16722752.434895622, 8426512.579836711),
            ["Volumen und Raumhöhe|Eingang.PhiRadAW"] = new("963f4d3dde8031e37d0d6eae968492371e598190ab9fec1e0134e7929e465a89", 0, 8868887.908964269, 8868887.908964269, 4464993.8951825),
            ["Volumen und Raumhöhe|Eingang.PhiRadIW"] = new("7ba1f48d98dec8deb43f8b3655cbf0cb9d16c265c931e388eb96eb894cfdd94e", 0, 8372376.80679103, 8372376.80679103, 4215028.052468911),
            ["Volumen und Raumhöhe|Eingang.PhiConv"] = new("554a3c4988e6605d2633dbd096cca99f6c73090e7a6e23693ef266532fa3547a", 0, 3528607.719140612, 3528607.719140612, 1770281.6321853015),
            ["Volumen und Raumhöhe|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Volumen und Raumhöhe|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Volumen und Raumhöhe|HeizlastW"] = new("b951f484eb1762e09e21fab4bc0e5f98f5289f68257a7dae82f22a532f6fb1b0", 0, 72641406.46694075, 72641406.46694075, 32163532.829536423),
            ["Volumen und Raumhöhe|Raumtemperatur"] = new("e2824d387e31ed6bf0b31cfa855e0bce057a8e69779de72dc645f66a48157fa8", 0, 176887.76945726533, 176887.76945726533, 88798.37401698322),
            ["Volumen und Raumhöhe|OperativeTemperatur"] = new("ab0816477de088628112a9182e25a8e61177519e4b262989cb9ef1f5f2696e58", 0, 171293.03061625874, 171293.03061625874, 86338.2835524089),
            ["Volumen und Raumhöhe|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Volumen und Raumhöhe|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Volumen und Raumhöhe|Kennzahlen"] = new("8c6e5067398cf72b2c2bdea4e1a95c21a3ffedb40c4ffdd917f7641cc90a237d", 3, 73436.95050202592, 73436.95050202592, 43994.39024975437),
        };
    }
}
