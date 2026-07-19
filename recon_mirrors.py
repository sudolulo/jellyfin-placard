import sys, json, urllib.request
GITEA="https://git.onetick.ninja/api/v1"
token=sys.stdin.readline().strip()
def g(path):
    r=urllib.request.Request(GITEA+path, headers={"Authorization":f"token {token}"})
    with urllib.request.urlopen(r,timeout=20) as x: return json.loads(x.read())
repos=[r["name"] for r in g("/users/flan/repos?limit=50") if not r["private"]]
for name in repos:
    try: pm=g(f"/repos/flan/{name}/push_mirrors")
    except Exception: pm=[]
    try: topics=g(f"/repos/flan/{name}/topics").get("topics",[])
    except Exception: topics=[]
    if pm:
        for m in pm:
            print(f"[MIRROR] {name:26s} -> {m.get('remote_address')}  sync_on_commit={m.get('sync_on_commit')}  topics={topics}")
    else:
        print(f"         {name:26s}  topics={topics}")
