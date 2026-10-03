using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";

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
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                _aus.WriteLine("=== " + Path.GetFileName(pfad) + " — Schema " + a.Quelle?.Schemastand);
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

        /// <summary>
        /// <b>Die Raumbezüge der Anwenderdatei MFH 1964</b> (Mehrzonenkonzept 6.5): Die Datei führt keine Raumgrenzen,
        /// aber je Raum ein <c>IfcRelReferencedInSpatialStructure</c>. Drei Decken trennen Geschosspaare (Keller/EG,
        /// EG/OG1, OG1/DG1); die Decke DG1/DG2 erklärt die Datei als oberste Geschossdecke gegen unbeheizt, während
        /// der Spitzboden „Wohnraum" nach dem Namen beheizt ist — dort gilt die Erklärung der Datei. Die Datei
        /// referenziert je Geschossdecke nur das erste Deckenteil (23,75 m² von rund 96 m² über dem EG, 2,26 m² über dem
        /// OG1): Beide Paare sind zu klein, um zu koppeln — benannt —, die Vorgabe bleibt Z5. 51 Innenwände liegen
        /// zwischen Räumen eines Geschosses, 19 Innenwände (89,9 m²) zählen einseitig; der Innenflächenfaktor kommt aus
        /// der Datei statt leer. Fehlt die Datei, endet der Fall ohne Prüfung.
        /// </summary>
        [Fact]
        public void MFH_1964_Trenndecken_und_innere_Masse_aus_den_Raumbezuegen()
        {
            string pfad = Quellen() == null ? null : Path.Combine(Quellen(), "MFH-Klein-unsaniert-1964.ifc");
            if (pfad == null || !File.Exists(pfad) || new FileInfo(pfad).Length < 1024) { _aus.WriteLine("MFH 1964 fehlt — übersprungen."); return; }
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(3, g.ZahlTrenndeckenReferenz);
            Assert.False(g.GeschosseGekoppelt);
            PruefMeldung bezug = Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_REFERENZ");
            Assert.Equal(new[] { "Gebäude", "3", "Keller/EG, EG/OG1, OG1/DG1", "51" }, bezug.Werte);
            Assert.Equal(new[] { "EG;OG1;23.75;96.42;25", "OG1;DG1;2.26;96.42;2" },
                         g.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_TRENNDECKE_KLEIN").Select(m => string.Join(";", m.Werte)));
            // Der Platzhaltername „Gebäude" weicht dem Dateinamen; das Baujahr führt die Datei, kein Jahreshinweis.
            Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_NAME_PLATZHALTER");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_BAUJAHR_DATEINAME");
            Assert.Equal("MFH-Klein-unsaniert-1964", GebaeudeZuordnungsModell.Vorschlagsname(a.Zuordnen(0, null)));
            PruefMeldung einseitig = Assert.Single(a.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_INNEN_EINSEITIG");
            Assert.Equal(new[] { "19", "89.9" }, einseitig.Werte);

            // Die Hülle des Einzonenwegs bleibt; die Wände zu den Abstellräumen zählen gegen unbeheizt.
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Equal("245.59", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert));
            Assert.Equal("148.68", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert));
            Assert.Equal("119.01", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert));
            Assert.Equal("14.78", Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert));
            Assert.Equal("1.95", Z(satz.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR).Wert));
            Assert.Equal(Importherkunft.Ifc, satz.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR).Herkunft);

            GebaeudeZonierung vorgabe = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z5, vorgabe.Vorgabe);
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            Assert.Contains(z4.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_GRENZEN_ENTKOPPELT");
            Assert.Equal(new[] { "DG1|OG1|2.26", "OG1|OG1 (unbeheizt)|5.85", "OG1|EG|23.75", "EG|EG (unbeheizt)|5.85", "EG|Keller|11.84" },
                         z4.Trennungen.Select(t => z4.Zonen[t.ZoneA].Name + "|" + z4.Zonen[t.ZoneB].Name + "|" + Z(Math.Max(t.FlaecheA, t.FlaecheB))));
        }

        /// <summary>
        /// <b>Die Anwenderdateien mit Schichtdicken in Millimetern</b> unter der Längeneinheit <c>METRE</c>:
        /// Der Leser rechnet die Schichtsätze um und übergeht Folien, jede Schicht liegt danach im Band der
        /// Schichtdicke, und der Bauteilvorschlag läuft mit und ohne Namensabgleich bis zum Ende — ein- und
        /// mehrzonig. Fehlt die Datei (oder liegt nur ein LFS-Zeiger), endet der Fall ohne Prüfung.
        /// </summary>
        [Theory]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc")]
        [InlineData("MFH-Klein-unsaniert-1964.ifc")]
        public void Schichtdicken_in_Millimetern_laufen_bis_zum_Bauteilvorschlag(string datei)
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
}
