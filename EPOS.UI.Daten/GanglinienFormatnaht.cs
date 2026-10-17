using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Wege eines Formatimports aus Sicht der Hüllen</b> (IM-4): Erkennen und Neuzerlegen einer
    /// Datei nach einer <see cref="GanglinienImportFormat"/> — hinter der gemeinsamen Fangstelle
    /// <see cref="Importfang"/>. Eine Ausnahme steht im Ausnahmeprotokoll und kommt als unlesbare
    /// Vorschau mit Grund zurück; der Optionendialog zeigt sie im Banner. Die Probelesung (die ganze
    /// Datei, das Bild) läuft schon auf dem Nebenfaden mit, nicht im Zeichenlauf.
    ///
    /// <para>Jede Hülle eines Sonderformats (Quellprofil, Spotpreis, TWW-Messreihen) reicht
    /// <see cref="Wege"/> an ihren Dialog weiter, der den Baustein <c>GanglinienFormatLauf</c> trägt.</para>
    /// </summary>
    internal static class GanglinienFormatnaht
    {
        /// <summary>Die Wege über den Kern, hinter der Fangstelle.</summary>
        internal static GanglinienFormatwege Wege() => new GanglinienFormatwege
        {
            Erkennen = Erkennen,
            Vorschau = Vorschau
        };

        private static Task<GanglinienVorschau> Erkennen(string pfad, GanglinienImportFormat format)
            => Importfang.Starten(pfad, () => MitProbe(GanglinienDatei.Erkenne(pfad, format)), Importfang.AlsVorschau);

        private static Task<GanglinienVorschau> Vorschau(string pfad, GanglinienImportOptionen optionen,
                                                         GanglinienImportFormat format)
            => Importfang.Starten(pfad, () => MitProbe(GanglinienDatei.Vorschau(pfad, optionen, format)),
                                  Importfang.AlsVorschau);

        /// <summary>Liest die vorgemerkte Probe gleich hier — auf dem Nebenfaden, hinter dem Fang.</summary>
        private static GanglinienVorschau MitProbe(GanglinienVorschau v)
        {
            if (v != null && v.Lesbar) _ = v.Probe;
            return v;
        }
    }
}
