// Der Weg vom Chatdienst zur Ausfuehrungsschicht (iU9-W15b.0a).
//
// WARUM ES DIESE DATEI GIBT. Mit W15b.0a ist KiChatService in den Kern gezogen
// (Befund W15b-B1: kein WinForms, kein Program., keine Registry, kein DPAPI).
// Die Vermessung hat dabei EINE Abhaengigkeit uebersehen, die der Uebersetzer
// sofort gemeldet hat (Befund W15b-B31): Der Dienst ruft in der Werkzeugrunde
// zehnmal KiAusfuehrer und einmal KiHilfe.KlarnamenAnmelden - beides liegt in
// der Windows-Anwendung und bleibt dort (KiAusfuehrer haengt an Control,
// Application.OpenForms und Form.ActiveForm.Modal; KiHilfe liest die Datenbank).
//
// Statt den Umzug zurueckzunehmen, bekommt der Kern hier dieselbe Bauart wie
// Dienste.* (Paket iU5): eine SCHNITTSTELLE mit einer stillen Standardfassung.
// Die Windows-Huelle legt beim Start ihre Fassung ein (Program.Main ->
// KiAusfuehrungAdapter), die iOS-Huelle spaeter ihre eigene; ohne Huelle - im
// Pruefstand, im Referenzlauf, in der Konsolenfassung - antwortet die stille
// Fassung mit einem leeren Register und einer protokollierten Ablehnung.
//
// DER RIEGEL BLEIBT UNBERUEHRT (Regel S-4). Diese Schnittstelle sitzt HINTER
// KiEinwilligung und hinter KiRiegel: Ohne Einwilligung entsteht gar keine
// Werkzeugrunde, in der sie gerufen wuerde. Sie ist kein zweiter Weg an der
// Schutzkette vorbei, sondern nur die Naht, an der Kern und Huelle sich treffen.

using System;
using System.Threading;
using System.Threading.Tasks;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was der Chatdienst von der Ausfuehrungsschicht braucht - und mehr nicht.
    /// </summary>
    /// <remarks>
    /// Die Windows-Fassung ist <c>KiAusfuehrungAdapter</c>; sie reicht jedes Glied
    /// unveraendert an <c>KiAusfuehrer</c> bzw. <c>KiHilfe</c> weiter. Wer eine
    /// zweite Fassung baut, muss die Reihenfolge einhalten, die
    /// <see cref="VorbereitenAsync"/> und
    /// <see cref="AusfuehrenAsync(KiAufruf, KiFreigabe, CancellationToken)"/>
    /// beschreiben: erst vorbereiten (Vorschau und Sicherungspunkt), dann die
    /// Entscheidung des Anwenders abwarten, dann ausfuehren.
    /// </remarks>
    public interface IKiAusfuehrung
    {
        /// <summary>Das gefuellte Aktionsregister.</summary>
        KiRegister Register { get; }

        /// <summary>Die Protokollzeile des zuletzt versuchten Laufs; leer, wenn keiner lief.</summary>
        string LetzteProtokollzeile { get; }

        /// <summary>Prueft Vorbedingung, Lizenz und Sicherungspunkt und erzeugt die Vorschau.</summary>
        Task<KiVorbereitung> VorbereitenAsync(KiAufruf aufruf, CancellationToken abbruch);

        /// <summary>Fuehrt eine Aktion OHNE Bestaetigungspflicht aus.</summary>
        Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, CancellationToken abbruch);

        /// <summary>Loest eine erteilte, abgelehnte oder verfallene Freigabe ein.</summary>
        Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, KiFreigabe freigabe, CancellationToken abbruch);

        /// <summary>Weist einen Aufruf ab und schreibt die eine Protokollzeile dieses Versuchs.</summary>
        KiErgebnis AbweisenUndVermerken(KiAufruf aufruf, string grund);

        /// <summary>
        /// Meldet Klarnamen an, die in freien Texten stehen koennen (H8). Der Kern kennt
        /// die Namensquellen nicht - sie stehen in der Datenbank.
        /// </summary>
        void KlarnamenAnmelden(KiPlatzhalter platzhalter, params string[] texte);
    }

    /// <summary>
    /// Die stille Standardfassung: leeres Register, jede Aktion abgelehnt.
    /// </summary>
    /// <remarks>
    /// Dasselbe Verhalten wie ein Chatfenster ohne Bestaetigungsweg - der Assistent
    /// antwortet, aber er aendert nichts. So laeuft der Kern auch dort, wo es gar
    /// keine Ausfuehrungsschicht gibt (Referenzlauf, Pruefstand, iOS vor iU11),
    /// ohne dass ein Aufrufer auf <c>null</c> pruefen muesste.
    /// </remarks>
    public sealed class KeineAusfuehrung : IKiAusfuehrung
    {
        private static readonly KiRegister LEER = new KiRegister();

        /// <inheritdoc/>
        public KiRegister Register => LEER;

        /// <inheritdoc/>
        public string LetzteProtokollzeile => "";

        /// <inheritdoc/>
        public Task<KiVorbereitung> VorbereitenAsync(KiAufruf aufruf, CancellationToken abbruch)
        {
            return Task.FromResult(new KiVorbereitung(null, Ablehnung(aufruf)));
        }

        /// <inheritdoc/>
        public Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, CancellationToken abbruch)
        {
            return Task.FromResult(Ablehnung(aufruf));
        }

        /// <inheritdoc/>
        public Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, KiFreigabe freigabe,
                                                CancellationToken abbruch)
        {
            return Task.FromResult(Ablehnung(aufruf));
        }

        /// <inheritdoc/>
        public KiErgebnis AbweisenUndVermerken(KiAufruf aufruf, string grund)
        {
            return KiErgebnis.Abgelehnt(grund ?? "");
        }

        /// <inheritdoc/>
        public void KlarnamenAnmelden(KiPlatzhalter platzhalter, params string[] texte)
        {
            // Ohne Datenbank gibt es keine Klarnamen anzumelden. Der Platzhalterbestand
            // bleibt, wie er ist - er wird dadurch nicht unsicherer, nur unvollstaendiger.
        }

        private static KiErgebnis Ablehnung(KiAufruf aufruf)
        {
            string name = aufruf == null ? "" : aufruf.Name;
            return KiErgebnis.Abgelehnt(
                string.Format(MyResource.Resource.KI_AKT_OHNE_BESTAETIGUNGSWEG, name));
        }
    }

    /// <summary>
    /// Der statische Halter der Ausfuehrungsschicht - dieselbe Bauart wie
    /// <c>Dienste</c> im Kern und aus demselben Grund: Die Huelle entsteht beim
    /// Programmstart, der Chatdienst wird spaeter und aus einem anderen Faden gerufen.
    /// </summary>
    public static class KiAusfuehrungsweg
    {
        private static IKiAusfuehrung _aktuell = new KeineAusfuehrung();

        /// <summary>
        /// Die eingelegte Fassung. Ein <c>null</c>-Wert stellt die stille Fassung
        /// wieder her - so kann ein Pruefling ohne Aufraeumzwang wechseln.
        /// </summary>
        public static IKiAusfuehrung Aktuell
        {
            get { return _aktuell; }
            set { _aktuell = value ?? new KeineAusfuehrung(); }
        }
    }
}
