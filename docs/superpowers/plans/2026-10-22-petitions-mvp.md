# 陳情と交渉 MVP 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 骨組み(不満圧の蓄積)の上に、陳情の発生・応答・約束・恨みのエスカレーションまでを実装し、設計書 §8 の段階2(MVP)を完成させる。

**Architecture:** 判定や数値の計算は `Source/Petitions/Core/` の純関数として書き、xUnit でテストする。ゲームに依存する処理は `PetitionsGameComponent` の1時間ごとの更新から呼び出す。陳情の保存データは `Petition` に、入植者ごとの状態は `PawnGrievanceState` に置く。応答は `PetitionResponder` に集約し、手紙・陳情タブ・自動応答の3つの入口から同じ処理を呼ぶ。

**Tech Stack:** C# / net10.0、RimWorld 1.7(参照パッケージ `Krafs.Rimworld.Ref` 1.7.4982-unstable)、xUnit v3、Harmony なし。

**Spec:** `docs/superpowers/specs/2026-10-22-petitions-design.md`

## Global Constraints

- 対象は RimWorld 1.7 のみ。`net10.0`、`TreatWarningsAsErrors`。Harmony とゲームの DLL はリポジトリに入れない。
- Core(`Source/Petitions/Core/`)は RimWorld の型を参照しない。
- 数値は設計書 §2.2・§2.4・§2.6・§2.7 の値をそのまま使う。
- 離脱は既定で有効。軽度の精神崩壊は抑えない(バニラの精神崩壊には一切触れない)。
- 文字列はすべて翻訳キーにし、英語と日本語の両方を用意する。defName と翻訳キーの接頭辞は `Petitions_`。
- 各タスクの終わりに必ず次の4つを通す: `dotnet build Source/Petitions.slnx -c Release`、`dotnet test --solution Source/Petitions.slnx`、`python3 -I tools/validate_mod_xml.py --core /home/user/rimworld-ref/rimworld_DLL/Data/Core/Defs`、1.7.4985 の参照でのコンパイル確認(Task 8)。

## Review Focus

1. **署名者が途中で死亡・離脱・キャラバンに出る**: 陳情と約束はその署名者を外して続ける。署名者が0人になったら、効果なしで閉じる(Withdrawn)。例外は出さない。
2. **交渉役がいない**: 入植者が1人のとき、または全員が署名者のとき。説得は無効にし、理由を表示する。却下で付ける対人思考は省く。
3. **陳情の手紙を開いたまま状態が変わる**: 開いている間に失効したり、陳情タブから応答されたりした場合。古い手紙からは二度と応答できないようにする(応答前に状態が Open かを確認する)。
4. **途中のセーブへの導入と、ロード後**: 骨組み版のセーブ(`states` だけがある)を読み込み、新しい項目が既定値になること。セーブとロードで陳情・約束・恨みが保たれること。
5. **最後通牒の最中に約束を結ぶ**: 期限が来ても、約束の結果が出るまで去らない。約束が破られたら即座に去る。離脱を無効にしている場合は、最後通牒の手紙を出さず、離脱もしない。

---

## File Structure

