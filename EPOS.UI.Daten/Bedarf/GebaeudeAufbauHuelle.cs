using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Bauteilaufbau als Daten der Oberfläche</b> (Konzept Bauteilaufbau beim Import 5.4, BA-3) — plattformfrei, ohne
    /// Datenbank: die Zuordnungsstufe je Bauteilkennung (Farbmodus „Aufbau"), die Legendensummen außen/innen, der
    /// Bauteilsteckbrief je Bauteil und die Liste „Bauteilaufbauten" des Imports. Zwei Quellen: der Importvorschlag
    /// (<see cref="GebaeudeBauteilvorschlag"/>) und die gespeicherten Bauteile samt Aufbauten (Datei erneut lesen). R₁, C₁ und
    /// die Kapazität rechnet allein die <see cref="Bauteilreduktion"/> des Kerns, nur für die Anzeige — die Rechnung bleibt
    /// unberührt, geschrieben wird nichts.
    /// </summary>
    internal static class GebaeudeAufbauHuelle
    {
        // ------------------------------------------------------------------
        //  Stufe und Herkunft
        // ------------------------------------------------------------------

        /// <summary>Die Stufe des Kerns als Wert der Oberfläche.</summary>
        internal static Aufbaustufe Stufe(Bauteilzuordnungsstufe s) => s switch
        {
            Bauteilzuordnungsstufe.A => Aufbaustufe.A,
            Bauteilzuordnungsstufe.B => Aufbaustufe.B,
            Bauteilzuordnungsstufe.C => Aufbaustufe.C,
            _ => Aufbaustufe.Transparent,
        };

        /// <summary>Der Befund des Kerns als Wert der Oberfläche (Abstimmung G5, B1).</summary>
        internal static Bauteilbefundstufe Befund(Bauteilbefund b) => b switch
        {
            Bauteilbefund.KoerperUnlesbar => Bauteilbefundstufe.KoerperUnlesbar,
            Bauteilbefund.OhneEigenschaften => Bauteilbefundstufe.OhneEigenschaften,
            _ => Bauteilbefundstufe.Ohne,
        };

        /// <summary>
        /// Die Herkunft der Fläche als kurzer Anzeigetext (Mengensatz, Raumgrenze, Körper, schematisch) aus dem gespeicherten
        /// Wert (<see cref="FlaechenherkunftWerte"/>); leer = von Hand bzw. nicht bestimmt, ein unbekannter Wert bleibt stehen.
        /// </summary>
        internal static string FlaechenherkunftText(string wert) => wert switch
        {
            null or "" => "",
            FlaechenherkunftWerte.MENGENSATZ => MyResource.Resource.GIMP_FLHK_MENGENSATZ,
            FlaechenherkunftWerte.RAUMGRENZE => MyResource.Resource.GIMP_FLHK_RAUMGRENZE,
            FlaechenherkunftWerte.KOERPER => MyResource.Resource.GIMP_FLHK_KOERPER,
            FlaechenherkunftWerte.SCHEMATISCH => MyResource.Resource.GIMP_FLHK_SCHEMATISCH,
            _ => wert,
        };

        /// <summary>Trägt Befund und Grundtext in einen Steckbrief.</summary>
        internal static BauteilsteckbriefDaten MitBefund(BauteilsteckbriefDaten d, Bauteilbefundgrund g)
            => d with { Befund = Befund(Bauteilbefunde.Befund(g)), Befundgrund = Bauteilbefunde.Text(g) };

        /// <summary>Zahl und Fläche je Befund (ohne, Körper unlesbar, ohne Eigenschaften, ohne Bauteil) — die Legende „Befund".</summary>
        internal static IReadOnlyList<GebaeudeAnsichtBefundsumme> Befundsummen(IEnumerable<(Bauteilbefundstufe Stufe, double Flaeche)> zeilen)
        {
            var liste = zeilen.ToList();
            return Enumerable.Range(0, GebaeudeAnsichtBefundstufen.ZAHL).Select(i =>
            {
                var s = (Bauteilbefundstufe)i;
                var je = liste.Where(x => x.Stufe == s).ToList();
                return new GebaeudeAnsichtBefundsumme(s, je.Count, je.Sum(x => x.Flaeche));
            }).ToList();
        }

        /// <summary>Der Schlüssel einer Herkunft des Imports; „Projektdatei" (BA-4) wird über den Namen erkannt.</summary>
        internal static string Herkunftsschluessel(Importherkunft h) => h switch
        {
            Importherkunft.Ifc or Importherkunft.GbXml => SteckbriefHerkunft.Datei,
            Importherkunft.Sqproj => SteckbriefHerkunft.Projektdatei,
            Importherkunft.Katalog => SteckbriefHerkunft.Katalog,
            Importherkunft.Vorgabe or Importherkunft.VorgabeFrei => SteckbriefHerkunft.Vorgabe,
            Importherkunft.Manuell => SteckbriefHerkunft.Manuell,
            _ => string.Equals(h.ToString(), "Projektdatei", StringComparison.Ordinal) ? SteckbriefHerkunft.Projektdatei : SteckbriefHerkunft.Leer,
        };

        /// <summary>Der Schlüssel eines gespeicherten Herkunftswerts (<c>IFC</c>, <c>GBXML</c>, <c>KATALOG</c>, …).</summary>
        internal static string Herkunftsschluessel(string wert) => wert switch
        {
            DbWerte.HERKUNFT_VORGABE => SteckbriefHerkunft.Vorgabe,
            DbWerte.HERKUNFT_KATALOG => SteckbriefHerkunft.Katalog,
            ImportherkunftWerte.IFC or ImportherkunftWerte.GBXML => SteckbriefHerkunft.Datei,
            null or "" => SteckbriefHerkunft.Leer,
            _ => wert.IndexOf("PROJEKTDATEI", StringComparison.OrdinalIgnoreCase) >= 0 || wert.IndexOf("SQPROJ", StringComparison.OrdinalIgnoreCase) >= 0
                ? SteckbriefHerkunft.Projektdatei : SteckbriefHerkunft.Manuell,
        };

        /// <summary>Der Anzeigetext eines Herkunftsschlüssels.</summary>
        internal static string Herkunftstext(string schluessel) => schluessel switch
        {
            SteckbriefHerkunft.Datei => MyResource.Resource.BTSB_HK_DATEI,
            SteckbriefHerkunft.Schichten => MyResource.Resource.BTSB_HK_SCHICHTEN,
            SteckbriefHerkunft.Vorgabe => MyResource.Resource.BTSB_HK_VORGABE,
            SteckbriefHerkunft.Projektdatei => MyResource.Resource.BTSB_HK_PROJEKTDATEI,
            SteckbriefHerkunft.Katalog => MyResource.Resource.BTSB_HK_KATALOG,
            SteckbriefHerkunft.Manuell => MyResource.Resource.BTSB_HK_MANUELL,
            _ => MyResource.Resource.GIMP_WERT_LEER,
        };

        /// <summary>
        /// Die Herkunft eines Aufbaus des Vorschlags: Ersatzaufbau = Vorgabe; ein Aufbau der HottCAD-Projektdatei (BA-4b:
        /// <see cref="GebaeudeAufbauzeile.AusProjektdatei"/>, Quelltyp <see cref="GebaeudeBauteilvorschlag.QUELLTYP_PD_AUFBAU"/>)
        /// = Projektdatei; sonst Katalog bzw. Datei.
        /// </summary>
        internal static string Aufbauherkunft(GebaeudeAufbauzeile a)
        {
            if (a == null) return SteckbriefHerkunft.Leer;
            if (a.Ersatz != null) return SteckbriefHerkunft.Vorgabe;
            if (a.AusProjektdatei || string.Equals(a.Quelltyp, GebaeudeBauteilvorschlag.QUELLTYP_PD_AUFBAU, StringComparison.Ordinal))
                return SteckbriefHerkunft.Projektdatei;
            return a.Herkunft == Importherkunft.Katalog ? SteckbriefHerkunft.Katalog : SteckbriefHerkunft.Datei;
        }

        /// <summary>
        /// Die Herkunft eines gespeicherten Aufbaus: Typaufbau = Vorgabe; ein Aufbau der Projektdatei trägt deren Dateinamen
        /// (<c>*.sqproj</c>) als Quelle (BA-4b) = Projektdatei; sonst nach dem Herkunftswert.
        /// </summary>
        internal static string Aufbauherkunft(BauteilaufbauModel a)
        {
            if (a == null) return SteckbriefHerkunft.Leer;
            if (!string.IsNullOrEmpty(a.Typaufbau)) return SteckbriefHerkunft.Vorgabe;
            if (a.Quelle != null && a.Quelle.Trim().EndsWith(".sqproj", StringComparison.OrdinalIgnoreCase))
                return SteckbriefHerkunft.Projektdatei;
            return Herkunftsschluessel(a.Herkunft);
        }

        /// <summary>
        /// Der Rang des Aufbaus nach der Rangfolge E98 (BA-4b) als Anzeigetext; leer ohne Rang oder wenn der Lauf keine
        /// Projektdatei betrachtet hat (<paramref name="mitProjektdatei"/>) — dann ist Rang 3/4 keine Auskunft.
        /// </summary>
        internal static string Rangtext(Aufbaurang r, bool mitProjektdatei) => !mitProjektdatei && r != Aufbaurang.Projektdatei && r != Aufbaurang.Projektkatalog
            ? ""
            : r switch
            {
                Aufbaurang.Projektdatei => MyResource.Resource.BTSB_RANG_1,
                Aufbaurang.Projektkatalog => MyResource.Resource.BTSB_RANG_2,
                Aufbaurang.IfcSchichten => MyResource.Resource.BTSB_RANG_3,
                Aufbaurang.Ersatz => MyResource.Resource.BTSB_RANG_4,
                _ => "",
            };

        // ------------------------------------------------------------------
        //  Kennwerte für die Anzeige (Bauteilreduktion)
        // ------------------------------------------------------------------

        /// <summary>R₁ [K/W], C₁ [J/K] (VDI 6007, Bezugsperiode nach Gl. (10)), C₁,korr je m² [J/(m²K)] und Σ d·ρ·c [J/(m²K)].</summary>
        internal readonly record struct Anzeigekennwerte(double R1_KW, double C1_Jk, double C1korrJeM2_JM2K, double Kapazitaet_JM2K, double USchichten_WM2K);

        /// <summary>
        /// Die Kennwerte eines Aufbaus für die Anzeige über <see cref="Bauteilreduktion"/> — oder der Grund, warum nicht (Schicht
        /// ohne Wert, Reduktion ungültig). Die Fläche geht als Bauteilfläche ein (R₁, C₁ des Bauteils), die Neigung gibt Richtung
        /// und Übergänge, der Rand die äußeren Übergänge.
        /// </summary>
        internal static Anzeigekennwerte? Kennwerte(BauteilaufbauModel aufbau, double flaeche, double neigung, Bauteilrand rand, out string grund)
        {
            grund = "";
            if (aufbau == null || aufbau.Schichten == null || aufbau.Schichten.Count == 0)
            {
                grund = MyResource.Resource.BTSB_OHNE_RECHNUNG;
                return null;
            }
            var schichten = new List<Schicht>();
            int nummer = 0;
            foreach (BauteilschichtModel m in aufbau.Schichten)
            {
                if (!BauteilaufbauCtrl.SchichtAusModell(m, ++nummer, out Schicht s, out string fehlt))
                {
                    grund = fehlt ?? MyResource.Resource.BTSB_OHNE_RECHNUNG;
                    return null;
                }
                schichten.Add(s);
            }
            try
            {
                Waermestromrichtung richtung = Bauteilreduktion.RichtungAusNeigung(neigung);
                Schichtkennwerte k = Bauteilreduktion.UWertAusSchichten(schichten, neigung, rand);
                Bezugsperiodenwahl wahl = Bauteilreduktion.BezugsperiodeWaehlen(schichten, flaeche > 0.0 ? flaeche : 1.0, richtung);
                Bezugsperiodenwahl jeM2 = Bauteilreduktion.BezugsperiodeWaehlen(schichten, 1.0, richtung);
                return new Anzeigekennwerte(wahl.Kennwerte.R1_KW, wahl.Kennwerte.C1_Jk, jeM2.Kennwerte.C1korr_Jk, k.Kapazitaet_JM2K, k.U_WM2K);
            }
            catch (GebaeudeModellException ex)
            {
                grund = ex.Message;
                return null;
            }
        }

        // ------------------------------------------------------------------
        //  Importvorschlag
        // ------------------------------------------------------------------

        /// <summary>
        /// Trägt die Stufen, die Legendensummen und die Steckbriefe des Vorschlags in die Daten der Ansicht, dazu den Befund je
        /// Bauteil und seine Legende (G5-3, Farbmodus „Befund" — samt Körperbefund des Imports); ohne Vorschlag oder ohne Zeilen
        /// bleiben die Daten unverändert (dann sind „Aufbau" und „Befund" gesperrt).
        /// </summary>
        internal static GebaeudeAnsichtDaten MitAufbau(GebaeudeAnsichtDaten daten, GebaeudeBauteilvorschlag v)
        {
            if (daten == null || v == null || v.Zeilen.Count == 0) return daten;
            var aufbauJeId = v.Aufbauten.GroupBy(a => a.Aufbau.ID).ToDictionary(g => g.Key, g => g.First());
            var stufen = new Dictionary<string, Aufbaustufe>(StringComparer.Ordinal);
            var befunde = new Dictionary<string, Bauteilbefundstufe>(StringComparer.Ordinal);
            var steckbriefe = new Dictionary<string, BauteilsteckbriefDaten>(StringComparer.Ordinal);
            foreach (GebaeudeBauteilzeile z in v.Zeilen)
            {
                if (string.IsNullOrEmpty(z.Kennung) || stufen.ContainsKey(z.Kennung)) continue;
                stufen[z.Kennung] = Stufe(z.Stufe);
                befunde[z.Kennung] = Befund(z.Befund);
                GebaeudeAufbauzeile a = z.Bauteil.ID_Aufbau is int id && aufbauJeId.TryGetValue(id, out GebaeudeAufbauzeile x) ? x : null;
                steckbriefe[z.Kennung] = Steckbrief(z, a, v.MitProjektdatei);
            }
            return daten with
            {
                Bauteilstufen = stufen,
                Steckbriefe = steckbriefe,
                Aufbausummen = Summen(v.Zeilen.Select(z => (Stufe(z.Stufe), z.Summenfeld == null, z.Bauteil.Flaeche))),
                Bauteilbefunde = befunde,
                Befundsummen = Befundsummen(v.Zeilen.Select(z => (Befund(z.Befund), z.Bauteil.Flaeche))),
            };
        }

        /// <summary>Zahl und Fläche je Stufe (A, B, C, transparent, ohne Bauteil), außen und innen getrennt.</summary>
        internal static IReadOnlyList<GebaeudeAnsichtAufbausumme> Summen(IEnumerable<(Aufbaustufe Stufe, bool Innen, double Flaeche)> zeilen)
        {
            var liste = zeilen.ToList();
            return Enumerable.Range(0, GebaeudeAnsichtAufbaustufen.ZAHL).Select(i =>
            {
                var s = (Aufbaustufe)i;
                var aussen = liste.Where(x => x.Stufe == s && !x.Innen).ToList();
                var innen = liste.Where(x => x.Stufe == s && x.Innen).ToList();
                return new GebaeudeAnsichtAufbausumme(s, aussen.Count, aussen.Sum(x => x.Flaeche), innen.Count, innen.Sum(x => x.Flaeche));
            }).ToList();
        }

        /// <summary>
        /// Der Steckbrief einer Zeile des Vorschlags mit ihrem Aufbau (<c>null</c> = keiner); mit
        /// <paramref name="mitProjektdatei"/> trägt er den Rang der Zeile (E98).
        /// </summary>
        internal static BauteilsteckbriefDaten Steckbrief(GebaeudeBauteilzeile z, GebaeudeAufbauzeile a, bool mitProjektdatei = false)
            => MitBefund(SteckbriefOhneBefund(z, a, mitProjektdatei), z.Befundgrund);

        private static BauteilsteckbriefDaten SteckbriefOhneBefund(GebaeudeBauteilzeile z, GebaeudeAufbauzeile a, bool mitProjektdatei)
        {
            BauteilModel b = z.Bauteil;
            Aufbaustufe stufe = Stufe(z.Stufe);
            bool transparent = stufe == Aufbaustufe.Transparent || string.Equals(b.Bauteilart, DbWerte.BAUTEILART_TUER, StringComparison.Ordinal);
            string uSchluessel = b.U_Wert.HasValue
                ? (z.HerkunftU == Importherkunft.Leer ? SteckbriefHerkunft.Datei : Herkunftsschluessel(z.HerkunftU))
                : z.USchichten.HasValue ? SteckbriefHerkunft.Schichten : SteckbriefHerkunft.Leer;
            double? u = b.U_Wert ?? z.USchichten;
            var d = Kopf(b, z.Kennung, stufe, transparent) with
            {
                UWert = UText(u),
                UHerkunftSchluessel = uSchluessel,
                UHerkunft = Herkunftstext(uSchluessel),
                UHinweis = z.UAbweichungHinweis && z.UDatei is double ud && z.USchichten is double us
                    ? Format(MyResource.Resource.BTSB_U_ABWEICHUNG, UText(ud), Zahl(Math.Round(100.0 * Math.Abs(ud / us - 1.0), 0)), UText(us)) : "",
                Fehlt = LueckenText(z.Fehlt),
                Aufbaurang = transparent ? "" : Rangtext(z.Aufbaurang, mitProjektdatei),
            };
            if (transparent || a == null) return d with { Rechengrund = transparent ? "" : MyResource.Resource.BTSB_OHNE_RECHNUNG };

            string aufbauSchluessel = Aufbauherkunft(a);
            var schichten = new List<BauteilsteckbriefSchicht>();
            for (int i = 0; i < a.Aufbau.Schichten.Count; i++)
            {
                BauteilschichtModel s = a.Aufbau.Schichten[i];
                string name = i < a.Schichtnamen.Count ? a.Schichtnamen[i] : "";
                if (name.Length == 0 && a.Ersatz != null && i < a.Ersatz.Typ.Schichten.Count) name = a.Ersatz.Typ.Schichten[i].Baustoff.Bezeichner;
                string hk = a.Ersatz != null ? SteckbriefHerkunft.Vorgabe
                    : i < a.Stammbaustoffe.Count && a.Stammbaustoffe[i].HasValue ? SteckbriefHerkunft.Katalog : aufbauSchluessel;
                schichten.Add(Schichtzeile(s, name, hk) with
                {
                    Materialschluessel = a.Ersatz == null && name.Length > 0 ? Baustoffabgleich.Schluessel(name) : null,
                });
            }
            foreach (GebaeudeWeggelasseneSchicht w in a.Weggelassen)
                schichten.Add(new BauteilsteckbriefSchicht(w.Name, DickeText(w.Dicke_M), "–", "–", "–", Herkunftstext(aufbauSchluessel), aufbauSchluessel)
                {
                    Weggelassen = true,
                    Grund = w.Grund == Schichtrelevanzgrund.Sperre ? MyResource.Resource.BTSB_GRUND_SPERRE : MyResource.Resource.BTSB_GRUND_DUENN,
                });
            return MitKennwerte(d with
            {
                Aufbau = a.Aufbau.Bezeichner ?? "",
                AufbauHerkunftSchluessel = aufbauSchluessel,
                AufbauHerkunft = Herkunftstext(aufbauSchluessel),
                Schichten = schichten,
                Ersatz = a.Ersatz != null ? Ersatztext(a.Ersatz) : "",
            }, b, a.Aufbau);
        }

        /// <summary>Der Ersatzaufbau als Text: Typ und abgeglichener Wert (Dämmdicke in cm bzw. λ).</summary>
        internal static string Ersatztext(Ersatzergebnis e)
        {
            if (e.Wert is double w && e.Abgleich == Typabgleich.Daemmdicke)
                return Format(MyResource.Resource.BTSB_ERSATZ_DICKE, e.Typ.Bezeichner, Zahl(Math.Round(100.0 * w, 1)));
            if (e.Wert is double l)
                return Format(MyResource.Resource.BTSB_ERSATZ_LAMBDA, e.Typ.Bezeichner, Zahl(Math.Round(l, 3)));
            return Format(MyResource.Resource.BTSB_ERSATZ_OHNE, e.Typ.Bezeichner);
        }

        /// <summary>Was einem Aufbau fehlt, als Aufzählung; leer ohne Lücke (der Vermerk „Ersatzaufbau" ist keine Lücke).</summary>
        internal static string LueckenText(Aufbauluecke l)
        {
            var teile = new List<string>();
            if ((l & Aufbauluecke.KeineSchichten) != 0) teile.Add(MyResource.Resource.BTSB_LUECKE_SCHICHTEN);
            if ((l & Aufbauluecke.Dicke) != 0) teile.Add(MyResource.Resource.BTSB_LUECKE_DICKE);
            if ((l & Aufbauluecke.Lambda) != 0) teile.Add(MyResource.Resource.BTSB_LUECKE_LAMBDA);
            if ((l & Aufbauluecke.Rohdichte) != 0) teile.Add(MyResource.Resource.BTSB_LUECKE_RHO);
            if ((l & Aufbauluecke.Waermekapazitaet) != 0) teile.Add(MyResource.Resource.BTSB_LUECKE_CP);
            if ((l & Aufbauluecke.Daemmung) != 0) teile.Add(MyResource.Resource.BTSB_LUECKE_DAEMMUNG);
            if ((l & Aufbauluecke.SpeicherndeSchicht) != 0) teile.Add(MyResource.Resource.BTSB_LUECKE_SPEICHER);
            return string.Join(", ", teile);
        }

        // ------------------------------------------------------------------
        //  Liste „Bauteilaufbauten" des Imports
        // ------------------------------------------------------------------

        /// <summary>
        /// Die Liste je Aufbau (nicht je Bauteil): Stufe, Art, Zahl und Fläche der Bauteile, U der Datei, U aus den Schichten,
        /// C₁,korr je m² (Anzeige über <see cref="Bauteilreduktion"/>), was fehlt, der Ersatztyp und die Schichten. Bauteile
        /// ohne Aufbau stehen je Bauteilart und Stufe in einer Zeile „ohne Aufbau". Transparente Bauteile fehlen.
        /// </summary>
        internal static IReadOnlyList<GebaeudeAufbaulistenzeileDaten> Aufbauliste(GebaeudeBauteilvorschlag v)
        {
            if (v == null) return Array.Empty<GebaeudeAufbaulistenzeileDaten>();
            var aufbauJeId = v.Aufbauten.GroupBy(a => a.Aufbau.ID).ToDictionary(g => g.Key, g => g.First());
            var liste = new List<GebaeudeAufbaulistenzeileDaten>();
            foreach (var gruppe in v.Zeilen.Where(z => z.Stufe != Bauteilzuordnungsstufe.Transparent)
                                          .GroupBy(z => z.Bauteil.ID_Aufbau is int id && aufbauJeId.ContainsKey(id)
                                                        ? "A" + id.ToString(CultureInfo.InvariantCulture)
                                                        : "O" + z.Bauteil.Bauteilart + "|" + Stufe(z.Stufe)))
            {
                List<GebaeudeBauteilzeile> zeilen = gruppe.ToList();
                GebaeudeBauteilzeile erste = zeilen[0];
                GebaeudeAufbauzeile a = erste.Bauteil.ID_Aufbau is int id && aufbauJeId.TryGetValue(id, out GebaeudeAufbauzeile x) ? x : null;
                Aufbaustufe stufe = zeilen.Select(z => Stufe(z.Stufe)).Max();
                double? uDatei = zeilen.Select(z => z.UDatei).FirstOrDefault(u => u.HasValue);
                double? uSchichten = zeilen.Select(z => z.USchichten).FirstOrDefault(u => u.HasValue);
                string c1 = "";
                if (a != null)
                {
                    double neigung = erste.Bauteil.Neigung ?? 90.0;
                    Bauteilrand rand = GebaeudeZonenabbildung.RandAusZeile(erste.Bauteil.Bauteilart, erste.Bauteil.Randbedingung) ?? Bauteilrand.Aussenluft;
                    if (Kennwerte(a.Aufbau, 1.0, neigung, rand, out _) is { } k)
                    {
                        c1 = Zahl(Math.Round(k.C1korrJeM2_JM2K / 1000.0, 1));
                        uSchichten ??= k.USchichten_WM2K;
                    }
                }
                Aufbauluecke fehlt = zeilen.Aggregate(Aufbauluecke.Keine, (s, z) => s | z.Fehlt) & ~Aufbauluecke.Ersatzaufbau;
                BauteilsteckbriefDaten steckbrief = Steckbrief(erste, a, v.MitProjektdatei);
                liste.Add(new GebaeudeAufbaulistenzeileDaten
                {
                    Schluessel = gruppe.Key,
                    Aufbau = a?.Aufbau.Bezeichner ?? MyResource.Resource.GIMP_AB_OHNE_AUFBAU,
                    Art = BauteilaufbauCtrl.BauteilartText(erste.Bauteil.Bauteilart),
                    Stufe = stufe,
                    Bauteile = zeilen.Count,
                    Flaeche = Zahl(Math.Round(zeilen.Sum(z => z.Bauteil.Flaeche), 1)),
                    UDatei = uDatei is double ud ? Zahl(Math.Round(ud, 3)) : "",
                    USchichten = uSchichten is double us ? Zahl(Math.Round(us, 3)) : "",
                    C1korr = c1,
                    Fehlt = LueckenText(fehlt),
                    Typaufbau = a?.Ersatz != null ? Format(MyResource.Resource.GIMP_AB_TYP, Ersatztext(a.Ersatz)) : "",
                    AufbauHerkunftSchluessel = steckbrief.AufbauHerkunftSchluessel,
                    Aufbaurang = steckbrief.Aufbaurang,
                    Schichten = steckbrief.Schichten,
                    Materialschluessel = steckbrief.Schichten.Where(s => !s.Weggelassen && !string.IsNullOrEmpty(s.Materialschluessel)
                                                                         && (s.Lambda == "–" || s.Rohdichte == "–" || s.Cp == "–"))
                                                              .Select(s => s.Materialschluessel).FirstOrDefault()
                                         ?? steckbrief.Schichten.Where(s => !s.Weggelassen).Select(s => s.Materialschluessel)
                                                                .FirstOrDefault(m => !string.IsNullOrEmpty(m)),
                    Typschluessel = a?.Ersatzschluessel,
                    Typcode = a?.Ersatz?.Typ.Code ?? "",
                    Typen = a?.Ersatz != null ? Typen(a.Ersatz.Typ.Code) : Array.Empty<GebaeudeZonenregelDaten>(),
                });
            }
            return liste.OrderBy(z => z.Stufe == Aufbaustufe.A ? 1 : 0).ThenByDescending(z => z.Stufe).ThenBy(z => z.Art, StringComparer.CurrentCulture)
                        .ThenBy(z => z.Aufbau, StringComparer.CurrentCulture).ToList();
        }

        /// <summary>Die Typaufbauten derselben Familie (Außenwand, Dach, Boden — Vorsatz des Codes) zur Wahl (E95-4).</summary>
        internal static IReadOnlyList<GebaeudeZonenregelDaten> Typen(string code)
        {
            int strich = (code ?? "").IndexOf('_');
            string familie = strich > 0 ? code.Substring(0, strich + 1) : code ?? "";
            return TypaufbauSaattabelle.Alle.Where(t => t.Code.StartsWith(familie, StringComparison.Ordinal))
                                       .Select(t => new GebaeudeZonenregelDaten(t.Code, t.Bezeichner)).ToList();
        }

        // ------------------------------------------------------------------
        //  Gespeicherte Bauteile (Datei erneut lesen)
        // ------------------------------------------------------------------

        /// <summary>
        /// Trägt Stufen, Summen und Steckbriefe aus den gespeicherten Bauteilen in die Daten der Ansicht: je Paar aus Kennung der
        /// Datei und Bauteil (über <c>Tab_Importzuordnung</c>), die Aufbauten des Projekts nach Id und die Namen der Baustoffe
        /// nach Id. Ohne Paare bleiben die Daten unverändert.
        /// </summary>
        internal static GebaeudeAnsichtDaten MitAufbau(GebaeudeAnsichtDaten daten, IReadOnlyList<(string Kennung, BauteilModel Bauteil)> bauteile,
                                                       IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten,
                                                       IReadOnlyDictionary<int, string> baustoffnamen)
        {
            if (daten == null || bauteile == null || bauteile.Count == 0) return daten;
            var stufen = new Dictionary<string, Aufbaustufe>(StringComparer.Ordinal);
            var befunde = new Dictionary<string, Bauteilbefundstufe>(StringComparer.Ordinal);
            var steckbriefe = new Dictionary<string, BauteilsteckbriefDaten>(StringComparer.Ordinal);
            foreach ((string kennung, BauteilModel b) in bauteile)
            {
                if (string.IsNullOrEmpty(kennung) || b == null || stufen.ContainsKey(kennung)) continue;
                Aufbaustufe stufe = Stufe(Bauteilzuordnung.Stufe(b, aufbauten));
                stufen[kennung] = stufe;
                befunde[kennung] = Befund(Bauteilbefunde.Befund(Bauteilbefunde.Grund(b)));
                BauteilaufbauModel a = b.ID_Aufbau is int id && aufbauten != null && aufbauten.TryGetValue(id, out BauteilaufbauModel x) ? x : null;
                steckbriefe[kennung] = Steckbrief(b, kennung, stufe, a, baustoffnamen);
            }
            return daten with
            {
                Bauteilstufen = stufen,
                Steckbriefe = steckbriefe,
                Aufbausummen = Summen(bauteile.Where(p => p.Bauteil != null)
                    .Select(p => (Stufe(Bauteilzuordnung.Stufe(p.Bauteil, aufbauten)), IstInnen(p.Bauteil), p.Bauteil.Flaeche))),
                // G5-3: gespeicherte Bauteile tragen nur den Befund ihrer Spalten — den Körperbefund gibt es allein beim Import.
                Bauteilbefunde = befunde,
                Befundsummen = Befundsummen(bauteile.Where(p => p.Bauteil != null)
                    .Select(p => (Befund(Bauteilbefunde.Befund(Bauteilbefunde.Grund(p.Bauteil))), p.Bauteil.Flaeche))),
            };
        }

        /// <summary>Ist ein gespeichertes Bauteil ein Innenbauteil (ohne äußere Randbedingung oder gegen eine Nachbarzone)?</summary>
        internal static bool IstInnen(BauteilModel b) => string.IsNullOrEmpty(b.Randbedingung) || b.ID_Nachbarzone.HasValue;

        /// <summary>Der Steckbrief eines gespeicherten Bauteils; die Kennwerte aus seinem Aufbau, Sprung in den Bauteildialog über die Id.</summary>
        internal static BauteilsteckbriefDaten Steckbrief(BauteilModel b, string kennung, Aufbaustufe stufe, BauteilaufbauModel a,
                                                         IReadOnlyDictionary<int, string> baustoffnamen)
            => MitBefund(SteckbriefOhneBefund(b, kennung, stufe, a, baustoffnamen), Bauteilbefunde.Grund(b));

        private static BauteilsteckbriefDaten SteckbriefOhneBefund(BauteilModel b, string kennung, Aufbaustufe stufe, BauteilaufbauModel a,
                                                                  IReadOnlyDictionary<int, string> baustoffnamen)
        {
            bool transparent = stufe == Aufbaustufe.Transparent || string.Equals(b.Bauteilart, DbWerte.BAUTEILART_TUER, StringComparison.Ordinal);
            string uSchluessel = b.U_Wert.HasValue ? Herkunftsschluessel(b.Herkunft) : a != null ? SteckbriefHerkunft.Schichten : SteckbriefHerkunft.Leer;
            var d = Kopf(b, kennung, stufe, transparent) with
            {
                UHerkunftSchluessel = uSchluessel,
                UHerkunft = Herkunftstext(uSchluessel),
                UWert = UText(b.U_Wert),
                IdBauteil = b.ID > 0 ? b.ID : null,
            };
            if (transparent || a == null) return d with { Rechengrund = transparent ? "" : MyResource.Resource.BTSB_OHNE_RECHNUNG };
            string aufbauSchluessel = Aufbauherkunft(a);
            var schichten = a.Schichten.Select(s => Schichtzeile(s,
                    s.ID_Baustoff is int id && baustoffnamen != null && baustoffnamen.TryGetValue(id, out string n) ? n ?? "" : "", aufbauSchluessel))
                .ToList();
            d = MitKennwerte(d with
            {
                Aufbau = a.Bezeichner ?? "",
                AufbauHerkunftSchluessel = aufbauSchluessel,
                AufbauHerkunft = Herkunftstext(aufbauSchluessel),
                Schichten = schichten,
                Ersatz = string.IsNullOrEmpty(a.Typaufbau) ? "" : (a.Beschreibung ?? a.Typaufbau),
            }, b, a);
            if (!b.U_Wert.HasValue && d.Rechengrund.Length == 0
                && Kennwerte(a, 1.0, b.Neigung ?? 90.0, Rand(b), out _) is { } k)
                d = d with { UWert = UText(k.USchichten_WM2K) };
            return d;
        }

        // ------------------------------------------------------------------
        //  Bausteine
        // ------------------------------------------------------------------

        private static BauteilsteckbriefDaten Kopf(BauteilModel b, string kennung, Aufbaustufe stufe, bool transparent)
        {
            string leer = MyResource.Resource.GIMP_WERT_LEER;
            return new BauteilsteckbriefDaten
            {
                Kennung = kennung ?? "",
                Name = b.Bezeichner ?? "",
                Art = BauteilaufbauCtrl.BauteilartText(b.Bauteilart),
                Stufe = stufe,
                Transparent = transparent,
                Flaeche = GebaeudeZuordnungsModell.ZahlText(Math.Round(b.Flaeche, 2)) + " m²",
                FlaechenherkunftSchluessel = b.Flaechenherkunft ?? "",
                Flaechenherkunft = FlaechenherkunftText(b.Flaechenherkunft),
                Azimut = b.Azimut.HasValue ? GebaeudeImportHuelle.AzimutText(b.Azimut.Value) : leer,
                Neigung = b.Neigung.HasValue ? GebaeudeZuordnungsModell.ZahlText(Math.Round(b.Neigung.Value, 1)) + "°" : leer,
                Randbedingung = GebaeudeImportHuelle.RandText(b.Randbedingung),
                GWert = transparent ? (b.g_Wert.HasValue ? Zahl(Math.Round(b.g_Wert.Value, 2)) : leer) : "",
                Rahmenanteil = transparent ? (b.Rahmenanteil.HasValue ? Zahl(Math.Round(100.0 * b.Rahmenanteil.Value, 0)) + " %" : leer) : "",
            };
        }

        private static Bauteilrand Rand(BauteilModel b)
            => GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung) ?? Bauteilrand.Aussenluft;

        private static BauteilsteckbriefDaten MitKennwerte(BauteilsteckbriefDaten d, BauteilModel b, BauteilaufbauModel a)
        {
            if (Kennwerte(a, b.Flaeche, b.Neigung ?? 90.0, Rand(b), out string grund) is not { } k) return d with { Rechengrund = grund };
            return d with
            {
                R1 = Zahl(Math.Round(k.R1_KW, 5)) + " K/W",
                C1 = Zahl(Math.Round(k.C1_Jk / 1000.0, 1)) + " kJ/K",
                Kapazitaet = Zahl(Math.Round(k.Kapazitaet_JM2K / 1000.0, 1)) + " kJ/(m²K)",
            };
        }

        private static BauteilsteckbriefSchicht Schichtzeile(BauteilschichtModel s, string name, string herkunft)
            => new BauteilsteckbriefSchicht(name ?? "", DickeText(s.Dicke),
                                            s.Lambda.HasValue ? Zahl(Math.Round(s.Lambda.Value, 4)) + " W/(mK)" : "–",
                                            s.Rho.HasValue ? Zahl(Math.Round(s.Rho.Value, 0)) + " kg/m³" : "–",
                                            s.Cp.HasValue ? Zahl(Math.Round(s.Cp.Value, 0)) + " J/(kgK)" : "–",
                                            Herkunftstext(herkunft), herkunft);

        private static string DickeText(double dicke_M) => Zahl(Math.Round(1000.0 * dicke_M, 1)) + " mm";

        private static string UText(double? u) => u is double w ? Zahl(Math.Round(w, 3)) + " W/(m²K)" : MyResource.Resource.GIMP_WERT_LEER;

        private static string Zahl(double wert) => GebaeudeZuordnungsModell.ZahlText(wert);

        private static string Format(string vorlage, params object[] werte) => string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte);
    }
}
