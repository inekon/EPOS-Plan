using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Brücke zwischen Gebäudezeile und Kalendermodell</b> (Konzept
    /// Konditionierungsprofile 3.4 und 6): Sie liest die Bestandsfelder einer Gebäude- oder
    /// Zonenzeile in einen <see cref="Matrixeingang"/>, bildet daraus mit den Vorgabezeilen die
    /// <see cref="Vorgabematrix"/>, löst die Kaskade Zone → Gebäude auf und liefert je Größe die
    /// <b>erste Quelle</b> der Kette:
    ///
    /// <list type="number">
    /// <item>Kalender der Zone angelegt,</item>
    /// <item>Kalender des Gebäudes angelegt,</item>
    /// <item>abgeleitet aus der wirksamen Matrix der Zone (Konzept 3.4).</item>
    /// </list>
    ///
    /// <para><b>Die Bauvorschrift der Byte-Gleichheit</b> (Konzept 6) steht in
    /// <see cref="Wirksam"/>: Ohne angelegten Kalender, ohne neue Matrixzelle und ohne Schalter
    /// <b>gibt es keinen Kalender</b> — der Eingang nimmt dann wörtlich den Bestandszweig, ohne
    /// Multiplikation mit 1, ohne neues Minimum und ohne Umweg über den Kalender.</para>
    ///
    /// <para><b>Ohne Datenbank:</b> Die Zeilen kommen als Listen herein; der Controller holt sie,
    /// der Lauf bekommt sie vom Datenweg. EINE Stelle, an der die Abbildung Bestandsspalte → Zelle
    /// steht.</para>
    /// </summary>
    public static class Konditionierungseingang
    {
        /// <summary>
        /// Liest die Bestandsfelder einer Gebäude- oder Zonenzeile in einen
        /// <see cref="Matrixeingang"/>. <paramref name="kopplungWirksam"/> und
        /// <paramref name="kuehlungWirksam"/> entscheidet der Lauf vor dem Fahrplan (AK1 bzw. E32);
        /// der Dialog übergibt dort, was er weiß.
        /// </summary>
        public static Matrixeingang Bestand(ProjektGebaeudeModel g, bool kopplungWirksam, bool kuehlungWirksam)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            Gebaeudemodellvorgaben.WirksamerLuftwechsel(g.Luftwechselrate, g.Luftwechsel_Infiltration,
                                                        g.Luftwechsel_Nutzer, out Luftwechselherkunft herkunft);
            var e = new Matrixeingang
            {
                SollTag = Endlich(g.Raumsolltemperatur_Tag),
                SollNacht = Endlich(g.Raumsolltemperatur_Nachtabsenkung),
                SollWochenende = Endlich(g.Raumsolltemperatur_Wochenende),
                SollFerien = Endlich(g.Raumsolltemperatur_Ferien),
                NachtBeginn = g.Nachtabsenkung_Beginn,
                NachtEnde = g.Nachtabsenkung_Ende,
                Ferienmerker = g.Ferien,
                Wochenendmerker = g.Wochenende,
                Sollwertprofil = g.Sollwertprofil,
                KopplungWirksam = kopplungWirksam,
                KuehlSollwert = g.Kuehl_Sollwert,
                KuehlSollwertNacht = g.Kuehl_Sollwert_Nacht,
                KuehlungWirksam = kuehlungWirksam,
                LuftwechselInfiltration = g.Luftwechsel_Infiltration,
                LuftwechselNutzer = g.Luftwechsel_Nutzer,
                Luftwechselrate = Endlich(g.Luftwechselrate),
                LuftwechselAusGesamtangabe = herkunft == Luftwechselherkunft.Luftwechselrate,
                InterneWaermegewinne = Endlich(g.Interne_Waermegewinne),
                Bewohner = g.Bewohner,
                Maximaleraumtemperatur = Endlich(g.Maximaleraumtemperatur),
            };
            e.Ferienbeginn[0] = g.Ferienbeginn_1;
            e.Ferienbeginn[1] = g.Ferienbeginn_2;
            e.Ferienbeginn[2] = g.Ferienbeginn_3;
            e.Ferienbeginn[3] = g.Ferienbeginn_4;
            e.Ferienende[0] = g.Ferienende_1;
            e.Ferienende[1] = g.Ferienende_2;
            e.Ferienende[2] = g.Ferienende_3;
            e.Ferienende[3] = g.Ferienende_4;
            return e;
        }

        /// <summary>
        /// Die wirksame Matrix eines Gebäudes (oder Katalogbaus) — Bestandsfelder plus
        /// Vorgabezeilen.
        /// </summary>
        public static Vorgabematrix Matrix(ProjektGebaeudeModel g, IEnumerable<Vorgabezeile> vorgaben,
                                           bool kopplungWirksam, bool kuehlungWirksam)
            => Vorgabematrix.Bilden(Bestand(g, kopplungWirksam, kuehlungWirksam), vorgaben);

        /// <summary>
        /// <b>Die erste Quelle je Größe</b> (Konzept 3.4). <paramref name="zonenkalender"/> und
        /// <paramref name="gebaeudekalender"/> sind die <em>angelegten</em> Kalender; fehlt beides,
        /// erzeugt der Generator aus <paramref name="matrix"/> — aber nur, wenn die Spalte eine
        /// Angabe trägt, die es ohne Kalender nicht gäbe (<see cref="Wirksam"/>).
        /// </summary>
        /// <returns>Der Kalender oder <c>null</c> — dann gilt wörtlich der Bestandszweig.</returns>
        public static Konditionierungskalender ErsteQuelle(
            Konditionierungsgroesse groesse, Vorgabematrix matrix,
            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> zonenkalender,
            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> gebaeudekalender,
            out Fahrplanlesung befund)
            => ErsteQuelle(groesse, matrix, zonenkalender, gebaeudekalender, false, out befund);

        /// <summary>
        /// Dieselbe Kette mit der <b>Nachtzeile der Zone</b> (Entwurf KP2, Festlegung 5): Trägt die
        /// Zone eine eigene Heiz-Nachtzeile mit Zeiten (<see cref="EigeneNachtzeile"/>), ist die
        /// Heizspalte wirksam — der Bestandszweig kennt nur die Nachtzeit des Gebäudes, die Matrix
        /// der Zone zeigt ihre eigene; so sehen Matrix und Lauf dieselbe.
        /// <para><b>Die Kette gilt wörtlich</b> (Konzept 3.4; festgelegt in Stufe KP2, Welle U1): Die
        /// Zone erbt den GANZEN angelegten Kalender des Gebäudes oder führt einen eigenen. Eine eigene
        /// Zelle der Zone — auch ihre Heiz-Nachtzeile — wirkt unter einem angelegten Kalender des
        /// Gebäudes erst, wenn die Zone einen eigenen Kalender anlegt („vom Gebäude übernehmen und
        /// anpassen"); die Zonenmatrix nennt diesen Zustand „vom Gebäude".</para>
        /// </summary>
        /// <param name="nachtzeileDerZone">Trägt die Zone eine eigene Heiz-Nachtzeile mit Zeiten?</param>
        public static Konditionierungskalender ErsteQuelle(
            Konditionierungsgroesse groesse, Vorgabematrix matrix,
            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> zonenkalender,
            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> gebaeudekalender,
            bool nachtzeileDerZone, out Fahrplanlesung befund)
        {
            befund = null;
            if (zonenkalender != null && zonenkalender.TryGetValue(groesse, out Konditionierungskalender z) && z != null)
                return z;
            if (gebaeudekalender != null && gebaeudekalender.TryGetValue(groesse, out Konditionierungskalender b) && b != null)
                return b;
            bool wirksam = Wirksam(matrix, groesse)
                           || (nachtzeileDerZone && groesse == Konditionierungsgroesse.Heizsoll);
            if (matrix == null || !wirksam) return null;

            befund = Standardfahrplan.Erzeugen(matrix, groesse, rundlaufPruefen: false);
            return befund.Befund == Fahrplanbefund.Erzeugt ? befund.Kalender : null;
        }

        /// <summary>
        /// <b>Der aufgelöste Bestand einer Zone</b> (Teilkonzept 3.4; Stufe KP2, Welle U4) — die EINE
        /// Stelle für den Lauf (<see cref="Konditionierungdatenweg"/>) und den Arbeitsstand
        /// (<see cref="Konditionierungsarbeitsstand.AufgeloesterBestand"/>): Sollwerte,
        /// Maximalraumtemperatur und Lüftung „Zone, sonst Gebäude", innere Gewinne und Bewohner aus der
        /// Vorgabenkaskade (eigener Wert, sonst Gebäude × Flächenanteil) — nur, wenn Zone oder Gebäude
        /// einen Wert führen; Nachtzeit, Ferienzeiträume, Merker und Kühlung kommen vom Gebäude, die
        /// Gesamtangabe der Lüftung folgt den aufgelösten Feldern. Ohne eigene Werte und mit dem Anteil 1
        /// ist jedes Feld das des Gebäudes.
        /// </summary>
        /// <param name="gebaeude">Die Bestandsfelder des Gebäudes.</param>
        /// <param name="zone">Die eigenen Angaben der Zone.</param>
        /// <param name="vorgaben">Die Vorgabenkaskade der Zone (<see cref="Zonenvorgaben.Bilden"/>).</param>
        public static Matrixeingang ZonenBestand(Matrixeingang gebaeude, Zoneneingaben zone, Zonenvorgaben vorgaben)
        {
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (vorgaben == null) throw new ArgumentNullException(nameof(vorgaben));
            Matrixeingang e = gebaeude.Kopie();
            e.SollTag = zone.SollTag ?? gebaeude.SollTag;
            e.SollNacht = zone.SollNacht ?? gebaeude.SollNacht;
            e.SollWochenende = zone.SollWochenende ?? gebaeude.SollWochenende;
            e.SollFerien = zone.SollFerien ?? gebaeude.SollFerien;
            e.Maximaleraumtemperatur = zone.Maximaleraumtemperatur ?? gebaeude.Maximaleraumtemperatur;
            e.LuftwechselInfiltration = zone.LuftwechselInfiltration ?? gebaeude.LuftwechselInfiltration;
            e.LuftwechselNutzer = zone.LuftwechselNutzer ?? gebaeude.LuftwechselNutzer;
            // KU3-3 (E67/E68): die Kühlzelle der Zone - Sollwert und Nachtwert der Zone, sonst des Gebäudes;
            // eine Zone mit Kuehlung_Aktiv = 0 oder ohne Sollwert kühlt nicht. Die Wirksamkeit (Projektschalter)
            // kommt mit dem Bestand des Aufrufers, im Lauf schon je Zone gebildet.
            e.KuehlSollwert = zone.KuehlSollwert ?? gebaeude.KuehlSollwert;
            e.KuehlSollwertNacht = zone.KuehlSollwertNacht ?? gebaeude.KuehlSollwertNacht;
            if (zone.KuehlungAktiv == false || !e.KuehlSollwert.HasValue) e.KuehlungWirksam = false;
            e.InterneWaermegewinne = zone.InterneWaermegewinne.HasValue || gebaeude.InterneWaermegewinne.HasValue
                ? vorgaben.InterneWaermegewinne.Wert : null;
            e.Bewohner = zone.Bewohner.HasValue || gebaeude.Bewohner.HasValue ? vorgaben.Bewohner.Wert : null;
            Gebaeudemodellvorgaben.WirksamerLuftwechsel(e.Luftwechselrate, e.LuftwechselInfiltration,
                                                        e.LuftwechselNutzer, out Luftwechselherkunft herkunft);
            e.LuftwechselAusGesamtangabe = herkunft == Luftwechselherkunft.Luftwechselrate;
            return e;
        }

        /// <summary>
        /// <b>Die wirksame Matrix einer Zone</b> (Konzept 3.4; Stufe KP2, Welle U4) — die EINE Stelle für
        /// den Lauf (<see cref="Konditionierungdatenweg"/>) und den Arbeitsstand
        /// (<see cref="Konditionierungsarbeitsstand.Matrix"/>): die Zeilen der Zone je Zelle über der
        /// Matrix des Gebäudes (F2).
        /// <para><b>Der Nennwert der Personen ist einer der Zone</b> — „Anteilskalender multiplizieren den
        /// Nennwert der Zone (eigener Wert oder Flächenanteil)": ihr eigener Wert, sonst der des Gebäudes ×
        /// <paramref name="flaechenanteil"/>, auf vier Nachkommastellen (Rundlauf). Wörtlich geerbt trüge
        /// jede Zone die Personen des ganzen Gebäudes. Den Nennwert der Geräte (die Bestandszelle
        /// <c>Interne_Waermegewinne</c>) schlüsselt schon der Bestand der Zone
        /// (<see cref="Zonenvorgaben"/>). Beim Anteil 1 bleibt jede Zelle bitgleich.</para>
        /// </summary>
        /// <param name="bestand">Die Bestandsfelder, über denen die Matrix der Zone gebildet wird.</param>
        /// <param name="zonenvorgaben">Die Vorgabezeilen der Zone.</param>
        /// <param name="gebaeudematrix">Die Matrix des Gebäudes über demselben Bestand.</param>
        /// <param name="flaechenanteil">A_Zone / A_Gebäude (<see cref="Zonenvorgaben.Flaechenanteil"/>).</param>
        public static Vorgabematrix Zonenmatrix(Matrixeingang bestand, IEnumerable<Vorgabezeile> zonenvorgaben,
                                                Vorgabematrix gebaeudematrix, double flaechenanteil)
        {
            if (bestand == null) throw new ArgumentNullException(nameof(bestand));
            Vorgabematrix zone = Vorgabematrix.Bilden(bestand.Kopie(), zonenvorgaben, Kalendereigentuemer.Zone);
            Vorgabematrix m = zone.Erben(gebaeudematrix);
            if (gebaeudematrix == null || flaechenanteil == 1.0) return m;

            Matrixzelle eigen = zone.Personen.Nennwert;
            Matrixzelle geerbt = gebaeudematrix.Personen.Nennwert;
            if (eigen.Belegt || !geerbt.Belegt || geerbt.Aus) return m;
            Matrixspalte s = m.Personen;
            Matrixzelle nennwert = Matrixzelle.AusWert(Anteilig(geerbt.Wert, flaechenanteil),
                                                       s.Nennwert.Von, s.Nennwert.Bis, s.Nennwert.BedingtK);
            return m.MitSpalte(Konditionierungsgroesse.Personen,
                               new Matrixspalte(s.Groesse, nennwert, s.Tag, s.Nacht, s.Wochenende, s.Ferien, s.Saison));
        }

        /// <summary>Ein Nennwert des Gebäudes × Flächenanteil, auf vier Nachkommastellen (Rundlauf).</summary>
        private static double Anteilig(double wert, double anteil)
            => Math.Round(wert * anteil, 4, MidpointRounding.AwayFromZero);

        /// <summary>
        /// <b>Die erste Quelle an einer Zone</b> (Konzept 3.4; Stufe KP2, Welle U4) — die Kette von
        /// <see cref="ErsteQuelle(Konditionierungsgroesse, Vorgabematrix, IReadOnlyDictionary{Konditionierungsgroesse, Konditionierungskalender}, IReadOnlyDictionary{Konditionierungsgroesse, Konditionierungskalender}, bool, out Fahrplanlesung)"/>,
        /// und gilt der angelegte Kalender des Gebäudes, trägt er an der Zone ihren Nennwert: Die Zone
        /// erbt den GANZEN Kalender, ein Anteilskalender multipliziert aber den Nennwert der Zone —
        /// ihren eigenen, sonst den des Gebäudekalenders × Flächenanteil
        /// (<see cref="Zonennennwerte"/>). Ohne Nennwert des Gebäudekalenders nimmt die Zone ihren
        /// eigenen oder bleibt ohne (Geräte: dann gilt der Rückfall <c>Interne_Waermegewinne</c> der Zone;
        /// Personen: 0 W, wie am Gebäude).
        /// </summary>
        public static Konditionierungskalender ErsteQuelleDerZone(
            Konditionierungsgroesse groesse, Vorgabematrix matrix,
            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> zonenkalender,
            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> gebaeudekalender,
            bool nachtzeileDerZone, Zonennennwerte nennwerte, out Fahrplanlesung befund)
        {
            Konditionierungskalender k = ErsteQuelle(groesse, matrix, zonenkalender, gebaeudekalender,
                                                     nachtzeileDerZone, out befund);
            if (k == null || nennwerte == null || !Konditionierungsgroessen.HatNennwert(groesse)) return k;
            bool eigener = zonenkalender != null && zonenkalender.TryGetValue(groesse, out Konditionierungskalender z) && z != null;
            bool vomGebaeude = !eigener && gebaeudekalender != null
                               && gebaeudekalender.TryGetValue(groesse, out Konditionierungskalender b) && ReferenceEquals(b, k);
            if (!vomGebaeude) return k;
            double? nennwert = nennwerte.Eigen(groesse)
                               ?? (k.Nennwert.HasValue ? Anteilig(k.Nennwert.Value, nennwerte.Flaechenanteil) : (double?)null);
            if (Kalendervergleich.Gleich(nennwert, k.Nennwert)) return k;
            return new Konditionierungskalender(k.Groesse, k.Grundangabe, nennwert, k.Perioden);
        }

        /// <summary>
        /// <b>Trägt eine Zone eine eigene Heiz-Nachtzeile mit Zeiten?</b> (Entwurf KP2,
        /// Festlegung 5) — die Zeile <c>HEIZSOLL</c>/<c>NACHT</c> mit Beginn oder Ende. Am Gebäude
        /// und am Katalogbau stehen die Heiz-Nachtzeiten allein in
        /// <c>Nachtabsenkung_Beginn</c>/<c>_Ende</c>; die Zone führt diese Spalten nicht.
        /// </summary>
        public static bool EigeneNachtzeile(IEnumerable<Vorgabezeile> zonenvorgaben)
        {
            if (zonenvorgaben == null) return false;
            string heiz = Konditionierungsgroessen.Kennwort(Konditionierungsgroesse.Heizsoll);
            foreach (Vorgabezeile v in zonenvorgaben)
                if (v != null && string.Equals(v.Groesse, heiz, StringComparison.Ordinal)
                    && string.Equals(v.Zeile, DbWerte.KOND_ZEILE_NACHT, StringComparison.Ordinal)
                    && (v.Von.HasValue || v.Bis.HasValue))
                    return true;
            return false;
        }

        /// <summary>
        /// <b>Trägt die Spalte etwas, das es ohne Kalender nicht gäbe?</b> Das ist die
        /// Bauvorschrift der Byte-Gleichheit (Konzept 6) — die Frage, die der Eingang <em>vor</em>
        /// dem Generator stellt:
        ///
        /// <list type="bullet">
        /// <item><b>Heizen:</b> Die vier Sollwerte, die Nachtzeit und die Ferien sind Bestandszellen
        /// und rechnen im Bestandszweig; <em>neu</em> ist allein die Saison (Heizperiode, E53) und
        /// ein „aus" aus der Vorgabetabelle.</item>
        /// <item><b>Kühlen:</b> Tag und Nacht sind Bestandszellen; der Bestandszweig rechnet die
        /// Nacht heute nicht — <em>eine gefüllte Nachtzelle ist deshalb neu</em> (P13 (a); in der
        /// Testdatenbank ist sie nirgends gefüllt, R14). Dazu Wochenende, Ferien und Saison.</item>
        /// <item><b>Lüftung:</b> Infiltration und Nutzerlüftung rechnen im Bestandszweig als
        /// Konstante; neu sind Nacht, Wochenende und Ferien.</item>
        /// <item><b>Geräte:</b> <c>Interne_Waermegewinne</c> rechnet als Konstante; neu ist jede
        /// Anteilszeile.</item>
        /// <item><b>Personen:</b> alles neu — ohne Kalender gibt es keine Personenwärme
        /// (Konzept 3.1).</item>
        /// </list>
        /// </summary>
        public static bool Wirksam(Vorgabematrix matrix, Konditionierungsgroesse groesse)
        {
            if (matrix == null) return false;
            Matrixspalte s = matrix.Spalte(groesse);
            switch (groesse)
            {
                case Konditionierungsgroesse.Heizsoll:
                    return Saison(s) || s.Tag.Aus || s.Nacht.Aus || s.Wochenende.Aus || s.Ferien.Aus;
                case Konditionierungsgroesse.Kuehlsoll:
                    return Saison(s) || s.Nacht.Belegt || s.Wochenende.Belegt || s.Ferien.Belegt || s.Tag.Aus;
                case Konditionierungsgroesse.Lueftung:
                    return s.Nacht.Belegt || s.Wochenende.Belegt || s.Ferien.Belegt || s.Tag.Aus;
                case Konditionierungsgroesse.Geraete:
                    return s.Tag.Belegt || s.Nacht.Belegt || s.Wochenende.Belegt || s.Ferien.Belegt;
                default:
                    return s.Nennwert.Belegt;
            }
        }

        private static bool Saison(Matrixspalte s) => s.Saison.Von.HasValue || s.Saison.Bis.HasValue;

        private static double? Endlich(double wert) => double.IsFinite(wert) ? wert : (double?)null;

        private static double? Endlich(double? wert)
            => wert.HasValue && double.IsFinite(wert.Value) ? wert : null;
    }

    /// <summary>
    /// <b>Die Nennwerte einer Zone</b> (Konzept 3.4; Stufe KP2, Welle U4) — was ein Anteilskalender an
    /// der Zone multipliziert: ihr EIGENER Wert, sonst der des Gebäudes × <see cref="Flaechenanteil"/>.
    /// </summary>
    /// <param name="Flaechenanteil">A_Zone / A_Gebäude (<see cref="Zonenvorgaben.Flaechenanteil"/>); 1 ohne eigene Fläche.</param>
    /// <param name="Geraete">Der eigene Geräte-Nennwert der Zone (<c>Interne_Waermegewinne</c> der Zone) [W]; <c>null</c> = keiner.</param>
    /// <param name="Personen">Der eigene Personen-Nennwert der Zone (Vorgabezelle) [W]; <c>null</c> = keiner.</param>
    public sealed record Zonennennwerte(double Flaechenanteil, double? Geraete, double? Personen)
    {
        /// <summary>Der eigene Nennwert der Größe; <c>null</c> = keiner (dann gilt der Flächenanteil).</summary>
        public double? Eigen(Konditionierungsgroesse g) => g switch
        {
            Konditionierungsgroesse.Geraete => Geraete,
            Konditionierungsgroesse.Personen => Personen,
            _ => null
        };

        /// <summary>Die Nennwerte einer Zone aus ihrem eigenen Bestand und ihren Vorgabezeilen.</summary>
        public static Zonennennwerte Aus(double flaechenanteil, Matrixeingang eigenerBestand, IEnumerable<Vorgabezeile> zonenvorgaben)
        {
            double? personen = null;
            string kennwort = Konditionierungsgroessen.Kennwort(Konditionierungsgroesse.Personen);
            foreach (Vorgabezeile v in zonenvorgaben ?? Array.Empty<Vorgabezeile>())
                if (v != null && !v.Aus && v.Wert.HasValue && double.IsFinite(v.Wert.Value)
                    && string.Equals(v.Groesse, kennwort, StringComparison.Ordinal)
                    && string.Equals(v.Zeile, DbWerte.KOND_ZEILE_NENNWERT, StringComparison.Ordinal))
                {
                    personen = v.Wert;
                    break;
                }
            return new Zonennennwerte(flaechenanteil, eigenerBestand?.InterneWaermegewinne, personen);
        }
    }
}