| ファイル | 役割 |
|---|---|
| `Source/Petitions/Core/PetitionOutcome.cs` (新規) | `PetitionOutcome` 列挙と、結果ごとの恨みの増減 `OutcomeEffects.ResentmentDelta` |
| `Source/Petitions/Core/PromiseEvaluator.cs` (新規) | 約束の判定(Remedy / Cessation) |
| `Source/Petitions/Core/UltimatumModel.cs` (新規) | 最後通牒の期限が来たときの判定 |
| `Source/Petitions/Core/CosignerSelector.cs` (新規) | 連名者の選び方 |
| `Source/Petitions/Petition.cs` (新規) | 1件の陳情(保存データ) |
| `Source/Petitions/PawnGrievanceState.cs` (変更) | 恨み・段階・最後通牒の期限・クールダウンを追加 |
| `Source/Petitions/PetitionsDefOf.cs` (新規) | 独自 Def の `[DefOf]` |
| `Source/Petitions/Negotiator.cs` (新規) | 交渉役の決定と説得成功率 |
| `Source/Petitions/PetitionFactory.cs` (新規) | 候補の選択、連名、陳情の作成 |
| `Source/Petitions/PetitionResponder.cs` (新規) | 応答(受諾・説得・却下・失効)と結果の適用 |
| `Source/Petitions/PromiseTracker.cs` (新規) | 約束中の陳情の評価 |
| `Source/Petitions/Escalation.cs` (新規) | 恨みの減衰、段階の変化、ヘディフ、最後通牒、離脱 |
| `Source/Petitions/ChoiceLetter_Petition.cs` / `ChoiceLetter_Ultimatum.cs` (新規) | 手紙 |
| `Source/Petitions/PetitionsGameComponent.cs` (変更) | 更新の順序(設計書 §3.2)と保存 |
| `Source/Petitions/PetitionsMod.cs` (変更) | 設定の追加 |
| `Source/Petitions/MainTabWindow_Petitions.cs` (変更) | 3つのセクション |
| `Source/Petitions/PetitionsDebugActions.cs` (変更) | 陳情の強制、恨みの操作 |
| `Common/Defs/ThoughtDefs/Thoughts_Petitions.xml`、`HediffDefs/Hediffs_Petitions.xml`、`LetterDefs/Letters_Petitions.xml` (新規) | 思考・ヘディフ・手紙の Def |
| `Common/Defs/GrievanceDefs/Grievances_Core.xml` (変更) | 閾値: Recreation 30、Cessation 20 |
| `Common/Languages/{English,Japanese}/…` (変更・新規) | 翻訳 |
| `tools/validate_mod_xml.py` (変更) | 独自の ThoughtDef / HediffDef / LetterDef の翻訳チェック、DefOf の存在チェック |

---

### Task 1: Core — 結果・約束・最後通牒・連名の純ロジック

**Files:**
- Create: `Source/Petitions/Core/PetitionOutcome.cs`、`PromiseEvaluator.cs`、`UltimatumModel.cs`、`CosignerSelector.cs`
- Test: `Source/Petitions.Tests/OutcomeEffectsTests.cs`、`PromiseEvaluatorTests.cs`、`UltimatumModelTests.cs`、`CosignerSelectorTests.cs`

**Interfaces:**
- Produces:
  - `enum PetitionOutcome { Accepted, PromiseKept, PromiseBroken, Rejected, Ignored, Persuaded, PersuasionFailed }`
  - `static float OutcomeEffects.ResentmentDelta(PetitionOutcome outcome, int cosigners)`。基本値は Accepted −10、PromiseKept −25、PromiseBroken +35、Rejected +20、Ignored +20、Persuaded −5、PersuasionFailed +30。正の値にだけ `ResentmentModel.ScaleIncrease` を掛ける。
  - `enum PromiseStatus { Pending, Kept, Broken }`
  - `static PromiseStatus PromiseEvaluator.EvaluateRemedy(int nowTick, int deadlineTick, int holdTicks, IReadOnlyList<int> clearSinceTicks)`。`clearSinceTicks` は現在いる署名者ごとの値で、思考が出ていれば −1。全員が `now − clearSince ≥ holdTicks` なら Kept。そうでなく `now ≥ deadline` なら Broken。それ以外は Pending。リストが空なら Pending(呼び出し側で Withdrawn にする)。
  - `static PromiseStatus PromiseEvaluator.EvaluateCessation(int nowTick, int deadlineTick, bool newOccurrence)`。newOccurrence なら Broken、`now ≥ deadline` なら Kept、それ以外は Pending。
  - `enum UltimatumDecision { Wait, Leave, Resolved }`
  - `static UltimatumDecision UltimatumModel.Decide(int nowTick, int deadlineTick, ResentmentStage stage, bool hasPendingPromise)`。stage < Ultimatum なら Resolved。約束中なら Wait。`now ≥ deadline` なら Leave。それ以外は Wait。
  - `const int UltimatumModel.DeadlineTicks = 3 * 60000`
  - `static List<int> CosignerSelector.Select(IReadOnlyList<float> pressures, int petitionerIndex, float threshold, float fraction, int max)`。petitioner を除き、`pressure ≥ threshold × fraction` のものを、圧の降順で最大 `max` 件まで返す(インデックスのリスト)。
  - `const int CosignerSelector.MaxCosigners = 5`

