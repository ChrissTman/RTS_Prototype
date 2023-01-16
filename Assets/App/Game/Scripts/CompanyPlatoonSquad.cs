using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Company
{
    public Team Team;
    public string Prefix;
    public List<Platoon> Platoons;
}

public enum PlatoonType { none, Rifle, Explosive, HQ, Artillery }
public class Platoon
{
    public PlatoonType Type;
    public Company Company = new Company();
    public Team Team;
    public int Index;
    public List<Squad> Squads = new List<Squad>();

    public Dictionary<AmmunitionType, int> Magazines = new Dictionary<AmmunitionType, int>();

    public int Alive;
}

public class Squad
{
    public Platoon Platoon;
    
    //public const int ReconPortion = 5;

    //TODO: check performance and maybe switch to FixedList
    public List<Unit> Units { get; set; } = new List<Unit>();
    
    public int Alive;

    //public List<Unit> ReconUnits { get; set; } = new List<Unit>();

    //public int ID { get { return id; } }

    //public string Info;

    public string Mock_Ammo;

    //int id;
    //static int staticID;
    public Team Team;
    public UnitType Type;

    public Squad(Team Team, UnitType Type)
    {
        this.Team = Team;
        this.Type = Type;
    }
}