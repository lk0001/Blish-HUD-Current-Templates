namespace lk0001.CurrentTemplates.Utility
{
    class TraitLine
    {
        public string Name;
        public TraitColumn[] Columns;
        public bool Elite = false;

        public TraitLine(string name, int[][] traits, bool elite)
        {
            this.Name = name;
            this.Elite = elite;
            this.Columns = new TraitColumn[3];
            for (int i = 0; i < 3; i++)
            {
                this.Columns[i] = new TraitColumn(traits[i]);
            }
        }

        public TraitLine(string name, int[][] traits) : this(name, traits, false) { }
    }
}
