using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

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
            => string.Join(", ", new[] { "ID" }.Concat(ZonenSchema.Bauteilspalten).Select(s => alias + ".\"" + s + "\""));

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
        /// = gültig, sonst die Meldung mit dem Namen der Zone bzw. des Bauteils.
        /// </summary>
        public static string Pruefen(IList<ZoneModel> zonen)
        {
            int nr = 0;
            foreach (ZoneModel z in zonen ?? new List<ZoneModel>())
            {
                nr++;
                if (z == null) continue;
                if (string.IsNullOrWhiteSpace(z.Bezeichner))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_NAME_LEER, nr);
                string zn = z.Bezeichner.Trim();
                string t = BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_NAME, zn, BaustoffSchema.LAENGE_BEZEICHNER)
                           ?? BaustoffCtrl.Laenge(MyResource.Resource.BAUTEIL_FELD_QUELLKENNUNG, z.Quellkennung, BaustoffSchema.LAENGE_QUELLKENNUNG)
                           ?? BaustoffCtrl.HerkunftPruefen(z.Herkunft);
                if (t != null) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_WERT, zn, t);
                if (z.Nutzflaeche.HasValue && (!(z.Nutzflaeche.Value > 0.0) || double.IsInfinity(z.Nutzflaeche.Value)))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_NUTZFLAECHE, zn);
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
                    string w = BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_NAME, bn, BaustoffSchema.LAENGE_BEZEICHNER)
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
            return null;
        }

        /// <summary>
        /// <b>Die Prüfregeln EINES Bauteils</b> (Mehrzonenkonzept 5.3, Ebene Bauteil) — die Regeln
        /// des Bauteildialogs, genau einmal, im Rückruf seiner Leiste: Name, Fläche größer null,
        /// U-Wert 0,1 … 6 W/(m²K), 0 &lt; g ≤ 1, Rahmenanteil 0,05 … 0,6, Verschattung (0; 1],
        /// Azimut 0 … 360°, Neigung 0 … 180°, ψ·L nicht negativ; ein Außenbauteil, das nicht
        /// waagerecht liegt, braucht einen Azimut (<see cref="BrauchtAzimut"/> — benannt
        /// abgelehnt, nicht auf Nord vorbelegt); die Randbedingung „Nachbarzone" rechnet EPOS erst
        /// mit mehreren Zonen (G6); ein Fenster oder eine Vorhangfassade grenzt an Außenluft oder
        /// einen unbeheizten Raum und trägt einen U-Wert; jedes andere Bauteil außerhalb der Zone
        /// braucht einen U-Wert oder einen Aufbau. Ohne Datenbank.
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
            if (b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE)
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_RAND_ZONE, name);

            var zeile = new BauteilModel { Bauteilart = b.Bauteilart, Randbedingung = b.Randbedingung, Neigung = b.Neigung };
            if (!b.Azimut.HasValue && BrauchtAzimut(zeile))
                return string.Format(k, MyResource.Resource.BAUTEIL_MSG_AZIMUT_FEHLT, name);

            Bauteilart? art = GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart);
            Bauteilrand? rand = GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung);
            bool transparent = art == Bauteilart.Fenster || art == Bauteilart.Vorhangfassade;
            if (transparent && rand != Bauteilrand.Aussenluft && rand != Bauteilrand.Unbeheizt)
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
        /// <param name="Leistungsgrenzen">Trägt das Gebäude eine Leistungsgrenze (Heizung, oder Kühlung bei
        /// gekühltem Gebäude)? Sie folgt dem Flächenschlüssel nicht: Im Klassenweg galt sie dem Katalogbau und
        /// wurde mit ihm nachmultipliziert, mit der Zone gilt sie der hochgerechneten Hülle — die Rückfrage nennt es.</param>
        public sealed record Uebernahmevorschlag(bool Ok, string Meldung, double Faktor, ZoneModel Zone,
                                                 double NutzflaecheGebaeude, string Einheit, double Angabe,
                                                 bool Leistungsgrenzen = false)
        {
            /// <summary>Ist die Angabe ein Verbrauch (keine Fläche)?</summary>
            public bool Verbrauchsangabe => !string.Equals(Einheit, GebaeudeVorbereitung.EINHEIT_FLAECHE, StringComparison.Ordinal);
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
            bool grenzen = g.Heizleistung_Max.HasValue || (g.Kuehlung_Aktiv && g.Kuehlleistung_Max.HasValue);

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            double faktor = GebaeudeBedarfCtrl.Hochrechnungsfaktor(idProjekt, projekt.m_ID_Klimaregion, g, out string befund);
            if (double.IsNaN(faktor))
                return new Uebernahmevorschlag(false, befund ?? "", double.NaN, null, g.Nutzflaeche, einheit, angabe);

            ZoneModel zone = GebaeudeZonenabbildung.AlsZoneModel(GebaeudeZonenuebernahme.AlsEineZone(g, faktor));
            zone.ID_Gebaeude = g.ID_Gebaeude;
            return new Uebernahmevorschlag(true, "", faktor, zone, g.Nutzflaeche, einheit, angabe, grenzen);
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
        /// </summary>
        public Ergebnis SpeichernJeGebaeude(int idGebaeude, IList<ZoneModel> zonen)
        {
            List<ZoneModel> liste = (zonen ?? new List<ZoneModel>()).Where(z => z != null).ToList();
            foreach (ZoneModel z in liste) z.Bauteile ??= new List<BauteilModel>();
            string fehler = Pruefen(liste);
            if (fehler != null) return Ergebnis.Fehler(fehler);

            // Die Spalten der Zone: die von ZonenSchema und - mit Schritt 137 - die drei der
            // Kuehluebergabe (E37); festgestellt VOR dem Vorgang, auf der gewoehnlichen Verbindung.
            IReadOnlyList<string> kuehl = Kuehlspalten();
            List<string> spalten = ZonenSchema.Zonenspalten.Concat(kuehl).ToList();
            IEnumerable<DbParam> Werte(ZoneModel z) => kuehl.Count > 0 ? Zonenwerte(z).Concat(Kuehlwerte(z)) : Zonenwerte(z);

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

                    var zonenBleiben = new HashSet<int>(liste.Where(z => z.ID > 0).Select(z => z.ID));
                    var bauteileBleiben = new HashSet<int>(liste.SelectMany(z => z.Bauteile.Where(b => b != null && b.ID > 0))
                                                                .Select(b => b.ID));

                    // 1) Entfernen: die Bauteile, die in der Liste fehlen.
                    foreach (int id in bauteilBestand.Where(id => !bauteileBleiben.Contains(id)))
                        v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" WHERE \"ID\" = ?", new DbParam("@id", id));

                    // 2) Aendern und Anlegen der Zonen, Rang aus der Listenreihenfolge.
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
                            z.ID = v.EinfuegenUndId("INSERT INTO \"" + ZonenSchema.TAB_ZONE + "\" (" +
                                                    string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                                                    ") VALUES (" + BaustoffCtrl.Fragezeichen(spalten.Count) + ")",
                                                    Werte(z).ToArray());
                    }

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
                            if (b.ID > 0)
                                v.Ausfuehren("UPDATE \"" + ZonenSchema.TAB_BAUTEIL + "\" SET " +
                                             string.Join(", ", ZonenSchema.Bauteilspalten.Select(s => "\"" + s + "\" = ?")) +
                                             " WHERE \"ID\" = ?", Bauteilwerte(b).Append(new DbParam("@id", b.ID)).ToArray());
                            else
                                b.ID = v.EinfuegenUndId("INSERT INTO \"" + ZonenSchema.TAB_BAUTEIL + "\" (" +
                                                        string.Join(", ", ZonenSchema.Bauteilspalten.Select(s => "\"" + s + "\"")) +
                                                        ") VALUES (" + BaustoffCtrl.Fragezeichen(ZonenSchema.Bauteilspalten.Count) + ")",
                                                        Bauteilwerte(b).ToArray());
                        }
                        z.Bauteile = z.Bauteile.Where(x => x != null).ToList();
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
        //  intern
        // =================================================================

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
