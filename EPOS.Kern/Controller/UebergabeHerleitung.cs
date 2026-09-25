using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die hergeleiteten Vorgaben der Wärmeübergabe eines Gebäudesatzes</b> (Anlagenkopplung
    /// 8.4, H10) — die zwei Zahlen, die der Gebäudedialog als „Vorgabe" neben die leeren Felder
    /// schreibt: das kälteste Tagesmittel der Klimareihe (abgerundet) und die stationäre
    /// Auslegungsheizlast des Satzes. Ausdrücklich <b>kein Normnachweis</b> (H-F12).
    /// </summary>
    internal sealed class UebergabeHerleitung
    {
        /// <summary>Die hergeleitete Auslegungs-Außentemperatur [°C]; <c>null</c> ohne Herleitung.</summary>
        internal double? AuslegungAussenC { get; init; }

        /// <summary>Die hergeleitete Auslegungsheizlast des Satzes [kW] (= Nennleistung bei leerem Feld); <c>null</c> ohne Herleitung.</summary>
        internal double? AuslegungsheizlastKw { get; init; }

        /// <summary>Warum es keine Zahl gibt — der benannte Grund (eine Prüfung des Eingangsbauers); leer mit Zahl.</summary>
        internal string Befund { get; init; } = "";
    }

    /// <summary>
    /// <b>Die Quelle der hergeleiteten Vorgaben</b> für den Gebäudedialog (Anlagenkopplung 8.4, 9.1:
    /// „Jede Vorgabe steht als Zahl in der Herleitungszeile"). Sie ruft den Rechenweg des Laufs —
    /// sie schreibt ihn nicht ab (Kern-Regel „Eine Auskunft ruft den Rechenweg des Laufs"): derselbe
    /// Klimakalender (<see cref="SimulationWaermebedarf.KlimakalenderLesen"/>) und derselbe
    /// Eingangsbauer (<see cref="SimulationWaermebedarf.UebergabeEingang"/>) mit Stufe AK1.
    ///
    /// <para><b>Der Klimakalender wird einmal je Quelle gelesen</b> (beim ersten Aufruf) — der
    /// Dialog hält eine Quelle, solange er offen ist, und leitet nach jeder Eingabe neu her, ohne
    /// die Klimadaten wieder zu lesen.</para>
    ///
    /// <para><b>Katalogbau, nicht Projektgebäude.</b> Hergeleitet wird für den Satz, wie er im
    /// Dialog steht; im Lauf skaliert die Nachmultiplikation (E8) die hergeleitete Nennleistung
    /// mit dem Gebäude (H7). Ohne Projekt oder ohne Klimaregion gibt es keine Zahl.</para>
    /// </summary>
    internal sealed class UebergabeHerleitungsquelle
    {
        private readonly int _idProjekt;
        private SimulationWaermebedarf _klima;
        private bool _gelesen;

        /// <param name="idProjekt">Das Projekt, dessen Klimaregion gilt; ≤ 0 = keines.</param>
        internal UebergabeHerleitungsquelle(int idProjekt)
        {
            _idProjekt = idProjekt;
        }

        /// <summary>Zahl der Klimakalender, die diese Quelle gelesen hat (Probe: höchstens einer).</summary>
        internal int Klimalesungen { get; private set; }

        /// <summary>
        /// Leitet Auslegungs-Außentemperatur und Auslegungsheizlast für <paramref name="satz"/> her.
        /// <c>null</c>, wenn es nichts herzuleiten gibt (keine Übergabeart, kein Projekt, keine
        /// Klimaregion); ein <see cref="UebergabeHerleitung.Befund"/>, wenn der Eingangsbauer den Satz
        /// ablehnt.
        /// </summary>
        internal UebergabeHerleitung Herleiten(GebaeudeModel satz)
        {
            if (satz == null || !Waermeuebergabe.ArtBekannt(satz.Uebergabe_Art)) return null;
            SimulationWaermebedarf klima = Klima();
            if (klima == null) return null;

            ProjektGebaeudeModel g = AusKatalogsatz(satz);
            g.Heizkreis_Aktiv = true;
            try
            {
                GebaeudeModellEingang e = klima.UebergabeEingang(g);
                return new UebergabeHerleitung
                {
                    AuslegungAussenC = e.AuslegungAussentemperaturHergeleitet
                        ? e.AuslegungAussentemperaturC
                        : Math.Floor(KaeltestesTagesmittel(e)),
                    AuslegungsheizlastKw = e.AuslegungsheizlastW / 1000.0,
                };
            }
            catch (GebaeudeModellException ex)
            {
                return new UebergabeHerleitung { Befund = ex.Message };
            }
        }

        /// <summary>Das kälteste Tagesmittel der Außenluft des Eingangs [°C] (H10) — dieselbe Bildung wie im Eingangsbauer.</summary>
        private static double KaeltestesTagesmittel(GebaeudeModellEingang e)
        {
            GebaeudeModellEingang.KaeltesterTag(e.ThetaOut, out double mittel);
            return mittel;
        }

        private SimulationWaermebedarf Klima()
        {
            if (_gelesen) return _klima;
            _gelesen = true;
            if (_idProjekt <= 0) return null;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(_idProjekt);
            int idKlimaregion = projekt.m_ID_Klimaregion;
            if (idKlimaregion <= 0) return null;

            var sim = new SimulationWaermebedarf { m_ID_Projekt = _idProjekt };
            sim.KlimakalenderLesen(idKlimaregion);
            Klimalesungen++;
            _klima = sim;
            return _klima;
        }

        /// <summary>
        /// <b>Ein Katalogsatz als Projektgebäude</b> — jedes öffentliche Feld gleichen Namens und
        /// Typs wird übernommen. Die beiden Modelle bilden dieselben Spalten ab
        /// (<c>Tab_Gebaeude(_STAMM)</c> bzw. die Sicht <c>Abfrage_Projektgebaeude</c>); was nur das
        /// Projekt kennt (Zuordnung, Einheit, Fläche der Auswahl), bleibt leer. Die Probe
        /// <c>AnlagenkopplungDialogKernTests</c> hält fest, dass jedes Feld, das der Eingangsbauer
        /// liest, ankommt.
        /// </summary>
        internal static ProjektGebaeudeModel AusKatalogsatz(GebaeudeModel satz)
        {
            if (satz == null) throw new ArgumentNullException(nameof(satz));
            var ziel = new ProjektGebaeudeModel();
            foreach (FieldInfo quelle in typeof(GebaeudeModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                FieldInfo f = typeof(ProjektGebaeudeModel).GetField(quelle.Name, BindingFlags.Public | BindingFlags.Instance);
                if (f == null || f.FieldType != quelle.FieldType || f.IsInitOnly) continue;
                f.SetValue(ziel, quelle.GetValue(satz));
            }
            return ziel;
        }

        /// <summary>Die Felder, die <see cref="AusKatalogsatz"/> überträgt (für die Probe).</summary>
        internal static IReadOnlyList<string> Uebertragen()
        {
            var namen = new List<string>();
            foreach (FieldInfo quelle in typeof(GebaeudeModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                FieldInfo f = typeof(ProjektGebaeudeModel).GetField(quelle.Name, BindingFlags.Public | BindingFlags.Instance);
                if (f != null && f.FieldType == quelle.FieldType && !f.IsInitOnly) namen.Add(quelle.Name);
            }
            return namen;
        }
    }
}
