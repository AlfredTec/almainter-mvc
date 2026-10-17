namespace AlmaInter.Services
{
    public class SupabaseOptions
    {
        public const string Seccion = "Supabase";

        public string Url { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string Bucket { get; set; } = "";
    }
}
