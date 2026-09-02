using System;
using System.Runtime.CompilerServices;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der STROMSPEICHERZWEIG von <see cref="SimulationControl"/> (Umsetzungskonzept
    /// iU3, Kante K8).
    ///
    /// <para>Die Methode steht hier <b>unverändert</b> — samt ihrer Begründung. Neu ist
    /// nur, dass der Kernteil sie über den Haken
    /// <see cref="SimulationControl.Speicherlauf"/> erreicht statt direkt, und dass
    /// dieser Haken beim Laden der Assembly gesetzt wird
    /// (<see cref="HakenSetzen"/>). Ohne diese Datei — im Rechenkern — bleibt der Haken
    /// leer, und die Kette rechnet ohne Speicherwirkung; genau der Fehlerfall, den die
    /// Methode ohnehin kennt.</para>
    /// </summary>
    partial class SimulationControl
    {
        /// <summary>
        /// Hängt den Speicherzweig ein, sobald die Assembly geladen ist — vor jedem
        /// Aufruf und ohne Zutun eines Aufrufers.
        /// </summary>
        [ModuleInitializer]
        internal static void HakenSetzen()
        {
            Speicherlauf = (sim, idProjekt) => sim.SpeicherlaufAusfuehren(idProjekt);
            SimulationRunner.Speicherergebnismodell = StromspeicherSimCtrl.AlsErgebnismodell;
        }

        /// <summary>
        /// Rechnet die aktive Speichervariante über die <c>SpeicherEngine</c> und
        /// liefert die ENTLADUNG je Viertelstunde als Leistung [kW] — oder
        /// <c>null</c>, wenn nicht gerechnet wurde.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Ersetzt den wirkungslosen <c>SimulationSSP</c>-Stub (AP2b, Fachkonzept 8.2,
        /// Rudiment 1). Gerechnet wird die Anlagenzeile der <b>aktiven Speichervariante</b>
        /// (AP9b, Fachkonzept 7.3) mit der Berechnungsart, die diese Variante vorgibt;
        /// nur ohne bestimmbare aktive Variante fällt der Lauf auf die Aggregation über
        /// alle <c>SP_TYP</c>-Anlagen zurück (Protokollhinweis). Die Reihen- und
        /// Parameterbeschaffung liegt vollständig in <see cref="StromspeicherSimCtrl"/>,
        /// die Formeln in der Engine.
        /// </para>
        /// <para>
        /// <b>Der Speicher darf den Lauf nicht kippen.</b> Jeder Fehler — fehlende
        /// Stammdaten, Rasterabweichung, Ausnahme aus der Engine — landet als Hinweis
        /// bzw. Warnung im Protokoll; die Kette rechnet dann ohne Speicherwirkung
        /// weiter, genau wie vor diesem Paket. Der Datenzugriff liegt im
        /// dialogfreien Modus (der ganze Lauf steht in
        /// <see cref="DataRepository.EngineModus"/>, Verschachtelung ist zulässig).
        /// </para>
        /// </remarks>
        private float[] SpeicherlaufAusfuehren(int ID_Projekt)
        {
            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            SpeicherEngine.SpeicherErgebnis ergebnis;

            try
            {
                ergebnis = ctrl.RechneAktiveVariante(this, ID_Projekt);
            }
            catch (Exception ex)
            {
                Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_SPEICHER_FEHLGESCHLAGEN, ex.Message));
                return null;
            }

            // Hinweise des Controllers (kein Speicher, keine Kapazität, 1-C-Rückfall)
            // gehören in jedem Fall ins Protokoll - auch wenn gerechnet wurde.
            if (!string.IsNullOrEmpty(ctrl.LetzterHinweis)) Protokoll.Hinweis(ctrl.LetzterHinweis);

            if (ergebnis == null) return null;

            float[] entladung = StromspeicherSimCtrl.EntladungLeistungKw(ergebnis);
            if (entladung.Length != Rest_Strombedarf_viertelstuendlich.Length)
            {
                Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_SPEICHER_RASTER_ABWEICHUNG,
                                                entladung.Length, Rest_Strombedarf_viertelstuendlich.Length));
                return null;
            }

            Speicherergebnis = ergebnis;
            Speicherkontext = ctrl.LetzterKontext;
            Speicherfuellstand_viertelstuendlich = SpeicherEngine.RasterAdapter.ZuFloat(ergebnis.SoCKwh);
            Speicherfuellstand_stuendlich = Viertelstunden_zu_Stundenwerte_Mittelwert(Speicherfuellstand_viertelstuendlich);

            return entladung;
        }
    }
}