- [ ] **Step 1: 失敗するテストを書く**

```csharp
// OutcomeEffectsTests
Assert.Equal(-10f, OutcomeEffects.ResentmentDelta(PetitionOutcome.Accepted, 3), 4);    // 減少には補正なし
Assert.Equal(26f, OutcomeEffects.ResentmentDelta(PetitionOutcome.Rejected, 3), 4);     // 20 × 1.3
Assert.Equal(52.5f, OutcomeEffects.ResentmentDelta(PetitionOutcome.PromiseBroken, 9), 4); // 35 × 1.5
Assert.Equal(30f, OutcomeEffects.ResentmentDelta(PetitionOutcome.PersuasionFailed, 0), 4);
// PromiseEvaluatorTests
Assert.Equal(PromiseStatus.Kept, EvaluateRemedy(now: 100_000, deadline: 200_000, hold: 60_000, new[] { 40_000, 30_000 }));
Assert.Equal(PromiseStatus.Pending, EvaluateRemedy(100_000, 200_000, 60_000, new[] { 40_000, -1 }));
Assert.Equal(PromiseStatus.Broken, EvaluateRemedy(200_000, 200_000, 60_000, new[] { 190_000 }));
Assert.Equal(PromiseStatus.Kept, EvaluateRemedy(200_000, 200_000, 60_000, new[] { 140_000 })); // 期限ちょうどでも達成が優先
Assert.Equal(PromiseStatus.Pending, EvaluateRemedy(100_000, 200_000, 60_000, new int[0]));
Assert.Equal(PromiseStatus.Broken, EvaluateCessation(100, 1000, newOccurrence: true));
Assert.Equal(PromiseStatus.Kept, EvaluateCessation(1000, 1000, false));
Assert.Equal(PromiseStatus.Pending, EvaluateCessation(999, 1000, false));
// UltimatumModelTests
Assert.Equal(UltimatumDecision.Resolved, Decide(5, 10, ResentmentStage.Disgruntled, false));
Assert.Equal(UltimatumDecision.Wait, Decide(20, 10, ResentmentStage.Ultimatum, hasPendingPromise: true));
Assert.Equal(UltimatumDecision.Leave, Decide(10, 10, ResentmentStage.Ultimatum, false));
Assert.Equal(UltimatumDecision.Wait, Decide(9, 10, ResentmentStage.Ultimatum, false));
// CosignerSelectorTests: pressures {30, 5, 12.5, 20, 12.4, 25, 26, 27, 28}, petitioner 0, threshold 25, fraction 0.5, max 5
//   → [8, 7, 6, 5, 3](12.5 以上で、上位5件。petitioner と 12.4、5 は除外)
```

- [ ] **Step 2: テストを実行して失敗を確認する** — `dotnet test --solution Source/Petitions.slnx` が、型が存在しないというコンパイルエラーで失敗する。
- [ ] **Step 3: 上の Interfaces どおりに4ファイルを実装する**
- [ ] **Step 4: テストを実行して全件が通ることを確認する**
- [ ] **Step 5: コミット** `feat(core): outcome, promise, ultimatum and cosigner logic`

### Task 2: Def と翻訳 — 思考・ヘディフ・手紙・閾値

**Files:**
- Create: `Common/Defs/ThoughtDefs/Thoughts_Petitions.xml`、`Common/Defs/HediffDefs/Hediffs_Petitions.xml`、`Common/Defs/LetterDefs/Letters_Petitions.xml`、`Source/Petitions/PetitionsDefOf.cs`、日本語の DefInjected(`ThoughtDef/`、`HediffDef/`、`LetterDef/` 配下)
- Modify: `Common/Defs/GrievanceDefs/Grievances_Core.xml`、`tools/validate_mod_xml.py`

