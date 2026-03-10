# -*- coding: utf-8 -*-
import csv, re
from collections import defaultdict, namedtuple

CatalogRow = namedtuple('CatalogRow','Category Shape MatchKey SizePatternLow SizePatternHigh Angle Unit UnitPrice TypeNamePattern RawRow')

def _norm_matchkey(s):
    if not s:
        return ''
    z = re.sub(r'\s+', '', s).upper()
    if z in ('MAXDIMLEQ','MAX_DIM_LEQ','MAX-DIM-LEQ'):
        return 'MAX_DIM_LEQ'
    if z in ('PAIR','REDUCTION','REDUKCJA'):
        return 'PAIR'
    return z

def _detect_delimiter(path):
    with open(path, 'rb') as fh:
        line = fh.readline()
    try:
        s = line.decode('utf-8','ignore')
    except:
        s = line
    if s.count(';') >= s.count(',') and s.count(';')>0:
        return ';'
    if '\t' in s:
        return '\t'
    return ','

def load_catalog(csv_path):
    rows = []
    delim = _detect_delimiter(csv_path)
    with open(csv_path, 'rb') as f:
        data = f.read().decode('utf-8','ignore').splitlines()
    reader = csv.DictReader(data, delimiter=delim)
    def _g(row, key):
        lk = key.lower()
        for k in row.keys():
            if k and k.strip().lower()==lk:
                return row[k]
        return ''
    for raw in reader:
        cat  = (_g(raw,'category') or '').strip()
        shp  = (_g(raw,'shape') or '').strip().lower()
        mkey = _norm_matchkey(_g(raw,'matchkey') or '')
        sp   = (_g(raw,'sizepattern') or '').strip()
        ang  = (_g(raw,'angle') or '').strip()
        unit = (_g(raw,'unit') or '').strip().lower().replace(u'²','2')
        upr  = (_g(raw,'unitprice') or '').strip().replace(' ','').replace(',','.')
        tnp  = (_g(raw,'typenamepattern') or '').strip()
        if not cat or not shp or not unit or not upr:
            continue
        low, high = None, None
        spn = sp.replace(' ','')
        if shp=='round' and (('-' in spn) or ('/' in spn)) and ('x' not in spn and u'×' not in spn):
            sep = '-' if '-' in spn else '/'
            parts = spn.split(sep)
            if len(parts)==2:
                try:
                    a = float(parts[0].replace(',','.'))
                    b = float(parts[1].replace(',','.'))
                    low, high = (min(a,b), max(a,b))
                    if not mkey:
                        mkey='PAIR'
                except:
                    low, high = None, None
        elif '-' in spn:
            a,b = spn.split('-',1)
            try:
                low  = float(a.replace(',','.'))
                high = float(b.replace(',','.'))
            except:
                low, high = None, None
        else:
            if spn:
                try:
                    low = float(spn.replace(',','.'))
                    high = float(spn.replace(',','.'))
                except:
                    low, high = None, None
        angle_num = None
        if ang:
            try:
                angle_num = int(float(ang.replace(',','.')))
            except:
                angle_num = None
        try:
            price_f = float(upr)
        except:
            continue
        rows.append(CatalogRow(
            Category=cat, Shape=shp, MatchKey=mkey,
            SizePatternLow=low, SizePatternHigh=high,
            Angle=angle_num, Unit=unit, UnitPrice=price_f,
            TypeNamePattern=tnp, RawRow=raw))
    return rows

def build_catalog_index(rows):
    idx = {
        'round_any'          : defaultdict(list),
        'round_any_size'     : defaultdict(list),
        'round_typename'     : defaultdict(list),
        'round_pair_typename': defaultdict(list),
        'rect_bucket'        : defaultdict(list)
    }
    for r in rows:
        if r.Shape=='round':
            if (r.SizePatternLow is not None and r.SizePatternHigh is not None and r.SizePatternLow!=r.SizePatternHigh):
                keyp = (r.Category, (r.TypeNamePattern or '').lower(), int(round(max(r.SizePatternLow,r.SizePatternHigh))), int(round(min(r.SizePatternLow,r.SizePatternHigh))), r.Angle)
                idx['round_pair_typename'][keyp].append(r)
            if r.MatchKey=='ANY' and r.SizePatternLow is not None and (r.SizePatternHigh is None or r.SizePatternHigh==r.SizePatternLow):
                dia = int(round(r.SizePatternLow))
                keya = (r.Category, dia)
                idx['round_any'][keya].append(r)
                idx['round_any_size'][dia].append(r)
            if r.MatchKey in ('TYPENAME','PAIR') and r.SizePatternLow is not None and (r.SizePatternHigh is None or r.SizePatternHigh==r.SizePatternLow):
                keyt = (r.Category, (r.TypeNamePattern or '').lower(), int(round(r.SizePatternLow)), r.Angle)
                idx['round_typename'][keyt].append(r)
        elif r.Shape=='rect' and r.MatchKey in ('MAX_DIM_LEQ','MAX_DIM'):
            ub = int(round(r.SizePatternHigh if r.SizePatternHigh is not None else (r.SizePatternLow or 0)))
            keyr = (r.Category, r.Shape, ub)
            idx['rect_bucket'][keyr].append(r)
    return idx
