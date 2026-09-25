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
    /// <b>Die hergeleiteten Vorgaben der Kühlübergabe eines Gebäudesatzes</b> (E37; Anlagenkopplung
    /// 7.2, 8.4, 9.1) — was der Gebäudedialog in die Herleitungszeilen der Kühlübergabe schreibt:
    /// Auslegungstag und seine Kühllast (= Nennleistung bei leerem Feld, sensibel), Quelle und Höhe
    /// des festen Kaltwasser-Vorlaufs und die Vorlaufgrenze. Ausdrücklich <b>kein Normnachweis</b>.
    /// </summary>
    internal sealed class KuehluebergabeHerleitung
    {
        /// <summary>Der Auslegungstag der Kühlung (0 … 364); <c>null</c> ohne Herleitung.</summary>
        internal int? Auslegungstag { get; init; }

        /// <summary>Der Auslegungstag als Monat und Tag in der Kultur des Aufrufs; leer ohne Herleitung.</summary>
        internal string AuslegungstagText { get; init; } = "";

        /// <summary>Das Tagesmittel der Außenluft am Auslegungstag [°C]; <c>null</c> ohne Herleitung.</summary>
        internal double? AuslegungstagMittelC { get; init; }

        /// <summary>Die Kühllast des Auslegungstags [kW] (= Nennleistung bei leerem Feld); <c>null</c> ohne Herleitung.</summary>
        internal double? AuslegungskuehllastKw { get; init; }

        /// <summary>Woher der Kaltwasser-Vorlauf kommt (Anlage oder Auslegung).</summary>
        internal Vorlaufquelle Vorlaufquelle { get; init; }

        /// <summary>Der Kaltwasser-Vorlauf der Quelle [°C] vor dem Hochmischen; <c>null</c> ohne Herleitung.</summary>
        internal double? VorlaufQuelleC { get; init; }

        /// <summary>Der feste Kaltwasser-Vorlauf am Gebäude [°C] = max(Quelle, Grenze); <c>null</c> ohne Herleitung.</summary>
        internal double? VorlaufC { get; init; }

        /// <summary>Mischt das Gebäude das Kaltwasser auf die Grenze hoch?</summary>
        internal bool Gekappt { get; init; }

        /// <summary>Die wirksame Vorlaufgrenze [°C]; <c>null</c> = keine Grenze (Gebläsekonvektor).</summary>
        internal double? VorlaufgrenzeC { get; init; }

        /// <summary>Liegt die Grenze über dem Auslegungsvorlauf (die Nennleistung wird nie erreicht)?</summary>
        internal bool GrenzeUeberAuslegung { get; init; }

        /// <summary>Warum es keine Zahl gibt — der benannte Grund; leer mit Zahl.</summary>
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

        /// <summary>
        /// Leitet die Kälteseite für <paramref name="satz"/> her (E37): Auslegungstag und Kühllast,
        /// Quelle und Höhe des Kaltwasser-Vorlaufs, Vorlaufgrenze. <c>null</c>, wenn es nichts
        /// herzuleiten gibt (keine Kühlübergabeart, kein Kühlsollwert, kein Projekt, keine
        /// Klimaregion); ein <see cref="KuehluebergabeHerleitung.Befund"/>, wenn der Eingangsbauer
        /// den Satz ablehnt. Die Heizseite bleibt dabei aus — die Herleitung der Kälteseite hängt
        /// nicht an ihr.
        /// </summary>
        internal KuehluebergabeHerleitung HerleitenKuehlung(GebaeudeModel satz)
        {
            if (satz == null || !Kuehluebergabe.ArtBekannt(satz.Kuehl_Uebergabe_Art) || !satz.Kuehl_Sollwert.HasValue) return null;
            SimulationWaermebedarf klima = Klima();
            if (klima == null) return null;

            ProjektGebaeudeModel g = AusKatalogsatz(satz);
            g.Heizkreis_Aktiv = false;
            g.Kuehlung_Aktiv = true;
            g.Kuehluebergabe_Aktiv = true;
            try
            {
                GebaeudeModellEingang e = klima.KuehluebergabeEingang(g);
                return new KuehluebergabeHerleitung
                {
                    Auslegungstag = e.AuslegungstagKuehlung,
                    AuslegungstagText = GebaeudeModellEingang.TagText(e.AuslegungstagKuehlung, CultureInfo.CurrentCulture),
                    AuslegungstagMittelC = e.AuslegungstagKuehlungMittelC,
                    AuslegungskuehllastKw = e.AuslegungskuehllastW / 1000.0,
                    Vorlaufquelle = e.KuehlVorlaufquelle,
                    VorlaufQuelleC = e.KuehlVorlaufQuelleC,
                    VorlaufC = e.KuehlVorlaufC,
                    Gekappt = e.KuehlVorlaufGekappt,
                    VorlaufgrenzeC = double.IsNaN(e.KuehlVorlaufgrenzeC) ? (double?)null : e.KuehlVorlaufgrenzeC,
                    GrenzeUeberAuslegung = e.KuehlGrenzeUeberAuslegung,
                };
            }
            catch (GebaeudeModellException ex)
            {
                return new KuehluebergabeHerleitung { Befund = ex.Message };
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
