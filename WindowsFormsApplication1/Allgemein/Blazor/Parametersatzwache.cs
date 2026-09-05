using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WACHE ueber einen Parametersatz — die Verallgemeinerung von
    /// <c>BlazorSeite.ZustandParameterPruefen</c> (Befund W16c‑B12) auf
    /// <b>jeden</b> Schluessel des Woerterbuchs.
    ///
    /// <para><b>Warum es sie braucht.</b> Eine Huelle reicht ihre Gaben als
    /// <c>IDictionary&lt;string, object&gt;</c> an <c>RootComponents.Add&lt;T&gt;</c>.
    /// Das Woerterbuch kennt keine Typen: Ein Schluessel, den die Komponente
    /// nicht als <c>[Parameter]</c> fuehrt, faellt beim Uebersetzen nicht auf.
    /// Blazor bemerkt ihn erst beim ERSTEN Zeichnen und wirft dort eine
    /// <c>InvalidOperationException</c> — im Verteiler, also fuer den Anwender
    /// als <c>TargetInvocationException</c> an <c>Application.Run</c> oder,
    /// schlimmer, als stille leere Flaeche. Genau das war W16c‑B12 (der
    /// Startabsturz, weil <c>Hauptfenster</c> den Parameter <c>Zustand</c>
    /// nicht kannte).</para>
    ///
    /// <para><b>Sie aendert nichts am Verhalten</b> — sie sagt dasselbe Nein
    /// frueher und mit Namen: gleich beim Bauen der Huelle, mit Huelle,
    /// Komponente und dem SCHLUESSEL im Wortlaut, statt tief im Verteiler.</para>
    ///
    /// <para><b>Ausnahme.</b> Eine Komponente mit
    /// <c>[Parameter(CaptureUnmatchedValues = true)]</c> nimmt jeden Namen
    /// entgegen; fuer sie prueft die Wache nichts.</para>
    ///
    /// <para><b>Die zweite Wache steht auf Linux:</b>
    /// <c>EPOS.UI.Tests/ParametersatzTests</c> haelt dieselbe Regel statisch
    /// gegen alle <c>Views/**/*Huelle.cs</c> — die hier greift am Geraet, jene
    /// im Gate.</para>
    /// </summary>
    internal static class Parametersatzwache
    {
        /// <summary>
        /// Prueft, ob jeder Schluessel eine <c>[Parameter]</c>-Eigenschaft von
        /// <paramref name="komponente"/> trifft.
        /// </summary>
        /// <param name="komponente">Die Razor-Komponente, die den Satz bekommt.</param>
        /// <param name="parameter">Der Parametersatz; <c>null</c> ist erlaubt.</param>
        /// <param name="huelle">Name der Huelle fuer die Meldung.</param>
        /// <exception cref="InvalidOperationException">
        /// Wenn mindestens ein Schluessel keinen Parameter trifft.
        /// </exception>
        internal static void Pruefen(Type komponente, IDictionary<string, object> parameter,
                                     string huelle)
        {
            if (komponente == null || parameter == null || parameter.Count == 0) return;

            PropertyInfo[] eigenschaften = komponente.GetProperties(
                BindingFlags.Public | BindingFlags.Instance);

            // Ein Sammelparameter nimmt jeden Namen - dann gibt es nichts zu pruefen.
            foreach (PropertyInfo p in eigenschaften)
            {
                ParameterAttribute merkmal =
                    (ParameterAttribute)Attribute.GetCustomAttribute(p, typeof(ParameterAttribute));
                if (merkmal != null && merkmal.CaptureUnmatchedValues) return;
            }

            HashSet<string> bekannt = new HashSet<string>(StringComparer.Ordinal);
            foreach (PropertyInfo p in eigenschaften)
            {
                if (!p.CanWrite) continue;
                if (!p.IsDefined(typeof(ParameterAttribute), true)) continue;
                bekannt.Add(p.Name);
            }

            List<string> fremd = parameter.Keys
                                          .Where(k => !bekannt.Contains(k))
                                          .OrderBy(k => k, StringComparer.Ordinal)
                                          .ToList();
            if (fremd.Count == 0) return;

            throw new InvalidOperationException(
                huelle + ": Der Parametersatz nennt " + fremd.Count +
                (fremd.Count == 1 ? " Schluessel, den" : " Schluessel, die") +
                " die Komponente " + komponente.FullName + " nicht als [Parameter] fuehrt: " +
                string.Join(", ", fremd) + ". " +
                "Entweder heisst die Eigenschaft anders, oder ihr fehlt das Merkmal " +
                "[Parameter] - Blazor wuerde den Satz beim ersten Zeichnen zurueckweisen.");
        }
    }
}
