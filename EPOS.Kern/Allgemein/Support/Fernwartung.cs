using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Fernwartung aus dem Menue (Konzept Administration und Support, Abschnitte 4.2 bis 4.4;
    /// Entwicklerhandbuch Abschnitt 4). Der Standardweg ist Quick Assist, das Hilfeprogramm von
    /// Windows; den Start uebernimmt die Windows-Schale (<c>QuickAssistStart</c>), weil er an
    /// Windows gebunden ist. Hier steht nur die Fernwartungsseite von INEKON als zweiter Weg — kein
    /// Download, keine Pruefsumme, keine eigene Schnittstelle. Ob der Weg offen ist, entscheidet
    /// <see cref="LizenzCtrl.FernwartungErlaubt()"/> (AS-Q8, Lesart b).
    /// </summary>
    public static class Fernwartung
    {
        /// <summary>Die Fernwartungsseite: Quick Assist in drei Schritten, Kontakt.</summary>
        public const string SEITE = "https://epos-plan.de/support/fernwartung";
    }

    /// <summary>
    /// Die Antwort auf „Darf dieser Arbeitsplatz die Fernwartung anbieten?".
    /// </summary>
    /// <param name="Erlaubt">Sind „Quick Assist starten" und „Fernwartungsseite oeffnen" frei?</param>
    /// <param name="Grund">Leer, wenn erlaubt; sonst der Sperrgrund in der Oberflaechensprache
    /// (<c>FW_SPERRE_TEST</c> oder <c>FW_SPERRE_LIZENZ</c>).</param>
    public sealed record Fernwartungsfreigabe(bool Erlaubt, string Grund)
    {
        /// <summary>Die freie Antwort.</summary>
        public static Fernwartungsfreigabe Frei { get; } = new Fernwartungsfreigabe(true, "");
    }
}
