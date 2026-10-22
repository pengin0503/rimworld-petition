using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>
    /// Entry point of the hourly update and owner of all saved petition data (spec section 3.2).
    /// </summary>
    public class PetitionsGameComponent : GameComponent
    {
        public const int UpdateIntervalTicks = 2500;

        private const float HoursPerUpdate = (float)UpdateIntervalTicks / GenDate.TicksPerHour;

        private const int MaxClosedPetitionsKept = 30;

        private List<PawnGrievanceState> states = new List<PawnGrievanceState>();

        private List<Petition> petitions = new List<Petition>();

        private int nextPetitionId = 1;

        public int lastColonyPetitionTick = -1;

        private readonly Dictionary<GrievanceDef, float> tmpSeverities = new Dictionary<GrievanceDef, float>();

        public PetitionsGameComponent(Game game)
        {
        }

        public static PetitionsGameComponent Instance => Current.Game?.GetComponent<PetitionsGameComponent>();

        public IReadOnlyList<PawnGrievanceState> States => states;

        public IReadOnlyList<Petition> Petitions => petitions;

        public int OpenPetitionCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < petitions.Count; i++)
                {
                    if (petitions[i].state == PetitionState.Open)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public static float EffectiveThreshold(GrievanceDef grievance)
        {
            return grievance.pressureThreshold * PetitionsMod.Settings.thresholdMultiplier;
        }

        public Petition GetPetition(int id)
        {
            for (int i = 0; i < petitions.Count; i++)
            {
                if (petitions[i].id == id)
                {
                    return petitions[i];
                }
            }
            return null;
        }

        /// <summary>Whether the pawn signed an open or promised petition about this grievance.</summary>
        public bool HasActivePetition(Pawn pawn, GrievanceDef grievance)
        {
            for (int i = 0; i < petitions.Count; i++)
            {
                Petition petition = petitions[i];
                if (petition.IsActive && petition.grievance == grievance && petition.HasSigner(pawn))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Whether the pawn signed any promised petition (used by the ultimatum).</summary>
        public bool HasPendingPromise(Pawn pawn)
        {
            for (int i = 0; i < petitions.Count; i++)
            {
                if (petitions[i].state == PetitionState.Promised && petitions[i].HasSigner(pawn))
                {
                    return true;
                }
            }
            return false;
        }

        public Petition AddPetition(GrievanceDef grievance, List<Pawn> signers, int nowTick, bool fromUltimatum)
        {
            Petition petition = new Petition
            {
                id = nextPetitionId++,
                grievance = grievance,
                signers = signers,
                createdTick = nowTick,
                fromUltimatum = fromUltimatum,
            };
            for (int i = 0; i < signers.Count; i++)
            {
                petition.clearSinceTicks.Add(-1);
            }
            petitions.Add(petition);
            return petition;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % UpdateIntervalTicks != 0)
            {
                return;
            }
            Update(HoursPerUpdate);
        }

        /// <summary>Runs one hourly update immediately. Used by debug actions.</summary>
        public void UpdateNow()
        {
            Update(HoursPerUpdate);
        }

        public PawnGrievanceState TryGetState(Pawn pawn)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].pawn == pawn)
                {
                    return states[i];
                }
            }
            return null;
        }

        public PawnGrievanceState GetOrCreateState(Pawn pawn)
        {
            PawnGrievanceState state = TryGetState(pawn);
            if (state == null)
            {
                state = new PawnGrievanceState(pawn);
                states.Add(state);
            }
            return state;
        }

        /// <summary>The hourly update, in the order of spec section 3.2.</summary>
        private void Update(float hours)
        {
            int now = Find.TickManager.TicksGame;
            RemoveStaleStates();
            UpdatePressure(hours);
            UpdatePromises(now);
            ExpireOpenPetitions(now);
            TryCreatePetition(now);
            PruneClosedPetitions();
        }

        /// <summary>Step 3.</summary>
        private void UpdatePromises(int now)
        {
            for (int i = 0; i < petitions.Count; i++)
            {
                PromiseTracker.Update(petitions[i], now);
            }
        }

        /// <summary>Keeps only the most recent closed petitions, for the Petitions tab history.</summary>
        private void PruneClosedPetitions()
        {
            int closed = 0;
            for (int i = petitions.Count - 1; i >= 0; i--)
            {
                if (!petitions[i].IsActive && ++closed > MaxClosedPetitionsKept)
                {
                    petitions.RemoveAt(i);
                }
            }
        }

        /// <summary>Steps 1-2: scan thoughts and update pressure. Also remembers severities for promise tracking.</summary>
        private void UpdatePressure(float hours)
        {
            float decayPerDay = PetitionsMod.Settings.pressureDecayPerDay;
            List<GrievanceDef> grievances = DefDatabase<GrievanceDef>.AllDefsListForReading;
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (!PetitionEligibility.CanHaveGrievances(pawn))
                {
                    continue;
                }
                PawnGrievanceState state = GetOrCreateState(pawn);
                GrievanceScanner.Scan(pawn, tmpSeverities);
                float mood = pawn.needs.mood.CurLevel;
                for (int i = 0; i < grievances.Count; i++)
                {
                    GrievanceDef grievance = grievances[i];
                    tmpSeverities.TryGetValue(grievance, out float severity);
                    state.SetPressure(grievance, PressureModel.Step(state.GetPressure(grievance), severity, mood, decayPerDay, hours));
                }
            }
            tmpSeverities.Clear();
        }

        /// <summary>Step 4.</summary>
        private void ExpireOpenPetitions(int now)
        {
            int expiryTicks = (int)(PetitionsMod.Settings.petitionExpiryDays * GenDate.TicksPerDay);
            for (int i = 0; i < petitions.Count; i++)
            {
                Petition petition = petitions[i];
                if (petition.state == PetitionState.Open && now - petition.createdTick >= expiryTicks)
                {
                    PetitionResponder.Expire(petition);
                }
            }
        }

        /// <summary>Step 6.</summary>
        private void TryCreatePetition(int now)
        {
            Petition petition = PetitionFactory.TryCreate(this, now);
            if (petition != null)
            {
                Deliver(petition);
            }
        }

        /// <summary>Sends the letter, or answers right away when the player chose an auto-response.</summary>
        public static void Deliver(Petition petition)
        {
            switch (PetitionsMod.Settings.AutoResponseFor(petition.grievance))
            {
                case AutoResponse.AlwaysAccept:
                    PetitionResponder.Accept(petition);
                    break;
                case AutoResponse.AlwaysReject:
                    PetitionResponder.Reject(petition);
                    break;
                default:
                    ChoiceLetter_Petition.Send(petition);
                    break;
            }
        }

        public static bool IsGone(Pawn pawn)
        {
            return pawn == null || pawn.Dead || pawn.Faction != Faction.OfPlayer;
        }

        /// <summary>
        /// Drops data of pawns who died or left the colony. Pawns who are only away
        /// (caravans) keep their data; it just isn't updated until they return.
        /// </summary>
        private void RemoveStaleStates()
        {
            states.RemoveAll(state => IsGone(state.pawn));
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "states", LookMode.Deep);
            Scribe_Collections.Look(ref petitions, "petitions", LookMode.Deep);
            Scribe_Values.Look(ref nextPetitionId, "nextPetitionId", 1);
            Scribe_Values.Look(ref lastColonyPetitionTick, "lastColonyPetitionTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Saves from the skeleton version have no petitions.
                states ??= new List<PawnGrievanceState>();
                petitions ??= new List<Petition>();
                petitions.RemoveAll(petition => petition == null || petition.grievance == null);
                RemoveStaleStates();
            }
        }
    }
}
