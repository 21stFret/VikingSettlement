using UnityEngine;

public static class VillagerNameGenerator
{
    private static readonly string[] firstNames = new string[]
    {
        "Bjorn", "Erik", "Harald", "Ragnar", "Leif", "Olaf", "Ivar", "Sigurd",
        "Astrid", "Freya", "Ingrid", "Helga", "Sigrid", "Gudrun", "Thorvald", "Gunnar"
    };
    
    private static readonly string[] lastNames = new string[]
    {
        "Ironside", "Bloodaxe", "the Bold", "the Wise", "Seafarer", "Stormbringer",
        "the Strong", "Ravenfeeder", "the Fearless", "Shieldmaiden", "Dragonslayer"
    };
    
    public static string GenerateNorseName()
    {
        string first = firstNames[Random.Range(0, firstNames.Length)];
        string last = lastNames[Random.Range(0, lastNames.Length)];
        return $"{first} {last}";
    }
}
