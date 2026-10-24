using UnityEngine;
using Verse;

namespace Petitions
{
    public class PetitionsMod : Mod
    {
        public static PetitionsSettings Settings { get; private set; }

        public PetitionsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PetitionsSettings>();
        }

        public override string SettingsCategory()
        {
            return "Petitions_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            PetitionsSettingsWindow.Draw(inRect, Settings);
        }
    }
}
