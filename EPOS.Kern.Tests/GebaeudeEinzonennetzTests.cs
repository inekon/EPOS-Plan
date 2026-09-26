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
    /// (<see cref="GebaeudeZonenabbildung.AlsZonensatz"/>) und ändert sich <b>benannt</b>, sobald
    /// der Lauf Zonenwerte auch bei einer Zone liest (Anwenderentscheid A5 = a, Welle W3). Jeder
    /// Fall prüft vorab, dass er tut, was sein Name sagt.</para>
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
            ProjektGebaeudeModel g;
            bool kuehlbetrieb = false;
            string stufe = null;
            SolardatenModel[] klima = fall == AK1_KAELTESEITE || fall == KUEHLUNG_IDEAL || fall == SOMMERLUEFTUNG ? Warm : Klima;
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
            return GebaeudeModellEingang.Bauen(g, klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                               GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb, stufe);
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
        // =====================================================================

        private static readonly Dictionary<string, Abdruck> Erwartet = new Dictionary<string, Abdruck>(StringComparer.Ordinal)
        {
            ["ideal|Eingang.ThetaEq"] = new("fd78958553528148ae441bb80d6d3915694925cba900c262789c8317e587d621", 0, 87599.99999999974, 92817.64824221369, 48310.78623780519),
            ["ideal|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["ideal|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["ideal|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["ideal|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["ideal|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["ideal|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["ideal|HeizlastW"] = new("85bd16833360571029f63de0f48256d1d4e4d64c2355a70be36adc5e0366c765", 0, 70071398.67801094, 70071398.67801094, 30970289.401812676),
            ["ideal|Raumtemperatur"] = new("d46e33733e00af7f1049f73c839aff98ee7df8a260a2fe1053f7893c18efe3a7", 0, 174830.34244233527, 174830.34244233527, 87691.40079088119),
            ["ideal|OperativeTemperatur"] = new("6577f173610668f5f0220df908c25b6cda262abf0a58df3d84ad532b6f7886a2", 0, 166026.74991402013, 166026.74991402013, 83797.2220146915),
            ["ideal|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["ideal|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["ideal|Kennzahlen"] = new("647ec6bf31fb0dfbee7637b5958634e34560edb9ee0d6a14f9ad878bc75e1023", 3, 70300.52143501042, 70300.52143501042, 42130.190651845885),
            ["AK1 Heizseite|Eingang.ThetaEq"] = new("fd78958553528148ae441bb80d6d3915694925cba900c262789c8317e587d621", 0, 87599.99999999974, 92817.64824221369, 48310.78623780519),
            ["AK1 Heizseite|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["AK1 Heizseite|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["AK1 Heizseite|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["AK1 Heizseite|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["AK1 Heizseite|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Heizseite|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["AK1 Heizseite|HeizlastW"] = new("706702791467261349f6948fbb4504778746a372cdf69e88be1a7fa521275069", 0, 65371019.284660846, 65371019.284660846, 28763889.52442855),
            ["AK1 Heizseite|Raumtemperatur"] = new("30491cf6d9c4483d97418b90f0a1b9410fbdf4a086e32db9d4ab5a999af41a98", 0, 169773.3025419245, 169773.3025419245, 85323.7945165459),
            ["AK1 Heizseite|OperativeTemperatur"] = new("cd0a987c5a27a65dacb3ade98850ac543294f2953370ad02833b37ab276d1d1a", 0, 161544.14015146354, 161544.14015146354, 81699.47267250299),
            ["AK1 Heizseite|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["AK1 Heizseite|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Heizseite|Heizkreis.VorlaufC"] = new("9abe39a812af20f1f2a04fe2471d1c775334926c8e723f78d34ec3a2b5e75afd", 1793, 281784.5099453338, 281784.5099453338, 133425.4203340356),
            ["AK1 Heizseite|Heizkreis.RuecklaufC"] = new("6c1c4566b1e781c1aca12f7737cb4ee22db7818ed11ccf27456683bbbf0cb945", 1793, 249362.63649438132, 249362.63649438132, 119159.47808128856),
            ["AK1 Heizseite|Heizkreis.UebergabeBegrenztAnteil"] = new("68b9324c1d118d9904ef6cee8e25b070f553345bfbc7d0310754e8eef311d375", 0, 62.325645499409475, 62.325645499409475, 6.211016920993093),
            ["AK1 Heizseite|Kennzahlen"] = new("ee82e5340f2a2e17c48eb1d673fa9ac29c2a531864a0b2dff534987577774b35", 3, 72343.71347832857, 72347.71347832857, 25906.051870397107),
            ["AK1 Kälteseite|Eingang.ThetaEq"] = new("6b2f557567d4ea8770cbc67c779e3aa1c1856436e9ebe696b0b83040c20e4cbc", 0, 131399.99999999983, 131399.99999999983, 70213.28623780517),
            ["AK1 Kälteseite|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["AK1 Kälteseite|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["AK1 Kälteseite|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["AK1 Kälteseite|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["AK1 Kälteseite|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Kälteseite|Eingang.ThetaMax"] = new("4ff151175f20e602e5dabe5a68e2c528c4873bc8e6c1bc249a4654b377417e98", 0, 210240, 210240, 105132),
            ["AK1 Kälteseite|HeizlastW"] = new("961983c110d7f645c933a2e0327e2dc4688b4e4225201a99439dff687e6a38b5", 0, 43124992.392325364, 43124992.392325364, 18135329.284955073),
            ["AK1 Kälteseite|Raumtemperatur"] = new("4a71011fb8d2972910cbdae8c807247bfb9e283c4a5345a98c85e09f04d8e885", 0, 184565.5650702281, 184565.5650702281, 93032.07696734057),
            ["AK1 Kälteseite|OperativeTemperatur"] = new("28f2a55c99f04599e0f1582488358d1827b63a1f9829607d1c6cb636083b24a1", 0, 179456.74363293347, 179456.74363293347, 90927.04533031759),
            ["AK1 Kälteseite|KuehlbedarfKwh"] = new("73afe24b8fbddd790c9a96f62a05bb3ad47b269958d1a094120cd636b7d41859", 0, 5067.478149902079, 5067.478149902079, 2772.887819493036),
            ["AK1 Kälteseite|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Kälteseite|Kuehlkreis.VorlaufC"] = new("781bddfbe97d27f94d8e8a4f6d918bc8e15f8079845e47444865b893ca53df44", 0, 140160, 140160, 70088),
            ["AK1 Kälteseite|Kuehlkreis.RuecklaufC"] = new("c24d48d4d5b3d51b7df177f5b066a8110ad8edc7fd1d89212430f9f7d2d691bd", 0, 143023.68599453475, 143023.68599453475, 71654.98850556486),
            ["AK1 Kälteseite|Kuehlkreis.UebergabeBegrenztAnteil"] = new("552174f2b8c10e2690611955d8f47cb536cc985d4b9e562e8cd03bf083bcd137", 0, 0, 0, 0),
            ["AK1 Kälteseite|Kennzahlen"] = new("b402bc2527b25ae9af1cd0b402d537633fefe97488f8ac1d3b08d883b4dbdc69", 0, 49629.19430544551, 49629.19430544551, 16961.948439358053),
            ["Kühlung ideal|Eingang.ThetaEq"] = new("6b2f557567d4ea8770cbc67c779e3aa1c1856436e9ebe696b0b83040c20e4cbc", 0, 131399.99999999983, 131399.99999999983, 70213.28623780517),
            ["Kühlung ideal|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Kühlung ideal|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Kühlung ideal|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Kühlung ideal|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Kühlung ideal|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Kühlung ideal|Eingang.ThetaMax"] = new("4ff151175f20e602e5dabe5a68e2c528c4873bc8e6c1bc249a4654b377417e98", 0, 210240, 210240, 105132),
            ["Kühlung ideal|HeizlastW"] = new("83858f7316eba3069567597aceb8369aaa88dda4ffab8179563758a561cee1a1", 0, 43124992.39228614, 43124992.39228614, 18135329.284925897),
            ["Kühlung ideal|Raumtemperatur"] = new("0a6f6aa9dca4d24e8d74e2fa1f1edc1a40494dc815fdeea1b68f4a9c6a0157be", 0, 183672.41802391812, 183672.41802391812, 92543.45050274453),
            ["Kühlung ideal|OperativeTemperatur"] = new("e8a9a3e473b67a18984ab06ce80b37acc79467286e8e3303eb25b290b879c515", 0, 179099.3947083447, 179099.3947083447, 90731.69971218439),
            ["Kühlung ideal|KuehlbedarfKwh"] = new("f5bcce02e243fdf130ea47c3645324f0145a63e6fd9277fea6b53cd31dbe7a9a", 0, 5023.212161240359, 5023.212161240359, 2749.072599999496),
            ["Kühlung ideal|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Kühlung ideal|Kennzahlen"] = new("7d2c074d4e7438d81f54320e5c5105c7b9228c7ea3e4473943c9269a0ff7c763", 0, 47097.147721539404, 47097.147721539404, 27688.388771589645),
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
            ["Sommerlüftung|Eingang.ThetaEq"] = new("6b2f557567d4ea8770cbc67c779e3aa1c1856436e9ebe696b0b83040c20e4cbc", 0, 131399.99999999983, 131399.99999999983, 70213.28623780517),
            ["Sommerlüftung|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Sommerlüftung|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Sommerlüftung|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Sommerlüftung|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Sommerlüftung|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Sommerlüftung|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Sommerlüftung|HeizlastW"] = new("e9929f92215c1b0d10311bf6a8243cea65630cd23ffa80360c7bc3301d879d1f", 0, 43124992.438629955, 43124992.438629955, 18135329.319389295),
            ["Sommerlüftung|Raumtemperatur"] = new("e17b766f26b5dcb197a0527ab8bc745146c48f446c958922d2a06989ab3076ab", 0, 188924.07277011048, 188924.07277011048, 95419.77120957362),
            ["Sommerlüftung|OperativeTemperatur"] = new("84aed60ae73e2bcfc76542e7d2ce2b58d51a5120929e3ff77a2cf73ebec8984e", 0, 183525.9481606182, 183525.9481606182, 93158.35864281797),
            ["Sommerlüftung|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Sommerlüftung|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Sommerlüftung|Kennzahlen"] = new("a4b6410c098653385e8c10bc136577949205de5b1330f36bd22ed880ceca3a2a", 3, 45900.77219059127, 45900.77219059127, 27761.949958262656),
            ["Leistungsgrenze|Eingang.ThetaEq"] = new("fd78958553528148ae441bb80d6d3915694925cba900c262789c8317e587d621", 0, 87599.99999999974, 92817.64824221369, 48310.78623780519),
            ["Leistungsgrenze|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Leistungsgrenze|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Leistungsgrenze|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Leistungsgrenze|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Leistungsgrenze|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Leistungsgrenze|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Leistungsgrenze|HeizlastW"] = new("ea696a854615e8390d8716a39f78a996219542b043e928939a6a9e853b7a6e8f", 0, 42577562.98261835, 42577562.98261835, 19995558.260134175),
            ["Leistungsgrenze|Raumtemperatur"] = new("d1c2dcf71c751cec61e411e3a31f9a4de6c1f0d716bcbe3f50d761dfec4e85c7", 0, 145250.29953705284, 145250.29953705284, 76035.21485514696),
            ["Leistungsgrenze|OperativeTemperatur"] = new("10966c6040b152d05740ed1be56e2045359247732728c9e1a9211d628e71c64e", 0, 139806.70978872085, 139806.70978872085, 73487.4692926132),
            ["Leistungsgrenze|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Leistungsgrenze|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Leistungsgrenze|Kennzahlen"] = new("7e6d0fd3203ed22504fdfb12ad0c668f31f27f8af9b03b7299d96185cec604e1", 3, 42771.961445894354, 42771.961445894354, 25649.092713906768),
            ["Volumen und Raumhöhe|Eingang.ThetaEq"] = new("e0eba83844e78b3fecb3d7245ba49787247ce2ec953ceef464155109f7fee039", 0, 87600.00000000022, 92814.43218888999, 48311.3628180716),
            ["Volumen und Raumhöhe|Eingang.PhiSolar"] = new("b8e0aba0bc68b18867c6f19f5e7d582c5625098a6bdda5f1a662dc43bfcbea47", 0, 16722752.434895622, 16722752.434895622, 8426512.579836711),
            ["Volumen und Raumhöhe|Eingang.PhiRadAW"] = new("963f4d3dde8031e37d0d6eae968492371e598190ab9fec1e0134e7929e465a89", 0, 8868887.908964269, 8868887.908964269, 4464993.8951825),
            ["Volumen und Raumhöhe|Eingang.PhiRadIW"] = new("7ba1f48d98dec8deb43f8b3655cbf0cb9d16c265c931e388eb96eb894cfdd94e", 0, 8372376.80679103, 8372376.80679103, 4215028.052468911),
            ["Volumen und Raumhöhe|Eingang.PhiConv"] = new("554a3c4988e6605d2633dbd096cca99f6c73090e7a6e23693ef266532fa3547a", 0, 3528607.719140612, 3528607.719140612, 1770281.6321853015),
            ["Volumen und Raumhöhe|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Volumen und Raumhöhe|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Volumen und Raumhöhe|HeizlastW"] = new("fab373031d081f718b15446dd3fdcf608c43da103b6c6d8d6fed8df15a30b2df", 0, 73158122.10763237, 73158122.10763237, 32299680.544287898),
            ["Volumen und Raumhöhe|Raumtemperatur"] = new("eb8e4e316c085b76a34e08817f8a257f83112efaee8c9dc437dd5f202f88709b", 0, 176521.126985384, 176521.126985384, 88607.79872637082),
            ["Volumen und Raumhöhe|OperativeTemperatur"] = new("eb0ecd24f87671038d552322914017b05f5e18cf1923f2a918e6e5eda1f8eca7", 0, 170514.22972393886, 170514.22972393886, 85971.26184492679),
            ["Volumen und Raumhöhe|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Volumen und Raumhöhe|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Volumen und Raumhöhe|Kennzahlen"] = new("1884fbe3ca01f0cf48fe91f2d719a0161f7de018db7fe1685bb96bc29ef28ce1", 3, 73891.1610844762, 73891.1610844762, 44267.9094162116),
        };
    }
}
