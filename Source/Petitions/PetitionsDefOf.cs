using RimWorld;
using Verse;

namespace Petitions
{
    [DefOf]
    public static class PetitionsDefOf
    {
        // Mood thoughts given to every signer (spec section 2.4).
        public static ThoughtDef Petitions_PetitionHeard;
        public static ThoughtDef Petitions_PromiseKept;
        public static ThoughtDef Petitions_PromiseBroken;
        public static ThoughtDef Petitions_PetitionRejected;
        public static ThoughtDef Petitions_PetitionIgnored;
        public static ThoughtDef Petitions_TalkedItThrough;
        public static ThoughtDef Petitions_PersuasionFailed;

        // Opinion of the negotiator.
        public static ThoughtDef Petitions_RejectedMyPetition;
        public static ThoughtDef Petitions_BrushedMeOff;

        public static HediffDef Petitions_Disgruntled;

        public static LetterDef Petitions_Petition;
        public static LetterDef Petitions_Ultimatum;

        static PetitionsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PetitionsDefOf));
        }
    }
}