**Interfaces:**
- Produces: `PetitionsDefOf` に次の Def。
  - 気分思考(`ThoughtDef`、`durationDays` と `stages/li/baseMoodEffect`、`stackLimit` 3)
    - `Petitions_PetitionHeard` +4・4日
    - `Petitions_PromiseKept` +8・8日
    - `Petitions_PromiseBroken` −12・10日
    - `Petitions_PetitionRejected` −5・5日
    - `Petitions_PetitionIgnored` −6・5日
    - `Petitions_TalkedItThrough` +2・2日
    - `Petitions_PersuasionFailed` −8・5日
  - 対人思考(`thoughtClass` は `Thought_MemorySocial`、`stackLimit` 10、`stackLimitForSameOtherPawn` 3、`stackedEffectMultiplier` 0.9)
    - `Petitions_RejectedMyPetition` 意見 −8・10日
    - `Petitions_BrushedMeOff` 意見 −15・15日
  - ヘディフ `Petitions_Disgruntled`(`HediffWithComps`、`isBad` true、stage の statFactors `WorkSpeedGlobal` 0.85)
  - 手紙 `Petitions_Petition`(`letterClass` は `Petitions.ChoiceLetter_Petition`)、`Petitions_Ultimatum`(`Petitions.ChoiceLetter_Ultimatum`)。どちらも `ParentName="CustomChoiceLetterBase"`。
- 閾値: `Petitions_Recreation` を 30、`Petitions_CessationBase` を 20 にする。

- [ ] **Step 1: 検証スクリプトに「Def の翻訳漏れ」と「DefOf と XML の対応」チェックを足す。** 具体的には、`Common/Defs` 内のすべての `ThoughtDef`・`HediffDef`・`LetterDef` について、日本語の DefInjected に label(思考は `stages.0.label` と `stages.0.description`)があるか。`PetitionsDefOf.cs` の各フィールド名が XML の defName に存在するか。
- [ ] **Step 2: スクリプトを実行して失敗を確認する**(`PetitionsDefOf.cs` がない、Def がない)。
- [ ] **Step 3: XML・DefOf・翻訳を書く。** `[DefOf]` クラスの静的コンストラクタで `DefOfHelper.EnsureInitializedInCtor(typeof(PetitionsDefOf))` を呼ぶ。
- [ ] **Step 4: 検証・ビルド・テストを通す。**
- [ ] **Step 5: コミット** `feat(defs): petition thoughts, hediff, letters and tuned thresholds`

### Task 3: 保存データ — Petition と PawnGrievanceState の拡張

**Files:**
- Create: `Source/Petitions/Petition.cs`
- Modify: `Source/Petitions/PawnGrievanceState.cs`、`Source/Petitions/PetitionsGameComponent.cs`(保存とロード)

**Interfaces:**
- Produces:
  - `enum PetitionState { Open, Promised, Kept, Broken, Rejected, Ignored, Persuaded, PersuasionFailed, Withdrawn }`
  - `class Petition : IExposable` のフィールド: `int id`、`GrievanceDef grievance`、`List<Pawn> signers`(先頭が陳情者)、`List<int> clearSinceTicks`(signers と同じ並び。Remedy の約束中だけ使う)、`int createdTick`、`PetitionState state`、`int promiseStartTick = -1`、`int promiseDeadlineTick = -1`、`bool fromUltimatum`。プロパティ: `Pawn Petitioner`、`int CosignerCount`、`bool IsActive`(Open または Promised)。メソッド: `void RemoveSigner(Pawn)`(clearSinceTicks も同じ位置を消す)、`bool HasSigner(Pawn)`。
  - `PawnGrievanceState` に追加: `float resentment`、`ResentmentStage stage`、`int ultimatumDeadlineTick = -1`、`int lastPetitionTick = -1`、`Dictionary<GrievanceDef,int> lastPetitionTickByGrievance`、`int LastPetitionTick(GrievanceDef)`(なければ −1)。
  - `PetitionsGameComponent` に追加: `List<Petition> petitions`、`int nextPetitionId`、`int lastColonyPetitionTick = -1`、`IReadOnlyList<Petition> Petitions`、`Petition GetPetition(int id)`、`int OpenPetitionCount`、`bool HasActivePetition(Pawn, GrievanceDef)`、`PawnGrievanceState TryGetState(Pawn)`。
