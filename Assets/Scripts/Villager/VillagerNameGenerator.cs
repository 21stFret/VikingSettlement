using UnityEngine;

public static class VillagerNameGenerator
{
    private static readonly string[] firstMaleNames = new string[]
    {
        "Bjorn", "Erik", "Harald", "Ragnar", "Leif", "Olaf", "Ivar", "Sigurd",
        "Gudrun", "Thorvald", "Gunnar"
    };
    
    private static readonly string[] lastMaleNames = new string[]
    {
        "Ironside", "Bloodaxe", "the Bold", "the Wise", "Seafarer", "Stormbringer",
        "the Strong", "Ravenfeeder", "the Fearless", "Dragonslayer"
    };

    private static readonly string[] firstFemaleNames = new string[]
    {
        "Astrid", "Freya", "Ingrid", "Helga", "Thyra", "Ragnhild",
        "Solveig", "Eirka", "Kari", "Liv", "Sunniva", "Bodil", "Hilda", "Yrsa"
    };

    private static readonly string[] lastFemaleNames = new string[]
    {
        "Shieldmaiden", "Ravenhair", "the Crone", "the Mother", "Landwalker", "Stormcalmer",
        "the Carer", "Dragonheart", "the Maiden", "Icebreaker", "Wavewalker"
    };
    
    public static string GenerateNorseName(Gender gender)
    {
        if (gender == Gender.Female)
        {
            string first = firstFemaleNames[Random.Range(0, firstFemaleNames.Length)];
            string last = lastFemaleNames[Random.Range(0, lastFemaleNames.Length)];
            return $"{first} {last}";
        }
        string firstMale = firstMaleNames[Random.Range(0, firstMaleNames.Length)];
        string lastMale = lastMaleNames[Random.Range(0, lastMaleNames.Length)];
        return $"{firstMale} {lastMale}";
    }
}
