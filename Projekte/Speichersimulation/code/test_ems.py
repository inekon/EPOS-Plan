import unittest
from datetime import datetime,timedelta,timezone
from math import sqrt
from ems import Battery,allocate,execute,simulate,bill,npv,optimize_horizon,validate_rows,DT
from downloads import price_intervals,resample
from project_value import evaluate
from run import rolling


def rows(loads,pvs=None,prices=None):
    return [{'timestamp':(datetime(2025,1,1,tzinfo=timezone.utc)+timedelta(minutes=15*i)).isoformat(),
        'load_kw':float(load),'pv_kw':float((pvs or [0]*len(loads))[i]),
        'buy_eur_kwh':float((prices or [0.30]*len(loads))[i]),'sell_eur_kwh':0.08} for i,load in enumerate(loads)]


class PhysicsTests(unittest.TestCase):
    def test_round_trip(self):
        b=Battery('a',100,100,100,sqrt(.9),sqrt(.9),0,1,0)
        r=execute(rows([0])[0],[b],[0],[-40])
        e=r['energy_end_kwh'][0]
        r2=execute(rows([100])[0],[b],[e],[100])
        self.assertAlmostEqual(r2['battery_kw'][0]*DT,9)
        self.assertAlmostEqual(r2['energy_end_kwh'][0],0)
        self.assertAlmostEqual(r['loss_kwh']+r2['loss_kwh'],1)

    def test_charge_boundary(self):
        b=Battery('a',100,100,100,.95,.95,0,1,.99)
        r=execute(rows([0])[0],[b],[99],[-100])
        self.assertAlmostEqual(r['energy_end_kwh'][0],100)
        self.assertAlmostEqual(-r['battery_kw'][0],1/(.95*.25))

    def test_reserve_release_only_for_peak(self):
        b=Battery('a',100,50,50,reserve_kwh=20,initial_soc=.3)
        r=execute(rows([150])[0],[b],[30],[40])
        self.assertEqual(r['battery_kw'],[0])
        r=execute(rows([150])[0],[b],[30],[40],peak_target_kw=110)
        self.assertAlmostEqual(r['grid_kw'],110)

    def test_peak_uses_net_load(self):
        b=Battery('a',100,50,50,initial_soc=.9)
        r=simulate(rows([150],[60]),[b],'peak',peak_target_kw=100)[0]
        self.assertEqual(r['battery_kw'],[0])
        self.assertEqual(r['grid_kw'],90)

    def test_residual_peak_recorded(self):
        b=Battery('a',100,50,50,initial_soc=.1)
        r=simulate(rows([150]),[b],'peak',peak_target_kw=100)[0]
        self.assertEqual(r['peak_violation_kw'],50)

    def test_allocation_resaturates(self):
        batteries=[Battery('a',100,5,5),Battery('b',100,50,50)]
        p=allocate(40,batteries,[50,50])
        self.assertEqual(p,[5,35])

    def test_no_battery_export_but_pv_export(self):
        b=Battery('a',100,50,50)
        r=execute(rows([20],[50])[0],[b],[50],[30],battery_export=False)
        self.assertEqual(r['export_kw'],30)
        self.assertEqual(r['battery_kw'],[0])

    def test_no_grid_charge(self):
        b=Battery('a',100,50,50)
        r=execute(rows([20],[30])[0],[b],[50],[-40],grid_charge=False)
        self.assertEqual(r['battery_kw'],[-10])

    def test_pv_curtailment(self):
        r=simulate(rows([20],[100]),[],'pv_greedy',export_limit_kw=30)[0]
        self.assertEqual(r['curtailed_kw'],50)
        self.assertEqual(r['grid_kw'],-30)

    def test_balance_many_steps(self):
        batteries=[Battery('a',100,25,25),Battery('b',60,10,15,aux_kw=.1)]
        rs=simulate(rows([20,200,30,10]*24,[0,0,100,40]*24),batteries,'pv_greedy',grid_charge=False)
        for r in rs:
            energy_delta=sum(r['energy_end_kwh'])-sum(r['energy_start_kwh'])
            self.assertAlmostEqual(energy_delta,-sum(r['battery_kw'])*DT-r['loss_kwh'])
            self.assertAlmostEqual(r['grid_kw'],r['load_kw']-r['pv_kw']+r['curtailed_kw']+r['aux_kw']-sum(r['battery_kw']))

    def test_opposing_commands_rejected(self):
        bs=[Battery('a',100,50,50),Battery('b',100,50,50)]
        with self.assertRaises(ValueError):
            execute(rows([0])[0],bs,[50,50],[10,-10])


