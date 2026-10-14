using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Quelle einer Sole-Wärmepumpe am Erdsondenfeld</b> (AK3-W3d; Festlegung 21, Konzept Simulationsablauf 23):
    /// Die Angebotsfunktion des Kreises liest die Quelltemperatur am Stundenbeginn aus dem Feldzustand
    /// (<see cref="Erdsondenfeld.TemperaturAmBeginnDerOffenenStunde"/>) — in der Stunde, deren Beginn das Feld kennt,
    /// also wenn alle Stunden davor gemeldet sind. Für jede andere Stunde (vor dem Lauf, nach dem Jahr, außer der
    /// Reihe) bleibt sie benannt beim Jahresprofil der Quelle: der Vorbelegung des Feldes, die der Lauf Stunde um
    /// Stunde mit dem Feldzustand überschreibt. Ohne Nebenwirkung auf das Feld; gezählt wird nur, woher gelesen wurde.
    /// </summary>
    internal sealed class Sondenquelle : IQuellzustand
    {
        private readonly Erdsondenfeld _feld;
        private readonly double[] _profil;

        /// <param name="feld">Das Sondenfeld der Wärmepumpe.</param>
        /// <param name="profil">Die Quellreihe des Moduls (Vorbelegung des Feldes, im Lauf nachgeführt).</param>
        /// <param name="kapptUnten">Kappung unter der untersten Stützstelle (wie <see cref="Quellprofil"/>).</param>
        internal Sondenquelle(Erdsondenfeld feld, double[] profil, bool kapptUnten = false)
        {
            _feld = feld ?? throw new ArgumentNullException(nameof(feld));
            _profil = profil ?? throw new ArgumentNullException(nameof(profil));
            KapptUnten = kapptUnten;
        }

        /// <summary>Zahl der Abfragen, die der Feldzustand beantwortet hat.</summary>
        internal int AbfragenAusFeld { get; private set; }

        /// <summary>Zahl der Abfragen, die beim Jahresprofil geblieben sind (das Feld kannte den Stundenbeginn nicht).</summary>
        internal int AbfragenAusProfil { get; private set; }

        /// <summary>Kennt das Feld den Zustand am Beginn der Stunde <paramref name="stunde"/>?</summary>
        internal bool FeldKennt(int stunde) => stunde == _feld.GemeldeteStunden && stunde < Erdsondenfeld.STUNDEN;

        public double TemperaturAmStundenbeginn(int stunde)
        {
            if (FeldKennt(stunde))
            {
                AbfragenAusFeld++;
                return _feld.TemperaturAmBeginnDerOffenenStunde;
            }
            AbfragenAusProfil++;
            return _profil[stunde];
        }

        public bool KapptUnten { get; }
    }
}
