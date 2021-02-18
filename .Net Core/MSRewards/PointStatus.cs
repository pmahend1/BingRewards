namespace MSRewards
{
    public class PointStatus
    {
        public int Current { get; set; }

        public int Maximum { get; set; }

        public PointStatus(int current, int max)
        {
            this.Current = current;
            this.Maximum = max;
        }
    }
}