using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Monatsstapel „Wärmebedarf &amp; Deckung" der Autarkie-Analyse — das
    /// Wärme-Gegenstück zum Stapel „Energie-Bedarf &amp; Deckung" der Photovoltaik.
    ///
    /// <para><b>Reihen von unten nach oben:</b> Solarthermie direkt, Solarthermie über den
    /// Speicher (nur, wenn der Lauf einen solaren Speicheranteil führt), Deckungslücke.
    /// Die Stapelhöhe ist damit der Wärmebedarf des Monats.</para>
    ///
    /// <para><b>Farben der Wärmeseite:</b> Solarthermie in ihrer Rolle
    /// <see cref="Farbrolle.WAERME_SOLAR"/>, der Speicheranteil in
    /// <see cref="Farbrolle.SPEICHERLADUNG"/> (Orange), die Lücke im Grau des
    /// ungedeckten Rests (<see cref="Farbrolle.REST"/>) — wie im Wärmering der Übersicht.
    /// Keine PV-Grüntöne.</para>
    /// </summary>
    public static class WaermeAutarkieBild
    {
        /// <summary>Die Beschriftungen; die Vorgaben sind der deutsche Rückfall.</summary>
        public sealed class Texte
        {
            public string Titel { get; set; } = "Wärmebedarf & Deckung";
            public string Direkt { get; set; } = "Solarthermie (direkt)";
            public string Speicher { get; set; } = "Solarthermie (Speicher)";
            public string Luecke { get; set; } = "Deckungslücke (Kessel/übrige Erzeuger)";
        }

        /// <summary>Das Bild als Zeichenmodell; <c>null</c> ohne Monatswerte.</summary>
        public static Zeichenmodell Modell(SolarWaermeMonate w, Texte texte = null, string einheit = "kWh")
        {
            if (w == null) return null;
            texte ??= new Texte();

            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(texte.Direkt, (double[])w.DirektKwh.Clone(), Farbrolle.WAERME_SOLAR)
            };
            if (w.HatSpeicheranteil)
                reihen.Add(new ChartRenderer.Reihe(texte.Speicher, (double[])w.SpeicherKwh.Clone(),
                                                   Farbrolle.SPEICHERLADUNG));
            reihen.Add(new ChartRenderer.Reihe(texte.Luecke, (double[])w.LueckeKwh.Clone(), Farbrolle.REST));

            return ChartRenderer.MonatsStapelModell(texte.Titel, einheit, reihen);
        }

        /// <summary>Dasselbe Bild als PNG (Prüfstand).</summary>
        public static byte[] Png(SolarWaermeMonate w, Texte texte = null, string einheit = "kWh")
        {
            Zeichenmodell z = Modell(w, texte, einheit);
            return z == null ? null : SkiaMaler.Png(z);
        }
    }
}