class DataFinanceTests(unittest.TestCase):
    def test_duplicate_timestamp_rejected(self):
        r=rows([1,2]);r[1]['timestamp']=r[0]['timestamp']
        with self.assertRaises(ValueError):validate_rows(r)

    def test_nan_rejected(self):
        r=rows([1]);r[0]['pv_kw']=float('nan')
        with self.assertRaises(ValueError):validate_rows(r)

    def test_hour_price_not_divided_by_four(self):
        a=datetime(2025,1,1,tzinfo=timezone.utc)
        data={'unix_seconds':[int(a.timestamp()),int((a+timedelta(hours=1)).timestamp())],
              'price':[100,100],'unit':'EUR / MWh'}
        out=resample(price_intervals(data,a,a+timedelta(hours=1)),a,a+timedelta(hours=1))
        self.assertEqual([r['value'] for r in out],[.1]*4)

    def test_resampling_gap_rejected(self):
        a=datetime(2025,1,1,tzinfo=timezone.utc)
        with self.assertRaises(ValueError):resample([(a,a+timedelta(minutes=15),1)],a,a+timedelta(minutes=30))

    def test_daylight_saving_lengths(self):
        from zoneinfo import ZoneInfo
        zone=ZoneInfo('Europe/Berlin')
        for year,month,day,expected in [(2025,3,30,92),(2025,10,26,100)]:
            a=datetime(year,month,day,tzinfo=zone).astimezone(timezone.utc)
            b=(datetime(year,month,day)+timedelta(days=1)).replace(tzinfo=zone).astimezone(timezone.utc)
            self.assertEqual(len(resample([(a,b,1)],a,b)),expected)

    def test_bill_savings_can_be_negative(self):
        base=simulate(rows([10]),[])
        storage=simulate(rows([10]),[Battery('a',100,50,50)],'peak',peak_target_kw=40)
        self.assertLess(bill(base,120)['total_eur']-bill(storage,120)['total_eur'],0)

    def test_pv_opportunity_cost_with_losses(self):
        b=Battery('a',10,10,10,1,.9,0,1,0)
        r=rows([0,3.6],[4,0]);base=simulate(r,[])
        sim=simulate(r,[b],'pv_greedy',grid_charge=False)
        self.assertAlmostEqual(bill(base)['total_eur']-bill(sim)['total_eur'],.9*.3-1*.08)

    def test_npv_known_result(self):
        self.assertAlmostEqual(npv(1000,[600,600],.1),41.322314049586566)

    def test_rainflow_half_cycles(self):
        from degradation import cycle_damage
        d=cycle_damage([0,.8,0],[(.01,10000),(.8,1000)])
        self.assertAlmostEqual(sum(x['count'] for x in d['cycles']),1)
        self.assertAlmostEqual(d['miner_damage'],.001)

    def test_forecast_leakage_falls_back(self):
        b=Battery('a',10,4,4)
        r=rows([4,4])
        forecast=[{**x,'decision_time':r[0]['timestamp'],'known_at':r[1]['timestamp']} for x in r]
        cfg={'planning':{'horizon_steps':2,'replan_steps':2,'solver_time_limit_s':10},
             'connection':{},'control':{'peak_target_kw':4},'tariff':{'demand_eur_kw_period':0}}
        result,log=rolling(r,[b],cfg,'arbitrage',forecasts=forecast)
        self.assertEqual(log[0]['status'],'fallback')
        self.assertEqual(len(result),2)


class OptimizerTests(unittest.TestCase):
    def test_profitable_shift_and_terminal(self):
        b=Battery('a',2,4,4,1,1,0,1,0,wear_eur_per_kwh_out=0)
        r=rows([0,4],prices=[.1,.4])
        plan=optimize_horizon(r,[b],[0])
        sim=simulate(r,[b],'planned',planned=plan['commands'])
        self.assertAlmostEqual(plan['terminal_energies'][0],0)
        self.assertAlmostEqual(bill(sim)['total_eur'],.1)

    def test_negative_prices_no_artificial_cycle(self):
        b=Battery('a',10,10,10,.9,.9,0,1,.5,wear_eur_per_kwh_out=0)
        r=rows([0],prices=[-.3]);r[0]['sell_eur_kwh']=-.4
        plan=optimize_horizon(r,[b],[5],battery_export=True)
        self.assertAlmostEqual(plan['commands'][0][0],0)

    def test_two_batteries_dispatch_and_peak(self):
        bs=[Battery('a',20,10,10,1,1,0,1,.5),Battery('b',10,5,5,1,1,0,1,.5)]
        r=rows([10,30,10,10])
        plan=optimize_horizon(r,bs,[10,5],peak_target_kw=20)
        self.assertLessEqual(plan['planned_peak_kw'],20.0001)
        self.assertAlmostEqual(sum(plan['commands'][1]),10,places=4)

    def test_plan_replay_keeps_individual_energy(self):
        bs=[Battery('a',100,50,50),Battery('b',50,25,25,.94,.94)]
        r=rows([20,100,20,20])
        plan=optimize_horizon(r,bs,[50,25],peak_target_kw=50)
        sim=simulate(r,bs,'planned',planned=plan['commands'],peak_target_kw=50)
        for actual,expected in zip(sim[-1]['energy_end_kwh'],[50,25]):
            self.assertAlmostEqual(actual,expected,places=5)

    def test_infeasible_is_not_reported_optimal(self):
        b=Battery('a',10,1,1)
        with self.assertRaises(RuntimeError):
            optimize_horizon(rows([100]),[b],[5],import_limit_kw=20)

    def test_economic_curtailment_replayed(self):
        b=Battery('a',10,1,1)
        r=rows([0],[10]);r[0]['sell_eur_kwh']=-.1
        plan=optimize_horizon(r,[b],[5])
        sim=simulate(r,[b],'planned',planned=plan['commands'],planned_curtailment=plan['planned_curtailment_kw'])
        self.assertAlmostEqual(sim[0]['curtailed_kw'],10)
        self.assertAlmostEqual(bill(sim)['total_eur'],0)

    def test_predictive_pv_is_chronological(self):
        b=Battery('a',4,4,4,1,1,0,1,0)
        r=rows([0,0,4],[4,4,0])
        plan=optimize_horizon(r,[b],[0],mode='pv')
        self.assertAlmostEqual(plan['commands'][0][0],0,places=4)
        self.assertAlmostEqual(plan['commands'][1][0],-4,places=4)
        self.assertAlmostEqual(plan['commands'][2][0],4,places=4)


if __name__=='__main__':unittest.main(verbosity=2)
