# 陳情と交渉 段階3a 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** イデオロギーや他MODの戒律が与える思考でも、不満として拾えるようにする。あわせて、約束後の発生判定の誤差を直し、交渉役の指名と陳情の履歴を加える。

**Architecture:** 照合・許容幅・交渉役の選択を、Core の純関数(ジェネリック)として書いてテストする。ゲーム側は Def から入力を組み立てて呼ぶだけにする。

**Tech Stack:** MVP と同じ(net10.0、Krafs.Rimworld.Ref 1.7.4982-unstable、xUnit v3)。

**Spec:** `docs/superpowers/specs/2026-10-22-petitions-design.md` §10

## Global Constraints

- MVP 計画の Global Constraints をすべて引き継ぐ。
- 検証は、ビルド・テスト・XML 検証(`--core`)・`tools/check_api_compat.sh` の4つ。

## Review Focus

1. **戒律が正の思考を与える出来事**(「処刑は必要」の戒律など): 正の気分は数えない(Scan の `offset < 0` で除外されることを確認する)。
2. **同じ思考が複数の不満の出来事に対応する**: 該当するすべての不満に数える。
3. **指名した交渉役が署名者になる、死亡する、勢力を離れる**: 自動の規則にフォールバックする。保存データに死んだ人の参照を残さない(Pawn の参照が null になれば自動扱い)。
4. **骨組み版や MVP 版のセーブを読み込む**: 交渉役の指名がない状態を「自動」として扱う。
5. **XML の historyEvents に存在しない出来事を書く**: 検証スクリプトで検出する(Core の HistoryEventDef と照合する)。

---

### Task 1: Core — 照合・許容幅・交渉役の選択

**Files:**
- Create: `Source/Petitions/Core/ThoughtEventIndex.cs`、`Source/Petitions/Core/NegotiatorSelector.cs`
- Modify: `Source/Petitions/Core/PromiseEvaluator.cs`
- Test: `Source/Petitions.Tests/ThoughtEventIndexTests.cs`、`NegotiatorSelectorTests.cs`、`PromiseEvaluatorTests.cs`

**Interfaces:**
- Produces:
  - `class ThoughtEventIndex<TThought, TEvent>`
    - コンストラクタ: `IEnumerable<(TThought thought, TEvent ev)>` を受け取る。
    - `bool Matches(TThought thought, ICollection<TThought> directThoughts, ICollection<TEvent> events)`: `directThoughts` に含まれるか、対応表上の出来事が `events` と1つでも重なれば true。
  - `const int PromiseEvaluator.MemoryAgeToleranceTicks = 300`
  - `static bool PromiseEvaluator.IsNewSincePromise(int memoryAge, int ticksSincePromise)`: `memoryAge + 300 < ticksSincePromise`
  - `static T NegotiatorSelector.Select<T>(T assigned, T leader, IEnumerable<(T pawn, float ability)> candidates, Func<T, bool> canNegotiate) where T : class`: 交渉できるなら指名した人、次に指導者、最後に交渉能力が最大の人を返す。誰もいなければ null。

- [ ] Step 1: 失敗するテストを書く。

```csharp
// ThoughtEventIndex: pairs {("KnowExecAbhorrent","ExecutedGuest"),("KnowExecAbhorrent","ExecutedPrisoner"),("Happy","ExecutedGuest")}
Assert.True(index.Matches("KnowExecAbhorrent", direct: {}, events: {"ExecutedPrisoner"}));
Assert.True(index.Matches("KnowGuestExecuted", direct: {"KnowGuestExecuted"}, events: {}));
Assert.False(index.Matches("KnowExecAbhorrent", {}, {"SoldSlave"}));
Assert.False(index.Matches("Unknown", {}, {"ExecutedGuest"}));
// IsNewSincePromise
Assert.True(IsNewSincePromise(100, 2500)); Assert.False(IsNewSincePromise(2300, 2500)); Assert.False(IsNewSincePromise(2200, 2500)); Assert.True(IsNewSincePromise(2199, 2500));
// NegotiatorSelector
Select("A", "L", {("B",1.0),("C",1.5)}, all) == "A"
Select("A", "L", ..., p => p != "A") == "L"
Select(null, null, {("B",1.0),("C",1.5)}, all) == "C"
Select("A", "L", {("A",2.0),("C",1.5)}, p => p == "C") == "C"
Select(null, null, {}, all) == null
```

