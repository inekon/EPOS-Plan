#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace EPOS.UI.Bausteine
{
    /// <summary>
    /// Der WIRT einer Wurzelkomponente — <c>Fehlerschranke</c> plus
    /// <typeparamref name="TInhalt"/>, mehr nicht (Befund <b>W13‑B‑1</b>,
    /// Windows-Abnahme 05.09.2026).
    ///
    /// <para><b>Wozu es ihn gibt.</b> Eine <c>ErrorBoundary</c> fängt die
    /// Ausnahmen ihrer NACHFAHREN — sie muss also ÜBER der Komponente stehen,
    /// die sie schützen soll. Eine Wurzelkomponente hat aber definitionsgemäß
    /// nichts über sich: <c>RootComponents.Add&lt;T&gt;("#app", …)</c> hängt
    /// <c>T</c> unmittelbar an den Renderer. Diese Klasse ist das fehlende
    /// Zwischenglied — die Hülle mountet <c>Wurzel&lt;T&gt;</c> statt <c>T</c>
    /// und reicht denselben Parametersatz durch.</para>
    ///
    /// <para><b>Warum der Parametersatz unverändert durchgeht.</b>
    /// <see cref="Gaben"/> fängt mit <c>CaptureUnmatchedValues</c> JEDEN
    /// Schlüssel des Wörterbuchs und legt ihn wieder auf
    /// <typeparamref name="TInhalt"/> — die Hülle ändert also keine Zeile an
    /// ihren Gaben. Und die zwei Wachen sehen weiterhin <c>T</c>:
    /// <c>Parametersatzwache.Pruefen(typeof(T), …)</c> läuft im Konstruktor der
    /// Hülle mit dem UNVERPACKTEN Typ, und <c>ParametersatzTests</c> liest den
    /// Quelltext der Hüllen, in dem weiterhin
    /// <c>new BlazorDialogForm&lt;T&gt;</c> steht.</para>
    ///
    /// <para><b>Warum kein <c>DynamicComponent</c>.</b> Der nähme den Typ als
    /// Wert entgegen und verlöre ihn damit für den Übersetzer. Hier ist
    /// <typeparamref name="TInhalt"/> ein Typparameter — <c>OpenComponent</c>
    /// kennt ihn, und ein falscher Typ fällt beim Übersetzen auf.</para>
    ///
    /// <para><b>Er zeichnet NICHTS Eigenes.</b> Kein Rahmen, keine Fläche,
    /// keine Klasse — solange nichts wirft, ist im DOM kein Unterschied zu
    /// sehen. Das ist Absicht: Jede Maske des Hauses geht seit W13‑B‑1 durch
    /// diesen Wirt, und ein Wirt, der Maße verschöbe, hätte sechzig Dialoge
    /// verschoben.</para>
    /// </summary>
    /// <typeparam name="TInhalt">Die eigentliche Wurzelkomponente aus EPOS.UI.</typeparam>
    public sealed class Wurzel<TInhalt> : ComponentBase where TInhalt : IComponent
    {
        /// <summary>
        /// Der Parametersatz der Hülle. Jeder Schlüssel, den
        /// <see cref="Wurzel{TInhalt}"/> selbst nicht führt — also jeder —,
        /// landet hier und geht unverändert an <typeparamref name="TInhalt"/>.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object>? Gaben { get; set; }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Fehlerschranke>(0);
            builder.AddComponentParameter(1, nameof(Fehlerschranke.Bezeichnung),
                                          typeof(TInhalt).Name);
            builder.AddComponentParameter(2, nameof(Fehlerschranke.ChildContent),
                                          (RenderFragment)Inhalt);
            builder.CloseComponent();
        }

        private void Inhalt(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TInhalt>(0);
            if (Gaben is not null && Gaben.Count > 0)
                builder.AddMultipleAttributes(1, (IEnumerable<KeyValuePair<string, object>>)Gaben);
            builder.CloseComponent();
        }
    }
}
