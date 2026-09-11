"""Offline runner. Predictive strategies require forecast snapshots or --oracle.

Forecast CSV: decision_time, known_at, timestamp, load_kw, pv_kw,
buy_eur_kwh, sell_eur_kwh. Each decision_time identifies a complete horizon.
Price publication times must be provided by the data-preparation layer.
"""
import argparse,csv,hashlib,json
from pathlib import Path
from ems import Battery,simulate,bill,optimize_horizon,timestamp,DT,validate_rows


def read_rows(path):
    with open(path,encoding='utf-8-sig',newline='') as f:
        rows=list(csv.DictReader(f))
    for r in rows:
        for key in ('load_kw','pv_kw','buy_eur_kwh','sell_eur_kwh'):
            r[key]=float(r[key])
    return rows


def rolling(rows,batteries,config,strategy,forecasts=None,oracle=False):
    validate_rows(rows)
    if forecasts is None and not oracle:
        raise ValueError('Predictive control requires forecasts or an explicit --oracle flag')
    horizon=config['planning']['horizon_steps']
    advance=config['planning']['replan_steps']
    if not 1<=advance<=horizon:
        raise ValueError('Invalid planning horizon/replan interval')
    state=[b.capacity_kwh*b.initial_soc for b in batteries]
    terminal=list(state)
    result=[]; logs=[];peak=0.0
    snapshot={}
    for f in forecasts or []:
        snapshot.setdefault(timestamp(f['decision_time']),[]).append(f)
    for start in range(0,len(rows),advance):
        decision=timestamp(rows[start]['timestamp'])
        n=min(horizon,len(rows)-start)
        execute_count=min(advance,len(rows)-start)
        view=rows[start:start+n] if oracle else sorted(snapshot.get(decision,[]),key=lambda r:timestamp(r['timestamp']))
        valid = len(view)>=n
        if valid and not oracle:
            view=view[:n]
            valid=all(timestamp(r['known_at'])<=decision and timestamp(r['timestamp'])==timestamp(rows[start+i]['timestamp']) for i,r in enumerate(view))
        options=dict(config['connection'])
        target=config['control']['peak_target_kw'] if strategy=='multi_use' else None
        if strategy=='pv_predictive':
            options.update(grid_charge=False,battery_export=False)
        try:
            if not valid:
                raise ValueError('Forecast unavailable, incomplete or published too late')
            plan=optimize_horizon(view,batteries,state,
                mode='pv' if strategy=='pv_predictive' else 'economic',
                terminal_energies=terminal,peak_target_kw=target,historical_peak_kw=peak,
                demand_eur_kw=config['tariff']['demand_eur_kw_period'] if strategy=='multi_use' else 0,
                time_limit_s=config['planning']['solver_time_limit_s'],**options)
            block=simulate(rows[start:start+execute_count],batteries,'planned',
                planned=plan['commands'][:execute_count],planned_curtailment=plan['planned_curtailment_kw'][:execute_count],
                initial_energies=state,peak_target_kw=target,**options)
            logs.append({'decision_time':decision.isoformat(),'status':'optimal','forecast_mode':'oracle' if oracle else 'vintage'})
        except (ValueError,RuntimeError) as err:
            # Explicit fallback; economic result is never labelled optimum.
            fallback='peak' if strategy=='multi_use' else 'pv_greedy'
            block=simulate(rows[start:start+execute_count],batteries,fallback,
                initial_energies=state,peak_target_kw=target,**options)
            logs.append({'decision_time':decision.isoformat(),'status':'fallback','reason':str(err)})
        result.extend(block)
        state=block[-1]['energy_end_kwh']
        peak=max(peak,max(r['import_kw'] for r in block))
    return result,logs


def run(input_path,config_path,strategy,out,forecast_path=None,oracle=False):
    rows=read_rows(input_path)
    config_bytes=Path(config_path).read_bytes()
    config=json.loads(config_bytes.decode('utf-8-sig'))
    batteries=[Battery(**b) for b in config['batteries']]
    options=config['connection']
    # The baseline retains identical PV and connection constraints, no battery auxiliaries.
    base=simulate(rows,[],'pv_greedy',**options)
    if strategy in ('arbitrage','pv_predictive','multi_use'):
        records,logs=rolling(rows,batteries,config,strategy,
            forecasts=read_rows(forecast_path) if forecast_path else None,oracle=oracle)
    else:
        records=simulate(rows,batteries,strategy,
            peak_target_kw=config['control']['peak_target_kw'] if strategy=='peak' else None,
            allocation=config['control']['allocation'],**options)
        logs=[]
    demand=config['tariff']['demand_eur_kw_period']
    baseline= bill(base,demand);with_storage=bill(records,demand)
    output=Path(out);output.mkdir(parents=True,exist_ok=True)
    summary={'strategy':strategy,'hours':len(rows)*DT,'billing_scope':config['tariff']['billing_scope'],
        'baseline':baseline,'with_storage':with_storage,
        'raw_bill_saving_eur':baseline['total_eur']-with_storage['total_eur'],
        'inventory_change_kwh':[records[-1]['energy_end_kwh'][i]-b.initial_soc*b.capacity_kwh for i,b in enumerate(batteries)],
        'comparison_note':'Normalize start/end inventory before annual project valuation; no short-period annualization.',
        'conversion_losses_kwh':sum(r['loss_kwh'] for r in records),
        'curtailed_kwh':sum(r['curtailed_kw']*DT for r in records),
        'violating_intervals':sum(r['import_violation_kw']>1e-6 for r in records),
        'peak_violating_intervals':sum(r['peak_violation_kw']>1e-5 for r in records),
        'forecasts':'perfect_foresight_upper_bound' if oracle else ('vintage' if forecast_path else 'reactive'),
        'fallback_count':sum(x['status']=='fallback' for x in logs),
        'input_sha256':hashlib.sha256(Path(input_path).read_bytes()).hexdigest(),
        'config_sha256':hashlib.sha256(config_bytes).hexdigest()}
    (output/'summary.json').write_text(json.dumps(summary,indent=2,ensure_ascii=False),encoding='utf-8')
    # Preserve the exact bytes that config_sha256 identifies, for later graphical review.
    (output/'config_snapshot.json').write_bytes(config_bytes)
    (output/'solver_log.json').write_text(json.dumps(logs,indent=2),encoding='utf-8')
    with (output/'timeseries.csv').open('w',newline='',encoding='utf-8') as f:
        flat=[]
        for r in records:
            line={k:v for k,v in r.items() if not isinstance(v,list)}
            for i,b in enumerate(batteries):
                line[b.id+'_power_kw']=r['battery_kw'][i]
                line[b.id+'_energy_kwh']=r['energy_end_kwh'][i]
            flat.append(line)
        w=csv.DictWriter(f,fieldnames=list(flat[0]));w.writeheader();w.writerows(flat)
    print(json.dumps(summary,indent=2,ensure_ascii=False))
    return summary


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--inputs',required=True);p.add_argument('--config',required=True)
    p.add_argument('--strategy',choices=['peak','pv_greedy','threshold','pv_predictive','arbitrage','multi_use'],required=True)
    p.add_argument('--out',required=True);p.add_argument('--forecasts');p.add_argument('--oracle',action='store_true')
    a=p.parse_args()
    run(a.inputs,a.config,a.strategy,a.out,a.forecasts,a.oracle)
