using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E9a (Schritt B, Entscheid E9a‑Q2 Lesart a; Konzept Wirtschaftlichkeit § 2.11.5,
    /// Zeile „Mengen") — <b>der Mengenfaktor eines Szenarios</b>: ein einheitlicher Faktor
    /// f = 1 + Menge/100 auf das MENGENGERÜST des Simulationsergebnisses einer Variante.
    ///
    /// <para><b>Die EINE Stelle, an der der Faktor die Mengen trifft.</b> Skaliert wird eine
    /// KOPIE des Ergebnisbaums und der Stundenreihen — Erzeugung von Strom und Wärme,
    /// Einspeisung, Bezug, Brennstoffeinsatz, Hilfsenergie, die Vollbenutzungsstunden (sie
    /// sind Energie ÷ Nennleistung) und die Lastgänge samt der Bezugsspitze, die aus ihnen
    /// gebildet ist. Danach laufen dieselben Rechenwege wie im Erwartungsfall über die
    /// skalierten Mengen: Energiekosten (Arbeitspreis × Menge), Einspeiseerlös, KWK-Zuschlag
    /// mit Jahresdeckel und Kontingent, Steuergutschriften, CO₂-Abgabe, PV-Vergütung,
    /// Betriebskosten, die sich an Endenergie, Stunden oder kWh bemessen. <b>Die Mengen treffen
    /// Preise, Sätze und Kontingente also erst NACH dem Faktor</b> — deshalb bleiben
    /// Festbeträge (Grundpreis, Pauschale nach § 9 KWKG, Zuschuss, Investition), die
    /// Gerätedaten (vorgehaltene Anschlussleistung der Brennstoffträger, Nennleistungen) und
    /// die gesetzlichen Sätze stehen, und ein Deckel, der greift, begrenzt auch das Szenario.
    /// Die Simulation selbst wird nicht ein zweites Mal gerechnet.</para>
    ///
    /// <para><b>Nicht skaliert</b> werden Leistungen aus Gerätedaten, Prozentwerte
    /// (Deckungsgrade, Nutzungsgrade), Temperaturen, Füllstände und Flächen. Die
    /// Ertragsänderung des Satzes bleibt eigenständig; sie wirkt wie bisher auf die Erlöszeilen
    /// der Eingabe.</para>
    ///
    /// <para><b>Ohne Pflege (f = 1,0) wird nichts kopiert</b> — die Aufrufer geben dann die
    /// Originale weiter, und jede Zahl ist bitgleich.</para>
    /// </summary>
    internal static class SzenarioMengen
    {
        /// <summary>Die Energiereihen des Zeitreihensatzes [kWh je Stunde], die der Faktor
        /// trifft. Füllstände, Temperaturen und Deckungsanteile bleiben.</summary>
        private static readonly string[] ENERGIEREIHEN =
        {
            ZeitreihenSatz.WAERMEBEDARF, ZeitreihenSatz.STROMBEDARF,
            ZeitreihenSatz.WP_WAERME, ZeitreihenSatz.WP_STROM, ZeitreihenSatz.HEIZSTAB,
            ZeitreihenSatz.BHKW_WAERME, ZeitreihenSatz.BHKW_STROM, ZeitreihenSatz.BHKW_UEBERSCHUSS,
            ZeitreihenSatz.KESSEL_WAERME, ZeitreihenSatz.SOLAR_WAERME,
            ZeitreihenSatz.PV_GENUTZT, ZeitreihenSatz.PV_UEBERSCHUSS,
            ZeitreihenSatz.NETZEINSPEISUNG, ZeitreihenSatz.BATTERIE_EINSPEISUNG,
            ZeitreihenSatz.PV_ABREGELUNG, ZeitreihenSatz.NETZBEZUG, ZeitreihenSatz.WAERMEREST
        };

        /// <summary>
        /// Die Variante mit skaliertem Mengengerüst — eine flache Kopie, deren Ergebnisbaum und
        /// Stundenreihen skaliert sind. <b>f = 1,0 gibt dieselbe Referenz zurück.</b> Die
        /// Energiekosten der Kopie rechnet der Aufrufer neu (<see cref="KostenEmissionRechner"/>),
        /// weil erst dort Menge und Preis zusammenkommen.
        /// </summary>
        internal static VariantenDaten Variante(VariantenDaten v, double faktor)
        {
            if (v == null || faktor == 1.0) return v;
            VariantenDaten k = v.Kopie();
            k.Ergebnis = Ergebnis(v.Ergebnis, faktor);
            k.Zeitreihen = Zeitreihen(v.Zeitreihen, faktor);
            return k;
        }

        /// <summary>
        /// Der Ergebnisbaum mit skalierten Energiemengen [MWh] und Vollbenutzungsstunden [h].
        /// <b>f = 1,0 gibt dieselbe Referenz zurück.</b> Speicher- und Gebäudelisten werden
        /// geteilt, nicht kopiert — die Wirtschaftlichkeit liest sie nicht.
        /// </summary>
        internal static ErgebnisModel Ergebnis(ErgebnisModel m, double f)
        {
            if (m == null || f == 1.0) return m;
            var k = new ErgebnisModel
            {
                ID = m.ID,
                ID_Projekt = m.ID_Projekt,
                Bezeichner = m.Bezeichner,
                Zeitstempel = m.Zeitstempel,
                ID_Klimaregion = m.ID_Klimaregion,
                Sim_Energiebedarf = m.Sim_Energiebedarf,
                Sim_Waermepumpe = m.Sim_Waermepumpe,
                Sim_Heizkessel = m.Sim_Heizkessel,
                Sim_Solarthermie = m.Sim_Solarthermie,
                Sim_BHKW = m.Sim_BHKW,
                Sim_PV = m.Sim_PV,
                Sim_Stromspeicher = m.Sim_Stromspeicher,
                Pufferspeicher = m.Pufferspeicher,
                Stromspeicher = m.Stromspeicher,
                Gebaeude = m.Gebaeude
            };

            if (m.Energiebedarf != null)
            {
                ErgebnisEnergiebedarfModel e = m.Energiebedarf;
                k.Energiebedarf = new ErgebnisEnergiebedarfModel
                {
                    Waermebedarf_Gesamt = e.Waermebedarf_Gesamt * f,
                    Waermelast_Max = e.Waermelast_Max,
                    Strombedarf_Gesamt = e.Strombedarf_Gesamt * f,
                    Strombedarf_Max = e.Strombedarf_Max,
                    Waermerestbedarf = e.Waermerestbedarf * f,
                    Stromrestbedarf = e.Stromrestbedarf * f,
                    Waermebedarf_Kanal = Mal(e.Waermebedarf_Kanal, f),
                    Kaeltebedarf_Gesamt = e.Kaeltebedarf_Gesamt * f,
                    Kaeltelast_Max = e.Kaeltelast_Max,
                    Kaelterestbedarf = e.Kaelterestbedarf * f
                };
            }

            if (m.Waermepumpe != null)
            {
                ErgebnisWaermepumpeModel w = m.Waermepumpe;
                var wk = new ErgebnisWaermepumpeModel
                {
                    Waermebedarf = w.Waermebedarf * f,
                    Restwaermebedarf = w.Restwaermebedarf * f,
                    Waermeproduktion_WP = w.Waermeproduktion_WP * f,
                    Stromverbrauch_WP = w.Stromverbrauch_WP * f,
                    Stromverbrauch_Heizstab = w.Stromverbrauch_Heizstab * f,
                    Kapazitaet_Pufferspeicher = w.Kapazitaet_Pufferspeicher,
                    Min_Spitzenkesselleistung = w.Min_Spitzenkesselleistung,
                    Waermebedarfsdeckung = w.Waermebedarfsdeckung,
                    Vollbenutzungsstunden = w.Vollbenutzungsstunden * f,
                    Bivalenzpunkt = w.Bivalenzpunkt,
                    Deckung_Kanal = w.Deckung_Kanal,
                    // KU2 (Kühlkonzept 6.1–6.3, E34/E35): die Kälteseite skaliert wie die Wärmeseite;
                    // Kühlträger und Abrechnungsart sind Konfiguration und bleiben. Ohne sie verlöre
                    // ein Mengenszenario Kosten und Emissionen des Kältestroms eines Kühlträgers.
                    Kaelteproduktion_WP = Mal(w.Kaelteproduktion_WP, f),
                    Stromverbrauch_Kuehlung = Mal(w.Stromverbrauch_Kuehlung, f)
                };
                if (w.Module != null)
                    foreach (ErgebnisWaermepumpeModulModel mo in w.Module)
                        wk.Module.Add(mo == null ? null : new ErgebnisWaermepumpeModulModel
                        {
                            Modul = mo.Modul,
                            Leistung = mo.Leistung,
                            Waermeproduktion = mo.Waermeproduktion * f,
                            Stromverbrauch = mo.Stromverbrauch * f,
                            Heizstab = mo.Heizstab * f,
                            Betriebsstunden = mo.Betriebsstunden * f,
                            Kaelteproduktion = Mal(mo.Kaelteproduktion, f),
                            Stromverbrauch_Kuehlung = Mal(mo.Stromverbrauch_Kuehlung, f),
                            Kaeltestrom_Netzbezug = Mal(mo.Kaeltestrom_Netzbezug, f),
                            Kuehl_CarrierId = mo.Kuehl_CarrierId,
                            Kuehl_EigenerZaehler = mo.Kuehl_EigenerZaehler
                        });
                k.Waermepumpe = wk;
            }

            if (m.BHKW != null)
            {
                ErgebnisBHKWModel b = m.BHKW;
                var bk = new ErgebnisBHKWModel
                {
                    Waermebedarf = b.Waermebedarf * f,
                    Restwaermebedarf = b.Restwaermebedarf * f,
                    Strombedarf = b.Strombedarf * f,
                    Reststrombedarf = b.Reststrombedarf * f,
                    Waermeproduktion = b.Waermeproduktion * f,
                    Waermeueberschuss = b.Waermeueberschuss * f,
                    Stromproduktion = b.Stromproduktion * f,
                    Betriebsstunden_Gesamt = b.Betriebsstunden_Gesamt * f,
                    Betriebsstunden_Durchschnitt = b.Betriebsstunden_Durchschnitt * f,
                    VbhElektrisch = b.VbhElektrisch * f,
                    Waermebedarfsdeckung = b.Waermebedarfsdeckung,
                    Strombedarfsdeckung = b.Strombedarfsdeckung,
                    Gasverbrauch = b.Gasverbrauch * f,
                    Oelverbrauch = b.Oelverbrauch * f,
                    Koks = b.Koks * f,
                    Rapsoelverbrauch = b.Rapsoelverbrauch * f,
                    Holzverbrauch = b.Holzverbrauch * f,
                    Kohle = b.Kohle * f,
                    Sonstigverbrauch = b.Sonstigverbrauch * f,
                    Pellets = b.Pellets * f,
                    TierischeFette = b.TierischeFette * f,
                    Deckung_Kanal = b.Deckung_Kanal
                };
                if (b.Module != null)
                    foreach (ErgebnisBHKWModulModel mo in b.Module)
                        bk.Module.Add(mo == null ? null : new ErgebnisBHKWModulModel
                        {
                            Modul = mo.Modul,
                            Waermeproduktion = mo.Waermeproduktion * f,
                            Stromproduktion = mo.Stromproduktion * f,
                            Brennstoff = mo.Brennstoff,
                            Verbrauch = mo.Verbrauch * f,
                            CarrierId = mo.CarrierId,
                            VbhThermisch = mo.VbhThermisch * f,
                            VbhElektrisch = mo.VbhElektrisch * f,
                            Hilfsenergie = mo.Hilfsenergie * f
                        });
                k.BHKW = bk;
            }

            if (m.Heizkessel != null)
            {
                ErgebnisHeizkesselModel h = m.Heizkessel;
                var hk = new ErgebnisHeizkesselModel
                {
                    Waermebedarf = h.Waermebedarf * f,
                    Restwaermebedarf = h.Restwaermebedarf * f,
                    Waermeproduktion = h.Waermeproduktion * f,
                    Strombedarf = h.Strombedarf * f,
                    Reststrombedarf = h.Reststrombedarf * f,
                    Waermebedarfsdeckung = h.Waermebedarfsdeckung,
                    Stromverbrauch = h.Stromverbrauch * f,
                    Maximale_Kesselleistung = h.Maximale_Kesselleistung,
                    Gasspitze = h.Gasspitze,
                    Quellwaerme = h.Quellwaerme * f,
                    Gasverbrauch = h.Gasverbrauch * f,
                    Oelverbrauch = h.Oelverbrauch * f,
                    Koks = h.Koks * f,
                    Rapsoelverbrauch = h.Rapsoelverbrauch * f,
                    Holzverbrauch = h.Holzverbrauch * f,
                    Kohle = h.Kohle * f,
                    Sonstigverbrauch = h.Sonstigverbrauch * f,
                    Pellets = h.Pellets * f,
                    TierischeFette = h.TierischeFette * f,
                    Deckung_Kanal = h.Deckung_Kanal
                };
                if (h.Module != null)
                    foreach (ErgebnisHeizkesselModulModel mo in h.Module)
                        hk.Module.Add(mo == null ? null : new ErgebnisHeizkesselModulModel
                        {
                            Modul = mo.Modul,
                            Waerme_Gas = mo.Waerme_Gas * f,
                            Waerme_Oel = mo.Waerme_Oel * f,
                            Waermeproduktion = mo.Waermeproduktion * f,
                            Brennstoff = mo.Brennstoff,
                            Verbrauch = mo.Verbrauch * f,
                            CarrierId = mo.CarrierId,
                            Jahresnutzungsgrad = mo.Jahresnutzungsgrad,
                            Hilfsenergie = mo.Hilfsenergie * f
                        });
                k.Heizkessel = hk;
            }

            if (m.Solarthermie != null)
            {
                ErgebnisSolarthermieModel s = m.Solarthermie;
                var sk = new ErgebnisSolarthermieModel
                {
                    Waermebedarf = s.Waermebedarf * f,
                    Restwaermebedarf = s.Restwaermebedarf * f,
                    Waermeproduktion = s.Waermeproduktion * f,
                    Waermebedarfsdeckung = s.Waermebedarfsdeckung,
                    Ueberschuss = s.Ueberschuss * f,
                    Deckung_Kanal = s.Deckung_Kanal
                };
                if (s.Module != null)
                    foreach (ErgebnisSolarthermieModulModel mo in s.Module)
                        sk.Module.Add(mo == null ? null : new ErgebnisSolarthermieModulModel
                        {
                            Modul = mo.Modul,
                            Flaeche = mo.Flaeche,
                            Anzahl = mo.Anzahl,
                            Waermeproduktion = mo.Waermeproduktion * f,
                            Ueberschuss = mo.Ueberschuss * f
                        });
                k.Solarthermie = sk;
            }

            if (m.Photovoltaik != null)
            {
                ErgebnisPhotovoltaikModel p = m.Photovoltaik;
                var pk = new ErgebnisPhotovoltaikModel
                {
                    Strombedarf = p.Strombedarf * f,
                    Reststrombedarf = p.Reststrombedarf * f,
                    Stromproduktion = p.Stromproduktion * f,
                    Strombedarfsdeckung = p.Strombedarfsdeckung,
                    Ueberschuss = p.Ueberschuss * f,
                    MaxSolareLeistung = p.MaxSolareLeistung
                };
                if (p.Module != null)
                    foreach (ErgebnisPhotovoltaikModulModel mo in p.Module)
                        pk.Module.Add(mo == null ? null : new ErgebnisPhotovoltaikModulModel
                        {
                            Modul = mo.Modul,
                            Flaeche = mo.Flaeche,
                            Anzahl = mo.Anzahl,
                            Stromproduktion = mo.Stromproduktion * f
                        });
                k.Photovoltaik = pk;
            }
            return k;
        }

        /// <summary>
        /// Der Zeitreihensatz mit skalierten Energiereihen und skalierter Bezugsspitze — die
        /// Spitze ist aus denselben Viertelstundenmengen gebildet wie der Netzbezug und folgt
        /// ihm deshalb. <b>f = 1,0 gibt dieselbe Referenz zurück.</b> Füllstände, Temperaturen
        /// und Beschriftungen bleiben geteilt.
        /// </summary>
        internal static ZeitreihenSatz Zeitreihen(ZeitreihenSatz z, double f)
        {
            if (z == null || f == 1.0) return z;
            var energie = new HashSet<string>(ENERGIEREIHEN, StringComparer.Ordinal);
            var k = new ZeitreihenSatz
            {
                Speicherreihen = z.Speicherreihen,
                Beschriftungen = z.Beschriftungen
            };
            foreach (KeyValuePair<string, double[]> r in z.Reihen)
            {
                // Die Kanalreihen des Bedarfs und der Deckung je Erzeuger sind ebenfalls
                // Energiemengen je Stunde (ZeitreihenExtraktor.Kanalreihe).
                bool skalieren = energie.Contains(r.Key) ||
                                 r.Key.StartsWith(ZeitreihenSatz.BEDARF_PRAEFIX, StringComparison.Ordinal) ||
                                 r.Key.StartsWith(ZeitreihenSatz.DECKUNG_PRAEFIX, StringComparison.Ordinal);
                k.Reihen[r.Key] = skalieren ? Mal(r.Value, f) : r.Value;
            }
            k.Bezugsspitze = Spitze(z.Bezugsspitze, f);

            // E35 (Konzept Gebäudesimulation N1.40): die eigenen Spitzen des Kältestroms folgen
            // ihrem Kältestrom wie die Bezugsspitze dem Netzbezug.
            if (z.Kaeltestromspitzen != null)
                foreach (KeyValuePair<int, Netzbezugsspitze> s in z.Kaeltestromspitzen)
                    if (s.Value != null) k.Kaeltestromspitzen[s.Key] = Spitze(s.Value, f);
            return k;
        }

        /// <summary>Eine Spitze mal f — neue Spitze, das Original bleibt; <c>null</c> bleibt <c>null</c>.</summary>
        private static Netzbezugsspitze Spitze(Netzbezugsspitze s, double f)
        {
            if (s == null) return null;
            var spitze = new Netzbezugsspitze { JahrKW = s.JahrKW * f };
            for (int mo = 0; mo < spitze.MonatKW.Length && s.MonatKW != null && mo < s.MonatKW.Length; mo++)
                spitze.MonatKW[mo] = s.MonatKW[mo] * f;
            return spitze;
        }

        /// <summary>Ein Wert mal f; <c>null</c> („nicht erhoben") bleibt <c>null</c>.</summary>
        private static double? Mal(double? wert, double f)
        {
            return wert.HasValue ? (double?)(wert.Value * f) : null;
        }

        /// <summary>Eine Reihe mal f — neue Reihe, das Original bleibt.</summary>
        private static double[] Mal(double[] reihe, double f)
        {
            if (reihe == null) return null;
            var neu = new double[reihe.Length];
            for (int i = 0; i < reihe.Length; i++) neu[i] = reihe[i] * f;
            return neu;
        }
    }
}
