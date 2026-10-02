using EPOS.UI.Dialoge.Waermepumpe;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorbelegung von Vor- und Rücklauf für die Projektdialoge</b> (Anwenderauftrag
    /// 30.09.2026: „Dialogfelder Vorlauf und Rücklauftemperatur übersichtlicher und
    /// Vorbelegung, falls 0, mit sinnvollen Vorgaben"). Die REGEL steht im Kern
    /// (<see cref="AnlagenTemperaturen"/>); hier steht nur, wie ihr Ergebnis in das Modell
    /// bzw. den Feldsatz eines Dialogs kommt — plattformfrei, damit die Windows-Hüllen sie
    /// rufen und die Tests sie prüfen können.
    /// </summary>
    public static class TemperaturVorbelegung
    {
        /// <summary>
        /// <b>Heizkessel</b>: Ist das Paar der Zeile unvollständig, setzt der Kern das Paar
        /// ein, mit dem die Simulation ohne Eintrag rechnet
        /// (<see cref="AnlagenTemperaturen.KesselPaarVorbelegen"/>) — IN DAS MODELL, damit es
        /// mit OK gespeichert wird; ein Abbruch verwirft die Liste des Aufrufers ohnehin.
        /// </summary>
        /// <param name="m">Die Anlagenzeile.</param>
        /// <param name="wizard">
        /// Assistentenbetrieb. Nur dort, und nur bei einer frisch aufgenommenen Zeile
        /// (vorläufige Id ab <see cref="WizardItemClass.ID_UNGESPEICHERT_START"/>), zeigt
        /// <c>ID_Kessel</c> noch auf den KATALOGSATZ; die Projektkopie entsteht dort erst beim
        /// Speichern (<c>WizardCtrl</c>). Jede andere Zeile zeigt auf ihre Projektkopie in
        /// <c>Tab_Heizkessel</c> — denselben Satz, den die Simulation liest.
        /// </param>
        /// <returns>Die fertige Herleitungszeile; leer, wenn nichts vorbelegt wurde.</returns>
        public static string Kessel(WErzeugerModel m, bool wizard)
        {
            if (m == null) return "";
            bool stammverweis = wizard && m.ID >= WizardItemClass.ID_UNGESPEICHERT_START;
            return AnlagenTemperaturen.Herleitung(AnlagenTemperaturen.KesselPaarVorbelegen(m, stammverweis));
        }

        /// <summary>
        /// <b>Wärmepumpe</b>: Steht der Rücklauf auf 0 oder leer, setzt der Kern Vorlauf −
        /// Rückfall-Spreizung ein (<see cref="AnlagenTemperaturen.WaermepumpeRuecklaufVorgabe"/>)
        /// und die Zeile, woher er stammt — im FELDSATZ; ins Modell kommt er mit dem OK. Ohne
        /// Vorlauf bleibt der Rücklauf, wie er ist; dann meldet der OK-Knopf.
        /// </summary>
        /// <returns><c>true</c>, wenn vorbelegt wurde.</returns>
        public static bool Waermepumpe(WaermepumpeAnlageDaten d)
        {
            if (d == null) return false;
            int? ruecklauf = AnlagenTemperaturen.WaermepumpeRuecklaufVorgabe(d.Vorlauf, d.Ruecklauf);
            if (!ruecklauf.HasValue || !d.Vorlauf.HasValue) return false;

            d.Ruecklauf = ruecklauf;
            d.RuecklaufHerleitung = AnlagenTemperaturen.WaermepumpeRuecklaufHerleitung(d.Vorlauf.Value, ruecklauf.Value);
            return true;
        }
    }
}
