using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die <b>eine</b> Definition der Assistentenseiten (Paket P4).
    ///
    /// <para>
    /// <b>Vorher.</b> Dieselben dreizehn Zeilen standen wortgleich zweimal in
    /// <c>Controller\MenueCtrl.cs</c> — einmal in <c>ProjektNeu()</c>, einmal in
    /// <c>ProjektBearbeiten()</c>. Die beiden Methoden unterschieden sich ausschließlich
    /// im <c>SetWizardMode(...)</c>; jede neue Seite hätte an beiden Stellen gepflegt
    /// werden müssen.
    /// </para>
    /// <para>
    /// <b>Reihenfolge und Inhalt sind unverändert übernommen.</b> Der Index einer Zeile
    /// ist zugleich ihre Kennung aus <see cref="WizardItemClass"/>
    /// (<c>KOMPONENTEN_ITEM</c> = 0 … <c>BHKW_ITEM</c> = 12) — <c>WizardParent</c>
    /// spricht die Liste ausschließlich über diese Konstanten an, deshalb darf hier
    /// nichts umsortiert werden.
    /// </para>
    /// <para>
    /// Die Erzeuger stehen als <see cref="Func{TResult}"/> und nicht als
    /// <see cref="Type"/>-Liste da: So bleibt der Bauplan vom Übersetzer geprüft
    /// (ein umbenanntes oder entferntes Formular bricht den Build), statt erst zur
    /// Laufzeit über <c>Activator.CreateInstance</c> aufzufallen.
    /// </para>
    /// </summary>
    internal static class AssistentSeiten
    {
        private static readonly Func<Form>[] ERZEUGER =
        {
            /* 0  KOMPONENTEN_ITEM   */ () => new Wizard_Komponenten(),
            /* 1  PROJEKT_ITEM       */ () => new Wizard_Projekt(),
            /* 2  GEBAEUDE_ITEM      */ () => new Form_Gebaeude(),
            /* 3  WAERMEBEDARF_ITEM  */ () => new Form_Waermebedarf(),
            /* 4  PROZESS_ITEM       */ () => new Form_Prozesswaerme(),
            /* 5  STROMSTD_ITEM      */ () => new Form_Stromverbraucher(),
            /* 6  STROMLASTGANG_ITEM */ () => new Wizard_Stromlastgang(),
            /* 7  WP_ITEM            */ () => new Form_WPAuswahl(),
            /* 8  SOLAR_ITEM         */ () => new Form_SolarKollektoren(),
            /* 9  PV_ITEM            */ () => new Form_PV(),
            /* 10 SP_ITEM            */ () => new Form_Stromspeicher(),
            // iU9-W6.3: Die Kesselseite ist eine Razor-Komponente; die Huelle baut ihre
            // WebView erst in Bestuecken (siehe BlazorAssistentSeite).
            /* 11 KESSEL_ITEM        */ () => HeizkesselHuelle.AssistentSeite(),
            /* 12 BHKW_ITEM          */ () => new Form_BHKWEing()
        };

        private static readonly ReadOnlyCollection<Type> _typen = new ReadOnlyCollection<Type>(new[]
        {
            typeof(Wizard_Komponenten), typeof(Wizard_Projekt), typeof(Form_Gebaeude),
            typeof(Form_Waermebedarf), typeof(Form_Prozesswaerme), typeof(Form_Stromverbraucher),
            typeof(Wizard_Stromlastgang), typeof(Form_WPAuswahl), typeof(Form_SolarKollektoren),
            typeof(Form_PV), typeof(Form_Stromspeicher),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Erzeuger.HeizkesselDialog>),
            typeof(Form_BHKWEing)
        });

        /// <summary>Anzahl der Assistentenseiten (13).</summary>
        public static int Anzahl
        {
            get { return ERZEUGER.Length; }
        }

        /// <summary>
        /// Die Seitentypen in ihrer festen Reihenfolge — <b>eine</b> unveränderliche
        /// Sammlung, die beide Aufrufer in <c>MenueCtrl</c> teilen. Dient dem Nachweis,
        /// dass es die frühere Doppelpflege nicht mehr gibt.
        /// </summary>
        public static ReadOnlyCollection<Type> Seitentypen
        {
            get { return _typen; }
        }

        /// <summary>
        /// Baut die Seitenliste für einen Assistentenlauf: je Zeile ein frisches
        /// Formular. Beide Einstiege (Neu und Bearbeiten) rufen genau diese Methode;
        /// sie unterscheiden sich danach nur noch in <c>SetWizardMode(...)</c>.
        /// </summary>
        public static List<WizardSeite> Erzeugen()
        {
            List<WizardSeite> seiten = new List<WizardSeite>(ERZEUGER.Length);
            for (int i = 0; i < ERZEUGER.Length; i++)
                seiten.Add(new WizardSeite(ERZEUGER[i](), i));
            return seiten;
        }
    }
}
