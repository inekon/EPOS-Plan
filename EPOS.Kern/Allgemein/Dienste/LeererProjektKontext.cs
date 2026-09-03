using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Projekt"/>: kein Projekt offen.
    ///
    /// <para>Ein Konsolenlauf rechnet ein Projekt, das ihm als Nummer übergeben wurde —
    /// er hat keinen „Kontext" im Sinne einer geöffneten Maske. <c>Id = 0</c> ist der
    /// Wert, den auch die Startmaske ohne Projekt führt; alle Leser prüfen darauf.</para>
    ///
    /// <para><see cref="Uebernehmen"/> merkt sich Name und Nummer trotzdem und löst
    /// <see cref="Gewechselt"/> aus — damit lässt sich ein Prüfstand ohne Oberfläche
    /// vollständig durchspielen. Ein leerer Name mit ID 0 heißt „keins" und wird mit
    /// <c>false</c> abgelehnt, genau wie in der Oberflächenfassung.</para>
    /// </summary>
    public sealed class LeererProjektKontext : IProjektKontext
    {
        private int _id;
        private string _name = "";

        /// <inheritdoc/>
        public int Id
        {
            get { return _id; }
        }

        /// <inheritdoc/>
        public string Name
        {
            get { return _name ?? ""; }
        }

        /// <inheritdoc/>
        public string Klimazone
        {
            get { return ""; }
        }

        /// <inheritdoc/>
        public bool Uebernehmen(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name) && id <= 0) return false;

            _id = id;
            _name = name ?? "";

            Action h = Gewechselt;
            if (h != null) h();
            return true;
        }

        /// <inheritdoc/>
        public event Action Gewechselt;
    }
}
