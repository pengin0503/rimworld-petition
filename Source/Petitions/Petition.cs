using System.Collections.Generic;
using Petitions.Core;
using Verse;

namespace Petitions
{
    public enum PetitionState
    {
        /// <summary>Waiting for the player's answer.</summary>
        Open,

        /// <summary>Accepted; the promise is being tracked.</summary>
        Promised,

        Kept,
        Broken,
        Rejected,
        Ignored,
        Persuaded,
        PersuasionFailed,

        /// <summary>Closed without effect because no signer is left.</summary>
        Withdrawn,

        /// <summary>Remedy promise closed without effect because no signer was at home to judge it.</summary>
        Unjudged,
    }

    /// <summary>One petition and, once accepted, its promise (spec sections 2.3-2.4).</summary>
    public class Petition : IExposable
    {
        public int id;

        public GrievanceDef grievance;

        /// <summary>The petitioner first, then cosigners.</summary>
        public List<Pawn> signers = new List<Pawn>();

        /// <summary>
        /// Same order as <see cref="signers"/>. Remedy promises only: the tick since which the
        /// grievance's thoughts have been absent for that signer, or -1.
        /// </summary>
        public List<int> clearSinceTicks = new List<int>();

        public int createdTick;

        public PetitionState state = PetitionState.Open;

        public int promiseStartTick = -1;

        public int promiseDeadlineTick = -1;

        /// <summary>Created from an ultimatum letter rather than by the pawn speaking up.</summary>
        public bool fromUltimatum;

        /// <summary>Remedy only: how far the deadline has been moved for signers who were away.</summary>
        public int absenceExtensionTicks;

        /// <summary>Remedy only: at the last update no signer was at home to be judged.</summary>
        public bool nobodyJudgeableLastUpdate;

        public Pawn Petitioner => signers.Count > 0 ? signers[0] : null;

        public int CosignerCount => System.Math.Max(0, signers.Count - 1);

        public bool IsActive => state == PetitionState.Open || state == PetitionState.Promised;

        public bool HasSigner(Pawn pawn)
        {
            return signers.Contains(pawn);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Defs.Look(ref grievance, "grievance");
            Scribe_Collections.Look(ref signers, "signers", LookMode.Reference);
            Scribe_Collections.Look(ref clearSinceTicks, "clearSinceTicks", LookMode.Value);
            Scribe_Values.Look(ref createdTick, "createdTick");
            Scribe_Values.Look(ref state, "state", PetitionState.Open);
            Scribe_Values.Look(ref promiseStartTick, "promiseStartTick", -1);
            Scribe_Values.Look(ref promiseDeadlineTick, "promiseDeadlineTick", -1);
            Scribe_Values.Look(ref fromUltimatum, "fromUltimatum");
            Scribe_Values.Look(ref absenceExtensionTicks, "absenceExtensionTicks");
            Scribe_Values.Look(ref nobodyJudgeableLastUpdate, "nobodyJudgeableLastUpdate");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                signers ??= new List<Pawn>();
                clearSinceTicks ??= new List<int>();
                // Drop signers that no longer resolve; an emptied petition is withdrawn on the next update.
                SignerPruner.Prune(signers, clearSinceTicks, pawn => pawn == null);
            }
        }
    }
}
