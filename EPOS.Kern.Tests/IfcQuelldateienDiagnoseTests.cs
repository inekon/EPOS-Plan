using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Diagnose des IFC-Imports an Anwenderdateien unter <c>Quellen/*.ifc</c></b>: schreibt je Datei
    /// Schema, Gebäude, Räume (beheizt), Fläche, Volumen, Bauteile und die Meldungen von Leser und
    /// Bauteilvorschlag ins Testprotokoll. Die Dateien sind Anwenderdaten und können fehlen — dann
    /// endet der Test ohne Prüfung. Die Regel selbst hält die synthetische Probe
    /// <c>ifc2x3_enthaltensein.ifc</c>.
    /// </summary>
    public sealed class IfcQuelldateienDiagnoseTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public IfcQuelldateienDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string Quellen([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            return o == null ? null : Path.Combine(o, "Quellen");
        }

        /// <summary>Der Pfad einer Anwenderdatei unter <c>Quellen/</c>; <c>null</c>, wenn sie fehlt (oder nur ein LFS-Zeiger liegt).</summary>
        internal static string Pfad(string datei)
        {
            string pfad = Quellen() == null ? null : Path.Combine(Quellen(), datei);
            return pfad == null || !File.Exists(pfad) || new FileInfo(pfad).Length < 1024 ? null : pfad;
        }

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";

        /// <summary>Das Mittel der Raumtemperaturen als Protokolltext: Wert, Spanne, Räume mit/ohne.</summary>
        private static string CadMittel(GebaeudeCadSollwert.Mittel? m)
            => m is GebaeudeCadSollwert.Mittel x
                ? Z(x.Wert) + " °C (" + Z(x.MinC) + "–" + Z(x.MaxC) + " °C, " + x.Raeume + " Räume mit, " + x.OhneTemperatur + " ohne"
                  + (x.SpanneGross ? ", Spanne > 2 K" : "") + ")"
                : "—";

        private static string Text(IEnumerable<PruefMeldung> meldungen)
            => string.Join(" | ", meldungen.Select(m => m.Stufe + " " + m.Schluessel + "(" + string.Join(";", m.Werte) + ")"));

        [Fact]
        public void Quelldateien_IFC_werden_gelesen_und_protokolliert()
        {
            string ordner = Quellen();
            if (ordner == null || !Directory.Exists(ordner)) { _aus.WriteLine("Quellen/ fehlt — übersprungen."); return; }
            string[] dateien = Directory.GetFiles(ordner, "*.ifc").OrderBy(d => d, StringComparer.Ordinal).ToArray();
            if (dateien.Length == 0) { _aus.WriteLine("Keine Quellen/*.ifc — übersprungen."); return; }

            foreach (string pfad in dateien)
            {
                // Eine Zeigerdatei von Git LFS ist keine IFC-Datei.
                if (new FileInfo(pfad).Length < 1024) { _aus.WriteLine(Path.GetFileName(pfad) + ": zu klein — übersprungen."); continue; }
                var a = new GebaeudeImportAblauf();
                var uhr = Stopwatch.StartNew();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                uhr.Stop();
                _aus.WriteLine("=== " + Path.GetFileName(pfad) + " — Schema " + a.Quelle?.Schemastand + ", gelesen in "
                               + uhr.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms");
                _aus.WriteLine("Leser: " + Text(a.Meldungen));
                if (a.Abbild == null || a.Abbild.Gebaeude.Count == 0) { _aus.WriteLine("kein Gebäude"); continue; }
                for (int gi = 0; gi < a.Abbild.Gebaeude.Count; gi++)
                {
                    AbbildGebaeude g = a.Abbild.Gebaeude[gi];
                    List<AbbildRaum> beheizt = g.Raeume.Where(r => r.Beheizt).ToList();
                    _aus.WriteLine("Gebäude " + g.Anzeigename + ": Geschosse " + g.Geschosse.Count + ", Räume " + g.Raeume.Count
                                   + ", beheizt " + beheizt.Count
                                   + ", Fläche beheizt " + Z(beheizt.Sum(r => r.FlaecheM2 ?? 0)) + " m², Volumen beheizt "
                                   + Z(beheizt.Sum(r => r.VolumenM3 ?? 0)) + " m³, Fläche alle " + Z(g.Raeume.Sum(r => r.FlaecheM2 ?? 0))
                                   + " m², Bauteile " + g.Bauteile.Count);
                    foreach (IGrouping<string, AbbildRaum> gr in g.Raeume.GroupBy(r => (r.Name ?? "?") + (r.Beheizt ? " [beheizt " : " [unbeheizt ") + r.BeheiztQuelle + "]"))
                        _aus.WriteLine("  " + gr.Count() + "× " + gr.Key + ", Fläche " + Z(gr.Sum(r => r.FlaecheM2 ?? 0))
                                       + " m², Volumen " + Z(gr.Sum(r => r.VolumenM3 ?? 0)) + " m³");
                    _aus.WriteLine("Gebäudemeldungen: " + Text(g.Meldungen));
                    // Bauteile und Öffnungen je wirksamer Randbedingung (Konzept HottCAD-Verbund 3.1) — nur Zahlen.
                    _aus.WriteLine("Randbedingung wirksam: " + string.Join(", ", g.Bauteile.Concat(g.Bauteile.SelectMany(x => x.Oeffnungen))
                        .GroupBy(x => x.RandbedingungWirksam?.ToString() ?? "ohne Seiten").OrderBy(x => x.Key, StringComparer.Ordinal)
                        .Select(x => x.Key + " " + x.Count())));
                    // Flächengruppen nach Randbedingung (Konzept HottCAD-Verbund 4.2): Bilanz und Gegenprobe je Gruppe — nur Zahlen.
                    if (g.FlaechengruppenBilanzM2 != null)
                    {
                        _aus.WriteLine("Flächengruppen [m² Körper / Menge]: " + string.Join(", ", g.FlaechengruppenBilanzM2.OrderBy(x => x.Key)
                            .Select(x => x.Key + " " + Z(x.Value) + "/" + Z(g.FlaechengruppenMengeM2[x.Key]))));
                        _aus.WriteLine("Flächengruppen: Bauteilkörper " + g.Bauteile.Count(x => x.Koerper != null) + " + Öffnungen "
                                       + g.Bauteile.SelectMany(x => x.Oeffnungen).Count(x => x.Koerper != null) + ", Flächen ohne Bauteil "
                                       + g.Flaechengruppen.Count(x => x.Beleg == Flaechenklassifikation.BELEG_OHNE_BAUTEIL)
                                       + ", Abweichung > 5 %: " + string.Join(" ", Flaechenklassifikation.Abweichungen(g)));
                    }

                    // Raumkörper aus der Datei (G7f-1): Arten, mit/ohne Körper, Dreiecke — nur Protokoll.
                    List<AbbildRaum> mitKoerper = g.Raeume.Where(r => r.Koerper != null).ToList();
                    Zonengeometrie zg = GebaeudeGrundriss.Bilden(a.Abbild, gi);
                    _aus.WriteLine("Raumkörper: " + mitKoerper.Count + " mit, " + (g.Raeume.Count - mitKoerper.Count) + " ohne, Dreiecke "
                                   + mitKoerper.Sum(r => r.Koerper.DreieckZahl) + " (Zonengeometrie " + zg.DateikoerperDreiecke
                                   + "), Randkanten " + mitKoerper.Sum(r => r.Koerper.Randkanten.Count) + "; Arten "
                                   + string.Join(", ", mitKoerper.GroupBy(r => r.Koerper.Art + (r.Koerper.Vermerke.Count > 0 ? " [" + string.Join(",", r.Koerper.Vermerke) + "]" : ""))
                                                                 .OrderBy(x => x.Key, StringComparer.Ordinal)
                                                                 .Select(x => x.Count() + "× " + x.Key + " (" + x.Min(r => r.Koerper.DreieckZahl) + "–" + x.Max(r => r.Koerper.DreieckZahl) + " Dreiecke)")));

                    GebaeudeImportSatz satz = a.Zuordnen(gi, null);
                    _aus.WriteLine("Satz: Nutzfläche " + Z(satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert) + " m², Volumen "
                                   + Z(satz.Zeile(GebaeudeZielfelder.VOLUMEN).Wert) + " m³, Raumhöhe "
                                   + Z(satz.Zeile(GebaeudeZielfelder.RAUMHOEHE).Wert) + " m, Außenwand "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert) + " m², Dach "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert) + " m², Fenster "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FENSTER_GESAMT).Wert) + " m², Grund "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert) + " m², Sonstige "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert) + " m²");
                    _aus.WriteLine("U-Werte: Außenwand " + Z(satz.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Wert) + ", Dach "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_DACH).Wert) + ", Fenster "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_FENSTER).Wert) + ", Grund "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_GRUND).Wert) + ", Sonstige "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_SONSTIGE).Wert) + " W/(m²K)");

                    GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, gi, null);
                    // Je Gruppe (Summenfeld, Randbedingung): Zahl der Zeilen, Fläche und flächengewichteter U-Wert.
                    foreach (var gr in v.Zeilen.GroupBy(z => (z.Summenfeld ?? "IW") + " " + (z.Bauteil.Randbedingung ?? "(innen)"))
                                               .OrderBy(x => x.Key, StringComparer.Ordinal))
                    {
                        double flaeche = gr.Sum(z => z.Bauteil.Flaeche);
                        double mitU = gr.Where(z => z.Bauteil.U_Wert.HasValue).Sum(z => z.Bauteil.Flaeche);
                        double ua = gr.Where(z => z.Bauteil.U_Wert.HasValue).Sum(z => (z.Bauteil.Flaeche) * z.Bauteil.U_Wert.Value);
                        _aus.WriteLine("  Gruppe " + gr.Key + ": " + gr.Count() + " Zeilen, " + Z(flaeche) + " m², U "
                                       + Z(mitU > 0.0 ? ua / mitU : (double?)null) + " W/(m²K) über " + Z(mitU) + " m²");
                    }
                    _aus.WriteLine("Vorschlag: " + Text(v.Meldungen));

                    // Kopf, innere Masse und Zonierung (Vorgabe und Z4) — die Zahlen der Trenndecke über Raumbezüge.
                    GebaeudeFeldzeile innen = satz.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR);
                    _aus.WriteLine("Kopf: " + GebaeudeZuordnungsModell.KopfText(satz) + "; Vorschlagsname „"
                                   + GebaeudeZuordnungsModell.Vorschlagsname(satz) + "“");
                    _aus.WriteLine("Innenflächenfaktor " + Z(innen.Wert) + " (" + innen.Herkunft + ", " + innen.Beleg?.Schluessel + ": "
                                   + string.Join(";", innen.Beleg?.Werte ?? Array.Empty<string>()) + ")");
                    GebaeudeZonierung vorgabe = GebaeudeZonierung.Bilden(a.Abbild, gi);
                    _aus.WriteLine("Zonierung: Regeln " + string.Join(",", vorgabe.Regeln) + ", Vorgabe " + vorgabe.Vorgabe
                                   + ", Trenndecken über Raumbezüge " + g.ZahlTrenndeckenReferenz + "; " + Text(vorgabe.Meldungen));
                    _aus.WriteLine("Körperpaare: " + g.ZahlKoerperpaare + (g.KoerperpaareGebildet ? " (gebildet)" : " (gezählt)") + ", Trennwand "
                                   + Z(g.KoerperTrennwandM2) + " m², Trenndecke " + Z(g.KoerperTrenndeckeM2) + " m²");
                    // Z6 nach Raumtemperatur und Nutzung — nur Protokoll: Zonen mit Temperatur, Nutzungsklassen, Fläche, Raumzahl.
                    _aus.WriteLine("Raumtypen: " + string.Join(", ", g.Raeume.GroupBy(r => (r.Raumtyp ?? "—") + "→" + GebaeudeZonierung.Nutzungsklasse(r))
                                                                         .OrderBy(x => x.Key, StringComparer.Ordinal)
                                                                         .Select(x => x.Key + " " + x.Count() + " (" + string.Join("/", x.Select(r => r.Name ?? "?").Distinct().Take(3)) + ")")));
                    // Die Raumtemperatur der Datei als Heizsollwert (nur auf Wunsch) — nur Protokoll: Gebäudemittel mit Spanne.
                    List<AbbildRaum> warm = g.Raeume.Where(r => GebaeudeRaumzeile.BeheiztWirksam(r, null)).ToList();
                    _aus.WriteLine("CAD-Sollwert: " + (GebaeudeCadSollwert.Moeglich(warm) ? "wählbar" : "nicht wählbar") + "; Gebäudemittel "
                                   + CadMittel(GebaeudeCadSollwert.Bilden(warm)));
                    if (vorgabe.Regeln.Contains(IfcImportProfil.ZONENREGEL_Z6))
                    {
                        GebaeudeZonierung z6 = GebaeudeZonierung.Bilden(a.Abbild, gi, IfcImportProfil.ZONENREGEL_Z6);
                        foreach (Importzone zone in z6.Zonen.Where(x => x.IstBeheizt))
                            _aus.WriteLine("  CAD-Sollwert Z6-Zone " + zone.Name + ": " + CadMittel(GebaeudeCadSollwert.Bilden(zone.Raeume)));
                        _aus.WriteLine("Z6: " + z6.Zonen.Count + " Zonen, Trennungen " + z6.Trennungen.Count + " mit "
                                       + Z(z6.Trennungen.Sum(t => Math.Max(t.FlaecheA, t.FlaecheB))) + " m²; " + Text(z6.Meldungen));
                        foreach (Importzone zone in z6.Zonen)
                            _aus.WriteLine("  Z6-Zone " + zone.Name + (zone.IstBeheizt ? " [beheizt]" : " [unbeheizt]") + ": " + Z(zone.FlaecheM2) + " m², "
                                           + zone.Raeume.Count + " Räume, Klassen " + string.Join("/", zone.Raeume.Select(GebaeudeZonierung.Nutzungsklasse).Distinct())
                                           + (zone.Zugeschlagen.Count > 0 ? ", zugeschlagen " + string.Join("; ", zone.Zugeschlagen) : ""));
                        // Der Zonenplan aus dem Z6-Vorschlag (Mehrzonenkonzept 6.4): Zonen mit Nutzung, offene und äußere Räume.
                        Zonenplan plan = Zonenplan.Vorschlag(a.Abbild, gi, IfcImportProfil.ZONENREGEL_Z6);
                        _aus.WriteLine("Plan Z6: " + plan.Zonen.Count + " Zonen, nicht zugeordnet " + plan.NichtZugeordnet.Count
                                       + ", außerhalb " + plan.RaeumeAusserhalb.Count);
                        foreach (Planbilanzzeile b in plan.Bilanz())
                            _aus.WriteLine("  Planzone " + b.Schluessel + " " + b.Name + ": Nutzung " + (b.Nutzung ?? "keine") + ", "
                                           + b.Raeume + " Räume, " + Z(b.FlaecheM2) + " m²");
                    }
                    else _aus.WriteLine("Z6: nicht wählbar");
                    if (vorgabe.Regeln.Contains(IfcImportProfil.ZONENREGEL_Z4))
                    {
                        GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, gi, IfcImportProfil.ZONENREGEL_Z4);
                        _aus.WriteLine("Z4: " + z4.Zonen.Count + " Zonen (" + string.Join(", ", z4.Zonen.Select(z => z.Name + " " + Z(z.FlaecheM2) + " m²"))
                                       + "), Trennungen " + z4.Trennungen.Count + " mit " + Z(z4.Trennungen.Sum(t => Math.Max(t.FlaecheA, t.FlaecheB)))
                                       + " m², Innen " + Z(z4.Flaechen.Where(f => f.Rand == Zonenrand.Innen).Sum(f => (f.BruttoM2 ?? 0) * (f.Beidseitig ? 2 : 1)))
                                       + " m²; " + Text(z4.Meldungen));
                        foreach (Zonentrennung t in z4.Trennungen)
                            _aus.WriteLine("  Trennung " + z4.Zonen[t.ZoneA].Name + " – " + z4.Zonen[t.ZoneB].Name + ": " + Z(Math.Max(t.FlaecheA, t.FlaecheB)) + " m²");
                        GebaeudeBauteilvorschlag v4 = GebaeudeBauteilvorschlag.BildenMitZonen(a, gi, null, IfcImportProfil.ZONENREGEL_Z4);
                        _aus.WriteLine("Vorschlag Z4: " + v4.Zeilen.Count + " Zeilen, Zone " + Z(v4.Zeilen.Where(r => r.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE)
                                                                                                   .Sum(r => r.Bauteil.Flaeche)) + " m²; "
                                       + Text(v4.Meldungen.Where(m => m.Stufe != PruefStufe.Info)));
                    }
                }
            }
        }

        // ==================================================================
        //  Kennzahlen je Anwenderdatei
        // ==================================================================

        /// <summary>Die Kennzahlen des Imports einer Anwenderdatei, als Texte gefasst (Zahlen auf zwei Stellen).</summary>
        internal sealed class Kennzahlen
        {
            /// <summary>„Werte/Vorgaben/leer" des Kopfs (wie <see cref="GebaeudeZuordnungsModell.KopfText"/> gezählt).</summary>
            public string Kopf;
            public string Baujahr;
            public string Klasse;
            public string Vorschlagsname;
            /// <summary>„Räume/beheizt/beheizte Fläche".</summary>
            public string Raeume;
            /// <summary>Die Vorgabe der Zonierung samt Zahl ihrer Zonen, etwa „Z4 3".</summary>
            public string Vorgabe;
            /// <summary>Die Zonen unter Z4: „Name Fläche" je Zone.</summary>
            public string ZonenZ4;
            /// <summary>Die Trennungen unter Z4: „A|B|Fläche".</summary>
            public string TrennungenZ4;
            /// <summary>Die geschätzten Trenndecken: „Unten/Oben geschätzt (referenziert)".</summary>
            public string Geschaetzt;
            public string Innenflaechenfaktor;
            /// <summary>Die Hülle des Einzonenwegs: Fläche und U-Wert von Außenwand, Dach, Grund, Fenster.</summary>
            public string Huelle;
            /// <summary>Beheizungsart der Datei: „beheizt/unbeheizt/offen".</summary>
            public string Beheizungsart;
            /// <summary>Die Hinweise „Erklärung vor Bezug": „Bauteil|Raum".</summary>
            public string ErklaerungVorBezug;
            /// <summary>Räume, die nach Lage bzw. Name unbeheizt sind: „Lage/Name".</summary>
            public string Unbeheizt;
            /// <summary>Fehler des Bauteilvorschlags mit der Vorgabe und unter Z4 (leer = keiner).</summary>
            public string Fehler;
            /// <summary>Die Flächenpaare der Raumkörper: „Paare/Σ Trennwand/Σ Trenndecke" [m²].</summary>
            public string Koerper;
            /// <summary>Z6: „Zonen/Trennungen/Σ Fläche/Gegenprobe über 2 %"; „—" = nicht wählbar.</summary>
            public string Z6;
            /// <summary>Gegenprobe unter Z4: Zahl der Zonenpaare über 2 % (<c>TRENNFLAECHE_UNGLEICH</c>).</summary>
            public string GegenprobeZ4;

            public override string ToString()
                => "Kopf " + Kopf + ", Baujahr " + Baujahr + ", Klasse " + Klasse + ", Name „" + Vorschlagsname + "“, Räume " + Raeume
                   + ", Vorgabe " + Vorgabe + ", Z4 [" + ZonenZ4 + "], Trennungen [" + TrennungenZ4 + "], geschätzt [" + Geschaetzt
                   + "], f_IW " + Innenflaechenfaktor + ", Hülle [" + Huelle + "], Beheizungsart " + Beheizungsart + ", Erklärung ["
                   + ErklaerungVorBezug + "], unbeheizt Lage/Name " + Unbeheizt + ", Fehler [" + Fehler + "], Körper " + Koerper
                   + ", Z6 " + Z6 + ", Gegenprobe Z4 " + GegenprobeZ4;
        }

        /// <summary>Misst die Kennzahlen einer gelesenen Anwenderdatei (Gebäude 0).</summary>
        internal static Kennzahlen Messen(GebaeudeImportAblauf a)
        {
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            int ausDatei = 0, vorgabe = 0, leer = 0;
            foreach (GebaeudeFeldzeile z in satz.Zeilen)
            {
                if (z.Herkunft == Importherkunft.GbXml || z.Herkunft == Importherkunft.Ifc) ausDatei++;
                else if (ImportherkunftWerte.IstVorgabe(z.Herkunft)) vorgabe++;
                else if (!z.HatWert) leer++;
            }
            string Feld(string f, string u) => Z(satz.Zeile(f).Wert) + " U " + Z(satz.Zeile(u).Wert);
            GebaeudeZonierung zv = GebaeudeZonierung.Bilden(a.Abbild, 0);
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            GebaeudeBauteilvorschlag vv = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            GebaeudeBauteilvorschlag v4 = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, IfcImportProfil.ZONENREGEL_Z4);
            List<AbbildRaum> beheizt = g.Raeume.Where(r => r.Beheizt).ToList();
            GebaeudeZonierung z6 = zv.Regeln.Contains(IfcImportProfil.ZONENREGEL_Z6) ? GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z6) : null;
            int Ungleich(GebaeudeZonierung z) => z.Meldungen.Count(m => m.Schluessel.EndsWith(GebaeudeZonierung.TRENNFLAECHE_UNGLEICH, StringComparison.Ordinal));
            PruefMeldung art = g.Meldungen.FirstOrDefault(m => m.Schluessel == "IMP_IFC_PROT_BEHEIZUNGSART");
            return new Kennzahlen
            {
                Kopf = ausDatei + "/" + vorgabe + "/" + leer,
                Baujahr = Z(satz.Zeile(GebaeudeZielfelder.BAUJAHR).Wert),
                Klasse = satz.Baualtersklasse?.ToString() ?? "—",
                Vorschlagsname = GebaeudeZuordnungsModell.Vorschlagsname(satz),
                Raeume = g.Raeume.Count + "/" + beheizt.Count + "/" + Z(beheizt.Sum(r => r.FlaecheM2 ?? 0.0)),
                Vorgabe = zv.Vorgabe + " " + zv.Zonen.Count,
                ZonenZ4 = string.Join(", ", z4.Zonen.Select(z => z.Name + " " + Z(z.FlaecheM2))),
                TrennungenZ4 = string.Join(", ", z4.Trennungen.Select(t => z4.Zonen[t.ZoneA].Name + "|" + z4.Zonen[t.ZoneB].Name + "|"
                                                                      + Z(Math.Max(t.FlaecheA, t.FlaecheB)))),
                Geschaetzt = string.Join(", ", g.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_GESCHAETZT")
                                                          .Select(m => m.Werte[0] + "/" + m.Werte[1] + " " + m.Werte[2] + " (" + m.Werte[3] + ")")),
                Innenflaechenfaktor = Z(satz.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR).Wert),
                Huelle = "AW " + Feld(GebaeudeZielfelder.FLAECHE_AUSSENWAND, GebaeudeZielfelder.U_AUSSENWAND)
                         + ", Dach " + Feld(GebaeudeZielfelder.FLAECHE_DACH, GebaeudeZielfelder.U_DACH)
                         + ", Grund " + Feld(GebaeudeZielfelder.FLAECHE_GRUND, GebaeudeZielfelder.U_GRUND)
                         + ", Fenster " + Feld(GebaeudeZielfelder.FENSTER_GESAMT, GebaeudeZielfelder.U_FENSTER),
                Beheizungsart = art == null ? "—" : art.Werte[0] + "/" + art.Werte[1] + "/"
                    + g.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_BEHEIZUNGSART_OFFEN").Sum(m => int.Parse(m.Werte[0], CultureInfo.InvariantCulture)),
                ErklaerungVorBezug = string.Join(", ", g.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_ERKLAERUNG_VOR_BEZUG")
                                                                  .Select(m => m.Werte[0] + "|" + m.Werte[1])),
                Unbeheizt = g.Raeume.Count(r => r.BeheiztQuelle == BeheiztQuelle.Lage) + "/" + g.Raeume.Count(r => r.BeheiztQuelle == BeheiztQuelle.Name),
                Fehler = string.Join(" | ", vv.Meldungen.Concat(v4.Meldungen).Where(m => m.Stufe == PruefStufe.Fehler)
                                              .Select(m => string.Join(";", m.Werte)).Distinct()),
                Koerper = g.ZahlKoerperpaare + "/" + Z(g.KoerperTrennwandM2) + "/" + Z(g.KoerperTrenndeckeM2),
                Z6 = z6 == null ? "—" : z6.Zonen.Count + "/" + z6.Trennungen.Count + "/" + Z(z6.Trennungen.Sum(t => Math.Max(t.FlaecheA, t.FlaecheB)))
                                        + "/" + Ungleich(z6),
                GegenprobeZ4 = Ungleich(z4).ToString(CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// <b>Die Erwartungen je Anwenderdatei</b> (HottCAD-Exporte, alle ohne Raumgrenzen, mit Raumbezügen, Gebäudename
        /// „Gebäude", Baujahr im Satz <c>HSETU_GebäudeAllgemein</c>, Beheizungsart je Raum in <c>HSETU_RaumAllgemein</c>):
        /// Kopf „Werte/Vorgaben/leer", Baujahr, Klasse, Vorschlagsname, Räume „alle/beheizt/beheizte Fläche", Vorgabe der
        /// Zonierung mit Zahl der Zonen, Zonen und Trennungen unter Z4, geschätzte Trenndecken, Innenflächenfaktor, Hülle
        /// des Einzonenwegs, Beheizungsart „beheizt/unbeheizt/offen", Erklärung vor Bezug, unbeheizt nach Lage/Name.
        /// Der Bauteilvorschlag läuft mit der Vorgabe und unter Z4 ohne Fehler.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, Kennzahlen> ERWARTET = new Dictionary<string, Kennzahlen>(StringComparer.Ordinal)
        {
            ["MFH_mittel_1984.ifc"] = new Kennzahlen
            {
                Kopf = "21/14/4", Baujahr = "1984", Klasse = "H", Vorschlagsname = "MFH_mittel_1984",
                Raeume = "29/24/298.84", Vorgabe = "Z4 5",
                ZonenZ4 = "DG2 29.43, DG1 104.11, EG 102.12, Keller 63.19, Keller (unbeheizt) 48.35",
                TrennungenZ4 = "DG2|DG1|27.3, DG1|EG|99.39, EG|Keller|62.53, EG|Keller (unbeheizt)|35.56, Keller|Keller (unbeheizt)|37.23",
                Geschaetzt = "", Innenflaechenfaktor = "2.95",
                Huelle = "AW 235.21 U 0.49, Dach 135.28 U 0.46, Grund 152.94 U 3.86, Fenster 53.75 U 2.63",
                Beheizungsart = "22/5/0", ErklaerungVorBezug = "Boden EG 002|Fitnessraum", Unbeheizt = "0/0", Fehler = "",
                Koerper = "100/280.33/224.41", Z6 = "5/9/285.8/0", GegenprobeZ4 = "0",
            },
            ["MFH-Klein-unsaniert-1964.ifc"] = new Kennzahlen
            {
                Kopf = "21/14/4", Baujahr = "1964", Klasse = "E", Vorschlagsname = "MFH-Klein-unsaniert-1964",
                Raeume = "30/23/275.21", Vorgabe = "Z4 7",
                ZonenZ4 = "DG2 48.78, DG1 98.15, OG1 88.53, OG1 (unbeheizt) 19.34, EG 88.53, EG (unbeheizt) 11.3, Keller 114.34",
                TrennungenZ4 = "DG2|DG1|42.33, DG2|OG1 (unbeheizt)|2.78, DG1|OG1|82.95, DG1|OG1 (unbeheizt)|13.74, OG1|OG1 (unbeheizt)|23.81, "
                    + "OG1|EG|88.53, OG1 (unbeheizt)|EG (unbeheizt)|11.3, EG|EG (unbeheizt)|23.81, EG|Keller|88.53, "
                    + "EG (unbeheizt)|Keller|11.3",
                Geschaetzt = "", Innenflaechenfaktor = "3.37",
                Huelle = "AW 245.59 U 1.4, Dach 178.75 U 0.6, Grund 199.11 U 1, Fenster 41.86 U 1.32",
                Beheizungsart = "18/2/0", ErklaerungVorBezug = "", Unbeheizt = "0/0", Fehler = "",
                Koerper = "120/305.03/339.02", Z6 = "4/6/308.56/0", GegenprobeZ4 = "0",
            },
            ["Sportheim_1970_unsaniert.ifc"] = new Kennzahlen
            {
                Kopf = "21/14/4", Baujahr = "1995", Klasse = "I", Vorschlagsname = "Sportheim_1970_unsaniert",
                Raeume = "56/41/718.99", Vorgabe = "Z4 4",
                ZonenZ4 = "OG 230.1, EG 450.78, UG (unbeheizt) 464.56, UG 38.11",
                TrennungenZ4 = "OG|EG|152.78, EG|UG (unbeheizt)|410.82, EG|UG|35.16, UG (unbeheizt)|UG|45.06",
                Geschaetzt = "", Innenflaechenfaktor = "2.66",
                Huelle = "AW 743.53 U 0.27, Dach 698.24 U 0.16, Grund 787.19 U 0.54, Fenster 115.32 U 0.91",
                Beheizungsart = "41/15/0", ErklaerungVorBezug = "Boden EG 005|Flur 003", Unbeheizt = "0/0", Fehler = "",
                Koerper = "168/952.78/574.99", Z6 = "4/6/847.86/0", GegenprobeZ4 = "0",
            },
            ["Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc"] = new Kennzahlen
            {
                Kopf = "21/14/4", Baujahr = "1969", Klasse = "F", Vorschlagsname = "Verwaltung_mit_Montage-2969_vollsaniert_2014",
                Raeume = "113/91/4343.08", Vorgabe = "Z4 8",
                ZonenZ4 = "OG4 546.81, OG3 649.47, OG3 (unbeheizt) 132.63, OG2 669.1, OG1 493.9, EG 1003.67, UG1 980.13, "
                    + "UG2 (unbeheizt) 154.46",
                TrennungenZ4 = "OG4|OG3|578.13, OG4|OG3 (unbeheizt)|67.38, OG3|OG3 (unbeheizt)|93.77, OG3|OG2|761.74, "
                    + "OG3 (unbeheizt)|OG2|57.93, OG3 (unbeheizt)|OG1|58.55, OG3 (unbeheizt)|EG|119.68, OG3 (unbeheizt)|UG1|124.89, "
                    + "OG3 (unbeheizt)|UG2 (unbeheizt)|11.26, OG2|OG1|621.36, OG2|EG|169.01, OG1|EG|707.17, EG|UG1|944.76, "
                    + "UG1|UG2 (unbeheizt)|166.21",
                Geschaetzt = "", Innenflaechenfaktor = "2.85",
                Huelle = "AW 2186.88 U 0.16, Dach 1118.67 U 0.21, Grund 1872.38 U 0.64, Fenster 975.68 U 1.11",
                Beheizungsart = "78/4/0", ErklaerungVorBezug = "Boden UG1 002|Treppenraum", Unbeheizt = "0/0", Fehler = "",
                Koerper = "467/2577.4/3993.85", Z6 = "4/5/3372.94/0", GegenprobeZ4 = "0",
            },
            ["WG-EH55_Poroton-GModG-2026.ifc"] = new Kennzahlen
            {
                Kopf = "21/14/4", Baujahr = "2026", Klasse = "M", Vorschlagsname = "WG-EH55_Poroton-GModG-2026",
                Raeume = "20/20/342.56", Vorgabe = "Z4 3",
                ZonenZ4 = "DG 114.9, EG 120.26, Keller 107.4",
                TrennungenZ4 = "DG|EG|113.56, EG|Keller|105.81",
                Geschaetzt = "", Innenflaechenfaktor = "3.48",
                Huelle = "AW 335.78 U 0.16, Dach 186.39 U 0.13, Grund 229.75 U 0.2, Fenster 97.56 U 0.87",
                Beheizungsart = "20/0/0", ErklaerungVorBezug = "", Unbeheizt = "0/0", Fehler = "",
                Koerper = "79/320.64/219.37", Z6 = "4/5/235.85/0", GegenprobeZ4 = "0",
            },
            ["Produktion_groß_mit_Verwaltung_EG55-2026.ifc"] = new Kennzahlen
            {
                Kopf = "21/14/4", Baujahr = "2026", Klasse = "M", Vorschlagsname = "Produktion_groß_mit_Verwaltung_EG55-2026",
                Raeume = "49/48/18481.66", Vorgabe = "Z4 3",
                ZonenZ4 = "OG1 8794.25, EG 9687.41, EG (unbeheizt) 130.54",
                TrennungenZ4 = "OG1|EG|9098.57, EG|EG (unbeheizt)|149.11",
                Geschaetzt = "", Innenflaechenfaktor = "1.73",
                Huelle = "AW 4272.65 U 0.21, Dach 9798.65 U 0.23, Grund 9952.47 U 2.87, Fenster 1253.89 U 1.29",
                Beheizungsart = "48/1/0", ErklaerungVorBezug = "", Unbeheizt = "0/0", Fehler = "",
                Koerper = "195/5846.78/8748.9", Z6 = "4/4/5516.08/0", GegenprobeZ4 = "0",
            },
        };

        /// <summary>
        /// <b>Je Anwenderdatei ein benannter Fall</b> mit den Erwartungen aus <see cref="ERWARTET"/>; die Kennzahlen stehen
        /// zuerst im Protokoll. Fehlt die Datei (oder liegt nur ein LFS-Zeiger), endet der Fall ohne Prüfung.
        /// </summary>
        [Theory]
        [InlineData("MFH_mittel_1984.ifc")]
        [InlineData("MFH-Klein-unsaniert-1964.ifc")]
        [InlineData("Sportheim_1970_unsaniert.ifc")]
        [InlineData("Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc")]
        [InlineData("WG-EH55_Poroton-GModG-2026.ifc")]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc")]
        public void Kennzahlen_der_Anwenderdatei(string datei)
        {
            GebaeudeImportAblauf a = Anwenderdatei(datei);
            if (a == null) return;
            Kennzahlen ist = Messen(a), soll = ERWARTET[datei];
            _aus.WriteLine(datei + ": " + ist);
            Assert.Equal(soll.Kopf, ist.Kopf);
            Assert.Equal(soll.Baujahr, ist.Baujahr);
            Assert.Equal(soll.Klasse, ist.Klasse);
            Assert.Equal(soll.Vorschlagsname, ist.Vorschlagsname);
            Assert.Equal(soll.Raeume, ist.Raeume);
            Assert.Equal(soll.Vorgabe, ist.Vorgabe);
            Assert.Equal(soll.ZonenZ4, ist.ZonenZ4);
            Assert.Equal(soll.TrennungenZ4, ist.TrennungenZ4);
            Assert.Equal(soll.Geschaetzt, ist.Geschaetzt);
            Assert.Equal(soll.Innenflaechenfaktor, ist.Innenflaechenfaktor);
            Assert.Equal(soll.Huelle, ist.Huelle);
            Assert.Equal(soll.Beheizungsart, ist.Beheizungsart);
            Assert.Equal(soll.ErklaerungVorBezug, ist.ErklaerungVorBezug);
            Assert.Equal(soll.Unbeheizt, ist.Unbeheizt);
            Assert.Equal(soll.Fehler, ist.Fehler);
            Assert.Equal(soll.Koerper, ist.Koerper);
            Assert.Equal(soll.Z6, ist.Z6);
            Assert.Equal(soll.GegenprobeZ4, ist.GegenprobeZ4);

            // Allen gemeinsam: keine Raumgrenzen, Baujahr aus dem Satz des CAD-Exports, Platzhaltername → Dateiname, kein
            // Jahreshinweis aus dem Dateinamen (die Datei führt ein Baujahr).
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(0, g.ZahlGrenzen);
            Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_BAUJAHR_RUECKFALL");
            Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_NAME_PLATZHALTER");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_BAUJAHR_DATEINAME");
        }

        /// <summary>
        /// <b>Die vier Befunde der Sichtprobe mit <c>MFH_mittel_1984.ifc</c></b> (Statuszeile #680): Die Datei führt keine
        /// Raumgrenzen, aber Raumbezüge und Raumkörper — die Trennflächen kommen aus den Körpern; der Name „Gebäude" weicht dem Dateinamen; das
        /// Baujahr 1984 steht im Satz des CAD-Exports und ergibt Klasse H (nicht F); der Kopf zählt 21 Werte, 14 Vorgaben,
        /// 4 leer (statt 18/14/6). Die Beheizungsart der Datei macht die Praxisräume im Keller beheizt; die Decke über dem
        /// Fitnessraum erklärt die Datei als Kellerdecke — der Widerspruch wird benannt. Die Körperpaare koppeln die Geschosse:
        /// Vorgabe Z4.
        /// </summary>
        [Fact]
        public void MFH_mittel_1984_die_Befunde_der_Sichtprobe()
        {
            GebaeudeImportAblauf a = Anwenderdatei("MFH_mittel_1984.ifc");
            if (a == null) return;
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(0, g.ZahlGrenzen);
            Assert.Equal(0, g.ZahlTrenndeckenReferenz);   // die Körperdecken ersetzen die Trenndecken der Raumbezüge
            Assert.Equal(new[] { "Gebäude", "100", "280.33", "224.41" },
                         Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_GRENZEN_AUS_KOERPER").Werte);
            PruefMeldung jahr = Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_BAUJAHR_RUECKFALL");
            Assert.Equal(new[] { "HSETU_GebäudeAllgemein", "YearOfConstruction (Datum)", "01.01.1984 00:00:00", "1984" }, jahr.Werte);
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Equal('H', satz.Baualtersklasse);
            Assert.Equal("MFH_mittel_1984", GebaeudeZuordnungsModell.Vorschlagsname(satz));
            Assert.Equal(new[] { "Fitnessraum", "Warte-R.", "Massage", "Bad/Dusche", "Flur", "Treppenraum" },
                         g.Raeume.Where(r => r.GeschossKennung == g.Geschosse.Single(x => x.Anzeigename == "Keller").Kennung && r.Beheizt)
                                 .Select(r => r.Name));
            Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_ERKLAERUNG_VOR_BEZUG");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, z.Vorgabe);
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_KEINE_GRENZEN" || m.Schluessel == "IMP_IFC_PROT_GRENZEN_ENTKOPPELT");
        }

        /// <summary>
        /// <b>Die Raumbezüge der Anwenderdatei MFH 1964</b> (Mehrzonenkonzept 6.5): Die Datei führt keine Raumgrenzen,
        /// aber je Raum ein <c>IfcRelReferencedInSpatialStructure</c> und einen Körper. Die Trenndecken der Geschosspaare
        /// (Keller/EG, EG/OG1, OG1/DG1, DG1/DG2) kommen aus den Körperpaaren (120 Paare, Trenndecken 339,02 m²) und ersetzen
        /// die referenzierten Deckenteile samt ihrer Schätzung aus den Raummengen (die Treppenräume mit 10 °C sind „getrennt
        /// beheizt" und damit unbeheizt). Der Spitzboden
        /// „Wohnraum" ist nach der Beheizungsart der Datei unbeheizt — die Decke DG1/DG2 trennt zur unbeheizten Zone, ohne
        /// Widerspruch —, Keller und Spitzboden hängen über ihre Decken an, und die beheizten Geschosse sind gekoppelt:
        /// Vorgabe Z4. 51 Innenwände liegen zwischen Räumen eines Geschosses, 19 (89,9 m²) zählen einseitig. Fehlt die
        /// Datei, endet der Fall ohne Prüfung.
        /// </summary>
        [Fact]
        public void MFH_1964_Trenndecken_und_innere_Masse_aus_den_Raumbezuegen()
        {
            GebaeudeImportAblauf a = Anwenderdatei("MFH-Klein-unsaniert-1964.ifc");
            if (a == null) return;
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            // Die Körperdecken ersetzen die vier Trenndecken der Raumbezüge; geschätzt wird nichts mehr.
            Assert.Equal(0, g.ZahlTrenndeckenReferenz);
            Assert.True(g.GeschosseGekoppelt);
            PruefMeldung bezug = Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_REFERENZ");
            Assert.Equal(new[] { "Gebäude", "0", "—", "51" }, bezug.Werte);
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_GESCHAETZT");
            Assert.Equal(new[] { "Gebäude", "120", "305.03", "339.02" },
                         Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_GRENZEN_AUS_KOERPER").Werte);
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_KLEIN");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_ERKLAERUNG_VOR_BEZUG");
            AbbildRaum spitzboden = g.Raeume.Single(r => r.GeschossKennung == g.Geschosse.Single(x => x.Anzeigename == "DG2").Kennung);
            Assert.False(spitzboden.Beheizt);
            Assert.Equal("HSETU_RaumAllgemein.HeatingType = bhtUnHeated", spitzboden.Zustandsangabe);
            // Der Platzhaltername „Gebäude" weicht dem Dateinamen; das Baujahr führt die Datei, kein Jahreshinweis.
            Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_NAME_PLATZHALTER");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_BAUJAHR_DATEINAME");
            Assert.Equal("MFH-Klein-unsaniert-1964", GebaeudeZuordnungsModell.Vorschlagsname(a.Zuordnen(0, null)));
            PruefMeldung einseitig = Assert.Single(a.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_INNEN_EINSEITIG");
            Assert.Equal(new[] { "19", "89.9" }, einseitig.Werte);

            // Die Hülle des Einzonenwegs: Die Trennflächen aus den Körpern zu Abstell- und Treppenräumen (10 °C) und zum
            // Spitzboden zählen gegen unbeheizt — vollständig, nicht mehr nur die referenzierten Teile.
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Equal("245.59", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert));
            Assert.Equal("178.75", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert));
            Assert.Equal("199.11", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert));
            Assert.Equal("61.03", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert));
            Assert.Equal("3.37", Z(satz.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR).Wert));
            Assert.Equal(Importherkunft.Ifc, satz.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR).Herkunft);

            GebaeudeZonierung vorgabe = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, vorgabe.Vorgabe);
            Assert.DoesNotContain(vorgabe.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_GRENZEN_ENTKOPPELT");
            Assert.Equal(new[] { "DG2|DG1|42.33", "DG2|OG1 (unbeheizt)|2.78", "DG1|OG1|82.95", "DG1|OG1 (unbeheizt)|13.74", "OG1|OG1 (unbeheizt)|23.81",
                                 "OG1|EG|88.53", "OG1 (unbeheizt)|EG (unbeheizt)|11.3", "EG|EG (unbeheizt)|23.81", "EG|Keller|88.53", "EG (unbeheizt)|Keller|11.3" },
                         vorgabe.Trennungen.Select(t => vorgabe.Zonen[t.ZoneA].Name + "|" + vorgabe.Zonen[t.ZoneB].Name + "|" + Z(Math.Max(t.FlaecheA, t.FlaecheB))));
        }

        /// <summary>
        /// <b>Die Trenndecke der Produktionsdatei aus den Raumkörpern</b>: Die Körperdecken EG/OG1 (9 098,57 m²) ersetzen die
        /// zwei referenzierten Deckenteile und deren Schätzung aus den Raummengen; sie koppeln alle beheizten Geschosse — Z4
        /// ist die Vorgabe. Nach der Beheizungsart der Datei ist allein der Entsorgungsraum unbeheizt; seine Zone grenzt über
        /// die Körperwände (149,11 m²) an das EG und bleibt.
        /// </summary>
        [Fact]
        public void Produktion_Trenndecke_aus_den_Raummengen_und_Z4_als_Vorgabe()
        {
            GebaeudeImportAblauf a = Anwenderdatei("Produktion_groß_mit_Verwaltung_EG55-2026.ifc");
            if (a == null) return;
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.True(g.GeschosseGekoppelt);
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_GESCHAETZT");
            Assert.Equal(new[] { "Entsorgungsraum" }, g.Raeume.Where(r => !r.Beheizt).Select(r => r.Name));
            Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_NAME_PLATZHALTER");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_BAUJAHR_DATEINAME");   // die Datei führt 2026
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, z.Vorgabe);
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_GRENZEN_ENTKOPPELT");
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_ZONE_OHNE_FLAECHEN");
            Assert.Equal(new[] { "OG1|EG|9098.57", "EG|EG (unbeheizt)|149.11" },
                         z.Trennungen.Select(t => z.Zonen[t.ZoneA].Name + "|" + z.Zonen[t.ZoneB].Name + "|" + Z(Math.Max(t.FlaecheA, t.FlaecheB))));
        }

        /// <summary>
        /// <b>Die drei Nachzüge der Quellendiagnose je Anwenderdatei</b> (Mehrzonenkonzept 6.5): der Hinweis auf ein
        /// abweichendes Jahr im Dateinamen, die Einstufung „getrennt beheizt" nach der Raumtemperatur und der Rückfall der
        /// Wärmekapazität masseloser IFC4-Schichten. Erwartet: Hinweis „Datei/Name", Räume „beheizt/unbeheizt" nach der
        /// Temperatur, Aufbauten „vollständig/masselos/unvollständig/ohne" (verschieden je Kennung) und Aufbauten mit Rückfall.
        /// </summary>
        [Theory]
        [InlineData("MFH_mittel_1984.ifc", "", "2/0", "62/0/0/0", 4)]
        [InlineData("MFH-Klein-unsaniert-1964.ifc", "", "5/5", "0/0/1/0", 0)]
        [InlineData("Sportheim_1970_unsaniert.ifc", "1995/1970", "", "91/0/0/0", 11)]
        [InlineData("Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc", "1969/2014", "13/18", "0/0/189/0", 0)]
        [InlineData("WG-EH55_Poroton-GModG-2026.ifc", "", "", "46/28/0/0", 5)]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc", "", "", "0/0/70/0", 0)]
        public void Nachzuege_der_Quellendiagnose(string datei, string widerspruch, string temperatur, string aufbauten, int rueckfall)
        {
            GebaeudeImportAblauf a = Anwenderdatei(datei);
            if (a == null) return;
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(widerspruch, string.Join(";", g.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_BAUJAHR_WIDERSPRUCH")
                                                               .Select(m => m.Werte[0] + "/" + m.Werte[1])));
            List<PruefMeldung> temp = g.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_BEHEIZUNGSART_TEMPERATUR").ToList();
            Assert.True(temp.Count <= 1);
            foreach (PruefMeldung m in temp) _aus.WriteLine(datei + ": " + string.Join(" | ", m.Werte));
            Assert.Equal(temperatur, string.Join(";", temp.Select(m => m.Werte[0] + "/" + m.Werte[1])));
            List<AbbildAufbau> liste = g.Bauteile.Where(x => x.Aufbau != null).Select(x => x.Aufbau)
                                        .GroupBy(x => x.Kennung).Select(x => x.First()).ToList();
            string ist = string.Join("/", new[] { Aufbaustatus.Vollstaendig, Aufbaustatus.Masselos, Aufbaustatus.Unvollstaendig, Aufbaustatus.OhneAufbau }
                                              .Select(st => liste.Count(x => x.Status == st)));
            List<PruefMeldung> cp = a.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_WAERMEKAPAZITAET_RUECKFALL").ToList();
            foreach (PruefMeldung m in cp) _aus.WriteLine(datei + ": " + string.Join(" | ", m.Werte));
            _aus.WriteLine(datei + ": Aufbauten " + ist + ", Rückfall " + cp.Count + ", beheizt " + Z(g.Raeume.Where(r => r.Beheizt).Sum(r => r.FlaecheM2 ?? 0.0)));
            Assert.Equal(aufbauten, ist);
            Assert.Equal(rueckfall, cp.Count);
            Assert.All(cp, m => Assert.Equal(PruefStufe.Info, m.Stufe));
        }

        /// <summary>Eine Anwenderdatei unter <c>Quellen/</c>, gelesen; <c>null</c>, wenn sie fehlt (oder nur ein LFS-Zeiger liegt).</summary>
        private GebaeudeImportAblauf Anwenderdatei(string datei)
        {
            string pfad = Pfad(datei);
            if (pfad == null) { _aus.WriteLine(datei + " fehlt — übersprungen."); return null; }
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            return a;
        }

        /// <summary>
        /// <b>Die Anwenderdateien mit Schichtdicken in Millimetern</b> unter der Längeneinheit <c>METRE</c>:
        /// Der Leser rechnet die Schichtsätze um und übergeht Folien, jede Schicht liegt danach im Band der
        /// Schichtdicke, und der Bauteilvorschlag läuft mit und ohne Namensabgleich bis zum Ende — ein- und
        /// mehrzonig. Fehlt die Datei (oder liegt nur ein LFS-Zeiger), endet der Fall ohne Prüfung.
        /// </summary>
        [Theory]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc", 9)]
        [InlineData("MFH-Klein-unsaniert-1964.ifc", 1)]
        public void Schichtdicken_in_Millimetern_laufen_bis_zum_Bauteilvorschlag(string datei, int uebergangen)
        {
            string pfad = Quellen() == null ? null : Path.Combine(Quellen(), datei);
            if (pfad == null || !File.Exists(pfad) || new FileInfo(pfad).Length < 1024) { _aus.WriteLine(datei + " fehlt — übersprungen."); return; }
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.True(a.Abbild != null && a.Abbild.Gebaeude.Count > 0, Text(a.Meldungen));
            // Gebündelt: genau eine Warnung und genau ein Hinweis je Datei.
            PruefMeldung mm = Assert.Single(a.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHTDICKE_MM");
            Assert.Equal(PruefStufe.Warnung, mm.Stufe);
            PruefMeldung duenn = Assert.Single(a.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHT_DUENN");
            Assert.Equal(PruefStufe.Info, duenn.Stufe);
            _aus.WriteLine(datei + ": " + Text(new[] { mm, duenn }));
            // Übergangen werden nur Schichten unter 0,5 mm (Folien); das 0,9-mm-Blech der Sandwichelemente bleibt.
            Assert.Equal(uebergangen.ToString(CultureInfo.InvariantCulture), duenn.Werte[0]);
            Assert.DoesNotContain("(0.9 mm)", duenn.Werte[1]);

            var abgleich = new Baustoffabgleich(BaustoffabgleichDaten.AusSaat());
            for (int gi = 0; gi < a.Abbild.Gebaeude.Count; gi++)
            {
                foreach (AbbildBauteil b in a.Abbild.Gebaeude[gi].Bauteile.Where(x => x.Aufbau != null))
                    foreach (AbbildSchicht x in b.Aufbau.Schichten.Where(x => x.DickeM.HasValue))
                        Assert.True(x.DickeM >= GebaeudeFestwerte.SCHICHT_DICKE_MIN_M && x.DickeM <= GebaeudeFestwerte.SCHICHT_DICKE_MAX_M,
                                    b.Aufbau.Kennung + " " + x.Name + ": d = " + Z(x.DickeM) + " m");
                foreach (Baustoffabgleich mit in new[] { null, abgleich })
                {
                    GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, gi, 'E', null, mit);
                    GebaeudeBauteilvorschlag z = GebaeudeBauteilvorschlag.BildenMitZonen(a, gi, 'E', null, null, mit);
                    Assert.False(v.Abgelehnt, Text(v.Meldungen));
                    Assert.False(z.Abgelehnt, Text(z.Meldungen));

                    // Die Bauteilliste des Dialogs (Katalogliste mit Suche und Trichtern): eine Zeile je Bauteil mit
                    // eindeutigem Schlüssel; Sortierung nach der Fläche und Suche laufen über alle Zeilen.
                    GebaeudeBauteileDaten daten = GebaeudeImportHuelle.BauteileDaten(v);
                    Assert.NotNull(daten.Profil);
                    Assert.Equal(v.Zeilen.Count, daten.Liste.Count);
                    Assert.Equal(daten.Liste.Count, daten.Liste.Select(l => l.Schluessel).Distinct(StringComparer.Ordinal).Count());
                    var sortiert = new Katalogfilterstand { Sortierspalte = GebaeudeImportZonen.SP_FLAECHE, Aufsteigend = false };
                    Assert.Equal(daten.Liste.Count, Katalogfilter.Anwenden(daten.Profil, daten.Liste, sortiert).Count);
                    string erster = daten.Liste[0].Bezeichner;
                    var gesucht = new Katalogfilterstand { Suche = erster };
                    Assert.Contains(Katalogfilter.Anwenden(daten.Profil, daten.Liste, gesucht), l => l.Bezeichner == erster);
                    _aus.WriteLine(datei + (mit == null ? " ohne" : " mit") + " Abgleich: " + v.Aufbauten.Count + " Aufbauten, "
                                   + v.Zeilen.Count + " Zeilen; " + Text(v.Meldungen.Where(m => m.Stufe != PruefStufe.Info)));
                }
            }
        }
    }

    /// <summary>
    /// <b>Der Durchgang bis zur Rechnung</b> an den Anwenderdateien unter <c>Quellen/*.ifc</c> (Muster „Durchgang bis
    /// zur Rechnung" aus G6c, Welle C): über die Programmwege des Gebäudedialogs — Lesen, Zuordnen mit der
    /// Baualtersklasse der Datei und der Zonenregel, Übernehmen als Zone(n) mit Bauteilen, Editor, Speichern der
    /// Gebäudeliste — in die Arbeitskopie der Testdatenbank (Projekt 1007), dann der Jahreslauf VDI 6007 über die
    /// Fassade des Gebäudedialogs (<see cref="GebaeudeBedarfCtrl"/>). Je Datei mit der Vorgabe der Zonierung und zum
    /// Vergleich mit der anderen Regel (Z4 ↔ Z5); die Auslegungsheizlast nach der Herleitung der Übergabe
    /// (stationär, Anlagenkopplung 8.4) für die eine Zone (Z5) — mehrzonig gibt es sie nur gekoppelt nicht. Fehlt
    /// die Datei oder die Testdatenbank, endet der Fall ohne Prüfung; die Zahlen stehen im Protokoll.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class IfcQuelldateienDurchgangTests : IDisposable
    {
        private const int PROJEKT = 1007;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public IfcQuelldateienDurchgangTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static IReadOnlyDictionary<string, bool> Keine => new Dictionary<string, bool>();

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";

        /// <summary>Das Ergebnis eines Durchgangs mit einer Zonenregel.</summary>
        internal sealed record Rechnung(string Regel, int Zonen, double NutzflaecheM2, double HeizwaermeMwh, double MaxLastKw,
                                        double? SpitzeTagesmittelKw, double? AuslegungsheizlastKw, IReadOnlyList<string> Hinweise,
                                        double Sekunden, string Befund)
        {
            internal double SpezifischKwhM2 => NutzflaecheM2 > 0.0 ? HeizwaermeMwh * 1000.0 / NutzflaecheM2 : double.NaN;
        }

        /// <summary>
        /// Ein Durchgang: Lesen, Zuordnen (Klasse der Datei, <paramref name="regel"/>), Übernehmen als Zone(n) mit Bauteilen,
        /// Editor, Speichern der Gebäudeliste, Jahreslauf; <c>null</c>, wenn Datei oder Testdatenbank fehlen.
        /// </summary>
        private async Task<Rechnung> Durchgang(string pfad, string regel, string name)
        {
            var uhr = Stopwatch.StartNew();
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            GebaeudeImportweg weg = ((Func<GebaeudeImportweg>)gaben["ImportGaben"])();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)weg.Gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(pfad, null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)weg.Gaben["Zuordnen"];
            int? klasse = zuordnen(new GebaeudeZuordnungsanfrage(0, null, Keine, Zonenregel: regel)).KlasseDerDatei;
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, klasse, Keine, Zonenregel: regel));
            Assert.Equal(regel, stand.Zonierung!.Regel);
            Assert.True(stand.Bauteile!.Moeglich, stand.Bauteile.Ablehnung);

            var ergebnis = new GebaeudeImportErgebnis(0, klasse, name, Keine, stand.Zeilen.ToList(), AlsZone: true, Zonenregel: regel);
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)weg.Gaben["Pruefen"];
            Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == EPOS.UI.Bausteine.WarnStufe.Fehler);
            Assert.Null(await ((Func<GebaeudeImportErgebnis, Task<string>>)weg.Gaben["Uebernehmen"])(ergebnis));

            IReadOnlyDictionary<string, object> editor = weg.EditorGaben();
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden((GebaeudeKatalogDaten)editor["Daten"], neu: true);
            GebaeudePruefbefund befund = arbeit.Pruefen(true, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());
            Assert.True(befund == null, befund?.Meldung);
            arbeit.Ableiten();
            var speichern = (Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>)editor["Speichern"];
            Assert.True(speichern(arbeit.Stand, true, arbeit.Stand.Name).Erfolg);
            GebaeudeProjektZeile zeile = weg.Aufnehmen();
            Assert.NotNull(zeile);
            ((List<GebaeudeProjektZeile>)gaben["Zeilen"]).Add(zeile);
            ((Action)gaben["Geaendert"])();
            Z_ProjGebModel neu = modelle.Single(m => m.Gebaeudename == name);
            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);

            // Der Jahreslauf über die Fassade des Gebäudedialogs, auf dem VDI-Weg.
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, neu.ID_Z, DbWerte.GEBAEUDE_MODELL_VDI6007);
            string laufbefund = e.Erfolgreich && p.IstFehlerfrei ? null : e.Befund ?? string.Join(" | ", p.Fehler);

            // Die Projektkopie: Fläche und Zonen; einzonig dazu die Auslegungsheizlast nach der Herleitung der Übergabe.
            var gebaeude = new ProjektGebaeudeCtrl();
            gebaeude.ReadAll(PROJEKT);
            int kopie = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ? AND ID_Projekt = ?",
                new DbParam("@z", neu.ID_Z), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);
            ProjektGebaeudeModel item = gebaeude.items.Single(g => g.ID_Gebaeude == kopie);
            List<ZoneModel> zonenmodelle = new GebaeudeZonenCtrl().LesenJeGebaeude(kopie).ToList();
            int zonen = zonenmodelle.Count;
            double flaeche = zonenmodelle.Where(z => z.IstBeheizt).Sum(z => z.Nutzflaeche ?? 0.0);
            double? auslegung = null;
            if (zonen <= 1 && laufbefund == null)
            {
                var sim = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
                sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
                item.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
                item.Heizkreis_Aktiv = true;
                item.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
                auslegung = sim.UebergabeEingang(item).AuslegungsheizlastW / 1000.0;
            }
            uhr.Stop();
            return new Rechnung(regel, zonen, flaeche, e.HeizwaermeMwh, e.MaxLastKw, e.SpitzeTagesmittelKw, auslegung,
                                p.Hinweise.Concat(p.Warnungen).Distinct().ToList(), uhr.Elapsed.TotalSeconds, laufbefund);
        }

        /// <summary>Beide Durchgänge einer Datei — Vorgabe zuerst, dann die andere Regel —, ins Protokoll geschrieben.</summary>
        private async Task<(Rechnung Vorgabe, Rechnung Vergleich)> Beide(string datei)
        {
            if (!_db.Vorhanden) { _aus.WriteLine("Testdatenbank fehlt — übersprungen."); return (null, null); }
            string pfad = IfcQuelldateienDiagnoseTests.Pfad(datei);
            if (pfad == null) { _aus.WriteLine(datei + " fehlt — übersprungen."); return (null, null); }
            string vorgabe;
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            vorgabe = GebaeudeZonierung.Bilden(a.Abbild, 0).Vorgabe;
            string andere = vorgabe == IfcImportProfil.ZONENREGEL_Z4 ? IfcImportProfil.ZONENREGEL_Z5 : IfcImportProfil.ZONENREGEL_Z4;
            string stamm = Path.GetFileNameWithoutExtension(datei);
            Rechnung r1 = await Durchgang(pfad, vorgabe, stamm + " " + vorgabe);
            Rechnung r2 = await Durchgang(pfad, andere, stamm + " " + andere);
            foreach (Rechnung r in new[] { r1, r2 })
                _aus.WriteLine(datei + " " + r.Regel + (r == r1 ? " (Vorgabe)" : " (Vergleich)") + ": " + r.Zonen + " Zonen, A " + Z(r.NutzflaecheM2)
                               + " m², Q " + Z(r.HeizwaermeMwh) + " MWh/a = " + Z(r.SpezifischKwhM2) + " kWh/(m²a), Spitze " + Z(r.MaxLastKw)
                               + " kW, Tagesmittel " + Z(r.SpitzeTagesmittelKw) + " kW, Auslegungsheizlast " + Z(r.AuslegungsheizlastKw)
                               + " kW, " + Z(r.Sekunden) + " s" + (r.Befund == null ? "" : "; BEFUND: " + r.Befund)
                               + "; Hinweise: " + string.Join(" | ", r.Hinweise));
            return (r1, r2);
        }

        /// <summary>Nah an einem gemessenen Wert (relativ).</summary>
        private static void Nah(double erwartet, double ist, double relativ, string was)
            => Assert.True(Math.Abs(ist - erwartet) <= relativ * Math.Abs(erwartet), was + ": erwartet " + Z(erwartet) + ", ist " + Z(ist));

        /// <summary>
        /// Die gemessenen Werte je Datei (VDI-Weg, Klimaregion des Projekts 1007): Vorgabe und Zonenzahl, Jahresheizwärme
        /// [MWh/a], Spitze der idealen Last und Spitze als Tagesmittel [kW] mit der Vorgabe; Jahresheizwärme mit der anderen
        /// Regel (<c>NaN</c> = der Lauf lehnt sie ab); Auslegungsheizlast der einen Zone (Z5) [kW]. Gemessen mit dem
        /// Erdreichwiderstand nach DIN EN ISO 13370 (Rechenweg RP2a); die Produktion (Bodenplatte 9 952 m², U 2,87) fiel
        /// damit von 225 auf 100 kWh/(m²a), MFH-Klein (ohne Bauteil am Erdreich) blieb. Ohne Raumgrenzen tragen die
        /// Trennflächen aus den Raumkörpern die Nachbarschaft der Räume (Mehrzonenkonzept 6.2): Alle sechs Dateien haben Z4
        /// als Vorgabe, die Flächen gegen unbeheizte Räume sind vollständig.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, (string Regel, int Zonen, double Q, double Spitze, double Tagesmittel, double QVergleich, double Auslegung)> SOLL
            = new Dictionary<string, (string, int, double, double, double, double, double)>(StringComparer.Ordinal)
            {
                ["MFH_mittel_1984.ifc"] = ("Z4", 5, 45.17, 26.71, 16.47, 48.12, 19.46),
                ["MFH-Klein-unsaniert-1964.ifc"] = ("Z4", 7, 41.36, 29.94, 17.46, 53.86, 22.03),
                ["Sportheim_1970_unsaniert.ifc"] = ("Z4", 4, 59.29, 52.62, 28.05, 71.07, 37.21),
                ["Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc"] = ("Z4", 8, 236.04, 266.65, 132.12, 227.80, 184.64),
                ["WG-EH55_Poroton-GModG-2026.ifc"] = ("Z4", 3, 17.67, 21.77, 9.76, 17.47, 14.13),
                ["Produktion_groß_mit_Verwaltung_EG55-2026.ifc"] = ("Z4", 3, 1856.82, 1273.75, 862.81, 1867.99, 1012.48),
            };

        /// <summary>
        /// <b>Je Anwenderdatei der Durchgang mit der Vorgabe und mit der anderen Regel</b>: Die Vorgabe rechnet fehlerfrei,
        /// mit den gemessenen Werten (relativ 1 %); der Vergleich rechnet ebenso, die Verwaltung unter Z4 mit der
        /// Lastumkehr im Innern eines geregelten Abschnitts (Rechenbefund RB-Z4, <see cref="ZonenkopplungAbschnittsregelTests"/>).
        /// </summary>
        [Theory]
        [InlineData("MFH_mittel_1984.ifc")]
        [InlineData("MFH-Klein-unsaniert-1964.ifc")]
        [InlineData("Sportheim_1970_unsaniert.ifc")]
        [InlineData("Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc")]
        [InlineData("WG-EH55_Poroton-GModG-2026.ifc")]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc")]
        public async Task Durchgang_bis_zur_Rechnung(string datei)
        {
            (Rechnung vorgabe, Rechnung vergleich) = await Beide(datei);
            if (vorgabe == null) return;
            var soll = SOLL[datei];
            Assert.Null(vorgabe.Befund);
            Assert.Equal(soll.Regel, vorgabe.Regel);
            Assert.Equal(soll.Zonen, vorgabe.Zonen);
            Nah(soll.Q, vorgabe.HeizwaermeMwh, 0.01, "Jahresheizwärme");
            Nah(soll.Spitze, vorgabe.MaxLastKw, 0.01, "Spitze");
            Nah(soll.Tagesmittel, vorgabe.SpitzeTagesmittelKw ?? double.NaN, 0.01, "Spitze als Tagesmittel");
            Rechnung z5 = vorgabe.Regel == IfcImportProfil.ZONENREGEL_Z5 ? vorgabe : vergleich;
            Assert.Null(z5.Befund);
            Nah(soll.Auslegung, z5.AuslegungsheizlastKw ?? double.NaN, 0.01, "Auslegungsheizlast");
            if (double.IsNaN(soll.QVergleich))
                Assert.NotNull(vergleich.Befund);
            else
            {
                Assert.Null(vergleich.Befund);
                Nah(soll.QVergleich, vergleich.HeizwaermeMwh, 0.01, "Jahresheizwärme mit der anderen Regel");
            }
        }
    }
}