- 保存: 署名者は `Scribe_Collections.Look(ref signers, "signers", LookMode.Reference)`。PostLoadInit で null の署名者を(clearSinceTicks と合わせて)除き、`grievance` が null の陳情は捨てる。骨組み版のセーブでは新しい項目が欠けるが、既定値で補う。

- [ ] **Step 1: 実装する。** ゲームなしではテストできない層なので、Core に寄せられる判定は Task 1 で済ませてある。
- [ ] **Step 2: ビルド・テスト・検証を通す。**
- [ ] **Step 3: コミット** `feat: petition and per-pawn state persistence`

### Task 4: 陳情の作成 — 候補・頻度制限・連名・交渉役・手紙

**Files:**
- Create: `Source/Petitions/Negotiator.cs`、`Source/Petitions/PetitionFactory.cs`、`Source/Petitions/ChoiceLetter_Petition.cs`
- Modify: `Source/Petitions/PetitionsGameComponent.cs`、`Source/Petitions/PetitionsMod.cs`、翻訳

**Interfaces:**
- Consumes: Task 1 の `CosignerSelector`、骨組みの `PetitionRateLimiter` と `PersuasionModel`、Task 3 の保存データ。
- Produces:
  - `static Pawn Negotiator.Find(IReadOnlyCollection<Pawn> excludedSigners)`。イデオロギーが有効なら、プレイヤーの主イデオロギーの `IdeoRole_Leader` の `ChosenPawnSingle()` を使う。ただしその人が陳情できる入植者で、署名者でない場合に限る。いなければ、陳情できる入植者のうち署名者でない人から、`NegotiationAbility` が最大の人を選ぶ。誰もいなければ null。
  - `static float Negotiator.PersuasionChance(Pawn negotiator, Petition petition)`。`PersuasionModel` に、署名者全員の交渉役への意見の平均(整数に丸める)、連名者数、`persuasionDifficulty` を渡す。
  - `static Petition PetitionFactory.TryCreate(PetitionsGameComponent comp, int nowTick)`。設計書 §2.3 の手順で最大1件作る。条件は `PetitionEligibility.CanHaveGrievances`、精神状態異常でない、行動不能でない、同じ不満の進行中の陳情に加わっていない、頻度制限を通ること。署名者全員の該当する不満圧を 0 にし、陳情者の `lastPetitionTick` と `lastPetitionTickByGrievance` を更新し、`lastColonyPetitionTick` を更新する。
  - 設定(`PetitionsSettings`)に追加: `minDaysBetweenPetitions` 2、`maxOpenPetitions` 2、`pawnCooldownDays` 5、`grievanceCooldownDays` 10、`petitionExpiryDays` 2、`enableDisgruntled` true、`enableUltimatum` true、`Dictionary<string, AutoResponse> autoResponses`(キーは defName。`enum AutoResponse { Ask, AlwaysAccept, AlwaysReject }`)、`RateLimits ToRateLimits()`、`AutoResponse AutoResponseFor(GrievanceDef)`。
  - `ChoiceLetter_Petition : ChoiceLetter` の保存項目は `int petitionId`。選択肢は「受諾」「説得(成功率を表示。交渉役がいなければ理由付きで無効)」「却下」「後で決める」(`Option_Postpone`)。陳情が Open でなければ `Option_Close` だけ。`CanShowInLetterStack` は陳情が Open のときだけ true。本文は作成時に組み立てる。内容は要求文、署名者、主な原因の思考の上位3件(陳情者の現在の思考のうち、その不満に該当するもの)、交渉役と成功率、各選択肢の結果の要約。
- 更新の順序(`PetitionsGameComponent.Update`): 設計書 §3.2 の 1→6。手順6は自動応答の方針が Ask のとき手紙を送り、それ以外は Task 5 の `PetitionResponder` を直接呼んで Message で知らせる。

