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
        {
            befund = null;
            if (zonenkalender != null && zonenkalender.TryGetValue(groesse, out Konditionierungskalender z) && z != null)
                return z;
            if (gebaeudekalender != null && gebaeudekalender.TryGetValue(groesse, out Konditionierungskalender b) && b != null)
                return b;
            if (matrix == null || !Wirksam(matrix, groesse)) return null;

            befund = Standardfahrplan.Erzeugen(matrix, groesse, rundlaufPruefen: false);
            return befund.Befund == Fahrplanbefund.Erzeugt ? befund.Kalender : null;
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
}
