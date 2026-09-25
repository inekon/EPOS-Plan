using System;
using System.Text;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Der Rechennachweis des Zapfprofilgenerators gegen echte Messreihen</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Kapitel 7 Stufe Z5, offener Punkt K5).
    ///
    /// <para>Die Reihen liegen beim Anwender und kommen nie ins Repositorium; hierher kommt nur der
    /// Bericht, und der trägt nur Verhältniszahlen. Alles Fachliche rechnet der Kern
    /// (<c>ZapfprofilRechner</c>, <c>Messvergleich</c>, <c>Messkalibrierung</c>,
    /// <c>Messreihenleser</c>); das Werkzeug bringt Pfade, Dateien und Konsole mit — das, was dem
    /// Kern verboten ist.</para>
    ///
    /// <para>Der Ablauf steht in <see cref="Einstieg"/>, damit die Proben ihn ohne Prozessgrenze
    /// fahren können. <c>Main</c> setzt nur die Ausgabekodierung und fängt das Unerwartete.</para>
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Die Ausgabe ist UTF-8, auf jeder Plattform: Die Berichtszeilen fuehren Umlaute und den
            // Geviertstrich. Gekapselt wie in Auslieferungsvorlage und Referenzlauf - eine Umgebung
            // ohne setzbare Konsolen-Codepage soll das Werkzeug nicht zu Fall bringen.
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }
            try
            {
                return Einstieg.Lauf(args, Console.Out, Console.Error);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unerwartet: " + ex.GetType().Name + ": " + ex.Message);
                return Einstieg.UNERWARTET;
            }
        }
    }
}
