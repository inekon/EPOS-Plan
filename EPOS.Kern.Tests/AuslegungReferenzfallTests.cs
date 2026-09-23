using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der unabhängige Referenzfall der Auslegung</b> (Umsetzungskonzept Zapfprofilgenerator
    /// Kapitel 7, Zeile Z2; Muster des Referenzfalls der Stufe Z1).
    ///
    /// <para>Das Python-Skript <c>Proben/Zapfprofil/auslegung_referenzfall_bauen.py</c> rechnet aus
    /// <c>auslegung_referenzfall_eingabe.json</c> — drei Summenlinien (Ladespeicher mit Schätzformel
    /// des Übertragers, gemischter Speicher mit U·A, ein nicht monotoner Nachweis) und eine
    /// Speicherauslegung nach V4 samt DIN-4708-Kennzahl — nach den Formeln des Papiers, ohne den
    /// C#-Code, und legt jede Größe mit zwölf Nachkommastellen ab: Punkt, Ladezeit, Suchweg,
    /// Wertepaarkurve, Zeitkonstante und Speicherinhalt je Minute; Ladeleistung, D(t) je Stunde,
    /// D_max, Zeitpunkt, alle Volumina, Band, Nenninhalt, Füllstand. Dieser Test rechnet dieselbe
    /// Eingabe mit <see cref="Summenlinie"/> und <see cref="TwwSpeicherauslegung"/> und verlangt
    /// <b>Abweichung 0 auf 1e-9</b>: |C# − Referenz| ≤ ½ · 1e-9 + 1e-12 · |Referenz|. Alle Werte
    /// sind erfunden; keine Normzahl.</para>
    ///
    /// <para><b>Fassadenfall (N10):</b> dazu eine Gruppe aus zwei Zonen durch die ganze Fassade
    /// — Wochenreihe, f_KW,A, Wahl und Umrechnung des Bedarfstags, Laufzeitfenster.</para>
    /// </summary>
    public sealed class AuslegungReferenzfallTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Provenienz Fiktiv = new Provenienz("Referenzfall (fiktiv)", null, "REF-Z2", Herkunftsart.Fiktiv);

        [Fact]
        public void Die_Summenlinien_stimmen_auf_neun_Stellen()
        {
            Dictionary<string, double?> r = Referenz();
            using JsonDocument doc = Eingabe();
            int fall = 0, verglichen = 0;
            foreach (JsonElement f in doc.RootElement.GetProperty("summenlinie").EnumerateArray())
            {
                string n = "sl" + (++fall) + "_";
                Bedarfstag tag = Bedarfstag.AusEreignissen(ZapfBedarfstagquelle.Konstruktor, "Referenz " + fall,
                    f.GetProperty("ereignisse").EnumerateArray().Select(e =>
                        new Zapfereignis(e[0].GetInt32(), e[1].GetInt32(), e[2].GetDouble())).ToArray(), Fiktiv);
                int punkte = f.GetProperty("wertepaare").GetInt32();
                Summenlinienergebnis e = Summenlinie.Rechnen(tag, Parameter(f), punkte);

                verglichen += Gleich(r, n + "volumen_l", e.Punkt.VolumenL);
                verglichen += Gleich(r, n + "leistung_kw", e.Punkt.LeistungKw);
                verglichen += Gleich(r, n + "ladezeit_h", e.Punkt.LadezeitH);
                verglichen += Gleich(r, n + "suche", (int)e.Punkt.Suche);
                verglichen += Gleich(r, n + "kleinster_abstand_kwh", e.Nachweis.KleinsterAbstandKwh);
                if (r.ContainsKey(n + "zeitkonstante_min")) verglichen += Gleich(r, n + "zeitkonstante_min", e.ZeitkonstanteMin.Value);
                else Assert.Null(e.ZeitkonstanteMin);
                Assert.Equal(punkte, e.Wertepaare.Count);
                for (int k = 1; k <= punkte; k++)
                {
                    verglichen += Gleich(r, n + "wertepaar_" + k + "_leistung_kw", e.Wertepaare[k - 1].LeistungKw);
                    verglichen += Gleich(r, n + "wertepaar_" + k + "_volumen_l", e.Wertepaare[k - 1].VolumenL);
                    verglichen += Gleich(r, n + "wertepaar_" + k + "_ladezeit_h", e.Wertepaare[k - 1].LadezeitH);
                }
                Assert.Equal(1441, e.Nachweis.InhaltKwh.Count);
                for (int i = 0; i <= 1440; i++)
                    verglichen += Gleich(r, n + "inhalt_" + i.ToString("0000", CultureInfo.InvariantCulture), e.Nachweis.InhaltKwh[i]);
            }
            Assert.Equal(3, fall);
            Assert.True(verglichen > 4000, verglichen + " Größen verglichen.");
            // Der Referenzfall deckt beide Suchwege.
            Assert.Equal(1.0, r["sl1_suche"]);
            Assert.Equal(2.0, r["sl3_suche"]);
        }

        [Fact]
        public void Die_Speicherauslegung_stimmt_auf_neun_Stellen()
        {
            Dictionary<string, double?> r = Referenz();
            using JsonDocument doc = Eingabe();
            JsonElement s = doc.RootElement.GetProperty("speicherauslegung");
            JsonElement d = s.GetProperty("din");

            var werte = new Dictionary<string, double>
            {
                [ZapfAuslegungParameter.DIN4708_A1] = d.GetProperty("a1").GetDouble(),
                [ZapfAuslegungParameter.DIN4708_A2] = d.GetProperty("a2").GetDouble(),
                [ZapfAuslegungParameter.DIN4708_Z] = d.GetProperty("z").GetDouble(),
                [ZapfAuslegungParameter.DIN4708_PB] = d.GetProperty("p_b").GetDouble(),
                [ZapfAuslegungParameter.DIN4708_WB_ZAPFSTELLE] = d.GetProperty("w_b").GetDouble(),
                [ZapfAuslegungParameter.DIN4708_WB_BEDARF] = d.GetProperty("W_b").GetDouble(),
                [ZapfAuslegungParameter.DIN4708_KAPPUNG] = d.GetProperty("kappung").GetDouble(),
                [ZapfAuslegungParameter.KLASSISCH_LITER] = s.GetProperty("klassisch_liter").GetDouble(),
                [ZapfAuslegungParameter.KLASSISCH_SPREIZUNG] = s.GetProperty("klassisch_spreizung").GetDouble(),
                [ZapfAuslegungParameter.KLASSISCH_WARNFAKTOR] = s.GetProperty("klassisch_warnfaktor").GetDouble(),
                [ZapfAuslegungParameter.GLF_GUELTIGKEITSGRENZE] = s.GetProperty("glf_gueltigkeitsgrenze").GetDouble(),
                [ZapfAuslegungParameter.NENNINHALT_RASTER] = s.GetProperty("nenninhalt_raster").GetDouble(),
            };
            Parametersatz ps = Parametersatz.Aus("REF-Z2", werte.Select(kv => new ZapfParameterwert(kv.Key, kv.Value, "", Fiktiv)).ToArray());

            // Wohnungstabelle: Ausstattung als Katalogklasse, ohne Angabe die Einheitswohnung.
            var klassen = new List<Din4708Ausstattung>();
            var wohnungen = new List<WohnungstypStand>();
            foreach (JsonElement w in s.GetProperty("wohnungen").EnumerateArray())
            {
                int? id = null;
                if (w[2].ValueKind == JsonValueKind.Number)
                {
                    id = klassen.Count + 1;
                    klassen.Add(new Din4708Ausstattung(id.Value, "Klasse " + id, w[2].GetDouble()));
                }
                wohnungen.Add(new WohnungstypStand { Anzahl = w[0].GetInt32(), Personen = w[1].GetDouble(), IdAusstattung = id });
            }
            ZonenStand zone = ZapfprofilTestbau.Zone("Referenz") with { Wohnungen = wohnungen };
            double speicherC = s.GetProperty("speicher_c").GetDouble(), kw = s.GetProperty("kaltwasser_auslegung_c").GetDouble();
            double nutz = s.GetProperty("nutzanteil").GetDouble();
            Din4708Ergebnis din = Din4708Kennzahl.Rechnen(new[] { (zone, ZapfprofilTestbau.Art()) },
                new Din4708Katalog(null, klassen), ps, speicherC - kw, nutz);

            // Wochenreihe: Stundenwerte mal Tagesfaktor.
            var stunden = new double[24];
            foreach (JsonProperty p in s.GetProperty("stunden").EnumerateObject())
                stunden[int.Parse(p.Name, CultureInfo.InvariantCulture)] = p.Value.GetDouble();
            double[] faktoren = s.GetProperty("tagesfaktoren").EnumerateArray().Select(x => x.GetDouble()).ToArray();
            var reihe = new double[168];
            for (int k = 0; k < 7; k++) for (int h = 0; h < 24; h++) reihe[k * 24 + h] = stunden[h] * faktoren[k];
            Wochenreihe woche = Wochenreihe.Aus(reihe, s.GetProperty("erster_tag").GetInt32(), s.GetProperty("wochentag_erster_tag").GetInt32(),
                s.GetProperty("tagtypen").EnumerateArray().Select(x => (ZapfTagtyp)x.GetInt32()).ToArray());

            var e = new Speicherauslegungseingang
            {
                Woche = woche, SpeicherC = speicherC, KaltwasserAuslegungC = kw, Nutzanteil = nutz,
                Zuschlag = s.GetProperty("zuschlag").GetDouble(),
                Ladefenster = new Tagesfenster(s.GetProperty("ladefenster_beginn_h").GetDouble(), s.GetProperty("ladefenster_h").GetDouble()),
                LadeAuto = s.GetProperty("lade_auto").GetBoolean(),
                LadeManuellKw = s.GetProperty("lade_manuell_kw").ValueKind == JsonValueKind.Number ? s.GetProperty("lade_manuell_kw").GetDouble() : null,
                Zirkulation = new Schaetzwert(true, s.GetProperty("zirkulation_kw").GetDouble(), null),
                ZirkulationLaufzeit = new Tagesfenster(s.GetProperty("zirkulation_beginn_h").GetDouble(), s.GetProperty("zirkulation_laufzeit_h").GetDouble()),
                Din = din, Personen = din.Personen, Wohnen = true,
                Nenninhalte = Nenninhaltsliste.Aus(s.GetProperty("nenninhalte").EnumerateArray().Select(x => x.GetDouble()))
            };
            Speicherauslegungsergebnis a = TwwSpeicherauslegung.Rechnen(e, ps);

            int v = 0;
            v += Gleich(r, "sa_ladeleistung_kw", a.Ladeleistung.Angesetzt);
            v += Gleich(r, "sa_lade_vorschlag_kw", a.Ladeleistung.Vorschlag);
            v += Gleich(r, "sa_dmax_kwh", a.DmaxKwh);
            v += Gleich(r, "sa_zeitpunkt_stunde", a.ZeitpunktStunde.Value);
            v += Gleich(r, "sa_volumen_profil_l", a.VolumenProfilL.Value);
            v += Gleich(r, "sa_kennzahl_n", din.KennzahlN.Value);
            v += Gleich(r, "sa_personen", din.Personen.Value);
            v += Gleich(r, "sa_wz_kwh", din.WzKwh.Value);
            v += Gleich(r, "sa_volumen_din_l", a.VolumenDinL.Value);
            v += Gleich(r, "sa_glf", a.Gleichzeitigkeitsfaktor.Value);
            v += Gleich(r, "sa_volumen_glf_l", a.VolumenGlfL.Value);
            v += Gleich(r, "sa_volumen_klassisch_l", a.VolumenKlassischL.Value);
            v += Gleich(r, "sa_band_min_l", a.BandMinL.Value);
            v += Gleich(r, "sa_band_max_l", a.BandMaxL.Value);
            v += Gleich(r, "sa_nenninhalt_l", a.NenninhaltL.Value);
            v += Gleich(r, "sa_kapazitaet_kwh", a.KapazitaetKwh.Value);
            v += Gleich(r, "sa_min_fuellstand_kwh", a.MinFuellstandKwh.Value);
            v += Gleich(r, "sa_reserve", a.ReserveAnteil.Value);
            for (int t = 1; t <= 336; t++)
                v += Gleich(r, "sa_defizit_" + t.ToString("000", CultureInfo.InvariantCulture), a.DefizitKwh[t - 1]);
            Assert.Equal(18 + 336, v);
        }

        /// <summary>
        /// <b>Der Fassadenfall (N10)</b>: zwei Zonen am Durchfluss durch die ganze Fassade
        /// <see cref="ZapfprofilAuslegung.Rechnen"/> — Jahresenergie und f_KW,A je Zone, Zirkulation
        /// und Laufzeitfenster, die Wochenreihe der Auslegung (Fenster, Tagtypen, 168 Stunden,
        /// Summenkontrolle), die Wahl des Bedarfstags (gewählter Katalogtag), seine Skalierung und
        /// Umrechnung auf θ_KW,A und die Minutenwerte samt Spitze — gegen das Skript.
        /// </summary>
        [Fact]
        public void Die_Fassade_stimmt_auf_neun_Stellen()
        {
            Dictionary<string, double?> r = Referenz();
            using JsonDocument doc = Eingabe();
            JsonElement fa = doc.RootElement.GetProperty("fassade");

            Parametersatz ps = Parametersatz.Aus("REF-Z2", fa.GetProperty("parameter").EnumerateObject()
                .Select(p => new ZapfParameterwert(p.Name, p.Value.GetDouble(), "", Fiktiv)).ToArray());
            Assert.Equal(ZapfprofilTestbau.Bezug.ZapftemperaturC, fa.GetProperty("bezug_zapftemperatur_c").GetDouble());
            Assert.Equal(ZapfprofilTestbau.Bezug.KaltwasserC, fa.GetProperty("bezug_kaltwasser_c").GetDouble());

            JsonElement ts = fa.GetProperty("tagesgangsatz");
            Tagesgangsatz satz = ZapfprofilTestbau.Satz(1, Gang(ts.GetProperty("werktag")), Gang(ts.GetProperty("samstag")),
                                                        Gang(ts.GetProperty("sonntag")), Gang(ts.GetProperty("ruhetag")));
            var arten = new List<Nutzungsart>();
            foreach (JsonElement n in fa.GetProperty("nutzungsarten").EnumerateArray())
                arten.Add(ZapfprofilTestbau.Art(n.GetProperty("id").GetInt32(), (ZapfBezugsart)n.GetProperty("bezugsart").GetInt32(),
                    Zahlen(n.GetProperty("bedarf")), (ZapfBilanzgrenze)n.GetProperty("grenze").GetInt32(),
                    (ZapfKalenderart)n.GetProperty("kalenderart").GetInt32(), Zahl(n, "ferienfaktor"),
                    Zahlen(n.GetProperty("monate")), Zahlen(n.GetProperty("woche")), satz));

            var zonen = new List<ZonenStand>();
            foreach (JsonElement z in fa.GetProperty("zonen").EnumerateArray())
            {
                var beginn = new int?[4];
                var ende = new int?[4];
                int i = 0;
                foreach (JsonElement f in z.GetProperty("ferien").EnumerateArray())
                {
                    beginn[i] = f[0].GetInt32();
                    ende[i++] = f[1].GetInt32();
                }
                zonen.Add(new ZonenStand
                {
                    Id = zonen.Count + 1, Reihenfolge = zonen.Count + 1, Name = z.GetProperty("name").GetString(),
                    IdNutzungsart = z.GetProperty("nutzungsart").GetInt32(), Bezugsmenge = z.GetProperty("bezugsmenge").GetDouble(),
                    Niveau = (ZapfNiveau)z.GetProperty("niveau").GetInt32(), Zirkulation = z.GetProperty("zirkulation").GetBoolean(),
                    Topologie = ZapfTopologie.Durchfluss, Ferienbeginn = beginn, Ferienende = ende
                });
            }

            JsonElement pj = fa.GetProperty("projekt");
            ProjektStand projekt = ZapfprofilTestbau.Projekt() with
            {
                ZirkMethode = (ZapfZirkulationsmethode)pj.GetProperty("zirk_methode").GetInt32(),
                KaltwasserAuslegungC = Zahl(pj, "kaltwasser_auslegung_c"),
                IdBedarfstag = pj.GetProperty("id_bedarfstag").GetInt32()
            };
            int jan1 = fa.GetProperty("wochentag_jan1").GetInt32();
            var eingang = new Zapfprofileingang
            {
                Zonen = zonen, Projekt = projekt, WochentagJan1 = jan1, Parameter = ps,
                We = ZapfprofilTestbau.We(jan1, fa.GetProperty("feiertage").EnumerateArray().Select(x => x.GetInt32()).ToArray())
            };
            JsonElement t = fa.GetProperty("bedarfstag");
            var katalogtag = new BedarfstagKatalogzeile(t.GetProperty("id").GetInt32(), t.GetProperty("bezeichner").GetString(), "REF-Z2",
                (ZapfBedarfstagquelle)t.GetProperty("quelle_art").GetInt32(), t.GetProperty("bezugsmenge").GetDouble(), Fiktiv,
                t.GetProperty("ereignisse").EnumerateArray().Select(e => new Zapfereignis(e[0].GetInt32(), e[1].GetInt32(), e[2].GetDouble())).ToArray());

            Auslegungsergebnis a = ZapfprofilAuslegung.Rechnen(eingang, arten, new Auslegungseingang { Bedarfstage = new[] { katalogtag } });
            Assert.Empty(a.Ablehnungen);
            Auslegungsgruppe g = Assert.Single(a.Gruppen);
            Assert.Equal(ZapfTopologie.Durchfluss, g.Topologie);
            double Herkunft(string zone, string feld) => a.Herkunft.Last(x => x.Zone == zone && x.Feld == feld).Wert.Value;

            int v = 0;
            for (int i = 1; i <= zonen.Count; i++)
            {
                v += Gleich(r, "fa_zone_" + i + "_jahresenergie_kwh", Herkunft(zonen[i - 1].Name, ZapfFeld.JAHRESENERGIE));
                v += Gleich(r, "fa_zone_" + i + "_fkwa", Herkunft(zonen[i - 1].Name, "Auslegung.Kaltwasserfaktor"));
            }
            v += Gleich(r, "fa_zirkulation_leistung_kw", Herkunft("", ZapfFeld.ZIRKULATION_LEISTUNG));
            v += Gleich(r, "fa_laufzeit_beginn_h", g.ZirkulationLaufzeit.BeginnH);
            v += Gleich(r, "fa_laufzeit_h", g.ZirkulationLaufzeit.LaengeH);
            v += Gleich(r, "fa_woche_erster_tag", g.Woche.ErsterTag);
            v += Gleich(r, "fa_woche_wochentag_erster_tag", g.Woche.WochentagErsterTag);
            for (int k = 1; k <= 7; k++) v += Gleich(r, "fa_woche_tagtyp_" + k, (int)g.Woche.Tagtypen[k - 1]);
            v += Gleich(r, "fa_woche_summe_kwh", g.Woche.WochensummeKwh);
            v += Gleich(r, "fa_woche_fenstersumme_kwh", g.Woche.FenstersummeKwh.Value);
            Assert.True(g.Woche.SummenkontrolleErfuellt);
            for (int h = 1; h <= 168; h++)
                v += Gleich(r, "fa_woche_" + h.ToString("000", CultureInfo.InvariantCulture), g.Woche.StundenKwh[h - 1]);

            // Wahl des Bedarfstags: ohne ausdrückliche Quelle der gewählte Katalogtag (Vorgaberegel 4.5).
            Assert.Equal(katalogtag.QuelleArt, g.Bedarfstagwahl.Quelle);
            v += Gleich(r, "fa_bedarfstag_quelle", (int)g.Bedarfstag.Quelle);
            v += Gleich(r, "fa_bedarfstag_faktor", Herkunft("", "Auslegung.Bedarfstagfaktor"));
            v += Gleich(r, "fa_bedarfstag_summe_kwh", g.Bedarfstag.TagessummeKwh);
            v += Gleich(r, "fa_minutenspitze_kw", g.Bedarfstag.GroessteMinutenleistungKw);
            v += Gleich(r, "fa_stundenspitze_kw", g.Bedarfstag.GroessteStundenleistungKw);
            for (int i = 0; i < 1440; i++)
                v += Gleich(r, "fa_tag_" + i.ToString("0000", CultureInfo.InvariantCulture), g.Bedarfstag.MinutenKwh[i]);
            // Die eine Empfehlung am Durchfluss ist die Minutenspitze.
            Assert.Equal(g.Bedarfstag.GroessteMinutenleistungKw, g.Empfehlung.LeistungKw);
            Assert.Equal(r.Keys.Count(k => k.StartsWith("fa_", StringComparison.Ordinal)), v);
        }

        private static double[] Gang(JsonElement e)
        {
            var a = new double[24];
            foreach (JsonProperty p in e.EnumerateObject()) a[int.Parse(p.Name, CultureInfo.InvariantCulture)] = p.Value.GetDouble();
            return a;
        }

        private static double[] Zahlen(JsonElement e) => e.EnumerateArray().Select(x => x.GetDouble()).ToArray();

        // =================================================================================
        // Eingabe und Erwartung
        // =================================================================================

        private static Summenlinienparameter Parameter(JsonElement f)
        {
            Uebertrager ue = null;
            if (f.GetProperty("uebertrager").ValueKind == JsonValueKind.Object)
            {
                JsonElement u = f.GetProperty("uebertrager");
                ue = new Uebertrager(Zahl(u, "leistung_kw"), Zahl(u, "ua_w_k"), Zahl(u, "flaeche_m2"), Zahl(u, "u"),
                                     Zahl(u, "uebertemperatur_k"), Zahl(u, "steigung"), Zahl(u, "achsabschnitt"));
            }
            return new Summenlinienparameter
            {
                KaltwasserAuslegungC = f.GetProperty("kaltwasser_auslegung_c").GetDouble(),
                SpeicherC = f.GetProperty("speicher_c").GetDouble(),
                Ladungsfaktor = f.GetProperty("ladungsfaktor").GetDouble(),
                SensorhoeheAnteil = f.GetProperty("sensorhoehe").GetDouble(),
                Speicherart = (ZapfSpeicherart)f.GetProperty("speicherart").GetInt32(),
                MischwasserC = Zahl(f, "mischwasser_c"),
                VerzoegerungMin = f.GetProperty("verzoegerung_min").GetDouble(),
                SpeicherverlustKw = f.GetProperty("speicherverlust_kw").GetDouble(),
                Zirkulation = new Zirkulationslast(f.GetProperty("zirkulation_kw").GetDouble(),
                    new Tagesfenster(f.GetProperty("zirkulation_beginn_h").GetDouble(), f.GetProperty("zirkulation_laufzeit_h").GetDouble())),
                ErzeugerKw = Zahl(f, "erzeuger_kw"),
                Uebertrager = ue,
                ZeitkonstanteKoeffizient = Zahl(f, "zeitkonstante_koeffizient")
            };
        }

        private static double? Zahl(JsonElement e, string name)
            => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

        /// <summary>Abweichung 0 auf 1e-9: |C# − Referenz| ≤ ½ · 1e-9 + 1e-12 · |Referenz|.</summary>
        private static int Gleich(Dictionary<string, double?> r, string name, double ist)
        {
            Assert.True(r.TryGetValue(name, out double? soll) && soll.HasValue, "Die Referenz nennt „" + name + "“ nicht.");
            double rand = 0.5e-9 + 1e-12 * Math.Abs(soll.Value);
            Assert.True(Math.Abs(ist - soll.Value) <= rand,
                name + ": C# " + ist.ToString("R", CultureInfo.InvariantCulture) + ", Referenz "
                + soll.Value.ToString("R", CultureInfo.InvariantCulture));
            return 1;
        }

        private static string Ordner()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern.Tests", "Proben", "Zapfprofil");
                if (Directory.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Der Probenordner EPOS.Kern.Tests/Proben/Zapfprofil wurde nicht gefunden.");
            return null;
        }

        private static JsonDocument Eingabe()
            => JsonDocument.Parse(File.ReadAllText(Path.Combine(Ordner(), "auslegung_referenzfall_eingabe.json")));

        private static Dictionary<string, double?> Referenz()
        {
            var r = new Dictionary<string, double?>(StringComparer.Ordinal);
            foreach (string z in File.ReadAllLines(Path.Combine(Ordner(), "auslegung_referenzfall_ergebnis.csv")))
            {
                if (z.StartsWith("#", StringComparison.Ordinal) || z.StartsWith("groesse", StringComparison.Ordinal)) continue;
                string[] t = z.Split(',');
                r[t[0]] = t[1].Length == 0 ? null : double.Parse(t[1], CultureInfo.InvariantCulture);
            }
            return r;
        }
    }
}
