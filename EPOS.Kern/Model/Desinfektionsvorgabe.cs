using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die thermische Desinfektion des Projekts</b> (Welle M7, BW5; Schemaschritt
    /// <see cref="PufferOptionenSchema"/>) — unveränderlich: Schalter, Intervall [Tage], Stunde des Tages,
    /// Zieltemperatur [°C] und aufgeheiztes Volumen [l]. Gelesen je Lauf
    /// (<see cref="KonfigurationCtrl.DesinfektionLesen"/>), geschrieben in einem <c>UPDATE</c>
    /// (<see cref="KonfigurationCtrl.DesinfektionSchreiben"/>).
    ///
    /// <para><b>Leer heißt aus.</b> Ein leeres Feld trägt seine Vorgabe (7 Tage, 2 Uhr, 70 °C, Volumen der
    /// Brauchwasserspeicher); ein Wert außerhalb der Prüfklauseln gilt als leer.</para>
    /// </summary>
    public sealed record Desinfektionsvorgabe
    {
        /// <summary>Keine Desinfektion — so liest sich eine fehlende Zeile oder Spalte.</summary>
        public static readonly Desinfektionsvorgabe Aus = new Desinfektionsvorgabe(false, null, null, null, null);

        /// <summary>Vorgabe des Intervalls [Tage].</summary>
        public const int INTERVALL_VORGABE = 7;

        /// <summary>Vorgabe der Stunde des Tages.</summary>
        public const int STUNDE_VORGABE = 2;

        /// <summary>Vorgabe der Zieltemperatur [°C].</summary>
        public const double ZIEL_VORGABE_C = 70;

        /// <summary>Legt eine Vorgabe an und normalisiert sie (Werte außerhalb der Grenzen gelten als leer).</summary>
        public Desinfektionsvorgabe(bool aktiv, int? intervallTage, int? stunde, double? zielC, double? volumenL)
        {
            Aktiv = aktiv;
            IntervallTage = intervallTage.HasValue && intervallTage.Value >= PufferOptionenSchema.INTERVALL_MIN &&
                            intervallTage.Value <= PufferOptionenSchema.INTERVALL_MAX ? intervallTage : null;
            Stunde = stunde.HasValue && stunde.Value >= PufferOptionenSchema.STUNDE_MIN &&
                     stunde.Value <= PufferOptionenSchema.STUNDE_MAX ? stunde : null;
            ZielC = Endlich(zielC) && zielC.Value >= PufferOptionenSchema.ZIEL_MIN &&
                    zielC.Value <= PufferOptionenSchema.ZIEL_MAX ? zielC : null;
            VolumenL = Endlich(volumenL) && volumenL.Value >= 0 && volumenL.Value <= PufferOptionenSchema.VOLUMEN_MAX
                ? volumenL : null;
        }

        private static bool Endlich(double? w) => w.HasValue && !double.IsNaN(w.Value) && !double.IsInfinity(w.Value);

        /// <summary>Läuft die Desinfektion?</summary>
        public bool Aktiv { get; init; }

        /// <summary>Intervall [Tage]; <c>null</c> = 7.</summary>
        public int? IntervallTage { get; init; }

        /// <summary>Stunde des Tages 0 … 23; <c>null</c> = 2.</summary>
        public int? Stunde { get; init; }

        /// <summary>Zieltemperatur [°C]; <c>null</c> = 70.</summary>
        public double? ZielC { get; init; }

        /// <summary>Aufgeheiztes Volumen [l]; <c>null</c> = Volumen der Brauchwasserspeicher.</summary>
        public double? VolumenL { get; init; }

        /// <summary>Das Intervall, wie es wirkt.</summary>
        public int IntervallWirksam => IntervallTage ?? INTERVALL_VORGABE;

        /// <summary>Die Stunde, wie sie wirkt.</summary>
        public int StundeWirksam => Stunde ?? STUNDE_VORGABE;

        /// <summary>Die Zieltemperatur, wie sie wirkt.</summary>
        public double ZielWirksamC => ZielC ?? ZIEL_VORGABE_C;

        /// <summary>
        /// Prüft eine Eingabe gegen die Grenzen der Spalten, BEVOR sie normalisiert wird; <c>null</c> = in
        /// Ordnung, sonst die benannte Ablehnung.
        /// </summary>
        public static string Pruefen(int? intervallTage, int? stunde, double? zielC, double? volumenL)
        {
            if (intervallTage.HasValue && (intervallTage < PufferOptionenSchema.INTERVALL_MIN ||
                                           intervallTage > PufferOptionenSchema.INTERVALL_MAX))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.DESINF_PRUEF_INTERVALL,
                                     PufferOptionenSchema.INTERVALL_MIN, PufferOptionenSchema.INTERVALL_MAX);
            if (stunde.HasValue && (stunde < PufferOptionenSchema.STUNDE_MIN || stunde > PufferOptionenSchema.STUNDE_MAX))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.DESINF_PRUEF_STUNDE,
                                     PufferOptionenSchema.STUNDE_MIN, PufferOptionenSchema.STUNDE_MAX);
            if (zielC.HasValue && (!Endlich(zielC) || zielC < PufferOptionenSchema.ZIEL_MIN || zielC > PufferOptionenSchema.ZIEL_MAX))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.DESINF_PRUEF_ZIEL,
                                     PufferOptionenSchema.ZIEL_MIN, PufferOptionenSchema.ZIEL_MAX);
            if (volumenL.HasValue && (!Endlich(volumenL) || volumenL < 0 || volumenL > PufferOptionenSchema.VOLUMEN_MAX))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.DESINF_PRUEF_VOLUMEN,
                                     PufferOptionenSchema.VOLUMEN_MAX);
            return null;
        }
    }
}
