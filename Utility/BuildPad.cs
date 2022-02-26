using System;
using System.Collections.Generic;
using Gw2Sharp.WebApi.V2.Models;

namespace lk0001.CurrentTemplates.Utility
{
    class BuildPad
    {
        public static readonly string CONFIG_FILE_NAME = "config.ini";

        private readonly string path;
        private Dictionary<Constants.Profession, List<Build>> builds;

        public BuildPad(string path)
        {
            this.path = path;
            LoadBuilds();
        }

        public void LoadBuilds()
        {
            builds = new Dictionary<Constants.Profession, List<Build>>();
            foreach (Constants.Profession profession in Enum.GetValues(typeof(Constants.Profession)))
            {
                builds[profession] = new List<Build>();
            }

            try
            {
                bool started = false;
                foreach (string line in System.IO.File.ReadLines(this.path))
                {
                    if (!started)
                    {
                        if (line == "[Builds]") started = true;
                    }
                    else
                    {
                        if (line.Length > 0)
                        {
                            if (line[0] == '[')
                            {
                                break; // Next section is starting so we can stop
                            }
                            else
                            {
                                string[] items = line.Split('|');
                                Build build = new Build(items[5], items[1]);
                                builds[build.Profession].Add(build);
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                Module.Logger.Debug(ex, "Failed to read BuildPad config");
            }
        }

        public string? GetName(BuildTemplate buildTemplate)
        {
            Constants.Profession professionId = Constants.GetProfessionId(buildTemplate.Profession);
            foreach (Build build in builds[professionId])
            {
                if (build.EquivalentTo(buildTemplate.Specializations))
                {
                    return build.Name;
                }
            }
            return null;
        }

        public static bool ValidPath(string path)
        {
            return System.IO.File.Exists(path) && System.IO.Path.GetFileName(path) == CONFIG_FILE_NAME;
        }
    }
}
