namespace WPF.BazhenovAI
{
    public class AnomalyResult
    {
        public DateTime Time { get; set; }
        public float ErrorCount { get; set; }
        public bool IsAnomaly { get; set; }
        public double Score { get; set; }
    }
}
