#nullable enable
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace EPOS.UI.Bausteine
{
    /// <summary>
    /// <b>Die Wurzel eines Dialogs im EIGENEN Fenster</b> (Anwenderentscheid 30.09.2026,
    /// „Kopf+Fuß fest"): <see cref="Wurzel{TInhalt}"/> mit Fehlerschranke und Dialog, dahinter
    /// die <see cref="Fenstermarke"/> — beide in EINER Wurzelkomponente an <c>#app</c>.
    ///
    /// <para><b>Warum keine zweite Wurzelkomponente.</b> Die Marke hing zuerst als eigene
    /// Wurzel an <c>body::after</c>. Diesen Selektor belegt der WinForms-BlazorWebView selbst,
    /// sobald die Entwicklerwerkzeuge laufen (Nachladen der Stilblätter,
    /// <c>static-content-hot-reload.js</c>); <c>WebViewManager.AddRootComponentAsync</c> warf
    /// „There is already a root component with selector 'body::after'" in der Fensterprozedur
    /// des neuen Fensters, und der Prozess endete wortlos (<c>0xc000041d</c>). Eine Wurzel,
    /// ein Selektor, den nur die Hülle vergibt: <c>#app</c>.</para>
    ///
    /// <para><b>DOM.</b> <c>#app &gt; .epos-dialog</c> bleibt, wie es war (die Fehlerschranke
    /// zeichnet keine Hülle), die Marke steht als Geschwister dahinter:
    /// <c>#app &gt; span.epos-fenstermarke</c>. Das Hausblatt fragt
    /// <c>#app:has(&gt; .epos-fenstermarke)</c>.</para>
    ///
    /// <para><b>Der Parametersatz</b> geht unverändert durch (<see cref="Gaben"/> fängt ihn wie
    /// bei <see cref="Wurzel{TInhalt}"/> mit <c>CaptureUnmatchedValues</c>); geprüft wird er in
    /// der Hülle weiterhin gegen <typeparamref name="TInhalt"/>.</para>
    /// </summary>
    public sealed class Fensterwurzel<TInhalt> : ComponentBase where TInhalt : IComponent
    {
        /// <summary>Der Parametersatz des Dialogs — unverändert weitergereicht.</summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object>? Gaben { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Wurzel<TInhalt>>(0);
            if (Gaben is not null && Gaben.Count > 0)
                builder.AddMultipleAttributes(1, (IEnumerable<KeyValuePair<string, object>>)Gaben);
            builder.CloseComponent();

            builder.OpenComponent<Fenstermarke>(2);
            builder.CloseComponent();
        }
    }
}
