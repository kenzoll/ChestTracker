namespace ChestCounter;

// Replay properties are deltas, not complete snapshots. Identity and mesh must survive channel changes.
public sealed record ContainerInteraction(float Time,string Key,string Blueprint,uint Pawn,bool Destroyed,string Mesh);
public sealed class ContainerTracker
{
    private sealed class State
    {
        public string Blueprint="",Mesh="";
        public uint? Pawn,TerminalPawn;
        public float PawnAt,TerminalAt;
        public bool Terminal,Destroyed;
    }
    private readonly Dictionary<string,State> states=[];
    private readonly Dictionary<string,string> aliases=[];
    private string Canonical(string key){while(aliases.TryGetValue(key,out var next)&&next!=key)key=next;return key;}
    public void Alias(string previous,string canonical)
    {
        previous=Canonical(previous);canonical=Canonical(canonical);if(previous==canonical)return;
        aliases[previous]=canonical;
        if(!states.Remove(previous,out var old))return;
        if(!states.TryGetValue(canonical,out var current)){states[canonical]=old;return;}
        if(string.IsNullOrEmpty(current.Mesh))current.Mesh=old.Mesh;
        if(string.IsNullOrEmpty(current.Blueprint))current.Blueprint=old.Blueprint;
        if(old.PawnAt>current.PawnAt){current.PawnAt=old.PawnAt;current.Pawn=old.Pawn;}
        if(old.Terminal&&(!current.Terminal||old.TerminalAt<current.TerminalAt))
        {current.Terminal=true;current.TerminalAt=old.TerminalAt;current.TerminalPawn=old.TerminalPawn;current.Destroyed=old.Destroyed;}
    }
    public void Observe(string key,string blueprint,float time,uint? pawn,bool? searched,bool? destroyed,string mesh)
    {
        if(string.IsNullOrEmpty(key)||!float.IsFinite(time))return;
        key=Canonical(key);
        if(!states.TryGetValue(key,out var state))states[key]=state=new();
        if(!string.IsNullOrWhiteSpace(blueprint)&&blueprint!="?")state.Blueprint=blueprint;
        if(!string.IsNullOrWhiteSpace(mesh))state.Mesh=mesh;
        if(pawn.HasValue){state.Pawn=pawn.Value==0?null:pawn;state.PawnAt=time;}
        if(!state.Terminal&&(searched==true||destroyed==true))
        {
            state.Terminal=true;state.TerminalAt=time;state.Destroyed=destroyed==true;
            if(state.Pawn.HasValue&&time-state.PawnAt<=2)state.TerminalPawn=state.Pawn;
        }
        else if(state.Terminal&&!state.TerminalPawn.HasValue&&pawn is >0&&time>=state.TerminalAt&&time-state.TerminalAt<=1)
            state.TerminalPawn=pawn;
        // A later destruction of an already opened chest is the same interaction, not a second chest.
    }
    public IEnumerable<ContainerInteraction> Interactions=>states.Where(s=>s.Value.Terminal&&s.Value.TerminalPawn.HasValue)
        .Select(s=>new ContainerInteraction(s.Value.TerminalAt,s.Key,s.Value.Blueprint,s.Value.TerminalPawn!.Value,s.Value.Destroyed,s.Value.Mesh));
    public string MeshFor(string key)=>states.TryGetValue(Canonical(key),out var state)?state.Mesh:"";
}

public static class ContainerKind
{
    public static string Resolve(string blueprint,string? mesh)
    {
        string basic=ChestKinds.Classify(blueprint);
        if(basic is not (ChestKinds.Normal or ChestKinds.Rare or ChestKinds.Other))return basic;
        string appearance=(mesh??"").ToLowerInvariant();
        if(appearance.Contains("sprite")||appearance.Contains("spirit"))return ChestKinds.PosterChest;
        if(appearance.Contains("relic")||appearance.Contains("extraction"))return ChestKinds.Extraction;
        if(appearance.StartsWith("loot_chest_piratetreasure_legendary")||appearance.StartsWith("loot_chest_piratetreasure_rare"))return ChestKinds.Rare;
        if(appearance is "loot_chest_piratetreasure" or "loot_chest_piratetreasure_searched")return ChestKinds.Normal;
        // This spawner is also used for ordinary chests; its name alone is not proof of rarity.
        if(blueprint.Contains("AlwaysSpawn_RareChest",StringComparison.OrdinalIgnoreCase))return ChestKinds.Other;
        return basic;
    }
}
