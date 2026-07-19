import sys, json, time, urllib.request
BASE="http://192.168.50.1:30013"; USER="flan"
MYURL="https://git.onetick.ninja/flan/jellyfin-placard/raw/branch/main/manifest.json"
H='MediaBrowser Client="poster-pin", Device="code", DeviceId="poster-pin-code-8e1f", Version="1.0"'
def call(p,m="GET",d=None,h=None):
    r=urllib.request.Request(BASE+p,data=d,method=m,headers=h or {})
    with urllib.request.urlopen(r,timeout=30) as x:
        b=x.read(); return json.loads(b) if b else None
pw=sys.stdin.readline().rstrip("\n")
res=call("/Users/AuthenticateByName","POST",json.dumps({"Username":USER,"Pw":pw}).encode(),
         {"Authorization":H,"Content-Type":"application/json"})
TOK={"Authorization":f'MediaBrowser Token="{res["AccessToken"]}"'}
repos=call("/Repositories",h=TOK) or []
print("existing repos:", [r.get("Name") for r in repos])
if not any(r.get("Url")==MYURL for r in repos):
    repos.append({"Name":"Placard (flan)","Url":MYURL,"Enabled":True})
    call("/Repositories","POST",json.dumps(repos).encode(),{**TOK,"Content-Type":"application/json"})
    print("-> repo added")
else:
    print("-> repo already present")
time.sleep(5)
pkgs=call("/Packages",h=TOK) or []
placard=[p for p in pkgs if "placard" in (p.get("name","")).lower()]
if placard:
    p=placard[0]
    print("CATALOG OK:", p.get("name"), "| guid:", p.get("guid"),
          "| versions:", [v.get("version") for v in p.get("versions",[])])
else:
    print("CATALOG: Placard NOT found (container may not reach the manifest URL, or cache lag)")
