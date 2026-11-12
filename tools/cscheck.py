"""Exact CS0102 guard.
Direct members of each type are split with a brace-aware scan (nested blocks are
skipped), so a field and a method with one name in the same type are detected,
and nested types / locals never produce false positives."""
import re, sys, os, glob

def blank(text):
    out=[]; i=0; n=len(text); in_str=False; in_chr=False
    while i<n:
        ch=text[i]
        if in_str:
            if ch=='\\': i+=2; continue
            if ch=='"': in_str=False; out.append('"')
            i+=1; continue
        if in_chr:
            if ch=='\\': i+=2; continue
            if ch=="'": in_chr=False; out.append("'")
            i+=1; continue
        if ch=='/' and i+1<n and text[i+1]=='/':
            i+=2
            while i<n and text[i]!='\n': i+=1
            continue
        if ch=='/' and i+1<n and text[i+1]=='*':
            i+=2
            while i+1<n and not (text[i]=='*' and text[i+1]=='/'):
                if text[i]=='\n': out.append('\n')
                i+=1
            i+=2; continue
        if ch=='"': in_str=True; out.append('"'); i+=1; continue
        if ch=="'": in_chr=True; out.append("'"); i+=1; continue
        out.append(ch); i+=1
    return ''.join(out)

TYPE_RE = re.compile(r'\b(?:class|struct|interface|enum)\s+(\w+)')
FIELD_NAME = re.compile(r'([A-Za-z_]\w*)\s*(?:=.*)?$', re.S)
METHOD_NAME = re.compile(r'([A-Za-z_]\w*)\s*\([^;{}]*\)\s*$', re.S)
PROPERTY_NAME = re.compile(r'([A-Za-z_]\w*)\s*\{?\s*$', re.S)

def type_body(code, m):
    brace=code.find('{', m.end())
    if brace<0: return None
    depth=0; i=brace
    while i<len(code):
        if code[i]=='{': depth+=1
        elif code[i]=='}':
            depth-=1
            if depth==0: return brace+1, i
        i+=1
    return None

def direct_members(body):
    """Return list of (kind, name). kind: field | method | property"""
    items=[]; i=0; n=len(body); depth=0; start=0
    while i<n:
        ch=body[i]
        if ch in '([{':
            if ch=='{' and depth==0:
                header=body[start:i].strip()
                if header:
                    items.append(('body', header))
                # skip the block
                d=1; i+=1
                while i<n and d>0:
                    if body[i]=='{': d+=1
                    elif body[i]=='}': d-=1
                    i+=1
                start=i
                continue
            depth+=1
        elif ch in ')]}':
            if depth>0: depth-=1
        elif ch==';' and depth==0:
            header=body[start:i].strip()
            if header: items.append(('decl', header))
            start=i+1
        i+=1
    return items

def classify(kind, header):
    header=' '.join(header.split())
    if not header: return None
    m=TYPE_RE.search(header)
    if m and '=' not in header: return None          # nested type, skip
    if kind=='body':
        mm=METHOD_NAME.search(header)
        if mm: return ('method', mm.group(1))
        mm=PROPERTY_NAME.search(header)
        if mm and '=' not in header: return ('property', mm.group(1))
        return None
    if header.startswith('using ') or header.startswith('return'): return None
    mm=FIELD_NAME.search(header)
    if not mm: return None
    name=mm.group(1)
    if name in ('true','false','null'): return None
    return ('field', name)

def check(path):
    code=blank(open(path,encoding='utf-8').read())
    problems=[]
    for m in TYPE_RE.finditer(code):
        span=type_body(code, m)
        if not span: continue
        body=code[span[0]:span[1]]
        kinds={}
        for k,h in direct_members(body):
            c=classify(k,h)
            if not c: continue
            kinds.setdefault(c[1], set()).add(c[0])
        for name, ks in kinds.items():
            if 'field' in ks and ('method' in ks or 'property' in ks):
                problems.append((path, m.group(1), f'CS0102: "{name}" is a {"/".join(sorted(ks))} name clash'))
    b=code.count('{')-code.count('}'); p=code.count('(')-code.count(')')
    if b or p: problems.append((path,'-',f'unbalanced braces={b} parens={p}'))
    return problems

if __name__=='__main__':
    root=sys.argv[1]; total=0
    for f in sorted(glob.glob(os.path.join(root,'**','*.cs'), recursive=True)):
        if 'RTLTMPro' in f: continue
        for x in check(f):
            print('  ',x[0].split('/')[-1],'|',x[1],'|',x[2]); total+=1
    print('problems:', total)
