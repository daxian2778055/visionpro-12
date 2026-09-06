namespace WindowsFormsApplication1
{
    internal class Datas
    {
        public string tab { get; set; }
        public Tags tags { get; set; }
        public Fields fields { get; set; }
    }
    internal class Message
    {
        public string cmd { get; set; }
        public Datas data { get; set; }
    }
    internal class Tags
    {
        public string line { get; set; }
        public string psn { get; set; }
        public string esn { get; set; }
    }
    internal class Fields
    {
        public string modulelength { get; set; }
        public string modulewidth { get; set; }
        public string modulebottomflatness { get; set; }
        public string modulemountinghole1 { get; set; }
        public string modulemountinghole2 { get; set; }
        public string modulemountinghole3 { get; set; }
        public string modulemountinghole4 { get; set; }
        public string modulemountinghole5 { get; set; }
        public string modulemountinghole6 { get; set; }
        public string Moduleflatness { get; set; }
        public string Cellflatness { get; set; }
        public string Cellheightdiffmax { get; set; }
        public string Cellheightdiffmin { get; set; }
        public string modulediagonal1 { get; set; }
        public string modulediagonal2 { get; set; }
        public string modulediagonal3 { get; set; }
        public string modulediagonal4 { get; set; }
        public string modulediagonal5 { get; set; }
        public string modulediagonal6{ get; set; }
        public string result { get; set; }
        public string time { get; set; }
    }
}