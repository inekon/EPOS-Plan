"""Create deterministic synthetic quarter-hour inputs, never measured site data."""
from datetime import datetime,timedelta,timezone
from pathlib import Path
import csv,math

def create(path,days=3):
    result=[]
    for i in range(days*96):
        h=(i%96)/4
        load=40+(25 if 7<=h<18 else 0)+(135 if 16<=h<16.5 else 0)
        pv=max(0,120*math.sin(math.pi*(h-6)/12))
        buy=.12 if h<6 else (.40 if 17<=h<21 else .25)
        result.append({'timestamp':(datetime(2025,6,1,tzinfo=timezone.utc)+timedelta(minutes=15*i)).isoformat(),
            'load_kw':load,'pv_kw':round(pv,6),'buy_eur_kwh':buy,'sell_eur_kwh':.08})
    path=Path(path);path.parent.mkdir(parents=True,exist_ok=True)
    with path.open('w',newline='',encoding='utf-8') as f:
        w=csv.DictWriter(f,fieldnames=list(result[0]));w.writeheader();w.writerows(result)

if __name__=='__main__':create(Path(__file__).resolve().parents[1]/'beispieldaten'/'synthetisch_3_tage.csv')
