// Die Windows-Fassung der Ausfuehrungsschicht (iU9-W15b.0a).
//
// Sie reicht jedes Glied UNVERAENDERT an KiAusfuehrer bzw. KiHilfe weiter - es gibt
// hier keine zweite Fachlogik und keine zweite Reihenfolge. Der Adapter existiert
// allein, weil KiChatService seit W15b.0a im Kern liegt und der Kern KiAusfuehrer
// nicht kennen darf: Der Ausfuehrer haengt an Control (UI-Faden), an
// Application.OpenForms und an Form.ActiveForm.Modal.
//
// Eingelegt wird er in Program.Main, direkt neben den uebrigen Dienstfassungen.
// Wird er NICHT eingelegt, antwortet die stille Fassung KeineAusfuehrung mit einem
// leeren Register und einer Ablehnung - dieselbe Zusage, die der Aktionsharnisch
// von einem fehlenden Bestaetigungsweg kennt.

using System.Threading;
using System.Threading.Tasks;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <see cref="IKiAusfuehrung"/> ueber <see cref="KiAusfuehrer"/> - die Fassung
    /// der Windows-Anwendung.
    /// </summary>
    internal sealed class KiAusfuehrungAdapter : IKiAusfuehrung
    {
        /// <inheritdoc/>
        public KiRegister Register => KiAusfuehrer.Register;

        /// <inheritdoc/>
        public string LetzteProtokollzeile => KiAusfuehrer.LetzteProtokollzeile;

        /// <inheritdoc/>
        public Task<KiVorbereitung> VorbereitenAsync(KiAufruf aufruf, CancellationToken abbruch)
            => KiAusfuehrer.VorbereitenAsync(aufruf, abbruch);

        /// <inheritdoc/>
        public Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, CancellationToken abbruch)
            => KiAusfuehrer.AusfuehrenAsync(aufruf, abbruch);

        /// <inheritdoc/>
        public Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, KiFreigabe freigabe,
                                                CancellationToken abbruch)
            => KiAusfuehrer.AusfuehrenAsync(aufruf, freigabe, abbruch);

        /// <inheritdoc/>
        public KiErgebnis AbweisenUndVermerken(KiAufruf aufruf, string grund)
            => KiAusfuehrer.AbweisenUndVermerken(aufruf, grund);

        /// <inheritdoc/>
        public void KlarnamenAnmelden(KiPlatzhalter platzhalter, params string[] texte)
            => KiHilfe.KlarnamenAnmelden(platzhalter, texte);
    }
}
