using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace KiKern
{
    /// <summary>Ein vom Modell vorgeschlagener Werkzeugaufruf - noch UNGEPRUEFT.</summary>
    /// <remarks>
    /// Bewusst roh: Name und Argumenttext, wie sie hereinkamen. Aus ihnen wird ein
    /// <see cref="KiAufruf"/> erst in <see cref="KiAbsicht"/> - ueber
    /// <see cref="KiPruefung"/> und damit gegen die Deklaration.
    /// </remarks>
    public sealed class KiWerkzeugruf
    {
        internal KiWerkzeugruf(string name, string argumenteJson)
        {
            Name = name ?? "";
            ArgumenteJson = string.IsNullOrWhiteSpace(argumenteJson) ? "{}" : argumenteJson;
        }

        /// <summary>Name, den das Modell genannt hat - kann unbekannt oder erfunden sein.</summary>
        public string Name { get; }

        /// <summary>Argumente als JSON-Objekttext; „{}", wenn keine kamen.</summary>
        public string ArgumenteJson { get; }

        /// <inheritdoc/>
        public override string ToString() => Name + " " + ArgumenteJson;
    }

    /// <summary>
    /// Die zerlegte Antwort des Modells auf <c>generateContent</c> (Fachkonzept 3.3, Weg A).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reine Funktion: Text hinein, Struktur heraus. Kein Netz, kein Zustand - deshalb in
    /// <see cref="KiKern"/> und damit pruefbar, ohne dass ein Modell befragt werden muesste
    /// (Fachkonzept 8, Etappe 2: die Modellanbindung wird NICHT automatisiert getestet).
    /// </para>
    /// <para>
    /// <see cref="InhaltJson"/> traegt den Inhaltsblock des ersten Kandidaten unveraendert.
    /// Er muss vor dem Ergebnis wieder in den Verlauf geschrieben werden, sonst weiss das
    /// Modell in der naechsten Runde nicht, worauf sich die <c>functionResponse</c> bezieht.
    /// </para>
    /// </remarks>
    public sealed class KiModellantwort
    {
        private KiModellantwort(string text, IReadOnlyList<KiWerkzeugruf> rufe,
                                string abschlussgrund, string inhaltJson)
        {
            Text = text ?? "";
            Werkzeugrufe = rufe ?? Array.Empty<KiWerkzeugruf>();
            Abschlussgrund = abschlussgrund ?? "";
            InhaltJson = inhaltJson ?? "";
        }

        /// <summary>Alle Textteile der Antwort, zusammengefasst und getrimmt.</summary>
        public string Text { get; }

        /// <summary>Die Werkzeugaufrufe der Antwort, in der Reihenfolge der Teile.</summary>
        public IReadOnlyList<KiWerkzeugruf> Werkzeugrufe { get; }

        /// <summary>Feld <c>finishReason</c> des Kandidaten; leer, wenn keines kam.</summary>
        public string Abschlussgrund { get; }

        /// <summary>Der Inhaltsblock <c>candidates[0].content</c> im Rohzustand.</summary>
        public string InhaltJson { get; }

        /// <summary>Hat das Modell eine Aktion gerufen?</summary>
        public bool HatWerkzeugruf => Werkzeugrufe.Count > 0;

        /// <summary>
        /// Der Anbieter hat einen missglueckten Werkzeugaufruf gemeldet
        /// (<c>MALFORMED_FUNCTION_CALL</c>) - ein Fall fuer die Korrekturrunde.
        /// </summary>
        public bool WerkzeugrufMissglueckt
            => string.Equals(Abschlussgrund, "MALFORMED_FUNCTION_CALL", StringComparison.OrdinalIgnoreCase);

        /// <summary>Eine leere Antwort - fuer Fehlerpfade und Tests.</summary>
        public static KiModellantwort Leer()
            => new KiModellantwort("", Array.Empty<KiWerkzeugruf>(), "", "");

        /// <summary>
        /// Zerlegt den Antwortrumpf. Ist er kein JSON oder fehlt der Kandidat, entsteht eine
        /// leere Antwort - eine Ausnahme waere hier falsch, weil der Aufrufer daraufhin nur
        /// eine Klartextmeldung zeigen kann.
        /// </summary>
        public static KiModellantwort Lesen(string? rumpf)
        {
            if (string.IsNullOrWhiteSpace(rumpf)) return Leer();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(rumpf!);
                JsonElement wurzel = doc.RootElement;

                if (wurzel.ValueKind != JsonValueKind.Object) return Leer();
                if (!wurzel.TryGetProperty("candidates", out JsonElement kandidaten)
                    || kandidaten.ValueKind != JsonValueKind.Array
                    || kandidaten.GetArrayLength() == 0)
                    return Leer();

                JsonElement erster = kandidaten[0];

                string grund = erster.TryGetProperty("finishReason", out JsonElement fr)
                               && fr.ValueKind == JsonValueKind.String
                    ? fr.GetString() ?? "" : "";

                if (!erster.TryGetProperty("content", out JsonElement inhalt))
                    return new KiModellantwort("", Array.Empty<KiWerkzeugruf>(), grund, "");

                string inhaltJson = inhalt.GetRawText();

                var text = new StringBuilder();
                var rufe = new List<KiWerkzeugruf>();

                if (inhalt.TryGetProperty("parts", out JsonElement teile)
                    && teile.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement teil in teile.EnumerateArray())
                    {
                        if (teil.TryGetProperty("text", out JsonElement t)
                            && t.ValueKind == JsonValueKind.String)
                        {
                            string s = t.GetString() ?? "";
                            if (s.Length > 0)
                            {
                                if (text.Length > 0) text.Append('\n');
                                text.Append(s);
                            }
                        }

                        if (teil.TryGetProperty("functionCall", out JsonElement fc)
                            && fc.ValueKind == JsonValueKind.Object)
                        {
                            string name = fc.TryGetProperty("name", out JsonElement n)
                                          && n.ValueKind == JsonValueKind.String
                                ? n.GetString() ?? "" : "";
                            string args = fc.TryGetProperty("args", out JsonElement a)
                                          && a.ValueKind == JsonValueKind.Object
                                ? a.GetRawText() : "{}";
                            if (name.Length > 0) rufe.Add(new KiWerkzeugruf(name, args));
                        }
                    }
                }

                return new KiModellantwort(text.ToString().Trim(), rufe, grund, inhaltJson);
            }
            catch (JsonException)
            {
                return Leer();
            }
        }
    }
}
