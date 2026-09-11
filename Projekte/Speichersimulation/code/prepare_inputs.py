"""Join local GROSS load with downloaded price and AC-PV interval series.

Tariffs are explicit: retail buy = spot * factor + addition.
Export = fixed export price (or spot * factor + export addition).
No levy, tax exemption or export entitlement is inferred.
"""
import argparse,csv,json
from pathlib import Path
from ems import timestamp,validate_rows


def read_series(path,column):
    result={}
    with open(path,encoding='utf-8-sig',newline='') as f:
        for row in csv.DictReader(f):
            t=timestamp(row['timestamp']).isoformat()
            if t in result:
                raise ValueError(f'Duplicate timestamp in {path}')
            result[t]=float(row[column])
    return result


def prepare(load_path,price_path,pv_path,tariff):
    load=read_series(load_path,'load_kw')
    price=read_series(price_path,'value')
    pv=read_series(pv_path,'value') if pv_path else {t:0 for t in load}
    if not (set(load)==set(price)==set(pv)):
        raise ValueError('All series must cover exactly the same timestamps')
    rows=[{'timestamp':t,'load_kw':load[t],'pv_kw':pv[t],
        'buy_eur_kwh':price[t]*tariff['buy_spot_factor']+tariff['buy_addition_eur_kwh'],
        'sell_eur_kwh':(tariff['fixed_export_eur_kwh'] if tariff.get('fixed_export_eur_kwh') is not None else price[t]*tariff['sell_spot_factor']+tariff['sell_addition_eur_kwh'])} for t in sorted(load)]
    validate_rows(rows)
    return rows


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--load',required=True);p.add_argument('--prices',required=True)
    p.add_argument('--pv');p.add_argument('--config',required=True);p.add_argument('--out',required=True)
    a=p.parse_args()
    config=json.loads(Path(a.config).read_text(encoding='utf-8'))
    rows=prepare(a.load,a.prices,a.pv,config['tariff'])
    Path(a.out).parent.mkdir(parents=True,exist_ok=True)
    with open(a.out,'w',encoding='utf-8',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
    print(f'{len(rows)} input rows written')
