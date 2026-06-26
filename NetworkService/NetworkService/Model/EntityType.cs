namespace NetworkService.Model
{
    public class EntityType
    {
        public string Ime { get; set; }
        public string Slika { get; set; }

        public static readonly EntityType SolarniPanel = new EntityType
        {
            Ime = "Solarni panel",
            Slika = "/Resources/Images/SolarniPanel.png"
        };

        public static readonly EntityType Vjetrogenerator = new EntityType
        {
            Ime = "Vjetrogenerator",
            Slika = "/Resources/Images/Vetrogenerator.png"
        };
    }
}
