using System;
using System.Collections.Generic;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Eingaben der Wärmequelle Erdreich EINER Anlage, mit denen ein Simulationslauf rechnet,
    /// ohne dass sie gespeichert sind — der Lauf aus dem Erdreichdialog (Anwendermeldung 10.10.2026).
    ///
    /// <para><b>Warum es das gibt.</b> Der Dialog schreibt seine Eingaben erst beim OK (Hausregel:
    /// geschrieben wird im OK-Weg, sonst ist Abbrechen eine Behauptung). Rechnete sein Lauf mit dem
    /// gespeicherten Stand, zeigte er das Ergebnis der ALTEN Eingaben, und das Ergebnis der neuen sähe
    /// der Anwender erst nach OK, Wiederöffnen und einem zweiten Lauf. Die Vorgabe legt die Eingaben
    /// für die Dauer des Laufs über die gespeicherten Werte der Anlage — gelesen, nie geschrieben.</para>
    ///
    /// <para><b>Wo sie wirkt.</b> An den drei Lesestellen der Erdreicheingaben: den beiden Lesern
    /// je Anlage <see cref="WaermequelleClass.WertLesen(int,string)"/> und
    /// <see cref="WaermequelleClass.WertLesenStill(int,string)"/> (Quellsystem, Tiefe, Fläche,
    /// Anzahl, Bodentyp, Spreizung, Quelltyp, Sondenfeld) und der Klimazone des Projekts
    /// (<see cref="ErdreichAuswertung.KlimazoneDesProjekts"/>). Ohne angewandte Vorgabe gehen alle
    /// drei unverändert in die Datenbank — der Rechenweg bleibt derselbe.</para>
    ///
    /// <para><b>Gültigkeit.</b> Die Vorgabe hängt am <see cref="AsyncLocal{T}"/> des Laufs: Sie gilt
    /// in dem Ablauf, der sie anwendet, und in allem, was er startet (auch über
    /// <c>Kulturweitergabe</c>), nie im Oberflächenfaden daneben.</para>
    /// </summary>
    public sealed class ErdreichLaufvorgabe
    {
        private static readonly AsyncLocal<ErdreichLaufvorgabe> _aktiv = new AsyncLocal<ErdreichLaufvorgabe>();

        private readonly Dictionary<string, object> _spalten =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Das Projekt, dessen Klimazone die Vorgabe trägt.</summary>
        public int IdProjekt { get; }

        /// <summary>Die Energieanlage, deren Spalten die Vorgabe trägt.</summary>
        public int IdAnlage { get; }

        /// <summary>Die Klimazone (DIN 4710) für den Lauf.</summary>
        public int Klimazone { get; }

        private ErdreichLaufvorgabe(int idProjekt, int idAnlage, int klimazone)
        {
            IdProjekt = idProjekt;
            IdAnlage = idAnlage;
            Klimazone = klimazone;
        }

        /// <summary>
        /// Die Vorgabe aus denselben Sätzen, die der OK-Weg schreibt
        /// (<see cref="WaermequelleClass.QuelleSchreiben"/> mit Typ Erdreich und
        /// <see cref="ErdsondenfeldCtrl.Schreiben"/>): dieselben Spalten mit denselben Werten.
        /// <paramref name="feld"/> gilt nur beim Quellsystem Sonde; <c>null</c> lässt die
        /// gespeicherten Sondenfeldwerte stehen, wie es auch der OK-Weg tut.
        /// </summary>
        public static ErdreichLaufvorgabe Aus(int idProjekt, int idAnlage, QuelleErgebnis quelle,
                                               ErdsondenfeldEingabe feld, int klimazone)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));

            var v = new ErdreichLaufvorgabe(idProjekt, idAnlage, klimazone);
            v._spalten["WQ_Typ"] = WaermequelleClass.TYP_ERDREICH;
            v._spalten["WQ_Quellsystem"] = quelle.Quellsystem ?? "";
            v._spalten["WQ_Tiefe"] = quelle.Tiefe;
            v._spalten["WQ_Flaeche"] = quelle.Flaeche;
            v._spalten["WQ_Anzahl"] = quelle.Anzahl;
            v._spalten["WQ_Bodentyp"] = quelle.Bodentyp ?? "";
            v._spalten["WQ_Spreizung"] = quelle.SpreizungErdreich;

            if (feld != null)
            {
                v._spalten[ErdsondenfeldSchema.SPALTE_ABSTAND] = feld.AbstandM;
                v._spalten[ErdsondenfeldSchema.SPALTE_BOHRLOCHDURCHMESSER] = feld.BohrlochdurchmesserMm;
                v._spalten[ErdsondenfeldSchema.SPALTE_BOHRLOCHWIDERSTAND] = feld.Bohrlochwiderstand;
                v._spalten[ErdsondenfeldSchema.SPALTE_KOPFUEBERDECKUNG] = feld.KopfueberdeckungM;
                v._spalten[ErdsondenfeldSchema.SPALTE_BETRACHTUNGSJAHR] = feld.Betrachtungsjahr;
                v._spalten[ErdsondenfeldSchema.SPALTE_ANORDNUNG] = feld.Anordnung?.ToString();
            }
            return v;
        }

        /// <summary>
        /// Legt die Vorgabe über den laufenden Ablauf; das Ergebnis stellt beim Entsorgen den
        /// vorigen Stand wieder her (<c>using</c>).
        /// </summary>
        public IDisposable Anwenden()
        {
            ErdreichLaufvorgabe vorher = _aktiv.Value;
            _aktiv.Value = this;
            return new Rueckstellung(vorher);
        }

        /// <summary>
        /// Trägt die angewandte Vorgabe einen Wert für diese Spalte dieser Anlage? <c>null</c> im
        /// Ergebnis heißt „leer" (wie <c>DBNull</c> beim Lesen aus der Datenbank).
        /// </summary>
        internal static bool Wert(int idAnlage, string spalte, out object wert)
        {
            wert = null;
            ErdreichLaufvorgabe v = _aktiv.Value;
            if (v == null || v.IdAnlage != idAnlage || spalte == null) return false;
            return v._spalten.TryGetValue(spalte, out wert);
        }

        /// <summary>Die Klimazone der angewandten Vorgabe für dieses Projekt, sonst <c>null</c>.</summary>
        internal static int? KlimazoneFuer(int idProjekt)
        {
            ErdreichLaufvorgabe v = _aktiv.Value;
            return v != null && v.IdProjekt == idProjekt ? v.Klimazone : (int?)null;
        }

        private sealed class Rueckstellung : IDisposable
        {
            private readonly ErdreichLaufvorgabe _vorher;
            private bool _fertig;

            public Rueckstellung(ErdreichLaufvorgabe vorher) => _vorher = vorher;

            public void Dispose()
            {
                if (_fertig) return;
                _fertig = true;
                _aktiv.Value = _vorher;
            }
        }
    }
}
