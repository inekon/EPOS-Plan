"""Project NPV from explicit yearly, inventory-normalized simulation accounts.

Input JSON: capex_eur, discount_rate, residual_value_eur and years[] with
baseline_bill_eur, storage_bill_eur, opex_eur, replacement_eur.
Each record represents ONE full project year, not a repeated test-day saving.
Wear penalties from dispatch are not charged again as cash expenditure.
"""
import argparse,json
from pathlib import Path
from ems import npv


def evaluate(project):
    flows=[y['baseline_bill_eur']-y['storage_bill_eur']-y['opex_eur']-y['replacement_eur'] for y in project['years']]
    value=npv(project['capex_eur'],flows,project['discount_rate'],project.get('residual_value_eur',0))
    accumulated=-project['capex_eur'];payback=None
    for year,cf in enumerate(flows,1):
        accumulated+=cf/(1+project['discount_rate'])**year
        if accumulated>=0 and payback is None:
            payback=year
    return {'annual_net_cashflows_eur':flows,'npv_eur':value,'discounted_payback_first_year':payback}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('input');a=p.parse_args()
    print(json.dumps(evaluate(json.loads(Path(a.input).read_text(encoding='utf-8'))),indent=2))
