using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Filterstand JE KATALOG ueber die Sitzung</b> — Frage
    /// <b>W14a-E-10-Q2</b> aus Kapitel 8 des <c>Konzept_Katalogfilter</c>, Stufe
    /// S2.5: „Filterzustand merken — nur fuer die Sitzung oder dauerhaft?
    /// <b>Sitzung</b>, je Katalog, gemeinsam fuer Verwaltung und Projektdialog."
    ///
    /// <para><b>Wozu.</b> Wer im Projektdialog „Heizkessel" nach Gas filtert, den
    /// Katalog ueber „Verwaltung…" oeffnet, dort einen Satz bearbeitet und
    /// zurueckkommt, will seinen Filter wiederfinden — es ist DERSELBE Katalog.
    /// Bis hierher fuehrte jeder Wirt seinen eigenen
    /// <see cref="Katalogfilterstand"/> als Feld; er war nach dem Schliessen des
    /// Dialogs weg, und Verwaltung und Projektdialog wussten nichts voneinander.</para>
    ///
    /// <para><b>Warum im KERN und nicht als <c>static</c> in einer Razor-Komponente.</b>
    /// Ein statisches Feld in einer Komponente waere prozessweiter Zustand an einem
    /// Ort, den weder eine Kernprobe noch eine bunit-Probe zuruecksetzen kann — und
    /// der Zustand ist FACHLICH: „welcher Katalog steht wie gefiltert" ist eine
    /// Frage an die Anwendung, nicht an ein Steuerelement. Hier liegt er neben
    /// <see cref="Katalogfilterprofil"/> und <see cref="Katalogfilter"/> und ist
    /// ohne Oberflaeche pruefbar.</para>
    ///
    /// <para><b>Sitzung, nicht dauerhaft.</b> Es wird nichts geschrieben — kein
    /// <c>Dienste.Einstellungen</c>, keine Datei. Der Stand lebt so lange wie der
    /// Prozess. Die Begruendung steht in Kapitel 8: „ein gespeicherter Filter, an
    /// den man sich beim naechsten Programmstart nicht erinnert, laesst einen
    /// Katalog leer wirken." Dauerhaft wird er erst, wenn der Anwender es nach der
    /// Abnahme vermisst.</para>
    ///
    /// <para><b>Der PROJEKTWECHSEL raeumt nicht auf</b> — und das ist Absicht: Der
    /// Filter haengt am KATALOG, und der Katalog ist projektuebergreifend. Was ein
    /// Projektwechsel unbrauchbar machte, waere die Spalte „im Projekt verwendet";
    /// die ist aber kein Filterstand, sondern ein Wert je Zeile (S2.3).</para>
    /// </summary>
    public static class Katalogfilterregister
    {
        private static readonly object _schloss = new object();

        private static readonly Dictionary<string, Katalogfilterstand> _staende =
            new Dictionary<string, Katalogfilterstand>(StringComparer.Ordinal);

        /// <summary>
        /// Der Stand dieses Katalogs — beim ersten Zugriff ein frischer, danach
        /// derselbe. <b>Es ist DIESELBE Instanz fuer Verwaltung und Projektdialog</b>;
        /// genau daran haengt das gemeinsame Gedaechtnis.
        /// </summary>
        public static Katalogfilterstand Stand(Anlagenart art)
        {
            return Stand("ANLAGE_" + art);
        }

        /// <summary>
        /// Derselbe Weg ueber den sprachneutralen KATALOGSCHLUESSEL
        /// (<see cref="Katalogfilterprofil.Schluessel"/>) — seit Stufe S3, in der sechs
        /// Kataloge dazukommen, die keine <see cref="Anlagenart"/> sind (drei Bedarfe,
        /// drei Zeitreihen). Fuer die acht Anlagenkataloge liefert
        /// <see cref="Stand(Anlagenart)"/> denselben Eintrag; das gemeinsame
        /// Gedaechtnis aus S2.5 bleibt unberuehrt.
        /// </summary>
        public static Katalogfilterstand Stand(string katalog)
        {
            string schluessel = katalog ?? "";

            lock (_schloss)
            {
                Katalogfilterstand s;
                if (!_staende.TryGetValue(schluessel, out s))
                {
                    s = new Katalogfilterstand();
                    _staende[schluessel] = s;
                }
                return s;
            }
        }

        /// <summary>
        /// Fuehrt dieser Katalog schon einen Stand? (Pruefhilfe — sie legt keinen an.)
        /// </summary>
        public static bool Bekannt(Anlagenart art)
        {
            return Bekannt("ANLAGE_" + art);
        }

        /// <summary>Dieselbe Pruefhilfe ueber den Katalogschluessel (Stufe S3).</summary>
        public static bool Bekannt(string katalog)
        {
            lock (_schloss) { return _staende.ContainsKey(katalog ?? ""); }
        }

        /// <summary>
        /// Wirft ALLE gemerkten Staende weg. Gedacht fuer Proben und fuer den Fall,
        /// dass die Datenbank gewechselt wird — ein Filterausdruck auf einer anderen
        /// Datei ist keine Erinnerung, sondern ein Missverstaendnis.
        /// </summary>
        public static void Leeren()
        {
            lock (_schloss) { _staende.Clear(); }
        }
    }
}
