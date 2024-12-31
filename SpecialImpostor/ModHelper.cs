namespace SpecialImpostor
{
    public static class ModHelper
    {
        public static bool IsModded(this Vent vent) => vent.Id < 0;

        public static PlayerControl GetModdedInfo(this Vent vent) => GameData.Instance.GetPlayerById((byte)(vent.Id - int.MinValue)).Object;
    }
}