using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die gemeinsame Zuordnung (E2) eines Gebäudes auf den Einzonen-Weg</b> (Zonenregel X4) —
    /// formatfrei, der IFC-Leser (G4a) benutzt sie mit. Eingang ist das normierte
    /// <see cref="GebaeudeAbbild"/>, Ausgang der <see cref="GebaeudeImportSatz"/>. Ohne Datenbank.
    ///
    /// <para><b>Die Regeln.</b></para>
    /// <list type="bullet">
    /// <item><b>Zone:</b> Die beheizten Räume bilden die eine Zone — Nutzfläche = Σ Fläche,
    /// Volumen = Σ Volumen, Raumhöhe = Volumen ÷ Nutzfläche, wenn beides da ist. Fehlt einem
    /// beheizten Raum die Fläche, bleibt die Nutzfläche leer (keine geratene Teilsumme).</item>
    /// <item><b>Hülle:</b> Ein Bauteil mit einem beheizten Nachbarn und Außentyp gehört nach seinem
    /// Typ zur Hülle; zwei beheizte Nachbarn = innere Masse, keine Hülle; beheizt ↔ unbeheizt =
    /// Hülle „gegen unbeheizt": Boden → Grundfläche mit Randbedingung KELLER, Decke → Dach (als
    /// Außenluft gerechnet), Wand → sonstige Flächen. Ein Nachbar, den es nicht gibt, gilt als
    /// unbeheizt. Alles gegen Erdreich geht in die Grundfläche mit ERDREICH — nur sie kennt im Modell
    /// eine Erdreich-Randbedingung (<c>GebaeudeModellEingang</c>: Wand, Dach und Sonstiges rechnen
    /// gegen die Außenluft). Böden gegen Außenluft und Außentüren gehen in die sonstigen Flächen.</item>
    /// <item><b>Raumhöhe:</b> die angegebene Höhe der Räume flächengewichtet, wenn jeder beheizte
    /// Raum eine trägt (IFC); sonst Volumen ÷ Nutzfläche; sonst die Vorgabe des Profils
    /// (<see cref="GebaeudeImportProfil.RueckfallRaumhoeheM"/>, IFC 2,5 m; gbXML keine). Die
    /// angegebene Höhe wird gegen das Volumen gehalten: Weicht Nutzfläche × Raumhöhe um mehr als
    /// 20 % vom Volumen ab, warnt die Zuordnung (Prüfgröße, Umsetzungskonzept 3.4).</item>
    /// <item><b>Vorgabe-Rückfälle der Hüllflächen</b> (Umsetzungskonzept 3.4, nur mit
    /// <see cref="GebaeudeImportProfil.FlaechenRueckfaelle"/>, also IFC): Ist für eine Gruppe KEINE
    /// Fläche gelesen, gilt für das Dach die Grundfläche des obersten beheizten Geschosses, für die
    /// Grundfläche Nutzfläche ÷ Zahl der beheizten Geschosse (führt die Datei kein Bodenbauteil, ist
    /// auch die Randbedingung eine Vorgabe: Erdreich), für die sonstigen Flächen 0 — jeweils mit
    /// Herkunft „Vorgabe" und Beleg; ist der Rückfall selbst nicht bestimmbar, bleibt die Zeile, wie
    /// sie ist.</item>
    /// <item><b>Außenbauteil ohne Nachbarraum:</b> Setzt das Format
    /// <see cref="AbbildBauteil.HuelleOhneNachbar"/> (IFC: <c>IsExternal</c> ohne Raumgrenze), zählt es
    /// nach seiner Randbedingung zur Hülle.</item>
    /// <item><b>Fensterabzug (U14):</b> A_Wand = Brutto − Σ Fenster − Σ Außentüren derselben Fläche;
    /// negativ → die Nettofläche der Datei, wenn sie eine angibt (IFC: <c>NetSideArea</c>), sonst 0 und
    /// Zeile Fehler. Der Bruttowert wird mitgeführt.</item>
    /// <item><b>Baualtersklasse:</b> die gewählte; ohne sie die aus dem Baujahr der Datei
    /// (<see cref="Baujahrregel"/>, A…H), Herkunft der Datei.</item>
    /// <item><b>Luftwechsel:</b> gelesen nur auf die Infiltration (D12); liest die Datei für keinen
    /// beheizten Raum einen, gelten die Vorgaben des Stundenmodells (0,3 und 0,4 1/h).</item>
    /// <item><b>U-Werte:</b> flächengewichtet je Gruppe, U = Σ(U·A)/ΣA; fehlt der U-Wert bei mehr
    /// als 30 % der Gruppenfläche, gilt die Gruppe als nicht aus der Datei → Vorgabe der Klasse. Ein
    /// eingetragener U-Wert hat Vorrang; sonst rechnet <see cref="Bauteilreduktion"/> ihn aus den
    /// Schichten (über <see cref="SchichtwerteNaht"/>).</item>
    /// <item><b>Fenster:</b> Sektor nach dem Azimut des Wirtsbauteils — nächste Mitte (N 0°, O 90°,
    /// S 180°, W 270°), Breite 90°, die Grenze gehört zum größeren Sektor (45° → Ost). Ohne Azimut
    /// (auch auf waagerechten Wirten) gleichmäßig auf alle vier, mit Warnung.</item>
    /// <item><b>Vorgaben:</b> U, g und ψ aus <see cref="GebaeudeVorgaben"/> (U12, U15), die
    /// Anschlusslängen bleiben leer.</item>
    /// <item><b>Bauart aus Schichten</b> (Umsetzungskonzept 3.4, Zeile Bauweise): raumseitige
    /// Schichten bis 10 cm, C″ = Σ ρ·c·d je Aufbau (<see cref="SchichtwerteNaht.WirksameKapazitaetAusSchichten"/>),
    /// flächengewichtet über die opaken Hüllbauteile der beheizten Zone, ÷ 3600 → Wh/(m²K) —
    /// nur, wenn JEDES dieser Bauteile einen vollständigen Aufbau trägt; die Bauart rastet über
    /// <see cref="Gebaeudebauweise.BauartAusBauweise"/> ein. Die Bauweise selbst bleibt leer
    /// (<see cref="GebaeudeZielfeld.Abgeleitet"/>): Der Gebäudeeditor bildet sie im Modus Neu vor
    /// dem Speichern aus Bauart und Nutzfläche und überschriebe einen freien Wert
    /// (<c>GebaeudeArbeitsstand.Laden</c> setzt <c>BauweiseNachfuehren</c> für einen neuen Satz,
    /// <c>Ableiten</c> schreibt Nutzfläche × 20/50/100). Der Wert aus den Schichten steht im Beleg.</item>
    /// <item><b>Räume:</b> „beheizt" entscheidet der Leser; der Anwender kann es je Raum
    /// übersteuern (Raumliste des Dialogs) — gelesen wird der wirksame Zustand
    /// (<see cref="GebaeudeRaumzeile.BeheiztWirksam"/>), das Abbild bleibt unverändert.</item>
    /// </list>
    /// </summary>
    internal static class GebaeudeAggregation
    {
        /// <summary>Anteil der Gruppenfläche ohne U-Wert, ab dem die Gruppe als „nicht aus der Datei" gilt (E2, Umsetzungskonzept 3.4).</summary>
        internal const double ANTEIL_OHNE_U_GRENZE = 0.30;

        /// <summary>Relative Abweichung, ab der zwei Seiten einer Trennfläche als ungleich gemeldet werden (3.5).</summary>
        internal const double TRENNFLAECHE_TOLERANZ = 0.02;

        /// <summary>
        /// Neigungsabstand zur Waagerechten [°], unter dem ein Wirtsbauteil keine Himmelsrichtung hat:
        /// Ein Oberlicht im Flachdach trägt zwar einen Azimut in der Datei, aber er sagt nichts.
        /// </summary>
        internal const double WAAGERECHT_GRAD = 10.0;

        private enum Huelle { Aussenwand, Dach, Grund, Sonstige }

        private enum Seite { Aussen, Erdreich, Unbeheizt }

        private sealed class Posten
        {
            public AbbildBauteil Bauteil;
            public Huelle Gruppe;
            public string GrundRand;
            public double? NettoM2;
            public double? BruttoM2;
            public double AbzugM2;
            public double? U;
            public Randbedingung Rand;
            public string Paar;
            public string Richtung;
            public bool Verworfen;
            public readonly List<Fensterposten> Fenster = new List<Fensterposten>();
            public readonly List<Posten> Tueren = new List<Posten>();
        }

        private sealed class Fensterposten
        {
            public AbbildBauteil Oeffnung;
            public double? FlaecheM2;
            public double? AzimutGrad;
            public double? U;
            public double? G;
        }

        /// <summary>Bildet den Satz eines Gebäudes.</summary>
        /// <param name="uebersteuert">Raumkennung → beheizt (die Haken der Raumliste); <c>null</c> = keine.</param>
        internal static GebaeudeImportSatz Bilden(GebaeudeAbbild abbild, int index, char? klasse,
                                                  GebaeudeQuelle quelle, GebaeudeImportProfil profil,
                                                  IReadOnlyDictionary<string, bool> uebersteuert = null)
        {
            AbbildGebaeude g = abbild.Gebaeude[index];
            Importherkunft datei = string.Equals(abbild.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal)
                ? Importherkunft.Ifc : Importherkunft.GbXml;
            // Die Klasse: die gewählte hat Vorrang; sonst folgt sie dem Baujahr der Datei (A…H,
            // Umsetzungskonzept 3.4) — ab 2001 gibt es keine abgeleitete Klasse.
            char? k = Klasse(klasse);
            Func<AbbildRaum, bool> istBeheizt = r => GebaeudeRaumzeile.BeheiztWirksam(r, uebersteuert);
            bool klasseAusBaujahr = false;
            if (!k.HasValue && g.Baujahr is int jahr)
            {
                k = Baujahrregel.Klasse(jahr);
                klasseAusBaujahr = k.HasValue;
            }

            var meldungen = new List<PruefMeldung>(abbild.Meldungen);
            meldungen.AddRange(g.Meldungen);
            foreach (AbbildBauteil b in g.Bauteile)
            {
                meldungen.AddRange(b.Meldungen);
                foreach (AbbildBauteil o in b.Oeffnungen) meldungen.AddRange(o.Meldungen);
            }

            // Die Haken der Raumliste: je umgestelltem Raum dieses Gebäudes eine Info.
            var abweichungen = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (AbbildRaum r in g.Raeume)
            {
                bool wirksam = istBeheizt(r);
                if (wirksam == r.Beheizt) continue;
                abweichungen[r.Kennung] = wirksam;
                meldungen.Add(new PruefMeldung(PruefStufe.Info,
                    GebaeudeImportAblauf.MELDUNG + (wirksam ? "RAUM_ALS_BEHEIZT" : "RAUM_ALS_UNBEHEIZT"),
                    r.Kennung, r.Name ?? ""));
            }

            var zeilen = GebaeudeZielfelder.Alle.Select(f => new GebaeudeFeldzeile(f.Schluessel)).ToList();
            var z = zeilen.ToDictionary(r => r.Zielfeld, StringComparer.Ordinal);

            // Alle Räume der Datei — ein Nachbar kann im Nachbargebäude liegen.
            var raeume = new Dictionary<string, AbbildRaum>(StringComparer.Ordinal);
            var raumGebaeude = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < abbild.Gebaeude.Count; i++)
                foreach (AbbildRaum r in abbild.Gebaeude[i].Raeume)
                    if (!raeume.ContainsKey(r.Kennung)) { raeume[r.Kennung] = r; raumGebaeude[r.Kennung] = i; }

            List<AbbildRaum> beheizt = g.Raeume.Where(istBeheizt).ToList();

            Kenngroessen(g, beheizt, datei, k, klasseAusBaujahr, profil, z, meldungen);

            // ---- Hülle ----
            var posten = new List<Posten>();
            var zaehler = new SortedDictionary<string, double[]>(StringComparer.Ordinal);   // Info-Sammler: Schlüssel → {Zahl, Fläche}
            foreach (AbbildBauteil s in g.Bauteile)
            {
                Posten p = Einordnen(s, index, raeume, raumGebaeude, istBeheizt, zaehler, z);
                if (p != null) posten.Add(p);
            }
            Trennflaechen(posten, profil, meldungen);
            Oeffnungen(posten, profil, z, meldungen);
            UWerte(posten, meldungen);
            foreach (KeyValuePair<string, double[]> e in zaehler)
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + e.Key,
                    Zahl(e.Value[0]), Zahl(e.Value[1])));

            List<Posten> aktiv = posten.Where(p => !p.Verworfen).ToList();
            Gruppe(aktiv, Huelle.Aussenwand, GebaeudeZielfelder.FLAECHE_AUSSENWAND, GebaeudeZielfelder.U_AUSSENWAND, datei, k, z, meldungen);
            Gruppe(aktiv, Huelle.Dach, GebaeudeZielfelder.FLAECHE_DACH, GebaeudeZielfelder.U_DACH, datei, k, z, meldungen);
            Gruppe(aktiv, Huelle.Grund, GebaeudeZielfelder.FLAECHE_GRUND, GebaeudeZielfelder.U_GRUND, datei, k, z, meldungen);
            var sonstige = aktiv.Where(p => p.Gruppe == Huelle.Sonstige).Concat(aktiv.SelectMany(p => p.Tueren)).ToList();
            Gruppe(sonstige, Huelle.Sonstige, GebaeudeZielfelder.FLAECHE_SONSTIGE, GebaeudeZielfelder.U_SONSTIGE, datei, k, z, meldungen);
            Grundrand(aktiv, datei, z, meldungen);
            if (profil.FlaechenRueckfaelle)
                Flaechenrueckfaelle(aktiv, sonstige, g, beheizt, z);
            Bauart(aktiv, datei, z, meldungen);
            Fenster(aktiv, datei, k, z, meldungen);
            Waermebruecken(k, z);
            Lueftung(beheizt, datei, z, meldungen);
            Sollwerte(beheizt, datei, z, meldungen);

            // Prüfgrößen und abgeleitete Felder (gesamte Fensterfläche, Bauweise) übernimmt nie jemand.
            foreach (GebaeudeFeldzeile r in zeilen)
                r.Uebernehmen = r.HatWert && r.HakenSetzbar;

            // ---- Quellzuordnungen (Tab_Importzuordnung, 7.2) — Ziel in G4c immer das Gebäude ----
            var zuordnungen = new List<GebaeudeQuellzuordnung> { new GebaeudeQuellzuordnung(g.Quelltyp, g.Kennung, ImportZiel.Gebaeude) };
            foreach (AbbildRaum r in beheizt)
                zuordnungen.Add(new GebaeudeQuellzuordnung(r.Quelltyp, r.Kennung, ImportZiel.Gebaeude));
            foreach (Posten p in aktiv)
            {
                zuordnungen.Add(new GebaeudeQuellzuordnung(p.Bauteil.Quelltyp, p.Bauteil.Kennung, ImportZiel.Gebaeude));
                foreach (Fensterposten f in p.Fenster)
                    zuordnungen.Add(new GebaeudeQuellzuordnung(f.Oeffnung.Quelltyp, f.Oeffnung.Kennung, ImportZiel.Gebaeude));
                foreach (Posten t in p.Tueren)
                    zuordnungen.Add(new GebaeudeQuellzuordnung(t.Bauteil.Quelltyp, t.Bauteil.Kennung, ImportZiel.Gebaeude));
            }

            var satz = new GebaeudeImportSatz(quelle, g.Name, g.Kennung, k, profil.Zonenregel, g.Zonenvorschlag,
                                              zeilen, meldungen, zuordnungen)
            {
                Uebersteuerungen = abweichungen,
                Baujahr = g.Baujahr,
            };

            // Obergrenze der Zonen (3.3) — für X4 ist es genau eine.
            int zonen = Zonenzahl(profil.Zonenregel, g, beheizt.Count);
            if (zonen > GebaeudeImportProfil.MAX_ZONEN)
                satz.Ablehnung = new PruefMeldung(PruefStufe.Fehler, profil.Meldung("ZU_VIELE_ZONEN"),
                    zonen.ToString(CultureInfo.InvariantCulture),
                    GebaeudeImportProfil.MAX_ZONEN.ToString(CultureInfo.InvariantCulture));
            return satz;
        }

        // ==================================================================
        //  Kenngrößen der einen Zone
        // ==================================================================

        private static void Kenngroessen(AbbildGebaeude g, List<AbbildRaum> beheizt, Importherkunft datei, char? k,
                                         bool klasseAusBaujahr, GebaeudeImportProfil profil,
                                         Dictionary<string, GebaeudeFeldzeile> z, List<PruefMeldung> meldungen)
        {
            if (beheizt.Count == 0)
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "KEINE_BEHEIZTEN_RAEUME", g.Anzeigename));

            double? nutzflaeche = null, volumen = null;
            List<string> ohneFlaeche = beheizt.Where(r => !(r.FlaecheM2 > 0.0)).Select(r => r.Kennung).ToList();
            if (beheizt.Count > 0 && ohneFlaeche.Count == 0)
            {
                nutzflaeche = beheizt.Sum(r => r.FlaecheM2.Value);
                Setzen(z[GebaeudeZielfelder.NUTZFLAECHE], nutzflaeche, datei,
                    new GebaeudeBeleg("GIMP_BELEG_RAEUME", Zahl(beheizt.Count)));
            }
            else if (ohneFlaeche.Count > 0)
            {
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "RAUMFLAECHE_FEHLT",
                    Zahl(ohneFlaeche.Count), Liste(ohneFlaeche)));
                z[GebaeudeZielfelder.NUTZFLAECHE].Markieren(PruefStufe.Warnung);
            }

            if (beheizt.Count > 0 && beheizt.All(r => r.VolumenM3 > 0.0))
            {
                volumen = beheizt.Sum(r => r.VolumenM3.Value);
                Setzen(z[GebaeudeZielfelder.VOLUMEN], volumen, datei,
                    new GebaeudeBeleg("GIMP_BELEG_RAEUME", Zahl(beheizt.Count)));
            }

            // Raumhöhe: die angegebene Höhe flächengewichtet, wenn JEDER beheizte Raum Höhe und Fläche
            // trägt (IFC: Qto_SpaceBaseQuantities.Height, Umsetzungskonzept 3.4); sonst Volumen ÷ Nutzfläche;
            // sonst die Vorgabe des Formats (IFC 2,5 m; gbXML keine — dort bleibt die Zeile leer).
            GebaeudeFeldzeile hoehe = z[GebaeudeZielfelder.RAUMHOEHE];
            bool hoeheAngegeben = false;
            if (beheizt.Count > 0 && beheizt.All(r => r.HoeheM > 0.0 && r.FlaecheM2 > 0.0))
            {
                double summeA = beheizt.Sum(r => r.FlaecheM2.Value);
                Setzen(hoehe, beheizt.Sum(r => r.HoeheM.Value * r.FlaecheM2.Value) / summeA, datei,
                    new GebaeudeBeleg("GIMP_BELEG_RAUMHOEHE_GEWICHTET", Zahl(beheizt.Count), Zahl(summeA)));
                hoeheAngegeben = true;
            }
            else if (nutzflaeche > 0.0 && volumen > 0.0)
                Setzen(hoehe, volumen.Value / nutzflaeche.Value, datei,
                    new GebaeudeBeleg("GIMP_BELEG_RAUMHOEHE", Zahl(volumen.Value), Zahl(nutzflaeche.Value)));
            else if (profil.RueckfallRaumhoeheM is double vorgabeHoehe)
            {
                hoehe.VorgabeWert = vorgabeHoehe;
                hoehe.VorgabeBeleg = new GebaeudeBeleg("GIMP_BELEG_RAUMHOEHE_VORGABE", Zahl(vorgabeHoehe));
                VorgabeUebernehmen(hoehe);
            }

            // Prüfgröße (3.4): Die angegebene Höhe wird gegen das Volumen gehalten — weicht
            // Nutzfläche × Raumhöhe um mehr als 20 % ab, ist eines von beiden falsch; gewarnt wird hier,
            // damit der Dialog es zeigt, bevor der Anwender OK drückt (dieselbe Regel prüft Pruefen).
            if (hoeheAngegeben && nutzflaeche > 0.0 && volumen > 0.0)
            {
                double soll = nutzflaeche.Value * hoehe.Wert.Value;
                if (Math.Abs(volumen.Value - soll) > GebaeudeImportAblauf.VOLUMEN_TOLERANZ * volumen.Value)
                {
                    meldungen.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "VOLUMEN_ABWEICHUNG",
                        Zahl(volumen.Value), Zahl(soll)));
                    hoehe.Markieren(PruefStufe.Warnung);
                }
            }

            // Fläche je Nutzer: Σ Fläche ÷ Σ Personen — nur, wenn JEDER beheizte Raum eine Angabe trägt.
            var personen = beheizt.Select(r => r.Personen
                ?? (r.FlaecheJePersonM2 > 0.0 && r.FlaecheM2 > 0.0 ? r.FlaecheM2 / r.FlaecheJePersonM2 : null)).ToList();
            int mitPersonen = personen.Count(p => p.HasValue);
            if (beheizt.Count > 0 && mitPersonen == beheizt.Count && nutzflaeche > 0.0 && personen.Sum(p => p.Value) > 0.0)
            {
                double summe = personen.Sum(p => p.Value);
                Setzen(z[GebaeudeZielfelder.FLAECHE_JE_NUTZER], nutzflaeche.Value / summe, datei,
                    new GebaeudeBeleg("GIMP_BELEG_PERSONEN", Zahl(summe), Zahl(beheizt.Count)));
            }
            else if (mitPersonen > 0)
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "PERSONEN_UNVOLLSTAENDIG",
                    Zahl(mitPersonen), Zahl(beheizt.Count - mitPersonen)));

            // Innere Gewinne: Die Datei trägt AUSLEGUNGSleistungen je Fläche (mit Zeitplänen gemeint),
            // das Modell einen zeitlich konstanten Gewinn — nicht dieselbe Größe. Nur Vorschlag im Beleg.
            List<AbbildRaum> mitLeistung = beheizt.Where(r => r.FlaecheM2 > 0.0 && (r.LichtWm2.HasValue || r.GeraeteWm2.HasValue)).ToList();
            if (mitLeistung.Count > 0)
                z[GebaeudeZielfelder.INNERE_GEWINNE].Beleg = new GebaeudeBeleg("GIMP_BELEG_GEWINNE_VORSCHLAG",
                    Zahl(mitLeistung.Sum(r => ((r.LichtWm2 ?? 0.0) + (r.GeraeteWm2 ?? 0.0)) * r.FlaecheM2.Value)),
                    Zahl(mitLeistung.Count));

            GebaeudeFeldzeile bak = z[GebaeudeZielfelder.BAUALTERSKLASSE];
            if (k.HasValue)
            {
                bak.Textwert = k.Value.ToString();
                if (klasseAusBaujahr)
                {
                    bak.Herkunft = datei;
                    bak.Beleg = new GebaeudeBeleg("GIMP_BELEG_KLASSE_BAUJAHR",
                        g.Baujahr.Value.ToString(CultureInfo.InvariantCulture), g.BaujahrText ?? "");
                }
                else
                {
                    bak.Herkunft = Importherkunft.Manuell;
                    bak.Beleg = new GebaeudeBeleg("GIMP_BELEG_KLASSE_ANWENDER");
                }
                Baualtersvorgabe v = GebaeudeVorgaben.Fuer(k);
                if (v == null || v.Katalogsaetze == 0)
                    meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "KLASSE_OHNE_VORGABE", k.Value.ToString()));
            }
            else
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "KEINE_BAUALTERSKLASSE"));
        }

        // ==================================================================
        //  Bauart aus den Schichten (Umsetzungskonzept 3.4, Zeile Bauweise)
        // ==================================================================

        /// <summary>
        /// Die Bauart der Zone: aus den Schichten, wenn JEDES opake Hüllbauteil einen vollständigen
        /// Aufbau trägt — C″ je Aufbau über <see cref="SchichtwerteNaht.WirksameKapazitaetAusSchichten"/>,
        /// flächengewichtet mit der Nettofläche, ÷ 3600 → Wh/(m²K); die Bauart rastet über
        /// <see cref="Gebaeudebauweise.BauartAusBauweise"/> ein (Schwellen 30 und 75 Wh/(m²K)). Sonst
        /// „schwer" als Vorgabe — nie ein absoluter Wert (Gebaeudebauweise.BauweiseAusBauart).
        ///
        /// <para><b>Die Bauweise bleibt leer</b>, auch wenn sie aus den Schichten bestimmbar ist:
        /// Der Gebäudeeditor bildet sie im Modus Neu vor dem Speichern aus Bauart und Nutzfläche
        /// (<c>GebaeudeArbeitsstand.Laden</c>: <c>BauweiseNachfuehren = neu || …</c>;
        /// <c>Ableiten</c>: Nutzfläche × 20/50/100) — ein übernommener freier Wert ginge dort
        /// still verloren. Die Zahl steht deshalb im Beleg; das Plausibilitätsband 5 … 200 Wh/(m²K)
        /// (Konzept 4.8) prüft den spezifischen Wert und warnt.</para>
        ///
        /// <para>Die Türen stehen nicht in <paramref name="aktiv"/> (sie hängen an ihrer Wand) und
        /// zählen als Öffnung nicht mit, ebenso die Fenster.</para>
        /// </summary>
        private static void Bauart(List<Posten> aktiv, Importherkunft datei, Dictionary<string, GebaeudeFeldzeile> z,
                                   List<PruefMeldung> meldungen)
        {
            GebaeudeFeldzeile bauart = z[GebaeudeZielfelder.BAUART];
            GebaeudeFeldzeile bauweise = z[GebaeudeZielfelder.BAUWEISE];
            double? nutzflaeche = z[GebaeudeZielfelder.NUTZFLAECHE].Wert;

            int vollstaendig = 0;
            double summeCA = 0.0, summeA = 0.0;
            foreach (Posten p in aktiv)
            {
                double? c = p.NettoM2.HasValue ? SchichtwerteNaht.WirksameKapazitaetAusSchichten(p.Bauteil.Aufbau) : null;
                if (!c.HasValue) continue;
                vollstaendig++;
                summeCA += c.Value * p.NettoM2.Value;
                summeA += p.NettoM2.Value;
            }

            if (aktiv.Count > 0 && vollstaendig == aktiv.Count && summeA > 0.0)
            {
                double jeM2 = summeCA / summeA / 3600.0;   // J/(m²K) → Wh/(m²K)
                double? gesamt = nutzflaeche > 0.0 ? jeM2 * nutzflaeche.Value : null;
                int index = gesamt.HasValue
                    ? Gebaeudebauweise.BauartAusBauweise(gesamt.Value, nutzflaeche.Value)
                    : Gebaeudebauweise.BauartAusBauweise(jeM2, 1.0);

                bauart.Textwert = GebaeudeZielfelder.BauartSchluessel(index);
                bauart.Herkunft = datei;
                bauart.Beleg = new GebaeudeBeleg("GIMP_BELEG_BAUART_SCHICHTEN",
                    Zahl(Math.Round(jeM2, 2)), Zahl(vollstaendig), Zahl(Math.Round(summeA, 2)));
                bauweise.Beleg = gesamt.HasValue
                    ? new GebaeudeBeleg("GIMP_BELEG_BAUWEISE_SCHICHTEN", Zahl(Math.Round(gesamt.Value)), Zahl(Math.Round(jeM2, 2)))
                    : new GebaeudeBeleg("GIMP_BELEG_BAUWEISE_AUS_BAUART");

                if (!(jeM2 >= GebaeudeFestwerte.BAUWEISE_JE_M2_MIN && jeM2 <= GebaeudeFestwerte.BAUWEISE_JE_M2_MAX))
                {
                    meldungen.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "BAUWEISE_AUSSERHALB",
                        Zahl(Math.Round(jeM2, 2)), Zahl(GebaeudeFestwerte.BAUWEISE_JE_M2_MIN),
                        Zahl(GebaeudeFestwerte.BAUWEISE_JE_M2_MAX)));
                    bauart.Markieren(PruefStufe.Warnung);
                }
                return;
            }

            bauart.Textwert = GebaeudeZielfelder.BAUART_SCHWER;
            bauart.Herkunft = Importherkunft.Vorgabe;
            bauart.Beleg = new GebaeudeBeleg("GIMP_BELEG_BAUART_VORGABE", Zahl(vollstaendig), Zahl(aktiv.Count));
            bauweise.Beleg = new GebaeudeBeleg("GIMP_BELEG_BAUWEISE_AUS_BAUART");
        }

        // ==================================================================
        //  Hülle: Einordnen je Bauteil
        // ==================================================================

        private static Posten Einordnen(AbbildBauteil s, int index, Dictionary<string, AbbildRaum> raeume,
                                        Dictionary<string, int> raumGebaeude, Func<AbbildRaum, bool> istBeheizt,
                                        SortedDictionary<string, double[]> zaehler, Dictionary<string, GebaeudeFeldzeile> z)
        {
            // Der beheizte Nachbar DIESES Gebäudes; ohne ihn gehört das Bauteil nicht zur Hülle.
            int hPos = -1;
            for (int i = 0; i < s.Nachbarn.Count; i++)
                if (raeume.TryGetValue(s.Nachbarn[i].Kennung, out AbbildRaum r) && istBeheizt(r)
                    && raumGebaeude[s.Nachbarn[i].Kennung] == index) { hPos = i; break; }
            // Ausnahme: Ein Außenbauteil ohne jeden Nachbarraum, das das Format seinem Gebäude zuordnet
            // (IFC: IsExternal ohne Raumgrenze), zählt nach seiner Randbedingung zur Hülle.
            bool ohneNachbar = hPos < 0 && s.HuelleOhneNachbar && s.Nachbarn.Count == 0
                               && (s.Randbedingung == Randbedingung.Aussenluft || s.Randbedingung == Randbedingung.Erdreich);
            if (hPos < 0 && !ohneNachbar) return null;

            int aPos = -1;
            for (int i = 0; i < s.Nachbarn.Count; i++)
                if (i != hPos) { aPos = i; break; }

            Seite seite;
            AbbildRaum andererRaum = null;
            if (aPos < 0)
            {
                if (s.Randbedingung == Randbedingung.Aussenluft) seite = Seite.Aussen;
                else if (s.Randbedingung == Randbedingung.Erdreich) seite = Seite.Erdreich;
                else seite = Seite.Unbeheizt;   // Innentyp mit nur einem Nachbarn: die andere Seite ist unbekannt
            }
            else
            {
                raeume.TryGetValue(s.Nachbarn[aPos].Kennung, out andererRaum);
                if (andererRaum != null && istBeheizt(andererRaum)) return null;   // innere Masse
                seite = Seite.Unbeheizt;                                          // unbeheizt oder unbekannt
            }

            var p = new Posten { Bauteil = s, BruttoM2 = s.BruttoflaecheM2 };
            switch (seite)
            {
                case Seite.Aussen:
                    if (s.Art == Bauteilart.Aussenwand) p.Gruppe = Huelle.Aussenwand;
                    else if (s.Art == Bauteilart.Dach || s.Art == Bauteilart.Decke) p.Gruppe = Huelle.Dach;
                    else
                    {
                        p.Gruppe = Huelle.Sonstige;
                        if (s.Art == Bauteilart.Bodenplatte) Zaehlen(zaehler, "BODEN_AUSSENLUFT", s);
                    }
                    break;

                case Seite.Erdreich:
                    p.Gruppe = Huelle.Grund;
                    p.GrundRand = DbWerte.GRUND_ERDREICH;
                    if (s.Art != Bauteilart.Bodenplatte) Zaehlen(zaehler, "ERDREICH_ZUR_GRUNDFLAECHE", s);
                    break;

                default:
                    bool? boden = IstWaagerechteArt(s.Art) ? Boden(s, hPos, aPos) : null;
                    if (boden == true)
                    {
                        p.Gruppe = Huelle.Grund;
                        p.GrundRand = DbWerte.GRUND_KELLER;
                        Zaehlen(zaehler, "BODEN_GEGEN_UNBEHEIZT", s);
                    }
                    else if (boden == false)
                    {
                        p.Gruppe = Huelle.Dach;
                        Zaehlen(zaehler, "DECKE_GEGEN_UNBEHEIZT", s);
                    }
                    else
                    {
                        p.Gruppe = Huelle.Sonstige;
                        Zaehlen(zaehler, "FLAECHE_GEGEN_UNBEHEIZT", s);
                    }
                    if (aPos >= 0 && andererRaum != null)
                    {
                        string h = s.Nachbarn[hPos].Kennung, a = s.Nachbarn[aPos].Kennung;
                        p.Paar = string.CompareOrdinal(h, a) < 0 ? h + "\u0001" + a : a + "\u0001" + h;
                        p.Richtung = s.Nachbarn[0].Kennung;
                    }
                    else
                        z[FeldDerGruppe(p.Gruppe)].Markieren(PruefStufe.Warnung);   // Nachbar unbekannt (3.8)
                    break;
            }

            if (!p.BruttoM2.HasValue)
                z[FeldDerGruppe(p.Gruppe)].Markieren(PruefStufe.Warnung);   // Geometrie fehlt (3.8)
            p.Rand = seite == Seite.Aussen ? Randbedingung.Aussenluft
                   : seite == Seite.Erdreich ? Randbedingung.Erdreich : Randbedingung.Unbeheizt;
            return p;
        }

        /// <summary>Ist die Fläche für den beheizten Nachbarn Boden (<c>true</c>) oder Decke (<c>false</c>)? <c>null</c> = unbestimmt.</summary>
        private static bool? Boden(AbbildBauteil s, int hPos, int aPos)
        {
            // 1. Die Sicht des beheizten Raums (AdjacentSpaceId/@surfaceType), 2. die des anderen.
            bool? b = SichtIstBoden(s.Nachbarn[hPos].Sicht);
            if (b.HasValue) return b;
            if (aPos >= 0)
            {
                b = SichtIstBoden(s.Nachbarn[aPos].Sicht);
                if (b.HasValue) return !b.Value;
            }

            // 3. Die Neigung: Die Normale zeigt vom ERSTEN Nachbarn weg (gbXML-Hausannahme) —
            //    nach oben (Neigung < 90°) heißt: der erste liegt darunter, die Fläche ist seine Decke.
            bool hIstErster = hPos == 0;
            if (s.NeigungGrad is double t && Math.Abs(t - 90.0) > WAAGERECHT_GRAD)
            {
                bool ersterUnten = t < 90.0;
                return hIstErster ? !ersterUnten : ersterUnten;
            }

            // 4. Die Art der Fläche aus Sicht des ersten Nachbarn (Ceiling, InteriorFloor …).
            b = SichtIstBoden(s.Quellart);
            if (b.HasValue) return hIstErster ? b.Value : !b.Value;
            return null;
        }

        /// <summary>
        /// Die Sicht „Boden" eines Nachbarraums auf die Fläche — der gbXML-Wortlaut, den auch der
        /// IFC-Leser setzt, wenn er die Lage einer Decke aus den Geschosshöhen kennt.
        /// </summary>
        internal const string SICHT_BODEN = "InteriorFloor";

        /// <summary>Die Sicht „Decke" eines Nachbarraums auf die Fläche (siehe <see cref="SICHT_BODEN"/>).</summary>
        internal const string SICHT_DECKE = "Ceiling";

        /// <summary>Ist eine Flächenart aus Sicht ihres Raums ein Boden (<c>true</c>), eine Decke (<c>false</c>) oder keins von beiden?</summary>
        internal static bool? SichtIstBoden(string art)
        {
            if (string.IsNullOrEmpty(art)) return null;
            switch (art)
            {
                case "InteriorFloor": case "SlabOnGrade": case "UndergroundSlab": case "RaisedFloor": case "ExposedFloor":
                    return true;
                case "Ceiling": case "Roof": case "UndergroundCeiling":
                    return false;
                default:
                    return null;
            }
        }

        private static bool IstWaagerechteArt(Bauteilart art)
            => art == Bauteilart.Decke || art == Bauteilart.Bodenplatte || art == Bauteilart.Dach;

        /// <summary>
        /// Die U-Werte der Hüllenbauteile, ihrer Fenster und Türen — nach dem Einordnen, weil erst
        /// dann die Randbedingung feststeht; Öffnungen ohne eigene Neigung nehmen die ihres Wirts.
        /// </summary>
        private static void UWerte(List<Posten> posten, List<PruefMeldung> meldungen)
        {
            var gemeldet = new HashSet<string>(StringComparer.Ordinal);
            foreach (Posten p in posten)
            {
                p.U = UWert(p.Bauteil, p.Bauteil.NeigungGrad, p.Rand, meldungen, gemeldet);
                foreach (Fensterposten f in p.Fenster)
                    f.U = UWert(f.Oeffnung, f.Oeffnung.NeigungGrad ?? p.Bauteil.NeigungGrad, p.Rand, meldungen, gemeldet);
                foreach (Posten t in p.Tueren)
                    t.U = UWert(t.Bauteil, t.Bauteil.NeigungGrad ?? p.Bauteil.NeigungGrad, p.Rand, meldungen, gemeldet);
            }
        }

        /// <summary>
        /// Der U-Wert eines Bauteils: der eingetragene (Vorrang, Mehrzonenkonzept 3.4), sonst der aus
        /// den Schichten über <see cref="Bauteilreduktion"/>, sonst keiner. Liegen die Stoffwerte
        /// außerhalb des Bandes, wird das je Aufbau einmal gemeldet — nie geworfen.
        /// </summary>
        private static double? UWert(AbbildBauteil b, double? neigungGrad, Randbedingung rand,
                                     List<PruefMeldung> meldungen, HashSet<string> gemeldet)
        {
            if (b.UWertWm2K.HasValue) return b.UWertWm2K;
            if (b.Aufbau == null) return null;
            double? u = SchichtwerteNaht.UWertAusSchichten(b.Aufbau, neigungGrad, rand, out string grund);
            if (grund != null && gemeldet.Add(b.Aufbau.Kennung))
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "SCHICHTEN_AUSSERHALB",
                    b.Aufbau.Kennung, grund));
            return u;
        }

        // ==================================================================
        //  Trennflächen und Öffnungen
        // ==================================================================

        /// <summary>
        /// Die Gegenprobe der Trennflächen (3.5): Beschreibt die Datei dieselbe Trennfläche von beiden
        /// Seiten (A→B und B→A), zählt nur die größere; weichen sie um mehr als 2 % ab, wird es gemeldet.
        /// </summary>
        private static void Trennflaechen(List<Posten> posten, GebaeudeImportProfil profil, List<PruefMeldung> meldungen)
        {
            foreach (IGrouping<string, Posten> paar in posten.Where(p => p.Paar != null).GroupBy(p => p.Paar, StringComparer.Ordinal))
            {
                var richtungen = paar.GroupBy(p => p.Richtung, StringComparer.Ordinal).ToList();
                if (richtungen.Count < 2) continue;
                var summen = richtungen.Select(r => new { r.Key, Flaeche = r.Sum(p => p.BruttoM2 ?? 0.0), Posten = r.ToList() })
                                       .OrderByDescending(r => r.Flaeche).ThenBy(r => r.Key, StringComparer.Ordinal).ToList();
                double gross = summen[0].Flaeche, klein = summen[1].Flaeche;
                foreach (var r in summen.Skip(1))
                    foreach (Posten p in r.Posten) p.Verworfen = true;
                if (gross > 0.0 && (gross - klein) > TRENNFLAECHE_TOLERANZ * gross)
                {
                    string[] raeume = paar.Key.Split('\u0001');
                    meldungen.Add(new PruefMeldung(PruefStufe.Warnung, profil.Meldung("TRENNFLAECHE_UNGLEICH"),
                        raeume[0], raeume[1], Zahl(gross), Zahl(klein)));
                }
            }
        }

        /// <summary>Fenster und Türen der Hüllenbauteile samt Fensterabzug (U14).</summary>
        private static void Oeffnungen(List<Posten> posten, GebaeudeImportProfil profil,
                                       Dictionary<string, GebaeudeFeldzeile> z, List<PruefMeldung> meldungen)
        {
            foreach (Posten p in posten)
            {
                AbbildBauteil s = p.Bauteil;
                double? azimut = HatHimmelsrichtung(s) ? s.AzimutGrad : null;
                foreach (AbbildBauteil o in s.Oeffnungen)
                {
                    if (o.Art == Bauteilart.Fenster)
                    {
                        p.Fenster.Add(new Fensterposten
                        {
                            Oeffnung = o, FlaecheM2 = o.BruttoflaecheM2, AzimutGrad = azimut, G = o.GWert,
                        });
                    }
                    else if (o.Art == Bauteilart.Tuer)
                    {
                        p.Tueren.Add(new Posten
                        {
                            Bauteil = o, Gruppe = Huelle.Sonstige, NettoM2 = o.BruttoflaecheM2, BruttoM2 = o.BruttoflaecheM2,
                            Rand = p.Rand, Verworfen = p.Verworfen,
                        });
                    }
                    else continue;

                    if (o.BruttoflaecheM2.HasValue) p.AbzugM2 += o.BruttoflaecheM2.Value;
                    else if (!p.Verworfen)
                        z[o.Art == Bauteilart.Fenster ? GebaeudeZielfelder.FENSTER_GESAMT : GebaeudeZielfelder.FLAECHE_SONSTIGE]
                            .Markieren(PruefStufe.Warnung);
                }

                if (!p.BruttoM2.HasValue) continue;
                double netto = p.BruttoM2.Value - p.AbzugM2;
                if (netto < 0.0 && s.NettoflaecheM2 is double eigen && eigen >= 0.0)
                {
                    // U14: Rückfall auf die Nettofläche, die die Datei selbst angibt (IFC: NetSideArea).
                    if (!p.Verworfen)
                        meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "NETTOFLAECHE_RUECKFALL",
                            s.Kennung, Zahl(p.BruttoM2.Value), Zahl(p.AbzugM2), Zahl(eigen)));
                    netto = eigen;
                }
                if (netto < 0.0)
                {
                    netto = 0.0;
                    if (!p.Verworfen)
                    {
                        meldungen.Add(new PruefMeldung(PruefStufe.Fehler, profil.Meldung("NETTOFLAECHE_NEGATIV"),
                            s.Kennung, Zahl(p.BruttoM2.Value), Zahl(p.AbzugM2)));
                        z[FeldDerGruppe(p.Gruppe)].Markieren(PruefStufe.Fehler);
                    }
                }
                p.NettoM2 = netto;
            }
        }

        private static bool HatHimmelsrichtung(AbbildBauteil wirt)
            => wirt.AzimutGrad is double a && !double.IsNaN(a) && !double.IsInfinity(a)
               && !(wirt.NeigungGrad is double t && (t < WAAGERECHT_GRAD || t > 180.0 - WAAGERECHT_GRAD));

        // ==================================================================
        //  Gruppen
        // ==================================================================

        private static void Gruppe(List<Posten> alle, Huelle gruppe, string feldFlaeche, string feldU, Importherkunft datei,
                                   char? k, Dictionary<string, GebaeudeFeldzeile> z, List<PruefMeldung> meldungen)
        {
            List<Posten> liste = alle.Where(p => p.Gruppe == gruppe && !p.Verworfen).ToList();
            double flaeche = 0.0, brutto = 0.0, abzug = 0.0, ua = 0.0, mitU = 0.0, ohneU = 0.0;
            int n = 0, nMitU = 0;
            foreach (Posten p in liste)
            {
                if (!p.NettoM2.HasValue) continue;
                double a = p.NettoM2.Value;
                flaeche += a;
                brutto += p.BruttoM2 ?? a;
                abzug += p.AbzugM2;
                n++;
                if (p.U.HasValue) { ua += p.U.Value * a; mitU += a; nMitU++; }
                else ohneU += a;
            }

            GebaeudeFeldzeile zf = z[feldFlaeche];
            zf.Wert = flaeche;
            zf.Herkunft = datei;
            if (abzug > 0.0)
            {
                zf.Bruttowert = brutto;
                zf.Beleg = new GebaeudeBeleg("GIMP_BELEG_FLAECHE_NETTO", Zahl(n), Zahl(brutto), Zahl(abzug));
            }
            else
                zf.Beleg = new GebaeudeBeleg("GIMP_BELEG_FLAECHE", Zahl(n));

            GebaeudeFeldzeile zu = z[feldU];
            Vorgabe(zu, k);
            double gesamt = mitU + ohneU;
            if (gesamt > 0.0 && mitU > 0.0 && ohneU <= ANTEIL_OHNE_U_GRENZE * gesamt)
                Setzen(zu, ua / mitU, datei, new GebaeudeBeleg("GIMP_BELEG_U_GEWICHTET", Zahl(nMitU), Zahl(mitU)));
            else
            {
                VorgabeUebernehmen(zu);
                if (gesamt > 0.0 && mitU > 0.0)
                {
                    string anteil = Zahl(Math.Round(100.0 * ohneU / gesamt, 1));
                    zu.Beleg = new GebaeudeBeleg("GIMP_BELEG_U_UNVOLLSTAENDIG", anteil, Zahl(ua / mitU));
                    meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "U_UNVOLLSTAENDIG",
                        zu.Zielfeld, anteil));
                }
            }
        }

        private static void Grundrand(List<Posten> aktiv, Importherkunft datei, Dictionary<string, GebaeudeFeldzeile> z,
                                      List<PruefMeldung> meldungen)
        {
            List<Posten> grund = aktiv.Where(p => p.Gruppe == Huelle.Grund).ToList();
            if (grund.Count == 0) return;
            double erdreich = grund.Where(p => p.GrundRand == DbWerte.GRUND_ERDREICH).Sum(p => p.NettoM2 ?? 0.0);
            double keller = grund.Where(p => p.GrundRand == DbWerte.GRUND_KELLER).Sum(p => p.NettoM2 ?? 0.0);
            bool gemischt = grund.Any(p => p.GrundRand == DbWerte.GRUND_ERDREICH) && grund.Any(p => p.GrundRand == DbWerte.GRUND_KELLER);
            string wahl = keller > erdreich ? DbWerte.GRUND_KELLER
                        : erdreich > keller ? DbWerte.GRUND_ERDREICH
                        : grund.Any(p => p.GrundRand == DbWerte.GRUND_ERDREICH) ? DbWerte.GRUND_ERDREICH : DbWerte.GRUND_KELLER;

            GebaeudeFeldzeile zr = z[GebaeudeZielfelder.GRUND_RANDBEDINGUNG];
            zr.Textwert = wahl;
            zr.Herkunft = datei;
            zr.Beleg = new GebaeudeBeleg("GIMP_BELEG_RANDBEDINGUNG", Zahl(erdreich), Zahl(keller));
            if (gemischt)
            {
                zr.Markieren(PruefStufe.Warnung);
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "RANDBEDINGUNG_GEMISCHT",
                    Zahl(erdreich), Zahl(keller), wahl));
            }
        }

        // ==================================================================
        //  Vorgabe-Rückfälle der Hüllflächen (Umsetzungskonzept 3.4, Spalte „Rückfall")
        // ==================================================================

        /// <summary>
        /// <b>Die Rückfälle, wenn für eine Gruppe KEINE Fläche gelesen ist</b> — kein Bauteil der Gruppe
        /// trägt eine Fläche, sei es, dass die Mengen fehlen (3.5 Nr. 5), sei es, dass die Datei gar
        /// keines führt. Eine Teilsumme (einige Bauteile mit, andere ohne Fläche) bleibt stehen und ist
        /// gelb markiert — eine gelesene Teilfläche und eine Vorgabe werden nicht vermischt.
        ///
        /// <list type="bullet">
        /// <item><b>Dach:</b> die Grundfläche des obersten Geschosses mit beheizten Räumen — seine
        /// Bruttogrundfläche aus der Datei, sonst die Summe seiner beheizten Räume.</item>
        /// <item><b>Grundfläche:</b> Nutzfläche ÷ Zahl der Geschosse mit beheizten Räumen; führt die Datei
        /// kein einziges Bodenbauteil, ist auch die Randbedingung eine Vorgabe (Erdreich, 3.4).</item>
        /// <item><b>Sonstige Flächen:</b> 0.</item>
        /// </list>
        ///
        /// <para>Jede Vorgabe trägt Herkunft „Vorgabe" und ihren Beleg; lässt sich der Rückfall selbst
        /// nicht bestimmen (kein Geschoss, keine Nutzfläche), bleibt die Zeile, wie die Gruppe sie
        /// gebildet hat. Der U-Wert der Gruppe fällt dann ohnehin auf die Vorgabe der Klasse.</para>
        /// </summary>
        private static void Flaechenrueckfaelle(List<Posten> aktiv, List<Posten> sonstige, AbbildGebaeude g,
                                                List<AbbildRaum> beheizt, Dictionary<string, GebaeudeFeldzeile> z)
        {
            if (!aktiv.Any(p => p.Gruppe == Huelle.Dach && p.NettoM2.HasValue))
            {
                AbbildGeschoss oben = OberstesGeschoss(g, beheizt);
                if (oben != null && oben.GrundflaecheM2 > 0.0)
                    FlaecheVorgeben(z[GebaeudeZielfelder.FLAECHE_DACH], oben.GrundflaecheM2.Value,
                        new GebaeudeBeleg("GIMP_BELEG_DACH_VORGABE_GESCHOSS", oben.Anzeigename, Zahl(oben.GrundflaecheM2.Value)));
                else if (oben != null)
                {
                    List<AbbildRaum> dort = beheizt.Where(r => string.Equals(r.GeschossKennung, oben.Kennung, StringComparison.Ordinal)).ToList();
                    if (dort.Count > 0 && dort.All(r => r.FlaecheM2 > 0.0))
                    {
                        double summe = dort.Sum(r => r.FlaecheM2.Value);
                        FlaecheVorgeben(z[GebaeudeZielfelder.FLAECHE_DACH], summe,
                            new GebaeudeBeleg("GIMP_BELEG_DACH_VORGABE_RAEUME", oben.Anzeigename, Zahl(dort.Count), Zahl(summe)));
                    }
                }
            }

            List<Posten> grund = aktiv.Where(p => p.Gruppe == Huelle.Grund).ToList();
            if (!grund.Any(p => p.NettoM2.HasValue))
            {
                double? nutzflaeche = z[GebaeudeZielfelder.NUTZFLAECHE].Wert;
                int geschosse = beheizt.Select(r => r.GeschossKennung).Where(s => s != null).Distinct(StringComparer.Ordinal).Count();
                if (nutzflaeche > 0.0 && geschosse > 0)
                {
                    FlaecheVorgeben(z[GebaeudeZielfelder.FLAECHE_GRUND], nutzflaeche.Value / geschosse,
                        new GebaeudeBeleg("GIMP_BELEG_GRUND_VORGABE", Zahl(nutzflaeche.Value), Zahl(geschosse)));
                    if (grund.Count == 0)
                    {
                        // Ohne jedes Bodenbauteil hat Grundrand nichts gesetzt: Rückfall Erdreich (3.4).
                        GebaeudeFeldzeile zr = z[GebaeudeZielfelder.GRUND_RANDBEDINGUNG];
                        zr.Textwert = DbWerte.GRUND_ERDREICH;
                        zr.Herkunft = Importherkunft.Vorgabe;
                        zr.Beleg = new GebaeudeBeleg("GIMP_BELEG_RANDBEDINGUNG_VORGABE");
                    }
                }
            }

            List<Posten> uebrige = sonstige.Where(p => !p.Verworfen).ToList();
            if (!uebrige.Any(p => p.NettoM2.HasValue))
                FlaecheVorgeben(z[GebaeudeZielfelder.FLAECHE_SONSTIGE], 0.0,
                    new GebaeudeBeleg("GIMP_BELEG_SONSTIGE_VORGABE", Zahl(uebrige.Count)));
        }

        /// <summary>
        /// Das oberste Geschoss mit beheizten Räumen — nach der Höhenlage (3.5 Nr. 2: nur die Reihenfolge);
        /// <c>null</c>, wenn die Räume keinem bekannten Geschoss angehören oder eine Lage fehlt.
        /// </summary>
        private static AbbildGeschoss OberstesGeschoss(AbbildGebaeude g, List<AbbildRaum> beheizt)
        {
            var kennungen = new HashSet<string>(beheizt.Select(r => r.GeschossKennung).Where(s => s != null), StringComparer.Ordinal);
            List<AbbildGeschoss> liste = g.Geschosse.Where(s => kennungen.Contains(s.Kennung)).ToList();
            if (liste.Count == 1) return liste[0];
            if (liste.Count == 0 || liste.Any(s => !s.LageM.HasValue)) return null;
            AbbildGeschoss oben = liste[0];
            foreach (AbbildGeschoss s in liste)
                if (s.LageM.Value > oben.LageM.Value) oben = s;
            return oben;
        }

        private static void FlaecheVorgeben(GebaeudeFeldzeile zeile, double wert, GebaeudeBeleg beleg)
        {
            zeile.VorgabeWert = wert;
            zeile.VorgabeBeleg = beleg;
            zeile.Bruttowert = null;
            VorgabeUebernehmen(zeile);
        }

        // ==================================================================
        //  Fenster
        // ==================================================================

        /// <summary>
        /// Der Sektor eines Azimuts: 0 Nord, 1 Ost, 2 Süd, 3 West — nächste Mitte, Breite 90°, die
        /// Grenze gehört zum größeren Sektor (45° → Ost, 135° → Süd, 225° → West, 315° → Nord).
        /// </summary>
        internal static int Sektor(double azimutGrad)
        {
            double a = azimutGrad % 360.0;
            if (a < 0.0) a += 360.0;
            return (int)Math.Floor((a + 45.0) / 90.0) % 4;
        }

        private static readonly string[] Sektorfelder =
        {
            GebaeudeZielfelder.FENSTER_NORD, GebaeudeZielfelder.FENSTER_OST,
            GebaeudeZielfelder.FENSTER_SUED, GebaeudeZielfelder.FENSTER_WEST,
        };

        private static void Fenster(List<Posten> aktiv, Importherkunft datei, char? k,
                                    Dictionary<string, GebaeudeFeldzeile> z, List<PruefMeldung> meldungen)
        {
            var sektor = new double[4];
            var zahl = new int[4];
            double ohneAzimut = 0.0, uA = 0.0, mitU = 0.0, gA = 0.0, mitG = 0.0, gesamt = 0.0;
            int nOhne = 0, nMitU = 0, nMitG = 0;

            foreach (Fensterposten f in aktiv.SelectMany(p => p.Fenster))
            {
                if (!f.FlaecheM2.HasValue) continue;
                double a = f.FlaecheM2.Value;
                gesamt += a;
                if (f.AzimutGrad.HasValue)
                {
                    int s = Sektor(f.AzimutGrad.Value);
                    sektor[s] += a;
                    zahl[s]++;
                }
                else
                {
                    ohneAzimut += a;
                    nOhne++;
                    for (int s = 0; s < 4; s++) sektor[s] += a / 4.0;
                }
                if (f.U.HasValue) { uA += f.U.Value * a; mitU += a; nMitU++; }
                if (f.G.HasValue) { gA += f.G.Value * a; mitG += a; nMitG++; }
            }

            for (int s = 0; s < 4; s++)
                Setzen(z[Sektorfelder[s]], sektor[s], datei, new GebaeudeBeleg("GIMP_BELEG_FENSTER_SEKTOR", Zahl(zahl[s])));
            Setzen(z[GebaeudeZielfelder.FENSTER_GESAMT], sektor.Sum(), datei, new GebaeudeBeleg("GIMP_BELEG_FENSTER_SUMME"));
            if (nOhne > 0)
            {
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "FENSTER_OHNE_AZIMUT",
                    Zahl(nOhne), Zahl(ohneAzimut)));
                foreach (string f in Sektorfelder) z[f].Markieren(PruefStufe.Warnung);
            }

            Gewichtet(z[GebaeudeZielfelder.U_FENSTER], uA, mitU, nMitU, gesamt, datei, k, meldungen);
            // Der g-Wert aus gbXML trägt einen Hinweis im Beleg: SolarHeatGainCoeff gilt dort meist für
            // das ganze Fenster, EPOS mindert zusätzlich um den Rahmenanteil (Protokoll G4, Entscheid 7).
            Gewichtet(z[GebaeudeZielfelder.G_WERT], gA, mitG, nMitG, gesamt, datei, k, meldungen,
                      datei == Importherkunft.GbXml ? "GIMP_BELEG_G_GBXML" : "GIMP_BELEG_U_GEWICHTET");
        }

        /// <summary>Flächengewichtetes Mittel mit der 30-%-Regel; sonst die Vorgabe der Klasse.</summary>
        private static void Gewichtet(GebaeudeFeldzeile zeile, double summeWA, double mitWert, int nMitWert, double gesamt,
                                      Importherkunft datei, char? k, List<PruefMeldung> meldungen,
                                      string belegSchluessel = "GIMP_BELEG_U_GEWICHTET")
        {
            Vorgabe(zeile, k);
            double ohne = gesamt - mitWert;
            if (gesamt > 0.0 && mitWert > 0.0 && ohne <= ANTEIL_OHNE_U_GRENZE * gesamt)
            {
                Setzen(zeile, summeWA / mitWert, datei, new GebaeudeBeleg(belegSchluessel, Zahl(nMitWert), Zahl(mitWert)));
                return;
            }
            VorgabeUebernehmen(zeile);
            if (gesamt > 0.0 && mitWert > 0.0)
            {
                string anteil = Zahl(Math.Round(100.0 * ohne / gesamt, 1));
                zeile.Beleg = new GebaeudeBeleg("GIMP_BELEG_U_UNVOLLSTAENDIG", anteil, Zahl(summeWA / mitWert));
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "U_UNVOLLSTAENDIG", zeile.Zielfeld, anteil));
            }
        }

        // ==================================================================
        //  Wärmebrücken, Lüftung, Sollwerte
        // ==================================================================

        private static void Waermebruecken(char? k, Dictionary<string, GebaeudeFeldzeile> z)
        {
            foreach (string f in new[] { GebaeudeZielfelder.PSI_FENSTER_WAND, GebaeudeZielfelder.PSI_WAND_DACH,
                                         GebaeudeZielfelder.PSI_AUSSENWAND_KELLER })
            {
                Vorgabe(z[f], k);
                VorgabeUebernehmen(z[f]);
            }
            // U15: Die Anschlusslängen bleiben leer — eine geratene Länge sähe aus wie eine gemessene.
            foreach (string f in new[] { GebaeudeZielfelder.LAENGE_FENSTER_WAND, GebaeudeZielfelder.LAENGE_WAND_DACH,
                                         GebaeudeZielfelder.LAENGE_AUSSENWAND_KELLER })
                z[f].Beleg = new GebaeudeBeleg("GIMP_BELEG_LAENGE_LEER");
        }

        private static void Lueftung(List<AbbildRaum> beheizt, Importherkunft datei, Dictionary<string, GebaeudeFeldzeile> z,
                                     List<PruefMeldung> meldungen)
        {
            // D12: der ganze gelesene Luftwechsel auf die Infiltration, die Nutzerlüftung bleibt leer.
            z[GebaeudeZielfelder.LUFTWECHSEL_NUTZER].Beleg = new GebaeudeBeleg("GIMP_BELEG_NUTZERLUEFTUNG_LEER");

            List<AbbildRaum> mit = beheizt.Where(r => r.LuftwechselJeH.HasValue).ToList();
            if (mit.Count == 0)
            {
                // Nichts gelesen (IFC liest den Luftwechsel nie, Umsetzungskonzept 3.4): die Vorgaben des
                // Stundenmodells — Infiltration 0,3 und Nutzerlüftung 0,4 1/h —, als Vorgabe gekennzeichnet.
                VorgabeLuftwechsel(z[GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION], GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION);
                VorgabeLuftwechsel(z[GebaeudeZielfelder.LUFTWECHSEL_NUTZER], GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER);
                return;
            }
            if (mit.Count < beheizt.Count)
            {
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "LUFTWECHSEL_UNVOLLSTAENDIG",
                    Zahl(mit.Count), Zahl(beheizt.Count - mit.Count)));
                return;
            }

            // Volumengewichtet (Luftstrom = n·V); ohne Volumina flächengewichtet, sonst gleich gewichtet.
            Func<AbbildRaum, double> gewicht;
            string beleg;
            if (mit.All(r => r.VolumenM3 > 0.0)) { gewicht = r => r.VolumenM3.Value; beleg = "GIMP_BELEG_LUFTWECHSEL"; }
            else if (mit.All(r => r.FlaecheM2 > 0.0)) { gewicht = r => r.FlaecheM2.Value; beleg = "GIMP_BELEG_LUFTWECHSEL_FLAECHE"; }
            else { gewicht = r => 1.0; beleg = "GIMP_BELEG_LUFTWECHSEL_MITTEL"; }
            double summeW = mit.Sum(gewicht);
            Setzen(z[GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION], mit.Sum(r => r.LuftwechselJeH.Value * gewicht(r)) / summeW,
                datei, new GebaeudeBeleg(beleg, Zahl(mit.Count)));
        }

        private static void VorgabeLuftwechsel(GebaeudeFeldzeile zeile, double wert)
        {
            zeile.VorgabeWert = wert;
            zeile.VorgabeBeleg = new GebaeudeBeleg("GIMP_BELEG_LUFTWECHSEL_VORGABE");
            VorgabeUebernehmen(zeile);
        }

        private static void Sollwerte(List<AbbildRaum> beheizt, Importherkunft datei, Dictionary<string, GebaeudeFeldzeile> z,
                                      List<PruefMeldung> meldungen)
        {
            List<double> werte = beheizt.Where(r => r.SollHeizenC.HasValue).Select(r => r.SollHeizenC.Value).ToList();
            if (werte.Count == 0) return;
            double min = werte.Min(), max = werte.Max();
            if (werte.Count == beheizt.Count && max - min <= 1e-9)
                Setzen(z[GebaeudeZielfelder.SOLL_TAG], min, datei, new GebaeudeBeleg("GIMP_BELEG_SOLLWERT", Zahl(werte.Count)));
            else
                meldungen.Add(new PruefMeldung(PruefStufe.Info, GebaeudeImportAblauf.MELDUNG + "SOLLWERT_UNEINHEITLICH",
                    Zahl(min), Zahl(max), Zahl(werte.Count), Zahl(beheizt.Count - werte.Count)));
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static int Zonenzahl(string regel, AbbildGebaeude g, int beheizteRaeume)
        {
            switch (regel)
            {
                case GebaeudeImportProfil.ZONENREGEL_X1: return g.ZahlZonen;
                case GebaeudeImportProfil.ZONENREGEL_X2: return g.ZahlGeschosseMitRaeumen;
                case GebaeudeImportProfil.ZONENREGEL_X3: return beheizteRaeume;
                default: return 1;
            }
        }

        private static char? Klasse(char? klasse)
        {
            if (!klasse.HasValue) return null;
            char c = char.ToUpperInvariant(klasse.Value);
            return c >= 'A' && c <= 'U' ? c : (char?)null;
        }

        private static void Setzen(GebaeudeFeldzeile zeile, double? wert, Importherkunft herkunft, GebaeudeBeleg beleg)
        {
            zeile.Wert = wert;
            zeile.Herkunft = wert.HasValue ? herkunft : Importherkunft.Leer;
            zeile.Beleg = beleg;
        }

        private static void Vorgabe(GebaeudeFeldzeile zeile, char? k)
        {
            zeile.VorgabeWert = GebaeudeVorgaben.Wert(k, zeile.Zielfeld);
            Baualtersvorgabe v = GebaeudeVorgaben.Fuer(k);
            zeile.VorgabeBeleg = zeile.VorgabeWert.HasValue && v != null
                ? new GebaeudeBeleg("GIMP_BELEG_VORGABE_KLASSE", v.Klasse.ToString(), Zahl(v.Katalogsaetze))
                : null;
        }

        private static void VorgabeUebernehmen(GebaeudeFeldzeile zeile)
        {
            zeile.Wert = zeile.VorgabeWert;
            zeile.Herkunft = zeile.VorgabeWert.HasValue ? Importherkunft.Vorgabe : Importherkunft.Leer;
            zeile.Beleg = zeile.VorgabeBeleg;
        }

        private static string FeldDerGruppe(Huelle g)
        {
            switch (g)
            {
                case Huelle.Aussenwand: return GebaeudeZielfelder.FLAECHE_AUSSENWAND;
                case Huelle.Dach: return GebaeudeZielfelder.FLAECHE_DACH;
                case Huelle.Grund: return GebaeudeZielfelder.FLAECHE_GRUND;
                default: return GebaeudeZielfelder.FLAECHE_SONSTIGE;
            }
        }

        private static void Zaehlen(SortedDictionary<string, double[]> zaehler, string schluessel, AbbildBauteil s)
        {
            if (!zaehler.TryGetValue(schluessel, out double[] w)) zaehler[schluessel] = w = new double[2];
            w[0] += 1.0;
            w[1] += s.BruttoflaecheM2 ?? 0.0;
        }

        private static string Liste(List<string> kennungen)
            => kennungen.Count <= 5 ? string.Join(", ", kennungen) : string.Join(", ", kennungen.Take(5)) + ", …";

        private static string Zahl(double w) => GebaeudeImportAblauf.Zahl(w);

        private static string Zahl(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
