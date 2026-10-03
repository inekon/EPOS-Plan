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
        //  Rechenweg RP2a (Erdreichwiderstand nach DIN EN ISO 13370): die Zeilen ThetaEq, Lasten, Temperaturen,
        //  Kreise und Kennzahlen der sieben Fälle unter Linux neu erfasst; ihre Windows-Prüfsummen zieht der
        //  nächste Lauf unter Windows x64 nach (dort gilt die strenge Regel).
        // =====================================================================

        private static readonly Dictionary<string, Abdruck> Erwartet = new Dictionary<string, Abdruck>(StringComparer.Ordinal)
        {
            ["ideal|Eingang.ThetaEq"] = new("ee9d7f9306e47f0b2d0fba32f5f38bc952a2815f4dd6273141e7fd5b15883843", 0, 87599.99999999999, 93382.8138492988, 48211.254213536864),
            ["ideal|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["ideal|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["ideal|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["ideal|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["ideal|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["ideal|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["ideal|HeizlastW"] = new("d35671a2ff3084b890a260df5bcd2fdc214d8f166218f569a25abd4f6658a70e", 0, 67622712.94363438, 67622712.94363438, 29959996.78236149),
            ["ideal|Raumtemperatur"] = new("f17b22c99ede93f3059ed3baabee152705d2265d5f65b93bbbd0276ee4898591", 0, 175225.98699319604, 175225.98699319604, 87901.52778382527),
            ["ideal|OperativeTemperatur"] = new("d0778e96350e62aec72c5640eeab0238764f04652a2d3fcccb5e796686913b54", 0, 166826.16793420757, 166826.16793420757, 84175.55756219024),
            ["ideal|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["ideal|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["ideal|Kennzahlen"] = new("ee63a1dce4eedc3bc6cf0c408964bd79f9a956a7e19aff86d896687c85a5a273", 3, 67843.81372143637, 67843.81372143637, 40657.31429221996),
            ["AK1 Heizseite|Eingang.ThetaEq"] = new("ee9d7f9306e47f0b2d0fba32f5f38bc952a2815f4dd6273141e7fd5b15883843", 0, 87599.99999999999, 93382.8138492988, 48211.254213536864),
            ["AK1 Heizseite|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["AK1 Heizseite|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["AK1 Heizseite|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["AK1 Heizseite|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["AK1 Heizseite|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Heizseite|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["AK1 Heizseite|HeizlastW"] = new("157c259d91ec826fe6c32c554424e96ddedf743526681edffb820763986d7eda", 0, 63153197.26439207, 63153197.26439207, 27855709.362628),
            ["AK1 Heizseite|Raumtemperatur"] = new("ddaf9cd0003cbd2c8b83bac82ddee4cb4c9fbc2e5b3fb15ea2ce361894e7e766", 0, 170250.71906854503, 170250.71906854503, 85566.00525000812),
            ["AK1 Heizseite|OperativeTemperatur"] = new("c312957d4cd41446d75173bab7a9afa2940388d37b00415731723332840d7a9a", 0, 162391.3694347421, 162391.3694347421, 82094.73003498287),
            ["AK1 Heizseite|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["AK1 Heizseite|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Heizseite|Heizkreis.VorlaufC"] = new("9abe39a812af20f1f2a04fe2471d1c775334926c8e723f78d34ec3a2b5e75afd", 1793, 281784.5099453338, 281784.5099453338, 133425.4203340356),
            ["AK1 Heizseite|Heizkreis.RuecklaufC"] = new("96b8ece9fedb3a623d1c065617fcd194118448646077481f5b5f57bb2f1d77da", 1793, 249586.71756249087, 249586.71756249087, 119223.56855795134),
            ["AK1 Heizseite|Heizkreis.UebergabeBegrenztAnteil"] = new("e055dd93468d2414c9a197a11cb04be9657b1a5430ca3da1cebb9fd2c8418843", 0, 54.85677568107812, 54.85677568107812, 3.33665552769517),
            ["AK1 Heizseite|Kennzahlen"] = new("c8d3c81d1fe8d746936371d37e28bc7bb3b79cfa159324816496df9f78fb1cee", 3, 69996.42596198767, 70000.42596198767, 25093.333675687103),
            ["AK1 Kälteseite|Eingang.ThetaEq"] = new("52e37c8924ce09ce2755fbfec33aaedc3b62ab8dd4199aad477aa439c27d839d", 0, 131400.00000000006, 131400.00000000006, 70113.75421353652),
            ["AK1 Kälteseite|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["AK1 Kälteseite|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["AK1 Kälteseite|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["AK1 Kälteseite|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["AK1 Kälteseite|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Kälteseite|Eingang.ThetaMax"] = new("4ff151175f20e602e5dabe5a68e2c528c4873bc8e6c1bc249a4654b377417e98", 0, 210240, 210240, 105132),
            ["AK1 Kälteseite|HeizlastW"] = new("cf0b6ec02902fce50220026fbbb7bfb48beec2ea0ff713b519c154598a6adf9d", 0, 41733061.2122141, 41733061.2122141, 17621326.031766456),
            ["AK1 Kälteseite|Raumtemperatur"] = new("bafb89e0bcb96d2e93734379e9354f049b80e31b5bfc43f20db87789ec66be4f", 0, 184749.54078935165, 184749.54078935165, 93114.60091455816),
            ["AK1 Kälteseite|OperativeTemperatur"] = new("e63054e8036bccdef0ce261253870ac25299b67cbc7ec359be397e87ab291db8", 0, 179889.86588291524, 179889.86588291524, 91105.03584667321),
            ["AK1 Kälteseite|KuehlbedarfKwh"] = new("3b9c6f79cd37a317d1d8999ab8a5e40712732543689ba5172b32c12b1d1c9606", 0, 5252.338101858425, 5252.338101858425, 2867.1640424973966),
            ["AK1 Kälteseite|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["AK1 Kälteseite|Kuehlkreis.VorlaufC"] = new("781bddfbe97d27f94d8e8a4f6d918bc8e15f8079845e47444865b893ca53df44", 0, 140160, 140160, 70088),
            ["AK1 Kälteseite|Kuehlkreis.RuecklaufC"] = new("35d977d42fcbf5de567c84216713f340b2fe81a6dcc4a024140d09edb995b94d", 0, 143108.35941840993, 143108.35941840993, 71697.4603860009),
            ["AK1 Kälteseite|Kuehlkreis.UebergabeBegrenztAnteil"] = new("552174f2b8c10e2690611955d8f47cb536cc985d4b9e562e8cd03bf083bcd137", 0, 0, 0, 0),
            ["AK1 Kälteseite|Kennzahlen"] = new("07ef0b43ef11fc89a741ad37ef758d9725fc9513977cca1db47bcc6919cbd248", 0, 48353.011023662024, 48353.011023662024, 16543.011611717622),
            ["Kühlung ideal|Eingang.ThetaEq"] = new("52e37c8924ce09ce2755fbfec33aaedc3b62ab8dd4199aad477aa439c27d839d", 0, 131400.00000000006, 131400.00000000006, 70113.75421353652),
            ["Kühlung ideal|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Kühlung ideal|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Kühlung ideal|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Kühlung ideal|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Kühlung ideal|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Kühlung ideal|Eingang.ThetaMax"] = new("4ff151175f20e602e5dabe5a68e2c528c4873bc8e6c1bc249a4654b377417e98", 0, 210240, 210240, 105132),
            ["Kühlung ideal|HeizlastW"] = new("66bb95221f35ae4f3bbe68e92aa6b60cd132d9f7b1972d3c430056bd09056c68", 0, 41733061.21209464, 41733061.21209464, 17621326.03167756),
            ["Kühlung ideal|Raumtemperatur"] = new("693b34511863e7967a527f2ccdb2f0b34f93895f5f5f10c92627510760e8ab8b", 0, 183830.49124228169, 183830.49124228169, 92613.01794990798),
            ["Kühlung ideal|OperativeTemperatur"] = new("3de6eed6de0ecbab0c5ee3e7f1b01d9c026c0d3a5d47b4b15e0ab9c0aa2926e1", 0, 179526.69044259278, 179526.69044259278, 90907.00208307862),
            ["Kühlung ideal|KuehlbedarfKwh"] = new("1005d2f07247bd2e96ea5362cf85a04ef109bfe7b3f2f764fcf5ce59ce196705", 0, 5207.774749783328, 5207.774749783328, 2843.289945115971),
            ["Kühlung ideal|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Kühlung ideal|Kennzahlen"] = new("7395bff21d78894c99a33b32ba0ee8346866e6949528ac1afedb9c9b17ec0e6b", 0, 45800.876654235, 45800.876654235, 26902.444974306647),
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
            ["Sommerlüftung|Eingang.ThetaEq"] = new("52e37c8924ce09ce2755fbfec33aaedc3b62ab8dd4199aad477aa439c27d839d", 0, 131400.00000000006, 131400.00000000006, 70113.75421353652),
            ["Sommerlüftung|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Sommerlüftung|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Sommerlüftung|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Sommerlüftung|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Sommerlüftung|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Sommerlüftung|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Sommerlüftung|HeizlastW"] = new("0e4d4244e690ff73954a8c64d19fac04d906d04e1846ec7a336f30c0845ceb2d", 0, 41733061.35058365, 41733061.35058365, 17621326.134755008),
            ["Sommerlüftung|Raumtemperatur"] = new("787b4ead93879fa8b416238258756d6bcf0026b16076c451d4b90015381fd6e1", 0, 189403.80327591364, 189403.80327591364, 95658.96357974387),
            ["Sommerlüftung|OperativeTemperatur"] = new("b0858cd3bd0275313032f18103aa678999258d38a930fe4f2983044a905d18d4", 0, 184256.21784668017, 184256.21784668017, 93494.29112705372),
            ["Sommerlüftung|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Sommerlüftung|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Sommerlüftung|Kennzahlen"] = new("3f7927b527dd62f2d6487ce18fd6d34c4e61be8e5d4515e32e7643a97edc744f", 3, 44584.35540888561, 44584.35540888561, 26988.22944818671),
            ["Leistungsgrenze|Eingang.ThetaEq"] = new("ee9d7f9306e47f0b2d0fba32f5f38bc952a2815f4dd6273141e7fd5b15883843", 0, 87599.99999999999, 93382.8138492988, 48211.254213536864),
            ["Leistungsgrenze|Eingang.PhiSolar"] = new("75f03ade3af9a622e63395a2f4825a185b7b4452fe0761cef7f3486c4c22428b", 0, 9184099.918387333, 9184099.918387333, 4667113.905858398),
            ["Leistungsgrenze|Eingang.PhiRadAW"] = new("9b17973ef7dc7f77c4c36c4b387256272cef28fe2ecbd742d94d979663e76511", 0, 6534304.983412855, 6534304.983412855, 3310221.3051216174),
            ["Leistungsgrenze|Eingang.PhiRadIW"] = new("6aadfeec96b656196012b2be8b884fd25ace0f081dddf86de3cf337e27c67b2e", 0, 3846785.942319343, 3846785.942319343, 1948747.8492095273),
            ["Leistungsgrenze|Eingang.PhiConv"] = new("4ba6d8aa095ccf09df62a4eec857d3f932e1dac581e9ccd858dc741313a1be74", 0, 2850128.9926548554, 2850128.9926548554, 1431935.751527257),
            ["Leistungsgrenze|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Leistungsgrenze|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Leistungsgrenze|HeizlastW"] = new("1c477efe68f64dc419521c85fcb2d5225db4aa979a83828426fc9c5b81cf96ea", 0, 42060300.608567014, 42060300.608567014, 19749004.651921082),
            ["Leistungsgrenze|Raumtemperatur"] = new("78f32a0ff86201f93b09e3ad5fed5e15bf6d6f1d6cf3ee90271e9402621d13ac", 0, 146771.03587703887, 146771.03587703887, 76695.82760379223),
            ["Leistungsgrenze|OperativeTemperatur"] = new("902bbdf868eb454aa3e5d42cd4b60f1a6b7a9b5f295ea80c494522873203b364", 0, 141462.31287691597, 141462.31287691597, 74210.15025064145),
            ["Leistungsgrenze|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Leistungsgrenze|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Leistungsgrenze|Kennzahlen"] = new("bd4b532f93aafd4fb471ee4eed0e5a21503246709ca6614d9627f7b09f02ee60", 3, 42253.35647376734, 42253.35647376734, 25337.9823153236),
            ["Volumen und Raumhöhe|Eingang.ThetaEq"] = new("34b48d71e63367012b8744b938dca70cca73e2efdbab0e371fd5d021d1d84386", 0, 87599.99999999996, 93391.5300159847, 48209.746419504605),
            ["Volumen und Raumhöhe|Eingang.PhiSolar"] = new("b8e0aba0bc68b18867c6f19f5e7d582c5625098a6bdda5f1a662dc43bfcbea47", 0, 16722752.434895622, 16722752.434895622, 8426512.579836711),
            ["Volumen und Raumhöhe|Eingang.PhiRadAW"] = new("963f4d3dde8031e37d0d6eae968492371e598190ab9fec1e0134e7929e465a89", 0, 8868887.908964269, 8868887.908964269, 4464993.8951825),
            ["Volumen und Raumhöhe|Eingang.PhiRadIW"] = new("7ba1f48d98dec8deb43f8b3655cbf0cb9d16c265c931e388eb96eb894cfdd94e", 0, 8372376.80679103, 8372376.80679103, 4215028.052468911),
            ["Volumen und Raumhöhe|Eingang.PhiConv"] = new("554a3c4988e6605d2633dbd096cca99f6c73090e7a6e23693ef266532fa3547a", 0, 3528607.719140612, 3528607.719140612, 1770281.6321853015),
            ["Volumen und Raumhöhe|Eingang.ThetaSoll"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Volumen und Raumhöhe|Eingang.ThetaMax"] = new("2f07f55bd39cdf776c57af645863a0579e3bc24832c2b8928b9118273d376a89", 8760, 0, 0, 0),
            ["Volumen und Raumhöhe|HeizlastW"] = new("54971f6725255583404bdeaceb3bfea28ad5065b8507533df0c5500beb3da091", 0, 72641406.46694075, 72641406.46694075, 32163532.829536423),
            ["Volumen und Raumhöhe|Raumtemperatur"] = new("094bdc9193a17baaa2b6ea7ec0660b7f02143c391ab5350e44703d48a1a5e769", 0, 176887.76945726533, 176887.76945726533, 88798.37401698322),
            ["Volumen und Raumhöhe|OperativeTemperatur"] = new("bdacca8e0c9d3e7121c3769e97e4ff7b24234e037c64472a478be35bb0cea13a", 0, 171293.03061625874, 171293.03061625874, 86338.2835524089),
            ["Volumen und Raumhöhe|KuehlbedarfKwh"] = new("keine", 0, 0, 0, 0),
            ["Volumen und Raumhöhe|Heizsollwert"] = new("30df3dd575266d5bea413c58dea32a372f2c794d7f88a64e56a10a18499db7d9", 0, 169360, 169360, 84692.33333333333),
            ["Volumen und Raumhöhe|Kennzahlen"] = new("8c6e5067398cf72b2c2bdea4e1a95c21a3ffedb40c4ffdd917f7641cc90a237d", 3, 73436.95050202592, 73436.95050202592, 43994.39024975437),
        };
    }
}
