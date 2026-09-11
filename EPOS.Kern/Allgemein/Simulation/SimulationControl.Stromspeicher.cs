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
            Speicherlauf = (sim, idProjekt, abbruch) =>
                sim.SpeicherlaufAusfuehren(idProjekt, abbruch);
            SpeicherflotteAktiv = SpeicherFlottenProjektCtrl.IstAktiv;
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
        internal double[] SpeicherlaufAusfuehren(int ID_Projekt,
            System.Threading.CancellationToken abbruch = default)
        {
            if (SpeicherflottenEingaben != null || SpeicherFlottenProjektCtrl.IstAktiv(ID_Projekt))
            {
                try
                {
                    SpeicherFlottenProjektLauf lauf = SpeicherflottenEingaben != null
                        ? SpeicherFlottenProjektCtrl.Rechnen(this, ID_Projekt,
                            SpeicherflottenEingaben, abbruch)
                        : SpeicherFlottenProjektCtrl.Rechnen(this, ID_Projekt, abbruch);
                    if (lauf.NetzleistungKw.Length != Rest_Strombedarf_viertelstuendlich.Length)
                        throw new InvalidOperationException(string.Format(
                            MyResource.Resource.SIMENG_SPEICHER_RASTER_ABWEICHUNG,
                            lauf.NetzleistungKw.Length, Rest_Strombedarf_viertelstuendlich.Length));

                    Speicherergebnis = lauf.Kompatibilitaetsergebnis;
                    Speicherkontext = lauf.Kontext;
                    Speicherflottenergebnis = lauf.Studie;
                    Speicherflottenkonfiguration = lauf.Konfiguration;
                    Speicherflottenlauf = lauf;
                    if (!string.IsNullOrWhiteSpace(lauf.Hinweis)) Protokoll.Hinweis(lauf.Hinweis);
                    Speicherfuellstand_viertelstuendlich = SpeicherEngine.RasterAdapter.Kopie(
                        lauf.Kompatibilitaetsergebnis.SoCKwh);
                    Speicherfuellstand_stuendlich = Viertelstunden_zu_Stundenwerte_Mittelwert(
                        Speicherfuellstand_viertelstuendlich);

                    // Der übrige Projektlauf liest Rest_Strombedarf ausschließlich als
                    // nichtnegativen Netzbezug. Der vorzeichenbehaftete Saldo darf hier
                    // deshalb nicht stehen: Einspeisung würde sonst den Jahresbezug
                    // mindern. Die vollständige Einspeisung bleibt getrennt nach PV,
                    // BHKW und Batterie in der Flottenbilanz erhalten.
                    Speicherflottennetzbilanz = StromspeicherSimCtrl.ProjektNetzbilanz(lauf.Studie);
                    if (Speicherflottennetzbilanz.NetzbezugKw.Length != Rest_Strombedarf_viertelstuendlich.Length)
                        throw new InvalidOperationException(string.Format(
                            MyResource.Resource.SIMENG_SPEICHER_RASTER_ABWEICHUNG,
                            Speicherflottennetzbilanz.NetzbezugKw.Length,
                            Rest_Strombedarf_viertelstuendlich.Length));
                    Rest_Strombedarf_viertelstuendlich =
                        (double[])Speicherflottennetzbilanz.NetzbezugKw.Clone();
                    SpeicherflotteErsetztReststrom = true;
                    return new double[Rest_Strombedarf_viertelstuendlich.Length];
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Protokoll.Warnung("Die Speicherflotte für diesen Projektlauf konnte nicht gerechnet werden: " + ex.Message);
                    throw new InvalidOperationException(
                        "Die Speicherflotte für diesen Projektlauf ist ungültig oder konnte nicht geplant werden: " + ex.Message, ex);
                }
            }

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

            double[] entladung = StromspeicherSimCtrl.EntladungLeistungKw(ergebnis);
            if (entladung.Length != Rest_Strombedarf_viertelstuendlich.Length)
            {
                Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_SPEICHER_RASTER_ABWEICHUNG,
                                                entladung.Length, Rest_Strombedarf_viertelstuendlich.Length));
                return null;
            }

            Speicherergebnis = ergebnis;
            Speicherkontext = ctrl.LetzterKontext;
            Speicherfuellstand_viertelstuendlich = SpeicherEngine.RasterAdapter.Kopie(ergebnis.SoCKwh);
            Speicherfuellstand_stuendlich = Viertelstunden_zu_Stundenwerte_Mittelwert(Speicherfuellstand_viertelstuendlich);

            return entladung;
        }
    }
}
