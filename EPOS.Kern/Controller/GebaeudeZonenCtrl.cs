using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Zonen und Bauteile eines Projektgebäudes</b> (Gebäudesimulation Stufe G3, Schritt S-C;
    /// Softwarearchitektur 2.2 und 2.9, Mehrzonenkonzept 4.1). Bauform B: geordnete Kindlisten
    /// mit Rang, Kaskade nur zum Eltern, kein eigenes <c>ID_Projekt</c> am Kind (W16).
    ///
    /// <para><b>Zwei Lesewege</b> (2.9): <see cref="LesenJeGebaeude"/> für den Dialog,
    /// <see cref="LesenJeProjekt"/> für den Rechenkern — je EINE Abfrage für die Zonen und EINE
    /// für die Bauteile, sortiert nach (<c>ID_Gebaeude</c>, <c>Rang</c>) bzw.
    /// (<c>ID_Zone</c>, <c>Rang</c>); die Zuordnung geschieht im Speicher über die gelesenen Ids.
    /// Nie eine Abfrage je Zone — bei 150 Zonen und 3 000 Bauteilen wäre das der N+1-Fall.</para>
    ///
    /// <para><b>Der Schreibweg ist ein Aggregat je Gebäude mit Abgleich über die Ids</b> (Muster
    /// A6, Mehrzonenkonzept 4.1): Was in der Liste fehlt, wird gelöscht; was eine positive Id
    /// trägt, wird über diese Id geändert; was eine Id ≤ 0 trägt (eine vorläufige Zeile), wird
    /// angelegt. Alles in EINER Transaktion, der <c>Rang</c> lückenlos aus der
    /// Listenreihenfolge, die Schlüssel unangetastet — an ihnen hängen der Import
    /// (<c>Quellkennung</c>, Importzuordnung) und die Verweise der Bauteile. Das Löschen ist
    /// geteilt: Bauteile, die in der Liste fehlen, fallen ZUERST; Zonen, die fehlen, ZULETZT —
    /// so fällt ein Bauteil, das in eine andere Zone verschoben wurde, nicht über die Kaskade
    /// seiner alten Zone.</para>
    ///
    /// <para><b>Die Kaskadenfalle A1 ist gemessen</b> (24.09.2026): Kein gewöhnlicher
    /// Speicherweg eines Projektgebäudes löscht und legt die Zeile in <c>Tab_Gebaeude</c> neu an —
    /// die Gebäudeliste wird abgeglichen (<c>WizardCtrl.Schreibe_Projekt_ZuordungGebäude</c>),
    /// die Feld-Übernahme ändert zielgenau (<c>MerkmalUebernahmeCtrl</c>), der Katalogeditor
    /// schreibt den Katalog, und in der Betriebsart Projekt („Hülle und Zonen…") überschreibt er die
    /// Projektkopie zeilengenau (<c>GebaeudeStammCtrl.ProjektkopieUeberschreiben</c>, ein UPDATE unter
    /// derselben Id). Fällt die Zeile, dann weil das Gebäude aus dem Projekt genommen
    /// oder gegen einen anderen Katalogsatz getauscht wurde — dann gehen seine Zonen mit. Eine
    /// Rettung an der Löschstelle braucht es deshalb nicht; die Probe hält beide Fälle fest.</para>
    /// </summary>
    public sealed class GebaeudeZonenCtrl
    {
        /// <summary>Was ein Schreibversuch ergeben hat — dieselbe Form wie <see cref="BaustoffCtrl.Ergebnis"/>.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung)
        {
            internal static readonly Ergebnis Gut = new Ergebnis(true, "");
            internal static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "");
        }

        private static string ZonenspaltenSql(string alias)
            => string.Join(", ", new[] { "ID" }.Concat(ZonenSchema.Zonenspalten).Concat(Kuehlspalten())
                                             .Select(s => alias + ".\"" + s + "\""));

        /// <summary>
        /// Die drei Spalten der Kühlübergabe an der Zone (E37, Schritt 137;
        /// <see cref="KuehluebergabeSchema.SpaltenZone"/>) — gelesen und geschrieben NEBEN den
        /// Spalten von <see cref="ZonenSchema.Zonenspalten"/>, die unverändert bleiben; leer, solange
        /// die Datenbank den Schritt nicht trägt. So reisen sie über jeden Weg des Aggregats mit,
        /// NULL bleibt NULL.
        /// </summary>
        private static IReadOnlyList<string> Kuehlspalten()
            => GebaeudeZonenanschluss.KuehlspaltenVorhanden()
                ? KuehluebergabeSchema.SpaltenZone.Select(s => s.Key).ToList()
                : (IReadOnlyList<string>)Array.Empty<string>();

        private static string BauteilspaltenSql(string alias)
            => string.Join(", ", new[] { "ID" }.Concat(Bauteilspalten()).Select(s => alias + ".\"" + s + "\""));

        /// <summary>
        /// Die Spalten des Bauteils: die von <see cref="ZonenSchema.Bauteilspalten"/> und — mit dem
        /// Schemaschritt S-G (<see cref="ZonenkopplungSchema.SpaltenBauteil"/>) — Nachbarzone und
        /// Trennflächenzuordnung; ohne S-G (iOS migriert nicht nach) nur die ersten. Festgestellt
        /// über die gemerkte Probe <see cref="GebaeudeZonenanschluss.KopplungVorhanden"/>.
        /// </summary>
        private static IReadOnlyList<string> Bauteilspalten()
            => GebaeudeZonenanschluss.KopplungVorhanden()
                ? ZonenSchema.Bauteilspalten.Concat(ZonenkopplungSchema.SpaltenBauteil.Select(s => s.Key)).ToList()
                : ZonenSchema.Bauteilspalten;

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>
        /// Die Zonen EINES Gebäudes in Rangfolge, je samt Bauteilen in Rangfolge; nie <c>null</c>.
        /// Eine leere Liste heißt „keine Zone" — der Klassenweg. Zwei Abfragen.
        /// </summary>
        public List<ZoneModel> LesenJeGebaeude(int idGebaeude)
        {
            DataTable zonen = DataRepository.GetDataTable(
                "SELECT " + ZonenspaltenSql("z") + " FROM \"" + ZonenSchema.TAB_ZONE + "\" z " +
                "WHERE z.\"ID_Gebaeude\" = ? ORDER BY z.\"ID_Gebaeude\", z.\"Rang\", z.\"ID\"",
                new DbParam("@g", idGebaeude));
            DataTable bauteile = DataRepository.GetDataTable(
                "SELECT " + BauteilspaltenSql("b") + " FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" b " +
                "INNER JOIN \"" + ZonenSchema.TAB_ZONE + "\" z ON z.\"ID\" = b.\"ID_Zone\" " +
                "WHERE z.\"ID_Gebaeude\" = ? ORDER BY b.\"ID_Zone\", b.\"Rang\", b.\"ID\"",
                new DbParam("@g", idGebaeude));
            return Zusammenfuehren(zonen, bauteile);
        }

        /// <summary>
        /// <b>Der Leseweg des Rechenkerns</b> (2.9): die Zonen ALLER Gebäude eines Projekts samt
        /// Bauteilen, je Gebäude-Id in Rangfolge — ZWEI Abfragen über das ganze Projekt, je mit JOIN
        /// über <c>Tab_Gebaeude</c> und <c>Z_ProjektGebaeude</c>. Ein Gebäude ohne Zone fehlt im
        /// Ergebnis.
        /// </summary>
        public Dictionary<int, List<ZoneModel>> LesenJeProjekt(int idProjekt)
        {
            const string VERBUND =
                "INNER JOIN \"Tab_Gebaeude\" g ON g.\"ID\" = z.\"ID_Gebaeude\" " +
                "INNER JOIN \"Z_ProjektGebaeude\" p ON p.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE p.\"ID_Projekt\" = ? ";
            DataTable zonen = DataRepository.GetDataTable(
                "SELECT " + ZonenspaltenSql("z") + " FROM \"" + ZonenSchema.TAB_ZONE + "\" z " + VERBUND +
                "ORDER BY z.\"ID_Gebaeude\", z.\"Rang\", z.\"ID\"",
                new DbParam("@p", idProjekt));
            DataTable bauteile = DataRepository.GetDataTable(
                "SELECT " + BauteilspaltenSql("b") + " FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" b " +
                "INNER JOIN \"" + ZonenSchema.TAB_ZONE + "\" z ON z.\"ID\" = b.\"ID_Zone\" " + VERBUND +
                "ORDER BY b.\"ID_Zone\", b.\"Rang\", b.\"ID\"",
                new DbParam("@p", idProjekt));

            var ergebnis = new Dictionary<int, List<ZoneModel>>();
            foreach (ZoneModel z in Zusammenfuehren(zonen, bauteile))
            {
                if (!ergebnis.TryGetValue(z.ID_Gebaeude, out List<ZoneModel> liste))
                    ergebnis[z.ID_Gebaeude] = liste = new List<ZoneModel>();
                liste.Add(z);
            }
            return ergebnis;
        }

        /// <summary>
        /// Die Luftströme zwischen den Zonen EINES Gebäudes (Schritt S-G), sortiert nach
        /// (<c>ID_ZoneA</c>, <c>ID_ZoneB</c>); EINE Abfrage. Leer ohne S-G oder ohne Luftstrom.
        /// </summary>
        public List<ZonenluftstromModel> LuftstroemeJeGebaeude(int idGebaeude)
        {
            if (!GebaeudeZonenanschluss.KopplungVorhanden()) return new List<ZonenluftstromModel>();
            DataTable t = DataRepository.GetDataTable(
                "SELECT l.\"ID\", l.\"ID_ZoneA\", l.\"ID_ZoneB\", l.\"Volumenstrom\", z.\"ID_Gebaeude\" " +
                "FROM \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\" l " +
                "INNER JOIN \"" + ZonenSchema.TAB_ZONE + "\" z ON z.\"ID\" = l.\"ID_ZoneA\" " +
                "WHERE z.\"ID_Gebaeude\" = ? ORDER BY l.\"ID_ZoneA\", l.\"ID_ZoneB\", l.\"ID\"",
                new DbParam("@g", idGebaeude));
            return Luftstroeme(t).Select(x => x.Strom).ToList();
        }

        /// <summary>
        /// <b>Die Luftströme aller Gebäude eines Projekts</b> (Schritt S-G) je <c>Tab_Gebaeude.ID</c> —
        /// EINE Abfrage über das ganze Projekt, wie <see cref="LesenJeProjekt"/>; ein Gebäude ohne
        /// Luftstrom fehlt. Ein Paar gehört dem Gebäude seiner Zone A (beide Zonen liegen im selben
        /// Gebäude, <see cref="Zonenkopplungsregeln"/>).
        /// </summary>
        public Dictionary<int, List<ZonenluftstromModel>> LuftstroemeJeProjekt(int idProjekt)
        {
            var ergebnis = new Dictionary<int, List<ZonenluftstromModel>>();
            if (!GebaeudeZonenanschluss.KopplungVorhanden()) return ergebnis;
            DataTable t = DataRepository.GetDataTable(
                "SELECT l.\"ID\", l.\"ID_ZoneA\", l.\"ID_ZoneB\", l.\"Volumenstrom\", z.\"ID_Gebaeude\" " +
                "FROM \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\" l " +
                "INNER JOIN \"" + ZonenSchema.TAB_ZONE + "\" z ON z.\"ID\" = l.\"ID_ZoneA\" " +
                "INNER JOIN \"Tab_Gebaeude\" g ON g.\"ID\" = z.\"ID_Gebaeude\" " +
                "INNER JOIN \"Z_ProjektGebaeude\" p ON p.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE p.\"ID_Projekt\" = ? ORDER BY z.\"ID_Gebaeude\", l.\"ID_ZoneA\", l.\"ID_ZoneB\", l.\"ID\"",
                new DbParam("@p", idProjekt));
            foreach ((int gebaeude, ZonenluftstromModel strom) in Luftstroeme(t))
            {
                if (!ergebnis.TryGetValue(gebaeude, out List<ZonenluftstromModel> liste))
                    ergebnis[gebaeude] = liste = new List<ZonenluftstromModel>();
                liste.Add(strom);
            }
            return ergebnis;
        }

        private static IEnumerable<(int Gebaeude, ZonenluftstromModel Strom)> Luftstroeme(DataTable t)
        {
            if (t == null) yield break;
            foreach (DataRow r in t.Rows)
                yield return (Convert.ToInt32(r["ID_Gebaeude"], CultureInfo.InvariantCulture), new ZonenluftstromModel
                {
                    ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                    ID_ZoneA = Convert.ToInt32(r["ID_ZoneA"], CultureInfo.InvariantCulture),
                    ID_ZoneB = Convert.ToInt32(r["ID_ZoneB"], CultureInfo.InvariantCulture),
                    Volumenstrom = Convert.ToDouble(r["Volumenstrom"], CultureInfo.InvariantCulture),
                });
        }

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Die Neigung, die ohne Angabe gilt: Dach und Decke 0°, Bodenplatte 180°, alles übrige
        /// 90° (senkrecht) — Mehrzonenkonzept 4.2. Dieselbe Vorgabe, mit der der Bauteilweg eine
        /// leere Neigung liest (<see cref="BauteilEingang.VorgabeNeigung"/>).
        /// </summary>
        public static double NeigungVorgabe(string bauteilart)
            => GebaeudeZonenabbildung.ArtAusZeile(bauteilart) is Bauteilart art ? BauteilEingang.VorgabeNeigung(art) : 90.0;

        /// <summary>
        /// Heißt eine leere Randbedingung an dieser Bauteilart „innerhalb der Zone"? Ja an Innenwand
        /// und Decke, sonst heißt sie Außenluft — die Regel der Abbildung
        /// (<see cref="GebaeudeZonenabbildung.LeerHeisstInnen"/>), für die Vorgabe der Auswahl im
        /// Bauteildialog.
        /// </summary>
        public static bool LeerHeisstInnen(string bauteilart)
            => GebaeudeZonenabbildung.ArtAusZeile(bauteilart) is Bauteilart art && GebaeudeZonenabbildung.LeerHeisstInnen(art);

        /// <summary>
        /// Braucht das Bauteil einen Azimut? Genau dann, wenn es an die Außenluft grenzt und
        /// nicht waagerecht liegt — Neigung (bzw. ihre Vorgabe nach Bauteilart) weder 0° noch
        /// 180°. Ob es an die Außenluft grenzt, sagt die Regel der leeren Randbedingung
        /// (<see cref="GebaeudeZonenabbildung.RandAusZeile(string, string)"/>: NULL heißt Außenluft,
        /// an Innenwand und Decke „innerhalb der Zone"). Eine Wand ohne Azimut wird benannt
        /// abgelehnt, nicht auf Nord vorbelegt; an Erdreich, Zone, unbeheiztem Raum oder innerhalb
        /// der Zone trägt der Azimut keine Sonne.
        /// </summary>
        public static bool BrauchtAzimut(BauteilModel b)
        {
            if (b == null) return false;
            bool aussen = GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung) == Bauteilrand.Aussenluft;
            double neigung = b.Neigung ?? NeigungVorgabe(b.Bauteilart);
            return aussen && Math.Abs(neigung) > 1e-9 && Math.Abs(neigung - 180.0) > 1e-9;
        }

        /// <summary>
        /// Die Prüfung der Zonenliste eines Gebäudes vor dem Schreiben, ohne Datenbank — <c>null</c>
        /// = gültig, sonst die Meldung mit dem Namen der Zone bzw. des Bauteils. Sie gilt im Dialog
        /// (vor dem OK-Weg) und im Schreibweg (<see cref="SpeichernJeGebaeude"/>).
        ///
        /// <para><b>Über die ganze Liste</b> (Stufe G6a): höchstens
        /// <see cref="GebaeudeZonenregeln.Hoechstzahl()"/> Zonen je Gebäude; ab zwei Zonen braucht jede
        /// ihre Nutzfläche — leer hieße „die Fläche des Gebäudes" und zählte sie doppelt; eine positive
        /// Id einer Zone oder eines Bauteils steht höchstens einmal in der Liste, sonst schriebe der
        /// Abgleich dieselbe Zeile zweimal. Eine einzelne Zone prüft sich wie bisher.</para>
        ///
        /// <para><b>Zwischen den Zonen</b> (Stufe G6b): zuletzt die Regeln von
        /// <see cref="Zonenkopplungsregeln.Pruefen"/> — Nachbar im selben Gebäude, Randbedingung und
        /// Nachbar nur gemeinsam, Trennflächenbilanz, mindestens eine beheizte Zone, ψ·L der
        /// wärmeren Zone. Die Luftströme prüft die Überladung mit Luftstromliste.</para>
        /// </summary>
        public static string Pruefen(IList<ZoneModel> zonen)
            => Pruefen(zonen, GebaeudeZonenregeln.Hoechstzahl());

        /// <summary>Dasselbe mit ausdrücklicher Höchstzahl der Zonen (Prüfhilfe).</summary>
        internal static string Pruefen(IList<ZoneModel> zonen, int hoechstzahl)
        {
            List<ZoneModel> liste = (zonen ?? new List<ZoneModel>()).Where(z => z != null).ToList();
            if (liste.Count > hoechstzahl)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_ZU_VIELE,
                                     hoechstzahl.ToString(CultureInfo.CurrentCulture),
                                     liste.Count.ToString(CultureInfo.CurrentCulture));
            bool mehrere = liste.Count >= 2;
            var zonenIds = new HashSet<int>();
            var bauteilIds = new HashSet<int>();

            int nr = 0;
            foreach (ZoneModel z in zonen ?? new List<ZoneModel>())
            {
                nr++;
                if (z == null) continue;
                if (string.IsNullOrWhiteSpace(z.Bezeichner))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_NAME_LEER, nr);
                string zn = z.Bezeichner.Trim();
                if (z.ID > 0 && !zonenIds.Add(z.ID))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_ID_DOPPELT, zn, z.ID);
                string t = BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_NAME, zn, BaustoffSchema.LAENGE_BEZEICHNER)
                           ?? BaustoffCtrl.Laenge(MyResource.Resource.BAUTEIL_FELD_QUELLKENNUNG, z.Quellkennung, BaustoffSchema.LAENGE_QUELLKENNUNG)
                           ?? BaustoffCtrl.HerkunftPruefen(z.Herkunft);
                if (t != null) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_WERT, zn, t);
                if (z.Nutzflaeche.HasValue && (!(z.Nutzflaeche.Value > 0.0) || double.IsInfinity(z.Nutzflaeche.Value)))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_NUTZFLAECHE, zn);
                if (mehrere && !z.Nutzflaeche.HasValue)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_NUTZFLAECHE_PFLICHT, zn);
                if (z.Uebergabe_Art != null && z.Uebergabe_Art != DbWerte.UEBERGABE_IDEAL && z.Uebergabe_Art != DbWerte.UEBERGABE_RADIATOR
                    && z.Uebergabe_Art != DbWerte.UEBERGABE_FLAECHE && z.Uebergabe_Art != DbWerte.UEBERGABE_KONVEKTOR)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_UEBERGABEART, zn, z.Uebergabe_Art);
                if (z.Kuehl_Uebergabe_Art != null && !Waermeuebergabevorgaben.KuehlArten.Contains(z.Kuehl_Uebergabe_Art))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_KUEHLUEBERGABEART, zn, z.Kuehl_Uebergabe_Art);

                foreach (BauteilModel b in z.Bauteile ?? new List<BauteilModel>())
                {
                    if (b == null) continue;
                    if (string.IsNullOrWhiteSpace(b.Bezeichner))
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_NAME_LEER, zn);
                    string bn = b.Bezeichner.Trim();
                    if (b.ID > 0 && !bauteilIds.Add(b.ID))
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_ID_DOPPELT, bn, b.ID, zn);
                    string w =BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_NAME, bn, BaustoffSchema.LAENGE_BEZEICHNER)
                               ?? BaustoffCtrl.Laenge(MyResource.Resource.BAUTEIL_FELD_QUELLKENNUNG, b.Quellkennung, BaustoffSchema.LAENGE_QUELLKENNUNG)
                               ?? BaustoffCtrl.HerkunftPruefen(b.Herkunft)
                               ?? BauteilaufbauCtrl.BauteilartPruefen(b.Bauteilart, false);
                    if (w != null) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_WERT, bn, w);
                    if (!(b.Flaeche > 0) || double.IsInfinity(b.Flaeche))
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_FLAECHE, bn);
                    if (b.Randbedingung != null && !DbWerte.RANDBEDINGUNGEN.Contains(b.Randbedingung))
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_RANDBEDINGUNG, bn, b.Randbedingung);
                    if (!b.Azimut.HasValue && BrauchtAzimut(b))
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AZIMUT_FEHLT, bn);
                }
            }

            // Stufe G6b: die Regeln ZWISCHEN den Zonen (Nachbar, Trennflaechenbilanz, eine beheizte
            // Zone, psi-L der waermeren Zone) - die eine Regelklasse, die auch der Lauf ruft.
            return Zonenkopplungsregeln.Pruefen(liste);
        }

        /// <summary>
        /// Dasselbe samt den Luftströmen zwischen den Zonen (Stufe G6b; <see cref="Zonenkopplungsregeln"/>):
        /// nur Paare zweier Zonen der Liste, V̇ &gt; 0, jedes Paar einmal. <paramref name="luftstroeme"/>
        /// <c>null</c> heißt „nicht Teil dieser Prüfung".
        /// </summary>
        /// <param name="zonen">Die Zonen eines Gebäudes in Listenfolge.</param>
        /// <param name="luftstroeme">Die Luftströme zwischen ihnen; <c>null</c> = nicht geprüft.</param>
        /// <param name="sollTagGebaeude">Der Tagessollwert des Gebäudes [°C] für Zonen ohne eigenen (Regel ψ·L); <c>null</c> = keiner.</param>
        public static string Pruefen(IList<ZoneModel> zonen, IList<ZonenluftstromModel> luftstroeme, double? sollTagGebaeude = null)
        {
            string fehler = Pruefen(zonen, GebaeudeZonenregeln.Hoechstzahl());
            if (fehler != null) return fehler;
            List<ZoneModel> liste = (zonen ?? new List<ZoneModel>()).Where(z => z != null).ToList();
            return Zonenkopplungsregeln.Pruefen(liste, luftstroeme, sollTagGebaeude);
        }

        /// <summary>
        /// Was <see cref="Hinweise(IEnumerable{Zonenangabe}, double?)"/> von einer Zone liest: Name,
        /// Nutzfläche (<c>null</c> = die des Gebäudes) und je Bauteil Bauteilart und Randbedingung
        /// (Persistenzwerte).
        /// </summary>
        public sealed record Zonenangabe(string Bezeichner, double? Nutzflaeche,
                                         IReadOnlyList<(string Bauteilart, string Randbedingung)> Bauteile);

        /// <summary>
        /// <b>Die Hinweise über die Zonenliste eines Gebäudes</b> (Stufe G6a; Mehrzonenkonzept 5.3) —
        /// ein eigener Kanal neben <see cref="Pruefen(IList{ZoneModel})"/>: Ein Hinweis hält kein
        /// Speichern an. Ohne Datenbank; leer = nichts zu sagen.
        /// <list type="bullet">
        /// <item>Ab zwei Zonen: Weicht Σ Nutzfläche der Zonen um mindestens
        /// <see cref="GebaeudeZonenregeln.FLAECHENABWEICHUNG_HINWEIS"/> von der Nutzfläche des
        /// Gebäudes ab (<c>Tab_Gebaeude.Nutzflaeche</c>, der Wert des Laufs; E19), nennt der Hinweis
        /// beide Summen und die Abweichung — doppelt gezählte oder vergessene Räume. Eine einzelne
        /// Zone ist davon ausgenommen: Ihre Fläche ist nach der Übernahme mit Hochrechnung bewusst die
        /// des wirklichen Gebäudes, nicht die des Katalogbaus.</item>
        /// <item>Eine Zone ohne Bauteil an Außenluft, Erdreich oder unbeheiztem Raum
        /// (Außenbauteilgruppe) wird benannt — zulässig, aber auffällig.</item>
        /// </list>
        /// </summary>
        /// <param name="zonen">Die Zonen in Listenfolge.</param>
        /// <param name="nutzflaecheGebaeude">Die Nutzfläche des Gebäudes [m²]; <c>null</c> = keine (dann kein Flächenhinweis).</param>
        public static IReadOnlyList<string> Hinweise(IEnumerable<Zonenangabe> zonen, double? nutzflaecheGebaeude)
        {
            List<Zonenangabe> liste = (zonen ?? Enumerable.Empty<Zonenangabe>()).Where(z => z != null).ToList();
            var hinweise = new List<string>();
            CultureInfo k = CultureInfo.CurrentCulture;

            if (liste.Count >= 2 && nutzflaecheGebaeude is double ag && ag > 0.0 && !double.IsInfinity(ag))
            {
                double summe = 0.0;
                foreach (Zonenangabe z in liste) summe += z.Nutzflaeche ?? ag;
                double abweichung = (summe - ag) / ag;
                if (Math.Abs(abweichung) >= GebaeudeZonenregeln.FLAECHENABWEICHUNG_HINWEIS - 1e-12)
                    hinweise.Add(string.Format(k, MyResource.Resource.ZONE_HINWEIS_FLAECHE,
                                               summe.ToString("0.##", k), ag.ToString("0.##", k),
                                               (abweichung * 100.0).ToString("+0.#;−0.#;0", k)));
            }

            foreach (Zonenangabe z in liste)
            {
                bool aussen = (z.Bauteile ?? Array.Empty<(string, string)>()).Any(b =>
                {
                    Bauteilrand? rand = GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung);
                    return rand == Bauteilrand.Aussenluft || rand == Bauteilrand.Erdreich || rand == Bauteilrand.Unbeheizt;
                });
                if (!aussen)
                    hinweise.Add(string.Format(k, MyResource.Resource.ZONE_HINWEIS_OHNE_AUSSEN, (z.Bezeichner ?? "").Trim()));
            }
            return hinweise;
        }

        /// <summary>Dasselbe für gespeicherte Zonen.</summary>
        public static IReadOnlyList<string> Hinweise(IList<ZoneModel> zonen, double? nutzflaecheGebaeude)
            => Hinweise((zonen ?? new List<ZoneModel>()).Where(z => z != null)
                            .Select(z => new Zonenangabe(z.Bezeichner, z.Nutzflaeche,
                                (z.Bauteile ?? new List<BauteilModel>()).Where(b => b != null)
                                    .Select(b => (b.Bauteilart, b.Randbedingung)).ToList())),
                        nutzflaecheGebaeude);

        /// <summary>
        /// <b>Die Prüfregeln EINES Bauteils</b> (Mehrzonenkonzept 5.3, Ebene Bauteil) — die Regeln
        /// des Bauteildialogs, genau einmal, im Rückruf seiner Leiste: Name, Fläche größer null,
        /// U-Wert 0,1 … 6 W/(m²K), 0 &lt; g ≤ 1, Rahmenanteil 0,05 … 0,6, Verschattung (0; 1],
        /// Azimut 0 … 360°, Neigung 0 … 180°, ψ·L nicht negativ; ein Außenbauteil, das nicht
        /// waagerecht liegt, braucht einen Azimut (<see cref="BrauchtAzimut"/> — benannt
        /// abgelehnt, nicht auf Nord vorbelegt); die Randbedingung „Nachbarzone" und die Angabe der
        /// Nachbarzone stehen nur gemeinsam, die Zuordnung IW/AW nur an einer Trennfläche (Stufe G6b);
        /// ein Fenster oder eine Vorhangfassade grenzt an Außenluft, einen unbeheizten Raum oder eine
        /// Nachbarzone (Festlegung 4 des Auftrags G6b: <c>ZONE</c> gilt für dieselben Arten wie
        /// <c>UNBEHEIZT</c>) und trägt einen U-Wert; jedes andere Bauteil außerhalb der Zone braucht
        /// einen U-Wert oder einen Aufbau. Ohne Datenbank.
        /// </summary>
        /// <returns><c>null</c> = gültig, sonst die Meldung mit dem Namen des Bauteils.</returns>
        public static string BauteilPruefen(Bauteilangabe b)
        {
            if (b == null) return null;
            CultureInfo k = CultureInfo.CurrentCulture;
            string name = (b.Bezeichner ?? "").Trim();
            if (name.Length == 0) return MyResource.Resource.BAUTEIL_MSG_NAME_FEHLT;
            string w = BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_NAME, name, BaustoffSchema.LAENGE_BEZEICHNER)
                       ?? BauteilaufbauCtrl.BauteilartPruefen(b.Bauteilart, false);
            if (w != null) return string.Format(k, MyResource.Resource.BAUTEIL_MSG_WERT, name, w);

            if (!(b.Flaeche > 0.0) || double.IsInfinity(b.Flaeche.Value))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_FLAECHE, name);
            if (b.UWert.HasValue && !(b.UWert.Value >= GebaeudeFestwerte.U_MIN && b.UWert.Value <= GebaeudeFestwerte.U_MAX))
                return Bereich(name, "U", b.UWert.Value, GebaeudeFestwerte.U_MIN, GebaeudeFestwerte.U_MAX, "W/(m²K)");
            if (b.GWert.HasValue && !(b.GWert.Value > 0.0 && b.GWert.Value <= 1.0))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_BEREICH, name, "g", Text(b.GWert.Value), "(0; 1]");
            if (b.Rahmenanteil.HasValue && !(b.Rahmenanteil.Value >= RAHMENANTEIL_MIN && b.Rahmenanteil.Value <= RAHMENANTEIL_MAX))
                return Bereich(name, "1 − F_F", b.Rahmenanteil.Value, RAHMENANTEIL_MIN, RAHMENANTEIL_MAX, "");
            if (b.Verschattung.HasValue && !(b.Verschattung.Value > 0.0 && b.Verschattung.Value <= 1.0))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_BEREICH, name, "F_S", Text(b.Verschattung.Value), "(0; 1]");
            if (b.Azimut.HasValue && !(b.Azimut.Value >= 0.0 && b.Azimut.Value <= 360.0))
                return Bereich(name, "Azimut", b.Azimut.Value, 0.0, 360.0, "°");
            if (b.Neigung.HasValue && !(b.Neigung.Value >= 0.0 && b.Neigung.Value <= 180.0))
                return Bereich(name, "Neigung", b.Neigung.Value, 0.0, 180.0, "°");
            if (b.PsiL.HasValue && (!(b.PsiL.Value >= 0.0) || double.IsInfinity(b.PsiL.Value)))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_BEREICH, name, "ψ·L", Text(b.PsiL.Value), "[0; ∞) W/K");

            if (b.Randbedingung != null && !DbWerte.RANDBEDINGUNGEN.Contains(b.Randbedingung))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_RANDBEDINGUNG, name, b.Randbedingung);
            // Stufe G6b: die Randbedingung „Nachbarzone" und ihr Nachbar stehen nur gemeinsam; die
            // Zuordnung IW/AW gilt nur einer Trennflaeche (A1 = M3 (b)). Ob der Nachbar zum Gebaeude
            // gehoert, prueft die Liste (Zonenkopplungsregeln).
            bool zone = b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE;
            if (zone && !b.ID_Nachbarzone.HasValue)
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_RAND_ZONE, name);
            if (!zone && b.ID_Nachbarzone.HasValue)
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_NACHBAR_OHNE_RAND, name);
            if (b.TrennflaecheZuordnung != null && (!zone || !DbWerte.TRENNFLAECHE_ZUORDNUNGEN.Contains(b.TrennflaecheZuordnung)))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_TRENNFLAECHE_ZUORDNUNG, name, b.TrennflaecheZuordnung);

            var zeile = new BauteilModel { Bauteilart = b.Bauteilart, Randbedingung = b.Randbedingung, Neigung = b.Neigung };
            if (!b.Azimut.HasValue && BrauchtAzimut(zeile))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_AZIMUT_FEHLT, name);

            Bauteilart? art = GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart);
            Bauteilrand? rand = GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung);
            bool transparent = art == Bauteilart.Fenster || art == Bauteilart.Vorhangfassade;
            if (transparent && rand != Bauteilrand.Aussenluft && rand != Bauteilrand.Unbeheizt && rand != Bauteilrand.Zone)
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_FENSTER_RAND, name);
            if (transparent ? !b.UWert.HasValue : (!b.UWert.HasValue && !b.MitAufbau && rand != Bauteilrand.Innen))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_UWERT_FEHLT, name);
            return null;
        }

        /// <summary>Untere Grenze des Rahmenanteils im Bauteildialog [–] (Mehrzonenkonzept 5.3).</summary>
        public const double RAHMENANTEIL_MIN = 0.05;

        /// <summary>Obere Grenze des Rahmenanteils im Bauteildialog [–] (Mehrzonenkonzept 5.3).</summary>
        public const double RAHMENANTEIL_MAX = 0.6;

        private static string Bereich(string name, string groesse, double wert, double min, double max, string einheit)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_BEREICH, name, groesse, Text(wert),
                             Text(min) + " … " + Text(max) + (einheit.Length > 0 ? " " + einheit : ""));

        private static string Text(double w) => w.ToString("0.###", CultureInfo.CurrentCulture);

        // =================================================================
        //  „Gebäude als eine Zone übernehmen" — der Vorschlag mit Hochrechnung
        // =================================================================

        /// <summary>
        /// Der Vorschlag für „Gebäude als eine Zone übernehmen" — was die Rückfrage nennt und was
        /// danach im Arbeitsstand des Dialogs steht. Geschrieben ist nichts.
        /// </summary>
        /// <param name="Ok">Ließ sich der Vorschlag bilden?</param>
        /// <param name="Meldung">Der Grund, wenn nicht (Laufprotokoll der Fassade oder fehlende Projektkopie).</param>
        /// <param name="Faktor">Der Hochrechnungsfaktor der Fassade (E8).</param>
        /// <param name="Zone">Die Zone als neue Zeilen mit negativen vorläufigen Ids, hochgerechnet.</param>
        /// <param name="NutzflaecheGebaeude">Die Nutzfläche des Gebäudes [m²] — der Katalogbau.</param>
        /// <param name="Einheit">Die Einheit der Angabe im Projekt (<c>Wohnfläche [m²]</c> oder ein Verbrauch).</param>
        /// <param name="Angabe">Die Angabe im Projekt in dieser Einheit.</param>
        /// <param name="HeizgrenzeKw">Die Heizleistungsgrenze des Gebäudes [kW]; <c>null</c> = keine.</param>
        /// <param name="KuehlgrenzeKw">Die Kühlleistungsgrenze [kW] eines gekühlten Gebäudes; <c>null</c> = keine
        /// oder das Gebäude wird nicht gekühlt.</param>
        public sealed record Uebernahmevorschlag(bool Ok, string Meldung, double Faktor, ZoneModel Zone,
                                                 double NutzflaecheGebaeude, string Einheit, double Angabe,
                                                 double? HeizgrenzeKw = null, double? KuehlgrenzeKw = null)
        {
            /// <summary>Ist die Angabe ein Verbrauch (keine Fläche)?</summary>
            public bool Verbrauchsangabe => !string.Equals(Einheit, GebaeudeVorbereitung.EINHEIT_FLAECHE, StringComparison.Ordinal);

            /// <summary>
            /// Trägt das Gebäude eine Leistungsgrenze (Heizung, oder Kühlung bei gekühltem Gebäude)? Sie
            /// wird <b>nicht hochgerechnet</b> (E40, Konzept N1.45 Punkt 4): Im Klassenweg galt sie dem
            /// Katalogbau und wurde mit ihm nachmultipliziert, mit der Zone gilt sie unverändert der
            /// hochgerechneten Hülle — die Rückfrage nennt sie mit ihrem Wert.
            /// </summary>
            public bool Leistungsgrenzen => HeizgrenzeKw.HasValue || KuehlgrenzeKw.HasValue;
        }

        /// <summary>
        /// <b>Der Vorschlag der Übernahme mit Hochrechnung</b> (Anwenderentscheid vom 25.09.2026
        /// „Hochrechnen“; Softwarearchitektur 3.2 Regel 3). Das Projektgebäude der Zuordnung
        /// <paramref name="idZ"/> — so, wie der Lauf es liest, mit den Gebäudewerten des
        /// Arbeitsstands <paramref name="arbeitsstand"/> darüber (<c>null</c> = die gespeicherten) —
        /// bekommt seinen Hochrechnungsfaktor aus der Fassade
        /// (<see cref="GebaeudeBedarfCtrl.Hochrechnungsfaktor"/>, gerufen, nicht nachgerechnet), und
        /// <see cref="GebaeudeZonenuebernahme.AlsEineZone(ProjektGebaeudeModel, double)"/> bildet damit
        /// die Zone: Flächen und ψ·L mal Faktor, Nutzfläche Faktor × Nutzfläche des Gebäudes. So
        /// bleibt das Ergebnis beim Übernehmen gleich, und die Zone ist danach die echte Hülle.
        /// Schreibt nichts — die Zone entsteht im Arbeitsstand des Dialogs und wird erst in dessen
        /// OK-Weg über <see cref="SpeichernJeGebaeude"/> geschrieben.
        /// </summary>
        public static Uebernahmevorschlag Uebernahme(int idProjekt, int idZ, GebaeudeModel arbeitsstand)
        {
            ProjektGebaeudeModel g = GebaeudeBedarfCtrl.Projektgebaeude(idProjekt, idZ);
            if (g == null)
                return new Uebernahmevorschlag(false, MyResource.Resource.ZONE_MSG_UEBERNAHME_OHNE_KOPIE,
                                               double.NaN, null, double.NaN, null, double.NaN);
            if (arbeitsstand != null) UebergabeHerleitungsquelle.Ueberlagern(arbeitsstand, g);
            string einheit = g.Einheit;
            double angabe = g.Z_AuswahlWohnflaeche;
            double? heizgrenze = g.Heizleistung_Max;
            double? kuehlgrenze = g.Kuehlung_Aktiv ? g.Kuehlleistung_Max : null;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            double faktor = GebaeudeBedarfCtrl.Hochrechnungsfaktor(idProjekt, projekt.m_ID_Klimaregion, g, out string befund);
            if (double.IsNaN(faktor))
                return new Uebernahmevorschlag(false, befund ?? "", double.NaN, null, g.Nutzflaeche, einheit, angabe);

            ZoneModel zone = GebaeudeZonenabbildung.AlsZoneModel(GebaeudeZonenuebernahme.AlsEineZone(g, faktor));
            zone.ID_Gebaeude = g.ID_Gebaeude;
            return new Uebernahmevorschlag(true, "", faktor, zone, g.Nutzflaeche, einheit, angabe, heizgrenze, kuehlgrenze);
        }

        // =================================================================
        //  Schreiben — das Aggregat je Gebäude (A6)
        // =================================================================

        /// <summary>
        /// <b>Speichert die Zonen eines Gebäudes als Aggregat</b> — Abgleich über die Ids in EINER
        /// Transaktion (Klassenkopf). Nach Erfolg tragen alle Modelle ihre Id, ihr Eltern-Id und
        /// ihren Rang; bei einem Fehler ist nichts geschrieben.
        ///
        /// <para><b>Abgelehnt, bevor etwas geschrieben ist:</b> ein Prüfbefund
        /// (<see cref="Pruefen"/>), ein Gebäude, das es nicht gibt, eine positive Id, die nicht zu
        /// diesem Gebäude gehört, ein Aufbau, der nicht zum Projekt des Gebäudes gehört. Eine leere
        /// Liste entfernt alle Zonen — das Gebäude rechnet danach den Klassenweg.</para>
        ///
        /// <para>Die Luftströme bleiben dabei stehen (<c>null</c> der Überladung mit Luftstromliste);
        /// die Luftströme einer entfernten Zone fallen mit ihr (Kaskade).</para>
        /// </summary>
        public Ergebnis SpeichernJeGebaeude(int idGebaeude, IList<ZoneModel> zonen)
            => SpeichernJeGebaeude(idGebaeude, zonen, null);

        /// <summary>
        /// <b>Dasselbe samt der Kopplung</b> (Stufe G6b, Schemaschritt S-G): Trennflächen mit ihrer
        /// Nachbarzone und die Luftströme zwischen den Zonen, alles in EINER Transaktion.
        /// <list type="bullet">
        /// <item><b>Vorläufige Ids werden umgeschlüsselt:</b> Nach der Vergabe der Zonen-Ids zeigen
        /// <c>ID_Nachbarzone</c> und beide Zonen eines Luftstroms auf die endgültige Id; eine Id ≤ 0
        /// nennt eine neue Zone derselben Liste (Regel: <see cref="Zonenkopplungsregeln"/>, dort auch
        /// die Eindeutigkeit).</item>
        /// <item><b>Schritt 3b, die Luftströme</b> nach den Bauteilen: entfernen, ändern, anlegen. Das
        /// Paar wird nach der Id-Vergabe auf A &lt; B gedreht. Geändert wird unter derselben Id nur der
        /// Volumenstrom; wechselt das Paar, fällt die Zeile und entsteht neu — so trifft kein
        /// Zwischenstand den eindeutigen Index des Paares.</item>
        /// <item>„Zonen entfernen" bleibt der letzte Schritt.</item>
        /// </list>
        /// <paramref name="luftstroeme"/> <c>null</c> lässt die gespeicherten Luftströme stehen; eine
        /// leere Liste entfernt sie. <b>Ohne S-G</b> (iOS migriert nicht nach) wird eine Trennfläche oder
        /// ein Luftstrom benannt abgelehnt (<c>ZONE_MSG_OHNE_KOPPLUNG</c>), bevor etwas geschrieben ist.
        /// </summary>
        public Ergebnis SpeichernJeGebaeude(int idGebaeude, IList<ZoneModel> zonen, IList<ZonenluftstromModel> luftstroeme)
        {
            List<ZoneModel> liste = (zonen ?? new List<ZoneModel>()).Where(z => z != null).ToList();
            foreach (ZoneModel z in liste) z.Bauteile ??= new List<BauteilModel>();
            List<ZonenluftstromModel> stroeme = luftstroeme?.Where(l => l != null).ToList();
            // Der Tagessollwert des Gebaeudes nur, wenn eine Trennflaeche psi-L traegt (Regel: psi-L
            // gehoert der waermeren Zone; eine Zone ohne eigenen Sollwert hat den des Gebaeudes).
            double? sollTag = liste.SelectMany(z => z.Bauteile).Any(b => b != null && b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE
                                                                          && (b.Psi_L ?? 0.0) > 0.0)
                ? SollTagGebaeude(idGebaeude)
                : null;
            string fehler = Pruefen(liste, stroeme, sollTag);
            if (fehler != null) return Ergebnis.Fehler(fehler);

            // Schritt S-G: ohne ihn keine Trennflaeche und kein Luftstrom - benannt, nicht still.
            bool kopplung = GebaeudeZonenanschluss.KopplungVorhanden();
            if (!kopplung && (stroeme?.Count > 0 || liste.SelectMany(z => z.Bauteile).Any(b => b != null
                    && (b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE || b.ID_Nachbarzone.HasValue || b.Trennflaeche_Zuordnung != null))))
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG,
                                                     ZonenkopplungSchema.SCHRITT));

            // Die Spalten der Zone: die von ZonenSchema und - mit Schritt 137 - die drei der
            // Kuehluebergabe (E37); die des Bauteils samt S-G. Festgestellt VOR dem Vorgang, auf der
            // gewoehnlichen Verbindung.
            IReadOnlyList<string> kuehl = Kuehlspalten();
            List<string> spalten = ZonenSchema.Zonenspalten.Concat(kuehl).ToList();
            IEnumerable<DbParam> Werte(ZoneModel z) => kuehl.Count > 0 ? Zonenwerte(z).Concat(Kuehlwerte(z)) : Zonenwerte(z);
            IReadOnlyList<string> bauteilspalten = Bauteilspalten();

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    object projekt = v.Skalar("SELECT COALESCE(\"ID_Projekt\", 0) FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?",
                                              new DbParam("@g", idGebaeude));
                    if (projekt == null)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_GEBAEUDE_FEHLT, idGebaeude));
                    }
                    int idProjekt = Convert.ToInt32(projekt, CultureInfo.InvariantCulture);

                    // Der Bestand dieses Gebaeudes: Zonen und Bauteile samt ihrer Zone.
                    var zonenBestand = new HashSet<int>(v.Lese(
                        "SELECT \"ID\" FROM \"" + ZonenSchema.TAB_ZONE + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude))
                        .Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)));
                    var bauteilBestand = new HashSet<int>(v.Lese(
                        "SELECT b.\"ID\" FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" b INNER JOIN \"" + ZonenSchema.TAB_ZONE + "\" z " +
                        "ON z.\"ID\" = b.\"ID_Zone\" WHERE z.\"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude))
                        .Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)));

                    // Fremde Ids und fremde Aufbauten weist der Abgleich ab, bevor er schreibt.
                    var aufbauten = new HashSet<int>(v.Lese(
                        "SELECT \"ID\" FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID_Projekt\" = ?", new DbParam("@p", idProjekt))
                        .Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)));
                    foreach (ZoneModel z in liste)
                    {
                        if (z.ID > 0 && !zonenBestand.Contains(z.ID))
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_FREMD, z.ID));
                        }
                        foreach (BauteilModel b in z.Bauteile.Where(x => x != null))
                        {
                            if (b.ID > 0 && !bauteilBestand.Contains(b.ID))
                            {
                                v.Rollback();
                                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_FREMD,
                                                                     z.Bezeichner.Trim(), b.ID));
                            }
                            if (b.ID_Aufbau.HasValue && !aufbauten.Contains(b.ID_Aufbau.Value))
                            {
                                v.Rollback();
                                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_FREMD,
                                                                     b.Bezeichner.Trim(), b.ID_Aufbau.Value));
                            }
                        }
                    }

                    // Der Bestand der Luftstroeme (S-G): Id -> Paar; eine fremde Id weist der Abgleich ab.
                    var stromBestand = new Dictionary<int, (int A, int B)>();
                    if (kopplung && stroeme != null)
                    {
                        foreach (DataRow r in v.Lese(
                            "SELECT l.\"ID\", l.\"ID_ZoneA\", l.\"ID_ZoneB\" FROM \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\" l " +
                            "INNER JOIN \"" + ZonenSchema.TAB_ZONE + "\" z ON z.\"ID\" = l.\"ID_ZoneA\" WHERE z.\"ID_Gebaeude\" = ?",
                            new DbParam("@g", idGebaeude)).Rows)
                            stromBestand[Convert.ToInt32(r[0], CultureInfo.InvariantCulture)] =
                                (Convert.ToInt32(r[1], CultureInfo.InvariantCulture), Convert.ToInt32(r[2], CultureInfo.InvariantCulture));
                        foreach (ZonenluftstromModel l in stroeme)
                            if (l.ID > 0 && !stromBestand.ContainsKey(l.ID))
                            {
                                v.Rollback();
                                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_LUFTSTROM_FREMD, l.ID));
                            }
                    }

                    var zonenBleiben = new HashSet<int>(liste.Where(z => z.ID > 0).Select(z => z.ID));
                    var bauteileBleiben = new HashSet<int>(liste.SelectMany(z => z.Bauteile.Where(b => b != null && b.ID > 0))
                                                                .Select(b => b.ID));

                    // 1) Entfernen: die Bauteile, die in der Liste fehlen.
                    foreach (int id in bauteilBestand.Where(id => !bauteileBleiben.Contains(id)))
                        v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" WHERE \"ID\" = ?", new DbParam("@id", id));

                    // 2) Aendern und Anlegen der Zonen, Rang aus der Listenreihenfolge. Die vorlaeufige
                    //    Id jeder neuen Zone merkt sich ihre endgueltige (S-G: Nachbar und Luftstrom).
                    var endgueltig = new Dictionary<int, int>();
                    int rangZone = 0;
                    foreach (ZoneModel z in liste)
                    {
                        z.ID_Gebaeude = idGebaeude;
                        z.Rang = ++rangZone;
                        z.Bezeichner = z.Bezeichner.Trim();
                        if (z.ID > 0)
                            v.Ausfuehren("UPDATE \"" + ZonenSchema.TAB_ZONE + "\" SET " +
                                         string.Join(", ", spalten.Select(s => "\"" + s + "\" = ?")) +
                                         " WHERE \"ID\" = ?", Werte(z).Append(new DbParam("@id", z.ID)).ToArray());
                        else
                        {
                            int vorlaeufig = z.ID;
                            z.ID = v.EinfuegenUndId("INSERT INTO \"" + ZonenSchema.TAB_ZONE + "\" (" +
                                                    string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                                                    ") VALUES (" + BaustoffCtrl.Fragezeichen(spalten.Count) + ")",
                                                    Werte(z).ToArray());
                            endgueltig.TryAdd(vorlaeufig, z.ID);
                        }
                    }
                    int Endgueltig(int id) => id > 0 ? id : endgueltig[id];

                    // 3) Aendern und Anlegen der Bauteile - ein verschobenes Bauteil bekommt hier
                    //    seine neue Zone, bevor die alte fallen kann.
                    foreach (ZoneModel z in liste)
                    {
                        int rangBauteil = 0;
                        foreach (BauteilModel b in z.Bauteile.Where(x => x != null))
                        {
                            b.ID_Zone = z.ID;
                            b.Rang = ++rangBauteil;
                            b.Bezeichner = b.Bezeichner.Trim();
                            if (kopplung && b.ID_Nachbarzone.HasValue) b.ID_Nachbarzone = Endgueltig(b.ID_Nachbarzone.Value);
                            if (b.ID > 0)
                                v.Ausfuehren("UPDATE \"" + ZonenSchema.TAB_BAUTEIL + "\" SET " +
                                             string.Join(", ", bauteilspalten.Select(s => "\"" + s + "\" = ?")) +
                                             " WHERE \"ID\" = ?", Bauteilwerte(b, kopplung).Append(new DbParam("@id", b.ID)).ToArray());
                            else
                                b.ID = v.EinfuegenUndId("INSERT INTO \"" + ZonenSchema.TAB_BAUTEIL + "\" (" +
                                                        string.Join(", ", bauteilspalten.Select(s => "\"" + s + "\"")) +
                                                        ") VALUES (" + BaustoffCtrl.Fragezeichen(bauteilspalten.Count) + ")",
                                                        Bauteilwerte(b, kopplung).ToArray());
                        }
                        z.Bauteile = z.Bauteile.Where(x => x != null).ToList();
                    }

                    // 3b) Die Luftstroeme (S-G): umschluesseln und auf A < B drehen, dann entfernen,
                    //     aendern (nur der Volumenstrom unter derselben Id), anlegen. Ein Paar, das
                    //     wechselt, faellt und entsteht neu - kein Zwischenstand trifft den
                    //     eindeutigen Index des Paares.
                    if (kopplung && stroeme != null)
                    {
                        foreach (ZonenluftstromModel l in stroeme)
                        {
                            int a = Endgueltig(l.ID_ZoneA), b = Endgueltig(l.ID_ZoneB);
                            (l.ID_ZoneA, l.ID_ZoneB) = a < b ? (a, b) : (b, a);
                        }
                        var aendern = stroeme.Where(l => l.ID > 0 && stromBestand[l.ID] == (l.ID_ZoneA, l.ID_ZoneB)).ToList();
                        var bleiben = new HashSet<int>(aendern.Select(l => l.ID));
                        foreach (int id in stromBestand.Keys.Where(id => !bleiben.Contains(id)))
                            v.Ausfuehren("DELETE FROM \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
                        foreach (ZonenluftstromModel l in aendern)
                            v.Ausfuehren("UPDATE \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\" SET \"Volumenstrom\" = ? WHERE \"ID\" = ?",
                                         new DbParam("@v", DbParamTyp.Double) { Wert = l.Volumenstrom }, new DbParam("@id", l.ID));
                        foreach (ZonenluftstromModel l in stroeme.Where(l => !bleiben.Contains(l.ID)))
                            l.ID = v.EinfuegenUndId("INSERT INTO \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\" " +
                                                    "(\"ID_ZoneA\", \"ID_ZoneB\", \"Volumenstrom\") VALUES (?, ?, ?)",
                                                    new[] { new DbParam("@a", l.ID_ZoneA), new DbParam("@b", l.ID_ZoneB),
                                                            new DbParam("@v", DbParamTyp.Double) { Wert = l.Volumenstrom } });
                    }

                    // 4) Entfernen: die Zonen, die in der Liste fehlen - zuletzt (Klassenkopf).
                    foreach (int id in zonenBestand.Where(id => !zonenBleiben.Contains(id)))
                        v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_ZONE + "\" WHERE \"ID\" = ?", new DbParam("@id", id));

                    v.Commit();
                    return Ergebnis.Gut;
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_NICHT_GESPEICHERT, ex.Message));
            }
        }

        // =================================================================
        //  Schreiben — der Bauteilvorschlag eines Imports (G4b)
        // =================================================================

        /// <summary>
        /// Was das Schreiben eines Bauteilvorschlags ergeben hat.
        /// </summary>
        /// <param name="Ok">Ist alles geschrieben?</param>
        /// <param name="Befund">Der Grund, wenn nicht — Schlüssel und Werte (<c>IMP_BAUTEIL_PROT_*</c> oder die
        /// erste Fehlermeldung des Vorschlags); <c>null</c> bei Erfolg.</param>
        /// <param name="Meldung">Der Text des Befunds in der Anzeigekultur; leer bei Erfolg.</param>
        /// <param name="IdZone">Die Kennung der neuen Zone (<c>Tab_Zone.ID</c>); 0 ohne.</param>
        /// <param name="Zuordnungen">Die Paarungen Quellentität ↔ neue Zeile mit Ziel-Id — beheizte Räume auf
        /// die Zone, Flächen und Öffnungen auf ihre Bauteile, Konstruktionen auf ihre Aufbauten, abgeglichene
        /// Baustoffe der Datei auf die Projektkopie ihres Katalogbaustoffs; der Eingang von
        /// <see cref="GebaeudeImportCtrl.SchreibeHerkunft"/>. Leer bei einem Fehler.</param>
        /// <param name="Zone">Die geschriebene Zone mit endgültigen Ids samt Bauteilen; <c>null</c> bei einem Fehler.</param>
        /// <param name="Aufbauten">Die geschriebenen Aufbauten mit endgültigen Ids und freien Namen; leer bei einem Fehler.</param>
        /// <param name="Zonen">Alle geschriebenen Zonen in Rangfolge (Stufe G6c); im Einzonenweg die eine; leer bei einem Fehler.</param>
        internal sealed record Vorschlagsergebnis(bool Ok, PruefMeldung Befund, string Meldung, int IdZone,
                                                  IReadOnlyList<GebaeudeQuellzuordnung> Zuordnungen, ZoneModel Zone,
                                                  IReadOnlyList<BauteilaufbauModel> Aufbauten,
                                                  IReadOnlyList<ZoneModel> Zonen = null)
        {
            internal static Vorschlagsergebnis Fehler(PruefMeldung befund)
                => new Vorschlagsergebnis(false, befund, GebaeudeZuordnungsModell.MeldungText(befund), 0,
                                          Array.Empty<GebaeudeQuellzuordnung>(), null, Array.Empty<BauteilaufbauModel>());

            internal static Vorschlagsergebnis Fehler(string schluessel, params string[] werte)
                => Fehler(new PruefMeldung(PruefStufe.Fehler, schluessel, werte));
        }

        /// <summary>
        /// <b>Schreibt einen Bauteilvorschlag für ein vorhandenes Projektgebäude</b> (Stufe G4b;
        /// <see cref="GebaeudeBauteilvorschlag"/>) — in EINEM Vorgang: die Aufbauten samt Schichten als
        /// Projektkopien (Namen im Projekt frei gemacht), die Zone, die Bauteile mit den abgebildeten
        /// <c>ID_Aufbau</c>. Trägt eine Schicht Werte aus dem Namensabgleich
        /// (<see cref="GebaeudeAufbauzeile.Stammbaustoffe"/>), kommt ihr Katalogbaustoff über
        /// <see cref="BaustoffCtrl.CopyFromStamm(DbVorgang, int, int)"/> in das Projekt, und
        /// <c>ID_Baustoff</c> der Schicht zeigt auf die Projektkopie — die Stoffwerte der Schicht bleiben
        /// die Kopie des Vorschlags. Nur über <see cref="DataRepository"/> mit <c>?</c>-Parametern; bei einem
        /// Fehler ist nichts geschrieben. Der Vorschlag selbst bleibt unverändert (es wird eine Kopie
        /// geschrieben).
        ///
        /// <para><b>Benannt abgelehnt, bevor etwas geschrieben ist:</b> ein abgelehnter Vorschlag (seine
        /// erste Fehlermeldung), ein Prüfbefund der Zeilen (<see cref="Pruefen"/>,
        /// <see cref="BauteilaufbauCtrl.Pruefen"/>), ein Gebäude, das es nicht gibt oder das keine
        /// Projektkopie ist, und <b>ein Gebäude, das schon eine Zone trägt</b> — ersetzt wird nichts.</para>
        ///
        /// <para><b>Stufe G6c — mehrere Zonen:</b> Ein Vorschlag mit Zonierung schreibt alle Zonen
        /// (<see cref="GebaeudeBauteilvorschlag.Zonen"/>) im selben Vorgang, in Rangfolge; die
        /// vorläufigen Ids von <c>ID_Nachbarzone</c> und der Raumpaarungen werden auf die endgültigen
        /// abgebildet. Geprüft wird vorher wie in der Pflege aus G6a samt den Regeln zwischen den Zonen
        /// (<see cref="Pruefen(IList{ZoneModel})"/>); ohne Schritt S-G wird eine Trennfläche benannt
        /// abgelehnt.</para>
        ///
        /// <para>Die Paarungen für <c>Tab_Importzuordnung</c> schreibt diese Methode NICHT — sie gibt
        /// sie mit den neuen Kennungen zurück (<see cref="Vorschlagsergebnis.Zuordnungen"/>); die
        /// Herkunft schreibt <see cref="GebaeudeImportCtrl.SchreibeHerkunft"/>. Mit
        /// <paramref name="vorgang"/> läuft das Schreiben im Vorgang des Aufrufers (als
        /// Sicherungspunkt, Muster <see cref="GebaeudeImportCtrl.SchreibeHerkunft"/>) — so schreibt
        /// der Aufrufer Vorschlag und Herkunft in EINEM Vorgang.</para>
        /// </summary>
        /// <param name="idGebaeude">Die Projektkopie (<c>Tab_Gebaeude.ID</c>).</param>
        /// <param name="vorschlag">Der Vorschlag.</param>
        /// <param name="vorgang">Der Vorgang des Aufrufers; <c>null</c> = ein eigener.</param>
        internal Vorschlagsergebnis VorschlagSchreiben(int idGebaeude, GebaeudeBauteilvorschlag vorschlag, DbVorgang vorgang = null)
        {
            if (vorschlag == null) throw new ArgumentNullException(nameof(vorschlag));
            if (vorschlag.Abgelehnt)
                return Vorschlagsergebnis.Fehler(vorschlag.Meldungen.FirstOrDefault(m => m.Stufe == PruefStufe.Fehler)
                    ?? new PruefMeldung(PruefStufe.Fehler, GebaeudeBauteilvorschlag.KEIN_GEBAEUDE, "", ""));

            // Arbeitskopien: der Vorschlag bleibt, wie er ist. Stufe G6c: alle Zonen des Vorschlags.
            List<ZoneModel> zonen = vorschlag.Zonen.Select(z => z.Kopie()).ToList();
            List<BauteilaufbauModel> aufbauten = vorschlag.Aufbauten.Select(a => a.Aufbau.Kopie()).ToList();
            string fehler = Pruefen(zonen);
            foreach (BauteilaufbauModel a in aufbauten) fehler ??= BauteilaufbauCtrl.Pruefen(a);
            if (fehler != null) return Vorschlagsergebnis.Fehler(GebaeudeBauteilvorschlag.NICHT_GESCHRIEBEN, fehler);

            IReadOnlyList<string> kuehl = Kuehlspalten();
            List<string> spalten = ZonenSchema.Zonenspalten.Concat(kuehl).ToList();
            IReadOnlyList<string> bauteilspalten = Bauteilspalten();
            bool kopplung = bauteilspalten.Count > ZonenSchema.Bauteilspalten.Count;
            // Ohne Schritt S-G keine Trennfläche — benannt, bevor etwas geschrieben ist.
            if (!kopplung && zonen.SelectMany(z => z.Bauteile).Any(b => b.ID_Nachbarzone.HasValue || b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE))
                return Vorschlagsergebnis.Fehler(GebaeudeBauteilvorschlag.NICHT_GESCHRIEBEN,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG, ZonenkopplungSchema.SCHRITT));
            string id = idGebaeude.ToString(CultureInfo.InvariantCulture);
            var stoffJeStamm = new Dictionary<int, int>();
            var zoneJeVorlaeufig = new Dictionary<int, int>();
            using Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    DataTable geb = v.Lese("SELECT \"ID_Projekt\" FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?", new DbParam("@g", idGebaeude));
                    if (geb.Rows.Count == 0)
                    {
                        v.Rollback();
                        return Vorschlagsergebnis.Fehler(GebaeudeBauteilvorschlag.GEBAEUDE_FEHLT, id);
                    }
                    int idProjekt = geb.Rows[0][0] is DBNull ? 0 : Convert.ToInt32(geb.Rows[0][0], CultureInfo.InvariantCulture);
                    if (idProjekt <= 0)
                    {
                        v.Rollback();
                        return Vorschlagsergebnis.Fehler(GebaeudeBauteilvorschlag.KEINE_PROJEKTKOPIE, id);
                    }
                    long vorhanden = Convert.ToInt64(v.Skalar(
                        "SELECT COUNT(*) FROM \"" + ZonenSchema.TAB_ZONE + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude)),
                        CultureInfo.InvariantCulture);
                    if (vorhanden > 0)
                    {
                        v.Rollback();
                        return Vorschlagsergebnis.Fehler(GebaeudeBauteilvorschlag.ZONE_VORHANDEN, id,
                                                         vorhanden.ToString(CultureInfo.InvariantCulture));
                    }

                    // 1) Die Aufbauten samt Schichten — Namen, die das Projekt schon führt, werden ergänzt.
                    var vergeben = new HashSet<string>(v.Lese(
                        "SELECT \"Bezeichner\" FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID_Projekt\" = ?",
                        new DbParam("@p", idProjekt)).Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)),
                        StringComparer.Ordinal);
                    //    Die Schichten aus dem Namensabgleich zeigen auf die Projektkopie ihres
                    //    Katalogbaustoffs — über den vorhandenen Kopierweg (eine vorhandene Kopie gleichen
                    //    Namens und Herstellers wird genommen), im selben Vorgang.
                    var aufbauJeVorlaeufig = new Dictionary<int, int>();
                    for (int j = 0; j < aufbauten.Count; j++)
                    {
                        BauteilaufbauModel a = aufbauten[j];
                        IReadOnlyList<int?> stamm = vorschlag.Aufbauten[j].Stammbaustoffe;
                        for (int i = 0; i < a.Schichten.Count && i < stamm.Count; i++)
                            if (stamm[i] is int idStamm)
                                a.Schichten[i].ID_Baustoff = Projektbaustoff(v, idStamm, idProjekt, stoffJeStamm);
                        int vorlaeufig = a.ID;
                        a.Bezeichner = BauteilaufbauCtrl.FreierName(vergeben, a.Bezeichner);
                        aufbauJeVorlaeufig[vorlaeufig] = BauteilaufbauCtrl.ProjektaufbauEinfuegen(v, idProjekt, a);
                    }

                    // 2) Die Zonen in Rangfolge; jede vorläufige Id merkt sich ihre endgültige.
                    int rangZone = 0;
                    foreach (ZoneModel zone in zonen)
                    {
                        zone.ID_Gebaeude = idGebaeude;
                        zone.Rang = ++rangZone;
                        zone.Bezeichner = zone.Bezeichner.Trim();
                        int vorlaeufig = zone.ID;
                        IEnumerable<DbParam> werte = kuehl.Count > 0 ? Zonenwerte(zone).Concat(Kuehlwerte(zone)) : Zonenwerte(zone);
                        zone.ID = v.EinfuegenUndId("INSERT INTO \"" + ZonenSchema.TAB_ZONE + "\" (" +
                                                   string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                                                   ") VALUES (" + BaustoffCtrl.Fragezeichen(spalten.Count) + ")", werte.ToArray());
                        zoneJeVorlaeufig[vorlaeufig] = zone.ID;
                    }

                    // 3) Die Bauteile, Aufbau und Nachbarzone über ihre vorläufige Id abgebildet.
                    foreach (ZoneModel zone in zonen)
                    {
                        int rang = 0;
                        foreach (BauteilModel b in zone.Bauteile)
                        {
                            b.ID_Zone = zone.ID;
                            b.Rang = ++rang;
                            b.Bezeichner = b.Bezeichner.Trim();
                            if (b.ID_Aufbau.HasValue)
                            {
                                if (!aufbauJeVorlaeufig.TryGetValue(b.ID_Aufbau.Value, out int echt))
                                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                        "Das Bauteil {0} zeigt auf den Aufbau {1}, den der Vorschlag nicht führt.", b.Bezeichner, b.ID_Aufbau.Value));
                                b.ID_Aufbau = echt;
                            }
                            if (b.ID_Nachbarzone.HasValue)
                                b.ID_Nachbarzone = zoneJeVorlaeufig[b.ID_Nachbarzone.Value];
                            b.ID = v.EinfuegenUndId("INSERT INTO \"" + ZonenSchema.TAB_BAUTEIL + "\" (" +
                                                    string.Join(", ", bauteilspalten.Select(s => "\"" + s + "\"")) +
                                                    ") VALUES (" + BaustoffCtrl.Fragezeichen(bauteilspalten.Count) + ")",
                                                    Bauteilwerte(b, kopplung).ToArray());
                        }
                    }

                    v.Commit();
                }
            }
            catch (Exception ex)
            {
                return Vorschlagsergebnis.Fehler(GebaeudeBauteilvorschlag.NICHT_GESCHRIEBEN, ex.Message);
            }

            // Die Paarungen mit den neuen Kennungen (Tab_Importzuordnung schreibt GebaeudeImportCtrl): jeder
            // Raum auf seine Zone (im Einzonenweg ohne vorläufige Id die eine), jede Zeile auf ihr Bauteil —
            // je Zone in der Reihenfolge ihrer Zeilen.
            var zuordnungen = new List<GebaeudeQuellzuordnung>();
            foreach (GebaeudeQuellzuordnung r in vorschlag.Raeume)
                zuordnungen.Add(new GebaeudeQuellzuordnung(r.Quelltyp, r.Quellkennung, ImportZiel.Zone,
                    r.ZielId.HasValue && zoneJeVorlaeufig.TryGetValue(r.ZielId.Value, out int zid) ? zid : zonen[0].ID));
            var stelleJeZone = new int[zonen.Count];
            foreach (GebaeudeBauteilzeile z in vorschlag.Zeilen)
            {
                BauteilModel geschrieben = zonen[z.Zone].Bauteile[stelleJeZone[z.Zone]++];
                if (z.Quelltyp != null)
                    zuordnungen.Add(new GebaeudeQuellzuordnung(z.Quelltyp, z.Kennung, ImportZiel.Bauteil, geschrieben.ID));
            }
            for (int j = 0; j < vorschlag.Aufbauten.Count; j++)
            {
                GebaeudeAufbauzeile a = vorschlag.Aufbauten[j];
                zuordnungen.Add(new GebaeudeQuellzuordnung(a.Quelltyp, a.Kennung, ImportZiel.Aufbau, aufbauten[j].ID));
            }
            // Die Baustoffe der Datei, die über den Namensabgleich einen Katalogbaustoff tragen, auf dessen
            // Projektkopie — je Quellentität eine Paarung.
            var gepaart = new HashSet<(string, string)>();
            foreach (GebaeudeAufbauzeile a in vorschlag.Aufbauten)
                foreach (GebaeudeBaustoffquelle q in a.Baustoffquellen)
                    if (stoffJeStamm.TryGetValue(q.IdStamm, out int stoff) && gepaart.Add((q.Quelltyp, q.Kennung)))
                        zuordnungen.Add(new GebaeudeQuellzuordnung(q.Quelltyp, q.Kennung, ImportZiel.Baustoff, stoff));
            return new Vorschlagsergebnis(true, null, "", zonen[0].ID, zuordnungen.AsReadOnly(), zonen[0], aufbauten.AsReadOnly(),
                                          zonen.AsReadOnly());
        }

        /// <summary>
        /// Die Projektkopie eines Katalogbaustoffs im Vorgang <paramref name="v"/> — über
        /// <see cref="BaustoffCtrl.CopyFromStamm(DbVorgang, int, int)"/> (eine vorhandene Kopie gleichen Namens
        /// und Herstellers wird genommen), je Katalogbaustoff einmal. Wirft, wenn der Katalogbaustoff fehlt —
        /// der Vorgang des Vorschlags rollt dann ganz zurück.
        /// </summary>
        private static int Projektbaustoff(DbVorgang v, int idStamm, int idProjekt, Dictionary<int, int> jeStamm)
        {
            if (jeStamm.TryGetValue(idStamm, out int bekannt)) return bekannt;
            int kopie = BaustoffCtrl.CopyFromStamm(v, idStamm, idProjekt);
            if (kopie <= 0)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "Der Katalogbaustoff {0} fehlt; er lässt sich nicht in das Projekt {1} kopieren.", idStamm, idProjekt));
            jeStamm[idStamm] = kopie;
            return kopie;
        }

        // =================================================================
        //  intern
        // =================================================================

        /// <summary>Der Tagessollwert des Gebäudes [°C]; <c>null</c> ohne Zeile oder Wert.</summary>
        private static double? SollTagGebaeude(int idGebaeude)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Raumsolltemperatur_Tag\" FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?", new DbParam("@g", idGebaeude));
            return t != null && t.Rows.Count > 0 ? BaustoffCtrl.ZahlAus(t.Rows[0], "Raumsolltemperatur_Tag") : null;
        }

        /// <summary>Die Werte einer Zone in der Reihenfolge von <see cref="ZonenSchema.Zonenspalten"/>; NULL bleibt NULL.</summary>
        private static IEnumerable<DbParam> Zonenwerte(ZoneModel z)
        {
            yield return new DbParam("@g", z.ID_Gebaeude);
            yield return new DbParam("@r", z.Rang);
            yield return new DbParam("@b", z.Bezeichner);
            yield return BaustoffCtrl.Zahl("@nf", z.Nutzflaeche);
            yield return BaustoffCtrl.Zahl("@rh", z.Raumhoehe);
            yield return BaustoffCtrl.Zahl("@vol", z.Volumen);
            yield return new DbParam("@beh", z.IstBeheizt ? 1 : 0);
            yield return BaustoffCtrl.Zahl("@t1", z.Raumsolltemperatur_Tag);
            yield return BaustoffCtrl.Zahl("@t2", z.Raumsolltemperatur_Nachtabsenkung);
            yield return BaustoffCtrl.Zahl("@t3", z.Raumsolltemperatur_Wochenende);
            yield return BaustoffCtrl.Zahl("@t4", z.Raumsolltemperatur_Ferien);
            yield return BaustoffCtrl.Zahl("@tmax", z.Maximaleraumtemperatur);
            yield return BaustoffCtrl.Zahl("@hs", z.Heizung_Strahlungsanteil);
            yield return BaustoffCtrl.Zahl("@hl", z.Heizleistung_Max);
            yield return BaustoffCtrl.Zahl("@li", z.Luftwechsel_Infiltration);
            yield return BaustoffCtrl.Zahl("@ln", z.Luftwechsel_Nutzer);
            yield return BaustoffCtrl.Zahl("@iw", z.Interne_Waermegewinne);
            yield return BaustoffCtrl.Zahl("@bw", z.Bewohner);
            yield return BaustoffCtrl.Zahl("@ks", z.Kuehl_Sollwert);
            yield return BaustoffCtrl.Zahl("@kl", z.Kuehlleistung_Max);
            yield return new DbParam("@ka", DbParamTyp.Integer)
                { Wert = z.Kuehlung_Aktiv.HasValue ? (object)(z.Kuehlung_Aktiv.Value ? 1 : 0) : DBNull.Value };
            yield return BaustoffCtrl.Zahl("@kn", z.Kuehl_Sollwert_Nacht);
            yield return BaustoffCtrl.Text("@ua", z.Uebergabe_Art);
            yield return BaustoffCtrl.Zahl("@ue", z.Uebergabe_Exponent);
            yield return BaustoffCtrl.Zahl("@ul", z.Uebergabe_Leistung_Nenn);
            yield return BaustoffCtrl.Text("@h", z.Herkunft);
            yield return BaustoffCtrl.Text("@qk", z.Quellkennung);
        }

        /// <summary>Die Werte der Kühlübergabe einer Zone in der Reihenfolge von <see cref="KuehluebergabeSchema.SpaltenZone"/>; NULL bleibt NULL.</summary>
        private static IEnumerable<DbParam> Kuehlwerte(ZoneModel z)
        {
            yield return BaustoffCtrl.Text("@kua", z.Kuehl_Uebergabe_Art);
            yield return BaustoffCtrl.Zahl("@kue", z.Kuehl_Uebergabe_Exponent);
            yield return BaustoffCtrl.Zahl("@kul", z.Kuehl_Uebergabe_Leistung_Nenn);
        }

        /// <summary>
        /// Die Werte eines Bauteils in der Reihenfolge von <see cref="ZonenSchema.Bauteilspalten"/>,
        /// mit <paramref name="kopplung"/> dazu die zwei Spalten von S-G
        /// (<see cref="ZonenkopplungSchema.SpaltenBauteil"/>); NULL bleibt NULL.
        /// </summary>
        private static IEnumerable<DbParam> Bauteilwerte(BauteilModel b, bool kopplung)
        {
            foreach (DbParam p in Bauteilwerte(b)) yield return p;
            if (!kopplung) yield break;
            yield return new DbParam("@nz", DbParamTyp.Integer) { Wert = b.ID_Nachbarzone.HasValue ? (object)b.ID_Nachbarzone.Value : DBNull.Value };
            yield return BaustoffCtrl.Text("@tz", b.Trennflaeche_Zuordnung);
        }

        /// <summary>Die Werte eines Bauteils in der Reihenfolge von <see cref="ZonenSchema.Bauteilspalten"/>; NULL bleibt NULL.</summary>
        private static IEnumerable<DbParam> Bauteilwerte(BauteilModel b)
        {
            yield return new DbParam("@z", b.ID_Zone);
            yield return new DbParam("@r", b.Rang);
            yield return new DbParam("@b", b.Bezeichner);
            yield return new DbParam("@art", b.Bauteilart);
            yield return new DbParam("@auf", DbParamTyp.Integer) { Wert = b.ID_Aufbau.HasValue ? (object)b.ID_Aufbau.Value : DBNull.Value };
            yield return new DbParam("@fl", DbParamTyp.Double) { Wert = b.Flaeche };
            yield return BaustoffCtrl.Zahl("@u", b.U_Wert);
            yield return BaustoffCtrl.Zahl("@g", b.g_Wert);
            yield return BaustoffCtrl.Zahl("@ra", b.Rahmenanteil);
            yield return BaustoffCtrl.Zahl("@vs", b.Verschattungsfaktor);
            yield return BaustoffCtrl.Zahl("@ne", b.Neigung);
            yield return BaustoffCtrl.Zahl("@az", b.Azimut);
            yield return BaustoffCtrl.Text("@rb", b.Randbedingung);
            yield return BaustoffCtrl.Zahl("@psi", b.Psi_L);
            yield return BaustoffCtrl.Text("@h", b.Herkunft);
            yield return BaustoffCtrl.Text("@qk", b.Quellkennung);
        }

        /// <summary>Zonen und Bauteile zusammenführen — im Speicher, über die gelesenen Ids.</summary>
        private static List<ZoneModel> Zusammenfuehren(DataTable zonen, DataTable bauteile)
        {
            var liste = new List<ZoneModel>();
            if (zonen == null) return liste;
            var jeId = new Dictionary<int, ZoneModel>();
            foreach (DataRow r in zonen.Rows)
            {
                var z = new ZoneModel
                {
                    ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                    ID_Gebaeude = Convert.ToInt32(r["ID_Gebaeude"], CultureInfo.InvariantCulture),
                    Rang = Convert.ToInt32(r["Rang"], CultureInfo.InvariantCulture),
                    Bezeichner = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                    Nutzflaeche = BaustoffCtrl.ZahlAus(r, "Nutzflaeche"),
                    Raumhoehe = BaustoffCtrl.ZahlAus(r, "Raumhoehe"),
                    Volumen = BaustoffCtrl.ZahlAus(r, "Volumen"),
                    IstBeheizt = (BaustoffCtrl.GanzAus(r, "IstBeheizt") ?? 1) != 0,
                    Raumsolltemperatur_Tag = BaustoffCtrl.ZahlAus(r, "Raumsolltemperatur_Tag"),
                    Raumsolltemperatur_Nachtabsenkung = BaustoffCtrl.ZahlAus(r, "Raumsolltemperatur_Nachtabsenkung"),
                    Raumsolltemperatur_Wochenende = BaustoffCtrl.ZahlAus(r, "Raumsolltemperatur_Wochenende"),
                    Raumsolltemperatur_Ferien = BaustoffCtrl.ZahlAus(r, "Raumsolltemperatur_Ferien"),
                    Maximaleraumtemperatur = BaustoffCtrl.ZahlAus(r, "Maximaleraumtemperatur"),
                    Heizung_Strahlungsanteil = BaustoffCtrl.ZahlAus(r, "Heizung_Strahlungsanteil"),
                    Heizleistung_Max = BaustoffCtrl.ZahlAus(r, "Heizleistung_Max"),
                    Luftwechsel_Infiltration = BaustoffCtrl.ZahlAus(r, "Luftwechsel_Infiltration"),
                    Luftwechsel_Nutzer = BaustoffCtrl.ZahlAus(r, "Luftwechsel_Nutzer"),
                    Interne_Waermegewinne = BaustoffCtrl.ZahlAus(r, "Interne_Waermegewinne"),
                    Bewohner = BaustoffCtrl.ZahlAus(r, "Bewohner"),
                    Kuehl_Sollwert = BaustoffCtrl.ZahlAus(r, "Kuehl_Sollwert"),
                    Kuehlleistung_Max = BaustoffCtrl.ZahlAus(r, "Kuehlleistung_Max"),
                    Kuehlung_Aktiv = BaustoffCtrl.GanzAus(r, "Kuehlung_Aktiv") is int ka ? ka != 0 : (bool?)null,
                    Kuehl_Sollwert_Nacht = BaustoffCtrl.ZahlAus(r, "Kuehl_Sollwert_Nacht"),
                    Uebergabe_Art = BaustoffCtrl.TextAus(r, "Uebergabe_Art"),
                    Uebergabe_Exponent = BaustoffCtrl.ZahlAus(r, "Uebergabe_Exponent"),
                    Uebergabe_Leistung_Nenn = BaustoffCtrl.ZahlAus(r, "Uebergabe_Leistung_Nenn"),
                    Kuehl_Uebergabe_Art = BaustoffCtrl.TextAus(r, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_ART),
                    Kuehl_Uebergabe_Exponent = BaustoffCtrl.ZahlAus(r, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT),
                    Kuehl_Uebergabe_Leistung_Nenn = BaustoffCtrl.ZahlAus(r, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_LEISTUNG_NENN),
                    Herkunft = BaustoffCtrl.TextAus(r, "Herkunft"),
                    Quellkennung = BaustoffCtrl.TextAus(r, "Quellkennung")
                };
                liste.Add(z);
                jeId[z.ID] = z;
            }
            if (bauteile != null)
                foreach (DataRow r in bauteile.Rows)
                {
                    var b = new BauteilModel
                    {
                        ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                        ID_Zone = Convert.ToInt32(r["ID_Zone"], CultureInfo.InvariantCulture),
                        Rang = Convert.ToInt32(r["Rang"], CultureInfo.InvariantCulture),
                        Bezeichner = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                        Bauteilart = Convert.ToString(r["Bauteilart"], CultureInfo.InvariantCulture),
                        ID_Aufbau = BaustoffCtrl.GanzAus(r, "ID_Aufbau"),
                        Flaeche = Convert.ToDouble(r["Flaeche"], CultureInfo.InvariantCulture),
                        U_Wert = BaustoffCtrl.ZahlAus(r, "U_Wert"),
                        g_Wert = BaustoffCtrl.ZahlAus(r, "g_Wert"),
                        Rahmenanteil = BaustoffCtrl.ZahlAus(r, "Rahmenanteil"),
                        Verschattungsfaktor = BaustoffCtrl.ZahlAus(r, "Verschattungsfaktor"),
                        Neigung = BaustoffCtrl.ZahlAus(r, "Neigung"),
                        Azimut = BaustoffCtrl.ZahlAus(r, "Azimut"),
                        Randbedingung = BaustoffCtrl.TextAus(r, "Randbedingung"),
                        ID_Nachbarzone = BaustoffCtrl.GanzAus(r, ZonenkopplungSchema.SPALTE_ID_NACHBARZONE),
                        Trennflaeche_Zuordnung = BaustoffCtrl.TextAus(r, ZonenkopplungSchema.SPALTE_TRENNFLAECHE_ZUORDNUNG),
                        Psi_L = BaustoffCtrl.ZahlAus(r, "Psi_L"),
                        Herkunft = BaustoffCtrl.TextAus(r, "Herkunft"),
                        Quellkennung = BaustoffCtrl.TextAus(r, "Quellkennung")
                    };
                    if (jeId.TryGetValue(b.ID_Zone, out ZoneModel z)) z.Bauteile.Add(b);
                }
            return liste;
        }
    }
}