- [ ] **Step 1: 実装する。**
- [ ] **Step 2: ビルド・テスト・検証(翻訳キーの過不足)を通す。**
- [ ] **Step 3: コミット** `feat: petition creation, negotiator and petition letter`

### Task 5: 応答と結果

**Files:**
- Create: `Source/Petitions/PetitionResponder.cs`
- Modify: `ChoiceLetter_Petition.cs`(選択肢から呼ぶ)、`PetitionsGameComponent.cs`(失効)、翻訳

**Interfaces:**
- Consumes: Task 1 の `OutcomeEffects`、Task 2 の `PetitionsDefOf`、Task 4 の `Negotiator`。
- Produces:
  - `static void PetitionResponder.Accept(Petition)`: 状態を Promised にし、`promiseStartTick` と `promiseDeadlineTick`(`promiseDays` 日後)を設定する。Remedy なら `clearSinceTicks` を −1 で初期化する。結果は Accepted。
  - `static void PetitionResponder.Persuade(Petition)`: `Rand.Chance(PersuasionChance)` で判定し、Persuaded か PersuasionFailed にする。失敗時は署名者全員に、交渉役への `Petitions_BrushedMeOff` を付ける。
  - `static void PetitionResponder.Reject(Petition)`: Rejected にし、交渉役がいれば `Petitions_RejectedMyPetition` を付ける。
  - `static void PetitionResponder.Expire(Petition)`: Ignored にする。
  - `static void PetitionResponder.ApplyOutcome(Petition, PetitionOutcome)`: 署名者全員に、結果に対応する気分思考と恨みの増減(`ResentmentModel.Clamp`)を適用する。
  - 共通の前処理として、`state != Open` なら何もしない(Review Focus 3)。応答後は、該当する手紙をレターの一覧から外す(`Find.LetterStack.RemoveLetter`)。結果は Message で知らせる。
- 失効: 手順4で、`now − createdTick ≥ petitionExpiryDays × 60000` の Open な陳情に `Expire` を呼ぶ。

- [ ] **Step 1: 実装する。**
- [ ] **Step 2: ビルド・テスト・検証を通す。**
- [ ] **Step 3: コミット** `feat: petition responses and outcomes`

### Task 6: 約束の追跡

**Files:**
- Create: `Source/Petitions/PromiseTracker.cs`
- Modify: `PetitionsGameComponent.cs`(手順2・3)

**Interfaces:**
- Consumes: Task 1 の `PromiseEvaluator`、Task 5 の `ApplyOutcome`、手順1の severity。
- Produces: `static void PromiseTracker.Update(Petition p, int nowTick, Func<Pawn, GrievanceDef, float> severityOf)`。
  - 最初に、いなくなった署名者(死亡・勢力離脱)を外す。署名者が0人なら Withdrawn。
  - Remedy: 拠点にいる署名者については、severity が 0 なら `clearSince` を設定し(すでに設定済みなら保つ)、0 より大きければ −1 に戻す。評価の対象は拠点にいる署名者だけ。対象が空なら Pending のまま待つ。
  - Cessation: 署名者のいずれかに、その不満の思考の記憶で `age < now − promiseStartTick` のものがあれば newOccurrence とする。
  - 判定が Kept なら PromiseKept、Broken なら PromiseBroken を適用する。結果は手紙(良い知らせ/悪い知らせ)で知らせる。

- [ ] **Step 1: 実装する。**
- [ ] **Step 2: ビルド・テスト・検証を通す。**
- [ ] **Step 3: コミット** `feat: promise tracking for remedy and cessation`

### Task 7: エスカレーション — 減衰・段階・怠業・最後通牒・離脱

**Files:**
- Create: `Source/Petitions/Escalation.cs`、`Source/Petitions/ChoiceLetter_Ultimatum.cs`
- Modify: `PetitionsGameComponent.cs`(手順5)、翻訳

