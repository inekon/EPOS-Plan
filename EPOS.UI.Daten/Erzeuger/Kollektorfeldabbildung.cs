using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Felder des Solarkreises zwischen Anlagenmodell und Dialogzeile</b> (Welle M2 der
    /// Entscheidungsvorlage Modellgrenzen: ST1 bis ST4) — plattformfrei, damit jede Hülle des
    /// <c>SolarkollektorenDialog</c> (Windows-Projektdialog, Assistent) dieselbe Abbildung nimmt.
    /// </summary>
    /// <remarks>
    /// <para><b>Leer bleibt leer:</b> <c>null</c> heißt an beiden Enden „es gilt die Vorgabe"
    /// (8 % Verluste, 5 K Grädigkeit, 10 K Spreizung, kein Pumpenstrom). Eine gepflegte 0 bleibt
    /// eine 0.</para>
    /// <para><b>Die Arbeitstemperatur ist ein Ja/Nein der Maske</b> und in der Ablage der
    /// Persistenzwert <see cref="DbWerte.SOLAR_ARBEITSTEMPERATUR_SPEICHER"/>; „fest" wird als
    /// <see cref="DbWerte.SOLAR_ARBEITSTEMPERATUR_FEST"/> nur geschrieben, wenn die Zeile vorher
    /// einen Wert trug — eine nie gepflegte Zeile bleibt NULL.</para>
    /// </remarks>
    public static class Kollektorfeldabbildung
    {
        /// <summary>Anlagenmodell → Dialogzeile.</summary>
        public static void InZeile(WErzeugerModel m, ErzeugerZeile z)
        {
            if (m == null || z == null) return;
            z.SolarPumpenleistungW = m.Pumpenleistung_W;
            z.SolarkreisverlusteProzent = m.Solarkreisverluste_Prozent;
            z.SolarGraedigkeitK = m.Uebertrager_Graedigkeit_K;
            z.SolarSpreizungK = m.Kollektor_Spreizung_K;
            z.SolarArbeitstemperaturAusSpeicher = Solarkreis.ArbeitstemperaturAusSpeicher(m.Arbeitstemperatur_Weg);
        }

        /// <summary>Dialogzeile → Anlagenmodell (der Weg von „Übernehmen").</summary>
        public static void InModell(ErzeugerZeile z, WErzeugerModel m)
        {
            if (m == null || z == null) return;
            m.Pumpenleistung_W = z.SolarPumpenleistungW;
            m.Solarkreisverluste_Prozent = z.SolarkreisverlusteProzent;
            m.Uebertrager_Graedigkeit_K = z.SolarGraedigkeitK;
            m.Kollektor_Spreizung_K = z.SolarSpreizungK;
            m.Arbeitstemperatur_Weg = Weg(z.SolarArbeitstemperaturAusSpeicher, m.Arbeitstemperatur_Weg);
        }

        /// <summary>Der Persistenzwert des Wegs: „speicher", „fest" oder — nie gepflegt und fest — NULL.</summary>
        public static string Weg(bool ausSpeicher, string bisher)
        {
            if (ausSpeicher) return DbWerte.SOLAR_ARBEITSTEMPERATUR_SPEICHER;
            return string.IsNullOrEmpty(bisher) ? null : DbWerte.SOLAR_ARBEITSTEMPERATUR_FEST;
        }
    }
}
