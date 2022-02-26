using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gw2Sharp.WebApi.V2.Models;

namespace lk0001.CurrentTemplates.Utility
{
    class Build
    {
        public string Name;
        public string ChatCode;
        public Constants.Profession Profession;
        public List<BuildSpecialization> Specializations;
        private string SerializedBuild;

        static readonly Regex chatCodeRegex = new Regex(@"\[&((?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?)\]", RegexOptions.Compiled);

        public Build(string name, string chatCode)
        {
            Name = name;
            ChatCode = chatCode;

            MatchCollection matches = chatCodeRegex.Matches(chatCode);

            if (matches.Count == 1 && matches[0].Groups.Count == 2)
            {
                string byteString = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(matches[0].Groups[1].Value));
                int[] bytes = new int[byteString.Length];
                for (int i = 0; i < byteString.Length; i++)
                {
                    bytes[i] = (int)byteString[i];
                }

                if (bytes.Length > 0 && bytes[0] != 0x0D)
                {
                    throw new System.Exception("Wrong header type");
                } else if (bytes.Length >= 44)
                {
                    Profession = (Constants.Profession)bytes[1];
                    // Module.Logger.Debug("Profession: {0}", Profession.ToString(), "");

                    Specializations = new List<BuildSpecialization>();
                    for (int s = 0; s < 3; s++)
                    {

                        int offset = s * 2;
                        int[] traits = new int[3];
                        // Specialization ID
                        for (int t = 0; t < 3; t++)
                        {
                            // 2 bit trait values
                            traits[t] = bytes[offset + 3] >> t * 2 & 0x03;
                        }
                        Specializations.Add(new BuildSpecialization((Constants.Specialization)bytes[offset + 2], traits));
                        // Module.Logger.Debug("Trait line: {0} ({1}, {2}, {3})", Specializations[s].Id, Specializations[s].BuildTraits[0], Specializations[s].BuildTraits[1], Specializations[s].BuildTraits[2]);
                    }

                    // TODO skills & specific
                } else
                {
                    throw new System.Exception("Invalid build template");
                }
            } else
            {
                throw new System.Exception("Invalid format");
            }
            Specializations.Sort((x, y) => x.Id.CompareTo(y.Id));
            SerializedBuild = Serialize();
        }

        public bool EquivalentTo(IReadOnlyList<BuildTemplateSpecialization> specializations)
        {
            return SerializedBuild == Build.SerializeFromApi(specializations);
        }

        public string Serialize()
        {
            return string.Join("-", Specializations.Select(x => ((int)x.Id).ToString() + '-' + string.Join("-", x.Traits.Select(y => y.ToString()))));
        }

        public static string SerializeFromApi(IReadOnlyList<BuildTemplateSpecialization> specializations)
        {
            try
            {
                List<BuildTemplateSpecialization> specs = new List<BuildTemplateSpecialization>();
                for (int i = 0; i < 3; i++)
                {
                    if (specializations[i].Id == null)
                    {
                        return "empty";
                    }
                    specs.Add(specializations[i]);
                }
                specs.Sort((x, y) => ((int)x.Id).CompareTo((int)y.Id));
                return string.Join("-", specs.Select(x => x.Id.ToString() + '-' + string.Join("-", x.Traits.Select(y => y.ToString()))));
            } catch (Exception ex)
            {
                Module.Logger.Debug(ex, "Failed to serialize build from API.");
                return "error";
            }
        }
    }
}
