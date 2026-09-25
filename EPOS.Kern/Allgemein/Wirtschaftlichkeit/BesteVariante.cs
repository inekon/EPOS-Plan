using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die beste Variante einer Vergleichsgruppe</b> (Konzept Berichtsvorlagen 5.1 und 9.5,
    /// Etappe BV-E3) — EINE Regel dafür, welchen Stand die vier Kennzahlkarten der
    /// Wirtschaftlichkeitsseite zeigen. Dieselbe Regel liefert im Bericht die Werte
    /// <c>wirtschaft.beste.anzeige</c> und <c>wirtschaft.beste.&lt;zeile&gt;</c> (Etappe BV-E4);
    /// stünde sie zweimal da, nennten Karte und Bericht verschiedene Varianten.
    ///
    /// <para><b>Die Regel in Worten.</b></para>
    /// <list type="number">
    /// <item><b>Szenario.</b> Gewählt wird im Erwartungsfall
    /// (<see cref="WirtschaftlichkeitSzenario.ERWARTET"/>) — gleich, welches Szenario die Seite
    /// gerade darunter zeigt. Ein anderes Szenario nur, wenn der Aufrufer es ausdrücklich
    /// nennt.</item>
    /// <item><b>Stände.</b> Es zählen die übergebenen Stände in ihrer Reihenfolge (auf der Seite
    /// die gewählten Spalten), je Stand sein erstes Ergebnis in diesem Szenario. Ein Stand ohne
    /// Ergebnis nimmt nicht teil: Er wird weder als Null gezählt noch verdrängt er einen
    /// anderen. Ohne Standliste zählen alle Ergebnisse des Szenarios in ihrer Listenfolge, je
    /// Stand das erste.</item>
    /// <item><b>Kriterium.</b> Die Kapitalwertdifferenz
    /// (<see cref="WirtschaftlichkeitErgebnis.KapitalwertDiff"/>, gerechnet gegen die Referenz
    /// der Rechnung). Teil nehmen nur Varianten — Ergebnisse OHNE den Merker
    /// <see cref="WirtschaftlichkeitErgebnis.IstStamm"/> —, die eine Differenz tragen; die
    /// Referenz selbst trägt keine und nimmt deshalb nie teil, der Stamm auch dann nicht, wenn
    /// er gegen eine Referenzvariante eine Differenz trägt. Beste ist die Variante mit der
    /// GRÖSSTEN Differenz, auch wenn sie negativ ist: Ein Vergleich mit dem Stamm findet
    /// nicht statt, die Karte zeigt dann, um wie viel die beste Variante schlechter ist.</item>
    /// <item><b>Gleichstand.</b> Bei gleicher Differenz bleibt der Stand, der in der
    /// Reihenfolge zuerst kommt.</item>
    /// <item><b>Ohne Variante der Stamm.</b> Trägt keine Variante eine Differenz (keine gewählt,
    /// keine gerechnet oder nur die Referenz), steht das Ergebnis des Stamms: das erste mit dem
    /// Merker IstStamm unter den Ständen. Fehlt auch das, gibt es kein Ergebnis — die Auswahl
    /// nennt dann den Stamm (<c>idStamm</c>) ohne Ergebnis.</item>
    /// </list>
    ///
    /// <para><b>Stamm heißt Merker.</b> Ob ein Ergebnis der Stamm ist, entscheidet sein Merker
    /// <see cref="WirtschaftlichkeitErgebnis.IstStamm"/>, nicht der Vergleich mit <c>idStamm</c>.
    /// Die Kennung nennt den Stand der Auswahl nur, wenn kein Ergebnis vorliegt.</para>
    ///
    /// <para><b>Plattformfrei und ohne Datenbank:</b> Die Klasse bekommt die Ergebnisse
    /// hereingereicht, rechnet nichts und wählt nur aus.</para>
    /// </summary>
    public static class BesteVariante
    {
        /// <summary>Warum die <see cref="Auswahl"/> diesen Stand nennt.</summary>
        public enum Auswahlgrund
        {
            /// <summary>
            /// Kein Ergebnis zum Zeigen: keine Variante mit Kapitalwertdifferenz und kein
            /// Ergebnis des Stamms im Szenario. <see cref="Auswahl.Ergebnis"/> ist <c>null</c>,
            /// <see cref="Auswahl.IdProjekt"/> nennt den Stamm.
            /// </summary>
            KeinErgebnis = 0,

            /// <summary>
            /// Keine Variante trägt eine Kapitalwertdifferenz (keine gewählt, keine gerechnet
            /// oder nur die Referenz) — es steht das Ergebnis des Stamms.
            /// </summary>
            StammOhneVarianten = 1,

            /// <summary>Die Variante mit der größten Kapitalwertdifferenz.</summary>
            BestesKriterium = 2
        }

        /// <summary>Die Antwort der Regel: welcher Stand, mit welchem Ergebnis, aus welchem Grund.</summary>
        public sealed class Auswahl
        {
            /// <summary>
            /// Der gewählte Stand (<c>Tab_Projekt.ID</c>): die beste Variante, sonst der Stamm —
            /// bei <see cref="Auswahlgrund.KeinErgebnis"/> die übergebene Kennung des Stamms.
            /// </summary>
            public int IdProjekt;

            /// <summary>
            /// Sein Ergebnis im <see cref="Szenario"/>; <c>null</c> nur bei
            /// <see cref="Auswahlgrund.KeinErgebnis"/>. Beim Stamm können die Kennzahlen leer
            /// sein (nicht bestimmbar) — die Auswahl bleibt trotzdem der Stamm.
            /// </summary>
            public WirtschaftlichkeitErgebnis Ergebnis;

            /// <summary>Warum dieser Stand.</summary>
            public Auswahlgrund Grund;

            /// <summary>Das Szenario, in dem gewählt wurde.</summary>
            public string Szenario = WirtschaftlichkeitSzenario.ERWARTET;

            /// <summary>Ist der gewählte Stand eine Variante (und nicht der Stamm)?</summary>
            public bool IstVariante => Grund == Auswahlgrund.BestesKriterium;
        }

        /// <summary>
        /// Wählt nach der Regel dieser Klasse (siehe dort) den Stand, den die Kennzahlkarten
        /// zeigen.
        /// </summary>
        /// <param name="ergebnisse">
        /// Die Ergebnisse der Gruppe, alle Szenarien gemischt; <c>null</c> oder leer =
        /// <see cref="Auswahlgrund.KeinErgebnis"/>. Einträge <c>null</c> werden übergangen.
        /// </param>
        /// <param name="idStamm">
        /// Der Stamm der Gruppe — genannt, wenn kein Ergebnis vorliegt. Die Regel selbst erkennt
        /// den Stamm an seinem Merker.
        /// </param>
        /// <param name="staende">
        /// Die teilnehmenden Stände in ihrer Reihenfolge (auf der Seite die gewählten Spalten);
        /// sie entscheidet den Gleichstand. <c>null</c> = alle Ergebnisse des Szenarios in ihrer
        /// Listenfolge.
        /// </param>
        /// <param name="szenario">
        /// Das Szenario der Wahl; Vorgabe und <c>null</c> = Erwartungsfall.
        /// </param>
        public static Auswahl Waehle(IReadOnlyList<WirtschaftlichkeitErgebnis> ergebnisse, int idStamm,
                                     IReadOnlyList<int> staende = null,
                                     string szenario = WirtschaftlichkeitSzenario.ERWARTET)
        {
            string s = szenario ?? WirtschaftlichkeitSzenario.ERWARTET;
            List<WirtschaftlichkeitErgebnis> kandidaten = ErgebnisseDerStaende(ergebnisse, staende, s);

            // Die größte Kapitalwertdifferenz unter den Varianten; bei Gleichstand bleibt die
            // erste (strikt größer).
            WirtschaftlichkeitErgebnis beste = null;
            foreach (WirtschaftlichkeitErgebnis x in kandidaten)
                if (!x.IstStamm && x.KapitalwertDiff.HasValue &&
                    (beste == null || x.KapitalwertDiff.Value > beste.KapitalwertDiff.Value))
                    beste = x;

            if (beste != null)
                return new Auswahl
                {
                    IdProjekt = beste.IdProjekt,
                    Ergebnis = beste,
                    Grund = Auswahlgrund.BestesKriterium,
                    Szenario = s
                };

            // Ohne Variante mit Differenz der Stamm — das erste Ergebnis mit dem Merker.
            WirtschaftlichkeitErgebnis stamm = kandidaten.Find(x => x.IstStamm);
            if (stamm != null)
                return new Auswahl
                {
                    IdProjekt = stamm.IdProjekt,
                    Ergebnis = stamm,
                    Grund = Auswahlgrund.StammOhneVarianten,
                    Szenario = s
                };

            return new Auswahl
            {
                IdProjekt = idStamm,
                Ergebnis = null,
                Grund = Auswahlgrund.KeinErgebnis,
                Szenario = s
            };
        }

        /// <summary>
        /// Je Stand sein erstes Ergebnis im Szenario, in der Reihenfolge der Stände; ein Stand
        /// ohne Ergebnis fehlt. Ohne Standliste die Ergebnisse des Szenarios in Listenfolge,
        /// je Stand das erste.
        /// </summary>
        private static List<WirtschaftlichkeitErgebnis> ErgebnisseDerStaende(
            IReadOnlyList<WirtschaftlichkeitErgebnis> ergebnisse, IReadOnlyList<int> staende, string szenario)
        {
            var liste = new List<WirtschaftlichkeitErgebnis>();
            if (ergebnisse == null) return liste;

            if (staende != null)
            {
                foreach (int id in staende)
                {
                    WirtschaftlichkeitErgebnis e = Erstes(ergebnisse, id, szenario);
                    if (e != null) liste.Add(e);
                }
                return liste;
            }

            var gesehen = new HashSet<int>();
            foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
                if (e != null && string.Equals(e.Szenario, szenario, StringComparison.Ordinal) &&
                    gesehen.Add(e.IdProjekt))
                    liste.Add(e);
            return liste;
        }

        private static WirtschaftlichkeitErgebnis Erstes(IReadOnlyList<WirtschaftlichkeitErgebnis> ergebnisse,
                                                         int idProjekt, string szenario)
        {
            foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
                if (e != null && e.IdProjekt == idProjekt &&
                    string.Equals(e.Szenario, szenario, StringComparison.Ordinal))
                    return e;
            return null;
        }
    }
}
