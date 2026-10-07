namespace Sparnarok.Core.Constants;

public static class SeniorityMapper
{
    public static string GetTitleForLevel(int level)
    {
        return level switch
        {
            <= 4 => "Junior / Neófito",
            <= 9 => "Semi-Senior / Adepto",
            <= 14 => "Senior / Veterano",
            <= 19 => "Staff / Maestro",
            _ => "Principal / Leyenda"
        };
    }
}
