using Unity.VisualScripting;
using UnityEngine;

public static class VillagerNameGenerator
{
    private static readonly string[] firstMaleNames = new string[]
    {
        "Bjorn", "Erik", "Harald", "Ragnar", "Leif", "Olaf", "Ivar", "Sigurd",
        "Gudrun", "Thorvald", "Gunnar", "Knut", "Aslak", "Svein", "Floki", 
    };
    
    private static readonly string[] clanNames = new string[]
    {
        "Bloodaxe", "Ironfist", "Stormborn", "Wolfson", "Bearclaw", "Dragonbane",
    };

    private static readonly string[] firstFemaleNames = new string[]
    {
        "Astrid", "Freya", "Ingrid", "Helga", "Thyra", "Ragnhild",
        "Solveig", "Eirka", "Kari", "Liv", "Sunniva", "Bodil", "Hilda", "Yrsa"
    };

    private static readonly string maleSuffix = "son";
    private static readonly string femaleSuffix = "sdottir";

    public static string GenerateNorseName(Gender gender, string parentName)
    {
        string first = "";
        string last = "";
        string name = "";

        if (gender == Gender.Female)
        {
            first = firstFemaleNames[Random.Range(0, firstFemaleNames.Length)];
        }
        else
        {
            first = firstMaleNames[Random.Range(0, firstMaleNames.Length)];
        }

        if (string.IsNullOrEmpty(parentName))
        {

            if (gender == Gender.Female)
            {
                last = firstMaleNames[Random.Range(0, firstMaleNames.Length)] + femaleSuffix;
            }
            else
            {
                last = firstMaleNames[Random.Range(0, firstMaleNames.Length)] + maleSuffix;
            }
        }
        else
        {
            if (gender == Gender.Female)
            {
                last = parentName.Split(' ')[0] + femaleSuffix;
            }
            else
            {
                last = parentName.Split(' ')[0] + maleSuffix;
            }
        }

        name = $"{first} {last}";
        return name;
    }

    public static string GenerateClanName()
    {
        return clanNames[Random.Range(0, clanNames.Length)];
    }

}