- [ ] Step 2: 実行して失敗を確認する。
- [ ] Step 3: 実装する。
- [ ] Step 4: 実行して通ることを確認する。
- [ ] Step 5: コミット `feat(core): event index, memory age tolerance, negotiator selection`

### Task 2: 出来事による照合をゲーム側に組み込む

**Files:**
- Modify: `GrievanceDef.cs`(`List<HistoryEventDef> historyEvents`、`bool Matches(ThoughtDef)`、ConfigErrors に「thoughts と historyEvents の両方が空」の検出を加える)、`GrievanceScanner.cs`、`PromiseTracker.cs`、`Common/Defs/GrievanceDefs/Grievances_Core.xml`、`tools/validate_mod_xml.py`(historyEvents の存在確認)
- Create: `Source/Petitions/PreceptThoughtIndex.cs`(全 PreceptDef の comps から、`PreceptComp_KnowsMemoryThought` / `PreceptComp_SelfTookMemoryThought` の (thought, eventDef) の組を集め、`ThoughtEventIndex<ThoughtDef, HistoryEventDef>` を遅延生成して持つ)

**Interfaces:**
- `GrievanceDef.Matches(ThoughtDef thought)` は `PreceptThoughtIndex.Index.Matches(thought, thoughts, historyEvents)` を呼ぶ。
- `GrievanceScanner` と `GrievanceScanner.Causes` と `PromiseTracker.HasNewOccurrence` は、`grievance.thoughts.Contains` の代わりに `Matches` を使う。Scan は、思考ごとに全不満を `Matches` で判定する。
- `PromiseTracker` の発生判定は `PromiseEvaluator.IsNewSincePromise` を使う。
- Cessation の ConfigErrors に「historyEvents で引ける思考は記憶思考に限る」は加えない。戒律の Knows / SelfTook の comps は、どれも記憶思考を与えるため。
- XML の対応は設計書 §10.1 の表のとおり。

- [ ] Step 1: 検証スクリプトに historyEvents の存在確認を足す。XML に historyEvents を追加し、存在しない名前を入れたコピーで、スクリプトが失敗することを確認する。
- [ ] Step 2: 実装し、ビルド・テスト・検証・互換チェックを通す。
- [ ] Step 3: コミット `feat: match grievances by history event so ideology precept thoughts count`

### Task 3: 交渉役の指名と陳情の履歴

**Files:**
- Modify: `PetitionsGameComponent.cs`(`Pawn assignedNegotiator` を参照として保存)、`Negotiator.cs`(`NegotiatorSelector` を使う)、`MainTabWindow_Petitions.cs`(交渉役のドロップダウン、「最近の陳情」区画)、翻訳

**Interfaces:**
- `PetitionsGameComponent.assignedNegotiator`: null なら自動。`IsGone` になったら、更新時に null に戻す。
- ドロップダウン(`FloatMenu`)の選択肢は「自動」と、陳情できる入植者。
- 履歴の区画: 終了した陳情を `id` の降順で表示する。署名者、不満、結果のラベル(`Petitions_Outcome_<State>`)を並べる。

- [ ] Step 1: 実装し、ビルド・テスト・検証(翻訳キーの過不足)・互換チェックを通す。
- [ ] Step 2: コミットしてプッシュする `feat: assign a negotiator and show petition history`

### Task 4: 仕上げ

- [ ] README と About.xml を更新する。全検証を実行し、Review Focus の5項目をコードで確かめる。コミットしてプッシュし、PR の本文を更新する。