**Interfaces:**
- Consumes: `ResentmentModel`、Task 1 の `UltimatumModel`、Task 5 の `Accept` の効果。
- Produces:
  - `static void Escalation.Update(PawnGrievanceState s, int nowTick, float days)`。手順は次のとおり。
    1. 恨みを減衰させる。
    2. `NextStage` で新しい段階を求め、段階が変わったら通知する(段階1・2は Message)。
    3. 怠業: 段階が2以上で `enableDisgruntled` なら、ヘディフ `Petitions_Disgruntled` を付ける。条件を外れたら取り除く。
    4. 段階3に入ったとき、`enableUltimatum` なら期限を設定し、`ChoiceLetter_Ultimatum` を送る。
    5. 期限の判定は `UltimatumModel.Decide` で行う。Leave なら、入植者が陳情できる状態のときに限り、バニラの `MentalStateDefOf.GiveUpExit` を `TryStartMentalState(reason: …, forceWake: true)` で開始し、期限を −1 に戻す。Resolved なら期限を −1 に戻す。
  - 約束が破られた陳情(`fromUltimatum` を含む)の署名者で、期限が設定されていて段階が3の人は、次の更新で即座に去る。具体的には、`PromiseTracker` で Broken になった時点で、その署名者の `ultimatumDeadlineTick` を `now` にする。
  - `ChoiceLetter_Ultimatum : ChoiceLetter` の保存項目は `Pawn pawn`。選択肢は2つ。
    - 「〔不満〕に対処すると約束する」: 不満圧が最大で、進行中の陳情がない不満について、`fromUltimatum` で Open の陳情を作り、`PetitionResponder.Accept` を呼ぶ。該当する不満がなければ、理由付きで無効にする。
    - 「様子を見る」
  - 離脱の文面には、恨みの原因(直近の陳情の不満)を入れる。

- [ ] **Step 1: 実装する。**
- [ ] **Step 2: ビルド・テスト・検証を通す。**
- [ ] **Step 3: コミット** `feat: resentment escalation, ultimatum and leaving`

### Task 8: 設定画面・陳情タブ・デバッグ操作・仕上げ

**Files:**
- Modify: `PetitionsMod.cs`(設定画面)、`MainTabWindow_Petitions.cs`、`PetitionsDebugActions.cs`、`About/About.xml`、`README.md`、翻訳
- Create: `tools/check_api_compat.sh`

**Interfaces:**
- 設定画面: 骨組みの2項目に、Task 4 の頻度設定、怠業と最後通牒の切り替え、不満ごとの自動応答(ボタンを押すたびに Ask → AlwaysAccept → AlwaysReject を循環)を加える。スクロール付き。
- 陳情タブの3セクション:
  - (a) 未回答の陳情: 署名者、不満、残り時間、受諾/説得/却下ボタン(`PetitionResponder` を呼ぶ)
  - (b) 約束中の陳情: 残り日数。Remedy は「達成まであと何人か」も表示する。
  - (c) 入植者: 恨みのバー、段階、不満圧の上位3件
- デバッグ操作の追加:
  - `Force petition`(入植者を選ぶ): 不満圧が最大の不満で、頻度制限を無視して陳情を作る
  - `Add 20 resentment` / `Clear resentment`
  - `Expire open petitions`
  - `Advance promises to deadline`
- `tools/check_api_compat.sh`: 参照を `Krafs.Rimworld.Ref 1.7.4985-unstable` に差し替えて一時ディレクトリにビルドし、新しいゲームで API が消えていないか確かめる。
- About.xml の説明を、MVP 完成版の内容に更新する。

- [ ] **Step 1: 実装する。**
- [ ] **Step 2: 全検証を実行する。** ビルド(NuGet の参照・ローカルの DLL・1.7.4985 の参照の3通り)、テスト、XML 検証。
- [ ] **Step 3: コミットしてプッシュする** `feat: settings, petitions tab and debug actions`

### Task 9: ブランチ全体のレビュー

- [ ] 設計書と差分を突き合わせ、Review Focus の5項目を1つずつコードで確かめる。見つかった問題は修正し、全検証をやり直してからコミット・プッシュする。
