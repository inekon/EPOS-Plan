using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Zapfereignis des Bedarfstags (<c>Tab_TwwBedarfstagEreignis_STAMM</c>, Konzept 3.1):
    /// Beginn [Minute des Tages 0 … 1439], Dauer [min, ≥ 1] und Energie [kWh] bei den
    /// Auslegungstemperaturen. Die Energie verteilt sich gleichmäßig auf die Minuten des
    /// Ereignisses; ein Ereignis über Mitternacht läuft am Tagesanfang weiter (der Bedarfstag
    /// wiederholt sich). Eine Minutenreihe ist eine Ereignisliste mit Dauer 1.
    /// </summary>
    internal sealed record Zapfereignis(int MinuteBeginn, int DauerMin, double EnergieKwh);

    /// <summary>
    /// Eine Regel des Konstruktors (A100, NA.5.2.3): ein Zapfvorgang mit Volumenstrom [l/min],
    /// Dauer [min] und Zapftemperatur [°C] — etwa „Dusche". Die Regeln stehen als Parameter im
    /// Katalog (<see cref="ZapfAuslegungParameter.KONSTRUKTOR_REGEL"/>), nie im Quelltext.
    /// </summary>
    internal sealed record Zapfregel(string Name, double VolumenstromLJeMin, double DauerMin, double ZapftemperaturC)
    {
        /// <summary>Volumen eines Vorgangs [l] = Volumenstrom · Dauer.</summary>
        internal double VolumenJeVorgangL => VolumenstromLJeMin * DauerMin;

        /// <summary>
        /// Die Regeln des Parametersatzes: je Name die drei Schlüssel
        /// <c>Konstruktor.Regel.{Name}.Volumenstrom</c>, <c>….Dauer</c>, <c>….Temperatur</c>;
        /// eine Regel, der einer fehlt, wird benannt abgelehnt. Geordnet nach Name (ordinal).
        /// </summary>
        internal static IReadOnlyList<Zapfregel> AusParametern(Parametersatz ps)
        {
            var namen = new SortedSet<string>(StringComparer.Ordinal);
            if (ps != null)
                foreach (string s in ps.Werte.Keys)
                {
                    if (!s.StartsWith(ZapfAuslegungParameter.KONSTRUKTOR_REGEL, StringComparison.Ordinal)) continue;
                    string rest = s.Substring(ZapfAuslegungParameter.KONSTRUKTOR_REGEL.Length);
                    int punkt = rest.LastIndexOf('.');
                    if (punkt > 0) namen.Add(rest.Substring(0, punkt));
                }
            var regeln = new List<Zapfregel>();
            foreach (string n in namen)
            {
                string k = ZapfAuslegungParameter.KONSTRUKTOR_REGEL + n + ".";
                regeln.Add(new Zapfregel(n,
                    Auslegungspruefung.Positiv(ps.Wert(k + "Volumenstrom"), ZapfSatz.Neu("BEGRIFF_REGEL_VOLUMENSTROM", n)),
                    Auslegungspruefung.Positiv(ps.Wert(k + "Dauer"), ZapfSatz.Neu("BEGRIFF_REGEL_DAUER", n)),
                    Auslegungspruefung.Endlich(ps.Wert(k + "Temperatur"), ZapfSatz.Neu("BEGRIFF_REGEL_TEMPERATUR", n))));
            }
            return regeln.AsReadOnly();
        }
    }

    /// <summary>
    /// Eine Zeile des Konstruktors (A100, NA.5.2.3, Muster Tabelle NA.3): Zeitfenster
    /// [Beginn; Ende) in Minuten, gezapftes Volumen [l] bei der Zapftemperatur [°C], Verbraucher.
    /// Das Volumen verteilt sich gleichmäßig über das Fenster.
    /// </summary>
    internal sealed record Konstruktorzeile(int MinuteBeginn, int MinuteEnde, double VolumenL, double ZapftemperaturC,
                                            string Verbraucher = "")
    {
        /// <summary>Eine Zeile aus Vorgängen einer Regel: Volumen = Anzahl · Volumenstrom · Dauer.</summary>
        internal static Konstruktorzeile AusVorgaengen(int beginn, int ende, double anzahl, Zapfregel regel,
                                                       string verbraucher = null)
        {
            if (regel == null) throw new ArgumentNullException(nameof(regel));
            Auslegungspruefung.NichtNegativ(anzahl, ZapfSatz.Neu("BEGRIFF_ANZAHL_VORGAENGE"));
            return new Konstruktorzeile(beginn, ende, anzahl * regel.VolumenJeVorgangL, regel.ZapftemperaturC,
                                        verbraucher ?? regel.Name);
        }
    }

    /// <summary>
    /// Ein Zapfblock des DIN-4708-Profils: Beginn [min], Dauer [min] und Anteil am Wärmebedarf
    /// W_z der Zapfperiode. Lage, Dauer und Anteil sind Katalogkonstanten
    /// (<see cref="ZapfAuslegungParameter.DIN4708_PROFIL_BLOCK"/>), nie Quelltext.
    /// </summary>
    internal sealed record Zapfblock(int MinuteBeginn, int DauerMin, double AnteilWz)
    {
        /// <summary>
        /// Die Blöcke des Parametersatzes: Zahl aus <see cref="ZapfAuslegungParameter.DIN4708_PROFIL_BLOECKE"/>,
        /// je Block <c>DIN4708.Profil.Block.{k}.Beginn|Dauer|Anteil</c> (k = 1 … n). Fehlt ein
        /// Schlüssel, benannte Ablehnung (<see cref="ParametersatzException"/>).
        /// </summary>
        internal static IReadOnlyList<Zapfblock> AusParametern(Parametersatz ps)
        {
            double zahl = ps.Wert(ZapfAuslegungParameter.DIN4708_PROFIL_BLOECKE);
            if (double.IsNaN(zahl) || zahl < 1 || zahl != Math.Floor(zahl) || zahl > Bedarfstag.MINUTEN)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_ZAPFBLOECKE_ZAHL"));
            var bloecke = new List<Zapfblock>();
            for (int k = 1; k <= (int)zahl; k++)
            {
                string s = ZapfAuslegungParameter.DIN4708_PROFIL_BLOCK + k.ToString(CultureInfo.InvariantCulture) + ".";
                bloecke.Add(new Zapfblock(Ganz(ps.Wert(s + "Beginn"), s + "Beginn"), Ganz(ps.Wert(s + "Dauer"), s + "Dauer"),
                                          Auslegungspruefung.NichtNegativ(ps.Wert(s + "Anteil"),
                                                                          ZapfSatz.Neu("BEGRIFF_PARAMETERWERT", s + "Anteil"))));
            }
            return bloecke.AsReadOnly();
        }

        private static int Ganz(double w, string was)
        {
            if (double.IsNaN(w) || double.IsInfinity(w) || w != Math.Floor(w) || w < 0 || w > int.MaxValue)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_KEINE_MINUTENZAHL", was));
            return (int)w;
        }
    }

    /// <summary>
    /// Eine Katalogzeile eines Bedarfstags (<c>Tab_TwwBedarfstag_STAMM</c> samt Ereignissen,
    /// Konzept 3.1): A100-Referenz, DIN-4708-Profil, Konstruktor oder Ecodesign. Die
    /// Bezugsmenge (etwa N oder Personen) dient der Skalierung; die Energien gelten bei den
    /// Auslegungstemperaturen (die Tabelle trägt keine eigenen Temperaturen).
    /// </summary>
    internal sealed record BedarfstagKatalogzeile(int Id, string Bezeichner, string Katalogversion,
                                                  ZapfBedarfstagquelle QuelleArt, double? Bezugsmenge,
                                                  Provenienz Herkunft, IReadOnlyList<Zapfereignis> Ereignisse)
    {
        /// <summary>Der Stand der Katalogzeile.</summary>
        public ZapfKatalogstatus Status { get; init; }

        /// <summary>Gehört die Zeile zur Auslieferung (<c>ReadOnly</c>)?</summary>
        public bool ReadOnly { get; init; }

        /// <summary>
        /// Die Bezugsart der <see cref="Bezugsmenge"/> (<c>Tab_TwwBedarfstag_STAMM.Bezugsart</c>, Schritt 120;
        /// N10 (j)): Skaliert wird nur auf eine Menge derselben Bezugsart. <c>null</c> = ohne Angabe (ein
        /// Tag ohne Bezugsmenge wird nie skaliert; ein Tag mit Bezugsmenge ohne Bezugsart skaliert auf
        /// die eine Bezugsart der Gruppe).
        /// </summary>
        public ZapfBezugsart? Bezugsart { get; init; }
    }

    /// <summary>
    /// <b>Der Bedarfstag der Auslegung</b> (Umsetzungskonzept Zapfprofilgenerator 4.5): 1440
    /// Minutenwerte in kWh, gebildet aus Zapfereignissen — nie aus der Jahresreihe. Die Energie
    /// gilt bei den Auslegungstemperaturen (θ_KW,Auslegung, 4.0).
    ///
    /// <para><b>Quellen (4.5):</b> (1) Stundenprofil der Zonen am Tag des größten Tagesbedarfs,
    /// gleichmäßig auf Minuten verteilt — nur mit dem Vermerk „Spitzen unterschätzt"
    /// (<see cref="SpitzenUnterschaetzt"/>); (2) A100-Referenzprofil aus dem Katalog;
    /// (3) DIN-4708-Profil aus Kennzahl N und Wärmebedarf W_z, Zapfblöcke aus dem Katalog;
    /// (4) Konstruktor nach A100 (NA.5.2.3), Regeln als Parameter; (5) Ecodesign aus dem
    /// Katalog. Die Vorgaberegel steht in <see cref="Bedarfstagregel"/>.</para>
    ///
    /// <para><b>Unveränderlich.</b> Die Minutenwerte gibt der Tag nur lesend heraus.</para>
    /// </summary>
    internal sealed class Bedarfstag
    {
        /// <summary>Minuten eines Tages.</summary>
        internal const int MINUTEN = 1440;

        /// <summary>Minuten je Stunde.</summary>
        internal const int MINUTEN_JE_STUNDE = 60;

        /// <summary>Kennung des Vermerks bei einem Tag aus dem Stundenprofil (VDI-6002-Warnung zu Einzeltagesspitzen).</summary>
        internal const string VERMERK_SPITZEN_UNTERSCHAETZT = "SPITZEN_UNTERSCHAETZT";

        private readonly double[] _minutenKwh;
        private readonly Zapfereignis[] _ereignisse;

        private Bedarfstag(ZapfBedarfstagquelle? quelle, string bezeichner, Zapfereignis[] ereignisse, Provenienz herkunft,
                           bool spitzenUnterschaetzt, ZapfSatz benennung = null)
        {
            Quelle = quelle;
            Benennung = benennung;
            Bezeichner = benennung?.Klartext ?? bezeichner ?? "";
            Herkunft = herkunft;
            SpitzenUnterschaetzt = spitzenUnterschaetzt;
            _ereignisse = ereignisse;
            _minutenKwh = new double[MINUTEN];
            foreach (Zapfereignis e in ereignisse)
            {
                double jeMinute = e.EnergieKwh / e.DauerMin;
                for (int k = 0; k < e.DauerMin; k++)
                    _minutenKwh[(e.MinuteBeginn + k) % MINUTEN] += jeMinute;
            }
            double summe = 0.0, spitze = 0.0;
            for (int i = 0; i < MINUTEN; i++)
            {
                summe += _minutenKwh[i];
                if (_minutenKwh[i] > spitze) spitze = _minutenKwh[i];
            }
            TagessummeKwh = summe;
            GroessteMinutenleistungKw = spitze * MINUTEN_JE_STUNDE;
            double stunde = 0.0;
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
            {
                double s = 0.0;
                for (int m = 0; m < MINUTEN_JE_STUNDE; m++) s += _minutenKwh[h * MINUTEN_JE_STUNDE + m];
                if (s > stunde) stunde = s;
            }
            GroessteStundenleistungKw = stunde;
        }

        /// <summary>
        /// Die Quelle des Tages (4.5); <c>null</c> bei einem gezogenen Tag des Auslegungsensembles
        /// (<see cref="AusZiehung"/>, 4.4) — er ist weder Katalog- noch Vorgabetag.
        /// </summary>
        internal ZapfBedarfstagquelle? Quelle { get; }

        /// <summary>Ist der Tag eine Realisierung des Auslegungsensembles (4.4)?</summary>
        internal bool Gezogen => !Quelle.HasValue;

        /// <summary>Der neutrale Name des Tages (bei einem gebildeten Tag der deutsche Wortlaut seiner <see cref="Benennung"/>).</summary>
        internal string Bezeichner { get; }

        /// <summary>
        /// Der Name eines GEBILDETEN Tags als Satz (Kennung und Werte, N11 (k)) — Stundenprofil
        /// „Tag d", DIN-4708-Profil „N = …"; <c>null</c> bei einem Katalogtag, dessen Bezeichner
        /// ein Datum des Katalogs ist.
        /// </summary>
        internal ZapfSatz Benennung { get; }

        /// <summary>Die Provenienz des Tages; <c>null</c> beim Stundenprofil (es stammt aus der Zone).</summary>
        internal Provenienz Herkunft { get; }

        /// <summary>
        /// Stammt der Tag aus einem Stundenprofil? Dann trägt er den Vermerk „Spitzen
        /// unterschätzt" und ist nie Empfehlung ohne Rückfrage (4.5).
        /// </summary>
        internal bool SpitzenUnterschaetzt { get; }

        /// <summary>Die Ereignisse in ihrer Reihenfolge — nur lesbar.</summary>
        internal IReadOnlyList<Zapfereignis> Ereignisse => Array.AsReadOnly(_ereignisse);

        /// <summary>Die 1440 Minutenwerte [kWh je Minute] — nur lesbar.</summary>
        internal IReadOnlyList<double> MinutenKwh => Array.AsReadOnly(_minutenKwh);

        /// <summary>Die Energie des Tages [kWh].</summary>
        internal double TagessummeKwh { get; }

        /// <summary>Die größte Minutenleistung [kW] = größter Minutenwert · 60.</summary>
        internal double GroessteMinutenleistungKw { get; }

        /// <summary>Die größte Stundenleistung [kW] = größte Summe einer Uhrstunde (nachrichtlich).</summary>
        internal double GroessteStundenleistungKw { get; }

        /// <summary>Der Tag mit allen Energien mal <paramref name="faktor"/> (Skalierung auf die Bezugsmenge).</summary>
        internal Bedarfstag Mal(double faktor)
        {
            Auslegungspruefung.NichtNegativ(faktor, ZapfSatz.Neu("BEGRIFF_SKALIERUNGSFAKTOR"));
            var neu = new Zapfereignis[_ereignisse.Length];
            for (int i = 0; i < neu.Length; i++)
                neu[i] = _ereignisse[i] with { EnergieKwh = _ereignisse[i].EnergieKwh * faktor };
            return new Bedarfstag(Quelle, Bezeichner, neu, Herkunft, SpitzenUnterschaetzt, Benennung);
        }

        // =================================================================================
        // Bildung
        // =================================================================================

        /// <summary>
        /// Ein Tag aus Ereignissen. Beginn außerhalb 0 … 1439, Dauer kleiner 1 oder über einem
        /// Tag, negative oder nicht endliche Energie und eine leere Liste werden benannt abgelehnt.
        /// </summary>
        internal static Bedarfstag AusEreignissen(ZapfBedarfstagquelle quelle, string bezeichner,
                                                  IEnumerable<Zapfereignis> ereignisse, Provenienz herkunft,
                                                  ZapfSatz benennung = null)
        {
            string name = benennung?.Klartext ?? bezeichner;
            Zapfereignis[] liste = Geprueft(name, ereignisse);
            if (liste.Length == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_BEDARFSTAG_LEER", (object)benennung ?? name ?? ""));
            return new Bedarfstag(quelle, bezeichner, liste, herkunft, quelle == ZapfBedarfstagquelle.Stundenprofil, benennung);
        }

        /// <summary>
        /// <b>Ein gezogener Tag</b> des Auslegungsensembles (4.4, 4.5 b): die Ereignisse einer
        /// Realisierung, ohne Quelle und ohne Provenienz. Anders als ein Katalog- oder Vorgabetag darf
        /// er leer sein — eine Realisierung ohne Zapfung ist ein Ergebnis, kein Fehler. Ein Ereignis
        /// über Mitternacht läuft am Tagesanfang weiter wie bei jedem Bedarfstag.
        /// </summary>
        internal static Bedarfstag AusZiehung(string bezeichner, IEnumerable<Zapfereignis> ereignisse)
            => new Bedarfstag(null, bezeichner, Geprueft(bezeichner, ereignisse), null, false);

        /// <summary>Prüft die Ereignisse (Beginn im Tag, Dauer 1 … 1440, Energie endlich und nicht negativ).</summary>
        private static Zapfereignis[] Geprueft(string bezeichner, IEnumerable<Zapfereignis> ereignisse)
        {
            var liste = new List<Zapfereignis>();
            if (ereignisse != null)
                foreach (Zapfereignis e in ereignisse)
                {
                    if (e == null || e.MinuteBeginn < 0 || e.MinuteBeginn >= MINUTEN || e.DauerMin < 1 || e.DauerMin > MINUTEN)
                        throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                            ZapfSatz.Neu("AUSLEGUNG_EREIGNIS_AUSSERHALB", bezeichner ?? ""));
                    if (double.IsNaN(e.EnergieKwh) || double.IsInfinity(e.EnergieKwh) || e.EnergieKwh < 0)
                        throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                            ZapfSatz.Neu("AUSLEGUNG_EREIGNIS_ENERGIE", bezeichner ?? ""));
                    liste.Add(e);
                }
            return liste.ToArray();
        }

        /// <summary>
        /// <b>Quelle (1): Stundenprofil.</b> 24 Stundenwerte [kWh] des Tages mit dem größten
        /// Tagesbedarf, gleichmäßig auf die 60 Minuten jeder Stunde verteilt. Trägt immer den
        /// Vermerk „Spitzen unterschätzt".
        /// </summary>
        internal static Bedarfstag AusStunden(double[] stundenKwh, string bezeichner, ZapfSatz benennung = null)
        {
            if (stundenKwh == null || stundenKwh.Length != Zapfkalender.STUNDEN_TAG)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_STUNDENPROFIL_RASTER"));
            var e = new Zapfereignis[Zapfkalender.STUNDEN_TAG];
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                e[h] = new Zapfereignis(h * MINUTEN_JE_STUNDE, MINUTEN_JE_STUNDE, stundenKwh[h]);
            return AusEreignissen(ZapfBedarfstagquelle.Stundenprofil, bezeichner, e, null, benennung);
        }

        /// <summary>
        /// <b>Quellen (2) und (5): Katalogzeile</b> (A100-Referenz, Ecodesign; auch ein
        /// gespeicherter Konstruktor- oder DIN-4708-Tag), mit <paramref name="skalierung"/> auf die
        /// Bezugsmenge der Zone gebracht (<see cref="Skalierung"/>).
        /// </summary>
        internal static Bedarfstag AusKatalog(BedarfstagKatalogzeile zeile, double skalierung)
        {
            if (zeile == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_BEDARFSTAG_FEHLT"));
            Bedarfstag t = AusEreignissen(zeile.QuelleArt, zeile.Bezeichner, zeile.Ereignisse, zeile.Herkunft);
            return skalierung == 1.0 ? t : t.Mal(skalierung);
        }

        /// <summary>
        /// Die Skalierung eines Katalogtags auf die Bezugsmenge der Auslegung:
        /// <c>Ziel / Bezugsmenge des Tages</c>; ohne Bezugsmenge des Tages oder ohne Ziel 1.
        /// </summary>
        internal static double Skalierung(BedarfstagKatalogzeile zeile, double? zielbezugsmenge)
        {
            if (zeile?.Bezugsmenge == null || !(zeile.Bezugsmenge.Value > 0) || !zielbezugsmenge.HasValue) return 1.0;
            Auslegungspruefung.Positiv(zielbezugsmenge.Value, ZapfSatz.Neu("BEGRIFF_BEZUGSMENGE_AUSLEGUNG"));
            return zielbezugsmenge.Value / zeile.Bezugsmenge.Value;
        }

        /// <summary>
        /// <b>Quelle (3): DIN-4708-Profil</b> aus dem Wärmebedarf W_z(N) [kWh] der Kennzahl
        /// (<see cref="Din4708Kennzahl"/>) und den Zapfblöcken des Katalogs: Block k trägt
        /// <c>Anteil_k · W_z</c> über seine Dauer. Nur Wohnen (Gültigkeit prüft der Aufrufer).
        /// </summary>
        internal static Bedarfstag Din4708(double wzKwh, IReadOnlyList<Zapfblock> bloecke, double kennzahlN)
        {
            Auslegungspruefung.NichtNegativ(wzKwh, ZapfSatz.Neu("BEGRIFF_WZ"));
            if (bloecke == null || bloecke.Count == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_DIN_PROFIL_OHNE_BLOCK"));
            var e = new List<Zapfereignis>(bloecke.Count);
            foreach (Zapfblock b in bloecke) e.Add(new Zapfereignis(b.MinuteBeginn, b.DauerMin, b.AnteilWz * wzKwh));
            return AusEreignissen(ZapfBedarfstagquelle.Din4708Profil, null, e, null,
                                  ZapfSatz.Neu("AUSTEXT_TAG_DIN4708", kennzahlN));
        }

        /// <summary>
        /// <b>Quelle (4): Konstruktor nach A100</b> (NA.5.2.3). Je Zeile ein Ereignis über
        /// [Beginn; Ende) mit der Energie <c>E = V · c_w · (θ_Zapf − θ_KW,Auslegung) / 1000</c>
        /// [kWh]. Ein leeres Fenster, ein Ende über Mitternacht (1440), ein negatives Volumen oder
        /// eine Zapftemperatur nicht über dem Kaltwasser werden benannt abgelehnt.
        /// </summary>
        internal static Bedarfstag Konstruieren(IReadOnlyList<Konstruktorzeile> zeilen, double kaltwasserAuslegungC,
                                                string bezeichner)
        {
            if (zeilen == null || zeilen.Count == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_KONSTRUKTOR_OHNE_ZEILE"));
            var e = new List<Zapfereignis>(zeilen.Count);
            foreach (Konstruktorzeile z in zeilen)
            {
                if (z == null || z.MinuteBeginn < 0 || z.MinuteBeginn >= MINUTEN || z.MinuteEnde <= z.MinuteBeginn
                    || z.MinuteEnde > MINUTEN)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_KONSTRUKTOR_FENSTER"));
                Auslegungspruefung.NichtNegativ(z.VolumenL, ZapfSatz.Neu("BEGRIFF_ZEILE_VOLUMEN", z.Verbraucher ?? ""));
                double delta = Auslegungspruefung.Spreizung(z.ZapftemperaturC, kaltwasserAuslegungC,
                    ZapfSatz.Neu("BEGRIFF_ZEILE_SPREIZUNG", z.Verbraucher ?? ""));
                e.Add(new Zapfereignis(z.MinuteBeginn, z.MinuteEnde - z.MinuteBeginn, Mengengeruest.EnergieKwh(z.VolumenL, delta)));
            }
            return AusEreignissen(ZapfBedarfstagquelle.Konstruktor, bezeichner, e, null);
        }
    }

    /// <summary>
    /// <b>Die Minutenstatistik der Einheiten einer Zone</b> im Auslegungsensemble (4.5 b, Konzept
    /// S4c): je Minute des Bedarfstags Summe und Quadratsumme der Minutenlast einer Einheit
    /// [kWh je Minute] über die Stichproben (Einheiten × Realisierungen), daraus Mittel und Varianz
    /// für den Vergleich <c>μ + z · σ / √N</c>.
    ///
    /// <para><b>Ein Minutentyp wie der <see cref="Bedarfstag"/></b> (Invariante 2.4): Minutenwerte
    /// der Auslegung stehen nur in diesen Typen; die Klassen des Ensembles reichen sie nie als
    /// Zahlenfeld weiter (Wache <c>ZapfprofilTrennungWacheTests</c>). Summiert wird in der Folge der
    /// Aufrufe — der Aufrufer legt sie fest (Einheit für Einheit, dann Realisierung für
    /// Realisierung), damit parallel und seriell dieselben Bits entstehen.</para>
    /// </summary>
    internal sealed class Minutenstatistik
    {
        private readonly double[] _summeKwh = new double[Bedarfstag.MINUTEN];
        private readonly double[] _quadratKwh2 = new double[Bedarfstag.MINUTEN];

        /// <summary>Die Zahl der aufgenommenen Stichproben (Einheitentage).</summary>
        internal long Stichproben { get; private set; }

        /// <summary>
        /// Nimmt den Minutengang einer Einheit auf — 1440 Werte [kWh je Minute], Minute für Minute
        /// in Summe und Quadratsumme.
        /// </summary>
        internal void Hinzufuegen(double[] minutenKwh)
        {
            if (minutenKwh == null || minutenKwh.Length != Bedarfstag.MINUTEN)
                throw new ArgumentException("Ein Minutengang trägt 1440 Werte.", nameof(minutenKwh));
            for (int t = 0; t < Bedarfstag.MINUTEN; t++)
            {
                double x = minutenKwh[t];
                _summeKwh[t] += x;
                _quadratKwh2[t] += x * x;
            }
            Stichproben++;
        }

        /// <summary>Nimmt eine Teilstatistik auf: ihre Summen je Minute werden in dieser Folge addiert.</summary>
        internal void Hinzufuegen(Minutenstatistik teil)
        {
            if (teil == null) throw new ArgumentNullException(nameof(teil));
            for (int t = 0; t < Bedarfstag.MINUTEN; t++)
            {
                _summeKwh[t] += teil._summeKwh[t];
                _quadratKwh2[t] += teil._quadratKwh2[t];
            }
            Stichproben += teil.Stichproben;
        }

        /// <summary>Mittel der Minutenlast einer Einheit in Minute <paramref name="minute"/> [kWh je Minute]; 0 ohne Stichprobe.</summary>
        internal double MittelKwh(int minute) => Stichproben > 0 ? _summeKwh[minute] / Stichproben : 0.0;

        /// <summary>Varianz der Minutenlast einer Einheit in Minute <paramref name="minute"/> [kWh² je Minute²], ≥ 0.</summary>
        internal double Varianz(int minute)
        {
            if (Stichproben == 0) return 0.0;
            double m = _summeKwh[minute] / Stichproben;
            double v = _quadratKwh2[minute] / Stichproben - m * m;
            return v > 0 ? v : 0.0;
        }
    }

    /// <summary>
    /// Die Wahl des Bedarfstags einer Topologiegruppe (Vorgaberegel 4.5): Quelle, ob der
    /// Konstruktor zu öffnen ist, und der Satz der Wahl als Kennung und Werte (N11 (k)).
    /// </summary>
    internal sealed record Bedarfstagwahl(ZapfBedarfstagquelle? Quelle, bool KonstruktorOeffnen, ZapfSatz Grund)
    {
        /// <summary>Der deutsche Wortlaut des Satzes (Protokoll, Test).</summary>
        public string GrundText => Grund?.Klartext ?? "";
    }

    /// <summary>
    /// <b>Die Vorgaberegel des Bedarfstags</b> (4.5): Eine ausdrückliche Wahl des Projekts gilt
    /// vor einem gewählten Katalogtag; ohne beide: Wohnen → DIN-4708-Profil, sobald es rechenbar
    /// ist (Katalogkonstanten und Wohnungstabelle vorhanden, K1/K8), sonst und bei Nichtwohnen
    /// der Konstruktor. Ohne konstruierten Tag öffnet die Auslegung den Konstruktor, statt still
    /// das Stundenprofil zu nehmen.
    /// </summary>
    internal static class Bedarfstagregel
    {
        /// <summary>
        /// Wählt die Quelle. <paramref name="wohnen"/>: alle Zonen der Gruppe tragen eine
        /// Nutzungsart mit Kalenderart Wohnen; <paramref name="din4708Rechenbar"/>: die
        /// DIN-4708-Kennzahl und die Zapfblöcke sind rechenbar; <paramref name="gewaehlterTag"/>:
        /// die Katalogzeile aus <c>Tab_TwwProjekt.ID_Bedarfstag</c> oder <c>null</c>.
        /// </summary>
        internal static Bedarfstagwahl Waehlen(ZapfBedarfstagquelle? projektwahl, bool wohnen, bool din4708Rechenbar,
                                               BedarfstagKatalogzeile gewaehlterTag)
        {
            if (projektwahl.HasValue)
            {
                switch (projektwahl.Value)
                {
                    case ZapfBedarfstagquelle.Stundenprofil:
                        return new Bedarfstagwahl(ZapfBedarfstagquelle.Stundenprofil, false, ZapfSatz.Neu("AUSTEXT_WAHL_STUNDENPROFIL"));
                    case ZapfBedarfstagquelle.Din4708Profil:
                        if (!wohnen)
                            return new Bedarfstagwahl(null, true, ZapfSatz.Neu("AUSTEXT_WAHL_DIN_NUR_WOHNEN"));
                        if (!din4708Rechenbar)
                            return new Bedarfstagwahl(null, true, ZapfSatz.Neu("AUSTEXT_WAHL_DIN_NICHT_RECHENBAR"));
                        return new Bedarfstagwahl(ZapfBedarfstagquelle.Din4708Profil, false, ZapfSatz.Neu("AUSTEXT_WAHL_DIN"));
                    default:
                        if (gewaehlterTag != null && gewaehlterTag.QuelleArt == projektwahl.Value)
                            return new Bedarfstagwahl(projektwahl.Value, false,
                                ZapfSatz.Neu("AUSTEXT_WAHL_KATALOGTAG", gewaehlterTag.Bezeichner ?? ""));
                        return new Bedarfstagwahl(null, true, ZapfSatz.Neu("AUSTEXT_WAHL_TAG_FEHLT"));
                }
            }
            if (gewaehlterTag != null)
                return new Bedarfstagwahl(gewaehlterTag.QuelleArt, false,
                    ZapfSatz.Neu("AUSTEXT_WAHL_KATALOGTAG", gewaehlterTag.Bezeichner ?? ""));
            if (wohnen && din4708Rechenbar)
                return new Bedarfstagwahl(ZapfBedarfstagquelle.Din4708Profil, false, ZapfSatz.Neu("AUSTEXT_WAHL_VORGABE_WOHNEN_DIN"));
            return new Bedarfstagwahl(null, true,
                ZapfSatz.Neu(wohnen ? "AUSTEXT_WAHL_VORGABE_WOHNEN_KONSTRUKTOR" : "AUSTEXT_WAHL_VORGABE_NICHTWOHNEN"));
        }
    }
}
