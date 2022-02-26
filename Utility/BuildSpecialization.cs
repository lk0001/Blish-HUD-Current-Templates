namespace lk0001.CurrentTemplates.Utility
{
    class BuildSpecialization
    {
        public Constants.Specialization Id;
        public int[] BuildTraits;
        public int[] Traits;

        public BuildSpecialization(Constants.Specialization id, int[] traits)
        {
            this.Id = id;
            this.BuildTraits = traits;
            this.Traits = new int[3];
            for (int i = 0; i < 3; i++)
            {
                this.Traits[i] = Constants.TraitLines[id].Columns[i].Traits[traits[i]];
            }
        }
    }
}
